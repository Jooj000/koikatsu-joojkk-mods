using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IllusionUtility.GetUtility;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// diretamente na boca conta como beijo"). The game only has head, cheek, hands and chest. The butt gets hit areas of
    /// ours at the sides of her hips (the talk camera sees her from the front), for touching and for staring; touching
    /// her lips (inside the game's head and cheek areas) is a kiss she takes or turns away, with what a kiss means: the
    /// memory, the girls who saw it, her mood. The game's own clip does not play for these: her pose, face and line are ours.
    /// </summary>
    internal static partial class Conversation
    {
        private static readonly System.Reflection.MethodInfo EnterFuncMethod = AccessTools.Method(typeof(TalkScene), "EnterFunc");
        private static readonly System.Reflection.MethodInfo ExitFuncMethod = AccessTools.Method(typeof(TalkScene), "ExitFunc");
        private static readonly System.Reflection.FieldInfo TouchWaitField = AccessTools.Field(typeof(TalkScene), "touchDisposableInfo");
        private static TalkScene _spotsTalk;
        private static readonly Dictionary<string, int> SpotCount = new Dictionary<string, int>();
        private static Transform _mouth;

        public static void InstallTouchMore(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(TalkScene), "InitTouch"), postfix: new HarmonyMethod(typeof(Conversation), nameof(AddSpots)));
                harmony.Patch(AccessTools.Method(typeof(TalkScene), "TouchFunc"), prefix: new HarmonyMethod(typeof(Conversation), nameof(MouthIsKiss)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Conversation touch areas: " + e.Message); }
        }

        private static void AddSpots(TalkScene __instance)
        {
            try
            {
                SpotCount.Clear();
                _spotsTalk = __instance;
                _mouth = null;
                var h = __instance.targetHeroine;
                var cha = h != null ? h.chaCtrl : null;
                if (!Campus.On || cha == null || cha.objBodyBone == null) return;
                var layer = LayerMask.NameToLayer("Chara");
                foreach (var side in new[] { "L", "R" })
                {
                    var bone = cha.objBodyBone.transform.FindLoop("cf_j_siri_" + side);
                    if (bone == null) continue;
                    var go = new GameObject("cl_touch_butt_" + side) { layer = layer };
                    go.transform.SetParent(bone.transform, false);
                    // Her left is the screen's right: out to the side of the hip, where you can reach from the front.
                    go.transform.position = bone.transform.position + cha.transform.right * (side == "L" ? -0.05f : 0.05f);
                    var col = go.AddComponent<SphereCollider>();
                    col.radius = 0.075f / Mathf.Max(0.01f, go.transform.lossyScale.x);
                    go.AddComponent<TouchSpot>().Kind = "butt";
                }
                var head = cha.objHeadBone != null ? cha.objHeadBone.transform : cha.objBodyBone.transform;
                foreach (var name in new[] { "cf_J_MouthBase_tr", "cf_J_MouthBase_s", "cf_J_MouthBase_rx", "cf_J_MouthMove" })
                {
                    var m = head.FindLoop(name);
                    if (m != null) { _mouth = m.transform; break; }
                }
                CampusLifePlugin.Log.LogDebug("Talk touch areas: butt " + cha.GetComponentsInChildren<TouchSpot>(true).Length + ", mouth " + (_mouth != null ? _mouth.name : "none"));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk touch areas: " + e.Message); }
        }

        /// <summary>Her lips, in the touch mode: the game's head or cheek reaction does not play (no kind, no clip), ours does.</summary>
        private static void MouthIsKiss(TalkScene __instance, ref string _kind)
        {
            try
            {
                if (!Campus.On || _kind == null || __instance.touchMode != 0 || (_kind != "Head" && _kind != "Cheek")) return;
                if (!NearMouth()) return;
                _kind = null;   // TouchFunc leaves at once for an unknown place
                SpotTouch("mouth", false);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk kiss: " + e.Message); }
        }

        private static bool NearMouth()
        {
            var cam = Camera.main;
            if (_mouth == null || cam == null) return false;
            var at = cam.WorldToScreenPoint(_mouth.position);
            if (at.z <= 0f) return false;
            var edge = cam.WorldToScreenPoint(_mouth.position + cam.transform.right * 0.03f);
            var r = Mathf.Max(12f, Mathf.Abs(edge.x - at.x));
            return ((Vector2)Input.mousePosition - (Vector2)at).sqrMagnitude <= r * r;
        }

        /// <summary>The touch buttons are up and nothing plays (the same moments the game's own areas answer).</summary>
        private static bool SpotReady(TalkScene ts)
        {
            if (ts == null || ts != G.TalkScene || !Campus.On || _goodbye || Scenes.Running || ADV.Program.isADVProcessing || GiftMenu.IsOpen || !CanvasOn(ts)) return false;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return false;
            if (TransVoiceField != null && TransVoiceField.GetValue(ts) as UnityEngine.Object != null) return false;
            var wait = TouchWaitField != null ? TouchWaitField.GetValue(ts) : null;
            return wait == null || !Traverse.Create(wait).Property("check").GetValue<bool>();
        }

        internal static void SpotHover(bool on)
        {
            try
            {
                var ts = G.TalkScene;
                if (ts == null || ts != _spotsTalk) return;
                if (on && SpotReady(ts)) EnterFuncMethod.Invoke(ts, new object[] { "Siri" });
                else if (!on) ExitFuncMethod.Invoke(ts, new object[] { "Siri" });
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk touch hover: " + e.Message); }
        }

        /// <summary>The butt (touched or stared at) or her lips: how far you went decides how she takes it.</summary>
        internal static void SpotTouch(string part, bool fromSpot)
        {
            var ts = G.TalkScene;
            if (fromSpot && !SpotReady(ts)) return;
            if (ts == null || ts.targetHeroine == null) return;
            var h = ts.targetHeroine;
            var look = part == "butt" && ts.touchMode == 1;
            var key = part + (look ? ":look" : "");
            int n;
            SpotCount.TryGetValue(key, out n);
            SpotCount[key] = ++n;
            try { var lad = LadField != null ? LadField.GetValue(ts) : null; if (lad != null) Traverse.Create(lad).Method("End").GetValue(); }
            catch { }
            try { if (ExitFuncMethod != null) ExitFuncMethod.Invoke(ts, new object[] { part }); }
            catch { }

            var gf = h.isGirlfriend;
            var flirty = Interested(h) && h.relation >= 2 || MoodOf(h) == CampusLife.Mood.Flirty || h.lewdness >= 60 && h.relation >= 1; // a stranger is never "in the mood" from H alone
            int level;
            var kiss = "";
            if (part == "mouth")
            {
                kiss = KissTaken(h, n);
                level = kiss == "yes" ? 0 : kiss == "wary" ? 1 : n >= 3 || h.relation <= 0 ? 3 : n == 2 ? 2 : 1;
            }
            else
            {
                // Staring is softer than touching; a girlfriend or a girl in the mood takes more before it is too much.
                var steps = (look ? 1 : 0) + (gf || flirty ? 1 : 0);
                level = h.isAnger ? 3 : Mathf.Clamp(n - 1 - steps + (gf || flirty ? 0 : 1), 0, 3);
            }
            CampusLifePlugin.Log.LogInfo("Conversation: " + GirlInfo.FirstName(h) + ": " + (look ? "stared at" : "touched") + " " + part + " #" + n + " level " + level + (kiss != "" ? " kiss " + kiss : ""));
            _pend = new Pend { Her = h, Kind = "touch", PartName = part, Look = look, Level = level, Kiss = kiss, At = Time.realtimeSinceStartup, Info = Traverse.Create(ts).Property("info").GetValue<ActionGame.Communication.Info>() };
            // The pose comes at once, the line when her reaction can show (TickReact).
            // No game part matches these places. The kiss looks for its own poses in her whole pose table (shy, happy;
            // annoyed or surprised when she turns away); the level's touch poses are the fallback. Never one that covers
            // the chest (mid-cycle, 2026-10-28: "ela tampa o peito ao ser beijada").
            var level2 = PosePool(_pend.Info, "touch", Mathf.Min(level, 2)).Where(x => !Covers(x)).ToList();
            var own = part == "mouth" ? (kiss == "wary" ? WaryPoses(h) : KissPoses(h, kiss == "yes")) : new List<string>();
            var pose = VariedPose(own, own.Count > 0 ? own : level2, "t" + part + kiss, null, level);
            if (pose != null) PlayPose(ts, pose);
        }

        private static readonly string[] CoverWords = { "胸", "隠", "尻" };
        private static readonly string[] KissYesWords = { "照れ", "恥ずかし", "喜", "嬉", "笑", "好", "楽" };
        private static readonly string[] KissNoWords = { "嫌", "困", "驚", "怒", "拒" };
        private static readonly Dictionary<string, List<string>> KissPoolOf = new Dictionary<string, List<string>>();

        private static bool Covers(string pose) { return CoverWords.Any(w => pose.Contains(w)); }

        /// <summary>The wary kiss: her shy poses (embarrassed, at a loss), the welcome ones when she has none.</summary>
        private static List<string> WaryPoses(SaveData.Heroine h)
        {
            var shy = KissPoses(h, true).Where(x => x.Contains("照れ") || x.Contains("恥ずかし")).ToList();
            return shy.Count > 0 ? shy : KissPoses(h, true);
        }

        /// <summary>Her personality's whole talk pose table (Manager.Communication), by name.</summary>
        internal static Dictionary<string, string[]> PoseTable(SaveData.Heroine h)
        {
            if (h == null || !Singleton<Manager.Communication>.IsInstance()) return null;
            var all = Traverse.Create(Singleton<Manager.Communication>.Instance).Field("dicPoseChara").GetValue<Dictionary<int, Dictionary<string, string[]>>>();
            Dictionary<string, string[]> mine;
            return all != null && all.TryGetValue(h.FixCharaIDOrPersonality, out mine) ? mine : null;
        }

        private static List<string> KissPoses(SaveData.Heroine h, bool yes)
        {
            var key = h.FixCharaIDOrPersonality + (yes ? "y" : "n");
            List<string> l;
            if (KissPoolOf.TryGetValue(key, out l)) return l;
            var table = PoseTable(h);
            var words = yes ? KissYesWords : KissNoWords;
            l = table == null ? new List<string>() : table.Keys.Where(x => !Covers(x) && words.Any(w => x.Contains(w))).ToList();
            KissPoolOf[key] = l;
            CampusLifePlugin.Log.LogInfo("Talk kiss poses " + (yes ? "yes" : "no") + ": " + l.Count + " (" + string.Join(", ", l.ToArray()) + ")");
            return l;
        }

        /// <summary>
        /// Does she take the kiss? "yes", "wary" or "no". A girlfriend almost always (less with a crowd watching);
        /// otherwise how close you are, whether she likes you, her mood and how lewd she is; never when she is angry, and
        /// less each time you try again. A close friend who likes you a lot and has a high H (favor 80+, H 50+) lets it
        /// era amiga máxima e com H alto... deveria pelo menos aceitar receosa").
        /// </summary>
        private static string KissTaken(SaveData.Heroine h, int tries)
        {
            if (h.isAnger) return "no";
            if (KissRoll(h, tries)) return "yes";
            var close = !h.isGirlfriend && h.relation >= 1 && h.favor >= 80 && h.lewdness >= 50 && tries <= 2;
            var mood = MoodOf(h);
            return close && mood != CampusLife.Mood.Upset && mood != CampusLife.Mood.Stressed ? "wary" : "no";
        }

        internal static string ForceKiss;

        private static bool KissRoll(SaveData.Heroine h, int tries)
        {
            if (ForceKiss != null) return ForceKiss == "yes";
            var crowd = Onlookers(h).Count;
            float p;
            if (h.isGirlfriend) p = 0.92f - (crowd >= 4 ? 0.3f : 0f);
            else
            {
                p = 0f;
                if (Interested(h)) p += 0.3f;
                if (h.relation >= 2) p += 0.25f;
                else if (h.relation == 1) p += 0.08f;
                var mood = MoodOf(h);
                if (mood == CampusLife.Mood.Flirty) p += 0.25f;
                else if (mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Stressed) p -= 0.2f;
                if (h.lewdness >= 60) p += 0.15f;
                if (crowd >= 4) p -= 0.2f;
            }
            p -= 0.15f * (tries - 1);
            return Random.value < Mathf.Clamp01(p);
        }

        /// <summary>What a touch of ours leaves behind: the kiss is remembered (and seen), going too far costs favor.</summary>
        private static Action SpotAfter(Pend p)
        {
            var h = p.Her;
            if (p.PartName == "mouth" && (p.Kiss == "yes" || p.Kiss == "wary"))
                return () =>
                {
                    var wary = p.Kiss == "wary";
                    h.favor = Mathf.Min(100, h.favor + (wary ? 2 : 3));
                    h.lewdness = Mathf.Min(100, h.lewdness + (wary ? 3 : 4));
                    // The kiss as a fact (Lore "kissed") and an incident only the girls who really saw it know.
                    Social.Add(Social.Kinds.Flirted, h, Witness.Who(h, Witness.Kiss), Witness.Kiss);
                    if (!wary) return;
                    // A story out of it: she keeps thinking about it, and her "what are we" scene comes next time.
                    CampusLife.Memories.Add(h, "kiss_wary", "you kissed me, and I let you", 1, true);
                    var gs = S.Girl(GirlInfo.Key(h));
                    if (string.IsNullOrEmpty(gs.Pending)) gs.Pending = "SceneTalk:feelings";
                    CampusLifePlugin.Log.LogInfo("Talk kiss wary: " + GirlInfo.FirstName(h) + " let it happen; her feelings scene is next (" + gs.Pending + ")");
                };
            if (p.PartName == "mouth")
                return () =>
                {
                    h.favor = Mathf.Max(0, h.favor - (p.Level >= 2 ? 3 : 1));
                    if (p.Level >= 3) { h.anger = Mathf.Min(100, h.anger + 35); _endNext = true; }
                };
            // The butt.
            return () =>
            {
                if (p.Level == 0) h.lewdness = Mathf.Min(100, h.lewdness + (p.Look ? 1 : 3));
                else if (p.Level == 2) h.favor = Mathf.Max(0, h.favor - 2);
                else if (p.Level >= 3) { h.favor = Mathf.Max(0, h.favor - 5); h.anger = Mathf.Min(100, h.anger + 40); _endNext = true; }
                if (!p.Look)
                {
                    var near = Witness.Who(h, Witness.Touch);
                    if (near.Count > 0) Social.Add(Social.Kinds.Flirted, h, near, Witness.Touch);
                }
            };
        }
    }

    /// <summary>A hit area of ours on her body (the talk camera's mouse events, like the game's own areas).</summary>
    internal sealed class TouchSpot : MonoBehaviour
    {
        public string Kind = "";

        private void OnMouseEnter() { Conversation.SpotHover(true); }

        private void OnMouseExit() { Conversation.SpotHover(false); }

        private void OnMouseDown()
        {
            try { Conversation.SpotTouch(Kind, true); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk touch " + Kind + ": " + e.Message); }
        }
    }
}
