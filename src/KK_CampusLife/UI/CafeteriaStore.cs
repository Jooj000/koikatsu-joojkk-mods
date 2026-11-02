using System;
using KKAPI.MainGame;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The school store is a real spot in the cafeteria: an action icon like the game's own (lockers, vending
    /// machines...). Clicking it opens the store view of the Campus Life menu.
    /// </summary>
    internal static class CafeteriaStore
    {
        private static IDisposable _icon;
        private static int _registeredMap = -1;

        /// <summary>Default spot: in front of the cafeteria counter, between the plates (map 38 coordinates, from the furniture
        /// positions: counter x 4.3-5.2, z 0.5-4.1). The spots file can move it anywhere (PositionTool).</summary>
        public static Vector3 Position = new Vector3(3.8f, 0f, 2.3f);

        /// <summary>Re-reads the spot and places the icon again.</summary>
        public static void Replace()
        {
            if (_icon != null) _icon.Dispose();
            _icon = null;
            _registeredMap = -1;
        }

        public static void Update()
        {
            if (!Cfg.GiftsEnabled.Value && !Cfg.ActivitiesEnabled.Value) return;
            Spots.Spot custom;
            var hasCustom = Spots.TryGetStore(out custom);
            var map = hasCustom ? custom.Map : Places.MapFor("cafeteria");
            if (map < 0 || map == _registeredMap) return;
            try
            {
                if (_icon != null) _icon.Dispose();
                var on = StoreIcon.Get(true);
                var off = StoreIcon.Get(false);
                var pos = hasCustom ? custom.Pos : Position;
                _icon = GameAPI.AddActionIcon(map, pos, on, off, Open, null, true, true);
                _registeredMap = map;
                CampusLifePlugin.Log.LogInfo("Campus store placed on map " + map + " at " + pos + ".");
            }
            catch (Exception e)
            {
                _registeredMap = map;
                CampusLifePlugin.Log.LogError("Could not place the university store: " + e);
            }
        }

        private static void Open()
        {
            try { StatusUi.Open(PanelTab.Shop); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }
    }

    /// <summary>
    /// The store's map icon: Campus Life's round badge (IconArt.Spot) with a shopping bag, pink when you are next to it.
    /// icons (160 px at the same 100 pixels per unit), the bag a little bigger inside the badge.
    /// </summary>
    internal static class StoreIcon
    {
        private static Sprite _on, _off;
        private static readonly float[] Pink = { 1f, 0.45f, 0.62f };

        public static Sprite Get(bool on)
        {
            if (on && _on != null) return _on;
            if (!on && _off != null) return _off;
            var s = MapSprite.Make(IconArt.Spot("bag", Pink, 160, on, 1.12f), 160, 100f, on ? "cl_store_on" : "cl_store_off");
            if (on) _on = s; else _off = s;
            return s;
        }
    }

    /// <summary>A float RGBA picture (IconArt) as a sprite for the map (a SpriteRenderer: pixels per unit sets its size).</summary>
    internal static class MapSprite
    {
        public static Sprite Make(float[] px, int size, float ppu, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var cols = new Color[size * size];
            for (var i = 0; i < cols.Length; i++) cols[i] = new Color(px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]);
            tex.SetPixels(cols);
            tex.Apply(true, true);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu);
            sp.name = name;
            return sp;
        }
    }
}

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Activity spot icons: one drawing per activity (a book to
    /// study, a ball for basketball, a wave at the pool, a paw for the cats...) on the round badge, the ring in the color of
    /// the stat it trains. A spot with several activities shows the first one of its main kind.
    /// </summary>
    internal static class ActivityIcon
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Cache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite Get(ActivitySpots.Kind kind, bool on) { return Get(kind, null, on); }

        public static Sprite Get(ActivitySpots.Kind kind, string activityId, bool on)
        {
            var glyph = activityId != null ? IconArt.GlyphOf(activityId) : Fallback(kind);
            var key = kind + ":" + glyph + (on ? "_on" : "_off");
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            s = MapSprite.Make(IconArt.Spot(glyph, Tint(kind), 128, on), 128, 100f, "cl_act_" + glyph + (on ? "_on" : "_off"));
            Cache[key] = s;
            return s;
        }

        private static string Fallback(ActivitySpots.Kind kind)
        {
            switch (kind)
            {
                case ActivitySpots.Kind.Int: return "book";
                case ActivitySpots.Kind.Str: return "dumbbell";
                case ActivitySpots.Kind.Sens: return "star";
                case ActivitySpots.Kind.Job: return "coin";
                default: return "moon";
            }
        }

        /// <summary>The stat's color (the same as the clipboard's stat bars).</summary>
        public static float[] Tint(ActivitySpots.Kind kind)
        {
            switch (kind)
            {
                case ActivitySpots.Kind.Int: return new[] { 0.42f, 0.71f, 1f };
                case ActivitySpots.Kind.Str: return new[] { 1f, 0.42f, 0.42f };
                case ActivitySpots.Kind.Sens: return new[] { 1f, 0.54f, 0.85f };
                case ActivitySpots.Kind.Job: return new[] { 0.95f, 0.72f, 0.2f };
                default: return new[] { 0.45f, 0.78f, 0.5f };
            }
        }
    }
}
