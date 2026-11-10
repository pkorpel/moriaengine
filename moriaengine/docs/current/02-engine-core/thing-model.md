# The Thing model

Everything that can exist in the game world - items, characters, corpses, houses, doors,
spawn gems - descends from one root class: `Thing` (`GameCore/Thing.cs`).

## The class hierarchy

```
Thing (GameCore/Thing.cs)
├── AbstractItem (GameCore/AbstractItem.cs)
│     └── Item (scripts/Basic/ItemClasses/Item.cs)     <- what real content extends
│           ├── Container, Door, Corpse, MultiItem (→ House, Ship), ...
└── AbstractCharacter (GameCore/AbstractCharacter.cs)
      └── Character (scripts/Basic/CharClasses/Character.cs)
            ├── Player (scripts/Basic/CharClasses/Player.cs)
            └── NPC (scripts/Basic/CharClasses/NPC.cs)
```

The split between `AbstractItem`/`AbstractCharacter` and `Item`/`Character` is deliberate:
`AbstractItem`/`AbstractCharacter` live in `GameCore` and define the engine-level contract
(triggers, persistence plumbing, networking hooks) that has to exist regardless of what game
content looks like. `Item`/`Character` live in `scripts/` (the compiled-separately "scripts"
assembly) and are what actual game classes extend - this is also the layer the `.ct` template
pipeline generates code against (see [scripting-pipeline.md](scripting-pipeline.md)). A
script author never touches `GameCore` directly; everything they write is a subclass (direct
or indirect) of `Item` or `Character`.

Every concrete game thing also has a matching `*Def` class (`ItemDef`, `CharacterDef`,
`NPCDef`, `HouseDef`, ...) - the *template* an instance was created from. See "Instances vs.
defs" below.

## UIDs and lookup

Every `Thing` gets a unique, permanent `Uid` (`int`) the moment it's created, assigned and
tracked by a `Thing`-level mechanism (`GameCore/Thing.cs`). The lookup helpers:

- `Thing.UidGetThing(uid)` - resolves any `Thing` by uid, or `null`.
- `Thing.UidGetItem(uid)` / `Thing.UidGetCharacter(uid)` - typed convenience wrappers; return
  `null` if the uid exists but isn't that kind of thing.
- `Thing.AllThings` - an `IEnumerable<Thing>` over every live thing in the world. This is
  deliberately a plain enumeration, not an index - anything that needs to scan "every X in
  the world" (e.g. the earlier, now-replaced global item-decay sweep) pays for a full
  iteration. See [item-decay.md](../04-gameplay-systems/item-decay.md) for why the decay
  system moved away from using this on a timer.

Uids survive save/load - the same item keeps the same uid across a server restart, which is
what lets test code (and GM tooling) hold onto a uid across a save/reload cycle and expect to
find the same object again.

## Instances vs. defs: the template split

Every `Item` has a `TypeDef` (its `ItemDef`); every `Character`/`NPC` has a `Def`
(`CharacterDef`/`NPCDef`). The split exists because Sphere-style content is template-driven:
a `.def` file configures *one* `ItemDef` (e.g. `i_sword_viking_copper`), and every sword
created from it shares that def's configured values unless the instance itself overrides
them.

This has a direct consequence for where a given piece of data should live:

- **Def-level data** (`ItemDef`/`CharacterDef` fields) is the *template default* - shared by
  every instance unless the instance's own field has been independently set. These fields are
  backed by `FieldValue` (see [persistence-and-saving.md](persistence-and-saving.md)) and are
  what `.def` content lines configure (e.g. `ATTR = attr_static|attr_magic` sets
  `ItemDef.Attributes`).
- **Instance-level data** (`Item`/`Character` fields) is per-object state, independent per
  instance once seeded. A common pattern (used by, e.g., the item ATTR flag system - see
  [item-attr-flags.md](../04-gameplay-systems/item-attr-flags.md)) is: the def holds a
  *default* value, and the instance's `On_Create()` copies that default into its own
  independent field once, at creation time - after which a GM can change ONE specific
  instance without touching the def's default for every other instance of that type.

