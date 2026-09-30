using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Students and each other: romance between two girls (Crush, Talking, Couple, Strained, Broken and back), what they
    /// share (trust, jealousy, intimacy, how known it is) and the stories two students live through on their own (Bonds in
    /// Motion: drifting apart, rivals who must work together, a shared hobby, making up). And the player in the middle:
    /// a girl in a couple who is drawn to you becomes Conflicted; you can steal her (her ex is hurt for a while and tells
    /// her side of it, with a way to make amends) or, with two girlfriends who get along, agree on a harem, where each one
    /// minds being left behind. Everything is gentle: the point is fun, not punishment.
    /// </summary>
    internal static class Pairs
    {
        private static CampusState S { get { return Campus.S; } }

        // ================= who is open to it =================

        /// <summary>Open to romance with other girls: the card's "likes girls", or about a third of the others (fixed per girl).</summary>
        public static bool GirlsOk(SaveData.Heroine h)
        {
            if (h == null || !Cfg.StudentRomance.Value) return false;
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.GirlsOk < 0)
            {
                bool likes;
                try { likes = h.parameter != null && h.parameter.attribute != null && GirlInfo.Quirks(h).likeGirls; } catch { likes = false; }
                gs.GirlsOk = likes || (uint)GirlInfo.Key(h).GetHashCode() % 100 < 35 ? 1 : 0;
            }
            return gs.GirlsOk == 1;
        }

        public static PairState Of(SaveData.Heroine a, SaveData.Heroine b)
        {
            return a == null || b == null ? null : S.Pair(GirlInfo.Key(a), GirlInfo.Key(b));
        }

        /// <summary>Her girlfriend among the students (Couple or Strained), or null.</summary>
        public static SaveData.Heroine PartnerOf(SaveData.Heroine h)
        {
            if (h == null) return null;
            var p = PairOf(h);
            return p == null ? null : Social.Get(p.Other(GirlInfo.Key(h)));
        }

        public static PairState PairOf(SaveData.Heroine h)
        {
            if (h == null) return null;
            var k = GirlInfo.Key(h);
            return S.Pairs.Values.FirstOrDefault(p => (p.A == k || p.B == k) && (p.Stage == PairStage.Couple || p.Stage == PairStage.Strained) && Social.Get(p.Other(k)) != null);
        }

        public static IEnumerable<PairState> Couples { get { return S.Pairs.Values.Where(p => p.Stage == PairStage.Couple || p.Stage == PairStage.Strained); } }

        public static string StageLabel(PairState p)
        {
            if (p == null) return "-";
            switch (p.Stage)
            {
                case PairStage.Crush: return "a crush";
                case PairStage.Talking: return "getting close";
                case PairStage.Couple: return "a couple";
                case PairStage.Strained: return "a couple (rough patch)";
                case PairStage.Broken: return "broke up";
                default: return "friends";
            }
        }

        /// <summary>Small push from a scene: romance both ways and trust between them.</summary>
        public static void Nudge(string a, string b, int romance, int trust)
        {
            var p = S.Pair(a, b);
            if (p == null) return;
            var ha = Social.Get(a);
            var hb = Social.Get(b);
            if (GirlsOk(ha) && GirlsOk(hb)) { p.AddRomance(a, romance); p.AddRomance(b, romance); }
            p.Trust = PairState.Clamp(p.Trust + trust);
            Advance(p);
        }

        /// <summary>You teased two friends about being a couple: if both are open to it, it plants something.</summary>
        public static void OnTeasedTogether(SaveData.Heroine a, SaveData.Heroine b)
        {
            if (!GirlsOk(a) || !GirlsOk(b)) return;
            var p = Of(a, b);
            p.AddRomance(GirlInfo.Key(a), 5);
            p.AddRomance(GirlInfo.Key(b), 5);
            Advance(p);
        }

        /// <summary>She spent a period with someone (another girl, or with you when other is null).</summary>
        public static void NoteTogether(SaveData.Heroine h, SaveData.Heroine other)
        {
            if (h == null || other == null || h == other) return;
            var p = Of(h, other);
            if (p.LastTogether == S.Day * 16 + (int)G.CurrentMask) return;
            p.LastTogether = S.Day * 16 + (int)G.CurrentMask;
            p.Together++;
            if (Social.Bond(h, other) >= 40 && GirlsOk(h) && GirlsOk(other))
            {
                p.AddRomance(GirlInfo.Key(h), Random.Range(0, 3));
                p.AddRomance(GirlInfo.Key(other), Random.Range(0, 3));
            }
            if (p.Stage == PairStage.Couple || p.Stage == PairStage.Strained) { p.Intimacy = PairState.Clamp(p.Intimacy + 1); p.Jealousy = Mathf.Max(0, p.Jealousy - 2); }

            // Time together grows on its own, not only through you: a little closer each time, more
            // with a popular girl; two girls into each other warm up (lewdness), a couple more so.
            var bond = Social.Bond(h, other);
            if (bond > -20) Arcs.AddBond(GirlInfo.Key(h), GirlInfo.Key(other), 1 + (Standing.Of(other) >= 65 || Standing.Of(h) >= 65 ? 1 : 0));
            if (Cfg.StudentH.Value && GirlsOk(h) && GirlsOk(other))
            {
                var couple = p.Stage == PairStage.Couple;
                if (couple || p.Stage == PairStage.Crush || bond >= 60)
                    foreach (var g in new[] { h, other })
                        if (g.lewdness < 100 && (couple || Random.value < 0.5f)) G.AddFavor(g, 0, couple ? 2 : 1);
            }
        }

        // ================= stages =================

        /// <summary>Moves the pair along by its numbers (romance both ways, trust, jealousy). Scenes play the big steps.</summary>
        public static void Advance(PairState p, bool offscreen = false)
        {
            if (p == null) return;
            var before = p.Stage;
            var ha = Social.Get(p.A);
            var hb = Social.Get(p.B);
            if (ha == null || hb == null) return;
            var ok = GirlsOk(ha) && GirlsOk(hb);
            var hi = Mathf.Max(p.RomAB, p.RomBA);
            var lo = Mathf.Min(p.RomAB, p.RomBA);
            switch (p.Stage)
            {
                case PairStage.None:
                    if (ok && hi >= 30) p.Stage = PairStage.Crush;
                    break;
                case PairStage.Crush:
                    if (lo >= 35) p.Stage = PairStage.Talking;
                    else if (hi < 15) p.Stage = PairStage.None;
                    break;
                case PairStage.Talking:
                    // The step to Couple is a confession (a director scene) when the player is around; off screen it happens by itself.
                    if (offscreen && lo >= 60 && Random.value < 0.35f) BecomeCouple(p, null);
                    else if (lo < 20) p.Stage = PairStage.Crush;
                    break;
                case PairStage.Couple:
                    if (p.Jealousy >= 70 || p.Trust <= 25) Strain(p, "jealousy");
                    break;
                case PairStage.Strained:
                    if (p.Trust >= 55 && p.Jealousy <= 35) { p.Stage = PairStage.Couple; News(GirlInfo.FirstName(ha) + " and " + GirlInfo.FirstName(hb) + " made up. They're holding hands again."); Milestone(p, "made up", 2); }
                    else if (p.Trust <= 10 || (offscreen && S.Day - p.StageDay >= 5 && Random.value < 0.3f)) Break(p, "drifted apart");
                    break;
                case PairStage.Broken:
                    if (offscreen && S.Day - p.StageDay >= 7 && lo >= 40 && p.Trust >= 45 && Random.value < 0.25f)
                    {
                        p.Stage = PairStage.Talking; p.StageDay = S.Day;
                        News(GirlInfo.FirstName(ha) + " and " + GirlInfo.FirstName(hb) + " were seen talking again, a little shyly.");
                    }
                    break;
            }
            if (p.Stage != before)
            {
                if (p.Stage != PairStage.Broken && p.Stage != PairStage.Strained) p.StageDay = S.Day;
                CampusLifePlugin.Log.LogInfo("Pair " + GirlInfo.FirstName(ha) + " & " + GirlInfo.FirstName(hb) + ": " + before + " -> " + p.Stage + " (rom " + p.RomAB + "/" + p.RomBA + ", trust " + p.Trust + ", jealousy " + p.Jealousy + ")");
            }
        }

        public static void BecomeCouple(PairState p, string how)
        {
            var ha = Social.Get(p.A);
            var hb = Social.Get(p.B);
            p.Stage = PairStage.Couple;
            p.StageDay = S.Day;
            p.CoupleSince = S.Day;
            p.Trust = Mathf.Max(p.Trust, 55);
            var text = GirlInfo.FirstName(ha) + " and " + GirlInfo.FirstName(hb) + " are a couple now" + (string.IsNullOrEmpty(how) ? "." : " (" + how + ").");
            News(text);
            Milestone(p, "couple", 4);
            Lore.Record("started_dating", p.A, p.B);
            Toasts.Show(text);
        }

        public static void Strain(PairState p, string why)
        {
            if (p.Stage != PairStage.Couple) return;
            p.Stage = PairStage.Strained;
            p.StageDay = S.Day;
            News(GirlInfo.FirstName(p.A) + " and " + GirlInfo.FirstName(p.B) + " are going through a rough patch (" + why + ").");
        }

        public static void Break(PairState p, string why)
        {
            p.Stage = PairStage.Broken;
            p.StageDay = S.Day;
            p.Jealousy = 0;
            p.RomAB = Mathf.Min(p.RomAB, 35);
            p.RomBA = Mathf.Min(p.RomBA, 35);
            var text = GirlInfo.FirstName(p.A) + " and " + GirlInfo.FirstName(p.B) + " broke up (" + why + ").";
            News(text);
            Milestone(p, "breakup", 3);
            Lore.Record("broke_up", p.A, p.B, note: why ?? "");
            Toasts.Show(text);
        }

        public static void News(string text)
        {
            S.SchoolNews.Add(text);
            if (S.SchoolNews.Count > 30) S.SchoolNews.RemoveAt(0);
            S.Log("School life: " + text);
        }

        private static void Milestone(PairState p, string kind, int weight)
        {
            Anthology.Add(kind, GirlInfo.FirstName(p.A) + " and " + GirlInfo.FirstName(p.B) + (kind == "couple" ? " became a couple" : kind == "breakup" ? " broke up" : " made up"), weight, p.A + "," + p.B);
        }

        // ================= every day, off screen =================

        /// <summary>Once a day (new day): romance drifts, couples live on, crushes grow or fade, Bonds in Motion stories move.</summary>
        public static void Daily()
        {
            if (!Cfg.DirectorEnabled.Value) return;
            var students = Social.Students.ToList();
            if (students.Count < 2) return;
            // Close friends who are both open to it start to feel something now and then.
            if (Cfg.StudentRomance.Value)
                foreach (var a in students.Where(GirlsOk).OrderBy(_ => Random.value).Take(6).ToList())
                {
                    var b = Social.FriendsOf(a, 40).FirstOrDefault(GirlsOk);
                    if (b == null) continue;
                    var p = Of(a, b);
                    if (p.Stage == PairStage.Broken && S.Day - p.StageDay < 5) continue;
                    var gain = 1 + (p.Together >= 2 ? 2 : 0) + (Social.Bond(a, b) >= 60 ? 2 : 0) + (GirlInfo.ToneOf(a) == GirlInfo.ToneOf(b) ? 1 : 0);
                    p.AddRomance(GirlInfo.Key(a), Random.Range(0, gain + 1));
                }
            foreach (var p in S.Pairs.Values.ToList())
            {
                if (Social.Get(p.A) == null || Social.Get(p.B) == null) continue;
                if (p.Stage == PairStage.Couple)
                {
                    // Time together keeps a couple well; time apart and secrets wear it.
                    if (p.Together == 0 && Random.value < 0.3f) p.Jealousy = PairState.Clamp(p.Jealousy + 3);
                    p.Trust = PairState.Clamp(p.Trust + (p.Together > 0 ? 1 : 0));
                }
                else if (p.Stage == PairStage.Strained)
                {
                    p.Jealousy = Mathf.Max(0, p.Jealousy - 4);
                    p.Trust = PairState.Clamp(p.Trust + Random.Range(-2, 5));
                }
                else if (p.RomAB > 0 || p.RomBA > 0)
                {
                    // Crushes fade a little without time together.
                    if (p.Together == 0) { p.RomAB = Mathf.Max(0, p.RomAB - 1); p.RomBA = Mathf.Max(0, p.RomBA - 1); }
                }
                Advance(p, true);
            }
            BondsInMotion.Daily(students);
            Harem.Daily();
        }

        /// <summary>Monday: the week's togetherness counters start again.</summary>
        public static void Weekly()
        {
            foreach (var p in S.Pairs.Values) p.Together = 0;
            // Pairs with nothing left to remember are dropped (the save stays small).
            foreach (var k in S.Pairs.Where(kv => kv.Value.Stage == PairStage.None && kv.Value.RomAB < 5 && kv.Value.RomBA < 5 && kv.Value.Arc == PairArc.None).Select(kv => kv.Key).ToList())
                S.Pairs.Remove(k);
        }

        // ================= the player in the middle =================

        /// <summary>How much she holds on to her partner (0-100).</summary>
        public static int Loyalty(SaveData.Heroine h)
        {
            var p = PairOf(h);
            if (p == null) return 0;
            var days = p.CoupleSince < 0 ? 0 : Mathf.Min(30, S.Day - p.CoupleSince);
            var q = GirlInfo.Quirks(h);
            var faithful = q.majime && !q.bitch && !q.choroi ? 100 : q.bitch || q.choroi ? 0 : 50;
            return Mathf.RoundToInt(0.4f * p.Intimacy + 0.3f * p.Trust + 0.2f * days * 100f / 30f + 0.1f * faithful);
        }

        /// <summary>How much she is drawn to you (0-100).</summary>
        public static int Attraction(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return Mathf.RoundToInt(0.4f * h.favor + 0.3f * Social.Trust(h) + 0.2f * gs.Attention + 0.1f * Mathf.Min(100, G.Sens));
        }

        /// <summary>Each free period: girls in a couple who are drawn to you become Conflicted (three periods in a row).</summary>
        public static void PeriodTick()
        {
            if (!Cfg.DirectorEnabled.Value || !Cfg.StealAndHarem.Value) return;
            foreach (var p in Couples.ToList())
                foreach (var k in new[] { p.A, p.B })
                {
                    var h = Social.Get(k);
                    if (h == null) continue;
                    var gs = S.Girl(k);
                    if (Attraction(h) > Loyalty(h) - 10) { gs.Conflicted++; if (gs.Conflicted == 3) { gs.ConflictedDay = S.Day; CampusLifePlugin.Log.LogInfo(GirlInfo.FirstName(h) + " is torn between " + GirlInfo.FirstName(p.Other(k)) + " and you (A " + Attraction(h) + ", L " + Loyalty(h) + ")"); } }
                    else gs.Conflicted = Mathf.Max(0, gs.Conflicted - 1);
                }
        }

        public static bool IsConflicted(SaveData.Heroine h) { return h != null && S.Girl(GirlInfo.Key(h)).Conflicted >= 3 && PairOf(h) != null; }

        /// <summary>
        /// She leaves her partner for you. Her ex is hurt (less trust and favor, cold for a few days, her side of the story
        /// goes around), her ex's friends think a little less of you; nothing is permanent.
        /// </summary>
        public static void Steal(SaveData.Heroine h)
        {
            var p = PairOf(h);
            if (p == null) return;
            var ex = Social.Get(p.Other(GirlInfo.Key(h)));
            Break(p, GirlInfo.FirstName(h) + " chose you");
            Director.Changed(h, "left her partner");
            Director.Changed(ex, "was left");
            S.Girl(GirlInfo.Key(h)).Conflicted = 0;
            if (ex == null) return;
            var es = S.Girl(GirlInfo.Key(ex));
            es.ScornedUntil = S.Day + 3;
            es.Scorned = 70;
            es.Redemption = 0;
            Social.AddTrust(ex, -25);
            G.AddFavor(ex, -15, 0);
            foreach (var f in Social.FriendsOf(ex, 40).Where(f => f != h)) G.AddFavor(f, -Mathf.Clamp(Social.Bond(ex, f) / 20, 1, 4), 0);
            Rumors.Start("stole", GirlInfo.Key(ex), GirlInfo.Key(h), GirlInfo.Key(ex), 70);
            Anthology.Add("steal", GirlInfo.FirstName(h) + " left " + GirlInfo.FirstName(ex) + " for you", 4, GirlInfo.Key(h) + "," + GirlInfo.Key(ex));
            Toasts.Show(GirlInfo.FirstName(ex) + " is hurt. She'll need time... and an apology.");
        }

        public static bool IsScorned(SaveData.Heroine h)
        {
            if (h == null) return false;
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.ScornedUntil >= S.Day;
        }

        /// <summary>The apology can be made once the first day has passed.</summary>
        public static bool CanRedeem(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Scorned > 0 && gs.Redemption == 0 && gs.ScornedUntil - 2 <= S.Day;
        }

        /// <summary>
        /// Redemption, three steps: an apology (talking to her once the first day has passed), a present she likes, then
        /// standing up for her or helping her. Each step costs a little reputation-free effort and softens her.
        /// </summary>
        public static void Redeem(SaveData.Heroine h, string step)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Scorned <= 0) return;
            gs.Redemption++;
            gs.Scorned = Mathf.Max(0, gs.Scorned - 25);
            Social.AddTrust(h, 6);
            if (gs.Redemption == 1) gs.ScornedUntil = S.Day; // she talks again from tomorrow
            if (gs.Scorned <= 0 || gs.Redemption >= 3)
            {
                gs.Scorned = 0;
                gs.ScornedUntil = -1;
                Toasts.Show(GirlInfo.FirstName(h) + " has forgiven you. Mostly.");
                Memories.Add(h, "redeemed", "you kept trying to make things right with me", 1);
                Anthology.Add("redeemed", GirlInfo.FirstName(h) + " forgave you", 2, GirlInfo.Key(h));
            }
            else Toasts.Show(GirlInfo.FirstName(h) + " is softening a little (" + gs.Redemption + "/3). " + (gs.Redemption == 1 ? "Next: a present she likes." : "Next: help her or stand up for her."));
            CampusLifePlugin.Log.LogInfo("Redemption with " + GirlInfo.FirstName(h) + ": step " + gs.Redemption + " (" + step + "), hurt " + gs.Scorned);
        }

        /// <summary>A gift to a girl who is still hurt counts as the second step of the redemption.</summary>
        public static void OnGift(SaveData.Heroine h, bool liked)
        {
            Director.Changed(h, "a gift");
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Scorned > 0 && gs.Redemption == 1 && liked) Redeem(h, "gift");
        }

        /// <summary>Helping her or defending her counts as the third step.</summary>
        public static void OnHelped(SaveData.Heroine h)
        {
            if (h == null) return;
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Scorned > 0 && gs.Redemption == 2) Redeem(h, "help");
        }

        /// <summary>After H with the player: a girl in a couple is cheating; her partner may find out.</summary>
        public static void OnHWithPlayer(SaveData.Heroine h)
        {
            var p = PairOf(h);
            if (p == null) return;
            p.Trust = PairState.Clamp(p.Trust - 10);
            p.Jealousy = PairState.Clamp(p.Jealousy + 15);
            var partner = Social.Get(p.Other(GirlInfo.Key(h)));
            S.Girl(GirlInfo.Key(h)).Conflicted += 2;
            if (partner != null && Random.value < 0.35f)
            {
                Strain(p, GirlInfo.FirstName(partner) + " suspects something");
                Rumors.Start("cheated", GirlInfo.Key(h), "", GirlInfo.Key(partner), 50);
            }
        }

        // ================= what she talks about (Scene Talk subjects) =================

        public static IEnumerable<Subject> Subjects(SaveData.Heroine h)
        {
            if (!Cfg.DirectorEnabled.Value) yield break;
            var t = Torn(h); if (t != null) yield return t;
            var c = Trouble(h); if (c != null) yield return c;
            var cr = Crush(h); if (cr != null) yield return cr;
            var hm = Harem.Subject(h); if (hm != null) yield return hm;
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        private static void Choice(SceneBuilder b, params string[] labelsAndTags) { b.Choice(labelsAndTags); }

        /// <summary>She is torn between her girlfriend and you.</summary>
        private static Subject Torn(SaveData.Heroine h)
        {
            if (!Cfg.StealAndHarem.Value || !IsConflicted(h)) return null;
            var partner = PartnerOf(h);
            if (partner == null) return null;
            var pn = GirlInfo.FirstName(partner);
            var name = GirlInfo.FirstName(h);
            var a = Attraction(h);
            var l = Loyalty(h);
            return new Subject
            {
                Id = "torn", Weight = 4f,
                Write = b =>
                {
                    b.Narr(name + " has been glancing at you all through class. Now that you're alone, she can't seem to start.");
                    b.Face(h, Feel.Worried).Her_(T(h, "I'm with " + pn + "... I really care about her. So why do I keep thinking about you?",
                        "Ugh, this is so unfair! I have " + pn + "! And then YOU keep being... you!",
                        "I have " + pn + ". That should be enough. Lately it isn't. Because of you.",
                        "Let's be honest. I'm with " + pn + ", and I'm standing here thinking about you. That's your fault."));
                    b.Face(h, Feel.Sad).Her_(T(h, "I don't want to hurt anyone. I just... don't know what to do.", "I don't wanna be the bad guy! Tell me what to do!", "...I don't like not knowing what I want.", "So? Say something. I can't be the only one who's confused."));
                    var opts = new List<string> { "Tell her to stay with " + pn, "tn_stay", "Tell her you want her to choose you", "tn_me", "Say you'll wait, whatever she decides", "tn_wait" };
                    if (G.Int >= 35) { opts.Add("Ask what she really wants [INT]"); opts.Add("tn_ask"); }
                    Choice(b, opts.ToArray());
                    b.Branch("tn_stay"); b.Face(h, Feel.Sad).Her_(T(h, "...You're right. Thank you for being honest. Even if it hurts a little.", "...Yeah. Yeah, you're right. Thanks. I mean it.", "...That's the answer I needed. Thank you.", "...How noble. ...Thank you. Really.")); b.End();
                    b.Branch("tn_me");
                    if (a - l > 15) { b.Face(h, Feel.Love).Her_(T(h, "...I think I already have. I'll talk to " + pn + ". It's only fair to her.", "...Okay. Okay! I'll tell " + pn + " today. Before I lose my nerve.", "...Then I choose you. I'll tell " + pn + " myself.", "...Fine. You win. I'll tell " + pn + ". Gently.")); }
                    else { b.Face(h, Feel.Worried).Her_(T(h, "I... can't. Not yet. Please don't push me.", "Whoa, wait! That's too fast! I can't just...!", "...No. Not like this.", "...Don't. I'm not that easy.")); }
                    b.End();
                    b.Branch("tn_wait"); b.Face(h, Feel.Shy).Her_(T(h, "...Thank you. You're too kind to me.", "...You'd wait? ...Thanks.", "...Patient. Hm.", "...You're making this harder, you know.")); b.End();
                    b.Branch("tn_ask");
                    b.Narr("You ask her questions until the truth surfaces on its own: " + (a > l ? "she lights up every time she talks about you." : "she still smiles when she says " + pn + "'s name."));
                    b.Face(h, Feel.Normal).Her_(a > l ? T(h, "...I think I already know. Don't I?", "...Ugh. You figured it out before me.", "...You saw it before I did.", "...Smart. Annoyingly.") : T(h, "...I love her. I think I just needed to hear myself say it.", "...It's " + pn + ". It was always " + pn + ". Thanks.", "...It's her. Thank you.", "...Okay. It's her. Don't look so smug."));
                    b.End();
                },
                Result = c =>
                {
                    var p = PairOf(h);
                    var gs = S.Girl(GirlInfo.Key(h));
                    switch (c)
                    {
                        case "tn_stay": gs.Conflicted = 0; if (p != null) { p.Trust = PairState.Clamp(p.Trust + 10); p.Intimacy = PairState.Clamp(p.Intimacy + 5); } Social.AddTrust(h, 6); Memories.Add(h, "torn", "you told me to stay with " + pn, 1); break;
                        case "tn_me":
                            if (a - l > 15) { Steal(h); G.AddFavor(h, 8, 4); Memories.Add(h, "torn", "you asked me to choose you", 1); Board.Add(Board.Make(RequestKind.Talk, h, "Be there for " + name, name + " broke up with " + pn + " for you. Talk to her tomorrow.", 6)); }
                            else { Social.AddTrust(h, -4); gs.Conflicted = 1; }
                            break;
                        case "tn_wait": Social.AddTrust(h, 3); G.AddFavor(h, 3, 0); break;
                        case "tn_ask": Social.AddTrust(h, 4); if (a <= l) { gs.Conflicted = 0; if (p != null) p.Trust = PairState.Clamp(p.Trust + 6); } break;
                    }
                },
                Interest = 0,
            };
        }

        /// <summary>Her couple is going through a rough patch and she vents to you.</summary>
        private static Subject Trouble(SaveData.Heroine h)
        {
            var p = PairOf(h);
            if (p == null || p.Stage != PairStage.Strained) return null;
            var partner = Social.Get(p.Other(GirlInfo.Key(h)));
            if (partner == null) return null;
            var pn = GirlInfo.FirstName(partner);
            return new Subject
            {
                Id = "couple", Weight = 3f,
                Write = b =>
                {
                    b.Narr(GirlInfo.FirstName(h) + " sighs before you've even said hello.");
                    b.Face(h, Feel.Worried).Her_(T(h, pn + " and I had a fight. It was about something small, and then it wasn't small anymore...", "Me and " + pn + " are fighting! Over NOTHING! And now we're not talking!",
                        pn + " and I aren't speaking. It started over something stupid.", "So. " + pn + " and I are having a 'moment'. Don't laugh."));
                    b.Face(h, Feel.Sad).Her_(T(h, "I miss her. Isn't that silly? She's right there, and I miss her.", "And the worst part? I miss her already!", "...I don't know how to fix it.", "...I don't even know who's supposed to apologize."));
                    Choice(b, "Tell her to talk to " + pn + " directly", "cp_talk", "Take her side", "cp_side", "Suggest a little surprise for " + pn, "cp_date", "Say it's between the two of them", "cp_out");
                    b.Branch("cp_talk"); b.Face(h, Feel.Normal).Her_(T(h, "...You're right. I'll find her after class.", "Yeah! No more sulking! I'm going!", "...Fine. Directly. Ugh.", "...Fine. But if it goes badly, I'm blaming you.")); b.End();
                    b.Branch("cp_side"); b.Face(h, Feel.Smile).Her_(T(h, "...Thank you. It's nice to hear someone's on my side.", "RIGHT?! Thank you!", "...At least someone gets it.", "Finally, a sensible person.")); b.End();
                    b.Branch("cp_date"); b.Face(h, Feel.Shy).Her_(T(h, "A surprise...? She does like strawberry milk... Okay!", "Ooh! A surprise! She'd love that!", "...That might work.", "Hm. Sneaky. I like it.")); b.End();
                    b.Branch("cp_out"); b.Face(h, Feel.Normal).Her_(T(h, "...Mm. You're right. Sorry for dumping it on you.", "Yeah... sorry. Thanks for listening, though!", "...Fair.", "...Fine. Thanks for nothing. Kidding.")); b.End();
                },
                Result = c =>
                {
                    switch (c)
                    {
                        case "cp_talk": p.Trust = PairState.Clamp(p.Trust + 15); p.Jealousy = Mathf.Max(0, p.Jealousy - 15); Social.AddTrust(h, 3); break;
                        case "cp_side": p.Trust = PairState.Clamp(p.Trust - 5); Social.AddTrust(h, 4); G.AddFavor(h, 3, 0); S.Girl(GirlInfo.Key(h)).Conflicted++; break;
                        case "cp_date": p.Trust = PairState.Clamp(p.Trust + 10); p.Intimacy = PairState.Clamp(p.Intimacy + 6); p.Jealousy = Mathf.Max(0, p.Jealousy - 10); Social.AddTrust(h, 3); break;
                        case "cp_out": p.Trust = PairState.Clamp(p.Trust + 3); break;
                    }
                    Advance(p);
                    Memories.Add(h, "couple", "you listened when " + pn + " and I were fighting", 1);
                },
                Interest = 1,
            };
        }

        /// <summary>She has a crush on another student and asks you for advice.</summary>
        private static Subject Crush(SaveData.Heroine h)
        {
            if (!Cfg.StudentRomance.Value || PairOf(h) != null) return null;
            var k = GirlInfo.Key(h);
            var p = S.Pairs.Values.Where(x => (x.A == k || x.B == k) && (x.Stage == PairStage.Crush || x.Stage == PairStage.Talking) && x.RomanceOf(k) >= 35)
                .OrderByDescending(x => x.RomanceOf(k)).FirstOrDefault();
            if (p == null || p.Cooldown >= S.Day) return null;
            var other = Social.Get(p.Other(k));
            if (other == null) return null;
            var on = GirlInfo.FirstName(other);
            return new Subject
            {
                Id = "crush", Weight = 2.2f,
                Write = b =>
                {
                    b.Narr(GirlInfo.FirstName(h) + " pulls you aside, red to the ears.");
                    b.Face(h, Feel.Shy).Her_(T(h, "Can I ask you something embarrassing? It's about " + on + "... I think I like her. Like, LIKE her.", "Okay, emergency! I think I have a crush on " + on + "! What do I DO?!",
                        "I need advice. It's about " + on + ". I... have feelings. It's inconvenient.", "Promise not to laugh. I like " + on + ". Like that. Now say something useful."));
                    var opts = new List<string> { "Encourage her to tell " + on, "cr_go", "Suggest she waits and gets closer first", "cr_wait", "Tease her about it", "cr_tease" };
                    if (h.favor >= 70) { opts.Add("Say she could also like someone else... [SEN]"); opts.Add("cr_me"); }
                    Choice(b, opts.ToArray());
                    b.Branch("cr_go"); b.Face(h, Feel.Surprised).Her_(T(h, "J-just tell her?! ...Okay. Okay. I'll try. Wish me luck!", "Just GO for it?! ...You're right. I'm doing it!", "...Directly. Fine. Logically, yes.", "Bold advice. ...I'll take it.")); b.End();
                    b.Branch("cr_wait"); b.Face(h, Feel.Smile).Her_(T(h, "Mm... spend more time with her first. That feels right.", "Slow and steady! Okay, I can do that!", "...Sensible.", "Patience. Ugh. Fine.")); b.End();
                    b.Branch("cr_tease"); b.Face(h, Feel.Pout).Her_(T(h, "M-mean! I'm serious!", "Hey! I'm being serious here!", "...I regret telling you.", "...Laugh it up. I'll remember this.")); b.End();
                    b.Branch("cr_me");
                    if (G.Sens >= 40 && Random.value < 0.5f + G.Sens / 300f) b.Face(h, Feel.Love).Her_(T(h, "Someone else...? ...You mean you? ...Oh no. Now I'm confused in a whole new way.", "Wait. WAIT. Are you saying... you?! Ahh, my brain!", "...You. That's what you mean. ...Interesting.", "...Oh? Throwing your hat in the ring? ...I'll consider it."));
                    else b.Face(h, Feel.Pout).Her_(T(h, "...That's not funny right now.", "Dude! Not the time!", "...No.", "...Nice try."));
                    b.End();
                },
                Result = c =>
                {
                    p.Cooldown = S.Day + 2;
                    switch (c)
                    {
                        case "cr_go": p.AddRomance(k, 8); Director.Seed("confession", k, p.Other(k)); Social.AddTrust(h, 3); break;
                        case "cr_wait": p.AddRomance(k, 3); p.Trust = PairState.Clamp(p.Trust + 5); Social.AddTrust(h, 2); break;
                        case "cr_tease": Social.AddTrust(h, -2); break;
                        case "cr_me": if (h.favor >= 70 && G.Sens >= 40) { p.AddRomance(k, -10); G.AddFavor(h, 5, 4); } else Social.AddTrust(h, -3); break;
                    }
                    Advance(p);
                    Memories.Add(h, "crush", "I told you about my crush on " + on, 1, false, GirlInfo.Key(other));
                },
                Interest = 1,
            };
        }
    }
}
