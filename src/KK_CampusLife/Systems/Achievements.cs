using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// tarefas, para adicionar mais motivações para os jogadores que quiserem platinar"). Each one is a record (the day it
    /// fell and with whom, in the Album tab of F7) and a task: its goal and progress always show ("3/10"), the secret
    /// ones show a hint instead of their goal, and the album counts "N of M". Most read what the mod already keeps
    /// (lore, incidents, school events, harem, reputation); the rest are counters the scenes bump (Count).
    /// <para>Stages: one
    /// achievement climbs through its stages, each with its own name and goal ("Familiar Face", 3/4, "Talk with 10
    /// different girls", next: 20). Each story heroine and each teacher has one whose stages are her story's
    /// interactions, named after the one just reached. Every stage is a record of its own and pops its own card.</para>
    /// </summary>
    internal static class Achievements
    {
        internal sealed class Stage
        {
            public int At;               // the value it falls at; 0: the Def's Goal2 ("every girl")
            public string Name, Goal;
        }

        internal sealed class Def
        {
            public string Id, Cat, Name, Goal, Hint;
            public int Target = 1;
            public int Tier = 1;         // 1 simple, 2 specific (a situation to set up), 3 a bit of grinding
            public bool Secret;
            public bool Roster;          // "all of them": the record keeps how many there were (and if only the game's own)
            public string Mod;           // another mod it is about (Mods.Has): without it, it does not show
            public Func<int> Progress;   // null: the counter with this id
            public Func<int> Goal2;      // a target that depends on the school (every girl, every story)
            public Stage[] Stages;       // null: a single one (Name, Goal, Target); else Name is the whole climb's
            public SaveData.Heroine Girl; // the story heroine or teacher it is about (it falls "with" her)
        }

        public static readonly string[] Cats = { "Friendship", "Romance", "Campus", "Club", "Stories", "Trouble", "Extras", "Secrets" };
        public static readonly string[] Tiers = { "", "Getting started", "Special moments", "The long run" };

        private static CampusState S { get { return Campus.S; } }

        private static int C(string key) { int v; return S != null && S.AchCount.TryGetValue(key, out v) ? v : 0; }

        private static Stage St(int at, string name, string goal) { return new Stage { At = at, Name = name, Goal = goal }; }

        /// <summary>Every student at school (regular ones and story heroines), not the teachers.</summary>
        private static int Students(Func<SaveData.Heroine, bool> f)
        {
            try { return G.Heroines.Count(h => h != null && !h.isTeacher && f(h)); }
            catch { return 0; }
        }

        private static int TeacherCount(Func<SaveData.Heroine, bool> f)
        {
            try { return Teachers.All.Count(f); }
            catch { return 0; }
        }

        private static int Stories(Func<SaveData.Heroine, bool> f)
        {
            try { return StoryCast.StoryGirls.Count(f); }
            catch { return 0; }
        }

        private static int MaxGirl(Func<GirlState, int> f)
        {
            try { return S.Girls.Values.Select(f).DefaultIfEmpty(0).Max(); }
            catch { return 0; }
        }

        private static int SumGirl(Func<GirlState, int> f)
        {
            try { return S.Girls.Values.Sum(f); }
            catch { return 0; }
        }

        private static int KissedGirls()
        {
            return S == null ? 0 : S.Lore.Where(f => f.Verb == "kissed" && f.Subject == Lore.Player).Select(f => f.Obj).Distinct().Count();
        }

        private static int LoreCount(string verb)
        {
            return S == null ? 0 : S.Lore.Count(f => f.Verb == verb && f.Subject == Lore.Player);
        }

        private static int SumH()
        {
            try { return G.Heroines.Where(h => h != null).Sum(h => h.hCount); }
            catch { return 0; }
        }

        private static int MaxH()
        {
            try { return G.Heroines.Where(h => h != null).Select(h => h.hCount).DefaultIfEmpty(0).Max(); }
            catch { return 0; }
        }

        /// <summary>Two students (not you) who did it, as far as the school's lore knows.</summary>
        private static int OthersH()
        {
            return S == null ? 0 : S.Lore.Count(f => f.Verb == "had_h" && f.Subject != Lore.Player && f.Obj != Lore.Player);
        }

        /// <summary>Your girlfriends who are also with someone else: in a couple, or kissing or sleeping with another.</summary>
        private static int CheatedOn()
        {
            try
            {
                var gfs = G.Heroines.Where(h => h != null && h.isGirlfriend).ToList();
                var n = gfs.Count(h => { var p = Pairs.PairOf(h); return p != null && (p.Stage == PairStage.Couple || p.Stage == PairStage.Strained); });
                var keys = new HashSet<string>(gfs.Select(GirlInfo.Key));
                n += S.Lore.Where(f => (f.Verb == "had_h" || f.Verb == "kissed" || f.Verb == "cheated") && f.Subject != Lore.Player && f.Obj != Lore.Player && (keys.Contains(f.Subject) || keys.Contains(f.Obj)))
                    .Select(f => keys.Contains(f.Subject) ? f.Subject : f.Obj).Distinct().Count();
                return n;
            }
            catch { return 0; }
        }

        private static int ClubContents()
        {
            try { return Singleton<Manager.Game>.Instance.saveData.clubContents.Values.Sum(x => x != null ? x.Count : 0); }
            catch { return 0; }
        }

        private static int SchoolEvents() { return Calendar.EventIds.Count(id => S.EventLastWeek.ContainsKey(id)); }

        /// <summary>Characters in the save: all of them, and how many came with the game (story heroines, teachers).</summary>
        public static void Roster(out int all, out int game)
        {
            all = game = 0;
            try
            {
                all = G.Heroines.Count(h => h != null);
                game = G.Heroines.Count(h => h != null && h.fixCharaID != 0);
            }
            catch { }
        }

        // Inside each category: the simple ones first, then the specific ones, then the long run.
        // A climb sits in the step of its first stage.
        public static readonly List<Def> All = new List<Def>
        {
            // ---- Friendship ----
            new Def { Id = "first_talk", Cat = "Friendship", Tier = 1, Name = "Social Life", Progress = () => S.MetDay.Count, Stages = new[] {
                St(1, "Hello There", "Talk with a girl."),
                St(5, "Making the Rounds", "Talk with 5 different girls."),
                St(10, "Familiar Face", "Talk with 10 different girls."),
                St(20, "Everyone's Neighbor", "Talk with 20 different girls.") } },
            new Def { Id = "first_friend", Cat = "Friendship", Tier = 1, Name = "Friendship", Progress = () => Students(h => h.relation >= 1 || h.isGirlfriend), Stages = new[] {
                St(1, "A Friend", "Become friends with a girl."),
                St(3, "Circle of Friends", "Become friends with 3 girls."),
                St(5, "Inner Circle", "Become friends with 5 girls."),
                St(10, "Popular Kid", "Become friends with 10 girls.") } },
            new Def { Id = "duo:bestfriend", Cat = "Friendship", Tier = 2, Name = "Friend of a Friend", Goal = "Let a friend introduce you to her best friend." },
            new Def { Id = "settle:middle", Cat = "Friendship", Tier = 2, Name = "Peacemaker", Goal = "Two girls disagree: stay in the middle and settle it." },
            new Def { Id = "close", Cat = "Friendship", Tier = 2, Name = "Closeness", Progress = () => Students(h => h.relation >= 2 || h.isGirlfriend), Stages = new[] {
                St(1, "Close Friend", "Get close to a girl."),
                St(3, "Trusted", "Get close to 3 girls."),
                St(5, "Heart to Heart", "Get close to 5 girls."),
                St(10, "Everybody's Favorite", "Get close to 10 girls.") } },
            new Def { Id = "listen", Cat = "Friendship", Tier = 2, Name = "Listening", Progress = () => MaxGirl(g => g.Facts.Count), Stages = new[] {
                St(3, "Curious", "Learn 3 things about one girl from her own mouth."),
                St(5, "Good Listener", "Learn 5 things about one girl from her own mouth."),
                St(10, "Confidant", "Learn 10 things about one girl from her own mouth."),
                St(20, "Knows Her by Heart", "Learn 20 things about one girl from her own mouth.") } },
            new Def { Id = "trio_talks", Cat = "Friendship", Tier = 2, Name = "Three Together", Progress = () => C("trio:natural"), Stages = new[] {
                St(1, "Plus One", "Talk with two girls at once until they have to go."),
                St(3, "Party of Three", "Talk with two girls at once until they have to go, 3 times."),
                St(10, "Social Hub", "Talk with two girls at once until they have to go, 10 times.") } },
            new Def { Id = "all_friends", Cat = "Friendship", Tier = 3, Name = "Whole University", Goal = "Become friends with every student on campus.", Roster = true, Goal2 = () => Students(h => true), Progress = () => Students(h => h.relation >= 1 || h.isGirlfriend) },

            // ---- Romance ----
            new Def { Id = "held_hands", Cat = "Romance", Tier = 1, Name = "Hand in Hand", Progress = () => LoreCount("held_hands"), Stages = new[] {
                St(1, "Hand in Hand", "Hold a girl's hand."),
                St(5, "Warm Hands", "Hold hands 5 times."),
                St(20, "Never Let Go", "Hold hands 20 times.") } },
            new Def { Id = "first_kiss", Cat = "Romance", Tier = 1, Name = "First Kiss", Goal = "Kiss a girl.", Progress = KissedGirls },
            new Def { Id = "girlfriend", Cat = "Romance", Tier = 1, Name = "Going Steady", Goal = "Have a girlfriend.", Progress = () => Students(h => h.isGirlfriend) },
            new Def { Id = "kiss_wary", Cat = "Romance", Tier = 2, Name = "She Let You", Goal = "Kiss a girl who wasn't sure, and have her let you." },
            new Def { Id = "rain_kiss", Cat = "Romance", Tier = 2, Name = "Rain Date", Goal = "Kiss a girl while it rains." },
            new Def { Id = "first_night", Cat = "Romance", Tier = 2, Name = "Nights", Progress = () => Students(h => h.hCount > 0), Stages = new[] {
                St(1, "First Night", "Go all the way with a girl."),
                St(3, "Lady's Man", "Go all the way with 3 different girls."),
                St(10, "Casanova", "Go all the way with 10 different girls.") } },
            new Def { Id = "chapter_one", Cat = "Romance", Tier = 2, Name = "Your Story Together", Progress = () => MaxGirl(g => g.Chapter), Stages = new[] {
                St(1, "Next Chapter", "Finish a chapter of your story with your girlfriend."),
                St(2, "Turning Pages", "Finish two chapters with one girlfriend."),
                St(3, "Happily Ever After", "Finish all three chapters with one girlfriend.") } },
            new Def { Id = "h:outdoor", Cat = "Romance", Tier = 2, Name = "Fresh Air", Goal = "Go all the way outdoors." },
            new Def { Id = "h_one", Cat = "Romance", Tier = 3, Name = "Made for Each Other", Progress = MaxH, Stages = new[] {
                St(5, "Familiar Touch", "Go all the way 5 times with the same girl."),
                St(15, "Second Nature", "Go all the way 15 times with the same girl."),
                St(40, "Made for Each Other", "Go all the way 40 times with the same girl.") } },
            new Def { Id = "h_total", Cat = "Romance", Tier = 3, Name = "Passion", Progress = SumH, Stages = new[] {
                St(5, "Regulars", "Go all the way 5 times."),
                St(25, "Insatiable", "Go all the way 25 times."),
                St(75, "Campus Legend", "Go all the way 75 times.") } },
            new Def { Id = "harem", Cat = "Romance", Tier = 3, Name = "Shared Heart", Goal = "Have two girlfriends who agreed to share you.", Target = 2, Progress = () => S.Harem.Count },
            new Def { Id = "all_dates", Cat = "Romance", Tier = 3, Name = "Everyone's Sweetheart", Goal = "Date every student on campus.", Roster = true, Goal2 = () => Students(h => true), Progress = () => Students(h => h.isGirlfriend) },

            // ---- Campus ----
            new Def { Id = "school_days", Cat = "Campus", Tier = 1, Name = "College Days", Progress = () => S.Day, Stages = new[] {
                St(7, "First Week", "Spend 7 days on campus."),
                St(30, "Regular", "Spend 30 days on campus."),
                St(60, "Old Hand", "Spend 60 days on campus."),
                St(100, "Senpai", "Spend 100 days on campus.") } },
            new Def { Id = "festival", Cat = "Campus", Tier = 2, Name = "Campus Events", Progress = SchoolEvents, Stages = new[] {
                St(1, "Festival Goer", "Live through a college event (exams, sports festival, culture festival or field trip)."),
                St(2, "Event Regular", "Live through 2 different campus events."),
                St(4, "Season Pass", "Live through the exams, sports festival, culture festival and field trip.") } },
            new Def { Id = "honor", Cat = "Campus", Tier = 2, Name = "Honor Roll", Goal = "Score 80 or more on an exam.", Progress = () => S.ExamScores.Any(x => x >= 80) ? 1 : 0 },
            new Def { Id = "routine", Cat = "Campus", Tier = 2, Name = "Everyday Life", Progress = () => C("duo:lunch") + C("duo:study") + C("duo:club") + C("duo:season"), Stages = new[] {
                St(1, "Lunch Buddy", "Have lunch, study or go to club with two friends."),
                St(5, "Everyday Life", "Have lunch, study or go to club with two friends, 5 times."),
                St(15, "Inseparable", "Have lunch, study or go to club with two friends, 15 times.") } },
            new Def { Id = "reputation", Cat = "Campus", Tier = 3, Name = "Reputation", Progress = () => S.Reputation, Stages = new[] {
                St(30, "Noticed", "Reach a reputation of 30."),
                St(50, "Well Known", "Reach a reputation of 50."),
                St(70, "Popular", "Reach a reputation of 70."),
                St(90, "Campus Icon", "Reach a reputation of 90.") } },

            // ---- Club (the game's own club) ----
            new Def { Id = "club_member", Cat = "Club", Tier = 1, Name = "Club Members", Progress = () => Students(h => h.isStaff), Stages = new[] {
                St(1, "Recruiter", "Bring a girl into the club."),
                St(5, "Full House", "Have 5 girls in the club."),
                St(10, "Club Legend", "Have 10 girls in the club.") } },
            new Def { Id = "club_items", Cat = "Club", Tier = 2, Name = "Club Room", Progress = ClubContents, Stages = new[] {
                St(1, "First Touch", "Unlock something for the club."),
                St(5, "Decorator", "Unlock 5 things for the club."),
                St(15, "Interior Designer", "Unlock 15 things for the club.") } },
            new Def { Id = "club_stories", Cat = "Club", Tier = 2, Name = "Club Chronicle", Progress = () => S.ClubHistory.Count, Stages = new[] {
                St(1, "Club Diary", "Live a story of the club."),
                St(3, "Club Chronicle", "Live 3 stories of the club."),
                St(6, "Club History", "Live 6 stories of the club.") } },

            // ---- Stories (the game's heroines and teachers; each one also has her own climb, see Dynamic) ----
            new Def { Id = "story_met", Cat = "Stories", Tier = 1, Name = "Main Cast", Goal = "Meet one of the story heroines.", Progress = () => Stories(h => S.MetDay.ContainsKey(GirlInfo.Key(h)) || h.talkEvent.Contains(0)) },
            new Def { Id = "teacher_met", Cat = "Stories", Tier = 1, Name = "Office Hours", Goal = "Talk with a professor.", Progress = () => TeacherCount(Teachers.Met) },
            new Def { Id = "route_one", Cat = "Stories", Tier = 2, Name = "Story Routes", Goal2 = () => Stories(h => true), Progress = () => Stories(StoryCast.StoryOver), Stages = new[] {
                St(1, "The End?", "Finish a story heroine's route."),
                St(0, "Completionist", "Finish every story heroine's route.") } },
            new Def { Id = "teacher_done", Cat = "Stories", Tier = 2, Name = "Faculty", Goal2 = () => TeacherCount(h => true), Progress = () => TeacherCount(Teachers.EventOver), Stages = new[] {
                St(1, "Professor's Pet", "See a professor's story to its end."),
                St(0, "Faculty Favorite", "See every professor's story to its end.") } },
            new Def { Id = "teacher_h", Cat = "Stories", Tier = 2, Name = "Extra Lessons", Goal = "Go all the way with a professor.", Progress = () => TeacherCount(h => h.hCount > 0) },

            // ---- Trouble (they only record; nothing is taken from you) ----
            new Def { Id = "seen", Cat = "Trouble", Tier = 1, Name = "Gossip", Progress = () => S.Incidents.Count, Stages = new[] {
                St(1, "Seen", "Get seen doing something you shouldn't."),
                St(5, "Rumor Mill", "Be part of 5 things the university talks about."),
                St(10, "Talk of the university", "Be part of 10 things the university talks about.") } },
            new Def { Id = "duo:caught", Cat = "Trouble", Tier = 2, Name = "Caught Out", Goal = "Get confronted by your girlfriend about another girl." },
            new Def { Id = "walked_off", Cat = "Trouble", Tier = 2, Name = "Third Wheel", Progress = () => C("trio:aleft") + C("trio:bleft"), Stages = new[] {
                St(1, "Third Wheel", "Make a girl walk off while you talk with her and a friend."),
                St(3, "Crowd Control", "Make a girl walk off on you, 3 times.") } },
            new Def { Id = "settle:side", Cat = "Trouble", Tier = 2, Name = "Picking Sides", Progress = () => C("settle:a") + C("settle:b"), Stages = new[] {
                St(1, "Picking Sides", "Two girls disagree: side with one of them."),
                St(3, "Partisan", "Side with one girl in 3 disagreements.") } },
            new Def { Id = "h:cheat", Cat = "Trouble", Tier = 2, Name = "Two-Timer", Stages = new[] {
                St(1, "Two-Timer", "Go all the way with a girl while another girl is your girlfriend."),
                St(5, "Serial Cheater", "Cheat on your girlfriend 5 times.") } },
            new Def { Id = "h:taken", Cat = "Trouble", Tier = 2, Name = "Homewrecker", Stages = new[] {
                St(1, "Homewrecker", "Go all the way with a girl who is with someone else."),
                St(5, "Forbidden Fruit", "Do it 5 times with girls who are taken.") } },
            new Def { Id = "cheated_on", Cat = "Trouble", Tier = 2, Name = "Not the Only One", Goal = "Find out your girlfriend is with someone else.", Progress = CheatedOn },
            new Def { Id = "h:seen", Cat = "Trouble", Tier = 2, Name = "Exhibition", Stages = new[] {
                St(1, "Exhibition", "Get seen while going all the way."),
                St(5, "No Shame", "Get seen 5 times while going all the way.") } },
            new Def { Id = "broke_up", Cat = "Trouble", Tier = 2, Name = "It's Over", Goal = "Break up with a girlfriend.", Progress = () => LoreCount("broke_up") },
            new Def { Id = "walked_in", Cat = "Trouble", Tier = 3, Name = "Wrong Door", Progress = () => SumGirl(g => g.Caught), Stages = new[] {
                St(1, "Oops", "Walk in on a girl with someone else."),
                St(3, "Wrong Door", "Walk in on a girl with someone else, 3 times.") } },

            // ---- Extras (other mods: each one shows only with its mod) ----
            new Def { Id = "fairy_met", Cat = "Extras", Tier = 1, Mod = Mods.PantyFairy, Name = "Panty Fairy", Progress = Mods.FairyStage, Stages = new[] {
                St(1, "A Fairy's Favor", "Meet the Panty Fairy."),
                St(2, "Fairy Tale Ending", "Finish the Panty Fairy's story.") } },
            new Def { Id = "crest_one", Cat = "Extras", Tier = 2, Mod = Mods.LewdCrestX, Name = "Crests", Progress = () => Students(h => Mods.Crest(h) != ""), Stages = new[] {
                St(1, "Marked", "A girl on campus wears a crest."),
                St(5, "Crest Collector", "Five girls on campus wear a crest.") } },
            new Def { Id = "pregnant", Cat = "Extras", Tier = 2, Mod = Mods.Pregnancy, Name = "Family Way", Goal = "A girl gets pregnant.", Progress = () => Students(Mods.EverPregnant) },
            new Def { Id = "fairy_pairs", Cat = "Extras", Tier = 3, Mod = Mods.PantyFairy, Name = "The Fairy's Collection", Progress = Mods.FairyHaul, Stages = new[] {
                St(5, "Pickpocket", "Take 5 pairs for the fairy."),
                St(20, "Lingerie Thief", "Take 20 pairs for the fairy."),
                St(50, "Fairy's Champion", "Take 50 pairs for the fairy.") } },

            // ---- Secrets (the goal shows once it falls; until then, a hint) ----
            new Def { Id = "trio_kiss", Cat = "Secrets", Tier = 2, Name = "Audience", Secret = true, Goal = "Kiss a girl right in front of her friend.", Hint = "A kiss is louder with someone watching." },
            new Def { Id = "duo:trisal", Cat = "Secrets", Tier = 2, Name = "Two Hearts", Secret = true, Goal = "Both your girlfriends come to see you together.", Hint = "When two hearts agree, they come as one." },
            new Def { Id = "trio:h3", Cat = "Secrets", Tier = 2, Name = "Three's Company", Secret = true, Goal = "Go somewhere private with two girls at once.", Hint = "Some invitations work better for three." },
            new Def { Id = "diplomat", Cat = "Secrets", Tier = 3, Name = "Diplomat", Secret = true, Goal = "Settle 3 disagreements without picking a side.", Target = 3, Progress = () => C("settle:middle") + C("settle:change"), Hint = "Sometimes nobody has to win." },
            new Def { Id = "others_h", Cat = "Secrets", Tier = 2, Name = "Thin Walls", Secret = true, Progress = OthersH, Hint = "Walls are thin, and students talk.", Stages = new[] {
                St(1, "Thin Walls", "Learn that two students went all the way."),
                St(5, "Campus Heat", "Learn of 5 times students went all the way."),
                St(15, "Everyone's Doing It", "Learn of 15 times students went all the way.") } },
            new Def { Id = "h:rain", Cat = "Secrets", Tier = 2, Name = "Singing in the Rain", Secret = true, Goal = "Go all the way outdoors in the rain.", Hint = "Some don't run from the rain." },
            new Def { Id = "kissed_five", Cat = "Secrets", Tier = 3, Name = "Heartbreaker", Secret = true, Goal = "Kiss 5 different girls.", Target = 5, Progress = KissedGirls, Hint = "Count the lips, not the days." },
        };

        // ================= each story heroine's and teacher's own climb =================

        // The name of her whole climb, then one name per interaction of her story; the last name is always her ending.
        private static readonly Dictionary<int, string[]> StoryNames = new Dictionary<int, string[]>
        {
            { -5, new[] { "Idol Conqueror", "The Idol Next Door", "Autograph", "Off Stage", "Rehearsal", "Behind the Curtain", "Encore", "Duet", "Her Biggest Fan" } },
            { -8, new[] { "Senpai Material", "The New Girl", "Lost in the Halls", "Bento for Two", "Study Buddy", "A Little Courage", "Brave Words", "Hand in Hand", "Her Senpai" } },
            { -9, new[] { "Rebel Heart", "Trouble on the Rooftop", "Skipping Class", "The Fight", "Bandages", "Soft Side", "Leaning on You", "No More Fights", "Her Knight" } },
            { -10, new[] { "Classic Romance", "The Girl on the Rooftop", "Class Rep's Request", "After Class", "The Walk Home", "Letters", "Confession", "A Promise", "Ever After" } },
        };

        private static readonly string[] AnyStory = { "", "First Meeting", "Getting Closer", "Her Secret", "Just Us", "A Hard Day", "Opening Up", "Confession", "Her Ending" };

        private static readonly Stage[] TeacherStages =
        {
            St(1, "Office Hours", "Meet her."),
            St(2, "After Class", "Become her friend."),
            St(3, "Forbidden Lesson", "Become more than her student."),
            St(4, "Professor's Favorite", "Get truly close to her."),
            St(5, "Off the Clock", "See her story to its end: she joins the university as herself."),
        };

        private static readonly Dictionary<string, Def> DynDefs = new Dictionary<string, Def>();
        private static List<Def> _dyn = new List<Def>();
        private static float _dynAt = -10f;
        private static CampusState _dynFor;

        /// <summary>Her story's interactions, in order (the game's event schedule), or null outside the school.</summary>
        private static List<int> StoryEvents(SaveData.Heroine h)
        {
            try
            {
                var act = G.Act;
                List<FixEventSchedule.Param> list;
                if (act == null || act.Cycle == null || act.Cycle.fixEventSchedule == null) return null;
                if (!act.Cycle.fixEventSchedule.TryGetValue(h.fixCharaID, out list) || list == null || list.Count == 0) return null;
                var r = new List<int>();
                foreach (var p in list)
                {
                    int a;
                    if (int.TryParse(p.Asset, out a)) r.Add(a);
                }
                return r.Count > 0 ? r : null;
            }
            catch { return null; }
        }

        private static Def StoryDef(SaveData.Heroine h)
        {
            var id = "story:" + h.fixCharaID;
            var ev = StoryEvents(h);
            if (ev != null) S.AchCount["stages:" + id] = ev.Count;   // the school may be closed next time the album opens
            int n;
            if (!S.AchCount.TryGetValue("stages:" + id, out n) || n <= 0) return null;
            Def d;
            if (!DynDefs.TryGetValue(id, out d) || d.Stages.Length != n)
            {
                string[] names;
                if (!StoryNames.TryGetValue(h.fixCharaID, out names)) names = AnyStory;
                var first = GirlInfo.FirstName(h);
                var st = new Stage[n];
                for (var k = 1; k <= n; k++)
                {
                    var name = k == n ? names[names.Length - 1] : names[Mathf.Min(k, names.Length - 2)];
                    if (k < n && k > names.Length - 2) name += " " + (k - names.Length + 3);   // more interactions than names
                    st[k - 1] = St(k, name, k == n ? "See " + first + "'s story to its end." : "Part " + k + " of " + first + "'s story.");
                }
                d = new Def { Id = id, Cat = "Stories", Tier = 2, Name = names == AnyStory ? first + "'s Story" : names[0], Stages = st };
                DynDefs[id] = d;
            }
            d.Girl = h;
            d.Progress = () =>
            {
                if (h.isTaked) return d.Stages.Length;
                var e = StoryEvents(h);
                int seen;
                if (e == null) return S.AchCount.TryGetValue("seen:" + d.Id, out seen) ? seen : 0;
                seen = e.Count(a => h.talkEvent.Contains(a));
                S.AchCount["seen:" + d.Id] = seen;
                return seen;
            };
            return d;
        }

        private static Def TeacherDef(SaveData.Heroine h)
        {
            var id = "teacher:" + GirlInfo.Key(h);
            Def d;
            if (!DynDefs.TryGetValue(id, out d))
            {
                d = new Def { Id = id, Cat = "Stories", Tier = 3, Name = GirlInfo.FirstName(h) + "'s Lessons", Stages = TeacherStages };
                DynDefs[id] = d;
            }
            d.Girl = h;
            d.Progress = () => Teachers.EventOver(h) ? 5 : Teachers.Close(h) ? 4 : Teachers.HOpen(h) ? 3 : Teachers.Friend(h) ? 2 : Teachers.Met(h) ? 1 : 0;
            return d;
        }

        /// <summary>One climb per story heroine and per teacher in this save (rebuilt every few seconds at most).</summary>
        private static List<Def> Dynamic()
        {
            if (S == null) return new List<Def>();
            if (_dynFor == S && Time.realtimeSinceStartup - _dynAt < 3f) return _dyn;
            _dynFor = S;
            _dynAt = Time.realtimeSinceStartup;
            var r = new List<Def>();
            try
            {
                foreach (var h in StoryCast.StoryGirls.OrderByDescending(x => x.fixCharaID))
                {
                    var d = StoryDef(h);
                    if (d != null) r.Add(d);
                }
                foreach (var h in Teachers.All) r.Add(TeacherDef(h));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Achievements (stories): " + e.Message); }
            _dyn = r;
            return r;
        }

        /// <summary>The ones that show in this game (their mod, if any, is loaded), with each story's own climb.</summary>
        public static IEnumerable<Def> Shown { get { return All.Where(d => Mods.Has(d.Mod)).Concat(Dynamic()); } }

        /// <summary>Every stage that shows (a single achievement is one stage).</summary>
        public static int ShownCount { get { return Shown.Sum(d => StageCount(d)); } }

        /// <summary>The categories with anything to show.</summary>
        public static IEnumerable<string> ShownCats { get { return Cats.Where(c => Shown.Any(d => d.Cat == c)); } }

        /// <summary>Its goal; for "every girl", how many there are now (at least 1).</summary>
        public static int TargetOf(Def d)
        {
            if (d.Goal2 == null) return d.Target;
            try { return Mathf.Max(1, d.Goal2()); }
            catch { return 1; }
        }

        public static int StageCount(Def d) { return d.Stages == null ? 1 : d.Stages.Length; }

        /// <summary>Stage k (1-based); a single achievement is its own only stage.</summary>
        public static Stage StageAt(Def d, int k)
        {
            if (d.Stages == null) return new Stage { At = TargetOf(d), Name = d.Name, Goal = d.Goal };
            k = Mathf.Clamp(k, 1, d.Stages.Length);
            var s = d.Stages[k - 1];
            if (s.At > 0) return s;
            // "Every girl": always past the stage before it, even in a school of one.
            var prev = k > 1 ? StageAt(d, k - 1).At + 1 : 1;
            return new Stage { At = Mathf.Max(TargetOf(d), prev), Name = s.Name, Goal = s.Goal };
        }

        /// <summary>The record's id of stage k: the first stage keeps the plain id (so old records still count).</summary>
        private static string Key(Def d, int k) { return k <= 1 ? d.Id : d.Id + "#" + k; }

        public static Def Find(string id) { return All.Concat(Dynamic()).FirstOrDefault(d => d.Id == id); }

        private static bool Has(string key) { return S != null && S.AchDone.Any(x => x.StartsWith(key + "|", StringComparison.Ordinal)); }

        /// <summary>How many stages fell (0 none).</summary>
        public static int Reached(Def d)
        {
            for (var k = StageCount(d); k >= 1; k--)
                if (Has(Key(d, k))) return k;
            return 0;
        }

        /// <summary>Every stage fell.</summary>
        public static bool Complete(Def d) { return Reached(d) >= StageCount(d); }

        public static bool Done(string id) { return Has(id); }

        public static int Value(Def d)
        {
            if (S == null) return 0;
            try { return Mathf.Max(0, d.Progress != null ? d.Progress() : C(d.Id)); }
            catch { return 0; }
        }

        /// <summary>"id|day|girl key" of a fallen stage (the highest one by default), or null.</summary>
        public static string[] Record(Def d, int k = 0)
        {
            if (k <= 0) k = Reached(d);
            if (S == null || k <= 0) return null;
            var key = Key(d, k) + "|";
            var r = S.AchDone.FirstOrDefault(x => x.StartsWith(key, StringComparison.Ordinal));
            return r == null ? null : r.Split(new[] { '|' }, 3);   // the girl's key has '|' of its own ("c:last|first|...")
        }

        /// <summary>Stages that fell, out of ShownCount.</summary>
        public static int DoneCount { get { return S == null ? 0 : Shown.Sum(d => Reached(d)); } }

        /// <summary>Achievements with every stage done.</summary>
        public static int CompleteCount { get { return S == null ? 0 : Shown.Count(Complete); } }

        /// <summary>A scene says something happened (with a girl, if any): its counter goes up, and anything due falls.</summary>
        public static void Count(string key, SaveData.Heroine with = null, int add = 1)
        {
            try
            {
                if (S == null || !Campus.On) return;
                int v;
                S.AchCount.TryGetValue(key, out v);
                S.AchCount[key] = v + add;
                Check(with);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Achievement count " + key + ": " + e.Message); }
        }

        /// <summary>
        /// taken, outdoors, in the rain. Being seen comes from Lore.FromH.
        /// </summary>
        public static void AfterH(SaveData.Heroine h, List<SaveData.Heroine> all)
        {
            try
            {
                if (h == null) return;
                if (G.Heroines.Any(x => x != null && x.isGirlfriend && x != h && (all == null || !all.Contains(x)))) Count("h:cheat", h);
                var p = Pairs.PairOf(h);
                if (p != null && (p.Stage == PairStage.Couple || p.Stage == PairStage.Strained || p.Stage == PairStage.Talking)) Count("h:taken", h);
                if (Weather.Outdoor(G.PlayerMapNo))
                {
                    Count("h:outdoor", h);
                    if (Weather.Raining) Count("h:rain", h);
                }
                Check(h);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Achievements after H: " + e.Message); }
        }

        /// <summary>A kiss (touch or option): first kiss, the wary one, the rain.</summary>
        public static void Kissed(SaveData.Heroine h, bool wary)
        {
            if (wary) Count("kiss_wary", h, 1);
            if (Weather.Raining) Count("rain_kiss", h, 1);
            Check(h);
        }

        private static float _nextTick;

        /// <summary>About every 5 s: the ones that read the save fall on their own.</summary>
        public static void Tick()
        {
            if (S == null || !Campus.On || Time.realtimeSinceStartup < _nextTick) return;
            _nextTick = Time.realtimeSinceStartup + 5f;
            try { Check(null); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Achievements: " + e.Message); }
        }

        private static readonly Dictionary<string, KeyValuePair<string, int>> Moved = new Dictionary<string, KeyValuePair<string, int>>
        {
            { "familiar_face", new KeyValuePair<string, int>("first_talk", 3) },
            { "inner_circle", new KeyValuePair<string, int>("first_friend", 3) },
            { "close_ten", new KeyValuePair<string, int>("close", 4) },
            { "listener", new KeyValuePair<string, int>("listen", 2) },
            { "trio:natural", new KeyValuePair<string, int>("trio_talks", 2) },
            { "chapter_all", new KeyValuePair<string, int>("chapter_one", 3) },
            { "first_week", new KeyValuePair<string, int>("school_days", 1) },
            { "days", new KeyValuePair<string, int>("school_days", 2) },
            { "days_100", new KeyValuePair<string, int>("school_days", 4) },
            { "season_pass", new KeyValuePair<string, int>("festival", 3) },
            { "everyday", new KeyValuePair<string, int>("routine", 2) },
            { "popular", new KeyValuePair<string, int>("reputation", 3) },
            { "club_five", new KeyValuePair<string, int>("club_member", 2) },
            { "club_ten", new KeyValuePair<string, int>("club_member", 3) },
            { "club_room", new KeyValuePair<string, int>("club_items", 2) },
            { "club_story", new KeyValuePair<string, int>("club_stories", 2) },
            { "routes_all", new KeyValuePair<string, int>("route_one", 2) },
            { "teachers_all", new KeyValuePair<string, int>("teacher_done", 2) },
            { "talk_of_school", new KeyValuePair<string, int>("seen", 3) },
            { "wrong_door", new KeyValuePair<string, int>("walked_in", 2) },
            { "crest_five", new KeyValuePair<string, int>("crest_one", 2) },
            { "fairy_done", new KeyValuePair<string, int>("fairy_met", 2) },
            { "fairy_haul", new KeyValuePair<string, int>("fairy_pairs", 2) },
        };

        private static CampusState _migrated;

        private static void Migrate()
        {
            if (_migrated == S) return;
            _migrated = S;
            var moved = 0;
            for (var i = 0; i < S.AchDone.Count; i++)
            {
                var rec = S.AchDone[i];
                var bar = rec.IndexOf('|');
                KeyValuePair<string, int> to;
                if (bar <= 0 || !Moved.TryGetValue(rec.Substring(0, bar), out to)) continue;
                var key = to.Value <= 1 ? to.Key : to.Key + "#" + to.Value;
                if (Has(key)) { S.AchDone.RemoveAt(i--); continue; }
                S.AchDone[i] = key + rec.Substring(bar);
                moved++;
            }
            if (moved > 0) CampusLifePlugin.Log.LogInfo("Achievements: " + moved + " old records became stages");
        }

        private static void Check(SaveData.Heroine with)
        {
            Migrate();
            var due = new List<KeyValuePair<Def, int>>();
            foreach (var d in Shown)
            {
                var n = StageCount(d);
                var v = Value(d);
                for (var k = Reached(d) + 1; k <= n && v >= StageAt(d, k).At; k++) due.Add(new KeyValuePair<Def, int>(d, k));
            }
            if (due.Count == 0) return;
            // An old save opening the album for the first time: one card for the whole batch, not a wall of them.
            var quiet = due.Count > 2;
            foreach (var x in due) Unlock(x.Key, x.Value, quiet ? null : with ?? x.Key.Girl, quiet);
            if (quiet) AchievementPopup.Show(due.Count + " achievements unlocked", "See the Album in F7", DoneCount, ShownCount);
        }

        private static void Unlock(Def d, int k, SaveData.Heroine with, bool quiet = false)
        {
            var n = StageCount(d);
            S.AchDone.Add(Key(d, k) + "|" + S.Day + "|" + (with != null ? GirlInfo.Key(with) : ""));
            if (d.Roster && k >= n)
            {
                // "Every girl": it falls with whoever is at school, and the album says how many that was.
                int all, game;
                Roster(out all, out game);
                S.AchCount["roster:" + d.Id] = TargetOf(d);
                S.AchCount["rosterAll:" + d.Id] = all;
                S.AchCount["rosterGame:" + d.Id] = game;
            }
            var st = StageAt(d, k);
            var done = DoneCount;
            var total = ShownCount;
            var withText = with != null ? "with " + GirlInfo.FirstName(with) : "";
            var sub = n > 1 ? d.Name + "  " + k + "/" + n + (withText != "" ? ", " + withText : "") : withText != "" ? withText : st.Goal;
            if (!quiet) AchievementPopup.Show(st.Name, sub, done, total);
            S.Log("Achievement: " + st.Name + (n > 1 ? " (" + d.Name + " " + k + "/" + n + ")" : "") + (with != null ? ", with " + GirlInfo.FirstName(with) : "") + ".");
            CampusLifePlugin.Log.LogInfo("Achievement " + Key(d, k) + " '" + st.Name + "' day " + S.Day + (with != null ? " with " + GirlInfo.FirstName(with) : "") + " (" + done + "/" + total + ")");
        }

        public static bool Force(string id)
        {
            var d = Find(id);
            if (d == null || S == null) return false;
            var k = Reached(d) + 1;
            if (k > StageCount(d)) return false;
            Unlock(d, k, d.Girl);
            return true;
        }

        public static string Describe()
        {
            if (S == null) return "achievements: no save";
            int all, game;
            Roster(out all, out game);
            return "achievements " + CompleteCount + "/" + Shown.Count() + " complete, stages " + DoneCount + "/" + ShownCount + " (of " + All.Count + " + " + Dynamic().Count + " stories; characters " + all + ", game's " + game + "; " + Mods.Describe() + "): "
                + string.Join(", ", Shown.Select(d =>
                {
                    var r = Reached(d);
                    var n = StageCount(d);
                    var next = r < n ? " " + Value(d) + "/" + StageAt(d, r + 1).At : " DONE";
                    return d.Id + "[" + d.Tier + "]" + (n > 1 ? " " + r + "/" + n + " '" + StageAt(d, Mathf.Max(1, r)).Name + "'" : "") + next;
                }).ToArray());
        }
    }

    internal sealed partial class CampusState
    {
        public List<string> AchDone = new List<string>();                         // "id|day|girl" (the album); a climb's stage k: "id#k|day|girl"
        public Dictionary<string, int> AchCount = new Dictionary<string, int>(); // what the scenes counted

        private void WriteAch(Dictionary<string, object> root)
        {
            root["achD"] = AchDone.Cast<object>().ToList();
            root["achC"] = JsonLists.IntDict(AchCount);
        }

        private void ReadAch(Dictionary<string, object> d)
        {
            AchDone = JsonLists.Strings(d.GetList("achD"));
            AchCount = JsonLists.ReadIntDict(d.GetObj("achC"));
        }
    }
}
