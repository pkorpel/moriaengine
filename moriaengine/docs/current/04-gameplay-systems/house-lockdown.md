# House lockdown & security

Tracking issue: #221 (part of #215). Built entirely on top of [Housing](housing.md)'s
`House` system (#74) and [Item ATTR flags](item-attr-flags.md)'s `attr_lockeddown`/
`attr_secure`.

## Finding the containing house

`House.FindHouseAt(IPoint4D point)` (`House.cs:141-147`):

```csharp
public static House FindHouseAt(IPoint4D point) {
    MultiRegion multiRegion = point.GetMap().GetRegionFor(point) as MultiRegion;
    if (multiRegion != null) {
        return multiRegion.multiItem as House;
    }
    return null;
}
```

This relies on `Sector.GetRegionFor` always checking dynamic regions (which includes
every `MultiRegion`, a house's own region) before static ones - see
[Regions](../02-engine-core/regions.md) - so it reliably finds the containing house
regardless of what static region (town, wilderness) surrounds it.

## Who can move locked items

`House.CanMoveLockedItems(Character ch)` (`House.cs:153-166`) is owner/co-owner/GM only -
**deliberately narrower** than `HasAccess`. Friends can use a house but, per real UO's
own lockdown rules, cannot move another player's locked-down belongings; only the owner
and co-owners can.

## Two hooks, two different strengths

`attr_lockeddown` and `attr_secure` are both ATTR flags on the item itself (see
[Item ATTR flags](item-attr-flags.md)), but they wire into two different deny hooks with
genuinely different scope:

**`Item.On_DenyPickup`** (`Item.cs:158-178`, extended from #220's `Flag_NonMovable`
check) - denies pickup when **either** `attr_lockeddown` or `attr_secure` is set, unless
the picker passes `CanMoveLockedItems`. Both flags are treated identically for this
half - it's the "can't be moved" restriction.

```csharp
if (this.HasAttribute(ItemAttributes.attr_lockeddown) || this.HasAttribute(ItemAttributes.attr_secure)) {
    House house = House.FindHouseAt(this);
    if ((house != null) && !house.CanMoveLockedItems(args.PickingChar as Character)) {
        args.Result = DenyResultMessages.Deny_ThatIsLocked;
        return TriggerResult.Cancel;
    }
}
```

**`Item.On_DenyPickupItemFrom`** (`Item.cs:198-207`) - `attr_secure` **only**. This is
the stronger "restricts access too, not just movement" half: called on the container
when someone tries to take an item **out** of it, gated to `House.HasAccess` (owner,
co-owners, friends - not the narrower `CanMoveLockedItems`). `attr_lockeddown` alone
never touches this hook at all, so it correctly leaves viewing/access completely
unrestricted - matching the real UO distinction between the two flags (lockdown
prevents the item itself moving; secure additionally restricts who can get at what's
inside it, which only matters if the item is a container).

## Player-facing commands

`.LockDown`/`.Secure` (`HouseCommands`, `HouseDeed.cs:155-171`) are player-facing, not
GM-only - a deliberate choice matching real UO, where any resident can lock down their
own belongings. Both are target-cursor commands (see
[GM commands](../03-admin-tooling/gm-commands.md) for the target-cursor command shape in
general) that toggle the respective flag via `SetAttribute`, gated by
`CanMoveLockedItems`:

- `.LockDown` → `Targ_LockDown.On_TargonThing` - any item.
- `.Secure` → `Targ_Secure.On_TargonThing` - requires `item.IsContainer`; there's
  nothing for "restrict access" to mean on a non-container.

Both also remain reachable via `D_AttrList`'s GM-only checkbox dialog (see
[Item ATTR flags](item-attr-flags.md)), since it's the same underlying `SetAttribute`
API either way.

## Deliberate scope limit

A secure container's own open/double-click action is **not** restricted - only taking
items **out** of it once already open. This is a reasonable, defensible reading of
"restricts access" without touching the separate container-open trigger chain; flagged
here as a scope choice, not an oversight, in case stricter scope is wanted later.

## Test-authoring gotcha found while verifying this

A correctly-**allowed** pickup and a pickup denied for an unrelated reason (too far
away, container not open) produce the **identical** "nothing happened" observable
symptom from outside - `CanReach` runs strictly after the full deny-trigger chain
passes. See [Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md) for
the general mechanism. Two concrete instances hit while building this feature's tests:

1. Every fresh test character spawns at its own far-away default position - fixed by
   teleporting to the house before any pickup attempt.
2. Taking an item out of a container (even one the picker legitimately has access to)
   requires that connection to have already explicitly double-clicked/opened the
   container first (`OpenedContainers.HasContainerOpenFromAt`) - unlike a deny-chain
   `Result`, a void `On_DenyPickupItemFrom` call doesn't itself short-circuit the
   container-open check for an **allowed** picker, only for an actually-denied one.

Also see 08-history/known-gaps-and-tech-debt.md for issue #230 - a pre-existing,
unrelated regression in `HousingTests.cs`'s own hardcoded placement offsets found (not
caused) while investigating this.
