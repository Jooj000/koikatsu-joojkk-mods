using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The black screen with a line of text that the game shows for its "dark" actions, drawn by us: fade to black, one
    /// line at a time (click or wait to go on), fade back. No ADV scene, so it cannot get stuck or leave the screen white.
    /// </summary>
    internal static class DarkScreen
    {
        private const float Fade = 0.35f;

        public static IEnumerator Show(IList<string> lines)
        {
            var go = new GameObject("cl_dark_screen");
            Object.DontDestroyOnLoad(go);
            try
            {
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32000;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                go.AddComponent<GraphicRaycaster>();
                var group = go.AddComponent<CanvasGroup>();
                group.alpha = 0f;

                var bg = new GameObject("bg", typeof(RectTransform)).AddComponent<Image>();
                bg.transform.SetParent(go.transform, false);
                bg.color = Color.black;
                Stretch(bg.rectTransform);

                var text = new GameObject("text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
                text.transform.SetParent(go.transform, false);
                if (GameUi.Font != null) text.font = GameUi.Font;
                text.fontSize = 40f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.enableWordWrapping = true;
                var rt = text.rectTransform;
                rt.anchorMin = new Vector2(0.15f, 0.3f);
                rt.anchorMax = new Vector2(0.85f, 0.7f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                text.text = "";

                _canvasGroup = group;
                _text = text;
            }
            catch (System.Exception e)
            {
                CampusLifePlugin.Log.LogError("Dark screen: " + e);
                Object.Destroy(go);
                yield break;
            }

            for (var t = 0f; t < Fade; t += Time.unscaledDeltaTime) { if (_canvasGroup != null) _canvasGroup.alpha = t / Fade; yield return null; }
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                if (_text != null) _text.text = line;
                var until = Time.realtimeSinceStartup + Mathf.Clamp(1.2f + line.Length * 0.045f, 2f, 6f);
                yield return null;
                while (Time.realtimeSinceStartup < until && !Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.Space)) yield return null;
            }
            if (_text != null) _text.text = "";
            for (var t = 0f; t < Fade; t += Time.unscaledDeltaTime) { if (_canvasGroup != null) _canvasGroup.alpha = 1f - t / Fade; yield return null; }
            Object.Destroy(go);
            _canvasGroup = null;
            _text = null;
        }

        private static CanvasGroup _canvasGroup;
        private static TextMeshProUGUI _text;

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
