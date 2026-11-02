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
    /// The director's stage: the girls it holds stay where it wants them (the player's map), whatever the game's AI
    /// draws. User, 2026-10-11: "the director controls 100% of the AI, so control the director": tests put the director
    /// at maximum weight instead of calling girls who walk off again a minute later.
    /// <list type="bullet">
    /// <item>A held girl: every action the game draws for her (NextAction) becomes a calm one on the player's map
    /// (reading, phone, music, a drink, talking with another held girl). When the player changes map she walks after
    /// him. What she is doing on purpose goes on first: H, running off, getting dressed, a staged scene.</item>
    /// <item><see cref="Max"/> (tests): nobody else walks up to the player either (the game's "talk to him", "H with
    /// him", "come with me", "hey!", the shy wait).</item>
    /// </list>
    /// </summary>
    internal static class Stage
    {
        /// <summary>Tests: the director's word is final; no girl comes to the player on her own.</summary>
        public static bool Max;

        private static readonly List<SaveData.Heroine> _held = new List<SaveData.Heroine>();
        private static readonly Dictionary<SaveData.Heroine, float> _lastPush = new Dictionary<SaveData.Heroine, float>();
        private static readonly Dictionary<SaveData.Heroine, float> _lastWarp = new Dictionary<SaveData.Heroine, float>();
        public static readonly List<string> Recent = new List<string>();

        /// <summary>Calm things to do near the player, in order of preference.</summary>
        private static readonly int[] Calm = { 10, 13, 9, 16, 15, 12, 11, 21, 18, 17 };
        /// <summary>The game's actions that go to the player.</summary>
        private static readonly int[] ToPlayer = { 5, 8, 23, 24, 25, 28, 29, 30 };
        /// <summary>
        /// What she is doing on purpose, not interrupted: H, running off, getting dressed, waiting for the player's answer.
        /// went to eat and never came).
        /// </summary>
        private static readonly int[] OnPurpose = { 0, 4, 20, 26, 27, 28, 29, 30 };

        public static IList<SaveData.Heroine> Held { get { return _held; } }
        public static bool Holds(SaveData.Heroine h) { return h != null && _held.Contains(h); }

        /// <summary>These girls (and only these) are on stage now.</summary>
        public static void Set(IEnumerable<SaveData.Heroine> girls)
        {
            _held.Clear();
            Keep(girls);
        }

        public static void Keep(IEnumerable<SaveData.Heroine> girls)
        {
            foreach (var h in girls.Where(x => x != null && !_held.Contains(x))) { _held.Add(h); _lastPush.Remove(h); Note(GirlInfo.FirstName(h) + " held"); }
        }

        public static void Release(SaveData.Heroine h) { if (_held.Remove(h)) Note(GirlInfo.FirstName(h) + " released"); }

        public static void Clear() { _held.Clear(); _lastPush.Clear(); _lastWarp.Clear(); _why.Clear(); Max = false; }

        private static void Note(string s)
        {
            Recent.Add(Campus.S.Day + "/" + G.CurrentMask + " " + s);
            if (Recent.Count > 40) Recent.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Stage: " + s);
        }

        /// <summary>
        /// From the NextAction postfix (Intents), before anything else: the game drew her next action. True when the
        /// director decided it (nothing else leans it then).
        /// </summary>
        public static bool Steer(ActionControl ac, SaveData.Heroine h, ref ActionControl.ResultInfo r)
        {
            if (h == null || r == null || ac == null) return false;
            // Stopped by the director: what the game draws for her becomes something calm where she
            // stands; she is not walking anywhere until the director lets her go.
            if (Director.Halted(h))
            {
                var here = GirlInfo.CurrentMap(h);
                if (here >= 0 && !IsCalmHere(r, here)) { var calm = CalmOn(ac, h, here); if (calm != null && calm.actionNo != 8) r = calm; }
                return true;
            }
            if (Holds(h))
            {
                if (Duo.Busy || WalkTalk.Active && WalkTalk.Her == h) return true;
                var map = G.PlayerMapNo;
                if (map < 0) return true;
                if (IsCalmHere(r, map)) return true;
                var res = CalmOn(ac, h, map);
                if (res != null) r = res;
                return true;
            }
            // With the director's brain on, nobody draws 5/8/... at maximum weight anyway (Brain); this is for when it is off.
            if (Max && !Cfg.FullControl.Value && Array.IndexOf(ToPlayer, r.actionNo) >= 0)
            {
                // Nobody else walks up to the player while the director runs the test.
                var again = ac.NextAction(h, ToPlayer);
                if (again != null) r = again;
                return true;
            }
            return false;
        }

        private static bool IsCalmHere(ActionControl.ResultInfo r, int map)
        {
            // -1 is the game's "wait around" (CreateWaitAction: no spot for the calm action asked for), fine too.
            if (Array.IndexOf(Calm, r.actionNo) >= 0 || r.actionNo == -1) return r.point != null && r.point.MapNo == map;
            // Talking with another held girl who is here too.
            return r.actionNo == 7 && r.heroine != null && Holds(r.heroine) && GirlInfo.CurrentMap(r.heroine) == map;
        }

        /// <summary>A calm action for her on this map (null: the game has no spot here for any of them).</summary>
        private static ActionControl.ResultInfo CalmOn(ActionControl ac, SaveData.Heroine h, int map)
        {
            // Sometimes she chats with another held girl who is already here.
            var friend = _held.FirstOrDefault(o => o != h && GirlInfo.CurrentMap(o) == map && GirlInfo.CurrentMap(h) == map);
            if (friend != null && Random.value < 0.3f)
            {
                var talk = ac.SetAction(h, 7, -1);
                if (talk != null) { talk.heroine = friend; return talk; }
            }
            var start = Random.Range(0, 4); // a little variety among the first ones
            for (var i = 0; i < Calm.Length; i++)
            {
                var a = Calm[(i + (i < 4 ? start : 0)) % Calm.Length];
                var res = ac.SetAction(h, a, map);
                if (res != null && res.point != null && res.point.MapNo == map && Brain.SpotTaken(res.point, h) == null) return res;
            }
            // She is on another map and none of them has a spot here (the game then offers a wait on HER map): she comes
            // to the player (8); once she is on this map the tick gives her a calm thing to do before she reaches him.
            if (GirlInfo.CurrentMap(h) != map) return ac.SetAction(h, 8, -1);
            return null;
        }

        /// <summary>
        /// The game's own summon move (<c>ActionMap.PlayerMapNearWarp</c>), toward any map: she appears at the door of that
        /// map on her way from where she is, and walks the rest. Only at maximum weight (tests); in play girls walk.
        /// </summary>
        public static bool BringTo(NPC n, int map)
        {
            var act = G.Act;
            if (n == null || act == null || act.Map == null || map < 0 || n.mapNo == map) return false;
            try
            {
                var am = act.Map;
                var gate = am.NearGate(n.mapNo, map, null);
                var from = n.mapNo;
                // NearGate gives the last door of her route, on the map BEFORE the destination (the game's summon leaves her
                // door on that side (the gate's link).
                if (gate != null && am.gateInfoDic.ContainsKey(gate.linkID)) { n.mapNo = map; n.position = am.gateInfoDic[gate.linkID].pos; }
                else
                {
                    var g2 = am.calcGateDic[map][Random.Range(0, am.calcGateDic[map].Count)];
                    n.mapNo = map;
                    n.position = g2.pos;
                }
                Note(GirlInfo.FirstName(n.heroine) + " brought to the door of map " + map + " (from " + from + ", now " + n.mapNo + ")");
                OutOfSwimsuit(act, n, map);
                return true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Stage bring: " + e.Message); return false; }
        }

        /// <summary>
        /// The game keeps a girl in a swimsuit off most maps (<c>swimsuitBanArea</c>); one brought here from the pool skipped
        /// that check and stayed dressed for the pool.
        /// </summary>
        private static void OutOfSwimsuit(ActionScene act, NPC n, int map)
        {
            try
            {
                var c = n.chaCtrl;
                if (c == null || c.fileStatus == null) return;
                var type = (ChaFileDefine.CoordinateType)c.fileStatus.coordinateType;
                var swim = c.GetNowClothesType() == 2 && (type == ChaFileDefine.CoordinateType.Swim || type == ChaFileDefine.CoordinateType.Club && n.heroine.clubActivities == 1);
                if (!swim) return;
                var ban = Traverse.Create(act.actCtrl).Property("swimsuitBanArea").GetValue<HashSet<int>>();
                if (ban == null || !ban.Contains(map)) return;
                c.ChangeCoordinateTypeAndReload(ChaFileDefine.CoordinateType.School01);
                Note(GirlInfo.FirstName(n.heroine) + " changed out of her swimsuit for map " + map);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Stage swimsuit: " + e.Message); }
        }

        /// <summary>
        /// The director picks where an H happens: the game drops H on the player's map and wherever other people are
        /// around.
        /// Several spots are drawn and the emptiest map wins (never the player's); then that one is set.
        /// </summary>
        public static ActionControl.ResultInfo QuietSpot(ActionControl ac, SaveData.Heroine h, int action, params SaveData.Heroine[] with)
        {
            var act = G.Act;
            if (ac == null || act == null) return null;
            var player = G.PlayerMapNo;
            var best = -1;
            var bestScore = int.MaxValue;
            var tried = new HashSet<int>();
            for (var i = 0; i < 10; i++)
            {
                var r = ac.SetAction(h, action, -1);
                if (r == null || r.point == null || !tried.Add(r.point.MapNo)) continue;
                var m = r.point.MapNo;
                var people = act.npcList.Count(x => x != null && x.heroine != h && !with.Contains(x.heroine) && x.mapNo == m);
                var score = people * 10 + (m == player ? 1000 : 0) + (m == GirlInfo.CurrentMap(h) ? 0 : 3);
                if (score < bestScore) { bestScore = score; best = m; }
            }
            if (best < 0) return null;
            var res = ac.SetAction(h, action, best);
            if (res != null && res.point != null) Note(GirlInfo.FirstName(h) + ": H spot on map " + best + " (" + bestScore / 10 + " people there)");
            return res;
        }

        private static float _next;
        private static readonly Dictionary<SaveData.Heroine, string> _why = new Dictionary<SaveData.Heroine, string>();

        /// <summary>What the tick does about her now (null: it pushes her), logged when it changes.</summary>
        private static string Skip(ActionScene act, SaveData.Heroine h, NPC n, int map)
        {
            if (n == null) return "no NPC";
            if (n.AI == null) return "no AI";
            if (!n.AI.isRunning) return "AI not running";
            if (n.AI.isPaused) return "AI paused";
            if (WalkTalk.Active && WalkTalk.Her == h) return "walk-and-talk";
            if (n.isOnanism || n.isLesbian) return "in H";
            var cur = n.AI.result;
            // At maximum weight a held girl does not leave for an errand on another map.
            var leaving = Max && cur != null && cur.actionNo == 0 && cur.point != null && cur.point.MapNo != map;
            if (cur != null && !leaving && Array.IndexOf(OnPurpose, cur.actionNo) >= 0) return "busy on purpose (action " + cur.actionNo + ")";
            if (cur != null && IsCalmHere(cur, map)) return "calm here (action " + cur.actionNo + ")";
            if (cur != null && cur.actionNo == 8 && n.mapNo != map) return "on her way over (map " + n.mapNo + ")";
            return null;
        }

        private static void Why(SaveData.Heroine h, string why)
        {
            string last;
            if (_why.TryGetValue(h, out last) && last == why) return;
            _why[h] = why;
            Note(GirlInfo.FirstName(h) + ": " + why);
        }

        public static string Report()
        {
            var act = G.Act;
            if (act == null) return "no scene";
            return string.Join(" | ", _held.Select(h =>
            {
                var n = act.npcList.FirstOrDefault(x => x != null && x.heroine == h);
                var cur = n != null && n.AI != null ? n.AI.result : null;
                return GirlInfo.FirstName(h) + " map " + (n != null ? n.mapNo : -1) + " action " + (cur != null ? cur.actionNo + (cur.point != null ? "@" + cur.point.MapNo : "") : "-") + " (" + (Skip(act, h, n, G.PlayerMapNo) ?? "would push") + ")";
            }).ToArray());
        }

        private static string _globalWhy;

        /// <summary>Every frame from the plugin; works twice a second. Held girls who are elsewhere walk to the player.</summary>
        public static void Tick()
        {
            if (_held.Count == 0 || Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.5f;
            try
            {
                var act = G.Act;
                string g = act == null || act.Player == null ? "no scene" : !G.IsFreeRoaming ? "not free roaming" : Scenes.Running ? "our scene playing" : G.TalkSceneOpen ? "talk scene open" : Duo.Busy ? "a duo is busy" : null;
                if (g != _globalWhy) { _globalWhy = g; Note("tick " + (g ?? "running")); }
                if (g != null) return;
                var map = G.PlayerMapNo;
                foreach (var h in _held.ToList())
                {
                    var n = act.npcList.FirstOrDefault(x => x != null && x.heroine == h);
                    // The game only switches bodies on when the map changes (ActionMap, IsPop): one we put through a door
                    // has the right map number and stays off, unseen and out of the scenes.
                    if (n != null && !n.isActive && act.Map.IsPop(n)) n.SetActive(true);
                    var skip = Skip(act, h, n, map);
                    if (skip != null) { Why(h, skip); continue; }
                    float last;
                    if (_lastPush.TryGetValue(h, out last) && Time.realtimeSinceStartup - last < 4f) continue;
                    _lastPush[h] = Time.realtimeSinceStartup;
                    // At maximum weight she does not cross the whole school: the game's own summon puts her behind the
                    float warped;
                    if (Max && n.mapNo != map && (!_lastWarp.TryGetValue(h, out warped) || Time.realtimeSinceStartup - warped > 20f))
                    {
                        _lastWarp[h] = Time.realtimeSinceStartup;
                        BringTo(n, map);
                    }
                    var res = CalmOn(act.actCtrl, h, map);
                    if (res == null) { Why(h, "no spot for her on map " + map); continue; }
                    Traverse.Create(n.AI).Field("actResult").Property("Value").SetValue(res);
                    Why(h, (n.mapNo == map ? "stays here" : "walks over from map " + n.mapNo) + ": action " + res.actionNo + " on map " + (res.point != null ? res.point.MapNo : -1));
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Stage: " + e); }
        }
    }
}

