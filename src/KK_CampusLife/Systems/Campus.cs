using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Communication;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>Central coordinator: owns the save state and routes game events to every system.</summary>
    internal static class Campus
    {
        public static CampusState S = new CampusState();
        public static bool Loaded;

        public static bool On { get { return Cfg.Enabled.Value && Loaded && G.HasGame; } }

        // ---------------- lifecycle ----------------

        public static void Load(string json)
        {
            try { S = string.IsNullOrEmpty(json) ? new CampusState() : CampusState.FromJson(json); }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError("Campus Life data in this save could not be read, starting fresh. " + e);
                S = new CampusState();
            }
            Loaded = true;
            // Older versions could post requests for story heroines, who have no everyday conversations.
            var dropped = S.Requests.RemoveAll(r => r.State == RequestState.Active && ((r.Girl ?? "").StartsWith("fix:") || (r.Other ?? "").StartsWith("fix:")));
            if (dropped > 0) CampusLifePlugin.Log.LogInfo("Removed " + dropped + " request(s) for story heroines.");
            EnsureStarted();
            if (Spotlight.IsActive && S.Spotlight.Count == 0) Spotlight.Rotate();
        }

        public static void Reset()
        {
            S = new CampusState();
            Loaded = true;
            EnsureStarted();
        }

        /// <summary>
        /// Week numbers count school weeks: the first Monday is always week 1. Saves from v1 counted the
        /// Sunday a new game starts on as week 1, so their first Monday became week 2. Runs once the day is known.
        /// </summary>
        public static void FixWeekNumbering()
        {
            if (S.Version >= CampusState.CurrentVersion || !S.Started) return;
            var w = G.Weekday;
            if (!w.HasValue) return;
            // Cycle.Week: Monday = 0 ... Saturday = 5, Holiday (Sunday) = 6.
            var startDay = (((int)w.Value - (S.Day - 1)) % 7 + 7) % 7;
            if (startDay == (int)Cycle.Week.Holiday && S.Week >= 1)
            {
                S.ShiftWeeks(-1);
                CampusLifePlugin.Log.LogInfo("Week numbering corrected: now week " + S.Week);
            }
            S.Version = CampusState.CurrentVersion;
        }

        public static void EnsureStarted()
        {
            if (S.Started || !G.HasGame) return;
            S.Started = true;
            S.Version = 1; // FixWeekNumbering sets the right first week once the weekday is known
            S.Money = Cfg.StartingMoney.Value;
            S.Energy = Activities.MaxEnergy;
            S.Reputation = 20;
            S.Log("Campus Life begins. The Koikatsu Club has a board now, and the school feels a little bigger.");
            Moods.RollAll();
            Spotlight.Rotate();
            if (G.IsSchoolDay) Board.GenerateDaily();
        }

        // ---------------- game events ----------------

        public static void OnDayChange(Cycle.Week day)
        {
            if (!On) return;
            EnsureStarted();

            S.Day++;
            if (day == Cycle.Week.Monday)
            {
                S.Week++;
                S.LastWeekStats = new Dictionary<string, int>(S.WeekStats);
                S.WeekStats.Clear();
                if (S.LastAllowanceWeek != S.Week && Cfg.WeeklyAllowance.Value > 0)
                {
                    S.LastAllowanceWeek = S.Week;
                    S.Money += Cfg.WeeklyAllowance.Value;
                    S.Log("Weekly allowance: +" + Cfg.WeeklyAllowance.Value + " yen.");
                }
                Spotlight.Rotate();
            }

            Board.ExpireAll(beforeDay: S.Day);
            S.Energy = Activities.MaxEnergy;
            // Story director bookkeeping (dir:, grad:) is permanent; real buffs expire.
            S.Buffs = S.Buffs.Where(kv => kv.Value >= S.Day - 1 || kv.Key.StartsWith("dir:") || kv.Key.StartsWith("grad:")).ToDictionary(kv => kv.Key, kv => kv.Value);
            S.PendingHappening = "";

            Spotlight.OffscreenLife();
            Moods.RollAll();

            if (day != Cycle.Week.Holiday)
            {
                Calendar.OnSchoolDay(day);
                StoryCast.ChooseToday();
                Arcs.OnNewDay();
                Board.GenerateDaily();
            }
        }

        public static void OnPeriodChange(Cycle.Type period)
        {
            if (!On) return;
            Board.ExpirePassedWindows();
            if (G.ToMask(period) != PeriodMask.None && G.IsSchoolDay)
            {
                Calendar.OnPeriodStart(period);
                if (period == Cycle.Type.LunchTime && S.Requests.Any(r => r.State == RequestState.Active))
                    Toasts.Show("Club board: " + S.Requests.Count(r => r.State == RequestState.Active) + " open request(s). Press " + Cfg.PlannerKey.Value + ".");
            }
        }

        public static void OnNightMenu()
        {
            if (!On) return;
            Report.MaybeShow();
        }

        /// <summary>Called after every conversation closes (from the TalkScene.OnDestroy postfix).</summary>
        public static void OnTalkEnd(SaveData.Heroine h, ResultEnum result)
        {
            if (!On || h == null) return;
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            var firstToday = gs.LastTalkDay != S.Day;
            if (firstToday) Devotion.OnFirstTalkToday(h, gs);
            gs.LastTalkDay = S.Day;
            gs.Talks++;
            gs.Attention = Mathf.Clamp(gs.Attention + (firstToday ? 15 : 3), 0, 100);
            S.Stat("talks");
            Chat.OnTalkEnd(h, gs, result == ResultEnum.None);

            // Time cost: only for plain conversations; invites already move the clock themselves.
            if (result == ResultEnum.None && G.InRoamingPeriod && Cfg.TalkTimeCost.Value > 0)
            {
                var cost = (float)Cfg.TalkTimeCost.Value;
                if (Cfg.StrengthReducesTalkCost.Value) cost *= 1f - 0.4f * G.Str / 100f;
                G.AddPeriodSeconds(cost);
            }

            if (S.HasBuff("cologne") && firstToday) G.AddFavor(h, 2, 0);

            Moods.OnTalkResult(h, result);
            Board.OnTalkResult(h, result);
            Arcs.OnTalkResult(h, result);
        }

        public static void OnHEnd(List<SaveData.Heroine> heroines, bool freeH)
        {
            if (!On || freeH || heroines == null) return;
            foreach (var h in heroines.Where(x => x != null))
            {
                var gs = S.Girl(GirlInfo.Key(h));
                gs.Attention = Mathf.Clamp(gs.Attention + 20, 0, 100);
                S.Stat("h");
                Board.OnIntimate(h);
                if (gs.Mood == Mood.Flirty) { G.AddFavor(h, Mathf.RoundToInt(5 * Cfg.MoodBonusMultiplier.Value), 0); gs.Mood = Mood.Cheerful; }
            }
        }

        // ---------------- rewards ----------------

        /// <summary>
        /// Applies rewards with stat scaling: INT boosts favor, SEN boosts lewd. Favor past 100 becomes devotion.
        /// Returns the applied values.
        /// </summary>
        public static Rewards Apply(Rewards r, SaveData.Heroine h, bool scale = true)
        {
            var m = scale ? Cfg.RewardMultiplier.Value : 1f;
            var applied = new Rewards
            {
                Favor = Mathf.RoundToInt(r.Favor * m * (r.Favor > 0 ? 1f + G.Int / 200f : 1f)),
                Lewd = Mathf.RoundToInt(r.Lewd * m * (r.Lewd > 0 ? 1f + G.Sens / 150f : 1f)),
                Money = Mathf.RoundToInt(r.Money * m),
                Rep = r.Rep, Int = r.Int, Str = r.Str, Sens = r.Sens, Club = r.Club,
                Devotion = Mathf.RoundToInt(r.Devotion * m), Item = r.Item ?? "",
            };
            if (h != null && Cfg.DevotionEnabled.Value)
            {
                applied.Devotion += Devotion.Overflow(h, applied.Favor);
                Devotion.Add(h, applied.Devotion);
            }
            else applied.Devotion = 0;
            if (h != null) G.AddFavor(h, applied.Favor, applied.Lewd);
            S.Money = Math.Max(0, S.Money + applied.Money);
            S.Reputation = Mathf.Clamp(S.Reputation + applied.Rep, 0, 100);
            G.AddStats(applied.Int, applied.Str, applied.Sens);
            if (applied.Club > 0)
            {
                S.ClubFunds += applied.Club;
                G.AddClubPoints(applied.Club);
            }
            if (!string.IsNullOrEmpty(applied.Item) && Items.Get(applied.Item) != null) S.AddItem(applied.Item, 1);
            return applied;
        }

        public static bool SpendEnergy(int n)
        {
            if (!Cfg.EnergyEnabled.Value) return true;
            if (S.Energy < n) return false;
            S.Energy -= n;
            return true;
        }

        /// <summary>Stat check used by exams, festivals and happenings. Consumes a lucky charm if owned.</summary>
        public static bool Check(int stat, int difficulty, out int roll)
        {
            roll = stat + UnityEngine.Random.Range(0, 41);
            if (S.Count("omamori") > 0)
            {
                S.AddItem("omamori", -1);
                roll += 15;
            }
            return roll >= difficulty;
        }

        // ---------------- activities ----------------

        /// <summary>The activity's result. It costs energy, not period time: only the real time of its animation passes.</summary>
        public static ActivityResult DoActivity(ActivityDef a)
        {
            var block = Activities.Blocker(S, a);
            if (block != null) { Toasts.Show(block); return null; }
            if (!SpendEnergy(a.Energy)) return null;
            if (a.OncePerDay) S.Buffs["done:" + a.Id] = S.Day;

            var res = Activities.Do(S, a);
            var fan = Devotion.Cheerleader();
            if (fan != null && (res.Gains.Int > 0 || res.Gains.Str > 0 || res.Gains.Sens > 0))
            {
                if (res.Gains.Int > 0) res.Gains.Int++;
                if (res.Gains.Str > 0) res.Gains.Str++;
                if (res.Gains.Sens > 0) res.Gains.Sens++;
                res.Text += "\n" + GirlInfo.FirstName(fan) + " cheered you on (+1).";
            }
            var applied = Apply(res.Gains, null, false);
            res.Gains = applied;
            S.Stat("activities");
            S.Stat("act:" + a.Id);

            var enc = Activities.Encounter(a);
            if (enc != null)
            {
                res.EncounterGirl = GirlInfo.Key(enc);
                var bonus = 3 + G.Sens / 30;
                G.AddFavor(enc, bonus, 0);
                var gs = S.Girl(res.EncounterGirl);
                gs.Attention = Mathf.Clamp(gs.Attention + 8, 0, 100);
                res.Text += "\n" + GirlInfo.FirstName(enc) + " was here too and joined you for a while (+" + bonus + " favor).";
            }

            S.Log(a.Name + ": " + res.Text.Replace("\n", " ") + " (" + applied.Describe() + ")");
            Board.OnActivity(a);
            Arcs.OnActivity(a);
            return res;
        }

        // ---------------- shop & items ----------------

        public static bool Buy(ItemDef item, int qty = 1)
        {
            var price = Items.PriceOf(item) * qty;
            if (S.Money < price) { Toasts.Show("Not enough money."); return false; }
            S.Money -= price;
            S.AddItem(item.Id, qty);
            S.Stat("spent", price);
            return true;
        }

        public static void UseConsumable(ItemDef item)
        {
            if (S.Count(item.Id) <= 0) return;
            switch (item.Id)
            {
                case "energy_drink":
                    S.AddItem(item.Id, -1);
                    S.Energy = Math.Min(Activities.MaxEnergy + 2, S.Energy + 2);
                    Toasts.Show("Energy +2.");
                    break;
                case "cologne":
                    S.AddItem(item.Id, -1);
                    S.Buffs["cologne"] = S.Day;
                    Toasts.Show("You smell great. Conversations give +2 favor today.");
                    break;
                default:
                    Toasts.Show(item.Name + " is used automatically when it matters.");
                    break;
            }
        }
    }

    /// <summary>Small on-screen notifications.</summary>
    internal static class Toasts
    {
        private sealed class Toast { public string Text; public float Until; }
        private static readonly List<Toast> Active = new List<Toast>();

        public static void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            CampusLifePlugin.Log.LogInfo(text);
            if (!Cfg.ShowToasts.Value) return;
            Active.Add(new Toast { Text = text, Until = Time.realtimeSinceStartup + Cfg.ToastSeconds.Value });
            if (Active.Count > 5) Active.RemoveAt(0);
        }

        /// <summary>While a loading screen covers them, toasts wait: their time runs only once it is gone.</summary>
        public static void Hold(float seconds)
        {
            foreach (var t in Active) t.Until += seconds;
        }

        public static IEnumerable<string> Current
        {
            get
            {
                var now = Time.realtimeSinceStartup;
                Active.RemoveAll(t => t.Until < now);
                return Active.Select(t => t.Text);
            }
        }
    }
}
