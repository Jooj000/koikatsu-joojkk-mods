using System;
using System.Collections.Generic;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// "How are you?" speaks from the bank. Who asks and what it teaches you stay with the caller; only
    /// the words come from here. {amood} is her mood, or "again" when you already asked today, and {close} is Lines.Closeness.
    /// </summary>
    internal static partial class Conversation
    {
        private static Dictionary<string, string> AskCtx(SaveData.Heroine h, string mood)
        {
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "ask" });
            c["amood"] = mood;
            c["close"] = Lines.Closeness(h).ToString();
            return c;
        }

        private static string AskSay(SaveData.Heroine h, string mood)
        {
            DlgRule rule;
            return Bank.Pick("ask", AskCtx(h, mood), GirlInfo.Key(h), out rule) ?? "...";
        }

        /// <summary>Your question.</summary>
        internal static string AskMe(SaveData.Heroine h)
        {
            DlgRule rule;
            return Bank.Pick("me.ask", AskCtx(h, "none"), "player", out rule) ?? "How are you?";
        }

        /// <summary>Her answer, as open as she is with you.</summary>
        internal static string AskMood(SaveData.Heroine h, Mood mood)
        {
            if (mood == Mood.Flirty && Lines.Closeness(h) >= 2 && GirlInfo.ToneOf(h) == Tone.Teasing) return Lines.FlirtyTease();
            return AskSay(h, mood.ToString().ToLowerInvariant());
        }

        /// <summary>She is asked again on the same day.</summary>
        internal static string AskAgain(SaveData.Heroine h) { return AskSay(h, "again"); }

        internal static IEnumerable<string> AskSamples(SaveData.Heroine h)
        {
            yield return "ask me: " + AskMe(h);
            foreach (Mood m in Enum.GetValues(typeof(Mood))) yield return "ask " + m + ": " + AskMood(h, m);
            yield return "ask again: " + AskAgain(h);
        }
    }
}
