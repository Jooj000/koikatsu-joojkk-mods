using System;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// One drawing per store item, in the same signed-distance style as the activity and request drawings, so the store
    /// and the bag show what each thing is: a chocolate bar, a cookie, a can, a bouquet... Each item also has its own
    /// color.
    /// </summary>
    internal static partial class IconArt
    {
        /// <summary>The drawings only items use (the preview sheet lists them).</summary>
        public static readonly string[] ItemGlyphs = { "choco", "cookie", "mochi", "can", "bean", "flame", "bento", "cookbook", "apron", "bottle", "band", "note", "pen", "bow", "magazine", "cat", "photobook", "flowers", "perfume", "lace", "heart", "energycan", "books", "shaker", "cologne", "charm", "fish" };

        /// <summary>The drawing of a store item (ItemDef.Id).</summary>
        public static string ItemGlyph(string itemId)
        {
            switch (itemId)
            {
                case "chocolate": return "choco";
                case "cookies": return "cookie";
                case "daifuku": return "mochi";
                case "canned_coffee": return "can";
                case "coffee_beans": return "bean";
                case "spicy_chips": return "flame";
                case "bento": return "bento";
                case "cookbook": return "cookbook";
                case "apron": return "apron";
                case "sports_drink": return "bottle";
                case "wristband": return "band";
                case "notebook": return "note";
                case "pen": return "pen";
                case "hair_clip": return "bow";
                case "magazine": return "magazine";
                case "plush_cat": return "cat";
                case "photo_book": return "photobook";
                case "flowers": return "flowers";
                case "perfume": return "perfume";
                case "lingerie": return "lace";
                case "toy": return "heart";
                case "energy_drink": return "energycan";
                case "study_guide": return "books";
                case "protein": return "shaker";
                case "cologne": return "cologne";
                case "omamori": return "charm";
                case "cat_food": return "fish";
                default: return "box";
            }
        }

        /// <summary>Each item's own color, RGB.</summary>
        public static float[] ItemColor(string itemId)
        {
            switch (itemId)
            {
                case "chocolate": return new[] { 0.72f, 0.48f, 0.34f };
                case "cookies": return new[] { 0.95f, 0.72f, 0.38f };
                case "daifuku": return new[] { 1f, 0.6f, 0.7f };
                case "canned_coffee": return new[] { 0.55f, 0.42f, 0.34f };
                case "coffee_beans": return new[] { 0.66f, 0.46f, 0.3f };
                case "spicy_chips": return new[] { 1f, 0.36f, 0.28f };
                case "bento": return new[] { 0.95f, 0.45f, 0.4f };
                case "cookbook": return new[] { 1f, 0.62f, 0.3f };
                case "apron": return new[] { 1f, 0.56f, 0.72f };
                case "sports_drink": return new[] { 0.36f, 0.72f, 1f };
                case "wristband": return new[] { 0.32f, 0.58f, 0.96f };
                case "notebook": return new[] { 0.5f, 0.72f, 0.98f };
                case "pen": return new[] { 0.56f, 0.5f, 0.92f };
                case "hair_clip": return new[] { 1f, 0.5f, 0.66f };
                case "magazine": return new[] { 0.96f, 0.46f, 0.82f };
                case "plush_cat": return new[] { 1f, 0.7f, 0.4f };
                case "photo_book": return new[] { 0.9f, 0.62f, 0.42f };
                case "flowers": return new[] { 1f, 0.5f, 0.6f };
                case "perfume": return new[] { 0.82f, 0.52f, 1f };
                case "lingerie": return new[] { 0.92f, 0.36f, 0.52f };
                case "toy": return new[] { 0.78f, 0.36f, 0.86f };
                case "energy_drink": return new[] { 0.5f, 0.86f, 0.36f };
                case "study_guide": return new[] { 0.42f, 0.62f, 0.9f };
                case "protein": return new[] { 0.4f, 0.8f, 0.78f };
                case "cologne": return new[] { 0.36f, 0.66f, 0.82f };
                case "omamori": return new[] { 0.98f, 0.36f, 0.34f };
                case "cat_food": return new[] { 0.44f, 0.76f, 0.92f };
                default: return new[] { 0.6f, 0.7f, 0.85f };
            }
        }

        /// <summary>b drawn over a, with a thin gap around it so it reads as a separate piece.</summary>
        private static float Inset(float a, float b, float gap) { return Math.Min(Math.Max(a, -(b - gap)), b); }

        /// <summary>The item drawings (v up, about -0.7..0.7); null-like circle for an unknown name.</summary>
        private static float ItemShape(string g, float u, float v)
        {
            switch (g)
            {
                case "choco": return Choco(u, v);
                case "cookie": return Cookie(u, v);
                case "mochi": return Mochi(u, v);
                case "can": return CanShape(u, v, false);
                case "energycan": return CanShape(u, v, true);
                case "bean": return Bean(u, v);
                case "flame": return Flame(u, v);
                case "bento": return Bento(u, v);
                case "cookbook": return Cookbook(u, v);
                case "apron": return Apron(u, v);
                case "bottle": return Bottle(u, v);
                case "band": return Wristband(u, v);
                case "bow": return Bow(u, v);
                case "cat": return CatHead(u, v);
                case "photobook": return Math.Max(BookCover(u, v), -(Paw(u / 0.5f, (v + 0.02f) / 0.5f) * 0.5f));
                case "flowers": return Flowers(u, v);
                case "perfume": return Perfume(u, v);
                case "cologne": return Cologne(u, v);
                case "lace": return Lace(u, v);
                case "shaker": return Shaker(u, v);
                case "charm": return Charm(u, v);
                case "fish": return Fish(u, v);
                default: return Circle(u, v, 0.4f);
            }
        }

        private static float Choco(float u, float v)
        {
            // a bar with its squares, the wrapper pulled down over the bottom
            var bar = RoundBox(u, v - 0.12f, 0.36f, 0.5f, 0.06f);
            var grooves = Math.Min(Math.Abs(u) - 0.025f, Math.Min(Math.Abs(v - 0.34f) - 0.025f, Math.Abs(v - 0.06f) - 0.025f));
            var choc = Math.Max(bar, -grooves);
            var wrap = RoundBox(u, v + 0.44f, 0.42f, 0.24f, 0.04f);
            return Inset(choc, wrap, 0.06f);
        }

        private static float Cookie(float u, float v)
        {
            var c = Math.Max(Circle(u, v, 0.6f), -Circle(u - 0.56f, v - 0.46f, 0.24f));
            var chips = Math.Min(Math.Min(Circle(u + 0.24f, v - 0.22f, 0.09f), Circle(u - 0.12f, v - 0.14f, 0.08f)),
                        Math.Min(Math.Min(Circle(u - 0.02f, v + 0.12f, 0.09f), Circle(u + 0.28f, v + 0.24f, 0.08f)), Circle(u - 0.3f, v + 0.26f, 0.09f)));
            return Math.Max(c, -chips);
        }

        private static float Mochi(float u, float v)
        {
            // ichigo daifuku: a soft dome of mochi with the strawberry's tip coming out of the top (seeds cut out)
            var body = SmoothMin(Circle(u, v + 0.16f, 0.48f), RoundBox(u, v + 0.42f, 0.6f, 0.16f, 0.14f), 0.2f);
            var berry = SmoothMin(Circle(u, v - 0.2f, 0.19f), Triangle(u, v, -0.19f, 0.22f, 0.19f, 0.22f, 0f, 0.6f), 0.08f);
            var seeds = Math.Min(Circle(u + 0.07f, v - 0.3f, 0.032f), Math.Min(Circle(u - 0.07f, v - 0.3f, 0.032f), Circle(u, v - 0.44f, 0.032f)));
            return Inset(body, Math.Max(berry, -seeds), 0.06f);
        }

        private static float CanShape(float u, float v, bool bolt)
        {
            var body = RoundBox(u, v + 0.04f, 0.32f, 0.56f, 0.1f);
            var lip = RoundBox(u, v - 0.6f, 0.24f, 0.05f, 0.03f);
            var can = Inset(body, lip, 0.05f);
            if (bolt) return Math.Max(can, -(Bolt(u / 0.62f, (v + 0.04f) / 0.62f) * 0.62f));
            var window = RoundBox(u, v + 0.06f, 0.22f, 0.22f, 0.05f);
            return Math.Min(Math.Max(can, -window), Bean(u / 0.3f, (v + 0.06f) / 0.3f) * 0.3f);
        }

        private static float Bean(float u, float v)
        {
            const float c = 0.82f, s = 0.57f;
            var x = u * c + v * s;
            var y = -u * s + v * c;
            var ell = (Len(x / 0.4f, y / 0.6f) - 1f) * 0.4f;
            var slit = Math.Max(Math.Abs(x - 0.08f * (float)Math.Sin(y * 5f)) - 0.045f, Math.Abs(y) - 0.44f);
            return Math.Max(ell, -slit);
        }

        private static float Bento(float u, float v)
        {
            var box = RoundBox(u, v, 0.66f, 0.46f, 0.1f);
            var rooms = Math.Min(RoundBox(u + 0.24f, v, 0.3f, 0.34f, 0.05f),
                        Math.Min(RoundBox(u - 0.36f, v - 0.15f, 0.2f, 0.17f, 0.04f), RoundBox(u - 0.36f, v + 0.2f, 0.2f, 0.12f, 0.04f)));
            return Math.Min(Math.Max(box, -rooms), Circle(u + 0.24f, v, 0.12f));
        }

        /// <summary>A closed book's cover with its spine on the left.</summary>
        private static float BookCover(float u, float v)
        {
            var cover = RoundBox(u - 0.04f, v, 0.46f, 0.6f, 0.06f);
            return Math.Max(cover, -RoundBox(u + 0.3f, v, 0.025f, 0.62f, 0f));
        }

        private static float Cookbook(float u, float v)
        {
            var fork = Math.Min(Segment(u, v, 0f, -0.42f, 0f, 0.1f, 0.04f),
                       Math.Min(Segment(u, v, -0.09f, 0.1f, -0.09f, 0.36f, 0.03f), Math.Min(Segment(u, v, 0f, 0.1f, 0f, 0.36f, 0.03f), Segment(u, v, 0.09f, 0.1f, 0.09f, 0.36f, 0.03f))));
            var bridge = RoundBox(u, v - 0.1f, 0.12f, 0.04f, 0.03f);
            var spoon = Math.Min(Segment(u, v, 0.26f, -0.42f, 0.26f, 0.06f, 0.04f), (Len((u - 0.26f) / 0.1f, (v - 0.24f) / 0.15f) - 1f) * 0.1f);
            return Math.Max(BookCover(u + 0.06f, v), -(Math.Min(Math.Min(fork, bridge), spoon) - 0.02f));
        }

        private static float Apron(float u, float v)
        {
            var bib = RoundBox(u, v - 0.3f, 0.22f, 0.18f, 0.04f);
            var skirt = RoundBox(u, v + 0.2f, 0.42f, 0.36f, 0.14f);
            var body = SmoothMin(bib, skirt, 0.08f);
            var neck = Math.Max(Math.Abs(Circle(u, v - 0.5f, 0.2f)) - 0.035f, -(v - 0.5f));
            var ties = Math.Min(Segment(u, v, -0.4f, 0.02f, -0.64f, -0.08f, 0.035f), Segment(u, v, 0.4f, 0.02f, 0.64f, -0.08f, 0.035f));
            var pocket = Math.Abs(RoundBox(u, v + 0.22f, 0.18f, 0.11f, 0.03f)) - 0.025f;
            return Math.Min(Math.Max(body, -pocket), Math.Min(neck, ties));
        }

        private static float Bottle(float u, float v)
        {
            var body = RoundBox(u, v + 0.14f, 0.26f, 0.48f, 0.12f);
            var neck = RoundBox(u, v - 0.42f, 0.12f, 0.1f, 0.03f);
            var cap = RoundBox(u, v - 0.6f, 0.15f, 0.07f, 0.03f);
            var b = Inset(SmoothMin(body, neck, 0.08f), cap, 0.05f);
            var label = Math.Abs(Math.Abs(v + 0.12f) - 0.15f) - 0.025f;
            var drop = Tear(u / 0.32f, (v + 0.12f) / 0.32f) * 0.32f;
            return Math.Min(Math.Max(b, -Math.Min(label, drop - 0.03f)), Math.Max(drop, -0.5f));
        }

        private static float Wristband(float u, float v)
        {
            var e = (Len(u / 0.62f, v / 0.4f) - 1f) * 0.4f;
            var band = Math.Abs(e) - 0.13f;
            var stripe = Math.Abs(e) - 0.025f;
            return Math.Max(band, -stripe);
        }

        private static float Bow(float u, float v)
        {
            var x = Math.Abs(u);
            var wing = Triangle(x, v, 0.64f, 0.36f, 0.64f, -0.36f, 0.06f, 0f) - 0.08f;
            var tail = Triangle(x, v, 0.04f, -0.06f, 0.3f, -0.6f, 0.14f, -0.64f) - 0.03f;
            var knot = RoundBox(u, v, 0.13f, 0.17f, 0.06f);
            return Inset(Math.Min(wing, tail), knot, 0.05f);
        }

        private static float CatHead(float u, float v)
        {
            var x = Math.Abs(u);
            var head = SmoothMin(Circle(u, v + 0.08f, 0.5f), Triangle(x, v, 0.5f, 0.1f, 0.14f, 0.36f, 0.46f, 0.68f) - 0.04f, 0.06f);
            var eyes = Circle(x - 0.19f, v + 0.02f, 0.075f);
            var nose = Triangle(u, v, -0.07f, -0.12f, 0.07f, -0.12f, 0f, -0.2f) - 0.01f;
            var whiskers = Math.Min(Segment(x, v, 0.24f, -0.2f, 0.6f, -0.16f, 0.022f), Segment(x, v, 0.24f, -0.27f, 0.58f, -0.32f, 0.022f));
            return Math.Max(head, -Math.Min(Math.Min(eyes, nose), whiskers));
        }

        private static float Flower(float u, float v, float cx, float cy, float r)
        {
            var dx = u - cx; var dy = v - cy;
            var a = (float)Math.Atan2(dy, dx);
            var petals = Len(dx, dy) - r * (0.8f + 0.2f * (float)Math.Cos(5f * a));
            return Math.Max(petals, -Circle(dx, dy, r * 0.28f));
        }

        private static float Flowers(float u, float v)
        {
            var heads = Math.Min(Math.Min(Flower(u, v, -0.26f, 0.28f, 0.22f), Flower(u, v, 0.26f, 0.28f, 0.22f)), Flower(u, v, 0f, 0.5f, 0.22f));
            var wrap = Triangle(u, v, -0.42f, 0.14f, 0.42f, 0.14f, 0f, -0.7f) - 0.03f;
            var bandCut = Math.Abs(v + 0.18f) - 0.03f;
            return Inset(heads, Math.Max(wrap, -bandCut), 0.05f);
        }

        private static float Perfume(float u, float v)
        {
            var body = RoundBox(u + 0.06f, v + 0.2f, 0.36f, 0.34f, 0.16f);
            var neck = RoundBox(u + 0.06f, v - 0.2f, 0.09f, 0.08f, 0.02f);
            var cap = RoundBox(u + 0.06f, v - 0.38f, 0.17f, 0.11f, 0.05f);
            var bulb = Math.Min(Circle(u - 0.48f, v - 0.36f, 0.13f), Segment(u, v, 0.22f, 0.4f, 0.4f, 0.38f, 0.03f));
            var shine = Segment(u, v, -0.16f, 0.0f, -0.08f, 0.1f, 0.035f);
            return Math.Min(Math.Max(Inset(SmoothMin(body, neck, 0.05f), cap, 0.05f), -shine), bulb);
        }

        private static float Cologne(float u, float v)
        {
            var body = RoundBox(u, v + 0.16f, 0.32f, 0.42f, 0.06f);
            var cap = RoundBox(u, v - 0.48f, 0.2f, 0.14f, 0.03f);
            var label = Math.Abs(RoundBox(u, v + 0.16f, 0.18f, 0.16f, 0.02f)) - 0.025f;
            return Inset(Math.Max(body, -label), cap, 0.05f);
        }

        private static float Lace(float u, float v)
        {
            // a bra: two cups rising to the straps, the band under them, a little bow in the middle
            var x = Math.Abs(u);
            var cup = SmoothMin(Triangle(x, v, 0.06f, -0.2f, 0.62f, -0.2f, 0.44f, 0.3f), Circle(x - 0.34f, v + 0.04f, 0.26f), 0.1f);
            cup = Math.Max(cup, -(v + 0.2f) - 0.5f);
            var band = RoundBox(u, v + 0.3f, 0.66f, 0.07f, 0.03f);
            var strap = Segment(x, v, 0.44f, 0.28f, 0.42f, 0.62f, 0.035f);
            var trim = Math.Abs(Circle(x - 0.34f, v + 0.04f, 0.17f)) - 0.02f;
            var bow = Circle(u, v + 0.12f, 0.07f);
            return Math.Min(Math.Min(Math.Max(Math.Min(cup, band), -trim), strap), Inset(Math.Max(Math.Min(cup, band), -trim), bow, 0.04f));
        }

        private static float Shaker(float u, float v)
        {
            var body = RoundBox(u, v + 0.2f, 0.32f, 0.44f, 0.08f);
            var lid = RoundBox(u, v - 0.38f, 0.34f, 0.1f, 0.05f);
            var spout = RoundBox(u - 0.14f, v - 0.58f, 0.08f, 0.08f, 0.03f);
            var marks = Math.Min(Segment(u, v, -0.22f, 0.02f, -0.06f, 0.02f, 0.025f), Math.Min(Segment(u, v, -0.22f, -0.18f, -0.1f, -0.18f, 0.025f), Segment(u, v, -0.22f, -0.38f, -0.06f, -0.38f, 0.025f)));
            return Inset(Math.Max(body, -marks), Math.Min(lid, spout), 0.05f);
        }

        private static float Charm(float u, float v)
        {
            var pouch = SmoothMin(RoundBox(u, v + 0.14f, 0.32f, 0.46f, 0.1f), Triangle(u, v, -0.32f, 0.3f, 0.32f, 0.3f, 0f, 0.5f) - 0.04f, 0.06f);
            var knot = Math.Min(Circle(u, v - 0.54f, 0.08f), Math.Abs(Circle(u, v - 0.68f, 0.1f)) - 0.03f);
            var sign = Math.Min(RoundBox(u, v + 0.1f, 0.03f, 0.24f, 0f), Math.Min(RoundBox(u, v - 0.02f, 0.14f, 0.025f, 0f), RoundBox(u, v + 0.22f, 0.12f, 0.025f, 0f)));
            return Inset(Math.Max(pouch, -sign), knot, 0.04f);
        }

        private static float Fish(float u, float v)
        {
            var body = (Len((u + 0.1f) / 0.5f, v / 0.3f) - 1f) * 0.3f;
            var tail = Triangle(u, v, 0.3f, 0f, 0.66f, 0.3f, 0.66f, -0.3f) - 0.03f;
            var eye = Circle(u + 0.38f, v - 0.06f, 0.06f);
            var gill = Math.Max(Math.Abs(Circle(u + 0.5f, v, 0.3f)) - 0.025f, Math.Abs(v) - 0.2f);
            return Math.Max(SmoothMin(body, tail, 0.06f), -Math.Min(eye, Math.Max(gill, -(u + 0.25f))));
        }
    }
}
