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

### Migration phases

#### Phase 0 - Baseline and boundaries

1. Freeze and document static mutation boundaries (this page + code comments at key roots).
2. Add a static-state inventory test/checklist used in PRs touching startup/resync.
3. Define `World` contract interfaces (`IWorld`, lifecycle hooks, core service accessors).

**Exit criteria:** agreed world contract + explicit inventory of static owners and reset semantics.

#### Phase 1 - Introduce `World` shell with compatibility bridge

1. Add `World` class and `WorldRuntime`/`WorldContext` scaffolding.
2. Add `World.Current` + guard rails (clear errors if accessed without active world).
3. Keep existing static entry points, but route new code through world-aware adapters.

**Exit criteria:** server still boots exactly as today; no behavior change, but `World` lifecycle exists.

#### Phase 2 - Move startup/resync orchestration into `World`

1. Move `Init`/`LoadAll`/`ForgetAll` orchestration into `World.Initialize()`, `World.Reload()`, `World.Reset()`.
2. Keep `MainClass` as thin host wiring (console, process shutdown, aux process hookup).
3. Keep run-level transitions but make them world-scoped where possible.
4. Introduce operation contracts that return structured results, for example:
   - `Initialize(WorldInitInput) -> WorldInitResult`
   - `Reload(WorldReloadInput) -> WorldReloadResult`
   - `Reset(WorldResetInput) -> WorldResetResult`

**Exit criteria:** `MainClass` delegates world lifecycle; startup/recompile behavior parity preserved.

#### Phase 3 - De-staticize script/runtime registries

1. Move script registries from static dictionaries into world-owned services.
2. Update `ScriptLoader`, `ClassManager`, `GeneratedCodeUtil` consumers to use world services.
3. Keep temporary static facades delegating to `World.Current`.

**Exit criteria:** recompile/reset no longer depends on broad static `ForgetAll` lists for moved subsystems.

#### Phase 4 - De-staticize world data + map dynamic state

1. Move `Thing`/account/world collections and map dynamic indexes under world state objects.
2. Make world load/save paths consume explicit world state instances and return explicit output/state updates (not hidden global mutations).
3. Isolate references crossing world boundary (network sessions, aux links).

**Exit criteria:** most gameplay/runtime mutable data is world-instance-owned.

#### Phase 5 - Timers/jobs/deferred work

1. Scope timer queues and job/deferred callbacks to world instance.
2. Ensure reload/shutdown guarantees: no callbacks from old world after swap/reset.
3. Add cancellation/disposal ordering tests.

**Exit criteria:** no cross-world timer/job leakage in stress reload tests.

#### Phase 6 - Test harness enablement (lightweight worlds)

1. Add a lightweight world host for tests (no sockets, optional reduced data load).
2. Add fixture helpers: create world, load minimal scripts, run assertions, dispose world.
3. Add optional knobs:
   - skip aux/network listeners,
   - disable timer thread unless requested,
   - load targeted script subsets.

**Exit criteria:** unit/integration tests can run isolated world boots in-process without full server bring-up.

#### Phase 7 - Retire compatibility statics

1. Remove static shim APIs subsystem-by-subsystem.
2. Fail builds on new mutable static fields in migrated areas.
3. Keep only explicit process-level statics.

**Exit criteria:** static mutable state is exceptional/audited, not default.

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

### Suggested first implementation slice

A practical first slice with low risk:

1. Introduce `World` + lifecycle skeleton.
2. Move orchestration methods (`Init`/`LoadAll`/`ForgetAll`) into world class without changing internals yet.
3. Route one small registry (for example script function table) through `World.Current`.
4. Add one test fixture that boots/disposes a lightweight world twice in one process and verifies no leftover commands/functions from the first run.

That gives immediate testing value while proving the migration shape before deeper subsystem moves.
