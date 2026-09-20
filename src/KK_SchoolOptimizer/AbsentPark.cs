using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ExtensibleSaveFormat;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Students who did not come to school today (the day's draw gave them no NPC) keep only their game data in memory.
    /// User, 2026-10-26: "já que algumas alunas faltam, faça com que elas não sejam carregadas em nada que não vai ser
    /// usado no dia. apenas as informações do campus life e outros mods que mexem com faltas", then "informação de corpo
    /// não é usada, estou falando de informação real como a do campus life ou do pregnancy".
    /// <list type="bullet">
    /// clothes overlays as bytes), with no body loaded.</item>
    /// <item>After the day's draw, each absent girl's large mod entries go to a file in BepInEx\cache\joojkk_absent (written,
    /// read back and checked first) and leave her card in memory. Game data stays: KK_Pregnancy, LewdCrestX, ours, and
    /// anything small. Campus Life keeps its data in the save, not on the card.</item>
    /// <item>They come back by themselves the moment anything asks for them: ExtendedSave's getters, a card copy (her body
    /// being built), a card write (the game saving). After a save she is parked again if she is still away.</item>
    /// <item>Data a mod set again meanwhile wins: a parked entry never overwrites a newer one.</item>
    /// </list>
    /// </summary>
    internal static class AbsentPark
    {
        private sealed class Rec
        {
            public string Path, Name;
            public string[] Keys;
            public bool Parked, ParkAfterSave;
            public long Bytes;
            public Dictionary<string, PluginData> Back; // what was put back, to park again without a new file if unchanged
        }

        private static readonly Dictionary<ChaFile, Rec> Recs = new Dictionary<ChaFile, Rec>();
        private static readonly HashSet<string> Keep = new HashSet<string> { "KK_Pregnancy", "LewdCrestX" };
        private const long MinBytes = 32 * 1024;
        private static MethodInfo _ser, _de;
        private static string _dir;
        private static bool _inside;

        public static bool Suspend;
        public static bool TestOff;
        public static int Restores;
        /// <summary>Girls whose data came back for something other than a save: she came to school.</summary>
        public static readonly List<string> Returned = new List<string>();

        public static bool On { get { return !TestOff && SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.ParkAbsent.Value; } }
        public static int ParkedCount { get { return Recs.Values.Count(r => r.Parked); } }
        public static long ParkedBytes { get { return Recs.Values.Where(r => r.Parked).Sum(r => r.Bytes); } }

        public static void Patch(Harmony harmony)
        {
            var map = typeof(Dictionary<string, PluginData>);
            _ser = AccessTools.Method(typeof(ExtendedSave), "MessagePackSerialize");
            _de = AccessTools.Method(typeof(ExtendedSave), "MessagePackDeserialize");
            var all = AccessTools.Method(typeof(ExtendedSave), nameof(ExtendedSave.GetAllExtendedData), new[] { typeof(ChaFile) });
            var byId = AccessTools.Method(typeof(ExtendedSave), nameof(ExtendedSave.GetExtendedDataById), new[] { typeof(ChaFile), typeof(string) });
            var save = AccessTools.Method(typeof(ChaFile), "SaveFile", new[] { typeof(BinaryWriter), typeof(bool) });
            var copy = AccessTools.Method(typeof(ChaFile), "CopyAll");
            var create = AccessTools.Method(typeof(Character), nameof(Character.CreateChara));
            if (_ser == null || _de == null || all == null || byId == null || save == null || copy == null || create == null)
                throw new Exception("ExtendedSave or game methods not found");
            _ser = _ser.MakeGenericMethod(map);
            _de = _de.MakeGenericMethod(map);

            _dir = Path.Combine(BepInEx.Paths.CachePath, "joojkk_absent");
            try { if (Directory.Exists(_dir)) foreach (var f in Directory.GetFiles(_dir, "*.bin")) File.Delete(f); } // last session's: no card points to them
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Absent cache cleanup: " + e.Message); }

            harmony.Patch(all, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(AllPrefix)));
            harmony.Patch(byId, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(ByIdPrefix)));
            harmony.Patch(save, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(SavePrefix)), postfix: new HarmonyMethod(typeof(AbsentPark), nameof(SavePostfix)));
            harmony.Patch(copy, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(CopyPrefix)));
            harmony.Patch(create, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(CreatePrefix)));
            foreach (var m in AccessTools.GetDeclaredMethods(typeof(ChaFile)).Where(x => x.Name == "LoadFile"))
                harmony.Patch(m, prefix: new HarmonyMethod(typeof(AbsentPark), nameof(LoadPrefix)));
        }

        // ---------- hooks: everything that may read her card puts it back first ----------

        private static void AllPrefix(ChaFile file) { Restore(file, "read"); }

        private static void ByIdPrefix(ChaFile file, string id)
        {
            if (Recs.Count == 0 || file == null || id == null) return;
            Rec r;
            if (Recs.TryGetValue(file, out r) && r.Parked && Array.IndexOf(r.Keys, id) >= 0) Restore(file, id);
        }

        private static void CopyPrefix(object[] __args) { if (Recs.Count > 0 && __args.Length > 0) Restore(__args[0] as ChaFile, "copy"); }

        private static void CreatePrefix(ChaFileControl _chaFile) { Restore(_chaFile, "body"); }

        private static void SavePrefix(ChaFile __instance)
        {
            Rec r;
            if (Recs.Count == 0 || __instance == null || !Recs.TryGetValue(__instance, out r) || !r.Parked) return;
            Restore(__instance, "save");
            r.ParkAfterSave = true;
        }

        private static void SavePostfix(ChaFile __instance)
        {
            try
            {
                if (!On) return;
                Rec r;
                if (Recs.Count == 0 || __instance == null || !Recs.TryGetValue(__instance, out r) || !r.ParkAfterSave) return;
                r.ParkAfterSave = false;
                var h = Singleton<Game>.Instance.HeroineList.FirstOrDefault(x => x != null && x.charFile == __instance);
                if (h != null && Away(h, Singleton<Game>.Instance.actScene)) Park(h);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Absent park after save: " + e.Message); }
        }

        /// <summary>A new card is read into this object: what was parked belongs to the old one.</summary>
        private static void LoadPrefix(ChaFile __instance)
        {
            try
            {
                Rec r;
                if (Recs.Count == 0 || __instance == null || !Recs.TryGetValue(__instance, out r)) return;
                Recs.Remove(__instance);
                Delete(r.Path);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Absent forget: " + e.Message); }
        }

        // ---------- park / restore ----------

        /// <summary>No NPC today, no body, not a story character.</summary>
        private static bool Away(SaveData.Heroine h, ActionScene act)
        {
            if (h == null || h.charFile == null || h.chaCtrl != null || h.fixCharaID != 0) return false;
            if (act == null || act.npcList == null) return false;
            return !act.npcList.Any(n => n != null && n.heroine == h);
        }

        /// <summary>After the day's draw (and after every period's load): parks everyone who is away. Spread over frames.</summary>
        public static IEnumerator ParkAll(ActionScene act)
        {
            if (!On || act == null) yield break;
            var t0 = Time.realtimeSinceStartup;
            var heroines = Singleton<Game>.Instance.HeroineList.Where(h => h != null).ToList();
            // cards of an older save loaded before: nothing will ask for them any more
            foreach (var stale in Recs.Keys.Where(f => !heroines.Any(h => h.charFile == f)).ToList()) { Delete(Recs[stale].Path); Recs.Remove(stale); }
            int n = 0;
            long bytes = 0;
            foreach (var h in heroines)
            {
                if (!Away(h, act)) continue;
                Rec r;
                if (Recs.TryGetValue(h.charFile, out r) && r.Parked) continue;
                var b = Park(h);
                if (b > 0) { n++; bytes += b; yield return null; }
            }
            if (n > 0 || ParkedCount > 0)
                SchoolOptimizerPlugin.Log.LogInfo(string.Format("Absent students: {0} more parked ({1:0} MB), {2} parked in all ({3:0} MB of appearance data out of memory), {4:0.0} s",
                    n, bytes / 1048576.0, ParkedCount, ParkedBytes / 1048576.0, Time.realtimeSinceStartup - t0));
        }

        private static long Park(SaveData.Heroine h)
        {
            var file = h.charFile;
            try
            {
                Dictionary<string, PluginData> dict;
                _inside = true;
                try { dict = ExtendedSave.GetAllExtendedData(file); }
                finally { _inside = false; }
                if (dict == null) return 0;
                var keys = dict.Where(kv => kv.Value != null && !Keep.Contains(kv.Key) && !kv.Key.StartsWith("joojkk.", StringComparison.Ordinal) && Size(kv.Value.data, 0) >= MinBytes)
                    .Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).ToArray();
                if (keys.Length == 0) return 0;

                Rec r;
                Recs.TryGetValue(file, out r);
                var same = r != null && r.Back != null && File.Exists(r.Path) && r.Keys.SequenceEqual(keys) && keys.All(k => ReferenceEquals(dict[k], r.Back[k]));
                if (!same)
                {
                    var part = keys.ToDictionary(k => k, k => dict[k]);
                    var bytes = (byte[])_ser.Invoke(null, new object[] { part });
                    Directory.CreateDirectory(_dir);
                    var path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".bin");
                    File.WriteAllBytes(path, bytes);
                    var check = File.ReadAllBytes(path);
                    var back = check.Length == bytes.Length && Equal(check, bytes) ? (Dictionary<string, PluginData>)_de.Invoke(null, new object[] { check }) : null;
                    if (back == null || back.Count != keys.Length || keys.Any(k => !back.ContainsKey(k)))
                    {
                        Delete(path);
                        SchoolOptimizerPlugin.Log.LogWarning("Absent student " + h.parameter.fullname + ": cache file did not read back, her data stays in memory.");
                        return 0;
                    }
                    if (r != null) Delete(r.Path);
                    r = new Rec { Path = path, Keys = keys, Bytes = bytes.Length, Name = h.parameter.fullname };
                    Recs[file] = r;
                }
                foreach (var k in keys) dict.Remove(k);
                r.Parked = true;
                r.Back = null;
                SchoolOptimizerPlugin.Log.LogDebug("Parked " + r.Name + ": " + string.Join(", ", keys) + " (" + r.Bytes / 1024 + " KB" + (same ? ", file kept" : "") + ")");
                return r.Bytes;
            }
            catch (Exception e)
            {
                SchoolOptimizerPlugin.Log.LogWarning("Absent student " + h.parameter.fullname + " not parked: " + e.Message);
                return 0;
            }
        }

        private static void Restore(ChaFile file, string why)
        {
            if (_inside || Suspend || Recs.Count == 0 || file == null) return;
            Rec r;
            if (!Recs.TryGetValue(file, out r) || !r.Parked) return;
            _inside = true;
            try
            {
                var back = (Dictionary<string, PluginData>)_de.Invoke(null, new object[] { File.ReadAllBytes(r.Path) });
                foreach (var kv in back)
                    if (ExtendedSave.GetExtendedDataById(file, kv.Key) == null) // a newer entry wins
                        ExtendedSave.SetExtendedDataById(file, kv.Key, kv.Value);
                var dict = ExtendedSave.GetAllExtendedData(file);
                r.Back = r.Keys.Where(k => dict != null && dict.ContainsKey(k)).ToDictionary(k => k, k => dict[k]);
                r.Parked = false;
                Restores++;
                if (why != "save" && why != "all" && !Returned.Contains(r.Name)) Returned.Add(r.Name);
                SchoolOptimizerPlugin.Log.LogInfo("Absent student " + r.Name + ": appearance data back in memory (" + why + ", " + r.Bytes / 1024 + " KB)");
            }
            catch (Exception e)
            {
                SchoolOptimizerPlugin.Log.LogError("Absent student " + r.Name + ": could not read her parked data back from " + r.Path + ": " + e);
            }
            finally { _inside = false; }
        }

        /// <summary>Everything back (option switched off, plugin unloading).</summary>
        public static void RestoreAll()
        {
            foreach (var f in Recs.Keys.ToList()) Restore(f, "all");
        }

        public static string Describe(ChaFile file)
        {
            Rec r;
            return file != null && Recs.TryGetValue(file, out r) && r.Parked ? "parked " + r.Bytes / 1024 + "K (" + string.Join(", ", r.Keys) + ")" : "";
        }

        // ---------- helpers ----------

        private static long Size(object o, int depth)
        {
            if (o == null || depth > 8) return 0;
            var b = o as byte[];
            if (b != null) return b.Length;
            var s = o as string;
            if (s != null) return s.Length * 2;
            var d = o as IDictionary;
            if (d != null) { long n = 0; foreach (DictionaryEntry e in d) n += Size(e.Key, depth + 1) + Size(e.Value, depth + 1); return n; }
            var en = o as IEnumerable;
            if (en != null) { long n = 0; foreach (var x in en) n += Size(x, depth + 1); return n; }
            return 8;
        }

        private static bool Equal(byte[] a, byte[] b)
        {
            for (var i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static void Delete(string path)
        {
            try { if (path != null && File.Exists(path)) File.Delete(path); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Absent cache delete: " + e.Message); }
        }
    }
}
