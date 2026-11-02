using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// não são condizentes 'dizer que sabe que dói' para uma estranha, e sendo que o assunto nem tem a ver com isso. os
    /// motivos no clear the air devem dar as opções, não opções genéricas"). The incident gives the five answers (an
    /// apology for a rude word, making up a broken promise, "wrong door" for the restroom, the truth about a kiss); then,
    /// in this order, her memory (the same thing again: "this again"), whether it happened to her or to a friend of hers,
    /// whether she saw it, and how close you are (no "I know it hurt" to a girl you barely know). Each answer has your
    /// own line (Resp.Me), so what you say matches what you chose.
    /// </summary>
    internal static partial class Conversation
    {
        private static Resp Rm(string label, string me, string tag, Q q, string[] her) { var r = Rp(label, tag, q, her); r.Me = me; return r; }

        /// <summary>The chance to talk your way out, shown and used within 5..95%.</summary>
        private static float ClampChance(float x) { return Mathf.Clamp(x, 0.05f, 0.95f); }

        /// <summary>She already let one like it go: the same kind of thing, talked through before (memory first).</summary>
        private static bool AgainOf(SaveData.Heroine h, Incident g)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Cleared.Any(id => id != g.Id && S.Incidents.Any(i => i.Id == id && i.Kind == g.Kind));
        }

        private static List<Resp> ClearAirResps(SaveData.Heroine h, Incident g, string what, bool self, bool explained, float chance)
        {
            var about = Social.Get(g.Girl);
            var who = about == null || about == h ? "her" : GirlInfo.FirstName(about);
            var x = about == null || about == h ? "her" : GirlInfo.FirstName(about);
            var saw = self || Social.Saw(h, g);
            var again = AgainOf(h, g);
            var close = h.isGirlfriend || h.relation >= 1;
            var stranger = h.relation < 0;
            var pct = G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(chance * 100) + "%)</color>" : "";
            var l = new List<Resp>();
            string[] heard = !saw
                ? Tn("...Just what people say. I'd rather hear it from you.", "Only what everyone's saying! So?", "...The short version. Not flattering.", "The juicy version. Correct it, if you can.")
                : Tn("...I didn't hear it. I was there.", "Heard?! I SAW it!", "...I don't need to hear it. I saw it.", "Heard? Cute. I had a front-row seat.");
            switch (g.Kind)
            {
                case Social.Kinds.Rude:
                    if (self)
                        l.Add(again
                            ? Rm("Apologize, and mean it this time", "I said sorry last time and did it again. This time I mean it.", "ca_feel", Q.Neutral, Tn("...You said that before. ...But okay. Last time.", "AGAIN?! ...Fine. Really the last time!", "...Words. Show me instead.", "Second apology. Are you collecting them?"))
                            : Rm("Apologize for what you said", "I'm sorry. What I said to you was out of line.", "ca_feel", Q.Good, Tn("...Okay. Thank you for saying it.", "...Fine! Apology accepted. Mostly!", "...Accepted. Don't repeat it.", "...Hm. Took you long enough.")));
                    else
                        l.Add(Rm("Say you'll apologize to " + who + " yourself", "You're right. I'll apologize to " + who + " myself. Today.", "ca_feel", Q.Good, Tn("...Good. She'd like that.", "You'd better! Today!", "...Then do it.", "To her face. Not to me.")));
                    l.Add(self
                        ? Rm("Ask what hurt the most", "What part of it hit you the hardest?", "ca_facts", Q.Neutral, Tn("...The way you said it. Like I didn't matter.", "The TONE! The whole tone!", "...The part where you meant it.", "Pick one. I'll wait."))
                        : Rm("Ask what " + who + " told her", "What did " + who + " tell you?", "ca_facts", Q.Neutral, saw ? heard : Tn("...Only that it hurt. She doesn't complain much.", "Everything! She was so upset!", "...Enough.", "The highlights. Not flattering.")));
                    l.Add(Rm("Explain you were having an awful day" + pct, "I was in a terrible mood and I took it out on the wrong person. It's not an excuse. It's what happened.", "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...Oh. I didn't know you were having a day like that.", "Wait, that bad?! ...Okay. That explains it.", "...Context. Fine.", "...Huh. You do look like you had a day.")
                                  : Tn("...Everyone has bad days. Not everyone does that.", "So it's the day's fault?! Come on!", "...A bad day isn't a license.", "Blame the calendar. Classic.")));
                    l.Add(Rm(stranger ? "Admit it was uncalled for" : "Admit you went too far", stranger ? "It was uncalled for. I'm sorry." : "I went too far. No excuses.", "ca_admit", Q.Good,
                        again ? Tn("...You know it. That's something. Twice, though.", "At least you know! Twice, though!", "...Admitted. Again.", "Same confession as last time. Noted.")
                              : Tn("...Thank you for being honest.", "...Okay. Respect for owning it.", "...Honest. Good.", "...More than most would say.")));
                    break;

                case Social.Kinds.Ignored:
                    l.Add(self
                        ? Rm("Say you should have stopped for her", "I should've stopped. I'm sorry I walked right past you.", "ca_feel", again ? Q.Neutral : Q.Good, again ? Tn("...That's what you said last time.", "You said that LAST time!", "...Again.", "Deja vu. Same words, same walk.") : Tn("...Yes. You should have. ...Thank you.", "Yes! You should have! ...Okay.", "...Noted.", "Mm. A little late, but noted."))
                        : Rm("Say you'll go check on " + who, "I'll go check on " + who + ". I should have stopped.", "ca_feel", Q.Good, Tn("...Please do. She'd be glad.", "Go! Right now!", "...Do that.", "Go on, then. I'll know if you didn't.")));
                    l.Add(self
                        ? Rm("Ask what she needed back then", "What did you need, back then? Tell me.", "ca_facts", Q.Neutral, Tn("...Just a hand. Someone to notice.", "A HAND! That's all!", "...Help. Briefly.", "Oh, nothing. Just a decent human."))
                        : Rm("Ask if " + who + " is all right now", "Is " + who + " okay now?", "ca_facts", Q.Neutral, Tn("...She's fine now. Someone else helped.", "She's okay! No thanks to you!", "...Recovered.", "Alive. Unimpressed.")));
                    l.Add(Rm("Explain you didn't notice she needed help" + pct, "I honestly didn't notice. If I had, I'd have stopped.", "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...You really didn't see? ...Okay. I believe you.", "You didn't SEE? ...Oh. Okay!", "...Plausible.", "...Fine. You were in your own world.")
                                  : Tn("...You looked right at " + (self ? "me" : who) + ".", "You LOOKED right at us!", "...You saw.", "Your eyes said otherwise.")));
                    l.Add(Rm("Admit you were in a hurry", "I was in a hurry and I didn't stop. That's on me.", "ca_admit", Q.Good, Tn("...Thank you for not making excuses.", "...Okay. At least you said it!", "...Honest.", "...Honest, if not heroic.")));
                    break;

                case Social.Kinds.BrokePromise:
                    if (self)
                        l.Add(close
                            ? Rm("Promise to make it up to her", "I'll make it up to you. Properly, this time.", "ca_feel", again ? Q.Neutral : Q.Good, again ? Tn("...Another promise about a promise?", "A promise about a PROMISE?!", "...Promises. Again.", "Bold. Promising to keep promises.") : Tn("...You'd better. I'm holding you to it.", "You'd better! I'm counting!", "...Noted. Don't break this one.", "Careful. I keep receipts."))
                            : Rm("Say you'll keep the next one", "Next time I say I'll do something, I'll do it.", "ca_feel", Q.Neutral, Tn("...We'll see.", "You'd better!", "...Proof, not words.", "Mm. I'll remember that line.")));
                    else
                        l.Add(Rm("Say you'll make it right with " + who, "I'll make it right with " + who + ". I owe her that.", "ca_feel", Q.Good, Tn("...Good. She was looking forward to it.", "You'd better! She waited!", "...Then do it.", "Tell her, not me.")));
                    l.Add(self
                        ? Rm("Ask how much it mattered to her", "Was it a big deal to you? I want to know.", "ca_facts", Q.Neutral, Tn("...It was. I was looking forward to it.", "YES! I waited and waited!", "...It mattered.", "I cleared my afternoon. So, yes."))
                        : Rm("Ask how " + who + " took it", "How did " + who + " take it?", "ca_facts", Q.Neutral, Tn("...She waited a long time.", "She waited FOREVER!", "...Poorly.", "She's pretending she didn't care. Badly.")));
                    l.Add(Rm("Explain what came up" + pct, "Something came up that I couldn't get out of. I should have said so.", "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...Oh. You should have said, but okay. I get it.", "Something big?! ...Okay, okay. Next time, text!", "...Understandable. Communicate next time.", "...Fine. A real emergency. Rare.")
                                  : Tn("...Something always comes up.", "Something ALWAYS comes up with you!", "...Convenient.", "The famous \"something came up.\"")));
                    l.Add(Rm("Admit you simply forgot", "Honestly? I forgot. That's worse, I know.", "ca_admit", Q.Good, Tn("...It is worse. ...But thank you for not lying.", "You FORGOT?! ...Okay. At least it's the truth.", "...Honest. Painful, but honest.", "...Ouch. Points for honesty.")));
                    break;

                case Social.Kinds.SeenGirlsRoom:
                case Social.Kinds.SeenLockers:
                    l.Add(again
                        ? Rm("Swear you'll stay out from now on", "I'll stay out of there. For good. I swear.", "ca_feel", Q.Neutral, Tn("...That's what you said before.", "You said that LAST time!", "...Repeat offender.", "Mm-hm. Famous last words."))
                        : stranger
                        ? Rm("Apologize for the scare", "Sorry if I scared anyone. That wasn't okay.", "ca_feel", Q.Good, Tn("...It was scary. Thank you for saying it.", "It WAS scary! ...Okay.", "...Accepted.", "...Fine. Knock next time. Or better, don't."))
                        : Rm("Say it won't happen again", "It won't happen again. I mean it.", "ca_feel", Q.Good, Tn("...Okay. I believe you.", "It'd better not!", "...See that it doesn't.", "Mm. Prove it.")));
                    l.Add(Rm("Ask who saw you there", "Who saw me there?", "ca_facts", saw ? Q.Bad : Q.Neutral, saw ? Tn("...Me. I saw you.", "ME! I saw you!", "...I did.", "Look in the mirror. Then at me.") : Tn("...A girl from my class. She told everyone.", "Half the floor! It's all over!", "...Enough people.", "Does it matter? Everyone knows now.")));
                    l.Add(Rm("Explain it was the wrong door" + pct, "It was an honest mistake. Wrong door. I left the second I realized.", "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...The signs are confusing on that floor. Okay.", "Wrong door?! ...Actually, those signs are awful.", "...Plausible. The signs are bad.", "...Fine. You do look lost a lot.")
                                  : Tn("...It's clearly marked.", "It says GIRLS! In big letters!", "...It's labeled.", "The sign has a skirt on it.")));
                    l.Add(Rm("Admit you shouldn't have been there", "I shouldn't have been there. I'm sorry.", "ca_admit", Q.Good, Tn("...No, you shouldn't. Thank you for saying it.", "Nope! ...Okay. Apology taken.", "...Correct.", "...At least you know.")));
                    break;

                case Social.Kinds.Flirted:
                    l.Add(h.isGirlfriend
                        ? Rm("Say she should have heard it from you", "You should have heard it from me first. I'm sorry.", "ca_feel", Q.Good, Tn("...Yes. From you. Not like this.", "YES! From YOU!", "...That would have been decent.", "From you. Not from the grapevine."))
                        : Rm("Say you never meant to hurt her", "I never wanted you to find out like that.", "ca_feel", Q.Neutral, Tn("...But I did. ...Okay.", "Well, I DID find out!", "...Intentions.", "And yet, here we are.")));
                    l.Add(saw
                        ? Rm("Ask what exactly she saw", "What did you see, exactly?", "ca_facts", Q.Bad, Tn("...Enough. More than enough.", "EVERYTHING! I saw everything!", "...Sufficient detail.", "Want me to draw it for you?"))
                        : Rm("Ask who told her", "Who told you that?", "ca_facts", Q.Neutral, Tn("...Does it matter who? Is it true?", "Doesn't matter WHO! Is it TRUE?!", "...A reliable source.", "A little bird. Answer the question.")));
                    string expl, expMe;
                    switch (g.What)
                    {
                        case Witness.Kiss: expl = "Explain the kiss wasn't what it looked like"; expMe = "It wasn't what it looked like. It happened, and it ended there."; break;
                        case Witness.Hand: expl = "Explain you were only helping " + x; expMe = "I was just helping " + x + ". It looked worse than it was."; break;
                        case Witness.Touch: expl = "Explain things got out of hand"; expMe = "It got out of hand, and I stopped it."; break;
                        default: expl = "Explain you were only being friendly"; expMe = "I was being friendly with " + x + ". That's all it was."; break;
                    }
                    l.Add(Rm(expl + pct, expMe, "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...Oh. So that's all it was. ...Okay.", "Wait, REALLY? ...Okay! That changes things!", "...Fine. I believe you.", "...Huh. Smooth. Fine.")
                                  : Tn("...That's not what it looked like to me.", "Oh, come ON!", "...Unconvincing.", "Nice try. Really.")));
                    l.Add(h.isGirlfriend
                        ? Rm("Admit you have feelings for " + x + " too", "I won't lie to you. I have feelings for " + x + " too.", "ca_admit", Q.Good, Tn("...Thank you for not lying to me. It still hurts.", "...Okay. Honest. OUCH, but honest.", "...Honest. I need to think.", "...Well. That's a confession."))
                        : Rm("Admit you and " + x + " are close", x + " and I are close. I won't pretend we aren't.", "ca_admit", Q.Good, Tn("...I see. Thank you for telling me straight.", "...Okay! At least you said it!", "...Understood.", "...Honest. Annoyingly.")));
                    break;

                default:
                    l.Add(Rm(close ? "Say you know it upset her" : "Say you're sorry about " + what, close ? "I know it upset you. I'm sorry." : "I'm sorry about " + what + ".", "ca_feel", self ? Q.Good : Q.Neutral, Tn("...Thank you for saying that.", "...Okay. That helps. A little.", "...Hm. Fine.", "...That's a start.")));
                    l.Add(Rm("Ask what she heard about it", "What did you hear about it?", "ca_facts", self ? Q.Bad : Q.Neutral, heard));
                    l.Add(Rm("Explain what really happened" + pct, "Let me tell you what really happened.", "ca_explain", explained ? Q.Good : Q.Bad,
                        explained ? Tn("...Oh. I didn't know that part.", "Wait, REALLY? Oh!", "...Different. Okay.", "...Huh. Decent excuse.") : Tn("...That sounds like an excuse.", "Come on, really?", "...I don't buy it.", "Nice try.")));
                    l.Add(Rm("Admit the part that's true", "The part you heard that's true... it's true. I'm sorry.", "ca_admit", Q.Good, Tn("...Thank you for being honest.", "...Okay. Respect.", "...Honest.", "...More than most would do.")));
                    break;
            }
            l.Add(stranger
                ? Rm("Leave her be for now", "I'll leave you be. Sorry for all this.", "ca_space", Q.Neutral, Tn("...Okay.", "...Fine. Bye.", "...Yes.", "...Running off? Okay."))
                : Rm("Give her some space", "I'll give you some space. We can talk when you're ready.", "ca_space", Q.Neutral, Tn("...Yeah. Maybe that's best for now.", "...Okay. Later, then.", "...Fine.", "...Running away? ...Okay. Fine.")));
            return l;
        }

        private static List<Resp> ClearAirAllResps(SaveData.Heroine h, List<Incident> all, bool saw, bool explained, float chance)
        {
            var mine = all.Any(i => Social.Get(i.Girl) == h);
            var flirt = all.Any(i => i.Kind == Social.Kinds.Flirted);
            var again = all.Any(i => AgainOf(h, i));
            var stranger = h.relation < 0;
            var pct = G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(chance * 100) + "%)</color>" : "";
            return new List<Resp>
            {
                mine
                    ? Rm(again ? "Say you know it keeps happening" : "Say you've let her down more than once", again ? "I know it keeps happening. I'm not proud of it." : "I've let you down more than once. I know.", "ca_feel", Q.Neutral,
                        Tn("...Knowing isn't the same as stopping.", "...Okay. But it's a LOT.", "...Then stop.", "...Self-awareness. Finally."))
                    : Rm("Say you'll make it right with each of them", "I'll make it right with every one of them. One at a time.", "ca_feel", Q.Neutral,
                        Tn("...That's a long list.", "That's a LOT of making right!", "...Start, then.", "Better clear your schedule.")),
                Rm("Ask what exactly she heard", saw ? "What did you see, exactly?" : "What exactly did you hear?", "ca_facts", saw ? Q.Bad : Q.Neutral,
                    saw ? Tn("...Some of it I didn't need to hear. I saw it.", "Heard?! I SAW some of it!", "...Part of it I watched.", "Heard? Cute. I had a good view.")
                        : Tn("...All of it. Bit by bit.", "Everything! Piece by piece!", "...Enough.", "The whole collection.")),
                Rm((flirt ? "Explain none of it is what it looks like" : "Explain how it all happened") + pct, flirt ? "None of it is what it looks like. Let me explain, one thing at a time." : "Let me explain how it all happened. It's not as bad as it sounds.", "ca_explain", explained ? Q.Good : Q.Bad,
                    explained ? Tn("...Oh. So it wasn't all how it looked. I'm sorry.", "Wait, REALLY? All of it? ...Okay, that changes things!", "...That's different from what I heard. Fine.", "...Huh. Smooth. Fine, I believe you.")
                              : Tn("...One excuse for everything? No.", "That's not gonna cover ALL of it!", "...Too many coincidences.", "One story for all of that? Nice try.")),
                Rm("Admit all of it", "All of it's true. I won't hide behind excuses.", "ca_admit", Q.Good,
                    Tn("...Thank you for being honest. It still hurts.", "...Okay. Respect. Ouch, but respect.", "...All of it. Good. Honest, at least.", "...Well. That's a confession and a half.")),
                stranger
                    ? Rm("Leave her be for now", "I'll leave you be. Sorry for all this.", "ca_space", Q.Neutral, Tn("...Okay.", "...Fine. Bye.", "...Yes.", "...Running off? Okay."))
                    : Rm("Give her some space", "I'll give you some space. We can talk when you're ready.", "ca_space", Q.Neutral, Tn("...Yeah. Maybe that's best for now.", "...Okay. Later, then.", "...Fine.", "...Running away? ...Okay. Fine.")),
            };
        }

        /// <summary>
        /// Admitting it settles it everywhere: the
        /// confession topic about that girl is told, and every "you and her" grudge she holds is cleared.
        /// </summary>
        internal static void SettledAbout(SaveData.Heroine h, SaveData.Heroine other)
        {
            if (h == null || other == null || S == null) return;
            var key = GirlInfo.Key(h);
            var ok = GirlInfo.Key(other);
            if (S.Lore != null)
                foreach (var f in S.Lore)
                    if (Confided(f, key) && (f.Subject == ok || f.Obj == ok)) MarkTopic(key, "confide:" + ok + ":" + f.Id);
            var s = Sess;
            foreach (var i in Social.Grudges(h).Where(i => i.Kind == Social.Kinds.Flirted && i.Girl == ok).ToList())
            {
                Social.Cleared(h, i);
                if (s != null) s.Did.Add("cleared:" + i.Id);
            }
        }
    }
}
