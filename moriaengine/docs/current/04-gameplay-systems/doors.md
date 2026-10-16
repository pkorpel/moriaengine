# Doors

`scripts/Basic/ItemClasses/Door.cs`. Related decision: #225 (`attr_opened`, closed
not-planned - see [Item ATTR flags](item-attr-flags.md)).

## State is derived, not stored

`Door.IsOpen` (`Door.cs:233-241`) is **derived directly from the door's own `Model`
value**, not backed by any separate boolean:

```csharp
private int RD {
    get { return this.Model - this.baseModel; }
}

public bool IsOpen {
    get {
        this.SetBaseDoorModelOrThrow();
        if ((this.RD % 2) == 0) {
            return false;
        }
        return true;
    }
}
```

Every real door model comes in an adjacent even/odd pair (closed/open) within one of a
fixed set of known `baseModels` (`Door.cs:25-46`, 18 entries - secret stone doors, iron
doors, wooden doors, gates, etc). `RD` (relative-to-base model offset) being even means
closed, odd means open. `SetBaseDoorModelOrThrow` (`Door.cs:266-285`) finds which base
model this door's current `Model` falls within (or throws if it matches none of the 18
known pairs - `0x190e`/`0x190f`, the one irregular 2-model bar door, is special-cased
separately).

This single fact - **no redundant state exists for door-open/closed** - is the reason
`attr_opened` was deliberately not built; see below.

## Opening and closing

`SetOpen`/`SetClose` (`Door.cs:125-165`, `170-210`) increment/decrement `Model` by 1
(moving to the adjacent model in the pair) and reposition the door's `X`/`Y` coordinates
to visually swing it open or closed, based on `RD`'s value at the time. Both are
idempotent against being called when already in that state (`if (!this.IsOpen)` /
`if (this.IsOpen)` guards).

## Deny-trigger chain

Opening/closing goes through the standard deny-trigger chain pattern (see
[Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md) for the pattern
in general) - `Door.Trigger_Open`/`Trigger_Close` (`Door.cs:65-117`) run
`tkDenyOpenDoor`/`tkDenyCloseDoor` on the user first, then `tkDenyOpen`/`tkDenyClose` on
the door itself, before calling `SetOpen`/`SetClose`. `On_DenyOpen`/`On_DenyClose`
(`Door.cs:119-123`) are the overridable hook points for content to block a specific door.

## `attr_opened` - investigated, closed not-planned (#225)

Per the issue's own instruction, this was investigated **before** any code was written.
Conclusion: `Door.IsOpen` is already a complete, working mechanism, derived directly
from `Model`. Adding `attr_opened` would mean one of two things, both worse than doing
nothing:

- A **new source of truth** - reinvents the existing model-swap logic for no benefit.
- A **"compatibility alias"** that mirrors `IsOpen` - a flag that could drift out of
  sync if `Model` is ever set through some path other than `SetOpen`/`SetClose`, making
  door state **strictly less reliable** than the existing derived property, not more.

This was a deliberate "don't build something net worse than what already exists" call,
not neglect. The `attr_opened` enum value itself stays defined (see
[Item ATTR flags](item-attr-flags.md)) for `.def`-compatibility completeness, same
precedent as `attr_norepair` - just unused.
