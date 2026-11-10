# Campus Life for Koikatsu

**Campus life on top of Koikatsu's story mode.** The girls at your college get moods, memories, friendships and grudges of their own; a director plans their days; conversations become real conversations; and everything you do with them becomes part of a story the whole campus talks about.

> Part of [Joojkk's Koikatsu mods](https://github.com/Jooj000/koikatsu-joojkk-mods) ·
> Sister mods: [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer) ·
> [H Male Wardrobe](https://github.com/Jooj000/KK_HMaleWardrobe)

| | |
|---|---|
| GUID | `joojkk.campuslife` |
| Version | 3.6.1 |
| Game | Koikatu / Koikatsu Party, main game (story mode), HF Patch recommended |
| Menu | **F7** |
| Language | English (the XUnity AutoTranslator does not touch it) |

**At a glance:** ~47k lines of C# · 37 dialogue files where each line is written with the options that answer it · six tones (gentle, energetic, cool, teasing, shy, proper) with 4+ versions of every line · 54 staged achievements plus a climb for every story heroine and professor.

## Installation

1. Download `KK_CampusLife-<version>.zip` from the [releases](https://github.com/Jooj000/KK_CampusLife/releases).
2. Drop the `BepInEx` folder from the zip into the game's folder (the one with `Koikatu.exe`) and let it merge. That is all: the mod is one file, `BepInEx\plugins\Joojkk\KK_CampusLife.dll`, and it works from any folder under `BepInEx\plugins`.
3. Start the story mode and press **F7**.

It needs BepInEx and KKAPI, which the HF Patch already has. To update, drop the new zip the same way.

Campus Life keeps its data inside your save (ExtensibleSaveFormat). Removing the mod leaves the save playable.

## Features

### Conversation
- **A new talk screen** with five icon categories (Talk, Actions, Together, Romance, Spicy). Options are built from what she knows, what she likes, where you are and what happened today, not from a fixed list, and each one answers the line she just said.
- **Procedural topics**: her day, the two of you, you, gossip, campus, small talk, each scored for this moment. She keeps facts (and who told her, and twisted versions), remembers what you said and brings it back days later. Every line that depends on a like or on the rain has the side of a girl who does not care for it.
- **Her voice**: every line is written in her tone and shaped by her personality, mood and your relationship; short answers vary and never repeat back to back; a fitting clip of her own recorded game voice plays with each line.
- **Touches decided by the director**: hand, head, cheek, chest and butt, plus her lips and a look. Every touch gets one of the four answers, weighed by what you are to her, her pace, mood, trust, who is watching and how often you already touched there. The same touch in the same period gets the same answer, and insisting after a no makes it worse.
- **Requests with their own talk**: each kind has your line, four options written for it and her answer to each.
- **The rumor that she likes you** has its own scene: she answers as she feels, and you can confess from it.
- **Talk with two girls**: both side by side; aim at one, the other or both. They react to each other, tease, get jealous, help you out or walk off, and talk to each other about what is true right now. Pair-only options: ask one about the other, an embarrassing story, a note, invite both, a compliment in front of her, a hidden touch.
- **Badges** under the H bar show her mood and likes once you have found them out.
- A talk log for the whole conversation, and nothing acts while a line is on screen: categories, options and touches wait until the box closes.

### The director
- **The director decides what the girls do**: each period it updates their needs (company, food, rest), intentions, pairs and rumors, and picks the situations that play in front of you or off screen.
- **Four answers** to everything that matters (touches, the kiss, invitations, confession, dates, H): **yes**, **wary yes**, **wary no** and **no**, read from as many factors as it can: what you are to her, her pace and bars, the days together, your reputation, trust, mood, H before, a recent kiss, another girlfriend, a perfect day, how often she already said no.
  - A **wary yes** leaves guilt: next talk she asks you to take responsibility or pulls away, and you settle it.
  - A **wary no** is an almost-yes: she wanted to, and she says what held her back.
- **Pace by personality**: reserved, normal and open girls take different numbers of days (see below). H does not need dating; it depends on the context. Three Ease options make each pace easier or harder.
- **Breakups and cheating**: a girlfriend who finds out about another girl reacts by her mood, anger, trust, how often it happened, who the other girl is, how far it went and whether she saw it.
- **Card markers** (easy, lewd, submissive, serious...): when a card has all of them ticked, or none, the mod draws the ones that fit her personality.
- **Before the game's H** she says aloud what her card refuses (protection, kisses, the back door, touching, toys), in her own voice. The game's H itself is unchanged.

#### Days to each milestone

Well-played days (three talks a day, good answers), default Ease, averaged over two girls of each pace, no special mood:

| Milestone | Reserved | Normal | Open |
|---|---|---|---|
| Flirting and compliments land | 2 | 2 | 2 |
| Holding hands | 3 | 2 | 2 |
| Friend | 4 | 3 | 3 |
| Girlfriend | 5 | 4 | 3 |
| Chest and butt (dating) | 5 | 4 | — |
| Chest and butt (friends only) | — | — | wary yes 3, yes 4 |
| Kiss (dating) | 5 | 4 | 3 |
| Kiss (friends only) | — | — | wary yes 3, yes 4 |
| H while dating | 7 | 4-5 | wary yes 2, yes 3 |
| H as friends only | wary yes 8 | wary yes 6-7 | wary yes 2, yes 4 |

- A dash is a combination not measured on its own: a reserved or normal girl usually dates you before she lets you go that far as a friend.
- **Mood**: flirty brings things about a day earlier (on a perfect first day an open girl can say yes to H); cheerful half a day; lonely barely changes it; upset holds hands back to around day 5 and H by 1-2 days.
- **Answer order**: in all 60 runs (6 girls x 5 moods x dating or not) the H answer never went back down: no, wary no, wary yes, yes (sometimes skipping a step, never going back).

### A college that lives
- **Friendships, rivalries and romances between the girls** (crush, talking, couple, strained, broken and back), with trust, jealousy and intimacy.
- **Gossip and rumors** that travel along friendships and get twisted on the way.
- **Real witnesses**: only someone close enough, looking your way and with nothing in between saw it (a talk, a flirt, holding hands, a touch, a kiss, H).
- **Each girl's reputation** (0-100), popular or left out, driven by her friendships.
- **Bonds in Motion**: three- or four-day stories two students live on their own.
- **Pairs on the map**: two girls walk over together with a reason to call you.
- **Harem**: two girlfriends who like each other can agree to share you, over three talks.
- **Grudges and making up**, jealousy by relationship, and a girlfriend who remembers what she already decided.
- **Moods and needs** (cheerful, hungry, bored, stressed, lonely, flirty, sleepy, upset).
- **Weather of the day** by the month (a rainy season in June, typhoons in September): rain changes lines, who goes outside and events.
- **Professors and the nurse act like staff**: faculty desks, the infirmary, the halls. Talking to one opens a conversation of her own, by her personality and her job; students gossip when you get involved with one, and professors scold.
- **Story heroines and professors on campus**: the director brings them in; with a big class only those with an event that day are sure to come, and the girl you are courting comes back the next day.
- **Weekly spotlight** for big classes: nobody is left out for more than three weeks.
- **A girl napping anywhere** can be teased awake; half asleep she gives in more easily.

### Campus life
- **Club board** with requests the girls tell you about (one per girl at a time, none at night), and shape markers on the minimap, her card and the menu. Girls who miss you show up there too.
- **"Meet me at..."**: reach the place in the right period and she really comes.
- **Activities and part-time jobs** at real places of the campus, with an animation.
- **Campus store** in the cafeteria, gifts she likes and today's gift requests; a gift that fulfils her request is framed in pink.
- **Devotion**: past 100 favor, devotion keeps growing and every level unlocks a perk.
- **Free travel** from the menu (no time lost) and a walking guide with a gold arrow over the right door.
- **The radar becomes a map**: the floor plan, and girls on another floor are dimmed with a small arrow.
- **Loading screen** with a photo of the destination map (shipped for every map), "Going to: ..." and a tip, instead of the game's blank white fade.
- **Alt+F7** places the activity spots, the nap tease icon and each map's loading photo where you stand.

### Story and records
- **Relationship chapters**: becoming her boyfriend is not the finish line.
- **Sunday stories** in three parts after the game's own date, on the game's backgrounds.
- **Spotlight episodes** staged like story mode, on the game's own backgrounds.
- **Calendar events** as long scenes with choices that meet again; each keeps its nature (Chocolate Day, an exam, a trip).
- **Club project** before the festival, **student union race** every 9-week loop, **club rank** as a story chapter.
- **The Class Anthology**: the season's big moments, shown as a book on the last Saturday of the loop.
- **Album of achievements** (F7): a record and a to-do list, with stages ("Familiar Face, 3/4"), secrets with hints and a Steam-style card with a ding when one falls.
- **Weekly report** and journal in the menu; at night a clipboard icon in your room opens the menu to plan the next day.

### Male cards
- A card saved as male is detected on its own, and the **male** folder shows next to **female** when you pick the class. He uses the boys' toilet, loses reputation in the girls' areas and catches girls who walk into the boys' toilet.
- Lines **about him** say he/him/his and boyfriend; **his own** lines call himself a boyfriend or a guy; his voice and personality stay the same. His card says "Boyfriend".

## Optional mods

Detected at run time, never required. Without them, every option, line or achievement about them is hidden.

| Mod | What Campus Life does with it |
|---|---|
| [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer) | Shares which students matter today, so a lower student limit never leaves them out. |
| KK_LewdCrestX | Crest achievements. |
| KK_PantyFairy | Panty Fairy achievements. |
| KK_Pregnancy | "Family Way" achievement. |

## Configuration

Everything is in the F7 menu and in `BepInEx\config\joojkk.campuslife.cfg` (also through ConfigurationManager, F1, in numbered sections). Hotkeys: **F7** opens the menu, **Alt+F7** the activity spots window. The game's own keys (Esc, F1-F5, F8, F11) are left alone.

## Compatibility

- Harmony prefixes and postfixes only, every patch inside try/catch: on any error the game's own behaviour stays.
- Works with the HF Patch's usual plugins (IllusionFixes, KK_Plugins, MaterialEditor, Sideloader).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- Made by **Joojkk**.
- Built on BepInEx, HarmonyX and KKAPI / ExtensibleSaveFormat (IllusionMods).
- Koikatsu is © ILLUSION. No game files are included.

## License

MIT, see [LICENSE](../../LICENSE).

## Note

- The mod does not add any characters, and all explicit dialogue has been tested only with characters over the age of 18. Neither the mod nor its creator assumes responsibility for how it is used.