# GameCore startup/resync static-state map

This page tracks what `GameCore` startup and resync paths do to **static state**.  
Goal: make it easier to design a lightweight-but-working test boot by knowing what must be initialized, what can be skipped, and what must be reset between runs.

## Entry points and high-level flows

Main runtime entry is `MainClass.Main()` → `SteamMain()` in `GameCore/Main.cs`.

There are three relevant flows:

1. **Cold startup** (`Init()`).
2. **Incremental script resync** (`Globals.Resync()` → `ScriptLoader.Resync()`) when no C# sources changed.
3. **Full script recompile/reload** (`RecompileScripts()` / `RetryRecompilingScripts()`) when compiled script sources changed.

## A. Cold startup (`MainClass.Init`)

Sequence in order (`GameCore/Main.cs`):

1. `RunLevelManager.SetStartup()` (also pauses server time).
2. `AuxServerPipeClient.Init()`.
3. `Globals.Init()` (loads/holds process-wide config/static settings).
4. `CompilerInvoker.CompileScripts(true)`:
   - class discovery/registration (`ClassManager.InitClasses(...)`),
   - compile `scripts/`,
   - run central generated-code pipeline,
   - load generated assembly.
5. MUL/static data loads:
   - `TileData.Init()`, `SoundMul.Init()`, `MultiData.Init()`.
6. Script/data load:
   - `ScriptLoader.Load()`,
   - `TileData.GenerateMissingDefs()`, `CharData.GenerateMissingDefs()`,
   - `WorldSaver.Load()`,
   - `ClassManager.CheckCodeMadeDefs()`.
7. Runtime wiring:
   - `GameServer.Init()`,
   - `Map.Init()`,
   - `ClassManager.InitScripts()`,
   - `ScriptedLocResxBridge.RegisterAll()`,
   - `PluginDef.Init()`.
8. Run-level flip: `UnsetStartup()` + `SetRunning()`.
9. Trigger `@startup` (`Globals.Instance.TryTrigger(...)`).
10. Immediately run `Globals.Resync()` once.
11. Start timer thread (`Timer.StartTimerThread()`).

## B. Incremental resync path (no C# source changes)

`Globals.Resync()`:

- tries `MainClass.TryResyncCompiledScripts()`;
- if no compiled-source changes, runs `ScriptLoader.Resync()` and then `MainClass.CollectGarbage()`.

`ScriptLoader.Resync()` (`GameCore/ScriptLoader.cs`) does:

1. `allFiles.GetChangedFiles()` (tracks changed/deleted `.scp`/`.def` files).
2. `RunLevelManager.SetStartup()` (pauses server time).
3. `ObjectSaver.StartingLoading()`.
4. For each changed file:
   - existing file: `LoadFile(f)`,
   - deleted file: `f.Unload()`.
5. Finish/relink steps:
   - optional `Constant.ResolveAll()` + `AbstractDef.ResolveAll()`,
   - `ObjectSaver.LoadingFinished()`,
   - `AbstractDef.LoadingFinished()`,
   - `AbstractDefTriggerGroupHolder.LoadingFinished()`,
   - `ScriptedGumpDef.LoadingFinished()`.
6. `RunLevelManager.UnsetStartup()`.

This path updates script-defined registries incrementally and **does not** run the full static teardown list used by recompilation.

## C. Full recompile/reload path (compiled sources changed)

Triggered by `TryResyncCompiledScripts()` when `CompilerInvoker.FindIfSourcesHaveChanged()` is true, or by explicit recompile.

`MainClass.RecompileScripts()`:

1. `RunLevelManager.SetRecompiling()`.
2. `WorldSaver.Save()` (must succeed to continue).
3. Broadcast pause + trigger `@shutdown`.
4. `OpenedContainers.SendRemoveAllOpenedContainersFromView()`.
5. `GameServer.BackupLinksToCharacters()`.
6. `ForgetAll()` (major static teardown; list below).
7. `LoadAll()` (recompile + world/load/relink/init).
8. On success: `ScriptRecompilingSucceeded()` → GC + `GameServer.RemoveBackupLinks()` + `UnsetRecompiling()`.
9. On failure: `SetAwaitingRetry()` and wait for retry command.

`RetryRecompilingScripts()` re-runs steps 6–9 (without save).

## Static-state teardown inventory (full recompile path)

### `ClearWorld()` (`GameCore/Main.cs`)

- `Thing.ClearAll()`
- `AbstractAccount.ClearAll()`
- `Globals.ClearAll()`
- `Map.ClearAllDynamicStuff()`
- `ObjectSaver.ClearJobs()`
- `OpenedContainers.ClearAll()`
- `Commands.ClearGMCommandsCache()`
- `AosToolTips.ClearCache()`
- `ItemOnGroundUpdater.ClearCache()`

