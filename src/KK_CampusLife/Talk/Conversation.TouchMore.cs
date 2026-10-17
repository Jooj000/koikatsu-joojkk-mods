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
                harmony.Patch(EnterFuncMethod, postfix: new HarmonyMethod(typeof(Conversation), nameof(HoverNote)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Conversation touch areas: " + e.Message); }
        }

        private static int _clickFrame = -1;
        internal static bool OneBeat = true;
        internal static readonly List<string> TouchTrace = new List<string>();

        /// <summary>A click is one touch: the first of the game's areas and ours to take this frame's click keeps it.</summary>
        private static bool ClaimClick()
        {
            if (_clickFrame == Time.frameCount) return false;
            _clickFrame = Time.frameCount;
            return true;
        }

        internal static void TouchNote(string what)
        {
            TouchTrace.Add(what);
            if (TouchTrace.Count > 40) TouchTrace.RemoveAt(0);
        }

        private static void HoverNote(string _kind) { TouchNote("hover " + _kind); }

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
                if (!Campus.On || _kind == null) return;
                TouchNote("click " + _kind);
                if (!ClaimClick()) { _kind = null; return; }
                if (__instance.touchMode != 0 || (_kind != "Head" && _kind != "Cheek")) return;
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

        private static float _busyUntil;
        internal static string BusyWhy = "";

        /// <summary>
        /// A line is still on screen (ours or the game's), the gift menu is up or she is saying goodbye: nothing the
        /// player clicks may act until that box closes, plus a short beat after it so the click that closed it does not
        /// also press what was under it ("as funções só devem funcionar depois de você fechar a caixa de diálogo atual",
        /// </summary>
        internal static bool TalkBusy(out string why)
        {
            why = Scenes.Running ? "a scene is playing" : ADV.Program.isADVProcessing ? "the game's line is open"
                : _goodbye ? "she is saying goodbye" : null;
            var now = Time.realtimeSinceStartup;
            if (why != null) { _busyUntil = now + 0.35f; BusyWhy = why; return true; }
            if (now < _busyUntil) { why = "the line just closed"; return true; }
            if (GiftMenu.IsOpen) { why = "the gift menu is open"; return true; }   // a menu, not a line: no beat after it
            why = "";
            return false;
        }

        internal static bool TalkBusy() { string why; return TalkBusy(out why); }

        /// <summary>Busy for a reason other than the gift menu (a line, its closing beat, the goodbye).</summary>
        private static bool TalkBusyLine() { string why; return TalkBusy(out why) && why != "the gift menu is open"; }

        /// <summary>Logs a click that came while a line was open (it is dropped, not queued).</summary>
        private static bool Busy(string what)
        {
            string why;
            if (!TalkBusy(out why)) return false;
            CampusLifePlugin.Log.LogInfo("Talk: " + what + " ignored: the line is still open (" + why + ")");
            return true;
        }

        /// <summary>The touch buttons are up and nothing plays (the same moments the game's own areas answer).</summary>
        private static bool SpotReady(TalkScene ts)
        {
            if (ts == null || ts != G.TalkScene || !Campus.On || TalkBusy() || !CanvasOn(ts)) return false;
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
            if (fromSpot && (!SpotReady(ts) || !ClaimClick())) return;
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

            int level;
            var kiss = "";
            var answer = "";
            if (part == "mouth")
            {
                kiss = KissTaken(h, n);
                level = kiss == "yes" ? 0 : kiss == "wary" || kiss == "wary_no" ? 1 : n >= 3 || h.relation <= 0 ? 3 : n == 2 ? 2 : 1;
            }
            else
            {
                // The director's four answers, like the game's hand and chest; a no after insisting,
                // from a girl who barely knows you, is the angry one.
                float c;
                string why;
                answer = h.isAnger ? "no" : Consent.TouchAnswer(h, part, look, n, Onlookers(h).Count, out c, out why);
                level = answer == "yes" ? 0 : answer == "wary_yes" || answer == "wary_no" ? 1 : h.isAnger || n >= 3 || h.relation < 0 ? 3 : 2;
            }
            CampusLifePlugin.Log.LogInfo("Conversation: " + GirlInfo.FirstName(h) + ": " + (look ? "stared at" : "touched") + " " + part + " #" + n + " level " + level + (kiss != "" ? " kiss " + kiss : " " + answer));
            _pend = new Pend { Her = h, Kind = "touch", PartName = part, Look = look, Level = level, Kiss = kiss, Answer = answer, At = Time.realtimeSinceStartup, Info = Traverse.Create(ts).Property("info").GetValue<ActionGame.Communication.Info>() };
            // With both girls the other one is right there: a kiss always gets her reaction, a touch sometimes.
            if (_trio != null && h == _trio.A)
            {
                _trio.LastTo = h;
                if (kiss != "") _trio.LastKiss = kiss == "wary_no" ? "no" : kiss;
                else { _trio.LastLvl = look ? Lvl.Friend : Lvl.Romance; _trio.LastQ = level <= 0 ? Q.Good : level == 1 ? Q.Neutral : Q.Bad; }
            }
            // The pose comes at once, the line when her reaction can show (TickReact).
            // No game part matches these places. The kiss looks for its own poses in her whole pose table (shy, happy;
            // annoyed or surprised when she turns away); the level's touch poses are the fallback. Never one that covers
            // the chest (mid-cycle, 2026-10-28: "ela tampa o peito ao ser beijada").
            var level2 = PosePool(_pend.Info, "touch", Mathf.Min(level, 2)).Where(x => !Covers(x)).ToList();
            var own = part == "mouth" ? (kiss == "wary" || kiss == "wary_no" ? WaryPoses(h) : KissPoses(h, kiss == "yes")) : new List<string>();
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
        /// </summary>
        internal static string KissTaken(SaveData.Heroine h, int tries)
        {
            if (ForceKiss != null) return ForceKiss;
            string why;
            return Consent.Kiss(h, tries, Onlookers(h).Count, out why);
        }

        internal static string ForceKiss;

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
                    Achievements.Kissed(h, wary);
                    if (!wary) return;
                    // A story out of it: she keeps thinking about it, and her "what are we" scene comes next time.
                    CampusLife.Memories.Add(h, "kiss_wary", "you kissed me, and I let you", 1, true);
                    var gs = S.Girl(GirlInfo.Key(h));
                    if (string.IsNullOrEmpty(gs.Pending)) gs.Pending = "SceneTalk:feelings";
                    CampusLifePlugin.Log.LogInfo("Talk kiss wary: " + GirlInfo.FirstName(h) + " let it happen; her feelings scene is next (" + gs.Pending + ")");
                };
            // A wary no: she almost let you. No favor lost; she remembers how close it was.
            if (p.PartName == "mouth" && p.Kiss == "wary_no")
                return () =>
                {
                    CampusLife.Memories.Add(h, "almost", "we almost kissed", 1, true);
                    CampusLifePlugin.Log.LogInfo("Talk kiss almost: " + GirlInfo.FirstName(h) + " wanted to, and held back");
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
                else if (p.Answer == "wary_yes") h.lewdness = Mathf.Min(100, h.lewdness + 1);
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
        public bool ForB;   // on the second girl of the conversation with both (Conversation.BTouch)

        private void OnMouseEnter() { Conversation.SpotHover(true); }

        private void OnMouseExit() { Conversation.SpotHover(false); }

        private static int _frame = -1;

        private void OnMouseDown()
        {
            if (ForB && _frame == Time.frameCount) return;
            _frame = Time.frameCount;
            try { if (ForB) Conversation.BTouch(Kind, true); else Conversation.SpotTouch(Kind, true); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk touch " + Kind + ": " + e.Message); }
        }

        /// <summary>
        /// The second girl's areas look for the click themselves: Unity's OnMouseDown goes only to the nearest collider,
        /// click on her was lost). The nearest of our areas under the mouse takes it, once per click.
        /// </summary>
        private void Update()
        {
            if (!ForB || !Input.GetMouseButtonDown(0) || _frame == Time.frameCount) return;
            try
            {
                var cam = Camera.main;
                if (cam == null) return;
                TouchSpot best = null;
                var near = float.MaxValue;
                foreach (var hit in Physics.RaycastAll(cam.ScreenPointToRay(Input.mousePosition), 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                {
                    var spot = hit.collider != null ? hit.collider.GetComponent<TouchSpot>() : null;
                    if (spot != null && spot.ForB && hit.distance < near) { best = spot; near = hit.distance; }
                }
                _frame = Time.frameCount;
                if (best != null) Conversation.BTouch(best.Kind, true);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk touch (second girl): " + e.Message); }
        }
    }
}
