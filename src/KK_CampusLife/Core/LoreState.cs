using System;
using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    /// <summary>How a girl knows a fact: she was in it, she saw it, someone told her, or it reached her as a rumor.</summary>
    internal enum How { Participant, Witness, Heard, Rumor }

    /// <summary>
    /// One thing that happened (the procedural dialogue's memory, note koikatsu-arquitetura-dialogo-procedural): who did
    /// what to whom, where and when, how much it weighs and how hidden it is. "player" is the player's key.
    /// </summary>
    internal sealed class LoreFact
    {
        public const int Secret = 0, Rumored = 1, Known = 2;
        public int Id, Day, Mask, Valence, Weight, Exposure = Known;
        public int Since = -1;                 // days after the two first met (started_dating, had_h); -1 not counted
        public string Place = "", Subject = "", Verb = "", Obj = "", About = "", Tags = "", Note = "";

        public bool Involves(string key) { return Subject == key || Obj == key || About == key; }
        public bool Between(string a, string b) { return Subject == a && Obj == b || Subject == b && Obj == a; }

        public List<object> ToList()
        {
            return new List<object> { Id, Day, Mask, Valence, Weight, Exposure, Since, Place, Subject, Verb, Obj, About, Tags, Note };
        }

        public static LoreFact FromList(List<object> l)
        {
            if (l == null || l.Count < 14) return null;
            Func<int, int> i = n => l[n] is double ? (int)(double)l[n] : 0;
            Func<int, string> s = n => l[n] != null ? l[n].ToString() : "";
            return new LoreFact
            {
                Id = i(0), Day = i(1), Mask = i(2), Valence = i(3), Weight = i(4), Exposure = i(5), Since = i(6),
                Place = s(7), Subject = s(8), Verb = s(9), Obj = s(10), About = s(11), Tags = s(12), Note = s(13)
            };
        }
    }

    /// <summary>What one girl knows of a fact, and from where.</summary>
    internal sealed class LoreKnow
    {
        public string Girl = "", From = "";
        public int Fact, Day, Opinion, Distort, Told;
        public int ToPlayer;                   // 0 never brought up with you, 1 mentioned, 2 talked about
        public How How;

        public List<object> ToList() { return new List<object> { Girl, Fact, (int)How, From, Day, Opinion, Distort, Told, ToPlayer }; }

        public static LoreKnow FromList(List<object> l)
        {
            if (l == null || l.Count < 9) return null;
            Func<int, int> i = n => l[n] is double ? (int)(double)l[n] : 0;
            return new LoreKnow
            {
                Girl = l[0] != null ? l[0].ToString() : "", Fact = i(1), How = (How)Math.Max(0, Math.Min(3, i(2))), From = l[3] != null ? l[3].ToString() : "",
                Day = i(4), Opinion = i(5), Distort = i(6), Told = i(7), ToPlayer = i(8)
            };
        }
    }

    internal sealed partial class CampusState
    {
        public List<LoreFact> Lore = new List<LoreFact>();
        public List<LoreKnow> Known = new List<LoreKnow>();
        public int NextFactId = 1;
        public int LoreSpreadStamp = -1;
        public Dictionary<string, int> MetDay = new Dictionary<string, int>();   // girl -> day you first talked
        public List<string> Dating = new List<string>();                         // girls dating you at the last check
        public List<string> LinesSaid = new List<string>();                      // "rule|girl|day|stamp" (no repeats)
        public List<string> TopicsDone = new List<string>();                     // "girl|topic|day" (a topic used is gone)

        private void WriteLore(Dictionary<string, object> root)
        {
            root["lore"] = Lore.Select(f => (object)f.ToList()).ToList();
            root["know"] = Known.Select(k => (object)k.ToList()).ToList();
            root["loreN"] = NextFactId;
            root["loreS"] = LoreSpreadStamp;
            root["met"] = JsonLists.IntDict(MetDay);
            root["dating"] = Dating.Cast<object>().ToList();
            root["said"] = LinesSaid.Cast<object>().ToList();
            root["tdone"] = TopicsDone.Cast<object>().ToList();
            WriteAch(root);
        }

        private void ReadLore(Dictionary<string, object> d)
        {
            var l = d.GetList("lore");
            Lore = l == null ? new List<LoreFact>() : l.OfType<List<object>>().Select(LoreFact.FromList).Where(f => f != null).ToList();
            var k = d.GetList("know");
            Known = k == null ? new List<LoreKnow>() : k.OfType<List<object>>().Select(LoreKnow.FromList).Where(x => x != null).ToList();
            NextFactId = Math.Max(1, d.GetInt("loreN", 1));
            LoreSpreadStamp = d.GetInt("loreS", -1);
            MetDay = JsonLists.ReadIntDict(d.GetObj("met"));
            Dating = JsonLists.Strings(d.GetList("dating"));
            LinesSaid = JsonLists.Strings(d.GetList("said"));
            TopicsDone = JsonLists.Strings(d.GetList("tdone"));
            ReadAch(d);
        }
    }
}
