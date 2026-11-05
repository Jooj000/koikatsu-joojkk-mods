using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame;
using ActionGame.Chara;
using KKAPI.MainGame;
using KKAPI.Utilities;
using Manager;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>Safe access to the running main game.</summary>
    internal static class G
    {
        public static bool HasGame { get { return Singleton<Game>.IsInstance() && Singleton<Game>.Instance.saveData != null; } }

        public static ActionScene Act
        {
            get { return Singleton<Game>.IsInstance() ? Singleton<Game>.Instance.actScene : null; }
        }

        public static Cycle Cycle
        {
            get { var a = Act; return a != null ? a.Cycle : null; }
        }

        public static SaveData.Player Player
        {
            get { return HasGame ? Singleton<Game>.Instance.Player : null; }
        }

        public static List<SaveData.Heroine> Heroines
        {
            get { return HasGame ? Singleton<Game>.Instance.HeroineList : new List<SaveData.Heroine>(); }
        }

        public static Cycle.Type? Period
        {
            get { var c = Cycle; return c != null ? (Cycle.Type?)c.nowType : null; }
        }

        public static Cycle.Week? Weekday
        {
            get { var c = Cycle; return c != null ? (Cycle.Week?)c.nowWeek : null; }
        }

        public static PeriodMask ToMask(Cycle.Type t)
        {
            switch (t)
            {
                case Cycle.Type.LunchTime: return PeriodMask.Lunch;
                case Cycle.Type.StaffTime: return PeriodMask.Club;
                case Cycle.Type.AfterSchool: return PeriodMask.After;
                default: return PeriodMask.None;
            }
        }

        public static PeriodMask CurrentMask
        {
            get { var p = Period; return p.HasValue ? ToMask(p.Value) : PeriodMask.None; }
        }

        public static bool IsSchoolDay
        {
            get { var w = Weekday; return w.HasValue && w.Value != Cycle.Week.Holiday; }
        }

        /// <summary>Walking around the school in one of the three free periods.</summary>
        public static bool InRoamingPeriod
        {
            get { return Act != null && CurrentMask != PeriodMask.None && IsSchoolDay; }
        }

        /// <summary>Free roaming with nothing else on screen: safe to start an event or open a menu.</summary>
        public static bool IsFreeRoaming
        {
            get
            {
                try
                {
                    var act = Act;
                    if (act == null || !act.isActiveAndEnabled) return false;
                    if (!InRoamingPeriod || GameAPI.InsideHScene) return false;
                    if (act.isEventNow) return false;
                    // F8 (Cycle.ActionEnd) puts the clock at 500 and the period only changes a few frames later: a scene
                    // opened in between stayed under the period's white fade for good.
                    var cy = Cycle;
                    if (cy != null && cy.isActionEnd) return false;
                    if (act.Map == null || act.Map.isMapLoading) return false;
                    if (!Singleton<Scene>.IsInstance()) return false;
                    var sc = Singleton<Scene>.Instance;
                    if (sc.IsNowLoading || sc.IsNowLoadingFade || sc.IsFadeNow) return false;
                    if (!string.IsNullOrEmpty(sc.AddSceneName)) return false;
                    if (TalkSceneOpen) return false;
                    var adv = act.AdvScene;
                    if (adv != null && adv.isActiveAndEnabled) return false;
                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>Why <see cref="IsFreeRoaming"/> is false right now.</summary>
        public static string RoamWhy()
        {
            var why = new List<string>();
            try
            {
                var act = Act;
                if (act == null || !act.isActiveAndEnabled) return "no action scene";
                if (!InRoamingPeriod) why.Add("not a roaming period (mask " + CurrentMask + ", college days " + IsSchoolDay + ")");
                if (GameAPI.InsideHScene) why.Add("inside an H scene");
                if (act.isEventNow) why.Add("isEventNow");
                var cy = Cycle;
                if (cy != null && cy.isActionEnd) why.Add("Cycle.isActionEnd");
                if (act.Map == null) why.Add("no map"); else if (act.Map.isMapLoading) why.Add("map loading");
                if (Singleton<Scene>.IsInstance())
                {
                    var sc = Singleton<Scene>.Instance;
                    if (sc.IsNowLoading) why.Add("scene loading");
                    if (sc.IsNowLoadingFade) why.Add("loading fade");
                    if (sc.IsFadeNow) why.Add("fading");
                    if (!string.IsNullOrEmpty(sc.AddSceneName)) why.Add("add scene " + sc.AddSceneName);
                }
                if (TalkSceneOpen) why.Add("talk scene open");
                var adv = act.AdvScene;
                if (adv != null && adv.isActiveAndEnabled) why.Add("ADV scene active");
            }
            catch (Exception e) { why.Add("error " + e.Message); }
            return why.Count == 0 ? "free" : string.Join("; ", why.ToArray());
        }

        private static float _talkCheckTime;
        private static TalkScene _talk;
        private static int _talkHooked; // 0 = not checked yet, 1 = Hooks.TalkSceneAwake is installed, -1 = it is not

        /// <summary>
        /// The open talk scene, or null. KKAPI's GameAPI.GetTalkScene is a FindObjectOfType over every object of the
        /// scene (about 54k on a busy map, ~7 ms). Hooks.TalkSceneAwake hands every new talk scene over, so no search
        /// is needed; only if that patch is missing the search runs, twice a second at most.
        /// </summary>
        public static TalkScene TalkScene
        {
            get
            {
                if (_talk != null && _talk.isActiveAndEnabled) return _talk; // FindObjectOfType skipped inactive ones too
                if (_talkHooked == 0) _talkHooked = AwakeHooked() ? 1 : -1;
                if (_talkHooked > 0 || Time.realtimeSinceStartup - _talkCheckTime < 0.5f) return null;
                _talkCheckTime = Time.realtimeSinceStartup;
                _talk = UnityEngine.Object.FindObjectOfType<TalkScene>();
                return _talk;
            }
        }

        internal static void OnTalkSceneAwake(TalkScene scene) { _talk = scene; }

        private static bool AwakeHooked()
        {
            try
            {
                var info = HarmonyLib.Harmony.GetPatchInfo(HarmonyLib.AccessTools.Method(typeof(TalkScene), "Awake"));
                return info != null && info.Postfixes.Any(p => p.owner == CampusLifePlugin.GUID);
            }
            catch { return false; }
        }

        public static bool TalkSceneOpen { get { return TalkScene != null; } }

        public static int PlayerMapNo
        {
            get
            {
                var act = Act;
                if (act == null || act.Map == null) return -1;
                return act.Map.no;
            }
        }

        public static string MapName(int mapNo)
        {
            var act = Act;
            if (act == null || act.Map == null || act.Map.infoDic == null) return "";
            MapInfo.Param p;
            return act.Map.infoDic.TryGetValue(mapNo, out p) && p != null ? p.MapName ?? "" : "";
        }

        public static void AddPeriodSeconds(float seconds)
        {
            var c = Cycle;
            if (c == null || seconds <= 0f) return;
            c.AddTimer(seconds / Cycle.TIME_LIMIT);
        }

        /// <summary>
        /// Longer periods: the game adds Time.deltaTime to the period clock each frame while you walk around (cursor
        /// locked, nothing regulating). With a multiplier above 1 part of it is given back, every frame from Update.
        /// </summary>
        public static void StretchPeriod()
        {
            var m = Cfg.PeriodLength.Value;
            if (m <= 1.001f) return;
            var act = Act;
            var c = Cycle;
            if (act == null || c == null || !c.isAction || !act.isCursorLock) return;
            if (!Singleton<Game>.IsInstance() || Singleton<Game>.Instance.IsRegulate(true)) return;
            if (c.timer <= 0f || c.timer >= Cycle.TIME_LIMIT) return;
            c.AddTimer(-Time.deltaTime * (1f - 1f / m) / Cycle.TIME_LIMIT);
        }

        public static float PeriodSecondsLeft
        {
            get { var c = Cycle; return c == null ? 0f : Mathf.Max(0f, Cycle.TIME_LIMIT - c.timer); }
        }

        // ---- player stats (0..100) ----
        public static int Str { get { var p = Player; return p == null ? 0 : p.physical; } }
        public static int Int { get { var p = Player; return p == null ? 0 : p.intellect; } }
        public static int Sens { get { var p = Player; return p == null ? 0 : p.hentai; } }

        public static void AddStats(int intel, int str, int sens)
        {
            var p = Player;
            if (p == null) return;
            p.intellect = Mathf.Clamp(p.intellect + intel, 0, 100);
            p.physical = Mathf.Clamp(p.physical + str, 0, 100);
            p.hentai = Mathf.Clamp(p.hentai + sens, 0, 100);
        }

        public static void AddFavor(SaveData.Heroine h, int favor, int lewd)
        {
            if (h == null) return;
            var before = h.favor;
            if (favor > 0) favor = Daily(h, "fday:", favor, 8f);
            if (lewd > 0) lewd = Daily(h, "hday:", lewd, 6f);
            h.favor = Mathf.Clamp(h.favor + favor, 0, 100);
            Chat.NoteFavor(h, h.favor - before);
            // The club report counts "contact" from favor won in the game's own talk (Communication.ChangeParam: favor x2,
            // x4 for a club member); ours changed favor directly and the report stayed at 0.
            if (h.favor > before && HasGame)
                try { Singleton<Game>.Instance.saveData.clubReport.comAdd = (h.favor - before) * (h.isStaff ? 4 : 2); } catch { }
            h.lewdness = Mathf.Clamp(h.lewdness + lewd, 0, 100);
        }

        /// <summary>
        /// Favor and H won from one girl in one day have diminishing returns (user, 2026-10-28: "tá dando pra namorar
        /// qualquer uma em um dia só"; 2026-10-30: "balancear direito o afeto para não encher tão rápido ... não colocar
        /// um limite artificial de piso de dias"): the first 8 favor points of the day (6 of H) count whole, as many again
        /// at half, then a tenth; the steps grow with her pace (Consent.Multiplier: open x1.4, reserved from x0.7). So a
        /// well-played day fills ~20 favor for a normal girl, and the days to date her come from the bar, not from a floor.
        /// A part of a point is paid by chance (2026-11-07), so a beat late in the day often pays nothing.
        /// Losses always count. Kept in the save (Buffs "fday:girl" / "hday:girl" = day x 1000 + points won).
        /// </summary>
        private static int Daily(SaveData.Heroine h, string tag, int add, float step)
        {
            var s = Campus.S;
            if (s == null) return add;
            var k = tag + GirlInfo.Key(h);
            int v;
            var won = s.Buffs.TryGetValue(k, out v) && v / 1000 == s.Day ? v % 1000 : 0;
            float m = 1f;
            try { m = Consent.Multiplier(h); } catch { }
            var full = step * m;
            float got = 0f;
            for (var i = 0; i < add; i++)
            {
                var n = won + i;
                got += n < full ? 1f : n < 2f * full ? 0.5f : 0.1f;
            }
            // Past the curve a beat pays a fraction of a point; it rounds by chance instead of up to 1, so many small beats
            // in one day no longer fill the bar (2026-11-07: "dava pra repetir sweet talk e afins até acabar o tempo e
            // farmar favor e H").
            // The part of a point carries to the next beat of the day (2026-11-07: context, not chance): three tenths
            // pay nothing, the tenth tenth pays one. Kept in Buffs "frac:" + tag + girl = day x 1000 + hundredths.
            var fk = "frac:" + k;
            int fv;
            var carry = s.Buffs.TryGetValue(fk, out fv) && fv / 1000 == s.Day ? (fv % 1000) / 100f : 0f;
            var sum = got + carry;
            var paid = Mathf.FloorToInt(sum + 0.001f);
            s.Buffs[fk] = s.Day * 1000 + Mathf.Clamp(Mathf.RoundToInt((sum - paid) * 100f), 0, 99);
            s.Buffs[k] = s.Day * 1000 + Math.Min(999, won + add);
            if (paid < add) CampusLifePlugin.Log.LogInfo((tag == "fday:" ? "Favor" : "H") + " today: " + GirlInfo.FirstName(h) + " " + won + " won, +" + add + " pays +" + paid);
            return paid;
        }

        public static void AddClubPoints(int n)
        {
            if (!HasGame || n <= 0) return;
            try { Singleton<Game>.Instance.saveData.ClubPointAdd(n); } catch { }
        }
    }

    /// <summary>Identity, names, tone and profile of a heroine.</summary>
    internal static class GirlInfo
    {
        private static readonly Dictionary<string, string> NameCache = new Dictionary<string, string>();

        public static string Key(SaveData.Heroine h)
        {
            if (h == null) return "";
            if (h.fixCharaID != 0) return "fix:" + h.fixCharaID;
            var p = h.parameter;
            return "c:" + p.lastname + "|" + p.firstname + "|" + p.personality + "|" + p.birthMonth + "-" + p.birthDay;
        }

        public static SaveData.Heroine Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            foreach (var h in G.Heroines)
                if (h != null && Key(h) == key) return h;
            return null;
        }

        private static string Translate(string jp)
        {
            if (string.IsNullOrEmpty(jp)) return "";
            string cached;
            if (NameCache.TryGetValue(jp, out cached)) return cached;
            var result = jp;
            // Only Japanese needs translating. AutoTranslator's sync lookup can hand back its name templates unfilled
            // ("Migurdia Roxy" -> "{{A}} Roxy"), so plain ASCII stays as is and a result with a placeholder is ignored.
            if (jp.All(ch => ch < 128)) { NameCache[jp] = jp; return jp; }
            try
            {
                string tl;
                if (TranslationHelper.TryTranslate(jp, out tl) && !string.IsNullOrEmpty(tl) && tl.IndexOf("{{", StringComparison.Ordinal) < 0) result = tl;
            }
            catch { }
            // Only cache when it actually changed or is plain ASCII, so async translations can still land later.
            if (result != jp || jp.All(ch => ch < 128)) NameCache[jp] = result;
            return result;
        }

        public static string FirstName(SaveData.Heroine h)
        {
            if (h == null) return "?";
            // Some cards hold a blank first name (Ozen: a lone space), which printed "Greyrat + : close friends" in the director's log.
            var n = Translate(h.parameter.firstname);
            return string.IsNullOrEmpty(n) || n.Trim().Length == 0 ? (Translate(h.parameter.lastname) ?? "").Trim() : n;
        }

        public static string FullName(SaveData.Heroine h)
        {
            if (h == null) return "?";
            var full = (Translate(h.parameter.firstname) + " " + Translate(h.parameter.lastname)).Trim();
            return string.IsNullOrEmpty(full) ? "?" : full;
        }

        public static string FirstName(string key) { return FirstName(Find(key)); }

        public static bool IsStudent(SaveData.Heroine h) { return h != null && !h.isTeacher; }

        /// <summary>A student the everyday systems can use (talk, requests, arcs): not a teacher, not a story heroine.</summary>
        public static bool IsRegular(SaveData.Heroine h) { return h != null && !h.isTeacher && h.fixCharaID == 0; }
        public static bool IsTeacher(SaveData.Heroine h) { return h != null && h.isTeacher; }

        // ---- personality tone ----
        private static readonly string[] Teasing = { "セクシー", "Ｓ", "S気", "Sッ", "不良", "姉御", "勝ち気", "勝気", "意地っ張り", "ヤンデレ", "艶やか", "小悪魔", "ビッチ" };
        private static readonly string[] Cool = { "クール", "無感情", "お嬢", "高飛車", "意識高い", "ミステリ", "邪気眼", "ものぐさ", "怠惰", "ひねくれ", "無口", "高潔", "几帳面" };
        private static readonly string[] Energetic = { "ギャル", "気さく", "ミーハー", "ワイルド", "野性", "野生", "単純", "チョロ", "方言", "帰国", "電波", "ボーイッシュ", "天真爛漫", "ノリノリ", "ボクっ娘", "元気" };
        private static readonly string[] Gentle = { "大和撫子", "純真", "母性", "文学", "モジモジ", "もじもじ", "誠実", "正統派", "後輩", "不幸", "素直" };

        public static string PersonalityName(SaveData.Heroine h)
        {
            if (h == null || !Singleton<Game>.IsInstance()) return "";
            try
            {
                string name;
                if (Singleton<Game>.Instance.HeroinePersonalitys.TryGetValue(h.personality, out name)) return name ?? "";
            }
            catch { }
            return "";
        }

        public static string PersonalityNameEn(SaveData.Heroine h)
        {
            var jp = PersonalityName(h);
            return string.IsNullOrEmpty(jp) ? "?" : Translate(jp);
        }

        public static Tone ToneOf(SaveData.Heroine h)
        {
            if (h == null) return Tone.Gentle;
            switch (h.fixCharaID)
            {
                case -10: return Tone.Gentle;    // Ai Minase, classic heroine
                case -9: return Tone.Teasing;    // Sakura Yuuki, tough girl
                case -5: return Tone.Energetic;  // Mai Himekawa, idol
                case -8: return Tone.Gentle;     // Konomi Hiiragi, underclassman
            }
            var name = PersonalityName(h);
            if (!string.IsNullOrEmpty(name))
            {
                if (Teasing.Any(name.Contains)) return Tone.Teasing;
                if (Cool.Any(name.Contains)) return Tone.Cool;
                if (Energetic.Any(name.Contains)) return Tone.Energetic;
                if (Gentle.Any(name.Contains)) return Tone.Gentle;
            }
            return (Tone)(Math.Abs(h.personality) % 4);
        }

        // ---- club ----
        public static string ClubName(SaveData.Heroine h)
        {
            if (h == null) return "";
            try
            {
                ClubInfo.Param p;
                if (Game.ClubInfos.TryGetValue(h.clubActivities, out p) && p != null) return Translate(p.Name);
            }
            catch { }
            return "";
        }

        public static bool InSportsClub(SaveData.Heroine h)
        {
            if (h == null) return false;
            try
            {
                ClubInfo.Param p;
                return Game.ClubInfos.TryGetValue(h.clubActivities, out p) && p != null && p.isSports;
            }
            catch { return false; }
        }

        public static bool InNoClub(SaveData.Heroine h) { return h != null && h.clubActivities == 0; }

        /// <summary>She joined your Koikatsu Club (the game's staff): club time is in the club room with you now.</summary>
        public static bool InKoikatsuClub(SaveData.Heroine h) { return h != null && h.isStaff; }

        /// <summary>
        /// Her own club as a word for the dialogue rules ("swimming", "track_field", "tea_ceremony"...), from the game's
        /// club name; "none" for the go-home club.
        /// </summary>
        public static string ClubId(SaveData.Heroine h)
        {
            if (h == null || InNoClub(h)) return "none";
            string raw = "";
            try { ClubInfo.Param p; if (Game.ClubInfos.TryGetValue(h.clubActivities, out p) && p != null) raw = p.Name ?? ""; }
            catch { }
            // The game's own (Japanese) names, so the word does not depend on the translation.
            if (raw.Contains("水泳")) return "swim";
            if (raw.Contains("漫")) return "manga";
            if (raw.Contains("茶道")) return "tea_ceremony";
            if (raw.Contains("陸上")) return "track_field";
            if (raw.Contains("チア")) return "cheerleading";
            return "club" + h.clubActivities;
        }

        // ---- likes (from the character profile) ----
        public static readonly string[] LikeIds = { "animals", "food", "cooking", "exercise", "study", "fashion", "coffee", "spicy", "sweets" };
        public static readonly string[] LikeLabels = { "animals", "eating", "cooking", "exercise", "studying", "fashion", "black coffee", "spicy food", "sweets" };

        public static bool Likes(SaveData.Heroine h, string likeId)
        {
            if (h == null) return false;
            var a = h.parameter.awnser;
            switch (likeId)
            {
                case "animals": return a.animal;
                case "food": return a.eat;
                case "cooking": return a.cook;
                case "exercise": return a.exercise;
                case "study": return a.study;
                case "fashion": return a.fashionable;
                case "coffee": return a.blackCoffee;
                case "spicy": return a.spicy;
                case "sweets": return a.sweet;
            }
            return false;
        }

        public static List<string> LikesOf(SaveData.Heroine h)
        {
            return LikeIds.Where(id => Likes(h, id)).ToList();
        }

        public static string LikeLabel(string likeId)
        {
            var i = Array.IndexOf(LikeIds, likeId);
            return i >= 0 ? LikeLabels[i] : likeId;
        }

        public static ChaFileParameter.Attribute Quirks(SaveData.Heroine h)
        {
            return h == null ? new ChaFileParameter.Attribute() : CardTraits.Of(h);
        }

        public static string RelationLabel(SaveData.Heroine h)
        {
            if (h == null) return "";
            if (h.isGirlfriend) return Gender.IsBoy(h) ? "Boyfriend" : "Girlfriend";
            switch (h.relation)
            {
                case 1: return "Friend";
                case 0: return "Acquaintance";
                default: return "Stranger";
            }
        }

        /// <summary>
        /// Her NPC of this period, from the scene's own list:
        /// h.charaBase is not cleared when the game hands her NPC to another girl at a reshuffle, so it could name someone
        /// else's map, and a girl on another map has her NPC switched off, which read as "not on campus".
        /// </summary>
        private static ActionGame.Chara.NPC NpcNow(SaveData.Heroine h)
        {
            try
            {
                var act = G.Act;
                if (h == null || act == null || act.npcList == null) return null;
                foreach (var n in act.npcList) if (n != null && n.heroine == h) return n;
            }
            catch { }
            return null;
        }

        public static int CurrentMap(SaveData.Heroine h)
        {
            try
            {
                if (h == null) return -1;
                if (G.Act != null) { var n = NpcNow(h); return n != null ? n.mapNo : -1; }
                return h.charaBase == null ? -1 : h.charaBase.mapNo;
            }
            catch { return -1; }
        }

        /// <summary>She is at school this period (her NPC is loaded and on a map), on any map.</summary>
        public static bool IsSpawned(SaveData.Heroine h)
        {
            try
            {
                if (h == null) return false;
                if (G.Act != null) { var n = NpcNow(h); return n != null && n.mapNo >= 0; }
                return h.charaBase != null && h.charaBase.isActiveAndEnabled;
            }
            catch { return false; }
        }
    }
}
