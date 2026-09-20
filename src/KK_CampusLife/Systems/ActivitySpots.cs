using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using HarmonyLib;
using IllusionUtility.GetUtility;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using UnityEngine.AI;

namespace Joojkk.CampusLife
{
    /// <summary>One of the game's player actions (Manager.PlayerAction.ActionInfo, read by reflection).</summary>
    internal sealed class GameAction
    {
        public int No;
        public string State;
        public bool IsDark;
        public string Target;
        public Vector3 Move, Rot;
        public readonly List<string[]> Items = new List<string[]>();
    }

    /// <summary>One of the game's action points (the STR/INT/SEN spots), with every action it can play.</summary>
    internal sealed class GamePoint
    {
        public int No;
        public Vector3 Pos;
        public Quaternion Rot;
        public int[] Actions;
    }

    /// <summary>
    /// Physical activity spots. Every activity of a place is done at a real spot on that map: by default next to one of
    /// the game's own action points (the study desk, the training corner...), or wherever the player put it with the
    /// position tools. Each spot has an action icon; using it (or "Do it" in the menu) walks you there and plays the
    /// game's own animation for it (studying at the desk, squats, reading...) or, for chores and jobs, the game's
    /// black-screen narration. Never uses the game's daily action count.
    /// </summary>
    internal static class ActivitySpots
    {
        private const int Desk = -2;   // the map's study desk (an action with a chair), or reading standing up
        private const int Dark = -1;   // black screen with the result text, like the game's "isDark" actions

        public enum Kind { Int, Str, Sens, Job, Rest }

        /// <summary>Animation (game action number, Desk or Dark) and icon kind of every activity.</summary>
        private static readonly Dictionary<string, KeyValuePair<int, Kind>> Look = new Dictionary<string, KeyValuePair<int, Kind>>
        {
            { "review_notes", P(Desk, Kind.Int) }, { "class_duty", P(Dark, Kind.Str) },
            { "study", P(Desk, Kind.Int) }, { "romance_novel", P(0, Kind.Sens) }, { "browse_magazines", P(0, Kind.Int) }, { "library_job", P(Dark, Kind.Job) },
            { "workout", P(3, Kind.Str) }, { "basketball", P(Dark, Kind.Str) }, { "gym_job", P(Dark, Kind.Job) },
            { "swim", P(Dark, Kind.Str) }, { "lifeguard", P(Dark, Kind.Job) },
            { "run", P(4, Kind.Str) }, { "garden", P(Dark, Kind.Job) }, { "feed_cats", P(Dark, Kind.Rest) }, { "park_walk", P(Dark, Kind.Rest) },
            { "hearty_lunch", P(Dark, Kind.Rest) }, { "kitchen_job", P(Dark, Kind.Job) },
            { "club_meeting", P(Desk, Kind.Int) }, { "club_report", P(Desk, Kind.Int) }, { "research_material", P(6, Kind.Sens) }, { "fundraise", P(Dark, Kind.Job) },
            { "nap", P(Dark, Kind.Rest) }, { "practice_lines", P(2, Kind.Sens) }, { "stargaze", P(Dark, Kind.Sens) },
            { "rest", P(Dark, Kind.Rest) }, { "nurse_job", P(Dark, Kind.Job) },
            { "tutoring", P(Desk, Kind.Int) }, { "teacher_errands", P(Dark, Kind.Job) },
        };

        private static KeyValuePair<int, Kind> P(int anim, Kind kind) { return new KeyValuePair<int, Kind>(anim, kind); }

        public static Kind KindOf(ActivityDef a)
        {
            KeyValuePair<int, Kind> l;
            if (Look.TryGetValue(a.Id, out l)) return l.Value;
            return a.IsJob ? Kind.Job : Kind.Int;
        }

        private static int AnimOf(ActivityDef a)
        {
            KeyValuePair<int, Kind> l;
            return Look.TryGetValue(a.Id, out l) ? l.Key : Dark;
        }

        /// <summary>Where an activity is done on the current map, and how.</summary>
        internal sealed class Anchor
        {
            public string Key;             // "p<point no>" or "c<activity id>"
            public Vector3 Pos;            // where the player stands for the animation
            public Quaternion Rot;
            public Vector3 IconPos;
            public bool Custom;
            public readonly List<ActivityDef> Acts = new List<ActivityDef>();
            public readonly Dictionary<string, int> AnimFor = new Dictionary<string, int>();
        }

