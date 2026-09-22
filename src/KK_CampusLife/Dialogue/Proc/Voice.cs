using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// How one girl talks (procedural dialogue, section 3): seven axes from her personality, her quirks and her card's
    /// numbers, and a signature drawn once from her card (favorite exclamation, filler, laugh, how she calls you, a pet
    /// theme, "~" or not, how long she talks). Two girls of the same personality sound different; the same girl always
    /// sounds like herself.
    /// </summary>
    internal sealed class Voice
    {
        public float Warm, Energy, Formal, Tease, Shy, Blunt, Verbose;
        public string Exclaim = "Oh!", Filler = "Well,", Laugh = "Hehe.", CallYou = "you", Pet = "", Kind = "";
        public bool Tilde;
        public float FillerRate, StutterRate;
        public int Seed;

        private static readonly Dictionary<string, Voice> Cache = new Dictionary<string, Voice>();
        private static int _cacheDay = -1;

        public static Voice Of(SaveData.Heroine h)
        {
            if (h == null) return new Voice { Warm = 0.5f, Energy = 0.5f, Formal = 0.5f };
            var key = GirlInfo.Key(h);
            var day = Campus.S != null ? Campus.S.Day : 0;
            if (day != _cacheDay) { Cache.Clear(); _cacheDay = day; }   // how she calls you changes as you get close
            Voice v;
            if (Cache.TryGetValue(key, out v)) return v;
            v = Make(h, key);
            Cache[key] = v;
            return v;
        }

        // ---------------- axes ----------------

        private sealed class Row
        {
            public string[] Keys;
            public string Kind;
            public float? Warm, Energy, Formal, Tease, Shy, Blunt, Verbose;
        }

        private static readonly Row[] Table =
        {
            new Row { Keys = new[] { "モジモジ", "もじもじ", "内気", "ネガティブ", "不幸", "無口" }, Kind = "shy", Shy = 0.85f, Energy = 0.25f, Verbose = 0.3f },
            new Row { Keys = new[] { "ギャル", "ミーハー" }, Kind = "gyaru", Energy = 0.9f, Formal = 0.1f, Tease = 0.5f, Verbose = 0.7f },
            new Row { Keys = new[] { "お嬢", "高飛車" }, Kind = "ojou", Formal = 0.9f, Tease = 0.5f, Warm = 0.35f },
            new Row { Keys = new[] { "クール", "無感情" }, Kind = "cool", Warm = 0.2f, Verbose = 0.2f, Energy = 0.2f, Blunt = 0.6f },
            new Row { Keys = new[] { "元気", "天真爛漫", "ノリノリ", "気さく" }, Kind = "genki", Energy = 0.9f, Warm = 0.75f, Shy = 0.1f },
            new Row { Keys = new[] { "ヤンデレ" }, Kind = "yandere", Warm = 0.85f, Tease = 0.4f, Blunt = 0.55f },
            new Row { Keys = new[] { "意地っ張り", "勝ち気", "勝気", "ツンデレ" }, Kind = "tsundere", Tease = 0.6f, Blunt = 0.75f, Warm = 0.35f },
            new Row { Keys = new[] { "大和撫子", "正統派", "純真" }, Kind = "proper", Formal = 0.7f, Warm = 0.7f, Energy = 0.4f },
            new Row { Keys = new[] { "母性" }, Kind = "motherly", Warm = 0.9f, Formal = 0.5f, Verbose = 0.6f },
            new Row { Keys = new[] { "セクシー", "艶やか", "小悪魔", "ビッチ", "Ｓ", "S気" }, Kind = "vamp", Tease = 0.88f, Shy = 0.05f },
            new Row { Keys = new[] { "不良", "姉御", "ワイルド", "野性", "野生" }, Kind = "tough", Blunt = 0.88f, Formal = 0.1f, Tease = 0.5f },
            new Row { Keys = new[] { "文学", "ミステリ" }, Kind = "bookish", Verbose = 0.75f, Energy = 0.3f, Formal = 0.6f },
            new Row { Keys = new[] { "邪気眼" }, Kind = "chuuni", Verbose = 0.85f, Formal = 0.45f, Energy = 0.7f },
            new Row { Keys = new[] { "ものぐさ", "怠惰" }, Kind = "lazy", Energy = 0.15f, Verbose = 0.25f },
            new Row { Keys = new[] { "単純", "チョロ", "天然", "電波" }, Kind = "airhead", Energy = 0.7f, Blunt = 0.3f, Warm = 0.7f },
            new Row { Keys = new[] { "後輩" }, Kind = "kouhai", Formal = 0.55f, Warm = 0.75f, Energy = 0.65f },
            new Row { Keys = new[] { "高潔", "几帳面", "誠実", "意識高い" }, Kind = "serious", Formal = 0.75f, Blunt = 0.55f, Tease = 0.15f },
            new Row { Keys = new[] { "ボーイッシュ", "ボクっ娘" }, Kind = "boyish", Blunt = 0.65f, Energy = 0.7f, Formal = 0.2f },
            new Row { Keys = new[] { "ひねくれ" }, Kind = "twisted", Tease = 0.6f, Warm = 0.3f, Blunt = 0.6f },
            new Row { Keys = new[] { "方言", "帰国" }, Kind = "open", Warm = 0.7f, Energy = 0.65f, Blunt = 0.55f },
        };

        private static Voice Make(SaveData.Heroine h, string key)
        {
            var v = new Voice();
            switch (GirlInfo.ToneOf(h))
            {
                case Tone.Gentle: v.Set(0.75f, 0.4f, 0.5f, 0.2f, 0.5f, 0.3f, 0.5f); v.Kind = "gentle"; break;
                case Tone.Energetic: v.Set(0.7f, 0.85f, 0.25f, 0.35f, 0.15f, 0.5f, 0.6f); v.Kind = "energetic"; break;
                case Tone.Cool: v.Set(0.3f, 0.3f, 0.6f, 0.25f, 0.3f, 0.6f, 0.3f); v.Kind = "cool"; break;
                default: v.Set(0.5f, 0.6f, 0.3f, 0.85f, 0.1f, 0.6f, 0.5f); v.Kind = "teasing"; break;
            }
            var name = GirlInfo.PersonalityName(h) ?? "";
            var row = Table.FirstOrDefault(r => r.Keys.Any(k => name.Contains(k)));
            if (row != null)
            {
                v.Kind = row.Kind;
                if (row.Warm.HasValue) v.Warm = row.Warm.Value;
                if (row.Energy.HasValue) v.Energy = row.Energy.Value;
                if (row.Formal.HasValue) v.Formal = row.Formal.Value;
                if (row.Tease.HasValue) v.Tease = row.Tease.Value;
                if (row.Shy.HasValue) v.Shy = row.Shy.Value;
                if (row.Blunt.HasValue) v.Blunt = row.Blunt.Value;
                if (row.Verbose.HasValue) v.Verbose = row.Verbose.Value;
            }
            var a = GirlInfo.Quirks(h);
            if (a.hitori) { v.Verbose -= 0.15f; v.Shy += 0.1f; }
            if (a.kappatu) v.Energy += 0.2f;
            if (a.majime) v.Formal += 0.15f;
            if (a.taida) v.Energy -= 0.2f;
            if (a.bitch) { v.Tease += 0.15f; v.Shy -= 0.3f; }
            if (a.ukemi) { v.Shy += 0.15f; v.Blunt -= 0.1f; }
            if (a.friendly) v.Warm += 0.15f;
            if (a.donkan) v.Blunt += 0.1f;
            if (a.choroi) v.Warm += 0.1f;
            try
            {
                var p = h.parameter;
                v.Warm += (p.kindness - 1) * 0.08f;
                v.Blunt += (p.aggressive - 1) * 0.1f;
                v.Formal += (p.diligence - 1) * 0.08f;
            }
            catch { }
            v.Clamp();

            // ---- signature, fixed by her card ----
            v.Seed = Hash(key);
            var rng = new System.Random(v.Seed);
            v.Exclaim = Pick(rng, Exclaims(v));
            v.Filler = Pick(rng, Fillers(v));
            v.Laugh = Pick(rng, Laughs(v));
            v.Tilde = rng.NextDouble() < 0.15 + 0.4 * v.Tease * (1 - v.Formal) + (v.Kind == "gyaru" ? 0.2 : 0);
            v.FillerRate = 0.12f + 0.25f * (float)rng.NextDouble() * (0.5f + v.Shy);
            v.StutterRate = v.Shy >= 0.65f ? 0.2f + 0.25f * v.Shy : 0f;
            v.CallYou = CallFor(h, v, rng);
            v.Pet = PetTheme(h, a, rng);
            return v;
        }

        private void Set(float warm, float energy, float formal, float tease, float shy, float blunt, float verbose)
        {
            Warm = warm; Energy = energy; Formal = formal; Tease = tease; Shy = shy; Blunt = blunt; Verbose = verbose;
        }

        private void Clamp()
        {
            Warm = Mathf.Clamp01(Warm); Energy = Mathf.Clamp01(Energy); Formal = Mathf.Clamp01(Formal); Tease = Mathf.Clamp01(Tease);
            Shy = Mathf.Clamp01(Shy); Blunt = Mathf.Clamp01(Blunt); Verbose = Mathf.Clamp01(Verbose);
        }

        public static int Hash(string s)
        {
            unchecked
            {
                var x = (int)2166136261;
                foreach (var c in s ?? "") x = (x ^ c) * 16777619;
                return x & 0x7fffffff;
            }
        }

        private static string Pick(System.Random r, List<string> l) { return l.Count == 0 ? "" : l[r.Next(l.Count)]; }

        private static List<string> Exclaims(Voice v)
        {
            var l = new List<string>();
            if (v.Energy >= 0.65f) l.AddRange(new[] { "Whoa!", "No way!", "Wow!", "Ooh!", "Seriously?!", "Oh wow!", "Woah, woah!", "Yesss!" });
            if (v.Shy >= 0.6f || v.Warm >= 0.7f && v.Energy < 0.5f) l.AddRange(new[] { "Oh!", "Ah!", "Oh my...", "Eh?", "Goodness...", "Oh dear." });
            if (v.Warm < 0.4f && v.Energy < 0.45f) l.AddRange(new[] { "Hm.", "Huh.", "Oh.", "...Hm?", "Tch." });
            if (v.Tease >= 0.6f) l.AddRange(new[] { "Oho?", "Ohh?", "Well, well.", "My, my.", "Ara~" });
            if (v.Formal >= 0.7f) l.AddRange(new[] { "Oh my.", "Goodness.", "My word." });
            if (v.Blunt >= 0.7f) l.AddRange(new[] { "Geez!", "Ugh.", "Hah!", "Huh?!", "Whoa, hey." });
            if (l.Count == 0) l.AddRange(new[] { "Oh!", "Huh!", "Wow." });
            return l;
        }

        private static List<string> Fillers(Voice v)
        {
            var l = new List<string>();
            if (v.Shy >= 0.6f) l.AddRange(new[] { "Um,", "U-um,", "Er,", "Ah, um," });
            if (v.Energy >= 0.65f && v.Formal < 0.5f) l.AddRange(new[] { "Like,", "So,", "Okay so,", "Ya know," });
            if (v.Warm < 0.4f) l.AddRange(new[] { "Well.", "Hmm.", "Anyway," });
            if (v.Tease >= 0.6f) l.AddRange(new[] { "Hmm~", "Well~", "Say,", "So~" });
            if (v.Formal >= 0.65f) l.AddRange(new[] { "Well,", "I suppose,", "You see," });
            if (v.Blunt >= 0.7f) l.AddRange(new[] { "Look,", "Listen,", "Honestly,", "Okay," });
            if (l.Count == 0) l.AddRange(new[] { "Well,", "Hmm,", "So," });
            return l;
        }

        private static List<string> Laughs(Voice v)
        {
            var l = new List<string>();
            if (v.Shy >= 0.6f) l.AddRange(new[] { "Ehehe...", "Hehe...", "Fufu..." });
            if (v.Energy >= 0.65f) l.AddRange(new[] { "Ahaha!", "Hehe!", "Nyahaha!", "Ehehe!" });
            if (v.Warm < 0.4f) l.AddRange(new[] { "Heh.", "Hm hm.", "Pfft." });
            if (v.Tease >= 0.6f) l.AddRange(new[] { "Fufufu~", "Heh heh~", "Kukuku.", "Teehee~" });
            if (v.Formal >= 0.7f) l.AddRange(new[] { "Ohoho.", "Fufu.", "Hohoho~" });
            if (l.Count == 0) l.AddRange(new[] { "Haha.", "Hehe." });
            return l;
        }

        private static string PlayerName()
        {
            try { var p = KKAPI.MainGame.EventUtils.GetPlayerData(); return p != null && !string.IsNullOrEmpty(p.firstname) ? p.firstname : ""; }
            catch { return ""; }
        }

        /// <summary>How she calls you: by name, with -san or -kun, senpai, or a pet word once you are close.</summary>
        private static string CallFor(SaveData.Heroine h, Voice v, System.Random r)
        {
            var p = PlayerName();
            if (string.IsNullOrEmpty(p)) return "you";
            if (h.relation < 0) return v.Formal >= 0.6f ? p + "-san" : "you";
            var close = h.isGirlfriend || h.relation >= 2;
            var l = new List<string>();
            if (v.Formal >= 0.65f) l.Add(p + "-san");
            if (v.Kind == "kouhai") { l.Add("senpai"); l.Add(p + "-senpai"); }
            if (v.Shy >= 0.6f || v.Warm >= 0.7f) l.Add(p + "-kun");
            if (v.Blunt >= 0.6f || v.Energy >= 0.65f) l.Add(p);
            if (close && v.Tease >= 0.6f) l.Add("dummy");
            if (close && v.Warm >= 0.7f) l.Add(p + "-kun");
            if (close) l.Add(p);
            if (l.Count == 0) l.Add(p);
            return l[r.Next(l.Count)];
        }

        private static string PetTheme(SaveData.Heroine h, ChaFileParameter.Attribute a, System.Random r)
        {
            var l = new List<string>();
            if (a.harapeko) l.Add("food");
            if (a.dokusyo) l.Add("books");
            if (a.ongaku) l.Add("music");
            if (a.undo) l.Add("sports");
            if (a.kireizuki) l.Add("cleaning");
            if (a.taida) l.Add("naps");
            if (a.majime) l.Add("studying");
            foreach (var like in GirlInfo.LikesOf(h))
                switch (like)
                {
                    case "animals": l.Add("cats"); break;
                    case "sweets": l.Add("sweets"); break;
                    case "fashion": l.Add("clothes"); break;
                    case "cooking": l.Add("cooking"); break;
                    case "coffee": l.Add("coffee"); break;
                    case "exercise": l.Add("running"); break;
                }
            return l.Count == 0 ? "" : l[r.Next(l.Count)];
        }

        // ---------------- speaking ----------------

        /// <summary>
        /// A line put in her voice: a stutter when shy, "!" when lively, flat when cold, her filler at the start now and
        /// then, "~" if that is her, her laugh after a light line. Leaves rich text and short lines alone.
        /// </summary>
        public static string Say(SaveData.Heroine h, string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 8 || text.IndexOf('<') >= 0 || h == null) return text;
            try
            {
                var v = Of(h);
                var t = text.Trim();
                var end = t[t.Length - 1];
                if (v.Energy >= 0.72f && end == '.' && !t.EndsWith("...", StringComparison.Ordinal) && Random.value < 0.45f) t = t.Substring(0, t.Length - 1) + "!";
                else if (v.Energy < 0.3f && v.Warm < 0.45f && end == '!' && !t.EndsWith("?!", StringComparison.Ordinal) && Random.value < 0.7f) t = t.Substring(0, t.Length - 1) + ".";
                if (v.FillerRate > 0f && Random.value < v.FillerRate && char.IsLetter(t[0]) && !StartsWithFiller(t))
                    t = v.Filler + " " + LowerFirst(t);
                else if (v.StutterRate > 0f && Random.value < v.StutterRate && char.IsLetter(t[0]))
                {
                    var w = t.Split(' ')[0];
                    if (w.Length >= 2 && char.IsLetter(w[1])) t = t.Substring(0, 1) + "-" + t;
                }
                end = t[t.Length - 1];
                if (v.Tilde && (end == '.' || end == '!') && !t.EndsWith("...", StringComparison.Ordinal) && Random.value < 0.3f) t = t.Substring(0, t.Length - 1) + "~";
                return t;
            }
            catch { return text; }
        }

        private static bool StartsWithFiller(string t)
        {
            foreach (var f in new[] { "Um", "U-um", "Er", "Ah", "Like", "So", "Okay", "Ya know", "Well", "Hmm", "Anyway", "Say", "Look", "Listen", "Honestly", "I suppose", "You see", "Oh", "...", "Eh", "Huh", "Heh" })
                if (t.StartsWith(f, StringComparison.Ordinal)) return true;
            return false;
        }

        private static string LowerFirst(string t)
        {
            if (t.Length < 2) return t;
            // "I", "I'm" and names keep their capital.
            if (t[0] == 'I' && (t[1] == ' ' || t[1] == '\'')) return t;
            var w = t.Split(' ')[0].TrimEnd(',', '.', '!', '?');
            if (GirlNames.Contains(w)) return t;
            return char.ToLowerInvariant(t[0]) + t.Substring(1);
        }

        private static HashSet<string> _names;
        private static int _namesDay = -2;
        private static HashSet<string> GirlNames
        {
            get
            {
                var day = Campus.S != null ? Campus.S.Day : -1;
                if (_names == null || _namesDay != day)
                {
                    _namesDay = day;
                    _names = new HashSet<string>();
                    try { foreach (var g in G.Heroines) if (g != null) _names.Add(GirlInfo.FirstName(g)); } catch { }
                    var p = PlayerName();
                    if (!string.IsNullOrEmpty(p)) _names.Add(p);
                }
                return _names;
            }
        }

        public string Describe()
        {
            return Kind + ": warm " + Warm.ToString("0.00") + ", energy " + Energy.ToString("0.00") + ", formal " + Formal.ToString("0.00") + ", tease " + Tease.ToString("0.00")
                   + ", shy " + Shy.ToString("0.00") + ", blunt " + Blunt.ToString("0.00") + ", verbose " + Verbose.ToString("0.00")
                   + " | exclaim '" + Exclaim + "', filler '" + Filler + "' (" + FillerRate.ToString("0.00") + "), laugh '" + Laugh + "', calls you '" + CallYou + "'"
                   + (Tilde ? ", ~" : "") + (StutterRate > 0 ? ", stutter " + StutterRate.ToString("0.00") : "") + (Pet != "" ? ", pet theme " + Pet : "");
        }
    }
}
