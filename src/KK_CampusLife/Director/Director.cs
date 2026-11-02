using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>One situation the director found: a mold, who plays it, and why.</summary>
    internal sealed class Situation
    {
        public Mold Mold;
        public SaveData.Heroine A, B, W;   // actor, target, witness (any may be the only girl; B and W may be null)
        public string Why = "";            // the seed, as a phrase ("jealousy", "a broken promise"...)
        public float Weight = 1f;
        public int Stamp;                  // period it was found in
        public string Signature { get { return Mold.Id + "|" + K(A) + "|" + K(B) + "|" + Why; } }
        private static string K(SaveData.Heroine h) { return h == null ? "" : GirlInfo.Key(h); }
        public override string ToString() { return Mold.Id + " (" + GirlInfo.FirstName(A) + (B != null ? " + " + GirlInfo.FirstName(B) : "") + (W != null ? ", " + GirlInfo.FirstName(W) + " watching" : "") + ": " + Why + ")"; }
    }

    /// <summary>
    /// The school life director. It never draws "event X": each period it updates the world (needs, intentions, pairs,
    /// rumors), looks for seeds in the data (two friends spending every break together, a one-sided crush, a jealous
    /// girlfriend, a rumor with heat) and picks at most one situation, filled in from a mold. If the girls of that
    /// situation cross your path, it plays in front of you (the two-girl stage: they walk over, the scene plays in the
    /// talk screen); if not, it happens off screen and you hear about it (school news, the weekly report). A drama budget
    /// per week keeps big scenes rare, tension rises and cools, recent signatures weigh less and each pair rests between
    /// scenes.
    /// </summary>
    internal static partial class Director
    {
        private static CampusState S { get { return Campus.S; } }
        private static Situation _pending;
        private static float _nextTry;
        public static string LastPlayed = "", LastOffscreen = "";
        public static readonly List<string> Seeds = new List<string>(); // "kind|a|b", planted by scenes

        public static Situation Pending { get { return _pending; } }
        private static int Stamp { get { return S.Day * 16 + (int)G.CurrentMask; } }

        /// <summary>A scene planted a seed (a confession she means to make, a fight about to happen).</summary>
        public static void Seed(string kind, string a, string b)
        {
            var s = kind + "|" + a + "|" + b;
            if (!Seeds.Contains(s)) Seeds.Add(s);
            if (Seeds.Count > 12) Seeds.RemoveAt(0);
        }

        public static bool HasSeed(string kind, string a, string b) { return Seeds.Contains(kind + "|" + a + "|" + b) || Seeds.Contains(kind + "|" + b + "|" + a); }
        public static void UseSeed(string kind, string a, string b) { Seeds.Remove(kind + "|" + a + "|" + b); Seeds.Remove(kind + "|" + b + "|" + a); }

        // ================= ticks =================

        /// <summary>New free period on a school day.</summary>
        public static void OnPeriod()
        {
            if (!Campus.On || !Cfg.DirectorEnabled.Value) return;
            try
            {
                Agenda.NewPeriod();
                if (S.DramaWeek != S.Week) { S.DramaWeek = S.Week; S.DramaUsed = 0; }
                S.Tension = Mathf.Max(0, S.Tension - 8);
                // What was waiting and never crossed your path happens anyway.
                if (_pending != null && _pending.Stamp != Stamp) { if (Present(_pending)) Offscreen(_pending); _pending = null; }
                Intents.OnPeriod();
                Pairs.PeriodTick();
                Intents.TuneDesires();
                Rumors.PeriodTick();
                Harem.PeriodTick();
                _pending = Pick(false);
                PlanLine();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Director period: " + e); }
        }

        /// <summary>
        /// A save from the middle of a day came back: the world stays where it was (no needs, pairs or rumors ticking), and only
        /// what lives outside the save is set up again, the agenda of the period and the situation waiting to happen.
        /// </summary>
        public static void OnResumed()
        {
            if (!Campus.On || !Cfg.DirectorEnabled.Value) return;
            try
            {
                Agenda.NewPeriod();
                Intents.TuneDesires();
                _pending = Pick(false);
                PlanLine();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Director resume: " + e); }
        }

        /// <summary>
        /// One dense line per period: what the girls mean to do (by kind), who is looking for the player, the situation
        /// waiting to happen, the week's drama and the tension.
        /// </summary>
        private static void PlanLine()
        {
            var girls = Social.Students.ToList();
            var kinds = girls.Select(h => (S.Girl(GirlInfo.Key(h)).Intent ?? "").Split(':')[0]).Where(x => x != "")
                .GroupBy(x => x).OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count()).ToArray();
            var seek = girls.Where(h => (S.Girl(GirlInfo.Key(h)).Intent ?? "") == Intents.Player).Select(GirlInfo.FirstName).ToArray();
            CampusLifePlugin.Log.LogInfo("Director plan " + S.Day + "/" + G.CurrentMask + ": " + girls.Count + " girls | intents " + string.Join(", ", kinds) +
                " | looking for you: " + (seek.Length == 0 ? "nobody" : string.Join(", ", seek)) + " | waiting: " + (_pending == null ? "-" : _pending.ToString()) +
                " | drama " + S.DramaUsed + "/" + Cfg.DramaPerWeek.Value + ", tension " + S.Tension);
        }

        /// <summary>New day (any day).</summary>
        public static void OnDay(Cycle.Week day)
        {
            if (!Campus.On || !Cfg.DirectorEnabled.Value) return;
            try
            {
                if (_pending != null) { Offscreen(_pending); _pending = null; }
                if (day == Cycle.Week.Monday)
                {
                    S.LastWeekNews = new List<string>(S.SchoolNews);
                    S.SchoolNews.Clear();
                    Pairs.Weekly();
                }
                Intents.OnDay();
                Standing.Daily();
                Pairs.Daily();
                Rumors.Daily();
                if (S.Signatures.Count > 16) S.Signatures.RemoveRange(0, S.Signatures.Count - 16);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Director day: " + e); }
        }

        // ================= keeping the plan current =================

        private static readonly HashSet<SaveData.Heroine> _changed = new HashSet<SaveData.Heroine>();
        private static readonly List<string> _changedWhy = new List<string>();

        /// <summary>
        /// Something changed for her (a conversation, a gift, a scene, H, a pair changing): her intention is drawn again
        /// and the situation waiting to happen is checked again, at the next tick. Before, the director set its goals at
        /// the start of the period and never looked again (user, 2026-10-11).
        /// </summary>
        public static void Changed(SaveData.Heroine h, string why)
        {
            if (h == null || !Campus.On) return;
            _changed.Add(h);
            if (_changedWhy.Count < 8) _changedWhy.Add(GirlInfo.FirstName(h) + ": " + why);
        }

        /// <summary>Is this situation still true? Its mold looks at the same girls again (a forced one always is).</summary>
        private static bool StillFits(Situation s)
        {
            if (s == null || s.A == null) return false;
            if (s.Why == "forced") return true;
            try
            {
                var girls = new List<SaveData.Heroine> { s.A };
                if (s.B != null) girls.Add(s.B);
                if (s.W != null) girls.Add(s.W);
                return s.Mold.Detect(girls).Any(x => x.A == s.A && x.B == s.B);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director recheck " + s.Mold.Id + ": " + e.Message); return true; }
        }

        private static void Refresh()
        {
            if (_changed.Count == 0) return;
            var girls = _changed.ToList();
            var why = string.Join("; ", _changedWhy.ToArray());
            _changed.Clear();
            _changedWhy.Clear();
            foreach (var h in girls)
            {
                try
                {
                    var gs = S.Girl(GirlInfo.Key(h));
                    var before = Intents.Label(h);
                    Intents.Choose(h, gs);
                    var now = Intents.Label(h);
                    if (now != before) CampusLifePlugin.Log.LogInfo("Director: " + GirlInfo.FirstName(h) + " now " + now + " (was " + before + ")");
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director re-choose: " + e.Message); }
            }
            var p = _pending;
            if (p == null || !girls.Any(h => h == p.A || h == p.B || h == p.W)) return;
            if (StillFits(p)) return;
            CampusLifePlugin.Log.LogInfo("Director: " + p + " no longer fits (" + why + "); planning again");
            _pending = Pick(false);
            if (_pending != null) CampusLifePlugin.Log.LogInfo("Director: waiting to happen this period: " + _pending);
        }

        /// <summary>About once a second from Calendar.Tick: the waiting situation plays when its girls are near you.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.DirectorEnabled.Value) return;
            try { Refresh(); }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Director refresh: " + e); }
            // (shiro + Elma)" waited all lunch with both absent). Once the school knows, a plan with an absent girl goes.
            if (_pending != null && !Present(_pending))
            {
                CampusLifePlugin.Log.LogInfo("Director: " + _pending + " dropped (a girl in it is not on campus today)");
                _pending = Pick(false);
                if (_pending != null && !Present(_pending)) _pending = null;
                if (_pending != null) CampusLifePlugin.Log.LogInfo("Director: waiting to happen this period: " + _pending);
            }
            try { TickGather(); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director gather: " + e.Message); _gathered = null; }
            if (_gathered != null) return;
            if (Duo.Busy || !Scenes.CanPlayOnMap || WalkTalk.Active) return;
            if (Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 15f;
            if (S.LastSituationStamp == Stamp) return;
            var near = Duo.Candidates();
            // The planned one, if its girls are here; else something that grows out of who is around you right now.
            var sit = _pending != null && Here(_pending, near) ? _pending : null;
            if (sit == null && Random.value < 0.3f) sit = Pick(true);
            if (sit == null) return;
            // Checked again right before it plays: what was true at the start of the period may not be any more.
            if (!StillFits(sit))
            {
                CampusLifePlugin.Log.LogInfo("Director: " + sit + " no longer fits; not played");
                if (sit == _pending) _pending = null;
                return;
            }
            if (!Agenda.May("situation " + sit.Mold.Id, BeatKind.Social, sit.A, sit.B)) return;
            // Both on your map: it happens there first, where you can see it; they come to you later.
            if (TryGather(sit)) return;
            if (Play(sit)) Agenda.Took("situation " + sit.Mold.Id, BeatKind.Social, sit.A, sit.B); // spent only once it really played, see Play
        }

        /// <summary>Every girl in it came to school today (true while the school cannot tell yet).</summary>
        private static bool Present(Situation s)
        {
            return s == null || Truth.AtSchool(s.A) && (s.B == null || Truth.AtSchool(s.B)) && (s.W == null || Truth.AtSchool(s.W));
        }

        private static bool Here(Situation s, List<NPC> near)
        {
            return near.Any(n => n.heroine == s.A) && (s.B == null || near.Any(n => n.heroine == s.B));
        }

        // ================= choosing =================

        /// <summary>
        /// All situations the molds find (on the map only among the girls near you), weighed by freshness and the
        /// drama budget; one is drawn, or none.
        /// </summary>
        public static Situation Pick(bool onMap)
        {
            var students = onMap ? Duo.Candidates().Select(n => n.heroine).ToList() : Social.Students.ToList();
            if (students.Count == 0) return null;
            var all = new List<Situation>();
            foreach (var m in Molds.All)
            {
                try { foreach (var s in m.Detect(students)) { s.Mold = m; s.Stamp = Stamp; all.Add(s); } }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Mold " + m.Id + ": " + e.Message); }
            }
            all = all.Where(Allowed).ToList();
            if (all.Count == 0) return null;
            foreach (var s in all)
            {
                var sig = s.Signature;
                var recent = S.Signatures.Count(x => x.StartsWith(s.Mold.Id + "|", StringComparison.Ordinal));
                if (S.Signatures.Contains(sig)) s.Weight *= 0.15f;
                s.Weight *= Mathf.Pow(0.6f, recent);
                if (s.Mold.Big && S.Tension >= 50) s.Weight *= 0.3f;    // after a storm, calm first
                if (!s.Mold.Big && S.Tension < 20) s.Weight *= 0.8f;    // a calm week leans toward something happening
            }
            var none = onMap ? 1.5f : 3f;
            var roll = Random.value * (all.Sum(x => x.Weight) + none);
            foreach (var s in all) { roll -= s.Weight; if (roll <= 0f) return s; }
            return null;
        }

        private static bool Allowed(Situation s)
        {
            if (s.A == null) return false;
            if (s.Mold.Big && S.DramaUsed >= Cfg.DramaPerWeek.Value) return false;
            // Each pair rests between scenes.
            if (s.B != null)
            {
                var p = S.FindPair(GirlInfo.Key(s.A), GirlInfo.Key(s.B));
                if (p != null && p.Cooldown >= S.Day && !s.Mold.IgnoresCooldown) return false;
            }
            return true;
        }

        private static void Spent(Situation s)
        {
            S.Signatures.Add(s.Signature);
            S.LastSituationStamp = Stamp;
            if (s.Mold.Big) { S.DramaUsed++; S.Tension = Mathf.Min(100, S.Tension + 30); }
            else S.Tension = Mathf.Min(100, S.Tension + 8);
            if (s.B != null)
            {
                var p = S.Pair(GirlInfo.Key(s.A), GirlInfo.Key(s.B));
                if (p != null) p.Cooldown = S.Day + 1;
            }
        }

        // ================= playing =================

        /// <summary>The girls walk over and the scene plays (the two-girl stage). False if it could not start.</summary>
        public static bool Play(Situation s)
        {
            if (s == null || Duo.Busy) return false;
            var na = NpcOf(s.A);
            var nb = s.B != null ? NpcOf(s.B) : null;
            if (na == null || s.B != null && nb == null) return false;
            var intro = new SceneBuilder(null, false);
            var show = new Duo.Show { A = na, B = nb, Name = "situation " + s, Intro = intro };
            Action<string> result = null;
            show.Main = inTalk =>
            {
                var sb = new SceneBuilder(s.A, inTalk);
                s.Mold.Write(s, inTalk ? intro : new SceneBuilder(null, false), sb, out result);
                show.Result = c =>
                {
                    try { if (result != null) result(c ?? ""); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                };
                return sb;
            };
            if (s.B != null) show.Outro = () => Outro(s);
            // Spent only when the scene really happened. If they never reached you (you walked away, changed map) it
            // stays planned for the period, and happens off screen at its end as before.
            show.Done = played =>
            {
                if (!played) { CampusLifePlugin.Log.LogInfo("Director: " + s + " did not reach you; still planned"); return; }
                Spent(s);
                if (_pending == s) _pending = null;
                foreach (var h in new[] { s.A, s.B }.Where(x => x != null)) { S.Girl(GirlInfo.Key(h)).DuoDay = S.Day; Changed(h, "played " + s.Mold.Id); }
                LastPlayed = s.ToString();
            };
            var main = show.Main(true);
            if (!Duo.Stage(show, main)) return false;
            CampusLifePlugin.Log.LogInfo("Director: playing " + s);
            return true;
        }

        private static SceneBuilder Outro(Situation s)
        {
            var o = new SceneBuilder(null, false);
            o.Narr(GirlInfo.FirstName(s.A) + " and " + GirlInfo.FirstName(s.B) + " head off" + (s.Mold.Big ? ", each lost in her own thoughts." : " together."));
            return o;
        }

        /// <summary>It happened without you: the mold decides how it went; you hear about it.</summary>
        public static void Offscreen(Situation s)
        {
            if (s == null) return;
            try
            {
                var news = s.Mold.Offscreen(s);
                Spent(s);
                LastOffscreen = s + (string.IsNullOrEmpty(news) ? "" : ": " + news);
                if (!string.IsNullOrEmpty(news)) Pairs.News(news);
                CampusLifePlugin.Log.LogInfo("Director: off screen " + LastOffscreen);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Director off screen: " + e); }
        }

        public static string Force(string moldId, SaveData.Heroine a, SaveData.Heroine b, bool offscreen)
        {
            var m = Molds.All.FirstOrDefault(x => string.Equals(x.Id, moldId, StringComparison.OrdinalIgnoreCase));
            if (m == null) return "no mold '" + moldId + "' (" + string.Join(", ", Molds.All.Select(x => x.Id).ToArray()) + ")";
            var s = m.Detect(new List<SaveData.Heroine> { a, b }.Where(x => x != null).ToList()).FirstOrDefault(x => x.A == a || x.A == b)
                    ?? new Situation { A = a, B = m.Solo ? null : b, Why = "forced" };
            s.Mold = m;
            s.Stamp = Stamp;
            if (offscreen) { Offscreen(s); return "off screen: " + LastOffscreen; }
            return Play(s) ? "playing " + s : "could not stage " + s;
        }

        internal static NPC NpcOf(SaveData.Heroine h)
        {
            return h == null || G.Act == null ? null : G.Act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
        }
    }
}
