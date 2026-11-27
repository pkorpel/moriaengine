# Doors

Doors go by item type, as in Sphere: `t_door`, `t_door_locked`, `t_door_open`. Locking a door is a change of its type. Code:
`scripts/Basic/ItemClasses/Door.cs`, the `Door` class; locks and keys (`LockUid`, `HasKeyFor`) are in `ItemLock.cs`, as chests
and the lock spells use them too. EreborNext's `CItem.Use_Door` is the reference.

Since #1558 a door is a `Door` (an `Item`), its def a `DoorDef`: the converter gives every `t_door*` def that header, and the dupes
of their models too, so a door made from any of its models is a `Door`. A hand-written door def needs `[DoorDef ...]`. No door
code lives on `Item`, and a door has no triggers of its own: it is asked like anything else on a double click
([trigger catalog 1.10](../02-engine-core/trigger-catalog.md)).

A door is its def's model plus an offset of 0 to 15, the lowest bit being open. A swing changes the offset and moves the door to
where that model stands. `.flip` turns a door by the same offset, see [flip.md](flip.md).

- Double click or the client's OpenDoor macro swings it (`Door.On_DClick`). The macro takes the first door within 4 tiles, open
  ones too, and `t_door_open` isn't one it looks for. It is a double click with only the reach test skipped
  (`DenyClickArgs.SkipReachChecks`), so the door's denies apply to it as to a click. 0.99z8's macro bypassed the item's
  `@UserDClick`, but there a scripted door was another type, which the macro skipped; here a quest door is a door with a
  `@denyDClick` handler, so the macro has to ask it. Being a whole double click, it also ends the presser's current skill
  (a cast) and breaks their own paralysis, as any item use does here (#1558).
- An open door closes by itself after 60 seconds.
- A closed door stops walking: its tile is impassable and `IgnoreDoors` is off for players. NPCs don't open doors yet.
- `t_door_locked` wants a `t_key` in the pack with the same `LockUid`, Sphere's MORE1 (`Door.On_DenyDClick`, open or shut, as
  Sphere's `Use_Item`). A lock of 0 takes any key. A GM hears it's locked and goes through anyway. A house lock makes a door
  `t_door_locked`, and its friends and up have the key.
- A quest or other condition is a `@denyDClick` handler on the door: it refuses with its own message, or allows and cancels, which
  skips the key check (the q008 gate, `E_Q008Gate`).

A dupe of an item takes its master's type, so every model of a door is a door.

The world has no doors in the archive: Erebor's were placed live, GMs place them.

## Not ported

`t_portculis`: its macro entry and its own Z movement. Magic doors' face on a refused click.
