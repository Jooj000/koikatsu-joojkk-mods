using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using ActionGame.Chara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Utilities;
using Manager;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Keeps big classes playable on 16 GB machines:
    /// - loads at most N students per period (the game loads up to 38), choosing the ones that matter first
    ///   (other plugins can rank girls through <see cref="PriorityProviders"/>; Campus Life does);
    /// - frees unused assets while the loading screen is still up;
    /// - makes students steer around each other instead of walking through one another;
    /// - stops animating the skeleton of students the camera does not see;
    /// - runs hair and clothes physics of far students at a lower rate (and not at all far away);
    /// - switches off idle cameras other mods leave running and re-applies MaterialEditor edits lost by slow loads (see <see cref="OtherMods"/>).
    /// - releases the clothes of students on other maps and rebuilds them before they are seen (<see cref="OffMapClothes"/>);
    /// - keeps only the data of students on other maps and loads them back before they are seen (<see cref="OffMap"/>, experimental);
    /// - removes character pieces left lying at the middle of the map by interrupted reloads (<see cref="Orphans"/>).
    /// </summary>
    [BepInPlugin(GUID, "School Optimizer", Version)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, "1.45")]
    [BepInDependency("com.bepis.bepinex.extendedsave")]   // AbsentPark keeps the parked students' extended data
    [BepInDependency("com.bepis.bepinex.sideloader")]     // MissingMods reads what Sideloader loaded
    // Optional: loaded first when installed, so OtherMods finds its types (without this the ANAL camera fix never ran).
    [BepInDependency("org.njaecha.plugins.anal", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.materialeditor", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.hairaccessorycustomizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.deathweasel.bepinex.uncensorselector", BepInDependency.DependencyFlags.SoftDependency)]
    public class SchoolOptimizerPlugin : BaseUnityPlugin
    {
        public const string GUID = "joojkk.schooloptimizer";
        public const string Version = "1.7.1";

        internal static ManualLogSource Log;
        internal static SchoolOptimizerPlugin Instance;

        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<int> MaxStudents;
        public static ConfigEntry<int> PrioritySlots;
        public static ConfigEntry<bool> CleanupOnLoad;
        public static ConfigEntry<bool> AutoLimit;
        public static ConfigEntry<int> HighMemoryLoad;
        public static ConfigEntry<bool> FixCrowding;
        public static ConfigEntry<float> PersonalSpace;
        public static ConfigEntry<bool> PauseOffscreen;
        public static ConfigEntry<bool> SkipDuplicateReloads;
        public static ConfigEntry<bool> BackgroundOutfits;
        public static ConfigEntry<bool> FastLoadingScreens;
        public static ConfigEntry<bool> BoneLod;
        public static ConfigEntry<float> BoneFullDistance;
        public static ConfigEntry<float> BoneOffDistance;
        public static ConfigEntry<bool> BoneOffScreen;
        public static ConfigEntry<int> MaxOutfitTexture;
        public static ConfigEntry<bool> AnalCameras;
        public static ConfigEntry<bool> UncensorHiddenParts;
        public static ConfigEntry<int> MeMaxTexture;
        public static ConfigEntry<bool> MeRefresh;
        public static ConfigEntry<bool> KsoxLowMemory;
        public static ConfigEntry<bool> UnloadOffMap;
        public static ConfigEntry<bool> StripOffMap;
        public static ConfigEntry<bool> OrphanCleanup;
        public static ConfigEntry<bool> ParkAbsent;
        public static ConfigEntry<bool> HairWatchOn;

        /// <summary>
        /// Other plugins add a scoring function here (higher = load her first, 0 = no opinion). Campus Life ranks girls
        /// with open requests, ongoing stories and the weekly spotlight. Kept as plain delegates so no hard dependency
        /// is needed (add through reflection).
        /// </summary>
        /// <summary>Seats another plugin left free today (Campus Life keeps professors and story heroines home on a full day): they go to the students.</summary>
        public static Func<int> SeatBonus;

        public static readonly List<Func<SaveData.Heroine, float>> PriorityProviders = new List<Func<SaveData.Heroine, float>>();

        private void Awake()
        {
            Log = Logger;
            Instance = this;
            const string s = "1. Memory";
            Enabled = Config.Bind(s, "Enable", true, new ConfigDescription("Master switch for the School Optimizer.", null, new ConfigurationManagerAttributes { Order = 10 }));
            MaxStudents = Config.Bind(s, "Max students loaded", 24, new ConfigDescription(
                "How many students are loaded at school at the same time (the game itself loads up to 38, more with KK_GameplayMod). " +
                "Every loaded girl costs roughly 150-400 MB with modded cards. 0 = use the game's own setting.", new AcceptableValueRange<int>(0, 99),
                new ConfigurationManagerAttributes { Order = 9 }));
            PrioritySlots = Config.Bind(s, "Reserved for important girls", 8, new ConfigDescription(
                "Part of the limit kept for girls that matter today (girlfriends, open requests, stories, spotlight). The rest is random, like vanilla.",
                new AcceptableValueRange<int>(0, 38), new ConfigurationManagerAttributes { Order = 8 }));
            CleanupOnLoad = Config.Bind(s, "Free memory on period change", true, new ConfigDescription(
                "Unloads unused assets and runs the garbage collector while the loading screen is still showing (no stutter during play).",
                null, new ConfigurationManagerAttributes { Order = 7 }));
            AutoLimit = Config.Bind(s, "Lower the limit when RAM is full", true, new ConfigDescription(
                "When the computer's memory is nearly full (Windows starts swapping to disk, which causes long freezes), each new period " +
                "loads 4 fewer students, down to 10; when memory frees up the limit slowly goes back to normal.",
                null, new ConfigurationManagerAttributes { Order = 6 }));
            MaxOutfitTexture = Config.Bind(s, "Max outfit texture size", 2048, new ConfigDescription(
                "The game paints every outfit piece of every character into its own texture on the graphics card, as big as the " +
                "piece's original (4096 for many modded clothes: 85 MB of video memory each). With 25+ students that is more than a " +
                "4 GB card holds, and the game stutters while Windows moves textures in and out. Pieces larger than this are painted " +
                "at this size instead (story mode only; the character maker keeps full size). 0 = no limit. Applies to characters " +
                "loaded after the change.", new AcceptableValueList<int>(0, 1024, 2048, 4096), new ConfigurationManagerAttributes { Order = 4 }));
            HighMemoryLoad = Config.Bind(s, "RAM usage threshold (%)", 85, new ConfigDescription(
                "System memory use (as in Task Manager) above which the limit is lowered.", new AcceptableValueRange<int>(60, 95),
                new ConfigurationManagerAttributes { Order = 5 }));
            UnloadOffMap = Config.Bind(s, "Unload students on other maps (experimental)", false, new ConfigDescription(
                "Experimental, off: in the first test students came back in T-pose, with hidden uncensor parts showing, and with errors every frame. Students on other maps keep only their data (where they go, what they do, what they wear); their body, " +
                "clothes and textures are released after 20 seconds away from your map. They are loaded back in the " +
                "background before you can see them: when they walk towards your map, when you walk up to a door that leads " +
                "to theirs, or at once when you teleport there. Story characters are never unloaded.",
                null, new ConfigurationManagerAttributes { Order = 3 }));
            StripOffMap = Config.Bind(s, "Drop clothes of students on other maps", true, new ConfigDescription(
                "Clothes and accessories hold most of a student's video memory (each piece is painted into its own texture: 30-200 MB " +
                "per girl). Students away from your map for a while keep their body and only drop their clothes and accessories; " +
                "what they wear stays in their data. The outfit is rebuilt in the background before you can see them: when they " +
                "walk towards your map, when you walk up to a door that leads to theirs, and at once (hidden until it is ready) " +
                "when you teleport there or they are called. Story characters are never touched.",
                null, new ConfigurationManagerAttributes { Order = 2 }));
            ParkAbsent = Config.Bind(s, "Absent students keep only game data", true, new ConfigDescription(
                "Students who did not come to school today have no body, but their cards still held every texture of their " +
                "look in memory (MaterialEditor, skin and clothes overlays: 282 MB for 30 absent girls in one test). For the day " +
                "that appearance data is moved to a file in BepInEx/cache/joojkk_absent and comes back by itself the moment " +
                "anything needs it (her body being built, the game saving). Game data such as Pregnancy and LewdCrestX stays.",
                null, new ConfigurationManagerAttributes { Order = 1 }));
            const string c = "2. Crowds";
            FixCrowding = Config.Bind(c, "Students avoid each other", true, new ConfigDescription(
                "Turns on high-quality avoidance with varied priorities so students walk around each other instead of through each other.",
                null, new ConfigurationManagerAttributes { Order = 6 }));
            PersonalSpace = Config.Bind(c, "Personal space radius", 0.25f, new ConfigDescription(
                "Minimum avoidance radius of a student in meters.", new AcceptableValueRange<float>(0.15f, 0.6f),
                new ConfigurationManagerAttributes { Order = 5 }));

            const string r = "3. Rendering";
            PauseOffscreen = Config.Bind(r, "Pause students off screen", true, new ConfigDescription(
                "Students the camera does not see stop updating their skeleton (they keep walking; Unity resumes them as soon as " +
                "they come into view). Saves CPU on busy maps such as hallways and the courtyard. Nothing changes on screen.",
                null, new ConfigurationManagerAttributes { Order = 4 }));
            BoneLod = Config.Bind(r, "Hair and clothes physics by distance", true, new ConfigDescription(
                "Hair, skirt and accessory physics is the most expensive part of every student, and it gets more expensive the " +
                "lower the FPS (the game catches up with up to 3 steps a frame). Near the camera it stays as it is; farther away it " +
                "takes one smooth step a frame, and far away it stops (it resets smoothly when she comes closer). Only while walking around school.",
                null, new ConfigurationManagerAttributes { Order = 3 }));
            BoneOffScreen = Config.Bind(r, "Physics off for students off screen", true, new ConfigDescription(
                "With hair and clothes physics by distance on: students out of the camera's view (behind you; with a margin, so it is back before they show up) and " +
                "farther than the full physics distance stop their physics too. It resets smoothly when she comes into view.",
                null, new ConfigurationManagerAttributes { Order = 2 }));
            BoneFullDistance = Config.Bind(r, "Full physics distance (m)", 7f, new ConfigDescription(
                "Students closer to the camera than this keep their physics at full rate.", new AcceptableValueRange<float>(2f, 30f),
                new ConfigurationManagerAttributes { Order = 2 }));
            BoneOffDistance = Config.Bind(r, "Physics off distance (m)", 16f, new ConfigDescription(
                "Past this distance from the camera, hair and clothes physics stops.", new AcceptableValueRange<float>(5f, 60f),
                new ConfigurationManagerAttributes { Order = 1 }));

            // An option about another mod shows only when that mod is installed.
            Func<string, bool> has = g => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(g);
            const string m = "5. Other mods";
            AnalCameras = Config.Bind(m, "AmazingNewAccessoryLogic: idle cameras off", true, new ConfigDescription(
                "AmazingNewAccessoryLogic (by njaecha) gives every character a camera that renders a full-screen texture every " +
                "frame, even with its editor closed (28 students = 28 extra camera passes and ~300 MB of video memory). This " +
                "switches each one on only while that character's ANAL editor is open. ANAL's files are not changed.",
                null, new ConfigurationManagerAttributes { Order = 1, Browsable = has("org.njaecha.plugins.anal") }));
            UncensorHiddenParts = Config.Bind(m, "UncensorSelector: small textures for hidden parts", true, new ConfigDescription(
                "UncensorSelector paints the skin of every uncensor part into a 2048 texture (21 MB of video memory), also the " +
                "penis that most girls keep hidden. While it paints a part the card hides, this makes that texture 512 instead " +
                "(story mode only). Cards that show the part are untouched. UncensorSelector's files are not changed.",
                null, new ConfigurationManagerAttributes { Order = 2, Browsable = has("com.deathweasel.bepinex.uncensorselector") }));
            MeMaxTexture = Config.Bind(m, "MaterialEditor: max texture size", 2048, new ConfigDescription(
                "MaterialEditor keeps every texture stored in a card uncompressed on the graphics card (a 4096 one takes 85 MB). " +
                "In story mode, bigger ones are drawn at this size instead; the card itself keeps the full image (saving and " +
                "exporting are unaffected). 0 = no limit. Applies to characters loaded after the change. MaterialEditor's " +
                "files are not changed.", new AcceptableValueList<int>(0, 1024, 2048, 4096), new ConfigurationManagerAttributes { Order = 3, Browsable = has("com.deathweasel.bepinex.materialeditor") }));
            MeRefresh = Config.Bind(m, "Re-apply MaterialEditor and hair accessory colors after slow loads", true, new ConfigDescription(
                "Outside the maker, MaterialEditor and HairAccessoryCustomizer apply a card's edits once, right after the " +
                "character loads. Pieces built later (outfits loaded over several frames) stayed without them: white clothes " +
                "or hair accessories, or the vanilla hair the card hides showing again (fixed by opening the maker). When an " +
                "outfit load takes more than one frame, and the first time each student appears, both plugins' own update is " +
                "run again for her. Their files are not changed.",
                null, new ConfigurationManagerAttributes { Order = 4, Browsable = has("com.deathweasel.bepinex.materialeditor") || has("com.deathweasel.bepinex.hairaccessorycustomizer") }));
            HairWatchOn = Config.Bind(m, "Find and fix broken hair on the map", true, new ConfigDescription(
                "Every second one student near you is looked at: hair without its color or texture, hair pieces missing, strands " +
                "blown up by the physics, or her mod data still away while she is at school. Each case is fixed once (colors, " +
                "MaterialEditor and HairAccessoryCustomizer applied again, strands reset) and written to the log as " +
                "\"Hair watch: ...\" with what was going on around her, to find what breaks it.",
                null, new ConfigurationManagerAttributes { Order = 3 }));

            KsoxLowMemory = Config.Bind(m, "Skin Overlay Mod: compressed overlays at original size", true, new ConfigDescription(
                "Sets two of KSOX's own options (by ManlyMarco) the way this install runs them: overlay textures compressed in " +
                "RAM (about a quarter of the memory) and never larger than the body texture they paint over. Off leaves " +
                "KSOX's options as you set them. Takes effect after a scene reload. KSOX's files are not changed.",
                null, new ConfigurationManagerAttributes { Order = 5, Browsable = has("KSOX") }));
            KsoxLowMemory.SettingChanged += (o, e) => OtherMods.ApplyKsox();

            const string l = "4. Loading";
            SkipDuplicateReloads = Config.Bind(l, "Skip duplicate character rebuilds (experimental)", false, new ConfigDescription(
                "The game builds every student when she is loaded and then rebuilds her whole body and outfit again the first " +
                "time she appears on screen, although nothing changed. This skips that second rebuild (and the outfit reload " +
                "that follows when she already wears the right outfit). Girls that were swapped for another one are still rebuilt. " +
                "Experimental: with it on, hair and accessories were seen left on the floor and doubled, so it is off by default.",
                null, new ConfigurationManagerAttributes { Order = 4 }));

            BackgroundOutfits = Config.Bind(l, "Change outfits of unseen students in the background", true, new ConfigDescription(
                "At the start of a period the game changes the outfit of every student at once (uniform, gym clothes, swimsuit, " +
                "club outfit), also of those on other maps, which makes the loading screen much longer. With this on, only the " +
                "students on your map change during the loading screen; the others change one by one during play, spread over " +
                "several frames, before you meet them (or the moment they walk into your map).",
                null, new ConfigurationManagerAttributes { Order = 3 }));

            FastLoadingScreens = Config.Bind(l, "Faster loading screens", true, new ConfigDescription(
                "While a loading screen is up, Unity may spend up to 50 ms per frame finishing assets that load in the " +
                "background (the game leaves it at a few ms). During play the normal value is kept, so there is no extra stutter.",
                null, new ConfigurationManagerAttributes { Order = 2 }));
            OrphanCleanup = Config.Bind(l, "Remove clothes left behind", true, new ConfigDescription(
                "When a character's outfit reload is cut short, the game can leave an accessory lying at the middle of the map " +
                "(it is created loose and attached to her only at the end). Pieces that belong to nobody are removed, and " +
                "KK_AccessoryClothes pieces left unbound (drawn at the middle of the map) are bound to her body.",
                null, new ConfigurationManagerAttributes { Order = 1 }));
            Config.Bind("6. Missing zipmods", "Missing zipmods", "", new ConfigDescription(
                "Lists every zipmod that the girls of the loaded save and the cards in UserData/chara use but that is not " +
                "installed (or is installed in an older version without some items), with the characters and items that need " +
                "it. Search and sort in the window; the list is also saved to UserData/missing_zipmods.txt.",
                null, new ConfigurationManagerAttributes { CustomDrawer = MissingMods.DrawButton, HideDefaultButton = true, HideSettingName = true }));
            Log.LogDebug("Background loading priority at start: " + Application.backgroundLoadingPriority);

            var harmony = Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
            try { OutfitDefer.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Background outfit changes not available: " + e.Message); }
            try { ReloadSkip.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Duplicate rebuild skip not available: " + e.Message); }
            try { OtherMods.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Fixes for other mods not available: " + e.Message); }
            try { OffMapClothes.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Releasing clothes on other maps not available: " + e.Message); StripOffMap.Value = false; }
            try { OffMap.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Unloading students on other maps not available: " + e.Message); UnloadOffMap.Value = false; }
            try { AbsentPark.Patch(harmony); }
            catch (Exception e) { Log.LogWarning("Parking data of absent students not available: " + e.Message); ParkAbsent.Value = false; }
            ParkAbsent.SettingChanged += (o, ea) => { if (!ParkAbsent.Value) AbsentPark.RestoreAll(); };
            BoneLod.SettingChanged += (o, ea) => { if (!BoneLod.Value) DynamicBoneLod.RestoreAll(); };
        }

        private float _nextBoneLod, _nextOrphans;

        private float _nextCrowdFix;

        private void OnGUI()
        {
            try { MissingMods.OnGUI(); }
            catch (Exception e) { Log.LogDebug("Missing zipmods window: " + e.Message); }
        }

        private void Start()
        {
            OtherMods.ApplyKsox();
        }

        private void Update()
        {
            try { LoadingPriority.Update(); }
            catch (Exception e) { Log.LogDebug("Loading priority: " + e.Message); }
            try { OutfitDefer.Update(); }
            catch (Exception e) { Log.LogDebug("Background outfits: " + e.Message); }
            try { OffMapClothes.Update(); }
            catch (Exception e) { Log.LogDebug("Off-map clothes: " + e.Message); }
            try { OffMap.Update(); }
            catch (Exception e) { Log.LogDebug("Off-map students: " + e.Message); }
            try { HairWatch.Update(); }
            catch (Exception e) { Log.LogDebug("Hair watch: " + e.Message); }
            if (Time.realtimeSinceStartup >= _nextOrphans)
            {
                _nextOrphans = Time.realtimeSinceStartup + 2f;
                try { Orphans.Sweep(); }
                catch (Exception e) { Log.LogDebug("Orphan sweep: " + e.Message); }
                try { OutfitDefer.Sweep(); }
                catch (Exception e) { Log.LogDebug("Changing state sweep: " + e.Message); }
            }
            if (Time.realtimeSinceStartup >= _nextBoneLod)
            {
                _nextBoneLod = Time.realtimeSinceStartup + 0.25f;
                try { DynamicBoneLod.Apply(); }
                catch (Exception e) { Log.LogDebug("Bone LOD: " + e.Message); }
            }
            if (!Enabled.Value || Time.realtimeSinceStartup < _nextCrowdFix) return;
            _nextCrowdFix = Time.realtimeSinceStartup + 2f;
            if (FixCrowding.Value)
            {
                try { Crowds.Apply(); }
                catch (Exception e) { Log.LogDebug("Crowd fix: " + e.Message); }
            }
            if (PauseOffscreen.Value)
            {
                try { Offscreen.Apply(); }
                catch (Exception e) { Log.LogDebug("Offscreen pause: " + e.Message); }
            }
        }

        internal static float Score(SaveData.Heroine h)
        {
            var score = 0f;
            if (h.isGirlfriend) score += 30f;
            foreach (var p in PriorityProviders)
            {
                try { score += p(h); }
                catch { }
            }
            return score;
        }
    }

    internal static class Hooks
    {
        /// <summary>
        /// Wraps the coroutine that loads the students of a period: lowers the random pick to make room for the
        /// important girls, loads those afterwards (still behind the loading screen) and frees memory.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ActionScene), nameof(ActionScene.NPCLoadAll))]
        private static void NPCLoadAllPostfix(ActionScene __instance, bool isShuffle, ref IEnumerator __result)
        {
            try
            {
                if (!SchoolOptimizerPlugin.Enabled.Value) return;
                __result = Loader.Wrap(__instance, isShuffle, __result);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogError(e); }
        }

        private static int _hairFixes;

        /// <summary>
        /// The game's hair length update throws a NullReferenceException every frame when one of its length bones was
        /// without the optimizer). The dead bones are dropped from its arrays, so the rest keeps working and the log
        /// stays clean. Nothing is skipped.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(ChaCustomHairComponent), "Update")]
        private static void HairUpdatePrefix(ChaCustomHairComponent __instance)
        {
            try
            {
                var t = __instance.trfLength;
                if (t == null) return;
                var b = __instance.baseLength;
                var bad = b == null || b.Length < t.Length;
                for (var i = 0; !bad && i < t.Length; i++) bad = t[i] == null;
                if (!bad) return;
                var keepT = new List<Transform>();
                var keepB = new List<float>();
                for (var i = 0; i < t.Length; i++)
                {
                    if (t[i] == null) continue;
                    keepT.Add(t[i]);
                    keepB.Add(b != null && i < b.Length ? b[i] : t[i].localPosition.y);
                }
                __instance.trfLength = keepT.ToArray();
                __instance.baseLength = keepB.ToArray();
                if (_hairFixes++ < 20)
                    SchoolOptimizerPlugin.Log.LogInfo("Hair length bones: dropped " + (t.Length - keepT.Count) + " destroyed bone(s) of " + __instance.name);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Hair length fix: " + e.Message); }
        }

        private static int _bustFixes;
        private static readonly AccessTools.FieldRef<BustNormal, bool> BustInit = AccessTools.FieldRefAccess<BustNormal, bool>("initEnd");
        private static readonly AccessTools.FieldRef<BustNormal, Dictionary<GameObject, SkinnedMeshRenderer>> BustSmr =
            AccessTools.FieldRefAccess<BustNormal, Dictionary<GameObject, SkinnedMeshRenderer>>("dictSmr");

        /// <summary>
        /// The bust normals of a clothes piece are only let go when the game itself removes that piece. A piece gone
        /// in BustNormal.Blend) left them pointing at a destroyed mesh. Such an entry is released the game's own way
        /// (BustNormal.Release), so Blend just skips it; the next time the piece is built it is set up again.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(BustNormal), nameof(BustNormal.Blend))]
        private static void BustBlendPrefix(BustNormal __instance)
        {
            try
            {
                if (__instance == null || !BustInit(__instance)) return;
                var smr = BustSmr(__instance);
                if (smr == null) return;
                foreach (var kv in smr)
                {
                    if (kv.Key != null && kv.Value != null && kv.Value.sharedMesh != null) continue;
                    __instance.Release();
                    if (_bustFixes++ < 20) SchoolOptimizerPlugin.Log.LogInfo("Bust normals of a removed clothes piece released");
                    return;
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Bust normal fix: " + e.Message); }
        }
    }

    internal static class Loader
    {
        public static int LastLoaded;

        // The game itself stops at 38; KK_GameplayMod can raise that, so the game's own setting is used as is.
        private static int GameMax { get { return Manager.Config.ActData.MaxCharaNum; } }

        public static IEnumerator Wrap(ActionScene act, bool isShuffle, IEnumerator original)
        {
            var bonus = 0;
            try { if (SchoolOptimizerPlugin.SeatBonus != null) bonus = SchoolOptimizerPlugin.SeatBonus(); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Seat bonus: " + e.Message); }
            var cap = SchoolOptimizerPlugin.MaxStudents.Value <= 0 ? GameMax : Mathf.Min(GameMax, SchoolOptimizerPlugin.MaxStudents.Value + bonus);
            cap = MemoryGuard.Adjust(cap);
            if (!isShuffle) Trim(act, cap);
            List<SaveData.Heroine> vip = null;
            var saved = Manager.Config.ActData.MaxCharaNum;
            if (isShuffle)
            {
                vip = PickImportant(act, Mathf.Min(cap, SchoolOptimizerPlugin.PrioritySlots.Value));
                Manager.Config.ActData.MaxCharaNum = Mathf.Max(0, cap - vip.Count);
            }

            // The random pick (and the Take) happens in the first step of the original coroutine.
            bool more;
            try { more = original.MoveNext(); }
            finally { Manager.Config.ActData.MaxCharaNum = saved; }
            while (more)
            {
                yield return original.Current;
                more = original.MoveNext();
            }

            if (vip != null)
            {
                // Important girls that the random pick already brought leave their reserved slot free: fill it randomly.
                var missing = vip.Count(h => !act.npcList.Any(n => n != null && n.heroine == h));
                var free = cap - act.npcList.Count - missing;
                if (free > 0) vip.AddRange(RandomOthers(act, vip, free));
            }
            if (vip != null && vip.Count > 0)
            {
                var added = 0;
                var created = new List<NPC>();
                foreach (var h in vip)
                {
                    if (act.npcList.Any(n => n != null && n.heroine == h)) continue;
                    if (act.npcList.Count >= cap) break;
                    NPC npc;
                    try
                    {
                        npc = NPC.Create(act.transform, h);
                        act.npcList.Add(npc);
                        npc.AI.Reset(true);
                        npc.LoadStart(false);
                    }
                    catch (Exception e) { SchoolOptimizerPlugin.Log.LogWarning("Could not load " + h.parameter.fullname + ": " + e.Message); continue; }
                    created.Add(npc);
                    added++;
                }
                // The game gives everyone their first action (SetAllCharaActive) right after the loading coroutine.
                var t0 = Time.realtimeSinceStartup;
                while (created.Any(n => n != null && !n.initialized) && Time.realtimeSinceStartup - t0 < 60f) yield return null;
                if (added > 0) SchoolOptimizerPlugin.Log.LogInfo("Loaded " + added + " important (or fill-in) students on top of the random pick.");
                var left = vip.Where(h => SchoolOptimizerPlugin.Score(h) > 0f && !act.npcList.Any(n => n != null && n.heroine == h)).Select(h => h.parameter.fullname).ToArray();
                if (left.Length > 0) SchoolOptimizerPlugin.Log.LogInfo("Important students left out (limit " + cap + "): " + string.Join(", ", left));
            }
            LastLoaded = act.npcList.Count;
            SchoolOptimizerPlugin.Log.LogInfo("Students loaded this period: " + LastLoaded + " (limit " + cap + ")");
            if (OtherMods.MeTexturesShrunk > 0 || OtherMods.HiddenUncensorTextures > 0)
                SchoolOptimizerPlugin.Log.LogInfo(string.Format("Smaller textures so far: {0} MaterialEditor ({1:0} MB less before mipmaps), {2} hidden uncensor parts.",
                    OtherMods.MeTexturesShrunk, OtherMods.MeBytesSaved / 1048576.0, OtherMods.HiddenUncensorTextures));

            yield return AbsentPark.ParkAll(act);

            if (SchoolOptimizerPlugin.CleanupOnLoad.Value)
            {
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }
        }

        /// <summary>Same students as last period (no shuffle) but over the limit: the least important ones go home.</summary>
        private static void Trim(ActionScene act, int cap)
        {
            var extra = act.npcList.Count(n => n != null) - cap;
            if (extra <= 0) return;
            var drop = act.npcList.Where(n => n != null && n.heroine != null && n.heroine.fixCharaID == 0)
                .OrderBy(n => SchoolOptimizerPlugin.Score(n.heroine) + Random.value).Take(extra).ToList();
            foreach (var n in drop)
            {
                act.npcList.Remove(n);
                n.heroine.SetRoot(null);
                UnityEngine.Object.Destroy(n.gameObject);
            }
            if (drop.Count > 0) SchoolOptimizerPlugin.Log.LogInfo(drop.Count + " student(s) went home early to free memory.");
        }

        private static IEnumerable<SaveData.Heroine> Pool()
        {
            IEnumerable<SaveData.Heroine> pool = Singleton<Game>.Instance.HeroineList.Where(p => p != null && p.fixCharaID == 0);
            if (!Manager.Config.AddData.OtherClassRegisterMax)
            {
                var playerClass = Singleton<Game>.Instance.Player.schoolClass;
                pool = pool.Where(p => p.schoolClass == playerClass || p.schoolClassIndex < 5);
            }
            return pool;
        }

        private static List<SaveData.Heroine> RandomOthers(ActionScene act, List<SaveData.Heroine> taken, int n)
        {
            return Pool().Where(h => !taken.Contains(h) && !act.npcList.Any(x => x != null && x.heroine == h))
                .OrderBy(_ => Random.value).Take(n).ToList();
        }

        /// <summary>Most important students for this period, same class rules as the game.</summary>
        private static List<SaveData.Heroine> PickImportant(ActionScene act, int n)
        {
            if (n <= 0) return new List<SaveData.Heroine>();
            return Pool().Select(h => new { h, s = SchoolOptimizerPlugin.Score(h) })
                .Where(x => x.s > 0f)
                .OrderByDescending(x => x.s + Random.value)
                .Take(n).Select(x => x.h).ToList();
        }
    }

    /// <summary>
    /// Base.Start (every school character) loads the character, then on her first activation runs
    /// <c>chaCtrl.Reload(false, false, false, false)</c> and <c>ChangeNowCoordinate()</c> in a lambda that also handles
    /// the case where the character was swapped for another girl (<c>isReplace</c>). Right after a load the full rebuild
    /// is identical to what was just built, and the outfit reload is too when she already wears that outfit. The lambda's
    /// two calls are redirected here: swapped characters go through untouched, fresh ones skip the duplicate work.
    /// </summary>
    internal static class ReloadSkip
    {
        public static int SkippedRebuilds, SkippedOutfits;
        private static MethodInfo _isReplace;
        private static ChaControl _justSkipped;

        public static void Patch(Harmony harmony)
        {
            _isReplace = AccessTools.PropertyGetter(typeof(Base), "isReplace");
            var reload = AccessTools.Method(typeof(ChaControl), nameof(ChaControl.Reload), new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
            var change = AccessTools.Method(typeof(Base), nameof(Base.ChangeNowCoordinate));
            if (_isReplace == null || reload == null || change == null) throw new Exception("game methods not found");
            var targets = new List<MethodBase>();
            foreach (var t in Nested(typeof(Base)))
            {
                foreach (var m in AccessTools.GetDeclaredMethods(t))
                {
                    if (m.ContainsGenericParameters || m.IsAbstract) continue;
                    List<object> operands;
                    try { operands = PatchProcessor.ReadMethodBody(m).Select(i => i.Value).ToList(); }
                    catch { continue; }
                    if (operands.Contains(reload) && operands.Contains(change)) targets.Add(m);
                }
            }
            if (targets.Count != 1) throw new Exception("expected one first-activation handler, found " + targets.Count);
            harmony.Patch(targets[0], transpiler: new HarmonyMethod(typeof(ReloadSkip), nameof(RewriteFirstActivation)));
            harmony.PatchAll(typeof(ReloadSkip));
        }

        /// <summary>
        /// Students are built with the outfit saved in their card (usually the uniform) and changed to the period's outfit
        /// on their first activation, which rebuilds the clothes once more. Building them directly in the period's outfit
        /// avoids that. Only the private copy of the card made for this character is changed.
        /// </summary>
        [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.CreateChara))]
        private static void CreateCharaPrefix(GameObject parent, ChaFileControl _chaFile)
        {
            try
            {
                if (!SchoolOptimizerPlugin.Enabled.Value || !SchoolOptimizerPlugin.SkipDuplicateReloads.Value || parent == null || _chaFile == null) return;
                var npc = parent.GetComponent<NPC>();
                if (npc == null || npc.heroine == null || npc.heroine.charFile == _chaFile) return;
                var want = npc.heroine.NowCoordinate;
                if (want >= 0 && want < _chaFile.coordinate.Length) _chaFile.status.coordinateType = want;
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Starting outfit: " + e.Message); }
        }

        /// <summary>Outfit textures painted at no more than "Max outfit texture size" (aspect kept).</summary>
        [HarmonyPrefix, HarmonyPatch(typeof(CustomTextureCreate), nameof(CustomTextureCreate.Initialize))]
        private static void OutfitTextureSizePrefix(ref int width, ref int height)
        {
            try
            {
                var max = SchoolOptimizerPlugin.MaxOutfitTexture.Value;
                // Set only while UncensorSelector paints a part this character hides (checked for story mode there).
                if (OtherMods.HiddenUncensorPart) { max = 512; OtherMods.HiddenUncensorTextures++; }
                if (!SchoolOptimizerPlugin.Enabled.Value || max <= 0 || width <= max && height <= max) return;
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.MainGame) return;
                var k = (float)max / Mathf.Max(width, height);
                width = Mathf.Max(1, Mathf.RoundToInt(width * k));
                height = Mathf.Max(1, Mathf.RoundToInt(height * k));
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Outfit texture size: " + e.Message); }
        }

        private static IEnumerable<Type> Nested(Type t)
        {
            foreach (var n in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                yield return n;
                foreach (var d in Nested(n)) yield return d;
            }
        }

        private static IEnumerable<CodeInstruction> RewriteFirstActivation(IEnumerable<CodeInstruction> instructions)
        {
            var reload = AccessTools.Method(typeof(ChaControl), nameof(ChaControl.Reload), new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool) });
            var change = AccessTools.Method(typeof(Base), nameof(Base.ChangeNowCoordinate));
            foreach (var ins in instructions)
            {
                if ((ins.opcode == OpCodes.Callvirt || ins.opcode == OpCodes.Call) && Equals(ins.operand, reload))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ReloadSkip), nameof(Reload))).WithLabels(ins.labels).WithBlocks(ins.blocks);
                else if ((ins.opcode == OpCodes.Callvirt || ins.opcode == OpCodes.Call) && Equals(ins.operand, change))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ReloadSkip), nameof(ChangeNowCoordinate))).WithLabels(ins.labels).WithBlocks(ins.blocks);
                else yield return ins;
            }
        }

        private static Base Owner(ChaControl cha)
        {
            var t = cha.transform.parent;
            for (var i = 0; i < 3 && t != null; i++, t = t.parent)
            {
                var b = t.GetComponent<Base>();
                if (b != null) return b;
            }
            return null;
        }

        /// <summary>Stands in for <c>chaCtrl.Reload(a, b, c, d)</c> in the first-activation handler.</summary>
        public static bool Reload(ChaControl cha, bool noClothes, bool noHead, bool noHair, bool noBody)
        {
            _justSkipped = null;
            try
            {
                if (SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.SkipDuplicateReloads.Value && cha != null && cha.loadEnd
                    && !noClothes && !noHead && !noHair && !noBody)
                {
                    var owner = Owner(cha);
                    if (owner != null && owner.chaCtrl == cha && !(bool)_isReplace.Invoke(owner, null))
                    {
                        SkippedRebuilds++;
                        _justSkipped = cha;
                        return true;
                    }
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Rebuild skip: " + e.Message); _justSkipped = null; }
            return cha.Reload(noClothes, noHead, noHair, noBody);
        }

        /// <summary>Stands in for <c>ChangeNowCoordinate()</c> right after it.</summary>
        public static void ChangeNowCoordinate(Base b)
        {
            var skipped = _justSkipped;
            _justSkipped = null;
            try
            {
                var npc = b as NPC;
                if (skipped != null && npc != null && npc.chaCtrl == skipped && npc.heroine != null)
                {
                    // NPC.Start's own handler (SynchroCoordinate) runs right after this one and switches her to
                    // NowCoordinate, so the outfit in between never shows: skip it when she wears either one.
                    var c = npc.heroine.coordinates;
                    var first = c != null && c.Length > 0 ? c[0] : 0;
                    var now = skipped.fileStatus.coordinateType;
                    if (now == first || now == npc.heroine.NowCoordinate)
                    {
                        SkippedOutfits++;
                        return;
                    }
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Outfit skip: " + e.Message); }
            // Unseen students get the outfit later, in the background.
            var n = b as NPC;
            if (n != null) OutfitDefer.ChangeOrQueue(n);
            else b.ChangeNowCoordinate();
        }
    }

    /// <summary>
    /// Outfit changes of students the player cannot see are postponed and done one at a time in the background.
    /// <list type="bullet">
    /// <item>NPC.ReStart (start of every period) calls ChangeNowCoordinate for everybody: redirected here, students not on
    /// the player's map are queued instead.</item>
    /// <item>NPC.SynchroCoordinate (activation, or the AI changing clothes) for an unseen student is queued too. When she
    /// shows up on the player's map before her turn, the normal path runs (KK_Fix_MainGameOptimizations makes it async).</item>
    /// <item>The queue is worked off during free roaming: ChangeCoordinateType + ReloadAsync with its async steps, the same
    /// calls ChangeCoordinateTypeAndReload makes, so KKAPI and the other plugins see the usual events.</item>
    /// <item>One reload per character at a time: while hers (ours, or IllusionFixes' async one) is running, any new outfit
    /// change of hers waits in the queue. Two at once left doubled outfits and pieces lying in the middle of the map.</item>
    /// </list>
    /// </summary>
    internal static class OutfitDefer
    {
        public static int Deferred, DoneInBackground;
        private static readonly Dictionary<NPC, bool> Pending = new Dictionary<NPC, bool>();
        private static readonly HashSet<NPC> Busy = new HashSet<NPC>();
        private const int Parallel = 2;

        private static bool On { get { return SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.BackgroundOutfits.Value; } }

        public static void Patch(Harmony harmony)
        {
            var restart = AccessTools.Method(typeof(NPC), nameof(NPC.ReStart));
            var sync = AccessTools.Method(typeof(NPC), nameof(NPC.SynchroCoordinate));
            if (restart == null || sync == null) throw new Exception("game methods not found");
            harmony.Patch(restart, transpiler: new HarmonyMethod(typeof(OutfitDefer), nameof(ReStartTranspiler)));
            harmony.Patch(sync, prefix: new HarmonyMethod(typeof(OutfitDefer), nameof(SynchroPrefix)) { priority = Priority.First });
        }

        private static IEnumerable<CodeInstruction> ReStartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var change = AccessTools.Method(typeof(Base), nameof(Base.ChangeNowCoordinate));
            var npcChange = AccessTools.Method(typeof(NPC), nameof(NPC.ChangeNowCoordinate));
            foreach (var ins in instructions)
            {
                if ((ins.opcode == OpCodes.Callvirt || ins.opcode == OpCodes.Call) && (Equals(ins.operand, change) || Equals(ins.operand, npcChange)))
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(OutfitDefer), nameof(ChangeOrQueue))).WithLabels(ins.labels).WithBlocks(ins.blocks);
                else yield return ins;
            }
        }

        private static bool Unseen(NPC npc)
        {
            var act = npc.actScene;
            return act != null && act.Map != null && npc.mapNo != act.Map.no && !act.isEventNow;
        }

        public static void ChangeOrQueue(NPC npc)
        {
            try
            {
                if (On && npc != null && npc.chaCtrl != null && npc.heroine != null && !OffMap.IsUnloaded(npc) && !OffMapClothes.IsStripped(npc) && (Unseen(npc) || Reloading(npc)))
                {
                    if (!Pending.ContainsKey(npc)) Pending[npc] = false;
                    Deferred++;
                    return;
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Outfit queue: " + e.Message); }
            npc.ChangeNowCoordinate();
        }

        // KK_Fix_MainGameOptimizations (IllusionFixes) "Async clothes loading": the characters it is reloading right now.
        private static System.Collections.IList _fixRunning;
        private static bool _fixLooked;

        /// <summary>
        /// An outfit reload of this character is still running (ours in the background, or IllusionFixes' async one).
        /// Starting a second one at the same time is what leaves two outfits on a girl ("2 coordinates loaded at once",
        /// as IllusionFixes itself warns) and old pieces lying on the floor of the map.
        /// </summary>
        public static bool Reloading(NPC npc)
        {
            if (Busy.Contains(npc)) return true;
            if (!_fixLooked)
            {
                _fixLooked = true;
                try
                {
                    var t = AccessTools.TypeByName("IllusionFixes.MainGameOptimizations");
                    var f = t == null ? null : AccessTools.Field(t, "_runningReloadCoroutines");
                    _fixRunning = f == null ? null : f.GetValue(null) as System.Collections.IList;
                }
                catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("IllusionFixes reload list: " + e.Message); }
            }
            return _fixRunning != null && npc.chaCtrl != null && _fixRunning.Contains(npc.chaCtrl);
        }

        /// <summary>Runs before the game's (and KK_Fix_MainGameOptimizations') SynchroCoordinate.</summary>
        private static bool SynchroPrefix(NPC __instance, ref bool isRemove)
        {
            try
            {
                if (!SchoolOptimizerPlugin.Enabled.Value || __instance == null || __instance.chaCtrl == null || __instance.heroine == null) return true;
                if (OffMap.IsUnloaded(__instance) || OffMapClothes.IsStripped(__instance))
                {
                    // No body (or no clothes) on another map: only her data changes, the load builds the outfit.
                    Pending.Remove(__instance);
                    Apply(__instance, isRemove);
                    return false;
                }
                bool queuedRemove;
                var queued = Pending.TryGetValue(__instance, out queuedRemove);
                if (__instance.chaCtrl.fileStatus.coordinateType == __instance.heroine.NowCoordinate && !isRemove) // nothing to reload
                {
                    if (queued && !queuedRemove) Pending.Remove(__instance);
                    return true;
                }
                // Never a second reload on top of one that is still running: it waits in the queue (Update picks it up).
                // This guard holds with background outfits off too (IllusionFixes' async loading alone can double up).
                if (Reloading(__instance) || On && Unseen(__instance))
                {
                    Pending[__instance] = isRemove || queuedRemove;
                    Deferred++;
                    return false;
                }
                // She is (or comes) into view: the normal path now, with the clothes state that was waiting.
                if (queued)
                {
                    Pending.Remove(__instance);
                    if (queuedRemove) isRemove = true;
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Outfit queue: " + e.Message); }
            return true;
        }

        public static void Update()
        {
            if (Pending.Count == 0) return;
            if (!Singleton<Game>.IsInstance() || Singleton<Game>.Instance.actScene == null) { Pending.Clear(); Busy.Clear(); Postponed.Clear(); return; }
            Busy.RemoveWhere(n => n == null);
            if (Busy.Count >= Parallel) return;
            var scene = Singleton<Scene>.Instance;
            if (scene.IsNowLoadingFade || scene.IsNowLoading || Singleton<Game>.Instance.actScene.isEventNow) return;

            NPC pick = null;
            var found = false;
            foreach (var kv in Pending)
            {
                if (kv.Key == null || kv.Key.chaCtrl == null || OffMap.IsUnloaded(kv.Key) || OffMapClothes.IsStripped(kv.Key) || !Reloading(kv.Key)) { pick = kv.Key; found = true; break; }
            }
            if (!found) return; // every one of them is still being reloaded: next frame
            bool remove;
            Pending.TryGetValue(pick, out remove);
            Pending.Remove(pick);
            if (pick == null || pick.chaCtrl == null || pick.heroine == null) return;
            remove = remove && Changing(pick); // she already left the locker room: only the outfit changes
            if (OffMap.IsUnloaded(pick) || OffMapClothes.IsStripped(pick)) { Apply(pick, remove); return; }
            if (!On || !Unseen(pick))
            {
                // Waited for a reload to finish and she is in view now (or background outfits are off): the game's own
                // path (async with IllusionFixes).
                pick.SynchroCoordinate(remove);
                return;
            }
            var cha = pick.chaCtrl;
            var want = pick.heroine.NowCoordinate;
            if (cha.fileStatus.coordinateType == want && !remove) return;
            if (cha.fileStatus.coordinateType != want && !cha.ChangeCoordinateType((ChaFileDefine.CoordinateType)want, true)) return;
            if (remove) cha.RandomChangeOfClothesLowPoly(pick.heroine.lewdness);
            Busy.Add(pick);
            cha.StartCoroutine(Run(pick, cha));
        }

        // Whole rebuilds (ChaControl.Reload) asked for while one of hers was running, done when it ends.
        private static readonly Dictionary<ChaControl, bool[]> Postponed = new Dictionary<ChaControl, bool[]>();

        /// <summary>
        /// The game rebuilds a student the first time she shows up (ChaControl.Reload, all at once). When she walks into
        /// the map while her outfit is still being rebuilt in the background, the two ran over each other: the background
        /// one put its accessories on her after the game's had, and pieces were left behind (doubled, or lying at the
        /// middle of the map). Such a rebuild now waits until the running one ends. False: go ahead now.
        /// </summary>
        public static bool Postpone(ChaControl cha, bool noClothes, bool noHead, bool noHair, bool noBody)
        {
            if (!SchoolOptimizerPlugin.Enabled.Value || cha == null) return false;
            NPC owner = null;
            foreach (var n in Busy)
                if (n != null && n.chaCtrl == cha) { owner = n; break; }
            if (owner == null) return false;
            bool[] prev;
            // Two waiting rebuilds become one that changes what either of them would.
            Postponed[cha] = Postponed.TryGetValue(cha, out prev)
                ? new[] { prev[0] && noClothes, prev[1] && noHead, prev[2] && noHair, prev[3] && noBody }
                : new[] { noClothes, noHead, noHair, noBody };
            Deferred++;
            return true;
        }

        /// <summary>Her outfit change as data only (no reload).</summary>
        private static void Apply(NPC npc, bool remove)
        {
            var cha = npc.chaCtrl;
            var want = npc.heroine.NowCoordinate;
            if (cha.fileStatus.coordinateType != want) cha.ChangeCoordinateType((ChaFileDefine.CoordinateType)want, true);
            if (remove && Changing(npc)) cha.RandomChangeOfClothesLowPoly(npc.heroine.lewdness);
        }

        /// <summary>
        /// She is at a changing point right now (AI action 0; the game keeps the state through action 25). The game takes
        /// some of her clothes off there (RandomChangeOfClothesLowPoly) and puts them back when her next action starts.
        /// </summary>
        private static bool Changing(NPC npc)
        {
            try { return npc != null && npc.AI != null && (npc.AI.actionNo == 0 || npc.AI.actionNo == 25); }
            catch { return false; }
        }

        public static int LeftHalfDressed;

        /// <summary>
        /// A change of clothes that waited in the queue used to be applied after she had already left the locker room:
        /// the game never put her clothes back (it does that only when her next action starts), so she walked around
        /// with pieces missing, and the game saw it (her clothes state), until the period ended. Whoever still has the
        /// changing-room state away from a changing point gets it ended, as the game would.
        /// </summary>
        public static void Sweep()
        {
            if (!SchoolOptimizerPlugin.Enabled.Value || !Singleton<Game>.IsInstance()) return;
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || act.npcList == null) return;
            var scene = Singleton<Scene>.Instance;
            if (scene.IsNowLoading || scene.IsNowLoadingFade || !string.IsNullOrEmpty(scene.AddSceneName)) return;
            foreach (var npc in act.npcList)
            {
                if (npc == null || npc.chaCtrl == null || !npc.chaCtrl.isChangeOfClothesRandom || Changing(npc)) continue;
                npc.chaCtrl.RandomChangeOfClothesLowPolyEnd();
                LeftHalfDressed++;
                SchoolOptimizerPlugin.Log.LogDebug("Clothes of " + (npc.heroine != null ? npc.heroine.Name : "?") + " put back (changing state left over, action " + npc.AI.actionNo + ")");
            }
            // clothes off (AI action 2: SetClothesStateAll(3)) and the toilet the lower ones (action 1); the game puts them
            // back only when her next action starts. Moved on without that (a summon, a director's push, a period change),
            // she walked to the cafeteria in the shower state. Seen twice in a row away from those points, it is ended.
            foreach (var npc in act.npcList)
            {
                if (npc == null || npc.chaCtrl == null || npc.chaCtrl.fileStatus == null || npc.AI == null) { continue; }
                var st = npc.chaCtrl.fileStatus.clothesState;
                var a2 = npc.AI.actionNo;
                var bare = st != null && st.Length > 1 && st[0] == 3 && st[1] == 3 || st != null && st.Length > 5 && st[1] == 3 && st[3] == 3 && st[5] == 3;
                if (!bare || a2 == 0 || a2 == 1 || a2 == 2 || a2 == 25 || npc.isLesH || npc.isOnanism || npc.chaCtrl.isChangeOfClothesRandom) { Bare.Remove(npc); continue; }
                if (Bare.Add(npc)) continue; // the first look: her next action may be starting this very moment
                Bare.Remove(npc);
                npc.chaCtrl.SetClothesStateAll(0);
                try { npc.MapShoesSetting(npc.mapNo); } catch { }
                LeftUndressed++;
                SchoolOptimizerPlugin.Log.LogInfo("Clothes of " + (npc.heroine != null ? npc.heroine.Name : "?") + " put back (shower or toilet state left over, action " + a2 + ", map " + npc.mapNo + ")");
            }
        }

        private static readonly HashSet<NPC> Bare = new HashSet<NPC>();
        public static int LeftUndressed;

        /// <summary>She is about to be unloaded: a queued outfit change becomes data only.</summary>
        public static void Forget(NPC npc)
        {
            bool remove;
            if (!Pending.TryGetValue(npc, out remove)) return;
            Pending.Remove(npc);
            Apply(npc, remove);
        }

        private static IEnumerator Run(NPC npc, ChaControl cha)
        {
            var active = cha.GetActiveTop();
            var inner = ReloadOutfit(npc, cha);
            while (inner.MoveNext()) yield return inner.Current;
            if (npc != null && cha != null) cha.SetActiveTop(npc.isActive || active);
            Leave(npc, cha);
            DoneInBackground++;
        }

        /// <summary>
        /// Rebuilds her outfit from her current outfit data over several frames (hidden while it builds), as one of the
        /// reloads <see cref="Reloading"/> knows about. The caller shows her again and then calls <see cref="Leave"/>.
        /// </summary>
        public static IEnumerator ReloadOutfit(NPC npc, ChaControl cha)
        {
            Busy.Add(npc);
            Singleton<Character>.Instance.enableCharaLoadGCClear = false;
            var inner = cha.ReloadAsync(false, true, true, true, true);
            while (true)
            {
                bool more;
                try { more = inner.MoveNext(); }
                catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Background outfit: " + e.Message); more = false; }
                if (!more) break;
                yield return inner.Current;
            }
        }

        /// <summary>Her reload is over: a whole rebuild that waited for it runs now.</summary>
        public static void Leave(NPC npc, ChaControl cha)
        {
            Busy.Remove(npc);
            bool[] wait;
            if (cha != null && Postponed.TryGetValue(cha, out wait))
            {
                Postponed.Remove(cha);
                try { cha.Reload(wait[0], wait[1], wait[2], wait[3]); }
                catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Postponed rebuild: " + e.Message); }
            }
            if (Busy.Count == 0) Singleton<Character>.Instance.enableCharaLoadGCClear = true;
        }
    }

    /// <summary>Raises Unity's background loading time slice while a loading screen is showing.</summary>
    internal static class LoadingPriority
    {
        private static ThreadPriority? _normal;
        private static bool _raised;

        public static bool Loading
        {
            get
            {
                if (!Singleton<Scene>.IsInstance()) return false;
                var scene = Singleton<Scene>.Instance;
                return scene.IsNowLoading || scene.IsNowLoadingFade;
            }
        }

        public static void Update()
        {
            if (_normal == null) _normal = Application.backgroundLoadingPriority;
            var want = SchoolOptimizerPlugin.Enabled.Value && SchoolOptimizerPlugin.FastLoadingScreens.Value && Loading;
            if (want == _raised) return;
            _raised = want;
            Application.backgroundLoadingPriority = want ? ThreadPriority.High : _normal.Value;
        }
    }

    /// <summary>Lowers the student limit while Windows is short of memory (swapping freezes the game for seconds).</summary>
    internal static class MemoryGuard
    {
        [StructLayout(LayoutKind.Sequential)]
        private class MemoryStatus
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MemoryStatus));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatus status);

        private static int _reduction;

        /// <summary>System memory in use, 0-100 (-1 if unknown).</summary>
        public static int Load()
        {
            try
            {
                var m = new MemoryStatus();
                return GlobalMemoryStatusEx(m) ? (int)m.dwMemoryLoad : -1;
            }
            catch { return -1; }
        }

        public static int Adjust(int cap)
        {
            if (!SchoolOptimizerPlugin.AutoLimit.Value) { _reduction = 0; return cap; }
            var load = Load();
            if (load < 0) return cap;
            var high = SchoolOptimizerPlugin.HighMemoryLoad.Value;
            var before = _reduction;
            if (load >= high) _reduction = Mathf.Min(_reduction + 4, Mathf.Max(0, cap - 10));
            else if (load < high - 12) _reduction = Mathf.Max(0, _reduction - 2);
            if (_reduction != before)
                SchoolOptimizerPlugin.Log.LogInfo("System memory at " + load + "%: student limit " + (cap - _reduction) + " (normally " + cap + ").");
            return cap - _reduction;
        }
    }

    /// <summary>
    /// Students' body animators run every frame even when nobody sees them. CullUpdateTransforms keeps the state machine
    /// going but skips bone updates while all of a student's renderers are off camera.
    /// </summary>
    internal static class Offscreen
    {
        public static void Apply()
        {
            if (!Singleton<Game>.IsInstance()) return;
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || act.npcList == null) return;
            foreach (var npc in act.npcList)
            {
                if (npc == null) continue;
                var cha = npc.chaCtrl;
                if (cha == null || !cha.loadEnd) continue;
                var anim = cha.animBody;
                if (anim != null && anim.cullingMode == AnimatorCullingMode.AlwaysAnimate) anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }
    }

    /// <summary>Students steer around each other.</summary>
    internal static class Crowds
    {
        private static readonly Dictionary<int, int> BasePriority = new Dictionary<int, int>();

        public static void Apply()
        {
            if (!Singleton<Game>.IsInstance()) return;
            var act = Singleton<Game>.Instance.actScene;
            if (act == null || act.npcList == null) return;
            foreach (var npc in act.npcList)
            {
                if (npc == null) continue;
                var a = npc.agent;
                if (a == null) continue;
                if (a.obstacleAvoidanceType != ObstacleAvoidanceType.HighQualityObstacleAvoidance)
                    a.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                if (a.radius < SchoolOptimizerPlugin.PersonalSpace.Value) a.radius = SchoolOptimizerPlugin.PersonalSpace.Value;
                // The game gives every student the same priority (0 while walking around, 99 while changing maps), so
                // nobody yields and they push through each other. A fixed random rank per student makes one of the two
                // step aside. Values stay above 0 so the player (0) always has the right of way.
                int p;
                var id = npc.GetInstanceID();
                if (!BasePriority.TryGetValue(id, out p)) { p = Random.Range(20, 80); BasePriority[id] = p; }
                var want = a.avoidancePriority >= 99 ? Mathf.Min(98, p + 18) : p;
                if (a.avoidancePriority != want) a.avoidancePriority = want;
            }
            if (BasePriority.Count > 400) BasePriority.Clear();
        }
    }
}
