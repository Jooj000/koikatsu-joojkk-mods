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

        /// <summary>Her minimap badge: the same as on the board.</summary>
        public static Sprite For(Request r) { return Badge(r); }

        // The request is a round badge with the drawing of what she asks for (a
        // speech bubble to talk, a bowl for lunch, a book to study...). The first request of a kind has the kind's color; a
        // second or third one with the same drawing takes the next colors, so the badge on her minimap icon matches one
        // request on the board. The gold star stays today's story heroine.
        private static readonly Dictionary<string, Sprite> BadgeCache = new Dictionary<string, Sprite>();

        public static Sprite Badge(Request r)
        {
            if (r == null) return StoryBadge();
            var glyph = GlyphOf(r);
            return Badge(glyph, ColorOf(r, glyph));
        }

        public static Sprite StoryBadge() { return Badge("star", Gold); }

        /// <summary>A round badge with any drawing (IconArt's glyphs), for the panel's rows.</summary>
        public static Sprite Glyph(string glyph, Color c) { return Badge(glyph, new[] { c.r, c.g, c.b }); }

        internal static string Describe(Request r)
        {
            var g = GlyphOf(r);
            var c = ColorOf(r, g);
            return g + " rgb(" + c[0].ToString("0.00") + "," + c[1].ToString("0.00") + "," + c[2].ToString("0.00") + ")";
        }

        /// <summary>What she asks for, as a drawing.</summary>
        public static string GlyphOf(Request r)
        {
            if (r.Lewd || r.Kind == RequestKind.Intimate) return "heart";
            switch (r.Kind)
            {
                case RequestKind.Talk: return "bubble";
                case RequestKind.MeetAt: return "pin";
                case RequestKind.Lunch: return "bowl";
                case RequestKind.Study: return "book";
                case RequestKind.Exercise: return "dumbbell";
                case RequestKind.Club: return "people";
                case RequestKind.WalkHome: return "shoe";
                case RequestKind.Gift: return "gift";
                case RequestKind.Errand: return "box";
                case RequestKind.Activity: return Activities.Get(r.Activity) != null ? IconArt.GlyphOf(r.Activity) : "star";
                default: return "star";
            }
        }

        private static readonly float[][] Palette =
        {
            new[] { 0.42f, 0.7f, 0.98f }, new[] { 1f, 0.6f, 0.3f }, new[] { 0.45f, 0.82f, 0.52f }, new[] { 0.7f, 0.5f, 0.92f },
            new[] { 0.98f, 0.45f, 0.42f }, new[] { 0.3f, 0.8f, 0.82f }, new[] { 0.95f, 0.78f, 0.28f }, new[] { 1f, 0.55f, 0.8f },
        };

        /// <summary>The kind's color for the first request with this drawing, the next colors for the others.</summary>
        private static float[] ColorOf(Request r, string glyph)
        {
            var same = Board.Active.Where(x => x != null && GlyphOf(x) == glyph).OrderBy(x => x.Id).ToList();
            var n = Math.Max(0, same.FindIndex(x => x.Id == r.Id));
            var first = KindColor(r);
            if (n == 0) return first;
            var start = Array.FindIndex(Palette, c => Math.Abs(c[0] - first[0]) + Math.Abs(c[1] - first[1]) + Math.Abs(c[2] - first[2]) < 0.15f);
            return Palette[((start < 0 ? 0 : start) + n) % Palette.Length];
        }

        private static readonly float[] Gold = { 0.98f, 0.76f, 0.26f };

        /// <summary>The badge's color as a Color (the board block paints its kind label with it).</summary>
        public static Color TintOf(Request r)
        {
            var c = ColorOf(r, GlyphOf(r));
            return new Color(c[0], c[1], c[2], 1f);
        }

        /// <summary>What kind of request it is, in one or two words (the colored label on the board).</summary>
        public static string KindLabel(Request r)
        {
            if (r.Source == "arc") return "Story";
            if (r.Lewd || r.Kind == RequestKind.Intimate) return "Private";
            switch (r.Kind)
            {
                case RequestKind.Talk: return "Talk";
                case RequestKind.MeetAt: return "Meet up";
                case RequestKind.Lunch: return "Lunch";
                case RequestKind.Study: return "Study";
                case RequestKind.Exercise: return "Workout";
                case RequestKind.Club: return "Club";
                case RequestKind.WalkHome: return "Walk home";
                case RequestKind.Gift: return "Gift";
                case RequestKind.Errand: return "Errand";
                case RequestKind.Activity: return "Activity";
                default: return "Request";
            }
        }

        private static float[] KindColor(Request r)
        {
            if (r.Lewd || r.Kind == RequestKind.Intimate) return Palette[7];
            switch (r.Kind)
            {
                case RequestKind.Talk: case RequestKind.Study: return Palette[0];
                case RequestKind.Lunch: return Palette[1];
                case RequestKind.Errand: case RequestKind.Activity: return Palette[2];
                case RequestKind.WalkHome: return Palette[3];
                case RequestKind.Exercise: case RequestKind.Club: return Palette[4];
                case RequestKind.MeetAt: return Palette[5];
                case RequestKind.Gift: return Palette[7];
                default: return Palette[6];
            }
        }

        private static Sprite Badge(string glyph, float[] color)
        {
            var key = glyph + ":" + color[0] + "," + color[1] + "," + color[2];
            Sprite s;
            if (BadgeCache.TryGetValue(key, out s) && s != null) return s;
            const int size = 72;
            s = MapSprite.Make(IconArt.RequestBadge(glyph, color, size), size, 100f, "cl_req_" + glyph);
            BadgeCache[key] = s;
            return s;
        }

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
                var s = parentRt != null ? Mathf.Clamp(parentRt.rect.width * 0.48f, 10f, 14f) : 12f;
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
