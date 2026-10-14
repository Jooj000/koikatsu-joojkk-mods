using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ActionGame;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.MainGame;
using Manager;
using UnityEngine;

namespace Joojkk.CampusSaves
{
    /// <summary>Keeps the campus id inside the save, so the id follows the file wherever it goes.</summary>
    internal sealed class SavesController : GameCustomFunctionController
    {
        protected override void OnGameLoad(GameSaveLoadEventArgs args)
        {
            try
            {
                var d = GetExtendedData();
                object id = null;
                SaveFlow.CampusId = SaveFlow.TakePendingCampus() ?? (d != null && d.data.TryGetValue("id", out id) ? id as string : null);
                SaveFlow.OnLoaded(args.FileName);
            }
            catch (Exception e) { CampusSavesPlugin.Log.LogWarning("Load: " + e.Message); }
        }

        protected override void OnGameSave(GameSaveLoadEventArgs args)
        {
            try
            {
                SaveFlow.EnsureCampus();
                var d = new PluginData();
                d.data["id"] = SaveFlow.CampusId;
                SetExtendedData(d);
            }
            catch (Exception e) { CampusSavesPlugin.Log.LogWarning("Save: " + e.Message); }
        }

        protected override void OnNewGame() { SaveFlow.CampusId = null; }
        protected override void OnPeriodChange(Cycle.Type period) { SaveFlow.Trigger(CampusSavesPlugin.OnPeriod.Value ? "period" : null); }
        protected override void OnEnterNightMenu() { SaveFlow.Trigger(CampusSavesPlugin.OnDayEnd.Value ? "day end" : null); }
        protected override void OnStartH(MonoBehaviour proc, HFlag hFlag, bool vr) { SaveFlow.Dirty = true; }
        protected override void OnEndH(MonoBehaviour proc, HFlag hFlag, bool vr) { SaveFlow.Trigger(CampusSavesPlugin.AfterH.Value ? "after H" : null); }
    }

    /// <summary>
    /// A save goes to a temporary file first and replaces the slot only when it is whole, then becomes a version in the
    /// background. Every save passes here, the game's own screen included. If anything here fails, the game saves as usual.
    /// </summary>
    internal static class SaveFlow
    {
        private const string Temp = "~campus.tmp";
        public static string CampusId;
        public static bool Dirty;
        public static string PendingLoad;              // a rebuilt version to read in place of the slot file
        public static string PendingCampus;            // the campus of that version; an imported one has no id inside
        public static string NextKind = "manual", NextTrigger = "";

        private static string _pendingTrigger;
        private static string _finalName, _resume, _rebuilt;
        private static byte[] _picture;
        private static float _lastSave;
        private static float _resumeAt = -1f;
        private static string[] _resumeData;
        private static bool _armed, _warping;

        private static string GamePath { get { return Path.GetFullPath(Campuses.GameDir).TrimEnd('/', '\\'); } }

        private static bool IsGameDir(string path)
        {
            try { return string.Equals(Path.GetFullPath(path).TrimEnd('/', '\\'), GamePath, StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        public static string TakePendingCampus()
        {
            var c = PendingCampus;
            PendingCampus = null;
            return c;
        }

        /// <summary>A save that left the slot list and came back, or one from before this mod, finds its campus here.</summary>
        public static void EnsureCampus()
        {
            if (_finalName == null) return;
            var save = Singleton<Game>.Instance.saveData;
            var c = CampusId != null ? Campuses.ById(CampusId) : null;
            if (c == null) c = Campuses.BySlot(_finalName);
            if (c == null) c = Campuses.ByName(save.accademyName, save.emblemID);
            if (c == null) c = Campuses.Create(save.accademyName, save.emblemID, _finalName);
            CampusId = c.Id;
        }

        /// <summary>After a crash in the middle of a swap: the old file goes back if the new one never got there.</summary>
        public static void Recover()
        {
            foreach (var old in Directory.GetFiles(Campuses.GameDir, "*.dat.old"))
            {
                var head = old.Substring(0, old.Length - 4);
                if (File.Exists(head)) continue;
                File.Move(old, head);
                CampusSavesPlugin.Log.LogWarning("Restored " + Path.GetFileName(head) + " after an interrupted save");
            }
        }

        // ================= saving =================

        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(SaveData), nameof(SaveData.Save), typeof(string), typeof(string))]
        private static void SavePrefix(string path, ref string fileName)
        {
            try
            {
                _finalName = null;
                if (!IsGameDir(path) || !fileName.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)) return;
                _resume = ResumeNow();
                _picture = Thumbs.Take();
                _finalName = fileName;
                fileName = Temp;
            }
            catch (Exception e) { _finalName = null; CampusSavesPlugin.Log.LogWarning("Save redirect: " + e.Message); }
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(SaveData), nameof(SaveData.Save), typeof(string), typeof(string))]
        private static void SavePostfix(string path)
        {
            if (_finalName == null) return;
            var slot = _finalName;
            _finalName = null;
            try
            {
                var tmp = Path.Combine(path, Temp);
                var head = Path.Combine(path, slot);
                var fi = new FileInfo(tmp);
                if (!fi.Exists || fi.Length < 1024 || SaveHeader.Read(tmp) == null) throw new IOException("the new save is not whole");
                if (!Campuses.WaitIdle(120000)) throw new IOException("the background work did not finish");
                var old = head + ".old";
                if (File.Exists(old)) File.Delete(old);
                if (File.Exists(head)) File.Move(head, old);
                File.Move(tmp, head);
                Absorb(slot, head, old);
            }
            catch (Exception e)
            {
                // The temporary holds the save: it must not be lost because the swap failed.
                CampusSavesPlugin.Log.LogError("Save swap: " + e);
                CampusSavesPlugin.Toast("Save kept as " + Temp + ": " + e.Message);
            }
        }

