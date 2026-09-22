using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Shared memories: what the player and a girl went through together (a walk, a favor, a quiet moment, a Sunday).
    /// Every scene of the new conversation leaves one; a new memory waits as a callback that she brings up herself a day
    /// or more later ("You remember when you walked me to the gym?"), and the season's anthology reads the big ones.
    /// </summary>
    internal static class Memories
    {
        private static CampusState S { get { return Campus.S; } }

        /// <param name="text">Reads after "Remember when ...": "you walked me to the gym".</param>
        /// <param name="callback">She brings it up herself later (a callback waits).</param>
        public static Moment Add(SaveData.Heroine h, string kind, string text, int tone = 1, bool callback = true, string with = "")
        {
            if (h == null || S == null || string.IsNullOrEmpty(text)) return null;
            var gs = S.Girl(GirlInfo.Key(h));
            var m = new Moment
            {
                Id = S.NextMomentId++, Day = S.Day, Kind = kind ?? "", Text = text, Place = Places.CurrentId ?? "", With = with ?? "",
                Pending = callback, Tone = tone,
            };
            gs.Moments.Add(m);
            if (gs.Moments.Count > 24) gs.Moments.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Memory with " + GirlInfo.FirstName(h) + ": " + kind + " - " + text);
            try { Lore.FromMemory(h, kind, text, tone, with); } catch (System.Exception e) { CampusLifePlugin.Log.LogDebug("Lore memory: " + e.Message); }
            return m;
        }

        /// <summary>A memory old enough to bring up (not from today), the most recent first; null when none waits.</summary>
        public static Moment Callback(SaveData.Heroine h)
        {
            if (h == null) return null;
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Moments.Where(m => m.Pending && m.Day < S.Day && S.Day - m.Day <= 10).OrderByDescending(m => m.Day).FirstOrDefault();
        }

        public static void Mentioned(Moment m)
        {
            if (m != null) m.Pending = false;
        }

        /// <summary>The last memory with her (for concrete options: "Bring up the walk to the gym").</summary>
        public static Moment Last(SaveData.Heroine h, int maxAge = 14)
        {
            if (h == null) return null;
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Moments.Where(m => S.Day - m.Day <= maxAge).OrderByDescending(m => m.Id).FirstOrDefault();
        }

        public static int Count(SaveData.Heroine h)
        {
            return h == null ? 0 : S.Girl(GirlInfo.Key(h)).Moments.Count;
        }

        /// <summary>"yesterday", "on Monday", "the other day".</summary>
        public static string When(Moment m)
        {
            if (m == null) return "the other day";
            var ago = S.Day - m.Day;
            if (ago <= 0) return "earlier today";
            if (ago == 1) return "yesterday";
            if (ago < 7)
            {
                var w = G.Weekday;
                if (w.HasValue)
                {
                    var idx = (((int)w.Value - ago) % 7 + 7) % 7;
                    return "on " + new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" }[idx];
                }
            }
            return "the other day";
        }

        /// <summary>Second person to first person for her lines: "you walked me to the gym" stays, "your" stays.</summary>
        public static string HerView(Moment m) { return m == null ? "" : m.Text; }

        public static IEnumerable<Moment> All(SaveData.Heroine h)
        {
            return h == null ? Enumerable.Empty<Moment>() : S.Girl(GirlInfo.Key(h)).Moments;
        }
    }
}
