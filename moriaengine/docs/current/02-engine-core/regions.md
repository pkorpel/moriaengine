# Regions

## Class hierarchy

- **`Region`** (`GameCore/Regions/Region.cs:27`) - abstract base. A region is a
  `PluginHolder` with one or more rectangles and a parent region (regions nest).
- **`StaticRegion`** (`GameCore/Regions/StaticRegion.cs`) - map-authored regions that never
  move. **Since #813 they come from the Erebor archive**: the converter turns
  `spheremap.scp`'s 769 `[AREA]`s into `[FlaggedRegion]` sections in
  `scripts/converted/map.def` (computing each one's parent from the rectangles), and the
  script loader loads them on every boot (`FlaggedRegion.Bootstrap` registers the section
  type; `StaticRegion.LoadFromScripts`). The world save only holds what GMs changed in-game:
  - a region created in-game is saved whole;
  - an archive region edited in-game (rects, name, spawn point, flags - `IsGmModified`) is
    saved and applied **on top of** the archive one with the same defname at the next boot
    (`ApplySavedOverride`);
  - an archive region deleted in-game is saved as a `deleted=true` tombstone, or the archive
    would bring it back.
  `IsFromScripts`/`IsGmModified` tell them apart. To change the map for good, change the
  archive fork and regenerate - like all other converted content. Until #813 the regions
  lived only in a `regions.sav` committed once at the repo's start (511 of the 769 areas,
  older names, no events/resources/weather), because the converter crashed on every
  `[AREA]` (it wrote a `DateTime`, no longer saveable) and silently emitted nothing. The
  baseline `saves/2000-01-01/00_00_00/regions.sav` is now empty.
  Parents are resolved by defname after everything is loaded (`ResolvePendingParentsAndDeletes`);
  a script reload doesn't reload regions - archive region changes need a restart.
- **`DynamicRegion`** (`GameCore/Regions/DynamicRegion.cs:23`) - runtime-placed regions:
  houses, ships, anything a player action creates. `Place(Point4D p)`
  (`DynamicRegion.cs:48-61`) registers it with the map for the first time; this is also where
  the real #90 bug lived (the original check tested whether the incoming *parameter* was
  non-null instead of whether `this.Parent` - the region's own placement state - was already
  set, so it threw on literally the first-ever placement). `this.Parent != null` is now the
  correct "already placed" guard (`DynamicRegion.cs:50-54`); placement itself goes through
  `Map.AddDynamicRegion`, which can fail (footprint collides with another dynamic region).
- **`FlaggedRegion`** (`scripts/Basic/Regions/FlaggedRegion.cs:51`) - adds a `RegionFlags`
  bitmask (`FlaggedRegion.cs:27`): `NoMagicIn`/`Out`, `NoHarmfulMagicIn`/`Out`,
  `NoBeneficialMagicIn`/`Out`, `NoTeleportingIn`/`Out`, `NoEnemyTeleportingIn`/`Out`,
  `Underground`/`Dungeon`, `NoDecay`, `Safe`, `Guarded`, `NoPvp`, `Arena`. Flags are
  inherited from the parent region chain (`HasInheritedFlag`, `FlaggedRegion.cs`) unless a
  child region explicitly sets its own bit.
  From the archive it also keeps, as plain data (#813): `RegionEvents` (Sphere `EVENTS=`,
  e.g. r_nosumon, r_playerhouse), `RegionResources` (`RESOURCES=` region types, e.g.
  r_default_rock, r_doly), `RainChance`/`ColdChance` (no weather system yet) and `Group`
  ("Doly", "Mesta Gondor", ...). `FLAGS` is read in Sphere's hex form (`00812`).

## Lookup priority: dynamic regions always win

`Sector.GetRegionFor(x, y)` (`GameCore/Regions/Sector.cs:287-301`) checks every dynamic
region rectangle registered in that sector **first**, and only falls back to the sector's
static rectangles (checked back-to-front, `i = rectangles.Length - 1 downto 0`) if nothing
dynamic matched:

```csharp
public Region GetRegionFor(int x, int y) {
    if (this.dynamicRegionRects != null) {
        foreach (RegionRectangle dynamicRect in this.dynamicRegionRects) {
            if (dynamicRect.Contains(x, y)) {
                return dynamicRect.region;
            }
        }
    }
    for (int i = this.rectangles.Length - 1; i >= 0; i--) {
        ...
    }
    return StaticRegion.WorldRegion;
}
```

This is why `House.FindHouseAt(point)` (added for the house-lockdown feature) reliably finds
a house's own region regardless of what static region (town, wilderness, dungeon) happens to
surround it - a house's `MultiRegion` is a `DynamicRegion`, so it's always checked ahead of
whatever static region the house sits inside.

## MultiItem / MultiRegion: how a placed house gets its own region

A `MultiItemDef` (base for `HouseDef` and ship defs) declares one or more `MULTIREGION`
rectangles in its `.def` content, relative to the multi's own anchor point. When an instance
is created on the ground, `MultiItem.InitMultiRegion()`
(`scripts/Basic/ItemClasses/MultiItem.cs:255`) builds a real `MultiRegion` (a
`DynamicRegion` subclass) from those rectangles and calls `Place` on it.

```csharp
public bool IsRegionPlaced { get; private set; }
```

(`MultiItem.cs:250`) is `true` only once that placement actually succeeded. `Region != null`
alone isn't a safe placement check - a failed placement (footprint collided with another
multi's region) leaves no region at all, but code that only checks for a non-null region
could still be fooled by other states. Anything that cares whether a house/ship deed
actually placed should check `IsRegionPlaced`, not just `Region`.

## `flag_nodecay`: an existing flag that had no way to read it back

`FlaggedRegion.LoadLine`'s `"flag_nodecay"` case (`FlaggedRegion.cs`) has always parsed that
`.def`/save-file keyword into the raw `flags` bitmask at bit `0x1000`
(`RegionFlags.NoDecay`, `FlaggedRegion.cs:42`) - but until the item-decay system was built
out, there was no real property to read that bit back out, so nothing could ever act on it.
`Flag_NoDecay` (`FlaggedRegion.cs:133-135`) is the property that finally exposes it:

```csharp
public bool Flag_NoDecay {
    get { return this.HasInheritedFlag(RegionFlags.NoDecay); }
}
```

This is worth remembering as a general pattern in this codebase: a `flag_xxx` keyword being
parsed by `LoadLine` into the generic bitmask is **not** proof that anything can actually
read it - check for a real property before assuming a flag needs new storage added.

A concrete example of this paying off: every one of the 256 real player-house regions on
this map (`a_dum_1` through roughly `a_dum_258`, named `"dum 1"` .. `"dum N"` in the
committed `saves/2000-01-01/00_00_00/regions.sav` baseline) already had the `NoDecay` bit
set, carried over faithfully from the original Sphere conversion - confirmed by a direct
bitwise check (`flags & 0x1000`) across every single one of them. The data had been correct
the whole time; the code just couldn't read it until `Flag_NoDecay` existed. No house-region
content needed to change at all once the item-decay system could finally consult the flag -
see [item decay](../04-gameplay-systems/item-decay.md).
