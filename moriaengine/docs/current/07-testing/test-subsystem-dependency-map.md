# Test subsystem dependency map

This document maps the test suite to the engine subsystems it actually exercises.
It separates the **primary subsystem under test** from the **supporting subsystems
required by the current harness**. The point is to identify what can disappear in
an in-process lightweight world and which subsystems naturally form migration groups.

## Scope and method

The inventory covers:

- fast in-process tests under `moriaengine/Tests/*.cs`,
- fast fixture infrastructure under `moriaengine/Tests/Fixtures/`,
- protocol/full-server tests under `moriaengine/Tests/Protocol/`,
- script-hosted tests under `moriaengine/scripts/Tests/`,
- performance tests under `moriaengine/Tests/Perf/`,
- generator tests under `moriaengine/Tools/SteamEngine.Generators.Tests/`.

The suite has three materially different execution shapes:

1. **Pure fast tests**: no server state or process boot.
2. **In-process fast tests**: `EngineTestFixture` plus shared process/static state,
   but no real GameCore/AuxiliaryServer process pair.
3. **Protocol full-server tests**: real processes, real saves, real wire protocol,
   and normally the complete script/world boot.

## Existing test scopes and fixtures

The suite already has more scopes than one class method vs. one full server pair.
The practical ladder is:

### Scope 0: no fixture / isolated object

Examples:

- formula and static utility tests,
- `ConvertToolsTests`,
- timer heap/clear/wake tests,
- save buffer primitives,
- pure LScript grammar/parsing tests.

Characteristics:

- a test creates its own inputs,
- no temp directory or fixture is needed,
- process-wide state is usually accidental rather than intentional.

This is already close to the desired functional style.

### Scope 1: single temp-directory/process-state fixture

The existing fast fixture is `EngineTestFixture` plus `EngineCollection`.

It provides:

- a disposable temp working directory,
- a first-touch setup for one-shot `Globals` initialization,
- process-wide serialization for classes that can touch Globals.

It does not provide:

- real `MainClass.Init()`,
- real `.mul` loading,
- script compilation,
- world loading,
- networking,
- timers,
- persistence save format.

This scope is useful for pure engine classes that require one-shot configuration,
but it is too broad and too implicit for a future test world. It serializes
classes even when they do not need each other's state.

### Scope 2: dedicated Roslyn/in-memory compilation fixture

The generator test projects run with a synthetic Roslyn `CSharpCompilation`.
They provide syntax trees and metadata references rather than a server.

Provided dependencies:

- an empty or stubbed compilation,
- generator instance,
- direct generated-source and diagnostic output.

Not provided:

- world,
- scripts assembly,
- save/load,
- networking,
- timers.

This is the cleanest existing example of explicit inputs and returned results.
The missing-attribute tests should remain here and should use an empty compilation.

### Scope 3: in-process script runtime suite

`TestSuite` plus methods marked `[RegisterWithRunTests]` represents another
existing intermediate scope.

Methods such as `LScriptTest.RunLScriptTests()` and
`MemberWrapperTests.RunMWTests()` are discovered by `ClassManager`, then executed
through `Globals.RunTests()` and `TestSuite.RunAllTests()`.

This scope:

- loads classes and script runtime state,
- executes test methods inside the real server process,
- can inspect real registries and world objects,
- is reset/rebuilt during recompile.

It is useful, but it is more implicit than a modern fixture: discovery is
assembly-wide, state comes from static `TestSuite.tests`, and the test suite
does not declare which systems it depends on.

### Scope 4: isolated protocol collection

Most protocol test files define a dedicated collection with:

```text
[Collection("...Collection")]
[CollectionDefinition(..., DisableParallelization = true)]
ICollectionFixture<GameServerPairFixture>
```

The collection creates one real server pair for a test class/family.
It is not a test-method fixture: methods share the server pair and often also
share accounts, characters, script state, caches, and persistence.

This scope is intermediate only in infrastructure terms:

