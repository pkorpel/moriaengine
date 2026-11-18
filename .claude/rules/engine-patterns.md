---
paths:
  - "moriaengine/**/*.cs"
---

# Engine patterns and gotchas

## Pattern first (#1366)

Before using or changing an engine mechanism (plugins, code-made defs, def fields, tags, triggers,
timers, persistence), read its page in `moriaengine/docs/current/02-engine-core/` and the existing code.
The original engine is commit `a27bbf6` (`git show a27bbf6:<path>`). If the page is missing or wrong,
fix it first (a PR of its own is fine). Build in that pattern. A bug found in the pattern is fixed in the
pattern, and the reviewer blocks a second mechanism. Prefer a plugin that reacts to triggers over a
static class called from many files, and typed def fields over tags for def values.

Generated code: `scripting-pipeline.md` and `code generators/*.md` describe the Roslyn generators
(things/plugins, `[SteamFunction]`, deep copy, save) and the boot-time `Generated/` output. Never edit
generated output.

## Defs and names

- **Compare defs by object**: `x.Def == ThingDef.GetByDefname("c_foo")` (case-insensitive). Never compare
  name strings: `PrettyDefname` is lowercased, a numbered def's `Defname` is its number (`0x19b7`), a
  skill's `PrettyDefname` is `skill_<id>`, and a `ProfessionDef`'s key is its header (`profession_mage`).
  A string `switch` on a name silently never matches.
- `GetByModel` can return an NPCDef; use `FindItemDef` for items.
- Every code-made def is an `instance` on its def class and registered before the world loads
  (`CheckCodeMadeDefs` reports misses). A hand-written def class needs a `Bootstrap()`, marked `new`.
  `GetByDefname` inside a static initializer caches null.
- Two defs on one numeric `[Def 0xHEX]` slot: the loser's whole block, `DEFNAME` included, is dropped
  silently, or shows up as a confusing FormatException.
- `SingletonScript<T>.Instance` matches **any subclass** of T. For dialogs, use an abstract base with
  sealed leaves, or `GumpDef.GetByDefname("ClassName")`.
- FieldValue keys of an `AbilityDef` subclass must be unique; reusing the base's `chance`/`cooldown`
  fails silently at boot. A list field named like its keyword loads in the first pass, so name list
  fields differently.

## Commands and permissions

- A `[SteamFunction]` command is GM-only until it's on `E_CommandsAuthorization_Global`'s player list
  (`scripts/Basic/administration/CommandsAuthorization.cs`). Commands are authorized against the account's
  **MaxPLevel**, not PLevel, and only on the typed word. Never allowlist a property just because a dialog
  reads it.
- Game commands resolve on the character first. A command name that matches a member on `self` (`Race`,
  `Disconnect`, `RpRank`) or a class name runs the member instead, with no error. SteamFunction names are
  global, so pick distinct ones.
- `Test*` commands bypass authorization only when `STEAMENGINE_TEST_COMMANDS=1` (set by the test
  fixture). Never set it on a real server.

## Settings

- Tunables go on a `.settings` category: a `SettingsMetaCategory` subclass (model: `CombatSettings.cs`)
  registered with `[InfoField]` on `SettingsCategories` (`MetaSettings.cs`). Property names must be
  unique across categories (use a `shared` prefix), or pages overwrite each other.
- Every monster ability's defaults also go on `MonsterAbilitiesSettings` as `<Ability><Property>`.
  `MonsterAbilitiesSettingsCoverageTests` enforces it.
- `.info` edits the live NPC, not its def. A GM-tunable per-NPC value needs an instance field seeded
  from the def.

## Triggers and plugins

- Plugins hear triggers newest first, and that order survives save, load and dupe. `Plugin.IsDeleted` is
  real. `DeletePlugins` works in rounds, so an `@unAssign` may add or remove plugins.
- Erebor's `@AfterSwing`/`@aftergetswing` are our `@swingLanding`/`@getSwingLanding`, which run before the
  dodge. `@hit` runs after it. `@afterHit` is skipped on a killing blow against an NPC, so use `@kill`.
- `On_EnterRegion` fires before `On_Create`. Never call `Disconnect()` inside `On_LogOut`; use a timer.
- `@dclick` is cancellable, so a TEVENTS trigger group can replace a def's `On_DClick`.

## Timers and threads

- Lock order: `globalLock`, then the timer lock. `Timer.changesLock` is a leaf lock.
- A one-shot `BoundTimer` is deleted before `OnTimeout`, so re-arming inside it does nothing: give it a
  period. A timer whose Period and Due both read as disabled deletes itself. `TagHolder.AddTimer` keeps
  one timer per key, so a shared key silently replaces another timer.
- `Globals.src` is the last packet sender. From a timer, message the characters involved explicitly.
- Disconnected characters (ridden mounts, offline players) aren't ticked. Their skill timers resume at
  login. NPCs far from players sleep (`npcAreaAwakeSeconds` in `steamengine.ini`).
- A dupe copies no timers. Duping an NPC makes a new NPC of its def. Both follow Sphere.

## Things, items, damage

- The `Cont` setter merges stackables and deletes the incoming item, so set `Amount` before `Cont`/`P()`.
- Use `SetAttribute`. Setting `Flag_NonMovable`/`Attributes` directly skips decay and side effects.
- `Character.Weapon` is a cache that a scripted equip or delete leaves stale; call
  `InvalidateCombatWeaponValues` or read the hand layers. Two-handed weapons are layer 1.
- Floor damage at 0, because negative damage heals. `DamageType.Irresistable` is 0 (compare with `==`).
  `DamageType.Magic` throws; use `MagicMystical`. `CauseDeath(null)` happens (starving, scripted kills),
  and it dismounts before `On_Death`.
- Damage paths: `CauseDamage`/`CauseCustomDamage`/`CauseSpellDamage` go through the full chain (Erebor's
  `damage_final`); `CauseDamageNoResponse` skips it (Erebor's `damage_noresponse`).
- `UseThing` returns true even when `OnThing` refuses. A `Go` onto a step item fires it.

## Persistence

- Statics that must survive a restart need `[SaveableClass]`/`[SaveableData]`, and any type stored on a
  saved object must be saveable: one unsaveable value aborts the whole world save. Prove persistence with
  a real save, restart and reload.
- Tag keys containing `/` collapse on restart. Make defaults coded, not one-time saved flags.
- The server is blank: write no migration code for old saves.

## Map, regions, movement

- LOS: statics block on `Noshoot`, terrain on `Impassable`. `Surface`/`Roof` floors block along the
  whole sight line. Reach uses Sphere's 3D distance. `GetFixedZ` lands on the floor.
- A sector tie-break picks the smaller region. Get a house's city by geometry, not by its `Parent`.
- Each dungeon has its own Mapplane; plane 2 is `map2.mul`. Plane 250 is the hidden parking plane.
- Forced moves go through `ForceMove`, and pathing through `AStar.FindPath`. An NPC moves at most one tile
  per AI tick; MoveRate scales how often it thinks.

## Gumps and clients

- Button 0 is the right-click close, so handle it. The server accepts any button id a client sends.
- TazUO: `checkertrans` dims everything it overlaps. HTML without a background is black unless it has a
  scrollbar (`AddHtml(..., false, true)` or a basefont). TazUO sends real single clicks on hover, so click
  text goes in `Item.ClickMessage`, never in a SysMessage. Check a client handler's preconditions
  in `/workspace/TazUO` (secure trade needs both boxes known).

## Builds

- Zero warnings. Release builds strip `Logger.WriteDebug` and packet logs. `FatalException` must
  escalate.
