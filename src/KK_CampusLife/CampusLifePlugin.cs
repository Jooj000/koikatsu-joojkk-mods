using System;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Campus Life: turns the main game's school into a living place.
    /// Club board requests, moods, gifts, activities and jobs, short stories, school events and everyday happenings.
    /// Author: Joojkk.
    /// </summary>
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.30.1")]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInDependency("joojkk.schooloptimizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    public sealed class CampusLifePlugin : BaseUnityPlugin
    {
        public const string GUID = "joojkk.campuslife";
        public const string PluginName = "Campus Life";
        public const string Version = "3.2.0";

        internal static CampusLifePlugin Instance;
        internal static ManualLogSource Log;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Cfg.Init(Config);
            try
            {
                var harmony = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
                ClassLoan.Install(harmony);
                ActivitySpots.Install(harmony);
                try { Consent.Install(harmony); }
                catch (Exception e) { Log.LogWarning("Director answers not installed, the game answers: " + e.Message); }
                Conversation.InstallReact(harmony);
                Conversation.InstallTouchMore(harmony);
                RadarMap.Install(harmony);
                try { Reactions.Install(harmony); }
                catch (Exception e) { Log.LogWarning("Girls react as in the game: " + e.Message); }
                try { Brain.Install(harmony); }
                catch (Exception e) { Log.LogError(e); }
                try { Intents.Install(harmony); }
                catch (Exception e) { Log.LogWarning("Students keep the game's own movement: " + e.Message); }
                try { Sunday.Install(harmony); }
                catch (Exception e) { Log.LogWarning("Sunday stories not available: " + e.Message); }
                CardTraits.Install(harmony);
                try { TransitionScreen.Install(harmony); }
                catch (Exception e) { Log.LogWarning("Loading screen destination not available: " + e.Message); }
            }
            catch (Exception e)
            {
                Log.LogError("Some Campus Life hooks could not be installed: " + e);
            }
            GameAPI.RegisterExtraBehaviour<CampusController>(GUID);
            TalkButtons.Register();
            OptimizerLink.Register();
#if DEBUG_TOOLS
            AutoTest.Init(gameObject);
#endif
        }

        /// <summary>After the game's own camera of the talk screen: the frame with both girls (TrioStage).</summary>
        private void LateUpdate()
        {
            try { if (TrioStage.On) TrioStage.LateTick(Conversation.TrioUiVisible); }
            catch (Exception e) { Log.LogDebug("Trio camera: " + e.Message); }
        }

        private void Update()
        {
            try
            {
                ClassLoan.Sweep();
                Scenes.Watchdog();
                Forms.KeepHeld();
                Reactions.Tick();
                Consent.Tick();
                Stage.Tick();
                Sunday.Tick();
                Achievements.Tick();
                TransitionScreen.Update();
                ToastUi.Update();
                AchievementPopup.Update();
                SpotHint.Update();
                WalkHint.Update();
                StatusUi.WatchBroken();
                Weather.Update();
                if (!Cfg.Enabled.Value) return;
                if (Campus.On)
                {
                    if (Cfg.PlannerKey.Value.IsDown()) ToggleMenu();
                    Calendar.Tick();
                    G.StretchPeriod();
                    Chat.Track();
                    Conversation.Watch();
                    TalkBadges.Update();
                    PositionTool.Update();
                    Markers.UpdateMinimap();
                    RadarMap.Update();
                    CafeteriaStore.Update();
                }
                if (GiftMenu.IsOpen && !G.TalkSceneOpen) GiftMenu.Close();
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }

        /// <summary>Menu hotkey (F7): open the Campus Life menu in the clipboard, or close it.</summary>
        private static void ToggleMenu()
        {
            if (StatusUi.IsOpen)
            {
                if (CampusPanel.Visible) StatusUi.Close();
                else CampusPanel.Show(PanelTab.Today);
                return;
            }
            StatusUi.Open(PanelTab.Today);
        }

        private void OnGUI()
        {
            try { if (Campus.On) PositionTool.OnGUI(); }
            catch (Exception e) { Log.LogDebug(e); }
        }

    }
}
