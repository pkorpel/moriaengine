# The `.settings` dialog framework

GM-tunable, persistent configuration values, grouped into categories and edited live
in-game - no server restart, no `.ini` editing, no code change needed to tweak a number.

## How it's wired together

`.settings` dispatches to `SettingsCategories.Settings` (`scripts/Basic/administration/
Settings/MetaSettings.cs:33`), which opens the generic reflection-based `.info` dialog
(`D_Info` - see [viewable-classes-and-info-dialog.md](../02-engine-core/viewable-classes-and-info-dialog.md))
against a `DialogArgs` wrapping the singleton `SettingsCategories.instance`.

`SettingsCategories` (`MetaSettings.cs:28`) is itself a `[ViewableClass]`. Every public
property on it tagged `[InfoField("Category Name")]` becomes one row in the top-level
settings list, and each one returns a `SettingsMetaCategory` subclass singleton - clicking
that row opens `D_Info` again, this time against that singleton, showing whatever
`[SaveableData]` fields it declares.

`SettingsMetaCategory` (`MetaSettings.cs:20`) itself is just an empty marker base class -
all it does is let `D_Info`'s own dispatch logic recognize "this is a settings category,"
nothing more.

## Why edits are live AND persistent

Two independent mechanisms stack on top of each other:

- **Live editing**: `SettingsProvider.AssertSettings` (`scripts/Basic/administration/
  Settings/SettingsProvider.cs:36`) is what actually applies a dialog submission - for each
  edited field it compares the submitted string against the field's current string value
  (via the generic `IDataFieldView`/`ObjectSaver` machinery the whole `.info` reflection
  system already uses) and calls `field.SetStringValue` or `field.SetValue` (enums are
  special-cased, matched by name) if it changed. No settings-specific plumbing here at
  all - it's the exact same reflection+save-string round-trip every other `.info`-editable
  property already supports.
- **Persistence**: a settings category class is marked `[SaveableClass][HasSavedMembers]`,
  its singleton instance is `[SavedMember]`, and each tunable field is `[SaveableData]` -
  the same generic attribute-driven save pipeline described in
  [persistence-and-saving.md](../02-engine-core/persistence-and-saving.md). There's no
  special "settings save" step; a category's current values just get written out and
  reloaded like any other `[SaveableClass]` object.

## Adding a new category - the recipe

1. Write a new class extending `SettingsMetaCategory`, marked `[SaveableClass][HasSavedMembers]
   [ViewableClass]`, with a `[SavedMember] public static YourSettings instance = new
   YourSettings();` singleton and a `[LoadingInitializer]` parameterless constructor.
2. Add `[SaveableData]` fields for each tunable value (any type `ObjectSaver` can
   round-trip to/from a string - numbers, strings, enums are the common case).
3. Register it with one new `[InfoField("Category Name")]` property on
   `SettingsCategories` (`MetaSettings.cs`), returning `YourSettings.instance`.

`scripts/Basic/skills/Combat/CombatSettings.cs` is the cleanest template to copy - a flat
list of `double`/`int` fields (attack values, ranges, speeds), nothing exotic.

## Real categories as of this writing

Read straight off `SettingsCategories`'s own `[InfoField(...)]` properties
(`MetaSettings.cs:54-105`):

| Category | Backing class | Notes |
|---|---|---|
| Database Settings | `DbManager.Config` (a `DbConfig`) | not a `SettingsMetaCategory` subclass, just returned directly - the dispatch only cares that `D_Info` can render it |
| Combat Settings | `CombatSettings` | the template example above |
| Magery Settings | `MagerySettings` | |
| Poisons Settings | `PoisoningSettings` | |
| Abilities | `AllAbilitiesMassSetting` | a `MassSettings_ByClass_List<AbilityDef>`, a different shape entirely (bulk-editing many `AbilityDef`s at once, not a flat settings bag) |
| Level System | `LevelingSettings` | `nextLevelCoefficient`, `experienceLossPercentage`, `pkGainPercentage` |
| Profession System | `ProfessionSettings` | `enforceWeaponArmorRestrictions`, `grantSkillsOnAssign` |
| Item Decay | `DecaySettings` | see below |
| Notoriety | `NotorietySettings` | `murdererThreshold`, `criminalFlagSeconds`, `wildMonstersRenderOrange` - see [Notoriety & murder counts](../04-gameplay-systems/notoriety.md) |
| Movement | `CharCollisionSettings` | `requireFullStamina`, `flatPushCost`, `playersBlockedByCharacters`, `npcsBlockedByCharacters`, `fatigueAndOverload`, `maxLoadPercent`, `staminaLossAtWeight`, `runningStaminaPenalty` - see [Movement & character collision](../04-gameplay-systems/movement-and-collision.md) |
| NPC Aggro | `NpcAggroSettings` | `aggroRange`, `giveUpRange`, `npcsDefendThemselves`, `petsAssistOwner` - see [Pets & NPC AI](../04-gameplay-systems/pets-and-npc-ai.md#pet-commands-768) |
| Logout | `LogoutSettings` | `logoutDelaySeconds` (default 60), `showLogoutEffect` - see [Networking & the protocol layer](../02-engine-core/networking-protocol.md#disconnect-lingering-and-the-logout-delay-261) |

## Worked example: `DecaySettings`

The most recently added category (`scripts/Basic/ItemClasses/DecaySettings.cs`, #219/#236)
is a good illustration of "don't collapse genuinely independent tunables into one value
just because the first draft only needed one": it started as a single
`decayIntervalMinutes` field, then got split into three independent rates once the real
requirement turned out to be "a player's own corpse, an NPC's corpse, and a plain decaying
item are three different real-world defaults" -
`playerCorpseDecayMinutes` (10), `npcCorpseDecayMinutes` (5), `itemDecayMinutes` (5). See
[item-decay.md](../04-gameplay-systems/item-decay.md) for how these actually get consumed.
