using System;
using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    public enum Mood { None, Cheerful, Hungry, Bored, Stressed, Lonely, Flirty, Sleepy, Upset }
    public enum Tone { Gentle, Energetic, Cool, Teasing }

    public enum RequestKind
    {
        Talk,       // talk with her during the window
        Lunch,      // have lunch together
        Study,      // study together
        Exercise,   // exercise together
        Club,       // club activity together
        WalkHome,   // walk home together
        Gift,       // give her a gift (optionally of a category)
        Intimate,   // H with her
        Errand,     // do an activity somewhere, then talk to her
        MeetAt,     // talk with her while you are at a place
        Activity,   // just do an activity (no girl)
    }

    /// <summary>Hidden: she has the problem but the player does not know yet (learned by talking, from friends or events).</summary>
    public enum RequestState { Active, Done, Failed, Hidden }

    [Flags]
    public enum PeriodMask { None = 0, Lunch = 1, Club = 2, After = 4, Any = 7 }

    internal sealed class Rewards
    {
        public int Favor, Lewd, Money, Rep, Int, Str, Sens, Club, Devotion;
        public string Item = "";   // item id she gives you

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                {"f", Favor}, {"l", Lewd}, {"m", Money}, {"r", Rep}, {"i", Int}, {"s", Str}, {"x", Sens}, {"c", Club},
                {"dv", Devotion}, {"it", Item}
            };
        }

        public static Rewards FromDict(Dictionary<string, object> d)
        {
            var r = new Rewards();
            if (d == null) return r;
            r.Favor = d.GetInt("f"); r.Lewd = d.GetInt("l"); r.Money = d.GetInt("m"); r.Rep = d.GetInt("r");
            r.Int = d.GetInt("i"); r.Str = d.GetInt("s"); r.Sens = d.GetInt("x"); r.Club = d.GetInt("c");
            r.Devotion = d.GetInt("dv"); r.Item = d.GetString("it");
            return r;
        }

        public string Describe()
        {
            var parts = new List<string>();
            if (Favor != 0) parts.Add((Favor > 0 ? "+" : "") + Favor + " favor");
            if (Lewd != 0) parts.Add((Lewd > 0 ? "+" : "") + Lewd + " lewd");
            if (Money != 0) parts.Add((Money > 0 ? "+" : "") + Money + " yen");
            if (Rep != 0) parts.Add((Rep > 0 ? "+" : "") + Rep + " reputation");
            if (Int != 0) parts.Add("+" + Int + " INT");
            if (Str != 0) parts.Add("+" + Str + " STR");
            if (Sens != 0) parts.Add("+" + Sens + " SEN");
            if (Club != 0) parts.Add("+" + Club + " club pts");
            if (Devotion != 0) parts.Add("+" + Devotion + " devotion");
            if (!string.IsNullOrEmpty(Item)) parts.Add("got " + Items.NameOf(Item));
            return parts.Count == 0 ? "-" : string.Join(", ", parts.ToArray());
        }
    }

    internal sealed class Request
    {
        public int Id;
        public RequestKind Kind;
        public RequestState State;
        public string Girl = "";       // heroine key (may be empty for Activity)
        public string Other = "";      // second girl for drama/introductions
        public string Item = "";       // gift category or item id
        public string Activity = "";   // activity id for Errand/Activity
        public string Place = "";      // place id for MeetAt/Errand
        public PeriodMask Periods = PeriodMask.Any;
        public int DeadlineDay;
        public int CreatedDay;
        public string Title = "";
        public string Text = "";
        public Rewards Reward = new Rewards();
        public string Arc = "";        // arc id when part of a story
        public int ArcStep;
        public int Progress;           // e.g. errand activity already done
        public bool Lewd;
        public string Source = "board"; // board, arc, happening, event, drama
        public string Button = "";     // label of the temporary talk button for story steps
        public string LearnedFrom = "";  // "" (posted), "her", key of the friend who told, "event"
        public string Chain = "";        // multi-part request id (e.g. lost_item)
        public int ChainStep;

        public Request Btn(string label) { Button = label; return this; }

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                {"id", Id}, {"k", (int)Kind}, {"st", (int)State}, {"g", Girl}, {"o", Other}, {"it", Item},
                {"ac", Activity}, {"pl", Place}, {"p", (int)Periods}, {"dl", DeadlineDay}, {"cd", CreatedDay},
                {"t", Title}, {"tx", Text}, {"rw", Reward.ToDict()}, {"arc", Arc}, {"as", ArcStep},
                {"pg", Progress}, {"lw", Lewd}, {"src", Source}, {"b", Button}, {"lf", LearnedFrom}, {"ch", Chain}, {"cs", ChainStep}
            };
        }

        public static Request FromDict(Dictionary<string, object> d)
        {
            return new Request
            {
                Id = d.GetInt("id"), Kind = (RequestKind)d.GetInt("k"), State = (RequestState)d.GetInt("st"),
                Girl = d.GetString("g"), Other = d.GetString("o"), Item = d.GetString("it"), Activity = d.GetString("ac"),
                Place = d.GetString("pl"), Periods = (PeriodMask)d.GetInt("p", 7), DeadlineDay = d.GetInt("dl"),
                CreatedDay = d.GetInt("cd"), Title = d.GetString("t"), Text = d.GetString("tx"),
                Reward = Rewards.FromDict(d.GetObj("rw")), Arc = d.GetString("arc"), ArcStep = d.GetInt("as"),
                Progress = d.GetInt("pg"), Lewd = d.GetBool("lw"), Source = d.GetString("src", "board"), Button = d.GetString("b"),
                LearnedFrom = d.GetString("lf"), Chain = d.GetString("ch"), ChainStep = d.GetInt("cs")
            };
        }
    }

    internal sealed class ArcState
    {
        public string Id = "";
        public int Step;
        public int StartedDay;
        public int Counter;             // generic progress counter (e.g. study sessions)
        public string Other = "";       // partner girl for two-girl stories
        public string Choice = "";      // last choice made in a scene
        public int Retries;             // failed steps already retried

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object> { {"id", Id}, {"s", Step}, {"d", StartedDay}, {"c", Counter}, {"o", Other}, {"ch", Choice}, {"r", Retries} };
        }

        public static ArcState FromDict(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var a = new ArcState { Id = d.GetString("id"), Step = d.GetInt("s"), StartedDay = d.GetInt("d"), Counter = d.GetInt("c"), Other = d.GetString("o"), Choice = d.GetString("ch"), Retries = d.GetInt("r") };
            return string.IsNullOrEmpty(a.Id) ? null : a;
        }
    }

    internal sealed partial class GirlState
    {
        public string Key = "";
        public Mood Mood;
        public int MoodDay;
        public int MoodKnownDay = -1;   // day the player learned today's mood
        public int LastTalkDay = -1;
        public int LastGiftDay = -1;
        public int Talks;
        public int Gifts;
        public int KnownLikes;          // bitmask over GirlInfo.LikeNames discovered by the player
        public int LastSpotlightWeek;
        public int Attention = 50;      // how seen she feels (0-100)
        public int Devotion;            // grows forever; favor gained at max favor turns into devotion
        public int LastPerkDay = -1;    // day her daily devotion perk was used
        public int TalkDay = -1;        // day TalksToday counts
        public int TalksToday;          // conversations with her today (talk fatigue)
        public int ChatDay = -1;        // day of the last topic chat
        public string LastTopics = "";  // topics of the last chat, comma separated
        public int Trust = -1;          // 0-100, -1 = not set yet (starts from her favor)
        public List<int> Known = new List<int>();       // incidents she heard about
        public List<int> Unmentioned = new List<int>(); // ...and has not brought up with you yet
        public List<int> Saw = new List<int>();         // ...the ones she saw with her own eyes (not heard)
        public List<int> TalkDays = new List<int>();    // days you talked (last 10)
        public int JealousDay = -1;     // last day she told you she feels left out
        public int VentDay = -1;        // last day she vented to you
        public int ComplimentDay = -1;  // last day you complimented her
        public List<int> Cleared = new List<int>();     // incidents you talked through with her
        public int DuoDay = -1;         // last day she came over in a two-girl scene
        public List<int> DuoHandled = new List<int>();  // incidents a two-girl scene was already about
        public List<string> Episodes = new List<string>(); // spotlight episodes she starred in, oldest first (ids, "id:ending")
        public int EpisodeDay = -1;     // last day she starred in one
        public ArcState Arc;
        public List<string> FinishedArcs = new List<string>();
        public Dictionary<string, int> Bonds = new Dictionary<string, int>(); // affinity to other girls (-100..100)
        public int Rep = 50;            // her own reputation at school (Standing), 0-100
        public Dictionary<string, int> Share = new Dictionary<string, int>();     // what she decided about you and another girl (Share.Stance)
        public Dictionary<string, int> ShareCalm = new Dictionary<string, int>(); // how close a refusal is to acceptance (0-100)
        public List<string> ShareSaid = new List<string>();                       // girls she already told you her stance about

        public Dictionary<string, object> ToDict()
        {
            var bonds = new Dictionary<string, object>();
            foreach (var kv in Bonds) bonds[kv.Key] = kv.Value;
            var d = new Dictionary<string, object>
            {
                {"k", Key}, {"m", (int)Mood}, {"md", MoodDay}, {"mk", MoodKnownDay}, {"lt", LastTalkDay},
                {"lg", LastGiftDay}, {"t", Talks}, {"g", Gifts}, {"kl", KnownLikes}, {"ls", LastSpotlightWeek},
                {"at", Attention}, {"dv", Devotion}, {"pd", LastPerkDay}, {"arc", Arc == null ? null : Arc.ToDict()},
                {"td", TalkDay}, {"tt", TalksToday}, {"cd", ChatDay}, {"lto", LastTopics},
                {"tr", Trust}, {"kn", Known.Cast<object>().ToList()}, {"um", Unmentioned.Cast<object>().ToList()}, {"sw", Saw.Cast<object>().ToList()},
                {"tds", TalkDays.Cast<object>().ToList()}, {"clr", Cleared.Cast<object>().ToList()}, {"jd", JealousDay}, {"vd", VentDay}, {"cpd", ComplimentDay},
                {"fa", FinishedArcs.Cast<object>().ToList()}, {"b", bonds}, {"dud", DuoDay}, {"duh", DuoHandled.Cast<object>().ToList()},
                {"eps", Episodes.Cast<object>().ToList()}, {"epd", EpisodeDay}, {"rp", Rep}
            };
            var sh = new Dictionary<string, object>();
            foreach (var kv in Share) sh[kv.Key] = kv.Value;
            var shc = new Dictionary<string, object>();
            foreach (var kv in ShareCalm) shc[kv.Key] = kv.Value;
            d["sh"] = sh; d["shc"] = shc; d["shs"] = ShareSaid.Cast<object>().ToList();
            WriteWorld(d);
            return d;
        }

        private static List<int> Ints(List<object> l)
        {
            return l == null ? new List<int>() : l.Select(o => o is double ? (int)(double)o : o is int ? (int)o : 0).ToList();
        }

        public static GirlState FromDict(Dictionary<string, object> d)
        {
            var g = new GirlState
            {
                Key = d.GetString("k"), Mood = (Mood)d.GetInt("m"), MoodDay = d.GetInt("md"), MoodKnownDay = d.GetInt("mk", -1),
                LastTalkDay = d.GetInt("lt", -1), LastGiftDay = d.GetInt("lg", -1), Talks = d.GetInt("t"), Gifts = d.GetInt("g"),
                KnownLikes = d.GetInt("kl"), LastSpotlightWeek = d.GetInt("ls"), Attention = d.GetInt("at", 50),
                Devotion = d.GetInt("dv"), LastPerkDay = d.GetInt("pd", -1),
                TalkDay = d.GetInt("td", -1), TalksToday = d.GetInt("tt"), ChatDay = d.GetInt("cd", -1), LastTopics = d.GetString("lto"),
                Arc = ArcState.FromDict(d.GetObj("arc")),
                Trust = d.GetInt("tr", -1), JealousDay = d.GetInt("jd", -1), VentDay = d.GetInt("vd", -1), ComplimentDay = d.GetInt("cpd", -1),
                Known = Ints(d.GetList("kn")), Unmentioned = Ints(d.GetList("um")), Saw = Ints(d.GetList("sw")), TalkDays = Ints(d.GetList("tds")), Cleared = Ints(d.GetList("clr")),
                DuoDay = d.GetInt("dud", -1), DuoHandled = Ints(d.GetList("duh")), EpisodeDay = d.GetInt("epd", -1), Rep = d.GetInt("rp", 50)
            };
            var eps = d.GetList("eps");
            if (eps != null) g.Episodes = eps.Where(o => o != null).Select(o => o.ToString()).ToList();
            var fa = d.GetList("fa");
            if (fa != null) g.FinishedArcs = fa.Where(o => o != null).Select(o => o.ToString()).ToList();
            var b = d.GetObj("b");
            if (b != null) foreach (var kv in b) g.Bonds[kv.Key] = b.GetInt(kv.Key);
            var sh = d.GetObj("sh");
            if (sh != null) foreach (var kv in sh) g.Share[kv.Key] = sh.GetInt(kv.Key);
            var shc = d.GetObj("shc");
            if (shc != null) foreach (var kv in shc) g.ShareCalm[kv.Key] = shc.GetInt(kv.Key);
            var shs = d.GetList("shs");
            if (shs != null) g.ShareSaid = shs.Where(o => o != null).Select(o => o.ToString()).ToList();
            g.ReadWorld(d);
            return g;
        }
    }

    /// <summary>Something that happened between the player and a girl, which other girls can hear about.</summary>
    internal sealed class Incident
    {
        public int Id;
        public int Day;
        public string Kind = "";   // see Social.Kinds
        public string Girl = "";   // girl it happened to ("" = nobody in particular)
        public string What = "";   // for "flirted": what it was (Witness.Flirt, Hand, Touch, Kiss); "" = older saves

        public Dictionary<string, object> ToDict() { return new Dictionary<string, object> { {"id", Id}, {"d", Day}, {"k", Kind}, {"g", Girl}, {"w", What} }; }

        public static Incident FromDict(Dictionary<string, object> d)
        {
            return new Incident { Id = d.GetInt("id"), Day = d.GetInt("d"), Kind = d.GetString("k"), Girl = d.GetString("g"), What = d.GetString("w") };
        }
    }

    internal sealed class JournalEntry
    {
        public int Day;
        public string Text;
    }

    /// <summary>Everything Campus Life remembers about one save file.</summary>
    internal sealed partial class CampusState
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public bool Started;
        public int Day = 1;               // absolute school day counter (starts at 1)
        public int Week = 1;              // absolute week counter (0 while a game started on a Sunday waits for Monday)
        public int Money;
        public int Energy;
        public int Reputation;            // school-wide reputation 0-100
        public int ClubFunds;             // club points earned through Campus Life
        public int NextRequestId = 1;
        public int LastAllowanceWeek;
        public int LastReportWeek;
        public int SpotlightWeek;
        public Dictionary<string, int> Inventory = new Dictionary<string, int>();
        public Dictionary<string, GirlState> Girls = new Dictionary<string, GirlState>();
        public List<Request> Requests = new List<Request>();
        public List<string> Spotlight = new List<string>();
        public List<JournalEntry> Journal = new List<JournalEntry>();
        public Dictionary<string, int> WeekStats = new Dictionary<string, int>();
        public Dictionary<string, int> LastWeekStats = new Dictionary<string, int>();
        public Dictionary<string, int> Buffs = new Dictionary<string, int>();     // buff id -> last day it is active
        public Dictionary<string, int> EventLastWeek = new Dictionary<string, int>(); // school event id -> week it last happened
        public string PendingEvent = "";      // school event waiting to be attended today
        public string PendingHappening = "";  // happening waiting to be played
        public int LastHappeningDay;
        public int LastHappeningPeriod = -1;
        public List<int> ExamScores = new List<int>();
        public string DirectorPick = "";     // story heroine whose next event is open today
        public int DirectorDay = -1;
        public int DirectorEvents;           // her finished event count when she was picked
        public List<Incident> Incidents = new List<Incident>();
        public int NextIncidentId = 1;
        public int LastSpreadDay;
        public int LastEpisodeDay = -1;      // last day a spotlight episode played

        /// <summary>Week number to show the player (a Sunday before the first school week still reads "Week 1").</summary>
        public int ShownWeek { get { return Math.Max(1, Week); } }

        /// <summary>Moves every stored week number (used when the week counter is corrected).</summary>
        public void ShiftWeeks(int by)
        {
            Week += by;
            if (LastAllowanceWeek != 0) LastAllowanceWeek += by;
            if (LastReportWeek != 0) LastReportWeek += by;
            if (SpotlightWeek != 0) SpotlightWeek += by;
            foreach (var k in EventLastWeek.Keys.ToList()) if (EventLastWeek[k] != 0) EventLastWeek[k] += by;
            foreach (var g in Girls.Values) if (g.LastSpotlightWeek != 0) g.LastSpotlightWeek += by;
        }

        public GirlState Girl(string key)
        {
            GirlState g;
            if (!Girls.TryGetValue(key, out g))
            {
                g = new GirlState { Key = key };
                Girls[key] = g;
            }
            return g;
        }

        public int Count(string item)
        {
            int n;
            return Inventory.TryGetValue(item, out n) ? n : 0;
        }

        public void AddItem(string item, int n)
        {
            var v = Count(item) + n;
            if (v <= 0) Inventory.Remove(item);
            else Inventory[item] = v;
        }

        public void Stat(string key, int add = 1)
        {
            int v;
            WeekStats.TryGetValue(key, out v);
            WeekStats[key] = v + add;
        }

        public int GetStat(string key)
        {
            int v;
            return WeekStats.TryGetValue(key, out v) ? v : 0;
        }

        public bool HasBuff(string id)
        {
            int until;
            return Buffs.TryGetValue(id, out until) && until >= Day;
        }

        public void Log(string text)
        {
            Journal.Add(new JournalEntry { Day = Day, Text = text });
            if (Journal.Count > 300) Journal.RemoveRange(0, Journal.Count - 300);
        }

        private static Dictionary<string, object> IntDict(Dictionary<string, int> d)
        {
            var r = new Dictionary<string, object>();
            foreach (var kv in d) r[kv.Key] = kv.Value;
            return r;
        }

        private static Dictionary<string, int> ReadIntDict(Dictionary<string, object> d)
        {
            var r = new Dictionary<string, int>();
            if (d == null) return r;
            foreach (var kv in d) r[kv.Key] = d.GetInt(kv.Key);
            return r;
        }

        public string ToJson()
        {
            var girls = new Dictionary<string, object>();
            foreach (var kv in Girls) girls[kv.Key] = kv.Value.ToDict();
            var root = new Dictionary<string, object>
            {
                {"v", CurrentVersion}, {"started", Started}, {"day", Day}, {"week", Week}, {"money", Money},
                {"energy", Energy}, {"rep", Reputation}, {"funds", ClubFunds}, {"nextReq", NextRequestId},
                {"allowW", LastAllowanceWeek}, {"reportW", LastReportWeek}, {"spotW", SpotlightWeek},
                {"inv", IntDict(Inventory)}, {"girls", girls},
                {"req", Requests.Select(r => (object)r.ToDict()).ToList()},
                {"spot", Spotlight.Cast<object>().ToList()},
                {"journal", Journal.Select(j => (object)new Dictionary<string, object> { {"d", j.Day}, {"t", j.Text} }).ToList()},
                {"ws", IntDict(WeekStats)}, {"lws", IntDict(LastWeekStats)}, {"buffs", IntDict(Buffs)},
                {"evw", IntDict(EventLastWeek)}, {"pev", PendingEvent}, {"phap", PendingHappening},
                {"hapD", LastHappeningDay}, {"hapP", LastHappeningPeriod},
                {"exams", ExamScores.Cast<object>().ToList()},
                {"dirP", DirectorPick}, {"dirD", DirectorDay}, {"dirE", DirectorEvents},
                {"inc", Incidents.Select(i => (object)i.ToDict()).ToList()}, {"incN", NextIncidentId}, {"sprD", LastSpreadDay}, {"epD", LastEpisodeDay}
            };
            WriteWorld(root);
            return MiniJson.Serialize(root);
        }

        public static CampusState FromJson(string json)
        {
            var d = MiniJson.Parse(json) as Dictionary<string, object>;
            var s = new CampusState();
            if (d == null) return s;
            s.Version = d.GetInt("v", 1);
            s.Started = d.GetBool("started");
            s.Day = Math.Max(1, d.GetInt("day", 1));
            s.Week = Math.Max(0, d.GetInt("week", 1));
            s.Money = d.GetInt("money");
            s.Energy = d.GetInt("energy");
            s.Reputation = d.GetInt("rep");
            s.ClubFunds = d.GetInt("funds");
            s.NextRequestId = Math.Max(1, d.GetInt("nextReq", 1));
            s.LastAllowanceWeek = d.GetInt("allowW");
            s.LastReportWeek = d.GetInt("reportW");
            s.SpotlightWeek = d.GetInt("spotW");
            s.Inventory = ReadIntDict(d.GetObj("inv"));
            var girls = d.GetObj("girls");
            if (girls != null)
                foreach (var kv in girls)
                {
                    var gd = kv.Value as Dictionary<string, object>;
                    if (gd == null) continue;
                    var g = GirlState.FromDict(gd);
                    if (string.IsNullOrEmpty(g.Key)) g.Key = kv.Key;
                    s.Girls[g.Key] = g;
                }
            var req = d.GetList("req");
            if (req != null) s.Requests = req.OfType<Dictionary<string, object>>().Select(Request.FromDict).ToList();
            var spot = d.GetList("spot");
            if (spot != null) s.Spotlight = spot.Where(o => o != null).Select(o => o.ToString()).ToList();
            var j = d.GetList("journal");
            if (j != null) s.Journal = j.OfType<Dictionary<string, object>>().Select(x => new JournalEntry { Day = x.GetInt("d"), Text = x.GetString("t") }).ToList();
            s.WeekStats = ReadIntDict(d.GetObj("ws"));
            s.LastWeekStats = ReadIntDict(d.GetObj("lws"));
            s.Buffs = ReadIntDict(d.GetObj("buffs"));
            s.EventLastWeek = ReadIntDict(d.GetObj("evw"));
            s.PendingEvent = d.GetString("pev");
            s.PendingHappening = d.GetString("phap");
            s.LastHappeningDay = d.GetInt("hapD");
            s.LastHappeningPeriod = d.GetInt("hapP", -1);
            var ex = d.GetList("exams");
            if (ex != null) s.ExamScores = ex.Select(o => o is double ? (int)(double)o : 0).ToList();
            s.DirectorPick = d.GetString("dirP");
            s.DirectorDay = d.GetInt("dirD", -1);
            s.DirectorEvents = d.GetInt("dirE");
            var inc = d.GetList("inc");
            if (inc != null) s.Incidents = inc.OfType<Dictionary<string, object>>().Select(Incident.FromDict).ToList();
            s.NextIncidentId = Math.Max(1, d.GetInt("incN", 1));
            s.LastSpreadDay = d.GetInt("sprD");
            s.LastEpisodeDay = d.GetInt("epD", -1);
            s.ReadWorld(d);
            return s;
        }
    }
}