        /// <summary>
        /// What the slot held is either the last version already, or something this mod never saw (an old save, a save made
        /// with the mod off): that one goes into the vault before it is gone.
        /// </summary>
        private static void Absorb(string slot, string head, string old)
        {
            var c = Campuses.ById(CampusId);
            if (c == null) { if (File.Exists(old)) File.Delete(old); return; }
            if (c.Slot != slot) { c.Slot = slot; c.Write(); }
            var kind = NextKind;
            var v = new SaveVersion { Kind = kind, Trigger = NextTrigger, At = DateTime.Now, Resume = _resume ?? "", HeadTicks = File.GetLastWriteTimeUtc(head).Ticks, Name = Label(), Day = DayNow(), Place = PlaceNow() };
            var picture = _picture;
            _picture = null;
            NextKind = "manual";
            NextTrigger = "";
            var keep = CampusSavesPlugin.KeepAutos.Value;
            Campuses.Run(() =>
            {
                if (File.Exists(old))
                {
                    var known = c.Vault.Versions.LastOrDefault();
                    if (known == null || File.GetLastWriteTimeUtc(old).Ticks != known.HeadTicks)
                        c.Vault.Absorb(old, new SaveVersion { Name = SaveHeader.Label(old), Kind = "imported", Trigger = "found", At = File.GetLastWriteTime(old), HeadTicks = File.GetLastWriteTimeUtc(old).Ticks, Parent = known != null ? known.Id : 0 });
                    File.Delete(old);
                }
                var prev = c.Vault.Versions.LastOrDefault();
                v.Parent = prev != null ? prev.Id : 0;
                var sw = Stopwatch.StartNew();
                var made = c.Vault.Absorb(head, v);
                if (made == null && prev != null) c.Vault.Same(prev, v, kind == "manual");
                var stored = made ?? prev;
                if (stored != null && picture != null) File.WriteAllBytes(c.Vault.PicturePath(stored.Id), picture);
                else if (stored != null) Thumbs.Reuse(c.Vault, stored);
                c.Prune(keep, DateTime.Now);
                CampusSavesPlugin.Log.LogInfo("Stored in " + sw.ElapsedMilliseconds + " ms, pack " + (c.Vault.PackSize / 1000000) + " MB");
            });
            Dirty = false;
            _lastSave = Time.realtimeSinceStartup;
            CampusSavesPlugin.Toast("Saved");
        }

        /// <summary>The weekday and the period: the game has no date and no hour.</summary>
        private static string Label()
        {
            try
            {
                var c = Singleton<Game>.Instance.actScene != null ? Singleton<Game>.Instance.actScene.Cycle : null;
                return c != null ? c.nowWeek + ", " + Period(c.nowType) : "Night";
            }
            catch { return "Save"; }
        }

        private static string Period(Cycle.Type t)
        {
            switch (t)
            {
                case Cycle.Type.LunchTime: return "Lunch break";
                case Cycle.Type.StaffTime: return "Club activity";
                case Cycle.Type.AfterSchool: return "After school";
                case Cycle.Type.MyHouse: return "Night";
                default: return t.ToString();
            }
        }

        /// <summary>The day number, which only Campus Life counts. This mod works without it.</summary>
        private static int DayNow()
        {
            try
            {
                var t = AccessTools.TypeByName("Joojkk.CampusLife.Campus");
                return t != null ? Traverse.Create(t).Field("S").Field("Day").GetValue<int>() : 0;
            }
            catch { return 0; }
        }

