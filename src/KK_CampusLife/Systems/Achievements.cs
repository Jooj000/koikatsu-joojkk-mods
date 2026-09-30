using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// tarefas, para adicionar mais motivações para os jogadores que quiserem platinar"). Each one is a record (the day it
    /// fell and with whom, in the Album tab of F7) and a task: its goal and progress always show ("3/10"), the secret
    /// ones show a hint instead of their goal, and the album counts "N of M". Most read what the mod already keeps
    /// (lore, incidents, school events, harem, reputation); the rest are counters the scenes bump (Count).
    /// </summary>
    internal static class Achievements
    {
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
        }

        public static readonly string[] Cats = { "Friendship", "Romance", "Campus", "Club", "Stories", "Trouble", "Extras", "Secrets" };
        public static readonly string[] Tiers = { "", "Getting started", "Special moments", "The long run" };

        private static CampusState S { get { return Campus.S; } }

        private static int C(string key) { int v; return S != null && S.AchCount.TryGetValue(key, out v) ? v : 0; }

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
        public static readonly List<Def> All = new List<Def>
        {
            // ---- Friendship ----
            new Def { Id = "first_talk", Cat = "Friendship", Tier = 1, Name = "Hello There", Goal = "Talk with a girl.", Progress = () => S.MetDay.Count },
            new Def { Id = "first_friend", Cat = "Friendship", Tier = 1, Name = "A Friend", Goal = "Become friends with a girl.", Progress = () => Students(h => h.relation >= 1 || h.isGirlfriend) },
            new Def { Id = "duo:bestfriend", Cat = "Friendship", Tier = 2, Name = "Friend of a Friend", Goal = "Let a friend introduce you to her best friend." },
            new Def { Id = "settle:middle", Cat = "Friendship", Tier = 2, Name = "Peacemaker", Goal = "Two girls disagree: stay in the middle and settle it." },
            new Def { Id = "listener", Cat = "Friendship", Tier = 2, Name = "Good Listener", Goal = "Learn 5 things about one girl from her own mouth.", Target = 5, Progress = () => MaxGirl(g => g.Facts.Count) },
            new Def { Id = "trio:natural", Cat = "Friendship", Tier = 3, Name = "Party of Three", Goal = "Talk with two girls at once until they have to go, 3 times.", Target = 3 },
            new Def { Id = "familiar_face", Cat = "Friendship", Tier = 3, Name = "Familiar Face", Goal = "Talk with 10 different girls.", Target = 10, Progress = () => S.MetDay.Count },
            new Def { Id = "inner_circle", Cat = "Friendship", Tier = 3, Name = "Inner Circle", Goal = "Become friends with 5 girls.", Target = 5, Progress = () => Students(h => h.relation >= 1 || h.isGirlfriend) },
            new Def { Id = "close_ten", Cat = "Friendship", Tier = 3, Name = "Everybody's Favorite", Goal = "Get close to 10 girls.", Target = 10, Progress = () => Students(h => h.relation >= 2 || h.isGirlfriend) },
            new Def { Id = "all_friends", Cat = "Friendship", Tier = 3, Name = "Whole School", Goal = "Become friends with every student at school.", Roster = true, Goal2 = () => Students(h => true), Progress = () => Students(h => h.relation >= 1 || h.isGirlfriend) },

            // ---- Romance ----
            new Def { Id = "held_hands", Cat = "Romance", Tier = 1, Name = "Hand in Hand", Goal = "Hold a girl's hand.", Progress = () => LoreCount("held_hands") },
            new Def { Id = "first_kiss", Cat = "Romance", Tier = 1, Name = "First Kiss", Goal = "Kiss a girl.", Progress = KissedGirls },
            new Def { Id = "girlfriend", Cat = "Romance", Tier = 1, Name = "Going Steady", Goal = "Have a girlfriend.", Progress = () => Students(h => h.isGirlfriend) },
            new Def { Id = "kiss_wary", Cat = "Romance", Tier = 2, Name = "She Let You", Goal = "Kiss a girl who wasn't sure, and have her let you." },
            new Def { Id = "rain_kiss", Cat = "Romance", Tier = 2, Name = "Rain Date", Goal = "Kiss a girl while it rains." },
            new Def { Id = "first_night", Cat = "Romance", Tier = 2, Name = "First Night", Goal = "Go all the way with a girl.", Progress = () => Students(h => h.hCount > 0) },
            new Def { Id = "chapter_one", Cat = "Romance", Tier = 2, Name = "Next Chapter", Goal = "Finish a chapter of your story with your girlfriend.", Progress = () => MaxGirl(g => g.Chapter) },
            new Def { Id = "harem", Cat = "Romance", Tier = 3, Name = "Shared Heart", Goal = "Have two girlfriends who agreed to share you.", Target = 2, Progress = () => S.Harem.Count },
            new Def { Id = "chapter_all", Cat = "Romance", Tier = 3, Name = "Happily Ever After", Goal = "Finish all three chapters with one girlfriend.", Target = 3, Progress = () => MaxGirl(g => g.Chapter) },
            new Def { Id = "all_dates", Cat = "Romance", Tier = 3, Name = "Everyone's Sweetheart", Goal = "Date every student at school.", Roster = true, Goal2 = () => Students(h => true), Progress = () => Students(h => h.isGirlfriend) },

            // ---- Campus ----
            new Def { Id = "first_week", Cat = "Campus", Tier = 1, Name = "First Week", Goal = "Spend 7 days at school.", Target = 7, Progress = () => S.Day },
            new Def { Id = "festival", Cat = "Campus", Tier = 2, Name = "Festival Goer", Goal = "Live through a school event (exams, sports festival, culture festival or field trip).", Progress = SchoolEvents },
            new Def { Id = "honor", Cat = "Campus", Tier = 2, Name = "Honor Roll", Goal = "Score 80 or more on an exam.", Progress = () => S.ExamScores.Any(x => x >= 80) ? 1 : 0 },
            new Def { Id = "days", Cat = "Campus", Tier = 3, Name = "Regular", Goal = "Spend 30 days at school.", Target = 30, Progress = () => S.Day },
            new Def { Id = "season_pass", Cat = "Campus", Tier = 3, Name = "Season Pass", Goal = "Live through the exams, sports festival, culture festival and field trip.", Target = 4, Progress = SchoolEvents },
            new Def { Id = "everyday", Cat = "Campus", Tier = 3, Name = "Everyday Life", Goal = "Have lunch, study or go to club with two friends, 5 times.", Target = 5, Progress = () => C("duo:lunch") + C("duo:study") + C("duo:club") + C("duo:season") },
            new Def { Id = "popular", Cat = "Campus", Tier = 3, Name = "Popular", Goal = "Reach a reputation of 70.", Target = 70, Progress = () => S.Reputation },
            new Def { Id = "days_100", Cat = "Campus", Tier = 3, Name = "Senpai", Goal = "Spend 100 days at school.", Target = 100, Progress = () => S.Day },

            // ---- Club (the game's own club) ----
            new Def { Id = "club_member", Cat = "Club", Tier = 1, Name = "Recruiter", Goal = "Bring a girl into the club.", Progress = () => Students(h => h.isStaff) },
            new Def { Id = "club_room", Cat = "Club", Tier = 2, Name = "Decorator", Goal = "Unlock 5 things for the club.", Target = 5, Progress = ClubContents },
            new Def { Id = "club_story", Cat = "Club", Tier = 2, Name = "Club Chronicle", Goal = "Live 3 stories of the club.", Target = 3, Progress = () => S.ClubHistory.Count },
            new Def { Id = "club_five", Cat = "Club", Tier = 3, Name = "Full House", Goal = "Have 5 girls in the club.", Target = 5, Progress = () => Students(h => h.isStaff) },
            new Def { Id = "club_ten", Cat = "Club", Tier = 3, Name = "Club Legend", Goal = "Have 10 girls in the club.", Target = 10, Progress = () => Students(h => h.isStaff) },

            // ---- Stories (the game's heroines and teachers) ----
            new Def { Id = "story_met", Cat = "Stories", Tier = 1, Name = "Main Cast", Goal = "Meet one of the story heroines.", Progress = () => Stories(h => S.MetDay.ContainsKey(GirlInfo.Key(h)) || h.talkEvent.Contains(0)) },
            new Def { Id = "teacher_met", Cat = "Stories", Tier = 1, Name = "Office Hours", Goal = "Talk with a teacher.", Progress = () => TeacherCount(Teachers.Met) },
            new Def { Id = "route_one", Cat = "Stories", Tier = 2, Name = "The End?", Goal = "Finish a story heroine's route.", Progress = () => Stories(StoryCast.StoryOver) },
            new Def { Id = "teacher_done", Cat = "Stories", Tier = 2, Name = "Teacher's Pet", Goal = "See a teacher's story to its end.", Progress = () => TeacherCount(Teachers.EventOver) },
            new Def { Id = "teacher_h", Cat = "Stories", Tier = 2, Name = "Extra Lessons", Goal = "Go all the way with a teacher.", Progress = () => TeacherCount(h => h.hCount > 0) },
            new Def { Id = "routes_all", Cat = "Stories", Tier = 3, Name = "Completionist", Goal = "Finish every story heroine's route.", Goal2 = () => Stories(h => true), Progress = () => Stories(StoryCast.StoryOver) },
            new Def { Id = "teachers_all", Cat = "Stories", Tier = 3, Name = "Faculty Favorite", Goal = "See every teacher's story to its end.", Goal2 = () => TeacherCount(h => true), Progress = () => TeacherCount(Teachers.EventOver) },

            // ---- Trouble (they only record; nothing is taken from you) ----
            new Def { Id = "seen", Cat = "Trouble", Tier = 1, Name = "Seen", Goal = "Get seen doing something you shouldn't.", Progress = () => S.Incidents.Count },
            new Def { Id = "duo:caught", Cat = "Trouble", Tier = 2, Name = "Caught Out", Goal = "Get confronted by your girlfriend about another girl." },
            new Def { Id = "walked_off", Cat = "Trouble", Tier = 2, Name = "Third Wheel", Goal = "Make a girl walk off while you talk with her and a friend.", Progress = () => C("trio:aleft") + C("trio:bleft") },
            new Def { Id = "settle:side", Cat = "Trouble", Tier = 2, Name = "Picking Sides", Goal = "Two girls disagree: side with one of them.", Progress = () => C("settle:a") + C("settle:b") },
            new Def { Id = "broke_up", Cat = "Trouble", Tier = 2, Name = "It's Over", Goal = "Break up with a girlfriend.", Progress = () => LoreCount("broke_up") },
            new Def { Id = "talk_of_school", Cat = "Trouble", Tier = 3, Name = "Talk of the School", Goal = "Be part of 10 things the school talks about.", Target = 10, Progress = () => S.Incidents.Count },
            new Def { Id = "wrong_door", Cat = "Trouble", Tier = 3, Name = "Wrong Door", Goal = "Walk in on a girl with someone else, 3 times.", Target = 3, Progress = () => SumGirl(g => g.Caught) },

            // ---- Extras (other mods: each one shows only with its mod) ----
            new Def { Id = "fairy_met", Cat = "Extras", Tier = 1, Mod = Mods.PantyFairy, Name = "A Fairy's Favor", Goal = "Meet the Panty Fairy.", Progress = () => Mods.FairyStage() >= 1 ? 1 : 0 },
            new Def { Id = "crest_one", Cat = "Extras", Tier = 2, Mod = Mods.LewdCrestX, Name = "Marked", Goal = "A girl at school wears a crest.", Progress = () => Students(h => Mods.Crest(h) != "") },
            new Def { Id = "pregnant", Cat = "Extras", Tier = 2, Mod = Mods.Pregnancy, Name = "Family Way", Goal = "A girl gets pregnant.", Progress = () => Students(Mods.EverPregnant) },
            new Def { Id = "fairy_done", Cat = "Extras", Tier = 2, Mod = Mods.PantyFairy, Name = "Fairy Tale Ending", Goal = "Finish the Panty Fairy's story.", Progress = () => Mods.FairyStage() >= 2 ? 1 : 0 },
            new Def { Id = "crest_five", Cat = "Extras", Tier = 3, Mod = Mods.LewdCrestX, Name = "Crest Collector", Goal = "Five girls at school wear a crest.", Target = 5, Progress = () => Students(h => Mods.Crest(h) != "") },
            new Def { Id = "fairy_haul", Cat = "Extras", Tier = 3, Mod = Mods.PantyFairy, Name = "Lingerie Thief", Goal = "Take 20 pairs for the fairy.", Target = 20, Progress = Mods.FairyHaul },

            // ---- Secrets (the goal shows once it falls; until then, a hint) ----
            new Def { Id = "trio_kiss", Cat = "Secrets", Tier = 2, Name = "Audience", Secret = true, Goal = "Kiss a girl right in front of her friend.", Hint = "A kiss is louder with someone watching." },
            new Def { Id = "duo:trisal", Cat = "Secrets", Tier = 2, Name = "Two Hearts", Secret = true, Goal = "Both your girlfriends come to see you together.", Hint = "When two hearts agree, they come as one." },
            new Def { Id = "trio:h3", Cat = "Secrets", Tier = 2, Name = "Three's Company", Secret = true, Goal = "Go somewhere private with two girls at once.", Hint = "Some invitations work better for three." },
            new Def { Id = "diplomat", Cat = "Secrets", Tier = 3, Name = "Diplomat", Secret = true, Goal = "Settle 3 disagreements without picking a side.", Target = 3, Progress = () => C("settle:middle") + C("settle:change"), Hint = "Sometimes nobody has to win." },
            new Def { Id = "kissed_five", Cat = "Secrets", Tier = 3, Name = "Heartbreaker", Secret = true, Goal = "Kiss 5 different girls.", Target = 5, Progress = KissedGirls, Hint = "Count the lips, not the days." },
        };

        /// <summary>The ones that show in this game (their mod, if any, is loaded).</summary>
        public static IEnumerable<Def> Shown { get { return All.Where(d => Mods.Has(d.Mod)); } }

        public static int ShownCount { get { return Shown.Count(); } }

        /// <summary>The categories with anything to show.</summary>
        public static IEnumerable<string> ShownCats { get { return Cats.Where(c => Shown.Any(d => d.Cat == c)); } }

        /// <summary>Its goal; for "every girl", how many there are now (at least 1).</summary>
        public static int TargetOf(Def d)
        {
            if (d.Goal2 == null) return d.Target;
            try { return Mathf.Max(1, d.Goal2()); }
            catch { return 1; }
        }

        public static Def Find(string id) { return All.FirstOrDefault(d => d.Id == id); }

        public static bool Done(string id) { return S != null && S.AchDone.Any(x => x.StartsWith(id + "|", StringComparison.Ordinal)); }

        public static int Value(Def d)
        {
            if (S == null) return 0;
            try { return Mathf.Max(0, d.Progress != null ? d.Progress() : C(d.Id)); }
            catch { return 0; }
        }

        /// <summary>"id|day|girl key" of a fallen one, or null.</summary>
        public static string[] Record(string id)
        {
            var r = S == null ? null : S.AchDone.FirstOrDefault(x => x.StartsWith(id + "|", StringComparison.Ordinal));
            return r == null ? null : r.Split(new[] { '|' }, 3);   // the girl's key has '|' of its own ("c:last|first|...")
        }

        public static int DoneCount { get { return S == null ? 0 : Shown.Count(d => Done(d.Id)); } }

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

        private static void Check(SaveData.Heroine with)
        {
            var due = Shown.Where(d => !Done(d.Id) && Value(d) >= TargetOf(d)).ToList();
            // An old save opening the album for the first time: one card for the whole batch, not a wall of them.
            foreach (var d in due) Unlock(d, due.Count <= 2 ? with : null, due.Count > 2);
            if (due.Count > 2) AchievementPopup.Show(due.Count + " achievements unlocked", "See the Album in F7", DoneCount, ShownCount);
        }

        private static void Unlock(Def d, SaveData.Heroine with, bool quiet = false)
        {
            S.AchDone.Add(d.Id + "|" + S.Day + "|" + (with != null ? GirlInfo.Key(with) : ""));
            if (d.Roster)
            {
                // "Every girl": it falls with whoever is at school, and the album says how many that was.
                int all, game;
                Roster(out all, out game);
                S.AchCount["roster:" + d.Id] = TargetOf(d);
                S.AchCount["rosterAll:" + d.Id] = all;
                S.AchCount["rosterGame:" + d.Id] = game;
            }
            var n = DoneCount;
            var total = ShownCount;
            if (!quiet) AchievementPopup.Show(d.Name, with != null ? "with " + GirlInfo.FirstName(with) : d.Goal, n, total);
            S.Log("Achievement: " + d.Name + (with != null ? ", with " + GirlInfo.FirstName(with) : "") + ".");
            CampusLifePlugin.Log.LogInfo("Achievement " + d.Id + " day " + S.Day + (with != null ? " with " + GirlInfo.FirstName(with) : "") + " (" + n + "/" + total + ")");
        }

        public static bool Force(string id)
        {
            var d = Find(id);
            if (d == null || S == null || Done(id)) return false;
            Unlock(d, null);
            return true;
        }

        public static string Describe()
        {
            if (S == null) return "achievements: no save";
            int all, game;
            Roster(out all, out game);
            return "achievements " + DoneCount + "/" + ShownCount + " (of " + All.Count + "; characters " + all + ", game's " + game + "; " + Mods.Describe() + "): "
                + string.Join(", ", Shown.Select(d => d.Id + "[" + d.Tier + "]" + (Done(d.Id) ? " DONE" : " " + Value(d) + "/" + TargetOf(d))).ToArray());
        }
    }

    internal sealed partial class CampusState
    {
        public List<string> AchDone = new List<string>();                         // "id|day|girl" (the album)
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
