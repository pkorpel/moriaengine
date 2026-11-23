# Persistence and saving

The in-memory world is saved as textual sections in `.sav` files. `WorldSaver` coordinates
saving/loading; `ObjectSaver` delegates categories to registered implementations. UIDs and
named definitions reconnect the object graph. Persistence is opt-in, not a dump of all fields.

## Def values: scripts give the default, a save keeps live changes

Scripts establish def defaults before world loading. Saved def sections apply live overrides,
such as GM edits. `FieldValue` keeps default and current values separately.

| Operation | Effect |
|---|---|
| Script load | Establishes the default and current value unless overridden live. |
| Set `CurrentValue` | Changes the live value, not the script default. |
| Load saved override | Restores the live value over the script default. |
| Reload scripts after a live edit | Updates the script default while preserving the live override. |

`ShouldBeSaved()` compares current with default; resetting to the default needs no override.
Script values can be resolved lazily so references to later definitions can work.

Custom parsing must establish defaults rather than accidentally create live edits.
`AbstractDef.SetTag` uses `FieldValue.SetFromCode` during script loading. Unlike
`SetFromScripts`, it refuses a value already changed live, without updating that default.
Preserve this distinction when extending custom loaders.

## Typed fields and custom syntax

Use `[DefField]` / `InitTypedField` for class-owned typed configuration, and tags for optional
extensions. Typed enum loading normalises `|` and `+` separators for `Enum.Parse`; conversion
also supports numeric values. Avoid duplicating these rules in each def.

Numeric tags can change boxed numeric type after textual round trips: `1.0` may save as `1`.
Use `ConvertTools` conversions instead of assuming a boxed `double` or `int`. Leading-zero
numeric text follows Sphere hexadecimal conventions; saved enums retain type information.

Custom range/list loaders should normalise input into typed fields. Definition references
that require other sections must remain in the postponed phase. See
[NPC definition loading](npc-definition-loading.md) for the keyword/backing-field distinction.

## Generic instance Save/Load

| State | Mechanism |
|---|---|
| Thing/Plugin template members | `[SaveableData]`; `[CopyableData]` independently controls copying. |
| Hand-written objects | `[SaveableClass]`, a supported load path, and `[SaveableData]` members and/or `[Save]` method. |
| Selected static members | `[HasSavedMembers]` type and `[SavedMember]` members, stored through globals by `StaticMemberSaver`. |
| Attached tags/timers/plugins | Saved holder references; referenced objects need their own save/load support. |

`[LoadingInitializer]` marks a parameterless construction path; `[LoadSection]` supports
section-based construction. `[LoadLine]` handles custom lines and `[LoadingFinalizer]`
post-load fixups. `[SavedMember]` is not the ordinary instance-field attribute.

The [save generator reference](code%20generators/save-implementor-generator.md) defines valid
combinations. Being saveable does not itself make an object a root: it must be reachable
through saved state or an explicit coordinator.

## Defs made in code, and what a save refers to by name

A saved plugin names its def, for example `def=#p_paralyze`. That def must be registered
before `WorldSaver.Load()`. Missing definitions can cause effects to be rejected on load.
Use a script section or a registered static instance on the def's own class:

```csharp
public static CraftingProcessPluginDef instance = (CraftingProcessPluginDef)
    new CraftingProcessPluginDef("p_craftingProcess", "C# scripts", -1).Register();
```

`ClassManager.InitClasses` runs the def class's static constructor before its `Bootstrap()`.
Template-generated plugin defs receive bootstrap wiring; hand-written defs need their
appropriate registration path. An unrelated class's lazy static field is not a reliable
way to register a saved def before load.

`CheckCodeMadeDefs` diagnoses misplaced/unregistered definitions after loading. Checking
late avoids accidentally initialising a missing def early and hiding the bug. Compiled
plugin handlers attach later through `PluginDef.Init()`; definition existence does not imply
those handlers are available during loading.

## Save and load pipeline

Globals and registered coordinator categories are saved. The Thing coordinator begins with
top-level Things, traverses contained objects and suppresses duplicates. `[EOF]` terminates
files; missing completion markers fail loading.

Loading restores objects/UIDs and queues unresolved references. `ObjectSaver.LoadingFinished()`
and `Timer.LoadingFinished()` complete deferred work. `WorldSaver.TryLoad` raises `@afterLoad`
before those calls: that hook is not proof the whole graph is ready.

Failed attempts clear partial world state before backup retries. Missing files can produce
an empty world; other failures are bounded by the load attempt limit. See [runtime lifecycle](runtime-lifecycle.md).

## Save requests and completion

| Request | Behaviour |
|---|---|
| Autosave | `[files] autosaveMinutes`, default 60; 0 or blank disables. `.nextsave` reports timing. |
| `.save` / `Globals.Save` / `TrySave` | Synchronous through writing completion. |
| Deliberate shutdown/recompile | Synchronous save before teardown. |
| `.exitnosave`, crash, SIGTERM | No new save guaranteed. |

All saves format pooled UTF-8 buffers while the world is held. Autosave's
`Globals.SaveInBackground` writes those buffers on a worker; it does not serialize live
mutable objects there. `@afterSave(path, success)` runs after writing under the global lock.
The next save/recompile/shutdown first calls `FinishPendingWrite`.

Logs and `WorldSaver.LastTimings` distinguish pause, total, format and write times. A short
pause does not mean the disk write has finished. Loading reserves buffer capacity from file
sizes to reduce allocation on the next save.

## Streams and failures

`@beforeSave` / `@beforeLoad` select paths; `@openSaveStream` / `@openLoadStream` can provide
streams. `E_BackupsManager_Global` handles timestamped backups and retention. Failed save
folders receive a `.failed` suffix and are excluded from selection. Without a script-supplied
writer, the fallback uses a temporary file renamed on completion.

Write, flush or close failure fails the save. `@afterSave` receives false, successful-backup
pruning must not run, and autosave is re-armed. Custom streams do not gain a multi-file atomic
transaction merely by participating in this pipeline.

## Sources

[WorldSaver](../../../GameCore/Persistence/WorldSaver.cs),
[ObjectSaver](../../../GameCore/Persistence/ObjectSaver.cs),
[FieldValue](../../../GameCore/FieldValue.cs), [AbstractDef](../../../GameCore/AbstractDef.cs),
[StaticMemberSaver](../../../scripts/Utils/Persistence/StaticMemberSaver.cs),
[BackupsManager](../../../scripts/Basic/BackupsManager.cs).
