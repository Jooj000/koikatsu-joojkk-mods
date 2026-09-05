# Campus Life - dialogue written for the mod

Total lines (counting every personality version): 1905

Every line below was written for Campus Life (author: Joojkk). Lines of the game itself
(normal conversations, story events, H) are not listed. Lines marked 'by personality' have four
versions: gentle / energetic / cool / teasing, chosen from the girl's personality.

Placeholders like {Name}, {OName} (second girl), {place} or {item} are filled in during the game.

## Shared lines (reactions used by many scenes)
`Dialogue/Lines.cs`

### Lines / MoodLine / step Mood.Cheerful
- **Her (by personality)**
  - _gentle:_ I'm in a really good mood today. I don't even know why... maybe because you came to talk to me?
  - _energetic:_ Today's awesome! I feel like I could run ten laps! Or eat ten crepes!
  - _cool:_ ...I'm fine. Better than fine, actually. Don't make it weird.
  - _teasing:_ Hm? You can tell I'm in a good mood? Then take advantage of it while it lasts~

### Lines / MoodLine / step Mood.Hungry
- **Her (by personality)**
  - _gentle:_ Um... sorry, my stomach keeps growling. I skipped breakfast...
  - _energetic:_ I'm STARVING. Like, I could eat the whole cafeteria. Wanna eat together?
  - _cool:_ I forgot my lunch. It's not a big deal. ...It's a little bit of a big deal.
  - _teasing:_ I'm hungry~ If someone bought me something, I'd be very grateful. Very.

### Lines / MoodLine / step Mood.Bored
- **Her (by personality)**
  - _gentle:_ Today feels so long... Nothing is happening. Is it okay if I talk with you for a while?
  - _energetic:_ Booooored! Let's do something! Anything! Please save me!
  - _cool:_ Classes today were tedious. Entertain me, if you can.
  - _teasing:_ I'm bored out of my mind. Got any ideas to make it... interesting?

### Lines / MoodLine / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ The tests are coming and I'm so behind... I'm a little scared.
  - _energetic:_ Ugh, my head's gonna explode! Too much homework!
  - _cool:_ I have too much on my plate. It's under control. Mostly.
  - _teasing:_ Everyone's getting on my nerves today. Except you, maybe. Maybe.

### Lines / MoodLine / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ Nobody really talked to me today... I'm glad you came.
  - _energetic:_ My friends are all busy today. Hang out with me, okay?
  - _cool:_ I was alone all morning. Not that I mind. ...Stay a bit.
  - _teasing:_ Oh, so you DO remember I exist. I was starting to wonder.

### Lines / MoodLine / step Mood.Flirty
- **Her (by personality)**
  - _gentle:_ I keep thinking about you today... is that strange?
  - _energetic:_ Hehe, you look kinda cool today. Did you do something different?
  - _cool:_ ...You're staring. I'm not saying stop.
  - _teasing:_ I'm feeling a little naughty today. Wanna find out how much?

### Lines / MoodLine / step Mood.Sleepy
- **Her (by personality)**
  - _gentle:_ Mm... sorry, I stayed up late reading. I can barely keep my eyes open...
  - _energetic:_ *yawn* I gamed until three. Totally worth it. Totally dying.
  - _cool:_ I didn't sleep. Speak slowly.
  - _teasing:_ So sleepy... wanna be my pillow?

### Lines / MoodLine / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ I'd... rather not talk about it right now. Sorry.
  - _energetic:_ Hmph! Today sucks and I don't wanna talk about it!
  - _cool:_ Leave me alone for now.
  - _teasing:_ Don't push your luck today. I'm not in the mood.
- **Her (by personality)**
  - _gentle:_ I'm okay, thank you for asking.
  - _energetic:_ Same old, same old!
  - _cool:_ Normal day.
  - _teasing:_ Why do you ask? Worried about me?

### Lines / LikeHint
- **Her (by personality)**
  - _gentle:_ By the way... I really like {likeLabel}. I don't tell many people that.
  - _energetic:_ Oh! Did I ever tell you I'm crazy about {likeLabel}? Like, crazy crazy!
  - _cool:_ If you must know, I have a weakness for {likeLabel}.
  - _teasing:_ Want a hint? I go weak for {likeLabel}. Use that information wisely~

### Lines / GiftLiked
- **Her (by personality)**
  - _gentle:_ Th-this is for me? {item}... You remembered what I like. Thank you so much!
  - _energetic:_ No way! {item}?! You're the best! Seriously the best!
  - _cool:_ ...{item}. You actually paid attention. I'll treasure it.
  - _teasing:_ Oh? {item}, huh? You know exactly how to make a girl happy~

### Lines / GiftNeutral
- **Her (by personality)**
  - _gentle:_ Oh, {item}... Thank you. It's very kind of you.
  - _energetic:_ Huh, {item}? Cool, thanks!
  - _cool:_ A gift? ...Thank you. I'll accept it.
  - _teasing:_ Aww, a present? It's not really my thing, but I'll take the thought.

### Lines / GiftIntimateYes
- **Her (by personality)**
  - _gentle:_ Th-this is... You want me to... for you? ...Okay. Only for you.
  - _energetic:_ Whoa, bold! ...Hehe, fine, I'll wear it. Just for you, got it?
  - _cool:_ ...You're shameless. I'll keep it. Don't expect me to say thanks out loud.
  - _teasing:_ Ooh~ So that's what you've been thinking about. I'll show you sometime.

### Lines / GiftIntimateNo
- **Her (by personality)**
  - _gentle:_ W-what?! I can't accept something like this! We're not... like that!
  - _energetic:_ Eh?! Dude, what is wrong with you?!
  - _cool:_ Absolutely not. Know your place.
  - _teasing:_ Wow. Bold move. Wrong girl. Try again in a few months.

### Lines / GiftAlreadyToday
- **Her (by personality)**
  - _gentle:_ You already gave me something today... I can't accept more, really.
  - _energetic:_ Another one? Save some for tomorrow!
  - _cool:_ One gift a day is plenty.
  - _teasing:_ Trying to buy me? One a day, cutie.

### Lines / PlayerGivesGift
- **Variants:** Here, I got you something. {item}. | This is for you. {item}. | I saw this and thought of you.

### Lines / RequestThanks
- **Her (by personality)**
  - _gentle:_ You really came... Thank you. That means a lot to me.
  - _energetic:_ You made it! Knew I could count on you!
  - _cool:_ You're on time. Good. I appreciate it.
  - _teasing:_ Took you long enough. ...Thanks, though.

### Lines / ErrandDelivered
- **Her (by personality)**
  - _gentle:_ You did it for me? Thank you, really! I owe you one.
  - _energetic:_ You're a lifesaver! Seriously!
  - _cool:_ Done already? Impressive. Thank you.
  - _teasing:_ Look at you, being useful. I like that.

### Lines / MeetAtLine
- **Her (by personality)**
  - _gentle:_ I like being here with you... the {place} feels different when you're around.
  - _energetic:_ See? The {place} is way more fun with company!
  - _cool:_ The {place} is quiet at this hour. I like that. And you're... fine, too.
  - _teasing:_ Just the two of us at the {place}~ Don't get any funny ideas. Or do.

