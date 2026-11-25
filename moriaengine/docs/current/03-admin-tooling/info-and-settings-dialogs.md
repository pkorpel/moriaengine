# The `.info` and `.settings` dialogs

Two GM tools built on one dialog. `.info` shows and edits the data of any object: an
item, a character, a region, a plugin. `.settings` shows the shard's GM-tunable
configuration in categories. You can edit both live; settings are saved with the world.

The page has three parts. Section 1 is for GMs using the dialogs. Sections 2-4 are for
scripters making things appear in them. Section 5 is for engine developers and covers
how it works inside.

## 1. Using the dialogs

### Opening

- `.info` opens the dialog on your own character. `.x info` gives a target cursor and
  opens it on whatever you target.
- `.settings` opens the list of settings categories.

### What is on the page

The page shows everything defined for each kind of thing the object is. An NPC's page has
what every character has, plus what every object with tags has, plus what only NPCs
have. This applies to fields, links and action buttons alike.

- **Headline**: "Info dialog - <type>" or "Settings dialog - <category>".
- **Fields**: the object's data, one row each. A value you can change has an edit box;
  a value you can only look at is plain text. A very long value is cut short and has
  a small button that shows it whole.
- **Links**: a field whose value is another object (an item's container, a character's
  account, a settings category) has a button next to it. Pressing it opens the same
  dialog on that object. Closing that dialog brings you back to the previous one, so
  you can walk down a chain of objects and back.
- **Action buttons**: a separate column of named commands that act on this object.
  They are not data. Pressing one runs the command on this object at once, with no
  confirmation and no need to press "Ulozit". Some act directly (Cleartags deletes every tag), and some
  open another dialog (Taglist, Attributes).
- **Paging**: fields and action buttons page separately when there are more than fit.
- **Top-right buttons**: help, which shows how to type each kind of value (number,
  text, object reference...) with examples, and close.

### Changing values

1. Type new values into any of the edit boxes.
2. Press **Ulozit** (save). Nothing changes before that.
3. A results dialog lists every value you changed, each marked as applied or failed.
   A failed one shows the text you typed and why it was rejected; the others still apply.

Applied changes take effect immediately, with no restart. In `.settings`, they are
written out with the next world save and survive a restart. In `.info`, an object's
data survives only if that field is one the object saves anyway.

### `.settings`

The top page lists categories, such as Combat, Magery, Logout and Monster Abilities.
Each one is a link to that category's page, which can in turn link to
subcategories. Everything else works as described above.

## 2. Scripting: showing a type in `.info`

What a GM sees in section 1 maps to code like this:

| On the page | In code |
|---|---|
| field row | a public field or property, or a descriptor `[GetMethod]` |
| edit box on it | a public setter / non-`readonly` field, or a matching `[SetMethod]` |
| link next to a field | automatic, when the value's type is itself viewable |
| action button | a `[Button]` method, on the type or in a descriptor |

There are two ways to provide them. You can mark the type itself, or describe it from a
separate class.

### Mark the type: `[ViewableClass]`

Every public instance field and property of a `[ViewableClass]` type is shown, unless
hidden - adding a public field is enough to make it appear (this is how
`Player.Level`/`Player.Experience` reached `.info` with no dialog code). `Plugin`
subclasses are viewable without the attribute. An illustrative type:

```csharp
[ViewableClass("Example")]              // optional headline; default is the type name
public class ExampleThing {
    public int Speed;                   // read-write row

    public int Crew { get; private set; } // read-only row: no public setter

    [InfoField("Deck height")]          // renamed label, same visibility
    public int deckZ;

    [NoShow]                            // hidden
    public Timer internalTimer;

    [Button("Anchor")]                  // action button; calls the method, ignores the result
    public void DropAnchor(bool silent = false) { ... }
}
```

- `[NoShow]` opts one member out; on an override or a shadowing member it also hides the
  base member of that name. Note the default is the reverse of the save and
  deep-copy attributes, which include only what is marked.
- Read-only vs. editable is decided by the member: no public setter, an `init` setter, or a
  `readonly` field is read-only. Every member of a struct is read-only.
- Static members, `const` fields and non-public members never show.

### Describe it from outside: `[ViewDescriptor]`

