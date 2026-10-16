# Dialog framework

How GM/player dialogs ("gumps," in UO protocol terms) get built in C#, how a click comes
back, and the builder API most new dialogs should actually use rather than the raw
low-level primitives.

## The base contract: `CompiledGumpDef`

Every C#-authored dialog is a subclass of `CompiledGumpDef`
(`GameCore/CompiledScripts/CompiledGump.cs`), which declares two abstract methods a
subclass must implement:

```csharp
public abstract void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args);
public abstract void OnResponse(Gump gi, GumpResponse gr, DialogArgs args);
```

- **`Construct`** builds the layout - every `Add*` call in this method appends to an
  internal `Gump` instance (`this.GumpInstance`), which gets compiled into the real wire
  packet once `Construct` returns.
- **`OnResponse`** is called when the player interacts with an already-open instance of
  this dialog (presses a button, submits a text entry, toggles a checkbox and clicks
  something). `GumpResponse` wraps the raw response data: `PressedButton`, plus
  `IsSwitched(int id)` for checkbox/radio state and `GetTextResponse`/`GetNumberResponse`
  for text/number entry fields.

Both methods are invoked inside a `try`/catch-and-log-only wrapper
(`CompiledGumpDef.InternalConstruct`/`CompiledGump.OnResponse`) - an exception inside
either one is logged and swallowed, not surfaced to the player as an error message. This
is why a dialog that "does nothing" when it should show something, or "does nothing" when
a button is clicked, is worth checking the server log for a swallowed exception before
assuming the dispatch itself never happened (see [[gm-commands]]'s command/property
collision trap for a different, unrelated way a handler can silently never run at all).

**`DialogArgs`** (`GameCore/Gump.cs:47`) is the one object that survives the round trip
between opening a dialog and handling its response - it's a `TagHolder` (so it carries
arbitrary tagged data, e.g. "which item is this dialog about") plus a positional
`object[]` accessed via `args[i]`, used by the raw-LScript-facing dialog path. A
C#-authored dialog almost always uses the tag side: stash a reference under an
instance-scoped `TagKey` in `Construct`, read it back by the same key in `OnResponse`.

## `SingletonScript<T>` - get the one instance of a dialog/target class

`CompiledGumpDef`/`CompiledTargetDef` subclasses are registered once as `AbstractScript`
instances at boot and never need per-call instantiation - `SingletonScript<T>.Instance`
(`scripts/Utils/SingletonScript.cs`) finds and caches that one instance via a linear scan
over `AbstractScript.AllScripts`, throwing a `FatalException` if the type either has no
instance or (a real safety check) has more than one. Every `viewer.Dialog(...)` or
`self.Target(...)` call in this codebase reaches its target class through this helper
rather than `new`-ing anything.

## `ImprovedDialog`/`GUTATable`/widget builders - the preferred way to build a layout

Rather than call `CompiledGumpDef`'s raw drawing primitives (`AddButton`, `AddText`,
`AddCheckBox`, etc. - a 1:1 wrapper around the wire-level gump opcodes, still present for
backward compatibility with older/LScript-facing dialogs) directly, most dialogs written
against this engine's own content use `ImprovedDialog`
(`scripts/Basic/dialogs/ImprovedDialog.cs`) and its `GUTA*` component tree
(`scripts/Basic/dialogs/GumpComponents.cs`) - a small grid layout system on top of the raw
primitives:

- `dlg.AddTable(new GUTATable(rowCount, colWidth0, colWidth1, ...))` starts a new
  row/column grid; `0` as a column width means "stretch to fill remaining space."
- `dlg.LastTable[row, col] = <SomeWidget>.Builder....Build()` places a widget
  (`GUTAText`, `GUTAButton`, `GUTACheckBox`, `GUTAInput`, etc, all in
  `GumpComponents.cs`) into a specific grid cell via a fluent builder.
- `dlg.MakeLastTableTransparent()` is a near-universal finishing touch - without it each
  table paints its own opaque background tile.
- A table's row count is fixed at construction - a dialog needing a variable number of
  rows (e.g. one row per configured item, unknown count ahead of time) builds one
  separate 1-row `GUTATable` per row instead of trying to resize an existing one (the
  weighted multi-monster spawn-list dialog does exactly this, see
  `scripts/Basic/Spawning/spawn.scp`).

### Worked example: `D_AttrList`

