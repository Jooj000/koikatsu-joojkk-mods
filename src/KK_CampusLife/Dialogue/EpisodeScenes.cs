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
            { "sunset", "that evening in the lecture hall" },
        };

        /// <summary>What she remembers, right at the start: your last outing, something you did, or how little you know each other.</summary>
        private static void Callback(Ctx c, SceneBuilder b)
        {
            string mem;
            if (c.Bad != null)
                Her(c, b, Feel.Pout, c.T("I'm still a little hurt about " + What(c.Bad) + "... But I didn't want to come alone." + " || " + What(c.Bad) + " still hurts a little... but I didn't want to be alone tonight. || I haven't forgotten " + What(c.Bad) + "... but I wanted to come with you." + " || " + What(c.Bad) + " still stings... I came anyway.", "I'm still kinda mad about " + What(c.Bad) + ", y'know! ...But I wanted to come. || Still a bit mad about " + What(c.Bad) + "! ...But I came!" + " || " + What(c.Bad) + "? Not over it! ...I still wanted to come, though! || I'm still grumpy about " + What(c.Bad) + "! But here I am!",
                    "Don't think I forgot " + What(c.Bad) + ". I just didn't want to waste the evening." + " || " + What(c.Bad) + " isn't forgotten. The evening shouldn't be wasted, though. || I remember " + What(c.Bad) + ". I came anyway." + " || " + What(c.Bad) + ". Not forgotten. Not tonight's topic.", "I haven't forgiven you for " + What(c.Bad) + " yet. Consider this your chance." + " || " + What(c.Bad) + "? Unforgiven. Tonight's your shot. || Still holding " + What(c.Bad) + " against you. Redeem yourself." + " || " + What(c.Bad) + " is on your record. Fix it tonight."));
            else if (!string.IsNullOrEmpty(c.LastEp) && Memory.TryGetValue(c.LastEp, out mem))
                Her(c, b, Feel.Smile, c.T("I still think about " + mem + " sometimes... I had so much fun. || I still remember " + mem + "... it was so fun." + " || " + mem + " was wonderful... I think about it. || I loved " + mem + "... I hope tonight is like that.", "Remember " + mem + "?! This has to be even better!" + " || " + mem + " was great! Tonight has to beat it! || Remember " + mem + "?! Let's top it! || After " + mem + ", tonight better be epic!",
                    "After " + mem + ", I figured you'd be reliable. Don't prove me wrong." + " || " + mem + " made me think you're reliable. Stay that way. || After " + mem + ", I expect good things." + " || " + mem + " set the bar. Meet it.", "After " + mem + "? You owe me another good one." + " || " + mem + " was good. Do better. || You owe me a sequel to " + mem + ". || After " + mem + "? Raise the bar."));
            else if (c.Good != null)
                Her(c, b, Feel.Shy, c.T("I never really thanked you for " + What(c.Good) + "... This is my way of saying it. || I never thanked you for " + What(c.Good) + "... so, this is it. || This is my thank-you for " + What(c.Good) + "... || For " + What(c.Good) + "... thank you. That's why tonight.", "This is a thank-you for " + What(c.Good) + "! So have fun, okay?! || Thank-you for " + What(c.Good) + "! Have fun! || This is for " + What(c.Good) + "! Enjoy! || Payback for " + What(c.Good) + "! The good kind!",
                    "Consider this payback for " + What(c.Good) + ". I don't like owing people. || Repaying " + What(c.Good) + ". I hate debts. || For " + What(c.Good) + ". Now we're even." + " || " + What(c.Good) + ". Consider it settled.", "Think of this as a reward for " + What(c.Good) + ". Don't let it go to your head. || A reward for " + What(c.Good) + ". Don't gloat. || Your prize for " + What(c.Good) + ". Stay humble. || For " + What(c.Good) + ". Don't get cocky."));
            else if (c.Girlfriend && c.Pal == null)
                Her(c, b, Feel.Shy, c.T("Just the two of us... It feels like a real date. ...It is a real date, isn't it? || Just us... it's a real date, isn't it? || The two of us alone... this is a date, right? || A date... a real one... right?", "Date! This is officially a date! I even fixed my hair, did you notice?! || Official date! I did my hair! See?! || Date time! Notice my hair?! || It's a DATE! Look at me!",
                    "A date. With my boyfriend. ...Don't grin like that. || A date. With my boyfriend. Stop grinning. || My boyfriend and me. A date. Don't smirk. || A date. Yes. Don't look so smug.", "My boyfriend finally takes me out. Took you long enough~ || Finally, my boyfriend takes me out~ || About time you took me out~ || My boyfriend, finally stepping up~"));
            else if (c.Tier == 0)
                Her(c, b, Feel.Normal, c.T("We don't talk much, do we... I thought this could be a start. || We don't talk much... I hoped this could change that. || We're not close... maybe tonight helps. || I thought this might help us get to know each other...", "We barely know each other! Perfect, that makes it an adventure! || We barely know each other! Adventure! || Strangers! Fun! || Getting to know you! Exciting!",
                    "We're not exactly close. That's fine. I needed someone and you were there. || Not close. Fine. You were available. || We hardly know each other. That's okay. || You were there. That's enough.", "We hardly know each other. Let's see if you're interesting. || Let's see if you're interesting. || Hardly know you. Impress me. || Prove you're worth knowing."));
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
            var ok = Consent.Yes(c.Her, "ep:close", CloseChance(c));   // context, not a roll
            b.Narr("On the way back, the street is quiet. Your hands keep almost touching.");
            b.Me(c.Girlfriend ? Lines.Pick("Come here. Give me your hand.", "Let's walk like this for a while.") : Lines.Pick("Can I... hold your hand? Just until the station.", "Your hands look cold. Here."));
            if (!ok)
            {
                Her(c, b, Feel.Surprised, c.T("Ah... s-sorry... not yet. I'm not ready for that... || S-sorry... not yet... I'm not ready... || Ah... maybe later... I'm too nervous... || I-I can't yet... sorry...", "Whoa! Um! Hands! That's, uh, a lot! Maybe next time?! || Whoa! Hands! Too much! Next time?! || Eep! Hands?! Maybe later?! || Uh! Not yet! Next time!", "...No. Not yet. || ...Not yet. || ...No. || ...Wait.", "Holding hands already? Earn it first. || Earn it first. || Too soon. Work for it. || Hands? Prove yourself."));
                Her(c, b, Feel.Shy, c.T("But... I'm happy you asked. Really. || But I'm glad you asked. Really. || Still... thank you for asking. || But... it made me happy.", "...But you can walk really close. That's allowed. || ...You can walk super close though! || ...Close walking is allowed! || ...Shoulder to shoulder is okay!", "...You can walk next to me, though. || ...Walk beside me. That's fine. || ...Beside me is fine. || ...Next to me. Okay.", "...But I'll think about it. Keep trying~ || ...I'll consider it. Keep trying~ || ...Maybe next time~ || ...Keep at it~"));
                t.On("fin_close", () => { t.Ending = "close_no"; t.Her(c.Her, 1, 2); });
                return;
            }
            b.Narr(c.Girlfriend ? "She laces her fingers through yours without even looking, like it's the most natural thing in the world." : "For a second she freezes. Then her fingers slowly close around yours.");
            Her(c, b, Feel.Shy, c.Girlfriend
                ? c.T("Mm... your hand is so warm. I could walk like this all night. || Your hand is so warm... let's walk all night. || So warm... I don't want to let go. || Like this... forever, please.", "Fingers laced! That's the official couple grip, you know! || Laced fingers! Couple grip! || Official couple hold! || Fingers together! Yay!", "...Don't let go before the station. || ...Hold until the station. || ...Don't let go. || ...Keep holding.", "If you let go first, you lose. Got it? || Let go first, you lose. || First to let go loses. || Don't you dare let go.")
                : c.T("My face is so hot right now... please don't look at me... || My face is burning... don't look... || I'm so red... please don't look... || D-don't look at me right now...", "Okay. This is fine. This is TOTALLY fine. ...It's really nice, actually. || Totally fine! ...It's really nice. || This is okay! Very okay! || I'm calm! ...It's nice!", "...Your hand is warmer than I expected. || ...Warmer than expected. || ...Your hand. Warm. || ...Hm. Nice.", "Oh? Taking the initiative today? ...I'll allow it. || Taking the lead? ...Allowed. || Bold today. ...Okay. || Initiative? ...I approve."));
            var o = new Opts();
            o.Keep("Just walk with her", "cl_walk", () =>
            {
                b.Narr("You don't talk much after that. You don't need to. The streetlights come on one by one.");
                Her(c, b, Feel.Smile, c.T("...I'll remember tonight. All of it. || ...I'll remember tonight. Every bit. || ...Tonight was precious. || ...I won't forget this.", "Today was the best. Like, the BEST best. || Best day! The BEST! || Today was amazing! || Best ever!", "...That was nice. Really nice. || ...That was really nice. || ...Good evening. Very. || ...I enjoyed it.", "Not bad. Not bad at all. || Not bad. At all. || Pretty good. || You did well."));
                t.On("cl_walk", () => t.Her(c.Her, 6, 4, c.Girlfriend ? 2 : 1, 3));
            });
            o.If(c.Girlfriend || c.Tier >= 3 || c.Mood == Mood.Flirty, "Kiss her", "cl_kiss", () =>
            {
                b.Narr("You stop under a streetlight. She looks up at you, and doesn't look away.");
                Her(c, b, Feel.Shy, c.T("...Here? Someone might... ...okay. || ...Here? Someone might see... okay. || ...Right here...? ...Alright. || ...Here? ...Okay...", "W-wait, right here? ...Okay. Okay! I'm ready! Eyes closed! || Right here?! ...Okay! Eyes closed! || Now?! ...Okay! Ready! || Wait! ...Okay! Go!", "...Don't make me wait. || ...Don't keep me waiting. || ...Hurry. || ...Well?", "You're thinking about it, aren't you? ...Then do it. || Thinking about it? ...Do it. || You want to. ...Go on. || Stop thinking. Do it."));
                b.Narr("Her lips are soft, and she's shaking a little. When you pull apart, she hides her face against your shoulder.");
                Her(c, b, Feel.Love, c.T("...My heart is beating so fast I can't hear anything else. || ...My heart is so loud... || ...I can't hear anything but my heart. || ...My heart's racing...", "That was... wow. Again. No wait, not in public! ...Later! || Wow! Again! Not here! Later! || That was... whoa! Later, again! || Wow! More later!", "...Again. Later. Somewhere quieter. || ...Again. Later. Quietly. || ...Later. Somewhere private. || ...More. Later.", "...Hmph. You're better at that than I expected. || ...Better than expected. || ...You're good at that. || ...Hmph. Impressive."));
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
            var ok = Consent.Yes(c.Her, "ep:confess", chance) && Consent.Chance(c.Her, Ask.Confess, out why) >= 0.5f;
            b.Narr("You stop walking. She notices and turns around.");
            b.Me(Lines.Pick("I've been wanting to tell you for a while. I like you. Not as a friend. I really like you.",
                "Today was one of the best days I've had. Because it was with you. ...I like you. A lot.",
                "I don't want to just be your friend. I like you. Will you go out with me?"));
            Her(c, b, Feel.Surprised, c.T("Eh...? Y-you mean... me...? || Eh? M-me...? || Me...? Really...? || W-wait... you mean me?", "W-WAIT. Wait wait wait. Say that again?! || WAIT. Say it again?! || What?! Again?! || Hold on! Repeat that!", "...Are you serious? || ...Serious? || ...You mean it? || ...Really?", "...Huh. You actually said it. || ...You said it. || ...Wow. You actually did. || ...Huh. Finally."));
            if (ok)
            {
                Her(c, b, Feel.Shy, c.T("I... I've liked you for so long. I thought it was only me... || I've liked you so long... I thought it was just me... || I liked you all along... I didn't think... || I've loved you for ages... I'm so happy...", "I like you too! I've liked you forever, you dummy! Why did you take so long?! || I like you too! Forever! Why so slow?! || Me too! Since forever! Dummy! || Yes! I've liked you so long!",
                    "...Fine. Yes. I like you too. I have for a while. || ...Yes. I like you too. For a while now. || ...Fine. Same. Long time. || ...I do too. For some time.", "Took you long enough. ...Yes. Obviously yes. || Slow. ...Yes. Obviously. || Finally. ...Yes, of course. || Took ages. ...Yes."));
                b.Narr("She takes your hand, and this time neither of you lets go.");
                Her(c, b, Feel.Love, c.T("So... from today, I'm your girlfriend. Please take care of me. || From today, I'm your girlfriend... please take care of me. || So... we're together now. Be good to me. || I'm your girlfriend now... please be kind.", "We're dating! We're DATING! I have to tell everyone! ...Okay, maybe not everyone. || Dating! We're DATING! Tell everyone! ...Some people! || We're a couple! Yay! || Girlfriend mode! On!",
                    "From today, you're mine. Don't forget it. || From today, you're mine. Remember. || You're mine now. Don't forget. || Mine. From today.", "From today you're my boyfriend. No take-backs~ || Boyfriend now. No refunds~ || You're my boyfriend. No returns~ || Mine. No take-backs~"));
                t.On("fin_confess", () =>
                {
                    t.Ending = "confess";
                    t.Her(c.Her, 10, 10, 3, 4);
                    t.Extra.Add(() => BecomeGirlfriend(c.Her));
                });
            }
            else
            {
                Her(c, b, Feel.Worried, c.T("I... I'm really happy. But I'm not ready yet... Can you wait for me a little? || I'm so happy... but not ready yet. Wait for me? || I'm glad... but can you give me time? || Thank you... but I need a little longer.", "Ah... um... I really like hanging out with you! But dating is... can I think about it? || I love hanging out! But dating... think about it? || Ah! You're great! Dating though... let me think? || I like you! But... can I think?",
                    "...Not yet. I need to be sure. Don't take it the wrong way. || ...Not yet. I need certainty. || ...Not yet. Don't misunderstand. || ...I need to be sure first.", "...You're sweet. But not yet. Ask me again when you've earned it. || Sweet. Not yet. Earn it. || Cute. Ask again later. || Not yet. Prove it first."));
                Her(c, b, Feel.Smile, c.T("Please don't stop talking to me because of this, okay? || Please keep talking to me, okay? || Don't avoid me because of this... || We're still okay, right?", "We're still friends! The best of friends! Okay?! || Still best friends! Okay?! || Friends still! Promise! || We're good! Right?!", "...We're fine. Really. || ...We're fine. || ...Nothing changes. || ...It's okay.", "Don't make that face. I didn't say never. || Don't pout. I didn't say never. || Chin up. Not never. || Not a no forever."));
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
            var ok = Consent.Yes(c.Her, "ep:private", PrivateChance(c));
            b.Narr("It's getting late, and neither of you seems to want the day to end.");
            b.Me(c.HCount > 0 ? Lines.Pick("I don't want to say goodbye yet. Want to go somewhere just the two of us? Like last time.", "Hey... nobody's around right now. Come with me?")
                              : Lines.Pick("I don't want to say goodbye yet. Want to go somewhere just the two of us?", "Let's go somewhere quieter. Just you and me."));
            if (!ok)
            {
                Her(c, b, Feel.Shy, c.T("S-somewhere just the two of us...? I... not today. My heart can't take it. || Just us alone...? Not today... my heart... || Somewhere private...? I can't today... || Alone...? Not tonight... I'm too nervous.", "Ehh?! Just us two, now?! N-no way, I'm way too nervous! ...Another day! || Just us?! Too nervous! Another day! || Now?! No way! Later! || Eek! Not today!",
                    "...I know what you mean. Not today. || ...I understand. Not today. || ...Not today. || ...I know. Not yet.", "Ohh? Bold. ...Not today, though. Make me want it more. || Bold. Not today. Make me want it. || Ooh. Not now. Tempt me more. || Not today. Try harder."));
                t.On("fin_h", () => { t.Ending = "h_no"; t.Her(c.Her, 1, 0, 0, 3); });
                return;
            }
            Her(c, b, Feel.Shy, c.T("...Okay. I was hoping you'd ask. I know a place nobody goes this late... || ...I hoped you'd ask. I know a quiet place... || ...Okay. Follow me, somewhere nobody goes... || ...Yes. I know a hidden spot...", "Hehe... I was totally thinking the same thing. Follow me, quick, before someone sees! || Same idea! Quick, follow me! || Hehe! Hurry! This way! || Yes! Quick, before anyone sees!",
                "...Fine. But we go now, before I change my mind. || ...Now. Before I change my mind. || ...Fine. Quickly. || ...Let's go. Now.", "Finally. I was starting to think I'd have to drag you there myself~ || Finally. I almost dragged you~ || About time~ This way. || I was about to kidnap you~"));
            b.Narr(c.HCount > 0 ? "She doesn't let go of your hand all the way back. You both know where you're going."
                                : "She takes your hand and pulls you along, glancing back at you with red cheeks.");
            Her(c, b, Feel.Love, c.T("...Be gentle with me, okay? || ...Gently, okay? || ...Please be gentle... || ...Be kind to me...", "Okay, nobody's here! ...Ehehe. I'm all yours. || Nobody here! ...I'm yours! || Alone! ...Ehehe. || All clear! ...Hehe.", "...Don't keep me waiting. || ...Don't make me wait. || ...Come here. || ...Well?", "Now then... let's see what you've got. || Let's see what you can do. || Show me. || Now... impress me."));
            t.IntoH = true;
            t.On("fin_h", () => { t.Ending = "h"; t.Her(c.Her, 3, 2, c.Girlfriend ? 2 : 0, 10); });
        }

        private static void Sorry(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("About " + What(c.Bad).Replace("me", "you").Replace("my", "your") + "... I'm sorry. I mean it.");
            var ok = c.Trust >= 30 || c.Tier >= 2;
            Her(c, b, ok ? Feel.Smile : Feel.Serious, ok
                ? c.T("...Thank you for saying it. I was waiting for that. It's okay now. || ...Thank you. I was waiting. It's okay now. || ...I forgive you. Thank you for saying it. || ...That's what I needed. It's okay.", "...Okay! Apology accepted! Clean slate! || Accepted! Clean slate! || Okay! Fresh start! || Forgiven! All good!", "...Fine. Thank you. Let's leave it here. || ...Thank you. Let's end it here. || ...Fine. Done. || ...Accepted. Moving on.", "Took you long enough. ...Okay. Forgiven. || Slow. ...Forgiven. || Finally. ...Okay. || About time. ...Fine.")
                : c.T("...I believe you. I just need a little time, okay? || ...I believe you. Give me time. || ...I want to believe you. Wait a bit. || ...Okay. I just need time.", "Hmm... okay. But you're on probation! || Okay! But probation! || Hmm! You're on thin ice! || Fine! Probation!", "Words are easy. We'll see. || Easy words. We'll see. || Talk is cheap. || Prove it.", "I'll think about it. Keep trying. || Keep trying. || I'll consider it. || Work for it."));
            t.On("fin_sorry", () => { t.Ending = "sorry"; t.Her(c.Her, ok ? 5 : 2, ok ? 10 : 5); });
        }

        private static void Thanks(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("Can I ask you something? Why did " + What(c.Good).Replace("me", "you") + " matter so much to you?");
            Her(c, b, Feel.Shy, c.Good.Kind == Social.Kinds.Defended
                ? c.T("Because nobody ever stood up for me before. I didn't know what to do with my face. || Nobody ever defended me before... I didn't know how to react. || No one stood up for me until you... || You defended me... no one ever had.", "Because that was SO cool! Nobody does that! ...It made me really happy. || That was SO cool! Nobody does that! || You were a hero! It made me so happy! || Nobody's ever done that! So cool!", "Because people usually just watch. You didn't. || Most people watch. You acted. || Others stand by. You didn't. || You stepped in. Rare.", "Because it was unexpected. I like being surprised by people. || Unexpected. I like surprises. || You surprised me. Nicely. || Didn't see that coming. I liked it.")
                : c.Good.Kind == Social.Kinds.KeptPromise
                ? c.T("Because people forget things they promise me. You didn't. || People forget their promises to me. You didn't. || You remembered... most don't. || You kept your promise... thank you.", "Because you actually did it! People usually forget! || You actually did it! People forget! || You remembered! Wow! || Most people flake! Not you!", "Because you did what you said. That's rare. || You kept your word. Rare. || Promise kept. Uncommon. || You followed through.", "Because I half expected you to forget. You didn't. || I expected you to forget. You didn't. || Thought you'd forget. Wrong. || You didn't forget. Impressive.")
                : c.T("Because I was really struggling, and you just... helped. Without me asking twice. || I was struggling, and you helped... without me asking twice. || You helped right away... I was so lost. || You came when I needed it...", "Because I was in a total panic and you showed up like a hero! || I was panicking and you swooped in! || Total panic! You saved me! || Hero entrance! Thanks!", "Because I hate asking for help. You made it easy. || I hate asking. You made it easy. || Asking is hard. You helped anyway. || You spared me asking.", "Because you didn't make a big deal of it. I liked that. || No fuss. I liked that. || You didn't make it a big deal. Nice. || Quiet help. I appreciated it."));
            Her(c, b, Feel.Smile, c.T("I'm glad we're friends. || I'm glad we're friends. || I'm happy we're friends... || You're a dear friend.", "You're a really good friend, you know that? || You're a great friend! || Best friend material! || You're awesome, friend!", "You're... alright. Better than alright. || You're... more than alright. || You're good. || ...Good friend.", "You're more reliable than you look. || More reliable than you look. || Surprisingly dependable. || Not bad, friend."));
            t.On("fin_thanks", () => { t.Ending = "thanks"; t.Her(c.Her, 5, 7, 1); });
        }

        private static readonly string[] HowWeMet =
        {
            "We sat next to each other on the first day and both forgot our pencil cases.",
            "We got lost on the same field trip and found our way back together. Mostly by accident.",
            "We fought over the last melon bread at the cafeteria. Then we split it.",
            "We were partners for cleaning duty and talked so much we forgot to clean.",
            "She lent me an umbrella in the rain and then we both got wet anyway.",
        };

        private static void Pals(Ctx c, SceneBuilder b, Tally t)
        {
            var story = HowWeMet[Random.Range(0, HowWeMet.Length)];
            Her(c, b, Feel.Laugh, c.T("Oh... that's a funny story. " + story + " || " + "Ah, a funny story... " + story + " || " + "Hehe... " + story + " || " + "Oh, this is silly... " + story, "Okay, okay, so! " + story + " || " + "Okay, listen! " + story + " || " + "So get this! " + story + " || " + "Story time! " + story, story + " That's all." + " || " + story + " That's it." + " || " + story + " End." + " || " + story + " Nothing more.", story + " Classic, right?" + " || " + story + " Classic." + " || " + story + " Right? Hilarious." + " || " + story + " Peak comedy."));
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
            Her(c, b, Feel.Smile, c.T("Next time... I'd like that. I'll hold you to it. || Next time... yes. I'll remember. || Next time... I'd love that. || I'll look forward to next time...", "YES! It's a plan! Pinky promise! || YES! Pinky promise! || Plan! Next time! || Deal! Promise!", "...Fine. Next time. Don't forget. || ...Next time. Remember. || ...Okay. Next time. || ...Don't forget.", "Already planning the next one? Okay. Deal. || Planning ahead? Deal. || Next one? Okay. || Sure. Next time."));
            t.On("fin_plan", () => { t.Ending = "plan"; t.Her(c.Her, 3, 5); });
        }

        private static readonly string[] Jokes =
        {
            "You tell her the one about the penguin who walks into a convenience store.",
            "You do your best impression of the vice dean's morning speech.",
            "You explain, very seriously, your theory that pigeons are government spies.",
            "You tell her about the time you got your head stuck in the campus gate.",
        };

        private static void Laugh(Ctx c, SceneBuilder b, Tally t)
        {
            var land = Roll(Math.Max(G.Int, G.Sens), 50);
            b.Narr(Jokes[Random.Range(0, Jokes.Length)]);
            if (land)
            {
                b.Narr("She tries to hold it in. She fails completely.");
                Her(c, b, Feel.Laugh, c.T("Ahaha... st-stop, my stomach hurts...! || Ahaha... stop, it hurts...! || Hehe... my stomach...! || Ahaha... too funny...!", "AHAHAHA! Again! Tell it again! || AHAHA! Again! || HAHA! One more time! || Again! Again!", "...Pfft. That was stupid. ...Pfft. || ...Pfft. Stupid. ...Pfft. || ...Heh. Dumb. ...Heh. || ...Ridiculous. ...Pfft.", "Okay, okay, you win. That was actually funny. || You win. Funny. || Okay, that was good. || Fine. Hilarious."));
            }
            else
            {
                b.Narr("Silence. A crow caws somewhere. The joke dies a slow, painful death.");
                Her(c, b, Feel.Laugh, c.T("...That was so bad it's actually funny. Ahaha... || ...So bad it's funny. Hehe... || ...Terrible... ahaha... || ...That's awful... hehe.", "That was TERRIBLE! Ahahaha! I love it! || TERRIBLE! Love it! || So bad! Hahaha! || Awful! Perfect!", "...I'm laughing at you, not with you. ...Okay, a little with you. || ...Laughing at you. ...Partly with you. || ...At you. Mostly. || ...Heh. Despite myself.", "That's the worst joke I've ever heard. I'm saving it forever. || Worst joke ever. Saving it. || Awful. I'll treasure it. || So bad. Keeping it forever."));
            }
            Pal(c, b, Feel.Laugh, c.P("Ahaha... you two are funny together.", "HAHA! I'm crying!", "...I didn't laugh. That wasn't a laugh.", "I'm telling everyone about this."));
            t.On("fin_laugh", () => { t.Ending = "laugh"; t.Her(c.Her, land ? 6 : 4, 2); if (c.Pal != null) t.Her(c.Pal, 3); });
        }

        // ---------------- the heart-to-heart ----------------

        private static string Dream(Ctx c)
        {
            if (c.Cooking) return c.T("I want to open a little cafe one day. With a cat sleeping in the window. || A little cafe someday... with a sleepy cat in the window. || I'd love a tiny cafe... and a cat. || A cozy cafe... that's my dream.", "I want my own restaurant! A huge one! With a neon sign! || My own restaurant! Huge! Neon! || A giant restaurant! Lights everywhere! || Restaurant! The biggest one!", "I'd like to be a chef. A real one, with a real kitchen. || A chef. A real one. || Professional chef. That's the goal. || I want to cook professionally.", "A cafe of my own, where I decide the menu and everybody loves it. || My cafe. My menu. Everyone loves it. || A cafe where I'm the boss. || My own place. Adored by all.");
            if (c.Sporty) return c.T("I want to run at the nationals. Just once. Even if I come last. || Nationals... just once. Even last place. || I want to run at nationals... just once. || Nationals... it's my dream.", "Nationals! I'm going to the nationals and I'm going to WIN! || Nationals! And WINNING! || I'm winning nationals! || Nationals! Gold!", "I want to compete at a national level. I'm not joking. || National level. Seriously. || Nationals. I mean it. || I'm aiming for nationals.", "Nationals. Then a medal. Then my face on a cereal box. || Nationals, medal, cereal box. || Medal, then fame. || Nationals. Then my face everywhere.");
            if (c.Bookish) return c.T("I want to study abroad... somewhere far, where nobody knows me. || Study abroad... somewhere nobody knows me. || I want to go far away to study... || Somewhere distant... a fresh start.", "I want to go to a university in another country! Like, really far! || University abroad! Far away! || Another country! Big adventure! || Overseas university!", "I'm aiming for a top university. Medicine, maybe. || A top university. Medicine, perhaps. || Top campus. Medicine. || Medical campus. Top tier.", "A top university, a fancy degree, and everyone who doubted me can watch. || Top campus, fancy degree, doubters watching. || A big degree. Everyone eats their words. || Prove everyone wrong. Top university.");
            if (c.Animals) return c.T("I want to be a vet. I can't stand seeing animals hurt. || A vet... I hate seeing animals hurt. || I want to help animals... a vet. || Being a vet... that's my dream.", "Vet! I'm gonna be a vet and pet every animal in the world! || Vet! Pet all the animals! || A vet! Puppies! Kittens! || Vet! Every animal!", "Veterinarian. Animals are more honest than people. || Vet. Animals are honest. || Veterinarian. Simpler than people. || Animals. Not people. Vet.", "A vet. Animals like me more than people do. Mostly. || Vet. Animals like me more. || A vet. Animals adore me. || Vet. Better company than humans.");
            if (c.Fashion) return c.T("I'd like to design clothes. Even just one dress that someone loves. || Design clothes... one dress someone loves. || I'd like to make clothes people love... || Fashion... even one dress.", "Fashion designer! My own brand! You'll wear my stuff! || My own brand! You'll wear it! || Fashion empire! || Designer! My label!", "Design. I already have three notebooks of sketches. || Design. Three notebooks already. || Fashion design. I sketch daily. || Design. Plenty of sketches.", "My own fashion brand. Everyone on campus will wear my stuff. || My brand. The whole university wearing it. || Everyone in my designs. || My label. Everywhere.");
            return c.T("I don't really know yet... I just want to find something that's mine. || I don't know yet... something that's mine. || I'm still searching... || Not sure yet... something meaningful.", "I don't know yet! Something BIG! I'll figure it out! || Don't know! Something BIG! || No idea! Huge though! || Something epic! Soon!", "I don't know. That's what bothers me. || I don't know. That bothers me. || Undecided. Irritating. || Unknown. Frustrating.", "Haven't decided. I want something nobody expects from me. || Undecided. Something unexpected. || Something no one expects. || Surprise everyone. That's the plan.");
        }

        private static void Heart(Ctx c, SceneBuilder b, Tally t)
        {
            b.Me("Hey. How are you really doing?");
            Her(c, b, Feel.Surprised, c.T("Eh? Me...? ...Nobody ever asks that. || Me...? Nobody asks that... || Eh? About me...? || ...Oh. No one ever asks.", "Huh? I'm great! I'm always great! ...Why are you looking at me like that? || Me? Great! Always! ...Why that look? || I'm fine! Totally! ...Why? || Great! ...Why are you staring?", "...Why do you ask? || ...Why ask? || ...Why? || ...What prompted that?", "Whoa, getting serious all of a sudden? || Serious all of a sudden? || Deep talk now? || Whoa. Serious mode?"));
            var lonely = c.Mood == Mood.Lonely || c.Gs.Attention < 35;
            if (c.Mood == Mood.Stressed || c.Mood == Mood.Upset && c.Bookish)
            {
                Her(c, b, Feel.Worried, c.T("Honestly... I'm tired. Everyone expects me to do well, and I'm scared of letting them down. || I'm tired... everyone expects so much. I'm scared. || Honestly, I'm worn out... I fear disappointing them. || Everyone's counting on me... I'm exhausted.", "I'm... kind of exhausted? Everybody thinks I'm fine because I'm loud. || I'm exhausted? People think loud means fine. || Kinda wiped out. Loud isn't always happy. || Tired! Everyone thinks I'm fine!",
                    "There's a lot of pressure. Grades, club, home. I handle it. Usually. || Pressure. Grades, club, home. Usually manageable. || Lots of pressure. I cope. Mostly. || Heavy load. I handle it.", "I'm a bit stressed. Everyone thinks I'm carefree. It's an act, sometimes. || Stressed. The carefree act is an act. || Everyone thinks I'm carefree. Not always. || Stress. Hidden behind jokes."));
                Her(c, b, Feel.Sad, c.T("Sometimes I lie in bed and just... count everything I haven't done yet. || Some nights I count everything I haven't done... || I lie awake listing my unfinished things... || At night I think of everything left undone...", "Sometimes I just want to scream into a pillow, you know? || Sometimes I wanna scream into a pillow! || Pillow screaming! It's needed! || I just wanna yell sometimes!", "Sometimes I wonder who I'm doing it all for. || Who is it all for? I wonder. || Sometimes I question why. || For whom, I wonder.", "Sometimes I want to just quit everything and see what happens. || Sometimes I want to quit everything. Just to see. || Drop everything. Tempting. || Quit it all. Sometimes."));
            }
            else if (lonely)
            {
                var friend = c.Best == null ? "my friends" : GirlInfo.FirstName(c.Best);
                Her(c, b, Feel.Sad, c.T("Lately I feel a bit... invisible. " + friend + " is busy, and I don't want to bother anyone. || I feel invisible lately... " + friend + " is busy..." + " || " + friend + " has no time lately... I'm alone. || Lately I'm alone... " + friend + "'s busy...", "Lately everyone's kinda busy... " + friend + " too. It's fine! Mostly! || Everyone's busy! " + friend + " too! Fine! Mostly!" + " || " + friend + "'s busy! It's okay! Kinda! || Busy friends! Even " + friend + "! Ehh!",
                    "I've been alone a lot lately. " + friend + " has other people now. That's normal. || Alone a lot. " + friend + " has others. Normal." + " || " + friend + " has new friends. That's life. || Alone often. " + friend + "'s elsewhere.", "Lately nobody seems to notice me. Not even " + friend + ". Weird, right? || Nobody notices me. Not even " + friend + ". Strange." + " || " + friend + " forgot me too. Odd. || Invisible. Even to " + friend + "."));
            }
            else if (c.Mood == Mood.Upset)
            {
                Her(c, b, Feel.Pout, c.T("Today was just... a bad day. Everything went wrong, one thing after another. || A bad day... one thing after another. || Everything went wrong today... || Nothing went right today...", "Today was the WORST. I tripped, I forgot my lunch, and the vending machine ate my coins! || WORST day! Tripped, no lunch, coins eaten! || Disaster day! Everything broke! || Awful! Everything went wrong!",
                    "Bad day. I don't want to list it. || Bad day. Not listing it. || Bad. Leave it. || Rough day. Skip details.", "Today was awful. The universe has it out for me. || Awful. The universe hates me. || Cursed day. || Today's out to get me."));
            }
            else if (c.Tier >= 3 && c.Trust >= 60)
            {
                Her(c, b, Feel.Shy, c.T("Can I tell you something I haven't told anyone? || Can I tell you a secret? || May I share something private? || Can I trust you with something?", "Okay, secret time! You can't tell anyone! || Secret time! Don't tell! || Shh! A secret! || Secret! Promise not to tell!", "I'll tell you something. Only you. || A secret. Only for you. || Something private. Just you. || I'll share something.", "Fine. Since you asked nicely. A secret. || Fine. A secret. || You asked nicely. Here. || A secret. Lucky you."));
                Her(c, b, Feel.Smile, Dream(c));
                Her(c, b, Feel.Worried, c.T("...Is that silly? || ...Silly, right? || ...Is it weird? || ...Do you think it's silly?", "...Don't laugh! Okay you can laugh a little. || ...Don't laugh! ...A little's okay! || ...No laughing! ...Okay, some! || ...Hehe, go ahead, laugh!", "...Say something. || ...Well? || ...Speak. || ...Say something.", "...Well? Go on, tell me it's crazy. || ...Tell me it's crazy. || ...Go on. Judge. || ...Well? Crazy?"));
            }
            else
            {
                Her(c, b, Feel.Worried, c.T("Sometimes I worry I'm boring. That people only put up with me. || I worry I'm boring... that people just tolerate me. || I'm afraid I'm dull... || Maybe people only put up with me...", "Sometimes I'm scared I'm too much. Too loud, you know? || Am I too much? Too loud? || I worry I'm too loud! || Sometimes I'm scared I'm exhausting!",
                    "Sometimes I think people find me cold. I'm not. I just don't know what to say. || People think I'm cold. I'm not. I'm just unsure. || I'm not cold. Just quiet. || People misread me as cold.", "Sometimes I wonder if anyone would notice if I just stopped joking. || If I stopped joking, would anyone notice? || What if I stopped joking? || Would anyone care without the jokes?"));
            }

            var o = new Opts();
            o.Keep("Just listen", "ht_listen", () =>
            {
                b.Narr("You don't say anything clever. You just listen, all the way to the end.");
                Her(c, b, Feel.Smile, c.T("...Thank you. That helped more than you think. || ...Thank you. That helped a lot. || ...You helped more than you know. || ...Thank you. Really.", "...Huh. I feel lighter. Thanks for not interrupting! || ...Lighter! Thanks for listening! || ...Better! Thanks! || ...Whew! Thanks!", "...Thanks for not trying to fix it. || ...Thanks for not fixing it. || ...You just listened. Thanks. || ...No lecture. Thanks.", "...You're a good listener. Who knew. || ...Good listener. Who knew. || ...Surprising. You listen. || ...You're good at this."));
                t.On("ht_listen", () => t.Her(c.Her, 4, 8, 1));
            });
            o.If(c.Gs.Talks >= 4 || c.Tier >= 2, "Tell her about your own worries", "ht_share", () =>
            {
                b.Me("You know... I feel like that too, sometimes. More than I let on.");
                b.Narr("You tell her things you don't usually say out loud. She listens, very quietly.");
                Her(c, b, Feel.Smile, c.T("...So it's not just me. That's... really comforting. || ...You too? That makes me feel better. || ...I'm not alone in this... thank you. || ...So you feel it too. That helps.", "Whoa, really? You too? ...We should talk more, you know! || You too?! We should talk more! || Really?! Same! Let's talk more! || No way! Twins!", "...Thanks for telling me. That's fair. Now we're even. || ...Thanks for sharing. Even now. || ...Fair. We're even. || ...Thank you. That's balanced.", "...Huh. You're more interesting than I thought. || ...More interesting than I thought. || ...Huh. You have depth. || ...Didn't expect that. Interesting."));
                t.On("ht_share", () => t.Her(c.Her, 5, 10, 2));
            }, 2f);
            var smart = G.Int >= G.Sens;
            var good = Roll(Math.Max(G.Int, G.Sens), 55);
            o.Add(smart ? "Give her some honest advice [INT]" : "Cheer her up [SEN]", "ht_help", () =>
            {
                b.Me(smart ? "Here's what I think. One thing at a time. Pick the one that matters most and let the rest wait." : "Hey. You're great, okay? I'm not just saying that. I'd tell you if you weren't.");
                Her(c, b, good ? Feel.Smile : Feel.Laugh, good
                    ? c.T("...You're right. I'll try. Thank you. || ...You're right. I'll try. || ...Okay. I'll do my best. Thank you. || ...That makes sense. I'll try.", "Okay! One thing at a time! I can do that! || One step at a time! Got it! || Okay! Little steps! || I can do that! Yeah!", "...That's actually useful. Thanks. || ...Useful advice. Thanks. || ...Practical. Thank you. || ...That helps.", "...Okay. That was a good answer. || ...Good answer. || ...Okay. Smart. || ...That was decent advice.")
                    : c.T("...That's a little clumsy. But it's sweet of you. Thank you. || ...A bit clumsy. But sweet. Thank you. || ...Awkward, but kind. Thanks. || ...That was sweet, even if clumsy.", "Pfft, that was so cheesy! ...But thanks! || So cheesy! ...Thanks! || Cheesy! Haha! ...But thanks! || Pfft! Corny! ...Thank you!", "...That was awkward. I appreciate the attempt. || ...Awkward. Appreciated. || ...Clumsy attempt. Thanks. || ...Not smooth. Still, thanks.", "That was so corny. ...It worked a little, though. || Corny. ...It worked a bit. || Cheesy. ...Fine, it helped. || So lame. ...Kinda worked."));
                t.On("ht_help", () => t.Her(c.Her, good ? 6 : 3, good ? 7 : 4));
            }, 1.5f);
            o.If(c.Trust >= 40, "Promise she can always count on you", "ht_promise", () =>
            {
                b.Me("Whenever it gets heavy, come find me. I mean it. Anytime.");
                Her(c, b, Feel.Shy, c.T("...Okay. I will. Thank you, really. || ...I will. Thank you. || ...Okay. Thank you so much. || ...I'll come to you. Thanks.", "Anytime? I'm gonna hold you to that! ...Thanks. || Anytime?! Holding you to it! || Promise?! Okay! Thanks! || I'll take that! Thanks!", "...I'll remember that. || ...Remembered. || ...I won't forget. || ...Noted.", "Anytime? Careful, I might take you up on it. || Anytime? I'll use that. || Dangerous offer. Accepted. || Careful. I'll call on you."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("Um... the night market by the shrine opens today. Would you come with me? || The night market by the shrine opens today... come with me? || Um... the shrine market's tonight. Would you go with me? || There's a night market tonight... with me?", "Night market! Tonight! You're coming, right? Right?! || Night market! You're coming! Right?! || Tonight! Market! Let's go! || Market night! Come come come!",
                    "The night stalls open today. I need someone to carry things. || Night stalls open today. I need a carrier. || Market tonight. Bring your arms. || Night stalls. You're carrying.", "Night market tonight. You're coming. That's not a question. || Market tonight. You're coming. || Night market. Not optional. || You're escorting me tonight. Market.")),
                Write = (c, b, t) =>
                {
                    b.Narr("That evening. The shrine road is lined with stalls, paper lanterns glowing red and orange in the dark.");
                    b.Narr("Grilled squid, sweet corn, candied apples... the smell alone is worth the trip.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("It's so pretty at night... Where should we start? || So pretty at night... where first? || It's beautiful... where shall we go? || The lights are lovely... where to start?", "Okay! Battle plan! We eat EVERYTHING! || Battle plan! Eat EVERYTHING! || Food raid! All of it! || Plan: try everything!", "Stay close. It gets crowded. || Stay near. Crowded. || Don't wander. || Keep close.", "Try to keep up. I know all the good stalls. || Keep up. I know the best stalls. || Follow me. Insider tour. || I know where the good stuff is."));
                    var o = new Opts();
                    if (c.Snack != "")
                    {
                        var liked = Items.Get(c.Snack) != null && Items.Get(c.Snack).Likes != null && Items.Get(c.Snack).Likes.Any(l => GirlInfo.Likes(c.Her, l));
                        o.Add("Share your " + ItemName(c.Snack) + " while you walk", "st_snack", () =>
                        {
                            b.Narr("You pull the " + ItemName(c.Snack) + " out of your bag. Her eyes go wide.");
                            Her(c, b, liked ? Feel.Laugh : Feel.Smile, liked ? c.T("That's my favorite! How did you know? || My favorite! How did you know? || Ah! That's my favorite! || You knew...? It's my favorite!", "NO WAY! My favorite! You're a genius! || My favorite! Genius! || NO WAY! You know me! || Yes! Favorite! Amazing!", "...That's my favorite. Lucky guess. || ...Favorite. Good guess. || ...My favorite. Lucky. || ...That one. Correct.", "My favorite? Were you spying on me? || My favorite? Spying again? || Stalker? It's my favorite. || How'd you know? Suspicious.")
                                                                     : c.T("For me? Thank you... it's good. || For me? Thanks... yummy. || Oh, thank you... it's tasty. || Ah, you're sweet... thanks.", "Snack before the snacks! Smart! || Pre-snack snack! Smart! || Snack warmup! || Ooh! Snack time!", "...Not bad. || ...Decent. || ...Fine. || ...Good.", "Bringing your own snacks to a market? Smart. || Pre-gaming the market? Smart. || Clever. Snacks first. || Planning ahead. Nice."));
                            t.On("st_snack", () => { t.Use(c.Snack); t.Her(c.Her, liked ? 7 : 4); });
                        }, liked ? 3f : 1.5f);
                    }
                    o.If(S.Money >= 800, "Treat her to whatever she wants (¥800)", "st_treat", () =>
                    {
                        b.Narr(c.Foodie ? "She points at three stalls at once. Then a fourth." : "She thinks hard and picks a candied apple, very seriously.");
                        Her(c, b, Feel.Laugh, c.T("You're spoiling me... Thank you. || You're spoiling me... thanks. || Too generous... thank you. || Ah... you're so kind.", "You're the best! Okay, now the takoyaki! || Best! Takoyaki next! || Yay! Next stop! || Thanks! More food!", "...I'll pay you back. Someday. || ...I'll repay it. || ...Owed. || ...I'll return the favor.", "Generous tonight, aren't you? || Generous tonight. || Big spender. || Spoiling me? Good."));
                        t.On("st_treat", () => { t.Money -= 800; t.Her(c.Her, 6); });
                    }, c.Foodie ? 2f : 1f);
                    var spicyWin = Roll(G.Str, 60);
                    o.If(c.Spicy || S.Count("spicy_chips") > 0, "Take the ghost-pepper challenge [STR]", "st_spicy", () =>
                    {
                        b.Narr("A stall sells 'Hell Skewers'. Finish one and your photo goes on the wall of fame.");
                        b.Narr(spicyWin ? "You eat it in four bites. Tears stream down your face, but you finish. The crowd cheers." : "Two bites in, you can't feel your face. She buys you milk. She is laughing so hard she has to sit down.");
                        Her(c, b, Feel.Laugh, spicyWin ? c.T("You did it...! Are you okay? Your face is so red! || You did it! Are you okay? You're so red! || Ah, you won! Your face...! || You made it! Are you alright?", "LEGEND! You're on the wall! Photo! Photo! || LEGEND! On the wall! Photo! || Champion! Picture! || Hero! Hall of fame!", "...Impressive. Stupid, but impressive. || ...Impressive. Foolish, but impressive. || ...Stupidly impressive. || ...Remarkable. And dumb.", "Okay, that was actually cool. || Okay, that was cool. || Not bad. Really. || That was legit cool.")
                                                      : c.T("Here, drink this... slowly... || Here, drink... slowly... || Water... sip it... || Careful... drink this...", "AHAHAHA! Your FACE! I got it on video! || Your FACE! On video! || HAHA! Recorded! || I got it all on camera!", "Water won't help. Milk. Here. || Milk. Not water. Here. || Milk helps. Take it. || Water's useless. Milk.", "I'm framing that picture. || Framing this photo. || This is going on my wall. || Picture saved forever."));
                        t.On("st_spicy", () => { t.Her(c.Her, 4); if (spicyWin) t.Rep += 2; });
                    }, c.Spicy ? 2.5f : 1f);
                    var ringWin = Roll(G.Int, 55);
                    o.Add("Win her a prize at the ring toss [INT]", "st_rings", () =>
                    {
                        b.Narr("You calculate the angle, the weight of the ring, the wind. You explain all of it out loud.");
                        b.Narr(ringWin ? "Third ring: it lands. The old man hands over a stuffed fox the size of a cat." : "Every ring bounces off. The old man gives you a pity eraser shaped like a fish.");
                        Her(c, b, ringWin ? Feel.Laugh : Feel.Smile, ringWin ? c.T("He's so cute...! I'll name him after this night. || He's adorable! I'll name him after tonight. || So cute...! I'll treasure him. || A fox... I love him.", "FOX! I love him! He's our mascot now! || FOX! Mascot! || Our fox! Love him! || Fox buddy! Yay!", "...I'll keep it. For the fox's sake. || ...I'll keep it. For the fox. || ...Fine. I'll keep him. || ...For the fox's sake.", "A fox? Okay, I'm impressed. || A fox? Impressive. || You won a fox? Nice. || Okay. Fox. Impressed.")
                                                                         : c.T("The fish eraser is cute too... I'll use it. || The fish eraser is cute... I'll use it. || A fish eraser... I like it. || So cute... a little fish.", "Fish eraser! Best prize! Seriously! || Fish eraser! Top prize! || Best prize ever! Fish! || Fishy! Love it!", "Math lost to a ring toss. Remarkable. || Math versus ring toss. Ring toss won. || Physics failed. Eraser acquired. || Calculations lost. Remarkable.", "All that math for an eraser? Adorable. || All that math. An eraser. Cute. || Genius for an eraser. Adorable. || Big brain, tiny prize."));
                        t.On("st_rings", () => { t.Her(c.Her, ringWin ? 5 : 3); if (ringWin) t.Int += 1; });
                    });
                    o.Keep("Let her lead the way", "st_lead", () =>
                    {
                        b.Narr("She takes you straight to a tiny stall at the end of the road, run by an old woman who knows her name.");
                        Her(c, b, Feel.Smile, c.T("I've come here since I was little. I never brought anyone before. || I've come here since I was little... you're the first I brought. || This stall is special... I never showed anyone. || My childhood stall... only you know.", "This is THE best stall! Secret spot! Don't tell anyone! || Secret best stall! Don't tell! || THE stall! Shh! || Top-secret spot! Promise!", "This is the only one worth it. Don't tell people. || The only stall worth it. Keep quiet. || The best one. Secret. || Only this one matters. Don't share.", "My secret spot. Consider yourself honored. || My secret spot. Honored? || Exclusive. Feel special. || You're privileged. My spot."));
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
                        Her(c, b, Feel.Serious, c.T("Um... sir? I could help... I'm in the cooking club. || Um, sir? Can I help? Cooking club... || Excuse me... I could help, I cook... || I can help, sir... cooking club.", "Move over, I've got this! Cooking club powers! || Move! Cooking club powers! || I got this! Cooking club! || Step back! Chef mode!", "Give me the pick. Watch. || Give me the pick. Observe. || Hand it over. Watch. || The pick. Now. Watch me.", "Step aside, amateur. Let a professional work. || Aside, amateur. || Move. Professional coming. || Let the expert work."));
                        b.Narr("She flips takoyaki like she was born doing it. The line moves twice as fast. The old man bows to her.");
                        t.On("st_cook", () => t.Her(c.Her, 6, 3));
                    }, 3f);
                    var helpOk = Roll(G.Int, 50);
                    o2.Keep("Help out behind the counter [INT]", "st_help", () =>
                    {
                        b.Narr(helpOk ? "You take orders, count change and keep the line in order. In twenty minutes the line is gone." : "You mix up four orders and give someone extra octopus. Nobody complains.");
                        b.Narr("The old man presses a box of takoyaki into your hands. \"On the house, kids.\"");
                        Her(c, b, Feel.Laugh, c.T("That was fun... I liked working with you. || That was fun... working together. || I liked working with you... || That was nice... teamwork.", "Best part of the night! We're a great team! || Best part! Great team! || Teamwork! Awesome! || We rock together!", "Not bad. You can keep the job. || Not bad. Keep the job. || Acceptable work. || Decent. You're hired.", "Look at you, a working man. I approve. || A working man. Approved. || Look at you, so useful. || Labor suits you."));
                        t.On("st_help", () => { t.Her(c.Her, 4, 2); t.Rep += helpOk ? 2 : 1; t.Money += helpOk ? 500 : 200; });
                    });
                    o2.If(c.Famous, "Use your fame to draw a crowd", "st_fame", () =>
                    {
                        b.Narr("A group of first-years recognizes you. \"Is that... him?\" In a minute the line is twice as long, and twice as happy.");
                        Her(c, b, Feel.Surprised, c.T("Everyone knows you... That's amazing. || Everyone knows you... amazing. || So many people know you... || You're so well known...", "You're like a celebrity! Can I have an autograph? || Celebrity! Autograph?! || You're famous! Sign this! || A star! Wow!", "...Your reputation is ridiculous. || ...Ridiculous reputation. || ...Absurd fame. || ...Too popular.", "Famous, huh? Don't let it go to your head. || Famous. Stay humble. || Don't get a big head. || A celebrity? Careful."));
                        t.On("st_fame", () => { t.Rep += 2; t.Her(c.Her, 4); });
                    }, 1.5f);
                    o2.Add("Just wait in line and talk", "st_wait", () =>
                    {
                        b.Narr("The line moves slowly. You talk about everything: professors, songs, the worst bento you ever ate.");
                        Her(c, b, Feel.Smile, c.T("This is nice... Just talking like this. || This is nice... talking like this. || I like this... just us talking. || Peaceful... just chatting.", "Waiting is fun when you're here! Weird! || Waiting's fun with you! || Lines are fun with you! || Weird! Waiting's not boring!", "...I don't mind the wait. || ...The wait is fine. || ...I don't mind. || ...Acceptable wait.", "Twenty minutes and you didn't bore me once. Impressive. || Twenty minutes, no boredom. Impressive. || You kept me entertained. Nice. || Not boring once. Wow."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("There's a fortune teller in the shopping street everyone talks about... Would you come with me? I'm scared to go alone. || There's a famous fortune teller... come with me? I'm scared alone. || Would you visit the fortune teller with me? I'm nervous. || The fortune teller on the shopping street... with me?", "There's a fortune teller who's NEVER wrong! We have to go! Today! || A fortune teller who's NEVER wrong! Let's go! || Fortune teller! Today! Come on! || She's never wrong! Let's go now!",
                    "There's a fortune teller downtown. I want to prove she's a fraud. Come. || Downtown fortune teller. I'll expose her. Come. || A fortune teller. Likely a fraud. Witness it. || I intend to debunk a fortune teller. Join me.", "Let's get our fortunes read. I want to know if you'll be rich someday. For my own reasons. || Fortunes. I want to know if you'll be rich. Reasons. || Let's see our futures. For research. || Fortune telling. I'm curious about yours.")),
                Write = (c, b, t) =>
                {
                    b.Narr("A narrow shop in the arcade: purple curtains, incense, a crystal ball the size of a melon.");
                    b.Narr("Madame Rei looks up from behind the ball as if she has been waiting for you two all week.");
                    Callback(c, b);
                    Her(c, b, Feel.Surprised, c.T("She's looking right at us... I'm nervous. || She's staring at us... I'm nervous. || She's looking right here... eek. || Her eyes... I'm scared.", "Whoa, it's so atmospheric! Look at the ball! || So spooky! Look at the ball! || Whoa! Crystal ball! || Atmosphere! Cool!", "Smoke and mirrors. Watch. || Theatrics. Watch. || Showmanship. || Smoke and tricks.", "She's good at the creepy stare. I'll give her that. || Great creepy stare. || The stare is good. I admit. || Spooky. Points for that."));
                    var o = new Opts();
                    o.Keep("Ask about her future", "ft_future", () =>
                    {
                        b.Narr("The crystal ball swirls. Madame Rei narrows her eyes.");
                        b.Narr(Future(c));
                        Her(c, b, Feel.Shy, c.T("...How did she know? I never told anyone that. || ...How did she know? I never told anyone. || ...That's my secret... how? || ...Nobody knows that...", "THAT'S EXACTLY MY DREAM! How?! HOW?! || MY DREAM! How?! || EXACTLY! How did she know?! || What?! That's it exactly!", "...Lucky guess. A very lucky guess. || ...Lucky guess. Very lucky. || ...Coincidence. || ...Statistically possible.", "...Okay. That was a little creepy. || ...Creepy. A little. || ...Okay, unsettling. || ...That was spooky."));
                        t.On("ft_future", () => t.Her(c.Her, 3, 4));
                    });
                    o.If(c.Tier >= 1, "Ask about your friendship", "ft_friends", () =>
                    {
                        b.Narr(c.Trust >= 50 ? "\"Two threads, tangled since spring. Pull on one and the other answers.\"" : "\"Two threads, side by side. Not tied yet. That depends on you.\"");
                        Her(c, b, Feel.Smile, c.T("Tangled threads... I like that. || Tangled threads... I like it. || Tangled together... nice. || Our threads tangle... hehe.", "We're tangled! That's the best kind of friends! || Tangled! Best friends! || Tangled forever! || We're knotted! Yay!", "...Vague. But not wrong. || ...Vague. Not wrong. || ...Ambiguous. Accurate. || ...Hm. Fair.", "So we're stuck with each other. Fine by me. || Stuck together. Fine. || Tangled? Okay by me. || Bound to you. Acceptable."));
                        t.On("ft_friends", () => t.Her(c.Her, 4, 3));
                    }, 1.5f);
                    o.Add("Ask about the next exam", "ft_exam", () =>
                    {
                        b.Narr("Madame Rei stares into the ball for a long time. \"...Study.\"");
                        Her(c, b, Feel.Laugh, c.T("Ahaha... that's good advice, at least. || Ahaha... good advice, at least. || Hehe... that's useful. || Well... it's sensible.", "That's IT?! Ahaha! Worth it! || That's IT?! Haha! || Ahaha! Worth it! || Seriously?! Lol!", "The only accurate prediction today. || Only accurate prediction. || Finally, accuracy. || That one I believe.", "Wow. Mystical. || So mystical. || Deep. Very deep. || Wow. Profound."));
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
                        Her(c, b, Feel.Pout, c.T("...H-hey! That's not for her to say! || H-hey! That's not hers to say! || Wait! She can't say that! || That's private...!", "HEY! Whose side is she on?! || HEY! Whose side?! || Excuse me?! || What?! Rude!", "...That's none of her business. || ...Not her business. || ...Overstepping. || ...Irrelevant.", "Ugh. Fine. She's not entirely wrong. || Fine. She's not wrong. || Ugh. Okay, maybe. || Annoyingly accurate."));
                        t.On("ft_forgive", () => t.Her(c.Her, 2, 4));
                    }, 2f);
                    o.Play(b, "ft_1");

                    b.Narr("\"One more thing,\" Madame Rei says. \"One of you carries luck. It wants to be given away.\"");
                    var o2 = new Opts();
                    o2.If(S.Count("omamori") > 0, "Give her your lucky charm", "ft_charm", () =>
                    {
                        b.Narr("You take out your omamori and put it in her hand.");
                        Her(c, b, Feel.Shy, c.T("For me...? I'll keep it with me every day. || For me? I'll carry it every day. || A charm...? I'll treasure it. || Thank you... I'll keep it close.", "Lucky charm! Now I'm invincible! Thank you! || Lucky charm! Invincible! || Charm power! Thanks! || Now I'm unstoppable!", "...You need it more than me. ...I'll keep it anyway. || ...You need it more. ...I'll keep it. || ...Unnecessary. ...Kept. || ...Fine. I'll hold onto it.", "Giving away your luck? Careful. I'll keep it. || Giving away luck? Mine now. || Your luck? I'll take it. || Careful. I'm keeping this."));
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
                        b.Narr(popular ? "\"Many people will come to you for help. You will not turn them away.\"" : "\"I see... a lot of assignments. A truly frightening amount.\"");
                        Her(c, b, Feel.Laugh, c.T("That sounds just like you. || So like you. || That's you exactly. || That sounds right.", "Ahaha! That's SO you! || SO you! || Ahaha! Perfect! || That's totally you!", "Accurate. || Accurate. || Correct. || Precise.", "Ha. She's got your number. || She read you. || Nailed it. || Ha. Spot on."));
                        t.On("ft_mine", () => { if (popular) t.Rep += 1; t.Her(c.Her, 2); });
                    });
                    o2.Keep("Laugh it off", "ft_laugh", () =>
                    {
                        b.Narr("You laugh. Madame Rei does not. \"Laugh now,\" she says, very quietly. The candles flicker.");
                        Her(c, b, Feel.Surprised, c.T("L-let's go. Let's go now. || L-let's leave now. || Let's go... please. || I want to go...", "Okay that was SCARY. Let's get crepes. || Scary! Crepes! || Too spooky! Crepe time! || Eek! Let's get dessert!", "...Unsettling. Let's leave. || ...Unsettling. Leave. || ...Let's go. || ...Enough.", "Okay, she's good. Let's go. || She's good. Go. || Okay. Out. || Let's leave. She's legit."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("Have you heard? The piano in the old music room plays by itself after dark... Would you check it with me? || The old music room piano plays itself at night... check with me? || A ghost piano... would you come look with me? || They say the piano plays alone... come with me?", "The seventh wonder of the university! A ghost piano! We're investigating tonight! || Ghost piano! We're investigating tonight! || Campus wonder number seven! Tonight! || Ghost hunt! Piano! Tonight!",
                    "There's a rumor about a ghost piano. I intend to prove it's nonsense. You're my witness. || Ghost piano rumor. I'll disprove it. Witness. || I'll debunk the piano rumor. Come. || Nonsense rumor. Help me prove it.", "Want to go ghost hunting? The old music room. After dark. Unless you're scared. || Ghost hunting? Unless you're scared. || Music room. Night. Scared? || Ghosts. Tonight. Chicken?")),
                Write = (c, b, t) =>
                {
                    b.Narr("After club hours. The university is empty, and the old wing is darker than it has any right to be.");
                    b.Narr("Somewhere upstairs, very faintly: a piano. Three notes. Then nothing.");
                    Callback(c, b);
                    Her(c, b, Feel.Surprised, c.T("D-did you hear that...? || D-did you hear...? || What was that...? || Eek... a sound...", "THERE IT IS! The ghost! Come on! || THERE! Ghost! || That's it! Come on! || It's here!", "...There's a logical explanation. || ...Logical explanation incoming. || ...Something mundane. || ...There's a reason.", "Oh, this is going to be good. || Oh, fun. || This'll be good. || Here we go."));
                    Pal(c, b, Feel.Worried, c.P("I want to go home. I want to go home now.", "This is the best night ever!", "Ghosts don't exist. ...Walk in front.", "If a ghost eats you, can I have your bag?"));
                    var o = new Opts();
                    var brave = Roll(G.Str, 50);
                    o.Keep("Follow the sound [STR]", "wd_follow", () =>
                    {
                        b.Narr(brave ? "You walk in front, steady. Every board creaks. She holds on to the back of your jacket." : "A mop falls out of a closet. You both scream. Then you both laugh, shaking.");
                        Her(c, b, brave ? Feel.Smile : Feel.Laugh, brave ? c.T("You're so calm... I feel safer. || You're so calm... I feel safe. || You're brave... I feel better. || Being with you helps...", "You're like a ghost hunter! A pro! || Pro ghost hunter! || You're fearless! || Ghostbuster!", "...Good. Keep walking. || ...Good. Continue. || ...Keep going. || ...Fine.", "Not bad. I thought you'd run. || Thought you'd run. || Not bad. No running. || Brave. Surprising.")
                                                                          : c.T("That was a mop... it was just a mop... || A mop... just a mop... || It was only a mop... || A mop... oh no...", "THE MOP! AHAHA! We screamed at a MOP! || THE MOP! We screamed at a mop! || A MOP! Hahaha! || Mop ghost! Lol!", "...We will never speak of this. || ...Never speak of it. || ...Forgotten. || ...This didn't happen.", "You screamed higher than me. Noted. || Your scream was higher. Noted. || You squealed. I heard. || Higher pitch than me. Ha."));
                        t.On("wd_follow", () => { t.Her(c.Her, 3, 3); if (brave) t.Str += 1; });
                    });
                    o.If(G.Int >= 35, "Check the club logbook first [INT]", "wd_log", () =>
                    {
                        b.Narr("In the music club's logbook, under today's date: \"Do NOT leave the window open again. — K.\"");
                        Her(c, b, Feel.Serious, c.T("The window...? So something comes in from outside? || The window...? Something gets in? || From outside...? The window? || The window's open...?", "A clue! We're real detectives now! || A clue! Detectives! || Evidence! Yes! || We're detectives!", "Interesting. That's evidence. || Interesting. Evidence. || A clue. Good. || Noted. Evidence.", "Ooh, a mystery with clues. Smart. || Clues. Smart mystery. || Ooh, evidence. || A real mystery."));
                        t.On("wd_log", () => { t.Int += 1; t.Her(c.Her, 3); });
                    }, 1.5f);
                    o.If(c.Animals, "Listen closely", "wd_meow", () =>
                    {
                        Her(c, b, Feel.Surprised, c.T("Wait... was that a meow? || Was that... a meow? || A meow...? || Did something meow?", "Did you hear a MEOW?! || A MEOW?! || Meow! Did you hear?! || MEOW?!", "...That sounded like a cat. || ...A cat. || ...Feline sound. || ...That's a cat.", "Ghosts don't meow. Usually. || Ghosts don't meow. || Meowing ghost? Hm. || That's no ghost."));
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
                    Her(c, b, Feel.Laugh, c.T("It's a cat...! The ghost is a cat! || A cat! The ghost is a cat! || It's a kitty...! || The ghost was a cat...!", "THE GHOST IS A CAT! Best ghost ever! || GHOST CAT! Best ever! || A cat! Hahaha! || Kitty ghost!", "...Case closed. As expected. || ...Case closed. || ...As predicted. || ...Solved.", "Pfft. The terrifying seventh wonder. || Terrifying wonder. A cat. || The seventh wonder. Meow. || Pfft. Spooky cat."));
                    var o2 = new Opts();
                    o2.If(S.Count("cat_food") > 0, "Offer him your cat food", "wd_feed", () =>
                    {
                        b.Narr("You shake the bag. The cat's ears turn. He trots over and eats out of your hand, purring like an engine.");
                        Her(c, b, Feel.Laugh, c.T("He likes you...! Can I pet him? Oh, he's so soft... || He likes you! Can I pet him? So soft... || He's so fluffy... || Aww, he's purring...", "He's PURRING! He loves us! || PURRING! He loves us! || Kitty loves us! || He's happy!", "...He has good taste. In food. || ...Good taste. In food. || ...He likes snacks. || ...Smart cat.", "Bribing a cat. Smooth. || Bribing a cat. Smooth. || Cat bribery. Clever. || Buying his love. Nice."));
                        t.On("wd_feed", () => { t.Use("cat_food"); t.Her(c.Her, c.Animals ? 8 : 5, 2); });
                    }, 3f);
                    var caught = Roll(G.Str, 55);
                    o2.Keep("Try to catch him", "wd_catch", () =>
                    {
                        b.Narr(caught ? "You catch him. He scratches you, then decides he likes being held. Typical." : "He leaps over your head, out the window, and knocks a stack of sheet music onto both of you.");
                        Her(c, b, Feel.Laugh, c.T("Are you okay? Ahaha... sorry, it's funny... || Are you okay? Hehe... sorry... || Ah... are you hurt? Hehe... || Pfft... sorry, are you okay?", "AHAHA! Paper everywhere! He won! || Paper everywhere! Cat wins! || HAHA! He beat you! || Cat: one, you: zero!", "...The cat outsmarted you. || ...Outsmarted by a cat. || ...The cat won. || ...Defeated. By a cat.", "Beaten by a cat. Iconic. || Beaten by a cat. Iconic. || Cat victory. Legendary. || Lost to a cat. Classic."));
                        t.On("wd_catch", () => t.Her(c.Her, 4));
                    });
                    var song = Roll(G.Sens, 50);
                    o2.Add("Play him a song yourself [SEN]", "wd_play", () =>
                    {
                        b.Narr(song ? "You sit down and play the one song you know, slowly. The cat lies down on the lid and listens." : "You play the one song you know, badly. The cat looks at you with deep disappointment and leaves.");
                        Her(c, b, song ? Feel.Smile : Feel.Laugh, song ? c.T("That was beautiful... I didn't know you could play. || So beautiful... I didn't know you played. || That was lovely... || You can play...? Wonderful.", "Whoa! You can PLAY?! Again! || You PLAY?! Again! || Whoa! Encore! || Amazing! More!", "...Not bad. At all. || ...Not bad at all. || ...Skilled. || ...Good.", "Hidden talent? I'm impressed. || Hidden talent? Impressed. || Secret pianist? Nice. || You play? Wow.")
                                                                       : c.T("He... didn't like it. I did, though. || He didn't like it... I did. || The cat left... but I loved it. || He walked away... I liked it, though.", "Even the cat left! AHAHA! || Even the cat left! || The cat bailed! Haha! || Cat said no! Ahaha!", "The cat has spoken. || The cat has judged. || Verdict: cat disapproves. || The cat disagrees.", "Critics everywhere. || Critics. || Tough crowd. || Everyone's a critic."));
                        t.On("wd_play", () => { t.Her(c.Her, song ? 5 : 3); if (song) t.Sens += 1; });
                    });
                    o2.If(c.Tier >= 2, "Keep it a secret between you", "wd_secret", () =>
                    {
                        b.Me("Let's not tell anyone. The seventh wonder can be ours.");
                        Her(c, b, Feel.Smile, c.T("Our secret... I like that. || Our secret... I like it. || Just ours... || A secret... hehe.", "A secret club of two! Plus one cat! || Secret club! Plus cat! || Two members and a cat! || Secret squad!", "...Fine. Our secret. || ...Our secret. || ...Fine. Secret. || ...Between us.", "A shared secret. You're full of surprises. || Shared secret. Surprising. || A secret together. Nice. || Our little secret."));
                        t.On("wd_secret", () => t.Her(c.Her, 4, 6));
                    }, 1.5f);
                    o2.Play(b, "wd_2");
                    b.Narr("You close the window on the way out. Somewhere in the courtyard, a cat meows goodnight.");
                    Finale(c, b, t, "wonder");
                    b.Narr("The seventh wonder is solved. The university will never know.");
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
                Invite = (c, i) => i.Say(c.Her, c.T("The haunted house behind the arcade reopened... I want to try it, but not alone... || The haunted house reopened... I want to go, but not alone... || Would you try the haunted house with me? || The haunted house... with you, maybe I can.", "The haunted house reopened! Whoever screams first buys crepes! || Haunted house! First scream buys crepes! || Haunted house bet! Loser buys crepes! || Scream first, pay for crepes!",
                    "The haunted house reopened. I've heard it's not scary. Let's confirm. || Haunted house. Supposedly tame. Let's verify. || The haunted house. Probably boring. Confirm with me. || Let's test the haunted house.", "Haunted house. You and me. Loser of 'who screams first' buys crepes. || Haunted house. Loser buys crepes. || You, me, haunted house. Bet. || First scream pays. Deal?")),
                Write = (c, b, t) =>
                {
                    b.Narr("The haunted house behind the arcade: rotten boards, flickering lanterns, a sign that says NO REFUNDS.");
                    b.Narr("Inside it smells like dust and fake blood. The door slams shut behind you.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Remember... whoever screams first... buys crepes... || First scream... buys crepes... || Remember... the bet... || The loser pays... okay...", "The bet is ON! Prepare your wallet! || Bet's ON! Wallet ready! || Game on! Get your wallet! || Prepare to pay!", "I won't scream. Prepare your wallet. || I won't scream. Your wallet. || Wallet ready? I won't scream. || I'm calm. You're paying.", "First one to scream pays. Ready? || First scream pays. Ready? || Ready to lose? || Bet stands. Go."));
                    var steady = Roll(G.Str, 55);
                    var o = new Opts();
                    o.Keep("Walk in front [STR]", "hh_front", () =>
                    {
                        b.Narr("A ghost bursts out of a closet with a horrible wail.");
                        b.Narr(steady ? "You don't even flinch. Behind you, she lets out a scream that echoes through the whole building." : "You make a noise you did not know you could make. Behind you, she is already laughing.");
                        Her(c, b, steady ? Feel.Pout : Feel.Laugh, steady ? c.T("That... that doesn't count! It came out by itself! || That doesn't count! It slipped out! || No! It wasn't a real scream! || That was an accident...!", "NO! That doesn't count! It was a surprise scream! || Doesn't count! Surprise scream! || NO! Reflex! Doesn't count! || That was a squeak! Not a scream!", "...That wasn't a scream. That was a cough. || ...A cough. Not a scream. || ...Clearing my throat. || ...That was nothing.", "Ugh. Fine. I'll buy the crepes. || Fine. I'll buy. || Ugh. My treat. || Okay. Crepes on me.")
                                                                           : c.T("You screamed first... crepes are on you. || You screamed first... crepes are yours. || You lost... crepes, please. || Hehe... you pay.", "YOU SCREAMED! Crepes! CREPES! || YOU SCREAMED! Crepes! || Crepes! You lost! || Pay up! Crepes!", "Crepes. Strawberry. Thank you. || Strawberry crepe. Thank you. || Crepe. Mine. Thanks. || Strawberry. Pay.", "I'll take the most expensive crepe, please. || The priciest crepe, please. || Most expensive one. || Deluxe crepe, thanks."));
                        t.On("hh_front", () => { t.Her(c.Her, 4, 2); if (!steady) t.Money -= 400; });
                    });
                    o.If(G.Int >= 30, "Read the map out loud [INT]", "hh_map", () =>
                    {
                        b.Narr("You take a shortcut through a door marked STAFF. Three zombies in full makeup look up from their lunch.");
                        b.Narr("\"...You're not supposed to be here.\" One of them points you back with a half-eaten sandwich.");
                        Her(c, b, Feel.Laugh, c.T("They were eating... onigiri... || They were eating onigiri... || The zombies had lunch... || Onigiri... the ghosts...", "The ZOMBIES were having LUNCH! AHAHA! || ZOMBIE LUNCH! || The undead eat onigiri! || Zombies snacking! Haha!", "...The magic is gone. || ...Magic ruined. || ...Immersion broken. || ...So much for scary.", "Best part of the haunted house. || Best part. || Highlight of the trip. || Peak haunted house."));
                        t.On("hh_map", () => t.Her(c.Her, 4));
                    });
                    o.If(G.Sens >= 40, "Scare the scarers [SEN]", "hh_scare", () =>
                    {
                        b.Narr("You hide behind a curtain. When the next ghost creeps by, you jump out first. He screams and drops his wig.");
                        Her(c, b, Feel.Laugh, c.T("You scared the ghost...! || You scared the ghost...! || The ghost screamed...! || You frightened it...!", "You made the GHOST scream! Legend! || GHOST screamed! Legend! || You scared it! Ha! || Reverse scare! Yes!", "...That was evil. I approve. || ...Evil. Approved. || ...Wicked. Good. || ...Devious. I like it.", "Turning the tables. Nice. || Tables turned. Nice. || Reverse haunting. Clever. || Scaring the scarer. Good."));
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
                        Her(c, b, Feel.Laugh, c.T("You made friends with the ghost... || You befriended the ghost... || The ghost likes you... || Friends with a ghost...", "You're friends with a GHOST now! || Ghost friend! || You have a GHOST pal! || Ghost buddy!", "...Only you would do that. || ...Only you. || ...Typical of you. || ...Of course.", "Networking with the undead. Smart. || Networking with the dead. || Undead connections. Smart. || Ghost contacts. Useful."));
                        t.On("hh_hello", () => { t.Her(c.Her, 4); t.Rep += 1; });
                    });
                    o2.If(c.Tier >= 2, "Stand between her and the well", "hh_shield", () =>
                    {
                        b.Narr("You step in front of her without thinking. The ghost stops, impressed, and gives you a thumbs-up.");
                        Her(c, b, Feel.Smile, c.T("...You stood in front of me. Thank you. || You protected me... thank you. || You stood in front... thanks. || You shielded me...", "Whoa, that was so cool! My bodyguard! || My bodyguard! Cool! || So cool! Protector! || Hero mode!", "...Unnecessary. ...Thanks. || ...Unneeded. ...Thanks. || ...Unnecessary. ...Thank you. || ...I was fine. ...Thanks.", "Protecting me from a part-timer? Brave. || Brave against a part-timer. || Protecting me from staff? Cute. || Bodyguard vs. employee. Bold."));
                        t.On("hh_shield", () => t.Her(c.Her, 4, 5));
                    }, 1.5f);
                    o2.If(c.Tone == Tone.Energetic || c.Tone == Tone.Teasing || G.Sens >= 50, "Pretend to faint", "hh_faint", () =>
                    {
                        b.Narr("You collapse dramatically onto the floor. The ghost panics and calls for a manager.");
                        Her(c, b, Feel.Laugh, c.T("Are you okay?! ...You're faking! You're faking! || Are you okay?! ...You're faking! || Oh no! ...Wait, faking?! || Are you hurt?! ...Liar!", "AHAHA! You gave the ghost a heart attack! || Ghost heart attack! || HAHA! You scared him! || Fake faint! Genius!", "...Get up. Everyone is staring. || ...Get up. People stare. || ...Stand. Now. || ...Up. Embarrassing.", "Ten out of ten. Very convincing. || Ten out of ten. || Oscar-worthy. || Very convincing."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("They say it'll snow tonight... Would you walk home with me? || It might snow tonight... walk home with me? || Snow's coming... would you walk with me? || They say snow tonight... come home with me?", "Snow tonight! Walk home with me! We can make snow angels! || Snow! Walk me home! Snow angels! || Snowfall tonight! Walk with me! || Snow! Let's walk together!",
                    "It's going to snow. Walk me home. The streets are dark. || Snow tonight. Walk me. Dark streets. || Snow's coming. Escort me. || It'll snow. Walk me home.", "Snow tonight. Walk me home. You can carry my bag. || Snow. Walk me. Carry my bag. || Snowy night. You're my porter. || Walk me home. Bag duty.")),
                Write = (c, b, t) =>
                {
                    b.Narr("Evening. Snow is falling on the empty street, soft and silent, under the streetlights.");
                    b.Narr("Your footsteps are the only sound. Her breath comes out in little white clouds.");
                    Callback(c, b);
                    Her(c, b, Feel.Worried, c.T("Ah... my glove. I had both of them a minute ago... || My glove... I had both a moment ago... || Oh no... a glove is gone... || Where's my other glove...?", "My glove! Where's my glove?! It was right here! || Glove! Where?! Right here! || My glove vanished! || GLOVE! Gone!", "...I lost a glove. Wonderful. || ...One glove. Lovely. || ...Lost a glove. Great. || ...A glove. Missing.", "Great. One glove. Very fashionable. || One glove. Trendy. || Single glove fashion. || Great. Half a pair."));
                    var o = new Opts();
                    var found = Roll(G.Int, 45);
                    o.Keep("Retrace your steps [INT]", "sn_find", () =>
                    {
                        b.Narr(found ? "Back at the vending machine, on top of the trash can: a small knitted glove with snow on it." : "You walk back three blocks. No glove. But you find a snowman someone built with a traffic cone for a hat.");
                        Her(c, b, found ? Feel.Laugh : Feel.Laugh, found ? c.T("You found it...! Thank you! || You found it! Thank you! || My glove! Thank you so much! || Ah! You're wonderful!", "MY GLOVE! You're a detective! || GLOVE! Detective! || Found it! Hero! || You're amazing! Glove!", "...Thanks. Good eyes. || ...Thanks. Sharp eyes. || ...Found. Good. || ...Thank you.", "Not bad, detective. || Nice, detective. || Good work, sleuth. || Not bad.")
                                                                        : c.T("No glove... but that snowman is so cute. || No glove... but the snowman's cute. || The snowman is adorable... || Forget the glove... look!", "Forget the glove! LOOK AT HIM! || LOOK AT HIM! || Snowman! Best! || Forget it! Snowman!", "...That snowman has more style than me. || ...More stylish than me. || ...That snowman has flair. || ...Dapper snowman.", "The cone hat is genius. || Cone hat. Genius. || That hat's perfect. || Brilliant hat."));
                        t.On("sn_find", () => t.Her(c.Her, found ? 5 : 3, 2));
                    });
                    o.Add("Lend her your gloves", "sn_gloves", () =>
                    {
                        b.Narr("You pull off your gloves and hold them out. She stares at them.");
                        Her(c, b, Feel.Shy, c.T("But then your hands... ...Okay. Thank you. They're warm. || But your hands... okay. Thank you. Warm. || Your hands will be cold... thank you. || Ah... they're so warm. Thank you.", "Really?! ...They're so big on me! Ahaha! || Really?! Huge on me! Haha! || So big! Hehe! Warm! || Giant gloves! Thanks!", "...You'll freeze. ...Fine. Thank you. || ...You'll freeze. ...Thanks. || ...Foolish. ...Thank you. || ...Fine. Warm. Thanks.", "Gallant. ...They're warm. Thanks. || Gallant. ...Warm. Thanks. || Chivalrous. ...Thanks. || Gentleman. ...Cozy."));
                        t.On("sn_gloves", () => t.Her(c.Her, 5, 3));
                    }, 1.3f);
                    var coffee = S.Count("canned_coffee") > 0;
                    o.If(coffee || S.Money >= 150, coffee ? "Share your canned coffee" : "Buy two hot drinks (¥150)", "sn_drink", () =>
                    {
                        b.Narr("The can is so hot she holds it with her sleeves. Steam rises between you.");
                        Her(c, b, Feel.Smile, c.Coffee ? c.T("Black coffee... how did you know? || Black coffee... you knew? || How did you know black? || Black... you remembered?", "Coffee! Hot coffee! You're a lifesaver! || Hot coffee! Lifesaver! || Coffee! Yes! || Warm drink! Hero!", "...Black. You remembered. || ...Black. Remembered. || ...You know my order. || ...Black. Correct.", "Black coffee. Good taste. || Good taste. || Black. Smart. || You know me.")
                                                       : c.T("It's so warm... thank you. || So warm... thanks. || Ah... it's warm. Thank you. || Mm... warm...", "Hot hot hot! Thank you! || Hot! Thanks! || Toasty! || Ahh! Warm!", "...Warm. Thanks. || ...Warm. || ...Thanks. || ...Good.", "My hands are thawing. Thanks. || Thawing. Thanks. || Hands alive again. || Defrosting. Thanks."));
                        t.On("sn_drink", () => { if (coffee) t.Use("canned_coffee"); else t.Money -= 150; t.Her(c.Her, c.Coffee ? 7 : 4); });
                    }, c.Coffee ? 2.5f : 1f);
                    var hit = Roll(G.Str, 50);
                    o.If(c.Tone == Tone.Energetic || c.Tier >= 1, "Start a snowball fight [STR]", "sn_fight", () =>
                    {
                        b.Narr("A snowball, perfectly round. You throw it. War is declared.");
                        b.Narr(hit ? "Five minutes later she surrenders, covered in snow from head to toe and laughing too hard to throw." : "Five minutes later you are the snowman. She is merciless.");
                        Her(c, b, Feel.Laugh, c.T("I give up! I give up! Ahaha... || I give up! Ahaha... || Okay, you win! Hehe! || I surrender! Ahaha!", "REMATCH! Right now! || REMATCH! || Again! Now! || One more!", "...I let you win. Obviously. || ...I let you win. || ...Allowed you that. || ...Mercy. Obviously.", "Victory is mine. Bow. || Victory. Bow. || I win. Kneel. || Mine. Bow down."));
                        t.On("sn_fight", () => t.Her(c.Her, 5));
                    }, 1.3f);
                    o.Play(b, "sn_1");

                    b.Bg("snow_park_night");
                    b.Narr("The little park near the station. One lamp, one bench, everything under a thin white blanket.");
                    b.Narr("You brush the snow off the bench and sit down. For a while, nobody says anything. It's nice.");
                    Her(c, b, Feel.Smile, c.T("It's so quiet... like the whole town is asleep. || So quiet... the town is asleep. || Everything's hushed... || So peaceful...", "It's so pretty! Like a snow globe! || Snow globe! So pretty! || Like a postcard! || Pretty!", "...I like the quiet. || ...Quiet. Good. || ...I like this silence. || ...Peaceful.", "Peaceful. For once. || Peaceful. Rare. || Calm. For once. || Quiet. Nice change."));
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
                    ? c.T("Could you help me practice after class? I need someone to play against. || Practice with me after class? I need a partner. || Could you play against me after class? || Would you help me practice later?", "One-on-one! After class! I need a rival! || One-on-one! Rival needed! || After class! Face me! || Rival time! After class!", "I need a practice partner. You'll do. || Practice partner needed. You qualify. || I need someone to practice with. You. || You'll be my practice partner.", "Practice with me after class. I'll go easy on you. || Practice later. I'll go easy. || After class. I'll be gentle. || Play me. I'll hold back.")
                    : c.T("Um... could you teach me to shoot a basket? Please don't laugh. || Teach me to shoot? Don't laugh... || Could you show me basketball? Please... || I want to learn baskets... help?", "Teach me basketball! I've never made a single basket in my life! || Teach me! Zero baskets ever! || Basketball lessons! I'm hopeless! || Teach me! I've never scored!", "I want to learn to shoot a basket. Don't make it weird. || Teach me shooting. Not weird. || Basketball. Teach me. Normally. || I want to learn. Keep it simple.", "Teach me basketball. If I'm bad, you're not allowed to laugh. || Teach me. No laughing. || Basketball. Laugh and die. || Show me. Mock me and regret it.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The gym after hours: one row of lights on, the smell of floor wax, the echo of a single ball bouncing.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.Sporty ? c.T("Don't hold back, okay? I want to get better. || Don't hold back. I want to improve. || Play for real, okay? || Please be serious with me.", "Okay! Game face! Don't go easy on me! || Game face! No mercy! || Serious mode! Go! || Don't go easy!", "Play seriously. Or don't bother. || Seriously, or not at all. || Full effort. || No holding back.", "Try to keep up. || Keep up. || Try, at least. || Don't bore me.")
                                                     : c.T("It's so far up... how does anyone get it in? || It's so high... how does anyone score? || The hoop is so far... || How do people reach that?", "Okay! I'm gonna dunk! ...What's a dunk? || I'll dunk! ...What's a dunk? || Dunk time! ...Wait, how? || I'm dunking! ...Somehow!", "The hoop is too high. That's a design flaw. || Too high. Bad design. || The hoop's placement is flawed. || Unreasonable height.", "How hard can it be? ...Don't answer. || How hard can it be? Don't answer. || Easy. ...Right? || Simple. ...Don't say it."));
                    var o = new Opts();
                    var win = Roll(G.Str, c.Sporty ? 70 : 45);
                    o.Keep("Play one-on-one [STR]", "gy_1on1", () =>
                    {
                        b.Narr(win ? "It's close. At the last second you sink one from the corner. She falls on her back, laughing and panting." : "She runs circles around you. By the end you're on the floor and she's spinning the ball on one finger.");
                        Her(c, b, win ? Feel.Pout : Feel.Laugh, win ? c.T("You're good... One more! Just one more! || You're good! One more! || Again... please! || You're so good... once more!", "NO WAY! Rematch! RIGHT NOW! || REMATCH! NOW! || No way! Again! || Again! Again!", "...Lucky. Again. || ...Luck. Again. || ...Fluke. Repeat. || ...Again.", "Okay, okay. You can play. A little. || Okay. You can play. Slightly. || Fine. You're decent. || Alright. Some skill.")
                                                                    : c.T("Ahaha... are you okay down there? || Are you okay down there? Hehe... || Ahaha... need help up? || Oops... you okay?", "I WIN! Victory dance! || WIN! Dance! || Victory! Woo! || I win! Yay!", "Too easy. Practice more. || Too easy. Practice. || Simple. Train more. || Easy win.", "Need a hand up, champ? || Need a hand, champ? || Help up, champion? || Floor comfy?"));
                        t.On("gy_1on1", () => { t.Her(c.Her, 4, 2); t.Str += 1; });
                    });
                    var form = Roll(G.Int, 50);
                    o.Add("Fix her form [INT]", "gy_form", () =>
                    {
                        b.Me("Elbow under the ball. Push with your legs, not your arms. Follow through, like you're reaching into a cookie jar.");
                        b.Narr(form ? "She tries it. Swish. Again. Swish. Five in a row. She stares at her hands." : "She tries it. The ball hits the bottom of the rim and bounces off her head. She laughs anyway.");
                        Her(c, b, Feel.Laugh, form ? c.T("It works...! You're a good professor! || It works! You're a great professor! || I scored! Thank you! || You're such a good professor!", "FIVE IN A ROW! Coach! You're my coach now! || Five straight! Coach! || FIVE! You're my coach! || Streak! Coach!", "...That's actually correct. Thanks. || ...Correct. Thanks. || ...It works. Thank you. || ...Effective.", "A cookie jar. Ridiculous. ...It works. || Cookie jar. Silly. Works. || Ridiculous trick. Effective. || Absurd. Works.")
                                                   : c.T("Ow... ahaha... the ball hates me. || Ow... the ball hates me. || Ouch... hehe... unlucky. || Ow... it bounced at me...", "It attacked me! The ball is evil! || The ball attacked! || Evil ball! || The ball's out to get me!", "...The physics were sound. The ball was not. || ...Sound physics. Rogue ball. || ...The ball misbehaved. || ...Theory correct. Ball wrong.", "Okay, that one was my fault. || My fault. That one. || Okay, mine. || I'll own that."));
                        t.On("gy_form", () => { t.Her(c.Her, form ? 5 : 3, 2); t.Int += 1; });
                    }, c.Sporty ? 1f : 1.8f);
                    o.If(S.Count("sports_drink") > 0, "Toss her a sports drink", "gy_drink", () =>
                    {
                        b.Narr("You toss her a cold bottle from your bag. She catches it one-handed.");
                        Her(c, b, Feel.Smile, c.T("You brought this for me? Thank you... || For me? Thank you... || You brought this? Thanks... || Oh, how kind...", "Ice cold! You're the best! || Ice cold! Best! || Cold drink! Yes! || Refreshing! Thanks!", "...Prepared. Good. || ...Prepared. Good. || ...Thoughtful. || ...Good planning.", "Thoughtful. I'll allow it. || Thoughtful. Allowed. || Considerate. Okay. || Nice. I'll accept."));
                        t.On("gy_drink", () => { t.Use("sports_drink"); t.Her(c.Her, c.Sporty ? 7 : 4); });
                    }, 2f);
                    o.Add("Rebound for her while she shoots", "gy_rebound", () =>
                    {
                        b.Narr("A hundred shots. You catch every one and pass it back. Somewhere around fifty, she starts talking.");
                        Her(c, b, Feel.Worried, c.Sporty ? c.T("The tournament is next month... Everyone's counting on me. I can't sleep some nights. || The tournament's next month... everyone counts on me. I can't sleep. || Next month's tournament... the pressure keeps me awake. || The tournament... I'm so afraid of failing everyone.", "Tournament's coming! I'm SO excited! ...And so scared, honestly. || Tournament! Excited! ...And terrified! || Big tournament! So pumped! ...And scared! || Next month! Hyped! ...Nervous, honestly.", "The tournament. If I miss, it's on me. || The tournament. A miss is mine. || If I fail at the tournament, it's my fault. || Tournament. All on me.", "The tournament's next month. Everyone thinks I'm confident. I'm faking it. || Next month. I look confident. I'm not. || Everyone thinks I'm fearless. Faking. || The tournament. My confidence is an act.")
                                                        : c.T("I've always been bad at sports... I just wanted to be good at one thing. || I've never been good at sports... I wanted one thing. || I'm bad at sports... I wanted to change that. || I just want to be good at something...", "Everybody's sporty except me! I wanted to try! || Everyone's sporty but me! I wanted in! || I'm the unsporty one! Wanted to try! || I wanted to be sporty too!", "I hate being bad at things. || I dislike being bad at things. || Being bad annoys me. || I hate incompetence. Mine.", "I hate losing. Even at things I don't care about. || I hate losing. At anything. || Losing is unacceptable. Always. || Even trivial losses bug me."));
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
                        Her(c, b, Feel.Laugh, shot ? c.T("You beat the captain...! || You beat the captain! || You won against the captain...! || The captain lost...!", "YOU BEAT THE CAPTAIN! Nobody does that! || BEAT THE CAPTAIN! || Captain down! Wow! || Nobody beats the captain!", "...Impressive. || ...Impressive. || ...Notable. || ...Well done.", "Okay, that was cool. || That was cool. || Okay. Cool. || Nice.")
                                                   : c.T("I'll help you mop. We're a team. || I'll mop too. Team. || Let me help mop. || We'll clean together.", "Mop race! Go! || Mop race! || Mopping contest! || Race you! Mops!", "...Give me the other mop. || ...Other mop. || ...Hand it over. || ...I'll take that one.", "Mopping together. Very glamorous. || Glamorous mopping. || So romantic. Mops. || Mopping date. Classy."));
                        t.On("gy_duel", () => { t.Her(c.Her, 3, shot ? 1 : 4); t.Rep += shot ? 2 : 0; });
                    });
                    var her = c.Sporty || Roll(40, 55);
                    o2.Add("Let her take the challenge", "gy_her", () =>
                    {
                        b.Narr(her ? "She steps up. Swish. Swish. Swish. The captain stares, then laughs and shakes her hand." : "She misses all three, then bursts out laughing with the captain. They're friends by the time the mop comes out.");
                        Her(c, b, her ? Feel.Laugh : Feel.Smile, her ? c.T("I did it...! Did you see? || I did it! Did you see? || I scored...! You saw? || Look! I did it!", "THREE FOR THREE! Did you SEE that?! || THREE OF THREE! See?! || Perfect! Did you see?! || All three! Wow!", "...As expected. || ...As expected. || ...Naturally. || ...Of course.", "Easy. || Easy. || Simple. || Too easy.")
                                                                     : c.T("I missed everything... but that was fun. || Missed all... but fun. || Zero... but I enjoyed it. || I missed... it was still fun.", "I missed them ALL! Ahaha! Best day ever! || Missed ALL! Best day! || Zero! Haha! Fun! || Total miss! Great day!", "...Next time. || ...Next time. || ...Later. || ...Again sometime.", "I wasn't trying. Obviously. || Not trying. Obviously. || Wasn't serious. || I held back. Clearly."));
                        t.On("gy_her", () => t.Her(c.Her, her ? 6 : 4, 3));
                    }, 1.3f);
                    o2.If(c.Famous, "Sign the captain's notebook", "gy_famous", () =>
                    {
                        b.Narr("The captain squints at you. \"Wait... you're that guy. Sign my notebook. My little sister won't believe it.\"");
                        Her(c, b, Feel.Surprised, c.T("You're famous...? || You're famous...? || People know you...? || You're a celebrity...?", "AUTOGRAPHS?! Since when?! || AUTOGRAPHS?! || Since when?! Famous! || You're a STAR?!", "...Unbelievable. || ...Unbelievable. || ...Absurd. || ...Really?", "Look at you, a celebrity. || A celebrity. Look at you. || Famous now? || Star status. Wow."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("The club time trial is tomorrow... Could you time me after class? || Time trial tomorrow... time me after class? || Could you hold the stopwatch for me later? || Would you time my run after class?", "Time trial tomorrow! Time me! I'm gonna break my record! || Time trial! Time me! Record time! || Stopwatch duty! Record breaker! || Time me! New record!",
                    "I need someone with a stopwatch and no opinions. After class. || Stopwatch. No opinions. After class. || Time me. Silently. Later. || A timer. You. After class.", "Time me after class. If I break my record, you buy me a drink. || Time me. Record means you buy drinks. || Beat my record, you pay. Deal? || Record = your treat.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The track at sunset. The tents from the last meet are still up, flapping in the wind.");
                    b.Narr("She ties her shoes twice, then three times, then stretches like her life depends on it.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Okay... I'm ready. I think. Maybe. || Ready... I think... maybe. || Okay... here goes... || I-I'm ready...", "Okay! Let's GO! Timer ready?! || GO! Timer?! || Let's go! Ready?! || Start! Now!", "Ready. Don't miss the start. || Ready. Catch the start. || Ready. Precisely. || Start on time.", "Watch closely. This'll be good. || Watch closely. || Pay attention. || Eyes on me."));
                    var o = new Opts();
                    var best = Roll(c.Sporty ? 60 : 30, 55);
                    o.Keep("Time her and call the splits", "tk_time", () =>
                    {
                        b.Narr("You shout her splits at every corner. On the last straight she gives everything.");
                        b.Narr(best ? "She crosses the line. You look at the phone. A new personal best by three tenths of a second." : "She crosses the line. Two tenths slower than her best. She lies down on the track and stares at the sky.");
                        Her(c, b, best ? Feel.Laugh : Feel.Pout, best ? c.T("Really?! I did it...! || Really?! I did it! || I made it...! || My best...!", "PERSONAL BEST! AAAH! || PERSONAL BEST! || RECORD! AAAH! || New best! Yes!", "...Good. Very good. || ...Good. Very good. || ...Excellent. || ...Improvement.", "Told you. Drink's on you. || Told you. Your treat. || Drinks on you. || Pay up.")
                                                                         : c.T("...Again. Once more. Please. || ...Again. Please. || ...One more try... || ...Let me retry.", "ARGH! Again! I can do it! || ARGH! Again! || Again! I can! || Retry! Now!", "...Unacceptable. Again. || ...Unacceptable. Again. || ...Insufficient. Repeat. || ...No. Again.", "The wind was against me. Obviously. || Headwind. Obviously. || The track's slow. || Bad wind."));
                        t.On("tk_time", () => t.Her(c.Her, best ? 6 : 4, 2));
                    });
                    var keep = Roll(G.Str, 55);
                    o.Add("Pace her by running alongside [STR]", "tk_pace", () =>
                    {
                        b.Narr(keep ? "You keep up for a full lap. Then a second. She pushes harder just to beat you." : "You keep up for exactly half a lap. Then your legs file a formal complaint. She waves as she passes you.");
                        Her(c, b, Feel.Laugh, keep ? c.T("You're fast...! That helped so much! || You're fast! That helped! || Running with you helped so much! || You kept pace... thank you!", "That was AWESOME! Race me again! || AWESOME! Again! || Race me again! || So fun! Again!", "...You kept up. Respect. || ...You kept up. Respect. || ...Decent pace. || ...Not bad.", "Not bad for a non-athlete. || Decent for a non-athlete. || Not bad, civilian. || For an amateur, fine.")
                                                   : c.T("Are you alive...? Here, water. || Alive? Water... || Are you okay? Drink... || Here... water...", "AHAHA! You're crawling! || You're crawling! Haha! || HAHA! Dying! || Lol! You're done!", "...Breathe. In. Out. || ...Breathe. || ...Inhale. Exhale. || ...Slowly.", "Need a stretcher? || Stretcher? || Medic? || Need an ambulance?"));
                        t.On("tk_pace", () => { t.Her(c.Her, 4); t.Str += 1; });
                    });
                    var start = Roll(G.Int, 50);
                    o.Add("Study her start [INT]", "tk_start", () =>
                    {
                        b.Me("You lift your head too early. Stay low for three steps, then come up.");
                        b.Narr(start ? "She tries it. The first ten meters are clearly faster. She looks at you like you performed a miracle." : "She tries it and nearly trips. The second time, it's a little better.");
                        Her(c, b, Feel.Smile, c.T("How did you notice that? Thank you... || You noticed that? Thank you... || How did you see that? Thanks... || You're observant... thank you.", "Whoa, you're like a real coach! || Real coach vibes! || Coach! Wow! || You're like a pro coach!", "...Good eye. || ...Good eye. || ...Sharp. || ...Observant.", "You were watching closely, huh. || Watching closely, huh. || Staring at me? Useful. || Eyes on me the whole time?"));
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
                        Her(c, b, Feel.Laugh, c.T("Ahaha... are you okay in there? || Are you okay in there? Hehe... || Ahaha... can you get out? || Oh no... hehe... okay?", "BURRITO! Hold still, I'm taking a picture! || BURRITO! Photo! || Wrapped up! Picture! || Burrito boy! Snap!", "...Heroic. Ridiculous, but heroic. || ...Heroic. Absurd. || ...Brave. Silly. || ...Ridiculous heroics.", "Best catch of the season. || Catch of the season. || Best save ever. || Highlight reel."));
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
                        Her(c, b, Feel.Worried, c.Sporty ? c.T("Sometimes I wonder why I run so much. I think... it's the only time I don't worry. || I run because it's the only time I don't worry. || When I run, my worries fade... || Running quiets my fears...", "When I run, my head goes quiet! That's why I love it! || Running makes my head quiet! || My brain shuts up when I run! || Running = peace!", "Running is simple. Everything else isn't. || Running is simple. The rest isn't. || Running. Uncomplicated. || Simple. Unlike life.", "Running is the only thing I'm sure I'm good at. || The only thing I'm sure I'm good at. || Running. My one certainty. || Running's my thing. Only one.")
                                                        : c.T("I'm not really sporty... I just wanted to see what it's like to try hard at something. || I'm not sporty... I wanted to try hard at something. || I just wanted to give my all once... || I wanted to know what effort feels like.", "I'm not even in a sports club! I just wanted to feel cool! || Not even sporty! Wanted to be cool! || I'm no athlete! Just wanted cool points! || Wanted to feel cool!", "I wanted to know if I could do it. That's all. || Testing myself. That's all. || I wanted to see if I could. || Curiosity. Self-test.", "I wanted to prove I could. Mostly to myself. || Proving it to myself. || To show myself I can. || Mostly for me."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("I have a make-up test tomorrow and I don't understand anything... Could you help me? Please? || Make-up test tomorrow... I don't get anything. Help me? || I'm lost for tomorrow's test... please help? || Could you tutor me? The test's tomorrow...", "Make-up test tomorrow and my brain is EMPTY! Save me! || Make-up test! Empty brain! Help! || Test tomorrow! Save me! || Brain empty! Test! Help!",
                    "Make-up test tomorrow. I need help. Don't make me say it twice. || Make-up test. Need help. Once. || Tomorrow. Test. Assist. || I need help. Saying it once.", "I need a tutor. You're the least boring option. Congratulations. || Tutor wanted. You're least boring. || You're my tutor. Congrats. || Least boring option: you.")),
                Write = (c, b, t) =>
                {
                    b.Narr("An empty lecture hall, desks still in exam rows from this morning. Her textbook is open to a page covered in red marks.");
                    Callback(c, b);
                    Her(c, b, Feel.Worried, c.T("I read it ten times... it just doesn't go in. || Ten times... it won't stick. || I keep reading... nothing. || It won't go into my head...", "I don't get ANY of it! The letters are dancing! || None of it! Letters dancing! || Can't read! Swirly words! || Zero understanding!", "I understand the words. Not the math. || Words fine. Math no. || I read it. Don't get it. || The math escapes me.", "This page is my enemy. || This page is my enemy. || I hate this page. || Nemesis: page 34."));
                    var o = new Opts();
                    var click = Roll(G.Int, 55);
                    o.Keep("Go through it step by step [INT]", "sd_steps", () =>
                    {
                        b.Narr(click ? "You go line by line. Halfway through, she grabs the pencil from you. \"Wait. Wait. I get it.\"" : "You go line by line. Halfway through, you realize you don't get it either. You stare at each other and burst out laughing.");
                        Her(c, b, click ? Feel.Laugh : Feel.Laugh, click ? c.T("I get it...! I actually get it! || I get it! Really! || It makes sense now...! || I understand!", "IT MAKES SENSE! I'm a genius! You're a genius! || IT CLICKED! Genius! || I GET IT! || Yes! Brain works!", "...That's it. That's all it was. || ...That's all it was. || ...Simple. || ...Understood.", "Okay. That was almost too easy. || Too easy. || Almost insulting. || Easy.")
                                                                         : c.T("We're both lost... ahaha... || Both lost... ahaha... || We're both confused... hehe. || Neither of us knows...", "We're DOOMED! Together! || DOOMED! Together! || We're toast! || Failing together!", "...The blind leading the blind. || ...Blind leading blind. || ...Hopeless pair. || ...Two lost souls.", "Great tutor. Five stars. || Five-star tutor. || Great help. Really. || Best tutor ever. Not."));
                        t.On("sd_steps", () => { t.Her(c.Her, click ? 6 : 3, 2); t.Int += 1; });
                    });
                    var guide = S.Count("study_guide") > 0 ? "study_guide" : S.Count("notebook") > 0 ? "notebook" : "";
                    o.If(guide != "", "Give her your " + ItemName(guide), "sd_guide", () =>
                    {
                        b.Narr("You slide your " + ItemName(guide) + " across the desk.");
                        Her(c, b, Feel.Shy, c.T("You'd give me this...? Thank you, really. || For me? Thank you... || You'd give this? Thanks... || This is so kind...", "WHOA! This is gonna save my life! || Lifesaver! || WHOA! Saved! || This rocks!", "...Useful. I owe you. || ...Useful. Owed. || ...I owe you. || ...Helpful.", "Bribing me with stationery? It works. || Stationery bribe. Works. || Bribery. Accepted. || Pens? I'm sold."));
                        t.On("sd_guide", () => { t.Use(guide); t.Her(c.Her, c.Bookish ? 8 : 5, 2); });
                    }, 2f);
                    var catchy = Roll(G.Sens, 45);
                    o.Add("Turn the formulas into a song [SEN]", "sd_song", () =>
                    {
                        b.Narr(catchy ? "Three verses and a chorus. By the end she's singing it on her own, tapping the beat on the desk." : "The song is objectively terrible. She remembers every word of it anyway, mostly out of trauma.");
                        Her(c, b, Feel.Laugh, c.T("It's stuck in my head... in a good way. || It's stuck in my head... nicely. || I can't stop humming it... || It's catchy...", "I'm gonna sing it during the test! Out loud! || Singing it in the test! || Out loud in class! || It's my anthem now!", "...I hate that it works. || ...It works. Annoyingly. || ...Effective. Irritating. || ...Ugh. Memorable.", "That's the worst song I've ever heard. I'll never forget it. || Worst song. Unforgettable. || Terrible. Burned into my brain. || Awful. I'll remember forever."));
                        t.On("sd_song", () => t.Her(c.Her, 5));
                    });
                    o.If(c.Snack != "", "Snack break first", "sd_snack", () =>
                    {
                        b.Narr("You split your " + ItemName(c.Snack) + ". Ten minutes of not thinking about math.");
                        Her(c, b, Feel.Smile, c.T("I feel better already... thank you. || Better already... thanks. || I feel better... thank you. || Thank you... much better.", "Brain fuel! Okay, I'm ready now! || Brain fuel! Ready! || Snack power! || Energy! Let's go!", "...Good idea. || ...Good idea. || ...Smart. || ...Wise.", "Smart. Feed the student first. || Feed the student. Smart. || Fuel first. Clever. || Smart tutor."));
                        t.On("sd_snack", () => { t.Use(c.Snack); t.Her(c.Her, 4, 2); });
                    });
                    o.Play(b, "sd_1");

                    b.Narr("The door slides open. The math professor, holding a stack of papers: \"Oh, good. Volunteers for a practice quiz.\"");
                    var o2 = new Opts();
                    var quiz = Roll(G.Int, 50);
                    o2.Keep("Take the quiz together", "sd_quiz", () =>
                    {
                        b.Narr(quiz ? "Ten questions. She gets eight right. The professor raises an eyebrow and writes something in his notebook." : "Ten questions. She gets five. \"Better than last time,\" the professor says. Coming from him, that's a compliment.");
                        Her(c, b, quiz ? Feel.Laugh : Feel.Smile, quiz ? c.T("Eight...! I've never gotten eight! || Eight! I've never gotten eight! || An eight...! First time! || Eight out of ten...!", "EIGHT! EIGHT OUT OF TEN! || EIGHT! || EIGHT! WOO! || Eight! Amazing!", "...Eight. Acceptable. || ...Eight. Acceptable. || ...Good. Eight. || ...Satisfactory.", "Eight. I'm a genius, apparently. || Genius, apparently. || Eight. I'm brilliant. || Eight. Naturally.")
                                                                       : c.T("Five... that's better than before, right? || Five... better than before, right? || Five... it's progress? || Five... I improved...", "Five! Halfway there! That counts! || Five! Halfway! || Half! Counts! || Five! Getting there!", "...Progress. || ...Progress. || ...Improvement. || ...Better.", "Five. Baby steps. || Baby steps. || Small wins. || Progress. Tiny."));
                        t.On("sd_quiz", () => { t.Her(c.Her, quiz ? 5 : 3, 2); if (quiz) t.Rep += 1; });
                    });
                    var bluff = Roll(G.Sens, 55);
                    o2.If(G.Sens >= 35, "Claim you're the course rep on official business [SEN]", "sd_bluff", () =>
                    {
                        b.Narr(bluff ? "\"Ah, of course. Carry on.\" The professor leaves. She stares at you in awe." : "\"The course rep is my niece.\" A long silence. You both take the quiz.");
                        Her(c, b, Feel.Laugh, bluff ? c.T("How did that work...? || How did that work...? || It worked...? || Wow... how?", "You're a criminal mastermind! || Criminal mastermind! || Genius plan! || Evil genius!", "...Dangerous. I respect it. || ...Dangerous. Respect. || ...Cunning. || ...Clever.", "Smooth. Very smooth. || Smooth. || Slick. || Very smooth.")
                                                    : c.T("His niece... ahaha... || His niece... hehe... || Ahaha... niece... || A niece... oh no...", "HIS NIECE! AHAHA! || NIECE! HAHA! || His NIECE?! || Niece! Lol!", "...We're doomed. || ...Doomed. || ...We're finished. || ...Hopeless.", "Worst bluff in history. || Worst bluff ever. || Historic failure. || Terrible lie."));
                        t.On("sd_bluff", () => t.Her(c.Her, 4));
                    });
                    o2.Add("Run for it", "sd_run", () =>
                    {
                        b.Narr("You grab her bag and your bag and bolt out the back door. You hear him shouting your names down the hall.");
                        Her(c, b, Feel.Laugh, c.T("We ran away from a professor...! I've never done that! || We ran from a professor! First time! || I've never run from a professor...! || That was so scary... and fun!", "WE'RE OUTLAWS NOW! || OUTLAWS! || We're fugitives! || Rebels!", "...That was stupid. Let's do it again sometime. || ...Stupid. Again sometime. || ...Foolish. Fun. Repeat. || ...Reckless. Let's do it again.", "My first crime. With you. Memorable. || My first crime. Memorable. || Partners in crime. || First offense. With you."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("This Sunday... would you come with me to the old temple town? I've always wanted to see it. || This Sunday... the old temple town? With me? || Would you go to the temple town with me Sunday? || Sunday... I'd love to see the temple town with you.", "Sunday! Temple town! Day trip! You and me! Say yes! || Sunday! Temple town! Say yes! || Day trip! Sunday! Come! || Temple town! You and me!",
                    "I'm going to the temple town on Sunday. You can come. If you want. || Temple town Sunday. Join if you like. || Sunday. Temple town. You may come. || I'm going Sunday. Come, if you want.", "Sunday trip to the temple town. You're coming. I already bought the tickets. Kidding. Mostly. || Sunday trip. Tickets bought. Kidding. Mostly. || Temple town. Sunday. You're coming. || Sunday. You. Me. Temples. Done.")),
                Write = (c, b, t) =>
                {
                    b.Narr("Sunday morning. The train rattles out of the city, fields and rivers sliding past the window.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("I'm so excited... I barely slept. || I couldn't sleep, I was so excited... || I've been looking forward to this so much... || I'm so happy we're going...", "Look! A river! Look! A cow! || A river! A cow! Look! || Mountains! Sheep! Wow! || Look out the window! Everything!", "...The view's nice. || ...Nice scenery. || ...Good view. || ...Pleasant.", "Window seat's mine, by the way. || Window seat is mine. || I call the window. || The window's taken. By me."));
                    var o = new Opts();
                    o.Keep("Play cards for the window seat", "tp_cards", () =>
                    {
                        b.Narr("Old maid, best of five. She cheats. Very obviously. You pretend not to notice.");
                        Her(c, b, Feel.Laugh, c.T("I won...! Um... I didn't cheat. ...Maybe a little. || I won! ...I didn't cheat. Much. || Yay... I won... maybe I peeked. || I won...! Honestly. Mostly.", "I WIN! Window seat is MINE! || WIN! Window's MINE! || Victory! Window! || Mine! Window seat!", "...I won fairly. Mostly. || ...Fair win. Mostly. || ...I won. Cleanly. Nearly. || ...Earned. Almost.", "You let me win. I saw that. || You let me win. Saw it. || Throwing the game? I noticed. || You went easy. Cute."));
                        t.On("tp_cards", () => t.Her(c.Her, 4));
                    });
                    o.If(S.Count("bento") > 0, "Share your deluxe bento", "tp_bento", () =>
                    {
                        b.Narr("You open the bento on your knees. Rolled omelet, karaage, rice shaped like a little bear.");
                        Her(c, b, Feel.Laugh, c.T("It's so cute... I can't eat the bear. || So cute... I can't eat the bear. || A bear...! Too cute to eat. || I can't bite him...", "BEAR RICE! I'm eating the ears first! || BEAR RICE! Ears first! || Bear bento! Nom! || Ears! Yum!", "...Good bento. || ...Good bento. || ...Well made. || ...Tasty.", "Bear-shaped rice. You have a soft side. || Bear rice. Soft side exposed. || Cute bento. You're sweet. || A bear? Softie."));
                        t.On("tp_bento", () => { t.Use("bento"); t.Her(c.Her, c.Foodie ? 8 : 5); });
                    }, 2f);
                    o.If(G.Int >= 30, "Plan the route [INT]", "tp_plan", () =>
                    {
                        b.Narr("You draw a map on a napkin: the temple, the old street, the best mochi shop, the station, in the perfect order.");
                        Her(c, b, Feel.Smile, c.T("You thought of everything... || You planned everything... || You thought of it all... || So thoughtful...", "You're like a tour guide! || Tour guide! || You're a pro planner! || Travel expert!", "...Efficient. Good. || ...Efficient. || ...Organized. Good. || ...Well planned.", "Organized. I like it. || Organized. Attractive. || I like a planner. || Good planning. Nice."));
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
                    Her(c, b, Feel.Surprised, c.T("It's even more beautiful than in the pictures... || Prettier than the pictures... || It's even lovelier in person... || So beautiful...", "WHOA! It's like a movie! || WHOA! Movie scene! || Amazing! Like a film! || So epic!", "...Worth the trip. || ...Worth it. || ...Good trip. || ...Worth the travel.", "Okay. I'm impressed. || Impressed. || Okay. Wow. || I'm impressed."));
                    var o2 = new Opts();
                    var luck = Random.Range(0, 3);
                    o2.Keep("Draw omikuji fortunes", "tp_omikuji", () =>
                    {
                        b.Narr(luck == 0 ? "You both draw Great Blessing. The shrine maiden says that almost never happens." : luck == 1 ? "She draws Great Blessing. You draw Great Curse. She laughs for a full minute." : "You both draw Curse. You tie them to the tree together, as the rules say, to leave the bad luck behind.");
                        Her(c, b, Feel.Laugh, c.T("This is a good memory, no matter what it says. || A good memory, whatever it says. || It's a nice memory anyway. || I'll remember this regardless.", "Fortunes! Ahaha! Best part! || Fortunes! Best part! || Haha! Fortunes! || Fortune time! Fun!", "...It's just paper. ...Let me read yours again. || ...Paper. ...Read yours again. || ...Meaningless. ...Show me yours. || ...Just paper. ...Again?", "Fate is funny. || Fate's funny. || Fate has jokes. || Destiny. Amusing."));
                        t.On("tp_omikuji", () => t.Her(c.Her, 4));
                    });
                    o2.If(S.Money >= 800, "Buy her a lucky charm (¥800)", "tp_charm", () =>
                    {
                        b.Narr("A little charm for good health, with a tiny bell. You hand it to her.");
                        Her(c, b, Feel.Shy, c.T("For me...? I'll hang it on my bag. Thank you. || For me? I'll hang it on my bag. || A charm... I'll keep it on my bag. || Thank you... I'll carry it.", "It jingles! I love it! || It jingles! Love it! || Jingly! Yay! || Ding ding! Love it!", "...Thank you. I'll keep it. || ...Thank you. Kept. || ...I'll keep it. || ...Thanks.", "A souvenir from you? I'll keep it. || A souvenir from you? Kept. || I'll treasure it. Maybe. || From you? Fine. Mine."));
                        t.On("tp_charm", () => { t.Money -= 800; t.Her(c.Her, 6, 3); });
                    }, 1.3f);
                    var climb = Roll(G.Str, 55);
                    o2.Add("Climb all the stairs to the top [STR]", "tp_climb", () =>
                    {
                        b.Narr(climb ? "Three hundred steps. At the top, the whole valley spreads out below you, roofs and rivers and mountains." : "Three hundred steps. At step two hundred you sit down and question every decision you ever made.");
                        Her(c, b, Feel.Smile, climb ? c.T("Look at this view... I'll never forget it. || This view... unforgettable. || I'll always remember this... || So beautiful up here...", "WE MADE IT! KING OF THE MOUNTAIN! || KING OF THE MOUNTAIN! || WE DID IT! || Summit! Woo!", "...Worth it. || ...Worth it. || ...Good climb. || ...Rewarding.", "Okay. That was worth it. || Worth it. || Fine. Worth it. || Okay. Good view.")
                                                    : c.T("Come on, just a little more... I'll wait for you. || A little more... I'll wait. || Almost there... come on. || I'll wait for you... just a bit more.", "Come on! I'll carry your bag! Not you though! || Come on! I'll carry your bag! || Go! Bag's mine! You walk! || Almost! Push!", "...Get up. We're close. || ...Up. Close now. || ...Keep going. || ...Nearly there.", "Need me to push you? || Need a push? || Shall I drag you? || Carry you? No."));
                        t.On("tp_climb", () => { t.Her(c.Her, 4, 2); t.Str += 1; });
                    });
                    o2.Add("Write wishes on an ema", "tp_ema", () =>
                    {
                        b.Narr("Wooden plaques, a marker each. She writes hers fast and hangs it high.");
                        Her(c, b, Feel.Shy, c.T("I wished we'd stay friends for a long time. ...Don't laugh. || I wished to stay friends forever... don't laugh. || I wished we'd always be close... || My wish was about us... don't laugh.", "I wished for ten more trips like this one! || Ten more trips! || More trips like this! || Infinite trips! That's my wish!", "...I wished for something about my family. Private. || ...Family wish. Private. || ...Something for my family. || ...Private wish.", "I wished for something. You'll find out when it comes true. || You'll know when it comes true. || A secret wish. You'll see. || You'll find out. Someday."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("I'm practicing for a tea ceremony demonstration... Would you be my guest? I need practice. || Tea ceremony practice... would you be my guest? || I need a guest for tea practice... please? || Could you be my practice guest?", "Tea ceremony practice! I need a guest! You! You're the guest! || Tea ceremony! You're the guest! || Guest needed! You! || Tea time! Be my guest!",
                    "I need a guest for tea ceremony practice. Sit still and be polite. That's all. || Tea practice. Sit and be polite. || I need a guest. Behave. || Tea ceremony. Sit still.", "Be my guest for tea ceremony practice. Try not to embarrass yourself. || Be my guest. Don't embarrass yourself. || Tea practice. Behave. || Guest duty. No jokes.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The traditional room behind the club building: tatami, sliding doors, a small garden with a stone lantern.");
                    b.Narr("She kneels very straight and whisks the tea with a seriousness you have never seen on her face.");
                    Callback(c, b);
                    Her(c, b, Feel.Serious, c.T("Please... take your seat. || Please... sit. || Here, please... || Your seat...", "Okay! Very serious now! Shhh! || Serious now! Shh! || Quiet! Ceremony! || Shh! Serious mode!", "Sit. Quietly. || Sit. Quietly. || Seat. Silence. || Sit.", "Sit. And no jokes. || Sit. No jokes. || Seat. Behave. || Sit. Be good."));
                    var o = new Opts();
                    var polite = Roll(G.Int, 50);
                    o.Keep("Follow the etiquette exactly [INT]", "te_rules", () =>
                    {
                        b.Narr(polite ? "You bow, turn the bowl twice, drink in three sips and praise the tea. She nods, almost proud." : "You turn the bowl the wrong way. Then the other wrong way. She bites her lip very hard.");
                        Her(c, b, polite ? Feel.Smile : Feel.Laugh, polite ? c.T("Perfect... You studied this? || Perfect... you studied? || You know this...? || So graceful...", "Whoa! You're a natural! || Natural! || Whoa! Pro! || You're good!", "...Correct. Surprisingly. || ...Correct. Surprising. || ...Accurate. || ...Proper.", "Huh. You actually know this. || You know this. Huh. || Studied? Impressive. || Cultured. Who knew.")
                                                                           : c.T("Ah... it's okay... ahaha... sorry... || It's okay... hehe... sorry... || Ah... that's fine... ahaha... || It's alright... hehe...", "AHAHA! That's not how... AHAHA! || HAHA! Not like that! || AHAHA! Wrong! || Ahaha! Oops!", "...Wrong. Very wrong. || ...Wrong. Very. || ...Incorrect. || ...No.", "That was a disaster. I loved it. || Disaster. Loved it. || Catastrophe. Delightful. || Awful. Perfect."));
                        t.On("te_rules", () => { t.Her(c.Her, polite ? 5 : 3, 2); if (polite) t.Int += 1; });
                    });
                    o.If(S.Count("daifuku") > 0, "Offer your strawberry daifuku as the sweet", "te_daifuku", () =>
                    {
                        b.Narr("Tea needs a sweet. You produce a strawberry daifuku from your bag, a little squashed.");
                        Her(c, b, Feel.Laugh, c.T("That's perfect...! How did you know? || Perfect! How did you know? || Ah, daifuku...! You knew? || Just right...!", "DAIFUKU! The perfect tea sweet! || DAIFUKU! Perfect! || Yes! Daifuku! || Best sweet!", "...Correct choice. || ...Correct. || ...Right choice. || ...Appropriate.", "Prepared, aren't you. || Prepared. || Studied up? || Ready, aren't you."));
                        t.On("te_daifuku", () => { t.Use("daifuku"); t.Her(c.Her, c.Sweet ? 8 : 5); });
                    }, 2.5f);
                    var compliment = Roll(G.Sens, 45);
                    o.Add("Compliment the tea bowl [SEN]", "te_bowl", () =>
                    {
                        b.Me("This bowl is beautiful. The glaze looks like a lake in winter.");
                        Her(c, b, compliment ? Feel.Shy : Feel.Laugh, compliment ? c.T("My grandmother made it... thank you. || My grandmother made it... thank you. || Grandma's bowl... thanks. || My grandma made this one...", "Right?! It's my favorite one! || My favorite! || Right?! Love it! || Best one!", "...You have an eye. Good. || ...Good eye. || ...Discerning. || ...You have taste.", "Poetic. I didn't expect that. || Poetic. Unexpected. || Romantic. Surprising. || Lyrical. Nice.")
                                                                                 : c.T("...It's from a hundred-yen shop. But thank you. || ...Hundred-yen shop. But thanks. || ...It's cheap. Thank you, though. || ...Hehe, it's from a discount shop.", "It's from the hundred-yen shop! Ahaha! || Hundred-yen shop! Haha! || Cheap! Ahaha! || Discount store! Lol!", "...Hundred yen. || ...Hundred yen. || ...Discount. || ...Cheap.", "It's a hundred-yen bowl, you poet. || Hundred-yen bowl, poet. || Bargain bin, Shakespeare. || Cheap bowl, romantic."));
                        t.On("te_bowl", () => t.Her(c.Her, compliment ? 5 : 3));
                    });
                    o.Add("Ask her why she likes this", "te_why", () =>
                    {
                        Her(c, b, Feel.Smile, c.T("Because everything slows down. In here, nothing is in a hurry. || Everything slows down here. Nothing hurries. || Time is gentle here... || In here, nothing rushes...", "Because it's the only time I'm quiet! Ahaha! || I'm quiet only here! Haha! || My only quiet time! || The one time I'm silent!", "Because it's precise. Every movement has a reason. || Precision. Every motion has purpose. || Exactness. I like it. || Purposeful movement.", "Because people think I can't be serious. I can. || People think I can't be serious. I can. || Proving I can be serious. || I'm capable of serious."));
                        t.On("te_why", () => t.Her(c.Her, 3, 5));
                    });
                    o.Play(b, "te_1");

                    b.Narr("Twenty minutes of kneeling. You can no longer feel your legs. You're not sure you still have legs.");
                    var o2 = new Opts();
                    o2.Keep("Stand up anyway", "te_stand", () =>
                    {
                        b.Narr("You stand. Your legs do not. You fold onto the tatami like a dropped puppet.");
                        Her(c, b, Feel.Laugh, c.T("Ahaha... I'm sorry, I'm sorry... are you okay? || Sorry, sorry... are you okay? || Ahaha... are you alright? || Oops... hehe... sorry!", "AHAHAHA! You fell like a tree! || Fell like a tree! || TIMBER! Haha! || Down you go! Ahaha!", "...Pfft. Dignified. || ...Dignified. Pfft. || ...Graceful. Not. || ...Elegant fall.", "Timber. || Timber. || Down. || Splat."));
                        t.On("te_stand", () => t.Her(c.Her, 4));
                    });
                    var endure = Roll(G.Str, 60);
                    o2.Add("Endure with dignity [STR]", "te_endure", () =>
                    {
                        b.Narr(endure ? "You sit perfectly still until the end. She looks at you with new respect." : "You endure for exactly one more minute, then tip over sideways, very slowly.");
                        Her(c, b, Feel.Smile, endure ? c.T("You lasted the whole time... || You lasted the whole time... || You didn't move at all... || You held on...!", "Iron legs! Impressive! || Iron legs! || Steel knees! || Unbreakable!", "...Discipline. I respect that. || ...Discipline. Respect. || ...Endurance. Good. || ...Admirable.", "Okay, you win this round. || You win this round. || Fine. Point to you. || Okay. You won.") : c.T("Ahaha... you tried so hard. || You tried so hard... hehe. || Ahaha... good effort. || So close... hehe.", "SLOW MOTION FALL! AHAHA! || SLOW FALL! || Slow-motion tumble! || Timber! Slowly!", "...Almost. || ...Nearly. || ...Close. || ...Almost.", "So close. || So close. || Nearly. || Almost had it."));
                        t.On("te_endure", () => { t.Her(c.Her, 4); if (endure) t.Str += 1; });
                    });
                    o2.Add("Admit your legs are dead", "te_admit", () =>
                    {
                        b.Me("I can't feel my legs.");
                        Her(c, b, Feel.Laugh, c.T("...Me neither. I never can. Ahaha... || Me neither... I never can. || Mine fell asleep too... hehe. || I can't either...", "Me neither! Mine died ten minutes ago! || Mine died ages ago! || My legs are dead! || Same! Numb!", "...Neither can I. Don't tell anyone. || ...Neither. Secret. || ...Same. Don't share. || ...Mine too. Quiet.", "Honestly? Mine too. Nobody can. || Mine too. Nobody can. || Same. Everyone fakes it. || Honestly? Same."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("The old street has its wishing bamboo up... Would you write a wish with me? || The wishing bamboo is up... write a wish with me? || Would you write a wish with me? || The bamboo's out... let's write wishes?", "The wishing bamboo is up! Let's write wishes! Big ones! || Wishing bamboo! Big wishes! || Wish time! Let's go! || Wishes! Biggest ones!",
                    "They put up the wishing bamboo on the old street. I want to write one. Come. || Wishing bamboo. I'll write one. Come. || Bamboo's up. Come write. || Old street. Wishes. Join me.", "Wishing bamboo. You and me. Let's see what you wish for. || Wishing bamboo. Show me your wish. || Let's see what you want. || Wishes. Yours. Curious.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The old street near the station: bamboo branches heavy with paper strips of every color, lanterns swaying overhead.");
                    b.Narr("A little table with pens and blank strips. Hundreds of wishes flutter in the warm wind.");
                    Callback(c, b);
                    Her(c, b, Feel.Smile, c.T("What will you wish for...? || What will you wish...? || What's your wish...? || Hmm... your wish?", "Okay! Biggest wish wins! || Biggest wish wins! || Big wish contest! || Go big!", "Think carefully. Wishes are serious. || Carefully. Wishes matter. || Think first. || Seriously.", "Make it a good one. || Make it good. || Good one, please. || Don't waste it."));
                    var o = new Opts();
                    o.Keep("'Good grades for both of us.'", "ws_grades", () =>
                    {
                        Her(c, b, Feel.Laugh, c.T("Practical... and kind. That's very you. || Practical and kind. So you. || That's very you... sweet. || Sensible... and nice.", "Boring! ...Thanks for including me though! || Boring! ...Thanks for me though! || Lame! ...But thanks! || Yawn! ...Sweet, though!", "...Sensible. || ...Sensible. || ...Practical. || ...Reasonable.", "Wasting a wish on grades? Classic you. || Grades? Classic you. || Studying wish? Typical. || So predictable."));
                        t.On("ws_grades", () => { t.Int += 1; t.Her(c.Her, 3); });
                    });
                    o.If(c.Tier >= 1, "'Let her dream come true.'", "ws_dream", () =>
                    {
                        Her(c, b, Feel.Shy, c.T("You wished... for me? ...Thank you. || For me...? Thank you. || You wished for me...? || Me...? That's so kind...", "For ME?! You used your wish on me?! || ME?! Your wish?! || For ME?! Wow! || You used it on ME?!", "...You didn't have to do that. || ...Unnecessary. || ...You shouldn't have. || ...Why?", "...That's not fair. Now I have to wish for you. || Unfair. Now I owe a wish. || Now I have to wish for you. || Cheater. My turn."));
                        t.On("ws_dream", () => t.Her(c.Her, 5, 5, 1));
                    }, 1.5f);
                    o.Add("'More days like this one.'", "ws_days", () =>
                    {
                        Her(c, b, Feel.Smile, c.T("...Me too. || ...Same. || ...Me too. || ...I wish that too.", "YES! Same! A hundred more! || SAME! A hundred more! || Yes! More! || Same! Forever!", "...Fine wish. || ...Good wish. || ...Acceptable. || ...Fine.", "Good answer. || Good answer. || Correct. || Smart wish."));
                        t.On("ws_days", () => t.Her(c.Her, 4, 2));
                    });
                    o.If(c.Infamous, "'A better reputation. Please. Anything.'", "ws_rep", () =>
                    {
                        Her(c, b, Feel.Laugh, c.T("Ahaha... it's not that bad. Okay, it's a little bad. || Not so bad... okay, a bit. || Hehe... it's okay... kind of. || Ahaha... it's fine. Mostly.", "AHAHA! The gods heard you sobbing! || The gods heard you crying! || Ahaha! Desperate! || Hahaha! Begging!", "...They're going to need a bigger miracle. || ...They'll need a miracle. || ...Big ask. || ...Optimistic.", "Desperate times. || Desperate times. || Begging the gods? || Pathetic. Cute."));
                        t.On("ws_rep", () => { t.Rep += 1; t.Her(c.Her, 3); });
                    }, 1.5f);
                    o.Play(b, "ws_1");

                    b.Narr("She writes hers with her back to you and ties it high, on her toes.");
                    var o2 = new Opts();
                    o2.Keep("Respect her privacy", "ws_respect", () =>
                    {
                        b.Narr("You look the other way. When you turn back, she's watching you.");
                        if (c.Trust >= 50)
                            Her(c, b, Feel.Shy, c.T("...You can read it. I don't mind. It says: 'I want to be braver.' || ...You can read it. 'I want to be braver.' || ...It's okay. It says: 'I want to be braver.' || ...Read it. 'I want to be braver.'", "...Fine, I'll tell you! 'Let me stop pretending everything's fine.' || ...Fine! 'Let me stop pretending everything's fine.' || ...Okay! 'Let me stop pretending I'm fine.' || ...It says: 'Let me stop faking fine.'", "...It says 'Let me be honest with people.' There. Now you know. || ...'Let me be honest with people.' Now you know. || ...It reads: 'Let me be honest.' || ...'Let me be honest with people.' There.", "...It says 'I want someone to see the real me.' Happy? || ...'I want someone to see the real me.' Happy? || ...'Someone to see the real me.' Satisfied? || ...It says I want to be seen. Really seen."));
                        else Her(c, b, Feel.Smile, c.T("Thank you for not looking. || Thanks for not looking. || Thank you for respecting it. || You didn't peek... thanks.", "Thanks! It's a secret wish! || Secret wish! Thanks! || Thanks! It's private! || Shh! Thanks!", "...Appreciated. || ...Appreciated. || ...Thank you. || ...Good.", "Smart move. || Smart. || Wise. || Good choice."));
                        t.On("ws_respect", () => t.Her(c.Her, 3, c.Trust >= 50 ? 7 : 4));
                    });
                    var sneaky = Roll(G.Sens, 55);
                    o2.Add("Try to read it [SEN]", "ws_peek", () =>
                    {
                        b.Narr(sneaky ? "You catch a glimpse as the wind turns it: \"I want to make one friend who stays.\"" : "You lean in. She grabs your face with both hands and turns it away. Firmly.");
                        Her(c, b, Feel.Pout, sneaky ? c.T("...You read it, didn't you. ...It's okay. You're one, you know. || ...You read it. It's okay. You're one. || ...You saw... it's fine. You're one of them. || ...You read it... yes, you're one.", "HEY! You read it! ...Well. You're kind of the answer anyway! || You read it! ...You're the answer anyway! || HEY! ...Well, it's about you! || Peeker! ...It's you, okay?!", "...You read it. ...Don't say anything. || ...Read it. ...Say nothing. || ...You saw. ...Silence. || ...Don't comment.", "...Snoop. ...Well. Now you know. || Snoop. Now you know. || Nosy. Well. Now you know. || Peeker. Fine. You know.")
                                                    : c.T("No peeking...! || No peeking! || Don't look...! || Not allowed!", "NO! Cheater! || Cheater! || NO! || Hands off!", "Absolutely not. || Absolutely not. || No. || Denied.", "Nice try. || Nice try. || Nope. || Cute attempt."));
                        t.On("ws_peek", () => t.Her(c.Her, sneaky ? 4 : 1, sneaky ? 3 : 0));
                    });
                    o2.If(c.Pal != null, "Swap wishes with her friend", "ws_swap", () =>
                    {
                        Pal(c, b, Feel.Shy, c.P("Mine says... 'Let us stay friends after graduation.' ...She's the 'us'.", "Mine says 'Best friends forever, for real this time!'", "...'Don't drift apart.' That's all.", "'Let her never get tired of me.' Don't tell her."));
                        Her(c, b, Feel.Surprised, c.T("...You wrote that? About me? || ...About me? You wrote that? || ...Is that for me...? || ...You wished that... about me?", "Wait, is that about ME?! || About ME?! || Is that ME?! || Me?! Really?!", "...Idiot. ...Me too. || ...Idiot. ...Same. || ...Fool. ...Me too. || ...Stupid. ...Likewise.", "...Okay, that one got me. || ...That got me. || ...Okay. Direct hit. || ...You win."));
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
                Invite = (c, i) => i.Say(c.Her, c.T("The class party is tomorrow and the room is still bare... Would you help me decorate? || The party's tomorrow and the room is empty... help me decorate? || Could you help decorate for tomorrow's party? || The room is so bare... would you help?", "Party tomorrow! Room's empty! HELP! || Party tomorrow! Empty room! HELP! || Decor emergency! Help! || Party! No decorations! Save me!",
                    "I'm in charge of decorations. Nobody else showed up. You're drafted. || Decorations. Nobody came. You're drafted. || I'm on decor. Alone. Help. || You're conscripted. Decorations.", "Decorating duty. You and me. Bring your artistic soul. || Decorating. You and me. Artistic soul required. || Decor duty. Bring your talent. || Help me decorate. Be creative.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The lecture hall after class, buried in orange paper, plastic spiders, pumpkins and a box labeled SPOOKY (DO NOT DROP).");
                    Callback(c, b);
                    Her(c, b, Feel.Laugh, c.T("Where do we even start...? || Where do we start...? || Um... where first? || So much to do...", "Okay! Pumpkins first! Then spiders! Then EVERYTHING! || Pumpkins! Spiders! EVERYTHING! || Go go go! || Decorate all of it!", "We have two hours. Move. || Two hours. Move. || Clock's ticking. || Begin. Now.", "Let's make it so scary the professor refuses to come in. || So scary the professor won't enter. || Terrifying. That's the goal. || Make it horrifying."));
                    var o = new Opts();
                    var art = Roll(G.Sens, 50);
                    o.Keep("Carve the pumpkins [SEN]", "dc_carve", () =>
                    {
                        b.Narr(art ? "Your pumpkin has a perfect, terrifying grin. She puts a candle in it and the whole corner glows." : "Your pumpkin looks less scary and more like it's having a very bad day. She names it Kevin.");
                        Her(c, b, Feel.Laugh, art ? c.T("It's so good...! It's the best one! || So good! The best! || Wow... it's wonderful! || That's amazing...!", "WHOA! That's terrifying! I love him! || Terrifying! Love him! || So creepy! Love it! || WHOA! Spooky!", "...That's actually good. || ...Actually good. || ...Skilled. || ...Not bad.", "Okay, artist. Not bad. || Not bad, artist. || Talent. Hm. || Nice work, Picasso.")
                                                  : c.T("Kevin... he looks so sad... I love him. || Kevin... so sad... I love him. || Poor Kevin... he's adorable. || Kevin looks so lonely... I love him.", "KEVIN! Kevin is the class mascot now! || KEVIN! Mascot! || Kevin forever! || Long live Kevin!", "...Kevin needs therapy. || ...Kevin needs help. || ...Kevin is troubled. || ...Concerning, Kevin.", "Kevin's my favorite. Don't tell the others. || Kevin's my favorite. Secret. || I love Kevin. Shh. || Kevin is the best. Don't tell."));
                        t.On("dc_carve", () => { t.Her(c.Her, 4); if (art) t.Sens += 1; });
                    });
                    var reach = Roll(G.Str, 50);
                    o.Add("Hang the streamers from the ceiling [STR]", "dc_hang", () =>
                    {
                        b.Narr(reach ? "You stand on a desk and hang the streamers corner to corner. She directs from below like a general." : "You stand on a chair. The chair has other plans. You come down with the streamers wrapped around you.");
                        Her(c, b, Feel.Laugh, reach ? c.T("It looks amazing...! || Looks amazing! || So pretty! || Wonderful...!", "PERFECT! Next one! || PERFECT! Next! || Great! More! || Yes! Next one!", "...Straight. Good. || ...Straight. Good. || ...Level. || ...Aligned.", "Nice work, ladder. || Nice, ladder. || Good work, tall guy. || Useful height.") : c.T("You look like a mummy... ahaha... || Mummy... hehe... || You're all wrapped up... ahaha... || A mummy... hehe!", "MUMMY COSTUME! You're done! You're ready! || MUMMY! Costume done! || You're a mummy! Ready! || Instant costume!", "...Efficient costume. || ...Efficient. || ...Practical costume. || ...Resourceful.", "A mummy. Fitting. || A mummy. Fitting. || Suits you. || Mummy. Appropriate."));
                        t.On("dc_hang", () => { t.Her(c.Her, 4); if (reach) t.Str += 1; });
                    });
                    var design = Roll(G.Int, 50);
                    o.If(G.Int >= 30, "Design the whole layout [INT]", "dc_plan", () =>
                    {
                        b.Narr(design ? "A haunted path from the door, a 'graveyard' of desks, cobwebs in the corners. The course rep will cry tears of joy." : "Your layout has a slight problem: nobody can reach the door.");
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
                    o2.Keep("Hide it in the professor's chair", "dc_prank", () =>
                    {
                        b.Narr(prank ? "Next morning the academic advisor screams so loud the class next door comes to look. It becomes a college legend." : "The professor finds it, sits it in the corner and gives it a desk. \"Your new classmate. He's smarter than you.\"");
                        Her(c, b, Feel.Laugh, c.T("We're going to be in so much trouble... ahaha... || So much trouble... ahaha... || We'll get caught... hehe... || Oh no... hehe...", "THIS IS THE BEST PRANK EVER! || BEST PRANK! || Legendary prank! || Prank of the year!", "...I didn't see anything. || ...Saw nothing. || ...I wasn't here. || ...No comment.", "We're partners in crime now. || Partners in crime. || Accomplices now. || Crime duo."));
                        t.On("dc_prank", () => { t.Her(c.Her, 4); t.Rep += prank ? 1 : -1; });
                    });
                    o2.Add("Make him the class mascot", "dc_mascot", () =>
                    {
                        b.Narr("You give him a party hat and a name tag: MR. BONES, HONORARY STUDENT.");
                        Her(c, b, Feel.Laugh, c.T("Mr. Bones... he's part of the class now. || Mr. Bones... part of the class. || Mr. Bones belongs here now... || He's one of us now...", "MR. BONES! He needs a seat! || MR. BONES! Seat him! || Desk for Mr. Bones! || Mr. Bones needs a chair!", "...He has better attendance than some people. || ...Better attendance than some. || ...More reliable than half the class. || ...Never absent.", "Mr. Bones for dean. || Mr. Bones for dean. || Vote Mr. Bones. || Mr. Bones, president."));
                        t.On("dc_mascot", () => t.Her(c.Her, 4, 2));
                    });
                    o2.If(c.Tone == Tone.Energetic || c.Tone == Tone.Teasing || c.Tier >= 2, "Use it to scare her", "dc_scare", () =>
                    {
                        b.Narr("While she's looking the other way, you sit Mr. Bones right behind her. She turns around.");
                        Her(c, b, Feel.Surprised, c.T("KYAAA! ...You...! I'll get you back for that! || KYAAA! ...I'll get you back! || EEK! ...Revenge! || KYA! ...You'll see!", "AAAAH! ...Okay. War. This is war. || AAAH! ...War! || EEEK! ...It's ON! || AAAH! ...Payback time!", "...I'm going to remember this. || ...Remembered. || ...You'll regret this. || ...Noted. For later.", "Oh, you're dead. You're so dead. || You're dead. || So dead. || You're finished."));
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
                Id = "sunset", Title = "Evening in the lecture hall", Bg = "classroom_sunset",
                Weight = c => (c.Low ? 3f : 0f) + (c.Bad != null ? 1.5f : 0f) + (c.Tier >= 2 ? 0.7f : 0.2f),
                Invite = (c, i) => i.Say(c.Her, c.T("...Hey. Do you have a minute? Not here... The lecture hall upstairs is empty. || ...A minute? Not here... upstairs is empty. || ...Can we talk? The upstairs room... || ...Do you have time? Somewhere quiet?", "Hey... can we talk? Somewhere quiet? Upstairs? || Talk? Somewhere quiet? Upstairs? || Hey... chat? Upstairs? || Got a sec? Quiet place?",
                    "Do you have a minute? I want to talk. Not here. || A minute? Not here. || I want to talk. Elsewhere. || Talk. Not here.", "Got a minute? I want to talk. Somewhere without an audience. || A minute? No audience. || Somewhere private. Talk. || Without spectators. Talk.")),
                Write = (c, b, t) =>
                {
                    b.Narr("The empty lecture hall at sunset. Chairs up on the desks, dust floating in the orange light.");
                    b.Narr("She sits on the windowsill and doesn't say anything for a long while.");
                    Callback(c, b);
                    var o = new Opts();
                    o.Keep("Wait for her to talk", "su_wait", () =>
                    {
                        b.Narr("You lean against the desk next to her and wait. Outside, the baseball club is shouting at a ball.");
                        Her(c, b, Feel.Smile, c.T("...Thank you for not rushing me. || ...Thank you for waiting. || ...You didn't rush me... thanks. || ...Thanks for your patience.", "...You're really patient, huh. Thanks. || ...So patient. Thanks. || ...You're patient. Thanks. || ...Thanks for waiting.", "...You're good at silence. Rare skill. || ...Good at silence. || ...Rare patience. || ...You wait well.", "...You didn't say anything stupid. Impressive. || ...No stupid comments. Impressive. || ...You stayed quiet. Nice. || ...Silent. Good."));
                        t.On("su_wait", () => t.Her(c.Her, 2, 4));
                    });
                    o.If(c.Snack != "", "Offer her your " + ItemName(c.Snack), "su_snack", () =>
                    {
                        b.Narr("You hold out the " + ItemName(c.Snack) + ". She takes it, and a little of the tension leaves her shoulders.");
                        Her(c, b, Feel.Smile, c.T("...Thank you. I didn't eat lunch. || ...Thank you. I skipped lunch. || ...Thanks... I was hungry. || ...Food... thank you.", "Food! You always know! ...Thanks. || Food! You know me! ...Thanks. || Snack! ...Thanks. || You're a mind reader! ...Thanks.", "...Thanks. I needed that. || ...Thanks. Needed. || ...Appreciated. || ...Good timing.", "...Bribery works on me. Apparently. || ...Bribery works. || ...Food bribes. Effective. || ...Snacks win me over."));
                        t.On("su_snack", () => { t.Use(c.Snack); t.Her(c.Her, 4, 2); });
                    }, 1.5f);
                    var funny = Roll(G.Sens, 45);
                    o.Add("Crack a joke to break the ice [SEN]", "su_joke", () =>
                    {
                        b.Me("So. Is this where you tell me you're secretly a ghost? Because I'm not ready for that.");
                        Her(c, b, Feel.Laugh, funny ? c.T("...Pfft. Idiot. ...Thanks. || ...Pfft. Silly. ...Thanks. || ...Hehe. Dummy. ...Thanks. || ...Idiot. ...Thank you.", "Ahaha! I'm not a ghost! ...Thanks for that. || Not a ghost! ...Thanks. || Haha! ...Thanks for that. || I'm alive! ...Thanks!", "...That was terrible. It helped. || ...Terrible. Helpful. || ...Bad joke. It worked. || ...Awful. Thanks.", "...Okay, that got a smile. Barely. || ...A smile. Barely. || ...Okay. Tiny smile. || ...Got me. Slightly.") : c.T("...That's not funny. ...Okay, it's a little funny. || ...Not funny. ...A little funny. || ...Hmph. ...Okay, a bit. || ...That's... okay, kind of funny.", "...Ha. Ha. ...Okay, I smiled. || ...Ha. ...Okay, smiled. || ...Heh. ...Fine. || ...Hmm. ...Okay, a smile.", "...No. || ...No. || ...Stop. || ...Not now.", "...Really? That's your opener? || ...That's your opener? || ...Seriously? || ...That's it?"));
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
