using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Concrete options (conversation by scene): instead of "Chat about..." the speech bubble offers three things to say
    /// that fit this moment, built from her mood, the place, a memory you share, her friends, the rumors, her club and the
    /// likes you discovered. Each one runs one of the conversation's turns (a topic, listening, asking about a friend, a
    /// compliment, a joke) with the player's line written for the situation. Options offered lately with her are skipped.
    /// Also: "Ask a favor..." (things you ask of her) and passing on a friend's message.
    /// </summary>
    internal static partial class Conversation
    {
        internal sealed class Concrete
        {
            public string Id = "", Label = "", Kind = "";
            public float Weight = 1f;
            public Action Run;
        }

        private static Concrete C(string id, string kind, string label, float w, Action run)
        {
            return new Concrete { Id = id, Kind = kind, Label = label, Weight = w * Random.Range(0.8f, 1.2f), Run = run };
        }

        public static List<string> LastConcrete = new List<string>();

        internal static List<string> PreviewConcrete(SaveData.Heroine h)
        {
            return ConcreteOptions(new Session { Her = h }).Select(c => c.Label).ToList();
        }

        private static List<Concrete> ConcreteOptions(Session s)
        {
            var h = s.Her;
            var gs = S.Girl(GirlInfo.Key(h));
            var l = new List<Concrete>();
            var mood = MoodOf(h);
            var name = GirlInfo.FirstName(h);

            if (Teachers.IsTeacherVoice(h))
            {
                foreach (var x in Catalog.Where(x => x.Teacher && x.Tier < 0 && Offerable(x, h)))
                { var subj = x; l.Add(C("c:" + x.Id, x.Topic.ToString(), Fill(x.Label, h), 2.5f, () => RunSubject(subj))); }
                l.Add(C("c:t_day", "Day", "Ask how her day is going", 1.5f, () => AskDay(null)));
                return Three(l, gs);
            }

            switch (mood)
            {
                case CampusLife.Mood.Hungry: l.Add(C("c:hungry", "Food", "Ask what she's craving for lunch", 3f, () => RunSubject(Find("m_hungry")))); break;
                case CampusLife.Mood.Sleepy: l.Add(C("c:sleepy", "AboutHer", "Ask if she stayed up late again", 3f, () => RunSubject(Find("m_sleepy")))); break;
                case CampusLife.Mood.Stressed: l.Add(C("c:stressed", "Listen", "Ask what's weighing on her", 3f, () => LetHerTalk("You look like you're carrying the whole school. What's weighing on you?"))); break;
                case CampusLife.Mood.Bored: l.Add(C("c:bored", "Weekend", "Suggest something fun for after class", 3f, () => RunSubject(Find("m_bored")))); break;
                case CampusLife.Mood.Lonely: l.Add(C("c:lonely", "AboutHer", "Say you were hoping to see her", 3f, () => RunSubject(Find("m_lonely")))); break;
                case CampusLife.Mood.Flirty: l.Add(C("c:flirty", "Compliment", "Tell her she looks great today", 3f, () => Compliment("looks", "Is it just me, or do you look really great today?"))); break;
                case CampusLife.Mood.Upset: l.Add(C("c:upset", "Listen", "Ask quietly if something happened", 3.5f, () => LetHerTalk("Hey... did something happen? You don't have to smile for me."))); break;
                case CampusLife.Mood.Cheerful: l.Add(C("c:cheer", "Day", "Ask what's got her smiling", 2.5f, () => AskDay("You're glowing today. What happened?"))); break;
                default: l.Add(C("c:day", "Day", "Ask how her day is going", 2f, () => AskDay(null))); break;
            }

            switch (Places.CurrentId)
            {
                case "library": l.Add(C("c:lib", "Hobbies", "Ask what she's reading", 2f, () => RunSubject(Find("p_library")))); break;
                case "gym": case "field": case "pool": l.Add(C("c:sport", "Sports", "Ask how training is going", 2f, () => RunSubject(Find("p_sport")))); break;
                case "cafeteria": l.Add(C("c:cafe", "Food", "Ask what's good on the menu today", 2f, () => RunSubject(Find("p_cafe")))); break;
                case "rooftop": l.Add(C("c:roof", "AboutHer", "Say the view is better with company", 2f, () => RunSubject(Find("p_roof")))); break;
                case "classroom": l.Add(C("c:class", "Classes", "Ask if she followed today's lesson", 1.6f, () => RunSubject(Find("p_class")))); break;
                case "courtyard": case "park": l.Add(C("c:yard", "Hobbies", "Point out the flowers by the bench", 1.6f, () => RunSubject(Find("p_yard")))); break;
                case "clubroom": if (!GirlInfo.InNoClub(h)) l.Add(C("c:clubroom", "Club", "Ask what the club is working on", 2f, () => RunSubject(Find("p_clubroom")))); break;
            }
            if (!GirlInfo.InNoClub(h) && Places.CurrentId != "clubroom")
                l.Add(C("c:club", "Club", "Ask how " + GirlInfo.ClubName(h) + " practice went", 1.2f, () => RunSubject(Find("club_practice"))));

            // Likes the player discovered (never the quiz: things you learned by talking).
            foreach (var like in GirlInfo.LikesOf(h))
            {
                var bit = 1 << Array.IndexOf(GirlInfo.LikeIds, like);
                if ((gs.KnownLikes & bit) == 0) continue;
                switch (like)
                {
                    case "animals": l.Add(C("c:l_animals", "Hobbies", "Tell her about the stray cat by the gate", 2.2f, () => RunSubject(Find("like_animals")))); break;
                    case "sweets": l.Add(C("c:l_sweets", "Food", "Ask if she's tried the new parfait place", 2.2f, () => RunSubject(Find("like_sweets")))); break;
                    case "food": case "cooking": l.Add(C("c:l_food", "Food", "Ask for her best bento recipe", 2.2f, () => RunSubject(Find("like_food")))); break;
                    case "exercise": l.Add(C("c:l_ex", "Sports", "Challenge her to a run after school", 2.2f, () => RunSubject(Find("like_exercise")))); break;
                    case "study": l.Add(C("c:l_study", "Classes", "Ask her to explain the homework", 2.2f, () => RunSubject(Find("like_study")))); break;
                    case "fashion": l.Add(C("c:l_fashion", "Hobbies", "Ask where she got her hair clip", 2.2f, () => RunSubject(Find("like_fashion")))); break;
                    case "coffee": l.Add(C("c:l_coffee", "Food", "Ask her which canned coffee is best", 2f, () => RunSubject(Find("like_coffee")))); break;
                    case "spicy": l.Add(C("c:l_spicy", "Food", "Dare her to try the extra-hot curry", 2f, () => RunSubject(Find("like_spicy")))); break;
                }
            }

            // What you learned about her comes back (her favorite food, the books she reads...), and questions that teach it.
            foreach (var x in Catalog.Where(x => x.Tier < 0 && x.Id.StartsWith("fact_", StringComparison.Ordinal) && Offerable(x, h)))
            { var subj = x; l.Add(C("c:" + x.Id, x.Topic.ToString(), Fill(x.Label, h), 2.4f, () => RunSubject(subj))); }
            foreach (var x in Catalog.Where(x => x.Tier < 0 && x.Id.StartsWith("ask_", StringComparison.Ordinal) && Offerable(x, h)))
            { var subj = x; l.Add(C("c:" + x.Id, "Ask", Fill(x.Label, h), 1.4f, () => RunSubject(subj))); }

            var m = CampusLife.Memories.Last(h);
            if (m != null && m.Day < S.Day)
                l.Add(C("c:mem" + m.Id, "Memory", MemoryLabel(m), 2.6f, () => BringUp(m)));

            var friend = Social.FriendsOf(h).FirstOrDefault();
            if (friend != null) l.Add(C("c:friend" + GirlInfo.Key(friend), "Friend", "Ask how " + GirlInfo.FirstName(friend) + " is doing", 1.6f, () => AskFriend(friend, null)));
            var partner = Pairs.PartnerOf(h);
            if (partner != null) l.Add(C("c:partner", "Friend", "Ask how things are with " + GirlInfo.FirstName(partner), 2.4f, () => AskFriend(partner, "So... how are things with " + GirlInfo.FirstName(partner) + "? You two seem close.")));
            if (Social.Gossip(h) != null) l.Add(C("c:rumor", "Rumor", "Ask what everyone's whispering about", 1.8f, () => AskSchool()));
            if (!s.Did.Contains("joke"))
                l.Add(C("c:joke", "Joke", Lines.Pick("Tell her about the teacher's tie with ducks", "Tell her the vending machine ate your coins again", "Tell her you saw the principal trip on the stairs") + JokeOdds(s), 1.2f,
                    () => Joke(Lines.Pick("Did you see the teacher's tie today? It had ducks on it. DUCKS.", "The vending machine ate my coins again. I think it hates me personally.", "I saw the principal miss a step this morning. He pretended it was a dance move."))));
            if (!GirlInfo.InNoClub(h)) l.Add(C("c:effort", "Compliment", "Tell her her hard work shows", 1f, () => Compliment("effort", "You've been working really hard lately. It shows.")));

            return Three(l, gs);
        }

        /// <summary>Three, of different kinds, not the ones offered lately.</summary>
        private static List<Concrete> Three(List<Concrete> l, GirlState gs)
        {
            var shown = new List<Concrete>();
            foreach (var c in l.OrderByDescending(c => (gs.LinesUsed.Contains(c.Id) ? 0.25f : 1f) * c.Weight))
            {
                if (shown.Count >= 3) break;
                if (shown.Any(x => x.Kind == c.Kind)) continue;
                shown.Add(c);
            }
            foreach (var c in shown)
            {
                gs.LinesUsed.Add(c.Id);
                if (gs.LinesUsed.Count > 14) gs.LinesUsed.RemoveAt(0);
            }
            LastConcrete = shown.Select(c => c.Label).ToList();
            return shown;
        }

        private static string MemoryLabel(Moment m)
        {
            switch (m.Kind)
            {
                case "walk": return "Ask if she'd like another walk sometime";
                case "quiet": return "Ask if she's feeling better now";
                case "sunday": return "Bring up last Sunday";
                case "favor": return "Ask if the favor worked out";
                case "triangle": return "Bring up what happened with " + (string.IsNullOrEmpty(m.With) ? "her friend" : GirlInfo.FirstName(m.With));
                default: return "Bring up what happened " + CampusLife.Memories.When(m);
            }
        }

        private static void BringUp(Moment m)
        {
            var s = Sess;
            var h = s.Her;
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(Lines.Pick("I was thinking about " + CampusLife.Memories.When(m) + "...", "Hey, about " + CampusLife.Memories.When(m) + "..."));
            var good = m.Tone >= 0;
            b.Her_(good
                ? Lines.T(tone, "When " + m.Text + "? Hehe... I remember. I was really happy.", "Oh yeah! When " + m.Text + "! That was great!", "...When " + m.Text + ". I remember.", "When " + m.Text + "? You remember that? Cute.")
                : Lines.T(tone, "...When " + m.Text + "? Let's not dwell on it.", "Ugh, when " + m.Text + "? Don't remind me!", "...I'd rather not talk about that.", "Oh, THAT. Smooth move bringing it up."));
            CampusLife.Memories.Mentioned(m);
            Play(b, good ? 2 : -1, good ? "♪ She remembers it fondly." : null);
        }

        // ================= asking her a favor =================

        private static bool AskedToday(SaveData.Heroine h, string what)
        {
            int d;
            return S.Buffs.TryGetValue("fav:" + GirlInfo.Key(h) + ":" + what, out d) && d == S.Day;
        }

        private static void MarkAsked(SaveData.Heroine h, string what) { S.Buffs["fav:" + GirlInfo.Key(h) + ":" + what] = S.Day; }

        private static List<KeyValuePair<string, Action>> FavorOptions(Session s)
        {
            var h = s.Her;
            var trust = Social.Trust(h);
            var l = new List<KeyValuePair<string, Action>>();
            if (!AskedToday(h, "study")) l.Add(Opt("Help me study for the next test" + (trust < 40 ? " <color=#9a9a9a>(trust)</color>" : ""), () => AskStudy(h)));
            var friend = Social.FriendsOf(h, 40).FirstOrDefault();
            if (friend != null && !AskedToday(h, "word")) l.Add(Opt("Put in a good word with " + GirlInfo.FirstName(friend), () => AskGoodWord(h, friend)));
            var tip = Social.FriendsOf(h, 30).FirstOrDefault(f => GirlInfo.LikesOf(f).Any(x => (S.Girl(GirlInfo.Key(f)).KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, x))) == 0));
            if (tip != null && !AskedToday(h, "tip")) l.Add(Opt("Ask what " + GirlInfo.FirstName(tip) + " likes", () => AskTip(h, tip)));
            if (!AskedToday(h, "cover") && l.Count < 4) l.Add(Opt("Cover for me with the class rep", () => AskCover(h)));
            return l;
        }

        private static void AskStudy(SaveData.Heroine h)
        {
            MarkAsked(h, "study");
            var tone = GirlInfo.ToneOf(h);
            var ok = Social.Trust(h) >= 40 || Random.value < 0.4f;
            var b = new SceneBuilder(h, true);
            b.Me("Could you help me study a bit? The next test is going to wreck me.");
            b.Her_(ok ? Lines.T(tone, "Of course! Let's go over the hard parts together.", "Study buddies! I'll quiz you!", "...Fine. Pay attention, I won't repeat myself.", "Tutoring costs extra. Kidding. Sit.")
                      : Lines.T(tone, "Ah... I'm not really good at explaining, sorry...", "Ehh, I can barely study myself!", "...Not today.", "Ask me when we're closer."));
            Play(b, ok ? 1 : 0, ok ? "She went through your notes with you. (+2 INT)" : null, ok ? (Action)(() => { G.AddStats(2, 0, 0); Social.AddTrust(h, 1); }) : null);
        }

        private static void AskGoodWord(SaveData.Heroine h, SaveData.Heroine friend)
        {
            MarkAsked(h, "word");
            var tone = GirlInfo.ToneOf(h);
            var fn = GirlInfo.FirstName(friend);
            var ok = Social.Trust(h) >= 35;
            var b = new SceneBuilder(h, true);
            b.Me("Could you put in a good word for me with " + fn + "?");
            b.Her_(ok ? Lines.T(tone, "Hehe... okay. I'll tell her you're nice.", "Leave it to me! I'll hype you up!", "...Fine. I'll say you're tolerable.", "Oh? Interested in " + fn + "? ...Fine. I'll do it.")
                      : Lines.T(tone, "Um... I don't know you well enough for that yet...", "Eh? Why me? Do it yourself!", "...No.", "Earn my good word first."));
            Play(b, ok ? 0 : -1, ok ? fn + " will hear good things about you. (+favor with " + fn + ")" : null, ok ? (Action)(() => { G.AddFavor(friend, 4, 0); Social.AddTrust(friend, 3); }) : null);
        }

        private static void AskTip(SaveData.Heroine h, SaveData.Heroine friend)
        {
            MarkAsked(h, "tip");
            var tone = GirlInfo.ToneOf(h);
            var fn = GirlInfo.FirstName(friend);
            var fs = S.Girl(GirlInfo.Key(friend));
            var like = GirlInfo.LikesOf(friend).FirstOrDefault(x => (fs.KnownLikes & (1 << Array.IndexOf(GirlInfo.LikeIds, x))) == 0);
            var ok = like != null && Social.Trust(h) >= 30;
            var b = new SceneBuilder(h, true);
            b.Me("What does " + fn + " like? Asking for... reasons.");
            b.Her_(ok ? Lines.T(tone, fn + "? She loves " + GirlInfo.LikeLabel(like) + ". Don't tell her I told you!", "Easy! " + GirlInfo.LikeLabel(like) + "! She can't resist!", "..." + GirlInfo.LikeLabel(like) + ". You didn't hear it from me.", "Hm. " + GirlInfo.LikeLabel(like) + ". Use it wisely.")
                      : Lines.T(tone, "Hmm... I'm not sure. Ask her yourself?", "No idea! Ask her!", "...Ask her.", "Do your own research."));
            Play(b, 0, ok ? "Learned: " + fn + " likes " + GirlInfo.LikeLabel(like) + "." : null, ok ? (Action)(() => fs.KnownLikes |= 1 << Array.IndexOf(GirlInfo.LikeIds, like)) : null);
        }

        private static void AskCover(SaveData.Heroine h)
        {
            MarkAsked(h, "cover");
            var tone = GirlInfo.ToneOf(h);
            var ok = Social.Trust(h) >= 50;
            var b = new SceneBuilder(h, true);
            b.Me("If the class rep asks where I was during cleaning... could you cover for me?");
            b.Her_(ok ? Lines.T(tone, "Ehh... just this once, okay?", "Hehe, partners in crime!", "...You owe me.", "Fine. But now you owe me a favor.")
                      : Lines.T(tone, "I-I can't lie to the class rep...", "No way! She's scary!", "...No.", "And what do I get? Nothing? Then no."));
            Play(b, 0, ok ? "She covered for you. (+1 reputation, you owe her one)" : null, ok ? (Action)(() =>
            {
                S.Reputation = Mathf.Clamp(S.Reputation + 1, 0, 100);
                var name = GirlInfo.FirstName(h);
                var r = Board.Make(RequestKind.Gift, h, "Pay " + name + " back", name + " covered for you. Bring her a little thank-you present.", 6);
                r.DeadlineDay = S.Day + 2; r.LearnedFrom = "her"; r.Source = "favor";
                Board.Add(r);
            }) : null);
        }

        // ================= a friend's message =================

        private static Request MessageFor(SaveData.Heroine h)
        {
            var key = GirlInfo.Key(h);
            return Board.Active.FirstOrDefault(r => r.Source == "favor" && r.Kind == RequestKind.Talk && r.Other == key && r.DeadlineDay >= S.Day);
        }

        private static void PassMessage(Request r, SaveData.Heroine h)
        {
            var from = GirlInfo.Find(r.Girl);
            var fn = from != null ? GirlInfo.FirstName(from) : "Your friend";
            var tone = GirlInfo.ToneOf(h);
            var b = new SceneBuilder(h, true);
            b.Me(fn + " asked me to tell you: " + r.Item + ".");
            b.Her_(Lines.T(tone, "Oh! Thank you for telling me... " + fn + " is so thoughtful.", "Ohh, got it! Thanks, messenger!", "...Understood. Thanks.", "A messenger, huh? Thanks, I guess."));
            Play(b, 1, "Message delivered. " + fn + " will be glad.", () =>
            {
                if (from != null)
                {
                    Board.Complete(r, from);
                    CampusLife.Memories.Add(from, "favor", "you passed my message to " + GirlInfo.FirstName(h), 1, true, GirlInfo.Key(h));
                    Arcs.AddBond(GirlInfo.Key(from), GirlInfo.Key(h), 3);
                    Arcs.AddBond(GirlInfo.Key(h), GirlInfo.Key(from), 3);
                }
                else r.State = RequestState.Done;
            });
        }
    }
}
