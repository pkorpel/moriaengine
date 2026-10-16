# Corpses & death

`scripts/Basic/ItemClasses/Corpse.cs`. Related: #218 (death/corpse-drop ATTR flags,
part of #215).

## Corpse creation: `InitFromChar`

`Corpse`/`CorpseDef` is a `Container`/`ContainerDef` subclass. `InitFromChar(Character
dieingChar)` (`Corpse.cs:74-117`) is the real creation/population flow, called once per
death:

```csharp
public void InitFromChar(Character dieingChar) {
    this.charDef = dieingChar.Def as CharacterDef;
    this.Amount = dieingChar.Model;
    this.Direction = dieingChar.Direction;
    this.Color = dieingChar.Color;
    string name = dieingChar.Name;
    this.Name = string.Concat("a corpse of ", name);
    if (dieingChar.IsPlayer) {
        this.owner = dieingChar;
        this.ownerName = name;
    }
    Item hair = dieingChar.Hair;
    if (hair != null) {
        this.hairModel = hair.ShortModel;
        this.hairColor = hair.ShortColor;
    }
    Item beard = dieingChar.Beard;
    if (beard != null) {
        this.beardModel = beard.ShortModel;
        this.beardColor = beard.ShortColor;
    }
    AbstractItem pack = dieingChar.Backpack;
    foreach (Item inBackpack in pack) {
        if (inBackpack.CanFallToCorpse) {
            inBackpack.Cont = this;
        }
    }
    foreach (Item equipped in dieingChar.VisibleEquip) {
        if (equipped.CanFallToCorpse && equipped != pack && equipped != hair && equipped != beard) {
            equipped.Cont = this;
            if (this.equippedItems == null) {
                this.equippedItems = new Dictionary<ICorpseEquipInfo, AbstractItem>();
            }
            this.equippedItems[equipped] = equipped;
        }
    }
    ItemDecaySystem.ScheduleOrCancel(this);
}
```

Copies the dying character's model/direction/color (so the corpse visually resembles
them), hair/beard model+color, then moves every backpack item and visible-equipment item
that passes `CanFallToCorpse` onto the corpse.

### `Owner`/`OwnerName` - the player/NPC discriminator

`this.owner`/`this.ownerName` are set **only** when `dieingChar.IsPlayer` is true - a
plain character's corpse (`Corpse.Owner`, `Corpse.cs:342-349`) stays `null`. This single
fact is the exact signal [Item decay](item-decay.md) uses to choose between
`playerCorpseDecayMinutes` and `npcCorpseDecayMinutes` - there's no separate "is this a
player corpse" flag, `Owner != null` **is** the check.

`Link` (`Corpse.cs:333-340`) returns `this.owner`, so `.find`-style owner lookups work
on a corpse the same way they do on any other linked `Thing`.

### Decay re-evaluation ordering

The call to `ItemDecaySystem.ScheduleOrCancel(this)` at the end of `InitFromChar` exists
because `On_EnterRegion` already fired once during this corpse's own `Create()` call -
**before** `InitFromChar` ever ran, so before `this.owner` was set above. That first,
earlier call scheduled an NPC-rate timer by default (since `Owner` was still null at
that point); this call corrects it to the player rate once ownership is actually known.
See [Item decay](item-decay.md) for the general On_Create/On_EnterRegion ordering issue
this is an instance of.

## Death: `Character.CauseDeath`

`CauseDeath` is the real death trigger - registers the murder with the notoriety system
(see [Notoriety & murder counts](notoriety.md) rather than re-explaining
`RegisterMurder` here), plays the death sound, creates and populates the corpse, then
branches: a player becomes a ghost, an NPC gets deleted.

**No-op once already dead**: a second `CauseDeath` call on an already-`Flag_Dead`
character does nothing. This is working as designed, but it's a real test-authoring
trap - a test that kills the same character twice without resurrecting in between will
find its second kill silently does nothing, which can make an unrelated assertion
"pass" for the wrong reason. See 07-testing/known-gotchas.md.

## Item-level death rules: `CanFallToCorpse`

`Item.CanFallToCorpse` (`Item.cs:542-556`) is the per-item gate `InitFromChar` checks
before moving an item onto the corpse - this is where `attr_newbie`/`attr_blessed`/
`attr_cursed`/`attr_insured` (see [Item ATTR flags](item-attr-flags.md) for the flags
themselves) actually get enforced:

```csharp
public virtual bool CanFallToCorpse {
    get {
        if (this.HasAttribute(ItemAttributes.attr_cursed)) {
            return true; //cursed always drops, overrides newbie/blessed/insured
        }
        if (this.HasAttribute(ItemAttributes.attr_newbie) || this.HasAttribute(ItemAttributes.attr_blessed)) {
            return false;
        }
        if (this.HasAttribute(ItemAttributes.attr_insured)) {
            this.SetAttribute(ItemAttributes.attr_insured, false); //consumed - protects once
            return false;
        }
        return true;
    }
}
```

`attr_newbie`/`attr_blessed` keep an item on the body permanently (every death).
`attr_cursed` forces a fall and overrides both of those if somehow combined on the same
item. `attr_insured` protects **exactly once** - the flag is cleared inside this same
getter the moment it's consumed. This mutation-inside-a-getter is safe specifically
because `Corpse.InitFromChar`'s death-handling pass is the **only** caller (confirmed by
grep) - nothing else reads `CanFallToCorpse` in a context where consuming the flag as a
side effect would be surprising.

## Resurrection: `ReturnStuffToChar`

`ReturnStuffToChar(Character resurrectedChar)` (`Corpse.cs:119-131`) is the
resurrection-side counterpart to `InitFromChar`:

```csharp
public void ReturnStuffToChar(Character resurrectedChar) {
    if (this.equippedItems != null) {
        foreach (Item i in this.equippedItems.Values) {
            i.Cont = resurrectedChar;
        }
    }
    AbstractItem backpack = resurrectedChar.Backpack;
    foreach (Item i in this) {
        i.Cont = backpack;
    }
    this.Delete();
}
```

Equipped items go back onto the resurrected character directly (not through a real
equip-trigger pass - marked `//TODO pickup/dropon backpack triggers...?` in the source);
everything else still sitting in the corpse goes into the resurrected character's
backpack. The corpse is deleted once emptied.
