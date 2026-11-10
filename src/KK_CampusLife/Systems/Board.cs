using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Communication;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>The club board: daily requests that tell the player who needs him today.</summary>
    internal static class Board
    {
        public static readonly string[][] Errands =
        {
            new[] { "library", "Return her overdue library books." },
            new[] { "cafeteria", "Buy her a melon bread before they sell out." },
            new[] { "staffroom", "Turn in her assignments at the faculty room." },
            new[] { "clubroom", "Fetch the notebook she forgot in the club room." },
            new[] { "gym", "Find the towel she left in the gym." },
            new[] { "pool", "Get her goggles back from the pool." },
            new[] { "infirmary", "Pick up her medicine at the infirmary." },
            new[] { "classroom", "Copy today's notes for her." },
        };

        private static CampusState S { get { return Campus.S; } }

        public static IEnumerable<Request> Active { get { return S.Requests.Where(r => r.State == RequestState.Active); } }

        public static IEnumerable<Request> ActiveFor(string key) { return Active.Where(r => r.Girl == key); }

        /// <summary>Problems a girl has that the player has not heard about yet.</summary>
        public static IEnumerable<Request> HiddenFor(string key)
        {
            return S.Requests.Where(r => r.State == RequestState.Hidden && r.Girl == key && r.DeadlineDay >= S.Day);
        }

        private static IEnumerable<Request> OpenFor(string key)
        {
            return S.Requests.Where(r => (r.State == RequestState.Active || r.State == RequestState.Hidden) && r.Girl == key);
        }

        /// <summary>The player learns about a hidden request: it goes on the board.</summary>
        public static void Reveal(Request r, string learnedFrom)
        {
            if (r == null || r.State != RequestState.Hidden) return;
            r.State = RequestState.Active;
            r.LearnedFrom = learnedFrom ?? "";
            var name = GirlInfo.FirstName(r.Girl);
            var who = string.IsNullOrEmpty(learnedFrom) || learnedFrom == "her" ? name : GirlInfo.FirstName(learnedFrom);
            if (who != name) r.Text = who + " told you: " + r.Text;
            Toasts.Show("New request: " + r.Title + ".");
            S.Log("Learned: " + r.Title + (who != name ? " (from " + who + ")" : "") + ".");
        }

        /// <summary>The problem in her own words (for "Let her talk").</summary>
        public static string ProblemFirst(Request r, Tone t)
        {
            // Her first words about it, from board.dlg by the kind and her tone: the request's own text and place are
            // slots.
            if (r.Kind == RequestKind.Intimate) return r.Text;
            return Conversation.BoardSay(GirlInfo.Find(r.Girl), r.Kind.ToString().ToLowerInvariant(), r.Place, r.Text, r.Source) ?? r.Text ?? "...";
        }

        /// <summary>The problem as a friend tells it.</summary>
        public static string ProblemThird(Request r)
        {
            var n = GirlInfo.FirstName(r.Girl);
            switch (r.Kind)
            {
                case RequestKind.Lunch: return n + " forgot her lunch.";
                case RequestKind.Study: return n + " is really behind in class.";
                case RequestKind.Exercise: return n + " wants someone to train with.";
                case RequestKind.Club: return n + " wants to do something with the club.";
                case RequestKind.WalkHome: return n + " doesn't want to walk home alone.";
                case RequestKind.Gift: return n + " is having a rough day.";
                case RequestKind.MeetAt: return n + " wants to see you at the " + Places.Name(r.Place) + ".";
                case RequestKind.Errand: return n + " needs a favor.";
                default: return n + " wants to talk to you.";
            }
        }

        public static List<SaveData.Heroine> Students
        {
            get { return G.Heroines.Where(GirlInfo.IsRegular).ToList(); } // story heroines have no everyday talks
        }

        // ---------------- generation ----------------

        public static int TargetCount
        {
            get
            {
                var cast = Students.Count;
                if (cast == 0) return 0;
                var n = Cfg.MinRequests.Value + cast / Math.Max(1, Cfg.CastPerExtraRequest.Value);
                if (Cfg.AppealAddsRequests.Value)
                {
                    if (G.Sens >= 50) n++;
                    if (G.Sens >= 90) n++;
                }
                if (S.Reputation >= 60) n++;
                return Mathf.Clamp(n, Cfg.MinRequests.Value, Math.Max(Cfg.MinRequests.Value, Cfg.MaxRequests.Value));
            }
        }

        /// <summary>
        /// The girls who know you and have not heard from you, by the signal the moods and the weekly report already use.
        /// One with her own talk request is left out.
        /// </summary>
        private static List<SaveData.Heroine> Missing()
        {
            return Students.Where(h =>
            {
                var gs = S.Girl(GirlInfo.Key(h));
                return (h.relation >= 1 || h.isGirlfriend) && (gs.Attention < 25 || gs.Mood == Mood.Lonely) && gs.LastTalkDay != S.Day && (!Truth.Known || Truth.AtSchool(h))
                       && !OpenFor(gs.Key).Any(r => r.Kind == RequestKind.Talk && r.Source != "miss");
            }).OrderBy(h => S.Girl(GirlInfo.Key(h)).Attention).ToList();
        }

        /// <summary>One entry for all of them, remade each day: Item holds their keys.</summary>
        private static void PostMissing()
        {
            S.Requests.RemoveAll(r => r.Source == "miss" && r.State == RequestState.Active);
            var girls = Missing();
            if (girls.Count == 0) return;
            var r0 = Make(RequestKind.Talk, girls[0], "", "", 3);
            r0.Source = "miss";
            Word(r0, girls.Select(GirlInfo.Key).ToList());
            Add(r0);
        }

        private static void Word(Request r, List<string> keys)
        {
            r.Item = string.Join(MissSep.ToString(), keys.ToArray());
            r.Girl = keys.Count == 1 ? keys[0] : "";    // one girl is a request of hers, with her mark and "Go to her"
            var names = keys.Select(GirlInfo.FirstName).ToList();
            var all = names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1).ToArray()) + " and " + names[names.Count - 1];
            r.Title = names.Count == 1 ? all + " misses you" : "They miss you";
            r.Text = names.Count == 1 ? all + " hasn't heard from you in a while. A word would do." : all + " haven't heard from you in a while. A word with each of them would do.";
        }

        /// <summary>Talking takes her name off; the last name closes the entry.</summary>
        internal const char MissSep = '\n';    // between the girls of the one entry: their keys have | in them

        private static void SawMissing(SaveData.Heroine h)
        {
            var r = Active.FirstOrDefault(x => x.Source == "miss");
            if (r == null) return;
            var key = GirlInfo.Key(h);
            var keys = r.Item.Split(MissSep).ToList();
            if (!keys.Remove(key)) return;
            if (keys.Count > 0) { Word(r, keys); return; }
            r.Girl = key;
            Complete(r, h);
        }

        /// <summary>Who turned out not to come today leaves the entry.</summary>
        private static void DropAbsentMissing()
        {
            var r = Active.FirstOrDefault(x => x.Source == "miss");
            if (r == null) return;
            var keys = r.Item.Split(MissSep).Where(Truth.AtSchool).ToList();
            if (keys.Count == 0) S.Requests.Remove(r);
            else Word(r, keys);
        }

        /// <summary>Makes sure today's board exists (e.g. a save loaded in the middle of a day).</summary>
        public static void EnsureToday()
        {
            int day;
            PostponeAbsent();
            if (S.Buffs.TryGetValue("board_day", out day) && day == S.Day) return;
            if (!G.IsSchoolDay || !G.InRoamingPeriod) return;
            Moods.RollAll();
            GenerateDaily();
        }

        /// <summary>
        /// A request today waits for a girl who did not come: an open one moves to tomorrow without a penalty. the board
        /// is made when the day starts, before the scene knows who came, so this runs again once it knows (once per
        /// period), and the second girl of a request counts too.
        /// </summary>
        public static void PostponeAbsent()
        {
            if (!Truth.Known) return;
            var stamp = S.Day * 16 + (int)G.CurrentMask;
            int done;
            if (S.Buffs.TryGetValue("board_absent", out done) && done == stamp) return;
            S.Buffs["board_absent"] = stamp;
            DropAbsentMissing();
            NoLunchOnSaturday();
            var dropped = 0;
            foreach (var r in S.Requests.Where(q => (q.State == RequestState.Active || q.State == RequestState.Hidden) && q.DeadlineDay <= S.Day
                                                    && (!string.IsNullOrEmpty(q.Girl) && !Truth.AtSchool(q.Girl) || !string.IsNullOrEmpty(q.Other) && !Truth.AtSchool(q.Other))).ToList())
            {
                // A board talk ("Say hi", "Check on her") is about today: it goes away, and a girl who is here gets one
                // instead.
                if (r.Kind == RequestKind.Talk && (r.Source == "board" || r.Source == "harem" || r.Source == "happening"))
                {
                    S.Requests.Remove(r);
                    dropped++;
                    CampusLifePlugin.Log.LogInfo("Board: " + r.Title + " dropped (" + GirlInfo.FirstName(r.Girl) + " is not on campus; it was only for today)");
                    continue;
                }
                // OptimizerLink loads her first.
                r.DeadlineDay = S.Day + 1;
                var who = !string.IsNullOrEmpty(r.Girl) && !Truth.AtSchool(r.Girl) ? r.Girl : r.Other;
                CampusLifePlugin.Log.LogInfo("Board: " + r.Title + " waits for tomorrow (" + GirlInfo.FirstName(who) + " is not on campus; she comes first tomorrow)");
            }
            if (dropped > 0) Post(dropped);
        }

        /// <summary>
        /// Sunday has no school, so a request due on it (one posted on Saturday "for tomorrow", a favor, one found out by
        /// talking) could never be done and was counted as missed on Monday. When Sunday starts, they move to Monday.
        /// </summary>
        public static void SkipHoliday()
        {
            var moved = 0;
            foreach (var r in S.Requests.Where(q => (q.State == RequestState.Active || q.State == RequestState.Hidden) && q.DeadlineDay == S.Day))
            {
                r.DeadlineDay = S.Day + 1;
                moved++;
            }
            if (moved > 0) CampusLifePlugin.Log.LogInfo("Board: " + moved + " request(s) due on Sunday move to Monday (no school on Sunday)");
        }

        /// <summary>
        /// The game has no lunch on Saturday (ActionControl skips AddLunch then): a lunch request due on Saturday could
        /// not be done. It waits for Monday, and no new one is posted on a Saturday.
        /// </summary>
        internal static void NoLunchOnSaturday(bool always = false)
        {
            if (!always && G.Weekday != Cycle.Week.Saturday) return;
            foreach (var r in S.Requests.Where(q => (q.State == RequestState.Active || q.State == RequestState.Hidden) && q.Kind == RequestKind.Lunch && q.DeadlineDay <= S.Day))
            {
                r.DeadlineDay = S.Day + 2;
                CampusLifePlugin.Log.LogInfo("Board: " + r.Title + " waits for Monday (no lunch on Saturday)");
            }
        }

        public static void GenerateDaily()
        {
            if (!Cfg.BoardEnabled.Value || !G.IsSchoolDay) return;
            // Made at the start of the day, before the scene knows who came, the board asked girls who stayed home. It
            // waits for the loaded period (EnsureToday, every second), so every request is for a girl who is here today.
            if (!Truth.Known) return;
            S.Buffs["board_day"] = S.Day;
            // Clean old finished requests (keep a few days for the journal view)
            S.Requests.RemoveAll(r => r.State != RequestState.Active && r.DeadlineDay < S.Day - 3);
            PostponeAbsent();
            PostMissing();

            var already = S.Requests.Count(r => (r.State == RequestState.Active || r.State == RequestState.Hidden) && r.Source == "board" && r.CreatedDay == S.Day);
            Post(TargetCount - already);
        }

        /// <summary>A request open today or tomorrow is about her (hers, or she is its second girl): she has to come.</summary>
        public static bool Expects(SaveData.Heroine h)
        {
            if (h == null) return false;
            var key = GirlInfo.Key(h);
            return S.Requests.Any(r => (r.State == RequestState.Active || r.State == RequestState.Hidden) && r.DeadlineDay >= S.Day
                                       && (r.Girl == key || r.Other == key || r.Source == "miss" && (r.Item ?? "").Split(MissSep).Contains(key)));
        }

        /// <summary>Nothing open on the board (active or still to find out) for today or later.</summary>
        public static bool Empty { get { return !S.Requests.Any(r => (r.State == RequestState.Active || r.State == RequestState.Hidden) && r.DeadlineDay >= S.Day); } }

        /// <summary>
        /// A new period with the board empty: three more, once per period. The day's first period has GenerateDaily.
        /// </summary>
        public static void RefillIfEmpty()
        {
            if (!Cfg.BoardEnabled.Value || !G.IsSchoolDay || !G.InRoamingPeriod) return;
            int day;
            if (!S.Buffs.TryGetValue("board_day", out day) || day != S.Day) return; // the first period: the daily board
            // After class is the last period of the day, with nothing left to do them in. The refill is for lunch and club
            // time only.
            if (G.CurrentMask == PeriodMask.After) return;
            var stamp = S.Day * 16 + (int)G.CurrentMask;
            int done;
            if (S.Buffs.TryGetValue("board_refill", out done) && done == stamp) return;
            S.Buffs["board_refill"] = stamp; // looked at once, when the period starts: emptied later, it waits for the next one
            if (!Empty) return;
            var n = Post(3);
            CampusLifePlugin.Log.LogInfo("Board: empty at the start of " + G.CurrentMask + "; " + n + " new requests");
            if (n > 0) Toasts.Show("New requests on the board (" + n + ").");
        }

        /// <summary>Up to <paramref name="want"/> requests from the girls at school with none of theirs open; how many were made.</summary>
        private static int Post(int want)
        {
            if (want <= 0) return 0;
            var made = 0;
            var pool = Students.Where(h => Truth.AtSchool(h) && !OpenFor(GirlInfo.Key(h)).Any(r => r.Source == "board")).ToList();
            if (pool.Count == 0) return 0;

            var spot = Spotlight.IsActive ? S.Spotlight : null;
            for (var i = 0; i < want && pool.Count > 0; i++)
            {
                var h = PickWeighted(pool, spot);
                pool.Remove(h);
                var r = MakeFor(h);
                if (r == null) continue;
                // Girls do not post their problems: you hear about them by talking (to her or her friends).
                if (Cfg.DiscoverRequests.Value && r.Kind != RequestKind.Talk)
                {
                    r.State = RequestState.Hidden;
                    r.DeadlineDay = S.Day + 1;
                }
                // Posted after class (a save loaded then, a dropped talk replaced): it is for tomorrow, never a miss tonight.
                if (G.CurrentMask == PeriodMask.After && r.Kind != RequestKind.Talk && r.DeadlineDay <= S.Day) r.DeadlineDay = S.Day + 1;
                if (G.CurrentMask == PeriodMask.After && r.Kind == RequestKind.Talk) continue;
                Add(r);
                made++;
            }
            return made;
        }

        private static SaveData.Heroine PickWeighted(List<SaveData.Heroine> pool, List<string> spotlight)
        {
            var weights = pool.Select(h =>
            {
                var gs = S.Girl(GirlInfo.Key(h));
                var w = 1f + (100 - gs.Attention) / 25f;
                if (spotlight != null)
                {
                    if (spotlight.Contains(gs.Key)) w *= 6f;
                    else if (!h.isGirlfriend) w *= 0.3f;
                }
                if (h.isGirlfriend) w *= 1.5f;
                if (gs.Mood != Mood.None && gs.Mood != Mood.Cheerful) w *= 1.3f;
                return w;
            }).ToList();
            var total = weights.Sum();
            var r = Random.value * total;
            for (var i = 0; i < pool.Count; i++)
            {
                r -= weights[i];
                if (r <= 0f) return pool[i];
            }
            return pool[pool.Count - 1];
        }

        public static void Add(Request r)
        {
            // One gift request per girl at a time, whoever posts it (two came in through Add).
            if (r.Kind == RequestKind.Gift && !string.IsNullOrEmpty(r.Girl) && Active.Any(x => x.Kind == RequestKind.Gift && x.Girl == r.Girl))
            {
                CampusLifePlugin.Log.LogInfo("Board: " + GirlInfo.FirstName(r.Girl) + " already has a gift request open; '" + r.Title + "' not posted.");
                return;
            }
            // Every request goes through here (harem "Time with X", "Cheer up X", "Be there for X", the arcs...), and some
            // were made at the start of the day, before the scene knew who came. A talk with a girl who is not here today
            // is not posted; anything else waits for tomorrow, when OptimizerLink loads her first.
            if (Truth.Known && r.Source != "miss")
            {
                var away = !string.IsNullOrEmpty(r.Girl) && !Truth.AtSchool(r.Girl) ? r.Girl : !string.IsNullOrEmpty(r.Other) && !Truth.AtSchool(r.Other) ? r.Other : null;
                if (away != null)
                {
                    if (r.Kind == RequestKind.Talk && r.Source != "arc")
                    {
                        CampusLifePlugin.Log.LogInfo("Board: '" + r.Title + "' not posted (" + GirlInfo.FirstName(away) + " is not on campus today)");
                        return;
                    }
                    if (r.DeadlineDay <= S.Day) r.DeadlineDay = S.Day + 1;
                    CampusLifePlugin.Log.LogInfo("Board: '" + r.Title + "' is for tomorrow (" + GirlInfo.FirstName(away) + " is not on campus today)");
                }
            }
            r.Id = S.NextRequestId++;
            r.CreatedDay = S.Day;
            if (r.DeadlineDay < S.Day) r.DeadlineDay = S.Day;
            S.Requests.Add(r);
        }

        private static Request MakeFor(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var name = GirlInfo.FirstName(h);
            var stranger = h.relation < 0;
            var friendPlus = h.relation >= 1 || h.isGirlfriend;
            var lewdOk = Cfg.LewdRequests.Value && G.Sens >= 40 && (h.isGirlfriend || (friendPlus && h.lewdness >= 80));

            if (stranger)
                return Make(RequestKind.Talk, h, "Say hi to " + name,
                    name + " keeps glancing at you in the hallway. Introduce yourself today.", 5);

            var options = new List<RequestKind>();
            switch (gs.Mood)
            {
                case Mood.Hungry: options.Add(RequestKind.Lunch); options.Add(RequestKind.Gift); break;
                case Mood.Stressed: options.Add(RequestKind.Study); options.Add(RequestKind.Gift); break;
                case Mood.Bored: options.Add(RequestKind.Exercise); options.Add(RequestKind.MeetAt); if (h.isStaff) options.Add(RequestKind.Club); break;
                case Mood.Lonely: options.Add(RequestKind.Talk); if (friendPlus) options.Add(RequestKind.WalkHome); break;
                case Mood.Flirty: if (lewdOk) options.Add(RequestKind.Intimate); options.Add(RequestKind.MeetAt); break;
                case Mood.Sleepy: options.Add(RequestKind.Gift); options.Add(RequestKind.Talk); break;
                case Mood.Upset: options.Add(RequestKind.Gift); break;
                default: options.AddRange(new[] { RequestKind.Errand, RequestKind.MeetAt, RequestKind.Talk, RequestKind.Lunch, RequestKind.Study, RequestKind.Exercise }); break;
            }
            // Already got her gift today, or another gift request of hers is open ("X could use a treat" and "X asked
            // you to bring her something" at once): no request for another one.
            if (G.Weekday == Cycle.Week.Saturday) { options.RemoveAll(k => k == RequestKind.Lunch); if (options.Count == 0) options.Add(RequestKind.Gift); }
            if (gs.LastGiftDay == S.Day || OpenFor(gs.Key).Any(q => q.Kind == RequestKind.Gift)) { options.RemoveAll(k => k == RequestKind.Gift); if (options.Count == 0) options.Add(RequestKind.Talk); }
            if (Random.value < 0.25f) options.Add(RequestKind.Errand);
            // Asking to meet somewhere is for a girl who knows you.
            if (!Social.KnowsYou(h)) { options.RemoveAll(k => k == RequestKind.MeetAt || k == RequestKind.WalkHome); if (options.Count == 0) options.Add(RequestKind.Talk); }
            var kind = options[Random.Range(0, options.Count)];

            switch (kind)
            {
                case RequestKind.Lunch:
                    return Make(kind, h, "Lunch with " + name, name + (gs.Mood == Mood.Hungry ? " forgot her lunch and looks starving." : " wants company for lunch.") + " Invite her to eat lunch.", 8, PeriodMask.Lunch);
                case RequestKind.Study:
                    return Make(kind, h, "Study with " + name, name + (gs.Mood == Mood.Stressed ? " is panicking about class." : " asked if you could study together.") + " Invite her to study.", 8, PeriodMask.Any, intel: 1);
                case RequestKind.Exercise:
                    return Make(kind, h, "Work out with " + name, name + " says she has too much energy today. Invite her to exercise.", 8, PeriodMask.Any, str: 1);
                case RequestKind.Club:
                    return Make(kind, h, "Club time with " + name, name + " wants to do something with the Koikatsu Club. Invite her to club activities.", 8, PeriodMask.Club, club: 2);
                case RequestKind.WalkHome:
                    return Make(kind, h, "Walk " + name + " home", name + " doesn't want to walk home alone today. Ask her to go home together after class.", 10, PeriodMask.After);
                case RequestKind.Talk:
                    return Make(kind, h, "Check on " + name, name + (gs.Mood == Mood.Lonely ? " seems lonely today." : gs.Mood == Mood.Sleepy ? " is half asleep at her desk." : " wants to tell you something.") + " Talk to her today.", 5);
                case RequestKind.Gift:
                {
                    var likes = GirlInfo.LikesOf(h);
                    var like = gs.Mood == Mood.Hungry ? "food" : gs.Mood == Mood.Stressed ? "sweets" : gs.Mood == Mood.Sleepy ? "coffee" : "";
                    if (like == "" && likes.Count > 0 && Random.value < 0.5f) like = likes[Random.Range(0, likes.Count)];
                    var what = like == "" ? "a little present" : "something (" + GirlInfo.LikeLabel(like) + ")";
                    var why = gs.Mood == Mood.Upset ? BadMoodWhy : TreatWhy;
                    var r = Make(kind, h, "A gift for " + name, name + why + " give her " + what + ". The university store is in the cafeteria.", 6);
                    r.Item = like;
                    return r;
                }
                case RequestKind.Intimate:
                {
                    var r = Make(kind, h, "Private time with " + name, name + " sent you a message: 'Find me today... somewhere quiet.' (H with her)", 6, PeriodMask.Any, lewd: 8, sens: 1);
                    r.Lewd = true;
                    return r;
                }
                case RequestKind.MeetAt:
                {
                    var places = new[] { "rooftop", "library", "courtyard", "pool", "gym", "cafeteria" };
                    var place = places[Random.Range(0, places.Length)];
                    var r = Make(kind, h, "Meet " + name + " at the " + Places.Name(place),
                        name + " wants to see you at the " + Places.Name(place) + ". Bring her there (or find her there) and talk.", 9);
                    r.Place = place;
                    return r;
                }
                default: // Errand
                {
                    var e = Errands[Random.Range(0, Errands.Length)];
                    var r = Make(RequestKind.Errand, h, "Errand for " + name, name + " asks a favor: " + e[1] + " Go to the " + Places.Name(e[0]) + ", then tell her.", 8);
                    r.Place = e[0];
                    return r;
                }
            }
        }

        public static Request Make(RequestKind kind, SaveData.Heroine h, string title, string text, int favor,
            PeriodMask periods = PeriodMask.Any, int lewd = 0, int money = 0, int intel = 0, int str = 0, int sens = 0, int club = 0, int rep = 1)
        {
            var r = new Request
            {
                Kind = kind, Girl = GirlInfo.Key(h), Title = title, Text = text, Periods = periods, DeadlineDay = S.Day,
                Reward = new Rewards { Favor = favor, Lewd = lewd, Money = money, Int = intel, Str = str, Sens = sens, Club = club, Rep = rep },
            };
            // Every request also pays something that still matters when favor is maxed out.
            r.Reward.Devotion = Math.Max(2, favor / 2 + (kind == RequestKind.Intimate ? 4 : 0));
            if (r.Reward.Money == 0 && !Cfg.DiscoverRequests.Value) r.Reward.Money = BasePay(kind);
            if (kind == RequestKind.Gift || (kind != RequestKind.Talk && Random.value < 0.25f)) r.Reward.Item = ThankYouItem();
            return r;
        }

        /// <summary>Yen the girl (or the club) pays you; reputation makes people more generous.</summary>
        private static int BasePay(RequestKind kind)
        {
            int baseYen;
            switch (kind)
            {
                case RequestKind.Talk: baseYen = 150; break;
                case RequestKind.Gift: baseYen = 0; break;
                case RequestKind.Errand: baseYen = 450; break;
                case RequestKind.MeetAt: baseYen = 250; break;
                case RequestKind.Intimate: baseYen = 0; break;
                default: baseYen = 300; break;
            }
            return (int)(baseYen * (1f + S.Reputation / 100f) / 10) * 10;
        }

        private static string ThankYouItem()
        {
            var pool = Items.All.Where(i => i.Type == ItemType.Consumable && i.Id != "cat_food").ToList();
            return pool[Random.Range(0, pool.Count)].Id;
        }

        // ---------------- completion ----------------

        internal static bool InWindow(Request r)
        {
            return r.DeadlineDay >= S.Day && (r.Periods & G.CurrentMask) != 0;
        }

        private static bool Matches(Request r, ResultEnum result)
        {
            switch (r.Kind)
            {
                case RequestKind.Lunch: return result == ResultEnum.Lunch;
                case RequestKind.Study: return result == ResultEnum.Study;
                case RequestKind.Exercise: return result == ResultEnum.Exercise;
                case RequestKind.Club: return result == ResultEnum.Club;
                case RequestKind.WalkHome: return result == ResultEnum.GoHome;
                case RequestKind.Intimate: return result == ResultEnum.H;
                // Plain board talks complete on any conversation; arc talks need their scene (talk button).
                case RequestKind.Talk: return r.Source != "arc" && !IsRumor(r) && !(r.Source == "favor" && !string.IsNullOrEmpty(r.Other)); // a message completes with the friend; the rumor with its scene
                case RequestKind.MeetAt: return r.Source != "arc" && Places.CurrentId == r.Place;
                case RequestKind.Errand: return r.Source != "arc" && r.Progress > 0;
                default: return false;
            }
        }

        public static void OnTalkResult(SaveData.Heroine h, ResultEnum result)
        {
            var key = GirlInfo.Key(h);
            SawMissing(h);
            foreach (var r in ActiveFor(key).ToList())
                if (InWindow(r) && Matches(r, result)) Complete(r, h);
        }

        public static void OnIntimate(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            foreach (var r in ActiveFor(key).Where(x => x.Kind == RequestKind.Intimate).ToList())
                if (r.DeadlineDay >= S.Day) Complete(r, h);
        }

        private static readonly HashSet<string> TalkKept = new HashSet<string>();
        private static int _talkKeptPeriod = -1;

        /// <summary>
        /// A girl with an open request for this period gets one point of talk time back (once a period) when she has
        /// none: with an empty blue bar the game ends the talk at once, so the request could not be done at all (a gift
        /// request with Nagatoro; no talk, no gift, and the next period "already got a gift today").
        /// </summary>
        public static void KeepTalkTime()
        {
            if (!G.InRoamingPeriod) return;
            var period = S.Day * 16 + (int)G.CurrentMask;
            if (period != _talkKeptPeriod) { _talkKeptPeriod = period; TalkKept.Clear(); }
            foreach (var r in Active.Where(x => !string.IsNullOrEmpty(x.Girl) && x.Kind != RequestKind.Activity && InWindow(x)).ToList())
            {
                if (TalkKept.Contains(r.Girl)) continue;
                var h = GirlInfo.Find(r.Girl);
                if (h == null || h.talkTime > 0) continue;
                h.talkTime = 1;
                TalkKept.Add(r.Girl);
                CampusLifePlugin.Log.LogDebug(GirlInfo.FullName(h) + ": one talk back for the request '" + r.Title + "'");
            }
        }

        /// <summary>
        /// The game ends a talk at once when her blue bar is empty; now, the first time you go up to her in a period with
        /// it empty, she gets one point back for a short talk: the free options only (as the point given back for a
        /// request), and the game's goodbye once they are used.
        /// </summary>
        public static bool KeepTalkFor(SaveData.Heroine h)
        {
            if (h == null || h.talkTime > 0 || !G.InRoamingPeriod) return false;
            var period = S.Day * 16 + (int)G.CurrentMask;
            if (period != _talkKeptPeriod) { _talkKeptPeriod = period; TalkKept.Clear(); }
            var key = GirlInfo.Key(h);
            if (TalkKept.Contains(key)) return false;
            h.talkTime = 1;
            TalkKept.Add(key);
            CampusLifePlugin.Log.LogInfo(GirlInfo.FullName(h) + ": empty blue bar, one point back for a short talk (free options only).");
            return true;
        }

        /// <summary>She has a request open now (the talk stays open on an empty blue bar, with the free options only).</summary>
        public static bool HasOpenNow(SaveData.Heroine h)
        {
            if (h == null) return false;
            return ActiveFor(GirlInfo.Key(h)).Any(r => r.Kind != RequestKind.Activity && InWindow(r));
        }

        /// <summary>Her talk point is the one given back for a request: it is for the request, not for chatting.</summary>
        public static bool TalkKeptFor(SaveData.Heroine h)
        {
            return h != null && TalkKept.Contains(GirlInfo.Key(h)) && h.talkTime <= 1;
        }

        /// <summary>She asked for a gift and this one fits: the once-a-day limit does not apply.</summary>
        public static bool WantsGift(SaveData.Heroine h, ItemDef item)
        {
            if (h == null || item == null) return false;
            return ActiveFor(GirlInfo.Key(h)).Any(r => r.Kind == RequestKind.Gift && r.DeadlineDay >= S.Day
                                                       && (string.IsNullOrEmpty(r.Item) || Items.MatchesCategory(item, r.Item) || r.Item == item.Id));
        }

        /// <summary>Who has an open gift request (on the board) that this item fulfils: their first names.</summary>
        public static List<string> GiftFitsFor(ItemDef item)
        {
            // Only requests for a kind or an item: one for any gift would mark the whole store.
            if (item == null) return new List<string>();
            return Active.Where(r => r.Kind == RequestKind.Gift && r.DeadlineDay >= S.Day && !string.IsNullOrEmpty(r.Girl)
                                     && !string.IsNullOrEmpty(r.Item) && (Items.MatchesCategory(item, r.Item) || r.Item == item.Id))
                .Select(r => GirlInfo.FirstName(r.Girl)).Distinct().ToList();
        }

        /// <summary>Girls who asked for a gift of any kind (the store says so once, at the top).</summary>
        public static List<string> AnyGiftFor()
        {
            return Active.Where(r => r.Kind == RequestKind.Gift && r.DeadlineDay >= S.Day && !string.IsNullOrEmpty(r.Girl) && string.IsNullOrEmpty(r.Item))
                .Select(r => GirlInfo.FirstName(r.Girl)).Distinct().ToList();
        }

        /// <summary>The color of "this fulfils a request" (the border of the items that fit).</summary>
        public static readonly Color FitColor = new Color(1f, 0.55f, 0.8f, 1f);

        public static void OnGift(SaveData.Heroine h, ItemDef item)
        {
            var key = GirlInfo.Key(h);
            foreach (var r in ActiveFor(key).Where(x => x.Kind == RequestKind.Gift).ToList())
                if (r.DeadlineDay >= S.Day && (string.IsNullOrEmpty(r.Item) || Items.MatchesCategory(item, r.Item) || r.Item == item.Id))
                    Complete(r, h);
        }

        public static void OnActivity(ActivityDef a)
        {
            foreach (var r in Active.Where(x => x.Kind == RequestKind.Activity && x.Activity == a.Id).ToList())
                Complete(r, null);
        }

        /// <summary>
        /// A story step that a temporary talk button can finish right now. Plain board requests need no button:
        /// talking, inviting or giving the gift completes them by itself.
        /// </summary>
        public static Request CompletableInTalk(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            return ActiveFor(key).FirstOrDefault(r =>
                (r.Source == "arc" || IsRumor(r)) && r.DeadlineDay >= S.Day && (r.Periods & G.CurrentMask) != 0 &&
                (r.Kind == RequestKind.Talk ||
                 (r.Kind == RequestKind.MeetAt && Places.CurrentId == r.Place) ||
                 (r.Kind == RequestKind.Errand && r.Progress > 0)));
        }

        /// <summary>Text for the temporary talk button of a story step.</summary>
        public static string ButtonLabel(Request r)
        {
            if (!string.IsNullOrEmpty(r.Button)) return r.Button;
            switch (r.Kind)
            {
                case RequestKind.MeetAt: return "Meet up";
                case RequestKind.Errand: return "It's done";
                default: return "Talk about it";
            }
        }

        /// <summary>Errands finish when you reach the place (called every second while roaming).</summary>
        public static void CheckErrands()
        {
            if (!G.InRoamingPeriod) return;
            var here = Places.CurrentId;
            if (string.IsNullOrEmpty(here)) return;
            foreach (var r in Active.Where(x => x.Kind == RequestKind.Errand && x.Progress == 0 && x.Place == here && InWindow(x)).ToList())
            {
                r.Progress = 1;
                Toasts.Show((r.Source == "arc" ? "Found it! " : "Errand done. ") + "Now talk to " + GirlInfo.FirstName(r.Girl) + ".");
            }
        }

        /// <summary>Plays the request scene inside the conversation, then completes it.</summary>
        public static bool PlayInTalk(Request r, SaveData.Heroine h, Action after = null)
        {
            var b = new SceneBuilder(h, true);
            var tone = GirlInfo.ToneOf(h);
            if (r.Source == "arc")
            {
                Arcs.BuildTalkScene(r, h, b);
            }
            else if (r.Source == "happening" && r.Title == RumorTitle) RumorScene(b, h, tone);
            else
            {
                switch (r.Kind)
                {
                    case RequestKind.MeetAt:
                        if (r.Source == "happening" && r.Title == "A love letter")
                        {
                            b.Me(Lines.Pick("So the letter was from you.", "I found your letter. I came."));
                            b.Her_(Lines.T(tone, "Y-you came... I wasn't sure you would. || You really came... I'm so glad. || Y-you're here... I was nervous you wouldn't come. || Ah... you came. Thank you.", "You came! Okay, okay, deep breath... || You're here! Okay, okay, don't freak out, me! || Yay, you came! Ahh, now I'm nervous! || You actually came! Deep breaths!", "...You actually came. || ...You came. Good. || ...So you came. || ...I wasn't sure you would.", "Took you long enough. I was starting to think you chickened out. || You came. Good boy. || Finally. I almost gave up on you. || So you're not a coward. Noted."));
                            b.Her_(Lines.T(tone, "I just... wanted to see you, away from everyone. || I wanted to see you... just the two of us. || I just wanted some time alone with you. || I wanted to see you without anyone around...", "I wanted you all to myself for a bit, hehe. || I wanted you all to myself, hehe! || No crowd, just us! I wanted that! || Just you and me! That was the plan!", "I wanted to talk without anyone watching. || I wanted to talk privately. || No witnesses. That's why. || I wanted to speak where no one listens.", "Let's just say I wanted a better look at you. || I wanted you without an audience. || Let's say I wanted you alone~ || Away from everyone. Interesting, isn't it?"));
                            break;
                        }
                        if (r.Source == "note")
                        {
                            NoteScene(b, h, tone, r);
                            break;
                        }
                        b.Me(Lines.Pick("Hey. I'm here, like you asked.", "Sorry, did I keep you waiting?"));
                        b.Her_(Lines.MeetAtLine(tone, Places.Name(r.Place)));
                        break;
                    case RequestKind.Errand:
                        b.Me(Lines.Pick("Done. Took care of it for you.", "All set. That favor you asked for."));
                        b.Her_(Lines.ErrandDelivered(tone));
                        break;
                    default:
                        b.Me(r.LearnedFrom == "her" ? Lines.Pick("About what you told me earlier...", "I came like I said I would.")
                            : !string.IsNullOrEmpty(r.LearnedFrom) ? Lines.Pick(GirlInfo.FirstName(r.LearnedFrom) + " told me you needed a hand.", "I heard from " + GirlInfo.FirstName(r.LearnedFrom) + " that you wanted to talk.")
                            : Lines.Pick("Hey, you wanted to talk?", "I saw your note on the club board."));
                        b.Her_(Lines.RequestThanks(tone));
                        break;
                }
            }
            return Scenes.PlayInTalk(b, choice =>
            {
                if (r.Source == "arc") Arcs.OnTalkSceneDone(r, h, choice);
                else Complete(r, h);
                if (r.Source == "note") NoteAfter(h, choice);
                if (r.Source == "happening" && r.Title == RumorTitle) RumorAfter(h, choice);
                if (after != null) after();
            });
        }

        /// <summary>
        /// The rooftop talk from the note in your locker (it never had a line of its own): the place, what she needs (her
        /// voice) and three ways to answer, each with its own reaction.
        /// </summary>
        private static void NoteScene(SceneBuilder b, SaveData.Heroine h, Tone tone, Request r)
        {
            var name = GirlInfo.FirstName(h);
            b.Narr(Lines.Pick("The rooftop is empty at this hour. Wind, a far-off club practice, and " + name + " waiting by the fence.",
                              name + " is waiting by the fence, up on the rooftop. The sky is going orange behind her.",
                              "Nobody else is up here. " + name + " turns around the moment the door creaks."));
            b.Me(Lines.Pick("I got your note. What's up?", "So you're the one who left the note."));
            b.Her_(Lines.NoteHelp(tone));
            b.Choice("Offer to go through it with her.", "help", "Tell her she'll do fine without any practice.", "cheer", "Tease her about the mystery signature.", "tease");
            b.Branch("help");
            b.Me(Lines.Pick("Sure. Let's take a look.", "Of course. Show me."));
            b.Her_(Lines.NoteThanks(tone));
            b.Narr("You stay until the sun is nearly gone. It's a better afternoon than most.");
            b.End();
            b.Branch("cheer");
            b.Me(Lines.Pick("You don't need practice. You'll be fine.", "Honestly? You've got this. Don't overthink it."));
            b.Her_(Lines.T(tone, "...You really think so? ...Okay. I'll try to believe it. || ...You think so? ...Then I'll try to believe it too. || ...Really? ...Thank you. I feel a bit braver. || ...If you say so... okay. I'll believe you.", "Hehe, you're too nice! But... thank you, I feel lighter. || Aww, stop it! But thanks, really! || Hehe, you're the best! I feel way better! || You're too nice! Okay, I'm cheered up!", "...Flattery. Fine. It worked a little. || ...Flattery. It worked, slightly. || ...Thank you. That helps. || ...Hm. I'll accept that.", "Oh? Faith in me already? Careful, I'll hold you to it~ || Faith in me? Bold. I'll remember it~ || Oh? You believe in me? Cute. || Careful, I'll hold you to that."));
            b.End();
            b.Branch("tease");
            b.Me(Lines.Pick("Just \"" + name.Substring(0, 1).ToUpper() + ".\"? Very mysterious.", "A single letter. Were you going for spy novel?"));
            b.Her_(Lines.T(tone, "T-that was... I was embarrassed! Please don't make it worse... || D-don't tease me... I was so embarrassed! || P-please, I was mortified... don't make it worse. || Ah... I wanted to hide forever...", "Ahaha, busted! I panicked, okay?! || Haha, busted! I freaked out, okay?! || Ugh, caught! I panicked! || Okay, okay, I chickened out! Don't laugh!", "...I didn't want my name on it. Drop it. || ...I didn't want my name on it. Drop it. || ...Anonymous was intentional. Leave it. || ...It wasn't meant to be traced. Forget it.", "Mysterious is my brand. Now, help me, spy. || A girl needs her mysteries. Now help me, detective. || Mystery suits me. So, helping or not? || Caught me. Now you have to help."));
            b.End();
        }

        public const string RumorTitle = "Is the rumor true?";

        /// <summary>The rumor that she likes you: done with its own scene (the talk button), not by any chat.</summary>
        public static bool IsRumor(Request r) { return r != null && r.Source == "happening" && r.Title == RumorTitle; }

        /// <summary>Her answer to the rumor, from the director's confession score: yes, a shy yes, a shy no, no.</summary>
        private static string RumorAnswer(SaveData.Heroine h)
        {
            string why;
            var c = Consent.Chance(h, Ask.Confess, out why);
            var a = c >= 0.6f ? "yes" : c >= 0.3f ? "wary_yes" : c >= 0f ? "wary_no" : "no";
            CampusLifePlugin.Log.LogInfo("Board: rumor talk with " + GirlInfo.FirstName(h) + " -> " + a + " (score " + c.ToString("0.00") + ": " + why + ")");
            return a;
        }

        /// <summary>
        /// The rumor that she likes you: you bring it up, she answers as the director has her (sure, flustered, dodging,
        /// flat no), and you choose what to say back; telling her you like her too leads into the game's own confession
        /// when she is open to it.
        /// </summary>
        private static void RumorScene(SceneBuilder b, SaveData.Heroine h, Tone tone)
        {
            var name = GirlInfo.FirstName(h);
            var a = RumorAnswer(h);
            _rumorAnswer = a;
            b.Narr(Lines.Pick("A couple of girls nearby go quiet the moment you walk up to " + name + ".", name + " sees you coming and starts fixing her hair.", "Someone behind you whispers \"that's him\" as you reach " + name + "."));
            b.Me(Lines.Pick("So... I heard a rumor about you. About you and me.", "People are saying something about you. About how you feel about me.", "I keep hearing a rumor. That you like me. Is it true?"));
            switch (a)
            {
                case "yes":
                    b.Face(h, Feel.Love).Her_(Lines.T(tone, "...It's true. I didn't want you to hear it from them, but... it's true. I like you. || ...I wanted to tell you myself. Yes. I like you. || ...It's true. I've liked you for a while now.",
                        "Ugh, they told you?! ...Okay, fine, yes! I like you! A lot! || They blabbed?! ...Yes. Yes, I like you! || ...Yeah. It's true. Don't laugh!",
                        "...Yes. It's true. I wasn't going to deny it. || ...It's true. I like you. There. || ...I won't pretend. It's true.",
                        "And if it is? ...It is. I like you. Your move. || So the secret's out. Yes, I like you. Now what? || True. Surprised? You shouldn't be."));
                    break;
                case "wary_yes":
                    b.Face(h, Feel.Shy).Her_(Lines.T(tone, "W-who told you that?! ...I mean... maybe it's a little true... || I... um... it's not... okay, maybe it's a little true. || Don't look at me like that... it might be true.",
                        "WHAT?! Who said that?! ...Okay, maybe. A little. Don't make it weird! || Th-that's... ugh, maybe! A little! || Who's spreading that?! ...It's not totally wrong.",
                        "...People talk too much. ...It isn't entirely wrong. || ...I didn't say it wasn't true. || ...It's possible. Don't push it.",
                        "Oh? And you came running to check? ...Maybe I do. Maybe. || You want it to be true, don't you? ...It might be. || Hmm. Maybe it is. You'll have to earn the rest."));
                    break;
                case "wary_no":
                    b.Face(h, Feel.Shy).Her_(Lines.T(tone, "P-people say all kinds of things... I like talking to you, that's all. || I... I don't know. I like being around you. Isn't that enough for now? || It's... complicated. I like you as a friend. I think.",
                        "Ehh?! People say weird stuff! I mean, you're fun, but... || Whoa, slow down! You're cool, okay? That's all I'm saying! || Rumors are rumors! ...You're nice, though.",
                        "...It's a rumor. Treat it like one. I don't dislike you. || ...I haven't decided anything. || ...Don't read too much into it.",
                        "Getting ahead of yourself? I didn't say anything like that. Yet. || Cute that you believed it. Maybe someday. || Rumors move faster than I do. Patience."));
                    break;
                default:
                    b.Face(h, Feel.Serious).Her_(Lines.T(tone, "I'm sorry... that's just gossip. I don't feel that way. || It isn't true. I'm sorry if they got your hopes up. || I don't know who started it, but it isn't true.",
                        "No way! Somebody's messing with you! || Nope! Not true! Who's spreading this?! || Ha, no. Sorry! Somebody got it wrong!",
                        "No. It isn't true. || Gossip. Nothing more. || I don't. Sorry.",
                        "Oh, please. Someone's playing a prank on you. || You believed that? No. || Not even close. Nice try."));
                    break;
            }
            var warm = a == "yes" || a == "wary_yes";
            b.Choice("Tell her you like her too.", "confess", warm ? "Say you're glad you heard it." : "Say you're glad you can still be friends.", "warm", "Laugh it off.", "laugh");
            b.Branch("confess");
            b.Me(Lines.Pick("I like you too. I wanted to say it properly.", "Then let me say it too. I like you.", "For what it's worth... I like you. Really."));
            if (warm)
            {
                b.Face(h, Feel.Love).Her_(Lines.T(tone, "...Then say it properly. I want to hear it right. || ...Really? Then... say it again, properly. || ...Ask me properly. I'll answer.",
                    "R-really?! Then do it right! Like, properly properly! || Seriously?! Then say it for real! || Wait wait wait, say it right! I want to remember it!",
                    "...Then ask me. Properly. || ...Say it right, then. I'm listening. || ...Then do it properly.",
                    "Oh? Then go on. Make it a real confession. || Then say it properly. I'm waiting~ || Finally. Now do it right."));
            }
            else
            {
                b.Face(h, Feel.Shy).Her_(Lines.T(tone, "...Thank you. I'm not ready for that. Can we just... stay like this a while? || ...I'm glad. But I need more time. || ...That's sweet. Not yet, okay?",
                    "Whoa! That's... that's a lot! Can I think about it?! || Ah... you're sweet! But not yet, okay?! || Eh?! Give me some time!",
                    "...I heard you. Give me time. || ...Not now. Ask me again when we know each other better. || ...I'll think about it.",
                    "Bold. I like bold. But no. Not yet. || Look at you. Ask me again later~ || That was cute. The answer's still not yet."));
            }
            b.End();
            b.Branch("warm");
            b.Me(warm ? Lines.Pick("I'm glad I heard it. Really.", "Honestly? That made my day.") : Lines.Pick("That's fine. I like talking to you too.", "Then we're good. No pressure."));
            b.Face(h, warm ? Feel.Love : Feel.Smile).Her_(warm ? Lines.T(tone, "...Me too. I'm glad it's out. || ...Hehe. Okay. Good. || ...Then I'm glad they talked.", "Good! Because I'm not taking it back! || Yay! Okay, okay, I'm happy! || Then it's a good rumor!", "...Good. || ...Then it worked out. || ...I'm glad too.", "Of course you are~ || Good answer. || Then the rumor did its job.")
                : Lines.T(tone, "...Thank you for being kind about it. || ...Thanks. Really. || ...You're nice, you know.", "Phew! Okay, good! Friends! || You're the best! Friends! || Ahaha, okay! Good!", "...Good. || ...Fine by me. || ...Thanks.", "Smart answer. || Look at you, all mature. || Good. Keep it that way. For now~"));
            b.End();
            b.Branch("laugh");
            b.Me(Lines.Pick("Ha, people will say anything.", "Right, as if. Funny rumor."));
            b.Face(h, warm ? Feel.Sad : Feel.Smile).Her_(warm ? Lines.T(tone, "...Yeah. Anything. || ...Right. Funny. || ...Ha. Yeah.", "...Ha. Yeah. Totally. || ...Haha... right. || ...Yeah, so funny.", "...Right. || ...Of course. || ...Sure.", "...Wow. Okay. || ...You're laughing? Noted. || ...Funny, sure.")
                : Lines.T(tone, "Hehe, right? People are silly. || Right? So silly. || I know, right?", "Right?! So dumb! || Hahaha, I know! || Gossip, ugh!", "...Agreed. || ...Exactly. || ...Ridiculous.", "Right? As if~ || So silly. || Please, as if~"));
            b.End();
        }

        private static string _rumorAnswer = "";

        private static void RumorAfter(SaveData.Heroine h, string choice)
        {
            try
            {
                if (h == null) return;
                var warm = _rumorAnswer == "yes" || _rumorAnswer == "wary_yes";
                if (choice == "confess")
                {
                    if (warm && !h.isGirlfriend) Conversation.QueueConfession();   // the game's confession, answered by the director
                    else if (!warm) { Social.AddTrust(h, 1); }
                }
                else if (choice == "warm") { G.AddFavor(h, warm ? 4 : 2, 0); Social.AddTrust(h, 2); }
                else if (choice == "laugh")
                {
                    if (warm) { G.AddFavor(h, -4, 0); Social.AddTrust(h, -3); Toasts.Show(GirlInfo.FirstName(h) + " looks hurt. (-4 favor)"); }
                    else G.AddFavor(h, 1, 0);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Rumor after: " + e.Message); }
        }

        private static void NoteAfter(SaveData.Heroine h, string choice)
        {
            try
            {
                if (h == null) return;
                var tone = GirlInfo.ToneOf(h);
                if (choice == "help") { G.AddFavor(h, 3, 0); Social.AddTrust(h, 4); Social.Add(Social.Kinds.Helped, h); }
                else if (choice == "cheer") { Social.AddTrust(h, 2); G.AddFavor(h, 1, 0); }
                else if (choice == "tease")
                {
                    var fine = tone == Tone.Teasing || tone == Tone.Energetic;
                    Social.AddTrust(h, fine ? 1 : -3);
                    if (fine) G.AddFavor(h, 1, 0);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Note after: " + e.Message); }
        }

        /// <summary>She asked to meet you right here, now (a note, a letter, "meet me at..."), not a story step.</summary>
        public static Request MeetHere(SaveData.Heroine h)
        {
            if (h == null) return null;
            return ActiveFor(GirlInfo.Key(h)).FirstOrDefault(r => r.Kind == RequestKind.MeetAt && r.Source != "arc" && InWindow(r) && Places.CurrentId == r.Place);
        }

        public static void Complete(Request r, SaveData.Heroine h)
        {
            if (r.State != RequestState.Active) return;
            r.State = RequestState.Done;
            var reward = r.Reward;
            if (h != null && Cfg.DevotionEnabled.Value)
            {
                var mult = Devotion.RequestMultiplier(h);
                reward = new Rewards
                {
                    Favor = (int)(reward.Favor * mult), Lewd = (int)(reward.Lewd * mult), Money = (int)(reward.Money * mult * Devotion.MoneyMultiplier(h)),
                    Rep = reward.Rep, Int = reward.Int, Str = reward.Str, Sens = reward.Sens, Club = reward.Club,
                    Devotion = (int)(reward.Devotion * mult), Item = reward.Item,
                };
            }
            var applied = Campus.Apply(reward, h);
            try { Pairs.OnHelped(h); } catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
            S.Stat("requests");
            S.Stat("req:" + r.Kind);
            if (h != null)
            {
                var gs = S.Girl(r.Girl);
                gs.Attention = Mathf.Clamp(gs.Attention + 10, 0, 100);
            }
            if (h != null && r.Source != "event")
            {
                Social.Add(Social.Kinds.KeptPromise, h);
                Social.AddTrust(h, 3);
            }
            var msg = "Done: " + r.Title + " (" + applied.Describe() + ")";
            S.Log(msg);
            Toasts.Show(msg);
            if (r.Source == "arc") Arcs.OnRequestDone(r, h);
        }

        public static void Fail(Request r)
        {
            if (r.State != RequestState.Active) return;
            if (r.Source == "miss") { S.Requests.Remove(r); return; }
            r.State = RequestState.Failed;
            CampusLifePlugin.Log.LogInfo("Board: missed '" + r.Title + "' (due day " + r.DeadlineDay + ", window " + r.Periods + "; now day " + S.Day + " " + G.Weekday + " " + G.CurrentMask + ")");
            var h = GirlInfo.Find(r.Girl);
            if (Cfg.FailedRequestPenalty.Value && h != null && r.Source != "event" && !Devotion.ForgivesMisses(h))
            {
                G.AddFavor(h, -2, 0);
                var gs = S.Girl(r.Girl);
                gs.Attention = Mathf.Clamp(gs.Attention - 10, 0, 100);
            }
            if (h != null && r.Source != "event" && r.Source != "arc") Social.Add(Social.Kinds.BrokePromise, h);
            S.Stat("missed");
            S.Log("Missed: " + r.Title + ".");
            if (r.Source == "arc") Arcs.OnRequestFailed(r);
        }

        public const string EntryKey = "dir:entry";

        /// <summary>The day this save first met the mod: what it asked for before you could see it was never asked.</summary>
        private static bool EnteredOn(int day)
        {
            int entry;
            return S.Buffs.TryGetValue(EntryKey, out entry) && entry == day;
        }

        public static void ExpireAll(int beforeDay)
        {
            S.Requests.RemoveAll(x => x.State == RequestState.Hidden && x.DeadlineDay < beforeDay);
            if (EnteredOn(beforeDay - 1))
            {
                S.Requests.RemoveAll(x => x.State == RequestState.Active && x.DeadlineDay < beforeDay);
                return;
            }
            foreach (var r in Active.Where(x => x.DeadlineDay < beforeDay).ToList()) Fail(r);
        }

        /// <summary>Requests due today whose period window already passed.</summary>
        public static void ExpirePassedWindows()
        {
            if (EnteredOn(S.Day)) return;
            var now = G.CurrentMask;
            PeriodMask remaining;
            if (now == PeriodMask.Lunch) remaining = PeriodMask.Any;
            else if (now == PeriodMask.Club) remaining = PeriodMask.Club | PeriodMask.After;
            else if (now == PeriodMask.After) remaining = PeriodMask.After;
            else
            {
                // Before lunch nothing has passed yet; after school everything due today is over.
                var p = G.Period;
                if (p.HasValue && (p.Value == Cycle.Type.GotoMyHouse || p.Value == Cycle.Type.MyHouse))
                    foreach (var r in Active.Where(x => x.DeadlineDay <= S.Day && x.Kind != RequestKind.Intimate).ToList()) Fail(r);
                return;
            }
            foreach (var r in Active.Where(x => x.DeadlineDay == S.Day && (x.Periods & remaining) == 0).ToList()) Fail(r);
        }

        private const string BadMoodWhy = " is in a bad mood. Cheer her up:";
        private const string TreatWhy = " could use a treat:";

        /// <summary>A gift request of hers is open (board or a favor she asked): one at a time.</summary>
        public static bool HasGiftOpen(SaveData.Heroine h)
        {
            return h != null && OpenFor(GirlInfo.Key(h)).Any(q => q.Kind == RequestKind.Gift);
        }

        /// <summary>
        /// The request's text as of now: the reason written when it was posted follows her mood of this moment.
        /// </summary>
        public static string TextOf(Request r)
        {
            var t = r.Text ?? "";
            if (r.Kind != RequestKind.Gift || string.IsNullOrEmpty(r.Girl)) return t;
            var upset = S.Girl(r.Girl).Mood == Mood.Upset;
            if (!upset && t.Contains(BadMoodWhy)) return t.Replace(BadMoodWhy, TreatWhy);
            if (upset && t.Contains(TreatWhy)) return t.Replace(TreatWhy, BadMoodWhy);
            return t;
        }

        public static string HowTo(Request r)
        {
            switch (r.Kind)
            {
                case RequestKind.Talk: return r.Source == "arc" ? "Talk to her and pick '" + ButtonLabel(r) + "'." : "Just talk to her.";
                case RequestKind.Lunch: return "Invite her: lunch (lunch break).";
                case RequestKind.Study: return "Invite her: study together.";
                case RequestKind.Exercise: return "Invite her: exercise together.";
                case RequestKind.Club: return "Invite her: club activities (club time).";
                case RequestKind.WalkHome: return "Invite her: go home together (after class).";
                case RequestKind.Gift: return "Talk to her and pick 'Give a gift'" + (string.IsNullOrEmpty(r.Item) ? "." : " (" + GirlInfo.LikeLabel(r.Item) + ").");
                case RequestKind.Intimate: return "H with her (not Free H).";
                case RequestKind.MeetAt: return "Talk to her at the " + Places.Name(r.Place) + (r.Source == "arc" ? " and pick '" + ButtonLabel(r) + "'." : ".");
                case RequestKind.Errand: return r.Progress > 0 ? "Done. Now talk to her" + (r.Source == "arc" ? " and pick '" + ButtonLabel(r) + "'." : ".") : "Go to the " + Places.Name(r.Place) + ".";
                case RequestKind.Activity: return "Do '" + (Activities.Get(r.Activity) != null ? Activities.Get(r.Activity).Name : r.Activity) + "'.";
                default: return "";
            }
        }

        public static string WindowLabel(PeriodMask p)
        {
            if (p == PeriodMask.Any) return "any time";
            var parts = new List<string>();
            if ((p & PeriodMask.Lunch) != 0) parts.Add("lunch");
            if ((p & PeriodMask.Club) != 0) parts.Add("club time");
            if ((p & PeriodMask.After) != 0) parts.Add("after class");
            return string.Join(" / ", parts.ToArray());
        }
    }
}