- the test boots less than "all processes" but still boots GameCore/Auxiliary,
- collection lifetime controls world lifetime,
- each collection starts from a clean snapshot,
- test methods in the collection run sequentially,
- cross-Fact state leakage is common and must be handled explicitly.

Examples include class families such as:

- combat collections,
- NPC collections,
- quest/story collections,
- house/town/economy collections,
- timer/gathering/living-world collections.

### Scope 5: manually booted two-server world

Tests such as `RaceNationRealmReloadTests` and persistence-restart tests create
`GameServerPairFixture` instances directly and may boot twice in one Fact.

This scope adds:

- one or multiple server boots,
- save-preserving restart,
- identity-based relog/reselection,
- explicit second lifecycle,
- test-owned disposal and save handoff.

This is the closest existing shape to the future test fixture concept:
the test function owns lifecycle and passes state from one operation/result to
the next. It is still too coupled to processes and fixed ports.

### Scope 6: full protocol pair

`GameServerPairFixture` is the maximum existing scope:

- real temp save/snapshot cleanup,
- real AuxiliaryServer + GameCore processes,
- fixed ports,
- real login/authentication,
- real wire protocol,
- real script compilation,
- real `.def/.scp` loading,
- real MUL/map loading,
- real world persistence,
- real timers and process runtime.

The fixture also owns:

- boot progress and timeout,
- game-output capture,
- process cleanup,
- stale/orphan handling,
- port locking,
- save snapshot reset/preservation.

This is end-to-end proof, but it is too expensive to make it the unit for every
subsystem assertion.

## Recommended future fixture ladder

The in-between scopes should become explicit, reusable world fixtures rather
than process-static collections:

1. **Object-fixture**
   - creates plain in-memory objects,
   - no world lifecycle.
2. **Function-fixture**
   - clocks, allocators, parsers, map algorithms, save encoders.
3. **Registry-fixture**
   - defs, ScriptHolders, triggers, caches, generated metadata.
4. **Entity-fixture**
   - accounts, Things, inventories, world indexes.
5. **Map-fixture**
   - synthetic or checked-in map state plus regions/spatial indexes.
6. **Script-fixture**
   - explicit script files/change sets plus registry deltas/results.
7. **Timer-fixture**
   - virtual clock, scheduler, due-work, callback cancellation.
8. **Persistence-fixture**
   - snapshot serialization plus load/save result.
9. **World-fixture**
   - composed systems with no external network/process shell.
10. **Protocol-fixture**
   - real wire/process proof over a lightweight or full world.

The key change is that fixture composition should be explicit:
tests request only the systems they need, rather than inheriting a collection
that happens to boot a server pair.

These fixture levels are the test-facing names for the replaceable world levels
described in
[GameCore startup/resync static-state map](../02-engine-core/startup-and-resync-static-state.md).
They are intentionally not a second architecture: for example,
`Registry-fixture`, `Script-fixture`, and `Timer-fixture` become hosted scopes
inside `World`, while `Protocol-fixture` stays an outer process/transport
adapter.

Some mirrored files exist in both `Tests/Protocol/` and `scripts/Tests/`.
Those are not separate subsystem categories: the protocol file supplies the client
assertion or scenario, while the script file supplies server-side behavior and
diagnostic commands.

## Dependency notation

Each category below distinguishes:

- **Primary target**: the subsystem whose behavior the tests are validating.
- **Currently required support**: infrastructure or state the existing test reaches.
- **Could be removed or minimized**: support that an in-process functional world
  could provide directly or replace with test data.
- **Tight neighbors**: subsystems that currently share the same state, ordering,
  lifecycle, or fixtures.

## 1. World lifecycle, boot, and shutdown

**Primary target**

- startup and initialization,
- run levels,
- process/fixture teardown,
- script boot,
- save on shutdown,
- reboot/reload behavior,
- build reuse and stale-generation behavior.

**Representative tests**

