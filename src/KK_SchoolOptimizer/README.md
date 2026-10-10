# School Optimizer for Koikatsu

**A smoother, lighter school in Koikatsu's story mode.** Fewer students loaded at once (the important ones first), a memory guard, physics and animation only where you can see them, background outfit changes, and outside fixes for other mods that eat video memory.

> Part of [Joojkk's Koikatsu mods](https://github.com/Jooj000/koikatsu-joojkk-mods) ·
> Sister mods: [Campus Life](https://github.com/Jooj000/KK_CampusLife) ·
> [H Male Wardrobe](https://github.com/Jooj000/KK_HMaleWardrobe)

| | |
|---|---|
| GUID | `joojkk.schooloptimizer` |
| Game | Koikatu / Koikatsu Party, main game (story mode) |
| Works alone | Yes; Campus Life is not needed |

## Features

**Students**
- A cap on the students loaded per period, with slots reserved for the girls that matter (girlfriends, story heroines, the ones you talked to); the cap goes down by itself when RAM is almost full.
- Absent students keep only their game data; students on other maps can drop their clothes meshes (and, as an experiment, be unloaded entirely).
- Students avoid walking into each other (personal space).

**Rendering**
- Students off screen pause their animation.
- Hair and clothes physics by distance: full up close, off far away and off screen.
- A size limit for outfit textures in story mode.

**Loading**
- Outfit changes of unseen students happen in the background.
- Faster loading screens.
- Clothes and accessories left lying in the middle of the map are removed.
- A list of the zipmods your cards use but you do not have.

**Other mods, fixed from outside** (each one can be turned off; their files are never changed)

| Mod | Fix |
|---|---|
| AmazingNewAccessoryLogic | Its idle per-character cameras switched off while its editor is closed. |
| MaterialEditor | Texture size limit; its edits applied again after slow loads. |
| HairAccessoryCustomizer | Its colors applied again after slow loads. |
| UncensorSelector | Small textures for the parts a card keeps hidden. |
| KK_Plugins | Shared texture cache cleanup; accessory clothes bound back to their owner. |
| KK_Pregnancy, KK_LewdCrestX | Their data kept when a student is parked off the map. |
| KK_GameplayMod | Its higher student limit is respected. |

## Installation

1. Install [BepInEx 5.4.23+](https://github.com/BepInEx/BepInEx), [KKAPI 1.45+, ExtensibleSaveFormat and Sideloader](https://github.com/IllusionMods) (the HF Patch has them).
2. Download `KK_SchoolOptimizer.dll` from the [releases](https://github.com/Jooj000/KK_SchoolOptimizer/releases) and put it in `BepInEx\plugins\Joojkk\`.

## Configuration

`BepInEx\config\joojkk.schooloptimizer.cfg`, or ConfigurationManager (F1), in six groups: students, crowding, rendering, other mods (shown only for the mods you have), loading, and the missing zipmods list. A new install starts at the maximum optimization, the values used on the author's own game; values already in your config are kept.

## Compatibility

- Harmony prefixes and postfixes only, every patch inside try/catch.
- Reads IllusionFixes' MainGameOptimizations so the same work is never done twice.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- Made by **Joojkk**.
- The outside fixes touch AmazingNewAccessoryLogic (njaecha), MaterialEditor, HairAccessoryCustomizer, UncensorSelector and KK_Plugins (DeathWeasel1337 and the IllusionMods contributors), KK_Pregnancy, KK_LewdCrestX and KK_GameplayMod (ManlyMarco). All credit for those mods stays with their authors.
- Koikatsu is © ILLUSION. No game files are included.

## License

MIT, see [LICENSE](../../LICENSE).
