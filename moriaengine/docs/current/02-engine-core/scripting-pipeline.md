# Scripting & code generation pipeline

moriaengine content is split across three genuinely different file roles, plus two
*different* code-generation mechanisms that turn parts of it into real C#. Both run at
every boot.

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
`scripts/` at boot and writes one `<file name>.Generated.cs` per `.ct` file, right next to
it, holding every `[ThingTemplate ...]` and `[PluginTemplate ...]` section of that file (e.g.
`Item.ct` → `Item.Generated.cs`; `skills/Combat/Plugins.ct` → `Plugins.Generated.cs` with two
plugins). This is a different place from the central `moriaengine/Generated/` folder - see
mechanism 2 below.

A `.ct` file has two sections:

```
[ThingTemplate Item : AbstractItem]
Def:	//vars on the def
	public ResourcesList resources = null
	...
	public ResourcesList skillMake = null
	//how hard it is to craft this item (value compared against the skill (1000=100.0)
	public int difficulty = 1000
	public LightType light = (LightType) 0
Vars:	//vars defined in the class, so they basically do what tags do, but better
```

(`scripts/Basic/ItemClasses/Item.ct` as the original engine had it). For each `Def:` field
the generated def constructor calls `this.InitTypedField(name, defaultValue, type)` and the
def gets a property wrapping that `FieldValue` (see
[persistence-and-saving.md](persistence-and-saving.md)), so a `.def` line `DIFFICULTY = 500`
sets `ItemDef.Difficulty` with no parsing code. The field is named for what it is: every def
value is the default for things made from that def, so a `Def:` field's name never says
"default". For each `Vars:` field the generated code declares the field on the thing's class
with its save, load and deep-copy code.

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
and third) or not (second). `Item`'s Sphere `ATTR` flags (#215/#217) are the second kind: a
`Def:` field and a `Vars:` field, both called `attributes`, the instance's copied in
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

### One boot does both

A boot runs, in order (`Main.cs:171-173`, `CompilerInvoker.CompileScripts`):

1. `ClassTemplateParser.Init()` writes every `.ct`'s `X.Generated.cs` into `scripts/`;
2. the scripts project is compiled and loaded, and `ClassManager` reflects over it, feeding
   each generator;
3. `GeneratedCodeUtil.WriteOutAndCompile` writes the six `Generated/*.Generated.cs` files and
   compiles the Generated project against the scripts assembly just loaded, then loads it.

So a new `[SteamFunction]`, `[ViewableClass]` member or `.ct` field is picked up by the next
boot, once. Checked 2026-11-10: with all six `Generated/*.Generated.cs` files deleted, one
boot came up, passed `BootLogTests` and wrote them again.

## Reusing the last build

Since #1358 a boot doesn't compile what hasn't changed. Two parts:

- **Generated sources are written only when their text changes.**
  - Both generators write through `MsBuildLauncher.WriteSourceIfChanged`: `ClassTemplateParser`'s `X.Generated.cs` in `scripts/`, and `GeneratedCodeUtil`'s files in `Generated/`.
  - Before, every boot rewrote them with the same text. The new write times made MSBuild recompile both projects every time: about 12.5 s for the scripts and 10.7 s for Generated.
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

Measured on the test servers: a boot with nothing changed reached ready in 11 s. A boot that compiled both projects took 27 s. With only the first part (MSBuild finding both projects up to date), it took 15 s.

## Building outside a boot

A plain `dotnet build` of the solution (an IDE, the test scripts, the pre-push hook) compiles
the scripts and Generated projects from the generated files already on disk. On a fresh
checkout there are none, so `build.ps1`'s `Invoke-ScriptGeneration` boots GameCore first (twice
when the first boot has to write `steamengine.ini` and exits), and again whenever a `.ct` or
`.cs` source is newer than `Generated/generated-at.txt`. After a change that removes or renames something a generated file refers to (a
`[SteamFunction]` method, a `.ct` field), the old generated file can fail such a build until
the next boot rewrites it.