- `BootLogTests`
- `InfrastructureSmokeTests`
- `BuildReuseBootTests`
- `ShutdownSaveTests`
- `WorldSetupTests` / restart variants
- `WorldEventsRestartTests`
- `SaveTimingsTests`
- `TimerBurstTests`

**Currently required support**

- child processes,
- AuxiliaryServer login,
- real GameCore startup,
- scripts/defs loading,
- world persistence,
- fixed ports,
- network sessions.

**Could be removed or minimized**

- AuxiliaryServer and real sockets for tests that do not test login,
- persistence unless save/restart is the assertion,
- timer thread if timers are explicitly scheduled but not awaiting wall time,
- full map/MUL load if no spatial behavior is tested.

**Tight neighbors**

- persistence,
- scripts/assemblies,
- timers,
- networking only at the production shell edge,
- static registries and caches.

## 2. Scripts and script file loading/resync

**Primary target**

- `.scp`/`.def` parsing,
- script file change detection,
- incremental script resync,
- script file unload/reload,
- script constants and registered definitions,
- script-owned startup/resync behavior.

**Representative test families**

- `ContentRestorationTests`
- `DefPersistenceRegressionTests`
- `RaceNationRealmReloadTests`
- `GmFlagsReloadTests`
- `WorldSetupRestartTests`
- `SkillRestartTests`
- `NpcDefListsRestartTests`
- `GuildStoneRestartTests`
- `AuctionRestartTests`
- the story/quest/region restart families.

**Currently required support**

- full script compilation,
- world boot/save,
- defs and Thing registries,
- network for typing GM commands or reading logs,
- timers for post-load delayed effects.

**Could be removed or minimized**

- source scripts can be loaded from an explicit file-set input,
- recompile tests can use a test assembly plus fixture-owned registries,
- only changed files need to be replayed,
- protocol I/O can be replaced with direct callback/console result collection.

**Tighter migration boundary**

Script loading and script resync form a good first cohesive subsystem:
explicit input files, explicit change set, parsed output, registry delta,
diagnostics, and reload result.

## 3. Roslyn and script code generation

**Primary target**

- source-generator discovery,
- attribute/diagnostic contracts,
- generated partial classes,
- assembly compilation/reuse,
- script command dispatch,
- save/deep-copy/viewable implementors.

**Representative tests**

- `SteamEngine.Generators.Tests`
- `BuildReuseTests`
- `BuildReuseBootTests`
- `ClassAbilityTests`
- `InfoDialogTests`
- tests that expose generated views/fields/dispatch.

**Currently required support**

- scripts project build,
- class discovery,
- runtime/def metadata,
- full boot for end-to-end output verification.

**Could be removed or minimized**

- pure generator tests need only Roslyn syntax/compilation stubs,
- generated source can be asserted directly without world boot,
- assembly reuse belongs in a build/cache service rather than a world root.

**Tight neighbors**

- defs/templates,
- scripts,
- diagnostics/build,
- save/deep-copy implementors.

## 4. Definitions and templates

**Primary target**

- `ThingDef`, `PluginDef`, `AbstractDef`,
- def-field metadata,
- def lookup,
- save/load of def-facing values,
- Thing/Plugin template behavior,
- static code-made def initialization.

**Representative test families**

- `*DefFieldsTests`
- `NpcDefLists*`
- `EquippableDefFieldsTests`
- `RaceNationRealmReloadTests`
- `TrackingPluginsRegistrationTests`
- `MonsterAbilitiesSettingsCoverageTests`
- `Professions` / skill / NPC setting coverage.

**Currently required support**

- script/assembly initialization,
- def registries,
- world load for saved references,
- Thing/entity instances for some end-to-end assertions.

**Could be removed or minimized**

- pure def metadata can be built from in-memory def specifications,
- code-made def checks can use synthetic assemblies,
- tests can create def instances without loading MUL/world save files.

**Tight neighbors**

- Things/entities,
- script loading,
- code generation,
- persistence.

## 5. Things, Items, Characters, Accounts

