# Trigger system architecture

Triggers are named extension points: the raising code supplies a receiver, key and arguments,
dispatches handlers, then decides how to continue. The dispatch mechanism is shared by the
[legacy catalog](trigger-catalog.md) and [new catalog](new-trigger-catalog.md).

## Storage and behaviour

| Type | Responsibility |
|---|---|
| `TagHolder` | Keyed state and bound timers. |
| `PluginHolder` | Trigger groups, plugins and dispatch. |
| `TriggerGroup` | Shared behaviour attached by reference. |
| `Plugin` | Per-holder state and behaviour, with assignment/removal and persistence. |

Use a trigger group for shared policy without per-holder state, a plugin for an optional
effect or process, and a plain bound timer for delayed work that needs no event handlers.
Inherent class behaviour can remain on the class.

## Dispatch APIs

| API | Cancel return | Ordinary handler exception |
|---|---|---|
| `Trigger` | Does not stop dispatch. | Propagates. |
| `TryTrigger` | Does not stop dispatch. | Logged by dispatch handling. |
| `CancellableTrigger` | Stops at `1` / `TriggerResult.Cancel`. | Propagates. |
| `TryCancellableTrigger` | Stops at cancel. | Logged by dispatch handling. |

Fatal errors are not ordinary recoverable handler exceptions. `Try*` does not roll back
mutations or automatically protect a separate virtual `On_*` call made by the caller.

Cancellation stops a handler chain. The caller may interpret it as refusal, an already
handled action, or replacement of default checks. Read [deny chains](triggers-and-deny-chains.md).

## Handler order

A Thing dispatches through these layers:

1. Class-wide trigger groups (for an item, item groups before all-Thing groups).
2. Attached trigger groups (`EVENTS`).
3. Plugins, newest first.
4. Its def's scripted handlers and `TEVENTS` groups.
5. For an item, its `TYPE` handlers.

Cancellation can skip later layers. A plugin runs its def's scripted handler before its
compiled handler. `PluginDef.Init()` attaches the compiled handler generated from public
`On_*` methods.

Virtual `On_*` methods on engine objects are separate extension points invoked by the
surrounding `Trigger_*` method. Calling `TryTrigger` alone does not invoke all same-named
virtual methods. Preserve the ordering of each raising method.

## Arguments and compiled handlers

`ScriptArgs` can itself be a specialised payload, or contain a typed object in `Argv`.
Those shapes are not interchangeable.

Compiled trigger-group dispatch maps the first parameter from `self`. The special
exactly-two-parameter form with a `ScriptArgs`-compatible second parameter receives the
argument object; other remaining parameters come from `Argv`. Compiled plugin methods
have corresponding mapping without a trigger-group `self` parameter. Copy a working
handler for the actual event and check the raising call.

An actor in the payload is more reliable than ambient `Globals.Src`, which is not established
by every operation. In particular, timer callbacks have no ambient sender.

## Adding or changing a trigger

Document receiver, phase, argument wrapper, mutable outputs and cancel meaning. State which
participants may be deleted or moved by handlers, and revalidate them before continuing.
Keep virtual-hook exception handling consistent with neighbouring code. Verify cancellation,
normal continuation and mutation-sensitive traversal.

Notification, permission, implementation and calculation describe intent, not enforced
categories. An event called “success” may use cancellable dispatch to replace default work.
Changing dispatch order or cancel semantics is a script compatibility change.

## Sources

[PluginHolder](../../../GameCore/PluginHolder.cs), [Plugin](../../../GameCore/Plugin.cs),
[Thing](../../../GameCore/Thing.cs), [AbstractItem](../../../GameCore/AbstractItem.cs),
[PluginTriggerGroupGenerator](../../../GameCore/PluginTriggerGroupGenerator.cs),
[ThingManipulationTriggers](../../../GameCore/ThingManipulationTriggers.cs).
