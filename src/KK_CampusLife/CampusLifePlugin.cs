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
        public const string Version = "1.7.0";

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
                InviteOdds.Install(harmony);
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
            Tts.Init();
#if DEBUG_TOOLS
            AutoTest.Init(gameObject);
#endif
        }

        private void Update()
        {
            try
            {
                ClassLoan.Sweep();
                Scenes.Watchdog();
                Tts.Update();
                TransitionScreen.Update();
                ToastUi.Update();
                StatusUi.WatchBroken();
                if (!Cfg.Enabled.Value) return;
                if (Campus.On)
                {
                    if (Cfg.PlannerKey.Value.IsDown()) ToggleMenu();
                    Calendar.Tick();
                    G.StretchPeriod();
                    Chat.Track();
                    Conversation.Watch();
                    PositionTool.Update();
                    Markers.UpdateMinimap();
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

        private void OnDestroy()
        {
            Tts.Shutdown();
        }
    }
}
