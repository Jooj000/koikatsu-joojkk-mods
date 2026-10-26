using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using KKAPI.MainGame;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    internal enum Topic { Classes, Club, Food, Sports, Hobbies, Rumors, Weekend, AboutHer, Love }

    /// <summary>
    /// Campus Life's conversation, in the game's own topic list and buttons. The talk screen's three buttons open three
    /// pages: the speech bubble talking (topics, let her talk, ask about, jokes, compliments, the game's small talk and
    /// listen), the heart romance and H, the question mark invitations, gifts and company. Game options only show when
    /// the game allows them. Every turn moves the game's favor and H bars at once and uses the game's talk time (the
    /// blue bar), half a point by default; Interest (hidden) decides trust and devotion at the end. What she brings up first
    /// comes from the social director: gossip she heard, things you did, friendship jealousy, a girlfriend who heard
    /// you were sweet on someone else.
    /// </summary>
    internal static partial class Conversation
    {
        private const int MaxInterest = 5;
        private static CampusState S { get { return Campus.S; } }

        /// <summary>The three menus of the conversation, one per button of the game's talk screen.</summary>
        internal enum Page { Talk, Romance, Actions }

        private sealed class Session
        {
            public List<string> SmallPicked = new List<string>();
            public SaveData.Heroine Her;
            public int Interest, Turns, FavorGained, LewdGained;
            public bool Finished;
            public readonly List<Topic> Used = new List<Topic>();
            public readonly HashSet<string> Did = new HashSet<string>();
        }

        private static TalkScene _scene;
        private static Session _s;
        private static Button _template, _talk, _listen, _event;
        private static Button[] _eventContents;
        private static Sprite[] _sprites;
        private static GameObject _root, _eventRoot, _holder;
        private static Transform _list;
        private static Page _page = Page.Talk;
        private static Action _sub;            // a sub-page (topics, ask about...) or null for the page's main list
        private static readonly List<GameObject> Created = new List<GameObject>();
        private static readonly List<KeyValuePair<Transform, int>> Moved = new List<KeyValuePair<Transform, int>>();
        private static readonly Dictionary<string, float> TalkCost = new Dictionary<string, float>();
        private static int _talkCostDay = -1;
        private static readonly MethodInfo CommandFunc = AccessTools.Method(typeof(TalkScene), "CommandFunc");

        /// <summary>Our buttons are showing.</summary>
        public static bool IsOpen { get { return Created.Count > 0; } }

        /// <summary>The conversation of this talk scene is ours (regular students with the new conversation on).</summary>
        public static bool Active { get { return _scene != null && _root != null; } }

        // ================= hooking into the talk scene =================

        /// <summary>Every frame while Campus Life is on.</summary>
        public static void Watch()
        {
            TalkScene ts;
            try { ts = G.TalkScene; } catch { ts = null; }
            SleepTease.Watch(ts);
            if (ts == null) { if (_scene != null) Close(); return; }
            if (ts != _scene || (!_decided && ts.targetHeroine != null)) Attach(ts);
            if (_root == null) return;
            TalkBusy();   // keeps the beat after a line closes (only measured while the talk runs)
            try { if (ForeignListeners()) HookButtons(); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Conversation rehook: " + e.Message); }
            // Safety net: whatever opened the list, it shows our page (never the game's buttons).
            var shown = _root.activeInHierarchy;
            if (shown && !IsOpen && !GiftMenu.IsOpen && !Scenes.Running && !_goodbye && !TrioWaiting) Build();
            if (Unified && (_listen.gameObject.activeSelf || _event.gameObject.activeSelf)) { _listen.gameObject.SetActive(false); _event.gameObject.SetActive(false); }
            else if (!shown && IsOpen && !GiftMenu.IsOpen) Clear();
            if (_eventRoot != null && _eventRoot.activeSelf) _eventRoot.SetActive(false);
            // After the game's own apology the game goes back to its selection with the list closed: open our page again
            // (its topics reopen the list by themselves, so the safety net above covers them).
            if (_reopenAt > 0f && !shown && !IsOpen && !Scenes.Running && !ADV.Program.isADVProcessing && CanvasOn(ts))
            {
                if (Time.realtimeSinceStartup >= _reopenAt) { _reopenAt = 0f; Reopen(); }
            }
            else if (_reopenAt > 0f && Time.realtimeSinceStartup - _reopenAt > 60f) _reopenAt = 0f;
            OpenOnEntry(ts);
            TickCatBar();
            TickReact(ts);
            TrioTick(ts);
        }

        /// <summary>Duo is bringing the second girl over: the director's scene first, then the talk with both.</summary>
        private static bool TrioWaiting { get { return Duo.TrioPending && _trio == null; } }

        /// <summary>
        /// What she brings up by herself (a note, something she saw) plays as soon as the game's greeting is over, not
        /// was built, and with the list closed it never was).
        /// </summary>
        private static void OpenOnEntry(TalkScene ts)
        {
            try
            {
                if (!Unified || _root == null || Scenes.Running || _goodbye || GiftMenu.IsOpen || ADV.Program.isADVProcessing || !CanvasOn(ts) || TrioWaiting) return;
                var s = Sess;
                if (s == null || s.Did.Contains("opening")) return;
                if (OpeningUnified(s)) Hide();
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Conversation entry opening: " + e.Message); }
        }

        private static float _reopenAt;
        private static readonly FieldInfo CanvasField = AccessTools.Field(typeof(TalkScene), "canvas");

        private static bool CanvasOn(TalkScene ts)
        {
            try
            {
                var c = CanvasField != null ? CanvasField.GetValue(ts) as Canvas : null;
                return c != null && c.enabled;
            }
            catch { return false; }
        }

        private static bool _decided;   // the heroine was known when we looked (the scene exists a moment before she is set)

        private static void Attach(TalkScene ts)
        {
            if (ts != _scene) Close();
            _scene = ts;
            var h = ts.targetHeroine;
            _decided = h != null;
            if (!Cfg.NewConversation.Value || h == null || !GirlInfo.IsRegular(h) && TeacherTalk.Speaking != h || Duo.TalkHer == h) return; // story girls: as the game made them
            try
            {
                var tr = Traverse.Create(ts);
                var contents = tr.Field("buttonTalkContents").GetValue<Button[]>();
                _root = tr.Field("objTalkContentsRoot").GetValue<GameObject>();
                _eventRoot = tr.Field("objEventContentsRoot").GetValue<GameObject>();
                _eventContents = tr.Field("buttonEventContents").GetValue<Button[]>();
                _talk = tr.Field("buttonTalk").GetValue<Button>();
                _listen = tr.Field("buttonListen").GetValue<Button>();
                _event = tr.Field("buttonEvent").GetValue<Button>();
                _sprites = tr.Field("spriteAction").GetValue<Sprite[]>();
                if (contents == null || contents.Length == 0 || _root == null || _talk == null || _listen == null || _event == null) { _root = null; return; }
                _template = contents[0];
                _list = _template.transform.parent;

                // The game's own buttons (and other mods' ones) move to a hidden holder for as long as the talk lasts,
                // so they never flash in our menu. Other mods' buttons are offered through proxies (Actions page).
                _holder = new GameObject("cl_hidden_talk_buttons");
                _holder.SetActive(false);
                _holder.transform.SetParent(_list.parent, false);
                foreach (var c in _list.Cast<Transform>().ToList())
                {
                    Moved.Add(new KeyValuePair<Transform, int>(c, c.GetSiblingIndex()));
                    c.SetParent(_holder.transform, false);
                }

                HookButtons();
                // The single conversation: five buttons (the speech bubble and four copies); the game's listen and heart
                // buttons go away for this talk.
                _listen.gameObject.SetActive(!Unified);
                if (Unified) { _event.gameObject.SetActive(false); BuildCatBar(); }
                _page = Page.Talk;
                _sub = null;
                _cat = null;
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError("Conversation hook: " + e);
                _root = null;
            }
        }

        /// <summary>
        /// The three buttons open our three pages. Replacing the click event drops the game's handlers for this talk
        /// scene only (Talk and Event toggled the game's lists, Listen played the game's "listen"). The game subscribes
        /// them at the end of TalkScene.Start, after the fade-in and after we attached, so <see cref="Watch"/> hooks
        /// again whenever a button has a listener that is not ours; before this, both ran (our page opened and the game
        /// closed it at once, or played its own listen).
        /// </summary>
        private static void HookButtons()
        {
            _talk.onClick = new Button.ButtonClickedEvent();
            if (Unified) _talk.onClick.AddListener(() => CatClick("Talk"));
            else _talk.onClick.AddListener(() => Toggle(Page.Talk));
            _event.onClick = new Button.ButtonClickedEvent();
            _event.onClick.AddListener(() => Toggle(Unified ? Page.Talk : Page.Romance));
            _listen.onClick = new Button.ButtonClickedEvent();
            _listen.onClick.AddListener(() => Toggle(Unified ? Page.Talk : Page.Actions));
            _hooked = Traverse.Create(_scene).Field("crossFade").GetValue<Object>() != null;
        }

        private static bool _hooked;   // hooked after the game's own subscriptions
        private static readonly FieldInfo EventCalls = AccessTools.Field(typeof(UnityEngine.Events.UnityEventBase), "m_Calls");
        private static FieldInfo _runtimeCalls;

        /// <summary>Listeners added with AddListener (-1 when Unity's internals are not as expected).</summary>
        private static int Listeners(UnityEngine.Events.UnityEventBase e)
        {
            try
            {
                var calls = EventCalls != null ? EventCalls.GetValue(e) : null;
                if (calls == null) return -1;
                if (_runtimeCalls == null) _runtimeCalls = AccessTools.Field(calls.GetType(), "m_RuntimeCalls");
                var list = _runtimeCalls != null ? _runtimeCalls.GetValue(calls) as System.Collections.ICollection : null;
                return list != null ? list.Count : -1;
            }
            catch { return -1; }
        }

        private static bool ForeignListeners()
        {
            var n = new[] { Listeners(_talk.onClick), Listeners(_event.onClick), Listeners(_listen.onClick) };
            if (n.Any(x => x < 0))
                return !_hooked && Traverse.Create(_scene).Field("crossFade").GetValue<Object>() != null; // fallback
            return n.Any(x => x > 1);
        }

        public static void Close()
        {
            _reopenAt = 0f;
            TrioCleanup();
            Finish(true);
            Clear();
            ClearCatBar();
            foreach (var m in Moved)
                if (m.Key != null && _list != null)
                {
                    m.Key.SetParent(_list, false);
                    m.Key.SetSiblingIndex(Mathf.Min(m.Value, _list.childCount - 1));
                }
            Moved.Clear();
            if (_listRt != null) _listRt.anchoredPosition = _listHome;
            _listRt = null;
            if (_holder != null) Object.Destroy(_holder);
            _holder = null;
            _s = null;
            _scene = null;
            _goodbye = false;
            _decided = false;
            _root = null;
            _eventRoot = null;
            _template = null;
            _list = null;
            _sub = null;
            _cat = null;
            _rightLimit = -1f;
        }

        private static Session Sess
        {
            get
            {
                var h = _scene != null ? _scene.targetHeroine : null;
                if (h == null) return null;
                if (_s == null || _s.Her != h) _s = Begin(h);
                return _s;
            }
        }

        // ================= opening and closing the lists =================

        private static bool RootOpen { get { return _root != null && _root.activeSelf; } }

        /// <summary>A talk screen button was clicked: open its page, or close the list if that page was showing.</summary>
        private static void Toggle(Page p)
        {
            try
            {
                if (Scenes.Running) return;
                if (GiftMenu.IsOpen) GiftMenu.Close();
                Sfx.Select();
                // Inside a category the speech bubble goes back to the five buttons; on them, it closes the list.
                var close = RootOpen && _page == p && _sub == null && _cat == null;
                _page = p;
                _sub = null;
                _cat = null;
                SetOpen(!close);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        private static void SetOpen(bool on)
        {
            if (_scene == null || _root == null) return;
            var tr = Traverse.Create(_scene);
            try { tr.Property("activeEventContents").SetValue(false); } catch { }
            try { tr.Property("activeTalkContents").SetValue(on); } catch { _root.SetActive(on); }
            try
            {
                if (_sprites != null && _sprites.Length > 5)
                {
                    _talk.image.sprite = _sprites[on && _page == Page.Talk ? 1 : 0];
                    _event.image.sprite = _sprites[4 + (on && _page == Page.Romance ? 1 : 0)];
                }
                _listen.image.color = on && _page == Page.Actions ? new Color(1f, 0.85f, 0.55f, 1f) : Color.white;
            }
            catch { }
            _catRefreshAt = 0f;   // the five buttons show the change (open one marked, or none) at once
            if (on) Build(); else Clear();
        }

        /// <summary>After one of our scenes: the same page again.</summary>
        private static void Reopen()
        {
            if (_scene == null || _root == null) return;
            SetOpen(true);
        }

        // ================= starting a conversation =================

        private static Session Begin(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.TalkDay != S.Day) { gs.TalkDay = S.Day; gs.TalksToday = 0; }
            gs.TalksToday++;
            var trust = Social.Trust(h);
            var mood = MoodOf(h);
            var interest = 2;
            if (mood == CampusLife.Mood.Cheerful || mood == CampusLife.Mood.Lonely || mood == CampusLife.Mood.Flirty) interest++;
            if (mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Stressed) interest--;
            if (trust >= 60) interest++;
            if (trust < 25) interest--;
            if (Social.Grudge(h) != null) interest--;
            Social.RecordTalk(h);
            return new Session { Her = h, Interest = Mathf.Clamp(interest, 0, MaxInterest) };
        }

        private static Mood MoodOf(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.MoodDay == S.Day ? gs.Mood : CampusLife.Mood.None;
        }

        /// <summary>What she brings up herself the first time the menu opens.</summary>
        private static bool Opening(Session s)
        {
            if (s.Did.Contains("opening")) return false;
            s.Did.Add("opening");
            var h = s.Her;
            // She asked you here (a note in your locker, a letter, "meet me at..."): that comes first, with its own scene
            //.
            var meet = Board.MeetHere(h);
            if (meet != null && Board.PlayInTalk(meet, h, () => { s.Turns++; AfterScene(); })) return true;
            if (ShareOpening(s, h)) return true;
            // Conversation by scene: the director may open with a scene (Scene Talk, Quiet, Triangle, Callback, an aside).
            if (SceneTalk.Open(h, d => s.Interest = Mathf.Clamp(s.Interest + d, 0, MaxInterest), () => { s.Turns++; AfterScene(); })) return true;
            var gs = S.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            Action<string> after = null;

            var inc = Social.Salient(h);
            var jealous = inc == null ? Social.JealousOf(h) : null;
            if (inc != null)
            {
                Social.Mentioned(h, inc);
                var about = Social.Get(inc.Girl);
                var aboutName = about != null ? GirlInfo.FirstName(about) : "";
                if (inc.Kind == Social.Kinds.Flirted && about != h && h.isGirlfriend)
                {
                    var take = about != null && Share.Accepts(h, about) ? "shrugs" : Consent.Cheat(h, about, Social.WeightOf(inc.What), Social.Saw(h, inc));
                    var minds = take == "hurt" || take == "furious";
                    b.Her_(ConvoLines.Cheating(tone, aboutName, minds, inc.What, Social.Saw(h, inc)));
                    if (minds) s.Interest = Math.Max(0, s.Interest - (take == "furious" ? 2 : 1));
                    if (take == "furious") h.anger = Math.Min(100, h.anger + 25);
                }
                else if (inc.Kind == Social.Kinds.HerBoysRoom)
                {
                    b.Her_(ConvoLines.Incident(tone, inc.Kind, aboutName, true, false));
                    b.Choice("Promise to keep it secret.", "keep", "Tease her about it.", "tease");
                    b.Branch("keep"); b.Her_(EverydaySecret(h, true)); b.End();
                    b.Branch("tease"); b.Her_(EverydaySecret(h, false)); b.End();
                    after = c =>
                    {
                        if (c == "keep") { Social.AddTrust(h, 8); G.AddFavor(h, 3, 0); Toasts.Show(GirlInfo.FirstName(h) + " trusts you with her secret. (+trust)"); }
                        else if (c == "tease") { Social.AddTrust(h, -5); G.AddFavor(h, 0, 4); s.Interest = Math.Max(0, s.Interest - 1); }
                    };
                }
                else
                {
                    var friend = about != null && about != h && Social.Bond(h, about) >= 35;
                    b.Her_(ConvoLines.Incident(tone, inc.Kind, aboutName, about == h, friend, inc.What, Social.Saw(h, inc)));
                }
            }
            else if (jealous != null)
            {
                gs.JealousDay = S.Day;
                b.Her_(RomanceSay(h, "jealous", Interested(h) ? "love" : "plain", null, GirlInfo.FirstName(jealous)));
                s.Interest = Math.Max(0, s.Interest - 1);
                s.Did.Add("jealous");
            }
            else if (Board.HiddenFor(GirlInfo.Key(h)).Any() && (G.Int >= 40 || Random.value < 0.3f))
            {
                b.Narr(ConvoLines.WorriedHint(GirlInfo.FirstName(h)) + " <color=#6ab4ff>[INT]</color> Ask her... > Just listen.");
            }
            else return false;

            Scenes.PlayInTalk(b, c =>
            {
                if (after != null) after(c);
                Reopen();
            }, false);
            return true;
        }

        // ================= menu pages =================

        /// <summary>Fills the list with the current page (or sub-page).</summary>
        private static void Build()
        {
            var s = Sess;
            if (s == null || _list == null || TrioWaiting) return;
            if (Unified ? OpeningUnified(s) : Opening(s)) { Hide(); return; }
            Clear();
            if (Unified)
            {
                UnifiedPage();
                if (Created.Count == 0) return;   // the game's goodbye is on its way
                try { Uniform(); } catch (Exception e) { CampusLifePlugin.Log.LogDebug("Conversation layout: " + e.Message); }
                return;
            }
            if (_sub != null) _sub();
            else if (_page == Page.Romance) RomancePage();
            else if (_page == Page.Actions) ActionsPage();
            else TalkPage();
            if (Created.Count == 0) Add("<color=#9a9a9a>Nothing here right now.</color>", null);
            try { Uniform(); } catch (Exception e) { CampusLifePlugin.Log.LogDebug("Conversation layout: " + e.Message); }
        }

        /// <summary>
        /// One text size for the whole list (auto-sizing made short labels huge and long ones tiny), and every button
        /// as wide as the longest label needs.
        /// </summary>
        private static void Uniform()
        {
            var items = Created.Where(g => g != null).Select(g => new { Go = g, Txt = g.GetComponentInChildren<TextMeshProUGUI>(true) }).Where(x => x.Txt != null).ToList();
            if (items.Count == 0) return;
            var size = Mathf.Round(items[0].Txt.fontSizeMax * 0.8f);
            var pad = ConvArt.CardInside((RectTransform)items[0].Go.transform);
            var extra = 0f;
            foreach (var x in items)
            {
                x.Txt.enableAutoSizing = false;
                x.Txt.fontSize = size;
                x.Txt.enableWordWrapping = false;
                // the text box is the whole card; the margins keep the letters inside its frame
                var trt = (RectTransform)x.Txt.transform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                x.Txt.margin = Vector4.zero;
                var need = x.Txt.GetPreferredValues(x.Txt.text).x + pad.x + pad.z;
                var have = ((RectTransform)x.Go.transform).rect.width;
                if (have > 1f) extra = Mathf.Max(extra, need - have);
            }
            // Widen a little (all buttons alike); a label still too long goes on two lines and its card grows by a line
            // (user, 2026-10-26: "the options that overflow the width get one more line of height"), so its text never
            // runs out of the button over the game's buttons beside the list.
            extra = Mathf.Clamp(extra, 0f, 140f);
            foreach (var x in items)
            {
                if (extra > 0f)
                {
                    var rt = (RectTransform)x.Go.transform;
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rt.rect.width + extra);
                    var le = x.Go.GetComponent<LayoutElement>();
                    if (le != null && le.preferredWidth > 0f) le.preferredWidth += extra;
                }
                Fit(x.Go, x.Txt, pad);
                x.Txt.overflowMode = TextOverflowModes.Ellipsis;   // only past two lines
            }
            KeepClear();
        }

        /// <summary>
        /// The label inside the card's frame (<paramref name="pad"/>): it wraps when it does not fit the width, and the card
        /// grows to hold its lines (two at most) and the margins.
        /// </summary>
        private static void Fit(GameObject go, TextMeshProUGUI txt, Vector4 pad)
        {
            var rt = (RectTransform)go.transform;
            var width = rt.rect.width - pad.x - pad.z;
            txt.margin = Vector4.zero;   // measured without margins, so the sizes below are the letters' own
            var one = txt.GetPreferredValues("Ag").y;
            var wrap = width > 1f && txt.GetPreferredValues(txt.text).x > width;
            txt.enableWordWrapping = wrap;
            var lines = wrap ? Mathf.Min(txt.GetPreferredValues(txt.text, width, 0f).y, one * 2.1f) : one;
            txt.margin = pad;
            var h = lines + pad.y + pad.w;
            if (h <= rt.rect.height + 0.5f) return;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            var le = go.GetComponent<LayoutElement>();
            if (le != null) { if (le.preferredHeight > 0f) le.preferredHeight = h; if (le.minHeight > 0f) le.minHeight = h; }
        }

        private static RectTransform _listRt;
        private static Vector2 _listHome;

        /// <summary>
        /// The widened buttons grew over the game's orange buttons (Talk / Listen / Event / End) beside the list. The
        /// overlap is measured on screen and the list moves sideways just enough to clear them; it goes back to its
        /// place when the talk closes.
        /// </summary>
        private static void KeepClear()
        {
            var lrt = _list as RectTransform;
            if (lrt == null) return;
            if (_listRt != lrt) { _listRt = lrt; _listHome = lrt.anchoredPosition; }
            lrt.anchoredPosition = _listHome;
            LayoutRebuilder.ForceRebuildLayoutImmediate(lrt);
            var canvas = lrt.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var opts = Created.Where(g => g != null && g.activeInHierarchy).Select(g => OnScreen((RectTransform)g.transform, cam)).ToList();
            if (opts.Count == 0) return;
            Button end = null;
            try { end = Traverse.Create(_scene).Field("buttonEnd").GetValue<Button>(); } catch { }
            var shift = 0f;
            foreach (var b in new[] { _talk, _listen, _event, end })
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                var br = OnScreen((RectTransform)b.transform, cam);
                foreach (var o in opts)
                {
                    if (!o.Overlaps(br)) continue;
                    if (br.center.x >= o.center.x) shift = Mathf.Min(shift, br.xMin - o.xMax - 10f); // button on the right: list to the left
                    else shift = Mathf.Max(shift, br.xMax - o.xMin + 10f);
                }
            }
            // Never past the right edge of the screen.
            // a category's widened buttons covered it). The first build of a talk is the five short category buttons.
            var right = opts.Max(o => o.xMax);
            if (_rightLimit < 0f) _rightLimit = Unified && _talk != null ? OnScreen((RectTransform)_talk.transform, cam).xMin - 12f : Mathf.Min(right, Screen.width - 8f);
            if (right > _rightLimit) shift = Mathf.Min(shift, _rightLimit - right);
            if (Mathf.Abs(shift - _lastShift) > 0.5f)
                CampusLifePlugin.Log.LogInfo("Talk: list right edge " + right.ToString("0") + " of " + Screen.width + " px, shift " + shift.ToString("0"));
            var dy = Unified ? _barDy : 0f;   // the list goes up or down with the category column
            if (Mathf.Abs(shift) < 0.5f && Mathf.Abs(dy) < 0.5f) { _lastShift = 0f; return; }
            var parent = lrt.parent as RectTransform;
            if (parent == null) return;
            Vector2 a, c;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Vector2.zero, cam, out a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(shift, dy), cam, out c);
            lrt.anchoredPosition = _listHome + (c - a);
            if (Mathf.Abs(shift) < 0.5f) { _lastShift = 0f; return; }
            if (Mathf.Abs(shift - _lastShift) > 0.5f) CampusLifePlugin.Log.LogInfo("Conversation list moved " + shift.ToString("0") + " px clear of the game's buttons");
            _lastShift = shift;
        }

        private static float _lastShift, _rightLimit = -1f;

        private static readonly Vector3[] Corners = new Vector3[4];

        private static Rect OnScreen(RectTransform rt, Camera cam)
        {
            rt.GetWorldCorners(Corners);
            var min = RectTransformUtility.WorldToScreenPoint(cam, Corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(cam, Corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private static void ShowSub(Action page)
        {
            _sub = page;
            Build();
        }

        private static void Back()
        {
            _sub = null;
            Build();
        }

        /// <summary>Closes the list while one of our scenes plays (the page comes back afterwards).</summary>
        private static void Hide()
        {
            Clear();
            try { Traverse.Create(_scene).Property("activeTalkContents").SetValue(false); } catch { if (_root != null) _root.SetActive(false); }
        }

        /// <summary>Is the game's own event button i available now (the game decides in UpdateUI: period, place, relation, anger)?</summary>
        private static bool GameAllows(int i)
        {
            try { return _eventContents != null && i >= 0 && i < _eventContents.Length && _eventContents[i] != null && _eventContents[i].gameObject.activeSelf; }
            catch { return false; }
        }

        // ---------------- grouping ----------------

        /// <summary>
        /// Related options under one entry that opens a sub-page (with Back). When there are fewer than
        /// <paramref name="minForSub"/> of them they are listed as they are: no sub-page for a single option.
        /// The list is built again each time, so it always matches what she allows right now.
        /// </summary>
        private static void Group(string label, Func<List<KeyValuePair<string, Action>>> make, int minForSub = 3)
        {
            var items = make();
            if (items.Count == 0) return;
            if (items.Count < minForSub) { foreach (var i in items) Add(i.Key, i.Value); return; }
            Add(label, () => ShowSub(() =>
            {
                foreach (var i in make()) Add(i.Key, i.Value);
                Add("Back", Back);
            }));
        }

        private static KeyValuePair<string, Action> Opt(string label, Action a) { return new KeyValuePair<string, Action>(label, a); }

        // ---------------- speech bubble: talking ----------------

        private static void TalkPage()
        {
            var s = Sess;
            var h = s.Her;
            var req = Board.CompletableInTalk(h);
            if (req != null) Add("<color=#FFD27A>★</color> " + Board.ButtonLabel(req), () => { var r = Board.CompletableInTalk(h); if (r != null) { Hide(); Board.PlayInTalk(r, h); } });
            var msg = MessageFor(h);
            if (msg != null) Add("<color=#FFD27A>★</color> " + msg.Button, () => { var r = MessageFor(h); if (r != null) { Hide(); PassMessage(r, h); } });
            if (NeedsClearing(h, s)) Add("<color=#ff9b6a>Clear the air...</color>", () => ShowSub(ClearPage));
            // Conversation by scene: three concrete things to say, built from her mood, the place, what you share and her
            // friends (never the profile quiz). The old pages stay one level down under "Something else...".
            if (Cfg.ConversationForms.Value)
            {
                foreach (var o in ConcreteOptions(s)) { var opt = o; Add(opt.Label, () => opt.Run()); }
                Add("Something else...", () => ShowSub(ClassicPage));
                return;
            }
            ClassicEntries(s);
        }

        /// <summary>The conversation's first pages as they were (chat topics, asking, compliments, a joke).</summary>
        private static void ClassicEntries(Session s)
        {
            // Four entries at most besides the two that only show when needed:
            // listening lives under "Ask her...", the game's own talk under "Chat about...".
            Add("Chat about...", () => ShowSub(TopicPage));
            Add("Ask her...", () => ShowSub(AskPage));
            Add("Compliment her...", () => ShowSub(ComplimentPage));
            Add("Joke around" + JokeOdds(s), () => Joke());
        }

        private static void ClassicPage()
        {
            ClassicEntries(Sess);
            Add("Back", Back);
        }

        // ---------------- heart: romance and H ----------------

        private static void RomancePage()
        {
            var s = Sess;
            var h = s.Her;
            // Sweet talk and flirting: subjects for how close she is to you (and how lewd), each with her own answers
            // and a way to go on; they change as you get closer (before, the heart page was the same list for everyone).
            Group("<color=#ff8ad8>Sweet talk...</color>", () =>
            {
                var l = HeartSubjects(h, false, CanFlirt(h) && !s.Did.Contains("hand") ? 2 : 3).Select(x => Opt(SubjectLabel(x, h, s), () => RunSubject(x))).ToList();
                if (CanFlirt(h) && !s.Did.Contains("hand")) l.Add(Opt("<color=#ff8ad8>Hold her hand</color>", HoldHand));
                return l;
            }, 1);
            Group("<color=#ff8ad8>Flirt...</color>", () =>
            {
                var sneak = Cfg.LewdRequests.Value && !s.Did.Contains("sneak") && (h.isGirlfriend || h.lewdness >= 50);
                var l = (CanFlirt(h) || h.lewdness >= 30 ? HeartSubjects(h, true, sneak ? 2 : 3) : new List<TalkSubject>()).Select(x => Opt(SubjectLabel(x, h, s), () => RunSubject(x))).ToList();
                if (sneak) l.Add(Opt("<color=#ff8ad8>Sneak off together</color>" + Odds(SneakChance(s)), Sneak));
                return l;
            }, 1);
            if (GameAllows(3)) Add("<color=#ff8ad8>Invite her to H</color>", () => Game(13));
            Group("<color=#ff8ad8>Us two...</color>", () =>
            {
                var l = new List<KeyValuePair<string, Action>>();
                if (GameAllows(1)) l.Add(Opt("<color=#ff8ad8>Confess your feelings</color>", () => Game(11)));
                if (GameAllows(7)) l.Add(Opt("<color=#ff8ad8>Ask her on a date</color>", () => Game(17)));
                if (GameAllows(2)) l.Add(Opt("<color=#9a9a9a>Break up</color>", () => Game(12)));
                return l;
            }, 2);
        }

        // ---------------- question mark: invitations, gifts, company ----------------

        private static void ActionsPage()
        {
            var s = Sess;
            var h = s.Her;
            Group("Invite her...", () =>
            {
                var l = new List<KeyValuePair<string, Action>>();
                if (GameAllows(4)) l.Add(Opt("Have lunch together", () => Game(14)));
                if (GameAllows(8)) l.Add(Opt("Study together", () => Game(18)));
                if (GameAllows(9)) l.Add(Opt("Exercise together", () => Game(19)));
                if (GameAllows(5)) l.Add(Opt("Club activities together", () => Game(15)));
                if (GameAllows(6)) l.Add(Opt("Go home together", () => Game(16)));
                if (GameAllows(10)) l.Add(Opt("To the Koikatsu Club", () => Game(20)));
                if (GameAllows(11)) l.Add(Opt("Come with me", () => Game(21)));
                return l;
            });
            if (Cfg.GiftsEnabled.Value) Add("Give a gift", () => { var t = Created.Select(c => c.GetComponent<Button>()).FirstOrDefault(b => b != null); if (t == null) return; GiftMenu.Open(t, h); });
            if (Cfg.ConversationForms.Value) Group("Ask a favor...", () => FavorOptions(s), 1);
            Group("Other mods...", () => OtherModButtons().Select(b => Opt(LabelOf(b), () => { Hide(); b.onClick.Invoke(); })).ToList());
        }
        /// <summary>Buttons other mods put in the game's lists, still offered (we press them for the player).</summary>
        private static List<Button> OtherModButtons()
        {
            var result = new List<Button>();
            try
            {
                var vanilla = new HashSet<Button>(_eventContents ?? new Button[0]);
                var talkContents = Traverse.Create(_scene).Field("buttonTalkContents").GetValue<Button[]>();
                if (talkContents != null) foreach (var b in talkContents) vanilla.Add(b);
                var pools = new List<Transform>();
                if (_holder != null) pools.Add(_holder.transform);
                if (_eventRoot != null) pools.AddRange(_eventRoot.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<Button>() != null).Select(t => t.parent).Distinct());
                foreach (var pool in pools.Distinct())
                    foreach (Transform c in pool)
                    {
                        var b = c.GetComponent<Button>();
                        if (b == null || vanilla.Contains(b) || TalkButtons.IsOurs(b) || !c.gameObject.activeSelf) continue;
                        result.Add(b);
                    }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug(e); }
            return result;
        }

        private static string LabelOf(Button b)
        {
            var t = b.GetComponentInChildren<TextMeshProUGUI>(true);
            if (t != null && !string.IsNullOrEmpty(t.text)) return t.text;
            var u = b.GetComponentInChildren<Text>(true);
            return u != null && !string.IsNullOrEmpty(u.text) ? u.text : b.name;
        }

        // ---------------- chat about topics ----------------

        private static Topic HotTopic { get { return (Topic)((S.Day * 5 + 3) % 8); } }

        private static int Score(SaveData.Heroine h, Topic t, Session s)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var sc = 1;
            switch (GirlInfo.ToneOf(h))
            {
                case Tone.Gentle: if (t == Topic.Hobbies || t == Topic.AboutHer) sc += 2; if (t == Topic.Food) sc += 1; if (t == Topic.Rumors) sc -= 1; break;
                case Tone.Energetic: if (t == Topic.Sports || t == Topic.Weekend) sc += 2; if (t == Topic.Food) sc += 1; if (t == Topic.Classes) sc -= 1; break;
                case Tone.Cool: if (t == Topic.Classes || t == Topic.Hobbies) sc += 2; if (t == Topic.Rumors) sc -= 2; break;
                case Tone.Teasing: if (t == Topic.Rumors || t == Topic.Weekend || t == Topic.Love) sc += 2; if (t == Topic.Classes) sc -= 1; break;
            }
            switch (MoodOf(h))
            {
                case CampusLife.Mood.Cheerful: sc += 1; break;
                case CampusLife.Mood.Hungry: if (t == Topic.Food) sc += 3; break;
                case CampusLife.Mood.Bored: if (t == Topic.Rumors || t == Topic.Weekend) sc += 2; if (t == Topic.Classes) sc -= 2; break;
                case CampusLife.Mood.Stressed: if (t == Topic.Classes) sc -= 3; if (t == Topic.Hobbies || t == Topic.Weekend) sc += 2; break;
                case CampusLife.Mood.Lonely: if (t == Topic.AboutHer) sc += 3; break;
                case CampusLife.Mood.Flirty: if (t == Topic.Love) sc += 3; if (t == Topic.Weekend || t == Topic.Hobbies) sc += 1; break;
                case CampusLife.Mood.Sleepy: if (t == Topic.Sports) sc -= 2; if (t == Topic.Weekend) sc += 1; break;
                case CampusLife.Mood.Upset: sc -= 1; if (t == Topic.AboutHer) sc += 3; if (t == Topic.Rumors) sc -= 2; break;
            }
            if (t == Topic.Love) sc += h.isGirlfriend ? 2 : h.relation >= 1 ? 0 : -3;
            if (t == Topic.Club) sc += GirlInfo.InNoClub(h) ? -2 : 2;
            if (t == Topic.Sports && GirlInfo.InSportsClub(h)) sc += 1;
            if (t == HotTopic) sc += 1;
            if (s.Used.Contains(t)) sc -= 4;
            else if (gs.ChatDay == S.Day - 1 && (gs.LastTopics ?? "").Split(',').Contains(t.ToString())) sc -= 1;
            return sc;
        }

        private static bool CanRead(SaveData.Heroine h) { return S.Girl(GirlInfo.Key(h)).MoodKnownDay == S.Day || G.Int >= 50; }

        private static void TopicPage()
        {
            var s = Sess;
            // Things to talk about, each with its own answers (not a topic name with a generic reply).
            foreach (var x in TalkSubjects(s.Her, 3))
            {
                var subj = x;
                Add(SubjectLabel(subj, s.Her, s), () => RunSubject(subj));
            }
            Group("<color=#b0b0b0>The game's talk...</color>", () => new List<KeyValuePair<string, Action>>
            {
                Opt("<color=#b0b0b0>Small talk (game)</color>", () => Game(0)),
                Opt("<color=#b0b0b0>Listen (game)</color>", () => Game(3)),
            }, 2);
            Add("Back", Back);
        }

        private static void PickTopic(Topic t) { PickTopic(t, null, 0); }

        /// <param name="mine">The player's line (a concrete option says it its own way), or null for the topic's opener.</param>
        /// <param name="bonus">How much the concrete situation adds to the topic's score.</param>
        private static void PickTopic(Topic t, string mine, int bonus)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var sc = Score(h, t, s) + bonus;
            s.Used.Add(t);
            var delta = sc >= 4 ? 2 : sc >= 2 ? 1 : sc >= 0 ? 0 : -1;
            var b = new SceneBuilder(h, true);
            b.Me(mine ?? Opener(t));
            if (delta > 0) b.Her_(t == Topic.Love ? RomanceSay(h, "love", "") : EverydayTopic(h, t));
            else if (delta == 0) b.Her_(Everyday(h, "topic.meh"));
            else b.Her_(Everyday(h, "topic.bad"));
            Play(b, delta, delta >= 2 ? "♪♪ She loved that topic." : delta == 1 ? "♪ She liked that topic." : null, null, t == Topic.Love && delta > 0 ? 2 : 0);
        }

        // ---------------- let her talk ----------------

        private static void LetHerTalk() { LetHerTalk(null); }

        private static void LetHerTalk(string said)
        {
            var s = Sess;
            var h = s.Her;
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(said ?? ConvoLines.PlayerListens());
            var delta = 0;
            string toast = null;
            Action done = null;

            var mine = Board.HiddenFor(key).FirstOrDefault();
            var friendReq = mine == null && Random.value < 0.5f
                ? Social.FriendsOf(h).SelectMany(f => Board.HiddenFor(GirlInfo.Key(f))).FirstOrDefault() : null;
            var gossip = Social.Gossip(h);
            var mood = MoodOf(h);
            var vent = gs.VentDay != S.Day && (mood == CampusLife.Mood.Stressed || mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Lonely);

            if (mine != null && Social.Trust(h) >= 15)
            {
                b.Her_(Board.ProblemFirst(mine, tone));
                done = () => Board.Reveal(mine, "her");
                delta = 1;
                Social.AddTrust(h, 1);
            }
            else if (vent)
            {
                gs.VentDay = S.Day;
                b.Her_(EverydayVent(h, mood));
                b.Her_(Everyday(h, "vent.thanks"));
                delta = 1;
                done = () => { Social.AddTrust(h, 3); if (mood != CampusLife.Mood.Lonely || Random.value < 0.6f) gs.Mood = CampusLife.Mood.None; };
                toast = GirlInfo.FirstName(h) + " feels better after talking. (+trust)";
            }
            else if (friendReq != null)
            {
                var f = Social.Get(friendReq.Girl);
                b.Her_(ConvoLines.FriendProblem(tone, GirlInfo.FirstName(f), Board.ProblemThird(friendReq)));
                done = () => Board.Reveal(friendReq, key);
            }
            else if (gossip != null)
            {
                var about = Social.Get(gossip.Girl);
                b.Her_(ConvoLines.GossipLine(tone, gossip.Kind, about != null ? GirlInfo.FirstName(about) : "", gossip.What));
                Social.Mentioned(h, gossip);
            }
            else if (!s.Did.Contains("anecdote"))
            {
                s.Did.Add("anecdote");
                b.Her_(Everyday(h, "anecdote"));
                delta = GirlInfo.ToneOf(h) == Tone.Cool ? 0 : 1;
            }
            else b.Her_(Everyday(h, "nothing"));
            Play(b, delta, toast, done);
        }

        // ---------------- ask about ----------------

        private static void AskPage()
        {
            var s = Sess;
            Add("Just listen", () => LetHerTalk());
            Add("How's your day?", () => AskDay());
            foreach (var f in Social.FriendsOf(s.Her).Take(2).ToList())
            {
                var friend = f;
                Add("About " + GirlInfo.FirstName(f), () => AskFriend(friend));
            }
            var rival = Social.Students.Where(o => o != s.Her && Social.Bond(s.Her, o) <= -30).OrderBy(_ => Random.value).FirstOrDefault();
            if (rival != null && Random.value < 0.5f && Created.Count < 4) Add("About " + GirlInfo.FirstName(rival), () => AskFriend(rival));
            Add("About the university", AskSchool);
            Add("Back", Back);
        }

        private static void AskDay() { AskDay(null); }

        private static void AskDay(string mine)
        {
            var s = Sess;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            var b = new SceneBuilder(h, true);
            b.Me(mine ?? AskMe(h));
            var already = gs.MoodKnownDay == S.Day;
            b.Her_(already ? AskAgain(h) : AskMood(h, gs.Mood));
            string newLike = null;
            if (!already)
            {
                var unknown = GirlInfo.LikesOf(h).Where(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) == 0).ToList();
                if (unknown.Count > 0 && Random.value < 0.35f + G.Int / 200f)
                {
                    newLike = unknown[Random.Range(0, unknown.Count)];
                    b.Her_(Lines.LikeHint(tone, GirlInfo.LikeLabel(newLike), close));
                }
            }
            Play(b, already ? 0 : 1, null, () =>
            {
                if (already) return;
                gs.MoodKnownDay = S.Day;
                var tip = Lines.MoodTip(gs.Mood);
                Toasts.Show(GirlInfo.FirstName(h) + ": " + Lines.MoodName(gs.Mood) + (tip == "" ? "" : ". " + tip));
                if (newLike == null) return;
                gs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, newLike);
                Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + GirlInfo.LikeLabel(newLike) + ".");
            });
        }

        private static void AskFriend(SaveData.Heroine f) { AskFriend(f, null); }

        private static void AskFriend(SaveData.Heroine f, string mine)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var name = GirlInfo.FirstName(f);
            var fs = S.Girl(GirlInfo.Key(f));
            var b = new SceneBuilder(h, true);
            b.Me(mine ?? ConvoLines.AskFriend(name));
            var bond = Social.Bond(h, f);
            var delta = 0;
            Action done = null;
            if (bond <= -30)
            {
                b.Her_(ConvoLines.Rival(tone, name));
                delta = -1;
            }
            else
            {
                var hidden = Board.HiddenFor(GirlInfo.Key(f)).FirstOrDefault();
                if (hidden != null && Social.Trust(h) >= 30)
                {
                    b.Her_(ConvoLines.FriendProblem(tone, name, Board.ProblemThird(hidden)));
                    done = () => Board.Reveal(hidden, GirlInfo.Key(h));
                }
                else
                {
                    var fm = fs.MoodDay == S.Day ? fs.Mood : CampusLife.Mood.None;
                    b.Her_(ConvoLines.FriendMood(tone, name, fm));
                    if (fm != CampusLife.Mood.None) done = () => { fs.MoodKnownDay = S.Day; Toasts.Show(name + ": " + Lines.MoodName(fm) + "."); };
                    if (bond >= 60 && !s.Did.Contains("bond:" + name)) { s.Did.Add("bond:" + name); b.Her_(ConvoLines.FriendBond(tone, name)); delta = 1; }
                }
            }
            Play(b, delta, null, done);
        }

        private static void AskSchool()
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.AskSchool());
            b.Her_(EverydayRep(h, S.Reputation));
            var gossip = Social.Gossip(h);
            if (gossip != null)
            {
                var about = Social.Get(gossip.Girl);
                b.Her_(ConvoLines.GossipLine(tone, gossip.Kind, about != null ? GirlInfo.FirstName(about) : "", gossip.What));
                Social.Mentioned(h, gossip);
            }
            Play(b, 0, "Reputation: " + S.Reputation + "/100");
        }

        // ---------------- jokes, compliments, flirting ----------------

        private static float JokeChance(Session s)
        {
            var h = s.Her;
            var c = GirlInfo.ToneOf(h) == Tone.Energetic ? 0.7f : GirlInfo.ToneOf(h) == Tone.Teasing ? 0.65f : GirlInfo.ToneOf(h) == Tone.Gentle ? 0.55f : 0.4f;
            c += G.Sens / 250f + s.Interest * 0.03f;
            var m = MoodOf(h);
            if (m == CampusLife.Mood.Upset || m == CampusLife.Mood.Stressed) c -= 0.2f;
            if (s.Did.Contains("joke")) c -= 0.2f;
            return Mathf.Clamp(c, 0.05f, 0.95f);
        }

        private static string JokeOdds(Session s) { return G.Int >= 40 ? "  <color=#6ab4ff>(" + Mathf.RoundToInt(JokeChance(s) * 100) + "%)</color>" : ""; }

        private static void Joke(string mine = null)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var chance = JokeChance(s);
            s.Did.Add("joke");
            var roll = Random.value;
            var b = new SceneBuilder(h, true);
            b.Me(mine ?? ConvoLines.PlayerJoke());
            if (roll < chance) { b.Her_(Everyday(h, "joke.good")); Play(b, roll < chance * 0.4f ? 2 : 1, "♪ She laughed."); return; }
            var hurt = roll > 0.92f && (tone == Tone.Gentle || MoodOf(h) == CampusLife.Mood.Upset);
            b.Her_(Everyday(h, hurt ? "joke.hurt" : "joke.bad"));
            Play(b, -1, hurt ? "That joke hurt her feelings." : null, hurt ? (Action)(() => Social.Add(Social.Kinds.Rude, h)) : null);
        }

        private static void ComplimentPage()
        {
            var s = Sess;
            Add("Her hard work <color=#ff8ad8>[SEN]</color>", () => Compliment("effort"));
            if (!GirlInfo.InNoClub(s.Her)) Add("Her club activities", () => Compliment("club"));
            Add("How smart she is <color=#6ab4ff>[INT]</color>", () => Compliment("smart"));
            Add("Her looks", () => Compliment("looks"));
            Add("Back", Back);
        }

        private static void Compliment(string kind, string mine = null)
        {
            var s = Sess;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(mine ?? RomanceMe(h, "compliment", kind == "effort" || kind == "club" || kind == "smart" ? kind : "looks"));
            if (gs.ComplimentDay >= S.Day - 1)
            {
                RomanceSaid("compliment", gs.ComplimentDay == S.Day ? "today" : "yesterday");   // "you said that yesterday" only the next day
                b.Her_(RomanceSay(h, "compliment", "again"));
                Play(b, -1, null);
                return;
            }
            var stat = kind == "smart" ? G.Int : G.Sens;
            var chance = 0.35f + stat / 200f + (kind == "club" ? 0.15f : 0f) + (kind == "looks" ? (h.relation >= 1 ? 0.1f : -0.15f) : 0f);
            if (tone == Tone.Cool) chance -= 0.1f;
            var ok = Random.value < chance;
            b.Her_(ok ? RomanceSay(h, "compliment", "yes", kind == "looks" ? "looks" : "other") : RomanceSay(h, "compliment", "no"));
            Play(b, ok ? 1 : 0, ok ? "♪ She's happy you noticed." : null, () => gs.ComplimentDay = S.Day);
        }

        private static bool CanFlirt(SaveData.Heroine h) { return h.isGirlfriend || h.relation >= 1 || h.favor >= 50; }

        private static float FlirtChance(Session s)
        {
            var h = s.Her;
            if (h.isGirlfriend) return 0.95f;
            var c = 0.2f + h.favor / 200f + G.Sens / 300f + s.Interest * 0.04f;
            if (MoodOf(h) == CampusLife.Mood.Flirty) c += 0.2f;
            if (GirlInfo.ToneOf(h) == Tone.Cool) c -= 0.1f;
            return Mathf.Clamp(c, 0.05f, 0.9f);
        }

        private static string FlirtOdds(Session s) { return G.Sens >= 40 ? "  <color=#ff8ad8>(" + Mathf.RoundToInt(FlirtChance(s) * 100) + "%)</color>" : ""; }

        private static void Flirt()
        {
            var s = Sess;
            var h = s.Her;
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "flirt"));
            if (s.Did.Contains("flirt")) { b.Her_(RomanceSay(h, "flirt", "again")); Play(b, 0, null); return; }
            s.Did.Add("flirt");
            var ok = Random.value < FlirtChance(s);
            b.Her_(RomanceSay(h, "flirt", ok ? "yes" : "no"));
            var witnesses = Witness.Who(h, Witness.Flirt);
            Play(b, ok ? 2 : -1, ok ? "♥ " + GirlInfo.FirstName(h) + " is blushing." : null, ok ? (Action)(() =>
            {
                Social.AddTrust(h, 2);
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 2);
                Social.Add(Social.Kinds.Flirted, h, witnesses, Witness.Flirt);
            }) : null, ok ? 4 : 0);
        }

        /// <summary>The odds in pink once SEN is 40, like the flirting ones.</summary>
        private static string Odds(float chance) { return G.Sens >= 40 ? "  <color=#ff8ad8>(" + Mathf.RoundToInt(chance * 100) + "%)</color>" : ""; }

        private static float Bonus(SaveData.Heroine h)
        {
            var c = MoodOf(h) == CampusLife.Mood.Flirty ? 0.2f : 0f;
            var tone = GirlInfo.ToneOf(h);
            if (tone == Tone.Teasing) c += 0.1f;
            else if (tone == Tone.Cool) c -= 0.1f;
            return c;
        }

        private static void HoldHand()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("hand");
            var chance = h.isGirlfriend ? 1f : Mathf.Clamp(0.25f + h.favor / 150f + G.Sens / 400f + Bonus(h), 0.05f, 0.95f);
            var ok = Random.value < chance;
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "hand"));
            b.Her_(RomanceSay(h, "hand", ok ? "yes" : "no"));
            var witnesses = Witness.Who(h, Witness.Hand);
            Play(b, ok ? 2 : -1, ok ? "♥ " + GirlInfo.FirstName(h) + " holds your hand." : null, ok ? (Action)(() =>
            {
                Social.AddTrust(h, 2);
                Social.Add(Social.Kinds.Flirted, h, witnesses, Witness.Hand);
            }) : null, ok ? 2 : 0);
        }

        private static void AskFeelings()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("feelings");
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "feelings"));
            b.Her_(FeelingsLine(h));
            Play(b, h.favor >= 50 ? 1 : 0, null);
        }

        private static float TeaseChance(Session s)
        {
            var h = s.Her;
            if (h.isGirlfriend) return 0.85f;
            return Mathf.Clamp(0.15f + h.lewdness / 150f + G.Sens / 250f + s.Interest * 0.04f + Bonus(h), 0.05f, 0.9f);
        }

        private static void Tease()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("tease");
            var ok = Random.value < TeaseChance(s);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "tease"));
            b.Her_(RomanceSay(h, "tease", ok ? "yes" : "no"));
            Play(b, ok ? 1 : -1, ok ? "♥ " + GirlInfo.FirstName(h) + " is flustered." : null, null, ok ? 6 : 0);
        }

        private static float WhisperChance(Session s)
        {
            var h = s.Her;
            if (h.isGirlfriend) return 0.9f;
            return Mathf.Clamp(0.3f + h.lewdness / 200f + G.Sens / 300f + Bonus(h), 0.05f, 0.9f);
        }

        private static void Whisper()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("whisper");
            var ok = Random.value < WhisperChance(s);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "whisper", GfLvl(h)));
            b.Her_(RomanceSay(h, "whisper", ok ? "yes" : "no", GfLvl(h)));
            Play(b, ok ? 1 : -2, ok ? "♥ " + GirlInfo.FirstName(h) + " is breathing faster." : "She didn't like that. (-trust)", () =>
            {
                if (!ok) { Social.AddTrust(h, -2); return; }
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 1);
            }, ok ? 10 : 0);
        }

        private static void Memories()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("memories");
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "memories", MemLvl(h)));
            b.Her_(RomanceSay(h, "memories", "yes", MemLvl(h)));
            Play(b, 1, "♥ " + GirlInfo.FirstName(h) + " remembers it too.", () =>
            {
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.AddTalk(h, 1);
            }, 8);
        }

        /// <summary>The director's chance that she says yes to H now (Consent decides it, 2026-10-26).</summary>
        private static float SneakChance(Session s)
        {
            string why;
            return Consent.Chance(s.Her, Ask.H, out why);
        }

        /// <summary>She agrees: straight into the game's own H invitation when the game allows it here, else the H bar rises.</summary>
        private static void Sneak()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("sneak");
            var ok = Consent.Decide(h, Ask.H);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceMe(h, "sneak"));
            b.Her_(RomanceSay(h, "sneak", ok ? "yes" : "no"));
            var invite = ok && GameAllows(3);
            Play(b, ok ? 1 : -1, ok && !invite ? "♥ She's in the mood. Somewhere private, the game will let you invite her to H." : null, null, ok ? 12 : 0,
                invite ? (Action)(() => Game(13)) : null);
        }

        // ---------------- clearing the air ----------------

        private static bool NeedsClearing(SaveData.Heroine h, Session s)
        {
            return h.isAnger || (Social.Grudge(h) != null && !s.Did.Contains("cleared")) || (s.Did.Contains("jealous") && !s.Did.Contains("plans"));
        }

        private static void ClearPage()
        {
            var s = Sess;
            var h = s.Her;
            var g = Social.Grudge(h);
            if (g != null && !s.Did.Contains("cleared"))
            {
                Add("Explain yourself <color=#6ab4ff>[INT]</color>", () => Explain(g));
                Add("Apologize", () => Apologize(g));
            }
            if (s.Did.Contains("jealous") && !s.Did.Contains("plans")) Add("Make plans with her", MakePlans);
            if (h.isAnger) Add("Apologize for making her angry (game)", () => Game(10));
            Add("Back", Back);
        }

        private static void Explain(Incident g)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            // se ela já estiver irritada"): mood, anger, and how much she has heard about you lately.
            var chance = 0.25f + G.Int / 150f + S.Reputation / 300f + Social.Trust(h) / 300f;
            var mood = MoodOf(h);
            chance += mood == CampusLife.Mood.Cheerful ? 0.12f : mood == CampusLife.Mood.Flirty ? 0.06f : mood == CampusLife.Mood.Upset ? -0.2f : mood == CampusLife.Mood.Stressed ? -0.12f : 0f;
            chance -= h.isAnger ? 0.2f : h.anger / 400f;
            chance -= Mathf.Min(0.2f, 0.05f * Social.Grudges(h).Count);
            var ok = Random.value < chance;
            s.Did.Add("cleared");
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerExplain(g.Kind));
            b.Her_(Everyday(h, ok ? "explain.ok" : "explain.fail"));
            Play(b, ok ? 1 : -1, ok ? "Misunderstanding cleared. (+trust)" : "She didn't buy it.", () =>
            {
                if (!ok) return;
                Social.Cleared(h, g);
                Social.AddTrust(h, -Social.Effect(h, g));
            });
        }

        private static void Apologize(Incident g)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            s.Did.Add("cleared");
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerApologize());
            b.Her_(Everyday(h, "apology"));
            Play(b, 0, "Apology accepted.", () =>
            {
                Social.Cleared(h, g);
                Social.AddTrust(h, -Social.Effect(h, g) / 2);
            });
        }

        private static void MakePlans()
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            s.Did.Add("plans");
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerMakePlans(""));
            b.Her_(Everyday(h, "plans"));
            Play(b, 1, "Plans made: have lunch or study with her soon.", () =>
            {
                Social.AddTrust(h, 2);
                var r = Board.Make(RequestKind.Lunch, h, "Lunch with " + GirlInfo.FirstName(h), "You promised " + GirlInfo.FirstName(h) + " you'd spend more time together. Invite her to lunch.", 8, PeriodMask.Lunch);
                r.DeadlineDay = S.Day + 2;
                r.LearnedFrom = "her";
                Board.Add(r);
            });
        }

        // ---------------- the game's own commands ----------------

        /// <summary>Runs one of the game's own conversation commands (its scenes, checks, results and talk time).</summary>
        private static void Game(int command)
        {
            if (_scene == null || CommandFunc == null) return;
            var s = Sess;
            if (command >= 11 || command == -1) Finish(true); // invitations and goodbye end our part of the talk
            Clear();
            _sub = null;
            try { CommandFunc.Invoke(_scene, new object[] { command }); }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("Game talk command " + command + ": " + e.Message); }
            if (command == 10) _reopenAt = Time.realtimeSinceStartup + 0.5f; // the apology: see Watch
            if (s != null && command >= 0) s.Turns++;
        }

        // ================= turns and results =================

        /// <summary>
        /// Favor for one turn, from how it went (delta -1..2). Balanced against the game: a right answer in its
        /// small talk gives +5 to +10 after its corrections, so 4 of them (the whole blue bar) about +40; a perfect
        /// conversation of ours (8 turns of half a point) gives 8 x 5 = 40 too, before the INT bonus of Campus.Apply.
        /// </summary>
        private static int TurnFavor(int delta)
        {
            return delta >= 2 ? 5 : delta == 1 ? 3 : delta == 0 ? 1 : -3;
        }

        /// <summary>Plays a turn: the scene, then favor and H right away (the game's bars move), the talk time, and the page again.</summary>
        /// <param name="next">Instead of our page again: the next thing to run (a command of the game's).</param>
        private static void Play(SceneBuilder b, int delta, string toast, Action done = null, int lewd = 0, Action next = null)
        {
            PlayTurn(b, _ => new Turn { Delta = delta, Toast = toast, Done = done, Lewd = lewd, Next = next });
        }

        /// <summary>Plays a turn whose result depends on the choice made in it (a subject's "how do you go on").</summary>
        private static void PlayTurn(SceneBuilder b, Func<string, Turn> result)
        {
            var s = Sess;
            Hide();
            Scenes.PlayInTalk(b, choice =>
            {
                if (s == null) return;
                Turn t;
                try { t = result(choice ?? "") ?? new Turn(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); t = new Turn(); }
                var delta = t.Delta;
                var toast = t.Toast;
                var done = t.Done;
                var next = t.Next;
                s.Interest = Mathf.Clamp(s.Interest + delta, 0, MaxInterest);
                s.Turns++;
                var applied = Campus.Apply(new Rewards { Favor = TurnFavor(delta), Lewd = t.Lewd }, s.Her);
                s.FavorGained += applied.Favor;
                s.LewdGained += applied.Lewd;
                if (done != null) { try { done(); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } }
                if (!string.IsNullOrEmpty(toast)) Toasts.Show(toast);
                if (next != null) { next(); return; }
                var empty = SpendTalkTime(s.Her);
                if (FriendsNow(s.Her)) return;
                if (empty && !Board.HasOpenNow(s.Her))
                {
                    // The blue bar is empty: the game's own goodbye, like after its own topics. With a request open the
                    // talk stays, with the free options only; the page says goodbye when none is left.
                    Game(-1);
                    return;
                }
                _sub = null;
                Reopen();
            }, false);
        }

        /// <summary>Our turns use the game's talk time (the blue bar): Cfg "Talk time per turn" of a point each.
        /// Returns true when it ran out.</summary>
        private static bool SpendTalkTime(SaveData.Heroine h)
        {
            if (h == null) return false;
            if (_talkCostDay != S.Day) { TalkCost.Clear(); _talkCostDay = S.Day; }
            var key = GirlInfo.Key(h);
            float acc;
            TalkCost.TryGetValue(key, out acc);
            acc += Cfg.TurnTalkTime.Value;
            var whole = Mathf.FloorToInt(acc + 0.001f);
            TalkCost[key] = acc - whole;
            if (whole > 0)
            {
                try { Singleton<Communication>.Instance.DecreaseTalkTime(h, whole); }
                catch { h.talkTime = Mathf.Max(0, h.talkTime - whole); }
            }
            return h.talkTime <= 0;
        }

        /// <summary>Uses some of her talk time (the blue bar) from outside a conversation turn (forms, scenes).</summary>
        public static void Spend(SaveData.Heroine h, float points)
        {
            if (h == null || points <= 0f) return;
            if (_talkCostDay != S.Day) { TalkCost.Clear(); _talkCostDay = S.Day; }
            var key = GirlInfo.Key(h);
            float acc;
            TalkCost.TryGetValue(key, out acc);
            acc += points;
            var whole = Mathf.FloorToInt(acc + 0.001f);
            TalkCost[key] = acc - whole;
            if (whole <= 0) return;
            try { Singleton<Communication>.Instance.DecreaseTalkTime(h, whole); }
            catch { h.talkTime = Mathf.Max(0, h.talkTime - whole); }
        }

        private static bool _confessNext, _endNext;

        /// <summary>After the scene now playing: the game's own confession (she asked you to say it properly).</summary>
        public static void QueueConfession() { _confessNext = true; }

        /// <summary>The talk ends with the game's own goodbye (a girl who does not want to talk).</summary>
        public static void EndTalk() { _endNext = true; }

        /// <summary>After an opening scene: the game's confession or goodbye if one was asked for, else the menu.</summary>
        private static void AfterScene()
        {
            if (_endNext) { _endNext = false; Game(-1); return; }
            if (_confessNext)
            {
                _confessNext = false;
                if (GameAllows(1)) { Game(11); return; }
                Toasts.Show("She is waiting for you to say it properly (heart button > Us two...).");
            }
            var s = _s;
            if (s != null && s.Her != null && s.Her.talkTime <= 0 && !Board.HasOpenNow(s.Her)) { Game(-1); return; }
            if (s != null && FriendsNow(s.Her)) return;
            _sub = null;
            if (TrioAfter()) return;
            Reopen();
        }

        /// <summary>A gift was given from our menu: back to the conversation.</summary>
        public static void AfterGift()
        {
            if (_s == null) return;
            _s.Interest = Mathf.Clamp(_s.Interest + 1, 0, MaxInterest);
            _s.Turns++;
            _sub = null;
            if (FriendsNow(_s.Her)) return;
            Reopen();
        }

        private static readonly MethodInfo StartAdvList = AccessTools.Method(typeof(TalkScene), "StartADV", new[] { typeof(List<ADV.Program.Transfer>) });
        private static readonly MethodInfo TalkEndMethod = AccessTools.Method(typeof(TalkScene), "TalkEnd");

        /// <summary>
        /// Her favor reached 100 and she is not a friend yet: the game's own "we're friends now" ending plays at once
        /// (end-of-talk ADV 10, which makes her a friend), the same check the game makes after each of its own topics.
        /// Before, the bar sat full and the friend scene only came after closing the talk and opening it again.
        /// </summary>
        private static bool FriendsNow(SaveData.Heroine h)
        {
            if (_scene == null || h == null || h != _scene.targetHeroine || h.relation != 0 || h.favor < 100 || h.isGirlfriend || StartAdvList == null || TalkEndMethod == null) return false;
            try
            {
                var tr = Traverse.Create(_scene);
                var info = tr.Property("info").GetValue<ActionGame.Communication.Info>();
                if (info == null) return false;
                int no;
                var list = info.GetEndConversationADV(tr.Field("oldAnger").GetValue<bool>(), false, out no);
                if (list == null || no != 10) return false;
                Finish(true);
                Clear();
                _sub = null;
                tr.Field("endADVNo").SetValue(no);
                var canvas = tr.Field("canvas").GetValue<Canvas>();
                if (canvas != null) canvas.enabled = false;
                StartAdvList.Invoke(_scene, new object[] { list });
                _scene.StartCoroutine((System.Collections.IEnumerator)TalkEndMethod.Invoke(_scene, null));
                CampusLifePlugin.Log.LogInfo(GirlInfo.FullName(h) + ": favor full, the game's friend ending plays");
                return true;
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogWarning("Friend ending: " + e.Message);
                return false;
            }
        }

        /// <summary>Applies the end of the conversation once: trust, attention, devotion and a summary.</summary>
        private static void Finish(bool show)
        {
            var s = _s;
            if (s == null || s.Finished || s.Turns == 0) return;
            s.Finished = true;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            gs.ChatDay = S.Day;
            gs.LastTopics = string.Join(",", s.Used.Select(t => t.ToString()).ToArray());
            var trust = Unified ? 0 : s.Interest >= 4 ? 2 : s.Interest <= 1 ? -1 : 0;   // the single list pays trust per beat
            Social.AddTrust(h, trust);
            gs.Attention = Mathf.Clamp(gs.Attention + 4 + s.Interest, 0, 100);
            if (s.Interest >= 5)
            {
                if (Cfg.DevotionEnabled.Value) Devotion.AddTalk(h, 3);
                Social.Add(Social.Kinds.GoodTalk, h);
            }
            S.Stat("chats");
            if (show && (s.FavorGained != 0 || s.LewdGained != 0 || trust != 0))
                Toasts.Show("Talk with " + GirlInfo.FirstName(h) + ": " + (s.FavorGained >= 0 ? "+" : "") + s.FavorGained + " favor" +
                            (s.LewdGained != 0 ? ", +" + s.LewdGained + " H" : "") + (trust != 0 ? ", " + (trust > 0 ? "+" : "") + trust + " trust" : "") + ".");
        }

        // ================= buttons =================

        private static void Clear()
        {
            foreach (var go in Created) if (go != null) Object.Destroy(go);
            Created.Clear();
        }

        private static readonly PropertyInfo OverrideColorTags = typeof(TextMeshProUGUI).GetProperty("overrideColorTags");

        private static void Add(string label, Action onClick)
        {
            if (_template == null || _list == null) return;
            var go = Object.Instantiate(_template.gameObject, _list, false);
            go.name = "cl_talk_option";
            go.SetActive(true);
            var btn = go.GetComponent<Button>();
            // our card instead of the game's white button, framed in the open category's color (user, 2026-10-26)
            try { ConvArt.Style(btn, ConvArt.Card(OptionKind()), true); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Option card: " + e.Message); }
            btn.onClick = new Button.ButtonClickedEvent();
            btn.interactable = onClick != null;
            if (onClick != null)
                btn.onClick.AddListener(() =>
                {
                    if (Busy("option '" + label + "'")) return;
                    if (_trio != null) _trio.QuietSince = Time.realtimeSinceStartup;
                    try { Sfx.Select(); onClick(); }
                    catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                });
            var txt = go.GetComponentInChildren<TextMeshProUGUI>(true);
            if (txt != null)
            {
                txt.richText = true;
                // The game's button text ignores <color> tags (labels came out all black); not every TMP version has the switch.
                if (OverrideColorTags != null) try { OverrideColorTags.SetValue(txt, false, null); } catch { }
                txt.enableAutoSizing = true;
                txt.fontSizeMin = 12f;   // 14 cut "Whisper something sugges..." on the card
                txt.fontSizeMax = txt.fontSize;
                // the letters' face in the button's color, with a black outline (user, 2026-10-26)
                ConvArt.ColorFace(txt, onClick == null ? GameUi.Dim : OptionColor());
                txt.text = Gender.Fix(label, null, TalkingTo);
            }
            Created.Add(go);
        }

        // ================= topic lines (ported from the old Chat) =================

        private static string TopicName(Topic t)
        {
            switch (t)
            {
                case Topic.Classes: return "Classes";
                case Topic.Club: return "Her club";
                case Topic.Food: return "Food";
                case Topic.Sports: return "Sports";
                case Topic.Hobbies: return "Hobbies";
                case Topic.Rumors: return "Campus rumors";
                case Topic.Weekend: return "Weekend plans";
                case Topic.Love: return "<color=#ff8ad8>Love life</color>";
                default: return "Ask about her";
            }
        }

        private static string Opener(Topic t)
        {
            var ctx = new Dictionary<string, string>();
            ctx["topic"] = t.ToString().ToLowerInvariant();
            DlgRule rule;
            var girl = Sess != null && Sess.Her != null ? GirlInfo.Key(Sess.Her) : null;
            return Bank.Pick("me.opener", ctx, girl, out rule) ?? "So... how have you been?";
        }
    }
}
