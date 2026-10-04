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
        private static int R(int n) { return Random.Range(0, n); }

        // ================= openings =================

        public static string Greeting(Tone t, int close, Mood m)
        {
            if (m == Mood.Upset) return T(t, "...Oh. Hi.", "Hey. ...Sorry, not my best day.", "What is it?", "Make it quick, okay?");
            if (close >= 2)
                switch (R(3))
                {
                    case 0: return T(t, "Ah, it's you! I was hoping you'd come find me.", "There you are! I was looking for you!", "You came. ...Good.", "Took you long enough. I missed you. A little.");
                    case 1: return T(t, "Hi... my day just got better.", "Hey you! Perfect timing!", "Hey. I saved some time for you.", "Oh? Coming to see your girlfriend? Good choice.");
                    default: return T(t, "I was just thinking about you. Really!", "Yay! Talk to me, talk to me!", "I'm glad you're here.", "Hmm~ what took you so long?");
                }
            if (close == 1)
                switch (R(3))
                {
                    case 0: return T(t, "Oh, hi! Do you have a minute?", "Yo! What's up?", "Hey. What's going on?", "Well, well. Look who it is.");
                    case 1: return T(t, "Hello! It's nice to see you.", "Heeey! Perfect, I was bored!", "Oh, it's you. Hi.", "Came to see me? Smart.");
                    default: return T(t, "Ah, hi! How are you doing?", "Hey hey! Got something fun for me?", "Hi. Need something?", "Hi there. Entertain me.");
                }
            switch (R(3))
            {
                case 0: return T(t, "Oh, um... hello.", "Oh, hey! Hi!", "...Yes?", "Hm? Can I help you?");
                case 1: return T(t, "H-hello. Did you need something?", "Hi! You're from the club, right?", "Hello.", "Oh, it's the club guy. Hi.");
                default: return T(t, "Ah, hi... nice weather today, isn't it?", "Hiya!", "Do you need something?", "You look like you want to say something.");
            }
        }

        /// <summary>Jealous because she is into you (the friendship kind is Jealous).</summary>
        public static string JealousLove(Tone t, string friend)
        {
            switch (R(2))
            {
                case 0: return T(t, "You and " + friend + " looked really happy together... I'm happy for you. I think.",
                    "Hey! Why is it always " + friend + "?! What does she have that I don't?!",
                    "You've been with " + friend + " a lot. I noticed. That's all I'll say.",
                    "So " + friend + " gets your attention now? I can be much more fun, you know~");
                default: return T(t, "I keep thinking about you and " + friend + "... Sorry, that's weird of me.",
                    "I saw you with " + friend + " again! I'm not jealous! ...Okay, a little!",
                    "If you like " + friend + ", just say it. Don't keep me guessing.",
                    "Careful. If you keep spending time with " + friend + ", I might have to steal you back~");
            }
        }

        public static string Jealous(Tone t, string friend)
        {
            switch (R(3))
            {
                case 0: return T(t, "You've been spending a lot of time with " + friend + " lately... it's fine. I just noticed, that's all.",
                    "Hey! You hang out with " + friend + " every day now! What about me?!",
                    "You and " + friend + " are always together lately. I'm not complaining. I'm observing.",
                    "Oh, you remembered me? I thought " + friend + " had adopted you.");
                case 1: return T(t, "Um... did I do something wrong? You only talk to " + friend + " these days.",
                    "It's always " + friend + ", " + friend + ", " + friend + "! I'm fun too, you know!",
                    "I'm glad you and " + friend + " get along. ...Don't forget the rest of us.",
                    "Is " + friend + " that interesting? Should I be worried?");
                default: return T(t, "It's a little lonely when you're always with " + friend + "...",
                    "No fair! " + friend + " gets you all to herself!",
                    "You've been busy. With " + friend + ", mostly.",
                    "Let me guess, " + friend + " was busy today so you came to me?");
            }
        }

        /// <summary>Her opening when she heard about something you did.</summary>
        public static string Incident(Tone t, string kind, string about, bool self, bool friend, string what = null, bool saw = false)
        {
            switch (kind)
            {
                case Social.Kinds.Helped:
                case Social.Kinds.Defended:
                    if (self) return T(t, "Thank you again for earlier. It really meant a lot.", "Hey! Thanks for helping me out, you were awesome!", "About earlier. ...Thanks.", "You were pretty cool earlier. Don't let it go to your head.");
                    return T(t, about + " told me you helped her. That was really kind of you.", "I heard you helped " + about + "! Nice going!",
                        "Word is you helped " + about + ". Not bad.", "So you helped " + about + "? Trying to win points with everyone?");
                case Social.Kinds.KeptPromise:
                    if (self) return T(t, "You really did what I asked. I won't forget that.", "You came through for me! You're the best!", "You kept your word. I respect that.", "You actually did it. I'm impressed. Slightly.");
                    return T(t, about + " says you can be counted on.", about + " won't stop saying how reliable you are!", "I hear you keep your promises. Rare.", about + " seems to think you're dependable. Is that true?");
                case Social.Kinds.GoodTalk:
                    return T(t, about + " said she had a nice talk with you.", about + " says you're super fun to talk to!", "I hear you're decent company.", "Apparently you're a good listener. Prove it.");
                case Social.Kinds.Rude:
                    if (self) return T(t, "...What you said earlier really hurt.", "Hey. That wasn't cool earlier.", "I haven't forgotten what you said.", "Oh, you're talking to me now? After that?");
                    if (friend) return T(t, about + " is my friend, you know. She was upset after talking to you.", "Why were you mean to " + about + "?! Not cool!",
                        "I heard how you treated " + about + ". Explain.", "So you're the one who made " + about + " cry? Charming.");
                    return T(t, "I heard you were kind of mean to " + about + "...", "People say you were rude to " + about + ". Is that true?", "Rumor says you were rude to " + about + ".", "I heard about you and " + about + ". Tsk.");
                case Social.Kinds.Ignored:
                    if (self) return T(t, "You walked right past me earlier... I guess you were busy.", "You totally ignored me earlier! Rude!", "You saw me struggling and kept walking.", "Nice of you to just walk by earlier.");
                    return T(t, about + " said you didn't stop to help her...", "You left " + about + " hanging?", "I heard you walked past " + about + ".", "Not very gallant with " + about + ", were you?");
                case Social.Kinds.BrokePromise:
                    if (self) return T(t, "You said you'd help... I waited.", "Hey! You forgot about me!", "You didn't do what you said you would.", "I asked you for one thing. One.");
                    return T(t, about + " was let down. You promised her something.", "Dude, you bailed on " + about + "!", "I heard you didn't keep your word to " + about + ".", "Breaking promises to " + about + "? Bad look.");
                case Social.Kinds.SeenGirlsRoom:
                    return T(t, "Um... people are saying you were in the girls' restroom...?", "Okay, WHY were you in the girls' restroom?!",
                        "Explain the girls' restroom thing.", "So. The girls' restroom. I'm listening.");
                case Social.Kinds.SeenLockers:
                    return T(t, "I heard someone saw you near the girls' changing room... that's not true, right?", "You were in the girls' changing room?! Are you crazy?!",
                        "The changing room. Care to explain?", "The girls' changing room, huh? Bold.");
                case Social.Kinds.HerBoysRoom:
                    return T(t, "A-about earlier... in the restroom... please don't tell anyone. I got lost, I swear!",
                        "Okay, listen! The boys' restroom thing! Total accident! You didn't see anything!",
                        "About the restroom. It was the closest one. We will never speak of it.",
                        "You saw nothing in that restroom. Got it? ...Good.");
                case Social.Kinds.Flirted:
                    if (self && what == Witness.Kiss) return T(t, "That kiss... I keep thinking about it.", "Okay, so. The kiss. That happened!", "About the kiss. ...It wasn't bad.", "Thinking about that kiss? Me too~");
                    if (self && what == Witness.Hand) return T(t, "Your hand was really warm earlier...", "We held hands! Like, actually!", "Holding hands. ...It was fine.", "Still feel my hand in yours? Hehe.");
                    if (self) return T(t, "What you said yesterday... I keep thinking about it.", "You were being kinda smooth earlier, huh?", "About earlier... I didn't hate it.", "Still trying to charm me? Keep trying.");
                    return Seen(t, what, about, saw);
                default:
                    return Greeting(t, 1, Mood.None);
            }
        }

        /// <summary>
        /// You and another girl, from a girl who saw it herself or only heard about it; what it was sets the words (9th
        /// </summary>
        public static string Seen(Tone t, string what, string about, bool saw)
        {
            switch (what)
            {
                case Witness.Kiss:
                    return saw ? T(t, "I... saw you kiss " + about + ". I didn't mean to look.", "I SAW that! You and " + about + ", kissing! Right there!",
                                   "I saw you kiss " + about + ". Just so you know.", "Kissing " + about + " in front of everyone? I had a front-row seat.")
                               : T(t, "People are saying you kissed " + about + "... is that true?", "Is it true you kissed " + about + "?! Everyone's talking!",
                                   "I heard you kissed " + about + ". Rumor or fact?", "So the rumor is you kissed " + about + ". Do tell.");
                case Witness.Hand:
                    return saw ? T(t, "I saw you holding hands with " + about + "... you looked happy.", "I saw you two holding hands! You and " + about + "!",
                                   "You and " + about + ", hand in hand. I saw.", "Holding hands with " + about + " where anyone can see? Bold.")
                               : T(t, "Someone said you were holding hands with " + about + "...", "I heard you and " + about + " were holding hands!",
                                   "Word is you held hands with " + about + ".", "Hand in hand with " + about + ", I hear. Cute.");
                case Witness.Touch:
                    return saw ? T(t, "I saw where your hands were with " + about + "... w-what was that?", "Hey! I saw you getting all touchy with " + about + "!",
                                   "I saw you with " + about + ". Your hands were busy.", "Getting handsy with " + about + "? I saw everything~")
                               : T(t, "People say you were touching " + about + "... that's not true, right?", "Is it true you were all over " + about + "?!",
                                   "I hear you got handsy with " + about + ".", "Rumor has it your hands wandered with " + about + ". Hm~");
                case Witness.H:
                    return saw ? T(t, "I... I saw you and " + about + ". I can't unsee it.", "I SAW you and " + about + "! Out in the open?! Seriously?!",
                                   "I saw you and " + about + ". All of it.", "You and " + about + ", out where anyone could walk by? I did walk by.")
                               : T(t, "There's a rumor about you and " + about + "... the kind I can't even say.", "Everyone's whispering about you and " + about + "! Is it true?!",
                                   "The rumor about you and " + about + ". Tell me it's a rumor.", "You and " + about + ", huh? The whole school is blushing.");
                default:
                    return saw ? T(t, "I saw you being really sweet to " + about + "... she was all red.", "I saw you sweet-talking " + about + "! Smooth!",
                                   "I saw you charming " + about + ". It was something.", "I caught you flirting with " + about + ". Not subtle.")
                               : T(t, "I heard you were being really sweet to " + about + "...", "So, you and " + about + ", huh? Spill!",
                                   "I hear you've been charming " + about + ".", "Flirting with " + about + "? Interesting.");
            }
        }

        /// <summary>Your girlfriend heard you were flirting with someone else (or saw something more).</summary>
        public static string Cheating(Tone t, string other, bool minds, string what = null, bool saw = false)
        {
            if (minds && saw && (what == Witness.Kiss || what == Witness.H || what == Witness.Touch))
                return T(t, "I saw you... with " + other + ". I saw it. How could you...?",
                    "I SAW YOU! With " + other + "! Don't you dare say it's nothing!",
                    "I saw you with " + other + ". Don't insult me with excuses.",
                    "I had a lovely view of you and " + other + ". Explain. Slowly.");
            if (!minds && saw && what == Witness.Kiss)
                return T(t, "I saw you kiss " + other + "... I trust you. I just... want to hear it from you.",
                    "I saw you kiss " + other + "! Ugh, fine, as long as I'm still number one!",
                    "I saw the kiss with " + other + ". I'm not angry. I'm noting it.",
                    "Kissing " + other + " where I can see? You do like living dangerously~");
            if (!minds)
                return T(t, "People say you were sweet-talking " + other + "... I trust you, okay?",
                    "Heard you were flirting with " + other + "! Hah, as long as you come back to me!",
                    "I hear things about you and " + other + ". I'm not the jealous type. Mostly.",
                    "You and " + other + "? Cute. Just remember who you belong to~");
            return T(t, "Is it true...? You and " + other + "...? I thought we were...",
                "Hey! What's this about you and " + other + "?! I'm your girlfriend!",
                "Explain " + other + ". Now.",
                "Flirting with " + other + " behind my back? You've got some nerve.");
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

        public static string Vent(Tone t, Mood m)
        {
            switch (m)
            {
                case Mood.Stressed:
                    return T(t, "It's the exams... Everyone expects so much and I don't want to disappoint anyone. Sorry, I'm just dumping this on you.",
                        "Everything's piling up! Homework, club, my mom... I just wanna scream into a pillow!",
                        "I have three deadlines this week and the teacher added a fourth. I'm handling it. I just needed to say it out loud.",
                        "Everyone wants something from me today. You're the first person who just... listened.");
                case Mood.Upset:
                    return T(t, "I had a fight with a friend... I said something I didn't mean. Thank you for listening.",
                        "Someone said something really mean about me and everyone laughed. Ugh. Thanks for not laughing.",
                        "A bad day. Things didn't go how I planned. ...Thanks for hearing me out.",
                        "Fine, you want to know? Someone stood me up. There. Happy? ...Thanks, I guess.");
                case Mood.Lonely:
                    return T(t, "Sometimes it feels like nobody would notice if I weren't here... Sorry. That's a gloomy thing to say.",
                        "Everyone's got plans except me lately! It's kinda sad, right? Hehe...",
                        "My friends have been busy. It's quieter than I'd like.",
                        "Don't laugh, but I've been eating lunch alone all week.");
                default:
                    return T(t, "I'm just tired lately. Thank you for listening.", "Ugh, today was SO long!", "It's been a long week.", "Life's being annoying. That's all.");
            }
        }

        public static string VentThanks(Tone t)
        {
            return T(t, "...I feel lighter now. Thank you.", "Okay! I feel way better! You're like a human stress ball!", "That helped. Don't tell anyone I said that.", "You're surprisingly good at this.");
        }

        public static string Anecdote(Tone t)
        {
            switch (R(6))
            {
                case 0: return T(t, "This morning a little bird landed on my desk by the window. I didn't dare move all through first period.",
                    "I tried to jump the school fence this morning because I was late. Spoiler: the fence won.",
                    "The vending machine gave me two drinks for the price of one today. I've decided it's a sign.",
                    "A first-year asked me for directions and I sent him to the staff room on purpose. He'll thank me later.");
                case 1: return T(t, "My little sister made me a bento with a face on it. I couldn't bring myself to eat the eyes.",
                    "I beat my high score in that rhythm game! My fingers still hurt!",
                    "I finished a thousand-piece puzzle yesterday. One piece was missing. I'm still angry.",
                    "I convinced the cafeteria lady to give me extra karaage. Charm is a skill.");
                case 2: return T(t, "I found a really good bookstore near the station. It smells like old paper. I could live there.",
                    "There's a new crepe stand near the park! I've already tried four flavors!",
                    "I started learning chess. The club president beat me in six moves. Six.",
                    "I saw a cat wearing a tiny sweater on the way here. My day peaked at 7 a.m.");
                case 3: return T(t, "I've been practicing calligraphy. My characters still look like they're running away.",
                    "Our club's practice match is coming up and I've been training every morning!",
                    "I reorganized my whole room by color. It's very satisfying. Don't judge.",
                    "I've been writing a story. No, you can't read it. ...Maybe later.");
                case 4: return T(t, "I fell asleep on the train and woke up three stations late. I had to run all the way here...",
                    "My phone fell in the pool yesterday. I'm living like a caveman now!",
                    "I tried cooking dinner for my family. They were very... polite.",
                    "I dyed a streak of my hair last weekend. Nobody noticed. Do you?");
                default: return T(t, "The cherry tree behind the gym is starting to bud. I go look at it every morning.",
                    "I signed up to help at the school festival! It's gonna be huge!",
                    "The library got the new volume of the series I like. I reserved it the second I saw it.",
                    "I've been people-watching in the courtyard. You'd be amazed what you learn.");
            }
        }

        public static string GossipLine(Tone t, string kind, string about, string what = null)
        {
            switch (kind)
            {
                case Social.Kinds.Helped:
                case Social.Kinds.Defended:
                    return T(t, "Oh, " + about + " told everyone you helped her. She seemed really happy.", "Did you know " + about + " has been bragging that you helped her?",
                        about + " mentioned you helped her. People noticed.", about + " won't shut up about how you helped her. It's adorable.");
                case Social.Kinds.Rude:
                    return T(t, "Um... " + about + " seemed hurt by something you said.", "Heads up: " + about + " is mad at you!", about + " is not happy with you. Just so you know.", "You know " + about + " is telling everyone you were rude, right?");
                case Social.Kinds.BrokePromise:
                    return T(t, about + " said you forgot something you promised her...", about + " says you bailed on her!", about + " says you didn't keep your word.", "Rumor has it you stood up " + about + ".");
                case Social.Kinds.KeptPromise:
                    return T(t, about + " says she can always count on you.", about + " says you're super reliable!", "Apparently you keep your promises. " + about + " says so.", about + " thinks you're dependable. Don't disappoint her.");
                case Social.Kinds.GoodTalk:
                    return T(t, about + " said talking with you was fun.", about + " thinks you're a great listener!", about + " enjoyed your chat, I hear.", about + " seems to like talking with you. Should I be jealous?");
                case Social.Kinds.Flirted:
                    if (what == Witness.Kiss) return T(t, "People say you kissed " + about + "... is that true?", "Everyone's saying you kissed " + about + "!", "There's talk you kissed " + about + ".", "You kissed " + about + "? The whole class knows.");
                    if (what == Witness.Hand) return T(t, "Someone saw you holding hands with " + about + "...", "You and " + about + ", holding hands! Everyone saw!", "Word is you held hands with " + about + ".", "Hand in hand with " + about + "? People noticed.");
                    if (what == Witness.Touch) return T(t, "People say you were... touching " + about + "...", "Dude, people say you were all over " + about + "!", "There's talk about your hands and " + about + ".", "Your hands and " + about + ". That's the rumor.");
                    return T(t, "People say you and " + about + " are getting close...", "Everyone's talking about you and " + about + "!", "There's talk about you and " + about + ".", "You and " + about + ", huh? The whole class knows.");
                case Social.Kinds.SeenGirlsRoom:
                case Social.Kinds.SeenLockers:
                    return T(t, "There's a strange rumor going around about you... something about the girls' rooms.", "Dude, there's a weird rumor about you and the girls' rooms!", "There's an unflattering rumor about you going around.", "You're famous now. For the wrong reasons.");
                default:
                    return Anecdote(t);
            }
        }

        public static string NothingNew(Tone t)
        {
            return T(t, "Hmm... I can't think of anything new, sorry.", "Ehh, nothing's happening! Boring day!", "Nothing worth mentioning.", "You've heard all my stories today.");
        }

        // ================= ask about =================

        public static string AskFriend(string friend) { return P("How's " + friend + " doing?", "You're close with " + friend + ", right? How is she?", "Have you talked to " + friend + " lately?"); }

        public static string FriendMood(Tone t, string friend, Mood m)
        {
            switch (m)
            {
                case Mood.Stressed: return T(t, friend + " looks so stressed about tests... maybe she'd like to study with someone.", friend + "'s freaking out about homework!", friend + " is overwhelmed. Studying might help her.", friend + " is a wreck about exams. Go play hero.");
                case Mood.Hungry: return T(t, friend + " forgot her lunch today, I think.", friend + " was starving this morning!", friend + " skipped breakfast again.", friend + " is hungry. Feed her and she's yours.");
                case Mood.Lonely: return T(t, friend + " has seemed a little lonely lately... maybe you could talk to her?", friend + " looks kinda down! Go cheer her up!", friend + " could use company.", friend + " is moping. You could fix that.");
                case Mood.Upset: return T(t, "Something happened to " + friend + "... she's upset. Be gentle with her.", friend + " is in a bad mood! Careful!", friend + " is upset about something.", "Don't poke " + friend + " today. Trust me.");
                case Mood.Cheerful: return T(t, friend + " is in a great mood today.", friend + " is bouncing off the walls today!", friend + " seems happy.", friend + " is unbearably cheerful today.");
                case Mood.Bored: return T(t, friend + " said she's bored.", friend + " is SO bored, she said so ten times!", friend + " is bored out of her mind.", friend + " is bored. Entertain her.");
                case Mood.Sleepy: return T(t, friend + " nearly fell asleep in class.", friend + " was snoring in math!", friend + " looks exhausted.", friend + " was drooling on her notes. Cute.");
                case Mood.Flirty: return T(t, friend + " seems to be in a really good mood today... maybe there's someone she likes?", friend + " keeps smiling at her phone!", friend + " is acting strangely happy.", friend + "'s got that look today. You know the one.");
                default: return T(t, friend + "'s doing fine, I think.", friend + "'s good! Same as always!", "She's fine.", friend + "? Same as ever.");
            }
        }

        public static string FriendBond(Tone t, string friend)
        {
            return T(t, friend + " and I have been friends for a long time. She's really important to me.", friend + "'s my best friend! We do everything together!",
                friend + " and I understand each other. That's rare.", friend + "? She's tolerable. ...She's my best friend, okay?");
        }

        public static string Rival(Tone t, string friend)
        {
            return T(t, "Um... " + friend + " and I don't really get along...", "Ugh, why ask ME about " + friend + "?!", "I don't keep track of " + friend + ".", "Why are you asking about HER?");
        }

        public static string FriendProblem(Tone t, string friend, string problem)
        {
            return T(t, "Actually... " + friend + " has been worried. " + problem + " Maybe you could help her?",
                "Oh! " + friend + " has a problem! " + problem + " You should help her!",
                friend + " won't ask anyone, but: " + problem + " Just so you know.",
                "Want a tip? " + problem + " " + friend + " would never admit it, though.");
        }

        public static string AskSchool() { return P("Anything going on around school?", "What's everyone talking about lately?", "Heard any news?"); }

        public static string Reputation(Tone t, int rep)
        {
            if (rep >= 70) return T(t, "Everyone says you're really kind. I think so too.", "You're kinda famous! Everyone likes you!", "You have a good reputation. Don't waste it.", "You're popular, you know. It's a little annoying.");
            if (rep >= 40) return T(t, "People think you're nice, I think.", "People like you! Mostly!", "People don't mind you.", "You're... known. In a good way. Mostly.");
            if (rep >= 20) return T(t, "Um, not everyone knows you yet.", "Half the class doesn't know your name! Haha!", "You're not very well known.", "You're a bit of a mystery. Not the cool kind.");
            return T(t, "Some people say... strange things about you. I don't believe them!", "People are saying weird stuff about you, dude.", "Your reputation is poor. Work on it.", "Your reputation is in the gutter. Impressive, really.");
        }

        // ================= joke / tease =================

        public static string PlayerJoke()
        {
            return P("Why did the student eat his homework? The teacher said it was a piece of cake.",
                "I tried to catch fog this morning. I mist.",
                "Our math teacher is so strict even the numbers stand in line.",
                "I'd tell you a chemistry joke, but I know I wouldn't get a reaction.",
                "What do you call a sleeping bull? A bulldozer.",
                "I'm reading a book about anti-gravity. I can't put it down.");
        }

        public static string JokeGood(Tone t)
        {
            switch (R(3))
            {
                case 0: return T(t, "Pfft... ahaha! That's so silly!", "AHAHA! Stop, stop, my stomach!", "...Heh. Okay, that one was good.", "Ugh, that's terrible. *snort* Dammit.");
                case 1: return T(t, "Hehe... you're funny.", "Hahaha! Tell me another!", "I didn't expect to laugh. Well done.", "Fine. I laughed. Happy?");
                default: return T(t, "Ahaha... I'm going to think about that all day.", "Pfffft! You're the best!", "Heh. Not bad at all.", "Okay, okay, you win this round.");
            }
        }

        public static string JokeBad(Tone t)
        {
            switch (R(2))
            {
                case 0: return T(t, "Um... hehe...? Sorry, I don't think I got it.", "...Was that the joke?", "...", "Wow. That was painful. For both of us.");
                default: return T(t, "Oh... um, that's... nice.", "Booo! Get off the stage!", "I'll pretend you didn't say that.", "Did that sound funny in your head?");
            }
        }

        public static string JokeHurt(Tone t)
        {
            return T(t, "That's... kind of mean, isn't it?", "Hey! Not funny!", "That crossed a line.", "Careful. That one stung.");
        }

        // ================= compliments =================

        public static string PlayerCompliment(string kind, string club)
        {
            switch (kind)
            {
                case "effort": return P("You've been working really hard lately. It shows.", "I noticed how much effort you put in. That's amazing.");
                case "club": return P("I heard you're doing great at " + club + ".", "You're really good at your club stuff, you know?");
                case "smart": return P("You always explain things so clearly.", "You're really sharp, you know that?");
                default: return P("You look really nice today.", "Your hair looks great like that.", "That smile of yours is pretty great.");
            }
        }

        public static string ComplimentGood(Tone t, string kind)
        {
            if (kind == "looks")
                return T(t, "Eh?! Y-you think so...? Thank you...", "Hehe, really?! You noticed!", "...You're not bad yourself. Thanks.", "Oh? Took you long enough to notice.");
            return T(t, "R-really? That makes me really happy...", "Yay! Somebody noticed!", "...Thank you. I appreciate that.", "Finally, someone with taste.");
        }

        public static string ComplimentFlat(Tone t)
        {
            return T(t, "Oh... um, thank you, I guess.", "Haha, thanks? Random, but okay!", "Okay.", "Flattery won't get you far with me.");
        }

        public static string ComplimentRepeat(Tone t)
        {
            return T(t, "You said that yesterday too...", "You already told me! Got a new one?", "You're repeating yourself.", "Running out of material?");
        }

        // ================= romance =================

        public static string FlirtGood(Tone t, int close)
        {
            if (close >= 2)
                switch (R(2))
                {
                    case 0: return T(t, "M-me too... I'm so happy you said that.", "Aww! You're making me blush! Me too, okay?!", "...Me too. Don't make me say it twice.", "Hmph. Smooth talker. ...I feel the same.");
                    default: return T(t, "You always know what to say... I love that about you.", "Ehehe! I'm the luckiest girl in school!", "I like this. Us.", "Keep talking like that and I might fall for you all over again.");
                }
            switch (R(2))
            {
                case 0: return T(t, "Eh...? I-I like spending time with you too...", "Wh-whoa, where did that come from?! ...Thanks, hehe.", "...You're making me lose my composure.", "Oh my. Are you trying to make me blush?");
                default: return T(t, "That's... a really nice thing to say.", "Haha, you're so straightforward! I like it!", "...I don't dislike hearing that.", "Careful. I might start believing you.");
            }
        }

        public static string FlirtTooSoon(Tone t)
        {
            return T(t, "Um... we don't know each other that well yet...", "Whoa, slow down there, buddy!", "...That's a bit much.", "Nice try. You'll have to work harder than that.");
        }

        public static string LoveTalk(Tone t, int close)
        {
            if (close >= 2) return T(t, "Being your girlfriend... I still can't believe it sometimes.", "Everyone's jealous of us, you know! Hehe!", "I've been thinking about where we could go on our next date.", "So, what's the plan for our next date? It better be good.");
            return T(t, "Do you... have someone you like?", "So! Do you have a crush on anyone? Tell me!", "Have you ever been in love?", "Who do you like? Come on, I won't tell.");
        }

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

        public static string ExplainOk(Tone t)
        {
            return T(t, "Oh... I see. I'm sorry I doubted you.", "Ohhh! Okay, that makes sense! My bad!", "...Fine. That's reasonable.", "Hmph. Fine. I'll let it slide. This time.");
        }

        public static string ExplainFail(Tone t)
        {
            return T(t, "I... I don't know if I believe that.", "Uh-huh. Sure. Totally.", "That's not convincing.", "Do you hear yourself?");
        }

        public static string PlayerApologize() { return P("I'm sorry. Really.", "I messed up. I'm sorry.", "You're right. I'm sorry about that."); }

        public static string ApologyAccepted(Tone t)
        {
            return T(t, "...Okay. Thank you for saying that.", "Okay, okay! Apology accepted! Just don't do it again!", "Fine. Apology accepted.", "Hmph. You owe me one.");
        }

        public static string PlayerMakePlans(string friend) { return P("You're right. Let's hang out more, just us.", "Sorry. I'd like to spend more time with you too."); }

        public static string PlansAccepted(Tone t)
        {
            return T(t, "Really? Then... I'll be looking forward to it.", "Promise?! Okay! Lunch together sometime!", "...Okay. I'll hold you to that.", "You'd better mean it.");
        }

        public static string KeepSecret(Tone t, bool promise)
        {
            if (promise) return T(t, "Thank you... I really owe you one.", "You're a lifesaver! Our secret, okay?!", "Good. ...Thanks.", "Smart choice. I'll remember this.");
            return T(t, "You're so mean...!", "Noooo! You wouldn't!", "Not funny.", "Try it and see what happens.");
        }

        // ================= ending =================

        public static string OutOfPatience(Tone t)
        {
            return T(t, "Um, sorry, I should get going soon...", "Ah, I gotta run! Later!", "I need to go.", "That's enough for now. I have things to do.");
        }

        public static string Bye(Tone t, int interest)
        {
            if (interest >= 4) return T(t, "That was really fun. Thank you for talking with me.", "That was awesome! Let's do this again!", "...That was a good talk.", "Not bad. You're more interesting than you look.");
            if (interest >= 2) return T(t, "Thanks for chatting with me.", "Fun chat!", "That was fine.", "Okay, that wasn't terrible.");
            return T(t, "Um... see you.", "Ehh, I'm kinda tired of talking.", "Bye.", "Well. That happened.");
        }

        public static string TooTired(Tone t)
        {
            return T(t, "Sorry, I don't really have time to talk right now...", "Ugh, can we talk later? I'm beat!", "Not now.", "I've talked enough today. Come back tomorrow.");
        }
    }
}
