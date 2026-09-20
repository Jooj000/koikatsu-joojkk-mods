using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Bonds in Motion: short stories two students live through on their own, over three or four days. Best friends
    /// drifting apart, rivals stuck working together, two girls finding a hobby in common, an old quarrel being made up.
    /// Each step is a line in the school news; the end changes the pair for good (and goes to the anthology). The
    /// director's molds pick them up when the girls cross your path (a reunion for drifting friends, friction for
    /// rivals), so you can change how it ends.
    /// </summary>
    internal static class BondsInMotion
    {
        private static CampusState S { get { return Campus.S; } }
        private const int MaxActive = 2;

        public static IEnumerable<PairState> Active { get { return S.Pairs.Values.Where(p => p.Arc != PairArc.None); } }

        public static string Label(PairArc a)
        {
            switch (a)
            {
                case PairArc.Drifting: return "drifting apart";
                case PairArc.Rivals: return "rivals working together";
                case PairArc.Hobby: return "a hobby in common";
                case PairArc.Reconcile: return "making up";
                default: return "";
            }
        }

        public static void Daily(List<SaveData.Heroine> students)
        {
            foreach (var p in Active.ToList()) Step(p);
            if (Active.Count() >= MaxActive || Random.value > 0.35f) return;
            // A new story between two students, from what is true about them.
            var cands = new List<KeyValuePair<PairState, PairArc>>();
            var pool = students.OrderBy(_ => Random.value).Take(10).ToList();
            for (var i = 0; i < pool.Count; i++)
                for (var j = i + 1; j < pool.Count; j++)
                {
                    var a = pool[i]; var b = pool[j];
                    var bond = Social.Bond(a, b);
                    var p = S.FindPair(GirlInfo.Key(a), GirlInfo.Key(b));
                    if (p != null && (p.Arc != PairArc.None || p.ArcDay >= 0 && S.Day - p.ArcDay < 10)) continue;
                    var sameClub = !GirlInfo.InNoClub(a) && a.clubActivities == b.clubActivities;
                    var shared = GirlInfo.LikesOf(a).Intersect(GirlInfo.LikesOf(b)).Any();
                    if (bond >= 45 && (Pairs.PartnerOf(a) != null || Pairs.PartnerOf(b) != null)) cands.Add(Kv(a, b, PairArc.Drifting));
                    else if (bond <= 5 && (sameClub || a.schoolClass == b.schoolClass)) cands.Add(Kv(a, b, PairArc.Rivals));
                    else if (bond > 5 && bond < 45 && shared) cands.Add(Kv(a, b, PairArc.Hobby));
                    else if (bond <= -20 || p != null && p.Stage == PairStage.Broken && S.Day - p.StageDay >= 7) cands.Add(Kv(a, b, PairArc.Reconcile));
                }
            if (cands.Count == 0) return;
            var pick = cands[Random.Range(0, cands.Count)];
            Start(pick.Key, pick.Value);
        }

        private static KeyValuePair<PairState, PairArc> Kv(SaveData.Heroine a, SaveData.Heroine b, PairArc arc)
        {
            return new KeyValuePair<PairState, PairArc>(Pairs.Of(a, b), arc);
        }

        public static void Start(PairState p, PairArc arc)
        {
            if (p == null) return;
            p.Arc = arc;
            p.ArcStep = 0;
            p.ArcDay = S.Day;
            p.ArcNote = "";
            CampusLifePlugin.Log.LogInfo("Bonds in Motion: " + GirlInfo.FirstName(p.A) + " & " + GirlInfo.FirstName(p.B) + ": " + Label(arc));
            Step(p);
        }

        private static string A(PairState p) { return GirlInfo.FirstName(p.A); }
        private static string B(PairState p) { return GirlInfo.FirstName(p.B); }

        /// <summary>One day of the story.</summary>
        private static void Step(PairState p)
        {
            if (Social.Get(p.A) == null || Social.Get(p.B) == null) { p.Arc = PairArc.None; return; }
            p.ArcStep++;
            string line = null;
            switch (p.Arc)
            {
                case PairArc.Drifting:
                    if (p.ArcStep == 1) line = A(p) + " ate lunch alone today. " + B(p) + " was busy again.";
                    else if (p.ArcStep == 2) { line = A(p) + " and " + B(p) + " passed each other in the hall with just a nod."; Arcs.AddBond(p.A, p.B, -3); }
                    else Resolve(p, p.Together >= 2 || p.Trust >= 65);
                    break;
                case PairArc.Rivals:
                    if (p.ArcStep == 1) line = "The teacher paired " + A(p) + " and " + B(p) + " for a project. Neither looked happy.";
                    else if (p.ArcStep == 2) { line = A(p) + " and " + B(p) + " argued about the project for the whole period... but it's getting done."; p.Trust = PairState.Clamp(p.Trust + 5); }
                    else if (p.ArcStep == 3) line = A(p) + " admitted " + B(p) + "'s idea was good. Out loud.";
                    else Resolve(p, p.Trust >= 45);
                    break;
                case PairArc.Hobby:
                    if (p.ArcStep == 1) line = A(p) + " found out " + B(p) + " is into the same things she is.";
                    else if (p.ArcStep == 2) { line = A(p) + " and " + B(p) + " spent the break swapping recommendations."; Arcs.AddBond(p.A, p.B, 4); p.Together++; }
                    else Resolve(p, true);
                    break;
                case PairArc.Reconcile:
                    if (p.ArcStep == 1) line = A(p) + " and " + B(p) + " keep almost saying something to each other.";
                    else if (p.ArcStep == 2) { line = A(p) + " left a note on " + B(p) + "'s desk. Nobody knows what it said."; p.Trust = PairState.Clamp(p.Trust + 8); }
                    else Resolve(p, p.Trust >= 40 || Random.value < 0.5f);
                    break;
            }
            if (line != null) Pairs.News(line);
        }

        /// <summary>The end of the story (a reunion scene can end it early, for the better).</summary>
        public static void Resolve(PairState p, bool good)
        {
            if (p.Arc == PairArc.None) return;
            var arc = p.Arc;
            string line;
            switch (arc)
            {
                case PairArc.Drifting:
                    if (good) { Arcs.AddBond(p.A, p.B, 8); line = A(p) + " and " + B(p) + " found their way back to each other. Lunch together again."; }
                    else { Arcs.AddBond(p.A, p.B, -10); line = A(p) + " and " + B(p) + " aren't as close as they used to be."; }
                    break;
                case PairArc.Rivals:
                    if (good) { Arcs.AddBond(p.A, p.B, 15); line = A(p) + " and " + B(p) + " finished their project. They're... friends now? Apparently."; }
                    else { Arcs.AddBond(p.A, p.B, -6); line = A(p) + " and " + B(p) + " finished their project and swore never again."; }
                    break;
                case PairArc.Hobby:
                    Arcs.AddBond(p.A, p.B, 12);
                    if (Pairs.GirlsOk(Social.Get(p.A)) && Pairs.GirlsOk(Social.Get(p.B))) { p.AddRomance(p.A, 6); p.AddRomance(p.B, 6); }
                    line = A(p) + " and " + B(p) + " started a two-person club after school. It has a secret handshake.";
                    break;
                default:
                    if (good) { Arcs.AddBond(p.A, p.B, 20); p.Trust = PairState.Clamp(p.Trust + 15); line = A(p) + " and " + B(p) + " made up. Properly."; }
                    else line = A(p) + " and " + B(p) + " are still not talking.";
                    break;
            }
            p.Arc = PairArc.None;
            p.ArcStep = 0;
            p.ArcDay = S.Day;
            p.ArcNote = good ? "good" : "bad";
            Pairs.News(line);
            Pairs.Advance(p, true);
            Anthology.Add("bond", line, good ? 2 : 1, p.A + "," + p.B);
        }
    }
}
