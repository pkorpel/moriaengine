# Flip

`.flip` is Sphere's FLIP, by target cursor. Scripts have it as `item.flip`. Code: `Item.Flip` in
`scripts/Basic/ItemClasses/ItemFlip.cs`, `Corpse.Flip`. EreborNext's `CItem.Flip` and its checks against the real
0.99z8 are the reference, and `FlipTests` expects the numbers those gave.

| Target | What happens |
|---|---|
| Lit thing (`t_light_lit`, `t_fire`, `t_campfire`, `t_lava`, `t_window`, `t_spell`) | Next of the 56 light patterns, after the last the first. The model stays. |
| Door (`t_door`, `t_door_locked`, `t_door_open`) | Next by its offset from the base model. `.flip "ALIGN"`, `"HAND"`, `"SWING"` flip one bit of the offset instead. |
| Corpse with the corpse model `0x2006` | Turns one step. A corpse of another look goes by its flip group. |
| Character | Turns one step clockwise. Words are ignored. |
| Anything else | Next model of its flip group, after the last the base model. |

A GM's word is LScript, so it's quoted.

## Flip group

An itemdef's models, as Sphere's `m_flip_id`: `DupeList` in its order, then each def that names this one in `DUPEITEM`,
in the order they loaded. A model that's neither goes to the base.

## Dropping

An item dropped on the ground takes the next model of its group, whatever kind of item it is, so a door here doesn't
go by the door rules. Not a stack, and not a fixed or invisible item. `flipDroppedItems` in `steamengine.ini` is Sphere's
`FlipDroppedItems`, on by default. With it off, only items with `FLIP=1` turn.

## Not as in Sphere

`.flip` asks for a target, as #193 asked. In Sphere `.flip` runs on yourself and `.xflip` asks. Here `.x flip` runs it on
a target too, for items.
