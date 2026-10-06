using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Student Council Race, once every 9-week loop (loop week 6): two candidates from the cast. One asks for your help,
    /// the other has a popular promise. Monday: the announcement; Tuesday/Wednesday: the campaign (honest and organized,
    /// a persuasive speech, a compromise between both, or digging into the rumor); Thursday: the debate, with an
    /// accusation to answer; Friday: the count. The winner's promise changes school life for two weeks; trust, the
    /// candidates' bond and reputation move with how you ran it.
    /// </summary>
    internal static class Council
    {
        private static CampusState S { get { return Campus.S; } }
        private const int RaceWeek = 6;
        private static float _nextTry;

        private static readonly string[][] Platforms =
        {
            new[] { "club", "more money for the clubs" },
            new[] { "lunch", "a longer lunch break" },
            new[] { "library", "the library open until evening" },
            new[] { "events", "more school events" },
            new[] { "pet", "a class pet for every year" },
        };

        private static string PlatformText(string id) { var p = Platforms.FirstOrDefault(x => x[0] == id); return p == null ? id : p[1]; }

        private static string PlatformFor(SaveData.Heroine h, string not)
        {
            var likes = GirlInfo.LikesOf(h);
            string id = likes.Contains("study") ? "library" : likes.Contains("food") || likes.Contains("sweets") ? "lunch" : likes.Contains("animals") ? "pet"
                : GirlInfo.ToneOf(h) == Tone.Energetic ? "events" : "club";
            if (id == not) id = Platforms.Select(x => x[0]).First(x => x != not);
            return id;
        }

        public static string Status()
        {
            var c = S.Council;
            if (c != null && c.Week == S.Week && c.Stage < 4) return "Council race: " + GirlInfo.FirstName(c.A) + " vs " + GirlInfo.FirstName(c.B) + " (support " + c.Support + "%)";
            if (!string.IsNullOrEmpty(S.CouncilWinner) && S.CouncilEffectUntilWeek >= S.Week) return "Council president: " + GirlInfo.FirstName(S.CouncilWinner) + " (" + PlatformText(S.CouncilEffect) + ")";
            return "Council race in loop week " + RaceWeek + ".";
        }

        private static bool Pick(out SaveData.Heroine a, out SaveData.Heroine b)
        {
            a = b = null;
            var cast = Social.Students.Where(h => !Pairs.IsScorned(h)).ToList();
            a = cast.Where(h => h.favor >= 30).OrderByDescending(h => Social.Trust(h) + (GirlInfo.Quirks(h).majime ? 20 : 0) + Random.Range(0, 15)).FirstOrDefault();
            if (a == null) return false;
            var aa = a;
            b = cast.Where(h => h != aa).OrderBy(h => Social.Bond(aa, h) + Random.Range(0, 25)).FirstOrDefault();
            return b != null;
        }

        /// <summary>Monday of the race week, or each day: the next stage, or the count if you missed it.</summary>
        public static void OnDay(Cycle.Week day)
        {
            if (!Cfg.CouncilRace.Value) return;
            var c = S.Council;
            if (Calendar.LoopWeek == RaceWeek && day == Cycle.Week.Monday && (c == null || c.Week != S.Week))
            {
                SaveData.Heroine a, b;
                if (Pick(out a, out b))
                {
                    var pa = PlatformFor(a, "");
                    S.Council = new CouncilRace { Week = S.Week, A = GirlInfo.Key(a), B = GirlInfo.Key(b), Platform = pa, RivalPlatform = PlatformFor(b, pa), Support = 45 };
                    Toasts.Show("Student council race this week: " + GirlInfo.FirstName(a) + " vs " + GirlInfo.FirstName(b) + ".");
                }
            }
            // The count happens on Friday whether you were there or not.
            c = S.Council;
            if (c != null && c.Week == S.Week && c.Stage < 4 && day == Cycle.Week.Saturday) Count(c, null);
            // The winner's promise.
            if (!string.IsNullOrEmpty(S.CouncilEffect) && S.CouncilEffectUntilWeek >= S.Week && day != Cycle.Week.Holiday)
                switch (S.CouncilEffect)
                {
                    case "club": G.AddClubPoints(2); break;
                    case "lunch": S.Energy++; break;
                    case "library": if (Random.value < 0.5f) G.AddStats(1, 0, 0); break;
                    case "events": S.Reputation = Mathf.Min(100, S.Reputation + 1); break;
                    case "pet": foreach (var h in Social.Students) S.Girl(GirlInfo.Key(h)).NeedSocial = Mathf.Min(100, S.Girl(GirlInfo.Key(h)).NeedSocial + 10); break;
                }
        }

        public static void Tick()
        {
            if (!Campus.On || !Cfg.CouncilRace.Value || !G.IsSchoolDay || !Scenes.CanPlayOnMap || Duo.Busy || WalkTalk.Active) return;
            if (Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 20f;
            var c = S.Council;
            if (c == null || c.Week != S.Week || c.Stage >= 4) return;
            var day = G.Weekday;
            if (day == null) return;
            var want = day == Cycle.Week.Monday ? 0 : day == Cycle.Week.Tuesday || day == Cycle.Week.Wednesday ? 1 : day == Cycle.Week.Thursday ? 2 : day == Cycle.Week.Friday ? 3 : -1;
            if (want < 0 || c.Stage > want) return;
            Play(c, c.Stage);
        }

        public static string Force(SaveData.Heroine a, SaveData.Heroine b, int stage)
        {
            var c = S.Council;
            if (c == null || c.Week != S.Week || a != null)
            {
                if (a == null || b == null) { if (!Pick(out a, out b)) return "no candidates"; }
                var pa = PlatformFor(a, "");
                c = S.Council = new CouncilRace { Week = S.Week, A = GirlInfo.Key(a), B = GirlInfo.Key(b), Platform = pa, RivalPlatform = PlatformFor(b, pa), Support = 45 };
            }
            c.Stage = Mathf.Clamp(stage, 0, 3);
            return Play(c, c.Stage) ? "playing council stage " + c.Stage + " (" + GirlInfo.FirstName(c.A) + " vs " + GirlInfo.FirstName(c.B) + ")" : "could not play";
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        private static bool Play(CouncilRace c, int stage)
        {
            var a = Social.Get(c.A);
            var b = Social.Get(c.B);
            if (a == null || b == null) { c.Stage = 4; return false; }
            var sb = new SceneBuilder(null, false);
            Action<string> done;
            switch (stage)
            {
                case 0: done = Announce(c, a, b, sb); break;
                case 1: done = Campaign(c, a, b, sb); break;
                case 2: done = Debate(c, a, b, sb); break;
                default: done = ch => Count(c, ch); CountScene(c, a, b, sb); break;
            }
            var ok = Scenes.PlayOnMap(sb, ch => { try { done(ch); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } });
            if (ok) CampusLifePlugin.Log.LogInfo("Council race: stage " + stage);
            return ok;
        }

        private static Action<string> Announce(CouncilRace c, SaveData.Heroine a, SaveData.Heroine b, SceneBuilder sb)
        {
            sb.Bg("classroom_sunset");
            sb.Narr("\"Promises, Posters and Popularity\". The student council election is this Friday.");
            sb.Say(a, T(a, "I'm running for president... I want " + PlatformText(c.Platform) + ". Will you help me with the campaign? || Um... I decided to run for president. I want " + PlatformText(c.Platform) + ". Would you help me? || I'm going to be a candidate... for " + PlatformText(c.Platform) + ". Could you be on my team? || I signed up for the election. I really want " + PlatformText(c.Platform) + "... will you help?", "I'm running for president!! " + char.ToUpper(PlatformText(c.Platform)[0]) + PlatformText(c.Platform).Substring(1) + "! Help me win! || Guess what! I'm running for president! My promise: " + PlatformText(c.Platform) + "! Join my team! || Election time! I'm in it to win it, for " + PlatformText(c.Platform) + "! You're with me, right?! || I'm gonna be president! " + PlatformText(c.Platform) + ", that's the plan! Help me out!",
                "I'm a candidate. My platform: " + PlatformText(c.Platform) + ". I could use a campaign manager. || I've registered as a candidate. Platform: " + PlatformText(c.Platform) + ". I need a manager. || I'm running. " + PlatformText(c.Platform) + ". You'd make a decent campaign manager. || Candidacy filed. My aim is " + PlatformText(c.Platform) + ". Will you manage the campaign?", "Guess who's running for president? Me. Be my campaign manager. It's an honor. || I'm running for president, obviously. Want to be my right hand? || The school needs me as president. And I need you as my manager. || Election season. I'm the best candidate. Join the winning side."));
            sb.Narr(GirlInfo.FirstName(b) + " is running too, promising " + PlatformText(c.RivalPlatform) + ". People seem to like that.");
            sb.Choice("Agree to help " + GirlInfo.FirstName(a), "cn_help", "Help " + GirlInfo.FirstName(b) + " instead", "cn_other", "Stay neutral", "cn_neutral");
            sb.Branch("cn_help"); sb.Say(a, T(a, "Thank you! I knew I could count on you. || Really? Thank you so much... I'll work hard! || You'll help? I'm so relieved... thank you. || Thank you! With you, I think I can do it.", "YES! We're gonna win this! || Woohoo! Team us! We can't lose now! || Awesome! Let's crush the competition! || Yes! Best campaign manager ever!", "Good. We start tomorrow. || Good. Meet me tomorrow, we'll plan. || Then we begin tomorrow. Be on time. || Good choice. We start tomorrow.", "Excellent. Victory is assured. || Wise choice. You're on the winning team. || Of course you said yes. Victory's ours. || Excellent. Together, we're unstoppable.")); sb.End();
            sb.Branch("cn_other"); sb.Say(a, T(a, "...Oh. I see. Good luck, then. || ...Oh. Okay. I hope it goes well for you. || ...I see. Then... good luck. || ...Okay. I understand. Good luck.", "...Seriously? Fine! || ...Wow. Okay. Fine! See if I care! || ...Seriously?! Whatever! || ...Ugh, fine! Your loss!", "...Noted. || ...Understood. || ...I'll remember that. || ...I see.", "...Traitor. || ...How disloyal. || ...Turncoat. || ...You'll regret that.")); sb.End();
            sb.Branch("cn_neutral"); sb.Say(a, T(a, "...I understand. I'll do my best anyway. || ...That's okay. I'll keep trying anyway. || ...I understand. I'll do my best on my own. || ...Okay. Thank you for being honest.", "Aw. Okay, but vote for me! || Aww. Fine, but you're still voting for me! || Boo. Okay, but think about it! || Okay, okay! But my vote's still yours, right?", "...Fair. || ...Reasonable. || ...Fair enough. || ...Neutral, then.", "Fence-sitter. || Sitting on the fence? Coward. || Undecided? How boring. || Playing it safe, hm?")); sb.End();
            return ch =>
            {
                c.Stage = 1;
                if (ch == "cn_other") { var t = c.A; c.A = c.B; c.B = t; var tp = c.Platform; c.Platform = c.RivalPlatform; c.RivalPlatform = tp; Social.AddTrust(a, -4); G.AddFavor(Social.Get(c.A), 3, 0); }
                else if (ch == "cn_neutral") { c.Approach = "neutral"; c.Stage = 3; }
                else G.AddFavor(a, 3, 0);
            };
        }

        private static Action<string> Campaign(CouncilRace c, SaveData.Heroine a, SaveData.Heroine b, SceneBuilder sb)
        {
            sb.Bg("dark_hallway");
            sb.Narr("Posters go up on every wall. " + GirlInfo.FirstName(b) + "'s smile is on half of them.");
            sb.Say(a, T(a, "How should we run the campaign...? || Um... how should we do the campaign? || What do you think we should try first...? || I'm not sure where to start... any ideas?", "Okay, campaign manager! What's the plan?! || Alright, boss! What's the game plan?! || Strategy meeting! What do we do first?! || Okay manager, tell me how we win!", "Strategy. Now. || Let's set the strategy. || We need a plan. Proposals? || Strategy first. Talk.", "So, genius. How do we win? || Well, mastermind? How do we win this? || So, what's your brilliant plan? || Go on, impress me with a strategy."));
            sb.Choice("An honest, organized campaign [INT]", "cm_honest", "A persuasive speech [SEN]", "cm_speech", "Negotiate a compromise with " + GirlInfo.FirstName(b), "cm_deal", "Look into what people are saying", "cm_dig");
            sb.Branch("cm_honest"); sb.Narr("Schedules, flyers, answers to every question. Boring, solid, convincing."); sb.End();
            sb.Branch("cm_speech"); sb.Narr("You write her a speech that makes the hallway go quiet."); sb.End();
            sb.Branch("cm_deal"); sb.Narr("You sit both candidates down. A joint promise: " + PlatformText(c.Platform) + " AND " + PlatformText(c.RivalPlatform) + "."); sb.Say(b, T(b, "...That's fair, actually. || ...That makes sense, actually. || Oh... that's a good idea. || ...You're right. Let's try that.", "Huh! Okay, I'm in! || Ooh! Okay, I like it! || Hey, smart! Let's do it! || Yeah! That works!", "...Rational. || ...Sensible. || ...Agreed. || ...That's logical.", "...Clever. Fine. || ...Not bad. Fine. || ...Smart. I'll allow it. || ...Clever. I'm almost impressed.")); sb.End();
            sb.Branch("cm_dig"); sb.Narr("You listen around. There's a rumor going around about " + GirlInfo.FirstName(a) + "... it'll come up at the debate."); sb.End();
            return ch =>
            {
                c.Approach = ch;
                switch (ch)
                {
                    case "cm_honest": c.Support += 6 + G.Int / 10; break;
                    case "cm_speech": c.Support += 4 + G.Sens / 8; break;
                    case "cm_deal": c.Support += 3; Arcs.AddBond(c.A, c.B, 10); break;
                    case "cm_dig": c.Support += 2; break;
                }
                Rumors.Start("council", c.A, c.B, c.B, 45);
                c.Stage = 2;
            };
        }

        private static Action<string> Debate(CouncilRace c, SaveData.Heroine a, SaveData.Heroine b, SceneBuilder sb)
        {
            var sc = Rumors.Open.FirstOrDefault(x => x.Kind == "council" && x.Subject == c.A);
            sb.Bg("gym");
            sb.Narr("The debate, in the gym. Half the school is here.");
            sb.Say(b, T(b, "I heard... " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". Is that true? || Someone told me " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "... is it true? || I don't want to believe it, but... " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "? || Um... people say " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". Is that right?", "Everyone's saying " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "! Explain! || Hey! The whole school says " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "! What's up with that?! || Is it true?! " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "?! || Spill it! Everyone's saying " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "!",
                "A question. Is it true that " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "? || Answer me. " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". True? || I heard " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". Explain. || Is it a fact that " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "?", "So, is it true " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "? Hm? || Word is " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". Care to comment? || Mm? I hear " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + ". Naughty~ || So~ " + Rumors.What(sc ?? new Scandal { Kind = "council", Subject = c.A }, 1) + "? Do tell."));
            sb.Say(a, T(a, "That's... that's not...! || Th-that's not true...! || No... I would never...! || Wh-why would anyone say that...?", "WHAT?! That's not true! || WHAT?! That's a total lie! || No way! Who said that?! || That's NOT true! At all!", "...That is false. || ...That is a lie. || ...Incorrect. || ...Baseless.", "...Oh, you did NOT just say that. || ...Excuse me? Say that again. || ...Oh, someone's asking for it. || ...How dare you."));
            var opts = new List<string> { "Defend her publicly", "db_defend" };
            opts.Add(c.Approach == "cm_dig" || G.Int >= 45 ? "Show where the rumor came from [INT]" : "Ask for proof"); opts.Add("db_proof");
            opts.Add("Let her answer herself"); opts.Add("db_her");
            opts.Add("Make the gym laugh [SEN]"); opts.Add("db_laugh");
            sb.Choice(opts.ToArray());
            sb.Branch("db_defend"); sb.Narr("You stand up and say she'd never do that. Some people believe you. Some people roll their eyes."); sb.End();
            sb.Branch("db_proof"); sb.Narr(c.Approach == "cm_dig" || G.Int >= 45 ? "You lay it out: who said it first, to whom, and how it changed. The gym murmurs. The rumor dies on the spot." : "You ask for proof. There isn't any. There isn't any against it either."); sb.End();
            sb.Branch("db_her"); sb.Say(a, T(a, "...I've never bribed anyone. I just want " + PlatformText(c.Platform) + ". That's all. || ...I would never bribe anyone. I only want " + PlatformText(c.Platform) + ". || ...I didn't do anything like that. I just want " + PlatformText(c.Platform) + "... || ...It isn't true. All I want is " + PlatformText(c.Platform) + ".", "I'd never do that! I just want " + PlatformText(c.Platform) + "! Period! || I'd NEVER! All I want is " + PlatformText(c.Platform) + "! That's it! || No way, no how! I'm running for " + PlatformText(c.Platform) + ", not for bribes! || That's so wrong! I just want " + PlatformText(c.Platform) + "!", "...The accusation is baseless. My platform stands. || ...Unfounded. My platform speaks for itself. || ...No evidence, no case. My platform stands. || ...False. I'm running on my platform alone.", "...Please. If I bribed people, I'd be winning by more. || ...If I were bribing people, I'd be winning by a landslide. || ...Please. I don't need bribes. I have charm. || ...Cute accusation. Completely wrong, though.")); sb.End();
            sb.Branch("db_laugh"); sb.Narr(G.Sens >= 40 ? "One perfectly timed joke and the whole gym is laughing with her, not at her." : "Your joke gets a few polite laughs. The tension stays."); sb.End();
            return ch =>
            {
                switch (ch)
                {
                    case "db_defend": c.Support += 5; G.AddFavor(a, 4, 0); S.Reputation = Mathf.Clamp(S.Reputation + (Random.value < 0.5f ? 2 : -1), 0, 100); break;
                    case "db_proof": if (c.Approach == "cm_dig" || G.Int >= 45) { c.Support += 10; if (sc != null) Rumors.Close(sc, "cleared"); } else c.Support += 2; break;
                    case "db_her": c.Support += Social.Trust(a) >= 50 ? 7 : 3; Social.AddTrust(a, 4); break;
                    case "db_laugh": c.Support += G.Sens >= 40 ? 7 : 1; break;
                }
                c.Stage = 3;
            };
        }

        private static void CountScene(CouncilRace c, SaveData.Heroine a, SaveData.Heroine b, SceneBuilder sb)
        {
            sb.Bg("classroom_exam");
            sb.Narr("Friday. The votes are counted in the council room, and the results go up on the board after lunch.");
            sb.Say(a, T(a, "I can't look... || I'm too scared to look... || Please, you look first... I can't. || My heart is pounding... I can't watch.", "I can't LOOK! You look! || I can't watch! Tell me when it's over! || Ahhh! You look! I'll faint! || Nope, nope! You read it for me!", "...Let's see it. || ...Let's see the result. || ...Read it out. || ...Show me.", "Well? Don't keep me waiting. || Well? The suspense is boring me. || Come on, out with it. || Don't keep me waiting. Tell me."));
        }

        private static void Count(CouncilRace c, string choice)
        {
            if (c.Stage >= 4) return;
            var a = Social.Get(c.A);
            var b = Social.Get(c.B);
            var support = c.Support + Random.Range(-8, 9) + (c.Approach == "neutral" ? 5 : 0);
            var aWins = support >= 50;
            c.Winner = aWins ? c.A : c.B;
            c.Stage = 4;
            var win = Social.Get(c.Winner);
            var lose = aWins ? b : a;
            S.CouncilWinner = c.Winner;
            S.CouncilEffect = c.Approach == "cm_deal" ? (aWins ? c.Platform : c.RivalPlatform) : aWins ? c.Platform : c.RivalPlatform;
            S.CouncilEffectUntilWeek = S.Week + 2;
            if (win != null && c.Approach != "neutral") { G.AddFavor(win, aWins ? 6 : 1, 0); }
            if (lose != null && c.Approach != "neutral") Board.Add(Board.Make(RequestKind.Talk, lose, "Cheer up " + GirlInfo.FirstName(lose), GirlInfo.FirstName(lose) + " lost the council election.", 4));
            if (c.Approach == "cm_deal") Arcs.AddBond(c.A, c.B, 6);
            else Arcs.AddBond(c.A, c.B, -4);
            var text = GirlInfo.FirstName(c.Winner) + " won the student council election (" + PlatformText(S.CouncilEffect) + ")";
            Pairs.News(text + ".");
            Toasts.Show(text + "!");
            Anthology.Add("council", text, 3, c.A + "," + c.B);
            CampusLifePlugin.Log.LogInfo("Council race: " + text + " (support " + support + ")");
        }
    }
}
