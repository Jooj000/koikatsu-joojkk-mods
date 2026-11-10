using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The conversation's own look (see <see cref="IconArt"/>): the five category tiles and the option cards as sprites,
    /// made once and kept, and the colors the buttons use when the mouse is over them.
    /// </summary>
    internal static class ConvArt
    {
        private const int TileSize = 128, CardSize = 64;
        private static readonly Dictionary<int, Sprite> Made = new Dictionary<int, Sprite>();

        /// <summary>The tile of category <paramref name="kind"/> (0 Talk .. 4 Spicy); <paramref name="on"/>: the open one.</summary>
        public static Sprite Tile(int kind, bool on)
        {
            var key = kind * 2 + (on ? 1 : 0);
            Sprite s;
            if (Made.TryGetValue(key, out s) && s != null) return s;
            s = ToSprite(IconArt.Tile(kind, TileSize, on), TileSize, 0, "cl_cat_tile_" + key);
            Made[key] = s;
            return s;
        }

        /// <summary>An option card framed in the color of category <paramref name="kind"/>; -1 a neutral one.</summary>
        public static Sprite Card(int kind)
        {
            var key = 100 + kind;
            Sprite s;
            if (Made.TryGetValue(key, out s) && s != null) return s;
            int border;
            var px = IconArt.Card(kind, CardSize, out border);
            s = ToSprite(px, CardSize, border, "cl_option_card_" + kind);
            Made[key] = s;
            return s;
        }

        /// <summary>The option card with its frame and strip in the request's pink (not a second border around
        /// it).</summary>
        public static Sprite FitCard()
        {
            Sprite s;
            if (Made.TryGetValue(200, out s) && s != null) return s;
            int border;
            var c = Board.FitColor;
            var px = IconArt.Card(new[] { c.r, c.g, c.b }, true, CardSize, out border);
            s = ToSprite(px, CardSize, border, "cl_option_card_fit");
            Made[200] = s;
            return s;
        }

        /// <summary>
        /// The inside of an option card in the card's own units (left, top, right, bottom): past the color strip and the
        /// frame plus a few pixels (the text "must stay only inside the colored border, with a margin of a few pixels"). A
        /// sliced border scales down when the card is shorter than its two borders, so the top and bottom follow the card's
        /// height.
        /// </summary>
        public static Vector4 CardInside(RectTransform card)
        {
            var canvas = card != null ? card.GetComponentInParent<Canvas>() : null;
            var k = canvas != null && canvas.referencePixelsPerUnit > 0f ? canvas.referencePixelsPerUnit / 100f : 1f;
            var b = CardSize * 3f / 8f * k;   // the slice border IconArt.Card gives (3/8 of the size)
            var vs = card != null ? Mathf.Min(1f, card.rect.height / (2f * b)) : 1f;
            var frame = IconArt.CardFrame * CardSize * k;
            return new Vector4(IconArt.CardStrip * CardSize * k + 6f, frame * vs + 3f, frame + 6f, frame * vs + 3f);
        }

        /// <summary>A button on our sprite: plain tint transition, brighter under the mouse, darker pressed, grey off.</summary>
        public static void Style(Button b, Sprite sprite, bool sliced)
        {
            if (b == null || b.image == null) return;
            b.image.sprite = sprite;
            b.image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            b.image.color = Color.white;
            b.transition = Selectable.Transition.ColorTint;
            var c = b.colors;
            c.normalColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            c.highlightedColor = Color.white;
            c.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            c.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.75f);
            c.colorMultiplier = 1f;
            c.fadeDuration = 0.08f;
            b.colors = c;
        }

        private static readonly Dictionary<Material, Material> WhiteFace = new Dictionary<Material, Material>();
        private static bool _faceLogged;

        /// <summary>
        /// The game's button text has a dark face (the letters' middle came out dark, only the outline light), so a vertex
        /// color can only darken it. A copy of its material with a white face lets the color show; the outline stays. One
        /// copy per game material, shared by every option; its outline is black.
        /// </summary>
        public static void ColorFace(TMPro.TextMeshProUGUI txt, Color color)
        {
            if (txt == null) return;
            var src = txt.fontSharedMaterial;
            if (src != null && src.HasProperty("_FaceColor"))
            {
                if (!_faceLogged)
                {
                    _faceLogged = true;
                    CampusLifePlugin.Log.LogInfo("Option text: material " + src.name + ", face " + src.GetColor("_FaceColor") + (src.HasProperty("_OutlineColor") ? ", outline " + src.GetColor("_OutlineColor") : "") + ", vertex gradient " + txt.enableVertexGradient + ", color " + txt.color);
                }
                Material white;
                if (!WhiteFace.TryGetValue(src, out white) || white == null)
                {
                    if (WhiteFace.ContainsValue(src)) white = src;   // already ours
                    else
                    {
                        white = new Material(src) { name = src.name + " (Campus Life face)" };
                        white.SetColor("_FaceColor", Color.white);
                        // Black outline (his ask): the game's light grey one blurred the colored letters.
                        if (white.HasProperty("_OutlineColor")) white.SetColor("_OutlineColor", Color.black);
                        white.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    }
                    WhiteFace[src] = white;
                }
                txt.fontSharedMaterial = white;
            }
            txt.enableVertexGradient = false;
            txt.color = color;
        }

        private static readonly Dictionary<int, Sprite> Badges = new Dictionary<int, Sprite>();

        /// <summary>A round badge of <see cref="IconArt.Badge"/> (mood 1-8, the likes star, a like).</summary>
        public static Sprite Badge(int symbol)
        {
            Sprite s;
            if (Badges.TryGetValue(symbol, out s) && s != null) return s;
            const int size = 96; // drawn at the biggest size it is shown (the tray's 36 px at 2x), never stretched
            s = ToSprite(IconArt.Badge(symbol, size), size, 0, "cl_badge_" + symbol);
            Badges[symbol] = s;
            return s;
        }

        private static Sprite ToSprite(float[] px, int size, int border, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var cols = new Color[size * size];
            for (var i = 0; i < cols.Length; i++) cols[i] = new Color(px[i * 4], px[i * 4 + 1], px[i * 4 + 2], px[i * 4 + 3]);
            tex.SetPixels(cols);
            tex.Apply(false, true);
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
            s.name = name;
            s.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return s;
        }
    }
}
