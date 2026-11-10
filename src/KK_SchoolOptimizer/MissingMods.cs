using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using ExtensibleSaveFormat;
using HarmonyLib;
using KKAPI.Utilities;
using Sideloader.AutoResolver;
using UnityEngine;

namespace Joojkk.SchoolOptimizer
{
    /// <summary>
    /// Missing zipmods: every card keeps, for each Sideloader item it wears, the mod's GUID, author and name
    /// (UniversalAutoResolver's "info" list). This reads that list from the girls of the loaded save and from the cards in
    /// UserData/chara, compares it with the zipmods Sideloader loaded, and shows what is missing and who uses it (window
    /// opened from the plugin settings, with search and sort). The same list goes to UserData/missing_zipmods.txt as [
    /// zipmod ] [ creator ] > "name", "name" | "item", "item".
    /// </summary>
    internal static class MissingMods
    {
        internal class Entry
        {
            public string Guid, Name, Author, Website;
            public bool Outdated;
            public readonly SortedDictionary<string, bool> Chars = new SortedDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            public readonly SortedDictionary<string, SortedDictionary<int, bool>> Items = new SortedDictionary<string, SortedDictionary<int, bool>>(StringComparer.OrdinalIgnoreCase);
            /// <summary>"clothes Top 51312 (outfits 2, 3, 4)": one entry per item, with the outfits that wear it.</summary>
            public IEnumerable<string> ItemLabels
            {
                get
                {
                    foreach (var kv in Items)
                    {
                        var o = kv.Value.Keys.Where(x => x >= 0).Select(x => x.ToString()).ToArray();
                        yield return kv.Key + (o.Length == 0 ? "" : o.Length == 1 ? " (outfit " + o[0] + ")" : " (outfits " + string.Join(", ", o) + ")");
                    }
                }
            }
            public string Title { get { return string.IsNullOrEmpty(Name) ? Guid : Name; } }
            public string Creator { get { return string.IsNullOrEmpty(Author) || Regex.IsMatch(Author, @"^[\d.\s]+$") ? GuessAuthor(Guid) : Author; } }
        }

