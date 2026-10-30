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

        internal static bool Hold;

        /// <summary>Called about once a second from Calendar.Tick, when nothing else is playing.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.DuoScenes.Value || Hold || Busy || !Scenes.CanPlayOnMap) return;
            var period = S.Day * 16 + (int)G.CurrentMask;
            if (period == _lastPeriod || Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 20f;
            if (Random.value > 0.35f) return; // a motive found on this map plays within a minute or two
            var plan = PickPlan();
            if (plan == null) return;
            var kind = KindOf(plan.Why);
            if (!Agenda.May("duo " + plan.Why, kind, plan.A.heroine, plan.B.heroine)) return;
            if (Play(plan)) { _lastPeriod = period; Agenda.Took("duo " + plan.Why, kind, plan.A.heroine, plan.B.heroine); }
        }

        /// <summary>Where a motive sits in the director's agenda: what the player did comes back first.</summary>
        internal static BeatKind KindOf(Motive m)
        {
            switch (m)
            {
                case Motive.Confront: case Motive.Thanks: case Motive.Rivals: return BeatKind.Consequence;
                case Motive.Quarrel: case Motive.Gossip: return BeatKind.Social;
                default: return BeatKind.Personal;
            }
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
            Hangout,    // close friends who both like you just want your opinion
            Caught,     // your girlfriend saw you with her and confronts you both on the spot (Share)
            // Everyday moments: friends who both know you come over with nothing wrong, just to be with you.
            Lunch,      // lunchtime, one of them likes you: eat together
            AfterClub,  // same club, club or after school: how it went
            Study,      // exam week, same class: study together
            Season,     // a festival or seasonal day this week: plans
            Trisal      // both your girlfriends, and they agreed to share you
        }

        /// <summary>The moment's name in duo3.dlg and in the talk with both (the lines and the number of turns follow it).</summary>
        internal static string MomentOf(Motive m)
        {
            switch (m)
            {
                case Motive.Rivals: return "rivals";
                case Motive.Confront: return "confront";
                case Motive.Thanks: return "thanks";
                case Motive.Quarrel: return "quarrel";
                case Motive.Introduce: return "bestfriend";
                case Motive.LeftOut: return "leftout";
                case Motive.Gossip: return "gossip";
                case Motive.Caught: return "caught";
                case Motive.Lunch: return "lunch";
                case Motive.AfterClub: return "club";
                case Motive.Study: return "study";
                case Motive.Season: return "season";
                case Motive.Trisal: return "trisal";
                default: return "hangout";
            }
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
        internal static List<NPC> Candidates()
        {
            var act = G.Act;
            var player = act != null && act.Player != null ? act.Player.transform.position : Vector3.zero;
            return act.npcList.Where(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && GirlInfo.IsRegular(n.heroine)
                                          && (n.transform.position - player).sqrMagnitude < 20f * 20f && n.AI != null && n.AI.isRunning
                                          && n.heroine.talkTime > 0).ToList(); // the game would end the talk at once (she called you over, then sent you away)
        }

        /// <summary>The same pair with the same reason (and the same incident) is remembered with the period it played.</summary>
        private static string RepeatKey(Plan p)
        {
            var ka = GirlInfo.Key(p.A.heroine);
            var kb = GirlInfo.Key(p.B.heroine);
            if (string.CompareOrdinal(ka, kb) > 0) { var x = ka; ka = kb; kb = x; }
            return "duo:" + ka + "+" + kb + ":" + p.Why + (p.Inc != null ? ":" + p.Inc.Id : "");
        }

        /// <summary>
        /// Instead of "every other day": a pair and reason that just played weighs less and comes back with
        /// time; never twice in the same period, a quarter the same day, then 65%, 95%, full.
        /// </summary>
        private static float Fresh(Plan p)
        {
            int at;
            if (!S.Buffs.TryGetValue(RepeatKey(p), out at)) return 1f;
            if (at == S.Day * 16 + (int)G.CurrentMask) return 0f;
            var days = S.Day - at / 16;
            return days <= 0 ? 0.25f : Mathf.Clamp01(0.35f + 0.3f * days);
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
                    if (p == null) continue;
                    p.Weight *= Fresh(p);
                    if (p.Weight > 0f) plans.Add(p);
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

            // Your girlfriend saw you with her and won't wait.
            foreach (var pair in new[] { new[] { na, nb }, new[] { nb, na } })
            {
                bool confront, saw;
                if (pair[0].heroine.isGirlfriend && Share.PendingOther(pair[0].heroine, out confront, out saw) == pair[1].heroine && confront)
                    return MakePlan(pair[0], pair[1], Motive.Caught, 12f, null);
            }

            // Both your girlfriends, who agreed to share you: they come over together.
            if (a.isGirlfriend && b.isGirlfriend && Harem.Agreed(a, b)) return MakePlan(na, nb, Motive.Trisal, 3f, null);

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
            if (bond < 35) return Everyday(na, nb, bond);

            // A best friend you have never talked to, brought by a girl who is already your friend (a stranger came to
            if (bond >= 45)
            {
                if (S.Girl(GirlInfo.Key(b)).TalkDays.Count == 0 && YourFriend(a)) return MakePlan(na, nb, Motive.Introduce, 2f, null);
                if (S.Girl(GirlInfo.Key(a)).TalkDays.Count == 0 && YourFriend(b)) return MakePlan(nb, na, Motive.Introduce, 2f, null);
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

            var day = Everyday(na, nb, bond);
            if (day != null) return day;
            // Close friends who both like you: no news, they just want to see you.
            if (bond >= 60 && Social.Trust(a) >= 40 && Social.Trust(b) >= 40) return MakePlan(na, nb, Motive.Hangout, 0.5f, null);
            return null;
        }

        /// <summary>Friends who both know you, with a reason in the day itself: lunch, club, the exams, an event this week.</summary>
        private static Plan Everyday(NPC na, NPC nb, int bond)
        {
            var a = na.heroine;
            var b = nb.heroine;
            if (bond < 25 || !YourFriend(a) || !YourFriend(b)) return null;
            var lead = a.favor >= b.favor ? na : nb;   // the one who likes you more brings the other
            var other = lead == na ? nb : na;
            var mask = G.CurrentMask;
            if (Calendar.IsExamWeek && a.schoolClass == b.schoolClass) return MakePlan(lead, other, Motive.Study, 1.5f, null);
            if (mask == PeriodMask.Lunch && Mathf.Max(a.favor, b.favor) >= 50) return MakePlan(lead, other, Motive.Lunch, 1.5f, null);
            if ((mask == PeriodMask.Club || mask == PeriodMask.After) && !GirlInfo.InNoClub(a) && a.clubActivities == b.clubActivities)
                return MakePlan(lead, other, Motive.AfterClub, 1.5f, null);
            if (Calendar.SeasonEventThisWeek != "") return MakePlan(lead, other, Motive.Season, 1.2f, null);
            return null;
        }

        /// <summary>She knows you and is your friend (or more): only she brings a friend over or leaves you notes.</summary>
        internal static bool YourFriend(SaveData.Heroine h) { return h != null && (h.relation >= 1 || h.isGirlfriend) && Social.KnowsYou(h); }

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
                    ? Lines.T(ta, "Um... " + bn + " told me what you said to her. That wasn't very nice..." + " || " + bn + " told me what you said... that wasn't kind. || Um... " + bn + " was hurt by what you said. || What you said to " + bn + "... it made her sad.", "Hey! What's the big idea, being mean to " + bn + "?! || Hey! Why were you mean to " + bn + "?! || Not cool, being rude to " + bn + "! || What's your problem with " + bn + "?!", "I heard how you treated " + bn + ". Explain. || You mistreated " + bn + ". Explain." + " || " + bn + " told me. Account for it. || Your behavior toward " + bn + ". Explain.", "So you were rude to my friend. Want to try that again? || Rude to my friend? Try again. || You hurt " + bn + ". Bold. || So you were nasty to " + bn + ". Brave.")
                    : Lines.T(ta, "You promised " + bn + " something... and didn't keep it. She was really waiting... || You promised " + bn + "... and didn't come. She waited." + " || " + bn + " waited for you... you forgot. || You broke your promise to " + bn + "...", "Hey! You stood " + bn + " up! Not cool! || You stood " + bn + " up! Uncool! || Hey! " + bn + " waited forever! || You ditched " + bn + "! Seriously?!", "You gave " + bn + " your word. Then nothing. || You promised " + bn + ". Nothing followed. || A broken promise. To " + bn + "." + " || " + bn + " trusted your word.", "Breaking promises to my friend? Bold move. || Breaking promises to my friend? Brave. || Ghosting " + bn + "? Bold. || You flaked on " + bn + ". Explain."));
                sb.Say(bb, Lines.T(tb, "I-it's fine, really... you don't have to... || It's okay, really... || Y-you don't have to... || It's fine... honestly.", "Ehh, I'm not THAT mad... okay, a little. || I'm not THAT mad! Kinda! || Okay, maybe a little mad! || Eh, I'm only a bit upset!", "...I can speak for myself. But yes. || ...I can speak. But yes. || ...True. || ...Yes. That.", "Hmph. Let him squirm a little. || Let him squirm. || Make him sweat a bit. || Hmph. Let him suffer."));
                var opts = new List<string> { "Apologize to " + bn + " sincerely.", "sorry" };
                if (G.Int >= 40) { opts.Add("Explain what really happened. [INT]"); opts.Add("explain"); }
                opts.Add("That's none of your business."); opts.Add("brush");
                sb.Choice(opts.ToArray());
                sb.Branch("sorry");
                sb.Say(bb, Lines.T(tb, "...Okay. Thank you for saying that. || Okay. Thank you. || Thank you for saying that. || ...I forgive you.", "Apology accepted! Just this once! || Accepted! This time! || Okay! Forgiven! || Fine! Apology accepted!", "...Accepted. || ...Accepted. || ...Fine. || ...Very well.", "Fine. You're forgiven. Barely. || Forgiven. Barely. || Fine. Just barely. || You're forgiven. Don't push it."));
                sb.Say(a, Lines.T(ta, "See? That wasn't so hard. || See? Easy. || That wasn't so hard, was it? || Good... thank you.", "Good! Now we're all friends again! || Yay! Friends again! || Great! All good now! || Perfect! Group hug!", "Good. || Good. || Settled. || Fine.", "Hm. Respectable. || Respectable. || Not bad. || Hm. Decent."));
                sb.End();
                sb.Branch("explain");
                sb.Narr("You tell them how it really went. " + bn + "'s face softens.");
                sb.Say(bb, Lines.T(tb, "Oh... I didn't know that... || I didn't know... || Oh... I see now. || Ah... that's different.", "Ohh, so THAT's what happened! || Ohh, THAT'S what happened! || Oh! Got it! || Ahh, makes sense!", "...That changes things. || ...That changes things. || ...Different context. || ...I see.", "Hm. Alright, that's a decent excuse. || Decent excuse. || Alright, fair. || Hm. Acceptable reason."));
                sb.End();
                sb.Branch("brush");
                sb.Say(a, Lines.T(ta, "...I see. Come on, " + bn + ". || ...I see. Let's go, " + bn + ". || ...Okay. Come on, " + bn + ". || ...Alright. " + bn + ", let's go.", "Wow. Unbelievable. Let's go, " + bn + "! || Unbelievable! Come on, " + bn + "! || Wow! Let's go, " + bn + "! || Ugh! " + bn + ", we're leaving!", "Noted. Let's go. || Noted. Go. || Understood. " + bn + ", come. || Fine. Let's go.", "Charming. Come on, " + bn + ". || Charming. " + bn + ", let's go. || Lovely. Come on, " + bn + ". || Wow. " + bn + ", we're done here."));
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
                sb.Say(a, Lines.T(ta, "Go on, " + bn + "... you wanted to say something, right? || Go ahead, " + bn + "..." + " || " + bn + ", you wanted to tell him, right? || Come on, " + bn + "... say it.", "C'mon, " + bn + ", tell him! You've been talking about it all day! || C'mon, " + bn + "! Tell him! || Go, " + bn + "! You talked about it all day!" + " || " + bn + "! Spit it out!", "She has something to tell you. || She has something to say." + " || " + bn + ". Speak. || Go on, " + bn + ".", "Go on. Don't be shy now~ || Don't be shy now~ || Go on, " + bn + "~ || Say it, " + bn + ". He's waiting."));
                sb.Say(bb, defended
                    ? Lines.T(tb, "Thank you... for taking my side. It meant a lot. || Thank you for taking my side. || You defended me... thank you. || It meant so much... thank you.", "Thanks for having my back! You're the best! || You had my back! Thanks! || Thanks for sticking up for me! || You're the best! Thanks!", "...Thank you for standing up for me. || ...Thank you for that. || ...You stood up for me. Thanks. || ...I appreciate it.", "So. You took my side. ...Thanks, I guess. || You took my side. Thanks, I guess. || So, my side. Thanks. || Heh. Thanks for that.")
                    : Lines.T(tb, "Thank you for helping me the other day... || Thank you for helping me... || Thanks for the other day... || I'm grateful for your help.", "Thanks for the help! Seriously! || Thanks for the help! || Seriously, thanks! || You helped! Thanks!", "Thank you for your help. I mean it. || Thank you. I mean it. || Your help mattered. Thanks. || Thanks. Sincerely.", "You helped me out. I owe you one... maybe. || I owe you. Maybe. || You helped. I'll consider repaying. || Thanks. Don't let it go to your head."));
                sb.Choice("It was nothing.", "modest", "Anytime. I've got your back.", "anytime");
                sb.Branch("modest"); sb.Say(bb, Lines.T(tb, "It wasn't nothing to me... || It wasn't nothing to me... || It meant a lot to me... || It mattered to me...", "Don't be so humble! || Don't be humble! || Stop being modest! || Take the credit!", "...Still. Thanks. || ...Still. Thanks. || ...Regardless. Thanks. || ...Thank you anyway.", "Modest, too? Hm. || Modest? Hm. || Humble, too? || Oh, modest. Cute.")); sb.End();
                sb.Branch("anytime"); sb.Say(a, Lines.T(ta, "Ehehe... he's reliable, isn't he? || He's reliable, isn't he? || Hehe... he's dependable. || He's so kind, right?", "See?! Told you he's great! || Told you he's great! || See?! Awesome guy! || He's cool, right?!", "Reliable. Good. || Reliable. Good. || Dependable. || Competent.", "Ooh, smooth. || Smooth. || Ooh, charming. || Slick.")); sb.End();
                var inc = plan.Inc;
                result = c =>
                {
                    Handled(bb, inc);
                    if (c == "modest") { Social.AddTrust(bb, 4); Social.AddTrust(a, 2); G.AddFavor(bb, 2, 0); }
                    else { G.AddFavor(bb, 4, 0); Social.AddTrust(bb, 2); Social.AddTrust(a, 3); }
                };
            }
            else if (plan.Why == Motive.Caught)
            {
                intro.Narr(an + " storms over, pulling " + bn + " along by the wrist.");
                sb.Say(bb, Lines.T(tb, "I-I'm sorry, I didn't... || I-I'm sorry, I didn't mean... || Ah! Sorry... || Oh no... sorry...", "Whoa, whoa, okay, I can explain! || Whoa! I can explain! || Wait! Explanation! || Uh! Hold on!", "...This is awkward. || ...Awkward. || ...Uncomfortable. || ...Hm.", "Oh, this is going to be fun. || This'll be fun. || Oh, entertaining. || Ooh, drama."));
                var act = Share.Write(a, bb, true, sb);
                Share.Hush(a, bb);
                if (act == null) sb.Say(a, Lines.T(ta, "...Never mind. Sorry. || ...Never mind. Sorry. || ...Forget it. || ...It's nothing.", "...Forget it! Sorry! || Forget it! Sorry! || Never mind! || Nothing! Sorry!", "...Never mind. || ...Never mind. || ...Disregard. || ...Nothing.", "...Eh, forget it. || ...Forget it. || ...Eh, whatever. || ...Never mind."));
                result = c => { if (act != null) act(c); };
            }
            else if (plan.Why == Motive.Rivals)
            {
                // Your girlfriend and a girl who clearly likes you.
                var gf = a.isGirlfriend ? a : bb;
                var other = gf == a ? bb : a;
                var gn = GirlInfo.FirstName(gf);
                var on = GirlInfo.FirstName(other);
                intro.Narr(gn + " and " + on + " walk up to you at the same time. The air gets a little tense.");
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "Um, I was hoping to talk to you a little... || I hoped to talk to you a bit... || Um, could we talk? || I wanted to see you...", "Hey! I wanted to hang out with you! || I wanted to hang out! || Hey! Let's chill! || Hang with me!", "I wanted a word with you. || A word. || I wanted to talk. || A moment.", "Mind if I borrow him for a bit? || Can I borrow him? || Mind if I steal him? || He's coming with me for a bit."));
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "Ah... he's with me today, sorry... || He's with me today... sorry. || Sorry... he's mine today. || Ah... we're together today.", "Hey, hey! He's MY boyfriend, you know! || He's MY boyfriend! || Hands off! Mine! || Hey! Taken!", "He's busy. With me. || He's busy. With me. || Occupied. By me. || He's mine today.", "Borrow? He's not a library book. He's mine. || He's not a library book. Mine. || Borrow? No. Mine. || Not available. Mine."));
                sb.Choice("Take your girlfriend's hand.", "gf", "We can all talk together.", "both");
                sb.Branch("gf");
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "...Thank you. || ...Thank you. || ...Thanks. || ...I'm glad.", "Hehe! See? || See?! || Hehe! || Told you!", "Good answer. || Good answer. || Correct. || Good.", "Heh. Knew it. || Knew it. || Obviously. || Heh."));
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "I see... sorry for interrupting. || Sorry for interrupting. || I see... sorry. || Okay... another time.", "Aw, okay! Another time! || Another time! || Okay! Later! || Aw! Next time!", "...Understood. || ...Understood. || ...Fine. || ...Noted.", "Hmph. Fine. For now. || Fine. For now. || Hmph. Later. || For now."));
                sb.End();
                sb.Branch("both");
                sb.Say(gf, Lines.T(GirlInfo.ToneOf(gf), "...Okay. If you say so. || If you say so... || Okay... || Alright.", "Fiiine. Together. || Fine! Together! || Okay! All of us! || Fiiine!", "...Fine. || ...Fine. || ...Acceptable. || ...Alright.", "Generous of you. Hmph. || Generous. Hmph. || How kind. Hmph. || Fine. Hmph."));
                sb.Say(other, Lines.T(GirlInfo.ToneOf(other), "Thank you! I'm happy. || Thank you! I'm happy. || Yay... thanks. || Thank you so much.", "Yay! || Yay! || Woo! || Yes!", "Thanks. || Thanks. || Good. || Appreciated.", "How kind~ || How kind~ || Sweet~ || Generous~"));
                sb.End();
                result = c =>
                {
                    if (c == "gf") { G.AddFavor(gf, 5, 0); Social.AddTrust(gf, 4); G.AddFavor(other, -2, 0); }
                    else if (c == "both") { G.AddFavor(other, 3, 0); Social.AddTrust(gf, -3); Social.Add(Social.Kinds.Flirted, other, new[] { gf }, Witness.Flirt); }
                };
            }
            else if (plan.Why == Motive.Quarrel)
            {
                // A quarrel: you take a side, or calm them both down.
                var topic = Lines.Pick("the last melon bread", "whose turn it was to clean the lecture hall", "who gets the window seat", "the club room schedule", "a borrowed manga that came back bent");
                intro.Narr(an + " and " + bn + " are arguing about " + topic + ". They turn to you.");
                sb.Say(a, Lines.T(ta, "You saw it too, right? It was my turn... || It was my turn, right? || You saw it too...? || Tell her it was my turn...", "Tell her I'm right! || Tell her I'm right! || I'm right, right?! || Back me up!", "You're impartial. Settle this. || You're neutral. Decide. || Settle it. || Judge.", "Go on, tell her she's wrong. || Tell her she's wrong. || Go on, choose me. || Say I'm right."));
                sb.Say(bb, Lines.T(tb, "Please, say something... || Please, say something... || Help us... || Um... what do you think?", "No way, I'm right! Right?! || I'm right! Right?! || Tell her! || Me! Obviously!", "This is ridiculous. Decide. || Ridiculous. Decide. || Pick. || Resolve this.", "Oh, this'll be good. Choose wisely. || Choose wisely. || Careful now. || This'll be fun."));
                var opts = new List<string> { "Side with " + an + ".", "a", "Side with " + bn + ".", "b" };
                if (G.Int >= 40) { opts.Add("Find a compromise. [INT]"); opts.Add("calm"); }
                sb.Choice(opts.ToArray());
                sb.Branch("a"); sb.Say(a, Lines.T(ta, "Thank you...! || Thank you! || Yay... thanks! || I knew it!", "Ha! Told you! || Told you! || Ha! || Yes!", "Obviously. || Obviously. || Naturally. || Of course.", "See? || See? || Told you. || Duh."));
                sb.Say(bb, Lines.T(tb, "...I see how it is. || ...I see. || ...Oh. || ...Okay.", "Traitor! || Traitor! || Hey! || Unfair!", "...Noted. || ...Noted. || ...Hm. || ...Remembered.", "Wow. Okay. || Wow. || Okay then. || Noted.")); sb.End();
                sb.Branch("b"); sb.Say(bb, Lines.T(tb, "Thank you...! || Thank you! || Yay... thanks! || I knew it!", "Ha! Told you! || Told you! || Ha! || Yes!", "Obviously. || Obviously. || Naturally. || Of course.", "See? || See? || Told you. || Duh."));
                sb.Say(a, Lines.T(ta, "...I see how it is. || ...I see. || ...Oh. || ...Okay.", "Traitor! || Traitor! || Hey! || Unfair!", "...Noted. || ...Noted. || ...Hm. || ...Remembered.", "Wow. Okay. || Wow. || Okay then. || Noted.")); sb.End();
                sb.Branch("calm");
                sb.Narr("You suggest a fair way to split it. They grumble, then agree.");
                sb.Say(a, Lines.T(ta, "That's... actually fair. || That's fair. || That works... || Okay, that's fair.", "Huh. Okay, fine! || Fine! Okay! || Huh! Alright! || Okay, fair!", "Acceptable. || Acceptable. || Reasonable. || Fair.", "Hmph. Smart. || Smart. || Clever. || Hmph. Good.")); sb.End();
                result = c =>
                {
                    if (c == "a") { Social.Add(Social.Kinds.Defended, a, new[] { bb }); Social.AddTrust(bb, -4); G.AddFavor(a, 4, 0); }
                    else if (c == "b") { Social.Add(Social.Kinds.Defended, bb, new[] { a }); Social.AddTrust(a, -4); G.AddFavor(bb, 4, 0); }
                    else if (c == "calm") { Social.AddTrust(a, 3); Social.AddTrust(bb, 3); Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(bb), 10); Arcs.AddBond(GirlInfo.Key(bb), GirlInfo.Key(a), 10); S.Reputation = Mathf.Clamp(S.Reputation + 2, 0, 100); }
                };
            }
            else if (plan.Why >= Motive.Lunch)
            {
                // The everyday moments: their reason in their own words (duo3.dlg); the talk with both does the rest.
                Conversation.TrioOpener(intro, sb, a, bb, MomentOf(plan.Why));
                if (!inTalk || !Cfg.TrioTalk.Value)
                {
                    sb.Choice("Spend a little time with them.", "join", "Say you're busy right now.", "busy");
                    sb.Branch("join"); sb.Say(a, Lines.T(ta, "Yay... thank you. || Yay... thank you. || Thank you! || I'm so happy.", "Woohoo! || Woohoo! || Yay! || Yes!", "Good. || Good. || Fine. || Excellent.", "Knew you'd say yes~ || Knew you'd agree~ || Of course~ || Obviously~")); sb.End();
                    sb.Branch("busy"); sb.Say(bb, Lines.T(tb, "Ah... okay, next time. || Okay... next time. || Another time, then. || Alright...", "Boo! Next time then! || Boo! Next time! || Aww! Later! || Next time!", "...Understood. || ...Understood. || ...Fine. || ...Later.", "Your loss~ || Your loss~ || Pity~ || Too bad~")); sb.End();
                }
                result = c => { if (c != "busy") { G.AddFavor(a, 2, 0); G.AddFavor(bb, 2, 0); Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(bb), 1); } };
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
                    sb.Say(host, Lines.T(GirlInfo.ToneOf(host), "This is my friend " + nn + "! I wanted you two to meet. || This is " + nn + ", my friend! || I'd like you to meet " + nn + ". || Meet " + nn + "... she's my friend.", "Meet " + nn + ", my best friend! Be nice! || Meet " + nn + "! Bestie! || This is " + nn + "! Be nice! || Say hi to " + nn + "!", "This is " + nn + ". You should know each other." + " || " + nn + ". You should meet. || This is " + nn + "." + " || " + nn + ". Introductions.", "Here, I brought you a friend. Her name's " + nn + ". || Brought you a friend. " + nn + ". || Here's " + nn + ". Be charming." + " || " + nn + ". Behave yourself."));
                    sb.Say(newcomer, Lines.T(GirlInfo.ToneOf(newcomer), "N-nice to meet you... || N-nice to meet you... || H-hello... || Um... hi...", "Heya! I've heard about you! || Heya! Heard about you! || Hi! Finally! || Hey hey!", "Hello. || Hello. || Hi. || ...Hello.", "So you're the famous club guy. Hm. || The famous club guy. Hm. || So you're him. || Oh, you're the one."));
                    sb.Choice("Greet her warmly.", "warm", "Give a quick nod.", "nod");
                    sb.Branch("warm"); sb.Say(newcomer, Lines.T(GirlInfo.ToneOf(newcomer), "You're nice... I'm glad. || You're nice. I'm glad. || You seem kind... || I like you.", "Ooh, I like him! || I like him! || He's fun! || Cool guy!", "...Pleasure. || ...Pleasure. || ...Nice to meet you. || ...Fine.", "Polite. I approve. || Polite. Approved. || Good manners. || Not bad.")); sb.End();
                    sb.Branch("nod"); sb.Say(host, Lines.T(GirlInfo.ToneOf(host), "Um... he's nicer than he looks, I promise! || He's nicer than he looks! || He's really kind, I promise! || He's sweet, really!", "Hey, be friendlier! || Be friendlier! || Smile more! || Hey, be nice!", "...Charming as always. || ...Charming. || ...As usual. || ...Typical.", "Wow, so cool and mysterious. || So mysterious. || Wow, so cool. || Brooding, huh?")); sb.End();
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
                    sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "You and " + jn + " are always together lately... can I join sometimes? || You and " + jn + " are always together... can I join?" + " || " + jn + " and you are so close... may I come along? || I'd like to join you and " + jn + " sometimes...", "Hey! You keep stealing " + jn + " from me! Or me from... wait. Whatever! || You keep stealing " + jn + "! || Hey! " + jn + "'s always with you! || No fair! " + jn + " time!", "You two have been inseparable. I've noticed. || You and " + jn + ". Inseparable. || I've noticed you two." + " || " + jn + " and you. Always.", "So you've been monopolizing " + jn + ". Or is it the other way around? || Monopolizing " + jn + "? || Hogging " + jn + ", are we? || You and " + jn + "? Suspicious."));
                    sb.Say(jealousOf, Lines.T(GirlInfo.ToneOf(jealousOf), "Ah, sorry! I didn't mean to leave you out... || Sorry! I didn't mean to leave you out... || Ah, sorry... || I'm sorry, we forgot you...", "Haha, don't be jealous! || Don't be jealous! || Haha! Jealous?! || Aww, jealous!", "...It's not like that. || ...Not like that. || ...It's nothing. || ...Misunderstanding.", "Jealous? How cute. || Jealous? Cute. || Aww, envious? || Jealousy suits you."));
                    sb.Choice("Let's all hang out together.", "all", "Invite " + ln + " to lunch soon.", "lunch", "Shrug it off.", "shrug");
                    sb.Branch("all"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "Really? Okay! || Really? Okay! || Yay... okay! || Thank you!", "Yes! Group hangout! || Group hangout! || Yes! All of us! || Woo!", "...Fine. Good. || ...Good. || ...Fine. || ...Agreed.", "Deal. || Deal. || Done. || Agreed.")); sb.End();
                    sb.Branch("lunch"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "J-just us? Okay...! || Just us? Okay...! || J-just the two of us? || Oh... okay!", "A date? Kidding! Lunch it is! || Date? Kidding! Lunch! || Lunch! Hehe! || Just us! Fun!", "...I'll hold you to it. || ...I'll remember. || ...Noted. || ...Holding you to it.", "Oh? Now that's interesting. || Now that's interesting. || Oh? Just us? || Intriguing.")); sb.End();
                    sb.Branch("shrug"); sb.Say(left, Lines.T(GirlInfo.ToneOf(left), "...Oh. Okay. || ...Oh. Okay. || ...I see. || ...Alright.", "Wow. Okay then! || Wow. Okay! || Fine then! || Huh! Okay!", "...Right. || ...Right. || ...Hm. || ...Noted.", "Noted. Very noted. || Noted. || Very noted. || Remembered.")); sb.End();
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
                    sb.Say(a, ConvoLines.GossipLine(a, gossip.Kind, name, gossip.What));
                    sb.Say(bb, Lines.T(tb, "It's true, I heard it too... || I heard it too... || It's true... || Yes, I heard...", "Totally true! Everyone knows! || Totally true! || Everyone knows! || Yep!", "That's what they say. || So they say. || That's the talk. || Reportedly.", "The whole university's talking. || Whole university's talking. || Everyone knows. || The gossip's everywhere."));
                    sb.End();
                    result = c => { Social.Mentioned(a, gossip); Social.Mentioned(bb, gossip); };
                }
                else
                {
                    intro.Narr(an + " and " + bn + " wave you over.");
                    sb.Say(a, Lines.T(ta, "We were just talking about you. Only good things! || We were just talking about you. Good things! || Oh, we were talking about you... || Hehe, your ears must be burning.", "Hey! We need a tiebreaker: cats or dogs? || Tiebreaker: cats or dogs?! || Quick! Pizza or ramen?! || Settle it: summer or winter?!", "We were discussing the festival. Opinions? || The festival. Your opinion? || We need a third view. || Discussing plans. Input?", "Speak of the devil. || Speak of the devil. || Oh, it's you. Perfect. || Just the boy we wanted."));
                    sb.Say(bb, Lines.T(tb, "Hehe, it's true. || It's true, hehe. || Hehe... || Mm-hm.", "Answer carefully! || Careful! || Choose wisely! || No pressure!", "Your input is required. || Your input. || Answer. || Opinion required.", "Go on, say something interesting. || Say something interesting. || Impress us. || Go on."));
                    sb.Choice("Cats, obviously.", "x", "Dogs, no contest.", "y");
                    sb.Branch("x"); sb.Say(a, Lines.T(ta, "I knew it! || I knew it! || Yay! || Right!", "YES! || YES! || Woo! || Ha!", "Correct. || Correct. || Right. || Good.", "Good taste. || Good taste. || Smart. || Correct choice.")); sb.End();
                    sb.Branch("y"); sb.Say(bb, Lines.T(tb, "I knew it! || I knew it! || Yay! || Right!", "YES! || YES! || Woo! || Ha!", "Correct. || Correct. || Right. || Good.", "Good taste. || Good taste. || Smart. || Correct choice.")); sb.End();
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

        /// <summary>The talk scene opened for a duo that goes on with both girls: our conversation attaches, but waits.</summary>
        public static bool TrioPending { get; private set; }

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
            var intro = new SceneBuilder(null, false);
            var show = new Show { A = plan.A, B = plan.B, Name = "motive " + plan, Intro = intro, Trio = Cfg.TrioTalk.Value ? MomentOf(plan.Why) : null };
            show.Main = inTalk =>
            {
                Action<string> r;
                var sb = Compose(plan, inTalk, inTalk ? intro : new SceneBuilder(null, false), out r);
                show.Result = r;
                return sb;
            };
            show.Outro = () => Outro(plan.A.heroine, plan.B.heroine);
            show.Done = talked => { if (talked) Achievements.Count("duo:" + MomentOf(plan.Why), plan.A.heroine); };
            var main = show.Main(true); // fills the intro's narration too
            CallOver(intro, plan.A.heroine, plan.B.heroine);
            if (!Stage(show, main)) return false;
            foreach (var h in new[] { plan.A.heroine, plan.B.heroine }) S.Girl(GirlInfo.Key(h)).DuoDay = S.Day;
            S.Buffs[RepeatKey(plan)] = S.Day * 16 + (int)G.CurrentMask;
            return true;
        }

        /// <summary>
        /// A staged scene: who walks over (one or two girls), what plays on the map as they arrive, the scene played in the
        /// talk screen (and its map version if the talk screen cannot open), and their last words on the map.
        /// </summary>
        internal sealed class Show
        {
            public NPC A, B;                        // B may be null: one girl
            public string Name = "";
            public SceneBuilder Intro;              // on the map, when they arrive
            public Func<bool, SceneBuilder> Main;   // true: the talk screen version; false: on the map
            public Action<string> Result;           // set by Main
            public Func<SceneBuilder> Outro;        // may be null
            public Func<bool> IntoH;                // may be null; true after the scene: the talk ends in the game's H
            public Action<bool> Done;               // may be null; true when the scene really played
            public string Trio;                     // may be null; the moment: after the scene the talk goes on with both
        }

        /// <summary>Starts a staged scene (Spotlight episodes use it too). <paramref name="main"/> is Main(true), already built.</summary>
        internal static NPC LastA, LastB; // the last staged pair

        internal static bool Stage(Show show, SceneBuilder main)
        {
            if (Busy || show == null || show.A == null || G.Act == null || G.Act.Player == null) return false;
            var spots = FindSpots();
            if (spots == null) { Step("no free spots in front of the player"); return false; }
            if (show.B == null) spots[0] = (spots[0] + spots[1]) * 0.5f;
            LastA = show.A;
            LastB = show.B;
            Step(show.Name);
            CampusLifePlugin.Instance.StartCoroutine(Run(show, spots, main).PreventFromCrashing());
            return true;
        }

        /// <summary>Both of them ask for a moment (on the map, before the talk scene opens).</summary>
        private static void CallOver(SceneBuilder intro, SaveData.Heroine a, SaveData.Heroine b)
        {
            intro.Say(a, Lines.T(GirlInfo.ToneOf(a), "Um... do you have a minute? || Do you have a moment? || Um... a minute? || Could we talk?", "Hey hey! Got a sec? || Got a sec?! || Hey! Quick! || Yo! Minute?!", "A moment of your time. || A moment. || Time? || Briefly.", "Hey, you. Come here a sec. || Come here. || You. Here. || Hey, handsome. Moment?"));
            intro.Say(b, Lines.T(GirlInfo.ToneOf(b), "We wanted to talk to you... || We wanted to talk... || We'd like to talk to you. || Just a little chat...", "C'mon, it'll be quick! || Quick! Promise! || It'll be fast! || Short, really!", "It won't take long. || Brief. || Short. || Not long.", "Don't keep us waiting~ || Don't keep us waiting~ || Hurry~ || Chop chop~"));
        }

        private static SceneBuilder Outro(SaveData.Heroine a, SaveData.Heroine b)
        {
            var o = new SceneBuilder(null, false);
            o.Say(a, Lines.T(GirlInfo.ToneOf(a), "Thank you for listening... || Thank you for listening. || Thanks... || That was nice.", "That was fun! See ya! || Fun! See ya! || Great! Bye! || Awesome! Later!", "That's all. Thanks. || That's all. Thanks. || Done. || Thanks.", "Well, that was something. Later~ || Later~ || That was something. || See you around~"));
            o.Say(b, Lines.T(GirlInfo.ToneOf(b), "S-see you later... || See you... || Bye... || Take care...", "Bye bye! || Bye bye! || See ya! || Later!", "Until next time. || Until next time. || Later. || Bye.", "Don't miss us too much. || Don't miss us. || Miss us~ || Later, cutie."));
            o.Narr(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(b) + " walk off together.");
            return o;
        }

        private static IEnumerator Wait(Func<bool> done, float seconds)
        {
            var t0 = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - t0 < seconds) yield return null;
        }

        private static IEnumerator Run(Show show, Vector3[] spots, SceneBuilder main)
        {
            var na = show.A;
            var nb = show.B;
            var intro = show.Intro ?? new SceneBuilder(null, false);
            Busy = true;
            var owner = new object();
            Scenes.Owner = owner;
            bool[] agents = null;
            string choice = null;
            var talked = false;
            var intoH = false;
            var trioOn = false;
            var trioEnd = "";
            try { agents = Stop(na, nb); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo stop: " + e.Message); }

            // 1. They walk over to two free spots in front of you (never through or behind a wall). The spots follow you
            // if you walk on; if you leave the map or keep walking away, they give up and the scene does not happen.
            _walkAborted = false;
            yield return CampusLifePlugin.Instance.StartCoroutine(WalkTo(na, nb, spots));

            // 2. On the map: they call you over.
            var done = false;
            if (!_walkAborted && G.IsFreeRoaming && Scenes.PlayOnMap(intro, c => done = true, owner))
            {
                Step("intro playing");
                yield return Wait(() => done, 300f);
                // The game keeps isEventNow on while the ADV event fades out: until the map is free again, everything
                // below would be skipped and the two would just stand there.
                yield return Wait(() => G.IsFreeRoaming && !Scenes.Running, 15f);
                Step("intro over, free roaming " + G.IsFreeRoaming);

                // 3. The talk scene, with the first girl as usual and the second one standing next to her.
                // With no talk time left the game ends the talk at once: the map version instead (episodes too).
                if (Cfg.DuoInTalk.Value && G.IsFreeRoaming && na.heroine.talkTime > 0)
                {
                    Release(na, nb, agents, false); // the game pauses and hides everyone for the talk itself
                    agents = null;
                    var trio = show.Trio != null && nb != null;
                    if (trio) TrioPending = true; else TalkHer = na.heroine;
                    G.Act.SceneEvent(na);
                    yield return Wait(() => Ready(G.TalkScene), 40f);
                    var ts = G.TalkScene;
                    Step("talk scene " + (Ready(ts) ? "open" : "did not open (event " + (G.Act != null && G.Act.isEventNow) + ")"));
                    if (Ready(ts))
                    {
                        // Side by side from the start; again after her greeting, in case the game put them back.
                        try { if (nb != null) StandBeside(nb, na); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo beside: " + e.Message); }
                        yield return Wait(() => MenuShown(ts), 25f); // her greeting first
                        try { if (nb != null) StandBeside(nb, na); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo beside: " + e.Message); }
                        done = false;
                        if (Scenes.PlayInTalk(main, c => { choice = c; done = true; }, false))
                        {
                            talked = true;
                            yield return Wait(() => done, 600f);
                            Step("talk part over, choice '" + choice + "'");
                        }
                        else Step("could not play in the talk scene");
                        TrioPending = false;
                        // The talk goes on with both of them: who you talk to, their reactions, how it ends.
                        if (trio && talked && G.TalkScene == ts)
                        {
                            try { trioOn = Conversation.TrioBegin(ts, nb, show.Trio); }
                            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                            Step(trioOn ? "the talk goes on with both (" + show.Trio + ")" : "the talk with both did not start");
                            if (trioOn)
                            {
                                yield return Wait(() => !Conversation.TrioOn || G.TalkScene != ts, 1800f);
                                trioEnd = Conversation.TrioEnding;
                                Step("talk with both over: " + trioEnd);
                            }
                        }
                        if (!trioOn)
                        {
                            TrioStage.Unframe();
                            KeepStill(null);
                            try { intoH = talked && show.IntoH != null && G.TalkScene == ts && show.IntoH() && IntoH(ts); }
                            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                            try { if (!intoH && G.TalkScene == ts) Traverse.Create(ts).Method("CommandFunc", new object[] { -1 }).GetValue(); }
                            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo end talk: " + e.Message); }
                        }
                    }
                    TrioPending = false;
                    var h3 = trioEnd == "h3";
                    if (intoH) yield return Wait(() => G.TalkScene == null, 60f);
                    // With both, the player may go on talking to the first girl (the other walked off): wait for him.
                    else yield return Wait(() => G.TalkScene == null && G.IsFreeRoaming, trioOn ? 1800f : 60f);
                    if (h3)
                    {
                        intoH = true;
                        yield return CampusLifePlugin.Instance.StartCoroutine(H3(na, nb));
                    }
                    else if (trioEnd == "aleft" && nb != null && G.IsFreeRoaming)
                    {
                        // The first girl walked off: the talk goes on with the second one alone, as a normal talk
                        //. Back from the stage to her spot first, so the game opens it with her there.
                        try
                        {
                            if (!nb.gameObject.activeSelf) nb.SetActive(true);
                            if (spots.Length > 1) nb.transform.position = spots[1];
                        }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo B stays: " + e.Message); }
                        if (nb.heroine.talkTime > 0)
                        {
                            G.Act.SceneEvent(nb);
                            yield return Wait(() => Ready(G.TalkScene), 40f);
                            var tb = G.TalkScene;
                            Step("talk goes on with " + GirlInfo.FirstName(nb.heroine) + ": " + (Ready(tb) ? "open" : "did not open"));
                            if (Ready(tb)) yield return Wait(() => G.TalkScene == null && G.IsFreeRoaming, 1800f);
                        }
                        else Step(GirlInfo.FirstName(nb.heroine) + " has no talk time left: no talk with her alone");
                    }
                    TalkHer = null;
                    Step(intoH ? "talk closed into the game's H" : "back on the map, free roaming " + G.IsFreeRoaming);
                    if (!intoH && G.IsFreeRoaming)
                    {
                        // The second girl stood beside the talk scene's copy of the first one: back to her own spot.
                        // The one who walked off in the talk with both was hidden there: she is back on the map, walking away.
                        try { if (trioEnd == "bleft" && nb != null && !nb.gameObject.activeSelf) nb.SetActive(true); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo B back: " + e.Message); }
                        try { if (nb != null && spots.Length > 1) nb.transform.position = spots[1]; }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo back to spot: " + e.Message); }
                        try { agents = Stop(na, nb); Face(na, nb); }
                        catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo stop again: " + e.Message); }
                    }
                }

                // Fallback (talk scene off, or it could not open): the same scene on the map.
                if (!talked && !intoH && G.IsFreeRoaming)
                {
                    var onMap = show.Main(false);
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
                        if (show.Result != null) show.Result(choice ?? "");
                        Social.RecordTalk(na.heroine);
                        if (nb != null) Social.RecordTalk(nb.heroine);
                    }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }

                    // 4. Back on the map: a last word each (not when one of them walked off, or they went off with you).
                    var outro = show.Outro != null && !intoH && trioEnd != "aleft" && trioEnd != "bleft" ? show.Outro() : null;
                    if (outro != null && G.IsFreeRoaming)
                    {
                        done = false;
                        if (Scenes.PlayOnMap(outro, c => done = true, owner)) yield return Wait(() => done, 120f);
                    }
                }
            }
            if (agents != null) Release(na, nb, agents);
            else if (_besideAgentOff != null) Release(na, nb, new bool[2]);
            _besideAgentOff = null;
            TalkHer = null;
            TrioPending = false;
            Scenes.Owner = null;
            Busy = false;
            LastPlayed = talked;
            Step("done, talked " + talked);
            try { if (show.Done != null) show.Done(talked); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        private static void Step(string what) { CampusLifePlugin.Log.LogInfo("Duo: " + what); }

        /// <summary>
        /// Both said yes to somewhere private: the game's own H with the two of them (an action point H, as a teacher's
        /// event does, with both girls as its partners), once the map is free after the talk.
        /// </summary>
        private static IEnumerator H3(NPC na, NPC nb)
        {
            yield return Wait(() => G.IsFreeRoaming && !Scenes.Running, 15f);
            var act = G.Act;
            IEnumerator run = null;
            try
            {
                if (act != null && act.Player != null && na != null && nb != null)
                {
                    act.Player.isActionNow = true;
                    // The game's own 3P (free H mode 3, and a girl joining an H): category 3000 at a 3P spot. Without it
                    // the H opens in the one-girl category, the second girl has no animation and the load hangs
                    //.
                    var pos = act.Player.position;
                    var rot = act.Player.rotation;
                    try
                    {
                        var spot = GlobalMethod.GetHPointAppointNull(new GlobalMethod.HPointAppointNullDatail
                        {
                            mode = HFlag.EMode.houshi3P, idMap = act.Map != null ? act.Map.no : 0, kindGet = 2,
                            lstCategory = new List<int> { 3000 }, pos = pos
                        });
                        if (spot != null) { pos = spot.pos; rot = spot.rot; }
                    }
                    catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo H with both, spot: " + e.Message); }
                    run = act.ChangeHSpecial(new List<int> { 3000 }, false, pos, rot, d => { }, na, nb);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Duo H with both: " + e); }
            if (run == null) { if (act != null && act.Player != null) act.Player.isActionNow = false; Step("H with both did not start"); yield break; }
            Step("H with both " + GirlInfo.FirstName(na.heroine) + " and " + GirlInfo.FirstName(nb.heroine));
            yield return act.StartCoroutine(run);
            if (act != null && act.Player != null) act.Player.isActionNow = false;
        }

        /// <summary>
        /// Ends the talk the way the game does when she invites you to H and you accept (TalkScene, heroine event 1):
        /// result H, her invitation flags, and the talk unloaded; the action scene then takes the two of you to H.
        /// </summary>
        private static bool IntoH(TalkScene ts)
        {
            if (ts == null || ts.resultInfo == null || ts.targetHeroine == null) return false;
            ts.resultInfo.result = ActionGame.Communication.ResultEnum.H;
            ts.targetHeroine.talkEvent.Add(12);
            ts.targetHeroine.talkEvent.Add(8);
            Singleton<Manager.Scene>.Instance.UnLoad();
            Step("talk ends in H with " + GirlInfo.FirstName(ts.targetHeroine));
            return true;
        }

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
            var cam = (G.TalkScene != null ? Traverse.Create(G.TalkScene).Field("nowCamera").GetValue<Camera>() : null) ?? Camera.main;
            if (her == null || cam == null || nb.chaCtrl == null) return;
            nb.SetActive(true);
            if (nb.move != null) nb.move.animParamSpeed = 0f;
            if (nb.isArrival) nb.SetWaitPoint(null);
            if (nb.agent != null && nb.agent.enabled) { nb.agent.enabled = false; _besideAgentOff = nb; }
            // Both around the spot the camera looks at, as in the talk with both (user, 2026-10-31: one stood in the middle and
            // the other at the edge of the screen until that talk began).
            TrioStage.Frame(her, nb, cam);
            KeepStill(nb);
            nb.chaCtrl.ChangeLookEyesTarget(0, cam.transform, 0.5f, 0f, 1f, 2f);
            nb.chaCtrl.ChangeLookNeckTarget(0, cam.transform, 0.5f, 0f, 1f, 0.8f);
            KeepStill(nb);
        }

        // The second girl of a talk stays where we put her. The game pauses everyone for the talk, but her map body is shown
        // and NPC._isPlayerAction starts her AI again when the player's notice list changes: she left in her talk pose
        // (user, 2026-10-31: Asuka sliding off with her arms crossed in the middle of the talk).
        private static NPC _held;
        private static Vector3 _heldPos;
        private static Quaternion _heldRot;
        private static bool _heldSaid;

        /// <summary>Keeps her where she stands now until the talk closes; null lets go.</summary>
        internal static void KeepStill(NPC n)
        {
            _held = n;
            _heldSaid = false;
            if (n == null) return;
            _heldPos = n.transform.position;
            _heldRot = n.transform.rotation;
        }

        /// <summary>Every frame, after the game's Update (plugin LateUpdate).</summary>
        internal static void LateTick()
        {
            var n = _held;
            if (n == null) return;
            try
            {
                if (!G.TalkSceneOpen) { _held = null; return; }
                var woke = n.AI != null && n.AI.isRunning && !n.AI.isPaused;
                var moved = (n.transform.position - _heldPos).sqrMagnitude > 0.0001f;
                var agentOn = n.agent != null && n.agent.enabled;
                if ((woke || moved || agentOn) && !_heldSaid)
                {
                    _heldSaid = true;
                    Step("holding " + GirlInfo.FirstName(n.heroine) + " (AI awake " + woke + ", moved " + (n.transform.position - _heldPos).magnitude.ToString("0.00") + " m, agent on " + agentOn + ", action " + (n.AI != null ? n.AI.actionNo : -1) + ")");
                }
                if (woke) n.Pause(true);
                if (agentOn) n.agent.enabled = false;
                if (n.move != null && n.move.animParamSpeed != 0f) n.move.animParamSpeed = 0f;
                n.transform.SetPositionAndRotation(_heldPos, _heldRot);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo hold: " + e.Message); }
        }

        // ---------------- getting there ----------------

        /// <summary>
        /// Two standing spots next to each other in front of the player (or to the side), each reachable from the player
        /// on the navmesh in a straight line, so nobody ends up behind a wall.
        /// </summary>
        private static Vector3[] FindSpots()
        {
            var p = G.Act.Player.transform;
            // Around the middle of the player's view, not around him: after a talk he may face the camera, and with the two
            // measured from him one of them came up against the lens (user, 2026-10-31: one in the middle, the other at the edge).
            var cam = Camera.main;
            var from = cam != null ? new Vector3(cam.transform.position.x, p.position.y, cam.transform.position.z) : p.position;
            var fwd = cam != null ? cam.transform.forward : p.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            foreach (var angle in new[] { 0f, -15f, 15f, -35f, 35f, -70f, 70f, -110f, 110f, -145f, 145f, 180f })
                foreach (var dist in new[] { 2.2f, 1.8f, 2.6f, 3.2f })
                {
                    var dir = Quaternion.Euler(0f, angle, 0f) * fwd;
                    var side = Vector3.Cross(Vector3.up, dir);
                    var c = from + dir * dist;
                    Vector3 s1, s2;
                    if ((c - p.position).sqrMagnitude > 0.8f * 0.8f && Clear(p.position, c - side * 0.45f, out s1) && Clear(p.position, c + side * 0.45f, out s2) && (s1 - s2).sqrMagnitude > 0.5f * 0.5f)
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
                    if (n == null) { i++; continue; }
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
        /// <summary>The last walk over was given up (the player left the map or kept walking away).</summary>
        internal static bool _walkAborted;

        /// <summary>True when the last staged scene really played (false: they never reached you). Read by the director.</summary>
        internal static bool LastPlayed;

        /// <summary>The player walked on: new spots in front of where he is now, and both girls head there.</summary>
        private static bool Retarget(NPC[] girls, Vector3[] spots, bool[] done, float[] best, float[] bestAt)
        {
            var fresh = FindSpots();
            if (fresh == null) return false;
            if (girls[1] == null) fresh[0] = (fresh[0] + fresh[1]) * 0.5f;
            for (var i = 0; i < 2; i++)
            {
                spots[i] = fresh[i];
                var n = girls[i];
                if (n == null || n.agent == null) continue;
                try
                {
                    if (!n.agent.enabled) n.agent.enabled = true;
                    if (n.agent.isOnNavMesh) n.agent.SetDestination(spots[i]);
                    done[i] = false;
                    best[i] = float.MaxValue;
                    bestAt[i] = Time.realtimeSinceStartup;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo retarget: " + e.Message); }
            }
            return true;
        }

        private static IEnumerator WalkTo(NPC na, NPC nb, Vector3[] spots)
        {
            var girls = new[] { na, nb };
            var map = G.PlayerMapNo;
            var anchor = G.Act.Player.position;
            var retargets = 0;
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
                    var was = "agent " + (n.agent.enabled ? "on" : "off") + (n.agent.enabled && !n.agent.isOnNavMesh ? " off navmesh" : "") + ", " + (n.transform.position - spots[i]).magnitude.ToString("0.0") + " m away, speed " + n.agent.speed.ToString("0.0") + (n.move != null && n.move.isStop ? ", brake on" : "");
                    n.agent.enabled = true;
                    NavMeshHit hit;
                    if (!n.agent.isOnNavMesh && NavMesh.SamplePosition(n.transform.position, out hit, 3f, NavMesh.AllAreas)) n.agent.Warp(hit.position);
                    // A girl the stage just put on the map has no AI running: the mover's brake and speed are still as it left
                    // them, and the agent never moved.
                    if (n.move != null && n.move.isStop) n.move.isStop = false;
                    if (n.agent.isOnNavMesh) n.agent.isStopped = false;
                    if (n.agent.speed < 0.5f) n.agent.speed = 1.5f;
                    var ok = n.agent.isOnNavMesh && n.agent.SetDestination(spots[i]);
                    Step(GirlInfo.FirstName(n.heroine) + " walks over (" + was + (ok ? "" : ", no path") + ")");
                    if (!ok) Arrive(n, spots[i], done, i);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo walk: " + e.Message); if (n != null) Arrive(n, spots[i], done, i); }
            }
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 12f + 6f * retargets && !(done[0] && done[1]))
            {
                // The player changed map: they don't follow him through the door; the scene is off (the director keeps it).
                if (G.PlayerMapNo != map || G.Act == null || G.Act.Player == null) { _walkAborted = true; Step("the player left the map; they give up"); break; }
                // The player walked on: the spots move with him (before, they kept walking to where he had been).
                var pp = G.Act.Player.position;
                if ((pp - anchor).sqrMagnitude > 1.5f * 1.5f)
                {
                    anchor = pp;
                    if (retargets >= 6) { _walkAborted = true; Step("the player keeps walking away; they give up"); break; }
                    if (Retarget(girls, spots, done, best, bestAt)) { retargets++; Step("the player moved; new spots in front of him (" + retargets + ")"); }
                }
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
            // Still far from the player when time is up (he kept moving): no warp next to him, they give up.
            if (!_walkAborted && G.Act != null && G.Act.Player != null)
                for (var i = 0; i < 2; i++)
                    if (girls[i] != null && (girls[i].transform.position - G.Act.Player.position).sqrMagnitude > 6f * 6f) { _walkAborted = true; Step(GirlInfo.FirstName(girls[i].heroine) + " never caught up; they give up"); }
            for (var i = 0; i < 2; i++)
            {
                var n = girls[i];
                if (n == null) continue; // one-girl scenes (episodes)
                if (_walkAborted) { try { if (n.agent != null && n.agent.enabled && n.agent.isOnNavMesh) n.agent.ResetPath(); } catch { } continue; }
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
                    if (n == null) continue;
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

        // Their agents always go back on: they walked away from their seat or action point, so the state remembered by
        // Stop() no longer applies. A girl who had been sitting was remembered "off" and, after the scene, stood there for
        // good with her AI running (both girls of the gossip and "good listener" scenes, 2026-10-11).
        private static void Release(NPC na, NPC nb, bool[] agents, bool over = true)
        {
            foreach (var n in new[] { na, nb })
            {
                try
                {
                    if (n == null) continue;
                    if (n.agent != null)
                    {
                        n.agent.enabled = true;
                        NavMeshHit hit;
                        n.agent.Warp(NavMesh.SamplePosition(n.transform.position, out hit, 2f, NavMesh.AllAreas) ? hit.position : n.transform.position);
                    }
                    if (n.chaCtrl != null) { n.chaCtrl.ChangeLookEyesPtn(0); n.chaCtrl.ChangeLookNeckPtn(3); }
                    // A talk still open (a wait above ran out): the game shows and restarts everyone when it closes. Showing
                    // her now put her map body on top of the talk scene's copy of her (user, 2026-10-12).
                    if (G.TalkSceneOpen) continue;
                    // Shown again when she is on this map (SetActive does nothing when the game already thinks she is).
                    if (G.Act != null && G.Act.Map != null && n.mapNo == G.Act.Map.no)
                    {
                        n.SetActive(true);
                        if (n.chaCtrl != null && !n.chaCtrl.GetActiveTop()) n.chaCtrl.SetActiveTop(true);
                    }
                    n.Pause(false);
                    // Pause(true) only remembers a pause when the AI was running; the talk scene had already paused
                    // everyone else, so Pause(false) alone left them standing there for good.
                    if (n.AI != null && !n.AI.isRunning) n.AI.Start();
                    // Her action (the seat she left) is over: she picks the next one and walks off.
                    if (over && n.AI != null) n.AI.TimeEnd();
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo release: " + e.Message); }
            }
        }
    }
}