### Lines / NoteHelp
- **Her (by personality)**
  - _gentle:_ { could you go over the main points with me?")}
  - _energetic:_ { Help!")}
  - _cool:_ {")}
  - _teasing:_ {")}
- **Variants:** Y-you came... I have to read my report in front of the class tomorrow and I freeze every time. Could you listen while I practice? | Th-thanks for coming. I lost the notes for tomorrow's test... could you go over the main points with me?
- **Variants:** You came! Okay, don't laugh: I signed up to give the club speech and I have no idea what to say. Help me write it? | Yay, you're here! I'm making the poster for the school festival and it looks like a ransom note. Help!
- **Variants:** Good, you're here. I'm stuck on the math homework and I'd rather not ask in class. Ten minutes? | You came. I need someone to time my run-through for the class presentation. Quietly.
- **Variants:** Hey, you actually came. I need a second opinion on the festival poster, and you're the only one who'll be honest. | Look who showed up. I'm writing the class newsletter and I need a real person to tell me if it's boring.

### Lines / NoteThanks
- **Her (by personality)**
  - _gentle:_ ...That really helped. I think I can do it now. Thank you.
  - _energetic:_ Done! You're the best. I owe you a juice!
  - _cool:_ That's settled, then. ...Thanks. I mean it.
  - _teasing:_ Okay, that's actually good now. Don't let it go to your head~

### Lines / AskHowAreYou
- **Variants:** Hi. How's your day going? | Hey, how are you today? | Hello. Having a good day?
- **Variants:** How are you today? | How's your day going? | You okay? How are you feeling today?

### Lines / MoodLine / step Mood.Cheerful
- **Her (by personality)**
  - _gentle:_ Oh, um... I'm doing well, thank you. It's been a nice day so far.
  - _energetic:_ Pretty great, actually! Thanks for asking!
  - _cool:_ Fine. Good, even.
  - _teasing:_ Good enough. Why, are you doing a survey?

### Lines / MoodLine / step Mood.Hungry
- **Her (by personality)**
  - _gentle:_ I'm a little hungry, to be honest... I skipped breakfast.
  - _energetic:_ Honestly? Starving. Is it lunch yet?
  - _cool:_ Hungry. I forgot my lunch.
  - _teasing:_ Hungry. If you're heading to the cafeteria, that's all I'll say.

### Lines / MoodLine / step Mood.Bored
- **Her (by personality)**
  - _gentle:_ It's been a slow day... nothing much is happening.
  - _energetic:_ Kinda bored! Today is dragging on forever.
  - _cool:_ Bored. Classes were tedious.
  - _teasing:_ Bored. You're not going to make it worse, are you?

### Lines / MoodLine / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ A little nervous... there's a lot of studying to do.
  - _energetic:_ Stressed! Way too much homework this week.
  - _cool:_ Busy. I have a lot to get through.
  - _teasing:_ Stressed. Long story, and we don't know each other that well yet.

### Lines / MoodLine / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ It's been a quiet day... I haven't really talked to anyone.
  - _energetic:_ My friends are all busy today, so it's been kinda quiet.
  - _cool:_ Quiet. I've been on my own most of the day.
  - _teasing:_ Quiet day. Not many people to talk to.

### Lines / MoodLine / step Mood.Flirty
- **Her (by personality)**
  - _gentle:_ I'm in a pretty good mood, actually. I feel like talking today.
  - _energetic:_ I'm in a super chatty mood today! Fair warning!
  - _cool:_ Talkative, for once. Don't get used to it.
  - _teasing:_ In a playful mood. Lucky you caught me today.

### Lines / MoodLine / step Mood.Sleepy
- **Her (by personality)**
  - _gentle:_ Sorry... I stayed up late. I'm a bit sleepy.
  - _energetic:_ *yawn* Sorry! Late night. I'm running on fumes.
  - _cool:_ Tired. I didn't sleep much.
  - _teasing:_ Sleepy. Keep it short, okay?

### Lines / MoodLine / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ I'm sorry, I'd rather not talk about it.
  - _energetic:_ Not great, honestly. I'd rather not get into it.
  - _cool:_ Not a good day. Let's leave it at that.
  - _teasing:_ Bad day. Don't take it personally.
- **Her (by personality)**
  - _gentle:_ I'm fine, thank you for asking.
  - _energetic:_ Doing okay! And you?
  - _cool:_ Fine.
  - _teasing:_ Fine. You're the curious type, huh?

### Lines / FriendMoodLine / step Mood.Cheerful
- **Her (by personality)**
  - _gentle:_ I'm in a really good mood today! It's nice to see you.
  - _energetic:_ Today's awesome! I feel like I could run ten laps! Or eat ten crepes!
  - _cool:_ I'm good. Really good, actually.
  - _teasing:_ Good mood today. Enjoy it while it lasts.

### Lines / FriendMoodLine / step Mood.Hungry
- **Her (by personality)**
  - _gentle:_ I skipped breakfast and my stomach won't let me forget it...
  - _energetic:_ I'm STARVING. Wanna grab lunch together?
  - _cool:_ I forgot my lunch. It's... a bit of a problem.
  - _teasing:_ Hungry. A good friend would know what to do with that information.

### Lines / FriendMoodLine / step Mood.Bored
- **Her (by personality)**
  - _gentle:_ Today feels so long... Can we talk for a while?
  - _energetic:_ Booooored! Let's do something! Anything!
  - _cool:_ Classes were dull today. Got anything interesting?
  - _teasing:_ Bored out of my mind. Entertain me, friend.

### Lines / FriendMoodLine / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ The tests are coming and I'm so behind... I'm a little scared.
  - _energetic:_ Ugh, my head's gonna explode! Too much homework!
  - _cool:_ I have too much on my plate. It's under control. Mostly.
  - _teasing:_ Everyone's getting on my nerves today. You get a pass.

### Lines / FriendMoodLine / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ Nobody really talked to me today... I'm glad you came by.
  - _energetic:_ Everyone's busy today. Hang out with me for a bit?
  - _cool:_ I was on my own all morning. ...You can stay a while.
  - _teasing:_ Oh, a friendly face. Finally.

### Lines / FriendMoodLine / step Mood.Flirty
- **Her (by personality)**
  - _gentle:_ I'm in such a good mood I can't sit still today.
  - _energetic:_ I'm super hyped today! Ask me anything!
  - _cool:_ I'm in a... talkative mood. Rare, I know.
  - _teasing:_ I'm feeling mischievous today. Watch out.

### Lines / FriendMoodLine / step Mood.Sleepy
- **Her (by personality)**
  - _gentle:_ Mm... sorry, I stayed up late reading. I can barely keep my eyes open...
  - _energetic:_ *yawn* I gamed until three. Totally worth it. Totally dying.
  - _cool:_ I didn't sleep. Speak slowly.
  - _teasing:_ So sleepy... if I nod off, wake me before the teacher sees.

### Lines / FriendMoodLine / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ I'd... rather not talk about it right now. Sorry.
  - _energetic:_ Today sucks and I don't wanna talk about it! ...Thanks for asking, though.
  - _cool:_ Bad day. Maybe I'll tell you later.
  - _teasing:_ Don't push it today. I'm not mad at you, though.
- **Her (by personality)**
  - _gentle:_ I'm okay, thanks for asking!
  - _energetic:_ Same old, same old!
  - _cool:_ Normal day.
  - _teasing:_ Same as always. Why, worried about me?

### Lines / AlreadyAsked
- **Her (by personality)**
  - _gentle:_ Um... I just told you. But thank you.
  - _energetic:_ You already asked! Still the same!
  - _cool:_ You asked already.
  - _teasing:_ You asked that already. Memory trouble?
- **Her (by personality)**
  - _gentle:_ I already told you, silly... but thanks for caring.
  - _energetic:_ Same as before! You worried about me?
  - _cool:_ You asked already.
  - _teasing:_ Again? You really like my company, huh?
- **Her (by personality)**
  - _gentle:_ I already told you, silly... but thank you for caring.
  - _energetic:_ Same as before! You worried about me?
  - _cool:_ You asked already.
  - _teasing:_ Again? You really can't stop thinking about me~

### Lines / LikeHint
- **Her (by personality)**
  - _gentle:_ Can I tell you something? I really love {likeLabel}.
  - _energetic:_ Oh! Did I ever tell you I'm crazy about {likeLabel}? Like, crazy crazy!
  - _cool:_ ...I have a weakness for {likeLabel}. Keep that between us.
  - _teasing:_ Want a hint? I'm a sucker for {likeLabel}. Use that wisely.
- **Her (by personality)**
  - _gentle:_ Oh, um... I like {likeLabel}, if you were wondering.
  - _energetic:_ Oh, and I'm really into {likeLabel}!
  - _cool:_ If you must know, I like {likeLabel}.
  - _teasing:_ I like {likeLabel}. Don't spread it around.

## Conversation (topics, Let her talk, Ask about, jokes, compliments, flirting)
`Talk/Conversation.cs`

### Conversation / Opening
- **Choice:** Promise to keep it secret. | Tease her about it.
- **Narration:** {WorriedHint} <color=#6ab4ff>[INT]</color> Ask her... > Just listen.

### Conversation / PickTopic
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Oh, um... that's nice, I guess.
  - _energetic:_ Huh, yeah, sure!
  - _cool:_ I see.
  - _teasing:_ Mm-hm. Fascinating. Kind of.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Um... could we talk about something else?
  - _energetic:_ Ehh, boring! Next topic!
  - _cool:_ Not interested.
  - _teasing:_ Wow. You really know how to kill a conversation.

### Conversation / LetHerTalk
- **Her:** {GossipLine}

### Conversation / AskSchool
- **Her:** {GossipLine}

### Conversation / Flirt
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you already said something like that...
  - _energetic:_ Okay, okay, I get it! Hehe!
  - _cool:_ Once is enough.
  - _teasing:_ Laying it on thick, aren't you?

### Conversation / MakePlans
- **You:** {PlayerMakePlans}

### Conversation / Opener / step Topic.Classes
- **Variants:** How are classes going for you? | Did you get today's homework?

### Conversation / Opener / step Topic.Club
- **Variants:** How's your club doing lately? | Anything new at your club?

### Conversation / Opener / step Topic.Food
- **Variants:** Had anything good to eat today? | What's your favorite thing at the cafeteria?

### Conversation / Opener / step Topic.Sports
- **Variants:** Do you follow any sports? | Do you work out at all?

### Conversation / Opener / step Topic.Hobbies
- **Variants:** What do you do in your free time? | Into anything fun lately?

### Conversation / Opener / step Topic.Rumors
- **Variants:** Heard any interesting rumors around school? | Do you know what everyone's whispering about?

### Conversation / Opener / step Topic.Weekend
- **Variants:** Got any plans for the weekend? | Did you do anything fun last weekend?

### Conversation / Opener / step Topic.Love
- **Variants:** So... is there anyone you like? | Have you ever had a crush on someone? | What's your idea of a perfect date?
- **Variants:** Tell me a bit about yourself. | What's been on your mind lately?

### Conversation / Good / step Topic.Classes
- **Her (by personality)**
  - _gentle:_ Classes are going well! I finally understood today's math lesson.
  - _energetic:_ Classes? Ugh. But okay, history was actually kinda cool today!
  - _cool:_ Fine. I like it when a teacher actually explains things properly.
  - _teasing:_ I aced the quiz. Go on, be impressed.

### Conversation / Good / step Topic.Club
- **Her (by personality)**
  - _gentle:_ The club is so much fun lately. Everyone has been really kind to me.
  - _energetic:_ The club's on fire! We're practicing like crazy!
  - _cool:_ We've been productive. I like that.
  - _teasing:_ My club? Of course it's going great. I'm in it.

### Conversation / Good / step Topic.Food
- **Her (by personality)**
  - _gentle:_ Oh, the cafeteria curry! I could eat it every day.
  - _energetic:_ Food? My favorite subject! The yakisoba bread is the best!
  - _cool:_ The set lunch is decent. I have opinions about the miso soup.
  - _teasing:_ Food talk? Now you're speaking my language.

### Conversation / Good / step Topic.Sports
- **Her (by personality)**
  - _gentle:_ I'm not very athletic, but I love cheering at games with friends.
  - _energetic:_ Sports! Yes! I ran my best time this morning!
  - _cool:_ I swim on weekends. It clears my head.
  - _teasing:_ Sports? Bet I could beat you at anything. Want to find out?

### Conversation / Good / step Topic.Hobbies
- **Her (by personality)**
  - _gentle:_ I've been reading a really good novel... I can't put it down.
  - _energetic:_ I got into a new game and I'm SO hooked!
  - _cool:_ I've been sketching. I'm not showing you. Yet.
  - _teasing:_ My hobbies? Collecting interesting people. You might qualify.

### Conversation / Good / step Topic.Rumors
- **Her (by personality)**
  - _gentle:_ Um, I heard the library is getting new books... does that count as a rumor?
  - _energetic:_ Oh oh oh! Did you hear about the class rep and the vending machine?!
  - _cool:_ ...I heard something. Fine, I'll tell you. Quietly.
  - _teasing:_ Rumors are my specialty. Lean in.

### Conversation / Good / step Topic.Weekend
- **Her (by personality)**
  - _gentle:_ I might bake something this weekend. Maybe cookies?
  - _energetic:_ Karaoke! I'm going to sing until my voice gives out!
  - _cool:_ A quiet weekend. Books, tea, maybe a walk.
  - _teasing:_ My weekend plans are secret. But they're good ones.
- **Her (by personality)**
  - _gentle:_ About me? Well... I've always wanted to see the sea in winter.
  - _energetic:_ Me? I want to try everything at least once! Skydiving, maybe!
  - _cool:_ ...Not many people ask. I want to study abroad someday.
  - _teasing:_ About me? Okay, one secret: I'm terrible at mornings.

## Conversation lines (openings, gossip, friends, romance, clearing the air)
`Dialogue/ConvoLines.cs`

### ConvoLines / Greeting
- **Her (by personality)**
  - _gentle:_ ...Oh. Hi.
  - _energetic:_ Hey. ...Sorry, not my best day.
  - _cool:_ What is it?
  - _teasing:_ Make it quick, okay?

### ConvoLines / Greeting / step 0
- **Her (by personality)**
  - _gentle:_ Ah, it's you! I was hoping you'd come find me.
  - _energetic:_ There you are! I was looking for you!
  - _cool:_ You came. ...Good.
  - _teasing:_ Took you long enough. I missed you. A little.

### ConvoLines / Greeting / step 1
- **Her (by personality)**
  - _gentle:_ Hi... my day just got better.
  - _energetic:_ Hey you! Perfect timing!
  - _cool:_ Hey. I saved some time for you.
  - _teasing:_ Oh? Coming to see your girlfriend? Good choice.
- **Her (by personality)**
  - _gentle:_ I was just thinking about you. Really!
  - _energetic:_ Yay! Talk to me, talk to me!
  - _cool:_ I'm glad you're here.
  - _teasing:_ Hmm~ what took you so long?

### ConvoLines / Greeting / step 0
- **Her (by personality)**
  - _gentle:_ Oh, hi! Do you have a minute?
  - _energetic:_ Yo! What's up?
  - _cool:_ Hey. What's going on?
  - _teasing:_ Well, well. Look who it is.

### ConvoLines / Greeting / step 1
- **Her (by personality)**
  - _gentle:_ Hello! It's nice to see you.
  - _energetic:_ Heeey! Perfect, I was bored!
  - _cool:_ Oh, it's you. Hi.
  - _teasing:_ Came to see me? Smart.
- **Her (by personality)**
  - _gentle:_ Ah, hi! How are you doing?
  - _energetic:_ Hey hey! Got something fun for me?
  - _cool:_ Hi. Need something?
  - _teasing:_ Hi there. Entertain me.

### ConvoLines / Greeting / step 0
- **Her (by personality)**
  - _gentle:_ Oh, um... hello.
  - _energetic:_ Oh, hey! Hi!
  - _cool:_ ...Yes?
  - _teasing:_ Hm? Can I help you?

### ConvoLines / Greeting / step 1
- **Her (by personality)**
  - _gentle:_ H-hello. Did you need something?
  - _energetic:_ Hi! You're from the club, right?
  - _cool:_ Hello.
  - _teasing:_ Oh, it's the club guy. Hi.
- **Her (by personality)**
  - _gentle:_ Ah, hi... nice weather today, isn't it?
  - _energetic:_ Hiya!
  - _cool:_ Do you need something?
  - _teasing:_ You look like you want to say something.

### ConvoLines / Jealous / step 0
- **Her (by personality)**
  - _gentle:_ You've been spending a lot of time with {friend} lately... it's fine. I just noticed, that's all.
  - _energetic:_ Hey! You hang out with {friend} every day now! What about me?!
  - _cool:_ You and {friend} are always together lately. I'm not complaining. I'm observing.
  - _teasing:_ Oh, you remembered me? I thought {friend} had adopted you.

### ConvoLines / Jealous / step 1
- **Her (by personality)**
  - _gentle:_ Um... did I do something wrong? You only talk to {friend} these days.
  - _energetic:_ It's always {friend}, {friend}, {friend}! I'm fun too, you know!
  - _cool:_ I'm glad you and {friend} get along. ...Don't forget the rest of us.
  - _teasing:_ Is {friend} that interesting? Should I be worried?
- **Her (by personality)**
  - _gentle:_ It's a little lonely when you're always with {friend}...
  - _energetic:_ No fair! {friend} gets you all to herself!
  - _cool:_ You've been busy. With {friend}, mostly.
  - _teasing:_ Let me guess, {friend} was busy today so you came to me?

### ConvoLines / Incident / step Social.Kinds.Defended
- **Her (by personality)**
  - _gentle:_ Thank you again for earlier. It really meant a lot.
  - _energetic:_ Hey! Thanks for helping me out, you were awesome!
  - _cool:_ About earlier. ...Thanks.
  - _teasing:_ You were pretty cool earlier. Don't let it go to your head.
- **Her (by personality)**
  - _gentle:_ {about} told me you helped her. That was really kind of you.
  - _energetic:_ I heard you helped {about}! Nice going!
  - _cool:_ Word is you helped {about}. Not bad.
  - _teasing:_ So you helped {about}? Trying to win points with everyone?

### ConvoLines / Incident / step Social.Kinds.KeptPromise
- **Her (by personality)**
  - _gentle:_ You really did what I asked. I won't forget that.
  - _energetic:_ You came through for me! You're the best!
  - _cool:_ You kept your word. I respect that.
  - _teasing:_ You actually did it. I'm impressed. Slightly.
- **Her (by personality)**
  - _gentle:_ {about} says you can be counted on.
  - _energetic:_ {about} won't stop saying how reliable you are!
  - _cool:_ I hear you keep your promises. Rare.
  - _teasing:_ {about} seems to think you're dependable. Is that true?

### ConvoLines / Incident / step Social.Kinds.GoodTalk
- **Her (by personality)**
  - _gentle:_ {about} said she had a nice talk with you.
  - _energetic:_ {about} says you're super fun to talk to!
  - _cool:_ I hear you're decent company.
  - _teasing:_ Apparently you're a good listener. Prove it.

### ConvoLines / Incident / step Social.Kinds.Rude
- **Her (by personality)**
  - _gentle:_ ...What you said earlier really hurt.
  - _energetic:_ Hey. That wasn't cool earlier.
  - _cool:_ I haven't forgotten what you said.
  - _teasing:_ Oh, you're talking to me now? After that?
- **Her (by personality)**
  - _gentle:_ {about} is my friend, you know. She was upset after talking to you.
  - _energetic:_ Why were you mean to {about}?! Not cool!
  - _cool:_ I heard how you treated {about}. Explain.
  - _teasing:_ So you're the one who made {about} cry? Charming.
- **Her (by personality)**
  - _gentle:_ I heard you were kind of mean to {about}...
  - _energetic:_ People say you were rude to {about}. Is that true?
  - _cool:_ Rumor says you were rude to {about}.
  - _teasing:_ I heard about you and {about}. Tsk.

### ConvoLines / Incident / step Social.Kinds.Ignored
- **Her (by personality)**
  - _gentle:_ You walked right past me earlier... I guess you were busy.
  - _energetic:_ You totally ignored me earlier! Rude!
  - _cool:_ You saw me struggling and kept walking.
  - _teasing:_ Nice of you to just walk by earlier.
- **Her (by personality)**
  - _gentle:_ {about} said you didn't stop to help her...
  - _energetic:_ You left {about} hanging?
  - _cool:_ I heard you walked past {about}.
  - _teasing:_ Not very gallant with {about}, were you?

### ConvoLines / Incident / step Social.Kinds.BrokePromise
- **Her (by personality)**
  - _gentle:_ You said you'd help... I waited.
  - _energetic:_ Hey! You forgot about me!
  - _cool:_ You didn't do what you said you would.
  - _teasing:_ I asked you for one thing. One.
- **Her (by personality)**
  - _gentle:_ {about} was let down. You promised her something.
  - _energetic:_ Dude, you bailed on {about}!
  - _cool:_ I heard you didn't keep your word to {about}.
  - _teasing:_ Breaking promises to {about}? Bad look.

### ConvoLines / Incident / step Social.Kinds.SeenGirlsRoom
- **Her (by personality)**
  - _gentle:_ Um... people are saying you were in the girls' restroom...?
  - _energetic:_ Okay, WHY were you in the girls' restroom?!
  - _cool:_ Explain the girls' restroom thing.
  - _teasing:_ So. The girls' restroom. I'm listening.

### ConvoLines / Incident / step Social.Kinds.SeenLockers
- **Her (by personality)**
  - _gentle:_ I heard someone saw you near the girls' changing room... that's not true, right?
  - _energetic:_ You were in the girls' changing room?! Are you crazy?!
  - _cool:_ The changing room. Care to explain?
  - _teasing:_ The girls' changing room, huh? Bold.

### ConvoLines / Incident / step Social.Kinds.HerBoysRoom
- **Her (by personality)**
  - _gentle:_ A-about earlier... in the restroom... please don't tell anyone. I got lost, I swear!
  - _energetic:_ Okay, listen! The boys' restroom thing! Total accident! You didn't see anything!
  - _cool:_ About the restroom. It was the closest one. We will never speak of it.
  - _teasing:_ You saw nothing in that restroom. Got it? ...Good.

### ConvoLines / Incident / step Social.Kinds.Flirted
- **Her (by personality)**
  - _gentle:_ What you said yesterday... I keep thinking about it.
  - _energetic:_ You were being kinda smooth earlier, huh?
  - _cool:_ About earlier... I didn't hate it.
  - _teasing:_ Still trying to charm me? Keep trying.
- **Her (by personality)**
  - _gentle:_ I heard you were being really sweet to {about}...
  - _energetic:_ So, you and {about}, huh? Spill!
  - _cool:_ I hear you've been charming {about}.
  - _teasing:_ Flirting with {about}? Interesting.

### ConvoLines / Cheating
- **Her (by personality)**
  - _gentle:_ People say you were sweet-talking {other}... I trust you, okay?
  - _energetic:_ Heard you were flirting with {other}! Hah, as long as you come back to me!
  - _cool:_ I hear things about you and {other}. I'm not the jealous type. Mostly.
  - _teasing:_ You and {other}? Cute. Just remember who you belong to~
- **Her (by personality)**
  - _gentle:_ Is it true...? You and {other}...? I thought we were...
  - _energetic:_ Hey! What's this about you and {other}?! I'm your girlfriend!
  - _cool:_ Explain {other}. Now.
  - _teasing:_ Flirting with {other} behind my back? You've got some nerve.

### ConvoLines / Vent / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ It's the exams... Everyone expects so much and I don't want to disappoint anyone. Sorry, I'm just dumping this on you.
  - _energetic:_ Everything's piling up! Homework, club, my mom... I just wanna scream into a pillow!
  - _cool:_ I have three deadlines this week and the teacher added a fourth. I'm handling it. I just needed to say it out loud.
  - _teasing:_ Everyone wants something from me today. You're the first person who just... listened.

### ConvoLines / Vent / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ I had a fight with a friend... I said something I didn't mean. Thank you for listening.
  - _energetic:_ Someone said something really mean about me and everyone laughed. Ugh. Thanks for not laughing.
  - _cool:_ A bad day. Things didn't go how I planned. ...Thanks for hearing me out.
  - _teasing:_ Fine, you want to know? Someone stood me up. There. Happy? ...Thanks, I guess.

### ConvoLines / Vent / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ Sometimes it feels like nobody would notice if I weren't here... Sorry. That's a gloomy thing to say.
  - _energetic:_ Everyone's got plans except me lately! It's kinda sad, right? Hehe...
  - _cool:_ My friends have been busy. It's quieter than I'd like.
  - _teasing:_ Don't laugh, but I've been eating lunch alone all week.
- **Her (by personality)**
  - _gentle:_ I'm just tired lately. Thank you for listening.
  - _energetic:_ Ugh, today was SO long!
  - _cool:_ It's been a long week.
  - _teasing:_ Life's being annoying. That's all.

### ConvoLines / VentThanks
- **Her (by personality)**
  - _gentle:_ ...I feel lighter now. Thank you.
  - _energetic:_ Okay! I feel way better! You're like a human stress ball!
  - _cool:_ That helped. Don't tell anyone I said that.
  - _teasing:_ You're surprisingly good at this.

### ConvoLines / Anecdote / step 0
- **Her (by personality)**
  - _gentle:_ This morning a little bird landed on my desk by the window. I didn't dare move all through first period.
  - _energetic:_ I tried to jump the school fence this morning because I was late. Spoiler: the fence won.
  - _cool:_ The vending machine gave me two drinks for the price of one today. I've decided it's a sign.
  - _teasing:_ A first-year asked me for directions and I sent him to the staff room on purpose. He'll thank me later.

### ConvoLines / Anecdote / step 1
- **Her (by personality)**
  - _gentle:_ My little sister made me a bento with a face on it. I couldn't bring myself to eat the eyes.
  - _energetic:_ I beat my high score in that rhythm game! My fingers still hurt!
  - _cool:_ I finished a thousand-piece puzzle yesterday. One piece was missing. I'm still angry.
  - _teasing:_ I convinced the cafeteria lady to give me extra karaage. Charm is a skill.

### ConvoLines / Anecdote / step 2
- **Her (by personality)**
  - _gentle:_ I found a really good bookstore near the station. It smells like old paper. I could live there.
  - _energetic:_ There's a new crepe stand near the park! I've already tried four flavors!
  - _cool:_ I started learning chess. The club president beat me in six moves. Six.
  - _teasing:_ I saw a cat wearing a tiny sweater on the way here. My day peaked at 7 a.m.

### ConvoLines / Anecdote / step 3
- **Her (by personality)**
  - _gentle:_ I've been practicing calligraphy. My characters still look like they're running away.
  - _energetic:_ Our club's practice match is coming up and I've been training every morning!
  - _cool:_ I reorganized my whole room by color. It's very satisfying. Don't judge.
  - _teasing:_ I've been writing a story. No, you can't read it. ...Maybe later.

### ConvoLines / Anecdote / step 4
- **Her (by personality)**
  - _gentle:_ I fell asleep on the train and woke up three stations late. I had to run all the way here...
  - _energetic:_ My phone fell in the pool yesterday. I'm living like a caveman now!
  - _cool:_ I tried cooking dinner for my family. They were very... polite.
  - _teasing:_ I dyed a streak of my hair last weekend. Nobody noticed. Do you?
- **Her (by personality)**
  - _gentle:_ The cherry tree behind the gym is starting to bud. I go look at it every morning.
  - _energetic:_ I signed up to help at the school festival! It's gonna be huge!
  - _cool:_ The library got the new volume of the series I like. I reserved it the second I saw it.
  - _teasing:_ I've been people-watching in the courtyard. You'd be amazed what you learn.

### ConvoLines / GossipLine / step Social.Kinds.Defended
- **Her (by personality)**
  - _gentle:_ Oh, {about} told everyone you helped her. She seemed really happy.
  - _energetic:_ Did you know {about} has been bragging that you helped her?
  - _cool:_ {about} mentioned you helped her. People noticed.
  - _teasing:_ {about} won't shut up about how you helped her. It's adorable.

### ConvoLines / GossipLine / step Social.Kinds.Rude
- **Her (by personality)**
  - _gentle:_ Um... {about} seemed hurt by something you said.
  - _energetic:_ Heads up: {about} is mad at you!
  - _cool:_ {about} is not happy with you. Just so you know.
  - _teasing:_ You know {about} is telling everyone you were rude, right?

### ConvoLines / GossipLine / step Social.Kinds.BrokePromise
- **Her (by personality)**
  - _gentle:_ {about} said you forgot something you promised her...
  - _energetic:_ {about} says you bailed on her!
  - _cool:_ {about} says you didn't keep your word.
  - _teasing:_ Rumor has it you stood up {about}.

### ConvoLines / GossipLine / step Social.Kinds.KeptPromise
- **Her (by personality)**
  - _gentle:_ {about} says she can always count on you.
  - _energetic:_ {about} says you're super reliable!
  - _cool:_ Apparently you keep your promises. {about} says so.
  - _teasing:_ {about} thinks you're dependable. Don't disappoint her.

### ConvoLines / GossipLine / step Social.Kinds.GoodTalk
- **Her (by personality)**
  - _gentle:_ {about} said talking with you was fun.
  - _energetic:_ {about} thinks you're a great listener!
  - _cool:_ {about} enjoyed your chat, I hear.
  - _teasing:_ {about} seems to like talking with you. Should I be jealous?

### ConvoLines / GossipLine / step Social.Kinds.Flirted
- **Her (by personality)**
  - _gentle:_ People say you and {about} are getting close...
  - _energetic:_ Everyone's talking about you and {about}!
  - _cool:_ There's talk about you and {about}.
  - _teasing:_ You and {about}, huh? The whole class knows.

### ConvoLines / GossipLine / step Social.Kinds.SeenLockers
- **Her (by personality)**
  - _gentle:_ There's a strange rumor going around about you... something about the girls' rooms.
  - _energetic:_ Dude, there's a weird rumor about you and the girls' rooms!
  - _cool:_ There's an unflattering rumor about you going around.
  - _teasing:_ You're famous now. For the wrong reasons.

### ConvoLines / NothingNew
- **Her (by personality)**
  - _gentle:_ Hmm... I can't think of anything new, sorry.
  - _energetic:_ Ehh, nothing's happening! Boring day!
  - _cool:_ Nothing worth mentioning.
  - _teasing:_ You've heard all my stories today.

### ConvoLines / FriendMood / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ {friend} looks so stressed about tests... maybe she'd like to study with someone.
  - _energetic:_ {friend}'s freaking out about homework!
  - _cool:_ {friend} is overwhelmed. Studying might help her.
  - _teasing:_ {friend} is a wreck about exams. Go play hero.

### ConvoLines / FriendMood / step Mood.Hungry
- **Her (by personality)**
  - _gentle:_ {friend} forgot her lunch today, I think.
  - _energetic:_ {friend} was starving this morning!
  - _cool:_ {friend} skipped breakfast again.
  - _teasing:_ {friend} is hungry. Feed her and she's yours.

### ConvoLines / FriendMood / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ {friend} has seemed a little lonely lately... maybe you could talk to her?
  - _energetic:_ {friend} looks kinda down! Go cheer her up!
  - _cool:_ {friend} could use company.
  - _teasing:_ {friend} is moping. You could fix that.

### ConvoLines / FriendMood / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ Something happened to {friend}... she's upset. Be gentle with her.
  - _energetic:_ {friend} is in a bad mood! Careful!
  - _cool:_ {friend} is upset about something.
  - _teasing:_ Don't poke {friend} today. Trust me.

### ConvoLines / FriendMood / step Mood.Cheerful
- **Her (by personality)**
  - _gentle:_ {friend} is in a great mood today.
  - _energetic:_ {friend} is bouncing off the walls today!
  - _cool:_ {friend} seems happy.
  - _teasing:_ {friend} is unbearably cheerful today.

### ConvoLines / FriendMood / step Mood.Bored
- **Her (by personality)**
  - _gentle:_ {friend} said she's bored.
  - _energetic:_ {friend} is SO bored, she said so ten times!
  - _cool:_ {friend} is bored out of her mind.
  - _teasing:_ {friend} is bored. Entertain her.

### ConvoLines / FriendMood / step Mood.Sleepy
- **Her (by personality)**
  - _gentle:_ {friend} nearly fell asleep in class.
  - _energetic:_ {friend} was snoring in math!
  - _cool:_ {friend} looks exhausted.
  - _teasing:_ {friend} was drooling on her notes. Cute.

### ConvoLines / FriendMood / step Mood.Flirty
- **Her (by personality)**
  - _gentle:_ {friend} seems to be in a really good mood today... maybe there's someone she likes?
  - _energetic:_ {friend} keeps smiling at her phone!
  - _cool:_ {friend} is acting strangely happy.
  - _teasing:_ {friend}'s got that look today. You know the one.
- **Her (by personality)**
  - _gentle:_ {friend}'s doing fine, I think.
  - _energetic:_ {friend}'s good! Same as always!
  - _cool:_ She's fine.
  - _teasing:_ {friend}? Same as ever.

### ConvoLines / FriendBond
- **Her (by personality)**
  - _gentle:_ {friend} and I have been friends for a long time. She's really important to me.
  - _energetic:_ {friend}'s my best friend! We do everything together!
  - _cool:_ {friend} and I understand each other. That's rare.
  - _teasing:_ {friend}? She's tolerable. ...She's my best friend, okay?

### ConvoLines / Rival
- **Her (by personality)**
  - _gentle:_ Um... {friend} and I don't really get along...
  - _energetic:_ Ugh, why ask ME about {friend}?!
  - _cool:_ I don't keep track of {friend}.
  - _teasing:_ Why are you asking about HER?

### ConvoLines / FriendProblem
- **Her (by personality)**
  - _gentle:_ Actually... {friend} has been worried. {problem} Maybe you could help her?
  - _energetic:_ Oh! {friend} has a problem! {problem} You should help her!
  - _cool:_ {friend} won't ask anyone, but: {problem} Just so you know.
  - _teasing:_ Want a tip? {problem} {friend} would never admit it, though.

### ConvoLines / Reputation
- **Her (by personality)**
  - _gentle:_ Everyone says you're really kind. I think so too.
  - _energetic:_ You're kinda famous! Everyone likes you!
  - _cool:_ You have a good reputation. Don't waste it.
  - _teasing:_ You're popular, you know. It's a little annoying.
- **Her (by personality)**
  - _gentle:_ People think you're nice, I think.
  - _energetic:_ People like you! Mostly!
  - _cool:_ People don't mind you.
  - _teasing:_ You're... known. In a good way. Mostly.
- **Her (by personality)**
  - _gentle:_ Um, not everyone knows you yet.
  - _energetic:_ Half the class doesn't know your name! Haha!
  - _cool:_ You're not very well known.
  - _teasing:_ You're a bit of a mystery. Not the cool kind.
- **Her (by personality)**
  - _gentle:_ Some people say... strange things about you. I don't believe them!
  - _energetic:_ People are saying weird stuff about you, dude.
  - _cool:_ Your reputation is poor. Work on it.
  - _teasing:_ Your reputation is in the gutter. Impressive, really.

### ConvoLines / JokeGood / step 0
- **Her (by personality)**
  - _gentle:_ Pfft... ahaha! That's so silly!
  - _energetic:_ AHAHA! Stop, stop, my stomach!
  - _cool:_ ...Heh. Okay, that one was good.
  - _teasing:_ Ugh, that's terrible. *snort* Dammit.

### ConvoLines / JokeGood / step 1
- **Her (by personality)**
  - _gentle:_ Hehe... you're funny.
  - _energetic:_ Hahaha! Tell me another!
  - _cool:_ I didn't expect to laugh. Well done.
  - _teasing:_ Fine. I laughed. Happy?
- **Her (by personality)**
  - _gentle:_ Ahaha... I'm going to think about that all day.
  - _energetic:_ Pfffft! You're the best!
  - _cool:_ Heh. Not bad at all.
  - _teasing:_ Okay, okay, you win this round.

### ConvoLines / JokeBad / step 0
- **Her (by personality)**
  - _gentle:_ Um... hehe...? Sorry, I don't think I got it.
  - _energetic:_ ...Was that the joke?
  - _cool:_ ...
  - _teasing:_ Wow. That was painful. For both of us.
- **Her (by personality)**
  - _gentle:_ Oh... um, that's... nice.
  - _energetic:_ Booo! Get off the stage!
  - _cool:_ I'll pretend you didn't say that.
  - _teasing:_ Did that sound funny in your head?

### ConvoLines / JokeHurt
- **Her (by personality)**
  - _gentle:_ That's... kind of mean, isn't it?
  - _energetic:_ Hey! Not funny!
  - _cool:_ That crossed a line.
  - _teasing:_ Careful. That one stung.

### ConvoLines / ComplimentGood
- **Her (by personality)**
  - _gentle:_ Eh?! Y-you think so...? Thank you...
  - _energetic:_ Hehe, really?! You noticed!
  - _cool:_ ...You're not bad yourself. Thanks.
  - _teasing:_ Oh? Took you long enough to notice.
- **Her (by personality)**
  - _gentle:_ R-really? That makes me really happy...
  - _energetic:_ Yay! Somebody noticed!
  - _cool:_ ...Thank you. I appreciate that.
  - _teasing:_ Finally, someone with taste.

### ConvoLines / ComplimentFlat
- **Her (by personality)**
  - _gentle:_ Oh... um, thank you, I guess.
  - _energetic:_ Haha, thanks? Random, but okay!
  - _cool:_ Okay.
  - _teasing:_ Flattery won't get you far with me.

### ConvoLines / ComplimentRepeat
- **Her (by personality)**
  - _gentle:_ You said that yesterday too...
  - _energetic:_ You already told me! Got a new one?
  - _cool:_ You're repeating yourself.
  - _teasing:_ Running out of material?

### ConvoLines / FlirtGood / step 0
- **Her (by personality)**
  - _gentle:_ M-me too... I'm so happy you said that.
  - _energetic:_ Aww! You're making me blush! Me too, okay?!
  - _cool:_ ...Me too. Don't make me say it twice.
  - _teasing:_ Hmph. Smooth talker. ...I feel the same.
- **Her (by personality)**
  - _gentle:_ You always know what to say... I love that about you.
  - _energetic:_ Ehehe! I'm the luckiest girl in school!
  - _cool:_ I like this. Us.
  - _teasing:_ Keep talking like that and I might fall for you all over again.
- **Her (by personality)**
  - _gentle:_ Eh...? I-I like spending time with you too...
  - _energetic:_ Wh-whoa, where did that come from?! ...Thanks, hehe.
  - _cool:_ ...You're making me lose my composure.
  - _teasing:_ Oh my. Are you trying to make me blush?
- **Her (by personality)**
  - _gentle:_ That's... a really nice thing to say.
  - _energetic:_ Haha, you're so straightforward! I like it!
  - _cool:_ ...I don't dislike hearing that.
  - _teasing:_ Careful. I might start believing you.

### ConvoLines / FlirtTooSoon
- **Her (by personality)**
  - _gentle:_ Um... we don't know each other that well yet...
  - _energetic:_ Whoa, slow down there, buddy!
  - _cool:_ ...That's a bit much.
  - _teasing:_ Nice try. You'll have to work harder than that.

### ConvoLines / LoveTalk
- **Her (by personality)**
  - _gentle:_ Being your girlfriend... I still can't believe it sometimes.
  - _energetic:_ Everyone's jealous of us, you know! Hehe!
  - _cool:_ I've been thinking about where we could go on our next date.
  - _teasing:_ So, what's the plan for our next date? It better be good.
- **Her (by personality)**
  - _gentle:_ Do you... have someone you like?
  - _energetic:_ So! Do you have a crush on anyone? Tell me!
  - _cool:_ Have you ever been in love?
  - _teasing:_ Who do you like? Come on, I won't tell.

### ConvoLines / ExplainOk
- **Her (by personality)**
  - _gentle:_ Oh... I see. I'm sorry I doubted you.
  - _energetic:_ Ohhh! Okay, that makes sense! My bad!
  - _cool:_ ...Fine. That's reasonable.
  - _teasing:_ Hmph. Fine. I'll let it slide. This time.

### ConvoLines / ExplainFail
- **Her (by personality)**
  - _gentle:_ I... I don't know if I believe that.
  - _energetic:_ Uh-huh. Sure. Totally.
  - _cool:_ That's not convincing.
  - _teasing:_ Do you hear yourself?

### ConvoLines / ApologyAccepted
- **Her (by personality)**
  - _gentle:_ ...Okay. Thank you for saying that.
  - _energetic:_ Okay, okay! Apology accepted! Just don't do it again!
  - _cool:_ Fine. Apology accepted.
  - _teasing:_ Hmph. You owe me one.

### ConvoLines / PlansAccepted
- **Her (by personality)**
  - _gentle:_ Really? Then... I'll be looking forward to it.
  - _energetic:_ Promise?! Okay! Lunch together sometime!
  - _cool:_ ...Okay. I'll hold you to that.
  - _teasing:_ You'd better mean it.

### ConvoLines / KeepSecret
- **Her (by personality)**
  - _gentle:_ Thank you... I really owe you one.
  - _energetic:_ You're a lifesaver! Our secret, okay?!
  - _cool:_ Good. ...Thanks.
  - _teasing:_ Smart choice. I'll remember this.
- **Her (by personality)**
  - _gentle:_ You're so mean...!
  - _energetic:_ Noooo! You wouldn't!
  - _cool:_ Not funny.
  - _teasing:_ Try it and see what happens.

### ConvoLines / OutOfPatience
- **Her (by personality)**
  - _gentle:_ Um, sorry, I should get going soon...
  - _energetic:_ Ah, I gotta run! Later!
  - _cool:_ I need to go.
  - _teasing:_ That's enough for now. I have things to do.

### ConvoLines / Bye
- **Her (by personality)**
  - _gentle:_ That was really fun. Thank you for talking with me.
  - _energetic:_ That was awesome! Let's do this again!
  - _cool:_ ...That was a good talk.
  - _teasing:_ Not bad. You're more interesting than you look.
- **Her (by personality)**
  - _gentle:_ Thanks for chatting with me.
  - _energetic:_ Fun chat!
  - _cool:_ That was fine.
  - _teasing:_ Okay, that wasn't terrible.
- **Her (by personality)**
  - _gentle:_ Um... see you.
  - _energetic:_ Ehh, I'm kinda tired of talking.
  - _cool:_ Bye.
  - _teasing:_ Well. That happened.

### ConvoLines / TooTired
- **Her (by personality)**
  - _gentle:_ Sorry, I don't really have time to talk right now...
  - _energetic:_ Ugh, can we talk later? I'm beat!
  - _cool:_ Not now.
  - _teasing:_ I've talked enough today. Come back tomorrow.

## Romance and H (heart page: hands, teasing, whispers, last time, sneaking off)
`Dialogue/RomanceLines.cs`

### RomanceLines / PlayerHand
- **Variants:** Come here. Give me your hand. | I missed holding your hand. | Let's walk like this for a while.
- **Variants:** Can I... hold your hand for a bit? | Your hands look cold. Here. | Would it be weird if I held your hand?

### RomanceLines / HandYes / step 0
- **Her (by personality)**
  - _gentle:_ Mm... your hand is so warm. I could stay like this all day.
  - _energetic:_ Hehe, fingers laced! That's the official couple grip!
  - _cool:_ ...Don't let go yet.
  - _teasing:_ Holding hands in public? How bold. ...I'm not complaining.

### RomanceLines / HandYes / step 1
- **Her (by personality)**
  - _gentle:_ Every time we do this my heart races like the first time...
  - _energetic:_ Swing, swing! Ehehe, I love this!
  - _cool:_ Your hand fits mine. Funny how that works.
  - _teasing:_ Squeeze back if you love me. ...Hm. Good answer.
- **Her (by personality)**
  - _gentle:_ People are looking... but I don't mind. Not with you.
  - _energetic:_ Let's walk the long way so this lasts longer!
  - _cool:_ ...This is nice. Don't tell anyone I said that.
  - _teasing:_ If you let go first, you lose. Got it?

### RomanceLines / HandYes / step 0
- **Her (by personality)**
  - _gentle:_ Eh...? O-okay... just for a little while...
  - _energetic:_ Whoa! Um. Okay! Sure! This is fine! Totally fine!
  - _cool:_ ...Fine. Your hand's warmer than I expected.
  - _teasing:_ Oh? Taking the initiative today? ...I'll allow it.

### RomanceLines / HandYes / step 1
- **Her (by personality)**
  - _gentle:_ My face is so hot right now... please don't look...
  - _energetic:_ Ahaha, my hand's all sweaty, sorry! ...Don't let go though.
  - _cool:_ ...I suppose it's not unpleasant.
  - _teasing:_ Careful. If you hold on too long, I might not give it back.
- **Her (by personality)**
  - _gentle:_ It's... kind of nice, isn't it?
  - _energetic:_ Okay, this is kinda cute. Don't make it weird!
  - _cool:_ Hm. Only until the end of the hallway.
  - _teasing:_ You're shaking a little. Nervous? How cute.

### RomanceLines / HandNo
- **Her (by personality)**
  - _gentle:_ Ah... s-sorry, not here... people might see...
  - _energetic:_ Whoa, whoa! Hands to yourself, mister!
  - _cool:_ No.
  - _teasing:_ Holding hands already? Earn it first.

### RomanceLines / PlayerAskFeelings
- **Variants:** Can I ask you something? What do you think of me? | Be honest. How do you see me? | What am I to you, exactly?

### RomanceLines / Feelings
- **Her (by personality)**
  - _gentle:_ You're my boyfriend... and the only one who gets to see me like... that. Don't make me say more!
  - _energetic:_ You're my favorite person! And, um... I kinda can't stop thinking about you. In every way. Ehehe...
  - _cool:_ You're mine. All of you. That's what I think.
  - _teasing:_ You're my boyfriend and my favorite toy. Is that honest enough for you?
- **Her (by personality)**
  - _gentle:_ You're the most important person in the world to me. That's all.
  - _energetic:_ You're my boyfriend, silly! The best one in the whole school!
  - _cool:_ You're the person I chose. I don't regret it.
  - _teasing:_ You're mine. Isn't that obvious? Or do you just like hearing it?
- **Her (by personality)**
  - _gentle:_ I... I think about you a lot. More than I should, maybe...
  - _energetic:_ Honestly? You're kind of my favorite person right now. Don't let it go to your head!
  - _cool:_ You're... important to me. More than I planned.
  - _teasing:_ Hmm... let's just say if you asked me out, I wouldn't say no right away.
- **Her (by personality)**
  - _gentle:_ You're a really good friend. I'm glad we met.
  - _energetic:_ You're super fun to be around! Like, top three people, easy!
  - _cool:_ You're reliable. I like that.
  - _teasing:_ You're interesting. That's rare around here. Keep it up.
- **Her (by personality)**
  - _gentle:_ Um... you seem nice? I don't know you that well yet...
  - _energetic:_ Hmm, you're okay! We should hang out more, then I'll tell you!
  - _cool:_ I don't have an opinion yet.
  - _teasing:_ Ask me again when you've done something worth talking about.

### RomanceLines / PlayerTease
- **Variants:** You know what you look like right now? Very kissable. | That skirt's a little short today. Not that I'm complaining. | Is it hot in here, or is it just you?
- **Variants:** You've been staring at me, haven't you? | Careful, if you keep smiling like that I'll get ideas. | You look really good today. Like, distractingly good.

### RomanceLines / TeaseGood / step 0
- **Her (by personality)**
  - _gentle:_ Y-you can't just say things like that at school...! ...Say it again later.
  - _energetic:_ Ehehe, you dummy! ...Okay, maybe a little kiss. Later!
  - _cool:_ ...Keep that energy for when we're alone.
  - _teasing:_ Kissable? Then why are you still talking?

### RomanceLines / TeaseGood / step 1
- **Her (by personality)**
  - _gentle:_ I wore it for you... is that embarrassing?
  - _energetic:_ Hey! Eyes up here! ...Or not. I'm not your boss. Hehe.
  - _cool:_ Noticed, did you. Good.
  - _teasing:_ Short? Oh, you mean this? *She tugs the hem up a little, then lets it fall.* Oops.
- **Her (by personality)**
  - _gentle:_ It's you... you make me feel hot all over...
  - _energetic:_ Hot?! Wh- you're the one making it hot! Jerk! ...Hehe.
  - _cool:_ It's me. Obviously.
  - _teasing:_ Both. Want to find out which is hotter?

### RomanceLines / TeaseGood / step 0
- **Her (by personality)**
  - _gentle:_ I-I wasn't staring! ...Maybe a little.
  - _energetic:_ Me?! Staring?! Pfft, no way! ...Okay, a tiny bit.
  - _cool:_ ...You noticed. Annoying.
  - _teasing:_ And if I was? What would you do about it?

### RomanceLines / TeaseGood / step 1
- **Her (by personality)**
  - _gentle:_ Ideas...? W-what kind of ideas...?
  - _energetic:_ Ideas, huh? Hehe, you're kinda bold! I like it!
  - _cool:_ Ideas. Hm. Keep them to yourself for now.
  - _teasing:_ Get them, then. I want to see what you come up with.
- **Her (by personality)**
  - _gentle:_ Th-thank you... I tried a little harder today...
  - _energetic:_ Distractingly?! Ahaha, I'll take it!
  - _cool:_ ...I'll accept the compliment.
  - _teasing:_ Distracting you was the plan. Glad it's working.

### RomanceLines / TeaseBad
- **Her (by personality)**
  - _gentle:_ Th-that's embarrassing... please don't say that...
  - _energetic:_ Ew, gross! ...Kidding. But no.
  - _cool:_ That was unnecessary.
  - _teasing:_ Is that the best you've got? Pathetic. Try again someday.

### RomanceLines / PlayerWhisper
- **Variants:** *You lean in.* I can't stop thinking about last time. About your skin. | *Quietly.* I want you. Right now. Here. | *In her ear.* You smell amazing. I want to taste you.
- **Variants:** *You lean in.* You have no idea what I'm thinking about right now. | *Quietly.* I keep imagining what you look like without the uniform. | *In her ear.* If we were alone right now...

### RomanceLines / WhisperGood / step 0
- **Her (by personality)**
  - _gentle:_ Hah... d-don't breathe on my ear like that... my legs are going weak...
  - _energetic:_ Ahh, stop it! I'm getting all tingly! ...Don't stop.
  - _cool:_ ...Careful. I might drag you somewhere private.
  - _teasing:_ Taste me? Then hurry up and take me somewhere nobody can hear.

### RomanceLines / WhisperGood / step 1
- **Her (by personality)**
  - _gentle:_ Me too... I keep remembering your hands on me...
  - _energetic:_ Ehehe, same! I even dreamt about it! ...Was that too honest?
  - _cool:_ Then don't make me wait.
  - _teasing:_ Right now? Here? ...You're lucky I like it when you're reckless.
- **Her (by personality)**
  - _gentle:_ I-I'm all yours... just not in front of everyone...
  - _energetic:_ Ahh, my face is on fire! You're the worst! ...I love it.
  - _cool:_ ...You'll pay for saying that in a hallway. Later.
  - _teasing:_ Mm, keep whispering. I want to see how red you can make me.

### RomanceLines / WhisperGood / step 0
- **Her (by personality)**
  - _gentle:_ W-without the uniform?! ...D-don't imagine too hard...
  - _energetic:_ Hey! Pervert! ...What were you imagining, exactly?
  - _cool:_ ...That's indecent. Continue.
  - _teasing:_ Imagining is free. Seeing costs extra.

### RomanceLines / WhisperGood / step 1
- **Her (by personality)**
  - _gentle:_ If we were alone... I-I don't know what I'd do...
  - _energetic:_ Alone, huh? Then you'd better make it worth my time!
  - _cool:_ If we were alone, you'd find out.
  - _teasing:_ If we were alone, you'd already be on your knees.
- **Her (by personality)**
  - _gentle:_ My heart's beating so fast... you can't say that so close...
  - _energetic:_ Ahaha! Ticklish! And, um... kinda hot. Don't tell anyone!
  - _cool:_ ...You're bold. I'll remember it.
  - _teasing:_ Oh? Tell me more. In detail.

### RomanceLines / WhisperBad
- **Her (by personality)**
  - _gentle:_ Eek! I-I don't know you like that...!
  - _energetic:_ Whoa, way too much, way too fast! Down, boy!
  - _cool:_ Do that again and I'm reporting you.
  - _teasing:_ Cute. But you haven't earned the right to talk to me like that.

### RomanceLines / PlayerMemories
- **Variants:** I keep thinking about our first time. | Do you ever think about... that day? | That day... I haven't forgotten a single thing.
- **Variants:** I keep replaying last time in my head. | You were incredible the other day. | When can we do that again?

### RomanceLines / Memories
- **Her (by personality)**
  - _gentle:_ Our first time... I was so nervous, but you were gentle. I'll treasure it forever.
  - _energetic:_ Ahh, don't bring it up out of nowhere! ...It was amazing, okay? Way better than I imagined!
  - _cool:_ ...I think about it more than I'd like to admit.
  - _teasing:_ You were so nervous. It was adorable. Next time, I'm in charge.

### RomanceLines / Memories / step 0
- **Her (by personality)**
  - _gentle:_ Me too... I can't focus in class when I remember your voice...
  - _energetic:_ Hehe, you mean when I couldn't stop making noise? So embarrassing! ...Let's do it again!
  - _cool:_ You weren't bad. I want more.
  - _teasing:_ Incredible? I was holding back. Want to see what I can really do?

### RomanceLines / Memories / step 1
- **Her (by personality)**
  - _gentle:_ I still blush when I walk past... that place...
  - _energetic:_ I still have a mark, you know! Right here. *She tugs her collar.* Your fault!
  - _cool:_ ...Same place, next time. I liked it there.
  - _teasing:_ Again? You're insatiable. ...Good. So am I.
- **Her (by personality)**
  - _gentle:_ Whenever you want... I-I mean... soon, please...
  - _energetic:_ Soon! Tomorrow! Today?! Okay, I'm too excited, sorry!
  - _cool:_ When I say so. Which might be soon.
  - _teasing:_ Beg nicely and I'll think about it.

### RomanceLines / PlayerSneak
- **Variants:** Hey... want to sneak off somewhere quiet? Just the two of us. | I know a spot where nobody goes. Come with me? | Let's skip the next bit and find somewhere private.

### RomanceLines / SneakYes / step 0
- **Her (by personality)**
  - _gentle:_ ...Okay. Lead the way. Just... be gentle, okay?
  - _energetic:_ Ooh, a secret mission! I'm in! Don't get us caught!
  - _cool:_ Fine. But if a teacher finds us, it was your idea.
  - _teasing:_ Finally. I was wondering how long you'd make me wait.
- **Her (by personality)**
  - _gentle:_ M-my heart won't calm down... let's go before I lose my nerve...
  - _energetic:_ Race you there! Loser has to... ehehe, you'll see!
  - _cool:_ ...Somewhere with a lock on the door.
  - _teasing:_ Somewhere private? You read my mind. Hurry.

### RomanceLines / SneakNo
- **Her (by personality)**
  - _gentle:_ N-not today... I'm not ready for something like that...
  - _energetic:_ Ehh? Sneak off? With you? Pfft, no way! ...Not yet, anyway.
  - _cool:_ No. Don't push your luck.
  - _teasing:_ Sneak off with you? You wish. Try harder.

### RomanceLines / PlayerSweet
- **Variants:** I was looking forward to seeing you all day. | You're the best part of my day, you know. | Being with you is my favorite thing. | I keep thinking about our last date. | You look cute when you're thinking hard about something. | I'd skip every class just to sit here with you.
- **Variants:** I really like spending time with you. | You're kind of amazing, you know that? | Talking to you always cheers me up. | I'm glad I met you. | Your laugh is really cute. | I find myself looking for you in the hallway.

## Two-girl scenes
`Systems/Duo.cs`

### Plan / Compose
- **Her (by personality)**
  - _gentle:_ Um... {bn} told me what you said to her. That wasn't very nice...
  - _energetic:_ Hey! What's the big idea, being mean to {bn}?!
  - _cool:_ I heard how you treated {bn}. Explain.
  - _teasing:_ So you were rude to my friend. Want to try that again?
- **Her (by personality)**
  - _gentle:_ You promised {bn} something... and didn't keep it. She was really waiting...
  - _energetic:_ Hey! You stood {bn} up! Not cool!
  - _cool:_ You gave {bn} your word. Then nothing.
  - _teasing:_ Breaking promises to my friend? Bold move.
- **Her (by personality)**
  - _gentle:_ I-it's fine, really... you don't have to...
  - _energetic:_ Ehh, I'm not THAT mad... okay, a little.
  - _cool:_ ...I can speak for myself. But yes.
  - _teasing:_ Hmph. Let him squirm a little.
- **Her (by personality)**
  - _gentle:_ ...Okay. Thank you for saying that.
  - _energetic:_ Apology accepted! Just this once!
  - _cool:_ ...Accepted.
  - _teasing:_ Fine. You're forgiven. Barely.
- **Her (by personality)**
  - _gentle:_ See? That wasn't so hard.
  - _energetic:_ Good! Now we're all friends again!
  - _cool:_ Good.
  - _teasing:_ Hm. Respectable.
- **Her (by personality)**
  - _gentle:_ Oh... I didn't know that...
  - _energetic:_ Ohh, so THAT's what happened!
  - _cool:_ ...That changes things.
  - _teasing:_ Hm. Alright, that's a decent excuse.
- **Her (by personality)**
  - _gentle:_ ...I see. Come on, {bn}.
  - _energetic:_ Wow. Unbelievable. Let's go, {bn}!
  - _cool:_ Noted. Let's go.
  - _teasing:_ Charming. Come on, {bn}.
- **Her (by personality)**
  - _gentle:_ Go on, {bn}... you wanted to say something, right?
  - _energetic:_ C'mon, {bn}, tell him! You've been talking about it all day!
  - _cool:_ She has something to tell you.
  - _teasing:_ Go on. Don't be shy now~
- **Her (by personality)**
  - _gentle:_ Thank you... for taking my side. It meant a lot.
  - _energetic:_ Thanks for having my back! You're the best!
  - _cool:_ ...Thank you for standing up for me.
  - _teasing:_ So. You took my side. ...Thanks, I guess.
- **Her (by personality)**
  - _gentle:_ Thank you for helping me the other day...
  - _energetic:_ Thanks for the help! Seriously!
  - _cool:_ Thank you for your help. I mean it.
  - _teasing:_ You helped me out. I owe you one... maybe.
- **Her (by personality)**
  - _gentle:_ It wasn't nothing to me...
  - _energetic:_ Don't be so humble!
  - _cool:_ ...Still. Thanks.
  - _teasing:_ Modest, too? Hm.
- **Her (by personality)**
  - _gentle:_ Ehehe... he's reliable, isn't he?
  - _energetic:_ See?! Told you he's great!
  - _cool:_ Reliable. Good.
  - _teasing:_ Ooh, smooth.
- **Her (by personality)**
  - _gentle:_ Um, I was hoping to talk to you a little...
  - _energetic:_ Hey! I wanted to hang out with you!
  - _cool:_ I wanted a word with you.
  - _teasing:_ Mind if I borrow him for a bit?
- **Her (by personality)**
  - _gentle:_ Ah... he's with me today, sorry...
  - _energetic:_ Hey, hey! He's MY boyfriend, you know!
  - _cool:_ He's busy. With me.
  - _teasing:_ Borrow? He's not a library book. He's mine.
- **Her (by personality)**
  - _gentle:_ ...Thank you.
  - _energetic:_ Hehe! See?
  - _cool:_ Good answer.
  - _teasing:_ Heh. Knew it.
- **Her (by personality)**
  - _gentle:_ I see... sorry for interrupting.
  - _energetic:_ Aw, okay! Another time!
  - _cool:_ ...Understood.
  - _teasing:_ Hmph. Fine. For now.
- **Her (by personality)**
  - _gentle:_ ...Okay. If you say so.
  - _energetic:_ Fiiine. Together.
  - _cool:_ ...Fine.
  - _teasing:_ Generous of you. Hmph.
- **Her (by personality)**
  - _gentle:_ Thank you! I'm happy.
  - _energetic:_ Yay!
  - _cool:_ Thanks.
  - _teasing:_ How kind~
- **Variants:** the last melon bread | whose turn it was to clean the classroom | who gets the window seat | the club room schedule | a borrowed manga that came back bent
- **Her (by personality)**
  - _gentle:_ You saw it too, right? It was my turn...
  - _energetic:_ Tell her I'm right!
  - _cool:_ You're impartial. Settle this.
  - _teasing:_ Go on, tell her she's wrong.
- **Her (by personality)**
  - _gentle:_ Please, say something...
  - _energetic:_ No way, I'm right! Right?!
  - _cool:_ This is ridiculous. Decide.
  - _teasing:_ Oh, this'll be good. Choose wisely.
- **Her (by personality)**
  - _gentle:_ Thank you...!
  - _energetic:_ Ha! Told you!
  - _cool:_ Obviously.
  - _teasing:_ See?
- **Her (by personality)**
  - _gentle:_ ...I see how it is.
  - _energetic:_ Traitor!
  - _cool:_ ...Noted.
  - _teasing:_ Wow. Okay.
- **Her (by personality)**
  - _gentle:_ Thank you...!
  - _energetic:_ Ha! Told you!
  - _cool:_ Obviously.
  - _teasing:_ See?
- **Her (by personality)**
  - _gentle:_ ...I see how it is.
  - _energetic:_ Traitor!
  - _cool:_ ...Noted.
  - _teasing:_ Wow. Okay.
- **Her (by personality)**
  - _gentle:_ That's... actually fair.
  - _energetic:_ Huh. Okay, fine!
  - _cool:_ Acceptable.
  - _teasing:_ Hmph. Smart.
- **Her (by personality)**
  - _gentle:_ This is my friend {nn}! I wanted you two to meet.
  - _energetic:_ Meet {nn}, my best friend! Be nice!
  - _cool:_ This is {nn}. You should know each other.
  - _teasing:_ Here, I brought you a friend. Her name's {nn}.
- **Her (by personality)**
  - _gentle:_ N-nice to meet you...
  - _energetic:_ Heya! I've heard about you!
  - _cool:_ Hello.
  - _teasing:_ So you're the famous club guy. Hm.
- **Her (by personality)**
  - _gentle:_ You're nice... I'm glad.
  - _energetic:_ Ooh, I like him!
  - _cool:_ ...Pleasure.
  - _teasing:_ Polite. I approve.
- **Her (by personality)**
  - _gentle:_ Um... he's nicer than he looks, I promise!
  - _energetic:_ Hey, be friendlier!
  - _cool:_ ...Charming as always.
  - _teasing:_ Wow, so cool and mysterious.
- **Her (by personality)**
  - _gentle:_ You and {jn} are always together lately... can I join sometimes?
  - _energetic:_ Hey! You keep stealing {jn} from me! Or me from... wait. Whatever!
  - _cool:_ You two have been inseparable. I've noticed.
  - _teasing:_ So you've been monopolizing {jn}. Or is it the other way around?
- **Her (by personality)**
  - _gentle:_ Ah, sorry! I didn't mean to leave you out...
  - _energetic:_ Haha, don't be jealous!
  - _cool:_ ...It's not like that.
  - _teasing:_ Jealous? How cute.
- **Her (by personality)**
  - _gentle:_ Really? Okay!
  - _energetic:_ Yes! Group hangout!
  - _cool:_ ...Fine. Good.
  - _teasing:_ Deal.
- **Her (by personality)**
  - _gentle:_ J-just us? Okay...!
  - _energetic:_ A date? Kidding! Lunch it is!
  - _cool:_ ...I'll hold you to it.
  - _teasing:_ Oh? Now that's interesting.
- **Her (by personality)**
  - _gentle:_ ...Oh. Okay.
  - _energetic:_ Wow. Okay then!
  - _cool:_ ...Right.
  - _teasing:_ Noted. Very noted.
- **Her (by personality)**
  - _gentle:_ It's true, I heard it too...
  - _energetic:_ Totally true! Everyone knows!
  - _cool:_ That's what they say.
  - _teasing:_ The whole school's talking.
- **Her (by personality)**
  - _gentle:_ We were just talking about you. Only good things!
  - _energetic:_ Hey! We need a tiebreaker: cats or dogs?
  - _cool:_ We were discussing the festival. Opinions?
  - _teasing:_ Speak of the devil.
- **Her (by personality)**
  - _gentle:_ Hehe, it's true.
  - _energetic:_ Answer carefully!
  - _cool:_ Your input is required.
  - _teasing:_ Go on, say something interesting.
- **Her (by personality)**
  - _gentle:_ I knew it!
  - _energetic:_ YES!
  - _cool:_ Correct.
  - _teasing:_ Good taste.
- **Her (by personality)**
  - _gentle:_ I knew it!
  - _energetic:_ YES!
  - _cool:_ Correct.
  - _teasing:_ Good taste.

### Show / CallOver
- **Her (by personality)**
  - _gentle:_ Um... do you have a minute?
  - _energetic:_ Hey hey! Got a sec?
  - _cool:_ A moment of your time.
  - _teasing:_ Hey, you. Come here a sec.
- **Her (by personality)**
  - _gentle:_ We wanted to talk to you...
  - _energetic:_ C'mon, it'll be quick!
  - _cool:_ It won't take long.
  - _teasing:_ Don't keep us waiting~

### Show / Outro
- **Her (by personality)**
  - _gentle:_ Thank you for listening...
  - _energetic:_ That was fun! See ya!
  - _cool:_ That's all. Thanks.
  - _teasing:_ Well, that was something. Later~
- **Her (by personality)**
  - _gentle:_ S-see you later...
  - _energetic:_ Bye bye!
  - _cool:_ Until next time.
  - _teasing:_ Don't miss us too much.

## Club board requests (scenes when a request is completed)
`Systems/Board.cs`

### Board / ProblemFirst / step RequestKind.Lunch
- **Her (by personality)**
  - _gentle:_ I, um... forgot my lunch today. And I don't really like eating alone...
  - _energetic:_ I forgot my bento! And nobody to eat with! Tragic!
  - _cool:_ I forgot my lunch. Eating alone is fine. ...Mostly.
  - _teasing:_ Lunch alone again today. How sad for me~

### Board / ProblemFirst / step RequestKind.Study
- **Her (by personality)**
  - _gentle:_ I don't understand today's lesson at all... I'm scared about the test.
  - _energetic:_ I'm SO lost in class! Help me study?!
  - _cool:_ I'm behind in class. I'd study better with someone.
  - _teasing:_ The test is going to destroy me. Unless someone helps.

### Board / ProblemFirst / step RequestKind.Exercise
- **Her (by personality)**
  - _gentle:_ I want to get a little stronger... but working out alone is hard.
  - _energetic:_ I've got too much energy! I need to move!
  - _cool:_ I want to train, but alone it's boring.
  - _teasing:_ I need a training partner. One who can keep up.

### Board / ProblemFirst / step RequestKind.Club
- **Her (by personality)**
  - _gentle:_ I'd like to do something with the club... if that's okay.
  - _energetic:_ Let's do club stuff! I'm bored of waiting!
  - _cool:_ I have an idea for the club. I need a hand.
  - _teasing:_ The club's been quiet. Let's change that.

### Board / ProblemFirst / step RequestKind.WalkHome
- **Her (by personality)**
  - _gentle:_ The way home gets dark early... I don't like walking it alone.
  - _energetic:_ Walking home alone is so boring!
  - _cool:_ I'd rather not walk home alone today.
  - _teasing:_ If someone walked me home, I wouldn't complain.

### Board / ProblemFirst / step RequestKind.Gift
- **Her (by personality)**
  - _gentle:_ It's been a hard day... I wish something nice would happen.
  - _energetic:_ Ugh, today needs a treat! Like, badly!
  - _cool:_ I could use something to lift my mood.
  - _teasing:_ A little present would fix my mood. Just saying.

### Board / ProblemFirst / step RequestKind.MeetAt
- **Her (by personality)**
  - _gentle:_ Would you meet me at the {Name} sometime today?
  - _energetic:_ Meet me at the {Name} later, okay?!
  - _cool:_ Come by the {Name} later.
  - _teasing:_ The {Name}. Later. Don't be late.

### Board / ProblemFirst / step RequestKind.Errand
- **Her (by personality)**
  - _gentle:_ Could I ask a small favor? {Text}
  - _energetic:_ Favor time! {Text}
  - _cool:_ I need a favor. {Text}
  - _teasing:_ You owe me, remember? {Text}

### Board / ProblemFirst / step RequestKind.Intimate
- **Her (by personality)**
  - _gentle:_ Could we talk again later today? There's something I want to tell you.
  - _energetic:_ Come find me later! I've got stuff to tell you!
  - _cool:_ Talk to me again later. It's important.
  - _teasing:_ Find me later. I have something to say.

### Board / PlayInTalk / step RequestKind.MeetAt
- **You:** {Pick}
- **Variants:** I got your note. What's up? | So you're the one who left the note.
- **You:** {Pick}
- **Variants:** Sure. Let's take a look. | Of course. Show me.
- **You:** {Pick}
- **Variants:** Hey. I'm here, like you asked. | Sorry, did I keep you waiting?

### Board / PlayInTalk / step RequestKind.Errand
- **You:** {Pick}
- **Variants:** Done. Took care of it for you. | All set. That favor you asked for.
- **You:** {IsNullOrEmpty}{", "I heard from "}{FirstName}{Pick}
- **Variants:** About what you told me earlier... | I came like I said I would.
- **Variants:** {FirstName} told me you needed a hand. | I heard from {FirstName} that you wanted to talk.
- **Variants:** Hey, you wanted to talk? | I saw your note on the club board.

## Short stories (multi-day arcs)
`Systems/ArcStories.cs`

### MakeupExamArc / Scene
- **You:** What's wrong? You look like you saw a ghost.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I... I failed the math test. If I fail the make-up exam too, they'll call my parents...
  - _energetic:_ I bombed the math test. Like, totally. There's a make-up exam and I'm DOOMED.
  - _cool:_ I failed a test. It happens. ...There's a make-up exam. I'd rather not fail twice.
  - _teasing:_ Don't laugh. I failed the math test. If you laugh, you're dead.
- **Choice:** I'll tutor you. | You'll be fine on your own.
- **Her:** {",
                    "Oh? A private tutor? I'll be a very attentive student~")}
- **Her (by personality)**
  - _gentle:_ Really? You'd do that for me? Thank you... I'll do my best!
  - _energetic:_ Seriously?! You're a lifesaver! Let's start today!
  - _cool:_ ...Fine. I accept. Don't waste my time.
  - _teasing:_ Oh? A private tutor? I'll be a very attentive student~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Oh... yeah. You're right. I'll try.
  - _energetic:_ Easy for you to say...
  - _cool:_ Hmph. Of course.
  - _teasing:_ Wow. Thanks for nothing.
- **You:** So? How did the make-up exam go?
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I passed! I really passed! It's all thanks to you... thank you so much!
  - _energetic:_ I PASSED! Eighty-two points! You're the best teacher ever!
  - _cool:_ I passed. With a decent score, even. ...Your explanations were good.
  - _teasing:_ Passed~ Guess you're good for something after all. I owe you one... a big one.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I barely passed... but I passed. Thank you for staying with me.
  - _energetic:_ Phew, just above the line! Close call, but we did it!
  - _cool:_ Barely passed. Still, better than failing.
  - _teasing:_ Scraped by. Next time, teach better~ Kidding. Thanks.

### TournamentArc / Scene / step 0
- **You:** I heard there's a tournament coming up.
- **Her:** {",
                        "Coming to watch me? I look pretty good in my uniform, you know~")}
- **Her (by personality)**
  - _gentle:_ Yes... I'm a little nervous. Everyone's counting on me.
  - _energetic:_ Yup! And this year we're gonna WIN! ...Probably! Maybe!
  - _cool:_ It's this weekend. I intend to win. Obviously.
  - _teasing:_ Coming to watch me? I look pretty good in my uniform, you know~
- **Choice:** Let me train with you. | Good luck out there.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You'd train with me? Then... I'll count on you!
  - _energetic:_ Yes! A training buddy! Don't you dare slack off!
  - _cool:_ Try to keep up.
  - _teasing:_ Hope you can handle my pace~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you. I'll do my best.
  - _energetic:_ Thanks! Come cheer for me, okay?
  - _cool:_ Luck has nothing to do with it.
  - _teasing:_ Luck? I'll need more than that from you~

### TournamentArc / Scene / step 2
- **Narration:** {Name} is drilling hard. Sweat runs down her face.
- **You:** Go, {Name}! You've got this!
- **Narration:** You jump in and run the drills with her. She has to push herself to keep up with you.
- **Her:** {",
                        "Watching me sweat, huh? Enjoying the view?")}
- **Her (by personality)**
  - _gentle:_ Hah... hah... thank you for coming. It really helps.
  - _energetic:_ Did you see that?! I'm on fire today!
  - _cool:_ ...Your cheering is loud. ...It's not bad.
  - _teasing:_ Watching me sweat, huh? Enjoying the view?

### TournamentArc / Scene / step 3
- **You:** So? How did the tournament go?
- **Her:** {",
                            "We won~ So, what's my reward from you?")}
- **Her (by personality)**
  - _gentle:_ We won! I scored the winning point... I kept thinking about how you trained with me.
  - _energetic:_ WE WON! Champions! I'm treating you to crepes, no arguing!
  - _cool:_ First place. As expected. ...Your training helped. A little. A lot.
  - _teasing:_ We won~ So, what's my reward from you?
- **Her:** { Comfort me properly, okay?")}
- **Her (by personality)**
  - _gentle:_ We lost in the semifinals... but I'm not sad. I did my best, and you were there.
  - _energetic:_ Lost by one point! ONE! ...Next year for sure!
  - _cool:_ We lost. I'll be stronger next time.
  - _teasing:_ We lost. Comfort me properly, okay?

### LostCharmArc / Scene
- **You:** Lose something?
- **Her:** { If you find it, I'll make it worth your while~")}
- **Her (by personality)**
  - _gentle:_ My charm... my grandma gave it to me. I had it this morning, and now it's gone...
  - _energetic:_ My lucky charm's gone! I can't lose it, it's my LUCKY charm!
  - _cool:_ A charm. It's not important. ...It's important.
  - _teasing:_ My charm. If you find it, I'll make it worth your while~
- **You:** I'll help you look for it.
- **You:** Is this the one you lost?
- **Narration:** You hold out a small, worn charm.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ That's it! You found it! Oh... thank you, thank you so much...!
  - _energetic:_ NO WAY! You found it! You're amazing!
  - _cool:_ ...That's it. You actually found it. I... thank you.
  - _teasing:_ Oh! My hero~ Seriously, thanks.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ I'll never forget this.
  - _energetic:_ I owe you big time!
  - _cool:_ I'll repay you. Someday.
  - _teasing:_ You're too nice. That's dangerous~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ W-what do you want...? M-maybe... a kiss on the cheek?
  - _energetic:_ Ha! Fine, a reward! Close your eyes... *smooch*
  - _cool:_ ...Close your eyes. Don't tell anyone.
  - _teasing:_ Hmm~ How about I tell you later... in private?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Eh? Um... a thank-you note?
  - _energetic:_ My eternal gratitude! That's a lot!
  - _cool:_ My thanks. Don't push it.
  - _teasing:_ Nice try~

### SecretHobbyArc / Scene
- **You:** So... I saw you earlier.
- **Her:** {",
                    "Ah! You saw?! Okay, okay, yes, I love "}{LikeLabel}{")}
- **Her (by personality)**
  - _gentle:_ Y-you saw that?! Please don't tell anyone... it's embarrassing...
  - _energetic:_ Ah! You saw?! Okay, okay, yes, I love {LikeLabel}. Happy?
  - _cool:_ ...You saw nothing.
  - _teasing:_ Spying on me? That's a bit creepy. ...Fine, you know my secret now.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you... You're really kind.
  - _energetic:_ Pinky promise? Okay! Thanks!
  - _cool:_ ...Good. I trust you, then.
  - _teasing:_ Good boy~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You think so? ...Nobody ever said that before.
  - _energetic:_ Right?! Finally someone gets it!
  - _cool:_ ...You're strange. But thank you.
  - _teasing:_ Heh. Flattery works on me, just so you know.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Eh?! Um... w-whatever you want...?
  - _energetic:_ Ooh, blackmail? Kinky. I'm in.
  - _cool:_ ...You're terrible. Fine. We'll talk later.
  - _teasing:_ Now you're speaking my language~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ That's mean...
  - _energetic:_ Not funny!
  - _cool:_ Tell anyone and you die.
  - _teasing:_ Ha. In your dreams.
- **Narration:** You spend a while with {Name}, and she shows you all about {LikeLabel}.
- **Her:** { Want to know more?")}
- **Her (by personality)**
  - _gentle:_ It's nice to share this with someone. With you.
  - _energetic:_ See?! Isn't it the best?! Let's do this again!
  - _cool:_ You didn't laugh. ...I'm glad it was you who found out.
  - _teasing:_ Not bad, huh? Now you know one of my secrets. Want to know more?

### RivalryArc / Scene / step 0
- **You:** Hey, what happened back there?
- **Her:** {T(t, "It's "}{OName}{",
                        "Ugh, "}{OName}{OName}{OName}{")}
- **Her (by personality)**
  - _gentle:_ It's {OName}... she said things about me behind my back. I thought we were okay...
  - _energetic:_ Ugh, {OName} is SO annoying! She took my spot and acted like it was nothing!
  - _cool:_ {OName} took credit for my work. I won't forgive that.
  - _teasing:_ {OName} has been flirting with you, hasn't she? Don't play dumb.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You believe me... thank you.
  - _energetic:_ See?! Even you think so!
  - _cool:_ ...At least you understand.
  - _teasing:_ Good answer~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You're taking her side...?
  - _energetic:_ Huh?! Whose friend are you?!
  - _cool:_ ...I see.
  - _teasing:_ Oh, so that's how it is.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You'd do that? ...Okay. I'll trust you.
  - _energetic:_ Ugh, fine. But she has to say sorry first!
  - _cool:_ ...Fine. Try it.
  - _teasing:_ Playing peacemaker? Cute. Go ahead.

### RivalryArc / Scene / step 1
- **You:** {Name} is pretty upset about earlier.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I know... I didn't mean it like that. I just didn't know how to say sorry.
  - _energetic:_ She's upset?! SHE started it!
  - _cool:_ Her reaction was excessive. ...Though I may have been careless.
  - _teasing:_ Oh, she sent you? How cute. Tell her to come herself.
- **Narration:** You calmly explain how {Name} sees it. Little by little, {OName}'s expression softens.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...You're right. I'll apologize to her. Thank you.
  - _energetic:_ ...Okay, okay. I'll talk to her. Happy?
  - _cool:_ Very well. I'll speak to her.
  - _teasing:_ Fine, fine. You win. I'll make up with her.
- **Narration:** You try to reason with her, but she isn't ready to listen.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ I'm sorry... I need more time.
  - _energetic:_ Nope! Not happening!
  - _cool:_ Drop it.
  - _teasing:_ Nice try, peacemaker.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You stood up for me? ...Thank you. That means a lot.
  - _energetic:_ Wait, you defended me? Huh. You're alright!
  - _cool:_ ...You defended me. I won't forget it.
  - _teasing:_ Taking my side? I like you more already~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ So you're on her side... I see.
  - _energetic:_ Whatever! Go hang out with HER then!
  - _cool:_ Then we have nothing to discuss.
  - _teasing:_ Wow. Okay. Noted.
- **You:** I talked to {OName}.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ She apologized to me... We're okay now. You really did it.
  - _energetic:_ She said sorry! We're friends again! You're a genius!
  - _cool:_ We talked. It's settled. ...Thank you.
  - _teasing:_ She apologized. Impressive. You're full of surprises.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...I guess I overreacted too. But you didn't have to take her side.
  - _energetic:_ Hmph. Fine. Whatever.
  - _cool:_ ...Noted.
  - _teasing:_ I'll remember this.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you for being on my side.
  - _energetic:_ Thanks for having my back!
  - _cool:_ You were on my side. Good.
  - _teasing:_ You chose me. Smart.

### FriendshipArc / Scene
- **You:** You look like you want to say something.
- **Her:** { you know "}{OName}{",
                    "Hey, you're friends with "}{OName}{", right? She seems super fun! Introduce me!",
                    "You know "}{OName}{OName}{ Be a dear and introduce us?")}
- **Her (by personality)**
  - _gentle:_ Um... you know {OName}, right? I'd like to be friends with her, but I never know what to say...
  - _energetic:_ Hey, you're friends with {OName}, right? She seems super fun! Introduce me!
  - _cool:_ You know {OName}. ...I wouldn't mind getting to know her. That's all.
  - _teasing:_ {OName} seems interesting. Be a dear and introduce us?
- **You:** Leave it to me.
- **You:** Hey, do you know {Name}? She'd like to have lunch with us sometime.
- **Her:** {OTone, "Oh, "}{Name}{")}
- **Her (by personality)**
  - _gentle:_ Oh, {Name}? I've always wanted to talk to her! Of course!
  - _energetic:_ Sure! The more the merrier!
  - _cool:_ ...If you're there, I suppose it's fine.
  - _teasing:_ Ooh, matchmaking? Fine, I'm curious.

### ConfessionPracticeArc / Scene
- **Her:** { You're the dummy~")}
- **Her (by personality)**
  - _gentle:_ My friend wants to confess to someone, and... she asked me to practice with her. But I need someone to practice on... would you help?
  - _energetic:_ So! A friend of mine wants to confess and I'm helping her practice! Be my target, okay?!
  - _cool:_ A friend needs help with a confession. I need a practice partner. You'll do.
  - _teasing:_ I need to practice a confession. For a friend, obviously. You're the dummy~
- **You:** Uh... sure. Where?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ The rooftop... after school. Please don't be late.
  - _energetic:_ Rooftop, after school! It's the classic spot!
  - _cool:_ Rooftop. After school.
  - _teasing:_ The rooftop after school. Very cliché. Perfect.
- **Narration:** The wind is gentle on the rooftop. {Name} takes a deep breath.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I... I've liked you for a long time. Every day, I look for you in the hallway... W-will you go out with me?
  - _energetic:_ I like you! A lot! Like, really a lot! Go out with me!
  - _cool:_ I like you. I've thought about it carefully, and it's the only answer. Date me.
  - _teasing:_ I like you. Not as a joke. Not as practice... Wait. Pretend I didn't say that last part.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ R-really? Ahaha... yeah. Practice. Of course.
  - _energetic:_ Haha... yeah! Totally! Practice!
  - _cool:_ ...Of course. Practice.
  - _teasing:_ ...Idiot.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Maybe not. Don't make me say it twice.
  - _energetic:_ D-don't ask that! ...Maybe it wasn't!
  - _cool:_ ...You figure it out.
  - _teasing:_ What do you think~?
- **Her:** { Now do it right, with a real confession, okay?")}
- **Her (by personality)**
  - _gentle:_ Huh...? You... you mean it? I'm so happy... Then next time, please say it properly. For real.
  - _energetic:_ WAIT, REALLY?! Okay, okay, I'm not crying! Say it again for real sometime!
  - _cool:_ ...I see. Then I expect you to confess properly. Soon.
  - _teasing:_ ...Took you long enough. Now do it right, with a real confession, okay?

### LateStudyArc / Scene
- **Her:** { and other things~")}
- **Her (by personality)**
  - _gentle:_ Exams are coming... so I thought, maybe we could study together? Just the two of us.
  - _energetic:_ Study date! Snacks, notes, and you! Best plan ever!
  - _cool:_ We study together this week. That's decided.
  - _teasing:_ Let's study together. I'll even let you look at my notes... and other things~
- **You:** It's a date.

### BadDayArc / Scene
- **You:** Hey... are you okay?
- **Her:** {",
                "Do I LOOK okay?")}
- **Her (by personality)**
  - _gentle:_ N-no... everything went wrong today. I forgot my homework, I tripped in the hallway, and...
  - _energetic:_ NO! Today is the WORST! Everything is going wrong!
  - _cool:_ ...Not now.
  - _teasing:_ Do I LOOK okay?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Thank you for listening. I feel a bit lighter.
  - _energetic:_ ...Yeah. Thanks. Venting helps.
  - _cool:_ ...You're a decent listener.
  - _teasing:_ ...Fine. Thanks for not running away.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Pfft. That was so bad. ...Thanks.
  - _energetic:_ HAHA! That's so dumb! ...I needed that.
  - _cool:_ ...That's not funny. ...Okay, it's a little funny.
  - _teasing:_ Seriously? ...Ugh, I laughed. You win.
- **Narration:** You sit next to her in silence. After a while, she leans on your shoulder.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...You always know what I need.
  - _energetic:_ ...This is nice. Thanks.
  - _cool:_ ...Stay a bit longer.
  - _teasing:_ ...Don't tell anyone I did this.

## Everyday happenings
`Systems/Happenings.cs`

### Happenings / Play / step "coin"
- **Narration:** Something glints on the hallway floor... a 500 yen coin.
- **Choice:** Pocket it. | Turn it in at the staff room.
- **Narration:** Finders keepers.
- **Narration:** The teacher thanks you for your honesty.

### Happenings / Play / step "pop_quiz"
- **Narration:** A teacher stops you in the hallway: "You! What's the capital of Australia? Quick!"
- **Narration:** {"}

### Happenings / Play / step "carry_boxes"
- **Narration:** A teacher is struggling with a tower of boxes full of handouts.
- **Narration:** You take half the boxes. The teacher thanks you.
- **Narration:** You suddenly find the ceiling very interesting.
- **Narration:** You lift the whole tower in one go. A few students clap.

### Happenings / Play / step "love_letter"
- **Narration:** There's a pink envelope in your shoe locker. Inside: "Please come to the rooftop after school. I have something to tell you."
- **Narration:** It's signed with a little heart. No name.

### Happenings / Play / step "locker_note"
- **Narration:** There's a folded note in your shoe locker: "Could you come to the rooftop after school? I need help with something and I'd rather not ask in front of everyone."
- **Narration:** It's signed with a single letter: {initial}.

### Happenings / Play / step "dropped_notebooks"
- **Narration:** Someone bumps into {name} and her notebooks scatter across the floor.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah... my notes...
  - _energetic:_ Nooo! My stuff!
  - _cool:_ ...Great.
  - _teasing:_ Ugh. Perfect timing.
- **Choice:** Help her pick them up. | Keep walking.
- **Narration:** You gather everything and hand it back.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you so much!
  - _energetic:_ You're a lifesaver!
  - _cool:_ ...Thanks.
  - _teasing:_ Oh? A gentleman. Thanks~

### Happenings / Play / step "gossip"
- **Narration:** You overhear a group of students gossiping by the window...
- **Narration:** {\""}

### Happenings / Play / step "vending"
- **Narration:** You buy a drink from the vending machine. The little lottery display starts spinning... 7... 7... {value}
- **Narration:** {"}

### Happenings / Play / step "wind"
- **Narration:** A sudden gust of wind sweeps the hallway — and {name}'s skirt flips up!
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Th-thank you for looking away...
  - _energetic:_ Phew! You didn't see anything, right?! RIGHT?!
  - _cool:_ ...You looked away. Good.
  - _teasing:_ Aww, you didn't even peek? How boring~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you saw, didn't you...?
  - _energetic:_ You totally saw!
  - _cool:_ ...Forget it. Now.
  - _teasing:_ Enjoy the show?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Eh?! ...Y-you think so...?
  - _energetic:_ Hehe, really? I picked them this morning!
  - _cool:_ ...Idiot. ...Thanks.
  - _teasing:_ Only cute? I'll show you something better later~
- **Her (by personality)**
  - _gentle:_ P-pervert!!
  - _energetic:_ WHAT?! Pervert!
  - _cool:_ Die.
  - _teasing:_ Wow. Gross.

### Happenings / Play / step "rumor"
- **Narration:** {\""}

### Happenings / Play / step "inspection"
- **Narration:** Student council inspection! They're checking everyone's bags at the gate.
- **Narration:** Your bag is spotless. The council president nods approvingly.

### Happenings / Play
- **Narration:** The council member pulls out your... items. Everyone stares.
- **Narration:** You explain with a perfectly straight face...
- **Narration:** You sprint past the gate before anyone can stop you!

### Happenings / Play / step "stray_cat"
- **Narration:** A small stray cat follows you, meowing hopefully.
- **Narration:** It purrs and rubs against your leg, then trots off.
- **Narration:** It eats happily. A few girls watching from the window go "awww".

### Happenings / Play / step "rain"
- **Narration:** It starts pouring right as school ends. {name} is stuck at the entrance without an umbrella.
- **Choice:** Share your umbrella with her. | Lend it to her and run home in the rain. | Wait for the rain to stop.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Together...? O-okay. Your shoulder is getting wet...
  - _energetic:_ Umbrella buddies! Squeeze in!
  - _cool:_ ...Fine. Don't get any closer than necessary.
  - _teasing:_ Sharing an umbrella? How romantic~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ But you'll get soaked...! Thank you...
  - _energetic:_ Whoa, really?! You're the best!
  - _cool:_ ...You're an idiot. Thank you.
  - _teasing:_ My knight in soaked armor~

## School events (long scenes over painted backgrounds: exams, festivals, trip, seasonal days)
`Systems/CalendarEvents.cs`

### Choco
- **Narration:** February 14th. Chocolate Day.
- **Narration:** The whole school smells of cocoa. Every boy is pretending not to care, and every boy is failing.
- **Narration:** Your shoe locker is right in front of you. It might be empty. It might not be.
- **Choice:** Open it calmly | Open it with your eyes closed | Walk past it like a cool guy
- **Narration:** {"}
- **Narration:** You open it slowly, whispering a prayer to every god of romance you know...
- **Narration:** {"}
- **Narration:** You walk past without looking. Then you walk back. Then you look.
- **Narration:** {"}
- **Narration:** Classes drag on forever. The room is full of whispered plans and rustling paper bags.
- **Narration:** Lunch comes and goes. Nobody walks up to your desk. You eat your bread very slowly, just in case.
- **Narration:** Right before the bell, {N} stops next to you.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Um... I made too many for my club. You can have the extra one... if you want.
  - _energetic:_ Leftover chocolate! Catch! ...Nice catch!
  - _cool:_ I had one left over. Don't make it weird.
  - _teasing:_ Pity chocolate~ Don't cry, it's still chocolate.
- **You:** Thanks. Really.
- **Narration:** Next year, for sure.
- **Narration:** At lunch, it starts.
- **Narration:** {N} walks up to your desk, hands hidden behind her back.
- **Girl:** {")
                        : T}
- **Her (by personality)**
  - _gentle:_ I made these myself... for you. Only for you.
  - _energetic:_ Homemade! Eat them all, okay?!
  - _cool:_ Handmade. Don't make a fuss.
  - _teasing:_ Handmade, with love~ And maybe a little something extra.
- **Her (by personality)**
  - _gentle:_ Um, these are... thank-you chocolates! Just thanks!
  - _energetic:_ Here, friendship chocolate! Don't get the wrong idea!
  - _cool:_ Obligation chocolate. Obviously.
  - _teasing:_ Don't get your hopes up. ...Unless you want to.
- **Narration:** And {value} more boxes appear on your desk while you're not looking. The boys at the back are glaring.
- **Narration:** {N} is still standing there, watching you hold her box.
- **Choice:** Eat one right now | Ask if it's homemade | Save it for later
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ R-right now? ...How is it? Is it okay?
  - _energetic:_ Well?! Well?! Is it good?!
  - _cool:_ ...You have chocolate on your lip. Idiot.
  - _teasing:_ Mmh, you eat it so happily~ I should feed you more often.
- **Narration:** It's sweet, a little bitter at the end. Perfect.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-yes... I stayed up late. Don't tell anyone.
  - _energetic:_ Totally homemade! The kitchen is a disaster now!
  - _cool:_ Store bought. ...Fine. Homemade. Happy?
  - _teasing:_ Homemade, with a secret ingredient. Guess what it is~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Oh... okay. Eat it when you're alone, then.
  - _energetic:_ Saving it? Don't let it melt!
  - _cool:_ Hmph. Suit yourself.
  - _teasing:_ Saving the best for last? Smart boy~
- **Narration:** After school. Snow falls softly on the way home, and the streetlights come on one by one.
- **Narration:** Footsteps run up behind you. {N}, out of breath, cheeks red from the cold.
- **Girl:** { Got it?")}
- **Her (by personality)**
  - _gentle:_ Wait! The one this morning was... not the real one. This is the real one.
  - _energetic:_ Hah... hah... I forgot to give you the SPECIAL one!
  - _cool:_ ...That one at school was for show. This one is honmei. Don't make me say it twice.
  - _teasing:_ The one at school was obligation. This one's... not. Got it?
- **Choice:** Promise to return it on White Day | Take her cold hand | Just say thank you
- **You:** I'll give you something back on White Day. Something good.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...I'll be waiting. Every day until then.
  - _energetic:_ Three times as good, that's the rule! I'm counting!
  - _cool:_ I'll hold you to that.
  - _teasing:_ Something good? Then I'll expect something very good~
- **Narration:** Her fingers are freezing. She doesn't pull away. For a while, neither of you says anything.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Your hand is warm.
  - _energetic:_ W-what's this all of a sudden?! ...Don't let go.
  - _cool:_ ...Only because it's cold.
  - _teasing:_ Took you long enough~
- **You:** Thank you. I mean it.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Mm. That's all I wanted to hear.
  - _energetic:_ Hehe! You're welcome!
  - _cool:_ ...You're welcome.
  - _teasing:_ Just 'thank you'? Well, it's a start~
- **Narration:** You walk home alone, counting the boxes in your bag. Not a bad day at all.
- **Narration:** Chocolate Day is over. Your bag is heavier, and somehow so is your heart.

### Sports
- **Narration:** The Sports Festival! Flags snap in the wind and the whole school is out on the field.
- **Narration:** Tents, tables, a mountain of traffic cones and a teacher with a megaphone who is already hoarse.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Here's your class headband... Tie it tight, okay? Good luck today.
  - _energetic:_ HEADBANDS! Everybody gets one! Our class is winning this!
  - _cool:_ Headband. Put it on properly. We're not losing to the other classes.
  - _teasing:_ Headband~ Let me tie it for you. ...There. Now you look like you mean it.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Which event are you entering? I'll cheer for you from the tent!
  - _energetic:_ Let's WIN this! Which event are you in?
  - _cool:_ Try not to embarrass our class.
  - _teasing:_ I'll be watching you~ Don't trip.
- **Narration:** The sign-up sheet is in front of you. Pick your event.
- **Narration:** Anchor leg. The baton is coming toward you, and your class is in third place.
- **Narration:** You grab it and run like your life depends on it. The crowd becomes one long roar.
- **Narration:** {"}
- **Narration:** Thirty students on one rope. The referee raises the flag.
- **Narration:** PULL! Your feet slide in the dirt. Somebody behind you is screaming a war cry.
- **Narration:** {"}
- **Narration:** You grab the megaphone and lead the cheering section with all your heart.
- **Narration:** {"}
- **Narration:** Your ankle is tied to hers. You'll have to move together.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ W-we're really close like this... Left foot first, okay?
  - _energetic:_ Okay! One-two, one-two! Don't let go of me!
  - _cool:_ Keep up. And don't hold me that tight.
  - _teasing:_ Arm around my waist~ For balance, of course.
- **Narration:** The starting pistol fires. Her shoulder bumps yours with every step.
- **Narration:** {"}
- **Narration:** Afternoon. The girls' 200 meters is next.
- **Narration:** {N} is in lane four, bouncing on her toes, glancing toward your class's tent.
- **Choice:** Shout her name as loud as you can | Wait at the finish line with water | Film it for her
- **Narration:** {"}
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I heard you... It was so embarrassing. ...Thank you.
  - _energetic:_ THAT WAS YOU?! I ran faster because of that!
  - _cool:_ You're loud. ...I didn't hate it.
  - _teasing:_ Shouting my name in front of everyone? Bold~
- **Narration:** She crosses the line and nearly collapses. You're right there with a cold bottle.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You waited for me...? Thank you, I'm so thirsty.
  - _energetic:_ Water! You're a lifesaver!
  - _cool:_ ...Thoughtful. Thanks.
  - _teasing:_ Waiting at the finish line for me? How devoted~
- **Narration:** You record the whole race on your phone. Later she watches it three times, pretending she isn't.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't show anyone how my face looks when I run!
  - _energetic:_ Again! Play it again! Look how fast I was!
  - _cool:_ Delete the part where I almost tripped.
  - _teasing:_ Keeping a video of me? What else do you keep~?
- **Narration:** You cheer from the tent until your throat hurts.
- **Narration:** Closing ceremony in the gym. The scoreboard is covered with a cloth while the principal talks... and talks.
- **Narration:** {"}
- **Girl:** {", "Winners deserve a reward, don't they~?")
                                       : T}
- **Her (by personality)**
  - _gentle:_ We won... we really won! I'm so glad I was on your team.
  - _energetic:_ WE WON WE WON WE WON!
  - _cool:_ Of course we won. ...Okay, I'm happy. A little.
  - _teasing:_ Winners deserve a reward, don't they~?
- **Her (by personality)**
  - _gentle:_ We did our best. That's what matters.
  - _energetic:_ Argh! Next year we crush them!
  - _cool:_ Second place. Acceptable. Barely.
  - _teasing:_ Losing is fine. You looked cool anyway~
- **Choice:** Walk her home | Stay and help clean up
- **Narration:** The two of you walk home in the orange evening, sore and sunburned and very happy.
- **Narration:** You fold tents and carry cones until dark. The teachers notice. So does she: she stays to help.
- **Narration:** The Sports Festival is over. Tomorrow, everything will hurt.

### Exam
- **Narration:** Exam day. Desks pulled apart, the schedule written on the board, and a silence you could cut with a ruler.
- **Narration:** Ten minutes before the start. Everyone is flipping through notes as if the pages might change.
- **Narration:** {N} leans over from the next desk, notebook clutched to her chest.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Um... could you quiz me? Just the formulas... I'm so nervous.
  - _energetic:_ Help! I forgot EVERYTHING! Quick, ask me something!
  - _cool:_ Ask me three questions. I want to be sure.
  - _teasing:_ Quiz me~ If I get them all right, you owe me something.
- **Choice:** Quiz her on the formulas [INT] | Calm her down | Review on your own
- **Narration:** {"}
- **You:** You studied. You know this. Breathe.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Okay. Breathe. Thank you. I feel better.
  - _energetic:_ Breathe! Right! Hooo... okay! I'm ready!
  - _cool:_ ...Fine. I'm calm. I was always calm.
  - _teasing:_ You're cute when you try to comfort me~
- **Narration:** You bury your face in your notes. One last look at everything.
- **Narration:** The teacher hands out the papers face down. "Begin."
- **Narration:** Deep breath. Question one. You know this. Question two. You know this too.
- **Narration:** You scribble every formula you crammed into the margin before they leak out of your head.
- **Narration:** The teacher steps out to answer the phone. The answer key is right there, on the desk...
- **Narration:** {"}
- **Narration:** Question seven. Nothing you studied looks anything like question seven.
- **Choice:** Skip it and come back later | Fight it to the end [INT]
- **Narration:** You leave it and finish the rest. At the end you come back with five minutes left and write something reasonable.
- **Narration:** {"}
- **Narration:** "Pencils down." The room exhales all at once.
- **Girl:** {", "You looked so serious~ Was it hard, or were you thinking about me?")}
- **Her (by personality)**
  - _gentle:_ How did it go? I... think I did okay. Thanks to you, maybe.
  - _energetic:_ DONE! Wanna compare answers? No? Okay, food then!
  - _cool:_ Don't ask me about question seven.
  - _teasing:_ You looked so serious~ Was it hard, or were you thinking about me?
- **Narration:** The sun sets over the empty desks. Results tomorrow.

### Culture
- **Narration:** Two weeks before the Culture Festival. The class meeting has been going in circles for an hour.
- **Narration:** Three ideas are left on the board. The class rep looks at you: you get the deciding vote.
- **Choice:** Maid cafe [SEN] | Haunted house [STR] | Science exhibit [INT]
- **Narration:** {"}
- **Narration:** You stay late to work on it with {N}.
- **Girl:** {", "I'll play the ghost who drags boys into the dark~")
                        : T}
- **Her (by personality)**
  - _gentle:_ The aprons are done... Do you think this one suits me?
  - _energetic:_ Look, look! I'm practicing my 'Welcome home, master'!
  - _cool:_ I'm in charge of the menu. Nobody touches the menu.
  - _teasing:_ Want a private preview of the uniform~?
- **Her (by personality)**
  - _gentle:_ I'm painting the blood... it's just red paint, right? Right?
  - _energetic:_ BOO! Hahaha, your face!
  - _cool:_ The lighting is wrong. Move that lamp. No, the other one.
  - _teasing:_ I'll play the ghost who drags boys into the dark~
- **Her (by personality)**
  - _gentle:_ The volcano works! I'm so happy I could cry.
  - _energetic:_ Can we make it BIGGER? Like, way bigger?
  - _cool:_ The numbers check out. It'll work.
  - _teasing:_ Explaining chemistry to strangers? I'll do it in a lab coat~
- **Narration:** Festival day! Lanterns over every gate, music from the gym, and the smell of takoyaki everywhere.
- **Narration:** Visitors pour in. Your class's booth opens its doors, and all morning you work without a break.
- **Narration:** {"}
- **Narration:** {"}
- **Narration:** {"}
- **Narration:** In the afternoon you finally get some free time.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You want to walk with me? ...I'd love to.
  - _energetic:_ Yes! Let's hit every food stand!
  - _cool:_ ...Fine. You can escort me.
  - _teasing:_ A festival date? Bold~ I accept.
- **Narration:** Where to first?
- **Choice:** Share some takoyaki | Brave the haunted house together | Visit the fortune teller
- **Narration:** The takoyaki stand is run by the cooking club. The octopus pieces are suspiciously big.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Hot, hot... Ah, you have sauce right here. Hold still.
  - _energetic:_ Mmh! Okay, the next stand is on me!
  - _cool:_ ...It's good. Don't watch me eat.
  - _teasing:_ Say 'ah'~ Come on, nobody's looking.
- **You:** This is the best takoyaki I've ever had.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Maybe because you're eating it with me...?
  - _energetic:_ Right?! Let's get another box!
  - _cool:_ It's average. ...The company isn't bad.
  - _teasing:_ Of course. I made it taste better just by being here~
- **Narration:** The third-years built the haunted house. Paper lanterns, cobwebs, and a corridor that goes on far too long.
- **Narration:** It's pitch dark. Something wet touches your neck. A scream. She grabs your arm and doesn't let go.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I-I wasn't scared! ...Don't let go yet, okay?
  - _energetic:_ AAAH! Ha... haha! Again! Let's go again!
  - _cool:_ I'm not holding on to you. You're holding on to me.
  - _teasing:_ You screamed louder than me~ Cute.
- **Narration:** You come out the other side still arm in arm. Neither of you mentions it.
- **Narration:** A tent of purple curtains, a crystal ball, and a third-year in a veil who takes this very seriously.
- **Narration:** She looks at the two of you, then at your joined shadows, and smiles.
- **Narration:** {\""}
- **Girl:** {What did she say about us, exactly?", "Well? Going to argue with fate?")}
- **Her (by personality)**
  - _gentle:_ She said... we're compatible. W-what do you think?
  - _energetic:_ Did you hear that?! Ha, she says that to everyone. ...Right?
  - _cool:_ Fortune telling is nonsense. ...What did she say about us, exactly?
  - _teasing:_ Well? Going to argue with fate?
- **Narration:** Evening. The stalls glow red and orange. In the schoolyard, the bonfire for the closing dance is lit.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ The folk dance is starting... um...
  - _energetic:_ Bonfire! Dance! Come on, come on!
  - _cool:_ Are you going to dance? ...Just asking.
  - _teasing:_ Everyone's pairing up. Don't keep a girl waiting~
- **Choice:** Ask her to dance | Watch the fire together
- **Narration:** You hold out your hand. She takes it. You step on her foot twice. She doesn't care.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I'll remember this festival forever.
  - _energetic:_ Again! One more song!
  - _cool:_ ...You're a terrible dancer. Don't stop.
  - _teasing:_ Your hand's sweaty~ Nervous?
- **Narration:** You sit on the steps and watch the sparks rise into the dark. Her shoulder leans against yours.
- **Narration:** You spend the afternoon backstage: fixing lights, carrying boxes, saving a play from a broken curtain.
- **Narration:** By evening, the teachers know your name. In a good way, for once.
- **Narration:** The last song ends. The Culture Festival is over, and nobody wants to go home.

### Tanabata
- **Narration:** July 7th. Tanabata.
- **Narration:** Once a year, the weaver princess Orihime and the cowherd Hikoboshi cross the river of stars to meet.
- **Narration:** On earth, people write their wishes on strips of paper and tie them to bamboo, hoping the stars will read them.
- **Narration:** The old street near school is full of bamboo branches heavy with colored paper. Lanterns sway overhead.
- **Narration:** Someone tugs your sleeve. {N}, in a summer yukata.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah... you came too. Does it... look okay on me?
  - _energetic:_ Ta-da! Yukata! Say it's cute!
  - _cool:_ My mother made me wear it. Don't stare.
  - _teasing:_ Like what you see~? It took an hour to tie.
- **Narration:** She has a paper strip in her hand and hides it the moment you look.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah! Don't look at mine... It's embarrassing.
  - _energetic:_ I wished for something HUGE. Not telling!
  - _cool:_ Mine is private. Turn around.
  - _teasing:_ Want to know what I wished for? It involves you~
- **Narration:** She ties it up high, out of your reach.
- **Narration:** Your turn. You hold the pen over the paper for a long moment...
- **Choice:** 'I want to get smarter.' | 'I want to get stronger.' | 'I want to be more charming.' | 'I want to be loved.'
- **Narration:** You hang your wish on the highest branch you can reach. The paper turns slowly in the warm wind.
- **Narration:** Night falls. The stalls along the shrine road light up one after another.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Will you walk with me a little...?
  - _energetic:_ Stalls! Let's try everything!
  - _cool:_ I'm hungry. You're paying.
  - _teasing:_ A summer night, a girl in a yukata... You're lucky, you know~
- **Choice:** Goldfish scooping | Shaved ice | Walk under the lanterns
- **Narration:** Your paper net tears on the first try. Hers tears on the second. The old man gives you one goldfish out of pity.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ He's so small... Can I keep him? I'll name him after you.
  - _energetic:_ WE CAUGHT ONE! Kind of! He counts!
  - _cool:_ ...I'll take care of him. Since you can't.
  - _teasing:_ Our first pet together~
- **Narration:** Strawberry for her, melon for you. Brain freeze for both.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Want to try mine...? It's strawberry.
  - _energetic:_ Ow ow ow, my head! Worth it!
  - _cool:_ Your tongue is green. Very dignified.
  - _teasing:_ Indirect kiss~ Just kidding. Unless...?
- **Narration:** You walk slowly under the paper lanterns. The crowd pushes you closer together. Neither of you steps away.
- **Narration:** On the way back you pass her bamboo branch. Her paper strip has come loose and flutters down at your feet.
- **Narration:** You can't help reading it.
- **Narration:** {T}
- **Her (by personality)**
  - _gentle:_ "I hope he notices how I feel."
  - _energetic:_ "Let me spend every summer with him!"
  - _cool:_ "I want to be honest with him. Just once."
  - _teasing:_ "Make him fall for me. Hard."
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you read it...! ...Well? Is it going to come true?
  - _energetic:_ AAAH! Forget you saw that! ...Or don't. Whatever!
  - _cool:_ ...Now you know. Don't make it weird.
  - _teasing:_ Oops~ Well, now you know. What are you going to do about it?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't read it! Please!
  - _energetic:_ Mine! Give it! No peeking!
  - _cool:_ Hand it over. Now.
  - _teasing:_ Curious? Earn it first~
- **Narration:** She ties it back up, higher this time.
- **Narration:** Above the street, the Milky Way is clear tonight. Somewhere up there, two lovers are meeting.

### Halloween
- **Narration:** Halloween. The classroom is covered in paper bats, jack-o'-lanterns and orange streamers.
- **Narration:** There's a costume contest after school, and the class rep says nobody leaves without a costume.
- **Choice:** Vampire, cape and all [SEN] | A bedsheet ghost | No costume. You're too cool.
- **Narration:** Fake fangs, a red-lined cape, hair slicked back. In the mirror, you look... surprisingly good?
- **Narration:** {"}
- **Narration:** A bedsheet with two holes. You bump into three desks on the way to your seat.
- **Narration:** At the contest the whole class laughs when your ghost walks into the door. Special prize: most dedicated ghost.
- **Narration:** The class rep sighs and sticks cat ears on your head. They stay there all day.
- **Narration:** At the contest you're not even entered. The cat ears get three votes anyway.
- **Narration:** At lunch, the trick-or-treaters arrive.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ T-trick or treat...?
  - _energetic:_ TRICK OR TREAT!
  - _cool:_ Trick or treat. Choose wisely.
  - _teasing:_ Trick or treat~ I'm hoping for the trick.
- **Narration:** You hand out sweets from your bag. Everyone's happy.
- **Narration:** You don't have enough sweets... Time for some tricks.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Then... I'll draw a cat face on your cheek. Hold still...
  - _energetic:_ No candy? TICKLE ATTACK!
  - _cool:_ No sweets. Then you carry my bag all day.
  - _teasing:_ No treats? Then close your eyes~ This trick is a secret.
- **Narration:** After dark: the test of courage. Pairs walk the old hallway with one flashlight and no adults.
- **Narration:** You draw lots. Your partner is {N}.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I-it's really dark... You'll stay close, right?
  - _energetic:_ This is gonna be AWESOME! Let's go!
  - _cool:_ It's a hallway. Hallways aren't scary.
  - _teasing:_ Alone in the dark with you~ Should I be scared of the ghosts, or of you?
- **Narration:** Halfway down, the flashlight flickers. Somewhere behind you, a locker door slowly creaks open.
- **Choice:** Take her hand | Jump out and scare her | Walk ahead bravely
- **Narration:** You find her hand in the dark. She squeezes it so hard it hurts. You don't let go until the end.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Thank you. I'm okay now. Just... don't let go.
  - _energetic:_ Ha! I wasn't scared! ...Keep holding it though.
  - _cool:_ ...This is for your sake. You looked scared.
  - _teasing:_ Bold~ Should we walk slower?
- **Narration:** "BOO!" She screams, then punches your arm, then laughs so hard she has to sit down.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ That was so mean! ...My heart's still racing.
  - _energetic:_ AHAHA! Okay, okay, you got me! Revenge later!
  - _cool:_ ...I will remember this.
  - _teasing:_ Scaring a girl in the dark? You're worse than the ghosts~
- **Narration:** You walk in front, flashlight high. Behind you, she holds the hem of your jacket the whole way.
- **Narration:** Back in the classroom, the jack-o'-lanterns are lit for the party.
- **Narration:** Pumpkin cake, orange juice and the bad horror movie somebody brought. Happy Halloween.

### Trip
- **Narration:** Field trip day! The whole class boards the bullet train to {value}.
- **Narration:** Rice fields fly past the window. Somebody already opened the snacks. The teacher has given up.
- **Narration:** There's a free seat next to you.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You saved me a seat...? Thank you.
  - _energetic:_ Window seat neighbor! Want some chips?
  - _cool:_ Fine. Don't hog the armrest.
  - _teasing:_ Two hours sitting next to me. Lucky you~
- **Narration:** You play cards for an hour. She cheats. You let her.
- **Narration:** Kyoto. Temples, old streets and far too many souvenir shops. The class splits up for free time.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Just the two of us? I was hoping you'd ask...
  - _energetic:_ Yay! Let's go everywhere!
  - _cool:_ I suppose you can come along.
  - _teasing:_ Sneaking away with me? I like it~
- **Choice:** Rent kimonos and walk the old streets | Write a wish on an ema together
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ How do I look...? You're staring.
  - _energetic:_ Look, look! I'm a real princess now!
  - _cool:_ Stop taking pictures. ...Fine, one more.
  - _teasing:_ Help me with the obi? It's so hard to tie alone~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't read mine! ...It's about you. A little.
  - _energetic:_ I wished for ten more trips like this one!
  - _cool:_ Wishes are childish. ...I wrote one anyway.
  - _teasing:_ Mine says 'that he notices me'. Oops, you read it~
- **Narration:** The inn is old and beautiful: tatami rooms, sliding doors, a garden with a stone lantern. Steam rises behind the bamboo fence.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so pretty here... I'm glad I came with you.
  - _energetic:_ Hot springs! Ping-pong! Milk in glass bottles! Let's go!
  - _cool:_ Quiet. Good. I like this place.
  - _teasing:_ Mixed bathing is around the back, I heard~ Just saying.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah, my yukata is coming loose... D-don't look!
  - _energetic:_ SMASH! Ha! Another one, loser buys milk!
  - _cool:_ You're going down. ...Stop looking at my collar.
  - _teasing:_ If I win, you do whatever I say tonight~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so quiet... I can hear the water. I like being here with you.
  - _energetic:_ Look! A frog! A HUGE frog!
  - _cool:_ ...This is nice. Don't ruin it by talking.
  - _teasing:_ A garden, a quiet afternoon, just us~ What are you thinking about?
- **Narration:** While the others nap, you climb the rocks behind the bamboo fence, holding your breath...
- **Narration:** Night at the inn. Lights out at ten, the teacher said. It's eleven-thirty and nobody is asleep.
- **Narration:** A soft knock on the sliding door. It's {N}, in her yukata, whispering.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I couldn't sleep... Can I stay for a bit?
  - _energetic:_ Psst! The girls are having a pillow fight, I escaped! Hide me!
  - _cool:_ My roommates snore. I'm staying here for ten minutes.
  - _teasing:_ Sneaking into the boys' room~ If we get caught, it's your fault.
- **Narration:** Footsteps in the corridor. The teacher's flashlight sweeps under the door.
- **Choice:** Hide her in the closet | Talk quietly until it passes | Send her back before you're caught
- **Narration:** You both squeeze into the futon closet. It's very dark and very small. You can hear her heartbeat. Or yours.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Your heart is so loud.
  - _energetic:_ Mmmph! Don't make me laugh!
  - _cool:_ ...Don't move. Don't breathe. Don't think.
  - _teasing:_ Cozy~ We should get caught more often.
- **Narration:** The footsteps pass. You talk in whispers until two in the morning: families, dreams, the stupid things you did as kids.
- **Narration:** You slide the door open just in time. She tiptoes back. The teacher only sees you, waving awkwardly.
- **Narration:** You watch the mountains go by and listen to music. Peaceful.
- **Narration:** {"}
- **Narration:** The train home. Everyone is exhausted. The car is quiet except for snoring.
- **Narration:** Something warm settles on your shoulder. Somebody fell asleep on you. You don't move for the rest of the trip.
- **Narration:** A trip to remember.

### Xmas
- **Narration:** Christmas Eve. Snow on the rooftops, lights in every window, couples on every corner.
- **Narration:** For once, you're one of them. {N} is waiting under the streetlight, her breath white in the cold.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ You came... Merry Christmas. I've been looking forward to this all month.
  - _energetic:_ MERRY CHRISTMAS! Look, it's snowing! It's actually snowing!
  - _cool:_ You're late. ...By two minutes. I'll allow it. Merry Christmas.
  - _teasing:_ Merry Christmas~ Did you dress up for me? You did, didn't you.
- **Narration:** You walk through the quiet streets. She slips her hand into your coat pocket, next to yours.
- **Narration:** The park is empty. Fresh snow on the bench, one lamp glowing gold. It feels like the whole world is holding its breath.
- **Narration:** You take out the present you've been hiding all evening.
- **Choice:** A warm scarf | A small necklace | A handmade card
- **Narration:** You wrap it around her neck yourself. It covers her up to the nose.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so warm... It smells like you.
  - _energetic:_ SO FLUFFY! I'm never taking it off!
  - _cool:_ ...Practical. I like practical. ...I like it a lot.
  - _teasing:_ Wrapping me up like a present? Who's unwrapping me later~?
- **Narration:** Her eyes go wide when she opens the little box. Her hands are shaking as you fasten it.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's beautiful... I'll treasure it forever.
  - _energetic:_ Shiny! It's so pretty! Was it expensive?! Don't tell me!
  - _cool:_ ...You didn't have to. ...I'm wearing it every day.
  - _teasing:_ Jewelry? Someone's serious about me~
- **Narration:** A card you drew yourself, a little crooked, with everything you never managed to say out loud.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...I'm crying. I'm sorry. It's the best present ever.
  - _energetic:_ You DREW this?! It's so bad! I love it!
  - _cool:_ ...I'll read it again. Later. Alone.
  - _teasing:_ Such sweet words~ I'm keeping this to tease you for years.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ This is from me... Open it.
  - _energetic:_ My turn! Open it, open it!
  - _cool:_ Here. It's nothing special. ...Open it.
  - _teasing:_ My present is in this box. The other one comes later~
- **Narration:** Gloves. Hand-knitted, a little uneven. One finger is longer than the others. They're perfect.
- **Narration:** Later. Your room, the lights off, snow falling past the window.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Merry Christmas... I'm so happy to spend it with you.
  - _energetic:_ Merry Christmas! Best. Christmas. Ever.
  - _cool:_ Merry Christmas. ...Stay with me tonight.
  - _teasing:_ Merry Christmas~ Your last present is... me. Kidding. Maybe.
- **Narration:** Outside, the snow keeps falling. Inside, nobody is cold anymore.
- **Narration:** Christmas Eve. Lights on every street, couples everywhere.
- **Narration:** You spend it with the Koikatsu Club: a cake too big for the club room, paper hats, and a gift exchange.
- **Narration:** You get socks. Somebody else gets your socks. It's that kind of party.
- **Narration:** On the way home the snow starts. The street is quiet and the houses glow warm behind their curtains.
- **Narration:** Your phone buzzes. A message from {N}.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Merry Christmas... I just wanted to say it to you first.
  - _energetic:_ MERRY CHRISTMAS!!! Save me some cake!!
  - _cool:_ Merry Christmas. That's all. ...Don't read into it.
  - _teasing:_ Merry Christmas~ Next year, keep the evening free for me.
- **Narration:** Another buzz. "...I'm at the park near the station. If you're close."
- **Choice:** Run to the park | Reply and go home
- **Narration:** She's on the bench under the lamp, snow on her hair, holding two cans of hot cocoa. One for you.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You actually came... Merry Christmas. For real this time.
  - _energetic:_ YOU CAME! I bet myself you would!
  - _cool:_ ...You're out of breath. Idiot. Sit.
  - _teasing:_ Running to me on Christmas Eve? That's practically a confession~
- **Narration:** You sit together until the cocoa goes cold, watching the snow.
- **You:** Merry Christmas to you too. See you at school.
- **Narration:** Next year will be different.

