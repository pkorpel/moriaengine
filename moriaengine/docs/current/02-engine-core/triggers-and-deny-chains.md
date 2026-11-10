# Triggers & deny chains

Most actions a character takes on a thing - picking it up, dropping it, equipping it,
starting a trade - first ask whether it's allowed, through a chain of deny triggers. Each
step of the chain is the same two-layer dispatch:

1. **`TryCancellableTrigger`** runs the thing's trigger groups and plugins, in the order
   given in [plugins.md](plugins.md#how-triggers-reach-a-plugin) (registered groups, the
   thing's `EVENTS`, its plugins, its def's `on=@x` lines and `TEVENTS`, an item's `TYPE`).
   Returning `1` (`TriggerResult.Cancel`) ends the chain.
2. **A virtual `On_XxxYyy` C# method** on the thing's class, the compiled override point for
   the same event.

The steps share one `args` object carrying a `DenyResult` (`args.Result`, allowed to begin
with). A step denies by setting `args.Result`, and usually cancels as well; the caller reads
`args.Result` at the end.

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

## Two worked examples

**Pickup** (`AbstractCharacter.CanPickup` -> `Trigger_DenyPickupItem`,
`GameCore/ThingManipulationTriggers.cs:1055,1065`), in order: the picking character
(`@denyPickupItem`, `On_DenyPickupItem`), the item (`@denyPickup`, `Item.On_DenyPickup`: a
house lock, `Flag_NonMovable`, the `attr_` flags), then where the item is: its container
(`@denyPickupItemFrom`, `Item.On_DenyPickupItemFrom`: `attr_secure`'s access, a trade box's
owner, corpse looting rules, a container locked against the taker), the character holding
it (`@denyPickupItemFrom`, `On_DenyPickupItemFrom`), or, on the ground, its region.

**Ground placement** (`AbstractCharacter.TryPutItemOnGround`,
`ThingManipulationTriggers.cs:1287`): the dropping character, the item
(`Item.On_DenyPutOnGround`, where `attr_nodroptrade` denies), then the region
(`Region.Trigger_DenyPutItemOn`); the default reach check there is `CanReachCoordinates`.

## Permission first, then reach

```csharp
public DenyResult CanPickup(AbstractItem item) {
	TriggerResult triggerResult;
	DenyResult denyResult = this.Trigger_DenyPickupItem(item, out triggerResult);
	//default implementation, can be skipped by returning true (cancelling)
	if (triggerResult != TriggerResult.Cancel && (denyResult.Allow)) {
		return this.CanReach(item);
	}
	return denyResult;
}
```

The deny chain decides whether this character may take this item at all: the rules of
ownership, locks, flags and scripts. `CanReach` (`GameCore/CanSeeImplementation.cs`) then
decides whether they can from where they are: whether they can see it, distance, line of
sight, and, for an item in a container, that they have that container open (`OpenedContainers.HasContainerOpenFromAt`), as
a client can only take from a container it has opened.

The reach check is the default, which the chain can replace, as the original comment says:
a step that cancels while leaving `args.Result` allowed lets the action through without
`CanReach`, and without the later steps of the chain (a `@denyPickupItem` that cancels skips
the item's locks and the container's rules too). The steps whose cancel counts are the
character's `@denyPickupItem`/`On_DenyPickupItem`, the item's `@denyPickup`/`On_DenyPickup`
and the container's or holder's `@denyPickupItemFrom` trigger; `On_DenyPickupItemFrom` and
the region's step can only deny, through `args.Result`. A step that denies is answered with
its own message, wherever the character stands.

A test of a deny rule therefore checks both sides from the same spot: the action refused for
the character the rule refuses, and allowed for one it allows. The allowed case proves the
character is in reach with the container open, so the refusal is the rule's.

## The one documented exception: SecureTrade bypasses the chain entirely

`SecureTrade.AddItem` (`scripts/Basic/Trading/SecureTrade.cs`) calls `who.PutItemInItem(
box, 0, 0, false)` directly - "no permission/reach checks - we've already established who is a
legitimate trade party". This is deliberate (the trade rules - reach, alive, online - are
checked when the trade starts, see [Trading & secure trade](../04-gameplay-systems/trading-secure-trade.md)),
but it means nothing routed through the normal `On_DenyPutOnGround`/`On_DenyPickup` chain ever
runs for an item entering a trade. When `attr_nodroptrade` (#223) needed to block this specific
path, it needed an *explicit* check inside `AddItem`, which returns a deny result so the caller
bounces the item back - relying on the deny-trigger chain alone would have silently done
nothing here. (Taking items OUT of a trade box does go through the chain:
`Item.On_DenyPickupItemFrom` lets only the box's owner do it, #810.)

## Related
- [Known gotchas](../07-testing/known-gotchas.md) - testing a refusal next to an allowed case.
- [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md) - the system that gave several
  of these `DenyResultMessages` entries their first real callers.
