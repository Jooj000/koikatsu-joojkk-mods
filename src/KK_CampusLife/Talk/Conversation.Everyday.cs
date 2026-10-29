using System;
using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>Her everyday answers, from data\dialogue\everyday.dlg.</summary>
    internal static partial class Conversation
    {
        private static Dictionary<string, string> EverydayCtx(SaveData.Heroine h, string id)
        {
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "everyday" });
            c["s"] = id;
            return c;
        }

        private static string EverydaySay(SaveData.Heroine h, Dictionary<string, string> c)
        {
            DlgRule rule;
            return Bank.Pick("everyday", c, GirlInfo.Key(h), out rule) ?? "...";
        }

        internal static string Everyday(SaveData.Heroine h, string id) { return EverydaySay(h, EverydayCtx(h, id)); }

        internal static string EverydayVent(SaveData.Heroine h, Mood mood)
        {
            var c = EverydayCtx(h, "vent");
            c["vmood"] = mood == Mood.Stressed ? "stressed" : mood == Mood.Upset ? "upset" : mood == Mood.Lonely ? "lonely" : "other";
            return EverydaySay(h, c);
        }

        internal static string EverydayRep(SaveData.Heroine h, int rep)
        {
            var c = EverydayCtx(h, "rep");
            c["trep"] = rep >= 70 ? "high" : rep >= 40 ? "good" : rep >= 20 ? "low" : "bad";
            return EverydaySay(h, c);
        }

        internal static string EverydaySecret(SaveData.Heroine h, bool keep)
        {
            var c = EverydayCtx(h, "secret");
            c["secret"] = keep ? "keep" : "tease";
            return EverydaySay(h, c);
        }

        private static readonly string[] WalkStops = { "gym", "field", "library", "cafeteria", "rooftop", "clubroom", "pool", "courtyard" };

        /// <summary>A stop on a walk with her.</summary>
        internal static string EverydayStop(SaveData.Heroine h, string place)
        {
            var c = EverydayCtx(h, "stop");
            c["stop"] = Array.IndexOf(WalkStops, place) >= 0 ? place : "other";
            return EverydaySay(h, c);
        }

        /// <summary>For the lines that only know her tone.</summary>
        internal static string EverydayTone(Tone t, string id)
        {
            var c = new Dictionary<string, string>();
            c["s"] = id;
            c["tone"] = t.ToString().ToLowerInvariant();
            DlgRule rule;
            return Bank.Pick("everyday", c, "tone", out rule) ?? "...";
        }

        internal static IEnumerable<string> EverydaySamples(SaveData.Heroine h)
        {
            foreach (var id in new[] { "hello", "anecdote", "nothing", "vent.thanks", "joke.good", "joke.bad", "joke.hurt", "explain.ok", "explain.fail", "apology", "plans" })
                yield return "everyday " + id + ": " + Everyday(h, id);
            foreach (var m in new[] { Mood.Stressed, Mood.Upset, Mood.Lonely, Mood.Bored }) yield return "everyday vent " + m + ": " + EverydayVent(h, m);
            foreach (var r in new[] { 80, 50, 30, 10 }) yield return "everyday rep " + r + ": " + EverydayRep(h, r);
            yield return "everyday keep: " + EverydaySecret(h, true);
            yield return "everyday tease: " + EverydaySecret(h, false);
            yield return "everyday by tone: " + EverydayTone(GirlInfo.ToneOf(h), "hello");
            foreach (var id in new[] { "study.yes", "study.no", "fallback" }) yield return "everyday " + id + ": " + Everyday(h, id);
            foreach (var p in WalkStops.Concat(new[] { "somewhere" })) yield return "everyday stop " + p + ": " + EverydayStop(h, p);
        }
    }
}
