using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Club Identity: in the week before the Culture Festival your club takes on a project with two or three members
    /// (club members first, then the girls closest to you). Planning (the members disagree; the choice sets the project:
    /// community, showcase, fundraiser or joint venture with another club), a setback (solved with INT, STR, money or the
    /// members' bonds) and the presentation at the festival. The result builds the club's identity and history; the same
    /// kind of project goes better next time.
    /// </summary>
    internal static class ClubProjects
    {
        private static CampusState S { get { return Campus.S; } }
        private const int PlanWeek = 4, ShowWeek = 5; // loop weeks (the Culture Festival is on Saturday of week 5)
        private static float _nextTry;

        public static string TypeName(string t)
        {
            switch (t)
            {
                case "community": return "Community Project";
                case "showcase": return "Competitive Showcase";
                case "fundraiser": return "Fundraising Campaign";
                case "joint": return "Joint Venture";
                default: return "Club project";
            }
        }

        public static string Status()
        {
            var p = S.Project;
            if (p == null || p.Week != CurrentProjectWeek()) return Calendar.LoopWeek < PlanWeek ? "The club project starts in loop week " + PlanWeek + "." : "No project this week.";
            var stage = p.Stage == 0 ? "planning soon" : p.Stage == 1 ? "planned, a setback is coming" : p.Stage == 2 ? "presentation at the Culture Festival" : "presented: " + p.Result;
            return (string.IsNullOrEmpty(p.Type) ? "Club project" : TypeName(p.Type)) + " (" + stage + ")";
        }

        /// <summary>The absolute week the current loop's project belongs to.</summary>
        private static int CurrentProjectWeek() { return S.Week - Calendar.LoopWeek + PlanWeek; }

        private static List<SaveData.Heroine> PickMembers()
        {
            var staff = Social.Students.Where(h => h.isStaff).OrderByDescending(h => h.favor).ToList();
            foreach (var h in Calendar.TopGirls(5)) if (!staff.Contains(h) && GirlInfo.IsRegular(h)) staff.Add(h);
            return staff.Take(3).ToList();
        }

        private static List<SaveData.Heroine> Members(ClubProject p) { return p.Members.Select(Social.Get).Where(h => h != null).ToList(); }

        /// <summary>About once a second: the next stage plays in the club period or after school.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.ClubProjects.Value || !G.IsSchoolDay || !Scenes.CanPlayOnMap || Duo.Busy || WalkTalk.Active) return;
            if (Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 20f;
            var mask = G.CurrentMask;
            if (mask != PeriodMask.Club && mask != PeriodMask.After) return;
            var lw = Calendar.LoopWeek;
            if (lw != PlanWeek && lw != ShowWeek) return;
            var p = S.Project;
            if (p == null || p.Week != CurrentProjectWeek())
            {
                if (lw != PlanWeek) return;
                var m = PickMembers();
                if (m.Count < 2) return;
                p = S.Project = new ClubProject { Week = CurrentProjectWeek(), Members = m.Select(GirlInfo.Key).ToList() };
            }
            if (p.Stage == 0 && lw == PlanWeek) Play(p, 1);
            else if (p.Stage == 1 && (lw == ShowWeek && G.Weekday != Cycle.Week.Saturday || lw == PlanWeek && G.Weekday >= Cycle.Week.Thursday)) Play(p, 2);
            else if (p.Stage == 2 && lw == ShowWeek && (G.Weekday == Cycle.Week.Saturday || G.Weekday == Cycle.Week.Friday && Random.value < 0.5f)) Play(p, 3);
        }

        public static string Force(int stage, List<SaveData.Heroine> members = null)
        {
            var p = S.Project;
            if (members != null && members.Count >= 2) p = S.Project = new ClubProject { Week = CurrentProjectWeek(), Members = members.Select(GirlInfo.Key).ToList(), Type = p != null && p.Week == CurrentProjectWeek() ? p.Type : "", Score = p != null && p.Week == CurrentProjectWeek() ? p.Score : 0 };
            if (p == null || p.Week != CurrentProjectWeek())
            {
                var m = PickMembers();
                if (m.Count < 2) return "not enough members";
                p = S.Project = new ClubProject { Week = CurrentProjectWeek(), Members = m.Select(GirlInfo.Key).ToList() };
            }
            if (stage >= 2 && string.IsNullOrEmpty(p.Type)) p.Type = "community";
            p.Stage = stage - 1;
            return Play(p, stage) ? "playing stage " + stage + " with " + string.Join(", ", Members(p).Select(GirlInfo.FirstName).ToArray()) : "could not play";
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        private static bool Play(ClubProject p, int stage)
        {
            var m = Members(p);
            if (m.Count < 2) return false;
            var b = new SceneBuilder(null, false);
            Action<string> done;
            if (stage == 1) done = Planning(p, m, b);
            else if (stage == 2) done = Setback(p, m, b);
            else done = Presentation(p, m, b);
            var ok = Scenes.PlayOnMap(b, c => { try { done(c); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } });
            if (ok) CampusLifePlugin.Log.LogInfo("Club project: stage " + stage + " (" + p.Type + ")");
            return ok;
        }

        // ---------------- 1. planning ----------------

        private static Action<string> Planning(ClubProject p, List<SaveData.Heroine> m, SceneBuilder b)
        {
            var a = m[0]; var c = m[1]; var d = m.Count > 2 ? m[2] : null;
            b.Bg("classroom_sunset");
            b.Narr("Club project: \"A Club Worth Remembering\". Part 1: the plan.");
            b.Narr("The Culture Festival is next week, and the club wants to do something of its own. Everyone has an opinion.");
            if (!string.IsNullOrEmpty(S.ClubIdentity)) b.Narr("Last time, the club became known as " + S.ClubIdentity + ".");
            b.Say(a, T(a, "Maybe we could do something for the whole school? Something everyone can join...", "Let's ENTER something! A contest! And WIN!", "A fundraiser is the logical choice. The club needs a budget.", "Why not team up with another club? Double the fun, half the work~"));
            b.Say(c, T(c, "A showcase could be nice too... if we're good enough.", "Or we could help people! That's cool too!", "I'd prefer something with measurable results.", "Or we just make a lot of money. Just saying."));
            if (d != null) b.Say(d, T(d, "Um... whatever we pick, I'll help!", "Can we do ALL of them?!", "Pick one. We're wasting time.", "Wake me up when you've decided."));
            b.Choice("A community project for the whole school", "cp_community", "A competitive showcase", "cp_showcase", "A fundraising campaign", "cp_fundraiser", "A joint venture with another club", "cp_joint");
            foreach (var t in new[] { "community", "showcase", "fundraiser", "joint" })
            {
                b.Branch("cp_" + t);
                b.Narr(t == "community" ? "A project open to everyone: decorating the main gate with messages from every class." : t == "showcase" ? "A showcase for the festival stage: the club will compete for the best exhibit."
                    : t == "fundraiser" ? "A fundraiser: homemade sweets and a raffle, all profits to the club." : "A joint venture with the art club: a giant mural, painted live during the festival.");
                b.Say(a, T(a, "...Okay! Let's do our best!", "YES! Let's go!", "Decided. Let's divide the work.", "Fine, fine. Let's make it good."));
                b.End();
            }
            return choice =>
            {
                p.Type = choice.StartsWith("cp_", StringComparison.Ordinal) ? choice.Substring(3) : "community";
                p.Stage = 1;
                if (S.ClubIdentity == Identity(p.Type)) p.Score += 10;
                foreach (var x in m) G.AddFavor(x, 2, 0);
                for (var i = 0; i < m.Count; i++) for (var j = i + 1; j < m.Count; j++) Arcs.AddBond(GirlInfo.Key(m[i]), GirlInfo.Key(m[j]), 2);
                Toasts.Show("Club project: " + TypeName(p.Type) + ". A setback is coming before the festival.");
            };
        }

        // ---------------- 2. setback ----------------

        private static Action<string> Setback(ClubProject p, List<SaveData.Heroine> m, SceneBuilder b)
        {
            var a = m[0]; var c = m[1];
            b.Bg("dark_hallway");
            b.Narr("Club project, part 2: unexpected trouble.");
            switch (p.Type)
            {
                case "showcase": b.Narr("A rival club entered the same category, with a budget three times yours."); break;
                case "fundraiser": b.Narr("The sweets you made for the fundraiser got crushed when a shelf collapsed overnight."); break;
                case "joint": b.Narr("The art club wants to change the whole design. Two days before the festival."); break;
                default: b.Narr("Rain soaked half of the messages for the gate. The ink ran on everything."); break;
            }
            b.Say(a, T(a, "What do we do now...?", "This is a DISASTER!", "We need a solution. Now.", "Well. This is fun."));
            b.Say(c, T(c, "We can't give up, right?", "We're NOT giving up!", "Options. Let's list them.", "Somebody be clever, quick."));
            b.Choice("Reorganize the plan [INT]", "cs_int", "Stay up all night redoing it [STR]", "cs_str", "Buy what's missing (1500 yen)", "cs_money", "Rally the members to help each other", "cs_rally");
            b.Branch("cs_int"); b.Narr(G.Int >= 45 ? "You redraw the whole schedule. It's tight, but it works." : "Your new plan has holes, but it's better than nothing."); b.End();
            b.Branch("cs_str"); b.Narr(G.Str >= 45 ? "You carry, cut, paint and glue until sunrise. It's done." : "You work until you can't keep your eyes open. Most of it gets done."); b.End();
            b.Branch("cs_money"); b.Narr(S.Money >= 1500 ? "You pay for new materials out of your own pocket. Problem solved." : "You don't have enough money. You buy what you can."); b.End();
            b.Branch("cs_rally"); b.Narr("You get everyone talking. One brings tape, one brings snacks, one brings an idea nobody expected."); b.End();
            return choice =>
            {
                var gain = 0;
                switch (choice)
                {
                    case "cs_int": gain = G.Int >= 45 ? 25 : 12; break;
                    case "cs_str": gain = G.Str >= 45 ? 25 : 12; Campus.SpendEnergy(1); break;
                    case "cs_money": if (S.Money >= 1500) { S.Money -= 1500; gain = 25; } else gain = 8; break;
                    case "cs_rally":
                        var bonds = 0; var n = 0;
                        for (var i = 0; i < m.Count; i++) for (var j = i + 1; j < m.Count; j++) { bonds += Social.Bond(m[i], m[j]); n++; Arcs.AddBond(GirlInfo.Key(m[i]), GirlInfo.Key(m[j]), 4); }
                        gain = n == 0 ? 10 : Mathf.Clamp(10 + bonds / n / 3, 5, 28);
                        break;
                }
                p.Score += gain;
                p.Stage = 2;
            };
        }

        // ---------------- 3. presentation ----------------

        private static Action<string> Presentation(ClubProject p, List<SaveData.Heroine> m, SceneBuilder b)
        {
            var a = m[0]; var c = m[1];
            b.Bg("culture_festival");
            b.Narr("Club project, part 3: the presentation, at the Culture Festival.");
            b.Narr(p.Type == "showcase" ? "The judges walk slowly past every exhibit, clipboards in hand." : p.Type == "fundraiser" ? "The raffle box fills up. People keep coming back for seconds."
                : p.Type == "joint" ? "Paint flies. A crowd gathers to watch the mural come alive." : "Visitors stop at the gate to read the messages. Some of them add their own.");
            b.Say(a, T(a, "Everyone's watching... I'm so nervous.", "This is it! Showtime!", "Everything is in place.", "Smile, everyone. We're on display."));
            b.Choice("Present it yourself", "pr_me", "Let " + GirlInfo.FirstName(a) + " present it", "pr_her", "Present it all together", "pr_all");
            b.Branch("pr_me"); b.Narr("You step forward and talk about the club, the plan, the setback and how you got through it."); b.End();
            b.Branch("pr_her"); b.Say(a, T(a, "M-me? ...Okay. I'll do it!", "Leave it to me!", "...Understood.", "Finally, the spotlight.")); b.End();
            b.Branch("pr_all"); b.Narr("Everyone takes a part. It's a little messy, and it's perfect."); b.End();
            return choice =>
            {
                p.Score += choice == "pr_me" ? (G.Sens + G.Int) / 10 : choice == "pr_her" ? 8 + a.favor / 20 : 10;
                p.Score += Random.Range(-10, 11);
                p.Stage = 3;
                var great = p.Score >= 45;
                var ok = p.Score >= 25;
                p.Result = great ? "a huge success" : ok ? "a success" : "a modest effort";
                var r = new Rewards { Rep = great ? 6 : ok ? 3 : 1, Club = great ? 8 : ok ? 4 : 2, Money = p.Type == "fundraiser" ? (great ? 6000 : ok ? 3000 : 1000) : 0 };
                if (p.Type == "showcase" && great) r.Rep += 4;
                Campus.Apply(r, a);
                foreach (var x in m) { G.AddFavor(x, great ? 5 : 3, 0); Memories.Add(x, "club", "we did the " + TypeName(p.Type).ToLower() + " together at the festival", 1); }
                if (p.Type == "joint" || choice == "pr_all") for (var i = 0; i < m.Count; i++) for (var j = i + 1; j < m.Count; j++) Arcs.AddBond(GirlInfo.Key(m[i]), GirlInfo.Key(m[j]), 6);
                if (ok) S.ClubIdentity = Identity(p.Type);
                S.ClubHistory.Add("Week " + p.Week + ": " + TypeName(p.Type) + ", " + p.Result + " (" + string.Join(", ", m.Select(GirlInfo.FirstName).ToArray()) + ")");
                if (S.ClubHistory.Count > 12) S.ClubHistory.RemoveAt(0);
                Anthology.Add("club", "The club's " + TypeName(p.Type).ToLower() + " was " + p.Result, great ? 4 : 2, string.Join(",", m.Select(GirlInfo.Key).ToArray()));
                Toasts.Show("Club project: " + p.Result + ". " + r.Describe());
            };
        }

        private static string Identity(string type)
        {
            switch (type)
            {
                case "showcase": return "the club that wins";
                case "fundraiser": return "the club with the budget";
                case "joint": return "the club that makes friends";
                default: return "the club that helps the school";
            }
        }
    }
}
