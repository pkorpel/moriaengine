# Item ATTR flags

Tracking issue: #215 ("do the whole thing" - all 21 flags, not a recommended-skip
subset), sub-issues #217-#225, shipped as PRs #226-#234. #225 closed not-planned
(see [Doors](doors.md)).

## The `ItemAttributes` enum

`scripts/Basic/ItemClasses/Item.cs:42-67` defines a `[Flags] enum ItemAttributes` with
21 real Sphere `attr_*` bits, named exactly as Sphere names them (snake_case, `attr_`
prefix) rather than C#-idiomatic PascalCase, for direct 1:1 compatibility with `.def`
content lines.

The values were cross-checked against SphereServer's own `sphere_defs.scp`
(`[DEFNAME attr_flags]`) and wiki.spherecommunity.net, not guessed. That cross-check
found a real bit-mapping conflict: this engine's old LScript constants
(`scripts/Basic/defs.scp`) had a non-standard `attr_stolen` at bit `0x01`, where real
Sphere defines `attr_identified` at that bit and doesn't define `attr_stolen` as an ATTR
bit at all. Fixed by repurposing `0x01` to `attr_identified` - zero real content used
either value at the time, so nothing broke.

## Def default + per-instance override

Same split as every other config-backed field in this engine (see
[The Thing model](../02-engine-core/thing-model.md) and
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md)):
`ItemDef.AttributesDefault` is the template-level default (`FieldValue`-backed), and
`Item.Attributes` (`Item.cs:73-80`) is the per-instance value, independently overridable
after creation. `Item.On_Create` (`Item.cs:263-276`) seeds the instance from the def's
default - the same "copy a Def-level default into the instance once at creation"
pattern `NPC.On_Create` already uses for `DefaultMovementSettings`.

`.def` content sets the default via an `ATTR = attr_x|attr_y` line. This needed **zero
new parser code**: `AbstractItemDef.LoadScriptLine` aliases the literal string `"attr"`
to `"attributesdefault"`, and the existing generic `FieldValue`/`ConvertTools.ConvertTo`
pipe-separated-`[Flags]`-enum pipeline (see
[Persistence & saving](../02-engine-core/persistence-and-saving.md)) does the rest - that
pipeline already replaces `|` with `,` before `Enum.Parse`, so pipe-separated flag lists
were already supported for every other `[Flags]` `.def` field in this engine.

## Programmatic API

`HasAttribute(ItemAttributes flag)` (`Item.cs:82-84`) is a plain bitwise-AND check.
`SetAttribute(ItemAttributes flag, bool value)` (`Item.cs:101-116`) is the single place
every flag's side effect gets wired in - it's worth reading in full, since it's the
real index of "what happens when this flag changes":

```csharp
public void SetAttribute(ItemAttributes flag, bool value) {
    if (value) {
        this.attributes |= flag;
    } else {
        this.attributes &= ~flag;
    }
    if ((flag == ItemAttributes.attr_decay) || (flag == ItemAttributes.attr_questitem)) {
        ItemDecaySystem.ScheduleOrCancel(this); //#219 - re-evaluate the per-item decay timer
    }
    if ((flag == ItemAttributes.attr_static) || (flag == ItemAttributes.attr_move_never) || (flag == ItemAttributes.attr_move_always)) {
        this.RecomputeNonMovable(); //#220
    }
    if (flag == ItemAttributes.attr_invis) {
        this.Flag_Invisible = value; //#224 - the only write path Flag_Invisible ever gets
    }
}
```

Any future flag that needs a side effect on toggle belongs here, not scattered at each
call site.

## D_AttrList - the dedicated GM dialog

**Design pivot, worth remembering for any future "many togglable things" GM dialog.**
The first draft put 22 individual public boolean properties on `Item` (e.g.
`AttrNewbie`), meant to auto-surface through the generic reflection-based `.info`
property list (see
[ViewableClasses & the `.info` dialog](../02-engine-core/viewable-classes-and-info-dialog.md)
- the same mechanism `Player.Level`/`Experience` use). The user's explicit follow-up:
"I like the idea of having entire separate dialog (just as we have timers, plugins,
tags) with all the possible attributes and checkbox next to them." Rebuilt as `D_AttrList`
(`scripts/Basic/administration/attributes/D_AttrList.cs`), modeled directly on the
existing `D_TagList`/`D_PluginList` pattern - one `GUTACheckBox` row per flag,
`OnResponse` writes every checkbox's state back via `SetAttribute`. See
[Dialog framework](../03-admin-tooling/dialogs-framework.md) for `GUTACheckBox`/`GUTATable`
itself.

Reachable two ways:
- A `.AttrList` target-cursor command (a `CompiledTargetDef`, matching `.flip`/`.kill`'s
  shape - NOT `.TagList`'s self-referential `TagHolder self` signature, since an item has
  no "self" context to invoke a command as; a GM always targets one externally. An early
  draft got this wrong (`AttrList(Item self)`) and had to be corrected.) See
  [GM commands](../03-admin-tooling/gm-commands.md) for the target-cursor-vs-self-referential
  command-shape distinction in general.
- An "Attributes" button added to the regular `.info` dialog via `ItemDescriptor.cs`'s
  `[ViewDescriptor(typeof(Item),...)]` (the same place `FixWeight` already lived).

**Standing guidance from this decision**: prefer a dedicated dialog with one checkbox
per toggle over scattering booleans into the generic reflection-based `.info` view,
whenever a feature needs many independent GM-togglable flags - matches this engine's
established mental model for GM tooling (Tags/Timers/Plugins all use this shape).