**Primary target**

- entity creation/deletion,
- inventory and containers,
- character/account state,
- stats and equipment,
- movement-independent object behavior,
- instance fields/plugins/tags.

**Representative test families**

- equipment/armor/items,
- bank, inventory, stacking, loot, corpse,
- character stats, hunger, healing, poisoning,
- NPC stats/equipment/target behavior,
- house/region ownership,
- account/character login and relog behavior.

**Currently required support**

- defs/templates,
- map coordinates/spatial lookup,
- timers,
- triggers/plugins,
- persistence for restart assertions,
- protocol only for player-facing verification.

**Could be removed or minimized**

- create model objects directly instead of login/packet setup,
- use synthetic inventories and in-memory maps,
- skip account/network persistence when testing entity logic,
- call server-side methods directly instead of using console commands.

**Tight neighbors**

- defs/templates,
- maps/regions,
- persistence,
- triggers/plugins,
- networking at the outer shell.

## 6. Maps and spatial behavior

**Primary target**

- tile data,
- sectors,
- range,
- line of sight,
- A* pathing,
- collision,
- houses/regions,
- multi-items,
- doors and fixed-Z behavior,
- road/roof/sector lists.

**Representative test families**

- `AStarTests`
- `LineOfSightTests`
- `LineOfSightBlockingTests`
- `RangeQueryTests`
- `FixedZTests`
- `CharCollisionTests`
- `MapFileTests`
- `MultiMapTests`
- `SectorListsTests`
- `Region*`
- `RoadsOutTests`
- `RoofLosTests`.

**Currently required support**

- map backing/MUL files,
- Things placed on the map,
- region lookup,
- pathfinding caches.

**Could be removed or minimized**

- synthetic coordinate grids for LOS/A*,
- a small in-memory map implementation for unit/integration tests,
- no actual MUL load if the assertion is derived from synthetic map state,
- no networking or persistence unless a test specifically reloads map state.

**Tight neighbors**

- Things/entities,
- regions/houses,
- NPC movement,
- combat range/targeting.

## 7. Combat, skills, progression, and formulas

**Primary target**

- damage formulas,
- combat resolution,
- skill gain,
- leveling,
- professions,
- shielding/backstab/ranged/magic rules,
- monster abilities,
- reputation and notoriety.

**Representative test families**

- pure formula tests:
  `BackstabFormulaTests`, `EreborCombatValuesTests`, `EreborDamageFormulaTests`,
  `KnockbackPushDistanceTests`, `MagicShieldFormulaTests`, `SkillGainFormulaTests`,
  `WeaponSkillMappingTests`.
- protocol/system tests:
  `Combat*`, `Healing*`, `Poison*`, `Pvp*`, `Taming*`,
  `Leveling*`, `SmallSkillTests`, `SkillGain*`, magic and monster families.

**Currently required support**

- Thing/entity and def state,
- map/range/LOS for live combat,
- skills and triggers,
- timers for sustained effects.

**Could be removed or minimized**

- pure formulas need only primitive inputs,
- combat tests can use synthetic attackers/targets and direct rule evaluation,
- map/LOS can be injected where exact spatial state matters,
- protocol I/O is unnecessary for formula-only assertions.

**Tight neighbors**

- Things/defs,
- maps,
- timers/plugins,
- triggers.

## 8. Triggers and plugins

**Primary target**

- trigger dispatch,
- cancellable triggers,
- plugin initialization,
- plugin reload,
- trigger/deny chains,
- startup/shutdown hooks,
- plugin-specific state and registration.

**Representative test families**

- `CancellableTriggerTests`
- `LevelingTriggersTests`
- `SpellTriggersTests`
- `HitTriggersTests`
- `PluginLoopTests`
- `RegenerationPlugin*`
- `TrackingPluginsRegistrationTests`
- `WorldEvents*`
- plugin reload/registration restart tests.

**Currently required support**

- script loading,
- script holders,
- Thing/def instances,
- timers,
- world lifecycle.

