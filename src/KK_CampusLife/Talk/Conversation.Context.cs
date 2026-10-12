using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// ex-professora, ex-vanilla, mood, local, período, clube, de personalidade, de corpo, signo, aniversário"): the
    /// context keys the pieces of pieces.dlg read, and the composition of a line from an opener, her line and a tail.
    /// </summary>
    internal static partial class Conversation
    {
        /// <summary>What you would do together for each like (the invite says "go to the cat café", not "do animals").</summary>
        private static readonly Dictionary<string, string[]> LikeDo = new Dictionary<string, string[]>
        {
            { "animals", new[] { "visit the cat café", "feed the stray cats behind the gym", "go to the pet shop" } },
            { "food", new[] { "grab something to eat", "try the new crepe stand", "eat out after class" } },
            { "cooking", new[] { "cook something together", "bake something in the club kitchen", "trade bento recipes" } },
            { "exercise", new[] { "go for a run", "shoot some hoops", "jog around the field" } },
            { "study", new[] { "study at the library", "do assignments together", "quiz each other" } },
            { "fashion", new[] { "go clothes shopping", "look at the new shop by the station", "browse some fashion magazines" } },
            { "coffee", new[] { "get coffee", "try the café by the station", "grab a black coffee" } },
            { "spicy", new[] { "try the spicy ramen place", "do the hot curry challenge", "get extra-spicy takoyaki" } },
            { "sweets", new[] { "get sweets", "share a parfait", "go to the cake shop" } },
        };

        private static readonly string[] Signs = { "capricorn", "aquarius", "pisces", "aries", "taurus", "gemini", "cancer", "leo", "virgo", "libra", "scorpio", "sagittarius", "capricorn" };
        private static readonly int[] SignStart = { 20, 19, 21, 20, 21, 21, 23, 23, 23, 23, 22, 22 };

        public static string SignOf(int month, int day)
        {
            if (month < 1 || month > 12) return "";
            return day < SignStart[month - 1] ? Signs[month - 1] : Signs[month];
        }

        /// <summary>
        /// The game has no calendar date: the 9-week loop of school events stands for a year (Chocolate Day in February,
        /// Tanabata in July, Halloween in October, Christmas Eve in December), so each loop week is a month.
        /// </summary>
        private static readonly int[] MonthOfLoopWeek = { 12, 2, 4, 6, 7, 9, 10, 11, 1 };

        public static int MonthNow { get { return MonthOfLoopWeek[Calendar.LoopWeek]; } }

        /// <summary>"today" (her week, her weekday), "week" (her month this week), "soon" (next week), or "no".</summary>
        public static string BirthdayOf(SaveData.Heroine h)
        {
            try
            {
                var p = h.charFile.parameter;
                var month = p.birthMonth;
                if (month < 1) return "no";
                if (month == MonthNow)
                {
                    var wd = G.Weekday.HasValue ? (int)G.Weekday.Value : 0;
                    return p.birthDay % 7 == wd ? "today" : "week";
                }
                return month == MonthOfLoopWeek[(Calendar.LoopWeek + 1) % MonthOfLoopWeek.Length] ? "soon" : "no";
            }
            catch { return "no"; }
        }

        /// <summary>Romantically into you: your girlfriend or lover, a close friend past 70 favor, or a crush on you in the lore.</summary>
        public static bool Interested(SaveData.Heroine h)
        {
            if (h == null) return false;
            if (h.isGirlfriend || h.relation >= 2) return true;
            if (h.relation >= 1 && h.favor >= 70) return true;
            var key = GirlInfo.Key(h);
            return S.Lore != null && S.Lore.Any(f => f.Verb == "crush" && f.Subject == key && f.Obj == Lore.Player);
        }

        /// <summary>How much she wants another girl (her side of the pair), 0..100.</summary>
        public static int RomanceFor(SaveData.Heroine h, SaveData.Heroine other)
        {
            var p = Pairs.Of(h, other);
            if (p == null) return 0;
            return p.A == GirlInfo.Key(h) ? p.RomAB : p.RomBA;
        }

        private static void MoreCtx(SaveData.Heroine h, GirlState gs, Dictionary<string, string> c)
        {
            try
            {
                var story = h.fixCharaID <= -5;
                var grad = StoryCast.Graduated(h);
                c["role"] = Teachers.IsTeacherVoice(h) ? "teacher" : story ? (grad ? "exstory" : "story") : "student";
                c["free"] = Teachers.Freed(h) ? "yes" : "no";
                c["job"] = Teachers.Job(h);
                var p = h.charFile.parameter;
                var sign = SignOf(p.birthMonth, p.birthDay);
                if (sign != "") c["sign"] = sign;
                c["birthday"] = BirthdayOf(h);
                var body = h.charFile.custom.body.shapeValueBody;
                var height = body[(int)ChaFileDefine.BodyShapeIdx.Height];
                var bust = body[(int)ChaFileDefine.BodyShapeIdx.BustSize];
                c["height"] = height >= 0.66f ? "tall" : height <= 0.36f ? "short" : "average";
                c["bust"] = bust >= 0.7f ? "big" : bust <= 0.3f ? "small" : "average";
            }
            catch { if (!c.ContainsKey("role")) c["role"] = Teachers.IsTeacherVoice(h) ? "teacher" : "student"; }
            if (!c.ContainsKey("job")) c["job"] = Teachers.Job(h);
            c["pers"] = h.personality.ToString();
            c["placeid"] = Places.CurrentId ?? "";
            // then a fight on her mind): the keys the composite lines need.
            try
            {
                c["weather"] = Weather.Now;
                c["wet"] = Weather.Raining ? "yes" : "no";
                if (Weather.Later) c["rainlater"] = "yes";
                c["outside"] = Weather.Outdoor(G.PlayerMapNo) ? "yes" : "no";
                c["rainlover"] = Weather.RainLover(h) ? "yes" : "no";
                foreach (var like in GirlInfo.LikesOf(h)) c["l_" + like] = "yes";
                FightCtx(h, c);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Context weather: " + e.Message); }
            c["interest"] = Interested(h) ? "yes" : "no";
            c["clubkind"] = GirlInfo.InNoClub(h) ? "none" : GirlInfo.InSportsClub(h) ? "sports" : "culture";
            var bff = Social.FriendsOf(h, 45).OrderByDescending(x => Social.Bond(h, x)).FirstOrDefault();
            if (bff != null) c["bff"] = GirlInfo.FirstName(bff);
            var partner = Pairs.PartnerOf(h);
            if (partner != null) c["partner"] = GirlInfo.FirstName(partner);
            c["days"] = gs.TalkDays.Count >= 8 ? "many" : gs.TalkDays.Count >= 3 ? "some" : "few";
            var mine = GirlInfo.LikesOf(h).ToList();
            if (mine.Count > 0)
            {
                var fav = mine[Math.Abs(GirlInfo.Key(h).GetHashCode()) % mine.Count];
                c["fav"] = GirlInfo.LikeLabel(fav);
                c["favdo"] = Do(fav);
            }
        }

        /// <summary>A fight on her mind: a girl she fought with (or fell out with) this past week, and not made up since.</summary>
        private static void FightCtx(SaveData.Heroine h, Dictionary<string, string> c)
        {
            var key = GirlInfo.Key(h);
            LoreFact fight = null;
            foreach (var kv in Lore.Of(key))
            {
                var f = kv.Value;
                if (!f.Involves(key) || f.Involves(Lore.Player) || S.Day - f.Day > 7) continue;
                if (f.Verb == "fought" || f.Verb == "insulted" && f.Obj == key) { if (fight == null || f.Day > fight.Day) fight = f; }
            }
            if (fight != null)
            {
                var other = fight.Subject == key ? fight.Obj : fight.Subject;
                if (Lore.Of(key).Any(kv => kv.Value.Verb == "made_up" && kv.Value.Between(key, other) && kv.Value.Day >= fight.Day)) fight = null;
                else { c["fight"] = "yes"; c["fightwho"] = GirlInfo.FirstName(other); return; }
            }
            // Or a girl she can't stand (bond), who is on the same map now.
            var rival = Social.GirlsOnMyMap().Where(o => o != h && Social.Bond(h, o) <= -30).OrderBy(o => Social.Bond(h, o)).FirstOrDefault();
            if (rival != null) { c["rivalnear"] = GirlInfo.FirstName(rival); }
        }

        /// <summary>The other girl of a fact, as she sees her: her best friend, her crush, her rival.</summary>
        private static void WhoCtx(SaveData.Heroine h, SaveData.Heroine other, Dictionary<string, string> c)
        {
            if (other == null) return;
            var bond = Social.Bond(h, other);
            c["whorel"] = RomanceFor(h, other) >= 40 ? "crush" : bond >= 40 ? "friend" : bond <= -30 ? "rival" : "none";
            c["whorole"] = Teachers.IsTeacherVoice(other) ? "teacher" : "student";
        }

        private static string Do(string like)
        {
            string[] l;
            return LikeDo.TryGetValue(like, out l) ? l[Random.Range(0, l.Length)] : "hang out";
        }

        private static bool Heavy(Dictionary<string, string> c)
        {
            string v;
            var kind = c.TryGetValue("kind", out v) ? v : "";
            return kind == "you" || kind == "gossip" && (Num(c, "valence") < 0 || Num(c, "weight") >= 2);
        }

        /// <summary>
        /// The first line of a topic with, now and then, an opener before it, chosen by the context (pieces.dlg). No tail here
        /// (2026-10-31): a tail is another subject, and it stood between her line and your options. No opener later in the
        /// exchange either: they greet you ("Hi, you."), which made no sense as an answer.
        /// </summary>
        private static string Compose(string line, Dictionary<string, string> c)
        {
            if (string.IsNullOrEmpty(line)) return line;
            var open = Random.value < (Heavy(c) ? 0.2f : 0.4f) ? Bank.Piece("open", c, 1) : "";
            // Openers end in a stop ("Mm, babe...", "Sensei!"), so her line keeps its capital and names stay whole.
            return open == "" ? line : open.Trim() + " " + line;
        }

        /// <summary>The last line of an exchange that went well takes a tail now and then: nothing comes after it to answer.</summary>
        private static string Close(string line, Dictionary<string, string> c)
        {
            if (string.IsNullOrEmpty(line) || Heavy(c) || Random.value >= 0.35f) return line;
            var tail = Bank.Piece("tail", c, 1);
            return tail == "" ? line : line.TrimEnd() + " " + tail.Trim();
        }

        /// <summary>Your line with an opener for who she is to you ("Hey, babe," "Sensei,").</summary>
        private static string ComposeMe(string line, Dictionary<string, string> c)
        {
            if (string.IsNullOrEmpty(line) || Random.value > 0.35f) return line;
            // The line may greet already: "Hey, sensei. Sensei, hi."
            if (line.IndexOf("ensei", StringComparison.Ordinal) >= 0 || line.StartsWith("Hey", StringComparison.Ordinal) || line.StartsWith("Hi", StringComparison.Ordinal)) return line;
            var open = Bank.Piece("meopen", c, 1);
            if (open == "") return line;
            return open.Trim() + " " + line;
        }
    }
}
