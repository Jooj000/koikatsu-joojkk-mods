using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Painted backgrounds behind our event scenes.
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

        private static string Dir { get { return Path.Combine(Path.Combine(Paths.PluginPath, "Joojkk"), "backgrounds"); } }

        public static bool Exists(string id)
        {
            return !string.IsNullOrEmpty(id) && File.Exists(Path.Combine(Dir, id + ".jpg"));
        }

        /// <summary>Loads the pictures a scene will use before it starts (each one once per game).</summary>
        public static void Preload(IEnumerable<string> ids)
        {
            foreach (var id in ids) Get(id);
        }

        private static Texture2D Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Texture2D tex;
            if (Cache.TryGetValue(id, out tex) && tex != null) return tex;
            var path = Path.Combine(Dir, id + ".jpg");
            if (!File.Exists(path)) { CampusLifePlugin.Log.LogDebug("Event background missing: " + path); Cache[id] = null; return null; }
            try
            {
                tex = new Texture2D(2, 2, TextureFormat.RGB24, false) { name = "cl_bg_" + id, wrapMode = TextureWrapMode.Clamp };
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
