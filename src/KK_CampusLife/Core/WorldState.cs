using System;
using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>How far two students are in a romance of their own (Couple is a couple; Strained and Broken can heal).</summary>
    internal enum PairStage { None, Crush, Talking, Couple, Strained, Broken }

    /// <summary>The story two students are living through on their own (Bonds in Motion).</summary>
    internal enum PairArc { None, Drifting, Rivals, Hobby, Reconcile }

    /// <summary>Something the player and a girl went through together, which she can bring up later (callbacks).</summary>
    internal sealed class Moment
    {
        public int Id;
        public int Day;
        public string Kind = "";     // shared_snack, walk, favor, quiet, sunday, tradition, scene, ...
        public string Text = "";     // reads after "Remember when ...": "you walked me to the gym"
        public string Place = "";
        public string With = "";     // another girl it involved
        public bool Pending;         // she has not brought it up yet (a callback waits)
        public int Tone;             // +1 good, 0 plain, -1 sore

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object> { {"id", Id}, {"d", Day}, {"k", Kind}, {"t", Text}, {"p", Place}, {"w", With}, {"pd", Pending}, {"tn", Tone} };
        }

        public static Moment FromDict(Dictionary<string, object> d)
        {
            return new Moment { Id = d.GetInt("id"), Day = d.GetInt("d"), Kind = d.GetString("k"), Text = d.GetString("t"), Place = d.GetString("p"), With = d.GetString("w"), Pending = d.GetBool("pd"), Tone = d.GetInt("tn") };
        }
    }

    /// <summary>Two students (keys sorted, A before B): their romance each way, and what they share.</summary>
    internal sealed class PairState
    {
        public string A = "", B = "";
        public int RomAB, RomBA;          // 0-100, how A feels about B and the other way
        public int Trust = 50;            // 0-100 between them
        public int Jealousy;              // 0-100
        public int Intimacy;              // 0-100
        public int Exposure;              // 0-100: secret, rumored (25+), everyone knows (50+)
        public PairStage Stage;
        public int StageDay;
        public int CoupleSince = -1;
        public int LastTogether = -1;     // day they last spent a period together
        public int Together;              // periods together this week
        public PairArc Arc;
        public int ArcStep;
        public int ArcDay = -1;
        public string ArcNote = "";
        public int Cooldown = -1;         // no director scene about them before this day

        public string Key { get { return A + "|" + B; } }
        public string Other(string k) { return k == A ? B : A; }
        public int RomanceOf(string k) { return k == A ? RomAB : RomBA; }
        public void AddRomance(string k, int d)
        {
            if (k == A) RomAB = Clamp(RomAB + d); else RomBA = Clamp(RomBA + d);
        }

        public static int Clamp(int v) { return v < 0 ? 0 : v > 100 ? 100 : v; }

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                {"a", A}, {"b", B}, {"rab", RomAB}, {"rba", RomBA}, {"tr", Trust}, {"je", Jealousy}, {"in", Intimacy}, {"ex", Exposure},
                {"st", (int)Stage}, {"sd", StageDay}, {"cs", CoupleSince}, {"lt", LastTogether}, {"tg", Together}, {"ar", (int)Arc},
                {"as", ArcStep}, {"ad", ArcDay}, {"an", ArcNote}, {"cd", Cooldown}
            };
        }

        public static PairState FromDict(Dictionary<string, object> d)
        {
            return new PairState
            {
                A = d.GetString("a"), B = d.GetString("b"), RomAB = d.GetInt("rab"), RomBA = d.GetInt("rba"), Trust = d.GetInt("tr", 50),
                Jealousy = d.GetInt("je"), Intimacy = d.GetInt("in"), Exposure = d.GetInt("ex"), Stage = (PairStage)d.GetInt("st"),
                StageDay = d.GetInt("sd"), CoupleSince = d.GetInt("cs", -1), LastTogether = d.GetInt("lt", -1), Together = d.GetInt("tg"),
                Arc = (PairArc)d.GetInt("ar"), ArcStep = d.GetInt("as"), ArcDay = d.GetInt("ad", -1), ArcNote = d.GetString("an"), Cooldown = d.GetInt("cd", -1)
            };
        }
    }

    /// <summary>A story going around the school (Rumor Chains): who it is about, who heard which version, how hot it is.</summary>
    internal sealed class Scandal
    {
        public int Id;
        public int Day;
        public string Kind = "";         // seen_with, couple_seen, girls_room, stole, cheated, council, ...
        public string Subject = "";      // girl it is about ("" = the player)
        public string Other = "";        // second girl in it
        public string Teller = "";       // who started it
        public int Heat = 60;            // 0-100, cools every day
        public int Step;                 // chain step the player reached (0 none, 1 heard, 2 asked a witness, 3 faced it, 4 settled)
        public bool Main;                // the one chain the director is following
        public bool Closed;
        public string Outcome = "";
        public Dictionary<string, int> Versions = new Dictionary<string, int>(); // girl -> 0 what happened, 1 twisted, 2 one side's story

        public Dictionary<string, object> ToDict()
        {
            var v = new Dictionary<string, object>();
            foreach (var kv in Versions) v[kv.Key] = kv.Value;
            return new Dictionary<string, object>
            {
                {"id", Id}, {"d", Day}, {"k", Kind}, {"s", Subject}, {"o", Other}, {"t", Teller}, {"h", Heat}, {"st", Step}, {"m", Main},
                {"c", Closed}, {"oc", Outcome}, {"v", v}
            };
        }

        public static Scandal FromDict(Dictionary<string, object> d)
        {
            var s = new Scandal
            {
                Id = d.GetInt("id"), Day = d.GetInt("d"), Kind = d.GetString("k"), Subject = d.GetString("s"), Other = d.GetString("o"),
                Teller = d.GetString("t"), Heat = d.GetInt("h"), Step = d.GetInt("st"), Main = d.GetBool("m"), Closed = d.GetBool("c"), Outcome = d.GetString("oc")
            };
            var v = d.GetObj("v");
            if (v != null) foreach (var kv in v) s.Versions[kv.Key] = v.GetInt(kv.Key);
            return s;
        }
    }

    /// <summary>A mark the season left (Class Anthology).</summary>
    internal sealed class Milestone
    {
        public int Day, Week, Weight;
        public string Kind = "", Text = "", Girls = "";

        public Dictionary<string, object> ToDict() { return new Dictionary<string, object> { {"d", Day}, {"w", Week}, {"wt", Weight}, {"k", Kind}, {"t", Text}, {"g", Girls} }; }

        public static Milestone FromDict(Dictionary<string, object> d)
        {
            return new Milestone { Day = d.GetInt("d"), Week = d.GetInt("w"), Weight = d.GetInt("wt"), Kind = d.GetString("k"), Text = d.GetString("t"), Girls = d.GetString("g") };
        }
    }

    /// <summary>The Koikatsu Club's project for the culture festival (Club Identity).</summary>
    internal sealed class ClubProject
    {
        public int Week = -1;            // loop week it belongs to (absolute week)
        public string Type = "";         // community, showcase, fundraiser, joint
        public int Stage;                // 0 not started, 1 planned, 2 setback handled, 3 presented
        public List<string> Members = new List<string>();
        public int Score;
        public string Result = "";

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object> { {"w", Week}, {"t", Type}, {"s", Stage}, {"m", Members.Cast<object>().ToList()}, {"sc", Score}, {"r", Result} };
        }

        public static ClubProject FromDict(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var p = new ClubProject { Week = d.GetInt("w", -1), Type = d.GetString("t"), Stage = d.GetInt("s"), Score = d.GetInt("sc"), Result = d.GetString("r") };
            var m = d.GetList("m");
            if (m != null) p.Members = m.Where(o => o != null).Select(o => o.ToString()).ToList();
            return p;
        }
    }

    /// <summary>A student council election (Student Council Race).</summary>
    internal sealed class CouncilRace
    {
        public int Week = -1;
        public string A = "", B = "";    // A asked for your help, B is her opponent
        public string Platform = "";     // A's main promise
        public string RivalPlatform = "";
        public int Stage;                // 0 announced, 1 helping, 2 campaign, 3 debate, 4 counted
        public int Support;              // A's share of the vote, 0-100
        public string Approach = "";     // how you ran it
        public string Winner = "";

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object> { {"w", Week}, {"a", A}, {"b", B}, {"p", Platform}, {"rp", RivalPlatform}, {"s", Stage}, {"su", Support}, {"ap", Approach}, {"wi", Winner} };
        }

        public static CouncilRace FromDict(Dictionary<string, object> d)
        {
            if (d == null) return null;
            return new CouncilRace
            {
                Week = d.GetInt("w", -1), A = d.GetString("a"), B = d.GetString("b"), Platform = d.GetString("p"), RivalPlatform = d.GetString("rp"),
                Stage = d.GetInt("s"), Support = d.GetInt("su"), Approach = d.GetString("ap"), Winner = d.GetString("wi")
            };
        }
    }

    internal static class JsonLists
    {
        public static List<int> Ints(List<object> l)
        {
            return l == null ? new List<int>() : l.Select(o => o is double ? (int)(double)o : o is int ? (int)o : 0).ToList();
        }

        public static List<string> Strings(List<object> l)
        {
            return l == null ? new List<string>() : l.Where(o => o != null).Select(o => o.ToString()).ToList();
        }

        public static Dictionary<string, object> IntDict(Dictionary<string, int> d)
        {
            var r = new Dictionary<string, object>();
            foreach (var kv in d) r[kv.Key] = kv.Value;
            return r;
        }

        public static Dictionary<string, int> ReadIntDict(Dictionary<string, object> d)
        {
            var r = new Dictionary<string, int>();
            if (d == null) return r;
            foreach (var kv in d) r[kv.Key] = d.GetInt(kv.Key);
            return r;
        }
    }

    internal sealed partial class GirlState
    {
        // ---- conversation by scene ----
        public List<Moment> Moments = new List<Moment>();
        public List<string> Forms = new List<string>();        // last conversation forms with her, newest last (5)
        public List<string> LinesUsed = new List<string>();    // last concrete option templates offered (12)
        public int MapFormStamp = -1;                          // period (day*16+mask) she last stopped you on the map
        public string Pending = "";                            // a scene she wants to have next time you talk (form:subject)
        public List<string> Facts = new List<string>();        // things she told you about herself (Conversation.Fact ids)

        // ---- the director ----
        public int NeedSocial = 60, NeedFood = 40, NeedRest = 70;   // 0-100, higher = more satisfied
        public string Intent = "";                             // what she wants this period ("with:key", "eat", ...)
        public int IntentStamp = -1;
        public int GirlsOk = -1;                               // open to romance with other girls (-1 not decided)
        public int ScornedUntil = -1;                          // she does not want to talk to you until this day
        public int Scorned;                                    // 0-100 how hurt (steal route), heals with redemption
        public int Redemption;                                 // steps of the redemption done
        public int Conflicted;                                 // periods in a row torn between her partner and you
        public int ConflictedDay = -1;
        public int Caught;                                     // times you walked in on her in H
        public int Pass;                                       // one-time passes: a hurtful thing about you is let go
        public int OwesH;                                      // 1: she owes you, says yes to H once
        public int CaughtTalked;                               // how many of those times you already talked about with her

        // ---- relationship seasons, sundays ----
        public int Chapter;                                    // relationship chapters finished (0-3)
        public int ChapterDay = -1;
        public string ChapterOpen = "";                        // an unresolved talk of a chapter, comes back later
        public List<string> Traditions = new List<string>();   // things the two of you do together
        public int Sundays;                                    // Sunday stories with her
        public string LastSunday = "";
        public string Epilogue = "";                           // her "after school" ending, if the season wrote one

        private void WriteWorld(Dictionary<string, object> d)
        {
            d["mo"] = Moments.Select(m => (object)m.ToDict()).ToList();
            d["fo"] = Forms.Cast<object>().ToList();
            d["lu"] = LinesUsed.Cast<object>().ToList();
            d["mfs"] = MapFormStamp;
            d["pe"] = Pending;
            d["fct"] = Facts.Cast<object>().ToList();
            d["ns"] = NeedSocial; d["nf"] = NeedFood; d["nr"] = NeedRest;
            d["it"] = Intent; d["its"] = IntentStamp; d["gok"] = GirlsOk;
            d["cau"] = Caught; d["pas"] = Pass; d["owh"] = OwesH; d["caut"] = CaughtTalked;
            d["scu"] = ScornedUntil; d["sc"] = Scorned; d["rd"] = Redemption; d["cf"] = Conflicted; d["cfd"] = ConflictedDay;
            d["ch"] = Chapter; d["chd"] = ChapterDay; d["cho"] = ChapterOpen;
            d["trd"] = Traditions.Cast<object>().ToList();
            d["sun"] = Sundays; d["lsun"] = LastSunday; d["epi"] = Epilogue;
        }

        private void ReadWorld(Dictionary<string, object> d)
        {
            var mo = d.GetList("mo");
            if (mo != null) Moments = mo.OfType<Dictionary<string, object>>().Select(Moment.FromDict).ToList();
            Forms = JsonLists.Strings(d.GetList("fo"));
            LinesUsed = JsonLists.Strings(d.GetList("lu"));
            MapFormStamp = d.GetInt("mfs", -1);
            Pending = d.GetString("pe");
            Facts = JsonLists.Strings(d.GetList("fct"));
            NeedSocial = d.GetInt("ns", 60); NeedFood = d.GetInt("nf", 40); NeedRest = d.GetInt("nr", 70);
            Intent = d.GetString("it"); IntentStamp = d.GetInt("its", -1); GirlsOk = d.GetInt("gok", -1);
            Caught = d.GetInt("cau"); Pass = d.GetInt("pas"); OwesH = d.GetInt("owh"); CaughtTalked = d.GetInt("caut");
            ScornedUntil = d.GetInt("scu", -1); Scorned = d.GetInt("sc"); Redemption = d.GetInt("rd"); Conflicted = d.GetInt("cf"); ConflictedDay = d.GetInt("cfd", -1);
            Chapter = d.GetInt("ch"); ChapterDay = d.GetInt("chd", -1); ChapterOpen = d.GetString("cho");
            Traditions = JsonLists.Strings(d.GetList("trd"));
            Sundays = d.GetInt("sun"); LastSunday = d.GetString("lsun"); Epilogue = d.GetString("epi");
        }
    }

    internal sealed partial class CampusState
    {
        public Dictionary<string, PairState> Pairs = new Dictionary<string, PairState>();
        public List<Scandal> Scandals = new List<Scandal>();
        public int NextScandalId = 1;
        public int NextMomentId = 1;

        // ---- the director ----
        public int DramaWeek = -1;            // week the drama budget belongs to
        public int DramaUsed;                 // tension points spent this week
        public int Tension;                   // recent tension, cools by itself (0-100)
        public List<string> Signatures = new List<string>(); // recent scenes "mold|a|b|place|why"
        public int LastSituationStamp = -1;   // period the last situation played
        public List<string> SchoolNews = new List<string>(); // off-screen happenings this week, for the report
        public List<string> LastWeekNews = new List<string>();

        // ---- harem ----
        public List<string> Harem = new List<string>();      // girls who agreed to share you
        public int HaremTalkDay = -1;                       // last negotiation (one a week)
        public string HaremTalk = "";                       // the negotiation going on: "a\tb\tstep" (1 probed, 2 both asked; keys hold "|")

        // ---- season / anthology ----
        public int Season = 1;
        public int SeasonStartWeek = 1;
        public List<Milestone> Milestones = new List<Milestone>();
        public List<string> Archive = new List<string>();    // past seasons, one paragraph each (lines joined with \n)
        public int LastCeremonyWeek = -1;

        // ---- club and council ----
        public ClubProject Project;
        public List<string> ClubHistory = new List<string>();
        public string ClubIdentity = "";
        public CouncilRace Council;
        public string CouncilWinner = "";
        public string CouncilEffect = "";
        public int CouncilEffectUntilWeek = -1;

        public PairState Pair(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b) return null;
            var x = string.CompareOrdinal(a, b) < 0 ? a : b;
            var y = x == a ? b : a;
            PairState p;
            if (!Pairs.TryGetValue(x + "|" + y, out p))
            {
                p = new PairState { A = x, B = y };
                Pairs[p.Key] = p;
            }
            return p;
        }

        public PairState FindPair(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b) || a == b) return null;
            var x = string.CompareOrdinal(a, b) < 0 ? a : b;
            var y = x == a ? b : a;
            PairState p;
            return Pairs.TryGetValue(x + "|" + y, out p) ? p : null;
        }

        private void WriteWorld(Dictionary<string, object> root)
        {
            root["pairs"] = Pairs.Values.Select(p => (object)p.ToDict()).ToList();
            root["scan"] = Scandals.Select(s => (object)s.ToDict()).ToList();
            root["scanN"] = NextScandalId;
            root["momN"] = NextMomentId;
            root["drW"] = DramaWeek; root["drU"] = DramaUsed; root["ten"] = Tension;
            root["sig"] = Signatures.Cast<object>().ToList();
            root["sitS"] = LastSituationStamp;
            root["news"] = SchoolNews.Cast<object>().ToList();
            root["lnews"] = LastWeekNews.Cast<object>().ToList();
            root["harem"] = Harem.Cast<object>().ToList();
            root["hrD"] = HaremTalkDay;
            root["hrT"] = HaremTalk;
            root["season"] = Season; root["seasonW"] = SeasonStartWeek;
            root["miles"] = Milestones.Select(m => (object)m.ToDict()).ToList();
            root["arch"] = Archive.Cast<object>().ToList();
            root["cerW"] = LastCeremonyWeek;
            root["proj"] = Project == null ? null : Project.ToDict();
            root["clubH"] = ClubHistory.Cast<object>().ToList();
            root["clubI"] = ClubIdentity;
            root["council"] = Council == null ? null : Council.ToDict();
            root["cWin"] = CouncilWinner; root["cEff"] = CouncilEffect; root["cEffW"] = CouncilEffectUntilWeek;
        }

        private void ReadWorld(Dictionary<string, object> d)
        {
            var pairs = d.GetList("pairs");
            if (pairs != null)
                foreach (var p in pairs.OfType<Dictionary<string, object>>().Select(PairState.FromDict))
                    if (!string.IsNullOrEmpty(p.A) && !string.IsNullOrEmpty(p.B)) Pairs[p.Key] = p;
            var scan = d.GetList("scan");
            if (scan != null) Scandals = scan.OfType<Dictionary<string, object>>().Select(Scandal.FromDict).ToList();
            NextScandalId = Math.Max(1, d.GetInt("scanN", 1));
            NextMomentId = Math.Max(1, d.GetInt("momN", 1));
            DramaWeek = d.GetInt("drW", -1); DramaUsed = d.GetInt("drU"); Tension = d.GetInt("ten");
            Signatures = JsonLists.Strings(d.GetList("sig"));
            LastSituationStamp = d.GetInt("sitS", -1);
            SchoolNews = JsonLists.Strings(d.GetList("news"));
            LastWeekNews = JsonLists.Strings(d.GetList("lnews"));
            Harem = JsonLists.Strings(d.GetList("harem"));
            HaremTalkDay = d.GetInt("hrD", -1);
            HaremTalk = d.GetString("hrT");
            Season = Math.Max(1, d.GetInt("season", 1)); SeasonStartWeek = d.GetInt("seasonW", 1);
            var miles = d.GetList("miles");
            if (miles != null) Milestones = miles.OfType<Dictionary<string, object>>().Select(Milestone.FromDict).ToList();
            Archive = JsonLists.Strings(d.GetList("arch"));
            LastCeremonyWeek = d.GetInt("cerW", -1);
            Project = ClubProject.FromDict(d.GetObj("proj"));
            ClubHistory = JsonLists.Strings(d.GetList("clubH"));
            ClubIdentity = d.GetString("clubI");
            Council = CouncilRace.FromDict(d.GetObj("council"));
            CouncilWinner = d.GetString("cWin"); CouncilEffect = d.GetString("cEff"); CouncilEffectUntilWeek = d.GetInt("cEffW", -1);
        }
    }
}
