using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// The social director: who is friends with whom, what happened between the player and each girl (incidents),
    /// how word spreads between friends, trust, and friendship jealousy. Conversations, the board and the two-girl
    /// scenes all read from here.
    /// </summary>
    internal static class Social
    {
        public static class Kinds
        {
            public const string Helped = "helped";            // you helped her
            public const string Rude = "rude";                // you were unkind to her
            public const string Ignored = "ignored";          // you walked past when she needed a hand
            public const string KeptPromise = "kept";         // you did what she asked
            public const string BrokePromise = "broke";       // you did not
            public const string GoodTalk = "good_talk";       // she enjoyed talking with you
            public const string SeenGirlsRoom = "girls_room"; // you were seen in the girls' restroom
            public const string SeenLockers = "lockers";      // you were seen in the girls' changing room
            public const string HerBoysRoom = "her_boys_room";// you found her in the boys' restroom (her secret)
            public const string Defended = "defended";        // you took her side
            public const string Flirted = "flirted";          // you were sweet on her (girlfriends may mind)
        }

        private static CampusState S { get { return Campus.S; } }

        // ---------------- girls ----------------

        private static int _cacheFrame = -1;
        private static Dictionary<string, SaveData.Heroine> _byKey = new Dictionary<string, SaveData.Heroine>();

        public static SaveData.Heroine Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (_cacheFrame != Time.frameCount)
            {
                _cacheFrame = Time.frameCount;
                _byKey = new Dictionary<string, SaveData.Heroine>();
                foreach (var h in G.Heroines) if (h != null) _byKey[GirlInfo.Key(h)] = h;
            }
            SaveData.Heroine r;
            return _byKey.TryGetValue(key, out r) ? r : null;
        }

        /// <summary>Girls that take part in school life (no teachers, no graduated story girls).</summary>
        public static IEnumerable<SaveData.Heroine> Students { get { return G.Heroines.Where(h => h != null && GirlInfo.IsRegular(h)); } }

        // ---------------- friendships ----------------

        /// <summary>
        /// How close two girls are (-100..100): a stable base from the class, the club and a per-pair roll,
        /// plus whatever stories changed (stored in GirlState.Bonds).
        /// </summary>
        public static int Bond(SaveData.Heroine a, SaveData.Heroine b)
        {
            if (a == null || b == null || a == b) return 0;
            var ka = GirlInfo.Key(a);
            var kb = GirlInfo.Key(b);
            var v = BaseBond(a, b, ka, kb);
            int extra;
            if (S.Girl(ka).Bonds.TryGetValue(kb, out extra)) v += extra;
            return Mathf.Clamp(v, -100, 100);
        }

        private static int BaseBond(SaveData.Heroine a, SaveData.Heroine b, string ka, string kb)
        {
            var pair = string.CompareOrdinal(ka, kb) < 0 ? ka + "/" + kb : kb + "/" + ka;
            var roll = (int)((uint)pair.GetHashCode() % 61) - 20; // -20..40
            if (a.schoolClass == b.schoolClass && a.schoolClass >= 0) roll += 15;
            if (!GirlInfo.InNoClub(a) && a.clubActivities == b.clubActivities) roll += 25;
            if (GirlInfo.ToneOf(a) == GirlInfo.ToneOf(b)) roll += 5;
            return roll;
        }

        public static List<SaveData.Heroine> FriendsOf(SaveData.Heroine h, int min = 35)
        {
            return Students.Where(o => o != h).Select(o => new { o, b = Bond(h, o) }).Where(x => x.b >= min)
                .OrderByDescending(x => x.b).Select(x => x.o).ToList();
        }

        public static SaveData.Heroine BestFriend(SaveData.Heroine h)
        {
            return FriendsOf(h, 50).FirstOrDefault();
        }

        // ---------------- trust ----------------

        public static int Trust(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Trust < 0) gs.Trust = Mathf.Clamp(20 + h.favor / 4 + (h.relation >= 1 ? 10 : 0), 0, 100);
            return gs.Trust;
        }

        public static int AddTrust(SaveData.Heroine h, int d)
        {
            if (h == null || d == 0) return 0;
            var gs = S.Girl(GirlInfo.Key(h));
            var before = Trust(h);
            gs.Trust = Mathf.Clamp(before + d, 0, 100);
            return gs.Trust - before;
        }

        public static string TrustLabel(int t)
        {
            return t >= 80 ? "Confides in you" : t >= 60 ? "Trusts you" : t >= 40 ? "Comfortable" : t >= 20 ? "Guarded" : "Wary";
        }

        // ---------------- incidents ----------------

        public static IEnumerable<SaveData.Heroine> GirlsOnMyMap()
        {
            var act = G.Act;
            if (act == null || act.Map == null) return Enumerable.Empty<SaveData.Heroine>();
            var map = act.Map.no;
            return act.npcList.Where(n => n != null && n.heroine != null && n.mapNo == map && GirlInfo.IsRegular(n.heroine)).Select(n => n.heroine);
        }

        /// <summary>Records an incident. The girl and the witnesses know at once; friends hear about it in the following days.</summary>
        public static Incident Add(string kind, SaveData.Heroine girl, IEnumerable<SaveData.Heroine> witnesses = null)
        {
            if (S == null) return null;
            // Seen in the girls' restroom or changing room again within a day: the same story, more witnesses (before,
            // every visit was a new incident, each one spread and had to be explained again, 2026-10-11).
            if (Merges(kind))
            {
                var key = girl != null ? GirlInfo.Key(girl) : "";
                var same = S.Incidents.LastOrDefault(i => i.Kind == kind && i.Girl == key && S.Day - i.Day <= 1);
                if (same != null)
                {
                    if (girl != null) Learn(girl, same, false);
                    if (witnesses != null)
                        foreach (var w in witnesses.Where(w => w != null && w != girl).Distinct().ToList()) Learn(w, same, true);
                    return same;
                }
            }
            var inc = new Incident { Id = S.NextIncidentId++, Day = S.Day, Kind = kind, Girl = girl != null ? GirlInfo.Key(girl) : "" };
            S.Incidents.Add(inc);
            if (S.Incidents.Count > 120) S.Incidents.RemoveRange(0, S.Incidents.Count - 120);
            var seen = 0;
            if (girl != null) { Learn(girl, inc, false); seen++; }
            if (witnesses != null)
                foreach (var w in witnesses.Where(w => w != null && w != girl).Distinct().ToList()) { Learn(w, inc, true); seen++; }
            var rep = RepDelta(kind);
            if (rep != 0 && seen > 0) S.Reputation = Mathf.Clamp(S.Reputation + rep, 0, 100);
            return inc;
        }

        /// <summary>Kinds that are one story however many times it happens in a day (being seen where you shouldn't be).</summary>
        private static bool Merges(string kind) { return kind == Kinds.SeenGirlsRoom || kind == Kinds.SeenLockers || kind == Kinds.HerBoysRoom; }

        /// <summary>The same story: same kind, about the same girl, within a couple of days.</summary>
        private static bool SameStory(Incident a, Incident b) { return a != null && b != null && a.Kind == b.Kind && a.Girl == b.Girl && Math.Abs(a.Day - b.Day) <= 2; }

        /// <summary>She was already told the truth about this story (an incident of it is cleared for her).</summary>
        private static bool AlreadyExplained(GirlState gs, Incident inc)
        {
            foreach (var id in gs.Cleared)
            {
                var c = S.Incidents.FirstOrDefault(i => i.Id == id);
                if (SameStory(c, inc)) return true;
            }
            return false;
        }

        private static int RepDelta(string kind)
        {
            switch (kind)
            {
                case Kinds.Helped: case Kinds.Defended: return 1;
                case Kinds.KeptPromise: return 1;
                case Kinds.Rude: return -2;
                case Kinds.BrokePromise: return -1;
                case Kinds.SeenGirlsRoom: case Kinds.SeenLockers: return -3;
                default: return 0;
            }
        }

        /// <summary>What hearing about this incident does to her trust in you.</summary>
        public static int Effect(SaveData.Heroine listener, Incident inc)
        {
            var about = Get(inc.Girl);
            var self = about == listener;
            var bond = self || about == null ? 0 : Bond(listener, about);
            var friend = bond >= 35;
            var rival = bond <= -30;
            switch (inc.Kind)
            {
                case Kinds.Helped: return self ? 6 : friend ? 3 : rival ? -1 : 1;
                case Kinds.Defended: return self ? 8 : friend ? 3 : rival ? -2 : 1;
                case Kinds.KeptPromise: return self ? 5 : friend ? 2 : 0;
                case Kinds.GoodTalk: return self ? 0 : friend ? 1 : 0;
                case Kinds.Rude: return self ? -8 : friend ? -4 : rival ? 1 : -1;
                case Kinds.Ignored: return self ? -3 : friend ? -1 : 0;
                case Kinds.BrokePromise: return self ? -8 : friend ? -3 : -1;
                case Kinds.SeenGirlsRoom: case Kinds.SeenLockers: return -4;
                case Kinds.Flirted:
                    if (self) return 1;
                    if (listener.isGirlfriend) return ConvoLines.MindsCheating(listener) ? -6 : 0;
                    return listener.favor >= 70 ? -1 : 0;
                default: return 0;
            }
        }

        private static void Learn(SaveData.Heroine h, Incident inc, bool mention)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Known.Contains(inc.Id)) return;
            gs.Known.Add(inc.Id);
            if (gs.Known.Count > 60) gs.Known.RemoveRange(0, gs.Known.Count - 60);
            // She already heard your side of this story: hearing it again changes nothing and she won't bring it up.
            if (AlreadyExplained(gs, inc)) { if (!gs.Cleared.Contains(inc.Id)) gs.Cleared.Add(inc.Id); return; }
            var e = Effect(h, inc);
            // Your girlfriend's one-time pass (she owed you one): she lets this one go.
            if (e <= -3 && h.isGirlfriend && Reactions.UsePass(h, inc)) { if (!gs.Cleared.Contains(inc.Id)) gs.Cleared.Add(inc.Id); return; }
            AddTrust(h, e);
            // Something about herself she only brings up when it matters; news about others always comes up.
            if (mention || Get(inc.Girl) != h || inc.Kind == Kinds.HerBoysRoom || inc.Kind == Kinds.BrokePromise) gs.Unmentioned.Add(inc.Id);
            if (gs.Unmentioned.Count > 8) gs.Unmentioned.RemoveAt(0);
        }

        /// <summary>Once a day: girls tell their friends what they heard (up to a week old; her secrets stay secret).</summary>
        public static void DailySpread()
        {
            if (S == null || S.LastSpreadDay == S.Day) return;
            S.LastSpreadDay = S.Day;
            var girls = Students.ToList();
            var fresh = S.Incidents.Where(i => S.Day - i.Day <= 7 && i.Kind != Kinds.HerBoysRoom).ToList();
            if (fresh.Count == 0) return;
            var told = new List<KeyValuePair<SaveData.Heroine, Incident>>();
            foreach (var teller in girls)
            {
                var known = S.Girl(GirlInfo.Key(teller)).Known;
                var cleared = S.Girl(GirlInfo.Key(teller)).Cleared;
                foreach (var inc in fresh.Where(i => known.Contains(i.Id) && !cleared.Contains(i.Id)))
                    foreach (var f in girls)
                    {
                        if (f == teller) continue;
                        var b = Bond(teller, f);
                        var chance = b >= 50 ? 0.6f : b >= 35 ? 0.35f : b >= 10 ? 0.08f : 0f;
                        if (chance > 0f && Random.value < chance) told.Add(new KeyValuePair<SaveData.Heroine, Incident>(f, inc));
                    }
            }
            foreach (var kv in told) Learn(kv.Key, kv.Value, true);
        }

        /// <summary>The thing she most wants to bring up with you, or null.</summary>
        public static Incident Salient(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            Incident best = null;
            var bestScore = 0;
            foreach (var id in gs.Unmentioned)
            {
                var inc = S.Incidents.FirstOrDefault(i => i.Id == id);
                if (inc == null || S.Day - inc.Day > 6) continue;
                var score = Math.Abs(Effect(h, inc)) + (inc.Kind == Kinds.HerBoysRoom ? 10 : 0) + (S.Day == inc.Day ? 2 : 0);
                if (score > bestScore) { bestScore = score; best = inc; }
            }
            return best;
        }

        public static void Mentioned(SaveData.Heroine h, Incident inc)
        {
            if (inc != null) S.Girl(GirlInfo.Key(h)).Unmentioned.Remove(inc.Id);
        }

        /// <summary>Gossip she heard about others that she has not told you yet.</summary>
        public static Incident Gossip(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Unmentioned.Select(id => S.Incidents.FirstOrDefault(i => i.Id == id))
                .Where(i => i != null && S.Day - i.Day <= 6 && Get(i.Girl) != h && i.Kind != Kinds.HerBoysRoom)
                .OrderByDescending(i => i.Day).FirstOrDefault();
        }

        /// <summary>A negative thing about you she has heard and not cleared up.</summary>
        public static Incident Grudge(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Known.Select(id => S.Incidents.FirstOrDefault(i => i.Id == id))
                .Where(i => i != null && S.Day - i.Day <= 6 && Effect(h, i) < -1 && !gs.Cleared.Contains(i.Id))
                .OrderByDescending(i => i.Day).FirstOrDefault();
        }

        /// <summary>
        /// Something you did to her herself that she has not let go (a grudge over a friend, heard as a rumor, is not hers to
        /// bring up as "what you said to me": a girl you never talked to opened the night market with a fight, 2026-10-12).
        /// </summary>
        public static Incident OwnGrudge(SaveData.Heroine h)
        {
            var g = Grudge(h);
            if (g != null && Get(g.Girl) == h) return g;
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Known.Select(id => S.Incidents.FirstOrDefault(i => i.Id == id))
                .Where(i => i != null && Get(i.Girl) == h && S.Day - i.Day <= 6 && Effect(h, i) < -1 && !gs.Cleared.Contains(i.Id))
                .OrderByDescending(i => i.Day).FirstOrDefault();
        }

        /// <summary>
        /// She knows you well enough to seek you out (invite you somewhere, call you over): met in the game, and you have
        /// actually talked a few times or she likes you. Strangers never start an outing with you.
        /// </summary>
        public static bool KnowsYou(SaveData.Heroine h)
        {
            if (h == null) return false;
            if (h.isGirlfriend || h.relation >= 1) return true;
            return h.relation >= 0 && (S.Girl(GirlInfo.Key(h)).Talks >= 2 || h.favor >= 25);
        }

        /// <summary>The air was cleared about this incident (it no longer counts as a grudge).</summary>
        public static void Cleared(SaveData.Heroine h, Incident inc)
        {
            if (h == null || inc == null) return;
            ClearStory(h, inc);
            // Her friends hear your side from her (the truth spreads too, not only the rumor).
            foreach (var f in FriendsOf(h, 35))
            {
                var fs = S.Girl(GirlInfo.Key(f));
                if (!fs.Known.Any(id => SameStory(S.Incidents.FirstOrDefault(i => i.Id == id), inc))) continue;
                if (Random.value < (Bond(h, f) >= 50 ? 0.85f : 0.5f))
                {
                    ClearStory(f, inc);
                    CampusLifePlugin.Log.LogInfo("Social: " + GirlInfo.FirstName(h) + " told " + GirlInfo.FirstName(f) + " your side (" + inc.Kind + ")");
                }
            }
        }

        /// <summary>Every incident of the same story she knows is cleared and none of them comes up again.</summary>
        private static void ClearStory(SaveData.Heroine h, Incident inc)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            foreach (var id in gs.Known.ToList())
            {
                var k = S.Incidents.FirstOrDefault(i => i.Id == id);
                if (k == null || !(k.Id == inc.Id || SameStory(k, inc))) continue;
                if (!gs.Cleared.Contains(k.Id)) gs.Cleared.Add(k.Id);
                gs.Unmentioned.Remove(k.Id);
            }
            if (!gs.Cleared.Contains(inc.Id)) gs.Cleared.Add(inc.Id);
            gs.Unmentioned.Remove(inc.Id);
            if (gs.Cleared.Count > 40) gs.Cleared.RemoveRange(0, gs.Cleared.Count - 40);
        }

        // ---------------- talks and friendship jealousy ----------------

        public static void RecordTalk(SaveData.Heroine h)
        {
            Director.Changed(h, "talked");
            var gs = S.Girl(GirlInfo.Key(h));
            if (!gs.TalkDays.Contains(S.Day)) gs.TalkDays.Add(S.Day);
            if (gs.TalkDays.Count > 10) gs.TalkDays.RemoveAt(0);
        }

        public static int TalkDaysRecent(SaveData.Heroine h, int days)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.TalkDays.Count(d => S.Day - d < days);
        }

        /// <summary>
        /// Her close friend you have been spending a lot of time with while she was left out, or null.
        /// At most once every 5 days per girl.
        /// </summary>
        public static SaveData.Heroine JealousOf(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (S.Day - gs.JealousDay < 5) return null;
            if (TalkDaysRecent(h, 4) > 0 || gs.TalkDays.Count == 0) return null; // only girls you used to talk to
            return FriendsOf(h, 45).FirstOrDefault(f => TalkDaysRecent(f, 5) >= 3);
        }

        // ---------------- places ----------------

        private static string _lastMapName;
        private static readonly HashSet<string> NotedThisPeriod = new HashSet<string>();
        private static int _period = -1;

        /// <summary>About once a second: being seen in the wrong restroom or changing room becomes an incident.</summary>
        public static void Tick()
        {
            if (!Campus.On || !G.InRoamingPeriod || !G.IsFreeRoaming) return;
            var period = S.Day * 16 + (int)G.CurrentMask;
            if (period != _period) { _period = period; NotedThisPeriod.Clear(); }
            var name = G.MapName(G.PlayerMapNo) ?? "";
            if (name == _lastMapName) return;
            _lastMapName = name;
            var kind = PlaceKind(name);
            if (kind == null || NotedThisPeriod.Contains(name)) return;
            var here = GirlsOnMyMap().ToList();
            if (here.Count == 0) return;
            NotedThisPeriod.Add(name);
            if (kind == "boys")
            {
                var g = here[Random.Range(0, here.Count)];
                Add(Kinds.HerBoysRoom, g);
                Toasts.Show(GirlInfo.FirstName(g) + " is in the boys' restroom...? She looks mortified.");
            }
            else
            {
                // Your girlfriend keeps it to herself (her lines already treat it as a tease, not a scandal).
                var gf = here.Where(g => g.isGirlfriend || Reactions.CalmWhenSeen(g)).ToList();
                here = here.Where(g => !g.isGirlfriend && !Reactions.CalmWhenSeen(g)).ToList();
                if (here.Count == 0)
                {
                    Toasts.Show(GirlInfo.FirstName(gf[0]) + " caught you here... and just shakes her head with a smile.");
                    return;
                }
                Add(kind == "lockers" ? Kinds.SeenLockers : Kinds.SeenGirlsRoom, null, here);
                Toasts.Show(here.Count == 1 ? GirlInfo.FirstName(here[0]) + " saw you here. This will get around." : here.Count + " girls saw you here. This will get around.");
            }
        }

        /// <summary>"girls" (girls' restroom), "lockers" (girls' changing room), "boys" (boys' restroom) or null.</summary>
        public static string PlaceKind(string mapName)
        {
            if (string.IsNullOrEmpty(mapName)) return null;
            var toilet = mapName.Contains("トイレ") || mapName.Contains("Toilet") || mapName.Contains("Restroom");
            var girls = mapName.Contains("女");
            var boys = mapName.Contains("男");
            if (toilet && girls) return "girls";
            if (toilet && boys) return "boys";
            if (mapName.Contains("更衣室") || mapName.Contains("Locker")) return boys ? null : "lockers";
            return null;
        }
    }
}
