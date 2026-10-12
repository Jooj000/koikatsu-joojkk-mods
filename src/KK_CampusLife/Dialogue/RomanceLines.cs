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
                    case 0: return T(t, "Mm... your hand is so warm. I could stay like this all day. || Your hand is so warm... I don't want to let go. || Mm... I feel safe like this. || Holding your hand makes everything feel okay.", "Hehe, fingers laced! That's the official couple grip! || Laced fingers! Super official! || Hand-holding power! Hehe! || Look! We're holding hands! Everyone, look!", "...Don't let go yet. || ...Keep holding. || ...Not yet. Don't let go. || ...Stay like this.", "Holding hands in public? How bold. ...I'm not complaining. || Public hand-holding? Scandalous. Keep going. || Showing me off? Good. || Brave, holding my hand here. I approve.");
                    case 1: return T(t, "Every time we do this my heart races like the first time... || My heart still races every time... || It's like the first time, every time... || Even now, my heart jumps when you take my hand.", "Swing, swing! Ehehe, I love this! || Swing! Swing! Ahaha! || Wheee! Hand swinging! || Ehehe, this never gets old!", "Your hand fits mine. Funny how that works. || Your hand fits mine. Odd. || ...We fit. Strange. || ...It fits. I noticed.", "Squeeze back if you love me. ...Hm. Good answer. || Squeeze if you love me. ...Correct. || Squeeze twice if you're mine. ...Good. || Prove it. Squeeze. ...Good boy.");
                    default: return T(t, "People are looking... but I don't mind. Not with you. || Let them look... I'm happy with you. || I don't care who sees. Not with you. || People are staring... it's okay.", "Let's walk the long way so this lasts longer! || Long way home! So this lasts longer! || Let's take the scenic route! Hehe! || Slow walk! Make it last!", "...This is nice. Don't tell anyone I said that. || ...This is pleasant. Keep that secret. || ...Nice. Don't repeat that. || ...I like this. Tell no one.", "If you let go first, you lose. Got it? || First to let go loses. Deal? || Let go and you lose. Got it? || Whoever lets go owes the other. Ready?");
                }
            switch (R(3))
            {
                case 0: return T(t, "Eh...? O-okay... just for a little while... || Eh...? O-okay... just a little... || Ah... um... alright... || Oh... okay... only for a bit...", "Whoa! Um. Okay! Sure! This is fine! Totally fine! || Whoa! Okay! This is fine! Totally fine! || Huh?! Sure! Yep! Hand! Okay! || Ah! Um! Cool! Totally normal!", "...Fine. Your hand's warmer than I expected. || ...Fine. Warm hand. || ...Alright. Surprisingly warm. || ...Acceptable.", "Oh? Taking the initiative today? ...I'll allow it. || Taking the lead? I'll allow it. || Oh? Bold today. Fine. || My, how forward. Go on.");
                case 1: return T(t, "My face is so hot right now... please don't look... || Please don't look at my face... it's so red... || My cheeks are burning... || D-don't look at me...", "Ahaha, my hand's all sweaty, sorry! ...Don't let go though. || My hand's sweaty, sorry! Don't let go! || Ahaha, so sweaty! Keep holding! || Ugh, clammy! Still don't let go!", "...I suppose it's not unpleasant. || ...Not unpleasant. || ...I'll tolerate it. || ...It's fine.", "Careful. If you hold on too long, I might not give it back. || Hold too long and I'll keep it. || Careful, I might not return it. || Keep holding and it's mine.");
                default: return T(t, "It's... kind of nice, isn't it? || It's nice, isn't it...? || This feels... nice. || I like this... a lot.", "Okay, this is kinda cute. Don't make it weird! || Okay, this is cute! Don't ruin it! || Kinda cute! No jokes! || Aww, this is nice! Shh!", "Hm. Only until the end of the hallway. || Only to the end of the hall. || Until the corner. || Briefly.", "You're shaking a little. Nervous? How cute. || You're trembling. Adorable. || Nervous? Cute. || Shaky hands? How sweet.");
            }
        }

        public static string HandNo(Tone t)
        {
            return T(t, "Ah... s-sorry, not here... people might see... || S-sorry... not here... || Ah, people might see... || N-not in front of everyone...", "Whoa, whoa! Hands to yourself, mister! || Hands off, mister! || Whoa, slow down! || Nope! Not yet!", "No. || No. || Not now. || Don't.", "Holding hands already? Earn it first. || Earn it first. || Too soon, sweetie. || Not so fast.");
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
                return T(t, "You're my boyfriend... and the only one who gets to see me like... that. Don't make me say more! || You're my boyfriend... and only you see me like... that. Don't make me say more! || You're the only one I'd ever... um... you know. || You're mine, and I'm yours... in every way. Ah, embarrassing!",
                    "You're my favorite person! And, um... I kinda can't stop thinking about you. In every way. Ehehe... || You're my favorite! And I think about you ALL the ways! Ehehe! || You're mine! Heart, body, everything! Too much? || I can't stop thinking about you! Like, at all! In every way!",
                    "You're mine. All of you. That's what I think. || You're mine. Entirely. || All of you belongs to me. || You're mine. Body included.",
                    "You're my boyfriend and my favorite toy. Is that honest enough for you? || My boyfriend and my favorite plaything. Honest enough? || Mine to love and mine to tease. || You're my boyfriend. And my toy. Both.");
            if (h.isGirlfriend)
                return T(t, "You're the most important person in the world to me. That's all. || You're the most precious person to me. || You mean everything to me. || You're my most important person.",
                    "You're my boyfriend, silly! The best one in the whole university! || You're my boyfriend, dummy! The best one! || Best boyfriend in the university! Easy! || You're mine! Number one!",
                    "You're the person I chose. I don't regret it. || I chose you. No regrets. || You're my choice. Final. || I picked you. Correctly.",
                    "You're mine. Isn't that obvious? Or do you just like hearing it? || You're mine. Need me to say it again? || Mine. Obviously. Like hearing it? || You belong to me. Isn't it clear?");
            if (h.favor >= 80)
                return T(t, "I... I think about you a lot. More than I should, maybe... || I think about you... a lot. Maybe too much. || You're on my mind so often... || I can't stop thinking about you...",
                    "Honestly? You're kind of my favorite person right now. Don't let it go to your head! || You're kinda my favorite person! Don't get cocky! || Top person right now? You! Shh! || Honestly, you're the best! Don't tell!",
                    "You're... important to me. More than I planned. || You matter to me. More than planned. || You're... important. Unexpectedly. || More important than I intended.",
                    "Hmm... let's just say if you asked me out, I wouldn't say no right away. || If you asked me out, I might say yes. Might. || Ask me out sometime. Maybe I'll agree. || Let's say I wouldn't refuse a date.");
            if (h.favor >= 50)
                return T(t, "You're a really good friend. I'm glad we met. || You're a wonderful friend. || I'm so glad we're friends. || I'm happy I met you.",
                    "You're super fun to be around! Like, top three people, easy! || You're SO fun! Top three, easy! || Best friend material! || Hanging with you rocks!",
                    "You're reliable. I like that. || You're dependable. || You're reliable. Good. || I trust you.",
                    "You're interesting. That's rare around here. Keep it up. || You're interesting. That's rare. || Not boring. I like that. || You're fun. Keep it up.");
            return T(t, "Um... you seem nice? I don't know you that well yet... || You seem nice...? I don't know you well yet... || Um... I need to know you more... || I'm not sure yet... you seem kind.",
                "Hmm, you're okay! We should hang out more, then I'll tell you! || You're okay! Hang out more and I'll decide! || Jury's out! Let's chill sometime! || Hmm! Ask me later!",
                "I don't have an opinion yet. || No opinion yet. || Undecided. || Insufficient data.",
                "Ask me again when you've done something worth talking about. || Do something interesting first. || Impress me, then ask. || Ask again later.");
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
                    case 0: return T(t, "Y-you can't just say things like that on campus...! ...Say it again later. || N-not on campus...! ...Say it again later. || Y-you can't say that here...! Later, okay? || Ah...! Not here... but later...", "Ehehe, you dummy! ...Okay, maybe a little kiss. Later! || You dummy! ...Maybe a kiss later! Hehe! || Hehe, later! A little one! || Not now! Later, though!", "...Keep that energy for when we're alone. || ...Save that for privacy. || ...When we're alone. || ...Later. Remember it.", "Kissable? Then why are you still talking? || Kissable? Then stop talking. || Then come here. || Words, words. Prove it.");
                    case 1: return T(t, "I wore it for you... is that embarrassing? || I wore it for you... is that weird? || I chose this for you... um... do you like it? || I hoped you'd notice...", "Hey! Eyes up here! ...Or not. I'm not your boss. Hehe. || Eyes up here! ...Or not. Hehe! || Hey! Looking?! ...It's okay! || Caught you looking! Hehe!", "Noticed, did you. Good. || You noticed. Good. || Hm. Observant. || ...You looked. Good.", "Short? Oh, you mean this? *She tugs the hem up a little, then lets it fall.* Oops. || Short? This? Oops~ || Like it? I chose it to tease you. || Noticed? That was the point.");
                    default: return T(t, "It's you... you make me feel hot all over... || It's you... you make me feel so warm... || You're why I'm so warm... || Because of you... I'm burning up...", "Hot?! Wh- you're the one making it hot! Jerk! ...Hehe. || YOU'RE making it hot! Jerk! Hehe! || Hot?! Your fault! || It's you, dummy! Hehe!", "It's me. Obviously. || Me. Obviously. || It's me. || Both of us. Mostly me.", "Both. Want to find out which is hotter? || Both. Shall we compare? || Find out which. Up close. || Want to test which is hotter?");
                }
            switch (R(3))
            {
                case 0: return T(t, "I-I wasn't staring! ...Maybe a little. || I wasn't staring...! Maybe a little... || I-I wasn't looking...! Okay, a bit... || Ah... you noticed...", "Me?! Staring?! Pfft, no way! ...Okay, a tiny bit. || Staring?! Me?! ...Okay, a little! || Pfft! No! ...Yeah, a bit! || Not staring! ...Okay, yes!", "...You noticed. Annoying. || ...You caught it. || ...Annoying that you noticed. || ...Observant.", "And if I was? What would you do about it? || And if I was? || What if I was? Then what? || I was. Problem?");
                case 1: return T(t, "Ideas...? W-what kind of ideas...? || Ideas...? What kind...? || W-what ideas...? || Um... ideas...?", "Ideas, huh? Hehe, you're kinda bold! I like it! || Bold! I like it! || Ideas, huh?! Hehe! || Ooh! What ideas?!", "Ideas. Hm. Keep them to yourself for now. || Keep them to yourself. For now. || Ideas. Hold them. || Hm. Later.", "Get them, then. I want to see what you come up with. || Show me your ideas. || Go on. Surprise me. || Let's see what you've got.");
                default: return T(t, "Th-thank you... I tried a little harder today... || Thank you... I tried harder today... || Ah... thank you... I put effort in... || Thank you... I hoped you'd like it.", "Distractingly?! Ahaha, I'll take it! || Distracting?! I'll take it! || Hehe! Mission accomplished! || Yes! Distraction successful!", "...I'll accept the compliment. || ...Compliment accepted. || ...Thanks. || ...Noted.", "Distracting you was the plan. Glad it's working. || Distraction was the plan. || Working, then. Good. || I meant to distract you.");
            }
        }

        public static string TeaseBad(Tone t)
        {
            return T(t, "Th-that's embarrassing... please don't say that... || P-please don't say that... || That's so embarrassing... || D-don't...", "Ew, gross! ...Kidding. But no. || Ew! ...Kidding. Still no. || Gross! ...Nah. No. || Yikes! Nope!", "That was unnecessary. || Unnecessary. || Inappropriate. || Don't.", "Is that the best you've got? Pathetic. Try again someday. || That's your best? Sad. || Pathetic. Try again later. || Weak. Very weak.");
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
                    case 0: return T(t, "Hah... d-don't breathe on my ear like that... my legs are going weak... || Hah... your breath... my knees are shaking... || D-don't whisper there... I'm melting... || Ah... my ear... I can't stand up...", "Ahh, stop it! I'm getting all tingly! ...Don't stop. || Stop! Tingly! ...Don't stop! || Ahh! Shivers! ...More! || Eek! Ticklish! ...Keep going!", "...Careful. I might drag you somewhere private. || ...Careful. I might take you somewhere private. || ...Keep that up and we're leaving. || ...Dangerous. Somewhere private, now.", "Taste me? Then hurry up and take me somewhere nobody can hear. || Then take me somewhere quiet. Now. || Hurry. Somewhere nobody hears. || Taste me? Find a locked room.");
                    case 1: return T(t, "Me too... I keep remembering your hands on me... || Me too... I remember your hands... || I keep thinking about your touch... || I still feel your hands on me...", "Ehehe, same! I even dreamt about it! ...Was that too honest? || Same! I dreamt about it! Too honest? || Me too! Every night! Oops! || Hehe, I replay it constantly!", "Then don't make me wait. || Then don't keep me waiting. || Then act. || Don't make me wait.", "Right now? Here? ...You're lucky I like it when you're reckless. || Here? You're reckless. I like that. || Now? Bold. Good. || Right here? Naughty boy.");
                    default: return T(t, "I-I'm all yours... just not in front of everyone... || I'm yours... just not in public... || All yours... somewhere private... || I belong to you... later, okay?", "Ahh, my face is on fire! You're the worst! ...I love it. || My face is on fire! I love it! || You're terrible! Do it again! || Ahh! So red! More!", "...You'll pay for saying that in a hallway. Later. || ...You'll pay for that later. || ...Bold. Later, you'll answer for it. || ...Later. You'll regret saying that here.", "Mm, keep whispering. I want to see how red you can make me. || Keep whispering. Make me redder. || More. Make me blush. || Go on. I want to burn.");
                }
            switch (R(3))
            {
                case 0: return T(t, "W-without the uniform?! ...D-don't imagine too hard... || W-without...?! Don't imagine too much...! || Ah! P-please don't picture that...! || Not the uniform...! Stop imagining!", "Hey! Pervert! ...What were you imagining, exactly? || Pervert! ...What were you picturing? || Hey! ...Tell me more, though! || Eek! ...Details?", "...That's indecent. Continue. || ...Indecent. Go on. || ...Shameless. Continue. || ...Inappropriate. Proceed.", "Imagining is free. Seeing costs extra. || Imagine all you like. Seeing costs extra. || Free to dream. Not to look. || Picture it. That's all you get. For now.");
                case 1: return T(t, "If we were alone... I-I don't know what I'd do... || If we were alone... I don't know what I'd do... || Alone... I'd probably melt... || If it were just us... um...", "Alone, huh? Then you'd better make it worth my time! || Alone? Make it worth it! || Just us? Better be good! || Alone, huh? Impress me!", "If we were alone, you'd find out. || Alone, you'd learn. || You'd find out. || If we were alone... you'd see.", "If we were alone, you'd already be on your knees. || Alone, you'd be kneeling by now. || Alone? You'd be mine already. || If we were alone, you'd beg.");
                default: return T(t, "My heart's beating so fast... you can't say that so close... || My heart... you're too close... || So close... my heart's racing... || D-don't say that so close...", "Ahaha! Ticklish! And, um... kinda hot. Don't tell anyone! || Ticklish! And... kinda hot! Shh! || Haha! Too close! ...I like it! || Eek! Tingly! Don't tell!", "...You're bold. I'll remember it. || ...Bold. Remembered. || ...Daring. Noted. || ...You're brave.", "Oh? Tell me more. In detail. || Tell me more. Slowly. || Go on. Details. || Mm. More.");
            }
        }

        public static string WhisperBad(Tone t)
        {
            return T(t, "Eek! I-I don't know you like that...! || Eek! I don't know you like that...! || W-what?! We're not that close...! || N-no! That's too much!", "Whoa, way too much, way too fast! Down, boy! || Way too fast! Down, boy! || Whoa! Too much! || Hey! Slow way down!", "Do that again and I'm reporting you. || Again, and I report you. || Unacceptable. || Repeat that and regret it.", "Cute. But you haven't earned the right to talk to me like that. || Cute. You haven't earned that. || Ambitious. Unearned. || Not yet, little boy.");
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
                return T(t, "Our first time... I was so nervous, but you were gentle. I'll treasure it forever. || Our first time... I'll treasure it forever. || I was so scared, but you were gentle... thank you. || I'll never forget it... our first time.",
                    "Ahh, don't bring it up out of nowhere! ...It was amazing, okay? Way better than I imagined! || Don't bring it up! ...It was amazing! || Ahh, our first time?! ...Best ever! || Embarrassing! ...But so good!",
                    "...I think about it more than I'd like to admit. || ...I think about it often. || ...More often than I'd admit. || ...It's on my mind. Regularly.",
                    "You were so nervous. It was adorable. Next time, I'm in charge. || You were so nervous. Adorable. Next time, I lead. || So shaky. Cute. I'm in charge next time. || You trembled. Next time, I drive.");
            switch (R(3))
            {
                case 0: return T(t, "Me too... I can't focus in class when I remember your voice... || Me too... I remember your voice in class... || I can't concentrate... I keep remembering... || Me too... I blush during lessons...", "Hehe, you mean when I couldn't stop making noise? So embarrassing! ...Let's do it again! || When I was so loud?! Embarrassing! Again! || Hehe, I was noisy! Let's repeat! || So loud! Oops! Again soon?", "You weren't bad. I want more. || You weren't bad. More. || Acceptable. I want more. || Good. Again.", "Incredible? I was holding back. Want to see what I can really do? || Incredible? I was holding back. || That was a warm-up. || You've seen nothing yet.");
                case 1: return T(t, "I still blush when I walk past... that place... || I blush passing that place... || That spot still makes me shy... || I can't walk by there without blushing...", "I still have a mark, you know! Right here. *She tugs her collar.* Your fault! || Still got a mark! Your fault! || Look! A mark! You did that! || The mark's still here! Hehe!", "...Same place, next time. I liked it there. || ...Same place next time. || ...That place. Again. || ...I liked that spot.", "Again? You're insatiable. ...Good. So am I. || Again? Insatiable. Good. || Already? Me too. || More? Greedy. I like it.");
                default: return T(t, "Whenever you want... I-I mean... soon, please... || Whenever... I mean... soon, please... || Anytime... soon would be nice... || Um... soon, okay...?", "Soon! Tomorrow! Today?! Okay, I'm too excited, sorry! || Soon! Today?! Too eager?! || Tomorrow! No, today! Hehe! || ASAP! Sorry, excited!", "When I say so. Which might be soon. || When I decide. Soon, maybe. || My call. Possibly soon. || When I say. Soon.", "Beg nicely and I'll think about it. || Beg nicely. || Ask sweetly and maybe. || Convince me.");
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
                case 0: return T(t, "...Okay. Lead the way. Just... be gentle, okay? || Okay... lead me. Gently, please. || Alright... I'll follow you. Be gentle. || Mm... take me there. Softly.", "Ooh, a secret mission! I'm in! Don't get us caught! || Secret mission! Don't get caught! || Sneak mode! I'm in! || Ooh! Adventure! Let's go!", "Fine. But if a professor finds us, it was your idea. || Fine. Your idea, if caught. || Alright. Your fault if anyone sees. || Very well. You take the blame.", "Finally. I was wondering how long you'd make me wait. || Finally. What took so long? || At last. Lead on. || About time.");
                default: return T(t, "M-my heart won't calm down... let's go before I lose my nerve... || My heart's racing... let's go before I chicken out... || Quick... before I lose my courage... || Let's go... my heart's pounding...", "Race you there! Loser has to... ehehe, you'll see! || Race you! Loser has to... you'll see! || Last one there pays! Hehe! || Go go go!", "...Somewhere with a lock on the door. || ...Somewhere with a lock. || ...A locked room. || ...Somewhere private. Lockable.", "Somewhere private? You read my mind. Hurry. || Private? You read my mind. || Hurry. I'm impatient. || Somewhere quiet. Fast.");
            }
        }

        public static string SneakNo(Tone t)
        {
            return T(t, "N-not today... I'm not ready for something like that... || Not today... I'm not ready... || I-I can't... not yet... || Sorry... not like this...", "Ehh? Sneak off? With you? Pfft, no way! ...Not yet, anyway. || Sneak off? Nope! Not yet! || Pfft, no! Maybe someday! || Ehh? Not today!", "No. Don't push your luck. || No. || Not happening. || Don't push.", "Sneak off with you? You wish. Try harder. || You wish. || Try harder. || In your dreams.");
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
