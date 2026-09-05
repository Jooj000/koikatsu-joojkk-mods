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
    internal static class Conversation
    {
        private const int MaxInterest = 5;
        private static CampusState S { get { return Campus.S; } }

        /// <summary>The three menus of the conversation, one per button of the game's talk screen.</summary>
        internal enum Page { Talk, Romance, Actions }

        private sealed class Session
        {
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
            if (ts == null) { if (_scene != null) Close(); return; }
            if (ts != _scene || (!_decided && ts.targetHeroine != null)) Attach(ts);
            if (_root == null) return;
            try { if (ForeignListeners()) HookButtons(); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Conversation rehook: " + e.Message); }
            // Safety net: whatever opened the list, it shows our page (never the game's buttons).
            var shown = _root.activeInHierarchy;
            if (shown && !IsOpen && !GiftMenu.IsOpen && !Scenes.Running) Build();
            else if (!shown && IsOpen && !GiftMenu.IsOpen) Clear();
            if (_eventRoot != null && _eventRoot.activeSelf) _eventRoot.SetActive(false);
            // After the game's own apology the game goes back to its selection with the list closed: open our page again
            // (its topics reopen the list by themselves, so the safety net above covers them).
            if (_reopenAt > 0f && !shown && !IsOpen && !Scenes.Running && !ADV.Program.isADVProcessing && CanvasOn(ts))
            {
                if (Time.realtimeSinceStartup >= _reopenAt) { _reopenAt = 0f; Reopen(); }
            }
            else if (_reopenAt > 0f && Time.realtimeSinceStartup - _reopenAt > 60f) _reopenAt = 0f;
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
            if (!Cfg.NewConversation.Value || h == null || !GirlInfo.IsRegular(h) || Duo.TalkHer == h) return; // story girls and teachers: as the game made them
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
                _listen.gameObject.SetActive(true);
                _page = Page.Talk;
                _sub = null;
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
            _talk.onClick.AddListener(() => Toggle(Page.Talk));
            _event.onClick = new Button.ButtonClickedEvent();
            _event.onClick.AddListener(() => Toggle(Page.Romance));
            _listen.onClick = new Button.ButtonClickedEvent();
            _listen.onClick.AddListener(() => Toggle(Page.Actions));
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
            Finish(true);
            Clear();
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
            _decided = false;
            _root = null;
            _eventRoot = null;
            _template = null;
            _list = null;
            _sub = null;
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
                var close = RootOpen && _page == p && _sub == null;
                _page = p;
                _sub = null;
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
                    var minds = ConvoLines.MindsCheating(h);
                    b.Her_(ConvoLines.Cheating(tone, aboutName, minds));
                    if (minds) s.Interest = Math.Max(0, s.Interest - 1);
                }
                else if (inc.Kind == Social.Kinds.HerBoysRoom)
                {
                    b.Her_(ConvoLines.Incident(tone, inc.Kind, aboutName, true, false));
                    b.Choice("Promise to keep it secret.", "keep", "Tease her about it.", "tease");
                    b.Branch("keep"); b.Her_(ConvoLines.KeepSecret(tone, true)); b.End();
                    b.Branch("tease"); b.Her_(ConvoLines.KeepSecret(tone, false)); b.End();
                    after = c =>
                    {
                        if (c == "keep") { Social.AddTrust(h, 8); G.AddFavor(h, 3, 0); Toasts.Show(GirlInfo.FirstName(h) + " trusts you with her secret. (+trust)"); }
                        else if (c == "tease") { Social.AddTrust(h, -5); s.Interest = Math.Max(0, s.Interest - 1); }
                    };
                }
                else
                {
                    var friend = about != null && about != h && Social.Bond(h, about) >= 35;
                    b.Her_(ConvoLines.Incident(tone, inc.Kind, aboutName, about == h, friend));
                }
            }
            else if (jealous != null)
            {
                gs.JealousDay = S.Day;
                b.Her_(ConvoLines.Jealous(tone, GirlInfo.FirstName(jealous)));
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
            if (s == null || _list == null) return;
            if (Opening(s)) { Hide(); return; }
            Clear();
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
            var extra = 0f;
            foreach (var x in items)
            {
                x.Txt.enableAutoSizing = false;
                x.Txt.fontSize = size;
                x.Txt.enableWordWrapping = false;
                var need = x.Txt.GetPreferredValues(x.Txt.text).x + 16f;
                var have = ((RectTransform)x.Txt.transform).rect.width;
                if (have > 1f) extra = Mathf.Max(extra, need - have);
            }
            // Widen a little (all buttons alike); a label still too long gets a smaller size and, at worst, an ellipsis,
            // so its text never runs out of the button over the game's buttons beside the list.
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
                var have = ((RectTransform)x.Txt.transform).rect.width + extra - 16f;
                var need = x.Txt.GetPreferredValues(x.Txt.text).x;
                if (have > 1f && need > have) x.Txt.fontSize = Mathf.Max(size * 0.72f, Mathf.Floor(size * have / need));
                x.Txt.overflowMode = TextOverflowModes.Ellipsis;
            }
            KeepClear();
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
            if (Mathf.Abs(shift) < 0.5f) return;
            var parent = lrt.parent as RectTransform;
            if (parent == null) return;
            Vector2 a, c;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, Vector2.zero, cam, out a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(shift, 0f), cam, out c);
            lrt.anchoredPosition = _listHome + new Vector2(c.x - a.x, 0f);
            if (Mathf.Abs(shift - _lastShift) > 0.5f) CampusLifePlugin.Log.LogInfo("Conversation list moved " + shift.ToString("0") + " px clear of the game's buttons");
            _lastShift = shift;
        }

        private static float _lastShift;

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
            if (NeedsClearing(h, s)) Add("<color=#ff9b6a>Clear the air...</color>", () => ShowSub(ClearPage));
            // Four entries at most besides the two that only show when needed:
            // listening lives under "Ask her...", the game's own talk under "Chat about...".
            Add("Chat about...", () => ShowSub(TopicPage));
            Add("Ask her...", () => ShowSub(AskPage));
            Add("Compliment her...", () => ShowSub(ComplimentPage));
            Add("Joke around" + JokeOdds(s), Joke);
        }

        // ---------------- heart: romance and H ----------------

        private static void RomancePage()
        {
            var s = Sess;
            var h = s.Her;
            Group("<color=#ff8ad8>Sweet talk...</color>", () =>
            {
                var l = new List<KeyValuePair<string, Action>>();
                if (CanFlirt(h)) l.Add(Opt("<color=#ff8ad8>Be sweet to her</color>" + FlirtOdds(s), Flirt));
                if (CanFlirt(h) && !s.Did.Contains("hand")) l.Add(Opt("<color=#ff8ad8>Hold her hand</color>", HoldHand));
                if (!s.Did.Contains("feelings")) l.Add(Opt("What am I to you?", AskFeelings));
                l.Add(Opt("Love talk (game)", () => Game(1)));
                return l;
            }, 2);
            Group("<color=#ff8ad8>Flirt...</color>", () =>
            {
                var l = new List<KeyValuePair<string, Action>>();
                if (CanFlirt(h) && !s.Did.Contains("tease") && (h.isGirlfriend || h.lewdness >= 30 || MoodOf(h) == CampusLife.Mood.Flirty))
                    l.Add(Opt("<color=#ff8ad8>Tease her</color>" + Odds(TeaseChance(s)), Tease));
                if (Cfg.LewdRequests.Value && !s.Did.Contains("whisper") && (h.isGirlfriend || h.lewdness >= 60))
                    l.Add(Opt("<color=#ff8ad8>Whisper to her</color>" + Odds(WhisperChance(s)), Whisper));
                if (Cfg.LewdRequests.Value && !s.Did.Contains("memories") && h.hCount > 0) l.Add(Opt("<color=#ff8ad8>Talk about last time</color>", Memories));
                if (Cfg.LewdRequests.Value && !s.Did.Contains("sneak") && (h.isGirlfriend || h.lewdness >= 50))
                    l.Add(Opt("<color=#ff8ad8>Sneak off together</color>" + Odds(SneakChance(s)), Sneak));
                l.Add(Opt("Lewd talk (game)", () => Game(2)));
                return l;
            }, 2);
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
                return l;
            });
            if (GameAllows(11)) Add("Come with me", () => Game(21));
            if (Cfg.GiftsEnabled.Value) Add("Give a gift", () => { var t = Created.Select(c => c.GetComponent<Button>()).FirstOrDefault(b => b != null); if (t == null) return; GiftMenu.Open(t, h); });
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
            var all = ((Topic[])Enum.GetValues(typeof(Topic))).Where(t => t != Topic.Love || s.Her.relation >= 1 || s.Her.favor >= 50).ToList();
            var read = CanRead(s.Her);
            foreach (var t in all.Where(t => !s.Used.Contains(t)).OrderBy(_ => Random.value).Take(4).ToList())
            {
                var topic = t;
                var label = TopicName(t) + (t == HotTopic ? " <color=#9AD0FF>(hot)</color>" : "");
                if (read && Score(s.Her, t, s) >= 3) label = "<color=#FFD27A>★</color> " + label;
                Add(label, () => PickTopic(topic));
            }
            Group("<color=#b0b0b0>The game's talk...</color>", () => new List<KeyValuePair<string, Action>>
            {
                Opt("<color=#b0b0b0>Small talk (game)</color>", () => Game(0)),
                Opt("<color=#b0b0b0>Listen (game)</color>", () => Game(3)),
            }, 2);
            Add("Back", Back);
        }

        private static void PickTopic(Topic t)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var sc = Score(h, t, s);
            s.Used.Add(t);
            var delta = sc >= 4 ? 2 : sc >= 2 ? 1 : sc >= 0 ? 0 : -1;
            var b = new SceneBuilder(h, true);
            b.Me(Opener(t));
            if (delta > 0) b.Her_(t == Topic.Love ? ConvoLines.LoveTalk(tone, Lines.Closeness(h)) : Good(t, tone));
            else if (delta == 0) b.Her_(Lines.T(tone, "Oh, um... that's nice, I guess.", "Huh, yeah, sure!", "I see.", "Mm-hm. Fascinating. Kind of."));
            else b.Her_(Lines.T(tone, "Um... could we talk about something else?", "Ehh, boring! Next topic!", "Not interested.", "Wow. You really know how to kill a conversation."));
            Play(b, delta, delta >= 2 ? "♪♪ She loved that topic." : delta == 1 ? "♪ She liked that topic." : null, null, t == Topic.Love && delta > 0 ? 2 : 0);
        }

        // ---------------- let her talk ----------------

        private static void LetHerTalk()
        {
            var s = Sess;
            var h = s.Her;
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerListens());
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
                b.Her_(ConvoLines.Vent(tone, mood));
                b.Her_(ConvoLines.VentThanks(tone));
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
                b.Her_(ConvoLines.GossipLine(tone, gossip.Kind, about != null ? GirlInfo.FirstName(about) : ""));
                Social.Mentioned(h, gossip);
            }
            else if (!s.Did.Contains("anecdote"))
            {
                s.Did.Add("anecdote");
                b.Her_(ConvoLines.Anecdote(tone));
                delta = GirlInfo.ToneOf(h) == Tone.Cool ? 0 : 1;
            }
            else b.Her_(ConvoLines.NothingNew(tone));
            Play(b, delta, toast, done);
        }

        // ---------------- ask about ----------------

        private static void AskPage()
        {
            var s = Sess;
            Add("Just listen", LetHerTalk);
            Add("How's your day?", AskDay);
            foreach (var f in Social.FriendsOf(s.Her).Take(2).ToList())
            {
                var friend = f;
                Add("About " + GirlInfo.FirstName(f), () => AskFriend(friend));
            }
            var rival = Social.Students.Where(o => o != s.Her && Social.Bond(s.Her, o) <= -30).OrderBy(_ => Random.value).FirstOrDefault();
            if (rival != null && Random.value < 0.5f && Created.Count < 4) Add("About " + GirlInfo.FirstName(rival), () => AskFriend(rival));
            Add("About the school", AskSchool);
            Add("Back", Back);
        }

        private static void AskDay()
        {
            var s = Sess;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            var b = new SceneBuilder(h, true);
            b.Me(Lines.AskHowAreYou(close));
            var already = gs.MoodKnownDay == S.Day;
            b.Her_(already ? Lines.AlreadyAsked(tone, close) : Lines.MoodLine(tone, gs.Mood, close));
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

        private static void AskFriend(SaveData.Heroine f)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var name = GirlInfo.FirstName(f);
            var fs = S.Girl(GirlInfo.Key(f));
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.AskFriend(name));
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
            b.Her_(ConvoLines.Reputation(tone, S.Reputation));
            var gossip = Social.Gossip(h);
            if (gossip != null)
            {
                var about = Social.Get(gossip.Girl);
                b.Her_(ConvoLines.GossipLine(tone, gossip.Kind, about != null ? GirlInfo.FirstName(about) : ""));
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

        private static void Joke()
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var chance = JokeChance(s);
            s.Did.Add("joke");
            var roll = Random.value;
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerJoke());
            if (roll < chance) { b.Her_(ConvoLines.JokeGood(tone)); Play(b, roll < chance * 0.4f ? 2 : 1, "♪ She laughed."); return; }
            var hurt = roll > 0.92f && (tone == Tone.Gentle || MoodOf(h) == CampusLife.Mood.Upset);
            b.Her_(hurt ? ConvoLines.JokeHurt(tone) : ConvoLines.JokeBad(tone));
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

        private static void Compliment(string kind)
        {
            var s = Sess;
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerCompliment(kind, GirlInfo.ClubName(h)));
            if (gs.ComplimentDay >= S.Day - 1) { b.Her_(ConvoLines.ComplimentRepeat(tone)); Play(b, -1, null); return; }
            var stat = kind == "smart" ? G.Int : G.Sens;
            var chance = 0.35f + stat / 200f + (kind == "club" ? 0.15f : 0f) + (kind == "looks" ? (h.relation >= 1 ? 0.1f : -0.15f) : 0f);
            if (tone == Tone.Cool) chance -= 0.1f;
            var ok = Random.value < chance;
            b.Her_(ok ? ConvoLines.ComplimentGood(tone, kind) : ConvoLines.ComplimentFlat(tone));
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
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerSweet(close));
            if (s.Did.Contains("flirt")) { b.Her_(Lines.T(tone, "Y-you already said something like that...", "Okay, okay, I get it! Hehe!", "Once is enough.", "Laying it on thick, aren't you?")); Play(b, 0, null); return; }
            s.Did.Add("flirt");
            var ok = Random.value < FlirtChance(s);
            b.Her_(ok ? ConvoLines.FlirtGood(tone, close) : ConvoLines.FlirtTooSoon(tone));
            var witnesses = Social.GirlsOnMyMap().Where(o => o != h).ToList();
            Play(b, ok ? 2 : -1, ok ? "♥ " + GirlInfo.FirstName(h) + " is blushing." : null, ok ? (Action)(() =>
            {
                Social.AddTrust(h, 2);
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.Add(h, 2);
                Social.Add(Social.Kinds.Flirted, h, witnesses);
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
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            s.Did.Add("hand");
            var chance = h.isGirlfriend ? 1f : Mathf.Clamp(0.25f + h.favor / 150f + G.Sens / 400f + Bonus(h), 0.05f, 0.95f);
            var ok = Random.value < chance;
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerHand(close));
            b.Her_(ok ? RomanceLines.HandYes(tone, close) : RomanceLines.HandNo(tone));
            var witnesses = Social.GirlsOnMyMap().Where(o => o != h).ToList();
            Play(b, ok ? 2 : -1, ok ? "♥ " + GirlInfo.FirstName(h) + " holds your hand." : null, ok ? (Action)(() =>
            {
                Social.AddTrust(h, 2);
                if (witnesses.Count > 0) Social.Add(Social.Kinds.Flirted, h, witnesses);
            }) : null, ok ? 2 : 0);
        }

        private static void AskFeelings()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("feelings");
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerAskFeelings());
            b.Her_(RomanceLines.Feelings(GirlInfo.ToneOf(h), h));
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
            var tone = GirlInfo.ToneOf(h);
            var close = Lines.Closeness(h);
            s.Did.Add("tease");
            var ok = Random.value < TeaseChance(s);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerTease(close));
            b.Her_(ok ? RomanceLines.TeaseGood(tone, close) : RomanceLines.TeaseBad(tone));
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
            var tone = GirlInfo.ToneOf(h);
            s.Did.Add("whisper");
            var ok = Random.value < WhisperChance(s);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerWhisper(h.isGirlfriend));
            b.Her_(ok ? RomanceLines.WhisperGood(tone, h.isGirlfriend) : RomanceLines.WhisperBad(tone));
            Play(b, ok ? 1 : -2, ok ? "♥ " + GirlInfo.FirstName(h) + " is breathing faster." : "She didn't like that. (-trust)", () =>
            {
                if (!ok) { Social.AddTrust(h, -2); return; }
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.Add(h, 1);
            }, ok ? 10 : 0);
        }

        private static void Memories()
        {
            var s = Sess;
            var h = s.Her;
            s.Did.Add("memories");
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerMemories(h.hCount));
            b.Her_(RomanceLines.Memories(GirlInfo.ToneOf(h), h.hCount));
            Play(b, 1, "♥ " + GirlInfo.FirstName(h) + " remembers it too.", () =>
            {
                if (Cfg.DevotionEnabled.Value && h.isGirlfriend) Devotion.Add(h, 1);
            }, 8);
        }

        private static float SneakChance(Session s)
        {
            var h = s.Her;
            var c = (h.isGirlfriend ? 0.6f : 0.15f) + h.lewdness / 250f + G.Sens / 400f + Bonus(h);
            return Mathf.Clamp(c, 0.05f, 0.95f);
        }

        /// <summary>She agrees: straight into the game's own H invitation when the game allows it here, else the H bar rises.</summary>
        private static void Sneak()
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            s.Did.Add("sneak");
            var ok = Random.value < SneakChance(s);
            var b = new SceneBuilder(h, true);
            b.Me(RomanceLines.PlayerSneak());
            b.Her_(ok ? RomanceLines.SneakYes(tone) : RomanceLines.SneakNo(tone));
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
            var chance = 0.25f + G.Int / 150f + S.Reputation / 300f + Social.Trust(h) / 300f;
            var ok = Random.value < chance;
            s.Did.Add("cleared");
            var b = new SceneBuilder(h, true);
            b.Me(ConvoLines.PlayerExplain(g.Kind));
            b.Her_(ok ? ConvoLines.ExplainOk(tone) : ConvoLines.ExplainFail(tone));
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
            b.Her_(ConvoLines.ApologyAccepted(tone));
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
            b.Her_(ConvoLines.PlansAccepted(tone));
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
            var s = Sess;
            Hide();
            Scenes.PlayInTalk(b, _ =>
            {
                if (s == null) return;
                s.Interest = Mathf.Clamp(s.Interest + delta, 0, MaxInterest);
                s.Turns++;
                var applied = Campus.Apply(new Rewards { Favor = TurnFavor(delta), Lewd = lewd }, s.Her);
                s.FavorGained += applied.Favor;
                s.LewdGained += applied.Lewd;
                if (done != null) { try { done(); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } }
                if (!string.IsNullOrEmpty(toast)) Toasts.Show(toast);
                if (next != null) { next(); return; }
                var empty = SpendTalkTime(s.Her);
                if (FriendsNow(s.Her)) return;
                if (empty)
                {
                    // The blue bar is empty: the game's own goodbye, like after its own topics.
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
            if (_scene == null || h == null || h.relation != 0 || h.favor < 100 || h.isGirlfriend || StartAdvList == null || TalkEndMethod == null) return false;
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
            var trust = s.Interest >= 4 ? 2 : s.Interest <= 1 ? -1 : 0;
            Social.AddTrust(h, trust);
            gs.Attention = Mathf.Clamp(gs.Attention + 4 + s.Interest, 0, 100);
            if (s.Interest >= 5)
            {
                if (Cfg.DevotionEnabled.Value) Devotion.Add(h, 3);
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
            btn.onClick = new Button.ButtonClickedEvent();
            btn.interactable = onClick != null;
            if (onClick != null)
                btn.onClick.AddListener(() =>
                {
                    if (Scenes.Running) return;
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
                txt.fontSizeMin = 14f;
                txt.fontSizeMax = txt.fontSize;
                txt.text = label;
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
                case Topic.Rumors: return "School rumors";
                case Topic.Weekend: return "Weekend plans";
                case Topic.Love: return "<color=#ff8ad8>Love life</color>";
                default: return "Ask about her";
            }
        }

        private static string Opener(Topic t)
        {
            switch (t)
            {
                case Topic.Classes: return Lines.Pick("How are classes going for you?", "Did you get today's homework?");
                case Topic.Club: return Lines.Pick("How's your club doing lately?", "Anything new at your club?");
                case Topic.Food: return Lines.Pick("Had anything good to eat today?", "What's your favorite thing at the cafeteria?");
                case Topic.Sports: return Lines.Pick("Do you follow any sports?", "Do you work out at all?");
                case Topic.Hobbies: return Lines.Pick("What do you do in your free time?", "Into anything fun lately?");
                case Topic.Rumors: return Lines.Pick("Heard any interesting rumors around school?", "Do you know what everyone's whispering about?");
                case Topic.Weekend: return Lines.Pick("Got any plans for the weekend?", "Did you do anything fun last weekend?");
                case Topic.Love: return Lines.Pick("So... is there anyone you like?", "Have you ever had a crush on someone?", "What's your idea of a perfect date?");
                default: return Lines.Pick("Tell me a bit about yourself.", "What's been on your mind lately?");
            }
        }

        private static string Good(Topic t, Tone tone)
        {
            switch (t)
            {
                case Topic.Classes:
                    return Lines.T(tone, "Classes are going well! I finally understood today's math lesson.", "Classes? Ugh. But okay, history was actually kinda cool today!",
                        "Fine. I like it when a teacher actually explains things properly.", "I aced the quiz. Go on, be impressed.");
                case Topic.Club:
                    return Lines.T(tone, "The club is so much fun lately. Everyone has been really kind to me.", "The club's on fire! We're practicing like crazy!",
                        "We've been productive. I like that.", "My club? Of course it's going great. I'm in it.");
                case Topic.Food:
                    return Lines.T(tone, "Oh, the cafeteria curry! I could eat it every day.", "Food? My favorite subject! The yakisoba bread is the best!",
                        "The set lunch is decent. I have opinions about the miso soup.", "Food talk? Now you're speaking my language.");
                case Topic.Sports:
                    return Lines.T(tone, "I'm not very athletic, but I love cheering at games with friends.", "Sports! Yes! I ran my best time this morning!",
                        "I swim on weekends. It clears my head.", "Sports? Bet I could beat you at anything. Want to find out?");
                case Topic.Hobbies:
                    return Lines.T(tone, "I've been reading a really good novel... I can't put it down.", "I got into a new game and I'm SO hooked!",
                        "I've been sketching. I'm not showing you. Yet.", "My hobbies? Collecting interesting people. You might qualify.");
                case Topic.Rumors:
                    return Lines.T(tone, "Um, I heard the library is getting new books... does that count as a rumor?", "Oh oh oh! Did you hear about the class rep and the vending machine?!",
                        "...I heard something. Fine, I'll tell you. Quietly.", "Rumors are my specialty. Lean in.");
                case Topic.Weekend:
                    return Lines.T(tone, "I might bake something this weekend. Maybe cookies?", "Karaoke! I'm going to sing until my voice gives out!",
                        "A quiet weekend. Books, tea, maybe a walk.", "My weekend plans are secret. But they're good ones.");
                default:
                    return Lines.T(tone, "About me? Well... I've always wanted to see the sea in winter.", "Me? I want to try everything at least once! Skydiving, maybe!",
                        "...Not many people ask. I want to study abroad someday.", "About me? Okay, one secret: I'm terrible at mornings.");
            }
        }
    }
}
