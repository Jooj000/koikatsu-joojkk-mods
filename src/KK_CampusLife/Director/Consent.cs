using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>What a girl can be asked (or can ask you) in a conversation.</summary>
    internal enum Ask { Confess, H, Lunch, Club, GoHome, Date, Study, Exercise, KoiClub, Follow, Breakup, HoldHand }

    /// <summary>How fast a girl opens up (the dialogue proposal's pace profile, read from her card).</summary>
    internal enum Pace { Reserved, Normal, Open }

    /// <summary>
    /// The director answers for her (user, 2026-10-26: "the together, anger, accepting a confession, accepting H, accepting
    /// touch, etc., are things the director must do; the game only executes afterwards if the director says so").
    /// <list type="bullet">
    /// <item>Invitations, confession, date, H (Info.GetEventADV): the game's roll becomes the director's yes or no.
    /// Where the game has no "no" scene (a girlfriend's lunch, H in private), the conversation asks first and a no is
    /// played as our own "not now" (Conversation.Invite).</item>
    /// <item>What she brings up herself (Info.GetEventFromHeroineADV): at most one, the one the director wants.</item>
    /// <item>Touch (Info.GetTouchVoice): welcome, uneasy or "stop it", by how close you are and how often you touched.</item>
    /// <item>Anger (Communication.ChangeParam, plus a watch for the rest): how much a bad answer or touch hurts, and
    /// whether she gets angry at all. The apology (TalkScene.CommandFunc 10): whether she forgives.</item>
    /// </list>
    /// Each answer is a chance from the relationship (pace profile, favor, lewdness, trust, mood, grudges) and the
    /// director's own dice, fixed for the period: asking again in the same period gets the same answer.
    /// Prefixes and postfixes only, all in try/catch: on any error the game's own answer stays.
    /// </summary>
    internal static class Consent
    {
        private static CampusState S { get { return Campus.S; } }

        public static readonly List<string> Recent = new List<string>();
        private static readonly HashSet<string> Logged = new HashSet<string>();

        private static void Note(string s)
        {
            Recent.Add(S.Day + "/" + G.CurrentMask + " " + s);
            if (Recent.Count > 40) Recent.RemoveAt(0);
            CampusLifePlugin.Log.LogInfo("Director decides: " + s);
        }

        // ================= pace profile =================

        /// <summary>
        /// two different lists and a girl could be open for one and normal for the other.
        /// </summary>
        public static Pace Profile(SaveData.Heroine h)
        {
            switch (Rhythm.Of(h))
            {
                case Rhythm.Profile.Open: return Pace.Open;
                case Rhythm.Profile.Reserved: return Pace.Reserved;
                default: return Pace.Normal;
            }
        }

        /// <summary>
        /// The pace multiplier (user, 2026-10-26: "little by little the reserved ones' multiplier must grow, and when
        /// dating, everyone's goes up 0.1"): open 1.4, normal 1.0, reserved from 0.7 up to 1.0 with the days you spent
        /// with her and her trust; +0.1 while she is your girlfriend.
        /// </summary>
        public static float Multiplier(SaveData.Heroine h)
        {
            float m;
            switch (Profile(h))
            {
                case Pace.Open: m = 1.4f; break;
                case Pace.Normal: m = 1f; break;
                default:
                    var gs = S.Girl(GirlInfo.Key(h));
                    var days = Mathf.Clamp01((gs.TalkDays.Distinct().Count() - 1) / 6f);
                    var trust = Mathf.Clamp01((Social.Trust(h) - 30) / 60f);
                    m = 0.7f + 0.3f * (0.6f * days + 0.4f * trust);
                    break;
            }
            if (h.isGirlfriend) m += 0.1f;
            return m;
        }

        // ================= the answer =================

        /// <summary>How likely she says yes (0-1) and the main reasons, for the log.</summary>
        public static float Chance(SaveData.Heroine h, Ask ask, out string why)
        {
            var r = new List<string>();
            var gs = S.Girl(GirlInfo.Key(h));
            var pace = Profile(h);
            var mult = Multiplier(h);
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var trust = Social.Trust(h);
            var favor = h.favor * mult;
            var lewd = h.lewdness * mult;
            var rel = h.isGirlfriend ? "girlfriend" : h.relation >= 1 ? "friend" : h.relation == 0 ? "knows you" : "stranger";
            r.Add(rel);
            r.Add(pace.ToString().ToLowerInvariant() + " x" + mult.ToString("0.00"));
            float c;
            if (ask == Ask.Breakup)
            {
                c = Pairs.IsScorned(h) ? 0.6f : h.isAnger && Social.OwnGrudge(h) != null ? 0.4f : 0f;
                why = string.Join(", ", r.ToArray());
                return c;
            }
            if (h.isAnger) { why = "angry with you"; return 0f; }
            if (ask == Ask.H && Reactions.OwesH(h, false)) { why = "she owes you"; return 1f; }
            switch (ask)
            {
                case Ask.Confess:
                    // No floor of days (user, 2026-10-30): the daily curve of favor (G.AddFavor) makes it take ~5 days
                    // for a reserved girl, 4 for a normal one, 2-3 for an open one (pacesim).
                    c = (favor - 50f) / 40f;
                    break;
                case Ask.Date:
                    c = h.isGirlfriend ? 0.8f : h.relation >= 1 ? 0.2f + (favor - 40f) / 100f : 0.05f;
                    break;
                case Ask.H:
                {
                    // The bar decides, not a floor of days (user, 2026-10-30): H fills a little each day (G.AddFavor), so a
                    // reserved girl says yes ~2 days after you start dating (day ~7), a normal one the day after (day ~5).
                    if (pace == Pace.Reserved)
                    {
                        if (!h.isGirlfriend || gs.Chapter < 1) { r.Add("only after dating a while"); c = 0.02f; }
                        else c = (lewd - 60f) / 40f;
                    }
                    else if (pace == Pace.Normal)
                    {
                        if (h.isGirlfriend) c = (lewd - 40f) / 40f;
                        else if (h.relation >= 1 && h.lewdness >= 60 && mood == Mood.Flirty) c = 0.3f + lewd / 200f;
                        else { r.Add("not her girlfriend"); c = 0.03f; }
                    }
                    else
                    {
                        c = h.isGirlfriend ? 0.7f + lewd / 300f : h.relation >= 0 ? lewd / 60f : 0f;
                        if (gs.TalkDay == S.Day && gs.TalksToday >= 3) { c += 0.25f; r.Add("a perfect day"); }
                    }
                    if (mood == Mood.Sleepy) c -= 0.2f;
                    break;
                }
                case Ask.HoldHand:
                    c = h.isGirlfriend ? 0.95f : h.relation >= 0 ? 0.2f + favor / 120f : 0.02f;
                    break;
                default: // together: lunch, club, going home, study, exercise, the club, follow me
                    c = h.isGirlfriend ? 0.85f : h.relation >= 1 ? 0.55f + h.favor / 400f : h.relation == 0 ? 0.25f + h.favor / 300f : 0.1f;
                    var like = ask == Ask.Lunch ? "food" : ask == Ask.Study ? "study" : ask == Ask.Exercise ? "exercise" : null;
                    if (like != null && GirlInfo.Likes(h, like)) { c += 0.15f; r.Add("likes " + like); }
                    break;
            }
            var m = MoodShift(mood, ask, h);
            if (Math.Abs(m) > 0.001f) { c += m; r.Add(mood.ToString().ToLowerInvariant()); }
            c += Mathf.Clamp((trust - 50) / 500f, -0.1f, 0.1f);
            if (Social.Grudge(h) != null) { c -= 0.15f; r.Add("a grudge"); }
            if (Pairs.IsScorned(h)) { c -= 0.5f; r.Add("scorned"); }
            why = string.Join(", ", r.ToArray());
            return Mathf.Clamp01(c);
        }

        private static float MoodShift(Mood mood, Ask ask, SaveData.Heroine h)
        {
            var romantic = ask == Ask.Confess || ask == Ask.Date || ask == Ask.H || ask == Ask.HoldHand;
            switch (mood)
            {
                case Mood.Cheerful: return romantic ? 0.1f : 0.15f;
                case Mood.Hungry: return ask == Ask.Lunch ? (h.favor >= 40 || h.relation >= 1 || h.isGirlfriend ? 1f : 0.5f) : 0f;
                case Mood.Bored: return ask == Ask.Exercise || ask == Ask.Club ? 0.3f : ask == Ask.Follow ? 0.2f : ask == Ask.Study ? -0.15f : 0f;
                case Mood.Stressed: return ask == Ask.Study ? 0.35f : ask == Ask.Exercise ? -0.1f : 0f;
                case Mood.Lonely: return ask == Ask.GoHome ? 0.4f : ask == Ask.Follow ? 0.3f : ask == Ask.Lunch || ask == Ask.Date ? 0.2f : 0f;
                case Mood.Flirty: return ask == Ask.H ? 0.25f : ask == Ask.Date || ask == Ask.HoldHand ? 0.2f : ask == Ask.Confess ? 0.15f : 0f;
                case Mood.Sleepy: return ask == Ask.Exercise ? -0.4f : ask == Ask.Study ? -0.15f : ask == Ask.GoHome ? 0.2f : 0f;
                case Mood.Upset: return romantic ? -0.4f : -0.2f;
            }
            return 0f;
        }

        /// <summary>The director's dice for her, this ask and this period (0-1): the same question gets the same answer.</summary>
        private static float Dice(SaveData.Heroine h, string what)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var ch in GirlInfo.Key(h) + "|" + what + "|" + S.Day + "|" + (int)G.CurrentMask)
                {
                    hash ^= ch;
                    hash *= 16777619u;
                }
                hash ^= hash >> 13;
                hash *= 0x5bd1e995u;
                hash ^= hash >> 15;
                return (hash % 10000u) / 10000f;
            }
        }

        /// <summary>
        /// personalidade não aberta e valores que contam interesse, sendo eles favor e H acima de 50% na amiga"):
        /// "yes", "wary" or "no". Interest is favor and H above half on a friend; then mood, trust, a grudge, who is
        /// watching and how many times you tried. A girl who says yes but is not open by nature lets it happen warily;
        /// a girlfriend and an open girl just say yes. The same try in the same period gets the same answer.
        /// </summary>
        public static string Kiss(SaveData.Heroine h, int tries, int crowd, out string why)
        {
            var r = new List<string>();
            if (h.isAnger) { why = "angry with you"; return "no"; }
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var pace = Profile(h);
            var interest = h.favor > 50 && h.lewdness > 50;
            float c;
            if (h.isGirlfriend) { c = 0.92f; r.Add("girlfriend"); }
            else if (h.relation >= 1 && interest) { c = 0.55f + (h.favor - 50) / 200f + (h.lewdness - 50) / 200f; r.Add("friend, interested"); }
            else if (h.relation >= 1) { c = 0.08f + h.favor / 400f + h.lewdness / 500f; r.Add("friend"); }
            else if (h.relation == 0) { c = 0.03f + h.lewdness / 600f; r.Add("knows you"); }
            else { c = 0f; r.Add("stranger"); }
            r.Add(pace.ToString().ToLowerInvariant());
            if (mood == Mood.Flirty) { c += 0.2f; r.Add("flirty"); }
            else if (mood == Mood.Cheerful) c += 0.05f;
            else if (mood == Mood.Upset || mood == Mood.Stressed) { c -= 0.3f; r.Add(mood.ToString().ToLowerInvariant()); }
            c += Mathf.Clamp((Social.Trust(h) - 50) / 500f, -0.1f, 0.1f);
            if (Social.Grudge(h) != null) { c -= 0.15f; r.Add("a grudge"); }
            if (crowd >= 4) { c -= h.isGirlfriend ? 0.3f : 0.2f; r.Add("a crowd"); }
            if (tries > 1) { c -= 0.15f * (tries - 1); r.Add("try " + tries); }
            c = Mathf.Clamp01(c);
            var yes = Dice(h, "kiss" + tries) < c;
            var answer = !yes ? "no" : h.isGirlfriend || pace == Pace.Open ? "yes" : "wary";
            why = Mathf.RoundToInt(c * 100) + "%: " + string.Join(", ", r.ToArray());
            var line = GirlInfo.FirstName(h) + ": kiss -> " + answer + " (" + why + ")";
            if (Logged.Add(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/kiss" + tries)) Note(line);
            else CampusLifePlugin.Log.LogInfo("Director decides: " + line);
            return answer;
        }

        /// <summary>The director's answer to the player asking her.</summary>
        /// <summary>What the director would answer now, without logging it as a decision.</summary>
        public static bool WouldSay(SaveData.Heroine h, Ask ask)
        {
            string why;
            return Dice(h, ask.ToString()) < Chance(h, ask, out why);
        }

        public static bool Decide(SaveData.Heroine h, Ask ask)
        {
            string why;
            var c = Chance(h, ask, out why);
            var yes = Dice(h, ask.ToString()) < c;
            var line = GirlInfo.FirstName(h) + ": " + Label(ask) + " -> " + (yes ? "yes" : "no") + " (" + Mathf.RoundToInt(c * 100) + "%: " + why + ")";
            if (Logged.Add(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/" + ask)) Note(line);
            return yes;
        }

        private static string Label(Ask a)
        {
            switch (a)
            {
                case Ask.Confess: return "confession";
                case Ask.H: return "H";
                case Ask.GoHome: return "go home together";
                case Ask.KoiClub: return "the Koikatsu Club";
                case Ask.Follow: return "come with me";
                case Ask.HoldHand: return "hold hands";
                case Ask.Breakup: return "break up";
                default: return a.ToString().ToLowerInvariant();
            }
        }

        /// <summary>The game's event command (TalkScene command - 10) as an ask.</summary>
        public static Ask? OfCommand(int command)
        {
            switch (command)
            {
                case 1: return Ask.Confess;
                case 3: return Ask.H;
                case 4: return Ask.Lunch;
                case 5: return Ask.Club;
                case 6: return Ask.GoHome;
                case 7: return Ask.Date;
                case 8: return Ask.Study;
                case 9: return Ask.Exercise;
                case 10: return Ask.KoiClub;
                case 11: return Ask.Follow;
            }
            return null;
        }

        /// <summary>
        /// At her girlfriend stage the game has no "no" scene for most invitations (it always agrees): the conversation asks
        /// the director first and plays its own "not now" when the answer is no.
        /// </summary>
        public static bool GameHasNoRefusal(SaveData.Heroine h, int command)
        {
            return h.isGirlfriend && command != 2;
        }

        // ================= hooks =================

        private static System.Reflection.MethodInfo _infoHeroine;
        private static int _eventCommand = -1;
        private static SaveData.Heroine _eventHer;
        private static bool? _eventVerdict;   // one answer per invitation (the game may roll more than once)
        private static bool _herTurn;
        private static SaveData.Heroine _herTurnGirl;
        private static bool _herTurnTaken;

        public static void Install(Harmony harmony)
        {
            var info = AccessTools.TypeByName("ActionGame.Communication.Info");
            if (info == null) { CampusLifePlugin.Log.LogWarning("Director answers: the game's talk logic was not found; the game answers."); return; }
            _infoHeroine = AccessTools.PropertyGetter(info, "heroine");
            Patch(harmony, AccessTools.Method(info, "GetEventADV"), nameof(EventBefore), nameof(EventAfter));
            Patch(harmony, AccessTools.Method(info, "RandomBranch"), nameof(Roll), null);
            Patch(harmony, AccessTools.Method(info, "GetEventFromHeroineADV", new[] { typeof(ActionGame.Communication.ChangeValueAbstractInfo).MakeByRefType(), typeof(int).MakeByRefType() }), nameof(HerTurnBefore), nameof(HerTurnAfter));
            Patch(harmony, AccessTools.Method(info, "CheckConditions"), null, nameof(HerWish));
            Patch(harmony, AccessTools.Method(info, "GetTouchVoice"), nameof(Touch), null);
            Patch(harmony, AccessTools.Method(typeof(Manager.Communication), "ChangeParam"), nameof(AngerBefore), nameof(AngerAfter));
            Patch(harmony, AccessTools.Method(typeof(TalkScene), "CommandFunc"), nameof(Command), null);
        }

        private static void Patch(Harmony harmony, System.Reflection.MethodBase m, string pre, string post)
        {
            if (m == null) { CampusLifePlugin.Log.LogWarning("Director answers: a game method was not found (" + (pre ?? post) + "); that answer stays the game's."); return; }
            harmony.Patch(m, pre == null ? null : new HarmonyMethod(AccessTools.Method(typeof(Consent), pre)), post == null ? null : new HarmonyMethod(AccessTools.Method(typeof(Consent), post)));
        }

        private static bool On { get { return Campus.On && Cfg.Enabled.Value; } }

        private static SaveData.Heroine HerOf(object info)
        {
            try { return _infoHeroine == null || info == null ? null : _infoHeroine.Invoke(info, null) as SaveData.Heroine; }
            catch { return null; }
        }

        // ---------------- the player asks (events) ----------------

        private static void EventBefore(object __instance, int _command)
        {
            try { _eventCommand = _command; _eventHer = HerOf(__instance); _eventVerdict = null; }
            catch { _eventCommand = -1; }
        }

        private static void EventAfter() { _eventCommand = -1; _eventHer = null; _eventVerdict = null; }

        /// <summary>Every yes/no roll inside an invitation is the director's answer.</summary>
        private static void Roll(int[] _values)
        {
            try
            {
                if (_eventCommand < 0 || _eventHer == null || _values == null || _values.Length != 2 || _values[0] + _values[1] != 100 || !On) return;
                var ask = OfCommand(_eventCommand);
                if (!ask.HasValue) return;
                if (!_eventVerdict.HasValue)
                {
                    _eventVerdict = Decide(_eventHer, ask.Value);
                    if (_eventVerdict.Value && ask.Value == Ask.H) Reactions.OwesH(_eventHer, true);
                }
                var yes = _eventVerdict.Value;
                _values[0] = yes ? 100 : 0;
                _values[1] = 100 - _values[0];
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (invitation): " + e.Message); }
        }

        // ---------------- she brings something up ----------------

        private static void HerTurnBefore(object __instance)
        {
            _herTurn = true;
            _herTurnGirl = HerOf(__instance);
            _herTurnTaken = false;
        }

        private static void HerTurnAfter() { _herTurn = false; _herTurnGirl = null; }

        /// <summary>The game's "would she bring it up now" chance becomes the director's choice: one thing at most.</summary>
        private static void HerWish(int _stage, int _condition, ref bool __result)
        {
            try
            {
                if (!_herTurn || _herTurnGirl == null || !On) return;
                var h = _herTurnGirl;
                // The game's own limits (favor and lewdness ranges) stay; only its dice are replaced.
                var cond = Singleton<Manager.Communication>.Instance.GetOccurrenceCondition(_stage, _condition);
                if (cond != null && (!cond.favor.through && !cond.favor.Check(h.favor) || !cond.lewdness.through && !cond.lewdness.Check(h.lewdness))) return;
                var game = __result;
                if (_herTurnTaken) { __result = false; return; }
                var ask = OfHerCondition(_condition);
                string why;
                var c = Chance(h, ask, out why) * Initiative(h, ask);
                var yes = Dice(h, "her:" + ask) < c;
                __result = yes;
                if (yes) _herTurnTaken = true;
                if (yes || game) Note(GirlInfo.FirstName(h) + " brings up " + Label(ask) + ": " + (yes ? "yes" : "no") + " (" + Mathf.RoundToInt(c * 100) + "%: " + why + (game && !yes ? "; the game wanted it" : "") + ")");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (her turn): " + e.Message); }
        }

        /// <summary>The game's condition numbers for what she proposes (x00/x01 confession, x02 H, x03 lunch...; 200/201 break up).</summary>
        private static Ask OfHerCondition(int cond)
        {
            if (cond == 200 || cond == 201) return Ask.Breakup;
            switch (cond % 100)
            {
                case 0: case 1: return Ask.Confess;
                case 2: return Ask.H;
                case 3: return Ask.Lunch;
                case 4: return Ask.Club;
                case 5: return Ask.GoHome;
                case 6: return Ask.Date;
                case 7: return Ask.Study;
                case 8: return Ask.Exercise;
                case 9: return Ask.KoiClub;
            }
            return Ask.Follow;
        }

        /// <summary>How readily she takes the first step herself.</summary>
        private static float Initiative(SaveData.Heroine h, Ask ask)
        {
            if (ask == Ask.Breakup) return 1f;
            var pace = Profile(h);
            var f = pace == Pace.Open ? 0.35f : pace == Pace.Normal ? 0.25f : 0.12f;
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            if (mood == Mood.Lonely && (ask == Ask.GoHome || ask == Ask.Lunch || ask == Ask.Follow)) f *= 1.5f;
            if (mood == Mood.Flirty && (ask == Ask.H || ask == Ask.Date || ask == Ask.Confess)) f *= 1.5f;
            if (ask == Ask.Confess) return Chance(h, Ask.Confess, out _) >= 0.8f ? f * 1.5f : 0f; // she confesses only when she is sure
            if (ask == Ask.H) f *= 0.6f;
            return f;
        }

        // ---------------- touch ----------------

        private static TalkScene _touchScene;
        private static bool _touchSeen;
        private static readonly Dictionary<int, int> TouchCount = new Dictionary<int, int>();

        /// <summary>
        /// The game picks the reaction from how many times you touched the same place (with a roll). The director picks it:
        /// 0 welcome, 1 uneasy, 2 "stop it" (the game's own lines and values for each).
        /// </summary>
        private static void Touch(object __instance, bool _touch, int _parts, ref int _stage)
        {
            try
            {
                if (!On) return;
                var h = HerOf(__instance);
                if (h == null || h.isAnger) return;
                var ts = G.TalkScene;
                if (ts != _touchScene) { _touchScene = ts; TouchCount.Clear(); _touchSeen = false; }
                // map, only the ones in range, looking and with nothing in between) knows it first-hand: "I saw", not
                // "they told me you flirted". Your girlfriend's stance comes from it (Share).
                if (_touch && !_touchSeen && !h.isGirlfriend)
                {
                    var seen = Witness.Who(h, Witness.Touch);
                    if (seen.Count > 0) { _touchSeen = true; Social.Add(Social.Kinds.Flirted, h, seen, Witness.Touch); Note(GirlInfo.FirstName(h) + ": touched in sight of " + string.Join(", ", seen.Select(g => GirlInfo.FirstName(g)).ToArray())); }
                }
                var key = _parts + (_touch ? 0 : 10);
                int n;
                TouchCount.TryGetValue(key, out n);
                TouchCount[key] = ++n;
                var tol = TouchTolerance(h, _touch, _parts);
                var level = n <= tol ? 0 : n <= tol + 2 ? 1 : 2;
                if (level != _stage)
                    Note(GirlInfo.FirstName(h) + ": touch " + (_touch ? "" : "(look) ") + PartName(_parts, _touch) + " #" + n + " -> " + (level == 0 ? "welcome" : level == 1 ? "uneasy" : "stop it") + " (the game rolled " + _stage + "; tolerance " + tol + ")");
                _stage = level;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (touch): " + e.Message); }
        }

        /// <summary>How many touches of that place she enjoys before it gets too much.</summary>
        private static int TouchTolerance(SaveData.Heroine h, bool touch, int part)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var pace = Profile(h);
            var lewd = h.lewdness * Multiplier(h);
            int tol;
            if (part == 1) // chest
                tol = h.isGirlfriend ? (lewd >= 20 ? 3 : 1) : pace == Pace.Open && lewd >= 50 && h.relation >= 1 ? 2 : 0;
            else
                tol = h.isGirlfriend ? 6 : h.relation >= 1 ? 3 : h.relation == 0 ? 1 : 0;
            if (!touch) tol += 1;
            if (mood == Mood.Flirty) tol += 1;
            if (mood == Mood.Upset) tol -= 1;
            if (Social.Trust(h) >= 75) tol += 1;
            // Who is watching: a crowd makes her shy unless she enjoys the show; alone she lets you closer.
            var watching = Conversation.Onlookers(h).Count;
            if (watching >= 3 && lewd < 60 && !GirlInfo.Quirks(h).bitch) tol -= 1;
            else if (watching == 0 && h.relation >= 1) tol += 1;
            return Math.Max(0, tol);
        }

        private static string PartName(int part, bool touch)
        {
            switch (part)
            {
                case 0: return touch ? "hand" : "head/cheek";
                case 1: return "chest";
                case 2: return "head";
                case 3: return "cheek";
            }
            return "part " + part;
        }

        // ---------------- anger ----------------

        private struct AngerState { public int Anger, Raw; public bool Angry, Date; public SaveData.Heroine Her; }

        private static void AngerBefore(SaveData.Heroine _heroine, ActionGame.Communication.ChangeValueAbstractInfo _value, out AngerState __state)
        {
            __state = new AngerState();
            // Raw: the anger the game asked for; it clamps at 99/100 before our postfix sees it
            try { if (_heroine != null) __state = new AngerState { Anger = _heroine.anger, Raw = _value != null ? _value.anger : 0, Angry = _heroine.isAnger, Date = _heroine.isDate, Her = _heroine }; }
            catch { }
        }

        /// <summary>How much a bad answer or touch hurts, and whether she gets angry, is the director's call.</summary>
        private static void AngerAfter(SaveData.Heroine _heroine, AngerState __state)
        {
            try
            {
                var h = _heroine;
                if (h == null || __state.Her != h || !On) return;
                var d = Math.Max(h.anger - __state.Anger, __state.Raw);
                if (d <= 0 || __state.Angry) return;
                var gs = S.Girl(GirlInfo.Key(h));
                var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
                var k = 1f;
                var r = new List<string>();
                if (h.isGirlfriend) { k *= 0.6f; r.Add("girlfriend"); }
                else if (h.relation >= 1) { k *= 0.8f; r.Add("friend"); }
                if (Social.Trust(h) >= 70) { k *= 0.8f; r.Add("trusts you"); }
                if (mood == Mood.Cheerful) { k *= 0.8f; r.Add("cheerful"); }
                if (mood == Mood.Upset) { k *= 1.3f; r.Add("upset"); }
                if (Social.Grudge(h) != null) { k *= 1.2f; r.Add("a grudge"); }
                var cap = !h.talkEvent.Contains(0) && !h.talkEvent.Contains(1) ? 99 : 100;
                var want = Mathf.Clamp(__state.Anger + Mathf.RoundToInt(d * k), 0, cap);
                var angry = want >= 100;
                h.anger = want;
                if (h.isAnger && !angry)
                {
                    h.isAnger = false;
                    h.isDate = __state.Date;
                    try { var rank = Singleton<Manager.Game>.Instance.rankSaveData; if (rank.angerCount > 0) rank.angerCount -= 1U; } catch { }
                    Note(GirlInfo.FirstName(h) + ": annoyed, not angry (anger " + __state.Anger + " -> " + want + ", the game had her angry; " + string.Join(", ", r.ToArray()) + ")");
                }
                else if (angry)
                {
                    if (!h.isAnger)
                    {
                        h.isAnger = true;
                        h.isDate = false;
                        try { Singleton<Manager.Game>.Instance.rankSaveData.angerCount += 1U; } catch { }
                    }
                    Note(GirlInfo.FirstName(h) + ": angry (anger " + __state.Anger + " -> " + want + "; " + string.Join(", ", r.ToArray()) + ")");
                    Fallout(h);
                }
                _sanctioned = h;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (anger): " + e.Message); }
        }

        private static SaveData.Heroine _sanctioned;
        private static int _breakupFrame = -1;
        private static readonly Dictionary<SaveData.Heroine, bool> WasAngry = new Dictionary<SaveData.Heroine, bool>();
        private static float _nextWatch;

        /// <summary>
        /// Anger the game sets elsewhere (a date with another girl, being caught where you shouldn't be): the director
        /// confirms it, or softens it for a girl who trusts you deeply. A break-up you chose always stands.
        /// </summary>
        public static void Tick()
        {
            if (Time.realtimeSinceStartup < _nextWatch) return;
            _nextWatch = Time.realtimeSinceStartup + 0.5f;
            try
            {
                if (!On || !Singleton<Manager.Game>.IsInstance()) return;
                foreach (var h in Singleton<Manager.Game>.Instance.HeroineList)
                {
                    if (h == null) continue;
                    bool was;
                    var known = WasAngry.TryGetValue(h, out was);
                    WasAngry[h] = h.isAnger;
                    if (!known || was || !h.isAnger) continue;
                    if (h == _sanctioned || Time.frameCount - _breakupFrame < 600) { _sanctioned = null; continue; }
                    if (Social.Trust(h) >= 80 && h.favor >= 80 && Social.Grudge(h) == null)
                    {
                        h.isAnger = false;
                        h.anger = 70;
                        WasAngry[h] = false;
                        Note(GirlInfo.FirstName(h) + ": hurt, not angry (she trusts you; the game had her angry)");
                    }
                    else { Note(GirlInfo.FirstName(h) + ": angry (the game's reason stands)"); Fallout(h); }
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (anger watch): " + e.Message); }
        }

        // ---------------- what anger costs ----------------

        private static readonly Dictionary<string, int> AngryToday = new Dictionary<string, int>();

        /// <summary>
        /// She just got angry (user, 2026-10-26): it costs favor, and with the bar near 0 her level may drop, more likely
        /// from 0. A friend becomes an acquaintance and a girlfriend breaks up with you, both with the bar at the middle;
        /// an acquaintance is the floor. The game keeps the level as marks (girlfriend flag, talk event 2), so a drop
        /// takes the mark off.
        /// </summary>
        private static void Fallout(SaveData.Heroine h)
        {
            try
            {
                if (h == null || h.relation < 0) return;
                var key = S.Day + "/" + GirlInfo.Key(h);
                int n;
                AngryToday.TryGetValue(key, out n);
                AngryToday[key] = ++n;
                var gs = S.Girl(GirlInfo.Key(h));
                var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
                var grudge = Social.Grudge(h) != null;
                var trust = Social.Trust(h);
                var k = 1f;
                if (trust >= 70) k *= 0.6f;
                if (mood == Mood.Cheerful) k *= 0.8f;
                if (mood == Mood.Upset) k *= 1.2f;
                if (grudge) k *= 1.3f;
                var before = h.favor;
                h.favor = Mathf.Clamp(before - Mathf.RoundToInt(12f * k), 0, 100);
                var line = GirlInfo.FirstName(h) + ": anger costs favor " + before + " -> " + h.favor;
                if (h.relation == 0 || h.favor > 15) { Note(line); return; }
                var c = before <= 0 ? 0.6f : 0.15f + 0.25f * (15 - h.favor) / 15f;
                var why = new List<string> { before <= 0 ? "bar at 0" : "bar near 0" };
                c -= (trust - 50) / 200f;
                if (grudge) { c += 0.15f; why.Add("a grudge"); }
                if (mood == Mood.Upset) { c += 0.1f; why.Add("upset"); }
                if (mood == Mood.Cheerful) { c -= 0.1f; why.Add("cheerful"); }
                if (n > 1) { c += 0.15f; why.Add("angry again today"); }
                why.Add("trust " + trust);
                c = Mathf.Clamp01(c);
                var drop = Dice(h, "drop" + n) < c;
                Note(line + "; level drop " + (drop ? "yes" : "no") + " (" + Mathf.RoundToInt(c * 100) + "%: " + string.Join(", ", why.ToArray()) + ")");
                if (!drop) return;
                var name = GirlInfo.FirstName(h);
                if (h.isGirlfriend)
                {
                    h.isGirlfriend = false;
                    h.isDate = false;
                    Toasts.Show(name + " broke up with you.");
                }
                else Toasts.Show(name + " is no longer your friend.");
                h.talkEvent.Remove(2);
                if (!h.talkEvent.Contains(0) && !h.talkEvent.Contains(1)) h.talkEvent.Add(0);
                h.favor = 50;
                Note(name + ": now an acquaintance, favor 50");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (anger fallout): " + e.Message); }
        }

        // ---------------- the apology ----------------

        private static readonly Dictionary<string, int> Apologies = new Dictionary<string, int>();

        /// <summary>
        /// The game takes 30-50 anger off at random when you apologize. The director decides: she forgives you (anger 0) or
        /// she is still angry, with how much she has left.
        /// </summary>
        private static void Command(TalkScene __instance, int _kind)
        {
            try
            {
                if (!On || __instance == null) return;
                var h = __instance.targetHeroine;
                if (h == null) return;
                if (_kind == 12) { _breakupFrame = Time.frameCount; return; }
                if (_kind != 10 || !h.isAnger) return;
                var key = S.Day + "/" + GirlInfo.Key(h);
                int n;
                Apologies.TryGetValue(key, out n);
                Apologies[key] = ++n;
                var gs = S.Girl(GirlInfo.Key(h));
                var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
                var c = 0.3f + Social.Trust(h) / 200f + h.favor / 300f + (n - 1) * 0.15f;
                if (Social.Grudge(h) != null) c -= 0.2f;
                if (mood == Mood.Upset) c -= 0.2f;
                if (mood == Mood.Cheerful) c += 0.1f;
                c = Mathf.Clamp01(c);
                var yes = Dice(h, "sorry" + n) < c;
                var left = yes ? 0 : Mathf.Clamp(h.anger - 40, 20, 80);
                h.anger = yes ? 0 : left + 40; // the game then takes 30-50 off: 0 stays 0; the rest lands near "left"
                Note(GirlInfo.FirstName(h) + ": apology #" + n + " -> " + (yes ? "forgiven" : "still angry (about " + left + ")") + " (" + Mathf.RoundToInt(c * 100) + "%)");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (apology): " + e.Message); }
        }
    }
}