A static class can add action buttons and computed fields to another type's page without that
type carrying any dialog code. Use this for core types (`TagHolder`, `Item`, `NPC`) whose
dialog behaviour belongs in administration scripts, not in `GameCore`, or other
System or 3rd party types.

```csharp
[ViewDescriptor(typeof(TagHolder), "Tag Holder")]
public static class TagHolderDescriptor {
    [GetMethod("TagsCount", typeof(int))]          // computed read-only field
    public static object GetTagsCount(object target) {
        return ((TagHolder) target).GetAllTags().Count();
    }

    [Button("Taglist")]                            // button opening its own dialog
    public static void TagList(object target) {
        D_TagList.TagList((TagHolder) target, null);
    }
}
```

A `[GetMethod]` and a `[SetMethod]` with the same name (case-insensitive) and the same
type make an editable field - `NPCDescriptor`'s "Home leash":

```csharp
[GetMethod("Home leash", typeof(int))]
public static object GetLeash(object target) {
    NPCAiTimer ai = NPCAiTimer.GetFor((NPC) target);
    return (ai == null) ? 0 : ai.WanderLeash;
}

[SetMethod("Home leash", typeof(int))]
public static void SetLeash(object target, object value) {
    NPCAiTimer ai = NPCAiTimer.GetFor((NPC) target);
    if (ai == null) {
        throw new SEException("Tahle postava nema AI");   // shown as an error row
    }
    ai.WanderLeash = Math.Max(0, Convert.ToInt32(value, CultureInfo.InvariantCulture));
}
```

The third constructor argument hides fields by name - `MutableRectangleDescriptor`
hides the inherited `MinX`/`MinY`/`MaxX`/`MaxY`:

```csharp
[ViewDescriptor(typeof(MutableRectangle), "Rectangle",
    new[] { "MinX", "MinY", "MaxX", "MaxY" })]
public static class MutableRectangleDescriptor { }
```

Descriptors stack along the type hierarchy, parent first: a `Container` gets its own
descriptor's members and those of `ItemDescriptor` and `TagHolderDescriptor`. The
descriptor's label is the headline of a type that has no `[ViewableClass]`; an explicit
`[ViewableClass("...")]` label wins.

## 3. Scripting: adding a GM-tunable setting

### A new category

A settings category uses three independent systems:

1. **Saving an object**: `[SaveableClass]`, `[LoadingInitializer]` and `[SaveableData]`
   make the category instance and its values part of the world save. See
   [Persistence & saving](../02-engine-core/persistence-and-saving.md).
2. **Saving a static field**: `[HasSavedMembers]` and `[SavedMember]` save which instance
   the static `instance` field points to, so after a load it is the saved instance.
3. **Showing it**: `[ViewableClass]` puts its fields on a dialog page. Deriving from
   `SettingsMetaCategory` marks it as a settings category, and a property on
   `SettingsCategories` (`MetaSettings.cs`) adds it to the top `.settings` page.

`LogoutSettings` (`scripts/Basic/LogoutSettings.cs`) is the whole pattern:

```csharp
[SaveableClass]       // instances can be saved and loaded
[HasSavedMembers]     // the class has static fields that are saved
[ViewableClass]
public class LogoutSettings : SettingsMetaCategory {

    [SavedMember]     // this static field is saved
    public static LogoutSettings instance = new LogoutSettings();

    [LoadingInitializer]  // the constructor used when loading an instance
    public LogoutSettings() {
    }

    [SaveableData]    // this value is saved with the instance; the initializer is the default
    public double logoutDelaySeconds = 60;

    [SaveableData]
    public bool showLogoutEffect = true;
}

// In SettingsCategories (MetaSettings.cs): the "Logout" row on the top .settings page.
// [InfoField] only sets the row's label; a property named Logout without it is the same.
[InfoField("Logout")]
public LogoutSettings LogoutSettings {
    get { return LogoutSettings.instance; }
}
```

Game code reads `LogoutSettings.instance.logoutDelaySeconds` wherever it needs the value.
A field without `[SaveableData]` still shows and can be edited, but the edit is lost on
restart.

### Bulk-editing many defs