        private static readonly Dictionary<int, List<GamePoint>> PointsByMap = new Dictionary<int, List<GamePoint>>();
        private static readonly Dictionary<int, List<Anchor>> AnchorsByMap = new Dictionary<int, List<Anchor>>();
        private static readonly Dictionary<int, List<IDisposable>> Icons = new Dictionary<int, List<IDisposable>>();
        private static Dictionary<int, GameAction> _actions;

        public static bool Busy { get; private set; }

        private static bool _own;

        /// <summary>Busy for anyone but the running activity itself (Activities.Blocker).</summary>
        public static bool BlocksOthers { get { return Busy && !_own; } }

        private static string OwnBlocker(ActivityDef a)
        {
            _own = true;
            try { return Activities.Blocker(Campus.S, a); }
            finally { _own = false; }
        }

        /// <summary>The activity's result.</summary>
        private static ActivityResult OwnDo(ActivityDef a)
        {
            _own = true;
            try { return Campus.DoActivity(a); }
            finally { _own = false; }
        }

        public static bool On { get { return Cfg.ActivitiesEnabled.Value && Cfg.ActivitySpots.Value; } }

        // ---------------- game data ----------------

        public static void Install(Harmony harmony)
        {
            var target = AccessTools.Method(typeof(PlayerAction), "LoadPoint");
            if (target == null) { CampusLifePlugin.Log.LogWarning("PlayerAction.LoadPoint not found: activity spots stay off."); return; }
            harmony.Patch(target, postfix: new HarmonyMethod(AccessTools.Method(typeof(ActivitySpots), nameof(LoadPointPostfix))));
        }

