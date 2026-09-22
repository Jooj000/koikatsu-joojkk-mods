using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Builds uGUI pieces out of the game's own sprites and font (the ones the clipboard / Character Status screen
    /// uses), so Campus Life menus look like the rest of the game instead of a debug window.
    /// </summary>
    internal static class GameUi
    {
        // Sprite names from the Character Status scene and the talk menu.
        public const string SprButton = "sp_00_00_07";   // header buttons (Name / Affection / Switch Info)
        public const string SprRedBar = "btn_04";        // red label bar (Location, Pers., ...)
        public const string SprBlueBar = "btn_05";       // blue label bar (Club Members, Lover, ...)
        public const string SprFrame = "rect_01_01";     // card frame
        public const string SprBackground = "sp_31_00_00";
        public const string SprTalkButton = "sp_05_00_00";
        public const string FontName = "JKG-M_3";

        public static readonly Color White = Color.white;
        public static readonly Color Dim = new Color(0.78f, 0.82f, 0.9f, 1f);
        public static readonly Color Gold = new Color(1f, 0.86f, 0.45f, 1f);
        public static readonly Color Green = new Color(0.6f, 1f, 0.65f, 1f);
        public static readonly Color Warn = new Color(1f, 0.62f, 0.5f, 1f);
        public static readonly Color PanelBack = new Color(0f, 0f, 0f, 0.86f);
        /// <summary>Behind notifications and hints: dark graphite, unlike any text color on it (white, gold, STR / INT /
        /// SEN, warning orange); the game's blue bar hid the INT blue.</summary>
        public static readonly Color NoteBack = new Color(0.07f, 0.07f, 0.09f, 0.9f);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static TMP_FontAsset _font;
        private static Material _fontMaterial;

        /// <summary>Remembers sprites and the font from a loaded game screen (keeps them alive for later menus).</summary>
        public static void Capture(GameObject root)
        {
            if (root == null) return;
            foreach (var img in root.GetComponentsInChildren<Image>(true))
                if (img.sprite != null && !Sprites.ContainsKey(img.sprite.name)) Sprites[img.sprite.name] = img.sprite;
            if (_font == null)
                foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                    if (t.font != null && t.font.name == FontName) { _font = t.font; _fontMaterial = t.fontSharedMaterial; break; }
        }

        public static Sprite Sprite(string name)
        {
            Sprite s;
            if (Sprites.TryGetValue(name, out s) && s != null) return s;
            s = Resources.FindObjectsOfTypeAll<Sprite>().FirstOrDefault(x => x.name == name);
            if (s != null) Sprites[name] = s;
            return s;
        }

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font == null) _font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f => f.name == FontName);
                return _font;
            }
        }

        public static bool Ready { get { return Font != null && Sprite(SprFrame) != null; } }

        // ---------------- building blocks ----------------

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Anchors to the parent's top-left corner, pivot top-left.</summary>
        public static RectTransform Place(this RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static Image Image(Transform parent, string name, string sprite, Color? color = null)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            var s = sprite == null ? null : Sprite(sprite);
            img.sprite = s;
            if (s != null && s.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = color ?? (s == null && sprite != null ? new Color(0.15f, 0.35f, 0.7f, 0.95f) : Color.white);
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string text, float size = 20f, Color? color = null,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool wrap = true)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            if (_fontMaterial != null) t.fontSharedMaterial = _fontMaterial;
            t.fontSize = size;
            t.color = color ?? White;
            t.alignment = align;
            t.enableWordWrapping = wrap;
            t.richText = true;
            t.raycastTarget = false;
            t.text = text ?? "";
            return t;
        }

        /// <summary>A header-style button (the blue "Name / Affection / Switch Info" buttons).</summary>
        public static Button Button(Transform parent, string name, string label, UnityAction onClick, string sprite = SprButton, float fontSize = 22f)
        {
            var img = Image(parent, name, sprite);
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.highlightedColor = new Color(1f, 0.92f, 0.75f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.8f);
            b.colors = colors;
            var t = Text(img.transform, "text", label, fontSize, White, TextAlignmentOptions.Center, false);
            t.rectTransform.Stretch(4, 0, 4, 2);
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = fontSize;
            if (onClick != null) b.onClick.AddListener(() =>
            {
                try { Sfx.Select(); onClick(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });
            return b;
        }

        public static void SetLabel(this Button b, string label)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null && t.text != label) t.text = label;
        }

        /// <summary>Red label bar with white text, like "Location" on the status cards.</summary>
        public static RectTransform Bar(Transform parent, string name, string label, bool blue = false, float fontSize = 20f)
        {
            var img = Image(parent, name, blue ? SprBlueBar : SprRedBar);
            var t = Text(img.transform, "text", label, fontSize, White, TextAlignmentOptions.Center, false);
            t.rectTransform.Stretch(2, 0, 2, 2);
            t.enableAutoSizing = true;
            t.fontSizeMin = 11f;
            t.fontSizeMax = fontSize;
            return img.rectTransform;
        }

        /// <summary>Vertical scroll list; returns the content transform (children are laid out top to bottom).</summary>
        public static RectTransform ScrollList(Transform parent, string name, out ScrollRect scroll, float spacing = 8f)
        {
            var root = Image(parent, name, null, new Color(0, 0, 0, 0));
            root.raycastTarget = true;
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            // Almost clear, not white: if the mask graphic ever shows (the week report came out as a white sheet with
            // pixels under 0.001 alpha, and then nothing inside would draw.
            var viewport = Image(root.transform, "Viewport", null, new Color(1f, 1f, 1f, 0.01f));
            viewport.rectTransform.Stretch(0, 0, 14, 0);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            viewport.raycastTarget = true;
            var content = Rect(viewport.transform, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(4, 4, 4, 8);
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childControlWidth = true;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;

            // Thin scrollbar on the right, like the status screen's.
            var barImg = Image(root.transform, "Scrollbar", null, new Color(1f, 1f, 1f, 0.12f));
            barImg.raycastTarget = true;
            var barRt = barImg.rectTransform;
            barRt.anchorMin = new Vector2(1f, 0f);
            barRt.anchorMax = new Vector2(1f, 1f);
            barRt.pivot = new Vector2(1f, 0.5f);
            barRt.sizeDelta = new Vector2(10f, 0f);
            barRt.anchoredPosition = Vector2.zero;
            var handle = Image(barRt, "Handle", null, new Color(1f, 1f, 1f, 0.55f));
            handle.raycastTarget = true;
            handle.rectTransform.Stretch();
            var sb = barImg.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle.rectTransform;
            sb.targetGraphic = handle;
            sb.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = sb;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        /// <summary>A block inside a scroll list: a frame with a vertical layout for rows.</summary>
        public static RectTransform Block(Transform list, string name, bool framed = true, float spacing = 4f)
        {
            var img = Image(list, name, framed ? SprFrame : null, framed ? Color.white : new Color(0, 0, 0, 0));
            var v = img.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = framed ? new RectOffset(12, 12, 10, 12) : new RectOffset(0, 0, 0, 0);
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childControlWidth = true;
            return img.rectTransform;
        }

        /// <summary>A horizontal row inside a block.</summary>
        public static RectTransform Row(Transform block, string name, float height, float spacing = 8f)
        {
            var rt = Rect(block, name);
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childForceExpandHeight = true;
            h.childForceExpandWidth = false;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childAlignment = TextAnchor.MiddleLeft;
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            return rt;
        }

        public static LayoutElement Size(this Component c, float width = -1f, float height = -1f, float flex = -1f)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) { le.minWidth = width; le.preferredWidth = width; }
            if (height >= 0) { le.minHeight = height; le.preferredHeight = height; }
            if (flex >= 0) le.flexibleWidth = flex;
            return le;
        }

        /// <summary>Wrapped paragraph that sizes itself in a vertical layout.</summary>
        public static TextMeshProUGUI Para(Transform block, string text, float size = 20f, Color? color = null)
        {
            var t = Text(block, "para", text, size, color);
            var fit = t.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return t;
        }

        public static void Clear(Transform t)
        {
            for (var i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                Object.Destroy(c);
            }
        }

        /// <summary>A screen-space canvas of our own (toasts, reports) scaled like the game's 1920x1080 UI.</summary>
        public static Canvas OverlayCanvas(string name, int order)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            go.layer = 5;
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        private static readonly System.Text.RegularExpressions.Regex StatWord = new System.Text.RegularExpressions.Regex(@"(?<![#\w>])(STR|INT|SEN)(?![\w<])");

        /// <summary>STR, INT and SEN in the game's stat colors wherever they appear in a line of text.</summary>
        public static string StatColors(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf("STR", StringComparison.Ordinal) < 0 && text.IndexOf("INT", StringComparison.Ordinal) < 0 && text.IndexOf("SEN", StringComparison.Ordinal) < 0) return text;
            return StatWord.Replace(text, m => "<color=" + (m.Value == "STR" ? "#ff6a6a" : m.Value == "INT" ? "#6ab4ff" : "#ff8ad8") + ">" + m.Value + "</color>");
        }
    }

    /// <summary>The game's own click sounds.</summary>
    internal static class Sfx
    {
        public static void Select() { Play(Illusion.Game.SystemSE.sel); }
        public static void Ok() { Play(Illusion.Game.SystemSE.ok_s); }
        public static void Cancel() { Play(Illusion.Game.SystemSE.cancel); }

        private static void Play(Illusion.Game.SystemSE se)
        {
            try { Illusion.Game.Utils.Sound.Play(se); } catch { }
        }
    }
}
