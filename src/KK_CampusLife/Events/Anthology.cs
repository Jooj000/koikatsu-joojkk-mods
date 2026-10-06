using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Class Anthology: the big moments of the season are written down as they happen (couples, breakups, a girl won
    /// over, forgiveness, club projects, the council race, traditions, Sundays, stories between students). On the last
    /// Saturday of each 9-week loop a short ceremony looks back: the moment of the season, the people who changed, the
    /// stories still open; you choose what to celebrate. Girls whose story with you reached its end get an epilogue
    /// ("What Comes After School"). The season is archived and the next one begins; it can be read any time from the
    /// clipboard.
    /// </summary>
    internal static class Anthology
    {
        private static CampusState S { get { return Campus.S; } }
        private const int CeremonyWeek = 8; // loop week; the ceremony is on its Saturday
        private static float _nextTry;

        public static void Add(string kind, string text, int weight, string girls)
        {
            if (!Cfg.Anthology.Value || string.IsNullOrEmpty(text)) return;
            // The same thing twice the same day counts once.
            if (S.Milestones.Any(m => m.Day == S.Day && m.Text == text)) return;
            S.Milestones.Add(new Milestone { Day = S.Day, Week = S.Week, Weight = weight, Kind = kind, Text = text, Girls = girls ?? "" });
            if (S.Milestones.Count > 120) S.Milestones.RemoveAt(0);
        }

        private static IEnumerable<Milestone> ThisSeason { get { return S.Milestones.Where(m => m.Week >= S.SeasonStartWeek); } }

        public static string Status()
        {
            var n = ThisSeason.Count();
            return "Season " + S.Season + ": " + n + " moment" + (n == 1 ? "" : "s") + " written down. Ceremony on Saturday of loop week " + CeremonyWeek + ".";
        }

        /// <summary>Lines for the clipboard page: this season so far, then the archive.</summary>
        public static List<string> PageLines()
        {
            var l = new List<string> { "THE CLASS ANTHOLOGY", "Season " + S.Season + " (so far)" };
            var top = ThisSeason.OrderByDescending(m => m.Weight).ThenByDescending(m => m.Day).Take(8).ToList();
            if (top.Count == 0) l.Add("  Nothing written yet. Go make some memories.");
            foreach (var m in top) l.Add("  - " + m.Text + " (week " + m.Week + ")");
            if (S.Archive.Count > 0)
            {
                l.Add("");
                l.Add("Past seasons");
                foreach (var a in S.Archive.AsEnumerable().Reverse().Take(3)) foreach (var line in a.Split('\n')) l.Add("  " + line);
            }
            return l;
        }

        public static void Tick()
        {
            if (!Campus.On || !Cfg.Anthology.Value || !Scenes.CanPlayOnMap || Duo.Busy || WalkTalk.Active) return;
            if (Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 20f;
            if (Calendar.LoopWeek != CeremonyWeek || G.Weekday != Cycle.Week.Saturday || S.LastCeremonyWeek == S.Week) return;
            if (G.CurrentMask != PeriodMask.After && G.CurrentMask != PeriodMask.Club) return;
            Ceremony();
        }

        // ================= the ceremony =================

        private sealed class Changed { public string Text; public int Score; }

        /// <summary>People who changed: pairs whose stage moved, girls with the most moments.</summary>
        private static List<string> PeopleWhoChanged()
        {
            var l = new List<Changed>();
            foreach (var p in S.Pairs.Values)
            {
                var a = GirlInfo.FirstName(p.A); var b = GirlInfo.FirstName(p.B);
                if (p.Stage == PairStage.Couple) l.Add(new Changed { Text = a + " and " + b + ", together since day " + Mathf.Max(0, p.CoupleSince), Score = 6 });
                else if (p.Stage == PairStage.Broken) l.Add(new Changed { Text = a + " and " + b + ", who tried and parted", Score = 3 });
                else if (p.ArcNote == "good") l.Add(new Changed { Text = a + " and " + b + ", closer than when the season began", Score = 4 });
            }
            foreach (var g in ThisSeason.SelectMany(m => m.Girls.Split(',')).Where(x => x != "").GroupBy(x => x).OrderByDescending(x => x.Count()).Take(3))
                l.Add(new Changed { Text = GirlInfo.FirstName(g.Key) + ", in " + g.Count() + " of this season's moments", Score = g.Count() });
            return l.OrderByDescending(x => x.Score).Select(x => x.Text).Distinct().Take(4).ToList();
        }

        private static List<string> Unfinished()
        {
            var l = new List<string>();
            foreach (var p in BondsInMotion.Active) l.Add(GirlInfo.FirstName(p.A) + " and " + GirlInfo.FirstName(p.B) + ": " + BondsInMotion.Label(p.Arc));
            foreach (var p in S.Pairs.Values.Where(x => x.Stage == PairStage.Crush || x.Stage == PairStage.Talking)) l.Add("something between " + GirlInfo.FirstName(p.A) + " and " + GirlInfo.FirstName(p.B));
            var r = Rumors.Main;
            if (r != null) l.Add("the rumor that " + Rumors.What(r, 1));
            foreach (var h in Social.Students.Where(x => x.isGirlfriend && S.Girl(GirlInfo.Key(x)).Chapter < 3)) l.Add("you and " + GirlInfo.FirstName(h) + ", still writing your chapters");
            return l.Take(4).ToList();
        }

        /// <summary>Girls whose story reached its end this season get an epilogue.</summary>
        private static List<SaveData.Heroine> EpilogueGirls()
        {
            return Social.Students.Where(h => S.Girl(GirlInfo.Key(h)).Chapter >= 3 || h.schoolClass == 3 && h.favor >= 60).Take(3).ToList();
        }

        public static string Epilogue(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var dream = string.IsNullOrEmpty(gs.Epilogue) ? Seasons.Dream(h) : gs.Epilogue;
            var friend = Social.BestFriend(h);
            var trad = gs.Traditions.Count > 0 ? gs.Traditions[gs.Traditions.Count - 1] : null;
            var name = GirlInfo.FirstName(h);
            var text = "After school, " + name + " set out to " + dream + ".";
            if (friend != null) text += " " + GirlInfo.FirstName(friend) + " still calls her every week.";
            if (trad != null) text += " She never quite gave up " + trad + ".";
            if (h.isGirlfriend) text += " And on the hard days, she knows exactly who to call.";
            return text;
        }

        public static bool Ceremony(SaveData.Heroine hostGirl = null)
        {
            var moments = ThisSeason.OrderByDescending(m => m.Weight).ThenByDescending(m => m.Day).ToList();
            var moment = moments.FirstOrDefault();
            var people = PeopleWhoChanged();
            var open = Unfinished();
            var epi = EpilogueGirls();
            var host = hostGirl ?? Calendar.TopGirls(1).FirstOrDefault();
            var b = new SceneBuilder(null, false);
            b.Bg("gym");
            b.Narr("THE CLASS ANTHOLOGY. Season " + S.Season + ": \"What We Remember\".");
            b.Narr("The whole class gathers in the gym for a small ceremony. Someone made a slideshow. Someone else is already crying.");
            b.Narr("The moment of the season: " + (moment != null ? moment.Text + "." : "a quiet season, the kind you only miss later."));
            if (people.Count > 0) b.Narr("People who changed: " + string.Join("; ", people.ToArray()) + ".");
            if (open.Count > 0) b.Narr("Unfinished stories: " + string.Join("; ", open.ToArray()) + ".");
            if (host != null) b.Say(host, Lines.T(GirlInfo.ToneOf(host), "...It went by so fast. What should we celebrate? || ...A whole loop already. What should we remember most? || It feels like yesterday we started... What do we celebrate? || Um... I'd like to raise a glass. To what, though?", "Okay! Speech time! What do we celebrate?! || Wooo! End of the loop! What's the big toast?! || Okay okay, everyone quiet! What are we celebrating?! || Party time! So what's the headline of this loop?!", "We should choose what to highlight. || Let's decide what this loop was about. || A toast needs a subject. Choose one. || ...Pick the moment worth remembering.", "So. What's the toast to? || Well? What are we drinking to, hm? || Toast time. Make it a good one. || So, which moment gets the honor?"));
            b.Choice("Celebrate the club", "an_club", "Celebrate the friendships and couples", "an_bonds", "Celebrate how everyone grew", "an_growth", "Celebrate the class as a whole", "an_class");
            foreach (var t in new[] { "an_club", "an_bonds", "an_growth", "an_class" })
            {
                b.Branch(t);
                b.Narr(t == "an_club" ? "You raise a glass to the club" + (string.IsNullOrEmpty(S.ClubIdentity) ? "" : ", " + S.ClubIdentity) + ". The members cheer the loudest."
                    : t == "an_bonds" ? "You raise a glass to everyone who found someone this season. A few couples go very red."
                    : t == "an_growth" ? "You raise a glass to everyone who is not who they were nine weeks ago. People nod, quietly."
                    : "You raise a glass to the class. Everyone. Even the ones who slept through it.");
                b.End();
            }
            if (epi.Count > 0)
            {
                b.Bg("classroom_sunset");
                b.Narr("What Comes After School.");
                foreach (var h in epi) b.Narr(Epilogue(h));
            }
            var ok = Scenes.PlayOnMap(b, c =>
            {
                try { Archive(c, moment, people, open, epi); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });
            if (ok) S.LastCeremonyWeek = S.Week;
            return ok;
        }

        private static void Archive(string emphasis, Milestone moment, List<string> people, List<string> open, List<SaveData.Heroine> epi)
        {
            var lines = new List<string> { "Season " + S.Season + " (weeks " + S.SeasonStartWeek + "-" + S.Week + "), remembered for " + (emphasis == "an_club" ? "the club" : emphasis == "an_bonds" ? "its bonds" : emphasis == "an_growth" ? "how everyone grew" : "the class") };
            if (moment != null) lines.Add("Moment: " + moment.Text);
            if (people.Count > 0) lines.Add("Changed: " + string.Join("; ", people.ToArray()));
            if (open.Count > 0) lines.Add("Still open: " + string.Join("; ", open.ToArray()));
            foreach (var h in epi) { var gs = S.Girl(GirlInfo.Key(h)); gs.Epilogue = Epilogue(h); lines.Add("Epilogue, " + GirlInfo.FirstName(h) + ": " + gs.Epilogue); }
            S.Archive.Add(string.Join("\n", lines.ToArray()));
            if (S.Archive.Count > 8) S.Archive.RemoveAt(0);
            var r = emphasis == "an_club" ? new Rewards { Club = 6, Rep = 2 } : emphasis == "an_bonds" ? new Rewards { Rep = 3 } : emphasis == "an_growth" ? new Rewards { Int = 1, Str = 1, Sens = 1 } : new Rewards { Rep = 4 };
            Campus.Apply(r, null, false);
            if (emphasis == "an_bonds") foreach (var p in Pairs.Couples) p.Trust = PairState.Clamp(p.Trust + 5);
            Toasts.Show("Season " + S.Season + " archived in the Class Anthology. " + r.Describe());
            S.Season++;
            S.SeasonStartWeek = S.Week + 1;
            CampusLifePlugin.Log.LogInfo("Anthology: season archived (" + lines.Count + " lines)");
        }
    }
}
