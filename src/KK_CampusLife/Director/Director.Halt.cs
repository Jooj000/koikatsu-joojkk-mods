using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Stopping a girl is the director's action, not taking her AI away. Her AI keeps running; the director holds her feet
    /// (the mover's own stop, the walk speed at zero, her spot kept every frame) and owns what she does next: whatever the
    /// game draws for her while she is stopped becomes a calm thing to do where she stands (Stage.Steer), and anything
    /// that goes to the player (call him for H, "come with me", talk to him) is swapped at once. Release gives her feet
    /// back; her AI goes on from where it was. Each stop and its reason goes to the director's log and to Truth ("stopped:
    /// talking with you").
    /// </summary>
    internal static partial class Director
    {
        private sealed class HaltInfo { public string Why; public Vector3 At; public Quaternion Rot; public float Since; public bool Face; public NPC Look; public bool Walking; public bool Driven; public Vector3 Dest; public float Best = float.MaxValue; public float BestAt; }

        private static readonly Dictionary<NPC, HaltInfo> _halted = new Dictionary<NPC, HaltInfo>();
        private static readonly int[] GoesToPlayer = { 5, 8, 23, 24, 25, 28, 29, 30 };

        public static bool Halted(SaveData.Heroine h) { return h != null && _halted.Keys.Any(n => n != null && n.heroine == h); }

        public static string HaltReason(SaveData.Heroine h)
        {
            var kv = _halted.FirstOrDefault(x => x.Key != null && x.Key.heroine == h);
            return kv.Value == null ? null : kv.Value.Why;
        }

        /// <summary>She stops where she is (and faces the player when face is set), until <see cref="Release"/>.</summary>
        public static void Halt(NPC n, string why, bool face = false)
        {
            try
            {
                if (n == null) return;
                HaltInfo hi;
                if (!_halted.TryGetValue(n, out hi)) { hi = new HaltInfo { Since = Time.realtimeSinceStartup }; _halted[n] = hi; }
                hi.Why = why ?? "";
                hi.At = n.transform.position;
                hi.Rot = n.transform.rotation;
                hi.Face = face;
                hi.Walking = false;
                hi.Driven = false;
                hi.Look = null;
                Feet(n, hi);
                // Something that walks her to the player would play out while she stands here: give her a calm one now.
                if (n.AI != null && Array.IndexOf(GoesToPlayer, n.AI.actionNo) >= 0 && G.Act != null && G.Act.actCtrl != null)
                {
                    var r = G.Act.actCtrl.SetAction(n.heroine, 10, n.mapNo);
                    if (r != null) HarmonyLib.Traverse.Create(n.AI).Field("actResult").Property("Value").SetValue(r);
                }
                CampusLifePlugin.Log.LogInfo("Director: halt " + GirlInfo.FirstName(n.heroine) + " (" + hi.Why + "), action " + (n.AI != null ? n.AI.actionNo : -1));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director halt: " + e.Message); }
        }

        /// <summary>Stopped, facing another girl (two talking on the map).</summary>
        public static void HaltFacing(NPC n, NPC other, string why)
        {
            Halt(n, why);
            HaltInfo hi;
            if (n != null && _halted.TryGetValue(n, out hi)) hi.Look = other;
        }

        /// <summary>
        /// The director walks her to a spot (to stand with another girl): her AI keeps running, the director owns where her
        /// feet go until she is there, then she stands facing <paramref name="other"/> (a halt like any other).
        /// </summary>
        public static void Lead(NPC n, Vector3 to, NPC other, string why)
        {
            try
            {
                if (n == null || n.agent == null) return;
                HaltInfo hi;
                if (!_halted.TryGetValue(n, out hi)) { hi = new HaltInfo { Since = Time.realtimeSinceStartup }; _halted[n] = hi; }
                hi.Why = why ?? "";
                hi.Look = other;
                hi.Walking = true;
                hi.Driven = false;
                hi.Dest = to;
                hi.Best = float.MaxValue;
                hi.BestAt = Time.realtimeSinceStartup;
                hi.At = n.transform.position;
                n.agent.enabled = true;
                UnityEngine.AI.NavMeshHit hit;
                if (!n.agent.isOnNavMesh && UnityEngine.AI.NavMesh.SamplePosition(n.transform.position, out hit, 3f, UnityEngine.AI.NavMesh.AllAreas)) n.agent.Warp(hit.position);
                if (n.move != null) n.move.isStop = false;
                if (n.agent.isOnNavMesh) { n.agent.isStopped = false; if (n.agent.speed < 0.5f) n.agent.speed = 1.5f; n.agent.SetDestination(to); }
                CampusLifePlugin.Log.LogInfo("Director: lead " + GirlInfo.FirstName(n.heroine) + " (" + hi.Why + "), " + (to - n.transform.position).magnitude.ToString("0.0") + " m");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director lead: " + e.Message); }
        }

        /// <summary>
        /// The director's scene moves her itself (a duo walking over to you, Duo.WalkTo): her AI keeps running and anything
        /// it draws that goes to the player is swapped, but her feet are the scene's until it calls Halt or Release.
        /// </summary>
        public static void Drive(NPC n, string why)
        {
            if (n == null) return;
            HaltInfo hi;
            if (!_halted.TryGetValue(n, out hi)) { hi = new HaltInfo { Since = Time.realtimeSinceStartup }; _halted[n] = hi; }
            hi.Why = why ?? "";
            hi.Driven = true;
            hi.Walking = false;
            hi.Look = null;
            hi.At = n.transform.position;
            CampusLifePlugin.Log.LogInfo("Director: " + GirlInfo.FirstName(n.heroine) + " in a scene that moves her (" + hi.Why + "), action " + (n.AI != null ? n.AI.actionNo : -1));
        }

        /// <summary>She is still being walked somewhere by the director.</summary>
        public static bool Walking(NPC n) { HaltInfo hi; return n != null && _halted.TryGetValue(n, out hi) && hi.Walking; }

        /// <summary>Her feet back: she goes on with what her AI was doing.</summary>
        public static void Release(NPC n)
        {
            try
            {
                if (n == null) return;
                HaltInfo hi;
                if (!_halted.TryGetValue(n, out hi)) return;
                _halted.Remove(n);
                var moved = (n.transform.position - hi.At).magnitude;
                if (n.move != null) n.move.isStop = false;
                if (n.agent != null && n.agent.enabled && n.agent.isOnNavMesh) n.agent.isStopped = false;
                CampusLifePlugin.Log.LogInfo("Director: release " + GirlInfo.FirstName(n.heroine) + " (" + hi.Why + ") after " + (Time.realtimeSinceStartup - hi.Since).ToString("0") + " s, moved " + moved.ToString("0.00") + " m" + (moved > 0.3f ? " (SLID)" : ""));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director release: " + e.Message); }
        }

        public static void ReleaseAll() { foreach (var n in _halted.Keys.ToList()) Release(n); }

        /// <summary>Every frame: the mover and the agent would take her along her path again.</summary>
        public static void KeepHalted()
        {
            if (_halted.Count == 0) return;
            foreach (var kv in _halted.ToList())
            {
                var n = kv.Key;
                try
                {
                    if (n == null || n.heroine == null || !n.gameObject.activeInHierarchy && !G.TalkSceneOpen) { _halted.Remove(n); continue; }
                    if (kv.Value.Driven) continue;
                    if (kv.Value.Walking) Walk(n, kv.Value);
                    else Feet(n, kv.Value);
                }
                catch (Exception e) { _halted.Remove(n); CampusLifePlugin.Log.LogDebug("Director keep halted: " + e.Message); }
            }
        }

        /// <summary>One frame of a walk the director leads: there, stuck for 2.5 s or 20 s gone, she stops where she is.</summary>
        private static void Walk(NPC n, HaltInfo hi)
        {
            var d = (n.transform.position - hi.Dest).magnitude;
            var now = Time.realtimeSinceStartup;
            if (d < hi.Best - 0.1f) { hi.Best = d; hi.BestAt = now; }
            var agentOk = n.agent != null && n.agent.enabled && n.agent.isOnNavMesh;
            if (!agentOk || d < 0.35f || now - hi.BestAt > 2.5f || now - hi.Since > 20f)
            {
                if (agentOk) n.agent.ResetPath();
                hi.Walking = false;
                hi.At = n.transform.position;
                hi.Rot = n.transform.rotation;
                CampusLifePlugin.Log.LogInfo("Director: " + GirlInfo.FirstName(n.heroine) + " got there (" + d.ToString("0.0") + " m left, " + hi.Why + ")");
                Feet(n, hi);
                return;
            }
            // Her AI may give the agent its own spot: the director's comes first.
            if ((n.agent.destination - hi.Dest).sqrMagnitude > 0.09f) n.agent.SetDestination(hi.Dest);
            if (n.move != null) n.move.isStop = false;
            n.agent.isStopped = false;
            n.AgentVelocityMoveAnimeUpdate();
            var v = n.agent.velocity;
            v.y = 0f;
            if (v.sqrMagnitude > 0.04f) n.transform.rotation = Quaternion.Slerp(n.transform.rotation, Quaternion.LookRotation(v), 0.25f);
        }

        private static void Feet(NPC n, HaltInfo hi)
        {
            if (n.move != null) { n.move.isStop = true; n.move.animParamSpeed = 0f; }
            if (n.agent != null && n.agent.enabled && n.agent.isOnNavMesh) { n.agent.isStopped = true; n.agent.velocity = Vector3.zero; }
            if ((n.transform.position - hi.At).sqrMagnitude > 0.0004f) n.transform.position = hi.At;
            if (hi.Look != null && hi.Look.gameObject.activeInHierarchy)
            {
                var look = hi.Look.transform.position - n.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) n.transform.rotation = Quaternion.Slerp(n.transform.rotation, Quaternion.LookRotation(look), 0.2f);
            }
            else if (hi.Face && G.Act != null && G.Act.Player != null)
            {
                var look = G.Act.Player.transform.position - n.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) n.transform.rotation = Quaternion.LookRotation(look);
            }
            else n.transform.rotation = hi.Rot;
        }
    }
}
