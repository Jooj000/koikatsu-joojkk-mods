using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Dialogue text. Every line a girl says has four variants by personality tone:
    /// Gentle (sweet, shy, motherly), Energetic (cheerful, gyaru, tomboy), Cool (calm, proud, aloof), Teasing (flirty, tsundere, sadistic).
    /// </summary>
    internal static class Lines
    {
        public static string T(Tone t, string gentle, string energetic, string cool, string teasing)
        {
            switch (t)
            {
                case Tone.Energetic: return energetic;
                case Tone.Cool: return cool;
                case Tone.Teasing: return teasing;
                default: return gentle;
            }
        }

        public static string Pick(params string[] options)
        {
            return options[Random.Range(0, options.Length)];
        }

        // ---------------- moods ----------------

        public static string MoodName(Mood m)
        {
            switch (m)
            {
                case Mood.Cheerful: return "Cheerful";
                case Mood.Hungry: return "Hungry";
                case Mood.Bored: return "Bored";
                case Mood.Stressed: return "Stressed";
                case Mood.Lonely: return "Lonely";
                case Mood.Flirty: return "Flirty";
                case Mood.Sleepy: return "Sleepy";
                case Mood.Upset: return "Upset";
                default: return "Normal";
            }
        }

        public static string MoodTip(Mood m)
        {
            switch (m)
            {
                case Mood.Cheerful: return "Anything goes today. Great day to ask her out.";
                case Mood.Hungry: return "Invite her to lunch or give her food.";
                case Mood.Bored: return "Invite her to exercise or to the club, or just chat.";
                case Mood.Stressed: return "Study together or give her something sweet.";
                case Mood.Lonely: return "Talk to her, walk her home.";
                case Mood.Flirty: return "Flirting, her hand, a kiss and sneaking off go over well.";
                case Mood.Sleepy: return "Coffee helps. Don't drag her into exercise.";
                case Mood.Upset: return "Apologize, give a gift, be gentle.";
                default: return "";
            }
        }

        public static string MoodLine(Tone t, Mood m)
        {
            switch (m)
            {
                case Mood.Cheerful:
                    return T(t, "I'm in a really good mood today. I don't even know why... maybe because you came to talk to me?",
                        "Today's awesome! I feel like I could run ten laps! Or eat ten crepes!",
                        "...I'm fine. Better than fine, actually. Don't make it weird.",
                        "Hm? You can tell I'm in a good mood? Then take advantage of it while it lasts~");
                case Mood.Hungry:
                    return T(t, "Um... sorry, my stomach keeps growling. I skipped breakfast...",
                        "I'm STARVING. Like, I could eat the whole cafeteria. Wanna eat together?",
                        "I forgot my lunch. It's not a big deal. ...It's a little bit of a big deal.",
                        "I'm hungry~ If someone bought me something, I'd be very grateful. Very.");
                case Mood.Bored:
                    return T(t, "Today feels so long... Nothing is happening. Is it okay if I talk with you for a while?",
                        "Booooored! Let's do something! Anything! Please save me!",
                        "Classes today were tedious. Entertain me, if you can.",
                        "I'm bored out of my mind. Got any ideas to make it... interesting?");
                case Mood.Stressed:
                    return T(t, "The tests are coming and I'm so behind... I'm a little scared.",
                        "Ugh, my head's gonna explode! Too much homework!",
                        "I have too much on my plate. It's under control. Mostly.",
                        "Everyone's getting on my nerves today. Except you, maybe. Maybe.");
                case Mood.Lonely:
                    return T(t, "Nobody really talked to me today... I'm glad you came.",
                        "My friends are all busy today. Hang out with me, okay?",
                        "I was alone all morning. Not that I mind. ...Stay a bit.",
                        "Oh, so you DO remember I exist. I was starting to wonder.");
                case Mood.Flirty:
                    return T(t, "I keep thinking about you today... is that strange?",
                        "Hehe, you look kinda cool today. Did you do something different?",
                        "...You're staring. I'm not saying stop.",
                        "I'm feeling a little naughty today. Wanna find out how much?");
                case Mood.Sleepy:
                    return T(t, "Mm... sorry, I stayed up late reading. I can barely keep my eyes open...",
                        "*yawn* I gamed until three. Totally worth it. Totally dying.",
                        "I didn't sleep. Speak slowly.",
                        "So sleepy... wanna be my pillow?");
                case Mood.Upset:
                    return T(t, "I'd... rather not talk about it right now. Sorry.",
                        "Hmph! Today sucks and I don't wanna talk about it!",
                        "Leave me alone for now.",
                        "Don't push your luck today. I'm not in the mood.");
                default:
                    return T(t, "I'm okay, thank you for asking.", "Same old, same old!", "Normal day.", "Why do you ask? Worried about me?");
            }
        }

        /// <summary>Hint about something she likes (reveals a profile like).</summary>
        public static string LikeHint(Tone t, string likeLabel)
        {
            return T(t,
                "By the way... I really like " + likeLabel + ". I don't tell many people that.",
                "Oh! Did I ever tell you I'm crazy about " + likeLabel + "? Like, crazy crazy!",
                "If you must know, I have a weakness for " + likeLabel + ".",
                "Want a hint? I go weak for " + likeLabel + ". Use that information wisely~");
        }

        // ---------------- gifts ----------------

        public static string GiftLiked(Tone t, string item)
        {
            return T(t,
                "Th-this is for me? " + item + "... You remembered what I like. Thank you so much!",
                "No way! " + item + "?! You're the best! Seriously the best!",
                "..." + item + ". You actually paid attention. I'll treasure it.",
                "Oh? " + item + ", huh? You know exactly how to make a girl happy~");
        }

        public static string GiftNeutral(Tone t, string item)
        {
            return T(t,
                "Oh, " + item + "... Thank you. It's very kind of you.",
                "Huh, " + item + "? Cool, thanks!",
                "A gift? ...Thank you. I'll accept it.",
                "Aww, a present? It's not really my thing, but I'll take the thought.");
        }

        public static string GiftIntimateYes(Tone t)
        {
            return T(t,
                "Th-this is... You want me to... for you? ...Okay. Only for you.",
                "Whoa, bold! ...Hehe, fine, I'll wear it. Just for you, got it?",
                "...You're shameless. I'll keep it. Don't expect me to say thanks out loud.",
                "Ooh~ So that's what you've been thinking about. I'll show you sometime.");
        }

        public static string GiftIntimateNo(Tone t)
        {
            return T(t,
                "W-what?! I can't accept something like this! We're not... like that!",
                "Eh?! Dude, what is wrong with you?!",
                "Absolutely not. Know your place.",
                "Wow. Bold move. Wrong girl. Try again in a few months.");
        }

        public static string GiftAlreadyToday(Tone t)
        {
            return T(t,
                "You already gave me something today... I can't accept more, really.",
                "Another one? Save some for tomorrow!",
                "One gift a day is plenty.",
                "Trying to buy me? One a day, cutie.");
        }

        public static string PlayerGivesGift(string item)
        {
            return Pick("Here, I got you something. " + item + ".", "This is for you. " + item + ".", "I saw this and thought of you.");
        }

        // ---------------- requests (talk-based completion) ----------------

        public static string RequestThanks(Tone t)
        {
            return T(t,
                "You really came... Thank you. That means a lot to me.",
                "You made it! Knew I could count on you!",
                "You're on time. Good. I appreciate it.",
                "Took you long enough. ...Thanks, though.");
        }

        public static string ErrandDelivered(Tone t)
        {
            return T(t,
                "You did it for me? Thank you, really! I owe you one.",
                "You're a lifesaver! Seriously!",
                "Done already? Impressive. Thank you.",
                "Look at you, being useful. I like that.");
        }

        public static string MeetAtLine(Tone t, string place)
        {
            return T(t,
                "I like being here with you... the " + place + " feels different when you're around.",
                "See? The " + place + " is way more fun with company!",
                "The " + place + " is quiet at this hour. I like that. And you're... fine, too.",
                "Just the two of us at the " + place + "~ Don't get any funny ideas. Or do.");
        }

        /// <summary>The girl who left the note in your locker explains what she needs.</summary>
        public static string NoteHelp(Tone t)
        {
            return T(t,
                Pick("Y-you came... I have to read my report in front of the class tomorrow and I freeze every time. Could you listen while I practice?",
                     "Th-thanks for coming. I lost the notes for tomorrow's test... could you go over the main points with me?"),
                Pick("You came! Okay, don't laugh: I signed up to give the club speech and I have no idea what to say. Help me write it?",
                     "Yay, you're here! I'm making the poster for the school festival and it looks like a ransom note. Help!"),
                Pick("Good, you're here. I'm stuck on the math homework and I'd rather not ask in class. Ten minutes?",
                     "You came. I need someone to time my run-through for the class presentation. Quietly."),
                Pick("Hey, you actually came. I need a second opinion on the festival poster, and you're the only one who'll be honest.",
                     "Look who showed up. I'm writing the class newsletter and I need a real person to tell me if it's boring."));
        }

        public static string NoteThanks(Tone t)
        {
            return T(t,
                "...That really helped. I think I can do it now. Thank you.",
                "Done! You're the best. I owe you a juice!",
                "That's settled, then. ...Thanks. I mean it.",
                "Okay, that's actually good now. Don't let it go to your head~");
        }

        public static string AskHowAreYou(int closeness)
        {
            if (closeness == 0) return Pick("Hi. How's your day going?", "Hey, how are you today?", "Hello. Having a good day?");
            return Pick("How are you today?", "How's your day going?", "You okay? How are you feeling today?");
        }

        // ---------------- how close she is to you ----------------

        /// <summary>0 = stranger or acquaintance, 1 = friend, 2 = girlfriend (from the game's own relation).</summary>
        public static int Closeness(SaveData.Heroine h)
        {
            if (h == null) return 0;
            var r = h.relation;
            return r >= 2 ? 2 : r == 1 ? 1 : 0;
        }

        /// <summary>Her answer to "How are you today?", as open as she is with you.</summary>
        public static string MoodLine(Tone t, Mood m, int closeness)
        {
            if (closeness >= 2) return MoodLine(t, m);
            if (closeness == 1) return FriendMoodLine(t, m);
            switch (m)
            {
                case Mood.Cheerful:
                    return T(t, "Oh, um... I'm doing well, thank you. It's been a nice day so far.",
                        "Pretty great, actually! Thanks for asking!",
                        "Fine. Good, even.",
                        "Good enough. Why, are you doing a survey?");
                case Mood.Hungry:
                    return T(t, "I'm a little hungry, to be honest... I skipped breakfast.",
                        "Honestly? Starving. Is it lunch yet?",
                        "Hungry. I forgot my lunch.",
                        "Hungry. If you're heading to the cafeteria, that's all I'll say.");
                case Mood.Bored:
                    return T(t, "It's been a slow day... nothing much is happening.",
                        "Kinda bored! Today is dragging on forever.",
                        "Bored. Classes were tedious.",
                        "Bored. You're not going to make it worse, are you?");
                case Mood.Stressed:
                    return T(t, "A little nervous... there's a lot of studying to do.",
                        "Stressed! Way too much homework this week.",
                        "Busy. I have a lot to get through.",
                        "Stressed. Long story, and we don't know each other that well yet.");
                case Mood.Lonely:
                    return T(t, "It's been a quiet day... I haven't really talked to anyone.",
                        "My friends are all busy today, so it's been kinda quiet.",
                        "Quiet. I've been on my own most of the day.",
                        "Quiet day. Not many people to talk to.");
                case Mood.Flirty:
                    return T(t, "I'm in a pretty good mood, actually. I feel like talking today.",
                        "I'm in a super chatty mood today! Fair warning!",
                        "Talkative, for once. Don't get used to it.",
                        "In a playful mood. Lucky you caught me today.");
                case Mood.Sleepy:
                    return T(t, "Sorry... I stayed up late. I'm a bit sleepy.",
                        "*yawn* Sorry! Late night. I'm running on fumes.",
                        "Tired. I didn't sleep much.",
                        "Sleepy. Keep it short, okay?");
                case Mood.Upset:
                    return T(t, "I'm sorry, I'd rather not talk about it.",
                        "Not great, honestly. I'd rather not get into it.",
                        "Not a good day. Let's leave it at that.",
                        "Bad day. Don't take it personally.");
                default:
                    return T(t, "I'm fine, thank you for asking.", "Doing okay! And you?", "Fine.", "Fine. You're the curious type, huh?");
            }
        }

        private static string FriendMoodLine(Tone t, Mood m)
        {
            switch (m)
            {
                case Mood.Cheerful:
                    return T(t, "I'm in a really good mood today! It's nice to see you.",
                        "Today's awesome! I feel like I could run ten laps! Or eat ten crepes!",
                        "I'm good. Really good, actually.",
                        "Good mood today. Enjoy it while it lasts.");
                case Mood.Hungry:
                    return T(t, "I skipped breakfast and my stomach won't let me forget it...",
                        "I'm STARVING. Wanna grab lunch together?",
                        "I forgot my lunch. It's... a bit of a problem.",
                        "Hungry. A good friend would know what to do with that information.");
                case Mood.Bored:
                    return T(t, "Today feels so long... Can we talk for a while?",
                        "Booooored! Let's do something! Anything!",
                        "Classes were dull today. Got anything interesting?",
                        "Bored out of my mind. Entertain me, friend.");
                case Mood.Stressed:
                    return T(t, "The tests are coming and I'm so behind... I'm a little scared.",
                        "Ugh, my head's gonna explode! Too much homework!",
                        "I have too much on my plate. It's under control. Mostly.",
                        "Everyone's getting on my nerves today. You get a pass.");
                case Mood.Lonely:
                    return T(t, "Nobody really talked to me today... I'm glad you came by.",
                        "Everyone's busy today. Hang out with me for a bit?",
                        "I was on my own all morning. ...You can stay a while.",
                        "Oh, a friendly face. Finally.");
                case Mood.Flirty:
                    return T(t, "I'm in such a good mood I can't sit still today.",
                        "I'm super hyped today! Ask me anything!",
                        "I'm in a... talkative mood. Rare, I know.",
                        "I'm feeling mischievous today. Watch out.");
                case Mood.Sleepy:
                    return T(t, "Mm... sorry, I stayed up late reading. I can barely keep my eyes open...",
                        "*yawn* I gamed until three. Totally worth it. Totally dying.",
                        "I didn't sleep. Speak slowly.",
                        "So sleepy... if I nod off, wake me before the teacher sees.");
                case Mood.Upset:
                    return T(t, "I'd... rather not talk about it right now. Sorry.",
                        "Today sucks and I don't wanna talk about it! ...Thanks for asking, though.",
                        "Bad day. Maybe I'll tell you later.",
                        "Don't push it today. I'm not mad at you, though.");
                default:
                    return T(t, "I'm okay, thanks for asking!", "Same old, same old!", "Normal day.", "Same as always. Why, worried about me?");
            }
        }

        /// <summary>She is asked again on the same day.</summary>
        public static string AlreadyAsked(Tone t, int closeness)
        {
            if (closeness == 0)
                return T(t, "Um... I just told you. But thank you.", "You already asked! Still the same!", "You asked already.", "You asked that already. Memory trouble?");
            if (closeness == 1)
                return T(t, "I already told you, silly... but thanks for caring.", "Same as before! You worried about me?", "You asked already.", "Again? You really like my company, huh?");
            return T(t, "I already told you, silly... but thank you for caring.", "Same as before! You worried about me?", "You asked already.", "Again? You really can't stop thinking about me~");
        }

        /// <summary>Hint about something she likes, as open as she is with you.</summary>
        public static string LikeHint(Tone t, string likeLabel, int closeness)
        {
            if (closeness >= 2) return LikeHint(t, likeLabel);
            if (closeness == 1)
                return T(t,
                    "Can I tell you something? I really love " + likeLabel + ".",
                    "Oh! Did I ever tell you I'm crazy about " + likeLabel + "? Like, crazy crazy!",
                    "...I have a weakness for " + likeLabel + ". Keep that between us.",
                    "Want a hint? I'm a sucker for " + likeLabel + ". Use that wisely.");
            return T(t,
                "Oh, um... I like " + likeLabel + ", if you were wondering.",
                "Oh, and I'm really into " + likeLabel + "!",
                "If you must know, I like " + likeLabel + ".",
                "I like " + likeLabel + ". Don't spread it around.");
        }
    }
}
