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
| Game | Koikatu / Koikatsu Party, main game (story mode), HF Patch recommended |
| Menu | **F7** |
| Language | English (the XUnity AutoTranslator does not touch it) |

## Features

**Conversation**
- A new talk screen with five icon categories (Talk, Actions, Together, Romance, Spicy): options are built from
  what she knows, what she likes, where you are and what happened today, not from a fixed list.
- A procedural dialogue bank: thousands of lines with her voice (gentle, energetic, cool, teasing), her personality,
  her mood and your relationship; she remembers what you told her and brings it back days later.
- Touch her and she answers in her own words, by part, by how close you are and by what her card allows.
- Talk with two girls at once: aim at one, the other or both; they react to each other, tease, get jealous, help you
  out or walk off. Options that only make sense in a pair (ask one about the other, slip a note, a hidden touch).

**A school that lives**
- A director that decides for the girls: invitations, confessions, kisses, H, anger and forgiveness, all from her
  personality, her pace, her trust in you and the day she is having. The game only plays what the director decided.
- Pace by personality: reserved, normal and open girls take different numbers of days to date you and to go further.
- Friendships, rivalries, gossip, witnesses, jealousy, shared girlfriends, grudges and making up.
- Moods and needs (food, rest, company), weather and rain, a calendar of school events, a club board with requests.
- Story heroines walk around as students every day, and the director picks one whose story event opens today.
- Teachers with their own conversation-based events.
- Activities and part-time jobs at real places of the school, with the game's own animations.

**Story and records**
- Relationship chapters with your girlfriend, Sunday stories, short episodes staged like story mode.
- An Album of achievements that climb through stages ("Familiar Face, 3/4, talk with 10 different girls; next: 20"),
  a climb for each story heroine and teacher (one stage per part of her story), Steam-style cards, secrets with hints.
- Before the game's H, a girl says aloud what her card refuses (protection, kisses, the back door, touching, toys), in
  her own voice; the game's H itself is unchanged.

## Installation

1. Install [BepInEx 5.4.23+](https://github.com/BepInEx/BepInEx) and
   [KKAPI 1.30.1+ / ExtensibleSaveFormat](https://github.com/IllusionMods/IllusionModdingAPI) (the HF Patch has them).
2. Download `KK_CampusLife.dll` from the [releases](https://github.com/Jooj000/KK_CampusLife/releases) and put it in
   `BepInEx\plugins\Joojkk\`.
3. Optional: put the `backgrounds` folder next to it for the painted event backgrounds.
4. Start the story mode and press **F7**.

Campus Life keeps its data inside your save (ExtensibleSaveFormat). Removing the mod leaves the save playable.

## Optional mods

Detected at run time, never required. Without them, every option, line or achievement about them is hidden.

| Mod | What Campus Life does with it |
|---|---|
| [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer) | Shares which students are loaded on each map. |
| KK_LewdCrestX | Crest achievements. |
| KK_PantyFairy | Panty Fairy achievements. |
| KK_Pregnancy | "Family Way" achievement. |

## Configuration

Everything is in the F7 menu and in `BepInEx\config\joojkk.campuslife.cfg` (also through ConfigurationManager, F1).
Hotkeys: **F7** opens the menu. The game's own keys (Esc, F1–F5, F8, F11) are left alone.

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
