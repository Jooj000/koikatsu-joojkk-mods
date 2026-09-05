using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.MainGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>Notifications drawn with the game's bar sprite and font, stacked on the right side of the screen.</summary>
    internal static class ToastUi
    {
        private static Canvas _canvas;
        private static RectTransform _stack;
        private static string _shownKey = "";

        public static void Update()
        {
            var list = Toasts.Current.ToList();
            var hide = list.Count == 0 || GameAPI.InsideHScene || !Cfg.ShowToasts.Value || TransitionScreen.Visible;
            if (hide)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
                _shownKey = "";
                return;
            }
            if (_canvas == null) Build();
            if (!_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
            var key = string.Join("\n", list.ToArray());
            if (key == _shownKey) return;
            _shownKey = key;
            GameUi.Clear(_stack);
            foreach (var text in list)
            {
                var bar = GameUi.Image(_stack, "toast", null, GameUi.NoteBack);
                var v = bar.gameObject.AddComponent<VerticalLayoutGroup>();
                v.padding = new RectOffset(18, 18, 10, 12);
                v.childControlHeight = true;
                v.childControlWidth = true;
                v.childForceExpandHeight = false;
                var t = GameUi.Para(bar.transform, GameUi.StatColors(text), 22f, Color.white);
                t.raycastTarget = false;
            }
        }

        private static void Build()
        {
            _canvas = GameUi.OverlayCanvas("CampusLife_Toasts", 400);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            _stack = GameUi.Rect(_canvas.transform, "stack");
            _stack.anchorMin = _stack.anchorMax = new Vector2(1f, 1f);
            _stack.pivot = new Vector2(1f, 1f);
            _stack.anchoredPosition = new Vector2(-24f, -150f);
            _stack.sizeDelta = new Vector2(560f, 0f);
            var v = _stack.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = 8f;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            var fit = _stack.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    /// <summary>The weekly report: a framed sheet over the night menu.</summary>
    internal static class ReportUi
    {
        private static Canvas _canvas;

        public static bool Visible { get { return _canvas != null && _canvas.gameObject.activeSelf; } }

        public static void Show(List<string> lines)
        {
            if (_canvas != null) Object.Destroy(_canvas.gameObject);
            _canvas = GameUi.OverlayCanvas("CampusLife_Report", 450);

            var shade = GameUi.Image(_canvas.transform, "shade", null, new Color(0f, 0f, 0f, 0.55f));
            shade.rectTransform.Stretch();
            shade.raycastTarget = true;

            var frame = GameUi.Image(_canvas.transform, "frame", GameUi.SprFrame);
            frame.raycastTarget = true;
            var rt = frame.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1180f, 820f);
            var back = GameUi.Image(rt, "back", null, GameUi.PanelBack);
            back.rectTransform.Stretch(6f, 6f, 6f, 6f);

            var title = lines.Count > 0 ? lines[0] : "Weekly report";
            var header = GameUi.Bar(rt, "header", title, true, 28f);
            header.Place(20f, 18f, 1140f, 40f);

            ScrollRect scroll;
            var list = GameUi.ScrollList(rt, "list", out scroll, 6f);
            ((RectTransform)scroll.transform).Place(24f, 72f, 1132f, 660f);
            var block = GameUi.Block(list, "text");
            foreach (var l in lines.Skip(1))
                GameUi.Para(block, string.IsNullOrEmpty(l) ? " " : l, 22f, l.StartsWith("  ", StringComparison.Ordinal) ? GameUi.Dim : Color.white);

            var close = GameUi.Button(rt, "close", "Close", Hide);
            ((RectTransform)close.transform).Place(1180f / 2f - 110f, 750f, 220f, 48f);
        }

        public static void Hide()
        {
            if (_canvas != null) Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }
    }
}
