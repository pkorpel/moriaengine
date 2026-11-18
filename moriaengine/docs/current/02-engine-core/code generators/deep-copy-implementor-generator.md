# DeepCopyImplementor `Generator`

`SteamEngine.Generators.DeepCopyImplementors.Generator` provides build-time deep-copy wiring for
classes marked with `[DeepCopyableClass]`.

## Why use it

- removes manual deep-copy boilerplate for decorated classes,
- surfaces invalid deep-copy declarations during compile (instead of later at runtime),
- keeps generated deep-copy behavior deterministic across builds.

## How to author a deep-copyable class

1. Mark the class with `[DeepCopyableClass]`.
2. Mark exactly one constructor or static method with `[DeepCopyImplementation]`.
3. Mark members that must be copied with `[CopyableData]`.

Example:

```csharp
[DeepCopyableClass]
public sealed class SpellState {
	[CopyableData]
	public int Charges;

	[CopyableData]
	public object[] Payload { get; set; } = Array.Empty<object>();

	[DeepCopyImplementation]
	public SpellState(SpellState copyFrom) {
	}
}
```

## Rules you must satisfy

- `[DeepCopyImplementation]` must appear exactly once per `[DeepCopyableClass]`.
- `[DeepCopyImplementation]` target must be:
  - a constructor, or
  - a static method whose return type is assignable from the declaring type.
- The deep-copy initializer can have:
  - zero parameters, or
  - one parameter of the declaring type.
- `[CopyableData]` can be used only on:
  - public instance fields, or
  - public instance non-indexer properties with public getter and setter.
- Deep-copyable type names must be globally unique (case-insensitive).

## Troubleshooting

When rules are violated, the generator reports compile-time diagnostics in the `SEGEN20x` range.
Fix the reported member/type declaration and rebuild.