**Could be removed or minimized**

- trigger dispatch can use explicit source/args/result objects,
- startup/shutdown trigger tests can call lifecycle hooks directly,
- reload tests can replace a plugin registry snapshot rather than boot a server.

**Tight neighbors**

- scripts,
- plugins,
- command dispatch,
- timers,
- lifecycle.

## 9. LScript and command dispatch

**Primary target**

- LScript grammar and evaluation,
- member/property/field resolution,
- command parsing,
- SteamFunction dispatch,
- authorization and raw-vs-parser command behavior,
- server-side script test suites.

**Representative tests**

- `LScriptGrammarTests`
- `LScriptTests`
- `RandomExpressionParseTests`
- `PetCommandParserTests`
- `CommandDispatchTests`
- `StaticCommandTests`
- `BasicCommandsTests`
- `GmCommandsTests`
- `CommandPermissionTests`
- `MemberWrapperTests`
- `LScriptTest`.

**Currently required support**

- expression test objects,
- Globals/def lookup,
- command cache,
- Things/accounts for real command behavior,
- networking for protocol command entry.

**Could be removed or minimized**

- parser/grammar tests need only an expression input and parse result,
- dispatch tests can call an explicit resolver/function registry,
- authorization can receive an explicit source/permission/trigger result,
- protocol tests need only a thin networking shell.

**Tight neighbors**

- scripts,
- Triggers/plugins,
- SteamFunctions,
- Things/accounts.

## 10. Persistence and save/load

**Primary target**

- save buffers,
- static/member serialization,
- enum/tag serialization,
- ObjectSaver,
- def/thing reload,
- item/corpse/quest/timer persistence,
- restart/resync behavior.

**Representative test families**

- `SaveBufferTests`
- `StaticMemberSaverTests`
- `ObjectSaverEnumTests`
- `PersistenceTests`
- `EnumTagPersistenceTests`
- `DefPersistence*`
- `ItemDecayPersistenceTests`
- `CorpseDirectionPersistenceTests`
- `GatheringDelayPersistenceTests`
- `ShutdownSaveTests`
- all `*RestartTests`.

**Currently required support**

- in-memory objects,
- defs/Thing state,
- world boot/save formatting,
- real filesystem snapshots,
- network relog for protocol persistence checks.

**Could be removed or minimized**

- serialization primitives can operate on buffers/DTOs with no server,
- saveable objects can be created directly and round-tripped,
- real restart tests only need explicit snapshot load/save results,
- filesystem snapshots and child-process relaunch remain only for end-to-end proof.

**Tight neighbors**

- Things/defs,
- timers,
- script registries,
- world lifecycle.

## 11. Timers and deferred work

**Primary target**

- timer heap,
- wake signals,
- timer clearing,
- monotonic time,
- delayed copy/load,
- delayed messages,
- timer preservation across recompile/save.

**Representative tests**

- `MonotonicTimeTests`
- `TimerHeapTests`
- `TimerClearTests`
- `WakeSignalTests`
- `TimerBurstTests`
- `DelayedMessagesTests`
- `SaveTimingsTests`
- gathering/door/spawn delay tests.

**Currently required support**

- timer thread,
- world run levels,
- entities,
- deferred object resolution,
- recompile/save lifecycle.

**Could be removed or minimized**

- heap/scheduling tests need a clock + queued work only,
- timer thread can be a fake/synchronous scheduler in unit tests,
- reload tests can compare old/converted/new scheduled work explicitly,
- no networking or full world boot unless a protocol-visible outcome matters.

**Tight neighbors**

- world lifecycle,
- persistence,
- Things,
- deferred copy/load.

## 12. Networking and sessions

**Primary target**

- login handshakes,
- encryption/compression,
- packets,
- game-state/session mapping,
- character enter/logout,
- protocol behavior.

**Representative tests**

