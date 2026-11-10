using System;
using System.Collections.Generic;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The romance page's plain steps speak from the bank (romance.dlg): holding hands, "what am I to you", being sweet,
    /// compliments, love talk, jealousy, the "not now" answers. The answer is decided as before by the caller; only the
    /// words come from here. {act} is the step, {q} how she took it, {close} Lines.Closeness and {lvl} how far her feelings
    /// go. The intimate steps (teasing, whispering, the last time, sneaking off, a bold line, what held her back) are in
    /// intimate.dlg.
    /// </summary>
    internal static partial class Conversation
    {
        private static Dictionary<string, string> RomanceCtx(SaveData.Heroine h, string act, string q, string lvl, string rival = null)
        {
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "romance" });
            WatchCtx(h, c);
            c["act"] = act;
            c["q"] = q;
            c["close"] = Lines.Closeness(h).ToString();
            if (lvl != null) c["lvl"] = lvl;
            if (rival != null) c["rname"] = rival;
            // You already tried this step today: her no says so. Counted when she answered (Consent.Pushed).
            var tries = act == "flirt" ? Consent.Tries(h, "flirt", "sweet") : act == "noticed" ? Consent.Tries(h, "compliment") : Consent.Tries(h, act);
            c["pushed"] = tries >= 2 ? "yes" : "no";
            return c;
        }

        // The hook of the line you just said: her answer to the same step in the same frame reads it, so "Kissable?" only
        // follows "Very kissable." Set by RomanceMe, or by RomanceSaid when your line was built in C#.
        private static string _saidAct, _saidHook;
        private static int _saidFrame = -1;

        /// <summary>Her line for a step. Every step and tone has rules; "..." only if a .dlg override left one out.</summary>
        internal static string RomanceSay(SaveData.Heroine h, string act, string q, string lvl = null, string rival = null)
        {
            DlgRule rule;
            var c = RomanceCtx(h, act, q, lvl, rival);
            if (_saidFrame == Time.frameCount && _saidAct == act && !string.IsNullOrEmpty(_saidHook)) c["hook"] = _saidHook;
            return Bank.Pick("romance", c, GirlInfo.Key(h), out rule) ?? "...";
        }

        /// <summary>Your line for a step; its hook goes to her answer.</summary>
        internal static string RomanceMe(SaveData.Heroine h, string act, string lvl = null, string trait = null, string mem = null)
        {
            DlgRule rule;
            var c = RomanceCtx(h, act, "", lvl);
            if (trait != null) c["trait"] = trait;   // what you noticed about her ("your smile")
            if (mem != null) c["mem"] = mem;         // a moment of yours ("we watched the fireworks")
            var line = Bank.Pick("me.romance", c, "player", out rule) ?? "...";
            RomanceSaid(act, rule != null ? rule.Hook : null);
            return line;
        }

        /// <summary>What your line was about, when it was not drawn from the bank.</summary>
        private static void RomanceSaid(string act, string hook)
        {
            _saidAct = act;
            _saidHook = hook;
            _saidFrame = Time.frameCount;
        }

        /// <summary>Her honest answer, from how much she likes you, your relation and how lewd she is with you.</summary>
        internal static string FeelingsLine(SaveData.Heroine h)
        {
            return RomanceSay(h, "feelings", "yes", h.isGirlfriend && h.lewdness >= 70 ? "intimate" : h.isGirlfriend ? "gf" : h.favor >= 80 ? "deep" : h.favor >= 50 ? "friend" : "far");
        }

        /// <summary>{lvl} for whispering and a bold line: your girlfriend or not.</summary>
        private static string GfLvl(SaveData.Heroine h) { return h.isGirlfriend ? "gf" : "notgf"; }

        /// <summary>{lvl} for the last time: your first time together, or after it.</summary>
        private static string MemLvl(SaveData.Heroine h) { return h.hCount <= 1 ? "first" : "again"; }

        /// <summary>What held her back (Consent.HeldBack) as {q}; anything else is "other".</summary>
        private static readonly string[] HeldBackWhys = { "personality", "mood", "guilt", "girlfriend", "soon", "asked", "grudge", "neglected", "name" };

        internal static IEnumerable<string> RomanceSamples(SaveData.Heroine h)
        {
            yield return "tone " + GirlInfo.ToneOf(h) + ", close " + Lines.Closeness(h) + ", girlfriend " + h.isGirlfriend + ", lewdness " + h.lewdness + ", hCount " + h.hCount;
            yield return "hand me: " + RomanceMe(h, "hand");
            yield return "hand yes: " + RomanceSay(h, "hand", "yes");
            yield return "hand no: " + RomanceSay(h, "hand", "no");
            yield return "feelings me: " + RomanceMe(h, "feelings");
            yield return "feelings yes: " + FeelingsLine(h);
            yield return "feelings no: " + RomanceSay(h, "feelings", "no");
            yield return "flirt me: " + RomanceMe(h, "flirt");
            yield return "flirt yes: " + RomanceSay(h, "flirt", "yes");
            yield return "flirt no: " + RomanceSay(h, "flirt", "no");
            yield return "flirt again: " + RomanceSay(h, "flirt", "again");
            yield return "compliment looks: " + RomanceSay(h, "compliment", "yes", "looks");
            yield return "compliment other: " + RomanceSay(h, "compliment", "yes", "other");
            yield return "compliment no: " + RomanceSay(h, "compliment", "no");
            RomanceSaid("compliment", "today");
            yield return "compliment again today: " + RomanceSay(h, "compliment", "again");
            RomanceSaid("compliment", "yesterday");
            yield return "compliment again yesterday: " + RomanceSay(h, "compliment", "again");
            foreach (var kind in new[] { "looks", "effort", "club", "smart" })
            {
                RomanceSaid("compliment", "today");
                yield return "compliment again " + kind + ": " + RomanceSay(h, "compliment", "again", kind);
            }
            yield return "noticed yes: " + RomanceSay(h, "noticed", "yes");
            yield return "noticed no: " + RomanceSay(h, "noticed", "no");
            yield return "love: " + RomanceSay(h, "love", "");
            yield return "jealous love: " + RomanceSay(h, "jealous", "love", null, "Elma");
            yield return "jealous plain: " + RomanceSay(h, "jealous", "plain", null, "Elma");
            RomanceSaid("notnow", "notnow.date"); yield return "notnow date: " + RomanceSay(h, "notnow", "date");
            RomanceSaid("notnow", "notnow.confess"); yield return "notnow confess: " + RomanceSay(h, "notnow", "confess");
            RomanceSaid("notnow", "notnow.lunch"); yield return "notnow lunch: " + RomanceSay(h, "notnow", "lunch");
            RomanceSaid("notnow", "notnow.home"); yield return "notnow home: " + RomanceSay(h, "notnow", "home");
            RomanceSaid("notnow", "notnow.study"); yield return "notnow later: " + RomanceSay(h, "notnow", "later");
            yield return "breakup stay: " + RomanceSay(h, "breakup", "stay");
            yield return "wary no: " + RomanceSay(h, "wary", "no");
            yield return "me compliment: " + RomanceMe(h, "compliment", "effort") + " / " + RomanceMe(h, "compliment", "club") + " / " + RomanceMe(h, "compliment", "smart") + " / " + RomanceMe(h, "compliment", "looks");
            yield return "feelings intimate: " + RomanceSay(h, "feelings", "yes", "intimate");
            yield return "tease me: " + RomanceMe(h, "tease") + " / yes: " + RomanceSay(h, "tease", "yes") + " / no: " + RomanceSay(h, "tease", "no");
            foreach (var hook in new[] { "sleepy", "stomach", "looking", "swimsuit", "gym", "library", "rooftop", "cute" })
            {
                RomanceSaid("tease", "tease." + hook);
                yield return "tease " + hook + " yes: " + RomanceSay(h, "tease", "yes");
            }
            yield return "whisper me: " + RomanceMe(h, "whisper", GfLvl(h)) + " / yes: " + RomanceSay(h, "whisper", "yes", GfLvl(h)) + " / no: " + RomanceSay(h, "whisper", "no");
            yield return "memories me: " + RomanceMe(h, "memories", MemLvl(h)) + " / yes: " + RomanceSay(h, "memories", "yes", MemLvl(h)) + " / no: " + RomanceSay(h, "memories", "no");
            yield return "private yes: " + RomanceSay(h, "private", "yes", "moment") + " / no: " + RomanceSay(h, "private", "no", "moment");
            yield return "sneak me: " + RomanceMe(h, "sneak") + " / yes: " + RomanceSay(h, "sneak", "yes") + " / wary: " + RomanceSay(h, "sneak", "wary") + " / no: " + RomanceSay(h, "sneak", "no");
            yield return "bold me: " + RomanceMe(h, "bold", GfLvl(h)) + " / yes: " + RomanceSay(h, "bold", "yes") + " / maybe: " + RomanceSay(h, "bold", "maybe") + " / no: " + RomanceSay(h, "bold", "no");
            foreach (var why in HeldBackWhys) yield return "heldback " + why + ": " + HeldBackLine(h, why);
            yield return "heldback other: " + HeldBackLine(h, "other");
            yield return "wary lead: " + WaryLead(h) + "/ no spicy: " + WaryNoLine(h, true);
        }
    }
}
