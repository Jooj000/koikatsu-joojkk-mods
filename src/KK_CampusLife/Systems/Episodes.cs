using System;
using System.Collections.Generic;
using System.Linq;
using ActionGame.Chara;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Spotlight episodes: a girl the director has in the spotlight (sometimes with a close friend) walks over and invites
    /// you out; the outing plays in the talk screen like the game's story mode, with her standing in front of one of the
    /// painted backgrounds and changing faces as she talks. The choices are built for her (<see cref="Ctx"/>): how close you
    /// are, what she remembers (what you did for her, a grudge, your last outing), your reputation and stats, the items you
    /// carry, her likes, club and mood. Each episode ends in one of a few finales the player picks among the ones the
    /// context opens: a heart-to-heart, talking about something you did, a laugh, or a promise of the next outing; alone
    /// with her, also the romantic ones (her hand, a confession, somewhere private, which ends in the game's own H).
    /// Episodes are written in <c>Dialogue/EpisodeScenes.cs</c>.
    /// </summary>
    internal static partial class Episodes
    {
        private static CampusState S { get { return Campus.S; } }
        private static float _nextTry;
        private static int _lastPeriod = -1;

        /// <summary>Id of the episode that played last.</summary>
        public static string LastPlayed = "";

        // ================= what the scene knows about her =================

        internal sealed class Ctx
        {
            public SaveData.Heroine Her, Pal;    // Pal: a close friend who came along, or null
            public GirlState Gs;
            public Tone Tone, PalTone;
            public string Name = "", PalName = "";
            public int Favor, Trust, Bond, Tier;  // Tier 0 stranger, 1 acquaintance, 2 friend, 3 close friend
            public bool Girlfriend;
            public int Lewd, HCount;
            public Incident Good;                 // the best thing you did for her that she knows about
            public Incident Bad;                  // something she holds against you and was not cleared
            public string LastEp = "";            // your last outing with her (episode id)
            public int Outings;
            public Mood Mood;
            public bool Sporty, Bookish, Foodie, Sweet, Spicy, Coffee, Animals, Fashion, Cooking;
            public string Snack = "";             // a snack you carry, the one she likes first
            public string Club = "";
            public SaveData.Heroine Best;         // her best friend (not the pal), for the talks

            public bool Famous { get { return Campus.S.Reputation >= 60; } }
            public bool Infamous { get { return Campus.S.Reputation < 25; } }
            public bool Low { get { return Mood == Mood.Stressed || Mood == Mood.Upset || Mood == Mood.Lonely; } }

            /// <summary>Her line for her personality.</summary>
            public string T(string gentle, string energetic, string cool, string teasing) { return Lines.T(Tone, gentle, energetic, cool, teasing); }
            public string P(string gentle, string energetic, string cool, string teasing) { return Lines.T(PalTone, gentle, energetic, cool, teasing); }
        }

        internal static Ctx Read(SaveData.Heroine her, SaveData.Heroine pal)
        {
            var key = GirlInfo.Key(her);
            var c = new Ctx { Her = her, Pal = pal, Gs = S.Girl(key), Tone = GirlInfo.ToneOf(her), Name = GirlInfo.FirstName(her) };
            if (pal != null) { c.PalTone = GirlInfo.ToneOf(pal); c.PalName = GirlInfo.FirstName(pal); c.Bond = Social.Bond(her, pal); }
            c.Favor = her.favor;
            c.Trust = Social.Trust(her);
            c.Tier = c.Favor < 25 ? 0 : c.Favor < 50 ? 1 : c.Favor < 75 ? 2 : 3;
            c.Girlfriend = her.isGirlfriend;
            c.Lewd = her.lewdness;
            c.HCount = her.hCount;
            c.Mood = c.Gs.MoodDay == S.Day ? c.Gs.Mood : Mood.None;
            c.Bad = Social.OwnGrudge(her);
            c.Good = S.Incidents.Where(i => i.Girl == key && S.Day - i.Day <= 21 && (i.Kind == Social.Kinds.Helped || i.Kind == Social.Kinds.Defended || i.Kind == Social.Kinds.KeptPromise))
                .OrderByDescending(i => i.Kind == Social.Kinds.Defended ? 2 : i.Kind == Social.Kinds.Helped ? 1 : 0).ThenByDescending(i => i.Day).FirstOrDefault();
            c.Outings = c.Gs.Episodes.Count;
            if (c.Outings > 0) c.LastEp = c.Gs.Episodes[c.Outings - 1].Split(':')[0];
            c.Sporty = GirlInfo.InSportsClub(her) || GirlInfo.Likes(her, "exercise");
            c.Bookish = GirlInfo.Likes(her, "study");
            c.Foodie = GirlInfo.Likes(her, "food");
            c.Sweet = GirlInfo.Likes(her, "sweets");
            c.Spicy = GirlInfo.Likes(her, "spicy");
            c.Coffee = GirlInfo.Likes(her, "coffee");
            c.Animals = GirlInfo.Likes(her, "animals");
            c.Fashion = GirlInfo.Likes(her, "fashion");
            c.Cooking = GirlInfo.Likes(her, "cooking");
            c.Club = GirlInfo.InNoClub(her) ? "" : GirlInfo.ClubName(her);
            c.Best = Social.FriendsOf(her, 50).FirstOrDefault(f => f != pal);
            foreach (var id in SnackOrder(c)) if (S.Count(id) > 0) { c.Snack = id; break; }
            return c;
        }

        private static IEnumerable<string> SnackOrder(Ctx c)
        {
            if (c.Spicy) yield return "spicy_chips";
            if (c.Sweet) { yield return "daifuku"; yield return "cookies"; yield return "chocolate"; }
            if (c.Coffee) yield return "canned_coffee";
            if (c.Sporty) yield return "sports_drink";
            foreach (var id in new[] { "chocolate", "cookies", "daifuku", "bento", "sports_drink", "canned_coffee" }) yield return id;
        }

        internal static string ItemName(string id)
        {
            var d = Items.Get(id);
            return d == null ? id : d.Name.ToLowerInvariant();
        }

        // ================= procedural choices =================

        /// <summary>
        /// The options of one choice: each one only if the context allows it, with a weight. At most four are shown (asked
        /// for few options): the ones marked always first, then the heaviest. Every shown option writes its branch, and the
        /// branches meet again at the join label.
        /// </summary>
        internal sealed class Opts
        {
            private sealed class O { public string Label, Tag; public float W; public bool Always; public Action Body; public int Order; }
            private readonly List<O> _l = new List<O>();

            public Opts Add(string label, string tag, Action body, float weight = 1f) { return If(true, label, tag, body, weight); }

            public Opts If(bool cond, string label, string tag, Action body, float weight = 1f)
            {
                if (cond) _l.Add(new O { Label = label, Tag = tag, Body = body, W = weight * Random.Range(0.85f, 1.15f), Order = _l.Count });
                return this;
            }

            /// <summary>Always shown (the safe fallback).</summary>
            public Opts Keep(string label, string tag, Action body)
            {
                _l.Add(new O { Label = label, Tag = tag, Body = body, W = 1000f, Always = true, Order = _l.Count });
                return this;
            }

            /// <summary>Writes the choice and its branches; returns the tags offered.</summary>
            public List<string> Play(SceneBuilder b, string join, int max = 4)
            {
                var shown = _l.OrderByDescending(o => o.W).Take(Math.Max(1, max)).OrderBy(o => o.Order).ToList();
                var args = new List<string>();
                foreach (var o in shown) { args.Add(o.Label); args.Add(o.Tag); }
                b.Choice(args.ToArray());
                foreach (var o in shown)
                {
                    b.Branch(o.Tag);
                    try { o.Body(); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); }
                    b.Goto(join);
                }
                b.Label(join);
                return shown.Select(o => o.Tag).ToList();
            }
        }

        // ================= rewards =================

        /// <summary>What an episode pays, per branch taken; summed and paid once at the end, with one message.</summary>
        internal sealed class Tally
        {
            public readonly Dictionary<string, Action> OnPick = new Dictionary<string, Action>();
            public readonly Dictionary<SaveData.Heroine, int[]> Girls = new Dictionary<SaveData.Heroine, int[]>(); // favor, trust, devotion, attention, lewd
            public int Rep, Int, Str, Sens, Money;
            public readonly List<string> ItemsUsed = new List<string>();
            public readonly List<string> ItemsGot = new List<string>();
            public readonly List<Action> Extra = new List<Action>();
            public string Ending = "";
            public bool IntoH;                   // she said yes to somewhere private: the talk ends in the game's H

            public void On(string tag, Action a)
            {
                Action old;
                OnPick[tag] = OnPick.TryGetValue(tag, out old) ? old + a : a;
            }

            public void Her(SaveData.Heroine h, int favor, int trust = 0, int devotion = 0, int lewd = 0)
            {
                if (h == null) return;
                int[] v;
                if (!Girls.TryGetValue(h, out v)) Girls[h] = v = new int[5];
                v[0] += favor; v[1] += trust; v[2] += devotion; v[3] += 10; v[4] += lewd;
            }

            public void Use(string item) { ItemsUsed.Add(item); }
            public void Get(string item) { ItemsGot.Add(item); }
        }

        private static void PayOut(Ep ep, Ctx c, Tally t, HashSet<string> picks)
        {
            foreach (var p in picks)
            {
                Action a;
                if (t.OnPick.TryGetValue(p, out a)) { try { a(); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } }
            }
            t.Her(c.Her, 3); // the outing itself
            if (c.Pal != null) t.Her(c.Pal, 2);
            foreach (var a in t.Extra) { try { a(); } catch (Exception e) { CampusLifePlugin.Log.LogError(e); } }

            var parts = new List<string>();
            foreach (var kv in t.Girls)
            {
                var h = kv.Key;
                var v = kv.Value;
                var applied = Campus.Apply(new Rewards { Favor = v[0], Devotion = v[2], Lewd = v[4] }, h, true);
                var tr = Social.AddTrust(h, v[1]);
                var gs = S.Girl(GirlInfo.Key(h));
                gs.Attention = Mathf.Clamp(gs.Attention + v[3], 0, 100);
                Social.RecordTalk(h);
                var bits = new List<string>();
                if (applied.Favor != 0) bits.Add((applied.Favor > 0 ? "+" : "") + applied.Favor + " favor");
                if (tr != 0) bits.Add((tr > 0 ? "+" : "") + tr + " trust");
                if (applied.Devotion > 0) bits.Add("+" + applied.Devotion + " devotion");
                if (applied.Lewd > 0) bits.Add("+" + applied.Lewd + " H");
                if (bits.Count > 0) parts.Add(GirlInfo.FirstName(h) + " " + string.Join(", ", bits.ToArray()));
            }
            foreach (var id in t.ItemsUsed) if (S.Count(id) > 0) S.AddItem(id, -1);
            foreach (var id in t.ItemsGot) S.AddItem(id, 1);
            var me = Campus.Apply(new Rewards { Rep = t.Rep, Int = t.Int, Str = t.Str, Sens = t.Sens, Money = t.Money }, null, false);
            var mine = me.Describe();
            if (!string.IsNullOrEmpty(mine) && mine.Trim() != "-") parts.Add(mine);
            if (t.ItemsGot.Count > 0) parts.Add("got " + string.Join(", ", t.ItemsGot.Select(ItemName).ToArray()));

            var tag = ep.Id + (string.IsNullOrEmpty(t.Ending) ? "" : ":" + t.Ending);
            foreach (var h in new[] { c.Her, c.Pal })
            {
                if (h == null) continue;
                var gs = S.Girl(GirlInfo.Key(h));
                gs.Episodes.Add(tag);
                if (gs.Episodes.Count > 12) gs.Episodes.RemoveAt(0);
                gs.EpisodeDay = S.Day;
            }
            if (c.Bad != null && picks.Contains("fin_sorry")) Social.Cleared(c.Her, c.Bad);
            if (t.Ending == "heart" || t.Ending == "sorry" || t.Ending == "close" || t.Ending == "confess") Social.Add(Social.Kinds.GoodTalk, c.Her);
            var who = c.Name + (c.Pal != null ? " and " + c.PalName : "");
            var msg = ep.Title + " with " + who + (parts.Count > 0 ? " (" + string.Join("; ", parts.ToArray()) + ")" : "");
            S.Log(msg);
            Toasts.Show(msg);
            CampusLifePlugin.Log.LogInfo("Episode " + tag + " paid: " + msg + " | picks " + string.Join(",", picks.ToArray()));
        }

        // ================= the catalog =================

        internal sealed class Ep
        {
            public string Id = "", Title = "", Bg = "";
            public bool PalOk;                            // a close friend may come along
            public Func<Ctx, float> Weight = c => 1f;     // 0 or less: does not fit her
            public Action<Ctx, SceneBuilder> Invite;      // on the map, as she walks over
            public Action<Ctx, SceneBuilder, Tally> Write;
        }

        private static List<Ep> _all;
        internal static List<Ep> All { get { return _all ?? (_all = Catalog()); } }

        internal static Ep Find(string id) { return All.FirstOrDefault(e => e.Id == id); }

        // ================= the director =================

        /// <summary>Girls who can star: the spotlight, or with a small class the ones closest to you and the ones feeling unseen.</summary>
        internal static List<SaveData.Heroine> Cast()
        {
            var students = Board.Students.Where(x => Truth.AtSchool(x)).ToList(); // only girls who came today
            var spot = students.Where(Spotlight.InSpotlight).ToList();
            if (spot.Count > 0) return spot;
            return students.OrderByDescending(h => h.favor / 2 + (100 - S.Girl(GirlInfo.Key(h)).Attention) / 2 + Random.Range(0, 30)).Take(5).ToList();
        }

        /// <summary>Called about once a second from Calendar.Tick, when nothing else is playing.</summary>
        public static void Tick()
        {
            if (!Campus.On || !Cfg.EpisodesEnabled.Value || Duo.Busy || !Scenes.CanPlayOnMap || !G.IsSchoolDay) return;
            if (S.LastEpisodeDay >= 0 && S.Day - S.LastEpisodeDay < Cfg.EpisodeEveryDays.Value) return;
            var mask = G.CurrentMask;
            if (mask != PeriodMask.Club && mask != PeriodMask.After && mask != PeriodMask.Lunch) return;
            var period = S.Day * 16 + (int)mask;
            if (period == _lastPeriod || Time.realtimeSinceStartup < _nextTry) return;
            _nextTry = Time.realtimeSinceStartup + 25f;
            if (Random.value > 0.4f) return;
            var near = NearbyStars();
            if (near.Count == 0) return;
            var star = near[Random.Range(0, near.Count)];
            if (!Agenda.May("episode", BeatKind.Personal, star.heroine)) return;
            _lastPeriod = period;
            if (Play(star, null, null)) Agenda.Took("episode", BeatKind.Personal, star.heroine);
        }

        /// <summary>Stars on the player's map, near and walking about (as the duo director picks them).</summary>
        private static List<NPC> NearbyStars()
        {
            var act = G.Act;
            if (act == null || act.Player == null || act.Map == null) return new List<NPC>();
            var cast = new HashSet<SaveData.Heroine>(Cast());
            var p = act.Player.transform.position;
            // Only a girl who knows you invites you out, and not one sore at you over a friend (she would not ask you).
            return act.npcList.Where(n => n != null && n.heroine != null && n.isActive && n.mapNo == act.Map.no && cast.Contains(n.heroine)
                                          && Social.KnowsYou(n.heroine) && (Social.Grudge(n.heroine) == null || Social.OwnGrudge(n.heroine) != null)
                                          && (n.transform.position - p).sqrMagnitude < 25f * 25f && n.AI != null && n.AI.isRunning
                                          && S.Girl(GirlInfo.Key(n.heroine)).EpisodeDay != S.Day).ToList();
        }

        /// <summary>A close friend of hers on the same map who can come along (for episodes that allow it), or null.</summary>
        private static NPC PalFor(NPC star)
        {
            var act = G.Act;
            if (act == null || act.Player == null) return null;
            var p = act.Player.transform.position;
            return act.npcList.Where(n => n != null && n != star && n.heroine != null && n.isActive && n.mapNo == star.mapNo && GirlInfo.IsRegular(n.heroine)
                                          && (n.transform.position - p).sqrMagnitude < 25f * 25f && n.AI != null && n.AI.isRunning
                                          && Social.Bond(star.heroine, n.heroine) >= 40)
                .OrderByDescending(n => Social.Bond(star.heroine, n.heroine)).FirstOrDefault();
        }

        /// <summary>The episode that fits her best today, weighted, skipping the last two she had.</summary>
        /// <summary>Outings away from school (and at night): only a friend or a girlfriend asks you.</summary>
        private static readonly HashSet<string> Outings = new HashSet<string> { "stalls", "fortune", "haunt", "snow", "trip", "tea", "wishes" };

        private static Ep Choose(Ctx c, bool withPal)
        {
            var recent = c.Gs.Episodes.Skip(Math.Max(0, c.Gs.Episodes.Count - 2)).Select(x => x.Split(':')[0]).ToList();
            var friend = c.Her.relation >= 1 || c.Her.isGirlfriend;
            var pool = All.Where(e => (!withPal || e.PalOk) && !recent.Contains(e.Id) && (friend || !Outings.Contains(e.Id)))
                .Select(e => new { e, w = e.Weight(c) }).Where(x => x.w > 0f).ToList();
            if (pool.Count == 0) return null;
            var roll = Random.value * pool.Sum(x => x.w);
            foreach (var x in pool) { roll -= x.w; if (roll <= 0f) return x.e; }
            return pool[pool.Count - 1].e;
        }

        /// <summary>Starts an episode with this girl (pal: null = maybe a friend joins; id: null = the director picks).</summary>
        public static bool Play(NPC star, NPC pal, string id, bool palAllowed = true)
        {
            try
            {
                if (star == null || star.heroine == null || Duo.Busy) return false;
                if (pal == null && palAllowed && Random.value < 0.35f) pal = PalFor(star);
                var c = Read(star.heroine, pal == null ? null : pal.heroine);
                var ep = string.IsNullOrEmpty(id) ? Choose(c, pal != null) : Find(id);
                if (ep == null && pal != null) { pal = null; c = Read(star.heroine, null); ep = Choose(c, false); }
                if (ep == null) { CampusLifePlugin.Log.LogInfo("Episodes: nothing fits " + c.Name); return false; }
                if (pal != null && !ep.PalOk) { pal = null; c = Read(star.heroine, null); }

                var intro = new SceneBuilder(null, false);
                ep.Invite(c, intro);
                var show = new Duo.Show { A = star, B = pal, Name = "episode " + ep.Id + " (" + c.Name + (pal != null ? " + " + c.PalName : "") + ")", Intro = intro };
                Tally tally = null;
                show.Main = inTalk =>
                {
                    var t = new Tally();
                    tally = t;
                    var b = new SceneBuilder(c.Her, inTalk);
                    b.Bg(ep.Bg);
                    ep.Write(c, b, t);
                    show.Result = _ => PayOut(ep, c, t, new HashSet<string>(Scenes.Picks));
                    return b;
                };
                show.IntoH = () => tally != null && tally.IntoH && Scenes.Picks.Contains("fin_h") && Cfg.LewdRequests.Value;
                show.Outro = () =>
                {
                    var o = new SceneBuilder(null, false);
                    o.Narr(c.Girlfriend || Scenes.Picks.Contains("fin_close") || Scenes.Picks.Contains("fin_confess")
                        ? "Back on campus, the bell rings somewhere far away. Your hand still feels warm."
                        : "Back on campus, the bell rings somewhere far away. That was a good one.");
                    return o;
                };
                var main = show.Main(true);
                if (!Duo.Stage(show, main)) return false;
                S.LastEpisodeDay = S.Day;
                LastPlayed = ep.Id;
                foreach (var h in new[] { c.Her, c.Pal }) if (h != null) S.Girl(GirlInfo.Key(h)).EpisodeDay = S.Day;
                return true;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); return false; }
        }
    }
}
