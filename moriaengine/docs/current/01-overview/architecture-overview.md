# Architecture overview

moriaengine is a .NET 8 Ultima Online server derived from SteamEngine, with converted Erebor
content. Engine mechanics, compiled gameplay and interpreted content are separate layers.

## Processes and layers

| Component | Responsibility |
|---|---|
| AuxiliaryServer | Account login, server list and redirect to a game server. |
| GameCore | World process: entities, networking, timers, persistence and script hosting. |
| `scripts/` assembly | Gameplay classes, plugins, skills, NPC behaviour and administration. |
| `.def` / `.scp` content | Named definitions, interpreted triggers/functions and dialogs. |
| Communication / Common | Shared transport and utility support. |
| RemoteConsole / ProcessRestarter | Administration and process supervision tools. |
| SphereShardConverter | Converts archive content; not part of normal gameplay execution. |

Clients authenticate with AuxiliaryServer and reconnect to GameCore for character selection
and gameplay. The processes communicate through the auxiliary pipe. See
[networking](../02-engine-core/networking-protocol.md) for session details.

## World state

There is no single World instance holding all state. Things, accounts, maps, definitions,
connections and timers have separate process-wide registries. `Globals.Instance` owns global
script data; replacing it does not replace the entire world.

Network callbacks and timer work mutate the world under `MainClass.globalLock`. This provides
one mutation boundary across several threads, not a guarantee that every operation runs on
one event-loop thread. Background save writing consumes already-formatted buffers.

The [runtime lifecycle guide](../02-engine-core/runtime-lifecycle.md) is the canonical reference
for ownership, initialisation, clearing, recompilation and shutdown.

## Content model

A C# gameplay class defines behaviour and schema. Its `*Def` instances configure named content
types. World instances reference those definitions and keep their own mutable state.

For example, one item class can support many item definitions, with many world items created
from each. Def-only values, copied defaults and instance overrides have different update
semantics; see the [Thing model](../02-engine-core/thing-model.md).

Triggers provide named extension points. Shared trigger groups attach behaviour; plugins
add optional per-holder state and reactions. [Trigger architecture](../02-engine-core/trigger-system-architecture.md)
explains dispatch; [plugins](../02-engine-core/plugins.md) explains authoring.

## Boot sequence

Startup builds or reuses the compiled scripts, discovers their types and produces the two
remaining runtime-generated outputs. Definitions load before the saved world. Maps, scripts
and compiled plugin handlers are then initialised, startup hooks run and timers begin.

Roslyn generators run during compilation; runtime generators use loaded types. They are
separate phases. Do not assume each boot recompiles unchanged inputs or that script resync
hot-patches every live object. See [scripting pipeline](../02-engine-core/scripting-pipeline.md).

## Persistence and development

Saves are textual object graphs with UIDs and named definition references. Saving is opt-in,
and saved overrides coexist with script defaults. See [persistence](../02-engine-core/persistence-and-saving.md).

Start with the [repo tour](repo-structure.md) for locations and the
[documentation index](../README.md) for task-based reading routes. Dated research belongs in
[history](../08-history/research/README.md); current contracts belong in the relevant guide.

Sources: [Main](../../../GameCore/Main.cs),
[CompilerInvoker](../../../GameCore/CompiledScripts/CompilerInvoker.cs),
[Thing](../../../GameCore/Thing.cs), [Globals](../../../GameCore/Globals.cs).
