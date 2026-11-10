using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IllusionUtility.GetUtility;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Touching the second girl of the conversation with both. The game's touch areas exist only on the first girl, so the second one gets ours on her bones
    /// (head, lips, hands, chest, hips), answering the game's own touch and look modes (the icons at the top). The head
    /// pats her, near the lips it is a kiss, the hands take her hand, the chest and hips are a bold touch (or a stare in
    /// the look mode). Each goes through her own turn, with the first girl watching.
    /// </summary>
    internal static partial class Conversation
    {
        private static Transform _mouthB;
        private static readonly List<GameObject> BSpots = new List<GameObject>();

        /// <summary>TrioStage.Begin: our touch areas on her body.</summary>
        internal static void AddBSpots(ChaControl cha)
        {
            ClearBSpots();
            try
            {
                if (cha == null || cha.objBodyBone == null) return;
                var layer = LayerMask.NameToLayer("Chara");
                var body = cha.objBodyBone.transform;
                Spot(body, "cf_j_head", "head", 0.11f, Vector3.up * 0.05f, layer);
                Spot(body, "cf_j_hand_L", "hand", 0.07f, Vector3.zero, layer);
                Spot(body, "cf_j_hand_R", "hand", 0.07f, Vector3.zero, layer);
                Spot(body, "cf_d_bust01_L", "bust", 0.07f, Vector3.zero, layer);
                Spot(body, "cf_d_bust01_R", "bust", 0.07f, Vector3.zero, layer);
                // The same butt area as the first girl's: the game's hip collider, copied (Conversation.ButtArea).
                var butt = new List<string>();
                foreach (var side in new[] { "L", "R" })
                {
                    var spot = ButtArea(cha, side, true);
                    if (spot == null) continue;
                    BSpots.Add(spot.gameObject);
                    butt.Add(spot.Shape);
                }
                _mouthB = null;
                var head = cha.objHeadBone != null ? cha.objHeadBone.transform : body;
                foreach (var name in new[] { "cf_J_MouthBase_tr", "cf_J_MouthBase_s", "cf_J_MouthBase_rx", "cf_J_MouthMove" })
                {
                    var m = head.FindLoop(name);
                    if (m != null) { _mouthB = m.transform; break; }
                }
                CampusLifePlugin.Log.LogInfo("Trio touch areas on the second girl: " + string.Join(", ", BSpots.Select(g => g.name).ToArray()) + "; butt " + (butt.Count > 0 ? string.Join("; ", butt.ToArray()) : "none (no cf_hit_siri)") + ", lips " + (_mouthB != null ? _mouthB.name : "none"));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio touch areas: " + e.Message); }
        }

        private static void Spot(Transform body, string bone, string kind, float radius, Vector3 shift, int layer)
        {
            var b = body.FindLoop(bone);
            if (b == null) return;
            var go = new GameObject("cl_btouch_" + kind + "_" + bone) { layer = layer };
            go.transform.SetParent(b.transform, false);
            go.transform.position = b.transform.position + shift;
            var col = go.AddComponent<SphereCollider>();
            col.radius = radius / Mathf.Max(0.01f, go.transform.lossyScale.x);
            var t = go.AddComponent<TouchSpot>();
            t.Kind = kind;
            t.ForB = true;
            BSpots.Add(go);
        }

        internal static int BTouchClicks;
        internal static string BTouchLast = "";

        internal static bool BSpotPoint(string kind, out Vector3 world, out string why)
        {
            world = Vector3.zero;
            why = "";
            if (kind == "lips" || kind == "mouth")
            {
                if (_mouthB == null) { why = "no mouth bone found on her"; return false; }
                world = _mouthB.position;
                return true;
            }
            var go = BSpots.FirstOrDefault(g => g != null && g.name.StartsWith("cl_btouch_" + kind + "_", StringComparison.Ordinal));
            if (go == null) { why = "no " + kind + " area on her (" + string.Join(", ", BSpots.Where(g => g != null).Select(g => g.name).ToArray()) + ")"; return false; }
            var area = go.GetComponent<Collider>();
            world = area != null ? area.bounds.center : go.transform.position;   // the butt area's middle is not its bone
            return true;
        }

        internal static void ClearBSpots()
        {
            // Off her body at once: Destroy waits for the frame's end, and AddBSpots, right after this, looks for her butt area by name.
            foreach (var g in BSpots) if (g != null) { g.SetActive(false); g.transform.SetParent(null, false); UnityEngine.Object.Destroy(g); }
            BSpots.Clear();
            _mouthB = null;
        }

        private static bool NearMouthB()
        {
            var cam = Camera.main;
            if (_mouthB == null || cam == null) return false;
            var at = cam.WorldToScreenPoint(_mouthB.position);
            if (at.z <= 0f) return false;
            var edge = cam.WorldToScreenPoint(_mouthB.position + cam.transform.right * 0.03f);
            var r = Mathf.Max(12f, Mathf.Abs(edge.x - at.x));
            return ((Vector2)Input.mousePosition - (Vector2)at).sqrMagnitude <= r * r;
        }

        /// <summary>A click on one of her areas.</summary>
        internal static void BTouch(string part, bool fromSpot, bool? lookMode = null, bool? atLips = null)
        {
            var t = _trio;
            var ts = G.TalkScene;
            if (t == null || ts == null || ts != _scene) return;
            if (fromSpot) { BTouchClicks++; BTouchLast = part + (SpotReady(ts) ? "" : " (ignored: the talk is busy or the mouse is over the UI)"); }
            if (fromSpot && (!SpotReady(ts) || !ClaimClick())) return;
            if (Busy("touch on " + GirlInfo.FirstName(t.B) + "'s " + part)) return;
            var look = lookMode ?? ts.touchMode == 1;
            if (part == "head" && !look && (atLips ?? NearMouthB())) part = "mouth";
            CampusLifePlugin.Log.LogInfo("Trio: " + (look ? "stared at" : "touched") + " " + GirlInfo.FirstName(t.B) + "'s " + part + (fromSpot ? " (mouse)" : " (direct)"));
            if (fromSpot) BTouchLast += " -> " + (look ? "look " : "touch ") + part;
            if (part == "mouth") { BKiss(); return; }
            if (look) { BLook(part); return; }
            switch (part)
            {
                case "head": BAct("pat"); return;
                case "hand": BAct("hand"); return;
                default: BGrope(part); return;
            }
        }

        /// <summary>Her chest or hips: how far you went decides how she takes it (the same rule as the first girl's).</summary>
        private static void BGrope(string part)
        {
            var t = _trio;
            var b0 = t.B;
            var a = t.A;
            var bn = GirlInfo.FirstName(b0);
            var key = "b:" + part;
            int n;
            SpotCount.TryGetValue(key, out n);
            SpotCount[key] = ++n;
            var gf = b0.isGirlfriend;
            var flirty = Interested(b0) && b0.relation >= 2 || MoodOf(b0) == CampusLife.Mood.Flirty || b0.lewdness >= 60 && b0.relation >= 1;
            var level = b0.isAnger ? 3 : Mathf.Clamp(n - 1 - (gf || flirty ? 1 : 0) + (gf || flirty ? 0 : 1), 0, 3);
            var q = level <= 0 ? Q.Good : level == 1 ? Q.Neutral : Q.Bad;
            var b = new SceneBuilder(a, true);
            b.Narr(BeatSay(b0, part == "bust" ? "grope.bust.narr" : "grope.hip.narr") ?? "...");
            Say2(b, b0, a, "duo3.reply", null, X("act", "grope", "part", part, "q", QWord(q)), FaceFor(q));   // duo3.dlg
            TLog("touch " + bn + " " + part + " #" + n + " level " + level);
            TrioPlay(b0, b, () =>
            {
                PayTo(b0, Lvl.Spicy, q, "t3b:touch_" + part);
                if (q != Q.Bad) Social.Add(Social.Kinds.Flirted, b0, new[] { a }, Witness.Touch);
                if (_trio != null) { _trio.LastTo = b0; _trio.LastLvl = Lvl.Spicy; _trio.LastQ = q; }
            });
        }

        /// <summary>The look mode on her: noticing her hair, her hands, or staring where you shouldn't.</summary>
        private static void BLook(string part)
        {
            var t = _trio;
            var b0 = t.B;
            var a = t.A;
            var bn = GirlInfo.FirstName(b0);
            var bold = part == "bust" || part == "butt";
            var q = bold ? (b0.isGirlfriend || b0.lewdness >= 50 ? Q.Neutral : Q.Bad) : Q.Good;
            var b = new SceneBuilder(a, true);
            b.Narr(BeatSay(b0, bold ? "look.bold.narr" : part == "hand" ? "look.hand.narr" : "look.face.narr") ?? "...");
            Say2(b, b0, a, "duo3.reply", null, X("act", bold ? "look_bold" : "look_plain", "part", part, "q", QWord(q)), FaceFor(q));   // duo3.dlg
            TrioPlay(b0, b, () =>
            {
                PayTo(b0, bold ? Lvl.Romance : Lvl.Friend, q, "t3b:look_" + part);
                if (_trio != null) { _trio.LastTo = b0; _trio.LastLvl = bold ? Lvl.Romance : Lvl.Friend; _trio.LastQ = q; }
            });
        }
    }
}
