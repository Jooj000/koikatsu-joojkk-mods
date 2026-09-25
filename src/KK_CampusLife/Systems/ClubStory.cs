using System;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The club's rank as part of the story: the game's club report fills the gauge toward B (2000),
    /// A (5000) and S (10000). Each rank reached becomes a chapter of the story (Anthology and the log), the members are
    /// proud of it, and the panel says how far the next one is.
    /// </summary>
    internal static class ClubStory
    {
        private static readonly int[] Steps = { 2000, 5000, 10000 };
        private static readonly string[] Names = { "C", "B", "A", "S" };

        public static int Points
        {
            get
            {
                try { return Singleton<Manager.Game>.Instance.saveData.clubReport.point; }
                catch { return 0; }
            }
        }

        public static int Rank(int points) { var r = 0; while (r < Steps.Length && points >= Steps[r]) r++; return r; }

        /// <summary>"Rank B, 62% to A".</summary>
        public static string Label()
        {
            var p = Points;
            var r = Rank(p);
            if (r >= Steps.Length) return "Rank S";
            var from = r == 0 ? 0 : Steps[r - 1];
            var pct = (int)(100f * (p - from) / (Steps[r] - from));
            return "Rank " + Names[r] + ", " + pct + "% to " + Names[r + 1];
        }

        /// <summary>From Calendar.Tick: a new rank is a chapter.</summary>
        public static void Tick()
        {
            var S = Campus.S;
            if (S == null) return;
            var r = Rank(Points);
            int seen;
            if (!S.Buffs.TryGetValue("club_rank", out seen)) { S.Buffs["club_rank"] = r; return; }   // the rank the save already had
            if (r <= seen) return;
            S.Buffs["club_rank"] = r;
            var members = Board.Students.Where(h => h.isStaff).ToList();
            var text = r == 1 ? "The club reached rank B: people finally know it exists"
                     : r == 2 ? "The club reached rank A: the school talks about it"
                     : "The club reached rank S: the best club in school";
            S.Log(text + ".");
            Toasts.Show(text + "! (+3 favor with the members)");
            Anthology.Add("club", text, 2 + r, string.Join(",", members.Select(GirlInfo.Key).ToArray()));
            foreach (var h in members) G.AddFavor(h, 3, 0);
            CampusLifePlugin.Log.LogInfo("Club story: rank " + Names[r] + " (" + Points + " points)");
        }
    }
}
