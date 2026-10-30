# Campus Life - dialogue written for the mod

Total lines (counting every personality version): 1298

Every line below was written for Campus Life (author: Joojkk). Lines of the game itself
(normal conversations, story events, H) are not listed. Lines marked 'by personality' have four
versions: gentle / energetic / cool / teasing, chosen from the girl's personality.

Placeholders like {Name}, {OName} (second girl), {place} or {item} are filled in during the game.

## Shared lines (reactions used by many scenes)
`Dialogue/Lines.cs`

### Lines / LikeHint
- **Her (by personality)**
  - _gentle:_ By the way... I really like {likeLabel}. I don't tell many people that. || By the way... I love {likeLabel}. Not many people know. || Um... I'm really fond of {likeLabel}. || I'll tell you a secret... {likeLabel} makes me so happy.
  - _energetic:_ Oh! Did I ever tell you I'm crazy about {likeLabel}? Like, crazy crazy! || I'm obsessed with {likeLabel}! Did you know?! || {likeLabel}! I love it so much! || Fun fact: {likeLabel} is my favorite thing ever!
  - _cool:_ If you must know, I have a weakness for {likeLabel}. || I'm partial to {likeLabel}. || {likeLabel}. A weakness of mine. || If you care: {likeLabel}.
  - _teasing:_ Want a hint? I go weak for {likeLabel}. Use that information wisely~ || Hint: {likeLabel}. Use it well~ || I melt for {likeLabel}. Remember that. || Want to win me over? {likeLabel}.

### Lines / GiftLiked
- **Her (by personality)**
  - _gentle:_ Th-this is for me? {item}... You remembered what I like. Thank you so much! || {item}? For me? You remembered! Thank you! || Oh... {item}... you know me so well. Thank you. || {item}! I'm so happy... thank you.
  - _energetic:_ No way! {item}?! You're the best! Seriously the best! || {item}?! Best gift ever! || No way! {item}! You rock! || {item}! I love it! I love you! Kidding! ...Kinda!
  - _cool:_ ...{item}. You actually paid attention. I'll treasure it. || ...{item}. You paid attention. Thank you. || ...{item}. I'll keep it safe. || ...{item}. Thoughtful.
  - _teasing:_ Oh? {item}, huh? You know exactly how to make a girl happy~ || {item}? You know how to please a girl~ || Oh my, {item}. Points for you. || {item}~ You're learning.

### Lines / GiftNeutral
- **Her (by personality)**
  - _gentle:_ Oh, {item}... Thank you. It's very kind of you. || {item}... thank you. That's kind. || Oh, {item}. Thank you. || Thank you for {item}.
  - _energetic:_ Huh, {item}? Cool, thanks! || {item}? Cool! Thanks! || Oh, {item}! Thanks! || Thanks for {item}!
  - _cool:_ A gift? ...Thank you. I'll accept it. || ...Thank you. || ...A gift. Thanks. || ...Accepted.
  - _teasing:_ Aww, a present? It's not really my thing, but I'll take the thought. || A present? Not my thing, but thanks. || Hm, {item}. It's the thought. || Cute. Not my style, but cute.

### Lines / GiftIntimateYes
- **Her (by personality)**
  - _gentle:_ Th-this is... You want me to... for you? ...Okay. Only for you. || I-is this... for me to wear? ...I'll think about it. Thank you. || Oh... oh my. Only you get to see this, okay? || F-for me to wear...? Only for you... || Ah... um... only you can see, okay? || This is... so embarrassing... thank you.
  - _energetic:_ Whoa, bold! ...Hehe, fine, I'll wear it. Just for you, got it? || Wow, you're bold! I kinda like it, though! || Seriously? Hehe, okay, you win. || Bold! Okay, just for you! || Whoa! Hehe, fine! || You're shameless! I like it!
  - _cool:_ ...You're shameless. I'll keep it. Don't expect me to say thanks out loud. || ...Interesting choice. I'll accept it. || Hm. Bold. I'll keep it. || ...Shameless. I'll keep it. || ...Bold. Accepted. || ...Hm. Kept.
  - _teasing:_ Ooh~ So that's what you've been thinking about. I'll show you sometime. || Oh my~ Want to see it on me sometime? || Naughty boy~ I'll put it to good use. || Want to see it on me? || Naughty. I'll model it. Someday. || So that's your taste~

### Lines / GiftIntimateNo
- **Her (by personality)**
  - _gentle:_ W-what?! I can't accept something like this! We're not... like that! || N-no! That's too... we're not that close! || I-I can't take this! It's embarrassing! || I can't accept this! We're not like that! || N-no! That's too much! || I-I can't! It's embarrassing!
  - _energetic:_ Eh?! Dude, what is wrong with you?! || Gross! No way! || Are you serious?! No! || What is WRONG with you?! || Ew! No! || Seriously?! No way!
  - _cool:_ Absolutely not. Know your place. || No. Inappropriate. || Take it back. Now. || Absolutely not. || Inappropriate. || Take it back.
  - _teasing:_ Wow. Bold move. Wrong girl. Try again in a few months. || Cute. No. Not yet. || Oh, you wish. Try harder. || Wrong girl. Try later. || Cute. Not yet. || In your dreams.

### Lines / GiftAlreadyToday
- **Her (by personality)**
  - _gentle:_ You already gave me something today... I can't accept more, really. || You're too generous... one is enough for today. || Thank you, but please, save it for another day. || You already gave me something... thank you, though. || One gift is plenty today. || Save it for tomorrow, please.
  - _energetic:_ Another one? Save some for tomorrow! || Whoa, two in one day? Slow down! || Hey, I already got one today! || Save some for tomorrow! || Two?! Slow down! || Already got one!
  - _cool:_ One gift a day is plenty. || Already got one today. || Enough for today. || One a day. || Enough today. || Tomorrow.
  - _teasing:_ Trying to buy me? One a day, cutie. || Spoiling me? Tomorrow, cutie. || Patience. One per day~ || Buying me? One a day. || Spoiling me? Tomorrow. || Patience, cutie.

### Lines / PlayerGivesGift
- **Variants:** Here, I got you something. {item}. | This is for you. {item}. | I saw this and thought of you.

### Lines / RequestThanks
- **Her (by personality)**
  - _gentle:_ You really came... Thank you. That means a lot to me. || You came! I'm so relieved. || Thank you for coming. Really. || You came... thank you so much. || I'm so glad you came. || You came for me... thank you.
  - _energetic:_ You made it! Knew I could count on you! || You're here! Awesome! || Yes! You came through! || You made it! || You came! Yay! || Knew you'd come!
  - _cool:_ You're on time. Good. I appreciate it. || You came. Thank you. || Punctual. I like that. || On time. Thank you. || You came. Good. || Punctual. Thanks.
  - _teasing:_ Took you long enough. ...Thanks, though. || Oh, you came. Good boy~ || Finally. I was starting to think you forgot. || Finally. Thanks. || Good boy. You came. || About time. Thanks.

### Lines / ErrandDelivered
- **Her (by personality)**
  - _gentle:_ You did it for me? Thank you, really! I owe you one. || You really did it... thank you so much. || You're so kind. I won't forget this. || You did it for me? Thank you! || Thank you so much... really. || I owe you. Thank you.
  - _energetic:_ You're a lifesaver! Seriously! || You're the best! Thank you! || Hero! My hero! || You saved me! || You're the best! || My hero!
  - _cool:_ Done already? Impressive. Thank you. || Efficient. Thank you. || You did it. I'm grateful. || Done? Thank you. || Efficient. Thanks. || Grateful.
  - _teasing:_ Look at you, being useful. I like that. || Good job~ I might reward you. || My, how helpful. I'll remember this. || Useful. I like it. || Good job. Reward pending. || Helpful boy. Noted.

### Lines / MeetAtLine
- **Her (by personality)**
  - _gentle:_ I like being here with you... the {place} feels different when you're around. || The {place} feels warmer with you here. || I like the {place} more when you're with me. || Being at the {place} with you is nice...
  - _energetic:_ See? The {place} is way more fun with company! || The {place} is way better with you! || Hanging at the {place} with you! Yay! || The {place} rocks with company!
  - _cool:_ The {place} is quiet at this hour. I like that. And you're... fine, too. || The {place} is calm. You're fine too. || The {place} is quiet. I don't mind sharing it. || The {place}. Peaceful. With you, still peaceful.
  - _teasing:_ Just the two of us at the {place}~ Don't get any funny ideas. Or do. || Alone at the {place}~ Behave. Or don't. || Just us at the {place}. Hmm~ || The {place}, you and me. Interesting.

