# ThingAndPluginClasses `Generator`

`SteamEngine.Generators.ThingAndPluginClasses.Generator` replaces runtime class-template generation for
`[ThingTemplate]` and `[PluginTemplate]` with build-time Roslyn generation.

For reusable conventions that should guide other generator work, see
[`generator-architecture-guidance.md`](./generator-architecture-guidance.md).

## Why we need this generator

- **Build-time feedback**: template wiring errors surface during normal compile, not at server boot.
- **IDE support**: generated members are visible to the compiler and navigation features while editing.
- **Deterministic output**: the same source inputs produce the same generated partials every build.
- **Runtime simplification**: less generator logic has to run during script/runtime initialization.

## How it works

The generator runs as an analyzer in `scripts/SteamEngine.Scripts.csproj` via a project reference
to `Tools/SteamEngine.Generators` with `OutputItemType="Analyzer"`.

During compilation it:

1. Finds classes marked with template attributes (`ThingTemplate`, `ThingDefTemplate`,
   `PluginTemplate`, `PluginDefTemplate`).
2. Pairs instance/def classes by naming convention (`Xxx` + `XxxDef`), even when only one side
   is authored.
3. Emits partial classes that provide legacy class-template behavior.

The generated source is injected directly into the current compilation; generation is no longer
a runtime file-emission step.

## How fields are authored

- **Instance members are opt-in**. Only fields/properties explicitly annotated with
  `[SaveableData]` and/or `[CopyableData]` on the template class
  (`[ThingTemplate]` / `[PluginTemplate]`) participate in generated persistence/copy logic.
  Unannotated members are ignored by the generator.
- **Def class fields** are authored as private `FieldValue` fields on the def template class
  (`[ThingDefTemplate]` / `[PluginDefTemplate]`) and must use `[DefField(...)]`, for example:
  - `[DefField(typeof(int), DefaultExpression = "5")]`
  - `private FieldValue pulseSeconds;`
  From this, the generator emits constructor initialization through `InitTypedField(...)`
  and a typed property wrapper (for example `int PulseSeconds`).

## What it generates

- **Def partials**:
  - `Bootstrap()` registration (`ThingDef.RegisterThingDef` / `PluginDef.RegisterPluginDef`)
  - typed field initialization from `[DefField]`
  - typed property wrappers for def fields
  - `CreateImpl()` wiring
- **Instance partials**:
  - `TypeDef` helper
  - required constructor (`Thing` takes def, `Plugin` gets loading initializer ctor)
  - optional copy constructor with deep-copy wiring (or omitted when `Copy = false`)
  - `Save`/`LoadLine` for template-managed fields
  - delayed load/copy helper methods when needed
  - parity attributes such as `DeepCopyableClass`, `SaveableClass`, and copy implementation markers

## Visual Studio view of generated files

The generator does not maintain source-adjacent `*.Generated.cs` files anymore.
In Visual Studio, generated files are visible under the scripts project’s:

- **Dependencies**
  - **Analyzers**
    - `SteamEngine.Generators`

and can be opened from there when navigating generated symbols.
