using System;
using System.Collections;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using ActionGame.Communication;
using HarmonyLib;
using Illusion.Component;
using KKAPI.Utilities;
using Manager;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// A professor's everyday talk is the same conversation as anyone's, with her own lines ("substituindo o sistema do
    /// jogo de spammar conversa"). The game's talk scene is reused, walking or at her event spot. Its tables are by
    /// personality and a teacher's id is not in them, so during the talk she answers with her personality.
    /// </summary>
    internal static class TeacherTalk
    {
        /// <summary>The teacher in the talk scene right now.</summary>
        public static SaveData.Heroine Speaking;

        private const string SceneName = "Talk";

        /// <summary>What the game's _SceneEvent does around a talk, which a teacher never reaches.</summary>
        public static IEnumerator Run(Base chara)
        {
            var act = G.Act;
            var tr = Traverse.Create(act);
            var prev = string.Empty;
            var volume = 1f;
            var now = act.Cycle.nowType;
            tr.Field("_isInChargeBGM").SetValue(true);
            act.Player.isActionNow = true;
            tr.Property("isEventNow").SetValue(true);
            act.Player.isLesMotionPlay = false;
            Traverse.Create(act).Property("shortcutKey").SetValue(false);
            yield return act.StartCoroutine(Inner(chara, act, (b, v) => { prev = b; volume = v; }).PreventFromCrashing());
            Speaking = null;
            if (tr.Field("_isInChargeBGM").GetValue<bool>() && act.Cycle.isAction && now == act.Cycle.nowType)
                yield return act.StartCoroutine(Illusion.Game.Utils.Sound.GetFadePlayerWhileNull(prev, volume));
            Traverse.Create(act).Property("shortcutKey").SetValue(true);
            tr.Field("_isInChargeBGM").SetValue(false);
            tr.Property("isEventNow").SetValue(false);
            act.Player.isActionNow = false;
        }

        private static IEnumerator Inner(Base chara, ActionScene act, Action<string, float> bgm)
        {
            yield return act.StartCoroutine(Illusion.Game.Utils.Sound.GetBGMandVolume(bgm));
            yield return null;
            yield return new WaitUntil(() => Singleton<Scene>.Instance.AddSceneName.IsNullOrEmpty());
            act.Player.ChaserEnd();
            Speaking = chara.heroine;
            act.StartCoroutine(Watch());
            var npc = chara as NPC;
            if (npc != null)
            {
                var e = Traverse.Create(act).Method("_SceneEventNPC", new[] { typeof(NPC) }).GetValue<IEnumerator>(npc);
                yield return act.StartCoroutine(e.PreventFromCrashing());
            }
            else yield return act.StartCoroutine(AtHerSpot(act, chara as Fix).PreventFromCrashing());
        }

        /// <summary>She is herself again as soon as the talk closes: what follows (H, the map) wants her real id.</summary>
        private static IEnumerator Watch()
        {
            var t0 = Time.realtimeSinceStartup;
            while (UnityEngine.Object.FindObjectOfType<TalkScene>() == null && Time.realtimeSinceStartup - t0 < 30f) yield return null;
            while (UnityEngine.Object.FindObjectOfType<TalkScene>() != null) yield return null;
            Speaking = null;
        }

        /// <summary>_SceneEventNPC for the period's Fix object: same talk scene, and only what can come out of it for her.</summary>
        private static IEnumerator AtHerSpot(ActionScene act, Fix fix)
        {
            var h = fix.heroine;
            act.Player.move.isStop = true;
            TalkScene talk = null;
            ResultInfo result = null;
            var opened = false;
            Singleton<Scene>.Instance.LoadReserve(new Scene.Data
            {
                assetBundleName = "action/menu/talk.unity3d",
                levelName = SceneName,
                isAdd = true,
                onLoad = () =>
                {
                    Traverse.Create(act).Property("shortcutKey").SetValue(true);
                    talk = Scene.GetRootComponent<TalkScene>(SceneName);
                    talk.targetHeroine = h;
                    talk.otherInitialize += () =>
                    {
                        opened = true;
                        act.MiniMapAndCameraActive = false;
                        act.npcList.ForEach(p => p.SetActive(false));
                        act.npcList.ForEach(p => p.Pause(true));
                        fix.SetActive(false);
                        act.Player.SetActive(false);
                    };
                    talk.necessaryInfo = new NecessaryInfo(act.Map.no, act.Cycle.nowType, false, false, !act.Map.Info.is2D, true, true, 0, false, false, false, act.Player.isChaseWarning, false, new[] { act.Map.Info.MapName });
                    talk.OnDestroyAsObservable().Subscribe(_ => result = talk.resultInfo);
                }
            }, false);
            yield return new WaitWhile(() => result == null);
            act.Player.move.isStop = false;
            if (result.result == ResultEnum.H)
            {
                yield return act.StartCoroutine(act.ChangeH(null, false, false, fix));
                yield break;
            }
            if (h.chaCtrl != null && h.chaCtrl.hiPoly)
            {
                Singleton<Character>.Instance.DeleteChara(h.chaCtrl, false);
                yield return new WaitUntil(() => h.chaCtrl == null);
            }
            h.SetRoot(fix.gameObject);
            act.Player.SetActive(true);
            act.npcList.ForEach(p => p.SetActive(p.mapNo == act.Map.no));
            act.npcList.ForEach(p => p.Pause(false));
            fix.SetActive(fix.mapNo == act.Map.no);
            if (opened) act.CameraState.SetAngle(act.Player.eulerAngles);
            Singleton<Scene>.Instance.SetFadeColorDefault();
        }
    }
}
