# Item double-click & the "use" action framework

Tracking issue: #370. How double-clicking an item (and `.usehand`, which reuses the exact
same dispatch) decides what happens, and the generic mechanism that lets any equippable
tool opt into "equip, then immediately do something" with one `.def` line.

## Two independent dispatch paths exist, and both already fire on every dclick

`Thing.Trigger_DClick` (`GameCore/Thing.cs`) is the one entry point - wired to the real
client 0x06 double-click packet and to `Player.UseHand()` (`.usehand`, which looks up
`Hand1`/falls back to `Hand2` and calls this same trigger). It does two genuinely
separate things, in order, on every call:

1. Fires the item's own `.def` `TYPE=` trigger-group `@dclick` handler, if any -
   `Lockpicking`'s `t_lockpick : CompiledTriggerGroup` (`On_DClick(Item self, Character
   clicker)`) is the one real example, calling `clicker.SelectSkill(...)` directly. This
   fires **unconditionally**, regardless of whether the item is equipped.
2. Calls the class-level virtual `Item.On_DClick(AbstractCharacter dclicker)` - the one
   `Equippable` overrides (see below).

Confirmed by direct reading of `Trigger_DClick` before #370: both already fired on every
double-click, independent of equip state - the `TYPE=` path was never the gap.

## The real gap `#370` fixed: `Equippable.On_DClick` never did anything beyond equipping

Before #370, `Equippable.On_DClick` (inherited by every weapon/armor/tool in the game)
only ever equipped the item if it wasn't already equipped - nothing happened at all once
equipped, and nothing happened on the *first* dclick either beyond the equip step itself.
A second dclick on an already-worn tool was a silent no-op - the literal reported symptom
(".usehand"/dclick on an in-hand tool does nothing).

Fixed shape (`scripts/Basic/ItemClasses/Equippable.cs`):

```csharp
public override void On_DClick(AbstractCharacter from) {
    if (this.IsEquippable) {
        bool alreadyEquipped = (this.Cont == from) && (this.Z == this.Layer);
        if (!alreadyEquipped) {
            // ...existing equip logic, unchanged...
            if (!dr.Allow) { dr.SendDenyMessage(from); return; }
        }
        this.OnUse(from);
    }
}
```

`OnUse` always runs once the item is confirmed equipped - whether this call just equipped
it, or it was already worn. Equip-failure still short-circuits (nothing to "use" if it
never made it into the player's hand).

## `OnUse` is data-driven off the def's skill, not a new class per tool

`EquippableDef.UseSkill` is the only thing a tool needs to opt into "equip-and-use". It is
the def's `associatedSkill` when a `.def` line sets one (`associatedskill = <SkillName>`),
else derived (#392): an axe's `WeaponType` gives Lumberjacking, `tooltype = Pickaxe` or
`FishingPole` gives Mining or Fishing. `associatedSkill` and `toolType` are typed `Def:`
fields in `Equippable.ct` since #1384 (def tags with hand-written parsing before):

```csharp
public virtual void OnUse(AbstractCharacter from) {
    SkillName skill = this.TypeDef.UseSkill;
    if (skill != SkillName.None) {
        Character clicker = from as Character;
        if (clicker != null) {
            clicker.SelectSkill(SkillSequenceArgs.Acquire(clicker, skill, this));
        }
    }
}
```

This reuses the exact same `SelectSkill`/`SkillSequenceArgs` machinery the client's own
skill-list "use skill" packet already calls (`IncomingPackets.cs`'s
`RequestSkillEtcUseInPacket.Handle`) - dclicking a tool is just a second, convenience
entry point into a mechanism that already fully works, not a separate skill-start path.
`virtual` so a specific item class can still override `OnUse` directly for a richer
"use" action later (e.g. a real target-based chop-tree/cut-cloth flow) without being
forced through the generic skill-select shape.

**Why not reuse the `TYPE=` trigger-group mechanism from part 1 instead of adding a new
field?** Checked real weapon content before deciding: a converted weapon's own `TYPE=`
value is almost always `//guessed by Converter` (confirmed looking at `i_dagger_copper`,
`#157`'s own tracked data-quality gap) and the trigger-group classes several of those
guessed types point to (e.g. `t_weapon_fence`) are themselves completely empty stubs
(`scripts/Basic/BasicTypes/type_by_profession.cs` has ~40 of these). Reusing that field
for a new purpose would mean the dispatch key is unreliable/shared across many
non-matching items. The def's skill is new, clean, per-def data instead.

## Shipped tools (#370, #392): pickaxe, axes, fishing pole

#370 added `associatedskill =` lines by hand; since #392 the tools get their skill from
type-level data and no shipped content has an `associatedskill =` line:

- The pickaxe (`[WeaponDef 0xe85]`, `scripts/converted/item_tinker.def`) - a weapon (the
  archive's `TYPE = t_weapon_bow_run` makes it compute as a bow) with the converter's
  `tooltype = Pickaxe`, so Mining.
- Every axe (`i_hatchet` and the rest) - `WeaponType` `OneHandAxe`/`TwoHandAxe`, so
  Lumberjacking.
- The fishing pole (`[EquippableDef 0xdbf]`, `scripts/converted/item_nabytek.def`) - the
  converter makes `T_FISH_POLE` an equippable `EquippableDef` with
  `tooltype = FishingPole`, so Fishing.

**Mandatory tool, a moriaengine-only design decision, not ported from Erebor**: Mining/
Lumberjacking/Fishing's `On_Select` now deny (with a real system message) unless the
matching tool is equipped in either hand - `Equippable.IsHoldingToolFor(Character,
SkillName)` checks both `LayerNames.Hand1`/`Hand2` for an `Equippable` whose def's
`UseSkill` matches. Real Erebor's `miningfix.scp` has no such check at all
(confirmed via source read) - user explicitly confirmed wanting this gate anyway,
applied consistently to all three gathering tools rather than just Mining.

## Explicitly out of scope, flagged not guessed at

- "Cut cloth with a knife/sword" and a staff's own "use" action - no existing mechanic
  anywhere (not Tailoring, not any skill) to hook `OnUse` into; would need real design
  work, not just wiring, if picked up later.
- The ~40 empty tool/material `TYPE=` stub classes (`t_cloth_bolt`, `t_shovel`,
  `t_dye_vat`, etc.) are a separate, much larger content-authoring gap (#373-adjacent
  severity, same class as `#12`/`#157`) - not touched here.
- Lumberjacking/Fishing's own tool requirement could in principle extend to other
  gathering-adjacent skills later - only these three were in scope for #370.

## Test coverage

`Tests/Protocol/GatheringTests.cs`: dclick a pickaxe sitting in the backpack equips it
and starts Mining in one step (end-to-end through to real ore in the backpack); dclick
an already-equipped pickaxe also starts Mining (the originally-reported no-op case);
Mining/Lumberjacking/Fishing all deny with the real system message when the matching
tool isn't equipped, and proceed when it is.
