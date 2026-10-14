using System;
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
    /// one campus. Nothing here changes a file until a button is pressed, and what removes something asks for a second click.
    /// </summary>
    internal sealed class SavesPanel : MonoBehaviour
    {
        private const float MainW = 1180f, MainH = 640f, DetailW = 1040f, DetailH = 780f;
        private const float BlockH = 160f, Gap = 12f, RowH = 92f;

        public static bool Shown;
        public static Campus Selected;     // the campus on the history page; null on the blocks page
        public static int Pick;            // the version chosen in the history
        public static LoadScene LoadUi;    // the game's Load screen under the panel, which loads for it on the title and at night

        private static Action _loadNext;
        private Vector2 _grid, _list;
        private string _sure = "";
        private float _lastClick;
        private int _lastRow;
        private readonly List<BaseInputModule> _held = new List<BaseInputModule>();
        private readonly Dictionary<string, Texture2D> _pics = new Dictionary<string, Texture2D>();

        private GUIStyle _panel, _card, _cardHover, _rowOn, _title, _name, _nameLong, _text, _dim, _note, _center;
        private GUIStyle _btn, _blue, _red, _green, _gold, _empty;
        private Texture2D _lens, _trash, _shade;

        public static void OpenOver(LoadScene screen)
        {
            LoadUi = screen;
            Selected = null;
            Campuses.Status = "";
            Shown = true;
            _over = true;
        }

        /// <summary>Closes the panel and, when it stands over the Load screen, that screen too.</summary>
        private static void Leave()
        {
            Shown = false;
            Selected = null;
            if (LoadUi != null) LoadUi.onReturn();
        }

        private static bool _over;

        private void Update()
        {
            try { if (CampusSavesPlugin.PanelKey.Value.IsDown()) Shown = !Shown; }
            catch { }

            // The right button leaves the game's Load screen on its own.
            if (_over && LoadUi == null)
            {
                _over = false;
                Shown = false;
                Selected = null;
            }

            HoldClicks();
            if (!Shown && _pics.Count > 0) ClearPictures();
        }

        /// <summary>
        /// Clicks on the panel must not reach the game's buttons behind it. Scenes added on top bring their own event systems,
        /// so every input module is held, and the Load screen, which would still take a click, stops seeing the mouse.
        /// </summary>
        private void HoldClicks()
        {
            var list = LoadUi != null ? Traverse.Create(LoadUi).Field("_canvasGroup").GetValue<CanvasGroup>() : null;
            if (list != null) list.blocksRaycasts = !Shown;
            if (Shown)
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

        private static bool CanLoad { get { return LoadUi != null || AtTitle; } }

        private const string LoadElsewhere = "Saves load from the game's Load screen";

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
            _gold = Button(new Color(0.76f, 0.52f, 0.12f), 16);
            _lens = Art.Lens();
            _trash = Art.Trash();
        }

        private void OnGUI()
        {
            if (!Shown) return;
            Styles();
            GUI.depth = -50;
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
            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.ScrollWheel) e.Use();
        }

        // ================= the blocks =================

        private void DrawBlocks()
        {
            GUI.Label(new Rect(0, 12, MainW, 36), "Campus Saves", _title);
            var view = new Rect(20, 56, MainW - 40, BlockH * 3 + Gap * 2);
            var list = Campuses.Snapshot();
            var rows = (list.Count + 1) / 2;
            var content = new Rect(0, 0, view.width - 18, rows * (BlockH + Gap) - Gap);
            var cell = (content.width - Gap) / 2f;
            var over = view.Contains(Event.current.mousePosition);
            _grid = GUI.BeginScrollView(view, _grid, content, false, false);
            for (var i = 0; i < list.Count; i++)
            {
                var top = (i / 2) * (BlockH + Gap);
                if (top + BlockH < _grid.y || top > _grid.y + view.height) continue;
                Block(list[i], new Rect((i % 2) * (cell + Gap), top, cell, BlockH), over);
            }
            GUI.EndScrollView();
            if (list.Count == 0) GUI.Label(new Rect(0, 240, MainW, 30), "No campuses yet. Import brings your saves in, and your files stay as they are.", _center);

            var y = MainH - 62;
            GUI.enabled = !Campuses.Busy;
            if (GUI.Button(new Rect(20, y, 100, 42), "Import", _btn)) ImportAll();
            if (Sure("free", "Free space", new Rect(128, y, 120, 42), _btn)) FreeSpace();
            var plan = _sure == "export" ? ExportPlan() : null;
            if (Sure("export", plan != null ? Describe(plan) : "EXPORT SAVES", new Rect(MainW / 2f - 170, y, 340, 42), _gold, plan == null || plan.Count > 0)) ExportAll();
            GUI.enabled = true;
            if (LoadUi != null && GUI.Button(new Rect(256, y, 120, 42), "Game's list", _btn)) Shown = false;
            if (GUI.Button(new Rect(MainW - 130, y, 110, 42), "Close", _btn)) Leave();
            GUI.Label(new Rect(20, MainH - 19, MainW - 40, 18), Campuses.Busy ? "Working... " + Campuses.Status : Campuses.Status, _note);
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
            GUI.Label(new Rect(x, r.y + 104 + down, w, 26), c.Vault.Count + (c.Vault.Count == 1 ? " save, " : " saves, ") + Mb(c.Vault.PackSize), _dim);

            // A row scrolled out of the view must not take clicks, so outside the view the buttons are only drawn.
            var lens = new Rect(r.xMax - 56, r.y + 14, 44, 44);
            var bin = new Rect(r.xMax - 56, r.yMax - 58, 44, 44);
            if (!over) GUI.Box(lens, GUIContent.none, _blue);
            else if (GUI.Button(lens, GUIContent.none, _blue)) { Selected = c; Pick = 0; _list = Vector2.zero; }
            GUI.DrawTexture(Inset(lens, 11f), _lens);
            var key = "campus" + c.Id;
            GUI.enabled = !Campuses.Busy && (AtTitle || c.Id != SaveFlow.CampusId);
            if (!over) GUI.Box(bin, GUIContent.none, _red);
            else if (Sure(key, "", bin, _red)) Campuses.Delete(c);
            if (_sure != key) GUI.DrawTexture(Inset(bin, 11f), _trash);
            GUI.enabled = true;

            if (!hover || lens.Contains(e.mousePosition) || bin.Contains(e.mousePosition) || e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            if (!CanLoad) Campuses.Status = LoadElsewhere;
            else if (last != null && !Campuses.Busy) Campuses.Status = LoadVersion(c.Id, last.Id);
        }

        private static Rect Inset(Rect r, float by) { return new Rect(r.x + by, r.y + by, r.width - by * 2f, r.height - by * 2f); }

        /// <summary>The second click does the job; the first one only arms it.</summary>
        private bool Sure(string key, string label, Rect r, GUIStyle style, bool can = true)
        {
            var text = _sure == key && label.Length < 16 ? "Sure?" : label;
            if (!GUI.Button(r, text, style)) return false;
            if (_sure == key)
            {
                _sure = "";
                return can;
            }
            _sure = key;
            return false;
        }

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
                GUI.enabled = CanLoad && !Campuses.Busy;
                if (GUI.Button(new Rect(left + width - 120, y, 120, 42), "Play", _green)) Campuses.Status = LoadVersion(c.Id, pick.Id);
                GUI.enabled = true;
                y += 52;
                if (pick.Place != "") Info(left, ref y, width, "Place", pick.Place);
                Info(left, ref y, width, "Saved on", pick.At.ToString("dd/MM/yyyy"));
                Info(left, ref y, width, "Saved at", pick.At.ToString("HH:mm"));
                Info(left, ref y, width, "Type", Kind(pick));
                Info(left, ref y, width, "Size", Mb(pick.Size));
                Info(left, ref y, width, "Comes back to", pick.Resume != "" ? "This moment of the day" : "The night menu");

                GUI.enabled = !Campuses.Busy;
                if (GUI.Button(new Rect(left, DetailH - 58, 150, 38), "Export this one", _btn)) Export(c, pick);
                if (Sure("del" + c.Id + pick.Id, "Delete this one", new Rect(left + 160, DetailH - 58, 150, 38), _btn)) { Campuses.Forget(c, pick); Pick = 0; }
                GUI.enabled = true;
            }
            else GUI.Label(new Rect(left, y, width, 30), "No saves yet", _text);

            // Right: every version, newest first.
            var rx = mid + 6f;
            var rw = DetailW - rx - 20f;
            GUI.Label(new Rect(rx, 62, rw, 30), "SAVE HISTORY", _name);
            var view = new Rect(rx, 100, rw, (RowH + 4) * 6);
            var content = new Rect(0, 0, rw - 18, list.Count * (RowH + 4));
            var over = view.Contains(Event.current.mousePosition);
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
            GUI.Label(new Rect(r.x + 16, r.y + 50, r.width - 32, 26), v.At.ToString("dd/MM/yyyy HH:mm") + "   " + Kind(v), on ? _text : _dim);
            if (!hover || e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            var twice = _lastRow == v.Id && Time.realtimeSinceStartup - _lastClick < 0.35f;
            Pick = v.Id;
            _lastRow = v.Id;
            _lastClick = Time.realtimeSinceStartup;
            if (!twice) return;
            if (!CanLoad) Campuses.Status = LoadElsewhere;
            else if (!Campuses.Busy) Campuses.Status = LoadVersion(c.Id, v.Id);
        }

        // ================= what the buttons do =================

        /// <summary>Rebuilds a version and loads it from the title screen. The slot does not change until the next save.</summary>
        public static string LoadVersion(string campus, int version)
        {
            var c = Campuses.ById(campus);
            var v = c != null ? c.Vault.Snapshot().FirstOrDefault(x => x.Id == version) : null;
            if (v == null) return "No such version";
            if (v.Resume != "" && !AtTitle) return "A save from the middle of a day loads from the title screen";
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
                    if (LoadUi != null)
                    {
                        // The screen knows what follows a load where it was opened.
                        Traverse.Create(LoadUi).Field("onEnter").GetValue<Action<string>>()(c.Slot);
                        return;
                    }
                    Game.SaveFileName = c.Slot;
                    Singleton<Game>.Instance.Load();
                    Singleton<Scene>.Instance.LoadReserve(new Scene.Data { levelName = "Action", fadeType = Scene.Data.FadeType.In }, true);
                };
            });
            return "Loading";
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

        /// <summary>Everything not already in a slot, newest first in each campus.</summary>
        private static List<Job> ExportPlan()
        {
            var jobs = new List<Job>();
            foreach (var c in Campuses.Snapshot())
                foreach (var v in c.Vault.Snapshot().OrderByDescending(x => x.At))
                    if (!c.InSlot(v)) jobs.Add(new Job { C = c, V = v });
            return jobs;
        }

        private static string Describe(List<Job> plan)
        {
            if (plan.Count == 0) return "All saves are in slots already";
            return "Export " + plan.Count + " saves, " + Mb(plan.Sum(j => j.V.Size)) + ": sure?";
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
