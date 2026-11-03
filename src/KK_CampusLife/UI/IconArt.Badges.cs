using System;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// da barra de H na conversa... faça um ícone diferente para cada mood... nos likes, só faça um ícone e ao passar o
    /// mouse aparecem os gostos com seus ícones"): a disc in the badge's color with a thin dark rim and a dark symbol, so
    /// a 30 px icon still reads. Mood faces and signs, a star for the likes, and one symbol per like.
    /// </summary>
    internal static partial class IconArt
    {
        /// <summary>Badge symbols: the moods in <see cref="Mood"/> order (1-8), then the likes star, then the likes in
        /// <see cref="GirlInfo.LikeIds"/> order.</summary>
        public const int LikesStar = 9, FirstLike = 10;

        private static readonly float[][] BadgeColor =
        {
            new[] { 0.8f, 0.8f, 0.8f },     // none
            new[] { 1f, 0.84f, 0.3f },      // cheerful
            new[] { 1f, 0.6f, 0.25f },      // hungry
            new[] { 0.62f, 0.67f, 0.74f },  // bored
            new[] { 0.72f, 0.52f, 1f },     // stressed
            new[] { 0.45f, 0.66f, 1f },     // lonely
            new[] { 1f, 0.52f, 0.82f },     // flirty
            new[] { 0.5f, 0.5f, 0.9f },     // sleepy
            new[] { 1f, 0.4f, 0.36f },      // upset
            new[] { 1f, 0.86f, 0.45f },     // likes
        };

        // every badge has a gradient (light top, deeper bottom) and a gloss, instead of one flat mint disc for all.
        private static readonly float[][] LikeColors =
        {
            new[] { 0.96f, 0.64f, 0.34f },  // animals: warm orange
            new[] { 0.56f, 0.84f, 0.42f },  // food: green
            new[] { 1f, 0.48f, 0.36f },     // cooking: tomato
            new[] { 0.38f, 0.68f, 1f },     // exercise: blue
            new[] { 0.62f, 0.54f, 1f },     // study: violet
            new[] { 1f, 0.52f, 0.78f },     // fashion: pink
            new[] { 0.78f, 0.6f, 0.44f },   // black coffee: latte brown
            new[] { 1f, 0.32f, 0.3f },      // spicy: red
            new[] { 0.42f, 0.88f, 0.86f },  // sweets: mint candy
        };

        /// <summary>A round badge, size x size, RGBA rows from the bottom.</summary>
        public static float[] Badge(int symbol, int size)
        {
            var px = new float[size * size * 4];
            var c = symbol < BadgeColor.Length ? BadgeColor[symbol] : LikeColors[Math.Min(LikeColors.Length - 1, Math.Max(0, symbol - FirstLike))];
            var unit = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var disc = Circle(u, v, 0.94f);
                var fill = Cover(disc, unit);
                if (fill <= 0f) continue;
                // gradient: lighter at the top, deeper at the bottom; a soft gloss on the upper left
                var r = c[0]; var g = c[1]; var b = c[2];
                if (v > 0f) { var lift = v * 0.3f; r = Lerp(r, 1f, lift); g = Lerp(g, 1f, lift); b = Lerp(b, 1f, lift); }
                else { var deep = -v * 0.32f; r *= 1f - deep; g *= 1f - deep; b *= 1f - deep; }
                var gx = (u + 0.3f) / 0.42f; var gy = (v - 0.48f) / 0.24f;
                var gloss = Clamp01(1f - (gx * gx + gy * gy)) * 0.45f;
                r = Lerp(r, 1f, gloss); g = Lerp(g, 1f, gloss); b = Lerp(b, 1f, gloss);
                var rim = Cover(Math.Abs(disc + 0.04f) - 0.05f, unit);
                r = Lerp(r, Graphite[0], rim); g = Lerp(g, Graphite[1], rim); b = Lerp(b, Graphite[2], rim);
                var s = Cover(BadgeSymbol(symbol, u / 0.86f, v / 0.86f) * 0.86f, unit);
                r = Lerp(r, Graphite[0], s); g = Lerp(g, Graphite[1], s); b = Lerp(b, Graphite[2], s);
                var i = (y * size + x) * 4;
                px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = fill;
            }
            return px;
        }

        private static float BadgeSymbol(int symbol, float u, float v)
        {
            switch (symbol)
            {
                case 1: return Smile(u, v);
                case 2: return Bowl(u, v);
                case 3: return Flat(u, v);
                case 4: return Bolt(u, v);
                case 5: return Tear(u, v);
                case 6: return Heart(u / 0.92f, (v + 0.06f) / 0.92f) * 0.92f;
                case 7: return Moon(u, v);
                case 8: return Frown(u, v);
                case LikesStar: return Star(u, v + 0.04f, 0.62f, 0.45f);
                case FirstLike + 0: return Paw(u, v);
                case FirstLike + 1: return RiceBall(u, v);
                case FirstLike + 2: return Pan(u, v);
                case FirstLike + 3: return Dumbbell(u, v);
                case FirstLike + 4: return Book(u, v);
                case FirstLike + 5: return Dress(u, v);
                case FirstLike + 6: return Cup(u, v);
                case FirstLike + 7: return Flame(u / 0.85f, v / 0.85f) * 0.85f;
                case FirstLike + 8: return Candy(u, v);
                default: return 1f;
            }
        }

        // ---- moods ----

        private static float Eyes(float u, float v, float y, float r)
        {
            return Math.Min(Circle(u + 0.27f, v - y, r), Circle(u - 0.27f, v - y, r));
        }

        private static float Smile(float u, float v)
        {
            var mouth = Math.Max(Math.Abs(Circle(u, v - 0.08f, 0.44f)) - 0.075f, v + 0.12f);
            return Math.Min(Eyes(u, v, 0.2f, 0.1f), mouth);
        }

        private static float Flat(float u, float v)
        {
            var eyes = Math.Min(RoundBox(u + 0.27f, v - 0.16f, 0.15f, 0.05f, 0.05f), RoundBox(u - 0.27f, v - 0.16f, 0.15f, 0.05f, 0.05f));
            return Math.Min(eyes, RoundBox(u, v + 0.32f, 0.28f, 0.055f, 0.055f));
        }

        private static float Frown(float u, float v)
        {
            var brows = Math.Min(Segment(u, v, -0.48f, 0.42f, -0.14f, 0.24f, 0.06f), Segment(u, v, 0.48f, 0.42f, 0.14f, 0.24f, 0.06f));
            var mouth = Math.Max(Math.Abs(Circle(u, v + 0.7f, 0.42f)) - 0.075f, -(v + 0.46f));
            return Math.Min(Math.Min(Eyes(u, v, 0.04f, 0.09f), brows), mouth);
        }

        private static float Bowl(float u, float v)
        {
            var bowl = Math.Max(Circle(u, v + 0.08f, 0.56f), v + 0.08f);
            var rim = RoundBox(u, v + 0.06f, 0.62f, 0.06f, 0.05f);
            var sticks = Math.Min(Segment(u, v, -0.05f, 0.02f, 0.4f, 0.66f, 0.045f), Segment(u, v, 0.1f, 0.02f, 0.58f, 0.56f, 0.045f));
            return Math.Min(Math.Min(bowl, rim), sticks);
        }

        private static float Bolt(float u, float v)
        {
            var top = Triangle(u, v, 0.2f, 0.74f, -0.34f, -0.06f, 0.1f, -0.06f);
            var bottom = Triangle(u, v, -0.1f, 0.08f, 0.34f, 0.08f, -0.2f, -0.74f);
            return Math.Min(top, bottom);
        }

        private static float Tear(float u, float v)
        {
            return SmoothMin(Circle(u, v + 0.2f, 0.34f), Triangle(u, v, -0.29f, -0.08f, 0.29f, -0.08f, 0f, 0.66f), 0.06f);
        }

        private static float Moon(float u, float v)
        {
            var moon = Math.Max(Circle(u + 0.06f, v + 0.04f, 0.5f), -Circle(u - 0.2f, v - 0.16f, 0.42f));
            var z = Math.Min(Math.Min(Segment(u, v, 0.26f, 0.56f, 0.52f, 0.56f, 0.035f), Segment(u, v, 0.52f, 0.56f, 0.26f, 0.3f, 0.035f)), Segment(u, v, 0.26f, 0.3f, 0.52f, 0.3f, 0.035f));
            return Math.Min(moon, z);
        }

        // ---- likes ----

        private static float Paw(float u, float v)
        {
            var pad = SmoothMin(Circle(u, v + 0.24f, 0.28f), RoundBox(u, v + 0.34f, 0.34f, 0.16f, 0.14f), 0.08f);
            var toes = Math.Min(Math.Min(Circle(u + 0.42f, v - 0.08f, 0.13f), Circle(u - 0.42f, v - 0.08f, 0.13f)),
                                Math.Min(Circle(u + 0.16f, v - 0.36f, 0.14f), Circle(u - 0.16f, v - 0.36f, 0.14f)));
            return Math.Min(pad, toes);
        }

        private static float RiceBall(float u, float v)
        {
            var ball = Triangle(u, v, -0.42f, -0.36f, 0.42f, -0.36f, 0f, 0.4f) - 0.14f;
            var nori = RoundBox(u, v + 0.38f, 0.2f, 0.16f, 0.02f);
            return Math.Max(ball, -nori);
        }

        private static float Pan(float u, float v)
        {
            var pan = Circle(u + 0.14f, v + 0.04f, 0.38f);
            var handle = RoundBox(u - 0.48f, v + 0.04f, 0.22f, 0.07f, 0.06f);
            return Math.Min(pan, handle);
        }

        private static float Dumbbell(float u, float v)
        {
            var bar = RoundBox(u, v, 0.42f, 0.055f, 0.04f);
            var plates = Math.Min(RoundBox(Math.Abs(u) - 0.36f, v, 0.08f, 0.32f, 0.05f), RoundBox(Math.Abs(u) - 0.53f, v, 0.06f, 0.2f, 0.04f));
            return Math.Min(bar, plates);
        }

        private static float Book(float u, float v)
        {
            var left = RoundBox(u + 0.27f, v - 0.02f, 0.24f, 0.36f, 0.05f);
            var right = RoundBox(u - 0.27f, v - 0.02f, 0.24f, 0.36f, 0.05f);
            var spine = RoundBox(u, v + 0.36f, 0.5f, 0.05f, 0.04f);
            return Math.Min(Math.Min(left, right), spine);
        }

        private static float Dress(float u, float v)
        {
            var skirt = Triangle(u, v, -0.46f, -0.6f, 0.46f, -0.6f, 0f, 0.2f);
            var top = RoundBox(u, v - 0.3f, 0.18f, 0.2f, 0.04f);
            var straps = Math.Min(Segment(u, v, -0.14f, 0.5f, -0.1f, 0.66f, 0.04f), Segment(u, v, 0.14f, 0.5f, 0.1f, 0.66f, 0.04f));
            return Math.Min(Math.Min(skirt, top), straps);
        }

        private static float Cup(float u, float v)
        {
            // the handle on the right, joined to the cup; two wisps of steam rising from its middle
            var cup = RoundBox(u + 0.1f, v + 0.1f, 0.3f, 0.34f, 0.1f);
            var handle = Math.Abs(Circle(u - 0.28f, v + 0.08f, 0.15f)) - 0.06f;
            var steam = Math.Min(
                Math.Min(Segment(u, v, -0.22f, 0.36f, -0.16f, 0.5f, 0.04f), Segment(u, v, -0.16f, 0.5f, -0.22f, 0.66f, 0.04f)),
                Math.Min(Segment(u, v, -0.02f, 0.36f, 0.04f, 0.5f, 0.04f), Segment(u, v, 0.04f, 0.5f, -0.02f, 0.66f, 0.04f)));
            return Math.Min(Math.Min(cup, handle), steam);
        }

        private static float Candy(float u, float v)
        {
            var ball = Circle(u, v, 0.26f);
            var wraps = Math.Min(Triangle(u, v, -0.2f, 0f, -0.64f, 0.26f, -0.64f, -0.26f), Triangle(u, v, 0.2f, 0f, 0.64f, 0.26f, 0.64f, -0.26f));
            return Math.Min(ball, wraps);
        }

        // ---- shape formulas used only here ----

        private static float Segment(float px, float py, float ax, float ay, float bx, float by, float r)
        {
            float pax = px - ax, pay = py - ay, bax = bx - ax, bay = by - ay;
            var h = Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            return Len(pax - bax * h, pay - bay * h) - r;
        }

        /// <summary>A five-pointed star (Inigo Quilez's sdStar5): outer radius r, inner share rf.</summary>
        private static float Star(float px, float py, float r, float rf)
        {
            const float k1x = 0.809016994f, k1y = -0.587785252f;
            const float k2x = -k1x, k2y = k1y;
            px = Math.Abs(px);
            var d1 = Math.Max(k1x * px + k1y * py, 0f);
            px -= 2f * d1 * k1x; py -= 2f * d1 * k1y;
            var d2 = Math.Max(k2x * px + k2y * py, 0f);
            px -= 2f * d2 * k2x; py -= 2f * d2 * k2y;
            px = Math.Abs(px);
            py -= r;
            float bax = rf * -k1y, bay = rf * k1x - 1f;
            var h = Math.Max(0f, Math.Min(r, (px * bax + py * bay) / (bax * bax + bay * bay)));
            return Len(px - bax * h, py - bay * h) * Math.Sign(py * bax - px * bay);
        }
    }
}
