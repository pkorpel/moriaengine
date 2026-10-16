# Scripting & code generation pipeline

moriaengine content is split across three genuinely different file roles, plus two
*different* code-generation mechanisms that turn parts of it into real C#. Confusing one
for the other is a common source of wasted time (confirmed repeatedly across this project's
own session history) - this page exists to make the distinction unambiguous.

## The three content file types

| Extension | Role | Example |
|---|---|---|
| `.ct` | **Class templates** - declares a C# class's shape (fields, which are def-level vs instance-level) | `scripts/Basic/ItemClasses/Item.ct` |
| `.def` | **Content data** - configures one `*Def` instance's values (an `ItemDef`, `CharacterDef`, etc) | `scripts/converted/item_deed.def` |
| `.scp` | **LScript source** - trigger groups, functions, raw-LScript dialogs; real executable script code, not data | `scripts/Basic/Spawning/spawn.scp` |

A `.def` file's `[ItemDef i_something]` block *configures an instance of a class the .ct
pipeline already generated* - it never defines a new class. A `.ct` file never contains game
content, only the shape a class should have. A `.scp` file is interpreted LScript, not
compiled - see [lscript.md](lscript.md).

## Mechanism 1: `.ct` → per-class generated C# (`ClassTemplateParser`)

`GameCore/CompiledScripts/ClassTemplates/ClassTemplateParser.cs` reads every `.ct` file under
`scripts/` at boot and, for each `[ThingTemplate ClassName : BaseClassName]` section, writes
a **co-located** `<ClassName>.Generated.cs` file - sitting right next to the `.ct` file it
came from (e.g. `Item.ct` → `Item.Generated.cs`, in the same `scripts/Basic/ItemClasses/`
directory). This is a completely different output location from the central `Generated/`
folder at the repo root - see mechanism 2 below.

A `.ct` file has two sections:

```
[ThingTemplate Item : AbstractItem]
Def:    // def-level fields - become FieldValue-backed properties on ItemDef,
        // settable from .def content, shared by every instance unless overridden
    public ItemAttributes attributesDefault = ItemAttributes.none
Vars:   // instance-level fields - become plain fields on Item itself,
        // with auto-generated Save/Load code
    private ItemAttributes attributes = ItemAttributes.none
```

(real example, trimmed - see `scripts/Basic/ItemClasses/Item.ct`). For each `Def:` field,
the generated code calls `this.InitTypedField(name, defaultValue, type)` in the def's
constructor and generates a get/set property wrapping a `FieldValue` (see
[persistence-and-saving.md](persistence-and-saving.md)) - this is what lets a `.def` line
like `ATTR = attr_static|attr_magic` set `ItemDef.AttributesDefault` with zero custom parsing
code (the generic `FieldValue`/`ConvertTools.ConvertTo` pipeline already handles pipe-
separated `[Flags]` enum values for *any* `Def:` field of that shape). For each `Vars:`
field, the generated code emits the plain field declaration plus generated Save/Load/
DeepCopy support.

## Mechanism 2: attribute-scanning → central `Generated/*.Generated.cs`

A second, unrelated generation pass produces six files directly under the repo-root
`Generated/` folder:

- `CompiledScriptHolders.Generated.cs` - one generated dispatch class per `[SteamFunction]`
  method, so a chat command like `.Reveal` resolves to the right method.
- `CompiledTriggerGroups.Generated.cs` - compiled-C# trigger group dispatch.
- `PluginTriggerGroups.Generated.cs` - same idea for plugin trigger groups.
- `DecoratedClassesSaveImplementors.Generated.cs` - save/load glue for `[SaveableClass]`
  types that aren't `.ct`-generated (settings categories, hand-written timers, etc).
- `DeepCopyImplementor.Generated.cs` - deep-copy glue for `[DeepCopyableClass]` types.
- `ViewableClasses.Generated.cs` - reflection metadata backing the `.info` dialog's property
  discovery for every `[ViewableClass]` type (see
  [viewable-classes-and-info-dialog.md](viewable-classes-and-info-dialog.md)).

This is driven by `GameCore/CompiledScripts/GeneratedCodeUtil.cs`'s `WriteOutAndCompile`,
which runs a loop of registered `ISteamCSCodeGenerator` instances (one per file above - see
e.g. `CompiledScriptHolderGenerator` in `GameCore/CompiledScripts/CompiledScriptHolder.cs`).
Each generator's `WriteSources()` emits CodeDom-built C# from whatever it has already
collected via reflection; `ClassManager` (`GameCore/CompiledScripts/ClassManager.cs`) is what
actually does that reflection - after the scripts assembly is compiled, it walks
`assembly.GetTypes()` and, for example, calls `CompiledScriptHolderGenerator.AddCompiledSHType`
for every method carrying `[SteamFunctionAttribute]`.

**This is why two boots are needed to pick up a new `[SteamFunction]` or `[ViewableClass]`
member**: boot N compiles whatever scripts source exists, and *then* (only after that
compile) reflects over the result to refresh each generator's data and rewrite the six
`Generated/*.Generated.cs` files - but those freshly-rewritten files haven't been compiled
into anything yet. Boot N+1 compiles again, this time against the just-rewritten generated
files, producing a consistent, fully-aware assembly. A single boot leaves you one step behind
whatever you just added.

## The manual regeneration workflow

`build.ps1`'s `Invoke-ScriptGeneration` step automates this, but no `pwsh` exists in a
typical Linux dev sandbox, so the equivalent manual sequence (used throughout this project's
own session history) is:

1. Delete all six `Generated/*.Generated.cs` files.
2. Run `dotnet GameCore/bin/Debug/net8.0/SteamEngine.GameCore.dll`, wait for `"Init done."` /
   `"Getting started"` in its stdout, then stop it.
3. Run it again, same wait condition.
4. `dotnet build SteamEngine.sln -c Debug`.

**When this is actually needed**: adding or renaming a `[SteamFunction]` method, adding a new
public property to a `[ViewableClass]` type (so it shows up in `.info`), or anything else
that changes what the attribute-scanning pass in mechanism 2 would discover. A plain
`dotnet build` is *not* sufficient for these - the stale generated files will either fail to
reference the new member (a `CS0117`/`CS1061` compile error) or, worse, silently not expose
it at runtime while still compiling fine against old metadata.

**When a plain `dotnet build` is enough**: any change to a `.ct` file (that regenerates
automatically the next time `GameCore.dll` boots, via mechanism 1, independent of mechanism
2's two-boot requirement - though in practice, since both run during the same boot, the
"delete + boot twice" sequence above regenerates both mechanisms' output together), or any
ordinary code change that doesn't add new attribute-discoverable members.
