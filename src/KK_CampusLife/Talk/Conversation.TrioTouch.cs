using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using IllusionUtility.GetUtility;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// menu", as the proposal koikatsu-arquitetura-conversa-com-duas already said). The game's touch areas exist only on
    /// the first girl, so the second one gets ours on her bones (head, lips, hands, chest, hips), answering the game's
    /// own touch and look modes (the icons at the top). The head pats her, near the lips it is a kiss, the hands take her
    /// hand, the chest and hips are a bold touch (or a stare in the look mode). Each goes through her own turn, with the
    /// first girl watching.
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
                foreach (var side in new[] { "L", "R" })
                    Spot(body, "cf_j_siri_" + side, "butt", 0.075f, cha.transform.right * (side == "L" ? -0.05f : 0.05f), layer);
                _mouthB = null;
                var head = cha.objHeadBone != null ? cha.objHeadBone.transform : body;
                foreach (var name in new[] { "cf_J_MouthBase_tr", "cf_J_MouthBase_s", "cf_J_MouthBase_rx", "cf_J_MouthMove" })
                {
                    var m = head.FindLoop(name);
                    if (m != null) { _mouthB = m.transform; break; }
                }
                CampusLifePlugin.Log.LogInfo("Trio touch areas on the second girl: " + string.Join(", ", BSpots.Select(g => g.name).ToArray()) + ", lips " + (_mouthB != null ? _mouthB.name : "none"));
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
            world = go.transform.position;
            return true;
        }

        internal static void ClearBSpots()
        {
            foreach (var g in BSpots) if (g != null) UnityEngine.Object.Destroy(g);
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
            if (fromSpot && !SpotReady(ts)) return;
            if (Scenes.Running) return;
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
            b.Narr(part == "bust" ? "Your hand brushes " + bn + "'s chest." : "Your hand slides down to " + bn + "'s hip.");
            Reply(b, b0, a, "touch_" + part, q, q == Q.Bad
                ? Tn("Kyaa! Wh-what are you doing?!", "HEY! Hands off!", "...Remove your hand. Now.", "Whoa, buddy. Not in front of her.")
                : q == Q.Neutral ? Tn("Ah... n-not here... she's watching...", "Eh?! H-hey, not now!", "...Not here.", "Bold~ but later, okay?")
                : Tn("Mm... you're so bold today...", "Hehe~ you can't keep your hands off me!", "...Hm. I don't mind.", "Mmm~ right in front of her? Naughty."));
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
            b.Narr(bold ? "Your eyes wander over " + bn + "." : part == "hand" ? "You look at " + bn + "'s hands." : "You look at " + bn + "'s face for a moment.");
            Reply(b, b0, a, "look_" + part, q, bold
                ? (q == Q.Bad ? Tn("U-um... my eyes are up here...", "Hey! Where are you looking?!", "...Eyes up.", "Enjoying the view? Too bad~")
                              : Tn("Ah... you're staring...", "Hehe, caught you looking!", "...You're staring.", "Like what you see~?"))
                : Tn("Um... is there something on my face?", "What? What is it?", "...What?", "Hmm? Lost in my eyes~?"));
            TrioPlay(b0, b, () =>
            {
                PayTo(b0, bold ? Lvl.Romance : Lvl.Friend, q, "t3b:look_" + part);
                if (_trio != null) { _trio.LastTo = b0; _trio.LastLvl = bold ? Lvl.Romance : Lvl.Friend; _trio.LastQ = q; }
            });
        }
    }
}
