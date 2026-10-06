# Campus Life for Koikatsu

**School life on top of Koikatsu's story mode.** The girls at your school get moods, memories, friendships and
grudges of their own; a director plans their days; conversations become real conversations; and everything you do
with them becomes part of a story the whole school talks about.

> Part of [Joojkk's Koikatsu mods](https://github.com/Jooj000/koikatsu-joojkk-mods) ·
> Sister mods: [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer) ·
> [H Male Wardrobe](https://github.com/Jooj000/KK_HMaleWardrobe)

| | |
|---|---|
| GUID | `joojkk.campuslife` |
| Version | 3.2.0 |
| Game | Koikatu / Koikatsu Party, main game (story mode), HF Patch recommended |
| Menu | **F7** |
| Language | English (the XUnity AutoTranslator does not touch it) |

**At a glance:** ~46k lines of C# · ~2,600 hand-written situations plus a procedural bank of ~6,500 lines · four
voices (gentle, energetic, cool, teasing) with 4+ versions of every line · 45 staged achievements plus a climb for
every story heroine and teacher.

## Features

### Conversation
- **A new talk screen** with five icon categories (Talk, Actions, Together, Romance, Spicy). Options are built from
  what she knows, what she likes, where you are and what happened today, not from a fixed list.
- **Procedural topics**: her day, the two of you, you, gossip, school, small talk, each scored for this moment. She
  keeps facts (and who told her, and twisted versions), remembers what you said and brings it back days later.
- **Her voice**: every line is written for four tones and shaped by her personality, mood and your relationship; short
  answers vary and never repeat back to back; a fitting clip of her own recorded game voice plays with each line.
- **Touches that answer**: head, cheek, hands, chest, plus places of our own (butt, lips). She answers in her own words
  by part, by how often you insisted, by how close you are and by what her card allows. A kiss can be yes, wary or no.
- **Talk with two girls**: aim at one, the other or both. They react to each other, tease, get jealous, help you out or
  walk off. Pair-only options: ask one about the other, an embarrassing story, a note, invite both, a compliment in
  front of her, a hidden touch.
- **Scene shapes**: a scene, an aside, a one-liner, a favor, a walk together, a quiet moment, a triangle, a callback.
- **Badges** under the H bar show her mood and likes once you have found them out.
- Nothing acts while a line is on screen: categories, options and touches wait until the box closes.

### The director
- **The director runs 100% of the girls' AI**: every action of every girl is its choice, from her needs (company, food,
  rest), her bonds and her life. Nothing of the vanilla AI is left.
- **Four answers** to everything that matters: **yes**, **wary yes**, **wary no** and **no**, read from as many factors
  as it can: what you are to her, her pace and bars, the days together, your reputation, trust, mood, H before, a
  recent kiss, another girlfriend, a perfect day, how often she already said no.
  - A **wary yes** leaves guilt: next talk she asks you to take responsibility or pulls away, and you settle it.
  - A **wary no** is an almost-yes: she wanted to, and she says what held her back.
- **Pace by personality**: reserved, normal and open girls take different numbers of days (see below). H does not
  need dating; it depends on the context.
- Invitations, confessions, holding hands, kisses, breakups and cheating are all the director's call. A girlfriend who
  finds out about another girl reacts by her mood, anger, trust, how often it happened, who the other girl is and
  whether she saw it.
- **Card markers** (easy, lewd, submissive, serious...) are read sensibly even when a card has all of them ticked.
- **Before the game's H** she says aloud what her card refuses (protection, kisses, the back door, touching, toys), in
  her own voice. The game's H itself is unchanged.

#### Days to each milestone

girls of each pace, no special mood:

| Milestone | Reserved | Normal | Open |
|---|---|---|---|
| Flirting and compliments land | 2 | 2 | 2 |
| Holding hands | 3 | 2 | 2 |
| Friend | 4 | 3 | 3 |
| Girlfriend | 5 | 4 | 3 |
| Butt welcomed at the first touch (dating / friends only) | 5 / 6 | 4 / 6 | 3 / 5 |
| Kiss (dating / friends only, wary) | 5 / 6 | 4 / 5 | 3 / 4 |
| H: first "wary no" (almost yes) | 5 | 4 | skips straight to yes |
| H while dating | yes on day 7 | yes on day 5 | wary yes on 2, yes on 3 |
| H as friends only | wary yes on day 8 | wary yes on day 6-7 | wary yes on 2, yes on 5 |

- The game's own hand and chest touches are welcomed at the first touch on any day; the stage (acquaintance, friend,
  girlfriend) picks her voice table, and insisting on the same place goes to uneasy, "stop" and anger.
- **Mood**: flirty brings everything about a day earlier (hands, flirting and butt on day 1; an open girl says yes to H
  on day 1); cheerful half a day; lonely barely changes it; upset holds hands back to day 5 and H by 1-2 days.
- **Answer order**: in all 60 runs (6 girls x 5 moods x dating or not) the H answer never went back down: no, wary no,
  wary yes, yes (sometimes skipping a step, never going back).