## Flag groups and their real wiring

### Movement & placement: `attr_move_never` / `attr_move_always` / `attr_static`

These drive `Flag_NonMovable` (`Item.cs:373-380`), which had **zero consumers at all**
before this work - not just a missing write path (confirmed via grep: no read path
either; `AbstractItem.On_DenyPickup`'s default implementation returned `Continue`
unconditionally). `RecomputeNonMovable` (`Item.cs:143-149`) derives the flag:
`attr_static`/`attr_move_never` set it, `attr_move_always` is an explicit override that
wins if somehow combined with either on the same item. It stops players, not GMs: a GM can
drag a fixed item and it stays fixed where it lands (#854), but not with `.gm` off. `Item.On_DenyPickup`
(`Item.cs:158-178`) is the new read path, denying pickup when `Flag_NonMovable` is true.

### Magic/crafting-quality display: `attr_magic` / `attr_exceptional` / `attr_enchanted` / `attr_imbued` / `attr_reforged`

Flags + display only, deliberately no mechanical effect - no crafting-bonus/
item-upgrade system exists in this engine to attach a real effect to, confirmed
acceptable scope with the user. `Item.Name` (`Item.cs:246-257`) prepends a quality-word
prefix computed by the private `QualityPrefix` getter (`Item.cs:224-244`), reusing the
existing name/cliloc pipeline (`GetNameCliloc` already reads `this.Name`) rather than
building a new tooltip mechanism. Precedence for an item carrying more than one flag at
once (real content shouldn't normally do this): reforged > imbued > enchanted >
exceptional > magic.

`attr_exceptional`'s prefix always shows, regardless of identification - a visible
craftsmanship mark, matching real UO. The other four require `attr_identified` to be
set first; this **is** `attr_identified`'s entire effect in this pass, deliberately not
a new Identification-skill mechanic.

### Ownership & trade: `attr_owned` / `attr_nodroptrade` / `attr_norepair`

`attr_owned` backs `Item.OwnedBy` (`Item.cs:91-99`, tag-backed). Deliberately **not**
named `Owner` - `House` and `Corpse` (both `Item` subclasses) each already have their
own, semantically different `Owner` (who owns the house / whose body is this), and a
same-named property here would have silently shadowed both (confirmed by the real
`CS0108` warnings a first attempt produced when it was named `Owner`). See
[Housing](housing.md) and [Corpses & death](corpses-and-death.md).

`attr_nodroptrade` has **two** separate enforcement points, because they're genuinely
different code paths: `Item.On_DenyPutOnGround` (`Item.cs:184-190`) for the ground-drop
half, via the normal deny-trigger chain (see
[Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md)); and an
explicit check inside `SecureTrade.AddItem` for the trade-window half, since that path
deliberately bypasses the whole deny-trigger chain already - `On_DenyPutOnGround` alone
would never have caught it.

`attr_norepair` has a confirmed **zero current consumer** - no repair system exists
anywhere in this engine. This is documented as intentional, exactly as the original
issue anticipated as an acceptable outcome, not an oversight.

### Visibility & identification: `attr_invis` / `attr_identified`

`attr_invis` writes to `Flag_Invisible` (`Item.cs:113-115`) - its only write path ever.
The per-viewer exception list (`IsSeenBy`/`GrantVisibilityTo`/`RevokeVisibilityFrom`,
`Item.cs:123-136`) uses a dynamically-keyed `TagKey.Acquire("_seenBy_" + viewer.Uid)` per
viewer - matching real Sphere's own `TAG.SeenBy_[CharUID]` pattern exactly, per the
original issue's explicit instruction not to invent a different mechanism. See
[Tags & timers](../02-engine-core/tags-and-timers.md) for the dynamically-keyed-`TagKey`
idiom in general.

`attr_identified` gates the quality-flag name prefixes described above - see that
section for the full mechanism.

### Death & corpse-drop: `attr_newbie` / `attr_blessed` / `attr_cursed` / `attr_insured`

Covered in [Corpses & death](corpses-and-death.md) - this is specifically about
`Item.CanFallToCorpse`, not re-explained here.

### Decay: `attr_decay` / `attr_questitem`

Covered in [Item decay](item-decay.md).

### House lockdown: `attr_lockeddown` / `attr_secure`

Covered in [House lockdown & security](house-lockdown.md).

### Door state: `attr_opened`

Investigated and deliberately **not** implemented (#225, closed not-planned) - `Door.
IsOpen` already derives state directly from the door's `Model` field with no separate
boolean to begin with, so a parallel ATTR flag would be strictly less reliable than the
existing mechanism, not a safe addition. The enum value stays defined for
`.def`-compatibility completeness, same precedent as `attr_norepair`. See
[Doors](doors.md) for the full reasoning.

## Verification

Verified via real protocol tests (`Tests/Protocol/AttrFlagTests.cs`), including a full
`D_AttrList` dialog round-trip: target an item, open `.AttrList`, submit the checkbox
gump with specific boxes checked via a real `GumpMenuSelectionInPacket` (0xB1), confirm
exactly those flags land on the item.

**Real test-authoring gotcha, not a product bug**: `[Flags]` enum `ToString()` lists
flags in **enum declaration order**, not the order they were OR'd together or appeared
in a `.def` line - `ATTR = attr_static|attr_magic` prints as `"attr_magic, attr_static"`
(magic declared earlier in the enum), not `"attr_static, attr_magic"`. A first test draft
asserted the wrong order; fixed the assertion, not the enum.
