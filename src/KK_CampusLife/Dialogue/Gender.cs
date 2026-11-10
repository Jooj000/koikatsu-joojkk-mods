using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Boys in the class. A card saved as male is a boy (only the card's own sex counts): lines
    /// about him say he/him/his and boyfriend, and his own lines call himself a boyfriend or a guy. Tone and voice stay
    /// the same. See <see cref="Boys"/> for the rest.
    /// </summary>
    internal static class Gender
    {
        private static HashSet<string> _boys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static List<string> _girls = new List<string>();
        private static string _cfg;

        internal static string Forced = "";
        private static int _day = -2, _count = -1;

        public static bool IsBoy(SaveData.Heroine h)
        {
            if (h == null) return false;
            Refresh();
            return _boys.Count > 0 && _boys.Contains(GirlInfo.FirstName(h));
        }

        public static bool AnyBoys { get { Refresh(); return _boys.Count > 0; } }

        /// <summary>Rebuilt once a day, or when the class changes size.</summary>
        private static void Refresh()
        {
            try
            {
                var cfg = Forced ?? "";
                var day = Campus.S != null ? Campus.S.Day : -1;
                var count = G.Heroines.Count;
                if (cfg == _cfg && day == _day && count == _count) return;
                _cfg = cfg; _day = day; _count = count;
                var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var n in cfg.Split(',', ';'))
                    if (n.Trim().Length > 0) listed.Add(n.Trim());
                var boys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var girls = new List<string>();
                foreach (var h in G.Heroines)
                {
                    if (h == null) continue;
                    var first = GirlInfo.FirstName(h);
                    var male = h.charFile != null && h.charFile.parameter != null && h.charFile.parameter.sex == 0;
                    if (male || listed.Contains(first) || listed.Contains(GirlInfo.FullName(h))) boys.Add(first);
                    else girls.Add(first);
                }
                _boys = boys;
                _girls = girls;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Gender refresh: " + e.Message); }
        }

        /// <summary>
        /// The line as shown. <paramref name="speaker"/> says it (null: narration, the player or a label);
        /// <paramref name="scene"/> is the girl the scene is about, who "she" means when the line names nobody.
        /// </summary>
        public static string Fix(string text, SaveData.Heroine speaker, SaveData.Heroine scene)
        {
            if (string.IsNullOrEmpty(text) || !AnyBoys) return text;
            try
            {
                var t = text;
                var speakerName = speaker != null ? GirlInfo.FirstName(speaker) : null;
                if (speaker != null && _boys.Contains(speakerName)) t = Self(t);
                bool boyNamed = false, girlNamed = false;
                foreach (var b in _boys) if (b != speakerName && Names(t, b)) { boyNamed = true; break; }
                foreach (var g in _girls) if (g != speakerName && Names(t, g)) { girlNamed = true; break; }
                var about = boyNamed && !girlNamed
                            || !boyNamed && !girlNamed && scene != null && scene != speaker && IsBoy(scene);
                return about ? ThirdPerson(t) : t;
            }
            catch { return text; }
        }

        private static bool Names(string t, string name)
        {
            if (string.IsNullOrEmpty(name) || name == "?") return false;
            var i = t.IndexOf(name, StringComparison.Ordinal);
            while (i >= 0)
            {
                var before = i == 0 || !char.IsLetter(t[i - 1]);
                var j = i + name.Length;
                if (before && (j >= t.Length || !char.IsLetter(t[j]))) return true;
                i = t.IndexOf(name, j, StringComparison.Ordinal);
            }
            return false;
        }

        // ---- his own lines: he is the boyfriend now ----

        private static readonly Regex SelfRx = new Regex(
            @"\b(your|his|the|I'm a|a) girlfriend\b|\bGirlfriend (here|mode)\b|\b(luckiest|happiest|only|lucky|a|good|cute) girl\b|\bgirl like me\b|\bas a girl\b");

        private static string Self(string t)
        {
            return SelfRx.Replace(t, m => m.Value.Replace("girlfriend", "boyfriend").Replace("Girlfriend", "Boyfriend").Replace("girl", "guy"));
        }

        // ---- lines about him ----

        private static readonly Regex SheRx = new Regex(@"\b([Ss])he\b");
        private static readonly Regex HerselfRx = new Regex(@"\b([Hh])erself\b");
        private static readonly Regex HersRx = new Regex(@"\b([Hh])ers\b");
        private static readonly Regex HerRx = new Regex(@"\b([Hh])er\b(\s*)([A-Za-z]*)");
        private static readonly Regex GirlRx = new Regex(@"\b(lucky|good|that|this|the|a|poor|sweet|silly|your|my|nice|cute|new|other) girl\b", RegexOptions.IgnoreCase);
        private static readonly Regex GirlfriendRx = new Regex(@"\b([Gg])irlfriend");

        /// <summary>After "her" these words mean "him" ("tell her about it", "make her laugh"); any other word, "his".</summary>
        private static readonly HashSet<string> ObjectNext = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "", "to", "a", "an", "about", "and", "you", "talk", "in", "she", "he", "on", "the", "up", "too", "now", "it", "for",
            "your", "my", "our", "their", "there", "here", "today", "tonight", "tomorrow", "after", "before", "that", "this", "some",
            "or", "but", "so", "home", "as", "what", "something", "anything", "nothing", "one", "at", "out", "like", "is", "was", "by",
            "awake", "angry", "again", "yourself", "myself", "without", "within", "while", "which", "why", "where", "how", "when",
            "who", "if", "shy", "guilty", "from", "into", "directly", "close", "closer", "already", "yet", "warmly", "softly", "wait",
            "walk", "through", "soon", "somewhere", "someday", "smile", "laugh", "cry", "blush", "sleepy", "show", "separately",
            "see", "say", "save", "sad", "happy", "mad", "upset", "lonely", "publicly", "pass", "over", "once", "off", "of",
            "normally", "never", "make", "let", "leave", "later", "lately", "honestly", "hard", "go", "come", "give", "everyone",
            "every", "either", "each", "during", "down", "do", "cold", "carry", "be", "behind", "asking", "around", "along", "alone",
            "with", "than", "then", "just", "very", "much", "more", "less", "all", "first", "anymore", "away", "well", "enough",
            "feel", "know", "think", "worry", "win", "back", "right", "still", "even", "really", "instead", "until", "since", "near"
        };

        private static string ThirdPerson(string t)
        {
            t = HerselfRx.Replace(t, m => m.Groups[1].Value == "H" ? "Himself" : "himself");
            t = HersRx.Replace(t, m => m.Groups[1].Value == "H" ? "His" : "his");
            t = HerRx.Replace(t, m =>
            {
                var cap = m.Groups[1].Value == "H";
                var next = m.Groups[3].Value;
                // "her." / "her," / "her!" and a word that takes an object after it
                var obj = next.Length == 0 || ObjectNext.Contains(next);
                var w = obj ? (cap ? "Him" : "him") : (cap ? "His" : "his");
                return w + m.Groups[2].Value + next;
            });
            t = SheRx.Replace(t, m => m.Groups[1].Value == "S" ? "He" : "he");
            t = GirlfriendRx.Replace(t, m => m.Groups[1].Value == "G" ? "Boyfriend" : "boyfriend");
            t = GirlRx.Replace(t, m => m.Groups[1].Value + " guy");
            return t;
        }
    }
}
