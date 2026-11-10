# H Male Wardrobe for Koikatsu

**Dress the male in H the same way you dress the girl.** The game's own dress icon is copied into the H menu bar, right after hers, with all its sub-menus (clothes, accessories, change outfit, outfit type). Every copied button does for him exactly what the original does for her. Changes last only for that H scene.

> Part of [Joojkk's Koikatsu mods](https://github.com/Jooj000/koikatsu-joojkk-mods) ·
> Sister mods: [Campus Life](https://github.com/Jooj000/KK_CampusLife) ·
> [School Optimizer](https://github.com/Jooj000/KK_SchoolOptimizer)

| | |
|---|---|
| GUID | `joojkk.hmalewardrobe` |
| Game | Koikatu / Koikatsu Party (H scenes of the main game) |
| Hotkey | **F10** opens his dress menu (configurable) |

## Installation

1. Download `KK_HMaleWardrobe-<version>.zip` from the [releases](https://github.com/Jooj000/KK_HMaleWardrobe/releases).
2. Drop the `BepInEx` folder from the zip into the game's folder (the one with `Koikatu.exe`) and let it merge. That is all: the mod is one file, `BepInEx\plugins\Joojkk\KK_HMaleWardrobe.dll`, and it works from any folder under `BepInEx\plugins`.
3. Start an H scene and press **F10**, or click the dress icon after hers.

It needs BepInEx and KKAPI, which the HF Patch already has. To update, drop the new zip the same way.

## Configuration

`BepInEx\config\joojkk.hmalewardrobe.cfg`: the hotkey that opens his dress menu.

## Compatibility

No optional mods. No Harmony patches: it copies the game's own menu and calls the game's own methods for him; the girl's menu is never changed.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

- Made by **Joojkk**.
- Built on BepInEx, HarmonyX and KKAPI (IllusionMods).
- Koikatsu is © ILLUSION. No game files are included.

## License

MIT, see [LICENSE](../../LICENSE).
