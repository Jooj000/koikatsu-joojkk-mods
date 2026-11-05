using System;
using System.Collections;
using System.Linq;
using ActionGame.Chara;
using KKAPI.MainGame;
using KKAPI.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A girl asleep on an infirmary bed: the game only says she is sleeping and ends the talk. Now,
    /// once that is over, you can tease her awake; half asleep she gives in far more easily than awake, and if she does,
    /// the game's H starts where she lies (as a girl can wake you with her hand when you nap there).
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
                    if (Places.CurrentId != "infirmary") return;
                    _pending = h;
                    return;
                }
                _seen = null;
                if (_pending == null) return;
                var her = _pending;
                _pending = null;
                CampusLifePlugin.Instance.StartCoroutine(Offer(her).PreventFromCrashing());
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Sleep tease: " + e.Message); }
        }

        private static IDisposable _icon;
        private static SaveData.Heroine _iconFor;
        internal static SaveData.Heroine IconFor { get { return _iconFor; } }
        private static int _iconMap = -1;
        private static float _nextLook;
        private static readonly float[] Purple = { 0.6f, 0.3f, 0.86f };
        private static Sprite _on, _off;

        /// <summary>
        /// ícone, faça um diabinho roxo." While a girl sleeps on an infirmary bed (the game's sleep action), an activity
        /// icon with a purple imp stands by her; walking into it and pressing it offers the tease (the old way, after the
        /// game's own "she is sleeping" talk, stays too). The icon goes when she wakes or you leave.
        /// </summary>
        public static void Update()
        {
            if (Time.realtimeSinceStartup < _nextLook) return;
            _nextLook = Time.realtimeSinceStartup + 0.5f;
            try
            {
                var act = G.Act;
                NPC her = null;
                if (Campus.On && Cfg.Enabled.Value && act != null && act.Map != null && act.npcList != null && Places.CurrentId == "infirmary" && !G.TalkSceneOpen)
                {
                    her = act.npcList.FirstOrDefault(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && GirlInfo.IsRegular(n.heroine)
                                                          && n.AI != null && n.AI.actionNo == SleepAction && n.isArrival);
                    var near = act.npcList.FirstOrDefault(n => n != null && n.heroine != null && n.mapNo == act.Map.no && n.AI != null && n.AI.actionNo == SleepAction);
                    Why = her != null ? "asleep: " + GirlInfo.FirstName(her.heroine)
                        : near == null ? "nobody here with the sleep action"
                        : GirlInfo.FirstName(near.heroine) + ": active " + near.isActive + ", regular " + GirlInfo.IsRegular(near.heroine) + ", arrived " + near.isArrival;
                }
                else Why = "not in the infirmary (" + Places.CurrentId + "), talk open " + G.TalkSceneOpen;
                var h = her != null ? her.heroine : null;
                if (h == _iconFor && (h == null || _iconMap == act.Map.no)) return;
                Drop();
                if (h == null) return;
                if (_on == null) _on = MapSprite.Make(IconArt.Spot("devil", Purple, 128, true), 128, 100f, "cl_tease_on");
                if (_off == null) _off = MapSprite.Make(IconArt.Spot("devil", Purple, 128, false), 128, 100f, "cl_tease_off");
                var pos = her.position + her.transform.right * 0.9f;
                var target = h;
                _icon = GameAPI.AddActionIcon(act.Map.no, pos, _on, _off, () =>
                {
                    try { Drop(); CampusLifePlugin.Instance.StartCoroutine(Offer(target).PreventFromCrashing()); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                }, null, true, true);
                _iconFor = h;
                _iconMap = act.Map.no;
                CampusLifePlugin.Log.LogInfo("Sleep tease: " + GirlInfo.FirstName(h) + " asleep in the infirmary, imp icon at " + pos + ".");
            }
            catch (Exception e) { Why = "error: " + e.Message; if (_warned++ < 3) CampusLifePlugin.Log.LogWarning("Sleep tease icon: " + e); Drop(); }
        }

        /// <summary>The game's AI action for sleeping in the infirmary (Director.Intents.SleepAction).</summary>
        private const int SleepAction = 22;

        internal static string Why = "";
        private static int _warned;

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

        private static IEnumerator Offer(SaveData.Heroine h)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 8f && !Scenes.CanPlayOnMap) yield return null;
            if (!Scenes.CanPlayOnMap || Truth.NpcOf(h) == null) yield break;
            var name = GirlInfo.FirstName(h);
            var t = GirlInfo.ToneOf(h);
            var chance = Chance(h);
            var b = new SceneBuilder(null, false);
            b.Narr(Conversation.BeatSay(h, "sleeptease.asleep") ?? "...");
            b.Choice("Tease her awake.", "tease", "Let her sleep.", "leave");
            b.Branch("leave");
            b.Narr(Conversation.BeatSay(h, "sleeptease.leave") ?? "...");
            b.End();
            var yes = Consent.Yes(h, "sleeptease", chance);
            b.Branch("tease");
            b.Narr(Conversation.BeatSay(h, "sleeptease.tease") ?? "...");
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
            Scenes.PlayOnMap(b, c =>
            {
                if (c != "tease") return;
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
        }
    }
}
