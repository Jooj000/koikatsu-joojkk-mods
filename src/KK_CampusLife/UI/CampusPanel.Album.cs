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
            var stages = Achievements.DoneCount;
            var allStages = Achievements.ShownCount;
            int chars, game;
            Achievements.Roster(out chars, out game);
            var b = GameUi.Block(_left, "album");
            var hrow = GameUi.Row(b, "head", 46f);
            RowText(hrow, "Album", 28f, GameUi.Gold);
            RowText(hrow, "<align=right>" + chars + " characters  <color=" + Hex(GameUi.Dim) + ">(" + game + " from the game, " + (chars - game) + " yours)</color></align>", 18f);
            Line(b, "<color=" + Hex(GameUi.Gold) + ">" + Achievements.CompleteCount + " of " + shown.Count + "</color> achievements complete", 22f);
            Line(b, "<color=" + Hex(GameUi.Gold) + ">" + stages + " of " + allStages + "</color> stages  " + Bar(stages, allStages, 20), 20f);
            Line(b, "Each one is a goal while you work on it and a memory once it's done: the day it happened, and with whom. Most of them climb through stages.", 18f, GameUi.Dim);
            foreach (var cat in Achievements.ShownCats)
            {
                var c = cat;
                var defs = shown.Where(d => d.Cat == c).ToList();
                var n = defs.Count(Achievements.Complete);
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
            if (cat == "Stories") Line(head, "Each story heroine and teacher has her own: one stage for every part of her story.", 18f, GameUi.Dim);
            for (var tier = 1; tier <= 3; tier++)
            {
                var t = tier;
                var defs = Achievements.Shown.Where(x => x.Cat == cat && x.Tier == t).ToList();
                if (defs.Count == 0) continue;
                var sec = GameUi.Block(_right, "tier", false);
                Line(sec, "<color=" + Hex(GameUi.Gold) + ">" + Achievements.Tiers[t] + "</color>  <color=#7f8590>" + defs.Count(Achievements.Complete) + "/" + defs.Count + "</color>", 20f);
                foreach (var d in defs) AlbumCard(d);
            }
        }

        /// <summary>
        /// One achievement. A climb shows the stage it reached (its name, its squares "■■□□ 2/4", what it took, the day
        /// and the girl) and then the next one with its progress ("Next: Familiar Face, talk with 10 different girls
        /// ■■■■□□ 6/10"); before its first stage, the first one's goal and progress.
        /// </summary>
        private static void AlbumCard(Achievements.Def d)
        {
            var n = Achievements.StageCount(d);
            var r = Achievements.Reached(d);
            var got = r > 0;
            var hidden = d.Secret && !got;
            var cur = Achievements.StageAt(d, Mathf.Max(1, r));
            var rec = Achievements.Record(d);
            var v = Achievements.Value(d);
            var card = GameUi.Block(_right, "ach");
            var row = GameUi.Row(card, "row", 38f);
            RowText(row, (got ? "<color=" + Hex(GameUi.Gold) + ">★</color> " : "<color=#7f8590>☆</color> ") + (hidden ? "???" : cur.Name), 24f, got ? GameUi.White : GameUi.Dim);
            if (n > 1) RowText(row, "<align=right>" + Squares(r, n) + "  <color=" + Hex(got ? GameUi.Gold : GameUi.Dim) + ">" + r + "/" + n + "</color></align>", 19f);
            else if (got) RowText(row, "<align=right><color=" + Hex(GameUi.Green) + ">" + Story(rec) + "</color></align>", 19f);
            else if (!hidden || cur.At > 1) RowText(row, "<align=right>" + (cur.At > 1 ? Bar(Mathf.Min(v, cur.At), cur.At, 10) + "  " : "") + Mathf.Min(v, cur.At) + "/" + cur.At + "</align>", 19f, GameUi.Dim);

            if (n > 1 && !hidden) Line(card, "<color=" + Hex(GameUi.Gold) + ">" + d.Name + "</color>" + (got ? "  <color=" + Hex(GameUi.Green) + ">" + Story(rec) + "</color>" : ""), 18f, GameUi.Dim);
            Line(card, hidden ? "Hint: " + d.Hint : cur.Goal, 19f, got ? GameUi.Dim : GameUi.White);
            if (n > 1 && !got)
                Line(card, Bar(Mathf.Min(v, cur.At), cur.At, 16) + "  " + Mathf.Min(v, cur.At) + "/" + cur.At, 18f, GameUi.Dim);
            else if (n > 1 && r < n)
            {
                var next = Achievements.StageAt(d, r + 1);
                Line(card, "Next: <color=" + Hex(GameUi.White) + ">" + next.Name + "</color>, " + Lower(next.Goal) + "  " + Bar(Mathf.Min(v, next.At), next.At, 12) + "  " + Mathf.Min(v, next.At) + "/" + next.At, 18f, GameUi.Dim);
            }
            else if (n > 1) Line(card, "<color=" + Hex(GameUi.Gold) + ">Every stage done.</color>", 18f);
            if (got && d.Roster && r >= n) Line(card, RosterNote(d), 18f, GameUi.Dim);
        }

        /// <summary>"Talk with..." inside a sentence: "talk with...".</summary>
        private static string Lower(string s) { return string.IsNullOrEmpty(s) ? "" : char.ToLowerInvariant(s[0]) + s.Substring(1); }

        /// <summary>One square per stage: the ones reached in gold.</summary>
        private static string Squares(int r, int n)
        {
            return "<color=" + Hex(GameUi.Gold) + ">" + new string('■', Mathf.Clamp(r, 0, n)) + "</color><color=#5a5f68>" + new string('□', Mathf.Max(0, n - r)) + "</color>";
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
