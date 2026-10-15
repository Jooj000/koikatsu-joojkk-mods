using Manager;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// quarto"), to plan the next day. Painted here because the game's sprites are only captured once the clipboard opens.
    /// </summary>
    internal static class NightIcon
    {
        private const float Size = 96f, Margin = 24f;

        private static Texture2D _icon;
        private static GUIStyle _button, _label;

        private static bool Idle
        {
            get
            {
                if (!StatusUi.AtNight || StatusUi.IsOpen) return false;
                var scene = Singleton<Scene>.Instance;
                return scene.NowSceneNames.Count <= 2 && !scene.IsNowLoadingFade;
            }
        }

        private static void Styles()
        {
            if (_button != null) return;
            _icon = Clipboard(new Color(1f, 0.86f, 0.45f));
            var c = new Color(0.11f, 0.12f, 0.17f, 0.9f);
            _button = new GUIStyle(GUI.skin.button) { border = new RectOffset(10, 10, 10, 10) };
            _button.normal.background = Rounded(c, 10);
            _button.hover.background = _button.active.background = Rounded(new Color(0.24f, 0.27f, 0.37f, 0.95f), 10);
            _label = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, wordWrap = false };
            _label.normal.textColor = Color.white;
        }

        public static void OnGUI()
        {
            if (!Idle) return;
            Styles();
            var s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            var r = new Rect(Margin, Margin, Size, Size);
            if (GUI.Button(r, GUIContent.none, _button))
            {
                Sfx.Ok();
                StatusUi.Open(PanelTab.Today);
            }
            GUI.DrawTexture(new Rect(r.x + 24f, r.y + 8f, 48f, 48f), _icon);
            GUI.Label(new Rect(r.x, r.y + 60f, Size, 26f), "Campus Life", _label);
            GUI.matrix = old;
        }

        private static Texture2D Rounded(Color c, int r)
        {
            var size = r * 2 + 2;
            return Paint(size, (x, y) =>
            {
                var dx = Mathf.Max(r - x, x - (size - r));
                var dy = Mathf.Max(r - y, y - (size - r));
                return dx <= 0f || dy <= 0f || dx * dx + dy * dy <= r * r;
            }, c);
        }

        /// <summary>A clipboard: the board, the clip on top and the lines of the page cut out.</summary>
        private static Texture2D Clipboard(Color c)
        {
            return Paint(64, (x, y) =>
            {
                if (x >= 23f && x <= 41f && y >= 3f && y <= 13f) return true;
                if (x < 10f || x > 54f || y < 8f || y > 61f) return false;
                if (y >= 8f && y <= 15f && x >= 20f && x <= 44f) return false;
                var line = x >= 18f && x <= 46f && (y >= 24f && y <= 28f || y >= 35f && y <= 39f || y >= 46f && y <= 50f);
                return !line;
            }, c);
        }

        private static Texture2D Paint(int size, System.Func<float, float, bool> inside, Color c)
        {
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var hits = 0;
                    for (var sy = 0; sy < 4; sy++)
                        for (var sx = 0; sx < 4; sx++)
                            if (inside(x + (sx + 0.5f) / 4f, y + (sy + 0.5f) / 4f)) hits++;
                    px[(size - 1 - y) * size + x] = new Color(c.r, c.g, c.b, c.a * hits / 16f);
                }
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }
    }
}
