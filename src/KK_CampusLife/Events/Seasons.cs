using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Relationship Seasons: becoming her boyfriend is not the finish line. Each girlfriend goes through chapters, one at
    /// a time and days apart: "Our First Tradition" (something that is only yours), "Something Feels Different" (a talk
    /// grown from your own history: a sore memory, a broken promise, too little attention, or just a quiet worry; drama is
    /// optional) and "Looking Ahead" (next term, the club, what you want to do again). A chapter that goes badly comes
    /// back later. Traditions you build are remembered and come back in Sunday stories.
    /// </summary>
    internal static class Seasons
    {
        private static CampusState S { get { return Campus.S; } }
        public static readonly string[] ChapterNames = { "Our First Tradition", "Something Feels Different", "Looking Ahead" };

        public static string ChapterLabel(SaveData.Heroine h)
        {
            if (h == null || !h.isGirlfriend) return "";
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Chapter >= ChapterNames.Length) return "Learning to Be Us: complete";
            return "Learning to Be Us, chapter " + (gs.Chapter + 1) + ": " + ChapterNames[gs.Chapter] + (string.IsNullOrEmpty(gs.ChapterOpen) ? "" : " (unresolved)");
        }

        public static void AddTradition(SaveData.Heroine h, string text)
        {
            if (h == null || string.IsNullOrEmpty(text)) return;
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Traditions.Contains(text)) return;
            gs.Traditions.Add(text);
            if (gs.Traditions.Count > 6) gs.Traditions.RemoveAt(0);
            Toasts.Show("New tradition with " + GirlInfo.FirstName(h) + ": " + text + ".");
            Anthology.Add("tradition", "You and " + GirlInfo.FirstName(h) + " started a tradition: " + text, 1, GirlInfo.Key(h));
        }

        public static string Tradition(SaveData.Heroine h)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            return gs.Traditions.Count == 0 ? null : gs.Traditions[Random.Range(0, gs.Traditions.Count)];
        }

        private static string T(SaveData.Heroine h, string g, string e, string c, string x) { return Lines.T(GirlInfo.ToneOf(h), g, e, c, x); }

        /// <summary>The traditions her likes suggest: [label, tradition text].</summary>
        private static List<string[]> TraditionIdeas(SaveData.Heroine h)
        {
            var l = new List<string[]>();
            foreach (var like in GirlInfo.LikesOf(h))
                switch (like)
                {
                    case "food": case "sweets": l.Add(new[] { "A new sweet from the store every Friday", "a new sweet every Friday" }); break;
                    case "cooking": l.Add(new[] { "She makes lunch on Mondays, you rate it", "her Monday lunches" }); break;
                    case "study": l.Add(new[] { "Study together in the library on Wednesdays", "Wednesday study dates" }); break;
                    case "exercise": l.Add(new[] { "A run around the track before class", "morning runs" }); break;
                    case "animals": l.Add(new[] { "Visit the cat behind the gym", "visiting the gym cat" }); break;
                    case "fashion": l.Add(new[] { "Pick each other's accessory once a week", "picking each other's accessory" }); break;
                    case "coffee": l.Add(new[] { "Canned coffee on the rooftop after club", "rooftop coffee" }); break;
                    case "spicy": l.Add(new[] { "The spiciest thing on the cafeteria menu, once a week", "the spicy challenge" }); break;
                }
            if (l.Count < 2) l.Add(new[] { "Walk home together the long way", "the long way home" });
            if (l.Count < 3) l.Add(new[] { "Watch the sunset from the rooftop", "rooftop sunsets" });
            return l.GroupBy(x => x[1]).Select(g => g.First()).Take(3).ToList();
        }

        // ================= chapters as Scene Talk subjects =================

        public static IEnumerable<Subject> Subjects(SaveData.Heroine h)
        {
            if (!Cfg.RelationshipSeasons.Value || h == null || !h.isGirlfriend) yield break;
            var gs = S.Girl(GirlInfo.Key(h));
            if (gs.Chapter >= ChapterNames.Length) yield break;
            var gap = gs.ChapterDay < 0 ? 99 : S.Day - gs.ChapterDay;
            if (!string.IsNullOrEmpty(gs.ChapterOpen))
            {
                if (gap >= 2) yield return Chapter2(h, gs, true);
                yield break;
            }
            if (gs.Chapter == 0 && gap >= 1 && Social.Trust(h) >= 30) yield return Chapter1(h, gs);
            else if (gs.Chapter == 1 && gap >= 4) yield return Chapter2(h, gs, false);
            else if (gs.Chapter == 2 && gap >= 4 && Social.Trust(h) >= 45) yield return Chapter3(h, gs);
        }

        private static Subject Chapter1(SaveData.Heroine h, GirlState gs)
        {
            var ideas = TraditionIdeas(h);
            return new Subject
            {
                Id = "season1", Weight = 2.2f,
                Write = b =>
                {
                    b.Narr("Chapter 1: Our First Tradition.");
                    b.Face(h, Feel.Shy).Her_(T(h, "Um... I was thinking. Couples have things that are only theirs, right? Little rituals... I want us to have one.", "Okay, so! We need a THING. A couple thing! Something only we do!",
                        "I've been thinking. We should have a tradition. Something that's ours.", "Every couple has some cheesy little ritual. We need one. Ours will be the best."));
                    var opts = new List<string>();
                    for (var i = 0; i < ideas.Count; i++) { opts.Add(ideas[i][0]); opts.Add("s1_" + i); }
                    opts.Add("Let her pick"); opts.Add("s1_her");
                    b.Choice(opts.ToArray());
                    for (var i = 0; i < ideas.Count; i++) { b.Branch("s1_" + i); b.Face(h, Feel.Love).Her_(T(h, "...Yes. That's perfect. It's ours now.", "YES! Perfect! It's official!", "...Good choice. It's ours.", "Ooh, I like it. Ours. No take-backs.")); b.End(); }
                    b.Branch("s1_her"); b.Face(h, Feel.Smile).Her_(T(h, "Then... " + ideas[0][1] + "? I've wanted that for a while.", "Then " + ideas[0][1] + "! Obviously!", "Then: " + ideas[0][1] + ".", "Then " + ideas[0][1] + ". Don't you dare forget.")); b.End();
                },
                Result = c =>
                {
                    var i = c == "s1_her" ? 0 : c.StartsWith("s1_", StringComparison.Ordinal) ? Mathf.Clamp(int.Parse(c.Substring(3)), 0, ideas.Count - 1) : 0;
                    AddTradition(h, ideas[i][1]);
                    Campus.Apply(new Rewards { Favor = 4, Devotion = 4 }, h);
                    Social.AddTrust(h, c == "s1_her" ? 6 : 4);
                    Memories.Add(h, "tradition", "we started " + ideas[i][1], 1, false);
                    Done(h, gs, "");
                },
                Interest = 2,
            };
        }

        /// <summary>What in your history makes something feel different (none: a gentle version).</summary>
        private static string Trouble(SaveData.Heroine h, GirlState gs, out string kind)
        {
            var sore = gs.Moments.LastOrDefault(m => m.Tone < 0 && S.Day - m.Day <= 14);
            if (sore != null) { kind = "sore"; return sore.Text; }
            var g = Social.OwnGrudge(h);
            if (g != null && g.Kind == Social.Kinds.BrokePromise) { kind = "promise"; return "you didn't keep a promise"; }
            if (gs.Attention < 40) { kind = "attention"; return "we've barely seen each other"; }
            var rival = Social.JealousOf(h);
            if (rival != null) { kind = "jealous"; return "you've been spending a lot of time with " + GirlInfo.FirstName(rival); }
            kind = "quiet";
            return "things have been so calm that I started wondering if you're bored";
        }

        private static Subject Chapter2(SaveData.Heroine h, GirlState gs, bool again)
        {
            string kind;
            var trouble = Trouble(h, gs, out kind);
            return new Subject
            {
                Id = "season2", Weight = again ? 3f : 2f,
                Write = b =>
                {
                    b.Narr(again ? "She brings it up again, the thing you never really finished talking about." : "Chapter 2: Something Feels Different.");
                    b.Face(h, Feel.Worried).Her_(T(h, "Can I be honest? Lately it feels like... " + trouble + ". Maybe it's silly.", "Okay, I gotta say it: " + trouble + ". And it's bugging me!",
                        "I'll be direct. " + char.ToUpper(trouble[0]) + trouble.Substring(1) + ". I've noticed.", "So, uh. " + char.ToUpper(trouble[0]) + trouble.Substring(1) + ". Just saying."));
                    var opts = new List<string> { "Just listen", "s2_listen" };
                    opts.Add(G.Int >= 35 ? "Explain your side calmly [INT]" : "Explain your side"); opts.Add("s2_explain");
                    opts.Add("Propose a deal"); opts.Add("s2_deal");
                    opts.Add("Ask for a little time first"); opts.Add("s2_space");
                    b.Choice(opts.ToArray());
                    b.Branch("s2_listen"); b.Narr("You don't try to fix it. You just let her say all of it."); b.Face(h, Feel.Smile).Her_(T(h, "...Thank you. I feel lighter.", "...Huh. Just saying it out loud helped!", "...That was what I needed.", "...Okay. You're good at this.")); b.End();
                    b.Branch("s2_explain");
                    if (G.Int >= 35 || Social.Trust(h) >= 55) b.Face(h, Feel.Normal).Her_(T(h, "...I see. I didn't know that. I'm glad you told me.", "...Oh! Okay, that makes sense now!", "...Understood. That changes things.", "...Fine. That's a good reason. Annoyingly."));
                    else b.Face(h, Feel.Pout).Her_(T(h, "...That sounds like an excuse.", "That's not the point!", "...That doesn't answer it.", "...Wow. Okay."));
                    b.End();
                    b.Branch("s2_deal"); b.Face(h, Feel.Smile).Her_(T(h, "A promise, then. We'll both try harder.", "Deal! Pinky promise!", "Agreed. Terms accepted.", "Deal. And I'm holding you to it.")); b.End();
                    b.Branch("s2_space"); b.Face(h, Feel.Sad).Her_(T(h, "...Okay. But we'll talk about it, right?", "...Fine. But don't forget!", "...Fine. Later.", "...Don't take too long.")); b.End();
                },
                Result = c =>
                {
                    var good = c == "s2_listen" || c == "s2_deal" || c == "s2_explain" && (G.Int >= 35 || Social.Trust(h) >= 55);
                    if (good)
                    {
                        Campus.Apply(new Rewards { Favor = 3, Devotion = again ? 6 : 4 }, h);
                        Social.AddTrust(h, again ? 10 : 7);
                        gs.Attention = Mathf.Min(100, gs.Attention + 15);
                        if (kind == "sore") foreach (var m in gs.Moments.Where(m => m.Tone < 0)) m.Tone = 0;
                        if (c == "s2_deal") AddTradition(h, "a promise to talk things out");
                        Memories.Add(h, "season", "we talked through something hard", 1, false);
                        Done(h, gs, "");
                    }
                    else
                    {
                        Social.AddTrust(h, c == "s2_space" ? 0 : -3);
                        gs.ChapterOpen = kind;
                        gs.ChapterDay = S.Day;
                    }
                },
                Interest = 0,
            };
        }

        private static Subject Chapter3(SaveData.Heroine h, GirlState gs)
        {
            var trad = Tradition(h);
            return new Subject
            {
                Id = "season3", Weight = 2f,
                Write = b =>
                {
                    b.Narr("Chapter 3: Looking Ahead.");
                    b.Face(h, Feel.Smile).Her_(T(h, "Next term... will we still be like this? I hope so. I've been thinking about what we could do.", "Hey! Next term! We should plan stuff! Big stuff!",
                        "I've been thinking about next term. About us.", "So. Next term. What's the plan, boyfriend?"));
                    var opts = new List<string> { "Talk about next term", "s3_term" };
                    if (!GirlInfo.InNoClub(h)) { opts.Add("Plan something for her club"); opts.Add("s3_club"); }
                    if (trad != null) { opts.Add("Promise to keep up " + trad); opts.Add("s3_trad"); }
                    opts.Add("Ask about her dreams"); opts.Add("s3_dream");
                    b.Choice(opts.ToArray());
                    b.Branch("s3_term"); b.Face(h, Feel.Love).Her_(T(h, "...Together, then. That's my plan.", "Together! That's the whole plan!", "...Together. Agreed.", "Together. Obviously.")); b.End();
                    b.Branch("s3_club"); b.Face(h, Feel.Laugh).Her_(T(h, "You'd help with the club? Then let's make it the best one!", "YES! Club domination!", "...With your help, it could work.", "Club plans with you? Dangerous. I like it.")); b.End();
                    b.Branch("s3_trad"); b.Face(h, Feel.Love).Her_(T(h, "...You remembered. Of course you did.", "You remembered! Ahh, I love that!", "...You remembered.", "...Okay, that's unfairly sweet.")); b.End();
                    b.Branch("s3_dream"); b.Face(h, Feel.Shy).Her_(T(h, "My dream...? I want to " + Dream(h) + ". Don't laugh.", "My dream? To " + Dream(h) + "! Big, right?!", "I intend to " + Dream(h) + ".", "My dream? To " + Dream(h) + ". Impressed?")); b.End();
                },
                Result = c =>
                {
                    Campus.Apply(new Rewards { Favor = 4, Devotion = 6 }, h);
                    Social.AddTrust(h, 5);
                    if (c == "s3_club") G.AddClubPoints(3);
                    if (c == "s3_dream") gs.Epilogue = Dream(h);
                    Memories.Add(h, "season", c == "s3_dream" ? "I told you my dream" : "we made plans for next term", 1, false);
                    Done(h, gs, "");
                    Anthology.Add("season", "You and " + GirlInfo.FirstName(h) + " finished \"Learning to Be Us\"", 3, GirlInfo.Key(h));
                },
                Interest = 2,
            };
        }

        public static string Dream(SaveData.Heroine h)
        {
            var likes = GirlInfo.LikesOf(h);
            if (likes.Contains("cooking")) return "open a little restaurant";
            if (likes.Contains("study")) return "get into a top university";
            if (likes.Contains("exercise")) return "compete at the national level";
            if (likes.Contains("fashion")) return "design my own clothes";
            if (likes.Contains("animals")) return "become a vet";
            if (likes.Contains("sweets") || likes.Contains("food")) return "run a sweets shop";
            if (likes.Contains("coffee")) return "run a quiet café";
            switch (GirlInfo.ToneOf(h))
            {
                case Tone.Energetic: return "travel all over the world";
                case Tone.Cool: return "become a researcher";
                case Tone.Teasing: return "become famous";
                default: return "become a teacher";
            }
        }

        private static void Done(SaveData.Heroine h, GirlState gs, string open)
        {
            gs.Chapter++;
            gs.ChapterDay = S.Day;
            gs.ChapterOpen = open;
            CampusLifePlugin.Log.LogInfo("Season: " + GirlInfo.FirstName(h) + " finished chapter " + gs.Chapter);
        }

        public static string Force(SaveData.Heroine h, int chapter)
        {
            var gs = S.Girl(GirlInfo.Key(h));
            gs.Chapter = Mathf.Clamp(chapter, 0, 2);
            gs.ChapterDay = -1;
            gs.ChapterOpen = "";
            gs.Pending = "SceneTalk:season" + (gs.Chapter + 1);
            return GirlInfo.FirstName(h) + ": " + ChapterLabel(h) + (h.isGirlfriend ? "" : " (not a girlfriend: chapters only for girlfriends)");
        }
    }
}
