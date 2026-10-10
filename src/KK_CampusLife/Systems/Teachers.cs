using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using KKAPI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Teachers, the Campus Life way (user, 2026-10-11):
    /// <list type="bullet">
    /// <item>They walk around the school like the story heroines (StoryCast adds them as NPCs; ClassLoan lends a
    /// timetable).</item>
    /// <item>Their event is a conversation with choices instead of the game's string of short talks: each choice moves
    /// favor (and, later, H). The game's own milestones are kept in her talkEvent (0 met, 1 friend, 2 H open, 4 close),
    /// so the game's sleep event and the rest of her data stay consistent.</item>
    /// <item>Before she is a friend she only talks at her event spot (her Fix of the period); walking around she is busy.
    /// From friend on she talks anywhere. When the friendship is complete her event is over and she joins the school as
    /// a free character (a regular seat, like the graduated story heroines), and her conversation lines stay a
    /// teacher's.</item>
    /// </list>
    /// </summary>
    internal static class Teachers
    {
        private static CampusState S { get { return Campus.S; } }

        public static IEnumerable<SaveData.Heroine> All { get { return G.Heroines.Where(h => h != null && h.isTeacher); } }

        private static string CopyKey(SaveData.Heroine h) { return "teachercopy:" + GirlInfo.Key(h); }
        private static string DoneKey(SaveData.Heroine h) { return "teacherdone:" + GirlInfo.Key(h); }
        private static string StampKey(SaveData.Heroine h) { return "teachertalk:" + GirlInfo.Key(h); }

        /// <summary>A regular seat made from a teacher whose event is over: her lines in conversation are a teacher's.</summary>
        public static bool IsTeacherVoice(SaveData.Heroine h) { return h != null && (h.isTeacher || S != null && S.Buffs.ContainsKey(CopyKey(h))); }

        /// <summary>The seat of a teacher whose event is over: a student only in the code (user, 2026-10-31), a teacher to everyone at school.</summary>
        public static bool Freed(SaveData.Heroine h) { return h != null && !h.isTeacher && IsTeacherVoice(h); }

        /// <summary>Her name as the school says it: students and colleagues alike call a teacher sensei.</summary>
        public static string Called(SaveData.Heroine h) { return IsTeacherVoice(h) ? GirlInfo.FirstName(h) + "-sensei" : GirlInfo.FirstName(h); }

        public static string Called(string key) { return Called(GirlInfo.Find(key)); }

        private static readonly Dictionary<string, string> Causes = new Dictionary<string, string>
        {
            { "raise", "her raise" }, { "results", "her class's results" }, { "praise", "what the principal said" }, { "grading", "her grading" },
            { "lessons", "her lesson plans" }, { "staffparty", "last night's staff party" }, { "parents", "the parents" }, { "principal", "the principal" },
            { "complaint", "a parent's complaint" }, { "ignored", "her class" }, { "meeting", "the staff meeting" }, { "staffroom", "the staff room" }, { "nolunch", "lunch" },
        };

        /// <summary>Why a teacher is in this mood today, from her job ({tcause} in the dialogue), or null: grading in an exam week, else the same reason all day.</summary>
        public static string Cause(SaveData.Heroine h, CampusLife.Mood mood)
        {
            if (!IsTeacherVoice(h)) return null;
            string[] l;
            switch (mood)
            {
                case CampusLife.Mood.Cheerful: l = new[] { "raise", "results", "praise" }; break;
                case CampusLife.Mood.Sleepy: l = Calendar.IsExamWeek ? new[] { "grading" } : new[] { "grading", "lessons", "staffparty" }; break;
                case CampusLife.Mood.Stressed: l = Calendar.IsExamWeek ? new[] { "grading" } : new[] { "grading", "parents", "principal" }; break;
                case CampusLife.Mood.Upset: l = new[] { "complaint", "ignored", "principal" }; break;
                case CampusLife.Mood.Bored: l = new[] { "meeting" }; break;
                case CampusLife.Mood.Lonely: l = new[] { "staffroom" }; break;
                case CampusLife.Mood.Hungry: l = new[] { "nolunch" }; break;
                default: return null;
            }
            return l[Math.Abs((GirlInfo.Key(h) + S.Day).GetHashCode()) % l.Length];
        }

        public static string CauseThing(string cause) { string t; return Causes.TryGetValue(cause, out t) ? t : "her day"; }

        public static bool EventOver(SaveData.Heroine h) { return h != null && S.Buffs.ContainsKey(DoneKey(h)); }

        public static bool Met(SaveData.Heroine h) { return h.talkEvent.Contains(0); }
        public static bool Friend(SaveData.Heroine h) { return h.talkEvent.Contains(1); }
        public static bool HOpen(SaveData.Heroine h) { return h.talkEvent.Contains(2); }
        public static bool Close(SaveData.Heroine h) { return h.talkEvent.Contains(4); }

        public static string Stage(SaveData.Heroine h)
        {
            if (EventOver(h)) return "event over (free)";
            if (Close(h)) return "close";
            if (HOpen(h)) return "lovers";
            if (Friend(h)) return "friend";
            if (Met(h)) return "acquaintance";
            return "not met";
        }

        // ================= clicks =================

        /// <summary>
        /// Prefix of ActionScene._SceneEvent: a teacher (her Fix at her event spot, or walking around) gets Campus Life's
        /// conversation instead of the game's. True when handled (the game's event is dropped).
        /// </summary>
        public static bool Click(Base chara)
        {
            var h = chara != null ? chara.heroine : null;
            if (h == null || !h.isTeacher || !Campus.On || !Cfg.TeacherTalks.Value) return false;
            var atEvent = chara is Fix;
            CampusLifePlugin.Instance.StartCoroutine(TalkCo(chara, atEvent).PreventFromCrashing());
            return true;
        }

        private static IEnumerator TalkCo(Base chara, bool atEvent)
        {
            // The click came from the game's event start: wait until the map is free for our scene.
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 5f && !Scenes.CanPlayOnMap) yield return null;
            if (!Scenes.CanPlayOnMap) yield break;
            try { Talk(chara, atEvent); }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Teacher talk: " + e); }
        }

        private static int Stamp { get { return S.Day * 16 + (int)G.CurrentMask; } }

        internal static void ResetStamp(SaveData.Heroine h) { S.Buffs.Remove(StampKey(h)); }

        /// <summary>One conversation with her (or, outside her event before you are friends, a polite "not now").</summary>
        public static void Talk(Base chara, bool atEvent)
        {
            var h = chara.heroine;
            var n = chara as NPC;
            var name = TeacherName(h);
            int last;
            S.Buffs.TryGetValue(StampKey(h), out last);
            if (!Friend(h) && Met(h) && !atEvent)
            {
                var b0 = new SceneBuilder(null, false);
                b0.Say(h, T(h, "Ah, I'm sorry, I'm in a hurry. If you need me, come by when I'm free. || Sorry, I'm rushing somewhere. Come find me when I'm free. || Ah, not now, I'm afraid. Later, alright? || I'm in a bit of a hurry. Visit me later?", "Hey! Can't stop now, sorry! Catch me later! || Can't talk! Gotta run! Later! || Whoops, in a rush! Find me later! || Busy busy! Later, okay?!", "Not now. I have work. Find me at my desk later. || Not now. Later, at my desk. || I'm occupied. Come back later. || Later. I'm busy.", "Busy busy. Come find me when I'm sitting still, okay? || Running late. Catch me when I'm sitting, okay? || Not now, cutie. Later. || Busy. Come back when I'm bored."));
                Scenes.PlayOnMap(b0, c => { });
                Log(name + ": busy (not friends yet, outside her event)");
                return;
            }
            if (last == Stamp && (!Friend(h) || Random.value < 0.5f))
            {
                var b1 = new SceneBuilder(null, false);
                b1.Say(h, T(h, "We already talked this period, didn't we? Go on, your friends are waiting. || We've talked already this period. Go see your friends. || Didn't we just talk? Go on, have fun. || Again? Go enjoy your break.", "Again? Hehe, I like the enthusiasm! Later, okay? || Again?! Love the energy! Later! || You again! Hehe, later! || Back already? Later, kiddo!", "We spoke already. Go. || We spoke. Go. || Already done. Go. || Enough for now.", "Missed me already? Later, troublemaker. || Can't get enough of me? Later. || Again? Clingy. Later. || Back so soon? Later, troublemaker."));
                Scenes.PlayOnMap(b1, c => { });
                return;
            }
            S.Buffs[StampKey(h)] = Stamp;
            if (n != null) Forms.Hold(n);
            Action<string> after;
            var b = Build(h, out after);
            if (!Scenes.PlayOnMap(b, c =>
            {
                if (n != null) Forms.Let(n);
                try { after(c ?? ""); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                Social.RecordTalk(h);
                Advance(chara);
            }))
            { if (n != null) Forms.Let(n); }
        }

        // ================= the conversation =================

        private static string T(SaveData.Heroine h, string gentle, string energetic, string cool, string teasing) { return Lines.T(GirlInfo.ToneOf(h), gentle, energetic, cool, teasing); }

        private static string TeacherName(SaveData.Heroine h)
        {
            var last = h.Name;
            try { last = h.parameter.lastname; } catch { }
            return string.IsNullOrEmpty(last) ? GirlInfo.FirstName(h) : last + "-sensei";
        }

        private sealed class Option { public string Label, Tag, Reply; public int Favor, Lewd; public Feel Face = Feel.Smile; }

        private static Option O(string label, string tag, string reply, int favor, int lewd = 0, Feel face = Feel.Smile)
        {
            return new Option { Label = label, Tag = tag, Reply = reply, Favor = favor, Lewd = lewd, Face = face };
        }

        /// <summary>The scene for her stage: the first meeting, a topic with three answers, or a milestone.</summary>
        private static SceneBuilder Build(SaveData.Heroine h, out Action<string> after)
        {
            var b = new SceneBuilder(null, false);
            var name = TeacherName(h);
            if (!Met(h))
            {
                b.Narr("A teacher you only know from the hallways stops when she sees you.");
                b.Face(h, Feel.Smile).Say(h, T(h, "Oh, you're one of the new students, right? I'm " + name + ". If anything troubles you, you can come to me. || You must be new. I'm " + name + ". Come to me if anything bothers you. || Welcome! I'm " + name + ". Don't hesitate to ask me things. || Hello, new student. I'm " + name + ". I'm here if you need help.",
                    "Hiya! You're the new kid, right? I'm " + name + "! Nice to meet ya! || New kid! I'm " + name + "! Welcome aboard! || Hey hey! I'm " + name + "! You're new, right?! || Welcome! " + name + "'s the name! Nice to meet you!", "You. New student. I'm " + name + ". Don't cause trouble. || New student. " + name + ". Behave. || I'm " + name + ". No trouble, please." + " || " + name + ". Follow the rules.", "Well, well. The famous transfer student. I'm " + name + ". Behave... or don't, it's more fun. || The new transfer. I'm " + name + ". Be good. Or interesting. || I'm " + name + ". Behave... if you can. || Ah, the new one. " + name + ". Let's have fun this term."));
                var opts0 = new[]
                {
                    O("Bow and introduce yourself properly.", "t0_polite", T(h, "So polite. I think we'll get along. || How polite. We'll get along. || Such good manners. I like that. || So courteous. Thank you.", "Wow, so formal! I like you! || So formal! Ha! I like you! || Wow, manners! Cool! || Polite kid! Nice!", "...Good manners. Noted. || ...Good manners. || ...Proper. Good. || ...Courteous. Noted.", "Polite, too. Dangerous combination. || Polite and cute. Risky. || Manners too? Dangerous. || Polite? How charming."), 6),
                    O("Say you'll come to her for help a lot.", "t0_help", T(h, "Ufufu, then I'll be waiting. || Ufufu, I'll look forward to it. || Then I'll be expecting you. || Anytime, then.", "Anytime! I mean it! || Anytime! Really! || Come whenever! || Door's always open!", "...Within reason. || ...Reasonably. || ...Within limits. || ...Not too often.", "A lot? Should I clear my schedule? || A lot? I'll make time. || Often? Clear my calendar? || Planning to visit a lot, hm?"), 4),
                    O("Say she doesn't look like a teacher.", "t0_young", T(h, "E-eh? Is that a compliment...? || Eh? Was that a compliment...? || O-oh... thank you...? || Um... is that a compliment?", "Ha! I get that a lot! || Ha! Heard that before! || Hehe! Thanks! || Yeah, I know! Kidding!", "...I'll take that as you meant it kindly. || ...I'll assume kindness. || ...Interpreted kindly. || ...Hm.", "Flattery on day one. Bold. || Flattery already? Bold. || Smooth on day one. || Flirting with your teacher? Brave."), 3, 1, Feel.Shy),
                };
                return Finish(b, h, opts0, c => { h.talkEvent.Add(0); Log(name + ": met"); }, out after);
            }
            if (!Friend(h) && h.favor >= 50)
                return Milestone(b, h, "friend", out after);
            if (Friend(h) && !HOpen(h) && h.favor >= 85 && h.lewdness >= 40)
                return Milestone(b, h, "h", out after);
            if (HOpen(h) && !Close(h) && h.favor >= 100 && (h.hCount > 0 || h.lewdness >= 70))
                return Milestone(b, h, "close", out after);

            var pool = new List<Func<SaveData.Heroine, SceneBuilder, Option[]>> { Lesson, HerDay, Career, Compliment };
            if (Friend(h)) { pool.Add(AfterHours); pool.Add(Flirt); }
            var topic = pool[Random.Range(0, pool.Count)];
            var opts = topic(h, b);
            return Finish(b, h, opts, null, out after);
        }

        private static SceneBuilder Finish(SceneBuilder b, SaveData.Heroine h, Option[] opts, Action<string> extra, out Action<string> after)
        {
            var args = new List<string>();
            foreach (var o in opts) { args.Add(o.Label); args.Add(o.Tag); }
            b.Choice(args.ToArray());
            foreach (var o in opts) { b.Branch(o.Tag); b.Face(h, o.Face).Say(h, o.Reply); b.End(); }
            after = c =>
            {
                var o = opts.FirstOrDefault(x => x.Tag == c);
                if (o != null)
                {
                    G.AddFavor(h, o.Favor, o.Lewd);
                    Log(TeacherName(h) + ": '" + o.Label + "' favor " + (o.Favor >= 0 ? "+" : "") + o.Favor + (o.Lewd != 0 ? ", H +" + o.Lewd : "") + " -> favor " + h.favor + ", H " + h.lewdness + " (" + Stage(h) + ")");
                    Director.Changed(h, "talked (teacher)");
                }
                if (extra != null) extra(c);
            };
            return b;
        }

        // ---- topics: three answers each, the best one fits a teacher ----

        private static Option[] Lesson(SaveData.Heroine h, SceneBuilder b)
        {
            b.Face(h, Feel.Normal).Say(h, T(h, "Did today's lesson make sense? You looked a little lost. || Did the lesson make sense? You seemed lost. || How was today's class? Was it clear? || Did you follow along today?", "So! Today's class! Did it stick, or did it fly out the window? || Today's class! Did it stick?! || So! Did you get it?! || Lesson check! Thumbs up or down?", "Today's material. Questions? || Questions about today? || Today's lesson. Understood? || Anything unclear?", "You were staring out the window again. What was so interesting? || Window-watching again? What's out there? || Daydreaming in my class? Rude. || What were you staring at today?"));
            var smart = G.Int >= 40;
            return new[]
            {
                O("Ask a sharp question about the material." + (smart ? "" : " [INT]"), "tl_ask", smart ? T(h, "Oh! That's a very good question... you've been thinking. || That's a good question... you've been thinking. || Oh! What a thoughtful question. || Very good... you're really listening.", "Ooh, good one! Someone's paying attention! || Great question! Someone's awake! || Ooh! Nice one! || Yes! Good question!", "...Good. That's the right question. || ...Correct question. || ...Good. Precisely. || ...Well asked.", "Look at you, actually listening. I'm impressed. || Actually listening? Impressive. || Oh? Paying attention? Cute. || A real question! Wow.") : T(h, "Um... that's... not quite it, but I'm glad you're trying. || Not quite... but I'm glad you're trying. || Hmm, close... keep trying. || Almost... good effort.", "Close! ...Ish! || Close-ish! || Nearly! Kinda! || Almost! Not really!", "No. Read chapter three again. || No. Reread chapter three. || Incorrect. || Wrong. Review it.", "Cute try. Wrong, but cute. || Wrong, but adorable. || Cute guess. No. || Nice try. Totally wrong."), smart ? 8 : 2, 0, smart ? Feel.Smile : Feel.Worried),
                O("Admit you didn't follow and ask for help.", "tl_help", T(h, "Thank you for being honest. Come to the staff room after class. || Thank you for being honest. Come by after class. || I'm glad you told me. Let's go over it later. || Honesty is good. Visit the staff room.", "Honesty! Love it! I'll walk you through it! || Honest! Love it! Let's fix it! || Great! I'll explain it after! || Okay! Lesson two, after class!", "...Fine. After class. Bring your notes. || ...After class. With notes. || ...Fine. Later. Bring your book. || ...Come later.", "Asking me for private lessons? ...After class, then. || Private lessons? After class, then. || A one-on-one? Sure. || Want extra time with me? Alright."), 6),
                O("Joke that the window was more interesting.", "tl_joke", T(h, "M-mou... that hurts a little, you know. || Mou... that stings a little. || That's a bit hurtful... || Oh... I'm sorry it's boring...", "Hey! My lessons are fun! ...Right? || Hey! My class is fun! Right?! || Boring?! No way! || Rude! I'm fun!", "...Detention is also interesting. || ...Detention is also an option. || ...Noted. Detention awaits. || ...Interesting. Detention.", "Ha. Next time I'll stand in front of the window. || Next time I'll block the window. || Then I'll be more interesting. Watch. || Ha. I'll stand in your view."), -2, 0, Feel.Pout),
            };
        }

        private static Option[] HerDay(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr(TeacherName(h) + " is carrying a tall stack of papers and sighs.");
            b.Say(h, T(h, "Ah... there's so much to grade this week... || So much grading this week... || Ah... these tests won't grade themselves... || The pile keeps growing...", "Ugh, paperwork! The final boss of teaching! || Paperwork! My nemesis! || Ugh! Grading mountain! || Help! Paper monster!", "...Forty essays. Again. || ...Forty essays. || ...Too many papers. || ...Again.", "Grading. The glamorous life of a teacher. || Grading. So glamorous. || Living the dream. Grading. || Ah, paperwork. My love."));
            return new[]
            {
                O("Offer to carry the papers for her.", "td_carry", T(h, "Really? Thank you... you're very kind. || Really? Thank you, you're kind. || You'd help? Thank you. || Oh, thank you so much.", "My hero! Here, take half! || Hero! Take half! || Yes! Thanks! || Lifesaver!", "...I accept. Careful with them. || ...Accepted. Carefully. || ...Thank you. Don't drop them. || ...Fine. Gently.", "A gentleman. Don't drop them, or you're grading them. || Gentleman. Drop them and you grade them. || Careful, or you're grading. || How chivalrous. Don't spill."), 7),
                O("Tell her she works too hard.", "td_hard", T(h, "Ufufu... I don't hear that very often. Thank you. || I rarely hear that... thank you. || Ufufu, how kind. Thank you. || That's sweet of you.", "Right?! Someone gets it! || Right?! Finally someone gets it! || Yes! Appreciation! || Thank you!", "...Someone has to. || ...Someone must. || ...It's necessary. || ...Duty.", "Worried about me? How sweet. || Worried about me? Cute. || Concerned? How sweet. || Aww, you care."), 5),
                O("Ask if your test is in that pile.", "td_test", T(h, "Ah... you'll find out tomorrow, okay? || You'll see tomorrow. || Tomorrow, okay? || Patience until tomorrow.", "No spoilers! || No spoilers! || Secret! || Wait and see!", "...Yes. Study harder. || ...Yes. Study. || ...Probably. Study harder. || ...Prepare.", "Maybe. Want to bribe me? || Maybe. Bribe me? || Could be. What's it worth? || Perhaps. Charm me."), 1, 0, Feel.Normal),
            };
        }

        private static Option[] Career(SaveData.Heroine h, SceneBuilder b)
        {
            b.Say(h, T(h, "Have you thought about what you want to do after graduating? || What would you like to do after you graduate? || Have you thought about your future? || Any dreams for after school?", "So! Future plans! Rock star? Astronaut? || Future plans! Pilot? Pop star? || What's next?! Something cool?! || After school! Big plans?!", "Career forms are due next week. Thought about it? || Career forms. Next week. Ideas? || Your future plans. Thought about it? || Post-graduation. Decided?", "So, what's the plan after school? Besides breaking hearts. || After school, what then? Besides charming girls. || Future plans? Other than being a menace? || What's your plan? Aside from flirting."));
            return new[]
            {
                O("Ask why she became a teacher.", "tc_why", T(h, "Me? I had a teacher who believed in me... I wanted to be that for someone. || A teacher believed in me once... I wanted to do the same. || Someone helped me... I wanted to pass it on. || I wanted to be the teacher I once had.", "Because school was the best! So I never left! Ha! || School was the best! So I stayed! || I loved school so much I came back! || Teaching is fun! Simple!", "...I like seeing people grow. Don't tell anyone. || ...I like watching growth. Secret. || ...Helping people improve. Quietly. || ...Seeing progress. That's why.", "Summer vacations. ...And a teacher who changed my life. Mostly the vacations. || Vacations. And a great teacher. Mostly vacations. || Long summers. And someone who inspired me. || Holidays. Kidding. Mostly."), 8),
                O("Say you might want to be a teacher too.", "tc_teach", T(h, "Really? I think you'd be good at it. || I think you'd be wonderful at it. || You? That would be lovely. || You'd be a good teacher, I think.", "Whoa! Colleague-in-training! || Future colleague! || Teacher buddies! || Whoa! Cool!", "...It's harder than it looks. But you could. || ...Hard work. You could manage. || ...Possible. || ...Not easy. But doable.", "You? In front of a class? I'd pay to see that. || You teaching? I'd attend. || I'd sit in the front row. || Now that I'd pay to watch."), 5),
                O("Say you haven't thought about it at all.", "tc_none", T(h, "That's okay. There's still time... but think a little, okay? || There's still time... but think about it. || It's okay. Just consider it a little. || No rush... but do think.", "Living in the moment! ...Think about it a bit, though. || Carefree! ...Think a bit, though! || Living free! But think! || Cool! But plan a little!", "Start thinking. || Think. || Start considering it. || Decide soon.", "Honest, at least. || Honest. At least. || Ha. Honest answer. || Well, you're truthful."), 1, 0, Feel.Worried),
            };
        }

        private static Option[] Compliment(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("Between classes, " + TeacherName(h) + " stops to fix her hair in the window's reflection.");
            var close = h.favor >= 50;
            return new[]
            {
                O("Tell her today's lesson was the best one yet.", "tm_lesson", T(h, "Fufu... that makes all the work worth it. || That makes it all worthwhile. || Fufu... thank you. That means a lot. || I'm so happy to hear that.", "Yesss! Validation! || Yes! Validation! || Woo! Best feedback! || Thank you!", "...Thank you. I prepared for it. || ...Thank you. I prepared. || ...Good to hear. || ...Appreciated.", "The best one, huh? Keep the compliments coming. || The best? More compliments, please. || Keep going. I like this. || Oh? More of that."), 6),
                O("Tell her she looks pretty today.", "tm_pretty", close ? T(h, "E-eh?! You shouldn't say that to a teacher... b-but thank you... || You shouldn't say that... but thank you... || Eh?! To your teacher...? Thank you... || Oh my... that's not proper... thank you.", "Wha-! You can't just say that! ...Hehe. || You can't say that! ...Hehe! || Whoa! ...Thanks! || Hey! ...Hehe, okay!", "...Inappropriate. ...Thank you. || ...Inappropriate. ...Thanks. || ...Improper. ...Thank you. || ...Don't. ...Thanks.", "Oh? Careful, or I'll start believing you. || Careful. I might believe you. || Oh? Keep that up. || Flirting? I'll allow it.") : T(h, "That's... not something to say to your teacher. || That's not something to say to a teacher. || Please, that's not appropriate. || That isn't proper...", "Whoa there! Let's keep it professional! || Whoa! Professional, please! || Hey now! Keep it school! || Easy there!", "...Inappropriate. || ...Inappropriate. || ...Improper. || ...No.", "Nice try, kid. || Nice try, kid. || Cute. No. || Ha. Nope."), close ? 4 : -3, close ? 3 : 0, close ? Feel.Shy : Feel.Pout),
                O("Say her hair looks fine as it is.", "tm_hair", T(h, "You think so? Thank you. || You think so? Thank you. || Really? I'm glad. || Oh, thank you.", "Phew! Okay, good! || Phew! Good! || Yay! Okay! || Great!", "...Hm. Good. || ...Good. || ...Hm. || ...Fine.", "Just fine? I was aiming for stunning. || Fine? I aimed for stunning. || Only fine? Hmph. || Just fine? Try again."), 3),
            };
        }

        private static Option[] AfterHours(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("After the last bell, " + TeacherName(h) + " is still at school, stretching.");
            b.Say(h, T(h, "Oh, you're still here? Shouldn't you be going home? || Still here? Shouldn't you go home? || It's late... aren't you going home? || Oh, you're still at school?", "Still here? Me too! Teachers never go home, apparently! || Still here? Me too! || You stayed late too?! || Late club?! Me too!", "Still here. Why? || Still here. Reason? || Why are you here? || Late. Why?", "Lingering after school? Waiting for someone? || Lingering? Waiting for someone? || Still here? For me? || Hanging around late? Suspicious."));
            return new[]
            {
                O("Offer to walk her to the station.", "ta_walk", T(h, "Then... just to the station. Thank you. || Just to the station, then. Thank you. || Alright, until the station. Thanks. || That's kind... to the station.", "Escort service! Sure, let's go! || Escort! Let's go! || Sure! Walk buddy! || Yay! Company!", "...Fine. It's on the way. || ...On the way. Fine. || ...Acceptable. || ...Alright.", "A walk home with a student... I'll allow it. Just this once. || Walking a teacher home? Just once. || Scandalous. Fine. || A student escort? Cute. Okay."), 7, 1),
                O("Offer to help her grade.", "ta_grade", T(h, "You'd do that? Then I'll buy you a juice. || You'd help? I'll buy you a drink. || Thank you... I'll treat you. || How kind. Juice is on me.", "Yes! Free labor! I mean, thank you! || Free help! I mean, thanks! || Yes! Helper! || Woo! Thanks!", "...Red pen. Don't grade your own. || ...Red pen. Not your own. || ...Fine. Don't grade yours. || ...Careful with the grades.", "Help me grade? You just want to see the scores. || You just want to peek at scores. || Helping? Or snooping? || Cute excuse to see grades."), 6),
                O("Tease her about the staff room gossip.", "ta_gossip", T(h, "W-what gossip?! There's no gossip! || G-gossip?! There's nothing! || What gossip?! No! || Th-there's no gossip!", "Wait, what are they saying?! Tell me! || What are they saying?! Tell me! || Gossip?! Spill! || Wait, what?! Tell!", "...What did you hear. || ...What did you hear. || ...Explain. || ...Which gossip.", "Ooh, gossip about me? Spill. || Gossip about me? Do tell. || Ooh. Spill it. || About me? Details."), 3, 2, Feel.Surprised),
            };
        }

        private static Option[] Flirt(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("The classroom is empty. " + TeacherName(h) + " sits on the edge of her desk, closer than usual.");
            b.Say(h, T(h, "It's quiet when everyone's gone... I like talking with you like this. || It's so quiet now... I like talking like this. || The school's empty... it's nice with you. || Peaceful... I enjoy this.", "Just us! It's like detention but fun! || Just us! Fun detention! || Empty school! Cool! || Alone! Like a secret club!", "...It's quiet. I don't mind it, with you. || ...Quiet. Fine with you. || ...Silence. Pleasant, with you. || ...I don't mind this.", "Alone with a student after class... how scandalous. || Alone with a student. Scandalous. || After hours together? Naughty. || Just us. How improper."));
            var ready = h.favor >= 70;
            return new[]
            {
                O("Ask her out for coffee this weekend.", "tf_coffee", ready ? T(h, "A-as long as nobody from school sees us... okay. || As long as nobody sees... okay. || Somewhere no one knows us... alright. || If we're careful... yes.", "A date?! ...Shh! Yes! || A date?! Shh! Yes! || Ooh! Secret date! Yes! || Yes! But quietly!", "...Somewhere far from school. || ...Far from school. || ...Somewhere distant. || ...Away from here.", "A date with a student? ...Pick somewhere nice. || Pick somewhere nice. || A date? Impress me. || Fine. Somewhere classy.") : T(h, "Ah... that wouldn't be proper. || That wouldn't be proper. || I can't... I'm your teacher. || That isn't right...", "Whoa, whoa! I'm your teacher! || Whoa! Teacher here! || Hey! Can't! || Nope! Rules!", "No. || No. || Impossible. || Refused.", "Cute. No. || Cute. No. || Adorable. Nope. || Ha. No."), ready ? 6 : -2, ready ? 2 : 0, ready ? Feel.Love : Feel.Worried),
                O("Lean a little closer.", "tf_lean", ready ? T(h, "Y-you're too close... ...I don't hate it, though... || You're close... I don't hate it... || So close... it's okay... || Ah... too close... but okay...", "H-hey... you're kinda close... ...okay... || H-hey... close... okay... || Whoa... close... fine... || Um... close! Okay!", "...Close. ...Stay there. || ...Close. Stay. || ...Remain there. || ...Hm. Close.", "Bold. ...Closer, then. || Bold. Closer. || Closer, then. || Brave. Come here.") : T(h, "Th-that's too close! || Too close! || P-please, back up! || That's too close!", "Personal space! Personal space! || Space! Space! || Back up! || Too close!", "Back up. || Back up. || Distance. || Step back.", "Down, boy. || Down, boy. || Easy. || Heel."), ready ? 3 : -4, ready ? 5 : 0, ready ? Feel.Shy : Feel.Pout),
                O("Behave and just keep her company.", "tf_behave", T(h, "Fufu... you're sweet. || You're sweet. || Fufu... how nice. || So gentle.", "Aww, a good boy! || Good boy! || Aww! || Nice!", "...Thank you. || ...Thanks. || ...Good. || ...Hm.", "Behaving? How boring. ...Nice, though. || Behaving? Boring. Nice, though. || Polite. Dull. Sweet. || Good boy. Yawn. Cute."), 4),
            };
        }

        // ---- milestones (her talkEvent, like the game's) ----

        private static SceneBuilder Milestone(SceneBuilder b, SaveData.Heroine h, string which, out Action<string> after)
        {
            var name = TeacherName(h);
            Option[] opts;
            Action<string> extra;
            switch (which)
            {
                case "friend":
                    b.Narr(name + " smiles when she sees you, a real smile, not the classroom one.");
                    b.Face(h, Feel.Smile).Say(h, T(h, "You know... outside of class, you don't have to be so formal with me. I like talking with you. || Outside class, you can relax with me. I enjoy our talks. || You don't need to be formal outside class. || After class, just talk to me normally, okay?", "Hey, outside class we're friends, okay? Deal? Deal! || Outside class, we're pals! Deal! || Friends after hours! Okay?! || No formal stuff outside class!", "...Outside of class, you can talk to me normally. I'd like that. || ...Speak normally outside class. || ...Outside lessons, informal is fine. || ...Drop the formality. After class.", "Outside of class, drop the 'sensei'. ...Within reason. || No 'sensei' after hours. Mostly. || Outside class, call me by name. Carefully. || After school, drop the title."));
                    opts = new[]
                    {
                        O("Say you'd like that too.", "tx_yes", T(h, "Then it's settled. || Settled, then. || Then it's agreed. || Good.", "Yay! Friends! || Yay! Pals! || Friends! || Woo!", "...Good. || ...Good. || ...Agreed. || ...Fine.", "Good. Now I'll know where to find you. || Good. I'll find you. || Perfect. || Excellent."), 5),
                        O("Joke that you'll still call her sensei.", "tx_sensei", T(h, "Mou... fine, sensei it is. || Mou... sensei, then. || Alright... sensei. || Fine, have it your way.", "Ugh, so stubborn! Fine! || Stubborn! Fine! || Ugh! Okay! || Boo! Fine!", "...Suit yourself. || ...As you like. || ...Your choice. || ...Fine.", "Respectful. Or teasing. I can't tell. || Respectful or teasing? Hm. || Polite. Suspicious. || Is that teasing?"), 3),
                    };
                    extra = c => { h.talkEvent.Add(1); Toasts.Show(name + " is your friend now: she talks with you anywhere at school."); Log(name + ": friend"); };
                    break;
                case "h":
                    b.Narr("The door of the empty classroom slides shut behind " + name + ".");
                    b.Face(h, Feel.Shy).Say(h, T(h, "I shouldn't feel this way about a student... but I can't stop thinking about you. || I shouldn't... but I can't stop thinking of you. || It's wrong, I know... but my heart won't listen. || I'm your teacher... but I think about you constantly.", "This is SO against the rules... but I don't care anymore. || Totally against the rules! Don't care! || Rules? Forget them! || This is bad! I don't care!", "...I've been thinking about this for a while. About you. || ...I've thought about you. Long enough. || ...This has been on my mind. You. || ...I've considered it. You.", "I've decided to stop pretending. Come here. || I'm done pretending. Come. || Enough pretending. Here. || No more acting. Come close."));
                    opts = new[]
                    {
                        O("Take her hand.", "tx_h", T(h, "...Lock the door. || ...Please lock the door. || ...The door... lock it. || ...Lock it.", "C-close the door! Quick! || Door! Quick! || Close it! Hurry! || Lock it! Now!", "...The door. Lock it. || ...Lock it. || ...The door. || ...Lock.", "Good answer. The door, please. || Good. The door. || Lock it, darling. || The door. Now."), 6, 6, Feel.Love),
                        O("Say you feel the same, but not here.", "tx_wait", T(h, "...You're right. Thank you for being sensible. || You're right. Thank you. || ...Sensible. Thank you. || ...You're wiser than me.", "Aww. Okay... you're right. Later! || Aww. Right. Later! || Okay... later! || Fine! Later!", "...Right. Later. || ...Later. || ...Correct. Later. || ...Agreed.", "Sensible. Annoyingly sensible. || Sensible. Ugh. || Annoyingly mature. || Responsible. Boring."), 5, 3, Feel.Smile),
                    };
                    extra = c => { h.talkEvent.Add(2); Log(name + ": H open (" + c + ")"); if (c == "tx_h") StartH(h); };
                    break;
                default:
                    b.Narr(name + " walks with you to the school gate, the way she now does most days.");
                    b.Bg("@gate");
                    b.Face(h, Feel.Love).Say(h, T(h, "Somewhere along the way, you stopped being just a student to me... I'm glad. || You became more than a student to me... I'm glad. || Somewhere along the way, you became special. || You're not just a student anymore.", "You know what? You're my favorite person at this school. Don't tell the principal. || You're my favorite at this school! Shh! || Favorite person here! Secret! || Don't tell the principal! You're my fave!", "...You matter to me. That's all. || ...You matter. || ...You're important to me. || ...More than a student.", "I've gotten attached to you. Your fault, entirely. || I'm attached. Your fault. || I like you. Blame yourself. || You got under my skin."));
                    opts = new[]
                    {
                        O("Tell her she matters to you too.", "tx_close", T(h, "...Thank you. Really. || ...Thank you. || ...Thank you, truly. || ...That makes me happy.", "Hehe... okay. I'm happy! || Hehe! Happy! || Yay! || Aww!", "...I know. || ...I know. || ...Good. || ...Yes.", "...I know. Say it again sometime. || ...Say it again sometime. || ...I know. Repeat it. || ...More often, please."), 8, 2, Feel.Love),
                        O("Promise to visit her after you graduate.", "tx_promise", T(h, "I'll hold you to that. || I'll remember that. || I'll hold onto that promise. || I'm counting on it.", "Pinky promise! || Promise! || Pinky swear! || Deal!", "...I'll wait. || ...I'll wait. || ...Waiting. || ...Noted.", "You'd better. || You'd better. || Don't forget. || I'll hold you to it."), 8, 0, Feel.Smile),
                    };
                    extra = c => { h.talkEvent.Add(4); Log(name + ": close friends; her event is complete"); };
                    break;
            }
            return Finish(b, h, opts, extra, out after);
        }

        /// <summary>Into the game's H with her, where she stands (the game's own H start with a character).</summary>
        internal static void StartH(SaveData.Heroine h)
        {
            CampusLifePlugin.Instance.StartCoroutine(StartHCo(h).PreventFromCrashing());
        }

        private static IEnumerator StartHCo(SaveData.Heroine h)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 10f && !(G.IsFreeRoaming && !Scenes.Running)) yield return null;
            var act = G.Act;
            Base chara = act != null ? (Base)act.npcList.FirstOrDefault(n => n != null && n.heroine == h) : null;
            if (chara == null && act != null && act.fixChara != null && act.fixChara.heroine == h) chara = act.fixChara;
            if (chara == null) { Log("H: she is not here any more"); yield break; }
            IEnumerator run = null;
            try
            {
                act.Player.isActionNow = true;
                run = act.ChangeHEventWithActionPoint(chara.position, chara.rotation, -1, chara);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Teacher H: " + e); }
            if (run == null) { act.Player.isActionNow = false; yield break; }
            yield return act.StartCoroutine(run);
            if (act != null && act.Player != null) act.Player.isActionNow = false;
        }

        // ================= after a talk =================

        /// <summary>After each talk: once the friendship is complete, her event is over and she joins the school freely.</summary>
        private static void Advance(Base chara)
        {
            var h = chara.heroine;
            if (!Close(h) || EventOver(h)) return;
            S.Buffs[DoneKey(h)] = S.Day;
            Toasts.Show(TeacherName(h) + "'s event is complete. From tomorrow she is around school like everyone else, and you can talk to her normally.");
            Log(TeacherName(h) + ": event over, she becomes a free character");
        }

        /// <summary>
        /// Before the students of a period are loaded: a teacher whose event is over takes a free seat (a copy of her card,
        /// an everyday personality so every normal conversation works), keeping favor and H. Her lines stay a teacher's.
        /// </summary>
        /// <summary>Copies made before the portrait fix: the picture comes back from the girl they were copied from.</summary>
        public static void MendPortraits()
        {
            foreach (var h in G.Heroines.Where(x => x != null && x.charFile != null && (x.charFile.facePngData == null || x.charFile.facePngData.Length == 0)).ToList())
            {
                var from = G.Heroines.FirstOrDefault(x => x != null && x != h && x.charFile != null && x.charFile.facePngData != null && x.charFile.facePngData.Length > 0 && x.charFile.parameter.fullname == h.charFile.parameter.fullname);
                if (from == null) continue;
                h.charFile.pngData = from.charFile.pngData;
                h.charFile.facePngData = from.charFile.facePngData;
                Log(TeacherName(h) + ": portrait mended");
            }
        }

        public static void FreeFinished()
        {
            if (!Singleton<Manager.Game>.IsInstance()) return;
            var save = Singleton<Manager.Game>.Instance.saveData;
            if (save == null) return;
            foreach (var h in All.Where(x => EventOver(x) && !S.Buffs.ContainsKey("teacherfree:" + GirlInfo.Key(x))).ToList())
            {
                var seat = StoryCast.FreeSeatFor(save);
                if (seat == null) { Toasts.Show(TeacherName(h) + " wants to join school life freely, but every class seat is taken. Free a seat in the class roster."); return; }
                var card = new ChaFileControl();
                ChaFile.CopyChaFile(card, h.charFile);
                card.pngData = h.charFile.pngData;
                card.facePngData = h.charFile.facePngData; // CopyChaFile leaves the pictures out: the portrait came up as "?"
                card.parameter.personality = StoryCast.EverydayPersonalityFor(h);
                seat.SetCharFile(card);
                seat.charFileInitialized = true;
                seat.talkEvent.Add(0);
                seat.talkEvent.Add(1);
                seat.talkEvent.Add(2);
                seat.favor = Mathf.Clamp(Math.Max(h.favor, 60), 0, 100);
                seat.lewdness = h.lewdness;
                save.heroineList.Add(seat);
                S.Buffs["teacherfree:" + GirlInfo.Key(h)] = S.Day;
                S.Buffs[CopyKey(seat)] = S.Day;
                var from = S.Girl(GirlInfo.Key(h));
                var to = S.Girl(GirlInfo.Key(seat));
                to.Devotion = from.Devotion;
                to.Talks = from.Talks;
                Log(TeacherName(h) + " joined school life as a free character (seat in class " + seat.schoolClass + ")");
            }
        }

        internal static void FinishNow(SaveData.Heroine h) { S.Buffs[DoneKey(h)] = S.Day; FreeFinished(); }

        /// <summary>Teachers walking around as NPCs (her Fix of the period and teachers already free are left out).</summary>
        public static bool Walks(SaveData.Heroine h) { return h != null && h.isTeacher && Cfg.TeachersWalk.Value && !S.Buffs.ContainsKey("teacherfree:" + GirlInfo.Key(h)); }

        public static readonly List<string> Recent = new List<string>();

        private static void Log(string what)
        {
            Recent.Add(what);
            if (Recent.Count > 30) Recent.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Teachers: " + what);
        }
    }
}
