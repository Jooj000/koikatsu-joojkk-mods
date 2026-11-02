using System;
using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// conversar com duas namoradas na dupla, ou solo. com as opções de aceitar, recusar mas permitir, e recusar"). Alone
    /// with one girlfriend you ask about the other; with both of them in the duo talk you ask them together and each one
    /// answers, the second one hearing the first. Her answer is Share.AskTrio: yes (the three of you), no but you can
    /// see her (each apart), or no. Two yeses make the circle (Harem); a no stays remembered like any stance.
    /// </summary>
    internal static partial class Conversation
    {
        /// <summary>Your other girlfriends she has no "yes, the three of us" about yet.</summary>
        private static List<SaveData.Heroine> TrioCandidates(SaveData.Heroine h)
        {
            if (h == null || !h.isGirlfriend) return new List<SaveData.Heroine>();
            return Social.Students.Where(o => o != null && o != h && o.isGirlfriend && Share.Stance(h, o) != Share.Trio && !AskedTrioToday(h, o)).ToList();
        }

        private static bool AskedTrioToday(SaveData.Heroine h, SaveData.Heroine o)
        {
            int d;
            return S.Buffs.TryGetValue("trioask:" + GirlInfo.Key(h) + ":" + GirlInfo.Key(o), out d) && d == S.Day;
        }

        private static void MarkTrioAsked(SaveData.Heroine h, SaveData.Heroine o) { S.Buffs["trioask:" + GirlInfo.Key(h) + ":" + GirlInfo.Key(o)] = S.Day; }

        /// <summary>The romance entry alone with her (null when there is no other girlfriend to ask about).</summary>
        private static UEntry TrioAskEntry(SaveData.Heroine h)
        {
            var o = TrioCandidates(h).OrderByDescending(x => Harem.Compatibility(h, x)).FirstOrDefault();
            if (o == null) return null;
            return E("Ask how she'd feel about a trio with " + GirlInfo.FirstName(o), () => TrioAskSolo(h, o));
        }

        private static string AnswerWord(int stance) { return stance == Share.Trio ? "yes" : stance == Share.Separate || stance == Share.Friends ? "apart" : "no"; }

        /// <summary>Her answer in her voice: yes, no but apart, or no; "heard" when the other one answered first.</summary>
        private static string TrioAnswerLine(SaveData.Heroine h, SaveData.Heroine o, int stance, string otherAnswer)
        {
            var on = GirlInfo.FirstName(o);
            if (stance == Share.Trio)
                return otherAnswer == "yes"
                    ? T(h, "...If " + on + " wants it too... then yes. The three of us. || ...She said yes, so... me too. || ...Okay. Together, then.", "She's in?! Then I'm in too! The three of us! || If she's in, so am I! || Yes! All three!", "...If she agrees, I see no reason to refuse. Yes. || ...Then yes. || ...Agreed.", "Well, I'm not letting her have all the fun. Yes~ || Count me in~ || Oh, I'm in. Obviously.")
                    : T(h, "...I've thought about it too, honestly. With " + on + "... yes. || ...Yes. I think I'd like that. || ...With her? ...Okay. Yes.", "The three of us?! ...Yes! Let's do it! || Yes! Why not?! || Honestly? Yes!", "...I've considered it. With " + on + ", yes. || ...Yes. With conditions. || ...Acceptable.", "Took you long enough to ask. Yes~ || Oh, I like where this is going. Yes. || ...Bold. I like bold. Yes.");
            if (stance == Share.Separate || stance == Share.Friends)
                return T(h, "...Not the three of us. I can't. ...But I won't stop you from seeing " + on + ". || ...Not together. But you can see her, if you want. || ...I can't do that. But I won't keep you from her.",
                             "All three? No, no! ...But fine, you can still see her! Just not with me there! || Not together! ...But you can see her. || No trio! But... go see her, okay?",
                             "...No. Not together. You may see " + on + " on your own time. || ...Separately. That's my limit. || ...Apart is fine. Together is not.",
                             "The three of us? Pass. ...See her if you like. Just don't bring her to me. || Not my thing. You can still see her, though. || Together? No. Separately... fine.");
            return T(h, "...No. I'm sorry. And I don't want you seeing " + on + " either. || ...No. I can't share you. || ...Please don't ask me that again.",
                         "NO! No way! And no seeing her either! || No! Absolutely not! || What?! No! Just me!",
                         "...No. And I'd rather you stopped seeing " + on + ". || ...No. || ...That's a firm no.",
                         "Cute idea. No. And she's off the table too. || No. I don't share. || Not a chance, sweetheart.");
        }

        private static Feel TrioFace(int stance) { return stance == Share.Trio ? Feel.Shy : stance == Share.Refuses ? Feel.Pout : Feel.Serious; }

        /// <summary>Both said yes: the circle (the game's harem talk is settled as well).</summary>
        private static void TrioSettle(SaveData.Heroine a, SaveData.Heroine b, int sa, int sb)
        {
            if (sa == Share.Trio && sb == Share.Trio)
            {
                foreach (var g in new[] { a, b }) if (!S.Harem.Contains(GirlInfo.Key(g))) S.Harem.Add(GirlInfo.Key(g));
                Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(b), 4);
                Toasts.Show(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(b) + ": the three of you, together.");
            }
        }

        private static void TrioAskSolo(SaveData.Heroine h, SaveData.Heroine o)
        {
            string why;
            var otherSaid = Share.Said(o, h) && Share.Stance(o, h) == Share.Trio;
            var stance = Share.AskTrio(h, o, otherSaid, out why);
            MarkTrioAsked(h, o);
            var on = GirlInfo.FirstName(o);
            CampusLifePlugin.Log.LogInfo("Talk trio ask: " + GirlInfo.FirstName(h) + " about " + on + " -> " + AnswerWord(stance) + " (" + why + ")");
            Beat(h, Lvl.Romance, "trioask:" + GirlInfo.Key(o), b =>
            {
                b.Me(Lines.Pick("Can I ask you something big? You, me and " + on + "... the three of us. How would you feel about that?",
                                "I've been thinking about " + on + " and you. What if it were the three of us, together?",
                                "Be honest with me. If it were you, me and " + on + "... could that work for you?"));
                if (otherSaid) b.Narr("You tell her " + on + " already said yes.");
                b.Face(h, TrioFace(stance)).Her_(TrioAnswerLine(h, o, stance, otherSaid ? "yes" : ""));
            }, null, r =>
            {
                if (stance == Share.Trio) { Social.AddTrust(h, 3); TrioSettle(h, o, stance, Share.Stance(o, h)); }
                else if (stance == Share.Refuses) { Social.AddTrust(h, -2); G.AddFavor(h, -3, 0); Toasts.Show(GirlInfo.FirstName(h) + " won't share you."); }
                else { Social.AddTrust(h, 1); Toasts.Show(GirlInfo.FirstName(h) + " lets you see " + on + ", apart."); }
            });
        }

        /// <summary>In the duo talk, both of them your girlfriends: you ask them together.</summary>
        private static void TrioAskBoth()
        {
            var t = _trio;
            if (t == null) return;
            var a = t.A;
            var b0 = t.B;
            string wa, wb;
            // The one who wants it more answers first; the other hears her.
            var first = Harem.Compatibility(a, b0) >= 0 && a.lewdness >= b0.lewdness ? a : b0;
            var second = first == a ? b0 : a;
            var s1 = Share.AskTrio(first, second, false, out wa);
            var s2 = Share.AskTrio(second, first, s1 == Share.Trio, out wb);
            MarkTrioAsked(a, b0);
            MarkTrioAsked(b0, a);
            TLog("trio ask: " + GirlInfo.FirstName(first) + " " + AnswerWord(s1) + " (" + wa + "), " + GirlInfo.FirstName(second) + " " + AnswerWord(s2) + " (" + wb + ")");
            var b = new SceneBuilder(a, true);
            b.Me(Lines.Pick("I want to ask you both something, and I want honest answers. What if it were the three of us? Together.",
                            "You two mean a lot to me. Both of you. What would you say to the three of us... being together?",
                            "This is going to sound bold. The three of us, as a couple. Could you see it?"));
            b.Narr(GirlInfo.FirstName(first) + " and " + GirlInfo.FirstName(second) + " look at each other.");
            b.Face(first, TrioFace(s1)).Say(first, TrioAnswerLine(first, second, s1, ""));
            b.Face(second, TrioFace(s2)).Say(second, TrioAnswerLine(second, first, s2, AnswerWord(s1)));
            if (s1 == Share.Trio && s2 == Share.Trio) b.Narr("For a moment nobody says anything. Then " + GirlInfo.FirstName(first) + " laughs, and " + GirlInfo.FirstName(second) + " does too.");
            else if (s1 == Share.Trio || s2 == Share.Trio) b.Narr("It's not the same answer from both of them. The air goes a little tight.");
            var x1 = s1;
            var x2 = s2;
            TrioPlay(a, b, () =>
            {
                foreach (var kv in new[] { new KeyValuePair<SaveData.Heroine, int>(first, x1), new KeyValuePair<SaveData.Heroine, int>(second, x2) })
                {
                    if (kv.Value == Share.Trio) Social.AddTrust(kv.Key, 3);
                    else if (kv.Value == Share.Refuses) { Social.AddTrust(kv.Key, -2); G.AddFavor(kv.Key, -3, 0); }
                    else Social.AddTrust(kv.Key, 1);
                }
                TrioSettle(first, second, x1, x2);
                if (x1 != Share.Trio || x2 != Share.Trio)
                    Toasts.Show(GirlInfo.FirstName(first) + ": " + AnswerWord(x1) + ", " + GirlInfo.FirstName(second) + ": " + AnswerWord(x2));
            }, true);
        }
    }
}