        private static string PlaceNow()
        {
            try
            {
                var act = Singleton<Game>.Instance.actScene;
                MapInfo.Param p;
                return act != null && act.Map != null && Roaming(act.Cycle.nowType) && act.Map.infoDic.TryGetValue(act.Map.no, out p) && p != null ? English(p.MapName ?? "") : "";
            }
            catch { return ""; }
        }

        private static readonly string[][] Places =
        {
            new[] { "Library", "\u56F3\u66F8" }, new[] { "Gym", "\u4F53\u80B2\u9928" }, new[] { "Pool", "\u30D7\u30FC\u30EB" },
            new[] { "Rooftop", "\u5C4B\u4E0A" }, new[] { "Infirmary", "\u4FDD\u5065\u5BA4" }, new[] { "Cafeteria", "\u98DF\u5802", "\u5B66\u98DF", "\u8CFC\u8CB7" },
            new[] { "Club room", "\u90E8\u5BA4" }, new[] { "Faculty room", "\u8077\u54E1\u5BA4" }, new[] { "Classroom", "\u6559\u5BA4" },
            new[] { "Courtyard", "\u4E2D\u5EAD" }, new[] { "Field", "\u30B0\u30E9\u30A6\u30F3\u30C9", "\u6821\u5EAD", "\u904B\u52D5\u5834" }, new[] { "Park", "\u516C\u5712" },
        };

        /// <summary>The maps are named in Japanese, which the translator does not reach on this panel.</summary>
        private static string English(string map)
        {
            foreach (var p in Places)
                for (var i = 1; i < p.Length; i++)
                    if (map.Contains(p[i])) return p[0];
            return map;
        }

