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
                case Tone.Cool: case Tone.Proper: return Alt(cool);     // Proper falls back to the cool line
                case Tone.Teasing: return Alt(teasing);
                case Tone.Shy: return Shyer(Alt(gentle));              // Shy falls back to the gentle line, a little hesitant
                default: return Alt(gentle);
            }
        }

        /// <summary>The six voices: gentle, energetic, cool, teasing, shy, proper.</summary>
        public static string T(Tone t, string gentle, string energetic, string cool, string teasing, string shy, string proper)
        {
            switch (t)
            {
                case Tone.Shy: return Alt(shy);
                case Tone.Proper: return Alt(proper);
                default: return T(t, gentle, energetic, cool, teasing);
            }
        }

        /// <summary>A gentle line from a shy girl: now and then it starts with a stammer ("U-um... ").</summary>
        private static string Shyer(string s)
        {
            if (string.IsNullOrEmpty(s) || s.StartsWith("...", System.StringComparison.Ordinal) || s.Length < 2 || Random.value > 0.45f) return s;
            var c = s[0];
            if (!char.IsLetter(c)) return s;
            var low = c == 'I' && (s.Length == 1 || !char.IsLetter(s[1])) ? c : char.ToLowerInvariant(c);
            return Random.value < 0.5f ? "U-um... " + low + s.Substring(1) : c + "-" + low + s.Substring(1);
        }

        /// <summary>
        /// "first || second || third": one of them (at least three versions of each line). A line without " || "
        /// comes back as it is.
        /// </summary>
        public static string Alt(string s)
        {
            if (s == null || s.IndexOf(" || ", System.StringComparison.Ordinal) < 0) return s;
            var parts = s.Split(new[] { " || " }, System.StringSplitOptions.None);
            var key = parts[0].GetHashCode();
            int last, i = Random.Range(0, parts.Length);
            if (LastAlt.TryGetValue(key, out last) && last == i) i = (i + 1 + Random.Range(0, parts.Length - 1)) % parts.Length;
            LastAlt[key] = i;
            return parts[i];
        }

        /// <summary>The version each line said last time, so the same one does not come twice in a row.</summary>
        private static readonly System.Collections.Generic.Dictionary<int, int> LastAlt = new System.Collections.Generic.Dictionary<int, int>();

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

        /// <summary>The one mood answer not in ask.dlg: a teasing girlfriend in a flirty mood.</summary>
        public static string FlirtyTease()
        {
            return Alt("I'm feeling a little naughty today. Wanna find out how much? || You look delicious today. Is that a problem? || Mm~ I've got ideas today. Want to hear them? || I'm feeling naughty. Want proof? || You look tasty today. Problem? || I have ideas today. Interested?");
        }

        /// <summary>Hint about something she likes (reveals a profile like).</summary>
        public static string LikeHint(Tone t, string likeLabel)
        {
            return T(t,
                "By the way... I really like " + likeLabel + ". I don't tell many people that. || By the way... I love " + likeLabel + ". Not many people know. || Um... I'm really fond of " + likeLabel + ". || I'll tell you a secret... " + likeLabel + " makes me so happy.",
                "Oh! Did I ever tell you I'm crazy about " + likeLabel + "? Like, crazy crazy! || I'm obsessed with " + likeLabel + "! Did you know?!" + " || " + likeLabel + "! I love it so much! || Fun fact: " + likeLabel + " is my favorite thing ever!",
                "If you must know, I have a weakness for " + likeLabel + ". || I'm partial to " + likeLabel + "." + " || " + likeLabel + ". A weakness of mine. || If you care: " + likeLabel + ".",
                "Want a hint? I go weak for " + likeLabel + ". Use that information wisely~ || Hint: " + likeLabel + ". Use it well~ || I melt for " + likeLabel + ". Remember that. || Want to win me over? " + likeLabel + ".",
                "U-um... can I tell you a secret? I really, really like " + likeLabel + ". Please don't laugh. || I... I love " + likeLabel + ". I've never told anyone that. || " + likeLabel + " makes me happy... th-there, I said it.",
                "If I may confide in you: I'm quite devoted to " + likeLabel + ". || I have a particular fondness for " + likeLabel + ". I trust you'll remember. || Since you're dear to me, I'll say it: " + likeLabel + " is my weakness.");
        }

        // ---------------- gifts ----------------

        public static string GiftLiked(Tone t, string item)
        {
            return T(t,
                "Th-this is for me? " + item + "... You remembered what I like. Thank you so much!" + " || " + item + "? For me? You remembered! Thank you! || Oh... " + item + "... you know me so well. Thank you." + " || " + item + "! I'm so happy... thank you.",
                "No way! " + item + "?! You're the best! Seriously the best!" + " || " + item + "?! Best gift ever! || No way! " + item + "! You rock!" + " || " + item + "! I love it! I love you! Kidding! ...Kinda!",
                "..." + item + ". You actually paid attention. I'll treasure it. || ..." + item + ". You paid attention. Thank you. || ..." + item + ". I'll keep it safe. || ..." + item + ". Thoughtful.",
                "Oh? " + item + ", huh? You know exactly how to make a girl happy~" + " || " + item + "? You know how to please a girl~ || Oh my, " + item + ". Points for you." + " || " + item + "~ You're learning.",
                "F-for me? " + item + "... you remembered. I-I'm so happy I could cry. || " + item + "...? Th-thank you... I'll take good care of it. || You... you knew I liked " + item + "? Thank you... really.",
                "" + item + ". How thoughtful of you. I'm truly touched. || You remembered my tastes. " + item + "... thank you, sincerely. || What a lovely gift. " + item + " is exactly what I like. Thank you.");
        }

        public static string GiftNeutral(Tone t, string item)
        {
            return T(t,
                "Oh, " + item + "... Thank you. It's very kind of you." + " || " + item + "... thank you. That's kind. || Oh, " + item + ". Thank you. || Thank you for " + item + ".",
                "Huh, " + item + "? Cool, thanks!" + " || " + item + "? Cool! Thanks! || Oh, " + item + "! Thanks! || Thanks for " + item + "!",
                "A gift? ...Thank you. I'll accept it. || ...Thank you. || ...A gift. Thanks. || ...Accepted.",
                "Aww, a present? It's not really my thing, but I'll take the thought. || A present? Not my thing, but thanks. || Hm, " + item + ". It's the thought. || Cute. Not my style, but cute.",
                "Oh... " + item + ". Th-thank you. That's kind. || U-um, thank you for " + item + "... || " + item + "? Thank you... you didn't have to.",
                "Thank you for " + item + ". It's very considerate. || How kind. I'll accept " + item + " gratefully. || " + item + ". Thank you; the thought is appreciated.");
        }

        public static string GiftIntimateYes(Tone t)
        {
            return T(t,
                "Th-this is... You want me to... for you? ...Okay. Only for you. || I-is this... for me to wear? ...I'll think about it. Thank you. || Oh... oh my. Only you get to see this, okay? || F-for me to wear...? Only for you... || Ah... um... only you can see, okay? || This is... so embarrassing... thank you.",
                "Whoa, bold! ...Hehe, fine, I'll wear it. Just for you, got it? || Wow, you're bold! I kinda like it, though! || Seriously? Hehe, okay, you win. || Bold! Okay, just for you! || Whoa! Hehe, fine! || You're shameless! I like it!",
                "...You're shameless. I'll keep it. Don't expect me to say thanks out loud. || ...Interesting choice. I'll accept it. || Hm. Bold. I'll keep it. || ...Shameless. I'll keep it. || ...Bold. Accepted. || ...Hm. Kept.",
                "Ooh~ So that's what you've been thinking about. I'll show you sometime. || Oh my~ Want to see it on me sometime? || Naughty boy~ I'll put it to good use. || Want to see it on me? || Naughty. I'll model it. Someday. || So that's your taste~",
                "Th-this is... I... okay. Only you can ever see it. Promise? || I-I can't look at it... b-but I'll keep it. For you. || My face is burning... thank you... I think?",
                "This is... rather forward of you. ...I'll keep it. For your eyes only. || How improper. ...I won't refuse it. || I'll accept this, discreetly. Not a word to anyone.");
        }

        public static string GiftIntimateNo(Tone t)
        {
            return T(t,
                "W-what?! I can't accept something like this! We're not... like that! || N-no! That's too... we're not that close! || I-I can't take this! It's embarrassing! || I can't accept this! We're not like that! || N-no! That's too much! || I-I can't! It's embarrassing!",
                "Eh?! Dude, what is wrong with you?! || Gross! No way! || Are you serious?! No! || What is WRONG with you?! || Ew! No! || Seriously?! No way!",
                "Absolutely not. Know your place. || No. Inappropriate. || Take it back. Now. || Absolutely not. || Inappropriate. || Take it back.",
                "Wow. Bold move. Wrong girl. Try again in a few months. || Cute. No. Not yet. || Oh, you wish. Try harder. || Wrong girl. Try later. || Cute. Not yet. || In your dreams.",
                "N-no! I-I can't take this! Please put it away! || Wh-why would you... no, I can't... || I-I'm going to faint. No. Please, no.",
                "I must decline. This is entirely inappropriate. || Please put that away. We are not on such terms. || No. I'm disappointed you thought this was acceptable.");
        }

        public static string GiftAlreadyToday(Tone t)
        {
            return T(t,
                "You already gave me something today... I can't accept more, really. || You're too generous... one is enough for today. || Thank you, but please, save it for another day. || You already gave me something... thank you, though. || One gift is plenty today. || Save it for tomorrow, please.",
                "Another one? Save some for tomorrow! || Whoa, two in one day? Slow down! || Hey, I already got one today! || Save some for tomorrow! || Two?! Slow down! || Already got one!",
                "One gift a day is plenty. || Already got one today. || Enough for today. || One a day. || Enough today. || Tomorrow.",
                "Trying to buy me? One a day, cutie. || Spoiling me? Tomorrow, cutie. || Patience. One per day~ || Buying me? One a day. || Spoiling me? Tomorrow. || Patience, cutie.",
                "Y-you already gave me something today... I can't take more, I'll feel bad. || Another? N-no, one is plenty... thank you. || Please keep it... I already got one from you today.",
                "You're very generous, but one gift a day is quite enough. || I must refuse a second gift today. Thank you all the same. || Please, save it for another day.");
        }

        public static string PlayerGivesGift(string item)
        {
            return Pick("Here, I got you something. " + item + ".", "This is for you. " + item + ".", "I saw this and thought of you.");
        }

        // ---------------- requests (talk-based completion) ----------------

        public static string RequestThanks(Tone t)
        {
            return T(t,
                "You really came... Thank you. That means a lot to me. || You came! I'm so relieved. || Thank you for coming. Really. || You came... thank you so much. || I'm so glad you came. || You came for me... thank you.",
                "You made it! Knew I could count on you! || You're here! Awesome! || Yes! You came through! || You made it! || You came! Yay! || Knew you'd come!",
                "You're on time. Good. I appreciate it. || You came. Thank you. || Punctual. I like that. || On time. Thank you. || You came. Good. || Punctual. Thanks.",
                "Took you long enough. ...Thanks, though. || Oh, you came. Good boy~ || Finally. I was starting to think you forgot. || Finally. Thanks. || Good boy. You came. || About time. Thanks.",
                "Y-you really came... I was scared you wouldn't. Thank you. || You're here... I'm so relieved. Th-thank you. || You came for me? I... thank you.",
                "Thank you for coming. I appreciate your punctuality. || You came as promised. That means a great deal. || Thank you for keeping your word.");
        }

        public static string ErrandDelivered(Tone t)
        {
            return T(t,
                "You did it for me? Thank you, really! I owe you one. || You really did it... thank you so much. || You're so kind. I won't forget this. || You did it for me? Thank you! || Thank you so much... really. || I owe you. Thank you.",
                "You're a lifesaver! Seriously! || You're the best! Thank you! || Hero! My hero! || You saved me! || You're the best! || My hero!",
                "Done already? Impressive. Thank you. || Efficient. Thank you. || You did it. I'm grateful. || Done? Thank you. || Efficient. Thanks. || Grateful.",
                "Look at you, being useful. I like that. || Good job~ I might reward you. || My, how helpful. I'll remember this. || Useful. I like it. || Good job. Reward pending. || Helpful boy. Noted.",
                "Y-you did it for me? I... thank you. I don't know how to repay you. || You really did it... th-thank you so much. || I-I owe you one. A big one.",
                "Thank you. You've been a tremendous help. || Splendid. I'm in your debt. || That was very kind of you. I won't forget it.");
        }

        public static string MeetAtLine(Tone t, string place)
        {
            return T(t,
                "I like being here with you... the " + place + " feels different when you're around. || The " + place + " feels warmer with you here. || I like the " + place + " more when you're with me. || Being at the " + place + " with you is nice...",
                "See? The " + place + " is way more fun with company! || The " + place + " is way better with you! || Hanging at the " + place + " with you! Yay! || The " + place + " rocks with company!",
                "The " + place + " is quiet at this hour. I like that. And you're... fine, too. || The " + place + " is calm. You're fine too. || The " + place + " is quiet. I don't mind sharing it. || The " + place + ". Peaceful. With you, still peaceful.",
                "Just the two of us at the " + place + "~ Don't get any funny ideas. Or do. || Alone at the " + place + "~ Behave. Or don't. || Just us at the " + place + ". Hmm~ || The " + place + ", you and me. Interesting.",
                "The " + place + " is... nice with you here. Less scary. || I-I like being at the " + place + " with you. It's calm. || Just us at the " + place + "... my heart's a little loud.",
                "The " + place + " is lovely at this hour. And the company is too. || I'm glad we chose the " + place + ". It suits a quiet talk. || The " + place + ". A fine place to spend time with you.");
        }

        /// <summary>The girl who left the note in your locker explains what she needs.</summary>
        public static string NoteHelp(Tone t)
        {
            return T(t,
                Pick("Y-you came... I have to read my report in front of the class tomorrow and I freeze every time. Could you listen while I practice?",
                     "Th-thanks for coming. I lost the notes for tomorrow's test... could you go over the main points with me?") + " || " + "Y-you came... could you help me practice my presentation? I'm so nervous... || Thanks for coming... could you quiz me for tomorrow's test? || You're here... I need help with my history report. Please?",
                Pick("You came! Okay, don't laugh: I signed up to give the club speech and I have no idea what to say. Help me write it?",
                     "Yay, you're here! I'm making the poster for the campus festival and it looks like a ransom note. Help!") + " || " + "You came! Help me pick songs for the club party! || Yay! I need help making flyers for the club! || You're here! Help me plan the class trip game!",
                Pick("Good, you're here. I'm stuck on the math assignments and I'd rather not ask in class. Ten minutes?",
                     "You came. I need someone to time my run-through for the class presentation. Quietly.") + " || " + "You're here. I need someone to check my essay. Quietly. || Good. Proofread my report. Ten minutes. || You came. I need a practice partner for English.",
                Pick("Hey, you actually came. I need a second opinion on the festival poster, and you're the only one who'll be honest.",
                     "Look who showed up. I'm writing the class newsletter and I need a real person to tell me if it's boring.") + " || " + "You came. Rate my speech. Honestly. || Look who's here. I need an honest critic for my poem. || You showed up. Help me pick a costume for the festival.",
                "Y-you came... I have to speak in front of the class tomorrow and I can't even breathe. Could you listen to me practice? || Th-thank you for coming... I lost my notes and I'm too scared to ask the teacher. Could you help? || I-I'm sorry to call you out here... I need help with my report. Please?",
                "Thank you for coming. I'm preparing a speech for the student council and would value an honest listener. || I appreciate you making the time. Would you review my essay before I submit it? || Thank you. I need someone to quiz me for tomorrow's exam, and I trust you to be thorough.");
        }

        public static string NoteThanks(Tone t)
        {
            return T(t,
                "...That really helped. I think I can do it now. Thank you. || Thank you... I feel so much better about it now. || You helped a lot. I'm not scared anymore. || That helped so much. Thank you. || I feel ready now. Thank you. || You're wonderful. Thank you.",
                "Done! You're the best. I owe you a juice! || We did it! High five! || Finished! I'm buying you something! || Done! Juice is on me! || We did it! || Finished! You rock!",
                "That's settled, then. ...Thanks. I mean it. || Done. Thank you for your time. || That helped. Thank you. || Settled. Thanks. || Done. Appreciated. || Good. Thank you.",
                "Okay, that's actually good now. Don't let it go to your head~ || Not bad, partner~ || Okay, you're useful. I'll admit it. || Not bad, partner. || Okay, you're useful. || That's good now. Don't get smug.",
                "Th-that helped so much... I think I can do it now. Thank you. || I'm not as scared anymore... because of you. || Thank you... I-I'll buy you a juice, okay?",
                "Thank you. That was precisely what I needed. || Much better. I'm grateful for your patience. || Splendid. I'll repay the favor, I assure you.");
        }

        // ---------------- how close she is to you ----------------

        /// <summary>0 = stranger or acquaintance, 1 = friend, 2 = girlfriend (from the game's own relation).</summary>
        public static int Closeness(SaveData.Heroine h)
        {
            if (h == null) return 0;
            var r = h.relation;
            return r >= 2 ? 2 : r == 1 ? 1 : 0;
        }

        /// <summary>Hint about something she likes, as open as she is with you.</summary>
        public static string LikeHint(Tone t, string likeLabel, int closeness)
        {
            if (closeness >= 2) return LikeHint(t, likeLabel);
            if (closeness == 1)
                return T(t,
                    "Can I tell you something? I really love " + likeLabel + ". || Can I tell you...? I love " + likeLabel + ". || I really like " + likeLabel + "... it's my favorite." + " || " + likeLabel + "... it makes me so happy.",
                    "Oh! Did I ever tell you I'm crazy about " + likeLabel + "? Like, crazy crazy!" + " || " + likeLabel + "! Obsessed! || I LOVE " + likeLabel + "! || Did you know?! " + likeLabel + "!",
                    "...I have a weakness for " + likeLabel + ". Keep that between us." + " || " + likeLabel + ". Between us. || A weakness: " + likeLabel + ". || I'm fond of " + likeLabel + ". Quietly.",
                    "Want a hint? I'm a sucker for " + likeLabel + ". Use that wisely. || Hint: " + likeLabel + ". || I'm a sucker for " + likeLabel + "." + " || " + likeLabel + ". Use it wisely.",
                "U-um... I really like " + likeLabel + ". That's all. || C-can I tell you something? " + likeLabel + "... I love it. || " + likeLabel + " is my favorite. Please don't laugh.",
                "I'll tell you, since we're friends: I'm quite fond of " + likeLabel + ". || I have a weakness for " + likeLabel + ". I hope that isn't odd. || " + likeLabel + " is a particular pleasure of mine.");
            return T(t,
                "Oh, um... I like " + likeLabel + ", if you were wondering. || I like " + likeLabel + ", by the way. || Um, " + likeLabel + " is nice. I like it. || I'm fond of " + likeLabel + ".",
                "Oh, and I'm really into " + likeLabel + "! || I'm into " + likeLabel + "!" + " || " + likeLabel + "! Love it! || Big fan of " + likeLabel + "!",
                "If you must know, I like " + likeLabel + ". || I like " + likeLabel + "." + " || " + likeLabel + ". That's my preference." + " || " + likeLabel + ", if you care.",
                "I like " + likeLabel + ". Don't spread it around." + " || " + likeLabel + ". Keep it quiet. || I like " + likeLabel + ". Secret." + " || " + likeLabel + ". Don't tell.",
                "Um... I like " + likeLabel + "... if you wanted to know. || " + likeLabel + " is nice... s-sorry, random. || I-I like " + likeLabel + ". That's all.",
                "I'm rather fond of " + likeLabel + ", if you're curious. || " + likeLabel + " is something I enjoy. || I do like " + likeLabel + ", for what it's worth.");
        }
    }
}
