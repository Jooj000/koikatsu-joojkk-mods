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
                    b.Me(Lines.Pick("So what do you like about " + on + "?", "What's the best thing about " + on + ", in your opinion?", "Be honest. What's " + on + " really like?"));
                    Say2(b, to, other, "duo3.pair.about", rival
                        ? T(to, "...Like? That's a strong word.", "LIKE?! Ugh... she's... fine. Okay?", "...Her persistence. Barely.", "Hmm... her face when she loses to me~")
                        : T(to, on + " always notices when I'm down...", on + "'s SO fun! Every day's an adventure with her!", "She's honest. That's rare.", "She laughs at my jokes. Even the bad ones~"), x);
                    Say2(b, other, to, "duo3.pair.about.other", rival
                        ? T(other, "...I can hear you, you know.", "HEY! I'm right here!", "...Charming.", "Aww, she does care~")
                        : T(other, "E-eh... you're embarrassing me...", "Stoooop, you're making me blush!", "...Thanks. I suppose.", "Keep going, I'm listening~"), x, rival ? Feel.Pout : Feel.Shy);
                    more = () => Arcs.AddBond(GirlInfo.Key(to), GirlInfo.Key(other), rival ? 1 : 2);
                    seen = false;
                    break;
                case "story":
                    q = rival ? Q.Good : Funny(to);
                    b.Me(Lines.Pick("Tell me something embarrassing about " + on + ".", "Got any good stories about " + on + "?", "What's the dumbest thing " + on + " ever did?"));
                    Say2(b, to, other, "duo3.pair.story", T(to,
                        "Um... once " + on + " fell asleep in class and answered the teacher in her sleep...",
                        "Oh! Oh! " + on + " walked into the wrong locker room once! In front of EVERYONE!",
                        on + " cried at a cartoon last week. A cartoon about a lost umbrella.",
                        "She practices her confessions in the mirror. I've seen it~"), x);
                    Say2(b, other, to, "duo3.pair.story.other", T(other, "Wh-why would you tell him that?!", "THAT WAS ONE TIME!", "...You promised.", "Oh, you are so dead later~"), x, Feel.Surprised);
                    more = () => Arcs.AddBond(GirlInfo.Key(to), GirlInfo.Key(other), rival ? -1 : 1);
                    seen = false;
                    break;
                case "talks":
                    lvl = Lvl.Romance;
                    var into = Interested(other) || other.isGirlfriend;
                    b.Me(Lines.Pick("Does " + on + " ever talk about me?", "Be honest: does " + on + " mention me when I'm not around?", "So... does " + on + " say anything about me?"));
                    x["otherlikes"] = into ? "yes" : "no";
                    Say2(b, to, other, "duo3.pair.talks", into
                        ? T(to, "...She does. A lot, actually.", "ALL the time! It's kind of annoying!", "...More than she'd admit.", "Oh, constantly. Should I tell him what she said~?")
                        : T(to, "Not really... why, should she?", "Hmm, not that I remember!", "...No.", "Not really. Feeling ignored~?"), x);
                    Say2(b, other, to, "duo3.pair.talks.other", into
                        ? T(other, "D-don't tell him that!", "HEY! Traitor!", "...I'll remember this.", "...You're the worst. Okay, maybe a little.")
                        : T(other, "...Um.", "Why would I?!", "...Should I?", "Maybe if you were more interesting~"), x, into ? Feel.Shy : Feel.Normal);
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
                    seen = Random.value < 0.35f;
                    if (seen) Say2(b, other, to, "duo3.pair.note.other", T(other, "...Are you two passing notes?", "Hey! What was that?! Let me see!", "...I saw that.", "Secrets? Without me~?"), x, Feel.Pout);
                    break;
                case "invite":
                    q = QFromFavor(to, 40, 15);
                    b.Me(Lines.Pick("Want to go out after school sometime, " + tn + "? " + on + " can come too.", "Let's all hang out this weekend. You and " + on + ".", "There's a place I want to take you. " + on + " too, if she wants."));
                    Reply(b, to, other, "invite", q, q == Q.Bad ? Tn("Um... maybe...", "Eh? I'll think about it!", "...We'll see.", "Hmm. Earn it~") : Tn("I'd like that...", "Yes! The three of us!", "...Sure.", "A group date? Bold~"));
                    Say2(b, other, to, "duo3.pair.invite.other", rival
                        ? T(other, "...I'll pass, thanks.", "With HER? No way.", "...Pass.", "Hmm, I'll think about it. No.")
                        : T(other, "Me too? ...Okay!", "Count me in!", "...Fine. I'll come.", "Of course I'm coming~"), x, rival ? Feel.Pout : Feel.Smile);
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
                    b.Me(Lines.Pick("Honestly, " + tn + ", you're the cutest girl in school.", tn + " always looks amazing. Right, " + on + "?", "I could listen to " + tn + " talk all day."));
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
                    seen = Random.value < 0.3f;
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
            if (string.IsNullOrEmpty(line)) line = BoundsStock(h, c["hbound"]);
            id = r != null ? r.Id : "stock";
            // Said as it is: a greeting or a remark about the weather ("Sorry, do I know you?", "My shoes are still wet.")
            // does not belong right before H.
            return line;
        }

        private static string BoundsStock(SaveData.Heroine h, string what)
        {
            switch (what)
            {
                case "condom": return T(h, "...You have protection, right? I won't, otherwise.", "Protection first! No arguing!", "Condom. Non-negotiable.", "Rule one: you wear something. No exceptions~");
                case "kiss": return T(h, "Um... just... not on the lips. Not yet.", "No kissing, okay?! The rest is... fine.", "...Don't kiss me. That's all.", "Kisses are off the table. For now~");
                case "anal": return T(h, "And... nothing strange back there, please...", "Normal stuff only, got it?!", "...Nothing weird.", "Keep it vanilla, okay~?");
                case "caress": return T(h, "Don't... touch too much. I get nervous.", "Hands where I can see them!", "...Not too much touching.", "No groping. I mean it~");
                case "massage": return T(h, "...No toys. Please.", "No gadgets! Just you!", "...No devices.", "Leave the toys at home~");
                default: return T(h, "...Go slow. There's a lot I'm not ready for yet.", "Okay but... let's keep it simple, alright?!", "...Gently. I'll tell you what's off limits.", "Slow down, tiger. I have rules~");
            }
        }
    }
}
