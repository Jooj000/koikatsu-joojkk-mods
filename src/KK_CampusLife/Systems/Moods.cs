using System.Collections.Generic;
using System.Linq;
using ActionGame.Communication;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>Daily moods derived from each girl's quirks, relationship and recent attention.</summary>
    internal static class Moods
    {
        public static void RollAll()
        {
            if (!Cfg.MoodsEnabled.Value) return;
            var s = Campus.S;
            foreach (var h in G.Heroines.Where(x => x != null))
            {
                var gs = s.Girl(GirlInfo.Key(h));
                if (gs.MoodDay == s.Day) continue;
                gs.Mood = Roll(h, gs);
                gs.MoodDay = s.Day;
            }
        }

        private static Mood Roll(SaveData.Heroine h, GirlState gs)
        {
            var w = new Dictionary<Mood, float>
            {
                { Mood.None, 3f }, { Mood.Cheerful, 2f }, { Mood.Hungry, 1f }, { Mood.Bored, 1f }, { Mood.Stressed, 1f },
                { Mood.Lonely, 1f }, { Mood.Flirty, 0.7f }, { Mood.Sleepy, 1f }, { Mood.Upset, 0.2f },
            };
            var q = GirlInfo.Quirks(h);
            if (q.harapeko) w[Mood.Hungry] += 3f;
            if (q.taida) { w[Mood.Sleepy] += 2f; w[Mood.Bored] += 1f; }
            if (q.majime) w[Mood.Stressed] += 2f;
            if (q.hitori) w[Mood.Lonely] += 2f;
            if (q.kappatu || q.undo) { w[Mood.Cheerful] += 1f; w[Mood.Bored] += 1f; }
            if (q.bitch || q.mutturi || q.choroi) w[Mood.Flirty] += 2f;
            if (q.friendly) w[Mood.Cheerful] += 2f;
            if (h.lewdness >= 70) w[Mood.Flirty] += 2f;
            if (h.isGirlfriend) w[Mood.Flirty] += 1f;
            if (gs.Attention < 25) w[Mood.Lonely] += 3f;
            if (gs.Rep < 25) { w[Mood.Lonely] += 3f; w[Mood.Upset] += 0.5f; }   // left out at school (Standing)
            else if (gs.Rep >= 70) w[Mood.Cheerful] += 1.5f;
            if (h.isGirlfriend && gs.Attention < 20) w[Mood.Upset] += 2f;
            if (h.isAnger) w[Mood.Upset] += 4f;
            if (Calendar.IsExamWeek) w[Mood.Stressed] += 2f;

            var total = w.Values.Sum();
            var r = Random.value * total;
            foreach (var kv in w)
            {
                r -= kv.Value;
                if (r <= 0f) return kv.Key;
            }
            return Mood.None;
        }

        /// <summary>The player can read the mood if INT is high enough or she told him today.</summary>
        public static bool Known(GirlState gs)
        {
            return gs.MoodKnownDay == Campus.S.Day || G.Int >= Cfg.MoodHintMinIntelligence.Value;
        }

        public static string Label(GirlState gs)
        {
            if (!Cfg.MoodsEnabled.Value) return "";
            return Known(gs) ? Lines.MoodName(gs.Mood) : "???";
        }

        /// <summary>Bonus favor when what you did matches her mood; the mood then improves.</summary>
        public static void OnTalkResult(SaveData.Heroine h, ResultEnum result)
        {
            if (!Cfg.MoodsEnabled.Value) return;
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            var m = Cfg.MoodBonusMultiplier.Value;
            var bonus = 0;
            var resolved = false;
            switch (gs.Mood)
            {
                case Mood.Hungry:
                    if (result == ResultEnum.Lunch) { bonus = 6; resolved = true; }
                    break;
                case Mood.Bored:
                    if (result == ResultEnum.Exercise || result == ResultEnum.Club) { bonus = 5; resolved = true; }
                    else if (result == ResultEnum.None) bonus = 1;
                    break;
                case Mood.Stressed:
                    if (result == ResultEnum.Study) { bonus = 6; resolved = true; }
                    break;
                case Mood.Lonely:
                    if (result == ResultEnum.GoHome) { bonus = 7; resolved = true; }
                    else { bonus = 3; resolved = result != ResultEnum.None; }   // anything you do together lifts it (context)
                    break;
                case Mood.Sleepy:
                    if (result == ResultEnum.Exercise) bonus = -3;
                    break;
                case Mood.Cheerful:
                    if (result != ResultEnum.None) bonus = 2;
                    break;
                case Mood.Upset:
                    bonus = 0;
                    break;
            }
            if (bonus != 0) G.AddFavor(h, Mathf.RoundToInt(bonus * m), 0);
            if (resolved)
            {
                gs.Mood = Mood.Cheerful;
                if (bonus > 0) Toasts.Show(GirlInfo.FirstName(h) + " cheered up (+" + Mathf.RoundToInt(bonus * m) + " favor).");
            }
        }
    }
}
