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
        /// with her and her trust; +0.1 while she is your girlfriend; then times the player's "Ease" option for her pace.
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
            // The player's own difficulty per pace, 1 by default.
            var pace = Profile(h);
            m *= (pace == Pace.Open ? Cfg.EaseOpen : pace == Pace.Normal ? Cfg.EaseNormal : Cfg.EaseReserved).Value;
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
                // upset girlfriend under a friend who likes you a lot, and she never held your hand).
                case Ask.Date:
                    c = h.isGirlfriend ? Mathf.Max(0.8f, 0.5f + (favor - 40f) / 100f) : h.relation >= 1 ? 0.2f + (favor - 40f) / 100f : 0.05f;
                    break;
                case Ask.H:
                    c = HWant(h, gs, pace, favor, lewd, mood, r);
                    break;
                case Ask.HoldHand:
                    c = h.isGirlfriend ? Mathf.Max(0.95f, 0.55f + favor / 120f) : h.relation >= 0 ? 0.2f + favor / 120f : 0.02f;
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
            // After a wary H: waiting for you to take responsibility makes a confession welcome; pulling away cools the rest.
            if (gs.Guilt == 1 && ask == Ask.Confess) { c += 0.3f; r.Add("wants you to take responsibility"); }
            if (gs.DistanceUntil >= S.Day && ask != Ask.H && ask != Ask.Breakup)
            {
                var romantic = ask == Ask.Confess || ask == Ask.Date || ask == Ask.HoldHand;
                c -= romantic ? 0.3f : 0.15f;
                r.Add("keeping her distance");
            }
            why = string.Join(", ", r.ToArray());
            return Mathf.Clamp01(c);
        }

        // ================= H: the context decides =================

        /// <summary>
        /// How much she wants it now, before mood, trust, a grudge and scorn (Chance adds those for every ask). No fixed
        /// gates (user, 2026-10-30: "para todas, permita ter H sem namorar, porém, crie a restrição pelo contexto, não
        /// artificialmente"): the base comes from what you are to her, her pace and her bars, and every other thing that
        /// makes sense moves it: the days you have spent together, your reputation, H you already had, a recent kiss, a
        /// wary H she still feels guilty about, another girlfriend, a perfect day, how often she already said no today.
        /// A reserved girl who is not dating you needs very high bars and good context; a normal one needs less; an open
        /// </summary>
        private static float HWant(SaveData.Heroine h, GirlState gs, Pace pace, float favor, float lewd, Mood mood, List<string> r)
        {
            float c;
            Brakes.Clear();
            if (h.isGirlfriend)
            {
                c = pace == Pace.Reserved ? (lewd - 60f) / 40f : pace == Pace.Normal ? (lewd - 40f) / 40f : 0.7f + lewd / 300f;
                if (pace == Pace.Reserved) Brake("personality", 0.15f);
                if (pace == Pace.Reserved && gs.Chapter < 1) { c -= 0.25f; r.Add("dating only a short while"); Brake("soon", 0.25f); }
            }
            else if (h.relation >= 1)
            {
                // situations); she now needs maxed bars plus a good context (flirty, trust) to reach ~30%.
                c = pace == Pace.Reserved ? (lewd - 75f) / 40f + (favor - 80f) / 80f
                    : pace == Pace.Normal ? (lewd - 65f) / 40f + (favor - 70f) / 100f
                    : lewd / 60f;
                r.Add("not dating");
                if (pace != Pace.Open) Brake("personality", pace == Pace.Reserved ? 0.35f : 0.15f);
            }
            else if (h.relation == 0)
            {
                c = pace == Pace.Open ? lewd / 90f - 0.1f : -0.4f;
                r.Add("barely knows you");
                Brake(pace == Pace.Open ? "soon" : "personality", pace == Pace.Open ? 0.2f : 0.4f);
            }
            // strangers in a flirty or cheerful mood got 10-23%).
            else return pace == Pace.Open ? lewd / 120f - 0.35f : -0.6f;
            // Days together: a girl you met three days ago is not one you have known for a week.
            var days = gs.TalkDays.Distinct().Count();
            var d = Mathf.Clamp((days - 4) * 0.02f, -0.06f, 0.1f);
            if (Mathf.Abs(d) > 0.001f) { c += d; r.Add(days + " days together"); if (d < 0f) Brake("soon", -d); }
            // Your name at school: a boy everyone speaks well of is easier to trust with this.
            var rep = Mathf.Clamp((S.Reputation - 50) / 500f, -0.1f, 0.1f);
            if (Mathf.Abs(rep) >= 0.02f) { c += rep; r.Add("reputation " + S.Reputation); if (rep < 0f) Brake("name", -rep); }
            // What already happened between you.
            if (h.hCount > 0) { c += h.isGirlfriend ? 0.05f : 0.12f; r.Add("H before (" + h.hCount + ")"); }
            else if (!h.isGirlfriend && pace != Pace.Open) { c -= 0.08f; r.Add("her first time with you"); Brake("soon", 0.08f); }
            if (gs.Moments.Any(m => (m.Kind == "kiss_wary" || m.Kind == "almost") && S.Day - m.Day <= 5)) { c += 0.08f; r.Add("a recent kiss or almost"); }
            if (gs.Moments.Any(m => m.Kind == "feelings" && m.Tone > 0 && S.Day - m.Day <= 7)) { c += 0.05f; r.Add("you talked feelings"); }
            if (gs.Guilt > 0) { c -= 0.3f; r.Add(gs.Guilt == 1 ? "waiting for you to take responsibility" : "pulling away after last time"); Brake("guilt", 0.3f); }
            if (gs.DistanceUntil >= S.Day) { c -= 0.25f; r.Add("keeping her distance"); Brake("guilt", 0.25f); }
            // Someone else: another girlfriend weighs on one who is not yours.
            if (!h.isGirlfriend && OtherGirlfriend(h))
            {
                var gfw = pace == Pace.Open ? 0.05f : pace == Pace.Normal ? 0.12f : 0.18f;
                c -= gfw;
                r.Add("you have a girlfriend");
                Brake("girlfriend", gfw);
            }
            // The day: a perfect one helps; asking again after a no does not.
            if (gs.TalkDay == S.Day && gs.TalksToday >= 3) { c += pace == Pace.Open ? 0.25f : 0.1f; r.Add("a perfect day"); }
            int asked;
            if (HNoToday.TryGetValue(S.Day + "|" + GirlInfo.Key(h), out asked) && asked > 0) { c -= 0.1f * asked; r.Add("said no " + asked + "x today"); Brake("asked", 0.1f * asked); }
            if (gs.Devotion > 0) c += Mathf.Min(0.08f, gs.Devotion / 600f);
            if (h.isGirlfriend && gs.Attention < 30) { c -= 0.08f; r.Add("feels neglected"); Brake("neglected", 0.08f); }
            switch (mood)
            {
                case Mood.Sleepy: c -= 0.2f; Brake("mood", 0.2f); break;
                case Mood.Stressed: c -= 0.1f; Brake("mood", 0.1f); break;
                case Mood.Upset: Brake("mood", 0.4f); break;   // Chance takes it off through MoodShift
                case Mood.Lonely: if (!h.isGirlfriend) { c += 0.08f; r.Add("lonely"); } break;
            }
            return c;
        }

        /// <summary>What held her back in the last <see cref="HWant"/>: each brake and how much it weighed.</summary>
        private static readonly Dictionary<string, float> Brakes = new Dictionary<string, float>();

        private static void Brake(string what, float amount)
        {
            float v;
            Brakes.TryGetValue(what, out v);
            Brakes[what] = v + amount;
        }

        private static readonly Dictionary<string, string> HHeld = new Dictionary<string, string>();

        /// <summary>
        /// Why she said a wary no this period (her biggest brake: "personality", "mood", "guilt", "girlfriend", "soon",
        /// "asked", "grudge", "neglected", "name" or "moment"), or "" if she did not.
        /// </summary>
        public static string HeldBack(SaveData.Heroine h)
        {
            string a;
            return h != null && HHeld.TryGetValue(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h), out a) ? a : "";
        }

        private static string _held = "";

        private static bool OtherGirlfriend(SaveData.Heroine h)
        {
            return G.Heroines.Any(x => x != null && x != h && x.isGirlfriend);
        }

        private static readonly Dictionary<string, int> HNoToday = new Dictionary<string, int>();
        private static readonly Dictionary<string, string> HCache = new Dictionary<string, string>();

        /// <summary>The H answer this period ("yes", "wary_yes", "wary_no", "no"), or "" if she was not asked.</summary>
        public static string LastH(SaveData.Heroine h)
        {
            string a;
            return h != null && HCache.TryGetValue(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h), out a) ? a : "";
        }

        /// <summary>
        /// The four answers (user, 2026-10-30: "o diretor lê todas as variáveis, e decide se sim, sim com receio, não com
        /// receio ou não"). The dice against the chance say yes or no; then her unease turns a yes into a wary yes (she lets
        /// it happen, then feels guilty), and her interest turns a no into a wary no (she wanted to, but not now). Unease
        /// reads what you are to her, her pace, whether it is her first time, trust, another girlfriend, guilt still open,
        /// the days together and her mood; interest reads the chance itself, her bars, her mood and old hurts.
        /// </summary>
        public static string HAnswer(SaveData.Heroine h, out float chance, out string why)
        {
            chance = Chance(h, Ask.H, out why);
            if (h.isAnger) return "no";
            if (Reactions.OwesH(h, false)) return "yes";
            return HTier(h, Dice(h, Ask.H.ToString()) < chance, ref why);
        }

        /// <summary>The answer once the dice said yes or no.</summary>
        internal static string HTier(SaveData.Heroine h, bool yes, ref string why)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var pace = Profile(h);
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var trust = Social.Trust(h);
            if (yes)
            {
                var u = h.isGirlfriend ? 0f : 0.5f;
                u += pace == Pace.Reserved ? 0.25f : pace == Pace.Normal ? 0.1f : -0.35f;
                u += h.hCount == 0 ? 0.15f : -0.1f;
                u += trust < 40 ? 0.15f : trust >= 70 ? -0.1f : 0f;
                if (!h.isGirlfriend && OtherGirlfriend(h)) u += 0.2f;
                if (gs.Guilt > 0) u += 0.2f;
                if (gs.TalkDays.Distinct().Count() < (pace == Pace.Open ? 4 : 5)) u += 0.1f;
                if (mood == Mood.Flirty) u -= 0.15f;
                else if (mood == Mood.Stressed || mood == Mood.Upset) u += 0.1f;
                if (h.isGirlfriend && gs.Chapter < 1 && pace == Pace.Reserved) u += 0.3f;
                why += "; unease " + u.ToString("0.00");
                return u >= 0.5f ? "wary_yes" : "yes";
            }
            // A wary no is an almost-yes, the mirror of the wary yes (user, 2026-10-30: "não com receio seria um não só que
            // com interesse, não aceitou por causa de outra coisa (como a personalidade, humor etc)"): she wants it, and
            // something else held her back. Her wanting is measured apart from the brakes: her bars without her pace,
            // what already happened, a flirty or lonely day, a perfect day, trust; a grudge or scorn take it away.
            var desire = (h.lewdness - 40) / 60f + (h.favor - 50) / 100f;
            desire += h.isGirlfriend ? 0.15f : h.relation >= 1 ? 0.05f : h.relation < 0 ? -0.5f : 0f;
            if (h.hCount > 0) desire += 0.15f;
            if (gs.Moments.Any(m => (m.Kind == "kiss_wary" || m.Kind == "almost") && S.Day - m.Day <= 5)) desire += 0.1f;
            if (gs.Moments.Any(m => m.Kind == "feelings" && m.Tone > 0 && S.Day - m.Day <= 7)) desire += 0.05f;
            if (mood == Mood.Flirty) desire += 0.25f;
            else if (mood == Mood.Lonely) desire += 0.1f;
            if (gs.TalkDay == S.Day && gs.TalksToday >= 3) desire += 0.1f;
            desire += Mathf.Clamp((trust - 50) / 250f, -0.2f, 0.2f);
            var grudge = Social.Grudge(h) != null;
            if (grudge) { desire -= 0.3f; Brake("grudge", 0.15f); }
            if (Pairs.IsScorned(h)) desire -= 0.8f;
            _held = Brakes.Count > 0 ? Brakes.OrderByDescending(x => x.Value).First().Key : "moment";
            why += "; wants it " + desire.ToString("0.00") + (desire >= 0.5f ? ", held back by " + _held : "");
            return desire >= 0.5f ? "wary_no" : "no";
        }

        /// <summary>How many times she said no (or not now) to H today.</summary>
        public static int NoToday(SaveData.Heroine h)
        {
            int n;
            return HNoToday.TryGetValue(S.Day + "|" + GirlInfo.Key(h), out n) ? n : 0;
        }

        /// <summary>The director's H answer, fixed for the period and logged once.</summary>
        public static string DecideH(SaveData.Heroine h)
        {
            var key = S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h);
            string a;
            if (HCache.TryGetValue(key, out a)) return a;
            float c;
            string why;
            a = HAnswer(h, out c, out why);
            HCache[key] = a;
            if (a == "no" || a == "wary_no")
            {
                var k = S.Day + "|" + GirlInfo.Key(h);
                int n;
                HNoToday.TryGetValue(k, out n);
                HNoToday[k] = n + 1;
            }
            if (a == "wary_yes") WaryH[GirlInfo.Key(h)] = S.Day;
            if (a == "wary_no") HHeld[key] = _held;
            if (Logged.Add(key + "/H")) Note(GirlInfo.FirstName(h) + ": H -> " + a.Replace('_', ' ') + " (" + Mathf.RoundToInt(c * 100) + "%: " + why + ")");
            return a;
        }

        // ---------------- after a wary yes: guilt ----------------

        private static readonly Dictionary<string, int> WaryH = new Dictionary<string, int>();

        /// <summary>
        /// The H ended (Campus.OnHEnd). If she said yes warily, the guilt comes (user, 2026-10-30: "depois ela vai se
        /// sentir culpada, etc, pedir pra vc assumir ela ou tentar se afastar, e você tem que resolver os problemas
        /// todos"). Which way she goes is the director's: she asks you to take responsibility when she likes you a lot,
        /// trusts you, you have no girlfriend and it meant a lot to her; she pulls away when trust is low, you have a
        /// girlfriend, she cares about her name at school, or you barely know each other. Her scene is next time you talk.
        /// </summary>
        public static void AfterH(SaveData.Heroine h)
        {
            try
            {
                int day;
                var key = GirlInfo.Key(h);
                if (!WaryH.TryGetValue(key, out day) || S.Day - day > 1) return;
                WaryH.Remove(key);
                Guilt(h, "the H");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
        }

        /// <summary>Starts her guilt: which way she goes, and her scene next talk.</summary>
        public static int Guilt(SaveData.Heroine h, string after)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var r = new List<string>();
            var trust = Social.Trust(h);
            var stay = (h.favor - 70) / 60f + (trust - 50) / 80f;
            r.Add("favor " + h.favor);
            r.Add("trust " + trust);
            if (OtherGirlfriend(h)) { stay -= 0.4f; r.Add("you have a girlfriend"); }
            if (gs.Rep >= 70) { stay -= 0.15f; r.Add("her name at school"); }
            if (gs.TalkDays.Distinct().Count() < 5) { stay -= 0.2f; r.Add("barely know each other"); }
            if (Profile(h) == Pace.Reserved) { stay += 0.1f; r.Add("reserved: it meant a lot"); }
            if (gs.Moments.Any(m => m.Kind == "feelings" && m.Tone > 0)) { stay += 0.2f; r.Add("you talked feelings"); }
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            if (mood == Mood.Lonely) { stay += 0.1f; r.Add("lonely"); }
            else if (mood == Mood.Stressed || mood == Mood.Upset) { stay -= 0.1f; r.Add(mood.ToString().ToLowerInvariant()); }
            stay += (Dice(h, "guilt") - 0.5f) * 0.3f;
            gs.Guilt = stay >= 0f ? 1 : 2;
            gs.GuiltDay = S.Day;
            if (gs.Guilt == 2) gs.DistanceUntil = S.Day + 1;
            CampusLife.Memories.Add(h, "h_wary", "we went too far, and I let it happen", 0, true);
            gs.Pending = "SceneTalk:after_h";
            Note(GirlInfo.FirstName(h) + ": guilt after " + after + " -> " + (gs.Guilt == 1 ? "wants you to take responsibility" : "pulls away") +
                 " (" + stay.ToString("0.00") + ": " + string.Join(", ", r.ToArray()) + ")");
            return gs.Guilt;
        }

        // ================= your girlfriend finds out =================

        /// <summary>
        /// How your girlfriend takes you being sweet on another girl: "shrugs", "teases", "hurt" or "furious". Until the
        /// 2026-10-30: "assim como a traição, que se ela estiver de bom humor é mais fácil se safar do que se ela já
        /// estiver irritada, etc. tudo depende das variáveis"). Now the director reads: her tone and pace, her mood today,
        /// whether she is already angry or holds a grudge, how often she already heard about you and other girls, trust,
        /// what she decided about that girl (Share), whether that girl is her friend or her rival, how far it went and
        /// whether she saw it, plus the dice of the day. The same thing on the same day gets the same answer.
        /// </summary>
        public static string Cheat(SaveData.Heroine her, SaveData.Heroine other, int weight, bool saw, out string why)
        {
            var r = new List<string>();
            var gs = S.Girl(GirlInfo.Key(her));
            var tone = GirlInfo.ToneOf(her);
            var ease = tone == Tone.Teasing ? 0.45f : tone == Tone.Energetic ? 0.3f : 0.2f;
            r.Add(tone.ToString().ToLowerInvariant());
            var pace = Profile(her);
            if (pace == Pace.Open) { ease += 0.15f; r.Add("open"); }
            else if (pace == Pace.Reserved) { ease -= 0.05f; r.Add("reserved"); }
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            switch (mood)
            {
                case Mood.Cheerful: ease += 0.15f; r.Add("cheerful"); break;
                case Mood.Flirty: ease += 0.1f; r.Add("flirty"); break;
                case Mood.Lonely: ease -= 0.12f; r.Add("lonely"); break;
                case Mood.Stressed: ease -= 0.2f; r.Add("stressed"); break;
                case Mood.Upset: ease -= 0.3f; r.Add("upset"); break;
            }
            if (her.isAnger) { ease -= 0.4f; r.Add("already angry"); }
            else if (her.anger > 0) { ease -= her.anger / 250f; r.Add("anger " + her.anger); }
            // 108: the game died of a stack overflow on the first day change).
            if (S.Incidents.Any(i => i.Kind != Social.Kinds.Flirted && S.Day - i.Day <= 6 && gs.Known.Contains(i.Id) && !gs.Cleared.Contains(i.Id) && Social.Effect(her, i) < -1))
            { ease -= 0.15f; r.Add("a grudge"); }
            var before =S.Incidents.Count(i => i.Kind == Social.Kinds.Flirted && i.Girl != GirlInfo.Key(her) && gs.Known.Contains(i.Id) && S.Day - i.Day <= 14);
            // From the third, softer and capped.
            if (before > 2) { ease -= Mathf.Min(0.2f, 0.05f * (before - 2)); r.Add(before + " times lately"); }
            var trust = Social.Trust(her);
            ease += Mathf.Clamp((trust - 50) / 200f, -0.2f, 0.2f);
            r.Add("trust " + trust);
            if (gs.Attention < 30) { ease -= 0.1f; r.Add("feels neglected"); }
            if (other != null)
            {
                var st = Share.Stance(her, other);
                if (st == Share.Refuses) { ease -= 0.3f; r.Add("she said no to her"); }
                var bond = Social.Bond(her, other);
                if (bond <= -30) { ease -= 0.2f; r.Add("her rival"); }
                else if (bond >= 60) { ease += 0.05f; r.Add("her close friend"); }
            }
            ease -= 0.06f * weight;
            if (saw) { ease -= 0.1f; r.Add("she saw it"); }
            var score = ease + (Dice(her, "cheat:" + (other != null ? GirlInfo.Key(other) : "")) - 0.5f) * 0.3f;
            var answer = score >= 0.45f ? "shrugs" : score >= 0.2f ? "teases" : score >= -0.15f ? "hurt" : "furious";
            why = score.ToString("0.00") + ": " + string.Join(", ", r.ToArray());
            var key = S.Day + "/" + GirlInfo.Key(her) + "/cheat/" + (other != null ? GirlInfo.Key(other) : "") + "/" + weight + (saw ? "s" : "");
            if (Logged.Add(key)) Note(GirlInfo.FirstName(her) + ": you and " + (other != null ? GirlInfo.FirstName(other) : "another girl") + " -> " + answer + " (" + why + ")");
            return answer;
        }

        public static string Cheat(SaveData.Heroine her, SaveData.Heroine other, int weight, bool saw)
        {
            string why;
            return Cheat(her, other, weight, saw, out why);
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
        /// "yes", "wary", "wary_no" or "no" (<see cref="KissTier"/>). Interest is favor and H above half on a friend; then mood, trust, a grudge, who is
        /// watching and how many times you tried. A girl who says yes but is not open by nature lets it happen warily;
        /// a girlfriend and an open girl just say yes. The same try in the same period gets the same answer.
        /// </summary>
        public static string Kiss(SaveData.Heroine h, int tries, int crowd, out string why)
        {
            if (h.isAnger) { why = "angry with you"; return "no"; }
            var c = KissChance(h, tries, crowd, out why);
            var answer = KissTier(h, tries, crowd, Dice(h, "kiss" + tries) < c, ref why);
            var line = GirlInfo.FirstName(h) + ": kiss -> " + answer + " (" + why + ")";
            if (Logged.Add(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/kiss" + tries)) Note(line);
            else CampusLifePlugin.Log.LogInfo("Director decides: " + line);
            return answer;
        }

        /// <summary>The unease of a friend you met days ago: an open one says a wary yes on day 3 and a plain yes on day 4.</summary>
        private static float Soon(SaveData.Heroine h, GirlState gs, Pace pace)
        {
            if (h.isGirlfriend) return 0f;
            var days = gs.TalkDays.Distinct().Count();
            // 0.4: an open friend who trusts you said a plain yes on day 3. User: "sim receoso no 3 e sim no 4".
            return pace == Pace.Open ? (days < 4 ? 0.4f : 0f) : days < 5 ? 0.1f : 0f;
        }

        /// <summary>
        /// The kiss answer once the dice said yes or no: "yes", "wary" (a wary yes: she lets it happen), "wary_no" (16th
        /// are to her, her pace, trust, who is watching, her mood and guilt; wanting reads her bars, a flirty or lonely
        /// day, trust, a grudge, scorn and how many times you already tried.
        /// </summary>
        internal static string KissTier(SaveData.Heroine h, int tries, int crowd, bool yes, ref string why)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var pace = Profile(h);
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var trust = Social.Trust(h);
            if (yes)
            {
                var u = h.isGirlfriend ? 0f : 0.4f;
                u += pace == Pace.Reserved ? 0.2f : pace == Pace.Normal ? 0.1f : -0.2f;
                u += Soon(h, gs, pace);   // a girl met days ago lets it happen warily first
                u += trust < 40 ? 0.15f : trust >= 70 ? -0.1f : 0f;
                if (crowd >= 2) u += 0.1f;
                if (mood == Mood.Flirty) u -= 0.15f;
                if (gs.Guilt > 0) u += 0.15f;
                if (!gs.Moments.Any(m => m.Kind == "kiss_wary") && !h.isGirlfriend) u += 0.05f;   // the first one
                why += "; unease " + u.ToString("0.00");
                return u >= 0.5f ? "wary" : "yes";
            }
            var desire = (h.favor - 50) / 60f + (h.lewdness - 40) / 120f;
            desire += h.isGirlfriend ? 0.15f : h.relation >= 1 ? 0.05f : h.relation < 0 ? -0.5f : -0.1f;
            if (mood == Mood.Flirty) desire += 0.2f;
            else if (mood == Mood.Lonely) desire += 0.1f;
            desire += Mathf.Clamp((trust - 50) / 250f, -0.15f, 0.15f);
            if (Social.Grudge(h) != null) desire -= 0.3f;
            if (Pairs.IsScorned(h)) desire -= 0.8f;
            if (tries > 1) desire -= 0.2f * (tries - 1);
            why += "; wants it " + desire.ToString("0.00");
            return desire >= 0.5f ? "wary_no" : "no";
        }

        /// <summary>The kiss chance and its reasons, without asking.</summary>
        internal static float KissChance(SaveData.Heroine h, int tries, int crowd, out string why)
        {
            var r = new List<string>();
            if (h.isAnger) { why = "angry with you"; return 0f; }
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var pace = Profile(h);
            // Her pace and its multiplier count here too.
            var mult = Multiplier(h);
            var favor = h.favor * mult;
            var lewd = h.lewdness * mult;
            var interest = pace == Pace.Open ? favor > 40 && lewd > 30 : favor > 45 && lewd > 45;
            float c;
            if (h.isGirlfriend) { c = Mathf.Max(0.92f, 0.75f + (favor - 50) / 200f + (lewd - 50) / 200f); r.Add("girlfriend"); }
            else if (h.relation >= 1 && interest) { c = 0.55f + (favor - 50) / 200f + (lewd - 50) / 200f; r.Add("friend, interested"); }
            else if (h.relation >= 1) { c = 0.08f + favor / 400f + lewd / 500f; r.Add("friend"); }
            else if (h.relation == 0) { c = 0.03f + lewd / 600f; r.Add("knows you"); }
            else { c = 0f; r.Add("stranger"); }
            r.Add(pace.ToString().ToLowerInvariant());
            if (mood == Mood.Flirty) { c += 0.2f; r.Add("flirty"); }
            else if (mood == Mood.Cheerful) c += 0.05f;
            else if (mood == Mood.Upset || mood == Mood.Stressed) { c -= 0.3f; r.Add(mood.ToString().ToLowerInvariant()); }
            c += Mathf.Clamp((Social.Trust(h) - 50) / 500f, -0.1f, 0.1f);
            if (Social.Grudge(h) != null) { c -= 0.15f; r.Add("a grudge"); }
            if (gs.Guilt > 0 || gs.DistanceUntil >= S.Day) { c -= 0.25f; r.Add("still unsettled about last time"); }
            if (crowd >= 4) { c -= h.isGirlfriend ? 0.3f : 0.2f; r.Add("a crowd"); }
            if (tries > 1) { c -= 0.15f * (tries - 1); r.Add("try " + tries); }
            c = Mathf.Clamp01(c);
            why = Mathf.RoundToInt(c * 100) + "%: " + string.Join(", ", r.ToArray());
            return c;
        }

        // ================= romance and spicy lines: the same four answers =================

        /// <summary>
        /// A romance or spicy line of yours (a compliment, sweet talk, a tease, a whisper, flirting boldly, an intimate
        /// romance/spicy"): "yes", "wary_yes" (she takes it, flustered), "wary_no" (she wanted to, something held her
        /// back) or "no". The chance reads what you are to her, her pace and its multiplier, her bars, mood, trust, a
        /// grudge, scorn, guilt, who is watching and whether she already turned you down in this conversation.
        /// </summary>
        public static string Flirt(SaveData.Heroine h, bool spicy, string what, int crowd, bool refused)
        {
            string why;
            var c = FlirtChance(h, spicy, crowd, refused, out why);
            if (h.isAnger) return "no";
            var yes = Dice(h, (spicy ? "spicy:" : "romance:") + what) < c;
            var gs = S.Girl(GirlInfo.Key(h));
            var pace = Profile(h);
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var trust = Social.Trust(h);
            string a;
            if (yes)
            {
                var u = h.isGirlfriend ? 0f : spicy ? 0.35f : 0.15f;
                u += pace == Pace.Reserved ? 0.2f : pace == Pace.Normal ? 0.05f : -0.3f;
                u += trust < 40 ? 0.15f : trust >= 70 ? -0.1f : 0f;
                if (spicy && crowd >= 3) u += 0.1f;
                if (mood == Mood.Flirty) u -= 0.15f;
                why += "; unease " + u.ToString("0.00");
                a = u >= 0.5f ? "wary_yes" : "yes";
            }
            else
            {
                var desire = spicy ? (h.lewdness - 30) / 60f + (h.favor - 40) / 150f : (h.favor - 20) / 60f;
                desire += h.isGirlfriend ? 0.15f : h.relation >= 1 ? 0.05f : h.relation < 0 ? -0.5f : 0f;
                if (mood == Mood.Flirty) desire += 0.2f;
                desire += Mathf.Clamp((trust - 50) / 250f, -0.15f, 0.15f);
                if (Social.Grudge(h) != null) desire -= 0.3f;
                if (Pairs.IsScorned(h)) desire -= 0.8f;
                why += "; wants it " + desire.ToString("0.00");
                a = desire >= 0.5f ? "wary_no" : "no";
            }
            var key = S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/" + (spicy ? "spicy:" : "romance:") + what;
            if (Logged.Add(key)) Note(GirlInfo.FirstName(h) + ": " + (spicy ? "spicy " : "romance ") + what + " -> " + a.Replace('_', ' ') + " (" + why + ")");
            return a;
        }

        /// <summary>The romance or spicy chance and its reasons, without asking.</summary>
        internal static float FlirtChance(SaveData.Heroine h, bool spicy, int crowd, bool refused, out string why)
        {
            var r = new List<string>();
            if (h.isAnger) { why = "angry with you"; return 0f; }
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var pace = Profile(h);
            var mult = Multiplier(h);
            var favor = h.favor * mult;
            var lewd = h.lewdness * mult;
            float c;
            if (!spicy)
                c = h.isGirlfriend ? 0.95f : h.relation >= 1 ? 0.6f + favor / 150f : h.relation == 0 ? 0.35f + favor / 120f : 0.05f;
            else
                c = h.isGirlfriend ? 0.7f + lewd / 200f
                    : h.relation >= 1 ? 0.2f + lewd / 100f + (favor - 40f) / 200f + (pace == Pace.Open ? 0.15f : 0f)
                    : h.relation == 0 ? lewd / 120f - 0.05f + (pace == Pace.Open ? 0.15f : 0f)
                    : -0.3f;
            r.Add(h.isGirlfriend ? "girlfriend" : h.relation >= 1 ? "friend" : h.relation == 0 ? "knows you" : "stranger");
            r.Add(pace.ToString().ToLowerInvariant() + " x" + mult.ToString("0.00"));
            switch (mood)
            {
                case Mood.Flirty: c += 0.2f; r.Add("flirty"); break;
                case Mood.Cheerful: c += 0.05f; break;
                case Mood.Sleepy: c -= 0.05f; break;
                case Mood.Stressed: if (spicy) { c -= 0.15f; r.Add("stressed"); } break;
                case Mood.Upset: c -= 0.4f; r.Add("upset"); break;
            }
            c += Mathf.Clamp((Social.Trust(h) - 50) / 250f, -0.1f, 0.1f);
            if (Social.Grudge(h) != null) { c -= 0.15f; r.Add("a grudge"); }
            if (Pairs.IsScorned(h)) { c -= 0.5f; r.Add("scorned"); }
            if (spicy && (gs.Guilt > 0 || gs.DistanceUntil >= S.Day)) { c -= 0.2f; r.Add("still unsettled about last time"); }
            if (spicy && crowd >= 3 && lewd < 60 && !GirlInfo.Quirks(h).bitch) { c -= 0.15f; r.Add("a crowd"); }
            if (refused) { c -= 0.3f; r.Add("turned you down already"); }
            c = Mathf.Clamp01(c);
            why = Mathf.RoundToInt(c * 100) + "%: " + string.Join(", ", r.ToArray());
            return c;
        }

        /// <summary>Holding hands in the conversation: the hold-hands chance, then the touch tiers of the hand.</summary>
        public static string HoldHand(SaveData.Heroine h, int crowd)
        {
            string why;
            var c = Chance(h, Ask.HoldHand, out why);
            var a = h.isAnger ? "no" : TouchTier(h, "hand", false, 1, crowd, Dice(h, Ask.HoldHand.ToString()) < c, ref why);
            if (Logged.Add(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/" + Ask.HoldHand)) Note(GirlInfo.FirstName(h) + ": hold hands -> " + a.Replace('_', ' ') + " (" + Mathf.RoundToInt(c * 100) + "%: " + why + ")");
            return a;
        }

        /// <summary>What the director would answer now, without logging it as a decision.</summary>
        public static bool WouldSay(SaveData.Heroine h, Ask ask)
        {
            if (ask == Ask.H) { float c; string w; var a = HAnswer(h, out c, out w); return a == "yes" || a == "wary_yes"; }
            string why;
            return Dice(h, ask.ToString()) < Chance(h, ask, out why);
        }

        public static bool Decide(SaveData.Heroine h, Ask ask)
        {
            if (ask == Ask.H) { var a = DecideH(h); return a == "yes" || a == "wary_yes"; }
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
                var part = !_touch ? (_parts == 1 ? "chest" : "face") : _parts == 1 ? "chest" : _parts == 2 ? "head" : _parts == 3 ? "cheek" : "hand";
                float c;
                string why;
                var answer = TouchAnswer(h, part, !_touch, n, Conversation.Onlookers(h).Count, out c, out why);
                LastTouch = answer;
                // The game's three reactions: welcome, uneasy (she lets it happen, or almost does) and "stop it".
                var level = answer == "yes" ? 0 : answer == "no" ? 2 : 1;
                var line = GirlInfo.FirstName(h) + ": touch " + (_touch ? "" : "(look) ") + part + " #" + n + " -> " + answer.Replace('_', ' ') + " (the game rolled " + _stage + "; " + why + ")";
                if (Logged.Add(S.Day + "/" + (int)G.CurrentMask + "/" + GirlInfo.Key(h) + "/touch" + key + "/" + n)) Note(line);
                _stage = level;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Director answers (touch): " + e.Message); }
        }

        /// <summary>The answer to the game's last touch (yes, wary_yes, wary_no, no), for her line.</summary>
        internal static string LastTouch = "";

        // ================= touches: the same four answers =================

        /// <summary>
        /// cadê o diretor onipotente que eu sempre falo?"). Every place (hand, head, cheek, chest, butt, a look) gets the
        /// four answers of H and the kiss: the chance reads what you are to her, her pace and its multiplier, her bars,
        /// how intimate the place is, whether you only look, her mood, trust, a grudge, scorn, guilt still open, a recent
        /// kiss, who is watching and how many times you already touched it; the dice say yes or no; her unease turns a
        /// yes into a wary yes and her wanting turns a no into a wary no. The same touch in the same period gets the
        /// same answer.
        /// </summary>
        public static string TouchAnswer(SaveData.Heroine h, string part, bool look, int n, int crowd, out float chance, out string why)
        {
            chance = TouchChance(h, part, look, n, crowd, out why);
            if (h.isAnger) return "no";
            var yes = Dice(h, "touch:" + part + (look ? ":look" : "") + ":" + n) < chance;
            return TouchTier(h, part, look, n, crowd, yes, ref why);
        }

        private static bool Intimate(string part) { return part == "chest" || part == "butt"; }

        /// <summary>How many touches of a place feel natural before more is insisting.</summary>
        private static int Comfort(SaveData.Heroine h, bool intimate)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var c = h.isGirlfriend ? (intimate ? 3 : 6) : h.relation >= 1 ? (intimate ? 1 : 3) : 1;
            if (gs.MoodDay == S.Day && gs.Mood == Mood.Flirty) c++;
            if (Social.Trust(h) >= 75) c++;
            return c;
        }

        /// <summary>The touch chance (0-1) and its reasons, without asking.</summary>
        internal static float TouchChance(SaveData.Heroine h, string part, bool look, int n, int crowd, out string why)
        {
            var r = new List<string>();
            if (h.isAnger) { why = "angry with you"; return 0f; }
            var gs = S.Girl(GirlInfo.Key(h));
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var pace = Profile(h);
            var mult = Multiplier(h);
            var favor = h.favor * mult;
            var lewd = h.lewdness * mult;
            var intimate = Intimate(part);
            float c;
            if (!intimate)
                c = (h.isGirlfriend ? 0.95f : h.relation >= 1 ? 0.5f + favor / 150f : h.relation == 0 ? 0.2f + favor / 200f : 0.03f) - (part == "cheek" || part == "face" ? 0.05f : 0f);
            else
            {
                var butt = part == "butt" ? 0.08f : 0f;   // a little less personal than the chest
                if (h.isGirlfriend) c = 0.55f + lewd / 150f + butt;
                else if (h.relation >= 1) c = (lewd - (pace == Pace.Open ? 25f : pace == Pace.Normal ? 32f : 40f)) / 50f + (favor - 50f) / 150f + butt + (pace == Pace.Open ? 0.15f : 0f);
                else if (h.relation == 0) c = (pace == Pace.Open ? lewd / 200f - 0.15f : -0.35f) + butt;
                else c = -0.5f;
            }
            r.Add(h.isGirlfriend ? "girlfriend" : h.relation >= 1 ? "friend" : h.relation == 0 ? "knows you" : "stranger");
            r.Add(pace.ToString().ToLowerInvariant() + " x" + mult.ToString("0.00"));
            if (look) { c += 0.15f; r.Add("only looking"); }
            switch (mood)
            {
                case Mood.Flirty: c += intimate ? 0.2f : 0.1f; r.Add("flirty"); break;
                case Mood.Cheerful: c += 0.05f; break;
                case Mood.Lonely: if (!intimate) { c += 0.05f; r.Add("lonely"); } break;
                case Mood.Sleepy: c -= 0.05f; break;
                case Mood.Stressed: c -= 0.15f; r.Add("stressed"); break;
                case Mood.Upset: c -= 0.4f; r.Add("upset"); break;
            }
            c += Mathf.Clamp((Social.Trust(h) - 50) / 250f, -0.1f, 0.1f);
            if (Social.Grudge(h) != null) { c -= 0.15f; r.Add("a grudge"); }
            if (Pairs.IsScorned(h)) { c -= 0.5f; r.Add("scorned"); }
            if (gs.Guilt > 0 || gs.DistanceUntil >= S.Day) { c -= 0.2f; r.Add("still unsettled about last time"); }
            if (intimate && gs.Moments.Any(m => (m.Kind == "kiss_wary" || m.Kind == "almost") && S.Day - m.Day <= 5)) { c += 0.08f; r.Add("a recent kiss or almost"); }
            // Who is watching: a crowd makes her shy unless she enjoys the show; alone she lets you closer.
            if (crowd >= 3 && lewd < 60 && !GirlInfo.Quirks(h).bitch) { c -= intimate ? 0.25f : 0.1f; r.Add("a crowd"); }
            else if (crowd == 0 && h.relation >= 1) { c += 0.05f; r.Add("alone"); }
            var comfort = Comfort(h, intimate);
            if (n > comfort) { c -= (intimate ? 0.15f : 0.1f) * (n - comfort); r.Add("touch #" + n); }
            c = Mathf.Clamp01(c);
            why = Mathf.RoundToInt(c * 100) + "%: " + string.Join(", ", r.ToArray());
            return c;
        }

        /// <summary>The touch answer once the dice said yes or no.</summary>
        internal static string TouchTier(SaveData.Heroine h, string part, bool look, int n, int crowd, bool yes, ref string why)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            var pace = Profile(h);
            var mood = gs.MoodDay == S.Day ? gs.Mood : Mood.None;
            var trust = Social.Trust(h);
            var intimate = Intimate(part);
            var comfort = Comfort(h, intimate);
            if (yes)
            {
                var u = h.isGirlfriend ? 0f : intimate ? 0.45f : 0.2f;
                u += pace == Pace.Reserved ? 0.2f : pace == Pace.Normal ? 0.05f : -0.2f;
                if (intimate) u += Soon(h, gs, pace);
                u += trust < 40 ? 0.15f : trust >= 70 ? -0.1f : 0f;
                if (crowd >= 3) u += 0.15f;
                if (n > comfort) u += 0.15f;
                if (look) u -= 0.1f;
                if (mood == Mood.Flirty) u -= 0.15f;
                else if (mood == Mood.Stressed || mood == Mood.Upset) u += 0.1f;
                if (gs.Guilt > 0) u += 0.15f;
                why += "; unease " + u.ToString("0.00");
                return u >= 0.5f ? "wary_yes" : "yes";
            }
            var desire = intimate ? (h.lewdness - 40) / 60f + (h.favor - 50) / 120f : (h.favor - 30) / 60f;
            desire += h.isGirlfriend ? 0.15f : h.relation >= 1 ? 0.05f : h.relation < 0 ? -0.5f : 0f;
            if (mood == Mood.Flirty) desire += 0.2f;
            else if (mood == Mood.Lonely && !intimate) desire += 0.1f;
            desire += Mathf.Clamp((trust - 50) / 250f, -0.15f, 0.15f);
            if (Social.Grudge(h) != null) desire -= 0.3f;
            if (Pairs.IsScorned(h)) desire -= 0.8f;
            if (n > comfort + 1) desire -= 0.15f * (n - comfort - 1);   // insisting turns an almost into a no
            why += "; wants it " + desire.ToString("0.00");
            return desire >= 0.5f ? "wary_no" : "no";
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
