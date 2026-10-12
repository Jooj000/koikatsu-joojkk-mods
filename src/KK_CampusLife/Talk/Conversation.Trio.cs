using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The conversation with two girls side by side.
    /// Duo brings them over and plays the director's reason as the opening; then the talk screen stays open with both
    /// (<see cref="TrioStage"/>) and one button picks who you talk to: A, B or Both. With A or B the five usual
    /// categories (A's are the normal conversation; B's are ours, since the game only knows A); with Both, Talk, Group
    /// actions, Settle (only while they disagree), Romance and Spicy. After each turn the other girl may react as a
    /// witness (a kiss always), they talk to each other, someone left out may walk off, and after 4 to 7 turns the moment
    /// ends by itself. Silence turns into banter between them. Lines in duo3.dlg.
    /// </summary>
    internal static partial class Conversation
    {
        internal enum TrioWho { A, B, Both }

        private sealed class Trio
        {
            public SaveData.Heroine A, B;
            public NPC NB;
            public string Moment = "hangout";
            public TrioWho Target = TrioWho.A;
            public int Beats, Max = 5, SlightA, SlightB, Idles, Kisses;
            public bool Disagree;
            public string Argue = "";
            public bool Settled;   // you settled one already: a new fight is rarer
            public Session SB;
            public float QuietSince;
            public string LastKiss;
            public Lvl? LastLvl;
            public Q LastQ;
            public SaveData.Heroine LastTo;
            public string Ending = "";
            public readonly List<string> Log = new List<string>();
        }

        private static Trio _trio;

        /// <summary>The conversation with both is on.</summary>
        internal static bool TrioOn { get { return _trio != null; } }

        /// <summary>How the last one ended: natural, leave, aleft, bleft, h3 (Duo reads it).</summary>
        internal static string TrioEnding = "";

        /// <summary>The other girl of the conversation with both, or null (witnesses: she always sees it).</summary>
        internal static SaveData.Heroine TrioOther(SaveData.Heroine h)
        {
            var t = _trio;
            if (t == null || h == null) return null;
            return h == t.A ? t.B : h == t.B ? t.A : null;
        }

        /// <summary>The panel and the markers show while the talk screen's own menu is up (not under a scene's text).</summary>
        internal static bool TrioUiVisible
        {
            get { return _trio != null && _scene != null && CanvasOn(_scene) && !ADV.Program.isADVProcessing && !GiftMenu.IsOpen; }
        }

        private static Session SessFor(SaveData.Heroine h) { return _trio != null && h == _trio.B ? _trio.SB : Sess; }

        private static bool IsTalkHeroine(SaveData.Heroine h) { return _scene != null && _scene.targetHeroine == h; }

        private static void TLog(string what)
        {
            CampusLifePlugin.Log.LogInfo("Trio: " + what);
            if (_trio != null) { _trio.Log.Add(what); if (_trio.Log.Count > 30) _trio.Log.RemoveAt(0); }
        }

        // ================= start and end =================

        /// <summary>Duo: the director's scene played; the talk goes on with both of them. False: not ours (stay as before).</summary>
        internal static bool TrioBegin(TalkScene ts, NPC nb, string moment)
        {
            try
            {
                TrioEnding = "";
                if (!Unified || ts == null || ts != _scene || _root == null || nb == null || nb.heroine == null) return false;
                var a = ts.targetHeroine;
                var b = nb.heroine;
                if (a == null || a == b) return false;
                var font = _template != null ? _template.GetComponentInChildren<TextMeshProUGUI>(true) : null;
                if (!TrioStage.Begin(ts, nb, font != null ? font.font : null, font != null ? font.fontSharedMaterial : null)) return false;
                var s = Sess;
                if (s != null) s.Did.Add("opening");
                _trio = new Trio { A = a, B = b, NB = nb, Moment = moment ?? "hangout", SB = Begin(b), QuietSince = Time.realtimeSinceStartup };
                _trio.SB.Did.Add("opening");
                _trio.Max = MaxBeats(_trio.Moment);
                _trio.Disagree = _trio.Moment == "quarrel" || _trio.Moment == "rivals";
                if (_trio.Disagree) _trio.Argue = ArgueAbout(_trio.Moment);
                SetNames(false);
                BuildTargetButton();
                TLog("begin " + GirlInfo.FirstName(a) + " + " + GirlInfo.FirstName(b) + ", moment " + _trio.Moment + ", " + _trio.Max + " turns, bond " + Social.Bond(a, b) + (_trio.Disagree ? ", they disagree about " + _trio.Argue : ""));
                _cat = "Talk";
                _sub = null;
                SetOpen(true);
                return true;
            }
            catch (Exception e)
            {
                CampusLifePlugin.Log.LogError("Trio begin: " + e);
                TrioCleanup();
                return false;
            }
        }

        private static int MaxBeats(string moment)
        {
            switch (moment)
            {
                case "quarrel": case "caught": case "confront": return 4;
                case "hangout": case "lunch": case "trisal": return 6;
                case "season": case "club": return 6;
                default: return 5;
            }
        }

        private static string ArgueAbout(string moment)
        {
            var l = moment == "rivals"
                ? new[] { "who gets to walk home with you", "who you'd rather sit with at lunch", "who knows you better" }
                : new[] { "where to eat after class", "whose turn it was to clean", "which club works harder", "the festival theme", "a borrowed notebook" };
            return l[Random.Range(0, l.Length)];
        }

        /// <summary>Closes our part (the talk scene closed, or it goes on with A alone): stage, buttons, names.</summary>
        private static void TrioCleanup()
        {
            if (_trio != null) { FinishB(); TLog("over (" + (_trio.Ending == "" ? "talk closed" : _trio.Ending) + ")"); if (TrioEnding == "") TrioEnding = _trio.Ending == "" ? "closed" : _trio.Ending; }
            _trio = null;
            TrioStage.End();
            ClearTargetButton();
            SetNames(false);
        }

        private static void FinishB()
        {
            var t = _trio;
            if (t == null || t.SB == null) return;
            var keep = _s;
            try { _s = t.SB; Finish(true); }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio finish B: " + e.Message); }
            finally { _s = keep; }
        }

        /// <summary>How the moment ends (after its last lines have played).</summary>
        private static void TrioEnd(string how)
        {
            var t = _trio;
            if (t == null) return;
            t.Ending = how;
            TrioEnding = how;
            TLog("ending: " + how);
            Achievements.Count("trio:" + how, how == "bleft" ? t.B : t.A);
            switch (how)
            {
                case "bleft":
                    // She walks off; the talk goes on with the first girl as a normal conversation.
                    FinishB();
                    t.SB = null;
                    TrioStage.BLeaves();
                    _trio = null;
                    TrioStage.End();
                    ClearTargetButton();
                    SetNames(false);
                    _cat = "Talk";
                    Reopen();
                    return;
                case "aleft":
                    // The first girl walks off: this talk (hers) closes quietly and Duo opens a normal talk with the
                    // second one, who stayed.
                    FinishB();
                    t.SB = null;
                    Finish(true);
                    try
                    {
                        TrioStage.End();
                        if (_scene != null && _scene.resultInfo != null) _scene.resultInfo.result = ActionGame.Communication.ResultEnum.None;
                        Clear();
                        Singleton<Manager.Scene>.Instance.UnLoad();
                    }
                    catch (Exception e) { CampusLifePlugin.Log.LogError("Trio, first girl left: " + e); Game(-1); }
                    return;
                case "h3":
                    FinishB();
                    t.SB = null;
                    Finish(true);
                    try
                    {
                        if (_scene != null && _scene.resultInfo != null) _scene.resultInfo.result = ActionGame.Communication.ResultEnum.None;
                        Clear();
                        Singleton<Manager.Scene>.Instance.UnLoad();
                    }
                    catch (Exception e) { CampusLifePlugin.Log.LogError("Trio into H: " + e); Game(-1); }
                    return;
                default:
                    Game(-1);   // the game's own goodbye closes the talk (Duo says the last words on the map)
                    return;
            }
        }

        // ================= the five buttons: names for A/B and for Both =================

        private static void SetNames(bool both)
        {
            var names = both ? new[] { "Talk", "Group actions", "Settle", "Romance", "Spicy" } : new[] { "Talk", "Actions", "Do together", "Romance", "Spicy" };
            var caps = both ? new[] { "Talk", "Group", "Settle", "Romance", "Spicy" } : new[] { "Talk", "Actions", "Together", "Romance", "Spicy" };
            var was = CatNames;
            CatNames = names;
            CatCaptions = caps;
            for (var i = 0; i < CatTexts.Count && i < caps.Length; i++) if (CatTexts[i] != null) CatTexts[i].text = caps[i];
            // The open category keeps its place (Actions <-> Group actions, Together <-> Settle).
            if (_cat != null && was != names) { var k = Array.IndexOf(was, _cat); if (k >= 0) _cat = names[k]; }
        }

        // ================= who you talk to =================

        private const float TargetScale = 0.8f;
        private static Button _targetBtn;
        private static TextMeshProUGUI _targetCap;
        private static readonly List<GameObject> TargetMade = new List<GameObject>();

        private static void BuildTargetButton()
        {
            ClearTargetButton();
            if (_talk == null || CatButtons.Count == 0 || CatButtons[CatButtons.Count - 1] == null) return;
            // Under Spicy, 80% of its size, its right edge on Spicy's.
            var srt = (RectTransform)CatButtons[CatButtons.Count - 1].transform;
            var go = UnityEngine.Object.Instantiate(_talk.gameObject, srt.parent, false);
            go.name = "cl_trio_target";
            TargetMade.Add(go);
            foreach (Transform child in go.transform) if (child.name == "cl_cat_caption") UnityEngine.Object.Destroy(child.gameObject);
            const float k = TargetScale;
            var w = srt.rect.width > 10f ? srt.rect.width : 64f;
            var height = srt.rect.height > 10f ? srt.rect.height : 64f;
            var grt = (RectTransform)go.transform;
            grt.localScale = new Vector3(k, k, 1f);
            grt.anchoredPosition = srt.anchoredPosition + new Vector2((1f - srt.pivot.x) * w * (1f - k), -srt.pivot.y * height - 30f - (1f - srt.pivot.y) * height * k);
            _targetBtn = go.GetComponent<Button>();
            _targetBtn.onClick = new Button.ButtonClickedEvent();
            _targetBtn.onClick.AddListener(() =>
            {
                try
                {
                    if (_trio == null || Busy("target button")) return;
                    Sfx.Select();
                    TrioTarget(_trio.Target == TrioWho.A ? TrioWho.B : _trio.Target == TrioWho.B ? TrioWho.Both : TrioWho.A);
                }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
            });
            var cap = new GameObject("cl_trio_target_caption", typeof(RectTransform));
            TargetMade.Add(cap);
            var crt = (RectTransform)cap.transform;
            crt.SetParent(go.transform, false);
            // Right-aligned to the button and shrinking to fit: the button sits at the screen's edge ("To: Neferpitou" was cut).
            crt.anchorMin = crt.anchorMax = new Vector2(1f, 0f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-8f, -1f);
            crt.sizeDelta = new Vector2(112f, 22f);
            _targetCap = cap.AddComponent<TextMeshProUGUI>();
            var tmpl = _template != null ? _template.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            if (tmpl != null) { _targetCap.font = tmpl.font; _targetCap.fontSharedMaterial = tmpl.fontSharedMaterial; }
            // The button is drawn at 80%: the caption is enlarged back so it reads like the others.
            crt.sizeDelta = new Vector2(112f / TargetScale, 22f / TargetScale);
            _targetCap.fontSize = 17f / TargetScale;
            _targetCap.enableAutoSizing = true;
            _targetCap.fontSizeMin = 11f / TargetScale;
            _targetCap.fontSizeMax = 17f / TargetScale;
            _targetCap.enableWordWrapping = false;
            _targetCap.alignment = TextAlignmentOptions.TopRight;
            _targetCap.raycastTarget = false;
            _targetCap.color = Color.white;   // white like its tile
            RefreshTargetButton();
        }

        private static void ClearTargetButton()
        {
            foreach (var g in TargetMade) if (g != null) UnityEngine.Object.Destroy(g);
            TargetMade.Clear();
            _targetBtn = null;
            _targetCap = null;
        }

        private static void RefreshTargetButton()
        {
            var t = _trio;
            if (t == null || _targetBtn == null) return;
            var who = t.Target == TrioWho.A ? t.A : t.Target == TrioWho.B ? t.B : null;
            try
            {
                // A drawing like the other buttons, not her face: the two girls as they stand,
                // the one you talk to solid with the marker over her.
                ConvArt.Style(_targetBtn, ConvArt.Tile(t.Target == TrioWho.A ? IconArt.ToA : t.Target == TrioWho.B ? IconArt.ToB : IconArt.ToBoth, false), false);
                _targetBtn.image.preserveAspect = true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio target tile: " + e.Message); }
            if (_targetCap != null) _targetCap.text = "To: " + (who != null ? GirlInfo.FirstName(who) : "Both");
        }

        /// <summary>
        /// A <-> B: the same five buttons, only the options change. A/B <-> Both: the buttons too. Picking someone gives
        /// nothing by itself and resets nothing (the turn, the subject and the memories stay).
        /// </summary>
        private static void TrioTarget(TrioWho w)
        {
            var t = _trio;
            if (t == null) return;
            var wasBoth = t.Target == TrioWho.Both;
            t.Target = w;
            t.QuietSince = Time.realtimeSinceStartup;
            if (wasBoth != (w == TrioWho.Both)) SetNames(w == TrioWho.Both);
            TrioStage.Target(w == TrioWho.A ? 0 : w == TrioWho.B ? 1 : 2);
            RefreshTargetButton();
            TLog("target " + w);
            _catRefreshAt = 0f;
            if (RootOpen) { _sub = null; Build(); }
        }

        internal static bool TrioPick(string what)
        {
            if (_trio == null || Busy("target " + what)) return false;
            what = (what ?? "").ToLowerInvariant();
            if (what == "a") TrioTarget(TrioWho.A);
            else if (what == "b") TrioTarget(TrioWho.B);
            else if (what == "both") TrioTarget(TrioWho.Both);
            else return false;
            return true;
        }

        internal static bool TrioSlight(string who, int n)
        {
            if (_trio == null) return false;
            if (who == "a") _trio.SlightA = n; else _trio.SlightB = n;
            return true;
        }

        internal static string TrioDescribe()
        {
            var t = _trio;
            if (t == null) return "trio off (last ending: " + (TrioEnding == "" ? "-" : TrioEnding) + ")";
            return "trio " + GirlInfo.FirstName(t.A) + " + " + GirlInfo.FirstName(t.B) + ", moment " + t.Moment + ", target " + t.Target +
                   ", turn " + t.Beats + "/" + t.Max + ", bond " + Social.Bond(t.A, t.B) + ", disagree " + (t.Disagree ? t.Argue : "no") +
                   ", slights A" + t.SlightA + " B" + t.SlightB + ", kisses " + t.Kisses + ", time A" + t.A.talkTime + " B" + t.B.talkTime +
                   ", attention A" + S.Girl(GirlInfo.Key(t.A)).Attention + " B" + S.Girl(GirlInfo.Key(t.B)).Attention + "; " + TrioStage.Describe() +
                   (t.Log.Count > 0 ? "; last: " + t.Log[t.Log.Count - 1] : "");
        }

        // ================= the categories =================

        /// <summary>From ComputeCats: B's five, or the five for both. Null: A's normal ones.</summary>
        private static List<KeyValuePair<string, List<UEntry>>> TrioCats(bool noTime)
        {
            var t = _trio;
            if (t == null) return null;
            var c1 = new List<UEntry>();
            var c2 = new List<UEntry>();
            var c3 = new List<UEntry>();
            var c4 = new List<UEntry>();
            var c5 = new List<UEntry>();
            if (t.Target == TrioWho.B)
            {
                var b = t.B;
                var bn = GirlInfo.FirstName(b);
                var an = GirlInfo.FirstName(t.A);
                c1.Add(E("How's your day going?", () => BAsk("day")));
                c1.Add(E("How do you know " + an + "?", () => BAsk("know")));
                c1.Add(E("What do you think of " + an + "?", () => BAsk("think")));
                c1.Add(E("Tell her a joke", () => BAsk("joke")));
                // Touching her (pat, hand, kiss, more) is on her body, like the first girl.
                c2.Add(E("Offer her a snack", () => BAct("snack")));
                c2.Add(E("Say goodbye to both", TrioLeave, false));
                c3.Add(E("Ask her to hang out sometime", () => BAct("hangout")));
                c4.Add(E("Compliment her looks", () => BAct("compliment")));
                c5.Add(E("Whisper something suggestive", () => BAct("whisper")));
                PairOptions(b, t.A, c1, c2, c3, c4, c5);
            }
            else
            {
                var an = GirlInfo.FirstName(t.A);
                var bn = GirlInfo.FirstName(t.B);
                c1.Add(E("What were you two talking about?", () => BothTalk("what")));
                c1.Add(E("How did you two meet?", () => BothTalk("met")));
                c1.Add(E("Tell them a joke", () => BothTalk("joke")));
                var sub = MomentSubject(t.Moment);
                if (sub != null) c1.Add(E(sub, () => BothTalk("moment")));
                c2.Add(E("Treat them to a drink", () => BothTalk("drink")));
                c2.Add(E("Take a photo of the three of you", () => BothTalk("photo")));
                c2.Add(E("Say goodbye to both", TrioLeave, false));
                if (t.Disagree)
                {
                    c3.Add(E("Side with " + an, () => Settle("a")));
                    c3.Add(E("Side with " + bn, () => Settle("b")));
                    c3.Add(E("Stay in the middle", () => Settle("middle")));
                    c3.Add(E("Change the subject", () => Settle("change")));
                }
                c4.Add(E("Say you're glad to be with both", () => BothTalk("glad")));
                c4.Add(E("Compliment them both", () => BothTalk("compliment")));
                c5.Add(E("Tease them both", () => BothTalk("tease")));
                c5.Add(E("Suggest somewhere private, the three of you", TrioH3));
            }
            return new List<KeyValuePair<string, List<UEntry>>>
            {
                Cat(CatNames[0], c1, noTime), Cat(CatNames[1], c2, noTime), Cat(CatNames[2], c3, noTime),
                Cat(CatNames[3], c4, noTime), Cat(CatNames[4], c5, noTime)
            }.Where(c => c.Value.Count > 0).ToList();
        }

        private static string MomentSubject(string m)
        {
            switch (m)
            {
                case "lunch": return "Ask what they're having for lunch";
                case "club": return "Ask how club went";
                case "study": return "Offer to study together";
                case "season": return "Ask about " + SeasonName();
                case "trisal": return "Ask what the two of them planned";
                case "gossip": return "Ask who started the rumor";
                case "bestfriend": return "Ask what she's told her about you";
                default: return null;
            }
        }

        private static string SeasonName()
        {
            var id = Calendar.SeasonEventThisWeek;
            return id == "" ? "the next event" : Calendar.EventRef(id);
        }

        // ================= lines =================

        private static Dictionary<string, string> TrioCtx(SaveData.Heroine h, SaveData.Heroine other)
        {
            var c = Ctx(h, S.Girl(GirlInfo.Key(h)), new PTopic { Kind = "duo3" });
            var t = _trio;
            c["other"] = Teachers.Called(other);
            c["otherrole"] = Teachers.IsTeacherVoice(other) ? "teacher" : "student";
            c["otherjob"] = Teachers.Job(other);
            c["moment"] = t != null ? t.Moment : "hangout";
            c["pair"] = PairWord(h, other);
            c["trisal"] = h.isGirlfriend && other.isGirlfriend && Harem.Agreed(h, other) ? "yes" : "no";
            c["othergf"] = other.isGirlfriend ? "yes" : "no";
            c["likes"] = Interested(h) || h.isGirlfriend ? "yes" : "no";
            c["otherlikes"] = Interested(other) || other.isGirlfriend ? "yes" : "no";
            if (t != null && t.Argue != "") c["argue"] = t.Argue;
            if (t != null) { c["event"] = SeasonName(); c["evkind"] = Calendar.EventKind(Calendar.SeasonEventThisWeek); }
            return c;
        }

        private static string PairWord(SaveData.Heroine h, SaveData.Heroine other)
        {
            var bond = Social.Bond(h, other);
            return bond >= 60 ? "bff" : bond >= 25 ? "friend" : bond <= -30 ? "rival" : "other";
        }

        /// <summary>A line of hers from duo3.dlg (and the face its rule asks for), or the fallback.</summary>
        private static string TLine(string on, SaveData.Heroine h, SaveData.Heroine other, out Feel feel, string fallback, Dictionary<string, string> extra = null)
        {
            feel = Feel.Normal;
            try
            {
                var c = TrioCtx(h, other);
                if (extra != null) foreach (var kv in extra) c[kv.Key] = kv.Value;
                DlgRule r;
                var line = Bank.Pick(on, c, GirlInfo.Key(h), out r);
                if (!string.IsNullOrEmpty(line)) { feel = FeelOf(r, c); return line; }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Trio line " + on + ": " + e.Message); }
            return fallback;
        }

        private static void Say2(SceneBuilder b, SaveData.Heroine h, SaveData.Heroine other, string on, string fallback, Dictionary<string, string> extra = null, Feel? face = null)
        {
            Feel f;
            var line = TLine(on, h, other, out f, fallback, extra);
            if (string.IsNullOrEmpty(line)) return;
            b.Face(h, face ?? f).Say(h, line);
        }

        private static Dictionary<string, string> X(params string[] kv)
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i + 1 < kv.Length; i += 2) d[kv[i]] = kv[i + 1];
            return d;
        }

        /// <summary>The Duo scene for the new moments (lunch, after club, study, a season event, the trisal): who comes
        /// over and why, from duo3.dlg.</summary>
        internal static void TrioOpener(SceneBuilder intro, SceneBuilder sb, SaveData.Heroine a, SaveData.Heroine b, string moment)
        {
            var keep = _trio;
            _trio = new Trio { A = a, B = b, Moment = moment };
            try
            {
                var an = GirlInfo.FirstName(a);
                var bn = GirlInfo.FirstName(b);
                intro.Narr(moment == "lunch" ? an + " and " + bn + " wave at you, lunch in hand."
                    : moment == "club" ? an + " and " + bn + " come over, still talking about club."
                    : moment == "study" ? an + " and " + bn + " walk over with their notebooks."
                    : moment == "season" ? an + " and " + bn + " hurry over, full of plans."
                    : moment == "trisal" ? an + " and " + bn + " come over together, arm in arm."
                    : an + " and " + bn + " come over together.");
                Say2(sb, a, b, "duo3.open.a", T(a, "Ah, there you are... || Oh, you're here... || Ah, hello... || There you are... hi.", "Hey hey! Found you! || Found you! || Hey! There you are! || Yo! Gotcha!", "There you are. || Found you. || You're here. || There.", "Look who it is~ || Well, look who's here~ || Oh, it's you~ || Speak of the devil~"));
                Say2(sb, b, a, "duo3.open.b", T(b, "We were hoping to see you. || We hoped we'd run into you. || We were wondering where you were. || We wanted to see you.", "We were looking for you! || We searched everywhere! || Been looking for you! || We hunted you down!", "We wanted a word. || We need to talk. || A word. || We wanted you.", "We were just talking about you. || We were gossiping about you. || Your ears burning? || We were discussing you~"));
            }
            finally { _trio = keep; }
        }

        // ================= after each turn =================

        /// <summary>
        /// After a turn (ours, A's normal ones, a touch): the other girl reacts when it matters, someone passed over may
        /// walk off, the moment may end, and the two talk to each other. True: a scene is playing (it reopens the list).
        /// </summary>
        private static bool TrioAfter()
        {
            var t = _trio;
            if (t == null || t.Ending != "" || _scene == null) return false;
            t.Beats++;
            t.QuietSince = Time.realtimeSinceStartup;
            TrioStage.RefreshPanel();
            var b = new SceneBuilder(t.A, true);
            var any = false;
            string end = null;
            try
            {
                if (t.LastKiss != null && t.LastTo != null) any |= SawKiss(b, t, ref end);
                else if (t.LastLvl.HasValue && t.LastTo != null) any |= Witnessed(b, t);
                t.LastKiss = null;
                t.LastLvl = null;
                t.LastTo = null;
                if (end == null && t.SlightB >= 2) end = "bleft";
                else if (end == null && t.SlightA >= 2) end = "aleft";
                if (end == "bleft" || end == "aleft")
                {
                    var goes = end == "bleft" ? t.B : t.A;
                    var stays = end == "bleft" ? t.A : t.B;
                    Say2(b, goes, stays, "duo3.leave", T(goes, "...I'll go. You two have fun. || ...I'll leave. Have fun, you two. || ...I'll go now. || ...You two enjoy yourselves.", "Ugh, forget it! I'm out! || Forget it! Bye! || I'm done! Later! || Ugh! Leaving!", "...I'm leaving. || ...Leaving. || ...I'm going. || ...Goodbye.", "Wow. Okay. Enjoy yourselves. || Wow. Fine. Have fun. || Okay then. Enjoy. || Sure. Bye."), null, Feel.Pout);
                    b.Narr(GirlInfo.FirstName(goes) + " turns on her heel and walks off.");
                    Say2(b, stays, goes, "duo3.left", T(stays, "Ah... she left... || Ah... she's gone... || Oh no... she left... || She walked away...", "Whoa. She's mad. || Yikes! She's mad! || Whoa! Angry! || Uh oh!", "...Well. That happened. || ...Well. || ...That happened. || ...Hm.", "Oops. Somebody's jealous~ || Oops. Jealousy~ || Someone's upset~ || Touchy, isn't she?"));
                    any = true;
                }
                else if (end == null && t.Beats >= t.Max)
                {
                    end = "natural";
                    Say2(b, t.A, t.B, "duo3.end.a", T(t.A, "Ah, look at the time... || Oh, it's getting late... || Ah... time went so fast... || Look how late it is...", "Whoa, it's already that late?! || Already this late?! || Whoa! Time flew! || Late already?!", "We should get going. || Time to go. || It's late. || We should leave.", "Time flies, huh? || Time flies with me, huh? || Late already? Fun's over. || Where'd the time go~"));
                    Say2(b, t.B, t.A, "duo3.end.b", T(t.B, "This was nice. Let's do it again. || That was lovely. Again sometime? || I had fun. Let's do this again. || This was nice. Thank you.", "That was fun! Again sometime! || So fun! Again! || Best hangout! Again soon! || Let's repeat this!", "Until next time. || Next time, then. || Again sometime. || Later.", "Don't miss us too much~ || You'll miss us~ || Try not to miss us. || Dream of us~"));
                    any = true;
                }
                else if (end == null && (Random.value < 0.45f || t.Disagree && Random.value < 0.6f))
                    any |= Banter(b, t, "banter");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError("Trio after: " + e); }
            TLog("turn " + t.Beats + "/" + t.Max + (end != null ? ", ending " + end : "") + (any ? "" : ", quiet"));
            if (!any) return false;
            Hide();
            var how = end;
            if (!Scenes.PlayInTalk(b, c => { if (how != null) TrioEnd(how); else Reopen(); }, false))
            {
                if (how != null) TrioEnd(how);
                return false;
            }
            return true;
        }

        /// <summary>Witness of a kiss on the other girl: she always reacts, at once (she saw it, nobody told her).</summary>
        private static bool SawKiss(SceneBuilder b, Trio t, ref string end)
        {
            var to = t.LastTo;
            var w = TrioOther(to);
            if (w == null) return false;
            var kiss = t.LastKiss;
            t.Kisses++;
            var trisal = w.isGirlfriend && to.isGirlfriend && Harem.Agreed(w, to);
            var wrel = trisal ? "trisal" : w.isGirlfriend ? "yourgf" : PairWord(w, to);
            Say2(b, w, to, "duo3.kiss", kiss == "no"
                ? T(w, "Ah... she said no... || Oh... she refused... || Ah... no, huh... || She said no...", "Hah! Shot down! || Ha! Rejected! || Oof! Denied! || Shot down! Hah!", "...Denied. || ...Refused. || ...No. || ...Declined.", "Ouch. Rejected right in front of me~ || Ouch. Right in front of me~ || Rejected, live! || That stung, didn't it~")
                : T(w, "Eh?! R-right in front of me...? || Eh?! In front of me...? || W-with me right here...? || Um... I'm right here...", "WHOA! Hey! I'm right here! || Hey! I'm RIGHT here! || Whoa! Hello?! || Excuse me?!", "...I'm standing right here. || ...I'm present. || ...I'm here. || ...Hello.", "Oh my. Don't mind me~ || Oh my. Ignore me~ || Don't mind me~ || Go on. I'll watch."),
                X("kiss", kiss, "wrel", wrel), kiss == "no" ? Feel.Surprised : wrel == "yourgf" ? Feel.Pout : Feel.Shy);
            var wk = GirlInfo.Key(w);
            var tk = GirlInfo.Key(to);
            if (kiss != "no")
            {
                CampusLife.Memories.Add(w, "saw_kiss", "I saw you kiss " + GirlInfo.FirstName(to), wrel == "yourgf" || wrel == "rival" ? -1 : 1, true, tk);
                if (wrel == "yourgf")
                {
                    Share.Learned(w, to, true);
                    var take = Consent.Cheat(w, to, Social.WeightOf(Witness.Kiss), true);
                    if (take == "hurt" || take == "furious") { end = w == t.B ? "bleft" : "aleft"; if (take == "furious") w.anger = Math.Min(100, w.anger + 25); }
                    else if (take == "teases") (w == t.B ? (Action)(() => t.SlightB++) : () => t.SlightA++)();
                }
                else if (wrel == "rival" || Interested(w) && !trisal) { Arcs.AddBond(wk, tk, -4); if (w == t.B) t.SlightB++; else t.SlightA++; }
                else if (wrel == "bff" || wrel == "friend") Arcs.AddBond(wk, tk, 1);
            }
            if (kiss != "no") Achievements.Count("trio_kiss", to);
            TLog(GirlInfo.FirstName(w) + " saw the kiss (" + kiss + ", " + wrel + ")");
            return true;
        }

        /// <summary>The other girl reacts to a turn aimed at her friend: romance and spicy always, the rest sometimes.</summary>
        private static bool Witnessed(SceneBuilder b, Trio t)
        {
            var to = t.LastTo;
            var w = TrioOther(to);
            if (w == null) return false;
            var lvl = t.LastLvl.Value;
            if (lvl == Lvl.Friend && Random.value > 0.4f) return false;
            var trisal = w.isGirlfriend && to.isGirlfriend && Harem.Agreed(w, to);
            var wrel = trisal ? "trisal" : w.isGirlfriend ? "yourgf" : PairWord(w, to);
            var lv = lvl == Lvl.Friend ? "friend" : lvl == Lvl.Romance ? "romance" : "spicy";
            var q = t.LastQ == Q.Good ? "good" : t.LastQ == Q.Neutral ? "neutral" : "bad";
            Feel face = lvl == Lvl.Friend ? Feel.Smile : wrel == "yourgf" || wrel == "rival" ? Feel.Pout : Feel.Shy;
            if (t.LastQ == Q.Bad) face = wrel == "rival" ? Feel.Laugh : Feel.Worried;
            Say2(b, w, to, "duo3.react", WitnessStock(w, lv, q, wrel), X("lvl", lv, "q", q, "wrel", wrel), face);
            var wk = GirlInfo.Key(w);
            var tk = GirlInfo.Key(to);
            if (lvl != Lvl.Friend && t.LastQ != Q.Bad)
            {
                if (wrel == "yourgf") { Share.Learned(w, to, true); if (w == t.B) t.SlightB++; else t.SlightA++; }
                else if (wrel == "rival" || Interested(w) && !trisal) { Arcs.AddBond(wk, tk, -2); if (w == t.B) t.SlightB++; else t.SlightA++; }
                else if (wrel == "bff") Arcs.AddBond(wk, tk, 1);
                if (lvl == Lvl.Spicy) Social.Add(Social.Kinds.Flirted, to, new[] { w }, Witness.Flirt);
            }
            TLog(GirlInfo.FirstName(w) + " reacts to " + lv + "/" + q + " with " + GirlInfo.FirstName(to) + " (" + wrel + ")");
            return true;
        }

        private static string WitnessStock(SaveData.Heroine w, string lv, string q, string wrel)
        {
            if (q == "bad") return T(w, "Ah... that didn't go well... || Oh... that went badly... || Ah... not so good... || Um... oops...", "Pfft! Smooth. || Pfft! Smooth! || Haha! Fail! || Oof!", "...Nice try. || ...Nice try. || ...Failed. || ...Hm.", "Ooh, that's gotta hurt~ || Ooh, ouch~ || That hurt~ || Yikes~");
            if (lv == "friend") return T(w, "Hehe, you two get along. || You two get along so well. || Hehe, you're cute together. || You're close, huh?", "Hey, don't leave me out! || Hey! Include me! || Don't forget me! || Me too!", "...Hm. || ...Hm. || ...I see. || ...Mm.", "Aww, cute. || Aww. || Adorable. || Sweet~");
            if (wrel == "yourgf") return T(w, "U-um... I'm right here, you know... || U-um... I'm still here... || I'm right here, you know... || Hello...? I'm here...", "Hey! Your girlfriend is standing RIGHT here! || Your GIRLFRIEND is right here! || Hello! Girlfriend present! || HEY! Me! Here!", "...I'm still here. || ...I'm here. || ...Still present. || ...Ahem.", "Careful. I'm watching~ || Careful. Watching~ || I see everything~ || Eyes on you~");
            if (wrel == "rival") return T(w, "... || ...... || ...Hmph. || ......Mm.", "Ugh, seriously? || Seriously?! || Ugh! || Oh, come on!", "...Tch. || ...Tch. || ...Hmph. || ...Ugh.", "Please. She's not your type. || Please. Not your type. || She's not your style. || As if.");
            return T(w, "Ah... I-I'll pretend I didn't see that... || I-I'll pretend I didn't see... || Ah... I saw nothing... || Um... I'll look away...", "Ooh! Look at you two! || Ooh! You two! || Whoa! Look at you! || Hey hey!", "...Should I give you two a minute? || ...A minute alone? || ...Shall I leave? || ...Should I go?", "Ooh~ getting bold, are we? || Getting bold~ || Ooh, daring~ || My, my~");
        }

        /// <summary>The two of them talking to each other (after a turn, or in a silence); maybe a disagreement.</summary>
        private static bool Banter(SceneBuilder b, Trio t, string kind)
        {
            var pair = PairWord(t.A, t.B);
            var first = Random.value < 0.5f ? t.A : t.B;
            var second = TrioOther(first);
            var calm = t.Settled ? 0.3f : 1f;
            var argue = !t.Disagree && (pair == "rival" && Random.value < 0.5f * calm || pair == "other" && Random.value < 0.2f * calm);
            if (argue) { t.Disagree = true; t.Argue = ArgueAbout(t.Moment); }
            var x = X("kind", kind, "argue", t.Disagree ? "yes" : "no", "idle", t.Idles.ToString());
            if (t.Disagree) x["topic"] = t.Argue;
            Exchange(b, first, second, x);
            if (t.Disagree) b.Narr("They both look at you.");
            else if (kind == "idle" && t.Idles >= 2) Say2(b, first, second, "duo3.callback", T(first, "Ah, sorry, we left you out... || Sorry, we forgot you... || Ah, sorry, we left you out... || Oh, sorry... join us.", "Hey, you're quiet! Say something! || You're quiet! Talk! || Say something! || Hey, speak up!", "...You've been quiet. || ...Quiet today. || ...You're silent. || ...Nothing to say?", "Don't just stand there looking cute~ || Don't just stand there being cute~ || Speak, handsome~ || Quiet? How cute~"));
            return true;
        }

        /// <summary>
        /// One exchange, line and answer written together (banter.dlg): "first >> answer", or four answers for the voice of the
        /// second. Drawn apart they didn't answer each other (user, 2026-10-31).
        /// </summary>
        private static string[] PickExchange(SaveData.Heroine first, SaveData.Heroine second, Dictionary<string, string> x, out DlgRule rule, out Dictionary<string, string> c)
        {
            c = TrioCtx(first, second);
            if (x != null) foreach (var kv in x) c[kv.Key] = kv.Value;
            BanterCtx(first, second, c);
            if (x != null) foreach (var kv in x) c[kv.Key] = kv.Value;
            var raw = Bank.Pick("duo3.talk", c, GirlInfo.Key(first), out rule);
            var parts = (raw ?? "").Split(new[] { ">>" }, StringSplitOptions.None).Select(p => p.Trim()).ToArray(); // the voice may eat the space before "..."
            if (parts.Length < 2) return null;
            return new[] { parts[0], parts[parts.Length == 5 ? 1 + (int)GirlInfo.ToneOf(second) : 1] };
        }

        private static string _lastExchange;

        private static void Exchange(SceneBuilder b, SaveData.Heroine first, SaveData.Heroine second, Dictionary<string, string> x)
        {
            DlgRule r;
            Dictionary<string, string> c;
            var pair = PickExchange(first, second, x, out r, out c);
            for (var i = 0; i < 4 && pair != null && pair[0] == _lastExchange; i++) pair = PickExchange(first, second, x, out r, out c); // never the same line twice running
            if (pair == null) return;
            _lastExchange = pair[0];
            var feel = FeelOf(r, c);
            b.Face(first, feel).Say(first, pair[0]);
            b.Face(second, feel).Say(second, pair[1]);
            TLog("exchange " + (r != null ? r.Id : "?") + ": " + pair[0] + " / " + pair[1]);
        }

        /// <summary>What is really true of the two of them right now.</summary>
        private static void BanterCtx(SaveData.Heroine a, SaveData.Heroine b, Dictionary<string, string> c)
        {
            c["first"] = Teachers.Called(a);
            c["myrole"] = Teachers.IsTeacherVoice(a) ? "teacher" : "student";
            c["myjob"] = Teachers.Job(a);
            c["wet"] = Weather.Raining ? "yes" : "no";
            c["othermood"] = S.Girl(GirlInfo.Key(b)).Mood.ToString().ToLowerInvariant();
            var likes = GirlInfo.LikesOf(a).Intersect(GirlInfo.LikesOf(b)).ToList();
            if (likes.Count > 0) { c["shared"] = "yes"; c["sharedlike"] = GirlInfo.LikeLabel(likes[Random.Range(0, likes.Count)]); }
            if (!GirlInfo.InNoClub(a) && GirlInfo.ClubName(a) == GirlInfo.ClubName(b)) { c["sameclub"] = "yes"; c["club"] = GirlInfo.ClubName(a); }
        }

        /// <summary>Their exchanges side by side, in each situation.</summary>
        internal static string AuditExchanges(SaveData.Heroine a, SaveData.Heroine b)
        {
            if (a == null || b == null) return "no girls";
            var sb = new System.Text.StringBuilder();
            var cases = new[] { "", "pair=bff", "pair=rival", "period=lunch", "period=after", "wet=yes", "exam=yes", "sameclub=yes,club=Tennis Club", "shared=yes,sharedlike=cooking", "othermood=sleepy", "othermood=upset", "argue=yes,topic=where to eat after school", "trisal=yes" };
            foreach (var cs in cases)
                for (var i = 0; i < 3; i++)
                {
                    var x = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in cs.Split(',')) { var p = kv.Split('='); if (p.Length == 2) x[p[0]] = p[1]; }
                    DlgRule r;
                    Dictionary<string, string> c;
                    var pair = PickExchange(i == 2 ? b : a, i == 2 ? a : b, x, out r, out c);
                    sb.Append("\n  [").Append(cs).Append("] ").Append(r != null ? r.Id : "none").Append(": ").Append(pair != null ? pair[0] + " / " + pair[1] : "(nothing)");
                }
            return sb.ToString();
        }

        /// <summary>Every frame: a silence becomes their own conversation (instead of the game's "are you listening?").</summary>
        private static void TrioTick(TalkScene ts)
        {
            var t = _trio;
            if (t == null || t.Ending != "") return;
            var now = Time.realtimeSinceStartup;
            if (Scenes.Running || ADV.Program.isADVProcessing || GiftMenu.IsOpen || _goodbye || !CanvasOn(ts)) { t.QuietSince = now; return; }
            if (now - t.QuietSince < 18f || t.Idles >= 3) return;
            t.QuietSince = now;
            t.Idles++;
            var b = new SceneBuilder(t.A, true);
            Banter(b, t, "idle");
            TLog("silence " + t.Idles + ": they talk to each other");
            Hide();
            if (!Scenes.PlayInTalk(b, c => Reopen(), false)) Reopen();
        }

        // ================= the second girl's turns =================

        /// <summary>One of our turns: the scene, then pay, the blue bar, the witness and the list.</summary>
        private static void TrioPlay(SaveData.Heroine h, SceneBuilder b, Action pay, bool both = false)
        {
            var t = _trio;
            if (t == null) return;
            Hide();
            Scenes.PlayInTalk(b, c =>
            {
                try { if (pay != null) pay(); }
                catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                if (both && _trio != null) { var o = TrioOther(h); if (o != null) SpendTalkTime(o); if (_trio != null) _trio.LastLvl = null; }
                TrioStage.RefreshPanel();
                AfterBeat(SessFor(h));
            }, false);
        }

        private static Q QFromFavor(SaveData.Heroine h, int good, int bad)
        {
            var v = h.favor + (h.isGirlfriend ? 40 : 0) + h.relation * 10;
            return v >= good ? Q.Good : v < bad ? Q.Bad : Q.Neutral;
        }

        private static void PayTo(SaveData.Heroine h, Lvl lvl, Q q, string key)
        {
            var s = SessFor(h);
            if (s == null) return;
            Pay(s, lvl, q, key);
            if (q == Q.Good) s.Interest = Mathf.Clamp(s.Interest + 1, 0, MaxInterest);
            else if (q == Q.Bad) s.Interest = Mathf.Clamp(s.Interest - 1, 0, MaxInterest);
        }

        private static void Reply(SceneBuilder b, SaveData.Heroine h, SaveData.Heroine other, string act, Q q, string[] stock)
        {
            var qq = q == Q.Good ? "good" : q == Q.Neutral ? "neutral" : "bad";
            Say2(b, h, other, "duo3.reply", stock[(int)GirlInfo.ToneOf(h)], X("act", act, "q", qq), FaceFor(q));
        }

        private static void BAsk(string what)
        {
            var t = _trio;
            if (t == null) return;
            var b0 = t.B;
            var a = t.A;
            var b = new SceneBuilder(a, true);
            var an = GirlInfo.FirstName(a);
            Q q;
            switch (what)
            {
                case "day":
                    b.Me("How's your day going, " + GirlInfo.FirstName(b0) + "?");
                    var mood = MoodOf(b0);
                    q = mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Stressed ? Q.Neutral : Q.Good;
                    Reply(b, b0, a, "day", q, q == Q.Good ? Tn("It's been nice, thank you for asking.", "Great! Even better now!", "Fine. Better than yesterday.", "Oh? Checking up on me~?") : Tn("Um... it's been a long day...", "Ugh, don't ask...", "...Tiring.", "Could be better. Cheer me up?"));
                    break;
                case "know":
                    b.Me("So how do you know " + an + "?");
                    q = Q.Good;
                    var pair = PairWord(b0, a);
                    Say2(b, b0, a, "duo3.know", pair == "bff" ? T(b0, an + " and I have been friends forever..." + " || " + an + " and I have been friends forever... || I've known " + an + " since we were little..." + " || " + an + " and I go way back...", "Me and " + an + "? Since FOREVER! || Me and " + an + "? Since forever!" + " || " + an + "?! Childhood buddies!" + " || " + an + " and me! Ages!", "We go way back. || Long history. || We go back. || Years.", "Oh, she's stuck with me~ || She's stuck with me~ || She can't escape me. || She's mine forever~")
                        : pair == "rival" ? T(b0, "...We're in the same class. That's all. || ...Just classmates. || ...We share a class, that's all. || ...Same class. Nothing more.", "Ugh, we just... know each other. || We just know each other! || Ugh, classmates! || We're... acquainted!", "...Same class. || ...Classmates. || ...Same room. || ...Class.", "Unfortunately? Classmates. || Classmates. Sadly. || Unfortunately, same class. || Stuck in the same class.")
                        : T(b0, "We're in the same class. || Same class. || We share a lecture hall. || We're classmates.", "We hang out sometimes! || We hang out! || Sometimes we chill! || Friends-ish!", "Class. Club. You know. || Class. Club. || Around. || Here and there.", "Around. Why, curious~? || Around. Curious~? || Why do you ask~? || We know each other. Why?"), X("act", "know"));
                    if (pair == "bff") Say2(b, a, b0, "duo3.know.other", T(a, "Hehe... it's true. || Hehe... it's true. || That's right... || Mm, yes.", "Best friends! || Besties! || BFFs! || Best friends!", "...Unfortunately. || ...Sadly. || ...Unfortunately. || ...Yes.", "She'd be lost without me~ || She'd be lost without me~ || She needs me~ || She can't live without me."));
                    break;
                case "think":
                    b.Me("What do you think of " + an + "?");
                    var p2 = PairWord(b0, a);
                    q = p2 == "rival" ? Q.Neutral : Q.Good;
                    Say2(b, b0, a, "duo3.think", p2 == "rival" ? T(b0, "...Why would you ask me that, with her right here? || ...Why ask with her right here? || ...That's not fair, she's listening... || ...Do I have to answer?", "Her? She's... ugh. Fine. She's okay. || Her?! Ugh. Fine. She's okay. || She's... fine! Okay?! || Argh. She's alright!", "...No comment. || ...No comment. || ...Pass. || ...Next.", "She's alright. For a rival~ || She's alright. For a rival~ || Decent. For competition. || She's fine. I'm better.")
                        : T(b0, an + " is really kind. I'm lucky to know her." + " || " + an + " is so kind. I'm lucky. || I'm happy to know " + an + "." + " || " + an + " is wonderful.", an + "'s the best! Seriously!" + " || " + an + " rocks!" + " || " + an + "'s amazing! || Best ever, " + an + "!", an + " is reliable." + " || " + an + " is dependable." + " || " + an + " is solid." + " || " + an + " is trustworthy.", an + "? She's a handful. I love her." + " || " + an + "? A handful. Love her." + " || " + an + " is trouble. My kind." + " || " + an + "? Exhausting. Adore her."), X("act", "think"));
                    Say2(b, a, b0, "duo3.think.other", p2 == "rival" ? T(a, "...I heard that. || ...I heard that. || ...I can hear you. || ...Ahem.", "HEY! || HEY! || Rude! || I heard!", "...Hm. || ...Hm. || ...Noted. || ...I see.", "Aww, she's shy~ || Aww, shy~ || She's blushing~ || Look, she's flustered.") : T(a, "E-eh?! Stop it... || E-eh?! Stop... || Ah... don't... || You're embarrassing me...", "Awww! You're the best too! || Aww! You too! || Hehe! You're the best too! || Same to you!", "...Thanks. || ...Thanks. || ...Hm. Thanks. || ...Appreciated.", "Flatterer~ || Flatterer~ || Smooth~ || Sweet talker~"), null, p2 == "rival" ? Feel.Pout : Feel.Shy);
                    if (p2 != "rival") Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(b0), 2);
                    break;
                default:
                    b.Me(Lines.Pick("Want to hear a joke?", "So, a joke: why did the student eat his assignments? The professor said it was a piece of cake.", "Okay, okay, I've got a good one."));
                    var tone = GirlInfo.ToneOf(b0);
                    var chance = tone == Tone.Energetic || tone == Tone.Teasing ? 0.7f : 0.45f;
                    q = Random.value < chance ? Q.Good : Q.Neutral;
                    Reply(b, b0, a, "joke", q, q == Q.Good ? Tn("Pfft... hehe, that's silly.", "AHAHA! That's so dumb! I love it!", "...Heh.", "Okay, that one got me~") : Tn("Um... hehe...?", "...Wait, that's it?", "...", "Wow. Tragic."));
                    break;
            }
            TrioPlay(b0, b, () => PayTo(b0, Lvl.Friend, q, "t3b:" + what));
        }

        private static void BAct(string what)
        {
            var t = _trio;
            if (t == null) return;
            var b0 = t.B;
            var a = t.A;
            var bn = GirlInfo.FirstName(b0);
            var b = new SceneBuilder(a, true);
            var lvl = Lvl.Friend;
            Q q = Q.Good;
            Action more = null;
            switch (what)
            {
                case "snack":
                    b.Narr("You offer " + bn + " a snack from your bag.");
                    Reply(b, b0, a, "snack", q, Tn("For me? Thank you...", "Ooh, snacks! You're the best!", "...Thanks. I was hungry.", "Bribing me? It's working~"));
                    break;
                case "pat":
                    lvl = Lvl.Romance;
                    q = QFromFavor(b0, 60, 30);
                    b.Narr("You reach out and pat " + bn + "'s head.");
                    Reply(b, b0, a, "pat", q, q == Q.Bad ? Tn("Ah... p-please don't...", "Hey! I'm not a puppy!", "...Don't.", "Hands off, mister.") : Tn("Ehehe... that's nice...", "Hehe! Do it again!", "...I'm not a child. ...Don't stop.", "Oh? Getting bold~"));
                    break;
                case "hangout":
                    q = QFromFavor(b0, 45, 20);
                    b.Me("We should hang out sometime, " + bn + ".");
                    Reply(b, b0, a, "hangout", q, q == Q.Bad ? Tn("Um... maybe... sometime...", "Eh? Uh... we'll see!", "...Maybe.", "Hmm. Earn it first~") : Tn("I'd like that a lot.", "Yes! It's a promise!", "...Sure. I'd like that.", "Is that a date? Hehe, okay~"));
                    if (q == Q.Good) more = () => CampusLife.Memories.Add(b0, "promise_hangout", "you said we'd hang out sometime", 1, true, GirlInfo.Key(a));
                    break;
                case "compliment":
                    lvl = Lvl.Romance;
                    q = QFromFavor(b0, 55, 25);
                    b.Me(Lines.Pick("You look really cute today, " + bn + ".", "That hairstyle suits you, " + bn + ".", "You've got a great smile, you know."));
                    Reply(b, b0, a, "compliment", q, q == Q.Bad ? Tn("Ah... um... th-thanks...?", "Eh? Uh... okay?", "...Do you say that to everyone?", "Smooth. Not working, but smooth.") : Tn("R-really...? Thank you...", "Ehehe! You think so?!", "...Thanks. I wasn't fishing.", "I know~ but say it again."));
                    break;
                case "hand":
                    lvl = Lvl.Romance;
                    var yes = Consent.Decide(b0, Ask.HoldHand);
                    q = yes ? Q.Good : Q.Bad;
                    b.Narr("You reach for " + bn + "'s hand.");
                    if (yes) b.Narr("She lets you take it, fingers curling around yours.");
                    else b.Narr("She pulls her hand back.");
                    Reply(b, b0, a, "hand", q, yes ? Tn("...Your hand is warm.", "Hehe! Okay!", "...Just for a moment.", "Bold. In front of her, too~") : Tn("S-sorry... not here...", "Whoa, hey! Not so fast!", "...No.", "Nice try~"));
                    if (yes) more = () => Lore.Record("held_hands", Lore.Player, GirlInfo.Key(b0), witnesses: new[] { a });
                    break;
                case "whisper":
                    lvl = Lvl.Spicy;
                    q = b0.lewdness >= 50 || b0.isGirlfriend && b0.lewdness >= 30 ? Q.Good : b0.lewdness >= 25 ? Q.Neutral : Q.Bad;
                    b.Narr("You lean close and whisper something in " + bn + "'s ear.");
                    Reply(b, b0, a, "whisper", q, q == Q.Bad ? Tn("Wh-what?! Don't say things like that!", "EH?! Pervert!", "...Inappropriate.", "Ugh. Gross.") : q == Q.Neutral ? Tn("Eh... ehh?! Not with her right there...", "Wh- hey! Later, okay?!", "...Not now.", "Hmm~ maybe later.") : Tn("...You're terrible. ...I like it.", "Ooh! Hehe~ maybe!", "...Tempting.", "Mmm~ keep talking."));
                    break;
            }
            TrioPlay(b0, b, () => { PayTo(b0, lvl, q, "t3b:" + what); if (more != null) more(); });
        }

        private static void BKiss()
        {
            var t = _trio;
            if (t == null) return;
            var b0 = t.B;
            var a = t.A;
            var bn = GirlInfo.FirstName(b0);
            var key = "t3kiss:" + GirlInfo.Key(b0);
            int tries;
            S.Buffs.TryGetValue(key, out tries);
            tries = tries / 100 == S.Day ? tries % 100 + 1 : 1;
            S.Buffs[key] = S.Day * 100 + tries;
            string why;
            var answer = ForceKiss ?? Consent.Kiss(b0, tries, Onlookers(b0).Count, out why);
            var b = new SceneBuilder(a, true);
            if (answer == "yes") b.Narr("You lean in and kiss " + bn + ". She doesn't pull away.");
            else if (answer == "wary") b.Narr("You lean in. " + bn + " stiffens, hesitates... and lets you kiss her.");
            else if (answer == "wary_no") b.Narr("You lean in. " + bn + " almost lets you... then turns aside at the last second, flustered.");
            else b.Narr("You lean in for a kiss. " + bn + " turns her face away.");
            var q = answer == "no" ? Q.Bad : answer == "wary_no" ? Q.Neutral : Q.Good;
            Reply(b, b0, a, "kiss", q, answer == "yes" ? Tn("...Mm. In front of her, too...", "Hehe... wow...", "...That was... okay.", "Mmm~ again later?")
                : answer == "wary" ? Tn("Ah... um... th-that was...", "W-whoa... okay...", "...I didn't say no.", "...Huh. Okay then.")
                : answer == "wary_no" ? Tn("N-not with her watching... I mean... not now.", "W-wait! Not... not here! ...Later?", "...Almost. Not in front of her.", "Mm, tempting. Not with an audience, though.")
                : Tn("N-no... not here...", "Hey! No way!", "...No.", "Not a chance, buddy."));
            TLog("kiss " + bn + " #" + tries + " -> " + answer);
            TrioPlay(b0, b, () =>
            {
                PayTo(b0, Lvl.Spicy, q, "t3b:kiss");
                if (answer == "wary_no") CampusLife.Memories.Add(b0, "almost", "we almost kissed", 1, true);
                if (answer == "yes" || answer == "wary")
                {
                    Social.Add(Social.Kinds.Flirted, b0, Witness.Who(b0, Witness.Kiss), Witness.Kiss);
                    Lore.Record("kissed", Lore.Player, GirlInfo.Key(b0), witnesses: new[] { a });
                    if (answer == "wary") CampusLife.Memories.Add(b0, "kiss_wary", "you kissed me, and I let you", 1, true);
                    Achievements.Kissed(b0, answer == "wary");
                }
                if (_trio != null) { _trio.LastKiss = answer == "wary_no" ? "no" : answer; _trio.LastTo = b0; _trio.LastLvl = null; }
            });
        }

        // ================= both of them =================

        private static void BothTalk(string what)
        {
            var t = _trio;
            if (t == null) return;
            var a = t.A;
            var b0 = t.B;
            var b = new SceneBuilder(a, true);
            Q qa = Q.Good, qb = Q.Good;
            var lvl = Lvl.Friend;
            var bond = 0;
            switch (what)
            {
                case "what":
                    b.Me("So, what were you two talking about?");
                    Say2(b, a, b0, "duo3.what.a", T(a, "Ah, n-nothing much... || N-nothing much... || Ah, just chatting... || Um, nothing...", "Oh, you know! Stuff! || Stuff! You know! || Things! Hehe! || Just stuff!", "Nothing important. || Nothing important. || Trivial things. || Nothing.", "Wouldn't you like to know~ || Wouldn't you like to know~ || Secrets~ || Girl talk~"));
                    Say2(b, b0, a, "duo3.what.b", T(b0, "Hehe... it's a secret. || Hehe... secret. || It's a secret... || Shh... secret.", "It was about YOU! Kidding! ...Maybe. || About YOU! Kidding! Maybe! || You! Joking! ...Kinda! || Your name came up! Hehe!", "Girl stuff. || Girl stuff. || Private. || Nothing for you.", "Hmm~ should we tell him? || Should we tell him~? || Hmm, tell him? || Tell or not tell~?"));
                    break;
                case "met":
                    b.Me("How did you two meet?");
                    var pw = PairWord(a, b0);
                    Say2(b, a, b0, "duo3.met.a", pw == "rival" ? T(a, "...We didn't 'meet'. We just ended up in the same places. || ...We didn't really meet. We just kept running into each other. || ...It just happened. || ...We ended up together. That's all.", "Ugh, long story. || Long story! Ugh! || Don't ask! || Oof, long story!", "...Unavoidably. || ...Unavoidable. || ...Circumstance. || ...Inevitable.", "Fate hates me, apparently. || Fate's cruel. || Destiny's joke. || The universe hates me.") : T(a, "We sat next to each other on the first day... || We sat together on the first day... || She smiled at me on the first day... || We were seatmates from day one...", "First day of campus! She lent me an eraser! || First day! She lent me a pencil! || Day one! Instant friends! || She shared her lunch on day one!", "Class. She was the only one who made sense. || Class. She made sense. || Classmates. She was competent. || She was the only sensible one.", "She tripped over my bag. Love at first sight~ || She tripped over me. Love at first sight~ || She bumped into me. Destiny~ || She dropped her books. Classic."));
                    Say2(b, b0, a, "duo3.met.b", pw == "rival" ? T(b0, "...Let's not talk about it. || ...Let's not. || ...Please, never mind. || ...I'd rather not.", "Don't remind me! || Don't remind me! || Nooo, stop! || Ugh, forget it!", "... || ...... || ...No. || ...Skip.", "Her version is wrong, by the way. || Her version's wrong. || Don't believe her. || She lies. Cutely.") : T(b0, "Hehe... that's right. || Hehe... yes. || That's right... || Mm, true.", "And we've been inseparable since! || Inseparable ever since! || Best friends since! || Together ever since!", "...That's roughly accurate. || ...Roughly. || ...Accurate enough. || ...More or less.", "That is NOT how it went~ || That's NOT how it happened~ || Lies~ || Total fiction~"));
                    if (pw == "rival") { qa = qb = Q.Neutral; } else bond = 2;
                    break;
                case "joke":
                    b.Me(Lines.Pick("Okay, I've got one: what do you call a sleeping dinosaur? A dino-snore.", "Why was the math book sad? Too many problems.", "Want to hear a joke about the cafeteria food? Never mind, it's too hard to swallow."));
                    qa = Funny(a);
                    qb = Funny(b0);
                    Reply(b, a, b0, "joke", qa, qa == Q.Good ? Tn("Pfft... hehe...", "AHAHA! So dumb!", "...Heh.", "Okay, that's actually funny~") : Tn("Um... hehe...?", "Booo!", "...", "Tragic."));
                    Reply(b, b0, a, "joke", qb, qb == Q.Good ? Tn("Hehe, that's cute.", "HAHA! Another one!", "...Not bad.", "I'll allow it~") : Tn("...Oh.", "That was terrible!", "...No.", "Please stop~"));
                    if (qa != qb) b.Narr(qa == Q.Good ? GirlInfo.FirstName(a) + " laughs; " + GirlInfo.FirstName(b0) + " just stares at her." : GirlInfo.FirstName(b0) + " laughs; " + GirlInfo.FirstName(a) + " just stares at her.");
                    break;
                case "moment":
                    b.Me(MomentSubject(t.Moment) + "?");
                    Say2(b, a, b0, "duo3.subject.a", T(a, "Ah, well... || Ah, well... || Um, let's see... || Oh, well...", "Oh! Okay, so- || Okay so-! || Oh! Right! || So, so!", "Hm. Simple. || Simple. || Easy. || Hm.", "Glad you asked~ || Glad you asked~ || Oh, this is good~ || Funny you ask~"));
                    Say2(b, b0, a, "duo3.subject.b", T(b0, "Hehe, it's nothing special. || It's nothing special. || Hehe, just ordinary. || Nothing much.", "Yeah! And then- || Yeah! And then-! || Right?! Then-! || And THEN-!", "...That's about it. || ...That's it. || ...Basically. || ...Done.", "Isn't it obvious? || Obviously. || Isn't it clear? || Duh."));
                    bond = 1;
                    break;
                case "drink":
                    b.Narr("You buy three drinks from the vending machine.");
                    Reply(b, a, b0, "drink", Q.Good, Tn("Thank you... that's sweet.", "Yay! My favorite!", "...Thanks.", "Spoiling us? I approve~"));
                    Reply(b, b0, a, "drink", Q.Good, Tn("Ah, thank you!", "Wooo! Free drink!", "...Appreciated.", "Hmm~ you're learning."));
                    bond = 1;
                    break;
                case "photo":
                    b.Narr("You hold out your phone; the three of you squeeze into the frame.");
                    Say2(b, a, b0, "duo3.photo.a", T(a, "Ah, wait, my hair... || Wait, my hair...! || Ah, hold on...! || Not yet, my bangs...!", "Say cheese! || Cheese! || Smile! || Pose!", "...Fine. One picture. || ...One photo. || ...Fine. Once. || ...Quickly.", "Get my good side~ || My good side~ || Make me look good~ || Angle it right~"));
                    Say2(b, b0, a, "duo3.photo.b", T(b0, "Hehe... send it to me later? || Send it to me later? || Can I have a copy? || Hehe... send it?", "Again! I blinked! || Again! I blinked! || One more! || Redo!", "...Delete it if I look weird. || ...Delete if bad. || ...Erase it if I look odd. || ...Check it first.", "Ooh, this is going on my wall~ || Going on my wall~ || Framing this~ || Wallpaper material~"));
                    bond = 2;
                    break;
                case "glad":
                    lvl = Lvl.Romance;
                    b.Me("I'm really glad I get to spend time with you two.");
                    qa = QFromFavor(a, 55, 25);
                    qb = QFromFavor(b0, 55, 25);
                    Reply(b, a, b0, "glad", qa, qa == Q.Bad ? Tn("Ah... okay...", "Uh, sure?", "...Okay.", "Cheesy.") : Tn("Me too... really.", "Aww! Same!", "...Likewise.", "Aww, you sap~"));
                    Reply(b, b0, a, "glad", qb, qb == Q.Bad ? Tn("Um... thanks...?", "Eh? Okay!", "...Hm.", "Smooth.") : Tn("Hehe... I'm glad too.", "Yeah! Best trio!", "...It's not bad.", "You're stuck with us now~"));
                    bond = 1;
                    break;
                case "compliment":
                    lvl = Lvl.Romance;
                    b.Me("You two look great together, you know.");
                    qa = QFromFavor(a, 55, 25);
                    qb = QFromFavor(b0, 55, 25);
                    Reply(b, a, b0, "compliment", qa, qa == Q.Bad ? Tn("Ah... th-thanks...", "Eh? Uh, okay!", "...Thanks.", "Flattery won't work.") : Tn("E-eh... thank you...", "Hehe! We do, don't we?!", "...Thank you.", "Obviously~"));
                    Reply(b, b0, a, "compliment", qb, qb == Q.Bad ? Tn("Um...", "Huh?", "...Hm.", "Nice try.") : Tn("Hehe... you think so?", "Twins! Kinda!", "...Accepted.", "Say that again~"));
                    break;
                case "tease":
                    lvl = Lvl.Spicy;
                    b.Me(Lines.Pick("You two are blushing. Something on your minds?", "You know, you're both really cute when you get flustered.", "Careful, keep looking at me like that and I'll get ideas."));
                    qa = Lewdish(a);
                    qb = Lewdish(b0);
                    Reply(b, a, b0, "tease", qa, qa == Q.Bad ? Tn("D-don't say things like that!", "Hey! Pervert!", "...No.", "Gross.") : qa == Q.Neutral ? Tn("Eh... ehh...?", "Wh- hey!", "...Hm.", "Hmm~") : Tn("...Maybe I am.", "Hehe~ maybe!", "...Perhaps.", "Mmm~ ideas, huh?"));
                    Reply(b, b0, a, "tease", qb, qb == Q.Bad ? Tn("Ah... um... no...", "No way!", "...Inappropriate.", "Ew.") : qb == Q.Neutral ? Tn("Ehehe...?", "Uh, what?!", "...Hm.", "Cute try.") : Tn("...You're bad.", "Ooh! Hehe!", "...Interesting.", "Keep going~"));
                    break;
            }
            var qa2 = qa;
            var qb2 = qb;
            var lv2 = lvl;
            var bond2 = bond;
            TrioPlay(a, b, () =>
            {
                PayTo(a, lv2, qa2, "t3both:" + what);
                PayTo(b0, lv2, qb2, "t3both:" + what);
                if (bond2 != 0) Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(b0), bond2);
                if (what == "photo") { foreach (var g in new[] { a, b0 }) CampusLife.Memories.Add(g, "photo_three", "the photo of the three of us", 1, true, GirlInfo.Key(TrioOther(g) ?? g)); }
                if (what == "drink" || what == "photo") Lore.Record("shared", Lore.Player, GirlInfo.Key(a), GirlInfo.Key(b0), note: what == "photo" ? "took a photo together" : "shared a drink");
                TLog("both " + what + ": " + GirlInfo.FirstName(a) + " " + qa2 + ", " + GirlInfo.FirstName(b0) + " " + qb2);
            }, true);
        }

        private static Q Funny(SaveData.Heroine h)
        {
            var tone = GirlInfo.ToneOf(h);
            var c = tone == Tone.Energetic ? 0.75f : tone == Tone.Teasing ? 0.6f : tone == Tone.Gentle ? 0.5f : 0.3f;
            return Random.value < c ? Q.Good : Q.Neutral;
        }

        private static Q Lewdish(SaveData.Heroine h)
        {
            return h.lewdness >= 50 || h.isGirlfriend && h.lewdness >= 30 ? Q.Good : h.lewdness >= 25 ? Q.Neutral : Q.Bad;
        }

        /// <summary>They disagree and look at you: side with one, stay in the middle, or change the subject.</summary>
        private static void Settle(string how)
        {
            var t = _trio;
            if (t == null || !t.Disagree) return;
            var a = t.A;
            var b0 = t.B;
            var an = GirlInfo.FirstName(a);
            var bn = GirlInfo.FirstName(b0);
            var b = new SceneBuilder(a, true);
            Q qa, qb;
            var bond = 0;
            switch (how)
            {
                case "a":
                case "b":
                    var win = how == "a" ? a : b0;
                    var lose = how == "a" ? b0 : a;
                    b.Me("I think " + GirlInfo.FirstName(win) + "'s right about this one.");
                    Reply(b, win, lose, "settle_win", Q.Good, Tn("Th-thank you...", "HA! Told you!", "...Obviously.", "Thank you, judge~"));
                    Reply(b, lose, win, "settle_lose", Q.Bad, Tn("...I see.", "Whaaat?! Traitor!", "...Fine.", "Wow. Okay. Noted."));
                    qa = how == "a" ? Q.Good : Q.Bad;
                    qb = how == "a" ? Q.Bad : Q.Good;
                    bond = -3;
                    if (PairWord(a, b0) == "rival") { if (how == "a") t.SlightB++; else t.SlightA++; }
                    break;
                case "middle":
                    b.Me("Honestly? You're both a little right.");
                    Say2(b, a, b0, "duo3.middle.a", T(a, "...I guess that's fair. || ...That's fair. || ...I suppose that's fine. || ...Okay, fair.", "Ugh, that's such a cop-out! ...Fine. || Cop-out! ...Fine! || Lame! ...Okay! || Ugh! ...Alright!", "...Diplomatic. || ...Diplomatic. || ...Neutral. || ...Safe answer.", "Boring answer. But okay. || Boring. Okay. || Coward's answer. Fine. || Safe. Yawn."));
                    Say2(b, b0, a, "duo3.middle.b", T(b0, "Hehe... okay, truce? || Truce? || Hehe... peace? || Let's make up?", "Fine! Truce! || Truce! || Fine! Peace! || Deal! Truce!", "...Truce. || ...Truce. || ...Agreed. || ...Peace.", "Truce. For now~ || Truce. For now~ || Ceasefire~ || Temporary truce."));
                    qa = qb = Q.Neutral;
                    bond = PairWord(a, b0) == "rival" ? 3 : 2;
                    break;
                default:
                    b.Me("Hey, did you two hear about the new cafe near the station?");
                    Say2(b, a, b0, "duo3.change.a", T(a, "Ah... the one with the cakes? || The one with cakes? || Oh, the café? || The bakery?", "Oh! The pancake place?! || The pancake place?! || Ooh! The crepe shop?! || That dessert place?!", "...Nice dodge. || ...Nice dodge. || ...Smooth. || ...Evasive.", "Smooth subject change~ || Smooth change~ || Clever dodge~ || Changing the subject?"));
                    Say2(b, b0, a, "duo3.change.b", T(b0, "I've wanted to go there... || I've wanted to go... || I'd love to try it... || It looks so nice...", "We should ALL go! || Let's all go! || Group trip! || Everyone!", "...I heard it's good. || ...I hear it's good. || ...Worth a try. || ...Supposedly good.", "Is that an invitation~? || An invitation~? || Asking me out~? || Is that a date offer?"));
                    qa = qb = Q.Neutral;
                    break;
            }
            t.Disagree = false;
            t.Settled = true;
            var qa2 = qa;
            var qb2 = qb;
            var bond2 = bond;
            TLog("settle " + how + " (" + t.Argue + ")");
            Achievements.Count("settle:" + how);
            TrioPlay(a, b, () =>
            {
                PayTo(a, Lvl.Friend, qa2, "t3settle");
                PayTo(b0, Lvl.Friend, qb2, "t3settle");
                if (bond2 != 0) Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(b0), bond2);
                if (how == "a" || how == "b")
                {
                    var win = how == "a" ? a : b0;
                    Social.Add(Social.Kinds.Defended, win, new[] { TrioOther(win) });
                }
                else if (how == "middle") Lore.Record("made_up", GirlInfo.Key(a), GirlInfo.Key(b0), witnesses: null);
            }, true);
        }

        /// <summary>"Leave": a goodbye from both.</summary>
        private static void TrioLeave()
        {
            var t = _trio;
            if (t == null) return;
            var b = new SceneBuilder(t.A, true);
            b.Me(Lines.Pick("I should get going. See you two later.", "Sorry, I have to run. Later!", "I'll leave you two to it."));
            Say2(b, t.A, t.B, "duo3.bye.a", T(t.A, "Ah, okay... see you. || Okay... see you. || Alright, bye then. || See you later...", "Aww, already? Bye! || Already? Bye! || Aww! Bye! || Leaving? See ya!", "Later. || Later. || Bye. || Until next time.", "Running away? Bye~ || Fleeing? Bye~ || Running off? Bye. || Escaping? Later~"));
            Say2(b, t.B, t.A, "duo3.bye.b", T(t.B, "Bye bye... || Bye bye... || See you... || Take care...", "See ya! || See ya! || Bye bye! || Later!", "...Bye. || ...Bye. || ...Later. || ...Farewell.", "Don't be a stranger~ || Don't be a stranger~ || Come back soon~ || Miss me~"));
            t.Ending = "leave";
            Hide();
            if (!Scenes.PlayInTalk(b, c => TrioEnd("leave"), false)) TrioEnd("leave");
        }

        /// <summary>
        /// The three of you, somewhere private: each one decides for herself (the director's consent), and they must be
        /// fine with each other there (close friends, or both your girlfriends who agreed to share). If one says no,
        /// nothing starts, and every other option stays.
        /// </summary>
        private static void TrioH3()
        {
            var t = _trio;
            if (t == null) return;
            var a = t.A;
            var b0 = t.B;
            var together = Harem.Agreed(a, b0) || Social.Bond(a, b0) >= 50;
            var ya = Consent.Decide(a, Ask.H);
            var yb = Consent.Decide(b0, Ask.H);
            var go = ya && yb && together;
            var b = new SceneBuilder(a, true);
            b.Me(Lines.Pick("What if the three of us went somewhere quieter?", "Hey... want to go somewhere private? Both of you?", "I know a place where nobody will bother us. All three of us."));
            Reply(b, a, b0, "h3", ya ? Q.Good : Q.Bad, ya ? (together ? Tn("...If it's with her... okay.", "Eh?! ...Hehe, okay!", "...Fine.", "Ooh~ daring. I'm in.") : Tn("W-with her there? No...", "Wait, all three?! No way!", "...Not with her.", "Her too? Pass.")) : Tn("N-no... I can't...", "WHAT?! No!", "...Absolutely not.", "In your dreams."));
            Reply(b, b0, a, "h3", yb ? Q.Good : Q.Bad, yb ? (together ? Tn("...Okay. Be gentle.", "Hehe... why not!", "...Very well.", "Finally, a good idea~") : Tn("Not... with her...", "No way, not with her!", "...No. Not her.", "Her? Hard pass.")) : Tn("I-I'm not ready...", "Nope! No no no!", "...No.", "Not happening."));
            TLog("H for three: " + GirlInfo.FirstName(a) + " " + (ya ? "yes" : "no") + ", " + GirlInfo.FirstName(b0) + " " + (yb ? "yes" : "no") + ", together " + together);
            if (go)
            {
                Bounds(b, a);
                Bounds(b, b0);
                b.Narr("The three of you slip away together.");
                t.Ending = "h3";
                Hide();
                if (!Scenes.PlayInTalk(b, c => TrioEnd("h3"), false)) TrioEnd("h3");
                return;
            }
            TrioPlay(a, b, () =>
            {
                if (!ya) PayTo(a, Lvl.Spicy, Q.Bad, "t3h3");
                if (!yb) PayTo(b0, Lvl.Spicy, Q.Bad, "t3h3");
            }, true);
        }
    }
}
