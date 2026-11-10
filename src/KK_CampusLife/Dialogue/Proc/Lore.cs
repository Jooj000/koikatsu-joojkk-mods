using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>A verb of the memory: how it reads, how much it weighs, what a rumor turns it into.</summary>
    internal sealed class VerbInfo
    {
        public string Id, Phrase, Distort = "", Tags = "";
        public int Valence, Weight;
    }

    /// <summary>
    /// The one memory of the procedural dialogue: facts and who knows each
    /// one, and how (in it, saw it, was told by someone, a rumor). Talk topics, gossip and her lines all read from here;
    /// the old systems write here through small adapters (Social.Add, Memories.Add, Rumors.Start, pairs, gifts, H).
    /// </summary>
    internal static class Lore
    {
        public const string Player = "player";
        private static CampusState S { get { return Campus.S; } }

        // ================= verbs =================

        private static readonly Dictionary<string, VerbInfo> Verbs = new Dictionary<string, VerbInfo>();

        private static void V(string id, int valence, int weight, string phrase, string distort = "", string tags = "")
        {
            Verbs[id] = new VerbInfo { Id = id, Valence = valence, Weight = weight, Phrase = phrase, Distort = distort, Tags = tags };
        }

        static Lore()
        {
            // {s} {o} {a}: the people; {was} {has} {is}: agree with {s}; {s_pos}: your/my/her; {note}: free text.
            V("met", 0, 0, "{s} met {o}");
            V("talked", 1, 0, "{s} had a nice talk with {o}");
            V("helped", 2, 1, "{s} helped {o} out", "flirted", "kind");
            V("insulted", -2, 1, "{s} {was} rude to {o}", "fought", "fight");
            V("ignored", -1, 1, "{s} walked right past {o} when she needed a hand", "insulted", "fight");
            V("kept_promise", 2, 1, "{s} kept {s_pos} promise to {o}", "", "promise");
            V("broke_promise", -2, 2, "{s} broke {s_pos} promise to {o}", "", "promise");
            V("gave_gift", 2, 1, "{s} gave {o} {note}", "flirted", "gift");
            V("flirted", 1, 1, "{s} {was} flirting with {o}", "kissed", "romance");
            V("held_hands", 1, 1, "{s} held hands with {o}", "kissed", "romance");
            V("touched", 1, 1, "{s} {was} getting handsy with {o}", "kissed", "romance");
            V("kissed", 2, 2, "{s} kissed {o}", "had_h", "romance");
            V("confessed", 2, 2, "{s} confessed to {o}", "", "romance");
            V("rejected", -2, 2, "{s} turned {o} down", "", "romance");
            V("started_dating", 3, 3, "{s} and {o} started going out", "engaged", "romance");
            V("engaged", 3, 3, "{s} and {o} are secretly engaged", "", "romance");
            V("broke_up", -3, 3, "{s} and {o} broke up", "cheated", "romance,fight");
            V("dated", 2, 2, "{s} went on a date with {o}", "kissed", "romance");
            V("had_h", 2, 3, "{s} and {o} did it", "", "h");
            V("fought", -2, 2, "{s} and {o} had a fight", "", "fight");
            V("made_up", 2, 1, "{s} and {o} made up", "", "kind");
            V("seen_girls_room", -2, 2, "{s} {was} seen in the girls' restroom", "", "peep");
            V("seen_lockers", -2, 2, "{s} {was} seen in the girls' changing room", "", "peep");
            V("seen_boys_room", -1, 1, "{s} {was} caught in the boys' restroom", "", "peep");
            V("stood_up_for", 2, 1, "{s} stood up for {o}", "", "kind");
            V("cheated", -3, 3, "{s} cheated on {o}", "", "romance,fight");
            V("left_for", -3, 3, "{s} left {o} for {a}", "cheated", "romance,fight");
            V("crush", 1, 2, "{s} {has} a crush on {o}", "confessed", "romance");
            V("seen_together", 1, 2, "{s} and {o} were seen together behind the gym", "kissed", "romance");
            V("shared", 1, 1, "{note}", "", "us");
            V("ate_with", 1, 0, "{s} had lunch with {o}", "", "food");
            V("studied_with", 1, 0, "{s} studied with {o}", "", "study");
            V("exercised_with", 1, 0, "{s} worked out with {o}", "", "sport");
            V("walked_home", 1, 1, "{s} walked {o} home", "dated", "romance");
            V("club_with", 1, 0, "{s} did club activities with {o}", "", "club");
            V("council", 0, 2, "{s} and {o} are running for the council", "", "school");
        }

        public static VerbInfo Verb(string id)
        {
            VerbInfo v;
            return id != null && Verbs.TryGetValue(id, out v) ? v : Verbs["talked"];
        }

        // ================= writing =================

        /// <summary>
        /// Writes a fact. The girls in it know at once (Participant), the witnesses saw it; the same thing between the
        /// same people on the same day is one fact (more witnesses). Returns the fact (null outside the game).
        /// </summary>
        public static LoreFact Record(string verb, string subject, string obj, string about = "", int? valence = null, int? weight = null,
            int exposure = LoreFact.Known, string note = "", IEnumerable<SaveData.Heroine> witnesses = null)
        {
            try
            {
                if (S == null) return null;
                var v = Verb(verb);
                var f = S.Lore.LastOrDefault(x => x.Verb == v.Id && x.Day == S.Day && x.Subject == (subject ?? "") && x.Obj == (obj ?? "") && x.About == (about ?? ""));
                var fresh = f == null;
                if (fresh)
                {
                    f = new LoreFact
                    {
                        Id = S.NextFactId++, Day = S.Day, Mask = (int)G.CurrentMask, Place = PlaceHere(), Subject = subject ?? "", Verb = v.Id, Obj = obj ?? "",
                        About = about ?? "", Valence = valence ?? v.Valence, Weight = weight ?? v.Weight, Exposure = exposure, Tags = v.Tags, Note = note ?? ""
                    };
                    if (v.Id == "started_dating" || v.Id == "had_h" || v.Id == "kissed" || v.Id == "confessed")
                    {
                        var girl = f.Subject == Player ? f.Obj : f.Obj == Player ? f.Subject : "";
                        int met;
                        if (girl != "" && S.MetDay.TryGetValue(girl, out met)) f.Since = Math.Max(0, S.Day - met);
                    }
                    S.Lore.Add(f);
                    foreach (var k in new[] { f.Subject, f.Obj })
                        if (!string.IsNullOrEmpty(k) && k != Player) Learn(k, f, How.Participant);
                    CampusLifePlugin.Log.LogInfo("Lore #" + f.Id + ": " + Render(f, "", 0) + (f.Since >= 0 ? " (" + f.Since + " days after meeting)" : "") + " [" + f.Place + "]");
                }
                if (witnesses != null)
                    foreach (var w in witnesses.Where(w => w != null && GirlInfo.IsRegular(w)).Distinct().ToList())
                        Learn(GirlInfo.Key(w), f, How.Witness);
                return f;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Lore record: " + e.Message); return null; }
        }

        private static string PlaceHere()
        {
            try
            {
                var id = Places.CurrentId;
                if (!string.IsNullOrEmpty(id)) return Places.Name(id).ToLowerInvariant();
                var l = Places.MapLabel(G.PlayerMapNo);
                // An untranslated (Japanese) map name would land inside an English line.
                return string.IsNullOrEmpty(l) || l.StartsWith("map ", StringComparison.Ordinal) || l.Any(ch => ch > 127) ? "hallway" : l.ToLowerInvariant();
            }
            catch { return "hallway"; }
        }

        /// <summary>She knows this fact now, by the best way she has (being in it beats seeing it, seeing beats hearing).</summary>
        public static LoreKnow Learn(string girl, LoreFact f, How how, string from = "", int distort = 0)
        {
            if (S == null || f == null || string.IsNullOrEmpty(girl) || girl == Player) return null;
            var k = S.Known.FirstOrDefault(x => x.Fact == f.Id && x.Girl == girl);
            if (k != null)
            {
                if (how < k.How) { k.How = how; k.From = from ?? ""; k.Distort = 0; }
                return k;
            }
            k = new LoreKnow { Girl = girl, Fact = f.Id, How = how, From = from ?? "", Day = S.Day, Distort = distort, Opinion = OpinionOf(girl, f) };
            S.Known.Add(k);
            return k;
        }

        /// <summary>What she thinks of it: what it did to her, to her friends or rivals, or to you if she likes you.</summary>
        private static int OpinionOf(string girl, LoreFact f)
        {
            if (f.Obj == girl || f.Subject == girl) return f.Valence;
            var h = GirlInfo.Find(girl);
            if (h == null) return f.Valence / 2;
            var op = 0;
            foreach (var other in new[] { f.Subject, f.Obj })
            {
                if (string.IsNullOrEmpty(other) || other == Player) continue;
                var o = GirlInfo.Find(other);
                if (o == null) continue;
                var b = Social.Bond(h, o);
                op += b >= 40 ? f.Valence : b <= -30 ? -f.Valence : 0;
            }
            // Romance of yours with another girl stings a girl who likes you.
            if ((f.Tags.Contains("romance") || f.Tags.Contains("h")) && (f.Subject == Player || f.Obj == Player) && (h.isGirlfriend || h.favor >= 60)) op -= 2;
            return Mathf.Clamp(op == 0 ? f.Valence / 2 : op, -3, 3);
        }

        // ================= reading =================

        public static LoreFact Get(int id) { return S == null ? null : S.Lore.FirstOrDefault(f => f.Id == id); }

        public static IEnumerable<KeyValuePair<LoreKnow, LoreFact>> Of(string girl)
        {
            if (S == null) yield break;
            foreach (var k in S.Known)
            {
                if (k.Girl != girl) continue;
                var f = Get(k.Fact);
                if (f != null) yield return new KeyValuePair<LoreKnow, LoreFact>(k, f);
            }
        }

        public static LoreKnow KnowOf(string girl, LoreFact f) { return S == null || f == null ? null : S.Known.FirstOrDefault(k => k.Girl == girl && k.Fact == f.Id); }

        /// <summary>The player knows it: was in it, or some girl told it to him (the "tell" topic passes it on).</summary>
        public static bool PlayerKnows(LoreFact f) { return S != null && f != null && (f.Involves(Player) || S.Known.Any(k => k.Fact == f.Id && k.ToPlayer >= 1)); }

        /// <summary>How much she wants to talk about it: weight, how fresh, how close to her, how strong her feeling.</summary>
        public static float Salience(LoreKnow k, LoreFact f)
        {
            var age = Mathf.Max(0, S.Day - f.Day);
            var fresh = 1f / (1f + age / (1.5f + f.Weight * 2.5f));
            var rel = f.Involves(k.Girl) ? 2f : f.Involves(Player) ? 1.6f : 1f;
            var h = GirlInfo.Find(k.Girl);
            if (h != null && rel < 1.5f)
                foreach (var o in new[] { f.Subject, f.Obj })
                {
                    var oh = string.IsNullOrEmpty(o) || o == Player ? null : GirlInfo.Find(o);
                    if (oh != null && Math.Abs(Social.Bond(h, oh)) >= 40) { rel = 1.3f; break; }
                }
            var told = k.ToPlayer == 0 ? 1f : k.ToPlayer == 1 ? 0.45f : 0f;
            return (1f + f.Weight) * fresh * rel * (1f + Mathf.Abs(k.Opinion) / 3f) * told;
        }

        /// <summary>She told you about it (1) or you two talked it through (2); a topic used is gone until the fact changes.</summary>
        public static void Told(string girl, LoreFact f, int level = 2)
        {
            var k = KnowOf(girl, f);
            if (k != null && k.ToPlayer < level) k.ToPlayer = level;
        }

        // ================= rendering =================

        /// <summary>The name of a person in a fact, from <paramref name="viewer"/>'s mouth, talking to the player.</summary>
        public static string Name(string key, string viewer, bool subject)
        {
            if (string.IsNullOrEmpty(key)) return subject ? "someone" : "someone";
            if (key == Player) return "you";
            if (key == viewer) return subject ? "I" : "me";
            var n = GirlInfo.FirstName(key);
            return string.IsNullOrEmpty(n) || n == "?" ? "someone" : n;
        }

        /// <summary>The fact told by <paramref name="viewer"/>; a rumor (<paramref name="distort"/> 1) is told the way it reached her.</summary>
        public static string Render(LoreFact f, string viewer, int distort)
        {
            var v = Verb(f.Verb);
            if (distort > 0 && !string.IsNullOrEmpty(v.Distort)) v = Verb(v.Distort);
            var s = Name(f.Subject, viewer, true);
            var o = Name(f.Obj, viewer, false);
            // "you and I" sounds better than "I and you" for the pairs.
            var t = v.Phrase;
            if (t.StartsWith("{s} and {o}", StringComparison.Ordinal) && s == "I") { s = o == "you" ? "you" : o; o = "me"; t = "{s} and I" + t.Substring(11); }
            var note = string.IsNullOrEmpty(f.Note) ? (v.Id == "gave_gift" ? "a present" : "") : f.Note;
            var you = s == "you";
            var me = s == "I";
            t = t.Replace("{s}", s).Replace("{o}", o).Replace("{a}", Name(f.About, viewer, false)).Replace("{note}", note)
                 .Replace("{was}", you ? "were" : "was").Replace("{has}", you || me ? "have" : "has").Replace("{is}", you ? "are" : me ? "am" : "is")
                 .Replace("{s_pos}", you ? "your" : me ? "my" : "her");
            return t;
        }

        /// <summary>"today", "yesterday", "on Monday", "last week".</summary>
        public static string When(LoreFact f)
        {
            var age = S.Day - f.Day;
            if (age <= 0) return "today";
            if (age == 1) return "yesterday";
            if (age < 7) return "the other day";
            return age < 14 ? "last week" : "a while back";
        }

        // ================= spreading =================

        /// <summary>
        /// Once a period: each girl tells what she knows to the girls she is close to (by bond, how big it is and how
        /// much of a talker she is). Secrets only pass between close friends; a retelling may twist the verb, and the
        /// twist stays marked so the player can correct it.
        /// </summary>
        public static void OnPeriod()
        {
            try
            {
                if (S == null) return;
                var stamp = S.Day * 16 + (int)G.CurrentMask;
                if (S.LoreSpreadStamp == stamp) return;
                S.LoreSpreadStamp = stamp;
                WatchDating();
                // (a dictionary by key broke on the save's duplicate cards, two Elma and two Rose: the retelling never ran)
                var girls = Social.Students.ToList();
                var added = 0;
                foreach (var g in girls.OrderBy(_ => Random.value))
                {
                    var key = GirlInfo.Key(g);
                    var talk = Talker(g);
                    var mine = S.Known.Where(k => k.Girl == key && k.Told < 4).Select(k => new { K = k, F = Get(k.Fact) })
                        .Where(x => x.F != null && x.F.Weight >= 1 && S.Day - x.F.Day <= 7).OrderByDescending(x => x.F.Weight * 10 - (S.Day - x.F.Day)).Take(2).ToList();
                    foreach (var x in mine)
                    {
                        var friends = girls.Where(o => o != g && Social.Bond(g, o) >= (x.F.Exposure == LoreFact.Secret ? 70 : 30)).OrderByDescending(o => Social.Bond(g, o)).Take(3).ToList();   // her closest friends first
                        foreach (var o in friends)
                        {
                            var ok = GirlInfo.Key(o);
                            if (x.F.Involves(ok) || S.Known.Any(k => k.Girl == ok && k.Fact == x.F.Id)) continue;
                            // Context, not a roll: how close they are, how big it is, how much she talks, and the days it
                            // has had to come up; a story she only heard as a rumor gets twisted by a chatty teller.
                            var chance = 0.12f + Social.Bond(g, o) / 250f + x.F.Weight * 0.08f + talk + (S.Day - x.F.Day) * 0.06f;
                            if (!Consent.Yes(g, "lore:" + x.F.Id + ":" + ok, chance)) continue;
                            var twist = x.K.Distort > 0 || x.K.How == How.Rumor && Social.Chatty(g) ? 1 : 0;
                            Learn(ok, x.F, twist > 0 ? How.Rumor : How.Heard, key, twist);
                            x.K.Told++;
                            if (++added >= 150) break;
                        }
                        if (added >= 150) break;
                    }
                    if (added >= 150) break;
                }
                if (added > 0) CampusLifePlugin.Log.LogInfo("Lore: " + added + " retellings this period (" + S.Lore.Count + " facts, " + S.Known.Count + " known)");
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("Lore spread: " + e.Message); }
        }

        /// <summary>How much of a talker she is: friendly and lively girls pass things on, loners and serious ones less.</summary>
        private static float Talker(SaveData.Heroine h)
        {
            var a = GirlInfo.Quirks(h);
            var t = 0f;
            if (a.friendly) t += 0.12f;
            if (a.kappatu) t += 0.08f;
            if (a.hitori) t -= 0.12f;
            if (a.majime) t -= 0.05f;
            if (GirlInfo.ToneOf(h) == Tone.Energetic || GirlInfo.ToneOf(h) == Tone.Teasing) t += 0.05f;
            return t;
        }

        /// <summary>Dating starts and ends are facts (the game sets the flag itself, after its confession or break-up).</summary>
        public static void WatchDating()
        {
            if (S == null) return;
            foreach (var h in Social.Students)
            {
                var key = GirlInfo.Key(h);
                var was = S.Dating.Contains(key);
                if (h.isGirlfriend && !was) { S.Dating.Add(key); Record("started_dating", Player, key); }
                else if (!h.isGirlfriend && was) { S.Dating.Remove(key); Record("broke_up", Player, key); }
            }
        }

        /// <summary>The first talk with her: the day you met (the speed of things is counted from here).</summary>
        public static void Met(SaveData.Heroine h)
        {
            if (S == null || h == null) return;
            var key = GirlInfo.Key(h);
            if (S.MetDay.ContainsKey(key)) return;
            S.MetDay[key] = S.Day;
            Record("met", Player, key);
        }

        /// <summary>Daily: small old facts are forgotten; big ones stay as history.</summary>
        public static void Prune()
        {
            if (S == null) return;
            var keep = new[] { 3, 10, 40, 150 };
            var gone = new HashSet<int>(S.Lore.Where(f => S.Day - f.Day > keep[Mathf.Clamp(f.Weight, 0, 3)]).Select(f => f.Id));
            if (S.Lore.Count - gone.Count > 400)
                foreach (var f in S.Lore.Where(f => !gone.Contains(f.Id)).OrderBy(f => f.Weight).ThenBy(f => f.Day).Take(S.Lore.Count - gone.Count - 400)) gone.Add(f.Id);
            if (gone.Count > 0)
            {
                S.Lore.RemoveAll(f => gone.Contains(f.Id));
                S.Known.RemoveAll(k => gone.Contains(k.Fact));
            }
            S.LinesSaid.RemoveAll(l => { var p = l.Split('|'); int d; return p.Length < 3 || !int.TryParse(p[2], out d) || S.Day - d > 6; });
            S.TopicsDone.RemoveAll(l => { var p = l.Split('|'); int d; return p.Length < 3 || !int.TryParse(p[2], out d) || S.Day - d > 10; });
        }

        // ================= adapters (the old systems write here) =================

        /// <summary>Social.Add's incidents as facts about you.</summary>
        public static void FromIncident(string kind, SaveData.Heroine girl, IEnumerable<SaveData.Heroine> witnesses, string what = null)
        {
            var g = girl != null ? GirlInfo.Key(girl) : "";
            switch (kind)
            {
                case Social.Kinds.Helped: Record("helped", Player, g, witnesses: witnesses); break;
                case Social.Kinds.Rude: Record("insulted", Player, g, witnesses: witnesses); break;
                case Social.Kinds.Ignored: Record("ignored", Player, g, witnesses: witnesses); break;
                case Social.Kinds.KeptPromise: Record("kept_promise", Player, g, witnesses: witnesses); break;
                case Social.Kinds.BrokePromise: Record("broke_promise", Player, g, witnesses: witnesses); break;
                case Social.Kinds.GoodTalk: Record("talked", Player, g, witnesses: witnesses); break;
                case Social.Kinds.SeenGirlsRoom: Record("seen_girls_room", Player, "", witnesses: girl != null ? (witnesses ?? new SaveData.Heroine[0]).Concat(new[] { girl }) : witnesses); break;
                case Social.Kinds.SeenLockers: Record("seen_lockers", Player, "", witnesses: girl != null ? (witnesses ?? new SaveData.Heroine[0]).Concat(new[] { girl }) : witnesses); break;
                case Social.Kinds.HerBoysRoom: Record("seen_boys_room", g, "", exposure: LoreFact.Secret, witnesses: witnesses); break;
                case Social.Kinds.Defended: Record("stood_up_for", Player, g, witnesses: witnesses); break;
                case Social.Kinds.Flirted:
                    Record(what == Witness.Kiss ? "kissed" : what == Witness.Hand ? "held_hands" : what == Witness.Touch ? "touched" : "flirted", Player, g, witnesses: witnesses);
                    break;
            }
        }

        /// <summary>A shared memory (Memories.Add): a fact between you two; its text is how she tells it.</summary>
        public static void FromMemory(SaveData.Heroine h, string kind, string text, int tone, string with)
        {
            if (h == null) return;
            var verb = kind == "lunch" ? "ate_with" : kind == "walk" ? "walked_home" : kind == "confession" ? "confessed" : "shared";
            var f = Record(verb, Player, GirlInfo.Key(h), with ?? "", Mathf.Clamp(tone, -3, 3), null, LoreFact.Known, text);
            if (f != null && verb == "shared" && f.Tags.IndexOf(kind ?? "", StringComparison.Ordinal) < 0) f.Tags += "," + kind;
        }

        /// <summary>A rumor starting (Rumors.Start): a rumored fact; the girl who tells it saw it, the ones in it know.</summary>
        public static void FromRumor(string kind, string subject, string other, string teller)
        {
            LoreFact f = null;
            switch (kind)
            {
                case "stole": f = Record("left_for", other, subject, Player, null, null, LoreFact.Rumored); break;
                case "cheated": f = Record("cheated", subject, teller, "", null, null, LoreFact.Rumored); break;
                case "caught": f = Record("seen_together", subject, other, "", null, null, LoreFact.Rumored); break;
                case "crush": f = Record("crush", subject, other, "", null, null, LoreFact.Rumored); break;
            }
            if (f != null && !string.IsNullOrEmpty(teller) && !f.Involves(teller)) Learn(teller, f, How.Witness);
        }

        /// <summary>H with you: the fact (with how soon after meeting), and the girls close by saw it.</summary>
        public static void FromH(SaveData.Heroine h)
        {
            if (h == null) return;
            List<SaveData.Heroine> near;
            // Only the ones who really saw or heard it (Witness: in range, looking, nothing in between).
            try { near = Witness.Who(h, Witness.H); } catch { near = new List<SaveData.Heroine>(); }
            // The ones who ran off while it went on (the escape fires when H starts, before this end) saw it too.
            foreach (var kv in Fled.Where(x => Time.realtimeSinceStartup - x.Value <= 900f).ToList())
            {
                var w = GirlInfo.Find(kv.Key);
                if (w != null && w != h && !near.Contains(w)) near.Add(w);
            }
            Fled.Clear();
            var f = Record("had_h", Player, GirlInfo.Key(h), witnesses: near);
            _lastH = f;
            _lastHAt = Time.realtimeSinceStartup;
            if (near.Count > 0) Achievements.Count("h:seen", h);
            if (near.Count > 0) CampusLifePlugin.Log.LogInfo("Lore: H seen by " + string.Join(", ", near.Select(GirlInfo.FirstName).ToArray()));
        }

        private static LoreFact _lastH;
        private static float _lastHAt = -999f;
        private static readonly Dictionary<string, float> Fled = new Dictionary<string, float>();

        /// <summary>A girl running off just after H in the open saw it (Reactions' EscapeAction postfix).</summary>
        public static void OnEscape(SaveData.Heroine h)
        {
            if (h == null || !GirlInfo.IsRegular(h)) return;
            var key = GirlInfo.Key(h);
            if (_lastH == null || Time.realtimeSinceStartup - _lastHAt > 120f) { Fled[key] = Time.realtimeSinceStartup; return; }
            if (_lastH.Involves(key)) return;
            if (KnowOf(key, _lastH) == null) CampusLifePlugin.Log.LogInfo("Lore: " + GirlInfo.FirstName(h) + " ran off: she saw the H (#" + _lastH.Id + ")");
            Learn(key, _lastH, How.Witness);
        }

        private static IEnumerable<SaveData.Heroine> NearPlayer(float meters)
        {
            var act = G.Act;
            if (act == null || act.Map == null || act.Player == null) yield break;
            var pos = act.Player.position;
            foreach (var n in act.npcList)
            {
                if (n == null || n.heroine == null || n.mapNo != act.Map.no || !GirlInfo.IsRegular(n.heroine)) continue;
                if ((n.position - pos).magnitude <= meters) yield return n.heroine;
            }
        }


        public static string Describe(SaveData.Heroine h)
        {
            if (h == null || S == null) return "no girl";
            var key = GirlInfo.Key(h);
            var sb = new StringBuilder();
            sb.Append(GirlInfo.FirstName(h)).Append(" knows ").Append(S.Known.Count(k => k.Girl == key)).Append(" facts:");
            foreach (var kv in Of(key).OrderByDescending(x => Salience(x.Key, x.Value)).Take(12))
            {
                var k = kv.Key; var f = kv.Value;
                sb.Append("\n  #").Append(f.Id).Append(' ').Append(Render(f, key, k.Distort)).Append(" (").Append(When(f)).Append(", ").Append(f.Place).Append(") ")
                  .Append(k.How).Append(k.How == How.Heard || k.How == How.Rumor ? " from " + (k.From == Player ? "you" : GirlInfo.FirstName(k.From)) : "")
                  .Append(", op ").Append(k.Opinion).Append(", told you ").Append(k.ToPlayer).Append(", sal ").Append(Salience(k, f).ToString("0.0"));
            }
            return sb.ToString();
        }
    }
}
