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
                case Tone.Energetic: return Alt(energetic);
                case Tone.Cool: return Alt(cool);
                case Tone.Teasing: return Alt(teasing);
                default: return Alt(gentle);
            }
        }

        /// <summary>
        /// "first || second || third": one of them. A line
        /// without " || " comes back as it is.
        /// </summary>
        public static string Alt(string s)
        {
            if (s == null || s.IndexOf(" || ", System.StringComparison.Ordinal) < 0) return s;
            var parts = s.Split(new[] { " || " }, System.StringSplitOptions.None);
            return parts[Random.Range(0, parts.Length)];
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
                    return T(t, "I'm in a really good mood today. I don't even know why... maybe because you came to talk to me? || Everything feels a little brighter today. And now you're here, so even more. || I've been smiling all day. My friends think I'm weird.",
                        "Today's awesome! I feel like I could run ten laps! Or eat ten crepes! || Best day ever! I aced a quiz and the cafeteria had melon bread! || I'm so pumped today! Let's do something fun!",
                        "...I'm fine. Better than fine, actually. Don't make it weird. || Good. Unusually good. I'm suspicious of it. || I'm in a good mood. Don't ruin it. ...You won't.",
                        "Hm? You can tell I'm in a good mood? Then take advantage of it while it lasts~ || Great mood today. Maybe I'll even be nice to you~ || I'm in such a good mood I might spoil you. Might.");
                case Mood.Hungry:
                    return T(t, "Um... sorry, my stomach keeps growling. I skipped breakfast... || I'm so hungry... could we maybe eat together later? || Sorry, I can't focus. I forgot my bento at home...",
                        "I'm STARVING. Like, I could eat the whole cafeteria. Wanna eat together? || Food. I need food. Then hugs. Food first. || My stomach's been yelling since second period! Feed me!",
                        "I forgot my lunch. It's not a big deal. ...It's a little bit of a big deal. || Hungry. Very. I don't want to talk about it. || I skipped lunch for the student council. Regretting it.",
                        "I'm hungry~ If someone bought me something, I'd be very grateful. Very. || Starving~ A good boyfriend would know what to do. || Feed me and I'll be extra sweet to you today~");
                case Mood.Bored:
                    return T(t, "Today feels so long... Nothing is happening. Is it okay if I talk with you for a while? || It's such a slow day... I'm glad you came to save me. || Nothing's happening today. Stay with me a little?",
                        "Booooored! Let's do something! Anything! Please save me! || I'm gonna die of boredom! Take me somewhere! || So bored I counted the ceiling tiles. Two hundred twelve!",
                        "Classes today were tedious. Entertain me, if you can. || Today is dull. You're the most interesting thing so far. || Boring day. I'm open to suggestions.",
                        "I'm bored out of my mind. Got any ideas to make it... interesting? || So bored~ Distract me. Any way you like. || I need something exciting. Are you exciting?");
                case Mood.Stressed:
                    return T(t, "The tests are coming and I'm so behind... I'm a little scared. || There's so much to do... I don't know where to start. || I've been worrying all day. Can I lean on you a little?",
                        "Ugh, my head's gonna explode! Too much homework! || Three reports due this week! Who does that?! || My brain is full! No more room! Help!",
                        "I have too much on my plate. It's under control. Mostly. || Busy. Deadlines. I'll survive. || Stressed. I'll handle it. ...A coffee wouldn't hurt.",
                        "Everyone's getting on my nerves today. Except you, maybe. Maybe. || Today is testing me. Be extra nice, okay? || I'm wound up tight today. Help me relax?");
                case Mood.Lonely:
                    return T(t, "Nobody really talked to me today... I'm glad you came. || I felt a little alone today. You came at the right time. || I was hoping you'd find me. I missed you.",
                        "My friends are all busy today. Hang out with me, okay? || Everyone ditched me today! You won't, right? || So lonely! Good thing my boyfriend showed up!",
                        "I was alone all morning. Not that I mind. ...Stay a bit. || Quiet day. Too quiet. You can stay. || I didn't talk to anyone today. You're the first.",
                        "Oh, so you DO remember I exist. I was starting to wonder. || Where were you all day? I was starting to feel abandoned. || Finally. I was about to file a missing person report.");
                case Mood.Flirty:
                    return T(t, "I keep thinking about you today... is that strange? || I've been hoping to see you all day... || My heart jumped when I saw you. It still does that.",
                        "Hehe, you look kinda cool today. Did you do something different? || You look really good today. Just saying! || Hehe, I'm in a lovey mood today. Watch out!",
                        "...You're staring. I'm not saying stop. || I've been thinking about you. A lot. That's all. || Come closer. ...Closer.",
                        "I'm feeling a little naughty today. Wanna find out how much? || You look delicious today. Is that a problem? || Mm~ I've got ideas today. Want to hear them?");
                case Mood.Sleepy:
                    return T(t, "Mm... sorry, I stayed up late reading. I can barely keep my eyes open... || I'm so sleepy... can I lean on you a little? || Sorry, I barely slept. I kept thinking about stuff...",
                        "*yawn* I gamed until three. Totally worth it. Totally dying. || *yawn* Movie marathon last night. Big mistake! || Sleepy! I could nap standing up!",
                        "I didn't sleep. Speak slowly. || Tired. Don't expect wit today. || Three hours of sleep. Be gentle with me.",
                        "So sleepy... wanna be my pillow? || Sleepy~ Carry me? || *yawn* Your shoulder looks comfy.");
                case Mood.Upset:
                    return T(t, "I'd... rather not talk about it right now. Sorry. || It's been a hard day. Can we just... be quiet together? || Something happened. I'll tell you later, okay?",
                        "Hmph! Today sucks and I don't wanna talk about it! || Worst. Day. Ever. Don't ask! || Ugh! Everything's going wrong today!",
                        "Leave me alone for now. || Not today. Sorry. || I'm in a bad place. Give me time.",
                        "Don't push your luck today. I'm not in the mood. || Bad day. Tread carefully. || Today I bite. Be warned.");
                default:
                    return T(t, "I'm okay, thank you for asking. || I'm fine. Better now that you asked. || Normal day, nothing special. You?", "Same old, same old! || All good! Nothing to report! || Pretty normal! You?", "Normal day. || Nothing worth mentioning. || Ordinary.", "Why do you ask? Worried about me? || Fine. Unless you plan to make it better~ || Same as always. You can change that, though.");
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
                "Th-this is... You want me to... for you? ...Okay. Only for you. || I-is this... for me to wear? ...I'll think about it. Thank you. || Oh... oh my. Only you get to see this, okay?",
                "Whoa, bold! ...Hehe, fine, I'll wear it. Just for you, got it? || Wow, you're bold! I kinda like it, though! || Seriously? Hehe, okay, you win.",
                "...You're shameless. I'll keep it. Don't expect me to say thanks out loud. || ...Interesting choice. I'll accept it. || Hm. Bold. I'll keep it.",
                "Ooh~ So that's what you've been thinking about. I'll show you sometime. || Oh my~ Want to see it on me sometime? || Naughty boy~ I'll put it to good use.");
        }

        public static string GiftIntimateNo(Tone t)
        {
            return T(t,
                "W-what?! I can't accept something like this! We're not... like that! || N-no! That's too... we're not that close! || I-I can't take this! It's embarrassing!",
                "Eh?! Dude, what is wrong with you?! || Gross! No way! || Are you serious?! No!",
                "Absolutely not. Know your place. || No. Inappropriate. || Take it back. Now.",
                "Wow. Bold move. Wrong girl. Try again in a few months. || Cute. No. Not yet. || Oh, you wish. Try harder.");
        }

        public static string GiftAlreadyToday(Tone t)
        {
            return T(t,
                "You already gave me something today... I can't accept more, really. || You're too generous... one is enough for today. || Thank you, but please, save it for another day.",
                "Another one? Save some for tomorrow! || Whoa, two in one day? Slow down! || Hey, I already got one today!",
                "One gift a day is plenty. || Already got one today. || Enough for today.",
                "Trying to buy me? One a day, cutie. || Spoiling me? Tomorrow, cutie. || Patience. One per day~");
        }

        public static string PlayerGivesGift(string item)
        {
            return Pick("Here, I got you something. " + item + ".", "This is for you. " + item + ".", "I saw this and thought of you.");
        }

        // ---------------- requests (talk-based completion) ----------------

        public static string RequestThanks(Tone t)
        {
            return T(t,
                "You really came... Thank you. That means a lot to me. || You came! I'm so relieved. || Thank you for coming. Really.",
                "You made it! Knew I could count on you! || You're here! Awesome! || Yes! You came through!",
                "You're on time. Good. I appreciate it. || You came. Thank you. || Punctual. I like that.",
                "Took you long enough. ...Thanks, though. || Oh, you came. Good boy~ || Finally. I was starting to think you forgot.");
        }

        public static string ErrandDelivered(Tone t)
        {
            return T(t,
                "You did it for me? Thank you, really! I owe you one. || You really did it... thank you so much. || You're so kind. I won't forget this.",
                "You're a lifesaver! Seriously! || You're the best! Thank you! || Hero! My hero!",
                "Done already? Impressive. Thank you. || Efficient. Thank you. || You did it. I'm grateful.",
                "Look at you, being useful. I like that. || Good job~ I might reward you. || My, how helpful. I'll remember this.");
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
                "...That really helped. I think I can do it now. Thank you. || Thank you... I feel so much better about it now. || You helped a lot. I'm not scared anymore.",
                "Done! You're the best. I owe you a juice! || We did it! High five! || Finished! I'm buying you something!",
                "That's settled, then. ...Thanks. I mean it. || Done. Thank you for your time. || That helped. Thank you.",
                "Okay, that's actually good now. Don't let it go to your head~ || Not bad, partner~ || Okay, you're useful. I'll admit it.");
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
                    return T(t, "Oh, um... I'm doing well, thank you. It's been a nice day so far. || I-I'm good! Today's been nice. || Pretty good, thank you. The weather helps.",
                        "Pretty great, actually! Thanks for asking! || Super good! Today's been fun! || Awesome! Something good happened in class!",
                        "Fine. Good, even. || Good, thank you. || Better than usual.",
                        "Good enough. Why, are you doing a survey? || Good. Why, do you want to make it better? || Pretty good. And you're curious why?");
                case Mood.Hungry:
                    return T(t, "I'm a little hungry, to be honest... I skipped breakfast. || Um, a little hungry... sorry. || Hungry... I didn't eat this morning.",
                        "Honestly? Starving. Is it lunch yet? || Hungry! Way too hungry! || Starving! When's lunch?!",
                        "Hungry. I forgot my lunch. || Hungry, if you must know. || I missed lunch. That's all.",
                        "Hungry. If you're heading to the cafeteria, that's all I'll say. || Hungry. Hint, hint. || Hungry. You wouldn't happen to have a snack?");
                case Mood.Bored:
                    return T(t, "It's been a slow day... nothing much is happening. || It's a little boring today... || Quiet day. Not much going on.",
                        "Kinda bored! Today is dragging on forever. || Bored! Super bored! || Today's so slow! Ugh!",
                        "Bored. Classes were tedious. || Dull day. || Uneventful. Tedious.",
                        "Bored. You're not going to make it worse, are you? || Bored. Got anything interesting? || Bored. Surprise me.");
                case Mood.Stressed:
                    return T(t, "A little nervous... there's a lot of studying to do. || A bit stressed... lots of homework. || Busy... I have a test soon.",
                        "Stressed! Way too much homework this week. || Stressed! So many deadlines! || Busy busy busy! Too much stuff!",
                        "Busy. I have a lot to get through. || Stressed. It's manageable. || Busy. Lots on my plate.",
                        "Stressed. Long story, and we don't know each other that well yet. || Stressed. Don't ask, stranger. || On edge. Not a great time to impress me.");
                case Mood.Lonely:
                    return T(t, "It's been a quiet day... I haven't really talked to anyone. || A little lonely, honestly... || Quiet... nobody's talked to me much today.",
                        "My friends are all busy today, so it's been kinda quiet. || Kinda lonely today! Everyone's busy! || My friends abandoned me today!",
                        "Quiet. I've been on my own most of the day. || Alone most of the day. It's fine. || Quiet. Too quiet, maybe.",
                        "Quiet day. Not many people to talk to. || Lonely day. Good timing. || Nobody to talk to. Until now.");
                case Mood.Flirty:
                    return T(t, "I'm in a pretty good mood, actually. I feel like talking today. || Good! I feel chatty today. || Nice day... I'm in a good mood.",
                        "I'm in a super chatty mood today! Fair warning! || Great mood! Talk to me! || I'm on fire today! Ask me anything!",
                        "Talkative, for once. Don't get used to it. || Unusually good. || Good. In the mood to talk.",
                        "In a playful mood. Lucky you caught me today. || Playful today. Watch yourself. || Good mood. You picked the right time.");
                case Mood.Sleepy:
                    return T(t, "Sorry... I stayed up late. I'm a bit sleepy. || A little tired... sorry. || Sleepy... I studied late.",
                        "*yawn* Sorry! Late night. I'm running on fumes. || Sleepy! So sleepy! || *yawn* I need a nap!",
                        "Tired. I didn't sleep much. || Tired. || Short on sleep.",
                        "Sleepy. Keep it short, okay? || Sleepy. Talk slow. || Tired. Don't bore me or I'll doze off.");
                case Mood.Upset:
                    return T(t, "I'm sorry, I'd rather not talk about it. || Not a great day... sorry. || I'm a little down today.",
                        "Not great, honestly. I'd rather not get into it. || Bad day! Don't ask! || Ugh, not great.",
                        "Not a good day. Let's leave it at that. || Bad day. || Not good.",
                        "Bad day. Don't take it personally. || Bad day. Don't push it. || Rough day. Nothing to do with you.");
                default:
                    return T(t, "I'm fine, thank you for asking. || I'm okay, thanks. || Fine, thank you. And you?", "Doing okay! And you? || Good! You? || Not bad! How about you?", "Fine. || Fine, thanks. || Alright.", "Fine. You're the curious type, huh? || Fine. Why do you ask? || Fine. Is this small talk?");
            }
        }

        private static string FriendMoodLine(Tone t, Mood m)
        {
            switch (m)
            {
                case Mood.Cheerful:
                    return T(t, "I'm in a really good mood today! It's nice to see you. || Today's a good day! And seeing you makes it better. || I'm so happy today! Did something good happen to you too?",
                        "Today's awesome! I feel like I could run ten laps! Or eat ten crepes! || Best day ever! I aced a quiz and the cafeteria had melon bread! || I'm so pumped today! Let's do something fun!",
                        "I'm good. Really good, actually. || Good. I'll take it. || Good day. Rare, but good.",
                        "Good mood today. Enjoy it while it lasts. || Good mood. Don't waste it. || Great day, friend. Let's keep it that way.");
                case Mood.Hungry:
                    return T(t, "I skipped breakfast and my stomach won't let me forget it... || I'm so hungry... want to grab something together? || Sorry if my stomach growls. I'm starving...",
                        "I'm STARVING. Wanna grab lunch together? || Food! Now! Come with me! || I could eat a horse! Lunch together?",
                        "I forgot my lunch. It's... a bit of a problem. || Hungry. Very. || I skipped lunch. Not my best idea.",
                        "Hungry. A good friend would know what to do with that information. || Starving. Share your snack, friend? || Hungry. Your treat?");
                case Mood.Bored:
                    return T(t, "Today feels so long... Can we talk for a while? || Such a slow day... I'm glad you're here. || Nothing's happening today. Keep me company?",
                        "Booooored! Let's do something! Anything! || So bored! Let's go somewhere! || Bored bored bored! Save me!",
                        "Classes were dull today. Got anything interesting? || Dull day. You're my only hope. || Boring. Tell me something.",
                        "Bored out of my mind. Entertain me, friend. || Bored. Your job now is to fix that. || Bored. Be interesting.");
                case Mood.Stressed:
                    return T(t, "The tests are coming and I'm so behind... I'm a little scared. || There's so much to do... I don't know where to start. || I've been worrying all day. Can I lean on you a little?",
                        "Ugh, my head's gonna explode! Too much homework! || Three reports due this week! Who does that?! || My brain is full! No more room! Help!",
                        "I have too much on my plate. It's under control. Mostly. || Busy. Deadlines. I'll survive. || Stressed. I'll handle it. ...A coffee wouldn't hurt.",
                        "Everyone's getting on my nerves today. You get a pass. || Rough day. You're one of the few I'm not mad at. || Stressed. Help me forget it?");
                case Mood.Lonely:
                    return T(t, "Nobody really talked to me today... I'm glad you came by. || It's been lonely today... thanks for coming. || I missed having someone to talk to.",
                        "Everyone's busy today. Hang out with me for a bit? || All alone today! Hang out with me! || Nobody's around! You're stuck with me!",
                        "I was on my own all morning. ...You can stay a while. || Alone all day. You can keep me company. || Quiet day. Stay a bit.",
                        "Oh, a friendly face. Finally. || Finally, someone. I was going crazy. || Someone remembered me. Nice.");
                case Mood.Flirty:
                    return T(t, "I'm in such a good mood I can't sit still today. || I feel all bubbly today... || I'm in a funny mood today. A good one.",
                        "I'm super hyped today! Ask me anything! || So much energy today! Let's talk! || I feel amazing! Talk to me!",
                        "I'm in a... talkative mood. Rare, I know. || I'm in a good mood. Enjoy it. || Feeling... friendly today.",
                        "I'm feeling mischievous today. Watch out. || Feeling playful. Lucky you. || Good mood. I might tease you.");
                case Mood.Sleepy:
                    return T(t, "Mm... sorry, I stayed up late reading. I can barely keep my eyes open... || I'm so sleepy... can I lean on you a little? || Sorry, I barely slept. I kept thinking about stuff...",
                        "*yawn* I gamed until three. Totally worth it. Totally dying. || *yawn* Movie marathon last night. Big mistake! || Sleepy! I could nap standing up!",
                        "I didn't sleep. Speak slowly. || Tired. Don't expect wit today. || Three hours of sleep. Be gentle with me.",
                        "So sleepy... if I nod off, wake me before the teacher sees. || Sleepy... let me rest on your shoulder? || *yawn* Keep me awake, friend.");
                case Mood.Upset:
                    return T(t, "I'd... rather not talk about it right now. Sorry. || It's been a hard day. Can we just... be quiet together? || Something happened. I'll tell you later, okay?",
                        "Today sucks and I don't wanna talk about it! ...Thanks for asking, though. || Bad day! But thanks for asking! || Ugh, today's the worst! ...Thanks.",
                        "Bad day. Maybe I'll tell you later. || Not good. Later, maybe. || Rough day. I'll talk when I'm ready.",
                        "Don't push it today. I'm not mad at you, though. || Bad mood. Not your fault. || Not today, friend. Sorry.");
                default:
                    return T(t, "I'm okay, thanks for asking! || I'm good! Thanks! || Doing fine! How about you?", "Same old, same old! || All good! Nothing to report! || Pretty normal! You?", "Normal day. || Nothing worth mentioning. || Ordinary.", "Same as always. Why, worried about me? || Same old. You checking up on me? || Normal. Thanks for caring, I guess.");
            }
        }

        /// <summary>She is asked again on the same day.</summary>
        public static string AlreadyAsked(Tone t, int closeness)
        {
            if (closeness == 0)
                return T(t, "Um... I just told you. But thank you. || I-I already said... but it's nice of you. || Um, nothing changed since a minute ago.", "You already asked! Still the same! || Same as five minutes ago! || Ha! You asked already!", "You asked already. || Same answer. || Still the same.", "You asked that already. Memory trouble? || Again? Did you forget? || You're repeating yourself.");
            if (closeness == 1)
                return T(t, "I already told you, silly... but thanks for caring. || Same as before, silly. || Nothing's changed. But that's sweet.", "Same as before! You worried about me? || Still the same! You're cute! || Nothing new! But thanks!", "You asked already. || Same answer. || Still the same.", "Again? You really like my company, huh? || Again? Just admit you want to talk to me. || You asked already. Looking for excuses to stay?");
            return T(t, "I already told you, silly... but thank you for caring. || Same as before. But I love that you ask. || You already asked, silly. Come here.", "Same as before! You worried about me? || Still the same! You're cute! || Nothing new! But thanks!", "You asked already. || Same answer. || Still the same.", "Again? You really can't stop thinking about me~ || You asked already. You just like hearing my voice~ || Again? You're so obvious~");
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
