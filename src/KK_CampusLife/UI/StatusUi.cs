using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Joojkk.CampusLife
{
    internal enum PanelTab { None, Today, Calendar, Activities, Details, Shop }

    /// <summary>
    /// Campus Life inside the game's own clipboard (Character Status screen, middle mouse button):
    /// - a "Campus" info page on every girl's card (request, when, mood, devotion, likes) that "Switch Info" cycles to;
    /// - a note with the request's marker on the card of every girl who needs you;
    /// - a Campus Life card in the first row and a card for every place you can go to, with a warp button;
    /// - the Campus Life menu (Today / Calendar / Activities) opened from those cards, the header button or the menu hotkey (F7).
    /// The game pauses the period clock while this screen is open.
    /// </summary>
    internal static class StatusUi
    {
        public static PanelTab Pending = PanelTab.None;
        public static ChaStatusScene Scene;
        public static Transform CanvasRoot;

        private static readonly AccessTools.FieldRef<ChaStatusScene, Button> SortNameRef = AccessTools.FieldRefAccess<ChaStatusScene, Button>("btnSortName");
        private static readonly AccessTools.FieldRef<ChaStatusScene, Button> SortFavorRef = AccessTools.FieldRefAccess<ChaStatusScene, Button>("btnSortFavor");
        private static readonly AccessTools.FieldRef<ChaStatusScene, ChaStatusComponent> MaleRef = AccessTools.FieldRefAccess<ChaStatusScene, ChaStatusComponent>("cmpMale");
        private static readonly AccessTools.FieldRef<ChaStatusScene, ChaStatusComponent> FixRef = AccessTools.FieldRefAccess<ChaStatusScene, ChaStatusComponent>("cmpFix");
        private static readonly AccessTools.FieldRef<ChaStatusComponent, ChaStatusComponent.InfoObject[]> InfoRef = AccessTools.FieldRefAccess<ChaStatusComponent, ChaStatusComponent.InfoObject[]>("_infoObject");
        private static readonly AccessTools.FieldRef<ChaStatusComponent.InfoObject, GameObject> InfoObjRef = AccessTools.FieldRefAccess<ChaStatusComponent.InfoObject, GameObject>("_obj");
        private static readonly AccessTools.FieldRef<ChaStatusComponent.InfoObject, TextMeshProUGUI[]> InfoTextsRef = AccessTools.FieldRefAccess<ChaStatusComponent.InfoObject, TextMeshProUGUI[]>("_infos");

        private static readonly List<GameObject> PlaceCards = new List<GameObject>();
        private static readonly List<KeyValuePair<ChaStatusComponent, SaveData.Heroine>> Cards = new List<KeyValuePair<ChaStatusComponent, SaveData.Heroine>>();
        private static int _campusPageIndex = -1;
        private static int _socialPageIndex = -1;

        public static bool IsOpen { get { return Scene != null; } }

        /// <summary>Set by the Start prefix; if the game's own Start then fails, our postfix never runs.</summary>
        public static ChaStatusScene Starting;

        /// <summary>
        /// Escape hatch: if the game's clipboard failed to build (an exception in its Start), its close button and
        /// right click were never hooked up and the screen would stay forever. Right click / Esc closes it.
        /// </summary>
        public static void WatchBroken()
        {
            var s = Starting;
            if (s == null) return;
            if (s == Scene) { Starting = null; return; }
            if (!(Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))) return;
            Starting = null;
            CampusLifePlugin.Log.LogWarning("The clipboard failed to open properly; closing it.");
            try { s.Unload(); }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        // ---------------- open / close ----------------

        /// <summary>Opens the clipboard with the Campus Life menu on a tab (menu hotkey, cafeteria store icon).</summary>
        public static void Open(PanelTab tab)
        {
            if (Scene != null) { CampusPanel.Show(tab); return; }
            var act = G.Act;
            if (act == null || !G.IsFreeRoaming) return;
            Pending = tab;
            act.OpenStatus();
            if (!Singleton<Scene>.Instance.IsNowLoading && string.IsNullOrEmpty(Singleton<Scene>.Instance.AddSceneName) && Scene == null)
            {
                // OpenStatus refused (event running, regulated...): forget the request.
                Pending = PanelTab.None;
            }
        }

        public static void Close()
        {
            if (Scene != null) Scene.Unload();
        }

        // ---------------- building ----------------

        /// <summary>Postfix of ChaStatusScene.Start (cards already exist).</summary>
        public static void OnStart(ChaStatusScene scene)
        {
            Scene = scene;
            Cards.Clear();
            PlaceCards.Clear();
            _campusPageIndex = -1;
            _socialPageIndex = -1;
            GameUi.Capture(scene.gameObject);
            var canvas = scene.GetComponentsInChildren<Canvas>(true).FirstOrDefault(c => c.isRootCanvas);
            CanvasRoot = canvas != null ? canvas.transform : scene.transform;

            if (!Campus.On)
            {
                Pending = PanelTab.None;
                return;
            }

            Safe("cards", AddGirlPages);
            Safe("campus card", AddCampusCard);
            Safe("places", AddPlaceCards);
            Safe("header", AddHeaderButton);
            Safe("panel", () => CampusPanel.Build(CanvasRoot));

            var tab = Pending;
            Pending = PanelTab.None;
            if (tab != PanelTab.None) CampusPanel.Show(tab);
        }

        public static void OnDestroy()
        {
            Scene = null;
            CanvasRoot = null;
            Cards.Clear();
            PlaceCards.Clear();
            CampusPanel.Forget();
        }

        private static void Safe(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Clipboard (" + what + "): " + e); }
        }

        // ---- girl cards ----

        private static void AddGirlPages()
        {
            var male = MaleRef(Scene);
            var fix = FixRef(Scene);
            var act = G.Act;
            foreach (var comp in Scene.GetComponentsInChildren<ChaStatusComponent>(false))
            {
                if (comp == null || comp == male) continue;
                var h = comp.heroine;
                if (h == null && comp == fix && act != null && act.fixChara != null) h = act.fixChara.heroine;
                if (h == null || h.isTeacher) continue;
                Cards.Add(new KeyValuePair<ChaStatusComponent, SaveData.Heroine>(comp, h));
                AddCampusPage(comp, h);
                if (Cfg.DirectorEnabled.Value) Safe("social page", () => AddInfoPage(comp, h, "info_social", SocialLabels, FillSocial, false));
                AddNote(comp, h);
            }
        }

        private static readonly string[] PageLabels = { "Request", "When", "Mood", "Devotion", "Likes" };

        private static readonly string[] SocialLabels = { "Right now", "Her heart", "Best friend", "Trust in you", "She remembers" };

        /// <summary>Clones the first info page into a "Campus" page and appends it to the card's pages.</summary>
        private static void AddCampusPage(ChaStatusComponent comp, SaveData.Heroine h)
        {
            AddInfoPage(comp, h, "info_campus", PageLabels, Fill, true);
        }

        /// <summary>The school life page: what she wants now, her love life, her best friend, trust, a shared memory.</summary>
        private static void FillSocial(List<TextMeshProUGUI> v, SaveData.Heroine h)
        {
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            Action<int, string> set = (i, text) => { if (i < v.Count) v[i].text = text; };
            // What she is really doing, not the plan the director made earlier.
            var doing = Truth.Doing(h);
            set(0, Pairs.IsScorned(h) ? "hurt, avoiding you" : !string.IsNullOrEmpty(doing) ? doing : Intents.Label(gs.Intent));
            var partner = Pairs.PartnerOf(h);
            var pair = Pairs.PairOf(h);
            string heart;
            if (partner != null) heart = "with " + GirlInfo.FirstName(partner) + (pair != null && pair.Stage == PairStage.Strained ? " (rough patch)" : "") + (Pairs.IsConflicted(h) ? ", torn" : "");
            else if (h.isGirlfriend) heart = "your girlfriend" + (Campus.S.Harem.Contains(GirlInfo.Key(h)) ? " (shares you)" : "") + (string.IsNullOrEmpty(Seasons.ChapterLabel(h)) ? "" : ", ch. " + Mathf.Min(3, gs.Chapter + 1));
            else
            {
                var k = GirlInfo.Key(h);
                var crush = Campus.S.Pairs.Values.Where(p => (p.A == k || p.B == k) && (p.Stage == PairStage.Crush || p.Stage == PairStage.Talking) && p.RomanceOf(k) >= 35).OrderByDescending(p => p.RomanceOf(k)).FirstOrDefault();
                heart = crush != null && Social.TalkDaysRecent(h, 30) >= 3 ? "a crush (" + GirlInfo.FirstName(crush.Other(k)) + "?)" : "free";
            }
            set(1, heart);
            var bf = Social.BestFriend(h);
            set(2, bf == null ? "-" : GirlInfo.FirstName(bf));
            set(3, Social.TrustLabel(Social.Trust(h)));
            var m = Memories.Last(h, 14);
            set(4, m == null ? "-" : m.Text);
        }

        /// <summary>Clones the first info page into one of our pages and appends it to the card's pages.</summary>
        private static void AddInfoPage(ChaStatusComponent comp, SaveData.Heroine h, string name, string[] labels, Action<List<TextMeshProUGUI>, SaveData.Heroine> fill, bool campus)
        {
            var infos = InfoRef(comp);
            if (infos == null || infos.Length == 0 || infos[0] == null || InfoObjRef(infos[0]) == null) return;
            var src = InfoObjRef(infos[0]);
            var page = Object.Instantiate(src, src.transform.parent, false);
            page.name = name;
            page.SetActive(false);

            // Rows alternate: label bar, value. Label bars hold a "text" child; values hold a "text" child.
            var values = new List<TextMeshProUGUI>();
            var row = 0;
            for (var i = 0; i + 1 < page.transform.childCount && row < labels.Length; i += 2, row++)
            {
                var bar = page.transform.GetChild(i).GetComponentInChildren<TextMeshProUGUI>(true);
                var val = page.transform.GetChild(i + 1).GetComponentInChildren<TextMeshProUGUI>(true);
                if (bar != null) bar.text = labels[row];
                if (val != null)
                {
                    val.enableAutoSizing = true;
                    val.fontSizeMin = 11f;
                    val.fontSizeMax = val.fontSize;
                    val.enableWordWrapping = false;
                    val.overflowMode = TextOverflowModes.Ellipsis;
                    values.Add(val);
                }
            }
            fill(values, h);

            var ours = new ChaStatusComponent.InfoObject();
            InfoObjRef(ours) = page;
            InfoTextsRef(ours) = values.ToArray();
            // SetActiveInfoAll only cycles through enabled pages by index, so keep enabled pages first.
            var list = infos.Where(o => o != null && o.enabled).ToList();
            list.Add(ours);
            if (campus) _campusPageIndex = list.Count - 1;
            else _socialPageIndex = list.Count - 1;
            list.AddRange(infos.Where(o => o != null && !o.enabled));
            InfoRef(comp) = list.ToArray();
        }

        private static void Fill(List<TextMeshProUGUI> v, SaveData.Heroine h)
        {
            var gs = Campus.S.Girl(GirlInfo.Key(h));
            var r = Markers.ShownFor(h);
            Action<int, string> set = (i, text) => { if (i < v.Count) v[i].text = text; };
            set(0, r == null ? "-" : r.Title);
            set(1, r == null ? "-" : DueLabel(r));
            set(2, Moods.Known(gs) ? Lines.MoodName(gs.Mood) : "?");
            set(3, Devotion.Label(gs));
            var known = GirlInfo.LikeIds.Where((l, i) => (gs.KnownLikes & (1 << i)) != 0 && GirlInfo.Likes(h, l)).Select(GirlInfo.LikeLabel).ToArray();
            set(4, known.Length == 0 ? "?" : string.Join(", ", known));
        }

        public static string DueLabel(Request r)
        {
            var day = r.DeadlineDay <= Campus.S.Day ? "today" : r.DeadlineDay == Campus.S.Day + 1 ? "by tomorrow" : "in " + (r.DeadlineDay - Campus.S.Day) + " days";
            return Board.WindowLabel(r.Periods) + ", " + day;
        }

        /// <summary>A note pinned to the bottom of the student card: marker + what she needs.</summary>
        private static void AddNote(ChaStatusComponent comp, SaveData.Heroine h)
        {
            var r = Markers.ShownFor(h);
            var story = h != null && StoryCast.Shown.Contains(h);
            if ((r == null && !story) || comp.cmpStudentCard == null) return;
            var card = comp.cmpStudentCard.transform as RectTransform;
            var bar = GameUi.Image(card, "cl_note", GameUi.SprBlueBar);
            bar.rectTransform.Place(140f, 228f, 284f, 36f);
            var icon = GameUi.Image(bar.transform, "marker", null, Color.white);
            icon.sprite = story ? Markers.Get(Markers.StoryShape) : Markers.For(r);
            icon.rectTransform.Place(-14f, -4f, 44f, 44f);
            var t = GameUi.Text(bar.transform, "text", story ? "Story event today" : r.Title, 20f, Color.white, TextAlignmentOptions.MidlineLeft, false);
            t.rectTransform.Stretch(34f, 2f, 6f, 2f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 12f;
            t.fontSizeMax = 20f;
            t.overflowMode = TextOverflowModes.Ellipsis;
        }

        /// <summary>Header button that shows the Campus page on every card at once.</summary>
        private static void AddHeaderButton()
        {
            var sortName = SortNameRef(Scene);
            if (sortName == null) return;
            var parent = sortName.transform.parent;

            var campus = Object.Instantiate(sortName.gameObject, parent, false);
            campus.name = "btnCampusInfo";
            var rt = (RectTransform)campus.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x - 136f, rt.anchoredPosition.y);
            var b = campus.GetComponent<Button>();
            b.onClick = new Button.ButtonClickedEvent();
            b.SetLabel("Requests");
            b.onClick.AddListener(() =>
            {
                try
                {
                    Sfx.Select();
                    ShowCampusPages();
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });

            var menu = Object.Instantiate(sortName.gameObject, parent, false);
            menu.name = "btnCampusMenu";
            var rt2 = (RectTransform)menu.transform;
            rt2.anchoredPosition = new Vector2(rt.anchoredPosition.x - 136f, rt.anchoredPosition.y);
            var b2 = menu.GetComponent<Button>();
            b2.onClick = new Button.ButtonClickedEvent();
            b2.SetLabel("Campus Life");
            b2.onClick.AddListener(() =>
            {
                try { Sfx.Select(); CampusPanel.Show(PanelTab.Today); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });

            // Keep place cards after the girls when the game re-sorts the list.
            foreach (var sort in new[] { SortNameRef(Scene), SortFavorRef(Scene) })
                if (sort != null) sort.onClick.AddListener(() => { foreach (var c in PlaceCards) if (c != null) c.transform.SetAsLastSibling(); });
        }

        /// <summary>Requests first, then everyone else; all cards switch to the Campus page.</summary>
        /// <summary>Every card on the School life page.</summary>
        public static void ShowSocialPages()
        {
            if (_socialPageIndex < 0) return;
            foreach (var kv in Cards.Where(k => k.Key != null)) kv.Key.SetActiveInfoAll(_socialPageIndex);
        }

        public static void ShowCampusPages()
        {
            if (_campusPageIndex < 0) return;
            foreach (var kv in Cards.Where(k => k.Key != null))
                kv.Key.SetActiveInfoAll(_campusPageIndex);
            var withRequests = Cards.Where(k => k.Key != null && k.Key.heroine != null && Markers.ShownFor(k.Value) != null).ToList();
            foreach (var kv in withRequests.AsEnumerable().Reverse())
            {
                // Right after the first row (player, story girl, Campus Life card).
                var parent = kv.Key.transform.parent;
                var firstGirl = Cards.Where(k => k.Key != null && k.Key.heroine != null).Select(k => k.Key.transform.GetSiblingIndex()).DefaultIfEmpty(3).Min();
                kv.Key.transform.SetSiblingIndex(Math.Min(firstGirl, parent.childCount - 1));
            }
        }

        // ---- Campus Life card (first row) ----

        private static void AddCampusCard()
        {
            var grid = Scene.GetComponentsInChildren<GridLayoutGroup>(true).FirstOrDefault();
            if (grid == null) return;
            var dummyFix = grid.transform.Cast<Transform>().FirstOrDefault(c => c.name == "dummy_fix" && c.gameObject.activeSelf);
            var card = NewCard(grid.transform, "cl_campus_card");
            if (dummyFix != null)
            {
                card.SetSiblingIndex(dummyFix.GetSiblingIndex());
                dummyFix.gameObject.SetActive(false);
            }

            var s = Campus.S;
            var header = GameUi.Bar(card, "header", "Campus Life", true, 24f);
            header.Place(10f, 10f, 400f, 32f);
            var lines = new List<string>
            {
                "Week " + s.ShownWeek + ", " + CampusPanel.DayName(G.Weekday) + " - " + CampusPanel.PeriodName(),
                "Money: <color=#FFE08A>" + s.Money.ToString("N0") + " yen</color>",
                "Energy: " + (Cfg.EnergyEnabled.Value ? s.Energy + "/" + Activities.MaxEnergy + "  (for activities)" : "off"),
                "Reputation: " + s.Reputation + "/100",
                "Open requests: " + Board.Active.Count() + (string.IsNullOrEmpty(s.PendingEvent) ? "" : "   Today: " + Calendar.EventName(s.PendingEvent)),
            };
            var t = GameUi.Text(card, "lines", string.Join("\n", lines.ToArray()), 22f, Color.white);
            t.rectTransform.Place(16f, 50f, 400f, 220f);
            t.lineSpacing = 6f;

            var y = 10f;
            foreach (var tab in new[] { PanelTab.Today, PanelTab.Calendar, PanelTab.Activities })
            {
                var tabCopy = tab;
                var b = GameUi.Button(card, "btn" + tab, tab.ToString(), () => CampusPanel.Show(tabCopy));
                ((RectTransform)b.transform).Place(424f, y, 176f, 40f);
                y += 50f;
            }
            var help = GameUi.Text(card, "help", Cfg.PlannerKey.Value + " opens this menu", 16f, GameUi.Dim, TextAlignmentOptions.Center);
            help.rectTransform.Place(424f, y + 8f, 176f, 60f);
        }

        /// <summary>An empty card with the game's frame, sized by the grid.</summary>
        private static Transform NewCard(Transform grid, string name)
        {
            var img = GameUi.Image(grid, name, GameUi.SprFrame);
            img.raycastTarget = true;
            return img.transform;
        }

        // ---- place cards ----

        private static void AddPlaceCards()
        {
            var grid = Scene.GetComponentsInChildren<GridLayoutGroup>(true).FirstOrDefault();
            if (grid == null || !Cfg.ActivitiesEnabled.Value) return;
            var here = Places.CurrentId;
            foreach (var p in Places.All)
            {
                var mapNo = Places.MapFor(p.Id);
                if (mapNo < 0) continue;
                var card = NewCard(grid.transform, "cl_place_" + p.Id);
                PlaceCards.Add(card.gameObject);
                BuildPlaceCard(card, p, mapNo, here == p.Id);
            }
        }

        private static void BuildPlaceCard(Transform card, Places.Place p, int mapNo, bool isHere)
        {
            var header = GameUi.Bar(card, "header", p.Name, true, 24f);
            header.Place(10f, 10f, 404f, 32f);

            var thumb = PlaceThumbs.Get(mapNo);
            if (thumb != null)
            {
                var raw = GameUi.Rect(card, "thumb").gameObject.AddComponent<RawImage>();
                raw.texture = thumb;
                raw.raycastTarget = false;
                raw.rectTransform.Place(10f, 48f, 220f, 150f);
            }

            var acts = Activities.All.Where(a => a.Place == p.Id).ToList();
            var now = acts.Where(a => (a.Periods & G.CurrentMask) != 0).ToList();
            var girls = G.Act != null ? G.Act.npcList.Count(n => n != null && n.mapNo == mapNo) : 0;
            var events = PlaceEvents(p.Id);
            var info = new List<string>();
            info.Add(isHere ? "<color=#9CFFA6>You are here</color>" : "Students here: " + girls);
            info.Add("Activities now: " + now.Count + "/" + acts.Count);
            if (p.Id == "cafeteria") info.Add("<color=#FFE08A>School store</color>");
            foreach (var e in events.Take(2)) info.Add("<color=#FFE08A>" + e + "</color>");
            var t = GameUi.Text(card, "info", string.Join("\n", info.ToArray()), 19f, Color.white);
            t.rectTransform.Place(thumb != null ? 240f : 16f, 50f, thumb != null ? 176f : 400f, 220f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 13f;
            t.fontSizeMax = 19f;

            var y = 10f;
            if (!isHere)
            {
                var go = GameUi.Button(card, "btnGo", "Go there", () => Warp(mapNo));
                ((RectTransform)go.transform).Place(424f, y, 176f, 40f);
                y += 50f;
            }
            var actsBtn = GameUi.Button(card, "btnActs", "Activities", () => CampusPanel.Show(PanelTab.Activities, p.Id));
            ((RectTransform)actsBtn.transform).Place(424f, y, 176f, 40f);
            y += 50f;
            if (p.Id == "cafeteria" && isHere)
            {
                var shop = GameUi.Button(card, "btnShop", "School store", () => CampusPanel.Show(PanelTab.Shop));
                ((RectTransform)shop.transform).Place(424f, y, 176f, 40f);
            }
        }

        /// <summary>Requests and story steps that happen at a place.</summary>
        public static List<string> PlaceEvents(string placeId)
        {
            var list = new List<string>();
            foreach (var r in Board.Active.Where(r => r.Place == placeId && (r.Kind == RequestKind.MeetAt || r.Kind == RequestKind.Errand)))
                list.Add(r.Title);
            foreach (var r in Board.Active.Where(r => r.Kind == RequestKind.Activity && Activities.Get(r.Activity) != null && Activities.Get(r.Activity).Place == placeId))
                list.Add(r.Title);
            return list;
        }

        // ---------------- warping ----------------

        /// <summary>Warps the player to a map (same as the warp buttons on the girls' cards) and closes the clipboard.</summary>
        public static void Warp(int mapNo)
        {
            var act = G.Act;
            if (act == null || mapNo < 0) return;
            if (act.Player != null && act.Player.mapNo == mapNo) { Close(); return; }
            Sfx.Ok();
            FreeTravel.Begin();
            WalkTalk.OnTeleport();
            Spots.Spot spot;
            var has = Spots.TryGet("warp", mapNo, out spot);
            act.Map.PlayerMapWarp(mapNo, () =>
            {
                CarryChaser(act, mapNo);
                if (has) act.Player.StartCoroutine(PlaceAfterLoad(mapNo, () => spot.Pos, () => Quaternion.Euler(0f, spot.Yaw, 0f)));
            });
            Close();
        }

        /// <summary>
        /// Warps in front of a girl's NPC, facing her, like the clipboard's own warp button (KK_WarpToCharacters).
        /// </summary>
        public static void WarpToGirl(SaveData.Heroine h)
        {
            var act = G.Act;
            if (act == null || h == null) return;
            Base chara = act.npcList.FirstOrDefault(n => n != null && n.heroine == h);
            if (chara == null && act.fixChara != null && act.fixChara.heroine == h) chara = act.fixChara;
            if (chara == null) { Toasts.Show(GirlInfo.FirstName(h) + " isn't at school right now."); return; }
            var target = chara;
            if (act.Player.mapNo == target.mapNo) { Close(); return; }
            Sfx.Ok();
            FreeTravel.Begin();
            WalkTalk.OnTeleport();
            var map = target.mapNo;
            act.Map.PlayerMapWarp(map, () =>
            {
                CarryChaser(act, map);
                act.Player.StartCoroutine(PlaceAfterLoad(map, () => InFrontOf(target), () => Quaternion.identity, target));
            });
            Close();
        }

        /// <summary>A girl following the player comes along through the warp (as KK_WarpToCharacters does).</summary>
        private static void CarryChaser(ActionScene act, int mapNo)
        {
            try
            {
                var chaser = act.Player.chaser;
                if (chaser != null && chaser.mapNo == act.Player.mapNo) chaser.mapNo = mapNo;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Warp chaser: " + e.Message); }
        }

        /// <summary>1.2 m in front of her, on the walkable floor (null: she left the map).</summary>
        private static Vector3? InFrontOf(Base target)
        {
            if (target == null) return null;
            var pos = target.position + target.transform.forward * 1.2f;
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(pos, out hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position;
            else if (UnityEngine.AI.NavMesh.SamplePosition(target.position, out hit, 1.5f, UnityEngine.AI.NavMesh.AllAreas)) pos = hit.position;
            return pos;
        }

        /// <summary>
        /// The game's PlayerMapWarp runs its callback before the map changes, and the new map then puts the player at the
        /// 2026-10-11). So the place is set once the new map has loaded and the player can move.
        /// </summary>
        internal static IEnumerator PlaceAfterLoad(int mapNo, Func<Vector3?> pos, Func<Quaternion> rot, Base faceTo = null)
        {
            var t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 30f)
            {
                var act = G.Act;
                if (act != null && act.Map != null && !act.Map.isMapLoading && act.Player != null && act.Player.mapNo == mapNo && G.IsFreeRoaming) break;
                yield return null;
            }
            yield return null;
            yield return null;
            try
            {
                var act = G.Act;
                if (act == null || act.Player == null || act.Player.mapNo != mapNo) yield break;
                if (faceTo != null && faceTo.mapNo != mapNo) yield break; // she left meanwhile: stay at the door
                var p = pos();
                if (p == null) yield break;
                var player = act.Player;
                player.position = p.Value;
                if (faceTo != null)
                {
                    var look = faceTo.position - p.Value;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.01f) player.rotation = Quaternion.LookRotation(look);
                }
                else player.rotation = rot();
                // The camera kept the door's angle and turned the player back. The game's own
                // summon turns the camera the same way.
                act.CameraState.SetAngle(player.rotation.eulerAngles);
                CampusLifePlugin.Log.LogInfo("Warp: placed on map " + mapNo + (faceTo != null ? " in front of " + faceTo.name : " at the saved spot"));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Warp place: " + e.Message); }
        }
    }

    /// <summary>Map thumbnails from the game's Free H map list (used on the place cards).</summary>
    internal static class PlaceThumbs
    {
        private static readonly Dictionary<int, Texture> Cache = new Dictionary<int, Texture>();

        public static Texture Get(int mapNo)
        {
            Texture t;
            if (Cache.TryGetValue(mapNo, out t)) return t;
            t = null;
            try
            {
                var act = G.Act;
                MapInfo.Param p;
                if (act != null && act.Map.infoDic.TryGetValue(mapNo, out p) && p != null && !string.IsNullOrEmpty(p.ThumbnailBundle) && !string.IsNullOrEmpty(p.ThumbnailAsset))
                {
                    var op = AssetBundleManager.LoadAsset(p.ThumbnailBundle, p.ThumbnailAsset, typeof(Texture2D));
                    if (op != null && !op.IsEmpty()) t = op.GetAsset<Texture2D>();
                    if (t == null)
                    {
                        var opS = AssetBundleManager.LoadAsset(p.ThumbnailBundle, p.ThumbnailAsset, typeof(Sprite));
                        var sp = opS != null && !opS.IsEmpty() ? opS.GetAsset<Sprite>() : null;
                        if (sp != null) t = sp.texture;
                    }
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Thumbnail " + mapNo + ": " + e.Message); }
            Cache[mapNo] = t;
            return t;
        }
    }
}
