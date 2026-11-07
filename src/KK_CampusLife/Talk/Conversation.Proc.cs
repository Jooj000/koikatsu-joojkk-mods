using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// koikatsu-arquitetura-dialogo-procedural, sections 4-7). The Talk list shows topics born from what she knows
    /// (Lore): her day (one), the two of you, school, what she saw or heard about you, Gossip, and Small talk only when
    /// nothing else is left. A topic used is gone. Her line comes from the bank (the most specific rule, in her voice);
    /// your answers are moves on what that line left open (ask, comfort, share, tease, praise, invite, correct...), how
    /// well each lands is computed from her voice, mood, opinion and the relation, and her reaction may open a next step
    /// (up to 3).
    /// </summary>
    internal static partial class Conversation
    {
        private sealed class PTopic
        {
            public string Id = "", Label = "", Kind = "";
            public LoreFact Fact;
            public LoreKnow Know;
            public float Score;
            public bool Hot, Opening;
            public string Subject = "";        // small talk: what about
            public string ToldFit = "";        // "tell": how your news landed (good, neutral, bad)
            public SchoolTopicInfo School;
            public List<LoreFact> Told;        // "confide": every untold fact, which saying it once settles
            public List<LoreFact> Facts;       // "confide": your romances with other girls, told in one go
            public int Times;                  // "confide": how many untold steps with them in all (the impact)
            public List<string> Merged;        // topics with the same label folded into this one, settled with it
        }

        private sealed class PMove
        {
            public string Id, Label, Tag;
            public Q Q;
        }

        /// <summary>Last topics built.</summary>
        internal static readonly List<string> LastTopics = new List<string>();

        /// <summary>"food" for a gift she eats or drinks (by the item named in the fact's note), else "thing".</summary>
        internal static string GiftKind(string note)
        {
            if (string.IsNullOrEmpty(note)) return "thing";
            var n = note.ToLowerInvariant();
            var item = Items.All.Where(x => x.Type == ItemType.Gift && !string.IsNullOrEmpty(x.Name)).FirstOrDefault(x => n.EndsWith(x.Name.ToLowerInvariant(), StringComparison.Ordinal));
            if (item == null) return "thing";
            var eats = item.Likes.Any(l => l == "sweets" || l == "food" || l == "coffee" || l == "spicy" || l == "tea")
                       || new[] { "drink", "juice", "soda", "milk", "tea", "coffee", "can" }.Any(w => item.Name.ToLowerInvariant().Contains(w));
            return eats ? "food" : "thing";
        }

        private static bool TopicDone(string girl, string id) { return S.TopicsDone.Any(x => x.StartsWith(girl + "|" + id + "|", StringComparison.Ordinal)); }

        private static void MarkTopic(string girl, string id)
        {
            if (!TopicDone(girl, id)) S.TopicsDone.Add(girl + "|" + id + "|" + S.Day);
            if (S.TopicsDone.Count > 800) S.TopicsDone.RemoveRange(0, S.TopicsDone.Count - 800);
        }

        // ================= the topics =================

        private static List<PTopic> TalkTopics(SaveData.Heroine h, GirlState gs)
        {
            var key = GirlInfo.Key(h);
            var ctx = new List<PTopic>();
            var stranger = h.relation < 0;

            if (!stranger && !TopicDone(key, "day:" + S.Day)) ctx.Add(DayTopic(h, gs));

            foreach (var kv in Lore.Of(key))
            {
                var k = kv.Key; var f = kv.Value;
                if (k.ToPlayer >= 2 || TopicDone(key, "f:" + f.Id) || SharedAway(h, f)) continue;
                var us = f.Between(Lore.Player, key);
                var you = !us && f.Involves(Lore.Player) && k.How != How.Participant;
                if (us)
                {
                    if (stranger || f.Valence < 0 || f.Verb == "met" || f.Verb == "talked" || S.Day - f.Day > 14) continue;
                    if (f.Day == S.Day && f.Weight < 2) continue;   // what just happened is not a topic yet
                    if (f.Id >= _factsBefore) continue;            // made in this very talk: from the next one on
                    ctx.Add(new PTopic { Id = "f:" + f.Id, Kind = "us", Fact = f, Know = k, Label = UsLabel(f), Score = Lore.Salience(k, f) });
                }
                else if (you && f.Weight >= 1 && S.Day - k.Day <= 7 && !Confided(f, key))
                    ctx.Add(new PTopic { Id = "f:" + f.Id, Kind = "you", Fact = f, Know = k, Label = YouLabel(f, k), Score = Lore.Salience(k, f) * 1.2f, Hot = f.Weight >= 2 && f.Valence <= 0 || f.Verb == "had_h" });
            }

            if (!stranger)
            {
                var st = SchoolTopic(h);
                if (st != null && st.Id != "rumors" && !TopicDone(key, "school:" + st.Id)) ctx.Add(new PTopic { Id = "school:" + st.Id, Kind = "school", School = st, Label = "Talk about " + st.Label, Score = 1.4f });
            }

            // One topic per label (2026-10-29: Frieren had "Talk about what she saw" twice, the restroom and the
            // changing room): the strongest stays, the rest are settled together with it.
            ctx = ctx.GroupBy(t => t.Label).Select(g =>
            {
                var top = g.OrderByDescending(t => t.Score).First();
                if (g.Count() > 1) { top.Merged = g.Where(t => t != top).Select(t => t.Id).ToList(); top.Score += 0.3f * top.Merged.Count; }
                return top;
            }).ToList();
            var confide = stranger ? null : ConfidePick(h);
            if (confide != null) ctx.Add(confide);
            var gossip = stranger ? null : GossipPick(key);
            var tell = stranger ? null : TellPick(key);
            var list = ctx.OrderByDescending(t => t.Score).Take(gossip != null && tell != null ? 2 : 3).ToList();
            if (gossip != null) list.Add(gossip);
            if (tell != null) list.Add(tell);
            // Out of topics: two or three things to chat about, not one; one to fill a short list.
            var want = list.Count == 0 ? (stranger ? 2 : 3) : list.Count == 1 ? 1 : 0;
            if (want > 0) list.AddRange(SmallTalkTopics(h, gs, want));
            LastTopics.Clear();
            LastTopics.AddRange(list.Select(t => t.Kind + ": " + t.Label + " (" + t.Score.ToString("0.0") + ")"));
            return list;
        }

        /// <summary>Your girlfriend already has her stance on you and that girl (Share): it is not a topic again.</summary>
        private static bool SharedAway(SaveData.Heroine h, LoreFact f)
        {
            if (!h.isGirlfriend || !(f.Tags.Contains("romance") || f.Tags.Contains("h"))) return false;
            var key = GirlInfo.Key(h);
            if (f.Involves(key) || !f.Involves(Lore.Player)) return false;
            var other = Social.Get(f.Subject == Lore.Player ? f.Obj : f.Subject);
            return other != null && Share.Stance(h, other) != Share.Unknown;
        }

        private static PTopic DayTopic(SaveData.Heroine h, GirlState gs)
        {
            var key = GirlInfo.Key(h);
            var t = new PTopic { Id = "day:" + S.Day, Kind = "day", Score = 2f, Label = DayLabel(h, gs) };
            // Something that happened to her today (not with you) names the question.
            var today = Lore.Of(key).Where(x => x.Key.How == How.Participant && x.Value.Day == S.Day && !x.Value.Involves(Lore.Player) && x.Value.Verb != "met")
                            .OrderByDescending(x => x.Value.Weight).Select(x => x.Value).FirstOrDefault();
            if (today != null)
            {
                t.Fact = today;
                t.Know = Lore.KnowOf(key, today);
                var other = today.Subject == key ? today.Obj : today.Subject;
                var on = string.IsNullOrEmpty(other) ? "" : GirlInfo.FirstName(other);
                switch (today.Verb)
                {
                    case "ate_with": t.Label = "Ask how lunch with " + on + " was"; break;
                    case "fought": case "insulted": t.Label = "Ask what happened with " + on; break;
                    case "made_up": t.Label = "Ask if things are okay with " + on + " now"; break;
                    case "started_dating": t.Label = "Ask about her and " + on; break;
                    default: if (on != "") t.Label = "Ask about her day with " + on; break;
                }
                t.Score = 2.5f;
            }
            // so it plays the club small talk (club.dlg), not the lines about her day.
            else if (t.Label.StartsWith("Ask how ", StringComparison.Ordinal) && t.Label.EndsWith(" went today", StringComparison.Ordinal))
            {
                t.Kind = "small";
                t.Subject = "club";
                t.Id = "day:" + S.Day;   // done for today like the day question it replaces
            }
            return t;
        }

        private static string UsLabel(LoreFact f)
        {
            var when = Lore.When(f);
            switch (f.Verb)
            {
                case "gave_gift": return "Ask if she liked " + (string.IsNullOrEmpty(f.Note) ? "your present" : "the " + f.Note.Replace("some ", "").Replace("a ", "").Replace("an ", ""));
                case "helped": return "Bring up helping her " + when;
                case "stood_up_for": return "Bring up standing up for her";
                case "kept_promise": return "Bring up the promise you kept";
                case "ate_with": return "Bring up your lunch together " + when;
                case "walked_home": return "Bring up walking her home " + when;
                case "dated": return "Bring up your date " + when;
                case "confessed": return "Talk about the confession";
                case "started_dating": return f.Since >= 0 && f.Since <= 2 ? "Talk about how fast you two started going out" : "Talk about going out together";
                case "had_h": return f.Since >= 0 && f.Since <= 1 ? "Talk about how fast things went " + when : "Talk about " + when + ", the two of you";
                case "kissed": return "Bring up the kiss";
                case "flirted": return "Bring up your flirting " + when;
                default: return "Bring up what happened " + when;
            }
        }

        private static string YouLabel(LoreFact f, LoreKnow k)
        {
            var heard = k.How == How.Heard || k.How == How.Rumor;
            switch (f.Verb)
            {
                case "had_h": return heard ? "Ask what she heard about you" : "Talk about what she saw";
                case "seen_girls_room": case "seen_lockers": return heard ? "Ask what she heard about you" : "Talk about what she saw";
                case "started_dating": case "dated": case "flirted": case "kissed": case "held_hands":
                    var other = f.Subject == Lore.Player ? f.Obj : f.Subject;
                    return "Talk about you and " + GirlInfo.FirstName(other);
                default: return heard ? "Ask what people are saying about you" : "Ask what she saw";
            }
        }

        /// <summary>What she would gossip about: something she knows about others (not herself, not only you), not told yet.</summary>
        private static PTopic GossipPick(string key)
        {
            var best = Lore.Of(key).Where(x => !x.Value.Involves(key)
                                             && !x.Value.Between(Lore.Player, x.Value.Subject == Lore.Player ? x.Value.Obj : x.Value.Subject) && x.Value.Weight >= 1
                                             && x.Key.ToPlayer == 0 && !TopicDone(key, "f:" + x.Value.Id) && S.Day - x.Value.Day <= 10)
                .OrderByDescending(x => Lore.Salience(x.Key, x.Value)).FirstOrDefault();
            if (best.Value == null) return null;
            return new PTopic { Id = "f:" + best.Value.Id, Kind = "gossip", Fact = best.Value, Know = best.Key, Label = "Gossip", Score = Lore.Salience(best.Key, best.Value) };
        }

        /// <summary>
        /// News you could tell her (2026-10-27): a fact you know (you were told it) that she does not, about other girls
        /// (not her, not you), fresh and of some weight. The biggest and newest first.
        /// </summary>
        private static PTopic TellPick(string key)
        {
            if (S.Lore == null) return null;
            var f = S.Lore.Where(x => !x.Involves(key) && !x.Involves(Lore.Player) && x.Weight >= 1 && S.Day - x.Day <= 10 && x.Verb != "met" && x.Verb != "shared"
                                      && !TopicDone(key, "tell:" + x.Id) && Lore.KnowOf(key, x) == null && Lore.PlayerKnows(x))
                .OrderByDescending(x => x.Weight * 3 - (S.Day - x.Day)).FirstOrDefault();
            if (f == null) return null;
            var who = new[] { f.Subject, f.Obj }.FirstOrDefault(x => !string.IsNullOrEmpty(x) && x != Lore.Player);
            return new PTopic { Id = "tell:" + f.Id, Kind = "tell", Fact = f, Label = "Tell her the news about " + (who != null ? GirlInfo.FirstName(who) : "someone"), Score = 1f };
        }

        private static readonly string[] ConfideVerbs = { "flirted", "held_hands", "touched", "kissed", "dated", "confessed", "started_dating", "had_h" };

        private static int ConfideDepth(string verb)
        {
            switch (verb)
            {
                case "flirted": case "held_hands": case "touched": return 1;
                case "kissed": case "dated": return 2;
                case "confessed": case "started_dating": return 3;
                case "had_h": return 4;
                default: return 0;
            }
        }

        private const int ConfideRest = 7;

        /// <summary>A romance of yours with another girl (not her): the confide topic tells it, not a topic of its own.</summary>
        private static bool Confided(LoreFact f, string her)
        {
            if (!f.Involves(Lore.Player) || f.Involves(her) || Array.IndexOf(ConfideVerbs, f.Verb) < 0) return false;
            var other = f.Subject == Lore.Player ? f.Obj : f.Subject;
            return !string.IsNullOrEmpty(other) && other != her;
        }

        /// <summary>
        /// como se você estivesse contando. e quando tem mais de um desses, junte em um só"): your romances with other
        /// girls in the last two weeks (and girlfriends you still have), one topic for all of them, the deepest step with
        /// each girl. A girl told about already is left out until something new happens with her.
        /// </summary>
        private static PTopic ConfidePick(SaveData.Heroine h)
        {
            if (S.Lore == null) return null;
            // uma aleatória por estar pegando outras").
            if (!Interested(h)) return null;
            var key = GirlInfo.Key(h);
            int said;
            if (S.Buffs.TryGetValue("dir:confide:" + key, out said) && S.Day - said < ConfideRest) return null;
            var best = new Dictionary<string, LoreFact>();
            var told = new List<LoreFact>();
            foreach (var f in S.Lore)
            {
                if (!Confided(f, key) || f.Subject != Lore.Player && f.Obj != Lore.Player) continue;
                var other = f.Subject == Lore.Player ? f.Obj : f.Subject;
                var oh = GirlInfo.Find(other);
                if (oh == null || S.Day - f.Day > 14 && !(oh.isGirlfriend && f.Verb == "started_dating")) continue;
                // "começar trisal depois admitir faz com que a terceira continue na lista de traições").
                if (Harem.Agreed(h, oh) || h.isGirlfriend && Share.Accepts(h, oh) && Share.Said(h, oh)) continue;
                if (TopicDone(key, "confide:" + other + ":" + f.Id)) continue;
                told.Add(f);
                LoreFact had;
                if (!best.TryGetValue(other, out had) || ConfideDepth(f.Verb) > ConfideDepth(had.Verb) || ConfideDepth(f.Verb) == ConfideDepth(had.Verb) && f.Day > had.Day) best[other] = f;
            }
            if (best.Count == 0) return null;
            var facts = best.Values.OrderByDescending(f => ConfideDepth(f.Verb)).ThenByDescending(f => f.Day).ToList();
            // No names on the button:
            // they come out in your words when you tell her.
            var deepest = facts.Max(f => ConfideDepth(f.Verb));
            return new PTopic { Id = "confide", Kind = "confide", Facts = facts, Told = told, Fact = facts[0], Label = "Admit about the others", Score = 1.6f + deepest * 0.3f, Hot = deepest >= 3, Times = told.Count };
        }

        /// <summary>
        /// Telling her: who is in it decides how she takes it (her best friend, a friend, a girl she cannot stand, a girl
        /// she likes herself), whether she already knew (saw it, or heard it), whether she wants you herself, and, for your
        /// girlfriend, her stance on sharing you (decided now if she had none). She learns it from you.
        /// </summary>
        private static void ConfideHer(SaveData.Heroine h, PTopic t, Dictionary<string, string> c)
        {
            var key = GirlInfo.Key(h);
            var others = t.Facts.Select(f => GirlInfo.Find(f.Subject == Lore.Player ? f.Obj : f.Subject)).ToList();
            var names = others.Select(GirlInfo.FirstName).ToList();
            // Past four names the sentence stops listing: everyone is told all the same.
            c["who"] = names.Count == 1 ? names[0] : names.Count == 2 ? names[0] + " and " + names[1] : names.Count > 4 ? string.Join(", ", names.Take(3).ToArray()) + " and the others" : string.Join(", ", names.Take(names.Count - 1).ToArray()) + " and " + names[names.Count - 1];
            c["who1"] = names[0];
            c["count"] = names.Count == 1 ? "one" : names.Count == 2 ? "two" : "many";
            var deep = t.Facts.Max(f => ConfideDepth(f.Verb));
            c["depth"] = deep >= 4 ? "h" : deep == 3 ? "dating" : deep == 2 ? (t.Facts.Any(f => f.Verb == "kissed") ? "kiss" : "date") : "flirt";
            c["thing"] = names.Count == 1 ? "you and " + names[0] : "you and the others";
            // What she knew before you said a word.
            var knew = t.Facts.Select(f => Lore.KnowOf(key, f)).ToList();
            var known = knew.Count(k => k != null);
            c["knew"] = known == 0 ? "none" : known == knew.Count ? "all" : "some";
            var seen = knew.FirstOrDefault(k => k != null && k.How == How.Witness);
            var heard = knew.FirstOrDefault(k => k != null && k.How != How.Witness);
            if (known > 0) c["knewhow"] = seen != null ? "witness" : "heard";
            if (heard != null && !string.IsNullOrEmpty(heard.From) && heard.From != Lore.Player) c["src"] = GirlInfo.FirstName(heard.From);
            // Who is in the circle, from her side.
            string circle = "none", circleWho = null;
            var bff = Social.BestFriend(h);
            foreach (var o in others)
            {
                var bond = Social.Bond(h, o);
                var role = o == bff || bond >= 70 ? "bff" : RomanceFor(h, o) >= 40 ? "crush" : bond >= 40 ? "friend" : bond <= -30 ? "rival" : "none";
                if (role == "none") continue;
                if (circle == "none") { circle = role; circleWho = GirlInfo.FirstName(o); }
                else if (circle != role && (role == "rival" || circle == "rival")) { circle = "mixed"; if (role == "rival") c["rivalwho"] = GirlInfo.FirstName(o); else c["rivalwho"] = circleWho; }
            }
            c["circle"] = circle;
            if (circleWho != null) c["circlewho"] = circleWho;
            if (circle == "rival" && circleWho != null) c["rivalwho"] = circleWho;
            var wants = Interested(h);
            c["interested"] = wants ? "yes" : "no";
            if (h.isGirlfriend)
            {
                // Her stance on each of them (she decides now when she had none); the worst one shows.
                var worst = Share.Trio;
                foreach (var o in others) { Share.Learned(h, o, false); worst = Math.Min(worst, Share.Stance(h, o)); }
                c["stance"] = worst == Share.Refuses ? "refuses" : worst == Share.Separate ? "separate" : worst == Share.Friends ? "friends" : worst == Share.Trio ? "trio" : "unknown";
            }
            var hurt = h.isGirlfriend ? c["stance"] == "refuses" : wants && (deep >= 2 || names.Count >= 2);
            c["hurt"] = hurt ? "yes" : "no";
            // She hears it from you now (no "you and X" topic about these again), and an honest word counts.
            foreach (var f in t.Told)
            {
                var k = Lore.KnowOf(key, f) ?? Lore.Learn(key, f, How.Heard, Lore.Player);
                if (k != null) k.ToPlayer = 2;
                MarkTopic(key, "confide:" + (f.Subject == Lore.Player ? f.Obj : f.Subject) + ":" + f.Id);
            }
            S.Buffs["dir:confide:" + key] = S.Day;
            // the air sobre traição").
            foreach (var o in others) if (o != null && o != h) SettledAbout(h, o);
            // The more times (every step with every girl, not only the deepest), the bigger it lands (mid-cycle list, item
            // 1: "quanto mais de uma vez, maior o impacto"): it hurts more, and the honesty of saying it all counts more.
            var times = Math.Max(t.Times, t.Facts.Count);
            c["times"] = times <= 1 ? "once" : times <= 3 ? "few" : "many";
            if (known < knew.Count) Social.AddTrust(h, hurt ? 1 : Math.Min(1 + times / 2, 4));
            // pays it too, more than anyone, and one who only accepts it apart still feels a little of it.
            var cost = 0;
            if (hurt)
            {
                Social.AddTrust(h, -Math.Min(times, 6));
                cost = h.isGirlfriend ? Math.Min(6 + 2 * times, 16) : Math.Min(3 + 2 * times, 12);
            }
            else if (h.isGirlfriend && c["stance"] == "separate") cost = Math.Min(1 + times, 5);
            if (cost > 0) G.AddFavor(h, -cost, 0);
            CampusLifePlugin.Log.LogInfo("Talk confide: " + GirlInfo.FirstName(h) + " <- " + c["who"] + " (depth " + c["depth"] + ", knew " + c["knew"] + ", circle " + circle +
                                         (circleWho != null ? " " + circleWho : "") + ", interested " + c["interested"] + ", times " + times + ", favor -" + cost + (h.isGirlfriend ? ", stance " + c["stance"] : "") + ")");
        }

        /// <summary>
        /// You tell her: she learns it from you, and how it lands moves your reputation. Good news or juicy news for a
        /// talker raises it (she spreads that you know things); gossip that hurts her friend, or a mean secret, lowers it,
        /// costs her trust and keeps her from passing it on.
        /// </summary>
        private static void TellHer(SaveData.Heroine h, PTopic t)
        {
            var key = GirlInfo.Key(h);
            var f = t.Fact;
            t.Know = Lore.Learn(key, f, How.Heard, Lore.Player);
            if (t.Know != null) t.Know.ToPlayer = 2;
            var others = new[] { f.Subject, f.Obj, f.About }.Where(x => !string.IsNullOrEmpty(x) && x != Lore.Player && x != key).Select(x => GirlInfo.Find(x)).Where(x => x != null).ToList();
            var friendHurt = f.Valence < 0 && others.Any(o => Social.Bond(h, o) >= 40);
            var rivalHurt = f.Valence < 0 && others.Any(o => Social.Bond(h, o) <= -30);
            var v = Voice.Of(h);
            var talker = v.Energy + v.Tease >= 1.1f || GirlInfo.Quirks(h).friendly;
            var juicy = f.Weight >= 2 || f.Tags.Contains("romance") || f.Tags.Contains("h");
            var score = (f.Valence > 0 ? 0.6f : 0f) + (juicy ? 0.4f : 0f) + (talker ? 0.4f : -0.2f) - (friendHurt ? 1.4f : 0f) + (rivalHurt ? 0.3f : 0f)
                        - (f.Exposure == LoreFact.Secret && f.Valence < 0 ? 0.8f : 0f) - (v.Formal > 0.7f && f.Valence < 0 ? 0.4f : 0f);
            t.ToldFit = score >= 0.8f ? "good" : score >= 0f ? "neutral" : "bad";
            var name = GirlInfo.FirstName(h);
            switch (t.ToldFit)
            {
                case "good":
                    S.Reputation = Mathf.Clamp(S.Reputation + 2, 0, 100);
                    G.AddFavor(h, 3, 0);
                    Social.AddTrust(h, 1);
                    Toasts.Show(name + " loved the news. Reputation +2");
                    break;
                case "neutral":
                    G.AddFavor(h, 1, 0);
                    break;
                default:
                    S.Reputation = Mathf.Clamp(S.Reputation - 3, 0, 100);
                    Social.AddTrust(h, -4);
                    if (t.Know != null) t.Know.Told = 9;   // she will not pass it on
                    // The girl it is about hears who talked, sooner or later.
                    foreach (var o in others) Social.AddTrust(o, -2);
                    Toasts.Show(name + " didn't like that gossip. Reputation -3");
                    break;
            }
            CampusLifePlugin.Log.LogInfo("Talk tell: " + name + " <- lore #" + f.Id + " '" + Lore.Render(f, key, 0) + "' -> " + t.ToldFit + " (score " + score.ToString("0.00") + ", rep " + S.Reputation + ")");
        }

        /// <summary>
        /// Light talk when nothing else is left: her likes (the ones she has first; one she does not care about only now
        /// and then), her pet theme, her club, her best friend, her birthday and sign, the place, lunch, the exams, the
        /// weather. Distinct subjects, best first.
        /// </summary>
        private static List<PTopic> SmallTalkTopics(SaveData.Heroine h, GirlState gs, int count)
        {
            var key = GirlInfo.Key(h);
            var stranger = h.relation < 0;
            var subjects = new Dictionary<string, float>();
            foreach (var l in GirlInfo.LikeIds)
            {
                var hers = GirlInfo.Likes(h, l);
                var known = (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) != 0;
                subjects["like:" + l] = hers ? (known ? 1.6f : 1.2f) : 0.15f;
            }
            var v = Voice.Of(h);
            // "está tendo repetição de temas de gostos"). A pet that is also a like she has talks as that like; one she
            // does not have as a like (always hungry, but "food" not on her list) drops that like, so she never says she
            // doesn't care about food in the same chat.
            if (v.Pet != "")
            {
                var asLike = PetLike(v.Pet);
                if (asLike == null || !GirlInfo.Likes(h, asLike))
                {
                    if (asLike != null) subjects.Remove("like:" + asLike);
                    subjects["pet:" + v.Pet] = 1.3f;
                }
            }
            // The place weighs more where it has a lot to say.
            var pid = Places.CurrentId ?? "";
            subjects["place:" + pid] = pid == "library" || pid == "rooftop" || pid == "infirmary" || Weather.Raining && Weather.Outdoor(G.PlayerMapNo) ? 1.1f : 0.6f;
            if (G.CurrentMask == PeriodMask.Lunch) subjects["lunch"] = 1f;
            if (Calendar.IsExamWeek) subjects["exams"] = 1.2f;
            // Rain: the weather is worth talking about when it rains or is about to.
            subjects["weather"] = Weather.Raining ? 1.4f : Weather.Later ? 0.8f : 0.35f;
            subjects["school"] = 0.5f;
            subjects["weekend"] = 0.5f;
            if (!stranger)
            {
                if (!GirlInfo.InNoClub(h) || GirlInfo.InKoikatsuClub(h)) subjects["club"] = 0.9f;
                if (Social.FriendsOf(h, 45).Any()) subjects["friend"] = 0.8f;
                var bd = BirthdayOf(h);
                if (bd != "no") subjects["birthday"] = 1.5f;
                subjects["horoscope"] = 0.4f;
                if (Teachers.IsTeacherVoice(h)) subjects[Teachers.Job(h) == "nurse" ? "nursing" : "teaching"] = 1f;
                if (h.relation >= 1) subjects["future"] = 0.4f;
            }
            NewSubjects(h, gs, subjects, stranger);
            var open = subjects.Where(x => !TopicDone(key, "st:" + x.Key)).ToList();
            // are other subjects; with nothing else left, likes fill it.
            var likesOpen = open.Where(x => x.Key.StartsWith("like:", StringComparison.Ordinal)).OrderByDescending(x => x.Value + Random.value * 0.9f).ToList();
            if (count > 1 && open.Count - likesOpen.Count >= count - 1)
                open = open.Where(x => !x.Key.StartsWith("like:", StringComparison.Ordinal) || likesOpen.Count > 0 && x.Key == likesOpen[0].Key).ToList();
            // The same subjects while the talk lasts: drawn anew, the labels changed each time the list was rebuilt.
            var s = SessFor(h);
            var picked = (s != null ? s.SmallPicked : new List<string>()).Where(p => open.Any(x => x.Key == p)).Take(count).ToList();
            picked.AddRange(open.Where(x => !picked.Contains(x.Key)).OrderByDescending(x => x.Value + Random.value * 0.9f).Take(count - picked.Count).Select(x => x.Key).ToList());
            if (s != null) s.SmallPicked = picked;
            return picked.Select(p => new PTopic { Id = "st:" + p, Kind = "small", Subject = p, Label = stranger ? StrangerLabel(p) : SmallLabel(p), Score = 0.5f }).ToList();
        }

        /// <summary>
        /// her (a dream when she slept badly, her fears in a storm, travel in exam week, her class when she fought with a
        /// friend). Strangers get the light ones; a teacher does not talk about her class, her commute or a part-time job.
        /// </summary>
        private static void NewSubjects(SaveData.Heroine h, GirlState gs, Dictionary<string, float> subjects, bool stranger)
        {
            try
            {
                var mood = gs.Mood;
                var teacher = Teachers.IsTeacherVoice(h);
                subjects["movies"] = Weather.Raining ? 1.2f : 0.9f;
                subjects["games"] = G.CurrentMask == PeriodMask.After ? 1.1f : 0.85f;
                subjects["season"] = Weather.Now == "clear" && Weather.Outdoor(G.PlayerMapNo) ? 1.1f : 0.8f;
                subjects["phone"] = 0.85f;
                subjects["travel"] = Calendar.IsExamWeek ? 1.3f : 0.85f;
                subjects["peeves"] = mood == CampusLife.Mood.Upset || mood == CampusLife.Mood.Stressed ? 1.3f : 0.8f;
                if (!teacher) subjects["commute"] = Weather.Raining ? 1.2f : 0.8f;
                if (stranger) return;
                subjects["dreams"] = mood == CampusLife.Mood.Sleepy ? 1.4f : Interested(h) ? 1.1f : 0.85f;
                subjects["family"] = h.isGirlfriend || h.relation >= 2 ? 1.2f : 0.9f;
                subjects["hometown"] = mood == CampusLife.Mood.Lonely ? 1.3f : Weather.Raining ? 1.0f : 0.8f;
                subjects["childhood"] = h.relation >= 1 ? 1.0f : 0.75f;
                subjects["fears"] = Weather.Now == "storm" ? 1.5f : h.relation >= 1 ? 0.95f : 0.7f;
                subjects["wish"] = G.CurrentMask == PeriodMask.After ? 1.1f : 0.8f;
                subjects["memory"] = gs.TalkDays.Count >= 8 ? 1.2f : 0.8f;
                subjects["talent"] = 0.85f;
                subjects["advice"] = 0.8f;
                if (!teacher)
                {
                    subjects["parttime"] = mood == CampusLife.Mood.Hungry || mood == CampusLife.Mood.Bored ? 1.1f : 0.8f;
                    var ctx = new Dictionary<string, string>();
                    FightCtx(h, ctx);
                    subjects["classmates"] = ctx.ContainsKey("fight") ? 1.4f : 0.9f;
                }
            }
            catch (Exception e) { CampusLifePlugin.Log.LogDebug("New subjects: " + e.Message); }
        }

        private static string StrangerLabel(string subject)
        {
            switch (subject)
            {
                case "movies": return "Ask if she's seen any good movies";
                case "games": return "Ask if she plays games";
                case "season": return "Say something about the season";
                case "phone": return "Ask what she's looking at on her phone";
                case "travel": return "Ask if she's been anywhere fun";
                case "peeves": return "Grumble about the crowded hallways";
                case "commute": return "Ask how she gets to school";
            }
            if (subject.StartsWith("place:", StringComparison.Ordinal)) return "Say something about this place";
            if (subject == "weather") return Weather.Raining ? "Say something about the rain" : "Make small talk about the weather";
            if (subject == "lunch") return "Ask if she's had lunch";
            // Several "Make small talk" side by side read as the same option: a like or her pet keeps its subject.
            if (subject.StartsWith("like:", StringComparison.Ordinal)) return "Ask if she's into " + GirlInfo.LikeLabel(subject.Substring(5));
            if (subject.StartsWith("pet:", StringComparison.Ordinal)) return SmallLabel(subject);
            if (subject == "exams") return "Say something about the exams";
            if (subject == "school") return "Say something about classes";
            if (subject == "weekend") return "Ask about her weekend";
            return "Make small talk";
        }

        /// <summary>The like a pet theme stands for, or null (books, music, cleaning and naps are not likes).</summary>
        private static string PetLike(string pet)
        {
            switch (pet)
            {
                case "food": return "food";
                case "cats": return "animals";
                case "sweets": return "sweets";
                case "clothes": return "fashion";
                case "cooking": return "cooking";
                case "coffee": return "coffee";
                case "running": case "sports": return "exercise";
                case "studying": return "study";
                default: return null;
            }
        }

        private static string SmallLabel(string subject)
        {
            if (subject.StartsWith("like:", StringComparison.Ordinal)) return "Chat about " + GirlInfo.LikeLabel(subject.Substring(5));
            if (subject.StartsWith("pet:", StringComparison.Ordinal))
                switch (subject.Substring(4))
                {
                    case "food": return "Ask if she's hungry again";
                    case "books": return "Ask what she's reading";
                    case "music": return "Ask what she's listening to";
                    case "sports": return "Ask about her training";
                    case "cleaning": return "Chat about keeping things tidy";
                    case "naps": return "Ask if she got any sleep";
                    case "studying": return "Ask how her studying is going";
                    default: return "Chat about " + subject.Substring(4);
                }
            if (subject.StartsWith("place:", StringComparison.Ordinal)) return "Chat about this place";
            switch (subject)
            {
                case "lunch": return "Chat about lunch";
                case "exams": return "Chat about the exams";
                case "weather": return Weather.Raining ? "Chat about the rain" : "Chat about the weather";
                case "weekend": return "Chat about the weekend";
                case "club": return "Ask how her club is going";
                case "friend": return "Chat about her friends";
                case "birthday": return "Ask about her birthday";
                case "horoscope": return "Chat about horoscopes";
                case "teaching": return "Ask what teaching is like";
                case "nursing": return "Ask about the infirmary";
                case "future": return "Ask about her plans after graduation";
                case "dreams": return "Ask if she had any dreams last night";
                case "family": return "Ask about her family";
                case "hometown": return "Ask where she grew up";
                case "childhood": return "Ask what she was like as a kid";
                case "movies": return "Chat about movies";
                case "games": return "Ask if she plays games";
                case "season": return "Chat about the season";
                case "phone": return "Ask what's new on her phone";
                case "parttime": return "Ask about part-time jobs";
                case "fears": return "Ask what scares her";
                case "peeves": return "Ask what gets on her nerves";
                case "wish": return "Ask what she'd wish for";
                case "memory": return "Ask about her best school memory";
                case "commute": return "Ask how she gets to school";
                case "travel": return "Ask where she'd like to travel";
                case "classmates": return "Chat about her classmates";
                case "talent": return "Ask if she has a hidden talent";
                case "advice": return "Ask her for some advice";
                default: return "Make small talk";
            }
        }

        private static readonly string[] RomanceVerbs = { "started_dating", "dated", "kissed", "flirted", "held_hands" };

        /// <summary>
        /// conhecidas reclamando de eu estar namorando ... 'you and xx? i didnt know you were her type'"): your girlfriend,
        /// a girl who wants you, or a girl close to the other one (her friend, the girl she likes, her rival). A girl with
        /// no stake leaves it alone; it stays a topic you can pick.
        /// </summary>
        private static bool Stake(SaveData.Heroine h, LoreFact f)
        {
            if (Array.IndexOf(RomanceVerbs, f.Verb) < 0) return true;
            if (h.isGirlfriend || Interested(h)) return true;
            var other = GirlInfo.Find(f.Subject == Lore.Player ? f.Obj : f.Subject);
            if (other == null || other == h) return false;
            var bond = Social.Bond(h, other);
            return bond >= 40 || bond <= -30 || RomanceFor(h, other) >= 40;
        }

        /// <summary>
        /// Something she raises herself before you say a word: what she saw or heard about you lately that weighs (the
        /// H in the hallway, being seen where you should not), not brought up yet.
        /// </summary>
        private static PTopic RaiseTopic(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            var best = Lore.Of(key).Where(x => x.Value.Involves(Lore.Player) && x.Key.How != How.Participant && x.Key.ToPlayer == 0 && x.Value.Weight >= 2 && S.Day - x.Key.Day <= 3 && !SharedAway(h, x.Value) && Stake(h, x.Value))
                .OrderByDescending(x => Lore.Salience(x.Key, x.Value)).FirstOrDefault();
            if (best.Value == null) return null;
            return new PTopic { Id = "f:" + best.Value.Id, Kind = "you", Fact = best.Value, Know = best.Key, Label = YouLabel(best.Value, best.Key), Opening = true, Hot = true };
        }

        // ================= the context of a line =================

        private static Dictionary<string, string> Ctx(SaveData.Heroine h, GirlState gs, PTopic t)
        {
            var key = GirlInfo.Key(h);
            var v = Voice.Of(h);
            var c = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Func<float, string> n = x => x.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            c["name"] = GirlInfo.FirstName(h);
            c["you"] = v.CallYou;
            c["exclaim"] = v.Exclaim;
            c["filler"] = v.Filler;
            c["laugh"] = v.Laugh;
            if (v.Pet != "") c["pet"] = v.Pet;
            c["voice"] = v.Kind;
            c["tone"] = GirlInfo.ToneOf(h).ToString().ToLowerInvariant();
            c["warm"] = n(v.Warm); c["energy"] = n(v.Energy); c["formal"] = n(v.Formal); c["tease"] = n(v.Tease);
            c["shy"] = n(v.Shy); c["blunt"] = n(v.Blunt); c["verbose"] = n(v.Verbose);
            c["rel"] = h.isGirlfriend ? "girlfriend" : h.relation >= 2 ? "lover" : h.relation == 1 ? "friend" : h.relation == 0 ? "acquaintance" : "stranger";
            c["dating"] = h.isGirlfriend ? "yes" : "no";
            c["favor"] = h.favor.ToString();
            c["lewd"] = h.lewdness.ToString();
            c["profile"] = Rhythm.Of(h).ToString().ToLowerInvariant();
            // Her card's refusals as the talk reads them: touch.dlg answers a kiss or a bold touch
            // she refuses in her own words. A card that refuses everything only refuses kisses when her drawn traits say so.
            c["nokiss"] = CardTraits.RefusesKiss(h) ? "yes" : "no";
            c["nocaress"] = CardTraits.TalkRefuses(h, "caress") ? "yes" : "no";
            var mood = MoodOf(h);
            c["mood"] = mood.ToString().ToLowerInvariant();
            c["moodword"] = mood == CampusLife.Mood.None ? "fine" : Lines.MoodName(mood).ToLowerInvariant();
            c["low"] = Low(mood) || mood == CampusLife.Mood.Hungry || mood == CampusLife.Mood.Bored ? "yes" : "no";
            var pm = G.CurrentMask;
            c["period"] = pm == PeriodMask.Lunch ? "lunch" : pm == PeriodMask.Club ? "club" : pm == PeriodMask.After ? "after" : "day";
            c["place"] = PlaceName();
            ClubCtx(h, c);
            c["exam"] = Calendar.IsExamWeek ? "yes" : "no";
            c["kind"] = t.Kind;
            if (!string.IsNullOrEmpty(t.Subject))
            {
                c["subject"] = t.Subject.StartsWith("like:", StringComparison.Ordinal) ? t.Subject.Substring(5) : t.Subject.StartsWith("pet:", StringComparison.Ordinal) ? "pet" : t.Subject.StartsWith("place:", StringComparison.Ordinal) ? "place" : t.Subject;
                if (t.Subject.StartsWith("like:", StringComparison.Ordinal))
                {
                    var like = t.Subject.Substring(5);
                    c["like"] = GirlInfo.LikeLabel(like);
                    c["likes"] = GirlInfo.Likes(h, like) ? "yes" : "no";
                    c["thing"] = GirlInfo.LikeLabel(like);
                    c["likeid"] = like;
                    c["likedo"] = Do(like);
                }
                else if (t.Subject.StartsWith("pet:", StringComparison.Ordinal)) c["thing"] = v.Pet;
                else if (t.Subject.StartsWith("place:", StringComparison.Ordinal)) c["thing"] = "this place"; // her line may be about anything here
                else c["thing"] = SubjectThing(h, t.Subject);
            }
            if (t.School != null) { if (t.School.Id.StartsWith("ev:", StringComparison.Ordinal)) c["evkind"] = Calendar.EventKind(t.School.Id.Substring(3)); c["event"] = t.School.Label; c["eventline"] = t.School.Line; c["school"] = t.School.Id.StartsWith("ev:", StringComparison.Ordinal) ? "event" : t.School.Id; c["thing"] = t.School.Label; }
            if (t.Kind == "day")
            {
                c["thing"] = mood == CampusLife.Mood.Stressed ? (Calendar.IsExamWeek ? "the exams" : "her classes") : mood == CampusLife.Mood.Sleepy ? "last night" : mood == CampusLife.Mood.Hungry ? "lunch"
                             : mood == CampusLife.Mood.Lonely ? "her friends" : !GirlInfo.InNoClub(h) && (G.CurrentMask == PeriodMask.Club || G.CurrentMask == PeriodMask.After) ? "the " + GirlInfo.ClubName(h) : "her day";
                c["already"] = gs.MoodKnownDay == S.Day ? "yes" : "no";
            }
            // A teacher's mood comes from her job: the same reason all day, in the lines and in {mything}.
            var cause = Teachers.Cause(h, mood);
            if (cause != null) { c["tcause"] = cause; if (t.Kind == "day") c["thing"] = Teachers.CauseThing(cause); }
            var f = t.Fact;
            if (f != null)
            {
                var k = t.Know ?? Lore.KnowOf(key, f);
                var distort = k != null ? k.Distort : 0;
                c["verb"] = distort > 0 && Lore.Verb(f.Verb).Distort != "" ? Lore.Verb(f.Verb).Distort : f.Verb;
                c["realverb"] = f.Verb;
                if (f.Verb == "gave_gift") c["giftkind"] = GiftKind(f.Note);
                c["how"] = k != null ? k.How.ToString().ToLowerInvariant() : "participant";
                c["deed"] = Lore.Render(f, key, distort);
                c["when"] = Lore.When(f);
                c["fplace"] = string.IsNullOrEmpty(f.Place) ? "hallway" : f.Place;
                c["public"] = new[] { "infirmary", "club room", "toilet", "restroom", "shower", "changing", "storage", "park", "room" }.Any(x => c["fplace"].Contains(x)) ? "no" : "yes";
                c["valence"] = f.Valence.ToString();
                c["weight"] = f.Weight.ToString();
                c["opinion"] = (k != null ? k.Opinion : f.Valence).ToString();
                c["distorted"] = distort > 0 ? "yes" : "no";
                c["exposure"] = f.Exposure == LoreFact.Secret ? "secret" : f.Exposure == LoreFact.Rumored ? "rumored" : "known";
                c["subj"] = f.Subject == Lore.Player ? "player" : f.Subject == key ? "her" : "other";
                c["obj"] = f.Obj == Lore.Player ? "player" : f.Obj == key ? "her" : string.IsNullOrEmpty(f.Obj) ? "none" : "other";
                c["aboutplayer"] = f.Involves(Lore.Player) ? "yes" : "no";
                var other = new[] { f.Subject, f.Obj, f.About }.FirstOrDefault(x => !string.IsNullOrEmpty(x) && x != key && x != Lore.Player);
                if (other != null) { c["who"] = Teachers.Called(other); c["whobond"] = Social.Bond(h, GirlInfo.Find(other)).ToString(); WhoCtx(h, GirlInfo.Find(other), c); }
                var second = new[] { f.Subject, f.Obj }.Where(x => !string.IsNullOrEmpty(x) && x != key && x != Lore.Player && x != other).Select(Teachers.Called).FirstOrDefault();
                if (second != null) c["who2"] = second;
                if (k != null && !string.IsNullOrEmpty(k.From)) c["src"] = k.From == Lore.Player ? "you" : Teachers.Called(k.From);
                // the story names her as "her" ("...your promise to her"); if her name is still in it, no line names the teller.
                string srcName;
                if (k != null && !string.IsNullOrEmpty(k.From) && k.From != Lore.Player && c.TryGetValue("src", out srcName) && !string.IsNullOrEmpty(srcName) && c["deed"].Contains(srcName))
                {
                    var deed = c["deed"];
                    foreach (var prep in new[] { "to", "with", "on", "about", "for", "from", "at" }) deed = deed.Replace(" " + prep + " " + srcName, " " + prep + " her");
                    c["deed"] = deed;
                    if (deed.Contains(srcName)) c.Remove("src");
                }
                if (!string.IsNullOrEmpty(t.ToldFit)) c["told"] = t.ToldFit;
                if (!string.IsNullOrEmpty(f.Note)) c["note"] = f.Note.Replace("her Monday lunches", "the Monday lunches"); // saved like that before 2026-10-31
                if (f.Since >= 0) { c["since"] = f.Since.ToString(); c["speed"] = f.Since == 0 ? "same_day" : f.Since <= 2 ? "fast" : "normal"; }
                c["thing"] = Thing(f, key);
                c["tags"] = f.Tags;
                if (f.Tags.Contains("romance")) c["romance"] = "yes";
            }
            if (!c.ContainsKey("thing")) c["thing"] = "that";
            // The same thing from her own mouth ("her classes" -> "my classes").
            var th = c["thing"];
            c["mything"] = th.StartsWith("her ", StringComparison.Ordinal) ? "my " + th.Substring(4) : th == "what you two did" ? "that"
                           : th == "your date" ? "our date" : th.StartsWith("what she saw", StringComparison.Ordinal) ? "what I saw" + th.Substring(12) : th;
            c["raised"] = t.Opening ? "yes" : "no";
            MoreCtx(h, gs, c);
            if (!c.ContainsKey("likedo")) c["likedo"] = c.ContainsKey("favdo") ? c["favdo"] : "hang out";
            return c;
        }

        private static string SubjectThing(SaveData.Heroine h, string subject)
        {
            switch (subject)
            {
                case "lunch": return "lunch";
                case "exams": return "the exams";
                case "weekend": return "the weekend";
                case "weather": return Weather.Raining ? "the rain" : "the weather";
                case "club": return "the " + GirlInfo.ClubName(h);
                case "friend": return "her friends";
                case "birthday": return "her birthday";
                case "horoscope": return "horoscopes";
                case "teaching": return "teaching";
                case "nursing": return "the infirmary";
                case "future": return "her plans";
                case "dreams": return "dreams";
                case "family": return "her family";
                case "hometown": return "her hometown";
                case "childhood": return "when she was little";
                case "movies": return "movies";
                case "games": return "games";
                case "season": return "the season";
                case "phone": return "her phone";
                case "parttime": return "part-time jobs";
                case "fears": return "what scares her";
                case "peeves": return "what annoys her";
                case "wish": return "wishes";
                case "memory": return "her memories here";
                case "commute": return "the commute";
                case "travel": return "traveling";
                case "classmates": return "her classmates";
                case "talent": return "hidden talents";
                case "advice": return "advice";
                default: return "school";
            }
        }

        private static string PlaceName()
        {
            var id = Places.CurrentId;
            return string.IsNullOrEmpty(id) ? "hallway" : Places.Name(id).ToLowerInvariant();
        }

        /// <summary>A short noun for the fact, for your answers ("the hallway thing", "her fight with Rin").</summary>
        private static string Thing(LoreFact f, string viewer)
        {
            var other = new[] { f.Subject, f.Obj }.FirstOrDefault(x => !string.IsNullOrEmpty(x) && x != viewer && x != Lore.Player);
            var on = other != null ? GirlInfo.FirstName(other) : "";
            switch (f.Verb)
            {
                case "had_h": return f.Involves(viewer) ? "what you two did" : "what she saw in the " + (string.IsNullOrEmpty(f.Place) ? "hallway" : f.Place);
                case "seen_girls_room": return "the girls' restroom thing";
                case "seen_lockers": return "the changing room thing";
                case "gave_gift": return string.IsNullOrEmpty(f.Note) ? "the present" : "the " + f.Note.Replace("some ", "").Replace("a ", "").Replace("an ", "");
                case "fought": case "insulted": return on != "" ? "the fight with " + on : "the fight";
                case "started_dating": case "engaged": return on != "" ? on + "'s new relationship" : "the new couple";
                case "broke_up": return on != "" ? on + "'s breakup" : "the breakup";
                case "crush": return on != "" ? on + "'s crush" : "the crush";
                case "left_for": case "cheated": return "the drama with " + (on != "" ? on : "them");
                case "seen_together": return "what people saw behind the gym";
                case "helped": return "your help";
                case "ate_with": return "lunch";
                case "walked_home": return "the walk home";
                case "dated": return "your date";
                case "shared": return "that time";
                default: return "what happened";
            }
        }

        // ================= playing a topic =================

        private static void PlayTopic(SaveData.Heroine h, PTopic t)
        {
            var s = Sess;
            if (s == null || h == null) return;
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            MarkTopic(key, t.Id);
            if (t.Merged != null) foreach (var id in t.Merged) MarkTopic(key, id);
            if (t.Kind == "tell") TellHer(h, t);
            else if (t.Fact != null) Lore.Told(key, t.Fact, 1);
            var ctx = Ctx(h, gs, t);
            if (t.Kind == "confide") ConfideHer(h, t, ctx);

            // Her day: the mood she shows and, now and then, one of her likes (the badges under the H bar read these).
            string newLike = null;
            if (t.Kind == "day" && gs.MoodKnownDay != S.Day)
            {
                var unknown = GirlInfo.LikesOf(h).Where(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) == 0).ToList();
                if (unknown.Count > 0 && Random.value < 0.35f + G.Int / 200f) newLike = unknown[Random.Range(0, unknown.Count)];
            }
            if (newLike != null) ctx["newlike"] = GirlInfo.LikeLabel(newLike);

            DlgRule rule;
            var her = Bank.Pick(t.Kind, ctx, key, out rule) ?? Fallback(h, t);
            // A line with a hook has one subject, so nothing is glued to it (2026-10-31, the fifth complaint: a tail on another
            // subject and a like after it left the options answering neither).
            if (rule != null && rule.Hook != "") { ctx["hook"] = rule.Hook; her = Compose(her, ctx, true); }   // only an opener, now and then
            else her = Compose(her, ctx);
            // Your line comes after hers is drawn, though you say it first: with a hook, you ask what she's about to answer.
            var me = t.Opening ? null : ComposeMe(Bank.Pick("me." + t.Kind, ctx, "player", out _ignored), ctx);
            // The like she lets slip waits for the end of the exchange (OnMove): said here, it stood between her line and your options.
            if (newLike != null)
            {
                ctx["closing"] = Bank.Pick("likehint", ctx, key, out _ignored) ?? Lines.LikeHint(GirlInfo.ToneOf(h), GirlInfo.LikeLabel(newLike), Lines.Closeness(h));
                ctx["closinglike"] = newLike;
            }
            CampusLifePlugin.Log.LogInfo("Talk topic: " + GirlInfo.FirstName(h) + " " + t.Kind + " '" + t.Label + "' rule " + (rule != null ? rule.Id : "(fallback)"));

            if (t.Kind == "day" && gs.MoodKnownDay != S.Day)
            {
                gs.MoodKnownDay = S.Day;
                var tip = Lines.MoodTip(gs.Mood);
                Toasts.Show(GirlInfo.FirstName(h) + ": " + Lines.MoodName(gs.Mood) + (tip == "" ? "" : ". " + tip));
            }
            if (t.Kind == "small" && ctx.ContainsKey("like") && ctx["likes"] == "yes")
            {
                var bit = 1 << Array.IndexOf(GirlInfo.LikeIds, t.Subject.Substring(5));
                if ((gs.KnownLikes & bit) == 0) { gs.KnownLikes |= bit; Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + ctx["like"] + "."); }
            }
            Step(h, t, ctx, 1, me, her, rule);
        }

        /// <summary>The like she kept for the end of the exchange: she says it now and you learn it now.</summary>
        private static void SayClosing(SceneBuilder b, SaveData.Heroine h, Dictionary<string, string> ctx)
        {
            string closing, like;
            if (!ctx.TryGetValue("closing", out closing) || !ctx.TryGetValue("closinglike", out like)) return;
            b.Her_(closing);
            S.Girl(GirlInfo.Key(h)).KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, like);
            Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + GirlInfo.LikeLabel(like) + ".");
        }

        private static DlgRule _ignored;

        private static string Fallback(SaveData.Heroine h, PTopic t)
        {
            switch (t.Kind)
            {
                case "day": return AskMood(h, MoodOf(h));
                case "gossip": case "you": case "us": return t.Fact != null ? "You know... " + Lore.Render(t.Fact, GirlInfo.Key(h), t.Know != null ? t.Know.Distort : 0) + ". " + char.ToUpperInvariant(Lore.When(t.Fact)[0]) + Lore.When(t.Fact).Substring(1) + "." : "Hm.";
                default: return Everyday(h, "fallback");
            }
        }

        /// <summary>One step: her line (your line first on step 1), then your moves on what she said.</summary>
        private static void Step(SaveData.Heroine h, PTopic t, Dictionary<string, string> ctx, int step, string me, string her, DlgRule rule)
        {
            var s = Sess;
            if (s == null) return;
            Said(ctx, rule);
            var moves = Moves(h, t, ctx, rule, step);
            CampusLifePlugin.Log.LogInfo("Talk moves: " + GirlInfo.FirstName(h) + " '" + her + "' => " + string.Join(" | ", moves.Select(m => m.Label).ToArray()));
            var b = new SceneBuilder(h, true);
            if (me != null) { if (me.StartsWith("*")) b.Narr(me.Substring(1)); else b.Me(me); }
            b.Face(h, FeelOf(rule, ctx)).Her_(her);
            var args = new List<string>();
            foreach (var m in moves) { args.Add(m.Label); args.Add(m.Tag); }
            b.Choice(args.ToArray());
            foreach (var m in moves) { b.Branch(m.Tag); b.End(); }
            Hide();
            if (!Scenes.PlayInTalk(b, c => OnMove(h, t, ctx, step, moves.FirstOrDefault(m => m.Tag == c)), false)) Finish(s, t);
        }

        /// <summary>The rule of the line she just said: the labels of your options can answer that line ("when said=...").</summary>
        private static void Said(Dictionary<string, string> ctx, DlgRule rule)
        {
            if (rule != null) ctx["said"] = rule.Id.Split('~')[0];
            else ctx.Remove("said");
            // What kind of line it was (small talk): a statement, a question or something she offers. The options answer that.
            if (rule != null && rule.Shape != "") ctx["shape"] = rule.Shape;
            else ctx.Remove("shape");
        }

        private static void OnMove(SaveData.Heroine h, PTopic t, Dictionary<string, string> ctx, int step, PMove m)
        {
            var s = Sess;
            if (s == null) return;
            try
            {
                var key = GirlInfo.Key(h);
                if (t.Fact != null) Lore.Told(key, t.Fact, 2);
                if (m == null || m.Id == "change") { Finish(s, t); return; }
                var lvl = t.Kind == "us" && t.Fact != null && (t.Fact.Tags.Contains("romance") || t.Fact.Tags.Contains("h")) ? Lvl.Romance : Lvl.Friend;
                Pay(s, lvl, m.Q, "topic:" + t.Id + ":" + step);
                ctx["move"] = m.Id;
                ctx["fit"] = m.Q.ToString().ToLowerInvariant();
                ctx["step"] = step.ToString();
                MoveEffect(h, t, m);
                DlgRule rr;
                var react = Bank.Pick("react", ctx, key, out rr);
                var goesOn = step < 3 && m.Q != Q.Bad && rr != null && rr.Opens.Length > 0;
                react = react == null ? BeatSay(h, "", QWord(m.Q)) ?? "..." : !goesOn && m.Q == Q.Good ? Close(react, ctx, ctx.ContainsKey("hook")) : react;
                var mine = PlayerLine(h, ctx, m);
                CampusLifePlugin.Log.LogInfo("Talk move: " + GirlInfo.FirstName(h) + " step " + step + " " + m.Id + " '" + m.Label + "' -> " + m.Q + ", rule " + (rr != null ? rr.Id : "(stock)") + ": you '" + mine + "', she '" + react + "'");
                if (goesOn) { Step(h, t, ctx, step + 1, mine, react, rr); return; }
                var b = new SceneBuilder(h, true);
                if (mine != null) { if (mine.StartsWith("*")) b.Narr(mine.Substring(1)); else b.Me(mine); }
                b.Face(h, rr != null && rr.Feel != "" ? FeelOf(rr, ctx) : FaceFor(m.Q)).Her_(react);
                if (m.Q != Q.Bad) SayClosing(b, h, ctx);
                Hide();
                if (!Scenes.PlayInTalk(b, _ => Finish(s, t), false)) Finish(s, t);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); Finish(s, t); }
        }

        /// <summary>
        /// What you say with the option you took: a written line of the bank for that move and
        /// what she said ("me.move.(move)" rules, same context), else the label in your own words (Speech.Line).
        /// </summary>
        private static string PlayerLine(SaveData.Heroine h, Dictionary<string, string> ctx, PMove m)
        {
            try
            {
                DlgRule r;
                var w = Bank.Pick("me.move." + m.Id, ctx, "player", out r);
                if (!string.IsNullOrEmpty(w)) return w;
            }
            catch (Exception e) { CampusLifePlugin.Log.LogWarning("PlayerLine: " + e.Message); }
            return Speech.Line(m.Label);
        }

        private static void Finish(Session s, PTopic t)
        {
            if (t.Opening) { if (s != null) s.Turns++; AfterScene(); }
            else AfterBeat(s);
        }

        /// <summary>What a move does besides the reward: setting a twisted story straight, asking her to keep quiet.</summary>
        private static void MoveEffect(SaveData.Heroine h, PTopic t, PMove m)
        {
            if (t.Know == null) return;
            if (m.Id == "correct" && t.Know.Distort > 0 && m.Q == Q.Good) { t.Know.Distort = 0; Toasts.Show("You set the story straight."); }
            if (m.Id == "hush" && m.Q == Q.Good) t.Know.Told = 9;   // she keeps it to herself
            if (m.Id == "defend" && m.Q != Q.Bad) t.Know.Opinion = Mathf.Max(t.Know.Opinion, 0);
        }

        private static Feel FeelOf(DlgRule r, Dictionary<string, string> ctx)
        {
            if (r != null && !string.IsNullOrEmpty(r.Feel))
                try { return (Feel)Enum.Parse(typeof(Feel), r.Feel, true); } catch { }
            int v;
            if (ctx.ContainsKey("opinion") && int.TryParse(ctx["opinion"], out v)) return v >= 2 ? Feel.Smile : v <= -2 ? Feel.Sad : v < 0 ? Feel.Worried : Feel.Normal;
            string mood;
            return ctx.TryGetValue("low", out mood) && mood == "yes" ? Feel.Worried : Feel.Normal;
        }

        // ================= moves =================

        private static readonly string[] AllMoves = { "ask", "comfort", "share", "tease", "praise", "invite", "correct", "agree", "defend", "laugh", "admit", "hush", "deny", "thank" };

        /// <summary>Three moves on what she said (from what her line opened, then the topic's usual ones; a line with a hook has only its own), and "change the subject".</summary>
        private static List<PMove> Moves(SaveData.Heroine h, PTopic t, Dictionary<string, string> ctx, DlgRule rule, int step)
        {
            var ids = new List<string>();
            if (rule != null) ids.AddRange(rule.Opens.Where(o => AllMoves.Contains(o)));
            if (!ctx.ContainsKey("hook")) foreach (var d in DefaultMoves(t, ctx)) if (!ids.Contains(d)) ids.Add(d);
            // "correct" only where there is something wrong to correct, "comfort" where it hurts.
            if (!(ctx.ContainsKey("distorted") && ctx["distorted"] == "yes") && !(ctx.ContainsKey("subj") && ctx["subj"] == "player" && ctx.ContainsKey("valence") && Num(ctx, "valence") < 0)) ids.Remove("correct");
            var picked = ids.Take(3).ToList();
            var list = new List<PMove>();
            foreach (var id in picked)
            {
                ctx["move"] = id;
                var label = Bank.Label("move." + id, ctx) ?? DefaultLabel(id, ctx);
                list.Add(new PMove { Id = id, Label = label, Tag = "pm_" + step + "_" + id, Q = Fit(h, id, ctx, rule) });
            }
            ctx.Remove("move");
            list = list.OrderBy(_ => Random.value).ToList();
            list.Add(new PMove { Id = "change", Label = Bank.Label("move.change", ctx) ?? "Change the subject", Tag = "pm_" + step + "_change", Q = Q.Neutral });
            return list;
        }

        private static IEnumerable<string> DefaultMoves(PTopic t, Dictionary<string, string> ctx)
        {
            var low = ctx.ContainsKey("low") && ctx["low"] == "yes" || ctx.ContainsKey("opinion") && Num(ctx, "opinion") < 0;
            switch (t.Kind)
            {
                case "day": return low ? new[] { "comfort", "ask", "share", "invite" } : new[] { "ask", "share", "tease", "praise" };
                case "us": return new[] { "share", "tease", "invite", "thank", "ask" };
                case "you":
                    var interested = ctx.ContainsKey("interest") && ctx["interest"] == "yes";
                    if (ctx.ContainsKey("realverb") && (ctx["realverb"] == "had_h" || ctx["realverb"].StartsWith("seen_", StringComparison.Ordinal)))
                        return interested ? new[] { "admit", "hush", "tease", "deny", "correct" } : new[] { "hush", "tease", "deny", "correct" };
                    return interested ? new[] { "correct", "admit", "ask", "laugh" } : new[] { "correct", "ask", "laugh" };
                case "gossip": return low ? new[] { "ask", "defend", "agree", "laugh" } : new[] { "ask", "agree", "tease", "defend" };
                case "school": return new[] { "ask", "invite", "share", "tease" };
                case "tell": return ctx.ContainsKey("told") && ctx["told"] == "bad" ? new[] { "hush", "defend", "laugh" } : new[] { "ask", "laugh", "agree", "tease" };
                case "confide": return ctx.ContainsKey("hurt") && ctx["hurt"] == "yes" ? new[] { "comfort", "admit", "share", "ask" } : new[] { "ask", "share", "tease", "thank" };
                default: return new[] { "share", "ask", "tease", "praise" };
            }
        }

        private static string DefaultLabel(string id, Dictionary<string, string> ctx)
        {
            var thing = ctx.ContainsKey("thing") ? ctx["thing"] : "that";
            switch (id)
            {
                case "ask": return "Ask more about " + thing;
                case "comfort": return "Tell her it's okay";
                case "share": return "Share your side of " + thing;
                case "tease": return "Tease her about " + thing;
                case "praise": return "Praise her";
                case "invite": return "Suggest doing something together";
                case "correct": return "Say that's not what happened";
                case "agree": return "Agree with her";
                case "defend": return "Defend " + (ctx.ContainsKey("who") ? ctx["who"] : "them");
                case "laugh": return "Laugh it off";
                case "admit": return "Own up to it";
                case "hush": return "Ask her to keep it quiet";
                case "deny": return "Deny everything";
                case "thank": return "Thank her";
                default: return "Nod along";
            }
        }

        private static float Num(Dictionary<string, string> c, string k)
        {
            string v;
            float f;
            return c.TryGetValue(k, out v) && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f) ? f : 0f;
        }

        /// <summary>
        /// How well a move lands, computed (the player learns to read her): her voice, mood, her opinion of the thing,
        /// the relation and her likes. A rule may say it outright ("fit tease=bad").
        /// </summary>
        private static Q Fit(SaveData.Heroine h, string move, Dictionary<string, string> c, DlgRule rule)
        {
            string forced;
            if (rule != null && rule.Fit.TryGetValue(move, out forced)) return forced == "good" ? Q.Good : forced == "bad" ? Q.Bad : Q.Neutral;
            var v = Voice.Of(h);
            var op = c.ContainsKey("opinion") ? Num(c, "opinion") : 0f;
            var low = c.ContainsKey("low") && c["low"] == "yes" || op < 0;
            var close = h.relation >= 1 || h.isGirlfriend;
            var score = 0f;
            // What her line opened lands (2026-10-31): praise on anything but her day always came back as "Flattery won't
            // get you anywhere", comfort on a line that asked for it as "I didn't say I was sad".
            var opened = rule != null && rule.Opens.Contains(move);
            if (move == "invite" && rule != null && (rule.Shape == "offer" || rule.Shape == "show" || rule.Shape == "tell" || rule.Shape == "give")) return Q.Good; // she offered it herself
            switch (move)
            {
                case "ask": score = 0.6f + (v.Verbose - 0.4f) + (Mathf.Abs(op) >= 1 ? 0.4f : 0f) - (v.Shy > 0.75f && !close ? 0.6f : 0f); break;
                case "comfort": score = low || opened ? 1f + v.Warm * 0.5f : -0.2f - (v.Blunt > 0.65f ? 0.4f : 0f); break;
                case "share": score = 0.3f + v.Warm * 0.6f + (c.ContainsKey("likes") && c["likes"] == "yes" ? 0.6f : 0f); break;
                case "tease": score = (v.Tease - 0.45f) * 2.2f + (v.Energy - 0.5f) * 0.6f - (low ? 1f : 0f) - (v.Shy > 0.6f ? 0.5f : 0f) + (close ? 0.3f : -0.3f); break;
                case "praise": score = op > 0 || opened || c["kind"] == "day" && !low ? 0.6f + v.Warm * 0.5f - (v.Blunt > 0.7f && !close ? 0.6f : 0f) : -0.1f; break;
                case "invite": score = (close ? 0.6f : -0.3f) + (h.favor >= 40 ? 0.4f : 0f) + (c.ContainsKey("likes") && c["likes"] == "yes" ? 0.4f : 0f) - (op < -1 ? 0.8f : 0f); break;
                case "correct": score = c.ContainsKey("distorted") && c["distorted"] == "yes" ? 1.2f : (Social.Trust(h) >= 50 ? 0.3f : -0.5f); break;
                case "agree": score = op != 0 ? 0.8f : 0.2f; if (v.Warm > 0.7f && op < 0 && c.ContainsKey("whobond") && Num(c, "whobond") >= 30) score -= 1f; break;
                case "defend": score = (v.Warm - 0.4f) + (c.ContainsKey("whobond") ? Num(c, "whobond") / 60f : 0f) + (op < 0 ? -0.2f : 0.3f); break;
                case "laugh": score = (v.Energy + v.Tease - 0.8f) - (low ? 0.8f : 0f); break;
                case "admit": score = 0.6f + (v.Formal + v.Warm - 0.8f) * 0.5f + (close ? 0.3f : 0f); break;
                case "hush": score = 0.2f + (v.Shy * 0.6f) + (Social.Trust(h) >= 50 ? 0.4f : -0.2f) - (v.Blunt > 0.75f ? 0.4f : 0f); break;
                case "deny": score = c.ContainsKey("how") && c["how"] == "witness" ? -1f : c.ContainsKey("distorted") && c["distorted"] == "yes" ? 0.8f : -0.2f; break;
                case "thank": score = 0.7f + v.Warm * 0.4f; break;
            }
            var q = score >= 0.8f ? Q.Good : score >= 0f ? Q.Neutral : Q.Bad;
            // bem, porque ela falaria de forma convidativa?"): what her own warm line invited never lands badly. An invite she
            // asked for herself (a question, smiling) is a yes; anything else she opened is at worst lukewarm.
            if (opened && WarmFeel(rule))
            {
                if (move == "invite" && (rule.Shape == "q" || rule.Shape == "")) return Q.Good;
                if (q == Q.Bad) q = Q.Neutral;
            }
            return q;
        }

        private static bool WarmFeel(DlgRule rule)
        {
            if (rule == null) return false;
            var f = rule.Feel ?? "";
            return f == "smile" || f == "laugh" || f == "love" || f == "shy" || f == "surprised";
        }


        /// <summary>The topics she would offer now.</summary>
        internal static string DescribeTopics(SaveData.Heroine h)
        {
            if (h == null) return "no girl";
            TalkTopics(h, S.Girl(GirlInfo.Key(h)));
            return GirlInfo.FirstName(h) + ": " + (LastTopics.Count == 0 ? "(none)" : string.Join(" | ", LastTopics.ToArray()));
        }

        /// offers and every small talk subject, without playing them (tell and confide need their effects, so they stay out).</summary>
        internal static string AuditTopics(SaveData.Heroine h)
        {
            if (h == null) return "no girl";
            var key = GirlInfo.Key(h);
            var gs = S.Girl(key);
            var topics = TalkTopics(h, gs).Where(t => t.Kind != "tell" && t.Kind != "confide" && t.Kind != "small").ToList();
            var v = Voice.Of(h);
            var subjects = GirlInfo.LikeIds.Select(l => "like:" + l).Concat(new[] { "place:" + (Places.CurrentId ?? ""), "lunch", "exams", "weather", "school", "weekend", "club", "friend", "birthday", "horoscope", "future" }).ToList();
            if (v.Pet != "") subjects.Add("pet:" + v.Pet);
            if (Teachers.IsTeacherVoice(h)) subjects.Add(Teachers.Job(h) == "nurse" ? "nursing" : "teaching");
            topics.AddRange(subjects.Select(p => new PTopic { Id = "st:" + p, Kind = "small", Subject = p, Label = SmallLabel(p), Score = 0.5f }));
            var sb = new System.Text.StringBuilder();
            foreach (var t in topics)
            {
                var ctx = Ctx(h, gs, t);
                DlgRule rule;
                var her = Bank.Pick(t.Kind, ctx, key, out rule) ?? Fallback(h, t);
                if (rule != null && rule.Hook != "") ctx["hook"] = rule.Hook;
                var me = ComposeMe(Bank.Pick("me." + t.Kind, ctx, "player", out _ignored), ctx);
                Said(ctx, rule);
                sb.Append("\n  [").Append(t.Kind).Append(" ").Append(rule != null ? rule.Id : "fallback").Append("] ").Append(t.Label).Append(": YOU: ").Append(me).Append(" HER: ").Append(her);
                foreach (var m in Moves(h, t, ctx, rule, 1))
                {
                    if (m.Id == "change") continue;
                    ctx["move"] = m.Id;
                    ctx["fit"] = m.Q.ToString().ToLowerInvariant();
                    ctx["step"] = "1";
                    DlgRule rr;
                    var react = Bank.Pick("react", ctx, key, out rr);
                    sb.Append("\n      ").Append(m.Label).Append(" (").Append(m.Q).Append(") -> ").Append(react ?? "(stock)").Append(" [").Append(rr != null ? rr.Id : "stock").Append(rr != null && rr.Opens.Length > 0 ? ", then " + string.Join(",", rr.Opens) : "").Append("]");
                }
            }
            return GirlInfo.FirstName(h) + " (" + gs.Mood + "):" + sb;
        }

        /// <summary>The girl in the open talk, or null.</summary>
        internal static SaveData.Heroine TalkingTo { get { var s = Sess; return s != null ? s.Her : null; } }

        internal static string VarietyNow(SaveData.Heroine h, string on, int tries, string move = "ask", string fit = "good")
        {
            if (h == null) return "no girl";
            var kind = on.StartsWith("me.", StringComparison.Ordinal) ? on.Substring(3) : on;
            var t = TalkTopics(h, S.Girl(GirlInfo.Key(h))).FirstOrDefault(x => x.Kind == kind) ?? new PTopic { Kind = kind, Label = kind };
            var ctx = Ctx(h, S.Girl(GirlInfo.Key(h)), t);
            if (on == "react") { ctx["move"] = move; ctx["fit"] = fit; ctx["step"] = "1"; }
            if (on == "tell") ctx["told"] = fit;
            return GirlInfo.FirstName(h) + " " + Bank.Variety(on, ctx, tries);
        }

        /// <summary>
        /// (the same-day and same-talk filters left out): the note, the gift kind, the rule picked, her line and how many
        /// different lines it has.
        /// </summary>
        /// <summary>
        /// every warm line of hers that opens a move, answered with that move in the worst context (a bad day, her opinion
        /// against). None may land badly unless the rule says so itself.
        /// </summary>
        internal static string RuleFitReport(SaveData.Heroine h)
        {
            if (h == null) return "no girl";
            var worst = new Dictionary<string, string> { { "low", "yes" }, { "opinion", "-1" } };
            int rules = 0, moves = 0, bad = 0, invites = 0, goodInvites = 0;
            var ex = new List<string>();
            foreach (var r in Bank.All)
            {
                if (r == null || r.Opens.Length == 0 || !WarmFeel(r)) continue;
                rules++;
                foreach (var m in r.Opens)
                {
                    if (r.Fit.ContainsKey(m)) continue;
                    moves++;
                    var q = Fit(h, m, worst, r);
                    if (m == "invite") { invites++; if (q == Q.Good) goodInvites++; }
                    if (q == Q.Bad) { bad++; if (ex.Count < 5) ex.Add(r.Id + "/" + m); }
                }
            }
            return GirlInfo.FirstName(h) + ": " + rules + " warm rules that open a move, " + moves + " opened moves in the worst context, " + bad + " land badly" +
                   (ex.Count > 0 ? " (" + string.Join(", ", ex.ToArray()) + ")" : "") + "; invites " + goodInvites + "/" + invites + " good";
        }

        internal static string UsGiftNow(SaveData.Heroine h)
        {
            if (h == null) return "no girl";
            var key = GirlInfo.Key(h);
            var kv = Lore.Of(key).Where(x => x.Value.Verb == "gave_gift" && x.Value.Between(Lore.Player, key)).OrderByDescending(x => x.Value.Id).FirstOrDefault();
            if (kv.Value == null) return GirlInfo.FirstName(h) + ": no gift memory";
            var f = kv.Value;
            var t = new PTopic { Id = "f:" + f.Id, Kind = "us", Fact = f, Know = kv.Key, Label = UsLabel(f) };
            var ctx = Ctx(h, S.Girl(key), t);
            DlgRule used;
            var line = Bank.Pick("us", ctx, key, out used);
            return GirlInfo.FirstName(h) + ": note '" + f.Note + "', giftkind " + (ctx.ContainsKey("giftkind") ? ctx["giftkind"] : "-") +
                   ", rule " + (used == null ? "none" : used.Id) + ", hook " + (used == null ? "-" : used.Hook) + ": " + line + " || " + Bank.Variety("us", ctx, 60);
        }

        internal static bool PlayTopicNow(string kind)
        {
            var s = Sess;
            if (s == null || Scenes.Running) return false;
            var l = TalkTopics(s.Her, S.Girl(GirlInfo.Key(s.Her)));
            var t = l.FirstOrDefault(x => string.IsNullOrEmpty(kind) || x.Kind == kind || x.Id == kind);
            if (t == null && kind == "raise") t = RaiseTopic(s.Her);
            // "st:<subject>" (st:weather, st:place:library): that small-talk subject even when it is not on the list now.
            if (t == null && kind.StartsWith("st:", StringComparison.Ordinal)) t = new PTopic { Id = kind, Kind = "small", Subject = kind.Substring(3), Label = SmallLabel(kind.Substring(3)), Score = 0.5f };
            if (t == null) return false;
            PlayTopic(s.Her, t);
            return true;
        }
    }
}
