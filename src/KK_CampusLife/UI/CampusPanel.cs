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
    internal static class CampusPanel
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
            var x = 0f;
            TabButtons.Clear();
            foreach (var tab in new[] { PanelTab.Today, PanelTab.Calendar, PanelTab.Activities, PanelTab.Details })
            {
                var t = tab;
                var b = GameUi.Button(_tabs, "tab" + tab, tab.ToString(), () => Show(t, null));
                ((RectTransform)b.transform).Place(x, 0f, 200f, 44f);
                TabButtons[tab] = b;
                x += 212f;
            }
            var cards = GameUi.Button(_tabs, "tabCards", "Cards", Hide, GameUi.SprButton);
            ((RectTransform)cards.transform).Place(1840f - 200f, 0f, 200f, 44f);

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
            TabButtons.Clear();
        }

        public static void Show(PanelTab tab, string place = null)
        {
            if (_root == null) return;
            if (tab == PanelTab.None) { Hide(); return; }
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
            if (_title != null) _title.text = tab == PanelTab.Shop ? "School Store" : "Campus Life";
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
            if (_title != null && _savedTitle != null) _title.text = _savedTitle;
            _tab = PanelTab.None;
        }

        public static void Refresh()
        {
            if (_root == null || !_root.gameObject.activeSelf) return;
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
                    case PanelTab.Shop: ShopLeft(); ShopRight(); break;
                }
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError(e);
                GameUi.Para(_right, "Something went wrong drawing this page: " + e.Message, 20f, GameUi.Warn);
            }
            _leftScroll.verticalNormalizedPosition = 1f;
            _rightScroll.verticalNormalizedPosition = 1f;
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
                case Cycle.Type.AfterSchool: return "After school";
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
                    RowText(row, "School event: " + Calendar.EventName(s.PendingEvent), 22f, GameUi.Gold);
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
                    Line(d, string.Join(", ", top.Select(g => GirlInfo.FirstName(g.Key) + " (" + Devotion.Label(g) + ")").ToArray()), 19f);
                }
            }

            var arcs = Arcs.Describe().ToList();
            if (arcs.Count > 0)
            {
                var st = GameUi.Block(_left, "stories");
                Header(st, "Stories");
                foreach (var a in arcs) Line(st, a, 19f);
            }

            if (Spotlight.IsActive && s.Spotlight.Count > 0)
            {
                var sp = GameUi.Block(_left, "spotlight");
                Header(sp, "This week's spotlight");
                Line(sp, string.Join(", ", s.Spotlight.Select(GirlInfo.FirstName).ToArray()), 20f);
            }

            Bag(_left);
        }

        private static void Bag(Transform list)
        {
            var s = Campus.S;
            var items = Items.All.Where(i => s.Count(i.Id) > 0).ToList();
            var bag = GameUi.Block(list, "bag");
            Header(bag, "Bag");
            if (items.Count == 0) { Line(bag, "Empty. The school store is in the cafeteria.", 19f, GameUi.Dim); return; }
            foreach (var i in items)
            {
                var row = GameUi.Row(bag, "row", 42f);
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
            else if (active.Count == 0) Line(head, G.IsSchoolDay ? "Nobody needs you right now. Walk around, talk, or check back tomorrow." : "No school today.", 20f, GameUi.Dim);


            StoryBlock();
            foreach (var r in active) RequestBlock(r);

            var recent = s.Requests.Where(r => r.State != RequestState.Active && r.DeadlineDay >= s.Day - 1).OrderByDescending(r => r.Id).ToList();
            if (recent.Count > 0)
            {
                var b = GameUi.Block(_right, "recent");
                Header(b, "Recently closed", false);
                foreach (var r in recent.Take(5))
                    Line(b, (r.State == RequestState.Done ? "Done: " : "Missed: ") + r.Title, 19f, r.State == RequestState.Done ? GameUi.Green : GameUi.Warn);
                if (recent.Count > 5) Line(b, "+" + (recent.Count - 5) + " more in the Calendar tab.", 18f, GameUi.Dim);
            }
        }

        private static void StoryBlock()
        {
            var h = StoryCast.Waiting;
            if (h == null) return;
            var b = GameUi.Block(_right, "story");
            var row = GameUi.Row(b, "title", 46f);
            Icon(row, Markers.Get(Markers.StoryShape), 44f);
            RowText(row, "Story: " + GirlInfo.FullName(h) + "'s next chapter", 24f, GameUi.Gold);
            var map = StoryCast.EventMap;
            if (map >= 0 && map != G.PlayerMapNo) SmallButton(row, Places.MapLabel(map), () => StatusUi.Warp(map), 190f);
            if (map >= 0)
                Line(b, "She is waiting at the " + Places.MapLabel(map) + " right now. Talk to her to play her story event.", 19f);
            else
                Line(b, "She was chosen for today. Her event can start in a free period (break, lunch or after school): look for her and talk to her.", 19f);

        }

        private static void RequestBlock(Request r)
        {
            var s = Campus.S;
            var h = GirlInfo.Find(r.Girl);
            var b = GameUi.Block(_right, "req" + r.Id);
            var row = GameUi.Row(b, "title", 46f);
            Icon(row, Markers.For(r), 44f);
            var tag = r.Source == "arc" ? "Story: " : r.Lewd ? "Private: " : "";
            RowText(row, tag + r.Title, 24f, GameUi.Gold);
            if (h != null)
            {
                var spawned = GirlInfo.IsSpawned(h);
                var sameMap = spawned && GirlInfo.CurrentMap(h) == G.PlayerMapNo;
                if (spawned && !sameMap) SmallButton(row, "Go to her", () => StatusUi.WarpToGirl(h), 170f);
            }
            if ((r.Kind == RequestKind.Errand || r.Kind == RequestKind.MeetAt) && r.Progress == 0 && !string.IsNullOrEmpty(r.Place) && Places.CurrentId != r.Place)
            {
                var mapNo = Places.MapFor(r.Place);
                if (mapNo >= 0) SmallButton(row, Places.Name(r.Place), () => StatusUi.Warp(mapNo), 190f);
            }
            if (r.Kind == RequestKind.Activity && Activities.Get(r.Activity) != null)
            {
                var a = Activities.Get(r.Activity);
                var mapNo = Places.MapFor(a.Place);
                if (mapNo >= 0 && Places.CurrentId != a.Place) SmallButton(row, Places.Name(a.Place), () => StatusUi.Warp(mapNo), 190f);
            }

            Line(b, r.Text, 20f);
            var where = "";
            if (h != null)
            {
                var map = GirlInfo.CurrentMap(h);
                where = GirlInfo.IsSpawned(h) && map >= 0 ? "She is at: " + Places.MapLabel(map) : "She isn't at school right now.";
            }
            // One line of facts under the text; "How" only when it is more than talking to her.
            var dim = "<color=" + Hex(GameUi.Dim) + ">";
            var facts = new List<string>();
            if (r.Kind != RequestKind.Talk || r.Source == "arc") facts.Add(dim + "How:</color> " + Board.HowTo(r));
            facts.Add(dim + "When:</color> " + StatusUi.DueLabel(r));
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
            if (list.Count == 0) Line(b, "School events are turned off in the settings.", 20f, GameUi.Dim);
            foreach (var u in list)
            {
                var row = GameUi.Row(b, "row", 38f);
                RowText(row, u.Name, 22f, u.Days == 0 ? GameUi.Gold : Color.white);
                var when = GameUi.Text(row, "when", u.When, 20f, GameUi.Dim, TextAlignmentOptions.MidlineRight, false);
                when.Size(300f, -1f);
            }

            var ex = GameUi.Block(_left, "exams");
            Header(ex, "Exams");
            Line(ex, s.ExamScores.Count == 0 ? "No exams yet. Study (INT) before exam week." : "Scores: " + string.Join(", ", s.ExamScores.Select(x => x.ToString()).ToArray()), 20f);

            var wk = GameUi.Block(_left, "week");
            Header(wk, "Last week");
            var stats = s.LastWeekStats;
            Func<string, int> n = k => { int v; return stats.TryGetValue(k, out v) ? v : 0; };
            Line(wk, stats.Count == 0 ? "-" : "Talks " + n("talks") + ", requests " + n("requests") + ", missed " + n("missed") + ", activities " + n("activities") + ", gifts " + n("gifts"), 19f);
            var row2 = GameUi.Row(wk, "row", 44f);
            SmallButton(row2, "Weekly report", () => ReportUi.Show(Report.BuildNow()), 220f);
        }

        /// <summary>Journal, newest first (days and entries all in the same direction).</summary>
        private static void CalendarRight()
        {
            var s = Campus.S;
            var head = GameUi.Block(_right, "journal", false);
            Header(head, "Journal");
            if (s.Journal.Count == 0) { Line(head, "Nothing yet.", 20f, GameUi.Dim); return; }
            RectTransform day = null;
            var lastDay = -1;
            var shown = 0;
            for (var i = s.Journal.Count - 1; i >= 0 && shown < 150; i--, shown++)
            {
                var e = s.Journal[i];
                if (e.Day != lastDay || day == null)
                {
                    day = GameUi.Block(_right, "day" + e.Day);
                    var label = "Day " + e.Day + (e.Day == s.Day ? " (today)" : "") + " - " + DayOf(e.Day);
                    Header(day, label, false);
                    lastDay = e.Day;
                }
                var news = e.Text.StartsWith("News:", StringComparison.Ordinal);
                Line(day, e.Text, news ? 18f : 20f, news ? GameUi.Dim : Color.white);
            }
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
            var here = Places.CurrentId == placeId;

            var head = GameUi.Block(_right, "head");
            var row = GameUi.Row(head, "row", 46f);
            RowText(row, Places.Name(placeId), 28f, GameUi.Gold);
            var mapNo = Places.MapFor(placeId);
            if (!here && mapNo >= 0) SmallButton(row, "Go there", () => StatusUi.Warp(mapNo), 200f);
            if (placeId == "cafeteria" && here) SmallButton(row, "School store", () => Show(PanelTab.Shop), 200f);
            var events = StatusUi.PlaceEvents(placeId);
            foreach (var e in events) Line(head, "Event here: " + e, 20f, GameUi.Gold);
            if (!here) Line(head, "Go there to do these. Time only passes while you walk around, not in this menu.", 18f, GameUi.Dim);

            foreach (var a in Activities.All.Where(x => x.Place == placeId))
            {
                var b = GameUi.Block(_right, a.Id);
                var r = GameUi.Row(b, "row", 44f);
                var cost = (Cfg.EnergyEnabled.Value ? a.Energy + " energy, " : "") + Mathf.RoundToInt(a.Seconds * Cfg.ActivityTimeScale.Value) + " s" +
                           (a.MoneyCost > 0 ? ", " + a.MoneyCost + " yen" : "") + (a.IsJob ? ", job" : "") + (a.OncePerDay ? ", once a day" : "");
                RowText(r, a.Name + "  <size=18><color=" + Hex(GameUi.Dim) + ">(" + cost + ")</color></size>", 23f, Color.white);
                if (here)
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
                    Line(b, a.Desc + (block != null ? "  <color=" + Hex(GameUi.Warn) + ">" + block + "</color>" : ""), 19f, GameUi.Dim);
                }
                else Line(b, a.Desc, 19f, GameUi.Dim);
            }
        }


        // ---------------- Details ----------------

        private static void Para(Transform block, string title, string text)
        {
            Line(block, "<color=" + Hex(GameUi.Gold) + ">" + title + "</color>  " + text, 19f);
        }

        private static void DetailsLeft()
        {
            var s = Campus.S;
            var you = GameUi.Block(_left, "stats");
            Header(you, "Your stats");
            Para(you, "STR " + G.Str, "+" + G.Str / 25 + " energy a day (one per 25), sports jobs and festivals, exercise invitations.");
            Para(you, "INT " + G.Int, "favor gains +" + G.Int / 2 + "%, " + (G.Int >= Cfg.MoodHintMinIntelligence.Value ? "you read every mood" : "you read moods at " + Cfg.MoodHintMinIntelligence.Value) +
                                      ", joke odds shown at 40, exams, better pay in INT jobs.");
            Para(you, "SEN " + G.Sens, "H gains +" + Mathf.RoundToInt(G.Sens / 1.5f) + "%, " + (G.Sens >= 50 ? "more girls ask for you" : "more requests at 50") + ", " +
                                       (G.Sens >= 40 ? "intimate requests unlocked" : "intimate requests at 40") + ", flirting odds shown at 40, tips.");

            var e = GameUi.Block(_left, "energy");
            Header(e, "Energy, time and reputation");
            Para(e, "Energy", "for activities; refills every morning. Naps, rest, hearty lunches, energy drinks and devotion level 2 restore it.");
            Para(e, "Time", "a free period lasts " + Mathf.RoundToInt(500f * Cfg.PeriodLength.Value) + " s of walking around. Activities take time; menus, talks and menu teleports don't.");
            Para(e, "Reputation", "+" + s.Reputation + "% on request pay now, a daily request at 60, better fundraising. Helping raises it; odd places lower it.");

            if (Cfg.DevotionEnabled.Value)
            {
                var d = GameUi.Block(_left, "devotion");
                Header(d, "Devotion");
                Line(d, "Favor stops at 100; devotion keeps growing. Requests, liked gifts and stories add it, and favor past 100 turns into it. Every " + Devotion.PerLevel + " points is a level:", 18f);
                for (var i = 1; i <= Devotion.MaxLevel; i++)
                    Line(d, "<color=" + Hex(GameUi.Gold) + ">" + i + ". " + Devotion.LevelNames[i] + "</color>  " + Devotion.PerkText[i], 18f);
            }
        }

        private static void DetailsRight()
        {
            var c = GameUi.Block(_right, "talk");
            Header(c, "Conversations");
            Para(c, "Speech bubble", "talking: topics (a star marks what she likes when you can read her mood), let her talk (problems, gossip, what her friends need), ask about her day, her friends or the school, jokes, compliments, clearing the air, and the game's small talk and listen.");
            Para(c, "Heart", "romance and H: being sweet to her, love and lewd talk, and the game's confession, date, H and break up when the game allows them.");
            Para(c, "Question mark", "invitations (lunch, study, exercise, club, going home, the Koikatsu Club), \"come with me\", gifts, and buttons other mods add. Only what the game allows right now is shown.");
            Para(c, "Bars", "every turn moves her favor and H bars at once and uses " + Cfg.TurnTalkTime.Value.ToString("0.##") + " of the blue talk-time bar (the game's own options use 1). Her mood and trust change how invitations go: a hungry girl who likes you comes to lunch, a sleepy one won't exercise.");

            var m = GameUi.Block(_right, "moods");
            Header(m, "Moods");
            foreach (Mood mood in Enum.GetValues(typeof(Mood)))
            {
                if (mood == Mood.None) continue;
                Para(m, Lines.MoodName(mood), Lines.MoodTip(mood));
            }
            Line(m, "Ask \"How's your day?\" (Ask about...) to learn her mood, or have INT " + Cfg.MoodHintMinIntelligence.Value + "+.", 18f);

            var r = GameUi.Block(_right, "requests");
            Header(r, "Requests and stories");
            Para(r, "Board", "requests show once you learn of them (she or a friend tells you, or you see it). Its shape is the mark on her minimap icon.");
            Para(r, "Stories", "one story heroine with a pending event is chosen each school day.");
            Para(r, "Gifts", "a liked gift beats any chat and adds devotion. Learn likes by asking about her day or from library magazines.");

            var a = GameUi.Block(_right, "activities");
            Header(a, "Activities");
            Para(a, "Spots", "each activity has an icon at its place and plays the game's animation (or a short black screen). \"Do it\" takes you there.");
            Para(a, "Moving a spot", "11. Position tools > Show position readout, stand there and press \"Spot here\".");
        }

        // ---------------- School store ----------------

        private static void ShopLeft()
        {
            var s = Campus.S;
            var b = GameUi.Block(_left, "wallet");
            Header(b, "School store");
            Line(b, "Money: <color=" + Hex(GameUi.Gold) + ">" + s.Money.ToString("N0") + " yen</color>", 24f);
            Line(b, "<size=18><color=" + Hex(GameUi.Dim) + ">A gift she likes beats any chat. How to learn her likes: Details tab.</color></size>");
            var row = GameUi.Row(b, "row", 44f);
            SmallButton(row, "Leave", StatusUi.Close, 200f);
            Bag(_left);
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
                    var r = GameUi.Row(b, "row", 44f);
                    var have = s.Count(i.Id) > 0 ? "  <size=18><color=" + Hex(GameUi.Green) + ">(have " + s.Count(i.Id) + ")</color></size>" : "";
                    RowText(r, i.Name + "  <color=" + Hex(GameUi.Gold) + ">" + Items.PriceOf(i).ToString("N0") + " yen</color>" + have, 23f);
                    var item = i;
                    var can = s.Money >= Items.PriceOf(i) && (!i.LunchOnly || lunch);
                    SmallButton(r, "Buy", () => { if (Campus.Buy(item)) Sfx.Ok(); Refresh(); }, 140f, can);
                    var likes = i.Likes.Length > 0 ? "  <color=" + Hex(GameUi.Dim) + ">For girls who like " + string.Join(", ", i.Likes.Select(GirlInfo.LikeLabel).ToArray()) + ".</color>" : "";
                    Line(b, i.Desc + (i.LunchOnly ? " Sold at lunch only." : "") + likes, 19f);
                }
            }
        }
    }
}
