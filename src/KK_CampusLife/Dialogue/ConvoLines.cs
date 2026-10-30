using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Lines of the conversation system. Every girl line has four variants by tone (Gentle, Energetic, Cool, Teasing);
    /// most situations have several so talks do not repeat. Closeness: 0 acquaintance, 1 friend, 2 girlfriend.
    /// </summary>
    internal static class ConvoLines
    {
        private static string T(Tone t, string g, string e, string c, string x) { return Lines.T(t, g, e, c, x); }
        private static string P(params string[] o) { return Lines.Pick(o); }

        // ================= openings =================

        /// <summary>Jealous because she is into you (the friendship kind is Jealous).</summary>
        /// <summary>Her opening when she heard about something you did.</summary>
        public static string Incident(SaveData.Heroine h, string kind, string about, bool self, bool friend, string what = null, bool saw = false)
        {
            // From incident.dlg: {about} is the girl it was about. A flirt with someone else is what she saw or heard.
            if (kind == Social.Kinds.Flirted && !self) return Seen(h, what, about, saw);
            return Conversation.IncidentSay(h, "incident", kind, about, self, friend, what, saw) ?? Conversation.EverydayTone(GirlInfo.ToneOf(h), "hello");
        }

        /// <summary>
        /// You and another girl, from a girl who saw it herself or only heard about it; what it was sets the words (9th
        /// </summary>
        public static string Seen(SaveData.Heroine h, string what, string about, bool saw)
        {
            return Conversation.IncidentSay(h, "incident.seen", null, about, false, false, what, saw) ?? "...";
        }

        /// <summary>Your girlfriend heard you were flirting with someone else (or saw something more).</summary>
        public static string Cheating(Tone t, string other, bool minds, string what = null, bool saw = false)
        {
            if (minds && saw && (what == Witness.Kiss || what == Witness.H || what == Witness.Touch))
                return T(t, "I saw you... with " + other + ". I saw it. How could you...? || I saw you with " + other + "... how could you...? || You and " + other + "... I saw everything... why? || I saw... you and " + other + "... it hurts...",
                    "I SAW YOU! With " + other + "! Don't you dare say it's nothing! || I SAW YOU with " + other + "! Don't say nothing! || You and " + other + "! I saw! Don't lie!" + " || " + other + "?! I SAW!",
                    "I saw you with " + other + ". Don't insult me with excuses. || You and " + other + ". I saw. No excuses. || I saw. " + other + ". Spare me the excuses." + " || " + other + ". Witnessed. Don't lie.",
                    "I had a lovely view of you and " + other + ". Explain. Slowly. || Lovely view of you and " + other + ". Explain slowly. || You and " + other + ". Explain. Carefully. || I watched you and " + other + ". Talk.");
            if (!minds && saw && what == Witness.Kiss)
                return T(t, "I saw you kiss " + other + "... I trust you. I just... want to hear it from you. || I saw you kiss " + other + "... I trust you. Just tell me. || You kissed " + other + "... please explain it to me. || I saw the kiss with " + other + "... I want to hear it from you.",
                    "I saw you kiss " + other + "! Ugh, fine, as long as I'm still number one! || You kissed " + other + "! Fine, as long as I'm first! || Kissing " + other + "?! Okay! I'm still number one, right?! || Saw you and " + other + "! I'm still top, right?!",
                    "I saw the kiss with " + other + ". I'm not angry. I'm noting it. || The kiss with " + other + ". Not angry. Noting it." + " || " + other + ". Kiss. Recorded. || I saw. " + other + ". Noted.",
                    "Kissing " + other + " where I can see? You do like living dangerously~ || Kissing " + other + " in front of me? Daring~ || Risky, kissing " + other + " where I can see~ || You do love danger, kissing " + other + "~");
            if (!minds)
                return T(t, "People say you were sweet-talking " + other + "... I trust you, okay? || They say you flirted with " + other + "... I trust you. || You and " + other + "...? I believe in you, okay? || I heard about " + other + "... I trust you.",
                    "Heard you were flirting with " + other + "! Hah, as long as you come back to me! || Flirting with " + other + "! Come back to me!" + " || " + other + "?! Ha! You're still mine! || Heard about " + other + "! Just come home to me!",
                    "I hear things about you and " + other + ". I'm not the jealous type. Mostly. || Things about " + other + ". I'm not jealous. Mostly. || About " + other + "... I'm not the jealous type. Usually." + " || " + other + ". Noted. Not jealous. Much.",
                    "You and " + other + "? Cute. Just remember who you belong to~" + " || " + other + "? Remember who owns you~ || Cute, " + other + ". You're mine, though~ || Have fun with " + other + ". You're still mine~");
            return T(t, "Is it true...? You and " + other + "...? I thought we were... || You and " + other + "...? I thought we... || Is it real...? " + other + "...? || I thought... we were together...",
                "Hey! What's this about you and " + other + "?! I'm your girlfriend! || You and " + other + "?! I'm your girlfriend!" + " || " + other + "?! Hello?! Girlfriend here! || What's with " + other + "?! I'm yours!",
                "Explain " + other + ". Now." + " || " + other + ". Explain. || Explain " + other + ". Immediately." + " || " + other + ". Now.",
                "Flirting with " + other + " behind my back? You've got some nerve." + " || " + other + " behind my back? Nerve. || Behind my back with " + other + "? Bold. || Flirting with " + other + "? Brave.");
        }

        public static string WorriedHint(string name)
        {
            return P(name + " seems distracted, like something is on her mind.", name + " keeps glancing away. Something's bothering her.",
                name + " looks like she wants to say something but can't find the words.");
        }

        // ================= let her talk =================

        public static string PlayerListens()
        {
            return P("So, what's new with you?", "Tell me something. Anything.", "I'm all ears.", "What have you been up to?");
        }

        public static string GossipLine(SaveData.Heroine h, string kind, string about, string what = null)
        {
            // What she passes on about you, from incident.dlg; anything else is an everyday story.
            return Conversation.IncidentSay(h, "incident.gossip", kind, about, false, false, what, false) ?? Conversation.EverydayTone(GirlInfo.ToneOf(h), "anecdote");
        }

        // ================= ask about =================

        public static string AskFriend(string friend) { return P("How's " + friend + " doing?", "You're close with " + friend + ", right? How is she?", "Have you talked to " + friend + " lately?"); }

        public static string FriendMood(Tone t, string friend, Mood m)
        {
            switch (m)
            {
                case Mood.Stressed: return T(t, friend + " looks so stressed about tests... maybe she'd like to study with someone." + " || " + friend + " is stressed about tests... she might like a study partner." + " || " + friend + " looks worried about exams... maybe help her study?" + " || " + friend + " is anxious about tests...", friend + "'s freaking out about assignments!" + " || " + friend + "'s panicking about assignments!" + " || " + friend + "'s assignments meltdown!" + " || " + friend + " is drowning in assignments!", friend + " is overwhelmed. Studying might help her." + " || " + friend + " is overwhelmed. Study with her." + " || " + friend + " needs a study partner." + " || " + friend + ". Overloaded. Help her study.", friend + " is a wreck about exams. Go play hero." + " || " + friend + "'s an exam mess. Be a hero." + " || " + friend + " needs rescuing. Exams. || Play tutor for " + friend + ".");
                case Mood.Hungry: return T(t, friend + " forgot her lunch today, I think. || I think " + friend + " forgot her lunch..." + " || " + friend + " didn't bring lunch today..." + " || " + friend + " looks hungry...", friend + " was starving this morning!" + " || " + friend + " was starving!" + " || " + friend + "'s tummy was growling!" + " || " + friend + " needs food!", friend + " skipped breakfast again." + " || " + friend + " skipped breakfast." + " || " + friend + " hasn't eaten." + " || " + friend + ". No breakfast.", friend + " is hungry. Feed her and she's yours. || Feed " + friend + ". She'll be yours." + " || " + friend + "'s hungry. Opportunity. || Snack for " + friend + " = points.");
                case Mood.Lonely: return T(t, friend + " has seemed a little lonely lately... maybe you could talk to her?" + " || " + friend + " seems lonely... maybe talk to her?" + " || " + friend + " looks a little alone... || Could you talk to " + friend + "? She seems lonely.", friend + " looks kinda down! Go cheer her up!" + " || " + friend + " looks down! Cheer her up! || Go cheer " + friend + " up!" + " || " + friend + " needs a smile!", friend + " could use company." + " || " + friend + " needs company." + " || " + friend + " is alone a lot." + " || " + friend + ". Lonely.", friend + " is moping. You could fix that." + " || " + friend + " is moping. Fix it. || Go un-mope " + friend + "." + " || " + friend + " needs you. Go.");
                case Mood.Upset: return T(t, "Something happened to " + friend + "... she's upset. Be gentle with her." + " || " + friend + " is upset... be gentle. || Something happened to " + friend + "... be kind." + " || " + friend + " is sad... careful with her.", friend + " is in a bad mood! Careful!" + " || " + friend + "'s grumpy! Careful!" + " || " + friend + " is mad! Watch out! || Bad mood " + friend + "! Danger!", friend + " is upset about something." + " || " + friend + " is upset." + " || " + friend + ". Upset. || Something's wrong with " + friend + ".", "Don't poke " + friend + " today. Trust me. || Don't poke " + friend + " today. || Avoid " + friend + ". Trust me." + " || " + friend + " bites today.");
                case Mood.Cheerful: return T(t, friend + " is in a great mood today." + " || " + friend + " is so happy today." + " || " + friend + " is in a lovely mood." + " || " + friend + " looks cheerful.", friend + " is bouncing off the walls today!" + " || " + friend + " is SO hyper!" + " || " + friend + "'s bouncing around!" + " || " + friend + " is super happy!", friend + " seems happy." + " || " + friend + " is happy." + " || " + friend + ". Cheerful." + " || " + friend + " seems well.", friend + " is unbearably cheerful today." + " || " + friend + " is annoyingly happy." + " || " + friend + "'s sunshine today. Ugh." + " || " + friend + " won't stop smiling.");
                case Mood.Bored: return T(t, friend + " said she's bored." + " || " + friend + " is bored, she said." + " || " + friend + " said she has nothing to do." + " || " + friend + " seems bored.", friend + " is SO bored, she said so ten times!" + " || " + friend + " said \"bored\" ten times!" + " || " + friend + " is mega bored!" + " || " + friend + " is dying of boredom!", friend + " is bored out of her mind." + " || " + friend + " is very bored." + " || " + friend + ". Bored." + " || " + friend + " has nothing to do.", friend + " is bored. Entertain her. || Entertain " + friend + "." + " || " + friend + " needs amusement. || Go amuse " + friend + ".");
                case Mood.Sleepy: return T(t, friend + " nearly fell asleep in class." + " || " + friend + " almost dozed off in class." + " || " + friend + " looked so sleepy..." + " || " + friend + " was nodding off.", friend + " was snoring in math!" + " || " + friend + " snored in math!" + " || " + friend + " fell asleep in class! || Zzz! " + friend + " in class!", friend + " looks exhausted." + " || " + friend + " is exhausted." + " || " + friend + ". Tired." + " || " + friend + " needs sleep.", friend + " was drooling on her notes. Cute." + " || " + friend + " drooled on her notes. Cute." + " || " + friend + " napped. Adorably. || Sleepy " + friend + ". Precious.");
                case Mood.Flirty: return T(t, friend + " seems to be in a really good mood today... maybe there's someone she likes?" + " || " + friend + " is so cheerful... does she like someone?" + " || " + friend + " keeps smiling... a crush?" + " || " + friend + " seems happy... someone special?", friend + " keeps smiling at her phone!" + " || " + friend + " keeps grinning at her phone!" + " || " + friend + " is texting someone! Smiling!" + " || " + friend + "'s giggling at messages!", friend + " is acting strangely happy." + " || " + friend + " is oddly happy." + " || " + friend + ". Unusually cheerful." + " || " + friend + ". Suspiciously happy.", friend + "'s got that look today. You know the one." + " || " + friend + " has that look. || You know " + friend + "'s face today." + " || " + friend + "'s in love. Maybe.");
                default: return T(t, friend + "'s doing fine, I think." + " || " + friend + " is doing fine, I think." + " || " + friend + " seems okay. || I think " + friend + " is well.", friend + "'s good! Same as always!" + " || " + friend + "'s great!" + " || " + friend + "? Good as always!" + " || " + friend + " is fine!", "She's fine. || Fine. || She's well. || No change.", friend + "? Same as ever." + " || " + friend + "? Same old." + " || " + friend + "? Unchanged." + " || " + friend + ". Boring as usual.");
            }
        }

        public static string FriendBond(Tone t, string friend)
        {
            return T(t, friend + " and I have been friends for a long time. She's really important to me." + " || " + friend + " and I go way back. She's precious to me." + " || " + friend + " is so important to me." + " || " + friend + " and I have been friends forever.", friend + "'s my best friend! We do everything together!" + " || " + friend + "'s my BFF!" + " || " + friend + " and me! Best friends! || Me and " + friend + " do everything!",
                friend + " and I understand each other. That's rare." + " || " + friend + " and I understand each other." + " || " + friend + ". We get each other." + " || " + friend + " and I. A rare bond.", friend + "? She's tolerable. ...She's my best friend, okay?" + " || " + friend + "? Fine. ...Best friend, okay? || She's okay. ...My best friend. Shut up." + " || " + friend + "? Bearable. ...I love her, okay?");
        }

        public static string Rival(Tone t, string friend)
        {
            return T(t, "Um... " + friend + " and I don't really get along..." + " || " + friend + " and I... don't get along... || Um... " + friend + " and I aren't close... || I'm not friends with " + friend + "...", "Ugh, why ask ME about " + friend + "?! || Why ask ME about " + friend + "?!" + " || " + friend + "?! Don't ask me! || Ugh! Not " + friend + "!", "I don't keep track of " + friend + ". || I don't follow " + friend + "." + " || " + friend + ". Not my concern. || I don't track " + friend + ".", "Why are you asking about HER? || Why HER? || Asking about HER? Really? || Her? Why?");
        }

        public static string FriendProblem(Tone t, string friend, string problem)
        {
            return T(t, "Actually... " + friend + " has been worried. " + problem + " Maybe you could help her?" + " || " + friend + " has been worried, actually. " + problem + " Could you help her? || Um... " + friend + " is troubled. " + problem + " Maybe you can help? || I'm worried about " + friend + ". " + problem + " Would you help her?",
                "Oh! " + friend + " has a problem! " + problem + " You should help her!" + " || " + friend + "'s got trouble! " + problem + " Help her! || Hey! " + friend + " needs you! " + problem + " || " + "News! " + problem + " Go help " + friend + "!",
                friend + " won't ask anyone, but: " + problem + " Just so you know." + " || " + friend + " won't say it: " + problem + " Now you know. || For your information: " + problem + " " + friend + " won't ask." + " || " + problem + " " + friend + " keeps it to herself.",
                "Want a tip? " + problem + " " + friend + " would never admit it, though. || Tip: " + problem + " " + friend + " would deny it. || Insider info: " + problem + " Don't tell " + friend + " I said. || Psst. " + problem + " " + friend + " won't admit it.");
        }

        public static string AskSchool() { return P("Anything going on around campus?", "What's everyone talking about lately?", "Heard any news?"); }

        // ================= joke / tease =================

        public static string PlayerJoke()
        {
            return P("Why did the student eat his assignments? The professor said it was a piece of cake.",
                "I tried to catch fog this morning. I mist.",
                "Our math professor is so strict even the numbers stand in line.",
                "I'd tell you a chemistry joke, but I know I wouldn't get a reaction.",
                "What do you call a sleeping bull? A bulldozer.",
                "I'm reading a book about anti-gravity. I can't put it down.");
        }

        // ================= compliments =================

        // ================= romance =================

        // ================= clearing the air =================

        public static string PlayerExplain(string kind)
        {
            switch (kind)
            {
                case Social.Kinds.SeenGirlsRoom:
                case Social.Kinds.SeenLockers: return P("I got lost, I swear! The signs on that floor are confusing.", "It was a mistake. I turned around the second I noticed.");
                case Social.Kinds.Flirted: return P("She's just a friend. You're the one I care about.", "It's not what you think. I only have eyes for you.");
                case Social.Kinds.BrokePromise: return P("Something came up and I couldn't make it. I should have told you.", "I didn't forget, I just ran out of time. I'm sorry.");
                default: return P("That's not what happened. Let me explain.", "I think there's been a misunderstanding.");
            }
        }

        public static string PlayerApologize() { return P("I'm sorry. Really.", "I messed up. I'm sorry.", "You're right. I'm sorry about that."); }

        public static string PlayerMakePlans(string friend) { return P("You're right. Let's hang out more, just us.", "Sorry. I'd like to spend more time with you too."); }
    }
}
