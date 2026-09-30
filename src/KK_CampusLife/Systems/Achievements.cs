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
            public bool Secret;
            public Func<int> Progress;   // null: the counter with this id
        }

        public static readonly string[] Cats = { "Friendship", "Romance", "Trouble", "Campus", "Secrets" };

        private static CampusState S { get { return Campus.S; } }

        private static int C(string key) { int v; return S != null && S.AchCount.TryGetValue(key, out v) ? v : 0; }

        private static int Girls(Func<SaveData.Heroine, bool> f)
        {
            try { return G.Heroines.Count(h => h != null && GirlInfo.IsRegular(h) && f(h)); }
            catch { return 0; }
        }

        private static int KissedGirls()
        {
            return S == null ? 0 : S.Lore.Where(f => f.Verb == "kissed" && f.Subject == Lore.Player).Select(f => f.Obj).Distinct().Count();
        }

        public static readonly List<Def> All = new List<Def>
        {
            // ---- Friendship ----
            new Def { Id = "familiar_face", Cat = "Friendship", Name = "Familiar Face", Goal = "Talk with 10 different girls.", Target = 10, Progress = () => S.MetDay.Count },
            new Def { Id = "inner_circle", Cat = "Friendship", Name = "Inner Circle", Goal = "Become friends with 5 girls.", Target = 5, Progress = () => Girls(h => h.relation >= 1 || h.isGirlfriend) },
            new Def { Id = "duo:bestfriend", Cat = "Friendship", Name = "Friend of a Friend", Goal = "Let a friend introduce you to her best friend." },
            new Def { Id = "trio:natural", Cat = "Friendship", Name = "Party of Three", Goal = "Talk with two girls at once until they have to go, 3 times.", Target = 3 },
            new Def { Id = "settle:middle", Cat = "Friendship", Name = "Peacemaker", Goal = "Two girls disagree: stay in the middle and settle it.", },

            // ---- Romance ----
            new Def { Id = "first_kiss", Cat = "Romance", Name = "First Kiss", Goal = "Kiss a girl.", Progress = KissedGirls },
            new Def { Id = "kiss_wary", Cat = "Romance", Name = "She Let You", Goal = "Kiss a girl who wasn't sure, and have her let you." },
            new Def { Id = "rain_kiss", Cat = "Romance", Name = "Rain Date", Goal = "Kiss a girl while it rains." },
            new Def { Id = "girlfriend", Cat = "Romance", Name = "Going Steady", Goal = "Have a girlfriend.", Progress = () => Girls(h => h.isGirlfriend) },
            new Def { Id = "harem", Cat = "Romance", Name = "Shared Heart", Goal = "Have two girlfriends who agreed to share you.", Target = 2, Progress = () => S.Harem.Count },

            // ---- Trouble (they only record; nothing is taken from you) ----
            new Def { Id = "seen", Cat = "Trouble", Name = "Seen", Goal = "Get seen doing something you shouldn't.", Progress = () => S.Incidents.Count },
            new Def { Id = "talk_of_school", Cat = "Trouble", Name = "Talk of the School", Goal = "Be part of 10 things the school talks about.", Target = 10, Progress = () => S.Incidents.Count },
            new Def { Id = "duo:caught", Cat = "Trouble", Name = "Caught Out", Goal = "Get confronted by your girlfriend about another girl." },
            new Def { Id = "walked_off", Cat = "Trouble", Name = "Third Wheel", Goal = "Make a girl walk off while you talk with her and a friend.", Progress = () => C("trio:aleft") + C("trio:bleft") },
            new Def { Id = "settle:side", Cat = "Trouble", Name = "Picking Sides", Goal = "Two girls disagree: side with one of them.", Progress = () => C("settle:a") + C("settle:b") },

            // ---- Campus ----
            new Def { Id = "days", Cat = "Campus", Name = "Regular", Goal = "Spend 30 days at school.", Target = 30, Progress = () => S.Day },
            new Def { Id = "season_pass", Cat = "Campus", Name = "Season Pass", Goal = "Live through the exams, sports festival, culture festival and field trip.", Target = 4, Progress = () => Calendar.EventIds.Count(id => S.EventLastWeek.ContainsKey(id)) },
            new Def { Id = "honor", Cat = "Campus", Name = "Honor Roll", Goal = "Score 80 or more on an exam.", Progress = () => S.ExamScores.Any(x => x >= 80) ? 1 : 0 },
            new Def { Id = "popular", Cat = "Campus", Name = "Popular", Goal = "Reach a reputation of 70.", Target = 70, Progress = () => S.Reputation },
            new Def { Id = "everyday", Cat = "Campus", Name = "Everyday Life", Goal = "Have lunch, study or go to club with two friends, 5 times.", Target = 5, Progress = () => C("duo:lunch") + C("duo:study") + C("duo:club") + C("duo:season") },

            // ---- Secrets (the goal shows once it falls; until then, a hint) ----
            new Def { Id = "trio:h3", Cat = "Secrets", Name = "Three's Company", Secret = true, Goal = "Go somewhere private with two girls at once.", Hint = "Some invitations work better for three." },
            new Def { Id = "trio_kiss", Cat = "Secrets", Name = "Audience", Secret = true, Goal = "Kiss a girl right in front of her friend.", Hint = "A kiss is louder with someone watching." },
            new Def { Id = "duo:trisal", Cat = "Secrets", Name = "Two Hearts", Secret = true, Goal = "Both your girlfriends come to see you together.", Hint = "When two hearts agree, they come as one." },
            new Def { Id = "kissed_five", Cat = "Secrets", Name = "Heartbreaker", Secret = true, Goal = "Kiss 5 different girls.", Target = 5, Progress = KissedGirls, Hint = "Count the lips, not the days." },
            new Def { Id = "diplomat", Cat = "Secrets", Name = "Diplomat", Secret = true, Goal = "Settle 3 disagreements without picking a side.", Target = 3, Progress = () => C("settle:middle") + C("settle:change"), Hint = "Sometimes nobody has to win." },
        };

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

        public static int DoneCount { get { return S == null ? 0 : All.Count(d => Done(d.Id)); } }

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
            var due = All.Where(d => !Done(d.Id) && Value(d) >= d.Target).ToList();
            // An old save opening the album for the first time: one notice for the whole batch, not a wall of them.
            foreach (var d in due) Unlock(d, due.Count <= 2 ? with : null, due.Count > 2);
            if (due.Count > 2) Toasts.Show(due.Count + " achievements (" + DoneCount + "/" + All.Count + "): see the Album in F7");
        }

        private static void Unlock(Def d, SaveData.Heroine with, bool quiet = false)
        {
            S.AchDone.Add(d.Id + "|" + S.Day + "|" + (with != null ? GirlInfo.Key(with) : ""));
            var n = DoneCount;
            if (!quiet) Toasts.Show("Achievement: " + d.Name + (with != null ? " (" + GirlInfo.FirstName(with) + ")" : "") + "   " + n + "/" + All.Count);
            S.Log("Achievement: " + d.Name + (with != null ? ", with " + GirlInfo.FirstName(with) : "") + ".");
            CampusLifePlugin.Log.LogInfo("Achievement " + d.Id + " day " + S.Day + (with != null ? " with " + GirlInfo.FirstName(with) : "") + " (" + n + "/" + All.Count + ")");
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
            return "achievements " + DoneCount + "/" + All.Count + ": " + string.Join(", ", All.Select(d => d.Id + (Done(d.Id) ? " DONE" : " " + Value(d) + "/" + d.Target)).ToArray());
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
