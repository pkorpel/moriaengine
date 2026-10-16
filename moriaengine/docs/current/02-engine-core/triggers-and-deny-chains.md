# Triggers & deny chains

Most state-changing actions in this engine - picking something up, dropping it, equipping
it, opening a door, starting a trade - go through the same two-layer dispatch shape:

1. **`TryCancellableTrigger`** checks whether a script (a `.scp`/`.def` trigger group
   attached to the object, or dynamically via `AddTriggerGroup`) wants to intercept the
   action. Scripts can return `TriggerResult.Cancel` to stop everything that follows.
2. **A virtual `On_XxxYyy` C# method** is the compiled-script override point for the same
   event - the thing most of this session's own work (the item ATTR flag system, #217-#225)
   actually overrides, since none of that logic needs to be script-reachable.

For actions that can be *denied* rather than just observed, both layers share one more
piece: a `DenyResult` object threaded through as `args.Result`, which the whole chain
contributes to and which the caller inspects at the end - not the `TriggerResult` returned by
any individual step.

## DenyResult

`DenyResult` (`GameCore/DenyResult.cs`) is an abstract base with an `Allow` bool and a
`SendDenyMessage` method; concrete subclasses decide *how* to tell the player why something
failed:

- `DenyResult_StringSysMessage` - a plain system-message string.
- `DenyResult_ClilocSysMessage` - a client-side cliloc ID (the client renders the text itself,
  can be localized without the server knowing the wording).
- `DenyResult_Allow` / `DenyResult_NoMessage` - allowed, or denied silently.