### `ForgetAll()` adds:

- `Timer.Clear()`
- `LocManager.ForgetInstancesFromAssembly(...)`
- `LocManager.ForgetInstancesOfType(...)`
- `ClassManager.ForgetScripts()`
- `GeneratedCodeUtil.ForgetScripts()`
- `ScriptHolder.ForgetAllFunctions()`
- `ThingDef.ForgetAll()`
- `PluginDef.ForgetAll()`
- `AbstractScript.ForgetAll()`
- `Constant.ForgetAll()`
- `TestSuite.ForgetAll()`
- `ObjectSaver.ForgetScripts()`
- `DeepCopyFactory.ForgetScripts()`
- `ScriptLoader.ForgetScripts()`
- `AbstractDef.ForgetScripts()`
- `AbstractSkillDef.ForgetAll()`
- `Map.ForgetScripts()`
- `FieldValue.ForgetScripts()`

## Run-level/static coordination to keep in mind

- `RunLevelManager.SetStartup/SetRecompiling/SetShutdown/...` mutate global run state and pause/unpause server time (`GameCore/RunLevel.cs`).
- Shutdown path (`Exit()`) also clears timers and marks `Dead`.
- Recompile path intentionally separates:
  - world/entity/static teardown (`ForgetAll()`),
  - recompile+reload (`LoadAll()`),
  - relink/cleanup (`ScriptRecompilingSucceeded()`).

## Implications for lightweight test boot

For a lightweight but functional test boot, the practical minimum is:

1. deterministic run-level transition (`SetStartup` → init/load → `SetRunning`),
2. script compile/load path (`CompileScripts` + `ScriptLoader.Load`),
3. world load (`WorldSaver.Load`) and `Map.Init`,
4. script bootstrap (`ClassManager.InitScripts`, `PluginDef.Init`),
5. optional timer thread (enable only if the test needs timer behavior),
6. explicit reset path between tests using the same process (`ForgetAll`/`ClearWorld` subset).

Without a reset strategy, static registries/caches from one test run leak into the next and hide ordering/reload bugs.

## Future direction: rooted `World` runtime instead of static roots

### Target architecture

Introduce a process-local `World` root object that owns mutable runtime state currently spread across static fields (things, defs/registries, map dynamic state, script runtime tables, timers/jobs/caches, connection-bound world links).

Design intent:

- **one active world per process** in production (same behavior as today),
- **multiple isolated worlds in one process** in tests (sequential first, optional parallel later),
- explicit lifecycle: `Create` → `Initialize` → `Run` → `Resync/Recompile` → `Shutdown` → `Dispose`,
- **functional core, imperative shell** for heavy operations:
  - loading/saving/reload steps take explicit input parameters (state + options + dependencies),
  - they return explicit result objects (state deltas, diagnostics, side effects to apply),
  - orchestration lives in one place (production host `MainClass`, or test fixtures),
- most current static APIs become either:
  - thin compatibility shims to `World.Current`,
  - or instance APIs on `World` services.

### Orchestration model (production and tests)

The long-term shape should be:

- **Orchestrator layer** (`MainClass` in production, fixture host in tests):
  - chooses sequence and policy,
  - provides dependencies/options,
  - commits side effects (network/listeners/threads/logging/IO boundaries).
- **World operations layer** (pure-ish domain operations):
  - no hidden static reads/writes,
  - no implicit singleton lookups,
  - deterministic outputs for deterministic inputs.

This is what enables lightweight test boots: tests can call the same operations with fake/minimal dependencies and assert on returned results without requiring full process-global setup.

### What should become world-owned first

High-value mutable state to de-staticize first:

- runtime entity/index state (`Thing`, accounts, opened containers, map dynamic data),
- script/runtime registries (`AbstractScript`/def maps, script functions, constants, loader tables),
- generated/runtime helper state (`ObjectSaver`, `DeepCopyFactory`, script holder registries),
- timer/job queues and world-linked deferred work,
- caches that currently require global `Clear*` calls on recompile.

Lower-priority or keep-static candidates:

- immutable process metadata (`Build`, fixed configuration defaults),
- truly global OS-facing glue (process watchdog hooks),
- narrow, audited singleton interop points while migration is ongoing.

### Migration structure: hierarchy of replaceable state levels

The previous broad phase list is not enough as an implementation plan. The
better unit of migration is a **world state level**: a set of objects that can
be constructed together, reloaded together, and discarded together.

The test fixture ladder below is useful because it describes real groups of
dependencies. The world should expose the same groups as nested scopes rather
than as a flat list of static reset methods.

Each level:

1. has explicit inputs and explicit construction results,
2. owns all mutable state introduced at that level,
3. exposes only the lifecycle operations needed by the level above,
4. is discarded wholesale when reloading invalidates it,
5. is independently constructible by a test fixture,
6. and does not reach back into a lower level except through instance references.

