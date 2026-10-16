# ViewableClasses & the `.info` dialog

`.info` (`D_Info`, `scripts/Basic/administration/InfoDialog/D_Info.cs`) is a single,
generic GM dialog that can display and edit the fields of *any* object in this engine -
an `Item`, a `Character`, a `SettingsMetaCategory` singleton (reused directly for the
`.settings` dialog - `D_Info.Construct` even branches its headline on `target is
SettingsMetaCategory`), the result of following a button on another `.info` dialog, and
so on. Nothing about `D_Info` itself knows what an `Item` or a `Player` is - the entire
field list, button list, and read/write behavior is generated once per type, at compile
time, by reflecting over `[ViewableClass]`-marked types.

## Why generate code instead of reflecting at dialog-open time

`ViewableClassGenerator` (`scripts/Basic/administration/InfoDialog/Generator/
ViewableClassGenerator.cs`) is an `ISteamCSCodeGenerator` - one of the compile-time code
generators that run as part of this engine's own build (same family as the `.ct`
`ClassTemplateParser` pipeline described in [Scripting & code generation
pipeline](scripting-pipeline.md), emitting into `Generated/ViewableClasses.Generated.cs`).
For every type carrying `[ViewableClass]` (or matching the small hardcoded
`viewableSpecials` list, currently just `Plugin`), it emits a dedicated
`GeneratedDataView_<Type>_<N>` class containing one small sealed inner class per visible
field/property/button - each just a direct, non-reflective cast-and-access
(`((Item) target).FixWeight()`, `((Item) target).Weight`, etc.). Opening `.info` on an
item at runtime is then a lookup into a plain `Hashtable`/`SortedList`
(`DataViewProvider.dataViewsForTypes`/`dataViewsForbaseClasses`,
`scripts/Basic/administration/InfoDialog/DataViewProvider.cs`) followed by ordinary
virtual calls - no `Type.GetMembers`/`PropertyInfo.GetValue` happens at dialog-open time,
only once, at the server's own startup, when `ClassManager`'s type-registration pass
calls `ViewableClassGenerator.CheckViewabilityClass` for every loaded type.

The practical consequence for day-to-day script work: **adding a plain public field or
property to a `[ViewableClass]`-marked type is enough to make it show up in `.info`,
with zero other wiring** - this is exactly how `Player.Level`/`Player.Experience`
(leveling system, #126) ended up visible with no explicit dialog code at all, and it's
also why an earlier pass in this session's own history (see CLAUDE.md's #154 section)
mistakenly assumed a NEW `.info`-surfacing mechanism needed to be built, when a working
one already existed and just needed noticing.

## Three ways to control what shows up

1. **`[ViewableClassAttribute]`** on the class itself opts it in at all; an optional
   constructor string overrides the dialog's headline label (otherwise it's the bare
   type name).
2. **`[NoShowAttribute]`** on a field/property opts that one member back out - everything
   else public is included by default (`GeneratedInstance`'s constructor uses
   `HasntAttribute` with `NoShowAttribute` as the filter, `ViewableClassGenerator.cs:261`)
   - the *inverse* of most attribute-driven systems in this engine, which default to
   excluded-unless-marked. Worth remembering when adding a field to an already-viewable
   class: it will appear in `.info` unless you explicitly hide it, not the other way
   around.
3. **`[ButtonAttribute]`** on a method turns it into an action button (`FixWeight`-style -
   calls the method with no further UI); **`[InfoFieldAttribute(name)]`** on a field
   just renames its displayed label without changing visibility.

A `ReadOnlyDataFieldView` vs. `ReadWriteDataFieldView` split is decided automatically
from reflection, not a separate attribute - a property with no public setter (or a
`readonly` field) is read-only in the dialog.

## `[ViewDescriptor]` - adding dialog-only buttons/fields without touching the class

The more interesting mechanism, and the one most gameplay-system code actually reaches
for: a **separate static class**, decorated `[ViewDescriptor(typeof(X), "Label")]`, can
inject extra `[Button]`/`[GetMethod]`/`[SetMethod]` members into `X`'s generated
`.info` page without `X` itself needing a `[ViewableClass]` attribute or carrying any
dialog-specific code at all. `ItemDescriptor`
(`scripts/Basic/administration/Settings/implementations/Descriptors/ItemDescriptor.cs`)
is the simplest real example:

