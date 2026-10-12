using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Big casts: each week a few girls are "in the spotlight" (most requests and stories come from them),
    /// while everyone else keeps living off-screen. Nobody is left out for more than three weeks.
    /// </summary>
    internal static class Spotlight
    {
        private static CampusState S { get { return Campus.S; } }

        public static bool IsActive
        {
            get { return Cfg.SpotlightEnabled.Value && Board.Students.Count >= Cfg.SpotlightMinCast.Value; }
        }

        public static void Rotate()
        {
            S.Spotlight.Clear();
            S.SpotlightWeek = S.Week;
            if (!IsActive) return;
            var students = Board.Students;
            var size = Mathf.Clamp(Cfg.SpotlightSize.Value, 1, students.Count);
            var picked = new List<SaveData.Heroine>();

            // Girls out of the spotlight for 3+ weeks go first.
            foreach (var h in students.OrderBy(x => S.Girl(GirlInfo.Key(x)).LastSpotlightWeek).ThenBy(_ => Random.value))
            {
                if (picked.Count >= size) break;
                if (S.Week - S.Girl(GirlInfo.Key(h)).LastSpotlightWeek >= 3) picked.Add(h);
            }
            // Then lonely girls and random ones.
            var rest = students.Except(picked).OrderBy(x => S.Girl(GirlInfo.Key(x)).Attention + Random.Range(0, 60)).ToList();
            foreach (var h in rest)
            {
                if (picked.Count >= size) break;
                picked.Add(h);
            }
            foreach (var h in picked)
            {
                var gs = S.Girl(GirlInfo.Key(h));
                gs.LastSpotlightWeek = S.Week;
                S.Spotlight.Add(gs.Key);
            }
            var names = string.Join(", ", picked.Select(GirlInfo.FirstName).ToArray());
            S.Log("This week's spotlight: " + names + ".");
            Toasts.Show("This week's spotlight: " + names);
        }

        public static bool InSpotlight(SaveData.Heroine h) { return S.Spotlight.Contains(GirlInfo.Key(h)); }

        private static readonly string[] NewsTogether =
        {
            "{0} and {1} were seen shopping together after class.",
            "{0} helped {1} with assignments during lunch.",
            "{0} and {1} shared a parfait at the cafe near the station.",
            "{0} and {1} stayed late practicing for something.",
            "{0} lent {1} a manga. They talked about it all day.",
        };

        private static readonly string[] NewsSolo =
        {
            "{0} got a perfect score on a quiz.",
            "{0} fell asleep in class and got scolded.",
            "{0} brought homemade cookies for her club.",
            "{0} was seen staring out the window for a long time.",
            "{0} got a new hairpin. Did you notice?",
            "{0} was asked out by an upperclassman. She said no.",
        };

        /// <summary>Daily off-screen life: attention decays, girls bond with each other, small news appears.</summary>
        public static void OffscreenLife()
        {
            var students = Board.Students;
            if (students.Count == 0) return;
            var yesterday = S.Day - 1;
            foreach (var h in students)
            {
                var gs = S.Girl(GirlInfo.Key(h));
                if (gs.LastTalkDay >= yesterday) continue;
                gs.Attention = Mathf.Clamp(gs.Attention - (h.isGirlfriend ? 8 : 4), 0, 100);
                // Forgotten girls slowly cool down (girlfriends notice more); never below the vanilla floor of 0.
                var drift = Cfg.OffscreenDrift.Value;
                if (drift > 0 && gs.Attention < 20 && Random.value < (h.isGirlfriend ? 0.6f : 0.3f))
                {
                    G.AddFavor(h, -Random.Range(1, drift + 1), 0);
                    if (h.isGirlfriend) gs.Mood = Mood.Lonely;
                }
            }

            if (!Cfg.OffscreenLife.Value || students.Count < 2) return;
            var events = Mathf.Clamp(students.Count / 10, 1, 3);
            for (var i = 0; i < events; i++)
            {
                var a = students[Random.Range(0, students.Count)];
                if (Random.value < 0.6f)
                {
                    var b = students[Random.Range(0, students.Count)];
                    if (b == a) continue;
                    // Clubmates bond faster.
                    Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(b), a.clubActivities == b.clubActivities ? 6 : 3);
                    S.Log("News: " + string.Format(NewsTogether[Random.Range(0, NewsTogether.Length)], GirlInfo.FirstName(a), GirlInfo.FirstName(b)));
                }
                else
                {
                    S.Log("News: " + string.Format(NewsSolo[Random.Range(0, NewsSolo.Length)], GirlInfo.FirstName(a)));
                }
            }
        }
    }
}
