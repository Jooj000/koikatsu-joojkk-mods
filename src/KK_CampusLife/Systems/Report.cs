using System.Collections.Generic;
using System.Linq;
using ActionGame;

namespace Joojkk.CampusLife
{
    /// <summary>Weekly summary shown on Saturday night.</summary>
    internal static class Report
    {
        private static CampusState S { get { return Campus.S; } }

        public static void MaybeShow()
        {
            if (!Cfg.WeeklyReport.Value) return;
            var w = G.Weekday;
            if (!w.HasValue || w.Value != Cycle.Week.Saturday || S.LastReportWeek == S.Week) return;
            S.LastReportWeek = S.Week;
            Show();
        }

        public static void Show()
        {
            ReportUi.Show(Build());
        }

        public static List<string> BuildNow() { return Build(); }

        private static List<string> Build()
        {
            var t = new List<string>();
            var ws = S.WeekStats;
            System.Func<string, int> n = k => { int v; return ws.TryGetValue(k, out v) ? v : 0; };

            t.Add("Week " + S.ShownWeek + " report");
            t.Add("");
            t.Add("Conversations: " + n("talks") + "    Requests done: " + n("requests") + "    Missed: " + n("missed"));
            t.Add("Activities: " + n("activities") + "    Intimate moments: " + n("h") + "    Stories finished: " + n("arcs"));
            t.Add("Money: " + S.Money + " yen (spent " + n("spent") + ")    Reputation: " + S.Reputation + "/100");
            t.Add("You: STR " + G.Str + "  INT " + G.Int + "  SEN " + G.Sens);
            if (S.ExamScores.Count > 0) t.Add("Last exam: " + S.ExamScores[S.ExamScores.Count - 1] + "/100");
            t.Add("");

            var students = Board.Students;
            var close = students.OrderByDescending(h => h.favor + (h.isGirlfriend ? 100 : 0)).Take(5).ToList();
            if (close.Count > 0)
                t.Add("Closest to you: " + string.Join(", ", close.Select(h => GirlInfo.FirstName(h) + " (" + h.favor + ")").ToArray()));

            var neglected = students.Where(h => S.Girl(GirlInfo.Key(h)).Attention < 20)
                .OrderBy(h => S.Girl(GirlInfo.Key(h)).Attention).Take(5).ToList();
            if (neglected.Count > 0)
                t.Add("Feeling ignored: " + string.Join(", ", neglected.Select(GirlInfo.FirstName).ToArray()));

            var arcs = Arcs.Describe().ToList();
            if (arcs.Count > 0)
            {
                t.Add("Ongoing stories:");
                t.AddRange(arcs.Select(a => "  - " + a));
            }

            var upcoming = Calendar.UpcomingEvents().Take(4).Select(u => u.Name + ": " + u.When).ToList();
            if (upcoming.Count > 0)
            {
                t.Add("Coming up:");
                t.AddRange(upcoming.Select(a => "  - " + a));
            }

            t.Add("");
            t.Add(Verdict(n("requests"), n("missed"), n("talks")));
            return t;
        }

        private static string Verdict(int done, int missed, int talks)
        {
            if (done == 0 && talks == 0) return "A quiet week. The club board is waiting for you.";
            if (missed > done) return "A rough week. Some girls waited for you in vain.";
            if (done >= 10) return "What a week! The whole school is talking about the Koikatsu Club.";
            return "A good week. Keep it up.";
        }
    }
}
