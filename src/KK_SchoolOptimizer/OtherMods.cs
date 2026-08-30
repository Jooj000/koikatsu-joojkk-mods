using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Performance fixes for other authors' plugins, applied from outside (their files are not changed). Each one is
    /// skipped when the plugin is not installed or its code no longer looks the same.
    ///
    /// AmazingNewAccessoryLogic (ANAL) by njaecha: every character gets a camera that renders into its own full-screen
    /// render texture (about 11 MB of video memory each at 1600x900) on every frame, even while the node editor is
    /// closed, so a school with 28 students paid 28 extra camera passes per frame and ~320 MB of VRAM. The camera is
    /// only needed while that character's editor is open; it is now switched off (and its texture freed) otherwise.
    /// ANAL's accessory logic itself keeps running normally.
    ///
    /// UncensorSelector by DeathWeasel1337 and contributors: every uncensor part gets its skin colour painted into its own
    /// 2048x2048 texture (21 MB of video memory with mipmaps), also the futa penis of girls that do not show one (the
    /// part is loaded and hidden). 29 girls = ~600 MB for meshes nobody sees. While UncensorSelector paints a part
    /// that card hides (penis or balls switched off), the texture is made small instead (see
    /// <see cref="HiddenUncensorPart"/>); a card that shows it is untouched, and switching it on rebuilds it at full size.
    /// </summary>
    internal static class OtherMods
    {
        private static FieldInfo _cam, _tex, _shown;
        public static int AnalCamerasOff;

        /// <summary>True while UncensorSelector paints the skin of a part this character keeps hidden.</summary>
        public static bool HiddenUncensorPart;
        public static int HiddenUncensorTextures;
        private static PropertyInfo _displayPenis, _displayBalls, _penisData, _ballsData;

        public static void Patch(Harmony harmony)
        {
            try { PatchUncensor(harmony); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogWarning("UncensorSelector hidden part fix not available: " + e.Message); }
            try { PatchMaterialEditor(harmony); }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogWarning("MaterialEditor texture size limit not available: " + e.Message); }
            PatchAnal(harmony);
        }

        // ---------- MaterialEditor ----------
        public static int MeTexturesShrunk;
        public static long MeBytesSaved;

        private static void PatchMaterialEditor(Harmony harmony)
        {
            var t = AccessTools.TypeByName("KK_Plugins.TextureContainerManager");
            if (t == null) return;
            var load = AccessTools.Method(t, "TextureFromBytes");
            if (load == null || load.ReturnType != typeof(Texture))
            {
                SchoolOptimizerPlugin.Log.LogWarning("MaterialEditor changed; its texture size limit is off.");
                return;
            }
            harmony.Patch(load, postfix: new HarmonyMethod(typeof(OtherMods), nameof(MeTexturePostfix)));
            SchoolOptimizerPlugin.Log.LogInfo("MaterialEditor texture size limit installed.");
        }

        /// <summary>
        /// MaterialEditor (by DeathWeasel1337 and contributors) keeps every texture stored in a card as an uncompressed
        /// render texture with mipmaps, as big as the image: 85 MB of video memory for a 4096 one. In story mode, bigger
        /// ones are copied into a smaller render texture. The card's own image data is kept by MaterialEditor, so saving
        /// or exporting is unaffected; only what is drawn gets smaller.
        /// </summary>
        private static void MeTexturePostfix(ref Texture __result)
        {
            try
            {
                var src = __result as RenderTexture;
                var max = SchoolOptimizerPlugin.MeMaxTexture.Value;
                if (src == null || max <= 0 || !SchoolOptimizerPlugin.Enabled.Value || Mathf.Max(src.width, src.height) <= max) return;
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.MainGame) return;
                var k = (float)max / Mathf.Max(src.width, src.height);
                var dst = new RenderTexture(Mathf.Max(1, Mathf.RoundToInt(src.width * k)), Mathf.Max(1, Mathf.RoundToInt(src.height * k)), 0, src.format);
                dst.useMipMap = src.useMipMap;
                dst.wrapMode = src.wrapMode;
                dst.filterMode = src.filterMode;
                dst.anisoLevel = src.anisoLevel;
                Graphics.Blit(src, dst);
                MeBytesSaved += (long)src.width * src.height * 4 - (long)dst.width * dst.height * 4;
                MeTexturesShrunk++;
                src.Release();
                UnityEngine.Object.Destroy(src);
                __result = dst;
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("MaterialEditor texture: " + e.Message); }
        }

        private static void PatchUncensor(Harmony harmony)
        {
            var t = AccessTools.TypeByName("KK_Plugins.UncensorSelector+UncensorSelectorController");
            if (t == null) return;
            _displayPenis = AccessTools.Property(t, "DisplayPenis");
            _displayBalls = AccessTools.Property(t, "DisplayBalls");
            _penisData = AccessTools.Property(t, "PenisData");
            _ballsData = AccessTools.Property(t, "BallsData");
            var paint = AccessTools.GetDeclaredMethods(t).FirstOrDefault(m => m.Name == "SetSkinColor" && m.GetParameters().Length == 2);
            if (_displayPenis == null || _displayBalls == null || _penisData == null || _ballsData == null || paint == null)
            {
                SchoolOptimizerPlugin.Log.LogWarning("UncensorSelector changed; its hidden part fix is off.");
                return;
            }
            harmony.Patch(paint, prefix: new HarmonyMethod(typeof(OtherMods), nameof(UncensorPaintPrefix)),
                postfix: new HarmonyMethod(typeof(OtherMods), nameof(UncensorPaintPostfix)));
            SchoolOptimizerPlugin.Log.LogInfo("UncensorSelector hidden part fix installed.");
        }

        private static void UncensorPaintPrefix(object __instance, string file)
        {
            HiddenUncensorPart = false;
            try
            {
                if (!SchoolOptimizerPlugin.Enabled.Value || !SchoolOptimizerPlugin.UncensorHiddenParts.Value || string.IsNullOrEmpty(file)) return;
                if (KKAPI.KoikatuAPI.GetCurrentGameMode() != KKAPI.GameMode.MainGame) return;
                HiddenUncensorPart = Hides(__instance, _displayPenis, _penisData, file) || Hides(__instance, _displayBalls, _ballsData, file);
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("UncensorSelector part: " + e.Message); }
        }

        private static void UncensorPaintPostfix() { HiddenUncensorPart = false; }

        private static bool Hides(object controller, PropertyInfo display, PropertyInfo data, string file)
        {
            if ((bool)display.GetValue(controller, null)) return false;
            var d = data.GetValue(controller, null);
            var f = d == null ? null : AccessTools.Field(d.GetType(), "File");
            return f != null && file == f.GetValue(d) as string;
        }

        private static void PatchAnal(Harmony harmony)
        {
            var t = AccessTools.TypeByName("AmazingNewAccessoryLogic.AnalCharaController");
            if (t == null)
            {
                // Installed but not found: the load order is wrong again (see the soft dependency on the plugin class).
                if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("org.njaecha.plugins.anal"))
                    SchoolOptimizerPlugin.Log.LogWarning("AmazingNewAccessoryLogic is installed but its types were not found; its idle camera fix is off.");
                return;
            }
            _cam = AccessTools.Field(t, "rCam");
            _tex = AccessTools.Field(t, "rTex");
            _shown = AccessTools.Field(t, "displayGraph");
            var start = AccessTools.Method(t, "Start");
            var show = AccessTools.Method(t, "Show");
            var hide = AccessTools.Method(t, "Hide");
            if (_cam == null || _tex == null || _shown == null || start == null || show == null || hide == null)
            {
                SchoolOptimizerPlugin.Log.LogWarning("AmazingNewAccessoryLogic changed; its idle camera fix is off.");
                return;
            }
            var post = new HarmonyMethod(typeof(OtherMods), nameof(AnalSync));
            harmony.Patch(start, postfix: post);
            harmony.Patch(show, postfix: post);
            harmony.Patch(hide, postfix: post);
            SchoolOptimizerPlugin.Log.LogInfo("AmazingNewAccessoryLogic idle camera fix installed.");
        }

        private static void AnalSync(object __instance)
        {
            try
            {
                var cam = _cam.GetValue(__instance) as Camera;
                if (cam == null) return;
                var shown = (bool)_shown.GetValue(__instance);
                var want = shown || !SchoolOptimizerPlugin.Enabled.Value || !SchoolOptimizerPlugin.AnalCameras.Value;
                if (cam.enabled == want) return;
                cam.enabled = want;
                if (!want)
                {
                    AnalCamerasOff++;
                    // Unity recreates the texture by itself the next time the camera renders into it.
                    var tex = _tex.GetValue(__instance) as RenderTexture;
                    if (tex != null && tex.IsCreated()) tex.Release();
                }
            }
            catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("ANAL camera: " + e.Message); }
        }
    }
}
