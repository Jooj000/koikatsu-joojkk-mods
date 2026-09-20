using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The director's brain: every action a girl takes is the director's choice. User, 2026-10-11: "the director must
    /// control 100%. nothing of the vanilla AI may be left." The game drew each next action from its own desire table
    /// (that is how girls went off into H on their own); now its draw (<c>ActionControl.NextAction</c> and, at the start of
    /// a period, <c>FirstAction</c>) is replaced by the director's, and the game only carries it out (walking, animating).
    /// <list type="bullet">
    /// <item>Everyday actions are weighed from the period, her needs (food, rest, company), her intention, her mood and
    /// her club. Where she goes and with whom then follows the intention (<see cref="Intents"/>).</item>
    /// <item>H on her own (4), asking the player for H (5) or to talk (8): only by the director's rules (her lewdness, your
    /// relationship, her mood), never on her own desire; none at all while a test runs the director at maximum weight.
    /// The scene between two girls (26) only comes from a couple's intention (<see cref="Intents"/>, Privacy).</item>
    /// <item>The game's other decisions are the director's elsewhere: running off and being seen (<see cref="Reactions"/>),
    /// who comes when you sleep, the partner of a scene between girls, the stage (<see cref="Stage"/>).</item>
    /// </list>
    /// </summary>
    internal static class Brain
    {
        private static CampusState S { get { return Campus.S; } }

        public static int Decided, LeftToGame, SpotsRefused, Clashes;
        public static readonly Dictionary<int, int> Picks = new Dictionary<int, int>();
        public static readonly List<string> Recent = new List<string>();

        private static bool On { get { return Campus.On && Cfg.DirectorEnabled.Value && Cfg.FullControl.Value; } }

        public static void Install(Harmony harmony)
        {
            var first = AccessTools.Method(typeof(ActionControl), "FirstAction", new[] { typeof(SaveData.Heroine) });
            if (first == null) CampusLifePlugin.Log.LogWarning("ActionControl.FirstAction not found: the game picks the first action of a period.");
            else harmony.Patch(first, postfix: new HarmonyMethod(AccessTools.Method(typeof(Brain), nameof(FirstPostfix))));
        }

        private static bool _inside;

        private static void FirstPostfix(ActionControl __instance, SaveData.Heroine _heroine, ref ActionControl.ResultInfo __result)
        {
            if (_inside || _heroine == null) return;
            try
            {
                if (!On) return;
                _inside = true;
                if (Stage.Steer(__instance, _heroine, ref __result)) return;
                Decide(__instance, _heroine, null, ref __result, true);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Brain first: " + e.Message); }
            finally { _inside = false; }
        }

        /// <summary>
        /// From the NextAction postfix (after the stage): the director's own draw replaces the game's. True when it decided.
        /// </summary>
        public static bool Decide(ActionControl ac, SaveData.Heroine h, int[] not, ref ActionControl.ResultInfo r, bool first = false)
        {
            if (!On || ac == null || h == null) return false;
            var weights = Weigh(h, not);
            // Draw without putting back: an action with no spot for her falls to the next one.
            while (weights.Count > 0)
            {
                var total = weights.Sum(kv => kv.Value);
                var roll = Random.value * total;
                var pick = weights[weights.Count - 1].Key;
                foreach (var kv in weights) { roll -= kv.Value; if (roll <= 0f) { pick = kv.Key; break; } }
                weights.RemoveAll(kv => kv.Key == pick);
                // The first action of a period places her at its spot while the map loads (SetAllCharaActive): it must have
                // one, and no target.
                if (first && !TargetsPoint(pick)) continue;
                var res = ac.SetAction(h, pick, -1);
                if (res == null || res.point == null && (first || TargetsPoint(pick))) continue;
                var taken = SpotTaken(res.point, h);
                if (taken != null)
                {
                    SpotsRefused++;
                    Note(GirlInfo.FirstName(h) + ": action " + pick + " not on " + GirlInfo.FirstName(taken.heroine) + "'s spot");
                    continue;
                }
                r = res;
                Decided++;
                int c;
                Picks.TryGetValue(pick, out c);
                Picks[pick] = c + 1;
                if (pick == 4 || pick == 5 || pick == 8) Note(GirlInfo.FirstName(h) + ": action " + pick + (first ? " (first)" : ""));
                return true;
            }
            LeftToGame++;
            Note(GirlInfo.FirstName(h) + ": no action of the director's had a spot; the game's " + (r != null ? r.actionNo.ToString() : "none") + " stays");
            return false;
        }

        /// <summary>
        /// Another girl already has that spot or one right on it: the game reserves each waiting point, but one chair can
        /// personagens sentando na mesma cadeira").
        /// </summary>
        public static ActionGame.Chara.NPC SpotTaken(WaitPoint p, SaveData.Heroine h)
        {
            try
            {
                if (p == null || G.Act == null) return null;
                var pos = p.position;
                foreach (var n in G.Act.npcList)
                {
                    if (n == null || n.heroine == null || n.heroine == h || n.AI == null || n.AI.result == null) continue;
                    var q = n.AI.result.point;
                    if (q == null || q.MapNo != p.MapNo) continue;
                    if (q == p) return n;
                    var d = q.position - pos;
                    if (Mathf.Abs(d.y) < 1f && d.x * d.x + d.z * d.z < 0.16f) return n;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("SpotTaken: " + e.Message); }
            return null;
        }

        private static float _nextClashCheck;
        private static readonly HashSet<string> ClashSeen = new HashSet<string>();
        private static int _clashPeriod = -1;

        /// <summary>
        /// Every few seconds: two girls standing or sitting in the same place (whatever put them there) go in the log and
        /// </summary>
        public static void CheckClashes()
        {
            if (!Campus.On || G.Act == null || !G.InRoamingPeriod || Time.realtimeSinceStartup < _nextClashCheck) return;
            _nextClashCheck = Time.realtimeSinceStartup + 3f;
            try
            {
                var period = S.Day * 16 + (int)G.CurrentMask;
                if (period != _clashPeriod) { _clashPeriod = period; ClashSeen.Clear(); }
                var still = G.Act.npcList.Where(n => n != null && n.heroine != null && n.mapNo >= 0 && n.isActiveAndEnabled && n.AI != null && n.AI.result != null && n.AI.result.point != null
                                                     && (n.position - n.AI.result.point.position).sqrMagnitude < 0.36f).ToList();
                for (var i = 0; i < still.Count; i++)
                    for (var j = i + 1; j < still.Count; j++)
                    {
                        var a = still[i]; var b = still[j];
                        if (a.mapNo != b.mapNo) continue;
                        var d = a.position - b.position;
                        if (Mathf.Abs(d.y) >= 1f || d.x * d.x + d.z * d.z >= 0.12f) continue;
                        // Talking to each other (7) or a scene between two girls (26/27) puts them close on purpose.
                        var an = a.AI.result.actionNo; var bn = b.AI.result.actionNo;
                        if (an == 7 || bn == 7 || an == 26 || an == 27 || bn == 26 || bn == 27) continue;
                        var key = GirlInfo.Key(a.heroine) + "|" + GirlInfo.Key(b.heroine);
                        if (!ClashSeen.Add(key)) continue;
                        Clashes++;
                        var text = "Clash: " + GirlInfo.FirstName(a.heroine) + " (" + an + ") and " + GirlInfo.FirstName(b.heroine) + " (" + bn + ") on the same spot, " + Places.MapLabel(a.mapNo);
                        Note(text);
                        CampusLifePlugin.Log.LogWarning(text);
                    }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("CheckClashes: " + e.Message); }
        }

        /// <summary>Actions that need a spot (waiting point) for SetAction to be any use.</summary>
        private static bool TargetsPoint(int a) { return a != 7 && a != 8 && a != 5; }

        private static void Note(string s)
        {
            Recent.Add(S.Day + "/" + G.CurrentMask + " " + s);
            if (Recent.Count > 40) Recent.RemoveAt(0);
        }

        /// <summary>The director's weights for her next action (the game's numbers, see aidump).</summary>
        private static List<KeyValuePair<int, float>> Weigh(SaveData.Heroine h, int[] not)
        {
            var w = new Dictionary<int, float>();
            var mask = G.CurrentMask;
            var gs = S.Girl(GirlInfo.Key(h));
            var intent = (gs.Intent ?? "").Split(new[] { ':' }, 2)[0];
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var sporty = GirlInfo.InSportsClub(h);

            // Getting changed: only when her outfit is wrong for where she is (the game's own check).
            try { if (h.isDresses != null && Array.IndexOf(h.isDresses, false) != -1) w[0] = 40f; } catch { }
            w[1] = 3f; // the toilet
            if (sporty && (mask == PeriodMask.Club || mask == PeriodMask.After)) w[2] = 6f; // a shower after practice
            // Food: lunch, and when she is hungry.
            w[3] = (mask == PeriodMask.Lunch ? 18f : 2f) + (100 - gs.NeedFood) / 6f + (intent == Intents.Eat ? 25f : 0f);
            // Her club, in club time.
            if (!GirlInfo.InNoClub(h) && (mask == PeriodMask.Club || mask == PeriodMask.After)) w[6] = (mask == PeriodMask.Club ? 30f : 8f) + (intent == Intents.Club ? 25f : 0f);
            // Company: another girl (whom: her intention, see Intents).
            w[7] = 6f + (100 - gs.NeedSocial) / 8f + (intent == Intents.Friend || intent == Intents.Partner ? 20f : 0f);
            // Things to do on her own.
            w[9] = 5f; w[10] = 6f; w[11] = 4f; w[12] = 3f; w[13] = 5f; w[14] = 3f; w[15] = 4f; w[16] = 5f;
            w[17] = 4f + (intent == Intents.Rest ? 12f : 0f) + (intent == Intents.Hide || intent == Intents.Avoid ? 8f : 0f);
            w[18] = sporty ? 7f : 3f;
            w[19] = 4f;
            w[21] = 3f + (intent == Intents.Study ? 25f : 0f) + (Calendar.IsExamWeek ? 6f : 0f);
            w[22] = gs.NeedRest < 30 || intent == Intents.Rest ? 6f + (100 - gs.NeedRest) / 10f : 0f;
            switch (mood)
            {
                case Mood.Bored: w[10] += 4f; w[11] += 4f; break;
                case Mood.Cheerful: w[14] += 4f; w[12] += 3f; break;
                case Mood.Sleepy: w[22] += 6f; break;
                case Mood.Stressed: w[13] += 4f; w[17] += 4f; break;
                case Mood.Lonely: w[7] += 8f; break;
                case Mood.Hungry: w[3] += 15f; w[16] += 4f; break;
            }

            var max = Stage.Max; // a test runs the director at maximum weight: nothing spontaneous toward the player or H
            // The player: talking to him (8) when she is looking for him, asking for H (5) only when it really fits.
            if (!max && !Pairs.IsScorned(h))
            {
                if (intent == Intents.Player) w[8] = 10f + h.favor / 10f;
                if (h.lewdness >= 80 && (h.isGirlfriend || h.relation >= 2) && (mood == Mood.Flirty || h.lewdness >= 95)) w[5] = 2f;
            }
            // H on her own: the director allows it rarely, for a girl with a high lewdness.
            if (!max && Cfg.StudentH.Value && h.lewdness >= 60 && mask != PeriodMask.Lunch) w[4] = 0.5f + (h.lewdness - 60) / 20f + (mood == Mood.Flirty ? 1.5f : 0f);

            if (not != null) foreach (var n in not) w.Remove(n);
            return w.Where(kv => kv.Value > 0f).ToList();
        }
    }
}
