# Campus Saves

One save slot per campus. The slot stays a normal Koikatsu save, and every save also becomes a version in a vault next to it, so a long game keeps its whole history in a fraction of the space. Adds saving in the middle of the day and autosave.

Author: Joojkk.

## What it does

- **Safe saving:** the game writes to a temporary file first, and the slot is replaced only when the new save is whole. A crash in the middle of a save leaves the old one.
- **Versions:** each save is cut into pieces and only new pieces are stored. In testing, 12 versions of an 89 MB save took 75 MB.
- **Save during the day:** Ctrl+F6 when you are free on the map. Loading it brings back the period, the clock, the map and your position.
- **Autosave:** on period change, at the end of the day, after H, and on a timer (10 minutes by default, 0 turns it off). It waits for a calm moment, never in the middle of a conversation or an event.
- **Old versions thin out:** the last 10 automatic saves stay, then one per hour, then one per day. Saves made by hand are never removed.

## The panel

The game's Load and Save buttons open it, on the title screen and in the night menu, and Ctrl+Shift+F6 opens it anywhere. The save screen is the same panel: a click on a campus saves into it. The game's own list stays one button away, under the ice cream icon, and a setting turns the replacement off.

- **Blocks:** one per campus in a list that scrolls, each with the picture, the day, the dates and the cards of its latest save. One click loads that save. The magnifier opens the history and the bin deletes the campus, after asking.
- **History:** every version of the campus, newest first, each with its own number of cards. A click selects one and shows its picture and details, without loading. Play or a double click loads exactly that version, from the title screen or from the night menu.
- **Import:** groups your slots by campus and copies them into the vaults. Your files are not changed. When it leaves repeated slots behind, it offers to free them.
- **Free space:** deletes the imported slots that are not the newest of their campus, after checking that each one can be rebuilt. It asks first.
- **Export saves:** rebuilds the last version of each day as a plain save, in the free slots, with the date it was saved. Saves made during a day all open on the night before, so a day needs only one. It says how many saves and how much space before it starts, and stops when the game runs out of its 100 slots.

## The Esc menu

In story mode, Esc opens Resume, Save, Save & Quit to title, Quit to title and Quit, in place of the game's quit screen. Saving needs the same calm moment as autosave. A setting turns it off.

Pictures exist from the moment this mod saves: imported saves show none, because the game's saves have no picture. The game has no date and no hour, so a save shows its weekday and period, and the day number when Campus Life is installed. A save from the middle of a day loads from the title screen, not from the night menu.

## Removing the mod

Press Export saves first. Each version takes a slot at its full size again. The slots are normal saves and open without this mod; a save made during the day then opens the way the game opens any save, not at the saved moment.

## Installation

1. Install [BepInEx 5.4.23+](https://github.com/BepInEx/BepInEx) and [KKAPI 1.45.1+ / ExtensibleSaveFormat](https://github.com/IllusionMods/IllusionModdingAPI) (the HF Patch has them).
2. Download `KK_CampusSaves-<version>.zip` from the [releases](https://github.com/Jooj000/KK_CampusSaves/releases) and unpack it over the game's folder (it puts `KK_CampusSaves.dll` in `BepInEx\plugins\Joojkk\`).
3. Open Load or Save as usual, or press Ctrl+Shift+F6. Vaults live in `UserData\save\campus`.
