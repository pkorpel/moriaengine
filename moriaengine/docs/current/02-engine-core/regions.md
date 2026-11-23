# Regions

Regions combine spatial rectangles, a parent hierarchy and trigger behaviour. They are not
just labels: placement, movement and gameplay flags consult them.

## Types and ownership

| Type | Purpose |
|---|---|
| `Region` | `PluginHolder` base with rectangles and a parent. |
| `StaticRegion` | Script-authored or GM-created stationary areas. |
| `DynamicRegion` | Runtime placement, such as a house or ship footprint. |
| `FlaggedRegion` | Static regions with flags and converted area metadata. |
| `MultiRegion` | Dynamic region associated with a multi item. |

## Static regions and saves

The converter generates `[FlaggedRegion]` sections from the archive's map areas. Script
loading establishes the baseline; saves retain GM changes:

- GM-created regions are saved in full.
- Modified script regions are saved as overrides applied to the same defname at load.
- Deleted script regions leave `deleted=true` tombstones to prevent resurrection.

`IsFromScripts` and `IsGmModified` distinguish those cases. Parent resolution and deletions
finish after definitions are available. Script-authored region changes require restart;
ordinary script resync does not reload them. Change converted source through the archive and
converter workflow, not by hand-editing generated `.def` files.

## Lookup and dynamic placement

`Sector.GetRegionFor` checks dynamic rectangles before static rectangles. Static rectangles
are checked back-to-front, falling back to `StaticRegion.WorldRegion`. A house can therefore
supply the region at a point inside a larger static town.

`DynamicRegion.Place` rejects already-placed regions and registers the footprint with the
map. Placement can fail on overlap. `MultiItem.InitMultiRegion` creates the footprint from
the def's relative `MULTIREGION` rectangles. Use `MultiItem.IsRegionPlaced` to determine
whether placement succeeded; merely having a region reference is insufficient.

## Flags and inherited policy

`FlaggedRegion` stores magic/teleport restrictions, safe/guarded/no-PvP, dungeon/underground,
no-decay and related flags. Properties such as `Flag_NoDecay` use `HasInheritedFlag` to
consult the parent chain. An absent bit is not an explicit false override of an ancestor's
true bit; inspect the actual property before assuming override semantics.

Converted events, resources, weather chances and grouping are data. A parsed keyword does
not prove gameplay consumes it. Check the corresponding property and caller before adding
new storage or assuming an archive feature is implemented.

## Movement hooks

Checked transitions ask departing and arriving region branches for permission; forced
relocation uses notification paths. Do not describe both as one unconditional sequence of
deny checks followed by duplicate notifications. See [legacy triggers](trigger-catalog.md)
and [movement](../04-gameplay-systems/movement-and-collision.md).

## Sources

[Region](../../../GameCore/Regions/Region.cs),
[StaticRegion](../../../GameCore/Regions/StaticRegion.cs),
[DynamicRegion](../../../GameCore/Regions/DynamicRegion.cs),
[Sector](../../../GameCore/Regions/Sector.cs),
[FlaggedRegion](../../../scripts/Basic/Regions/FlaggedRegion.cs),
[MultiItem](../../../scripts/Basic/ItemClasses/MultiItem.cs).
