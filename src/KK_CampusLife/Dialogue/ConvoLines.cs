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
            if (m == Mood.Upset) return T(t, "...Oh. Hi. || ...Oh. Hello. || ...Hi... || ...Ah. You.", "Hey. ...Sorry, not my best day. || Hey. ...Rough day, sorry. || Hi. ...Not feeling great. || Oh, hey. ...Ugh, sorry.", "What is it? || Yes? || What? || Something?", "Make it quick, okay? || Quickly, please. || Short version, okay? || Keep it brief.");
            if (close >= 2)
                switch (R(3))
                {
                    case 0: return T(t, "Ah, it's you! I was hoping you'd come find me. || Oh, it's you! I hoped you'd find me. || You came! I was waiting... || Ah, I'm so glad you're here!", "There you are! I was looking for you! || Found you! I was searching everywhere! || Finally! Where were you?! || There! I've been looking!", "You came. ...Good. || You're here. ...Good. || Oh. You found me. Good. || ...You came. Fine.", "Took you long enough. I missed you. A little. || Finally. I missed you. Slightly. || About time. I missed you. Maybe. || Late. ...I missed you a bit.");
                    case 1: return T(t, "Hi... my day just got better. || Hi... now my day is better. || Hello... you made today brighter. || Oh... hi. I'm so happy.", "Hey you! Perfect timing! || Hey! Great timing! || You! Perfect! || Hiii! Just in time!", "Hey. I saved some time for you. || Hey. I made time for you. || Hi. I kept a slot free. || Hello. I was free. For you.", "Oh? Coming to see your girlfriend? Good choice. || Visiting your girlfriend? Wise. || Ah, coming to see me? Good boy. || Here for your girlfriend? Correct.");
                    default: return T(t, "I was just thinking about you. Really! || I was thinking about you! Honestly! || You were on my mind... || I was hoping to see you...", "Yay! Talk to me, talk to me! || Talk to me! Now! || Yay! Chat time! || Tell me everything!", "I'm glad you're here. || I'm glad you came. || Good. You're here. || Glad you're here.", "Hmm~ what took you so long? || Hmm~ you're late. || What kept you? || So slow~");
                }
            if (close == 1)
                switch (R(3))
                {
                    case 0: return T(t, "Oh, hi! Do you have a minute? || Oh, hi! Got a minute? || Hello! Are you free? || Ah, hi! Can we talk?", "Yo! What's up? || Yo! What's new? || Hey! What's happening? || Sup!", "Hey. What's going on? || Hey. What's up? || Hi. Something new? || What's going on?", "Well, well. Look who it is. || Look who's here. || Well, hello. || Oh, it's you.");
                    case 1: return T(t, "Hello! It's nice to see you. || Hello! Good to see you. || Hi! I'm glad you came. || Ah, hello! Nice to see you.", "Heeey! Perfect, I was bored! || Heeey! I was so bored! || Hi! Save me from boredom! || Yay! Someone fun!", "Oh, it's you. Hi. || Oh. Hi. || Hello. || It's you. Hello.", "Came to see me? Smart. || Came to see me? Good call. || Visiting me? Smart. || Clever, coming here.");
                    default: return T(t, "Ah, hi! How are you doing? || Hi! How are you? || Ah, hello! Doing well? || Hi! How's your day?", "Hey hey! Got something fun for me? || Hey! Got something fun? || Hey! Any news? || Hi hi! Fun stuff?", "Hi. Need something? || Hi. Something you need? || Hello. Yes? || Need anything?", "Hi there. Entertain me. || Hi. Amuse me. || Hello. Impress me. || Entertain me, please.");
                }
            switch (R(3))
            {
                case 0: return T(t, "Oh, um... hello. || Oh... um, hi. || Ah... hello. || Um... hi there.", "Oh, hey! Hi! || Oh! Hi! || Hey there! || Oh, hiya!", "...Yes? || ...Yes? || ...Hello? || ...What?", "Hm? Can I help you? || Hm? Yes? || Can I help? || Need something?");
                case 1: return T(t, "H-hello. Did you need something? || H-hi. Need something? || Hello... did you want me? || Um, hello. Yes?", "Hi! You're from the club, right? || Hey! Club guy, right? || Hi! From the club! || Oh! You're in the club!", "Hello. || Hello. || Hi. || ...Hello.", "Oh, it's the club guy. Hi. || Ah, club guy. Hi. || The club boy. Hello. || Oh, club member. Hi.");
                default: return T(t, "Ah, hi... nice weather today, isn't it? || Hi... nice day, right? || Ah, hello... pretty weather. || Um... nice day today.", "Hiya! || Hiya! || Hey hey! || Yo!", "Do you need something? || Need something? || Something? || Yes?", "You look like you want to say something. || You want to say something. || Spit it out. || Something on your mind?");
            }
        }

        /// <summary>Jealous because she is into you (the friendship kind is Jealous).</summary>
        public static string JealousLove(Tone t, string friend)
        {
            switch (R(2))
            {
                case 0: return T(t, "You and " + friend + " looked really happy together... I'm happy for you. I think. || You and " + friend + " looked so happy... I'm glad. I think. || You seem close to " + friend + "... that's nice. Probably." + " || " + friend + " makes you smile... I'm happy for you. Mostly.",
                    "Hey! Why is it always " + friend + "?! What does she have that I don't?! || Why " + friend + "?! What's she got that I don't?!" + " || " + friend + " again?! Not fair! || Always " + friend + "! What about me?!",
                    "You've been with " + friend + " a lot. I noticed. That's all I'll say. || You and " + friend + ". A lot lately. Noted." + " || " + friend + ", often. I see. || I've noticed you and " + friend + ". That's all.",
                    "So " + friend + " gets your attention now? I can be much more fun, you know~" + " || " + friend + " has you now? I'm more fun~ || Choosing " + friend + "? I'm better~" + " || " + friend + "? I'm way more entertaining~");
                default: return T(t, "I keep thinking about you and " + friend + "... Sorry, that's weird of me. || I keep thinking about you and " + friend + "... sorry. || You and " + friend + "... it's on my mind. Sorry. || I can't stop thinking about you two... sorry.",
                    "I saw you with " + friend + " again! I'm not jealous! ...Okay, a little! || You and " + friend + " again! Not jealous! ...A bit!" + " || " + friend + " again?! I'm fine! ...Kinda not! || Saw you with " + friend + "! Totally fine! ...No!",
                    "If you like " + friend + ", just say it. Don't keep me guessing. || If it's " + friend + ", say so. || Just tell me if it's " + friend + ". || Be honest about " + friend + ".",
                    "Careful. If you keep spending time with " + friend + ", I might have to steal you back~ || Keep it up with " + friend + " and I'll steal you back~ || Careful. I'll take you from " + friend + "~" + " || " + friend + " can't keep you forever~");
            }
        }

        public static string Jealous(Tone t, string friend)
        {
            switch (R(3))
            {
                case 0: return T(t, "You've been spending a lot of time with " + friend + " lately... it's fine. I just noticed, that's all. || You're with " + friend + " a lot lately... it's okay. I just noticed. || So much time with " + friend + "... it's fine, really. || I see you with " + friend + " a lot... that's all.",
                    "Hey! You hang out with " + friend + " every day now! What about me?!" + " || " + friend + " every day?! What about me?! || Hey! Me too! Not just " + friend + "!" + " || " + friend + " again?! Hang with me!",
                    "You and " + friend + " are always together lately. I'm not complaining. I'm observing. || You and " + friend + ", constantly. Just observing. || Always with " + friend + ". An observation." + " || " + friend + ", daily. Noted.",
                    "Oh, you remembered me? I thought " + friend + " had adopted you. || Remembered me? I thought " + friend + " kept you. || Oh, you escaped " + friend + "?" + " || " + friend + " let you off the leash?");
                case 1: return T(t, "Um... did I do something wrong? You only talk to " + friend + " these days. || Did I do something wrong? It's always " + friend + "... || Am I boring? You only talk to " + friend + "... || Did I upset you? It's just " + friend + " now...",
                    "It's always " + friend + ", " + friend + ", " + friend + "! I'm fun too, you know!" + " || " + friend + ", " + friend + "! I'm fun too! || Always " + friend + "! Pick me sometimes! || Enough " + friend + "! My turn!",
                    "I'm glad you and " + friend + " get along. ...Don't forget the rest of us. || Good that you and " + friend + " get along. Remember others." + " || " + friend + " is fine. Don't forget us. || Don't forget the rest of us.",
                    "Is " + friend + " that interesting? Should I be worried? || Is " + friend + " that great? Should I worry?" + " || " + friend + ", huh? Am I being replaced? || What's so special about " + friend + "?");
                default: return T(t, "It's a little lonely when you're always with " + friend + "... || It's lonely when you're with " + friend + "... || I miss you when you're with " + friend + "... || It gets quiet without you...",
                    "No fair! " + friend + " gets you all to herself!" + " || " + friend + " hogs you! No fair!" + " || " + friend + " gets you all day! Unfair! || Share! " + friend + " keeps you!",
                    "You've been busy. With " + friend + ", mostly. || Busy. With " + friend + ". || You've been with " + friend + "." + " || " + friend + " has your time.",
                    "Let me guess, " + friend + " was busy today so you came to me?" + " || " + friend + " was busy, so me? || Second choice after " + friend + "? || Oh, " + friend + " wasn't free?");
            }
        }

        /// <summary>Her opening when she heard about something you did.</summary>
        public static string Incident(Tone t, string kind, string about, bool self, bool friend, string what = null, bool saw = false)
        {
            switch (kind)
            {
                case Social.Kinds.Helped:
                case Social.Kinds.Defended:
                    if (self) return T(t, "Thank you again for earlier. It really meant a lot. || Thank you for earlier. It meant so much. || About earlier... thank you, really. || I'm still grateful for earlier.", "Hey! Thanks for helping me out, you were awesome! || Thanks for earlier! You rocked! || You were amazing earlier! Thanks! || Earlier? Awesome! Thanks!", "About earlier. ...Thanks. || Earlier. ...Thank you. || About earlier. Thanks. || ...Thanks for earlier.", "You were pretty cool earlier. Don't let it go to your head. || Pretty cool earlier. Don't gloat. || You did well earlier. Stay humble. || Earlier? Not bad. Don't brag.");
                    return T(t, about + " told me you helped her. That was really kind of you." + " || " + about + " said you helped her. So kind. || I heard you helped " + about + "... that's sweet." + " || " + about + " told me what you did. Thank you.", "I heard you helped " + about + "! Nice going! || You helped " + about + "! Awesome!" + " || " + about + " says you saved her! Nice! || Way to help " + about + "!",
                        "Word is you helped " + about + ". Not bad. || You helped " + about + ". Good." + " || " + about + " mentioned your help. || I heard about " + about + ". Decent.", "So you helped " + about + "? Trying to win points with everyone? || Helping " + about + "? Collecting points? || Charming " + about + " too? || Playing hero for " + about + "?");
                case Social.Kinds.KeptPromise:
                    if (self) return T(t, "You really did what I asked. I won't forget that. || You did what I asked. I'll remember. || You really helped me. I won't forget. || You kept your word... thank you.", "You came through for me! You're the best! || You came through! Best! || You did it! Amazing! || You delivered! Yay!", "You kept your word. I respect that. || You kept your word. Respect. || Promise kept. Good. || Reliable. I respect it.", "You actually did it. I'm impressed. Slightly. || You did it. Mildly impressive. || Actually done? Impressive. || Wow. You delivered.");
                    return T(t, about + " says you can be counted on." + " || " + about + " says you're reliable." + " || " + about + " trusts you a lot." + " || " + about + " said you always help.", about + " won't stop saying how reliable you are!" + " || " + about + " keeps praising you!" + " || " + about + " says you're super reliable!" + " || " + about + " won't shut up about you!", "I hear you keep your promises. Rare. || You keep promises. Rare. || Reliable, they say. || I hear you're dependable.", about + " seems to think you're dependable. Is that true? || Dependable, says " + about + ". True?" + " || " + about + " vouches for you. Hm." + " || " + about + " says reliable. Prove it.");
                case Social.Kinds.GoodTalk:
                    return T(t, about + " said she had a nice talk with you." + " || " + about + " enjoyed talking to you." + " || " + about + " said you're nice to talk to." + " || " + about + " liked your chat.", about + " says you're super fun to talk to!" + " || " + about + " says you're fun!" + " || " + about + " loved chatting with you! || You're fun, says " + about + "!", "I hear you're decent company. || You're decent company, I hear. || Good conversation, they say. || I hear you talk well.", "Apparently you're a good listener. Prove it. || A good listener? Show me. || Good listener, huh? Prove it. || Listen to me, then.");
                case Social.Kinds.Rude:
                    if (self) return T(t, "...What you said earlier really hurt. || ...What you said hurt. || ...Your words earlier hurt me. || ...I'm still hurt.", "Hey. That wasn't cool earlier. || That was not cool earlier. || Hey. Earlier? Rude. || Not okay, earlier.", "I haven't forgotten what you said. || I remember what you said. || I haven't forgotten. || Your words. I recall them.", "Oh, you're talking to me now? After that? || Talking to me now? After that? || Oh, now you're nice? || You've got nerve.");
                    if (friend) return T(t, about + " is my friend, you know. She was upset after talking to you." + " || " + about + " is my friend. She was sad after you. || You upset " + about + "... she's my friend." + " || " + about + " cried... because of you.", "Why were you mean to " + about + "?! Not cool! || Why'd you hurt " + about + "?! || You were mean to " + about + "! Why?! || Not cool with " + about + "!",
                        "I heard how you treated " + about + ". Explain. || How you treated " + about + ". Explain. || About " + about + ". Explain yourself." + " || " + about + ". Explain.", "So you're the one who made " + about + " cry? Charming. || You made " + about + " cry? Lovely." + " || " + about + " in tears. Nice work. || Making " + about + " cry? Classy.");
                    return T(t, "I heard you were kind of mean to " + about + "... || I heard you were mean to " + about + "... || People say you hurt " + about + "... || Were you unkind to " + about + "...?", "People say you were rude to " + about + ". Is that true? || Were you rude to " + about + "?! || Did you really insult " + about + "?! || Rude to " + about + "?! True?!", "Rumor says you were rude to " + about + ". || You were rude to " + about + ", I hear. || Rudeness to " + about + ". Rumored." + " || " + about + ". Rude. Reportedly.", "I heard about you and " + about + ". Tsk. || Heard about " + about + ". Tsk tsk." + " || " + about + ", huh? Tsk. || Naughty with " + about + ".");
                case Social.Kinds.Ignored:
                    if (self) return T(t, "You walked right past me earlier... I guess you were busy. || You walked past me earlier... busy, I guess. || You didn't notice me earlier... || You passed by me... it's okay.", "You totally ignored me earlier! Rude! || You ignored me! Rude! || You walked right by! || Hey! You didn't stop!", "You saw me struggling and kept walking. || You saw me. You kept walking. || I struggled. You left. || You walked past.", "Nice of you to just walk by earlier. || Lovely, just walking past. || Very kind, ignoring me. || Thanks for nothing earlier.");
                    return T(t, about + " said you didn't stop to help her..." + " || " + about + " said you didn't help her... || You didn't stop for " + about + "..." + " || " + about + " needed help... you didn't stop.", "You left " + about + " hanging? || You left " + about + " stuck?! || You ditched " + about + "?!" + " || " + about + " needed you!", "I heard you walked past " + about + ". || You passed " + about + " by." + " || " + about + ". You walked on. || You didn't help " + about + ".", "Not very gallant with " + about + ", were you? || Not so gallant with " + about + "." + " || " + about + "? Ignored? Tsk. || Left " + about + " alone? Nice.");
                case Social.Kinds.BrokePromise:
                    if (self) return T(t, "You said you'd help... I waited. || You said you'd help... I waited. || I waited for you... || You promised... I was waiting.", "Hey! You forgot about me! || You forgot me! || Hey! You didn't come! || You bailed!", "You didn't do what you said you would. || You didn't do it. || Promise broken. || You didn't follow through.", "I asked you for one thing. One. || One thing. I asked one thing. || Just one favor. Failed. || Unbelievable.");
                    return T(t, about + " was let down. You promised her something." + " || " + about + " was disappointed. You promised. || You let " + about + " down..." + " || " + about + " trusted your promise...", "Dude, you bailed on " + about + "! || You bailed on " + about + "! || You ditched " + about + "!" + " || " + about + "'s upset with you!", "I heard you didn't keep your word to " + about + ". || You broke your word to " + about + "." + " || " + about + ". Promise broken. || Not kept with " + about + ".", "Breaking promises to " + about + "? Bad look. || Promises to " + about + "? Broken. Bad look. || Failing " + about + "? Ugly. || Bad look with " + about + ".");
                case Social.Kinds.SeenGirlsRoom:
                    return T(t, "Um... people are saying you were in the girls' restroom...? || People say you were in the girls' restroom...? || Um... the girls' restroom...? Really? || Is it true... the girls' restroom?", "Okay, WHY were you in the girls' restroom?! || WHY the girls' restroom?! || The girls' bathroom?! Seriously?! || What were you doing in there?!",
                        "Explain the girls' restroom thing. || The girls' restroom. Explain. || Explain the restroom. || The restroom. Talk.", "So. The girls' restroom. I'm listening. || The girls' restroom. Go on. || So... the girls' restroom? || I'm curious. The restroom.");
                case Social.Kinds.SeenLockers:
                    return T(t, "I heard someone saw you near the girls' changing room... that's not true, right? || Someone saw you near the girls' changing room... not true, right? || The changing room...? That's a rumor, right? || Please say the changing room thing isn't true...", "You were in the girls' changing room?! Are you crazy?! || The girls' changing room?! Insane! || Changing room?! What?! || You went in there?!",
                        "The changing room. Care to explain? || The changing room. Explain. || Changing room. Talk. || About the changing room.", "The girls' changing room, huh? Bold. || Changing room, huh? Daring. || The changing room? Bold boy. || Peeking? Naughty.");
                case Social.Kinds.HerBoysRoom:
                    return T(t, "A-about earlier... in the restroom... please don't tell anyone. I got lost, I swear! || About the restroom... please keep it secret. I got lost! || The restroom... I was lost, I promise! Don't tell! || P-please forget the restroom...!",
                        "Okay, listen! The boys' restroom thing! Total accident! You didn't see anything! || The boys' restroom! Accident! Saw nothing! || Total mistake! You didn't see! || Restroom thing? Never happened!",
                        "About the restroom. It was the closest one. We will never speak of it. || The restroom. Closest one. Never mention it. || It was nearest. Forget it. || We don't discuss the restroom.",
                        "You saw nothing in that restroom. Got it? ...Good. || You saw nothing. Right? Good. || Restroom? Forgotten. Understood? || Nothing happened. Agreed? Good.");
                case Social.Kinds.Flirted:
                    if (self && what == Witness.Kiss) return T(t, "That kiss... I keep thinking about it. || The kiss... I keep remembering. || I can't stop thinking about the kiss... || That kiss... still on my mind.", "Okay, so. The kiss. That happened! || The kiss! It happened! || We kissed! Really! || Okay! The kiss! Wow!", "About the kiss. ...It wasn't bad. || The kiss. ...Not bad. || About the kiss. ...Fine. || ...The kiss was good.", "Thinking about that kiss? Me too~ || Thinking of the kiss? Same~ || Still on your lips? || That kiss~ Remember?");
                    if (self && what == Witness.Hand) return T(t, "Your hand was really warm earlier... || Your hand was warm... || I still feel your hand... || Your hand... so warm...", "We held hands! Like, actually! || Hands! We held them! || Hand holding! Real! || We held hands! Wow!", "Holding hands. ...It was fine. || Hands. ...Fine. || ...Holding hands. Okay. || ...It was acceptable.", "Still feel my hand in yours? Hehe. || Still feel my hand? || Miss my hand? || Hands~ Remember?");
                    if (self) return T(t, "What you said yesterday... I keep thinking about it. || What you said... I keep thinking. || Your words yesterday... I remember. || I think about what you said...", "You were being kinda smooth earlier, huh? || You were smooth earlier! || Smooth talker! || Ooh, so charming earlier!", "About earlier... I didn't hate it. || Earlier... I didn't mind. || ...It was fine. || ...Not unwelcome.", "Still trying to charm me? Keep trying. || Still charming me? Go on. || Trying to win me? Continue. || Keep charming. It's working.");
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
                    return saw ? T(t, "I... saw you kiss " + about + ". I didn't mean to look. || I saw you kiss " + about + "... I didn't mean to. || I saw you and " + about + "... kissing. Sorry. || You kissed " + about + "... I saw.", "I SAW that! You and " + about + ", kissing! Right there! || I SAW! You and " + about + "! Kissing! || You kissed " + about + "! Right there! || Kissing " + about + "! I saw!",
                                   "I saw you kiss " + about + ". Just so you know. || You kissed " + about + ". I saw. || Saw you and " + about + ". Kissing." + " || " + about + ". Kiss. Witnessed.", "Kissing " + about + " in front of everyone? I had a front-row seat. || Front row seat to you and " + about + ". || Kissing " + about + " publicly? I watched. || Saw you and " + about + ". Steamy.")
                               : T(t, "People are saying you kissed " + about + "... is that true? || Did you really kiss " + about + "...? || People say you kissed " + about + "... || Is the kiss with " + about + " true?", "Is it true you kissed " + about + "?! Everyone's talking! || You kissed " + about + "?! Everyone's saying it! || Kiss with " + about + "?! True?! || Rumor! You and " + about + "... kissing?!",
                                   "I heard you kissed " + about + ". Rumor or fact? || You kissed " + about + "? Fact or rumor?" + " || " + about + ". Kiss. True? || Rumor: you kissed " + about + ". Confirm.", "So the rumor is you kissed " + about + ". Do tell. || Rumor says you kissed " + about + ". Spill. || You and " + about + "? Tell me. || Kissed " + about + "? Details.");
                case Witness.Hand:
                    return saw ? T(t, "I saw you holding hands with " + about + "... you looked happy. || You held hands with " + about + "... you looked happy. || You and " + about + ", hand in hand... || I saw you and " + about + "... holding hands.", "I saw you two holding hands! You and " + about + "! || You and " + about + "! Hands! || Holding hands with " + about + "! I saw!" + " || " + about + "'s hand! Yours!",
                                   "You and " + about + ", hand in hand. I saw. || You and " + about + ". Hands. Saw it. || Hand in hand. With " + about + ". || I saw. You and " + about + ".", "Holding hands with " + about + " where anyone can see? Bold. || Holding hands with " + about + " in public? Daring." + " || " + about + "'s hand? Bold. || Hands with " + about + "? Cute. Public.")
                               : T(t, "Someone said you were holding hands with " + about + "... || They say you held hands with " + about + "... || Someone mentioned you and " + about + "... hands... || Is it true? You and " + about + "?", "I heard you and " + about + " were holding hands! || You and " + about + " holding hands?! || Heard about you and " + about + "! || Hand holding with " + about + "!",
                                   "Word is you held hands with " + about + ". || Held hands with " + about + ", they say. || Rumor: hands with " + about + "." + " || " + about + ". Hands. Rumored.", "Hand in hand with " + about + ", I hear. Cute. || Hands with " + about + ". Adorable." + " || " + about + ", hand in hand? Sweet. || Cute rumor. You and " + about + ".");
                case Witness.Touch:
                    return saw ? T(t, "I saw where your hands were with " + about + "... w-what was that? || Your hands with " + about + "... w-what? || I saw your hands... with " + about + "... || W-what were you doing to " + about + "...?", "Hey! I saw you getting all touchy with " + about + "! || Touchy with " + about + "! I saw! || Hands all over " + about + "! || Hey! Handsy with " + about + "!",
                                   "I saw you with " + about + ". Your hands were busy. || Your hands were busy with " + about + ". || I saw. Hands. " + about + ". || With " + about + ". Hands. Saw.", "Getting handsy with " + about + "? I saw everything~ || Handsy with " + about + "? I watched~ || Busy hands with " + about + "~ || Saw everything with " + about + "~")
                               : T(t, "People say you were touching " + about + "... that's not true, right? || You touched " + about + "...? Not true, right? || People say... you and " + about + "... touching? || Please say it's not true... about " + about + ".", "Is it true you were all over " + about + "?! || You were all over " + about + "?! || Touching " + about + "?! Really?! || Handsy with " + about + "?!",
                                   "I hear you got handsy with " + about + ". || Handsy with " + about + ", I hear." + " || " + about + ". Touching. Rumored. || Your hands. " + about + ". Rumors.", "Rumor has it your hands wandered with " + about + ". Hm~ || Wandering hands with " + about + "? Hm~ || Rumor: handsy with " + about + "~ || Touchy with " + about + "? Interesting~");
                case Witness.H:
                    return saw ? T(t, "I... I saw you and " + about + ". I can't unsee it. || I saw you and " + about + "... I can't forget it. || You and " + about + "... I saw it all. || I wish I hadn't seen you and " + about + "...", "I SAW you and " + about + "! Out in the open?! Seriously?! || You and " + about + "! In the open?! || I saw you and " + about + "! Seriously?! || Out there?! With " + about + "?!",
                                   "I saw you and " + about + ". All of it. || You and " + about + ". I saw it all. || Everything. You and " + about + ". || I witnessed you and " + about + ".", "You and " + about + ", out where anyone could walk by? I did walk by. || I walked by you and " + about + ". Surprise. || Out in the open with " + about + "? I saw. || You and " + about + ". Public. I watched.")
                               : T(t, "There's a rumor about you and " + about + "... the kind I can't even say. || There's a rumor about you and " + about + "... I can't even repeat it. || People are saying things about you and " + about + "... embarrassing things. || I heard something about you and " + about + "... I'm blushing.", "Everyone's whispering about you and " + about + "! Is it true?! || The whole university's whispering about you and " + about + "! True?! || Rumor alert! You and " + about + "! || Everyone's buzzing about you and " + about + "!",
                                   "The rumor about you and " + about + ". Tell me it's a rumor. || The rumor with " + about + ". Say it's false. || About you and " + about + ". Tell me it's a rumor. || That " + about + " rumor. Explain.", "You and " + about + ", huh? The whole university is blushing. || You and " + about + "? Everyone's red-faced. || The whole university is flustered over you and " + about + ". || You and " + about + ". Scandalous.");
                default:
                    return saw ? T(t, "I saw you being really sweet to " + about + "... she was all red. || You were so sweet to " + about + "... she turned red. || I saw you with " + about + "... she was blushing. || You made " + about + " blush... I saw.", "I saw you sweet-talking " + about + "! Smooth! || Sweet-talking " + about + "! Smooth! || You charmed " + about + "! I saw! || Ooh! Flirting with " + about + "!",
                                   "I saw you charming " + about + ". It was something. || Charming " + about + ". I saw. || You and " + about + ". Charm. Noted. || I saw you with " + about + ". Quite smooth.", "I caught you flirting with " + about + ". Not subtle. || Flirting with " + about + ". Subtle, not. || Caught you charming " + about + ". || Not subtle with " + about + ", are you?")
                               : T(t, "I heard you were being really sweet to " + about + "... || They say you were sweet to " + about + "... || I heard you're nice to " + about + "... || You've been kind to " + about + ", I hear...", "So, you and " + about + ", huh? Spill! || You and " + about + "?! Tell me! || Spill about " + about + "! || What's up with you and " + about + "?!",
                                   "I hear you've been charming " + about + ". || You're charming " + about + ", I hear. || Rumor: " + about + ". Charm." + " || " + about + ". You're charming her.", "Flirting with " + about + "? Interesting. || Flirting with " + about + "? Hm." + " || " + about + ", huh? Intriguing. || Charming " + about + "? Interesting choice.");
            }
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

        public static string Vent(Tone t, Mood m)
        {
            switch (m)
            {
                case Mood.Stressed:
                    return T(t, "It's the exams... Everyone expects so much and I don't want to disappoint anyone. Sorry, I'm just dumping this on you. || Exams... everyone expects so much. I'm scared to disappoint them. Sorry for unloading this. || The tests are close and everyone's counting on me... sorry, I needed to say it. || I'm so afraid of letting everyone down... sorry for dumping it on you.",
                        "Everything's piling up! Assignments, club, my mom... I just wanna scream into a pillow! || Assignments, club, mom... too much! I wanna scream! || Everything's stacking up! I need a pillow to yell into! || Too much stuff! My head's exploding!",
                        "I have three deadlines this week and the professor added a fourth. I'm handling it. I just needed to say it out loud. || Three deadlines, now four. I'm handling it. Just venting. || Four deadlines this week. Manageable. I needed to say it. || An extra deadline appeared. I'll manage. Venting.",
                        "Everyone wants something from me today. You're the first person who just... listened. || Everyone wants something today. You just listened. || All day, demands. You're the only one who listened. || Everyone needs me. You just... heard me out.");
                case Mood.Upset:
                    return T(t, "I had a fight with a friend... I said something I didn't mean. Thank you for listening. || I fought with a friend... I said something cruel. Thanks for listening. || I hurt a friend's feelings... thank you for hearing me. || I said something I regret... thanks for listening.",
                        "Someone said something really mean about me and everyone laughed. Ugh. Thanks for not laughing. || Someone mocked me and everyone laughed. Thanks for not laughing. || People laughed at me today. Thanks for being different. || Someone was mean and everybody laughed. Ugh. Thanks.",
                        "A bad day. Things didn't go how I planned. ...Thanks for hearing me out. || Plans went wrong. ...Thanks for listening. || Bad day. ...Thanks for hearing it. || Not my day. ...Thank you.",
                        "Fine, you want to know? Someone stood me up. There. Happy? ...Thanks, I guess. || Someone stood me up. Happy? ...Thanks. || I got stood up. There. ...Thanks, I suppose. || Stood up. Fine. ...Thanks.");
                case Mood.Lonely:
                    return T(t, "Sometimes it feels like nobody would notice if I weren't here... Sorry. That's a gloomy thing to say. || Sometimes I think no one would notice if I disappeared... sorry. Gloomy. || I feel invisible lately... sorry for saying that. || Nobody seems to notice me... sorry, that's sad.",
                        "Everyone's got plans except me lately! It's kinda sad, right? Hehe... || Everyone has plans but me! Sad, right? Hehe... || Nobody invites me lately! Hehe... sad. || All my friends are busy! Hehe... lonely.",
                        "My friends have been busy. It's quieter than I'd like. || My friends are busy. It's too quiet. || Quieter than I'd like lately. || My friends are occupied. Quiet days.",
                        "Don't laugh, but I've been eating lunch alone all week. || Don't laugh. I've eaten alone all week. || Lunch alone all week. Don't laugh. || Solo lunches. All week. Laugh and die.");
                default:
                    return T(t, "I'm just tired lately. Thank you for listening. || I'm just tired. Thanks for listening. || Just tired... thank you. || So tired lately... thanks.", "Ugh, today was SO long! || Today was SO long! || Ugh, endless day! || Longest day ever!", "It's been a long week. || Long week. || A tiring week. || The week dragged.", "Life's being annoying. That's all. || Life's annoying. That's all. || Just annoyed with life. || Everything's irritating.");
            }
        }

        public static string VentThanks(Tone t)
        {
            return T(t, "...I feel lighter now. Thank you. || ...I feel better. Thank you. || ...That helped so much. || ...Lighter now. Thanks.", "Okay! I feel way better! You're like a human stress ball! || Way better! Human stress ball! || I feel great now! Thanks! || Better! You're magic!", "That helped. Don't tell anyone I said that. || That helped. Keep it secret. || Useful. Don't tell. || Helped. Between us.", "You're surprisingly good at this. || You're good at this. Surprising. || Not bad at this. || Surprisingly helpful.");
        }

        public static string Anecdote(Tone t)
        {
            switch (R(6))
            {
                case 0: return T(t, "This morning a little bird landed on my desk by the window. I didn't dare move all through first period. || A bird landed on my desk this morning. I didn't move all period. || A little sparrow visited my window today. I held my breath. || A bird sat by me in class. I froze so it wouldn't leave.",
                    "I tried to jump the university fence this morning because I was late. Spoiler: the fence won. || I tried jumping the fence when late. The fence won. || Fence climb this morning! Fence: one, me: zero! || Tried to vault the gate! Faceplant!",
                    "The vending machine gave me two drinks for the price of one today. I've decided it's a sign. || Two drinks for one today. A sign. || The vending machine glitched in my favor. A sign. || Free drink. The universe approves.",
                    "A first-year asked me for directions and I sent him to the faculty room on purpose. He'll thank me later. || Sent a first-year to the faculty room on purpose. He'll thank me. || Gave a newbie wrong directions. Character building. || Misled a first-year. Educational.");
                case 1: return T(t, "My little sister made me a bento with a face on it. I couldn't bring myself to eat the eyes. || My sister made me a bento with a face. I couldn't eat the eyes. || A smiley bento from my sister... too cute to eat. || My sister drew a face on my rice... I couldn't eat it.",
                    "I beat my high score in that rhythm game! My fingers still hurt! || New high score in my rhythm game! Fingers hurt! || Beat my record! My hands are dead! || High score! Ouch, fingers!",
                    "I finished a thousand-piece puzzle yesterday. One piece was missing. I'm still angry. || Thousand-piece puzzle. One piece missing. Still angry. || Finished a puzzle. Minus one piece. Furious. || Puzzle done. One missing. Unacceptable.",
                    "I convinced the cafeteria lady to give me extra karaage. Charm is a skill. || Extra karaage from the lunch lady. Charm. || Charmed my way to extra karaage. || Free karaage. I'm irresistible.");
                case 2: return T(t, "I found a really good bookstore near the station. It smells like old paper. I could live there. || There's a bookstore near the station... it smells like old paper. || I found a cozy bookstore... I could stay forever. || A little bookstore by the station... heaven.",
                    "There's a new crepe stand near the park! I've already tried four flavors! || New crepe stand by the park! Four flavors already! || Crepes near the park! I tried them all! || Crepe stand! Four down!",
                    "I started learning chess. The club president beat me in six moves. Six. || Started chess. Lost in six moves. Six. || Chess. Six moves. I lost. || Learning chess. Humbling.",
                    "I saw a cat wearing a tiny sweater on the way here. My day peaked at 7 a.m. || Cat in a sweater this morning. Peak day. || Saw a cat in a sweater. Day peaked early. || A sweatered cat. Nothing tops that.");
                case 3: return T(t, "I've been practicing calligraphy. My characters still look like they're running away. || My calligraphy characters look like they're fleeing... || Practicing calligraphy... still messy. || Calligraphy is hard... my strokes wobble.",
                    "Our club's practice match is coming up and I've been training every morning! || Training every morning for our match! || Practice match soon! Training hard! || Morning drills! Match coming!",
                    "I reorganized my whole room by color. It's very satisfying. Don't judge. || Organized my room by color. Satisfying. || Color-coded my room. Don't judge. || Room by color. Peaceful.",
                    "I've been writing a story. No, you can't read it. ...Maybe later. || Writing a story. No, you can't read it. Yet. || A story. Secret. ...Maybe someday. || I write. You can't see it. Maybe later.");
                case 4: return T(t, "I fell asleep on the train and woke up three stations late. I had to run all the way here... || I slept on the train and missed my stop... I had to run. || Three stations too far... I ran all the way. || I dozed off on the train... so late.",
                    "My phone fell in the pool yesterday. I'm living like a caveman now! || My phone fell in the pool! Caveman life! || Phone in the pool! No tech! || Dropped my phone in water! Disaster!",
                    "I tried cooking dinner for my family. They were very... polite. || Cooked for my family. They were polite. || Made dinner. Polite reactions. || I cooked. They were... kind.",
                    "I dyed a streak of my hair last weekend. Nobody noticed. Do you? || Dyed my hair. Nobody noticed. Do you? || New hair streak. See it? || Notice anything? My hair?");
                default: return T(t, "The cherry tree behind the gym is starting to bud. I go look at it every morning. || The cherry tree by the gym is budding... I visit every morning. || The cherry buds are coming... I check daily. || Spring's coming... the tree is budding.",
                    "I signed up to help at the campus festival! It's gonna be huge! || I'm helping with the festival! Huge! || Festival volunteer! So excited! || Signed up for the festival! Big!",
                    "The library got the new volume of the series I like. I reserved it the second I saw it. || New volume at the library. Reserved instantly. || The library got my series. Reserved. || New volume in. I claimed it.",
                    "I've been people-watching in the courtyard. You'd be amazed what you learn. || People-watching in the courtyard. Educational. || Watching people. You learn things. || The courtyard is full of secrets.");
            }
        }

        public static string GossipLine(Tone t, string kind, string about, string what = null)
        {
            switch (kind)
            {
                case Social.Kinds.Helped:
                case Social.Kinds.Defended:
                    return T(t, "Oh, " + about + " told everyone you helped her. She seemed really happy." + " || " + about + " told everyone you helped her. She was so happy." + " || " + about + " keeps saying you helped her... it's sweet." + " || " + about + " is happy you helped her.", "Did you know " + about + " has been bragging that you helped her?" + " || " + about + "'s bragging you helped her!" + " || " + about + " says you're her hero!" + " || " + about + " won't stop talking about your help!",
                        about + " mentioned you helped her. People noticed." + " || " + about + " mentioned your help. People heard." + " || " + about + " spoke of your help. || Your help to " + about + ". Noticed.", about + " won't shut up about how you helped her. It's adorable." + " || " + about + " won't stop talking about you. Cute." + " || " + about + " is gushing. Adorable." + " || " + about + " adores you now.");
                case Social.Kinds.Rude:
                    return T(t, "Um... " + about + " seemed hurt by something you said." + " || " + about + " seemed hurt by your words... || Um... " + about + " looked upset because of you." + " || " + about + " was sad after you talked...", "Heads up: " + about + " is mad at you! || Warning: " + about + "'s mad at you!" + " || " + about + " is angry! Watch out! || Uh oh! " + about + "'s upset!", about + " is not happy with you. Just so you know." + " || " + about + " is displeased with you." + " || " + about + " isn't happy. FYI." + " || " + about + ". Upset. With you.", "You know " + about + " is telling everyone you were rude, right?" + " || " + about + " says you were rude. To everyone." + " || " + about + "'s spreading that you were rude. || Rude to " + about + "? She's telling people.");
                case Social.Kinds.BrokePromise:
                    return T(t, about + " said you forgot something you promised her..." + " || " + about + " said you forgot a promise... || You forgot something for " + about + "..." + " || " + about + " was waiting on your promise...", about + " says you bailed on her!" + " || " + about + " says you ditched her! || You bailed on " + about + "?!" + " || " + about + "'s saying you flaked!", about + " says you didn't keep your word." + " || " + about + " says you broke your word. || Word broken. Says " + about + "." + " || " + about + ". Promise. Broken.", "Rumor has it you stood up " + about + ". || You stood up " + about + ", they say. || Ditched " + about + "? Rumor says so." + " || " + about + " got stood up. By you.");
                case Social.Kinds.KeptPromise:
                    return T(t, about + " says she can always count on you." + " || " + about + " says she can rely on you." + " || " + about + " trusts you so much." + " || " + about + " says you're always there.", about + " says you're super reliable!" + " || " + about + " says you're super dependable!" + " || " + about + " trusts you totally!" + " || " + about + " says you never fail!", "Apparently you keep your promises. " + about + " says so. || You keep promises. " + about + " confirms." + " || " + about + " vouches for you. || Reliable, per " + about + ".", about + " thinks you're dependable. Don't disappoint her." + " || " + about + " trusts you. Don't fail her. || Dependable, says " + about + ". Careful." + " || " + about + " counts on you. No pressure.");
                case Social.Kinds.GoodTalk:
                    return T(t, about + " said talking with you was fun." + " || " + about + " enjoyed talking with you." + " || " + about + " had fun with you." + " || " + about + " liked your chat.", about + " thinks you're a great listener!" + " || " + about + " says you're a great listener!" + " || " + about + " loved talking to you!" + " || " + about + " thinks you're awesome to talk to!", about + " enjoyed your chat, I hear." + " || " + about + " liked your chat. || Good chat, " + about + " said." + " || " + about + " enjoyed it.", about + " seems to like talking with you. Should I be jealous?" + " || " + about + " likes talking to you. Jealous? Me? || Should I worry about " + about + "?" + " || " + about + " enjoys you. Hm.");
                case Social.Kinds.Flirted:
                    if (what == Witness.Kiss) return T(t, "People say you kissed " + about + "... is that true? || Did you really kiss " + about + "...? || They say you kissed " + about + "... || A kiss with " + about + "...? Really?", "Everyone's saying you kissed " + about + "! || You kissed " + about + "! Everyone says so! || Kiss with " + about + "! The university knows!" + " || " + about + " and you kissed?!", "There's talk you kissed " + about + ". || Talk of a kiss with " + about + ". || Rumor: you and " + about + ". Kiss. || A kiss. " + about + ". Rumored.", "You kissed " + about + "? The whole class knows. || Kissed " + about + "? Whole class knows. || Everyone knows about " + about + "." + " || " + about + ". Kiss. Famous now.");
                    if (what == Witness.Hand) return T(t, "Someone saw you holding hands with " + about + "... || You held hands with " + about + ", someone said... || Holding hands with " + about + "...? || They saw you and " + about + "... hands.", "You and " + about + ", holding hands! Everyone saw! || Holding hands with " + about + "! Everyone saw! || You and " + about + "! Hands! Public! || Hands with " + about + "! Seen!", "Word is you held hands with " + about + ". || Hands with " + about + ", they say. || Seen: you and " + about + ". Hands." + " || " + about + ". Hand in hand.", "Hand in hand with " + about + "? People noticed. || Hand in hand with " + about + "? Noticed." + " || " + about + "'s hand. People saw. || Public hand holding. " + about + ".");
                    if (what == Witness.Touch) return T(t, "People say you were... touching " + about + "... || They say you... touched " + about + "... || People are saying things... you and " + about + "... || You and " + about + "... touching? People say...", "Dude, people say you were all over " + about + "! || People say you were all over " + about + "! || Handsy with " + about + "?! Rumor! || You and " + about + "! Hands everywhere!", "There's talk about your hands and " + about + ". || Talk about your hands. And " + about + ". || Rumor: hands. " + about + ". || Your hands and " + about + ". Discussed.", "Your hands and " + about + ". That's the rumor. || Your hands and " + about + ". The rumor. || Hands on " + about + ", they say. || Rumor: you and " + about + ". Hands.");
                    return T(t, "People say you and " + about + " are getting close... || You and " + about + " are getting close, they say... || People think you and " + about + " are close... || You and " + about + "... close?", "Everyone's talking about you and " + about + "! || You and " + about + "! Everyone's talking! || The university's buzzing about you and " + about + "! || You and " + about + "! Big news!", "There's talk about you and " + about + ". || Talk of you and " + about + ". || You and " + about + ". Discussed. || Rumors. You and " + about + ".", "You and " + about + ", huh? The whole class knows. || You and " + about + "? Class knows. || Everyone knows about you and " + about + ". || You and " + about + ". Famous.");
                case Social.Kinds.SeenGirlsRoom:
                case Social.Kinds.SeenLockers:
                    return T(t, "There's a strange rumor going around about you... something about the girls' rooms. || There's a weird rumor about you... the girls' rooms... || People say odd things about you... girls' rooms? || A strange rumor... about the girls' rooms...", "Dude, there's a weird rumor about you and the girls' rooms! || Weird rumor about you and the girls' rooms! || Dude! Girls' rooms rumor! || What's this girls' room rumor?!", "There's an unflattering rumor about you going around. || An unflattering rumor. About you. || A bad rumor is circulating. || Rumors about you. Not nice ones.", "You're famous now. For the wrong reasons. || Famous. Wrong reasons. || Infamous now. Congrats. || You're notorious.");
                default:
                    return Anecdote(t);
            }
        }

        public static string NothingNew(Tone t)
        {
            return T(t, "Hmm... I can't think of anything new, sorry. || Sorry, nothing new comes to mind. || Hmm... I'm out of news. || Nothing new, sorry...", "Ehh, nothing's happening! Boring day! || Nothing's happening! Boring! || No news! Lame! || Zero news today!", "Nothing worth mentioning. || Nothing to report. || Nothing new. || No news.", "You've heard all my stories today. || You've heard everything today. || Stories all used up. || That's all I've got.");
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

        public static string Reputation(Tone t, int rep)
        {
            if (rep >= 70) return T(t, "Everyone says you're really kind. I think so too. || Everyone thinks you're kind. Me too. || People say you're so nice... I agree. || You're known as kind. It's true.", "You're kinda famous! Everyone likes you! || You're famous! Everyone loves you! || Popular guy! Everyone likes you! || You're a celebrity!", "You have a good reputation. Don't waste it. || Good reputation. Keep it. || People respect you. Don't ruin it. || Well regarded. Maintain it.", "You're popular, you know. It's a little annoying. || Popular. Slightly irritating. || Everyone likes you. Annoying. || You're liked. How tiresome.");
            if (rep >= 40) return T(t, "People think you're nice, I think. || People like you, I think. || You seem well liked... || I think people think well of you.", "People like you! Mostly! || People like you! Mostly! || You're liked! Kinda! || Most people dig you!", "People don't mind you. || Tolerated. || People are fine with you. || Acceptable reputation.", "You're... known. In a good way. Mostly. || Known. Nicely. Mostly. || A decent reputation. Sort of. || Not bad. Not great.");
            if (rep >= 20) return T(t, "Um, not everyone knows you yet. || Not everyone knows you yet... || Some people don't know you... || You're still new to many...", "Half the class doesn't know your name! Haha! || Half the class doesn't know you! Ha! || Who? Haha! Kidding! Sorta! || Nobody knows your name! Haha!", "You're not very well known. || Not widely known. || Low profile. || Few know you.", "You're a bit of a mystery. Not the cool kind. || Mysterious. Not cool mysterious. || An unknown. Not intriguing. || A mystery. A boring one.");
            return T(t, "Some people say... strange things about you. I don't believe them! || People say strange things... I don't believe them! || There are odd rumors... but I trust you! || Some say bad things... I don't buy it!", "People are saying weird stuff about you, dude. || People say weird stuff about you! || Dude, weird rumors! || Your rep's kinda weird, man!", "Your reputation is poor. Work on it. || Poor reputation. Fix it. || Bad standing. Improve. || Your reputation needs work.", "Your reputation is in the gutter. Impressive, really. || Gutter reputation. Impressive. || Your rep is trash. Wow. || Rock-bottom reputation. Remarkable.");
        }

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

        public static string JokeGood(Tone t)
        {
            switch (R(3))
            {
                case 0: return T(t, "Pfft... ahaha! That's so silly! || Pfft... hehe! So silly! || Ahaha! That's ridiculous! || Hehehe! Stop!", "AHAHA! Stop, stop, my stomach! || AHAHA! My stomach! || BWAHAHA! Stop! || Hahaha! I can't breathe!", "...Heh. Okay, that one was good. || ...Heh. Good one. || ...Pfft. Okay. || ...That got me.", "Ugh, that's terrible. *snort* Dammit. || Terrible. *snort* Ugh. || Awful. *giggle* Damn. || So bad. ...Heh. Dammit.");
                case 1: return T(t, "Hehe... you're funny. || Hehe... funny. || You're so funny... || Ehehe... good one.", "Hahaha! Tell me another! || Another! Another! || Hahaha! More! || Encore!", "I didn't expect to laugh. Well done. || Unexpected laugh. Good. || ...I laughed. Well done. || Not bad.", "Fine. I laughed. Happy? || I laughed. Satisfied? || Fine. Funny. || Okay. Ha.");
                default: return T(t, "Ahaha... I'm going to think about that all day. || I'll think about that all day... hehe. || Ahaha... that'll stay with me. || Hehe... I'll remember that.", "Pfffft! You're the best! || Pffft! Best! || HA! Love it! || You're hilarious!", "Heh. Not bad at all. || Heh. Good. || Not bad. || ...Decent.", "Okay, okay, you win this round. || You win this round. || Fine. Point to you. || Okay. You got me.");
            }
        }

        public static string JokeBad(Tone t)
        {
            switch (R(2))
            {
                case 0: return T(t, "Um... hehe...? Sorry, I don't think I got it. || Um... hehe? I didn't get it. || Ah... sorry, I don't understand. || Hehe...? Was that a joke?", "...Was that the joke? || ...That's it? || Wait, that was the joke? || ...Huh?", "... || ... || ...Hm. || ...No.", "Wow. That was painful. For both of us. || Painful. For everyone. || Ouch. That hurt us both. || Yikes.");
                default: return T(t, "Oh... um, that's... nice. || Oh... that's... nice. || Um... okay... || Ah... hehe...", "Booo! Get off the stage! || Booo! || Boo! Get off! || Lame!", "I'll pretend you didn't say that. || I didn't hear that. || Ignoring that. || Forgotten.", "Did that sound funny in your head? || Funny in your head? || That sounded better in your mind, right? || Did you test that?");
            }
        }

        public static string JokeHurt(Tone t)
        {
            return T(t, "That's... kind of mean, isn't it? || That's a bit mean... || Isn't that unkind...? || That wasn't nice...", "Hey! Not funny! || Not funny! || Hey! Mean! || Rude!", "That crossed a line. || Over the line. || Too far. || That's not okay.", "Careful. That one stung. || That stung. Careful. || Watch it. || Ouch. Careful.");
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
                return T(t, "Eh?! Y-you think so...? Thank you... || Eh?! Really...? Thank you... || Y-you think so? Thanks... || Ah... thank you...!", "Hehe, really?! You noticed! || Really?! You noticed! || Hehe! Thanks! || You saw?! Yay!", "...You're not bad yourself. Thanks. || ...You're not bad either. || ...Thanks. You too. || ...Likewise. Thanks.", "Oh? Took you long enough to notice. || Took you long enough. || Finally noticed? || Slow, but okay.");
            return T(t, "R-really? That makes me really happy... || Really? I'm so happy... || That makes me glad... || Oh... thank you so much...", "Yay! Somebody noticed! || Someone noticed! Yay! || Woo! Noticed! || Finally! Yay!", "...Thank you. I appreciate that. || ...Thank you. || ...Appreciated. || ...Thanks.", "Finally, someone with taste. || Good taste. Finally. || Someone with eyes. || At last, taste.");
        }

        public static string ComplimentFlat(Tone t)
        {
            return T(t, "Oh... um, thank you, I guess. || Oh... thanks, I guess. || Um... thank you? || Ah... okay, thanks.", "Haha, thanks? Random, but okay! || Thanks? Random! || Ha! Okay, thanks! || Huh? Thanks!", "Okay. || Okay. || Fine. || Noted.", "Flattery won't get you far with me. || Flattery won't work. || Nice try. || Not that easy.");
        }

        public static string ComplimentRepeat(Tone t)
        {
            return T(t, "You said that yesterday too... || You said that before... || That again...? || You told me yesterday...", "You already told me! Got a new one? || Heard it! New one! || Old news! || You already said!", "You're repeating yourself. || Repeating. || Same line. || Again?", "Running out of material? || Out of material? || Rerun? || Recycling?");
        }

        // ================= romance =================

        public static string FlirtGood(Tone t, int close)
        {
            if (close >= 2)
                switch (R(2))
                {
                    case 0: return T(t, "M-me too... I'm so happy you said that. || M-me too... I'm so happy. || I feel the same... || Me too... really.", "Aww! You're making me blush! Me too, okay?! || Me too! Stop, I'm blushing! || Aww! Same! || Me too, okay?!", "...Me too. Don't make me say it twice. || ...Same. Once only. || ...Me too. || ...Likewise. Don't repeat it.", "Hmph. Smooth talker. ...I feel the same. || Smooth. ...Same. || Charmer. ...Me too. || Hmph. ...Me too.");
                    default: return T(t, "You always know what to say... I love that about you. || You always know what to say... I love it. || You're so sweet... I love that. || Your words make me happy...", "Ehehe! I'm the luckiest girl on campus! || Luckiest girl on campus! || I'm so lucky! || Best boyfriend ever!", "I like this. Us. || I like us. || This is good. || I like this.", "Keep talking like that and I might fall for you all over again. || Keep it up and I'll fall again. || Careful. I'm falling again. || More, and I'm yours all over again.");
                }
            switch (R(2))
            {
                case 0: return T(t, "Eh...? I-I like spending time with you too... || I-I like being with you too... || Eh...? Me too... || I enjoy our time too...", "Wh-whoa, where did that come from?! ...Thanks, hehe. || Whoa! ...Thanks, hehe. || Where'd that come from?! ...Hehe. || Wh-what?! ...Thanks!", "...You're making me lose my composure. || ...You're rattling me. || ...My composure. Gone. || ...Stop. I'm flustered.", "Oh my. Are you trying to make me blush? || Making me blush? || Oh my. Trying to fluster me? || Is that a blush attempt?");
                default: return T(t, "That's... a really nice thing to say. || That's so nice... || What a kind thing... || That's lovely...", "Haha, you're so straightforward! I like it! || So direct! I like it! || Straightforward! Nice! || Ha! Honest! Love it!", "...I don't dislike hearing that. || ...I don't mind it. || ...Not unpleasant. || ...Fine to hear.", "Careful. I might start believing you. || I might believe you. Careful. || Don't make me believe it. || Dangerous words.");
            }
        }

        public static string FlirtTooSoon(Tone t)
        {
            return T(t, "Um... we don't know each other that well yet... || We don't know each other well yet... || Um... too soon... || I'm not ready for that...", "Whoa, slow down there, buddy! || Whoa, slow down! || Easy there! || Too fast, buddy!", "...That's a bit much. || ...Too much. || ...Excessive. || ...Slow down.", "Nice try. You'll have to work harder than that. || Try harder. || Not that easy. || Nice try.");
        }

        public static string LoveTalk(Tone t, int close)
        {
            if (close >= 2) return T(t, "Being your girlfriend... I still can't believe it sometimes. || I still can't believe I'm your girlfriend... || Being your girlfriend feels like a dream... || Your girlfriend... it still surprises me.", "Everyone's jealous of us, you know! Hehe! || Everyone envies us! Hehe! || We're the university's couple! || People are so jealous!", "I've been thinking about where we could go on our next date. || I've been planning our next date. || Next date ideas. I have some. || Where shall we go next?", "So, what's the plan for our next date? It better be good. || Next date plan? Make it good. || Our next date. Impress me. || Plan something good. Next date.");
            return T(t, "Do you... have someone you like? || Is there... someone you like? || Do you like anyone...? || Um... anyone special?", "So! Do you have a crush on anyone? Tell me! || Crush! Who?! Tell me! || Got a crush?! Spill! || Who do you like?! Tell!", "Have you ever been in love? || Ever been in love? || Love. Experienced it? || Have you loved someone?", "Who do you like? Come on, I won't tell. || Who's your crush? I'll keep it secret. || Spill. Who do you like? || Your crush. Tell. I won't tell.");
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
            return T(t, "Oh... I see. I'm sorry I doubted you. || Oh... I'm sorry I doubted you. || I see... sorry. || Ah... forgive me.", "Ohhh! Okay, that makes sense! My bad! || Ohh! Makes sense! My bad! || Oh! Okay! Sorry! || Got it! Oops!", "...Fine. That's reasonable. || ...Reasonable. || ...Fine. || ...Acceptable.", "Hmph. Fine. I'll let it slide. This time. || Fine. This time. || I'll allow it. Once. || Hmph. Pass. Once.");
        }

        public static string ExplainFail(Tone t)
        {
            return T(t, "I... I don't know if I believe that. || I don't know if I believe you... || Hmm... really...? || I'm not sure...", "Uh-huh. Sure. Totally. || Uh-huh. Sure. || Riiight. || Totally. Not.", "That's not convincing. || Unconvincing. || Not persuasive. || Weak.", "Do you hear yourself? || Listen to yourself. || Really? That's it? || Come on.");
        }

        public static string PlayerApologize() { return P("I'm sorry. Really.", "I messed up. I'm sorry.", "You're right. I'm sorry about that."); }

        public static string ApologyAccepted(Tone t)
        {
            return T(t, "...Okay. Thank you for saying that. || ...Okay. Thank you. || ...I forgive you. || ...Thanks for saying sorry.", "Okay, okay! Apology accepted! Just don't do it again! || Accepted! Don't do it again! || Okay! Forgiven! || Fine! Apology accepted!", "Fine. Apology accepted. || Accepted. || Fine. || Apology noted.", "Hmph. You owe me one. || You owe me. || Hmph. Owed. || One favor. Owed.");
        }

        public static string PlayerMakePlans(string friend) { return P("You're right. Let's hang out more, just us.", "Sorry. I'd like to spend more time with you too."); }

        public static string PlansAccepted(Tone t)
        {
            return T(t, "Really? Then... I'll be looking forward to it. || Really? I'll look forward to it. || Then I'll wait for it... || Okay... I can't wait.", "Promise?! Okay! Lunch together sometime! || Promise?! Lunch together! || Okay! Lunch date! || Yay! Promise!", "...Okay. I'll hold you to that. || ...Holding you to it. || ...Okay. Noted. || ...I'll remember.", "You'd better mean it. || Mean it. || You'd better. || Don't lie.");
        }

        public static string KeepSecret(Tone t, bool promise)
        {
            if (promise) return T(t, "Thank you... I really owe you one. || Thank you... I owe you. || Thanks so much... || I'm in your debt...", "You're a lifesaver! Our secret, okay?! || Lifesaver! Secret! || Our secret! Thanks! || You rock! Shh!", "Good. ...Thanks. || Good. ...Thanks. || ...Thank you. || ...Appreciated.", "Smart choice. I'll remember this. || Smart. I'll remember. || Wise choice. || Good. Noted.");
            return T(t, "You're so mean...! || So mean...! || You're cruel...! || How could you...!", "Noooo! You wouldn't! || No! You wouldn't! || Don't you dare! || Nooo!", "Not funny. || Not funny. || Stop. || Unamusing.", "Try it and see what happens. || Try it. See. || Go ahead. I dare you. || Do it and regret it.");
        }

        // ================= ending =================

        public static string OutOfPatience(Tone t)
        {
            return T(t, "Um, sorry, I should get going soon... || Sorry, I need to go... || I should leave soon... || Um, I have to go...", "Ah, I gotta run! Later! || Gotta run! Later! || Bye! Gotta go! || Running! See ya!", "I need to go. || Leaving. || I have to go. || Time to go.", "That's enough for now. I have things to do. || Enough for now. || That's all for today. || I'm busy. Bye.");
        }

        public static string Bye(Tone t, int interest)
        {
            if (interest >= 4) return T(t, "That was really fun. Thank you for talking with me. || That was fun. Thank you. || I enjoyed that so much. || Thank you for the chat... it was fun.", "That was awesome! Let's do this again! || Awesome! Again soon! || So fun! Again! || Let's do it again!", "...That was a good talk. || ...Good talk. || ...Nice conversation. || ...That was pleasant.", "Not bad. You're more interesting than you look. || More interesting than you look. || Not bad. Surprising. || You're fun. Who knew.");
            if (interest >= 2) return T(t, "Thanks for chatting with me. || Thanks for talking. || Thank you. || That was nice.", "Fun chat! || Fun! || Nice chat! || Fun talk!", "That was fine. || Fine. || Okay. || Adequate.", "Okay, that wasn't terrible. || Not terrible. || Passable. || Okay. Fine.");
            return T(t, "Um... see you. || ...See you. || Um... bye. || ...Bye.", "Ehh, I'm kinda tired of talking. || Kinda talked out. || I'm done talking. || Tired of chatting.", "Bye. || Bye. || ...Later. || Goodbye.", "Well. That happened. || Well. That was something. || Okay then. || That happened.");
        }

        public static string TooTired(Tone t)
        {
            return T(t, "Sorry, I don't really have time to talk right now... || Sorry, no time right now... || I can't talk now, sorry... || Not now, sorry...", "Ugh, can we talk later? I'm beat! || Later? I'm wiped! || Ugh, later! || I'm beat! Later!", "Not now. || Not now. || Later. || No.", "I've talked enough today. Come back tomorrow. || Talked enough. Tomorrow. || Come back tomorrow. || Done for today.");
        }
    }
}
