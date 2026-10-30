using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    // Step numbers >= 90 mean "the story ends after this step".
    // Choices made in a scene arrive in OnChoice before the step is marked as done.

    /// <summary>She failed a test; tutor her (INT matters) before the make-up exam.</summary>
    internal sealed class MakeupExamArc : ArcDef
    {
        public MakeupExamArc() { Id = "makeup_exam"; Name = "The Make-up Exam"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs)
        {
            return h.relation >= 0 && (GirlInfo.Quirks(h).majime || gs.Mood == Mood.Stressed || !GirlInfo.Likes(h, "study"));
        }

        public override string StepHint(ArcCtx c) { return c.St.Step == 1 ? "study sessions " + c.St.Counter + "/2" : ""; }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " looks pale", c.Name + " was staring at a test paper all morning. Talk to her.", 3).Btn("Ask what's wrong");
                case 1: return Of(c, RequestKind.Study, "Tutor " + c.Name + " (" + (c.St.Counter + 1) + "/2)", "Invite " + c.Name + " to study together. Her make-up exam is close.", 5, 3);
                case 2: return Talk(c, c.H, c.Name + "'s make-up exam", "The make-up exam is over. Ask " + c.Name + " how it went.", 3).Btn("Ask about the make-up exam");
                default: return null;
            }
        }

        public override void OnStepDone(ArcCtx c, Request r)
        {
            if (c.St.Step == 1 && ++c.St.Counter < 2) return;
            c.St.Step++;
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            if (c.St.Step == 0)
            {
                b.Me("What's wrong? You look like you saw a ghost.");
                b.Her_(Lines.T(t, "I... I failed the math test. If I fail the make-up exam too, they'll call my parents... || I failed math... if I fail the make-up too, my parents will find out... || The math test... I failed. I'm so scared of the make-up... || I got a terrible score... and there's a retest soon...",
                    "I bombed the math test. Like, totally. There's a make-up exam and I'm DOOMED. || Math test? Total disaster! Make-up exam incoming! Help! || I failed! Spectacularly! And there's a retest! || Zero hope! Math destroyed me!",
                    "I failed a test. It happens. ...There's a make-up exam. I'd rather not fail twice. || I failed. A make-up exam follows. Failing twice is unacceptable. || Math. Failed. I need to pass the retest. || A failing grade. I intend to fix it.",
                    "Don't laugh. I failed the math test. If you laugh, you're dead. || I failed math. Laugh and I'll end you. || Don't you dare laugh. I failed. || Math beat me. Not a word."));
                b.Choice("I'll tutor you.", "help", "You'll be fine on your own.", "skip");
                b.Branch("help");
                b.Her_(Lines.T(t, "Really? You'd do that for me? Thank you... I'll do my best! || You'd help me? Thank you so much... I'll work hard! || Really? Thank you... I won't let you down. || You're so kind... let's study together!",
                    "Seriously?! You're a lifesaver! Let's start today! || You're my hero! Let's start now! || Yes! Study buddy! Today! || Awesome! Lessons start now!",
                    "...Fine. I accept. Don't waste my time. || ...Accepted. Don't waste my time. || ...Fine. Teach efficiently. || ...Good. Begin.",
                    "Oh? A private tutor? I'll be a very attentive student~ || A private tutor? I'll be attentive~ || Tutoring me? Lucky you. || Lessons with you? I'll pay attention. To you."));
                b.End();
                b.Branch("skip");
                b.Her_(Lines.T(t, "Oh... yeah. You're right. I'll try. || Oh... right. I'll try. || Mm... I guess so. || Okay... I'll do my best.", "Easy for you to say... || Easy for YOU to say! || Gee, thanks! || Ugh, sure!", "Hmph. Of course. || Hmph. Obviously. || ...Of course. || ...Naturally.", "Wow. Thanks for nothing. || Thanks for nothing. || Wow. So helpful. || Brilliant advice. Really."));
                b.End();
                return;
            }
            // Result: INT plus the study sessions decide it.
            int roll;
            var pass = Campus.Check(G.Int + 10 * c.St.Counter, 60, out roll);
            c.St.Counter = pass ? 100 : 50;
            b.Me("So? How did the make-up exam go?");
            if (pass)
                b.Her_(Lines.T(t, "I passed! I really passed! It's all thanks to you... thank you so much! || I passed! Thank you so much! || I really passed... it's all because of you! || I did it! Thank you!",
                    "I PASSED! Eighty-two points! You're the best professor ever! || PASSED! You're the best professor! || I passed! Treat time! || Yes! Passed! You rock!",
                    "I passed. With a decent score, even. ...Your explanations were good. || Passed. Good score. Your explanations helped. || A pass. Thanks to you. || Passed. You're a competent professor.",
                    "Passed~ Guess you're good for something after all. I owe you one... a big one. || Passed~ You're useful after all. I owe you. || Passed. I'll repay you. Generously. || Passed! You earned a reward."));
            else
                b.Her_(Lines.T(t, "I barely passed... but I passed. Thank you for staying with me. || I barely made it... thank you for staying with me. || Just barely... but I passed. Thanks. || Close, but I passed. Thank you.",
                    "Phew, just above the line! Close call, but we did it! || Just over the line! We did it! || Phew! Barely! But yes! || Close call! Passed!",
                    "Barely passed. Still, better than failing. || Barely. Better than failing. || Narrow pass. Acceptable. || Passed. Barely.",
                    "Scraped by. Next time, teach better~ Kidding. Thanks. || Scraped by. Teach better next time. Kidding. || Barely. Your fault. Joking. Thanks. || Close one. Thanks, sensei."));
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step == 0 && choice == "skip") { c.St.Step = 90; return; }
            if (c.St.Step == 2)
            {
                var pass = c.St.Counter >= 100;
                Reward(c, c.H, new Rewards { Favor = pass ? 12 : 6, Int = 2, Rep = 2 }, pass ? c.Name + " passed with a great score" : c.Name + " barely passed");
            }
        }
    }

    /// <summary>A sports club girl trains for a tournament. STR and support decide how she does.</summary>
    internal sealed class TournamentArc : ArcDef
    {
        public TournamentArc() { Id = "club_tournament"; Name = "The Tournament"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.relation >= 0 && GirlInfo.InSportsClub(h); }

        public override Request MakeStep(ArcCtx c)
        {
            var club = GirlInfo.ClubName(c.H);
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + "'s tournament", c.Name + " (" + club + ") has a tournament coming up. Talk to her.", 3).Btn("Ask about the tournament");
                case 1: return Of(c, RequestKind.Exercise, "Train with " + c.Name, "Invite " + c.Name + " to exercise together before the tournament.", 6, 3);
                case 2:
                {
                    var r = Board.Make(RequestKind.MeetAt, c.H, "Cheer at " + c.Name + "'s practice", "Find " + c.Name + " at the " + Places.Name(PracticePlace(c)) + " and cheer her on.", 5);
                    r.Place = PracticePlace(c);
                    r.DeadlineDay = Campus.S.Day + 2;
                    return r.Btn("Cheer her on");
                }
                case 3: return Talk(c, c.H, "Tournament results", "The tournament is over. Ask " + c.Name + " how it went.", 3).Btn("Ask about the tournament");
                default: return null;
            }
        }

        private static string PracticePlace(ArcCtx c)
        {
            var club = GirlInfo.ClubName(c.H).ToLower();
            if (club.Contains("swim")) return "pool";
            if (club.Contains("track") || club.Contains("soccer") || club.Contains("softball") || club.Contains("tennis")) return "field";
            return "gym";
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            switch (c.St.Step)
            {
                case 0:
                    b.Me("I heard there's a tournament coming up.");
                    b.Her_(Lines.T(t, "Yes... I'm a little nervous. Everyone's counting on me. || Yes... I'm nervous. Everyone's relying on me. || The tournament... I'm scared I'll let them down. || I'm a little anxious about the match...",
                        "Yup! And this year we're gonna WIN! ...Probably! Maybe! || Yep! We're gonna WIN! Probably! || This year's ours! Hopefully! || Tournament time! Victory! Maybe!",
                        "It's this weekend. I intend to win. Obviously. || This weekend. I'll win. || The match is soon. Victory is the plan. || I intend to take first place.",
                        "Coming to watch me? I look pretty good in my uniform, you know~ || Coming to watch? I look great in uniform~ || Watching me play? Good choice. || Come see me win. And look cute."));
                    b.Choice("Let me train with you.", "train", "Good luck out there.", "luck");
                    b.Branch("train");
                    b.Her_(Lines.T(t, "You'd train with me? Then... I'll count on you! || You'd train with me? I'll count on you! || Thank you... let's train together! || With you, I'll work harder!", "Yes! A training buddy! Don't you dare slack off! || Training buddy! No slacking! || Yes! Let's sweat! || Partner! Keep up!", "Try to keep up. || Keep up. || Don't fall behind. || Try not to slow me down.", "Hope you can handle my pace~ || Can you handle my pace~? || Hope you have stamina. || Let's see if you can keep up."));
                    b.End();
                    b.Branch("luck");
                    b.Her_(Lines.T(t, "Thank you. I'll do my best. || Thank you. I'll try my best. || Thanks... I'll do it. || Thank you so much.", "Thanks! Come cheer for me, okay? || Thanks! Come cheer! || Cheer for me! || Be there, okay?!", "Luck has nothing to do with it. || Luck is irrelevant. || I don't need luck. || Skill decides.", "Luck? I'll need more than that from you~ || Luck? I want more than that. || Just luck? Stingy. || I expected a kiss. Kidding."));
                    b.End();
                    break;
                case 2:
                    b.Narr(c.Name + " is drilling hard. Sweat runs down her face.");
                    b.Me("Go, " + c.Name + "! You've got this!");
                    if (G.Str >= 40)
                    {
                        b.Narr("You jump in and run the drills with her. She has to push herself to keep up with you.");
                        c.St.Counter += 2;
                    }
                    else c.St.Counter += 1;
                    b.Her_(Lines.T(t, "Hah... hah... thank you for coming. It really helps. || Thank you for coming... it helps so much. || You came... hah... thank you. || Your cheering helps me...",
                        "Did you see that?! I'm on fire today! || Did you see that?! On fire! || Woo! I'm unstoppable! || Watch me go!",
                        "...Your cheering is loud. ...It's not bad. || ...Loud cheering. Not bad. || ...I heard you. Thanks. || ...Your voice carried.",
                        "Watching me sweat, huh? Enjoying the view? || Enjoying watching me sweat? || Like the view? || Watching closely, hm?"));
                    break;
                case 3:
                {
                    var chance = 0.3f + c.H.favor / 300f + G.Str / 300f + 0.1f * c.St.Counter;
                    var won = Consent.Yes(c.H, "arc:won", chance);   // her bars, your STR and how often you came to cheer
                    c.St.Counter = won ? 100 : 0;
                    b.Me("So? How did the tournament go?");
                    if (won)
                        b.Her_(Lines.T(t, "We won! I scored the winning point... I kept thinking about how you trained with me. || We won! I thought of our training when I scored... || We won... thank you for training with me. || Victory... I couldn't have done it without you.",
                            "WE WON! Champions! I'm treating you to crepes, no arguing! || CHAMPIONS! Crepes on me! || We WON! Celebrate! || Victory! You're coming to the party!",
                            "First place. As expected. ...Your training helped. A little. A lot. || First place. Your training helped. Considerably. || We won. You contributed. || Champions. Thanks to you, partly.",
                            "We won~ So, what's my reward from you? || We won. My reward? || Victory. Now spoil me. || Won! What's my prize?"));
                    else
                        b.Her_(Lines.T(t, "We lost in the semifinals... but I'm not sad. I did my best, and you were there. || We lost... but I did my best, and you were there. || Semifinals... it's okay. You came. || We didn't win... but I'm not sad.",
                            "Lost by one point! ONE! ...Next year for sure! || Lost by ONE point! Next year! || So close! Next time! || Argh! One point! Next year!",
                            "We lost. I'll be stronger next time. || We lost. I'll improve. || Defeat. I'll train harder. || Lost. Lesson learned.",
                            "We lost. Comfort me properly, okay? || We lost. Comfort me. Properly. || Lost. Make me feel better. || Defeat. You owe me hugs."));
                    break;
                }
                default:
                    base.Scene(c, r, b);
                    break;
            }
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step == 0 && choice == "luck") c.St.Step = 1; // becomes 2 when the step is done: no training session
            if (c.St.Step == 3)
            {
                var won = c.St.Counter >= 100;
                Reward(c, c.H, new Rewards { Favor = won ? 12 : 7, Club = won ? 3 : 1, Rep = won ? 3 : 1, Str = 1 },
                    won ? c.Name + "'s team won the tournament" : c.Name + " fought well");
            }
        }
    }

    /// <summary>She lost a precious charm somewhere in school. Find it (errand), return it.</summary>
    internal sealed class LostCharmArc : ArcDef
    {
        private static readonly string[] Spots = { "library", "gym", "pool", "courtyard", "classroom", "rooftop", "infirmary" };

        public LostCharmArc() { Id = "lost_charm"; Name = "The Lost Charm"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.relation >= 0; }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " is searching for something", c.Name + " is looking under every desk. Ask her what's wrong.", 3).Btn("Ask what she lost");
                case 1:
                {
                    var spot = Spots[Random.Range(0, Spots.Length)];
                    var r = Board.Make(RequestKind.Errand, c.H, "Find " + c.Name + "'s charm",
                        "Someone saw a small charm at the " + Places.Name(spot) + ". Go there to find it, then give it back to " + c.Name + ".", 10, PeriodMask.Any, rep: 2);
                    r.Place = spot;
                    r.DeadlineDay = Campus.S.Day + 3;
                    return r.Btn("Give back her charm");
                }
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            if (c.St.Step == 0)
            {
                b.Me("Lose something?");
                b.Her_(Lines.T(t, "My charm... my grandma gave it to me. I had it this morning, and now it's gone... || My grandma's charm... it's gone... || I lost my charm... it means so much to me... || The charm my grandmother gave me... I can't find it...",
                    "My lucky charm's gone! I can't lose it, it's my LUCKY charm! || My lucky charm's missing! Emergency! || I lost my charm! Help me find it! || No! My charm! Gone!",
                    "A charm. It's not important. ...It's important. || A charm. Unimportant. ...It's important. || I lost something. A charm. It matters. || My charm is missing. I need it.",
                    "My charm. If you find it, I'll make it worth your while~ || Find my charm and I'll reward you~ || Lost my charm. Hero wanted. || My charm's gone. Find it, get a prize."));
                b.Me("I'll help you look for it.");
                return;
            }
            b.Me("Is this the one you lost?");
            b.Narr("You hold out a small, worn charm.");
            b.Her_(Lines.T(t, "That's it! You found it! Oh... thank you, thank you so much...! || You found it! Thank you so much! || That's it! Oh, thank you...! || My charm! Thank you, thank you!",
                "NO WAY! You found it! You're amazing! || You found it! Amazing! || YES! You're incredible! || No way! Found it! Hero!",
                "...That's it. You actually found it. I... thank you. || ...You found it. Thank you. || ...That's it. I'm grateful. || ...Found. Thank you. Truly.",
                "Oh! My hero~ Seriously, thanks. || My hero~ Thanks. || Look at you. Thanks, really. || Impressive. Thank you."));
            var opts = new List<string> { "Here you go.", "give" };
            if (G.Sens >= 40) { opts.Add("What do I get for finding it? [SEN]"); opts.Add("tease"); }
            b.Choice(opts.ToArray());
            b.Branch("give");
            b.Her_(Lines.T(t, "I'll never forget this. || I'll never forget this. || I'll always remember. || Thank you, forever.", "I owe you big time! || I owe you huge! || Big debt! || I'll pay you back!", "I'll repay you. Someday. || I'll repay you. || A debt. Noted. || Someday, I'll repay this.", "You're too nice. That's dangerous~ || Too nice. Dangerous~ || You're dangerously sweet. || Careful, I'll fall for you."));
            b.End();
            b.Branch("tease");
            if (c.H.lewdness >= 50 || c.H.isGirlfriend)
                b.Her_(Lines.T(t, "W-what do you want...? M-maybe... a kiss on the cheek? || A reward...? Maybe... a kiss on the cheek? || Um... would a little kiss be okay? || W-what would you like...? A kiss?", "Ha! Fine, a reward! Close your eyes... *smooch* || Reward time! Eyes closed! *smooch* || Fine! Close your eyes! Hehe! || Okay! Cheek kiss! Ready?!", "...Close your eyes. Don't tell anyone. || ...Close your eyes. Secret. || ...Eyes shut. Tell no one. || ...Don't look. Quickly.", "Hmm~ How about I tell you later... in private? || Later. In private~ || Hmm. I'll pay you somewhere quiet. || Patience. Private reward later."));
            else
                b.Her_(Lines.T(t, "Eh? Um... a thank-you note? || A thank-you card? || Um... a letter of thanks? || My gratitude?", "My eternal gratitude! That's a lot! || Eternal gratitude! Huge! || My thanks! Forever! || Gratitude! Lots!", "My thanks. Don't push it. || Thanks. Enough. || My gratitude. That's all. || Thanks. Don't push.", "Nice try~ || Nice try~ || Cute. No. || Ha. No."));
            b.End();
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step == 1 && choice == "tease" && (c.H.lewdness >= 50 || c.H.isGirlfriend))
                Reward(c, c.H, new Rewards { Lewd = 10, Sens = 1 }, c.Name + " gave you a little reward");
        }
    }

    /// <summary>You discover her secret hobby. Keep it, share it, and win her heart.</summary>
    internal sealed class SecretHobbyArc : ArcDef
    {
        public SecretHobbyArc() { Id = "secret_hobby"; Name = "Her Secret Hobby"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.relation >= 0 && GirlInfo.LikesOf(h).Count > 0; }

        private static string Like(ArcCtx c)
        {
            if (c.St.Counter <= 0 || c.St.Counter > GirlInfo.LikeIds.Length)
            {
                var likes = GirlInfo.LikesOf(c.H);
                var like = likes.Count > 0 ? likes[Random.Range(0, likes.Count)] : "sweets";
                c.St.Counter = Array.IndexOf(GirlInfo.LikeIds, like) + 1;
            }
            return GirlInfo.LikeIds[c.St.Counter - 1];
        }

        private static string Place(string like)
        {
            switch (like)
            {
                case "animals": return "park";
                case "study": return "library";
                case "exercise": return "gym";
                case "fashion": return "courtyard";
                default: return "cafeteria";
            }
        }

        private static string Activity(string like)
        {
            switch (like)
            {
                case "animals": return "feeding the stray cats behind the university";
                case "study": return "reading a thick novel, totally absorbed";
                case "exercise": return "practicing moves alone in an empty gym";
                case "fashion": return "sketching outfit designs in a notebook";
                case "cooking": return "reading a recipe book and taking notes";
                case "coffee": return "brewing coffee with a tiny hand grinder";
                case "spicy": return "eating ridiculously spicy chips without blinking";
                case "sweets": return "sneaking a whole box of sweets";
                default: return "stuffing her face with a giant sandwich";
            }
        }

        public override Request MakeStep(ArcCtx c)
        {
            var like = Like(c);
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, "You saw " + c.Name + "...", "You caught " + c.Name + " " + Activity(like) + ". She looked embarrassed. Talk to her about it.", 3).Btn("Ask about her hobby");
                case 1:
                {
                    var p = Place(like);
                    var r = Board.Make(RequestKind.MeetAt, c.H, "Join " + c.Name + " at the " + Places.Name(p), c.Name + " wants to show you her hobby. Meet her at the " + Places.Name(p) + ".", 6);
                    r.Place = p;
                    r.DeadlineDay = Campus.S.Day + 3;
                    return r.Btn("Join her hobby");
                }
                case 2:
                {
                    var r = Of(c, RequestKind.Gift, "A present for " + c.Name, "Surprise " + c.Name + " with something related to her hobby (" + GirlInfo.LikeLabel(like) + ").", 10, 4);
                    r.Item = like;
                    return r;
                }
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            var like = Like(c);
            if (c.St.Step == 0)
            {
                b.Me("So... I saw you earlier.");
                b.Her_(Lines.T(t, "Y-you saw that?! Please don't tell anyone... it's embarrassing... || You saw?! Please don't tell... || Oh no... you saw that... it's so embarrassing... || P-please keep it secret...",
                    "Ah! You saw?! Okay, okay, yes, I love " + GirlInfo.LikeLabel(like) + ". Happy? || You saw?! Fine, I love " + GirlInfo.LikeLabel(like) + "! Happy?! || Caught! Yes, " + GirlInfo.LikeLabel(like) + "! || Argh! Okay! " + GirlInfo.LikeLabel(like) + " is my thing!",
                    "...You saw nothing. || ...You saw nothing. || ...Forget it. || ...That didn't happen.",
                    "Spying on me? That's a bit creepy. ...Fine, you know my secret now. || Spying? Creepy. Fine, you know. || Peeking? Fine. Secret's out. || Caught me. Now you owe silence."));
                var opts = new List<string> { "Your secret is safe with me.", "keep", "That's actually really cool.", "cool" };
                if (G.Sens >= 50) { opts.Add("What will you do to keep me quiet? [SEN]"); opts.Add("tease"); }
                b.Choice(opts.ToArray());
                b.Branch("keep");
                b.Her_(Lines.T(t, "Thank you... You're really kind. || Thank you... you're so kind. || Thank you for keeping it secret... || You're really kind.", "Pinky promise? Okay! Thanks! || Pinky promise! Thanks! || Promise?! Thanks! || Deal! Thanks!", "...Good. I trust you, then. || ...Good. I trust you. || ...Trust given. || ...Fine.", "Good boy~ || Good boy~ || Smart. || Good choice~"));
                b.End();
                b.Branch("cool");
                b.Her_(Lines.T(t, "You think so? ...Nobody ever said that before. || Really? Nobody's said that before... || You think so? That's nice... || No one ever told me that...", "Right?! Finally someone gets it! || Finally someone gets it! || Right?! Yes! || See?! You get it!", "...You're strange. But thank you. || ...Strange. Thank you. || ...Odd. Thanks. || ...Unexpected. Thank you.", "Heh. Flattery works on me, just so you know. || Flattery works. Just so you know. || Keep flattering. || Oh, sweet talk. Continue."));
                b.End();
                b.Branch("tease");
                if (c.H.lewdness >= 40)
                    b.Her_(Lines.T(t, "Eh?! Um... w-whatever you want...? || Um... w-whatever you want...? || Eh?! W-what do you want...? || Um... okay...?", "Ooh, blackmail? Kinky. I'm in. || Blackmail? Kinky. I'm in. || Ooh, a deal! Okay! || Bribery? Fun!", "...You're terrible. Fine. We'll talk later. || ...Terrible. Later. || ...You're awful. We'll discuss. || ...Fine. Later.", "Now you're speaking my language~ || Now we're talking~ || My language~ || Ooh. Negotiations."));
                else
                    b.Her_(Lines.T(t, "That's mean... || That's mean... || So mean... || Don't be cruel...", "Not funny! || Not funny! || Rude! || Hey!", "Tell anyone and you die. || Tell anyone and you die. || Silence. Or else. || Breathe a word and regret it.", "Ha. In your dreams. || In your dreams. || Ha. Never. || Nice try."));
                b.End();
                return;
            }
            b.Narr("You spend a while with " + c.Name + ", and she shows you all about " + GirlInfo.LikeLabel(like) + ".");
            b.Her_(Lines.T(t, "It's nice to share this with someone. With you. || I'm glad I can share this with you. || It's nice to have someone to share it with. || Sharing this with you makes me happy.",
                "See?! Isn't it the best?! Let's do this again! || Best, right?! Again! || Isn't it great?! || So fun! Let's do it more!",
                "You didn't laugh. ...I'm glad it was you who found out. || You didn't laugh. I'm glad it was you. || ...Good that it's you. || ...You understood. Thanks.",
                "Not bad, huh? Now you know one of my secrets. Want to know more? || One secret down. Want more? || Now you know. More secrets? || Like it? There's more."));
            var gs = c.Gs;
            gs.KnownLikes |= 1 << (c.St.Counter - 1);
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step != 0) return;
            if (choice == "cool") G.AddFavor(c.H, 3, 0);
            if (choice == "keep") G.AddFavor(c.H, 3, 0);
            if (choice == "tease")
            {
                if (c.H.lewdness >= 40) Reward(c, c.H, new Rewards { Lewd = 8 }, c.Name + " liked your boldness");
                else G.AddFavor(c.H, -4, 0);
            }
        }
    }

    /// <summary>Two girls clash. Side with one, defend the other, or mediate (INT).</summary>
    internal sealed class RivalryArc : ArcDef
    {
        public RivalryArc() { Id = "rivalry"; Name = "Rivals"; Drama = true; NeedsOther = true; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.relation >= 1 || h.isGirlfriend; }

        public override bool OtherOk(SaveData.Heroine h, SaveData.Heroine o)
        {
            if (!base.OtherOk(h, o)) return false;
            int bond;
            Campus.S.Girl(GirlInfo.Key(h)).Bonds.TryGetValue(GirlInfo.Key(o), out bond);
            return bond <= 10;
        }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " is fuming", c.Name + " stormed out of class after arguing with " + c.OName + ". Talk to her.", 3).Btn("Ask about the fight");
                case 1: return Talk(c, c.O, "Talk to " + c.OName, "Hear " + c.OName + "'s side of the fight with " + c.Name + ".", 3).Btn("Hear her side");
                case 2: return Talk(c, c.H, "Back to " + c.Name, "Tell " + c.Name + " what happened with " + c.OName + ".", 3).Btn("Tell her what happened");
                default: return null;
            }
        }

        public override string StepHint(ArcCtx c)
        {
            return c.St.Counter == 1 ? "you sided with " + c.Name : c.St.Counter == 2 ? "you defended " + c.OName : c.St.Counter == 3 ? "you are mediating" : "";
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            var ot = c.OTone;
            switch (c.St.Step)
            {
                case 0:
                {
                    b.Me("Hey, what happened back there?");
                    b.Her_(Lines.T(t, "It's " + c.OName + "... she said things about me behind my back. I thought we were okay... || It's " + c.OName + "... she talked behind my back..." + " || " + c.OName + " said mean things about me... I thought we were friends." + " || " + c.OName + "... she hurt me.",
                        "Ugh, " + c.OName + " is SO annoying! She took my spot and acted like it was nothing!" + " || " + c.OName + " is SO annoying! || Ugh! " + c.OName + " stole my spot!" + " || " + c.OName + "! Argh!",
                        c.OName + " took credit for my work. I won't forgive that." + " || " + c.OName + " took my credit. Unforgivable." + " || " + c.OName + " wronged me." + " || " + c.OName + ". I won't forgive her.",
                        c.OName + " has been flirting with you, hasn't she? Don't play dumb." + " || " + c.OName + "'s been flirting with you. Don't deny it." + " || " + c.OName + " has eyes for you. I noticed." + " || " + c.OName + ". You know why."));
                    var opts = new List<string> { "You're right, that's not okay.", "side", "Maybe it was a misunderstanding.", "defend" };
                    if (G.Int >= 40) { opts.Add("Let me talk to both of you. [INT]"); opts.Add("mediate"); }
                    b.Choice(opts.ToArray());
                    b.Branch("side");
                    b.Her_(Lines.T(t, "You believe me... thank you. || You believe me... thank you. || Thank you for believing me. || You're on my side... thanks.", "See?! Even you think so! || See?! You agree! || Right?! || Ha! See!", "...At least you understand. || ...You understand. || ...Good. || ...At least you see it.", "Good answer~ || Good answer~ || Correct. || Smart boy."));
                    b.End();
                    b.Branch("defend");
                    b.Her_(Lines.T(t, "You're taking her side...? || You're on her side...? || You think she's right...? || Oh... you side with her...", "Huh?! Whose friend are you?! || Whose friend ARE you?! || Seriously?! Her?! || Traitor!", "...I see. || ...I see. || ...Noted. || ...Understood.", "Oh, so that's how it is. || Oh, so that's how it is. || Interesting choice. || Wow. Okay."));
                    b.End();
                    b.Branch("mediate");
                    b.Her_(Lines.T(t, "You'd do that? ...Okay. I'll trust you. || You'd do that? Okay, I trust you. || You'll help? ...Alright. || Thank you... I'll trust you.", "Ugh, fine. But she has to say sorry first! || Fine! But she apologizes first! || Okay! She says sorry first! || Ugh, okay! Her first!", "...Fine. Try it. || ...Try it. || ...Fine. Attempt it. || ...Go ahead.", "Playing peacemaker? Cute. Go ahead. || Peacemaker? Cute. Go. || Diplomat? Adorable. Try. || Playing hero? Go on."));
                    b.End();
                    break;
                }
                case 1:
                {
                    b.Me(c.Name + " is pretty upset about earlier.");
                    b.Her_(Lines.T(ot, "I know... I didn't mean it like that. I just didn't know how to say sorry. || I didn't mean it... I didn't know how to apologize. || I never meant to hurt her... || I wanted to say sorry, but...",
                        "She's upset?! SHE started it! || She's upset?! She started it! || What?! She started it! || Her? Upset? Seriously?!",
                        "Her reaction was excessive. ...Though I may have been careless. || Excessive reaction. Though I was careless. || ...Perhaps I was thoughtless. || ...Partly my fault. Partly.",
                        "Oh, she sent you? How cute. Tell her to come herself. || She sent you? Tell her to come herself. || Messenger boy? Cute. Send her. || She can come herself."));
                    if (c.St.Counter == 3)
                    {
                        int roll;
                        var ok = Campus.Check(G.Int, 70, out roll);
                        c.St.Choice = ok ? "peace" : "nope";
                        if (ok)
                        {
                            b.Narr("You calmly explain how " + c.Name + " sees it. Little by little, " + c.OName + "'s expression softens.");
                            b.Her_(Lines.T(ot, "...You're right. I'll apologize to her. Thank you. || You're right. I'll apologize. Thank you. || I'll say sorry to her. Thanks. || Okay... I'll make up with her.", "...Okay, okay. I'll talk to her. Happy? || Okay! I'll talk to her! || Fine! I'll fix it! || Alright, alright!", "Very well. I'll speak to her. || Very well. I'll speak with her. || ...Fine. I'll talk. || ...I'll handle it.", "Fine, fine. You win. I'll make up with her. || You win. I'll make up. || Fine. Peace. || Okay, okay. I'll apologize."));
                        }
                        else
                        {
                            b.Narr("You try to reason with her, but she isn't ready to listen.");
                            b.Her_(Lines.T(ot, "I'm sorry... I need more time. || Sorry... I need more time. || I can't yet... || Not yet... please.", "Nope! Not happening! || Nope! || Not happening! || No way!", "Drop it. || Drop it. || No. || Enough.", "Nice try, peacemaker. || Nice try, peacemaker. || Cute. No. || Not today."));
                        }
                    }
                    else if (c.St.Counter == 2)
                        b.Her_(Lines.T(ot, "You stood up for me? ...Thank you. That means a lot. || You stood up for me...? Thank you. || You defended me... that means so much. || Thank you for protecting me.", "Wait, you defended me? Huh. You're alright! || You defended me?! You're alright! || Whoa! Thanks! || You had my back! Cool!", "...You defended me. I won't forget it. || ...You defended me. Remembered. || ...I won't forget. || ...Thank you for that.", "Taking my side? I like you more already~ || My side? I like you more. || Defending me? Good boy. || Smart choice. I like you."));
                    else
                        b.Her_(Lines.T(ot, "So you're on her side... I see. || You're on her side... I see. || So it's her... okay. || I understand... her side.", "Whatever! Go hang out with HER then! || Go hang with HER then! || Fine! Go! || Whatever! Her!", "Then we have nothing to discuss. || Nothing to discuss. || Then we're done. || Understood. Goodbye.", "Wow. Okay. Noted. || Noted. || Wow. Okay. || Remembered."));
                    break;
                }
                default:
                {
                    b.Me("I talked to " + c.OName + ".");
                    if (c.St.Counter == 3 && c.St.Choice == "peace")
                        b.Her_(Lines.T(t, "She apologized to me... We're okay now. You really did it. || She apologized... we're okay. You did it. || We made up... thank you so much. || She said sorry... it's all thanks to you.", "She said sorry! We're friends again! You're a genius! || Friends again! Genius! || She said sorry! You're amazing! || Made up! Woo!", "We talked. It's settled. ...Thank you. || Settled. Thank you. || Resolved. Thanks. || We talked. Thank you.", "She apologized. Impressive. You're full of surprises. || She apologized. Impressive. || You fixed it. Surprising. || Full of surprises, aren't you?"));
                    else if (c.St.Counter == 2)
                        b.Her_(Lines.T(t, "...I guess I overreacted too. But you didn't have to take her side. || I overreacted too... but you didn't have to side with her. || Maybe I was wrong too... still... || I guess... but it hurt.", "Hmph. Fine. Whatever. || Hmph! Fine! || Whatever! || Ugh! Fine!", "...Noted. || ...Noted. || ...Hm. || ...I see.", "I'll remember this. || I'll remember this. || Noted for later. || Don't forget I remember."));
                    else
                        b.Her_(Lines.T(t, "Thank you for being on my side. || Thank you for being on my side. || You were with me... thank you. || Thanks for supporting me.", "Thanks for having my back! || Thanks for backing me! || You had my back! || Thanks, partner!", "You were on my side. Good. || You sided with me. Good. || Good choice. || You supported me. Noted.", "You chose me. Smart. || You chose me. Smart. || Wise choice. || Of course you picked me."));
                    break;
                }
            }
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step == 0)
            {
                c.St.Counter = choice == "defend" ? 2 : choice == "mediate" ? 3 : 1;
                return;
            }
            if (c.St.Step == 2)
            {
                switch (c.St.Counter)
                {
                    case 1:
                        Reward(c, c.H, new Rewards { Favor = 10, Rep = 1 }, c.Name + " trusts you more");
                        G.AddFavor(c.O, -5, 0);
                        Bond(c, -20);
                        break;
                    case 2:
                        G.AddFavor(c.H, -3, 0);
                        Reward(c, c.O, new Rewards { Favor = 10 }, c.OName + " won't forget you defended her");
                        Bond(c, -10);
                        break;
                    default:
                        Reward(c, c.H, new Rewards { Favor = 8, Rep = 3 }, "You made peace between " + c.Name + " and " + c.OName);
                        G.AddFavor(c.O, 8, 0);
                        Bond(c, 40);
                        break;
                }
            }
        }

        public override void OnStepDone(ArcCtx c, Request r)
        {
            // Mediation result is decided in the scene of step 1; remember it in the counter.
            if (c.St.Step == 1 && c.St.Counter == 3 && c.St.Choice != "peace") c.St.Counter = 4; // failed mediation
            c.St.Step++;
        }
    }

    /// <summary>A shy girl wants to befriend another girl. Introduce them.</summary>
    internal sealed class FriendshipArc : ArcDef
    {
        public FriendshipArc() { Id = "friendship_bridge"; Name = "A New Friend"; NeedsOther = true; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs)
        {
            return h.relation >= 0 && (GirlInfo.Quirks(h).hitori || gs.Mood == Mood.Lonely || gs.Attention < 30);
        }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " wants to ask you something", c.Name + " keeps looking at you and then at " + c.OName + ". Talk to her.", 3).Btn("Ask what she wants");
                case 1: return Talk(c, c.O, "Introduce " + c.Name, "Tell " + c.OName + " about " + c.Name + " and ask her to have lunch with you two.", 4).Btn("Talk about her friend");
                case 2: return Of(c, RequestKind.Lunch, "Lunch: " + c.Name + " and " + c.OName, "Invite " + c.Name + " to lunch. " + c.OName + " promised to come too.", 6, 3, PeriodMask.Lunch);
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            if (c.St.Step == 0)
            {
                b.Me("You look like you want to say something.");
                b.Her_(Lines.T(t, "Um... you know " + c.OName + ", right? I'd like to be friends with her, but I never know what to say... || You know " + c.OName + "? I'd like to be friends with her... || Um... could you help me talk to " + c.OName + "? || I want to be friends with " + c.OName + ", but I'm shy...",
                    "Hey, you're friends with " + c.OName + ", right? She seems super fun! Introduce me! || You know " + c.OName + "?! Introduce me!" + " || " + c.OName + " seems fun! Help me meet her! || Hook me up with " + c.OName + "! As friends!",
                    "You know " + c.OName + ". ...I wouldn't mind getting to know her. That's all." + " || " + c.OName + ". I'd like to know her. || Introduce me to " + c.OName + "." + " || " + c.OName + " seems worthwhile. Arrange it.",
                    c.OName + " seems interesting. Be a dear and introduce us? || Introduce me to " + c.OName + ", dear?" + " || " + c.OName + " intrigues me. Make it happen. || Be useful. Introduce " + c.OName + "."));
                b.Me("Leave it to me.");
                return;
            }
            b.Me("Hey, do you know " + c.Name + "? She'd like to have lunch with us sometime.");
            b.Her_(Lines.T(c.OTone, "Oh, " + c.Name + "? I've always wanted to talk to her! Of course!" + " || " + c.Name + "? I've wanted to talk to her! || Oh, " + c.Name + "! Of course! || I'd love to meet " + c.Name + "!",
                "Sure! The more the merrier! || Sure! More friends! || Yes! Let's go! || The more the better!",
                "...If you're there, I suppose it's fine. || ...With you there, fine. || ...Acceptable. || ...If you're present.",
                "Ooh, matchmaking? Fine, I'm curious. || Matchmaking? I'm curious. || Fine. Intriguing. || Ooh, a setup? Okay."));
        }

        public override void OnStepDone(ArcCtx c, Request r)
        {
            if (c.St.Step == 2)
            {
                Bond(c, 30);
                Reward(c, c.H, new Rewards { Favor = 8, Rep = 2 }, c.Name + " and " + c.OName + " are friends now");
                G.AddFavor(c.O, 5, 0);
                c.Gs.Attention = 80;
            }
            c.St.Step++;
        }
    }

    /// <summary>"Practice" a confession with her... or is it practice?</summary>
    internal sealed class ConfessionPracticeArc : ArcDef
    {
        public ConfessionPracticeArc() { Id = "confession_practice"; Name = "Confession Practice"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return !h.isGirlfriend && h.favor >= 70 && h.relation >= 1; }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " has a strange request", c.Name + " asked to talk to you about something 'for a friend'.", 3).Btn("Hear her request");
                case 1:
                {
                    var r = Board.Make(RequestKind.MeetAt, c.H, "Rooftop, after class", c.Name + " wants to 'practice' on the rooftop after class.", 5, PeriodMask.After);
                    r.Place = "rooftop";
                    r.DeadlineDay = Campus.S.Day + 3;
                    return r.Btn("Start the practice");
                }
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            if (c.St.Step == 0)
            {
                b.Her_(Lines.T(t, "My friend wants to confess to someone, and... she asked me to practice with her. But I need someone to practice on... would you help? || My friend wants to confess... would you help me practice? || Could you be my practice partner? For a friend... || I need to practice a confession... for a friend. Help?",
                    "So! A friend of mine wants to confess and I'm helping her practice! Be my target, okay?! || Friend's confessing! Be my practice target! || Practice dummy wanted! You! || Help me rehearse! For a friend!",
                    "A friend needs help with a confession. I need a practice partner. You'll do. || A friend's confession. You're my practice partner. || I need a target for practice. You. || Practice partner required. You'll do.",
                    "I need to practice a confession. For a friend, obviously. You're the dummy~ || Practice confession. For a friend. Obviously. || You're my practice dummy. For a friend~ || Rehearsal time. Totally for a friend."));
                b.Me("Uh... sure. Where?");
                b.Her_(Lines.T(t, "The rooftop... after class. Please don't be late. || The rooftop, after class. Don't be late. || Meet me on the roof after class... please. || After class... the rooftop.", "Rooftop, after class! It's the classic spot! || Rooftop! After class! Classic! || Roof! After class! || Rooftop time! Be there!", "Rooftop. After class. || Rooftop. After class. || The roof. Later. || After class. Rooftop.", "The rooftop after class. Very cliché. Perfect. || Rooftop after class. Cliché. Perfect. || The roof. Very dramatic. || Rooftop. Don't be late."));
                return;
            }
            b.Narr("The wind is gentle on the rooftop. " + c.Name + " takes a deep breath.");
            b.Her_(Lines.T(t, "I... I've liked you for a long time. Every day, I look for you in the hallway... W-will you go out with me? || I've liked you for so long... will you go out with me? || Every day I look for you... please, date me. || I really like you... will you be my boyfriend?",
                "I like you! A lot! Like, really a lot! Go out with me! || I like you! So much! Go out with me! || I LIKE YOU! Date me! || You! Me! Dating! Please!",
                "I like you. I've thought about it carefully, and it's the only answer. Date me. || I like you. Date me. || I've decided. I like you. Go out with me. || My answer is you. Date me.",
                "I like you. Not as a joke. Not as practice... Wait. Pretend I didn't say that last part. || I like you. For real. ...Pretend I didn't say that. || Not practice. ...Wait, forget that. || I like you. Oops. That wasn't practice."));
            var opts = new List<string> { "That was great practice!", "practice", "Was that really practice?", "really" };
            if (G.Sens >= 30 || G.Int >= 50) { opts.Add("I like you too."); opts.Add("too"); }
            b.Choice(opts.ToArray());
            b.Branch("practice");
            b.Her_(Lines.T(t, "R-really? Ahaha... yeah. Practice. Of course. || R-right... practice. Of course. || Ahaha... yes. Just practice. || Oh... practice. Right.", "Haha... yeah! Totally! Practice! || Haha! Totally practice! || Yeah! Practice! Obviously! || Right! Just pretend!", "...Of course. Practice. || ...Practice. Yes. || ...Of course. || ...Naturally.", "...Idiot. || ...Idiot. || ...Dummy. || ...Clueless."));
            b.End();
            b.Branch("really");
            b.Her_(Lines.T(t, "...Maybe not. Don't make me say it twice. || ...Maybe not. || ...It might not have been. || ...Don't make me say it.", "D-don't ask that! ...Maybe it wasn't! || Don't ask! Maybe not! || Stop asking! ...Maybe! || Ugh! Maybe it wasn't!", "...You figure it out. || ...Figure it out. || ...You decide. || ...Think.", "What do you think~? || What do you think~? || Guess~ || You tell me~"));
            b.End();
            b.Branch("too");
            b.Her_(Lines.T(t, "Huh...? You... you mean it? I'm so happy... Then next time, please say it properly. For real. || You mean it...? I'm so happy... say it properly next time. || Really...? Then confess for real, okay? || I'm so happy... please say it again, properly.",
                "WAIT, REALLY?! Okay, okay, I'm not crying! Say it again for real sometime! || REALLY?! Not crying! Say it for real! || Wait! Yes! Say it properly! || OMG! Again! For real!",
                "...I see. Then I expect you to confess properly. Soon. || ...Then confess properly. Soon. || ...I expect a real confession. || ...Good. Properly, next time.",
                "...Took you long enough. Now do it right, with a real confession, okay? || Took you long enough. Do it right. || Finally. A real confession, please. || About time. Properly, now."));
            b.End();
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step != 1) return;
            switch (choice)
            {
                case "too":
                    c.H.favor = 100;
                    Reward(c, c.H, new Rewards { Lewd = 10, Rep = 1 }, c.Name + " is waiting for your real confession");
                    Campus.S.Log(c.Name + " is ready to hear a real confession from you.");
                    break;
                case "really":
                    Reward(c, c.H, new Rewards { Favor = 10 }, c.Name + "'s heart is racing");
                    break;
                default:
                    G.AddFavor(c.H, -2, 0);
                    break;
            }
        }
    }

    /// <summary>For girlfriends: a study date that turns into a long evening.</summary>
    internal sealed class LateStudyArc : ArcDef
    {
        public LateStudyArc() { Id = "late_night_study"; Name = "Study Date"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.isGirlfriend; }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " has a plan", c.Name + " texted you: 'Let's make a plan!' Talk to her.", 3).Btn("Hear her plan");
                case 1: return Of(c, RequestKind.Study, "Study date with " + c.Name, "Invite " + c.Name + " to study together.", 6, 3);
                case 2: return Of(c, RequestKind.WalkHome, "Walk " + c.Name + " home", "After studying, walk " + c.Name + " home.", 6, 3, PeriodMask.After);
                case 3:
                    if (!Cfg.LewdRequests.Value || G.Sens < 40) return null;
                    var r = Of(c, RequestKind.Intimate, "Stay a little longer", c.Name + " doesn't want the day to end yet... (H with her)", 8, 3);
                    r.Lewd = true;
                    r.Reward.Lewd = 10;
                    return r;
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            b.Her_(Lines.T(c.Tone, "Exams are coming... so I thought, maybe we could study together? Just the two of us. || Exams are close... study together? Just us? || Could we study together? Just the two of us... || I'd like to study with you...",
                "Study date! Snacks, notes, and you! Best plan ever! || Study date! Snacks included! || Study party! Us two! || Let's study! Together! Fun!",
                "We study together this week. That's decided. || We study together. Decided. || This week, we study. Together. || Study session. You and me.",
                "Let's study together. I'll even let you look at my notes... and other things~ || Study with me. I'll show my notes. And more. || Let's study. Among other things~ || Study date? I'll make it interesting."));
            b.Me("It's a date.");
        }

        public override void OnStepDone(ArcCtx c, Request r)
        {
            if (c.St.Step == 3 || (c.St.Step == 2 && (!Cfg.LewdRequests.Value || G.Sens < 40)))
                Reward(c, c.H, new Rewards { Favor = 5, Int = 1 }, "A perfect study date with " + c.Name);
            c.St.Step++;
        }
    }

    /// <summary>She is having a terrible day. Reading her right (INT) matters.</summary>
    internal sealed class BadDayArc : ArcDef
    {
        public BadDayArc() { Id = "bad_day"; Name = "A Bad Day"; }

        public override bool Eligible(SaveData.Heroine h, GirlState gs) { return h.relation >= 0 && (gs.Mood == Mood.Upset || h.isAnger); }

        public override Request MakeStep(ArcCtx c)
        {
            switch (c.St.Step)
            {
                case 0: return Talk(c, c.H, c.Name + " had a bad day", c.Name + " looks like she's about to cry or punch someone. Talk to her.", 3, 1).Btn("Comfort her");
                case 1:
                {
                    var r = Of(c, RequestKind.Gift, "Cheer " + c.Name + " up", "Bring " + c.Name + " something sweet to cheer her up.", 8, 2);
                    r.Item = "sweets";
                    return r;
                }
                default: return null;
            }
        }

        public override void Scene(ArcCtx c, Request r, SceneBuilder b)
        {
            var t = c.Tone;
            b.Me("Hey... are you okay?");
            b.Her_(Lines.T(t, "N-no... everything went wrong today. I forgot my assignments, I tripped in the hallway, and... || Everything went wrong today... || I forgot everything and tripped... it's awful... || Today's been terrible...",
                "NO! Today is the WORST! Everything is going wrong! || WORST day! Everything's broken! || Nothing's going right! || Today hates me!",
                "...Not now. || ...Not now. || ...Leave it. || ...Bad day.",
                "Do I LOOK okay? || Do I look okay? || Does it look like it? || Obviously not."));
            var opts = new List<string> { "Want to talk about it?", "listen", "Want to hear a joke?", "joke" };
            if (G.Int >= 30) { opts.Add("I'll just sit here with you. [INT]"); opts.Add("sit"); }
            b.Choice(opts.ToArray());
            b.Branch("listen");
            b.Her_(Lines.T(t, "...Thank you for listening. I feel a bit lighter. || Thank you for listening. || I feel better... thank you. || Thanks for hearing me out.", "...Yeah. Thanks. Venting helps. || Thanks! Venting helps! || Yeah. Thanks! || That helped!", "...You're a decent listener. || ...Decent listener. || ...Thanks. || ...Helpful.", "...Fine. Thanks for not running away. || Thanks for not running. || You stayed. Thanks. || You didn't flee. Impressive."));
            b.End();
            b.Branch("joke");
            b.Her_(Lines.T(t, "...Pfft. That was so bad. ...Thanks. || Pfft... so bad. Thanks. || Hehe... that was silly. Thank you. || That was awful... I laughed.", "HAHA! That's so dumb! ...I needed that. || HAHA! Dumb! Needed that! || So dumb! Thanks! || Ahaha! Perfect!", "...That's not funny. ...Okay, it's a little funny. || ...Not funny. ...A little. || ...Idiot. ...Funny. || ...Hm. Amusing.", "Seriously? ...Ugh, I laughed. You win. || Ugh, I laughed. You win. || Fine. Funny. || Seriously? ...Okay, I laughed."));
            b.End();
            b.Branch("sit");
            b.Narr("You sit next to her in silence. After a while, she leans on your shoulder.");
            b.Her_(Lines.T(t, "...You always know what I need. || You always know what I need. || You're always here for me... || This is exactly what I needed.", "...This is nice. Thanks. || This is nice. Thanks. || Thanks! This helps! || Mm! Nice!", "...Stay a bit longer. || ...Stay longer. || ...Don't go yet. || ...A little longer.", "...Don't tell anyone I did this. || Don't tell anyone. || Secret, okay? || Nobody hears about this."));
            b.End();
        }

        public override void OnChoice(ArcCtx c, Request r, string choice)
        {
            if (c.St.Step != 0) return;
            int roll;
            switch (choice)
            {
                case "sit":
                    Reward(c, c.H, new Rewards { Favor = 8 }, c.Name + " felt understood");
                    break;
                case "joke":
                    var t = c.Tone;
                    if (t == Tone.Energetic || t == Tone.Teasing) G.AddFavor(c.H, 4, 0);
                    else G.AddFavor(c.H, -2, 0);
                    break;
                default:
                    if (Campus.Check(G.Int, 50, out roll)) G.AddFavor(c.H, 5, 0);
                    break;
            }
        }

        public override void OnStepDone(ArcCtx c, Request r)
        {
            if (c.St.Step == 1)
            {
                c.H.isAnger = false;
                c.H.anger = 0;
                c.Gs.Mood = Mood.Cheerful;
                Toasts.Show(c.Name + " feels much better.");
            }
            c.St.Step++;
        }
    }
}
