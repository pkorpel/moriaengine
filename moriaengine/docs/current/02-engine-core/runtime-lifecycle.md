# Runtime lifecycle and state ownership

GameCore owns a process-wide world distributed across static registries. There is no single
world instance whose replacement resets everything. `MainClass` coordinates startup, reload
and shutdown; `RunLevelManager` tracks the execution phase.

## State ownership

| State | Owner | Lifetime |
|---|---|---|
| Items and characters | `Thing`'s static `UIDArray` | Construction through deletion or world clear. |
| Accounts | `AbstractAccount`'s static account dictionary | Loaded with the world; explicitly cleared. |
| Maps and spatial indexes | Static map registry, per-map sectors | Static map data and dynamic contents have separate cleanup paths. |
| Definitions and script types | `AbstractScript`, `ThingDef`, `PluginDef`, `ClassManager` registries | Registered before world load; rebuilt on recompilation. |
| Global script data | `Globals.Instance` | Saved state; recreated on world clear. |
| Connections | `GameServer` and `GameState` | Live sessions, not persistent entities. |
| Scheduled work | Static timer queue; bound timers also belong to holders | Cleared on recompile/shutdown; supported saved timers can be restored. |

`MainClass.ClearWorld()` clears roots, caches and pending jobs. A reference retained across
a reload still points to the old object. Client links are backed up separately and resolved
against reloaded characters.

## Startup

`MainClass.Init()` runs these phases in order:

1. Set startup state, start the auxiliary pipe connection and read `steamengine.ini`.
2. `CompilerInvoker.CompileScripts(true)` builds or reuses scripts, loads their types,
   and generates/loads the remaining runtime glue.
3. Load tile, sound and multi data; run `ScriptLoader.Load()`.
4. Generate missing definitions and run `WorldSaver.Load()`.
5. Check definitions created in code with `ClassManager.CheckCodeMadeDefs()`.
6. Initialise `GameServer`, maps and scripts; register the localization bridge; initialise plugin handlers.
7. Set running state and raise `@startup(true)` on globals.
8. Run `Globals.Resync()` and start the timer thread.

A type's `Bootstrap()` runs before world load and cannot assume entities exist. Definitions
named in saves must already be registered. Script initialisation and `@startup` can use the
restored world. `PluginDef.Init()` attaches compiled plugin handlers after world load;
do not depend on those handlers while loading.

## Threads and mutation

After startup the main thread waits for the exit token. Network callbacks and the timer
thread enter game logic under `MainClass.globalLock`. They are different threads sharing
one world mutation boundary. Background save writing uses formatted buffers.

When both the global lock and a timer lock are needed, acquire the global lock first.
Reversing that order can deadlock with deletion or rescheduling from packet handling.
A concurrent registry does not make multi-object world operations thread-safe.

Timer callbacks clear `Globals.Src` and restore it afterward. Pass actors explicitly to
operations that need damage attribution, permissions or a message recipient.

## Save and load boundaries

Synchronous saves format and write before returning. Autosaves format under the world lock
and write the buffers on a worker. Subsequent saves, recompilation and shutdown finish pending
writing first. `@afterSave` reports writing completion, not just formatting completion.

Loading constructs objects and resolves deferred references before gameplay resumes. Failed
loads clear partial state before trying a backup, within `WorldSaver.Load()`'s attempt limit.
See [persistence](persistence-and-saving.md) for hooks and authoring rules.

## Recompilation

`Globals.Resync()` detects script changes. A compiled-script change invokes
`MainClass.RecompileScripts()`, reconstructing the world:

1. Enter recompiling state and save. A save failure leaves the world intact.
2. Raise `@shutdown(false)`, close displayed containers and back up client links.
3. `ForgetAll()` clears world roots, timers, script registries and caches.
4. Compile/load scripts, load definitions and restore the save.
5. Relink clients, initialise maps/scripts/plugins and raise `@startup(false)`.
6. Release backup links and resume operation.

Compilation failure leaves the server awaiting retry. Retrying clears partial state before
loading again. Resync support is subsystem-specific: script-authored static regions require
a restart.

## Shutdown

| Entry | Save behaviour |
|---|---|
| `.exit`, remote-console `exit`, local console line | `SaveAndExit()` attempts a synchronous save while running; failure is logged and shutdown continues. |
| `.exitnosave` / console `exitnosave` | No new save. |
| Exit while awaiting recompilation retry | No new world save. |
| Crash/process termination, including SIGTERM | No guaranteed shutdown save; no SIGTERM save handler exists. |

Game/remote-console exit commands require plevel 5. Shutdown changes the run level under
the global lock so queued saves cannot begin against a world being torn down.

`Exit()` finishes pending writing, stops networking and the auxiliary connection, raises
`@shutdown(true)` if globals exist, clears timers and marks the process dead. The final
shutdown hook runs after the deliberate shutdown save; its mutations are not in that save.
`true` means process start/exit; `false` means recompilation.

## Sources

[Main](../../../GameCore/Main.cs), [RunLevel](../../../GameCore/RunLevel.cs),
[CompilerInvoker](../../../GameCore/CompiledScripts/CompilerInvoker.cs),
[WorldSaver](../../../GameCore/Persistence/WorldSaver.cs),
[Timer](../../../GameCore/Timers/Timer.cs).
