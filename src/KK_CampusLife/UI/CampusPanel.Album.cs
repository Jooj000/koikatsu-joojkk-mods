using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Album tab: the achievements as a record and a to-do list. Left, the
    /// categories with how many of each are done, the "N of M" total and, in the corner, how many characters the school
    /// has (all of them, and how many came with the game). Right, the open category in three steps, from the simple ones
    /// to the long run: done ones with the day and the girl (and, for "every girl", how many there were), the rest with
    /// their goal and progress, the secret ones with a hint until they fall. Ones about another mod show only with it.
    /// </summary>
    internal static partial class CampusPanel
    {
        private static string _albumCat = "Friendship";

        private static void AlbumLeft()
        {
            var shown = Achievements.Shown.ToList();
            var done = Achievements.DoneCount;
            int chars, game;
            Achievements.Roster(out chars, out game);
            var b = GameUi.Block(_left, "album");
            var hrow = GameUi.Row(b, "head", 46f);
            RowText(hrow, "Album", 28f, GameUi.Gold);
            RowText(hrow, "<align=right>" + chars + " characters  <color=" + Hex(GameUi.Dim) + ">(" + game + " from the game, " + (chars - game) + " yours)</color></align>", 18f);
            Line(b, "<color=" + Hex(GameUi.Gold) + ">" + done + " of " + shown.Count + "</color> achievements  " + Bar(done, shown.Count, 20), 22f);
            Line(b, "Each one is a goal while you work on it and a memory once it's done: the day it happened, and with whom.", 18f, GameUi.Dim);
            foreach (var cat in Achievements.ShownCats)
            {
                var c = cat;
                var defs = shown.Where(d => d.Cat == c).ToList();
                var n = defs.Count(d => Achievements.Done(d.Id));
                var row = GameUi.Row(b, "row", 44f);
                var btn = SmallButton(row, c + "   " + n + "/" + defs.Count, () => { _albumCat = c; Refresh(); }, 400f);
                if (c == _albumCat) ((Image)btn.targetGraphic).color = new Color(1f, 0.85f, 0.6f, 1f);
                if (n == defs.Count) RowText(row, "<color=" + Hex(GameUi.Gold) + ">complete</color>", 18f);
            }
        }

        private static void AlbumRight()
        {
            var cats = Achievements.ShownCats.ToList();
            var cat = cats.Contains(_albumCat) ? _albumCat : cats[0];
            var head = GameUi.Block(_right, "head");
            var hrow = GameUi.Row(head, "row", 46f);
            RowText(hrow, cat, 28f, GameUi.Gold);
            if (cat == "Trouble") Line(head, "These only keep the record: nothing is taken from you.", 18f, GameUi.Dim);
            if (cat == "Secrets") Line(head, "Hidden until they happen; each one leaves a hint.", 18f, GameUi.Dim);
            if (cat == "Extras") Line(head, "About other mods in your game; each shows only while its mod is installed.", 18f, GameUi.Dim);
            for (var tier = 1; tier <= 3; tier++)
            {
                var t = tier;
                var defs = Achievements.Shown.Where(x => x.Cat == cat && x.Tier == t).ToList();
                if (defs.Count == 0) continue;
                var sec = GameUi.Block(_right, "tier", false);
                Line(sec, "<color=" + Hex(GameUi.Gold) + ">" + Achievements.Tiers[t] + "</color>  <color=#7f8590>" + defs.Count(d => Achievements.Done(d.Id)) + "/" + defs.Count + "</color>", 20f);
                foreach (var d in defs) AlbumCard(d);
            }
        }

        private static void AlbumCard(Achievements.Def d)
        {
            var rec = Achievements.Record(d.Id);
            var got = rec != null;
            var target = Achievements.TargetOf(d);
            var card = GameUi.Block(_right, "ach");
            var row = GameUi.Row(card, "row", 38f);
            RowText(row, (got ? "<color=" + Hex(GameUi.Gold) + ">★</color> " : "<color=#7f8590>☆</color> ") + (got || !d.Secret ? d.Name : "???"), 24f, got ? GameUi.White : GameUi.Dim);
            if (got) RowText(row, "<color=" + Hex(GameUi.Green) + ">" + Story(rec) + "</color>", 19f);
            else if (!d.Secret || target > 1)
            {
                var v = Mathf.Min(Achievements.Value(d), target);
                RowText(row, (target > 1 ? Bar(v, target, 10) + "  " : "") + v + "/" + target, 19f, GameUi.Dim);
            }
            Line(card, got || !d.Secret ? d.Goal : "Hint: " + d.Hint, 19f, got ? GameUi.Dim : GameUi.White);
            if (got && d.Roster) Line(card, RosterNote(d), 18f, GameUi.Dim);
        }

        /// <summary>For "every girl": how many there were when it fell (and if they were only the game's own).</summary>
        private static string RosterNote(Achievements.Def d)
        {
            var c = Campus.S.AchCount;
            int girls, all, game;
            c.TryGetValue("roster:" + d.Id, out girls);
            c.TryGetValue("rosterAll:" + d.Id, out all);
            c.TryGetValue("rosterGame:" + d.Id, out game);
            if (all <= 0) return "";
            return "With " + girls + " girl" + (girls == 1 ? "" : "s") + " at school" + (all == game ? ", only the game's own characters." : " (" + (all - game) + " of your own characters).");
        }

        /// <summary>"Day 12, with Mitama".</summary>
        private static string Story(string[] rec)
        {
            var day = rec.Length > 1 ? rec[1] : "?";
            var girl = rec.Length > 2 && rec[2] != "" ? Social.Get(rec[2]) : null;   // the record keeps her key, not her name
            return "Day " + day + (girl != null ? ", with " + GirlInfo.FirstName(girl) : "");
        }

        private static string Bar(int v, int max, int width)
        {
            if (max <= 0) return "";
            var full = Mathf.Clamp(Mathf.RoundToInt(width * (float)v / max), 0, width);
            return "<color=" + Hex(GameUi.Gold) + ">" + new string('■', full) + "</color><color=#5a5f68>" + new string('■', width - full) + "</color>";
        }
    }
}
