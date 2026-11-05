using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionGame.Chara;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Students on other maps keep only their data. The game itself already runs them as data there (the AI walks them
    /// from gate to gate, no animation, no physics), but every one of them kept her whole body, outfit, accessories and
    /// their textures in memory. After a while away from the player's map her character is released, the way the game
    /// does it at the start of every load (<see cref="ChaControl.ReleaseObject"/>), and her character object is switched
    /// off. What she wears, her clothes state and everything other plugins store stays in her data.
    /// <list type="bullet">
    /// <item>She is loaded back, in the background over several frames, before she can be seen: when her route will take
    /// her into the player's map within two gates, when the player walks up to a gate leading to her map, and in any case
    /// when she shows up on the player's map (teleport, summon, event).</item>
    /// <item>The game's AI keeps calling her animator while she is away (motion, height, speed). She gets a stand-in
    /// animator, switched off like the real one is on other maps, so nothing breaks.</item>
    /// <item>Outfit changes while she is away only change her data (<see cref="ChaControl.ChangeCoordinateType(ChaFileDefine.CoordinateType, bool)"/>);
    /// the load builds the right outfit. Reloads asked for in that time are skipped for the same reason.</item>
    /// <item>Story characters, and girls during events, are never unloaded.</item>
    /// </list>
    /// </summary>
    internal static class OffMap
    {
        private sealed class Entry
        {
            public GameObject Stub;
            public bool Loading;
            public bool Urgent;
            public bool? WantTop;
        }

        private static readonly Dictionary<NPC, Entry> Unloaded = new Dictionary<NPC, Entry>();
        private static readonly Dictionary<NPC, float> LastNeeded = new Dictionary<NPC, float>();
        private static readonly HashSet<int> NearMaps = new HashSet<int>();
        private const float Away = 20f;    // seconds not needed before she is unloaded
        private const float GateNear = 12f; // the player this close to a gate: the students behind it get ready
        private const int Parallel = 2;
        private static int _loadingNow, _sinceCleanup, _lastMap = -1;
        private static float _next;
        public static int UnloadedTotal, LoadedBack, LoadFailed;

        private static MethodInfo _setAnimBody, _kkapiReload, _initMapSe;

        public static int Count { get { return Unloaded.Count; } }

        private static bool On { get { return SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.UnloadOffMap.Value; } }

        public static void Patch(Harmony harmony)
        {
            _setAnimBody = AccessTools.PropertySetter(typeof(ChaInfo), "animBody");
            _initMapSe = AccessTools.Method(typeof(Base), "InitializeMapSE");
            var api = AccessTools.TypeByName("KKAPI.Chara.CharacterApi");
            _kkapiReload = api == null ? null : AccessTools.Method(api, "ReloadChara", new[] { typeof(ChaControl) });
            if (_setAnimBody == null || _kkapiReload == null) throw new Exception("game or KKAPI methods not found");

            harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.Summon)), prefix: new HarmonyMethod(typeof(OffMap), nameof(SummonPrefix)));
            harmony.Patch(AccessTools.Method(typeof(ChaControl), nameof(ChaControl.SetActiveTop)), prefix: new HarmonyMethod(typeof(OffMap), nameof(SetActiveTopPrefix)));
            harmony.Patch(AccessTools.Method(typeof(ChaControl), nameof(ChaControl.Reload)), prefix: new HarmonyMethod(typeof(OffMap), nameof(ReloadPrefix)));
            foreach (var m in AccessTools.GetDeclaredMethods(typeof(ChaControl)).Where(x => x.Name == nameof(ChaControl.ChangeCoordinateTypeAndReload)))
                harmony.Patch(m, prefix: new HarmonyMethod(typeof(OffMap), nameof(CoordinatePrefix)));
        }

        public static bool IsUnloaded(NPC npc) { return npc != null && Unloaded.ContainsKey(npc); }

        private static readonly Dictionary<NPC, float> BackAt = new Dictionary<NPC, float>();

        /// <summary>Seconds since she was loaded back from off-map (-1: not this session). For the hair watch.</summary>
        public static float LoadedBackAgo(NPC npc)
        {
            float t;
            return npc != null && BackAt.TryGetValue(npc, out t) ? Time.realtimeSinceStartup - t : -1f;
        }

        /// <summary>
        /// Her look built again from her card without a full reload (the hair watch): the hair pieces if asked, then what
        /// KKAPI does after a load (MaterialEditor, overlays, extra accessories), then the colors.
        /// </summary>
        public static void ReloadLook(ChaControl cha, bool hair)
        {
            if (cha == null) return;
            if (hair) cha.ChangeHair(true);
            try { if (_kkapiReload != null) _kkapiReload.Invoke(null, new object[] { cha }); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Hair watch reload: " + (e.InnerException ?? e).Message); }
            OtherMods.Reapply(cha, true);
        }

        private static NPC Owner(ChaControl cha)
        {
            if (Unloaded.Count == 0 || cha == null) return null;
            foreach (var npc in Unloaded.Keys)
                if (npc != null && npc.chaCtrl == cha) return npc;
            return null;
        }

        // ---------- hooks ----------

        /// <summary>Someone shows her (an event, a summon): she is loaded right away and shown when ready.</summary>
        private static void SetActiveTopPrefix(ChaControl __instance, bool active)
        {
            try
            {
                var npc = Owner(__instance);
                if (npc == null) return;
                var e = Unloaded[npc];
                if (e.Loading) return; // the load itself hides the body while it builds
                e.WantTop = active;
                if (active) e.Urgent = true;
            }
            catch (Exception ex) { SchoolOptimizerPlugin.Log.LogDebug("Off-map show: " + ex.Message); }
        }

        private static void SummonPrefix(SaveData.Heroine heroine)
        {
            try
            {
                foreach (var kv in Unloaded)
                    if (kv.Key != null && kv.Key.heroine == heroine) kv.Value.Urgent = true;
            }
            catch (Exception ex) { SchoolOptimizerPlugin.Log.LogDebug("Off-map summon: " + ex.Message); }
        }

        /// <summary>
        /// No body to rebuild: the load builds her from her current data. A rebuild asked for while another one of hers
        /// is still running waits for it (see <see cref="OutfitDefer.Postpone"/>).
        /// </summary>
        private static bool ReloadPrefix(ChaControl __instance, bool noChangeClothes, bool noChangeHead, bool noChangeHair, bool noChangeBody)
        {
            try
            {
                if (Owner(__instance) != null) return false;
                return !OutfitDefer.Postpone(__instance, noChangeClothes, noChangeHead, noChangeHair, noChangeBody);
            }
            catch { return true; }
        }

        /// <summary>Outfit change while she is away: data only.</summary>
        private static bool CoordinatePrefix(ChaControl __instance, object[] __args)
        {
            try
            {
                if (Owner(__instance) == null) return true;
                if (__args.Length >= 2 && __args[0] is ChaFileDefine.CoordinateType)
                    __instance.ChangeCoordinateType((ChaFileDefine.CoordinateType)__args[0], (bool)__args[1]);
                else
                    __instance.ChangeCoordinateType(__args.Length > 0 && __args[0] is bool ? (bool)__args[0] : true);
                return false;
            }
            catch (Exception ex)
            {
                SchoolOptimizerPlugin.Log.LogDebug("Off-map outfit: " + ex.Message);
                return true;
            }
        }

        // ---------- every frame ----------

        public static void Update()
        {
            if (Unloaded.Count == 0 && !On) return;
            if (!Singleton<Game>.IsInstance() || Singleton<Game>.Instance.actScene == null)
            {
                Unloaded.Clear(); LastNeeded.Clear(); _loadingNow = 0; _lastMap = -1;
                return;
            }
            var act = Singleton<Game>.Instance.actScene;
            if (act.Map == null || act.npcList == null || act.Player == null) return;

            // Urgent loads every frame: she is on the player's map or someone wants her shown.
            if (Unloaded.Count > 0)
            {
                foreach (var kv in Unloaded.ToList())
                {
                    var npc = kv.Key;
                    if (npc == null || npc.chaCtrl == null) { Drop(npc, kv.Value); continue; }
                    if (!kv.Value.Loading && (kv.Value.Urgent || !On || npc.isActive || npc.mapNo == act.Map.no))
                        StartLoad(npc, kv.Value, true);
                }
            }

            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.5f;
            var scene = Singleton<Scene>.Instance;
            if (scene.IsNowLoading || scene.IsNowLoadingFade || act.isEventNow || !string.IsNullOrEmpty(scene.AddSceneName)) return;

            var map = act.Map.no;
            if (map != _lastMap)
            {
                // A new map: a good moment (the screen just faded) to give back what the unloaded students held.
                if (_lastMap != -1 && _sinceCleanup >= 3)
                {
                    _sinceCleanup = 0;
                    Resources.UnloadUnusedAssets();
                }
                _lastMap = map;
            }
            NearGates(act, map, GateNear, NearMaps);

            var now = Time.realtimeSinceStartup;
            NPC unload = null;
            var oldest = 0f;
            foreach (var npc in act.npcList)
            {
                if (npc == null || npc.chaCtrl == null || npc.heroine == null) continue;
                var needed = !On || npc.mapNo == map || npc.isActive || NearMaps.Contains(npc.mapNo) || Coming(act, npc, map) || npc.heroine.fixCharaID != 0;
                Entry e;
                var gone = Unloaded.TryGetValue(npc, out e);
                if (needed)
                {
                    LastNeeded[npc] = now;
                    if (gone && !e.Loading) StartLoad(npc, e, false);
                    continue;
                }
                float last;
                if (!LastNeeded.TryGetValue(npc, out last)) { LastNeeded[npc] = now; continue; }
                if (gone || !npc.chaCtrl.loadEnd || OutfitDefer.Reloading(npc)) continue;
                var idle = now - last;
                if (idle >= Away && idle > oldest) { oldest = idle; unload = npc; }
            }
            if (unload != null) Unload(unload);
            if (LastNeeded.Count > act.npcList.Count + 20)
                foreach (var k in LastNeeded.Keys.Where(k => k == null || !act.npcList.Contains(k)).ToList()) LastNeeded.Remove(k);
        }

        /// <summary>Maps behind the gates the player is standing near (within <paramref name="radius"/> meters).</summary>
        internal static void NearGates(ActionScene act, int map, float radius, HashSet<int> into)
        {
            into.Clear();
            var gates = act.Map.gateInfoDic;
            if (gates == null) return;
            var p = act.Player.position;
            foreach (var g in gates.Values)
            {
                if (g == null || g.mapNo != map || (g.pos - p).sqrMagnitude > radius * radius) continue;
                ActionGame.Point.GateInfo other;
                if (gates.TryGetValue(g.linkID, out other) && other != null) into.Add(other.mapNo);
            }
        }

        /// <summary>Her route crosses into the player's map within the next two gates.</summary>
        internal static bool Coming(ActionScene act, NPC npc, int map)
        {
            var route = npc.AI == null ? null : npc.AI.route;
            if (route == null || route.Count == 0) return false;
            var gates = act.Map.gateInfoDic;
            // The route is the whole trip; start at the gate on the map she is on now.
            var start = 0;
            for (var i = 0; i < route.Count; i++)
            {
                ActionGame.Point.GateInfo g;
                if (gates.TryGetValue(route[i], out g) && g.mapNo == npc.mapNo) { start = i; break; }
            }
            for (var i = start; i < route.Count && i < start + 2; i++)
            {
                ActionGame.Point.GateInfo g, other;
                if (!gates.TryGetValue(route[i], out g) || !gates.TryGetValue(g.linkID, out other)) continue;
                if (other.mapNo == map) return true;
            }
            return false;
        }

        // ---------- unload and load ----------

        private static void Unload(NPC npc)
        {
            var cha = npc.chaCtrl;
            var chara = Singleton<Character>.Instance;
            var gc = chara.enableCharaLoadGCClear;
            try
            {
                OutfitDefer.Forget(npc);
                chara.enableCharaLoadGCClear = false;
                var stub = new GameObject("joojkk_offmap_animator");
                stub.SetActive(false);
                stub.transform.SetParent(npc.transform, false);
                var anim = stub.AddComponent<Animator>();
                cha.ReleaseObject();
                _setAnimBody.Invoke(cha, new object[] { anim });
                npc.LoadAnimator();
                cha.gameObject.SetActive(false);
                Unloaded[npc] = new Entry { Stub = stub };
                UnloadedTotal++;
                _sinceCleanup++;
                SchoolOptimizerPlugin.Log.LogDebug("Unloaded " + npc.heroine.Name + " (map " + npc.mapNo + ")");
            }
            catch (Exception e)
            {
                SchoolOptimizerPlugin.Log.LogWarning("Could not unload " + npc.name + ": " + e.Message);
                // Whatever happened, she must come back whole.
                if (!Unloaded.ContainsKey(npc)) Unloaded[npc] = new Entry();
                Unloaded[npc].Urgent = true;
            }
            finally { chara.enableCharaLoadGCClear = gc; }
        }

        private static void StartLoad(NPC npc, Entry e, bool urgent)
        {
            if (!urgent && _loadingNow >= Parallel) return;
            e.Loading = true;
            _loadingNow++;
            var cha = npc.chaCtrl;
            cha.gameObject.SetActive(true);
            cha.StartCoroutine(Load(npc, cha, e));
        }

        private static IEnumerator Load(NPC npc, ChaControl cha, Entry e)
        {
            var chara = Singleton<Character>.Instance;
            chara.enableCharaLoadGCClear = false;
            // reflectStatus: her outfit, clothes state and the rest of her status are put back as they were.
            var inner = cha.LoadAsync(true, true);
            while (true)
            {
                bool more;
                try { more = inner.MoveNext(); }
                catch (Exception ex) { SchoolOptimizerPlugin.Log.LogWarning("Loading a student back: " + ex.Message); more = false; }
                if (!more) break;
                yield return inner.Current;
            }
            try
            {
                if (npc != null && cha != null)
                {
                    if (!cha.loadEnd) LoadFailed++;
                    npc.LoadAnimator();
                    if (e.Stub != null) UnityEngine.Object.Destroy(e.Stub);
                    // What KKAPI does after the game's own loading: MaterialEditor, overlays, body sliders and the rest.
                    _kkapiReload.Invoke(null, new object[] { cha });
                    if (npc.mapNo >= 0 && npc.actScene != null && npc.actScene.Map.infoDic.ContainsKey(npc.mapNo)) npc.MapShoesSetting(npc.mapNo);
                    cha.SetActiveTop(npc.isActive || e.WantTop == true);
                    if (npc.isActive)
                    {
                        npc.PlayAnimation();
                        npc.LookForDefault();
                        if (_initMapSe != null) _initMapSe.Invoke(npc, null);
                    }
                    LoadedBack++;
                    BackAt[npc] = Time.realtimeSinceStartup;
                    if (BackAt.Count > 200) BackAt.Clear();
                    SchoolOptimizerPlugin.Log.LogDebug("Loaded " + npc.heroine.Name + " back" + (e.Urgent ? " (urgent)" : ""));
                }
            }
            catch (Exception ex) { SchoolOptimizerPlugin.Log.LogWarning("Finishing a student's load: " + ex.Message); }
            finally
            {
                Unloaded.Remove(npc);
                if (npc != null) LastNeeded[npc] = Time.realtimeSinceStartup;
                _loadingNow = Mathf.Max(0, _loadingNow - 1);
                if (_loadingNow == 0) chara.enableCharaLoadGCClear = true;
            }
        }

        private static void Drop(NPC npc, Entry e)
        {
            if (e != null && e.Stub != null) UnityEngine.Object.Destroy(e.Stub);
            if (e != null && e.Loading) _loadingNow = Mathf.Max(0, _loadingNow - 1);
            Unloaded.Remove(npc);
        }

        /// <summary>Loads every unloaded student now (option switched off, leaving the school).</summary>
        public static void LoadAll()
        {
            foreach (var kv in Unloaded.ToList())
                if (kv.Key != null && kv.Key.chaCtrl != null && !kv.Value.Loading) StartLoad(kv.Key, kv.Value, true);
        }
    }
}
