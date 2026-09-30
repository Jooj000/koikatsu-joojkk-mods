using System.Collections.Generic;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// How fast she lets things go (procedural dialogue, section 7; decision koikatsu-decisao-ritmo-de-5-dias-por-personalidade):
    /// a reserved girl gains romance and spicy at 0.7, rising with the days you know her and her trust up to normal; an
    /// open one at 1.4; dating adds 0.1. A good day builds momentum (up to x1.5 the same day) and the week's spotlight
    /// girls give x1.25.
    /// </summary>
    internal static class Rhythm
    {
        internal enum Profile { Reserved, Normal, Open }

        private static CampusState S { get { return Campus.S; } }

        public static Profile Of(SaveData.Heroine h)
        {
            if (h == null) return Profile.Normal;
            var a = GirlInfo.Quirks(h);
            var v = Voice.Of(h);
            var open = 0;
            if (a.bitch) open += 2;
            if (a.choroi) open += 1;
            if (a.mutturi && Social.Trust(h) >= 50) open += 1;
            if (v.Kind == "gyaru" || v.Kind == "vamp") open += 1;
            if (v.Tease >= 0.75f) open += 1;
            var closed = 0;
            if (a.majime) closed += 1;
            if (a.kireizuki) closed += 1;
            if (a.hitori) closed += 1;
            try { if (CardTraits.RefusesKiss(h)) closed += 2; } catch { }
            if (v.Kind == "shy" || v.Kind == "ojou" || v.Kind == "serious" || v.Kind == "proper") closed += 1;
            if (open - closed >= 2) return Profile.Open;
            if (closed - open >= 2) return Profile.Reserved;
            return Profile.Normal;
        }

        /// <summary>The multiplier for a positive gain at this level (0 friend, 1 romance, 2 spicy).</summary>
        public static float Gain(SaveData.Heroine h, int level)
        {
            if (h == null || S == null) return 1f;
            var key = GirlInfo.Key(h);
            var f = 1f;
            if (level > 0)
            {
                switch (Of(h))
                {
                    case Profile.Reserved:
                        int met;
                        var days = S.MetDay.TryGetValue(key, out met) ? S.Day - met : 0;
                        f = Mathf.Min(1f, 0.7f + days * 0.03f + Social.Trust(h) / 300f);
                        break;
                    case Profile.Open: f = 1.4f; break;
                }
                if (h.isGirlfriend) f += 0.1f;
            }
            f *= 1f + Mathf.Min(0.5f, Momentum(key) * 0.1f);
            if (Spotlight.IsActive && S.Spotlight.Contains(key)) f *= 1.25f;
            return f;
        }

        private static readonly Dictionary<string, int> Good = new Dictionary<string, int>();
        private static int _day = -1;

        private static int Momentum(string key)
        {
            if (S == null || _day != S.Day) { Good.Clear(); _day = S != null ? S.Day : -1; }
            int n;
            return Good.TryGetValue(key, out n) ? n : 0;
        }

        /// <summary>A good answer today: the day builds up (resets overnight).</summary>
        public static void OnGood(SaveData.Heroine h)
        {
            if (h == null) return;
            var key = GirlInfo.Key(h);
            Good[key] = Momentum(key) + 1;
        }

        public static string Describe(SaveData.Heroine h)
        {
            return Of(h) + ", friend x" + Gain(h, 0).ToString("0.00") + ", romance x" + Gain(h, 1).ToString("0.00") + ", momentum " + Momentum(GirlInfo.Key(h));
        }
    }
}
