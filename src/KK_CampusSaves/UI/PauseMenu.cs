using System;
using HarmonyLib;
using Manager;
using UnityEngine;

namespace Joojkk.CampusSaves
{
    /// <summary>
    /// What Esc opens in story mode, in place of the game's Quit screen. Time stops as it does under that screen. Save needs a
    /// calm moment, the same one autosave waits for; what would lose the day asks on the game's confirmation screen.
    /// </summary>
    internal sealed class PauseMenu : MonoBehaviour
    {
        private const float W = 460f, H = 420f, ButtonH = 52f;
        private const string Lost = " Anything not saved is lost.";

        public static bool Shown;

        private static PauseMenu _me;
        private float _time = 1f;
        private bool _lock, _cursor, _giveBack, _relock;
        private string _note = "";
        private GUIStyle _panel, _btn, _title, _noteStyle;
        private Texture2D _shade;

        public static bool InStory
        {
            get { return Singleton<Game>.IsInstance() && Singleton<Game>.Instance.saveData != null && Singleton<Game>.Instance.actScene != null; }
        }

        private static bool MapLock
        {
            get { return Singleton<Game>.IsInstance() && Singleton<Game>.Instance.actScene != null && Singleton<Game>.Instance.actScene.isCursorLock; }
            set { if (Singleton<Game>.IsInstance() && Singleton<Game>.Instance.actScene != null) Traverse.Create(Singleton<Game>.Instance.actScene).Field("_isCursorLock").SetValue(value); }
        }

        private void Awake() { _me = this; }

        /// <summary>Esc opens it and Esc closes it.</summary>
        public static void Toggle()
        {
            if (Shown) _me.Close();
            else _me.Open();
        }

        private void Open()
        {
            _note = "";
            _giveBack = false;
            _time = 1f;
            _lock = Cursor.lockState != CursorLockMode.None;
            _cursor = Cursor.visible;
            Shown = true;
        }

        private void Close()
        {
            Shown = false;
            Time.timeScale = _time;
            _giveBack = true;
            Cursor.lockState = _lock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = _cursor;
        }

        private void Update()
        {
            // The quit screen that closed this menu puts back the stopped time when it goes.
            if (_giveBack && !Singleton<Scene>.Instance.NowSceneNames.Contains("Exit"))
            {
                _giveBack = false;
                Time.timeScale = _time;
            }
            // The map locks the cursor again as a screen added over it goes, and a locked cursor reaches no button.
            if (Shown || SavesPanel.Shown)
            {
                if (MapLock && !SavesPanel.Asking)
                {
                    MapLock = false;
                    _relock = true;
                }
            }
            else if (_relock)
            {
                _relock = false;
                MapLock = true;
            }
            if (!Shown) return;
            if (!InStory) { Close(); return; }
            if (SavesPanel.Asking) return;
            if (Input.GetMouseButtonDown(1)) { Close(); return; }
            if (Time.timeScale != 0f)
            {
                _time = Time.timeScale;
                Time.timeScale = 0f;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Styles()
        {
            if (_panel != null) return;
            _shade = Art.Solid(new Color(0f, 0f, 0f, 0.6f));
            _panel = new GUIStyle { border = new RectOffset(14, 14, 14, 14) };
            _panel.normal.background = Art.Card(new Color(0.17f, 0.19f, 0.26f), 14);
            _title = new GUIStyle(GUI.skin.label) { fontSize = 21, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _title.normal.textColor = Color.white;
            _noteStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            _noteStyle.normal.textColor = new Color(1f, 0.82f, 0.45f);
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 18, border = new RectOffset(8, 8, 8, 8), fontStyle = FontStyle.Bold };
            var c = new Color(0.33f, 0.38f, 0.52f);
            _btn.normal.background = Art.Card(c, 8);
            _btn.hover.background = _btn.active.background = Art.Card(Color.Lerp(c, Color.white, 0.18f), 8);
            _btn.normal.textColor = _btn.hover.textColor = _btn.active.textColor = Color.white;
        }

        private void OnGUI()
        {
            if (!Shown || SavesPanel.Asking) return;
            Styles();
            GUI.depth = -60;
            var s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            var sw = Screen.width / s;
            var sh = Screen.height / s;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _shade);
            var r = new Rect((sw - W) / 2f, (sh - H) / 2f, W, H);
            GUI.Box(r, GUIContent.none, _panel);
            GUI.BeginGroup(r);
            GUI.Label(new Rect(0, 12, W, 36), "Menu", _title);
            var safe = SaveFlow.Safe() && !Campuses.Busy;
            if (Row(0, "Resume")) Close();
            GUI.enabled = safe;
            if (Row(1, "Save")) _note = SaveFlow.SaveNow("manual", "menu");
            if (Row(2, "Save & Quit to title")) SaveAndLeave();
            GUI.enabled = true;
            if (Row(3, "Quit to title")) Ask("Quit to the title screen?" + Lost, ToTitle);
            if (Row(4, "Quit")) Ask("Quit the game?" + Lost, () => Singleton<Scene>.Instance.GameEnd(false));
            GUI.Label(new Rect(16, H - 34, W - 32, 26), safe ? _note : "You can save when you are free on the map or in your room", _noteStyle);
            GUI.EndGroup();
            GUI.matrix = old;
            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp) e.Use();
        }

        private bool Row(int i, string label) { return GUI.Button(new Rect(40f, 60f + i * (ButtonH + 12f), W - 80f, ButtonH), label, _btn); }

        private void SaveAndLeave()
        {
            _note = SaveFlow.SaveNow("manual", "menu");
            if (_note == "Saved") ToTitle();
        }

        private void Ask(string text, Action yes)
        {
            Time.timeScale = _time;
            SavesPanel.Ask(text, yes);
        }

        private void ToTitle()
        {
            Close();
            StartCoroutine(Illusion.Game.Utils.Scene.ReturnTitle(true));
        }
    }
}
