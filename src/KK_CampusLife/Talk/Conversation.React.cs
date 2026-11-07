using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Communication;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Touches and silences in the conversation. The game always plays the same clip for a
    /// place and level and the same "are you listening?" after 30 s: we pick among every clip of the same meaning at her
    /// stage and the one below (another voice, face and pose), and after it she says a line of ours from touch.dlg /
    /// idle.dlg that knows who she is to you, her mood, the place and who is watching. Some of it has weight: a crowd makes
    /// touching harder, being alone makes it easier, friends tease, rivals sneer, and a girl who barely knows you walks off
    /// if you keep staring at her in silence.
    /// </summary>
    internal static partial class Conversation
    {
        private sealed class Pend
        {
            public SaveData.Heroine Her;
            public string Kind = "";       // touch | idle
            public int Part, Level;
            public bool Look;
            public string PartName, Kiss = "", Answer = "";   // butt / mouth (ours, TouchMore); yes / wary / wary_no / no for a kiss; the director's answer to a touch
            public float At, ReadyAt;
            public Info Info;
            /// <summary>One click, one reaction: her line answers this touch, so the game's clip gives way and its pose goes with the line.</summary>
            public bool Speak = true, Cut;
            public string Pose;
            public int Frame = -1;   // the frame TouchFunc asked for the game's clip (TouchClip)
        }

        private static Pend _pend;
        private static readonly Dictionary<string, string> LastClip = new Dictionary<string, string>();
        private static readonly System.Reflection.FieldInfo TransVoiceField = AccessTools.Field(typeof(TalkScene), "transVoice");

        public static void InstallReact(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(Info), "GetTouchVoice"), postfix: new HarmonyMethod(typeof(Conversation), nameof(TouchClip)));
                harmony.Patch(AccessTools.Method(typeof(Info), "GetLeaveAloneVoice"), postfix: new HarmonyMethod(typeof(Conversation), nameof(IdleClip)));
                harmony.Patch(AccessTools.Method(typeof(TalkScene), "TouchFunc"), postfix: new HarmonyMethod(typeof(Conversation), nameof(CutTouchVoice)));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Conversation reactions: " + e.Message); }
        }

        private static int StageOf(SaveData.Heroine h)
        {
            if (h.isGirlfriend) return 2;
            if (h.talkEvent.Contains(2)) return 1;
            return h.talkEvent.Contains(0) || h.talkEvent.Contains(1) ? 0 : -1;
        }

        /// <summary>Another clip of the same meaning (never the last one played for that key when there is a choice).</summary>
        private static void Swap(ReactionBase r, List<Info.TalkData> pool, string key)
        {
            pool = pool.Where(t => t != null && !string.IsNullOrEmpty(t.file)).ToList();
            if (pool.Count < 2) return;
            string last;
            LastClip.TryGetValue(key, out last);
            var fresh = pool.Where(t => t.file != last && t.file != r.file).ToList();
            if (fresh.Count == 0 || Random.value < 0.25f) return;   // the game's own clip now and then
            var t0 = fresh[Random.Range(0, fresh.Count)];
            r.assetbundle = t0.assetbundle; r.file = t0.file; r.pose = t0.pose; r.facialExpression = t0.facialExpression;
            r.eyesLook = t0.eyeLook; r.neckLook = t0.neckLook;
            LastClip[key] = t0.file;
        }

        // ---------------- animations (2026-10-28: "adicionar outras animações tanto para a reação do toque quanto para
        // ficar parada com o player sem fazer nada") ----------------

        private static readonly System.Reflection.MethodInfo AnimePlayMethod = AccessTools.Method(typeof(TalkScene), "AnimePlay");
        private static readonly Dictionary<string, List<string>> PosePools = new Dictionary<string, List<string>>();
        private static Info _poseInfo;

        /// <summary>
        /// Every pose the game itself uses for that feeling, from every stage and body part: a touch at level L takes the
        /// poses of every Touch and See clip at level L; a silence takes the LeaveAlone and Interlude poses. The clip's
        /// voice and face stay, only the body moves differently, so a poke on the head can get the shy pose of a cheek touch.
        /// </summary>
        private static List<string> PosePool(Info info, string kind, int level)
        {
            if (info == null) return new List<string>();
            if (_poseInfo != info) { PosePools.Clear(); _poseInfo = info; }
            var key = kind + level;
            List<string> l;
            if (PosePools.TryGetValue(key, out l)) return l;
            var set = new HashSet<string>();
            foreach (var byGroup in info.dicTalkInfo.Values)
            {
                if (kind == "touch")
                {
                    foreach (var g in new[] { Info.Group.Touch, Info.Group.See })
                    {
                        Dictionary<int, List<Info.BasicInfo>> byPart;
                        if (!byGroup.TryGetValue(g, out byPart)) continue;
                        foreach (var levels in byPart.Values)
                        {
                            var gi = level >= 0 && level < levels.Count ? levels[level] as Info.GenericInfo : null;
                            if (gi != null && gi.talk != null) foreach (var t in gi.talk) if (t != null && !string.IsNullOrEmpty(t.pose)) set.Add(t.pose);
                        }
                    }
                }
                else
                {
                    foreach (var g in new[] { Info.Group.LeaveAlone, Info.Group.Interlude })
                    {
                        Dictionary<int, List<Info.BasicInfo>> byKey;
                        if (!byGroup.TryGetValue(g, out byKey)) continue;
                        foreach (var gi in byKey.Values.SelectMany(x => x).OfType<Info.GenericInfo>().Where(x => x.talk != null))
                            foreach (var t in gi.talk) if (t != null && !string.IsNullOrEmpty(t.pose)) set.Add(t.pose);
                    }
                }
            }
            // Only poses this personality can play.
            var pers = G.TalkScene != null && G.TalkScene.targetHeroine != null ? G.TalkScene.targetHeroine.FixCharaIDOrPersonality : -1;
            l = set.Where(x => pers < 0 || Singleton<Manager.Communication>.Instance.GetPoseInfo(pers, x) != null).ToList();
            PosePools[key] = l;
            CampusLifePlugin.Log.LogDebug("Talk poses " + key + ": " + l.Count + " (" + string.Join(", ", l.ToArray()) + ")");
            return l;
        }

        /// <summary>
        /// The poses the game uses for that body part (touched or looked at), at every stage and level: a poke on the head
        /// first gets another head pose, so the movement still fits the place.
        /// </summary>
        private static List<string> PartPool(Info info, bool touch, int part, int level)
        {
            if (info == null) return new List<string>();
            if (_poseInfo != info) { PosePools.Clear(); _poseInfo = info; }
            var key = "part" + touch + part + "L" + level;
            List<string> l;
            if (PosePools.TryGetValue(key, out l)) return l;
            var set = new HashSet<string>();
            foreach (var byGroup in info.dicTalkInfo.Values)
            {
                Dictionary<int, List<Info.BasicInfo>> byPart;
                List<Info.BasicInfo> levels;
                if (!byGroup.TryGetValue(touch ? Info.Group.Touch : Info.Group.See, out byPart) || !byPart.TryGetValue(part, out levels)) continue;
                // Only her answers at this level (a pat she likes must not get an angry pose), from every stage.
                var gi = level >= 0 && level < levels.Count ? levels[level] as Info.GenericInfo : null;
                if (gi != null && gi.talk != null)
                    foreach (var t in gi.talk) if (t != null && !string.IsNullOrEmpty(t.pose)) set.Add(t.pose);
            }
            var pers = G.TalkScene != null && G.TalkScene.targetHeroine != null ? G.TalkScene.targetHeroine.FixCharaIDOrPersonality : -1;
            l = set.Where(x => pers < 0 || Singleton<Manager.Communication>.Instance.GetPoseInfo(pers, x) != null).ToList();
            PosePools[key] = l;
            CampusLifePlugin.Log.LogDebug("Talk poses " + key + ": " + l.Count + " (" + string.Join(", ", l.ToArray()) + ")");
            return l;
        }

        private static string _lastPose;
        private static readonly Dictionary<string, string> LastPoseOf = new Dictionary<string, string>();

        private static string OtherPose(List<string> pool, params string[] not)
        {
            var fresh = pool.Where(x => x != _lastPose && !not.Contains(x)).ToList();
            if (fresh.Count == 0) return null;
            var pick = fresh[Random.Range(0, fresh.Count)];
            _lastPose = pick;
            return pick;
        }

        /// <summary>
        /// 2026-10-28 ("a pose do toque já era essa. é para adicionar variações"): the game's own pose for that clip, and
        /// the last pose of the same place, are never picked again; two times in three a pose of the same body part, else
        /// any pose of the same feeling.
        /// </summary>
        /// <summary>
        /// Does the pose fit how she took it?
        /// 0 welcome: no covering, no anger, no sadness; 1 uneasy: she may cover or shrink, not rage; 2-3 stop and angry:
        /// nothing happy. -1: anything (silence).
        /// </summary>
        internal static bool FitsReaction(string pose, int reaction)
        {
            if (reaction < 0 || string.IsNullOrEmpty(pose)) return true;
            if (reaction == 0) return !Covers(pose) && !HasAny(pose, "怒", "嫌", "哀", "拒", "驚");
            if (reaction == 1) return !HasAny(pose, "怒", "喜", "楽");
            return !HasAny(pose, "喜", "嬉", "笑", "楽", "好");
        }

        private static bool HasAny(string s, params string[] words) { return words.Any(w => s.Contains(w)); }

        /// <summary>Happy and shy poses of her whole table (joy, fun, embarrassed), for a touch she welcomes.</summary>
        private static List<string> WelcomePoses(SaveData.Heroine h)
        {
            var table = PoseTable(h);
            if (table == null) return new List<string>();
            return table.Keys.Where(x => FitsReaction(x, 0) && HasAny(x, "喜", "楽", "嬉", "笑", "照れ", "恥ずかし")).ToList();
        }

        private static string VariedPose(List<string> part, List<string> level, string slot, string gamePose, int reaction = -1)
        {
            string last;
            LastPoseOf.TryGetValue(slot, out last);
            if (reaction >= 0)
            {
                var p2 = part.Where(x => FitsReaction(x, reaction)).ToList();
                var l2 = level.Where(x => FitsReaction(x, reaction)).ToList();
                if (reaction == 0 && G.TalkScene != null) p2 = p2.Union(WelcomePoses(G.TalkScene.targetHeroine)).ToList();
                if (p2.Count + l2.Count > 0) { part = p2; level = l2; }
            }
            Func<List<string>, List<string>> fresh = l => l.Where(x => x != gamePose && x != last && x != _lastPose).ToList();
            var a = fresh(part);
            var b = fresh(level);
            var from = a.Count > 0 && (b.Count == 0 || Random.value < 0.67f) ? a : b;
            if (from.Count == 0) return null;
            var pick = from[Random.Range(0, from.Count)];
            LastPoseOf[slot] = pick;
            _lastPose = pick;
            CampusLifePlugin.Log.LogInfo("Talk pose " + slot + ": " + pick + (gamePose != null ? " (game had " + gamePose + ")" : "") + ", " + (from == a ? "part" : "level") + " pool " + from.Count);
            return pick;
        }

        /// <summary>Plays one of the talk scene's poses on her now (the game's own AnimePlay: body, IK and cross-fade).</summary>
        private static void PlayPose(TalkScene ts, string pose)
        {
            if (ts == null || string.IsNullOrEmpty(pose) || AnimePlayMethod == null) return;
            try { AnimePlayMethod.Invoke(ts, new object[] { pose, false }); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk pose " + pose + ": " + e.Message); }
        }

        private static void TouchClip(Info __instance, bool _touch, int _parts, int _stage, TouchReactionInfo __result)
        {
            try
            {
                if (!Campus.On || __result == null) return;
                var ts = G.TalkScene;
                var h = ts != null ? ts.targetHeroine : null;
                if (h == null) return;
                if (!h.isAnger)
                {
                    var stage = StageOf(h);
                    var pool = new List<Info.TalkData>();
                    var group = _touch ? Info.Group.Touch : Info.Group.See;
                    foreach (var st in new[] { stage, stage, stage - 1 })
                    {
                        Dictionary<Info.Group, Dictionary<int, List<Info.BasicInfo>>> byGroup;
                        Dictionary<int, List<Info.BasicInfo>> byPart;
                        List<Info.BasicInfo> levels;
                        if (!__instance.dicTalkInfo.TryGetValue(st, out byGroup) || !byGroup.TryGetValue(group, out byPart) || !byPart.TryGetValue(_parts, out levels)) continue;
                        var gi = _stage >= 0 && _stage < levels.Count ? levels[_stage] as Info.GenericInfo : null;
                        if (gi != null && gi.talk != null) pool.AddRange(gi.talk);
                    }
                    var gamePose = __result.pose;
                    Swap(__result, pool, "t" + _touch + _parts + _stage);
                    var pose = VariedPose(PartPool(__instance, _touch, _parts, _stage), PosePool(__instance, "touch", _stage), "t" + _touch + _parts, gamePose, Mathf.Min(_stage, 3));
                    if (pose != null) __result.pose = pose;
                }
                if (Active)
                {
                    var level = h.isAnger ? 3 : _stage;
                    // Not after every poke: the first touch of each place, every "stop it", and a third of the rest.
                    var speak = Count("react:touch:" + PartWord(_parts, !_touch)) == 0 || level >= 2 || Random.value <= 0.35f;
                    _pend = new Pend { Her = h, Kind = "touch", Part = _parts, Look = !_touch, Level = level, Answer = h.isAnger ? "no" : Consent.LastTouch, At = Time.realtimeSinceStartup, Info = __instance, Speak = speak, Frame = Time.frameCount };
                    TouchNote("clip " + PartWord(_parts, !_touch) + (speak ? " (her line answers)" : " (only the clip)"));
                    // She used to take the clip, stand again, and take the line with another pose: two reactions to one
                    // click (user, 2026-11-01). The pose waits for the line and her face is the line's; the voice keeps its
                    // file, so TouchFunc and TouchEnd run as always (an empty one froze the talk and the map, 2026-11-02),
                    // and CutTouchVoice takes it away in the same frame.
                    if (speak && OneBeat)
                    {
                        _pend.Pose = __result.pose;
                        __result.facialExpression = "";
                        __result.pose = "";
                    }
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Touch clip: " + e.Message); }
        }

        private static void IdleClip(Info __instance, LeaveAloneReactionInfo __result)
        {
            try
            {
                if (!Campus.On || __result == null) return;
                var ts = G.TalkScene;
                var h = ts != null ? ts.targetHeroine : null;
                if (h == null) return;
                if (!h.isAnger)
                {
                    var stage = StageOf(h);
                    var pool = new List<Info.TalkData>();
                    foreach (var st in new[] { stage, stage, stage - 1 })
                    {
                        Dictionary<Info.Group, Dictionary<int, List<Info.BasicInfo>>> byGroup;
                        Dictionary<int, List<Info.BasicInfo>> byKey;
                        if (!__instance.dicTalkInfo.TryGetValue(st, out byGroup) || !byGroup.TryGetValue(Info.Group.LeaveAlone, out byKey)) continue;
                        foreach (var gi in byKey.Values.SelectMany(l => l).OfType<Info.GenericInfo>().Where(g => g.conditions != 0 && g.talk != null))
                            pool.AddRange(gi.talk);
                    }
                    var gamePose = __result.pose;
                    Swap(__result, pool, "idle");
                    var pose = VariedPose(new List<string>(), PosePool(__instance, "idle", 0), "idle", gamePose);
                    if (pose != null) __result.pose = pose;
                }
                // With both girls a silence is theirs to fill (TrioTick), not her "are you listening?".
                if (Active && _trio == null) _pend = new Pend { Her = h, Kind = "idle", At = Time.realtimeSinceStartup, Info = __instance };
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Idle clip: " + e.Message); }
        }

        // ---------------- who is watching ----------------

        /// <summary>Regular girls near you on this map (not her), closest first.</summary>
        internal static List<SaveData.Heroine> Onlookers(SaveData.Heroine her, float radius = 9f)
        {
            var l = new List<SaveData.Heroine>();
            try
            {
                var act = G.Act;
                if (act == null || act.Map == null || act.Player == null) return l;
                var pp = act.Player.transform.position;
                var map = act.Map.no;
                l.AddRange(act.npcList.Where(n => n != null && n.heroine != null && n.heroine != her && n.isActive && n.mapNo == map && GirlInfo.IsRegular(n.heroine)
                                                   && (n.transform.position - pp).sqrMagnitude <= radius * radius)
                    .OrderBy(n => (n.transform.position - pp).sqrMagnitude).Select(n => n.heroine));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Onlookers: " + e.Message); }
            return l;
        }

        private static void WatchCtx(SaveData.Heroine h, Dictionary<string, string> c)
        {
            var near = Onlookers(h);
            c["watchers"] = near.Count == 0 ? "none" : near.Count == 1 ? "one" : near.Count <= 3 ? "few" : "crowd";
            if (near.Count == 0) return;
            // The one that matters most: your girlfriend, then her rival, her best friend, a friend, anyone.
            var w = near.FirstOrDefault(x => x.isGirlfriend) ?? near.FirstOrDefault(x => Social.Bond(h, x) <= -30)
                    ?? near.OrderByDescending(x => Social.Bond(h, x)).First();
            var bond = Social.Bond(h, w);
            c["watcher"] = GirlInfo.FirstName(w);
            c["wrel"] = w.isGirlfriend ? "yourgf" : bond <= -30 ? "rival" : bond >= 60 ? "bff" : bond >= 25 ? "friend" : "other";
            c["wlikes"] = w.relation >= 2 || w.isGirlfriend ? "yes" : "no";
            var friends = near.Count(x => Social.Bond(h, x) >= 25);
            c["group"] = friends >= 2 ? "friends" : near.Count(x => Social.Bond(h, x) <= -30) >= 2 ? "rivals" : "mixed";
        }

        // ---------------- her line ----------------

        /// <summary>Every frame: once the game's clip is over and the list is back, her line.</summary>
        private static readonly System.Reflection.FieldInfo LadField = AccessTools.Field(typeof(TalkScene), "ladInfo");
        private static readonly System.Reflection.MethodInfo SetLeaveAlone = AccessTools.Method(typeof(TalkScene), "SetLeaveAloneTimer");
        private static float _nextArm;

        /// <summary>
        /// The game's 30 s silence timer switches itself off for good when it fires while the talk buttons are hidden (one
        /// of our scenes, a touch reaction): after the greeting she never spoke in a silence again. Once the
        /// buttons are back and nothing plays, a timer that is off is started again.
        /// </summary>
        private static void ReArmIdle(TalkScene ts)
        {
            var now = Time.realtimeSinceStartup;
            if (ts == null || now < _nextArm || LadField == null || SetLeaveAlone == null) return;
            _nextArm = now + 0.5f;
            if (_goodbye || Scenes.Running || ADV.Program.isADVProcessing || GiftMenu.IsOpen || !CanvasOn(ts)) return;
            if (TransVoiceField != null && TransVoiceField.GetValue(ts) as UnityEngine.Object != null) return;
            var lad = LadField.GetValue(ts);
            if (lad == null) return;
            var t = Traverse.Create(lad);
            var timer = t.Property("timer").GetValue() as UniRx.SingleAssignmentDisposable;
            var wait = t.Property("wait").GetValue() as UniRx.SingleAssignmentDisposable;
            if (timer != null && !timer.IsDisposed) return;
            if (wait != null && !wait.IsDisposed) return;
            SetLeaveAlone.Invoke(ts, null);
            CampusLifePlugin.Log.LogDebug("Talk: silence timer started again");
        }

        private static void TickReact(TalkScene ts)
        {
            try { ReArmIdle(ts); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk silence timer: " + e.Message); _nextArm = Time.realtimeSinceStartup + 10f; }
            var p = _pend;
            if (p == null) return;
            var now = Time.realtimeSinceStartup;
            if (ts == null || ts.targetHeroine != p.Her || _s == null || now - p.At > 20f || _goodbye) { _pend = null; return; }
            if (Scenes.Running || ADV.Program.isADVProcessing || GiftMenu.IsOpen || !CanvasOn(ts)) return;
            object voice = null;
            try { voice = TransVoiceField != null ? TransVoiceField.GetValue(ts) : null; } catch { }
            if (voice as UnityEngine.Object != null)
            {
                p.ReadyAt = 0f;
                if (OneBeat && p.Kind == "touch" && p.Speak && !p.Cut)
                {
                    CutVoice((Transform)voice);
                    p.Cut = true;
                }
                return;
            }
            if (p.ReadyAt <= 0f) { p.ReadyAt = now + (OneBeat && p.Speak ? 0.05f : 0.35f); return; }
            if (now < p.ReadyAt) return;
            _pend = null;
            try { PlayReact(p); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk reaction: " + e.Message); }
        }

        /// <summary>
        /// The game's voice for a touch our line answers goes away in the frame it was asked for, before it loads: no sound, and
        /// no caption from KK_Subtitles, which showed its text apart from our box ("I told you to stop! so persistent!", always
        /// the same; user, 2026-11-02). TouchEnd sees the voice gone and gives the talk back as usual.
        /// </summary>
        private static void CutTouchVoice(TalkScene __instance)
        {
            try
            {
                var p = _pend;
                if (p == null || !OneBeat || p.Kind != "touch" || !p.Speak || p.Cut || p.Frame != Time.frameCount) return;
                var voice = TransVoiceField != null ? TransVoiceField.GetValue(__instance) as Transform : null;
                if (voice == null) return;
                CutVoice(voice);
                p.Cut = true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Touch voice: " + e.Message); }
        }

        /// <summary>
        /// Manager.Voice.Stop(Transform) stops only the voices that follow that transform (a mouth), not the voice it returned:
        /// with TalkScene's transVoice it stopped nothing, and the game's clip played to the end under our line (2026-11-05).
        /// The voice is the object Voice.Play returned; destroying it ends the sound, its loader and its caption.
        /// </summary>
        private static void CutVoice(Transform voice)
        {
            if (voice == null) return;
            UnityEngine.Object.Destroy(voice.gameObject);
            TouchNote("voice cut");
        }

        private static int Count(string key)
        {
            int n;
            var s = _s;
            if (s == null) return 0;
            n = s.Did.Count(d => d.StartsWith(key, StringComparison.Ordinal));
            return n;
        }

        private static string PartWord(int part, bool look)
        {
            if (look) return part == 1 ? "chest" : "face";
            switch (part) { case 0: return "hand"; case 1: return "chest"; case 2: return "head"; case 3: return "cheek"; }
            return "hand";
        }

        private static void PlayReact(Pend p)
        {
            var s = _s;
            var h = p.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var key = GirlInfo.Key(h);
            var c = Ctx(h, gs, new PTopic { Kind = p.Kind });
            WatchCtx(h, c);
            string on;
            if (p.Kind == "touch")
            {
                var part = p.PartName ?? PartWord(p.Part, p.Look);
                var n = Count("react:touch:" + part) + 1;
                s.Did.Add("react:touch:" + part + ":" + n);
                // Decided with the clip (TouchClip). Our own places (butt, lips) always answer: the game says nothing for them.
                if (p.PartName == null && !p.Speak) return;
                if (p.Kiss != "") c["kiss"] = p.Kiss;
                if (p.Answer != "") c["answer"] = p.Answer;
                c["part"] = part;
                c["look"] = p.Look ? "yes" : "no";
                c["level"] = p.Level == 0 ? "welcome" : p.Level == 1 ? "uneasy" : p.Level == 2 ? "stop" : "angry";
                c["again"] = n > 1 ? "yes" : "no";
                on = "touch";
            }
            else
            {
                var n = Count("react:idle") + 1;
                s.Did.Add("react:idle:" + n);
                if (n > 3) return;   // after three she just waits with the game's own clips
                c["idle"] = n.ToString();
                on = "idle";
            }
            DlgRule rule;
            var line = Bank.Pick(on, c, key, out rule);
            if (string.IsNullOrEmpty(line))
            {
                if (p.PartName != null) { var a = SpotAfter(p); if (a != null) a(); }
                else if (p.Pose != null) PlayPose(G.TalkScene, p.Pose);
                return;
            }
            TouchNote(on + " line " + (rule != null ? rule.Id : "?"));
            var b = new SceneBuilder(h, true);
            if (p.Kiss == "yes") b.Narr(BeatSay(h, "kiss.yes.narr") ?? "...");
            else if (p.Kiss == "wary") b.Narr(BeatSay(h, "kiss.wary.narr") ?? "...");
            else if (p.Kiss == "wary_no") b.Narr(BeatSay(h, "kiss.waryno.narr") ?? "...");
            else if (p.Kiss == "no") b.Narr(BeatSay(h, "kiss.no.narr") ?? "...");
            b.Face(h, p.Kiss == "yes" ? Feel.Love : p.Kiss == "wary" || p.Kiss == "wary_no" ? Feel.Shy : FeelOf(rule, c)).Her_(line);
            // Weight: she walks off after the third silence if she barely knows you; a crowd watching you
            // touch her gets talked about; a rival watching remembers.
            Action after = p.PartName != null ? SpotAfter(p) : null;
            // walks off after the third one, whoever she is. Only the empty blue bar or your goodbye closes the talk.
            if (p.PartName == null && on == "touch" && c.ContainsKey("wrel"))
            {
                var near = Onlookers(h);
                if (c["wrel"] == "rival" && near.Count > 0) after = () => { foreach (var x in near.Where(x => Social.Bond(h, x) <= -30)) Arcs.AddBond(key, GirlInfo.Key(x), -2); };
                else if (c["wrel"] == "bff" || c["wrel"] == "friend") after = () => { foreach (var x in near.Where(x => Social.Bond(h, x) >= 25)) Arcs.AddBond(key, GirlInfo.Key(x), 1); };
            }
            // Someone who matters walks over during the second silence: her friend teases you both and the
            // story spreads, a rival sneers and pushes her closer to you, your girlfriend sees who you are with.
            SaveData.Heroine by = null;
            if (on == "idle" && c["idle"] == "2" && c.ContainsKey("wrel") && c["wrel"] != "other")
                by = Onlookers(h).FirstOrDefault(x => GirlInfo.FirstName(x) == c["watcher"]);
            // She walks over when it matters to her (context, 2026-11-07): your girlfriend, a rival, or a close friend.
            if (by != null && !by.isGirlfriend && Social.Bond(h, by) > -30 && Social.Bond(h, by) < 45) by = null;
            if (by != null)
            {
                b = new SceneBuilder(h, true);
                after = Interrupt(h, by, c["wrel"], b);
                c["interrupt"] = c["wrel"];
            }
            CampusLifePlugin.Log.LogInfo("Conversation: " + GirlInfo.FirstName(h) + ": " + on + (by != null ? " interrupted by " + GirlInfo.FirstName(by) : "") + " reaction (" + (rule != null ? rule.Id : "?") + ")");
            // A second movement with her line (another pose of the same feeling), and back to standing when it ends.
            var ts = G.TalkScene;
            var follow = by != null ? null : !string.IsNullOrEmpty(p.Pose) ? p.Pose : OtherPose(PosePool(p.Info, on, on == "touch" ? Mathf.Min(p.Level, 2) : 0));
            if (Scenes.PlayInTalk(b, _ =>
                {
                    if (after != null) try { after(); } catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
                    if (follow != null && G.TalkScene == ts) PlayPose(ts, h.isAnger ? "怒り立ち" : "通常立ち");
                    AfterScene();
                }, false))
            {
                Hide();
                if (follow != null) PlayPose(ts, follow);
            }
        }

        private static Action Interrupt(SaveData.Heroine h, SaveData.Heroine by, string wrel, SceneBuilder b)
        {
            var th = GirlInfo.ToneOf(h);
            var tb = GirlInfo.ToneOf(by);
            var hn = GirlInfo.FirstName(h);
            var bn = GirlInfo.FirstName(by);
            var kh = GirlInfo.Key(h);
            var kb = GirlInfo.Key(by);
            if (wrel == "yourgf")
            {
                b.Narr(bn + " walks over and slips her arm around yours.");
                b.Face(by, Feel.Smile).Say(by, Lines.T(tb, "Hi, " + hn + ". Mind if I borrow him? || Um, " + hn + "... could I borrow him for a moment? || Hi, " + hn + "! Sorry, do you mind if I join?" + " || " + hn + ", hi... Can I steal him for a bit?", "Heeey! What are you two talking about? || Hiii! What are you guys up to? || Hey, hey! Mind if I crash this? || Yo! What's the topic? Let me in!", "...Hey. Am I interrupting? || ...Am I interrupting something? || ...Hey. Is this a bad time? || ...Mind if I stay?", "Oh? Having fun without me? || Oh my, having fun without me? || Well, well. A private chat, hm? || Starting the party without me? Rude~"));
                b.Face(h, Feel.Worried).Say(h, Lines.T(th, "Oh! N-no, of course not. || Ah! N-no, it's okay! || Oh, of course not, it's fine! || N-no, not at all...!", "Ah! No, no, go ahead! || Nope! Not at all, go ahead! || Huh? Nah, it's cool! || Ah, sure, sure! No problem!", "...We were just talking. || ...We were only talking. || ...It's fine. || ...Nothing important.", "Relax, I wasn't stealing him. Yet. || Relax. He's still yours. For now. || Don't worry, I'm only borrowing. Probably. || Calm down~ I won't keep him. Maybe."));
                // A girl who likes you, seen with you by your girlfriend: Share decides how she takes it.
                return () => { if (Interested(h) || h.relation >= 2) Share.Learned(by, h, true); };
            }
            if (wrel == "rival")
            {
                b.Narr(bn + " passes by and stops.");
                b.Face(by, Feel.Serious).Say(by, Lines.T(tb, "Oh... it's you, " + hn + ". || Oh... " + hn + ". Hello... || ...Ah. It's you, " + hn + "." + " || " + hn + "... I didn't think I'd see you here.", "Ha! " + hn + " actually found someone to talk to? || Whoa, " + hn + " found a friend? That's new! || Hah! " + hn + ", talking to people? Big day! || No way, " + hn + " has a conversation partner?!", "...Don't let me interrupt your little chat. || ...Don't let me ruin your chat. || ...Carry on. Pretend I'm not here. || ...How nice for you, " + hn + ".", "Wow, " + hn + ". Lowering your standards? || Wow, " + hn + ". Settling for him, are we? || Oh, " + hn + ". Scraping the bottom now?" + " || " + hn + ", really? Your standards are slipping~"));
                b.Face(h, Feel.Pout).Say(h, Lines.T(th, "...Please just go away. || ...Could you please leave us alone? || ...Please, just go. || ...I don't want to talk to you right now.", "Get lost, " + bn + "! || Beat it, " + bn + "! || Go away, " + bn + "! Seriously! || Shoo, " + bn + "! Nobody invited you!", "...Are you done? || ...Are you finished? || ...Is that all? || ...You can go now.", "Jealous? Bye-bye~ || Jealous much? Bye~ || Aww, someone's bitter. See you never~ || Run along now, " + bn + "~"));
                b.Narr(bn + " clicks her tongue and leaves. " + hn + " moves a little closer to you.");
                b.Face(h, Feel.Shy).Her_(Lines.T(th, "Sorry about her. Thanks for staying. || I'm sorry about her... Thank you for not leaving. || Sorry she said that... I'm glad you stayed. || Please don't mind her... thanks for staying with me.", "Ugh! Sorry. You're way better company. || Ugh, sorry about that! You're way more fun anyway! || Sorry, sorry! Ignore her, you're the best! || Man, sorry! Glad you're still here though!", "...Thanks for not leaving. || ...Thanks for staying. || ...Sorry about that. And thanks. || ...You didn't leave. Good.", "She's just mad I'm with you. || She's just sour that I'm with you. || Ignore her. She wishes she were me right now. || Someone's jealous. Can't blame her, really."));
                return () => { Arcs.AddBond(kh, kb, -3); h.favor = Mathf.Min(100, h.favor + 2); };
            }
            // A friend of hers.
            b.Narr(bn + " sneaks up behind " + hn + ".");
            b.Face(by, Feel.Laugh).Say(by, Lines.T(tb, "Ah, sorry! Am I interrupting something? || Oh, sorry! Did I interrupt? || Ah... I'm sorry, am I in the way? || Oh! Sorry, I didn't mean to barge in...", "Ooh! What's this, what's this? A date? || Ooooh! Is this a date?! Is it?! || Whoa whoa! What's going on here?! Spill! || Hey hey! You two look cozy!", "...So this is where you were. || ...So this is where you've been. || ...Found you. || ...I see. That's where you went.", "Well, well. " + hn + ", you never told me~ || Well, well. " + hn + ", you kept this quiet~ || My, my. " + hn + ", hiding something? || Oh~ " + hn + ", you never mentioned him."));
            b.Face(h, Feel.Shy).Say(h, Lines.T(th, "I-it's not like that! || I-it's not what it looks like! || No, no! It's not like that at all! || W-we're just talking...!", "Shut up, it's not a date! || It's NOT a date! Shut up! || Ugh, quit it! It's not like that! || Stop! It's not a date, okay?!", "...Go away. || ...Leave. || ...Don't. || ...Go somewhere else.", "Maybe it is. Jealous? || And if it is? Jealous? || Maybe it is. Got a problem with that? || Who knows~ Does it bother you?"));
            b.Face(by, Feel.Smile).Say(by, Lines.T(tb, "Hehe. I'll leave you two alone, then. || Ehehe... I'll give you two some space. || Okay, I'll leave you alone, then. Bye! || Sorry for interrupting. Have fun, you two.", "Okay, okay! Have fun, lovebirds! || Alright, alright! Enjoy, lovebirds! || Okay, I'm out! Have fun! || Bye, you two! Don't be too cute!", "...Sure. See you later. || ...Fine. Later. || ...I'll go, then. || ...See you.", "Don't do anything I wouldn't do~ || Behave yourselves~ Or don't. || Have fun. Not too much fun~ || I'll be going. Don't miss me too much~"));
            // The friend tells the others: the story spreads, a little warmer between the two of them.
            return () => { Arcs.AddBond(kh, kb, 2); Social.Add(Social.Kinds.Flirted, h, new[] { by }, Witness.Flirt); };
        }
    }
}
