using System;
using System.Collections.Generic;
using System.IO;
using ActionGame;
using BepInEx;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Backgrounds behind our event scenes: the game's own ADV pictures first ("@place", see <c>GamePlaces</c>), the
    /// user's painted pictures (BepInEx\plugins\Joojkk\backgrounds\*.jpg) only where the game has none.
    /// Uses the game's own ADV backdrop (<c>TextScenario.BGParam</c>, the one its BackGroundLoad command fills): the map is
    /// hidden and the picture is drawn by the backdrop camera. As in the game's story mode, a scene played in the talk
    /// screen (<see cref="Scenes.Staged"/>) keeps the girls posed in front of the picture, with the color mask on; a map
    /// narration has nobody posed, so the characters are hidden and the mask stays off.
    /// A scene picks a picture with <see cref="SceneBuilder.Bg"/>; it changes when that line shows and goes away when the
    /// scene ends.
    /// </summary>
    internal static class EventBackdrop
    {
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static ADV.TextScenario _shownOn;
        private static string _shownId;
        private static Camera _cam;
        private static int _camMask;

        public static string ShownId { get { return _shownId; } }
        public static bool CharsHidden { get { return _cam != null; } }

        private static string Dir { get { return Path.Combine(Path.Combine(Paths.PluginPath, "Joojkk"), "backgrounds"); } }

        /// <summary>
        /// The game's own ADV backdrops (abdatadvg), by place: day, evening and night picture. A scene asks for
        /// "@place" (or "@place|painted_id"): the game's picture comes first, the painted one only if the game's fails
        /// (user's rule: "use the game's backgrounds by preference, mine are only an addition").
        /// </summary>
        private static readonly Dictionary<string, string[]> GamePlaces = new Dictionary<string, string[]>
        {
            { "cafe", new[] { "adv/bg/00.unity3d", "bg_04", "bg_05_e", "bg_05_e" } },
            { "shop", new[] { "adv/bg/00.unity3d", "bg_08", "bg_09_e", "bg_09_e" } },
            { "street", new[] { "adv/bg/00.unity3d", "bg_10", "bg_11_e", "bg_11_e" } },
            { "sky", new[] { "adv/bg/00.unity3d", "bg_12", "bg_13_e", "bg_13_e" } },
            { "parkway", new[] { "adv/bg/00.unity3d", "bg_14", "bg_15_e", "bg_15_e" } },
            { "gate", new[] { "adv/bg/00.unity3d", "bg_16", "bg_16_e", "bg_16_n" } },
            { "school", new[] { "adv/bg/00.unity3d", "bg_op", "bg_op", "bg_op" } },
            { "sweets", new[] { "adv/bg/00.unity3d", "cg_ti_00", "cg_ti_00", "cg_ti_00" } },
            { "room", new[] { "adv/bg/00.unity3d", "bg_06", "bg_06", "bg_06" } },
            { "park", new[] { "adv/bg/20.unity3d", "bg_park_no", "bg_park_ev", "bg_park_ni" } },
            { "station", new[] { "adv/bg/20.unity3d", "bg_station_no", "bg_station_ev", "bg_station_ni" } },
            { "themepark", new[] { "adv/bg/20.unity3d", "bg_theme_park_no", "bg_theme_park_ev", "bg_theme_park_ni" } },
            { "nightview", new[] { "adv/bg/20.unity3d", "bg_night_view", "bg_night_view", "bg_night_view_late" } },
            { "ferriswheel", new[] { "adv/bg/20.unity3d", "bg_ferris_wheel_ni", "bg_ferris_wheel_ni", "bg_ferris_wheel_ni" } },
            { "chapel", new[] { "adv/bg/20.unity3d", "ev_chapel_no", "ev_chapel_ev", "ev_chapel_ev" } },
        };

        /// <summary>0 day, 1 evening (after school), 2 night.</summary>
        internal static int TimeSlot()
        {
            var p = G.Period;
            if (p == null) return 0;
            switch (p.Value)
            {
                case Cycle.Type.AfterSchool: case Cycle.Type.StaffTime: return 1;
                case Cycle.Type.GotoMyHouse: case Cycle.Type.MyHouse: case Cycle.Type.WakeUp: return 2;
                default: return 0;
            }
        }

        public static bool Exists(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            foreach (var one in id.Split('|'))
            {
                if (one.StartsWith("@") ? GamePlaces.ContainsKey(one.Substring(1)) : File.Exists(Path.Combine(Dir, one + ".jpg"))) return true;
            }
            return false;
        }

        /// <summary>The game's own picture for "@place" at this time of day (null if the bundle or picture is missing).</summary>
        private static Texture2D GetGame(string place)
        {
            string[] e;
            if (!GamePlaces.TryGetValue(place, out e)) return null;
            var name = e[1 + TimeSlot()];
            var key = "@" + e[0] + "/" + name;
            Texture2D tex;
            if (Cache.TryGetValue(key, out tex) && tex != null) return tex;
            tex = null;
            try
            {
                tex = AssetBundleManager.LoadAsset(e[0], name, typeof(Texture2D), null).GetAsset<Texture2D>();
                if (tex == null) CampusLifePlugin.Log.LogDebug("Game background missing: " + key);
            }
            catch (Exception ex) { CampusLifePlugin.Log.LogDebug("Game background " + key + ": " + ex.Message); }
            Cache[key] = tex;
            return tex;
        }

        /// <summary>Loads the pictures a scene will use before it starts (each one once per game).</summary>
        public static void Preload(IEnumerable<string> ids)
        {
            foreach (var id in ids) Get(id);
        }

        /// <summary>A picture by scene id ("@room", "classroom_exam"...), for the loading screen.</summary>
        internal static Texture2D Picture(string id) { return Get(id); }

        private static Texture2D Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id.IndexOf('|') >= 0)
            {
                foreach (var one in id.Split('|')) { var t = Get(one); if (t != null) return t; }
                return null;
            }
            if (id.StartsWith("@")) return GetGame(id.Substring(1));
            Texture2D tex;
            if (Cache.TryGetValue(id, out tex) && tex != null) return tex;
            var path = Path.Combine(Dir, id + ".jpg");
            if (!File.Exists(path)) { CampusLifePlugin.Log.LogDebug("Event background missing: " + path); Cache[id] = null; return null; }
            try
            {
                tex = new Texture2D(2, 2, TextureFormat.RGB24, true) { name = "cl_bg_" + id, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                if (!tex.LoadImage(File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(tex); tex = null; }
                else UnityEngine.Object.DontDestroyOnLoad(tex);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Event background " + id + ": " + e.Message); tex = null; }
            Cache[id] = tex;
            return tex;
        }

        /// <summary>Shows a picture behind the scene now playing (null or "map": back to the map).</summary>
        public static void Show(string id, ADV.TextScenario scenario)
        {
            try
            {
                if (scenario == null || scenario.BGParam == null) return;
                if (string.IsNullOrEmpty(id) || id == "map") { Hide(); return; }
                if (id == _shownId && scenario == _shownOn) return;
                var tex = Get(id);
                if (tex == null) return;
                scenario.BGParam.SetTexture(tex);
                scenario.BGParam.visibleAll = true;
                if (Scenes.Staged) Mask(scenario, true); // girls posed in front of the picture keep their own colors
                else
                {
                    // No color mask: it still draws the (hidden) characters into Amplify Color's mask and they came out
                    // as dark see-through silhouettes over the picture.
                    Mask(scenario, false);
                    HideCharacters(scenario);
                }
                _shownOn = scenario;
                _shownId = id;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Event background show: " + e.Message); }
        }

        /// <summary>Back to the map (scene ended or the scene asked for it). Safe to call any time.</summary>
        public static void Hide()
        {
            var sc = _shownOn;
            _shownOn = null;
            _shownId = null;
            ShowCharacters();
            if (sc == null) return;
            try
            {
                if (sc.BGParam != null)
                {
                    sc.BGParam.visibleAll = false;
                    sc.BGParam.Release(false);
                }
                Mask(sc, false);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Event background hide: " + e.Message); }
        }

        /// <summary>
        /// Our map scenes are narration (no one posed for the camera): the player seen from behind and the students far
        /// away would float over the picture, so the camera stops drawing the character layer meanwhile.
        /// </summary>
        private static void HideCharacters(ADV.TextScenario sc)
        {
            if (_cam != null) return;
            var cam = Camera.main ?? sc.AdvCamera;
            var layer = LayerMask.NameToLayer("Chara");
            if (cam == null || layer < 0) return;
            _cam = cam;
            _camMask = cam.cullingMask;
            cam.cullingMask &= ~(1 << layer);
        }

        private static void ShowCharacters()
        {
            try { if (_cam != null) _cam.cullingMask = _camMask; }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Event background camera: " + e.Message); }
            _cam = null;
        }

        /// <summary>The game turns this on with a backdrop so posed characters keep their own colors; we hide them instead.</summary>
        private static void Mask(ADV.TextScenario sc, bool on)
        {
            if (sc.AdvCamera == null) return;
            var mask = sc.AdvCamera.GetComponentInChildren<CameraEffectorColorMask>();
            if (mask != null) mask.Enabled = on;
        }
    }
}
