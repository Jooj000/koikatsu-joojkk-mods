# Joojkk's Koikatsu mods

BepInEx plugins for the Koikatsu main game (Koikatu / Koikatsu Party, HF Patch install).

| Plugin | GUID | What it does |
|---|---|---|
| **Campus Life** (`KK_CampusLife`) | `joojkk.campuslife` | School life on top of the story mode: a club board with requests girls tell you about, moods, gifts and a school store, activities and part-time jobs at real spots of the school (with the game's own animations), a new conversation (talk / romance / invitations pages), a social director (friendships, trust, gossip, jealousy), short stories, calendar events, story heroines walking around as students, girls voiced with clips from the game, a director that answers yes, wary yes, wary no or no by context, and male cards (he/him, boyfriend). Overview: [OVERVIEW.md](OVERVIEW.md) (Portuguese: [APRESENTACAO.pt-BR.md](APRESENTACAO.pt-BR.md)). Menu: **F7**. |
| **Campus Saves** (`KK_CampusSaves`) | `joojkk.campussaves` | One save slot per campus, a version vault that keeps old days in little space, saving in the middle of the day and smart autosaves. **Ctrl+F6**. |
| **H Male Wardrobe** (`KK_HMaleWardrobe`) | `joojkk.hmalewardrobe` | Change the male character's outfit during H with a copy of the girl's outfit menu. **F10**. |
| **School Optimizer** (`KK_SchoolOptimizer`) | `joojkk.schooloptimizer` | Fewer students loaded per period with priority for the important ones, memory guard, crowd avoidance, off-screen animation pause, hair and clothes physics by distance, background outfit changes, outfit texture size limit, and outside fixes for other mods (idle AmazingNewAccessoryLogic cameras, MaterialEditor texture size, hidden UncensorSelector parts) that cut video memory use, removal of character pieces left lying in the middle of the map, and (experimental, off) unloading students on other maps. |

## Repositories

This repository holds the four mods together (it is where they are developed and built). Each one also has a repository of its own, with its own README, changelog and releases:

| Mod | Its repository | Docs here |
|---|---|---|
| Campus Life | [Jooj000/KK_CampusLife](https://github.com/Jooj000/KK_CampusLife) | [README](src/KK_CampusLife/README.md) · [CHANGELOG](src/KK_CampusLife/CHANGELOG.md) |
| School Optimizer | [Jooj000/KK_SchoolOptimizer](https://github.com/Jooj000/KK_SchoolOptimizer) | [README](src/KK_SchoolOptimizer/README.md) · [CHANGELOG](src/KK_SchoolOptimizer/CHANGELOG.md) |
| Campus Saves | [Jooj000/KK_CampusSaves](https://github.com/Jooj000/KK_CampusSaves) | [README](src/KK_CampusSaves/README.md) · [CHANGELOG](src/KK_CampusSaves/CHANGELOG.md) |
| H Male Wardrobe | [Jooj000/KK_HMaleWardrobe](https://github.com/Jooj000/KK_HMaleWardrobe) | [README](src/KK_HMaleWardrobe/README.md) · [CHANGELOG](src/KK_HMaleWardrobe/CHANGELOG.md) |

Downloads are on each mod's Releases page. The single-mod repositories are exported from this one with `tools\split_repos.ps1`.

## Requirements
- Koikatsu with BepInEx 5.4.23+, KKAPI 1.45+ and ExtensibleSaveFormat (HF Patch has them).
- The full list of required and optional mods, and what each plugin does with them: `DEPENDENCIES.md`.
- Building: the Roslyn `csc.exe` of Visual Studio 2019 (the game runs on .NET 3.5 / Unity 5.6).

## Building
```
powershell -ExecutionPolicy Bypass -File build.ps1 -Game "<Koikatsu folder>" [-Install]
```
`-Install` copies the DLLs and `data\` to `BepInEx\plugins\Joojkk\`. `-Only KK_CampusLife` builds just one (Campus Saves is built only this way: `-Only KK_CampusSaves`). `tools\package_release.ps1` makes the release zips.

## Layout
- `src\` – the four plugins.
- `tools\package_release.ps1` – builds the release zips, one per mod.
- `data\` – files installed next to the DLLs.
- `DIALOGUES.md` – every line of dialogue written for Campus Life.

## Credits
- Made by **Joojkk**.
- Built on [BepInEx](https://github.com/BepInEx/BepInEx), [HarmonyX](https://github.com/BepInEx/HarmonyX) and [KKAPI / ExtensibleSaveFormat](https://github.com/IllusionMods/IllusionModdingAPI) (IllusionMods).
- The School Optimizer patches some other plugins from outside, without changing their files: AmazingNewAccessoryLogic (njaecha), MaterialEditor and UncensorSelector (DeathWeasel1337 and the IllusionMods contributors). Each fix can be turned off in its config.
- Koikatsu is © ILLUSION. No game files are included in this repository.

## License
MIT, see `LICENSE`.
