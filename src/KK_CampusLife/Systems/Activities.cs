using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    internal sealed class ActivityDef
    {
        public string Id;
        public string Name;
        public string Place;            // place id, or "" for anywhere
        public PeriodMask Periods = PeriodMask.Any;
        public int Energy = 1;
        public int MoneyCost;
        public bool OncePerDay;
        public bool IsJob;
        public int MinStr, MinInt, MinSens;
        public string Desc;
        /// <summary>
        /// What it gives, as "INT 3-5, REP 1": INT / STR / SEN / REP / CLUB with a number or a range (both ends
        /// included), EN (energy back) and PAY base+per-point/STAT ("PAY 700+12/INT"). Read by <see cref="Activities.Do"/>
        /// and shown on the menu and next to the map icon (<see cref="Activities.GainText"/>).
        /// </summary>
        public string Gain = "";
    }

    internal sealed class ActivityResult
    {
        public string Text = "";
        public Rewards Gains = new Rewards();
        public string EncounterGirl = "";
    }

    /// <summary>Things to do around the school besides talking: study, train, work, club work, rest.</summary>
    internal static class Activities
    {
        public static readonly List<ActivityDef> All = new List<ActivityDef>
        {
            // classroom
            new ActivityDef { Id = "review_notes", Name = "Review lecture notes", Place = "classroom", Energy = 1, Gain = "INT 3-5", Desc = "Quiet, steady progress." },
            new ActivityDef { Id = "class_duty", Name = "Classroom cleaning duty", Place = "classroom", Periods = PeriodMask.After, Energy = 1, OncePerDay = true, Gain = "REP 2, STR 2", Desc = "Teachers notice." },
            // library
            new ActivityDef { Id = "study", Name = "Study session", Place = "library", Energy = 1, Gain = "INT 4-6", Desc = "Double with a study guide. Exams use INT." },
            new ActivityDef { Id = "romance_novel", Name = "Read a romance novel", Place = "library", Energy = 1, Gain = "SEN 3, INT 2", Desc = "Learn how hearts work." },
            new ActivityDef { Id = "browse_magazines", Name = "Browse girls' magazines", Place = "library", Energy = 1, Desc = "Discover what a classmate likes (better with high INT)." },
            new ActivityDef { Id = "library_job", Name = "Library assistant shift", Place = "library", Periods = PeriodMask.After, Energy = 2, OncePerDay = true, IsJob = true, Gain = "PAY 700+12/INT, INT 2", Desc = "Part-time job. Pay scales with INT." },
            // gym
            new ActivityDef { Id = "workout", Name = "Work out", Place = "gym", Energy = 1, Gain = "STR 4-6", Desc = "Double with a protein shake. STR gives energy and festival wins." },
            new ActivityDef { Id = "basketball", Name = "Pickup basketball", Place = "gym", Energy = 1, Gain = "STR 3, REP 1", Desc = "Sporty girls like it." },
            new ActivityDef { Id = "gym_job", Name = "Set up equipment for the teams", Place = "gym", Periods = PeriodMask.After | PeriodMask.Club, Energy = 2, OncePerDay = true, IsJob = true, MinStr = 15, Gain = "PAY 600+14/STR, STR 2", Desc = "Part-time job. Pay scales with STR." },
            // pool
            new ActivityDef { Id = "swim", Name = "Swim laps", Place = "pool", Energy = 1, Gain = "STR 4", Desc = "Good for stamina." },
            new ActivityDef { Id = "lifeguard", Name = "Lifeguard shift", Place = "pool", Periods = PeriodMask.After | PeriodMask.Club, Energy = 2, OncePerDay = true, IsJob = true, MinStr = 30, Gain = "PAY 1100+10/STR, STR 2", Desc = "Part-time job (STR 30+). Good pay." },
            // field / courtyard / park
            new ActivityDef { Id = "run", Name = "Go for a run", Place = "field", Energy = 1, Gain = "STR 3", Desc = "Fresh air and a clear head." },
            new ActivityDef { Id = "garden", Name = "Help the gardening committee", Place = "courtyard", Energy = 1, OncePerDay = true, Gain = "REP 2, PAY 300", Desc = "The school likes a helping hand." },
            new ActivityDef { Id = "feed_cats", Name = "Feed the stray cats", Place = "courtyard", Energy = 0, OncePerDay = true, Gain = "REP 1", Desc = "Needs cat food. Animal lovers will hear about it." },
            new ActivityDef { Id = "park_walk", Name = "Walk around the park", Place = "park", Energy = 0, OncePerDay = true, Gain = "EN 1", Desc = "Clear your head." },
            // cafeteria
            new ActivityDef { Id = "hearty_lunch", Name = "Eat a hearty lunch", Place = "cafeteria", Periods = PeriodMask.Lunch, Energy = 0, MoneyCost = 500, OncePerDay = true, Gain = "EN 2", Desc = "Katsu curry, large." },
            new ActivityDef { Id = "kitchen_job", Name = "Kitchen helper shift", Place = "cafeteria", Periods = PeriodMask.Lunch | PeriodMask.After, Energy = 2, OncePerDay = true, IsJob = true, Gain = "PAY 700+8/SEN, SEN 2", Desc = "Part-time job. Tips scale with SEN (charm)." },
            // club room
            new ActivityDef { Id = "club_meeting", Name = "Plan club activities", Place = "clubroom", Energy = 1, Gain = "CLUB 3, INT 2", Desc = "Club points raise the game's club level (more with INT)." },
            new ActivityDef { Id = "club_report", Name = "Write the club report", Place = "clubroom", Energy = 1, OncePerDay = true, Gain = "INT 3, CLUB 2, REP 1", Desc = "Official, boring, approved." },
            new ActivityDef { Id = "research_material", Name = "Study 'research material'", Place = "clubroom", Energy = 1, Gain = "SEN 3-5", Desc = "It's for the club. Really." },
            new ActivityDef { Id = "fundraise", Name = "Club fundraising", Place = "clubroom", Periods = PeriodMask.After, Energy = 2, OncePerDay = true, IsJob = true, MinSens = 20, Gain = "PAY 500+10/SEN, REP 1", Desc = "Sell club goods. Pay scales with SEN and Reputation." },
            // rooftop
            new ActivityDef { Id = "nap", Name = "Nap in the sun", Place = "rooftop", Energy = 0, OncePerDay = true, Gain = "EN 2", Desc = "Let the wind do the rest." },
            new ActivityDef { Id = "practice_lines", Name = "Practice smooth lines", Place = "rooftop", Energy = 1, Gain = "SEN 3", Desc = "Compliments, rehearsed to the sky." },
            new ActivityDef { Id = "stargaze", Name = "Watch the sunset", Place = "rooftop", Periods = PeriodMask.After, Energy = 1, Gain = "SEN 2, INT 2", Desc = "Romantic spot." },
            // infirmary
            new ActivityDef { Id = "rest", Name = "Rest in bed", Place = "infirmary", Energy = 0, OncePerDay = true, Gain = "EN 2", Desc = "Clean sheets and silence." },
            new ActivityDef { Id = "nurse_job", Name = "Help the school nurse", Place = "infirmary", Periods = PeriodMask.After | PeriodMask.Club, Energy = 2, OncePerDay = true, IsJob = true, MinInt = 20, Gain = "PAY 600+8/INT, INT 2", Desc = "Part-time job (INT 20+)." },
            // staff room
            new ActivityDef { Id = "tutoring", Name = "Ask for extra tutoring", Place = "staffroom", Energy = 1, MoneyCost = 1500, OncePerDay = true, Gain = "INT 7-10", Desc = "One-on-one with a teacher." },
            new ActivityDef { Id = "teacher_errands", Name = "Run errands for teachers", Place = "staffroom", Energy = 1, OncePerDay = true, Gain = "REP 2, PAY 400", Desc = "The school likes a helping hand." },
        };

        public static ActivityDef Get(string id) { return All.FirstOrDefault(a => a.Id == id); }

        public static int MaxEnergy
        {
            get { return Cfg.BaseEnergy.Value + G.Str / 25; }
        }

        private static string DoneKey(string id) { return "done:" + id; }

        public static bool DoneToday(CampusState s, string id)
        {
            int day;
            return s.Buffs.TryGetValue(DoneKey(id), out day) && day == s.Day;
        }

        /// <summary>Why the activity can't be done right now, or null if it can.</summary>
        public static string Blocker(CampusState s, ActivityDef a)
        {
            if (!G.InRoamingPeriod) return "Only during lunch, club time or after school.";
            if (ActivitySpots.BlocksOthers) return "You're in the middle of something.";
            if (!string.IsNullOrEmpty(a.Place) && Places.CurrentId != a.Place) return "Go to the " + Places.Name(a.Place) + ".";
            if ((a.Periods & G.CurrentMask) == 0) return "Not available in this period.";
            if (Cfg.EnergyEnabled.Value && s.Energy < a.Energy) return "Not enough energy.";
            if (a.MoneyCost > 0 && s.Money < a.MoneyCost) return "Not enough money.";
            if (a.OncePerDay && DoneToday(s, a.Id)) return "Already done today.";
            if (G.Str < a.MinStr) return "Needs STR " + a.MinStr + ".";
            if (G.Int < a.MinInt) return "Needs INT " + a.MinInt + ".";
            if (G.Sens < a.MinSens) return "Needs SEN " + a.MinSens + ".";
            if (a.Id == "feed_cats" && s.Count("cat_food") <= 0) return "You need cat food (school store).";
            if (G.PeriodSecondsLeft < 15f) return "The period is almost over.";
            return null;
        }

        private static int Pay(int baseYen, float perStat, int stat)
        {
            return Mathf.RoundToInt((baseYen + perStat * stat) * Cfg.PayMultiplier.Value);
        }

        private sealed class GainPart
        {
            public string Key, Stat = "";
            public int Min, Max;
            public float Per;
        }

        private static readonly Dictionary<string, List<GainPart>> Parsed = new Dictionary<string, List<GainPart>>();

        private static List<GainPart> Parse(ActivityDef a)
        {
            List<GainPart> list;
            if (Parsed.TryGetValue(a.Id, out list)) return list;
            list = new List<GainPart>();
            foreach (var part in (a.Gain ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
            {
                var bits = part.Split(' ');
                if (bits.Length < 2) continue;
                var x = new GainPart { Key = bits[0] };
                var val = bits[1];
                if (x.Key == "PAY")
                {
                    // "700+12/INT" or "300"
                    var plus = val.Split('+');
                    int.TryParse(plus[0], out x.Min);
                    if (plus.Length > 1)
                    {
                        var per = plus[1].Split('/');
                        float.TryParse(per[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x.Per);
                        x.Stat = per.Length > 1 ? per[1] : "";
                    }
                }
                else
                {
                    var range = val.Split('-');
                    int.TryParse(range[0], out x.Min);
                    x.Max = x.Min;
                    if (range.Length > 1) int.TryParse(range[1], out x.Max);
                }
                list.Add(x);
            }
            Parsed[a.Id] = list;
            return list;
        }

        private static int StatValue(string stat)
        {
            switch (stat)
            {
                case "INT": return G.Int;
                case "STR": return G.Str;
                case "SEN": return G.Sens;
                default: return 0;
            }
        }

        /// <summary>
        /// What it gives and costs, short: "+3-5 INT, -1 energy" or "+1,084 yen, +2 STR, -2 energy" (pay with today's
        /// stats). Empty when there is nothing to show.
        /// </summary>
        public static string GainText(ActivityDef a, bool costs = true)
        {
            var parts = new List<string>();
            foreach (var x in Parse(a))
            {
                if (x.Key == "PAY") { parts.Add("+" + Pay(x.Min, x.Per, StatValue(x.Stat)).ToString("N0") + " yen"); continue; }
                var n = x.Min >= x.Max ? x.Min.ToString() : x.Min + "-" + x.Max;
                var label = x.Key == "EN" ? "energy" : x.Key == "REP" ? "Rep" : x.Key == "CLUB" ? "Club" : x.Key;
                parts.Add("+" + n + " " + label);
            }
            if (a.Id == "fundraise") parts.Add("+10 yen per Rep");
            if (a.Id == "browse_magazines") parts.Add("a classmate's like");
            if (costs && a.MoneyCost > 0) parts.Add("-" + a.MoneyCost.ToString("N0") + " yen");
            if (costs && Cfg.EnergyEnabled.Value && a.Energy > 0) parts.Add("-" + a.Energy + " energy");
            return string.Join(", ", parts.ToArray());
        }

        public static ActivityResult Do(CampusState s, ActivityDef a)
        {
            var r = new ActivityResult();
            var g = r.Gains;
            foreach (var x in Parse(a))
            {
                var v = x.Min >= x.Max ? x.Min : Random.Range(x.Min, x.Max + 1);
                switch (x.Key)
                {
                    case "INT": g.Int += v; break;
                    case "STR": g.Str += v; break;
                    case "SEN": g.Sens += v; break;
                    case "REP": g.Rep += v; break;
                    case "CLUB": g.Club += v; break;
                    case "EN": s.Energy = Math.Min(MaxEnergy, s.Energy + v); break;
                    case "PAY": g.Money += Pay(x.Min, x.Per, StatValue(x.Stat)); break;
                }
            }
            switch (a.Id)
            {
                case "review_notes":
                    r.Text = "You go over today's lessons until they finally make sense.";
                    break;
                case "class_duty":
                    r.Text = "You sweep the floor and wipe the blackboard. The homeroom teacher gives you a nod.";
                    break;
                case "study":
                    if (s.Count("study_guide") > 0) { s.AddItem("study_guide", -1); g.Int *= 2; r.Text = "With the study guide, everything clicks. "; }
                    r.Text += "You fill two pages with notes in the quiet library.";
                    s.Stat("study");
                    break;
                case "romance_novel":
                    r.Text = "A dramatic confession scene... you take mental notes.";
                    break;
                case "browse_magazines":
                    r.Text = Discover(s);
                    break;
                case "library_job":
                    r.Text = "You shelve returned books and help first-years find their reports.";
                    s.Stat("jobs");
                    break;
                case "workout":
                    if (s.Count("protein") > 0) { s.AddItem("protein", -1); g.Str *= 2; r.Text = "The protein shake kicks in. "; }
                    r.Text += "Push-ups, squats, the bench... you'll feel this tomorrow.";
                    s.Stat("workout");
                    break;
                case "basketball":
                    r.Text = Random.value < 0.3f + G.Str / 250f ? "You sink the winning shot. A few girls clap from the stage." : "You lose, but it was fun.";
                    s.Stat("workout");
                    break;
                case "gym_job":
                    r.Text = "You haul hurdles and mats for the athletic clubs.";
                    s.Stat("jobs");
                    break;
                case "swim":
                    r.Text = "Twenty laps. The water is cold and perfect.";
                    s.Stat("workout");
                    break;
                case "lifeguard":
                    r.Text = "You watch over the pool from the high chair. Mostly uneventful. Mostly.";
                    s.Stat("jobs");
                    break;
                case "run":
                    r.Text = "Three laps around the field.";
                    s.Stat("workout");
                    break;
                case "garden":
                    r.Text = "You water flower beds and pull weeds with the gardening committee.";
                    break;
                case "feed_cats":
                    s.AddItem("cat_food", -1);
                    s.Stat("cats");
                    r.Text = "Three stray cats show up. One lets you pet it.";
                    break;
                case "park_walk":
                    r.Text = "A slow walk under the trees. You feel rested.";
                    break;
                case "hearty_lunch":
                    s.Money -= a.MoneyCost;
                    r.Text = "Katsu curry, large. Energy restored.";
                    break;
                case "kitchen_job":
                    r.Text = "You serve trays with a smile. The tips are better when you smile.";
                    s.Stat("jobs");
                    break;
                case "club_meeting":
                    g.Club += G.Int / 25;
                    r.Text = "You plan next week's Koikatsu Club 'activities' on the whiteboard.";
                    break;
                case "club_report":
                    r.Text = "You write a very official, very boring club report. The student council approves.";
                    break;
                case "research_material":
                    r.Text = "You study the club's 'reference library'. Purely academic.";
                    break;
                case "fundraise":
                    g.Money += Mathf.RoundToInt(s.Reputation * 10 * Cfg.PayMultiplier.Value);
                    r.Text = "You sell club pins and photo prints at the entrance.";
                    s.Stat("jobs");
                    break;
                case "nap":
                    r.Text = "You doze off to the sound of the wind.";
                    break;
                case "practice_lines":
                    r.Text = "You rehearse compliments to the sky. A crow judges you.";
                    break;
                case "stargaze":
                    r.Text = "The city turns orange, then blue.";
                    break;
                case "rest":
                    r.Text = "Clean sheets and silence. Energy restored.";
                    break;
                case "nurse_job":
                    r.Text = "You sort first-aid kits and file health records.";
                    s.Stat("jobs");
                    break;
                case "tutoring":
                    s.Money -= a.MoneyCost;
                    r.Text = "A teacher walks you through last month's material one-on-one.";
                    s.Stat("study");
                    break;
                case "teacher_errands":
                    r.Text = "Copies, coffee, a box of handouts. The staff room likes you.";
                    break;
            }
            return r;
        }

        /// <summary>Reveal one like of a girl you know (INT raises the chance of a useful find).</summary>
        private static string Discover(CampusState s)
        {
            var known = G.Heroines.Where(GirlInfo.IsStudent).Where(h => h.relation >= 0 || s.Girl(GirlInfo.Key(h)).Talks > 0).ToList();
            if (known.Count == 0) known = G.Heroines.Where(GirlInfo.IsStudent).ToList();
            if (known.Count == 0) return "Nothing interesting today.";
            var chance = 0.45f + G.Int / 200f;
            for (var tries = 0; tries < 6; tries++)
            {
                var h = known[Random.Range(0, known.Count)];
                var gs = s.Girl(GirlInfo.Key(h));
                var likes = GirlInfo.LikesOf(h);
                var unknown = likes.Where(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) == 0).ToList();
                if (unknown.Count == 0) continue;
                if (Random.value > chance) return "You read about trends, but nothing rings a bell about anyone you know.";
                var like = unknown[Random.Range(0, unknown.Count)];
                gs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, like);
                var text = "An article reminds you of something " + GirlInfo.FirstName(h) + " said: she likes " + GirlInfo.LikeLabel(like) + ".";
                s.Log(text);
                return text;
            }
            return "You already know what everyone around you likes. Impressive.";
        }

        /// <summary>Sometimes a girl who is in the same place joins in.</summary>
        public static SaveData.Heroine Encounter(ActivityDef a)
        {
            if (string.IsNullOrEmpty(a.Place) || a.IsJob) return null;
            var here = G.PlayerMapNo;
            var near = G.Heroines.Where(h => GirlInfo.IsSpawned(h) && GirlInfo.CurrentMap(h) == here).ToList();
            if (near.Count == 0 || Random.value > 0.4f) return null;
            return near[Random.Range(0, near.Count)];
        }
    }
}