For "the same field on every def of a kind", derive from
`MassSettings_ByClass_List<TDef>` instead of listing properties by hand - see
`AllSpellsMassSetting` (`scripts/Basic/skills/Magery/MagerySettings.cs`) and
`AllPoisonPotionsMassSetting` (`.../Poisoning/PoisoningSettings.cs`).

### Subcategories

A category property can return another category singleton; it shows as a link, as on
`SettingsCategories` itself. For the current list of categories, read the
`[InfoField]` properties of `SettingsCategories`.

## 4. Scripting tips and lessons learned

- **Check `.info` before building UI.** A public field on a viewable type is already
  visible and editable. An earlier change planned a new mechanism for something
  `[ViewableClass]` already did.
- **Hide what GMs must not touch.** Since members show by default, put `[NoShow]` on
  internal state (timers, caches, back-references) when adding it to a viewable type.
- **Many related toggles: one button, one dialog.** For a set of flags that belong
  together, add a descriptor button that opens a dedicated dialog rather than dozens of
  rows. `ItemDescriptor`'s "Attributes" button opens the `D_AttrList` checkbox dialog
  for item ATTR flags (#215/#217); `TagHolderDescriptor`'s "Taglist"/"Timerlist" follow
  the same shape.
- **Validate in the setter.** Throw `SEException` with a GM-readable message from a
  property setter or `[SetMethod]`; the dialog shows the row as failed and keeps the rest
  of the submission.
- **Don't merge independent tunables.** `DecaySettings` began with one
  `decayIntervalMinutes` and had to be split into `playerCorpseDecayMinutes`,
  `npcCorpseDecayMinutes` and `itemDecayMinutes` once the real defaults differed. Give
  each real-world quantity its own field from the start, with units in the name.
- **Name the unit.** `logoutDelaySeconds`, `criminalFlagSeconds`, `maxLoadPercent`: the
  field name is the only label most settings get.
- **Read settings where they are used.** Read `XSettings.instance.field` at the point of
  use rather than caching it, so a GM edit applies to the next action.
- **Test the logic, not the dialog.** Settings tests (for example
  `scripts/Tests/MiningSettingsTests.cs`) set the field directly and check its effect on
  gameplay. The dialog plumbing is shared and has nothing specific to your setting.

## 5. How it works inside

### The dialog

Both commands are `[SteamFunction]`s that open `D_Info`
(`scripts/Basic/administration/InfoDialog/D_Info.cs`) on one object: `D_Info.Info` on
the source character or its argument, and `SettingsCategories.Settings`
(`scripts/Basic/administration/Settings/MetaSettings.cs`) on `SettingsCategories.instance`.

- **Building the page**: `DataViewProvider` finds the `IDataView` for the target's
  type (see below). `D_Info` writes a page of its field views and a separate page of its
  action-button views. A field gets a link button when its value's type has a data view.
  The only `.settings`-specific code is the headline: "Settings dialog" when the target is
  a `SettingsMetaCategory`.
- **Ulozit**: the edit boxes go to `SettingsProvider.AssertSettings`
  (`.../Settings/SettingsProvider.cs`). It compares each submitted string with the
  field's current string value, matching enums by name. For each changed field it calls
  `SetStringValue`/`SetValue`, catches any exception as a failed result, and returns the
  list shown by `D_Settings_Result`.
- **Action button**: `ButtonDataFieldView.OnButton(target)` calls the method.
- **Link**: opens a new `D_Info` on the field's value and stacks the current dialog
  (`DialogStacking`) so that closing returns to it.

### Data views: the contract between dialog and types

`D_Info` knows nothing about the object it shows. It only talks to two interfaces in
`scripts/Basic/administration/InfoDialog/IDataView.cs`:

- **`IDataView`**, one per type: `HandledType`, `HandleSubclasses`, `GetName` (the
  headline), field and action-button counts, and `GetDataFieldsPage` /
  `GetActionButtonsPage` for paging.
- **`IDataFieldView`**, one per row: `GetName`, `ReadOnly`, `FieldType`,
  `GetValue`/`SetValue`, `GetStringValue`/`SetStringValue` (what the edit box shows and
  accepts), and `IsButtonEnabled`/`OnButton` for action buttons
  (`ButtonDataFieldView`).

`DataViewProvider.Bootstrap` registers every `IDataView` class at startup.
`FindDataViewByType` looks up the exact type first, then the most derived registered
base type with `HandleSubclasses` set, and caches the result. A type with no view
gets "No DataView found" in the dialog.

Most views are generated (below). Some are hand-written: `MassSettingsView`
(`.../Settings/MassSettingsView.cs`) handles every `IMassSettings` and builds one row
per def. Write one by hand only when the rows are not the members of a type.

### How views are generated

`ViewableClassGenerator` (`Tools/SteamEngine.Generators/ViewableClasses/`) runs during
the Scripts compilation and emits `ViewableClasses.g.cs`. It runs only where the
Scripts-side `ViewableClassDataView` base type exists, so GameCore gets nothing. The
attributes live in `Tools/SteamEngine.Generators.Abstractions` under the
`SteamEngine.CompiledScripts.Dialogs` namespace.

For each `[ViewableClass]` type, each `Plugin` subclass and each type named by a
`[ViewDescriptor]` (including types from referenced assemblies such as GameCore), it
emits a `ViewableClassDataView` subclass. Field and button views are shared sealed
classes with direct access (`((Item) target).Weight`) - no reflection. At startup
`DataViewProvider` registers these generated views; opening a dialog is a dictionary
lookup and virtual calls.

### Rules

- **Fields and properties**: public instance; a property needs a public getter and
  cannot be an indexer. The member type must fit the `object`-valued field-view API:
  no pointers, function pointers, ref-like or open generic types. These rules are errors
  only for members declared in Scripts source, where `[NoShow]` can fix them. Members
  compiled into another assembly (System, 3rd party, GameCore) that break them are left
  out silently, and a descriptor's hidden-field list is applied first.
- **Buttons on the type**: public instance, non-generic, no by-reference return, and
  callable without arguments - every parameter optional or `params`.
- **Descriptor class**: non-generic; the handled type is a closed class or struct.
- **Descriptor buttons**: `public static void M(object target)`.
- **`[GetMethod]`**: `public static object M(object target)` (any non-void return).
- **`[SetMethod]`**: `public static void M(object target, object value)`.
- Descriptor methods are non-generic with by-value parameters (no `ref`/`out`/`in`).
  A getter and setter for one field must declare the same type, and every field needs
  a getter.
- A type cannot be both viewable (or a plugin) and a descriptor.

### Diagnostics

| ID | Severity | Meaning | Effect |
|---|---|---|---|
| SEGEN900 | error | a required Viewable attribute is missing from the compilation | nothing generated |
| SEGEN901 | error | a required engine type is missing | nothing generated |
| SEGEN902 | warning | an attribute with a Viewable attribute's name resolves to a different type | that usage is not recognised |
| SEGEN950 | error | a viewable type or member cannot be generated; the message names it and the reason (indexer, no public getter, unsupported type, required button parameter, ...) | that member/type is left out |
| SEGEN951 | error | an invalid descriptor or descriptor member: bad signature or field type, duplicate getter/setter, type mismatch, field without getter | that member/field is left out |
| SEGEN952 | error | a type is both viewable (or a plugin) and a descriptor | resolve the roles |
| SEGEN953 | warning | a member type has the same name as a canonical engine type, so value conversion may be wrong | inspect the reported types |

A member error leaves the rest of the view generated, so a successful build is not proof
that every declaration made it in - fix all diagnostics.

Tests, in `Tools/SteamEngine.Generators.Tests/`:

- `ViewableClassGeneratorTests.cs`: unit tests on stub engine types, covering every
  diagnostic and the generated text.
- `ViewableClassGeneratorRuntimeTests.cs`: compiles against the real GameCore and Scripts
  (via the shared `EngineRuntimeHarness`), then drives the generated views through the real
  `IDataView`/`IDataFieldView` (fields, edits, buttons, descriptors, labels).

The `.info` protocol tests in `Tests/` are an extra regression layer.

## Related

- [Dialog framework](dialogs-framework.md) - `CompiledGumpDef`, `ImprovedDialog`, used by `D_Info`.
- [Persistence & saving](../02-engine-core/persistence-and-saving.md) - `[SaveableClass]`, `[SaveableData]`, `[SavedMember]`.
- [Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md) and
  [scripting generators](../02-engine-core/code%20generators/scripting-generators.md).
- [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md) - the `D_AttrList` dialog.
