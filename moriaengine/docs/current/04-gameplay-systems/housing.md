# Housing

Tracking issue: #74 (part of the #67 verification pass). Pre-existing system, built
before the [House lockdown & security](house-lockdown.md) layer that sits on top of it
(#221).

## `House`/`HouseDef`

`scripts/Basic/ItemClasses/Housing/House.cs`. `House` is a `MultiItem` subclass,
`HouseDef` a `MultiItemDef` subclass - built directly on top of `MultiItem`/
`MultiRegion`, which already existed and already worked (ships use the same base). This
system adds ownership and the security-list data model on top; it didn't need to build
multi-tile-structure placement/region machinery from scratch.

Ownership and the three lists are tag-backed (`House.cs:56-59`), not `[SaveableData]`
fields:

```csharp
private static readonly TagKey ownerTK = TagKey.Acquire("_houseOwner_");
private static readonly TagKey coOwnersTK = TagKey.Acquire("_houseCoOwners_");
private static readonly TagKey friendsTK = TagKey.Acquire("_houseFriends_");
private static readonly TagKey bannedTK = TagKey.Acquire("_houseBanned_");
```

Deliberate choice, not a stylistic one: `Item`/`Thing`-family classes serialize via
their own hand-rolled Save/LoadLine chain rather than generic attribute-reflection (see
[Persistence & saving](../02-engine-core/persistence-and-saving.md)), so a raw
`[SaveableData]` field here would silently never be written. Tags are the correct
per-instance-data idiom for this class family - see
[Tags & timers](../02-engine-core/tags-and-timers.md).

`House.HasAccess(Character ch)` (`House.cs:89-112`): owner, co-owners, and friends can
all access; the ban list explicitly overrides even ownership-adjacent access (checked
first); GMs always pass regardless of any list.

## Placement flow

`HouseDeed`/`HouseDeedDef` (`scripts/Basic/ItemClasses/Housing/HouseDeed.cs`) link a deed
item to a specific `HouseDef` via a tag-backed `HouseDef` property, set from a new
`.def` line, `housedef = <defname>`. The original converted content already had house
deeds (`scripts/converted/item_deed.def`, all commented out) that referenced the target
house via a `RESOURCES` line shared with crafting's ingredient-list syntax - that's
consumption-list semantics (check/take items from a backpack), not what a deed needs, so
this uses the dedicated `housedef` property instead when restoring that content.

Flow: double-click the deed (`t_deed_house.On_DClick`) → `player.Target(Targ_PlaceHouse,
...)` → target the ground → `Targ_PlaceHouse.On_TargonGround` creates the `House`, then
checks `house.IsRegionPlaced`:

```csharp
House house = (House) houseDef.Create(targetted.X, targetted.Y, targetted.Z, self.M);
if (!house.IsRegionPlaced) {
    //MultiItem.InitMultiRegion (ran during Create, via MultiItemDef.On_Create) already did the
    //real footprint-overlap check against every other multi's region
    house.Delete();
    self.SysMessage("There is not enough room for a house there.");
    return TargetResult.RestartTargetting;
}
```

The real footprint-overlap rejection happens inside `MultiItem.InitMultiRegion`
(already ran during `Create`, via `MultiItemDef.On_Create`) - `IsRegionPlaced` is just
the after-the-fact signal that it succeeded. This is what actually catches "there's
already a house/ship here," not a separate single-point check.

## Release

`.releasehouse` (`HouseCommands.ReleaseHouse`, `HouseDeed.cs:128-153`) requires the
caller to be standing **inside** the house's own `MultiRegion` specifically
(`self.Region as MultiRegion`), not just "nearby" - `FindOwnedHouseNearby` reads
`self.Region` directly rather than searching a radius.

## Historical bug: #90

`DynamicRegion.Place` checked whether its own **parameter** was non-null instead of
whether the region's own `Parent` was already set - this broke house placement
completely on the very first attempt ever made against a fresh region. Fixed; see
[Regions](../02-engine-core/regions.md) for `DynamicRegion` in general.

## Deliberately out of scope

Flagged in code, not silently dropped:
- A friend/co-owner/ban **list-editing UI** - the data model (`AddCoOwner`/`AddFriend`/
  `AddBanned`) is ready for one, no dialog exists yet.
- Decay (idle-time house destruction).
- Interior customization/redecoration.

See [House lockdown & security](house-lockdown.md) for the `attr_lockeddown`/
`attr_secure` layer built on top of this system.
