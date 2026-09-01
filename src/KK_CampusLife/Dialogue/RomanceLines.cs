using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// Lines of the heart page of a conversation: holding hands, asking how she feels, teasing, whispering, talking about
    /// the last time, sneaking off. Every line of hers has the four personality tones (gentle, energetic, cool, teasing).
    /// </summary>
    internal static class RomanceLines
    {
        private static string T(Tone t, string gentle, string energetic, string cool, string teasing) { return Lines.T(t, gentle, energetic, cool, teasing); }
        private static int R(int n) { return Random.Range(0, n); }

        // ================= holding hands =================

        public static string PlayerHand(int close)
        {
            if (close >= 2) return Lines.Pick("Come here. Give me your hand.", "I missed holding your hand.", "Let's walk like this for a while.");
            return Lines.Pick("Can I... hold your hand for a bit?", "Your hands look cold. Here.", "Would it be weird if I held your hand?");
        }

        public static string HandYes(Tone t, int close)
        {
            if (close >= 2)
                switch (R(3))
                {
                    case 0: return T(t, "Mm... your hand is so warm. I could stay like this all day.", "Hehe, fingers laced! That's the official couple grip!", "...Don't let go yet.", "Holding hands in public? How bold. ...I'm not complaining.");
                    case 1: return T(t, "Every time we do this my heart races like the first time...", "Swing, swing! Ehehe, I love this!", "Your hand fits mine. Funny how that works.", "Squeeze back if you love me. ...Hm. Good answer.");
                    default: return T(t, "People are looking... but I don't mind. Not with you.", "Let's walk the long way so this lasts longer!", "...This is nice. Don't tell anyone I said that.", "If you let go first, you lose. Got it?");
                }
            switch (R(3))
            {
                case 0: return T(t, "Eh...? O-okay... just for a little while...", "Whoa! Um. Okay! Sure! This is fine! Totally fine!", "...Fine. Your hand's warmer than I expected.", "Oh? Taking the initiative today? ...I'll allow it.");
                case 1: return T(t, "My face is so hot right now... please don't look...", "Ahaha, my hand's all sweaty, sorry! ...Don't let go though.", "...I suppose it's not unpleasant.", "Careful. If you hold on too long, I might not give it back.");
                default: return T(t, "It's... kind of nice, isn't it?", "Okay, this is kinda cute. Don't make it weird!", "Hm. Only until the end of the hallway.", "You're shaking a little. Nervous? How cute.");
            }
        }

        public static string HandNo(Tone t)
        {
            return T(t, "Ah... s-sorry, not here... people might see...", "Whoa, whoa! Hands to yourself, mister!", "No.", "Holding hands already? Earn it first.");
        }

        // ================= how she feels about you =================

        public static string PlayerAskFeelings()
        {
            return Lines.Pick("Can I ask you something? What do you think of me?", "Be honest. How do you see me?", "What am I to you, exactly?");
        }

        /// <summary>Her honest answer, from how much she likes you, your relation and how lewd she is with you.</summary>
        public static string Feelings(Tone t, SaveData.Heroine h)
        {
            if (h.isGirlfriend && h.lewdness >= 70)
                return T(t, "You're my boyfriend... and the only one who gets to see me like... that. Don't make me say more!",
                    "You're my favorite person! And, um... I kinda can't stop thinking about you. In every way. Ehehe...",
                    "You're mine. All of you. That's what I think.",
                    "You're my boyfriend and my favorite toy. Is that honest enough for you?");
            if (h.isGirlfriend)
                return T(t, "You're the most important person in the world to me. That's all.",
                    "You're my boyfriend, silly! The best one in the whole school!",
                    "You're the person I chose. I don't regret it.",
                    "You're mine. Isn't that obvious? Or do you just like hearing it?");
            if (h.favor >= 80)
                return T(t, "I... I think about you a lot. More than I should, maybe...",
                    "Honestly? You're kind of my favorite person right now. Don't let it go to your head!",
                    "You're... important to me. More than I planned.",
                    "Hmm... let's just say if you asked me out, I wouldn't say no right away.");
            if (h.favor >= 50)
                return T(t, "You're a really good friend. I'm glad we met.",
                    "You're super fun to be around! Like, top three people, easy!",
                    "You're reliable. I like that.",
                    "You're interesting. That's rare around here. Keep it up.");
            return T(t, "Um... you seem nice? I don't know you that well yet...",
                "Hmm, you're okay! We should hang out more, then I'll tell you!",
                "I don't have an opinion yet.",
                "Ask me again when you've done something worth talking about.");
        }

        // ================= teasing =================

        public static string PlayerTease(int close)
        {
            if (close >= 2) return Lines.Pick("You know what you look like right now? Very kissable.", "That skirt's a little short today. Not that I'm complaining.", "Is it hot in here, or is it just you?");
            return Lines.Pick("You've been staring at me, haven't you?", "Careful, if you keep smiling like that I'll get ideas.", "You look really good today. Like, distractingly good.");
        }

        public static string TeaseGood(Tone t, int close)
        {
            if (close >= 2)
                switch (R(3))
                {
                    case 0: return T(t, "Y-you can't just say things like that at school...! ...Say it again later.", "Ehehe, you dummy! ...Okay, maybe a little kiss. Later!", "...Keep that energy for when we're alone.", "Kissable? Then why are you still talking?");
                    case 1: return T(t, "I wore it for you... is that embarrassing?", "Hey! Eyes up here! ...Or not. I'm not your boss. Hehe.", "Noticed, did you. Good.", "Short? Oh, you mean this? *She tugs the hem up a little, then lets it fall.* Oops.");
                    default: return T(t, "It's you... you make me feel hot all over...", "Hot?! Wh- you're the one making it hot! Jerk! ...Hehe.", "It's me. Obviously.", "Both. Want to find out which is hotter?");
                }
            switch (R(3))
            {
                case 0: return T(t, "I-I wasn't staring! ...Maybe a little.", "Me?! Staring?! Pfft, no way! ...Okay, a tiny bit.", "...You noticed. Annoying.", "And if I was? What would you do about it?");
                case 1: return T(t, "Ideas...? W-what kind of ideas...?", "Ideas, huh? Hehe, you're kinda bold! I like it!", "Ideas. Hm. Keep them to yourself for now.", "Get them, then. I want to see what you come up with.");
                default: return T(t, "Th-thank you... I tried a little harder today...", "Distractingly?! Ahaha, I'll take it!", "...I'll accept the compliment.", "Distracting you was the plan. Glad it's working.");
            }
        }

        public static string TeaseBad(Tone t)
        {
            return T(t, "Th-that's embarrassing... please don't say that...", "Ew, gross! ...Kidding. But no.", "That was unnecessary.", "Is that the best you've got? Pathetic. Try again someday.");
        }

        // ================= whispering =================

        public static string PlayerWhisper(bool girlfriend)
        {
            if (girlfriend) return Lines.Pick("*You lean in.* I can't stop thinking about last time. About your skin.", "*Quietly.* I want you. Right now. Here.", "*In her ear.* You smell amazing. I want to taste you.");
            return Lines.Pick("*You lean in.* You have no idea what I'm thinking about right now.", "*Quietly.* I keep imagining what you look like without the uniform.", "*In her ear.* If we were alone right now...");
        }

        public static string WhisperGood(Tone t, bool girlfriend)
        {
            if (girlfriend)
                switch (R(3))
                {
                    case 0: return T(t, "Hah... d-don't breathe on my ear like that... my legs are going weak...", "Ahh, stop it! I'm getting all tingly! ...Don't stop.", "...Careful. I might drag you somewhere private.", "Taste me? Then hurry up and take me somewhere nobody can hear.");
                    case 1: return T(t, "Me too... I keep remembering your hands on me...", "Ehehe, same! I even dreamt about it! ...Was that too honest?", "Then don't make me wait.", "Right now? Here? ...You're lucky I like it when you're reckless.");
                    default: return T(t, "I-I'm all yours... just not in front of everyone...", "Ahh, my face is on fire! You're the worst! ...I love it.", "...You'll pay for saying that in a hallway. Later.", "Mm, keep whispering. I want to see how red you can make me.");
                }
            switch (R(3))
            {
                case 0: return T(t, "W-without the uniform?! ...D-don't imagine too hard...", "Hey! Pervert! ...What were you imagining, exactly?", "...That's indecent. Continue.", "Imagining is free. Seeing costs extra.");
                case 1: return T(t, "If we were alone... I-I don't know what I'd do...", "Alone, huh? Then you'd better make it worth my time!", "If we were alone, you'd find out.", "If we were alone, you'd already be on your knees.");
                default: return T(t, "My heart's beating so fast... you can't say that so close...", "Ahaha! Ticklish! And, um... kinda hot. Don't tell anyone!", "...You're bold. I'll remember it.", "Oh? Tell me more. In detail.");
            }
        }

        public static string WhisperBad(Tone t)
        {
            return T(t, "Eek! I-I don't know you like that...!", "Whoa, way too much, way too fast! Down, boy!", "Do that again and I'm reporting you.", "Cute. But you haven't earned the right to talk to me like that.");
        }

        // ================= the last time =================

        public static string PlayerMemories(int hCount)
        {
            if (hCount <= 1) return Lines.Pick("I keep thinking about our first time.", "Do you ever think about... that day?", "That day... I haven't forgotten a single thing.");
            return Lines.Pick("I keep replaying last time in my head.", "You were incredible the other day.", "When can we do that again?");
        }

        public static string Memories(Tone t, int hCount)
        {
            if (hCount <= 1)
                return T(t, "Our first time... I was so nervous, but you were gentle. I'll treasure it forever.",
                    "Ahh, don't bring it up out of nowhere! ...It was amazing, okay? Way better than I imagined!",
                    "...I think about it more than I'd like to admit.",
                    "You were so nervous. It was adorable. Next time, I'm in charge.");
            switch (R(3))
            {
                case 0: return T(t, "Me too... I can't focus in class when I remember your voice...", "Hehe, you mean when I couldn't stop making noise? So embarrassing! ...Let's do it again!", "You weren't bad. I want more.", "Incredible? I was holding back. Want to see what I can really do?");
                case 1: return T(t, "I still blush when I walk past... that place...", "I still have a mark, you know! Right here. *She tugs her collar.* Your fault!", "...Same place, next time. I liked it there.", "Again? You're insatiable. ...Good. So am I.");
                default: return T(t, "Whenever you want... I-I mean... soon, please...", "Soon! Tomorrow! Today?! Okay, I'm too excited, sorry!", "When I say so. Which might be soon.", "Beg nicely and I'll think about it.");
            }
        }

        // ================= sneaking off =================

        public static string PlayerSneak()
        {
            return Lines.Pick("Hey... want to sneak off somewhere quiet? Just the two of us.", "I know a spot where nobody goes. Come with me?", "Let's skip the next bit and find somewhere private.");
        }

        public static string SneakYes(Tone t)
        {
            switch (R(2))
            {
                case 0: return T(t, "...Okay. Lead the way. Just... be gentle, okay?", "Ooh, a secret mission! I'm in! Don't get us caught!", "Fine. But if a teacher finds us, it was your idea.", "Finally. I was wondering how long you'd make me wait.");
                default: return T(t, "M-my heart won't calm down... let's go before I lose my nerve...", "Race you there! Loser has to... ehehe, you'll see!", "...Somewhere with a lock on the door.", "Somewhere private? You read my mind. Hurry.");
            }
        }

        public static string SneakNo(Tone t)
        {
            return T(t, "N-not today... I'm not ready for something like that...", "Ehh? Sneak off? With you? Pfft, no way! ...Not yet, anyway.", "No. Don't push your luck.", "Sneak off with you? You wish. Try harder.");
        }

        // ================= being sweet (more variants of the old Flirt) =================

        public static string PlayerSweet(int close)
        {
            if (close >= 2) return Lines.Pick("I was looking forward to seeing you all day.", "You're the best part of my day, you know.", "Being with you is my favorite thing.", "I keep thinking about our last date.",
                "You look cute when you're thinking hard about something.", "I'd skip every class just to sit here with you.");
            return Lines.Pick("I really like spending time with you.", "You're kind of amazing, you know that?", "Talking to you always cheers me up.", "I'm glad I met you.",
                "Your laugh is really cute.", "I find myself looking for you in the hallway.");
        }
    }
}