Both halves of this split are declared together in a single `.ct` file (`Def:` section for
the def-level fields, `Vars:` section for the instance-level fields) - see
[scripting-pipeline.md](scripting-pipeline.md) for the exact mechanics of how that becomes
real C#.

## Position and containment

A `Thing`'s location is one of exactly three shapes at any moment:

1. **On the ground** - has real X/Y/Z/M coordinates (`Point4D`/`IPoint4D`), `Cont` is `null`,
   `IsOnGround` is true. The thing is part of the map's spatial index for that location.
2. **Inside a container** - `Cont` is the containing `AbstractItem`, `IsOnGround` is false.
3. **Equipped on / held by a character** - `Cont` is the `AbstractCharacter`, `Layer`
   indicates which equipment slot (or the special "dragging"/hand layer for an item currently
   picked up but not yet dropped), `IsEquipped` is true.

`AbstractItem.Cont`'s setter (`GameCore/ThingManipulationTriggers.cs`) is the single place all
three transitions funnel through - setting it to a character equips/holds the item (via
`Trigger_EnterChar`), setting it to a container item places it inside (`Trigger_EnterItem`),
and there's a separate `Trigger_EnterRegion`/`On_EnterRegion` path specifically for landing on
the ground (not a `Cont` assignment at all - ground placement sets position fields directly).
`MakeLimbo()` is the inverse: pulls a thing out of wherever it currently is (firing the
matching `Trigger_Leave*`/`On_Leave*` hook) before it's moved somewhere else. See
[triggers-and-deny-chains.md](triggers-and-deny-chains.md) for the general shape of this
trigger pattern, and [item-decay.md](../04-gameplay-systems/item-decay.md) for a concrete,
non-trivial example of code hooking `On_EnterRegion`/`On_LeaveRegion` to track "is this
currently on the ground."

A real, specific gotcha worth knowing here: `Trigger_EnterRegion` requires the thing to
already be in limbo state, and - critically for anyone writing code against these hooks -
fires *before* `On_Create()` runs when an item is created directly onto the ground
(`ItemDef.Create(x, y, z, m)` calls `PutOnGround` before `Trigger_Create`). Any `On_Create()`
override that seeds instance state other code might read during `On_EnterRegion` needs to
account for that ordering - see the item-decay and corpse-ownership examples in
[item-decay.md](../04-gameplay-systems/item-decay.md) for how this was actually hit and
worked around.

## Deleting a Thing (#206, #786)

`Thing.InternalDeleteNoRFV()` (the core of every `Delete()`) runs in this order:
1. `Trigger_Destroy()` - fires `@destroy`, deletes contained items, and (characters) removes
   it from the map and marks it **limbo**.
2. `base.Delete()` - tears down plugins, trigger groups and timers.
3. Frees the uid; `IsDeleted` is true from here on.

Step 1 has to come before step 2 so that per-instance `@destroy` handlers still exist when
`@destroy` fires (#206, see [Spawning](../04-gameplay-systems/spawning.md)). As a result, a
Thing is in limbo but **not yet deleted** while its plugins tear down, and a plugin's
`On_UnAssign` can legitimately touch it (e.g. `WeaponSkillTargetQueuePlugin` aborting the
current skill). `TagHolder.ThrowIfDeleted()` normally treats "limbo while the server runs" as a
stray-object bug and deletes the Thing; during a delete that used to restart the delete from
inside itself, forever, until GameCore died with a stack overflow (exit 134) - any NPC deleted
mid-swing, e.g. by a test's cleanup (#786). Now `Thing.IsBeingDeleted` is true for the whole
of `InternalDeleteNoRFV()`: a nested delete returns at once, and `ThrowIfDeleted()` leaves a
Thing that's being deleted alone. Regression test: `Tests/Protocol/DeleteFightingNpcTests.cs`.