### A school that lives
- **Friendships, rivalries and romances between the girls** (crush, talking, couple, strained, broken and back), with
  trust, jealousy and intimacy.
- **Gossip and rumors** that travel along friendships and get twisted on the way.
- **Real witnesses**: only someone close enough for what happened (a talk, a flirt, holding hands, a touch, a kiss, H)
  saw it.
- **Each girl's reputation** (0-100), popular or left out, driven by her friendships.
- **Bonds in Motion**: three- or four-day stories two students live on their own.
- **Pairs on the map**: two girls walk over together with a reason to call you.
- **Harem**: two girlfriends who like each other can agree to share you, over three talks.
- **Grudges and making up**, jealousy by relationship, and a girlfriend who remembers what she already decided.
- **Moods and needs** (cheerful, hungry, bored, stressed, lonely, flirty, sleepy, upset).
- **Rain** that changes lines, movement and events, done the lightest way possible.
- **Story heroines walk around as students** every day; the director picks one whose story event opens today.
- **Teachers** walk the school too, with their own conversation-based events.
- **Weekly spotlight** for big classes: nobody is left out for more than three weeks.
- **A girl asleep in the infirmary** can be teased awake; half asleep she gives in more easily.

### School life
- **Club board** with requests the girls tell you about, and shape markers on the minimap, her card and the menu.
- **"Meet me at..."**: reach the place in the right period and she really comes.
- **Activities and part-time jobs** at real places of the school, with the game's own animations.
- **School store** in the cafeteria, gifts she likes and today's gift requests.
- **Devotion**: past 100 favor, devotion keeps growing and every level unlocks a perk.
- **Free travel** from the menu (no time lost) and a walking guide with a gold arrow over the right door.
- **The radar becomes a map**: the floor plan, and girls on another floor are dimmed with a small arrow.
- **Transition screen** with tips instead of the game's blank white fade.

### Story and records
- **Relationship chapters**: becoming her boyfriend is not the finish line.
- **Sunday stories** in three parts after the game's own date.
- **Spotlight episodes** staged like story mode, on the game's own backgrounds.
- **Calendar events** as long scenes with choices that meet again.
- **Club project** before the festival, **student council race** every 9-week loop, **club rank** as a story chapter.
- **The Class Anthology**: the season's big moments, shown as a book on the last Saturday of the loop.
- **Album of achievements** (F7): a record and a to-do list, with stages ("Familiar Face, 3/4"), secrets with hints
  and a Steam-style card with a ding when one falls.
- **Weekly report** and journal in the menu.

### Male cards (new in 3.2)
- A card saved as male is detected on its own; a femboy card saved as a girl can be listed under **Boys in the class**
  (first names, comma separated).
- Lines **about him** say he/him/his and boyfriend; **his own** lines call himself a boyfriend or a guy; his voice and
  personality stay the same. His card says "Boyfriend".

## Installation

1. Install [BepInEx 5.4.23+](https://github.com/BepInEx/BepInEx) and
   [KKAPI 1.45+ / ExtensibleSaveFormat](https://github.com/IllusionMods/IllusionModdingAPI) (the HF Patch has them).
2. Download `KK_CampusLife.dll` from the [releases](https://github.com/Jooj000/KK_CampusLife/releases) and put it in
   `BepInEx\plugins\Joojkk\`.
3. Optional: put the `backgrounds` folder next to it for the painted event backgrounds.
4. Start the story mode and press **F7**.

Campus Life keeps its data inside your save (ExtensibleSaveFormat). Removing the mod leaves the save playable.

## Optional mods

Detected at run time, never required. Without them, every option, line or achievement about them is hidden.

| Mod | What Campus Life does with it |
|---|---|
| [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer) | Shares which students matter today, so a lower student limit never leaves them out. |
| KK_LewdCrestX | Crest achievements. |
| KK_PantyFairy | Panty Fairy achievements. |
| KK_Pregnancy | "Family Way" achievement. |

## Configuration

Everything is in the F7 menu and in `BepInEx\config\joojkk.campuslife.cfg` (also through ConfigurationManager, F1,
grouped by subject). Hotkeys: **F7** opens the menu. The game's own keys (Esc, F1-F5, F8, F11) are left alone.

## Compatibility

- Harmony prefixes and postfixes only, every patch inside try/catch: on any error the game's own behaviour stays.
- Works with the HF Patch's usual plugins (IllusionFixes, KK_Plugins, MaterialEditor, Sideloader).

## Building from source

See the [combined repository](https://github.com/Jooj000/koikatsu-joojkk-mods#building): `build.ps1` compiles with
the Roslyn `csc.exe` of Visual Studio 2019 against the game's own DLLs (.NET 3.5 / Unity 5.6). The dialogue bank
(`data\dialogue\*.dlg`) is built into the DLL.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- Made by **Joojkk**.
- Built on BepInEx, HarmonyX and KKAPI / ExtensibleSaveFormat (IllusionMods).
- Koikatsu is © ILLUSION. No game files are included.

## License

MIT, see [LICENSE](../../LICENSE).
