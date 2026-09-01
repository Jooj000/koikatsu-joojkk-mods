using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Two girls walk over together and call you on the map; the scene itself plays in the talk scene with the first
    /// girl, the second one standing next to her; back on the map they say a last word and leave. The social director
    /// only sends a pair that has a reason (<see cref="Motive"/>): a friend who wants an apology for something you did
    /// to her friend, or brings her to say thank you, your girlfriend meeting a girl who likes you, a quarrel, an
    /// introduction, a girl who feels left out, gossip, or close friends who just want to see you. A girl comes over
    /// with someone at most every other day, and each incident is the reason for one scene only.
    /// </summary>
    internal static class Duo
    {
        private static CampusState S { get { return Campus.S; } }
        private static int _lastPeriod = -1;
        private static float _nextTry;

        /// <summary>Called about once a second from Calendar.Tick, when nothing else is playing.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.DuoScenes.Value || Busy || !Scenes.CanPlayOnMap) return;
            var period = S.Day * 16 + (int)G.CurrentMask;
            if (period == _lastPeriod || Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 20f;
            if (Random.value > 0.35f) return; // a motive found on this map plays within a minute or two
            var plan = PickPlan();
            if (plan == null) return;
            if (Play(plan)) _lastPeriod = period;
        }

        // ---------------- the director: why two girls come over ----------------

        /// <summary>Why the two of them come to you. The director only sends a pair that has one.</summary>
        internal enum Motive
        {
            Rivals,     // your girlfriend and a girl who clearly likes you meet at your side
            Confront,   // a friend brings a girl you were rude to, or broke a promise to, and wants an apology
            Thanks,     // a friend brings a girl you helped or stood up for, so she says thank you
            Quarrel,    // two girls who do not get along want you to settle an argument
            Introduce,  // a girl you know brings her best friend to meet you
            LeftOut,    // a girl feels her best friend took her place with you
            Gossip,     // two friends have news about another girl
            Hangout     // close friends who both like you just want your opinion
        }

        internal sealed class Plan
        {
            public NPC A, B;          // A talks first (the talk scene opens with her)
            public Motive Why;
            public Incident Inc;      // Confront / Thanks / Gossip
            public float Weight;
            public override string ToString() { return Why + " (" + GirlInfo.FirstName(A.heroine) + " + " + GirlInfo.FirstName(B.heroine) + ")"; }
        }

        /// <summary>Girls on the player's map within reach, walking about (not in an event or seated in class).</summary>
        private static List<NPC> Candidates()
        {
            var act = G.Act;
            var player = act != null && act.Player != null ? act.Player.transform.position : Vector3.zero;
            return act.npcList.Where(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && GirlInfo.IsRegular(n.heroine)
                                          && (n.transform.position - player).sqrMagnitude < 20f * 20f && n.AI != null && n.AI.isRunning
                                          && Rested(n.heroine)).ToList();
        }

        /// <summary>A girl comes over with someone at most every other day.</summary>
        private static bool Rested(SaveData.Heroine h)
        {
            var d = S.Girl(GirlInfo.Key(h)).DuoDay;
            return d < 0 || S.Day - d >= 2;
        }

        /// <summary>Every motive found among the girls nearby; one is drawn by weight (stronger reasons first).</summary>
        private static Plan PickPlan()
        {
            var near = Candidates();
            var plans = new List<Plan>();
            for (var i = 0; i < near.Count; i++)
                for (var j = i + 1; j < near.Count; j++)
                {
                    var p = FindMotive(near[i], near[j]);
                    if (p != null) plans.Add(p);
                }
            if (plans.Count == 0) return null;
            var roll = Random.value * plans.Sum(p => p.Weight);
            foreach (var p in plans)
            {
                roll -= p.Weight;
                if (roll <= 0f) return p;
            }
            return plans[plans.Count - 1];
        }

        /// <summary>The strongest reason these two have to come to you together, or null.</summary>
        internal static Plan FindMotive(NPC na, NPC nb)
        {
            var a = na.heroine;
            var b = nb.heroine;
            var bond = Social.Bond(a, b);

            // Your girlfriend and a girl who clearly likes you.
            if (a.isGirlfriend != b.isGirlfriend && (a.isGirlfriend ? b : a).favor >= 70)
                return a.isGirlfriend ? MakePlan(na, nb, Motive.Rivals, 3f, null) : MakePlan(nb, na, Motive.Rivals, 3f, null);

            if (bond >= 35)
            {
                // Something you did to one of them, which the other heard about.
                foreach (var pair in new[] { new[] { na, nb }, new[] { nb, na } })
                {
                    var friend = pair[0];
                    var her = pair[1];
                    var inc = AboutHer(her.heroine, friend.heroine, Social.Kinds.Rude, Social.Kinds.BrokePromise);
                    if (inc != null) return MakePlan(friend, her, Motive.Confront, 4f, inc);
                    inc = AboutHer(her.heroine, friend.heroine, Social.Kinds.Helped, Social.Kinds.Defended);
                    if (inc != null) return MakePlan(friend, her, Motive.Thanks, 3f, inc);
                }
            }
            if (bond <= -30) return MakePlan(na, nb, Motive.Quarrel, 2f, null);
            if (bond < 35) return null;

            // A best friend you have never talked to.
            if (bond >= 45)
            {
                if (S.Girl(GirlInfo.Key(b)).TalkDays.Count == 0 && S.Girl(GirlInfo.Key(a)).TalkDays.Count > 0) return MakePlan(na, nb, Motive.Introduce, 2f, null);
                if (S.Girl(GirlInfo.Key(a)).TalkDays.Count == 0 && S.Girl(GirlInfo.Key(b)).TalkDays.Count > 0) return MakePlan(nb, na, Motive.Introduce, 2f, null);
            }
            // One of them left out while you spend your days with the other.
            if (bond >= 45)
            {
                if (Social.TalkDaysRecent(a, 5) >= 3 && Social.TalkDaysRecent(b, 5) == 0 && S.Girl(GirlInfo.Key(b)).TalkDays.Count > 0) return MakePlan(nb, na, Motive.LeftOut, 2f, null);
                if (Social.TalkDaysRecent(b, 5) >= 3 && Social.TalkDaysRecent(a, 5) == 0 && S.Girl(GirlInfo.Key(a)).TalkDays.Count > 0) return MakePlan(na, nb, Motive.LeftOut, 2f, null);
            }
            var gossip = Social.Gossip(a);
            if (gossip != null) return MakePlan(na, nb, Motive.Gossip, 1.5f, gossip);
            gossip = Social.Gossip(b);
            if (gossip != null) return MakePlan(nb, na, Motive.Gossip, 1.5f, gossip);

            // Close friends who both like you: no news, they just want to see you.
            if (bond >= 60 && Social.Trust(a) >= 40 && Social.Trust(b) >= 40) return MakePlan(na, nb, Motive.Hangout, 0.5f, null);
            return null;
        }

        private static Plan MakePlan(NPC a, NPC b, Motive why, float weight, Incident inc)
        {
            return new Plan { A = a, B = b, Why = why, Weight = weight, Inc = inc };
        }

        /// <summary>A recent incident of these kinds about her, which her friend knows and no two-girl scene was about yet.</summary>
        private static Incident AboutHer(SaveData.Heroine her, SaveData.Heroine friend, params string[] kinds)
        {
            var key = GirlInfo.Key(her);
            var gs = S.Girl(key);
            var known = S.Girl(GirlInfo.Key(friend)).Known;
            return S.Incidents.Where(i => i.Girl == key && kinds.Contains(i.Kind) && S.Day - i.Day <= 4 && known.Contains(i.Id)
                                          && !gs.DuoHandled.Contains(i.Id) && !gs.Cleared.Contains(i.Id))
                .OrderByDescending(i => i.Day).FirstOrDefault();
        }

        private static void Handled(SaveData.Heroine h, Incident inc)
        {
            if (h == null || inc == null) return;
            var l = S.Girl(GirlInfo.Key(h)).DuoHandled;
            if (!l.Contains(inc.Id)) l.Add(inc.Id);
            if (l.Count > 30) l.RemoveAt(0);
        }

        /// <summary>The scene itself (played in the talk scene with the first girl, or on the map as a fallback).</summary>
        private static SceneBuilder Compose(Plan plan, bool inTalk, SceneBuilder intro, out Action<string> result)
        {
            var na = plan.A;
            var nb = plan.B;
            var a = na.heroine;
            var bb = nb.heroine;
            var ta = GirlInfo.ToneOf(a);
            var tb = GirlInfo.ToneOf(bb);
            var an = GirlInfo.FirstName(a);
            var bn = GirlInfo.FirstName(bb);
            var sb = new SceneBuilder(inTalk ? na.heroine : null, inTalk);

            if (plan.Why == Motive.Confront)
            {
                // A: the friend who heard about it. B: the girl it happened to.
                var rude = plan.Inc.Kind == Social.Kinds.Rude;
                intro.Narr(an + " marches over, " + bn + " trailing behind her.");
                sb.Say(a, rude
                    ? Lines.T(ta, "Um... " + bn + " told me what you said to her. That wasn't very nice...", "Hey! What's the big idea, being mean to " + bn + "?!", "I heard how you treated " + bn + ". Explain.", "So you were rude to my friend. Want to try that again?")
                    : Lines.T(ta, "You promised " + bn + " something... and didn't keep it. She was really waiting...", "Hey! You stood " + bn + " up! Not cool!", "You gave " + bn + " your word. Then nothing.", "Breaking promises to my friend? Bold move."));
                sb.Say(bb, Lines.T(tb, "I-it's fine, really... you don't have to...", "Ehh, I'm not THAT mad... okay, a little.", "...I can speak for myself. But yes.", "Hmph. Let him squirm a little."));
                var opts = new List<string> { "Apologize to " + bn + " sincerely.", "sorry" };
                if (G.Int >= 40) { opts.Add("Explain what really happened. [INT]"); opts.Add("explain"); }
                opts.Add("That's none of your business."); opts.Add("brush");
                sb.Choice(opts.ToArray());
                sb.Branch("sorry");
                sb.Say(bb, Lines.T(tb, "...Okay. Thank you for saying that.", "Apology accepted! Just this once!", "...Accepted.", "Fine. You're forgiven. Barely."));
                sb.Say(a, Lines.T(ta, "See? That wasn't so hard.", "Good! Now we're all friends again!", "Good.", "Hm. Respectable."));
                sb.End();
                sb.Branch("explain");
                sb.Narr("You tell them how it really went. " + bn + "'s face softens.");
                sb.Say(bb, Lines.T(tb, "Oh... I didn't know that...", "Ohh, so THAT's what happened!", "...That changes things.", "Hm. Alright, that's a decent excuse."));
                sb.End();
                sb.Branch("brush");
                sb.Say(a, Lines.T(ta, "...I see. Come on, " + bn + ".", "Wow. Unbelievable. Let's go, " + bn + "!", "Noted. Let's go.", "Charming. Come on, " + bn + "."));
                sb.End();
                var inc = plan.Inc;
                result = c =>
                {
                    Handled(bb, inc);
                    if (c == "sorry") { Social.Cleared(bb, inc); Social.Cleared(a, inc); Social.AddTrust(bb, 5); Social.AddTrust(a, 3); }
                    else if (c == "explain") { Social.Cleared(bb, inc); Social.Cleared(a, inc); Social.AddTrust(bb, 3); Social.AddTrust(a, 4); S.Reputation = Mathf.Clamp(S.Reputation + 1, 0, 100); }
                    else { Social.AddTrust(a, -6); Social.AddTrust(bb, -3); Social.Add(Social.Kinds.Rude, a, new[] { bb }); }
                };
            }
            else if (plan.Why == Motive.Thanks)
            {
                // A: the friend who heard about it. B: the girl you helped.
                var defended = plan.Inc.Kind == Social.Kinds.Defended;
                intro.Narr(an + " comes over with " + bn + ", nudging her forward.");
                sb.Say(a, Lines.T(ta, "Go on, " + bn + "... you wanted to say something, right?", "C'mon, " + bn + ", tell him! You've been talking about it all day!", "She has something to tell you.", "Go on. Don't be shy now~"));
                sb.Say(bb, defended
                    ? Lines.T(tb, "Thank you... for taking my side. It meant a lot.", "Thanks for having my back! You're the best!", "...Thank you for standing up for me.", "So. You took my side. ...Thanks, I guess.")
                    : Lines.T(tb, "Thank you for helping me the other day...", "Thanks for the help! Seriously!", "Thank you for your help. I mean it.", "You helped me out. I owe you one... maybe."));
                sb.Choice("It was nothing.", "modest", "Anytime. I've got your back.", "anytime");
                sb.Branch("modest"); sb.Say(bb, Lines.T(tb, "It wasn't nothing to me...", "Don't be so humble!", "...Still. Thanks.", "Modest, too? Hm.")); sb.End();
                sb.Branch("anytime"); sb.Say(a, Lines.T(ta, "Ehehe... he's reliable, isn't he?", "See?! Told you he's great!", "Reliable. Good.", "Ooh, smooth.")); sb.End();
                var inc = plan.Inc;
                result = c =>
                {
                    Handled(bb, inc);
                    if (c == "modest") { Social.AddTrust(bb, 4); Social.AddTrust(a, 2); G.AddFavor(bb, 2, 0); }
                    else { G.AddFavor(bb, 4, 0); Social.AddTrust(bb, 2); Social.AddTrust(a, 3); }
                };
            }
            else if (plan.Why == Motive.Rivals)
            {
                // Your girlfriend and a girl who clearly likes you.
                var gf = a.isGirlfriend ? a : bb;
                var other = gf == a ? bb : a;
                var gn = GirlInfo.FirstName(gf);
                var on = GirlInfo.FirstName(other);
                intro.Narr(gn + " and " + on + " walk up to you at the same time. The air gets a little tense.");
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "Um, I was hoping to talk to you a little...", "Hey! I wanted to hang out with you!", "I wanted a word with you.", "Mind if I borrow him for a bit?"));
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "Ah... he's with me today, sorry...", "Hey, hey! He's MY boyfriend, you know!", "He's busy. With me.", "Borrow? He's not a library book. He's mine."));
                sb.Choice("Take your girlfriend's hand.", "gf", "We can all talk together.", "both");
                sb.Branch("gf");
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "...Thank you.", "Hehe! See?", "Good answer.", "Heh. Knew it."));
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "I see... sorry for interrupting.", "Aw, okay! Another time!", "...Understood.", "Hmph. Fine. For now."));
                sb.End();
                sb.Branch("both");
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "...Okay. If you say so.", "Fiiine. Together.", "...Fine.", "Generous of you. Hmph."));
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "Thank you! I'm happy.", "Yay!", "Thanks.", "How kind~"));
                sb.End();
                result = c =>
                {
                    if (c == "gf") { G.AddFavor(gf, 5, 0); Social.AddTrust(gf, 4); G.AddFavor(other, -2, 0); }
                    else if (c == "both") { G.AddFavor(other, 3, 0); Social.AddTrust(gf, -3); Social.Add(Social.Kinds.Flirted, other, new[] { gf }); }
                };
            }
            else if (plan.Why == Motive.Quarrel)
            {
                // A quarrel: you take a side, or calm them both down.
                var topic = Lines.Pick("the last melon bread", "whose turn it was to clean the classroom", "who gets the window seat", "the club room schedule", "a borrowed manga that came back bent");
                intro.Narr(an + " and " + bn + " are arguing about " + topic + ". They turn to you.");
                sb.Say(a, Lines.T(ta, "You saw it too, right? It was my turn...", "Tell her I'm right!", "You're impartial. Settle this.", "Go on, tell her she's wrong."));
                sb.Say(bb, Lines.T(tb, "Please, say something...", "No way, I'm right! Right?!", "This is ridiculous. Decide.", "Oh, this'll be good. Choose wisely."));
                var opts = new List<string> { "Side with " + an + ".", "a", "Side with " + bn + ".", "b" };
                if (G.Int >= 40) { opts.Add("Find a compromise. [INT]"); opts.Add("calm"); }
                sb.Choice(opts.ToArray());
                sb.Branch("a"); sb.Say(a, Lines.T(ta, "Thank you...!", "Ha! Told you!", "Obviously.", "See?"));
                sb.Say(bb, Lines.T(tb, "...I see how it is.", "Traitor!", "...Noted.", "Wow. Okay.")); sb.End();
                sb.Branch("b"); sb.Say(bb, Lines.T(tb, "Thank you...!", "Ha! Told you!", "Obviously.", "See?"));
                sb.Say(a, Lines.T(ta, "...I see how it is.", "Traitor!", "...Noted.", "Wow. Okay.")); sb.End();
                sb.Branch("calm");
                sb.Narr("You suggest a fair way to split it. They grumble, then agree.");
                sb.Say(a, Lines.T(ta, "That's... actually fair.", "Huh. Okay, fine!", "Acceptable.", "Hmph. Smart.")); sb.End();
                result = c =>
                {
                    if (c == "a") { Social.Add(Social.Kinds.Defended, a, new[] { bb }); Social.AddTrust(bb, -4); G.AddFavor(a, 4, 0); }
                    else if (c == "b") { Social.Add(Social.Kinds.Defended, bb, new[] { a }); Social.AddTrust(a, -4); G.AddFavor(bb, 4, 0); }
                    else if (c == "calm") { Social.AddTrust(a, 3); Social.AddTrust(bb, 3); Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(bb), 10); Arcs.AddBond(GirlInfo.Key(bb), GirlInfo.Key(a), 10); S.Reputation = Mathf.Clamp(S.Reputation + 2, 0, 100); }
                };
            }
            else
            {
                // A comes to you; B is her best friend you never met (Introduce) or the friend who took her place (LeftOut).
                var newcomer = plan.Why == Motive.Introduce ? bb : null;
                var jealousOf = plan.Why == Motive.LeftOut ? bb : null;
                var gossip = plan.Why == Motive.Gossip ? plan.Inc : null;
                if (newcomer != null)
                {
                    var host = a;
                    var hn = GirlInfo.FirstName(host);
                    var nn = GirlInfo.FirstName(newcomer);
                    intro.Narr(hn + " comes over, pulling " + nn + " by the hand.");
                    sb.Say(host, Lines.T(GirlInfo.ToneOf(host), "This is my friend " + nn + "! I wanted you two to meet.", "Meet " + nn + ", my best friend! Be nice!", "This is " + nn + ". You should know each other.", "Here, I brought you a friend. Her name's " + nn + "."));
                    sb.Say(newcomer, Lines.T(GirlInfo.ToneOf(newcomer), "N-nice to meet you...", "Heya! I've heard about you!", "Hello.", "So you're the famous club guy. Hm."));
                    sb.Choice("Greet her warmly.", "warm", "Give a quick nod.", "nod");
                    sb.Branch("warm"); sb.Say(newcomer, Lines.T(GirlInfo.ToneOf(newcomer), "You're nice... I'm glad.", "Ooh, I like him!", "...Pleasure.", "Polite. I approve.")); sb.End();
                    sb.Branch("nod"); sb.Say(host, Lines.T(GirlInfo.ToneOf(host), "Um... he's nicer than he looks, I promise!", "Hey, be friendlier!", "...Charming as always.", "Wow, so cool and mysterious.")); sb.End();
                    result = c =>
                    {
                        if (c == "warm") { G.AddFavor(newcomer, 5, 0); Social.AddTrust(newcomer, 5); Social.AddTrust(host, 2); Social.RecordTalk(newcomer); }
                        else G.AddFavor(host, -1, 0);
                    };
                }
                else if (jealousOf != null)
                {
                    var left = a;
                    var ln = GirlInfo.FirstName(left);
                    var jn = GirlInfo.FirstName(jealousOf);
                    intro.Narr(an + " and " + bn + " walk up to you together.");
                    sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "You and " + jn + " are always together lately... can I join sometimes?", "Hey! You keep stealing " + jn + " from me! Or me from... wait. Whatever!", "You two have been inseparable. I've noticed.", "So you've been monopolizing " + jn + ". Or is it the other way around?"));
                    sb.Say(jealousOf, Lines.T(GirlInfo.ToneOf(jealousOf), "Ah, sorry! I didn't mean to leave you out...", "Haha, don't be jealous!", "...It's not like that.", "Jealous? How cute."));
                    sb.Choice("Let's all hang out together.", "all", "Invite " + ln + " to lunch soon.", "lunch", "Shrug it off.", "shrug");
                    sb.Branch("all"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "Really? Okay!", "Yes! Group hangout!", "...Fine. Good.", "Deal.")); sb.End();
                    sb.Branch("lunch"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "J-just us? Okay...!", "A date? Kidding! Lunch it is!", "...I'll hold you to it.", "Oh? Now that's interesting.")); sb.End();
                    sb.Branch("shrug"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "...Oh. Okay.", "Wow. Okay then!", "...Right.", "Noted. Very noted.")); sb.End();
                    result = c =>
                    {
                        if (c == "all") { Social.AddTrust(left, 3); G.AddFavor(left, 3, 0); G.AddFavor(jealousOf, 2, 0); }
                        else if (c == "lunch")
                        {
                            Social.AddTrust(left, 4);
                            var r = Board.Make(RequestKind.Lunch, left, "Lunch with " + ln, "You promised " + ln + " lunch together. Invite her to lunch.", 8, PeriodMask.Lunch);
                            r.DeadlineDay = S.Day + 2; r.LearnedFrom = "her";
                            Board.Add(r);
                            Toasts.Show("New request: lunch with " + ln + ".");
                        }
                        else { Social.AddTrust(left, -4); S.Girl(GirlInfo.Key(left)).JealousDay = S.Day; }
                    };
                }
                else if (gossip != null)
                {
                    var about = Social.Get(gossip.Girl);
                    var name = about != null ? GirlInfo.FirstName(about) : "";
                    intro.Narr(an + " and " + bn + " come over, whispering to each other.");
                    sb.Say(a, ConvoLines.GossipLine(ta, gossip.Kind, name));
                    sb.Say(bb, Lines.T(tb, "It's true, I heard it too...", "Totally true! Everyone knows!", "That's what they say.", "The whole school's talking."));
                    sb.End();
                    result = c => { Social.Mentioned(a, gossip); Social.Mentioned(bb, gossip); };
                }
                else
                {
                    intro.Narr(an + " and " + bn + " wave you over.");
                    sb.Say(a, Lines.T(ta, "We were just talking about you. Only good things!", "Hey! We need a tiebreaker: cats or dogs?", "We were discussing the festival. Opinions?", "Speak of the devil."));
                    sb.Say(bb, Lines.T(tb, "Hehe, it's true.", "Answer carefully!", "Your input is required.", "Go on, say something interesting."));
                    sb.Choice("Cats, obviously.", "x", "Dogs, no contest.", "y");
                    sb.Branch("x"); sb.Say(a, Lines.T(ta, "I knew it!", "YES!", "Correct.", "Good taste.")); sb.End();
                    sb.Branch("y"); sb.Say(bb, Lines.T(tb, "I knew it!", "YES!", "Correct.", "Good taste.")); sb.End();
                    result = c => { G.AddFavor(c == "x" ? a : bb, 3, 0); G.AddFavor(a, 1, 0); G.AddFavor(bb, 1, 0); };
                }
            }

            return sb;
        }
        // ---------------- the whole scene ----------------

        /// <summary>The girls are on their way or the scene is playing.</summary>
        public static bool Busy { get; private set; }

        /// <summary>The talk scene opened for a duo with this girl (the conversation menu stays out of it).</summary>
        public static SaveData.Heroine TalkHer { get; private set; }

        /// <summary>
        /// These two come over now: with the director's motive if they have one, else as close friends
        /// passing the time.
        /// </summary>
        public static bool Play(NPC na, NPC nb)
        {
            if (na == null || nb == null) return false;
            return Play(FindMotive(na, nb) ?? new Plan { A = na, B = nb, Why = Motive.Hangout });
        }

        public static bool Play(Plan plan)
        {
            if (Busy || plan == null || plan.A == null || plan.B == null || G.Act == null || G.Act.Player == null) return false;
            var spots = FindSpots();
            if (spots == null) { Step("no free spots in front of the player"); return false; }
            Step("motive " + plan);
            var intro = new SceneBuilder(null, false);
            Action<string> result;
            var main = Compose(plan, true, intro, out result);
            CallOver(intro, plan.A.heroine, plan.B.heroine);
            foreach (var h in new[] { plan.A.heroine, plan.B.heroine }) S.Girl(GirlInfo.Key(h)).DuoDay = S.Day;
            CampusLifePlugin.Instance.StartCoroutine(Run(plan, spots, intro, main, result).PreventFromCrashing());
            return true;
        }

        /// <summary>Both of them ask for a moment (on the map, before the talk scene opens).</summary>
        private static void CallOver(SceneBuilder intro, SaveData.Heroine a, SaveData.Heroine b)
        {
            intro.Say(a, Lines.T(GirlInfo.ToneOf(a), "Um... do you have a minute?", "Hey hey! Got a sec?", "A moment of your time.", "Hey, you. Come here a sec."));
            intro.Say(b, Lines.T(GirlInfo.ToneOf(b), "We wanted to talk to you...", "C'mon, it'll be quick!", "It won't take long.", "Don't keep us waiting~"));
        }

        private static SceneBuilder Outro(SaveData.Heroine a, SaveData.Heroine b)
        {
            var o = new SceneBuilder(null, false);
            o.Say(a, Lines.T(GirlInfo.ToneOf(a), "Thank you for listening...", "That was fun! See ya!", "That's all. Thanks.", "Well, that was something. Later~"));
            o.Say(b, Lines.T(GirlInfo.ToneOf(b), "S-see you later...", "Bye bye!", "Until next time.", "Don't miss us too much."));
            o.Narr(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(b) + " walk off together.");
            return o;
        }

        private static IEnumerator Wait(Func<bool> done, float seconds)
        {
            var t0 = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - t0 < seconds) yield return null;
        }

        private static IEnumerator Run(Plan plan, Vector3[] spots, SceneBuilder intro, SceneBuilder main, Action<string> result)
        {
            var na = plan.A;
            var nb = plan.B;
            Busy = true;
            var owner = new object();
            Scenes.Owner = owner;
            bool[] agents = null;
            string choice = null;
            var talked = false;
            try { agents = Stop(na, nb); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo stop: " + e.Message); }

            // 1. They walk over to two free spots in front of you (never through or behind a wall).
            yield return CampusLifePlugin.Instance.StartCoroutine(WalkTo(na, nb, spots));

            // 2. On the map: they call you over.
            var done = false;
            if (G.IsFreeRoaming && Scenes.PlayOnMap(intro, c => done = true, owner))
            {
                Step("intro playing");
                yield return Wait(() => done, 300f);
                // The game keeps isEventNow on while the ADV event fades out: until the map is free again, everything
                // below would be skipped and the two would just stand there.
                yield return Wait(() => G.IsFreeRoaming && !Scenes.Running, 15f);
                Step("intro over, free roaming " + G.IsFreeRoaming);

                // 3. The talk scene, with the first girl as usual and the second one standing next to her.
                if (Cfg.DuoInTalk.Value && G.IsFreeRoaming)
                {
                    Release(na, nb, agents); // the game pauses and hides everyone for the talk itself
                    agents = null;
                    TalkHer = na.heroine;
                    G.Act.SceneEvent(na);
                    yield return Wait(() => Ready(G.TalkScene), 40f);
                    var ts = G.TalkScene;
                    Step("talk scene " + (Ready(ts) ? "open" : "did not open (event " + (G.Act != null && G.Act.isEventNow) + ")"));
                    if (Ready(ts))
                    {
                        yield return Wait(() => MenuShown(ts), 25f); // her greeting first
                        try { StandBeside(nb, na); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo beside: " + e.Message); }
                        done = false;
                        if (Scenes.PlayInTalk(main, c => { choice = c; done = true; }, false))
                        {
                            talked = true;
                            yield return Wait(() => done, 600f);
                            Step("talk part over, choice '" + choice + "'");
                        }
                        else Step("could not play in the talk scene");
                        try { if (G.TalkScene == ts) Traverse.Create(ts).Method("CommandFunc", new object[] { -1 }).GetValue(); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo end talk: " + e.Message); }
                    }
                    yield return Wait(() => G.TalkScene == null && G.IsFreeRoaming, 60f);
                    TalkHer = null;
                    Step("back on the map, free roaming " + G.IsFreeRoaming);
                    if (G.IsFreeRoaming)
                    {
                        try { agents = Stop(na, nb); Face(na, nb); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo stop again: " + e.Message); }
                    }
                }

                // Fallback (talk scene off, or it could not open): the same scene on the map.
                if (!talked && G.IsFreeRoaming)
                {
                    Action<string> mapResult;
                    var onMap = Compose(plan, false, new SceneBuilder(null, false), out mapResult);
                    result = mapResult;
                    done = false;
                    if (Scenes.PlayOnMap(onMap, c => { choice = c; done = true; }, owner))
                    {
                        Step("playing on the map instead");
                        talked = true;
                        yield return Wait(() => done, 600f);
                        yield return Wait(() => G.IsFreeRoaming && !Scenes.Running, 15f);
                    }
                }

                if (talked)
                {
                    try
                    {
                        if (result != null) result(choice ?? "");
                        Social.RecordTalk(na.heroine);
                        Social.RecordTalk(nb.heroine);
                    }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }

                    // 4. Back on the map: a last word each.
                    if (G.IsFreeRoaming)
                    {
                        done = false;
                        if (Scenes.PlayOnMap(Outro(na.heroine, nb.heroine), c => done = true, owner)) yield return Wait(() => done, 120f);
                    }
                }
            }
            if (agents != null) Release(na, nb, agents);
            else if (_besideAgentOff != null) Release(na, nb, new bool[2]);
            _besideAgentOff = null;
            TalkHer = null;
            Scenes.Owner = null;
            Busy = false;
            Step("done, talked " + talked);
        }

        private static void Step(string what) { CampusLifePlugin.Log.LogInfo("Duo: " + what); }

        private static bool Ready(TalkScene ts)
        {
            return ts != null && Traverse.Create(ts).Field("crossFade").GetValue<UnityEngine.Object>() != null;
        }

        /// <summary>Her greeting is over and the talk buttons are usable.</summary>
        private static bool MenuShown(TalkScene ts)
        {
            if (ts == null) return true;
            var b = Traverse.Create(ts).Field("buttonTalk").GetValue<UnityEngine.UI.Button>();
            return b == null || b.gameObject.activeInHierarchy && b.interactable && b.GetComponentsInParent<CanvasGroup>().All(g => g.alpha > 0.99f && g.interactable);
        }

        /// <summary>The second girl next to the first one, facing the talk camera (the game hid her for the talk).</summary>
        private static void StandBeside(NPC nb, NPC na)
        {
            var her = na.heroine.chaCtrl; // the talk scene's copy of the first girl
            var cam = Camera.main;
            if (her == null || cam == null || nb.chaCtrl == null) return;
            nb.SetActive(true);
            if (nb.move != null) nb.move.animParamSpeed = 0f;
            if (nb.isArrival) nb.SetWaitPoint(null);
            if (nb.agent != null && nb.agent.enabled) { nb.agent.enabled = false; _besideAgentOff = nb; }
            var pos = her.transform.position + her.transform.right * 0.62f - her.transform.forward * 0.08f;
            var look = cam.transform.position - pos;
            look.y = 0f;
            nb.transform.position = pos;
            if (look.sqrMagnitude > 0.01f) nb.transform.rotation = Quaternion.LookRotation(look);
            nb.chaCtrl.ChangeLookEyesTarget(0, cam.transform, 0.5f, 0f, 1f, 2f);
            nb.chaCtrl.ChangeLookNeckTarget(0, cam.transform, 0.5f, 0f, 1f, 0.8f);
        }

        // ---------------- getting there ----------------

        /// <summary>
        /// Two standing spots next to each other in front of the player (or to the side), each reachable from the player
        /// on the navmesh in a straight line, so nobody ends up behind a wall.
        /// </summary>
        private static Vector3[] FindSpots()
        {
            var p = G.Act.Player.transform;
            var fwd = p.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            foreach (var angle in new[] { 0f, -35f, 35f, -70f, 70f, -110f, 110f, -145f, 145f, 180f })
                foreach (var dist in new[] { 1.4f, 1.15f, 1.8f, 2.3f })
                {
                    var dir = Quaternion.Euler(0f, angle, 0f) * fwd;
                    var side = Vector3.Cross(Vector3.up, dir);
                    var c = p.position + dir * dist;
                    Vector3 s1, s2;
                    if (Clear(p.position, c - side * 0.45f, out s1) && Clear(p.position, c + side * 0.45f, out s2) && (s1 - s2).sqrMagnitude > 0.5f * 0.5f)
                        return new[] { s1, s2 };
                }
            return null;
        }

        private static bool Clear(Vector3 from, Vector3 to, out Vector3 spot)
        {
            spot = to;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(to, out hit, 0.5f, NavMesh.AllAreas)) return false;
            spot = hit.position;
            NavMeshHit edge;
            if (NavMesh.Raycast(from, hit.position, out edge, NavMesh.AllAreas)) return false; // a wall or an edge in between
            return Mathf.Abs(hit.position.y - from.y) < 0.5f;
        }

        /// <summary>Stops their AI where they are (standing, not sitting) and keeps the agents' state for later.</summary>
        private static bool[] Stop(NPC na, NPC nb)
        {
            var agents = new bool[2];
            var i = 0;
            foreach (var n in new[] { na, nb })
            {
                try
                {
                    n.Pause(true);
                    if (n.move != null) n.move.animParamSpeed = 0f;
                    // Off her seat or wait point: without one the game plays the standing idle and clears the props.
                    if (n.isArrival) n.SetWaitPoint(null);
                    agents[i] = n.agent != null && n.agent.enabled;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo stop: " + e.Message); }
                i++;
            }
            return agents;
        }

        /// <summary>
        /// Both walk over. A girl sitting or busy at one of the map's action points has her agent switched off and may stand
        /// off the navmesh: she is put back on it first (otherwise she never moved and the scene started with one girl).
        /// One who cannot get there (no path, or no progress for 2.5 s) goes straight to her spot.
        /// </summary>
        private static IEnumerator WalkTo(NPC na, NPC nb, Vector3[] spots)
        {
            var girls = new[] { na, nb };
            var done = new bool[2];
            var best = new float[2];
            var bestAt = new float[2];
            for (var i = 0; i < 2; i++)
            {
                var n = girls[i];
                best[i] = float.MaxValue;
                bestAt[i] = Time.realtimeSinceStartup;
                try
                {
                    if (n == null || n.agent == null) { done[i] = true; continue; }
                    var was = "agent " + (n.agent.enabled ? "on" : "off") + (n.agent.enabled && !n.agent.isOnNavMesh ? " off navmesh" : "") + ", " + (n.transform.position - spots[i]).magnitude.ToString("0.0") + " m away";
                    n.agent.enabled = true;
                    NavMeshHit hit;
                    if (!n.agent.isOnNavMesh && NavMesh.SamplePosition(n.transform.position, out hit, 3f, NavMesh.AllAreas)) n.agent.Warp(hit.position);
                    var ok = n.agent.isOnNavMesh && n.agent.SetDestination(spots[i]);
                    Step(GirlInfo.FirstName(n.heroine) + " walks over (" + was + (ok ? "" : ", no path") + ")");
                    if (!ok) Arrive(n, spots[i], done, i);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo walk: " + e.Message); if (n != null) Arrive(n, spots[i], done, i); }
            }
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 12f && !(done[0] && done[1]))
            {
                for (var i = 0; i < 2; i++)
                {
                    if (done[i]) continue;
                    var n = girls[i];
                    try
                    {
                        var d = (n.transform.position - spots[i]).magnitude;
                        if (!n.agent.pathPending && d < 0.25f) { done[i] = true; n.agent.ResetPath(); continue; }
                        if (!n.agent.pathPending && n.agent.pathStatus != NavMeshPathStatus.PathComplete) { Step(GirlInfo.FirstName(n.heroine) + " has no path"); Arrive(n, spots[i], done, i); continue; }
                        if (d < best[i] - 0.1f) { best[i] = d; bestAt[i] = Time.realtimeSinceStartup; }
                        else if (Time.realtimeSinceStartup - bestAt[i] > 2.5f) { Step(GirlInfo.FirstName(n.heroine) + " stuck " + d.ToString("0.0") + " m away"); Arrive(n, spots[i], done, i); continue; }
                        n.AgentVelocityMoveAnimeUpdate();
                        var v = n.agent.velocity;
                        v.y = 0f;
                        if (v.sqrMagnitude > 0.04f) n.transform.rotation = Quaternion.Slerp(n.transform.rotation, Quaternion.LookRotation(v), 0.25f);
                    }
                    catch { Arrive(n, spots[i], done, i); }
                }
                yield return null;
            }
            for (var i = 0; i < 2; i++)
            {
                var n = girls[i];
                try
                {
                    if (n.agent != null && n.agent.enabled)
                    {
                        n.agent.ResetPath();
                        // Held up by a crowd: the last bit is skipped so the scene can start.
                        if ((n.transform.position - spots[i]).sqrMagnitude > 0.8f * 0.8f) n.agent.Warp(spots[i]);
                        n.agent.enabled = false;
                    }
                    if (n.move != null) { n.move.animParamSpeed = 0f; n.move.MoveUpdate(); }
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo arrive: " + e.Message); }
            }
            Face(na, nb);
        }

        /// <summary>She cannot walk there: put her on her spot.</summary>
        private static void Arrive(NPC n, Vector3 spot, bool[] done, int i)
        {
            done[i] = true;
            try
            {
                if (n.agent != null && n.agent.enabled) { if (n.agent.isOnNavMesh) n.agent.ResetPath(); n.agent.Warp(spot); }
                else n.transform.position = spot;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo arrive: " + e.Message); n.transform.position = spot; }
        }

        private static void Face(NPC na, NPC nb)
        {
            var p = G.Act.Player.transform;
            var cam = Camera.main;
            foreach (var n in new[] { na, nb })
            {
                try
                {
                    if (n.agent != null) n.agent.enabled = false;
                    var look = p.position - n.transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.01f) n.transform.rotation = Quaternion.LookRotation(look);
                    if (cam != null && n.chaCtrl != null)
                    {
                        n.chaCtrl.ChangeLookEyesTarget(0, cam.transform, 0.5f, 0f, 1f, 2f);
                        n.chaCtrl.ChangeLookNeckTarget(0, cam.transform, 0.5f, 0f, 1f, 0.8f);
                    }
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo face: " + e.Message); }
            }
        }


        // The second girl whose NavMeshAgent StandBeside switched off. Stop() on the way back saw it off and remembered
        // "off", so Release never switched it on again and she stood there for good.
        private static NPC _besideAgentOff;

        private static void Release(NPC na, NPC nb, bool[] agents)
        {
            var i = 0;
            foreach (var n in new[] { na, nb })
            {
                try
                {
                    if (n == null) continue;
                    if (n.agent != null && (agents[i] || n == _besideAgentOff)) { n.agent.enabled = true; n.agent.Warp(n.transform.position); }
                    if (n.chaCtrl != null) { n.chaCtrl.ChangeLookEyesPtn(0); n.chaCtrl.ChangeLookNeckPtn(3); }
                    n.Pause(false);
                    // Pause(true) only remembers a pause when the AI was running; the talk scene had already paused
                    // everyone else, so Pause(false) alone left them standing there for good.
                    if (n.AI != null && !n.AI.isRunning) n.AI.Start();
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo release: " + e.Message); }
                i++;
            }
        }
    }
}
