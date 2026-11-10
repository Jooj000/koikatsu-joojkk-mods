using System;
using System.Collections;
using ActionGame.Chara;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Talking to one of two girls who are together pulls in the other. The pair the director gathered on the map
    /// (Director.Gather), or two in the game's own chat (Truth.ChatPartner) within 4 m: the talk opens with the one you
    /// clicked, the other stands beside her as in a duo scene, and the talk with both starts at once in their mood (an
    /// argument stays an argument). When it closes, the second girl goes on with her day.
    /// </summary>
    internal static partial class Duo
    {
        /// <summary>From the conversation hook, when a talk with <paramref name="a"/> opens.</summary>
        internal static void PullIn(TalkScene ts, SaveData.Heroine a)
        {
            try
            {
                if (Busy || TrioPending || !Cfg.TrioTalk.Value || ts == null || a == null || !GirlInfo.IsRegular(a)) return;
                var na = Director.NpcOf(a);
                if (na == null) return;
                var nb = Director.GatherPartner(a);
                var moment = Director.GatherMoment(a);
                if (nb == null)
                {
                    var ph = Truth.ChatPartner(na);
                    nb = ph != null ? Director.NpcOf(ph) : null;
                    moment = nb != null && Social.Bond(a, ph) <= -30 ? "quarrel" : "hangout";
                }
                if (nb == null || nb.heroine == null || nb.heroine == a || !GirlInfo.IsRegular(nb.heroine) || nb.mapNo != na.mapNo) return;
                if ((nb.transform.position - na.transform.position).sqrMagnitude > 4f * 4f) return;
                Busy = true;
                TrioPending = true;
                CampusLifePlugin.Instance.StartCoroutine(PullRun(ts, na, nb, moment));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo pull in: " + e.Message); }
        }

        private static IEnumerator PullRun(TalkScene ts, NPC na, NPC nb, string moment)
        {
            var on = false;
            var end = "";
            Step("pull in: talking to " + GirlInfo.FirstName(na.heroine) + " brings " + GirlInfo.FirstName(nb.heroine) + " (" + moment + ")");
            // The director held them facing each other; the talk scene places them now.
            Director.Release(na);
            Director.Release(nb);
            yield return Wait(() => Ready(ts) || G.TalkScene != ts, 40f);
            if (G.TalkScene == ts && Ready(ts))
            {
                try { StandBeside(nb, na); }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo pull beside: " + e.Message); }
                yield return Wait(() => MenuShown(ts) || G.TalkScene != ts, 25f); // her greeting first
                try { if (G.TalkScene == ts) StandBeside(nb, na); }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo pull beside: " + e.Message); }
                TrioPending = false;
                if (G.TalkScene == ts)
                {
                    try { on = Conversation.TrioBegin(ts, nb, moment); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                }
                Step(on ? "pull in: the talk goes on with both" : "pull in: the talk with both did not start");
                if (on)
                {
                    try { Director.GatherTalked(na.heroine); Social.RecordTalk(nb.heroine); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                    yield return Wait(() => !Conversation.TrioOn || G.TalkScene != ts, 1800f);
                    end = Conversation.TrioEnding;
                    Step("pull in: talk with both over: " + end);
                }
                else
                {
                    TrioStage.Unframe();
                    KeepStill(null);
                }
            }
            TrioPending = false;
            yield return Wait(() => G.TalkScene == null && G.IsFreeRoaming, 1800f);
            if (end == "h3") yield return CampusLifePlugin.Instance.StartCoroutine(H3(na, nb));
            else
            {
                try { if (end == "bleft" && !nb.gameObject.activeSelf) nb.SetActive(true); }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Duo pull B back: " + e.Message); }
                Release(null, nb, new bool[2]);
                if (end == "aleft" && G.IsFreeRoaming && nb.heroine.talkTime > 0)
                {
                    // She stayed when the other walked off: the talk goes on with her alone, as in a duo scene.
                    G.Act.SceneEvent(nb);
                    yield return Wait(() => Ready(G.TalkScene), 40f);
                    if (Ready(G.TalkScene)) yield return Wait(() => G.TalkScene == null && G.IsFreeRoaming, 1800f);
                }
            }
            _besideAgentOff = null;
            Busy = false;
            Step("pull in: done (" + (end == "" ? "no talk with both" : end) + ")");
        }
    }
}
