# Scripting & code generation pipeline

moriaengine content is split across three genuinely different input roles, plus two
*different* code-generation mechanisms that turn parts of it into real C#. Both are part of
the boot/build pipeline.

## The three input types

| Input | Role | Example |
|---|---|---|
| Attributed C# (`.cs`) | **Class templates** - declares a C# class's shape (def-level vs instance-level fields, persistence/copy participation, template wiring) | `scripts/Basic/ItemClasses/Item.cs` |
| `.def` | **Content data** - configures one `*Def` instance's values (an `ItemDef`, `CharacterDef`, etc) | `scripts/converted/item_deed.def` |
| `.scp` | **LScript source** - trigger groups, functions, raw-LScript dialogs; real executable script code, not data | `scripts/Basic/Spawning/spawn.scp` |

A `.def` file's `[ItemDef i_something]` block configures an instance of a class whose shape
the attributed C# template pipeline already generated. It never defines a new class. A
`.scp` file is interpreted LScript, not compiled - see [lscript.md](lscript.md).

## Mechanism 1: attributed template classes → generated C# glue (Thing/Plugin generator)

`SteamEngine.Generators.ThingAndPluginClasses.Generator` reads template attributes in script
C# and emits the equivalent partial-class glue during build (as Roslyn-generated source in
the compilation, not source-adjacent files). This is a different place from the central
`moriaengine/Generated/` folder - see mechanism 2 below.

Template classes are authored directly in C#, for example:

```csharp
[ThingTemplate]
public partial class Item : AbstractItem {
	[SaveableData, CopyableData]
	private int amount = 1;
}

[ThingDefTemplate]
public partial class ItemDef : AbstractItemDef {
	[DefField(typeof(int), DefaultExpression = "1000")]
	private FieldValue difficulty;
}
```

For each `[DefField]`, generated def code initializes the typed `FieldValue` and emits a
typed property wrapper (see
[persistence-and-saving.md](persistence-and-saving.md)), so a `.def` line
`DIFFICULTY = 500` sets `ItemDef.Difficulty` with no per-field parsing code. For
`[SaveableData]` / `[CopyableData]` members, generated code provides save/load/deep-copy
plumbing on the thing/plugin class.

See [ThingAndPluginClasses `Generator`](code%20generators/thing-and-plugin-classes-generator.md)
for the exact generated members and constraints.

### A def value and a thing's own value

A thing reads a value from its def in one of three ways, all in the original engine:

- **The def's value only.** The thing has no field of its own and always asks the def:
  an item's weight is its def's weight per piece times its amount (`Item.Weight`,
  `scripts/Basic/ItemClasses/Item.cs`).
- **Copied when the thing is made.** The thing has its own field, filled from the def at
  creation and changed freely after; a later change to the def doesn't touch things already
  made. `Thing`'s `model` and `color` (`this.model = myDef.Model`, `GameCore/Thing.cs`); the
  model is saved only when it differs from the def's, the colour whenever it isn't 0.
