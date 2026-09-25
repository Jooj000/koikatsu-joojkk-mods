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
        }

        private sealed class PMove
        {
            public string Id, Label, Tag;
            public Q Q;
        }

        /// <summary>Last topics built.</summary>
        internal static readonly List<string> LastTopics = new List<string>();

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
                    ctx.Add(new PTopic { Id = "f:" + f.Id, Kind = "us", Fact = f, Know = k, Label = UsLabel(f), Score = Lore.Salience(k, f) });
                }
                else if (you && f.Weight >= 1 && S.Day - k.Day <= 7)
                    ctx.Add(new PTopic { Id = "f:" + f.Id, Kind = "you", Fact = f, Know = k, Label = YouLabel(f, k), Score = Lore.Salience(k, f) * 1.2f, Hot = f.Weight >= 2 && f.Valence <= 0 || f.Verb == "had_h" });
            }

            if (!stranger)
            {
                var st = SchoolTopic(h);
                if (st != null && st.Id != "rumors" && !TopicDone(key, "school:" + st.Id)) ctx.Add(new PTopic { Id = "school:" + st.Id, Kind = "school", School = st, Label = "Talk about " + st.Label, Score = 1.4f });
            }

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
            return t;
        }

        private static string UsLabel(LoreFact f)
        {
            var when = Lore.When(f);
            switch (f.Verb)
            {
                case "gave_gift": return "Ask if she liked " + (string.IsNullOrEmpty(f.Note) ? "your present" : "the " + f.Note.Replace("a ", "").Replace("an ", ""));
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
            if (v.Pet != "") subjects["pet:" + v.Pet] = 1.3f;
            subjects["place:" + (Places.CurrentId ?? "")] = 0.6f;
            if (G.CurrentMask == PeriodMask.Lunch) subjects["lunch"] = 1f;
            if (Calendar.IsExamWeek) subjects["exams"] = 1.2f;
            subjects["weather"] = 0.35f;
            subjects["school"] = 0.5f;
            subjects["weekend"] = 0.5f;
            if (!stranger)
            {
                if (!GirlInfo.InNoClub(h)) subjects["club"] = 0.9f;
                if (Social.FriendsOf(h, 45).Any()) subjects["friend"] = 0.8f;
                var bd = BirthdayOf(h);
                if (bd != "no") subjects["birthday"] = 1.5f;
                subjects["horoscope"] = 0.4f;
                if (h.isTeacher) subjects["teaching"] = 1f;
                if (h.relation >= 1) subjects["future"] = 0.4f;
            }
            var open = subjects.Where(x => !TopicDone(key, "st:" + x.Key)).ToList();
            var picked = open.OrderByDescending(x => x.Value + Random.value * 0.9f).Take(count).Select(x => x.Key).ToList();
            return picked.Select(p => new PTopic { Id = "st:" + p, Kind = "small", Subject = p, Label = stranger ? StrangerLabel(p) : SmallLabel(p), Score = 0.5f }).ToList();
        }

        private static string StrangerLabel(string subject)
        {
            if (subject.StartsWith("place:", StringComparison.Ordinal)) return "Say something about " + PlaceThing();
            if (subject == "weather") return "Make small talk about the weather";
            if (subject == "lunch") return "Ask if she's had lunch";
            return "Make small talk";
        }

        private static string SmallLabel(string subject)
        {
            if (subject.StartsWith("like:", StringComparison.Ordinal)) return "Chat about " + GirlInfo.LikeLabel(subject.Substring(5));
            if (subject.StartsWith("pet:", StringComparison.Ordinal)) return "Chat about " + subject.Substring(4);
            if (subject.StartsWith("place:", StringComparison.Ordinal)) return "Comment on " + PlaceThing();
            switch (subject)
            {
                case "lunch": return "Chat about lunch";
                case "exams": return "Chat about the exams";
                case "weather": return "Chat about the weather";
                case "weekend": return "Chat about the weekend";
                case "club": return "Ask how her club is going";
                case "friend": return "Chat about her friends";
                case "birthday": return "Ask about her birthday";
                case "horoscope": return "Chat about horoscopes";
                case "teaching": return "Ask what teaching is like";
                case "future": return "Ask about her plans after school";
                default: return "Make small talk";
            }
        }

        /// <summary>
        /// Something she raises herself before you say a word: what she saw or heard about you lately that weighs (the
        /// H in the hallway, being seen where you should not), not brought up yet.
        /// </summary>
        private static PTopic RaiseTopic(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            var best = Lore.Of(key).Where(x => x.Value.Involves(Lore.Player) && x.Key.How != How.Participant && x.Key.ToPlayer == 0 && x.Value.Weight >= 2 && S.Day - x.Key.Day <= 3 && !SharedAway(h, x.Value))
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
            var mood = MoodOf(h);
            c["mood"] = mood.ToString().ToLowerInvariant();
            c["moodword"] = mood == CampusLife.Mood.None ? "fine" : Lines.MoodName(mood).ToLowerInvariant();
            c["low"] = Low(mood) || mood == CampusLife.Mood.Hungry || mood == CampusLife.Mood.Bored ? "yes" : "no";
            var pm = G.CurrentMask;
            c["period"] = pm == PeriodMask.Lunch ? "lunch" : pm == PeriodMask.Club ? "club" : pm == PeriodMask.After ? "after" : "day";
            c["place"] = PlaceName();
            if (!GirlInfo.InNoClub(h)) { c["club"] = GirlInfo.ClubName(h); c["hasclub"] = "yes"; }
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
                else if (t.Subject.StartsWith("place:", StringComparison.Ordinal)) c["thing"] = PlaceThing();
                else c["thing"] = SubjectThing(h, t.Subject);
            }
            if (t.School != null) { c["event"] = t.School.Label; c["eventline"] = t.School.Line; c["school"] = t.School.Id.StartsWith("ev:", StringComparison.Ordinal) ? "event" : t.School.Id; c["thing"] = t.School.Label; }
            if (t.Kind == "day")
            {
                c["thing"] = mood == CampusLife.Mood.Stressed ? (Calendar.IsExamWeek ? "the exams" : "her classes") : mood == CampusLife.Mood.Sleepy ? "last night" : mood == CampusLife.Mood.Hungry ? "lunch"
                             : mood == CampusLife.Mood.Lonely ? "her friends" : !GirlInfo.InNoClub(h) && (G.CurrentMask == PeriodMask.Club || G.CurrentMask == PeriodMask.After) ? GirlInfo.ClubName(h) : "her day";
                c["already"] = gs.MoodKnownDay == S.Day ? "yes" : "no";
            }
            var f = t.Fact;
            if (f != null)
            {
                var k = t.Know ?? Lore.KnowOf(key, f);
                var distort = k != null ? k.Distort : 0;
                c["verb"] = distort > 0 && Lore.Verb(f.Verb).Distort != "" ? Lore.Verb(f.Verb).Distort : f.Verb;
                c["realverb"] = f.Verb;
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
                if (other != null) { c["who"] = GirlInfo.FirstName(other); c["whobond"] = Social.Bond(h, GirlInfo.Find(other)).ToString(); WhoCtx(h, GirlInfo.Find(other), c); }
                var second = new[] { f.Subject, f.Obj }.Where(x => !string.IsNullOrEmpty(x) && x != key && x != Lore.Player && x != other).Select(GirlInfo.FirstName).FirstOrDefault();
                if (second != null) c["who2"] = second;
                if (k != null && !string.IsNullOrEmpty(k.From)) c["src"] = k.From == Lore.Player ? "you" : GirlInfo.FirstName(k.From);
                if (!string.IsNullOrEmpty(t.ToldFit)) c["told"] = t.ToldFit;
                if (!string.IsNullOrEmpty(f.Note)) c["note"] = f.Note;
                if (f.Since >= 0) { c["since"] = f.Since.ToString(); c["speed"] = f.Since == 0 ? "same_day" : f.Since <= 2 ? "fast" : "normal"; }
                c["thing"] = Thing(f, key);
                c["tags"] = f.Tags;
                if (f.Tags.Contains("romance")) c["romance"] = "yes";
            }
            if (!c.ContainsKey("thing")) c["thing"] = "that";
            // The same thing from her own mouth ("her classes" -> "my classes").
            var th = c["thing"];
            c["mything"] = th.StartsWith("her ", StringComparison.Ordinal) ? "my " + th.Substring(4) : th;
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
                case "weather": return "the weather";
                case "club": return "the " + GirlInfo.ClubName(h);
                case "friend": return "her friends";
                case "birthday": return "her birthday";
                case "horoscope": return "horoscopes";
                case "teaching": return "teaching";
                case "future": return "her plans";
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
                case "gave_gift": return string.IsNullOrEmpty(f.Note) ? "the present" : "the " + f.Note.Replace("a ", "").Replace("an ", "");
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
            if (t.Kind == "tell") TellHer(h, t);
            else if (t.Fact != null) Lore.Told(key, t.Fact, 1);
            var ctx = Ctx(h, gs, t);

            // Her day: the mood she shows and, now and then, one of her likes (the badges under the H bar read these).
            string newLike = null;
            if (t.Kind == "day" && gs.MoodKnownDay != S.Day)
            {
                var unknown = GirlInfo.LikesOf(h).Where(l => (gs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, l))) == 0).ToList();
                if (unknown.Count > 0 && Random.value < 0.35f + G.Int / 200f) newLike = unknown[Random.Range(0, unknown.Count)];
            }
            if (newLike != null) ctx["newlike"] = GirlInfo.LikeLabel(newLike);

            DlgRule rule;
            var me = t.Opening ? null : ComposeMe(Bank.Pick("me." + t.Kind, ctx, "player", out rule), ctx);
            var her = Compose(Bank.Pick(t.Kind, ctx, key, out rule) ?? Fallback(h, t), ctx);
            var hint = newLike != null ? Bank.Pick("likehint", ctx, key, out _ignored) ?? Lines.LikeHint(GirlInfo.ToneOf(h), GirlInfo.LikeLabel(newLike), Lines.Closeness(h)) : null;
            CampusLifePlugin.Log.LogInfo("Talk topic: " + GirlInfo.FirstName(h) + " " + t.Kind + " '" + t.Label + "' rule " + (rule != null ? rule.Id : "(fallback)"));

            if (t.Kind == "day" && gs.MoodKnownDay != S.Day)
            {
                gs.MoodKnownDay = S.Day;
                var tip = Lines.MoodTip(gs.Mood);
                Toasts.Show(GirlInfo.FirstName(h) + ": " + Lines.MoodName(gs.Mood) + (tip == "" ? "" : ". " + tip));
            }
            if (newLike != null)
            {
                gs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, newLike);
                Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + GirlInfo.LikeLabel(newLike) + ".");
            }
            if (t.Kind == "small" && ctx.ContainsKey("like") && ctx["likes"] == "yes")
            {
                var bit = 1 << Array.IndexOf(GirlInfo.LikeIds, t.Subject.Substring(5));
                if ((gs.KnownLikes & bit) == 0) { gs.KnownLikes |= bit; Toasts.Show("Learned: " + GirlInfo.FirstName(h) + " likes " + ctx["like"] + "."); }
            }
            Step(h, t, ctx, 1, me, her, hint, rule);
        }

        private static DlgRule _ignored;

        private static string Fallback(SaveData.Heroine h, PTopic t)
        {
            switch (t.Kind)
            {
                case "day": return Lines.MoodLine(GirlInfo.ToneOf(h), MoodOf(h), Lines.Closeness(h));
                case "gossip": case "you": case "us": return t.Fact != null ? "You know... " + Lore.Render(t.Fact, GirlInfo.Key(h), t.Know != null ? t.Know.Distort : 0) + ". " + char.ToUpperInvariant(Lore.When(t.Fact)[0]) + Lore.When(t.Fact).Substring(1) + "." : "Hm.";
                default: return T(h, Lines.Pick("Mm... it's nice, just talking like this.", "I like these quiet chats.", "Sorry, I'm not very interesting today...", "Talking with you is relaxing."),
                                  Lines.Pick("Hey, this is fun! We should talk more!", "Ha, you always find something to say!", "I like hanging out like this!", "Okay, your turn to pick a topic!"),
                                  Lines.Pick("...This is fine.", "Quiet talk. I don't mind it.", "You're not bad company.", "...Go on, then."),
                                  Lines.Pick("Just chatting? Sure, I've got time for you.", "Running out of topics already?", "You just like hearing my voice, huh?", "Fine, entertain me."));
            }
        }

        /// <summary>One step: her line (your line first on step 1), then your moves on what she said.</summary>
        private static void Step(SaveData.Heroine h, PTopic t, Dictionary<string, string> ctx, int step, string me, string her, string extra, DlgRule rule)
        {
            var s = Sess;
            if (s == null) return;
            var moves = Moves(h, t, ctx, rule, step);
            var b = new SceneBuilder(h, true);
            if (me != null) b.Me(me);
            b.Face(h, FeelOf(rule, ctx)).Her_(her);
            if (extra != null) b.Her_(extra);
            var args = new List<string>();
            foreach (var m in moves) { args.Add(m.Label); args.Add(m.Tag); }
            b.Choice(args.ToArray());
            foreach (var m in moves) { b.Branch(m.Tag); b.End(); }
            Hide();
            if (!Scenes.PlayInTalk(b, c => OnMove(h, t, ctx, step, moves.FirstOrDefault(m => m.Tag == c)), false)) Finish(s, t);
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
                react = react != null ? Compose(react, ctx) : Stock(m.Q)[(int)GirlInfo.ToneOf(h)];
                CampusLifePlugin.Log.LogInfo("Talk move: " + GirlInfo.FirstName(h) + " step " + step + " " + m.Id + " -> " + m.Q + ", rule " + (rr != null ? rr.Id : "(stock)"));
                if (step < 3 && m.Q != Q.Bad && rr != null && rr.Opens.Length > 0) { Step(h, t, ctx, step + 1, null, react, null, rr); return; }
                var b = new SceneBuilder(h, true);
                b.Face(h, rr != null && rr.Feel != "" ? FeelOf(rr, ctx) : FaceFor(m.Q)).Her_(react);
                Hide();
                if (!Scenes.PlayInTalk(b, _ => Finish(s, t), false)) Finish(s, t);
            }
            catch (Exception e) { CampusLifePlugin.Log.LogError(e); Finish(s, t); }
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

        /// <summary>Three moves on what she said (from what her line opened, then the topic's usual ones), and "change the subject".</summary>
        private static List<PMove> Moves(SaveData.Heroine h, PTopic t, Dictionary<string, string> ctx, DlgRule rule, int step)
        {
            var ids = new List<string>();
            if (rule != null) ids.AddRange(rule.Opens.Where(o => AllMoves.Contains(o)));
            foreach (var d in DefaultMoves(t, ctx)) if (!ids.Contains(d)) ids.Add(d);
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
                case "you": return ctx.ContainsKey("realverb") && (ctx["realverb"] == "had_h" || ctx["realverb"].StartsWith("seen_", StringComparison.Ordinal)) ? new[] { "admit", "hush", "tease", "deny", "correct" } : new[] { "correct", "admit", "ask", "laugh" };
                case "gossip": return low ? new[] { "ask", "defend", "agree", "laugh" } : new[] { "ask", "agree", "tease", "defend" };
                case "school": return new[] { "ask", "invite", "share", "tease" };
                case "tell": return ctx.ContainsKey("told") && ctx["told"] == "bad" ? new[] { "hush", "defend", "laugh" } : new[] { "ask", "laugh", "agree", "tease" };
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
            switch (move)
            {
                case "ask": score = 0.6f + (v.Verbose - 0.4f) + (Mathf.Abs(op) >= 1 ? 0.4f : 0f) - (v.Shy > 0.75f && !close ? 0.6f : 0f); break;
                case "comfort": score = low ? 1f + v.Warm * 0.5f : -0.2f - (v.Blunt > 0.65f ? 0.4f : 0f); break;
                case "share": score = 0.3f + v.Warm * 0.6f + (c.ContainsKey("likes") && c["likes"] == "yes" ? 0.6f : 0f); break;
                case "tease": score = (v.Tease - 0.45f) * 2.2f + (v.Energy - 0.5f) * 0.6f - (low ? 1f : 0f) - (v.Shy > 0.6f ? 0.5f : 0f) + (close ? 0.3f : -0.3f); break;
                case "praise": score = op > 0 || c["kind"] == "day" && !low ? 0.6f + v.Warm * 0.5f - (v.Blunt > 0.7f && !close ? 0.6f : 0f) : -0.1f; break;
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
            return score >= 0.8f ? Q.Good : score >= 0f ? Q.Neutral : Q.Bad;
        }


        /// <summary>The topics she would offer now.</summary>
        internal static string DescribeTopics(SaveData.Heroine h)
        {
            if (h == null) return "no girl";
            TalkTopics(h, S.Girl(GirlInfo.Key(h)));
            return GirlInfo.FirstName(h) + ": " + (LastTopics.Count == 0 ? "(none)" : string.Join(" | ", LastTopics.ToArray()));
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

        internal static bool PlayTopicNow(string kind)
        {
            var s = Sess;
            if (s == null || Scenes.Running) return false;
            var l = TalkTopics(s.Her, S.Girl(GirlInfo.Key(s.Her)));
            var t = l.FirstOrDefault(x => string.IsNullOrEmpty(kind) || x.Kind == kind);
            if (t == null && kind == "raise") t = RaiseTopic(s.Her);
            if (t == null) return false;
            PlayTopic(s.Her, t);
            return true;
        }
    }
}
