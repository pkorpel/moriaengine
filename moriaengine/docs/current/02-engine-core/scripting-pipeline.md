# Scripting and code generation pipeline

## Inputs

| Input | Purpose |
|---|---|
| Attributed C# | Class shape, behaviour, fields and generated integration. |
| `.def` | Named content definitions configuring existing classes. |
| `.scp` | LScript functions, trigger groups and dialogs. |

The extensions describe content conventions; the script loader handles their sections.
A definition block creates/configures a def instance, not a new C# type.

## Two generation phases

| Phase | Outputs |
|---|---|
| Roslyn during C# compilation | Thing/Plugin partial-class wiring, SteamFunction holders, compiled trigger groups, save implementors and deep-copy implementors. |
| Runtime type discovery and generation | `Generated/PluginTriggerGroups.Generated.cs` and `Generated/ViewableClasses.Generated.cs`. |

The second phase uses registered `ISteamCSCodeGenerator` implementations after the scripts
assembly has loaded. `ClassManager` discovers types and feeds generators;
`GeneratedCodeUtil.WriteOutAndCompile` writes/compiles the central Generated project and
loads it. Do not confuse compiled trigger-group generation with plugin-handler generation.

The [generator reference](code%20generators/generator-architecture-guidance.md) covers each
implementation. This guide describes their place in the lifecycle.

## Template authoring

`[ThingTemplate]` / `[ThingDefTemplate]` and the Plugin equivalents describe partial classes.
`[DefField]` supplies typed definition storage; `[SaveableData]` and `[CopyableData]`
independently opt instance members into persistence and copying.

```csharp
[ThingTemplate]
public partial class Item : AbstractItem {
    [SaveableData, CopyableData]
    private int amount = 1;
}
```

This is the shape of an existing template, not an additional class to paste into the project.
Follow the [Thing/Plugin generator contract](code%20generators/thing-and-plugin-classes-generator.md)
for supported fields and generated members. Choose def/instance ownership using the
[Thing model](thing-model.md), and persistence using the [saving guide](persistence-and-saving.md).

## Startup and build reuse

Startup builds or reuses the scripts assembly, loads its types, generates remaining runtime
sources, then builds or reuses and loads Generated. Definitions and world data load afterward;
see [runtime lifecycle](runtime-lifecycle.md).

Generated sources are written only if text changes. `BuildFingerprint` hashes project source
and build inputs, inherited build settings and relevant engine assemblies. Generated's
fingerprint also includes the scripts assembly MVID. Build stamps validate the output path,
length and write time as well as the fingerprint. A matching stamp avoids launching `dotnet`.

Failed builds invalidate the stamp. External builds, changed inputs or a new assembly number
on in-game recompilation require revalidation/building. Failure to read inputs or write a
stamp falls back to a normal build. Generated builds with project references disabled so it
uses the scripts assembly already loaded rather than rebuilding that dependency.

Do not assume every boot recompiles everything, or that an arbitrary existing DLL is reusable.
The boot log distinguishes reuse from compilation.

## Building outside startup

An ordinary solution build consumes runtime-generated files already on disk. `build.ps1`
boots GameCore to produce them when missing or stale; initial configuration creation can
require a second boot. `Generated/generated-at.txt` records generation freshness because
unchanged source files keep their timestamps.

After renaming/removing symbols used by old generated files, regenerate through the supported
build/startup path. Do not hand-fix generated output. Roslyn outputs belong to the compilation;
the two central runtime files have a different lifecycle.

## Sources

[CompilerInvoker](../../../GameCore/CompiledScripts/CompilerInvoker.cs),
[GeneratedCodeUtil](../../../GameCore/CompiledScripts/GeneratedCodeUtil.cs),
[ClassManager](../../../GameCore/CompiledScripts/ClassManager.cs),
[BuildFingerprint](../../../GameCore/CompiledScripts/BuildFingerprint.cs).
