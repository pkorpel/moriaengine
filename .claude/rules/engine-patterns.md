---
paths:
  - "moriaengine/**/*.cs"
---

# Engine code

How the engine works is in `moriaengine/docs/current/`. This file only says which page to read first, and
lists the rules whose breakage is silent.

## Pattern first (#1366)

Before using or changing an engine mechanism, read its page and the existing code. The original engine is
commit `a27bbf6` (`git show a27bbf6:<path>`). If the page is missing or wrong, fix it first (its own PR is
fine). Build in that pattern. A bug in the pattern is fixed in the pattern, never worked around with a second
mechanism; the reviewer blocks one. Prefer a plugin that reacts to triggers over a static class called from many
files, and typed def fields over tags.

## Touching X? Read this first (`moriaengine/docs/current/…`)

| Area | Page |
|---|---|
| Things, defs, def names, containment, stacking, deletion | `02-engine-core/thing-model.md` |
| Triggers: how they work, adding one, TriggerGroup vs plugin | `02-engine-core/trigger-system-architecture.md` |
| Which triggers exist | `02-engine-core/trigger-catalog.md` (lists the state before the AI-added triggers, #1558) |
| Plugin details (declaring, saving, deleting) | `02-engine-core/plugins.md`. It and `triggers-and-deny-chains.md` are disputed (PR #1532, #1558): where they disagree with the two pages above, those win |
| Timers, tags, lock order | `02-engine-core/tags-and-timers.md` |
| Saving, loading, code-made defs | `02-engine-core/persistence-and-saving.md` |
| Generated code (Roslyn generators, compile time) | `02-engine-core/scripting-pipeline.md`, `code generators/scripting-generators.md` |
| `.info`/`.settings` dialogs, settings categories | `03-admin-tooling/info-and-settings-dialogs.md` |
| Commands, permissions, LScript | `03-admin-tooling/gm-commands.md`, `02-engine-core/lscript.md` |
| Regions, sectors, map | `02-engine-core/regions.md`, `04-gameplay-systems/movement-and-collision.md` |
| Threads | `02-engine-core/multithreading-and-stm.md` |
| Gumps | `03-admin-tooling/dialogs-framework.md` |
| A gameplay system | its page under `04-gameplay-systems/` |

## Adding a trigger

Check `trigger-catalog.md` for an existing one first. A new trigger is agreed on its issue before it's added:
the naming and chaining convention is still open (#1558), and earlier AI-added triggers were criticised as
ad-hoc. Build it as `trigger-system-architecture.md` section 8 says: pick the semantic type (notification, deny,
implementation, calculation), and write a `Trigger_X` method that raises `@X` with the matching dispatch and
then calls `On_X`. Never drop a bare `TryTrigger` call into gameplay code.

## Rules whose breakage is silent

- **Compare defs by object** (`x.Def == ThingDef.GetByDefname("c_foo")`), never by a name string.
- **Player commands go on the allowlist** in `CommandsAuthorization.cs` in the same PR. Commands are authorized
  against MaxPLevel, so test them from an auto-created account. A command name that matches a member on the
  character runs the member instead, so pick distinct names.
- **Tunables go on `.settings`** in the same PR. Monster ability defaults go on `MonsterAbilitiesSettings`
  (`<Ability><Property>`), which a coverage test enforces.
- **Never edit generated output.** Change the attributed source or the generator.
- **Lock order:** `globalLock`, then a timer's lock. Take nothing while holding `Timer.changesLock`.
- **Every gold payment by a player goes through `GoldUtils.Pay`.**
- **Anything saved must be saveable.** An unsaveable value is only logged as an error during the save
  (`ObjectSaver.FlushCache`) and is gone after a restart. Prove persistence with a real save, restart and reload.
- **Click text goes through `Item.ClickMessage`**, never a SysMessage (TazUO clicks on hover).
- **Zero build warnings.**
