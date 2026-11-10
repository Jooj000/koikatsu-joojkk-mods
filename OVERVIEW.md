# Campus Life — overview

*Português: [APRESENTACAO.pt-BR.md](APRESENTACAO.pt-BR.md)*

**A college life mod for Koikatsu's story mode.** The girls at your college get their own moods, memories, friendships and grudges; a director plans each one's day and decides every answer she gives you; talking to them becomes a real conversation; and everything you do with them becomes part of a story the whole campus talks about.

By **Joojkk** · version **3.6.0** · BepInEx 5 / KKAPI · Koikatu (HF Patch) · menu **F7** · interface and dialogue in English.

It comes with three sibling mods: **Campus Saves** (one save slot per campus, Ctrl+F6), **School Optimizer** (a full campus without stutter) and **H Male Wardrobe** (the protagonist's outfit during H, F10).

---

## In numbers

| | |
|---|---|
| Code | ~47k lines of C# (.NET 3.5), Harmony prefix/postfix only, everything wrapped in try/catch |
| Dialogue | 37 `.dlg` files where each line is written together with the options that answer it, plus the scenes and events written in code |
| Voices | 6 tones (gentle, energetic, cool, teasing, shy, proper), each line in four or more versions per tone, never the same one twice in a row |
| Achievements | 54 achievements that climb through stages, plus one climb for each story heroine and each professor |

---

## 1. Conversation

- **A new conversation screen** with five icon categories: **Talk**, **Actions**, **Together**, **Romance** and **Spicy**. The options come from what she knows, what she likes, where you are and what happened today, not from a fixed list, and each option answers the line she just said.
- **Procedural conversation**: subjects scored by context (her day, the two of you, you, gossip, the campus, small talk), facts she knows and who she heard them from, opinions and distorted versions. She remembers what you told her and brings it up days later. Every line that depends on a like or on the rain has the side of a girl who does not care for it.
- **Her own voice**: every line is written in her tone, shaped by her personality, mood and your relationship; the game plays a clip of her own recorded voice that fits the line.
- **Touches decided by the director**: hand, head, cheek, chest and butt, plus her lips and a look. Every touch gets one of the four answers (below), weighed by what you are to her, her pace, her mood, trust, who is watching and how many times you already touched there. The same touch in the same period gets the same answer, and insisting after a no makes it worse.
- **Requests with their own talk**: each kind of request has your line, four options written for it and her answer to each.
- **The rumor that she likes you** has its own scene: she answers as she feels, and you can confess from it.
- **Talk with two girls**: both stand side by side; talk to one, to the other or to both. They react to each other, tease, get jealous, help out or walk off, and what they say to each other is about things that are true right now. Options that only exist with two: asking one about the other, an embarrassing story, a note, an invitation for both, a compliment in front of the other, a hidden touch.
- **Conversation badges**: her mood and likes as round icons under the H bar (only after you find them out).
- **A talk log** for the whole conversation, and nothing acts while a line is on screen: categories, options and touches wait for the box to close.

## 2. The director (the girls' AI)

- **The director decides what the girls do**: each period it updates their needs (company, food, rest), intentions, pairs and rumors, and picks the situations that play in front of you or happen off screen.
- **Four answers** to everything that matters (touches, the kiss, invitations, confession, dates, H): **yes**, **wary yes**, **wary no** and **no**. It reads as many factors as it can: what you are to her, her pace, the bars, the days together, your reputation, trust, mood, past H, a recent kiss, another girlfriend, a perfect day, how many times she already said no.
  - A **wary yes** leaves guilt: in the next conversation she asks you to commit or pulls away, and you settle it.
  - A **wary no** is almost a yes: she wanted to, and she says what held her back (personality, mood, guilt, your girlfriend, too soon, already asked today, a grudge, feeling neglected, reputation, or just not here and now).
- **Pace by personality**: reserved, normal and open girls take different numbers of days (table below). H doesn't require dating: it depends on context. Three options (Ease: reserved / normal / open) make each pace easier or harder.
- **Breakups and cheating**: a girlfriend who finds out about another girl reacts by her mood, anger, trust, how many times it happened, who the other girl is, how far it went and whether she saw it.
- **Card traits** (easy, perverted, submissive, serious...): when a card has all of them ticked, or none, the mod draws the ones that fit her personality instead of making every girl easy.
- **Before the game's H** she says out loud what her card refuses (protection, kissing, back door, touches, toys), in her own voice. The game's H itself doesn't change.

### How many days to each milestone

Well-played days (3 conversations a day, good answers), with the default Ease, the average of two girls of each pace, no special mood:

| Milestone | Reserved | Normal | Open |
|---|---|---|---|
| Flirting and compliments work | 2 | 2 | 2 |
| Holding hands | 3 | 2 | 2 |
| Friend | 4 | 3 | 3 |
| Girlfriend | 5 | 4 | 3 |
| Chest and butt (as her boyfriend) | 5 | 4 | — |
| Chest and butt (just friends) | — | — | wary yes 3, yes 4 |
| Kiss (as her boyfriend) | 5 | 4 | 3 |
| Kiss (just friends) | — | — | wary yes 3, yes 4 |
| H while dating | 7 | 4–5 | wary yes 2, yes 3 |
| H as just friends | wary yes 8 | wary yes 6–7 | wary yes 2, yes 4 |

- A dash means that combination was not measured on its own: a reserved or normal girl usually becomes your girlfriend before she lets you go that far as a friend.
- **Mood**: flirty moves things about a day earlier (on a perfect first day an open girl can say yes to H); cheerful about half a day; lonely barely changes anything; upset pushes things back, the hand to around day 5 and H one or two days later.
- **Order of the answers**: across 60 simulations (6 girls × 5 moods × dating or not) the H answer never went backwards: always no → wary no → wary yes → yes (skipping steps, never going down).

## 3. A campus that lives

- **Friendships, rivalries and romances between the girls** (crush, talking, couple, crisis, breakup and getting back together), with trust, jealousy and intimacy.
- **Gossip and rumors** that pass from girl to girl through friendships and get distorted on the way.
- **Real witnesses**: only those close enough, looking your way and with nothing in between saw it (a talk, flirting, holding hands, a touch, a kiss, H).
- **Each girl's reputation** (0–100): goes up with friends, down with enemies; popular girls and outcasts.
- **Bonds in motion**: 3- or 4-day stories two students live on their own (friends drifting apart, rivals forced to work together, a shared hobby, an old fight settled).
- **Pairs on the map**: two girls come to call you together, with a reason.
- **Harem**: two girlfriends who like each other can agree to share you, over three conversations.
- **Grudges and making up**, jealousy by relationship, memory of what your girlfriend already decided about you.
- **Moods and needs** (cheerful, hungry, bored, stressed, lonely, flirty, sleepy, upset).
- **Weather of the day**, rolled by the month (a rainy season in June, typhoons in September): rain changes lines, who goes outside and events.
- **Professors and the nurse act like staff**: they sit at the faculty desks, work in the infirmary and walk the halls. Talking to one opens a conversation of her own, by her personality and her job (a raise, her class's results, grading all night, a complaint). Students gossip when you get involved with one, and professors scold.
- **Story heroines and professors on campus**: the director brings them in; with a big class, only those with an event that day are sure to come, and the girl you are courting comes back the next day.
- **Weekly spotlight**: in big classes, some girls are in focus; nobody is left out for more than 3 weeks.
- **A girl napping anywhere** can be teased awake; half asleep, she gives in more easily.

## 4. College life

- **Club board** with requests the girls tell you about (one per girl at a time, none at night), with markers (star, diamond, heart) on the minimap, on her card and in the menu. Girls who miss you show up on the board too.
- **"Meet me at..."**: arrive at the right place and period and she really comes.
- **Activities and part-time jobs** at real spots of the campus, with an animation (INT, STR, reputation, money).
- **A small store in the cafeteria**, gifts of what she likes and gift requests of the day; a gift that fulfils her request is framed in pink.
- **Devotion**: past 100 affection, devotion keeps rising and each level gives a bonus.
- **Teleport without spending time** from the menu, and walking with a golden arrow to the right door.
- **The radar becomes a map**: the floor plan, and whoever is on another floor shows faded with a small arrow.
- **Loading screen** with a photo of the destination map (every map has one, shipped with the mod), "Going to: ..." and a tip, instead of the game's white fade.
- **Alt+F7** places the activity spots, the nap tease icon and each map's loading photo where you stand.

## 5. Story and records

- **Dating chapters**: becoming her boyfriend isn't the end; each girlfriend goes through chapters a few days apart.
- **Sunday stories** in three parts after the game's date, with the game's backgrounds.
- **Featured episodes** staged like the story mode, with the game's backgrounds.
- **Calendar events** as long scenes with choices that come back later; each keeps its nature (Chocolate Day, an exam, a trip).
- **Club project** before the festival, **student council election** every 9-week cycle, **club rank** as a story chapter.
- **Class anthology**: the big moments of the season become a book shown on the last Saturday of the cycle.
- **Achievement album** (F7): a record and a to-do list at once, with stages ("Familiar Face, 3/4"), secrets with a hint and a small Steam-style card with a sound when one unlocks.
- **Weekly report** and a journal in the menu.

## 6. Male cards

- A card saved as male is detected on its own, and the **male** folder shows up next to **female** when choosing the class. He uses the men's restroom, loses reputation if he walks into the women's areas and catches the girls who walk into the men's.
- Lines **about him** say he/him/his and boyfriend; **he himself** says boyfriend or guy; the voice and personality stay the same. His card says "Boyfriend".

## 7. Menu and options

- **F7** opens the menu inside the game's clipboard, with its fonts and sprites: Today (status and requests), Calendar, Activities, Album, Details (how everything works), and the campus store. At night, a clipboard icon in your room opens it to plan the next day.
- On the game's clipboard (middle button), each card gets three pages: Now / With you / Love.
- Every option is also in the ConfigurationManager (F1), in numbered sections.

## The sibling mods

- **Campus Saves**: one save slot per campus, a version vault that keeps old days in little space, saving mid-day and smart autosaves (Ctrl+F6).
- **School Optimizer**: fewer students loaded per period with priority for the important ones (Campus Life says who), a memory guard, off-screen animation pause, physics by distance, a texture size limit, outside fixes for other mods (AmazingNewAccessoryLogic, MaterialEditor, UncensorSelector) and parking the data of whoever is absent.
- **H Male Wardrobe**: changes the protagonist's outfit in the middle of H with a copy of the girl's outfit menu (F10).

## Install

1. Download the zip from the [releases](https://github.com/Jooj000/KK_CampusLife/releases).
2. Drop its `BepInEx` folder into the game folder. The whole mod is one file, `BepInEx\plugins\Joojkk\KK_CampusLife.dll`.
3. Story mode, **F7**.

BepInEx and KKAPI come with the HF Patch.

The data lives inside the save; removing the mod leaves the save playable.