### Lines / NoteHelp
- **Her (by personality)**
  - _gentle:_ { could you go over the main points with me?")} || Y-you came... could you help me practice my presentation? I'm so nervous... || Thanks for coming... could you quiz me for tomorrow's test? || You're here... I need help with my history report. Please?
  - _energetic:_ { Help!")} || You came! Help me pick songs for the club party! || Yay! I need help making flyers for the club! || You're here! Help me plan the class trip game!
  - _cool:_ {")} || You're here. I need someone to check my essay. Quietly. || Good. Proofread my report. Ten minutes. || You came. I need a practice partner for English.
  - _teasing:_ {")} || You came. Rate my speech. Honestly. || Look who's here. I need an honest critic for my poem. || You showed up. Help me pick a costume for the festival.
- **Variants:** Y-you came... I have to read my report in front of the class tomorrow and I freeze every time. Could you listen while I practice? | Th-thanks for coming. I lost the notes for tomorrow's test... could you go over the main points with me?
- **Variants:** You came! Okay, don't laugh: I signed up to give the club speech and I have no idea what to say. Help me write it? | Yay, you're here! I'm making the poster for the campus festival and it looks like a ransom note. Help!
- **Variants:** Good, you're here. I'm stuck on the math assignments and I'd rather not ask in class. Ten minutes? | You came. I need someone to time my run-through for the class presentation. Quietly.
- **Variants:** Hey, you actually came. I need a second opinion on the festival poster, and you're the only one who'll be honest. | Look who showed up. I'm writing the class newsletter and I need a real person to tell me if it's boring.

### Lines / NoteThanks
- **Her (by personality)**
  - _gentle:_ ...That really helped. I think I can do it now. Thank you. || Thank you... I feel so much better about it now. || You helped a lot. I'm not scared anymore. || That helped so much. Thank you. || I feel ready now. Thank you. || You're wonderful. Thank you.
  - _energetic:_ Done! You're the best. I owe you a juice! || We did it! High five! || Finished! I'm buying you something! || Done! Juice is on me! || We did it! || Finished! You rock!
  - _cool:_ That's settled, then. ...Thanks. I mean it. || Done. Thank you for your time. || That helped. Thank you. || Settled. Thanks. || Done. Appreciated. || Good. Thank you.
  - _teasing:_ Okay, that's actually good now. Don't let it go to your head~ || Not bad, partner~ || Okay, you're useful. I'll admit it. || Not bad, partner. || Okay, you're useful. || That's good now. Don't get smug.

### Lines / LikeHint
- **Her (by personality)**
  - _gentle:_ Can I tell you something? I really love {likeLabel}. || Can I tell you...? I love {likeLabel}. || I really like {likeLabel}... it's my favorite. || {likeLabel}... it makes me so happy.
  - _energetic:_ Oh! Did I ever tell you I'm crazy about {likeLabel}? Like, crazy crazy! || {likeLabel}! Obsessed! || I LOVE {likeLabel}! || Did you know?! {likeLabel}!
  - _cool:_ ...I have a weakness for {likeLabel}. Keep that between us. || {likeLabel}. Between us. || A weakness: {likeLabel}. || I'm fond of {likeLabel}. Quietly.
  - _teasing:_ Want a hint? I'm a sucker for {likeLabel}. Use that wisely. || Hint: {likeLabel}. || I'm a sucker for {likeLabel}. || {likeLabel}. Use it wisely.
- **Her (by personality)**
  - _gentle:_ Oh, um... I like {likeLabel}, if you were wondering. || I like {likeLabel}, by the way. || Um, {likeLabel} is nice. I like it. || I'm fond of {likeLabel}.
  - _energetic:_ Oh, and I'm really into {likeLabel}! || I'm into {likeLabel}! || {likeLabel}! Love it! || Big fan of {likeLabel}!
  - _cool:_ If you must know, I like {likeLabel}. || I like {likeLabel}. || {likeLabel}. That's my preference. || {likeLabel}, if you care.
  - _teasing:_ I like {likeLabel}. Don't spread it around. || {likeLabel}. Keep it quiet. || I like {likeLabel}. Secret. || {likeLabel}. Don't tell.

## Conversation (topics, Let her talk, Ask about, jokes, compliments, flirting)
`Talk/Conversation.cs`

### Opening
- **Choice:** Promise to keep it secret. | Tease her about it.
- **Her:** {RomanceSay}
- **Narration:** {WorriedHint} <color=#6ab4ff>[INT]</color> Ask her... > Just listen.

### LetHerTalk
- **Her:** {Everyday}
- **Her:** {GossipLine}
- **Her:** {Everyday}
- **Her:** {Everyday}

### AskSchool
- **Her:** {GossipLine}

### Joke
- **Her:** {Everyday}
- **Her:** {Everyday}

### Compliment
- **You:** {mine ?? RomanceMe}
- **Her:** {RomanceSay}
- **Her:** {ok ? RomanceSay}

### Flirt
- **You:** {RomanceMe}
- **Her:** {RomanceSay}
- **Her:** {RomanceSay}

### HoldHand
- **You:** {RomanceMe}
- **Her:** {RomanceSay}

### AskFeelings
- **You:** {RomanceMe}

### Tease
- **You:** {RomanceMe}
- **Her:** {RomanceSay}

### Whisper
- **You:** {RomanceMe}
- **Her:** {RomanceSay}

### Memories
- **You:** {RomanceMe}
- **Her:** {RomanceSay}

### Sneak
- **You:** {RomanceMe}
- **Her:** {RomanceSay}

### Explain
- **Her:** {Everyday}

### Apologize
- **Her:** {Everyday}

### MakePlans
- **You:** {PlayerMakePlans}
- **Her:** {Everyday}

## Conversation lines (openings, gossip, friends, romance, clearing the air)
`Dialogue/ConvoLines.cs`

### ConvoLines / Cheating
- **Her (by personality)**
  - _gentle:_ I saw you... with {other}. I saw it. How could you...? || I saw you with {other}... how could you...? || You and {other}... I saw everything... why? || I saw... you and {other}... it hurts...
  - _energetic:_ I SAW YOU! With {other}! Don't you dare say it's nothing! || I SAW YOU with {other}! Don't say nothing! || You and {other}! I saw! Don't lie! || {other}?! I SAW!
  - _cool:_ I saw you with {other}. Don't insult me with excuses. || You and {other}. I saw. No excuses. || I saw. {other}. Spare me the excuses. || {other}. Witnessed. Don't lie.
  - _teasing:_ I had a lovely view of you and {other}. Explain. Slowly. || Lovely view of you and {other}. Explain slowly. || You and {other}. Explain. Carefully. || I watched you and {other}. Talk.
- **Her (by personality)**
  - _gentle:_ I saw you kiss {other}... I trust you. I just... want to hear it from you. || I saw you kiss {other}... I trust you. Just tell me. || You kissed {other}... please explain it to me. || I saw the kiss with {other}... I want to hear it from you.
  - _energetic:_ I saw you kiss {other}! Ugh, fine, as long as I'm still number one! || You kissed {other}! Fine, as long as I'm first! || Kissing {other}?! Okay! I'm still number one, right?! || Saw you and {other}! I'm still top, right?!
  - _cool:_ I saw the kiss with {other}. I'm not angry. I'm noting it. || The kiss with {other}. Not angry. Noting it. || {other}. Kiss. Recorded. || I saw. {other}. Noted.
  - _teasing:_ Kissing {other} where I can see? You do like living dangerously~ || Kissing {other} in front of me? Daring~ || Risky, kissing {other} where I can see~ || You do love danger, kissing {other}~
- **Her (by personality)**
  - _gentle:_ People say you were sweet-talking {other}... I trust you, okay? || They say you flirted with {other}... I trust you. || You and {other}...? I believe in you, okay? || I heard about {other}... I trust you.
  - _energetic:_ Heard you were flirting with {other}! Hah, as long as you come back to me! || Flirting with {other}! Come back to me! || {other}?! Ha! You're still mine! || Heard about {other}! Just come home to me!
  - _cool:_ I hear things about you and {other}. I'm not the jealous type. Mostly. || Things about {other}. I'm not jealous. Mostly. || About {other}... I'm not the jealous type. Usually. || {other}. Noted. Not jealous. Much.
  - _teasing:_ You and {other}? Cute. Just remember who you belong to~ || {other}? Remember who owns you~ || Cute, {other}. You're mine, though~ || Have fun with {other}. You're still mine~
- **Her (by personality)**
  - _gentle:_ Is it true...? You and {other}...? I thought we were... || You and {other}...? I thought we... || Is it real...? {other}...? || I thought... we were together...
  - _energetic:_ Hey! What's this about you and {other}?! I'm your girlfriend! || You and {other}?! I'm your girlfriend! || {other}?! Hello?! Girlfriend here! || What's with {other}?! I'm yours!
  - _cool:_ Explain {other}. Now. || {other}. Explain. || Explain {other}. Immediately. || {other}. Now.
  - _teasing:_ Flirting with {other} behind my back? You've got some nerve. || {other} behind my back? Nerve. || Behind my back with {other}? Bold. || Flirting with {other}? Brave.

### ConvoLines / FriendMood / step Mood.Stressed
- **Her (by personality)**
  - _gentle:_ {friend} looks so stressed about tests... maybe she'd like to study with someone. || {friend} is stressed about tests... she might like a study partner. || {friend} looks worried about exams... maybe help her study? || {friend} is anxious about tests...
  - _energetic:_ {friend}'s freaking out about assignments! || {friend}'s panicking about assignments! || {friend}'s assignments meltdown! || {friend} is drowning in assignments!
  - _cool:_ {friend} is overwhelmed. Studying might help her. || {friend} is overwhelmed. Study with her. || {friend} needs a study partner. || {friend}. Overloaded. Help her study.
  - _teasing:_ {friend} is a wreck about exams. Go play hero. || {friend}'s an exam mess. Be a hero. || {friend} needs rescuing. Exams. || Play tutor for {friend}.

### ConvoLines / FriendMood / step Mood.Hungry
- **Her (by personality)**
  - _gentle:_ {friend} forgot her lunch today, I think. || I think {friend} forgot her lunch... || {friend} didn't bring lunch today... || {friend} looks hungry...
  - _energetic:_ {friend} was starving this morning! || {friend} was starving! || {friend}'s tummy was growling! || {friend} needs food!
  - _cool:_ {friend} skipped breakfast again. || {friend} skipped breakfast. || {friend} hasn't eaten. || {friend}. No breakfast.
  - _teasing:_ {friend} is hungry. Feed her and she's yours. || Feed {friend}. She'll be yours. || {friend}'s hungry. Opportunity. || Snack for {friend} = points.

### ConvoLines / FriendMood / step Mood.Lonely
- **Her (by personality)**
  - _gentle:_ {friend} has seemed a little lonely lately... maybe you could talk to her? || {friend} seems lonely... maybe talk to her? || {friend} looks a little alone... || Could you talk to {friend}? She seems lonely.
  - _energetic:_ {friend} looks kinda down! Go cheer her up! || {friend} looks down! Cheer her up! || Go cheer {friend} up! || {friend} needs a smile!
  - _cool:_ {friend} could use company. || {friend} needs company. || {friend} is alone a lot. || {friend}. Lonely.
  - _teasing:_ {friend} is moping. You could fix that. || {friend} is moping. Fix it. || Go un-mope {friend}. || {friend} needs you. Go.

### ConvoLines / FriendMood / step Mood.Upset
- **Her (by personality)**
  - _gentle:_ Something happened to {friend}... she's upset. Be gentle with her. || {friend} is upset... be gentle. || Something happened to {friend}... be kind. || {friend} is sad... careful with her.
  - _energetic:_ {friend} is in a bad mood! Careful! || {friend}'s grumpy! Careful! || {friend} is mad! Watch out! || Bad mood {friend}! Danger!
  - _cool:_ {friend} is upset about something. || {friend} is upset. || {friend}. Upset. || Something's wrong with {friend}.
  - _teasing:_ Don't poke {friend} today. Trust me. || Don't poke {friend} today. || Avoid {friend}. Trust me. || {friend} bites today.

### ConvoLines / FriendMood / step Mood.Cheerful
- **Her (by personality)**
  - _gentle:_ {friend} is in a great mood today. || {friend} is so happy today. || {friend} is in a lovely mood. || {friend} looks cheerful.
  - _energetic:_ {friend} is bouncing off the walls today! || {friend} is SO hyper! || {friend}'s bouncing around! || {friend} is super happy!
  - _cool:_ {friend} seems happy. || {friend} is happy. || {friend}. Cheerful. || {friend} seems well.
  - _teasing:_ {friend} is unbearably cheerful today. || {friend} is annoyingly happy. || {friend}'s sunshine today. Ugh. || {friend} won't stop smiling.

### ConvoLines / FriendMood / step Mood.Bored
- **Her (by personality)**
  - _gentle:_ {friend} said she's bored. || {friend} is bored, she said. || {friend} said she has nothing to do. || {friend} seems bored.
  - _energetic:_ {friend} is SO bored, she said so ten times! || {friend} said "bored" ten times! || {friend} is mega bored! || {friend} is dying of boredom!
  - _cool:_ {friend} is bored out of her mind. || {friend} is very bored. || {friend}. Bored. || {friend} has nothing to do.
  - _teasing:_ {friend} is bored. Entertain her. || Entertain {friend}. || {friend} needs amusement. || Go amuse {friend}.

### ConvoLines / FriendMood / step Mood.Sleepy
- **Her (by personality)**
  - _gentle:_ {friend} nearly fell asleep in class. || {friend} almost dozed off in class. || {friend} looked so sleepy... || {friend} was nodding off.
  - _energetic:_ {friend} was snoring in math! || {friend} snored in math! || {friend} fell asleep in class! || Zzz! {friend} in class!
  - _cool:_ {friend} looks exhausted. || {friend} is exhausted. || {friend}. Tired. || {friend} needs sleep.
  - _teasing:_ {friend} was drooling on her notes. Cute. || {friend} drooled on her notes. Cute. || {friend} napped. Adorably. || Sleepy {friend}. Precious.

### ConvoLines / FriendMood / step Mood.Flirty
- **Her (by personality)**
  - _gentle:_ {friend} seems to be in a really good mood today... maybe there's someone she likes? || {friend} is so cheerful... does she like someone? || {friend} keeps smiling... a crush? || {friend} seems happy... someone special?
  - _energetic:_ {friend} keeps smiling at her phone! || {friend} keeps grinning at her phone! || {friend} is texting someone! Smiling! || {friend}'s giggling at messages!
  - _cool:_ {friend} is acting strangely happy. || {friend} is oddly happy. || {friend}. Unusually cheerful. || {friend}. Suspiciously happy.
  - _teasing:_ {friend}'s got that look today. You know the one. || {friend} has that look. || You know {friend}'s face today. || {friend}'s in love. Maybe.
- **Her (by personality)**
  - _gentle:_ {friend}'s doing fine, I think. || {friend} is doing fine, I think. || {friend} seems okay. || I think {friend} is well.
  - _energetic:_ {friend}'s good! Same as always! || {friend}'s great! || {friend}? Good as always! || {friend} is fine!
  - _cool:_ She's fine. || Fine. || She's well. || No change.
  - _teasing:_ {friend}? Same as ever. || {friend}? Same old. || {friend}? Unchanged. || {friend}. Boring as usual.

### ConvoLines / FriendBond
- **Her (by personality)**
  - _gentle:_ {friend} and I have been friends for a long time. She's really important to me. || {friend} and I go way back. She's precious to me. || {friend} is so important to me. || {friend} and I have been friends forever.
  - _energetic:_ {friend}'s my best friend! We do everything together! || {friend}'s my BFF! || {friend} and me! Best friends! || Me and {friend} do everything!
  - _cool:_ {friend} and I understand each other. That's rare. || {friend} and I understand each other. || {friend}. We get each other. || {friend} and I. A rare bond.
  - _teasing:_ {friend}? She's tolerable. ...She's my best friend, okay? || {friend}? Fine. ...Best friend, okay? || She's okay. ...My best friend. Shut up. || {friend}? Bearable. ...I love her, okay?

### ConvoLines / Rival
- **Her (by personality)**
  - _gentle:_ Um... {friend} and I don't really get along... || {friend} and I... don't get along... || Um... {friend} and I aren't close... || I'm not friends with {friend}...
  - _energetic:_ Ugh, why ask ME about {friend}?! || Why ask ME about {friend}?! || {friend}?! Don't ask me! || Ugh! Not {friend}!
  - _cool:_ I don't keep track of {friend}. || I don't follow {friend}. || {friend}. Not my concern. || I don't track {friend}.
  - _teasing:_ Why are you asking about HER? || Why HER? || Asking about HER? Really? || Her? Why?

### ConvoLines / FriendProblem
- **Her (by personality)**
  - _gentle:_ Actually... {friend} has been worried. {problem} Maybe you could help her? || {friend} has been worried, actually. {problem} Could you help her? || Um... {friend} is troubled. {problem} Maybe you can help? || I'm worried about {friend}. {problem} Would you help her?
  - _energetic:_ Oh! {friend} has a problem! {problem} You should help her! || {friend}'s got trouble! {problem} Help her! || Hey! {friend} needs you! {problem} || News! {problem} Go help {friend}!
  - _cool:_ {friend} won't ask anyone, but: {problem} Just so you know. || {friend} won't say it: {problem} Now you know. || For your information: {problem} {friend} won't ask. || {problem} {friend} keeps it to herself.
  - _teasing:_ Want a tip? {problem} {friend} would never admit it, though. || Tip: {problem} {friend} would deny it. || Insider info: {problem} Don't tell {friend} I said. || Psst. {problem} {friend} won't admit it.

## Two-girl scenes
`Systems/Duo.cs`

### Plan / Compose
- **Her (by personality)**
  - _gentle:_ Um... {bn} told me what you said to her. That wasn't very nice... || {bn} told me what you said... that wasn't kind. || Um... {bn} was hurt by what you said. || What you said to {bn}... it made her sad.
  - _energetic:_ Hey! What's the big idea, being mean to {bn}?! || Hey! Why were you mean to {bn}?! || Not cool, being rude to {bn}! || What's your problem with {bn}?!
  - _cool:_ I heard how you treated {bn}. Explain. || You mistreated {bn}. Explain. || {bn} told me. Account for it. || Your behavior toward {bn}. Explain.
  - _teasing:_ So you were rude to my friend. Want to try that again? || Rude to my friend? Try again. || You hurt {bn}. Bold. || So you were nasty to {bn}. Brave.
- **Her (by personality)**
  - _gentle:_ You promised {bn} something... and didn't keep it. She was really waiting... || You promised {bn}... and didn't come. She waited. || {bn} waited for you... you forgot. || You broke your promise to {bn}...
  - _energetic:_ Hey! You stood {bn} up! Not cool! || You stood {bn} up! Uncool! || Hey! {bn} waited forever! || You ditched {bn}! Seriously?!
  - _cool:_ You gave {bn} your word. Then nothing. || You promised {bn}. Nothing followed. || A broken promise. To {bn}. || {bn} trusted your word.
  - _teasing:_ Breaking promises to my friend? Bold move. || Breaking promises to my friend? Brave. || Ghosting {bn}? Bold. || You flaked on {bn}. Explain.
- **Her (by personality)**
  - _gentle:_ I-it's fine, really... you don't have to... || It's okay, really... || Y-you don't have to... || It's fine... honestly.
  - _energetic:_ Ehh, I'm not THAT mad... okay, a little. || I'm not THAT mad! Kinda! || Okay, maybe a little mad! || Eh, I'm only a bit upset!
  - _cool:_ ...I can speak for myself. But yes. || ...I can speak. But yes. || ...True. || ...Yes. That.
  - _teasing:_ Hmph. Let him squirm a little. || Let him squirm. || Make him sweat a bit. || Hmph. Let him suffer.
- **Her (by personality)**
  - _gentle:_ ...Okay. Thank you for saying that. || Okay. Thank you. || Thank you for saying that. || ...I forgive you.
  - _energetic:_ Apology accepted! Just this once! || Accepted! This time! || Okay! Forgiven! || Fine! Apology accepted!
  - _cool:_ ...Accepted. || ...Accepted. || ...Fine. || ...Very well.
  - _teasing:_ Fine. You're forgiven. Barely. || Forgiven. Barely. || Fine. Just barely. || You're forgiven. Don't push it.
- **Her (by personality)**
  - _gentle:_ See? That wasn't so hard. || See? Easy. || That wasn't so hard, was it? || Good... thank you.
  - _energetic:_ Good! Now we're all friends again! || Yay! Friends again! || Great! All good now! || Perfect! Group hug!
  - _cool:_ Good. || Good. || Settled. || Fine.
  - _teasing:_ Hm. Respectable. || Respectable. || Not bad. || Hm. Decent.
- **Her (by personality)**
  - _gentle:_ Oh... I didn't know that... || I didn't know... || Oh... I see now. || Ah... that's different.
  - _energetic:_ Ohh, so THAT's what happened! || Ohh, THAT'S what happened! || Oh! Got it! || Ahh, makes sense!
  - _cool:_ ...That changes things. || ...That changes things. || ...Different context. || ...I see.
  - _teasing:_ Hm. Alright, that's a decent excuse. || Decent excuse. || Alright, fair. || Hm. Acceptable reason.
- **Her (by personality)**
  - _gentle:_ ...I see. Come on, {bn}. || ...I see. Let's go, {bn}. || ...Okay. Come on, {bn}. || ...Alright. {bn}, let's go.
  - _energetic:_ Wow. Unbelievable. Let's go, {bn}! || Unbelievable! Come on, {bn}! || Wow! Let's go, {bn}! || Ugh! {bn}, we're leaving!
  - _cool:_ Noted. Let's go. || Noted. Go. || Understood. {bn}, come. || Fine. Let's go.
  - _teasing:_ Charming. Come on, {bn}. || Charming. {bn}, let's go. || Lovely. Come on, {bn}. || Wow. {bn}, we're done here.
- **Her (by personality)**
  - _gentle:_ Go on, {bn}... you wanted to say something, right? || Go ahead, {bn}... || {bn}, you wanted to tell him, right? || Come on, {bn}... say it.
  - _energetic:_ C'mon, {bn}, tell him! You've been talking about it all day! || C'mon, {bn}! Tell him! || Go, {bn}! You talked about it all day! || {bn}! Spit it out!
  - _cool:_ She has something to tell you. || She has something to say. || {bn}. Speak. || Go on, {bn}.
  - _teasing:_ Go on. Don't be shy now~ || Don't be shy now~ || Go on, {bn}~ || Say it, {bn}. He's waiting.
- **Her (by personality)**
  - _gentle:_ Thank you... for taking my side. It meant a lot. || Thank you for taking my side. || You defended me... thank you. || It meant so much... thank you.
  - _energetic:_ Thanks for having my back! You're the best! || You had my back! Thanks! || Thanks for sticking up for me! || You're the best! Thanks!
  - _cool:_ ...Thank you for standing up for me. || ...Thank you for that. || ...You stood up for me. Thanks. || ...I appreciate it.
  - _teasing:_ So. You took my side. ...Thanks, I guess. || You took my side. Thanks, I guess. || So, my side. Thanks. || Heh. Thanks for that.
- **Her (by personality)**
  - _gentle:_ Thank you for helping me the other day... || Thank you for helping me... || Thanks for the other day... || I'm grateful for your help.
  - _energetic:_ Thanks for the help! Seriously! || Thanks for the help! || Seriously, thanks! || You helped! Thanks!
  - _cool:_ Thank you for your help. I mean it. || Thank you. I mean it. || Your help mattered. Thanks. || Thanks. Sincerely.
  - _teasing:_ You helped me out. I owe you one... maybe. || I owe you. Maybe. || You helped. I'll consider repaying. || Thanks. Don't let it go to your head.
- **Her (by personality)**
  - _gentle:_ It wasn't nothing to me... || It wasn't nothing to me... || It meant a lot to me... || It mattered to me...
  - _energetic:_ Don't be so humble! || Don't be humble! || Stop being modest! || Take the credit!
  - _cool:_ ...Still. Thanks. || ...Still. Thanks. || ...Regardless. Thanks. || ...Thank you anyway.
  - _teasing:_ Modest, too? Hm. || Modest? Hm. || Humble, too? || Oh, modest. Cute.
- **Her (by personality)**
  - _gentle:_ Ehehe... he's reliable, isn't he? || He's reliable, isn't he? || Hehe... he's dependable. || He's so kind, right?
  - _energetic:_ See?! Told you he's great! || Told you he's great! || See?! Awesome guy! || He's cool, right?!
  - _cool:_ Reliable. Good. || Reliable. Good. || Dependable. || Competent.
  - _teasing:_ Ooh, smooth. || Smooth. || Ooh, charming. || Slick.
- **Her (by personality)**
  - _gentle:_ I-I'm sorry, I didn't... || I-I'm sorry, I didn't mean... || Ah! Sorry... || Oh no... sorry...
  - _energetic:_ Whoa, whoa, okay, I can explain! || Whoa! I can explain! || Wait! Explanation! || Uh! Hold on!
  - _cool:_ ...This is awkward. || ...Awkward. || ...Uncomfortable. || ...Hm.
  - _teasing:_ Oh, this is going to be fun. || This'll be fun. || Oh, entertaining. || Ooh, drama.
- **Her (by personality)**
  - _gentle:_ ...Never mind. Sorry. || ...Never mind. Sorry. || ...Forget it. || ...It's nothing.
  - _energetic:_ ...Forget it! Sorry! || Forget it! Sorry! || Never mind! || Nothing! Sorry!
  - _cool:_ ...Never mind. || ...Never mind. || ...Disregard. || ...Nothing.
  - _teasing:_ ...Eh, forget it. || ...Forget it. || ...Eh, whatever. || ...Never mind.
- **Her (by personality)**
  - _gentle:_ Um, I was hoping to talk to you a little... || I hoped to talk to you a bit... || Um, could we talk? || I wanted to see you...
  - _energetic:_ Hey! I wanted to hang out with you! || I wanted to hang out! || Hey! Let's chill! || Hang with me!
  - _cool:_ I wanted a word with you. || A word. || I wanted to talk. || A moment.
  - _teasing:_ Mind if I borrow him for a bit? || Can I borrow him? || Mind if I steal him? || He's coming with me for a bit.
- **Her (by personality)**
  - _gentle:_ Ah... he's with me today, sorry... || He's with me today... sorry. || Sorry... he's mine today. || Ah... we're together today.
  - _energetic:_ Hey, hey! He's MY boyfriend, you know! || He's MY boyfriend! || Hands off! Mine! || Hey! Taken!
  - _cool:_ He's busy. With me. || He's busy. With me. || Occupied. By me. || He's mine today.
  - _teasing:_ Borrow? He's not a library book. He's mine. || He's not a library book. Mine. || Borrow? No. Mine. || Not available. Mine.
- **Her (by personality)**
  - _gentle:_ ...Thank you. || ...Thank you. || ...Thanks. || ...I'm glad.
  - _energetic:_ Hehe! See? || See?! || Hehe! || Told you!
  - _cool:_ Good answer. || Good answer. || Correct. || Good.
  - _teasing:_ Heh. Knew it. || Knew it. || Obviously. || Heh.
- **Her (by personality)**
  - _gentle:_ I see... sorry for interrupting. || Sorry for interrupting. || I see... sorry. || Okay... another time.
  - _energetic:_ Aw, okay! Another time! || Another time! || Okay! Later! || Aw! Next time!
  - _cool:_ ...Understood. || ...Understood. || ...Fine. || ...Noted.
  - _teasing:_ Hmph. Fine. For now. || Fine. For now. || Hmph. Later. || For now.
- **Her (by personality)**
  - _gentle:_ ...Okay. If you say so. || If you say so... || Okay... || Alright.
  - _energetic:_ Fiiine. Together. || Fine! Together! || Okay! All of us! || Fiiine!
  - _cool:_ ...Fine. || ...Fine. || ...Acceptable. || ...Alright.
  - _teasing:_ Generous of you. Hmph. || Generous. Hmph. || How kind. Hmph. || Fine. Hmph.
- **Her (by personality)**
  - _gentle:_ Thank you! I'm happy. || Thank you! I'm happy. || Yay... thanks. || Thank you so much.
  - _energetic:_ Yay! || Yay! || Woo! || Yes!
  - _cool:_ Thanks. || Thanks. || Good. || Appreciated.
  - _teasing:_ How kind~ || How kind~ || Sweet~ || Generous~
- **Variants:** the last melon bread | whose turn it was to clean the lecture hall | who gets the window seat | the club room schedule | a borrowed manga that came back bent
- **Her (by personality)**
  - _gentle:_ You saw it too, right? It was my turn... || It was my turn, right? || You saw it too...? || Tell her it was my turn...
  - _energetic:_ Tell her I'm right! || Tell her I'm right! || I'm right, right?! || Back me up!
  - _cool:_ You're impartial. Settle this. || You're neutral. Decide. || Settle it. || Judge.
  - _teasing:_ Go on, tell her she's wrong. || Tell her she's wrong. || Go on, choose me. || Say I'm right.
- **Her (by personality)**
  - _gentle:_ Please, say something... || Please, say something... || Help us... || Um... what do you think?
  - _energetic:_ No way, I'm right! Right?! || I'm right! Right?! || Tell her! || Me! Obviously!
  - _cool:_ This is ridiculous. Decide. || Ridiculous. Decide. || Pick. || Resolve this.
  - _teasing:_ Oh, this'll be good. Choose wisely. || Choose wisely. || Careful now. || This'll be fun.
- **Her (by personality)**
  - _gentle:_ Thank you...! || Thank you! || Yay... thanks! || I knew it!
  - _energetic:_ Ha! Told you! || Told you! || Ha! || Yes!
  - _cool:_ Obviously. || Obviously. || Naturally. || Of course.
  - _teasing:_ See? || See? || Told you. || Duh.
- **Her (by personality)**
  - _gentle:_ ...I see how it is. || ...I see. || ...Oh. || ...Okay.
  - _energetic:_ Traitor! || Traitor! || Hey! || Unfair!
  - _cool:_ ...Noted. || ...Noted. || ...Hm. || ...Remembered.
  - _teasing:_ Wow. Okay. || Wow. || Okay then. || Noted.
- **Her (by personality)**
  - _gentle:_ Thank you...! || Thank you! || Yay... thanks! || I knew it!
  - _energetic:_ Ha! Told you! || Told you! || Ha! || Yes!
  - _cool:_ Obviously. || Obviously. || Naturally. || Of course.
  - _teasing:_ See? || See? || Told you. || Duh.
- **Her (by personality)**
  - _gentle:_ ...I see how it is. || ...I see. || ...Oh. || ...Okay.
  - _energetic:_ Traitor! || Traitor! || Hey! || Unfair!
  - _cool:_ ...Noted. || ...Noted. || ...Hm. || ...Remembered.
  - _teasing:_ Wow. Okay. || Wow. || Okay then. || Noted.
- **Her (by personality)**
  - _gentle:_ That's... actually fair. || That's fair. || That works... || Okay, that's fair.
  - _energetic:_ Huh. Okay, fine! || Fine! Okay! || Huh! Alright! || Okay, fair!
  - _cool:_ Acceptable. || Acceptable. || Reasonable. || Fair.
  - _teasing:_ Hmph. Smart. || Smart. || Clever. || Hmph. Good.
- **Her (by personality)**
  - _gentle:_ Yay... thank you. || Yay... thank you. || Thank you! || I'm so happy.
  - _energetic:_ Woohoo! || Woohoo! || Yay! || Yes!
  - _cool:_ Good. || Good. || Fine. || Excellent.
  - _teasing:_ Knew you'd say yes~ || Knew you'd agree~ || Of course~ || Obviously~
- **Her (by personality)**
  - _gentle:_ Ah... okay, next time. || Okay... next time. || Another time, then. || Alright...
  - _energetic:_ Boo! Next time then! || Boo! Next time! || Aww! Later! || Next time!
  - _cool:_ ...Understood. || ...Understood. || ...Fine. || ...Later.
  - _teasing:_ Your loss~ || Your loss~ || Pity~ || Too bad~
- **Her (by personality)**
  - _gentle:_ This is my friend {nn}! I wanted you two to meet. || This is {nn}, my friend! || I'd like you to meet {nn}. || Meet {nn}... she's my friend.
  - _energetic:_ Meet {nn}, my best friend! Be nice! || Meet {nn}! Bestie! || This is {nn}! Be nice! || Say hi to {nn}!
  - _cool:_ This is {nn}. You should know each other. || {nn}. You should meet. || This is {nn}. || {nn}. Introductions.
  - _teasing:_ Here, I brought you a friend. Her name's {nn}. || Brought you a friend. {nn}. || Here's {nn}. Be charming. || {nn}. Behave yourself.
- **Her (by personality)**
  - _gentle:_ N-nice to meet you... || N-nice to meet you... || H-hello... || Um... hi...
  - _energetic:_ Heya! I've heard about you! || Heya! Heard about you! || Hi! Finally! || Hey hey!
  - _cool:_ Hello. || Hello. || Hi. || ...Hello.
  - _teasing:_ So you're the famous club guy. Hm. || The famous club guy. Hm. || So you're him. || Oh, you're the one.
- **Her (by personality)**
  - _gentle:_ You're nice... I'm glad. || You're nice. I'm glad. || You seem kind... || I like you.
  - _energetic:_ Ooh, I like him! || I like him! || He's fun! || Cool guy!
  - _cool:_ ...Pleasure. || ...Pleasure. || ...Nice to meet you. || ...Fine.
  - _teasing:_ Polite. I approve. || Polite. Approved. || Good manners. || Not bad.
- **Her (by personality)**
  - _gentle:_ Um... he's nicer than he looks, I promise! || He's nicer than he looks! || He's really kind, I promise! || He's sweet, really!
  - _energetic:_ Hey, be friendlier! || Be friendlier! || Smile more! || Hey, be nice!
  - _cool:_ ...Charming as always. || ...Charming. || ...As usual. || ...Typical.
  - _teasing:_ Wow, so cool and mysterious. || So mysterious. || Wow, so cool. || Brooding, huh?
- **Her (by personality)**
  - _gentle:_ You and {jn} are always together lately... can I join sometimes? || You and {jn} are always together... can I join? || {jn} and you are so close... may I come along? || I'd like to join you and {jn} sometimes...
  - _energetic:_ Hey! You keep stealing {jn} from me! Or me from... wait. Whatever! || You keep stealing {jn}! || Hey! {jn}'s always with you! || No fair! {jn} time!
  - _cool:_ You two have been inseparable. I've noticed. || You and {jn}. Inseparable. || I've noticed you two. || {jn} and you. Always.
  - _teasing:_ So you've been monopolizing {jn}. Or is it the other way around? || Monopolizing {jn}? || Hogging {jn}, are we? || You and {jn}? Suspicious.
- **Her (by personality)**
  - _gentle:_ Ah, sorry! I didn't mean to leave you out... || Sorry! I didn't mean to leave you out... || Ah, sorry... || I'm sorry, we forgot you...
  - _energetic:_ Haha, don't be jealous! || Don't be jealous! || Haha! Jealous?! || Aww, jealous!
  - _cool:_ ...It's not like that. || ...Not like that. || ...It's nothing. || ...Misunderstanding.
  - _teasing:_ Jealous? How cute. || Jealous? Cute. || Aww, envious? || Jealousy suits you.
- **Her (by personality)**
  - _gentle:_ Really? Okay! || Really? Okay! || Yay... okay! || Thank you!
  - _energetic:_ Yes! Group hangout! || Group hangout! || Yes! All of us! || Woo!
  - _cool:_ ...Fine. Good. || ...Good. || ...Fine. || ...Agreed.
  - _teasing:_ Deal. || Deal. || Done. || Agreed.
- **Her (by personality)**
  - _gentle:_ J-just us? Okay...! || Just us? Okay...! || J-just the two of us? || Oh... okay!
  - _energetic:_ A date? Kidding! Lunch it is! || Date? Kidding! Lunch! || Lunch! Hehe! || Just us! Fun!
  - _cool:_ ...I'll hold you to it. || ...I'll remember. || ...Noted. || ...Holding you to it.
  - _teasing:_ Oh? Now that's interesting. || Now that's interesting. || Oh? Just us? || Intriguing.
- **Her (by personality)**
  - _gentle:_ ...Oh. Okay. || ...Oh. Okay. || ...I see. || ...Alright.
  - _energetic:_ Wow. Okay then! || Wow. Okay! || Fine then! || Huh! Okay!
  - _cool:_ ...Right. || ...Right. || ...Hm. || ...Noted.
  - _teasing:_ Noted. Very noted. || Noted. || Very noted. || Remembered.
- **Her (by personality)**
  - _gentle:_ It's true, I heard it too... || I heard it too... || It's true... || Yes, I heard...
  - _energetic:_ Totally true! Everyone knows! || Totally true! || Everyone knows! || Yep!
  - _cool:_ That's what they say. || So they say. || That's the talk. || Reportedly.
  - _teasing:_ The whole university's talking. || Whole university's talking. || Everyone knows. || The gossip's everywhere.
- **Her (by personality)**
  - _gentle:_ We were just talking about you. Only good things! || We were just talking about you. Good things! || Oh, we were talking about you... || Hehe, your ears must be burning.
  - _energetic:_ Hey! We need a tiebreaker: cats or dogs? || Tiebreaker: cats or dogs?! || Quick! Pizza or ramen?! || Settle it: summer or winter?!
  - _cool:_ We were discussing the festival. Opinions? || The festival. Your opinion? || We need a third view. || Discussing plans. Input?
  - _teasing:_ Speak of the devil. || Speak of the devil. || Oh, it's you. Perfect. || Just the boy we wanted.
- **Her (by personality)**
  - _gentle:_ Hehe, it's true. || It's true, hehe. || Hehe... || Mm-hm.
  - _energetic:_ Answer carefully! || Careful! || Choose wisely! || No pressure!
  - _cool:_ Your input is required. || Your input. || Answer. || Opinion required.
  - _teasing:_ Go on, say something interesting. || Say something interesting. || Impress us. || Go on.
- **Her (by personality)**
  - _gentle:_ I knew it! || I knew it! || Yay! || Right!
  - _energetic:_ YES! || YES! || Woo! || Ha!
  - _cool:_ Correct. || Correct. || Right. || Good.
  - _teasing:_ Good taste. || Good taste. || Smart. || Correct choice.
- **Her (by personality)**
  - _gentle:_ I knew it! || I knew it! || Yay! || Right!
  - _energetic:_ YES! || YES! || Woo! || Ha!
  - _cool:_ Correct. || Correct. || Right. || Good.
  - _teasing:_ Good taste. || Good taste. || Smart. || Correct choice.

### Show / CallOver
- **Her (by personality)**
  - _gentle:_ Um... do you have a minute? || Do you have a moment? || Um... a minute? || Could we talk?
  - _energetic:_ Hey hey! Got a sec? || Got a sec?! || Hey! Quick! || Yo! Minute?!
  - _cool:_ A moment of your time. || A moment. || Time? || Briefly.
  - _teasing:_ Hey, you. Come here a sec. || Come here. || You. Here. || Hey, handsome. Moment?
- **Her (by personality)**
  - _gentle:_ We wanted to talk to you... || We wanted to talk... || We'd like to talk to you. || Just a little chat...
  - _energetic:_ C'mon, it'll be quick! || Quick! Promise! || It'll be fast! || Short, really!
  - _cool:_ It won't take long. || Brief. || Short. || Not long.
  - _teasing:_ Don't keep us waiting~ || Don't keep us waiting~ || Hurry~ || Chop chop~

### Show / Outro
- **Her (by personality)**
  - _gentle:_ Thank you for listening... || Thank you for listening. || Thanks... || That was nice.
  - _energetic:_ That was fun! See ya! || Fun! See ya! || Great! Bye! || Awesome! Later!
  - _cool:_ That's all. Thanks. || That's all. Thanks. || Done. || Thanks.
  - _teasing:_ Well, that was something. Later~ || Later~ || That was something. || See you around~
- **Her (by personality)**
  - _gentle:_ S-see you later... || See you... || Bye... || Take care...
  - _energetic:_ Bye bye! || Bye bye! || See ya! || Later!
  - _cool:_ Until next time. || Until next time. || Later. || Bye.
  - _teasing:_ Don't miss us too much. || Don't miss us. || Miss us~ || Later, cutie.

## Club board requests (scenes when a request is completed)
`Systems/Board.cs`

### Board / PlayInTalk / step RequestKind.MeetAt
- **You:** {Pick}
- **Variants:** So the letter was from you. | I found your letter. I came.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you came... I wasn't sure you would. || You really came... I'm so glad. || Y-you're here... I was nervous you wouldn't come. || Ah... you came. Thank you.
  - _energetic:_ You came! Okay, okay, deep breath... || You're here! Okay, okay, don't freak out, me! || Yay, you came! Ahh, now I'm nervous! || You actually came! Deep breaths!
  - _cool:_ ...You actually came. || ...You came. Good. || ...So you came. || ...I wasn't sure you would.
  - _teasing:_ Took you long enough. I was starting to think you chickened out. || You came. Good boy. || Finally. I almost gave up on you. || So you're not a coward. Noted.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ I just... wanted to see you, away from everyone. || I wanted to see you... just the two of us. || I just wanted some time alone with you. || I wanted to see you without anyone around...
  - _energetic:_ I wanted you all to myself for a bit, hehe. || I wanted you all to myself, hehe! || No crowd, just us! I wanted that! || Just you and me! That was the plan!
  - _cool:_ I wanted to talk without anyone watching. || I wanted to talk privately. || No witnesses. That's why. || I wanted to speak where no one listens.
  - _teasing:_ Let's just say I wanted a better look at you. || I wanted you without an audience. || Let's say I wanted you alone~ || Away from everyone. Interesting, isn't it?
- **You:** {Pick}
- **Variants:** Hey. I'm here, like you asked. | Sorry, did I keep you waiting?

### Board / PlayInTalk / step RequestKind.Errand
- **You:** {Pick}
- **Variants:** Done. Took care of it for you. | All set. That favor you asked for.
- **You:** {IsNullOrEmpty}{", "I heard from "}{FirstName}{Pick}
- **Variants:** About what you told me earlier... | I came like I said I would.
- **Variants:** {FirstName} told me you needed a hand. | I heard from {FirstName} that you wanted to talk.
- **Variants:** Hey, you wanted to talk? | I saw your note on the club board.

### Board / NoteScene
- **Narration:** { Wind, a far-off club practice, and "}{name}{",
                              name}{ "}{name}{")}
- **Variants:** The rooftop is empty at this hour. Wind, a far-off club practice, and {name} waiting by the fence. | {name} is waiting by the fence, up on the rooftop. The sky is going orange behind her. | Nobody else is up here. {name} turns around the moment the door creaks.
- **You:** {Pick}
- **Variants:** I got your note. What's up? | So you're the one who left the note.
- **Choice:** Offer to go through it with her. | Tell her she'll do fine without any practice. | Tease her about the mystery signature.
- **You:** {Pick}
- **Variants:** Sure. Let's take a look. | Of course. Show me.
- **Narration:** You stay until the sun is nearly gone. It's a better afternoon than most.
- **You:** {Pick}
- **Variants:** You don't need practice. You'll be fine. | Honestly? You've got this. Don't overthink it.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...You really think so? ...Okay. I'll try to believe it. || ...You think so? ...Then I'll try to believe it too. || ...Really? ...Thank you. I feel a bit braver. || ...If you say so... okay. I'll believe you.
  - _energetic:_ Hehe, you're too nice! But... thank you, I feel lighter. || Aww, stop it! But thanks, really! || Hehe, you're the best! I feel way better! || You're too nice! Okay, I'm cheered up!
  - _cool:_ ...Flattery. Fine. It worked a little. || ...Flattery. It worked, slightly. || ...Thank you. That helps. || ...Hm. I'll accept that.
  - _teasing:_ Oh? Faith in me already? Careful, I'll hold you to it~ || Faith in me? Bold. I'll remember it~ || Oh? You believe in me? Cute. || Careful, I'll hold you to that.
- **You:** {Pick}
- **Variants:** {"} | A single letter. Were you going for spy novel?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ T-that was... I was embarrassed! Please don't make it worse... || D-don't tease me... I was so embarrassed! || P-please, I was mortified... don't make it worse. || Ah... I wanted to hide forever...
  - _energetic:_ Ahaha, busted! I panicked, okay?! || Haha, busted! I freaked out, okay?! || Ugh, caught! I panicked! || Okay, okay, I chickened out! Don't laugh!
  - _cool:_ ...I didn't want my name on it. Drop it. || ...I didn't want my name on it. Drop it. || ...Anonymous was intentional. Leave it. || ...It wasn't meant to be traced. Forget it.
  - _teasing:_ Mysterious is my brand. Now, help me, spy. || A girl needs her mysteries. Now help me, detective. || Mystery suits me. So, helping or not? || Caught me. Now you have to help.

## Short stories (multi-day arcs)
`Systems/ArcStories.cs`

### MakeupExamArc / Scene
- **You:** What's wrong? You look like you saw a ghost.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I... I failed the math test. If I fail the make-up exam too, they'll call my parents... || I failed math... if I fail the make-up too, my parents will find out... || The math test... I failed. I'm so scared of the make-up... || I got a terrible score... and there's a retest soon...
  - _energetic:_ I bombed the math test. Like, totally. There's a make-up exam and I'm DOOMED. || Math test? Total disaster! Make-up exam incoming! Help! || I failed! Spectacularly! And there's a retest! || Zero hope! Math destroyed me!
  - _cool:_ I failed a test. It happens. ...There's a make-up exam. I'd rather not fail twice. || I failed. A make-up exam follows. Failing twice is unacceptable. || Math. Failed. I need to pass the retest. || A failing grade. I intend to fix it.
  - _teasing:_ Don't laugh. I failed the math test. If you laugh, you're dead. || I failed math. Laugh and I'll end you. || Don't you dare laugh. I failed. || Math beat me. Not a word.
- **Choice:** I'll tutor you. | You'll be fine on your own.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ Really? You'd do that for me? Thank you... I'll do my best! || You'd help me? Thank you so much... I'll work hard! || Really? Thank you... I won't let you down. || You're so kind... let's study together!
  - _energetic:_ Seriously?! You're a lifesaver! Let's start today! || You're my hero! Let's start now! || Yes! Study buddy! Today! || Awesome! Lessons start now!
  - _cool:_ ...Fine. I accept. Don't waste my time. || ...Accepted. Don't waste my time. || ...Fine. Teach efficiently. || ...Good. Begin.
  - _teasing:_ Oh? A private tutor? I'll be a very attentive student~ || A private tutor? I'll be attentive~ || Tutoring me? Lucky you. || Lessons with you? I'll pay attention. To you.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Oh... yeah. You're right. I'll try. || Oh... right. I'll try. || Mm... I guess so. || Okay... I'll do my best.
  - _energetic:_ Easy for you to say... || Easy for YOU to say! || Gee, thanks! || Ugh, sure!
  - _cool:_ Hmph. Of course. || Hmph. Obviously. || ...Of course. || ...Naturally.
  - _teasing:_ Wow. Thanks for nothing. || Thanks for nothing. || Wow. So helpful. || Brilliant advice. Really.
- **You:** So? How did the make-up exam go?
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I passed! I really passed! It's all thanks to you... thank you so much! || I passed! Thank you so much! || I really passed... it's all because of you! || I did it! Thank you!
  - _energetic:_ I PASSED! Eighty-two points! You're the best professor ever! || PASSED! You're the best professor! || I passed! Treat time! || Yes! Passed! You rock!
  - _cool:_ I passed. With a decent score, even. ...Your explanations were good. || Passed. Good score. Your explanations helped. || A pass. Thanks to you. || Passed. You're a competent professor.
  - _teasing:_ Passed~ Guess you're good for something after all. I owe you one... a big one. || Passed~ You're useful after all. I owe you. || Passed. I'll repay you. Generously. || Passed! You earned a reward.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I barely passed... but I passed. Thank you for staying with me. || I barely made it... thank you for staying with me. || Just barely... but I passed. Thanks. || Close, but I passed. Thank you.
  - _energetic:_ Phew, just above the line! Close call, but we did it! || Just over the line! We did it! || Phew! Barely! But yes! || Close call! Passed!
  - _cool:_ Barely passed. Still, better than failing. || Barely. Better than failing. || Narrow pass. Acceptable. || Passed. Barely.
  - _teasing:_ Scraped by. Next time, teach better~ Kidding. Thanks. || Scraped by. Teach better next time. Kidding. || Barely. Your fault. Joking. Thanks. || Close one. Thanks, sensei.

### TournamentArc / Scene / step 0
- **You:** I heard there's a tournament coming up.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ Yes... I'm a little nervous. Everyone's counting on me. || Yes... I'm nervous. Everyone's relying on me. || The tournament... I'm scared I'll let them down. || I'm a little anxious about the match...
  - _energetic:_ Yup! And this year we're gonna WIN! ...Probably! Maybe! || Yep! We're gonna WIN! Probably! || This year's ours! Hopefully! || Tournament time! Victory! Maybe!
  - _cool:_ It's this weekend. I intend to win. Obviously. || This weekend. I'll win. || The match is soon. Victory is the plan. || I intend to take first place.
  - _teasing:_ Coming to watch me? I look pretty good in my uniform, you know~ || Coming to watch? I look great in uniform~ || Watching me play? Good choice. || Come see me win. And look cute.
- **Choice:** Let me train with you. | Good luck out there.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You'd train with me? Then... I'll count on you! || You'd train with me? I'll count on you! || Thank you... let's train together! || With you, I'll work harder!
  - _energetic:_ Yes! A training buddy! Don't you dare slack off! || Training buddy! No slacking! || Yes! Let's sweat! || Partner! Keep up!
  - _cool:_ Try to keep up. || Keep up. || Don't fall behind. || Try not to slow me down.
  - _teasing:_ Hope you can handle my pace~ || Can you handle my pace~? || Hope you have stamina. || Let's see if you can keep up.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you. I'll do my best. || Thank you. I'll try my best. || Thanks... I'll do it. || Thank you so much.
  - _energetic:_ Thanks! Come cheer for me, okay? || Thanks! Come cheer! || Cheer for me! || Be there, okay?!
  - _cool:_ Luck has nothing to do with it. || Luck is irrelevant. || I don't need luck. || Skill decides.
  - _teasing:_ Luck? I'll need more than that from you~ || Luck? I want more than that. || Just luck? Stingy. || I expected a kiss. Kidding.

### TournamentArc / Scene / step 2
- **Narration:** {Name} is drilling hard. Sweat runs down her face.
- **You:** Go, {Name}! You've got this!
- **Narration:** You jump in and run the drills with her. She has to push herself to keep up with you.
- **Her:** {",
                        "Watching me sweat, huh? Enjoying the view? || Enjoying watching me sweat? || Like the view? || Watching closely, hm?")}
- **Her (by personality)**
  - _gentle:_ Hah... hah... thank you for coming. It really helps. || Thank you for coming... it helps so much. || You came... hah... thank you. || Your cheering helps me...
  - _energetic:_ Did you see that?! I'm on fire today! || Did you see that?! On fire! || Woo! I'm unstoppable! || Watch me go!
  - _cool:_ ...Your cheering is loud. ...It's not bad. || ...Loud cheering. Not bad. || ...I heard you. Thanks. || ...Your voice carried.
  - _teasing:_ Watching me sweat, huh? Enjoying the view? || Enjoying watching me sweat? || Like the view? || Watching closely, hm?

### TournamentArc / Scene / step 3
- **You:** So? How did the tournament go?
- **Her:** { || Won! What's my prize?")}
- **Her (by personality)**
  - _gentle:_ We won! I scored the winning point... I kept thinking about how you trained with me. || We won! I thought of our training when I scored... || We won... thank you for training with me. || Victory... I couldn't have done it without you.
  - _energetic:_ WE WON! Champions! I'm treating you to crepes, no arguing! || CHAMPIONS! Crepes on me! || We WON! Celebrate! || Victory! You're coming to the party!
  - _cool:_ First place. As expected. ...Your training helped. A little. A lot. || First place. Your training helped. Considerably. || We won. You contributed. || Champions. Thanks to you, partly.
  - _teasing:_ We won~ So, what's my reward from you? || We won. My reward? || Victory. Now spoil me. || Won! What's my prize?
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ We lost in the semifinals... but I'm not sad. I did my best, and you were there. || We lost... but I did my best, and you were there. || Semifinals... it's okay. You came. || We didn't win... but I'm not sad.
  - _energetic:_ Lost by one point! ONE! ...Next year for sure! || Lost by ONE point! Next year! || So close! Next time! || Argh! One point! Next year!
  - _cool:_ We lost. I'll be stronger next time. || We lost. I'll improve. || Defeat. I'll train harder. || Lost. Lesson learned.
  - _teasing:_ We lost. Comfort me properly, okay? || We lost. Comfort me. Properly. || Lost. Make me feel better. || Defeat. You owe me hugs.

### LostCharmArc / Scene
- **You:** Lose something?
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ My charm... my grandma gave it to me. I had it this morning, and now it's gone... || My grandma's charm... it's gone... || I lost my charm... it means so much to me... || The charm my grandmother gave me... I can't find it...
  - _energetic:_ My lucky charm's gone! I can't lose it, it's my LUCKY charm! || My lucky charm's missing! Emergency! || I lost my charm! Help me find it! || No! My charm! Gone!
  - _cool:_ A charm. It's not important. ...It's important. || A charm. Unimportant. ...It's important. || I lost something. A charm. It matters. || My charm is missing. I need it.
  - _teasing:_ My charm. If you find it, I'll make it worth your while~ || Find my charm and I'll reward you~ || Lost my charm. Hero wanted. || My charm's gone. Find it, get a prize.
- **You:** I'll help you look for it.
- **You:** Is this the one you lost?
- **Narration:** You hold out a small, worn charm.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ That's it! You found it! Oh... thank you, thank you so much...! || You found it! Thank you so much! || That's it! Oh, thank you...! || My charm! Thank you, thank you!
  - _energetic:_ NO WAY! You found it! You're amazing! || You found it! Amazing! || YES! You're incredible! || No way! Found it! Hero!
  - _cool:_ ...That's it. You actually found it. I... thank you. || ...You found it. Thank you. || ...That's it. I'm grateful. || ...Found. Thank you. Truly.
  - _teasing:_ Oh! My hero~ Seriously, thanks. || My hero~ Thanks. || Look at you. Thanks, really. || Impressive. Thank you.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ I'll never forget this. || I'll never forget this. || I'll always remember. || Thank you, forever.
  - _energetic:_ I owe you big time! || I owe you huge! || Big debt! || I'll pay you back!
  - _cool:_ I'll repay you. Someday. || I'll repay you. || A debt. Noted. || Someday, I'll repay this.
  - _teasing:_ You're too nice. That's dangerous~ || Too nice. Dangerous~ || You're dangerously sweet. || Careful, I'll fall for you.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ W-what do you want...? M-maybe... a kiss on the cheek? || A reward...? Maybe... a kiss on the cheek? || Um... would a little kiss be okay? || W-what would you like...? A kiss?
  - _energetic:_ Ha! Fine, a reward! Close your eyes... *smooch* || Reward time! Eyes closed! *smooch* || Fine! Close your eyes! Hehe! || Okay! Cheek kiss! Ready?!
  - _cool:_ ...Close your eyes. Don't tell anyone. || ...Close your eyes. Secret. || ...Eyes shut. Tell no one. || ...Don't look. Quickly.
  - _teasing:_ Hmm~ How about I tell you later... in private? || Later. In private~ || Hmm. I'll pay you somewhere quiet. || Patience. Private reward later.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Eh? Um... a thank-you note? || A thank-you card? || Um... a letter of thanks? || My gratitude?
  - _energetic:_ My eternal gratitude! That's a lot! || Eternal gratitude! Huge! || My thanks! Forever! || Gratitude! Lots!
  - _cool:_ My thanks. Don't push it. || Thanks. Enough. || My gratitude. That's all. || Thanks. Don't push.
  - _teasing:_ Nice try~ || Nice try~ || Cute. No. || Ha. No.

### SecretHobbyArc / Scene
- **You:** So... I saw you earlier.
- **Her:** {",
                    "Ah! You saw?! Okay, okay, yes, I love "}{LikeLabel}. Happy? || You saw?! Fine, I love {LikeLabel}! Happy?! || Caught! Yes, {LikeLabel}! || Argh! Okay! {LikeLabel}{")}
- **Her (by personality)**
  - _gentle:_ Y-you saw that?! Please don't tell anyone... it's embarrassing... || You saw?! Please don't tell... || Oh no... you saw that... it's so embarrassing... || P-please keep it secret...
  - _energetic:_ Ah! You saw?! Okay, okay, yes, I love {LikeLabel}. Happy? || You saw?! Fine, I love {LikeLabel}! Happy?! || Caught! Yes, {LikeLabel}! || Argh! Okay! {LikeLabel} is my thing!
  - _cool:_ ...You saw nothing. || ...You saw nothing. || ...Forget it. || ...That didn't happen.
  - _teasing:_ Spying on me? That's a bit creepy. ...Fine, you know my secret now. || Spying? Creepy. Fine, you know. || Peeking? Fine. Secret's out. || Caught me. Now you owe silence.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you... You're really kind. || Thank you... you're so kind. || Thank you for keeping it secret... || You're really kind.
  - _energetic:_ Pinky promise? Okay! Thanks! || Pinky promise! Thanks! || Promise?! Thanks! || Deal! Thanks!
  - _cool:_ ...Good. I trust you, then. || ...Good. I trust you. || ...Trust given. || ...Fine.
  - _teasing:_ Good boy~ || Good boy~ || Smart. || Good choice~
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You think so? ...Nobody ever said that before. || Really? Nobody's said that before... || You think so? That's nice... || No one ever told me that...
  - _energetic:_ Right?! Finally someone gets it! || Finally someone gets it! || Right?! Yes! || See?! You get it!
  - _cool:_ ...You're strange. But thank you. || ...Strange. Thank you. || ...Odd. Thanks. || ...Unexpected. Thank you.
  - _teasing:_ Heh. Flattery works on me, just so you know. || Flattery works. Just so you know. || Keep flattering. || Oh, sweet talk. Continue.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Eh?! Um... w-whatever you want...? || Um... w-whatever you want...? || Eh?! W-what do you want...? || Um... okay...?
  - _energetic:_ Ooh, blackmail? Kinky. I'm in. || Blackmail? Kinky. I'm in. || Ooh, a deal! Okay! || Bribery? Fun!
  - _cool:_ ...You're terrible. Fine. We'll talk later. || ...Terrible. Later. || ...You're awful. We'll discuss. || ...Fine. Later.
  - _teasing:_ Now you're speaking my language~ || Now we're talking~ || My language~ || Ooh. Negotiations.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ That's mean... || That's mean... || So mean... || Don't be cruel...
  - _energetic:_ Not funny! || Not funny! || Rude! || Hey!
  - _cool:_ Tell anyone and you die. || Tell anyone and you die. || Silence. Or else. || Breathe a word and regret it.
  - _teasing:_ Ha. In your dreams. || In your dreams. || Ha. Never. || Nice try.
- **Narration:** You spend a while with {Name}, and she shows you all about {LikeLabel}.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ It's nice to share this with someone. With you. || I'm glad I can share this with you. || It's nice to have someone to share it with. || Sharing this with you makes me happy.
  - _energetic:_ See?! Isn't it the best?! Let's do this again! || Best, right?! Again! || Isn't it great?! || So fun! Let's do it more!
  - _cool:_ You didn't laugh. ...I'm glad it was you who found out. || You didn't laugh. I'm glad it was you. || ...Good that it's you. || ...You understood. Thanks.
  - _teasing:_ Not bad, huh? Now you know one of my secrets. Want to know more? || One secret down. Want more? || Now you know. More secrets? || Like it? There's more.

### RivalryArc / Scene / step 0
- **You:** Hey, what happened back there?
- **Her:** {T(t, "It's "}{OName}... she said things about me behind my back. I thought we were okay... || It's {OName}... she talked behind my back... || {OName} said mean things about me... I thought we were friends. || {OName}{",
                        "Ugh, "}{OName} is SO annoying! She took my spot and acted like it was nothing! || {OName} is SO annoying! || Ugh! {OName} stole my spot! || {OName}{OName} took credit for my work. I won't forgive that. || {OName} took my credit. Unforgivable. || {OName} wronged me. || {OName}{OName} has been flirting with you, hasn't she? Don't play dumb. || {OName}'s been flirting with you. Don't deny it. || {OName} has eyes for you. I noticed. || {OName}{")}
- **Her (by personality)**
  - _gentle:_ It's {OName}... she said things about me behind my back. I thought we were okay... || It's {OName}... she talked behind my back... || {OName} said mean things about me... I thought we were friends. || {OName}... she hurt me.
  - _energetic:_ Ugh, {OName} is SO annoying! She took my spot and acted like it was nothing! || {OName} is SO annoying! || Ugh! {OName} stole my spot! || {OName}! Argh!
  - _cool:_ {OName} took credit for my work. I won't forgive that. || {OName} took my credit. Unforgivable. || {OName} wronged me. || {OName}. I won't forgive her.
  - _teasing:_ {OName} has been flirting with you, hasn't she? Don't play dumb. || {OName}'s been flirting with you. Don't deny it. || {OName} has eyes for you. I noticed. || {OName}. You know why.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You believe me... thank you. || You believe me... thank you. || Thank you for believing me. || You're on my side... thanks.
  - _energetic:_ See?! Even you think so! || See?! You agree! || Right?! || Ha! See!
  - _cool:_ ...At least you understand. || ...You understand. || ...Good. || ...At least you see it.
  - _teasing:_ Good answer~ || Good answer~ || Correct. || Smart boy.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You're taking her side...? || You're on her side...? || You think she's right...? || Oh... you side with her...
  - _energetic:_ Huh?! Whose friend are you?! || Whose friend ARE you?! || Seriously?! Her?! || Traitor!
  - _cool:_ ...I see. || ...I see. || ...Noted. || ...Understood.
  - _teasing:_ Oh, so that's how it is. || Oh, so that's how it is. || Interesting choice. || Wow. Okay.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You'd do that? ...Okay. I'll trust you. || You'd do that? Okay, I trust you. || You'll help? ...Alright. || Thank you... I'll trust you.
  - _energetic:_ Ugh, fine. But she has to say sorry first! || Fine! But she apologizes first! || Okay! She says sorry first! || Ugh, okay! Her first!
  - _cool:_ ...Fine. Try it. || ...Try it. || ...Fine. Attempt it. || ...Go ahead.
  - _teasing:_ Playing peacemaker? Cute. Go ahead. || Peacemaker? Cute. Go. || Diplomat? Adorable. Try. || Playing hero? Go on.

### RivalryArc / Scene / step 1
- **You:** {Name} is pretty upset about earlier.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I know... I didn't mean it like that. I just didn't know how to say sorry. || I didn't mean it... I didn't know how to apologize. || I never meant to hurt her... || I wanted to say sorry, but...
  - _energetic:_ She's upset?! SHE started it! || She's upset?! She started it! || What?! She started it! || Her? Upset? Seriously?!
  - _cool:_ Her reaction was excessive. ...Though I may have been careless. || Excessive reaction. Though I was careless. || ...Perhaps I was thoughtless. || ...Partly my fault. Partly.
  - _teasing:_ Oh, she sent you? How cute. Tell her to come herself. || She sent you? Tell her to come herself. || Messenger boy? Cute. Send her. || She can come herself.
- **Narration:** You calmly explain how {Name} sees it. Little by little, {OName}'s expression softens.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...You're right. I'll apologize to her. Thank you. || You're right. I'll apologize. Thank you. || I'll say sorry to her. Thanks. || Okay... I'll make up with her.
  - _energetic:_ ...Okay, okay. I'll talk to her. Happy? || Okay! I'll talk to her! || Fine! I'll fix it! || Alright, alright!
  - _cool:_ Very well. I'll speak to her. || Very well. I'll speak with her. || ...Fine. I'll talk. || ...I'll handle it.
  - _teasing:_ Fine, fine. You win. I'll make up with her. || You win. I'll make up. || Fine. Peace. || Okay, okay. I'll apologize.
- **Narration:** You try to reason with her, but she isn't ready to listen.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ I'm sorry... I need more time. || Sorry... I need more time. || I can't yet... || Not yet... please.
  - _energetic:_ Nope! Not happening! || Nope! || Not happening! || No way!
  - _cool:_ Drop it. || Drop it. || No. || Enough.
  - _teasing:_ Nice try, peacemaker. || Nice try, peacemaker. || Cute. No. || Not today.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ You stood up for me? ...Thank you. That means a lot. || You stood up for me...? Thank you. || You defended me... that means so much. || Thank you for protecting me.
  - _energetic:_ Wait, you defended me? Huh. You're alright! || You defended me?! You're alright! || Whoa! Thanks! || You had my back! Cool!
  - _cool:_ ...You defended me. I won't forget it. || ...You defended me. Remembered. || ...I won't forget. || ...Thank you for that.
  - _teasing:_ Taking my side? I like you more already~ || My side? I like you more. || Defending me? Good boy. || Smart choice. I like you.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ So you're on her side... I see. || You're on her side... I see. || So it's her... okay. || I understand... her side.
  - _energetic:_ Whatever! Go hang out with HER then! || Go hang with HER then! || Fine! Go! || Whatever! Her!
  - _cool:_ Then we have nothing to discuss. || Nothing to discuss. || Then we're done. || Understood. Goodbye.
  - _teasing:_ Wow. Okay. Noted. || Noted. || Wow. Okay. || Remembered.
- **You:** I talked to {OName}.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ She apologized to me... We're okay now. You really did it. || She apologized... we're okay. You did it. || We made up... thank you so much. || She said sorry... it's all thanks to you.
  - _energetic:_ She said sorry! We're friends again! You're a genius! || Friends again! Genius! || She said sorry! You're amazing! || Made up! Woo!
  - _cool:_ We talked. It's settled. ...Thank you. || Settled. Thank you. || Resolved. Thanks. || We talked. Thank you.
  - _teasing:_ She apologized. Impressive. You're full of surprises. || She apologized. Impressive. || You fixed it. Surprising. || Full of surprises, aren't you?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...I guess I overreacted too. But you didn't have to take her side. || I overreacted too... but you didn't have to side with her. || Maybe I was wrong too... still... || I guess... but it hurt.
  - _energetic:_ Hmph. Fine. Whatever. || Hmph! Fine! || Whatever! || Ugh! Fine!
  - _cool:_ ...Noted. || ...Noted. || ...Hm. || ...I see.
  - _teasing:_ I'll remember this. || I'll remember this. || Noted for later. || Don't forget I remember.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you for being on my side. || Thank you for being on my side. || You were with me... thank you. || Thanks for supporting me.
  - _energetic:_ Thanks for having my back! || Thanks for backing me! || You had my back! || Thanks, partner!
  - _cool:_ You were on my side. Good. || You sided with me. Good. || Good choice. || You supported me. Noted.
  - _teasing:_ You chose me. Smart. || You chose me. Smart. || Wise choice. || Of course you picked me.

### FriendshipArc / Scene
- **You:** You look like you want to say something.
- **Her:** { you know "}{OName}, right? I'd like to be friends with her, but I never know what to say... || You know {OName}? I'd like to be friends with her... || Um... could you help me talk to {OName}? || I want to be friends with {OName}{",
                    "Hey, you're friends with "}{OName}, right? She seems super fun! Introduce me! || You know {OName}?! Introduce me! || {OName} seems fun! Help me meet her! || Hook me up with {OName}{"! As friends!",
                    "You know "}{OName}. ...I wouldn't mind getting to know her. That's all. || {OName}. I'd like to know her. || Introduce me to {OName}. || {OName}{OName} seems interesting. Be a dear and introduce us? || Introduce me to {OName}, dear? || {OName} intrigues me. Make it happen. || Be useful. Introduce {OName}{")}
- **Her (by personality)**
  - _gentle:_ Um... you know {OName}, right? I'd like to be friends with her, but I never know what to say... || You know {OName}? I'd like to be friends with her... || Um... could you help me talk to {OName}? || I want to be friends with {OName}, but I'm shy...
  - _energetic:_ Hey, you're friends with {OName}, right? She seems super fun! Introduce me! || You know {OName}?! Introduce me! || {OName} seems fun! Help me meet her! || Hook me up with {OName}! As friends!
  - _cool:_ You know {OName}. ...I wouldn't mind getting to know her. That's all. || {OName}. I'd like to know her. || Introduce me to {OName}. || {OName} seems worthwhile. Arrange it.
  - _teasing:_ {OName} seems interesting. Be a dear and introduce us? || Introduce me to {OName}, dear? || {OName} intrigues me. Make it happen. || Be useful. Introduce {OName}.
- **You:** Leave it to me.
- **You:** Hey, do you know {Name}? She'd like to have lunch with us sometime.
- **Her:** {OTone, "Oh, "}{Name}? I've always wanted to talk to her! Of course! || {Name}? I've wanted to talk to her! || Oh, {Name}! Of course! || I'd love to meet {Name}{")}
- **Her (by personality)**
  - _gentle:_ Oh, {Name}? I've always wanted to talk to her! Of course! || {Name}? I've wanted to talk to her! || Oh, {Name}! Of course! || I'd love to meet {Name}!
  - _energetic:_ Sure! The more the merrier! || Sure! More friends! || Yes! Let's go! || The more the better!
  - _cool:_ ...If you're there, I suppose it's fine. || ...With you there, fine. || ...Acceptable. || ...If you're present.
  - _teasing:_ Ooh, matchmaking? Fine, I'm curious. || Matchmaking? I'm curious. || Fine. Intriguing. || Ooh, a setup? Okay.

### ConfessionPracticeArc / Scene
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ My friend wants to confess to someone, and... she asked me to practice with her. But I need someone to practice on... would you help? || My friend wants to confess... would you help me practice? || Could you be my practice partner? For a friend... || I need to practice a confession... for a friend. Help?
  - _energetic:_ So! A friend of mine wants to confess and I'm helping her practice! Be my target, okay?! || Friend's confessing! Be my practice target! || Practice dummy wanted! You! || Help me rehearse! For a friend!
  - _cool:_ A friend needs help with a confession. I need a practice partner. You'll do. || A friend's confession. You're my practice partner. || I need a target for practice. You. || Practice partner required. You'll do.
  - _teasing:_ I need to practice a confession. For a friend, obviously. You're the dummy~ || Practice confession. For a friend. Obviously. || You're my practice dummy. For a friend~ || Rehearsal time. Totally for a friend.
- **You:** Uh... sure. Where?
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ The rooftop... after class. Please don't be late. || The rooftop, after class. Don't be late. || Meet me on the roof after class... please. || After class... the rooftop.
  - _energetic:_ Rooftop, after class! It's the classic spot! || Rooftop! After class! Classic! || Roof! After class! || Rooftop time! Be there!
  - _cool:_ Rooftop. After class. || Rooftop. After class. || The roof. Later. || After class. Rooftop.
  - _teasing:_ The rooftop after class. Very cliché. Perfect. || Rooftop after class. Cliché. Perfect. || The roof. Very dramatic. || Rooftop. Don't be late.
- **Narration:** The wind is gentle on the rooftop. {Name} takes a deep breath.
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ I... I've liked you for a long time. Every day, I look for you in the hallway... W-will you go out with me? || I've liked you for so long... will you go out with me? || Every day I look for you... please, date me. || I really like you... will you be my boyfriend?
  - _energetic:_ I like you! A lot! Like, really a lot! Go out with me! || I like you! So much! Go out with me! || I LIKE YOU! Date me! || You! Me! Dating! Please!
  - _cool:_ I like you. I've thought about it carefully, and it's the only answer. Date me. || I like you. Date me. || I've decided. I like you. Go out with me. || My answer is you. Date me.
  - _teasing:_ I like you. Not as a joke. Not as practice... Wait. Pretend I didn't say that last part. || I like you. For real. ...Pretend I didn't say that. || Not practice. ...Wait, forget that. || I like you. Oops. That wasn't practice.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ R-really? Ahaha... yeah. Practice. Of course. || R-right... practice. Of course. || Ahaha... yes. Just practice. || Oh... practice. Right.
  - _energetic:_ Haha... yeah! Totally! Practice! || Haha! Totally practice! || Yeah! Practice! Obviously! || Right! Just pretend!
  - _cool:_ ...Of course. Practice. || ...Practice. Yes. || ...Of course. || ...Naturally.
  - _teasing:_ ...Idiot. || ...Idiot. || ...Dummy. || ...Clueless.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Maybe not. Don't make me say it twice. || ...Maybe not. || ...It might not have been. || ...Don't make me say it.
  - _energetic:_ D-don't ask that! ...Maybe it wasn't! || Don't ask! Maybe not! || Stop asking! ...Maybe! || Ugh! Maybe it wasn't!
  - _cool:_ ...You figure it out. || ...Figure it out. || ...You decide. || ...Think.
  - _teasing:_ What do you think~? || What do you think~? || Guess~ || You tell me~
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ Huh...? You... you mean it? I'm so happy... Then next time, please say it properly. For real. || You mean it...? I'm so happy... say it properly next time. || Really...? Then confess for real, okay? || I'm so happy... please say it again, properly.
  - _energetic:_ WAIT, REALLY?! Okay, okay, I'm not crying! Say it again for real sometime! || REALLY?! Not crying! Say it for real! || Wait! Yes! Say it properly! || OMG! Again! For real!
  - _cool:_ ...I see. Then I expect you to confess properly. Soon. || ...Then confess properly. Soon. || ...I expect a real confession. || ...Good. Properly, next time.
  - _teasing:_ ...Took you long enough. Now do it right, with a real confession, okay? || Took you long enough. Do it right. || Finally. A real confession, please. || About time. Properly, now.

### LateStudyArc / Scene
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ Exams are coming... so I thought, maybe we could study together? Just the two of us. || Exams are close... study together? Just us? || Could we study together? Just the two of us... || I'd like to study with you...
  - _energetic:_ Study date! Snacks, notes, and you! Best plan ever! || Study date! Snacks included! || Study party! Us two! || Let's study! Together! Fun!
  - _cool:_ We study together this week. That's decided. || We study together. Decided. || This week, we study. Together. || Study session. You and me.
  - _teasing:_ Let's study together. I'll even let you look at my notes... and other things~ || Study with me. I'll show my notes. And more. || Let's study. Among other things~ || Study date? I'll make it interesting.
- **You:** It's a date.

### BadDayArc / Scene
- **You:** Hey... are you okay?
- **Her:** {")}
- **Her (by personality)**
  - _gentle:_ N-no... everything went wrong today. I forgot my assignments, I tripped in the hallway, and... || Everything went wrong today... || I forgot everything and tripped... it's awful... || Today's been terrible...
  - _energetic:_ NO! Today is the WORST! Everything is going wrong! || WORST day! Everything's broken! || Nothing's going right! || Today hates me!
  - _cool:_ ...Not now. || ...Not now. || ...Leave it. || ...Bad day.
  - _teasing:_ Do I LOOK okay? || Do I look okay? || Does it look like it? || Obviously not.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Thank you for listening. I feel a bit lighter. || Thank you for listening. || I feel better... thank you. || Thanks for hearing me out.
  - _energetic:_ ...Yeah. Thanks. Venting helps. || Thanks! Venting helps! || Yeah. Thanks! || That helped!
  - _cool:_ ...You're a decent listener. || ...Decent listener. || ...Thanks. || ...Helpful.
  - _teasing:_ ...Fine. Thanks for not running away. || Thanks for not running. || You stayed. Thanks. || You didn't flee. Impressive.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...Pfft. That was so bad. ...Thanks. || Pfft... so bad. Thanks. || Hehe... that was silly. Thank you. || That was awful... I laughed.
  - _energetic:_ HAHA! That's so dumb! ...I needed that. || HAHA! Dumb! Needed that! || So dumb! Thanks! || Ahaha! Perfect!
  - _cool:_ ...That's not funny. ...Okay, it's a little funny. || ...Not funny. ...A little. || ...Idiot. ...Funny. || ...Hm. Amusing.
  - _teasing:_ Seriously? ...Ugh, I laughed. You win. || Ugh, I laughed. You win. || Fine. Funny. || Seriously? ...Okay, I laughed.
- **Narration:** You sit next to her in silence. After a while, she leans on your shoulder.
- **Her:** {T}
- **Her (by personality)**
  - _gentle:_ ...You always know what I need. || You always know what I need. || You're always here for me... || This is exactly what I needed.
  - _energetic:_ ...This is nice. Thanks. || This is nice. Thanks. || Thanks! This helps! || Mm! Nice!
  - _cool:_ ...Stay a bit longer. || ...Stay longer. || ...Don't go yet. || ...A little longer.
  - _teasing:_ ...Don't tell anyone I did this. || Don't tell anyone. || Secret, okay? || Nobody hears about this.

## Everyday happenings
`Systems/Happenings.cs`

### Happenings / Play / step "coin"
- **Narration:** Something glints on the hallway floor... a 500 yen coin.
- **Choice:** Pocket it. | Turn it in at the faculty room.
- **Narration:** Finders keepers.
- **Narration:** The professor thanks you for your honesty.

### Happenings / Play / step "pop_quiz"
- **Narration:** A professor stops you in the hallway: "You! What's the capital of Australia? Quick!"
- **Narration:** {"}

### Happenings / Play / step "carry_boxes"
- **Narration:** A professor is struggling with a tower of boxes full of handouts.
- **Narration:** You take half the boxes. The professor thanks you.
- **Narration:** You suddenly find the ceiling very interesting.
- **Narration:** You lift the whole tower in one go. A few students clap.

### Happenings / Play / step "love_letter"
- **Narration:** There's a pink envelope in your locker. Inside: "Please come to the rooftop after class. I have something to tell you."
- **Narration:** It's signed with a little heart. No name.

### Happenings / Play / step "locker_note"
- **Narration:** There's a folded note in your locker: "Could you come to the rooftop after class? I need help with something and I'd rather not ask in front of everyone."
- **Narration:** It's signed with a single letter: {initial}.

### Happenings / Play / step "dropped_notebooks"
- **Narration:** Someone bumps into {name} and her notebooks scatter across the floor.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah... my notes... || Oh no... my notes... || Ah... everything's falling... || Wah... my papers...
  - _energetic:_ Nooo! My stuff! || Noooo! Everything's everywhere! || Gah! My stuff! || Argh! Not again!
  - _cool:_ ...Great. || ...Wonderful. || ...Of course. || ...Typical.
  - _teasing:_ Ugh. Perfect timing. || Ugh. Great. || Lovely. Just lovely. || Well, that's annoying.
- **Choice:** Help her pick them up. | Keep walking.
- **Narration:** You gather everything and hand it back.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you so much! || Thank you! You're so kind. || Ah, thank you so much! || Thank you... that really helped.
  - _energetic:_ You're a lifesaver! || You saved me! Thanks! || Thanks a million! || Woo! You're the best!
  - _cool:_ ...Thanks. || ...Thank you. || ...Appreciated. || ...Thanks. Really.
  - _teasing:_ Oh? A gentleman. Thanks~ || How gallant. Thanks~ || A knight! Thanks~ || Oh, chivalry. Thanks.

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
  - _gentle:_ Th-thank you for looking away... || Th-thank you... for not looking... || You looked away... thank you... || I-I'm so glad you didn't look...
  - _energetic:_ Phew! You didn't see anything, right?! RIGHT?! || You didn't see anything, right?! Promise?! || Phew! You saw nothing! Got it?! || Nothing happened! Nothing!
  - _cool:_ ...You looked away. Good. || ...Good. You looked away. || ...Thank you for your discretion. || ...Decent of you.
  - _teasing:_ Aww, you didn't even peek? How boring~ || Not even a peek? Disappointing~ || What a gentleman. How dull~ || You didn't look? I'm almost offended.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you saw, didn't you...? || Y-you saw it... didn't you...? || Ah... you saw... || Please tell me you didn't see...
  - _energetic:_ You totally saw! || You saw! You totally saw! || Hey! You looked! || Don't lie! You saw!
  - _cool:_ ...Forget it. Now. || ...Erase that memory. || ...You saw nothing. Understood? || ...Forget it.
  - _teasing:_ Enjoy the show? || Like the view? || Enjoyed that? || A free show, huh?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Eh?! ...Y-you think so...? || Eh?! ...R-really...? || W-wait, you think they're cute...? || Ah... um... thank you...?
  - _energetic:_ Hehe, really? I picked them this morning! || Hehe, right? They're my favorite! || Oh! You like them? Yay! || Really?! Thanks! I picked them myself!
  - _cool:_ ...Idiot. ...Thanks. || ...Idiot. ...Thank you. || ...Don't comment. ...Thanks. || ...Shut up. ...Thanks.
  - _teasing:_ Only cute? I'll show you something better later~ || Cute? Wait till you see the rest~ || Just cute? You've seen nothing yet. || Only cute? Hmph. Next time, then.
- **Her (by personality)**
  - _gentle:_ P-pervert!! || P-pervert! || Ah! You're terrible! || N-no! Pervert!
  - _energetic:_ WHAT?! Pervert! || PERVERT! || Ew! Pervert! || Whaaat?! Creep!
  - _cool:_ Die. || Disgusting. || Vanish. || Never speak again.
  - _teasing:_ Wow. Gross. || Gross. || Ew. Seriously? || Wow. Yikes.

### Happenings / Play / step "rumor"
- **Narration:** {\""}

### Happenings / Play / step "inspection"
- **Narration:** Student union inspection! They're checking everyone's bags at the gate.
- **Narration:** Your bag is spotless. The council president nods approvingly.

### Happenings / Play
- **Narration:** The council member pulls out your... items. Everyone stares.
- **Narration:** You explain with a perfectly straight face...
- **Narration:** You sprint past the gate before anyone can stop you!

### Happenings / Play / step "stray_cat"
- **Narration:** A small stray cat follows you, meowing hopefully.
- **Narration:** It purrs and rubs against your leg, then trots off.
- **Narration:** It eats happily. A few girls watching from the window go "awww".

### Happenings / Play / step "drenched"
- **Narration:** {name} runs in from outside, soaked to the skin, hair stuck to her cheeks.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ehehe... I couldn't help it. I like the rain... || Ehehe... I like rain. I couldn't resist. || The rain felt nice... sorry. || Hehe... I just wanted to feel it.
  - _energetic:_ Woo! That was AMAZING! Did you see how hard it's coming down?! || Wooo! The rain's crazy today! Did you see?! || That was so fun! Rain dance! || Yeah! Rain is awesome!
  - _cool:_ ...I walked. It's just water. || ...It's only water. || ...I didn't mind walking. || ...Rain is fine.
  - _teasing:_ Don't look at me like that. Rain is fun~ || Rain is fun. You should try it~ || Don't judge. Rain's fun. || What? Wet suits me.
- **Her (by personality)**
  - _gentle:_ Uuu... it came out of nowhere... I'm all wet... || Uuu... I'm soaked... || Ah... the rain came so suddenly... || I'm all wet... how embarrassing...
  - _energetic:_ Gah! Totally soaked! My shoes are squishing! || Soaked! Totally soaked! || Ugh, my socks are wet! || I look like a drowned cat!
  - _cool:_ ...Don't say anything. || ...Not a word. || ...Don't comment. || ...Don't look.
  - _teasing:_ Like what you see? ...Kidding. I'm freezing. || Enjoying the view? Joke. I'm cold. || Wet look. Trendy, huh? Brr. || Stop staring. ...I'm freezing.
- **Choice:** Give her your handkerchief. | Give her your jacket. | Laugh it off with her.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Thank you... it smells like you. || Thank you... it smells nice. || Thank you... it's warm. || Ah... thank you so much.
  - _energetic:_ You're a lifesaver! || You're a hero! Thanks! || Thanks! So warm! || Yay! Thank you!
  - _cool:_ ...Thanks. I'll wash it. || ...Thank you. I'll return it washed. || ...Appreciated. || ...Thanks. Clean, when I return it.
  - _teasing:_ A gentleman. I'll keep it~ || How sweet. I'm keeping this. || Mine now. Thanks~ || A gift? I accept~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ B-but then you'll be cold...! ...It's warm. || B-but you'll be cold... ...It's so warm. || Are you sure...? ...Thank you. || But... ...it's really warm. Thank you.
  - _energetic:_ Whoa, really?! So warm! || Really?! Whoa, warm! || Your jacket?! Thanks! || Ahh, toasty! You're the best!
  - _cool:_ ...You'll get sick. Idiot. ...Thank you. || ...You'll catch a cold. ...Thank you. || ...Idiot. ...Thanks. || ...Fine. Thank you.
  - _teasing:_ Your jacket? Bold move. I'll take it~ || Your jacket? Bold. Mine now~ || Ooh, smooth move. I'll take it. || Giving me your jacket? Adorable.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Hehe... you get it. || Hehe... you understand. || Ehehe... right? || Mm. You get it.
  - _energetic:_ Right?! Rain is the best! || Right?! Rain rules! || See?! Best weather! || Yes! Rain buddy!
  - _cool:_ ...Heh. || ...Heh. || ...Hm. Yes. || ...Right.
  - _teasing:_ See? Fun~ || Told you. Fun~ || See? Rain's fun. || Admit it. It's fun.
- **Her (by personality)**
  - _gentle:_ I-it's not funny... || D-don't laugh... || I-it's not funny... || Please don't laugh at me...
  - _energetic:_ Hey! Don't laugh! ...Okay it's a little funny. || Hey! Stop laughing! ...Okay it's funny. || Don't laugh! ...Pfft. || Rude! ...Haha, fine.
  - _cool:_ ...Hmph. || ...Hmph. || ...Not amusing. || ...Stop.
  - _teasing:_ Laugh now. I'll drip on you later. || Laugh it up. I'll drip on you. || Oh, you think it's funny? Hug time. || Keep laughing. I'm getting you wet.

### Happenings / Play / step "rain"
- **Narration:** It starts pouring right as campus ends. {name} is stuck at the entrance without an umbrella.
- **Choice:** Share your umbrella with her. | Lend it to her and run home in the rain. | Wait for the rain to stop.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Together...? O-okay. Your shoulder is getting wet... || Together...? Okay... you're getting wet, though. || Sharing...? Mm. Your shoulder's wet... || Th-thank you... let's share.
  - _energetic:_ Umbrella buddies! Squeeze in! || Umbrella squad! Get in here! || Share! Squeeze closer! || Umbrella buddies! Yay!
  - _cool:_ ...Fine. Don't get any closer than necessary. || ...Fine. Not too close. || ...Acceptable. Keep your distance. || ...Share, then. Carefully.
  - _teasing:_ Sharing an umbrella? How romantic~ || Sharing an umbrella? So cliché. I love it. || Under one umbrella? How scandalous~ || Romantic, aren't we?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ But you'll get soaked...! Thank you... || But you'll get wet... thank you... || You'll be soaked... thank you so much. || Are you sure...? Thank you...
  - _energetic:_ Whoa, really?! You're the best! || Seriously?! You rock! || For me?! Best day! || Wow! Thanks!
  - _cool:_ ...You're an idiot. Thank you. || ...Idiot. Thank you. || ...You'll get sick. Thanks. || ...Foolish. Thank you.
  - _teasing:_ My knight in soaked armor~ || My soggy hero~ || Drenched chivalry. Cute. || A wet knight. I'm charmed.

## School events (long scenes over painted backgrounds: exams, festivals, trip, seasonal days)
`Systems/CalendarEvents.cs`

### Choco
- **Narration:** February 14th. Chocolate Day.
- **Narration:** The whole university smells of cocoa. Every boy is pretending not to care, and every boy is failing.
- **Narration:** Your locker is right in front of you. It might be empty. It might not be.
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
  - _gentle:_ Um... I made too many for my club. You can have the extra one... if you want. || I had one extra... would you like it? || Um, there was a spare one. It's yours, if you want. || This one was left over... please take it.
  - _energetic:_ Leftover chocolate! Catch! ...Nice catch! || Spare chocolate! Here! || Extra one! Yours! Catch! || Leftover! Don't complain!
  - _cool:_ I had one left over. Don't make it weird. || A spare. Don't read into it. || Extra. Take it. || I had one more. Here.
  - _teasing:_ Pity chocolate~ Don't cry, it's still chocolate. || Pity chocolate. Be grateful. || Leftovers for you~ Still sweet. || Consolation chocolate. Cheer up.
- **You:** Thanks. Really.
- **Narration:** Next year, for sure.
- **Narration:** At lunch, it starts.
- **Narration:** {N} walks up to your desk, hands hidden behind her back.
- **Girl:** {")
                        : T}
- **Her (by personality)**
  - _gentle:_ I made these myself... for you. Only for you. || I made these... just for you. || Homemade... for you alone. || I worked hard on these... they're yours.
  - _energetic:_ Homemade! Eat them all, okay?! || Homemade! Eat every one! || I made them! All for you! || Kitchen chaos! For you!
  - _cool:_ Handmade. Don't make a fuss. || Handmade. No fuss. || I made them. Eat. || Homemade. Don't comment.
  - _teasing:_ Handmade, with love~ And maybe a little something extra. || Handmade with love. And mischief. || Made by me. Feel special. || Homemade~ You're lucky.
- **Her (by personality)**
  - _gentle:_ Um, these are... thank-you chocolates! Just thanks! || These are thank-you chocolates! Only thanks! || Just a thank-you! Nothing else! || Um, gratitude chocolate! That's all!
  - _energetic:_ Here, friendship chocolate! Don't get the wrong idea! || Friend chocolate! No ideas! || Buddy chocolate! Got it?! || Friendship! Only friendship!
  - _cool:_ Obligation chocolate. Obviously. || Obligation. Obviously. || Courtesy chocolate. || Duty chocolate.
  - _teasing:_ Don't get your hopes up. ...Unless you want to. || Don't hope too much. Unless you want to. || Just obligation. Probably. || Courtesy. Or is it?
- **Narration:** And {value} more boxes appear on your desk while you're not looking. The boys at the back are glaring.
- **Narration:** {N} is still standing there, watching you hold her box.
- **Choice:** Eat one right now | Ask if it's homemade | Save it for later
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ R-right now? ...How is it? Is it okay? || N-now? ...Is it good? || Um... how does it taste? || Do you like it...?
  - _energetic:_ Well?! Well?! Is it good?! || Well?! Good?! || Tell me! Tasty?! || Verdict?!
  - _cool:_ ...You have chocolate on your lip. Idiot. || ...Chocolate on your lip. Idiot. || ...You're messy. || ...Wipe your mouth.
  - _teasing:_ Mmh, you eat it so happily~ I should feed you more often. || You eat so happily. I'll feed you more. || Cute. Want another? || Look at you. Adorable.
- **Narration:** It's sweet, a little bitter at the end. Perfect.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-yes... I stayed up late. Don't tell anyone. || Yes... I stayed up late. Secret. || I made them last night... shh. || All by myself... it took hours.
  - _energetic:_ Totally homemade! The kitchen is a disaster now! || Homemade! Kitchen's wrecked! || Totally homemade! Messy! || Made them myself! Chaos!
  - _cool:_ Store bought. ...Fine. Homemade. Happy? || Bought. ...Fine. Homemade. || ...Homemade. Satisfied? || I made them. Don't gloat.
  - _teasing:_ Homemade, with a secret ingredient. Guess what it is~ || Secret ingredient included. Guess. || Homemade. With something special. || Made with a secret. Guess what~
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Oh... okay. Eat it when you're alone, then. || Okay... eat it alone, then. || Alright... later, then. || Save it? Okay...
  - _energetic:_ Saving it? Don't let it melt! || Don't let it melt! || Saving it? Eat soon! || Hide it well!
  - _cool:_ Hmph. Suit yourself. || Suit yourself. || Hmph. Fine. || As you like.
  - _teasing:_ Saving the best for last? Smart boy~ || Saving the best for last? Smart. || Later? Good boy. || Savoring me? Clever.
- **Narration:** After class. Snow falls softly on the way home, and the streetlights come on one by one.
- **Narration:** Footsteps run up behind you. {N}, out of breath, cheeks red from the cold.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Wait! The one this morning was... not the real one. This is the real one. || Wait! That one was... this is the real one. || The real one is this... please take it. || This morning's wasn't true. This one is.
  - _energetic:_ Hah... hah... I forgot to give you the SPECIAL one! || Forgot the SPECIAL one! Here! || Wait! The real one! || Hah... this one's the real deal!
  - _cool:_ ...That one on campus was for show. This one is honmei. Don't make me say it twice. || ...This is honmei. Once only. || ...The real one. Don't make me repeat. || ...That was decoy. This is real.
  - _teasing:_ The one on campus was obligation. This one's... not. Got it? || That was obligation. This isn't. Got it? || The real one. Treasure it. || Campus is was fake. This is real.
- **Choice:** Promise to return it on White Day | Take her cold hand | Just say thank you
- **You:** I'll give you something back on White Day. Something good.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...I'll be waiting. Every day until then. || I'll wait... every day. || I'll be looking forward to it. || I'll count the days...
  - _energetic:_ Three times as good, that's the rule! I'm counting! || Triple return! Counting! || Three times better! Rule! || I'll be waiting! Big!
  - _cool:_ I'll hold you to that. || I'll hold you to it. || Expected. || Remembered.
  - _teasing:_ Something good? Then I'll expect something very good~ || Something very good, then~ || Impress me. || Make it worth it.
- **Narration:** Her fingers are freezing. She doesn't pull away. For a while, neither of you says anything.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Your hand is warm. || ...Your hand is warm. || ...Warm hands. || ...This is nice.
  - _energetic:_ W-what's this all of a sudden?! ...Don't let go. || Wh-what?! ...Don't let go! || Whoa! ...Keep holding! || Huh?! ...Okay! Stay!
  - _cool:_ ...Only because it's cold. || ...Only because it's cold. || ...Cold weather. That's all. || ...For warmth.
  - _teasing:_ Took you long enough~ || Took you long enough~ || Finally~ || About time.
- **You:** Thank you. I mean it.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Mm. That's all I wanted to hear. || That's all I wanted. || Mm. That's enough. || I'm happy.
  - _energetic:_ Hehe! You're welcome! || You're welcome! || Hehe! Anytime! || No problem!
  - _cool:_ ...You're welcome. || ...Welcome. || ...Mm. || ...Fine.
  - _teasing:_ Just 'thank you'? Well, it's a start~ || Just thanks? A start. || Only thanks? Hm. || Thanks? More later.
- **Narration:** You walk home alone, counting the boxes in your bag. Not a bad day at all.
- **Narration:** Chocolate Day is over. Your bag is heavier, and somehow so is your heart.

### Sports
- **Narration:** The Sports Festival! Flags snap in the wind and the whole university is out on the field.
- **Narration:** Tents, tables, a mountain of traffic cones and a professor with a megaphone who is already hoarse.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Here's your class headband... Tie it tight, okay? Good luck today. || Your headband... tie it tight. Good luck. || Here, for you. Do your best today. || Our class headband... I'll be cheering.
  - _energetic:_ HEADBANDS! Everybody gets one! Our class is winning this! || HEADBANDS! We're winning! || Put it on! Victory! || Class colors! Let's go!
  - _cool:_ Headband. Put it on properly. We're not losing to the other classes. || Wear it properly. We won't lose. || Headband. Tight. || Properly. We win today.
  - _teasing:_ Headband~ Let me tie it for you. ...There. Now you look like you mean it. || Let me tie it. ...There. Serious face. || I'll tie it. Look heroic. || Headband on. Now you look the part.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Which event are you entering? I'll cheer for you from the tent! || Which event? I'll cheer from the tent! || What are you entering? I'll watch! || I'll cheer for you, okay?
  - _energetic:_ Let's WIN this! Which event are you in? || Let's WIN! Your event?! || Which race?! Go go! || What event?! Crush it!
  - _cool:_ Try not to embarrass our class. || Don't embarrass us. || Represent us well. || Try not to lose.
  - _teasing:_ I'll be watching you~ Don't trip. || Watching you. Don't trip. || I'll be watching~ || Eyes on you. Perform.
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
  - _gentle:_ W-we're really close like this... Left foot first, okay? || So close... left foot first. || We're touching... um, left foot. || Ready? Left first...
  - _energetic:_ Okay! One-two, one-two! Don't let go of me! || One-two! Hold tight! || Go go! Don't let go! || Left, right! Teamwork!
  - _cool:_ Keep up. And don't hold me that tight. || Keep pace. Loosen your grip. || Not so tight. Keep up. || Pace. Now.
  - _teasing:_ Arm around my waist~ For balance, of course. || Arm on my waist. For balance~ || Hold me closer. For the race. || Closer. Strategy.
- **Narration:** The starting pistol fires. Her shoulder bumps yours with every step.
- **Narration:** {"}
- **Narration:** Afternoon. The girls' 200 meters is next.
- **Narration:** {N} is in lane four, bouncing on her toes, glancing toward your class's tent.
- **Choice:** Shout her name as loud as you can | Wait at the finish line with water | Film it for her
- **Narration:** {"}
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I heard you... It was so embarrassing. ...Thank you. || I heard you... so embarrassing... thank you. || Your voice... I ran harder. Thank you. || You cheered for me... thank you.
  - _energetic:_ THAT WAS YOU?! I ran faster because of that! || THAT WAS YOU?! I ran faster! || You yelled! It worked! || Your cheering! Boost!
  - _cool:_ You're loud. ...I didn't hate it. || Loud. ...Not bad. || ...Noisy. Thanks. || ...You're loud. I liked it.
  - _teasing:_ Shouting my name in front of everyone? Bold~ || Yelling my name? Bold. || Cheering so loud? Cute. || Everyone heard. Brave.
- **Narration:** She crosses the line and nearly collapses. You're right there with a cold bottle.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You waited for me...? Thank you, I'm so thirsty. || You waited? Thank you. So thirsty... || Water... thank you so much. || You're so thoughtful...
  - _energetic:_ Water! You're a lifesaver! || Water! Lifesaver! || Yes! Hydration! || Thank you! Drink!
  - _cool:_ ...Thoughtful. Thanks. || ...Thoughtful. || ...Thanks. || ...Considerate.
  - _teasing:_ Waiting at the finish line for me? How devoted~ || Waiting for me? Devoted. || At the finish line? Sweet. || Loyal boy.
- **Narration:** You record the whole race on your phone. Later she watches it three times, pretending she isn't.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't show anyone how my face looks when I run! || Don't show my running face! || Please delete it... I look silly. || Don't let anyone see!
  - _energetic:_ Again! Play it again! Look how fast I was! || Again! Look how fast! || Replay! Replay! || Look at me go!
  - _cool:_ Delete the part where I almost tripped. || Delete the trip. || Cut the stumble. || Edit that out.
  - _teasing:_ Keeping a video of me? What else do you keep~? || Keeping videos of me? What else? || Saving me? Interesting. || A collection? Creepy. Cute.
- **Narration:** You cheer from the tent until your throat hurts.
- **Narration:** Closing ceremony in the gym. The scoreboard is covered with a cloth while the dean talks... and talks.
- **Narration:** {"}
- **Girl:** {")
                                       : T}
- **Her (by personality)**
  - _gentle:_ We won... we really won! I'm so glad I was on your team. || We won! I'm glad we were a team. || We really won... thank you! || Victory... with you.
  - _energetic:_ WE WON WE WON WE WON! || WE WON! || Champions! || Yes yes YES!
  - _cool:_ Of course we won. ...Okay, I'm happy. A little. || Of course. ...I'm happy. Slightly. || Won. Pleased. || Victory. Good.
  - _teasing:_ Winners deserve a reward, don't they~? || Winners get rewards~ || Prize time? || Reward me.
- **Her (by personality)**
  - _gentle:_ We did our best. That's what matters. || We tried our best. || It's okay. We did well. || Our best was enough.
  - _energetic:_ Argh! Next year we crush them! || Next year, revenge! || Argh! Next time! || We'll crush them next year!
  - _cool:_ Second place. Acceptable. Barely. || Second. Barely acceptable. || Acceptable. || Close. Not enough.
  - _teasing:_ Losing is fine. You looked cool anyway~ || You looked cool anyway. || Losing's fine. You were cute. || Still cool, loser~
- **Choice:** Walk her home | Stay and help clean up
- **Narration:** The two of you walk home in the orange evening, sore and sunburned and very happy.
- **Narration:** You fold tents and carry cones until dark. The professors notice. So does she: she stays to help.
- **Narration:** The Sports Festival is over. Tomorrow, everything will hurt.

### Exam
- **Narration:** Exam day. Desks pulled apart, the schedule written on the board, and a silence you could cut with a ruler.
- **Narration:** Ten minutes before the start. Everyone is flipping through notes as if the pages might change.
- **Narration:** {N} leans over from the next desk, notebook clutched to her chest.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Um... could you quiz me? Just the formulas... I'm so nervous. || Could you quiz me? I'm nervous. || Please test me on formulas... || Help me review?
  - _energetic:_ Help! I forgot EVERYTHING! Quick, ask me something! || Forgot everything! Quiz me! || Ask me stuff! Quick! || Brain empty! Help!
  - _cool:_ Ask me three questions. I want to be sure. || Three questions. || Test me. || Quiz me. Briefly.
  - _teasing:_ Quiz me~ If I get them all right, you owe me something. || Quiz me. Perfect score earns a prize. || Test me. Wrong answers, you pay. || Quiz time. Stakes high.
- **Choice:** Quiz her on the formulas [INT] | Calm her down | Review on your own
- **Narration:** {"}
- **You:** You studied. You know this. Breathe.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Okay. Breathe. Thank you. I feel better. || Breathe... thank you. || Okay... I'm calmer. Thanks. || I feel better now.
  - _energetic:_ Breathe! Right! Hooo... okay! I'm ready! || Breathe! Okay! Ready! || Hooo! Okay! || Calm! Sort of! Go!
  - _cool:_ ...Fine. I'm calm. I was always calm. || ...Calm. Always was. || ...Fine. || ...Composed.
  - _teasing:_ You're cute when you try to comfort me~ || Cute, comforting me. || You're sweet when you worry. || Comforting me? Adorable.
- **Narration:** You bury your face in your notes. One last look at everything.
- **Narration:** The professor hands out the papers face down. "Begin."
- **Narration:** Deep breath. Question one. You know this. Question two. You know this too.
- **Narration:** You scribble every formula you crammed into the margin before they leak out of your head.
- **Narration:** The professor steps out to answer the phone. The answer key is right there, on the desk...
- **Narration:** {"}
- **Narration:** Question seven. Nothing you studied looks anything like question seven.
- **Choice:** Skip it and come back later | Fight it to the end [INT]
- **Narration:** You leave it and finish the rest. At the end you come back with five minutes left and write something reasonable.
- **Narration:** {"}
- **Narration:** "Pencils down." The room exhales all at once.
- **Girl:** { Thinking of me? || Hard test, or hard thoughts? || Were you thinking of me during it?")}
- **Her (by personality)**
  - _gentle:_ How did it go? I... think I did okay. Thanks to you, maybe. || How was it? I think I did okay. || I think it went well... thanks to you. || Did you do okay? I hope so.
  - _energetic:_ DONE! Wanna compare answers? No? Okay, food then! || DONE! Food time! || Finished! Celebrate! || Over! Let's eat!
  - _cool:_ Don't ask me about question seven. || Not question seven. || Don't mention seven. || Skip question seven.
  - _teasing:_ You looked so serious~ Was it hard, or were you thinking about me? || So serious. Thinking of me? || Hard test, or hard thoughts? || Were you thinking of me during it?
- **Narration:** The sun sets over the empty desks. Results tomorrow.

### Culture
- **Narration:** Two weeks before the Culture Festival. The department meeting has been going in circles for an hour.
- **Narration:** Three ideas are left on the board. The course rep looks at you: you get the deciding vote.
- **Choice:** Maid cafe [SEN] | Haunted house [STR] | Science exhibit [INT]
- **Narration:** {"}
- **Narration:** You stay late to work on it with {N}.
- **Girl:** {")
                        : T}
- **Her (by personality)**
  - _gentle:_ The aprons are done... Do you think this one suits me? || The aprons are done... does it suit me? || Do I look okay in this? || I sewed this... how is it?
  - _energetic:_ Look, look! I'm practicing my 'Welcome home, master'! || 'Welcome home, master!' Practicing! || Look! Café mode! || Maid practice! How's this?!
  - _cool:_ I'm in charge of the menu. Nobody touches the menu. || I control the menu. Don't touch. || Menu is mine. || Hands off the menu.
  - _teasing:_ Want a private preview of the uniform~? || Private preview? || Want to see the outfit first? || Sneak peek?
- **Her (by personality)**
  - _gentle:_ I'm painting the blood... it's just red paint, right? Right? || It's only paint, right? Right? || Fake blood... I hope... || It's just red paint...
  - _energetic:_ BOO! Hahaha, your face! || BOO! Haha! || Gotcha! Ahaha! || RAWR! Your face!
  - _cool:_ The lighting is wrong. Move that lamp. No, the other one. || Lighting's wrong. Move it. || That lamp. Left. || Fix the light.
  - _teasing:_ I'll play the ghost who drags boys into the dark~ || I'll drag boys into the dark. || I'm the scary ghost. Beware. || Fear me, mortal.
- **Her (by personality)**
  - _gentle:_ The volcano works! I'm so happy I could cry. || It works! I could cry! || The volcano erupted... I'm so happy! || It worked! Yay...
  - _energetic:_ Can we make it BIGGER? Like, way bigger? || BIGGER! Make it bigger! || More! Huge! || Explosion size up!
  - _cool:_ The numbers check out. It'll work. || Numbers check. It works. || Calculated. Success. || As predicted.
  - _teasing:_ Explaining chemistry to strangers? I'll do it in a lab coat~ || Lab coat chemistry. Sexy science. || Chemistry with me? Explosive. || I'll explain in a lab coat. Watch.
- **Narration:** Festival day! Lanterns over every gate, music from the gym, and the smell of takoyaki everywhere.
- **Narration:** Visitors pour in. Your class's booth opens its doors, and all morning you work without a break.
- **Narration:** {"}
- **Narration:** {"}
- **Narration:** {"}
- **Narration:** In the afternoon you finally get some free time.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You want to walk with me? ...I'd love to. || Walk with me? I'd love to. || You want to come with me? Yes... || Together? Okay!
  - _energetic:_ Yes! Let's hit every food stand! || Every stand! Let's go! || Food tour! Yes! || Festival run!
  - _cool:_ ...Fine. You can escort me. || ...Escort me. || ...Fine. Come. || ...You may accompany me.
  - _teasing:_ A festival date? Bold~ I accept. || Festival date? Accepted. || A date? Okay~ || Bold. Yes.
- **Narration:** Where to first?
- **Choice:** Share some takoyaki | Brave the haunted house together | Visit the fortune teller
- **Narration:** The takoyaki stand is run by the cooking club. The octopus pieces are suspiciously big.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Hot, hot... Ah, you have sauce right here. Hold still. || Hot! ...Sauce on your cheek. Hold still. || Careful, hot... let me wipe that. || Mm... you have sauce here.
  - _energetic:_ Mmh! Okay, the next stand is on me! || Next stand's on me! || Yum! More! || Delicious! Next!
  - _cool:_ ...It's good. Don't watch me eat. || ...Good. Don't watch me eat. || ...Tasty. Eyes elsewhere. || ...Fine. Stop staring.
  - _teasing:_ Say 'ah'~ Come on, nobody's looking. || Say 'ah'~ || Open wide~ || Feed time~
- **You:** This is the best takoyaki I've ever had.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Maybe because you're eating it with me...? || Because we're together...? || It tastes better with you... || Maybe it's the company.
  - _energetic:_ Right?! Let's get another box! || Another box! || Let's get more! || Seconds!
  - _cool:_ It's average. ...The company isn't bad. || Average. Company's fine. || Ordinary. You aren't. || Normal food. Good company.
  - _teasing:_ Of course. I made it taste better just by being here~ || I made it better. Obviously. || My presence. Flavor boost. || I'm the seasoning.
- **Narration:** The third-years built the haunted house. Paper lanterns, cobwebs, and a corridor that goes on far too long.
- **Narration:** It's pitch dark. Something wet touches your neck. A scream. She grabs your arm and doesn't let go.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I-I wasn't scared! ...Don't let go yet, okay? || I wasn't scared! ...Hold on. || N-not scared...! Don't let go... || I'm fine...! Stay close...
  - _energetic:_ AAAH! Ha... haha! Again! Let's go again! || AAAH! Again! || Haha! One more! || Again! Again!
  - _cool:_ I'm not holding on to you. You're holding on to me. || You're holding me. Not reverse. || I'm not clinging. You are. || ...You grabbed me.
  - _teasing:_ You screamed louder than me~ Cute. || You screamed louder. Cute. || Scared? Adorable. || Who screamed? You.
- **Narration:** You come out the other side still arm in arm. Neither of you mentions it.
- **Narration:** A tent of purple curtains, a crystal ball, and a third-year in a veil who takes this very seriously.
- **Narration:** She looks at the two of you, then at your joined shadows, and smiles.
- **Narration:** {\""}
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ She said... we're compatible. W-what do you think? || Compatible... what do you think? || She said we match... || Um... do you believe it?
  - _energetic:_ Did you hear that?! Ha, she says that to everyone. ...Right? || She says that to everyone! Right?! || Ha! Fortune! ...Right?! || Totally random! ...Maybe!
  - _cool:_ Fortune telling is nonsense. ...What did she say about us, exactly? || Nonsense. ...What exactly did she say? || Rubbish. ...Repeat it. || Fake. ...Details?
  - _teasing:_ Well? Going to argue with fate? || Arguing with fate? || Destiny says yes. You? || Fate agrees with me.
- **Narration:** Evening. The stalls glow red and orange. In the campus grounds, the bonfire for the closing dance is lit.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ The folk dance is starting... um... || The dance is starting... um... || Would you... dance? || The folk dance... um...
  - _energetic:_ Bonfire! Dance! Come on, come on! || Dance! Come on! || Bonfire dance! Go! || Let's dance!
  - _cool:_ Are you going to dance? ...Just asking. || Dancing? ...Asking. || ...Will you dance? || ...Are you going?
  - _teasing:_ Everyone's pairing up. Don't keep a girl waiting~ || Don't keep me waiting. || Ask me already. || Everyone's paired. Hurry.
- **Choice:** Ask her to dance | Watch the fire together
- **Narration:** You hold out your hand. She takes it. You step on her foot twice. She doesn't care.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I'll remember this festival forever. || I'll never forget tonight. || This festival... forever. || I'll remember this always.
  - _energetic:_ Again! One more song! || One more song! || Again! || More dancing!
  - _cool:_ ...You're a terrible dancer. Don't stop. || Terrible dancer. Continue. || Clumsy. Don't stop. || ...Keep going.
  - _teasing:_ Your hand's sweaty~ Nervous? || Sweaty hand. Nervous? || Nervous? Cute. || Shaky. Adorable.
- **Narration:** You sit on the steps and watch the sparks rise into the dark. Her shoulder leans against yours.
- **Narration:** You spend the afternoon backstage: fixing lights, carrying boxes, saving a play from a broken curtain.
- **Narration:** By evening, the professors know your name. In a good way, for once.
- **Narration:** The last song ends. The Culture Festival is over, and nobody wants to go home.

### Tanabata
- **Narration:** July 7th. Tanabata.
- **Narration:** Once a year, the weaver princess Orihime and the cowherd Hikoboshi cross the river of stars to meet.
- **Narration:** On earth, people write their wishes on strips of paper and tie them to bamboo, hoping the stars will read them.
- **Narration:** The old street near campus is full of bamboo branches heavy with colored paper. Lanterns sway overhead.
- **Narration:** Someone tugs your sleeve. {N}, in a summer yukata.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah... you came too. Does it... look okay on me? || You came... does it look okay? || Ah... how do I look? || Is it nice on me?
  - _energetic:_ Ta-da! Yukata! Say it's cute! || Ta-da! Cute, right?! || Yukata! Say it's cute! || Look! Pretty?!
  - _cool:_ My mother made me wear it. Don't stare. || Mother insisted. Don't stare. || ...Don't stare. || ...Obligatory. Stop looking.
  - _teasing:_ Like what you see~? It took an hour to tie. || Like it? An hour to tie. || Admiring? Good. || Stare all you want.
- **Narration:** She has a paper strip in her hand and hides it the moment you look.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah! Don't look at mine... It's embarrassing. || Don't look at mine... || Please don't read it... || It's embarrassing...
  - _energetic:_ I wished for something HUGE. Not telling! || HUGE wish! Secret! || Big wish! Not telling! || Secret wish!
  - _cool:_ Mine is private. Turn around. || Private. Turn around. || Don't look. || Mine. Private.
  - _teasing:_ Want to know what I wished for? It involves you~ || My wish involves you~ || Guess my wish. Hint: you. || About you. Curious?
- **Narration:** She ties it up high, out of your reach.
- **Narration:** Your turn. You hold the pen over the paper for a long moment...
- **Choice:** 'I want to get smarter.' | 'I want to get stronger.' | 'I want to be more charming.' | 'I want to be loved.'
- **Narration:** You hang your wish on the highest branch you can reach. The paper turns slowly in the warm wind.
- **Narration:** Night falls. The stalls along the shrine road light up one after another.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Will you walk with me a little...? || Walk with me...? || Would you stroll with me? || Let's walk a little.
  - _energetic:_ Stalls! Let's try everything! || Stalls! All of them! || Food! Games! Go! || Let's try everything!
  - _cool:_ I'm hungry. You're paying. || Hungry. You pay. || Your treat. || Food. On you.
  - _teasing:_ A summer night, a girl in a yukata... You're lucky, you know~ || Summer night, me in yukata. Lucky boy. || You're lucky tonight. || Me in yukata. Appreciate it.
- **Choice:** Goldfish scooping | Shaved ice | Walk under the lanterns
- **Narration:** Your paper net tears on the first try. Hers tears on the second. The old man gives you one goldfish out of pity.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ He's so small... Can I keep him? I'll name him after you. || He's tiny... I'll name him after you. || So small... can I keep him? || A little goldfish... I'll love him.
  - _energetic:_ WE CAUGHT ONE! Kind of! He counts! || CAUGHT ONE! Kinda! || We got one! Sorta! || Success! A fish!
  - _cool:_ ...I'll take care of him. Since you can't. || I'll care for him. || ...I'll raise him. || ...He's mine now.
  - _teasing:_ Our first pet together~ || Our first pet~ || Our baby fish~ || A pet together. Cute.
- **Narration:** Strawberry for her, melon for you. Brain freeze for both.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Want to try mine...? It's strawberry. || Try mine? Strawberry. || Want some? It's strawberry. || Here... taste?
  - _energetic:_ Ow ow ow, my head! Worth it! || Brain freeze! Worth it! || Ow! Cold! Yum! || Headache! Delicious!
  - _cool:_ Your tongue is green. Very dignified. || Green tongue. Dignified. || ...Your tongue is green. || ...Colorful tongue.
  - _teasing:_ Indirect kiss~ Just kidding. Unless...? || Indirect kiss~ Kidding. Unless? || Share? Indirect kiss. || Taste mine~
- **Narration:** You walk slowly under the paper lanterns. The crowd pushes you closer together. Neither of you steps away.
- **Narration:** On the way back you pass her bamboo branch. Her paper strip has come loose and flutters down at your feet.
- **Narration:** You can't help reading it.
- **Narration:** {T}
- **Her (by personality)**
  - _gentle:_ "I hope he notices how I feel." || "I hope he notices me." || "Please let him see how I feel." || "Let him understand my heart."
  - _energetic:_ "Let me spend every summer with him!" || "Every summer with him!" || "More summers together!" || "Him! Always!"
  - _cool:_ "I want to be honest with him. Just once." || "Let me be honest with him." || "Honesty, once." || "Courage to tell him."
  - _teasing:_ "Make him fall for me. Hard." || "Make him fall for me." || "Him. Mine. Hard." || "Make him crazy about me."
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Y-you read it...! ...Well? Is it going to come true? || You read it! ...Will it come true? || Y-you saw...! Well? || You read it... so?
  - _energetic:_ AAAH! Forget you saw that! ...Or don't. Whatever! || AAAH! Forget it! Or don't! || Unsee it! ...Or whatever! || Nooo! ...Okay, fine!
  - _cool:_ ...Now you know. Don't make it weird. || ...Now you know. || ...Don't make it weird. || ...Well. Known.
  - _teasing:_ Oops~ Well, now you know. What are you going to do about it? || Oops~ Now what? || Read it? Your move. || Now you know. Act.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't read it! Please! || Don't read it! || Please, no! || Not mine!
  - _energetic:_ Mine! Give it! No peeking! || Mine! No peeking! || Give it! || Nope!
  - _cool:_ Hand it over. Now. || Hand it over. || Give. || Now.
  - _teasing:_ Curious? Earn it first~ || Earn it. || Curious? Pay. || Not free.
- **Narration:** She ties it back up, higher this time.
- **Narration:** Above the street, the Milky Way is clear tonight. Somewhere up there, two lovers are meeting.

### Halloween
- **Narration:** Halloween. The lecture hall is covered in paper bats, jack-o'-lanterns and orange streamers.
- **Narration:** There's a costume contest after class, and the course rep says nobody leaves without a costume.
- **Choice:** Vampire, cape and all [SEN] | A bedsheet ghost | No costume. You're too cool.
- **Narration:** Fake fangs, a red-lined cape, hair slicked back. In the mirror, you look... surprisingly good?
- **Narration:** {"}
- **Narration:** A bedsheet with two holes. You bump into three desks on the way to your seat.
- **Narration:** At the contest the whole class laughs when your ghost walks into the door. Special prize: most dedicated ghost.
- **Narration:** The course rep sighs and sticks cat ears on your head. They stay there all day.
- **Narration:** At the contest you're not even entered. The cat ears get three votes anyway.
- **Narration:** At lunch, the trick-or-treaters arrive.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ T-trick or treat...? || Trick or treat...? || T-trick or... treat? || Um... trick or treat?
  - _energetic:_ TRICK OR TREAT! || TRICK OR TREAT! || Candy! Now! || Treat me!
  - _cool:_ Trick or treat. Choose wisely. || Trick or treat. Choose. || Choose. || Treat. Or else.
  - _teasing:_ Trick or treat~ I'm hoping for the trick. || Hoping for the trick~ || Trick or treat? Pick trick. || Treat or trick~
- **Narration:** You hand out sweets from your bag. Everyone's happy.
- **Narration:** You don't have enough sweets... Time for some tricks.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Then... I'll draw a cat face on your cheek. Hold still... || A cat face on your cheek... hold still. || Then a little doodle... stay still. || Trick! Cat whiskers!
  - _energetic:_ No candy? TICKLE ATTACK! || TICKLE ATTACK! || No candy?! Tickles! || Trick time!
  - _cool:_ No sweets. Then you carry my bag all day. || You carry my bag. All day. || Penalty: bag duty. || Then you're my porter.
  - _teasing:_ No treats? Then close your eyes~ This trick is a secret. || Close your eyes. Secret trick. || Eyes shut~ || A secret trick. Ready?
- **Narration:** After dark: the test of courage. Pairs walk the old hallway with one flashlight and no adults.
- **Narration:** You draw lots. Your partner is {N}.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I-it's really dark... You'll stay close, right? || So dark... stay close? || It's scary... don't leave me. || Please stay next to me...
  - _energetic:_ This is gonna be AWESOME! Let's go! || AWESOME! Go! || Haunted! Fun! || Let's go!
  - _cool:_ It's a hallway. Hallways aren't scary. || A hallway. Not scary. || Just a corridor. || Nothing to fear.
  - _teasing:_ Alone in the dark with you~ Should I be scared of the ghosts, or of you? || Scared of ghosts or you? || Dark and you. Dangerous. || Which is scarier? You.
- **Narration:** Halfway down, the flashlight flickers. Somewhere behind you, a locker door slowly creaks open.
- **Choice:** Take her hand | Jump out and scare her | Walk ahead bravely
- **Narration:** You find her hand in the dark. She squeezes it so hard it hurts. You don't let go until the end.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Thank you. I'm okay now. Just... don't let go. || Thank you... don't let go. || I'm okay now... keep holding. || Better... stay close.
  - _energetic:_ Ha! I wasn't scared! ...Keep holding it though. || Not scared! ...Keep holding. || Ha! ...Don't let go. || I'm brave! ...Hold on.
  - _cool:_ ...This is for your sake. You looked scared. || ...For your sake. || ...You looked scared. || ...Purely for you.
  - _teasing:_ Bold~ Should we walk slower? || Bold. Walk slower? || Slower, maybe? || Holding me? Bold.
- **Narration:** "BOO!" She screams, then punches your arm, then laughs so hard she has to sit down.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ That was so mean! ...My heart's still racing. || Mean! ...My heart's racing. || That was awful! ...Wow. || So scary...! You're mean!
  - _energetic:_ AHAHA! Okay, okay, you got me! Revenge later! || You got me! Revenge later! || AHAHA! Payback soon! || Okay! You win! For now!
  - _cool:_ ...I will remember this. || ...Remembered. || ...You'll regret this. || ...Noted.
  - _teasing:_ Scaring a girl in the dark? You're worse than the ghosts~ || Worse than ghosts. || Scaring me? Bad boy. || You're the real monster.
- **Narration:** You walk in front, flashlight high. Behind you, she holds the hem of your jacket the whole way.
- **Narration:** Back in the lecture hall, the jack-o'-lanterns are lit for the party.
- **Narration:** Pumpkin cake, orange juice and the bad horror movie somebody brought. Happy Halloween.

### Trip
- **Narration:** Field trip day! The whole class boards the bullet train to {value}.
- **Narration:** Rice fields fly past the window. Somebody already opened the snacks. The professor has given up.
- **Narration:** There's a free seat next to you.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You saved me a seat...? Thank you. || You saved me a seat? Thanks. || A seat for me... thank you. || You kept it for me...
  - _energetic:_ Window seat neighbor! Want some chips? || Window buddy! Chips?! || Seatmates! Snacks! || Neighbors! Yay!
  - _cool:_ Fine. Don't hog the armrest. || Don't hog the armrest. || Armrest is mine. || Fine. Share fairly.
  - _teasing:_ Two hours sitting next to me. Lucky you~ || Two hours with me. Lucky. || Sitting with me? Fortunate. || Enjoy my company.
- **Narration:** You play cards for an hour. She cheats. You let her.
- **Narration:** Kyoto. Temples, old streets and far too many souvenir shops. The class splits up for free time.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Just the two of us? I was hoping you'd ask... || Just us? I hoped you'd ask. || The two of us? Yes... || I wanted that too...
  - _energetic:_ Yay! Let's go everywhere! || Everywhere! Go! || Yay! Explore! || Let's go!
  - _cool:_ I suppose you can come along. || You may come. || Fine. Join. || Acceptable.
  - _teasing:_ Sneaking away with me? I like it~ || Sneaking off? I like it. || Escaping? Fun. || Just us? Naughty.
- **Choice:** Rent kimonos and walk the old streets | Write a wish on an ema together
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ How do I look...? You're staring. || How do I look? You're staring. || Is this okay...? || Do I look nice?
  - _energetic:_ Look, look! I'm a real princess now! || Princess mode! || Look! Royal! || I'm a princess!
  - _cool:_ Stop taking pictures. ...Fine, one more. || One more photo. Last. || Stop. ...One more. || Enough. ...Okay, one.
  - _teasing:_ Help me with the obi? It's so hard to tie alone~ || Help with the obi? || Tie this for me? || Obi trouble. Help?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Don't read mine! ...It's about you. A little. || Don't read it! ...It's about you. || It's... a little about you. || Please don't look...
  - _energetic:_ I wished for ten more trips like this one! || Ten more trips! || More trips like this! || Again and again!
  - _cool:_ Wishes are childish. ...I wrote one anyway. || Childish. ...I wrote one. || Silly. ...I did too. || ...Mine's private.
  - _teasing:_ Mine says 'that he notices me'. Oops, you read it~ || 'That he notices me.' Oops. || Read it? Oops. || My wish? You saw.
- **Narration:** The inn is old and beautiful: tatami rooms, sliding doors, a garden with a stone lantern. Steam rises behind the bamboo fence.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so pretty here... I'm glad I came with you. || It's beautiful... glad I came with you. || So pretty... thank you for coming. || What a lovely place...
  - _energetic:_ Hot springs! Ping-pong! Milk in glass bottles! Let's go! || Hot springs! Ping-pong! Milk! || Onsen time! || Bath! Games! Snacks!
  - _cool:_ Quiet. Good. I like this place. || Quiet. Good. || Peaceful. I like it. || Calm. Pleasant.
  - _teasing:_ Mixed bathing is around the back, I heard~ Just saying. || Mixed bath's out back. Just saying. || Heard there's mixed bathing~ || The baths... interesting.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Ah, my yukata is coming loose... D-don't look! || My yukata's loose! Don't look! || Ah! Don't look! || Eek! My collar!
  - _energetic:_ SMASH! Ha! Another one, loser buys milk! || SMASH! Loser buys milk! || Point! Again! || Ha! Winner!
  - _cool:_ You're going down. ...Stop looking at my collar. || You're going down. Eyes up. || Focus. Not my collar. || Watch the ball.
  - _teasing:_ If I win, you do whatever I say tonight~ || If I win, you obey tonight. || Win and you're mine tonight. || Stakes: tonight. Ready?
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so quiet... I can hear the water. I like being here with you. || So quiet... I like being here with you. || Peaceful... with you. || Listen... the water.
  - _energetic:_ Look! A frog! A HUGE frog! || A frog! Huge! || Look! Fish! || Wow! Frog!
  - _cool:_ ...This is nice. Don't ruin it by talking. || ...Nice. Don't talk. || ...Silence. Good. || ...Peaceful.
  - _teasing:_ A garden, a quiet afternoon, just us~ What are you thinking about? || Just us. What are you thinking? || Quiet afternoon. Thoughts? || A garden, you, me. Hmm.
- **Narration:** While the others nap, you climb the rocks behind the bamboo fence, holding your breath...
- **Narration:** Night at the inn. Lights out at ten, the professor said. It's eleven-thirty and nobody is asleep.
- **Narration:** A soft knock on the sliding door. It's {N}, in her yukata, whispering.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ I couldn't sleep... Can I stay for a bit? || I couldn't sleep... can I stay? || Sleepless... may I sit here? || I wanted to see you...
  - _energetic:_ Psst! The girls are having a pillow fight, I escaped! Hide me! || Pillow fight! I escaped! Hide me! || Shh! Hide me! || Escaped! Cover me!
  - _cool:_ My roommates snore. I'm staying here for ten minutes. || Roommates snore. Ten minutes. || Too noisy there. Ten minutes. || I'll stay briefly.
  - _teasing:_ Sneaking into the boys' room~ If we get caught, it's your fault. || If we're caught, your fault. || Sneaking in. Exciting. || Boys' room. Risky.
- **Narration:** Footsteps in the corridor. The professor's flashlight sweeps under the door.
- **Choice:** Hide her in the closet | Talk quietly until it passes | Send her back before you're caught
- **Narration:** You both squeeze into the futon closet. It's very dark and very small. You can hear her heartbeat. Or yours.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...Your heart is so loud. || ...Your heart's so loud. || ...I can hear your heart. || ...So close...
  - _energetic:_ Mmmph! Don't make me laugh! || Mmph! Don't make me laugh! || Shh! Giggles! || Hmph! Quiet!
  - _cool:_ ...Don't move. Don't breathe. Don't think. || ...Don't move. || ...Silence. || ...Stay still.
  - _teasing:_ Cozy~ We should get caught more often. || Cozy. Get caught more often. || We should hide more. || Snug~
- **Narration:** The footsteps pass. You talk in whispers until two in the morning: families, dreams, the stupid things you did as kids.
- **Narration:** You slide the door open just in time. She tiptoes back. The professor only sees you, waving awkwardly.
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
  - _gentle:_ You came... Merry Christmas. I've been looking forward to this all month. || You came... Merry Christmas. || Merry Christmas... I waited all month. || I'm so glad you came. Merry Christmas.
  - _energetic:_ MERRY CHRISTMAS! Look, it's snowing! It's actually snowing! || MERRY CHRISTMAS! Snow! || It's snowing! Merry Christmas! || Christmas! Yay!
  - _cool:_ You're late. ...By two minutes. I'll allow it. Merry Christmas. || Late. Two minutes. Merry Christmas. || Merry Christmas. You're late. Slightly. || ...Merry Christmas.
  - _teasing:_ Merry Christmas~ Did you dress up for me? You did, didn't you. || Dressed up for me? You did. || Merry Christmas~ Looking sharp. || Did you dress for me? Cute.
- **Narration:** You walk through the quiet streets. She slips her hand into your coat pocket, next to yours.
- **Narration:** The park is empty. Fresh snow on the bench, one lamp glowing gold. It feels like the whole world is holding its breath.
- **Narration:** You take out the present you've been hiding all evening.
- **Choice:** A warm scarf | A small necklace | A handmade card
- **Narration:** You wrap it around her neck yourself. It covers her up to the nose.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's so warm... It smells like you. || So warm... smells like you. || Warm... thank you. || Cozy... I love it.
  - _energetic:_ SO FLUFFY! I'm never taking it off! || FLUFFY! Never removing it! || So soft! || Warm! Yay!
  - _cool:_ ...Practical. I like practical. ...I like it a lot. || Practical. I like it. A lot. || ...Useful. Thank you. || ...I'll wear it daily.
  - _teasing:_ Wrapping me up like a present? Who's unwrapping me later~? || Wrapping me up? Unwrapping later? || Gift-wrapped me? Bold. || Who unwraps me?
- **Narration:** Her eyes go wide when she opens the little box. Her hands are shaking as you fasten it.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ It's beautiful... I'll treasure it forever. || Beautiful... I'll treasure it. || It's lovely... thank you. || So pretty... forever.
  - _energetic:_ Shiny! It's so pretty! Was it expensive?! Don't tell me! || Shiny! Don't tell me the price! || Pretty! Wow! || Sparkly!
  - _cool:_ ...You didn't have to. ...I'm wearing it every day. || ...Every day. I'll wear it. || ...Unnecessary. Thank you. || ...Beautiful.
  - _teasing:_ Jewelry? Someone's serious about me~ || Jewelry? Serious, are we? || Someone's committed. || Ooh, jewelry. Serious.
- **Narration:** A card you drew yourself, a little crooked, with everything you never managed to say out loud.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ ...I'm crying. I'm sorry. It's the best present ever. || I'm crying... best gift ever. || Sorry... tears. It's perfect. || I love it so much...
  - _energetic:_ You DREW this?! It's so bad! I love it! || You DREW this?! So bad! Love it! || Handmade?! Amazing! || I love it! Ugly! Perfect!
  - _cool:_ ...I'll read it again. Later. Alone. || ...Later. Alone. I'll reread. || ...I'll keep it. || ...Private reading later.
  - _teasing:_ Such sweet words~ I'm keeping this to tease you for years. || Sweet words. I'll tease you for years. || I'm keeping this. Blackmail. || Adorable. Saving forever.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ This is from me... Open it. || From me... open it. || This is for you... please. || My gift... open it.
  - _energetic:_ My turn! Open it, open it! || My turn! Open! || Open it! Now! || Your present! Go!
  - _cool:_ Here. It's nothing special. ...Open it. || Nothing special. Open it. || Here. Open. || ...Yours.
  - _teasing:_ My present is in this box. The other one comes later~ || The other gift comes later. || Box one. Box two later. || Part one. Part two tonight.
- **Narration:** Gloves. Hand-knitted, a little uneven. One finger is longer than the others. They're perfect.
- **Narration:** Later. Your room, the lights off, snow falling past the window.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ Merry Christmas... I'm so happy to spend it with you. || Merry Christmas... I'm so happy. || So happy to spend it with you. || Merry Christmas, my love.
  - _energetic:_ Merry Christmas! Best. Christmas. Ever. || Best Christmas ever! || Merry Christmas! Woo! || Greatest Christmas!
  - _cool:_ Merry Christmas. ...Stay with me tonight. || Merry Christmas. Stay tonight. || ...Stay with me. || ...Merry Christmas. Don't go.
  - _teasing:_ Merry Christmas~ Your last present is... me. Kidding. Maybe. || Last present: me. Maybe. || Final gift? Me. Kidding. || Unwrap me later. Joking. Maybe.
- **Narration:** Outside, the snow keeps falling. Inside, nobody is cold anymore.
- **Narration:** Christmas Eve. Lights on every street, couples everywhere.
- **Narration:** You spend it with the Koikatsu Club: a cake too big for the club room, paper hats, and a gift exchange.
- **Narration:** You get socks. Somebody else gets your socks. It's that kind of party.
- **Narration:** On the way home the snow starts. The street is quiet and the houses glow warm behind their curtains.
- **Narration:** Your phone buzzes. A message from {N}.
- **Girl:** {")}
- **Her (by personality)**
  - _gentle:_ Merry Christmas... I just wanted to say it to you first. || Merry Christmas... I wanted to tell you first. || I wanted you to be first... Merry Christmas. || Merry Christmas... to you, first.
  - _energetic:_ MERRY CHRISTMAS!!! Save me some cake!! || MERRY CHRISTMAS! Cake! || Christmas! Save cake! || Yay! Merry Christmas!
  - _cool:_ Merry Christmas. That's all. ...Don't read into it. || Merry Christmas. That's all. || ...Merry Christmas. Don't read into it. || ...Season's greetings.
  - _teasing:_ Merry Christmas~ Next year, keep the evening free for me. || Next year, keep the evening free. || Reserve next year for me. || Next Christmas is mine.
- **Narration:** Another buzz. "...I'm at the park near the station. If you're close."
- **Choice:** Run to the park | Reply and go home
- **Narration:** She's on the bench under the lamp, snow on her hair, holding two cans of hot cocoa. One for you.
- **Girl:** {T}
- **Her (by personality)**
  - _gentle:_ You actually came... Merry Christmas. For real this time. || You came... Merry Christmas. Truly. || You're here... Merry Christmas. || I knew you'd come...
  - _energetic:_ YOU CAME! I bet myself you would! || YOU CAME! I knew it! || You're here! I bet on it! || Yay! You came!
  - _cool:_ ...You're out of breath. Idiot. Sit. || ...Out of breath. Sit. || ...Idiot. Rest. || ...You ran. Sit.
  - _teasing:_ Running to me on Christmas Eve? That's practically a confession~ || Running to me? A confession. || Rushing to me? Telling. || You ran? So obvious.
- **Narration:** You sit together until the cocoa goes cold, watching the snow.
- **You:** Merry Christmas to you too. See you on campus.
- **Narration:** Next year will be different.

