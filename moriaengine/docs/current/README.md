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

## How to use this

Each page is written at a "middle ground" depth: enough to explain how a system actually
works and *why* it's built the way it is (including real bugs found, alternatives rejected,
and open gaps), without reproducing exact method signatures the code itself already states
better. File:line references point at the real, current location of the thing being
described - if a reference goes stale, that's a documentation bug, fix it.

A lot of the "why" content here was mined from the session history in the repo-root
`CLAUDE.md` - hard-won design reasoning that would otherwise eventually rotate out of a
file whose job is session continuity, not permanent reference.

## Table of contents

### 1. Overview
- [Architecture overview](01-overview/architecture-overview.md) - the whole repo at a
  glance: what each top-level project is, how the processes relate at runtime (AuxiliaryServer
  + GameCore + a real UO client), and the Sphere-style `[ThingTemplate]` content model.
- [Repo structure](01-overview/repo-structure.md) - a directory-by-directory tour.
- [Glossary](01-overview/glossary.md) - Sphere/UO/engine-specific terms used throughout
  these docs (ItemDef, TagHolder, FieldValue, DupeItem, plevel, cliloc, etc).

### 2. Engine core (GameCore)
- [The Thing model](02-engine-core/thing-model.md) - `Thing`/`AbstractItem`/`AbstractCharacter`,
  UIDs, the Item/Character class-per-template split, `.ct` `Def:`/`Vars:` fields.
- [Scripting & code generation pipeline](02-engine-core/scripting-pipeline.md) - how
  `[ThingTemplate X : Y]` `.ct` files become real C# via `ClassTemplateParser`, the two
  *different* kinds of `*.Generated.cs` files, and the manual regeneration workflow.
- [LScript](02-engine-core/lscript.md) - the embedded scripting language used by `.scp`/`.def`
  content and raw-LScript dialogs: triggers, the `{a,b}` random-range operator, known parser
  quirks.
- [Persistence & saving](02-engine-core/persistence-and-saving.md) - `FieldValue`,
  `ConvertTools.ConvertTo`, the generic save/load pipeline, `isLoadingFromScript`, and the
  real bug it was added to fix (#168).
- [Tags & timers](02-engine-core/tags-and-timers.md) - `TagKey`/`TagHolder`, `Timer`/
  `BoundTimer`, why timers are "just tags" and what that buys you for persistence.
- [Triggers & deny chains](02-engine-core/triggers-and-deny-chains.md) - the
  `TryCancellableTrigger`/`On_Deny*`/`args.Result` pattern used everywhere an action can be
  blocked (pickup, drop, trade, equip, door open/close).
- [ViewableClasses & the `.info` dialog](02-engine-core/viewable-classes-and-info-dialog.md) -
  reflection-based property discovery, `[ViewDescriptor]`/`[Button]` injection, and when to
  use that instead of a dedicated dialog.
- [Regions](02-engine-core/regions.md) - `Region`/`StaticRegion`/`DynamicRegion`/
  `FlaggedRegion`, how dynamic regions (houses, ships) take priority over static ones.
- [Networking & the protocol layer](02-engine-core/networking-protocol.md) - packet
  read/write, `GameState`, login → character list → enter world, the target-cursor flow.

### 3. Admin & GM tooling
- [The `.settings` dialog framework](03-admin-tooling/settings-dialog.md) -
  `SettingsMetaCategory`, `[SaveableData]`, how to add a new GM-tunable setting.
- [GM commands](03-admin-tooling/gm-commands.md) - `CommandsAuthorization`, the
  default-deny-above-plevel-4 fallback, the target-cursor command pattern (`.flip`/`.kill`
  shape) vs self-referential commands (`.TagList` shape).
- [Dialog framework](03-admin-tooling/dialogs-framework.md) - `CompiledGumpDef`,
  `ImprovedDialog`/`GUTATable`/`GUTAButton`/`GUTACheckBox`, `DialogArgs`, dialog stacking.

### 4. Gameplay systems
- [Combat](04-gameplay-systems/combat.md)
- [Skills (overview)](04-gameplay-systems/skills-overview.md)
- [Magery & spells](04-gameplay-systems/magery-spells.md)
- [Notoriety & murder counts](04-gameplay-systems/notoriety.md)
- [Leveling & experience](04-gameplay-systems/leveling-experience.md)
- [Professions](04-gameplay-systems/professions.md)
- [Races & nations](04-gameplay-systems/races-and-nations.md)
- [Abilities & talents](04-gameplay-systems/abilities-and-talents.md)
- [Item ATTR flags](04-gameplay-systems/item-attr-flags.md) - the Sphere-style bitflag
  system (`attr_decay`, `attr_invis`, `attr_lockeddown`, ...), built out across #217-#225.
- [Item decay](04-gameplay-systems/item-decay.md) - the per-item-timer decay system and
  its corpse/item rate split.
- [Housing](04-gameplay-systems/housing.md)
- [House lockdown & security](04-gameplay-systems/house-lockdown.md) - `attr_lockeddown`/
  `attr_secure` on top of the Housing system.
- [Doors](04-gameplay-systems/doors.md)
- [Corpses & death](04-gameplay-systems/corpses-and-death.md)
- [Trading & secure trade](04-gameplay-systems/trading-secure-trade.md)
- [Economy: banking & vendors](04-gameplay-systems/economy-banking-vendors.md)
- [Spawning](04-gameplay-systems/spawning.md)
- [Pets & NPC AI](04-gameplay-systems/pets-and-npc-ai.md)
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
