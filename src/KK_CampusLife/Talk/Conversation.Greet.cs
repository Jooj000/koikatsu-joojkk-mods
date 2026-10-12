using System;
using System.Collections.Generic;
using System.Linq;
using ADV;

namespace Joojkk.CampusLife
{
    internal static partial class Conversation
    {
        /// <summary>
        /// The game's greeting is a fixed line of her personality, whoever she is and whatever the day ("Don't you think it'd
        /// be fun to go on a picnic...", from a professor between classes). It keeps its voice and her pose; the words come
        /// from the bank, by who she is to you (user, 2026-11-01, "literalmente todas as falas"). What ends the talk or says
        /// why she's angry stays the game's: conditions 0 to 7 and 16 (the restrooms, anger, no time left), and any _isEnd.
        /// </summary>
        public static void Greeting(SaveData.Heroine h, bool ends, int condition, List<Program.Transfer> list)
        {
            if (h == null || list == null || ends || condition < 8 || condition == 16 || !Unified) return;
            var texts = list.Where(t => t != null && t.param != null && t.param.Command == Command.Text).ToList();
            if (texts.Count == 0) return;
            var args = texts[0].param.Args;
            if (args == null || args.Length < 2) return;
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "greet" });
            c["evkind"] = "";
            DlgRule rule;
            var line = Bank.Pick("greet", c, GirlInfo.Key(h), out rule);
            if (string.IsNullOrEmpty(line)) return;
            args[1] = line;
            for (var i = 1; i < texts.Count; i++) list.Remove(texts[i]);
            CampusLifePlugin.Log.LogInfo("Talk greeting: " + GirlInfo.FirstName(h) + " rule " + (rule != null ? rule.Id : "?") + " (game's condition " + condition + "): " + line);
        }
    }
}
