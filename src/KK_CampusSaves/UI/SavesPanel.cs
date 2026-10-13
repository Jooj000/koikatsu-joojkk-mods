using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Manager;
using UnityEngine;

namespace Joojkk.CampusSaves
{
    /// <summary>
    /// The campuses and their versions, on the title screen and from the key. Nothing here changes a file until a button is
    /// pressed, and the first import leaves the game's saves as they are.
    /// </summary>
    internal sealed class SavesPanel : MonoBehaviour
    {
        public static bool Shown;
        private Rect _rect = new Rect(0, 0, 860, 640);
        private Vector2 _scroll;
        public static Campus Selected;
        private GUIStyle _window, _button, _card, _row, _note;

        private static Texture2D Solid(float r, float g, float b, float a = 1f)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, new Color(r, g, b, a));
            t.Apply();
            return t;
        }

        private void Styles()
        {
            if (_window != null) return;
            var panel = Solid(0.10f, 0.11f, 0.16f);
            _window = new GUIStyle(GUI.skin.window) { fontSize = 19, fontStyle = FontStyle.Bold, padding = new RectOffset(16, 16, 40, 14) };
            _window.normal.background = _window.onNormal.background = panel;
            _window.normal.textColor = _window.onNormal.textColor = Color.white;
            _button = new GUIStyle(GUI.skin.button) { fontSize = 15, fixedHeight = 32f };
            _button.normal.background = Solid(0.24f, 0.27f, 0.38f);
            _button.hover.background = _button.active.background = Solid(0.34f, 0.38f, 0.54f);
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = Color.white;
            _card = new GUIStyle(_button) { fontSize = 16, fixedHeight = 44f, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 4, 4), margin = new RectOffset(0, 0, 4, 4) };
            _card.normal.background = Solid(0.17f, 0.19f, 0.27f);
            _row = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft, fixedHeight = 32f };
            _row.normal.textColor = new Color(0.9f, 0.91f, 0.95f);
            _note = new GUIStyle(_row) { fontSize = 14, fixedHeight = 24f };
            _note.normal.textColor = new Color(1f, 0.82f, 0.45f);
        }
        private string _sure = "";

        private void Update()
        {
            try { if (CampusSavesPlugin.PanelKey.Value.IsDown()) Shown = !Shown; }
            catch { }
        }

        private static bool AtTitle { get { return Singleton<Scene>.IsInstance() && Singleton<Scene>.Instance.NowSceneNames.Contains("Title"); } }

        private void OnGUI()
        {
            if (!Shown) return;
            Styles();
            if (_rect.x == 0f) _rect.center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _rect = GUI.Window(0x4A4F4F, _rect, Draw, "Campus Saves", _window);
        }

        private static string Mb(long b) { return b >= 1000000000 ? (b / 1e9).ToString("0.0") + " GB" : (b / 1e6).ToString("0") + " MB"; }

        private void Draw(int id)
        {
            GUILayout.Label(Campuses.Busy ? "Working... " + Campuses.Status : Campuses.Status, _note);
            _scroll = GUILayout.BeginScrollView(_scroll);
            lock (Campuses.All)
                foreach (var c in Campuses.All.ToList())
                {
                    var last = c.Vault.Versions.LastOrDefault();
                    if (GUILayout.Button(c.Name + "   " + c.Vault.Versions.Count + " versions, " + Mb(c.Vault.PackSize) + (last != null ? ", " + last.At.ToString("yyyy-MM-dd HH:mm") : ""), _card)) Selected = Selected == c ? null : c;
                    if (Selected == c) Versions(c);
                }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUI.enabled = !Campuses.Busy;
            if (GUILayout.Button("Import", _button)) ImportAll();
            if (Sure("free", "Free space")) FreeSpace();
            if (GUILayout.Button("Normalize", _button)) Normalize();
            GUI.enabled = true;
            if (GUILayout.Button("Close", _button)) Shown = false;
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        private void Versions(Campus c)
        {
            foreach (var v in Enumerable.Reverse(c.Vault.Versions).ToList())
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label((v.Kind == "auto" ? "auto  " : v.Kind == "imported" ? "old   " : "saved ") + v.At.ToString("MM-dd HH:mm") + "  " + v.Name + (v.Resume != "" ? " (day)" : ""), _row);
                GUI.enabled = !Campuses.Busy && AtTitle;
                if (GUILayout.Button("Load", _button, GUILayout.Width(70))) Campuses.Status = LoadVersion(c.Id, v.Id);
                GUI.enabled = !Campuses.Busy;
                if (GUILayout.Button("Export", _button, GUILayout.Width(70))) Export(c, v);
                if (Sure("del" + c.Id + v.Id, "Delete", 70)) c.Vault.Remove(v);
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>Anything that removes something asks for a second click.</summary>
        private bool Sure(string key, string label, float width = 0f)
        {
            var text = _sure == key ? "Sure?" : label;
            var hit = width > 0f ? GUILayout.Button(text, _button, GUILayout.Width(width)) : GUILayout.Button(text, _button);
            if (!hit) return false;
            if (_sure == key) { _sure = ""; return true; }
            _sure = key;
            return false;
        }

        /// <summary>Rebuilds a version and loads it from the title screen. The slot does not change until the next save.</summary>
        public static string LoadVersion(string campus, int version)
        {
            var c = Campuses.ById(campus);
            var v = c != null ? c.Vault.Versions.FirstOrDefault(x => x.Id == version) : null;
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
                    Game.SaveFileName = c.Slot;
                    Singleton<Game>.Instance.Load();
                    Singleton<Scene>.Instance.LoadReserve(new Scene.Data { levelName = "Action", fadeType = Scene.Data.FadeType.In }, true);
                    Campuses.Status = "";
                };
            });
            return "Loading";
        }

        private static Action _loadNext;

        private void LateUpdate()
        {
            var a = _loadNext;
            if (a == null) return;
            _loadNext = null;
            Shown = false;
            try { a(); }
            catch (Exception e) { Campuses.Status = "Failed: " + e.Message; CampusSavesPlugin.Log.LogError(e); }
        }

        internal static void Export(Campus c, SaveVersion v)
        {
            var slot = Campuses.FreeSlot();
            if (slot == null) { Campuses.Status = "No free slot"; return; }
            Campuses.Run(() =>
            {
                c.Vault.Restore(v, Path.Combine(Campuses.GameDir, slot));
                Campuses.Status = "Exported to " + slot;
            });
        }

        /// <summary>First entry: groups the slots by campus and copies them in. The files stay where they are.</summary>
        private static void ImportAll()
        {
            Campuses.Run(() =>
            {
                var slots = Directory.GetFiles(Campuses.GameDir, "*.dat").Select(Path.GetFileName).Where(f => Campuses.All.All(c => !c.Imported.Contains(f) && c.Slot != f)).ToList();
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
                    var c = Campuses.All.FirstOrDefault(x => x.Name == heads[g.Key].Name && x.Emblem == heads[g.Key].Emblem) ?? Campuses.Create(heads[g.Key].Name, heads[g.Key].Emblem, files.Last());
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
                foreach (var c in Campuses.All.ToList())
                    foreach (var f in c.Imported.Where(x => x != c.Slot).ToList())
                    {
                        var path = Path.Combine(Campuses.GameDir, f);
                        if (!File.Exists(path)) continue;
                        var v = c.Vault.Versions.FirstOrDefault(x => x.Kind == "imported" && x.Name == f);
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

        /// <summary>Back to plain saves: hand and imported versions return to free slots. The vault stays until deleted by hand.</summary>
        private static void Normalize()
        {
            Campuses.Run(() =>
            {
                var n = 0;
                foreach (var c in Campuses.All.ToList())
                    foreach (var v in c.Vault.Versions.Where(x => x.Kind != "auto").ToList())
                    {
                        var slot = Campuses.FreeSlot();
                        if (slot == null) { Campuses.Status = "Stopped: no free slot"; return; }
                        c.Vault.Restore(v, Path.Combine(Campuses.GameDir, slot));
                        n++;
                    }
                Campuses.Status = "Normalized " + n + " saves";
            });
        }
    }
}
