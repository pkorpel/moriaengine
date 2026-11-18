# Compiled ScriptHolder `Generator`

`SteamEngine.Generators.CompiledScriptHolders.Generator` is the build-time generator for
`[SteamFunction]` dispatch classes.

## Why we need this generator

- lets script authors add functions with one attribute instead of writing holder classes manually,
- validates function declarations at compile time,
- keeps dispatch registration deterministic and consistent across builds.

## How to use it

Add `[SteamFunction]` to a `public static` method:

```csharp
using SteamEngine.CompiledScripts;

public static class AdminCommands {
	/// <summary>Reveal hidden entities around self.</summary>
	[SteamFunction("reveal")]
	public static int Reveal(object self, int range) {
		return range;
	}
}
```

The generated holder is created automatically and registered as a script function (`"reveal"` in
the example).

## Authoring rules that matter

- Method must be `public static`.
- Function name is:
  - `SteamFunction("name")` when non-empty, otherwise
  - method name.
- Function names are globally unique, case-insensitive.
- First parameter maps from `self`; remaining parameters map from `ScriptArgs.Argv`.
- Optional method XML docs become generated function description text.

## Current diagnostics

- `SEGEN101`: `[SteamFunction]` target is not `public static`.
- `SEGEN102`: duplicate function names (case-insensitive).
- `SEGEN103`: required `SteamFunctionAttribute` declaration is missing.
- `SEGEN104`: shadowed non-canonical `SteamFunction` attribute symbol.

If generation fails, fix the reported `SEGEN10x` diagnostics and rebuild.
