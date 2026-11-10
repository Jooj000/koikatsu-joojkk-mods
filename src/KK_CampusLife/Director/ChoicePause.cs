using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The world waits while you choose. A scene on the map (an ADV over the action scene) showing its options holds the
    /// period clock where it was and has the director stop every girl on your map (Director.Halt: her AI goes on, she does
    /// not walk off); once you answer, the clock runs from the same second and they go on. The talk screen already stops
    /// the world (the game's own).
    /// </summary>
    internal static class ChoicePause
    {
        private static float _clock = -1f;
        private static readonly List<NPC> _mine = new List<NPC>();
        private static System.Reflection.FieldInfo _timer;
        private static float _since;

        public static bool On { get { return _clock >= 0f; } }

        private static bool ChoiceShown()
        {
            try
            {
                var act = G.Act;
                if (act == null || G.TalkSceneOpen) return false;
                var adv = act.AdvScene;
                return adv != null && adv.isActiveAndEnabled && adv.Scenario != null && adv.Scenario.isChoice;
            }
            catch { return false; }
        }

        /// <summary>Every frame (plugin Update).</summary>
        public static void Tick()
        {
            try
            {
                var shown = ChoiceShown();
                if (_timer == null) _timer = HarmonyLib.AccessTools.Field(typeof(ActionGame.Cycle), "_timer");
                var cy = G.Cycle;
                if (shown && !On)
                {
                    _clock = cy != null && _timer != null ? (float)_timer.GetValue(cy) : 0f;
                    _since = Time.realtimeSinceStartup;
                    _mine.Clear();
                    var map = G.PlayerMapNo;
                    if (G.Act != null && G.Act.npcList != null)
                        foreach (var n in G.Act.npcList.Where(x => x != null && x.heroine != null && x.mapNo == map && !x.isLesH && !x.isOnanism && !Director.Halted(x.heroine)))
                        {
                            Director.Halt(n, "waiting on your answer");
                            _mine.Add(n);
                        }
                    CampusLifePlugin.Log.LogInfo("ChoicePause: on (clock " + _clock.ToString("0") + ", " + _mine.Count + " girls stopped)");
                }
                else if (shown)
                {
                    if (cy != null && _timer != null && !cy.isActionEnd && (float)_timer.GetValue(cy) > _clock) _timer.SetValue(cy, _clock);
                }
                else if (On)
                {
                    foreach (var n in _mine) Director.Release(n);
                    CampusLifePlugin.Log.LogInfo("ChoicePause: off after " + (Time.realtimeSinceStartup - _since).ToString("0.0") + " s (" + _mine.Count + " girls go on)");
                    _mine.Clear();
                    _clock = -1f;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("ChoicePause: " + e.Message); _clock = -1f; }
        }
    }
}
