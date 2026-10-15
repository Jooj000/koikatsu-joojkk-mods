using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using Manager;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Joojkk.CampusSaves
{
    /// <summary>
    /// The saves panel: one block per campus, two columns by three rows with scrolling, and a larger page with the history of
    /// one campus. Nothing here changes a file until a button is pressed, and what removes something asks first, on the game's
    /// own confirmation screen. Over the game's Save screen the same panel saves instead of loading.
    /// </summary>
    internal sealed class SavesPanel : MonoBehaviour
    {
        private const float MainW = 1180f, MainH = 800f, DetailW = 1040f, DetailH = 780f;
        private const float BlockH = 160f, Gap = 12f, RowH = 92f;

        public static bool Shown;
        public static Campus Selected;     // the campus on the history page; null on the blocks page
        public static int Pick;            // the version chosen in the history
        public static LoadScene LoadUi;    // the game's Load screen under the panel, which loads for it on the title and at night
        public static SaveScene SaveUi;    // the game's Save screen under the panel: the panel saves
        public static bool Asking;         // the game's confirmation screen is up, in front of whatever asked

        private static SavesPanel _me;
        private static Action _loadNext, _afterTitle;
        private static bool _offerFree;
        private Vector2 _grid, _list;
        private float _lastClick, _titleSince;
        private int _lastRow;
        private readonly List<BaseInputModule> _held = new List<BaseInputModule>();
        private readonly Dictionary<string, Texture2D> _pics = new Dictionary<string, Texture2D>();

        private GUIStyle _panel, _card, _cardHover, _rowOn, _title, _name, _nameLong, _text, _dim, _note, _center;
        private GUIStyle _btn, _blue, _red, _green, _tool, _toolText, _empty;
        private Texture2D _lens, _trash, _shade, _close, _download, _drive, _crate, _vanilla;
        private GUISkin _skin;

        private void Awake() { _me = this; }

        public static void OpenOver(LoadScene screen)
        {
            LoadUi = screen;
            Open();
        }

        public static void OpenOver(SaveScene screen)
        {
            SaveUi = screen;
            Open();
        }

        private static void Open()
        {
            Selected = null;
            Campuses.Status = "";
            Shown = true;
            _over = true;
        }

        /// <summary>Closes the panel and, when it stands over the game's Load or Save screen, that screen too.</summary>
        private static void Leave()
        {
            Shown = false;
            Selected = null;
            if (LoadUi != null) LoadUi.onReturn();
            if (SaveUi != null) Traverse.Create(SaveUi).Method("UnLoad").GetValue();
        }

        /// <summary>
        /// Asks on the game's own confirmation screen, the one Quit uses, with the whole sentence. Nothing is drawn here while
        /// it is up, because this panel would cover it. Whatever answer comes, the screen closes first and the answer runs after.
        /// </summary>
        public static void Ask(string text, Action yes, Action no = null)
        {
            if (Asking || _me == null) return;
            Asking = true;
            var param = new CheckScene.Parameter
            {
                Title = text,
                Yes = () => _me.StartCoroutine(_me.Answer(yes)),
                No = () => _me.StartCoroutine(_me.Answer(no)),
                isYesFocus = false,
            };
            _me.StartCoroutine(Illusion.Game.Utils.Scene.Check.Load(param, (Action<CheckScene>)(s => { })));
        }

        private IEnumerator Answer(Action then)
        {
            yield return null;
            Singleton<Scene>.Instance.UnLoad();
            yield return null;
            Asking = false;
            if (then == null) yield break;
            try { then(); }
            catch (Exception e) { Campuses.Status = "Failed: " + e.Message; CampusSavesPlugin.Log.LogError(e); }
        }

        private static bool _over;

        private void Update()
        {
            try { if (CampusSavesPlugin.PanelKey.Value.IsDown()) Shown = !Shown; }
            catch { }

            // The right button leaves the game's Load screen on its own.
            if (_over && LoadUi == null && SaveUi == null)
            {
                _over = false;
                Shown = false;
                Selected = null;
            }

            HoldClicks();
            if (!Shown && _pics.Count > 0) ClearPictures();
            if (_offerFree && !Campuses.Busy && !Asking)
            {
                _offerFree = false;
                if (Shown) OfferFree();
            }
            if (_afterTitle == null) return;
            if (!AtTitle || Scene.isReturnTitle || Singleton<Scene>.Instance.IsNowLoadingFade) { _titleSince = Time.realtimeSinceStartup; return; }
            if (Time.realtimeSinceStartup - _titleSince < 0.3f) return;
            var a = _afterTitle;
            _afterTitle = null;
            a();
        }

        /// <summary>
        /// Clicks on the panel must not reach the game's buttons behind it. Scenes added on top bring their own event systems,
        /// so every input module is held, and the Load screen, which would still take a click, stops seeing the mouse.
        /// </summary>
        private void HoldClicks()
        {
            var list = LoadUi != null ? Traverse.Create(LoadUi).Field("_canvasGroup").GetValue<CanvasGroup>() : SaveUi != null ? Traverse.Create(SaveUi).Field("_canvasGroup").GetValue<CanvasGroup>() : null;
            if (list != null) list.blocksRaycasts = !Shown;
            if ((Shown || PauseMenu.Shown) && !Asking)
            {
                if (_held.Count > 0 && Time.frameCount % 20 != 0) return;
                foreach (var m in FindObjectsOfType<BaseInputModule>())
                    if (m.enabled)
                    {
                        m.enabled = false;
                        _held.Add(m);
                    }
                return;
            }
            foreach (var m in _held) if (m != null) m.enabled = true;
            _held.Clear();
        }

        private void ClearPictures()
        {
            foreach (var t in _pics.Values) if (t != null) Destroy(t);
            _pics.Clear();
        }

        private void LateUpdate()
        {
            var a = _loadNext;
            if (a == null) return;
            _loadNext = null;
            Shown = false;
            Selected = null;
            try { a(); }
            catch (Exception e) { Campuses.Status = "Failed: " + e.Message; CampusSavesPlugin.Log.LogError(e); }
        }

        private static bool AtTitle { get { return Singleton<Scene>.IsInstance() && Singleton<Scene>.Instance.NowSceneNames.Contains("Title"); } }

        private static bool CanLoad { get { return SaveUi == null && (LoadUi != null || AtTitle || SaveFlow.Safe()); } }

        private const string LoadElsewhere = "Saves load when you are free on the map or from the Load screen";

        private static string Mb(long b) { return b >= 1000000000 ? (b / 1e9).ToString("0.0") + " GB" : (b / 1e6).ToString("0") + " MB"; }

        private static GUIStyle Box(Texture2D t, int r)
        {
            var s = new GUIStyle { border = new RectOffset(r, r, r, r) };
            s.normal.background = t;
            return s;
        }

        private static GUIStyle Text(int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, FontStyle f = FontStyle.Normal)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = a, fontStyle = f, wordWrap = false, clipping = TextClipping.Clip };
            s.normal.textColor = c;
            return s;
        }

        private static GUIStyle Button(Color c, int size = 15)
        {
            var s = new GUIStyle(GUI.skin.button) { fontSize = size, border = new RectOffset(8, 8, 8, 8), padding = new RectOffset(6, 6, 2, 2), fontStyle = FontStyle.Bold };
            s.normal.background = Art.Card(c, 8);
            s.hover.background = s.active.background = Art.Card(Color.Lerp(c, Color.white, 0.18f), 8);
            s.normal.textColor = s.hover.textColor = s.active.textColor = Color.white;
            return s;
        }

        private void Styles()
        {
            if (_panel != null) return;
            _shade = Art.Solid(new Color(0f, 0f, 0f, 0.6f));
            _panel = Box(Art.Card(new Color(0.11f, 0.12f, 0.17f), 14), 14);
            _card = Box(Art.Card(new Color(0.18f, 0.20f, 0.28f), 12), 12);
            _cardHover = Box(Art.Card(new Color(0.24f, 0.27f, 0.37f), 12), 12);
            _rowOn = Box(Art.Card(new Color(0.20f, 0.40f, 0.72f), 12), 12);
            _empty = Box(Art.Card(new Color(0.08f, 0.09f, 0.13f), 12), 12);
            var white = new Color(0.95f, 0.96f, 0.98f);
            var grey = new Color(0.68f, 0.71f, 0.78f);
            _title = Text(21, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            _name = Text(18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            _nameLong = Text(15, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            _nameLong.wordWrap = true;
            _text = Text(15, white);
            _dim = Text(14, grey);
            _note = Text(14, new Color(1f, 0.82f, 0.45f), TextAnchor.MiddleCenter);
            _center = Text(14, grey, TextAnchor.MiddleCenter);
            _btn = Button(new Color(0.26f, 0.29f, 0.40f));
            _blue = Button(new Color(0.16f, 0.45f, 0.88f));
            _red = Button(new Color(0.80f, 0.22f, 0.24f), 13);
            _green = Button(new Color(0.15f, 0.60f, 0.34f), 17);
            _tool = Button(new Color(0.20f, 0.23f, 0.32f));
            _toolText = Text(13, white, TextAnchor.MiddleCenter);
            _lens = Art.Lens();
            _trash = Art.Trash();
            _close = Art.Close();
            _download = Art.Download();
            _drive = Art.Drive();
            _crate = Art.Crate();
            _vanilla = Art.IceCream();
            _skin = Instantiate(GUI.skin);
            _skin.hideFlags = HideFlags.HideAndDontSave;
            _skin.verticalScrollbar = Bar(_skin.verticalScrollbar, new Color(0.08f, 0.09f, 0.13f));
            _skin.verticalScrollbarThumb = Bar(_skin.verticalScrollbarThumb, new Color(0.38f, 0.42f, 0.54f));
            _skin.verticalScrollbarUpButton = new GUIStyle { name = _skin.verticalScrollbarUpButton.name };
            _skin.verticalScrollbarDownButton = new GUIStyle { name = _skin.verticalScrollbarDownButton.name };
        }

        /// <summary>The scrollbars of the default skin are grey boxes.</summary>
        private static GUIStyle Bar(GUIStyle from, Color c)
        {
            var s = new GUIStyle(from) { fixedWidth = 12f, border = new RectOffset(6, 6, 6, 6), margin = new RectOffset(), padding = new RectOffset() };
            s.normal.background = Art.Card(c, 6);
            s.hover.background = s.active.background = Art.Card(Color.Lerp(c, Color.white, 0.15f), 6);
            return s;
        }

        private void OnGUI()
        {
            if (!Shown || Asking) return;
            Styles();
            GUI.depth = -50;
            var skin = GUI.skin;
            GUI.skin = _skin;
            var s = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
            var old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            var sw = Screen.width / s;
            var sh = Screen.height / s;
            GUI.DrawTexture(new Rect(0, 0, sw, sh), _shade);
            var w = Selected != null ? DetailW : MainW;
            var h = Selected != null ? DetailH : MainH;
            var r = new Rect((sw - w) / 2f, (sh - h) / 2f, w, h);
            GUI.Box(r, GUIContent.none, _panel);
            GUI.BeginGroup(r);
            if (Selected != null) DrawHistory(Selected);
            else DrawBlocks();
            GUI.EndGroup();
            GUI.matrix = old;
            GUI.skin = skin;
            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel) e.Use();
        }

        // ================= the blocks =================

        private void DrawBlocks()
        {
            GUI.Label(new Rect(0, 12, MainW, 36), SaveUi != null ? "Save" : "Campus Saves", _title);
            var view = new Rect(20, 56, MainW - 40, MainH - 56 - 124);
            var list = Campuses.Snapshot();
            var first = SaveUi != null ? 1 : 0;   // the Save screen starts with a block for a new save
            var rows = (list.Count + first + 1) / 2;
            var content = new Rect(0, 0, view.width - 16, rows * (BlockH + Gap) - Gap);
            var cell = (content.width - Gap) / 2f;
            var over = view.Contains(Event.current.mousePosition);
            Wheel(ref _grid, view, content.height);
            _grid = GUI.BeginScrollView(view, _grid, content, false, false);
            if (first == 1 && _grid.y < BlockH) NewBlock(new Rect(0, 0, cell, BlockH), over);
            for (var i = 0; i < list.Count; i++)
            {
                var at = i + first;
                var top = (at / 2) * (BlockH + Gap);
                if (top + BlockH < _grid.y || top > _grid.y + view.height) continue;
                Block(list[i], new Rect((at % 2) * (cell + Gap), top, cell, BlockH), over);
            }
            GUI.EndScrollView();
            if (list.Count == 0 && first == 0) GUI.Label(new Rect(0, 240, MainW, 30), "No campuses yet. Import brings your saves in, and your files stay as they are.", _center);

            var y = MainH - 112;
            GUI.enabled = !Campuses.Busy;
            if (Tool(0, y, _download, "Import")) ImportAll();
            if (Tool(1, y, _drive, "Free space")) OfferFree();
            if (Tool(2, y, _crate, "Export")) OfferExport();
            GUI.enabled = true;
            if (LoadUi != null && Tool(MainW - 248, y, _vanilla, "Vanilla", true)) Shown = false;
            if (Tool(MainW - 130, y, _close, "Close", true)) Leave();
            GUI.Label(new Rect(20, MainH - 30, MainW - 40, 22), Campuses.Busy ? "Working... " + Campuses.Status : Campuses.Status, _note);
        }

        /// <summary>A button of the bar: the icon with its name under it.</summary>
        private bool Tool(float at, float y, Texture2D icon, string label, bool placed = false)
        {
            var r = new Rect(placed ? at : 20f + at * 118f, y, 110f, 76f);
            var hit = GUI.Button(r, GUIContent.none, _tool);
            var tint = GUI.color;
            if (!GUI.enabled) GUI.color = new Color(1f, 1f, 1f, 0.4f);
            GUI.DrawTexture(new Rect(r.x + (r.width - 36f) / 2f, r.y + 7f, 36f, 36f), icon);
            GUI.Label(new Rect(r.x, r.y + 47f, r.width, 22f), label, _toolText);
            GUI.color = tint;
            return hit;
        }

        /// <summary>A step of the wheel moves less than a block, so the list reads as one long list.</summary>
        private static void Wheel(ref Vector2 at, Rect view, float height)
        {
            var e = Event.current;
            if (e.type != EventType.ScrollWheel || !view.Contains(e.mousePosition)) return;
            at.y = Mathf.Clamp(at.y + e.delta.y * 18f, 0f, Mathf.Max(0f, height - view.height));
            e.Use();
        }

        private void NewBlock(Rect r, bool over)
        {
            var e = Event.current;
            var hover = over && r.Contains(e.mousePosition);
            GUI.Box(r, GUIContent.none, hover ? _cardHover : _card);
            GUI.Label(new Rect(r.x, r.y + 46, r.width, 36), "+", _title);
            GUI.Label(new Rect(r.x, r.y + 86, r.width, 26), "New save", _toolText);
            if (!hover || e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            if (!Campuses.Busy) SaveHere(null);
        }

        private void SaveHere(Campus c)
        {
            Action save = () => _loadNext = () =>
            {
                var said = SaveFlow.SaveInto(c);
                CampusSavesPlugin.Toast(said);
                Leave();
            };
            if (c == null || c.Id == SaveFlow.CampusId) save();
            else Ask("Save over \"" + c.Name + "\"? Its history stays in Campus Saves.", save);
        }

        private void Block(Campus c, Rect r, bool over)
        {
            var e = Event.current;
            var last = c.Newest();
            var hover = over && r.Contains(e.mousePosition);
            GUI.Box(r, GUIContent.none, hover ? _cardHover : _card);
            var pic = new Rect(r.x + 12, r.y + 12, 240, BlockH - 24);
            DrawPicture(c, last, pic);
            var x = pic.xMax + 14;
            var w = r.xMax - x - 62;
            var two = _name.CalcSize(new GUIContent(c.Name)).x > w;
            var down = two ? 8f : 0f;
            if (two) GUI.Label(new Rect(x, r.y + 8, w, 46), c.Name, _nameLong);
            else GUI.Label(new Rect(x, r.y + 14, w, 30), c.Name, _name);
            GUI.Label(new Rect(x, r.y + 48 + down, w, 26), last != null ? InGame(last) : "No saves yet", _text);
            GUI.Label(new Rect(x, r.y + 76 + down, w, 26), last != null ? last.At.ToString("dd/MM/yyyy HH:mm") : "", _dim);
            GUI.Label(new Rect(x, r.y + 104 + down, w, 26), Cards(last, ", ") + c.Vault.Count + (c.Vault.Count == 1 ? " save, " : " saves, ") + Mb(c.Vault.PackSize), _dim);

            // A row scrolled out of the view must not take clicks, so outside the view the buttons are only drawn.
            var lens = new Rect(r.xMax - 56, r.y + 14, 44, 44);
            var bin = new Rect(r.xMax - 56, r.yMax - 58, 44, 44);
            if (!over) GUI.Box(lens, GUIContent.none, _blue);
            else if (GUI.Button(lens, GUIContent.none, _blue)) { Selected = c; Pick = 0; _list = Vector2.zero; }
            GUI.DrawTexture(Inset(lens, 11f), _lens);
            GUI.enabled = !Campuses.Busy && (AtTitle || c.Id != SaveFlow.CampusId);
            if (!over) GUI.Box(bin, GUIContent.none, _red);
            else if (GUI.Button(bin, GUIContent.none, _red)) Ask("Delete the campus \"" + c.Name + "\", its slot and its " + c.Vault.Count + " saves? This cannot be undone.", () => Campuses.Delete(c));
            GUI.DrawTexture(Inset(bin, 11f), _trash);
            GUI.enabled = true;

            if (!hover || lens.Contains(e.mousePosition) || bin.Contains(e.mousePosition) || e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            if (Campuses.Busy) return;
            if (SaveUi != null) SaveHere(c);
            else if (!CanLoad) Campuses.Status = LoadElsewhere;
            else if (last != null) TryLoad(c, last);
        }

        private static string Cards(SaveVersion v, string after) { return v != null && v.Cards > 0 ? v.Cards + (v.Cards == 1 ? " card" : " cards") + after : ""; }

        private static Rect Inset(Rect r, float by) { return new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f); }

        private void DrawPicture(Campus c, SaveVersion v, Rect r)
        {
            Texture2D t = null;
            if (v != null)
            {
                var path = c.Vault.PicturePath(v.Id);
                if (!_pics.TryGetValue(path, out t)) _pics[path] = t = Thumbs.Load(path);
            }
            if (t != null)
            {
                GUI.DrawTexture(r, t, ScaleMode.ScaleAndCrop);
                return;
            }
            GUI.Box(r, GUIContent.none, _empty);
            GUI.Label(r, "No picture", _center);
        }

        private static string InGame(SaveVersion v) { return (v.Day > 0 ? "Day " + v.Day + ", " : "") + v.Name; }

        // ================= the history of one campus =================

        private void DrawHistory(Campus c)
        {
            var list = c.Vault.Snapshot().OrderByDescending(v => v.At).ThenByDescending(v => v.Id).ToList();
            var pick = list.FirstOrDefault(v => v.Id == Pick) ?? list.FirstOrDefault();
            if (pick != null) Pick = pick.Id;
            var mid = DetailW * 0.54f;

            GUI.Label(new Rect(0, 12, DetailW, 36), c.Name, _title);
            if (GUI.Button(new Rect(DetailW - 104, 12, 84, 36), "Back", _btn)) { Selected = null; return; }

            // Left: what is selected.
            var left = 24f;
            var width = mid - left - 18f;
            var pic = new Rect(left, 62, width, width * 9f / 16f);
            DrawPicture(c, pick, pic);
            var y = pic.yMax + 12;
            if (pick != null)
            {
                GUI.Label(new Rect(left, y, width - 130, 42), InGame(pick), _name);
                GUI.enabled = (CanLoad || SaveUi != null) && !Campuses.Busy;
                if (GUI.Button(new Rect(left + width - 120, y, 120, 42), SaveUi != null ? "Save here" : "Play", _green))
                {
                    if (SaveUi != null) SaveHere(c);
                    else TryLoad(c, pick);
                }
                GUI.enabled = true;
                y += 52;
                if (pick.Place != "") Info(left, ref y, width, "Place", pick.Place);
                Info(left, ref y, width, "Saved on", pick.At.ToString("dd/MM/yyyy"));
                Info(left, ref y, width, "Saved at", pick.At.ToString("HH:mm"));
                Info(left, ref y, width, "Type", Kind(pick));
                Info(left, ref y, width, "Size", Mb(pick.Size));
                if (pick.Cards > 0) Info(left, ref y, width, "Cards", pick.Cards.ToString());
                Info(left, ref y, width, "Comes back to", pick.Resume != "" ? "This moment of the day" : "The night menu");

                GUI.enabled = !Campuses.Busy;
                if (GUI.Button(new Rect(left, DetailH - 58, 150, 38), "Export this one", _btn)) Export(c, pick);
                if (GUI.Button(new Rect(left + 160, DetailH - 58, 150, 38), "Delete this one", _btn))
                    Ask("Delete this save (" + InGame(pick) + ") from the history of \"" + c.Name + "\"?", () => { Campuses.Forget(c, pick); Pick = 0; });
                GUI.enabled = true;
            }
            else GUI.Label(new Rect(left, y, width, 30), "No saves yet", _text);

            // Right: every version, newest first.
            var rx = mid + 6f;
            var rw = DetailW - rx - 20f;
            GUI.Label(new Rect(rx, 62, rw, 30), "SAVE HISTORY", _name);
            var view = new Rect(rx, 100, rw, (RowH + 4) * 6.4f);
            var content = new Rect(0, 0, rw - 16, list.Count * (RowH + 4));
            var over = view.Contains(Event.current.mousePosition);
            Wheel(ref _list, view, content.height);
            _list = GUI.BeginScrollView(view, _list, content, false, false);
            for (var i = 0; i < list.Count; i++)
            {
                var r = new Rect(0, i * (RowH + 4), content.width, RowH);
                if (r.yMax < _list.y || r.y > _list.y + view.height) continue;
                Row(c, list[i], r, list[i] == pick, over);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(rx, DetailH - 42, rw, 22), Campuses.Busy ? "Working... " + Campuses.Status : Campuses.Status, _note);
        }

        private void Info(float x, ref float y, float w, string label, string value)
        {
            GUI.Label(new Rect(x, y, 130, 24), label, _dim);
            GUI.Label(new Rect(x + 136, y, w - 136, 24), value, _text);
            y += 25f;
        }

        private static string Kind(SaveVersion v)
        {
            if (v.Kind == "auto") return "Autosave (" + v.Trigger + ")";
            if (v.Kind == "imported") return v.Trigger == "found" ? "Found in the slot" : "Imported";
            return "Saved by hand";
        }

        private void Row(Campus c, SaveVersion v, Rect r, bool on, bool over)
        {
            var e = Event.current;
            var hover = over && r.Contains(e.mousePosition);
            GUI.Box(r, GUIContent.none, on ? _rowOn : hover ? _cardHover : _card);
            GUI.Label(new Rect(r.x + 16, r.y + 12, r.width - 32, 30), (on ? "● " : "") + InGame(v), _name);
            GUI.Label(new Rect(r.x + 16, r.y + 50, r.width - 32, 26), v.At.ToString("dd/MM/yyyy HH:mm") + "   " + Cards(v, "   ") + Kind(v), on ? _text : _dim);
            if (!hover || e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            var twice = _lastRow == v.Id && Time.realtimeSinceStartup - _lastClick < 0.35f;
            Pick = v.Id;
            _lastRow = v.Id;
            _lastClick = Time.realtimeSinceStartup;
            if (!twice || SaveUi != null) return;
            if (!CanLoad) Campuses.Status = LoadElsewhere;
            else if (!Campuses.Busy) TryLoad(c, v);
        }

        // ================= what the buttons do =================

        /// <summary>Loading while walking around drops the day, so that one asks. The Load screen already means it.</summary>
        private static void TryLoad(Campus c, SaveVersion v)
        {
            if (AtTitle || LoadUi != null) Campuses.Status = LoadVersion(c.Id, v.Id);
            else Ask("Load this save (" + InGame(v) + ")? Anything not saved is lost.", () => Campuses.Status = LoadVersion(c.Id, v.Id));
        }

        /// <summary>
        /// Rebuilds a version and loads it. The slot does not change until the next save. A save made during the day comes
        /// back through the start of the Action scene, so from anywhere else the game goes to the title first, on its own.
        /// </summary>
        public static string LoadVersion(string campus, int version)
        {
            var c = Campuses.ById(campus);
            var v = c != null ? c.Vault.Snapshot().FirstOrDefault(x => x.Id == version) : null;
            if (v == null) return "No such version";
            var file = Path.Combine(c.Dir, "restore.tmp");
            Campuses.Status = "Rebuilding the save";
            Campuses.Run(() =>
            {
                c.Vault.Restore(v, file);
                _loadNext = () =>
                {
                    SaveFlow.PendingLoad = file;
                    SaveFlow.PendingCampus = c.Id;
                    SaveFlow.ChosenVersion = v;
                    Campuses.Status = "";
                    if (LoadUi != null && (v.Resume == "" || AtTitle))
                    {
                        // The screen knows what follows a load where it was opened.
                        Traverse.Create(LoadUi).Field("onEnter").GetValue<Action<string>>()(c.Slot);
                        return;
                    }
                    if (AtTitle)
                    {
                        FromTitle(c);
                        return;
                    }
                    _afterTitle = () => FromTitle(c);
                    _me.StartCoroutine(Illusion.Game.Utils.Scene.ReturnTitle(true));
                };
            });
            return "Loading";
        }

        private static void FromTitle(Campus c)
        {
            Game.SaveFileName = c.Slot;
            Singleton<Game>.Instance.Load();
            Singleton<Scene>.Instance.LoadReserve(new Scene.Data { levelName = "Action", fadeType = Scene.Data.FadeType.In }, true);
        }

        internal static void Export(Campus c, SaveVersion v)
        {
            Campuses.Run(() =>
            {
                var slot = Campuses.FreeSlot();
                if (slot == null) { Campuses.Status = "No free slot"; return; }
                Campuses.ExportTo(c, v, slot);
                Campuses.Status = "Exported to " + slot;
            });
        }

        private sealed class Job { public Campus C; public SaveVersion V; }

        /// <summary>The game only comes back at night, so the saves of one day are all the same night to it.</summary>
        private static string DayOf(SaveVersion v) { return v.Day > 0 ? "day " + v.Day : v.At.ToString("yyyyMMdd") + " " + v.Name.Split(',')[0]; }

        /// <summary>The newest save of each day, when it is not in a slot already, newest first in each campus.</summary>
        private static List<Job> ExportPlan()
        {
            var jobs = new List<Job>();
            foreach (var c in Campuses.Snapshot())
                foreach (var day in c.Vault.Snapshot().GroupBy(DayOf))
                {
                    var v = day.OrderByDescending(x => x.At).ThenByDescending(x => x.Id).First();
                    if (!c.InSlot(v)) jobs.Add(new Job { C = c, V = v });
                }
            return jobs.OrderByDescending(j => j.V.At).ToList();
        }

        private static void OfferExport()
        {
            var plan = ExportPlan();
            if (plan.Count == 0) { Campuses.Status = "All saves are in slots already"; return; }
            Ask("Export " + plan.Count + (plan.Count == 1 ? " save" : " saves") + " to the game's list, one for each day? They take " + Mb(plan.Sum(j => j.V.Size)) + ".", ExportAll);
        }

        /// <summary>The slots Free space would remove: everything imported but the head of each campus.</summary>
        private static List<string> FreePlan()
        {
            var paths = new List<string>();
            foreach (var c in Campuses.Snapshot())
                foreach (var f in c.Imported.Where(x => x != c.Slot))
                {
                    var path = Path.Combine(Campuses.GameDir, f);
                    if (File.Exists(path) && c.Vault.Snapshot().Any(x => x.Kind == "imported" && x.Slot == f)) paths.Add(path);
                }
            return paths;
        }

        private static void OfferFree()
        {
            var plan = FreePlan();
            if (plan.Count == 0) { Campuses.Status = "Nothing to free: no repeated saves in the game's list"; return; }
            var size = plan.Sum(p => new FileInfo(p).Length);
            Ask("Delete " + plan.Count + " repeated " + (plan.Count == 1 ? "save" : "saves") + " from the game's list and free " + Mb(size) + "? They stay in Campus Saves.", FreeSpace);
        }

        private static long FreeDisk()
        {
            try { return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(Campuses.GameDir))).AvailableFreeSpace; }
            catch { return long.MaxValue; }
        }

        /// <summary>Every version back as a plain save of the game, with its own date, in the free slots.</summary>
        internal static void ExportAll()
        {
            Campuses.Run(() =>
            {
                var plan = ExportPlan();
                var need = plan.Sum(j => j.V.Size);
                if (need > FreeDisk())
                {
                    Campuses.Status = "Not enough disk space: the saves need " + Mb(need);
                    return;
                }
                var n = 0;
                foreach (var j in plan)
                {
                    var slot = Campuses.FreeSlot();
                    if (slot == null)
                    {
                        Campuses.Status = "Exported " + n + " of " + plan.Count + ": the game has no more free slots";
                        return;
                    }
                    Campuses.Status = "Exporting " + (n + 1) + " of " + plan.Count;
                    Campuses.ExportTo(j.C, j.V, slot);
                    n++;
                }
                Campuses.Status = "Exported " + n + " saves. The mod can be removed now";
            });
        }

        /// <summary>First entry: groups the slots by campus and copies them in. The files stay where they are.</summary>
        private static void ImportAll()
        {
            Campuses.Run(() =>
            {
                var known = Campuses.Snapshot();
                var slots = Directory.GetFiles(Campuses.GameDir, "*.dat").Select(Path.GetFileName).Where(f => known.All(c => !c.Imported.Contains(f) && c.Slot != f && c.Vault.Snapshot().All(v => v.Slot != f))).ToList();
                var groups = new Dictionary<string, List<string>>();
                var heads = new Dictionary<string, SaveHeader>();
                foreach (var f in slots)
                {
                    var h = SaveHeader.Read(Path.Combine(Campuses.GameDir, f));
                    if (h == null) continue;
                    var k = h.Name + "|" + h.Emblem;
                    if (!groups.ContainsKey(k)) { groups[k] = new List<string>(); heads[k] = h; }
                    groups[k].Add(f);
                }
                foreach (var g in groups)
                {
                    var files = g.Value.OrderBy(f => File.GetLastWriteTimeUtc(Path.Combine(Campuses.GameDir, f))).ToList();
                    var c = Campuses.ByName(heads[g.Key].Name, heads[g.Key].Emblem) ?? Campuses.Create(heads[g.Key].Name, heads[g.Key].Emblem, files.Last());
                    foreach (var f in files)
                    {
                        Campuses.Status = "Importing " + c.Name + ": " + f;
                        Campuses.Import(c, f);
                    }
                    c.Slot = files.Last();
                    c.Write();
                }
                Campuses.Status = "Imported " + slots.Count + " saves into " + groups.Count + " campuses";
                _offerFree = true;
            });
        }

        /// <summary>Removes the imported slots that are not the head, only after each one rebuilds to the same hash.</summary>
        internal static void FreeSpace()
        {
            Campuses.Run(() =>
            {
                long freed = 0;
                foreach (var c in Campuses.Snapshot())
                    foreach (var f in c.Imported.Where(x => x != c.Slot).ToList())
                    {
                        var path = Path.Combine(Campuses.GameDir, f);
                        if (!File.Exists(path)) continue;
                        var v = c.Vault.Snapshot().FirstOrDefault(x => x.Kind == "imported" && x.Slot == f);
                        if (v == null || Vault.HashFile(path) != v.Sha1) continue;
                        var check = Path.Combine(c.Dir, "check.tmp");
                        c.Vault.Restore(v, check);
                        File.Delete(check);
                        freed += new FileInfo(path).Length;
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                    }
                Campuses.Status = "Freed " + Mb(freed);
            });
        }
    }
}
