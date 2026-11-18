# Compiled TriggerGroup generator

## Why this exists

`CompiledTriggerGroup` classes rely on generated dispatch glue (`Run(self, tk, sa)`).
This generator emits that glue at build time from normal C# trigger classes, so trigger
dispatch is deterministic and validated during compilation instead of runtime CodeDom.

## Inputs and outputs

### Input types

- non-abstract, non-sealed classes assignable to `SteamEngine.CompiledScripts.CompiledTriggerGroup`
- public instance methods whose name starts with `on_` (case-insensitive)

### Example input

```csharp
using SteamEngine;
using SteamEngine.CompiledScripts;

public partial class E_BackupsManager_Global : CompiledTriggerGroup {
	public void On_BeforeSave(Globals self, ScriptArgs sa) {
		// ...
	}

	public object on_Command(Globals self, ISrc src, string command) {
		return null;
	}
}
```

### Generated output

For each input type, the generator emits one generated partial class part on the same type:

- class name: same as the source trigger-group type
- members:
  - `override Run(object self, TriggerKey tk, ScriptArgs sa)` dispatching by trigger name

## Authoring rules

- Trigger-group classes must be declared `partial`.
- Trigger methods are discovered by name prefix `on_`.
- Trigger names are the method name without `on_`.
- Trigger names must be unique per trigger-group type (case-insensitive).
- Invocation parameter mapping follows legacy behavior:
  - first method parameter comes from `self`
  - if the method has exactly two parameters and second is `ScriptArgs` (or subtype), second gets `sa`
  - other remaining parameters are read from `sa.Argv`

## Diagnostics

- `SEGEN402`: duplicate trigger method names on one trigger-group class.
- `SEGEN403`: trigger-group class is not declared `partial`.

Shared generator diagnostics can also appear (`SEGEN900+`) when required canonical types are missing.

## Troubleshooting

- If trigger dispatch is missing, check that the class is not abstract/sealed and inherits from `CompiledTriggerGroup`.
- If a trigger method is ignored, confirm it is public instance and starts with `on_`.
- If generation fails with `SEGEN402`, rename colliding `on_` methods so trigger names are unique.