        /// <summary>The map's action points were just instantiated (all of them; the ones not active this period remove
        /// themselves on their first frame): remember where they are, then place our icons for this map.</summary>
        private static void LoadPointPostfix(PlayerAction __instance, int _mapNo)
        {
            try
            {
                var map = _mapNo != -1 ? _mapNo : (G.Act != null && G.Act.Map != null ? G.Act.Map.no : -1);
                if (map < 0) return;
                var root = Traverse.Create(__instance).Field("objPoint").GetValue<GameObject>();
                // Only remember a map's points once they exist (the game may not have loaded them yet on the first visit).
                if (!PointsByMap.ContainsKey(map) && root != null)
                {
                    var info = (IDictionary)Traverse.Create(__instance).Field("dicPointInfo").GetValue();
                    var list = new List<GamePoint>();
                    if (root != null)
                        foreach (var ap in root.GetComponentsInChildren<PlayerActionPoint>(true))
                        {
                            var no = Traverse.Create(ap).Field("no").GetValue<int>();
                            var pi = info != null && info.Contains(no) ? info[no] : null;
                            var acts = pi != null ? Traverse.Create(pi).Property("action").GetValue<int[]>() : null;
                            list.Add(new GamePoint { No = no, Pos = ap.transform.position, Rot = ap.transform.rotation, Actions = acts ?? new int[0] });
                        }
                    if (list.Count > 0) PointsByMap[map] = list;
                }
                // KKAPI spawns the icons registered for this map right after this (ActionMap.Reserve postfix).
                Register(map, false);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Activity spots: " + e); }
        }

        private static Dictionary<int, GameAction> Actions
        {
            get
            {
                if (_actions != null) return _actions;
                var result = new Dictionary<int, GameAction>();
                try
                {
                    if (!Singleton<PlayerAction>.IsInstance() || !Singleton<PlayerAction>.Instance.isInit) return result;
                    var dic = (IDictionary)Traverse.Create(Singleton<PlayerAction>.Instance).Field("dicActionInfo").GetValue();
                    foreach (DictionaryEntry e in dic)
                    {
                        var t = Traverse.Create(e.Value);
                        var ga = new GameAction
                        {
                            No = (int)e.Key,
                            State = t.Property("state").GetValue<string>(),
                            IsDark = t.Property("isDark").GetValue<bool>(),
                            Target = t.Property("target").GetValue<string>(),
                            Move = t.Property("move").GetValue<Vector3>(),
                            Rot = t.Property("rot").GetValue<Vector3>(),
                        };
                        var items = t.Property("items").GetValue() as IList;
                        if (items != null)
                            foreach (var it in items)
                            {
                                var ti = Traverse.Create(it);
                                ga.Items.Add(new[] { ti.Property("bundle").GetValue<string>(), ti.Property("file").GetValue<string>(), ti.Property("manifest").GetValue<string>() });
                            }
                        result[ga.No] = ga;
                    }
                    _actions = result;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError("Activity spots: could not read the game's actions: " + e); }
                return result;
            }
        }

        private static GameAction ActionNo(int no)
        {
            GameAction ga;
            return no >= 0 && Actions.TryGetValue(no, out ga) ? ga : null;
        }

        private static bool IsDeskAction(int no)
        {
            var ga = ActionNo(no);
            return ga != null && ga.State == "Study2" && !string.IsNullOrEmpty(ga.Target);
        }

        // ---------------- anchors ----------------

        public static List<Anchor> AnchorsOn(int map)
        {
            List<Anchor> list;
            if (AnchorsByMap.TryGetValue(map, out list)) return list;
            list = Build(map);
            AnchorsByMap[map] = list;
            return list;
        }

        public static Anchor AnchorOf(ActivityDef a, int map)
        {
            return AnchorsOn(map).FirstOrDefault(x => x.Acts.Contains(a));
        }

        private static List<Anchor> Build(int map)
        {
            var anchors = new List<Anchor>();
            var place = Places.IdForMap(map);
            if (string.IsNullOrEmpty(place)) return anchors;
            List<GamePoint> points;
            PointsByMap.TryGetValue(map, out points);
            points = points ?? new List<GamePoint>();

            foreach (var a in Activities.All.Where(x => x.Place == place && !x.GameSleep))
            {
                var want = AnimOf(a);
                Spots.Spot custom;
                if (Spots.TryGet("act:" + a.Id, map, out custom))
                {
                    var rot = Quaternion.Euler(0f, custom.Yaw, 0f);
                    var c = new Anchor { Key = "c" + a.Id, Pos = custom.Pos, Rot = rot, IconPos = custom.Pos + rot * Vector3.forward * 0.6f, Custom = true };
                    c.Acts.Add(a);
                    // A desk action moves one specific chair of its own map: away from the game's desk, read standing up.
                    c.AnimFor[a.Id] = custom.Anim >= 0 ? custom.Anim : (want == Desk ? 0 : want);
                    anchors.Add(c);
                    continue;
                }
                if (points.Count == 0) continue;

                GamePoint gp = null;
                var anim = want;
                if (want == Desk)
                {
                    gp = points.FirstOrDefault(p => p.Actions.Any(IsDeskAction));
                    if (gp != null) anim = gp.Actions.First(IsDeskAction);
                    else anim = 0;
                }
                if (gp == null && anim >= 0)
                {
                    var ga = ActionNo(anim);
                    gp = points.FirstOrDefault(p => p.Actions.Contains(anim))
                         ?? points.FirstOrDefault(p => ga != null && p.Actions.Select(ActionNo).Any(x => x != null && x.State != null && ga.State != null && x.State.Substring(0, Math.Min(4, x.State.Length)) == ga.State.Substring(0, Math.Min(4, ga.State.Length))));
                }
                if (gp == null)
                {
                    // Chores and jobs (and anything without a matching point): the point with the fewest activities so far.
                    gp = points.OrderBy(p => anchors.Count(x => !x.Custom && x.Key.StartsWith("p" + p.No + ":"))).First();
                }
                // Every activity has its own icon (and anchor), even when several are done at the same game point.
                var anchor = new Anchor { Key = "p" + gp.No + ":" + a.Id, Pos = gp.Pos, Rot = gp.Rot };
                anchors.Add(anchor);
                anchor.Acts.Add(a);
                anchor.AnimFor[a.Id] = anim;
            }

            foreach (var an in anchors.Where(x => !x.Custom))
                an.IconPos = IconSpot(an, points, anchors);
            return anchors;
        }

        /// <summary>Beside the game's own point (so both icons can be used), on walkable floor, away from other icons.</summary>
        private static Vector3 IconSpot(Anchor an, List<GamePoint> points, List<Anchor> anchors)
        {
            var fwd = an.Rot * Vector3.forward;
            var right = an.Rot * Vector3.right;
            var tries = new List<Vector3>();
            foreach (var radius in new[] { 1.3f, 2f, 2.7f, 3.4f })
                foreach (var dir in new[] { -fwd, right, -right, (-fwd + right).normalized, (-fwd - right).normalized, fwd, (fwd + right).normalized, (fwd - right).normalized })
                    tries.Add(dir * radius);
            foreach (var off in tries)
            {
                var p = an.Pos + off;
                NavMeshHit hit;
                try
                {
                    if (!NavMesh.SamplePosition(p, out hit, 0.5f, NavMesh.AllAreas)) continue;
                    p = hit.position;
                }
                catch { }
                if (points.Any(g => Flat(g.Pos - p) < 1.1f)) continue;
                if (anchors.Any(o => o != an && o.IconPos != Vector3.zero && Flat(o.IconPos - p) < 1.1f)) continue;
                return p;
            }
            return an.Pos - fwd * 1.3f;
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        // ---------------- icons ----------------

        /// <summary>Registers the icons of a map once (KKAPI shows them on every visit). <paramref name="now"/> also spawns them
        /// right away (when the player is already on that map).</summary>
        private static void Register(int map, bool now)
        {
            if (Icons.ContainsKey(map) || !On) return;
            if (AnchorsOn(map).Count == 0) { AnchorsByMap.Remove(map); return; } // try again on the next visit
            var list = new List<IDisposable>();
            Icons[map] = list;
            foreach (var an in AnchorsOn(map))
            {
                var anchor = an;
                var kind = an.Acts.GroupBy(KindOf).OrderByDescending(g => g.Count()).First().Key;
                try
                {
                    // Walking into the icon shows the activity's name and what it gives (SpotHint).
                    Action<Illusion.Component.TriggerEnterExitEvent> created = evt =>
                    {
                        if (evt == null) return;
                        evt.onTriggerEnter += c => { if (c != null && c.CompareTag("Player")) SpotHint.Enter(anchor); };
                        evt.onTriggerExit += c => { if (c != null && c.CompareTag("Player")) SpotHint.Exit(anchor); };
                    };
                    list.Add(GameAPI.AddActionIcon(map, an.IconPos, ActivityIcon.Get(kind, true), ActivityIcon.Get(kind, false), () => Use(anchor), created, true, now));
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError("Activity spot icon: " + e); }
            }
            CampusLifePlugin.Log.LogDebug("Activity spots on map " + map + ": " + string.Join(", ", AnchorsOn(map).Select(x => x.Key + "[" + string.Join(",", x.Acts.Select(y => y.Id).ToArray()) + "]").ToArray()));
        }

        /// <summary>Removes and rebuilds the spots of a map (after the player moved one).</summary>
        public static void Rebuild(int map)
        {
            List<IDisposable> list;
            if (Icons.TryGetValue(map, out list)) foreach (var d in list) try { d.Dispose(); } catch { }
            Icons.Remove(map);
            AnchorsByMap.Remove(map);
            Register(map, G.PlayerMapNo == map);
        }

        /// <summary>Settings changed: drop every icon; they come back on the next map load if still on.</summary>
        public static void Reset()
        {
            foreach (var list in Icons.Values) foreach (var d in list) try { d.Dispose(); } catch { }
            Icons.Clear();
            AnchorsByMap.Clear();
            if (On && G.Act != null && G.Act.Map != null && G.Act.Map.no >= 0) Register(G.Act.Map.no, true);
        }

        internal static void Use(Anchor an)
        {
            try
            {
                if (Busy || Scenes.Running) return;
                var a = an.Acts.FirstOrDefault();
                if (a == null) return;
                var block = Activities.Blocker(Campus.S, a);
                if (block != null) { Toasts.Show(a.Name + ": " + block); return; }
                Sfx.Ok();
                Run(a, false);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        /// <summary>Remembers the player's spot for this activity on this map (position tools).</summary>
        public static void MarkHere(ActivityDef a)
        {
            var p = G.Act.Player;
            var map = G.PlayerMapNo;
            Spots.Set("act:" + a.Id, map, p.position, p.rotation.eulerAngles.y);
            Rebuild(map);
            Toasts.Show(a.Name + " is now done here (" + Places.MapLabel(map) + ").");
        }

        public static void ResetSpot(ActivityDef a)
        {
            var map = G.PlayerMapNo;
            if (!Spots.Remove("act:" + a.Id, map)) return;
            Rebuild(map);
            Toasts.Show(a.Name + ": back to its default spot.");
        }

        public static bool HasCustom(ActivityDef a)
        {
            Spots.Spot s;
            return Spots.TryGet("act:" + a.Id, G.PlayerMapNo, out s);
        }

        // ---------------- doing it ----------------

        /// <summary>Does the activity at its spot: animation or black screen, then the result. <paramref name="fromMenu"/>: wait
        /// for the menu to close first.</summary>
        public static void Run(ActivityDef a, bool fromMenu)
        {
            if (Busy) return;
            CampusLifePlugin.Instance.StartCoroutine(RunCo(a, fromMenu).PreventFromCrashing());
        }

        // Like the game's own action points, an activity costs only the real time its animation (or black screen) takes:
        // the period clock keeps running meanwhile, and nothing is added afterwards. Energy is what limits activities.

        private static IEnumerator RunCo(ActivityDef a, bool fromMenu)
        {
            Busy = true;
            if (fromMenu)
            {
                var t0 = Time.realtimeSinceStartup;
                while (!G.IsFreeRoaming && Time.realtimeSinceStartup - t0 < 6f) yield return null;
                yield return null;
            }
            var anchor = G.IsFreeRoaming ? AnchorOf(a, G.PlayerMapNo) : null;
            var anim = -1;
            if (anchor != null) anchor.AnimFor.TryGetValue(a.Id, out anim);
            var ga = ActionNo(anim);
            var block = OwnBlocker(a);
            if (block != null) { Busy = false; Toasts.Show(a.Name + ": " + block); yield break; }

            if (ga != null && !ga.IsDark && G.IsFreeRoaming)
            {
                var play = new Playback();
                if (play.Begin(ga, anchor))
                {
                    var t0 = Time.realtimeSinceStartup;
                    yield return null;
                    yield return null;
                    while (play.Running && Time.realtimeSinceStartup - t0 < 30f) yield return null;
                    play.End();
                    CampusLifePlugin.Log.LogInfo(a.Name + ": game action " + ga.No + " (" + ga.State + ") played for " + (Time.realtimeSinceStartup - t0).ToString("0.0") + " s.");
                }
                Finish(a, false);
                Busy = false;
                yield break;
            }

            // Black screen with the result text, like the game's "dark" actions (drawn by us; the player stands still).
            var res = OwnDo(a);
            if (res == null) { Busy = false; yield break; }
            var player = G.Act != null ? G.Act.Player : null;
            if (player != null) { player.isActionNow = true; try { player.agent.enabled = false; } catch { } }
            yield return CampusLifePlugin.Instance.StartCoroutine(DarkScreen.Show(res.Text.Split('\n')));
            if (player != null) { try { player.agent.enabled = true; } catch { } player.isActionNow = false; }
            Report(a, res, true);
            Busy = false;
        }

        private static void Finish(ActivityDef a, bool textShown)
        {
            var res = OwnDo(a);
            if (res != null) Report(a, res, textShown);
        }

        private static void Report(ActivityDef a, ActivityResult res, bool textShown)
        {
            CampusPanel.SetLastResult(a.Name + ": " + res.Text + "\n" + res.Gains.Describe());
            var others = new Rewards { Money = res.Gains.Money, Rep = res.Gains.Rep, Club = res.Gains.Club, Favor = res.Gains.Favor, Devotion = res.Gains.Devotion, Item = res.Gains.Item };
            var rest = others.Describe();
            if (!textShown) Toasts.Show(a.Name + ": " + res.Text.Replace("\n", " ") + (rest != "-" ? " (" + rest + ")" : ""));
            else if (rest != "-") Toasts.Show(a.Name + ": " + rest);
            ShowStatGain(res.Gains.Int, res.Gains.Str, res.Gains.Sens);
        }

        /// <summary>The game's own "+INT / +STR / +H" popup.</summary>
        private static void ShowStatGain(int intel, int str, int sens)
        {
            if (intel == 0 && str == 0 && sens == 0) return;
            try
            {
                var go = CommonLib.LoadAsset<GameObject>("action/playeraction/00.unity3d", "paramupdate", true, string.Empty);
                var ui = go != null ? go.GetComponent<ParamUpdateUI>() : null;
                if (ui == null) return;
                ui.physical = str;
                ui.intellect = intel;
                ui.hentai = sens;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>Plays a game action on the player like Manager.PlayerAction.Action, without its stat gain and action count.</summary>
        private sealed class Playback
        {
            private readonly List<GameObject> _items = new List<GameObject>();
            private readonly List<KeyValuePair<Transform, KeyValuePair<Vector3, Quaternion>>> _restore = new List<KeyValuePair<Transform, KeyValuePair<Vector3, Quaternion>>>();
            private ActionGame.Chara.Player _player;
            private bool _fps;
            private CrossFade _fade;

            public bool Running
            {
                get
                {
                    try { return _player != null && _player.isActive && _player.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 0.8f; }
                    catch { return false; }
                }
            }

            public bool Begin(GameAction ga, Anchor at)
            {
                try
                {
                    var act = G.Act;
                    _player = act.Player;
                    var pos = at != null ? at.Pos : _player.position;
                    var rot = at != null ? at.Rot : _player.rotation;
                    _player.agent.enabled = false;
                    _player.isActionNow = true;
                    _player.motion.state = ga.State;
                    _player.motion.Play(_player.animator);
                    foreach (var it in ga.Items)
                    {
                        var go = CommonLib.LoadAsset<GameObject>(it[0], it[1], true, it[2]);
                        if (go == null) continue;
                        go.transform.SetPositionAndRotation(pos, rot);
                        _items.Add(go);
                    }
                    _fade = Camera.main != null ? Camera.main.GetComponent<CrossFade>() : null;
                    if (_fade != null) _fade.FadeStart(-1f);
                    Keep(_player.transform);
                    _player.SetPositionAndRotation(pos, rot);
                    var fpsCam = act.CameraState.FPSCamera;
                    _fps = fpsCam != null;
                    if (_fps)
                    {
                        fpsCam.Angle = _player.eulerAngles;
                        act.CameraState.ModeChangeForce(CameraMode.TPS, false);
                        CampusLifePlugin.Instance.StartCoroutine(TurnCamera(act));
                    }
                    if (!string.IsNullOrEmpty(ga.Target) && act.Map != null && act.Map.mapRoot != null)
                    {
                        var target = act.Map.mapRoot.transform.FindLoop(ga.Target);
                        if (target != null)
                        {
                            Keep(target.transform);
                            target.transform.position = ga.Move;
                            target.transform.eulerAngles = ga.Rot;
                        }
                    }
                    return true;
                }
                catch (Exception e)
                {
                    CampusLifePlugin.Log.LogError("Activity animation: " + e);
                    End();
                    return false;
                }
            }

            private IEnumerator TurnCamera(ActionScene act)
            {
                yield return null;
                try
                {
                    var tps = act.CameraState.TPSCamera;
                    if (tps != null) tps.Angle = Quaternion.LookRotation(-_player.cachedTransform.forward).eulerAngles;
                }
                catch { }
            }

            private void Keep(Transform t)
            {
                _restore.Add(new KeyValuePair<Transform, KeyValuePair<Vector3, Quaternion>>(t, new KeyValuePair<Vector3, Quaternion>(t.position, t.rotation)));
            }

            public void End()
            {
                try
                {
                    var act = G.Act;
                    if (_fps && act != null) act.CameraState.ModeChangeForce(CameraMode.FPS, false);
                    if (_player != null && _player.isActive)
                    {
                        _player.agent.enabled = true;
                        _player.isActionNow = false;
                    }
                    if (_fade != null) _fade.FadeStart(-1f);
                    if (_player != null) _player.PlayAnimation();
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError("Activity animation end: " + e); }
                foreach (var go in _items) try { UnityEngine.Object.Destroy(go); } catch { }
                _items.Clear();
                foreach (var r in _restore) try { if (r.Key != null) r.Key.SetPositionAndRotation(r.Value.Key, r.Value.Value); } catch { }
                _restore.Clear();
                _player = null;
            }
        }
    }
}