- **Falls back to the def while unset.** The thing's field stays `null` until something sets
  it, and the getter returns the def's value meanwhile: an item's `name` (`AbstractItem.Name`
  returns the def's name, or its plural for a stack, while `name` is null; saved only when it
  differs from the def's).

Pick by whether a later change to the def should reach things already in the world (first
and third) or not (second). `Item`'s Sphere `ATTR` flags (#215/#217) are the second kind:
def-level and instance-level fields both called `attributes`, with the instance copied in
`Item.On_Create`. Sphere's own `ATTR` keyword reaches the def field through a hand-written
`"attr"` alias in `AbstractItemDef.LoadScriptLine`. (The def field was `attributesDefault`
until #1382.)

## Mechanism 2: attribute-scanning → central `Generated/*.Generated.cs`

A second, unrelated generation pass produces six files directly under the
`moriaengine/Generated/` folder:

- `CompiledScriptHolders.Generated.cs` - one generated dispatch class per `[SteamFunction]`
  method, so a chat command like `.Reveal` resolves to the right method.
- `CompiledTriggerGroups.Generated.cs` - compiled-C# trigger group dispatch.
- `PluginTriggerGroups.Generated.cs` - same idea for plugin trigger groups.
- `DecoratedClassesSaveImplementors.Generated.cs` - save/load glue for `[SaveableClass]`
  types that are hand-written (settings categories, hand-written timers, etc).
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

### One boot does both

A boot runs, in order (`Main.cs:171-173`, `CompilerInvoker.CompileScripts`):

1. the scripts project is compiled and loaded (including build-time template generation);
2. `ClassManager` reflects over it, feeding each generator;
3. `GeneratedCodeUtil.WriteOutAndCompile` writes the six `Generated/*.Generated.cs` files and
   compiles the Generated project against the scripts assembly just loaded, then loads it.

So a new `[SteamFunction]`, `[ViewableClass]` member, or template field/attribute change is
picked up by the next boot, once. Checked 2026-11-10: with all six
`Generated/*.Generated.cs` files deleted, one boot came up, passed `BootLogTests` and wrote
them again.

## Reusing the last build

Since #1358 a boot doesn't compile what hasn't changed. Two parts:

- **Generated sources are written only when their text changes.**
  - `GeneratedCodeUtil` generators write through `MsBuildLauncher.WriteSourceIfChanged`.
  - Before, every boot rewrote them with the same text. The new write times made MSBuild
    recompile both projects every time: about 12.5 s for the scripts and 10.7 s for Generated.
- **A fingerprint of each project's inputs.**
  - `BuildFingerprint.Of` hashes the content of every `.cs`, `.resx`, `.csproj`, `.props` and `.targets` file below the project. The top-level `bin/` and `obj/` folders and hidden folders are left out, as the SDK's default globs do.
  - It also hashes any `Directory.Build.*` files above the project, the `SteamEngine.*.dll` files the running GameCore was started from, the build type and the assembly number.
  - Generated's fingerprint also covers the scripts assembly's MVID.
  - `MsBuildLauncher.Compile` keeps it in `obj/<Configuration>/moria-build-stamp.<number>.txt`, next to the output's path, length and write time. While the stamp still matches, the output is loaded and `dotnet` doesn't run. That saves about 3 s per project even when MSBuild would find the project up to date.
  - A failed build deletes the stamp. An output rebuilt by anything else (an IDE, a plain `dotnet build`) no longer matches the stamp. A stamp pointing outside its project (a tree copied with its `obj/`) doesn't count. An in-game recompile has a new assembly number, so it always builds.
  - An input that can't be read, or a stamp that can't be written, only means a normal build.
  - Generated builds with `-p:BuildProjectReferences=false`. The scripts assembly it references is the one just loaded, and building that reference again could rewrite the loaded file.
  - Since the generated sources keep their write times, `build.ps1`'s "is generation current" check reads `Generated/generated-at.txt` instead, which `GeneratedCodeUtil` touches every time it generates.
  - The boot log says which happened: "Reused compiled C# scripts, no source changed." or "Done compiling C# scripts.", and the same for Generated.

Measured on the test servers: a boot with nothing changed reached ready in 11 s. A boot that
compiled both projects took 27 s. With only the first part (MSBuild finding both projects up
to date), it took 15 s.

## Building outside a boot

A plain `dotnet build` of the solution (an IDE, the test scripts, the pre-push hook) compiles
the scripts and Generated projects from the generated files already on disk. On a fresh
checkout there are none, so `build.ps1`'s `Invoke-ScriptGeneration` boots GameCore first (twice
when the first boot has to write `steamengine.ini` and exits), and again whenever a `.cs`
source is newer than `Generated/generated-at.txt`. After a change that removes or renames something a generated file refers to (a
`[SteamFunction]` method, a generated field/property), the old generated file can fail such a
build until the next boot rewrites it.