- `LoginHandshakeTests`
- `RealEncryptionHandshakeTests`
- `HuffmanDecompressorTests`
- `TestLoginClient` / `TestGameClient`
- protocol packet/command tests that exist only to communicate with world behavior.

**Currently required support**

- AuxiliaryServer/GameCore,
- live sessions,
- login/account state,
- world boot.

**Could be removed or minimized**

- most gameplay protocol tests can call in-process server-side operations directly,
- packets/codecs can have a separate synthetic byte-stream test,
- login should remain a thin shell test plus one or two end-to-end login proofs,
- packet relay should not require persistence unless packet behavior explicitly depends on saved state.

**Tight neighbors**

- Accounts,
- GameState,
- world lifecycle,
- world shell.

## 13. Regions, quests, stories, economy, and world content

**Primary target**

- quests/story state,
- regions/towns/venues,
- economy/trade,
- NPC schedules,
- living-world systems,
- regional events,
- guild/house/tavern/event content.

**Representative families**

- quest/story tests: `Q006Tests`, `Q007Tests`, `Q008Q31Tests`,
  `Q009Q018Tests`, `Q017Tests`, `Q026Tests`, `Q30Tests`,
  `Q041Q099Tests`, all city/story test files.
- economy/content: `EconomyTests`, `Bank*`, `TradeVenueTests`,
  `House*`, `GuildStone*`, `TavernTests`, `Auction*`.
- living-world: `WorldEvents*`, `LivingWorld*`, `AmbientLifeTests`,
  `WarCampTests`, `VenueLifeTests`, `RegionalStoryTests`.

**Currently required support**

- whole-world boot,
- defs/templates,
- scripts,
- Things/NPCs/accounts,
- maps/regions,
- timers,
- persistence,
- protocol actions.

**Could be removed or minimized**

- quest state machines can be unit-tested with explicit entity/event inputs,
- world events can use a virtual clock and synthetic regions,
- content verification can separate script-definition checks from live NPC behavior,
- retain a few protocol tests as end-to-end proof that direct APIs are wired.

**Tight neighbors**

- scripts,
- triggers/plugins,
- Things,
- maps,
- timers,
- persistence.

## 14. Localization, UI/meta information, and caches

**Primary target**

- localization retrieval,
- `.info`/viewable metadata,
- dialog text,
- menu/context metadata,
- command caches,
- tooltip/item updater caches,
- static lookup caches.

**Representative tests**

- `LocalizationVerificationTests`
- `SpeechPlaceholderTests`
- `QuestDialogTextTests`
- `InfoDialogTests`
- `MenuStyleTests`
- `ContextMenuTests`
- `QuickSlotTests`
- `LevelSpendDialogTests`
- `AosToolTips`-related behavior.

**Currently required support**

- script/loc initialization,
- defs/entities,
- player/session for dialogs,
- network or direct command invocation.

**Could be removed or minimized**

- localization/catalog tests need only localization inputs,
- viewable metadata can inspect generated symbol/def metadata directly,
- cache tests can use an explicit cache instance,
- UI packet tests need sockets only for one thin end-to-end assertion.

**Tight neighbors**

- script/loc loading,
- defs,
- code generation,
- networking.

## Dependency matrix

| Test category | Primary target | Map | Thing/def | Script/LScript | Trigger/plugin | Timer | Persistence | Network |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Pure formulas/parsers | formula/parser | optional | optional | optional | no | no | no | no |
| A* / LOS / range | Map/spatial | yes | synthetic/limited | no | no | no | no | no |
| Timer heap/deferred work | timer subsystem | no | optional | no | no | yes | optional | no |
| Save buffer/member serialization | persistence | no | synthetic | no | no | no | yes | no |
| LScript/SteamFunction | script runtime | no | synthetic | yes | optional | no | no | no |
| Def/template metadata | defs/templates | no | yes | yes | no | no | optional | no |
| Trigger/plugin reload | trigger/plugin | no | yes | yes | yes | optional | optional | no |
| World/entity gameplay | Things/world | optional | yes | yes | yes | yes | optional | optional |
| Full quest/story/content | scripts/content | yes | yes | yes | yes | yes | optional | protocol shell |
| Restart/reload | lifecycle/persistence | optional | yes | yes | yes | yes | yes | optional |
| Login/network codec | networking | no | no | no | no | no | no | yes |

