using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Students on other maps without their clothes and accessories (their body stays). Each outfit piece is painted into
    /// its own texture on the graphics card (30-200 MB per girl with modded cards); in the test save, 18 girls off the
    /// map held 1 GB of them plus 0.6 GB of textures nobody in view used.
    /// <list type="bullet">
    /// <item>Release: after a while away from the player's map, her current outfit data (a copy, never saved) gets "none"
    /// in every clothes and accessory slot and the game's own outfit reload runs: the pieces and their textures go away,
    /// body, skeleton and animator stay (releasing those breaks the game's mover, see OffMap).</item>
    /// <item>Rebuild: her outfit is read again from her card (<see cref="ChaControl.ChangeCoordinateType(ChaFileDefine.CoordinateType, bool)"/>,
    /// the call the game makes at every period change, so KKAPI, MoreAccessories and MaterialEditor see the usual
    /// events) and rebuilt over several frames, hidden until it is done. It starts when her route takes her into the
    /// player's map within two gates, when the player walks up to a gate leading to her map, and at once when she shows
    /// up (walks in, teleport, summon, event). She is never shown half dressed: until the rebuild ends she stays hidden.</item>
    /// <item>Any rebuild the game asks for while she has no clothes (ChaControl.Reload) first gets her outfit back from
    /// her card, so it builds the right one.</item>
    /// </list>
    /// </summary>
    internal static class OffMapClothes
    {
        private static readonly Dictionary<ChaControl, NPC> Stripped = new Dictionary<ChaControl, NPC>();
        private static readonly HashSet<NPC> Stripping = new HashSet<NPC>();
        private static readonly HashSet<NPC> Dressing = new HashSet<NPC>();
        private static readonly HashSet<NPC> WantShown = new HashSet<NPC>();
        private static readonly Dictionary<NPC, float> LastNeeded = new Dictionary<NPC, float>();
        private static readonly HashSet<int> NearMaps = new HashSet<int>();
        private static readonly HashSet<int> NeededMaps = new HashSet<int>();
        private const float Away = 15f;     // seconds not needed before her clothes go
        private const float GateNear = 18f; // the player this close to a gate: the students behind it get dressed
        private const int Parallel = 3;     // rebuilds at a time, except when she is needed right now
        private static int _sinceCleanup, _lastMap = -1;
        private static float _next;

        public static int StrippedTotal, DressedTotal, DressedUrgent, RebuiltByGame, KeptReleased, WaitedHidden;
        public static float DressSecondsMax, DressSecondsSum, UrgentSecondsMax;

        private static bool On { get { return SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.StripOffMap.Value; } }

        public static int Count { get { return Stripped.Count; } }

        public static void Patch(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(ChaControl), nameof(ChaControl.SetActiveTop)), prefix: new HarmonyMethod(typeof(OffMapClothes), nameof(SetActiveTopPrefix)));
            harmony.Patch(AccessTools.Method(typeof(ChaControl), nameof(ChaControl.Reload)), prefix: new HarmonyMethod(typeof(OffMapClothes), nameof(ReloadPrefix)) { priority = Priority.First });
            harmony.Patch(AccessTools.Method(typeof(ChaControl), nameof(ChaControl.ReloadAsync)), prefix: new HarmonyMethod(typeof(OffMapClothes), nameof(ReloadPrefix)) { priority = Priority.First });
        }

        /// <summary>Her clothes are released (or being released): outfit changes are data only until she is rebuilt.</summary>
        public static bool IsStripped(NPC npc)
        {
            return npc != null && npc.chaCtrl != null && (Stripped.ContainsKey(npc.chaCtrl) || Stripping.Contains(npc));
        }

        // ---------- hooks ----------

        /// <summary>Someone shows her without clothes (she walked in, an event, a summon): she stays hidden and is dressed now.</summary>
        private static void SetActiveTopPrefix(ChaControl __instance, ref bool active)
        {
            try
            {
                if (__instance == null || Stripped.Count == 0 && Dressing.Count == 0 && Stripping.Count == 0) return;
                NPC npc;
                if (!Stripped.TryGetValue(__instance, out npc)) npc = Dressing.Concat(Stripping).FirstOrDefault(n => n != null && n.chaCtrl == __instance);
                if (npc == null) return;
                if (!active) { WantShown.Remove(npc); return; } // hidden again (she left): no longer waiting to be shown
                active = false;
                if (WantShown.Add(npc) && Dressing.Contains(npc)) WaitedHidden++; // her rebuild over several frames is not done yet
            }
            catch (Exception ex) { SchoolOptimizerPlugin.Log.LogDebug("Off-map clothes show: " + ex.Message); }
        }

        /// <summary>
        /// A rebuild of her outfit asked for by someone else (the game does it at every period change). Still far away:
        /// her outfit data is emptied again, so the rebuild builds nothing and the loading screen is shorter. Needed:
        /// her outfit data comes back from her card first, so the rebuild builds the right outfit.
        /// </summary>
        private static void ReloadPrefix(ChaControl __instance, bool noChangeClothes)
        {
            try
            {
                NPC npc;
                if (noChangeClothes || __instance == null || !Stripped.TryGetValue(__instance, out npc)) return;
                if (On && npc != null && !NeededNow(npc))
                {
                    Empty(__instance);
                    KeptReleased++;
                    return;
                }
                Stripped.Remove(__instance);
                __instance.ChangeCoordinateType((ChaFileDefine.CoordinateType)__instance.fileStatus.coordinateType, false);
                RebuiltByGame++;
                SchoolOptimizerPlugin.Log.LogDebug("Clothes of " + Name(npc) + " rebuilt by the game");
            }
            catch (Exception ex) { SchoolOptimizerPlugin.Log.LogDebug("Off-map clothes reload: " + ex.Message); }
        }

        // ---------- every frame ----------

        public static void Update()
        {
            if (Stripped.Count == 0 && Dressing.Count == 0 && Stripping.Count == 0 && !On) return;
            if (!Singleton<Game>.IsInstance() || Singleton<Game>.Instance.actScene == null)
            {
                Stripped.Clear(); Stripping.Clear(); Dressing.Clear(); WantShown.Clear(); LastNeeded.Clear(); _lastMap = -1;
                return;
            }
            var act = Singleton<Game>.Instance.actScene;
            if (act.Map == null || act.npcList == null || act.Player == null) return;
            var map = act.Map.no;

            // Every frame: she is on the player's map, shown, or someone tried to show her.
            if (Stripped.Count > 0)
                foreach (var kv in Stripped.ToList())
                {
                    var npc = kv.Value;
                    if (kv.Key == null || npc == null || npc.chaCtrl != kv.Key) { Stripped.Remove(kv.Key); continue; }
                    if (!On || npc.isActive || npc.mapNo == map || WantShown.Contains(npc)) Dress(npc, true);
                }

            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.5f;
            var scene = Singleton<Scene>.Instance;
            var busy = scene.IsNowLoading || scene.IsNowLoadingFade || act.isEventNow || !string.IsNullOrEmpty(scene.AddSceneName);

            if (map != _lastMap)
            {
                // A new map (the screen just faded): give back the textures the released clothes held.
                if (_lastMap != -1 && _sinceCleanup >= 3)
                {
                    _sinceCleanup = 0;
                    Resources.UnloadUnusedAssets();
                }
                _lastMap = map;
            }
            OffMap.NearGates(act, map, GateNear, NearMaps);
            // Her map, the maps one gate away, and two gates away behind a gate the player is walking up to.
            NeededMaps.Clear();
            NeededMaps.Add(map);
            Linked(act, map, NeededMaps);
            foreach (var m in NearMaps.ToList()) { NeededMaps.Add(m); Linked(act, m, NeededMaps); }

            var now = Time.realtimeSinceStartup;
            NPC strip = null;
            var oldest = 0f;
            foreach (var npc in act.npcList)
            {
                if (npc == null || npc.chaCtrl == null || npc.heroine == null) continue;
                var needed = !On || NeededMaps.Contains(npc.mapNo) || npc.isActive || OffMap.Coming(act, npc, map) || npc.heroine.fixCharaID != 0;
                var stripped = Stripped.ContainsKey(npc.chaCtrl);
                if (needed)
                {
                    LastNeeded[npc] = now;
                    if (stripped && Dressing.Count < Parallel) Dress(npc, false);
                    continue;
                }
                float last;
                if (!LastNeeded.TryGetValue(npc, out last)) { LastNeeded[npc] = now; continue; }
                if (busy || stripped || Stripping.Contains(npc) || Dressing.Contains(npc) || !npc.chaCtrl.loadEnd || !npc.chaCtrl.gameObject.activeSelf ||
                    OffMap.IsUnloaded(npc) || OutfitDefer.Reloading(npc)) continue;
                var idle = now - last;
                if (idle >= Away && idle > oldest) { oldest = idle; strip = npc; }
            }
            // One at a time, and never while someone is being dressed (that one matters more).
            if (strip != null && Stripping.Count == 0 && Dressing.Count == 0) Strip(strip);
            if (LastNeeded.Count > act.npcList.Count + 20)
                foreach (var k in LastNeeded.Keys.Where(k => k == null || !act.npcList.Contains(k)).ToList()) LastNeeded.Remove(k);
        }

        // ---------- release and rebuild ----------

        private static void Strip(NPC npc)
        {
            var cha = npc.chaCtrl;
            try
            {
                OutfitDefer.Forget(npc); // a queued outfit change becomes data only
                Empty(cha);
                Stripping.Add(npc);
                cha.StartCoroutine(StripRun(npc, cha));
            }
            catch (Exception e)
            {
                SchoolOptimizerPlugin.Log.LogWarning("Could not release the clothes of " + Name(npc) + ": " + e.Message);
                Stripping.Remove(npc);
                // Whatever happened, she gets her outfit back from her card.
                try { cha.ChangeCoordinateType((ChaFileDefine.CoordinateType)cha.fileStatus.coordinateType, false); } catch { }
                Stripped[cha] = npc;
            }
        }

        private static IEnumerator StripRun(NPC npc, ChaControl cha)
        {
            var inner = OutfitDefer.ReloadOutfit(npc, cha);
            while (inner.MoveNext()) yield return inner.Current;
            Stripping.Remove(npc);
            if (cha != null) Stripped[cha] = npc;
            OutfitDefer.Leave(npc, cha);
            StrippedTotal++;
            _sinceCleanup++;
            if (npc != null) SchoolOptimizerPlugin.Log.LogDebug("Released the clothes of " + Name(npc) + " (map " + npc.mapNo + ")");
        }

        /// <summary>"None" in every clothes and accessory slot of her current outfit data (a copy of her card's).</summary>
        private static void Empty(ChaControl cha)
        {
            foreach (var p in cha.nowCoordinate.clothes.parts) p.id = 0;
            foreach (var p in cha.nowCoordinate.accessory.parts) p.type = 120; // MoreAccessories' extra slots are in the same array
        }

        /// <summary>Maps one gate away from <paramref name="map"/>.</summary>
        private static void Linked(ActionScene act, int map, HashSet<int> into)
        {
            var gates = act.Map.gateInfoDic;
            if (gates == null) return;
            foreach (var g in gates.Values)
            {
                ActionGame.Point.GateInfo other;
                if (g != null && g.mapNo == map && gates.TryGetValue(g.linkID, out other) && other != null) into.Add(other.mapNo);
            }
        }

        private static bool NeededNow(NPC npc)
        {
            var act = npc.actScene;
            return act == null || act.Map == null || npc.mapNo == act.Map.no || NeededMaps.Contains(npc.mapNo) || npc.isActive || WantShown.Contains(npc) || npc.heroine == null || npc.heroine.fixCharaID != 0;
        }

        /// <summary>
        /// Needed right now (she is on the player's map or being shown): built in this very frame, before she is drawn
        /// (one short hitch, hidden by the fade when the player teleports). Otherwise over several frames, hidden.
        /// </summary>
        private static void Dress(NPC npc, bool urgent)
        {
            var cha = npc.chaCtrl;
            Stripped.Remove(cha);
            try
            {
                OutfitDefer.Forget(npc);
                cha.ChangeCoordinateType((ChaFileDefine.CoordinateType)cha.fileStatus.coordinateType, false);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Off-map clothes outfit data: " + e.Message); }
            if (!urgent)
            {
                Dressing.Add(npc);
                cha.StartCoroutine(DressRun(npc, cha, false));
                return;
            }
            var t0 = Time.realtimeSinceStartup;
            var chara = Singleton<Character>.Instance;
            var gc = chara.enableCharaLoadGCClear;
            try
            {
                chara.enableCharaLoadGCClear = false;
                var inner = cha.ReloadAsync(false, true, true, true, false); // asyncFlags false: no yields, all done here
                while (inner.MoveNext()) { }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogWarning("Dressing " + Name(npc) + ": " + e.Message); }
            finally { chara.enableCharaLoadGCClear = gc; }
            Shown(npc, cha, WantShown.Remove(npc));
            Tally(npc, t0, true);
        }

        private static void Shown(NPC npc, ChaControl cha, bool want)
        {
            try
            {
                if (npc == null || cha == null) return;
                if (npc.mapNo >= 0 && npc.actScene != null && npc.actScene.Map != null && npc.actScene.Map.infoDic.ContainsKey(npc.mapNo)) npc.MapShoesSetting(npc.mapNo);
                cha.SetActiveTop(npc.isActive || want);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Off-map clothes show: " + e.Message); }
        }

        private static void Tally(NPC npc, float t0, bool urgent)
        {
            if (npc != null) LastNeeded[npc] = Time.realtimeSinceStartup;
            var s = Time.realtimeSinceStartup - t0;
            DressedTotal++;
            if (urgent) { DressedUrgent++; UrgentSecondsMax = Mathf.Max(UrgentSecondsMax, s); }
            else { DressSecondsSum += s; DressSecondsMax = Mathf.Max(DressSecondsMax, s); }
            if (npc != null) SchoolOptimizerPlugin.Log.LogDebug("Dressed " + Name(npc) + " in " + s.ToString("0.00") + " s" + (urgent ? " (needed now, in one frame)" : ""));
        }

        private static IEnumerator DressRun(NPC npc, ChaControl cha, bool urgent)
        {
            var t0 = Time.realtimeSinceStartup;
            var inner = OutfitDefer.ReloadOutfit(npc, cha);
            while (inner.MoveNext()) yield return inner.Current;
            Dressing.Remove(npc);
            Shown(npc, cha, WantShown.Remove(npc));
            OutfitDefer.Leave(npc, cha);
            Tally(npc, t0, urgent);
        }

        private static string Name(NPC npc)
        {
            return npc != null && npc.heroine != null ? npc.heroine.Name : "?";
        }
    }
}
