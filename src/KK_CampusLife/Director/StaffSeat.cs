using System;
using System.Collections.Generic;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A teacher sitting at her desk (or standing at one of her staff spots) is a NavMesh obstacle, like the game's own
    /// event teacher (Fix keeps a NavMeshObstacle), and not a walking agent: an idle agent steps aside for the player's
    /// agent, so walking into her slid her across the room in her chair. The game already switches the agent off at some waiting points
    /// (isNavMeshOffsetPoint) and back on when she leaves (SetWaitPoint(null)); the same moments are used here.
    /// </summary>
    internal static class StaffSeat
    {
        private static readonly HashSet<string> StaffLayers = new HashSet<string> { "FixHealth", "FixMath", "FixPhysical", "FixHomeRoom", "Reading", "Wait", "Dinner" };
        private static readonly HashSet<NPC> Anchored = new HashSet<NPC>();
        private static float _next;

        public static void Install(Harmony harmony)
        {
            var m = AccessTools.Method(typeof(NPC), nameof(NPC.SetWaitPoint));
            harmony.Patch(m, prefix: new HarmonyMethod(typeof(StaffSeat), nameof(Before)), postfix: new HarmonyMethod(typeof(StaffSeat), nameof(After)));
        }

        /// <summary>A staff member who has arrived at one of her staff spots.</summary>
        public static bool Seated(NPC n)
        {
            if (n == null || n.heroine == null || !Teachers.IsTeacherVoice(n.heroine) || Director.Halted(n.heroine)) return false;
            var wd = n.wpData;
            if (wd == null || wd.wp == null || wd.wp.parameterList == null) return false;
            if (wd.paramIndex < 0 || wd.paramIndex >= wd.wp.parameterList.Count) return false;
            var p = wd.wp.parameterList[wd.paramIndex];
            return p != null && StaffLayers.Contains(p.layer);
        }

        // Before the game turns her agent back on: the obstacle goes first, the two never run together.
        private static void Before(NPC __instance, Base.WaitPointData wpData)
        {
            try { if (wpData == null && Anchored.Contains(__instance)) Release(__instance, false); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Staff seat: " + e.Message); }
        }

        private static void After(NPC __instance)
        {
            try { if (Campus.On && Seated(__instance) && __instance.isActive) Anchor(__instance); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Staff seat: " + e.Message); }
        }

        /// <summary>Twice a second: the game turns the agent on again when her map becomes the player's; she leaves without a
        /// SetWaitPoint(null) now and then.</summary>
        public static void Tick()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            try
            {
                var act = G.Act;
                if (act == null || act.npcList == null) { Anchored.Clear(); return; }
                foreach (var n in new List<NPC>(Anchored))
                    if (n == null || !Seated(n) || !n.isActive || !Campus.On) Release(n, true);
                if (!Campus.On) return;
                foreach (var n in act.npcList)
                    if (n != null && n.isActive && n.agent != null && n.agent.enabled && Seated(n)) Anchor(n);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Staff seat: " + e.Message); }
        }

        private static void Anchor(NPC n)
        {
            var a = n.agent;
            if (a == null) return;
            var ob = a.GetComponent<NavMeshObstacle>();
            if (ob == null) ob = a.gameObject.AddComponent<NavMeshObstacle>();
            if (a.enabled)
            {
                a.velocity = Vector3.zero;
                a.enabled = false;
            }
            ob.shape = NavMeshObstacleShape.Capsule;
            ob.radius = Mathf.Max(0.3f, a.radius);
            ob.height = Mathf.Max(1.2f, a.height);
            ob.center = new Vector3(0f, ob.height * 0.5f, 0f);
            // Carved: the player walks with NavMeshAgent.Move, which keeps to the NavMesh but ignores other agents and
            // obstacles that do not carve, so without the hole the player walked into her chair.
            ob.carving = true;
            ob.carveOnlyStationary = true;
            ob.enabled = true;
            Anchored.Add(n);
        }

        /// <summary>Obstacle off; with <paramref name="agentOn"/> the agent back on too (the game does it itself in SetWaitPoint).</summary>
        private static void Release(NPC n, bool agentOn)
        {
            Anchored.Remove(n);
            if (n == null) return;
            var a = n.agent;
            if (a == null) return;
            var ob = a.GetComponent<NavMeshObstacle>();
            if (ob != null) ob.enabled = false;
            var offset = n.wpData != null && n.wpData.wp != null && n.wpData.wp.isNavMeshOffsetPoint;
            if (agentOn && n.isActive && !offset && !a.enabled) a.enabled = true;
        }
    }
}
