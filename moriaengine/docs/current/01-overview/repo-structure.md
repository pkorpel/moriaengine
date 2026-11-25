# Repo structure

A directory-by-directory tour, from the engine root (`moriaengine/moriaengine/` in a full
checkout - the outer `moriaengine/` wrapper is just the session's checkout folder and has
no code of its own, only repo-wide files like `AGENTS.md`, `CLAUDE.md`, `.claude/` and `docs/`).

## `GameCore/` - the engine

The core server process (see [Architecture overview](architecture-overview.md)). Major
subfolders:

- `GameCore/Networking/` - packet read/write, `GameState` (per-connection state), the
  login/char-list/world-entry flow. See
  [Networking & the protocol layer](../02-engine-core/networking-protocol.md).
- `GameCore/Regions/` - `Region`/`StaticRegion`/`DynamicRegion`. See
  [Regions](../02-engine-core/regions.md).
- `GameCore/Timers/` - `Timer`/`BoundTimer`. See
  [Tags & timers](../02-engine-core/tags-and-timers.md).
- `GameCore/CompiledScripts/` - the runtime script-compilation pipeline (`CompilerInvoker`,
  `ClassManager` and friends). See
  [Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md).
- `Tools/SteamEngine.Generators/` - Roslyn source generators, including Thing/Plugin class
  template generation. See
  [Scripting generators](../02-engine-core/code%20generators/scripting-generators.md).
- `GameCore/LScript/` - the embedded scripting language's parser/interpreter. See
  [LScript](../02-engine-core/lscript.md).
- `GameCore/Persistence/` - the generic save/load machinery. See
  [Persistence & saving](../02-engine-core/persistence-and-saving.md).
- `GameCore/AI/` - low-level AI scaffolding consumed by the real NPC AI implementation in
  `scripts/Basic/AI/` (see [Pets & NPC AI](../04-gameplay-systems/pets-and-npc-ai.md)).
- `GameCore/AuxServerPipe/` - the internal channel GameCore and AuxiliaryServer use to talk
  to each other (server registration/handoff for the login redirect).
- Top-level files directly under `GameCore/` (`Thing.cs`, `AbstractItem.cs`,
  `AbstractCharacter.cs`, `ThingManipulationTriggers.cs`, `TagHolder.cs`,
  `CanSeeImplementation.cs`, `FieldValue.cs`, `DenyResult.cs`, ...) hold the core object
  model described in [The Thing model](../02-engine-core/thing-model.md) and
  [Triggers & deny chains](../02-engine-core/triggers-and-deny-chains.md).

## `Common/` and `Communication/`

Shared libraries with no engine-specific logic: `Common/` is general-purpose utility code
(logging, misc helpers); `Communication/` is the low-level TCP connection/packet-framing
layer both `GameCore` and `AuxiliaryServer` build on.

## `scripts/` - game content and gameplay logic

GameCore builds the C# scripts when needed or reuses the last build at boot (see
[Architecture overview](architecture-overview.md#boot-sequence)). Subfolders:

- `scripts/Basic/` - the real, live gameplay systems: `ItemClasses/`, `CharClasses/`,
  `AI/`, `Regions/`, `skills/`, `Trading/`, `Economy/`, `Leveling/`, `Professions/`,
  `Races/`, `AbilitiesAndTalents/`, `Spawning/`, `administration/` (GM tooling, settings,
  dialogs), `dialogs/`, `Target/`, `Roles/`, `EffectDuration/`, `Regeneration/`,
  `Localization/`, `Menu/`, `mailingsystem/`, `db/`, `utilities/`, `Resources/`,
  `BasicTypes/`. Each of these maps roughly 1:1 to a page under
  [Gameplay systems](../README.md#4-gameplay-systems).
- `scripts/Optional/` - gameplay add-ons that aren't core (`PlayerVendor/`,
  `StorageBoxes/`, `Treasures/`).
- `scripts/Temporary/` - scratch/in-progress scripts not considered part of the stable
  content set.
- `scripts/Tests/` - **server-side** test support code: `[SteamFunction]` diagnostic
  commands (`Test*`) that protocol tests drive over the wire. This is distinct from
  `Tests/` at the repo root (the actual xUnit test *runner* code) - see
  [Test infrastructure](../07-testing/test-infrastructure.md) for how the two halves work
  together.
- `scripts/converted/` - the real Erebor shard content, machine-converted from the
  original Sphere source by `SphereShardConverter` (see
  [SphereShardConverter](../06-tooling/sphere-shard-converter.md)). This is where most
  real items/NPCs/dialogs actually come from; `scripts/Basic/` is the engine-side logic
  that content is built against.
- `scripts/defaults/` - baseline content defaults, including `chardefs/`.
- `scripts/Utils/` - shared content-side helper code, including `Persistence/`.
- `scripts/AI/` - content-side AI scripts (as opposed to `GameCore/AI/`'s engine-side
  scaffolding).

## `Tests/`

The xUnit test project. `Tests/Protocol/` holds the full end-to-end, real-process,
real-wire-protocol test suite (`GameServerPairFixture`, `TestGameClient`,
`TestLoginClient`, and every `*Tests.cs` file that drives a real game scenario over the
wire). Everything else directly under `Tests/` is the faster, non-process-spawning tier.
See [Test infrastructure](../07-testing/test-infrastructure.md).

## `Languages/`

Per-language localization override data, organized by source assembly
(`GameCore/`/`LScript/`/`Scripts/`) under each language folder (`Czech/`, `Default/`,
`English/`). See [Localization](../05-localization.md) for which of these folders is
actually read by running code (it's not all of them, and not obviously).

## `AuxiliaryServer/`, `SphereShardConverter/`, `RemoteConsole/`

Separate executables/tools, each covered on its own page:
[AuxiliaryServer](../06-tooling/auxiliary-server.md),
[SphereShardConverter](../06-tooling/sphere-shard-converter.md),
[RemoteConsole](../06-tooling/remote-console.md).

## `ThirdParty/`

Vendored client encryption (Twofish and UO login encryption) and the Grammatica parser runtime, both used by GameCore.

## `saves/`

Runtime save data - the actual persisted world state (`saves/<date>/<time>/*.sav`).
Treated as scratch/gitignored-in-spirit in practice even though a baseline snapshot
(`saves/2000-01-01/00_00_00/`) is committed as the clean starting point every fresh boot
falls back to. See [Test infrastructure](../07-testing/test-infrastructure.md) for why
stale directories here are a recurring source of confusing test failures.

## Documentation

`moriaengine/docs/current/` (where this page lives) documents the current systems.
`moriaengine/docs/wip/` holds drafts. Historical SteamEngine material is in the repo-root
`archive/docs/`; it is not maintained against the current code. See `AGENTS.md` for the rule
that keeps current documentation in sync with code changes.
