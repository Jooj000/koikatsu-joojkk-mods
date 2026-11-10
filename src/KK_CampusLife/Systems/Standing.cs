using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Each girl's reputation at school, 0-100. Once a day it drifts toward what her friendships say (friends raise it,
    /// enemies lower it, her club and your reputation if she is your girlfriend count a little) and moves with what
    /// happened: a hot rumor about her, being rude or breaking a promise lower it; being helped, a new couple, making up
    /// raise it. Popular girls are sought out more by the others (Intents) and are cheerful more often; excluded girls get
    /// lonely and fewer visits. Lewd girls also warm up a little by themselves each day, so the H between them does not
    /// wait for the player.
    /// </summary>
    internal static class Standing
    {
        private static CampusState S { get { return Campus.S; } }

        public static int Of(SaveData.Heroine h) { return h == null || S == null ? 50 : S.Girl(GirlInfo.Key(h)).Rep; }

        public static void Add(SaveData.Heroine h, int d, string why)
        {
            if (h == null || S == null || d == 0) return;
            var gs = S.Girl(GirlInfo.Key(h));
            var before = gs.Rep;
            gs.Rep = Mathf.Clamp(gs.Rep + d, 0, 100);
            if (Mathf.Abs(d) >= 3) CampusLifePlugin.Log.LogInfo("Standing: " + GirlInfo.FirstName(h) + " " + before + " -> " + gs.Rep + " (" + why + ")");
        }

        public static string Label(int r)
        {
            return r >= 80 ? "Popular" : r >= 60 ? "Well liked" : r >= 40 ? "Normal" : r >= 20 ? "Overlooked" : "Outcast";
        }

        /// <summary>New day: reputations drift and react to yesterday's facts; lewd girls warm up.</summary>
        public static void Daily()
        {
            try
            {
                if (S == null) return;
                var girls = Social.Students.ToList();
                if (girls.Count == 0) return;
                var moved = 0;
                // Relative to the school: having more friends than most makes her popular, fewer makes her left out
                // (an absolute count put every girl at the cap, 53 girls with many friends each).
                var friendsOf = girls.ToDictionary(h => h, h => girls.Count(o => o != h && Social.Bond(h, o) >= 40));
                var enemiesOf = girls.ToDictionary(h => h, h => girls.Count(o => o != h && Social.Bond(h, o) <= -30));
                var avgF = (float)friendsOf.Values.Average();
                var avgE = (float)enemiesOf.Values.Average();
                foreach (var h in girls)
                {
                    var key = GirlInfo.Key(h);
                    var gs = S.Girl(key);
                    var target = 50f + Mathf.Clamp((friendsOf[h] - avgF) * 6f, -30f, 30f) - Mathf.Clamp((enemiesOf[h] - avgE) * 6f, -25f, 25f)
                        + (GirlInfo.InNoClub(h) ? -3 : 3) + (h.isGirlfriend ? (S.Reputation - 50) / 5 : 0);
                    // Friends of popular girls are seen with them.
                    target += Mathf.Min(3, girls.Count(o => o != h && Social.Bond(h, o) >= 40 && S.Girl(GirlInfo.Key(o)).Rep >= 70)) * 2;
                    var goal = Mathf.Clamp(Mathf.RoundToInt(target), 5, 95);
                    var before = gs.Rep;
                    var diff = goal - gs.Rep;
                    if (diff != 0) gs.Rep += Math.Sign(diff) * Mathf.Min(Mathf.Abs(diff), 1 + Mathf.Abs(diff) / 8);
                    if (Rumors.HeatAbout(key) >= 40) gs.Rep -= 2;
                    foreach (var f in S.Lore.Where(x => x.Day >= S.Day - 1 && x.Involves(key)))
                    {
                        var subj = f.Subject == key;
                        switch (f.Verb)
                        {
                            case "insulted": case "ignored": case "broke_promise": case "cheated": case "left_for": if (subj) gs.Rep -= 3; break;
                            case "fought": gs.Rep -= 1; break;
                            case "helped": case "stood_up_for": gs.Rep += subj ? 2 : 1; break;
                            case "made_up": case "started_dating": gs.Rep += 1; break;
                            case "kept_promise": if (subj) gs.Rep += 1; break;
                        }
                    }
                    gs.Rep = Mathf.Clamp(gs.Rep, 0, 100);
                    if (gs.Rep != before) moved++;

                    // On her own: a lewd-minded girl gets a little more so each day (Moods reads it for Flirty).
                    var q = GirlInfo.Quirks(h);
                    // Most cards carry one of the two quirks, so it is a chance, not every day.
                    if (Cfg.StudentH.Value && h.lewdness < 90 && (gs.Mood == Mood.Flirty || (q.bitch || q.mutturi) && (gs.Mood == Mood.Bored || gs.Mood == Mood.Lonely || gs.Mood == Mood.Cheerful)))   // on a free-feeling day, not every day
                        G.AddFavor(h, 0, gs.Mood == Mood.Flirty ? 2 : 1);
                }
                CampusLifePlugin.Log.LogInfo("Standing: day " + S.Day + ", " + moved + " reputations moved; popular " +
                    string.Join(", ", girls.Where(h => S.Girl(GirlInfo.Key(h)).Rep >= 70).Select(h => GirlInfo.FirstName(h) + " " + S.Girl(GirlInfo.Key(h)).Rep).ToArray()) +
                    "; outcast " + string.Join(", ", girls.Where(h => S.Girl(GirlInfo.Key(h)).Rep < 25).Select(h => GirlInfo.FirstName(h) + " " + S.Girl(GirlInfo.Key(h)).Rep).ToArray()));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Standing: " + e.Message); }
        }

        public static string Describe()
        {
            if (S == null) return "no state";
            return string.Join(", ", Social.Students.OrderByDescending(h => S.Girl(GirlInfo.Key(h)).Rep)
                .Select(h => GirlInfo.FirstName(h) + " " + S.Girl(GirlInfo.Key(h)).Rep + " (" + Label(S.Girl(GirlInfo.Key(h)).Rep) + ", lewd " + h.lewdness + ")").ToArray());
        }
    }
}
