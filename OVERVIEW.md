# Campus Life — overview

*Português: [APRESENTACAO.pt-BR.md](APRESENTACAO.pt-BR.md)*

**A university life mod for Koikatsu's story mode.** The girls at your university get their own moods, memories, friendships and grudges; a director plans each one's day; talking to them becomes a real conversation; and everything you do with them becomes part of a story the whole school talks about.

By **Joojkk** · version **3.6.0** · BepInEx 5 / KKAPI · Koikatu (HF Patch) · menu **F7** · interface and dialogue in English.

It comes with three sibling mods: **Campus Saves** (one save slot per campus, Ctrl+F6), **School Optimizer** (a full university without stutter) and **H Male Wardrobe** (the protagonist's outfit during H, F10).

---

## In numbers

| | |
|---|---|
| Code | ~46k lines of C# (.NET 3.5), Harmony prefix/postfix only, everything wrapped in try/catch |
| Dialogue | ~2,600 hand-written situations + a procedural bank of ~6,500 lines (18 `.dlg` files) |
| Voices | 4 tones (gentle, energetic, cool, teasing); 4,464 lines with 4 or more versions per tone, never the same one twice in a row |
| Achievements | 45 staged achievements + one ladder for each story heroine and each teacher |

---

## 1. Conversation

- **A new conversation screen** with five icon categories: **Talk**, **Actions**, **Together**, **Romance** and **Spicy**. The options come from what she knows, what she likes, where you are and what happened today, not from a fixed list.
- **Procedural conversation**: scored topics (her day, the two of you, you, gossip, university, small talk), facts she knows and who she heard them from, opinions and distorted versions. She remembers what you told her and brings it up days later.
- **Her own voice**: every line is written in the 4 tones, shaped by her personality, mood and your relationship; varied short replies; the game plays a clip of her own recorded voice that fits the line.
- **Touches with answers**: head, cheek, hands, chest, plus spots of our own (butt, lips). She answers in her own words, depending on the spot, how many times you insisted, how close you are and what her card allows. A kiss can be a yes, a wary yes or a no.
- **Three-way conversation**: talk to one, to the other or to both. They react to each other, tease, get jealous, help out or walk off. Options that only exist with two girls: asking one about the other, an embarrassing story, a note, an invitation for both, a compliment in front of the other, a hidden touch.
- **Scene shapes**: a staged talk, an aside, a passing line, a favor, a walk together, a quiet moment, a triangle and a callback.
- **Conversation badges**: her mood and likes as round icons under the H bar (only after you find them out).
- **Nothing acts while a line is on screen**: categories, options and touches wait for the dialogue box to close.

## 2. The director (the girls' AI)

- **The director runs 100% of the AI**: every action of every girl is its choice, based on her needs (company, food, rest), her bonds and her life. Nothing of the original AI is left.
- **Four answers** to everything that matters: **yes**, **wary yes**, **wary no** and **no**. The director reads as many factors as it can: what you are to her, her pace, the bars, the days together, your reputation, trust, mood, past H, a recent kiss, another girlfriend, a perfect day, how many times she already said no.
  - A **wary yes** leaves guilt: in the next conversation she asks you to commit or pulls away, and you settle it.
  - A **wary no** is almost a yes: she wanted to, and she says what held her back (personality, mood, guilt, your girlfriend, too soon, already asked today, a grudge, reputation, or just not here and now).
- **Pace by personality**: reserved, normal and open girls take different numbers of days to become friends, date and go further (table below). H doesn't require dating: it depends on context.
- **Invitations, confession, holding hands, kissing, breakups, cheating**: all decided by the director. A girlfriend who finds out about another girl reacts by her mood, anger, trust, how many times it happened, who the other girl is and whether she saw it.
- **Card traits** (easy, perverted, submissive, serious...): when a card has all of them ticked, the mod reads the ones that make sense instead of making every girl easy.
- **Before the game's H** she says out loud what her card refuses (protection, kissing, back door, touches, toys), in her own voice. The game's H itself doesn't change.

### How many days to each milestone

Well-played days (3 conversations a day, good answers), the average of two girls of each pace, no special mood:

| Milestone | Reserved | Normal | Open |
|---|---|---|---|
| Flirting and compliments work | 2 | 2 | 2 |
| Holding hands | 3 | 2 | 2 |
| Friend | 4 | 3 | 3 |
| Girlfriend | 5 | 4 | 3 |
| Butt accepted on the 1st touch (dating / just friends) | 5 / 6 | 4 / 6 | 3 / 5 |
| Kiss (dating / just friends, wary) | 5 / 6 | 4 / 5 | 3 / 4 |
| H: first "wary no" (almost yes) | 5 | 4 | skips straight to yes |
| H while dating | yes on day 7 | yes on day 5 | wary yes on day 2, yes on day 3 |
| H as just friends | wary yes on day 8 | wary yes on day 6–7 | wary yes on day 2, yes on day 5 |

- Hand and chest touches (the game's own): accepted on the first touch on any day; what changes with the stage (acquaintance, friend, girlfriend) is the voice table, and insisting on the same spot leads to "uncomfortable", "stop" and anger.
- **Mood**: flirty moves everything about 1 day earlier (hand, flirting and butt on day 1; the open girl accepts H on day 1); cheerful moves things half a day earlier; lonely barely changes anything (the almost-yes comes a day earlier); upset pushes the hand to day 5 and H 1–2 days later.
- **Order of the answers**: across 60 simulations (6 girls × 5 moods × dating or not) the H answer never went backwards: always no → wary no → wary yes → yes (skipping steps, never going down).

## 3. A university that lives

- **Friendships, rivalries and romances between the girls** (crush, talking, couple, crisis, breakup and getting back together), with trust, jealousy and intimacy.
- **Gossip and rumors** that pass from girl to girl through friendships and get distorted on the way.
- **Real witnesses**: only those close enough saw it (a talk, flirting, holding hands, a touch, a kiss, H).
- **Each girl's reputation** (0–100): goes up with friends, down with enemies; popular girls and outcasts.
- **Bonds in motion**: 3- or 4-day stories two students live on their own (friends drifting apart, rivals forced to work together, a shared hobby, an old fight settled).
- **Pairs on the map**: two girls come to call you together, with a reason.
- **Harem**: two girlfriends who like each other can agree to share you, over three conversations.
- **Grudges and making up**, jealousy by relationship, memory of what your girlfriend already decided about you.
- **Moods and needs** (cheerful, hungry, bored, stressed, lonely, flirty, sleepy, upset).
- **Rain**: changes lines, movement and events, with the lightest method possible.
- **Story heroines as students** every day, and the director picks one whose story opens today.
- **Teachers** who walk around campus, with their own conversation events.
- **Weekly spotlight**: in big classes, some girls are in focus; nobody is left out for more than 3 weeks.
- **A girl sleeping in the infirmary**: you can tease her awake; half asleep, she gives in more easily.

## 4. University life

- **Club board** with requests the girls tell you about, with markers (star, diamond, heart) on the minimap, on her card and in the menu.
- **"Meet me at..."**: arrive at the right place and period and she really comes.
- **Activities and part-time jobs** at real spots of the campus, with the game's own animations (INT, STR, reputation, money).
- **A small store in the cafeteria**, gifts of what she likes and gift requests of the day.
- **Devotion**: past 100 affection, devotion keeps rising and each level gives a bonus.
- **Teleport without spending time** from the menu, and walking with a golden arrow to the right door.
- **The radar becomes a map**: the floor plan, and whoever is on another floor shows faded with a small arrow.
- **Loading screen**: gray with tips instead of the game's white.

## 5. Story and records

- **Dating chapters**: becoming her boyfriend isn't the end; each girlfriend goes through chapters a few days apart.
- **Sunday stories** in three parts after the game's date.
- **Featured episodes** staged like the story mode, with the game's backgrounds.
- **Calendar events** as long scenes with choices that come back later.
- **Club project** before the festival, **student council election** every 9-week cycle, **club rank** as a story chapter.
- **Class anthology**: the big moments of the season become a book shown on the last Saturday of the cycle.
- **Achievement album** (F7): a record and a to-do list at once, with stages ("Familiar Face, 3/4"), secrets with a hint and a small Steam-style card with a sound when one unlocks.
- **Weekly report** and a journal in the menu.

## 6. Male cards

- A card saved as male is detected on its own, and the **male** folder shows up next to **female** when choosing the class. He uses the men's restroom, loses reputation if he walks into the women's areas and catches the girls who walk into the men's.
- Lines **about him** say he/him/his and boyfriend; **he himself** says boyfriend or guy; the voice and personality stay the same. His card says "Boyfriend".

## 7. Menu and options

- **F7** opens the menu inside the game's clipboard, with its fonts and sprites: Today (status and requests), Calendar, Activities, Album, Details (how everything works).
- On the game's clipboard (middle button), each card gets three pages: Now / With you / Love.
- Every option is also in the ConfigurationManager (F1), grouped by subject.

## The sibling mods

- **Campus Saves**: one save slot per campus, a version vault that keeps old days in little space, saving mid-day and smart autosaves (Ctrl+F6).
- **School Optimizer**: fewer students loaded per period with priority for the important ones (Campus Life says who), a memory guard, off-screen animation pause, physics by distance, a texture size limit, outside fixes for other mods (AmazingNewAccessoryLogic, MaterialEditor, UncensorSelector) and parking the data of whoever is absent.
- **H Male Wardrobe**: changes the protagonist's outfit in the middle of H with a copy of the girl's outfit menu (F10).

## Install

1. BepInEx 5.4.23+ and KKAPI 1.45+ / ExtensibleSaveFormat (the HF Patch already has them).
2. Unpack the release zip over the game folder (`KK_CampusLife.dll` goes to `BepInEx\plugins\Joojkk\`).
3. Story mode, **F7**.

The data lives inside the save; removing the mod leaves the save playable.
