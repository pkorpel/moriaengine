# Save Implementor generator

## Why this exists

`[SaveableClass]` lets you mark hand-written classes for automatic persistence wiring.
The generator emits `DecoratedClassesSaveImplementor` classes at build time, so you keep
authoring in normal C# while load/save glue stays consistent and deterministic.

Use this for generic save/load classes (settings, runtime state holders, timers, etc.)
that are not `Thing`/`Plugin` template pairs.

## How to use it

Mark the type with `[SaveableClass]`, then provide:

- load path: either `[LoadingInitializer]` (constructor/static factory with no args) or
  `[LoadSection]` (constructor/static factory with `PropsSection`);
- save path: `[Save]` method and/or `[SaveableData]` members;
- optional `[LoadLine]` and `[LoadingFinalizer]` instance methods.

### Example

```csharp
using SteamEngine;
using SteamEngine.Persistence;

[SaveableClass]
public sealed class ArenaState {
	[SaveableData]
	public int Round;

	[SaveableData]
	public object Payload { get; set; }

	[LoadingInitializer]
	public ArenaState() {
	}

	[LoadLine]
	public void LoadLine(string filename, int line, string name, string value) {
		// optional custom line handling
	}

	[LoadingFinalizer]
	public void LoadingFinished() {
		// optional post-load fixups
	}

	[Save]
	public void Save(SaveStream output) {
		// optional extra saves
	}
}
```

## Authoring rules

- `[SaveableData]` must be on **public instance** fields/properties.
- Properties must have public getter and setter.
- `[LoadSection]` cannot be combined with `[LoadingInitializer]` or `[LoadLine]`.
- Saveable type names must be globally unique (case-insensitive).

## Troubleshooting

- Missing attribute/type declarations -> `SEGEN301`/`SEGEN302`.
- Invalid or duplicate attributed members -> `SEGEN303`/`SEGEN304`.
- Missing load path or invalid load combination -> `SEGEN305`/`SEGEN306`.
- Invalid `[SaveableData]` member shape -> `SEGEN307`.
- No save path (`[Save]` and no valid `[SaveableData]`) -> `SEGEN308`.
- Type-name collision between saveable classes -> `SEGEN309`.
