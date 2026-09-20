using System;
using System.Linq;
using KKAPI.MainGame;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Standing at an activity icon (the icon lit up): its name and what it gives ("+3-5 INT, -1 energy") in a bar
    /// above the bottom of the screen, like the game's own action prompts.
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
                // Dark graphite: unlike the game's blue bar it differs from every text color used on it (white, gold,
                // the STR / INT / SEN colors and the warning orange).
                var bar = GameUi.Image(_box, "hint", null, GameUi.NoteBack);
                bar.raycastTarget = false;
                var t = GameUi.Text(bar.transform, "text", text, 22f, Color.white, TMPro.TextAlignmentOptions.Center, false);
                t.enableWordWrapping = false;
                t.raycastTarget = false;
                // The box is as big as its lines (measured), plus a margin.
                var size = t.GetPreferredValues(text);
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(size.x + 36f, size.y + 18f);
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(18f, 9f);
                t.rectTransform.offsetMax = new Vector2(-18f, -9f);
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Spot hint: " + e.Message);
                _at = null;
            }
        }

        private static string Describe(ActivitySpots.Anchor an)
        {
            var gold = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Gold);
            var warn = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Warn);
            var dim = "#" + ColorUtility.ToHtmlStringRGB(GameUi.Dim);
            var lines = an.Acts.Take(3).Select(a =>
            {
                var gain = GameUi.StatColors(Activities.GainText(a));
                var block = Activities.Blocker(Campus.S, a);
                return "<color=" + gold + ">" + a.Name + "</color>" + (gain.Length > 0 ? "\n" + gain : "")
                       + "\n<size=18><color=" + dim + ">" + Activities.PeriodText(a) + "</color></size>"
                       + (block != null ? "\n<size=18><color=" + warn + ">" + block + "</color></size>" : "");
            });
            // Your energy next to what it costs.
            var energy = Cfg.EnergyEnabled.Value ? "\n<size=18>" + Activities.EnergyNow(Campus.S) + "</size>" : "";
            return string.Join("\n", lines.ToArray()) + energy;
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
