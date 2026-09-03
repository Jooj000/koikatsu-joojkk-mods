using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>School events: exams, sports festival, culture festival, field trips and seasonal days.</summary>
    internal static partial class Calendar
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

        private struct Slot
        {
            public int Week;
            public Cycle.Week Day;
            public string Id;
            public Slot(int week, Cycle.Week day, string id) { Week = week; Day = day; Id = id; }
        }

        /// <summary>
        /// One event a week on changing days, in a 9-week loop, so there are never more than 10 days between two events
        /// (with the old separate intervals the first seasonal day came after 2-3 weeks and the festivals after 6-10).
        /// A new game starts on a Sunday that is still week 0, so week 1 holds the first event (Chocolate Day, day 4).
        /// </summary>
        private static readonly Slot[] Schedule =
        {
            new Slot(1, Cycle.Week.Wednesday, "choco"),
            new Slot(2, Cycle.Week.Saturday, "sports"),
            new Slot(3, Cycle.Week.Friday, "exam"),
            new Slot(4, Cycle.Week.Thursday, "tanabata"),
            new Slot(5, Cycle.Week.Saturday, "culture"),
            new Slot(6, Cycle.Week.Thursday, "halloween"),
            new Slot(7, Cycle.Week.Saturday, "trip"),
            new Slot(8, Cycle.Week.Friday, "exam"),
            new Slot(0, Cycle.Week.Wednesday, "xmas"),
        };

        private const int Loop = 9;

        private static int LoopWeek { get { return ((S.Week % Loop) + Loop) % Loop; } }

        private static string EventOn(Cycle.Week day)
        {
            foreach (var s in Schedule) if (s.Week == LoopWeek && s.Day == day) return s.Id;
            return "";
        }

        public static bool IsExamWeek { get { return Cfg.CalendarEnabled.Value && Schedule.Any(s => s.Id == "exam" && s.Week == LoopWeek); } }

        // ---------------- day / period hooks ----------------

        public static void OnSchoolDay(Cycle.Week day)
        {
            if (!Cfg.CalendarEnabled.Value) return;
            if (!string.IsNullOrEmpty(S.PendingEvent)) ResolveMissed(S.PendingEvent);
            S.PendingEvent = "";

            if (day == Cycle.Week.Monday)
                foreach (var s in Schedule.Where(x => x.Week == LoopWeek && x.Day != Cycle.Week.Monday))
                    Toasts.Show(EventName(s.Id) + " this " + DayNames[(int)s.Day] + "!" + (s.Id == "exam" ? " Study (INT) to do well." : ""));

            S.PendingEvent = EventOn(day);

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
            if (Time.realtimeSinceStartup - _periodStartedAt > 40f) Episodes.Tick();
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
                case "choco": ok = Choco(); break;
                case "tanabata": ok = Tanabata(); break;
                case "halloween": ok = Halloween(); break;
                case "xmas": ok = Xmas(); break;
                default: ok = false; break;
            }
            if (ok) S.PendingEvent = "";
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
            // The next two loops, so each event shows its next date (the exams come twice a loop).
            foreach (var s in Schedule.OrderBy(x => ((x.Week - LoopWeek + Loop) % Loop) * 7 + (int)x.Day))
            {
                var weeks = (s.Week - LoopWeek + Loop) % Loop;
                if (weeks == 0 && (int)s.Day < today) weeks = Loop;
                add(s.Id, weeks, (int)s.Day);
            }
            return list.OrderBy(u => u.Days).ToList();
        }
    }
}
