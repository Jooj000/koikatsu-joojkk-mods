using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Two girlfriends who both care for you and get along can agree to share you. It takes three talks: one of them
    /// brings it up (she noticed), then the other, then the three of you together. It works when they like each other and
    /// their personalities allow it; if not, both are hurt for a couple of days. Once agreed, they form a circle: whoever
    /// gets less of your attention gets jealous, and the director turns that into scenes (friction, comfort, sides).
    /// </summary>
    internal static class Harem
    {
        private static CampusState S { get { return Campus.S; } }

        public static bool Agreed(SaveData.Heroine a, SaveData.Heroine b)
        {
            return a != null && b != null && S.Harem.Contains(GirlInfo.Key(a)) && S.Harem.Contains(GirlInfo.Key(b));
        }

        public static List<SaveData.Heroine> Circle { get { return S.Harem.Select(Social.Get).Where(h => h != null).ToList(); } }

        private static string[] Talk { get { return string.IsNullOrEmpty(S.HaremTalk) ? null : S.HaremTalk.Split('\t'); } }
        private static int Step { get { var t = Talk; int n; return t != null && t.Length > 2 && int.TryParse(t[2], out n) ? n : 0; } }

        private static bool InTalk(SaveData.Heroine h)
        {
            var t = Talk;
            return t != null && h != null && (t[0] == GirlInfo.Key(h) || t[1] == GirlInfo.Key(h));
        }

        private static SaveData.Heroine OtherInTalk(SaveData.Heroine h)
        {
            var t = Talk;
            if (t == null) return null;
            return Social.Get(t[0] == GirlInfo.Key(h) ? t[1] : t[0]);
        }

        /// <summary>How well these two could share (bond, trust in you, personalities).</summary>
        public static int Compatibility(SaveData.Heroine a, SaveData.Heroine b)
        {
            var v = Social.Bond(a, b) + (Social.Trust(a) + Social.Trust(b)) / 4;
            foreach (var h in new[] { a, b })
            {
                var q = GirlInfo.Quirks(h);
                var tone = GirlInfo.ToneOf(h);
                if (tone == Tone.Energetic || tone == Tone.Teasing) v += 6;
                if (tone == Tone.Cool) v -= 4;
                if (q.bitch || q.choroi) v += 8;
                if (q.majime) v -= 6;
            }
            return v;
        }

        private static bool Eligible(SaveData.Heroine a, SaveData.Heroine b)
        {
            return a != b && a.isGirlfriend && b.isGirlfriend && a.favor >= 60 && b.favor >= 60 && Social.Bond(a, b) >= 20 && !Agreed(a, b)
                   && !Pairs.IsScorned(a) && !Pairs.IsScorned(b);
        }

        /// <summary>Once a day: a negotiation may start; the circle's jealousy follows your attention.</summary>
        public static void Daily()
        {
            if (!Cfg.StealAndHarem.Value) return;
            if (Talk == null && (S.HaremTalkDay < 0 || S.Day - S.HaremTalkDay >= 7) && Random.value < 0.4f)
            {
                var gfs = Social.Students.Where(h => h.isGirlfriend).ToList();
                foreach (var a in gfs)
                {
                    var b = gfs.FirstOrDefault(x => Eligible(a, x));
                    if (b == null) continue;
                    var first = a.favor >= b.favor ? a : b;
                    var second = first == a ? b : a;
                    S.HaremTalk = GirlInfo.Key(first) + "\t" + GirlInfo.Key(second) + "\t0";
                    S.HaremTalkDay = S.Day;
                    S.Girl(GirlInfo.Key(first)).Pending = "SceneTalk:harem";
                    CampusLifePlugin.Log.LogInfo("Harem: " + GirlInfo.FirstName(first) + " wants to talk about " + GirlInfo.FirstName(second));
                    break;
                }
            }
            // A negotiation left hanging for a week fades.
            if (Talk != null && S.Day - S.HaremTalkDay > 7) S.HaremTalk = "";

            var circle = Circle;
            if (circle.Count < 2) return;
            var att = circle.ToDictionary(h => h, h => S.Girl(GirlInfo.Key(h)).Attention);
            var max = att.Values.Max();
            foreach (var h in circle)
            {
                var gap = max - att[h];
                if (gap < 30) continue;
                var top = circle.First(x => att[x] == max);
                var p = Pairs.Of(h, top);
                p.Jealousy = PairState.Clamp(p.Jealousy + gap / 6);
                var gs = S.Girl(GirlInfo.Key(h));
                if (gs.Mood == Mood.None || gs.Mood == Mood.Cheerful) gs.Mood = Mood.Lonely;
                if (!S.Requests.Any(r => r.State == RequestState.Active && r.Girl == GirlInfo.Key(h) && r.Source == "harem"))
                {
                    var r = Board.Make(RequestKind.Talk, h, "Time with " + GirlInfo.FirstName(h), GirlInfo.FirstName(h) + " feels left behind next to " + GirlInfo.FirstName(top) + ". Spend some time with her.", 5);
                    r.Source = "harem";
                    Board.Add(r);
                }
            }
        }

        public static void PeriodTick() { }

        /// <summary>You took one girlfriend's side in front of the other.</summary>
        public static void OnAttention(SaveData.Heroine h)
        {
            S.Girl(GirlInfo.Key(h)).Attention = Mathf.Min(100, S.Girl(GirlInfo.Key(h)).Attention + 10);
        }

        /// <summary>You found a way to please both: they might be open to sharing.</summary>
        public static void Hint(SaveData.Heroine a, SaveData.Heroine b)
        {
            if (!Cfg.StealAndHarem.Value || Talk != null || !Eligible(a, b)) return;
            S.HaremTalk = GirlInfo.Key(a) + "\t" + GirlInfo.Key(b) + "\t0";
            S.HaremTalkDay = S.Day;
            S.Girl(GirlInfo.Key(a)).Pending = "SceneTalk:harem";
        }

        public static bool ReadyToMeet(SaveData.Heroine a, SaveData.Heroine b)
        {
            return Step >= 2 && InTalk(a) && InTalk(b) && a != b;
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        /// <summary>Parts one and two: each girl, alone with you.</summary>
        public static Subject Subject(SaveData.Heroine h)
        {
            if (!Cfg.StealAndHarem.Value || !InTalk(h) || Step >= 2) return null;
            var t = Talk;
            var first = t[0] == GirlInfo.Key(h);
            if (Step == 0 && !first || Step == 1 && first) return null;
            var o = OtherInTalk(h);
            if (o == null) return null;
            var on = GirlInfo.FirstName(o);
            return new Subject
            {
                Id = "harem", Weight = 3f,
                Write = b =>
                {
                    if (first)
                    {
                        b.Narr(GirlInfo.FirstName(h) + " doesn't meet your eyes at first.");
                        b.Face(h, Feel.Serious).Her_(T(h, "I know about you and " + on + ". I'm not blind... and I'm not angry. I just want to know where I stand. || I know about " + on + "... I'm not angry. I just need to know where I stand. || I saw you with " + on + ". I'm not mad... I only want to understand." + " || " + on + "... I know. Please, can we talk about it?",
                            "Okay, I KNOW about " + on + ". I'm not mad! ...Okay, a little. But mostly I wanna talk about it. || I know about " + on + "! I'm not mad! ...Mostly! Let's talk! || So, " + on + ", huh? Yeah, I know! We need to talk! || Okay, " + on + ". I'm not gonna yell. Probably. Talk to me!",
                            "You're seeing " + on + " too. I'm aware. Let's discuss it like adults." + " || " + on + ". I know. Let's talk calmly. || You're with " + on + " as well. I'd like an explanation. || I'm aware of " + on + ". Let's be adults about it.", "So. " + on + ". Don't bother denying it. Let's talk." + " || " + on + ", hm? I'm not stupid. Talk. || So you've been busy with " + on + ". Explain. || Don't play dumb. " + on + ". Let's talk."));
                        b.Face(h, Feel.Shy).Her_(T(h, "...She's nice. I like her, actually. That makes it harder. || ...She's kind. I like her. That's what makes it hard. || ...I can't even hate her. She's sweet. || ...I like her too. That's the problem.", "The annoying part? I actually like her! || Worst part? She's actually great! || Ugh, I can't even be mad at her! She's nice! || And she's fun! That's so annoying!", "...She's not a bad person. That complicates things. || ...She's a decent person. Inconvenient. || ...I don't dislike her. That's the issue. || ...She's fine. It complicates matters.", "...And she's cute. Ugh. || ...And she's adorable. How annoying. || ...She's cute. I hate that. || ...Annoyingly charming, isn't she."));
                    }
                    else
                    {
                        b.Narr(GirlInfo.FirstName(h) + " has clearly heard something.");
                        b.Face(h, Feel.Worried).Her_(T(h, on + " talked to me... about you. About us. I don't know what to think." + " || " + on + " came to talk to me... about us. I'm confused." + " || " + on + " told me how she feels... and about you." + " || " + on + " and I talked... I don't know what to think now.", on + " told me EVERYTHING. My head is spinning!" + " || " + on + " told me everything! I'm so confused! || Whoa, " + on + " just spilled it all! My brain!" + " || " + on + " came over and talked and now I'm dizzy!",
                            on + " spoke with me. About the three of us." + " || " + on + " spoke to me. About all three of us." + " || " + on + " came to me with a proposal." + " || " + on + " and I had a conversation. About you.", on + " came to me with a very interesting proposal. Care to explain?" + " || " + on + " had a fascinating little idea. Your turn." + " || " + on + " told me something interesting. Explain yourself. || So " + on + " came to me. Clever girl. Now, you."));
                    }
                    b.Choice("Say you care about both of them", "hm_both", "Ask what she would want", "hm_ask", "Say you'll choose if she wants you to", "hm_choose");
                    b.Branch("hm_both"); b.Face(h, Feel.Shy).Her_(T(h, "...Both. I see. ...At least you're honest. || ...Both. I see. Thank you for being honest. || ...Both of us... At least you told me. || ...Both. That hurts, but... you were honest.", "...Both. Huh. Okay... okay. || ...Both?! Huh. Okay. Okay okay. || ...Both. Wow. Alright... || ...Both! Okay, deep breaths!", "...Both. Noted. || ...Both. Understood. || ...Both. I see. || ...Both. Honest, at least.", "...Greedy. ...Honest, though. || ...Both? Greedy boy. Honest boy. || ...Both. Bold. I'll give you that. || ...Both. You want it all, don't you.")); b.End();
                    b.Branch("hm_ask"); b.Face(h, Feel.Normal).Her_(T(h, "...I want to stay by your side. If that means sharing... maybe I can. || ...I want to stay with you. If sharing is the way... I'll try. || ...I don't want to leave you. Maybe I can share. || ...If it means staying by you... I'll try.", "...I don't wanna lose you. Or her, kind of. Is that weird? || ...I don't want to lose you! Or, um, her either? || ...I wanna keep you! Is sharing weird? Maybe not! || ...I can't give you up. So... okay, maybe!", "...I want to be treated fairly. That's my condition. || ...Fair treatment. That's my condition. || ...If it's equal, I'll consider it. || ...Treat us equally. Then we'll see.", "...I want my share of you. A big share. || ...I want my portion of you. A generous one. || ...Fine, but I get the bigger half. || ...I'll share. But I'm first in line.")); b.End();
                    b.Branch("hm_choose"); b.Face(h, Feel.Sad).Her_(T(h, "...Don't. I don't want you to have to choose. Not like that. || ...No. I don't want to make you choose like that. || ...Please don't. I don't want you to pick. || ...That's not what I want. Don't choose.", "...No! Don't make it a contest! || ...No way! This isn't a competition! || ...Nope! Don't you dare pick! || ...Stop! I don't want a winner and a loser!", "...That's not what I asked for. || ...I didn't ask you to choose. || ...That's not the question. || ...No. That's not it.", "...Don't be dramatic. That's not the point. || ...Don't be so dramatic. || ...Ugh, so theatrical. Not the point. || ...Calm down, hero. Nobody asked that.")); b.End();
                },
                Result = c =>
                {
                    var tt = Talk;
                    if (tt == null) return;
                    if (c == "hm_both" || c == "hm_ask") { Social.AddTrust(h, c == "hm_ask" ? 6 : 3); S.HaremTalk = tt[0] + "\t" + tt[1] + "\t" + (Step + 1); if (Step == 1) S.Girl(tt[1]).Pending = "SceneTalk:harem"; }
                    else { S.HaremTalk = ""; Social.AddTrust(h, -2); }
                    Memories.Add(h, "harem", "we talked about " + on + " and me", 0, true, GirlInfo.Key(o));
                },
                Interest = 0,
            };
        }

        /// <summary>Part three: the three of you together.</summary>
        public static void WriteMeeting(SaveData.Heroine a, SaveData.Heroine o, SceneBuilder intro, SceneBuilder b, out Action<string> result)
        {
            var comp = Compatibility(a, o);
            intro.Say(a, T(a, "We... both wanted to talk to you. Together. || We both wanted to talk to you... together. || Um... the two of us have something to say. || We came together... to talk with you.", "Okay! Family meeting! Kinda! || Alright! Group meeting time! || Surprise! We're both here! || Okay, team talk! All three of us!", "The three of us need to talk. || We three need to speak. || We came to talk. Together. || It's time we all talked.", "Well. Here we all are. || Well, well. All of us together. || Look who's here. Both of us. || Gather round. This concerns you."));
            intro.Say(o, T(o, "Please hear us out... || Please listen to us... || Please don't be scared... just listen. || Will you hear us out?", "Don't run away! || Don't you dare run! || Stay right there! || No escaping! Sit!", "It's time. || Now. || Listen. || We're starting.", "No escaping now~ || You can't run now~ || Trapped, aren't you? || No exits, darling~"));
            b.Narr(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(o) + " stand side by side. Someone has clearly rehearsed this.");
            b.Say(a, T(a, "We talked. A lot. We both... want to be with you. || We talked a lot... and we both want to be with you. || We both... love you. We talked it through. || After a long talk... we both chose you.", "So we talked, and... we both love you! Like, both of us! || We talked and talked and... we BOTH love you! || Big news! We both want you! Together! || So yeah! Both of us! You! Love!", "We've reached an understanding. Mostly. || We reached an agreement. More or less. || We've come to terms. Mostly. || An understanding. Not perfect, but workable.", "We've decided we can share. Probably. Maybe. || We agreed to share. In theory. || We've decided you're shareable. Probably. || We'll split you. Let's see how that goes."));
            b.Say(o, T(o, "But only if it's fair. To both of us. || But it has to be fair for both of us. || Only if we're both treated kindly... || But please be fair to us both.", "But there are RULES! || But there are RULES! Big ones! || With rules! Lots of rules! || But we made a rule list!", "There are conditions. || Conditions apply. || There are terms. || Under conditions.", "Terms and conditions apply~ || Fine print included~ || Of course, there are terms~ || Read the fine print, darling."));
            b.Choice("Promise to treat them equally", "ng_equal", "Let them set the rules", "ng_rules", "Say you can't choose and don't want to", "ng_honest");
            var bonus = 0;
            b.Branch("ng_equal"); b.Label("ng_end");
            b.Narr("For a long moment nobody says anything. Then " + GirlInfo.FirstName(a) + " looks at " + GirlInfo.FirstName(o) + ", and they both start to smile... or don't.");
            b.End();
            b.Branch("ng_rules");
            b.Say(o, T(o, "Rule one: Sundays are taken turns. Rule two: no secrets. || We take turns on Sundays. And no secrets, ever. || Turns on Sundays... and always be honest with us. || First, Sundays in turns. Second... no hiding things.", "Rule one: no favorites! Rule two: dates are split fifty-fifty! Rule three: ...I'll think of more! || One: no favorites! Two: equal dates! Three: ...TBD! || Rule one, equal everything! Rule two, no secrets! || No favorites! Half the dates each! More rules coming!", "Equal time. Full honesty. Non-negotiable. || Equal time. Total honesty. That's all. || Fair time, no secrets. Non-negotiable. || Two rules: equal time, full honesty.", "Rule one: I get Saturdays. Rule two: see rule one. || Rule one: Saturdays are mine. Rule two: see rule one. || Rule one: I win ties. Rule two: I always win ties. || Rules? I get the best days. The end."));
            b.Goto("ng_end");
            b.Branch("ng_honest");
            b.Say(a, T(a, "...That's the most honest thing you've said. || ...That's the most honest thing you've ever said. || ...Thank you. That was honest. || ...You really mean that. I can tell.", "...Okay. That's... actually kinda sweet. || ...Oh. That was really sweet. || ...Okay, that was kinda cute. || ...Aww. Okay. Fine. That's nice.", "...Honest. Good. || ...Honest. Good. || ...That was sincere. Acceptable. || ...Good answer.", "...Disgustingly sincere. || ...Ugh. Sickeningly sincere. || ...Gross. Sweet, but gross. || ...So earnest. Fine."));
            b.Goto("ng_end");
            result = c =>
            {
                bonus = c == "ng_rules" ? 12 : c == "ng_honest" ? 6 : 0;
                var ok = comp + bonus + Random.Range(-10, 11) >= 45;
                S.HaremTalk = "";
                S.HaremTalkDay = S.Day;
                if (ok)
                {
                    foreach (var h in new[] { a, o }) if (!S.Harem.Contains(GirlInfo.Key(h))) S.Harem.Add(GirlInfo.Key(h));
                    Arcs.AddBond(GirlInfo.Key(a), GirlInfo.Key(o), 10);
                    G.AddFavor(a, 5, 2); G.AddFavor(o, 5, 2);
                    Toasts.Show(GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(o) + " agreed to share you. Keep it fair.");
                    Anthology.Add("harem", GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(o) + " agreed to share you", 4, GirlInfo.Key(a) + "," + GirlInfo.Key(o));
                }
                else
                {
                    foreach (var h in new[] { a, o }) { var gs = S.Girl(GirlInfo.Key(h)); gs.ScornedUntil = S.Day + 1; gs.Scorned = 40; gs.Redemption = 0; G.AddFavor(h, -6, 0); }
                    Toasts.Show("It didn't work out. " + GirlInfo.FirstName(a) + " and " + GirlInfo.FirstName(o) + " need a little time.");
                }
                CampusLifePlugin.Log.LogInfo("Harem meeting " + GirlInfo.FirstName(a) + " + " + GirlInfo.FirstName(o) + ": " + (ok ? "agreed" : "failed") + " (compat " + comp + ", bonus " + bonus + ")");
            };
        }

        public static string Force(SaveData.Heroine a, SaveData.Heroine b, int step)
        {
            S.HaremTalk = GirlInfo.Key(a) + "\t" + GirlInfo.Key(b) + "\t" + step;
            S.HaremTalkDay = S.Day;
            if (step == 0) S.Girl(GirlInfo.Key(a)).Pending = "SceneTalk:harem";
            if (step == 1) S.Girl(GirlInfo.Key(b)).Pending = "SceneTalk:harem";
            return "harem talk " + S.HaremTalk + " (compat " + Compatibility(a, b) + ")";
        }
    }
}
