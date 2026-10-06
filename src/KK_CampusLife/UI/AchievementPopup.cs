using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// igual a steam"): a short ding (the club report's) and a small card that slides up from the bottom right corner,
    /// stays a few seconds and slides back down. Several in a row wait their turn. While a loading screen covers the
    /// game, the card waits too.
    /// </summary>
    internal static class AchievementPopup
    {
        private const float W = 440f, H = 108f, Margin = 24f, Slide = 0.35f, Stay = 5f;

        private sealed class Card { public string Title, Sub, Count; }

        private static readonly Queue<Card> Waiting = new Queue<Card>();
        private static Canvas _canvas;
        private static RectTransform _card;
        private static TextMeshProUGUI _title, _sub, _count;
        private static float _t = -1f;   // seconds since the shown card started; -1: none

        public static void Show(string title, string sub, int done, int total)
        {
            if (Cfg.AchievementPopups != null && !Cfg.AchievementPopups.Value) return;
            Waiting.Enqueue(new Card { Title = title, Sub = sub ?? "", Count = done + "/" + total });
            if (Waiting.Count > 4) Waiting.Dequeue();
        }

        public static string Current { get { return _t >= 0f && _title != null ? _title.text : ""; } }

        public static void Update()
        {
            try
            {
                if (TransitionScreen.Visible) return;
                if (_t < 0f)
                {
                    if (Waiting.Count == 0) return;
                    if (_canvas == null) Build();
                    var c = Waiting.Dequeue();
                    _title.text = c.Title;
                    _sub.text = c.Sub;
                    _count.text = c.Count;
                    _canvas.gameObject.SetActive(true);
                    _t = 0f;
                    try { Illusion.Game.Utils.Sound.Play(Illusion.Game.SystemSE.result_single); } catch { }
                }
                _t += Time.unscaledDeltaTime;
                // Up, a while there, back down.
                var k = _t < Slide ? _t / Slide : _t < Slide + Stay ? 1f : 1f - (_t - Slide - Stay) / Slide;
                k = Mathf.Clamp01(k);
                k = k * k * (3f - 2f * k);
                _card.anchoredPosition = new Vector2(-Margin, Mathf.Lerp(-H - 4f, Margin, k));
                if (_t >= Stay + 2f * Slide)
                {
                    _t = -1f;
                    if (Waiting.Count == 0) _canvas.gameObject.SetActive(false);
                }
            }
            catch (System.Exception e)
            {
                CampusLifePlugin.Log.LogDebug("Achievement card: " + e.Message);
                _t = -1f;
                if (_canvas != null) Object.Destroy(_canvas.gameObject);
                _canvas = null;
            }
        }

        private static void Build()
        {
            _canvas = GameUi.OverlayCanvas("CampusLife_Achievement", 460);
            _canvas.GetComponent<GraphicRaycaster>().enabled = false;
            var back = GameUi.Image(_canvas.transform, "card", null, GameUi.NoteBack);
            _card = back.rectTransform;
            _card.anchorMin = _card.anchorMax = new Vector2(1f, 0f);
            _card.pivot = new Vector2(1f, 0f);
            _card.sizeDelta = new Vector2(W, H);
            var edge = GameUi.Image(_card, "edge", null, GameUi.Gold);
            edge.rectTransform.Place(0f, 0f, 5f, H);
            // The star tile, in the game's gold.
            var tile = GameUi.Image(_card, "tile", null, new Color(0.22f, 0.18f, 0.08f, 1f));
            tile.rectTransform.Place(16f, 14f, 80f, 80f);
            var star = GameUi.Text(tile.rectTransform, "star", "★", 58f, GameUi.Gold, TextAlignmentOptions.Center, false);
            star.rectTransform.Stretch();
            var head = GameUi.Text(_card, "head", "Achievement unlocked", 17f, GameUi.Dim, TextAlignmentOptions.TopLeft, false);
            head.rectTransform.Place(112f, 12f, 240f, 22f);
            _count = GameUi.Text(_card, "count", "", 17f, GameUi.Gold, TextAlignmentOptions.TopRight, false);
            _count.rectTransform.Place(W - 96f, 12f, 80f, 22f);
            _title = GameUi.Text(_card, "title", "", 25f, GameUi.White, TextAlignmentOptions.TopLeft, false);
            _title.rectTransform.Place(112f, 36f, W - 128f, 32f);
            _title.overflowMode = TextOverflowModes.Ellipsis;
            _sub = GameUi.Text(_card, "sub", "", 17f, GameUi.Dim, TextAlignmentOptions.TopLeft, false);
            _sub.rectTransform.Place(112f, 70f, W - 128f, 24f);
            _sub.overflowMode = TextOverflowModes.Ellipsis;
        }
    }
}