The levels are not ten separate public APIs that all tests must initialize.
They are a dependency blueprint. A fixture selects the depth it needs.

#### Level 0 - Process host

**Owns**

- process lifecycle,
- OS/current-directory/configuration location,
- logging/console infrastructure,
- run-level coordinator,
- aux/network host boundaries,
- watchdogs and process shutdown.

**Lifecycle**

- create process host,
- create selected world,
- run,
- shut down world,
- dispose host.

This level belongs to `MainClass` or a test fixture. It should not own gameplay
state.

#### Level 1 - Pure engine inputs and clocks

**Owns**

- immutable configuration,
- random sources,
- monotonic clock,
- allocators and pure deterministic services,
- formula/parser/result types.

**Reload behavior**

- create or replace if configuration/clock policy changes,
- normally reconstructed between tests,
- no world-save data.

This is the smallest useful unit and already matches many fast tests.

#### Level 2 - Registries and caches

**Owns**

- type/name lookup,
- def and script registries,
- SteamFunction/script-holder lookup,
- trigger/plugin lookup,
- command caches,
- localization catalogs,
- weak pools and lookup caches.

**Reload behavior**

- `CreateRegistrySnapshot(inputs) -> Result`
- on script recompile: create a new registry tree and swap it,
- old registry objects are discarded rather than individually cleared.

This level removes the first major static state family:
`ClassManager`, `ScriptHolder`, `AbstractScript`, `Constant`, `TestSuite`,
`FieldValue`, and many lookup caches.

#### Level 3 - Definitions and templates

**Owns**

- definition objects,
- template metadata,
- def-field models,
- plugin/template class relationships,
- code-made def state.

**Reload behavior**

- definitions are rebuilt after script/registry assembly reload,
- existing Things must be relinked or converted against the new definitions,
- no direct clear/repopulate of old registries.

This level is tightly coupled to registries and code generation but should be
separately replaceable because a def-only resync may not invalidate the whole
world.

#### Level 4 - Script runtime

**Owns**

- parsed `.def`/`.scp` state,
- LScript parsed/executable state,
- ScriptHolder dispatch,
- trigger groups,
- script functions/constants,
- script-origin localization and scripted hooks.

**Reload behavior**

- incremental resync: new script runtime snapshot plus changed-file delta,
- full recompile: create a new assembly/script runtime level,
- trigger/command registries resolve through this level.

The functional operation can be:

`ReloadScripts(ScriptReloadInput) -> ScriptReloadResult`

The result contains changed definitions, trigger changes, diagnostics,
new registries, and lifecycle hooks to run. `MainClass` decides when and how to
commit that result.

#### Level 5 - Spatial world

**Owns**

- map/tiles/statics/sectors,
- regions,
- LOS/range/pathfinding indexes,
- static/multi-world spatial structures,
- spatial caches.

**Reload behavior**

- create a new map index snapshot for full reload,
- rebuild derived sectors/regions,
- relink existing world objects,
- discard old indexes wholesale.

For lightweight tests this level can be a synthetic map implementation.
For a real boot it can be backed by MUL/map files.

#### Level 6 - Live entities and accounts

**Owns**

- Things,
- accounts,
- characters/items/NPCs,
- inventories/opened containers,
- live plugins attached to entities,
- UID/world indexes,
- live entity cross-references.

**Reload behavior**

- world save/load creates entity state through explicit input/output,
- full recompile may preserve entity state, swap definitions/registries, and relink,
- world reset/failed load constructs a fresh entity scope and must never retain old references.

This level is coupled to definitions and spatial state but can exist in isolation
for entity/unit fixtures.

#### Level 7 - Deferred work and timers

**Owns**

- timer heap,
- delayed callbacks,
- delayed copy/load,
- scheduled save/world events,
- deferred object resolution,
- wake/cancellation state.

**Reload behavior**

- snapshot timers before recompile,
- stop old scheduler,
- create a new timer scope,
- convert/restore/reject work intentionally,
- never allow old callbacks to execute against a new world.

This is a natural standalone migration slice because it is stateful but compact.
It is also a prime source of cross-world leakage.

#### Level 8 - Persistence and snapshots

**Owns**

- serialization plans,
- save buffers,
- loaded snapshots,
- save-format version state,
- pending/async save jobs,
- snapshot load diagnostics,
- relink plans.

**Reload behavior**

- save operations return a snapshot/write plan rather than mutating static state,
- load operations return a snapshot/state delta plus diagnostics,
- relinking is performed by the orchestrator after entity/script/def scopes exist.

Persistence should remain a value/operation boundary: it should not own
world lifecycle or require the network layer.

