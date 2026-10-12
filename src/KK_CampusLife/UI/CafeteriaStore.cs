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

    /// <summary>Store icon in the style of the game's map action icons.</summary>
    internal static class StoreIcon
    {
        private static Sprite _on, _off;

        public static Sprite Get(bool on)
        {
            if (on && _on != null) return _on;
            if (!on && _off != null) return _off;
            var s = Draw(on);
            if (on) _on = s; else _off = s;
            return s;
        }

        /// <summary>A round badge with a shopping bag, pink when in range like the game's icons.</summary>
        private static Sprite Draw(bool on)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { name = on ? "cl_store_on" : "cl_store_off", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var ring = on ? new Color(1f, 0.45f, 0.62f, 1f) : new Color(0.55f, 0.55f, 0.6f, 1f);
            var face = Color.white;
            var bag = on ? new Color(1f, 0.45f, 0.62f, 1f) : new Color(0.45f, 0.45f, 0.5f, 1f);
            const int ss = 3;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var acc = new Color(0, 0, 0, 0);
                for (var sy = 0; sy < ss; sy++)
                for (var sx = 0; sx < ss; sx++)
                {
                    var u = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                    var v = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                    var r = Mathf.Sqrt(u * u + v * v);
                    Color c;
                    if (r > 0.96f) c = new Color(0, 0, 0, 0);
                    else if (r > 0.84f) c = ring;
                    else if (InBag(u, v)) c = bag;
                    else c = face;
                    acc += c;
                }
                px[y * size + x] = acc / (ss * ss);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sp.name = tex.name;
            return sp;
        }

        private static bool InBag(float x, float y)
        {
            // Bag body (trapezoid) plus a handle arc.
            var body = y >= -0.5f && y <= 0.18f && Mathf.Abs(x) <= 0.42f - (y + 0.5f) * 0.08f;
            var hx = x;
            var hy = y - 0.18f;
            var hr = Mathf.Sqrt(hx * hx + hy * hy);
            var handle = hy >= 0f && hr <= 0.26f && hr >= 0.17f;
            return body || handle;
        }
    }
}

namespace Joojkk.CampusLife
{
    /// <summary>Activity spot icons: the store badge style with a glyph and the color of the stat the activity trains.</summary>
    internal static class ActivityIcon
    {
        private static readonly System.Collections.Generic.Dictionary<string, Sprite> Cache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite Get(ActivitySpots.Kind kind, bool on)
        {
            var key = kind + (on ? "_on" : "_off");
            Sprite s;
            if (Cache.TryGetValue(key, out s) && s != null) return s;
            s = Draw(kind, on);
            Cache[key] = s;
            return s;
        }

        private static Color Tint(ActivitySpots.Kind kind)
        {
            switch (kind)
            {
                case ActivitySpots.Kind.Int: return new Color(0.42f, 0.71f, 1f, 1f);
                case ActivitySpots.Kind.Str: return new Color(1f, 0.42f, 0.42f, 1f);
                case ActivitySpots.Kind.Sens: return new Color(1f, 0.54f, 0.85f, 1f);
                case ActivitySpots.Kind.Job: return new Color(0.95f, 0.75f, 0.25f, 1f);
                default: return new Color(0.45f, 0.82f, 0.5f, 1f);
            }
        }

        private static Sprite Draw(ActivitySpots.Kind kind, bool on)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { name = "cl_act_" + kind + (on ? "_on" : "_off"), wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var tint = Tint(kind);
            var ring = on ? tint : new Color(0.55f, 0.55f, 0.6f, 1f);
            var glyph = on ? tint : new Color(0.45f, 0.45f, 0.5f, 1f);
            const int ss = 3;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var acc = new Color(0, 0, 0, 0);
                for (var sy = 0; sy < ss; sy++)
                for (var sx = 0; sx < ss; sx++)
                {
                    var u = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                    var v = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                    var r = Mathf.Sqrt(u * u + v * v);
                    Color c;
                    if (r > 0.96f) c = new Color(0, 0, 0, 0);
                    else if (r > 0.84f) c = ring;
                    else if (Glyph(kind, u, v)) c = glyph;
                    else c = Color.white;
                    acc += c;
                }
                px[y * size + x] = acc / (ss * ss);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sp.name = tex.name;
            return sp;
        }

        private static bool Glyph(ActivitySpots.Kind kind, float x, float y)
        {
            switch (kind)
            {
                case ActivitySpots.Kind.Int:
                {
                    // Open book: two pages tilted up from the spine.
                    var ax = Mathf.Abs(x);
                    var top = 0.3f - ax * 0.25f;
                    var bottom = -0.38f + ax * 0.1f;
                    var page = ax <= 0.5f && y <= top && y >= bottom && ax >= 0.04f;
                    var lines = page && ax > 0.12f && ax < 0.42f && Mathf.Repeat(y + 0.4f, 0.16f) < 0.05f && y < top - 0.06f && y > bottom + 0.06f;
                    return page && !lines;
                }
                case ActivitySpots.Kind.Str:
                {
                    // Dumbbell: bar and two plates.
                    var ax = Mathf.Abs(x);
                    var bar = ax <= 0.45f && Mathf.Abs(y) <= 0.06f;
                    var plate = ax >= 0.3f && ax <= 0.42f && Mathf.Abs(y) <= 0.3f;
                    var outer = ax > 0.42f && ax <= 0.52f && Mathf.Abs(y) <= 0.18f;
                    return bar || plate || outer;
                }
                case ActivitySpots.Kind.Sens:
                {
                    // Heart: (x^2 + y^2 - 1)^3 - x^2 y^3 <= 0, scaled.
                    var hx = x / 0.5f;
                    var hy = (y + 0.05f) / 0.5f;
                    var a = hx * hx + hy * hy - 1f;
                    return a * a * a - hx * hx * hy * hy * hy <= 0f;
                }
                case ActivitySpots.Kind.Job:
                {
                    // Coin with a yen sign.
                    var r = Mathf.Sqrt(x * x + y * y);
                    if (r > 0.5f) return false;
                    if (r > 0.42f) return true;
                    var ax = Mathf.Abs(x);
                    var stem = ax <= 0.05f && y <= 0.02f && y >= -0.3f;
                    var arms = y >= 0.02f && y <= 0.3f && Mathf.Abs(ax - (y - 0.02f) * 0.7f) <= 0.05f;
                    var bars = ax <= 0.18f && (Mathf.Abs(y + 0.04f) <= 0.025f || Mathf.Abs(y + 0.14f) <= 0.025f);
                    return !(stem || arms || bars);
                }
                default:
                {
                    // Crescent moon.
                    var inA = x * x + y * y <= 0.45f * 0.45f;
                    var dx = x - 0.2f;
                    var dy = y - 0.15f;
                    var inB = dx * dx + dy * dy <= 0.38f * 0.38f;
                    return inA && !inB;
                }
            }
        }
    }
}