`D_AttrList` (`scripts/Basic/administration/attributes/D_AttrList.cs`, #217) is a
representative, fully real example - a GM checkbox dialog listing every item `ATTR` flag
(see [[../04-gameplay-systems/item-attr-flags|Item ATTR flags]]), modeled on the
pre-existing `D_TagList`/`D_PluginList` dialogs:

```csharp
public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
    Item item = (Item) args.GetTag(holderTK);

    ImprovedDialog dlg = new ImprovedDialog(this.GumpInstance);
    dlg.CreateBackground(width);
    dlg.SetLocation(50, 50);

    dlg.AddTable(new GUTATable(1, 0, ButtonMetrics.D_BUTTON_WIDTH));
    dlg.LastTable[0, 0] = GUTAText.Builder.TextHeadline("Attributes - " + item.Name).Build();
    dlg.LastTable[0, 1] = GUTAButton.Builder.Type(LeafComponentTypes.ButtonCross).Id(0).Build();
    dlg.MakeLastTableTransparent();

    dlg.AddTable(new GUTATable(allFlags.Length, ButtonMetrics.D_BUTTON_WIDTH, 0));
    for (int i = 0; i < allFlags.Length; i++) {
        ItemAttributes flag = allFlags[i];
        dlg.LastTable[i, 0] = GUTACheckBox.Builder.Id(10 + i).Checked(item.HasAttribute(flag)).Build();
        dlg.LastTable[i, 1] = GUTAText.Builder.TextLabel(flag.ToString()).Build();
    }
    dlg.MakeLastTableTransparent();
    ...
    dlg.WriteOut();
}

public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
    if (gr.PressedButton == 0) { //exit without saving
        DialogStacking.ShowPreviousDialog(gi);
        return;
    }
    Item item = (Item) args.GetTag(holderTK);
    for (int i = 0; i < allFlags.Length; i++) {
        item.SetAttribute(allFlags[i], gr.IsSwitched(10 + i));
    }
    DialogStacking.ShowPreviousDialog(gi);
}
```

Points worth internalizing from this example, all real, not stylistic preference:

- **`args.GetTag(holderTK)`** is how the dialog knows *which item* it's about - set once
  by the static `ShowFor(Player viewer, Item item)` helper that both entry points (the
  `.AttrList` target-cursor command and an "Attributes" button wired into the generic
  `.info` dialog) call through, rather than each entry point duplicating the
  `DialogArgs`-construction logic.
- **Button id `0` is the real UO client's right-click-close signal**, not a custom id a
  layout chooses - `OnResponse` must special-case it (typically "discard and go back")
  separately from whatever id a dialog's own actual "confirm" button uses (`1` here).
  Forgetting this case means right-click-close either does nothing or gets misread as a
  real confirm.
- **One checkbox id per flag, offset by a base (`10 + i`)** - a common idiom for
  "N dynamically-generated rows of the same widget kind" - keeps ids out of the way of
  any fixed-id buttons elsewhere in the same dialog (`0`/`1` here).
- `item.SetAttribute(allFlags[i], gr.IsSwitched(10 + i))` is unconditional for every flag
  on confirm - every checkbox's current on-screen state becomes the item's new state in
  one pass, not an incremental diff against what was checked when the dialog opened.

## Dialog stacking: "go back" without hand-rolled state

`DialogStacking.ShowPreviousDialog(gi)` (`scripts/Basic/dialogs/DialogStacking.cs`) is the
standard way a dialog returns to whatever dialog opened it - e.g. closing `D_AttrList`
to reopen the generic `.info` dialog it was launched from. A per-`GameState` stack of
previously-shown `Gump` instances is maintained internally
(`DialogStacking.EnstackDialog`, called automatically when one dialog opens another); a
dialog's own `OnResponse` doesn't need to know or care what it's "returning to" - it just
calls `ShowPreviousDialog` and the stack resolves it.

## See also

- [GM commands](gm-commands.md) - the target-cursor and multi-step command shapes that
  often lead into opening a dialog (`.AttrList`'s own entry point is exactly this).
- [Item ATTR flags](../04-gameplay-systems/item-attr-flags.md) - `D_AttrList`'s actual
  gameplay purpose and the flag system it edits.
- [Networking & the protocol layer](../02-engine-core/networking-protocol.md) - the
  0xB1 `GumpMenuSelectionInPacket` wire format `GumpResponse` is built from.
