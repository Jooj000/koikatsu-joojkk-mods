using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Album tab: the achievements as a record and a to-do list. Left, the five categories with how many
    /// of each are done and the "N of M" total; right, the open category, one card each: done ones with the day and the
    /// girl, the rest with their goal and progress, the secret ones with a hint until they fall.
    /// </summary>
    internal static partial class CampusPanel
    {
        private static string _albumCat = "Friendship";

        private static void AlbumLeft()
        {
            var all = Achievements.All;
            var done = Achievements.DoneCount;
            var b = GameUi.Block(_left, "album");
            Header(b, "Album");
            Line(b, "<color=" + Hex(GameUi.Gold) + ">" + done + " of " + all.Count + "</color> achievements  " + Bar(done, all.Count, 20), 22f);
            Line(b, "Each one is a goal while you work on it and a memory once it's done: the day it happened, and with whom.", 18f, GameUi.Dim);
            foreach (var cat in Achievements.Cats)
            {
                var c = cat;
                var defs = all.Where(d => d.Cat == c).ToList();
                var n = defs.Count(d => Achievements.Done(d.Id));
                var row = GameUi.Row(b, "row", 44f);
                var btn = SmallButton(row, c + "   " + n + "/" + defs.Count, () => { _albumCat = c; Refresh(); }, 400f);
                if (c == _albumCat) ((Image)btn.targetGraphic).color = new Color(1f, 0.85f, 0.6f, 1f);
                if (n == defs.Count) RowText(row, "<color=" + Hex(GameUi.Gold) + ">complete</color>", 18f);
            }
        }

        private static void AlbumRight()
        {
            var cat = Achievements.Cats.Contains(_albumCat) ? _albumCat : Achievements.Cats[0];
            var head = GameUi.Block(_right, "head");
            var hrow = GameUi.Row(head, "row", 46f);
            RowText(hrow, cat, 28f, GameUi.Gold);
            if (cat == "Trouble") Line(head, "These only keep the record: nothing is taken from you.", 18f, GameUi.Dim);
            if (cat == "Secrets") Line(head, "Hidden until they happen; each one leaves a hint.", 18f, GameUi.Dim);
            foreach (var d in Achievements.All.Where(x => x.Cat == cat))
            {
                var rec = Achievements.Record(d.Id);
                var got = rec != null;
                var card = GameUi.Block(_right, "ach");
                var row = GameUi.Row(card, "row", 38f);
                RowText(row, (got ? "<color=" + Hex(GameUi.Gold) + ">★</color> " : "<color=#7f8590>☆</color> ") + (got || !d.Secret ? d.Name : "???"), 24f, got ? GameUi.White : GameUi.Dim);
                if (got) RowText(row, "<color=" + Hex(GameUi.Green) + ">" + Story(rec) + "</color>", 19f);
                else if (!d.Secret || d.Target > 1)
                {
                    var v = Mathf.Min(Achievements.Value(d), d.Target);
                    RowText(row, (d.Target > 1 ? Bar(v, d.Target, 10) + "  " : "") + v + "/" + d.Target, 19f, GameUi.Dim);
                }
                Line(card, got || !d.Secret ? d.Goal : "Hint: " + d.Hint, 19f, got ? GameUi.Dim : GameUi.White);
            }
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
