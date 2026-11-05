using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The card's markers (ChaFileParameter.Attribute: easy, lewd, closet pervert, submissive, friendly, serious...) when
    /// easy and lewd at once). With every marker yes, or every one no, the markers are ignored and drawn instead, once per
    /// girl and always the same for her (seeded by her key), weighted by her personality (its tone and how open or shy it
    /// is), her likes (the card's likes are kept as they are) and her card's refusals. A card with some yes and some no
    /// is used as it is. Only the mod's own characters: the game's story heroines keep theirs.
    ///
    /// Measured: the pace itself mostly came out Normal, since the same cards also refuse
    /// kisses (6 open against 6 shy), and Open for the sexy and gyaru ones. What made them easy was everything else that
    /// reads bitch/choroi/mutturi: the wish for H (+55), the flirty mood, the sleeping tease, cheating on a partner,
    /// sharing, slipping away with a girlfriend, the voice, and the game's own desire table.
    ///
    /// Our director reads them through GirlInfo.Quirks; the game's own AI reads them through ChaInfo.GetAttribute, which a
    /// postfix gives the same drawn markers. The card itself is never changed.
    /// </summary>
    internal static class CardTraits
    {
        public static readonly string[] Names = { "hinnyo", "harapeko", "donkan", "choroi", "bitch", "mutturi", "dokusyo", "ongaku", "kappatu", "ukemi", "friendly", "kireizuki", "taida", "sinsyutu", "hitori", "undo", "majime", "likeGirls" };

        private static readonly Dictionary<string, ChaFileParameter.Attribute> Drawn = new Dictionary<string, ChaFileParameter.Attribute>();
        private static readonly HashSet<string> KissShy = new HashSet<string>();
        private static readonly Dictionary<ChaFileControl, SaveData.Heroine> ByFile = new Dictionary<ChaFileControl, SaveData.Heroine>();

        internal static bool Raw;

        public static void Install(Harmony harmony)
        {
            try
            {
                var m = AccessTools.Method(typeof(ChaInfo), "GetAttribute", new[] { typeof(ChaFileControl) });
                if (m != null) harmony.Patch(m, postfix: new HarmonyMethod(typeof(CardTraits), nameof(GameReads)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Card traits: " + e.Message); }
        }

        public static bool[] Flags(ChaFileParameter.Attribute a)
        {
            return new[] { a.hinnyo, a.harapeko, a.donkan, a.choroi, a.bitch, a.mutturi, a.dokusyo, a.ongaku, a.kappatu, a.ukemi, a.friendly, a.kireizuki, a.taida, a.sinsyutu, a.hitori, a.undo, a.majime, a.likeGirls };
        }

        /// <summary>Every marker yes, or every one no: never really set.</summary>
        public static bool Unset(ChaFileParameter.Attribute a)
        {
            if (a == null) return false;
            var f = Flags(a);
            return f.All(x => x) || f.All(x => !x);
        }

        /// <summary>The markers to use for her: the card's, or the drawn ones.</summary>
        public static ChaFileParameter.Attribute Of(SaveData.Heroine h)
        {
            if (h == null || h.parameter == null) return new ChaFileParameter.Attribute();
            var a = h.parameter.attribute;
            if (Raw || h.fixCharaID != 0 || !Unset(a) || Cfg.CardTraits == null || !Cfg.CardTraits.Value) return a;
            var key = GirlInfo.Key(h);
            ChaFileParameter.Attribute d;
            if (Drawn.TryGetValue(key, out d)) return d;
            try { d = Draw(h); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Card traits " + key + ": " + e.Message); return a; }
            Drawn[key] = d;
            CampusLifePlugin.Log.LogInfo("Card traits: " + GirlInfo.FirstName(h) + " has every marker " + (a.choroi ? "yes" : "no") + "; drawn: " + Describe(d));
            return d;
        }

        /// <summary>
        /// massage, condom), which is as unset as the markers; then only a reserved few refuse kisses (drawn with her markers).
        /// </summary>
        public static bool RefusesKiss(SaveData.Heroine h)
        {
            if (h == null || h.parameter == null) return false;
            var d = h.parameter.denial;
            var all = d.kiss && d.aibu && d.anal && d.massage && d.notCondom;
            if (!all || Raw || h.fixCharaID != 0 || !Unset(h.parameter.attribute) || Cfg.CardTraits == null || !Cfg.CardTraits.Value) return d.kiss;
            Of(h);
            return KissShy.Contains(GirlInfo.Key(h));
        }

        /// <summary>
        /// What her card refuses in the game's H, most telling first: condom (she insists on one), kiss, anal, caress,
        /// massage (toys). Read as it is: the game's H keeps them, the mod only says them aloud.
        /// </summary>
        public static List<string> HRefusals(SaveData.Heroine h)
        {
            var l = new List<string>();
            if (h == null || h.parameter == null) return l;
            var d = h.parameter.denial;
            if (d.notCondom) l.Add("condom");
            if (d.kiss) l.Add("kiss");
            if (d.anal) l.Add("anal");
            if (d.aibu) l.Add("caress");
            if (d.massage) l.Add("massage");
            return l;
        }

        /// <summary>A refusal as the talk reads it: the card's, except on a card that refuses everything (as unset as
        /// the markers), where only a drawn kiss refusal counts (<see cref="RefusesKiss"/>).</summary>
        public static bool TalkRefuses(SaveData.Heroine h, string what)
        {
            if (what == "kiss") return RefusesKiss(h);
            var l = HRefusals(h);
            if (l.Count == 5 && h.fixCharaID == 0 && Cfg.CardTraits != null && Cfg.CardTraits.Value && !Raw) return false;
            return l.Contains(what);
        }

        public static string Describe(ChaFileParameter.Attribute a)
        {
            var f = Flags(a);
            var on = Names.Where((n, i) => f[i]).ToArray();
            return on.Length == 0 ? "none" : string.Join(", ", on);
        }

        private static ChaFileParameter.Attribute Draw(SaveData.Heroine h)
        {
            var rnd = new Random(Seed(GirlInfo.Key(h)));
            var tone = GirlInfo.ToneOf(h);
            var open = 0f;
            switch (h.personality)
            {
                case 0: case 13: open = 1f; break;                         // sexy, gyaru
                case 1: case 6: case 8: case 19: case 20: open = -1f; break; // ojousama, yamato nadeshiko, pure, bookish, timid
            }
            if (tone == Tone.Teasing) open += 0.5f;
            if (tone == Tone.Cool) open -= 0.3f;
            // Her refusals on the card (kiss, caress, anal, massage, condom): each one a little less easy, unless she
            // refuses everything (as unset as the markers).
            var d = h.parameter.denial;
            var refusals = new[] { d.kiss, d.aibu, d.anal, d.massage, d.notCondom }.Count(x => x);
            var shyCard = refusals == 5 ? 0f : refusals * 0.04f;
            var likes = h.parameter.awnser;
            Func<double, bool> roll = p => rnd.NextDouble() < Math.Max(0.02, Math.Min(0.9, p));
            var a = new ChaFileParameter.Attribute();
            a.choroi = roll(0.18 + 0.2 * open - shyCard);
            a.bitch = roll(0.1 + 0.25 * open - shyCard);
            a.mutturi = roll(0.18 + (open < 0 ? 0.15 : 0) + (tone == Tone.Gentle ? 0.05 : 0));
            a.ukemi = roll(0.2 + (tone == Tone.Gentle ? 0.15 : 0) - (tone == Tone.Teasing ? 0.1 : 0));
            a.friendly = roll(0.35 + (tone == Tone.Energetic ? 0.25 : 0) + 0.1 * open);
            a.kappatu = roll(0.25 + (tone == Tone.Energetic ? 0.3 : 0) - (tone == Tone.Cool ? 0.1 : 0));
            a.majime = roll(0.25 + (tone == Tone.Cool ? 0.2 : 0) - 0.15 * open + (likes.study ? 0.1 : 0));
            a.kireizuki = roll(0.2 + (open < 0 ? 0.1 : 0) + (likes.fashionable ? 0.1 : 0));
            a.hitori = roll(0.12 + (tone == Tone.Cool ? 0.15 : 0) - (tone == Tone.Energetic ? 0.08 : 0));
            a.donkan = roll(0.18 + (tone == Tone.Gentle ? 0.05 : 0));
            a.sinsyutu = roll(0.12);
            a.taida = roll(0.12 + (tone == Tone.Cool ? 0.05 : 0));
            a.hinnyo = roll(0.08);
            a.harapeko = roll(0.1 + (likes.eat ? 0.25 : 0) + (likes.sweet ? 0.05 : 0));
            a.dokusyo = roll(0.12 + (likes.study ? 0.3 : 0));
            a.ongaku = roll(0.18);
            a.undo = roll(0.08 + (likes.exercise ? 0.35 : 0));
            a.likeGirls = roll(0.12);
            if (refusals == 5 && roll(0.08 - 0.08 * open + (a.majime ? 0.1 : 0)))
                KissShy.Add(GirlInfo.Key(h));
            else KissShy.Remove(GirlInfo.Key(h));
            return a;
        }

        private static int Seed(string key)
        {
            unchecked
            {
                var x = (int)2166136261;
                foreach (var ch in key) x = (x ^ ch) * 16777619;
                return x;
            }
        }

        /// <summary>The game's AI asks for her markers: the same drawn ones.</summary>
        private static void GameReads(ChaFileControl _chafile, ref bool[] __result)
        {
            try
            {
                if (Raw || _chafile == null || !Campus.On || Cfg.CardTraits == null || !Cfg.CardTraits.Value) return;
                if (!Unset(_chafile.parameter.attribute)) return;
                SaveData.Heroine h;
                // The game's AI reads her through chaCtrl.GetAttribute(), on the ChaControl's own chaFile, which is not always
                // the heroine's charFile.
                if (!ByFile.TryGetValue(_chafile, out h) || h == null || (h.charFile != _chafile && (h.chaCtrl == null || h.chaCtrl.chaFile != _chafile)))
                {
                    h = G.Heroines.FirstOrDefault(x => x != null && (x.charFile == _chafile || (x.chaCtrl != null && x.chaCtrl.chaFile == _chafile)));
                    ByFile[_chafile] = h;
                }
                if (h == null || h.fixCharaID != 0) return;
                __result = Flags(Of(h));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Card traits (game): " + e.Message); }
        }

        public static string Report()
        {
            var lines = new List<string>();
            foreach (var h in G.Heroines.Where(x => x != null && GirlInfo.IsRegular(x)))
            {
                Raw = true;
                Pace before;
                float mBefore;
                try { before = Consent.Profile(h); mBefore = Consent.Multiplier(h); }
                finally { Raw = false; }
                var after = Consent.Profile(h);
                var dn = h.parameter.denial;
                lines.Add(GirlInfo.FirstName(h) + " (refuses " + new[] { dn.kiss, dn.aibu, dn.anal, dn.massage, dn.notCondom }.Count(x => x) + "/5, kiss " + (RefusesKiss(h) ? "no" : "ok") + "): card " + (Unset(h.parameter.attribute) ? "UNSET (" + (h.parameter.attribute.choroi ? "all yes" : "all no") + ")" : "set") +
                          " -> " + Describe(Of(h)) + "; pace " + before + " x" + mBefore.ToString("0.00") + " -> " + after + " x" + Consent.Multiplier(h).ToString("0.00"));
            }
            return string.Join("\n", lines.ToArray());
        }

        public static string Set(SaveData.Heroine h, string what)
        {
            var a = h.parameter.attribute;
            var list = what == "all" ? Names : what == "none" ? new string[0] : what.Split(',').Select(x => x.Trim()).ToArray();
            foreach (var n in Names) Traverse.Create(a).Property(n).SetValue(list.Contains(n));
            Drawn.Remove(GirlInfo.Key(h));
            ByFile.Clear();
            return GirlInfo.FirstName(h) + ": card now " + Describe(a) + " -> uses " + Describe(Of(h)) + ", pace " + Consent.Profile(h);
        }
    }
}
