# Variable gate

`i_varGate` is Erebor's configurable gate (`systems/variableGate.scp`), for dungeon entrances and quest gates.
Code: `scripts/Basic/VarGate`. A GM adds one with `.add i_varGate` and double-clicks it for its dialog. A GM's
own step just goes through it, without any of the conditions, so a dialog is opened from beside it.

Gates aren't in the archive, Erebor's GMs placed them live, so `.SetupWorld` has none.

## What a step does

In this order. Anything that stops the walker stops it before anything is taken or given.

1. No target set: nothing happens, and the server log gets a warning.
2. A linked guard's spawn has a monster alive: stays on the gate.
3. An NPC without "NPC muze projit", a possessing mystic without "Hypno-NPC muze projit" ("Tuto branu nemuzes
   pouzit."), a ghost without "Duchove mohou projit": stays on the gate.
4. Level: `min,max` is a range, inclusive. Only `min` means above it, not at it.
5. Remove list, add list, deny-capable functions, Time Seal: a refusal steps the walker back off the gate.
6. Taken: cursor, PvP tools, mount, bank box, the remove list.
7. Given: non-deny functions, the add list, the Time Seal count, experience.
8. Sent to the target: `MoreP`, the rune for a recall gate, or home.

## Options

| Option | Meaning |
|---|---|
| MoreP | The target, set to the GM's position by a button. |
| Recall Home | Goes to the `MoreP` of the rune in the pack, without one home. |
| `GoHome` | Property, in `.info`: always home. Erebor set this by tag. |
| Mezi-port | Hops through 10,10 so that a client forgets the old mapplane's items. |
| ExpGate | `exp,minutes`: experience, then the minutes before this player gets more from this gate. |
| | Levels `min,max`: all of it between them. A max of 0 means no limit. |
| | Decay `low,hi`: this many levels outside the range still get a part: `(exp / (low + 1)) * (low + 1 - levels under)`. |
| Odstran kurzor | Deletes what's in the hand. |
| Odstran PVP vybavu | From the pack: containers, potions, bandages, jewelry, robes, capes, bottles. Weapons and armor stay. |
| Odstran kone | Deletes the mount. |
| Odstran bank box | Empties the bank. |
| Odstran Object menu | Lines by item, type or event. An amount of 0 takes all. "Jdi": can pass without it. "Kontrola": nothing's taken, and whoever has more than the amount is refused. |
| Pridej Object menu | Lines by item or event. "Kontrola": nothing's given, and whoever has less is refused. A time limit in seconds gives it once per player. |
| Pridej Funkci menu | Named functions run on the walker, with the gate as the first argument. A deny-capable one stops them by returning true. |
| Pridej Strazce menu | Spawn gems, picked with a target. Erebor typed their uids. |
| Plugin: Time Seal | See below. |

Functions from Erebor that work here: `f_vg_bookDeny`, `f_vg_mountDeny`, `vg_pvp_denyMagic` (no identified magic item
carried or worn, the bank aside; crafted ones and mounts may).

## Time Seal

The first to pass starts a window of `delay` seconds, then the gate is shut for `length` seconds. During the
window up to `max players` follow, 0 being no limit. Each realm has its own seal. With "Stejny Realm" on, only the
first one's realm passes until the seal ends. A button resets it.

## Differences from Erebor

- Counting by type had its two checks swapped against what the dialog says, and against counting by item. Both now
  do what the dialog says.
- A single number for exp levels shifted Erebor's arguments and gave nothing. Here it means `min` with no maximum,
  which is no limit, as a maximum of 0 always was.
- Names are checked when they're entered. Erebor left that to fail at the gate.

## Not ported

Nothing here for them to act on:

- `f_vg_finishDng`: no weekly quests.
- `f_varGate_pvp_equip`, `f_varGate_pvp_clearequip`: no PvP arena kit.
- `f_q000_vg_Cave*`: the Grandmaster quest has its own cave gates.
- The Realm Controller plugin: "not working yet" on Erebor too.
- The bosses and quests that open a temporary gate with `newitem(i_vargate)` (`e_ztracene_mesto_paladin_boss`,
  Halda Molle): those events aren't ported yet.
