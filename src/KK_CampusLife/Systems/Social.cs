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
            var e = Effect(h, inc);
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
                foreach (var inc in fresh.Where(i => known.Contains(i.Id)))
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

        /// <summary>The air was cleared about this incident (it no longer counts as a grudge).</summary>
        public static void Cleared(SaveData.Heroine h, Incident inc)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (inc == null || gs.Cleared.Contains(inc.Id)) return;
            gs.Cleared.Add(inc.Id);
            if (gs.Cleared.Count > 30) gs.Cleared.RemoveAt(0);
        }

        // ---------------- talks and friendship jealousy ----------------

        public static void RecordTalk(SaveData.Heroine h)
        {
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
