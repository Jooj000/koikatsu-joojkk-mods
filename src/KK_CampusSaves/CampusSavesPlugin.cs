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
        internal static ConfigEntry<bool> OverLoad, EscMenu;
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
            OverLoad = Config.Bind("Panel", "Replace the Load screen", true, "The game's Load and Save buttons open this panel. Its own Load list stays one button away.");
            EscMenu = Config.Bind("Panel", "Esc menu in story mode", true, "Esc opens Resume, Save, Save & Quit to title, Quit to title and Quit instead of the game's Quit screen.");
            KeepAutos = Config.Bind("Autosave", "Recent automatic saves kept", 10, new ConfigDescription("Older ones thin out to one per hour, then one per day.", new AcceptableValueRange<int>(3, 200)));
            SaveKey = Config.Bind("Keys", "Save now", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl), "Saves during the day when you are free on the map.");
            PanelKey = Config.Bind("Keys", "Open the saves panel", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftControl, KeyCode.LeftShift), "Campuses, versions, import and normalize.");

            Campuses.Scan();
            SaveFlow.Recover();
            Harmony.CreateAndPatchAll(typeof(SaveFlow), GUID);
            GameAPI.RegisterExtraBehaviour<SavesController>(GUID);
            gameObject.AddComponent<SavesPanel>();
            gameObject.AddComponent<PauseMenu>();
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
                foreach (var v in c.Vault.Versions) s += " [" + v.Id + " " + v.Kind + " " + v.Trigger + " day " + v.Day + " cards " + v.Cards + " " + v.Name + " at " + v.Place + " slot " + v.Slot + " head " + (v == c.HeadVersion()) + " resume '" + v.Resume + "']";
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

        public static string Panel(string how)
        {
            SavesPanel.Shown = how != "off";
            SavesPanel.Selected = how.StartsWith("history") ? Campuses.All.FirstOrDefault() : null;
            SavesPanel.Pick = how.StartsWith("history ") ? int.Parse(how.Substring(8)) : 0;
            return "panel " + SavesPanel.Shown;
        }

        public static string Menu(string how)
        {
            if (how == "esc") Singleton<Manager.Scene>.Instance.GameEnd(true);
            return "menu " + PauseMenu.Shown;
        }

        public static string Cards(string slot) { return slot + ": " + SaveHeader.Cards(System.IO.Path.Combine(Campuses.GameDir, slot)); }

        public static string LoadId(string id) { return SavesPanel.LoadVersion(Campuses.All[0].Id, int.Parse(id)); }

        public static string ExportAll(string unused) { SavesPanel.ExportAll(); return "exporting all"; }
        public static string ImportOne(string slot) { var c = Campuses.All[0]; Campuses.Run(() => Campuses.Import(c, slot)); return "importing " + slot; }
        public static string Free(string unused) { SavesPanel.FreeSpace(); return "freeing"; }
        public static string Trigger(string why) { SaveFlow.Trigger(why); return "armed"; }
        public static string Status(string unused) { return Campuses.Status + " | busy " + Campuses.Busy + " | test " + Campuses.Testing + " | root " + Campuses.Root + " | slots " + string.Join(", ", System.IO.Directory.GetFiles(Campuses.GameDir, Campuses.TestSlots + "*.dat").Select(System.IO.Path.GetFileName).ToArray()); }
        public static string Scroll(string unused) { return SavesPanel.ScrollState; }
    }
}
