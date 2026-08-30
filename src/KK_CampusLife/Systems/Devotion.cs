using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Long-term bond with each girl. Favor stops at 100, devotion does not: requests and gifts always add some, and
    /// favor that would go past 100 turns into devotion. Every level unlocks a perk, so helping girls who already
    /// love you still pays off in the endgame.
    /// </summary>
    internal static class Devotion
    {
        public const int PerLevel = 40;
        public const int MaxLevel = 5;

        private static CampusState S { get { return Campus.S; } }

        public static readonly string[] LevelNames = { "-", "Friendly", "Close", "Devoted", "Inseparable", "Soulmate" };

        public static readonly string[] PerkText =
        {
            "",
            "Her requests pay 50% more yen.",
            "The first talk with her each day restores 1 energy.",
            "Once a day she has a little present for you when you talk.",
            "Activities in the same place as her give +1 to their stat.",
            "Her requests give double rewards, and she never holds a missed one against you.",
        };

        public static int Level(GirlState gs) { return gs == null ? 0 : Math.Min(MaxLevel, gs.Devotion / PerLevel); }

        public static int Level(SaveData.Heroine h) { return h == null ? 0 : Level(S.Girl(GirlInfo.Key(h))); }

        public static bool Has(SaveData.Heroine h, int level) { return Cfg.DevotionEnabled.Value && Level(h) >= level; }

        /// <summary>Progress text such as "Close (Lv 2) 95/120".</summary>
        public static string Label(GirlState gs)
        {
            var lv = Level(gs);
            if (lv >= MaxLevel) return LevelNames[lv] + " (max)";
            return (lv == 0 ? "Devotion" : LevelNames[lv]) + " " + gs.Devotion + "/" + (lv + 1) * PerLevel;
        }

        public static void Add(SaveData.Heroine h, int amount)
        {
            if (h == null || amount <= 0 || !Cfg.DevotionEnabled.Value) return;
            var gs = S.Girl(GirlInfo.Key(h));
            var before = Level(gs);
            gs.Devotion += amount;
            var after = Level(gs);
            if (after > before)
            {
                var msg = GirlInfo.FirstName(h) + " is now " + LevelNames[after] + " with you! " + PerkText[after];
                S.Log(msg);
                Toasts.Show(msg);
            }
        }

        /// <summary>Favor beyond 100 is not lost: it becomes devotion.</summary>
        public static int Overflow(SaveData.Heroine h, int favor)
        {
            if (h == null || favor <= 0) return 0;
            return Math.Max(0, h.favor + favor - 100);
        }

        // ---------------- perks ----------------

        /// <summary>First talk of the day with her (called before the talk is counted).</summary>
        public static void OnFirstTalkToday(SaveData.Heroine h, GirlState gs)
        {
            if (!Cfg.DevotionEnabled.Value) return;
            var lv = Level(gs);
            if (lv >= 2 && Cfg.EnergyEnabled.Value && S.Energy < Activities.MaxEnergy)
            {
                S.Energy++;
                Toasts.Show("Talking with " + GirlInfo.FirstName(h) + " recharged you. (+1 energy)");
            }
            if (lv >= 3 && gs.LastPerkDay != S.Day)
            {
                gs.LastPerkDay = S.Day;
                var item = PresentFor(h);
                S.AddItem(item.Id, 1);
                var msg = GirlInfo.FirstName(h) + " gave you a present: " + item.Name + ".";
                S.Log(msg);
                Toasts.Show(msg);
            }
        }

        private static ItemDef PresentFor(SaveData.Heroine h)
        {
            // Mostly useful consumables, sometimes a gift you can pass on.
            var pool = Items.All.Where(i => i.Type == ItemType.Consumable && i.Id != "cat_food").ToList();
            if (Random.value < 0.3f) pool = Items.All.Where(i => i.Type == ItemType.Gift && !i.Intimate && i.Price <= 1200).ToList();
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>Extra stat for an activity done where a level 4+ girl is.</summary>
        public static SaveData.Heroine Cheerleader()
        {
            if (!Cfg.DevotionEnabled.Value) return null;
            var here = G.PlayerMapNo;
            return G.Heroines.FirstOrDefault(h => GirlInfo.IsSpawned(h) && GirlInfo.CurrentMap(h) == here && Level(h) >= 4);
        }

        public static float RequestMultiplier(SaveData.Heroine h) { return Has(h, 5) ? 2f : 1f; }

        public static float MoneyMultiplier(SaveData.Heroine h) { return Has(h, 1) ? 1.5f : 1f; }

        public static bool ForgivesMisses(SaveData.Heroine h) { return Has(h, 5); }
    }
}
