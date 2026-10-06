using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Something she wants to talk about, as a scene: who writes it (this file, the pairs, the rumors, the relationship
    /// seasons), how much she wants to (weight), the form it takes, and what each answer leaves behind.
    /// </summary>
    internal sealed class Subject
    {
        public string Id = "";
        public Form Form = Form.SceneTalk;
        public float Weight = 1f;
        public Action<SceneBuilder> Write;               // the whole scene: lines, choices, branches
        public Action<string> Result;                   // the branch taken (empty when the scene had no choice)
        public int Interest;                            // how it changes the conversation's mood (-2..2)
        public override string ToString() { return Form + ":" + Id; }
    }

    /// <summary>
    /// Conversation by scene, in the talk screen: when you open a talk, the director may start it with a scene instead of
    /// the menu. Scene Talk (a long moment: her request told properly, a hurt, feelings she circles around, a friend's
    /// romance, a rumor, her partner), Quiet (she is tired, stressed or sad: you can just sit with her), Triangle (someone
    /// else is standing there: her partner, her best friend, your girlfriend or her rival joins in) and Callback (she
    /// brings back something you went through together). An aside she said in passing can be answered here too. Forms
    /// used lately with her weigh less; most talks still open straight on the menu.
    /// </summary>
    internal static class SceneTalk
    {
        private static CampusState S { get { return Campus.S; } }
        public static string LastOpened = "";

        // ================= choosing =================

        /// <summary>Everything she could open with now (the talk screen is open with her).</summary>
        internal static List<Subject> Candidates(SaveData.Heroine h)
        {
            var l = new List<Subject>();
            var gs = S.Girl(GirlInfo.Key(h));
            Add(l, AsideReply(h));
            Add(l, Quiet(h));
            Add(l, Triangle(h));
            Add(l, Callback(h));
            Add(l, Reveal(h));
            Add(l, Hurt(h));
            Add(l, Feelings(h));
            Add(l, AfterH(h));
            try { foreach (var s in Pairs.Subjects(h)) Add(l, s); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            try { foreach (var s in Rumors.Subjects(h)) Add(l, s); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            try { foreach (var s in Seasons.Subjects(h)) Add(l, s); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            // A scene she has been waiting to have (set by the map forms, the director or a story) comes first.
            if (!string.IsNullOrEmpty(gs.Pending))
                foreach (var s in l)
                    if (gs.Pending == s.ToString() || gs.Pending.EndsWith(":" + s.Id, StringComparison.Ordinal) || gs.Pending.StartsWith(s.Form + ":", StringComparison.Ordinal) && s.Form != Form.SceneTalk)
                        s.Weight += 50f;
            foreach (var s in l) s.Weight *= Forms.Fresh(gs, s.Form);
            return l;
        }

        private static void Add(List<Subject> l, Subject s) { if (s != null && s.Weight > 0f && s.Write != null) l.Add(s); }

        /// <summary>The scene she opens with, or null (the menu straight away). An aside or a waiting scene always plays.</summary>
        internal static Subject Choose(SaveData.Heroine h)
        {
            var l = Candidates(h);
            if (l.Count == 0) return null;
            var forced = l.Where(s => s.Weight >= 40f || s.Form == Form.Aside).OrderByDescending(s => s.Weight).FirstOrDefault();
            if (forced != null) return forced;
            var none = 2.6f;
            var roll = Random.value * (l.Sum(s => s.Weight) + none);
            foreach (var s in l) { roll -= s.Weight; if (roll <= 0f) return s; }
            return null;
        }

        /// <summary>
        /// Called when the conversation menu opens the first time (<see cref="Conversation"/>): plays a scene and returns
        /// true, or returns false for the menu. <paramref name="interest"/> moves the talk's Interest.
        /// </summary>
        public static bool Open(SaveData.Heroine h, Action<int> interest, Action done)
        {
            if (!Cfg.ConversationForms.Value || h == null) return false;
            if (Pairs.IsScorned(h)) return PlayScorned(h, done);
            var s = Choose(h);
            if (s == null) return false;
            return Play(h, s, interest, done);
        }

        internal static bool Play(SaveData.Heroine h, Subject s, Action<int> interest, Action done)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var b = new SceneBuilder(h, true);
            try { s.Write(b); }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Scene talk " + s + ": " + e); return false; }
            gs.Pending = "";
            LastOpened = s + " (" + GirlInfo.FirstName(h) + ")";
            CampusLifePlugin.Log.LogInfo("Scene talk: " + LastOpened);
            Forms.Used(h, s.Form);
            return Scenes.PlayInTalk(b, c =>
            {
                try { if (s.Result != null) s.Result(c ?? ""); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                if (interest != null && s.Interest != 0) interest(s.Interest);
                Forms.Spend(h, s.Form);
                Social.RecordTalk(h);
                if (done != null) done();
            }, false);
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        private static void Choice(SceneBuilder b, List<string[]> opts)
        {
            var args = new List<string>();
            foreach (var o in opts.Take(4)) { args.Add(o[0]); args.Add(o[1]); }
            b.Choice(args.ToArray());
        }

        // ================= an aside, answered =================

        private static Subject AsideReply(SaveData.Heroine h)
        {
            var bit = Forms.Aside != null && Forms.Aside.Girl == GirlInfo.Key(h) && Time.realtimeSinceStartup <= Forms.Aside.Until ? Forms.Aside.Bit : null;
            if (bit == null) return null;
            return new Subject
            {
                Id = "aside", Form = Form.Aside, Weight = 100f,
                Write = b =>
                {
                    Forms.TakeAside(h);
                    b.Narr("You catch up with " + GirlInfo.FirstName(h) + " right after what she said.");
                    b.Her_(T(h, "Ah... you heard that? || Oh... you heard me? || Ah... did you hear that? || Um... you were listening?", "Oh! You heard me? || Whoa! You heard?! || Oops! You heard that?! || Hey! Listening in?!", "...You heard that. || ...You heard. || ...Listening, were you. || ...So you heard.", "Eavesdropping? || Spying on me? || Nosy, aren't you? || Listening in? Naughty."));
                    var opts = bit.Replies.Take(3).Select(r => new[] { r.Label, r.Tag }).ToList();
                    Choice(b, opts);
                    foreach (var r in bit.Replies.Take(3)) { b.Branch(r.Tag); b.Her_(r.Her); b.End(); }
                },
                Result = c => Forms.Apply(h, bit.Replies.FirstOrDefault(r => r.Tag == c), Form.Aside),
                Interest = 1,
            };
        }

        // ================= Quiet =================

        private static Subject Quiet(SaveData.Heroine h)
        {
            var mood = Forms.MoodOf(h);
            var gs = S.Girl(GirlInfo.Key(h));
            var waiting = gs.Pending.StartsWith("Quiet:", StringComparison.Ordinal);
            if (mood != Mood.Sleepy && mood != Mood.Stressed && mood != Mood.Upset && !waiting) return null;
            var name = GirlInfo.FirstName(h);
            var coffee = S.Count("canned_coffee") > 0 ? "canned_coffee" : S.Count("chocolate") > 0 ? "chocolate" : S.Count("daifuku") > 0 ? "daifuku" : null;
            return new Subject
            {
                Id = "quiet", Form = Form.Quiet, Weight = mood == Mood.Upset ? 3.5f : 2.5f,
                Write = b =>
                {
                    if (mood == Mood.Sleepy)
                    {
                        b.Narr(name + " is resting her chin on her hand, blinking slowly, like the whole day weighs on her eyelids.");
                        b.Face(h, Feel.Normal).Her_(T(h, "Ah... sorry. I'm not much fun today... || Sorry... I'm a bit dull today... || I'm sorry, I'm not much company today... || Ah... forgive me, I'm tired...", "Mmh... hey... I'm awake... mostly... || Mmh... awake... sorta... || Huh? I'm up! Kinda! || Zzz... wha? I'm here!", "...Mm. I'm here. Barely. || ...Here. Barely. || ...Present. Mostly. || ...Mm.", "If you want entertainment, come back tomorrow. || Come back tomorrow for fun. || Not entertaining today. || Try again tomorrow."));
                    }
                    else if (mood == Mood.Stressed)
                    {
                        b.Narr(name + " has a notebook open in front of her, and she has been staring at the same page for a while. Her pencil taps, taps, taps.");
                        b.Face(h, Feel.Worried).Her_(T(h, "There's so much to do... and I keep thinking I'll fail all of it. || So much to do... I'm afraid I'll fail. || Everything's piling up... I'm scared. || I can't keep up...", "Everything's due at once and my brain's on strike! || Everything's due! Brain's on strike! || Deadlines! All of them! Now! || Too much! Brain overload!", "...Too many things. Not enough hours. || ...Too many tasks. || ...Not enough time. || ...Overloaded.", "Don't ask. I'm one bad grade away from losing it. || One more bad grade and I snap. || Don't. I'm on the edge. || I'm about to break. Don't ask."));
                    }
                    else
                    {
                        b.Narr(name + " looks away when you come near. Her eyes are a little red, and her hands are holding each other too tightly.");
                        b.Face(h, Feel.Sad).Her_(T(h, "...Sorry. Today's just... a bad day. || ...Sorry. Bad day. || ...It's just a hard day. || ...Today's not good.", "I'm fine! Totally! ...No. Not really. || I'm fine! ...Not really. || Totally okay! ...No. || All good! ...Nope.", "...Don't. I'll be fine. || ...I'll be fine. || ...Don't. || ...I'm managing.", "...You picked a terrible time. ...Stay, though. || ...Bad timing. Stay, though. || ...Terrible moment. Don't go. || ...You picked a bad day. Stay."));
                    }
                    var opts = new List<string[]> { new[] { "Sit next to her without a word", "q_sit" }, new[] { "Ask gently what happened", "q_ask" } };
                    if (coffee != null) opts.Add(new[] { "Leave your " + Episodes.ItemName(coffee) + " by her hand", "q_item" });
                    opts.Add(new[] { "Try to make her laugh", "q_joke" });
                    Choice(b, opts);
                    b.Branch("q_sit");
                    b.Narr("You sit down beside her. Neither of you says anything. Outside, someone laughs in the hallway; a bird lands on the window frame and leaves again.");
                    b.Narr("After a while, her shoulders come down a little. She lets out a long breath.");
                    b.Face(h, Feel.Smile).Her_(T(h, "...Thank you. I didn't want to talk. I just didn't want to be alone. || ...Thank you. I just didn't want to be alone. || ...You stayed. That's all I needed. || ...Thank you for being here.", "...Hey. That was... really nice. Thanks for not asking. || ...That was nice. Thanks for not asking. || ...Thanks. Really. || ...Hey. That helped.", "...You're good at silence. That's rare. || ...Good at silence. Rare. || ...Quiet company. Thanks. || ...You know when to be silent.", "...Don't tell anyone I liked that. || ...Don't tell anyone I liked that. || ...Secret. I liked it. || ...Nobody hears about this."));
                    b.End();
                    b.Branch("q_ask");
                    b.Me(Lines.Pick("You don't have to tell me. But I'm listening, if you want.", "What happened? You can tell me."));
                    b.Face(h, Feel.Worried).Her_(ConvoLines.Vent(GirlInfo.ToneOf(h), mood == Mood.Sleepy ? Mood.Stressed : mood));
                    b.Face(h, Feel.Smile).Her_(ConvoLines.VentThanks(GirlInfo.ToneOf(h)));
                    b.End();
                    if (coffee != null)
                    {
                        b.Branch("q_item");
                        b.Narr("You set the " + Episodes.ItemName(coffee) + " down next to her hand and turn to go. Behind you, a small sound: she picked it up.");
                        b.Face(h, Feel.Shy).Her_(T(h, "...You're too kind. I'll pay you back. Someday. || ...Too kind. I'll repay you. || ...Thank you so much. Someday I'll return it. || ...You're so good to me.", "...Hey! ...Thanks. Seriously. || ...Hey! Thanks! Seriously! || ...Wow. Thanks! || ...Thank you!", "...I'll remember this. || ...Remembered. || ...I won't forget. || ...Noted. Thanks.", "...Smooth. ...Thank you. || ...Smooth. Thanks. || ...Slick. Thank you. || ...Nice move. Thanks."));
                        b.End();
                    }
                    b.Branch("q_joke");
                    var works = mood != Mood.Upset ? Random.value < 0.6f : Random.value < 0.3f;
                    b.Me(ConvoLines.PlayerJoke());
                    if (works) b.Face(h, Feel.Laugh).Her_(T(h, "Pfft... that's so stupid. ...Thanks. I needed that. || Pfft... so silly. Thanks. || Hehe... that's dumb. I needed it. || Ahaha... thank you.", "Bahaha! Okay, okay, I'm alive again! || Bahaha! Alive again! || HA! I'm back! || Ahaha! Revived!", "...That was terrible. ...I'm smiling. Don't look. || ...Terrible. ...Smiling. Don't look. || ...Awful joke. ...Heh. || ...Stupid. ...Thanks.", "...Ugh. You made me laugh. Unforgivable. || Ugh. You made me laugh. || I laughed. Unforgivable. || Fine. That was funny."));
                    else b.Face(h, Feel.Pout).Her_(T(h, "...Not now. Sorry. || ...Not now. || ...Sorry, not now. || ...Please, not now.", "...Heh. Not really in the mood. || ...Not in the mood. || ...Heh. No. || ...Nah.", "...No. || ...No. || ...Not now. || ...Stop.", "...Read the room. || ...Read the room. || ...Bad timing. || ...Not the time."));
                    b.End();
                    gs.Pending = works ? "" : gs.Pending;
                    _quietJoke = works;
                },
                Result = c =>
                {
                    if (c == "q_sit") { Social.AddTrust(h, 4); Campus.Apply(new Rewards { Favor = 4 }, h); Memories.Add(h, "quiet", "you sat with me without saying anything"); if (mood != Mood.Sleepy) gs.Mood = Mood.None; }
                    else if (c == "q_ask") { Social.AddTrust(h, 3); Campus.Apply(new Rewards { Favor = 3 }, h); gs.VentDay = S.Day; if (Random.value < 0.6f) gs.Mood = Mood.None; }
                    else if (c == "q_item") { S.AddItem(coffee, -1); Social.AddTrust(h, 2); Campus.Apply(new Rewards { Favor = 6 }, h); Memories.Add(h, "quiet", "you left me your " + Episodes.ItemName(coffee) + " when I was down"); }
                    else if (c == "q_joke") { if (_quietJoke) { Campus.Apply(new Rewards { Favor = 3 }, h); gs.Mood = Mood.Cheerful; } else Social.AddTrust(h, -1); }
                },
                Interest = 1,
            };
        }

        private static bool _quietJoke;

        // ================= Triangle =================

        private static Subject Triangle(SaveData.Heroine h)
        {
            var act = G.Act;
            if (act == null || act.Player == null) return null;
            var p = act.Player.transform.position;
            var near = act.npcList.Where(n => n != null && n.heroine != null && n.heroine != h && n.isActive && n.mapNo == G.PlayerMapNo && GirlInfo.IsRegular(n.heroine)
                                              && (n.transform.position - p).sqrMagnitude < 7f * 7f).Select(n => n.heroine).ToList();
            if (near.Count == 0) return null;
            var key = GirlInfo.Key(h);
            SaveData.Heroine w = null;
            var kind = "";
            foreach (var o in near)
            {
                var pair = S.FindPair(key, GirlInfo.Key(o));
                if (pair != null && (pair.Stage == PairStage.Couple || pair.Stage == PairStage.Strained)) { w = o; kind = "partner"; break; }
            }
            if (w == null && !h.isGirlfriend) { w = near.FirstOrDefault(o => o.isGirlfriend && !S.Harem.Contains(GirlInfo.Key(o))); if (w != null) kind = "gf"; }
            if (w == null) { w = near.FirstOrDefault(o => Social.Bond(h, o) >= 50); if (w != null) kind = "friend"; }
            if (w == null) { w = near.FirstOrDefault(o => Social.Bond(h, o) <= -30); if (w != null) kind = "rival"; }
            if (w == null) return null;
            var wit = w;
            var hn = GirlInfo.FirstName(h);
            var wn = GirlInfo.FirstName(wit);
            var tw = GirlInfo.ToneOf(wit);
            return new Subject
            {
                Id = "tri_" + kind, Form = Form.Triangle, Weight = kind == "friend" ? 1.6f : 2.4f,
                Write = b =>
                {
                    if (kind == "partner")
                    {
                        b.Narr(wn + " walks up and stands close to " + hn + ", a little closer than a friend would.");
                        b.Say(wit, Lines.T(tw, "Ah, you're talking with " + hn + "? I'll just... wait here. || You're talking with " + hn + "? I'll wait. || Oh, " + hn + "... I'll wait here. || Ah, sorry... I'll wait for " + hn + ".", "Heyyy! What are you two talking about?! || What are you two talking about?! || Heyyy! Gossip?! || Ooh! What's up, you two?!", "...Don't mind me. I'm with her. || ...I'm with her. || ...Don't mind me. || ...Waiting for her.", "Oh? Chatting up my " + hn + "? Bold. || Chatting up my " + hn + "? Bold. || Flirting with " + hn + "? Brave. || Oh? Talking to my " + hn + "?"));
                        b.Face(h, Feel.Shy).Her_(T(h, "W-we were just talking... || W-we were only talking... || Just chatting...! || N-nothing happened...", "Hehe, we were just chatting! || Just chatting! || Talking! Hehe! || Only talking!", "...Relax. || ...Relax. || ...Calm down. || ...It's nothing.", "Jealous? Cute. || Jealous? Cute. || Aww, jealous? || Envious?"));
                        Choice(b, new List<string[]> { new[] { "Say they make a great couple", "t_bless" }, new[] { "Tease them both", "t_tease" }, new[] { "Excuse yourself and leave them be", "t_leave" } });
                        b.Branch("t_bless"); b.Say(wit, Lines.T(tw, "R-really? ...Thank you. || Really? ...Thank you. || You think so? Thanks... || Oh... thank you.", "Right?! We do! || Right?! We do! || Yes! Thanks! || Totally!", "...Good eye. || ...Good eye. || ...Observant. || ...Correct.", "Obviously. || Obviously. || Of course. || Naturally.")); b.Face(h, Feel.Love).Her_(T(h, "...Hehe. || ...Hehe. || ...Ehehe. || ...Fufu.", "Hehe! || Hehe! || Haha! || Hee!", "...Mm. || ...Mm. || ...Heh. || ...", "...Heh. || ...Heh. || ...Hm. || ...Fufu.")); b.End();
                        b.Branch("t_tease"); b.Say(wit, Lines.T(tw, "S-stop it...! || S-stop...! || Don't tease! || Please stop...!", "Hey! No teasing! || No teasing! || Hey! Quit it! || Stop!", "...Childish. || ...Childish. || ...Immature. || ...Stop.", "Watch it. || Watch it. || Careful. || Don't push it.")); b.End();
                        b.Branch("t_leave"); b.Face(h, Feel.Smile).Her_(T(h, "Ah, see you later! || See you later! || Bye! || Take care!", "Later! || Later! || Bye bye! || See ya!", "...Bye. || ...Bye. || ...Later. || ...Goodbye.", "Bye~ || Bye~ || Ciao~ || Later~")); b.End();
                    }
                    else if (kind == "gf")
                    {
                        b.Narr("From the corner of your eye you see " + wn + " - your girlfriend - stop a few steps away, arms folded, watching.");
                        b.Say(wit, Lines.T(tw, "...Oh. You're busy. || ...You're busy. || ...Oh. Never mind. || ...I see.", "Ahem! Hellooo? || Hellooo?! || Ahem! || Excuse me?!", "...Don't mind me. || ...Don't mind me. || ...Carry on. || ...Hm.", "Having fun? || Having fun? || Enjoying yourself? || Busy, huh?"));
                        b.Face(h, Feel.Surprised).Her_(T(h, "Ah... I didn't mean to take him from you! || I didn't mean to take him! || Sorry! I wasn't stealing him! || Ah, forgive me...!", "Oops! Is that your girlfriend?! || Oops! Your girlfriend?! || Whoa! Girlfriend alert! || Uh oh! Busted!", "...Awkward. || ...Awkward. || ...Uncomfortable. || ...Hm.", "Uh oh. Someone's jealous~ || Someone's jealous~ || Uh oh~ || Oops, jealousy~"));
                        Choice(b, new List<string[]> { new[] { "Introduce them to each other", "t_intro" }, new[] { "Keep it short with " + hn, "t_short" }, new[] { "Take " + wn + "'s hand", "t_hand" } });
                        b.Branch("t_intro"); b.Say(wit, Lines.T(tw, "...Nice to meet you. || ...Nice to meet you. || ...Hello. || ...Pleased to meet you.", "Hi! I'm the girlfriend! Hehe. || Hi! Girlfriend here! || Hey! I'm his girlfriend! || Hiya! The girlfriend!", "...Hello. || ...Hello. || ...Hi. || ...Greetings.", "Charmed. || Charmed. || Delighted. || Hello there.")); b.Her_(T(h, "Nice to meet you too! || Nice to meet you too! || Hello! || Pleased to meet you!", "Hiya! || Hiya! || Hey! || Hi hi!", "...Likewise. || ...Likewise. || ...Same. || ...Hello.", "So you're the lucky one. || The lucky one. || So you're her. || Lucky girl.")); b.End();
                        b.Branch("t_short"); b.Her_(T(h, "Ah... see you, then. || See you, then. || Bye, then... || Goodbye...", "Okay, okay, bye! || Bye! || Okay, bye! || See ya!", "...Bye. || ...Bye. || ...Later. || ...Farewell.", "Run along~ || Run along~ || Off you go~ || Shoo~")); b.End();
                        b.Branch("t_hand"); b.Say(wit, Lines.T(tw, "...Hehe. Okay. || ...Hehe. Okay. || ...Mm. Good. || ...Okay.", "Hehe! Mine! || Hehe! Mine! || Mine! || Yay! Mine!", "...Good. || ...Good. || ...Right. || ...Correct.", "Good answer. || Good answer. || Correct. || Smart.")); b.Face(h, Feel.Pout).Her_(T(h, "...Sweet. Bye. || ...Sweet. Bye. || ...How lovely. Bye. || ...Cute. See you.", "Aww. Lovebirds. Bye! || Lovebirds! Bye! || Aww! Bye! || So cute! Later!", "...I see. || ...I see. || ...Understood. || ...Hm.", "Show-offs. || Show-offs. || Cute. Gross. || Get a room.")); b.End();
                    }
                    else if (kind == "friend")
                    {
                        b.Narr(wn + " pops up behind " + hn + " and drapes an arm over her shoulders.");
                        b.Say(wit, Lines.T(tw, "Hehe, what are you two whispering about? || What are you whispering? || Hehe, secrets? || What are you two up to?", "Ooh! Am I interrupting something?! || Interrupting?! || Ooh! What's this?! || Am I in the way?!", "...So this is where you went. || ...So this is where. || ...Found you. || ...Here you are.", "Oho? Are you two a thing now? || Are you two a thing? || Oho? Something going on? || A couple now?"));
                        b.Face(h, Feel.Shy).Her_(T(h, "N-no! We were just talking! || N-no! Just talking! || No, no! It's nothing! || We weren't...!", "Wha-! It's not like that! || It's not like that! || Wha-! No! || Nope! Not that!", "...Don't. || ...Don't. || ...Stop. || ...No.", "Wouldn't you like to know~ || Wouldn't you like to know~ || Maybe~ || Who knows~"));
                        Choice(b, new List<string[]> { new[] { "Play along: \"Maybe we are\"", "t_play" }, new[] { "Laugh it off", "t_deny" }, new[] { "Invite " + wn + " to join you", "t_join" } });
                        b.Branch("t_play"); b.Face(h, Feel.Love).Her_(T(h, "Eh?! W-what are you saying...! || Eh?! What are you saying...! || W-what?! || Ah...! Don't say that!", "Hey! Don't encourage her! ...Hehe. || Don't encourage her! ...Hehe. || Hey! ...Heh! || Stop! ...Hehe.", "...You're both impossible. || ...Impossible, both of you. || ...Ridiculous. || ...Hopeless.", "Oh? Interesting answer. || Interesting answer. || Oh? Bold. || Ooh.")); b.Say(wit, Lines.T(tw, "Kyaa! || Kyaa! || Eek! || Aah!", "Ooooh! || Ooooh! || Whoa! || Woo!", "...Noted. || ...Noted. || ...Hm. || ...I see.", "I knew it! || Knew it! || Called it! || Ha!")); b.End();
                        b.Branch("t_deny"); b.Her_(T(h, "See? Just talking. || See? Just talking. || Only talking. || See?", "See! Nothing! || See! Nothing! || Told you! || Nothing!", "...Told you. || ...Told you. || ...Obviously. || ...As I said.", "Boring answer. || Boring. || Dull answer. || Yawn.")); b.End();
                        b.Branch("t_join"); b.Say(wit, Lines.T(tw, "Can I? Yay. || Can I? Yay. || Really? Yay! || I can join? Hehe.", "Don't mind if I do! || Don't mind if I do! || Scoot over! || Me too!", "...Fine. || ...Fine. || ...Alright. || ...Okay.", "Three's company. || Three's company. || A trio. Fun. || Room for me.")); b.Her_(T(h, "Hehe, the more the merrier. || The more the merrier. || Hehe, sure. || Of course!", "Party! || Party! || Woo! || Yay!", "...Fine. || ...Fine. || ...Okay. || ...Sure.", "Hm. Okay. || Hm. Okay. || Fine. || Sure.")); b.End();
                    }
                    else
                    {
                        b.Narr(wn + " walks past, slows down, and looks " + hn + " up and down.");
                        b.Say(wit, Lines.T(tw, "...Oh. It's you. || ...It's you. || ...Oh. You. || ...Hello.", "Hah! Look who it is! || Look who it is! || Hah! You! || Oh, great. You!", "...Hmph. || ...Hmph. || ...Tch. || ...You.", "Wow. Still wearing that? || Still wearing that? || Oh, it's you. Sad. || Wow. You again."));
                        b.Face(h, Feel.Pout).Her_(T(h, "...Please just leave me alone. || ...Leave me alone. || ...Please go. || ...Not now.", "What's your problem?! || What's your problem?! || What do you want?! || Go away!", "...Keep walking. || ...Keep walking. || ...Move along. || ...Leave.", "Oh look, it talks. || It talks. || Oh, it speaks. || How charming."));
                        var opts = new List<string[]> { new[] { "Stand up for " + hn, "t_defend" }, new[] { "Stay out of it", "t_out" } };
                        if (G.Int >= 40 || G.Sens >= 40) opts.Add(new[] { "Defuse it with a joke " + (G.Int >= G.Sens ? "[INT]" : "[SEN]"), "t_defuse" });
                        Choice(b, opts);
                        b.Branch("t_defend"); b.Say(wit, Lines.T(tw, "...Fine. Whatever. || ...Fine. || ...Whatever. || ...Okay.", "Tch. Whatever! || Tch! Whatever! || Fine! Hmph! || Ugh!", "...Noted. || ...Noted. || ...Hm. || ...Fine.", "Knight in shining armor, huh. || Knight in armor, huh. || Hero mode? Cute. || A white knight. How quaint.")); b.Face(h, Feel.Smile).Her_(T(h, "...Thank you. || ...Thank you. || ...Thanks. || ...I appreciate it.", "Thanks! Seriously! || Thanks! || Thank you! || You rock!", "...Thanks. || ...Thanks. || ...Appreciated. || ...Good.", "...Not bad. || ...Not bad. || ...Decent. || ...Okay, thanks.")); b.End();
                        b.Branch("t_out"); b.Narr(wn + " smirks and walks on. " + hn + " stares at the floor."); b.End();
                        b.Branch("t_defuse"); b.Narr("You say something silly enough that both of them snort despite themselves."); b.Say(wit, Lines.T(tw, "...Hmph. Whatever. Bye. || ...Hmph. Bye. || ...Whatever. Goodbye. || ...Fine. Bye.", "Pfft. Okay, fine. || Pfft. Fine. || Okay, okay. || Hmph. Sure.", "...Hm. || ...Hm. || ...Tch. || ...Fine.", "...Cheap shot. Funny, though. || ...Cheap shot. Funny. || ...Low blow. I laughed. || ...Okay, that was good.")); b.End();
                    }
                },
                Result = c =>
                {
                    var ka = GirlInfo.Key(h);
                    var kw = GirlInfo.Key(wit);
                    switch (c)
                    {
                        case "t_bless": Pairs.Nudge(ka, kw, 6, 4); G.AddFavor(h, 3, 0); G.AddFavor(wit, 3, 0); Memories.Add(h, "triangle", "you said " + wn + " and I make a great couple", 1, true, kw); break;
                        case "t_tease": G.AddFavor(h, 1, 0); Pairs.Nudge(ka, kw, 2, 0); break;
                        case "t_leave": Social.AddTrust(wit, 2); break;
                        case "t_intro": Social.AddTrust(wit, 2); Arcs.AddBond(ka, kw, 5); Arcs.AddBond(kw, ka, 5); break;
                        case "t_short": Social.AddTrust(wit, 3); G.AddFavor(h, -1, 0); break;
                        case "t_hand": G.AddFavor(wit, 5, 0); Social.AddTrust(wit, 3); if (Cfg.DevotionEnabled.Value) Devotion.Add(wit, 2); break;
                        case "t_play": G.AddFavor(h, 3, 2); Pairs.OnTeasedTogether(h, wit); break;
                        case "t_deny": break;
                        case "t_join": G.AddFavor(wit, 3, 0); G.AddFavor(h, 1, 0); Arcs.AddBond(ka, kw, 3); Arcs.AddBond(kw, ka, 3); Social.RecordTalk(wit); break;
                        case "t_defend": Social.Add(Social.Kinds.Defended, h, new[] { wit }); Social.AddTrust(wit, -3); Memories.Add(h, "triangle", "you stood up for me when " + wn + " was being mean", 1); break;
                        case "t_out": Social.AddTrust(h, -2); break;
                        case "t_defuse": Social.AddTrust(h, 2); Social.AddTrust(wit, 1); Arcs.AddBond(ka, kw, 4); Arcs.AddBond(kw, ka, 4); break;
                    }
                },
                Interest = kind == "rival" ? 0 : 1,
            };
        }

        // ================= Callback =================

        private static Subject Callback(SaveData.Heroine h)
        {
            var m = Memories.Callback(h);
            if (m == null) return null;
            var name = GirlInfo.FirstName(h);
            var sore = m.Tone < 0;
            return new Subject
            {
                Id = "callback", Form = Form.Callback, Weight = 3f,
                Write = b =>
                {
                    Memories.Mentioned(m);
                    b.Narr(name + " fiddles with her sleeve for a moment before she speaks.");
                    if (sore)
                    {
                        b.Face(h, Feel.Pout).Her_(T(h, "About " + Memories.When(m) + ", when " + m.Text + "... I've been thinking about it. It stung a little." + " || " + Memories.When(m) + ", when " + m.Text + "... it hurt a little. || About " + Memories.When(m) + "... when " + m.Text + ". I still think about it. || When " + m.Text + ", " + Memories.When(m) + "... it stung.",
                            "Hey. " + Memories.When(m) + "? When " + m.Text + "? Yeah, that wasn't great." + " || " + Memories.When(m) + "! When " + m.Text + "! Not cool! || Hey! " + Memories.When(m) + ", " + m.Text + "! Remember?! || So, " + Memories.When(m) + "... " + m.Text + ". Yeah. Not great.",
                            "I haven't forgotten " + Memories.When(m) + ". When " + m.Text + "." + " || " + Memories.When(m) + ". When " + m.Text + ". I remember. || I recall " + Memories.When(m) + ". " + m.Text + "." + " || " + m.Text + ", " + Memories.When(m) + ". Not forgotten.", "So. " + Memories.When(m) + ". When " + m.Text + ". We should talk about that." + " || " + Memories.When(m) + ". " + m.Text + ". Let's discuss. || About " + m.Text + ", " + Memories.When(m) + ". Talk. || Remember " + Memories.When(m) + "? When " + m.Text + "? I do."));
                        Choice(b, new List<string[]> { new[] { "Say you're sorry it went like that", "c_sorry" }, new[] { "Explain how you saw it", "c_explain" }, new[] { "Promise to make it up to her", "c_promise" } });
                        b.Branch("c_sorry"); b.Face(h, Feel.Smile).Her_(T(h, "...Okay. Thank you for saying it. || ...Thank you for saying it. || ...Okay. I forgive you. || ...Thank you.", "...Okay! Clean slate! || Clean slate! || Okay! Fresh start! || All good now!", "...Accepted. || ...Accepted. || ...Fine. || ...Resolved.", "...Fine. You're forgiven. || ...Forgiven. || ...Fine. Forgiven. || ...Okay. You're off the hook."));  b.End();
                        b.Branch("c_explain"); b.Face(h, Feel.Normal).Her_(T(h, "Oh... I didn't see it that way. Okay. || I didn't see it like that... okay. || Oh... I understand now. || Ah... I see.", "Huh. Okay, that makes sense. || Huh. Makes sense. || Oh! Okay! || Ahh, got it!", "...Fair. || ...Fair. || ...Reasonable. || ...Understood.", "...Hm. Decent excuse. || ...Decent excuse. || ...Acceptable. || ...Fine, fair.")); b.End();
                        b.Branch("c_promise"); b.Face(h, Feel.Smile).Her_(T(h, "...I'll hold you to that. || ...I'll hold you to that. || ...I'll remember. || ...Promise?", "You better! || You better! || Don't forget! || Promise!", "...I'll remember. || ...Remembered. || ...Noted. || ...We'll see.", "Big words. Let's see. || Big words. || Prove it. || Let's see.")); b.End();
                    }
                    else
                    {
                        b.Face(h, Feel.Smile).Her_(T(h, "Hey... do you remember " + Memories.When(m) + "? When " + m.Text + "? I keep thinking about it. || Do you remember " + Memories.When(m) + ", when " + m.Text + "? I treasure it." + " || " + Memories.When(m) + "... when " + m.Text + ". I still smile about it. || I keep remembering " + Memories.When(m) + ", when " + m.Text + "...",
                            "Remember " + Memories.When(m) + "?! When " + m.Text + "?! That was the best!" + " || " + Memories.When(m) + "! When " + m.Text + "! Best day! || Remember " + m.Text + "? " + Memories.When(m) + "! So fun! || Oh! " + Memories.When(m) + ", " + m.Text + "! Awesome!",
                            "..." + Memories.When(m) + ". When " + m.Text + ". I wanted to say... it meant something. || ..." + Memories.When(m) + ". " + m.Text + ". It mattered. || ...When " + m.Text + ". " + Memories.When(m) + ". I remember. || ..." + m.Text + ", " + Memories.When(m) + ". Important to me.",
                            "So, " + Memories.When(m) + ", when " + m.Text + "... Don't think I forgot." + " || " + Memories.When(m) + ", " + m.Text + ". Still on my mind. || I didn't forget " + m.Text + ". " + Memories.When(m) + "." + " || " + m.Text + "? " + Memories.When(m) + "? I remember everything."));
                        b.Face(h, Feel.Shy).Her_(T(h, "It's a small thing, but... it made my whole day. || Small, but it made my day. || It was little... but it meant so much. || Such a small thing... I was so happy.", "Honestly it made my week! Don't tell anyone! || Made my week! Secret! || Best thing all week! || It was awesome! Shh!", "...Small things matter. To me. || ...Small things matter. || ...It counted. || ...I noticed it.", "It was sweet. There. I said it. || It was sweet. There. || Fine. It was cute. || Sweet. I admit it."));
                        var opts = new List<string[]> { new[] { "Say you'd do it again anytime", "c_again" }, new[] { "Ask what it meant to her", "c_meant" }, new[] { "Joke that you're keeping score", "c_joke" } };
                        if (m.Kind == "walk" || m.Kind == "sunday" || m.Kind == "quiet") opts.Add(new[] { "Make it a habit of yours", "c_habit" });
                        Choice(b, opts);
                        b.Branch("c_again"); b.Face(h, Feel.Love).Her_(T(h, "...Then I'll let you. || ...Then I will. || ...Okay. I'll let you. || ...Alright.", "Deal! Anytime means anytime! || Deal! Anytime! || Okay! Promise! || Anytime! Got it!", "...I'll hold you to that. || ...Holding you to it. || ...Remembered. || ...We'll see.", "Careful. I'll take you up on that. || I'll take you up on it. || Careful. I'll accept. || Don't regret that.")); b.End();
                        b.Branch("c_meant"); b.Face(h, Feel.Shy).Her_(T(h, "That someone noticed me... that's all. It's a lot, for me. || That someone noticed me... that's a lot. || You saw me... that means everything. || Being noticed... it's precious to me.", "That you're fun! And nice! Ugh, don't make me say it! || You're fun and nice! Don't make me repeat it! || That you're awesome! Ugh! || That you're great! There!", "...That you pay attention. Most people don't. || ...You pay attention. Rare. || ...You notice. Most don't. || ...Attentive. That's it.", "...That you're not as dense as you look. || ...You're less dense than you look. || ...You're sharper than I thought. || ...You're not totally hopeless.")); b.End();
                        b.Branch("c_joke"); b.Face(h, Feel.Laugh).Her_(T(h, "Hehe... then I'm in your debt. || Then I owe you. || Hehe... in your debt. || I'll repay you.", "Hey! No scorekeeping! || No scorekeeping! || Hey! No counting! || Nope! No points!", "...Then I'm ahead. || ...I'm ahead. || ...My lead. || ...I'm winning.", "Then you're losing. Badly. || You're losing. Badly. || I'm way ahead. || You're behind.")); b.End();
                        b.Branch("c_habit"); b.Face(h, Feel.Love).Her_(T(h, "A habit... of ours? ...I'd like that. || A habit of ours... I'd like that. || Our own little habit... yes. || I'd love that.", "Our thing! We have a thing now! || Our thing! We have one! || Tradition! Ours! || Yes! Our thing!", "...A tradition. Fine. || ...A tradition. Fine. || ...Agreed. || ...Ours, then.", "Our little ritual, huh? ...Sure. || Our ritual? Sure. || A habit? Fine. || Our thing. Okay.")); b.End();
                    }
                },
                Result = c =>
                {
                    switch (c)
                    {
                        case "c_sorry": Social.AddTrust(h, 4); Campus.Apply(new Rewards { Favor = 2 }, h); break;
                        case "c_explain": Social.AddTrust(h, G.Int >= 40 ? 4 : 2); break;
                        case "c_promise": Social.AddTrust(h, 2); Pending(h, "Callback:callback"); break;
                        case "c_again": Campus.Apply(new Rewards { Favor = 4 }, h); Social.AddTrust(h, 2); break;
                        case "c_meant": Social.AddTrust(h, 4); Campus.Apply(new Rewards { Favor = 2 }, h); break;
                        case "c_joke": Campus.Apply(new Rewards { Favor = 2 }, h); break;
                        case "c_habit": Campus.Apply(new Rewards { Favor = 3 }, h); Seasons.AddTradition(h, m.Kind == "walk" ? "walks after class" : m.Kind == "quiet" ? "quiet breaks together" : "Sunday outings"); break;
                    }
                },
                Interest = sore ? 0 : 1,
            };
        }

        private static void Pending(SaveData.Heroine h, string what) { S.Girl(GirlInfo.Key(h)).Pending = what; }

        // ================= Scene Talk: her problem, told properly =================

        private static Subject Reveal(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            var r = Board.HiddenFor(key).FirstOrDefault();
            if (r == null || Social.Trust(h) < 15) return null;
            var tone = GirlInfo.ToneOf(h);
            var name = GirlInfo.FirstName(h);
            return new Subject
            {
                Id = "reveal", Weight = 1.8f,
                Write = b =>
                {
                    b.Narr(name + " glances around before she speaks, as if checking that no one else is listening.");
                    b.Face(h, Feel.Worried).Her_(T(h, "Can I tell you something? I didn't want to bother anyone with it, but... || Can I tell you something? I didn't want to bother anyone... || Um... I have a small problem... could you listen? || I need to tell someone... is that okay?", "Okay, so, um. Don't laugh, okay? I kinda need help. || Don't laugh! I need help! || Okay, um! Help, please?! || So! Problem! Don't laugh!",
                        "I'll say this once. I have a problem, and I'd rather not deal with it alone. || Once only. I have a problem. || I need assistance. Listen. || A problem. I'd rather not face it alone.", "Listen. I'm only telling you because you're... reliable. Sort of. || You're reliable. Sort of. Listen. || I'm telling you because you're useful. || Listen. You're the only one I'd ask."));
                    b.Face(h, Feel.Normal).Her_(Board.ProblemFirst(r, tone));
                    b.Face(h, Feel.Shy).Her_(T(h, "...Sorry. It's silly, isn't it? || ...Silly, isn't it? || ...Sorry. It's foolish. || ...I'm being dumb, right?", "It's dumb, right? Ugh. || Dumb, right?! Ugh! || Silly! I know! || Stupid, huh?", "...That's all. || ...That's all. || ...Done. || ...That's it.", "Well? Don't just stand there. || Well? Say something. || Don't just stare. || Respond."));
                    Choice(b, new List<string[]> { new[] { "\"I'll help. Leave it to me.\"", "r_help" }, new[] { "Ask her to tell you everything", "r_more" }, new[] { "Say it isn't silly at all", "r_kind" } });
                    b.Branch("r_help"); b.Face(h, Feel.Smile).Her_(T(h, "Really? ...Thank you. I feel lighter already. || Really? I feel lighter. || Thank you... that helps. || You'll help? Thank you...", "You will?! You're the best! || You will?! Best! || Yes! Thanks! || Awesome! You rock!", "...Good. I'm counting on you. || ...Counting on you. || ...Good. || ...Thank you.", "Knew it. Thanks. || Knew it. Thanks. || Obviously. Thanks. || Good. Thanks.")); b.End();
                    b.Branch("r_more");
                    b.Narr("She tells you the whole thing - more than she meant to, you think. Somewhere in the middle she starts to smile.");
                    b.Face(h, Feel.Smile).Her_(T(h, "...It's nice that you actually listen. || ...You really listen. || ...Thank you for listening. || ...It's nice to be heard.", "Wow, I talked forever! Sorry! ...Thanks. || I talked forever! Thanks! || Sorry! Long story! Thanks! || Blah blah! Thanks!", "...You listen well. || ...Good listener. || ...You listen. || ...Thanks.", "You're a good listener. Annoyingly. || Good listener. Annoying. || You listen well. Irritating. || You actually listened. Wow."));
                    b.End();
                    b.Branch("r_kind"); b.Face(h, Feel.Shy).Her_(T(h, "...You're kind. I'm glad I told you. || ...Glad I told you. || ...You're kind. || ...Thank you.", "...Hehe. Okay. Thanks. || ...Hehe. Thanks. || ...Okay! Thanks! || ...Cool. Thanks.", "...Hm. Thanks. || ...Hm. Thanks. || ...Thanks. || ...Appreciated.", "...Sweet talker. || ...Sweet talker. || ...Smooth. || ...Charmer.")); b.End();
                },
                Result = c =>
                {
                    Board.Reveal(r, "her");
                    Social.AddTrust(h, c == "r_more" ? 4 : 2);
                    if (c == "r_kind") Campus.Apply(new Rewards { Favor = 3 }, h);
                    Memories.Add(h, "scene", "I told you about my problem", 1, false);
                },
                Interest = 1,
            };
        }

        // ================= Scene Talk: a hurt =================

        private static Subject Hurt(SaveData.Heroine h)
        {
            var g = Social.Grudge(h);
            if (g == null) return null;
            var about = Social.Get(g.Girl);
            var self = about == h;
            var name = GirlInfo.FirstName(h);
            var an = about != null ? GirlInfo.FirstName(about) : "someone";
            return new Subject
            {
                Id = "hurt", Weight = 2.6f,
                Write = b =>
                {
                    b.Narr(name + " doesn't smile when she sees you. She waits until you're close.");
                    b.Face(h, Feel.Serious).Her_(ConvoLines.Incident(GirlInfo.ToneOf(h), g.Kind, an, self, !self && about != null && Social.Bond(h, about) >= 35, g.What, Social.Saw(h, g)));
                    b.Face(h, Feel.Sad).Her_(self
                        ? T(h, "I kept telling myself it didn't matter. But it did. It still does. || I told myself it didn't matter. It did. || I tried to forget... I couldn't. || It still hurts... I can't pretend.", "I tried to shrug it off! I couldn't! So now you get to hear it! || Tried to shrug it off! Couldn't! || I can't let it go! So here! || It bugged me! So listen!", "I don't hold grudges. Usually. This one stuck. || No grudges. Usually. This one stuck. || I don't hold grudges. This one, I do. || It lingered. I'll say it.", "I was going to let you sweat. But I'd rather just say it. || I wanted you to sweat. Easier to say it. || Could've made you squirm. I'll just say it. || I'll skip the games. Listen.")
                        : T(h, an + " is my friend. When she's hurt, I'm hurt too." + " || " + an + " is my friend. Her pain is mine. || When " + an + " hurts, I hurt too." + " || " + an + " means a lot to me...", an + "'s my friend, okay?! You don't get to do that!" + " || " + an + "'s my friend! Not okay! || Hey! " + an + "'s my bestie! || You hurt " + an + "! No!", "What you do to my friends, you do to me. || My friends are me. || Hurt my friend, hurt me." + " || " + an + " is mine to protect.", an + " didn't deserve that. Even I know that." + " || " + an + " didn't deserve that. || Even I know " + an + " deserved better. || Leave " + an + " alone."));
                    var opts = new List<string[]> { new[] { "Apologize, and mean it", "h_sorry" } };
                    if (G.Int >= 35) opts.Add(new[] { "Explain what really happened [INT]", "h_explain" });
                    opts.Add(new[] { "Ask what would make it right", "h_fix" });
                    opts.Add(new[] { "Say it's none of her business", "h_brush" });
                    Choice(b, opts);
                    b.Branch("h_sorry"); b.Face(h, Feel.Normal).Her_(ConvoLines.ApologyAccepted(GirlInfo.ToneOf(h))); b.End();
                    b.Branch("h_explain"); b.Face(h, Feel.Surprised).Her_(ConvoLines.ExplainOk(GirlInfo.ToneOf(h))); b.End();
                    b.Branch("h_fix"); b.Face(h, Feel.Smile).Her_(T(h, "...Just be kind. That's all I want. ...And maybe lunch. || Just be kind. And maybe lunch. || Be gentle... and buy me lunch? || Kindness... and lunch.", "Treat me to lunch and we're even! Deal? || Lunch and we're even! || Treat me and we're good! || Lunch! Deal?!", "...Do better. And lunch. || ...Do better. And lunch. || ...Improve. Lunch included. || ...Better behavior. Plus lunch.", "Lunch. Your treat. Then we'll see. || Lunch. Your treat. Then we'll see. || Feed me. Then maybe. || Lunch first. Forgiveness later.")); b.End();
                    b.Branch("h_brush"); b.Face(h, Feel.Pout).Her_(T(h, "...I see. I thought you were different. || ...I thought you were different. || ...I see. That's disappointing. || ...Oh. Okay.", "Wow. Okay. Wow. || Wow. Okay. || Seriously?! Wow. || Unbelievable.", "...Noted. || ...Noted. || ...Remembered. || ...Understood.", "...Cold. Even for you. || ...Cold. || ...Harsh. || ...Wow. Cold.")); b.End();
                },
                Result = c =>
                {
                    if (c == "h_sorry") { Social.Cleared(h, g); Social.AddTrust(h, -Social.Effect(h, g) / 2 + 1); }
                    else if (c == "h_explain") { Social.Cleared(h, g); Social.AddTrust(h, -Social.Effect(h, g)); }
                    else if (c == "h_fix")
                    {
                        Social.Cleared(h, g);
                        Social.AddTrust(h, 2);
                        var r = Board.Make(RequestKind.Lunch, h, "Make it up to " + name, "You promised " + name + " lunch to make things right. Invite her to lunch.", 8, PeriodMask.Lunch);
                        r.DeadlineDay = S.Day + 2; r.LearnedFrom = "her";
                        Board.Add(r);
                        Toasts.Show("New request: " + r.Title + ".");
                    }
                    else if (c == "h_brush") { Social.AddTrust(h, -5); Social.Add(Social.Kinds.Rude, h); }
                },
                Interest = -1,
            };
        }

        // ================= Scene Talk: feelings she circles around =================

        private static Subject Feelings(SaveData.Heroine h)
        {
            if (h.isGirlfriend || h.relation < 1 || h.favor < 80) return null;
            var mood = Forms.MoodOf(h);
            var gs = S.Girl(GirlInfo.Key(h));
            // A kiss she let happen warily brings it up by itself.
            var kissed = gs.Moments.Any(m => m.Kind == "kiss_wary" && S.Day - m.Day <= 3);
            if (!kissed && (h.favor < 85 || mood != Mood.Flirty && Social.Trust(h) < 60)) return null;
            if (gs.Moments.Any(m => m.Kind == "feelings" && S.Day - m.Day < 4)) return null;
            var name = GirlInfo.FirstName(h);
            return new Subject
            {
                Id = "feelings", Weight = 1.6f,
                Write = b =>
                {
                    if (kissed)
                    {
                        b.Narr(name + " touches her lips without noticing, then catches you looking and drops her hand.");
                        b.Face(h, Feel.Shy).Her_(T(h, "About... the kiss. I haven't stopped thinking about it. What did it mean? || The kiss... I keep thinking about it. What did it mean? || About the kiss... was it real? || The kiss... I can't stop thinking...", "So! The kiss! We're not gonna talk about it?! ...We should talk about it! || The kiss! We need to talk! || So! Kiss! Talk now! || We kissed! What does it mean?!",
                            "...The kiss. I need to know what it meant. To you. || ...The kiss. What did it mean? || ...Explain the kiss. || ...I need to know. The kiss.", "So. You kissed me. And I let you. Explain that. Both parts. || You kissed me. I let you. Explain. || The kiss. Both parts. Explain. || So. That kiss. Talk."));
                    }
                    else
                    {
                        b.Narr(name + " keeps starting sentences and abandoning them. She tucks her hair behind her ear twice.");
                        b.Face(h, Feel.Shy).Her_(T(h, "Um... do you ever think about... what we are? Like, to each other? || Do you think about... what we are? || Um... what are we, to each other? || Do you ever wonder about us?", "Okay, hypothetically! If someone liked someone! Would that be weird?! || Hypothetically! If someone liked someone?! || Okay, what if?! Someone liked someone! || Random question! Crushes! Weird?!",
                            "I've been thinking. About us. That's all. ...That's not all. || About us. I've been thinking. || Us. What are we? || I've wondered. About us.", "Hey. Let's say I liked someone. Hypothetically. What would you do? || Hypothetically, I like someone. You? || Say I liked someone. Then what? || What if I liked you? Hypothetically."));
                    }
                    b.Face(h, Feel.Love).Her_(T(h, "Forget it! I'm being strange... || Forget it! I'm being weird... || Never mind... silly me. || Ah, forget it!", "Ahh, never mind! Forget I said anything! || Never mind! Forget it! || Ahh! Nothing! || Forget I spoke!", "...Forget it. || ...Forget it. || ...Never mind. || ...Disregard.", "...You're supposed to say something now. || ...Your turn to speak. || ...Say something. || ...Well?"));
                    Choice(b, new List<string[]> { new[] { "Tell her you feel the same", "f_same" }, new[] { "Ask what she's trying to say", "f_ask" }, new[] { "Gently change the subject", "f_away" } });
                    b.Branch("f_same"); b.Face(h, Feel.Surprised).Her_(T(h, "Eh...? You... really? || Eh...? Really? || You... mean it? || Truly...?", "W-wait, seriously?! Seriously seriously?! || Seriously?! Seriously?! || Wait! Really?! || For real?!", "...You mean that. || ...You mean it. || ...Sincere. || ...Truly.", "...You'd better mean that. || ...You'd better mean it. || ...Mean it. || ...Better be true.")); b.Face(h, Feel.Love).Her_(T(h, "Then... can you say it properly? I want to hear it. || Say it properly? I want to hear. || Please say it right. || Properly... please?", "Then say it right! Properly! I'm ready! || Say it right! I'm ready! || Properly! Go! || Do it right! Now!", "...Then say it properly. || ...Properly. || ...Say it correctly. || ...Formally.", "Then go on. Say it properly. || Go on. Properly. || Say it right. || Properly. Now.")); b.End();
                    b.Branch("f_ask"); b.Face(h, Feel.Shy).Her_(T(h, "...You know what I'm saying. Don't make me say it first. || ...You know. Don't make me say it first. || ...You understand, right? || ...Don't make me go first.", "You're so dense! Ugh! ...I like being around you, okay?! || So dense! I like being with you! || Ugh! I like you, okay?! || Dense! I like hanging out!", "...That you matter. A lot. || ...You matter. A lot. || ...You're important. || ...That I care.", "...That I'd say yes. If someone asked. || ...I'd say yes. If asked. || ...Ask and find out. || ...That I wouldn't refuse.")); b.End();
                    b.Branch("f_away"); b.Face(h, Feel.Sad).Her_(T(h, "...Ah. Yes. The weather. Right. || ...Yes. The weather. || ...Right. Weather. || ...Oh. Okay.", "...Oh. Okay! Sure! Weather! Great weather! || ...Weather! Great! || ...Oh! Sure! Sunny! || ...Okay! Weather!", "...I see. || ...I see. || ...Hm. || ...Right.", "...Coward. || ...Coward. || ...Chicken. || ...Weak.")); b.End();
                },
                Result = c =>
                {
                    Memories.Add(h, "feelings", c == "f_away" ? (kissed ? "you kissed me and then acted like it was nothing" : "you changed the subject when I was trying to tell you something") : "we almost said it", c == "f_away" ? -1 : 1, c != "f_same");
                    if (kissed && c == "f_away") { Social.AddTrust(h, -4); h.favor = Math.Max(0, h.favor - 3); }
                    if (c == "f_same") { Campus.Apply(new Rewards { Favor = 3, Lewd = 3 }, h); Conversation.QueueConfession(); }
                    else if (c == "f_ask") { Social.AddTrust(h, 3); Campus.Apply(new Rewards { Favor = 3 }, h); }
                    else Social.AddTrust(h, -2);
                },
                Interest = 1,
            };
        }

        // ================= Scene Talk: after a wary H, her guilt =================

        /// <summary>
        /// pedir pra vc assumir ela ou tentar se afastar, e você tem que resolver os problemas todos"). The director chose
        /// which way she went (Consent.Guilt): she asks you to take responsibility, or she pulls away. Until you settle it,
        /// her H, kiss and invitations come harder, and the scene comes back every couple of days.
        /// </summary>
        private static Subject AfterH(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Guilt <= 0 || gs.Moments.Any(m => m.Kind == "after_h" && S.Day - m.Day < 2)) return null;
            var name = GirlInfo.FirstName(h);
            var wants = gs.Guilt == 1;
            var gf = G.Heroines.Any(x => x != null && x != h && x.isGirlfriend);
            return new Subject
            {
                Id = "after_h", Weight = 2f,
                Write = b =>
                {
                    if (wants)
                    {
                        b.Narr(name + " stands a little too close, then a little too far. She has clearly rehearsed this.");
                        b.Face(h, Feel.Shy).Her_(T(h, "About... what we did. I can't pretend it didn't happen. I don't want to. || What we did... I can't pretend. I don't want to. || I can't forget... and I don't want to. || What happened... it was real to me.", "So! About the other day! I've been thinking about it nonstop and I need to know where we stand! || The other day! Where do we stand?! || About that! I need answers! || We did stuff! What now?!",
                            "...What happened between us. I need to know what it was. || ...What was it? || ...Define what happened. || ...I need clarity.", "So. We crossed a line. I'm not the type to just walk away from that. Are you? || We crossed a line. Walking away? || That line. Are you running? || We did it. Now what?"));
                        b.Face(h, Feel.Worried).Her_(T(h, "...Will you take responsibility? For me? || ...Will you take responsibility? || ...Am I special to you? || ...Will you stay with me?", "Are we... something now? Please say we're something! || Are we something?! Please?! || Something now?! Say yes! || Please say we're something!", "...Tell me it wasn't nothing. || ...It wasn't nothing. Tell me. || ...Confirm it mattered. || ...Say it meant something.", "...Well? Take responsibility, or say it was nothing. Pick. || Responsibility or nothing. Pick. || Own it or deny it. Choose. || Which is it?"));
                        Choice(b, new List<string[]> { new[] { "Ask her to be your girlfriend", "g_ask" }, new[] { "Tell her it meant something to you", "g_meant" },
                            new[] { "Ask for a little time", "g_time" }, new[] { "Say it was a one-time thing", "g_once" } });
                        b.Branch("g_ask"); b.Face(h, Feel.Surprised).Her_(T(h, "...! Then... say it properly. I'm listening. || Then... say it properly. || ...! Please, properly. || Then tell me properly.", "Really?! Then say it right! Properly! || Say it right! || Properly! || Do it right!", "...Then say it properly. || ...Properly. || ...Correctly. || ...Say it.", "...Then do it right. I'm waiting. || Do it right. || Properly. I'm waiting. || Go on.")); b.End();
                        b.Branch("g_meant"); b.Face(h, Feel.Smile).Her_(T(h, "...Okay. I believe you. Don't make me wait forever. || ...I believe you. Don't make me wait. || ...Okay. Soon, please. || ...I trust you. Not forever, though.", "It did?! Okay... okay! That's a start! || It did?! A start! || Okay! Something! || Good! Start!", "...Good. Then act like it. || ...Act like it. || ...Show it. || ...Good. Prove it.", "...Then show me. Words are cheap. || ...Show me. || ...Words are cheap. || ...Prove it.")); b.End();
                        b.Branch("g_time"); b.Face(h, Feel.Sad).Her_(T(h, "...I understand. I'll wait. A little. || ...I'll wait. A little. || ...Okay. I'll wait. || ...I understand.", "Time?! ...Okay. But not too much! || Not too long! || Okay! Hurry! || Fine! Quickly!", "...Fine. Don't take long. || ...Don't take long. || ...Briefly. || ...Fine.", "...Clock's ticking. || ...Clock's ticking. || ...Tick tock. || ...Hurry.")); b.End();
                        b.Branch("g_once"); b.Face(h, Feel.Sad).Her_(T(h, "...Oh. I see. ...I'm such an idiot. || ...I'm such an idiot. || ...I see. Silly me. || ...Oh. I misunderstood.", "...Wow. Okay. I really thought... never mind. || ...Wow. I really thought... never mind. || ...Okay. Forget it. || ...Ouch. Okay.", "...Understood. Don't talk to me for a while. || ...Don't talk to me for a while. || ...Understood. Leave me. || ...Fine. Distance.", "...Right. Of course it was. ...Get away from me. || ...Of course. Go away. || ...Right. Leave. || ...Get away from me.")); b.End();
                    }
                    else
                    {
                        b.Narr(name + " keeps her eyes on the floor and her bag held in front of her like a shield.");
                        b.Face(h, Feel.Worried).Her_(T(h, "...We shouldn't have. I keep thinking about what people would say... || ...We shouldn't have. What would people say? || ...It was a mistake... I'm scared. || ...I keep worrying...", "I... I think the other day was a mistake! Not because of you! Just... it was too fast! || Mistake! Too fast! Not you! || Too fast! Sorry! || It was a mistake! Too quick!",
                            "...That was a mistake. I'm not like that. || ...A mistake. Not me. || ...Error. I'm not like that. || ...Mistake.", "Let's be honest. That was reckless. I don't do reckless. || That was reckless. Not me. || Reckless. Not my style. || Too reckless."));
                        if (gf) b.Face(h, Feel.Serious).Her_(T(h, "...And you have a girlfriend. What does that make me? || ...You have a girlfriend. What am I? || ...You're with someone. What does that make me? || ...And she exists...", "And you're not even single! What was I thinking?! || You're not single! What was I thinking?! || You're taken! Argh! || Not single! Stupid me!", "...You have someone. I forgot that. Conveniently. || ...You have someone. Forgot that. || ...You're taken. Convenient amnesia. || ...Someone's waiting for you.", "...You're taken. I knew that. That's the worst part. || ...You're taken. Worst part. || ...Taken. I knew. || ...You belong to someone."));
                        b.Face(h, Feel.Sad).Her_(T(h, "...Maybe we should keep some distance for a while. || ...Maybe some distance. || ...We should step back. || ...Some space, maybe.", "Maybe we should... cool off a little? Just for a bit? || Cool off a bit?! || Break for a bit?! || Space! A little!", "...I need distance. || ...Distance. || ...Space. || ...I need room.", "...I'm going to keep my distance. For both of us. || ...Distance. For both. || ...I'll stay away. || ...Space. Smart."));
                        Choice(b, new List<string[]> { new[] { "Tell her you want to be with her", "d_want" }, new[] { "Promise it stays between you", "d_secret" },
                            new[] { "Give her space", "d_space" }, new[] { "Agree to forget it happened", "d_forget" } });
                        b.Branch("d_want"); b.Face(h, Feel.Surprised).Her_(T(h, "...You mean that? Even after...? || ...Even after...? || ...You mean it? || ...Really...?", "Wait, what?! You mean it?! || You mean it?! || Wait! Really?! || Seriously?!", "...Say that again. Slowly. || ...Again. Slowly. || ...Repeat. || ...Say it again.", "...Don't say that unless you mean it. || ...Mean it or don't say it. || ...Careful. || ...Only if true.")); b.End();
                        b.Branch("d_secret"); b.Face(h, Feel.Shy).Her_(T(h, "...Thank you. That helps. A lot. || ...Thank you. That helps. || ...That's a relief. || ...Thank you so much.", "Our secret? ...Okay. Okay, that makes it less scary! || Our secret! Less scary! || Secret! Okay! || Shh! Better!", "...Good. Thank you. || ...Good. Thanks. || ...Fine. || ...Thank you.", "...Fine. Our secret. Don't make me regret it. || ...Our secret. Don't make me regret it. || ...Secret. Fine. || ...Keep it quiet.")); b.End();
                        b.Branch("d_space"); b.Face(h, Feel.Sad).Her_(T(h, "...Thank you for understanding. || ...Thank you. || ...Thanks for understanding. || ...I appreciate it.", "...Thanks. I'll come find you. Promise. || I'll find you! Promise! || Thanks! Later! || I'll come back!", "...Thank you. || ...Thanks. || ...Good. || ...Appreciated.", "...Appreciated. || ...Appreciated. || ...Thanks. || ...Good.")); b.End();
                        b.Branch("d_forget"); b.Face(h, Feel.Sad).Her_(T(h, "...Right. Forget it. ...If only it were that easy. || ...If only it were easy. || ...Forget it... I'll try. || ...Okay. Forgotten.", "...Yeah. Sure. Forgotten! Totally! || Sure! Forgotten! || Totally gone! || Erased!", "...Done. It never happened. || ...Never happened. || ...Done. || ...Erased.", "...Just like that, huh. Fine. || ...Just like that. Fine. || ...Easy for you. || ...Fine. Gone.")); b.End();
                    }
                },
                Result = c =>
                {
                    var trust = Social.Trust(h);
                    var cold = c == "g_once" || c == "d_forget";
                    Memories.Add(h, "after_h", cold ? "you said it was nothing" : c == "g_time" || c == "d_space" ? "we left it unsaid" : "we talked about what happened", cold ? -1 : 1, false);
                    switch (c)
                    {
                        case "g_ask": gs.Guilt = 0; Social.AddTrust(h, 4); Campus.Apply(new Rewards { Favor = 3 }, h); Conversation.QueueConfession(); break;
                        case "g_meant":
                            Social.AddTrust(h, 3);
                            if (trust >= 60) gs.Guilt = 0;   // she believes you will come around; otherwise she asks again in a couple of days
                            break;
                        case "g_time": Social.AddTrust(h, -1); break;
                        case "g_once":
                            gs.Guilt = 0; gs.DistanceUntil = S.Day + 3;
                            h.favor = Math.Max(0, h.favor - 8); Social.AddTrust(h, -8); Social.Add(Social.Kinds.Rude, h);
                            break;
                        case "d_want":
                            if (trust >= 45 || h.favor >= 85) { gs.Guilt = 0; gs.DistanceUntil = -1; Social.AddTrust(h, 3); Conversation.QueueConfession(); }
                            else { gs.DistanceUntil = S.Day + 1; Social.AddTrust(h, 1); }   // she wants to believe it, not yet
                            break;
                        case "d_secret": gs.Guilt = 0; gs.DistanceUntil = -1; Social.AddTrust(h, 3); break;
                        case "d_space": gs.DistanceUntil = S.Day + 2; Social.AddTrust(h, 1); break;
                        case "d_forget": gs.Guilt = 0; gs.DistanceUntil = -1; Social.AddTrust(h, -2); h.favor = Math.Max(0, h.favor - 3); break;
                    }
                    CampusLifePlugin.Log.LogInfo("Scene talk after H: " + name + " -> " + c + " (guilt " + gs.Guilt + ", distance until day " + gs.DistanceUntil + ", trust " + Social.Trust(h) + ")");
                },
                Interest = -1,
            };
        }

        // ================= scorned: she does not want to talk =================

        private static bool PlayScorned(SaveData.Heroine h, Action done)
        {
            var b = new SceneBuilder(h, true);
            var name = GirlInfo.FirstName(h);
            var gs = S.Girl(GirlInfo.Key(h));
            b.Narr(name + " won't look at you.");
            b.Face(h, Feel.Serious).Her_(T(h, "...I can't talk to you right now. Please. || ...I can't talk now. || ...Please, not now. || ...Leave me be.", "Nope. Not today. Not after that. || Not today! || Nope! || No way!", "...We have nothing to talk about. || ...Nothing to say. || ...Nothing. || ...No.", "Oh, now you want to talk? Funny. || Now you want to talk? || Funny. Now? || Oh, now?"));
            var canRedeem = Pairs.CanRedeem(h);
            if (canRedeem)
            {
                Choice(b, new List<string[]> { new[] { "Apologize properly, without excuses", "s_sorry" }, new[] { "Give her space", "s_go" } });
                b.Branch("s_sorry");
                b.Me(Lines.Pick("I know I hurt you. I'm not asking you to forgive me today. I just wanted you to hear it from me: I'm sorry.", "No excuses. What I did was unfair to you. I'm sorry."));
                b.Face(h, Feel.Sad).Her_(T(h, "...I heard you. That doesn't fix it. ...But I heard you. || ...I heard you. Not fixed. But heard. || ...Thank you for saying it. It still hurts. || ...I heard. Give me time.", "...Okay. I heard that. Doesn't mean we're good. || ...Heard. Not good yet. || ...Okay. Still upset. || ...Noted! Still mad!", "...Noted. It's a start. || ...A start. || ...Noted. || ...Beginning.", "...Hmph. Took you long enough. ...Still mad. || ...Took long enough. Still mad. || ...Late. Still angry. || ...Finally. Still upset."));
                b.End();
                b.Branch("s_go"); b.Narr("You step back. She turns away."); b.End();
            }
            gs.Pending = "";
            LastOpened = "Scorned (" + name + ")";
            return Scenes.PlayInTalk(b, c =>
            {
                if (c == "s_sorry") Pairs.Redeem(h, "apology");
                Conversation.EndTalk();
                if (done != null) done();
            }, false);
        }
    }
}
