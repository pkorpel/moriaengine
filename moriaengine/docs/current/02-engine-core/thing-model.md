# The Thing model

Every world item and character derives from `Thing`. Engine contracts live in GameCore;
gameplay classes live in the separately compiled scripts assembly.

| Engine base | Gameplay base | Examples |
|---|---|---|
| `Thing` → `AbstractItem` | `Item` | Container, Door, Corpse, MultiItem, House. |
| `Thing` → `AbstractCharacter` | `Character` | Player, NPC. |

A corresponding `*Def` describes each content type. A `.def` section configures a definition
instance; it does not create a new C# class.

## Identity and lookup

`Thing` registers instances in its static UID array. `UidGetThing`, `UidGetItem` and
`UidGetCharacter` resolve UIDs; typed lookups return null for the wrong kind. `AllThings`
enumerates the registry rather than providing a selective index.

UIDs survive save/load for existing objects but can be reused after deletion. Do not treat
a saved integer as the permanent identity of an object that may no longer exist. Reloading
also reconstructs objects; an old C# reference does not become a reference to the new instance.

## Instances vs. defs: the template split

| Ownership pattern | Behaviour after changing the def | Example |
|---|---|---|
| Read from def | Existing instances see the new value. | Item weight per piece. |
| Copy on creation | Existing instances retain their own value. | Model/colour; item attributes seeded in `On_Create`. |
| Instance override with fallback | Instances without an override see the new default. | Item name. |

Choose the pattern deliberately. A def field is not automatically an instance default that
gets copied, nor does every instance property always read through to its def. Def fields
use `FieldValue`; instance saving/copying has separate attributes and hooks.
See [persistence](persistence-and-saving.md) and [scripting](scripting-pipeline.md).

## Position and containment

An item can be on the ground, inside an item, held/equipped by a character, or in limbo
between placements. The dragging layer is a character placement, not a backpack insertion.
Characters also have map presence/disconnection state; these are not container items.

`MakeLimbo()` removes the old placement through its leave hooks. Container/character entry
uses the corresponding manipulation path; ground placement has its own `Trigger_EnterRegion`
path. Do not replace these operations with raw coordinate or reference assignments.

Ground entry requires limbo first. Creating an item directly on the ground places it before
`Trigger_Create` / `On_Create`. Entry hooks must therefore tolerate creation-time state that
has not yet been seeded by `On_Create`.

Low-level placement and interactive permission checks are separate. See
[deny chains](triggers-and-deny-chains.md) before using an operation to move player items.

## Deleting a Thing

`InternalDeleteNoRFV()` runs destroy handling first, including contained-item cleanup and
character map removal. Base cleanup then removes plugins, trigger groups and timers, and
the UID is freed.

Destroy handlers must still be present when `@destroy` runs. During plugin teardown the
holder can consequently be in limbo but not yet marked deleted. `IsBeingDeleted` guards the
whole operation against recursive deletion; `ThrowIfDeleted` recognises this teardown state.
Preserve that order when adding cleanup hooks. `On_UnAssign` may run while its holder is
being deleted, so it must not assume normal world presence.

## Sources

[Thing](../../../GameCore/Thing.cs), [AbstractItem](../../../GameCore/AbstractItem.cs),
[AbstractCharacter](../../../GameCore/AbstractCharacter.cs),
[ThingManipulationTriggers](../../../GameCore/ThingManipulationTriggers.cs),
[item decay example](../04-gameplay-systems/item-decay.md).
