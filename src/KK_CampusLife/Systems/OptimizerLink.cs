using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Tells School Optimizer (when installed) which girls should be at school today, so a lower student limit never
    /// leaves out the girl you have a request, story or spotlight with. Linked through reflection: no hard dependency.
    /// </summary>
    internal static class OptimizerLink
    {
        /// <summary>
        /// share a key, and both would take a reserved slot.
        /// </summary>
        public static readonly HashSet<SaveData.Heroine> Pinned = new HashSet<SaveData.Heroine>();

        public static void Register()
        {
            try
            {
                var t = AccessTools.TypeByName("Joojkk.SchoolOptimizer.SchoolOptimizerPlugin");
                if (t == null) return;
                var f = AccessTools.Field(t, "PriorityProviders");
                var list = f != null ? f.GetValue(null) as List<Func<SaveData.Heroine, float>> : null;
                if (list == null) return;
                list.Add(Score);
                var seats = AccessTools.Field(t, "SeatBonus");
                if (seats != null) seats.SetValue(null, new Func<int>(Scenery.SeatsSaved));
                CampusLifePlugin.Log.LogInfo("Linked with School Optimizer: girls with requests and stories are loaded first.");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("School Optimizer link: " + e.Message); }
        }

        private static float Score(SaveData.Heroine h)
        {
            if (h == null) return 0f;
            if (Pinned.Count > 0 && Pinned.Contains(h)) return 1000f;
            if (!Campus.On) return 0f;
            var key = GirlInfo.Key(h);
            var s = 0f;
            if (Board.ActiveFor(key).Any()) s += 20f;
            var gs = Campus.S.Girl(key);
            if (gs != null)
            {
                if (gs.Arc != null) s += 25f;
                s += Devotion.Level(gs) * 2f;
            }
            // The second girl of somebody else's story (rivals, friends) has to be there too.
            if (Campus.S.Girls.Values.Any(g => g != null && g.Arc != null && g.Arc.Other == key)) s += 15f;
            if (Spotlight.InSpotlight(h)) s += 10f;
            // The girl you are courting comes back tomorrow: with a big cast she was out two days in three.
            if (Social.TalkDaysRecent(h, 3) > 0) s += 5f + h.favor * 0.1f;
            return s;
        }
    }
}
