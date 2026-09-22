using System;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Campus Life's own drawings for the conversation (user, 2026-10-26: "redo the drawings of the 5 buttons and their
    /// options in the mod's style"): a graphite tile with a thin frame in the category's color and a white symbol (speech
    /// bubble, gift, two people, heart, flame), and the option cards. Drawn from shape formulas (signed distances) into
    /// plain RGBA arrays, with no Unity types, so tools/iconpreview can draw the same pictures outside the game.
    /// </summary>
    internal static partial class IconArt
    {
        public const int Talk = 0, Actions = 1, Together = 2, Romance = 3, Spicy = 4;

        /// <summary>The category colors: the mod's INT blue, gold, green, SEN pink and STR red.</summary>
        public static readonly float[][] Accent =
        {
            new[] { 0.42f, 0.71f, 1f },
            new[] { 1f, 0.84f, 0.42f },
            new[] { 0.55f, 0.95f, 0.62f },
            new[] { 1f, 0.54f, 0.85f },
            new[] { 1f, 0.45f, 0.42f },
        };

        private static readonly float[] Graphite = { 0.07f, 0.07f, 0.09f };

        /// <summary>Where a card's inside starts, as a share of the card's size from its edge: after the color strip on the
        /// left, and inside the thin frame elsewhere (the shapes in <see cref="Card"/>: strip to u = -0.62, frame to 0.145).</summary>
        public const float CardStrip = 0.19f, CardFrame = 0.0725f;

        /// <summary>A category tile, size x size, RGBA rows from the bottom (Unity's order). <paramref name="on"/>: the open one.</summary>
        public static float[] Tile(int kind, int size, bool on)
        {
            var px = new float[size * size * 4];
            var a = Accent[kind];
            var unit = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var box = RoundBox(u, v, 0.9f, 0.9f, 0.26f);
                var fill = Cover(box, unit);
                if (fill <= 0f) continue;
                // the tile: graphite, a soft glow of the color from below, brighter when open
                var glow = Clamp01(0.5f - v * 0.5f) * (on ? 0.55f : 0.22f);
                var r = Lerp(Graphite[0], a[0], glow);
                var g = Lerp(Graphite[1], a[1], glow);
                var b = Lerp(Graphite[2], a[2], glow);
                // the frame: thin, in the category color (white when open)
                var ring = Cover(Math.Abs(box + 0.045f) - (on ? 0.04f : 0.028f), unit);
                var fr = on ? 1f : a[0];
                var fg = on ? 1f : a[1];
                var fb = on ? 1f : a[2];
                r = Lerp(r, fr, ring); g = Lerp(g, fg, ring); b = Lerp(b, fb, ring);
                // the symbol, white, with a thin shadow under it
                var sym = Symbol(kind, u, v);
                var shadow = Cover(Symbol(kind, u, v + 0.05f) - 0.01f, unit) * 0.5f;
                r = Lerp(r, 0f, shadow); g = Lerp(g, 0f, shadow); b = Lerp(b, 0f, shadow);
                var s = Cover(sym, unit);
                r = Lerp(r, 1f, s); g = Lerp(g, 1f, s); b = Lerp(b, 1f, s);
                var i = (y * size + x) * 4;
                px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = fill * 0.95f;
            }
            return px;
        }

        /// <summary>
        /// An option card for the list (sliced: the corners keep their shape at any width): graphite with a thin frame,
        /// a color strip on the left. <paramref name="kind"/> -1 is a neutral card (white frame, no strip).
        /// </summary>
        public static float[] Card(int kind, int size, out int border)
        {
            border = size * 3 / 8;
            var px = new float[size * size * 4];
            var a = kind >= 0 ? Accent[kind] : new[] { 0.85f, 0.85f, 0.9f };
            var unit = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var box = RoundBox(u, v, 0.94f, 0.94f, 0.42f);
                var fill = Cover(box, unit);
                if (fill <= 0f) continue;
                float r = Graphite[0], g = Graphite[1], b = Graphite[2];
                var ring = Cover(Math.Abs(box + 0.05f) - 0.035f, unit) * 0.85f;
                r = Lerp(r, a[0], ring); g = Lerp(g, a[1], ring); b = Lerp(b, a[2], ring);
                if (kind >= 0)
                {
                    var strip = Cover(Math.Max(box + 0.1f, u + 0.62f), unit);
                    r = Lerp(r, a[0], strip); g = Lerp(g, a[1], strip); b = Lerp(b, a[2], strip);
                }
                var i = (y * size + x) * 4;
                px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = fill * 0.9f;
            }
            return px;
        }

        // ================= the symbols (signed distance, negative inside; the tile spans -1..1) =================

        private static float Symbol(int kind, float u, float v)
        {
            switch (kind)
            {
                case Talk: return Bubble(u, v);
                case Actions: return Gift(u, v);
                case Together: return People(u, v);
                case Romance: return Heart(u, v);
                default: return Flame(u, v);
            }
        }

        private static float Bubble(float u, float v)
        {
            var body = RoundBox(u, v - 0.1f, 0.5f, 0.34f, 0.2f);
            var tail = Triangle(u, v, -0.3f, -0.18f, -0.04f, -0.18f, -0.4f, -0.5f);
            var shape = Math.Min(body, tail);
            var dots = Math.Min(Circle(u + 0.24f, v - 0.1f, 0.07f), Math.Min(Circle(u, v - 0.1f, 0.07f), Circle(u - 0.24f, v - 0.1f, 0.07f)));
            return Math.Max(shape, -dots);
        }

        private static float Gift(float u, float v)
        {
            var body = RoundBox(u, v + 0.2f, 0.4f, 0.28f, 0.05f);
            var lid = RoundBox(u, v - 0.16f, 0.48f, 0.1f, 0.04f);
            var box = Math.Min(body, lid);
            var ribbon = Math.Abs(u) - 0.05f;               // a gap down the middle
            var gap = v + 0.03f;                              // between the lid and the box
            box = Math.Max(box, -ribbon);
            box = Math.Max(box, -(Math.Abs(gap) - 0.025f));
            var loopL = Math.Abs(Circle(u + 0.15f, v - 0.4f, 0.12f)) - 0.045f;
            var loopR = Math.Abs(Circle(u - 0.15f, v - 0.4f, 0.12f)) - 0.045f;
            return Math.Min(box, Math.Min(loopL, loopR));
        }

        private static float Person(float u, float v, float x, float s)
        {
            var head = Circle(u - x, v - 0.26f * s, 0.17f * s);
            var body = Math.Max(RoundBox(u - x, v + 0.3f * s, 0.27f * s, 0.32f * s, 0.24f * s), -(v + 0.5f));
            return Math.Min(head, body);
        }

        private static float People(float u, float v)
        {
            var back = Person(u, v - 0.06f, 0.2f, 0.92f);
            var front = Person(u, v + 0.02f, -0.16f, 1f);
            back = Math.Max(back, -(front - 0.07f));        // a cut so the two read as two
            return Math.Min(front, back);
        }

        /// <summary>The heart (Inigo Quilez's exact heart distance), scaled into the tile.</summary>
        private static float Heart(float u, float v)
        {
            const float k = 0.86f;
            var x = Math.Abs(u) / k;
            var y = (v + 0.5f) / k;
            float d;
            if (y + x > 1f) d = Len(x - 0.25f, y - 0.75f) - 0.35355339f;
            else
            {
                var m = 0.5f * Math.Max(x + y, 0f);
                d = (float)Math.Sqrt(Math.Min(Dot2(x, y - 1f), Dot2(x - m, y - m))) * Math.Sign(x - y);
            }
            return d * k;
        }

        private static float Flame(float u, float v)
        {
            // a round base, the main tongue leaning right and a small one on the left
            var main = SmoothMin(Circle(u, v + 0.24f, 0.36f), Triangle(u, v, -0.33f, -0.12f, 0.36f, -0.2f, 0.12f, 0.7f), 0.16f);
            var side = Triangle(u, v, -0.36f, -0.24f, -0.06f, -0.05f, -0.34f, 0.3f);
            var outer = SmoothMin(main, side, 0.1f);
            var inner = SmoothMin(Circle(u, v + 0.33f, 0.15f), Triangle(u, v, -0.14f, -0.3f, 0.15f, -0.3f, 0.05f, 0.12f), 0.08f);
            return Math.Max(outer, -inner);
        }

        // ================= shape formulas =================

        private static float Circle(float x, float y, float r) { return Len(x, y) - r; }

        private static float RoundBox(float x, float y, float hw, float hh, float r)
        {
            var qx = Math.Abs(x) - hw + r;
            var qy = Math.Abs(y) - hh + r;
            return Len(Math.Max(qx, 0f), Math.Max(qy, 0f)) + Math.Min(Math.Max(qx, qy), 0f) - r;
        }

        private static float Triangle(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
        {
            float e0x = bx - ax, e0y = by - ay, e1x = cx - bx, e1y = cy - by, e2x = ax - cx, e2y = ay - cy;
            float v0x = px - ax, v0y = py - ay, v1x = px - bx, v1y = py - by, v2x = px - cx, v2y = py - cy;
            var t0 = Clamp01((v0x * e0x + v0y * e0y) / (e0x * e0x + e0y * e0y));
            var t1 = Clamp01((v1x * e1x + v1y * e1y) / (e1x * e1x + e1y * e1y));
            var t2 = Clamp01((v2x * e2x + v2y * e2y) / (e2x * e2x + e2y * e2y));
            var d0 = Dot2(v0x - e0x * t0, v0y - e0y * t0);
            var d1 = Dot2(v1x - e1x * t1, v1y - e1y * t1);
            var d2 = Dot2(v2x - e2x * t2, v2y - e2y * t2);
            var s = Math.Sign(e0x * e2y - e0y * e2x);
            var c0 = s * (v0x * e0y - v0y * e0x);
            var c1 = s * (v1x * e1y - v1y * e1x);
            var c2 = s * (v2x * e2y - v2y * e2x);
            var d = Math.Min(d0, Math.Min(d1, d2));
            var inside = c0 > 0f && c1 > 0f && c2 > 0f ? -1f : 1f;
            return -(float)Math.Sqrt(d) * -inside;
        }

        private static float SmoothMin(float a, float b, float k)
        {
            var h = Clamp01(0.5f + 0.5f * (b - a) / k);
            return Lerp(b, a, h) - k * h * (1f - h);
        }

        private static float Len(float x, float y) { return (float)Math.Sqrt(x * x + y * y); }
        private static float Dot2(float x, float y) { return x * x + y * y; }
        private static float Clamp01(float x) { return x < 0f ? 0f : x > 1f ? 1f : x; }
        private static float Lerp(float a, float b, float t) { return a + (b - a) * t; }

        /// <summary>How much of a pixel the shape covers (smooth edge one pixel wide).</summary>
        private static float Cover(float d, float unit) { return Clamp01(0.5f - d * unit); }
    }
}
