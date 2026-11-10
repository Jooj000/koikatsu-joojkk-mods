using System;
using System.Collections;
using System.Linq;
using ActionGame.Chara;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A girl asleep on an infirmary bed: the game only says she is sleeping and ends the talk. Now, once that is over,
    /// you can tease her awake; half asleep she gives in far more easily than awake, and if she does, the game's H starts
    /// where she lies (as a girl can wake you with her hand when you nap there).
    /// </summary>
    internal static class SleepTease
    {
        private static TalkScene _seen;
        private static SaveData.Heroine _pending;

        /// <summary>Every frame, from Conversation.Watch.</summary>
        public static void Watch(TalkScene ts)
        {
            try
            {
                if (ts != null)
                {
                    if (ts == _seen) return;
                    _seen = ts;
                    var h = ts.targetHeroine;
                    if (!Campus.On || !Cfg.Enabled.Value || h == null || !GirlInfo.IsRegular(h) || ts.necessaryInfo == null || ts.necessaryInfo.state != 2) return;
                    // A nap teases on any map, not just the infirmary.
                    _pending = h;
                    return;
                }
                _seen = null;
                if (_pending == null) return;
                var her = _pending;
                _pending = null;
                Use(her);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Sleep tease: " + e.Message); }
        }

        private static IDisposable _icon;
        private static SaveData.Heroine _iconFor;
        internal static SaveData.Heroine IconFor { get { return _iconFor; } }
        internal static Vector3 IconPos { get; private set; }

        internal static bool Press()
        {
            var h = _iconFor;
            if (h == null) return false;
            return Use(h);
        }

        /// <summary>
        /// The icon is not taken away when used; it stays while the scene plays (the game hides it then) and after "Let her
        /// sleep", so you can come back to her. It goes when she gives in (H) or wakes up and says no.
        /// </summary>
        private static bool Use(SaveData.Heroine h)
        {
            if (_busy || h == null) return false;
            _busy = true;
            CampusLifePlugin.Instance.StartCoroutine(Offer(h).PreventFromCrashing());
            return true;
        }
        private static bool _busy;
        /// <summary>Who woke up and said no, and when (day * 10 + period): no icon for her again that period.</summary>
        private static string _awake = "";
        private static string Period(SaveData.Heroine h) { return GirlInfo.Key(h) + "@" + Campus.S.Day + "." + G.Period; }
        private static int _iconMap = -1;
        private static float _nextLook;
        private static readonly float[] Purple = { 0.6f, 0.3f, 0.86f };
        private static Sprite _on, _off;

        /// <summary>
        /// While a girl sleeps on an infirmary bed (the game's sleep action), an activity icon with a purple imp stands
        /// by her; walking into it and pressing it offers the tease (the old way, after the game's own "she is sleeping"
        /// talk, stays too). The icon goes when she wakes or you leave.
        /// </summary>
        public static void Update()
        {
            if (Time.realtimeSinceStartup < _nextLook) return;
            _nextLook = Time.realtimeSinceStartup + 0.5f;
            try
            {
                var act = G.Act;
                NPC her = null;
                if (Campus.On && Cfg.Enabled.Value && act != null && act.Map != null && act.npcList != null && !G.TalkSceneOpen)
                {
                    her = act.npcList.FirstOrDefault(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && GirlInfo.IsRegular(n.heroine)
                                                          && n.AI != null && n.AI.actionNo == SleepAction && n.isArrival && _awake != Period(n.heroine));
                    var near = act.npcList.FirstOrDefault(n => n != null && n.heroine != null && n.mapNo == act.Map.no && n.AI != null && n.AI.actionNo == SleepAction);
                    Why = her != null ? "asleep: " + GirlInfo.FirstName(her.heroine)
                        : near == null ? "nobody here with the sleep action"
                        : GirlInfo.FirstName(near.heroine) + ": active " + near.isActive + ", regular " + GirlInfo.IsRegular(near.heroine) + ", arrived " + near.isArrival;
                }
                else Why = "no map (" + Places.CurrentId + "), talk open " + G.TalkSceneOpen;
                var h = her != null ? her.heroine : null;
                // The imp floated over her during the tease scene itself. It stays off while one of our scenes plays and
                // comes back after it if she is still asleep ("Let her sleep").
                if (h != null && Scenes.Running) { Why = "a scene is open (" + GirlInfo.FirstName(h) + " asleep)"; h = null; }
                if (h == _iconFor && (h == null || _iconMap == act.Map.no)) return;
                Drop();
                if (h == null) return;
                if (_on == null) _on = MapSprite.Make(IconArt.Spot("devil", Purple, 128, true), 128, 100f, "cl_tease_on");
                if (_off == null) _off = MapSprite.Make(IconArt.Spot("devil", Purple, 128, false), 128, 100f, "cl_tease_off");
                // A spot he set for this map in the Alt+F7 window wins when it is near her (a map can have more than one
                // bed).
                Spots.Spot mine;
                var pos = Spots.TryGet(SpotKind, act.Map.no, out mine) && Flat(mine.Pos - her.position) <= 4f ? mine.Pos : Reachable(her, act.Player);
                var target = h;
                _icon = GameAPI.AddActionIcon(act.Map.no, pos, _on, _off, () =>
                {
                    try { Use(target); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                }, null, true, true);
                _iconFor = h;
                _iconMap = act.Map.no;
                IconPos = pos;
                CampusLifePlugin.Log.LogInfo("Sleep tease: " + GirlInfo.FirstName(h) + " asleep (" + Places.CurrentId + "), imp icon at " + pos + ".");
            }
            catch (Exception e) { Why = "error: " + e.Message; if (_warned++ < 3) CampusLifePlugin.Log.LogWarning("Sleep tease icon: " + e); Drop(); }
        }

        /// <summary>
        /// The icon stood 0.9 m to her right, which on the infirmary bed was inside the curtain, out of sight and out of
        /// reach (the walk stopped 2.2 m from it). Now it stands on the floor the player can walk to, as close to her as it
        /// gets, on the player's side when two spots are as close.
        /// </summary>
        private static Vector3 Reachable(NPC her, ActionGame.Chara.Player player)
        {
            var at = her.position;
            var from = player != null ? player.position : at;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(from, out hit, 1f, NavMesh.AllAreas)) from = hit.position;
            var path = new NavMeshPath();
            var best = at;
            var bestScore = float.MaxValue;
            foreach (var r in new[] { 0.6f, 0.9f, 1.3f, 1.8f, 2.4f })
                for (var k = 0; k < 12; k++)
                {
                    var p = at + Quaternion.Euler(0f, k * 30f, 0f) * Vector3.forward * r;
                    if (!NavMesh.SamplePosition(p, out hit, 1f, NavMesh.AllAreas)) continue;
                    p = hit.position;
                    var near = Flat(p - at);
                    if (near > 2.6f) continue;
                    // A path found with every area ended 2.5 m short for the player's own agent (library). The player's
                    // agent's areas and the path's real end are what count.
                    var agent = player != null ? player.agent : null;
                    var ok = agent != null && agent.isOnNavMesh ? agent.CalculatePath(p, path) : NavMesh.CalculatePath(from, p, NavMesh.AllAreas, path);
                    if (!ok || path.status != NavMeshPathStatus.PathComplete || path.corners.Length == 0 || Flat(path.corners[path.corners.Length - 1] - p) > 0.3f) continue;
                    var score = near + 0.15f * Flat(p - from);
                    if (score < bestScore) { bestScore = score; best = p; }
                }
            if (bestScore == float.MaxValue) CampusLifePlugin.Log.LogInfo("Sleep tease: no floor the player can reach by " + GirlInfo.FirstName(her.heroine) + "; the icon stands where she lies.");
            return best;
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        /// <summary>The game's AI action for sleeping in the infirmary (Director.Intents.SleepAction).</summary>
        private const int SleepAction = 22;

        internal static string Why = "";
        private static int _warned;

        /// <summary>The key of the imp's spot in joojkk.campuslife.spots.txt ("tease:&lt;map&gt;").</summary>
        internal const string SpotKind = "tease";

        /// <summary>The Alt+F7 window's "Here" / "1 body ahead": the imp stands there on this map from now on.</summary>
        internal static string SetSpot(Vector3 pos, float yaw)
        {
            var map = G.PlayerMapNo;
            Spots.Set(SpotKind, map, pos, yaw);
            Drop();
            _nextLook = 0f;
            CampusLifePlugin.Log.LogInfo(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Sleep tease: imp spot on map {0} at {1:0.00}, {2:0.00}, {3:0.00}", map, pos.x, pos.y, pos.z));
            return "imp spot set on map " + map;
        }

        /// <summary>The Alt+F7 window's "Default": back to the floor next to her that the player can reach.</summary>
        internal static bool ResetSpot()
        {
            if (!Spots.Remove(SpotKind, G.PlayerMapNo)) return false;
            Drop();
            _nextLook = 0f;
            return true;
        }

        /// <summary>For the Alt+F7 window: who sleeps here and where the imp is now.</summary>
        internal static string State()
        {
            if (_iconFor != null && _iconMap == G.PlayerMapNo)
                return "imp by " + GirlInfo.FirstName(_iconFor) + string.Format(System.Globalization.CultureInfo.InvariantCulture, " at {0:0.00}, {1:0.00}, {2:0.00}", IconPos.x, IconPos.y, IconPos.z);
            return string.IsNullOrEmpty(Why) ? "nobody asleep here" : Why;
        }

        private static void Drop()
        {
            if (_icon != null) try { _icon.Dispose(); } catch { }
            _icon = null;
            _iconFor = null;
            _iconMap = -1;
        }

        /// <summary>How likely she gives in, half asleep (much easier than the usual ask).</summary>
        public static float Chance(SaveData.Heroine h)
        {
            var q = GirlInfo.Quirks(h);
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == Campus.S.Day ? gs.Mood : Mood.None;
            var c = 0.3f + h.favor / 250f + h.lewdness / 180f;
            if (h.isGirlfriend) c += 0.4f;
            else if (h.relation >= 1) c += 0.12f;
            else if (h.relation < 0) c -= 0.2f;
            if (q.bitch) c += 0.2f;
            if (q.choroi) c += 0.1f;
            if (mood == Mood.Flirty) c += 0.15f;
            if (mood == Mood.Upset) c -= 0.15f;
            if (h.isAnger || Pairs.IsScorned(h)) c -= 0.5f;
            return Mathf.Clamp(c, 0.05f, 0.97f);
        }

        /// <summary>The infirmary lines talk of the bed and the curtain; anywhere else the ".out" ones.</summary>
        private static string Say(SaveData.Heroine h, string tag)
        {
            return (Places.CurrentId != "infirmary" ? Conversation.BeatSay(h, tag + ".out") : null) ?? Conversation.BeatSay(h, tag);
        }

        private static IEnumerator Offer(SaveData.Heroine h)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f && !Scenes.CanPlayOnMap) yield return null;
            if (!Scenes.CanPlayOnMap || Truth.NpcOf(h) == null) { _busy = false; yield break; }
            var name = GirlInfo.FirstName(h);
            var t = GirlInfo.ToneOf(h);
            var chance = Chance(h);
            var b = new SceneBuilder(null, false);
            b.Narr(Say(h, "sleeptease.asleep") ?? "...");
            b.Choice("Tease her awake.", "tease", "Let her sleep.", "leave");
            b.Branch("leave");
            b.Narr(Say(h, "sleeptease.leave") ?? "...");
            b.End();
            var yes = Consent.Yes(h, "sleeptease", chance);
            b.Branch("tease");
            b.Narr(Say(h, "sleeptease.tease") ?? "...");
            if (yes)
            {
                b.Face(h, Feel.Love).Say(h, Conversation.BeatSay(h, "sleeptease", "yes") ?? "...");
                b.Narr(Conversation.BeatSay(h, "sleeptease.yes.narr") ?? "...");
            }
            else
            {
                b.Face(h, Feel.Surprised).Say(h, Conversation.BeatSay(h, "sleeptease", "no") ?? "...");
                b.Narr(Conversation.BeatSay(h, "sleeptease.no.narr") ?? "...");
            }
            b.End();
            var played = Scenes.PlayOnMap(b, c =>
            {
                _busy = false;
                if (c != "tease")
                {
                    var n = Truth.NpcOf(h);
                    CampusLifePlugin.Log.LogInfo("Sleep tease: let " + name + " sleep; she is " + (n != null && n.AI != null && n.AI.actionNo == SleepAction ? "still asleep" : "awake now (action " + (n != null && n.AI != null ? n.AI.actionNo : -1) + ")") + ", the icon " + (_iconFor == h ? "stays" : "is gone"));
                    return;
                }
                if (_iconFor == h) Drop();
                if (!yes) _awake = Period(h);
                CampusLifePlugin.Log.LogInfo("Sleep tease: " + name + " " + (yes ? "gives in" : "says no") + " (score " + chance.ToString("0.00") + ")");
                if (yes)
                {
                    G.AddFavor(h, 2, 3);
                    Teachers.StartH(h);
                }
                else
                {
                    G.AddFavor(h, h.relation >= 1 ? -1 : -4, 1);
                    Social.Add(Social.Kinds.Flirted, h, Witness.Who(h, Witness.Flirt), Witness.Flirt);
                }
            });
            if (!played) _busy = false;
        }
    }
}
