using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// The game's seat and locker moves read the objects of the map, and one of them gone throws in the middle of the end of
        /// a talk: the screen stays on an empty map with the event never finished. The list is read again here.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionGame.Chara.Base), "KindMoveShift")]
        private static void FreshMapObjects(ActionGame.Chara.Base __instance)
        {
            try
            {
                var map = __instance.actScene != null ? __instance.actScene.Map : null;
                if (map == null || map.mapObjects == null || map.mapObjects.All(k => k != null)) return;
                CampusLifePlugin.Log.LogWarning("Map " + map.no + ": objects gone from the list, read again");
                var group = map.mapRoot != null ? map.mapRoot.transform.Find("MapObjectGroup") : null;
                Traverse.Create(map).Property("mapObjects").SetValue(group != null ? group.GetComponentsInChildren<ActionGame.MapObject.Kind>(true) : new ActionGame.MapObject.Kind[0]);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionGame.ActionMap), "Reserve")]
        private static void WatchReserve(ActionGame.ActionMap __instance)
        {
            try
            {
                if (__instance.infoDic != null && __instance.infoDic.ContainsKey(__instance.no)) return;
                CampusLifePlugin.Log.LogWarning("ActionMap.Reserve without info: no " + __instance.no + ", prev " + __instance.prevNo + ", loading " + __instance.isMapLoading + ", main " + (G.Act != null && G.Act.Map == __instance) + ", on " + __instance.gameObject.name);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>
        /// The talk scene stands its own copy of her where she was and hides every map body. Nothing may show her map body
        /// again while the talk is open (a map warp, a duo letting go, a timer): her two bodies stood on top of each other
        /// (user, 2026-10-12). The game shows her again itself once the talk has handed her root back.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ActionGame.Chara.Base), "SetActive")]
        private static void BodyShown(ActionGame.Chara.Base __instance, ref bool active)
        {
            try
            {
                if (!active || __instance == null) return;
                var h = __instance.heroine;
                if (h == null || h.chaCtrl == null || h.chaCtrl == __instance.chaCtrl) return;
                var ts = G.TalkScene;
                if (ts == null || ts.targetHeroine != h) return;
                active = false;
                CampusLifePlugin.Log.LogInfo("Talk: kept " + GirlInfo.FirstName(h) + "'s map body hidden (the talk scene has her).");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
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

        /// <summary>The talk scene's tables are by personality: a teacher in it answers with hers (TeacherTalk).</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(SaveData.Heroine), nameof(SaveData.Heroine.FixCharaIDOrPersonality), MethodType.Getter)]
        private static void FixCharaIDOrPersonalityPostfix(SaveData.Heroine __instance, ref int __result)
        {
            if (TeacherTalk.Speaking == __instance) __result = __instance.personality;
        }

        /// <summary>Her greeting comes from the dialogue bank.</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.Communication.Info), nameof(ActionGame.Communication.Info.GetIntroductionADV))]
        private static void GetIntroductionPostfix(ActionGame.Communication.Info __instance, bool _isEnd, int _eventNo, List<ADV.Program.Transfer> __result)
        {
            try { Conversation.Greeting(Traverse.Create(__instance).Property("heroine").GetValue<SaveData.Heroine>(), _isEnd, _eventNo, __result); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Greeting: " + e.Message); }
        }

        /// <summary>A teacher stays at her desk (StaffLife).</summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ActionGame.ActionControl), nameof(ActionGame.ActionControl.GetActionTime))]
        private static void GetActionTimePostfix(int _id, ref float __result)
        {
            StaffLife.DeskTime(_id, ref __result);
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
                LineVoice.OnLine(text, tag);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug(e);
            }
        }
        /// <summary>
        /// muito"). The game picks the result row by how it ended (HSceneProc.EndProc: 6/7 no climax, 8 no finish, 13 a
        /// girl with no card id and no climax) and draws a favor change from it that can be negative; for those endings a
        /// loss becomes zero. Gains stay.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSceneResult), "LoadResult")]
        private static void HResultNoLoss(HSceneResult __instance)
        {
            try
            {
                var t = Traverse.Create(__instance);
                var end = t.Field("finishADV").GetValue<int>();
                var add = t.Field("addFavor").GetValue<int>();
                if (add < 0 && (end == 6 || end == 7 || end == 8 || end == 13))
                {
                    t.Field("addFavor").SetValue(0);
                    CampusLifePlugin.Log.LogInfo("H result: ending " + end + " would cost " + (-add) + " favor; kept at 0");
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>
        /// de convidar alguém assistindo. saiu para a tela de conversa pós H, e loading infinito"): the game only checks it
        /// for the one-girl modes, so in 3P it stayed on and led to an endless loading. Hidden there; two girls together
        /// (lesbian) still get it, which is the game's own way to join them.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HSprite), nameof(HSprite.InitPointMenuAndHelp))]
        private static void HNoInviteIn3P(HSprite __instance)
        {
            try
            {
                if (__instance == null || __instance.flags == null || __instance.btnTrespassing == null) return;
                var m = __instance.flags.mode;
                var three = m == HFlag.EMode.houshi3P || m == HFlag.EMode.sonyu3P || m == HFlag.EMode.houshi3PMMF || m == HFlag.EMode.sonyu3PMMF
                            || __instance.flags.lstHeroine != null && __instance.flags.lstHeroine.Count >= 2 && m != HFlag.EMode.lesbian;
                if (three && __instance.btnTrespassing.gameObject.activeSelf)
                {
                    __instance.btnTrespassing.gameObject.SetActive(false);
                    CampusLifePlugin.Log.LogInfo("H: invite button hidden in " + m);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
        }

        /// <summary>
        /// ao invés do padrão, quando não são eventos"). The game gives every fixed character the grey "?" frame with no
        /// gauges; a teacher with Campus Life's conversations is a regular girl to talk to, so outside a step of her event
        /// (MilestoneDue) she gets the usual frame, relation icon, favor and lewdness, as the game draws them for a student.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ParamUI), nameof(ParamUI.SetHeroine))]
        private static void TeacherParamUi(ParamUI __instance, SaveData.Heroine _heroine)
        {
            try
            {
                if (__instance == null || _heroine == null || !_heroine.isTeacher || !Campus.On || !Cfg.TeacherTalks.Value || Teachers.MilestoneDue(_heroine)) return;
                var t = Traverse.Create(__instance);
                var frame = t.Field("imageFrame").GetValue<UnityEngine.UI.Image>();
                var frames = t.Field("spriteFrame").GetValue<UnityEngine.Sprite[]>();
                var state = t.Field("imageState").GetValue<UnityEngine.UI.Image>();
                var states = t.Field("spriteState").GetValue<UnityEngine.Sprite[]>();
                var favor = t.Field("imageFavor").GetValue<UnityEngine.UI.Image[]>();
                var lewd = t.Field("imageLewdness").GetValue<UnityEngine.UI.Image[]>();
                var num = UnityEngine.Mathf.Max(0, UnityEngine.Mathf.Min(2, _heroine.relation));
                if (frame != null && frames != null && frames.Length > 0) frame.sprite = frames[0];
                if (state != null && states != null && num * 2 + 1 < states.Length) { state.sprite = states[num * 2 + (_heroine.isAnger ? 1 : 0)]; state.enabled = true; }
                if (favor != null && favor.Length >= 3)
                    for (var i = 0; i < 3; i++) favor[i].fillAmount = i < num ? 1f : i == num ? UnityEngine.Mathf.InverseLerp(0f, 100f, _heroine.favor) : 0f;
                if (lewd != null)
                {
                    foreach (var im in lewd) if (im != null) im.enabled = true;
                    if (lewd.Length > 1) lewd[1].fillAmount = UnityEngine.Mathf.InverseLerp(0f, 100f, _heroine.lewdness);
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Teacher status bar: " + e.Message); }
        }
    }
}
