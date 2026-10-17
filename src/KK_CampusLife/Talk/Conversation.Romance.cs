using System;
using System.Collections.Generic;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The romance page's plain steps speak from the bank: holding hands, "what am I to you", being
    /// sweet, compliments, love talk, jealousy, the "not now" answers. The answer is decided as before by the caller; only the words come from here. {act} is the step, {q} how she
    /// took it, {close} Lines.Closeness and {lvl} how far her feelings go.
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
            return c;
        }

        /// <summary>Her line for a step. Every step and tone has rules; "..." only if a .dlg override left one out.</summary>
        internal static string RomanceSay(SaveData.Heroine h, string act, string q, string lvl = null, string rival = null)
        {
            DlgRule rule;
            return Bank.Pick("romance", RomanceCtx(h, act, q, lvl, rival), GirlInfo.Key(h), out rule) ?? "...";
        }

        /// <summary>Your line for a step.</summary>
        internal static string RomanceMe(SaveData.Heroine h, string act, string lvl = null)
        {
            DlgRule rule;
            return Bank.Pick("me.romance", RomanceCtx(h, act, "", lvl), "player", out rule) ?? "...";
        }

        /// <summary>Her honest answer, from how much she likes you, your relation and how lewd she is with you.</summary>
        internal static string FeelingsLine(SaveData.Heroine h)
        {
            if (h.isGirlfriend && h.lewdness >= 70) return RomanceLines.FeelingsIntimate(GirlInfo.ToneOf(h));
            return RomanceSay(h, "feelings", "yes", h.isGirlfriend ? "gf" : h.favor >= 80 ? "deep" : h.favor >= 50 ? "friend" : "far");
        }

        internal static IEnumerable<string> RomanceSamples(SaveData.Heroine h)
        {
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
            yield return "compliment again: " + RomanceSay(h, "compliment", "again");
            yield return "noticed yes: " + RomanceSay(h, "noticed", "yes");
            yield return "noticed no: " + RomanceSay(h, "noticed", "no");
            yield return "love: " + RomanceSay(h, "love", "");
            yield return "jealous love: " + RomanceSay(h, "jealous", "love", null, "Elma");
            yield return "jealous plain: " + RomanceSay(h, "jealous", "plain", null, "Elma");
            yield return "notnow date: " + RomanceSay(h, "notnow", "date");
            yield return "notnow confess: " + RomanceSay(h, "notnow", "confess");
            yield return "notnow lunch: " + RomanceSay(h, "notnow", "lunch");
            yield return "notnow home: " + RomanceSay(h, "notnow", "home");
            yield return "notnow later: " + RomanceSay(h, "notnow", "later");
            yield return "breakup stay: " + RomanceSay(h, "breakup", "stay");
            yield return "wary no: " + RomanceSay(h, "wary", "no");
            yield return "me compliment: " + RomanceMe(h, "compliment", "effort") + " / " + RomanceMe(h, "compliment", "club") + " / " + RomanceMe(h, "compliment", "smart") + " / " + RomanceMe(h, "compliment", "looks");
        }
    }
}
