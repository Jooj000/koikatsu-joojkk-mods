using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>One condition of a rule: key=value(|value), key!=value, key&gt;=n, key&lt;=n.</summary>
    internal sealed class DlgCond
    {
        public string Key, Op;
        public string[] Values;
        public float Num;

        public bool Holds(Dictionary<string, string> ctx)
        {
            string v;
            var has = ctx.TryGetValue(Key, out v) && !string.IsNullOrEmpty(v);
            switch (Op)
            {
                case "=": return has && Values.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
                case "!=": return !has || !Values.Any(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));
                default:
                    float n;
                    if (!has || !float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out n)) return false;
                    return Op == ">=" ? n >= Num : n <= Num;
            }
        }

        public override string ToString() { return Key + Op + (Op == "=" || Op == "!=" ? string.Join("|", Values) : Num.ToString(CultureInfo.InvariantCulture)); }
    }

    /// <summary>A rule of the bank: when it fits (conditions), what she may say, what the line leaves open for your reply.</summary>
    internal sealed class DlgRule
    {
        public string Id = "", On = "", File = "";
        public readonly List<DlgCond> When = new List<DlgCond>();
        public string[] Opens = new string[0];
        public readonly Dictionary<string, string> Fit = new Dictionary<string, string>();
        public string Feel = "";
        public readonly List<string> Say = new List<string>();
        public int Score { get { return When.Count; } }
    }

    /// <summary>
    /// The lines of the procedural dialogue (sections 4 and 6): rules from the .dlg files (built into the DLL, and the
    /// ones in BepInEx\plugins\Joojkk\dialogue replace a file of the same name). The most specific rule that fits plays,
    /// never the same line with the same girl for a few days nor the line another girl just said; a small grammar
    /// fills the slots and picks among alternatives.
    ///
    /// Format: "@rule id" then "on situation", "when k=v k!=v k>=n", "opens move,move", "fit move=good|neutral|bad",
    /// "feel smile", and lines starting "- ". "@list name" then "- item" lines. Inside lines: [a|b|c] picks one,
    /// {slot} fills from the context ({Slot} capitalized), {#list} picks from a list; a line whose slot is missing is
    /// skipped (a line never cites what she does not know).
    /// </summary>
    internal static class Bank
    {
        private static List<DlgRule> _rules;
        private static Dictionary<string, List<DlgRule>> _byOn;
        private static Dictionary<string, List<string>> _lists;
        public static string LoadedFrom = "";

        public static int Count { get { Ensure(); return _rules.Count; } }

        private static void Ensure() { if (_rules == null) Load(); }

        public static void Load()
        {
            _rules = new List<DlgRule>();
            _lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                foreach (var res in asm.GetManifestResourceNames().Where(n => n.EndsWith(".dlg", StringComparison.OrdinalIgnoreCase)))
                    using (var st = asm.GetManifestResourceStream(res))
                    using (var rd = new StreamReader(st, Encoding.UTF8))
                        files[Path.GetFileNameWithoutExtension(res.Substring(res.LastIndexOf("dialogue.", StringComparison.Ordinal) >= 0 ? res.LastIndexOf("dialogue.", StringComparison.Ordinal) + 9 : 0))] = rd.ReadToEnd();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Dialogue bank (built in): " + e.Message); }
            var over = 0;
            try
            {
                var dir = Path.Combine(Path.Combine(BepInEx.Paths.PluginPath, "Joojkk"), "dialogue");
                if (Directory.Exists(dir))
                    foreach (var f in Directory.GetFiles(dir, "*.dlg")) { files[Path.GetFileNameWithoutExtension(f)] = File.ReadAllText(f, Encoding.UTF8); over++; }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Dialogue bank (folder): " + e.Message); }
            foreach (var kv in files)
                try { Parse(kv.Value, kv.Key); }
                catch (Exception e) { CampusLifePlugin.Log.LogWarning("Dialogue bank " + kv.Key + ": " + e.Message); }
            _byOn = _rules.GroupBy(r => r.On).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            LoadedFrom = files.Count + " files (" + over + " from the folder)";
            CampusLifePlugin.Log.LogInfo("Dialogue bank: " + _rules.Count + " rules, " + _rules.Sum(r => r.Say.Count) + " lines, " + _lists.Count + " lists, " + LoadedFrom);
        }

        private static void Parse(string text, string file)
        {
            DlgRule rule = null;
            List<string> list = null;
            var n = 0;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                n++;
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.StartsWith("@rule ", StringComparison.Ordinal))
                {
                    rule = new DlgRule { Id = line.Substring(6).Trim(), File = file };
                    _rules.Add(rule);
                    list = null;
                    continue;
                }
                if (line.StartsWith("@list ", StringComparison.Ordinal))
                {
                    var name = line.Substring(6).Trim();
                    if (!_lists.TryGetValue(name, out list)) _lists[name] = list = new List<string>();
                    rule = null;
                    continue;
                }
                if (line.StartsWith("- ", StringComparison.Ordinal) || line == "-")
                {
                    var body = line.Length > 2 ? line.Substring(2).Trim() : "";
                    if (rule != null) rule.Say.Add(body);
                    else if (list != null) list.Add(body);
                    continue;
                }
                if (rule == null) { CampusLifePlugin.Log.LogDebug("Dialogue bank " + file + ":" + n + ": line outside a rule"); continue; }
                var sp = line.IndexOf(' ');
                var head = sp < 0 ? line : line.Substring(0, sp);
                var rest = sp < 0 ? "" : line.Substring(sp + 1).Trim();
                switch (head)
                {
                    case "on": rule.On = rest; break;
                    case "opens": rule.Opens = rest.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries); break;
                    case "feel": rule.Feel = rest; break;
                    case "when":
                        foreach (var c in rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) { var d = Cond(c); if (d != null) rule.When.Add(d); }
                        break;
                    case "fit":
                        foreach (var c in rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var eq = c.IndexOf('=');
                            if (eq > 0) rule.Fit[c.Substring(0, eq)] = c.Substring(eq + 1);
                        }
                        break;
                    default: CampusLifePlugin.Log.LogDebug("Dialogue bank " + file + ":" + n + ": unknown '" + head + "'"); break;
                }
            }
        }

        private static DlgCond Cond(string c)
        {
            foreach (var op in new[] { "!=", ">=", "<=", "=" })
            {
                var i = c.IndexOf(op, StringComparison.Ordinal);
                if (i <= 0) continue;
                var key = c.Substring(0, i);
                var val = c.Substring(i + op.Length);
                var d = new DlgCond { Key = key, Op = op, Values = val.Split('|') };
                if (op == ">=" || op == "<=") float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out d.Num);
                return d;
            }
            return null;
        }

        // ================= choosing =================

        public static IEnumerable<DlgRule> Matching(string on, Dictionary<string, string> ctx)
        {
            Ensure();
            List<DlgRule> l;
            if (!_byOn.TryGetValue(on, out l)) return Enumerable.Empty<DlgRule>();
            return l.Where(r => r.When.All(c => c.Holds(ctx)));
        }

        private static string Stamp { get { return Campus.S != null ? (Campus.S.Day * 16 + (int)G.CurrentMask).ToString(CultureInfo.InvariantCulture) : "0"; } }

        /// <summary>Said by her in the last days, or by anyone this period.</summary>
        private static bool Said(string lineId, string girl)
        {
            var S = Campus.S;
            if (S == null) return false;
            var stamp = Stamp;
            foreach (var x in S.LinesSaid)
            {
                if (!x.StartsWith(lineId + "|", StringComparison.Ordinal)) continue;
                var p = x.Split('|');
                if (p.Length >= 4 && (p[1] == girl || p[3] == stamp)) return true;
            }
            return false;
        }

        private static void MarkSaid(string lineId, string girl)
        {
            var S = Campus.S;
            if (S == null) return;
            S.LinesSaid.Add(lineId + "|" + girl + "|" + S.Day + "|" + Stamp);
            if (S.LinesSaid.Count > 1500) S.LinesSaid.RemoveRange(0, S.LinesSaid.Count - 1500);
        }

        /// <summary>
        /// The line for a situation: among the rules that fit, the most specific ones (one step of slack so the bank
        /// varies), not said lately; the first line whose slots are all known. Null when nothing fits.
        /// </summary>
        public static string Pick(string on, Dictionary<string, string> ctx, string girl, out DlgRule used)
        {
            used = null;
            var all = Matching(on, ctx).ToList();
            if (all.Count == 0) return null;
            var top = all.Max(r => r.Score);
            var pool = all.Where(r => r.Score >= top - 1).ToList();
            // Lines not said yet first; when every line was said lately, the bank repeats rather than stay quiet.
            for (var pass = 0; pass < 2; pass++)
            {
                var cands = new List<KeyValuePair<DlgRule, int>>();
                foreach (var r in pool)
                    for (var i = 0; i < r.Say.Count; i++)
                        if (pass == 1 || !Said(r.Id + "#" + i, girl)) cands.Add(new KeyValuePair<DlgRule, int>(r, i));
                while (cands.Count > 0)
                {
                    var total = cands.Sum(c => Weight(c.Key, top));
                    var roll = Random.value * total;
                    var pick = cands[cands.Count - 1];
                    foreach (var c in cands) { roll -= Weight(c.Key, top); if (roll <= 0f) { pick = c; break; } }
                    cands.Remove(pick);
                    var text = Expand(pick.Key.Say[pick.Value], ctx, 0);
                    if (text == null) continue;
                    MarkSaid(pick.Key.Id + "#" + pick.Value, girl);
                    used = pick.Key;
                    return text;
                }
            }
            return null;
        }

        private static float Weight(DlgRule r, int top) { return r.Score >= top ? 3f : 1f; }

        /// <summary>A label for a choice (move.ask, move.tease...): the most specific rule, any of its lines, not marked as said.</summary>
        public static string Label(string on, Dictionary<string, string> ctx)
        {
            var all = Matching(on, ctx).ToList();
            if (all.Count == 0) return null;
            var top = all.Max(r => r.Score);
            foreach (var r in all.Where(r => r.Score >= top).OrderBy(_ => Random.value))
                foreach (var line in r.Say.OrderBy(_ => Random.value))
                {
                    var text = Expand(line, ctx, 0);
                    if (text != null) return text;
                }
            return null;
        }

        // ================= grammar =================

        /// <summary>Fills a template: [a|b] alternatives (nested), {slot}, {Slot}, {#list}. Null if a slot is unknown.</summary>
        public static string Expand(string t, Dictionary<string, string> ctx, int depth)
        {
            if (t == null || depth > 6) return t;
            var sb = new StringBuilder();
            var i = 0;
            while (i < t.Length)
            {
                var c = t[i];
                if (c == '[')
                {
                    var close = Match(t, i, '[', ']');
                    if (close < 0) { sb.Append(c); i++; continue; }
                    var opts = SplitTop(t.Substring(i + 1, close - i - 1));
                    var o = Expand(opts[Random.Range(0, opts.Count)], ctx, depth + 1);
                    if (o == null) return null;
                    sb.Append(o);
                    i = close + 1;
                    continue;
                }
                if (c == '{')
                {
                    var close = t.IndexOf('}', i);
                    if (close < 0) { sb.Append(c); i++; continue; }
                    var slot = t.Substring(i + 1, close - i - 1);
                    string val;
                    if (slot.StartsWith("#", StringComparison.Ordinal))
                    {
                        List<string> l;
                        if (!_lists.TryGetValue(slot.Substring(1), out l) || l.Count == 0) return null;
                        val = Expand(l[Random.Range(0, l.Count)], ctx, depth + 1);
                        if (val == null) return null;
                    }
                    else
                    {
                        var cap = slot.Length > 0 && char.IsUpper(slot[0]);
                        var key = cap ? char.ToLowerInvariant(slot[0]) + slot.Substring(1) : slot;
                        if (!ctx.TryGetValue(key, out val) || val == null) return null;
                        if (cap && val.Length > 0) val = char.ToUpperInvariant(val[0]) + val.Substring(1);
                    }
                    sb.Append(val);
                    i = close + 1;
                    continue;
                }
                sb.Append(c);
                i++;
            }
            return depth == 0 ? Tidy(sb.ToString()) : sb.ToString();
        }

        private static int Match(string t, int open, char a, char b)
        {
            var n = 0;
            for (var i = open; i < t.Length; i++)
            {
                if (t[i] == a) n++;
                else if (t[i] == b && --n == 0) return i;
            }
            return -1;
        }

        private static List<string> SplitTop(string s)
        {
            var l = new List<string>();
            var n = 0;
            var start = 0;
            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] == '[') n++;
                else if (s[i] == ']') n--;
                else if (s[i] == '|' && n == 0) { l.Add(s.Substring(start, i - start)); start = i + 1; }
            }
            l.Add(s.Substring(start));
            return l;
        }

        private static string Tidy(string s)
        {
            s = s.Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            s = s.Replace(" ,", ",").Replace(" .", ".").Replace(" !", "!").Replace(" ?", "?").Replace(",,", ",").Replace("..,", "...");
            if (s.Length > 0 && char.IsLower(s[0])) s = char.ToUpperInvariant(s[0]) + s.Substring(1);
            return s;
        }


        /// <summary>How many distinct lines one situation gives over many tries (requirement 1: no NPC-like repeats).</summary>
        public static string Variety(string on, Dictionary<string, string> ctx, int tries)
        {
            var seen = new Dictionary<string, int>();
            for (var i = 0; i < tries; i++)
            {
                var rules = Matching(on, ctx).ToList();
                if (rules.Count == 0) break;
                var r = rules[Random.Range(0, rules.Count)];
                var line = r.Say.Count == 0 ? null : Expand(r.Say[Random.Range(0, r.Say.Count)], ctx, 0);
                if (line == null) continue;
                int n;
                seen[line] = seen.TryGetValue(line, out n) ? n + 1 : 1;
            }
            return on + ": " + seen.Count + " distinct of " + tries + " (" + Matching(on, ctx).Count() + " rules fit)";
        }
    }
}
