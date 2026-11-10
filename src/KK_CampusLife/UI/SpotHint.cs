using System;
using System.Collections.Generic;
using System.Linq;
using KKAPI.MainGame;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Standing at an activity icon (the icon lit up): each activity there with its icon, its name and what it gives
    /// ("+3-5 INT") in a bar above the bottom of the screen, like the game's own action prompts; why it is closed only when
    /// it is, and your energy as a small bar.
    /// </summary>
    internal static class SpotHint
    {
        private static Canvas _canvas;
        private static RectTransform _box;
        private static ActivitySpots.Anchor _at;
        private static int _atMap = -1;
        private static string _shown = "";
        private static float _nextRefresh;

        /// <summary>The player walked into this spot's icon trigger.</summary>
        /// <summary>The text on screen now.</summary>
        public static string ShownText { get { return _canvas != null && _canvas.gameObject.activeSelf ? System.Text.RegularExpressions.Regex.Replace(_shown, "<[^>]+>", "") : ""; } }

        public static void Enter(ActivitySpots.Anchor an)
        {
            _at = an;
            _atMap = G.PlayerMapNo;
            _nextRefresh = 0f;
        }

        public static void Exit(ActivitySpots.Anchor an)
        {
            if (_at == an) _at = null;
        }

        public static void Update()
        {
            try
            {
                var an = _at;
                var show = an != null && Campus.On && G.IsFreeRoaming && !Scenes.Running && !GameAPI.InsideHScene && !TransitionScreen.Visible
                           && !ActivitySpots.Busy && G.PlayerMapNo == _atMap && G.Act != null && G.Act.Player != null
                           && (G.Act.Player.position - an.IconPos).sqrMagnitude < 4f * 4f;
                if (!show)
                {
                    if (an != null && G.PlayerMapNo != _atMap) _at = null;
                    if (_canvas != null && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
                    _shown = "";
                    return;
                }
                if (_canvas == null) Build();
                if (!_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
                if (Time.realtimeSinceStartup < _nextRefresh && _shown.Length > 0) return;
                _nextRefresh = Time.realtimeSinceStartup + 0.5f; // energy, money and the period can change while she stands there
                var text = Describe(an);
                if (text == _shown) return;
                _shown = text;
                GameUi.Clear(_box);
                Draw(an);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Spot hint: " + e.Message);
                _at = null;
            }
        }

        // One line per activity, its map icon on the left, the name in gold and what it gives beside it; the period only
        // when it is the reason it is closed, the reason in one short warning; your energy as a small bar.
        private const float IconSize = 46f, Pad = 14f, Gap = 8f;

        /// <summary>The text of the hint.</summary>
        private static string Describe(ActivitySpots.Anchor an)
        {
            var parts = an.Acts.Take(3).Select(a => a.Name + " " + Activities.GainText(a) + " " + (Short(Activities.Blocker(Campus.S, a)) ?? "")).ToList();
            if (Cfg.EnergyEnabled.Value) parts.Add(Activities.EnergyNow(Campus.S));
            return string.Join("\n", parts.ToArray());
        }

        /// <summary>The blocker in a few words (null when she can do it).</summary>
        private static string Short(string block)
        {
            if (block == null) return null;
            if (block.StartsWith("Not available in this period", StringComparison.Ordinal))
            {
                var open = block.IndexOf('(');
                return open > 0 ? "Only " + block.Substring(open + 1).TrimEnd('.', ')') : "Not now";
            }
            if (block.StartsWith("Only during", StringComparison.Ordinal)) return "Free periods only";
            if (block.StartsWith("You're in the middle", StringComparison.Ordinal)) return "Busy";
            return block.TrimEnd('.');
        }

        private static void Draw(ActivitySpots.Anchor an)
        {
            var gold = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Gold);
            var warn = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Warn);
            var bar = GameUi.Image(_box, "hint", null, GameUi.NoteBack);
            bar.raycastTarget = false;
            var rows = new System.Collections.Generic.List<KeyValuePair<Image, TMPro.TextMeshProUGUI>>();
            float w = 0f, h = Pad;
            foreach (var a in an.Acts.Take(3))
            {
                var block = Short(Activities.Blocker(Campus.S, a));
                var icon = GameUi.Image(bar.transform, "icon", null, block == null ? Color.white : new Color(1f, 1f, 1f, 0.75f));
                icon.sprite = ActivityIcon.Get(ActivitySpots.KindOf(a), a.Id, block == null);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var gain = GameUi.StatColors(Activities.GainText(a));
                var text = "<color=" + gold + ">" + a.Name + "</color>" + (gain.Length > 0 ? "   <size=19>" + gain + "</size>" : "")
                           + (block != null ? "\n<size=17><color=" + warn + ">" + block + "</color></size>" : "");
                var t = GameUi.Text(bar.transform, "text", text, 22f, Color.white, TMPro.TextAlignmentOptions.MidlineLeft, false);
                t.raycastTarget = false;
                var size = t.GetPreferredValues(text);
                var rowH = Mathf.Max(IconSize, size.y);
                w = Mathf.Max(w, IconSize + 12f + size.x);
                rows.Add(new KeyValuePair<Image, TMPro.TextMeshProUGUI>(icon, t));
                Place(icon.rectTransform, Pad, h + (rowH - IconSize) / 2f, IconSize, IconSize);
                Place(t.rectTransform, Pad + IconSize + 12f, h, size.x + 4f, rowH);
                h += rowH + Gap;
            }
            if (Cfg.EnergyEnabled.Value)
            {
                // Energy: a thin bar with the number, under the activities.
                var s = Campus.S;
                var label = GameUi.Text(bar.transform, "energy", "<size=17>Energy " + s.Energy + "/" + Activities.MaxEnergy + "</size>", 17f, GameUi.Dim, TMPro.TextAlignmentOptions.MidlineLeft, false);
                label.raycastTarget = false;
                var lw = label.GetPreferredValues(label.text).x + 4f;
                Place(label.rectTransform, Pad, h, lw, 22f);
                var track = GameUi.Image(bar.transform, "track", null, new Color(1f, 1f, 1f, 0.15f));
                var fillW = Mathf.Max(120f, w - lw - 12f);
                Place(track.rectTransform, Pad + lw + 10f, h + 7f, fillW, 8f);
                var fill = GameUi.Image(track.transform, "fill", null, s.Energy * 3 < Activities.MaxEnergy ? GameUi.Warn : new Color(0.45f, 0.85f, 0.55f, 1f));
                var frac = Mathf.Clamp01(s.Energy / (float)Math.Max(1, Activities.MaxEnergy));
                var frt = fill.rectTransform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = new Vector2(frac, 1f);
                frt.offsetMin = frt.offsetMax = Vector2.zero;
                w = Mathf.Max(w, lw + 10f + fillW);
                h += 22f + Gap;
            }
            h += Pad - Gap;
            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w + 2f * Pad, h);
        }

        /// <summary>From the bar's top left corner, y down.</summary>
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void Build()
        {
            _canvas = GameUi.OverlayCanvas("CampusLife_SpotHint", 390);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            _box = GameUi.Rect(_canvas.transform, "box");
            _box.anchorMin = _box.anchorMax = new Vector2(0.5f, 0f);
            _box.pivot = new Vector2(0.5f, 0f);
            _box.anchoredPosition = new Vector2(0f, 230f);
            _box.sizeDelta = Vector2.zero;
        }


    }
}
