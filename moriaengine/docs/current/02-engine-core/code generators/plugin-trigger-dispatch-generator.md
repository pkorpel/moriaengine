# Plugin trigger dispatch generator

## Why this exists

Plugin classes handle compiled triggers through `Plugin.Run(TriggerKey, ScriptArgs)`.
This generator emits `partial` overrides of that method at build time from `On_*`
methods, so plugin trigger dispatch is deterministic and validated during compilation.

## Inputs and outputs

### Input types

- non-abstract classes assignable to `SteamEngine.Plugin`
- non-static instance methods whose name starts with `On_` (case-insensitive)
  - inherited `private` methods are excluded

### Generated output

For each input type, the generator emits one generated partial class part on the same plugin:

- class name: same as the source plugin type
- members:
  - `override Run(TriggerKey tk, ScriptArgs sa)` dispatching by trigger name

## Authoring rules

- Plugin classes with `On_*` methods must be declared `partial`.
- Nested plugin classes are ignored (warning).
- Trigger names are the method name without the `On_` prefix.
- Trigger names must be unique per plugin type (case-insensitive).
- Invocation parameter mapping follows the legacy plugin trigger behavior:
  - a single `ScriptArgs` parameter receives `sa`
  - all other parameters are read from `sa.Argv[index]`
  - numeric/date/enum types use `System.Convert`, reference/value types use casts

## Diagnostics

- `SEGEN406`: plugin class is not declared `partial`.
- `SEGEN906`: nested target type is ignored.
- `SEGEN408`: duplicate trigger names on one plugin class.

Shared generator diagnostics can also appear (`SEGEN900+`) when canonical engine
types are unavailable in the compilation.

## Troubleshooting

- If plugin dispatch is missing, ensure the class is non-abstract, inherits `Plugin`,
  is not nested, and is declared `partial`.
- If a trigger method is ignored, confirm it is non-static, starts with `On_`, and is
  not an inherited `private` method.
- If generation fails with `SEGEN408`, rename colliding `On_*` methods so trigger
  names are unique.
