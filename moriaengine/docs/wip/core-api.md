# Core API for scripts

Work in progress. Every way code in `scripts/` plugs into GameCore: a class to subclass, an
attribute to put on something, or a method with a special name. Most of it is picked up by
`ClassManager` (`GameCore/CompiledScripts/ClassManager.cs`) when the scripts assembly loads,
or by the Roslyn generators at compile time.

Links point to the current docs where a page exists; otherwise to the code.

[gen]: ../current/02-engine-core/code%20generators/scripting-generators.md

## Content types and definitions

| What | Description | Docs |
|---|---|---|
| `[ThingTemplate]` / `[ThingDefTemplate]` on a partial class | A new item or character class and its def class; the generator writes the boilerplate. | [Generators §1][gen], [Thing model](../current/02-engine-core/thing-model.md) |
| `[PluginTemplate]` / `[PluginDefTemplate]` on a partial class | A plugin and its def: an object attached to a thing that hears its triggers and saves with it. | [Plugins](../current/02-engine-core/plugins.md), [Generators §1][gen] |
| `[DefField]` | A def field read from the `.def` file and saved like any def value. | [Generators §1][gen], [Persistence](../current/02-engine-core/persistence-and-saving.md) |
| Subclass `AbstractDef` (or `AbstractIndexedDef<,>`, `AbstractSkillDef`) | A new kind of `[Header]` block in `.def`/`.scp` files. | [Persistence](../current/02-engine-core/persistence-and-saving.md) |
| A static def field on its own def class (`public static XDef instance = new XDef(...).Register()`) | A def made in code instead of a script file; it must exist before the world loads. | `ClassManager.CheckCodeMadeDefs` |
| Implement `IFieldValueParser` | Parses a custom value type in def fields and saves. | `GameCore/FieldValue.cs` |

## Functions, triggers, dialogs

| What | Description | Docs |
|---|---|---|
| `[SteamFunction]` on a static method | A function callable by name from LScript and commands. | [Generators §2][gen] |
| Subclass `CompiledScriptHolder` | The hand-written form of a named script function. | [Generators §2][gen] |
| Subclass `CompiledTriggerGroup` | A trigger group written in C#: `on_*` methods respond to triggers. | [Generators §3][gen], [Trigger system](../current/02-engine-core/trigger-system-architecture.md) |
| Subclass `GroundTileType` | A trigger group standing for a kind of map ground tile (`t_rock`, `t_grass`, ...). | `GameCore/Regions/Map.cs` |
| Override `On_*` trigger methods on a thing class | Handle a trigger in the class itself; `On_Deny*` can cancel the action. | [Trigger system](../current/02-engine-core/trigger-system-architecture.md#10-deny-results-and-the-default-check) |
| `public static void RegisterTriggerGroup(TriggerGroup)` on a `TagHolder` subclass | Lets a trigger group be attached to every instance of that class. | [Trigger system](../current/02-engine-core/trigger-system-architecture.md) |
| Subclass `CompiledGumpDef` | A dialog (gump) written in C#. | [Dialogs framework](../current/03-admin-tooling/dialogs-framework.md) |
| Subclass `CompiledSpeech` | NPC speech handling written in C#. | [NPC speech](../current/04-gameplay-systems/npc-speech.md) |

## State: tags, timers, regions

| What | Description | Docs |
|---|---|---|
| Subclass `TagHolder` / use `TagKey` | An object that carries tags and timers and can be saved. | [Tags & timers](../current/02-engine-core/tags-and-timers.md) |
| Subclass `Timer` / `BoundTimer` | A timer; saved with its holder like a tag. | [Tags & timers](../current/02-engine-core/tags-and-timers.md) |
| Subclass `Region` / `StaticRegion` | A map area with its own rules and triggers. | [Regions](../current/02-engine-core/regions.md) |

## Saving, copying, viewing

| What | Description | Docs |
|---|---|---|
| `[SaveableClass]` with `[SaveableData]`, `[LoadingInitializer]`, `[LoadingFinalizer]`, `[LoadSection]`, `[LoadLine]`, `[Save]` | Saves and loads an object that isn't a thing; the generator writes the save implementor. | [Generators §4][gen], [Persistence](../current/02-engine-core/persistence-and-saving.md) |
| Implement `ISaveImplementor` / `ISimpleSaveImplementor` | The hand-written form of the above. | [Persistence](../current/02-engine-core/persistence-and-saving.md) |
| `[DeepCopyableClass]` with `[CopyableData]`, `[DeepCopyImplementation]` | Copies an object when its holder is duplicated; the generator writes the copy implementor. | [Generators §5][gen] |
| Implement `IDeepCopyImplementor` | The hand-written form of the above. | [Generators §5][gen] |
| `[ViewableClass]` with `[NoShow]`, `[InfoField]`, `[Button]`, `[ViewDescriptor]`, `[GetMethod]`, `[SetMethod]` | Shows and edits an object's fields in the `.info` dialog. | [Info & settings dialogs](../current/03-admin-tooling/info-and-settings-dialogs.md) |

## Localization

| What | Description | Docs |
|---|---|---|
| Subclass `CompiledLocStringCollection<T>` | A set of translatable messages, overridable per language from `Languages/`. | [Localization](../current/05-localization.md) |

## Lifecycle and tests

| What | Description | Docs |
|---|---|---|
| `public static void Bootstrap()` | Runs first, right after the scripts assembly loads: register hooks and code-made defs. Static initializers of that class run before it. | `ClassManager.InitClasses` |
| `public static void Init()` | Runs after every class is registered, at the end of startup and of each recompile. | `ClassManager.InitScripts` |
| `[RegisterWithRunTests]` on a public static method | Adds the method to the in-server test suite. | `Common/TestSuite.cs` |
