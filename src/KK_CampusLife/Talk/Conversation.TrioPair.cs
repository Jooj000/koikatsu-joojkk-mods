using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// mais contextuais por estarem em dupla"): the other girl is right there, so some options use her. Ask about her, get
    /// an embarrassing story out of her friend, ask whether she talks about you, slip a note past her, invite both,
    /// compliment one in front of the other, a touch she is not supposed to see. Each one once per talk; the other girl
    /// answers or notices, and the witness rules of the talk with both follow (she may be jealous, a rival may sulk).
    /// Lines from duo3.dlg ("duo3.pair.*"), stock lines here as the fallback.
    /// </summary>
    internal static partial class Conversation
    {
        /// <summary>The options that use the other girl, added to the five lists of the girl you talk to.</summary>
        private static void PairOptions(SaveData.Heroine to, SaveData.Heroine other, List<UEntry> talk, List<UEntry> actions, List<UEntry> together, List<UEntry> romance, List<UEntry> spicy)
        {
            if (to == null || other == null || to.isAnger) return;
            var on = GirlInfo.FirstName(other);
            Func<string, bool> fresh = w => { var s = SessFor(to); return s == null || !s.Did.Contains("t3pair:" + w); };
            if (fresh("about")) talk.Insert(0, E("Ask what she likes about " + on, () => PairTurn("about", to)));
            if (fresh("story")) talk.Add(E("Ask for an embarrassing story about " + on, () => PairTurn("story", to)));
            if (fresh("talks") && to.relation >= 0) talk.Add(E("Ask if " + on + " ever talks about you", () => PairTurn("talks", to)));
            if (fresh("note") && to.relation >= 0) actions.Add(E("Slip her a note while " + on + " isn't looking", () => PairTurn("note", to)));
            if (fresh("invite")) together.Add(E("Invite her out, and " + on + " too", () => PairTurn("invite", to)));
            if (fresh("praise") && to.relation >= 0) romance.Add(E("Compliment her in front of " + on, () => PairTurn("praise", to)));
            if (fresh("hidden") && to.relation >= 1) spicy.Add(E("Brush her hand where " + on + " can't see", () => PairTurn("hidden", to)));
        }

        private static void PairTurn(string what, SaveData.Heroine to)
        {
            var t = _trio;
            if (t == null || to == null) return;
            var other = TrioOther(to);
            if (other == null) return;
            var on = GirlInfo.FirstName(other);
            var tn = GirlInfo.FirstName(to);
            var pair = PairWord(to, other);
            var rival = pair == "rival";
            var b = new SceneBuilder(t.A, true);
            var lvl = Lvl.Friend;
            var q = Q.Good;
            var seen = true;   // the other girl reacts after the turn (the witness rules)
            Action more = null;
            var x = X("act", what, "pair", pair);
            switch (what)
            {
                case "about":
                    q = rival ? Q.Neutral : Q.Good;
                    b.Me(Lines.Pick("So what do you like about " + on + "?", "What do you like most about " + on + ", in your opinion?", "Be honest. What do you like about " + on + "?"));   // her "Like? That's a strong word." quotes it
                    Say2(b, to, other, "duo3.pair.about", rival
                        ? T(to, "...Like? That's a strong word. || ...Like? That's a big word. || ...Um, like? Hmm... || ...What do I like about her? Let me think...", "LIKE?! Ugh... she's... fine. Okay? || Like?! She's... okay! FINE! || Ugh, okay! She's alright! || What do I like? ...She's fun. Don't tell her!", "...Her persistence. Barely. || ...She doesn't give up. That's all. || ...Her stubbornness. Slightly. || ...She's tolerable.", "Hmm... her face when she loses to me~ || Her face when I win. Priceless~ || When she pouts. Adorable. || How easy she is to tease~")
                        : T(to, on + " always notices when I'm down..." + " || " + on + " always knows when I'm sad..." + " || " + on + " is so kind to me..." + " || " + on + " always cheers me up.", on + "'s SO fun! Every day's an adventure with her!" + " || " + on + " makes everything fun!" + " || " + on + "'s the best partner in crime! || Life with " + on + " is never boring!", "She's honest. That's rare. || She's honest. Rare quality. || She's reliable. || She doesn't lie. I value that.", "She laughs at my jokes. Even the bad ones~ || She laughs at all my jokes~ || She puts up with me. Brave girl. || She's fun to mess with."), x);
                    Say2(b, other, to, "duo3.pair.about.other", rival
                        ? T(other, "...I can hear you, you know. || ...I can hear you. || ...I'm right here, you know... || ...You know I can hear you, right?", "HEY! I'm right here! || HELLO! Standing right here! || Hey! I can hear you! || I'm RIGHT HERE!", "...Charming. || ...How flattering. || ...Lovely. || ...I'm present.", "Aww, she does care~ || Aww, she likes me~ || She cares! How sweet! || Oh? She really does like me.")
                        : T(other, "E-eh... you're embarrassing me... || Ah... that's embarrassing... || D-don't say that... || Um... you're making me blush...", "Stoooop, you're making me blush! || Stop! I'm blushing! || Aww, quit it! || Hehe! Stop!", "...Thanks. I suppose. || ...Thank you. I guess. || ...Hm. Thanks. || ...Appreciated.", "Keep going, I'm listening~ || Go on, I'm enjoying this~ || More, please~ || Don't stop now."), x, rival ? Feel.Pout : Feel.Shy);
                    more = () => Arcs.AddBond(GirlInfo.Key(to), GirlInfo.Key(other), rival ? 1 : 2);
                    seen = false;
                    break;
                case "story":
                    q = rival ? Q.Good : Funny(to);
                    b.Me(Lines.Pick("Tell me something embarrassing about " + on + ".", "Got any good stories about " + on + "?", "What's the dumbest thing " + on + " ever did?"));
                    Say2(b, to, other, "duo3.pair.story", T(to,
                        "Um... once " + on + " fell asleep in class and answered the professor in her sleep..." + " || " + on + " once sneezed so loud the whole library jumped..." + " || " + on + " once waved back at someone who wasn't waving at her..." + " || " + on + " talks to the university cat. In full sentences.",
                        "Oh! Oh! " + on + " walked into the wrong locker room once! In front of EVERYONE!" + " || " + on + " tripped on the stage at assembly! Twice!" + " || " + on + " once ate my lunch thinking it was hers!" + " || " + on + " sang in the shower and the whole floor heard!",
                        on + " cried at a cartoon last week. A cartoon about a lost umbrella." + " || " + on + " got lost in the university. In second year." + " || " + on + " apologized to a door she walked into." + " || " + on + " named her plants. All of them.",
                        "She practices her confessions in the mirror. I've seen it~" + " || " + on + " writes his name in her notebook. Hearts too~ || She sleeps with a plushie. Named it after you~" + " || " + on + " rehearses talking to you. Out loud~"), x);
                    Say2(b, other, to, "duo3.pair.story.other", T(other, "Wh-why would you tell him that?! || Why did you tell him that?! || N-no! That's a secret! || Please stop!", "THAT WAS ONE TIME! || ONE TIME! It was ONE TIME! || HEY! You swore! || That's slander!", "...You promised. || ...We had a deal. || ...Traitor. || ...Unforgivable.", "Oh, you are so dead later~ || Oh, you're going to regret that~ || Payback's coming, sweetie. || You're so dead later~"), x, Feel.Surprised);
                    more = () => Arcs.AddBond(GirlInfo.Key(to), GirlInfo.Key(other), rival ? -1 : 1);
                    seen = false;
                    break;
                case "talks":
                    lvl = Lvl.Romance;
                    var into = Interested(other) || other.isGirlfriend;
                    b.Me(Lines.Pick("Does " + on + " ever talk about me?", "Be honest: does " + on + " mention me when I'm not around?", "So... does " + on + " say anything about me?"));
                    x["otherlikes"] = into ? "yes" : "no";
                    Say2(b, to, other, "duo3.pair.talks", into
                        ? T(to, "...She does. A lot, actually. || ...Yes. Quite a lot. || ...She does, actually. || ...Mm. Often.", "ALL the time! It's kind of annoying! || Nonstop! It's exhausting! || Constantly! Ugh! || All. The. Time!", "...More than she'd admit. || ...Frequently. || ...More than she lets on. || ...Often.", "Oh, constantly. Should I tell him what she said~? || Constantly. Want details~? || All the time. Should I share? || Oh, nonstop. I have quotes~")
                        : T(to, "Not really... why, should she? || Not really... should she? || Hmm, I don't think so. || Not that I know...", "Hmm, not that I remember! || Nope, not that I know! || Hmm, nah! || Not really!", "...No. || ...No. || ...Not that I've heard. || ...No.", "Not really. Feeling ignored~? || Not really. Jealous~? || Nope. Feeling neglected? || Hmm, no. Poor you."), x);
                    Say2(b, other, to, "duo3.pair.talks.other", into
                        ? T(other, "D-don't tell him that! || Don't tell him! || P-please, don't! || No! That's private!", "HEY! Traitor! || TRAITOR! || Hey! Zip it! || You promised!", "...I'll remember this. || ...I won't forget this. || ...Noted. || ...Unacceptable.", "...You're the worst. Okay, maybe a little. || ...Worst. ...Okay, maybe a bit true. || ...You're terrible. ...Fine. || ...Rude. Accurate, but rude.")
                        : T(other, "...Um. || ...Um... || ...Well... || ...I don't know...", "Why would I?! || Why would I?! || As if! || Nope!", "...Should I? || ...Should I? || ...Why? || ...Hm.", "Maybe if you were more interesting~ || Maybe if you tried harder~ || Be more interesting, then. || Earn it."), x, into ? Feel.Shy : Feel.Normal);
                    q = Q.Good;
                    if (into) more = () => PayTo(other, Lvl.Romance, Q.Good, "t3pair:talked");
                    seen = false;
                    break;
                case "note":
                    lvl = Lvl.Romance;
                    q = QFromFavor(to, 50, 25);
                    b.Narr("While " + on + " looks away, you slip " + tn + " a folded note.");
                    Reply(b, to, other, "note", q, q == Q.Bad
                        ? Tn("...Um. I'll read it later.", "A note? What is this, middle school?", "...No.", "Cute. Not happening~")
                        : Tn("...! (She tucks it away, red to the ears.)", "Hehe! Secret mission accepted!", "...Fine. Later.", "Ooh, a love letter? Saved~"));
                    seen = Notices(other, t, 0.35f, q);
                    if (seen) Say2(b, other, to, "duo3.pair.note.other", T(other, "...Are you two passing notes? || ...Are you two passing secrets? || ...What was that? || ...Did you just whisper something?", "Hey! What was that?! Let me see! || Hey! What's that?! Share! || I saw! What was it?! || No secrets! Show me!", "...I saw that. || ...I noticed. || ...Caught that. || ...Hm. I saw.", "Secrets? Without me~? || Secrets without me~? Rude. || Whispering? Spill. || Oh~ What's the secret?"), x, Feel.Pout);
                    break;
                case "invite":
                    q = QFromFavor(to, 40, 15);
                    b.Me(Lines.Pick("Want to go out after class sometime, " + tn + "? " + on + " can come too.", "Let's all hang out this weekend. You and " + on + ".", "There's a place I want to take you. " + on + " too, if she wants."));
                    Reply(b, to, other, "invite", q, q == Q.Bad ? Tn("Um... maybe...", "Eh? I'll think about it!", "...We'll see.", "Hmm. Earn it~") : Tn("I'd like that...", "Yes! The three of us!", "...Sure.", "A group date? Bold~"));
                    Say2(b, other, to, "duo3.pair.invite.other", rival
                        ? T(other, "...I'll pass, thanks. || ...No, thank you. || ...I'll skip it. || ...Maybe not.", "With HER? No way. || With her?! Nope! || No way! || Ew, no!", "...Pass. || ...No. || ...Pass. || ...Decline.", "Hmm, I'll think about it. No. || Let me think... no. || Tempting. No. || Hmm. Nah.")
                        : T(other, "Me too? ...Okay! || Me too? Okay! || I can come too? Yay... || Oh! Okay, I'll come!", "Count me in! || I'm in! || Woo! Count me in! || Me too! Yay!", "...Fine. I'll come. || ...I'll come. || ...Fine. || ...Alright.", "Of course I'm coming~ || Obviously I'm coming~ || Like I'd miss it. || Of course. I'm the fun one."), x, rival ? Feel.Pout : Feel.Smile);
                    if (q != Q.Bad)
                        more = () =>
                        {
                            CampusLife.Memories.Add(to, "promise_hangout", "you said the three of us would go out", 1, true, GirlInfo.Key(other));
                            if (!rival) CampusLife.Memories.Add(other, "promise_hangout", "you said the three of us would go out", 1, true, GirlInfo.Key(to));
                        };
                    seen = false;
                    break;
                case "praise":
                    lvl = Lvl.Romance;
                    q = QFromFavor(to, 55, 25);
                    b.Me(Lines.Pick("Honestly, " + tn + ", you're the cutest girl on campus.", tn + " always looks amazing. Right, " + on + "?", "I could listen to " + tn + " talk all day."));
                    Reply(b, to, other, "praise", q, q == Q.Bad
                        ? Tn("D-don't say that in front of her...", "Whoa, awkward much?!", "...Not here.", "Laying it on thick~")
                        : Tn("E-eh?! With " + on + " right here...", "Ehehe! Say it louder!", "...You're embarrassing me.", "I know~ but thanks."));
                    break;
                default:   // hidden
                    lvl = Lvl.Spicy;
                    q = Lewdish(to);
                    b.Narr("Out of " + on + "'s sight, your fingers brush " + tn + "'s hand... and linger.");
                    Reply(b, to, other, "hidden", q, q == Q.Bad
                        ? Tn("...! (She pulls her hand away.)", "Hey! (she hisses) Not now!", "...Stop that.", "...Behave.")
                        : q == Q.Neutral ? Tn("(She glances at " + on + ", flustered.)", "(She bites her lip, trying not to laugh.)", "...(She doesn't move her hand.)", "(She squeezes back, just once.)")
                        : Tn("(She laces her fingers with yours, eyes on the floor.)", "(She grins and squeezes back.)", "...(Her hand stays.)", "(She traces a slow circle on your palm.)"));
                    seen = Notices(other, t, 0.3f, q);
                    break;
            }
            TLog("pair " + what + " with " + tn + " (" + pair + ") -> " + q + (seen ? ", " + on + " notices" : ""));
            var lv = lvl;
            var qq = q;
            var noticed = seen;
            TrioPlay(to, b, () =>
            {
                var s = SessFor(to);
                if (s != null) s.Did.Add("t3pair:" + what);
                PayTo(to, lv, qq, "t3pair:" + what);
                if (more != null) more();
                if (_trio != null) { _trio.LastTo = noticed ? to : null; _trio.LastLvl = noticed ? (Lvl?)lv : null; _trio.LastQ = qq; }
            });
        }

        // ================= her card's refusals as her own words =================

        /// <summary>
        /// Right before the game's H: what her card refuses there (the game keeps those as they are), said her way. The
        /// first one that matters most (protection, then kisses, then the rest), or all at once on a card that refuses
        /// everything. Nothing when her card refuses nothing. From bounds.dlg.
        /// </summary>
        private static void Bounds(SceneBuilder b, SaveData.Heroine h)
        {
            try
            {
                string id;
                var line = BoundsLine(h, out id);
                if (line == null) return;
                b.Face(h, Feel.Shy).Her_(line);
                CampusLifePlugin.Log.LogInfo("Bounds: " + GirlInfo.FirstName(h) + " refuses " + string.Join(", ", CardTraits.HRefusals(h).ToArray()) + " in H (" + id + ")");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Bounds: " + e.Message); }
        }

        /// <summary>What she says about her card's refusals before H (null: she refuses nothing); id is the bank rule or "stock".</summary>
        internal static string BoundsLine(SaveData.Heroine h, out string id)
        {
            id = "";
            var list = CardTraits.HRefusals(h);
            if (list.Count == 0) return null;
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "bounds" });
            c["hbound"] = list.Count == 5 ? "all" : list[0];
            c["hbound2"] = list.Count > 1 && list.Count < 5 ? list[1] : "none";
            c["hbounds"] = list.Count.ToString();
            DlgRule r;
            var line = Bank.Pick("bounds", c, GirlInfo.Key(h), out r);
            if (string.IsNullOrEmpty(line)) return null;   // bounds.dlg has every refusal and tone
            id = r != null ? r.Id : "";
            // Said as it is: a greeting or a remark about the weather ("Sorry, do I know you?", "My shoes are still wet.")
            // does not belong right before H.
            return line;
        }
    }
}
