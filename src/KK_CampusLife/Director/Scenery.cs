using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// When there are more students than the day's limit, professors and story heroines are almost scenery and each one
    /// that walks around keeps a student home. The director draws them with a low chance, only a few a day, and is sure of
    /// the ones with an event today. The seats saved go back to the students: School Optimizer reads <see
    /// cref="SeatsSaved"/>. Whoever became a regular student is in the normal draw and never gets here.
    /// </summary>
    internal static class Scenery
    {
        private const int Chance = 25, MaxADay = 2;

        private static int _stamp = -1;
        private static HashSet<SaveData.Heroine> _today;
        private static int _saved;

        public static string Plan = "";

        /// <summary>Who would walk around with nothing limiting them.</summary>
        private static List<SaveData.Heroine> Candidates()
        {
            return StoryCast.StoryGirls.Where(h => !h.isTaked && !StoryCast.Graduated(h)).Concat(Teachers.All.Where(Teachers.Walks)).Distinct().ToList();
        }

        /// <summary>The optimizer's limit, or the game's own without it.</summary>
        public static int Limit
        {
            get
            {
#if DEBUG_TOOLS
                if (TestLimit > 0) return TestLimit;
#endif
                var max = Manager.Config.ActData.MaxCharaNum;
                try
                {
                    var t = AccessTools.TypeByName("Joojkk.SchoolOptimizer.SchoolOptimizerPlugin");
                    var f = t != null ? AccessTools.Field(t, "MaxStudents") : null;
                    var e = f != null ? f.GetValue(null) as BepInEx.Configuration.ConfigEntry<int> : null;
                    if (e != null && e.Value > 0 && e.Value < max) max = e.Value;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Scenery limit: " + e.Message); }
                return max;
            }
        }

        private static bool Sure(SaveData.Heroine h)
        {
            if (OptimizerLink.Pinned.Contains(h) || h == StoryCast.Pick || h == StoryCast.Waiting) return true;
            if (Spotlight.InSpotlight(h) || Board.ActiveFor(GirlInfo.Key(h)).Any()) return true;
            return h.isTeacher && Teachers.MilestoneDue(h);
        }

        private static void Refresh()
        {
            var stamp = Campus.S.Day * 16 + (int)G.CurrentMask;
            if (stamp == _stamp && _today != null) return;
            _stamp = stamp;
            _saved = 0;
            var all = Candidates();
            var students = G.Heroines.Count(h => h != null && h.fixCharaID == 0);
            if (!Campus.On || !Cfg.DirectorEnabled.Value || students <= Limit)
            {
                _today = new HashSet<SaveData.Heroine>(all);
                Plan = "scenery: all " + all.Count + " (" + students + " students, limit " + Limit + ")";
                return;
            }
            var sure = all.Where(Sure).ToList();
            // The same draw all day: a professor does not vanish between two periods.
            var drawn = all.Except(sure).Select(h => new { h, roll = Math.Abs((GirlInfo.Key(h) + Campus.S.Day).GetHashCode()) % 100 })
                .Where(x => x.roll < Chance).OrderBy(x => x.roll).Take(MaxADay).Select(x => x.h).ToList();
            _today = new HashSet<SaveData.Heroine>(sure.Concat(drawn));
            _saved = all.Count - _today.Count;
            Plan = "scenery: " + students + " students over the limit " + Limit + ", " + _saved + " seats back | sure: " + Names(sure) + " | drawn: " + Names(drawn);
            CampusLifePlugin.Log.LogInfo("Director " + Plan);
        }

        private static string Names(IEnumerable<SaveData.Heroine> l)
        {
            var a = l.Select(GirlInfo.FirstName).ToArray();
            return a.Length == 0 ? "nobody" : string.Join(", ", a);
        }

        public static bool Today(SaveData.Heroine h)
        {
            try { Refresh(); return _today.Contains(h); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Scenery: " + e.Message); return true; }
        }

        /// <summary>Seats the scenery left free today: School Optimizer adds them to its limit.</summary>
        public static int SeatsSaved()
        {
            try { Refresh(); return _saved; }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Scenery: " + e.Message); return 0; }
        }

#if DEBUG_TOOLS
        public static int TestLimit;
#endif

        public static void Reset() { _stamp = -1; _today = null; }
    }
}
