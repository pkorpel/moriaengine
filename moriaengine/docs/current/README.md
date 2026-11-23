# moriaengine documentation

This is the current, maintained documentation for moriaengine - a .NET 8 rewrite of
SteamEngine, itself a C# reimplementation of a Sphere-style Ultima Online server emulator,
carrying the Czech "Erebor" shard's converted content and lore (Lord of the Rings themed:
Gondor/Mordor realms, Carn Dûm, etc).

**This is not the same thing as `docs/` one level up.** That folder is the *original*
SteamEngine project's own historical material (LScript language notes, CVS history, trigger
lists, old Erebor planning pages) - useful background, not maintained, not guaranteed
accurate against the current code. Everything in `docs/current/` is written against the
code as it stands today and is expected to be kept that way - see the workflow rule in the
repo-root `CLAUDE.md` ("every code change that touches a documented system must update the
relevant page here in the same change").

## Start with your task

| Task | Read in order |
|---|---|
| Understand world ownership/startup/shutdown | [Architecture](01-overview/architecture-overview.md) → [Runtime lifecycle](02-engine-core/runtime-lifecycle.md) |
| Add a content type or field | [Thing model](02-engine-core/thing-model.md) → [Scripting](02-engine-core/scripting-pipeline.md) → [Persistence](02-engine-core/persistence-and-saving.md) |
| Add optional behaviour | [Trigger architecture](02-engine-core/trigger-system-architecture.md) → [Plugins](02-engine-core/plugins.md) → relevant catalog |
| Block or replace an action | [Deny chains](02-engine-core/triggers-and-deny-chains.md) → action's source method |
| Schedule/save state | [Tags and timers](02-engine-core/tags-and-timers.md) → [Persistence](02-engine-core/persistence-and-saving.md) |
| Investigate latency | [Performance](02-engine-core/performance.md) → dated [research](08-history/research/README.md) |

## Keeping these docs useful

Lead with the current contract: purpose, ownership, flow, extension points and failures.
Keep each rule in one canonical guide and link to it from related pages. Use source links
and symbol names rather than relying on line numbers that drift. Update affected docs in
the same change as code.

Keep dated benchmarks and migration reports under history. Preserve durable rationale in
the [decision log](08-history/design-decisions-log.md), without repeating session chronology
in every guide. Legacy and new triggers have separate catalogs with shared dispatch rules.
The generator subfolder remains a separate reference set.

## Table of contents

### 1. Overview
- [Architecture overview](01-overview/architecture-overview.md) - the whole repo at a
  glance: what each top-level project is, how the processes relate at runtime (AuxiliaryServer
  + GameCore + a real UO client), and the Sphere-style `[ThingTemplate]` content model.
- [Repo structure](01-overview/repo-structure.md) - a directory-by-directory tour.
- [Glossary](01-overview/glossary.md) - Sphere/UO/engine-specific terms used throughout
  these docs (ItemDef, TagHolder, FieldValue, DupeItem, plevel, cliloc, etc).

### 2. Engine core (GameCore)

- [Runtime lifecycle](02-engine-core/runtime-lifecycle.md) — ownership, boot, recompile and shutdown.
- [Thing model](02-engine-core/thing-model.md) — identity, definitions, instances, placement and deletion.
- [Scripting pipeline](02-engine-core/scripting-pipeline.md) — compilation, loading and generated outputs.
- [Generator reference](02-engine-core/code%20generators/generator-architecture-guidance.md) — separate generator documentation.
- [LScript](02-engine-core/lscript.md) — invocation context, language constraints and commands.
- [Persistence](02-engine-core/persistence-and-saving.md) — saved state, overrides, load completion and failures.
- [NPC definition loading](02-engine-core/npc-definition-loading.md) — typed ranges/lists and load order.
- [Tags and timers](02-engine-core/tags-and-timers.md) — keys, scheduling and timer ownership.
- [Trigger architecture](02-engine-core/trigger-system-architecture.md) — dispatch, ordering and arguments.
- [Legacy trigger catalog](02-engine-core/trigger-catalog.md) — inherited event families.
- [New trigger catalog](02-engine-core/new-trigger-catalog.md) — moriaengine extension events.
- [Deny chains](02-engine-core/triggers-and-deny-chains.md) — cancellation and permission results.
- [Plugins](02-engine-core/plugins.md) — attaching, dispatching, removing and saving behaviour.
- [Viewable classes and `.info`](02-engine-core/viewable-classes-and-info-dialog.md) — inspection metadata and descriptors.
- [Regions](02-engine-core/regions.md) — hierarchy, placement, flags and movement.
- [Networking](02-engine-core/networking-protocol.md) — sessions, targeting and packets.
- [Performance](02-engine-core/performance.md) — current constraints and measurement guidance.

### 3. Admin & GM tooling
- [The `.settings` dialog framework](03-admin-tooling/settings-dialog.md) -
  `SettingsMetaCategory`, `[SaveableData]`, how to add a new GM-tunable setting.
- [GM commands](03-admin-tooling/gm-commands.md) - `CommandsAuthorization`, the
  default-deny-above-plevel-4 fallback, the target-cursor command pattern (`.flip`/`.kill`
  shape) vs self-referential commands (`.TagList` shape).
- [World setup](03-admin-tooling/world-setup.md) - `.SetupWorld`, the add-only way to get things into the world
- [Dialog framework](03-admin-tooling/dialogs-framework.md) - `CompiledGumpDef`,
  `ImprovedDialog`/`GUTATable`/`GUTAButton`/`GUTACheckBox`, `DialogArgs`, dialog stacking.

### 4. Gameplay systems
- [Combat](04-gameplay-systems/combat.md)
- [Skills (overview)](04-gameplay-systems/skills-overview.md)
- [Magery & spells](04-gameplay-systems/magery-spells.md)
- [Notoriety & murder counts](04-gameplay-systems/notoriety.md)
- [Leveling & experience](04-gameplay-systems/leveling-experience.md)
- [Professions](04-gameplay-systems/professions.md)
- [Quests](04-gameplay-systems/quests/README.md) - one file per quest: the rank quests (Expert, Master, Grandmaster), the journal, and the rules for quest ports
- [Races & nations](04-gameplay-systems/races-and-nations.md)
- [Abilities & talents](04-gameplay-systems/abilities-and-talents.md)
- [Item ATTR flags](04-gameplay-systems/item-attr-flags.md) - the Sphere-style bitflag
  system (`attr_decay`, `attr_invis`, `attr_lockeddown`, ...), built out across #217-#225.
- [Item decay](04-gameplay-systems/item-decay.md) - the per-item-timer decay system and
  its corpse/item rate split.
- [Housing](04-gameplay-systems/housing.md)
- [Travel stones](04-gameplay-systems/travel-stones.md)
- [House lockdown & security](04-gameplay-systems/house-lockdown.md) - `attr_lockeddown`/
  `attr_secure` on top of the Housing system.
- [Doors](04-gameplay-systems/doors.md)
- [Corpses & death](04-gameplay-systems/corpses-and-death.md)
- [Trading & secure trade](04-gameplay-systems/trading-secure-trade.md)
- [Economy: banking & vendors](04-gameplay-systems/economy-banking-vendors.md)
- [Bankers, cheques and purses](04-gameplay-systems/banker.md)
- [Guilds and guildstones](04-gameplay-systems/guilds.md) - Erebor's guildstone, its menu and ranks (#884).
- [Parties](04-gameplay-systems/parties.md) - short-term groups and the `.party` dialog (#916).
- [Towns](04-gameplay-systems/towns.md) - Erebor's town stones: citizens, tax, treasury, mayors (#892).
- [Spawning](04-gameplay-systems/spawning.md)
- [Guard bosses](04-gameplay-systems/guard-boss.md)
- [Pets & NPC AI](04-gameplay-systems/pets-and-npc-ai.md)
- [Monster loot](04-gameplay-systems/monster-loot.md) - Erebor's @NPCRestock loot and
  i_monster_create gold, ported (#791); human NPCs' @NPCRestock clothing as newbie gear (#793).
- [Elite monsters](04-gameplay-systems/elite-monsters.md) - Erebor's rare elite form of spawned
  high-experience monsters: stronger, absorbs player damage, richer loot, leaves after 20 min (#794).
- [Monster abilities](04-gameplay-systems/monster-abilities.md) - Erebor's `TEVENTS=e_*` special
  attacks as passive abilities: every implemented one, where it hooks in, how it's tuned (#653, #797).
- [NPC speech](04-gameplay-systems/npc-speech.md) - Erebor's `[SPEECH]` blocks (TSPEECH), how hearing
  works, and the converted `[SpeechStub]` placeholders that hint GMs which speech is unported (#825).
- [Movement & character collision](04-gameplay-systems/movement-and-collision.md) - the
  `WalkRunOrFly` pipeline and Sphere's stamina-gated shove past other characters (#767).
- [Crafting & gathering](04-gameplay-systems/crafting-gathering.md)
- [Item double-click & the "use" action framework](04-gameplay-systems/item-use-and-dclick.md) -
  how dclick/`.usehand` decide what an item does, and the data-driven equip-and-use hook.
- [Day/night cycle & weather](04-gameplay-systems/day-night-weather.md)

### 5. Localization
- [Localization](05-localization.md) - the resx-based `CompiledLocStringCollection<T>`
  pipeline, `ScriptedLoc`, per-language override flow.

### 6. Separate tooling projects
- [SphereShardConverter](06-tooling/sphere-shard-converter.md) - the one-time Sphere →
  moriaengine content migration tool.
- [RemoteConsole](06-tooling/remote-console.md)
- [AuxiliaryServer](06-tooling/auxiliary-server.md) - login/account-list server, separate
  process from GameCore.

### 7. Testing
- [Test infrastructure](07-testing/test-infrastructure.md) - `GameServerPairFixture`,
  `TestGameClient`, the two-tier (fast/full-boot) test split, account/character conventions.
- [Known gotchas](07-testing/known-gotchas.md) - the accumulated list of sharp edges found
  while building protocol tests against this engine (encoding, account caps, the
  CanReach-after-deny-chain trap, ScriptArgs' dual shape, and more).

### 8. History & decisions
- [Design decisions log](08-history/design-decisions-log.md) - durable "why it works this
  way" reasoning distilled from session history, organized by system.
- [Known gaps & tech debt](08-history/known-gaps-and-tech-debt.md) - what's deliberately
  incomplete, deferred, or flagged as needing a future decision.

- [Research snapshots](08-history/research/README.md) — dated performance reports and concurrency analysis.