        /// <summary>Where this save comes back to; empty at night, which is where the game itself comes back.</summary>
        private static string ResumeNow()
        {
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || act.Cycle == null || act.Map == null || act.Player == null || !Roaming(act.Cycle.nowType)) return "";
            var p = act.Player.position;
            var i = CultureInfo.InvariantCulture;
            return string.Join(" ", new[] { act.Cycle.nowType.ToString(), act.Cycle.timer.ToString("0.000", i), act.Map.no.ToString(), p.x.ToString("0.000", i), p.y.ToString("0.000", i), p.z.ToString("0.000", i), act.Player.rotation.eulerAngles.y.ToString("0.0", i) });
        }

        private static bool Roaming(Cycle.Type t) { return t == Cycle.Type.LunchTime || t == Cycle.Type.StaffTime || t == Cycle.Type.AfterSchool; }

        /// <summary>True when a save cannot catch anything half done: free on the map, or in the night menu.</summary>
        public static bool Safe()
        {
            try
            {
                if (!Singleton<Game>.IsInstance() || !Singleton<Scene>.IsInstance()) return false;
                var game = Singleton<Game>.Instance;
                var scene = Singleton<Scene>.Instance;
                var act = game.actScene;
                if (act == null || !act.isActiveAndEnabled || game.saveData == null || game.saveData.isOpening) return false;
                if (scene.IsNowLoadingFade || GameAPI.InsideHScene || act.Map == null || act.Map.isMapLoading) return false;
                var names = scene.NowSceneNames;
                if (act.Cycle.nowType == Cycle.Type.MyHouse) return names.Contains("NightMenu") && names.Count <= 2;
                return names.Count == 1 && names[0] == "Action" && !act.isEventNow && Roaming(act.Cycle.nowType) && act.Cycle.timer > 0.5f;
            }
            catch { return false; }
        }

        public static void Trigger(string reason)
        {
            Dirty = true;
            if (reason != null) _pendingTrigger = reason;
        }

        /// <summary>Saves now, into this campus's slot. Returns what happened, for the toast.</summary>
        public static string SaveNow(string kind, string trigger)
        {
            if (!Safe()) return "Can't save here";
            var game = Singleton<Game>.Instance;
            var c = CampusId != null ? Campuses.ById(CampusId) : null;
            var slot = c != null && c.Slot != "" ? c.Slot : !string.IsNullOrEmpty(Game.SaveFileName) ? Game.SaveFileName : Campuses.FreeSlot();
            if (slot == null) return "No free slot";
            NextKind = kind;
            NextTrigger = trigger;
            Game.SaveFileName = slot;
            game.Save(slot);
            return "Saved";
        }

        public static void Tick()
        {
            if (_resumeAt >= 0f) TickResume();
            if (!Safe() || Campuses.Busy) return;
            var minutes = CampusSavesPlugin.Interval.Value;
            var due = minutes > 0 && Dirty && Time.realtimeSinceStartup - _lastSave > minutes * 60f;
            if (_pendingTrigger == null && !due) return;
            var why = _pendingTrigger ?? "timer";
            _pendingTrigger = null;
            _lastSave = Time.realtimeSinceStartup;
            CampusSavesPlugin.Log.LogInfo("Autosave: " + why + ", " + SaveNow("auto", why));
        }

        // ================= loading =================

        /// <summary>The game's Load screen, on the title and at night, opens the panel over it.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LoadScene), "Start")]
        private static void LoadScreen(LoadScene __instance)
        {
            try { if (CampusSavesPlugin.OverLoad.Value) SavesPanel.OpenOver(__instance); }
            catch (Exception e) { CampusSavesPlugin.Log.LogWarning("Load screen: " + e.Message); }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SaveData), nameof(SaveData.Load), typeof(string), typeof(string))]
        private static void LoadPrefix(ref string path, ref string fileName)
        {
            try
            {
                if (PendingLoad == null) return;
                path = Path.GetDirectoryName(PendingLoad) + Path.DirectorySeparatorChar;
                fileName = Path.GetFileName(PendingLoad);
                _rebuilt = PendingLoad;
                PendingLoad = null;
            }
            catch (Exception e) { PendingLoad = null; CampusSavesPlugin.Log.LogWarning("Load redirect: " + e.Message); }
        }

        /// <summary>The version chosen in the panel, or null to follow the slot file.</summary>
        public static SaveVersion ChosenVersion;

        public static void OnLoaded(string fileName)
        {
            if (_rebuilt != null) { File.Delete(_rebuilt); _rebuilt = null; }
            _armed = false;
            _resumeData = null;
            var c = CampusId != null ? Campuses.ById(CampusId) : Campuses.BySlot(Path.GetFileName(fileName));
            if (c == null)
            {
                var save = Singleton<Game>.Instance.saveData;
                c = Campuses.ByName(save.accademyName, save.emblemID);
            }
            if (c == null) { CampusId = null; ChosenVersion = null; return; }
            CampusId = c.Id;
            var v = ChosenVersion ?? c.HeadVersion();
            ChosenVersion = null;
            if (v == null || v.Resume == "") return;
            _resumeData = v.Resume.Split(' ');
            _armed = _resumeData.Length == 7;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Cycle), nameof(Cycle.Initialize))]
        private static void CycleInit(Cycle __instance)
        {
            try
            {
                if (!_armed) return;
                _armed = false;
                var type = (Cycle.Type)Enum.Parse(typeof(Cycle.Type), _resumeData[0]);
                var t = Traverse.Create(__instance);
                t.Property("_nowType").SetValue(type);
                t.Field("timeCtrl").Property("timeZone").SetValue(t.Method("TimeZoonCast", type).GetValue());
                _resumeAt = Time.realtimeSinceStartup;
                CampusSavesPlugin.Log.LogInfo("Coming back to " + type);
            }
            catch (Exception e) { _resumeAt = -1f; CampusSavesPlugin.Log.LogWarning("Resume: " + e.Message); }
        }

        private static void TickResume()
        {
            try
            {
                if (Time.realtimeSinceStartup - _resumeAt > 300f) { _resumeAt = -1f; return; }
                var game = Singleton<Game>.Instance;
                var act = game.actScene;
                if (act == null || !act.isActiveAndEnabled || act.isEventNow || act.Map == null || act.Map.isMapLoading || act.Cycle.timer <= 0f || Singleton<Scene>.Instance.IsNowLoadingFade) return;
                if (Singleton<Scene>.Instance.NowSceneNames.Count != 1) return;
                var i = CultureInfo.InvariantCulture;
                var d = _resumeData;
                var map = int.Parse(d[2]);
                if (_warping) return;
                if (act.Map.no != map)
                {
                    // Map first; this comes back here once the new map stands.
                    _warping = true;
                    act.Map.PlayerMapWarp(map, () => _warping = false);
                    return;
                }
                Traverse.Create(act.Cycle).Field("_timer").SetValue(float.Parse(d[1], i));
                act.Player.SetPositionAndRotation(new Vector3(float.Parse(d[3], i), float.Parse(d[4], i), float.Parse(d[5], i)), new Vector3(0f, float.Parse(d[6], i), 0f));
                _resumeAt = -1f;
                _lastSave = Time.realtimeSinceStartup;
                CampusSavesPlugin.Log.LogInfo("Back on map " + map);
            }
            catch (Exception e) { _resumeAt = -1f; CampusSavesPlugin.Log.LogWarning("Resume: " + e.Message); }
        }
    }
}