#### Level 9 - Network and session adapters

**Owns**

- active connections,
- client/session/game-state mapping,
- packet queues,
- character relink links,
- protocol transport state.

**Reload behavior**

- back up session links before recompile,
- keep transport objects or explicitly reconnect,
- relink characters after the new world scopes are committed,
- remove backup links after success.

This level should be thin infrastructure. Gameplay tests should not require it
unless they specifically test login, packets, or session behavior.

### How levels compose

A useful hierarchy is:

```text
ProcessHost
  World
    EngineInputs
    Registries
    Definitions
    ScriptRuntime
    SpatialWorld
    LiveEntities
    DeferredWork
    Persistence
    NetworkAdapter
```

The hierarchy is a **lifecycle grouping**, not a call-chain hierarchy:

- lower levels provide inputs/services,
- higher levels can own lower-level instances,
- reload invalidates only the levels whose input changed,
- the orchestrator decides which levels are preserved, rebuilt, or replaced.

Examples:

- pure formula test: Levels 0–1 only,
- LScript test: Levels 0–4,
- synthetic map/combat test: Levels 0–7,
- real save/restart test: Levels 0–8,
- login/protocol test: Levels 0–9.

### Concrete migration milestones

#### Milestone 1 - Functional operations and the first replaceable scope

1. Add a clock/scheduler value object and timer scope.
2. Move timer heap/clear/deferred work state behind explicit operations.
3. Add a lightweight fixture that creates and disposes the timer scope twice.
4. Confirm no callback survives across scope disposal.

**Proof:** timer unit tests become independent; no server boot or static clear is needed.

#### Milestone 2 - Replaceable registries

1. Add a registry scope for type/name/ScriptHolder/TriggerGroup lookups.
2. Make script discovery and class initialization produce registry snapshots.
3. Replace static `ForgetAll`/repopulate sequences with new-scope swapping.
4. Keep compatibility shims for existing callers.

**Proof:** script holder and trigger registries can be reloaded twice in one process.

#### Milestone 3 - Script runtime result + incremental resync

1. Move script file parsing behind explicit input/change-set operations.
2. Return a `ScriptReloadResult` containing diagnostics and registry deltas.
3. Let a test fixture commit or reject that result.
4. Keep `MainClass` responsible for process-level pause/save/relink side effects.

**Proof:** incremental script resync is reproducible in-process without a server pair.

#### Milestone 4 - Persistence planning

1. Convert save buffers and entity/static serialization to explicit input/output operations.
2. Return snapshot/write/load/relink plans.
3. Move filesystem/process sequencing to the orchestrator.
4. Retain real restart tests only for end-to-end proof.

**Proof:** object round trips and snapshot relinking work with no network/process boot.

#### Milestone 5 - Spatial and entity scopes

1. Introduce synthetic map scope and spatial index snapshots.
2. Move live entity collections, UID lookup, accounts, and containers behind world state.
3. Explicitly rebuild or relink definitions/entities after reload.
4. Add direct entity/combat tests under this scope.

**Proof:** synthetic map/combat tests run in-process across repeated world lifecycles.

#### Milestone 6 - Production orchestration and compatibility retirement

1. Move `MainClass.Init`, `LoadAll`, and `ForgetAll` sequences into a world builder/orchestrator.
2. Commit each level with a clear success/failure boundary.
3. Add retry/recompile behavior from structured results.
4. Retire static shims subsystem-by-subsystem.

**Proof:** production boot/recompile and test fixtures use the same operation contracts.

### Concrete first vertical slice

The first vertical slice should exercise the architecture across at least:

```text
ProcessHost
  EngineInputs
  Registries
  ScriptRuntime
  DeferredWork
```

Start with a script holder or trigger registry, not a full Thing/Map world:

1. create a clock,
2. create a fresh registry,
3. register synthetic/real script functions,
4. schedule deferred work,
5. perform resync/reload into a new registry/runtime scope,
6. verify old callbacks and functions are unreachable,
7. dispose the old scope,
8. repeat in a second test instance.

### Cross-cutting rules during migration

- Do not break save/reload compatibility while moving ownership.
- Maintain command/recompile/shutdown behavior parity at every phase.
- Prefer **strangler pattern** (facade delegates) over big-bang rewrites.
- For major operations (load/save/recompile/resync), prefer signatures of the form:
  - `Result = Operation(Input, Dependencies, Options)`
  - avoid reading implicit ambient state except at the orchestrator boundary.
- Keep side-effect execution at boundaries:
  - planning/computation returns intent/results,
  - orchestrator commits IO/thread/network/timer effects.
- For each migrated subsystem, add:
  - lifecycle tests (init/reset/reload),
  - leak checks (old world references not callable),
  - determinism checks (same inputs -> same resulting registries/state).
