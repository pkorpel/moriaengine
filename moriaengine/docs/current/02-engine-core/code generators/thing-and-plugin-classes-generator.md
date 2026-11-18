# ThingAndPluginClasses `Generator`

`SteamEngine.Generators.ThingAndPluginClasses.Generator` generates the boilerplate around
`[ThingTemplate]` / `[PluginTemplate]` pairs at build time.

## Why we need this generator

- eliminates repetitive save/load/deep-copy template glue,
- catches template mistakes during build instead of at boot,
- keeps generated behavior deterministic and visible to IDE tooling.

## How to use it

1. Define instance template class with `[ThingTemplate]` or `[PluginTemplate]`.
2. Define matching def template class with `[ThingDefTemplate]` or `[PluginDefTemplate]`.
3. Opt-in instance fields/properties with `[SaveableData]` / `[CopyableData]`.
4. Declare def fields as private `FieldValue` plus `[DefField(...)]`.

Example:

```csharp
[ThingTemplate]
public partial class ArcaneBeacon : MagicThing {
	[SaveableData, CopyableData]
	private int charge = 3;
}

[ThingDefTemplate]
public partial class ArcaneBeaconDef : MagicThingDef {
	[DefField(typeof(int), DefaultExpression = "9")]
	private FieldValue maxCharge;
}
```

From this, the generator provides the expected registration/bootstrap wiring, typed def property
(`MaxCharge`), instance save/load handling for `charge`, and deep-copy support for opted-in
members.

## Authoring rules that matter

- Template names are globally unique per family (`Thing`/`Plugin`), case-insensitive.
- `[SaveableData]` and `[CopyableData]` are opt-in: unannotated members are ignored.
- Unsupported member shapes (for example indexers, static members) produce diagnostics.
- Duplicate save/load keys (case-insensitive) produce diagnostics.

## Troubleshooting

If generation fails, fix the reported `SEGEN00x` diagnostics in the template declarations and
rebuild. Most errors are wiring/shape issues in attributes, naming, or member eligibility.

## Visual Studio view of generated files

Generated files are not written next to your source files. In Visual Studio, view them under:

- **Dependencies**
  - **Analyzers**
    - `SteamEngine.Generators`
