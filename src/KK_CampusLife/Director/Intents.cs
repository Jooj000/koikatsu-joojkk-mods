using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// What each student wants this period, from her needs (company, food, rest), her bonds and what is going on in her
    /// life: her club, a friend, her girlfriend, avoiding someone, looking for you, hiding, studying, eating, resting or
    /// some privacy with her girlfriend. The best three are weighed (softmax) and one is drawn each period. When the game
    /// picks her next action, a postfix leans it toward that intention (another map for the same action, a chosen friend
    /// as the one she walks up to, looking for you); anything that does not work keeps the game's own choice.
    /// </summary>
    internal static class Intents
    {
        private static CampusState S { get { return Campus.S; } }

        public const string Club = "club", Friend = "with", Partner = "partner", Avoid = "avoid", Player = "player", Hide = "hide",
            Study = "study", Eat = "eat", Rest = "rest", Privacy = "private";

        // Quiet places for resting or hiding, and the library for studying (map numbers).
        private static readonly int[] QuietMaps = { 36, 47, 28, 20 };
        private const int LibraryMap = 21, CafeteriaMap = 38;
        // The game's own actions (see aidump): 17 change of mood, 21 study, 22 sleep.
        private const int RestAction = 17, StudyAction = 21, SleepAction = 22;

        public static readonly Dictionary<string, int> Applied = new Dictionary<string, int>();
        public static readonly Dictionary<string, int> Seen = new Dictionary<string, int>();
        public static readonly List<string> Recent = new List<string>();

        private static int Stamp { get { return S.Day * 16 + (int)G.CurrentMask; } }

        // ================= needs and intentions (each period) =================

        /// <summary>New period: needs move, every student draws her intention.</summary>
        public static void OnPeriod()
        {
            if (!Cfg.DirectorEnabled.Value) return;
            var mask = G.CurrentMask;
            foreach (var h in Social.Students.ToList())
            {
                var gs = S.Girl(GirlInfo.Key(h));
                gs.NeedFood = Mathf.Clamp(gs.NeedFood - (mask == PeriodMask.Lunch ? 30 : 15), 0, 100);
                gs.NeedSocial = Mathf.Clamp(gs.NeedSocial - 10, 0, 100);
                gs.NeedRest = Mathf.Clamp(gs.NeedRest - (GirlInfo.InSportsClub(h) && mask == PeriodMask.Club ? 20 : 8), 0, 100);
                if (gs.Mood == Mood.Hungry) gs.NeedFood = Mathf.Min(gs.NeedFood, 25);
                if (gs.Mood == Mood.Lonely) gs.NeedSocial = Mathf.Min(gs.NeedSocial, 25);
                if (gs.Mood == Mood.Sleepy || gs.Mood == Mood.Stressed) gs.NeedRest = Mathf.Min(gs.NeedRest, 30);
                Choose(h, gs);
            }
        }

        /// <summary>Morning: rested, a little hungry, each one a bit differently (no breakfast, a late night, a quiet week).</summary>
        public static void OnDay()
        {
            foreach (var h in Social.Students.ToList())
            {
                var gs = S.Girl(GirlInfo.Key(h));
                gs.NeedRest = Mathf.Max(gs.NeedRest, Random.Range(60, 95));
                gs.NeedFood = Random.Range(40, 80);
                gs.NeedSocial = Mathf.Clamp(gs.NeedSocial + Random.Range(-15, 10), 0, 100);
            }
        }

        private sealed class Option { public string Id; public float Score; }

        /// <summary>Scores every intention and draws one among the best three (softmax).</summary>
        public static string Choose(SaveData.Heroine h, GirlState gs)
        {
            var opts = Score(h, gs);
            var top = opts.OrderByDescending(o => o.Score).Take(3).ToList();
            string pick = "";
            if (top.Count > 0)
            {
                var max = top[0].Score;
                var w = top.Select(o => Mathf.Exp((o.Score - max) / 0.8f)).ToList();
                var r = Random.value * w.Sum();
                for (var i = 0; i < top.Count; i++) { r -= w[i]; if (r <= 0f) { pick = top[i].Id; break; } }
                if (pick == "") pick = top[0].Id;
            }
            gs.Intent = pick;
            gs.IntentStamp = Stamp;
            return pick;
        }

        private static List<Option> Score(SaveData.Heroine h, GirlState gs)
        {
            var l = new List<Option>();
            var mask = G.CurrentMask;
            var q = GirlInfo.Quirks(h);
            Action<string, float> add = (id, sc) => { if (sc > 0f) l.Add(new Option { Id = id, Score = sc }); };

            // Club time is club time.
            if (!GirlInfo.InNoClub(h)) add(Club, 0.8f + (mask == PeriodMask.Club ? 4.5f : 0f));
            add(Eat, (100 - gs.NeedFood) / 30f + (mask == PeriodMask.Lunch ? 1.5f : 0f));
            add(Rest, (100 - gs.NeedRest) / 35f);
            if (q.majime || q.dokusyo || Calendar.IsExamWeek) add(Study, 0.9f + (q.majime ? 0.6f : 0f) + (q.dokusyo ? 0.6f : 0f) + (Calendar.IsExamWeek ? 1.2f : 0f));

            // A friend she wants to see.
            var lonely = Mathf.Min(2f, (100 - gs.NeedSocial) / 30f);
            // Popular friends are sought out more, outcasts less (Standing, 2026-10-27).
            var best = Social.FriendsOf(h, 35).Where(f => f != h && Truth.AtSchool(f)).OrderByDescending(f => Social.Bond(h, f) + (Standing.Of(f) - 50) / 4 + Random.Range(0, 20)).FirstOrDefault();
            if (best != null) add(Friend + ":" + GirlInfo.Key(best), 0.6f + lonely + Social.Bond(h, best) / 50f);
            // A popular girl she merely gets along with draws her in too.
            var star = Social.Students.Where(f => f != h && f != best && Truth.AtSchool(f) && Standing.Of(f) >= 70 && Social.Bond(h, f) >= 10)
                .OrderByDescending(f => Standing.Of(f) + Random.Range(0, 15)).FirstOrDefault();
            if (star != null) add(Friend + ":" + GirlInfo.Key(star), 0.3f + lonely * 0.6f + (Standing.Of(star) - 70) / 30f);

            // Her girlfriend, and some privacy with her.
            var p = Pairs.PairOf(h);
            if (p != null)
            {
                var k = p.Other(GirlInfo.Key(h));
                add(Partner + ":" + k, 1.4f + p.Intimacy / 40f + (p.Stage == PairStage.Strained ? -1f : 0f));
                if (Cfg.StudentH.Value && p.Stage == PairStage.Couple && p.Intimacy >= 35 && (mask == PeriodMask.After || mask == PeriodMask.Club))
                    add(Privacy + ":" + k, 0.4f + p.Intimacy / 45f);
            }
            else if (Cfg.StudentH.Value && Brain.Appetite(h) >= 45f && Pairs.GirlsOk(h) && (mask == PeriodMask.After || mask == PeriodMask.Club || mask == PeriodMask.Lunch))
            {
                // Lewd enough, no girlfriend: a willing girl she gets along with will do.
                var fling = LewdMatch(h);
                if (fling != null) add(Privacy + ":" + GirlInfo.Key(fling), Mathf.Min(1.5f, 0.08f + (Brain.Appetite(h) - 45f) / 60f));
            }

            // Someone she avoids (a recent breakup, a rival in a scandal).
            var sore = S.Pairs.Values.FirstOrDefault(x => (x.A == GirlInfo.Key(h) || x.B == GirlInfo.Key(h)) && x.Stage == PairStage.Broken && S.Day - x.StageDay <= 3);
            if (sore != null) add(Avoid + ":" + sore.Other(GirlInfo.Key(h)), 1.6f);

            // You: love, feeling unseen, being torn, wanting to tell you something. Only a girl who knows you goes looking
            // for you.
            if (!Pairs.IsScorned(h) && Social.KnowsYou(h))
            {
                var you = h.favor / 45f + (100 - gs.Attention) / 70f + (Pairs.IsConflicted(h) ? 1.2f : 0f) + (string.IsNullOrEmpty(gs.Pending) ? 0f : 1.2f);
                if (h.isGirlfriend) you += 0.8f;
                add(Player, you * 0.7f);
            }
            else if (Pairs.IsScorned(h)) add(Avoid + ":player", 2.4f);

            // Hiding: a rumor about her is hot.
            var heat = Rumors.HeatAbout(GirlInfo.Key(h));
            if (heat >= 40) add(Hide, heat / 30f);
            return l;
        }

        public static string Label(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return Label(gs.Intent);
        }

        public static string Label(string intent)
        {
            if (string.IsNullOrEmpty(intent)) return "nothing in particular";
            var parts = intent.Split(new[] { ':' }, 2); // keys hold ':' and '|' themselves
            var who = parts.Length > 1 ? (parts[1] == "player" ? "you" : GirlInfo.FirstName(parts[1])) : "";
            switch (parts[0])
            {
                case Club: return "club activities";
                case Friend: return "hanging out with " + who;
                case Partner: return "being with her girlfriend " + who;
                case Privacy: return "some time alone with " + who;
                case Avoid: return "avoiding " + who;
                case Player: return "looking for you";
                case Hide: return "keeping out of sight";
                case Study: return "studying";
                case Eat: return "getting something to eat";
                case Rest: return "taking a break";
                default: return intent;
            }
        }

        // ================= movement =================

        public static void Install(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(ActionControl), "NextAction", new[] { typeof(SaveData.Heroine), typeof(int[]) });
            if (target == null) CampusLifePlugin.Log.LogWarning("ActionControl.NextAction not found: students move as usual.");
            else harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(Intents), nameof(NextActionPostfix))));
            var les = AccessTools.Method(typeof(AI), "LesbianAction");
            if (les == null) CampusLifePlugin.Log.LogWarning("AI.LesbianAction not found: the game picks partners as usual.");
            else harmony.Patch(les, prefix: new HarmonyMethod(AccessTools.Method(typeof(Intents), nameof(LesbianPrefix))));
        }

        private static bool _inside;
        private static readonly Dictionary<string, int> _soughtPlayer = new Dictionary<string, int>();

        private static void Count(Dictionary<string, int> d, string id)
        {
            int n;
            d.TryGetValue(id, out n);
            d[id] = n + 1;
        }

        private static void Note(SaveData.Heroine h, string what)
        {
            Recent.Add(S.Day + "/" + G.CurrentMask + " " + GirlInfo.FirstName(h) + ": " + what);
            if (Recent.Count > 40) Recent.RemoveAt(0);
        }

        /// <summary>The game drew her next action: lean it toward her intention.</summary>
        private static void NextActionPostfix(ActionControl __instance, SaveData.Heroine _heroine, int[] _not, ref ActionControl.ResultInfo __result)
        {
            if (_inside || __result == null || _heroine == null) return;
            try
            {
                // The director's stage first: a girl it holds does what it says, whatever the game drew.
                _inside = true;
                if (Campus.On && Stage.Steer(__instance, _heroine, ref __result)) return;
                // Then the director's own draw replaces the game's (Brain); her intention is chosen first, it weighs in.
                if (Campus.On && Cfg.DirectorEnabled.Value && GirlInfo.IsRegular(_heroine))
                {
                    var gs0 = S.Girl(GirlInfo.Key(_heroine));
                    if (gs0.IntentStamp != Stamp) Choose(_heroine, gs0);
                }
                Brain.Decide(__instance, _heroine, _not, ref __result);
                _inside = false;
                if (!Campus.On || !Cfg.DirectorEnabled.Value || Cfg.IntentMovement.Value == MovementMode.Off || !GirlInfo.IsRegular(_heroine)) return;
                var gs = S.Girl(GirlInfo.Key(_heroine));
                if (gs.IntentStamp != Stamp) Choose(_heroine, gs);
                // Needs are met by what she actually does.
                if (__result.actionNo == 3) gs.NeedFood = 100;
                if (__result.actionNo == 7 || __result.actionNo == 8) gs.NeedSocial = Mathf.Min(100, gs.NeedSocial + 25);
                if (string.IsNullOrEmpty(gs.Intent) || __result.isPriority && __result.actionNo != 6) return;
                // The walk-and-talk girl and the girls of a staged scene are left alone.
                if (WalkTalk.Active && WalkTalk.Her == _heroine || Duo.Busy) return;
                var observe = Cfg.IntentMovement.Value == MovementMode.Observe;
                _inside = true;
                var changed = Lean(__instance, _heroine, gs, ref __result, observe);
                var kind = gs.Intent.Split(new[] { ':' }, 2)[0];
                Count(observe || !changed ? Seen : Applied, kind);
                if (changed) Note(_heroine, Label(gs.Intent) + (observe ? " (observed)" : "") + " -> action " + __result.actionNo + (__result.point != null ? " map " + __result.point.MapNo : __result.heroine != null ? " with " + GirlInfo.FirstName(__result.heroine) : ""));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Intents: " + e.Message); }
            finally
            {
                _inside = false;
                // the same place, walking into each other): a girl already in a chat or right next to one gets another
                // action instead.
                try
                {
                    if (Campus.On && __result != null && __result.actionNo == 7 && __result.heroine != null && !ChatFree(_heroine, __result.heroine))
                    {
                        _inside = true;
                        var alt = __instance.NextAction(_heroine, 7, 8);
                        if (alt != null) { Note(_heroine, "chat with " + GirlInfo.FirstName(__result.heroine) + " taken -> action " + alt.actionNo); __result = alt; }
                    }
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Intents chat guard: " + e.Message); }
                finally { _inside = false; }
            }
        }

        /// <summary>She can start a chat with the other girl: nobody else is talking to either of them, and no other chat
        /// is going on where the other girl stands.</summary>
        internal static bool ChatFree(SaveData.Heroine h, SaveData.Heroine other)
        {
            var act = G.Act;
            if (act == null || act.npcList == null) return true;
            NPC on = null;
            foreach (var n in act.npcList)
            {
                if (n == null || n.heroine == null || n.heroine == h) continue;
                if (n.heroine == other) { on = n; continue; }
                var r = n.AI != null ? n.AI.result : null;
                if (r != null && r.actionNo == 7 && (r.heroine == other || r.heroine == h)) return false;
            }
            if (on == null) return true;
            var or = on.AI != null ? on.AI.result : null;
            if (or != null && or.actionNo == 7 && or.heroine != null && or.heroine != h) return false;
            var pos = on.transform.position;
            foreach (var n in act.npcList)
                if (n != null && n != on && n.heroine != null && n.heroine != h && n.mapNo == on.mapNo && n.isTalking && (n.transform.position - pos).sqrMagnitude < 4f) return false;
            return true;
        }

        /// <summary>True when the result was (or, observing, would be) changed.</summary>
        private static bool Lean(ActionControl ac, SaveData.Heroine h, GirlState gs, ref ActionControl.ResultInfo r, bool observe)
        {
            var parts = gs.Intent.Split(new[] { ':' }, 2);
            var kind = parts[0];
            var who = parts.Length > 1 ? parts[1] : "";
            var playerMap = G.PlayerMapNo;
            switch (kind)
            {
                case Club:
                    if (r.actionNo == 6 || Random.value > 0.5f) return false;
                    return Swap(ac, h, ref r, 6, -1, observe);
                case Eat:
                    if (r.actionNo == 3) return false;
                    if (Random.value > 0.6f) return false;
                    if (Swap(ac, h, ref r, 3, CafeteriaMap, observe)) { gs.NeedFood = 100; return true; }
                    return false;
                case Study:
                    // The game's own "study" (21), in the library when it has a spot there.
                    if (r.actionNo == StudyAction || Random.value > 0.5f) return false;
                    return Swap(ac, h, ref r, StudyAction, LibraryMap, observe);
                case Rest:
                    // A break: the game's "change of mood" (17) or a nap (22), somewhere quiet.
                    if (r.actionNo == RestAction || r.actionNo == SleepAction || Random.value > 0.5f) return false;
                    if (Swap(ac, h, ref r, gs.NeedRest < 25 ? SleepAction : RestAction, QuietMaps[Random.Range(0, QuietMaps.Length)], observe)) { gs.NeedRest = Mathf.Min(100, gs.NeedRest + 30); return true; }
                    return false;
                case Hide:
                    if (r.point == null || r.point.MapNo != playerMap) return false;
                    return Move(ac, h, ref r, QuietMaps.Where(m => m != playerMap).ElementAt(Random.Range(0, QuietMaps.Length - 1)), observe);
                case Avoid:
                    {
                        var avoidMap = who == "player" ? playerMap : GirlInfo.CurrentMap(Social.Get(who));
                        if (r.heroine != null && GirlInfo.Key(r.heroine) == who)
                        {
                            if (observe) return true;
                            r.heroine = null; // she will not walk up to her: the game's wait instead
                            _inside = true;
                            var again = ac.NextAction(h, 7, 8);
                            if (again != null) r = again;
                            return true;
                        }
                        if (r.point != null && avoidMap >= 0 && r.point.MapNo == avoidMap)
                            return Move(ac, h, ref r, QuietMaps.Where(m => m != avoidMap).ElementAt(Random.Range(0, QuietMaps.Length - 1)), observe);
                        if (who == "player" && r.actionNo == 8) { if (observe) return true; var again = ac.NextAction(h, 8, 5); if (again != null) { r = again; return true; } }
                        return false;
                    }
                case Friend:
                case Partner:
                    {
                        var other = Social.Get(who);
                        if (other == null || !GirlInfo.IsSpawned(other)) return false;
                        var otherNpc = NpcOf(other);
                        if (otherNpc == null || otherNpc.AI == null || otherNpc.AI.result != null && otherNpc.AI.result.isPriority) return false;
                        if (r.actionNo == 7)
                        {
                            if (r.heroine == other) return false;
                            if (!observe) r.heroine = other;
                            Pairs.NoteTogether(h, other);
                            return true;
                        }
                        // Sometimes she goes looking for her on purpose (talk to another student, with her as the target).
                        if (Random.value < (kind == Partner ? 0.45f : 0.3f))
                        {
                            if (observe) return true;
                            var res = ac.SetAction(h, 7, -1);
                            if (res == null) return false;
                            res.heroine = other;
                            r = res;
                            Pairs.NoteTogether(h, other);
                            return true;
                        }
                        // Or she does her thing on the same map as her.
                        var m = GirlInfo.CurrentMap(other);
                        return r.point != null && m >= 0 && r.point.MapNo != m && Random.value < 0.4f && Move(ac, h, ref r, m, observe);
                    }
                case Privacy:
                    {
                        if (Stage.Max) return false; // a test: nothing spontaneous
                        // Two girlfriends slip away: the game's own scene between two girls (action 26, the partner joins).
                        var other = Social.Get(who);
                        if (other == null || !GirlInfo.IsSpawned(other) || r.actionNo == 26) return false;
                        var p = Pairs.Of(h, other);
                        if (p == null || Random.value > 0.25f + p.Intimacy / 400f) return false;
                        if (observe) return true;
                        // The partner must count as willing for the game (its own rule: "likes girls" or desire 26 >= 80).
                        if (ac.GetDesire(26, other) < 80) ac.SetDesire(26, other, 85);
                        // The director picks a quiet spot (the game drops it wherever people are around).
                        var res = Stage.QuietSpot(ac, h, 26, other) ?? ac.SetAction(h, 26, -1);
                        if (res == null) return false;
                        Arrange(h, other); // the game would take the first willing girl it finds
                        r = res;
                        p.Intimacy = PairState.Clamp(p.Intimacy + 3);
                        return true;
                    }
                case Player:
                    {
                        if (Stage.Max) return false; // a test: nobody comes to the player on her own
                        if (r.actionNo == 8 || !G.IsFreeRoaming || WalkTalk.Active || Scenes.Running) return false;
                        // One girl at a time comes looking for you, each at most once a period.
                        var key = GirlInfo.Key(h);
                        int st;
                        if (_soughtPlayer.TryGetValue(key, out st) && st == Stamp) return false;
                        if (_soughtPlayer.Count(kv => kv.Value == Stamp) >= 2) return false;
                        if (Random.value > 0.35f) return false;
                        if (observe) return true;
                        var res = ac.SetAction(h, 8, -1);
                        if (res == null) return false;
                        _soughtPlayer[key] = Stamp;
                        r = res;
                        return true;
                    }
            }
            return false;
        }

        /// <summary>Another action (and map) for her; null from the game keeps the original.</summary>
        private static bool Swap(ActionControl ac, SaveData.Heroine h, ref ActionControl.ResultInfo r, int action, int map, bool observe)
        {
            if (observe) return true;
            var res = ac.SetAction(h, action, map);
            if (res == null) return false;
            r = res;
            return true;
        }

        /// <summary>The same action somewhere else (only kept if the game found a spot on that map).</summary>
        private static bool Move(ActionControl ac, SaveData.Heroine h, ref ActionControl.ResultInfo r, int map, bool observe)
        {
            if (observe) return true;
            var res = ac.SetAction(h, r.actionNo, map);
            if (res == null || res.point == null || res.point.MapNo != map || Brain.SpotTaken(res.point, h) != null)
            {
                // Put the original spot back (SetAction released it).
                var back = ac.SetAction(h, r.actionNo, r.point != null ? r.point.MapNo : -1);
                if (back != null) r = back;
                return false;
            }
            r = res;
            return true;
        }

        private static NPC NpcOf(SaveData.Heroine h)
        {
            return h == null || G.Act == null ? null : G.Act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
        }

        // ================= student H =================

        /// <summary>
        /// A girl asks another girl for the game's scene between two girls: a girl with a girlfriend asks her girlfriend
        /// (if she is free); a girl who would ask someone else's girlfriend asks someone free instead when there is one.
        /// </summary>
        private static readonly Dictionary<SaveData.Heroine, SaveData.Heroine> _arranged = new Dictionary<SaveData.Heroine, SaveData.Heroine>();

        /// <summary>She waits at her spot (26): the arranged partner comes now (the game's own call, with the director's pick).</summary>
        public static void CallPartner(NPC me, NPC partner)
        {
            if (me == null || partner == null || me.AI == null) return;
            Arrange(me.heroine, partner.heroine);
            if (G.Act.actCtrl.GetDesire(26, partner.heroine) < 80) G.Act.actCtrl.SetDesire(26, partner.heroine, 85);
            me.AI.LesbianAction(partner);
        }

        /// <summary>A girl at school with no girlfriend, open to girls, lewd too and not on bad terms with her: a fling
        /// without dating (user, 2026-10-26: "podem fazer H entre si com lascívia alta mesmo sem namorarem").</summary>
        public static SaveData.Heroine LewdMatch(SaveData.Heroine h)
        {
            return Social.Students.Where(o => o != h && GirlInfo.IsSpawned(o) && Pairs.PartnerOf(o) == null && Brain.Appetite(o) >= 35f && Pairs.GirlsOk(o) && Social.Bond(h, o) >= 10)
                .OrderByDescending(o => o.lewdness + Social.Bond(h, o) + Random.Range(0, 30)).FirstOrDefault();
        }

        /// <summary>The director picks who joins her the next time she starts the game's scene between two girls (26).</summary>
        public static void Arrange(SaveData.Heroine h, SaveData.Heroine partner)
        {
            if (h == null || partner == null) return;
            _arranged[h] = partner;
        }

        private static void LesbianPrefix(AI __instance, ref NPC target)
        {
            try
            {
                if (!Campus.On || G.Act == null) return;
                var me = G.Act.npcList.FirstOrDefault(n => n != null && n.AI == __instance);
                if (me == null || me.heroine == null) return;
                // A partner the director arranged comes first (the game itself takes the first willing girl it finds).
                SaveData.Heroine arranged;
                if (_arranged.TryGetValue(me.heroine, out arranged))
                {
                    _arranged.Remove(me.heroine);
                    var an = NpcOf(arranged);
                    if (an != null && an != me && an.AI != null)
                    {
                        if (target != an) CampusLifePlugin.Log.LogInfo("Student H: " + GirlInfo.FirstName(me.heroine) + " with " + GirlInfo.FirstName(arranged) + " (the director's choice" + (target != null ? ", not " + GirlInfo.FirstName(target.heroine) : "") + ")");
                        target = an;
                        return;
                    }
                }
                // A girl the director holds on its stage is not someone else's partner.
                if (target != null && Stage.Holds(target.heroine))
                {
                    var spare = G.Act.npcList.FirstOrDefault(x => x != null && x != me && x.heroine != null && !Stage.Holds(x.heroine) && x.AI != null && x.AI.result != null && !x.AI.result.isPriority && x.AI.isTalkSuccess && !x.AI.isPlayerResWaiting && (x.heroine.parameter.attribute.likeGirls || G.Act.actCtrl.GetDesire(26, x.heroine) >= 80));
                    if (spare != null) { CampusLifePlugin.Log.LogInfo("Student H: " + GirlInfo.FirstName(target.heroine) + " is on the director's stage; " + GirlInfo.FirstName(me.heroine) + " asks " + GirlInfo.FirstName(spare.heroine)); target = spare; }
                }
                if (!Cfg.DirectorEnabled.Value || !Cfg.StudentH.Value || target == null) return;
                var partner = Pairs.PartnerOf(me.heroine);
                var tgt = target;
                Func<NPC, bool> free = n => n != null && n != me && n.heroine != null && n.AI != null && n.AI.result != null && !n.AI.result.isPriority && n.AI.isTalkSuccess && !n.AI.isPlayerResWaiting;
                if (partner != null)
                {
                    var pn = NpcOf(partner);
                    if (pn != null && free(pn) && pn != tgt) { target = pn; CampusLifePlugin.Log.LogInfo("Student H: " + GirlInfo.FirstName(me.heroine) + " asks her girlfriend " + GirlInfo.FirstName(partner)); }
                }
                else
                {
                    var theirs = Pairs.PartnerOf(tgt.heroine);
                    if (theirs != null && theirs != me.heroine)
                    {
                        var other = G.Act.npcList.FirstOrDefault(n => free(n) && n != tgt && Pairs.PartnerOf(n.heroine) == null && (n.heroine.parameter.attribute.likeGirls || G.Act.actCtrl.GetDesire(26, n.heroine) >= 80));
                        if (other != null) target = other;
                        else
                        {
                            // Nobody else: it happens, and her girlfriend may hear of it.
                            var p = Pairs.PairOf(tgt.heroine);
                            if (p != null) { p.Trust = PairState.Clamp(p.Trust - 8); p.Jealousy = PairState.Clamp(p.Jealousy + 12); }
                            if (Random.value < 0.4f) { Rumors.Start("caught", GirlInfo.Key(tgt.heroine), GirlInfo.Key(me.heroine), GirlInfo.Key(theirs), 45); Standing.Add(tgt.heroine, -4, "caught cheating"); }
                        }
                    }
                }
                var pr = Pairs.Of(me.heroine, target.heroine);
                if (pr != null)
                {
                    pr.Intimacy = PairState.Clamp(pr.Intimacy + 4);
                    pr.Exposure = PairState.Clamp(pr.Exposure + 2);
                    if (pr.Stage == PairStage.None || pr.Stage == PairStage.Crush) { pr.AddRomance(pr.A, 4); pr.AddRomance(pr.B, 4); Pairs.Advance(pr); }
                }
                // Between them, like with you: closer afterwards, and the urge is spent for a while.
                Arcs.AddBond(GirlInfo.Key(me.heroine), GirlInfo.Key(target.heroine), 4);
                G.AddFavor(me.heroine, 0, -15);
                G.AddFavor(target.heroine, 0, -15);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Lesbian prefix: " + e.Message); }
        }

        /// <summary>Each period: girls with no girlfriend do not count as "willing" for the game by desire alone if they are
        /// in someone else's couple; couples who are close get the wish now and then.</summary>
        public static void TuneDesires()
        {
            if (!Cfg.DirectorEnabled.Value || !Cfg.StudentH.Value || G.Act == null || G.Act.actCtrl == null) return;
            var ac = G.Act.actCtrl;
            foreach (var p in Pairs.Couples)
            {
                var a = Social.Get(p.A);
                var b = Social.Get(p.B);
                if (a == null || b == null || p.Stage != PairStage.Couple) continue;
                if (p.Intimacy >= 30 && Random.value < p.Intimacy / 250f)
                    foreach (var h in new[] { a, b }) ac.AddDesire(26, h, 10 + p.Intimacy / 10);
            }
        }
    }
}
