using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// estão assim", pixelated). The game only has small thumbnails of its maps. The player takes the picture himself
    ///:
    /// he stands where he likes and presses "Take this map's photo" in the Alt+F7 window. His view is photographed without
    /// the characters, icons and rain, at 1920x1080, for the map and time of day (day, evening, night) he is in, and kept in
    /// BepInEx\config\joojkk_campuslife\mapshots. The loading screen shows that picture when it has one, the game's
    /// </summary>
    internal static class MapShots
    {
        private const int W = 1920, H = 1080;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        /// que estava boa a foto da sala 2-1 antes", a test's reset had deleted it).</summary>
        internal static string TestDir;

        private static string UserDir { get { return Path.Combine(Path.Combine(BepInEx.Paths.ConfigPath, "joojkk_campuslife"), "mapshots"); } }
        private static string Dir { get { return TestDir ?? UserDir; } }

        private static string FileFor(int map, int slot) { return Path.Combine(Dir, map + "_" + slot + ".jpg"); }

        /// <summary>The photos that come with the mod, under his own.</summary>
        private static string ShippedFor(int map, int slot) { return Path.Combine(Path.Combine(Path.Combine(BepInEx.Paths.PluginPath, "Joojkk"), "mapshots"), map + "_" + slot + ".jpg"); }

        internal static bool Shipped(int map, int slot) { return TestDir == null && File.Exists(ShippedFor(map, slot)); }

        internal static readonly string[] SlotNames = { "day", "evening", "night" };

        /// <summary>Why a picture cannot be taken right now (null when it can).</summary>
        internal static string Blocked()
        {
            var act = G.Act;
            return act == null || act.Map == null || act.Player == null ? "not in the action scene" : act.Map.isMapLoading || TransitionScreen.Visible ? "the map is loading"
                : Scenes.Running || G.TalkSceneOpen ? "a scene is open" : KKAPI.MainGame.GameAPI.InsideHScene ? "inside H" : StatusUi.Scene != null ? "a menu page is open"
                : Camera.main == null ? "no camera" : null;
        }

        /// <summary>Photographs his view for this map and time of day, over the old picture.</summary>
        internal static string TakeHere()
        {
            try
            {
                var why = Blocked();
                if (why != null) return "No photo: " + why + ".";
                var map = G.PlayerMapNo;
                var slot = EventBackdrop.TimeSlot();
                Forget(FileFor(map, slot));
                return Take(map, slot) ? "Photo of " + Places.MapLabel(map) + " (" + SlotNames[slot] + ") saved." : "The photo failed (see the log).";
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Map picture: " + e.Message); return "The photo failed: " + e.Message; }
        }

        /// <summary>Does this map have his picture for this time of day?</summary>
        internal static string SlotsOf(int map)
        {
            var o = new List<string>();
            for (var s = 0; s < 3; s++)
            {
                if (File.Exists(Path.Combine(UserDir, map + "_" + s + ".jpg"))) o.Add(SlotNames[s] + " (his)");
                else if (File.Exists(ShippedFor(map, s))) o.Add(SlotNames[s] + " (shipped)");
            }
            return o.Count == 0 ? "none" : string.Join(", ", o.ToArray());
        }

        internal static bool Has(int map, int slot) { return File.Exists(FileFor(map, slot)); }

        /// <summary>Deletes this map's picture for this time of day (the loading screen goes back to the game's thumbnail).</summary>
        internal static void Delete(int map, int slot)
        {
            var path = FileFor(map, slot);
            Forget(path);
            if (File.Exists(path)) File.Delete(path);
            CampusLifePlugin.Log.LogInfo("Map picture: " + G.MapName(map) + " (" + map + ", time " + slot + ") deleted");
        }

        private static void Forget(string path)
        {
            Texture2D t;
            if (Cache.TryGetValue(path, out t) && t != null) UnityEngine.Object.Destroy(t);
            Cache.Remove(path);
        }

        /// <summary>The player's own view (the game camera), with nobody in it.</summary>
        private static bool Take(int map, int slot)
        {
            var t0 = Time.realtimeSinceStartup;
            var ok = true;
            var go = new GameObject("joojkk_mapshot");
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.CopyFrom(Camera.main);
                cam.enabled = false;
                var chara = LayerMask.NameToLayer("Chara");
                if (chara >= 0) cam.cullingMask &= ~(1 << chara);
                // UI layers stay out, and what is closer than 0.8 m to the camera is cut.
                for (var l = 0; l < 32; l++)
                {
                    var ln = LayerMask.LayerToName(l) ?? "";
                    if (l == 5 || ln.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0 || ln.IndexOf("Icon", StringComparison.OrdinalIgnoreCase) >= 0 || ln.IndexOf("Map Object Icon", StringComparison.OrdinalIgnoreCase) >= 0)
                        cam.cullingMask &= ~(1 << l);
                }
                cam.nearClipPlane = Mathf.Max(cam.nearClipPlane, 0.8f);
                cam.aspect = (float)W / H;
                cam.targetTexture = rt;
                // Every character's renderers are off for this one frame.
                // (silhouette_template_*.prefab, shader Custom/Silhouette), the floating action icons are Sprites/Default on
                // the Map layer, and the rain is our particles. All of them, and every character, are off for this one frame.
                var hidden = new List<Renderer>();
                foreach (var cha in UnityEngine.Object.FindObjectsOfType<ChaControl>())
                    foreach (var r in cha.GetComponentsInChildren<Renderer>())
                        if (r != null && r.enabled) { r.enabled = false; hidden.Add(r); }
                foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
                {
                    if (r == null || !r.enabled) continue;
                    var m = r.sharedMaterial;
                    var sh = m != null && m.shader != null ? m.shader.name : "";
                    if (sh.IndexOf("Silhouette", StringComparison.OrdinalIgnoreCase) >= 0 || sh.StartsWith("Sprites/") || sh.StartsWith("Particles/") || r.name.StartsWith("Joojkk"))
                    { r.enabled = false; hidden.Add(r); }
                }
                try { cam.Render(); }
                finally { foreach (var r in hidden) if (r != null) r.enabled = true; }
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var o = new Texture2D(W, H, TextureFormat.RGB24, false);
                o.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                o.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;
                Directory.CreateDirectory(Dir);
                File.WriteAllBytes(FileFor(map, slot), o.EncodeToJPG(90));
                UnityEngine.Object.Destroy(o);
                CampusLifePlugin.Log.LogInfo("Map picture: " + G.MapName(map) + " (" + map + ", time " + slot + ") taken at " + W + "x" + H + " (layers " + Layers(cam.cullingMask) + ", " + hidden.Count + " renderers hidden: people, outlines, icons, rain) in " + Mathf.RoundToInt((Time.realtimeSinceStartup - t0) * 1000f) + " ms");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Map picture " + map + ": " + e.Message); ok = false; }
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(go);
            return ok;
        }

        private static string Layers(int mask)
        {
            var names = new List<string>();
            for (var l = 0; l < 32; l++) if ((mask & (1 << l)) != 0) { var n = LayerMask.LayerToName(l); names.Add(string.IsNullOrEmpty(n) ? l.ToString() : n); }
            return string.Join(",", names.ToArray());
        }

        /// <summary>The full-size picture of a map at this time of day (another time's if this one has none; null if none yet).</summary>
        public static Texture2D For(int map)
        {
            var slots = new[] { EventBackdrop.TimeSlot(), 0, 1, 2 };
            int twin;
            return For(map, slots) ?? (Twins.TryGetValue(map, out twin) ? For(twin, slots) : null);
        }

        /// <summary>
        /// the photo of a map built the same (classroom 2-1, the 2F toilet) until one of their own is taken.
        /// </summary>
        private static readonly Dictionary<int, int> Twins = new Dictionary<int, int> { { 5, 8 }, { 6, 8 }, { 7, 8 }, { 10, 9 }, { 11, 8 }, { 12, 9 }, { 13, 8 }, { 14, 15 } };

        /// <summary>
        /// For the Alt+F7 window:
        /// which picture the loading screen of this map uses now, found the way For finds it. Null when there is none.
        /// </summary>
        internal static string Source(int map)
        {
            var slots = new[] { EventBackdrop.TimeSlot(), 0, 1, 2 };
            var s = SourceOf(map, slots);
            int twin;
            if (s == null && Twins.TryGetValue(map, out twin)) { s = SourceOf(twin, slots); if (s != null) s += " of " + Places.MapLabel(twin); }
            return s;
        }

        private static string SourceOf(int map, int[] slots)
        {
            foreach (var s in slots)
            {
                if (File.Exists(FileFor(map, s)) || File.Exists(Path.Combine(UserDir, map + "_" + s + ".jpg"))) return "your photo (" + SlotNames[s] + ")";
                if (File.Exists(ShippedFor(map, s))) return "the mod's photo (" + SlotNames[s] + ")";
            }
            return null;
        }

        /// <summary>The picture of a map at exactly this time of day.</summary>
        public static Texture2D Exact(int map, int slot) { return For(map, new[] { slot }); }

        private static Texture2D For(int map, int[] slots)
        {
            foreach (var s in slots)
            {
                var path = FileFor(map, s);
                // certo"): a test writes its pictures in its own folder, but reads his and the shipped ones too (never
                // writing there), so a test's loading screen shows the pool as he sees it.
                if (!File.Exists(path) && TestDir != null) path = Path.Combine(UserDir, map + "_" + s + ".jpg");
                if (!File.Exists(path)) path = ShippedFor(map, s);
                Texture2D tex;
                if (Cache.TryGetValue(path, out tex) && tex != null) return tex;
                if (!File.Exists(path)) continue;
                try
                {
                    tex = new Texture2D(2, 2, TextureFormat.RGB24, true) { name = "cl_mapshot_" + map + "_" + s, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                    if (!tex.LoadImage(File.ReadAllBytes(path))) { UnityEngine.Object.Destroy(tex); continue; }
                    UnityEngine.Object.DontDestroyOnLoad(tex);
                    if (Cache.Count >= 6)
                    {
                        foreach (var old in Cache.Values) if (old != null) UnityEngine.Object.Destroy(old);
                        Cache.Clear();
                    }
                    Cache[path] = tex;
                    return tex;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Map picture file " + path + ": " + e.Message); }
            }
            return null;
        }

        public static void Reset()
        {
            foreach (var t in Cache.Values) if (t != null) UnityEngine.Object.Destroy(t);
            Cache.Clear();
            if (TestDir == null || !Directory.Exists(TestDir)) return;
            foreach (var f in Directory.GetFiles(TestDir, "*.jpg")) File.Delete(f);
        }
    }
}
