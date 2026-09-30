using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// de quem realmente testemunhou"). Before, every girl on the map was a witness of anything. Now a girl saw it only
    /// if she was close enough for what it was (a whispered word carries a few meters, a kiss is seen across the room),
    /// was looking your way (or was right next to you), and nothing stood between her and you (walls, doors, floors;
    /// people and furniture below eye height don't count). What it was travels with the incident: hearing that you
    /// flirted is not the same as seeing you kiss her.
    /// </summary>
    internal static class Witness
    {
        public const string Talk = "talk", Flirt = "flirt", Hand = "hand", Touch = "touch", Kiss = "kiss", H = "h";

        /// <summary>How far each thing is noticed: looking your way, and without looking (heard, or right beside you).</summary>
        private static void Range(string what, out float sight, out float near)
        {
            switch (what)
            {
                case Flirt: sight = 6f; near = 3.5f; break;  // words: she has to be close, or see her blushing
                case Hand: sight = 12f; near = 2.5f; break;
                case Touch: sight = 9f; near = 2f; break;
                case Kiss: sight = 14f; near = 3f; break;
                case H: sight = 15f; near = 7f; break;     // heard through the noise, close by
                default: sight = 8f; near = 4f; break;      // talk
            }
        }

        /// <summary>Half the angle a girl notices things in front of her (eyes plus a turn of the head).</summary>
        private const float Cone = 75f;

        /// <summary>The girls (not her) who really saw or heard it, closest first. Logs who did and why the others didn't.</summary>
        public static List<SaveData.Heroine> Who(SaveData.Heroine her, string what)
        {
            var saw = new List<SaveData.Heroine>();
            try
            {
                var act = G.Act;
                if (act == null || act.Map == null || act.Player == null) return saw;
                float sight, near;
                Range(what, out sight, out near);
                var pt = act.Player.transform;
                var target = pt.position + Vector3.up * 1.2f;
                var map = act.Map.no;
                var notes = new List<string>();
                foreach (var n in act.npcList.Where(n => n != null && n.heroine != null && n.heroine != her && n.mapNo == map && GirlInfo.IsRegular(n.heroine))
                                             .OrderBy(n => (n.transform.position - pt.position).sqrMagnitude))
                {
                    var flat = n.transform.position - pt.position;
                    flat.y = 0f;
                    var dist = flat.magnitude;
                    if (dist > sight * 1.5f) continue;
                    var name = GirlInfo.FirstName(n.heroine);
                    if (Mathf.Abs(n.transform.position.y - pt.position.y) > 2.5f) { notes.Add(name + " no (other floor)"); continue; }
                    if (dist > sight) { notes.Add(name + " no (" + dist.ToString("0.0") + " m)"); continue; }
                    var angle = dist < 0.3f ? 0f : Vector3.Angle(n.transform.forward, -flat);
                    if (dist > near && angle > Cone) { notes.Add(name + " no (looking away " + (int)angle + "°, " + dist.ToString("0.0") + " m)"); continue; }
                    var eye = n.Head != null ? n.Head.position : n.transform.position + Vector3.up * 1.3f;
                    var wall = Blocked(eye, target, n.transform, pt);
                    if (wall != null) { notes.Add(name + " no (" + wall + " between, " + dist.ToString("0.0") + " m)"); continue; }
                    saw.Add(n.heroine);
                    notes.Add(name + " " + (dist <= near && angle > Cone ? "heard" : "saw") + " (" + dist.ToString("0.0") + " m, " + (int)angle + "°)");
                }
                // The other girl of a talk with both is right beside you: she always sees it.
                var other = Conversation.TrioOther(her);
                if (other != null && !saw.Contains(other)) { saw.Add(other); notes.Add(GirlInfo.FirstName(other) + " saw (right beside you)"); }
                if (notes.Count > 0) CampusLifePlugin.Log.LogInfo("Witness " + what + (her != null ? " with " + GirlInfo.FirstName(her) : "") + ": " + string.Join("; ", notes.ToArray()));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Witness: " + e.Message); }
            return saw;
        }

        /// <summary>The name of what stands between her eyes and you, or null when she has a clear view.</summary>
        private static string Blocked(Vector3 from, Vector3 to, Transform me, Transform player)
        {
            var dir = to - from;
            var dist = dir.magnitude;
            if (dist < 0.8f) return null;
            foreach (var hit in Physics.RaycastAll(from, dir / dist, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                // Right at her face or at you (her own hair, the girl you're with, the desk you lean on) doesn't block.
                if (hit.distance < 0.4f || hit.distance > dist - 0.8f) continue;
                var t = hit.collider.transform;
                if (t.IsChildOf(me) || t.IsChildOf(player) || hit.collider.GetComponentInParent<ChaControl>() != null) continue;
                // The floor (yuka) never hides you: a raised step or a sloped floor mesh is not a wall, and another
                // floor is already ruled out by the height check.
                var cn = hit.collider.name.ToLowerInvariant();
                if (cn.Contains("yuka") || cn.Contains("floor")) continue;
                return hit.collider.name;
            }
            return null;
        }
    }
}
