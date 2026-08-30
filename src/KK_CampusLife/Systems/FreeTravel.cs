using System.Collections;
using KKAPI.Utilities;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Teleports from the menu cost no time: the period clock is noted before the warp and whatever it advanced during
    /// the map change (loading, fades) is given back once the player can walk again.
    /// </summary>
    internal static class FreeTravel
    {
        private static int _run;

        public static void Begin()
        {
            var c = G.Cycle;
            if (c == null) return;
            var run = ++_run;
            CampusLifePlugin.Instance.StartCoroutine(Wait(c.timer, run).PreventFromCrashing());
        }

        private static IEnumerator Wait(float before, int run)
        {
            var t0 = Time.realtimeSinceStartup;
            yield return null;
            // Until the new map is loaded and the player walks around again (at most 60 s).
            while (!G.IsFreeRoaming && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            if (run != _run) yield break;
            var c = G.Cycle;
            if (c == null || c.timer >= ActionGame.Cycle.TIME_LIMIT) yield break; // the period ended meanwhile
            var spent = c.timer - before;
            if (spent <= 0f) yield break;
            c.AddTimer(-spent / ActionGame.Cycle.TIME_LIMIT);
            CampusLifePlugin.Log.LogDebug("Teleport: gave back " + spent.ToString("0.0") + " s of the period clock.");
        }
    }
}
