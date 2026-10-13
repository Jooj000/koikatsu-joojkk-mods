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

Ctrl+Shift+F6 opens it. Load works on the title screen.

- **Import:** groups your slots by campus and copies them into the vaults. Your files are not changed.
- **Free space:** deletes the imported slots that are not the newest of their campus, after checking that each one can be rebuilt. It asks for a second click.
- **Normalize:** puts the versions made by hand and the imported ones back into free slots, as plain saves.
- **Export:** puts one version into a free slot.

## Removing the mod

Press Normalize first if you used Free space. The slots are normal saves and open without this mod; a save made during the day then opens the way the game opens any save, not at the saved moment.

## Requirements

BepInEx 5.4.23, KKAPI 1.45.1 and ExtensibleSaveFormat. Vaults live in `UserData\save\campus`.
