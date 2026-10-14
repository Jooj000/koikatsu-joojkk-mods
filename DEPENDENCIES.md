# Dependencies

What each plugin needs to load (**required**) and what it uses only when it is installed (**optional**). Everything in both lists comes with the HF Patch; without the HF Patch, install the required ones by hand. Optional mods are detected at run time (`BepInEx.Bootstrap.Chainloader.PluginInfos`) and read by reflection, never linked: when one is missing, every option, achievement or line about it is hidden and the plugin works the same without it.

## Required

| Mod | GUID | Version | Needed by | Author |
|---|---|---|---|---|
| BepInEx | – | 5.4.23 (5.4.x) | all three | BepInEx team |
| HarmonyX (`0Harmony.dll`, ships with BepInEx) | – | – | all three | BepInEx team |
| KKAPI (ModdingAPI) | `marco.kkapi` | 1.30.1+ (Campus Life, H Male Wardrobe), 1.45+ (School Optimizer) | all three | ManlyMarco |
| ExtensibleSaveFormat (BepisPlugins) | `com.bepis.bepinex.extendedsave` | – | Campus Life (its save data), School Optimizer (parked students) | BepisPlugins team |
| Sideloader (BepisPlugins) | `com.bepis.bepinex.sideloader` | – | School Optimizer (the missing zipmods list) | BepisPlugins team |

The game itself: Koikatu / Koikatsu Party main game (story mode), Unity 5.6.2 / .NET 3.5.

## Optional

### Campus Life (`joojkk.campuslife`)

| Mod | GUID | What Campus Life does with it | Author |
|---|---|---|---|
| School Optimizer | `joojkk.schooloptimizer` | Shares which students are loaded on each map (story heroines walking around, the director's plans). | Joojkk |
| KK_LewdCrestX | `LewdCrestX` | The "Crests" achievement (Marked, Crest Collector). | ManlyMarco |
| KK_PantyFairy | `PantyFairy` | The "Panty Fairy" and "The Fairy's Collection" achievements (her story and haul). | ManlyMarco |
| KK_Pregnancy | `KK_Pregnancy` | Achievement "Family Way". | ManlyMarco |

### School Optimizer (`joojkk.schooloptimizer`)

| Mod | GUID | What School Optimizer does with it | Author |
|---|---|---|---|
| AmazingNewAccessoryLogic (ANAL) | `org.njaecha.plugins.anal` | Its idle per-character cameras switched off while its editor is closed. | njaecha |
| MaterialEditor | `com.deathweasel.bepinex.materialeditor` | Texture size limit in story mode; its edits applied again after slow loads. | DeathWeasel1337 and contributors |
| HairAccessoryCustomizer | `com.deathweasel.bepinex.hairaccessorycustomizer` | Its colors applied again after slow loads. | DeathWeasel1337 and contributors |
| UncensorSelector | `com.deathweasel.bepinex.uncensorselector` | Small textures for the parts a card keeps hidden. | DeathWeasel1337 and contributors |
| KK_Plugins (TextureContainerManager, ChaAccessoryClothes) | – | Shared texture cache cleanup; accessory clothes left unbound bound back to her. | DeathWeasel1337 and contributors |
| IllusionFixes (MainGameOptimizations) | – | Read only, to avoid doing the same work twice. | ManlyMarco and contributors |
| KK_Pregnancy, KK_LewdCrestX | `KK_Pregnancy`, `LewdCrestX` | Their per-character data kept when a student is parked off the map. | ManlyMarco |
| KK_GameplayMod | `marco-gameplaymod` | Its higher student limit is respected (the game's own setting is read as is). | ManlyMarco |

The options of the "5. Other mods" section show only when their mod is installed.

### H Male Wardrobe (`joojkk.hmalewardrobe`)

No optional mods.

### Campus Saves (`joojkk.campussaves`)

- Campus Life: with it, each save also shows the day number. Campus Saves works without it.

## Developer only