```csharp
[ViewDescriptor(typeof(Item), "Item")]
public static class ItemDescriptor {
    [Button("FixWeight")]
    public static void FixWeight(object target) {
        ((Item) target).FixWeight();
    }

    [Button("Attributes")]
    public static void Attributes(object target) {
        D_AttrList.ShowFor((Player) Globals.SrcCharacter, (Item) target);
    }
}
```

Both buttons are static methods taking the generic `object target` - the generator casts
to the real type internally (see `GenerateButtonIDFW`,
`ViewableClassGenerator.cs:633`). `TagHolderDescriptor`
(`.../Descriptors/TagHolderDescriptor.cs`) is the richer example, and the one worth
copying as a template: it adds "Taglist"/"Timerlist" buttons that **open a completely
separate dedicated dialog** (`D_TagList`/`D_TimerList`) rather than acting inline, plus
two `[GetMethod]`-backed read-only fields (`TagsCount`/`TimersCount`) that have no
backing field on `TagHolder` at all - their value is computed on demand by the
descriptor's own static method.

**This "button opens a dedicated dialog" shape is the established precedent for any new
checkbox-list/sub-dialog need** - when the item ATTR flag system (#215/#217) needed a
GM-facing way to toggle 21 boolean flags, the user explicitly asked for "an entire
separate dialog... with all the possible attributes and checkbox next to them" rather
than scattering 21 individual boolean properties across `.info`'s own field list - the
resulting `D_AttrList` checkbox dialog, opened via `ItemDescriptor`'s `Attributes`
button above, is built on exactly the same `TagHolderDescriptor`-established pattern,
not a new idea. Prefer this over adding many individual reflection-surfaced booleans
when a set of related toggles belongs together conceptually.

`ViewDescriptor`'s `nonDisplayedFields` constructor parameter (third overload,
`ViewableClassAttributes.cs:134`) can also *suppress* specific field names from the
generated page - used when a descriptor wants to curate the field list, not just add to
it.

## Why a descriptor and not just editing the class

A `[ViewDescriptor]` never needs the described type (`Item`, `TagHolder`, ...) to carry
any info-dialog-specific code or attribute itself - the association is declared entirely
on the descriptor side (`ViewDescriptorAttribute.HandledType`), discovered the same way
`[ViewableClass]` is, via `ClassManager`'s type-scanning hook
(`ViewableClassGenerator.CheckViewabilityClass`, `ViewableClassGenerator.cs:126`, checks
for `[ViewDescriptor]` as the `else` branch of the same dispatch that checks for
`[ViewableClass]`). This matters for a type like `TagHolder` - the common base of nearly
everything in the engine - where adding dialog machinery directly to the class would mean
every subclass inherits it whether wanted or not, and where the actual *behavior* (how to
list an item's tags vs. a character's) can stay in administration-layer script code
(`scripts/Basic/administration/...`) instead of the core `GameCore` class. Descriptors
also stack by type hierarchy - `FindDescriptorsForType`
(`ViewableClassGenerator.cs:155`) collects every registered descriptor whose
`HandledType` is assignable from the generated class, sorted parent-first
(`TypeHierarchyComparer`), so a `Container` (which has its own `[ViewDescriptor(typeof(
Container))]`) gets both its own descriptor's fields AND everything `TagHolderDescriptor`
contributes via the `TagHolder` base.

## Related
- [Scripting & code generation pipeline](scripting-pipeline.md) - the other half of this
  engine's compile-time-codegen family; `ViewableClassGenerator` is one
  `ISteamCSCodeGenerator` among several.
- [The `.settings` dialog framework](../03-admin-tooling/settings-dialog.md) - reuses
  `D_Info`/`IDataView` directly for `SettingsMetaCategory` singletons rather than
  building a separate settings-specific dialog.
- [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md) - the `D_AttrList`
  dedicated-checkbox-dialog precedent referenced above.
