# Viewable classes and the `.info` dialog

`D_Info` displays and edits objects through generated data views. The same mechanism powers
`.settings`; the dialog does not hardcode every item, character or settings type.

## Discovery and generated access

`ViewableClassGenerator` discovers viewable types and descriptors during startup's type
registration/generation phase. It emits `Generated/ViewableClasses.Generated.cs` with direct
casts and member access. Opening a dialog looks up the data view and invokes those accessors;
it does not repeat reflection-based member discovery on every open.

This is one of the remaining runtime generation paths. See [scripting pipeline](scripting-pipeline.md).

## Controlling the view

| Mechanism | Effect |
|---|---|
| `[ViewableClass]` | Opts a type into a generated view; optional display label. |
| Public field/property | Included by default on a viewable type. |
| `[NoShow]` | Excludes a member. |
| `[InfoField(name)]` | Changes the displayed field label. |
| `[Button]` | Exposes a supported method as an action. |
| Read-only field / property without public setter | Read-only data view. |

Review public members when extending an already-viewable class: new public state can become
GM-editable without additional UI code. Visibility and writability do not imply persistence.
Use [persistence rules](persistence-and-saving.md) separately.

## Descriptors

A static `[ViewDescriptor(typeof(TargetType), "Label")]` class adds dialog-only behaviour
without putting administration code on the target class. `[GetMethod]` and `[SetMethod]`
supply computed fields; `[Button]` methods take the target object. The descriptor can also
suppress named fields through its `nonDisplayedFields` constructor parameter.

```csharp
[ViewDescriptor(typeof(Item), "Item")]
public static class ItemDescriptor {
    [Button("FixWeight")]
    public static void FixWeight(object target) {
        ((Item) target).FixWeight();
    }
}
```

Descriptors apply across the type hierarchy in parent-first order. A container can receive
its own controls alongside the base `TagHolder` controls.

## When to open a separate dialog

Keep simple scalar inspection in `.info`. For related toggles, collections or a multi-step
operation, add a descriptor button opening a dedicated dialog. `TagHolderDescriptor` opens
tag/timer lists; `ItemDescriptor` opens `D_AttrList`. This keeps a set of flags together
rather than expanding it into many unrelated boolean rows.

## Sources

[ViewableClassGenerator](../../../scripts/Basic/administration/InfoDialog/Generator/ViewableClassGenerator.cs),
[DataViewProvider](../../../scripts/Basic/administration/InfoDialog/DataViewProvider.cs),
[ItemDescriptor](../../../scripts/Basic/administration/Settings/implementations/Descriptors/ItemDescriptor.cs),
[settings framework](../03-admin-tooling/settings-dialog.md).
