using System;
using System.Collections.Generic;
using ActionGame.Chara;
using Manager;
using UnityEngine;
using UnityEngine.AI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A girl on your map shown as walking who does not move. Counted only while the game's clock runs (no menu, no talk,
    /// no scene, not stopped by the director): 6 s on the same spot and she is helped, one step more each time:
    /// <list type="number">
    /// <item>back on the navmesh where she stands, and a new action (her old one may be impossible: a taken or missing spot);</item>
    /// <item>the same, a little farther from the snag (3 m);</item>
    /// <item>straight to where she was going (on this map), or a new action again.</item>
    /// </list>
    /// </summary>
    internal static class StuckWatch
    {
        private sealed class Track { public Vector3 Pos; public float Still; public int Tries; }

        private static readonly Dictionary<NPC, Track> Tracks = new Dictionary<NPC, Track>();
        private static float _next, _last;
        private const float Step = 0.5f, Limit = 6f;

        public static int Fixed;
        public static string LastFix = "";

        public static void Update()
        {
            try
            {
                var now = Time.realtimeSinceStartup;
                if (now < _next) return;
                var dt = Mathf.Min(now - _last, 2f);
                _next = now + Step;
                _last = now;
                var act = G.Act;
                if (!Campus.On || act == null || act.npcList == null || Scenes.Running || G.TalkSceneOpen || Weather.InH) { Tracks.Clear(); return; }
                var paused = false;
                try { paused = Singleton<Game>.Instance.IsRegulate(true); } catch { }
                if (paused) return; // a menu or the clipboard: nobody walks, nobody is stuck
                var map = G.PlayerMapNo;
                foreach (var n in act.npcList)
                {
                    if (n == null || n.heroine == null) continue;
                    if (!Walking(n, map)) { Tracks.Remove(n); continue; }
                    Track t;
                    if (!Tracks.TryGetValue(n, out t)) { Tracks[n] = new Track { Pos = n.position }; continue; }
                    var moved = n.position - t.Pos; moved.y = 0f;
                    if (moved.sqrMagnitude > 0.25f * 0.25f) { t.Pos = n.position; t.Still = 0f; continue; }
                    t.Still += dt;
                    if (t.Still < Limit) continue;
                    Unstick(n, t);
                    t.Still = 0f;
                    t.Pos = n.position;
                    t.Tries++;
                }
                if (Tracks.Count > 60) Tracks.Clear();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("StuckWatch: " + e.Message); }
        }

        /// <summary>What the board and the status call walking: on her way somewhere, not arrived, free of talks and scenes.</summary>
        private static bool Walking(NPC n, int map) { return WhyNot(n, map) == null; }

        public static string WhyNot(NPC n, int map)
        {
            if (n.mapNo != map) return "another map";
            if (!n.isActive) return "not active";
            if (n.AI == null || !n.AI.isRunning) return "no AI running";
            if (n.isArrival) return "arrived";
            if (n.isTalking || n.isLesH || n.isOnanism) return "busy";
            if (Director.HaltReason(n.heroine) != null) return "held by the director (" + Director.HaltReason(n.heroine) + ")"; // his own walk watches that
            if (n.move != null && (n.move.isStop || n.move.isReglateMove)) return "stopped by the game";
            if (n.AI.result == null) return "no action";
            return null;
        }

        private static void Unstick(NPC n, Track t)
        {
            var name = GirlInfo.FirstName(n.heroine);
            var r = n.AI.result;
            var doing = Truth.Doing(n.heroine);
            var how = "";
            try
            {
                var agent = n.agent;
                var far = t.Tries >= 1 ? 3f : 1f;
                if (t.Tries >= 2 && r != null && r.point != null && r.point.MapNo == n.mapNo && agent != null)
                {
                    NavMeshHit hit;
                    var to = r.point.transform.position;
                    if (NavMesh.SamplePosition(to, out hit, 2f, NavMesh.AllAreas)) to = hit.position;
                    agent.enabled = true;
                    agent.Warp(to);
                    how = "taken to her spot";
                }
                else if (agent != null)
                {
                    agent.enabled = true;
                    NavMeshHit hit;
                    var from = n.transform.position + (t.Tries >= 1 ? n.transform.forward * -1.5f : Vector3.zero);
                    if (NavMesh.SamplePosition(from, out hit, far, NavMesh.AllAreas)) agent.Warp(hit.position);
                    how = t.Tries >= 1 ? "moved off the snag" : "back on the navmesh";
                }
                if (t.Tries < 2 || string.IsNullOrEmpty(how) || how != "taken to her spot")
                {
                    // Her action may be the snag (a spot taken or gone): she picks another one.
                    n.AI.TimeEnd();
                    how += ", new action";
                }
            }
            catch (Exception e) { how = "failed: " + e.Message; }
            Fixed++;
            LastFix = name + " (" + doing + ") stood " + Limit + " s at " + n.position.ToString("F1") + ": " + how;
            CampusLifePlugin.Log.LogInfo("Stuck: " + LastFix + " (try " + (t.Tries + 1) + ")");
        }
    }
}
