using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Communication;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>Everything an arc step needs to know about who is involved.</summary>
    internal sealed class ArcCtx
    {
        public ArcState St;
        public GirlState Gs;
        public SaveData.Heroine H;   // owner of the arc
        public SaveData.Heroine O;   // second girl (may be null)
        public string Name { get { return GirlInfo.FirstName(H); } }
        public string OName { get { return O == null ? "her friend" : GirlInfo.FirstName(O); } }
        public Tone Tone { get { return GirlInfo.ToneOf(H); } }
        public Tone OTone { get { return GirlInfo.ToneOf(O); } }
        public string Key { get { return GirlInfo.Key(H); } }
        public string OKey { get { return O == null ? "" : GirlInfo.Key(O); } }
    }

    /// <summary>A short personal story told over a few days through board requests and talk scenes.</summary>
    internal abstract class ArcDef
    {
        public string Id;
        public string Name;
        public bool Drama;          // two-girl conflict (can be turned off)
        public bool NeedsOther;

        public abstract bool Eligible(SaveData.Heroine h, GirlState gs);
        public virtual bool OtherOk(SaveData.Heroine h, SaveData.Heroine o) { return o != null && o != h && GirlInfo.IsStudent(o) && o.relation >= 0; }

        /// <summary>Request for the current step, or null when the story is over.</summary>
        public abstract Request MakeStep(ArcCtx c);

        /// <summary>Talk-scene content for steps completed with the "Campus request" button.</summary>
        public virtual void Scene(ArcCtx c, Request r, SceneBuilder b) { b.Her_(Lines.RequestThanks(c.Tone)); }

        public virtual void OnChoice(ArcCtx c, Request r, string choice) { }

        public virtual void OnStepDone(ArcCtx c, Request r) { c.St.Step++; }

        public virtual string StepHint(ArcCtx c) { return ""; }

        // ---- helpers ----
        protected static Request Talk(ArcCtx c, SaveData.Heroine who, string title, string text, int favor, int days = 2, PeriodMask p = PeriodMask.Any)
        {
            var r = Board.Make(RequestKind.Talk, who, title, text, favor, p);
            r.DeadlineDay = Campus.S.Day + days;
            return r;
        }

        protected static Request Of(ArcCtx c, RequestKind kind, string title, string text, int favor, int days = 2, PeriodMask p = PeriodMask.Any)
        {
            var r = Board.Make(kind, c.H, title, text, favor, p);
            r.DeadlineDay = Campus.S.Day + days;
            return r;
        }

        protected static void Reward(ArcCtx c, SaveData.Heroine who, Rewards rw, string why)
        {
            var applied = Campus.Apply(rw, who);
            Toasts.Show(why + " (" + applied.Describe() + ")");
        }

        protected static void Bond(ArcCtx c, int amount)
        {
            if (c.O == null) return;
            Arcs.AddBond(c.Key, c.OKey, amount);
        }
    }

    internal static class Arcs
    {
        public static readonly List<ArcDef> All = new List<ArcDef>
        {
            new MakeupExamArc(), new TournamentArc(), new LostCharmArc(), new SecretHobbyArc(), new RivalryArc(),
            new FriendshipArc(), new ConfessionPracticeArc(), new LateStudyArc(), new BadDayArc(),
        };

        private static CampusState S { get { return Campus.S; } }

        public static ArcDef Get(string id) { return All.FirstOrDefault(a => a.Id == id); }

        public static int ActiveCount { get { return S.Girls.Values.Count(g => g.Arc != null); } }

        public static void AddBond(string a, string b, int amount)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return;
            foreach (var pair in new[] { new[] { a, b }, new[] { b, a } })
            {
                var gs = S.Girl(pair[0]);
                int v;
                gs.Bonds.TryGetValue(pair[1], out v);
                gs.Bonds[pair[1]] = Mathf.Clamp(v + amount, -100, 100);
            }
        }

        private static ArcCtx Ctx(string ownerKey)
        {
            var h = GirlInfo.Find(ownerKey);
            if (h == null) return null;
            var gs = S.Girl(ownerKey);
            if (gs.Arc == null) return null;
            return new ArcCtx { St = gs.Arc, Gs = gs, H = h, O = GirlInfo.Find(gs.Arc.Other) };
        }

        /// <summary>The girl whose story a request belongs to (requests may target the second girl).</summary>
        private static string OwnerOf(Request r)
        {
            var gs = S.Girl(r.Girl);
            if (gs.Arc != null && gs.Arc.Id == r.Arc) return r.Girl;
            return string.IsNullOrEmpty(r.Other) ? r.Girl : r.Other;
        }

        // ---------------- lifecycle ----------------

        public static void OnNewDay()
        {
            if (!Cfg.ArcsEnabled.Value) return;
            // Re-issue steps that have no open request (after a retry, or a step that waits a day).
            foreach (var gs in S.Girls.Values.Where(g => g.Arc != null).ToList())
            {
                var c = Ctx(gs.Key);
                if (c == null || (c.O == null && !string.IsNullOrEmpty(gs.Arc.Other)))
                {
                    gs.Arc = null; // a girl left the save
                    continue;
                }
                if (!S.Requests.Any(r => r.State == RequestState.Active && r.Arc == gs.Arc.Id && OwnerOf(r) == gs.Key))
                    Advance(c);
            }
            if (ActiveCount < Cfg.MaxActiveArcs.Value && Random.value < Cfg.ArcChancePerDay.Value)
                TryStartRandom();
        }

        public static bool TryStartRandom()
        {
            var students = Board.Students;
            if (students.Count == 0) return false;
            var spot = Spotlight.IsActive ? S.Spotlight : null;
            var candidates = new List<KeyValuePair<SaveData.Heroine, ArcDef>>();
            foreach (var h in students)
            {
                var gs = S.Girl(GirlInfo.Key(h));
                if (gs.Arc != null) continue;
                if (spot != null && !spot.Contains(gs.Key) && Random.value < 0.7f) continue;
                foreach (var a in All)
                {
                    if (a.Drama && !Cfg.DramaEnabled.Value) continue;
                    if (gs.FinishedArcs.Contains(a.Id)) continue;
                    if (a.Eligible(h, gs)) candidates.Add(new KeyValuePair<SaveData.Heroine, ArcDef>(h, a));
                }
            }
            if (candidates.Count == 0) return false;
            var pick = candidates[Random.Range(0, candidates.Count)];
            return Start(pick.Key, pick.Value);
        }

        public static bool Start(SaveData.Heroine h, ArcDef a, int counter = 0)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Arc != null) return false;
            var st = new ArcState { Id = a.Id, StartedDay = S.Day, Counter = counter };
            if (a.NeedsOther)
            {
                var others = Board.Students.Where(o => a.OtherOk(h, o) && S.Girl(GirlInfo.Key(o)).Arc == null).ToList();
                if (others.Count == 0) return false;
                st.Other = GirlInfo.Key(others[Random.Range(0, others.Count)]);
            }
            gs.Arc = st;
            var c = Ctx(gs.Key);
            S.Log("A new story begins: " + a.Name + " (" + GirlInfo.FirstName(h) + ").");
            Toasts.Show("Something is going on with " + GirlInfo.FirstName(h) + "... (" + a.Name + ")");
            Advance(c);
            return true;
        }

        private static void Advance(ArcCtx c)
        {
            if (c == null) return;
            var def = Get(c.St.Id);
            if (def == null) { c.Gs.Arc = null; return; }
            Request r;
            try { r = def.MakeStep(c); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); r = null; }
            if (r == null) { Finish(c, true); return; }
            r.Source = "arc";
            r.Arc = def.Id;
            r.ArcStep = c.St.Step;
            if (r.Girl != c.Key) r.Other = c.Key;
            if (r.DeadlineDay < S.Day) r.DeadlineDay = S.Day + 1;
            Board.Add(r);
        }

        private static void Finish(ArcCtx c, bool completed)
        {
            var def = Get(c.St.Id);
            c.Gs.Arc = null;
            if (def == null) return;
            if (!c.Gs.FinishedArcs.Contains(def.Id)) c.Gs.FinishedArcs.Add(def.Id);
            if (completed)
            {
                S.Stat("arcs");
                S.Reputation = Mathf.Clamp(S.Reputation + 2, 0, 100);
                S.Log("Story finished: " + def.Name + " (" + c.Name + ").");
                Toasts.Show("Story finished: " + def.Name + ".");
            }
        }

        public static void Abandon(string ownerKey)
        {
            var c = Ctx(ownerKey);
            if (c == null) return;
            foreach (var r in S.Requests.Where(x => x.State == RequestState.Active && x.Arc == c.St.Id && OwnerOf(x) == ownerKey))
                r.State = RequestState.Failed;
            var def = Get(c.St.Id);
            S.Log("Story dropped: " + (def != null ? def.Name : c.St.Id) + " (" + c.Name + ").");
            c.Gs.Arc = null;
            if (def != null) c.Gs.FinishedArcs.Add(def.Id);
        }

        // ---------------- events from the board ----------------

        public static void BuildTalkScene(Request r, SaveData.Heroine talkingTo, SceneBuilder b)
        {
            var c = Ctx(OwnerOf(r));
            var def = Get(r.Arc);
            if (c == null || def == null) { b.Her_(Lines.RequestThanks(GirlInfo.ToneOf(talkingTo))); return; }
            def.Scene(c, r, b);
        }

        public static void OnTalkSceneDone(Request r, SaveData.Heroine h, string choice)
        {
            var c = Ctx(OwnerOf(r));
            var def = Get(r.Arc);
            if (c != null && def != null)
            {
                // Scenes without a choice keep whatever the scene itself stored (e.g. a stat check result).
                if (!string.IsNullOrEmpty(choice)) c.St.Choice = choice;
                try { def.OnChoice(c, r, choice ?? ""); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            }
            Board.Complete(r, h);
        }

        public static void OnRequestDone(Request r, SaveData.Heroine h)
        {
            var c = Ctx(OwnerOf(r));
            if (c == null || c.St.Id != r.Arc || c.St.Step != r.ArcStep) return;
            var def = Get(r.Arc);
            if (def == null) return;
            c.St.Retries = 0;
            def.OnStepDone(c, r);
            if (c.Gs.Arc != null) Advance(c);
        }

        public static void OnRequestFailed(Request r)
        {
            var c = Ctx(OwnerOf(r));
            if (c == null || c.St.Id != r.Arc) return;
            if (c.St.Retries < 1)
            {
                c.St.Retries++;
                Toasts.Show(c.Name + " is still waiting for you (" + (Get(r.Arc) != null ? Get(r.Arc).Name : "") + "). One more chance tomorrow.");
                return; // OnNewDay re-issues the step
            }
            G.AddFavor(c.H, -3, 0);
            c.Gs.Mood = Mood.Upset;
            Abandon(c.Key);
            Toasts.Show(c.Name + " gave up waiting. (-3 favor)");
        }

        public static void OnTalkResult(SaveData.Heroine h, ResultEnum result)
        {
            if (!Cfg.ArcsEnabled.Value || ActiveCount >= Cfg.MaxActiveArcs.Value) return;
            // A girl in a bad mood may open up after a conversation.
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Arc == null && gs.Mood == Mood.Upset && GirlInfo.IsStudent(h) && Random.value < 0.35f)
            {
                var a = Get("bad_day");
                if (a != null && a.Eligible(h, gs)) Start(h, a);
            }
        }

        public static void OnActivity(ActivityDef a)
        {
            if (!Cfg.ArcsEnabled.Value || ActiveCount >= Cfg.MaxActiveArcs.Value || Random.value > 0.25f) return;
            // Hobbies you share can start a "secret hobby" story with a girl who likes the same thing.
            string like;
            switch (a.Id)
            {
                case "feed_cats": like = "animals"; break;
                case "romance_novel": case "study": like = "study"; break;
                case "browse_magazines": like = "fashion"; break;
                case "hearty_lunch": case "kitchen_job": like = "cooking"; break;
                default: return;
            }
            var arc = Get("secret_hobby");
            var h = Board.Students.FirstOrDefault(x => GirlInfo.Likes(x, like) && arc.Eligible(x, S.Girl(GirlInfo.Key(x))));
            if (h != null) Start(h, arc, Array.IndexOf(GirlInfo.LikeIds, like) + 1);
        }

        // ---------------- planner info ----------------

        public static IEnumerable<string> Describe()
        {
            foreach (var gs in S.Girls.Values.Where(g => g.Arc != null))
            {
                var def = Get(gs.Arc.Id);
                var c = Ctx(gs.Key);
                if (def == null || c == null) continue;
                var hint = def.StepHint(c);
                yield return GirlInfo.FirstName(c.H) + " - " + def.Name + (c.O != null ? " (with " + c.OName + ")" : "") + (string.IsNullOrEmpty(hint) ? "" : ": " + hint);
            }
        }
    }
}
