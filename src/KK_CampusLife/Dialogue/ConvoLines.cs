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
                    return Conversation.EverydayTone(t, "hello");
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
                    return Conversation.EverydayTone(t, "anecdote");
            }
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
