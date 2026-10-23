using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The single conversation (specification in exportar\sistema-de-conversa-campus-life-especificacao.md, 2026-10-12):
    /// five real buttons in a column (Talk, Actions, Do together, Romance, Spicy: the game's speech bubble and copies of
    /// it), each showing its own options in the list beside them; no scrolling list, no Back. Friendship, romance and spicy are levels of content and reward,
    /// not pages. The game's own commands (invitations, confession, date, H, break up, apology) are the game's; our beats
    /// pay from one table (level x how fitting the answer was), and the same beat again on the same day pays half, then
    /// nothing.
    /// </summary>
    internal static partial class Conversation
    {
        private enum Lvl { Friend, Romance, Spicy }
        private enum Q { Good, Neutral, Bad }

        /// <summary>One answer inside a beat: its label, how fitting it is now, her reply (4 tones, or a stock one).</summary>
        private sealed class Resp
        {
            public string Label = "", Tag = "";
            public Q Q;
            public string[] Her;
            public Feel? Face;
            public Action Done;
        }

        private static Resp Rp(string label, string tag, Q q, string[] her = null, Action done = null, Feel? face = null)
        {
            return new Resp { Label = label, Tag = tag, Q = q, Her = her, Done = done, Face = face };
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        private static string[] Tn(string gentle, string energetic, string cool, string teasing) { return new[] { gentle, energetic, cool, teasing }; }

        private sealed class UEntry
        {
            public string Label;
            public Action Run;
            public bool Costs = true;   // uses the blue bar (gifts and requests do not)
        }

        private static UEntry E(string label, Action run, bool costs = true) { return new UEntry { Label = label, Run = run, Costs = costs }; }

        // ================= the list =================

        private static bool Unified { get { return Cfg.SingleConversation == null || Cfg.SingleConversation.Value; } }

        /// <summary>Last list shown.</summary>
        public static List<string> LastList = new List<string>();

        /// <summary>
        /// Everything that can happen now, by category (empty ones left out). The game's own buttons (what it allows now)
        /// are only refreshed when its screen redraws: with <paramref name="refreshGame"/> it is asked first, so a girl who
        /// just got angry shows the apology and not the invitations.
        /// </summary>
        private static List<KeyValuePair<string, List<UEntry>>> ComputeCats(bool refreshGame, out bool noTime)
        {
            var s = Sess;
            var h = s.Her;
            PruneRepeats();
            if (_trio != null && _trio.Target != TrioWho.A)
            {
                // The second girl, or both: our own five (the game only knows the first one).
                noTime = false;
                return TrioCats(false);
            }
            if (refreshGame) try { if (UpdateUiMethod != null) UpdateUiMethod.Invoke(_scene, new object[] { false }); } catch (Exception e) { CampusLifePlugin.Log.LogDebug("Talk UpdateUI: " + e.Message); }
            // An empty blue bar, or the point given back only for a request: the free options only.
            noTime = h.talkTime <= 0 || Board.TalkKeptFor(h);
            var gs = S.Girl(GirlInfo.Key(h));

            var talk = new List<UEntry>();
            var actions = new List<UEntry>();
            var together = new List<UEntry>();
            var romance = new List<UEntry>();
            var spicy = new List<UEntry>();

            // A girl still hurt after being left for you: only the redemption, when it can be started.
            if (Pairs.IsScorned(h) || gs.Scorned > 0 && gs.Redemption < 3 && gs.ScornedUntil >= S.Day)
            {
                var r = RedemptionEntry(h, gs);
                if (r != null) talk.Add(r);
                if (h.isAnger) talk.Add(E("Apologize", () => Game(10)));
            }
            else
            {
                BuildTalk(s, h, gs, talk);
                BuildActions(s, h, gs, actions);
                if (!h.isAnger) BuildTogether(together);
                if (h.relation >= 0 && !h.isAnger)
                {
                    BuildRomance(s, h, gs, romance);
                    BuildSpicy(s, h, gs, spicy);
                }
                // The other girl is right there.
                if (_trio != null) PairOptions(h, _trio.B, talk, actions, together, romance, spicy);
            }

            var cats = new List<KeyValuePair<string, List<UEntry>>>
            {
                Cat("Talk", talk, noTime), Cat("Actions", actions, noTime), Cat("Do together", together, noTime),
                Cat("Romance", romance, noTime), Cat("Spicy", spicy, noTime)
            }.Where(c => c.Value.Count > 0).ToList();
            LastList = new List<string>();
            LastCats.Clear();
            foreach (var c in cats)
            {
                LastList.Add("# " + c.Key);
                foreach (var e in c.Value) { LastList.Add(e.Label); if (!LastCats.ContainsKey(e.Label)) LastCats[e.Label] = c.Key; }
            }
            return cats;
        }

        private static void UnifiedPage()
        {
            bool noTime;
            var cats = ComputeCats(true, out noTime);
            RefreshCatBar(cats);
            if (cats.Count == 0)
            {
                if (noTime && !_goodbye)
                {
                    // The blue bar is empty and nothing free is left: the game's own goodbye (once).
                    LastList.Add("(no time left: goodbye)");
                    _goodbye = true;
                    Game(-1);
                }
                return;
            }
            // The five buttons beside the list choose the category; the list shows that category's options only (user,
            // 2026-10-12: "eu falei 5 botões, não 5 opções com sub opções"). A list opened by something else (the game,
            // after its apology) shows Talk, or the first category that has something.
            var open = _cat != null ? cats.FirstOrDefault(c => c.Key == _cat) : default(KeyValuePair<string, List<UEntry>>);
            if (open.Value == null) { open = cats.FirstOrDefault(c => c.Key == "Talk"); if (open.Value == null) open = cats[0]; _cat = open.Key; }
            foreach (var e in open.Value) Add(e.Label, e.Run);
            RefreshCatBar(cats);
        }

        /// <summary>The category shown now (null: none chosen yet).</summary>
        private static string _cat;

        /// <summary>Which category each option of the last build is in.</summary>
        internal static readonly Dictionary<string, string> LastCats = new Dictionary<string, string>();

        internal static bool ShowCategoryOf(string label)
        {
            string c;
            if (label == null || !LastCats.TryGetValue(label, out c)) return false;
            if (_cat == c) return true;
            _cat = c;
            Build();
            return true;
        }

        // ================= the five buttons =================

        private static string[] CatNames = { "Talk", "Actions", "Do together", "Romance", "Spicy" };
        private static string[] CatCaptions = { "Talk", "Actions", "Together", "Romance", "Spicy" };
        private static readonly List<Button> CatButtons = new List<Button>();
        private static readonly List<TextMeshProUGUI> CatTexts = new List<TextMeshProUGUI>();
        private static readonly List<GameObject> CatMade = new List<GameObject>();
        private static float _catRefreshAt;

        /// <summary>
        /// The game's speech bubble becomes the first of five buttons in a column (Talk, Actions, Do together, Romance,
        /// Spicy); the other four are copies of it. All five wear our own tiles (<see cref="ConvArt"/>: speech bubble,
        /// gift, two people, heart, flame, each framed in its color) and have a caption under them. The game's listen and
        /// heart buttons stay hidden for the talk.
        /// </summary>
        private static void BuildCatBar()
        {
            ClearCatBar();
            if (_talk == null) return;
            var trt = (RectTransform)_talk.transform;
            var parent = trt.parent;
            var height = trt.rect.height > 10f ? trt.rect.height : 64f;
            var step = height + 30f;   // room for the caption under each icon (24 hid "Talk" under the next one)
            var tmpl = _template != null ? _template.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            for (var i = 0; i < CatNames.Length; i++)
            {
                Button btn;
                if (i == 0) btn = _talk;
                else
                {
                    var go = UnityEngine.Object.Instantiate(_talk.gameObject, parent, false);
                    go.name = "cl_cat_" + CatCaptions[i];
                    CatMade.Add(go);
                    btn = go.GetComponent<Button>();
                    ((RectTransform)go.transform).anchoredPosition = trt.anchoredPosition + new Vector2(0f, -step * i);
                    foreach (Transform child in go.transform) if (child.name == "cl_cat_caption") UnityEngine.Object.Destroy(child.gameObject);
                    var idx = i;   // by place: with both girls the names change (Settle, Group actions)
                    btn.onClick = new Button.ButtonClickedEvent();
                    btn.onClick.AddListener(() => CatClick(CatNames[idx]));
                }
                try
                {
                    ConvArt.Style(btn, ConvArt.Tile(i, false), false);
                    btn.image.preserveAspect = true;
                }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Category tile: " + e.Message); }
                var cap = new GameObject("cl_cat_caption", typeof(RectTransform));
                CatMade.Add(cap);
                var crt = (RectTransform)cap.transform;
                crt.SetParent(btn.transform, false);
                crt.anchorMin = new Vector2(0.5f, 0f);
                crt.anchorMax = new Vector2(0.5f, 0f);
                crt.pivot = new Vector2(0.5f, 1f);
                crt.anchoredPosition = new Vector2(0f, -1f);
                crt.sizeDelta = new Vector2(110f, 22f);
                var t = cap.AddComponent<TextMeshProUGUI>();
                if (tmpl != null) { t.font = tmpl.font; t.fontSharedMaterial = tmpl.fontSharedMaterial; }
                t.fontSize = 17f;
                t.enableWordWrapping = false;
                t.alignment = TextAlignmentOptions.Top;
                t.raycastTarget = false;
                t.richText = false;
                t.text = CatCaptions[i];
                t.color = Color.white;
                CatButtons.Add(btn);
                CatTexts.Add(t);
            }
            CenterCatBar(parent as RectTransform);
            _catRefreshAt = 0f;
        }

        /// <summary>
        /// The column stands with its middle button (Together) at half the screen's height (user, 2026-10-26: "align it
        /// with Together in the center of the screen's height"). Measured on screen, so building twice does not move it
        /// twice; the option list follows by the same height (<see cref="KeepClear"/>), its top level with Talk's.
        /// </summary>
        private static void CenterCatBar(RectTransform parent)
        {
            _barDy = 0f;
            if (parent == null || CatButtons.Count < 3 || CatButtons[2] == null) return;
            try
            {
                var canvas = parent.GetComponentInParent<Canvas>();
                var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                var mid = OnScreen((RectTransform)CatButtons[2].transform, cam);
                var dy = Screen.height * 0.5f - mid.center.y;
                if (Mathf.Abs(dy) < 0.5f) return;
                Vector2 a, c;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Vector2.zero, cam, out a);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(0f, dy), cam, out c);
                var move = new Vector2(0f, c.y - a.y);
                foreach (var b in CatButtons) if (b != null) ((RectTransform)b.transform).anchoredPosition += move;
                _barDy = dy;
                CampusLifePlugin.Log.LogInfo("Talk: category column moved " + dy.ToString("0") + " px, Together at the screen's middle");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Category column: " + e.Message); }
        }

        /// <summary>How far (screen px, up is +) the column moved from where the game put its talk button.</summary>
        private static float _barDy;

        /// <summary>Which category the options on screen belong to (their cards take its color); -1 outside the five.</summary>
        private static int OptionKind()
        {
            return Unified && _cat != null ? Array.IndexOf(CatNames, _cat) : -1;
        }

        /// <summary>The color of the open category's cards, darkened, for the letters of its options; white outside the five.</summary>
        private static Color OptionColor()
        {
            var k = OptionKind();
            if (k < 0) return new Color(0.96f, 0.96f, 0.96f, 1f);
            var a = IconArt.Accent[k];
            // user, 2026-10-26: "the color got too strong, make it darker", then "80%, the green 70%"
            // Talk's deeper blue is darkened less, or its letters sink into the graphite.
            var dark = k == 2 ? 0.7f : k == 0 ? 0.95f : 0.8f;
            return new Color(a[0] * dark, a[1] * dark, a[2] * dark, 1f);
        }

        private static void ClearCatBar()
        {
            foreach (var g in CatMade) if (g != null) UnityEngine.Object.Destroy(g);
            CatMade.Clear();
            _barDy = 0f;
            CatButtons.Clear();
            CatTexts.Clear();
        }

        private static void CatClick(string name)
        {
            try
            {
                if (_scene == null) return;
                if (GiftMenu.IsOpen && !TalkBusyLine()) GiftMenu.Close();   // a category click puts the gift menu away
                if (Busy("category '" + name + "'")) return;
                Sfx.Select();
                if (RootOpen && _cat == name) { _cat = null; SetOpen(false); return; }
                _cat = name;
                _sub = null;
                SetOpen(true);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        internal static void OpenCategory(string name)
        {
            var n = CatNames.FirstOrDefault(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase) || x.StartsWith(name, StringComparison.OrdinalIgnoreCase));
            var ci = Array.FindIndex(CatCaptions, x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
            if (n == null) n = ci >= 0 ? CatNames[ci] : name;   // the caption works too ("Together" is "Do together")
            if (RootOpen && _cat == n) return;
            CatClick(n);
        }

        /// <summary>Greys the buttons with nothing now, gilds the caption of one with something urgent, marks the open one.</summary>
        private static void RefreshCatBar(List<KeyValuePair<string, List<UEntry>>> cats)
        {
            for (var i = 0; i < CatButtons.Count && i < CatNames.Length; i++)
            {
                var b = CatButtons[i];
                if (b == null) continue;
                var c = cats.FirstOrDefault(x => x.Key == CatNames[i]);
                var has = c.Value != null && c.Value.Count > 0;
                var hot = has && c.Value.Any(e => e.Label.IndexOf("★", StringComparison.Ordinal) >= 0 || e.Label.IndexOf("#ff9b6a", StringComparison.Ordinal) >= 0);
                var on = has && RootOpen && _cat == CatNames[i];
                b.interactable = has;
                try
                {
                    b.image.sprite = ConvArt.Tile(i, on);
                    b.transform.localScale = on ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;
                }
                catch { }
                if (i < CatTexts.Count && CatTexts[i] != null)
                    CatTexts[i].color = !has ? new Color(0.6f, 0.6f, 0.6f, 1f) : hot ? new Color(1f, 0.82f, 0.35f, 1f) : Color.white;
            }
        }

        /// <summary>
        /// About once a second while the list is closed: which categories have something now, so the buttons show it before
        /// the player opens any. Nothing is added to the screen, and the game's screen is not asked to redraw.
        /// </summary>
        private static void TickCatBar()
        {
            KeepTalkTile();
            if (!Unified || CatButtons.Count == 0 || RootOpen || Scenes.Running || Time.realtimeSinceStartup < _catRefreshAt) return;
            _catRefreshAt = Time.realtimeSinceStartup + 1f;
            try
            {
                if (Sess == null) return;
                bool noTime;
                RefreshCatBar(ComputeCats(false, out noTime));
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Category buttons: " + e.Message); }
        }

        /// <summary>
        /// The game's TalkScene.activeTalkContents setter puts its own speech bubble back on the Talk button every time
        /// it opens or closes the list (a scene with center choices does), so the first button came out in the old style
        /// beside our four. Checked every frame: a sprite compare, nothing more.
        /// </summary>
        private static void KeepTalkTile()
        {
            if (!Unified || CatButtons.Count == 0) return;
            var b = CatButtons[0];
            if (b == null || b.image == null) return;
            var sp = b.image.sprite;
            if (sp == ConvArt.Tile(0, false) || sp == ConvArt.Tile(0, true)) return;
            try { ConvArt.Style(b, ConvArt.Tile(0, RootOpen && _cat == CatNames[0]), false); b.image.preserveAspect = true; }
            catch { }
        }

        private static KeyValuePair<string, List<UEntry>> Cat(string name, List<UEntry> entries, bool noTime)
        {
            return new KeyValuePair<string, List<UEntry>>(name, entries.Where(e => e != null && (!noTime || !e.Costs)).ToList());
        }

        private static bool _goodbye;
        private static readonly System.Reflection.MethodInfo UpdateUiMethod = HarmonyLib.AccessTools.Method(typeof(TalkScene), "UpdateUI");

        internal static void RefreshList()
        {
            if (_scene == null || _root == null || Scenes.Running) { LastList = new List<string> { "(no list: " + (_scene == null ? "no talk" : Scenes.Running ? "a scene is playing" : "not ours") + ")" }; return; }
            _sub = null;
            SetOpen(true);
        }

        // ---------------- Talk ----------------

        private static void BuildTalk(Session s, SaveData.Heroine h, GirlState gs, List<UEntry> l)
        {
            // The procedural topics (Conversation.Proc): what she knows makes the list; the old fixed questions (her day,
            // a friend, school, the single "opener") are gone.
            var stranger = h.relation < 0;
            if (stranger) l.Add(E("Introduce yourself", () => Introduce(h)));
            else
            {
                // A request she has not told anyone yet only comes out by asking (the board shows the known ones).
                var hidden = Board.HiddenFor(GirlInfo.Key(h)).OrderBy(r => r.DeadlineDay).FirstOrDefault();
                if (hidden != null) l.Add(E("Ask what's on her mind", () => RequestTalk(h, hidden, true)));
                // A step of her story the director has due (a pair, a crush, a friend scene).
                var arc = SubjectFrom(h, x => x.Id == "torn" || x.Id == "couple" || x.Id == "crush" || x.Id == "reveal" || x.Id == "feelings" || x.Id.StartsWith("tri_", StringComparison.Ordinal)
                                              || !string.IsNullOrEmpty(gs.Pending) && gs.Pending.EndsWith(":" + x.Id, StringComparison.Ordinal));
                if (arc != null) l.Add(E("Continue " + ArcLabel(h, arc), () => PlaySubject(h, arc, Lvl.Friend)));
            }
            foreach (var t in TalkTopics(h, gs))
            {
                var topic = t;
                l.Add(E(topic.Hot ? "<color=#ff9b6a>" + topic.Label + "</color>" : topic.Label, () => PlayTopic(h, topic)));
            }
            var g = stranger ? Social.OwnGrudge(h) : Social.Grudge(h);
            var all = stranger ? null : OpenGrudges(h, s);
            if (all != null && all.Count >= 2) l.Add(E("<color=#ff9b6a>Clear the air about everything</color>", () => ClearAirAll(h, all)));
            else if (g != null && !s.Did.Contains("cleared:" + g.Id)) l.Add(E("<color=#ff9b6a>Clear the air about " + IncidentLabel(h, g) + "</color>", () => ClearAir(h, g)));
            if (h.isAnger) l.Add(E("Apologize", () => Game(10)));
            var red = RedemptionEntry(h, gs);
            if (red != null) l.Add(red);
        }

        // ---------------- Actions ----------------

        private static void BuildActions(Session s, SaveData.Heroine h, GirlState gs, List<UEntry> l)
        {
            if (Cfg.GiftsEnabled.Value && CanGift(h, gs))
                l.Add(E("Give a gift", () => { var t = Created.Select(c => c.GetComponent<Button>()).FirstOrDefault(b => b != null && b.interactable); if (t == null) return; GiftMenu.Open(t, h); }, false));
            var req = Board.CompletableInTalk(h);
            if (req != null) l.Add(E("<color=#FFD27A>★</color> " + Board.ButtonLabel(req), () => { var r = Board.CompletableInTalk(h); if (r != null) { Hide(); Board.PlayInTalk(r, h); } }, false));
            var msg = MessageFor(h);
            if (msg != null) l.Add(E("<color=#FFD27A>★</color> " + msg.Button, () => { var r = MessageFor(h); if (r != null) { Hide(); PassMessage(r, h); } }, false));
            JobActions(s, h, l);
        }

        /// <summary>What you can only do with her because of her job.</summary>
        private static void JobActions(Session s, SaveData.Heroine h, List<UEntry> l)
        {
            if (h.relation < 0) return;
            var job = Teachers.Job(h);
            Action<string, string, string, List<Resp>> add = (label, me, her, rs) =>
            {
                var key = "job:" + label;
                if (!s.Did.Contains(key)) l.Add(E(label, () => { s.Did.Add(key); Beat(h, Lvl.Friend, key, b => { b.Me(me); b.Her_(her); }, rs); }));
            };
            if (job == "nurse")
            {
                add("Say you don't feel well", "Sensei, I don't feel so good.", T(h, "Sit down, let me see. ...No fever. When did you last sleep? || Sit down. ...You're not warm. Did you sleep?", "Sit, sit! ...No fever! Did you sleep at all?! || Over here! ...Hm, no fever! Sleep?", "Sit. ...No fever. How much sleep? || Sit. Sleep?", "Hm. Let me guess. Sleep, food, or a test? || Sit. Is this a real one, or a test one?"), new List<Resp>
                {
                    Rp("Admit you stayed up too late", "jn_sleep", Q.Good, Tn("Thank you for being honest. Lie down for ten minutes.", "Knew it! Ten minutes, on the bed, go!", "Ten minutes. Then bed early tonight.", "Honest patients get the good pillow. Ten minutes.")),
                    Rp("Say it's just a headache", "jn_head", Q.Neutral, Tn("Then drink this, and rest your eyes for a bit.", "Water! Now! Then we'll see!", "Water. Come back if it stays.", "Mm. Water, then. The cure for everything.")),
                    Rp("Admit you wanted to skip class", "jn_skip", Q.Bad, Tn("Oh... at least you told me. Back to class, please.", "Nice try! Back you go!", "No. Class.", "I appreciate the honesty. Out.")),
                });
                add("Ask for a bandage", "Sensei, could I get a bandage?", T(h, "Of course. Let me see it. ...Just a scrape. Hold still. || Show me. ...It's small. Hold still.", "Sure! Show me! ...Ooh, a scrape! Hold still! || Show me! Don't move!", "Show me. ...Small. Hold still. || Hold still.", "Show me. ...You'll live. Hold still. || You'll live. Don't move."), new List<Resp>
                {
                    Rp("Thank her properly", "jn_thanks", Q.Good, Tn("You're welcome. Keep it clean.", "Anytime! Keep it dry!", "Keep it clean.", "No charge. This time.")),
                    Rp("Say it barely hurts", "jn_tough", Q.Neutral, Tn("Good. Tell me if it swells.", "Tough guy! Tell me if it swells!", "Tell me if it swells.", "So brave. Tell me if it swells.")),
                });
                if (h.relation >= 1)
                    add("Offer to help restock", "Do you need a hand with anything, sensei?", T(h, "Actually, yes. These boxes go on the top shelf. || Yes, please. The top shelf, if you can reach. ", "Yes! Boxes! Top shelf! Go! || Yes! Those boxes, up there!", "The top shelf. Those boxes. || Those. Up there.", "I was hoping someone tall would ask. Top shelf. || Top shelf, please. You volunteered."), new List<Resp>
                    {
                        Rp("Put the boxes away", "jn_boxes", Q.Good, Tn("Thank you. That saves me a lot of trouble.", "You're the best! Thanks!", "Thanks. That helps.", "You can come back. Healthy, even.")),
                        Rp("Ask what everything is for", "jn_what", Q.Good, Tn("You're curious. This one is for sprains, and this one for burns.", "Ooh, a student of medicine! Sprains here, burns there!", "Sprains. Burns. Don't mix them up.", "Want my job? Sprains here, burns there.")),
                    });
            }
            else if (job == "teacher")
            {
                add("Ask about today's lesson", "Sensei, could I ask about today's lesson?", T(h, "Of course. Which part was hard? || Of course. Where did you get lost?", "Yes! A question! Which part?! || Finally! Which part?!", "Go ahead. Which part? || Which part?", "You were listening? Which part? || A question? From you? Go on."), new List<Resp>
                {
                    Rp("Ask a real question", "jt_real", Q.Good, Tn("That's a good question. Let me show you.", "Yes! That's the stuff! Look here!", "Good. Look at this part again.", "So you were awake. Look here.")),
                    Rp("Admit you lost track early", "jt_lost", Q.Neutral, Tn("Thank you for telling me. Let's start from the top.", "From the top, then! We'll fix it!", "From the top, then.", "How early? No, don't tell me. From the top.")),
                });
                add("Offer to carry her papers", "Can I carry those for you, sensei?", T(h, "Really? Thank you. Just to the faculty room. || Oh, thank you. To the faculty room, please.", "My hero! Faculty room, go! || Yes! Take half!", "...To the faculty room, then. || Fine. Faculty room.", "Are you after something? ...Faculty room, please. || Sucking up? I accept."), new List<Resp>
                {
                    Rp("Just carry them", "jt_carry", Q.Good, Tn("Thank you. You're very kind.", "Thanks! You rock!", "Thank you.", "Hm. No catch. I'm impressed.")),
                    Rp("Ask if your test is in the pile", "jt_test", Q.Neutral, Tn("You'll find out tomorrow, okay?", "No spoilers!", "...Tomorrow.", "So there was a catch.")),
                });
            }
        }

        private static bool CanGift(SaveData.Heroine h, GirlState gs)
        {
            var gifts = Items.All.Where(i => i.Type == ItemType.Gift && S.Count(i.Id) > 0).ToList();
            if (gifts.Count == 0) return false;
            return gs.LastGiftDay != S.Day || gifts.Any(i => Board.WantsGift(h, i));
        }

        // ---------------- Do together (the game's own invitations) ----------------

        private static void BuildTogether(List<UEntry> l)
        {
            if (GameAllows(4)) l.Add(E("Have lunch together", () => Invite(4, "lunch")));
            if (GameAllows(5)) l.Add(E("Do club activities together", () => Invite(5, "club")));
            if (GameAllows(6)) l.Add(E("Go home together", () => Invite(6, "home")));
            if (GameAllows(8)) l.Add(E("Study together", () => Invite(8, "study")));
            if (GameAllows(9)) l.Add(E("Exercise together", () => Invite(9, "exercise")));
            if (GameAllows(10)) l.Add(E("Invite her to the Koikatsu Club", () => Invite(10, "club")));
            if (GameAllows(11)) l.Add(E("Come with me", () => Invite(11, "follow")));
        }

        /// <summary>
        /// The game's invitation, answered by the director (Consent): where the game has its own "no" scene, the director's
        /// answer is played through it; where it always agrees (her girlfriend stage), a no is our "not now".
        /// </summary>
        private static void Invite(int command, string what)
        {
            var h = Sess != null ? Sess.Her : null;
            var ask = Consent.OfCommand(command);
            if (h != null && what == "h" && Consent.DecideH(h) == "wary_no") { WaryNoH(h); return; }
            if (h != null && ask.HasValue && Consent.GameHasNoRefusal(h, command) && !Consent.Decide(h, ask.Value)) { NotNow(h, what); return; }
            Game(10 + command);
        }

        /// <summary>The game's own "Invite her to H" with a wary no: she says what held her back, like "Sneak off together".</summary>
        private static void WaryNoH(SaveData.Heroine h)
        {
            var b = new SceneBuilder(h, true);
            b.Narr(GirlInfo.FirstName(h) + " hesitates a moment too long before she answers.");
            b.Face(h, Feel.Shy).Her_(HeldBackLine(h, Consent.HeldBack(h)));
            Play(b, 0, null);
        }

        // ---------------- Romance ----------------

        /// <summary>Romance can land: she knows you, likes you a little (or is in a flirty mood) and is not angry.</summary>
        internal static bool RomanceOpen(SaveData.Heroine h)
        {
            return h.relation >= 0 && !h.isAnger && (h.isGirlfriend || h.favor >= 25 || MoodOf(h) == CampusLife.Mood.Flirty);
        }

        /// <summary>
        /// The director's answer to a romance or spicy line: "yes",
        /// "wary_yes" (she takes it, flustered), "wary_no" (she wanted to, something held her back) or "no".
        /// </summary>
        private static string FlirtAnswer(SaveData.Heroine h, bool spicy, string what)
        {
            var s = Sess;
            return Consent.Flirt(h, spicy, what, Onlookers(h).Count, s != null && s.Did.Contains("refused"));
        }

        private static bool Took(string a) { return a == "yes" || a == "wary_yes"; }

        private static Q QOf(string a) { return Took(a) ? Q.Good : a == "wary_no" ? Q.Neutral : Q.Bad; }

        /// <summary>A wary yes: a flustered beat before her line.</summary>
        private static string WaryLead(SaveData.Heroine h)
        {
            return T(h, "Eh... um... || A-ah... || Um... || Oh... um...", "W-whoa, okay... || Eh?! Um... || H-hey... || Uh... wow...", "...Hm. || ...... || ...Well. || ...Oh.", "W-well... || Hmph... || Oh... || My, my...") + " ";
        }

        /// <summary>A wary no: she wanted to take it and something held her back; flustered, never cold.</summary>
        private static string WaryNoLine(SaveData.Heroine h, bool spicy)
        {
            return spicy
                ? T(h, "...I-I can't answer that. Not yet. Please don't stop liking me. || ...My face is burning. Let's... not, for now. || ...I want to say something back. I just can't. Not yet. || ...Ask me again some other time. Please.",
                    "Ahh, you can't just SAY stuff like that! ...Ask me later, okay?! || Wh-what am I supposed to say to that?! Later! || My brain just stopped! Give me a minute! || Not fair! ...Ask me again sometime.",
                    "...Don't say things like that here. ...Not that I hated it. || ...Not now. Ask me again. || ...I'm not answering that. Yet. || ...You'll have to wait for that one.",
                    "Oh, you're tempting me. ...Not today. || Mm, nice try. Ask me another day~ || Careful, I almost said yes. || Not yet. Keep trying, though~")
                : RomanceSay(h, "wary", "no");
        }

        private static void BuildRomance(Session s, SaveData.Heroine h, GirlState gs, List<UEntry> l)
        {
            var trait = ComplimentTrait(h, gs);
            if (trait != null) l.Add(E("Compliment her on " + trait[0], () => ComplimentU(h, trait)));
            l.Add(E("Hold her hand", () => HoldHandU(h)));
            l.Add(E("Sweet-talk her", () => SweetTalk(h, gs)));
            l.Add(E("Ask what I mean to her", () => AskMeaning(h)));
            l.Add(E("Ask her on a date", () => { if (GameAllows(7)) Invite(7, "date"); else NotNow(h, "date"); }));
            if (!h.isGirlfriend) l.Add(E("Confess your feelings", () => { if (GameAllows(1)) Invite(1, "confess"); else NotNow(h, "confess"); }));
            var rival = Social.JealousOf(h);
            if (rival != null || s.Did.Contains("jealous") && !s.Did.Contains("plans"))
                l.Add(E("<color=#ff9b6a>Talk about her jealousy" + (rival != null ? " of " + GirlInfo.FirstName(rival) : "") + "</color>", () => Jealousy(h, rival)));
            var harem = Harem.Subject(h);
            if (harem != null) l.Add(E("Talk about sharing me", () => PlaySubject(h, harem, Lvl.Romance)));
            if (h.isGirlfriend) l.Add(E("<color=#9a9a9a>Break up</color>", () => BreakUp(h)));
        }

        // ---------------- Spicy ----------------

        /// <summary>Spicy can land: she knows you, is not angry or put off today, and is flirty, lewd enough or your girlfriend.</summary>
        private static bool SpicyOpen(Session s, SaveData.Heroine h)
        {
            return h.relation >= 0 && !h.isAnger && !s.Did.Contains("refused") && (MoodOf(h) == CampusLife.Mood.Flirty || h.lewdness >= 25 || h.isGirlfriend);
        }

        private static void BuildSpicy(Session s, SaveData.Heroine h, GirlState gs, List<UEntry> l)
        {
            var ctx = TeaseContext(h);
            if (ctx != null) l.Add(E("Tease her about " + ctx[0], () => TeaseU(h, ctx)));
            l.Add(E("Whisper something suggestive", () => WhisperU(h)));
            l.Add(E("Flirt more boldly", () => FlirtBold(h)));
            var mem = PrivateMemory(h, gs);
            if (mem != null) l.Add(E("Bring up " + mem[0], () => PrivateMemoryU(h, mem)));
            l.Add(E("Invite her to H", () => { if (GameAllows(3)) Invite(3, "h"); else NotNow(h, "h"); }));
            string sneakWhy;
            if (GameAllows(3) && SpicyOpen(s, h)) l.Add(E("Sneak off together" + Odds(Consent.Chance(h, Ask.H, out sneakWhy)), () => SneakU(h)));
            if (gs.OwesH > 0) l.Add(E("Settle our intimate debt", () => Debt(h, gs)));
            if (gs.Caught > gs.CaughtTalked) l.Add(E("Address being caught", () => Caught(h, gs)));
        }

        // ================= the contextual opener =================

        /// <summary>
        /// The first thing that applies, in this order: her request, a hurt or rumor she knows, a step of her story, a memory
        /// she is ready to bring up, her mood or need, a school event, the place you are in. Always one; never generic.
        /// </summary>
        private static UEntry Opener(Session s)
        {
            var h = s.Her;
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            var tone = GirlInfo.ToneOf(h);

            // "Talk about what she heard about Rin"). Only a hurt of her own, else the place.
            if (h.relation < 0)
            {
                var own = Social.OwnGrudge(h);
                if (own != null && !s.Did.Contains("cleared:" + own.Id)) return E("Talk about " + IncidentLabel(h, own), () => ClearAir(h, own));
                var spot = PlaceThing();
                return E("Comment on " + spot, () => PlaceTalk(h, spot));
            }

            // 1. Her request.
            var done = Board.CompletableInTalk(h);
            var mine = Board.ActiveFor(key).Where(r => r.DeadlineDay >= S.Day && r != done).OrderBy(r => r.DeadlineDay).FirstOrDefault();
            if (mine != null) return E("Ask her about " + Lower(mine.Title), () => RequestTalk(h, mine, false));
            var hidden = Board.HiddenFor(key).OrderBy(r => r.DeadlineDay).FirstOrDefault();
            if (hidden != null && h.relation >= 0) return E("Ask what's on her mind", () => RequestTalk(h, hidden, true));

            // 2. A hurt or a rumor she knows.
            var all = OpenGrudges(h, s);
            if (all.Count >= 2) return E("Clear the air about everything", () => ClearAirAll(h, all));
            var g = Social.Grudge(h);
            if (g != null && !s.Did.Contains("cleared:" + g.Id)) return E("Talk about " + IncidentLabel(h, g), () => ClearAir(h, g));
            var rumor = SubjectFrom(h, x => x.Id.StartsWith("rumor_", StringComparison.Ordinal));
            if (rumor != null) return E("Talk about the rumor going around", () => PlaySubject(h, rumor, Lvl.Friend));
            var gossip = Social.Gossip(h);
            if (gossip != null) return E("Talk about what she heard about " + GossipWho(gossip), () => GossipTalk(h, gossip));

            // 3. A step of her story (a pair, a friend scene, something she has been waiting to say).
            var arc = SubjectFrom(h, x => x.Id == "torn" || x.Id == "couple" || x.Id == "crush" || x.Id == "reveal" || x.Id == "feelings" || x.Id.StartsWith("tri_", StringComparison.Ordinal)
                                          || !string.IsNullOrEmpty(gs.Pending) && gs.Pending.EndsWith(":" + x.Id, StringComparison.Ordinal));
            if (arc != null) return E("Continue " + ArcLabel(h, arc), () => PlaySubject(h, arc, Lvl.Friend));

            // 4. A memory she is ready to bring up.
            var m = gs.Moments.Where(x => x.Pending && x.Tone >= 0 && S.Day - x.Day <= 14).OrderByDescending(x => x.Id).FirstOrDefault()
                    ?? CampusLife.Memories.Last(h, 7);
            if (m != null && m.Day < S.Day && m.Tone >= 0 && h.relation >= 0) return E("Bring up " + MemoryTitle(m), () => MemoryTalk(h, m));

            // 5. Her mood or a need you can see.
            var mood = MoodOf(h);
            var seems = SeemsWord(h, mood);
            if (seems != null) return E("Ask why she seems " + seems, () => MoodTalk(h, mood, seems));

            // 6. A school event, soon.
            var ev = Calendar.UpcomingEvents().FirstOrDefault(u => u.Days <= 3);
            if (ev != null) return E("Talk about the " + ev.Name, () => EventTalk(h, ev.Name, ev.When));

            // 7. The place itself.
            var place = PlaceThing();
            return E("Comment on " + place, () => PlaceTalk(h, place));
        }

        private static string Lower(string t) { return string.IsNullOrEmpty(t) ? t : char.ToLowerInvariant(t[0]) + t.Substring(1); }

        private static Subject SubjectFrom(SaveData.Heroine h, Func<Subject, bool> pick)
        {
            try { return SceneTalk.Candidates(h).Where(x => x.Form != Form.Aside && pick(x)).OrderByDescending(x => x.Weight).FirstOrDefault(); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Opener subjects: " + e.Message); return null; }
        }

        private static string ArcLabel(SaveData.Heroine h, Subject x)
        {
            var partner = Pairs.PartnerOf(h);
            var pn = partner != null ? GirlInfo.FirstName(partner) : "her";
            switch (x.Id)
            {
                case "torn": return "her talk about " + pn + " and you";
                case "couple": return "what's going on with " + pn;
                case "crush": return "what she said about " + pn;
                case "reveal": return "what she wanted to tell you";
                case "feelings": return "what she's been feeling";
                default: return x.Id.StartsWith("tri_", StringComparison.Ordinal) ? "what happened with the others" : "where you left off";
            }
        }

        private static string GossipWho(Incident i)
        {
            var about = Social.Get(i.Girl);
            return about != null ? GirlInfo.FirstName(about) : "you";
        }

        private static string MemoryTitle(Moment m)
        {
            switch (m.Kind)
            {
                case "walk": return "your walk " + CampusLife.Memories.When(m);
                case "sunday": return "last Sunday";
                case "favor": return "the favor " + CampusLife.Memories.When(m);
                case "tradition": return "your tradition";
                case "quiet": return "how she was feeling " + CampusLife.Memories.When(m);
                default: return "what happened " + CampusLife.Memories.When(m);
            }
        }

        private static string SeemsWord(SaveData.Heroine h, Mood mood)
        {
            switch (mood)
            {
                case CampusLife.Mood.Stressed: return "tense";
                case CampusLife.Mood.Upset: return "upset";
                case CampusLife.Mood.Lonely: return "lonely";
                case CampusLife.Mood.Sleepy: return "tired";
                case CampusLife.Mood.Hungry: return "hungry";
                case CampusLife.Mood.Bored: return "bored";
                case CampusLife.Mood.Cheerful: return "so happy";
                case CampusLife.Mood.Flirty: return "in such a good mood";
            }
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.NeedRest < 30) return "tired";
            if (gs.NeedFood < 25) return "hungry";
            if (gs.NeedSocial < 25) return "lonely";
            return null;
        }

        private static string PlaceThing()
        {
            switch (Places.CurrentId)
            {
                case "library": return "how quiet the library is";
                case "gym": return "the noise in the gym";
                case "field": return "the track team out on the field";
                case "pool": return "the smell of chlorine by the pool";
                case "cafeteria": return "the line at the cafeteria";
                case "rooftop": return "the view from the rooftop";
                case "classroom": return "the notes left on the board";
                case "courtyard": return "the flowers in the courtyard";
                case "park": return "the trees in the park";
                case "clubroom": return "the mess in the club room";
            }
            var id = Places.CurrentId;
            return string.IsNullOrEmpty(id) ? "the weather today" : "the " + Places.Name(id).ToLowerInvariant();
        }

        // ================= beats, rewards, repetition =================

        /// <summary>
        /// "balancear direito o afeto para não encher tão rápido ... não colocar um limite artificial de piso de dias"):
        /// with the daily curve (G.AddFavor) the days come out of the affection itself, measured with pacesim.
        /// </summary>
        private static int[] Table(Lvl l, Q q)
        {
            switch (l)
            {
                case Lvl.Friend: return q == Q.Good ? new[] { 5, 0, 2 } : q == Q.Neutral ? new[] { 2, 0, 0 } : new[] { -2, 0, -1 };
                case Lvl.Romance: return q == Q.Good ? new[] { 4, 3, 2 } : q == Q.Neutral ? new[] { 2, 2, 0 } : new[] { -2, -1, -1 };
                default: return q == Q.Good ? new[] { 2, 6, 1 } : q == Q.Neutral ? new[] { 1, 3, 0 } : new[] { -2, -1, 0 };
            }
        }

        internal static int[] GoodTable(int level) { return Table((Lvl)level, Q.Good); }

        /// <summary>
        /// Pays one beat. The same beat with the same girl on the same day (same key: a new memory, request or place is a
        /// new key) pays 100%, then 50%, then nothing; losses always count.
        /// </summary>
        private static void Pay(Session s, Lvl lvl, Q q, string repKey)
        {
            var h = s.Her;
            var t = Table(lvl, q);
            if (_trio != null) { _trio.LastLvl = lvl; _trio.LastQ = q; _trio.LastTo = h; }
            // Her rhythm (reserved, normal, open; the day's momentum; the spotlight) scales what is gained, not what is lost.
            var factor = RepeatFactor(h, repKey) * Rhythm.Gain(h, (int)lvl);
            if (q == Q.Good) Rhythm.OnGood(h);
            Func<int, int> up = v => v > 0 ? Mathf.RoundToInt(v * factor) : v;
            int favor = up(t[0]), lewd = up(t[1]), trust = up(t[2]);
            if (Cfg.DevotionEnabled.Value && favor > 0) Devotion.AddTalk(h, Devotion.Overflow(h, favor));
            var before = h.favor;
            var beforeH = h.lewdness;
            G.AddFavor(h, favor, lewd);
            if (trust != 0) Social.AddTrust(h, trust);
            s.FavorGained += h.favor - before;
            s.LewdGained += h.lewdness - beforeH;
            CampusLifePlugin.Log.LogInfo("Talk beat: " + GirlInfo.FirstName(h) + " " + repKey + " " + lvl + "/" + q + " x" + factor.ToString("0.0") + " -> favor " + favor + ", H " + lewd + ", trust " + trust);
        }

        private static string RepKey(SaveData.Heroine h, string key) { return "rep:" + GirlInfo.Key(h) + ":" + key; }

        private static float RepeatFactor(SaveData.Heroine h, string key)
        {
            var k = RepKey(h, key);
            int v;
            var n = S.Buffs.TryGetValue(k, out v) && v / 10 == S.Day ? v % 10 : 0;
            S.Buffs[k] = S.Day * 10 + Math.Min(9, n + 1);
            return n == 0 ? 1f : n == 1 ? 0.5f : 0f;
        }

        private static int _prunedDay = -1;

        private static void PruneRepeats()
        {
            if (_prunedDay == S.Day) return;
            _prunedDay = S.Day;
            foreach (var k in S.Buffs.Keys.Where(k => k.StartsWith("rep:", StringComparison.Ordinal) && S.Buffs[k] / 10 != S.Day).ToList()) S.Buffs.Remove(k);
            foreach (var k in S.Buffs.Keys.Where(k => (k.StartsWith("fday:", StringComparison.Ordinal) || k.StartsWith("hday:", StringComparison.Ordinal)) && S.Buffs[k] / 1000 != S.Day).ToList()) S.Buffs.Remove(k);
        }

        private static string[] Stock(Q q)
        {
            switch (q)
            {
                case Q.Good: return Tn("...Thank you. That really helps.", "Yeah! Exactly! You get it!", "...Yeah. Thanks.", "Heh. You're better at this than I thought.");
                case Q.Neutral: return Tn("Mm... I guess so.", "Huh. Okay!", "...Sure.", "If you say so.");
                default: return Tn("...Oh. Okay.", "Hey... that's not really what I meant.", "...That didn't help.", "Wow. Read the room.");
            }
        }

        private static Feel FaceFor(Q q) { return q == Q.Good ? Feel.Smile : q == Q.Neutral ? Feel.Normal : Feel.Pout; }

        /// <summary>
        /// One beat with answers: the scene (intro), the answers as a choice, her reply to each, then the pay for the one
        /// taken, its own effect, the blue bar and the list again.
        /// </summary>
        private static void Beat(SaveData.Heroine h, Lvl lvl, string repKey, Action<SceneBuilder> intro, List<Resp> rs, Action<Resp> after = null)
        {
            var s = Sess;
            if (s == null) return;
            var b = new SceneBuilder(h, true);
            intro(b);
            var tone = (int)GirlInfo.ToneOf(h);
            if (rs != null && rs.Count > 0)
            {
                var args = new List<string>();
                foreach (var r in rs) { args.Add(r.Label); args.Add(r.Tag); }
                b.Choice(args.ToArray());
                foreach (var r in rs)
                {
                    b.Branch(r.Tag);
                    b.Face(h, r.Face ?? FaceFor(r.Q)).Her_(Fill((r.Her ?? Stock(r.Q))[tone], h));
                    b.End();
                }
            }
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                Resp r = rs != null ? rs.FirstOrDefault(x => x.Tag == c) : null;
                try
                {
                    if (r != null) Pay(s, lvl, r.Q, repKey);
                    if (r != null && r.Done != null) r.Done();
                    if (after != null) after(r);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                AfterBeat(s);
            }, false);
        }

        /// <summary>A beat with no answers: the result is already decided (she takes your hand or not).</summary>
        private static void Plain(SaveData.Heroine h, Lvl lvl, Q q, string repKey, SceneBuilder b, string toast, Action done = null, bool pay = true)
        {
            var s = Sess;
            if (s == null) return;
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                try
                {
                    if (pay) Pay(s, lvl, q, repKey);
                    if (done != null) done();
                    if (!string.IsNullOrEmpty(toast)) Toasts.Show(toast);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                AfterBeat(s);
            }, false);
        }

        private static void AfterBeat(Session s)
        {
            if (s == null || s.Her == null) return;
            s.Turns++;
            SpendTalkTime(s.Her);
            if (FriendsNow(s.Her)) return;
            _sub = null;
            if (TrioAfter()) return;   // with both girls: the other one reacts, they talk, or the moment ends
            Reopen();   // the list again; with the blue bar empty it keeps only gifts and requests, or says goodbye
        }

        /// <summary>A scene of the director's (a pair, a rumor, the harem talk) from the list.</summary>
        private static void PlaySubject(SaveData.Heroine h, Subject x, Lvl lvl)
        {
            var s = Sess;
            Hide();
            if (!SceneTalk.Play(h, x, d => { if (s != null) s.Interest = Mathf.Clamp(s.Interest + d, 0, MaxInterest); }, () => AfterBeat(s))) Reopen();
        }

        // ================= Talk beats =================

        private static void Introduce(SaveData.Heroine h)
        {
            var name = GirlInfo.FirstName(h);
            var place = PlaceThing();
            Beat(h, Lvl.Friend, "intro", b =>
            {
                b.Me(Lines.Pick("Hi. I don't think we've really talked yet. I'm " + EventUtilsPlayerName() + ".", "Hey, I'm " + EventUtilsPlayerName() + ". I see you around a lot."));
                b.Her_(T(h, "Oh... h-hello. I'm " + name + ". || Um... hello. My name's " + name + ". || H-hi... I'm " + name + ". Nice to meet you. || Oh... hello. I'm " + name + ".", "Hi hi! I'm " + name + "! Nice to meet you! || Hey! " + name + ", that's me! || Yo! I'm " + name + "! Let's be friends! || Hello hello! " + name + " here!", "..." + name + ". Hello." + " || " + name + ". || ...I'm " + name + ". || Hello. " + name + ".", "Oh? Finally saying hi? I'm " + name + ". || Took you long enough. " + name + ". || Finally. I'm " + name + ". || Oh, hello. " + name + ". Remember it."));
            }, new List<Resp>
            {
                Rp("Say it's nice to finally meet her", "in_nice", Q.Good, Tn("...Nice to meet you too.", "Same here! Let's be friends!", "...Likewise.", "Smooth. I'll allow it.")),
                Teachers.IsTeacherVoice(h) ? Rp(Teachers.Job(h) == "nurse" ? "Ask how long she's been the nurse" : "Ask what she teaches", "in_job", Q.Good, Teachers.Job(h) == "nurse" ? Tn("A few years now. I hope you won't need me.", "Years! Try not to visit me!", "A few years.", "Long enough to know a fake fever.") : Tn("I'm happy to tell you. You'll see in class.", "You'll find out in class!", "You'll see.", "Come to class and find out.")) : Rp("Ask what class she's in", "in_class", Q.Good, Tn("Um, I'm in your year... I sit by the window.", "Same year as you! You never noticed?!", "Same year. Window seat.", "Same year. You really never noticed me? Ouch.")),
                Rp("Joke about " + place, "in_joke", GirlInfo.ToneOf(h) == Tone.Cool ? Q.Neutral : Q.Good, Tn("Hehe... that's true.", "Ahaha! Right?!", "...Hm. Fair.", "Heh. You're funny. A little.")),
            });
        }

        private static string EventUtilsPlayerName()
        {
            try { var p = KKAPI.MainGame.EventUtils.GetPlayerData(); return p != null && !string.IsNullOrEmpty(p.firstname) ? p.firstname : "the new guy"; }
            catch { return "the new guy"; }
        }

        private static string DayLabel(SaveData.Heroine h, GirlState gs)
        {
            var mood = MoodOf(h);
            if (gs.MoodKnownDay == S.Day && mood != CampusLife.Mood.None)
            {
                var w = SeemsWord(h, mood);
                if (w != null) return "Ask why she looks " + w;
            }
            if (!GirlInfo.InNoClub(h) && (G.CurrentMask == PeriodMask.Club || G.CurrentMask == PeriodMask.After)) return "Ask how " + GirlInfo.ClubName(h) + " went today";
            return "Ask about her day";
        }

        private static bool Low(Mood m) { return m == CampusLife.Mood.Stressed || m == CampusLife.Mood.Upset || m == CampusLife.Mood.Lonely || m == CampusLife.Mood.Sleepy; }

        private sealed class SchoolTopicInfo { public string Id, Label, Line; }

        /// <summary>Something real going on at school that she knows about (an event soon, the council race, the club project).</summary>
        private static SchoolTopicInfo SchoolTopic(SaveData.Heroine h)
        {
            var ev = Calendar.UpcomingEvents().FirstOrDefault(u => u.Days <= 7);
            if (ev != null) return new SchoolTopicInfo { Id = "ev:" + ev.Id, Label = Calendar.EventRef(ev.Id), Line = Calendar.EventRef(ev.Id) + " " + ev.Spoken };
            var c = S.Council;
            if (c != null && c.Week == S.Week && c.Stage < 4) return new SchoolTopicInfo { Id = "council", Label = "the council race", Line = "the council race between " + GirlInfo.FirstName(c.A) + " and " + GirlInfo.FirstName(c.B) };
            var p = S.Project;
            if (p != null && p.Stage < 3 && !string.IsNullOrEmpty(p.Type)) return new SchoolTopicInfo { Id = "project", Label = "the club project", Line = "the club's " + ClubProjects.TypeName(p.Type).ToLowerInvariant() };
            if (Rumors.Hottest() != null) return new SchoolTopicInfo { Id = "rumors", Label = "what everyone's whispering about", Line = "the rumor everyone's whispering about" };
            return null;
        }

        // ---------------- clearing the air (an important scene: five answers) ----------------

        /// <summary>The incident in a few words, from her side: whether it happened to her or she heard about it.</summary>
        private static string IncidentLabel(SaveData.Heroine h, Incident i)
        {
            var about = Social.Get(i.Girl);
            var who = about == null ? "" : about == h ? "her" : GirlInfo.FirstName(about);
            switch (i.Kind)
            {
                case Social.Kinds.Rude: return "what you said to " + (who == "" ? "someone" : who);
                case Social.Kinds.Ignored: return "walking past " + (who == "" ? "someone" : who) + " when she needed help";
                case Social.Kinds.BrokePromise: return "the promise you broke" + (who == "" || who == "her" ? "" : " to " + who);
                case Social.Kinds.SeenGirlsRoom: return "you being seen in the girls' restroom";
                case Social.Kinds.SeenLockers: return "you being seen in the girls' changing room";
                case Social.Kinds.Flirted:
                    {
                        var x = who == "" || who == "her" ? "someone" : who;
                        switch (i.What)
                        {
                            case Witness.Kiss: return "you kissing " + x;
                            case Witness.Hand: return "you holding hands with " + x;
                            case Witness.Touch: return "you getting handsy with " + x;
                            default: return "you flirting with " + x;
                        }
                    }
                default: return "what happened" + (who == "" || who == "her" ? "" : " with " + who);
            }
        }

        private static void ClearAir(SaveData.Heroine h, Incident g)
        {
            var s = Sess;
            var about = Social.Get(g.Girl);
            var self = about == h;
            var what = IncidentLabel(h, g);
            var explainChance = Mathf.Clamp01(0.25f + G.Int / 150f + S.Reputation / 300f + Social.Trust(h) / 300f);
            var explained = Random.value < explainChance;
            s.Did.Add("cleared:" + g.Id);
            Beat(h, Lvl.Friend, "clear:" + g.Id, b =>
            {
                b.Narr(GirlInfo.FirstName(h) + " folds her arms before you say anything.");
                b.Face(h, Feel.Serious).Her_(self
                    ? ConvoLines.Incident(GirlInfo.ToneOf(h), g.Kind, GirlInfo.FirstName(h), true, false, g.What)
                    : Social.Saw(h, g)
                    ? T(h, "I saw it, you know. " + char.ToUpperInvariant(what[0]) + what.Substring(1) + ". With my own eyes. || I saw " + what + ". I really did. || With my own eyes... " + what + ". || I was there. I saw " + what + ".", "Don't even try! I SAW it! " + char.ToUpperInvariant(what[0]) + what.Substring(1) + "! || I SAW " + what + "! Don't lie!" + " || " + what + "! I was right there! || Caught you! " + what + "!",
                        "I was there. I saw " + what + ". So? || I witnessed " + what + ". Explain." + " || " + what + ". I saw. || I was present for " + what + ".", "Funny thing. I happened to see " + what + ". Front row." + " || " + what + "? I had a great view. || I saw " + what + ". Front row seat. || Guess who saw " + what + "?")
                    : T(h, "I heard about " + what + "... from people talking. Is it true? || People are saying " + what + "... is it true? || I heard about " + what + "... please tell me. || Someone told me " + what + "...", "Okay, so I heard about " + what + "! Is that true?! || I heard " + what + "! True?! || Everyone's saying " + what + "! || Spill! " + what + "?!",
                        "People are talking about " + what + ". I'd rather hear it from you." + " || " + what + ". I've heard. Your side? || There's talk of " + what + ". Explain. || I'd rather hear " + what + " from you.", "So. " + char.ToUpperInvariant(what[0]) + what.Substring(1) + ". The whole class knows. Your version?" + " || " + what + "? The whole university knows. || So, " + what + ". Care to explain?" + " || " + what + ". Your version, please."));
            }, new List<Resp>
            {
                Rp("Tell her you understand why " + what + " hurt", "ca_feel", self ? Q.Good : Q.Neutral, Tn("...Thank you for saying that.", "...Okay. That helps. A little.", "...At least you get it.", "...Hm. Fine. That's a start.")),
                Rp("Ask what she heard about " + what, "ca_facts", self ? Q.Bad : Q.Neutral,
                    self ? Tn("...I didn't hear it. I was there.", "Heard?! I was THERE!", "...I don't need to hear it. I lived it.", "Heard? Cute. I was the one it happened to.")
                         : Tn("...Just that you did it. People exaggerate, I know.", "That you totally did it! Did you?", "...The short version. It didn't sound good.", "The juicy version. Want to correct it?")),
                Rp("Explain what really happened" + (G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(explainChance * 100) + "%)</color>" : ""), "ca_explain", explained ? Q.Good : Q.Bad,
                    explained ? Tn("...Oh. I didn't know that part. I'm sorry I assumed.", "Wait, REALLY? Oh... that changes things!", "...That's different from what I heard. Okay.", "...Huh. Fine. That's actually a decent excuse.")
                              : Tn("...That sounds like an excuse.", "Come on, really? That's your story?", "...I don't buy it.", "Nice try.")),
                Rp("Admit the part that's true about " + what, "ca_admit", Q.Good, Tn("...Thank you for being honest.", "...Okay. Respect for admitting it.", "...Honesty. Good.", "...Well. That's more than most would do.")),
                Rp("Give her some space", "ca_space", Q.Neutral, Tn("...Yeah. Maybe that's best for now.", "...Okay. Later, then.", "...Fine.", "...Running away? ...Okay. Fine.")),
            }, r =>
            {
                if (r == null) return;
                if (r.Tag == "ca_explain" && explained) { Social.Cleared(h, g); Social.AddTrust(h, -Social.Effect(h, g)); Toasts.Show("Misunderstanding cleared."); }
                else if (r.Tag == "ca_admit") { Social.Cleared(h, g); Social.AddTrust(h, -Social.Effect(h, g) / 2); Toasts.Show("She accepts it."); }
                else if (r.Tag == "ca_space") s.Did.Remove("cleared:" + g.Id);
            });
        }

        /// <summary>What she holds against you that this talk has not settled yet.</summary>
        private static List<Incident> OpenGrudges(SaveData.Heroine h, Session s)
        {
            return Social.Grudges(h).Where(i => !s.Did.Contains("cleared:" + i.Id)).ToList();
        }

        /// <summary>
        /// ter um geral"): one scene for every grudge she holds; the more there are, the harder it lands and the harder
        /// it is to talk your way out.
        /// </summary>
        private static void ClearAirAll(SaveData.Heroine h, List<Incident> all)
        {
            var s = Sess;
            var n = all.Count;
            var total = all.Sum(i => Social.Effect(h, i));
            var saw = all.Any(i => Social.Get(i.Girl) == h || Social.Saw(h, i));
            var many = n >= 3;
            var explainChance = Mathf.Clamp01(0.25f + G.Int / 150f + S.Reputation / 300f + Social.Trust(h) / 300f - 0.08f * (n - 1));
            var explained = Random.value < explainChance;
            foreach (var i in all) s.Did.Add("cleared:" + i.Id);
            var kinds = string.Join(", ", all.Select(i => IncidentLabel(h, i)).ToArray());
            CampusLifePlugin.Log.LogInfo("Talk clear all: " + GirlInfo.FirstName(h) + " " + n + " grudges (" + kinds + "), effect " + total + ", saw " + saw);
            Beat(h, Lvl.Friend, "clearall:" + all[0].Id, b =>
            {
                b.Narr(many ? GirlInfo.FirstName(h) + " has clearly been keeping count." : GirlInfo.FirstName(h) + " folds her arms before you say anything.");
                b.Face(h, Feel.Serious).Her_(saw
                    ? (many ? T(h, "...It's not one thing anymore. I saw some of it myself, and I heard the rest. || ...It's more than one thing now. I saw some of it. || ...So many things... I saw some myself. || ...It keeps piling up. I saw part of it.", "No, no, it's not just ONE thing! I saw it! And people keep telling me more! || It's not just one thing! I saw it AND heard it! || So many things! I saw some! || It keeps happening! I saw!",
                                 "One thing I could ignore. This is a pattern. I saw part of it with my own eyes. || It's a pattern. I saw part of it. || Multiple incidents. One witnessed. || Not isolated. I saw some.", "Let's see... what I saw, what I heard, what I heard again. You're busy. || Saw some, heard more. You've been busy. || What I saw, plus what I heard. Wow. || You've been very busy, haven't you?")
                            : T(h, "...There's more than one thing. And I saw at least one of them. || ...More than one thing. I saw one. || ...Two things. I saw one myself. || ...It's not just one. I saw it.", "It's not just one thing! And I SAW one of them! || Two things! I SAW one! || More than one! I saw it! || Not just one! Saw it!",
                                 "Two things. One I saw myself. Explain. || Two. One witnessed. Explain. || Two things. One firsthand. || Two. I saw one.", "Two things, and I had a front-row seat for one. Talk. || Two things. Front row for one. || Two. One I watched. Talk. || Two, and I saw one. Explain."))
                    : (many ? T(h, "...People keep telling me things about you. One after another. Are they all true? || ...People keep telling me things about you. || ...I keep hearing about you... are they true? || ...So many stories about you...", "Everyone keeps telling me stuff about you! Is ANY of it made up?! || Everyone keeps telling me stuff! Is any of it fake?! || Stories everywhere! True?! || I keep hearing things! Explain!",
                                 "I keep hearing about you. Again and again. I'd rather hear it from you. || Repeated reports. Your version. || I keep hearing. I'd rather hear you. || Again and again. Explain.", "You're the favorite topic around here. Over and over. Your version? || You're everyone's favorite topic. || Over and over, your name. Explain. || The gossip loves you.")
                            : T(h, "...I heard two things about you. I'd like to know if they're true. || ...Two things about you. Are they true? || ...I heard two things... || ...Two stories reached me...", "Two things! I heard TWO things! Are they true?! || Two things! True?! || I heard TWO things! || Two! Explain!",
                                 "Two stories about you reached me. Your version. || Two stories. Your version. || Two reports. Explain. || Two things. Clarify.", "Two rumors, one you. Explain. || Two rumors, one you. || Two stories. Talk. || Two things. Explain yourself.")));
            }, new List<Resp>
            {
                Rp("Tell her you understand why all of it hurt", "ca_feel", Q.Neutral, Tn("...Understanding isn't the same as stopping.", "...Okay. But it's a LOT.", "...Understanding is cheap.", "...Nice words. Lots of things, though.")),
                Rp("Ask what exactly she heard", "ca_facts", saw ? Q.Bad : Q.Neutral, saw ? Tn("...Some of it I didn't need to hear. I saw it.", "Heard?! I SAW some of it!", "...Part of it I watched.", "Heard? Cute. I had a good view.")
                                                                          : Tn("...All of it. Bit by bit.", "Everything! Piece by piece!", "...Enough.", "The whole collection.")),
                Rp("Explain what really happened" + (G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(explainChance * 100) + "%)</color>" : ""), "ca_explain", explained ? Q.Good : Q.Bad,
                    explained ? Tn("...Oh. So it wasn't all how it looked. I'm sorry.", "Wait, REALLY? All of it? ...Okay, that changes things!", "...That's different from what I heard. Fine.", "...Huh. Smooth. Fine, I believe you.")
                              : Tn("...One excuse for everything? No.", "That's not gonna cover ALL of it!", "...Too many coincidences.", "One story for all of that? Nice try.")),
                Rp("Admit all of it", "ca_admit", Q.Good, Tn("...Thank you for being honest. It still hurts.", "...Okay. Respect. Ouch, but respect.", "...All of it. Good. Honest, at least.", "...Well. That's a confession and a half.")),
                Rp("Give her some space", "ca_space", Q.Neutral, Tn("...Yeah. Maybe that's best for now.", "...Okay. Later, then.", "...Fine.", "...Running away? ...Okay. Fine.")),
            }, r =>
            {
                if (r == null) return;
                if (r.Tag == "ca_explain" && explained) { foreach (var i in all) Social.Cleared(h, i); Social.AddTrust(h, -total); Toasts.Show("Misunderstandings cleared."); }
                else if (r.Tag == "ca_admit") { foreach (var i in all) Social.Cleared(h, i); Social.AddTrust(h, -total / 2); Toasts.Show("She accepts it, all of it."); }
                else if (r.Tag == "ca_explain" || r.Tag == "ca_facts" && saw) { Social.AddTrust(h, -(n - 1)); } // a weak answer to a pile costs more
                else if (r.Tag == "ca_space") foreach (var i in all) s.Did.Remove("cleared:" + i.Id);
            });
        }

        // ---------------- redemption ----------------

        private static UEntry RedemptionEntry(SaveData.Heroine h, GirlState gs)
        {
            if (gs.Scorned <= 0) return null;
            if (Pairs.CanRedeem(h)) return E("<color=#ff9b6a>Start redemption: the way she was left</color>", () => Redemption(h, gs, true));
            if (gs.Redemption >= 1 && gs.Redemption < 3 && !Pairs.IsScorned(h)) return E("<color=#ff9b6a>Continue redemption: the way she was left</color>", () => Redemption(h, gs, false));
            return null;
        }

        private static void Redemption(SaveData.Heroine h, GirlState gs, bool start)
        {
            var next = gs.Redemption == 1 ? "a present she really likes" : "helping her, or standing up for her";
            Beat(h, Lvl.Friend, "redeem:" + gs.Redemption, b =>
            {
                b.Narr(GirlInfo.FirstName(h) + " looks at you for a long moment.");
                b.Face(h, Feel.Sad).Her_(start
                    ? T(h, "...You want to talk? After everything? || ...You want to talk now? || ...After everything...? || ...Now you want to talk?", "Oh, NOW you want to talk? || NOW you want to talk?! || Oh, now?! || Seriously, now?!", "...Say what you came to say. || ...Speak. || ...Go ahead. || ...Say it.", "...Go on. I'm listening. Barely. || ...Go on. I'm barely listening. || ...Talk. Quickly. || ...I'm listening. Sort of.")
                    : T(h, "...You're still trying. I noticed. || ...You're still trying. || ...I see you haven't given up. || ...You keep coming...", "...You keep coming back, huh. || ...You keep coming back. || ...Still here, huh. || ...You don't quit.", "...You haven't given up. || ...Persistent. || ...You keep trying. || ...Not giving up.", "...Persistent. I'll give you that. || ...Persistent. Fine. || ...Stubborn. I'll admit it. || ...You're relentless."));
            }, start ? new List<Resp>
            {
                Rp("Take responsibility, no excuses", "rd_own", Q.Good, Tn("...Thank you for not making excuses.", "...Okay. That... that matters.", "...Good. No excuses.", "...Fine. That's the right start."), () => Pairs.Redeem(h, "talk")),
                Rp("Offer to make it right with something concrete", "rd_repair", Q.Good, Tn("...Then show me. Not just words.", "...Prove it, then!", "...Actions. Not promises.", "...We'll see."), () => Pairs.Redeem(h, "talk")),
                Rp("Tell her you know how much it hurt", "rd_feel", Q.Neutral, Tn("...You have no idea. But thanks.", "...Yeah. It did.", "...It did.", "...Understatement.")),
                Rp("Ask her to hear your side first", "rd_facts", Q.Bad, Tn("...Your side. Of course.", "Seriously?! Your SIDE?", "...No.", "Wow. Bold.")),
                Rp("Give her space for now", "rd_space", Q.Neutral, Tn("...Yeah. Please.", "...Later. Maybe.", "...Fine.", "...Smart move.")),
            } : new List<Resp>
            {
                Rp("Promise to keep at it: " + next, "rd_next", Q.Good, Tn("...Okay. I'll be watching.", "...Okay. Don't stop.", "...We'll see.", "...I'm keeping score.")),
                Rp("Tell her you're not giving up on her", "rd_stay", Q.Good, Tn("...I know. That's what scares me.", "...Good. ...I mean. Whatever.", "...Noted.", "...Stubborn.")),
                Rp("Ask how she's doing now", "rd_how", Q.Neutral),
                Rp("Ask if things are fine already", "rd_rush", Q.Bad, Tn("...Not yet.", "No! Not yet!", "...No.", "...Don't push it.")),
                Rp("Give her space for now", "rd_space", Q.Neutral),
            });
        }

        // ---------------- the opener's beats (four answers) ----------------

        private static void RequestTalk(SaveData.Heroine h, Request r, bool hidden)
        {
            var title = Lower(r.Title);
            Beat(h, Lvl.Friend, "req:" + r.Id, b =>
            {
                b.Me(hidden ? "You look like something's on your mind." : "About " + title + "...");
                b.Her_(Board.ProblemFirst(r, GirlInfo.ToneOf(h)));
            }, new List<Resp>
            {
                Rp("Tell her it's no trouble at all", "rq_feel", Q.Good),
                Rp("Ask what she needs exactly for " + title, "rq_ask", Q.Good, Tn("Just... a little of your time. That's all.", "Just you! And maybe snacks!", "...Your time. Not much.", "Your undivided attention. Obviously.")),
                Rp("Promise to help with " + title + " " + Board.WindowLabel(r.Periods).ToLowerInvariant(), "rq_promise", Q.Good, Tn("...Really? I'll be waiting.", "Promise?! Yay!", "...Okay. I'll hold you to it.", "A promise. Don't break it.")),
                Rp("Joke that " + title + " sounds like a quest", "rq_joke", Low(MoodOf(h)) ? Q.Bad : Q.Good, Tn("Hehe... a small quest.", "Ahaha! Quest accepted?!", "...Hm. A side quest.", "Then you're my hero. Don't fail.")),
            }, x => { if (hidden) Board.Reveal(r, "her"); });
        }

        private static void GossipTalk(SaveData.Heroine h, Incident gossip)
        {
            var who = GossipWho(gossip);
            Beat(h, Lvl.Friend, "gossip:" + gossip.Id, b =>
            {
                b.Me("You heard something about " + who + "?");
                b.Her_(ConvoLines.GossipLine(GirlInfo.ToneOf(h), gossip.Kind, who, gossip.What));
                Social.Mentioned(h, gossip);
            }, new List<Resp>
            {
                Rp("Say it must be awkward for " + who, "gs_feel", Q.Good),
                Rp("Ask who told her about " + who, "gs_who", Q.Neutral, Tn("...Just people in class.", "Everyone! It's everywhere!", "...People talk.", "A little bird. Several birds.")),
                Rp("Suggest you both keep it quiet", "gs_quiet", Q.Good, Tn("...You're right. It's not fair to " + who + ".", "Okay, okay! My lips are sealed!", "...Agreed.", "...Fine. Boring, but fine."), () => Social.AddTrust(h, 1)),
                Rp("Joke about how fast rumors fly", "gs_joke", Q.Neutral, Tn("Hehe... faster than the bell.", "Faster than me in the hallway!", "...Too fast.", "Light speed.")),
            });
        }

        private static void MemoryTalk(SaveData.Heroine h, Moment m)
        {
            var when = CampusLife.Memories.When(m);
            Beat(h, Lvl.Friend, "mem:" + m.Id, b =>
            {
                b.Me(Lines.Pick("I was thinking about " + when + "...", "Hey, about " + when + "..."));
                b.Her_(T(h, "When " + m.Text + "? Hehe... I remember." + " || " + m.Text + "? I remember. || Hehe... " + m.Text + ". Yes. || Oh, when " + m.Text + ". I remember.", "Oh yeah! When " + m.Text + "! || Yeah! " + m.Text + "!" + " || " + m.Text + "! I remember! || Oh! " + m.Text + "!", "...When " + m.Text + ". I remember. || ..." + m.Text + ". Yes. || ...I remember " + m.Text + ". || ..." + m.Text + ". Mm.", "When " + m.Text + "? You remember that? Cute." + " || " + m.Text + "? Sweet. || You remember " + m.Text + "? Cute." + " || " + m.Text + "? Sentimental."));
                CampusLife.Memories.Mentioned(m);
            }, new List<Resp>
            {
                Rp("Tell her it was one of your favorite moments", "mm_feel", Q.Good),
                Rp("Ask what she remembers most about it", "mm_ask", Q.Good, Tn("...The way you looked at me, maybe.", "Everything! But mostly the end!", "...The quiet part.", "Your face. Obviously.")),
                Rp("Suggest you do it again soon", "mm_again", h.relation >= 1 ? Q.Good : Q.Neutral, Tn("...I'd like that.", "Yes! When?!", "...Maybe.", "Are you asking me out? Again?")),
                Rp("Tease her about how she was " + when, "mm_tease", GirlInfo.ToneOf(h) == Tone.Gentle ? Q.Neutral : Q.Good, Tn("H-hey! Don't remind me...", "Ahaha! Shut up!", "...We don't talk about that.", "Says you!")),
            });
        }

        private static void MoodTalk(SaveData.Heroine h, Mood mood, string seems)
        {
            var exam = Calendar.IsExamWeek;
            string thing;
            switch (mood)
            {
                case CampusLife.Mood.Stressed: thing = exam ? "the exam" : "class"; break;
                case CampusLife.Mood.Sleepy: thing = "last night"; break;
                case CampusLife.Mood.Hungry: thing = "lunch"; break;
                case CampusLife.Mood.Lonely: thing = "being on her own"; break;
                case CampusLife.Mood.Upset: thing = "what happened"; break;
                case CampusLife.Mood.Bored: thing = "today"; break;
                default: thing = "today"; break;
            }
            var low = Low(mood) || seems == "tired" || seems == "lonely";
            var gs = S.Girl(GirlInfo.Key(h));
            var rs = new List<Resp>
            {
                Rp(mood == CampusLife.Mood.Stressed ? "Tell her the hard part wasn't her fault" : low ? "Tell her it's okay to feel " + seems : "Say her mood is contagious", "md_feel", low ? Q.Good : Q.Neutral),
                Rp(mood == CampusLife.Mood.Stressed ? "Ask which part of " + thing + " was the worst" : "Ask what's behind " + thing, "md_ask", mood == CampusLife.Mood.Upset ? Q.Neutral : Q.Good,
                    Tn("Well... it's a long story.", "Okay, so, here's the thing!", "...If you really want to know.", "You want details? Fine.")),
                Rp(mood == CampusLife.Mood.Stressed ? "Offer to study " + thing + " together" : mood == CampusLife.Mood.Hungry ? "Offer to grab lunch together" : mood == CampusLife.Mood.Sleepy ? "Offer to keep her awake in class"
                    : mood == CampusLife.Mood.Lonely ? "Offer to hang out after class" : mood == CampusLife.Mood.Bored ? "Suggest something fun after class" : "Offer to listen whenever she wants", "md_act", Q.Good,
                    Tn("...You'd do that? Thank you.", "Really?! Deal!", "...Okay. I accept.", "Oh? Is that a date offer?"), () =>
                    {
                        if (mood == CampusLife.Mood.Stressed || mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Lonely) { gs.Mood = CampusLife.Mood.None; gs.MoodKnownDay = S.Day; }
                    }),
            };
            if (!low && mood != CampusLife.Mood.Upset)
                rs.Add(Rp("Make a light joke about " + thing, "md_joke", Q.Good, Tn("Hehe... you're silly.", "Ahaha! Stop it!", "...Hm. Almost funny.", "Heh. Okay, that one was good.")));
            Beat(h, Lvl.Friend, "mood:" + mood, b =>
            {
                b.Me(T(h, "You looked " + seems + " earlier. Want to talk about it? || You seemed " + seems + "... want to talk? || Are you okay? You looked " + seems + ". || You look " + seems + "... what happened?", "You look " + seems + "! What's up? || You're " + seems + "! What's up?! || Why so " + seems + "?! || Hey! You look " + seems + "!", "You seem " + seems + ". Something wrong?" + " || " + seems + ". Why? || You appear " + seems + ". || Something's off. You seem " + seems + ".", "So, why so " + seems + "? || Why so " + seems + ", hm?" + " || " + seems + "? Tell me. || What made you " + seems + "?"));
                b.Her_(AskMood(h, mood));
            }, rs, r => { gs.MoodKnownDay = S.Day; });
        }

        private static void EventTalk(SaveData.Heroine h, string name, string when)
        {
            Beat(h, Lvl.Friend, "event:" + name, b =>
            {
                b.Me("The " + name + " is " + when.ToLowerInvariant() + ". Ready?");
                b.Her_(T(h, "The " + name + "...? I'm a little nervous. || The " + name + "... I'm nervous. || The " + name + "? I'm a little scared. || Oh, the " + name + "...", "The " + name + "! I can't wait! || The " + name + "! Yes! || Can't wait for the " + name + "! || The " + name + "! Hype!", "...The " + name + ". I'm prepared. Mostly. || The " + name + ". Prepared. Mostly. || ...The " + name + ". Ready. || The " + name + ". We'll see.", "Ready? I was born ready. ...Mostly. || The " + name + "? Born ready. Mostly. || Ready? Always. Almost. || The " + name + "? Easy."));
            }, new List<Resp>
            {
                Rp("Tell her she'll do great at the " + name, "ev_feel", Q.Good),
                Rp("Ask what she's looking forward to at the " + name, "ev_ask", Q.Good, Tn("...Spending it with friends, I think.", "Everything! The food! The noise!", "...Getting it over with.", "Seeing you mess up. Kidding. Mostly.")),
                Rp("Offer to prepare for the " + name + " together", "ev_act", Q.Good, Tn("...Together? Okay.", "Yes! Training montage!", "...Fine.", "Ooh. A study date?")),
                Rp("Joke about the " + name, "ev_joke", Q.Neutral, Tn("Hehe...", "Ahaha!", "...Hm.", "Heh.")),
            });
        }

        private static void PlaceTalk(SaveData.Heroine h, string place)
        {
            Beat(h, Lvl.Friend, "place:" + place, b =>
            {
                b.Me("Have you noticed " + place + "?");
                b.Her_(T(h, "Hm? Oh... yes. I like it, actually. || Oh... yes. I like it too. || Mm, yes. Nice, isn't it? || Ah, yes... I noticed.", "Right?! I noticed too! || Right?! I noticed! || Yes! Totally! || Me too!", "...I noticed. || ...Noticed. || ...Yes. || ...I saw.", "Making small talk? Cute. ...Yes, I noticed. || Small talk? Cute. Yes. || Making conversation? Yes, I noticed. || Cute attempt. Yes."));
            }, new List<Resp>
            {
                Rp("Say it's nicer with her around", "pl_feel", h.relation >= 1 ? Q.Good : Q.Neutral, Tn("...Th-that's a nice thing to say.", "Aww! Smooth!", "...Hm.", "Flirting already?")),
                Rp("Ask what she usually does here", "pl_ask", Q.Good, Tn("I come here to think, sometimes.", "Hang out! Obviously!", "...Read. Quietly.", "Wait for interesting people. Like you, maybe.")),
                Rp("Suggest you come here together sometime", "pl_act", h.relation >= 0 ? Q.Good : Q.Neutral, Tn("...I'd like that.", "Yes! It's a plan!", "...Maybe.", "Is that an invitation?")),
                Rp("Make a joke about " + place, "pl_joke", GirlInfo.ToneOf(h) == Tone.Cool ? Q.Neutral : Q.Good, Tn("Hehe...", "Ahaha! True!", "...Hm.", "Heh. Okay.")),
            });
        }

        // ================= Romance beats =================

        /// <summary>Something true you noticed about her (a like you learned, a tradition, her club when you see her at it).</summary>
        private static string[] ComplimentTrait(SaveData.Heroine h, GirlState gs)
        {
            var like = GirlInfo.LikesOf(h).FirstOrDefault(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) != 0);
            if (gs.Traditions.Count > 0) return new[] { "keeping " + gs.Traditions[gs.Traditions.Count - 1] + " going", "trad" };
            if (like != null) return new[] { "her taste in " + GirlInfo.LikeLabel(like).ToLowerInvariant(), "like:" + like };
            if (!GirlInfo.InNoClub(h) && Places.CurrentId == "clubroom") return new[] { "how hard she works at " + GirlInfo.ClubName(h), "club" };
            var m = gs.Moments.Where(x => x.Tone > 0).OrderByDescending(x => x.Id).FirstOrDefault();
            if (m != null) return new[] { "how she was " + CampusLife.Memories.When(m), "mem:" + m.Id };
            return null;
        }

        private static void ComplimentU(SaveData.Heroine h, string[] trait)
        {
            var a = FlirtAnswer(h, false, "compliment");
            var ok = Took(a);
            var b = new SceneBuilder(h, true);
            b.Me("I really like " + trait[0].Replace("her ", "your ").Replace("she ", "you ") + ".");
            b.Face(h, ok || a == "wary_no" ? Feel.Shy : Feel.Normal).Her_(a == "wary_no" ? WaryNoLine(h, false) : ok ? (a == "wary_yes" ? WaryLead(h) : "") + RomanceSay(h, "noticed", "yes") : RomanceSay(h, "noticed", "no"));
            Plain(h, Lvl.Romance, QOf(a), "compliment:" + trait[1], b, ok ? "♥ " + GirlInfo.FirstName(h) + (a == "wary_yes" ? " is flustered, but happy you noticed." : " is happy you noticed.") : null);
        }

        private static void HoldHandU(SaveData.Heroine h)
        {
            var a = Consent.HoldHand(h, Onlookers(h).Count);
            var ok = Took(a);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "hand"));
            b.Face(h, ok || a == "wary_no" ? Feel.Shy : Feel.Surprised).Her_(a == "wary_no" ? WaryNoLine(h, false) : ok ? (a == "wary_yes" ? WaryLead(h) : "") + RomanceSay(h, "hand", "yes") : RomanceSay(h, "hand", "no"));
            var witnesses = Witness.Who(h, Witness.Hand);
            Plain(h, Lvl.Romance, QOf(a), "hand", b, ok ? "♥ " + GirlInfo.FirstName(h) + (a == "wary_yes" ? " lets you hold her hand, blushing." : " holds your hand.") : null, () =>
            {
                if (ok) Social.Add(Social.Kinds.Flirted, h, witnesses, Witness.Hand);
            });
        }

        private static void SweetTalk(SaveData.Heroine h, GirlState gs)
        {
            var a = FlirtAnswer(h, false, "sweet");
            var ok = Took(a);
            // Always about something real between you: a tradition, a good memory, a like you learned, or today.
            string detail;
            if (gs.Traditions.Count > 0) detail = "I keep thinking about " + gs.Traditions[gs.Traditions.Count - 1] + ". It's my favorite part of the week.";
            else
            {
                var m = gs.Moments.Where(x => x.Tone > 0).OrderByDescending(x => x.Id).FirstOrDefault();
                if (m != null) detail = "I still think about when " + m.Text + ".";
                else detail = RomanceMe(h, "flirt");
            }
            var b = new SceneBuilder(h, true);
            b.Me(detail);
            b.Face(h, a == "yes" ? Feel.Love : ok || a == "wary_no" ? Feel.Shy : Feel.Normal).Her_(a == "wary_no" ? WaryNoLine(h, false) : ok ? (a == "wary_yes" ? WaryLead(h) : "") + RomanceSay(h, "flirt", "yes") : RomanceSay(h, "flirt", "no"));
            Plain(h, Lvl.Romance, QOf(a), "sweet", b, ok ? "♥ " + GirlInfo.FirstName(h) + " is blushing." : null, () =>
            {
                if (ok && Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 1);
            });
        }

        private static void AskMeaning(SaveData.Heroine h)
        {
            var ok = h.relation >= 1 || h.isGirlfriend || h.favor >= 60 && Social.Trust(h) >= 40;
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "feelings"));
            b.Face(h, ok ? Feel.Shy : Feel.Surprised).Her_(ok ? FeelingsLine(h) : RomanceSay(h, "feelings", "no"));
            Plain(h, Lvl.Romance, ok ? Q.Good : Q.Bad, "meaning", b, null);
        }

        /// <summary>One of the game's own steps (date, confession, H) that the game does not allow now: she says no, nothing is lost.</summary>
        private static void NotNow(SaveData.Heroine h, string what)
        {
            var b = new SceneBuilder(h, true);
            switch (what)
            {
                case "date":
                    b.Me("Would you go out with me this weekend?");
                    b.Face(h, Feel.Worried).Her_(RomanceSay(h, "notnow", "date"));
                    break;
                case "confess":
                    b.Me("There's something I want to tell you...");
                    b.Face(h, Feel.Surprised).Her_(RomanceSay(h, "notnow", "confess"));
                    break;
                case "lunch":
                    b.Me("Want to have lunch together?");
                    b.Face(h, Feel.Worried).Her_(RomanceSay(h, "notnow", "lunch"));
                    break;
                case "home":
                    b.Me("Want to walk home together?");
                    b.Face(h, Feel.Worried).Her_(RomanceSay(h, "notnow", "home"));
                    break;
                case "study":
                case "exercise":
                case "club":
                case "follow":
                    b.Me(what == "study" ? "Want to study together?" : what == "exercise" ? "Want to work out together?" : what == "follow" ? "Come with me for a bit?" : "Want to do club activities together?");
                    b.Face(h, Feel.Worried).Her_(RomanceSay(h, "notnow", "later"));
                    break;
                default:
                    b.Me(Lines.Pick("Want to go somewhere... more private?", "How about we find somewhere quiet, just the two of us?"));
                    b.Face(h, Feel.Shy).Her_(T(h, "Eh?! N-not here... not now... || N-not here... || Not now... please. || Eh?! Not here...", "Whoa! Not now, okay?! || Not now! || Whoa! Later! || Not here!", "...Not now. || ...Not now. || ...Not here. || ...No.", "Not here, not now. Patience. || Patience. || Not here. || Wait."));
                    break;
            }
            Plain(h, Lvl.Romance, Q.Neutral, "notnow:" + what, b, null, null, false);
        }

        private static void Jealousy(SaveData.Heroine h, SaveData.Heroine rival)
        {
            var s = Sess;
            var gs = S.Girl(GirlInfo.Key(h));
            var rn = rival != null ? GirlInfo.FirstName(rival) : "the others";
            gs.JealousDay = S.Day;
            s.Did.Add("jealous");
            Beat(h, Lvl.Romance, "jealous", b =>
            {
                b.Her_(RomanceSay(h, "jealous", Interested(h) ? "love" : "plain", null, rn));
            }, new List<Resp>
            {
                Rp("Listen to how she feels about " + rn, "jl_listen", Q.Good, Tn("...I just miss you, that's all.", "I just... want you around too, okay?!", "...I'm not jealous. I just noticed.", "...Fine. I'm a little jealous. Happy?")),
                Rp("Explain you've just been busy with " + rn, "jl_explain", Social.Trust(h) >= 40 ? Q.Neutral : Q.Bad, Tn("...I know. I just felt left out.", "Busy, busy... yeah, yeah.", "...Fine.", "Busy. Sure.")),
                Rp("Make plans to spend time with her", "jl_plans", Q.Good, Tn("...Really? Then lunch, soon?", "Yes! Lunch! You promised!", "...Okay. Lunch.", "Lunch. You and me. Don't be late."), () =>
                {
                    s.Did.Add("plans");
                    var r = Board.Make(RequestKind.Lunch, h, "Lunch with " + GirlInfo.FirstName(h), "You promised " + GirlInfo.FirstName(h) + " you'd spend more time together. Invite her to lunch.", 8, PeriodMask.Lunch);
                    r.DeadlineDay = S.Day + 2;
                    r.LearnedFrom = "her";
                    Board.Add(r);
                }),
            });
        }

        private static void BreakUp(SaveData.Heroine h)
        {
            var name = GirlInfo.FirstName(h);
            var b = new SceneBuilder(h, true);
            b.Narr("You're about to end things with " + name + ". This can't be taken back.");
            b.Choice("End it with " + name, "bu_end", "Stay together", "bu_stay");
            b.Branch("bu_end"); b.End();
            b.Branch("bu_stay"); b.Face(h, Feel.Normal).Her_(RomanceSay(h, "breakup", "stay")); b.End();
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                if (c == "bu_end" && GameAllows(2)) { Game(12); return; }
                Reopen();
            }, false);
        }

        // ================= Spicy beats =================

        /// <summary>Something here and now to tease her about (never a hurt, a grudge or a painful rumor).</summary>
        private static string[] TeaseContext(SaveData.Heroine h)
        {
            var mood = MoodOf(h);
            if (mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Stressed || Social.Grudge(h) != null) return null;
            if (mood == CampusLife.Mood.Sleepy) return new[] { "her sleepy face", "I like your sleepy face. It's very... soft." };
            if (mood == CampusLife.Mood.Hungry) return new[] { "her growling stomach", "Was that your stomach? Should I feed you?" };
            if (mood == CampusLife.Mood.Flirty) return new[] { "the way she keeps looking at you", "You keep looking at me. Something on your mind?" };
            switch (Places.CurrentId)
            {
                case "pool": return new[] { "her swimsuit", "That swimsuit is... really working for you." };
                case "gym": case "field": return new[] { "her gym clothes", "Gym clothes suit you. A little too well." };
                case "library": return new[] { "how close you're sitting", "We're sitting pretty close for a library." };
                case "rooftop": return new[] { "being alone up here", "Just the two of us up here... convenient." };
            }
            return new[] { "how cute she looks right now", "You know you look really cute right now, right?" };
        }

        private static void TeaseU(SaveData.Heroine h, string[] ctx)
        {
            var s = Sess;
            var a = FlirtAnswer(h, true, "tease");
            var ok = Took(a);
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(ctx[1]);
            b.Face(h, ok || a == "wary_no" ? Feel.Shy : Feel.Pout).Her_(a == "wary_no" ? WaryNoLine(h, true) : ok ? (a == "wary_yes" ? WaryLead(h) : "") + RomanceLines.TeaseGood(tone, Lines.Closeness(h)) : RomanceLines.TeaseBad(tone));
            Plain(h, Lvl.Spicy, QOf(a), "tease:" + ctx[0], b, ok ? "♥ " + GirlInfo.FirstName(h) + " is flustered." : null);
        }

        private static void WhisperU(SaveData.Heroine h)
        {
            var s = Sess;
            var a = FlirtAnswer(h, true, "whisper");
            var ok = Took(a);
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerWhisper(h.isGirlfriend));
            b.Face(h, a == "yes" ? Feel.Love : ok || a == "wary_no" ? Feel.Shy : Feel.Pout).Her_(a == "wary_no" ? WaryNoLine(h, true) : ok ? (a == "wary_yes" ? WaryLead(h) : "") + RomanceLines.WhisperGood(tone, h.isGirlfriend) : RomanceLines.WhisperBad(tone));
            Plain(h, Lvl.Spicy, QOf(a), "whisper", b, ok ? "♥ " + GirlInfo.FirstName(h) + " is breathing faster." : null, () =>
            {
                if (ok && Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 1);
            });
        }

        private static void FlirtBold(SaveData.Heroine h)
        {
            var s = Sess;
            var a = FlirtAnswer(h, true, "bold");
            var strong = a == "yes" && (MoodOf(h) == CampusLife.Mood.Flirty || h.lewdness >= 50 || h.isGirlfriend);
            var q = strong ? Q.Good : a == "no" ? Q.Bad : Q.Neutral;
            var b = new SceneBuilder(h, true);
            b.Me(h.isGirlfriend ? "I can't stop thinking about you. All of you." : "Honestly? I've been wanting to get a lot closer to you.");
            b.Face(h, q == Q.Good ? Feel.Love : q == Q.Neutral ? Feel.Shy : Feel.Pout).Her_(q == Q.Good
                ? T(h, "...M-me too. Don't make me say it twice. || ...Me too. Don't make me repeat it. || ...I feel the same. || ...Me too... so much.", "Ah... you can't just SAY that! ...But me too. || You can't just say that! ...Me too! || Ahh! ...Me too! || Hey! ...Same!", "...Then come closer. || ...Then come here. || ...Closer. || ...Same.", "Finally. I was wondering when you'd say it. || Finally. Took you long enough. || About time you said it. || Knew it. Me too.")
                : a == "wary_no" ? WaryNoLine(h, true)
                : q == Q.Neutral ? T(h, "Eh... th-that's... um... || Eh... that's... um... || Ah... um... || W-well...", "Whoa, okay! That's... a lot! || Whoa! That's a lot! || Wow, okay! || Eep!", "...Bold. || ...Bold. || ...Daring. || ...Hm.", "Oh? Bold today, aren't we? || Bold today, aren't we? || Oh? Brave. || My, my.")
                : T(h, "...That's too much, I'm sorry. || ...That's too much, sorry. || ...I can't... sorry. || ...Too fast, sorry.", "Whoa, back up! Too fast! || Back up! Too fast! || Whoa! Slow down! || Too much!", "...No. || ...No. || ...Stop. || ...Not happening.", "Down, boy. || Down, boy. || Easy. || Nope."));
            Plain(h, Lvl.Spicy, q, "bold", b, q == Q.Good ? "♥ " + GirlInfo.FirstName(h) + " is clearly into it." : null);
        }

        /// <summary>A real intimate memory between you (H together, a secret, a confession, being caught...), or null.</summary>
        private static string[] PrivateMemory(SaveData.Heroine h, GirlState gs)
        {
            var m = gs.Moments.Where(x => x.Kind == "secret" || x.Kind == "feelings" || x.Kind == "couple" || x.Kind == "caught").OrderByDescending(x => x.Id).FirstOrDefault();
            if (m != null) return new[] { "that private moment " + CampusLife.Memories.When(m), "mem:" + m.Id, m.Text };
            if (h.hCount > 0) return new[] { "your last time together", "h:" + h.hCount, null };
            return null;
        }

        private static void PrivateMemoryU(SaveData.Heroine h, string[] mem)
        {
            var s = Sess;
            var a = FlirtAnswer(h, true, "private");
            var ok = Took(a);
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            if (mem[2] != null)
            {
                b.Me("I keep thinking about when " + mem[2] + "...");
                b.Face(h, ok ? Feel.Shy : Feel.Normal).Her_(ok ? T(h, "...M-me too. Every time I see you. || ...Me too. Every time. || ...I think about it too. || ...Me too... often.", "Shh! Not so loud! ...Me too. || Shh! ...Me too! || Quiet! ...Same! || Not so loud! ...Me too!", "...I remember. || ...I remember. || ...Same. || ...Yes.", "Oh? Me too. Often. || Me too. Often. || Same. Frequently. || Oh, me too.")
                                                             : T(h, "...Let's not talk about that here. || ...Not here, please. || ...Let's not, here. || ...Another place.", "Not here! || Not here! || Shh! || Later!", "...Not now. || ...Not now. || ...Later. || ...No.", "...Later. || ...Later. || ...Not here. || ...Patience."));
            }
            else
            {
                b.Me(RomanceLines.PlayerMemories(h.hCount));
                b.Face(h, ok ? Feel.Love : Feel.Normal).Her_(ok ? RomanceLines.Memories(tone, h.hCount) : T(h, "...Not here, please. || ...Not here. || ...Please, not here. || ...Somewhere else.", "Shh! Not here! || Not here! || Shh! || Quiet!", "...Not now. || ...Not now. || ...No. || ...Later.", "...Patience. || ...Patience. || ...Later. || ...Wait."));
            }
            if (a == "wary_yes") b.Narr("She looks away, red to the ears, but she doesn't change the subject.");
            Plain(h, Lvl.Spicy, ok ? Q.Good : Q.Neutral, "private:" + mem[1], b, ok ? "♥ " + GirlInfo.FirstName(h) + " remembers it too." : null);
        }

        /// <summary>
        /// A wary no is an almost-yes (user, 2026-10-30): she wants to, and says what held her back, the director's
        /// biggest brake (Consent.HeldBack).
        /// </summary>
        private static string HeldBackLine(SaveData.Heroine h, string why)
        {
            switch (why)
            {
                case "personality":
                    return T(h, "...I want to. I really do. But I'm not... that kind of girl. Not yet. Please don't hate me. || ...I want to. Really. But not yet. Please understand. || ...My heart says yes, but I'm not ready. Don't hate me. || ...I do want to... just not so soon.", "I-I want to! Honestly! But my brain is screaming and I can't! Give me time?! || I want to! But my brain's screaming! Give me time! || Honestly, yes! But no! Ugh! Wait for me?! || I wanna! But I can't yet! Sorry!",
                        "...Don't misread me. I want to. I just don't do this so easily. || ...I want to. I just don't move this fast. || ...Desire isn't the issue. Pace is. || ...Yes, I want to. Not yet.", "Mm. Believe me, I'm tempted. But I don't hand that out. Not yet. || Tempted? Very. But I don't give that away so fast. || Mm. Wanting isn't the same as saying yes. Not yet. || I'm tempted. You'll have to wait.");
                case "mood":
                    return T(h, "...If it were any other day... I'm just not okay today. Ask me again? || ...Any other day... today I'm just not okay. Ask again? || ...I'm sorry, today is bad. Another day? || ...Not today. I'm not feeling well.", "Ugh, today's the worst! Any other day I'd say yes! ...Rain check? || Today's awful! Any other day, yes! Rain check?! || Ugh, bad day! Ask tomorrow! || Not today! But soon!",
                        "...Not today. I'm not in a good place. It's not you. || ...Not today. It's not you. || ...Bad day. Not your fault. || ...Today, no. Another day.", "Bad day. Really bad. ...Ask me tomorrow, and I'll probably say yes. || Awful day. Ask tomorrow. I'll probably say yes. || Bad timing. Tomorrow, maybe. || Not today. Tomorrow's likely.");
                case "guilt":
                    return T(h, "...After last time, I can't. Not until we talk about it. ...I still want to, though. || ...Not until we talk about last time. I still want to, though. || ...We need to talk about before. Then... yes. || ...After last time, I can't yet. Let's talk first.", "I want to, but— we never talked about last time! We have to first! || We have to talk about last time first! || Last time! We never talked! Do that first! || Talk first! Then maybe!",
                        "...Not until we settle what happened. Then we'll see. || ...Settle last time first. || ...Resolve the past. Then ask. || ...We talk. Then we'll see.", "Last time is still hanging over us. Fix that first, and then... maybe. || Fix last time first. Then maybe. || Last time's unresolved. Fix it. || Deal with last time. Then ask.");
                case "girlfriend":
                    return T(h, "...You have a girlfriend. I... I want to, but I can't do that to her. || ...You have a girlfriend. I can't do that to her. || ...You're with someone. I want to, but no. || ...She'd be hurt. I can't.", "You're taken! I— ugh, why does that make it harder?! No! ...Not like this! || You're taken! No! Not like this! || You have a girlfriend! Argh! No! || Taken! Off limits! Even if...!",
                        "...You're with someone. I won't be the other girl. ...Even if I want to. || ...You're with someone. I won't be the other girl. || ...Not while you're taken. || ...You belong to someone. No.", "Tempting. But you belong to someone. I don't share well. Ask me when that changes. || You're taken. I don't share. Ask when you're free. || Spoken for. Come back single. || You belong to someone. Not my style.");
                case "soon":
                    return T(h, "...It's too soon. We barely... I want to know you more first. Is that okay? || ...Too soon. I want to know you more. || ...We've barely started... please wait. || ...Let's get closer first. Okay?", "Too fast, too fast! I like you, but— slow down a little! || Too fast! Slow down! || Whoa! Too soon! || Slow down a bit!",
                        "...We've only just started. Give it time. || ...Give it time. || ...Too early. || ...We just started.", "Patience. I want to, but not this fast. Make me wait for it. || Make me wait for it. || Patience. Not yet. || Too fast. Earn it.");
                case "asked":
                    return T(h, "...You already asked today. My answer hasn't changed... even if I want it to. || ...You asked already. Still no... even if I want to. || ...My answer's the same today. || ...Still no, today. Sorry.", "You asked already! Stop making it so hard to say no! || You asked already! Stop making it hard! || Again?! Still no! || Asked already! No!",
                        "...Still no. Asking again won't change today. || ...Still no. || ...Same answer. || ...No. Today, no.", "Again? Persistent. ...Still not today. Tomorrow, maybe. || Persistent. Still no. Tomorrow, maybe. || Again? No. Tomorrow. || Still no. Try tomorrow.");
                case "grudge":
                    return T(h, "...I'm still hurt about what happened. I want to, but... not while it hurts. || ...It still hurts. Not while it hurts. || ...I'm still sad about what happened. || ...I need to heal first.", "I'm still mad at you! ...Even if I kinda want to! Fix it first! || Still mad! Fix it first! || I'm upset! Apologize first! || Mad at you! Fix it!",
                        "...I haven't forgiven you yet. Ask me when I have. || ...Not forgiven yet. || ...Forgiveness first. || ...I'm still angry.", "You still owe me an apology. Then we'll talk. || Apology first. Then we talk. || You owe me. Pay first. || Say sorry first.");
                case "neglected":
                    return T(h, "...You've barely looked at me lately. I want to... but I want you to want me first. || ...You've been distant. I want you to want me first. || ...You barely see me lately... || ...Show me you care first.", "You've been ignoring me! And now this?! ...Spend time with me first! || You ignored me! Spend time with me first! || You've been gone! Hang out first! || Ignored! Fix that first!",
                        "...You've been distant. Fix that, then ask. || ...You've been distant. Fix it. || ...Absent lately. Correct that. || ...Attention first.", "Hm. You remember me only now? Earn it again first. || Only now you remember me? Earn it. || Neglect me, then ask? Earn it back. || You forgot me. Make it up.");
                case "name":
                    return T(h, "...People already talk about you. I want to, but I'm scared of what they'd say. || ...People talk about you. I'm scared of what they'd say. || ...Your reputation scares me a little. || ...I'm afraid of the rumors.", "Everyone's already gossiping about you! If they found out— no, no! || Everyone gossips about you! If they found out-! || The rumors! No way! || People would talk! No!",
                        "...With your reputation, I have to be careful. Not now. || ...Your reputation. I must be careful. || ...Too risky, given the talk. || ...Not with those rumors.", "Your name's a little dirty around campus. I like mine clean. ...For now. || Your name's a bit dirty. I like mine clean. || Your reputation precedes you. Not yet. || Too much gossip about you.");
                default:
                    return T(h, "...It's not that I don't want to. Just... not like this. Not here. Not yet. || ...Not like this. Not here. Not yet. || ...It's not that I don't want to... || ...Just... not now.", "Ahh, you're making my heart race! But not here! Not now! ...Ask me again sometime? || Heart racing! But not here! Ask again! || Not here! Not now! Later?! || Ahh! Not now!",
                        "...Not here. Not now. Don't take it the wrong way. || ...Not here. Not now. || ...Don't misunderstand. Not now. || ...Not the right moment.", "Tempting. Really. ...But not here. Keep that look for later. || Tempting. Not here. Later. || Keep that look for later. || Not here, darling.");
            }
        }

        /// <summary>
        /// Asking her to sneak off: the director's four answers. Yes and a wary yes both go to the
        /// game's H (a wary one leaves her guilty after it, Consent.AfterH); a wary no is an almost-yes, "I want to, but"
        /// with what held her back, and leaves the tension as a memory; a no costs a little trust, more when you already
        /// heard no today.
        /// </summary>
        private static void SneakU(SaveData.Heroine h)
        {
            var s = Sess;
            var a = Consent.DecideH(h);
            var ok = a == "yes" || a == "wary_yes";
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerSneak());
            switch (a)
            {
                case "yes": b.Face(h, Feel.Love).Her_(RomanceLines.SneakYes(tone)); break;
                case "wary_yes":
                    b.Narr(GirlInfo.FirstName(h) + " looks away, then back at you, and doesn't say no.");
                    b.Face(h, Feel.Shy).Her_(T(h, "...We shouldn't. ...But if it's you... just this once, okay? || ...We shouldn't. But for you... just once. || ...It's you... so... okay. || ...Just this once. Because it's you.", "Ah— um— okay! Okay. I think. ...Don't make it weird after, okay?! || Um! Okay! I think! Don't make it weird! || Okay! Yes! Ahh! || Alright! Let's go before I panic!",
                        "...This is a bad idea. ...Lead the way. || ...Bad idea. Lead on. || ...Unwise. Go. || ...Fine. Lead.", "...You know this changes things. ...Fine. Come on, before I think about it. || This changes things. Come on. || Before I think twice. Come. || Fine. Hurry, before I change my mind."));
                    break;
                case "wary_no":
                    b.Narr(GirlInfo.FirstName(h) + " hesitates a moment too long before she answers.");
                    b.Face(h, Feel.Shy).Her_(HeldBackLine(h, Consent.HeldBack(h)));
                    break;
                default: b.Face(h, Feel.Pout).Her_(RomanceLines.SneakNo(tone)); break;
            }
            if (ok) Bounds(b, h);   // what her card still refuses in the game's H, in her words
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                try { Pay(s, Lvl.Spicy, ok || a == "wary_no" ? (a == "yes" ? Q.Good : Q.Neutral) : Q.Bad, "sneak"); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                if (ok && GameAllows(3)) { Game(13); return; }   // the director already answered for this period
                s.Did.Add("refused");   // she is not in the mood for more today
                if (a == "wary_no") CampusLife.Memories.Add(h, "almost", "you asked, and I almost said yes", 1, true);
                else if (!ok)
                {
                    var no = Consent.NoToday(h);
                    Social.AddTrust(h, no >= 2 ? -3 : -1);   // pushing after a no hurts more
                    if (no >= 3) { h.anger = Mathf.Min(100, h.anger + 20); CampusLifePlugin.Log.LogInfo("Talk: " + GirlInfo.FirstName(h) + " is annoyed: asked for H " + no + " times today"); }
                }
                AfterBeat(s);
            }, false);
        }

        private static void Debt(SaveData.Heroine h, GirlState gs)
        {
            var s = Sess;
            var can = GameAllows(3);
            var b = new SceneBuilder(h, true);
            b.Face(h, Feel.Shy).Her_(T(h, "...About what I owe you. I haven't forgotten. || ...What I owe you. I remember. || ...I haven't forgotten my promise. || ...About what I owe...", "So... that thing I owe you. Yeah. I remember! || That thing I owe you! I remember! || My debt! Yep! || I owe you! I know!", "...My debt. I keep my word. || ...My debt. I keep my word. || ...I owe you. I remember. || ...Debt acknowledged.", "Ah, my little debt. Here to collect? || My debt? Collecting? || Here to collect? || Ah, the debt. Ready?"));
            if (can) b.Choice("Collect it now", "db_now", "Save it for later", "db_later");
            else b.Choice("Save it for somewhere more private", "db_later");
            b.Branch("db_now"); b.Face(h, Feel.Love).Her_(T(h, "...Okay. Now, then. || ...Okay. Now. || ...Alright. Right now. || ...Now, then.", "Now?! ...Okay! || Now?! Okay! || Right now?! Yes! || Now! Alright!", "...Fine. Now. || ...Now. || ...Fine. || ...Very well.", "Now? Bold. I like it. || Now? Bold. || Right now? I like it. || Eager. Good.")); Bounds(b, h); b.End();
            b.Branch("db_later"); b.Face(h, Feel.Smile).Her_(T(h, "...Then I'll be waiting. || ...I'll wait. || ...I'll be waiting for you. || ...Later, then.", "Later! It's a promise! || Later! Promise! || Later! Deal! || Okay! Later!", "...I'll remember. || ...Remembered. || ...Noted. || ...Later.", "I'll hold you to it. || I'll hold you to it. || Don't forget. || Later, then.")); b.End();
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                if (c == "db_now" && GameAllows(3)) { Game(13); return; }   // the game's invitation; the director (Consent) settles the debt there
                if (c == "db_later") CampusLife.Memories.Add(h, "promise", "you saved what I owe you for later", 0, true);
                AfterBeat(s);
            }, false);
        }

        private static void Caught(SaveData.Heroine h, GirlState gs)
        {
            var trust = Social.Trust(h);
            Beat(h, Lvl.Spicy, "caught:" + gs.Caught, b =>
            {
                b.Narr(GirlInfo.FirstName(h) + " goes red the moment you bring it up.");
                b.Face(h, Feel.Shy).Her_(T(h, "...You saw me. Back then. I know you did. || ...You saw me. I know. || ...Back then... you saw. || ...I know you saw me.", "You SAW! When I was... ugh! I know you saw! || You SAW! I know! || You walked in! Ugh! || You saw me! I know it!", "...About when you walked in on me. || ...When you walked in. || ...About that incident. || ...You saw.", "So. You walked in on me. Let's talk about that. || You walked in on me. Let's discuss. || So. You saw. Let's talk. || You got a show. Now talk."));
            }, new List<Resp>
            {
                Rp("Admit you saw everything", "cg_admit", Q.Good, Tn("...At least you're honest.", "Everything?! ...Okay. Okay.", "...Honest. Fine.", "Everything, huh? ...Good.")),
                Rp("Explain you walked in by accident", "cg_explain", trust >= 30 ? Q.Good : Q.Neutral, Tn("...I believe you.", "...Okay, okay, I believe you!", "...Fine.", "An accident. Sure~")),
                Rp("Promise to keep it between you two", "cg_secret", trust >= 30 ? Q.Good : Q.Neutral, Tn("...Thank you. Really.", "Promise?! Okay. Thanks.", "...Good.", "...Our secret, then."), () => Social.AddTrust(h, 3)),
                Rp("Tell her there's nothing to be ashamed of", "cg_feel", h.lewdness >= 50 ? Q.Good : Q.Neutral, Tn("...You really think so?", "...Really?", "...Hm.", "...Smooth.")),
                Rp("Let it go for now", "cg_space", Q.Neutral, Tn("...Okay.", "...Yeah. Let's drop it!", "...Fine.", "...Coward. ...Fine.")),
            }, r => { if (r != null && r.Tag != "cg_space") gs.CaughtTalked = gs.Caught; });
        }

        // ================= opening (only what must play by itself) =================

        /// <summary>
        /// What plays by itself when the talk opens: a relationship chapter that is due, the reply to an aside she just
        /// said on the map, or her refusing to talk while she is hurt. Everything else waits on the list (the opener).
        /// </summary>
        /// <summary>
        /// Your girlfriend and another girl (Share): she says it once, the way she decided (fine, a trio, or a fight), and
        /// everything else she knows about it is settled with it.
        /// </summary>
        private static bool ShareOpening(Session s, SaveData.Heroine h)
        {
            try
            {
                Share.FromLore(h);
                bool confront, saw;
                var other = Share.PendingOther(h, out confront, out saw);
                if (other == null) return false;
                var b = new SceneBuilder(h, true);
                var act = Share.Write(h, other, saw, b);
                Share.Hush(h, other);
                if (act == null) return false;
                Hide();
                Scenes.PlayInTalk(b, c => { try { act(c); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } s.Turns++; AfterScene(); }, false);
                return true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); return false; }
        }

        internal static bool RunSceneNow(string id)
        {
            var s = Sess;
            if (s == null) return false;
            var x = SceneTalk.Candidates(s.Her).FirstOrDefault(c => c.Id == id);
            return x != null && SceneTalk.Play(s.Her, x, d => s.Interest = Mathf.Clamp(s.Interest + d, 0, MaxInterest), () => { s.Turns++; AfterScene(); });
        }

        private static bool OpeningUnified(Session s)
        {
            if (s.Did.Contains("opening")) return false;
            s.Did.Add("opening");
            var h = s.Her;
            if (Pairs.IsScorned(h) && !Pairs.CanRedeem(h)) return SceneTalk.Open(h, null, () => AfterScene());
            // She asked you here (a note, a letter, "meet me at..."): that comes first, with its own scene (user,
            var meet = Board.MeetHere(h);
            if (meet != null && Board.PlayInTalk(meet, h, () => { s.Turns++; AfterScene(); })) return true;
            if (ShareOpening(s, h)) return true;
            // The same for the guilt after a wary H.
            var waiting = S.Girl(GirlInfo.Key(h)).Pending;
            if (waiting == "SceneTalk:feelings" || waiting == "SceneTalk:after_h")
            {
                Subject f = null;
                var id = waiting.Substring("SceneTalk:".Length);
                try { f = SceneTalk.Candidates(h).FirstOrDefault(c => c.Id == id); }
                catch (Exception e) { CampusLifePlugin.Log.LogDebug("Opening: " + e.Message); }
                if (f != null) return SceneTalk.Play(h, f, d => s.Interest = Mathf.Clamp(s.Interest + d, 0, MaxInterest), () => { s.Turns++; AfterScene(); });
            }
            // Something she saw or heard about you that weighs (the H in the hallway, you in the girls' restroom): she
            // brings it up herself.
            var raise = RaiseTopic(h);
            if (raise != null) { PlayTopic(h, raise); return true; }
            Subject x = null;
            try
            {
                x = SceneTalk.Candidates(h).Where(c => c.Form == Form.Aside || c.Id.StartsWith("season", StringComparison.Ordinal)).OrderByDescending(c => c.Weight).FirstOrDefault();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Opening: " + e.Message); }
            if (x == null) return false;
            return SceneTalk.Play(h, x, d => s.Interest = Mathf.Clamp(s.Interest + d, 0, MaxInterest), () => { s.Turns++; AfterScene(); });
        }
    }
}
