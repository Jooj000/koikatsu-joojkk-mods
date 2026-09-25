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
                b0.Say(h, T(h, "Ah, I'm sorry, I'm in a hurry. If you need me, come by when I'm free.", "Hey! Can't stop now, sorry! Catch me later!", "Not now. I have work. Find me at my desk later.", "Busy busy. Come find me when I'm sitting still, okay?"));
                Scenes.PlayOnMap(b0, c => { });
                Log(name + ": busy (not friends yet, outside her event)");
                return;
            }
            if (last == Stamp && (!Friend(h) || Random.value < 0.5f))
            {
                var b1 = new SceneBuilder(null, false);
                b1.Say(h, T(h, "We already talked this period, didn't we? Go on, your friends are waiting.", "Again? Hehe, I like the enthusiasm! Later, okay?", "We spoke already. Go.", "Missed me already? Later, troublemaker."));
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
                b.Face(h, Feel.Smile).Say(h, T(h, "Oh, you're one of the new students, right? I'm " + name + ". If anything troubles you, you can come to me.",
                    "Hiya! You're the new kid, right? I'm " + name + "! Nice to meet ya!", "You. New student. I'm " + name + ". Don't cause trouble.", "Well, well. The famous transfer student. I'm " + name + ". Behave... or don't, it's more fun."));
                var opts0 = new[]
                {
                    O("Bow and introduce yourself properly.", "t0_polite", T(h, "So polite. I think we'll get along.", "Wow, so formal! I like you!", "...Good manners. Noted.", "Polite, too. Dangerous combination."), 6),
                    O("Say you'll come to her for help a lot.", "t0_help", T(h, "Ufufu, then I'll be waiting.", "Anytime! I mean it!", "...Within reason.", "A lot? Should I clear my schedule?"), 4),
                    O("Say she doesn't look like a teacher.", "t0_young", T(h, "E-eh? Is that a compliment...?", "Ha! I get that a lot!", "...I'll take that as you meant it kindly.", "Flattery on day one. Bold."), 3, 1, Feel.Shy),
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
            b.Face(h, Feel.Normal).Say(h, T(h, "Did today's lesson make sense? You looked a little lost.", "So! Today's class! Did it stick, or did it fly out the window?", "Today's material. Questions?", "You were staring out the window again. What was so interesting?"));
            var smart = G.Int >= 40;
            return new[]
            {
                O("Ask a sharp question about the material." + (smart ? "" : " [INT]"), "tl_ask", smart ? T(h, "Oh! That's a very good question... you've been thinking.", "Ooh, good one! Someone's paying attention!", "...Good. That's the right question.", "Look at you, actually listening. I'm impressed.") : T(h, "Um... that's... not quite it, but I'm glad you're trying.", "Close! ...Ish!", "No. Read chapter three again.", "Cute try. Wrong, but cute."), smart ? 8 : 2, 0, smart ? Feel.Smile : Feel.Worried),
                O("Admit you didn't follow and ask for help.", "tl_help", T(h, "Thank you for being honest. Come to the staff room after class.", "Honesty! Love it! I'll walk you through it!", "...Fine. After class. Bring your notes.", "Asking me for private lessons? ...After class, then."), 6),
                O("Joke that the window was more interesting.", "tl_joke", T(h, "M-mou... that hurts a little, you know.", "Hey! My lessons are fun! ...Right?", "...Detention is also interesting.", "Ha. Next time I'll stand in front of the window."), -2, 0, Feel.Pout),
            };
        }

        private static Option[] HerDay(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr(TeacherName(h) + " is carrying a tall stack of papers and sighs.");
            b.Say(h, T(h, "Ah... there's so much to grade this week...", "Ugh, paperwork! The final boss of teaching!", "...Forty essays. Again.", "Grading. The glamorous life of a teacher."));
            return new[]
            {
                O("Offer to carry the papers for her.", "td_carry", T(h, "Really? Thank you... you're very kind.", "My hero! Here, take half!", "...I accept. Careful with them.", "A gentleman. Don't drop them, or you're grading them."), 7),
                O("Tell her she works too hard.", "td_hard", T(h, "Ufufu... I don't hear that very often. Thank you.", "Right?! Someone gets it!", "...Someone has to.", "Worried about me? How sweet."), 5),
                O("Ask if your test is in that pile.", "td_test", T(h, "Ah... you'll find out tomorrow, okay?", "No spoilers!", "...Yes. Study harder.", "Maybe. Want to bribe me?"), 1, 0, Feel.Normal),
            };
        }

        private static Option[] Career(SaveData.Heroine h, SceneBuilder b)
        {
            b.Say(h, T(h, "Have you thought about what you want to do after graduating?", "So! Future plans! Rock star? Astronaut?", "Career forms are due next week. Thought about it?", "So, what's the plan after school? Besides breaking hearts."));
            return new[]
            {
                O("Ask why she became a teacher.", "tc_why", T(h, "Me? I had a teacher who believed in me... I wanted to be that for someone.", "Because school was the best! So I never left! Ha!", "...I like seeing people grow. Don't tell anyone.", "Summer vacations. ...And a teacher who changed my life. Mostly the vacations."), 8),
                O("Say you might want to be a teacher too.", "tc_teach", T(h, "Really? I think you'd be good at it.", "Whoa! Colleague-in-training!", "...It's harder than it looks. But you could.", "You? In front of a class? I'd pay to see that."), 5),
                O("Say you haven't thought about it at all.", "tc_none", T(h, "That's okay. There's still time... but think a little, okay?", "Living in the moment! ...Think about it a bit, though.", "Start thinking.", "Honest, at least."), 1, 0, Feel.Worried),
            };
        }

        private static Option[] Compliment(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("Between classes, " + TeacherName(h) + " stops to fix her hair in the window's reflection.");
            var close = h.favor >= 50;
            return new[]
            {
                O("Tell her today's lesson was the best one yet.", "tm_lesson", T(h, "Fufu... that makes all the work worth it.", "Yesss! Validation!", "...Thank you. I prepared for it.", "The best one, huh? Keep the compliments coming."), 6),
                O("Tell her she looks pretty today.", "tm_pretty", close ? T(h, "E-eh?! You shouldn't say that to a teacher... b-but thank you...", "Wha-! You can't just say that! ...Hehe.", "...Inappropriate. ...Thank you.", "Oh? Careful, or I'll start believing you.") : T(h, "That's... not something to say to your teacher.", "Whoa there! Let's keep it professional!", "...Inappropriate.", "Nice try, kid."), close ? 4 : -3, close ? 3 : 0, close ? Feel.Shy : Feel.Pout),
                O("Say her hair looks fine as it is.", "tm_hair", T(h, "You think so? Thank you.", "Phew! Okay, good!", "...Hm. Good.", "Just fine? I was aiming for stunning."), 3),
            };
        }

        private static Option[] AfterHours(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("After the last bell, " + TeacherName(h) + " is still at school, stretching.");
            b.Say(h, T(h, "Oh, you're still here? Shouldn't you be going home?", "Still here? Me too! Teachers never go home, apparently!", "Still here. Why?", "Lingering after school? Waiting for someone?"));
            return new[]
            {
                O("Offer to walk her to the station.", "ta_walk", T(h, "Then... just to the station. Thank you.", "Escort service! Sure, let's go!", "...Fine. It's on the way.", "A walk home with a student... I'll allow it. Just this once."), 7, 1),
                O("Offer to help her grade.", "ta_grade", T(h, "You'd do that? Then I'll buy you a juice.", "Yes! Free labor! I mean, thank you!", "...Red pen. Don't grade your own.", "Help me grade? You just want to see the scores."), 6),
                O("Tease her about the staff room gossip.", "ta_gossip", T(h, "W-what gossip?! There's no gossip!", "Wait, what are they saying?! Tell me!", "...What did you hear.", "Ooh, gossip about me? Spill."), 3, 2, Feel.Surprised),
            };
        }

        private static Option[] Flirt(SaveData.Heroine h, SceneBuilder b)
        {
            b.Narr("The classroom is empty. " + TeacherName(h) + " sits on the edge of her desk, closer than usual.");
            b.Say(h, T(h, "It's quiet when everyone's gone... I like talking with you like this.", "Just us! It's like detention but fun!", "...It's quiet. I don't mind it, with you.", "Alone with a student after class... how scandalous."));
            var ready = h.favor >= 70;
            return new[]
            {
                O("Ask her out for coffee this weekend.", "tf_coffee", ready ? T(h, "A-as long as nobody from school sees us... okay.", "A date?! ...Shh! Yes!", "...Somewhere far from school.", "A date with a student? ...Pick somewhere nice.") : T(h, "Ah... that wouldn't be proper.", "Whoa, whoa! I'm your teacher!", "No.", "Cute. No."), ready ? 6 : -2, ready ? 2 : 0, ready ? Feel.Love : Feel.Worried),
                O("Lean a little closer.", "tf_lean", ready ? T(h, "Y-you're too close... ...I don't hate it, though...", "H-hey... you're kinda close... ...okay...", "...Close. ...Stay there.", "Bold. ...Closer, then.") : T(h, "Th-that's too close!", "Personal space! Personal space!", "Back up.", "Down, boy."), ready ? 3 : -4, ready ? 5 : 0, ready ? Feel.Shy : Feel.Pout),
                O("Behave and just keep her company.", "tf_behave", T(h, "Fufu... you're sweet.", "Aww, a good boy!", "...Thank you.", "Behaving? How boring. ...Nice, though."), 4),
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
                    b.Face(h, Feel.Smile).Say(h, T(h, "You know... outside of class, you don't have to be so formal with me. I like talking with you.", "Hey, outside class we're friends, okay? Deal? Deal!", "...Outside of class, you can talk to me normally. I'd like that.", "Outside of class, drop the 'sensei'. ...Within reason."));
                    opts = new[]
                    {
                        O("Say you'd like that too.", "tx_yes", T(h, "Then it's settled.", "Yay! Friends!", "...Good.", "Good. Now I'll know where to find you."), 5),
                        O("Joke that you'll still call her sensei.", "tx_sensei", T(h, "Mou... fine, sensei it is.", "Ugh, so stubborn! Fine!", "...Suit yourself.", "Respectful. Or teasing. I can't tell."), 3),
                    };
                    extra = c => { h.talkEvent.Add(1); Toasts.Show(name + " is your friend now: she talks with you anywhere at school."); Log(name + ": friend"); };
                    break;
                case "h":
                    b.Narr("The door of the empty classroom slides shut behind " + name + ".");
                    b.Face(h, Feel.Shy).Say(h, T(h, "I shouldn't feel this way about a student... but I can't stop thinking about you.", "This is SO against the rules... but I don't care anymore.", "...I've been thinking about this for a while. About you.", "I've decided to stop pretending. Come here."));
                    opts = new[]
                    {
                        O("Take her hand.", "tx_h", T(h, "...Lock the door.", "C-close the door! Quick!", "...The door. Lock it.", "Good answer. The door, please."), 6, 6, Feel.Love),
                        O("Say you feel the same, but not here.", "tx_wait", T(h, "...You're right. Thank you for being sensible.", "Aww. Okay... you're right. Later!", "...Right. Later.", "Sensible. Annoyingly sensible."), 5, 3, Feel.Smile),
                    };
                    extra = c => { h.talkEvent.Add(2); Log(name + ": H open (" + c + ")"); if (c == "tx_h") StartH(h); };
                    break;
                default:
                    b.Narr(name + " walks with you to the school gate, the way she now does most days.");
                    b.Bg("@gate");
                    b.Face(h, Feel.Love).Say(h, T(h, "Somewhere along the way, you stopped being just a student to me... I'm glad.", "You know what? You're my favorite person at this school. Don't tell the principal.", "...You matter to me. That's all.", "I've gotten attached to you. Your fault, entirely."));
                    opts = new[]
                    {
                        O("Tell her she matters to you too.", "tx_close", T(h, "...Thank you. Really.", "Hehe... okay. I'm happy!", "...I know.", "...I know. Say it again sometime."), 8, 2, Feel.Love),
                        O("Promise to visit her after you graduate.", "tx_promise", T(h, "I'll hold you to that.", "Pinky promise!", "...I'll wait.", "You'd better."), 8, 0, Feel.Smile),
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