`DenyResultMessages` (same file) is a static table of commonly-reused instances -
`Deny_ThatIsTooFarAway`, `Deny_ContainerClosed`, `Deny_ThatDoesNotBelongToYou`,
`Deny_YouCannotPickThatUp`, `Deny_ThatIsLocked`, and others. Some of these pre-date this
session with real callers already (`Deny_ContainerClosed` in `GameCore/
OpenedContainers.cs`, `Deny_ThatDoesNotBelongToYou` in `scripts/Basic/CharClasses/
Character.cs`) - but `Deny_YouCannotPickThatUp` had an enum entry and a wire-protocol mapping
table row (`GameCore/Networking/IncomingPackets.cs`) yet nothing ever actually *returned* it
anywhere, and `Deny_ThatIsLocked`'s own enum entry (`GameCore/Enums.cs`) was commented out
entirely. The item ATTR flag work (#217-#225) gave both their first real callers
(`Item.On_DenyPickup`, `scripts/Basic/ItemClasses/Item.cs`) rather than inventing new
messages - worth checking this table for an existing fit before adding a new `DenyResult`
instance.

## Three worked examples

**Pickup** (`AbstractCharacter.CanPickup` → `Trigger_DenyPickupItem`,
`GameCore/ThingManipulationTriggers.cs:1038,1048`): checks, in order, the picking
character's own `@denyPickupItem` trigger/`On_DenyPickupItem`, then the item's own
`@denyPickup` trigger/`Item.On_DenyPickup` (`scripts/Basic/ItemClasses/Item.cs:158`) - this is
where `Flag_NonMovable`, `attr_lockeddown`/`attr_secure`, and `attr_owned` all deny a pickup
- and finally, *only if the item is inside a container*, the container's own
`On_DenyPickupItemFrom` (`GameCore/ThingManipulationTriggers.cs:702`, overridden at
`scripts/Basic/ItemClasses/Item.cs:198` for `attr_secure`'s access restriction).

**Ground placement** (`Character.TryPutItemOnGround` → `On_DenyPutOnGround`,
`GameCore/ThingManipulationTriggers.cs`): the same shape, one layer shallower - the dropping
character's trigger, then the item's own `On_DenyPutOnGround`
(`scripts/Basic/ItemClasses/Item.cs:184`, where `attr_nodroptrade` denies the drop).

**Doors** (`Door.Trigger_Open`/`Trigger_Close`, `scripts/Basic/ItemClasses/Door.cs:65,92`):
the opening/closing character's `@denyOpenDoor`/`On_DenyOpenDoor`, then the door's own
`@denyOpen`/`On_DenyOpen` (`Door.cs:119`) or `@denyClose`/`On_DenyClose` (`Door.cs:122`).
*(Note: `Door.cs` has legacy non-UTF-8 byte sequences in its string literals - a plain
`grep -r` across this file silently drops matches; use `grep --binary-files=text` when
searching it, a gotcha this codebase hits repeatedly - see [Known gotchas](
../07-testing/known-gotchas.md).)*

## The gotcha: a correct allow and a denial for an unrelated reason look identical

`AbstractCharacter.CanPickup` (`GameCore/ThingManipulationTriggers.cs:1038`):

```csharp
public DenyResult CanPickup(AbstractItem item) {
    TriggerResult triggerResult;
    DenyResult denyResult = this.Trigger_DenyPickupItem(item, out triggerResult);
    if (triggerResult != TriggerResult.Cancel && (denyResult.Allow)) {
        return this.CanReach(item);
    }
    return denyResult;
}
```

`CanReach` - the distance/line-of-sight/container-open check (`GameCore/
CanSeeImplementation.cs:172`, delegating to `OpenedContainers.HasContainerOpenFromAt` for
anything inside a container) - only runs **after** the entire deny-trigger chain has already
allowed the action. This means:

- A test character standing too far from an item, or trying to take something out of a
  container it never explicitly double-clicked open, gets denied for a reason that has
  nothing to do with whatever deny-trigger logic is actually under test. The observable
  symptom - the item just doesn't move - is identical either way.
- **A `void`-returning deny method doesn't short-circuit anything by itself.**
  `On_DenyPickupItemFrom` returns `void`, not `TriggerResult` - looking at `Trigger_
  DenyPickupItem`'s own dispatch (`GameCore/ThingManipulationTriggers.cs:1048`), the
  `TriggerResult` that actually gets checked next comes only from the *trigger* call
  (`contItem.TryCancellableTrigger(TriggerKey.denyPickupItemFrom, args)`) immediately before
  it - the `On_DenyPickupItemFrom(args)` call itself can only influence anything by mutating
  `args.Result` directly. It works because `Trigger_DenyPickupItem` always returns
  `args.Result` at the end regardless of what any intermediate `TriggerResult` said, and
  `CanPickup` only calls `CanReach` if `denyResult.Allow` - so setting `args.Result` to a deny
  instance *does* correctly skip `CanReach`, it just does it through the `Result` value, not
  through `TriggerResult.Cancel`.

This combination bit two different pieces of #221/#223/#224 test-writing directly: a GM
character couldn't even *target* their own un-revealed invisible item (`HandleTarget`,
`GameCore/Networking/GameState.cs`, checks `self.CanSeeForUpdate(thing)` - which bottoms out
in the same `CanSeeVisibility` check - before invoking any target handler at all), and a
friend with real house access to a secure container still needed to explicitly open it first
before taking an item out, even though the access check itself correctly allowed it.

## The one documented exception: SecureTrade bypasses the chain entirely

`SecureTrade.AddItem` (`scripts/Basic/Trading/SecureTrade.cs:116`) calls `who.PutItemInItem(
box, 0, 0, false)` directly, with its own comment noting "no permission/reach checks - the
box has no real reachable position". This is deliberate - a trade box isn't really "in the
world" in a way `CanReach` could sensibly evaluate - but it means nothing routed through the
normal `On_DenyPutOnGround`/`On_DenyPickup` chain ever runs for an item entering a trade.
When `attr_nodroptrade` (#223) needed to block this specific path, it needed an *explicit*
check added directly inside `AddItem`, returning `true` (not `false`) to tell the caller
"handled, don't fall through to the generic container-drop path" without actually moving the
item - relying on the deny-trigger chain alone would have silently done nothing here.

## Related
- [Known gotchas](../07-testing/known-gotchas.md) - the test-writing consequences of the
  CanReach-after-deny-chain ordering, in more detail.
- [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md) - the system that gave several
  of these `DenyResultMessages` entries their first real callers.