## Most tightly coupled protocol clusters

The following groups are coupled because they share real-state lifecycles, not
because they inherently belong to the same domain:

1. **World + scripts + defs**:
   reboot/reload tests, def persistence, restart tests.
2. **Thing + map + combat**:
   NPC combat, ranged combat, LOS, melee, movement, collision.
3. **Script + trigger + timer**:
   world events, delayed messages, gathering, quest scheduling.
4. **Account + character + networking**:
   login, logout, bank, guild, party, trade, protocol movement.
5. **Persistence + all world objects**:
   only required because the assertion proves a real snapshot restart.

These are integration edges. They should remain full-server tests, but one or
more direct in-process tests should replace each protocol test's main
gameplay assertion once the underlying subsystem is isolated.

## Candidate migration groups, smallest to largest

### A. Pure, already-functional slice

Groups:

- formulas,
- parsers,
- packet/codecs,
- save buffers,
- deterministic map algorithms.

Why start here:

- smallest dependency footprint,
- many tests are already close to pure input/output,
- low risk to save format/world semantics.

Limitation:

- it proves functional APIs but not the world-root/lifecycle architecture.

### B. Small stateful slice: timers/deferred work

This is the smallest stateful migration group that exercises:

- mutable state,
- clock access,
- scheduling,
- clear/reload behavior,
- deferred callbacks.

Start with explicit inputs such as `WorldClock`, `TimerQueueSnapshot`,
and work items. Return schedule changes or due-work results instead of
allowing callbacks to reach hidden statics.

### C. Script and registry slice

Move:

- script file collection,
- script constants,
- ScriptHolder/SteamFunction registry,
- TriggerGroup registration,
- script/LScript reload.

This is a strong first root-of-world proof because it directly addresses
reload/static leaks and has a clear operation chain:
`discover -> parse -> register -> resync -> forget -> reload`.

### D. Persistence slice

Convert:

- save buffer formatting,
- static/member serialization,
- ObjectSaver operation planning,
- WorldSaver load/save input/output contracts.

Keep protocol/process save/restart as a final proof rather than the default test implementation.

### E. Map/spatial slice

Build an in-memory map state for synthetic tests and move LOS, range,
collision, A*, sectors, and region lookup behind explicit world-map inputs.

### F. Thing/def/entity slice

This is larger, but the suite shows a cohesive cluster:
def metadata → Thing creation → inventory/equipment → plugins/triggers.
It should follow scripts/registries and timers.

### G. Networking shell

Networking should remain an adapter, not the migration starting point.
It is valuable as a final protocol proof and for login/account transport,
but most gameplay assertions should stop depending on it.

## Recommended first functional-world slice

The strongest initial group is:

**Timer/deferred work + script/registry reload + persistence planning**

It is small enough to isolate, but broad enough to prove the architecture:

1. explicit clock/input state,
2. explicit script file change set,
3. explicit registry snapshot/delta,
4. explicit timer snapshot/delta,
5. explicit save/load operation result,
6. a test fixture calls the operations and owns all commit/cleanup,
7. `MainClass` only orchestrates the production side using the same operations.

A smaller proof can start with timers alone; a stronger proof can include
script resync and in-memory save/restart immediately after.

## End-to-end tests to retain

Do not convert all protocol tests into direct calls. Retain a small set of:

- boot/shutdown,
- login and client game entry,
- one real save/restart,
- one real script resync/recompile,
- one real Thing/map/combat path,
- one real networking packet family,
- one real quest/story or living-world scenario.

Everything else should first gain an in-process subsystem test; protocol
coverage then becomes proof of wiring rather than the only source of truth.
