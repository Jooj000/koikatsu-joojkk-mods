using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>School events: exams, sports festival, culture festival, field trips and seasonal days.</summary>
    internal static class Calendar
    {
        private static CampusState S { get { return Campus.S; } }
        private static float _periodStartedAt;
        private static float _lastTry;

        public static readonly string[] EventIds = { "exam", "sports", "culture", "trip" };

        public static string EventName(string id)
        {
            switch (id)
            {
                case "exam": return "Exams";
                case "sports": return "Sports Festival";
                case "culture": return "Culture Festival";
                case "trip": return "Field Trip";
                case "choco": return "Chocolate Day";
                case "tanabata": return "Tanabata";
                case "halloween": return "Halloween";
                case "xmas": return "Christmas Eve";
                default: return id;
            }
        }

        private static int Interval(string id)
        {
            switch (id)
            {
                case "exam": return Cfg.ExamInterval.Value;
                case "sports": return Cfg.SportsInterval.Value;
                case "culture": return Cfg.CultureInterval.Value;
                default: return Cfg.TripInterval.Value;
            }
        }

        private static int LastWeek(string id)
        {
            int w;
            return S.EventLastWeek.TryGetValue(id, out w) ? w : 0;
        }

        /// <summary>Weeks until the event is due (0 = this week).</summary>
        public static int WeeksUntil(string id)
        {
            return Math.Max(0, LastWeek(id) + Interval(id) - S.Week);
        }

        public static bool IsExamWeek { get { return Cfg.CalendarEnabled.Value && WeeksUntil("exam") == 0; } }

        /// <summary>Which Saturday event happens this week (only one per Saturday).</summary>
        public static string SaturdayEvent
        {
            get
            {
                foreach (var id in new[] { "sports", "culture", "trip" })
                    if (WeeksUntil(id) == 0) return id;
                return "";
            }
        }

        private static string SeasonalFor(Cycle.Week day)
        {
            // A 12-week "season" loop so the special days come around regularly.
            var w = S.Week % 12;
            if (w == 2 && day == Cycle.Week.Wednesday) return "choco";
            if (w == 5 && day == Cycle.Week.Thursday) return "tanabata";
            if (w == 8 && day == Cycle.Week.Tuesday) return "halloween";
            if (w == 11 && day == Cycle.Week.Wednesday) return "xmas";
            return "";
        }

        // ---------------- day / period hooks ----------------

        public static void OnSchoolDay(Cycle.Week day)
        {
            if (!Cfg.CalendarEnabled.Value) return;
            if (!string.IsNullOrEmpty(S.PendingEvent)) ResolveMissed(S.PendingEvent);
            S.PendingEvent = "";

            if (day == Cycle.Week.Monday)
            {
                if (IsExamWeek) Toasts.Show("Exams this Friday! Study (INT) to do well.");
                var sat = SaturdayEvent;
                if (sat != "") Toasts.Show(EventName(sat) + " this Saturday!");
            }

            if (day == Cycle.Week.Friday && IsExamWeek) S.PendingEvent = "exam";
            else if (day == Cycle.Week.Saturday && SaturdayEvent != "") S.PendingEvent = SaturdayEvent;
            else S.PendingEvent = SeasonalFor(day);

            if (S.PendingEvent != "") Toasts.Show("Today: " + EventName(S.PendingEvent) + ".");
        }

        public static void OnPeriodStart(Cycle.Type period)
        {
            _periodStartedAt = Time.realtimeSinceStartup;
            if (!Cfg.HappeningsEnabled.Value || !string.IsNullOrEmpty(S.PendingHappening)) return;
            if (S.LastHappeningDay == S.Day && S.LastHappeningPeriod == (int)period) return;
            S.LastHappeningDay = S.Day;
            S.LastHappeningPeriod = (int)period;
            if (Random.value < Cfg.HappeningChance.Value) S.PendingHappening = Happenings.Roll();
        }

        /// <summary>Called from Update: starts pending events when the player is free.</summary>
        public static void Tick()
        {
            if (!Campus.On || Scenes.Running) return;
            if (Time.realtimeSinceStartup - _lastTry < 1f) return;
            _lastTry = Time.realtimeSinceStartup;
            Campus.FixWeekNumbering();
            Board.EnsureToday();
            Board.CheckErrands();
            Rendezvous.Tick();
            Social.DailySpread();
            Social.Tick();
            if (Time.realtimeSinceStartup - _periodStartedAt > 30f) Duo.Tick();
            // Give the player a few seconds after each period starts.
            if (Time.realtimeSinceStartup - _periodStartedAt < 6f) return;
            if (!Scenes.CanPlayOnMap) return;

            if (!string.IsNullOrEmpty(S.PendingEvent) && Cfg.AutoPlaySchoolEvents.Value) { PlayEvent(); return; }
            if (!string.IsNullOrEmpty(S.PendingHappening) && Cfg.AutoPlayHappenings.Value
                && Time.realtimeSinceStartup - _periodStartedAt > 20f) PlayHappening();
        }

        public static bool PlayEvent()
        {
            var id = S.PendingEvent;
            if (string.IsNullOrEmpty(id) || !Scenes.CanPlayOnMap) return false;
            bool ok;
            switch (id)
            {
                case "exam": ok = Exam(); break;
                case "sports": ok = Sports(); break;
                case "culture": ok = Culture(); break;
                case "trip": ok = Trip(); break;
                default: ok = Seasonal(id); break;
            }
            if (ok)
            {
                S.PendingEvent = "";
                if (Array.IndexOf(EventIds, id) >= 0) S.EventLastWeek[id] = S.Week;
            }
            return ok;
        }

        public static bool PlayHappening()
        {
            var id = S.PendingHappening;
            if (string.IsNullOrEmpty(id) || !Scenes.CanPlayOnMap) return false;
            if (Happenings.Play(id)) { S.PendingHappening = ""; return true; }
            S.PendingHappening = ""; // could not build (no suitable girl): drop it
            return false;
        }

        private static void ResolveMissed(string id)
        {
            if (id == "exam")
            {
                var score = ExamScore(false);
                RecordExam(score);
                S.Log("You skipped the exam room... the teacher made you take it after class: " + score + " points.");
            }
            else S.Log("You missed the " + EventName(id) + ".");
            if (Array.IndexOf(EventIds, id) >= 0) S.EventLastWeek[id] = S.Week;
        }

        // ---------------- helpers ----------------

        /// <summary>Girls ranked for "who do you spend time with": girlfriends, spotlight and favor first.</summary>
        public static List<SaveData.Heroine> TopGirls(int n)
        {
            var spot = S.Spotlight;
            return Board.Students
                .OrderByDescending(h => (h.isGirlfriend ? 200 : 0) + (spot.Contains(GirlInfo.Key(h)) ? 50 : 0) + h.favor + Random.Range(0, 30))
                .Take(n).ToList();
        }

        private static SaveData.Heroine AnyGirl()
        {
            var list = Board.Students;
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        private static void Pay(Rewards r, SaveData.Heroine h, string why)
        {
            var applied = Campus.Apply(r, h, false);
            var msg = why + " (" + applied.Describe() + ")";
            S.Log(msg);
            Toasts.Show(msg);
        }

        // ---------------- exams ----------------

        private static int StudyCount()
        {
            Func<Dictionary<string, int>, string, int> g = (d, k) => { int v; return d.TryGetValue(k, out v) ? v : 0; };
            var n = 0;
            foreach (var d in new[] { S.WeekStats, S.LastWeekStats })
                n += g(d, "act:study") + g(d, "act:review_notes") + 2 * g(d, "act:tutoring") + g(d, "req:Study");
            return n;
        }

        private static int ExamScore(bool cram)
        {
            var score = 30 + G.Int * 0.6f + 3 * Mathf.Min(StudyCount(), 8) + Random.Range(-8, 9) + (cram ? 5 : 0);
            return Mathf.Clamp(Mathf.RoundToInt(score), 0, 100);
        }

        private static void RecordExam(int score)
        {
            S.ExamScores.Add(score);
            if (S.ExamScores.Count > 20) S.ExamScores.RemoveAt(0);
            Rewards r;
            string verdict;
            if (score >= 85) { r = new Rewards { Money = 3000, Rep = 5, Int = 2 }; verdict = "Top of the class! Your parents send a reward."; }
            else if (score >= 65) { r = new Rewards { Money = 1000, Rep = 2, Int = 1 }; verdict = "A solid result."; }
            else if (score >= 40) { r = new Rewards { Int = 1 }; verdict = "Passing. Nothing to brag about."; }
            else { r = new Rewards { Rep = -3 }; verdict = "Failed. The teachers are worried."; }
            Pay(r, null, "Exam score " + score + "/100. " + verdict);
        }

        private static bool Exam()
        {
            var b = new SceneBuilder(null, false);
            var girl = AnyGirl();
            b.Narr("Exam day. The classroom is silent except for pencils scratching on paper.");
            if (girl != null)
                b.Say(girl, Lines.T(GirlInfo.ToneOf(girl), "Good luck... let's both do our best.", "Let's crush this test!", "Don't distract me.", "If you fail, I'm laughing at you~"));
            var opts = new List<string> { "Stay calm and focus.", "calm", "Cram the formulas at the last minute.", "cram" };
            var peek = G.Sens >= 60;
            if (peek) { opts.Add("Peek at the answer key on the teacher's desk. [SEN]"); opts.Add("peek"); }
            b.Choice(opts.ToArray());
            b.Branch("calm"); b.Narr("You take a deep breath and start writing."); b.End();
            b.Branch("cram"); b.Narr("You mutter formulas until the last second. Your head is spinning, but they're in there."); b.End();
            b.Branch("peek"); b.Narr("While the teacher looks away, you lean over... "); b.End();
            return Scenes.PlayOnMap(b, choice =>
            {
                var score = ExamScore(choice == "cram");
                if (choice == "cram") Campus.SpendEnergy(1);
                if (choice == "peek")
                {
                    int roll;
                    if (Campus.Check(G.Sens, 85, out roll)) { score = Mathf.Min(100, score + 20); Toasts.Show("Nobody noticed. Smooth."); }
                    else
                    {
                        score = Mathf.Max(0, score - 25);
                        S.Reputation = Mathf.Clamp(S.Reputation - 10, 0, 100);
                        Toasts.Show("Caught! The teacher confiscates your test. (-10 reputation)");
                    }
                }
                RecordExam(score);
            });
        }

        // ---------------- sports festival ----------------

        private static bool Sports()
        {
            var partner = TopGirls(1).FirstOrDefault();
            var b = new SceneBuilder(null, false);
            b.Narr("The Sports Festival! Flags everywhere, the whole school is on the field.");
            if (partner != null)
                b.Say(partner, Lines.T(GirlInfo.ToneOf(partner), "Which event are you entering? I'll cheer for you!", "Let's WIN this! Which event are you in?", "Try not to embarrass our class.", "I'll be watching you~ Don't trip."));
            var opts = new List<string> { "Relay race (STR)", "relay", "Tug of war (STR, team)", "tug", "Cheer squad (SEN)", "cheer" };
            if (partner != null) { opts.Add("Three-legged race with " + GirlInfo.FirstName(partner)); opts.Add("three"); }
            b.Choice(opts.ToArray());
            b.Branch("relay"); b.Narr("You grab the baton and run like your life depends on it..."); b.End();
            b.Branch("tug"); b.Narr("Everyone grabs the rope. Pull!"); b.End();
            b.Branch("cheer"); b.Narr("You lead the cheering section with all your heart."); b.End();
            b.Branch("three"); b.Narr("Your ankle is tied to hers. You'll have to move together."); b.End();
            return Scenes.PlayOnMap(b, choice =>
            {
                int roll;
                switch (choice)
                {
                    case "tug":
                        if (Campus.Check(G.Str / 2 + 25, 55, out roll)) Pay(new Rewards { Rep = 4, Str = 1, Club = 2 }, null, "Your team won the tug of war!");
                        else Pay(new Rewards { Rep = 1, Str = 1 }, null, "Your team lost the tug of war.");
                        break;
                    case "cheer":
                        if (Campus.Check(G.Sens, 60, out roll)) Pay(new Rewards { Rep = 5, Sens = 1 }, null, "Your cheering became the talk of the school!");
                        else Pay(new Rewards { Rep = 1 }, null, "Your cheering was... enthusiastic.");
                        foreach (var g in TopGirls(3)) G.AddFavor(g, 2, 0);
                        break;
                    case "three":
                        if (partner == null) goto default;
                        var together = (G.Str + partner.favor) / 2;
                        if (Campus.Check(together, 70, out roll)) Pay(new Rewards { Favor = 10, Lewd = 3, Rep = 3 }, partner, "You and " + GirlInfo.FirstName(partner) + " won the three-legged race!");
                        else Pay(new Rewards { Favor = 6, Lewd = 2 }, partner, "You and " + GirlInfo.FirstName(partner) + " fell over laughing");
                        break;
                    default:
                        if (Campus.Check(G.Str, 65, out roll)) Pay(new Rewards { Rep = 6, Str = 2, Club = 2 }, null, "You won the relay race!");
                        else Pay(new Rewards { Rep = 1, Str = 1 }, null, "You finished the relay race in the middle of the pack.");
                        break;
                }
                foreach (var h in Board.Students.Where(x => GirlInfo.InSportsClub(x) || GirlInfo.Likes(x, "exercise")))
                    if (G.Str >= 50) G.AddFavor(h, 2, 0); // sporty girls noticed you
            });
        }

        // ---------------- culture festival ----------------

        private static bool Culture()
        {
            var tops = TopGirls(3);
            var b = new SceneBuilder(null, false);
            b.Narr("The Culture Festival! Your class has to decide what to run.");
            b.Choice("Maid cafe (SEN)", "maid", "Haunted house (STR)", "haunt", "Science exhibit (INT)", "science");
            foreach (var t in new[] { "maid", "haunt", "science" })
            {
                b.Branch(t);
                b.Narr(t == "maid" ? "Your class runs a maid cafe. You're in charge of bringing in customers." :
                    t == "haunt" ? "Your class builds a haunted house. You haul props and play a monster." :
                    "Your class sets up a science exhibit. You explain the experiments to visitors.");
                b.Narr("In the afternoon you have some free time to walk around the festival.");
                var opts = new List<string>();
                foreach (var g in tops) { opts.Add("Walk around with " + GirlInfo.FirstName(g)); opts.Add(t + "_g" + tops.IndexOf(g)); }
                opts.Add("Help backstage"); opts.Add(t + "_help");
                b.Choice(opts.ToArray());
                for (var i = 0; i < tops.Count; i++)
                {
                    b.Branch(t + "_g" + i);
                    b.Say(tops[i], Lines.T(GirlInfo.ToneOf(tops[i]), "You want to walk with me? ...I'd love to.", "Yes! Let's hit every food stand!", "...Fine. You can escort me.", "A festival date? Bold~ I accept."));
                    b.End();
                }
                b.Branch(t + "_help");
                b.Narr("You spend the afternoon helping backstage. The teachers are impressed.");
                b.End();
            }
            return Scenes.PlayOnMap(b, choice =>
            {
                var parts = (choice ?? "").Split('_');
                var booth = parts[0];
                int stat = booth == "maid" ? G.Sens : booth == "haunt" ? G.Str : G.Int;
                int roll;
                if (Campus.Check(stat, 65, out roll)) Pay(new Rewards { Money = 2500, Rep = 5, Club = 3 }, null, "Your class booth was a hit");
                else Pay(new Rewards { Money = 800, Rep = 2, Club = 1 }, null, "Your class booth did okay");
                if (parts.Length > 1 && parts[1].StartsWith("g"))
                {
                    int idx;
                    try { idx = int.Parse(parts[1].Substring(1)); } catch { idx = -1; }
                    if (idx >= 0 && idx < tops.Count) Pay(new Rewards { Favor = 10, Lewd = 4 }, tops[idx], "Festival date with " + GirlInfo.FirstName(tops[idx]));
                }
                else if (parts.Length > 1) Pay(new Rewards { Rep = 3, Int = 1 }, null, "You helped backstage");
            });
        }

        // ---------------- field trip ----------------

        private static bool Trip()
        {
            var dests = new[] { "Kyoto", "the beach", "a hot spring inn" };
            var dest = dests[Random.Range(0, dests.Length)];
            var tops = TopGirls(3);
            var b = new SceneBuilder(null, false);
            b.Narr("Field trip day! The whole class takes the bus to " + dest + ".");
            if (dest == "the beach") b.Narr("Swimsuits, sunscreen and sand everywhere. This is a dangerous place for your heart.");
            if (dest == "Kyoto") b.Narr("Temples, old streets, and way too many souvenir shops.");
            if (dest == "a hot spring inn") b.Narr("Steam rises from the outdoor baths. The girls' bath is right behind that bamboo fence...");
            var opts = new List<string>();
            for (var i = 0; i < tops.Count; i++) { opts.Add("Spend the free time with " + GirlInfo.FirstName(tops[i])); opts.Add("g" + i); }
            opts.Add("Explore on your own"); opts.Add("solo");
            if (dest == "a hot spring inn" && G.Sens >= 50) { opts.Add("Peek over the bamboo fence [SEN]"); opts.Add("peek"); }
            b.Choice(opts.ToArray());
            for (var i = 0; i < tops.Count; i++)
            {
                b.Branch("g" + i);
                b.Say(tops[i], Lines.T(GirlInfo.ToneOf(tops[i]), "Just the two of us? I was hoping you'd ask...", "Yay! Let's go everywhere!", "I suppose you can come along.", "Sneaking away with me? I like it~"));
                b.End();
            }
            b.Branch("solo"); b.Narr("You wander around on your own and pick up some souvenirs."); b.End();
            b.Branch("peek"); b.Narr("You climb the rocks behind the fence, holding your breath..."); b.End();
            return Scenes.PlayOnMap(b, choice =>
            {
                if (choice == "peek")
                {
                    int roll;
                    if (Campus.Check(G.Sens, 80, out roll))
                    {
                        Pay(new Rewards { Sens = 3 }, null, "You saw... paradise. Nobody noticed");
                        foreach (var g in tops) G.AddFavor(g, 0, 3);
                    }
                    else
                    {
                        var g = tops.FirstOrDefault();
                        if (g != null) G.AddFavor(g, -5, 2);
                        Pay(new Rewards { Rep = -6, Sens = 1 }, null, "Caught peeking! A bucket flies at your head");
                    }
                    return;
                }
                if (choice == "solo")
                {
                    S.AddItem("omamori", 1);
                    Pay(new Rewards { Int = 1 }, null, "You bought a lucky charm (omamori) as a souvenir");
                    return;
                }
                int idx;
                try { idx = int.Parse((choice ?? "g0").Substring(1)); } catch { idx = 0; }
                if (idx >= 0 && idx < tops.Count) Pay(new Rewards { Favor = 12, Lewd = dest == "Kyoto" ? 2 : 6 }, tops[idx], "A trip to remember with " + GirlInfo.FirstName(tops[idx]));
            });
        }

        // ---------------- seasonal days ----------------

        private static bool Seasonal(string id)
        {
            var b = new SceneBuilder(null, false);
            switch (id)
            {
                case "choco":
                {
                    var givers = Board.Students.Where(h => h.isGirlfriend || h.favor >= 60).OrderByDescending(h => h.favor).Take(6).ToList();
                    b.Narr("Chocolate Day. The whole school smells of cocoa, and every boy is pretending not to care.");
                    if (givers.Count == 0) b.Narr("...Nobody gave you anything this year. Next year, for sure.");
                    foreach (var g in givers.Take(3))
                        b.Say(g, g.isGirlfriend
                            ? Lines.T(GirlInfo.ToneOf(g), "I made these myself... for you. Only for you.", "Homemade! Eat them all, okay?!", "Handmade. Don't make a fuss.", "Handmade, with love~ And maybe a little something extra.")
                            : Lines.T(GirlInfo.ToneOf(g), "Um, these are... thank-you chocolates! Just thanks!", "Here, friendship chocolate! Don't get the wrong idea!", "Obligation chocolate. Obviously.", "Don't get your hopes up. ...Unless you want to."));
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        foreach (var g in givers) { S.AddItem("chocolate", 1); G.AddFavor(g, 2, 0); }
                        if (givers.Count > 0) Pay(new Rewards { Rep = Math.Min(5, givers.Count) }, null, "You received " + givers.Count + " chocolate(s)");
                    });
                }
                case "tanabata":
                    b.Narr("Tanabata. Bamboo branches in the courtyard are covered in paper wishes. You write yours...");
                    b.Choice("'I want to get smarter.'", "int", "'I want to get stronger.'", "str", "'I want to be more charming.'", "sen", "'I want to be loved.'", "love");
                    foreach (var t in new[] { "int", "str", "sen", "love" }) { b.Branch(t); b.Narr("You hang your wish on the highest branch you can reach."); b.End(); }
                    return Scenes.PlayOnMap(b, c =>
                    {
                        switch (c)
                        {
                            case "int": Pay(new Rewards { Int = 3 }, null, "Your wish was heard"); break;
                            case "str": Pay(new Rewards { Str = 3 }, null, "Your wish was heard"); break;
                            case "sen": Pay(new Rewards { Sens = 3 }, null, "Your wish was heard"); break;
                            default: foreach (var g in TopGirls(5)) G.AddFavor(g, 3, 0); Toasts.Show("Somehow, everyone seems a bit warmer today."); break;
                        }
                    });
                case "halloween":
                {
                    var girls = TopGirls(3);
                    var sweets = new[] { "chocolate", "cookies", "daifuku" };
                    var have = sweets.Sum(x => S.Count(x));
                    b.Narr("Halloween! Some girls came to school with cat ears and little capes.");
                    foreach (var g in girls) b.Say(g, Lines.T(GirlInfo.ToneOf(g), "T-trick or treat...?", "TRICK OR TREAT!", "Trick or treat. Choose wisely.", "Trick or treat~ I'm hoping for the trick."));
                    if (have >= girls.Count) b.Narr("You hand out sweets from your bag. Everyone's happy.");
                    else b.Narr("You don't have enough sweets... Time for some tricks.");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        var left = girls.Count;
                        foreach (var x in sweets) while (left > 0 && S.Count(x) > 0) { S.AddItem(x, -1); left--; }
                        for (var i = 0; i < girls.Count; i++)
                        {
                            if (i < girls.Count - left) G.AddFavor(girls[i], 5, 0);
                            else G.AddFavor(girls[i], 1, 4); // a playful trick
                        }
                        Toasts.Show("Happy Halloween!");
                    });
                }
                case "xmas":
                {
                    var gfs = Board.Students.Where(h => h.isGirlfriend).ToList();
                    b.Narr("Christmas Eve. Lights on every street, couples everywhere.");
                    if (gfs.Count == 0) b.Narr("You spend it with the Koikatsu Club, eating cake. Next year will be different.");
                    foreach (var g in gfs.Take(3))
                        b.Say(g, Lines.T(GirlInfo.ToneOf(g), "Merry Christmas... I'm so happy to spend it with you.", "Merry Christmas! Let's eat ALL the cake!", "Merry Christmas. ...Stay with me tonight.", "Merry Christmas~ Your present is... me. Kidding. Maybe."));
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        foreach (var g in gfs) G.AddFavor(g, 6, 5);
                        if (gfs.Count == 0) Pay(new Rewards { Club = 2, Rep = 1 }, null, "Club Christmas party");
                        else Toasts.Show("A Christmas to remember.");
                    });
                }
            }
            return false;
        }

        // ---------------- planner info ----------------

        public sealed class Upcoming
        {
            public int Days;      // 0 = today
            public string Id;
            public string Name;
            public string When;
        }

        private static readonly string[] DayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        private static string WhenLabel(int days, int weekday)
        {
            if (days <= 0) return "Today";
            if (days == 1) return "Tomorrow";
            if (days < 7) return DayNames[weekday] + " (in " + days + " days)";
            return DayNames[weekday] + ", in " + days + " days";
        }

        /// <summary>Next school events, nearest first.</summary>
        public static List<Upcoming> UpcomingEvents()
        {
            var list = new List<Upcoming>();
            if (!Cfg.CalendarEnabled.Value) return list;
            var today = G.Weekday.HasValue ? (int)G.Weekday.Value : 0; // Monday = 0 ... Sunday = 6
            Action<string, int, int> add = (id, weeks, weekday) =>
            {
                var days = weeks * 7 + weekday - today;
                if (days < 0) days += 7;
                if (list.Any(u => u.Id == id)) return;
                list.Add(new Upcoming { Days = days, Id = id, Name = EventName(id), When = WhenLabel(days, weekday) });
            };
            if (!string.IsNullOrEmpty(S.PendingEvent)) add(S.PendingEvent, 0, today);
            foreach (var id in EventIds) add(id, WeeksUntil(id), id == "exam" ? 4 : 5);
            var seasonWeek = S.Week % 12;
            foreach (var kv in new[] { new[] { 2, 2 }, new[] { 5, 3 }, new[] { 8, 1 }, new[] { 11, 2 } })
            {
                var id = kv[0] == 2 ? "choco" : kv[0] == 5 ? "tanabata" : kv[0] == 8 ? "halloween" : "xmas";
                var weeks = (kv[0] - seasonWeek + 12) % 12;
                if (weeks == 0 && kv[1] < today) weeks = 12;
                add(id, weeks, kv[1]);
            }
            return list.OrderBy(u => u.Days).ToList();
        }
    }
}
