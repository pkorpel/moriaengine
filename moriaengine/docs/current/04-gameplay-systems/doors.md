# Doors

Doors go by item type, as in Sphere: `t_door`, `t_door_locked`, `t_door_open`. Locking a door is a change of its type. Code:
`scripts/Basic/ItemClasses/ItemDoor.cs`. EreborNext's `CItem.Use_Door` is the reference.

A door is its def's model plus an offset of 0 to 15, the lowest bit being open. A swing changes the offset and moves the door to
where that model stands. `.flip` turns a door by the same offset, see [flip.md](flip.md).

- Double click or the client's OpenDoor macro swings it. The macro takes the first door within 4 tiles, open ones too, and
  `t_door_open` isn't one it looks for.
- An open door closes by itself after 60 seconds.
- A closed door stops walking: its tile is impassable and `IgnoreDoors` is off for players. NPCs don't open doors yet.
- `t_door_locked` wants a `t_key` in the pack with the same `LockUid`, Sphere's MORE1. A lock of 0 takes any key. A GM hears it's
  locked and goes through anyway.

A dupe of an item takes its master's type, so every model of a door is a door.

The world has no doors in the archive: Erebor's were placed live, GMs place them.

## Not ported

`t_portculis`: its macro entry and its own Z movement. Magic doors' face on a refused click.
