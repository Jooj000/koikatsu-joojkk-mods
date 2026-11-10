using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using Manager;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Broken hair on the map, found and fixed where it happens. Every second one student in sight (on your map, near you,
    /// fully loaded) is looked at, in turn:
    /// <list type="bullet">
    /// <item>her mod data still parked while she has a body (AbsentPark): put back and her whole look rebuilt;</item>
    /// <item>no color: a hair piece whose material lost its texture, or whose color is far from the card's hair color;</item>
    /// <item>missing parts: a hair slot the card fills with nothing built, or a hair renderer gone;</item>
    /// <item>messy strands: strands far from where they hung when she was first seen (physics blown up).</item>
    /// </list>
    /// Each case is fixed once (the game's own color pass, then MaterialEditor and HairAccessoryCustomizer again, or the
    /// strands reset) and written to the log with the situation around it ("Hair watch: ..."): how long she has been on
    /// the map, whether she was just loaded back or had her outfit changed, her distance, the physics state. A color the
    /// card's MaterialEditor edits on purpose stays as it is after one try.
    /// </summary>
    internal static class HairWatch
    {
        private sealed class Seen
        {
            public float Since;
            public float Repaired = -999f;
            public readonly Dictionary<DynamicBone, float> Rest = new Dictionary<DynamicBone, float>();
            public readonly HashSet<string> Tried = new HashSet<string>();
            // Hair renderers that had a texture when she was first seen: only those can "lose" it (the hair
            // accessories of some cards never have one, that is their shader).
            public readonly HashSet<Renderer> HadTex = new HashSet<Renderer>();
            // The color of each hair accessory when she was first seen (HairAccessoryCustomizer's): a color loss on these
            // cards is that color gone white.
            public readonly Dictionary<Renderer, Color> AcsColor = new Dictionary<Renderer, Color>();
            public int Fixes;
        }

        private static readonly Dictionary<ChaControl, Seen> Girls = new Dictionary<ChaControl, Seen>();
        private static float _next;
        private static int _turn, _map = -99;
        private static float _mapSince;

        public static int Checked, Fixed, Strands, Colors, Missing, ParkedShown;
        public static readonly List<string> Recent = new List<string>();

        private static bool On { get { return SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.HairWatchOn != null && SchoolOptimizerPlugin.HairWatchOn.Value; } }

        public static void Update()
        {
            var now = Time.realtimeSinceStartup;
            if (now < _next) return;
            _next = now + 1f;
            if (!On || !Singleton<Game>.IsInstance()) return;
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || act.npcList == null || act.Player == null || act.Map == null) { Girls.Clear(); return; }
            var sc = Singleton<Scene>.Instance;
            if (sc != null && (sc.IsNowLoading || sc.IsNowLoadingFade)) return;
            if (act.Map.no != _map) { _map = act.Map.no; _mapSince = now; }
            var me = act.Player.position;
            var near = act.npcList.Where(n => n != null && n.chaCtrl != null && n.mapNo == _map && n.isActive && n.chaCtrl.loadEnd
                                              && !OffMap.IsUnloaded(n) && !OutfitDefer.Reloading(n)
                                              && (n.position - me).sqrMagnitude < 25f * 25f).ToList();
            foreach (var gone in Girls.Keys.Where(c => c == null).ToList()) Girls.Remove(gone);
            if (near.Count == 0) return;
            var npc = near[_turn++ % near.Count];
            try { Check(npc, act, now); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Hair watch " + npc.name + ": " + e.Message); }
        }

        private static void Check(NPC npc, ActionScene act, float now)
        {
            var cha = npc.chaCtrl;
            Seen s;
            if (!Girls.TryGetValue(cha, out s)) { Girls[cha] = s = new Seen { Since = now }; }
            Checked++;
            // Seen this second for the first time: wait two seconds before judging (her pieces may still be settling).
            if (now - s.Since < 2f) { Measure(cha, s); return; }
            var problems = new List<string>();
            var rebuild = false;
            var rebuildHair = false;
            // A problem already repaired once and back again is the card's own (a MaterialEditor color, a hair from a mod
            // that is not installed): left as it is.
            Func<string, bool> fresh = key => !s.Tried.Contains(key);

            // Her mod data must be in her card while she has a body.
            var parked = AbsentPark.Describe(cha.chaFile);
            if (parked == "" && npc.heroine != null && npc.heroine.charFile != null) parked = AbsentPark.Describe(npc.heroine.charFile);
            if (parked != "")
            {
                AbsentPark.RestoreNow(cha.chaFile, "shown");
                if (npc.heroine != null) AbsentPark.RestoreNow(npc.heroine.charFile, "shown");
                problems.Add("her mod data was still parked (" + parked + ")");
                ParkedShown++;
                rebuild = true;
            }

            var hair = cha.chaFile != null ? cha.chaFile.custom.hair : null;
            var colorParts = new List<int>();
            if (hair != null && cha.objHair != null)
            {
                for (var i = 0; i < cha.objHair.Length && i < hair.parts.Length; i++)
                {
                    var obj = cha.objHair[i];
                    if (obj == null)
                    {
                        // Front and back hair always have a piece; side and extra hair may be "none" (id 0).
                        if ((i <= 1 || hair.parts[i].id != 0) && fresh("miss" + i)) { s.Tried.Add("miss" + i); problems.Add("hair slot " + i + " (id " + hair.parts[i].id + ") not built"); Missing++; rebuild = rebuildHair = true; }
                        continue;
                    }
                    // Its renderers: the hair component's list, or (no component found) every renderer under the piece.
                    var cmp = cha.GetCustomHairComponent(i);
                    Renderer[] rends;
                    if (cmp != null && cmp.rendHair != null && cmp.rendHair.Length > 0)
                    {
                        rends = cmp.rendHair;
                        var lost = rends.Count(r => r == null);
                        if (lost > 0 && fresh("gone" + i)) { s.Tried.Add("gone" + i); problems.Add("hair slot " + i + ": " + lost + " renderer(s) gone"); Missing++; rebuild = rebuildHair = true; }
                    }
                    else rends = obj.GetComponentsInChildren<Renderer>(true);
                    // A card can wear no hair of the game's own (the "none" piece, p_dummy) and all of it as hair
                    // accessories (Nonaka, Ozen, Yumeria): an empty piece then is the card, not a fault.
                    if (rends.Length == 0 && Dummy(cha, i)) continue;
                    if (rends.Length == 0 && fresh("empty" + i)) { s.Tried.Add("empty" + i); problems.Add("hair slot " + i + " (" + obj.name + ") has no renderer"); Missing++; rebuild = rebuildHair = true; }
                    foreach (var r in rends)
                    {
                        if (r == null || !r.enabled) continue;
                        var m = r.sharedMaterial;
                        if (!fresh("color" + i)) break;
                        if (m == null) { problems.Add("hair slot " + i + ": " + r.name + " has no material"); colorParts.Add(i); break; }
                        if (s.HadTex.Contains(r) && m.HasProperty("_MainTex") && m.mainTexture == null) { problems.Add("hair slot " + i + ": " + r.name + " lost its texture"); colorParts.Add(i); break; }
                        if (m.HasProperty("_Color") && Far(m.GetColor("_Color"), hair.parts[i].baseColor))
                        {
                            problems.Add("hair slot " + i + ": color " + Hex(m.GetColor("_Color")) + ", card " + Hex(hair.parts[i].baseColor));
                            colorParts.Add(i);
                            break;
                        }
                    }
                }
            }

            // Hair worn as accessories (the hair of many cards): no material, a texture it had and lost, or its color
            // (HairAccessoryCustomizer's) gone white when it was not white as she came.
            var acsLost = false;
            var acsWhite = false;
            foreach (var go in AccessoryHair(cha))
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || !r.enabled) continue;
                    var m = r.sharedMaterial;
                    string what = null;
                    if (m == null) what = " has no material";
                    else if (s.HadTex.Contains(r) && m.HasProperty("_MainTex") && m.mainTexture == null) what = " lost its texture";
                    else if (m.HasProperty("_Color"))
                    {
                        Color was;
                        var c = m.GetColor("_Color");
                        if (s.AcsColor.TryGetValue(r, out was) && White(c) && !White(was) && Far(c, was)) what = " turned white (was " + Hex(was) + ")";
                    }
                    if (what == null) continue;
                    if (!fresh("acs:" + go.name)) break;
                    s.Tried.Add("acs:" + go.name);
                    problems.Add("hair accessory " + go.name + ": " + r.name + what);
                    acsLost = true;
                    if (what.StartsWith(" turned white")) acsWhite = true;
                    break;
                }
            }
            if (acsLost) rebuild = true;
            if (acsWhite) Colors++;

            var blown = Blown(cha, s);
            if (blown != "") { problems.Add(blown); Strands++; }

            if (problems.Count == 0) return;
            foreach (var i in colorParts) s.Tried.Add("color" + i);
            if (now - s.Repaired < 20f) return; // strands: at most one reset every 20 s
            s.Repaired = now;
            s.Fixes++;
            var how = new List<string>();
            if (blown != "") { foreach (var b in s.Rest.Keys.Where(b => b != null)) b.ResetParticlesPosition(); how.Add("strands reset"); }
            if (rebuild)
            {
                OffMap.ReloadLook(cha, rebuildHair);
                how.Add("look rebuilt");
            }
            else if (colorParts.Count > 0)
            {
                foreach (var i in colorParts.Distinct())
                {
                    cha.ChangeSettingHairColor(i, true, true, true);
                    cha.ChangeSettingHairOutlineColor(i);
                    cha.ChangeSettingHairAcsColor(i);
                }
                OtherMods.Reapply(cha, true);
                Colors++;
                how.Add("hair colors and MaterialEditor/HairAccessoryCustomizer applied again");
            }
            Fixed++;
            var line = (npc.heroine != null ? npc.heroine.Name : cha.name) + ": " + string.Join("; ", problems.ToArray()) + " -> " + string.Join(", ", how.ToArray())
                       + ". Situation: " + Situation(npc, act, s, now);
            SchoolOptimizerPlugin.Log.LogInfo("Hair watch: " + line);
            Recent.Add(DateTime.Now.ToString("HH:mm:ss") + " " + line);
            if (Recent.Count > 20) Recent.RemoveAt(0);
        }

        /// <summary>How far each strand chain reaches from its root when she is first seen (it hangs at rest then).</summary>
        private static void Measure(ChaControl cha, Seen s)
        {
            var roots = new List<GameObject>();
            if (cha.objHair != null) roots.AddRange(cha.objHair.Where(o => o != null));
            roots.AddRange(AccessoryHair(cha));
            foreach (var obj in roots)
            {
                var isAcs = cha.objHair == null || !cha.objHair.Contains(obj);
                foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    if (r == null || r.sharedMaterial == null) continue;
                    if (r.sharedMaterial.HasProperty("_MainTex") && r.sharedMaterial.mainTexture != null) s.HadTex.Add(r);
                    if (isAcs && r.sharedMaterial.HasProperty("_Color") && !s.AcsColor.ContainsKey(r)) s.AcsColor[r] = r.sharedMaterial.GetColor("_Color");
                }
                foreach (var b in obj.GetComponentsInChildren<DynamicBone>(true))
                    if (b != null && b.m_Root != null && !s.Rest.ContainsKey(b)) s.Rest[b] = Reach(b.m_Root);
            }
        }

        /// <summary>The game's "none" hair piece: an empty object on purpose.</summary>
        private static bool Dummy(ChaControl cha, int i)
        {
            try
            {
                var info = cha.infoHair != null && i < cha.infoHair.Length ? cha.infoHair[i] : null;
                return info == null || info.GetInfo(ChaListDefine.KeyType.MainData) == "p_dummy";
            }
            catch { return true; }
        }

        /// <summary>Her accessories that are hair (they carry the game's hair component), shown ones only.</summary>
        private static IEnumerable<GameObject> AccessoryHair(ChaControl cha)
        {
            // Every hair component under her (moreAccessories puts the slots past the 20th outside objAccessory), but
            // the game's own pieces.
            var own = new HashSet<GameObject>(cha.objHair != null ? cha.objHair.Where(o => o != null) : Enumerable.Empty<GameObject>());
            foreach (var c in cha.GetComponentsInChildren<ChaCustomHairComponent>(false))
                if (c != null && !own.Contains(c.gameObject)) yield return c.gameObject;
        }

        private static float Reach(Transform root)
        {
            var max = 0f;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var d = (t.position - root.position).sqrMagnitude;
                if (d > max) max = d;
            }
            return Mathf.Sqrt(max);
        }

        private static string Blown(ChaControl cha, Seen s)
        {
            if (s.Rest.Count == 0 || s.Rest.Keys.Any(b => b == null)) { s.Rest.Clear(); Measure(cha, s); return ""; }
            foreach (var kv in s.Rest)
            {
                var b = kv.Key;
                if (!b.enabled || b.m_Root == null) continue;
                var now = Reach(b.m_Root);
                if (now > kv.Value * 1.8f + 0.25f)
                    return "strands of " + b.m_Root.name + " reach " + now.ToString("0.00") + " m (" + kv.Value.ToString("0.00") + " m at rest)";
            }
            return "";
        }

        private static bool White(Color c) { return c.r > 0.95f && c.g > 0.95f && c.b > 0.95f; }

        private static bool Far(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) > 0.35f;
        }

        private static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        /// <summary>What was going on around her: the clues to the situation that breaks hair.</summary>
        private static string Situation(NPC npc, ActionScene act, Seen s, float now)
        {
            var bones = s.Rest.Keys.Where(b => b != null).ToList();
            var parts = new List<string>
            {
                "map " + act.Map.no + " for " + (now - _mapSince).ToString("0") + " s",
                "seen " + (now - s.Since).ToString("0") + " s",
                (npc.position - act.Player.position).magnitude.ToString("0.0") + " m away",
                "outfit " + npc.chaCtrl.fileStatus.coordinateType,
                "physics on " + bones.Count(b => b.enabled) + "/" + bones.Count,
            };
            var back = OffMap.LoadedBackAgo(npc);
            if (back >= 0f) parts.Add("loaded back from off-map " + back.ToString("0") + " s ago");
            if (npc.heroine != null && AbsentPark.Returned.Contains(npc.heroine.Name)) parts.Add("was an absent student today (data came back)");
            if (OffMapClothes.IsStripped(npc)) parts.Add("off-map clothes stripped");
            if (s.Fixes > 1) parts.Add("repair " + s.Fixes + " for her");
            if (act.isEventNow) parts.Add("event running");
            return string.Join(", ", parts.ToArray());
        }

        public static string Summary()
        {
            return "checked " + Checked + ", fixed " + Fixed + " (colors " + Colors + ", missing " + Missing + ", strands " + Strands + ", parked while shown " + ParkedShown + ")";
        }
    }
}
