# Plugins

A plugin adds per-holder state and behaviour without changing the holder's class. It can
receive the holder's triggers, own timers, and persist with its holder. This is SteamEngine's
extension mechanism; Sphere's closest counterparts are memory items and `EVENTS`.

## Pieces and ownership

| Type | Role |
|---|---|
| `Plugin` | Attached instance; `Cont` is the holder, `Def` its definition. Also a `TagHolder`. |
| `PluginDef` | Creates plugin instances and owns shared configuration/handlers. |
| `PluginKey` | One slot on a holder, written `@@name` in scripts. |
| `PluginHolder` | Adds, finds, removes, deletes and dispatches plugins. |
| `TimerPlugin` | Adds a seconds-based `Timer` property that raises the plugin's `@timer`. |

Adding at an occupied key deletes the old plugin. Assignment raises `@assign`, removal
`@unAssign`, deletion `@destroy`; deleting a holder deletes its plugins. Removal and deletion
are distinct: choose the API matching the intended lifetime.

`AddNewPluginAsSimple` / `AddPluginAsSimple` attach without enrolling in the holder's trigger
list. A simple plugin still receives its own lifecycle and explicitly invoked events, such
as its timer; it does not hear every holder event.

## Declaring and installing a plugin

Use `[PluginTemplate]` and `[PluginDefTemplate]` for generated class/def wiring, following
an existing plugin's shape. Declare shared configuration on the def, mutable per-holder
state on the plugin, and mark saved/copied members independently.

Register the def before world loading if saves can reference it. Prefer a script def section
or a static registered instance on the def class itself. See [persistence](persistence-and-saving.md).
Use a cached `PluginKey` and `AddNewPlugin(key, def)` for normal installation; use `GetPlugin`
or `HasPlugin` when the existing instance should be retained rather than replaced.

Keep `On_Assign`/`On_UnAssign` symmetrical for subscriptions, temporary modifiers and timers.
Unassignment can happen during holder deletion, so cleanup must tolerate a holder in teardown.
Do not use installation solely to store state if a tag or field expresses the requirement.

## How triggers reach a plugin

The holder's attached groups run before its plugins. Plugins run newest first; save/load and
copy preserve the order. A plugin runs its def's scripted handler before its compiled handler.
Public `On_*` methods are collected by the runtime plugin-trigger generator and attached to
the def at `PluginDef.Init()`.

The complete Thing-level sequence, argument mapping and cancellation rules belong in
[trigger architecture](trigger-system-architecture.md). A cancel return stops cancellable
dispatch; it does not universally deny the action.

### Mutating plugins during dispatch

Handlers may remove themselves or other plugins. Removed plugins are skipped while traversal
can continue using retained next links. A newly added plugin goes at the front, behind the
current walk, and ordinarily hears the next event. Removing and re-adding an existing plugin
can make traversal revisit newer plugins. Do not assume exactly-once dispatch under arbitrary
remove/re-add mutation.

Installation order is not a stable dependency mechanism for noncommutative modifiers.
Place required formula stages at explicit points instead of relying on which plugin was added
last. Keep handlers tolerant of participants being deleted by earlier handlers.

## Timers and saving

Use `TimerPlugin` for a plugin-owned timeout event or a custom bound timer for more specialised
work. Timer callbacks have no ambient sender; pass actors explicitly. See [timers](tags-and-timers.md).

Saved holder references reconnect plugin instances and their defs. Plugin state still requires
save support and its named def must exist before loading. Compiled handlers attach after world
load; avoid depending on their availability in loading hooks.

## When to use a plugin

| Requirement | Prefer |
|---|---|
| Optional stateful effect reacting to events | Plugin. |
| Shared stateless policy | Trigger group. |
| One delayed action with no event reactions | Bound timer. |
| Sparse value with no lifecycle | Tag. |
| Behaviour intrinsic to every instance of a class | Class member/override. |

For event contracts use the separate [legacy](trigger-catalog.md) and
[new](new-trigger-catalog.md) catalogs. Keep gameplay-specific trigger lists there rather
than expanding this authoring guide with every new consumer.

## Sources and examples

[Plugin](../../../GameCore/Plugin.cs), [PluginDef](../../../GameCore/PluginDef.cs),
[PluginHolder](../../../GameCore/PluginHolder.cs),
[TimerPlugin](../../../scripts/Basic/TimerPlugin.cs),
[RegenerationPlugin](../../../scripts/Basic/Regeneration/RegenerationPlugin.cs).
