# Compiled ScriptHolder `Generator`

`SteamEngine.Generators.CompiledScriptHolders.Generator` is the build-time generator for
`[SteamFunction]` dispatch classes.

For shared architecture/phasing rules used across generators, see
[`generator-architecture-guidance.md`](./generator-architecture-guidance.md).

## Why we need this generator

- **Build-time feedback** for invalid `[SteamFunction]` declarations.
- **Deterministic generated dispatch** in the scripts compilation, instead of runtime CodeDom output.
- **Simpler authoring**: script authors expose command/functions by adding `[SteamFunction]` to
  public static methods, without writing registration glue.

## Input contract

- Methods marked with `SteamEngine.CompiledScripts.SteamFunctionAttribute`.
- Valid methods must be `public static`.
- Function name comes from:
  - `SteamFunction("name")` when provided and non-empty,
  - otherwise the method name.
- Optional method XML docs are used as description text for the generated holder.

## Output contract

For each valid function, the generator emits one sealed class under
`namespace SteamEngine.CompiledScripts`:

- class name: `GeneratedScriptHolder_*` (stable, sanitized from function name),
- base type: `CompiledScriptHolder`,
- ctor: calls base with function name + description and then `RegisterAsFunction()`,
- `Run(object self, ScriptArgs sa)`: invokes the target static method with parameter mapping:
  - parameter 0 from `self`,
  - remaining parameters from `sa.Argv`,
  - special 2-parameter form `(object self, ScriptArgs sa)` passes `sa` directly,
  - numeric/date/enum parameters use `System.Convert` conversion.

## Current diagnostics

- `SEGEN101`: `[SteamFunction]` target is not `public static`.
- `SEGEN102`: duplicate function names (case-insensitive).
- `SEGEN103`: required `SteamFunctionAttribute` declaration is missing.
- `SEGEN104`: shadowed non-canonical `SteamFunction` attribute symbol.
