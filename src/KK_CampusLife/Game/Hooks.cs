using System;
using System.Collections.Generic;
using ActionGame.Communication;
using HarmonyLib;
using UniRx;
using UniRx.Triggers;

namespace Joojkk.CampusLife
{
    /// <summary>Harmony patches. Every patch is wrapped so a failure never breaks the game's own code.</summary>
    internal static class Hooks
    {
        /// <summary>A talk scene was created: remember it (saves G.TalkScene a search over the whole scene).</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkScene), "Awake")]
        private static void TalkSceneAwake(TalkScene __instance)
        {
            try { G.OnTalkSceneAwake(__instance); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>A conversation closed: report who it was with and how it ended.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(TalkScene), "OnDestroy")]
        private static void TalkSceneDestroyed(TalkScene __instance)
        {
            try
            {
                GiftMenu.Close();
                Chat.Close();
                if (__instance == null || !Campus.On) return;
                var h = __instance.targetHeroine;
                var info = __instance.resultInfo;
                if (h == null) return;
                Campus.OnTalkEnd(h, info != null ? info.result : ResultEnum.None);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
            }
        }

        /// <summary>Before the clipboard builds its cards: every student must point at her own body.</summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaStatusScene), "Start")]
        private static void StatusSceneStarting(ChaStatusScene __instance)
        {
            try
            {
                StatusUi.Starting = __instance;
                StoryCast.RepairRoots(G.Act);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
            }
        }

        /// <summary>The clipboard (Character Status) opened: add Campus Life pages, cards and the menu.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaStatusScene), "Start")]
        private static void StatusSceneStarted(ChaStatusScene __instance)
        {
            try
            {
                StatusUi.OnStart(__instance);
                __instance.OnDestroyAsObservable().Subscribe(_ => StatusUi.OnDestroy());
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
            }
        }

        /// <summary>Today's story heroine gets her next event whatever the weekly schedule says.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.FindFixEventHeroine))]
        private static void FindFixEventHeroinePostfix(ActionScene __instance, ref SaveData.Heroine __result, ref ActionGame.FixEventScheduler.Result result)
        {
            try { StoryCast.AfterFindFix(__instance, ref __result, ref result); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        /// <summary>
        /// Every interaction with a character on the map goes through ActionScene._SceneEvent. The game sends story
        /// heroines (fixCharaID &lt; 0) to the fixed-event path, which needs the period's Fix object; for one walking around
        /// as a student that would lock the player, so the click is dropped (null makes the game ignore it).
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionScene), "_SceneEvent")]
        private static void SceneEventPrefix(ref ActionGame.Chara.Base chara)
        {
            try { StoryCast.GuardInteraction(ref chara); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        /// <summary>After the students of a period are loaded, the story heroines join them.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionScene), nameof(ActionScene.NPCLoadAll))]
        private static void NPCLoadAllPostfix(ActionScene __instance, ref System.Collections.IEnumerator __result)
        {
            try { __result = StoryCast.Wrap(__instance, __result); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        /// <summary>A text line is shown: its painted background (if the scene set one), and its voice if voice is on.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ADV.Commands.Base.Text), "Do")]
        private static void AdvTextShown(ADV.Commands.Base.Text __instance)
        {
            try
            {
                if (!Scenes.Running || __instance == null || __instance.args == null || __instance.args.Length < 2) return;
                var text = __instance.args[1];
                string bg;
                if (!string.IsNullOrEmpty(text) && Scenes.CurrentBackgrounds.TryGetValue(text, out bg)) EventBackdrop.Show(bg, __instance.scenario);
                List<FaceCue> faces;
                if (!string.IsNullOrEmpty(text) && Scenes.CurrentFaces.TryGetValue(text, out faces)) Expressions.Apply(faces);
                if (!Cfg.VoiceEnabled.Value) return;
                VoiceTag tag;
                if (string.IsNullOrEmpty(text) || !Scenes.CurrentVoices.TryGetValue(text, out tag)) return;
                Tts.OnLine(text, tag);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug(e);
            }
        }
    }
}
