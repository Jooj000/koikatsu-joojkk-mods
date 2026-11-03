using System;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// aumente um pouco), missões do quadro e radar, e o que mais está usando ícones placeholder"). A round badge like the
    /// game's own action icons (white face, a ring in the activity's stat color, grey when out of range) with one drawing
    /// per activity instead of one per stat: a book for studying, a ball for basketball, a wave for the pool, a paw for
    /// the cats... Same signed-distance shapes as the conversation's tiles, so tools\iconpreview can draw them too.
    /// </summary>
    internal static partial class IconArt
    {
        /// <summary>Every drawing, for tools\iconpreview.</summary>
        public static readonly string[] Glyphs = { "bubble", "gift", "heart", "pin", "note", "broom", "book", "lovebook", "magazine", "books", "dumbbell", "ball", "wave", "shoe", "leaf", "paw", "tree", "bowl", "pan", "people", "pen", "lens", "coin", "moon", "mask", "star", "cup", "cross", "cap", "box", "bag" };

        /// <summary>The drawing of each activity (ActivityDef.Id), for <see cref="Spot"/>.</summary>
        public static string GlyphOf(string activityId)
        {
            switch (activityId)
            {
                case "review_notes": return "note";
                case "class_duty": return "broom";
                case "study": return "book";
                case "romance_novel": return "lovebook";
                case "browse_magazines": return "magazine";
                case "library_job": return "books";
                case "workout": return "dumbbell";
                case "basketball": case "gym_job": return "ball";
                case "swim": case "lifeguard": return "wave";
                case "run": return "shoe";
                case "garden": return "leaf";
                case "feed_cats": return "paw";
                case "park_walk": return "tree";
                case "hearty_lunch": return "bowl";
                case "kitchen_job": return "pan";
                case "club_meeting": return "people";
                case "club_report": return "pen";
                case "research_material": return "lens";
                case "fundraise": return "coin";
                case "nap": return "moon";
                case "practice_lines": return "mask";
                case "stargaze": return "star";
                case "rest": return "cup";
                case "nurse_job": return "cross";
                case "tutoring": return "cap";
                case "teacher_errands": return "box";
                default: return "star";
            }
        }

        /// <summary>
        /// A map badge, size x size, RGBA rows from the bottom: white face, ring and drawing in <paramref name="tint"/>
        /// (grey when <paramref name="on"/> is false, as the game greys an icon you are not next to), a soft shadow under
        /// the drawing. <paramref name="scale"/> grows the drawing inside the badge (the store's bag is a little bigger).
        /// </summary>
        public static float[] Spot(string glyph, float[] tint, int size, bool on, float scale = 1f)
        {
            var px = new float[size * size * 4];
            var ring = on ? tint : new[] { 0.55f, 0.55f, 0.6f };
            var ink = on ? new[] { tint[0] * 0.82f, tint[1] * 0.82f, tint[2] * 0.82f } : new[] { 0.45f, 0.45f, 0.5f };
            var unit = size / 2f;
            var k = 0.74f * scale;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var disc = Circle(u, v, 0.95f);
                var fill = Cover(disc, unit);
                if (fill <= 0f) continue;
                // the face: white, a touch of the color from below
                var glow = Clamp01(0.4f - v * 0.45f) * (on ? 0.16f : 0.06f);
                var r = Lerp(1f, ring[0], glow);
                var g = Lerp(1f, ring[1], glow);
                var b = Lerp(1f, ring[2], glow);
                // the ring, with a thin dark line outside so it reads on a bright floor
                var band = Cover(Math.Abs(disc + 0.075f) - 0.075f, unit);
                r = Lerp(r, ring[0], band); g = Lerp(g, ring[1], band); b = Lerp(b, ring[2], band);
                var edge = Cover(Math.Abs(disc + 0.012f) - 0.014f, unit) * 0.7f;
                r = Lerp(r, 0.1f, edge); g = Lerp(g, 0.1f, edge); b = Lerp(b, 0.12f, edge);
                // the drawing, with its shadow a little below
                var shadow = Cover(Glyph(glyph, u / k, (v + 0.045f) / k) * k - 0.01f, unit) * 0.25f;
                r = Lerp(r, 0f, shadow); g = Lerp(g, 0f, shadow); b = Lerp(b, 0f, shadow);
                var s = Cover(Glyph(glyph, u / k, v / k) * k, unit);
                r = Lerp(r, ink[0], s); g = Lerp(g, ink[1], s); b = Lerp(b, ink[2], s);
                var i = (y * size + x) * 4;
                px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = fill;
            }
            return px;
        }

        /// <summary>
        /// A board request on its badge: a disc in the given color, the drawing of what she asks for (a speech bubble, a
        /// referente ao tipo de missão, variando as cores para caso haja mais de uma com o mesmo ícone").
        /// </summary>
        public static float[] RequestBadge(string glyph, float[] color, int size)
        {
            return RequestBadge(-1, glyph, color, size);
        }

        public static float[] RequestBadge(int shape, float[] color, int size) { return RequestBadge(shape, null, color, size); }

        private static float[] RequestBadge(int shape, string glyph, float[] color, int size)
        {
            var px = new float[size * size * 4];
            var unit = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var disc = Circle(u, v, 0.95f);
                var fill = Cover(disc, unit);
                if (fill <= 0f) continue;
                var lift = Clamp01(0.5f + v * 0.5f) * 0.25f;
                var r = Lerp(color[0], 1f, lift);
                var g = Lerp(color[1], 1f, lift);
                var b = Lerp(color[2], 1f, lift);
                var rim = Cover(Math.Abs(disc + 0.04f) - 0.05f, unit);
                r = Lerp(r, Graphite[0], rim); g = Lerp(g, Graphite[1], rim); b = Lerp(b, Graphite[2], rim);
                var d = glyph != null ? Glyph(glyph, u / 0.72f, v / 0.72f) * 0.72f : MarkShape(shape, u / 0.62f, v / 0.62f) * 0.62f;
                var line = Cover(d - 0.07f, unit);
                r = Lerp(r, Graphite[0], line); g = Lerp(g, Graphite[1], line); b = Lerp(b, Graphite[2], line);
                var w = Cover(d, unit);
                r = Lerp(r, 1f, w); g = Lerp(g, 0.98f, w); b = Lerp(b, 0.93f, w);
                var i = (y * size + x) * 4;
                px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = fill;
            }
            return px;
        }

        /// <summary>The radar's arrow for someone off its edge: a chevron, white with a dark line.</summary>
        public static float[] Chevron(int size)
        {
            var px = new float[size * size * 4];
            var unit = size / 2f;
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var u = (x + 0.5f) / unit - 1f;
                var v = (y + 0.5f) / unit - 1f;
                var d = Math.Max(Triangle(u, v, -0.7f, -0.6f, 0.7f, -0.6f, 0f, 0.8f), -Triangle(u, v, -0.5f, -0.75f, 0.5f, -0.75f, 0f, 0.05f));
                var all = Cover(d - 0.09f, unit);
                if (all <= 0f) continue;
                var w = Cover(d, unit);
                var i = (y * size + x) * 4;
                px[i] = Lerp(0.08f, 1f, w); px[i + 1] = Lerp(0.08f, 1f, w); px[i + 2] = Lerp(0.1f, 1f, w); px[i + 3] = all;
            }
            return px;
        }

        // ================= the drawings (signed distance, the badge's inside spans about -1..1) =================

        private static float Glyph(string g, float u, float v)
        {
            switch (g)
            {
                case "book": return Book(u, v);
                case "lovebook": return Math.Min(Math.Max(Book(u, v + 0.12f), -(Heart(u / 0.42f, (v - 0.42f) / 0.42f) * 0.42f - 0.06f)), Heart(u / 0.36f, (v - 0.42f) / 0.36f) * 0.36f);
                case "magazine": return Magazine(u, v);
                case "books": return Books(u, v);
                case "note": return Note(u, v);
                case "pen": return Pencil(u, v);
                case "broom": return Broom(u, v);
                case "dumbbell": return Dumbbell(u / 1.1f, v / 1.1f) * 1.1f;
                case "ball": return Ball(u, v);
                case "wave": return Wave(u, v);
                case "shoe": return Shoe(u, v);
                case "leaf": return Leaf(u, v);
                case "tree": return Tree(u, v);
                case "paw": return Paw(u, v);
                case "bowl": return Bowl(u, v);
                case "pan": return Pan(u, v);
                case "people": return People(u, v);
                case "lens": return Lens(u, v);
                case "coin": return Coin(u, v);
                case "moon": return Moon(u, v);
                case "mask": return Mask(u, v);
                case "star": return Star(u, v + 0.05f, 0.7f, 0.45f);
                case "cup": return Cup(u, v);
                case "cross": return Math.Min(RoundBox(u, v, 0.18f, 0.56f, 0.05f), RoundBox(u, v, 0.56f, 0.18f, 0.05f));
                case "cap": return Cap(u, v);
                case "box": return Box(u, v);
                case "bag": return Bag(u, v);
                case "bubble": return Bubble(u / 0.82f, v / 0.82f) * 0.82f;
                case "gift": return Gift(u / 0.82f, v / 0.82f) * 0.82f;
                case "heart": return Heart(u / 0.78f, (v + 0.04f) / 0.78f) * 0.78f;
                case "pin": return Pin(u, v);
                default: return ItemShape(g, u, v);
            }
        }

        private static float Magazine(float u, float v)
        {
            // a cover, a little tilted: the title band on top, a girl's head and shoulders on it, the corner folded
            var x = u * 0.985f + v * 0.17f;
            var y = -u * 0.17f + v * 0.985f;
            var page = RoundBox(x, y, 0.42f, 0.58f, 0.05f);
            var corner = Triangle(x, y, 0.42f, -0.3f, 0.42f, -0.58f, 0.14f, -0.58f);
            page = Math.Max(page, -corner);
            var fold = Triangle(x, y, 0.36f, -0.34f, 0.18f, -0.52f, 0.36f, -0.52f);
            var title = RoundBox(x, y - 0.4f, 0.3f, 0.08f, 0.03f);
            var head = Circle(x + 0.04f, y - 0.06f, 0.15f);
            var shoulders = Math.Max(Circle(x + 0.04f, y + 0.46f, 0.3f), y + 0.42f - 0.32f);
            var person = Math.Min(head, shoulders);
            return Math.Min(Math.Max(page, -Math.Min(title, person)), fold);
        }

        private static float Books(float u, float v)
        {
            var a = RoundBox(u + 0.34f, v + 0.04f, 0.13f, 0.5f, 0.04f);
            var b = RoundBox(u + 0.05f, v - 0.02f, 0.13f, 0.56f, 0.04f);
            // the third one leans on the others
            var cx = u - 0.36f; var cy = v + 0.04f;
            var rx = cx * 0.94f + cy * 0.34f; var ry = -cx * 0.34f + cy * 0.94f;
            var c = RoundBox(rx, ry, 0.13f, 0.48f, 0.04f);
            var bands = Math.Min(RoundBox(u, v - 0.3f, 0.7f, 0.025f, 0f), RoundBox(u, v + 0.3f, 0.7f, 0.025f, 0f));
            return Math.Max(Math.Min(Math.Min(a, b), c), -bands);
        }

        private static float Note(float u, float v)
        {
            var page = RoundBox(u, v, 0.42f, 0.56f, 0.06f);
            var lines = 1f;
            for (var i = 0; i < 4; i++) lines = Math.Min(lines, RoundBox(u + (i == 3 ? 0.1f : 0f), v - 0.3f + i * 0.2f, i == 3 ? 0.18f : 0.28f, 0.035f, 0.03f));
            return Math.Max(page, -lines);
        }

        private static float Pencil(float u, float v)
        {
            // along the diagonal
            var x = (u + v) * 0.7071f;
            var y = (v - u) * 0.7071f;
            var body = RoundBox(x - 0.08f, y, 0.42f, 0.13f, 0.03f);
            var tip = Triangle(x, y, -0.34f, 0.13f, -0.34f, -0.13f, -0.62f, 0f);
            var band = RoundBox(x - 0.36f, y, 0.025f, 0.2f, 0f);
            return Math.Max(Math.Min(body, tip), -band);
        }

        private static float Broom(float u, float v)
        {
            // the stick from the top right, the head a fan of bristles at the bottom left
            var stick = Segment(u, v, 0.5f, 0.58f, -0.02f, -0.02f, 0.06f);
            var x = (u + 0.2f) * 0.7071f + (v + 0.2f) * 0.7071f;   // along the stick
            var y = -(u + 0.2f) * 0.7071f + (v + 0.2f) * 0.7071f;  // across it
            var head = Math.Max(Math.Max(x - 0.16f, -x - 0.4f), Math.Abs(y) - (0.12f + (0.16f - x) * 0.45f));
            var cuts = Math.Max(Math.Min(Math.Abs(y - 0.1f), Math.Abs(y + 0.1f)) - 0.025f, x + 0.12f);
            var band = Math.Max(Math.Abs(x - 0.08f) - 0.03f, Math.Abs(y) - 0.2f);
            return Math.Min(stick, Math.Max(head, -Math.Min(cuts, band)));
        }

        private static float Ball(float u, float v)
        {
            var ball = Circle(u, v, 0.56f);
            var seams = Math.Min(Math.Abs(u) - 0.03f, Math.Abs(v) - 0.03f);
            seams = Math.Min(seams, Math.Abs(Circle(u - 0.78f, v, 0.62f)) - 0.03f);
            seams = Math.Min(seams, Math.Abs(Circle(u + 0.78f, v, 0.62f)) - 0.03f);
            return Math.Max(ball, -seams);
        }

        private static float Wave(float u, float v)
        {
            var d = 1f;
            for (var i = 0; i < 3; i++)
            {
                var y = v + 0.36f - i * 0.32f;
                var w = y - 0.09f * (float)Math.Sin(u * 7f + i);
                d = Math.Min(d, Math.Max(Math.Abs(w) - 0.065f, Math.Abs(u) - 0.6f));
            }
            return d;
        }

        private static float Shoe(float u, float v)
        {
            var sole = RoundBox(u, v + 0.3f, 0.62f, 0.1f, 0.08f);
            var body = SmoothMin(RoundBox(u + 0.2f, v + 0.04f, 0.36f, 0.26f, 0.2f), RoundBox(u - 0.2f, v + 0.12f, 0.36f, 0.18f, 0.16f), 0.1f);
            body = Math.Max(body, -(v + 0.2f));
            var laces = Math.Min(Segment(u, v, -0.02f, 0.14f, 0.14f, 0.28f, 0.03f), Segment(u, v, 0.12f, 0.06f, 0.28f, 0.2f, 0.03f));
            var line = Math.Abs(v + 0.2f) - 0.03f;
            return Math.Max(Math.Min(sole, body), -Math.Min(laces, Math.Max(line, Math.Abs(u) - 0.6f)));
        }

        private static float Leaf(float u, float v)
        {
            // long along the diagonal up and to the right, the stem out of its bottom left tip
            var x = (u + v) * 0.7071f + 0.06f;
            var y = (v - u) * 0.7071f;
            var leaf = Math.Max(Circle(x, y - 0.36f, 0.6f), Circle(x, y + 0.36f, 0.6f));
            var vein = Math.Max(Math.Abs(y) - 0.03f, Math.Abs(x) - 0.36f);
            var side = Math.Min(Segment(x, y, 0f, 0f, 0.2f, 0.16f, 0.025f), Segment(x, y, -0.16f, 0f, 0.02f, -0.16f, 0.025f));
            var stem = Segment(u, v, -0.28f, -0.28f, -0.5f, -0.5f, 0.05f);
            return Math.Min(Math.Max(leaf, -Math.Min(vein, side)), stem);
        }

        private static float Tree(float u, float v)
        {
            var crown = SmoothMin(Circle(u, v - 0.2f, 0.36f), Math.Min(Circle(u + 0.28f, v - 0.02f, 0.26f), Circle(u - 0.28f, v - 0.02f, 0.26f)), 0.1f);
            var trunk = RoundBox(u, v + 0.4f, 0.09f, 0.24f, 0.03f);
            var ground = RoundBox(u, v + 0.62f, 0.46f, 0.04f, 0.03f);
            return Math.Min(Math.Min(crown, trunk), ground);
        }

        private static float Lens(float u, float v)
        {
            // the glass up on the left, the handle down to the right, as a magnifier is drawn
            var ring = Math.Abs(Circle(u + 0.12f, v - 0.12f, 0.34f)) - 0.08f;
            var shine = Math.Max(Math.Abs(Circle(u + 0.12f, v - 0.12f, 0.2f)) - 0.035f, Math.Max(u + 0.12f, -(v - 0.12f)));
            var handle = Segment(u, v, 0.15f, -0.15f, 0.52f, -0.52f, 0.1f);
            return Math.Min(Math.Min(ring, handle), shine);
        }

        private static float Coin(float u, float v)
        {
            var coin = Circle(u, v, 0.56f);
            var rim = Math.Abs(Circle(u, v, 0.46f)) - 0.025f;
            var ax = Math.Abs(u);
            var yen = Math.Min(Math.Min(Segment(u, v, 0f, -0.3f, 0f, 0.02f, 0.05f), Math.Min(Segment(u, v, 0f, 0.02f, -0.2f, 0.28f, 0.05f), Segment(u, v, 0f, 0.02f, 0.2f, 0.28f, 0.05f))),
                               Math.Min(RoundBox(u, v + 0.02f, 0.18f, 0.025f, 0f), RoundBox(u, v + 0.13f, 0.18f, 0.025f, 0f)));
            return Math.Max(coin, -Math.Min(rim, yen + (ax > 0.3f ? 1f : 0f)));
        }

        private static float Mask(float u, float v)
        {
            // a theatre mask: a flat brow, wide cheeks, a pointed chin, slanted eyes and a big grin, on a stick
            var brow = RoundBox(u, v - 0.3f, 0.5f, 0.16f, 0.1f);
            var chin = Triangle(u, v, -0.5f, 0.2f, 0.5f, 0.2f, 0f, -0.56f) - 0.06f;
            var face = SmoothMin(brow, chin, 0.12f);
            var eyes = Math.Min(Segment(u, v, -0.36f, 0.24f, -0.12f, 0.16f, 0.07f), Segment(u, v, 0.36f, 0.24f, 0.12f, 0.16f, 0.07f));
            var grin = Math.Max(Math.Abs(Circle(u, v - 0.06f, 0.3f)) - 0.06f, v + 0.02f);
            var cut = Math.Min(eyes, grin);
            var stick = Segment(u, v, 0.4f, -0.12f, 0.6f, -0.62f, 0.05f);
            return Math.Min(Math.Max(face, -cut), stick);
        }

        private static float Cap(float u, float v)
        {
            // a graduation cap: the flat board seen at an angle, the cap under it apart by a thin gap, and the cord from the
            // button to the edge, where the tassel hangs
            var board = Math.Min(Triangle(u, v, -0.7f, 0.16f, 0f, 0.44f, 0.7f, 0.16f), Triangle(u, v, -0.7f, 0.16f, 0.7f, 0.16f, 0f, -0.12f));
            var crown = Math.Max(RoundBox(u, v + 0.28f, 0.36f, 0.2f, 0.06f), -(board - 0.07f));
            crown = Math.Max(crown, v - 0.02f);
            var cord = Segment(u, v, 0f, 0.16f, 0.5f, 0.1f, 0.028f);
            var button = Circle(u, v - 0.16f, 0.06f);
            board = Math.Max(board, -cord);
            var hang = Segment(u, v, 0.53f, 0.08f, 0.55f, -0.3f, 0.032f);
            var tassel = Triangle(u, v, 0.46f, -0.28f, 0.64f, -0.28f, 0.55f, -0.52f) - 0.02f;
            return Math.Min(Math.Min(Math.Min(board, button), crown), Math.Min(hang, tassel));
        }

        private static float Box(float u, float v)
        {
            var box = RoundBox(u, v + 0.08f, 0.5f, 0.42f, 0.05f);
            var tape = RoundBox(u, v - 0.04f, 0.09f, 0.5f, 0f);
            var lid = Math.Abs(v - 0.16f) - 0.03f;
            return Math.Max(box, -Math.Min(Math.Max(tape, -(v - 0.16f)), Math.Max(lid, Math.Abs(u) - 0.5f)));
        }

        private static float Pin(float u, float v)
        {
            // a map pin: a round head with a hole, pointing down
            var head = Circle(u, v - 0.16f, 0.38f);
            var tip = Triangle(u, v, -0.33f, -0.0f, 0.33f, -0.0f, 0f, -0.66f);
            return Math.Max(SmoothMin(head, tip, 0.06f), -Circle(u, v - 0.16f, 0.14f));
        }

        private static float Bag(float u, float v)
        {
            var body = Math.Max(RoundBox(u, v + 0.16f, 0.48f, 0.4f, 0.08f), u * 0f + Math.Abs(u) - 0.48f + (v + 0.56f) * 0.08f);
            var handle = Math.Max(Math.Abs(Circle(u, v - 0.24f, 0.24f)) - 0.06f, -(v - 0.24f));
            var holes = Math.Min(Circle(u + 0.24f, v - 0.16f, 0.05f), Circle(u - 0.24f, v - 0.16f, 0.05f));
            return Math.Max(Math.Min(body, handle), -holes);
        }

        /// <summary>The request shapes (Markers.ShapeNames order) as distances.</summary>
        private static float MarkShape(int shape, float u, float v)
        {
            var ax = Math.Abs(u);
            var ay = Math.Abs(v);
            switch (shape)
            {
                case 0: return Star(u, v + 0.06f, 0.95f, 0.45f);
                case 1: return (ax + ay - 0.9f) * 0.7071f;
                case 2: return Heart(u / 1.05f, (v - 0.02f) / 1.05f) * 1.05f;
                case 3: return Triangle(u, v, -0.85f, -0.66f, 0.85f, -0.66f, 0f, 0.86f);
                case 4: return Math.Max(Circle(u, v, 0.82f), -Circle(u - 0.36f, v - 0.22f, 0.66f));
                case 5: return Math.Min(RoundBox(u, v, 0.26f, 0.86f, 0.04f), RoundBox(u, v, 0.86f, 0.26f, 0.04f));
                case 6: return RoundBox(u, v, 0.72f, 0.72f, 0.1f);
                case 7: return Tear(u / 1.25f, v / 1.25f) * 1.25f;
                case 8: { var d = Math.Max(ax * 0.866f + ay * 0.5f, ay) - 0.78f; return d; }
                default: return Math.Abs(Circle(u, v, 0.64f)) - 0.2f;
            }
        }
    }
}