        public static string TxtPath { get { return Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), "missing_zipmods.txt"); } }

        private static List<Entry> _list = new List<Entry>();
        private static bool _show, _busy;
        private static string _status = "Not scanned yet.", _search = "";
        private static int _sort; // 0 zipmod, 1 creator, 2 characters, 3 items
        private static bool _desc;
        private static Vector2 _scroll;
        private static Rect _win = new Rect(80, 60, 900, 620);
        private static int _scanned;
        public static Vector2 ButtonAt;
        public static int ButtonFrame = -1;
        public static bool Showing { get { return _show; } }
        public static bool Busy { get { return _busy; } }
        public static string Status { get { return _status; } }

        /// <summary>The plugin settings row: a button that opens the window (and scans the first time).</summary>
        public static bool PressNext;

        public static void DrawButton(ConfigEntryBase entry)
        {
            var clicked = GUILayout.Button(_busy ? "Scanning..." : "Show missing zipmods", GUILayout.ExpandWidth(true));
            if (PressNext && Event.current.type == EventType.Repaint) { PressNext = false; clicked = true; }
            if (Event.current.type == EventType.Repaint)
            {
                var r = GUILayoutUtility.GetLastRect();
                ButtonAt = GUIUtility.GUIToScreenPoint(r.center);
                ButtonFrame = Time.frameCount;
            }
            if (clicked)
            {
                _show = true;
                if (_scanned == 0 && !_busy) Scan();
            }
        }

        public static void Scan()
        {
            if (_busy) return;
            SchoolOptimizerPlugin.Instance.StartCoroutine(ScanRoutine().PreventFromCrashing());
        }

        private static IEnumerator ScanRoutine()
        {
            _busy = true;
            _status = "Scanning...";
            var found = new Dictionary<string, Entry>();
            var loaded = new HashSet<string>();
            foreach (var r in UniversalAutoResolver.LoadedResolutionInfo)
                if (r != null && r.GUID != null) loaded.Add(r.GUID + "|" + r.Slot + "|" + r.Property);
            var deser = AccessTools.Method(typeof(ResolveInfo), "Deserialize", new[] { typeof(byte[]) });
            int chars = 0;

            // The save's students and the player (already loaded, nothing to read from disk).
            var save = Singleton<Manager.Game>.IsInstance() ? Singleton<Manager.Game>.Instance.saveData : null;
            if (save != null)
            {
                foreach (var h in save.heroineList)
                    if (h != null && h.charFile != null) { Collect(h.charFile, h.charFile.parameter.fullname + " (save)", loaded, deser, found); chars++; }
                if (save.player != null && save.player.charFile != null) { Collect(save.player.charFile, save.player.charFile.parameter.fullname + " (player)", loaded, deser, found); chars++; }
            }
            yield return null;

            // The cards in UserData/chara (the missing-mod warning is muted meanwhile, or every card would pop it on screen).
            var warn = AccessTools.Property(typeof(Sideloader.Sideloader), "MissingModWarning");
            var warnEntry = warn != null ? warn.GetValue(null, null) as ConfigEntry<bool> : null;
            var oldWarn = warnEntry != null && warnEntry.Value;
            if (warnEntry != null) warnEntry.Value = false;
            try
            {
                var root = Path.Combine(Path.Combine(Paths.GameRootPath, "UserData"), "chara");
                var files = new List<KeyValuePair<string, byte>>();
                foreach (var sub in new[] { "female", "male" })
                {
                    var dir = Path.Combine(root, sub);
                    if (!Directory.Exists(dir)) continue;
                    foreach (var f in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
                        files.Add(new KeyValuePair<string, byte>(f, (byte)(sub == "male" ? 0 : 1)));
                }
                for (int i = 0; i < files.Count; i++)
                {
                    try
                    {
                        var cf = new ChaFileControl();
                        if (cf.LoadCharaFile(files[i].Key, files[i].Value, true, true))
                        {
                            Collect(cf, cf.parameter.fullname + " (" + Path.GetFileName(files[i].Key) + ")", loaded, deser, found);
                            chars++;
                        }
                    }
                    catch (Exception e) { SchoolOptimizerPlugin.Log.LogDebug("Missing zipmods, card " + files[i].Key + ": " + e.Message); }
                    _status = "Scanning cards " + (i + 1) + "/" + files.Count + "...";
                    if (i % 4 == 3) yield return null;
                }
            }
            finally { if (warnEntry != null) warnEntry.Value = oldWarn; }

            _list = found.Values.ToList();
            _scanned++;
            WriteTxt();
            _status = _list.Count + " missing or outdated zipmods in " + chars + " characters. Saved to UserData/missing_zipmods.txt.";
            SchoolOptimizerPlugin.Log.LogInfo("Missing zipmods: " + _status);
            _busy = false;
        }

        private static readonly Regex Prefix = new Regex(@"^(outfit\d+\.)?(accessory\d+\.)?");

        private static void Collect(ChaFile file, string who, HashSet<string> loaded, System.Reflection.MethodInfo deser, Dictionary<string, Entry> found)
        {
            var data = ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtID) ?? ExtendedSave.GetExtendedDataById(file, UniversalAutoResolver.UARExtIDOld);
            object raw;
            if (data == null || data.data == null || !data.data.TryGetValue("info", out raw) || raw == null) return;
            var blobs = raw as IEnumerable;
            if (blobs == null) return;
            foreach (var o in blobs)
            {
                var bytes = o as byte[];
                if (bytes == null) continue;
                ResolveInfo ri;
                try { ri = deser.Invoke(null, new object[] { bytes }) as ResolveInfo; }
                catch { continue; }
                if (ri == null || string.IsNullOrEmpty(ri.GUID)) continue;
                var prop = ri.Property ?? "";
                var bare = Prefix.Replace(prop, "");
                if (loaded.Contains(ri.GUID + "|" + ri.Slot + "|" + bare)) continue;
                var outdated = Sideloader.Sideloader.GetManifest(ri.GUID) != null;
                Entry e;
                if (!found.TryGetValue(ri.GUID, out e))
                {
                    e = new Entry { Guid = ri.GUID, Name = ri.Name, Author = ri.Author, Website = ri.Website, Outdated = outdated };
                    found[ri.GUID] = e;
                }
                if (string.IsNullOrEmpty(e.Author) && !string.IsNullOrEmpty(ri.Author)) e.Author = ri.Author;
                if (string.IsNullOrEmpty(e.Name) && !string.IsNullOrEmpty(ri.Name)) e.Name = ri.Name;
                e.Chars[who.Trim()] = true;
                int outfit;
                var item = ItemName(prop, ri.Slot, out outfit);
                SortedDictionary<int, bool> outfits;
                if (!e.Items.TryGetValue(item, out outfits)) e.Items[item] = outfits = new SortedDictionary<int, bool>();
                outfits[outfit] = true;
            }
        }

        /// <summary>"outfit2.accessory5.ChaFileAccessory.PartsInfo.id" + 1234 → "accessory 1234", outfit 2 (-1 = not an outfit piece).</summary>
        private static string ItemName(string prop, int slot, out int outfit)
        {
            var m = Regex.Match(prop, @"^(?:outfit(\d+)\.)?(?:accessory\d+\.)?(.*)$");
            var kind = m.Groups[2].Value;
            if (kind.StartsWith("ChaFileAccessory")) kind = "accessory";
            else
            {
                var dot = kind.LastIndexOf('.');
                if (dot >= 0) kind = kind.Substring(dot + 1);
                kind = Regex.Replace(kind, "^(Clothes|Hair|Pattern)", m2 => m2.Value.ToLowerInvariant() + " ");
            }
            outfit = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : -1;
            return kind.Trim() + " " + slot;
        }

        /// <summary>Card without the author: guess it from GUIDs such as "com.mikke.clothes" or "[Mikke] clothes".</summary>
        private static string GuessAuthor(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return "unknown";
            // (some cards carry a version number where the author goes, e.g. "1.1")
            var b = Regex.Match(guid, @"^\[([^\]]+)\]");
            if (b.Success) return b.Groups[1].Value + "?";
            var parts = guid.Split('.');
            if (parts.Length >= 3 && (parts[0] == "com" || parts[0] == "net" || parts[0] == "org" || parts[0] == "jp" || parts[0] == "io")) return parts[1] + "?";
            return "unknown";
        }

        private static IEnumerable<Entry> View()
        {
            IEnumerable<Entry> q = _list;
            if (!string.IsNullOrEmpty(_search))
            {
                var s = _search.Trim();
                q = q.Where(e => Has(e.Title, s) || Has(e.Guid, s) || Has(e.Creator, s) || e.Chars.Keys.Any(c => Has(c, s)) || e.Items.Keys.Any(c => Has(c, s)));
            }
            Func<Entry, IComparable> key;
            switch (_sort)
            {
                case 1: key = e => e.Creator.ToLowerInvariant(); break;
                case 2: key = e => e.Chars.Count; break;
                case 3: key = e => e.Items.Count; break;
                default: key = e => e.Title.ToLowerInvariant(); break;
            }
            return _desc ? q.OrderByDescending(key) : q.OrderBy(key);
        }

        private static bool Has(string a, string s) { return a != null && a.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0; }

        private static string Line(Entry e)
        {
            return "[ " + e.Title + (e.Outdated ? " (outdated)" : "") + " ] [ " + e.Creator + " ] > " +
                   string.Join(", ", e.Chars.Keys.Select(c => "\"" + c + "\"").ToArray()) + " | " +
                   string.Join(", ", e.ItemLabels.Select(c => "\"" + c + "\"").ToArray());
        }

        private static void WriteTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Missing zipmods (School Optimizer, " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + "): " + _list.Count + " mods.");
            sb.AppendLine("# [ zipmod ] [ creator ] > \"character\", ... | \"item\", ...   (creator with ? = guessed from the GUID)");
            foreach (var e in _list.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)) sb.AppendLine(Line(e));
            File.WriteAllText(TxtPath, sb.ToString(), new UTF8Encoding(false));
        }

        public static void OnGUI()
        {
            if (!_show) return;
            _win = GUILayout.Window(0x6A6F6F6A, _win, Window, "Missing zipmods");
        }

        private static readonly string[] SortNames = { "Zipmod", "Creator", "Characters", "Items" };

        private static void Window(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(50));
            _search = GUILayout.TextField(_search, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Clear", GUILayout.Width(55))) _search = "";
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sort by", GUILayout.Width(50));
            for (int i = 0; i < SortNames.Length; i++)
            {
                var label = SortNames[i] + (_sort == i ? (_desc ? " ▼" : " ▲") : "");
                if (GUILayout.Toggle(_sort == i, label, GUI.skin.button, GUILayout.Width(110)))
                {
                    if (_sort != i) { _sort = i; _desc = i >= 2; }
                }
                else if (_sort == i) _desc = !_desc;
            }
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy;
            if (GUILayout.Button("Rescan", GUILayout.Width(70))) Scan();
            if (GUILayout.Button("Open txt", GUILayout.Width(75)) && File.Exists(TxtPath)) Application.OpenURL("file:///" + TxtPath.Replace('\\', '/'));
            GUI.enabled = true;
            if (GUILayout.Button("Close", GUILayout.Width(60))) _show = false;
            GUILayout.EndHorizontal();

            GUILayout.Label(_status);
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var e in View())
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.BeginHorizontal();
                GUILayout.Label("<b>" + e.Title + "</b>" + (e.Outdated ? "  <color=#e0b040>(outdated: the mod is installed but lacks these items)</color>" : ""), GUILayout.ExpandWidth(true));
                GUILayout.Label("by " + e.Creator, GUILayout.Width(170));
                GUILayout.EndHorizontal();
                if (e.Title != e.Guid) GUILayout.Label("<color=#909090>" + e.Guid + (string.IsNullOrEmpty(e.Website) ? "" : "   " + e.Website) + "</color>");
                GUILayout.Label("Characters (" + e.Chars.Count + "): " + string.Join(", ", e.Chars.Keys.ToArray()));
                GUILayout.Label("Items (" + e.Items.Count + "): " + string.Join(", ", e.ItemLabels.ToArray()));
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
}
