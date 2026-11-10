using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The Campus Life menu, drawn inside the clipboard screen with the game's sprites and font.
    /// Tabs: Today (status + requests), Calendar (upcoming events + journal), Activities (by place), Details (how
    /// everything works: the long explanations live there so the other tabs stay short).
    /// The school store is a separate view opened at the cafeteria.
    /// </summary>
    internal static partial class CampusPanel
    {
        private static RectTransform _root;
        private static RectTransform _tabs;
        private static RectTransform _left, _right;
        private static ScrollRect _leftScroll, _rightScroll;
        private static TextMeshProUGUI _title;
        private static string _savedTitle;
        private static PanelTab _tab = PanelTab.None;
        private static string _place = "";
        private static string _lastResult = "";

        public static void SetLastResult(string text) { _lastResult = text ?? ""; }
        private static readonly Dictionary<PanelTab, Button> TabButtons = new Dictionary<PanelTab, Button>();

        public static bool Visible { get { return _root != null && _root.gameObject.activeSelf; } }
        public static PanelTab Tab { get { return _tab; } }

        // ---------------- lifecycle ----------------

        public static void Build(Transform canvas)
        {
            Forget();
            var titleObj = canvas.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(t => t.name == "Title");
            _title = titleObj;

            var frame = GameUi.Image(canvas, "cl_campus_panel", GameUi.SprFrame);
            frame.raycastTarget = true;
            _root = frame.rectTransform;
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, -64f);
            _root.sizeDelta = new Vector2(1872f, 928f);

            var back = GameUi.Image(_root, "back", null, GameUi.PanelBack);
            back.rectTransform.Stretch(6f, 6f, 6f, 6f);
            back.raycastTarget = true;

            _tabs = GameUi.Rect(_root, "tabs");
            _tabs.Place(16f, 14f, 1840f, 44f);
            // The four pages you play with side by side, the help page apart after a gap, and on the right what leads out:
            // the store (when you are at the cafeteria) and back to the cards.
            var x = 0f;
            TabButtons.Clear();
            foreach (var tab in new[] { PanelTab.Today, PanelTab.Calendar, PanelTab.Activities, PanelTab.Album, PanelTab.Details })
            {
                var t = tab;
                if (tab == PanelTab.Details) x += 40f;
                var b = GameUi.Button(_tabs, "tab" + tab, tab.ToString(), () => Show(t, null));
                ((RectTransform)b.transform).Place(x, 0f, 196f, 44f);
                TabButtons[tab] = b;
                x += 204f;
            }
            var cards = GameUi.Button(_tabs, "tabCards", "Cards", Hide, GameUi.SprButton);
            ((RectTransform)cards.transform).Place(1840f - 196f, 0f, 196f, 44f);
            if (!StatusUi.AtNight && Places.CurrentId == "cafeteria")
            {
                var store = GameUi.Button(_tabs, "tabStore", "Campus store", () => Show(PanelTab.Shop), GameUi.SprButton);
                ((RectTransform)store.transform).Place(1840f - 196f - 12f - 220f, 0f, 220f, 44f);
            }

            _left = GameUi.ScrollList(_root, "left", out _leftScroll, 10f);
            ((RectTransform)_leftScroll.transform).Place(16f, 70f, 640f, 842f);
            _right = GameUi.ScrollList(_root, "right", out _rightScroll, 10f);
            ((RectTransform)_rightScroll.transform).Place(672f, 70f, 1184f, 842f);

            _root.gameObject.SetActive(false);
        }

        public static void Forget()
        {
            _root = null;
            _title = null;
            _tab = PanelTab.None;
            _drawnTab = PanelTab.None;
            TabButtons.Clear();
        }

        public static void Show(PanelTab tab, string place = null)
        {
            if (_root == null) return;
            if (tab == PanelTab.None) { Hide(); return; }
            if (tab == PanelTab.Album) { if (place != null) _albumCat = place; place = null; }   // a category, not a place
            if (place != null) _place = place;
            if (tab == PanelTab.Activities && place == null && string.IsNullOrEmpty(_place)) _place = Places.CurrentId;
            _tab = tab;
            if (!_root.gameObject.activeSelf)
            {
                _root.gameObject.SetActive(true);
                _root.SetAsLastSibling();
                if (_title != null) { _savedTitle = _title.text; }
                _lastResult = "";
            }
            // The game's own title sits under the time bar while our pages are up (the letters showed through it), so it
            // is hidden; the tab row and the store's heading already say where you are.
            if (_title != null) { _title.text = tab == PanelTab.Shop ? "Campus Store" : "Campus Life"; _title.enabled = false; }
            // The store is its own screen: no tabs.
            _tabs.gameObject.SetActive(tab != PanelTab.Shop);
            foreach (var kv in TabButtons)
                ((Image)kv.Value.targetGraphic).color = kv.Key == tab ? new Color(1f, 0.85f, 0.6f, 1f) : Color.white;
            Refresh();
        }

        public static void Hide()
        {
            if (_root == null) return;
            if (_tab == PanelTab.Shop) { StatusUi.Close(); return; }
            _root.gameObject.SetActive(false);
            if (_title != null) { if (_savedTitle != null) _title.text = _savedTitle; _title.enabled = true; }
            _tab = PanelTab.None;
            _drawnTab = PanelTab.None;
        }

        public static void Refresh()
        {
            if (_root == null || !_root.gameObject.activeSelf) return;
            // A refresh of the same page (a purchase, a button) keeps where you were; a new page starts at the top.
            var same = _drawnTab == _tab;
            var leftPos = _leftScroll != null ? _leftScroll.verticalNormalizedPosition : 1f;
            var rightPos = _rightScroll != null ? _rightScroll.verticalNormalizedPosition : 1f;
            var leftH = _leftScroll != null && _leftScroll.content != null ? _leftScroll.content.rect.height : 0f;
            var rightH = _rightScroll != null && _rightScroll.content != null ? _rightScroll.content.rect.height : 0f;
            _drawnTab = _tab;
            GameUi.Clear(_left);
            GameUi.Clear(_right);
            try
            {
                switch (_tab)
                {
                    case PanelTab.Today: TodayLeft(); TodayRight(); break;
                    case PanelTab.Calendar: CalendarLeft(); CalendarRight(); break;
                    case PanelTab.Activities: ActivitiesLeft(); ActivitiesRight(); break;
                    case PanelTab.Details: DetailsLeft(); DetailsRight(); break;
                    case PanelTab.Album: AlbumLeft(); AlbumRight(); break;
                    case PanelTab.Shop: ShopLeft(); ShopRight(); break;
                }
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
                GameUi.Para(_right, "Something went wrong drawing this page: " + e.Message, 20f, GameUi.Warn);
            }
            if (!same)
            {
                _leftScroll.verticalNormalizedPosition = 1f;
                _rightScroll.verticalNormalizedPosition = 1f;
                return;
            }
            KeepScroll(_leftScroll, leftPos, leftH);
            KeepScroll(_rightScroll, rightPos, rightH);
        }

        private static PanelTab _drawnTab = PanelTab.None;

        /// <summary>
        /// says where the list is after the page is drawn again.
        /// </summary>
        internal static string TestBuy()
        {
            if (_root == null || !_root.gameObject.activeSelf || _tab != PanelTab.Shop || _rightScroll == null) return "the shop is not open";
            Canvas.ForceUpdateCanvases();
            _rightScroll.verticalNormalizedPosition = 0.5f;
            var before = _rightScroll.verticalNormalizedPosition;
            var item = Items.All.Where(x => x.Type == ItemType.Gift || x.Type == ItemType.Consumable).Where(x => !x.LunchOnly).OrderBy(Items.PriceOf).FirstOrDefault();
            if (item == null) return "no item";
            if (Campus.S.Money < Items.PriceOf(item)) Campus.S.Money += Items.PriceOf(item);
            var ok = Campus.Buy(item);
            Refresh();
            Canvas.ForceUpdateCanvases();
            return "bought " + item.Name + " " + ok + ", list at " + before.ToString("0.00") + " before, " + _rightScroll.verticalNormalizedPosition.ToString("0.00") + " after";
        }

        /// <summary>The same distance from the top as before the page was drawn again.</summary>
        private static void KeepScroll(ScrollRect sc, float pos, float oldHeight)
        {
            try
            {
                if (sc == null || sc.content == null) return;
                LayoutRebuilder.ForceRebuildLayoutImmediate(sc.content);
                var view = sc.viewport != null ? sc.viewport.rect.height : ((RectTransform)sc.transform).rect.height;
                var oldTop = (1f - pos) * Mathf.Max(0f, oldHeight - view);
                var range = Mathf.Max(0f, sc.content.rect.height - view);
                sc.verticalNormalizedPosition = range <= 0f ? 1f : Mathf.Clamp01(1f - oldTop / range);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Keep scroll: " + e.Message); }
        }

        // ---------------- helpers ----------------

        private static readonly string[] DayNames = { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        public static string DayName(Cycle.Week? w) { return w.HasValue ? DayNames[(int)w.Value] : "?"; }

        public static string PeriodName()
        {
            var p = G.Period;
            if (!p.HasValue) return "";
            switch (p.Value)
            {
                case Cycle.Type.LunchTime: return "Lunch break";
                case Cycle.Type.StaffTime: return "Club time";
                case Cycle.Type.AfterSchool: return "After class";
                case Cycle.Type.Morning: return "Morning";
                case Cycle.Type.MyHouse: return "Night";
                default: return p.Value.ToString();
            }
        }

        /// <summary>Weekday of an older day number, from today's.</summary>
        private static string DayOf(int day)
        {
            var w = G.Weekday;
            if (!w.HasValue) return "";
            var idx = (((int)w.Value - (Campus.S.Day - day)) % 7 + 7) % 7;
            return DayNames[idx];
        }

        private static RectTransform Header(Transform block, string text, bool blue = true)
        {
            var bar = GameUi.Bar(block, "header", text, blue, 24f);
            bar.Size(-1f, 34f);
            return bar;
        }

        private static TextMeshProUGUI Line(Transform block, string text, float size = 21f, Color? color = null)
        {
            return GameUi.Para(block, GameUi.StatColors(text), size, color);
        }

        private static Button SmallButton(Transform parent, string label, Action onClick, float width = 190f, bool enabled = true)
        {
            var b = GameUi.Button(parent, "btn", label, () => onClick());
            b.Size(width, 40f);
            b.interactable = enabled;
            return b;
        }

        private static Image Icon(Transform parent, Sprite sprite, float size = 40f)
        {
            var img = GameUi.Image(parent, "icon", null, Color.white);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.Size(size, size);
            return img;
        }

        private static TextMeshProUGUI RowText(Transform row, string text, float size = 22f, Color? color = null)
        {
            var t = GameUi.Text(row, "text", GameUi.StatColors(text), size, color, TextAlignmentOptions.MidlineLeft, false);
            t.enableAutoSizing = true;
            t.fontSizeMin = 13f;
            t.fontSizeMax = size;
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.Size(-1f, -1f, 1f);
            return t;
        }

        private static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

        // Each girl or entry is a row with a round icon, the name in gold, a gauge of squares and the rest dimmed.
        private static readonly Color Pink = new Color(1f, 0.55f, 0.75f, 1f);
        private static readonly Color Blue = new Color(0.45f, 0.72f, 1f, 1f);

        private static string C(Color c, string text) { return "<color=" + Hex(c) + ">" + text + "</color>"; }

        /// <summary>A gauge of squares, "■■■□□".</summary>
        private static string Squares(int have, int max, int cells)
        {
            if (max <= 0) max = 1;
            var full = Mathf.Clamp(Mathf.RoundToInt((float)have * cells / max), 0, cells);
            return new string('\u25A0', full) + new string('\u25A1', cells - full);
        }

        /// <summary>One row: a round icon and a line of text.</summary>
        private static RectTransform IconRow(Transform block, string glyph, Color tint, string text, float size = 20f, float height = 36f)
        {
            var row = GameUi.Row(block, "row", height);
            Icon(row, Markers.Glyph(glyph, tint), height - 4f);
            RowText(row, text, size);
            return row;
        }

        /// <summary>Where she is now and how she feels, for a row.</summary>
        private static string WhereNow(SaveData.Heroine h)
        {
            if (h == null) return "";
            var map = GirlInfo.CurrentMap(h);
            var where = GirlInfo.IsSpawned(h) && map >= 0 ? Places.MapLabel(map) : "not on campus";
            var mood = Campus.S.Girl(GirlInfo.Key(h)).Mood;
            return where + (mood != Mood.None ? ", " + Lines.MoodName(mood).ToLowerInvariant() : "");
        }

        // ---------------- Today ----------------

        private static void TodayLeft()
        {
            var s = Campus.S;
            var b = GameUi.Block(_left, "today");
            Header(b, "Week " + s.ShownWeek + ", " + DayName(G.Weekday) + " - " + PeriodName());
            if (G.InRoamingPeriod) Line(b, "Time left: " + Mathf.CeilToInt(G.PeriodSecondsLeft) + " s");
            Line(b, "<color=" + Hex(GameUi.Gold) + ">" + s.Money.ToString("N0") + " yen</color>    Reputation " + s.Reputation +
                    (Cfg.EnergyEnabled.Value ? "    Energy " + s.Energy + "/" + Activities.MaxEnergy : ""));
            Line(b, "STR " + G.Str + "    INT " + G.Int + "    SEN " + G.Sens, 24f);
            Line(b, "<size=17><color=" + Hex(GameUi.Dim) + ">What each of these does: Details tab.</color></size>");

            if (!string.IsNullOrEmpty(s.PendingEvent) || !string.IsNullOrEmpty(s.PendingHappening))
            {
                var e = GameUi.Block(_left, "event");
                Header(e, "Happening now", false);
                if (!string.IsNullOrEmpty(s.PendingEvent))
                {
                    var row = GameUi.Row(e, "row", 44f);
                    RowText(row, "Campus event: " + Calendar.EventName(s.PendingEvent), 22f, GameUi.Gold);
                    SmallButton(row, "Attend", () => { StatusUi.Close(); Calendar.PlayEvent(); }, 160f);
                }
                if (!string.IsNullOrEmpty(s.PendingHappening))
                {
                    var row = GameUi.Row(e, "row", 44f);
                    RowText(row, Happenings.Name(s.PendingHappening), 22f, GameUi.Gold);
                    SmallButton(row, "See", () => { StatusUi.Close(); Calendar.PlayHappening(); }, 160f);
                }
            }

            if (Cfg.DevotionEnabled.Value)
            {
                var top = Campus.S.Girls.Values.Where(g => g.Devotion > 0).OrderByDescending(g => g.Devotion).Take(5).ToList();
                if (top.Count > 0)
                {
                    var d = GameUi.Block(_left, "devotion");
                    Header(d, "Devotion");
                    foreach (var g in top)
                    {
                        var lv = Devotion.Level(g);
                        var max = lv >= Devotion.MaxLevel;
                        var into = max ? Devotion.PerLevel : g.Devotion - lv * Devotion.PerLevel;
                        IconRow(d, "heart", Pink, C(GameUi.Gold, GirlInfo.FirstName(g.Key)) + "  " + (lv == 0 ? C(GameUi.Dim, "on her way") : "Lv " + lv + " " + Devotion.LevelNames[lv]) +
                            "   " + C(Pink, Squares(into, Devotion.PerLevel, 8)) + " " + C(GameUi.Dim, max ? "max" : into + "/" + Devotion.PerLevel));
                    }
                    var first = top[0];
                    var next = Devotion.Level(first) + 1;
                    if (next <= Devotion.MaxLevel)
                        Line(d, C(GameUi.Dim, "Next for " + GirlInfo.FirstName(first.Key) + ": " + Devotion.PerkText[next]), 17f);
                }
            }

            var arcs = Arcs.Describe().ToList();
            if (arcs.Count > 0)
            {
                var st = GameUi.Block(_left, "stories");
                Header(st, "Stories");
                foreach (var a in arcs) Line(st, a, 19f);
            }

            if (Cfg.SpotlightEnabled.Value && s.Spotlight.Count > 0)
            {
                var sp = GameUi.Block(_left, "spotlight");
                Header(sp, "This week's spotlight");
                Line(sp, C(GameUi.Dim, Spotlight.IsActive ? "Stories, outings and requests come from them first this week." : "This week's outings and stories star them."), 17f);
                foreach (var key in s.Spotlight)
                {
                    var h = GirlInfo.Find(key);
                    var row = IconRow(sp, "star", GameUi.Gold, C(GameUi.Gold, GirlInfo.FirstName(key)) + (h != null ? "   " + C(GameUi.Dim, WhereNow(h)) : ""), 20f, 40f);
                    if (h != null && !StatusUi.AtNight && GirlInfo.IsSpawned(h) && GirlInfo.CurrentMap(h) >= 0 && GirlInfo.CurrentMap(h) != G.PlayerMapNo)
                    {
                        var girl = h;
                        SmallButton(row, "Go to her", () => StatusUi.WarpToGirl(girl), 150f);
                    }
                }
            }

            Bag(_left);
        }

        private static void Bag(Transform list)
        {
            var s = Campus.S;
            var items = Items.All.Where(i => s.Count(i.Id) > 0).ToList();
            var bag = GameUi.Block(list, "bag");
            Header(bag, "Bag");
            if (items.Count == 0) { Line(bag, "Empty. The university store is in the cafeteria.", 19f, GameUi.Dim); return; }
            foreach (var i in items)
            {
                var row = GameUi.Row(bag, "row", 42f);
                Icon(row, ItemIcon(i), 38f);
                RowText(row, i.Name + " x" + s.Count(i.Id) + (i.Type == ItemType.Gift ? "  <color=" + Hex(GameUi.Dim) + ">(gift)</color>" : ""), 20f);
                if (i.Id == "energy_drink" || i.Id == "cologne")
                {
                    var item = i;
                    SmallButton(row, "Use", () => { Campus.UseConsumable(item); Refresh(); }, 120f);
                }
            }
        }

        private static int TalkCost()
        {
            var cost = (float)Cfg.TalkTimeCost.Value;
            if (Cfg.StrengthReducesTalkCost.Value) cost *= 1f - 0.4f * G.Str / 100f;
            return Mathf.RoundToInt(cost);
        }

        private static void TodayRight()
        {
            var s = Campus.S;
            var active = Board.Active.OrderBy(r => r.DeadlineDay).ThenBy(r => r.Source == "arc" ? 0 : 1).ThenBy(r => r.Id).ToList();
            var head = GameUi.Block(_right, "requests", false);
            Header(head, "Requests (" + active.Count + ")");
            if (!Cfg.BoardEnabled.Value) Line(head, "The club board is turned off in the settings.", 20f, GameUi.Warn);
            else if (active.Count == 0) Line(head, G.IsSchoolDay ? "Nobody needs you right now. Walk around, talk, or check back tomorrow." : "No class today.", 20f, GameUi.Dim);


            StoryBlock();
            foreach (var r in active) RequestBlock(r);

            var recent = s.Requests.Where(r => r.State != RequestState.Active && r.DeadlineDay >= s.Day - 1).OrderByDescending(r => r.Id).ToList();
            if (recent.Count > 0)
            {
                var b = GameUi.Block(_right, "recent");
                Header(b, "Recently closed", false);
                foreach (var r in recent.Take(5))
                {
                    var done = r.State == RequestState.Done;
                    var who = GirlInfo.Find(r.Girl);
                    var row = GameUi.Row(b, "row", 36f);
                    Icon(row, done ? Markers.Badge(r) : Markers.Glyph("cross", GameUi.Warn), 32f);
                    RowText(row, C(done ? GameUi.Green : GameUi.Warn, done ? "Done" : "Missed") + "   " + r.Title +
                        (who != null ? "  " + C(GameUi.Dim, "- " + GirlInfo.FirstName(who)) : "") +
                        (done ? "  " + C(GameUi.Dim, "(" + r.Reward.Describe() + ")") : ""), 19f);
                }
                if (recent.Count > 5) Line(b, "+" + (recent.Count - 5) + " more in the Calendar tab.", 18f, GameUi.Dim);
            }
        }

        private static void StoryBlock()
        {
            var n = 0;
            foreach (var h in StoryCast.Shown)
            {
                var b = GameUi.Block(_right, "story" + n++);
                var row = GameUi.Row(b, "title", 46f);
                Icon(row, Markers.StoryBadge(), 44f);
                RowText(row, "Story: " + GirlInfo.FullName(h) + "'s next chapter", 24f, GameUi.Gold);
                var map = StoryCast.MapOf(h);
                if (!StatusUi.AtNight && map >= 0 && map != G.PlayerMapNo) SmallButton(row, Places.MapLabel(map), () => { if (GirlInfo.IsSpawned(h)) StatusUi.WarpToGirl(h); else StatusUi.Warp(map); }, 190f);
                if (map >= 0)
                    Line(b, "She is waiting at the " + Places.MapLabel(map) + " right now. Talk to her to play her story event.", 19f);
                else
                    Line(b, "She was chosen for today. Her event can start in a free period (break, lunch or after class): look for her and talk to her.", 19f);
            }
        }

        private static void RequestBlock(Request r)
        {
            var s = Campus.S;
            var h = GirlInfo.Find(r.Girl);
            var b = GameUi.Block(_right, "req" + r.Id);
            var row = GameUi.Row(b, "title", 46f);
            Icon(row, Markers.Badge(r), 44f);
            var tag = r.Source == "arc" ? "Story: " : r.Lewd ? "Private: " : "";
            RowText(row, tag + r.Title, 24f, GameUi.Gold);
            if (h != null)
            {
                var spawned = GirlInfo.IsSpawned(h);
                var sameMap = spawned && GirlInfo.CurrentMap(h) == G.PlayerMapNo;
                if (!StatusUi.AtNight && spawned && !sameMap) SmallButton(row, "Go to her", () => StatusUi.WarpToGirl(h), 170f);
            }
            if ((r.Kind == RequestKind.Errand || r.Kind == RequestKind.MeetAt) && r.Progress == 0 && !string.IsNullOrEmpty(r.Place) && Places.CurrentId != r.Place)
            {
                var mapNo = Places.MapFor(r.Place);
                if (mapNo >= 0 && !StatusUi.AtNight) SmallButton(row, Places.Name(r.Place), () => StatusUi.Warp(mapNo), 190f);
            }
            if (r.Kind == RequestKind.Activity && Activities.Get(r.Activity) != null)
            {
                var a = Activities.Get(r.Activity);
                var mapNo = Places.MapFor(a.Place);
                if (mapNo >= 0 && !StatusUi.AtNight && Places.CurrentId != a.Place) SmallButton(row, Places.Name(a.Place), () => StatusUi.Warp(mapNo), 190f);
            }

            // The kind in its badge color, her name picked out, her mood of now, and the deadline colored by how close it
            // is.
            var text = Board.TextOf(r);
            if (h != null)
            {
                var first = GirlInfo.FirstName(h);
                if (!string.IsNullOrEmpty(first) && text.Contains(first))
                {
                    var at = text.IndexOf(first, StringComparison.Ordinal);
                    text = text.Substring(0, at) + "<color=" + Hex(GameUi.Gold) + ">" + first + "</color>" + text.Substring(at + first.Length);
                }
            }
            var label = "<color=" + Hex(Markers.TintOf(r)) + "><b>" + Markers.KindLabel(r) + "</b></color>";
            var mood = h != null ? Campus.S.Girl(r.Girl).Mood : Mood.None;
            var moodTag = mood != Mood.None ? "  <color=" + Hex(GameUi.Dim) + ">(" + Lines.MoodName(mood).ToLowerInvariant() + ")</color>" : "";
            Line(b, label + "   " + text + moodTag, 20f);
            var where = "";
            if (h != null)
            {
                var map = GirlInfo.CurrentMap(h);
                var doing = GirlInfo.IsSpawned(h) && map >= 0 ? Truth.Doing(h) : "";
                where = GirlInfo.IsSpawned(h) && map >= 0 ? "She is at: " + Places.MapLabel(map) + (doing != "" ? " (" + doing + ")" : "") : "She isn't on campus right now.";
            }
            else if (r.Source == "miss")
                where = string.Join("   ", r.Item.Split(Board.MissSep).Select(GirlInfo.Find).Where(g => g != null && GirlInfo.IsSpawned(g) && GirlInfo.CurrentMap(g) >= 0)
                    .Select(g => GirlInfo.FirstName(g) + ": " + Places.MapLabel(GirlInfo.CurrentMap(g))).ToArray());
            // One line of facts under the text; "How" only when it is more than talking to her.
            var dim = "<color=" + Hex(GameUi.Dim) + ">";
            var facts = new List<string>();
            if (r.Kind != RequestKind.Talk || r.Source == "arc") facts.Add(dim + "How:</color> " + Board.HowTo(r));
            var due = StatusUi.DueLabel(r);
            var dueColor = r.DeadlineDay <= Campus.S.Day ? GameUi.Warn : r.DeadlineDay == Campus.S.Day + 1 ? GameUi.Gold : GameUi.Green;
            facts.Add(dim + "When:</color> <color=" + Hex(dueColor) + ">" + due + "</color>");
            facts.Add(dim + "Reward:</color> " + r.Reward.Describe());
            if (where != "") facts.Add(dim + where + "</color>");
            Line(b, string.Join("   ", facts.ToArray()), 18f);
        }

        // ---------------- Calendar ----------------

        private static void CalendarLeft()
        {
            var s = Campus.S;
            var b = GameUi.Block(_left, "coming");
            Header(b, "Coming up");
            var list = Calendar.UpcomingEvents();
            if (list.Count == 0) Line(b, "Campus events are turned off in the settings.", 20f, GameUi.Dim);
            foreach (var u in list)
            {
                var row = GameUi.Row(b, "row", 38f);
                Icon(row, Markers.Glyph(u.Days == 0 ? "star" : "pin", u.Days == 0 ? GameUi.Gold : u.Days <= 3 ? Pink : Blue), 34f);
                RowText(row, u.Days == 0 ? "<b>" + u.Name + "</b>" : u.Name, 22f, u.Days == 0 ? GameUi.Gold : Color.white);
                var when = GameUi.Text(row, "when", u.When, 20f, u.Days == 0 ? GameUi.Gold : u.Days <= 3 ? Pink : GameUi.Dim, TextAlignmentOptions.MidlineRight, false);
                when.Size(300f, -1f);
            }

            var ex = GameUi.Block(_left, "exams");
            Header(ex, "Exams");
            if (s.ExamScores.Count == 0) IconRow(ex, "pen", Blue, "No exams yet. " + C(GameUi.Dim, "Study (INT) before exam week."));
            else
            {
                Func<int, string> grade = x => C(x >= 80 ? GameUi.Green : x >= 50 ? GameUi.Gold : GameUi.Warn, x.ToString());
                IconRow(ex, "pen", Blue, "Scores: " + string.Join("   ", s.ExamScores.Select(grade).ToArray()));
                Line(ex, C(GameUi.Dim, "Average " + Mathf.RoundToInt((float)s.ExamScores.Average()) + ", best " + s.ExamScores.Max() + ". 80 or more is the honor roll."), 17f);
            }

            if (Cfg.DirectorEnabled.Value)
            {
                var se = GameUi.Block(_left, "season");
                Header(se, "Season " + s.Season);
                IconRow(se, "people", GameUi.Green, ClubProjects.Status(), 19f);
                IconRow(se, "pen", Blue, Council.Status(), 19f);
                var rumor = Rumors.Main;
                if (rumor != null && rumor.Step >= 1) IconRow(se, "bubble", GameUi.Dim, C(GameUi.Dim, "Rumor: ") + Rumors.What(rumor, 1), 19f);
                var couples = Pairs.Couples.Count();
                if (couples > 0) IconRow(se, "heart", Pink, couples + " couple" + (couples == 1 ? "" : "s") + C(GameUi.Dim, " among the students."), 19f);
                var srow = GameUi.Row(se, "row", 44f);
                SmallButton(srow, "Class anthology", () => ReportUi.Show(Anthology.PageLines()), 220f);
            }

            var wk = GameUi.Block(_left, "week");
            Header(wk, "Last week");
            var stats = s.LastWeekStats;
            Func<string, int> n = k => { int v; return stats.TryGetValue(k, out v) ? v : 0; };
            if (stats.Count == 0) Line(wk, C(GameUi.Dim, "Your first week is still going."), 19f);
            else
            {
                IconRow(wk, "bubble", Blue, "Talks " + C(GameUi.Gold, n("talks").ToString()) + "    Gifts " + C(GameUi.Gold, n("gifts").ToString()) + "    H " + C(Pink, n("h").ToString()), 19f);
                IconRow(wk, "note", GameUi.Green, "Requests " + C(GameUi.Green, n("requests").ToString()) + "    Missed " + C(n("missed") > 0 ? GameUi.Warn : GameUi.Dim, n("missed").ToString()) +
                    "    Activities " + C(GameUi.Gold, n("activities").ToString()), 19f);
            }
            var row2 = GameUi.Row(wk, "row", 44f);
            SmallButton(row2, "Weekly report", () => ReportUi.Show(Report.BuildNow()), 220f);
        }

        /// <summary>
        /// Journal, newest first. each entry is one short row with an icon and a color for its kind instead of the written
        /// prefix ("Done:", "Missed:", "Achievement:"...); the reward is a small colored tag; a day's misses are one row;
        /// the news goes last, small and dimmed; days before yesterday are folded into one summary row with a button to
        /// open them.
        /// </summary>
        private static void CalendarRight()
        {
            var s = Campus.S;
            var head = GameUi.Block(_right, "journal", false);
            Header(head, "Journal");
            if (s.Journal.Count == 0) { Line(head, "Nothing yet.", 20f, GameUi.Dim); return; }
            var days = s.Journal.Select(e => e.Day).Distinct().OrderByDescending(d => d).Take(30).ToList();
            foreach (var d in days)
            {
                var day = d;
                var entries = s.Journal.Where(e => e.Day == day).Reverse().Select(e => JournalEntryOf(e.Text)).ToList();
                var block = GameUi.Block(_right, "day" + day);
                Header(block, "Day " + day + (day == s.Day ? " (today)" : day == s.Day - 1 ? " (yesterday)" : "") + " - " + DayOf(day), day == s.Day);
                var open = day >= s.Day - 1 || OpenDays.Contains(day);
                if (!open)
                {
                    var row = GameUi.Row(block, "row", 40f);
                    Icon(row, Markers.Glyph("note", GameUi.Dim), 30f);
                    RowText(row, JournalSummary(entries), 19f);
                    SmallButton(row, "Open", () => { OpenDays.Add(day); Refresh(); }, 120f);
                    continue;
                }
                foreach (var e in entries.Where(x => x.Kind != JKind.Missed && x.Kind != JKind.News))
                    JournalRow(block, e.Glyph, e.Tint, e.Text + (e.Tag.Length > 0 ? "  " + e.Tag : ""), 19f);
                var missed = entries.Where(x => x.Kind == JKind.Missed).Select(x => x.Text).ToList();
                if (missed.Count > 0)
                    JournalRow(block, "cross", GameUi.Warn, C(GameUi.Warn, "Missed" + (missed.Count > 1 ? " " + missed.Count : "")) + C(GameUi.Dim, ": ") + MergeTitles(missed), 18f);
                foreach (var e in entries.Where(x => x.Kind == JKind.News))
                    JournalRow(block, "bubble", GameUi.Dim, C(GameUi.Dim, e.Text), 16f);
                if (day < s.Day - 1)
                {
                    var row = GameUi.Row(block, "row", 40f);
                    RowText(row, "", 16f);
                    SmallButton(row, "Fold", () => { OpenDays.Remove(day); Refresh(); }, 120f);
                }
            }
        }

        /// <summary>The older days the player opened (until the game is closed).</summary>
        private static readonly HashSet<int> OpenDays = new HashSet<int>();

        private enum JKind { Other, Done, Missed, News, Story, Achievement, Learned, Life, Money }

        private struct JEntry
        {
            public JKind Kind;
            public string Glyph, Text, Tag;
            public Color Tint;
        }

        /// <summary>"(+7 favor, +1 reputation, -2 devotion)" at the end of an entry.</summary>
        private static readonly System.Text.RegularExpressions.Regex RewardTail =
            new System.Text.RegularExpressions.Regex(@"\s*\(([^()]*[+-]\s?\d[^()]*)\)\s*\.?\s*$");

        /// <summary>"Gave X to Nonaka (she loved it): +16 favor, +7 devotion": the reward after a colon (gifts).</summary>
        private static readonly System.Text.RegularExpressions.Regex RewardAfterColon =
            new System.Text.RegularExpressions.Regex(@":\s*([+-]\s?\d[^:()]*?)\s*\.?\s*$");

        /// <summary>What an entry is, by the words the systems log it with (Board, Arcs, Achievements, Spotlight...).</summary>
        private static JEntry JournalEntryOf(string raw)
        {
            var e = new JEntry { Kind = JKind.Other, Glyph = "pin", Tint = Blue, Text = raw ?? "", Tag = "" };
            var t = e.Text;
            Func<string, bool> cut = pre =>
            {
                if (!t.StartsWith(pre, StringComparison.Ordinal)) return false;
                t = t.Substring(pre.Length).Trim();
                return true;
            };
            if (cut("News:")) { e.Kind = JKind.News; e.Glyph = "bubble"; e.Tint = GameUi.Dim; }
            else if (cut("Missed:")) { e.Kind = JKind.Missed; t = t.TrimEnd('.'); }
            else if (cut("You missed ")) { e.Kind = JKind.Missed; t = t.TrimEnd('.'); }
            else if (cut("Done:")) { e.Kind = JKind.Done; e.Glyph = "note"; e.Tint = GameUi.Green; }
            else if (cut("Achievement:")) { e.Kind = JKind.Achievement; e.Glyph = "star"; e.Tint = GameUi.Gold; }
            else if (cut("A new story begins:")) { e.Kind = JKind.Story; e.Glyph = "book"; e.Tint = GameUi.Gold; t = C(GameUi.Dim, "New story ") + t; }
            else if (cut("Story finished:")) { e.Kind = JKind.Story; e.Glyph = "book"; e.Tint = GameUi.Gold; t = C(GameUi.Gold, "Story done ") + t; }
            else if (cut("Story dropped:")) { e.Kind = JKind.Story; e.Glyph = "book"; e.Tint = GameUi.Dim; t = C(GameUi.Dim, "Story dropped ") + t; }
            else if (cut("Learned:")) { e.Kind = JKind.Learned; e.Glyph = "heart"; e.Tint = Pink; t = C(GameUi.Dim, "Learned ") + t; }
            else if (cut("Campus life:")) { e.Kind = JKind.Life; e.Glyph = "people"; e.Tint = Blue; }
            else if (cut("This week's spotlight:")) { e.Kind = JKind.Life; e.Glyph = "star"; e.Tint = Pink; t = C(Pink, "Spotlight ") + t.TrimEnd('.'); }
            else if (cut("Weekly allowance:")) { e.Kind = JKind.Money; e.Glyph = "coin"; e.Tint = GameUi.Gold; t = C(GameUi.Gold, t.TrimEnd('.')) + C(GameUi.Dim, " allowance"); }
            else if (t.Contains("confession")) { e.Glyph = "heart"; e.Tint = Pink; }
            var m = RewardTail.Match(t);
            if (!m.Success) m = RewardAfterColon.Match(t);
            if (m.Success)
            {
                t = t.Substring(0, m.Index).TrimEnd('.', ' ');
                e.Tag = RewardTag(m.Groups[1].Value);
            }
            if (e.Kind == JKind.Achievement) t = C(GameUi.Gold, t);
            e.Text = t;
            return e;
        }

        /// <summary>"+7 favor, -1 reputation" as a small tag: gains green, losses red.</summary>
        private static string RewardTag(string inner)
        {
            var parts = inner.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0)
                .Select(x => C(x.StartsWith("-", StringComparison.Ordinal) ? GameUi.Warn : x.StartsWith("+", StringComparison.Ordinal) ? GameUi.Green : GameUi.Dim, x));
            return "<size=15>" + string.Join(C(GameUi.Dim, " · "), parts.ToArray()) + "</size>";
        }

        /// <summary>"Say hi to Asuka", "Say hi to Mei" -> "Say hi to Asuka, Mei"; other titles as they are.</summary>
        private static string MergeTitles(List<string> titles)
        {
            var keys = new List<string>();
            var names = new Dictionary<string, List<string>>();
            foreach (var t in titles)
            {
                var k = t.LastIndexOf(' ');
                var grouped = k > 0 && t.Split(' ').Length >= 3;
                var key = grouped ? t.Substring(0, k) : t;
                if (!names.ContainsKey(key)) { names[key] = new List<string>(); keys.Add(key); }
                if (grouped && !names[key].Contains(t.Substring(k + 1))) names[key].Add(t.Substring(k + 1));
            }
            return string.Join(C(GameUi.Dim, "; "), keys.Select(k => names[k].Count > 0 ? k + " " + string.Join(", ", names[k].ToArray()) : k).ToArray());
        }

        /// <summary>A folded day in one row: "3 done · 2 missed · 1 story · 4 news".</summary>
        private static string JournalSummary(List<JEntry> entries)
        {
            Func<JKind, int> n = k => entries.Count(x => x.Kind == k);
            var parts = new List<string>();
            if (n(JKind.Achievement) > 0) parts.Add(C(GameUi.Gold, n(JKind.Achievement) + " achievement" + (n(JKind.Achievement) > 1 ? "s" : "")));
            if (n(JKind.Story) > 0) parts.Add(C(GameUi.Gold, n(JKind.Story) + " stor" + (n(JKind.Story) > 1 ? "ies" : "y")));
            if (n(JKind.Done) > 0) parts.Add(C(GameUi.Green, n(JKind.Done) + " done"));
            if (n(JKind.Missed) > 0) parts.Add(C(GameUi.Warn, n(JKind.Missed) + " missed"));
            var other = n(JKind.Other) + n(JKind.Life) + n(JKind.Learned) + n(JKind.Money);
            if (other > 0) parts.Add(other + " other");
            if (n(JKind.News) > 0) parts.Add(C(GameUi.Dim, n(JKind.News) + " news"));
            return string.Join(C(GameUi.Dim, " · "), parts.ToArray());
        }

        /// <summary>A row with an icon whose text wraps (the row grows with it).</summary>
        private static void JournalRow(Transform block, string glyph, Color tint, string text, float size)
        {
            var row = GameUi.Row(block, "entry", 28f);
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = -1f;
            var h = row.GetComponent<HorizontalLayoutGroup>();
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.UpperLeft;
            Icon(row, Markers.Glyph(glyph, tint), 26f);
            var t = GameUi.Text(row, "text", GameUi.StatColors(text), size, null, TextAlignmentOptions.TopLeft, true);
            t.Size(-1f, -1f, 1f);
        }

        // ---------------- Activities ----------------

        private static void ActivitiesLeft()
        {
            var s = Campus.S;
            var here = Places.CurrentId;
            var b = GameUi.Block(_left, "places");
            Header(b, "Places");
            Line(b, "You are at: " + (string.IsNullOrEmpty(here) ? Places.MapLabel(G.PlayerMapNo) : Places.Name(here)) +
                    (Cfg.EnergyEnabled.Value ? "    Energy " + s.Energy + "/" + Activities.MaxEnergy : ""), 20f);
            foreach (var p in Places.All)
            {
                var acts = Activities.All.Where(a => a.Place == p.Id).ToList();
                if (acts.Count == 0) continue;
                var place = p;
                var row = GameUi.Row(b, "row", 44f);
                var label = p.Name + (p.Id == here ? "  (here)" : "") + "  " + acts.Count(a => Activities.Blocker(s, a) == null || p.Id != here) + "/" + acts.Count;
                var btn = SmallButton(row, label, () => Show(PanelTab.Activities, place.Id), 400f);
                if (p.Id == _place) ((Image)btn.targetGraphic).color = new Color(1f, 0.85f, 0.6f, 1f);
                var events = StatusUi.PlaceEvents(p.Id);
                if (events.Count > 0) RowText(row, "<color=" + Hex(GameUi.Gold) + ">" + events.Count + " event(s)</color>", 18f);
            }
            if (!string.IsNullOrEmpty(_lastResult))
            {
                var res = GameUi.Block(_left, "result");
                Header(res, "Result", false);
                Line(res, _lastResult, 20f);
            }
            Bag(_left);
        }

        private static void ActivitiesRight()
        {
            var s = Campus.S;
            if (!Cfg.ActivitiesEnabled.Value) { Line(_right, "Activities are turned off in the settings.", 20f, GameUi.Warn); return; }
            var placeId = string.IsNullOrEmpty(_place) ? Places.CurrentId : _place;
            if (string.IsNullOrEmpty(placeId)) placeId = Places.All[0].Id;
            var here = !StatusUi.AtNight && Places.CurrentId == placeId;

            var head = GameUi.Block(_right, "head");
            var row = GameUi.Row(head, "row", 46f);
            RowText(row, Places.Name(placeId), 28f, GameUi.Gold);
            var mapNo = Places.MapFor(placeId);
            if (!here && !StatusUi.AtNight && mapNo >= 0) SmallButton(row, "Go there", () => StatusUi.Warp(mapNo), 200f);
            if (placeId == "cafeteria" && here) SmallButton(row, "Campus store", () => Show(PanelTab.Shop), 200f);
            var events = StatusUi.PlaceEvents(placeId);
            foreach (var e in events) Line(head, "Event here: " + e, 20f, GameUi.Gold);
            if (!here) Line(head, StatusUi.AtNight ? "Planning for tomorrow: these open when you walk around again." : "Go there to do these. Time only passes while you walk around, not in this menu.", 18f, GameUi.Dim);
            if (Cfg.EnergyEnabled.Value) Line(head, Activities.EnergyNow(s) + ",  " + s.Money.ToString("N0") + " yen", 18f, GameUi.Dim);

            foreach (var a in Activities.All.Where(x => x.Place == placeId))
            {
                var b = GameUi.Block(_right, a.Id);
                var r = GameUi.Row(b, "row", 44f);
                Icon(r, ActivityIcon.Get(ActivitySpots.KindOf(a), a.Id, true), 42f);
                var cost = string.Join(", ", new[] { Activities.PeriodText(a), Cfg.EnergyEnabled.Value ? a.Energy + " energy" : null, a.MoneyCost > 0 ? a.MoneyCost + " yen" : null, a.IsJob ? "job" : null, a.OncePerDay ? "once a day" : null }.Where(x => x != null).ToArray());
                var gain = Activities.GainText(a, false);
                var info = (gain.Length > 0 ? "<color=" + Hex(GameUi.Gold) + ">" + gain + "</color>  " : "") + a.Desc;
                RowText(r, a.Name + (cost.Length > 0 ? "  <size=18><color=" + Hex(GameUi.Dim) + ">(" + cost + ")</color></size>" : ""), 23f, Color.white);
                if (here && a.GameSleep)
                {
                    var block = Activities.Blocker(s, a);
                    Line(b, info + "  <color=" + Hex(block == null ? GameUi.Green : GameUi.Warn) + ">" + (block ?? "Use the game's sleep icon here.") + "</color>", 19f, GameUi.Dim);
                }
                else if (here)
                {
                    var block = Activities.Blocker(s, a);
                    var act = a;
                    var btn = SmallButton(r, block == null ? "Do it" : "Not now", () =>
                    {
                        if (ActivitySpots.On)
                        {
                            StatusUi.Close();
                            ActivitySpots.Run(act, true);
                            return;
                        }
                        var res = Campus.DoActivity(act);
                        if (res != null) _lastResult = act.Name + ": " + res.Text + "\n" + res.Gains.Describe();
                        Refresh();
                    }, 170f, block == null && !Scenes.Running);
                    if (Cfg.PositionReadout.Value && ActivitySpots.On)
                    {
                        SmallButton(r, "Spot here", () => { ActivitySpots.MarkHere(act); Refresh(); }, 150f);
                        if (ActivitySpots.HasCustom(act)) SmallButton(r, "Default spot", () => { ActivitySpots.ResetSpot(act); Refresh(); }, 170f);
                    }
                    Line(b, info + (block != null ? "  <color=" + Hex(GameUi.Warn) + ">" + block + "</color>" : ""), 19f, GameUi.Dim);
                }
                else
                {
                    // From far away: what would stop it there, except the walk.
                    var block = Activities.Blocker(s, a);
                    var far = block != null && !block.StartsWith("Go to the") && !block.StartsWith("Only during") ? block : null;
                    Line(b, info + (far != null ? "  <color=" + Hex(GameUi.Warn) + ">" + far + "</color>" : ""), 19f, GameUi.Dim);
                }
            }
        }


        // ---------------- Details ----------------

        /// <summary>One thing to know: its name, then one short line. A block of these reads at a glance, unlike a paragraph.</summary>
        private static void Para(Transform block, string title, string text)
        {
            Line(block, "<color=" + Hex(GameUi.Gold) + "><b>" + title + "</b></color>", 21f);
            Line(block, text, 18f, GameUi.Dim);
        }

        private static void Stat(Transform block, string name, int value, params string[] gives)
        {
            Line(block, "<b>" + name + "</b>  <size=26>" + value + "</size>", 22f);
            Line(block, string.Join("   |   ", gives), 18f, GameUi.Dim);
        }

        private static void DetailsLeft()
        {
            var s = Campus.S;
            var you = GameUi.Block(_left, "stats");
            Header(you, "Your stats");
            Stat(you, "STR", G.Str, "+" + G.Str / 25 + " energy a day", "sports jobs", "festivals");
            Stat(you, "INT", G.Int, "+" + G.Int / 2 + "% favor", G.Int >= Cfg.MoodHintMinIntelligence.Value ? "you read every mood" : "moods at " + Cfg.MoodHintMinIntelligence.Value, "exams");
            Stat(you, "SEN", G.Sens, "+" + Mathf.RoundToInt(G.Sens / 1.5f) + "% H", G.Sens >= 40 ? "intimate requests" : "intimate requests at 40", G.Sens >= 50 ? "more requests" : "more requests at 50");

            var e = GameUi.Block(_left, "energy");
            Header(e, "Your day");
            Para(e, "Energy", "Pays for activities. Refills every morning.");
            Para(e, "Time", "A free period lasts " + Mathf.RoundToInt(500f * Cfg.PeriodLength.Value) + " s of walking. Menus and menu teleports cost none.");
            Para(e, "Reputation " + s.Reputation, "+" + s.Reputation + "% on request pay. Helping raises it, odd places lower it.");
            Para(e, ClubStory.Label(), "Talks, new members and H fill the club gauge. Each rank is a chapter.");

            if (Cfg.DevotionEnabled.Value)
            {
                var d = GameUi.Block(_left, "devotion");
                Header(d, "Devotion");
                Line(d, "Grows past 100 favor. A level every " + Devotion.PerLevel + " points.", 18f, GameUi.Dim);
                for (var i = 1; i <= Devotion.MaxLevel; i++)
                    IconRow(d, "heart", Pink, C(GameUi.Gold, "Lv " + i + " " + Devotion.LevelNames[i]) + "   " + C(GameUi.Dim, Devotion.PerkText[i]), 18f);
            }
        }

        private static void DetailsRight()
        {
            var c = GameUi.Block(_right, "talk");
            Header(c, "Conversations");
            Para(c, "Speech bubble", "Topics, let her talk, ask about her day or her friends. A star marks what she likes.");
            Para(c, "Heart", "Sweet talk, teasing, how she feels about you, and the game's confession, date and H.");
            Para(c, "Question mark", "Invitations, \"come with me\" and gifts. Only what is possible right now shows up.");
            Para(c, "Bars", "Each turn moves favor and H at once and uses " + Cfg.TurnTalkTime.Value.ToString("0.##") + " of the blue talk-time bar.");

            var m = GameUi.Block(_right, "moods");
            Header(m, "Moods");
            foreach (Mood mood in Enum.GetValues(typeof(Mood)))
            {
                if (mood == Mood.None) continue;
                Line(m, "<color=" + Hex(GameUi.Gold) + "><b>" + Lines.MoodName(mood) + "</b></color>   <color=" + Hex(GameUi.Dim) + ">" + Lines.MoodTip(mood) + "</color>", 19f);
            }
            Line(m, "Ask \"How's your day?\" to learn her mood, or have INT " + Cfg.MoodHintMinIntelligence.Value + "+.", 18f, GameUi.Dim);

            var r = GameUi.Block(_right, "requests");
            Header(r, "Requests and stories");
            Para(r, "Board", "Requests show once you learn of them. Their shape is the mark on her minimap icon.");
            Para(r, "Stories", "One story heroine with a pending event is chosen each college day.");
            Para(r, "Outings", "A girl in the spotlight may invite you out, and alone with her it can turn romantic.");
            Para(r, "Gifts", "A liked gift beats any chat. Learn likes from her day or from library magazines.");

            var a = GameUi.Block(_right, "activities");
            Header(a, "Activities");
            Para(a, "Spots", "Each activity has an icon at its place. \"Do it\" takes you there.");
            Para(a, "Moving a spot", "11. Position tools > Show position readout, stand there and press \"Spot here\".");
        }

        // ---------------- School store ----------------

        // Your money in big gold letters, every item on a row with its icon (the like it is for, or the bag), the price as
        // a gold tag, the likes as their round icons instead of a sentence, and the girls you know would love it.
        private static void ShopLeft()
        {
            var s = Campus.S;
            var b = GameUi.Block(_left, "wallet");
            var top = GameUi.Row(b, "title", 64f);
            Icon(top, StoreIcon.Get(true), 60f);
            RowText(top, "Campus store", 30f, GameUi.Gold);
            var money = GameUi.Row(b, "money", 46f);
            RowText(money, "<color=" + Hex(GameUi.Gold) + "><size=30>" + s.Money.ToString("N0") + "</size></color> yen", 24f);
            SmallButton(money, "Leave", StatusUi.Close, 160f);
            Line(b, "<size=18><color=" + Hex(GameUi.Dim) + ">A gift she likes beats any chat. The round icons are the likes it is for; the names, girls you know love it.</color></size>");
            var anyGift = Board.AnyGiftFor();
            if (anyGift.Count > 0) Line(b, "<size=19><color=" + Hex(Board.FitColor) + ">\u2605 " + string.Join(", ", anyGift.ToArray()) + (anyGift.Count > 1 ? " each want" : " wants") + " a gift: any of these will do.</color></size>");
            Bag(_left);
        }

        private static readonly Dictionary<string, Sprite> ItemIcons = new Dictionary<string, Sprite>();

        // Each item has its own drawing and color (IconArt.Items.cs), on a round badge like the board's requests, in
        // the store and in the bag.
        private static Sprite ItemIcon(ItemDef i)
        {
            Sprite sp;
            if (ItemIcons.TryGetValue(i.Id, out sp) && sp != null) return sp;
            const int size = 96;
            sp = MapSprite.Make(IconArt.RequestBadge(IconArt.ItemGlyph(i.Id), IconArt.ItemColor(i.Id), size), size, 100f, "cl_item_" + i.Id);
            ItemIcons[i.Id] = sp;
            return sp;
        }

        private static void ShopRight()
        {
            var s = Campus.S;
            var lunch = G.CurrentMask == PeriodMask.Lunch;
            foreach (var type in new[] { ItemType.Gift, ItemType.Consumable })
            {
                var head = GameUi.Block(_right, "head" + type, false);
                Header(head, type == ItemType.Gift ? "Gifts" : "Useful items");
                foreach (var i in Items.All.Where(x => x.Type == type))
                {
                    if (i.Intimate && !Cfg.LewdRequests.Value) continue;
                    var b = GameUi.Block(_right, i.Id);
                    // The note names who asked.
                    var fitsFor = type == ItemType.Gift ? Board.GiftFitsFor(i) : new List<string>();
                    // No frame here; the pink border is in the talk's gift menu.
                    var r = GameUi.Row(b, "row", 46f);
                    Icon(r, ItemIcon(i), 42f);
                    var have = s.Count(i.Id) > 0 ? "  <size=18><color=" + Hex(GameUi.Green) + ">x" + s.Count(i.Id) + "</color></size>" : "";
                    RowText(r, i.Name + have, 23f);
                    foreach (var l in i.Likes)
                    {
                        var li = Array.IndexOf(GirlInfo.LikeIds, l);
                        if (li >= 0) Icon(r, ConvArt.Badge(IconArt.FirstLike + li), 32f);
                    }
                    var price = GameUi.Text(r, "price", Items.PriceOf(i).ToString("N0") + " yen", 22f, GameUi.Gold, TextAlignmentOptions.MidlineRight, false);
                    price.Size(130f, 40f);
                    var item = i;
                    var can = s.Money >= Items.PriceOf(i) && (!i.LunchOnly || lunch);
                    SmallButton(r, "Buy", () => { if (Campus.Buy(item)) Sfx.Ok(); Refresh(); }, 120f, can);
                    var fans = i.Likes.Length == 0 ? new List<string>() : Social.Students.Where(h => h != null && i.Likes.Any(l => Conversation.KnownLike(h, l))).Select(GirlInfo.FirstName).Distinct().Take(5).ToList();
                    var note = (fitsFor.Count > 0 ? "<color=" + Hex(Board.FitColor) + ">\u2605 For " + string.Join(", ", fitsFor.ToArray()) + "'s request.</color> " : "")
                               + i.Desc + (i.LunchOnly ? " <color=" + Hex(GameUi.Warn) + ">Lunch only.</color>" : "")
                               + (fans.Count > 0 ? "  <color=" + Hex(GameUi.Gold) + ">" + string.Join(", ", fans.ToArray()) + "</color>" : "");
                    Line(b, note, 19f, GameUi.Dim);
                }
            }
        }
    }
}
