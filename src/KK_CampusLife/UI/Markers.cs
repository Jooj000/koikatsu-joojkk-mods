using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Request markers: every open request gets a shape (star, diamond, heart...) shown on the girl's minimap icon,
    /// on her clipboard card and next to the request in the Campus Life menu. Shapes, not colors, because the game
    /// already colors the minimap icons by relationship.
    /// </summary>
    internal static class Markers
    {
        public static readonly string[] ShapeNames = { "Star", "Diamond", "Heart", "Triangle", "Moon", "Cross", "Square", "Drop", "Hexagon", "Circle" };

        private static readonly Sprite[] Cache = new Sprite[ShapeNames.Length];

        /// <summary>The star is kept for today's story heroine; requests use the other nine shapes.</summary>
        public const int StoryShape = 0;

        public static int ShapeOf(Request r) { return r == null ? 1 : 1 + Math.Abs(r.Id) % (ShapeNames.Length - 1); }

        public static Sprite For(Request r) { return Get(ShapeOf(r)); }

        public static Sprite Get(int shape)
        {
            shape = Mathf.Clamp(shape, 0, ShapeNames.Length - 1);
            if (Cache[shape] != null) return Cache[shape];
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false) { name = "cl_marker_" + ShapeNames[shape], wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            var fill = new Color(1f, 0.97f, 0.88f, 1f);
            var line = new Color(0.12f, 0.08f, 0.12f, 1f);
            const int ss = 4; // supersampling per axis
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                int outer = 0, inner = 0;
                for (var sy = 0; sy < ss; sy++)
                for (var sx = 0; sx < ss; sx++)
                {
                    var u = ((x + (sx + 0.5f) / ss) / size) * 2f - 1f;
                    var v = ((y + (sy + 0.5f) / ss) / size) * 2f - 1f;
                    if (Inside(shape, u / 0.98f, v / 0.98f)) outer++;
                    if (Inside(shape, u / 0.76f, v / 0.76f)) inner++;
                }
                var a = outer / (float)(ss * ss);
                var f = inner / (float)(ss * ss);
                var c = Color.Lerp(line, fill, a <= 0f ? 0f : f / a);
                c.a = a;
                px[y * size + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            UnityEngine.Object.DontDestroyOnLoad(tex);
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sp.name = tex.name;
            UnityEngine.Object.DontDestroyOnLoad(sp);
            Cache[shape] = sp;
            return sp;
        }

        /// <summary>Point-in-shape test in [-1,1] space (y up).</summary>
        private static bool Inside(int shape, float x, float y)
        {
            var ax = Mathf.Abs(x);
            var ay = Mathf.Abs(y);
            switch (shape)
            {
                case 0: return InStar(x, y);
                case 1: return ax + ay <= 0.95f;
                case 2:
                {
                    // Classic implicit heart, scaled to fit.
                    var hx = x * 1.25f;
                    var hy = (y + 0.15f) * 1.25f;
                    var k = hx * hx + hy * hy - 1f;
                    return k * k * k - hx * hx * hy * hy * hy <= 0f;
                }
                case 3: return y >= -0.72f && y <= 0.88f - 1.85f * ax;
                case 4:
                {
                    var inBig = x * x + y * y <= 0.85f * 0.85f;
                    var dx = x - 0.38f;
                    var dy = y - 0.22f;
                    return inBig && dx * dx + dy * dy > 0.68f * 0.68f;
                }
                case 5: return (ax <= 0.27f && ay <= 0.88f) || (ay <= 0.27f && ax <= 0.88f);
                case 6: return ax <= 0.72f && ay <= 0.72f;
                case 7:
                {
                    var cy = y + 0.28f;
                    if (x * x + cy * cy <= 0.6f * 0.6f) return true;
                    return y >= -0.28f && y <= 0.92f && ax <= 0.6f * (0.92f - y) / 1.2f;
                }
                case 8: return ay <= 0.85f * 0.866f && ay <= 1.732f * (0.85f - ax);
                default:
                {
                    var r2 = x * x + y * y;
                    return r2 <= 0.85f * 0.85f && r2 >= 0.42f * 0.42f;
                }
            }
        }

        private static bool InStar(float x, float y)
        {
            // Five-pointed star polygon, point up.
            const int n = 10;
            var inside = false;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi, yi, xj, yj;
                StarVertex(i, out xi, out yi);
                StarVertex(j, out xj, out yj);
                if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) inside = !inside;
            }
            return inside;
        }

        private static void StarVertex(int i, out float x, out float y)
        {
            var r = i % 2 == 0 ? 0.98f : 0.42f;
            var ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            x = Mathf.Cos(ang) * r;
            y = Mathf.Sin(ang) * r - 0.06f;
        }

        // ---------------- minimap ----------------

        private static readonly AccessTools.FieldRef<NPC, ActionGame.bl_MiniMapItem> NpcIcon = AccessTools.FieldRefAccess<NPC, ActionGame.bl_MiniMapItem>("miniMapIcon");
        private static float _next;
        private const string ChildName = "cl_request_marker";

        /// <summary>Requests that should be highlighted right now (open, due today or later, in a period that is still ahead).</summary>
        public static Request ShownFor(SaveData.Heroine h)
        {
            if (h == null || !Campus.On) return null;
            var key = GirlInfo.Key(h);
            return Board.ActiveFor(key).OrderBy(r => r.DeadlineDay).ThenBy(r => r.Id).FirstOrDefault();
        }

        private static readonly List<KeyValuePair<RectTransform, RectTransform>> Badges = new List<KeyValuePair<RectTransform, RectTransform>>();

        public static void UpdateMinimap()
        {
            Upright();
            if (Time.realtimeSinceStartup < _next) return;
            _next = Time.realtimeSinceStartup + 0.75f;
            var act = G.Act;
            if (act == null || act.npcList == null) return;
            foreach (var npc in act.npcList)
            {
                if (npc == null) continue;
                ActionGame.bl_MiniMapItem item;
                try { item = NpcIcon(npc); } catch { continue; }
                if (item == null || item.iconItem == null) continue;
                var r = Cfg.Enabled.Value ? ShownFor(npc.heroine) : null;
                Apply(item.iconItem.transform, r, item.iconItem.graphic != null ? item.iconItem.graphic.rectTransform : null);
            }
        }

        /// <summary>
        /// Every frame: the badge sits at the icon's top-right corner on the screen and stands upright. As a child of the
        /// icon it turned with it (the icon points where the girl looks) and circled around it.
        /// </summary>
        private static void Upright()
        {
            for (var i = Badges.Count - 1; i >= 0; i--)
            {
                var icon = Badges[i].Key;
                var badge = Badges[i].Value;
                if (icon == null || badge == null) { Badges.RemoveAt(i); continue; }
                if (!badge.gameObject.activeInHierarchy) continue;
                var half = icon.rect.size * 0.5f;
                badge.rotation = Quaternion.identity;
                badge.position = icon.TransformPoint(icon.rect.center) + new Vector3(half.x, half.y, 0f) * icon.lossyScale.x;
            }
        }

        private static void Apply(Transform icon, Request r, RectTransform graphic)
        {
            var t = icon.Find(ChildName);
            if (r == null)
            {
                if (t != null && t.gameObject.activeSelf) t.gameObject.SetActive(false);
                return;
            }
            Image img;
            if (t == null)
            {
                img = GameUi.Image(icon, ChildName, null, Color.white);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(1f, 1f);
                // A small corner badge, not a second icon.
                var parentRt = icon as RectTransform;
                var s = parentRt != null ? Mathf.Clamp(parentRt.rect.width * 0.4f, 8f, 12f) : 10f;
                rt.sizeDelta = new Vector2(s, s);
                var anchor = graphic ?? parentRt;
                if (anchor != null) Badges.Add(new KeyValuePair<RectTransform, RectTransform>(anchor, rt));
            }
            else img = t.GetComponent<Image>();
            if (img == null) return;
            var sp = For(r);
            if (img.sprite != sp) img.sprite = sp;
            if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
        }
    }
}
