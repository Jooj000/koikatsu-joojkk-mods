using System;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>The spotlight episodes' text (see <see cref="Episodes"/>): friendship stories, with romantic endings when you are alone with her.</summary>
    internal static partial class Episodes
    {
        private static bool Roll(int stat, int difficulty) { return stat + Random.Range(0, 41) >= difficulty; }

        private static void Her(Ctx c, SceneBuilder b, Feel f, string line) { b.Face(c.Her, f).Her_(line); }

        private static void Pal(Ctx c, SceneBuilder b, Feel f, string line)
        {
            if (c.Pal != null) b.Face(c.Pal, f).Say(c.Pal, line);
        }

        private static string What(Incident i)
        {
            if (i == null) return "";
            switch (i.Kind)
            {
                case Social.Kinds.Helped: return "helping me the other day";
                case Social.Kinds.Defended: return "standing up for me";
                case Social.Kinds.KeptPromise: return "keeping your promise";
                case Social.Kinds.Rude: return "what you said to me";
                case Social.Kinds.BrokePromise: return "the promise you broke";
                case Social.Kinds.Ignored: return "walking right past me when I needed help";
                default: return "the other day";
            }
        }

        private static readonly Dictionary<string, string> Memory = new Dictionary<string, string>
        {
            { "stalls", "the night market" }, { "fortune", "the fortune teller" }, { "wonder", "the piano ghost" },
            { "haunt", "the haunted house" }, { "snow", "the walk in the snow" }, { "gym", "the gym" },
            { "track", "the time trial" }, { "study", "the make-up test" }, { "trip", "the temple town" },
            { "tea", "the tea room" }, { "wishes", "the wishing street" }, { "decor", "the pumpkin disaster" },
            { "sunset", "that evening in the classroom" },
        };

        /// <summary>What she remembers, right at the start: your last outing, something you did, or how little you know each other.</summary>
        private static void Callback(Ctx c, SceneBuilder b)
        {
            string mem;
            if (c.Bad != null)
                Her(c, b, Feel.Pout, c.T("I'm still a little hurt about " + What(c.Bad) + "... But I didn't want to come alone.", "I'm still kinda mad about " + What(c.Bad) + ", y'know! ...But I wanted to come.",
                    "Don't think I forgot " + What(c.Bad) + ". I just didn't want to waste the evening.", "I haven't forgiven you for " + What(c.Bad) + " yet. Consider this your chance."));
            else if (!string.IsNullOrEmpty(c.LastEp) && Memory.TryGetValue(c.LastEp, out mem))
                Her(c, b, Feel.Smile, c.T("I still think about " + mem + " sometimes... I had so much fun.", "Remember " + mem + "?! This has to be even better!",
                    "After " + mem + ", I figured you'd be reliable. Don't prove me wrong.", "After " + mem + "? You owe me another good one."));
            else if (c.Good != null)
                Her(c, b, Feel.Shy, c.T("I never really thanked you for " + What(c.Good) + "... This is my way of saying it.", "This is a thank-you for " + What(c.Good) + "! So have fun, okay?!",
                    "Consider this payback for " + What(c.Good) + ". I don't like owing people.", "Think of this as a reward for " + What(c.Good) + ". Don't let it go to your head."));
            else if (c.Girlfriend && c.Pal == null)
                Her(c, b, Feel.Shy, c.T("Just the two of us... It feels like a real date. ...It is a real date, isn't it?", "Date! This is officially a date! I even fixed my hair, did you notice?!",
                    "A date. With my boyfriend. ...Don't grin like that.", "My boyfriend finally takes me out. Took you long enough~"));
            else if (c.Tier == 0)
                Her(c, b, Feel.Normal, c.T("We don't talk much, do we... I thought this could be a start.", "We barely know each other! Perfect, that makes it an adventure!",
                    "We're not exactly close. That's fine. I needed someone and you were there.", "We hardly know each other. Let's see if you're interesting."));
            if (c.Pal != null)
                Pal(c, b, Feel.Smile, c.P("I hope it's okay that I came too...", "I invited myself! The more the merrier!", "She dragged me along. I'm only here to supervise.", "Don't mind me. I'm just here to watch you two."));
        }

        // ================= the finale =================

        /// <summary>
        /// The last choice: which kind of ending the context opens. A heart-to-heart (close, trusting, or she is having a bad
        /// day), clearing a grudge, talking about something you did for her, how the two friends met, a laugh (always), or a
        /// promise of the next outing.
        /// </summary>
        private static void Finale(Ctx c, SceneBuilder b, Tally t, string id)
        {
            var o = new Opts();
            o.If(c.Bad != null, "Apologize for " + What(c.Bad).Replace("me", "her").Replace("my", "her"), "fin_sorry", () => Sorry(c, b, t), 6f);
            o.If(c.Tier >= 2 || c.Trust >= 45 || c.Low, "Ask how she's really doing", "fin_heart", () => Heart(c, b, t), c.Low ? 4f : 2.5f);
            o.If(c.Good != null && c.Bad == null, "Ask about " + What(c.Good).Replace("me", "her"), "fin_thanks", () => Thanks(c, b, t), 2f);
            o.If(c.Pal != null, "Ask how they became friends", "fin_pals", () => Pals(c, b, t), 2f);
            o.If(c.Tier >= 1, "Plan the next outing", "fin_plan", () => Plan(c, b, t, id), 1.5f);
            // Alone with her: the romantic endings.
            var flirty = c.Mood == Mood.Flirty;
            o.If(c.Pal == null && c.Bad == null && (c.Girlfriend || c.Tier >= 2 || flirty), c.Girlfriend ? "Walk back hand in hand" : "Reach for her hand", "fin_close", () => Close(c, b, t),
                c.Girlfriend || flirty ? 3f : 1.8f);
            o.If(c.Pal == null && c.Bad == null && !c.Girlfriend && c.Tier >= 3 && c.Trust >= 35, "Tell her how you feel", "fin_confess", () => Confess(c, b, t), 3.5f);
            o.If(c.Pal == null && c.Bad == null && Cfg.LewdRequests.Value && (c.Girlfriend || c.Lewd >= 50 || c.HCount > 0), "Find somewhere private", "fin_h", () => Private(c, b, t),
                flirty || c.Lewd >= 70 ? 4f : 2.2f);
            o.Keep("Make her laugh", "fin_laugh", () => Laugh(c, b, t));
            o.Play(b, "fin_end");
        }

        // ---------------- romantic endings (alone with her) ----------------

        private static float CloseChance(Ctx c)
        {
            if (c.Girlfriend) return 1f;
            return UnityEngine.Mathf.Clamp(0.2f + c.Favor / 150f + G.Sens / 400f + c.Trust / 400f + (c.Mood == Mood.Flirty ? 0.2f : 0f), 0.05f, 0.95f);
        }

        /// <summary>Her hand on the way back; when she takes it and you are close enough, maybe a kiss.</summary>
        private static void Close(Ctx c, SceneBuilder b, Tally t)
        {
            var ok = Random.value < CloseChance(c);
            b.Narr("On the way back, the street is quiet. Your hands keep almost touching.");
            b.Me(c.Girlfriend ? Lines.Pick("Come here. Give me your hand.", "Let's walk like this for a while.") : Lines.Pick("Can I... hold your hand? Just until the station.", "Your hands look cold. Here."));
            if (!ok)
            {
                Her(c, b, Feel.Surprised, c.T("Ah... s-sorry... not yet. I'm not ready for that...", "Whoa! Um! Hands! That's, uh, a lot! Maybe next time?!", "...No. Not yet.", "Holding hands already? Earn it first."));
                Her(c, b, Feel.Shy, c.T("But... I'm happy you asked. Really.", "...But you can walk really close. That's allowed.", "...You can walk next to me, though.", "...But I'll think about it. Keep trying~"));
                t.On("fin_close", () => { t.Ending = "close_no"; t.Her(c.Her, 1, 2); });
                return;
            }
            b.Narr(c.Girlfriend ? "She laces her fingers through yours without even looking, like it's the most natural thing in the world." : "For a second she freezes. Then her fingers slowly close around yours.");
            Her(c, b, Feel.Shy, c.Girlfriend
                ? c.T("Mm... your hand is so warm. I could walk like this all night.", "Fingers laced! That's the official couple grip, you know!", "...Don't let go before the station.", "If you let go first, you lose. Got it?")
                : c.T("My face is so hot right now... please don't look at me...", "Okay. This is fine. This is TOTALLY fine. ...It's really nice, actually.", "...Your hand is warmer than I expected.", "Oh? Taking the initiative today? ...I'll allow it."));
            var o = new Opts();
            o.Keep("Just walk with her", "cl_walk", () =>
            {
                b.Narr("You don't talk much after that. You don't need to. The streetlights come on one by one.");
                Her(c, b, Feel.Smile, c.T("...I'll remember tonight. All of it.", "Today was the best. Like, the BEST best.", "...That was nice. Really nice.", "Not bad. Not bad at all."));
                t.On("cl_walk", () => t.Her(c.Her, 6, 4, c.Girlfriend ? 2 : 1, 3));
            });
            o.If(c.Girlfriend || c.Tier >= 3 || c.Mood == Mood.Flirty, "Kiss her", "cl_kiss", () =>
            {
                b.Narr("You stop under a streetlight. She looks up at you, and doesn't look away.");
                Her(c, b, Feel.Shy, c.T("...Here? Someone might... ...okay.", "W-wait, right here? ...Okay. Okay! I'm ready! Eyes closed!", "...Don't make me wait.", "You're thinking about it, aren't you? ...Then do it."));
                b.Narr("Her lips are soft, and she's shaking a little. When you pull apart, she hides her face against your shoulder.");
                Her(c, b, Feel.Love, c.T("...My heart is beating so fast I can't hear anything else.", "That was... wow. Again. No wait, not in public! ...Later!", "...Again. Later. Somewhere quieter.", "...Hmph. You're better at that than I expected."));
                t.On("cl_kiss", () => t.Her(c.Her, 8, 5, c.Girlfriend ? 3 : 1, 8));
            }, 2f);
            o.Play(b, "cl_end", 2);
            t.On("fin_close", () => t.Ending = "close");
        }

        /// <summary>A confession at the end of the day; if she says yes she becomes your girlfriend, as the game's own confession does.</summary>
        private static void Confess(Ctx c, SceneBuilder b, Tally t)
        {
            var chance = UnityEngine.Mathf.Clamp(0.3f + (c.Favor - 75) / 60f + c.Trust / 250f + G.Sens / 400f + (c.Mood == Mood.Flirty ? 0.15f : 0f) - (c.Mood == Mood.Upset ? 0.2f : 0f), 0.1f, 0.95f);
            string why;
            var ok = Random.value < chance && Consent.Chance(c.Her, Ask.Confess, out why) >= 0.5f;
            b.Narr("You stop walking. She notices and turns around.");
            b.Me(Lines.Pick("I've been wanting to tell you for a while. I like you. Not as a friend. I really like you.",
                "Today was one of the best days I've had. Because it was with you. ...I like you. A lot.",
                "I don't want to just be your friend. I like you. Will you go out with me?"));
            Her(c, b, Feel.Surprised, c.T("Eh...? Y-you mean... me...?", "W-WAIT. Wait wait wait. Say that again?!", "...Are you serious?", "...Huh. You actually said it."));
            if (ok)
            {
                Her(c, b, Feel.Shy, c.T("I... I've liked you for so long. I thought it was only me...", "I like you too! I've liked you forever, you dummy! Why did you take so long?!",
                    "...Fine. Yes. I like you too. I have for a while.", "Took you long enough. ...Yes. Obviously yes."));
                b.Narr("She takes your hand, and this time neither of you lets go.");
                Her(c, b, Feel.Love, c.T("So... from today, I'm your girlfriend. Please take care of me.", "We're dating! We're DATING! I have to tell everyone! ...Okay, maybe not everyone.",
                    "From today, you're mine. Don't forget it.", "From today you're my boyfriend. No take-backs~"));
                t.On("fin_confess", () =>
                {
                    t.Ending = "confess";
                    t.Her(c.Her, 10, 10, 3, 4);
                    t.Extra.Add(() => BecomeGirlfriend(c.Her));
                });
            }
            else
            {
                Her(c, b, Feel.Worried, c.T("I... I'm really happy. But I'm not ready yet... Can you wait for me a little?", "Ah... um... I really like hanging out with you! But dating is... can I think about it?",
                    "...Not yet. I need to be sure. Don't take it the wrong way.", "...You're sweet. But not yet. Ask me again when you've earned it."));
                Her(c, b, Feel.Smile, c.T("Please don't stop talking to me because of this, okay?", "We're still friends! The best of friends! Okay?!", "...We're fine. Really.", "Don't make that face. I didn't say never."));
                t.On("fin_confess", () => { t.Ending = "confess_no"; t.Her(c.Her, 2, 4); });
            }
        }

        /// <summary>What the game does when she accepts a confession (TalkScene, heroine event 0).</summary>
        internal static void BecomeGirlfriend(SaveData.Heroine h)
        {
            if (h == null || h.isGirlfriend) return;
            h.isGirlfriend = true;
            h.confessed = true;
            if (!h.isFirstGirlfriend)
            {
                h.isFirstGirlfriend = true;
                Singleton<Manager.Game>.Instance.rankSaveData.girlfriendCount += 1U;
            }
            CampusLifePlugin.Log.LogInfo("Episodes: " + GirlInfo.FirstName(h) + " is your girlfriend now");
        }

        private static float PrivateChance(Ctx c)
        {
            var p = (c.Girlfriend ? 0.6f : 0.15f) + c.Lewd / 200f + G.Sens / 400f + (c.HCount > 0 ? 0.15f : 0f) + (c.Mood == Mood.Flirty ? 0.2f : 0f) - (c.Low ? 0.15f : 0f);
            return UnityEngine.Mathf.Clamp(p, 0.05f, 0.95f);
        }

        /// <summary>Somewhere private: if she says yes, the talk ends in the game's own H with her (see Duo.IntoH).</summary>
        private static void Private(Ctx c, SceneBuilder b, Tally t)
        {
            var ok = Random.value < PrivateChance(c);
            b.Narr("It's getting late, and neither of you seems to want the day to end.");
            b.Me(c.HCount > 0 ? Lines.Pick("I don't want to say goodbye yet. Want to go somewhere just the two of us? Like last time.", "Hey... nobody's around right now. Come with me?")
                              : Lines.Pick("I don't want to say goodbye yet. Want to go somewhere just the two of us?", "Let's go somewhere quieter. Just you and me."));
            if (!ok)
            {
                Her(c, b, Feel.Shy, c.T("S-somewhere just the two of us...? I... not today. My heart can't take it.", "Ehh?! Just us two, now?! N-no way, I'm way too nervous! ...Another day!",
                    "...I know what you mean. Not today.", "Ohh? Bold. ...Not today, though. Make me want it more."));
                t.On("fin_h", () => { t.Ending = "h_no"; t.Her(c.Her, 1, 0, 0, 3); });
                return;
            }
            Her(c, b, Feel.Shy, c.T("...Okay. I was hoping you'd ask. I know a place nobody goes this late...", "Hehe... I was totally thinking the same thing. Follow me, quick, before someone sees!",
                "...Fine. But we go now, before I change my mind.", "Finally. I was starting to think I'd have to drag you there myself~"));
            b.Narr(c.HCount > 0 ? "She doesn't let go of your hand all the way back. You both know where you're going."
                                : "She takes your hand and pulls you along, glancing back at you with red cheeks.");
            Her(c, b, Feel.Love, c.T("...Be gentle with me, okay?", "Okay, nobody's here! ...Ehehe. I'm all yours.", "...Don't keep me waiting.", "Now then... let's see what you've got."));
            t.IntoH = true;
            t.On("fin_h", () => { t.Ending = "h"; t.Her(c.Her, 3, 2, c.Girlfriend ? 2 : 0, 10); });
        }

        private static void Sorry(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("About " + What(c.Bad).Replace("me", "you").Replace("my", "your") + "... I'm sorry. I mean it.");
            var ok = c.Trust >= 30 || c.Tier >= 2;
            Her(c, b, ok ? Feel.Smile : Feel.Serious, ok
                ? c.T("...Thank you for saying it. I was waiting for that. It's okay now.", "...Okay! Apology accepted! Clean slate!", "...Fine. Thank you. Let's leave it here.", "Took you long enough. ...Okay. Forgiven.")
                : c.T("...I believe you. I just need a little time, okay?", "Hmm... okay. But you're on probation!", "Words are easy. We'll see.", "I'll think about it. Keep trying."));
            t.On("fin_sorry", () => { t.Ending = "sorry"; t.Her(c.Her, ok ? 5 : 2, ok ? 10 : 5); });
        }

        private static void Thanks(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("Can I ask you something? Why did " + What(c.Good).Replace("me", "you") + " matter so much to you?");
            Her(c, b, Feel.Shy, c.Good.Kind == Social.Kinds.Defended
                ? c.T("Because nobody ever stood up for me before. I didn't know what to do with my face.", "Because that was SO cool! Nobody does that! ...It made me really happy.", "Because people usually just watch. You didn't.", "Because it was unexpected. I like being surprised by people.")
                : c.Good.Kind == Social.Kinds.KeptPromise
                ? c.T("Because people forget things they promise me. You didn't.", "Because you actually did it! People usually forget!", "Because you did what you said. That's rare.", "Because I half expected you to forget. You didn't.")
                : c.T("Because I was really struggling, and you just... helped. Without me asking twice.", "Because I was in a total panic and you showed up like a hero!", "Because I hate asking for help. You made it easy.", "Because you didn't make a big deal of it. I liked that."));
            Her(c, b, Feel.Smile, c.T("I'm glad we're friends.", "You're a really good friend, you know that?", "You're... alright. Better than alright.", "You're more reliable than you look."));
            t.On("fin_thanks", () => { t.Ending = "thanks"; t.Her(c.Her, 5, 7, 1); });
        }

        private static readonly string[] HowWeMet =
        {
            "We sat next to each other on the first day and both forgot our pencil cases.",
            "We got lost on the same school trip and found our way back together. Mostly by accident.",
            "We fought over the last melon bread at the cafeteria. Then we split it.",
            "We were partners for cleaning duty and talked so much we forgot to clean.",
            "She lent me an umbrella in the rain and then we both got wet anyway.",
        };

        private static void Pals(Ctx c, SceneBuilder b, Tally t)
        {
            var story = HowWeMet[Random.Range(0, HowWeMet.Length)];
            Her(c, b, Feel.Laugh, c.T("Oh... that's a funny story. " + story, "Okay, okay, so! " + story, story + " That's all.", story + " Classic, right?"));
            Pal(c, b, Feel.Laugh, c.Bond >= 70
                ? c.P("And we've been inseparable since. She's my best friend.", "Best friends forever! It's official!", "...She's the only one who puts up with me.", "And now she can't get rid of me.")
                : c.P("We're still getting to know each other, really.", "And now we're friends! Kind of! Mostly!", "We're... working on it.", "It's complicated. In a fun way."));
            t.On("fin_pals", () => { t.Ending = "pals"; t.Her(c.Her, 3, 4); t.Her(c.Pal, 4, 4); t.Extra.Add(() => BondUp(c.Her, c.Pal, 5)); });
        }

        private static void BondUp(SaveData.Heroine a, SaveData.Heroine bb, int d)
        {
            if (a == null || bb == null) return;
            foreach (var pair in new[] { new[] { a, bb }, new[] { bb, a } })
            {
                var gs = S.Girl(GirlInfo.Key(pair[0]));
                var k = GirlInfo.Key(pair[1]);
                int v;
                gs.Bonds.TryGetValue(k, out v);
                gs.Bonds[k] = Math.Max(-100, Math.Min(100, v + d));
            }
        }

        private static void Plan(Ctx c, SceneBuilder b, Tally t, string id)
        {
            var next = All.Where(e => e.Id != id && e.Weight(c) > 0f).OrderBy(_ => Random.value).FirstOrDefault();
            var what = next == null ? "something fun" : Memory[next.Id].Replace("the ", "");
            b.Me("Next time, let's do something else together. Maybe " + what + "?");
            Her(c, b, Feel.Smile, c.T("Next time... I'd like that. I'll hold you to it.", "YES! It's a plan! Pinky promise!", "...Fine. Next time. Don't forget.", "Already planning the next one? Okay. Deal."));
            t.On("fin_plan", () => { t.Ending = "plan"; t.Her(c.Her, 3, 5); });
        }

        private static readonly string[] Jokes =
        {
            "You tell her the one about the penguin who walks into a convenience store.",
            "You do your best impression of the vice principal's morning speech.",
            "You explain, very seriously, your theory that pigeons are government spies.",
            "You tell her about the time you got your head stuck in the school gate.",
        };

        private static void Laugh(Ctx c, SceneBuilder b, Tally t)
        {
            var land = Roll(Math.Max(G.Int, G.Sens), 50);
            b.Narr(Jokes[Random.Range(0, Jokes.Length)]);
            if (land)
            {
                b.Narr("She tries to hold it in. She fails completely.");
                Her(c, b, Feel.Laugh, c.T("Ahaha... st-stop, my stomach hurts...!", "AHAHAHA! Again! Tell it again!", "...Pfft. That was stupid. ...Pfft.", "Okay, okay, you win. That was actually funny."));
            }
            else
            {
                b.Narr("Silence. A crow caws somewhere. The joke dies a slow, painful death.");
                Her(c, b, Feel.Laugh, c.T("...That was so bad it's actually funny. Ahaha...", "That was TERRIBLE! Ahahaha! I love it!", "...I'm laughing at you, not with you. ...Okay, a little with you.", "That's the worst joke I've ever heard. I'm saving it forever."));
            }
            Pal(c, b, Feel.Laugh, c.P("Ahaha... you two are funny together.", "HAHA! I'm crying!", "...I didn't laugh. That wasn't a laugh.", "I'm telling everyone about this."));
            t.On("fin_laugh", () => { t.Ending = "laugh"; t.Her(c.Her, land ? 6 : 4, 2); if (c.Pal != null) t.Her(c.Pal, 3); });
        }

        // ---------------- the heart-to-heart ----------------

        private static string Dream(Ctx c)
        {
            if (c.Cooking) return c.T("I want to open a little cafe one day. With a cat sleeping in the window.", "I want my own restaurant! A huge one! With a neon sign!", "I'd like to be a chef. A real one, with a real kitchen.", "A cafe of my own, where I decide the menu and everybody loves it.");
            if (c.Sporty) return c.T("I want to run at the nationals. Just once. Even if I come last.", "Nationals! I'm going to the nationals and I'm going to WIN!", "I want to compete at a national level. I'm not joking.", "Nationals. Then a medal. Then my face on a cereal box.");
            if (c.Bookish) return c.T("I want to study abroad... somewhere far, where nobody knows me.", "I want to go to a university in another country! Like, really far!", "I'm aiming for a top university. Medicine, maybe.", "A top university, a fancy degree, and everyone who doubted me can watch.");
            if (c.Animals) return c.T("I want to be a vet. I can't stand seeing animals hurt.", "Vet! I'm gonna be a vet and pet every animal in the world!", "Veterinarian. Animals are more honest than people.", "A vet. Animals like me more than people do. Mostly.");
            if (c.Fashion) return c.T("I'd like to design clothes. Even just one dress that someone loves.", "Fashion designer! My own brand! You'll wear my stuff!", "Design. I already have three notebooks of sketches.", "My own fashion brand. Everyone at school will wear my stuff.");
            return c.T("I don't really know yet... I just want to find something that's mine.", "I don't know yet! Something BIG! I'll figure it out!", "I don't know. That's what bothers me.", "Haven't decided. I want something nobody expects from me.");
        }

        private static void Heart(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("Hey. How are you really doing?");
            Her(c, b, Feel.Surprised, c.T("Eh? Me...? ...Nobody ever asks that.", "Huh? I'm great! I'm always great! ...Why are you looking at me like that?", "...Why do you ask?", "Whoa, getting serious all of a sudden?"));
            var lonely = c.Mood == Mood.Lonely || c.Gs.Attention < 35;
            if (c.Mood == Mood.Stressed || c.Mood == Mood.Upset && c.Bookish)
            {
                Her(c, b, Feel.Worried, c.T("Honestly... I'm tired. Everyone expects me to do well, and I'm scared of letting them down.", "I'm... kind of exhausted? Everybody thinks I'm fine because I'm loud.",
                    "There's a lot of pressure. Grades, club, home. I handle it. Usually.", "I'm a bit stressed. Everyone thinks I'm carefree. It's an act, sometimes."));
                Her(c, b, Feel.Sad, c.T("Sometimes I lie in bed and just... count everything I haven't done yet.", "Sometimes I just want to scream into a pillow, you know?", "Sometimes I wonder who I'm doing it all for.", "Sometimes I want to just quit everything and see what happens."));
            }
            else if (lonely)
            {
                var friend = c.Best == null ? "my friends" : GirlInfo.FirstName(c.Best);
                Her(c, b, Feel.Sad, c.T("Lately I feel a bit... invisible. " + friend + " is busy, and I don't want to bother anyone.", "Lately everyone's kinda busy... " + friend + " too. It's fine! Mostly!",
                    "I've been alone a lot lately. " + friend + " has other people now. That's normal.", "Lately nobody seems to notice me. Not even " + friend + ". Weird, right?"));
            }
            else if (c.Mood == Mood.Upset)
            {
                Her(c, b, Feel.Pout, c.T("Today was just... a bad day. Everything went wrong, one thing after another.", "Today was the WORST. I tripped, I forgot my lunch, and the vending machine ate my coins!",
                    "Bad day. I don't want to list it.", "Today was awful. The universe has it out for me."));
            }
            else if (c.Tier >= 3 && c.Trust >= 60)
            {
                Her(c, b, Feel.Shy, c.T("Can I tell you something I haven't told anyone?", "Okay, secret time! You can't tell anyone!", "I'll tell you something. Only you.", "Fine. Since you asked nicely. A secret."));
                Her(c, b, Feel.Smile, Dream(c));
                Her(c, b, Feel.Worried, c.T("...Is that silly?", "...Don't laugh! Okay you can laugh a little.", "...Say something.", "...Well? Go on, tell me it's crazy."));
            }
            else
            {
                Her(c, b, Feel.Worried, c.T("Sometimes I worry I'm boring. That people only put up with me.", "Sometimes I'm scared I'm too much. Too loud, you know?",
                    "Sometimes I think people find me cold. I'm not. I just don't know what to say.", "Sometimes I wonder if anyone would notice if I just stopped joking."));
            }

            var o = new Opts();
            o.Keep("Just listen", "ht_listen", () =>
            {
                b.Narr("You don't say anything clever. You just listen, all the way to the end.");
                Her(c, b, Feel.Smile, c.T("...Thank you. That helped more than you think.", "...Huh. I feel lighter. Thanks for not interrupting!", "...Thanks for not trying to fix it.", "...You're a good listener. Who knew."));
                t.On("ht_listen", () => t.Her(c.Her, 4, 8, 1));
            });
            o.If(c.Gs.Talks >= 4 || c.Tier >= 2, "Tell her about your own worries", "ht_share", () =>
            {
                b.Me("You know... I feel like that too, sometimes. More than I let on.");
                b.Narr("You tell her things you don't usually say out loud. She listens, very quietly.");
                Her(c, b, Feel.Smile, c.T("...So it's not just me. That's... really comforting.", "Whoa, really? You too? ...We should talk more, you know!", "...Thanks for telling me. That's fair. Now we're even.", "...Huh. You're more interesting than I thought."));
                t.On("ht_share", () => t.Her(c.Her, 5, 10, 2));
            }, 2f);
            var smart = G.Int >= G.Sens;
            var good = Roll(Math.Max(G.Int, G.Sens), 55);
            o.Add(smart ? "Give her some honest advice [INT]" : "Cheer her up [SEN]", "ht_help", () =>
            {
                b.Me(smart ? "Here's what I think. One thing at a time. Pick the one that matters most and let the rest wait." : "Hey. You're great, okay? I'm not just saying that. I'd tell you if you weren't.");
                Her(c, b, good ? Feel.Smile : Feel.Laugh, good
                    ? c.T("...You're right. I'll try. Thank you.", "Okay! One thing at a time! I can do that!", "...That's actually useful. Thanks.", "...Okay. That was a good answer.")
                    : c.T("...That's a little clumsy. But it's sweet of you. Thank you.", "Pfft, that was so cheesy! ...But thanks!", "...That was awkward. I appreciate the attempt.", "That was so corny. ...It worked a little, though."));
                t.On("ht_help", () => t.Her(c.Her, good ? 6 : 3, good ? 7 : 4));
            }, 1.5f);
            o.If(c.Trust >= 40, "Promise she can always count on you", "ht_promise", () =>
            {
                b.Me("Whenever it gets heavy, come find me. I mean it. Anytime.");
                Her(c, b, Feel.Shy, c.T("...Okay. I will. Thank you, really.", "Anytime? I'm gonna hold you to that! ...Thanks.", "...I'll remember that.", "Anytime? Careful, I might take you up on it."));
                t.On("ht_promise", () => t.Her(c.Her, 4, 9, 3));
            });
            o.Play(b, "ht_end", 3);
            t.On("fin_heart", () => t.Ending = "heart");
        }

        // ================= the episodes =================

        private static List<Ep> Catalog()
        {
            return new List<Ep>
            {
                Stalls(), Fortune(), Wonder(), Haunt(), Snow(), Gym(), Track(), Study(), Trip(), Tea(), Wishes(), Decor(), Sunset(),
            };
        }

        // ---------------- the night market ----------------

        private static Ep Stalls()
        {
            return new Ep
            {
                Id = "stalls", Title = "The night market", Bg = "festival_stalls_night", PalOk = true,
                Weight = c => 1f + (c.Foodie || c.Sweet || c.Spicy ? 1.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("Um... the night market by the shrine opens today. Would you come with me?", "Night market! Tonight! You're coming, right? Right?!",
                    "The night stalls open today. I need someone to carry things.", "Night market tonight. You're coming. That's not a question.")),
                Write = (c, b, t) =>
                {
                    b.Narr("That evening. The shrine road is lined with stalls, paper lanterns glowing red and orange in the dark.");
                    b.Narr("Grilled squid, sweet corn, candied apples... the smell alone is worth the trip.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("It's so pretty at night... Where should we start?", "Okay! Battle plan! We eat EVERYTHING!", "Stay close. It gets crowded.", "Try to keep up. I know all the good stalls."));
                    var o = new Opts();
                    if (c.Snack != "")
                    {
                        var liked = Items.Get(c.Snack) != null && Items.Get(c.Snack).Likes != null && Items.Get(c.Snack).Likes.Any(l => GirlInfo.Likes(c.Her, l));
                        o.Add("Share your " + ItemName(c.Snack) + " while you walk", "st_snack", () =>
                        {
                            b.Narr("You pull the " + ItemName(c.Snack) + " out of your bag. Her eyes go wide.");
                            Her(c, b, liked ? Feel.Laugh : Feel.Smile, liked ? c.T("That's my favorite! How did you know?", "NO WAY! My favorite! You're a genius!", "...That's my favorite. Lucky guess.", "My favorite? Were you spying on me?")
                                                                     : c.T("For me? Thank you... it's good.", "Snack before the snacks! Smart!", "...Not bad.", "Bringing your own snacks to a market? Smart."));
                            t.On("st_snack", () => { t.Use(c.Snack); t.Her(c.Her, liked ? 7 : 4); });
                        }, liked ? 3f : 1.5f);
                    }
                    o.If(S.Money >= 800, "Treat her to whatever she wants (¥800)", "st_treat", () =>
                    {
                        b.Narr(c.Foodie ? "She points at three stalls at once. Then a fourth." : "She thinks hard and picks a candied apple, very seriously.");
                        Her(c, b, Feel.Laugh, c.T("You're spoiling me... Thank you.", "You're the best! Okay, now the takoyaki!", "...I'll pay you back. Someday.", "Generous tonight, aren't you?"));
                        t.On("st_treat", () => { t.Money -= 800; t.Her(c.Her, 6); });
                    }, c.Foodie ? 2f : 1f);
                    var spicyWin = Roll(G.Str, 60);
                    o.If(c.Spicy || S.Count("spicy_chips") > 0, "Take the ghost-pepper challenge [STR]", "st_spicy", () =>
                    {
                        b.Narr("A stall sells 'Hell Skewers'. Finish one and your photo goes on the wall of fame.");
                        b.Narr(spicyWin ? "You eat it in four bites. Tears stream down your face, but you finish. The crowd cheers." : "Two bites in, you can't feel your face. She buys you milk. She is laughing so hard she has to sit down.");
                        Her(c, b, Feel.Laugh, spicyWin ? c.T("You did it...! Are you okay? Your face is so red!", "LEGEND! You're on the wall! Photo! Photo!", "...Impressive. Stupid, but impressive.", "Okay, that was actually cool.")
                                                      : c.T("Here, drink this... slowly...", "AHAHAHA! Your FACE! I got it on video!", "Water won't help. Milk. Here.", "I'm framing that picture."));
                        t.On("st_spicy", () => { t.Her(c.Her, 4); if (spicyWin) t.Rep += 2; });
                    }, c.Spicy ? 2.5f : 1f);
                    var ringWin = Roll(G.Int, 55);
                    o.Add("Win her a prize at the ring toss [INT]", "st_rings", () =>
                    {
                        b.Narr("You calculate the angle, the weight of the ring, the wind. You explain all of it out loud.");
                        b.Narr(ringWin ? "Third ring: it lands. The old man hands over a stuffed fox the size of a cat." : "Every ring bounces off. The old man gives you a pity eraser shaped like a fish.");
                        Her(c, b, ringWin ? Feel.Laugh : Feel.Smile, ringWin ? c.T("He's so cute...! I'll name him after this night.", "FOX! I love him! He's our mascot now!", "...I'll keep it. For the fox's sake.", "A fox? Okay, I'm impressed.")
                                                                         : c.T("The fish eraser is cute too... I'll use it.", "Fish eraser! Best prize! Seriously!", "Math lost to a ring toss. Remarkable.", "All that math for an eraser? Adorable."));
                        t.On("st_rings", () => { t.Her(c.Her, ringWin ? 5 : 3); if (ringWin) t.Int += 1; });
                    });
                    o.Keep("Let her lead the way", "st_lead", () =>
                    {
                        b.Narr("She takes you straight to a tiny stall at the end of the road, run by an old woman who knows her name.");
                        Her(c, b, Feel.Smile, c.T("I've come here since I was little. I never brought anyone before.", "This is THE best stall! Secret spot! Don't tell anyone!", "This is the only one worth it. Don't tell people.", "My secret spot. Consider yourself honored."));
                        t.On("st_lead", () => t.Her(c.Her, 3, 4));
                    });
                    Pal(c, b, Feel.Smile, c.P("You two are funny. I'm getting another skewer.", "Wait for me! I want one too!", "I'll hold the bags. As usual.", "Don't eat it all without me."));
                    o.Play(b, "st_1");

                    b.Bg("culture_festival");
                    b.Narr("At the end of the road, a takoyaki stand with a line of twenty people.");
                    b.Narr("The old man behind the grill is alone, sweating, flipping three trays at once. His helper went home sick.");
                    var o2 = new Opts();
                    o2.If(c.Cooking, "Let her show off her cooking", "st_cook", () =>
                    {
                        Her(c, b, Feel.Serious, c.T("Um... sir? I could help... I'm in the cooking club.", "Move over, I've got this! Cooking club powers!", "Give me the pick. Watch.", "Step aside, amateur. Let a professional work."));
                        b.Narr("She flips takoyaki like she was born doing it. The line moves twice as fast. The old man bows to her.");
                        t.On("st_cook", () => t.Her(c.Her, 6, 3));
                    }, 3f);
                    var helpOk = Roll(G.Int, 50);
                    o2.Keep("Help out behind the counter [INT]", "st_help", () =>
                    {
                        b.Narr(helpOk ? "You take orders, count change and keep the line in order. In twenty minutes the line is gone." : "You mix up four orders and give someone extra octopus. Nobody complains.");
                        b.Narr("The old man presses a box of takoyaki into your hands. \"On the house, kids.\"");
                        Her(c, b, Feel.Laugh, c.T("That was fun... I liked working with you.", "Best part of the night! We're a great team!", "Not bad. You can keep the job.", "Look at you, a working man. I approve."));
                        t.On("st_help", () => { t.Her(c.Her, 4, 2); t.Rep += helpOk ? 2 : 1; t.Money += helpOk ? 500 : 200; });
                    });
                    o2.If(c.Famous, "Use your fame to draw a crowd", "st_fame", () =>
                    {
                        b.Narr("A group of first-years recognizes you. \"Is that... him?\" In a minute the line is twice as long, and twice as happy.");
                        Her(c, b, Feel.Surprised, c.T("Everyone knows you... That's amazing.", "You're like a celebrity! Can I have an autograph?", "...Your reputation is ridiculous.", "Famous, huh? Don't let it go to your head."));
                        t.On("st_fame", () => { t.Rep += 2; t.Her(c.Her, 4); });
                    }, 1.5f);
                    o2.Add("Just wait in line and talk", "st_wait", () =>
                    {
                        b.Narr("The line moves slowly. You talk about everything: teachers, songs, the worst bento you ever ate.");
                        Her(c, b, Feel.Smile, c.T("This is nice... Just talking like this.", "Waiting is fun when you're here! Weird!", "...I don't mind the wait.", "Twenty minutes and you didn't bore me once. Impressive."));
                        t.On("st_wait", () => t.Her(c.Her, 3, 4));
                    });
                    o2.Play(b, "st_2");
                    b.Narr("You sit on the shrine steps with the takoyaki between you, burning your tongues and not caring.");
                    Finale(c, b, t, "stalls");
                    b.Narr("You walk back under the lanterns, full, sticky with sauce and very happy.");
                }
            };
        }

        // ---------------- the fortune teller ----------------

        private static string Future(Ctx c)
        {
            if (c.Cooking) return "\"I see... a kitchen. A big one. And many people waiting for what you make.\"";
            if (c.Sporty) return "\"I see a finish line. And you, crossing it. The crowd is very loud.\"";
            if (c.Bookish) return "\"I see a pile of books taller than you. You climb it. At the top, a door.\"";
            if (c.Animals) return "\"I see... cats. So many cats. They all seem to love you.\"";
            if (c.Fashion) return "\"I see bright lights and a runway. And a dress no one has seen before.\"";
            return "\"I see a road with many turns. Every turn is yours to choose.\"";
        }

        private static Ep Fortune()
        {
            return new Ep
            {
                Id = "fortune", Title = "The fortune teller", Bg = "fortune_teller", PalOk = true,
                Weight = c => 1f + (c.Tier >= 1 ? 0.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("There's a fortune teller in the shopping street everyone talks about... Would you come with me? I'm scared to go alone.", "There's a fortune teller who's NEVER wrong! We have to go! Today!",
                    "There's a fortune teller downtown. I want to prove she's a fraud. Come.", "Let's get our fortunes read. I want to know if you'll be rich someday. For my own reasons.")),
                Write = (c, b, t) =>
                {
                    b.Narr("A narrow shop in the arcade: purple curtains, incense, a crystal ball the size of a melon.");
                    b.Narr("Madame Rei looks up from behind the ball as if she has been waiting for you two all week.");
                    Callback(c, b);
                    Her(c, b, Feel.Surprised, c.T("She's looking right at us... I'm nervous.", "Whoa, it's so atmospheric! Look at the ball!", "Smoke and mirrors. Watch.", "She's good at the creepy stare. I'll give her that."));
                    var o = new Opts();
                    o.Keep("Ask about her future", "ft_future", () =>
                    {
                        b.Narr("The crystal ball swirls. Madame Rei narrows her eyes.");
                        b.Narr(Future(c));
                        Her(c, b, Feel.Shy, c.T("...How did she know? I never told anyone that.", "THAT'S EXACTLY MY DREAM! How?! HOW?!", "...Lucky guess. A very lucky guess.", "...Okay. That was a little creepy."));
                        t.On("ft_future", () => t.Her(c.Her, 3, 4));
                    });
                    o.If(c.Tier >= 1, "Ask about your friendship", "ft_friends", () =>
                    {
                        b.Narr(c.Trust >= 50 ? "\"Two threads, tangled since spring. Pull on one and the other answers.\"" : "\"Two threads, side by side. Not tied yet. That depends on you.\"");
                        Her(c, b, Feel.Smile, c.T("Tangled threads... I like that.", "We're tangled! That's the best kind of friends!", "...Vague. But not wrong.", "So we're stuck with each other. Fine by me."));
                        t.On("ft_friends", () => t.Her(c.Her, 4, 3));
                    }, 1.5f);
                    o.Add("Ask about the next exam", "ft_exam", () =>
                    {
                        b.Narr("Madame Rei stares into the ball for a long time. \"...Study.\"");
                        Her(c, b, Feel.Laugh, c.T("Ahaha... that's good advice, at least.", "That's IT?! Ahaha! Worth it!", "The only accurate prediction today.", "Wow. Mystical."));
                        t.On("ft_exam", () => { t.Int += 1; t.Her(c.Her, 2); });
                    });
                    o.If(c.Pal != null, "Ask about the two of them", "ft_pair", () =>
                    {
                        b.Narr(c.Bond >= 60 ? "\"Two flowers from the same root. Storms will not separate you.\"" : "\"Two flowers in the same garden. One day you will understand each other very well.\"");
                        Pal(c, b, Feel.Laugh, c.P("Same root... that's us.", "SEE?! Best friends! It's fate!", "...She says that to everyone.", "Well, she's not wrong."));
                        t.On("ft_pair", () => { t.Her(c.Her, 3); t.Her(c.Pal, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 4)); });
                    }, 2f);
                    o.If(c.Bad != null, "Ask if she'll forgive you", "ft_forgive", () =>
                    {
                        b.Narr("Madame Rei glances at her, then at you. \"She already wants to. Do not make her wait.\"");
                        Her(c, b, Feel.Pout, c.T("...H-hey! That's not for her to say!", "HEY! Whose side is she on?!", "...That's none of her business.", "Ugh. Fine. She's not entirely wrong."));
                        t.On("ft_forgive", () => t.Her(c.Her, 2, 4));
                    }, 2f);
                    o.Play(b, "ft_1");

                    b.Narr("\"One more thing,\" Madame Rei says. \"One of you carries luck. It wants to be given away.\"");
                    var o2 = new Opts();
                    o2.If(S.Count("omamori") > 0, "Give her your lucky charm", "ft_charm", () =>
                    {
                        b.Narr("You take out your omamori and put it in her hand.");
                        Her(c, b, Feel.Shy, c.T("For me...? I'll keep it with me every day.", "Lucky charm! Now I'm invincible! Thank you!", "...You need it more than me. ...I'll keep it anyway.", "Giving away your luck? Careful. I'll keep it."));
                        t.On("ft_charm", () => { t.Use("omamori"); t.Her(c.Her, 6, 4); });
                    }, 3f);
                    o2.If(S.Money >= 800 && S.Count("omamori") == 0, "Buy one of her charms (¥800)", "ft_buy", () =>
                    {
                        b.Narr("Madame Rei produces a charm from her sleeve. It costs exactly what you have. Of course.");
                        t.On("ft_buy", () => { t.Money -= 800; t.Get("omamori"); });
                    });
                    var popular = Roll(G.Sens, 50);
                    o2.Add("Ask her about your own future", "ft_mine", () =>
                    {
                        b.Narr(popular ? "\"Many people will come to you for help. You will not turn them away.\"" : "\"I see... a lot of homework. A truly frightening amount.\"");
                        Her(c, b, Feel.Laugh, c.T("That sounds just like you.", "Ahaha! That's SO you!", "Accurate.", "Ha. She's got your number."));
                        t.On("ft_mine", () => { if (popular) t.Rep += 1; t.Her(c.Her, 2); });
                    });
                    o2.Keep("Laugh it off", "ft_laugh", () =>
                    {
                        b.Narr("You laugh. Madame Rei does not. \"Laugh now,\" she says, very quietly. The candles flicker.");
                        Her(c, b, Feel.Surprised, c.T("L-let's go. Let's go now.", "Okay that was SCARY. Let's get crepes.", "...Unsettling. Let's leave.", "Okay, she's good. Let's go."));
                        t.On("ft_laugh", () => t.Her(c.Her, 2));
                    });
                    o2.Play(b, "ft_2");
                    b.Narr("Outside, the arcade is bright and noisy again, as if the tent was a dream.");
                    Finale(c, b, t, "fortune");
                    b.Narr("Whatever the future holds, tonight was a good one.");
                }
            };
        }

        // ---------------- the seventh wonder ----------------

        private static Ep Wonder()
        {
            return new Ep
            {
                Id = "wonder", Title = "The seventh wonder", Bg = "dark_hallway", PalOk = true,
                Weight = c => 1f + (c.Tone == Tone.Energetic || c.Tone == Tone.Teasing ? 0.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("Have you heard? The piano in the old music room plays by itself after dark... Would you check it with me?", "The seventh wonder of the school! A ghost piano! We're investigating tonight!",
                    "There's a rumor about a ghost piano. I intend to prove it's nonsense. You're my witness.", "Want to go ghost hunting? The old music room. After dark. Unless you're scared.")),
                Write = (c, b, t) =>
                {
                    b.Narr("After club hours. The school is empty, and the old wing is darker than it has any right to be.");
                    b.Narr("Somewhere upstairs, very faintly: a piano. Three notes. Then nothing.");
                    Callback(c, b);
                    Her(c, b, Feel.Surprised, c.T("D-did you hear that...?", "THERE IT IS! The ghost! Come on!", "...There's a logical explanation.", "Oh, this is going to be good."));
                    Pal(c, b, Feel.Worried, c.P("I want to go home. I want to go home now.", "This is the best night ever!", "Ghosts don't exist. ...Walk in front.", "If a ghost eats you, can I have your bag?"));
                    var o = new Opts();
                    var brave = Roll(G.Str, 50);
                    o.Keep("Follow the sound [STR]", "wd_follow", () =>
                    {
                        b.Narr(brave ? "You walk in front, steady. Every board creaks. She holds on to the back of your jacket." : "A mop falls out of a closet. You both scream. Then you both laugh, shaking.");
                        Her(c, b, brave ? Feel.Smile : Feel.Laugh, brave ? c.T("You're so calm... I feel safer.", "You're like a ghost hunter! A pro!", "...Good. Keep walking.", "Not bad. I thought you'd run.")
                                                                          : c.T("That was a mop... it was just a mop...", "THE MOP! AHAHA! We screamed at a MOP!", "...We will never speak of this.", "You screamed higher than me. Noted."));
                        t.On("wd_follow", () => { t.Her(c.Her, 3, 3); if (brave) t.Str += 1; });
                    });
                    o.If(G.Int >= 35, "Check the club logbook first [INT]", "wd_log", () =>
                    {
                        b.Narr("In the music club's logbook, under today's date: \"Do NOT leave the window open again. — K.\"");
                        Her(c, b, Feel.Serious, c.T("The window...? So something comes in from outside?", "A clue! We're real detectives now!", "Interesting. That's evidence.", "Ooh, a mystery with clues. Smart."));
                        t.On("wd_log", () => { t.Int += 1; t.Her(c.Her, 3); });
                    }, 1.5f);
                    o.If(c.Animals, "Listen closely", "wd_meow", () =>
                    {
                        Her(c, b, Feel.Surprised, c.T("Wait... was that a meow?", "Did you hear a MEOW?!", "...That sounded like a cat.", "Ghosts don't meow. Usually."));
                        t.On("wd_meow", () => t.Her(c.Her, 3));
                    }, 2f);
                    o.If(c.Pal != null, "Send her friend ahead as bait", "wd_bait", () =>
                    {
                        Pal(c, b, Feel.Pout, c.P("Me?! Why me?! ...Fine. If I die, I'm haunting you both.", "Bait?! ...Okay, that's kind of cool actually.", "Absolutely not. ...Ugh. Fine.", "Bait? How rude. ...I'm going, I'm going."));
                        t.On("wd_bait", () => { t.Her(c.Pal, 1); t.Her(c.Her, 3); });
                    });
                    o.Play(b, "wd_1");

                    b.Narr("The music room door is open a crack. Moonlight on the piano. The keys go: plink... plonk... PLANG.");
                    b.Narr("A fat orange stray cat is strolling along the keyboard, very pleased with himself.");
                    Her(c, b, Feel.Laugh, c.T("It's a cat...! The ghost is a cat!", "THE GHOST IS A CAT! Best ghost ever!", "...Case closed. As expected.", "Pfft. The terrifying seventh wonder."));
                    var o2 = new Opts();
                    o2.If(S.Count("cat_food") > 0, "Offer him your cat food", "wd_feed", () =>
                    {
                        b.Narr("You shake the bag. The cat's ears turn. He trots over and eats out of your hand, purring like an engine.");
                        Her(c, b, Feel.Laugh, c.T("He likes you...! Can I pet him? Oh, he's so soft...", "He's PURRING! He loves us!", "...He has good taste. In food.", "Bribing a cat. Smooth."));
                        t.On("wd_feed", () => { t.Use("cat_food"); t.Her(c.Her, c.Animals ? 8 : 5, 2); });
                    }, 3f);
                    var caught = Roll(G.Str, 55);
                    o2.Keep("Try to catch him", "wd_catch", () =>
                    {
                        b.Narr(caught ? "You catch him. He scratches you, then decides he likes being held. Typical." : "He leaps over your head, out the window, and knocks a stack of sheet music onto both of you.");
                        Her(c, b, Feel.Laugh, c.T("Are you okay? Ahaha... sorry, it's funny...", "AHAHA! Paper everywhere! He won!", "...The cat outsmarted you.", "Beaten by a cat. Iconic."));
                        t.On("wd_catch", () => t.Her(c.Her, 4));
                    });
                    var song = Roll(G.Sens, 50);
                    o2.Add("Play him a song yourself [SEN]", "wd_play", () =>
                    {
                        b.Narr(song ? "You sit down and play the one song you know, slowly. The cat lies down on the lid and listens." : "You play the one song you know, badly. The cat looks at you with deep disappointment and leaves.");
                        Her(c, b, song ? Feel.Smile : Feel.Laugh, song ? c.T("That was beautiful... I didn't know you could play.", "Whoa! You can PLAY?! Again!", "...Not bad. At all.", "Hidden talent? I'm impressed.")
                                                                       : c.T("He... didn't like it. I did, though.", "Even the cat left! AHAHA!", "The cat has spoken.", "Critics everywhere."));
                        t.On("wd_play", () => { t.Her(c.Her, song ? 5 : 3); if (song) t.Sens += 1; });
                    });
                    o2.If(c.Tier >= 2, "Keep it a secret between you", "wd_secret", () =>
                    {
                        b.Me("Let's not tell anyone. The seventh wonder can be ours.");
                        Her(c, b, Feel.Smile, c.T("Our secret... I like that.", "A secret club of two! Plus one cat!", "...Fine. Our secret.", "A shared secret. You're full of surprises."));
                        t.On("wd_secret", () => t.Her(c.Her, 4, 6));
                    }, 1.5f);
                    o2.Play(b, "wd_2");
                    b.Narr("You close the window on the way out. Somewhere in the courtyard, a cat meows goodnight.");
                    Finale(c, b, t, "wonder");
                    b.Narr("The seventh wonder is solved. The school will never know.");
                }
            };
        }

        // ---------------- the haunted house ----------------

        private static Ep Haunt()
        {
            return new Ep
            {
                Id = "haunt", Title = "The haunted house", Bg = "haunted_house", PalOk = true,
                Weight = c => 1f + (c.Tone == Tone.Energetic || c.Tone == Tone.Teasing ? 0.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("The haunted house behind the arcade reopened... I want to try it, but not alone...", "The haunted house reopened! Whoever screams first buys crepes!",
                    "The haunted house reopened. I've heard it's not scary. Let's confirm.", "Haunted house. You and me. Loser of 'who screams first' buys crepes.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The haunted house behind the arcade: rotten boards, flickering lanterns, a sign that says NO REFUNDS.");
                    b.Narr("Inside it smells like dust and fake blood. The door slams shut behind you.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Remember... whoever screams first... buys crepes...", "The bet is ON! Prepare your wallet!", "I won't scream. Prepare your wallet.", "First one to scream pays. Ready?"));
                    var steady = Roll(G.Str, 55);
                    var o = new Opts();
                    o.Keep("Walk in front [STR]", "hh_front", () =>
                    {
                        b.Narr("A ghost bursts out of a closet with a horrible wail.");
                        b.Narr(steady ? "You don't even flinch. Behind you, she lets out a scream that echoes through the whole building." : "You make a noise you did not know you could make. Behind you, she is already laughing.");
                        Her(c, b, steady ? Feel.Pout : Feel.Laugh, steady ? c.T("That... that doesn't count! It came out by itself!", "NO! That doesn't count! It was a surprise scream!", "...That wasn't a scream. That was a cough.", "Ugh. Fine. I'll buy the crepes.")
                                                                           : c.T("You screamed first... crepes are on you.", "YOU SCREAMED! Crepes! CREPES!", "Crepes. Strawberry. Thank you.", "I'll take the most expensive crepe, please."));
                        t.On("hh_front", () => { t.Her(c.Her, 4, 2); if (!steady) t.Money -= 400; });
                    });
                    o.If(G.Int >= 30, "Read the map out loud [INT]", "hh_map", () =>
                    {
                        b.Narr("You take a shortcut through a door marked STAFF. Three zombies in full makeup look up from their lunch.");
                        b.Narr("\"...You're not supposed to be here.\" One of them points you back with a half-eaten sandwich.");
                        Her(c, b, Feel.Laugh, c.T("They were eating... onigiri...", "The ZOMBIES were having LUNCH! AHAHA!", "...The magic is gone.", "Best part of the haunted house."));
                        t.On("hh_map", () => t.Her(c.Her, 4));
                    });
                    o.If(G.Sens >= 40, "Scare the scarers [SEN]", "hh_scare", () =>
                    {
                        b.Narr("You hide behind a curtain. When the next ghost creeps by, you jump out first. He screams and drops his wig.");
                        Her(c, b, Feel.Laugh, c.T("You scared the ghost...!", "You made the GHOST scream! Legend!", "...That was evil. I approve.", "Turning the tables. Nice."));
                        t.On("hh_scare", () => { t.Her(c.Her, 4); t.Rep += 1; });
                    }, 1.5f);
                    o.If(c.Pal != null, "Let the two of them go first", "hh_pals", () =>
                    {
                        b.Narr("The two of them walk ahead, clinging to each other and shrieking at every shadow.");
                        Pal(c, b, Feel.Surprised, c.P("Why are we in front?! Why are WE in front?!", "THIS IS AMAZING! AAAAH!", "I'm not scared. She's scared. I'm holding her for her sake.", "Don't push me! ...Don't let go either!"));
                        t.On("hh_pals", () => { t.Her(c.Her, 3); t.Her(c.Pal, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 3)); });
                    }, 1.5f);
                    o.Play(b, "hh_1");

                    b.Narr("The last room: an old well in the middle of the floor. Something with long black hair is climbing out of it.");
                    var o2 = new Opts();
                    o2.Keep("RUN", "hh_run", () =>
                    {
                        b.Narr("All of you sprint for the exit, crash through the door and burst out into daylight, laughing like idiots.");
                        t.On("hh_run", () => t.Her(c.Her, 3));
                    });
                    o2.Add("Say hello to the ghost", "hh_hello", () =>
                    {
                        b.Me("Hi. Long day?");
                        b.Narr("The ghost stops. \"...Honestly? Yes.\" She turns out to be a university student working part-time. You chat for five minutes.");
                        Her(c, b, Feel.Laugh, c.T("You made friends with the ghost...", "You're friends with a GHOST now!", "...Only you would do that.", "Networking with the undead. Smart."));
                        t.On("hh_hello", () => { t.Her(c.Her, 4); t.Rep += 1; });
                    });
                    o2.If(c.Tier >= 2, "Stand between her and the well", "hh_shield", () =>
                    {
                        b.Narr("You step in front of her without thinking. The ghost stops, impressed, and gives you a thumbs-up.");
                        Her(c, b, Feel.Smile, c.T("...You stood in front of me. Thank you.", "Whoa, that was so cool! My bodyguard!", "...Unnecessary. ...Thanks.", "Protecting me from a part-timer? Brave."));
                        t.On("hh_shield", () => t.Her(c.Her, 4, 5));
                    }, 1.5f);
                    o2.If(c.Tone == Tone.Energetic || c.Tone == Tone.Teasing || G.Sens >= 50, "Pretend to faint", "hh_faint", () =>
                    {
                        b.Narr("You collapse dramatically onto the floor. The ghost panics and calls for a manager.");
                        Her(c, b, Feel.Laugh, c.T("Are you okay?! ...You're faking! You're faking!", "AHAHA! You gave the ghost a heart attack!", "...Get up. Everyone is staring.", "Ten out of ten. Very convincing."));
                        t.On("hh_faint", () => t.Her(c.Her, 4));
                    });
                    o2.Play(b, "hh_2");
                    b.Narr("Outside, the crepe stand is right where it should be. Strawberry and cream, the reward of the brave.");
                    Finale(c, b, t, "haunt");
                    b.Narr("You both agree it wasn't scary at all. Neither of you believes it.");
                }
            };
        }

        // ---------------- the snow ----------------

        private static Ep Snow()
        {
            return new Ep
            {
                Id = "snow", Title = "A walk in the snow", Bg = "snow_street_night",
                Weight = c => 1f + (c.Low ? 1.5f : 0f) + (c.Tier >= 2 ? 0.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("They say it'll snow tonight... Would you walk home with me?", "Snow tonight! Walk home with me! We can make snow angels!",
                    "It's going to snow. Walk me home. The streets are dark.", "Snow tonight. Walk me home. You can carry my bag.")),
                Write = (c, b, t) =>
                {
                    b.Narr("Evening. Snow is falling on the empty street, soft and silent, under the streetlights.");
                    b.Narr("Your footsteps are the only sound. Her breath comes out in little white clouds.");
                    Callback(c, b);
                    Her(c, b, Feel.Worried, c.T("Ah... my glove. I had both of them a minute ago...", "My glove! Where's my glove?! It was right here!", "...I lost a glove. Wonderful.", "Great. One glove. Very fashionable."));
                    var o = new Opts();
                    var found = Roll(G.Int, 45);
                    o.Keep("Retrace your steps [INT]", "sn_find", () =>
                    {
                        b.Narr(found ? "Back at the vending machine, on top of the trash can: a small knitted glove with snow on it." : "You walk back three blocks. No glove. But you find a snowman someone built with a traffic cone for a hat.");
                        Her(c, b, found ? Feel.Laugh : Feel.Laugh, found ? c.T("You found it...! Thank you!", "MY GLOVE! You're a detective!", "...Thanks. Good eyes.", "Not bad, detective.")
                                                                        : c.T("No glove... but that snowman is so cute.", "Forget the glove! LOOK AT HIM!", "...That snowman has more style than me.", "The cone hat is genius."));
                        t.On("sn_find", () => t.Her(c.Her, found ? 5 : 3, 2));
                    });
                    o.Add("Lend her your gloves", "sn_gloves", () =>
                    {
                        b.Narr("You pull off your gloves and hold them out. She stares at them.");
                        Her(c, b, Feel.Shy, c.T("But then your hands... ...Okay. Thank you. They're warm.", "Really?! ...They're so big on me! Ahaha!", "...You'll freeze. ...Fine. Thank you.", "Gallant. ...They're warm. Thanks."));
                        t.On("sn_gloves", () => t.Her(c.Her, 5, 3));
                    }, 1.3f);
                    var coffee = S.Count("canned_coffee") > 0;
                    o.If(coffee || S.Money >= 150, coffee ? "Share your canned coffee" : "Buy two hot drinks (¥150)", "sn_drink", () =>
                    {
                        b.Narr("The can is so hot she holds it with her sleeves. Steam rises between you.");
                        Her(c, b, Feel.Smile, c.Coffee ? c.T("Black coffee... how did you know?", "Coffee! Hot coffee! You're a lifesaver!", "...Black. You remembered.", "Black coffee. Good taste.")
                                                       : c.T("It's so warm... thank you.", "Hot hot hot! Thank you!", "...Warm. Thanks.", "My hands are thawing. Thanks."));
                        t.On("sn_drink", () => { if (coffee) t.Use("canned_coffee"); else t.Money -= 150; t.Her(c.Her, c.Coffee ? 7 : 4); });
                    }, c.Coffee ? 2.5f : 1f);
                    var hit = Roll(G.Str, 50);
                    o.If(c.Tone == Tone.Energetic || c.Tier >= 1, "Start a snowball fight [STR]", "sn_fight", () =>
                    {
                        b.Narr("A snowball, perfectly round. You throw it. War is declared.");
                        b.Narr(hit ? "Five minutes later she surrenders, covered in snow from head to toe and laughing too hard to throw." : "Five minutes later you are the snowman. She is merciless.");
                        Her(c, b, Feel.Laugh, c.T("I give up! I give up! Ahaha...", "REMATCH! Right now!", "...I let you win. Obviously.", "Victory is mine. Bow."));
                        t.On("sn_fight", () => t.Her(c.Her, 5));
                    }, 1.3f);
                    o.Play(b, "sn_1");

                    b.Bg("snow_park_night");
                    b.Narr("The little park near the station. One lamp, one bench, everything under a thin white blanket.");
                    b.Narr("You brush the snow off the bench and sit down. For a while, nobody says anything. It's nice.");
                    Her(c, b, Feel.Smile, c.T("It's so quiet... like the whole town is asleep.", "It's so pretty! Like a snow globe!", "...I like the quiet.", "Peaceful. For once."));
                    Finale(c, b, t, "snow");
                    b.Narr("The snow keeps falling as you walk her to her corner. She waves until you're out of sight.");
                }
            };
        }

        // ---------------- the gym ----------------

        private static Ep Gym()
        {
            return new Ep
            {
                Id = "gym", Title = "After hours in the gym", Bg = "gym",
                Weight = c => c.Sporty ? 2.5f : 0.7f,
                Invite = (c, i) => i.Say(c.Her, c.Sporty
                    ? c.T("Could you help me practice after school? I need someone to play against.", "One-on-one! After school! I need a rival!", "I need a practice partner. You'll do.", "Practice with me after school. I'll go easy on you.")
                    : c.T("Um... could you teach me to shoot a basket? Please don't laugh.", "Teach me basketball! I've never made a single basket in my life!", "I want to learn to shoot a basket. Don't make it weird.", "Teach me basketball. If I'm bad, you're not allowed to laugh.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The gym after hours: one row of lights on, the smell of floor wax, the echo of a single ball bouncing.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.Sporty ? c.T("Don't hold back, okay? I want to get better.", "Okay! Game face! Don't go easy on me!", "Play seriously. Or don't bother.", "Try to keep up.")
                                                     : c.T("It's so far up... how does anyone get it in?", "Okay! I'm gonna dunk! ...What's a dunk?", "The hoop is too high. That's a design flaw.", "How hard can it be? ...Don't answer."));
                    var o = new Opts();
                    var win = Roll(G.Str, c.Sporty ? 70 : 45);
                    o.Keep("Play one-on-one [STR]", "gy_1on1", () =>
                    {
                        b.Narr(win ? "It's close. At the last second you sink one from the corner. She falls on her back, laughing and panting." : "She runs circles around you. By the end you're on the floor and she's spinning the ball on one finger.");
                        Her(c, b, win ? Feel.Pout : Feel.Laugh, win ? c.T("You're good... One more! Just one more!", "NO WAY! Rematch! RIGHT NOW!", "...Lucky. Again.", "Okay, okay. You can play. A little.")
                                                                    : c.T("Ahaha... are you okay down there?", "I WIN! Victory dance!", "Too easy. Practice more.", "Need a hand up, champ?"));
                        t.On("gy_1on1", () => { t.Her(c.Her, 4, 2); t.Str += 1; });
                    });
                    var form = Roll(G.Int, 50);
                    o.Add("Fix her form [INT]", "gy_form", () =>
                    {
                        b.Me("Elbow under the ball. Push with your legs, not your arms. Follow through, like you're reaching into a cookie jar.");
                        b.Narr(form ? "She tries it. Swish. Again. Swish. Five in a row. She stares at her hands." : "She tries it. The ball hits the bottom of the rim and bounces off her head. She laughs anyway.");
                        Her(c, b, Feel.Laugh, form ? c.T("It works...! You're a good teacher!", "FIVE IN A ROW! Coach! You're my coach now!", "...That's actually correct. Thanks.", "A cookie jar. Ridiculous. ...It works.")
                                                   : c.T("Ow... ahaha... the ball hates me.", "It attacked me! The ball is evil!", "...The physics were sound. The ball was not.", "Okay, that one was my fault."));
                        t.On("gy_form", () => { t.Her(c.Her, form ? 5 : 3, 2); t.Int += 1; });
                    }, c.Sporty ? 1f : 1.8f);
                    o.If(S.Count("sports_drink") > 0, "Toss her a sports drink", "gy_drink", () =>
                    {
                        b.Narr("You toss her a cold bottle from your bag. She catches it one-handed.");
                        Her(c, b, Feel.Smile, c.T("You brought this for me? Thank you...", "Ice cold! You're the best!", "...Prepared. Good.", "Thoughtful. I'll allow it."));
                        t.On("gy_drink", () => { t.Use("sports_drink"); t.Her(c.Her, c.Sporty ? 7 : 4); });
                    }, 2f);
                    o.Add("Rebound for her while she shoots", "gy_rebound", () =>
                    {
                        b.Narr("A hundred shots. You catch every one and pass it back. Somewhere around fifty, she starts talking.");
                        Her(c, b, Feel.Worried, c.Sporty ? c.T("The tournament is next month... Everyone's counting on me. I can't sleep some nights.", "Tournament's coming! I'm SO excited! ...And so scared, honestly.", "The tournament. If I miss, it's on me.", "The tournament's next month. Everyone thinks I'm confident. I'm faking it.")
                                                        : c.T("I've always been bad at sports... I just wanted to be good at one thing.", "Everybody's sporty except me! I wanted to try!", "I hate being bad at things.", "I hate losing. Even at things I don't care about."));
                        t.On("gy_rebound", () => t.Her(c.Her, 3, 5));
                    });
                    o.Play(b, "gy_1");

                    b.Narr("The gym door bangs open. The basketball captain, a towel on her shoulder, looks at the two of you.");
                    b.Narr("\"Free throws. Three each. Loser mops the floor.\"");
                    var o2 = new Opts();
                    var shot = Roll(G.Str, 55);
                    o2.Keep("Accept the challenge [STR]", "gy_duel", () =>
                    {
                        b.Narr(shot ? "Three for three. The captain whistles, tosses you the towel and leaves. \"Not bad, rookie.\"" : "One for three. The captain hands you the mop with a smile. She grabs a second mop and helps.");
                        Her(c, b, Feel.Laugh, shot ? c.T("You beat the captain...!", "YOU BEAT THE CAPTAIN! Nobody does that!", "...Impressive.", "Okay, that was cool.")
                                                   : c.T("I'll help you mop. We're a team.", "Mop race! Go!", "...Give me the other mop.", "Mopping together. Very glamorous."));
                        t.On("gy_duel", () => { t.Her(c.Her, 3, shot ? 1 : 4); t.Rep += shot ? 2 : 0; });
                    });
                    var her = c.Sporty || Roll(40, 55);
                    o2.Add("Let her take the challenge", "gy_her", () =>
                    {
                        b.Narr(her ? "She steps up. Swish. Swish. Swish. The captain stares, then laughs and shakes her hand." : "She misses all three, then bursts out laughing with the captain. They're friends by the time the mop comes out.");
                        Her(c, b, her ? Feel.Laugh : Feel.Smile, her ? c.T("I did it...! Did you see?", "THREE FOR THREE! Did you SEE that?!", "...As expected.", "Easy.")
                                                                     : c.T("I missed everything... but that was fun.", "I missed them ALL! Ahaha! Best day ever!", "...Next time.", "I wasn't trying. Obviously."));
                        t.On("gy_her", () => t.Her(c.Her, her ? 6 : 4, 3));
                    }, 1.3f);
                    o2.If(c.Famous, "Sign the captain's notebook", "gy_famous", () =>
                    {
                        b.Narr("The captain squints at you. \"Wait... you're that guy. Sign my notebook. My little sister won't believe it.\"");
                        Her(c, b, Feel.Surprised, c.T("You're famous...?", "AUTOGRAPHS?! Since when?!", "...Unbelievable.", "Look at you, a celebrity."));
                        t.On("gy_famous", () => { t.Rep += 1; t.Her(c.Her, 3); });
                    });
                    o2.Play(b, "gy_2");
                    b.Narr("You turn off the last row of lights together. The gym goes dark and quiet behind you.");
                    Finale(c, b, t, "gym");
                }
            };
        }

        // ---------------- the time trial ----------------

        private static Ep Track()
        {
            return new Ep
            {
                Id = "track", Title = "The time trial", Bg = "sports_track", PalOk = true,
                Weight = c => c.Sporty ? 2f : 0.6f,
                Invite = (c, i) => i.Say(c.Her, c.T("The club time trial is tomorrow... Could you time me after school?", "Time trial tomorrow! Time me! I'm gonna break my record!",
                    "I need someone with a stopwatch and no opinions. After school.", "Time me after school. If I break my record, you buy me a drink.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The track at sunset. The tents from the last meet are still up, flapping in the wind.");
                    b.Narr("She ties her shoes twice, then three times, then stretches like her life depends on it.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Okay... I'm ready. I think. Maybe.", "Okay! Let's GO! Timer ready?!", "Ready. Don't miss the start.", "Watch closely. This'll be good."));
                    var o = new Opts();
                    var best = Roll(c.Sporty ? 60 : 30, 55);
                    o.Keep("Time her and call the splits", "tk_time", () =>
                    {
                        b.Narr("You shout her splits at every corner. On the last straight she gives everything.");
                        b.Narr(best ? "She crosses the line. You look at the phone. A new personal best by three tenths of a second." : "She crosses the line. Two tenths slower than her best. She lies down on the track and stares at the sky.");
                        Her(c, b, best ? Feel.Laugh : Feel.Pout, best ? c.T("Really?! I did it...!", "PERSONAL BEST! AAAH!", "...Good. Very good.", "Told you. Drink's on you.")
                                                                         : c.T("...Again. Once more. Please.", "ARGH! Again! I can do it!", "...Unacceptable. Again.", "The wind was against me. Obviously."));
                        t.On("tk_time", () => t.Her(c.Her, best ? 6 : 4, 2));
                    });
                    var keep = Roll(G.Str, 55);
                    o.Add("Pace her by running alongside [STR]", "tk_pace", () =>
                    {
                        b.Narr(keep ? "You keep up for a full lap. Then a second. She pushes harder just to beat you." : "You keep up for exactly half a lap. Then your legs file a formal complaint. She waves as she passes you.");
                        Her(c, b, Feel.Laugh, keep ? c.T("You're fast...! That helped so much!", "That was AWESOME! Race me again!", "...You kept up. Respect.", "Not bad for a non-athlete.")
                                                   : c.T("Are you alive...? Here, water.", "AHAHA! You're crawling!", "...Breathe. In. Out.", "Need a stretcher?"));
                        t.On("tk_pace", () => { t.Her(c.Her, 4); t.Str += 1; });
                    });
                    var start = Roll(G.Int, 50);
                    o.Add("Study her start [INT]", "tk_start", () =>
                    {
                        b.Me("You lift your head too early. Stay low for three steps, then come up.");
                        b.Narr(start ? "She tries it. The first ten meters are clearly faster. She looks at you like you performed a miracle." : "She tries it and nearly trips. The second time, it's a little better.");
                        Her(c, b, Feel.Smile, c.T("How did you notice that? Thank you...", "Whoa, you're like a real coach!", "...Good eye.", "You were watching closely, huh."));
                        t.On("tk_start", () => { t.Her(c.Her, start ? 6 : 3, 2); t.Int += 1; });
                    }, 1.2f);
                    o.If(c.Pal != null, "Let her friend race her", "tk_race", () =>
                    {
                        Pal(c, b, Feel.Serious, c.P("Me...? Okay. I'll try my best.", "A race?! You're ON!", "Fine. Don't cry when you lose.", "Prepare to be humbled."));
                        b.Narr("They race side by side the whole way. Photo finish. They both claim they won and argue about it for ten minutes.");
                        t.On("tk_race", () => { t.Her(c.Her, 3); t.Her(c.Pal, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 4)); });
                    }, 1.5f);
                    o.Play(b, "tk_1");

                    b.Bg("sports_tents");
                    b.Narr("The wind picks up. One of the tents tears loose and goes tumbling across the field like a giant jellyfish.");
                    var o2 = new Opts();
                    var catchIt = Roll(G.Str, 50);
                    o2.Keep("Chase the tent [STR]", "tk_chase", () =>
                    {
                        b.Narr(catchIt ? "You dive and grab a pole. The tent drags you three meters, but you hold on. The track club cheers." : "You catch it. Then it catches you. You end up wrapped inside it like a burrito.");
                        Her(c, b, Feel.Laugh, c.T("Ahaha... are you okay in there?", "BURRITO! Hold still, I'm taking a picture!", "...Heroic. Ridiculous, but heroic.", "Best catch of the season."));
                        t.On("tk_chase", () => { t.Her(c.Her, 4); t.Rep += 1; });
                    });
                    o2.If(G.Int >= 35, "Organize everyone to tie the tents down [INT]", "tk_org", () =>
                    {
                        b.Narr("You shout instructions. In five minutes the whole club is holding ropes, and every tent is staked down.");
                        b.Narr("The club captain comes over. \"You should join. We need someone who can think.\"");
                        t.On("tk_org", () => { t.Rep += 2; t.Her(c.Her, 3); });
                    }, 1.2f);
                    o2.Add("Take shelter in a tent", "tk_shelter", () =>
                    {
                        b.Narr("You duck into a tent until the gust passes. Sitting on the folded mats, she suddenly talks.");
                        Her(c, b, Feel.Worried, c.Sporty ? c.T("Sometimes I wonder why I run so much. I think... it's the only time I don't worry.", "When I run, my head goes quiet! That's why I love it!", "Running is simple. Everything else isn't.", "Running is the only thing I'm sure I'm good at.")
                                                        : c.T("I'm not really sporty... I just wanted to see what it's like to try hard at something.", "I'm not even in a sports club! I just wanted to feel cool!", "I wanted to know if I could do it. That's all.", "I wanted to prove I could. Mostly to myself."));
                        t.On("tk_shelter", () => t.Her(c.Her, 3, 5));
                    });
                    o2.Play(b, "tk_2");
                    Finale(c, b, t, "track");
                    b.Narr("The sun goes down behind the bleachers. Tomorrow's time trial suddenly feels easy.");
                }
            };
        }

        // ---------------- the make-up test ----------------

        private static Ep Study()
        {
            return new Ep
            {
                Id = "study", Title = "The make-up test", Bg = "classroom_exam",
                Weight = c => 1f + (c.Bookish ? 0.8f : 0f) + (c.Mood == Mood.Stressed ? 1f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("I have a make-up test tomorrow and I don't understand anything... Could you help me? Please?", "Make-up test tomorrow and my brain is EMPTY! Save me!",
                    "Make-up test tomorrow. I need help. Don't make me say it twice.", "I need a tutor. You're the least boring option. Congratulations.")),
                Write = (c, b, t) =>
                {
                    b.Narr("An empty classroom, desks still in exam rows from this morning. Her textbook is open to a page covered in red marks.");
                    Callback(c, b);
                    Her(c, b, Feel.Worried, c.T("I read it ten times... it just doesn't go in.", "I don't get ANY of it! The letters are dancing!", "I understand the words. Not the math.", "This page is my enemy."));
                    var o = new Opts();
                    var click = Roll(G.Int, 55);
                    o.Keep("Go through it step by step [INT]", "sd_steps", () =>
                    {
                        b.Narr(click ? "You go line by line. Halfway through, she grabs the pencil from you. \"Wait. Wait. I get it.\"" : "You go line by line. Halfway through, you realize you don't get it either. You stare at each other and burst out laughing.");
                        Her(c, b, click ? Feel.Laugh : Feel.Laugh, click ? c.T("I get it...! I actually get it!", "IT MAKES SENSE! I'm a genius! You're a genius!", "...That's it. That's all it was.", "Okay. That was almost too easy.")
                                                                         : c.T("We're both lost... ahaha...", "We're DOOMED! Together!", "...The blind leading the blind.", "Great tutor. Five stars."));
                        t.On("sd_steps", () => { t.Her(c.Her, click ? 6 : 3, 2); t.Int += 1; });
                    });
                    var guide = S.Count("study_guide") > 0 ? "study_guide" : S.Count("notebook") > 0 ? "notebook" : "";
                    o.If(guide != "", "Give her your " + ItemName(guide), "sd_guide", () =>
                    {
                        b.Narr("You slide your " + ItemName(guide) + " across the desk.");
                        Her(c, b, Feel.Shy, c.T("You'd give me this...? Thank you, really.", "WHOA! This is gonna save my life!", "...Useful. I owe you.", "Bribing me with stationery? It works."));
                        t.On("sd_guide", () => { t.Use(guide); t.Her(c.Her, c.Bookish ? 8 : 5, 2); });
                    }, 2f);
                    var catchy = Roll(G.Sens, 45);
                    o.Add("Turn the formulas into a song [SEN]", "sd_song", () =>
                    {
                        b.Narr(catchy ? "Three verses and a chorus. By the end she's singing it on her own, tapping the beat on the desk." : "The song is objectively terrible. She remembers every word of it anyway, mostly out of trauma.");
                        Her(c, b, Feel.Laugh, c.T("It's stuck in my head... in a good way.", "I'm gonna sing it during the test! Out loud!", "...I hate that it works.", "That's the worst song I've ever heard. I'll never forget it."));
                        t.On("sd_song", () => t.Her(c.Her, 5));
                    });
                    o.If(c.Snack != "", "Snack break first", "sd_snack", () =>
                    {
                        b.Narr("You split your " + ItemName(c.Snack) + ". Ten minutes of not thinking about math.");
                        Her(c, b, Feel.Smile, c.T("I feel better already... thank you.", "Brain fuel! Okay, I'm ready now!", "...Good idea.", "Smart. Feed the student first."));
                        t.On("sd_snack", () => { t.Use(c.Snack); t.Her(c.Her, 4, 2); });
                    });
                    o.Play(b, "sd_1");

                    b.Narr("The door slides open. The math teacher, holding a stack of papers: \"Oh, good. Volunteers for a practice quiz.\"");
                    var o2 = new Opts();
                    var quiz = Roll(G.Int, 50);
                    o2.Keep("Take the quiz together", "sd_quiz", () =>
                    {
                        b.Narr(quiz ? "Ten questions. She gets eight right. The teacher raises an eyebrow and writes something in his notebook." : "Ten questions. She gets five. \"Better than last time,\" the teacher says. Coming from him, that's a compliment.");
                        Her(c, b, quiz ? Feel.Laugh : Feel.Smile, quiz ? c.T("Eight...! I've never gotten eight!", "EIGHT! EIGHT OUT OF TEN!", "...Eight. Acceptable.", "Eight. I'm a genius, apparently.")
                                                                       : c.T("Five... that's better than before, right?", "Five! Halfway there! That counts!", "...Progress.", "Five. Baby steps."));
                        t.On("sd_quiz", () => { t.Her(c.Her, quiz ? 5 : 3, 2); if (quiz) t.Rep += 1; });
                    });
                    var bluff = Roll(G.Sens, 55);
                    o2.If(G.Sens >= 35, "Claim you're the class rep on official business [SEN]", "sd_bluff", () =>
                    {
                        b.Narr(bluff ? "\"Ah, of course. Carry on.\" The teacher leaves. She stares at you in awe." : "\"The class rep is my niece.\" A long silence. You both take the quiz.");
                        Her(c, b, Feel.Laugh, bluff ? c.T("How did that work...?", "You're a criminal mastermind!", "...Dangerous. I respect it.", "Smooth. Very smooth.")
                                                    : c.T("His niece... ahaha...", "HIS NIECE! AHAHA!", "...We're doomed.", "Worst bluff in history."));
                        t.On("sd_bluff", () => t.Her(c.Her, 4));
                    });
                    o2.Add("Run for it", "sd_run", () =>
                    {
                        b.Narr("You grab her bag and your bag and bolt out the back door. You hear him shouting your names down the hall.");
                        Her(c, b, Feel.Laugh, c.T("We ran away from a teacher...! I've never done that!", "WE'RE OUTLAWS NOW!", "...That was stupid. Let's do it again sometime.", "My first crime. With you. Memorable."));
                        t.On("sd_run", () => { t.Her(c.Her, 4); t.Rep -= 1; });
                    });
                    o2.Play(b, "sd_2");

                    b.Bg("classroom_sunset");
                    b.Narr("Later. The sun is going down, and the whole room glows orange. The red marks on her page are crossed out, one by one.");
                    Finale(c, b, t, "study");
                    b.Narr("Two days later, she runs up to you holding her test over her head. You can see the score from across the hallway.");
                }
            };
        }

        // ---------------- the temple town ----------------

        private static Ep Trip()
        {
            return new Ep
            {
                Id = "trip", Title = "The temple town", Bg = "train", PalOk = true,
                Weight = c => 0.8f + (c.Tier >= 1 ? 0.5f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("This Sunday... would you come with me to the old temple town? I've always wanted to see it.", "Sunday! Temple town! Day trip! You and me! Say yes!",
                    "I'm going to the temple town on Sunday. You can come. If you want.", "Sunday trip to the temple town. You're coming. I already bought the tickets. Kidding. Mostly.")),
                Write = (c, b, t) =>
                {
                    b.Narr("Sunday morning. The train rattles out of the city, fields and rivers sliding past the window.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("I'm so excited... I barely slept.", "Look! A river! Look! A cow!", "...The view's nice.", "Window seat's mine, by the way."));
                    var o = new Opts();
                    o.Keep("Play cards for the window seat", "tp_cards", () =>
                    {
                        b.Narr("Old maid, best of five. She cheats. Very obviously. You pretend not to notice.");
                        Her(c, b, Feel.Laugh, c.T("I won...! Um... I didn't cheat. ...Maybe a little.", "I WIN! Window seat is MINE!", "...I won fairly. Mostly.", "You let me win. I saw that."));
                        t.On("tp_cards", () => t.Her(c.Her, 4));
                    });
                    o.If(S.Count("bento") > 0, "Share your deluxe bento", "tp_bento", () =>
                    {
                        b.Narr("You open the bento on your knees. Rolled omelet, karaage, rice shaped like a little bear.");
                        Her(c, b, Feel.Laugh, c.T("It's so cute... I can't eat the bear.", "BEAR RICE! I'm eating the ears first!", "...Good bento.", "Bear-shaped rice. You have a soft side."));
                        t.On("tp_bento", () => { t.Use("bento"); t.Her(c.Her, c.Foodie ? 8 : 5); });
                    }, 2f);
                    o.If(G.Int >= 30, "Plan the route [INT]", "tp_plan", () =>
                    {
                        b.Narr("You draw a map on a napkin: the temple, the old street, the best mochi shop, the station, in the perfect order.");
                        Her(c, b, Feel.Smile, c.T("You thought of everything...", "You're like a tour guide!", "...Efficient. Good.", "Organized. I like it."));
                        t.On("tp_plan", () => { t.Int += 1; t.Her(c.Her, 3, 3); });
                    });
                    o.If(c.Pal != null, "Let the two of them sit together", "tp_pals", () =>
                    {
                        b.Narr("The two of them take the seats across from you and talk nonstop for an hour.");
                        Pal(c, b, Feel.Laugh, c.P("Sorry, are we ignoring you? ...Just a little longer.", "Okay okay, and THEN what happened?!", "...We'll include you. Eventually.", "You're our audience. Look entertained."));
                        t.On("tp_pals", () => { t.Her(c.Her, 3); t.Her(c.Pal, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 4)); });
                    }, 1.5f);
                    o.Play(b, "tp_1");

                    b.Bg("kyoto_temple");
                    b.Narr("The temple town: stone steps, red gates, incense drifting between old wooden houses.");
                    Her(c, b, Feel.Surprised, c.T("It's even more beautiful than in the pictures...", "WHOA! It's like a movie!", "...Worth the trip.", "Okay. I'm impressed."));
                    var o2 = new Opts();
                    var luck = Random.Range(0, 3);
                    o2.Keep("Draw omikuji fortunes", "tp_omikuji", () =>
                    {
                        b.Narr(luck == 0 ? "You both draw Great Blessing. The shrine maiden says that almost never happens." : luck == 1 ? "She draws Great Blessing. You draw Great Curse. She laughs for a full minute." : "You both draw Curse. You tie them to the tree together, as the rules say, to leave the bad luck behind.");
                        Her(c, b, Feel.Laugh, c.T("This is a good memory, no matter what it says.", "Fortunes! Ahaha! Best part!", "...It's just paper. ...Let me read yours again.", "Fate is funny."));
                        t.On("tp_omikuji", () => t.Her(c.Her, 4));
                    });
                    o2.If(S.Money >= 800, "Buy her a lucky charm (¥800)", "tp_charm", () =>
                    {
                        b.Narr("A little charm for good health, with a tiny bell. You hand it to her.");
                        Her(c, b, Feel.Shy, c.T("For me...? I'll hang it on my bag. Thank you.", "It jingles! I love it!", "...Thank you. I'll keep it.", "A souvenir from you? I'll keep it."));
                        t.On("tp_charm", () => { t.Money -= 800; t.Her(c.Her, 6, 3); });
                    }, 1.3f);
                    var climb = Roll(G.Str, 55);
                    o2.Add("Climb all the stairs to the top [STR]", "tp_climb", () =>
                    {
                        b.Narr(climb ? "Three hundred steps. At the top, the whole valley spreads out below you, roofs and rivers and mountains." : "Three hundred steps. At step two hundred you sit down and question every decision you ever made.");
                        Her(c, b, Feel.Smile, climb ? c.T("Look at this view... I'll never forget it.", "WE MADE IT! KING OF THE MOUNTAIN!", "...Worth it.", "Okay. That was worth it.")
                                                    : c.T("Come on, just a little more... I'll wait for you.", "Come on! I'll carry your bag! Not you though!", "...Get up. We're close.", "Need me to push you?"));
                        t.On("tp_climb", () => { t.Her(c.Her, 4, 2); t.Str += 1; });
                    });
                    o2.Add("Write wishes on an ema", "tp_ema", () =>
                    {
                        b.Narr("Wooden plaques, a marker each. She writes hers fast and hangs it high.");
                        Her(c, b, Feel.Shy, c.T("I wished we'd stay friends for a long time. ...Don't laugh.", "I wished for ten more trips like this one!", "...I wished for something about my family. Private.", "I wished for something. You'll find out when it comes true."));
                        t.On("tp_ema", () => t.Her(c.Her, 3, 5));
                    });
                    o2.Play(b, "tp_2");
                    Finale(c, b, t, "trip");
                    b.Bg("train");
                    b.Narr("The train home. She falls asleep against the window, holding a bag of souvenirs like a treasure.");
                }
            };
        }

        // ---------------- the tea room ----------------

        private static Ep Tea()
        {
            return new Ep
            {
                Id = "tea", Title = "The tea room", Bg = "inn_room_day",
                Weight = c => 0.8f + (c.Club.IndexOf("Tea", StringComparison.OrdinalIgnoreCase) >= 0 ? 2f : 0f) + (c.Tone == Tone.Cool || c.Tone == Tone.Gentle ? 0.3f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("I'm practicing for a tea ceremony demonstration... Would you be my guest? I need practice.", "Tea ceremony practice! I need a guest! You! You're the guest!",
                    "I need a guest for tea ceremony practice. Sit still and be polite. That's all.", "Be my guest for tea ceremony practice. Try not to embarrass yourself.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The traditional room behind the club building: tatami, sliding doors, a small garden with a stone lantern.");
                    b.Narr("She kneels very straight and whisks the tea with a seriousness you have never seen on her face.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Please... take your seat.", "Okay! Very serious now! Shhh!", "Sit. Quietly.", "Sit. And no jokes."));
                    var o = new Opts();
                    var polite = Roll(G.Int, 50);
                    o.Keep("Follow the etiquette exactly [INT]", "te_rules", () =>
                    {
                        b.Narr(polite ? "You bow, turn the bowl twice, drink in three sips and praise the tea. She nods, almost proud." : "You turn the bowl the wrong way. Then the other wrong way. She bites her lip very hard.");
                        Her(c, b, polite ? Feel.Smile : Feel.Laugh, polite ? c.T("Perfect... You studied this?", "Whoa! You're a natural!", "...Correct. Surprisingly.", "Huh. You actually know this.")
                                                                           : c.T("Ah... it's okay... ahaha... sorry...", "AHAHA! That's not how... AHAHA!", "...Wrong. Very wrong.", "That was a disaster. I loved it."));
                        t.On("te_rules", () => { t.Her(c.Her, polite ? 5 : 3, 2); if (polite) t.Int += 1; });
                    });
                    o.If(S.Count("daifuku") > 0, "Offer your strawberry daifuku as the sweet", "te_daifuku", () =>
                    {
                        b.Narr("Tea needs a sweet. You produce a strawberry daifuku from your bag, a little squashed.");
                        Her(c, b, Feel.Laugh, c.T("That's perfect...! How did you know?", "DAIFUKU! The perfect tea sweet!", "...Correct choice.", "Prepared, aren't you."));
                        t.On("te_daifuku", () => { t.Use("daifuku"); t.Her(c.Her, c.Sweet ? 8 : 5); });
                    }, 2.5f);
                    var compliment = Roll(G.Sens, 45);
                    o.Add("Compliment the tea bowl [SEN]", "te_bowl", () =>
                    {
                        b.Me("This bowl is beautiful. The glaze looks like a lake in winter.");
                        Her(c, b, compliment ? Feel.Shy : Feel.Laugh, compliment ? c.T("My grandmother made it... thank you.", "Right?! It's my favorite one!", "...You have an eye. Good.", "Poetic. I didn't expect that.")
                                                                                 : c.T("...It's from a hundred-yen shop. But thank you.", "It's from the hundred-yen shop! Ahaha!", "...Hundred yen.", "It's a hundred-yen bowl, you poet."));
                        t.On("te_bowl", () => t.Her(c.Her, compliment ? 5 : 3));
                    });
                    o.Add("Ask her why she likes this", "te_why", () =>
                    {
                        Her(c, b, Feel.Smile, c.T("Because everything slows down. In here, nothing is in a hurry.", "Because it's the only time I'm quiet! Ahaha!", "Because it's precise. Every movement has a reason.", "Because people think I can't be serious. I can."));
                        t.On("te_why", () => t.Her(c.Her, 3, 5));
                    });
                    o.Play(b, "te_1");

                    b.Narr("Twenty minutes of kneeling. You can no longer feel your legs. You're not sure you still have legs.");
                    var o2 = new Opts();
                    o2.Keep("Stand up anyway", "te_stand", () =>
                    {
                        b.Narr("You stand. Your legs do not. You fold onto the tatami like a dropped puppet.");
                        Her(c, b, Feel.Laugh, c.T("Ahaha... I'm sorry, I'm sorry... are you okay?", "AHAHAHA! You fell like a tree!", "...Pfft. Dignified.", "Timber."));
                        t.On("te_stand", () => t.Her(c.Her, 4));
                    });
                    var endure = Roll(G.Str, 60);
                    o2.Add("Endure with dignity [STR]", "te_endure", () =>
                    {
                        b.Narr(endure ? "You sit perfectly still until the end. She looks at you with new respect." : "You endure for exactly one more minute, then tip over sideways, very slowly.");
                        Her(c, b, Feel.Smile, endure ? c.T("You lasted the whole time...", "Iron legs! Impressive!", "...Discipline. I respect that.", "Okay, you win this round.") : c.T("Ahaha... you tried so hard.", "SLOW MOTION FALL! AHAHA!", "...Almost.", "So close."));
                        t.On("te_endure", () => { t.Her(c.Her, 4); if (endure) t.Str += 1; });
                    });
                    o2.Add("Admit your legs are dead", "te_admit", () =>
                    {
                        b.Me("I can't feel my legs.");
                        Her(c, b, Feel.Laugh, c.T("...Me neither. I never can. Ahaha...", "Me neither! Mine died ten minutes ago!", "...Neither can I. Don't tell anyone.", "Honestly? Mine too. Nobody can."));
                        b.Narr("You both collapse onto the tatami and lie there laughing, waiting for the pins and needles to pass.");
                        t.On("te_admit", () => t.Her(c.Her, 4, 4));
                    });
                    o2.Play(b, "te_2");
                    Finale(c, b, t, "tea");
                    b.Narr("Outside, the stone lantern in the garden catches the last light of the afternoon.");
                }
            };
        }

        // ---------------- the wishing street ----------------

        private static Ep Wishes()
        {
            return new Ep
            {
                Id = "wishes", Title = "The wishing street", Bg = "tanabata_street", PalOk = true,
                Weight = c => 0.9f,
                Invite = (c, i) => i.Say(c.Her, c.T("The old street has its wishing bamboo up... Would you write a wish with me?", "The wishing bamboo is up! Let's write wishes! Big ones!",
                    "They put up the wishing bamboo on the old street. I want to write one. Come.", "Wishing bamboo. You and me. Let's see what you wish for.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The old street near the station: bamboo branches heavy with paper strips of every color, lanterns swaying overhead.");
                    b.Narr("A little table with pens and blank strips. Hundreds of wishes flutter in the warm wind.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("What will you wish for...?", "Okay! Biggest wish wins!", "Think carefully. Wishes are serious.", "Make it a good one."));
                    var o = new Opts();
                    o.Keep("'Good grades for both of us.'", "ws_grades", () =>
                    {
                        Her(c, b, Feel.Laugh, c.T("Practical... and kind. That's very you.", "Boring! ...Thanks for including me though!", "...Sensible.", "Wasting a wish on grades? Classic you."));
                        t.On("ws_grades", () => { t.Int += 1; t.Her(c.Her, 3); });
                    });
                    o.If(c.Tier >= 1, "'Let her dream come true.'", "ws_dream", () =>
                    {
                        Her(c, b, Feel.Shy, c.T("You wished... for me? ...Thank you.", "For ME?! You used your wish on me?!", "...You didn't have to do that.", "...That's not fair. Now I have to wish for you."));
                        t.On("ws_dream", () => t.Her(c.Her, 5, 5, 1));
                    }, 1.5f);
                    o.Add("'More days like this one.'", "ws_days", () =>
                    {
                        Her(c, b, Feel.Smile, c.T("...Me too.", "YES! Same! A hundred more!", "...Fine wish.", "Good answer."));
                        t.On("ws_days", () => t.Her(c.Her, 4, 2));
                    });
                    o.If(c.Infamous, "'A better reputation. Please. Anything.'", "ws_rep", () =>
                    {
                        Her(c, b, Feel.Laugh, c.T("Ahaha... it's not that bad. Okay, it's a little bad.", "AHAHA! The gods heard you sobbing!", "...They're going to need a bigger miracle.", "Desperate times."));
                        t.On("ws_rep", () => { t.Rep += 1; t.Her(c.Her, 3); });
                    }, 1.5f);
                    o.Play(b, "ws_1");

                    b.Narr("She writes hers with her back to you and ties it high, on her toes.");
                    var o2 = new Opts();
                    o2.Keep("Respect her privacy", "ws_respect", () =>
                    {
                        b.Narr("You look the other way. When you turn back, she's watching you.");
                        if (c.Trust >= 50)
                            Her(c, b, Feel.Shy, c.T("...You can read it. I don't mind. It says: 'I want to be braver.'", "...Fine, I'll tell you! 'Let me stop pretending everything's fine.'", "...It says 'Let me be honest with people.' There. Now you know.", "...It says 'I want someone to see the real me.' Happy?"));
                        else Her(c, b, Feel.Smile, c.T("Thank you for not looking.", "Thanks! It's a secret wish!", "...Appreciated.", "Smart move."));
                        t.On("ws_respect", () => t.Her(c.Her, 3, c.Trust >= 50 ? 7 : 4));
                    });
                    var sneaky = Roll(G.Sens, 55);
                    o2.Add("Try to read it [SEN]", "ws_peek", () =>
                    {
                        b.Narr(sneaky ? "You catch a glimpse as the wind turns it: \"I want to make one friend who stays.\"" : "You lean in. She grabs your face with both hands and turns it away. Firmly.");
                        Her(c, b, Feel.Pout, sneaky ? c.T("...You read it, didn't you. ...It's okay. You're one, you know.", "HEY! You read it! ...Well. You're kind of the answer anyway!", "...You read it. ...Don't say anything.", "...Snoop. ...Well. Now you know.")
                                                    : c.T("No peeking...!", "NO! Cheater!", "Absolutely not.", "Nice try."));
                        t.On("ws_peek", () => t.Her(c.Her, sneaky ? 4 : 1, sneaky ? 3 : 0));
                    });
                    o2.If(c.Pal != null, "Swap wishes with her friend", "ws_swap", () =>
                    {
                        Pal(c, b, Feel.Shy, c.P("Mine says... 'Let us stay friends after graduation.' ...She's the 'us'.", "Mine says 'Best friends forever, for real this time!'", "...'Don't drift apart.' That's all.", "'Let her never get tired of me.' Don't tell her."));
                        Her(c, b, Feel.Surprised, c.T("...You wrote that? About me?", "Wait, is that about ME?!", "...Idiot. ...Me too.", "...Okay, that one got me."));
                        t.On("ws_swap", () => { t.Her(c.Her, 3); t.Her(c.Pal, 4, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 6)); });
                    }, 2f);
                    o2.Play(b, "ws_2");
                    Finale(c, b, t, "wishes");
                    b.Narr("Above the street, the paper strips turn slowly in the wind, carrying everybody's wishes up to the stars.");
                }
            };
        }

        // ---------------- the pumpkin disaster ----------------

        private static Ep Decor()
        {
            return new Ep
            {
                Id = "decor", Title = "The pumpkin disaster", Bg = "classroom_halloween", PalOk = true,
                Weight = c => 0.9f + (c.Tone == Tone.Energetic ? 0.4f : 0f),
                Invite = (c, i) => i.Say(c.Her, c.T("The class party is tomorrow and the room is still bare... Would you help me decorate?", "Party tomorrow! Room's empty! HELP!",
                    "I'm in charge of decorations. Nobody else showed up. You're drafted.", "Decorating duty. You and me. Bring your artistic soul.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The classroom after school, buried in orange paper, plastic spiders, pumpkins and a box labeled SPOOKY (DO NOT DROP).");
                    Callback(c, b);
                    Her(c, b, Feel.Laugh, c.T("Where do we even start...?", "Okay! Pumpkins first! Then spiders! Then EVERYTHING!", "We have two hours. Move.", "Let's make it so scary the teacher refuses to come in."));
                    var o = new Opts();
                    var art = Roll(G.Sens, 50);
                    o.Keep("Carve the pumpkins [SEN]", "dc_carve", () =>
                    {
                        b.Narr(art ? "Your pumpkin has a perfect, terrifying grin. She puts a candle in it and the whole corner glows." : "Your pumpkin looks less scary and more like it's having a very bad day. She names it Kevin.");
                        Her(c, b, Feel.Laugh, art ? c.T("It's so good...! It's the best one!", "WHOA! That's terrifying! I love him!", "...That's actually good.", "Okay, artist. Not bad.")
                                                  : c.T("Kevin... he looks so sad... I love him.", "KEVIN! Kevin is the class mascot now!", "...Kevin needs therapy.", "Kevin's my favorite. Don't tell the others."));
                        t.On("dc_carve", () => { t.Her(c.Her, 4); if (art) t.Sens += 1; });
                    });
                    var reach = Roll(G.Str, 50);
                    o.Add("Hang the streamers from the ceiling [STR]", "dc_hang", () =>
                    {
                        b.Narr(reach ? "You stand on a desk and hang the streamers corner to corner. She directs from below like a general." : "You stand on a chair. The chair has other plans. You come down with the streamers wrapped around you.");
                        Her(c, b, Feel.Laugh, reach ? c.T("It looks amazing...!", "PERFECT! Next one!", "...Straight. Good.", "Nice work, ladder.") : c.T("You look like a mummy... ahaha...", "MUMMY COSTUME! You're done! You're ready!", "...Efficient costume.", "A mummy. Fitting."));
                        t.On("dc_hang", () => { t.Her(c.Her, 4); if (reach) t.Str += 1; });
                    });
                    var design = Roll(G.Int, 50);
                    o.If(G.Int >= 30, "Design the whole layout [INT]", "dc_plan", () =>
                    {
                        b.Narr(design ? "A haunted path from the door, a 'graveyard' of desks, cobwebs in the corners. The class rep will cry tears of joy." : "Your layout has a slight problem: nobody can reach the door.");
                        t.On("dc_plan", () => { t.Her(c.Her, 3); if (design) { t.Rep += 1; t.Int += 1; } });
                    });
                    o.If(c.Pal != null, "Let the two of them handle the paint", "dc_paint", () =>
                    {
                        b.Narr("Ten minutes later, the paint is mostly on them. They're painting each other's faces like ghosts.");
                        Pal(c, b, Feel.Laugh, c.P("She started it! ...I finished it.", "PAINT WAR! Join us!", "...She started it.", "Hold still, I'm painting a mustache on you."));
                        t.On("dc_paint", () => { t.Her(c.Her, 3); t.Her(c.Pal, 3); t.Extra.Add(() => BondUp(c.Her, c.Pal, 3)); });
                    }, 1.5f);
                    o.Play(b, "dc_1");

                    b.Narr("At the bottom of the SPOOKY box: a life-size rubber skeleton with one arm missing.");
                    var o2 = new Opts();
                    var prank = Roll(G.Sens, 50);
                    o2.Keep("Hide it in the teacher's chair", "dc_prank", () =>
                    {
                        b.Narr(prank ? "Next morning the homeroom teacher screams so loud the class next door comes to look. It becomes a school legend." : "The teacher finds it, sits it in the corner and gives it a desk. \"Your new classmate. He's smarter than you.\"");
                        Her(c, b, Feel.Laugh, c.T("We're going to be in so much trouble... ahaha...", "THIS IS THE BEST PRANK EVER!", "...I didn't see anything.", "We're partners in crime now."));
                        t.On("dc_prank", () => { t.Her(c.Her, 4); t.Rep += prank ? 1 : -1; });
                    });
                    o2.Add("Make him the class mascot", "dc_mascot", () =>
                    {
                        b.Narr("You give him a party hat and a name tag: MR. BONES, HONORARY STUDENT.");
                        Her(c, b, Feel.Laugh, c.T("Mr. Bones... he's part of the class now.", "MR. BONES! He needs a seat!", "...He has better attendance than some people.", "Mr. Bones for class rep."));
                        t.On("dc_mascot", () => t.Her(c.Her, 4, 2));
                    });
                    o2.If(c.Tone == Tone.Energetic || c.Tone == Tone.Teasing || c.Tier >= 2, "Use it to scare her", "dc_scare", () =>
                    {
                        b.Narr("While she's looking the other way, you sit Mr. Bones right behind her. She turns around.");
                        Her(c, b, Feel.Surprised, c.T("KYAAA! ...You...! I'll get you back for that!", "AAAAH! ...Okay. War. This is war.", "...I'm going to remember this.", "Oh, you're dead. You're so dead."));
                        b.Narr("Revenge comes ten minutes later, involving a plastic spider and the back of your collar.");
                        t.On("dc_scare", () => t.Her(c.Her, 4));
                    });
                    o2.Play(b, "dc_2");
                    Finale(c, b, t, "decor");
                    b.Narr("When you turn off the lights, the pumpkins glow in the dark. The room is ready. Mostly.");
                }
            };
        }

        // ---------------- the sunset classroom ----------------

        private static Ep Sunset()
        {
            return new Ep
            {
                Id = "sunset", Title = "Evening in the classroom", Bg = "classroom_sunset",
                Weight = c => (c.Low ? 3f : 0f) + (c.Bad != null ? 1.5f : 0f) + (c.Tier >= 2 ? 0.7f : 0.2f),
                Invite = (c, i) => i.Say(c.Her, c.T("...Hey. Do you have a minute? Not here... The classroom upstairs is empty.", "Hey... can we talk? Somewhere quiet? Upstairs?",
                    "Do you have a minute? I want to talk. Not here.", "Got a minute? I want to talk. Somewhere without an audience.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The empty classroom at sunset. Chairs up on the desks, dust floating in the orange light.");
                    b.Narr("She sits on the windowsill and doesn't say anything for a long while.");
                    Callback(c, b);
                    var o = new Opts();
                    o.Keep("Wait for her to talk", "su_wait", () =>
                    {
                        b.Narr("You lean against the desk next to her and wait. Outside, the baseball club is shouting at a ball.");
                        Her(c, b, Feel.Smile, c.T("...Thank you for not rushing me.", "...You're really patient, huh. Thanks.", "...You're good at silence. Rare skill.", "...You didn't say anything stupid. Impressive."));
                        t.On("su_wait", () => t.Her(c.Her, 2, 4));
                    });
                    o.If(c.Snack != "", "Offer her your " + ItemName(c.Snack), "su_snack", () =>
                    {
                        b.Narr("You hold out the " + ItemName(c.Snack) + ". She takes it, and a little of the tension leaves her shoulders.");
                        Her(c, b, Feel.Smile, c.T("...Thank you. I didn't eat lunch.", "Food! You always know! ...Thanks.", "...Thanks. I needed that.", "...Bribery works on me. Apparently."));
                        t.On("su_snack", () => { t.Use(c.Snack); t.Her(c.Her, 4, 2); });
                    }, 1.5f);
                    var funny = Roll(G.Sens, 45);
                    o.Add("Crack a joke to break the ice [SEN]", "su_joke", () =>
                    {
                        b.Me("So. Is this where you tell me you're secretly a ghost? Because I'm not ready for that.");
                        Her(c, b, Feel.Laugh, funny ? c.T("...Pfft. Idiot. ...Thanks.", "Ahaha! I'm not a ghost! ...Thanks for that.", "...That was terrible. It helped.", "...Okay, that got a smile. Barely.") : c.T("...That's not funny. ...Okay, it's a little funny.", "...Ha. Ha. ...Okay, I smiled.", "...No.", "...Really? That's your opener?"));
                        t.On("su_joke", () => t.Her(c.Her, funny ? 4 : 2, 2));
                    });
                    o.Play(b, "su_1");
                    Finale(c, b, t, "sunset");
                    b.Narr("The sun slips behind the buildings. The room goes blue, then grey, and you walk out together.");
                }
            };
        }
    }
}
