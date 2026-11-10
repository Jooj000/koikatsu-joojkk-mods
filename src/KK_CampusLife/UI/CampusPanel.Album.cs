using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Album tab (redone in the 11th): the achievements as a record and a to-do list. Left, the categories with how
    /// many of each are done, the "N of M" total and, in the corner, how many characters the school has (all of them,
    /// and how many came with the game). Right, the open category in three steps, from the simple ones to the long run:
    /// done ones with the day and the girl (and, for "every girl", how many there were), the rest with their goal and
    /// progress, the secret ones with a hint until they fall. Ones about another mod show only with it.
    /// </summary>
    internal static partial class CampusPanel
    {
        private static string _albumCat = "Friendship";

        // Every category has its icon and
        // color, the left side shows the latest ones done, and each card has a badge, a compact gauge and the category's
        // color when it is done.
        private static string CatGlyph(string cat)
        {
            switch (cat)
            {
                case "Friendship": return "people";
                case "Romance": return "heart";
                case "Campus": return "pin";
                case "Club": return "note";
                case "Stories": return "book";
                case "Trouble": return "cross";
                case "Extras": return "gift";
                default: return "moon";
            }
        }

        private static Color CatColor(string cat)
        {
            switch (cat)
            {
                case "Friendship": return GameUi.Green;
                case "Romance": return Pink;
                case "Campus": return Blue;
                case "Club": return new Color(1f, 0.66f, 0.3f, 1f);
                case "Stories": return GameUi.Gold;
                case "Trouble": return GameUi.Warn;
                case "Extras": return new Color(0.55f, 0.9f, 0.85f, 1f);
                default: return new Color(0.72f, 0.55f, 1f, 1f);
            }
        }

        private static readonly Color Unlit = new Color(0.42f, 0.44f, 0.5f, 1f);

        private static void AlbumLeft()
        {
            var shown = Achievements.Shown.ToList();
            var stages = Achievements.DoneCount;
            var allStages = Achievements.ShownCount;
            int chars, game;
            Achievements.Roster(out chars, out game);
            var b = GameUi.Block(_left, "album");
            Header(b, "Album");
            IconRow(b, "star", GameUi.Gold, C(GameUi.Gold, Achievements.CompleteCount + " of " + shown.Count) + " achievements complete", 22f, 38f);
            IconRow(b, "note", GameUi.Gold, C(GameUi.Gold, stages + "/" + allStages) + " stages  " + Bar(stages, allStages, 14), 19f, 34f);
            IconRow(b, "people", Blue, chars + " characters " + C(GameUi.Dim, "(" + game + " from the game, " + (chars - game) + " yours)"), 18f, 32f);
            Line(b, "A goal while you work on it, a memory once it's done.", 17f, GameUi.Dim);

            var cb = GameUi.Block(_left, "cats");
            Header(cb, "Categories");
            foreach (var cat in Achievements.ShownCats)
            {
                var c = cat;
                var defs = shown.Where(d => d.Cat == c).ToList();
                var n = defs.Count(Achievements.Complete);
                var row = GameUi.Row(cb, "row", 44f);
                Icon(row, Markers.Glyph(CatGlyph(c), CatColor(c)), 36f);
                var btn = SmallButton(row, c, () => { _albumCat = c; Refresh(); }, 190f);
                if (c == _albumCat) ((Image)btn.targetGraphic).color = new Color(1f, 0.85f, 0.6f, 1f);
                RowText(row, Gauge(n, defs.Count, 8, CatColor(c)) + "  " + C(n == defs.Count ? GameUi.Gold : GameUi.Dim, n + "/" + defs.Count), 19f);
            }

            // The latest ones done, with the day and the girl.
            var latest = shown.Where(d => Achievements.Reached(d) > 0)
                .Select(d => new { d, rec = Achievements.Record(d) })
                .Where(x => x.rec != null)
                .OrderByDescending(x => { int day; return x.rec.Length > 1 && int.TryParse(x.rec[1], out day) ? day : 0; })
                .Take(5).ToList();
            if (latest.Count > 0)
            {
                var lb = GameUi.Block(_left, "latest");
                Header(lb, "Latest");
                foreach (var x in latest)
                {
                    var stage = Achievements.StageAt(x.d, Mathf.Max(1, Achievements.Reached(x.d)));
                    IconRow(lb, CatGlyph(x.d.Cat), CatColor(x.d.Cat), C(GameUi.Gold, stage.Name) + "  " + C(GameUi.Dim, Story(x.rec)), 18f, 32f);
                }
            }
        }

        private static void AlbumRight()
        {
            var cats = Achievements.ShownCats.ToList();
            var cat = cats.Contains(_albumCat) ? _albumCat : cats[0];
            var all = Achievements.Shown.Where(x => x.Cat == cat).ToList();
            var head = GameUi.Block(_right, "head");
            Header(head, cat);
            var hrow = GameUi.Row(head, "row", 52f);
            Icon(hrow, Markers.Glyph(CatGlyph(cat), CatColor(cat)), 48f);
            RowText(hrow, C(CatColor(cat), all.Count(Achievements.Complete) + " of " + all.Count) + " complete   " + Gauge(all.Count(Achievements.Complete), all.Count, 12, CatColor(cat)), 22f);
            if (cat == "Trouble") Line(head, "These only keep the record: nothing is taken from you.", 17f, GameUi.Dim);
            if (cat == "Secrets") Line(head, "Hidden until they happen; each one leaves a hint.", 17f, GameUi.Dim);
            if (cat == "Extras") Line(head, "About other mods in your game; each shows only while its mod is installed.", 17f, GameUi.Dim);
            if (cat == "Stories") Line(head, "Each story heroine and professor has her own: one stage for every part of her story.", 17f, GameUi.Dim);
            for (var tier = 1; tier <= 3; tier++)
            {
                var t = tier;
                var defs = all.Where(x => x.Tier == t).ToList();
                if (defs.Count == 0) continue;
                var sec = GameUi.Block(_right, "tier", false);
                IconRow(sec, "star", t == 3 ? GameUi.Gold : t == 2 ? Pink : Blue,
                    C(GameUi.Gold, Achievements.Tiers[t]) + "  " + C(GameUi.Dim, defs.Count(Achievements.Complete) + "/" + defs.Count), 21f, 34f);
                foreach (var d in defs) AlbumCard(d);
            }
        }

        /// <summary>A gauge of filled squares in a color.</summary>
        private static string Gauge(int have, int max, int cells, Color c)
        {
            if (max <= 0) max = 1;
            var full = Mathf.Clamp(Mathf.RoundToInt((float)have * cells / max), 0, cells);
            if (have > 0 && full == 0) full = 1;
            return C(c, new string('■', full)) + "<color=#4a4f58>" + new string('■', cells - full) + "</color>";
        }

        /// <summary>
        /// One achievement: a badge in the category's color (grey until it falls, a lens while secret), its name, what it
        /// takes, a compact gauge; a climb shows the stage reached, the day and the girl, then the next stage.
        /// </summary>
        private static void AlbumCard(Achievements.Def d)
        {
            var n = Achievements.StageCount(d);
            var r = Achievements.Reached(d);
            var got = r > 0;
            var done = r >= n;
            var hidden = d.Secret && !got;
            var cur = Achievements.StageAt(d, Mathf.Max(1, r));
            var rec = Achievements.Record(d);
            var v = Achievements.Value(d);
            var color = CatColor(d.Cat);
            var card = GameUi.Block(_right, "ach");
            if (done) card.GetComponent<Image>().color = new Color(1f, 0.93f, 0.75f, 1f);
            var row = GameUi.Row(card, "row", 48f, 12f);
            row.GetComponent<LayoutElement>().preferredHeight = -1f;
            var hl = row.GetComponent<HorizontalLayoutGroup>();
            hl.childForceExpandHeight = false;
            hl.childAlignment = TextAnchor.UpperLeft;
            Icon(row, Markers.Glyph(hidden ? "lens" : CatGlyph(d.Cat), got ? color : Unlit), 50f);
            var col = GameUi.Block(row, "col", false, 2f);
            col.Size(-1f, -1f, 1f);
            var top = GameUi.Row(col, "top", 32f);
            RowText(top, C(got ? GameUi.Gold : GameUi.White, hidden ? "???" : cur.Name) + (n > 1 && !hidden && cur.Name != d.Name ? C(GameUi.Dim, "  " + d.Name) : ""), 23f);
            if (n > 1) RowText(top, "<align=right>" + Gauge(r, n, n, color) + "  " + C(got ? GameUi.Gold : GameUi.Dim, r + "/" + n) + "</align>", 19f);
            else if (got) RowText(top, "<align=right>" + C(GameUi.Green, Story(rec)) + "</align>", 18f);
            Line(col, hidden ? C(GameUi.Dim, "Hint: ") + d.Hint : cur.Goal, 18f, got ? GameUi.Dim : GameUi.White);
            if (!got && cur.At > 1)
                Line(col, Gauge(Mathf.Min(v, cur.At), cur.At, 12, color) + "  " + C(GameUi.Dim, Mathf.Min(v, cur.At) + "/" + cur.At), 17f);
            else if (got && n > 1 && !done)
            {
                var next = Achievements.StageAt(d, r + 1);
                Line(col, C(GameUi.Green, Story(rec)) + C(GameUi.Dim, "   Next: ") + next.Name + C(GameUi.Dim, ", " + Lower(next.Goal) + "  ") + Gauge(Mathf.Min(v, next.At), next.At, 10, color) + C(GameUi.Dim, "  " + Mathf.Min(v, next.At) + "/" + next.At), 17f);
            }
            else if (got && n > 1) Line(col, C(GameUi.Green, Story(rec)) + "   " + C(GameUi.Gold, "Every stage done."), 17f);
            if (got && d.Roster && done) Line(col, RosterNote(d), 17f, GameUi.Dim);
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
            return "With " + girls + " girl" + (girls == 1 ? "" : "s") + " on campus" + (all == game ? ", only the game's own characters." : " (" + (all - game) + " of your own characters).");
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
