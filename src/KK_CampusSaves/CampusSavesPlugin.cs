using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI;
using KKAPI.MainGame;
using UnityEngine;

namespace Joojkk.CampusSaves
{
    /// <summary>
    /// One slot per campus: the slot stays a normal save of the game, and every save also becomes a version in a vault next
    /// to it, stored by pieces. Adds saving during the day and autosave. Author: Joojkk.
    /// </summary>
    [BepInPlugin(GUID, "Campus Saves", Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.30.1")]
    [BepInDependency(ExtendedSave.GUID)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    public sealed class CampusSavesPlugin : BaseUnityPlugin
    {
        public const string GUID = "joojkk.campussaves";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<int> Interval, KeepAutos;
        internal static ConfigEntry<bool> OnPeriod, OnDayEnd, AfterH;
        internal static ConfigEntry<KeyboardShortcut> SaveKey, PanelKey;

        private static string _toast = "";
        private static float _toastUntil;

        private void Awake()
        {
            Log = Logger;
            Interval = Config.Bind("Autosave", "Minutes between saves", 10, new ConfigDescription("0 turns the timer off. It waits for a calm moment and only counts if something happened.", new AcceptableValueRange<int>(0, 60)));
            OnPeriod = Config.Bind("Autosave", "On period change", true, "Saves once you are free on the map in the new period.");
            OnDayEnd = Config.Bind("Autosave", "At the end of the day", true, "Saves when the night menu opens.");
            AfterH = Config.Bind("Autosave", "After H", true, "Saves when you are free on the map again.");
            KeepAutos = Config.Bind("Autosave", "Recent automatic saves kept", 10, new ConfigDescription("Older ones thin out to one per hour, then one per day.", new AcceptableValueRange<int>(3, 50)));
            SaveKey = Config.Bind("Keys", "Save now", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl), "Saves during the day when you are free on the map.");
            PanelKey = Config.Bind("Keys", "Open the saves panel", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl, KeyCode.LeftShift), "Campuses, versions, import and normalize.");

            Campuses.Scan();
            SaveFlow.Recover();
            Harmony.CreateAndPatchAll(typeof(SaveFlow), GUID);
            GameAPI.RegisterExtraBehaviour<SavesController>(GUID);
            gameObject.AddComponent<SavesPanel>();
        }

        private void Update()
        {
            try
            {
                if (SaveKey.Value.IsDown()) Toast(SaveFlow.SaveNow("manual", "hotkey"));
                SaveFlow.Tick();
            }
            catch (Exception e) { Log.LogWarning(e.Message); }
        }

        internal static void Toast(string s)
        {
            _toast = s;
            _toastUntil = Time.realtimeSinceStartup + 2.5f;
        }

        private void OnGUI()
        {
            if (Time.realtimeSinceStartup > _toastUntil) return;
            GUI.Box(new Rect(Screen.width - 230, 96, 214, 26), _toast);
        }
    }

    public static class SavesApi
    {
        public static string SaveNow(string kind) { return SaveFlow.SaveNow(string.IsNullOrEmpty(kind) ? "manual" : kind, "test"); }

        public static string Info(string unused)
        {
            var s = "";
            foreach (var c in Campuses.All)
            {
                s += c.Name + " slot " + c.Slot + " pack " + (c.Vault.PackSize / 1000000f).ToString("0.0") + " MB:";
                foreach (var v in c.Vault.Versions) s += " [" + v.Id + " " + v.Kind + " " + v.Trigger + " " + v.Name + " resume '" + v.Resume + "']";
                s += "\n";
            }
            return s + "campus now " + SaveFlow.CampusId + ", busy " + Campuses.Busy + ", safe " + SaveFlow.Safe() + ", " + Campuses.Status;
        }

        /// <summary>From the title: the newest version made during the day, of the only campus a test has.</summary>
        public static string LoadDay(string unused)
        {
            var c = Campuses.All[0];
            var v = c.Vault.Versions.Last(x => x.Resume != "");
            return v.Id + " " + v.Resume + ": " + SavesPanel.LoadVersion(c.Id, v.Id);
        }

        public static string Panel(string on) { SavesPanel.Shown = on != "off"; SavesPanel.Selected = Campuses.All.FirstOrDefault(); return "panel " + SavesPanel.Shown; }
        public static string ImportOne(string slot) { var c = Campuses.All[0]; Campuses.Run(() => Campuses.Import(c, slot)); return "importing " + slot; }
        public static string Free(string unused) { SavesPanel.FreeSpace(); return "freeing"; }
        public static string Export(string id) { var c = Campuses.All[0]; SavesPanel.Export(c, c.Vault.Versions.First(v => v.Id == int.Parse(id))); return "exporting"; }
        public static string Trigger(string why) { SaveFlow.Trigger(why); return "armed"; }
    }
}
