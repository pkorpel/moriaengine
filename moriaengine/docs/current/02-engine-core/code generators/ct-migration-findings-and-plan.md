# `.ct` migration findings and plan

This note maps the remaining `.ct` templates to their C# counterparts and proposes a migration plan to `SteamEngine.Generators.ThingAndPluginClasses.Generator`.

## Scope scanned

- `scripts/**/*.ct`
- non-generated C# script sources (`scripts/**/*.cs`, excluding `*.Generated.cs`)

## Findings summary

- **116** `.ct` files
- **140** template declarations (`[ThingTemplate ...]` / `[PluginTemplate ...]`)
- **14** `.ct` files define **multiple** templates
- **0** templates missing an instance class counterpart
- **69** templates where ct `Vars:` fields are currently generated-only and must be authored into the instance class during migration
- **8** templates where instance classes already have **extra instance fields not present in ct `Vars:`** (high risk of persistence behavior change)
- **1** template with `Def:` fields but missing a source `*Def` class counterpart (`PotionKegDef`)

---

## Main caveat confirmed: accidental persistence changes

The current generator persists/copies instance **fields declared in the class**. During migration, `Vars:` values from `.ct` are moved into source classes. If a class already has other instance fields that were never part of `.ct` `Vars:`, those fields may become persisted/copied unless explicitly handled.

### High-risk templates (extra instance fields already present)

1. `Basic/CharClasses/Character.ct` (`Character`)
   - extra fields: `charModelInfo`, `combatArmorValues`, `weaponProjectile`
2. `Basic/ItemClasses/Container.ct` (`Container`)
   - extra field: `weight`
3. `Basic/ItemClasses/Corpse.ct` (`Corpse`)
   - extra fields: `charDef`, `hairItemsPackets`, `hasEquippedItems`, `hasHairItems`
4. `Basic/ItemClasses/Item.ct` (`Item`)
   - extra fields: `text`, `until`
5. `Basic/ItemClasses/MultiItem.ct` (`MultiItem`)
   - extra field: `region`
6. `Basic/LivingWorld/Venues/CityVenue.ct` (`CityVenue`)
   - extra fields: `rectsCache`, `rectsCacheSource`
7. `Basic/skills/Crafting/CraftingProcessPlugin.ct` (`CraftingProcessPlugin`)
   - extra field: `stopped`
8. `Basic/skills/Tracking/TrackingPlugin.ct` (`PlayerTrackingPlugin`)
   - extra field: `lastRefreshAt`

These are the primary places where “do not make this persistent” behavior can be broken.

---

## Other migration problems found

### Missing def counterpart class (must be added)

- `Basic/Potions/PotionKeg.ct`
  - `Def:` contains `capacity`
  - source has `PotionKeg` class, but no source `PotionKegDef` partial class
  - migration requires adding `PotionKegDef` source partial with `[ThingDefTemplate]` and `[DefField]` field(s)

### Multi-template files (batch migration complexity)

14 files define multiple template classes in one `.ct` file (e.g. `MysticPlugins.ct`, `TrackingPlugin.ct`, `playerVendor.ct`).
Migration should be done per-file with all contained template classes converted together to avoid mixed old/new behavior in one file.

---

## Migration plan

## Phase 0: safety preconditions (before bulk conversion)

1. **Add explicit field opt-out for generated persistence/copy/load** (recommended)
   - Add an attribute-level opt-out for instance fields (e.g. `Persist = false` / `Copy = false` / `Load = false`), or equivalent explicit inclusion model.
   - This is the safest way to preserve current behavior in the 8 high-risk templates.
2. Add focused generator tests for the opt-out behavior.

> Why first: without this, converting ct vars into source classes forces us to reshape runtime-only fields into properties/tags manually in risky classes, which is error-prone at this scale.

## Phase 1: convert low-risk templates first

- Migrate templates that have no “extra field” risk.
- For each `.ct` file:
  - add template attributes to source classes,
  - move `Vars:` into instance fields,
  - move `Def:` into `[DefField] private FieldValue ...` on `*Def` classes,
  - remove `.ct` file,
  - verify build and targeted tests.

## Phase 2: convert high-risk templates with explicit preservation

For the 8 high-risk templates, do one-by-one PR-sized changes:

- classify each extra field as one of:
  - persistent by design (keep generated handling),
  - runtime/transient/cache-only (mark opt-out or move out of generated field surface).
- add/adjust tests where behavior is subtle (especially tracking/crafting/region-cache/item flags/corpse visuals).

## Phase 3: resolve structural outliers

- Add missing `PotionKegDef` source partial and migrate `PotionKeg.ct`.
- Handle multi-template files carefully (convert entire file cohort in one change each).

## Phase 4: cleanup and hardening

- Remove all remaining `.ct` files.
- Add a guard test/tool check that fails if any `.ct` remains under `scripts/`.
- Run fast suite, then full suite before final merge.

---

## Execution checklist per `.ct` file

1. Add `[ThingTemplate]` / `[PluginTemplate]` and `[ThingDefTemplate]` / `[PluginDefTemplate]` in source classes.
2. Move `Vars:` to instance class fields with same defaults.
3. Move `Def:` to def class via `[DefField] private FieldValue ...`.
4. Preserve transient/non-persistent behavior for extra fields.
5. Delete `.ct` file.
6. Validate generated output compiles and behavior-critical tests pass.

---

## Appendix A: full template map

| `.ct` file | template class | kind | source class file(s) | source def file(s) | `Vars:` fields | `Def:` fields | migration notes |
|---|---|---|---|---|---:|---:|---|
| `Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingPlugins.ct` | `BleedingStrikePlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingStrikePlugin.cs | Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingStrikePlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingPlugins.ct` | `BleedingEffectPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingEffectPlugin.cs<br>Basic/EffectDuration/ActiveEffects.cs | Basic/AbilitiesAndTalents/Abilities/Bleeding/BleedingEffectPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Abilities/LongRange/LongRangePlugin.ct` | `LongRangePlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Abilities/LongRange/LongRangePlugin.cs | Basic/AbilitiesAndTalents/Abilities/LongRange/LongRangePlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Abilities/Warcry/Warcry.ct` | `WarcryEffectPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Abilities/Warcry/Warcry.cs | Basic/AbilitiesAndTalents/Abilities/Warcry/Warcry.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.ct` | `EntangledPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.cs | Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.ct` | `VineHoldPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.cs | Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/AbilitiesAndTalents/Talents/Bloodritual/Bloodritual.ct` | `BloodritualPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/Bloodritual/BloodritualPlugin.cs | Basic/AbilitiesAndTalents/Talents/Bloodritual/BloodritualPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/ClearCast/ClearCastPlugin.ct` | `ClearCastPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/ClearCast/ClearCastPlugin.cs | Basic/AbilitiesAndTalents/Talents/ClearCast/ClearCastPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/CoolDownSpeedup/CoolDownSpeedupPlugin.ct` | `CoolDownSpeedupPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/CoolDownSpeedup/CoolDownSpeedupPlugin.cs | Basic/AbilitiesAndTalents/Talents/CoolDownSpeedup/CoolDownSpeedupPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/EnergyShield/EnergyShieldPlugin.ct` | `EnergyShieldPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/EnergyShield/EnergyShieldPlugin.cs | Basic/AbilitiesAndTalents/Talents/EnergyShield/EnergyShieldPlugin.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionPlugins.ct` | `IgnitionActivatedPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionActivatedPlugin.cs | Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionActivatedPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionPlugins.ct` | `IgnitionEffectPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionEffectPlugin.cs | Basic/AbilitiesAndTalents/Talents/Ignition/IgnitionEffectPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/ManaSaving/ManaSavingPlugin.ct` | `ManaSavingPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/ManaSaving/ManaSavingPlugin.cs | Basic/AbilitiesAndTalents/Talents/ManaSaving/ManaSavingPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/SkillDelaySpeedup/SkillDelaySpeedupPlugin.ct` | `SkillDelaySpeedupPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/SkillDelaySpeedup/SkillDelaySpeedupPlugin.cs | Basic/AbilitiesAndTalents/Talents/SkillDelaySpeedup/SkillDelaySpeedupPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellClassDiscountPlugin.ct` | `SpellClassDiscountPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellClassDiscountPlugin.cs | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellClassDiscountPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellManaDiscountPlugin.ct` | `SpellManaDiscountPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellManaDiscountPlugin.cs | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellManaDiscountPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellSchoolManaDiscountPlugin.ct` | `SpellSchoolManaDiscountPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellSchoolManaDiscountPlugin.cs | Basic/AbilitiesAndTalents/Talents/SpellManaDiscounts/SpellSchoolManaDiscountPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/SpellPowerBonuses/SpellPowerBonusPlugin.ct` | `SpellPowerBonusPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/SpellPowerBonuses/SpellPowerBonusPlugin.cs | Basic/AbilitiesAndTalents/Talents/SpellPowerBonuses/SpellPowerBonusPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/Standcast/Standcast.ct` | `StandcastPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/Standcast/StandcastPlugin.cs | Basic/AbilitiesAndTalents/Talents/Standcast/StandcastPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/AbilitiesAndTalents/Talents/Standcast/Standcast.ct` | `CastFreezePlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/Standcast/StandcastPlugin.cs | Basic/AbilitiesAndTalents/Talents/Standcast/StandcastPlugin.cs | 0 | 0 | none |
| `Basic/AbilitiesAndTalents/Talents/WeaponSkillDmgBonus/WeaponSkillDmgBonusPlugin.ct` | `WeaponSkillDmgBonusPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/WeaponSkillDmgBonus/WeaponSkillDmgBonusPlugin.cs | Basic/AbilitiesAndTalents/Talents/WeaponSkillDmgBonus/WeaponSkillDmgBonusPlugin.cs | 0 | 1 | none |
| `Basic/AbilitiesAndTalents/Talents/WhirlWind/WhirlWindPlugin.ct` | `WhirlWindPlugin` | `PluginTemplate` | Basic/AbilitiesAndTalents/Talents/WhirlWind/WhirlWindPlugin.cs | Basic/AbilitiesAndTalents/Talents/WhirlWind/WhirlWindPlugin.cs | 0 | 0 | none |
| `Basic/CharClasses/Character.ct` | `Character` | `ThingTemplate` | Basic/CharClasses/Character.cs<br>Basic/CharClasses/PetCommands.cs<br>Basic/Housing/HouseEntry.cs | Basic/CharClasses/Character.cs | 15 | 27 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/CharClasses/NPC.ct` | `NPC` | `ThingTemplate` | Basic/CharClasses/NPC.cs | Basic/CharClasses/NPC.cs | 4 | 149 | ct_vars_missing_in_class_fields |
| `Basic/CharClasses/Player.ct` | `Player` | `ThingTemplate` | Basic/CharClasses/Player.cs<br>Basic/Speech/SkillTeacherSpeech.cs | Basic/CharClasses/Player.cs | 15 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/HypnosisPlugins.ct` | `PossessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/HypnosisPlugins.cs | Basic/ClassAbilities/HypnosisPlugins.cs | 22 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/HypnosisPlugins.ct` | `AlterEgoPlugin` | `PluginTemplate` | Basic/ClassAbilities/HypnosisPlugins.cs | Basic/ClassAbilities/HypnosisPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/HypnotizerBall.ct` | `HypnotizerBall` | `ThingTemplate` | Basic/ClassAbilities/MysticMind.cs | Basic/ClassAbilities/MysticMind.cs | 0 | 0 | none |
| `Basic/ClassAbilities/LifeCandle.ct` | `LifeCandle` | `ThingTemplate` | Basic/ClassAbilities/PriestAbilities.cs | Basic/ClassAbilities/PriestAbilities.cs | 0 | 0 | none |
| `Basic/ClassAbilities/LifePactPlugin.ct` | `LifePactPlugin` | `PluginTemplate` | Basic/ClassAbilities/LifePactPlugin.cs | Basic/ClassAbilities/LifePactPlugin.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/LifePactPlugin.ct` | `EmpathyPlugin` | `PluginTemplate` | Basic/ClassAbilities/LifePactPlugin.cs | Basic/ClassAbilities/LifePactPlugin.cs | 0 | 0 | none |
| `Basic/ClassAbilities/MysticPlugins.ct` | `MysticProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | - | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `ConcentrationPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `CatchPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `CatchFieldPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `CaughtPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `ControlNpcPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `BuffStealPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 7 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/MysticPlugins.ct` | `BuffStolenPlugin` | `PluginTemplate` | Basic/ClassAbilities/MysticPlugins.cs | Basic/ClassAbilities/MysticPlugins.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/PactPlugin.ct` | `PactPlugin` | `PluginTemplate` | Basic/ClassAbilities/PactPlugin.cs | Basic/ClassAbilities/PactPlugin.cs | 8 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/PriestProfessionPlugin.ct` | `PriestProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/PriestProfessionPlugin.cs | - | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/RangerProfessionPlugin.ct` | `RangerProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/RangerProfessionPlugin.cs | - | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/ShamanItem.ct` | `ShamanItem` | `ThingTemplate` | Basic/ClassAbilities/ShamanPacts.cs | Basic/ClassAbilities/ShamanPacts.cs | 0 | 0 | none |
| `Basic/ClassAbilities/ShamanProfessionPlugin.ct` | `ShamanProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/ShamanProfessionPlugin.cs | - | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/ShamanProfessionPlugin.ct` | `VoodooStatsPlugin` | `PluginTemplate` | Basic/ClassAbilities/ShamanProfessionPlugin.cs | Basic/ClassAbilities/ShamanProfessionPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/ShamanProfessionPlugin.ct` | `VoodooShieldPlugin` | `PluginTemplate` | Basic/ClassAbilities/ShamanProfessionPlugin.cs | Basic/ClassAbilities/ShamanProfessionPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/ThiefProfessionPlugin.ct` | `ThiefProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/ThiefProfessionPlugin.cs | - | 8 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/VoodooPlugins.ct` | `VoodooEffectPlugin` | `PluginTemplate` | Basic/ClassAbilities/VoodooPlugins.cs | Basic/ClassAbilities/VoodooPlugins.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/VoodooPlugins.ct` | `VoodooHandlePlugin` | `PluginTemplate` | Basic/ClassAbilities/VoodooPlugins.cs | Basic/ClassAbilities/VoodooPlugins.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ClassAbilities/WarriorProfessionPlugin.ct` | `WarriorProfessionPlugin` | `PluginTemplate` | Basic/ClassAbilities/WarriorProfessionPlugin.cs | - | 6 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Collectors/Collector.ct` | `Collector` | `ThingTemplate` | Basic/Collectors/Collector.cs | Basic/Collectors/Collector.cs | 0 | 0 | none |
| `Basic/Conquest/CaptainStone.ct` | `CaptainStone` | `ThingTemplate` | Basic/Conquest/KeepCaptains.cs | Basic/Conquest/KeepCaptains.cs | 0 | 0 | none |
| `Basic/Conquest/FortCashBox.ct` | `FortCashBox` | `ThingTemplate` | Basic/Conquest/FortressPalantir.cs | Basic/Conquest/FortressPalantir.cs | 0 | 0 | none |
| `Basic/Conquest/FortressPalantir.ct` | `FortressPalantir` | `ThingTemplate` | Basic/Conquest/FortressPalantir.cs | Basic/Conquest/FortressPalantir.cs | 0 | 0 | none |
| `Basic/Conquest/WarDeclarator.ct` | `WarDeclarator` | `ThingTemplate` | Basic/Conquest/WarDeclarator.cs | Basic/Conquest/WarDeclarator.cs | 0 | 0 | none |
| `Basic/Economy/CashiersItem.ct` | `CashiersItem` | `ThingTemplate` | Basic/Economy/CashiersItem.cs | Basic/Economy/CashiersItem.cs | 0 | 1 | none |
| `Basic/EffectDuration/EffectDurationPlugin.ct` | `EffectDurationPlugin` | `PluginTemplate` | Basic/EffectDuration/EffectDurationPlugin.cs | Basic/EffectDuration/EffectDurationPlugin.cs | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/EffectDuration/FadingEffectDurationPlugin.ct` | `FadingEffectDurationPlugin` | `PluginTemplate` | Basic/EffectDuration/FadingEffectDurationPlugin.cs | Basic/EffectDuration/FadingEffectDurationPlugin.cs | 0 | 4 | none |
| `Basic/EffectDuration/StatDrainingEffectDurationPlugin.ct` | `StatDrainingEffectDurationPlugin` | `PluginTemplate` | Basic/EffectDuration/StatDrainingEffectDurationPlugin.cs | Basic/EffectDuration/StatDrainingEffectDurationPlugin.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Housing/House.ct` | `House` | `ThingTemplate` | Basic/Housing/House.cs<br>Basic/Housing/HouseBank.cs<br>Basic/Housing/HouseLock.cs<br>Basic/Housing/HouseRent.cs | Basic/Housing/House.cs | 0 | 0 | none |
| `Basic/Housing/RentChest.ct` | `RentChest` | `ThingTemplate` | Basic/Housing/RentChest.cs | Basic/Housing/RentChest.cs | 0 | 0 | none |
| `Basic/Hunger/Hunger.ct` | `HungerPlugin` | `PluginTemplate` | Basic/Hunger/HungerPlugin.cs | Basic/Hunger/HungerPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/ItemClasses/Bank/BankBox.ct` | `BankBox` | `ThingTemplate` | Basic/ItemClasses/Bank/BankBox.cs | Basic/ItemClasses/Bank/BankBox.cs | 0 | 0 | none |
| `Basic/ItemClasses/ColoredArmor.ct` | `ColoredArmor` | `ThingTemplate` | Basic/ItemClasses/ColoredArmor.cs | Basic/ItemClasses/ColoredArmor.cs | 0 | 2 | none |
| `Basic/ItemClasses/ColoredProjectile.ct` | `ColoredProjectile` | `ThingTemplate` | Basic/ItemClasses/ColoredProjectile.cs | Basic/ItemClasses/ColoredProjectile.cs | 0 | 2 | none |
| `Basic/ItemClasses/ColoredStaff.ct` | `ColoredStaff` | `ThingTemplate` | Basic/ItemClasses/ColoredStaff.cs | Basic/ItemClasses/ColoredStaff.cs | 1 | 1 | ct_vars_missing_in_class_fields |
| `Basic/ItemClasses/ColoredWeapon.ct` | `ColoredWeapon` | `ThingTemplate` | Basic/ItemClasses/ColoredWeapon.cs | Basic/ItemClasses/ColoredWeapon.cs | 0 | 2 | none |
| `Basic/ItemClasses/Container.ct` | `Container` | `ThingTemplate` | Basic/ItemClasses/Container.cs | Basic/ItemClasses/Container.cs | 0 | 1 | extra_instance_fields_not_in_ct_vars |
| `Basic/ItemClasses/Corpse.ct` | `Corpse` | `ThingTemplate` | Basic/ItemClasses/Corpse.cs | Basic/ItemClasses/Corpse.cs | 8 | 0 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/ItemClasses/Equippable.ct` | `Equippable` | `ThingTemplate` | Basic/ItemClasses/Equippable.cs | Basic/ItemClasses/Equippable.cs | 0 | 4 | none |
| `Basic/ItemClasses/Item.ct` | `Item` | `ThingTemplate` | Basic/Housing/HouseLock.cs<br>Basic/ItemClasses/Item.cs<br>Basic/ItemClasses/ItemDoor.cs<br>Basic/ItemClasses/ItemFlip.cs<br>Basic/LivingWorld/VenueLife/AmbientLife.cs | Basic/ItemClasses/Item.cs | 1 | 8 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/ItemClasses/LootHold.ct` | `LootHoldPlugin` | `PluginTemplate` | Basic/ItemClasses/LootHold.cs | Basic/ItemClasses/LootHold.cs | 0 | 0 | none |
| `Basic/ItemClasses/MultiItem.ct` | `MultiItem` | `ThingTemplate` | Basic/ItemClasses/MultiItem.cs | Basic/ItemClasses/MultiItem.cs | 1 | 0 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/ItemClasses/Musical.ct` | `Musical` | `ThingTemplate` | Basic/ItemClasses/Musical.cs | Basic/ItemClasses/Musical.cs | 0 | 2 | none |
| `Basic/ItemClasses/ShiningItem/ShiningItem.ct` | `ShiningItem` | `ThingTemplate` | Basic/ItemClasses/ShiningItem/ShiningItem.cs | - | 0 | 0 | none |
| `Basic/LivingWorld/Venues/CityVenue.ct` | `CityVenue` | `ThingTemplate` | Basic/LivingWorld/Venues/CityVenue.cs | - | 0 | 0 | extra_instance_fields_not_in_ct_vars |
| `Basic/MagicItems/MagicBag.ct` | `MagicBag` | `ThingTemplate` | Basic/MagicItems/MagicItems.cs | Basic/MagicItems/MagicItems.cs | 0 | 2 | none |
| `Basic/Moderation/ConvictPlugin.ct` | `ConvictPlugin` | `PluginTemplate` | Basic/Moderation/ConvictPlugin.cs | Basic/Moderation/ConvictPlugin.cs | 5 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Moderation/GmFreeze.ct` | `GmFreezePlugin` | `PluginTemplate` | Basic/Moderation/GmFreeze.cs | Basic/Moderation/GmFreeze.cs | 0 | 0 | none |
| `Basic/Movement/Immobile.ct` | `ImmobilePlugin` | `PluginTemplate` | Basic/Movement/Immobile.cs | Basic/Movement/Immobile.cs | 0 | 0 | none |
| `Basic/PlayerVendors/EreborVendor.ct` | `EreborVendor` | `ThingTemplate` | Basic/PlayerVendors/EreborVendor.cs | Basic/PlayerVendors/EreborVendor.cs | 0 | 0 | none |
| `Basic/Potions/PetFigurine.ct` | `PetFigurine` | `ThingTemplate` | Basic/Potions/PetFigurine.cs | Basic/Potions/PetFigurine.cs | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Potions/Potion.ct` | `Potion` | `ThingTemplate` | Basic/Potions/Potion.cs | Basic/Potions/Potion.cs | 0 | 1 | none |
| `Basic/Potions/PotionEffectPlugin.ct` | `PotionEffectPlugin` | `PluginTemplate` | Basic/Potions/PotionEffectPlugin.cs | Basic/Potions/PotionEffectPlugin.cs | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Potions/PotionKeg.ct` | `PotionKeg` | `ThingTemplate` | Basic/Potions/PotionKeg.cs | - | 2 | 1 | ct_vars_missing_in_class_fields, missing_def_class |
| `Basic/Professions/ProfessionPlugin.ct` | `ProfessionPlugin` | `PluginTemplate` | Basic/Professions/ProfessionPlugin.cs | Basic/Professions/ProfessionPlugin.cs | 5 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Pvp/PvpTournamentStone.ct` | `PvpTournamentStone` | `ThingTemplate` | Basic/Pvp/PvpTournament.cs | Basic/Pvp/PvpTournament.cs | 0 | 0 | none |
| `Basic/Quests/Boosters/BoosterPlugin.ct` | `BoosterPlugin` | `PluginTemplate` | Basic/Quests/Boosters/BoosterPlugin.cs | Basic/Quests/Boosters/BoosterPlugin.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Quests/Weekly/WeeklyQuestPlugin.ct` | `WeeklyQuestPlugin` | `PluginTemplate` | Basic/Quests/Weekly/WeeklyQuestPlugin.cs | Basic/Quests/Weekly/WeeklyQuestPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Races/RaceNationPlugin.ct` | `RaceNationPlugin` | `PluginTemplate` | Basic/Races/RaceNationPlugin.cs | Basic/Races/RaceNationPlugin.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Races/RealmPlugin.ct` | `RealmPlugin` | `PluginTemplate` | Basic/Races/RealmPlugin.cs | Basic/Races/RealmPlugin.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Regeneration/RegenerationPlugin.ct` | `RegenerationPlugin` | `PluginTemplate` | Basic/Regeneration/RegenerationPlugin.cs | Basic/Regeneration/RegenerationPlugin.cs | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/RentedPlaces/Field.ct` | `Field` | `ThingTemplate` | Basic/RentedPlaces/RentedPlaces.cs | Basic/RentedPlaces/RentedPlaces.cs | 0 | 0 | none |
| `Basic/RentedPlaces/Stall.ct` | `Stall` | `ThingTemplate` | Basic/RentedPlaces/RentedPlaces.cs | Basic/RentedPlaces/RentedPlaces.cs | 0 | 0 | none |
| `Basic/Roles/Guild/GuildStone.ct` | `GuildStone` | `ThingTemplate` | Basic/Roles/Guild/GuildStone.cs | Basic/Roles/Guild/GuildStone.cs | 0 | 0 | none |
| `Basic/skills/Combat/Destroyable.ct` | `Destroyable` | `ThingTemplate` | Basic/skills/Combat/Destroyable.cs | Basic/skills/Combat/Destroyable.cs | 2 | 2 | ct_vars_missing_in_class_fields |
| `Basic/skills/Combat/Plugins.ct` | `WeaponSkillTargetQueuePlugin` | `PluginTemplate` | Basic/skills/Combat/WeaponSkillTargetQueuePlugin.cs | Basic/skills/Combat/WeaponSkillTargetQueuePlugin.cs | 0 | 0 | none |
| `Basic/skills/Combat/Plugins.ct` | `WeaponSkillTargetTrackerPlugin` | `PluginTemplate` | Basic/skills/Combat/WeaponSkillTargetTrackerPlugin.cs | Basic/skills/Combat/WeaponSkillTargetTrackerPlugin.cs | 0 | 0 | none |
| `Basic/skills/Combat/Projectile.ct` | `Projectile` | `ThingTemplate` | Basic/skills/Combat/Projectile.cs | Basic/skills/Combat/Projectile.cs | 0 | 6 | none |
| `Basic/skills/Combat/Weapon.ct` | `Weapon` | `ThingTemplate` | Basic/skills/Combat/Weapon.cs | Basic/skills/Combat/Weapon.cs | 0 | 19 | none |
| `Basic/skills/Combat/Wearable.ct` | `Wearable` | `ThingTemplate` | Basic/skills/Combat/Wearable.cs | Basic/skills/Combat/Wearable.cs | 0 | 5 | none |
| `Basic/skills/Crafting/CraftingProcessPlugin.ct` | `CraftingProcessPlugin` | `PluginTemplate` | Basic/skills/Crafting/CraftingProcessPlugin.cs | Basic/skills/Crafting/CraftingProcessPlugin.cs | 1 | 0 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/skills/Discordance/DiscordanceEffectPlugin.ct` | `DiscordanceEffectPlugin` | `PluginTemplate` | Basic/EffectDuration/ActiveEffects.cs<br>Basic/skills/Discordance/Discordance.cs | Basic/skills/Discordance/Discordance.cs | 16 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Hiding/HiddenPlugin.ct` | `HiddenPlugin` | `PluginTemplate` | Basic/skills/Hiding/HiddenPlugin.cs | Basic/skills/Hiding/HiddenPlugin.cs | 0 | 0 | none |
| `Basic/skills/Magery/SpellBook.ct` | `SpellBook` | `ThingTemplate` | Basic/skills/Magery/SpellBook.cs | Basic/skills/Magery/SpellBook.cs | 2 | 1 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/CoreSpells/IncognitoEffectPlugin.ct` | `IncognitoEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/CoreSpells/IncognitoSpellDef.cs | Basic/skills/Magery/Spells/CoreSpells/IncognitoSpellDef.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/CoreSpells/PolymorphEffectPlugin.ct` | `PolymorphEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/CoreSpells/PolymorphSpellDef.cs | Basic/skills/Magery/Spells/CoreSpells/PolymorphSpellDef.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/MagicReflection/MagicReflectionPlugin.ct` | `MagicReflectionPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/MagicReflection/MagicReflectionPlugin.cs | Basic/skills/Magery/Spells/MagicReflection/MagicReflectionPlugin.cs | 0 | 0 | none |
| `Basic/skills/Magery/Spells/ManaShield/ManaShieldPlugin.ct` | `ManaShieldPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/ManaShield/ManaShieldPlugin.cs | Basic/skills/Magery/Spells/ManaShield/ManaShieldPlugin.cs | 0 | 0 | none |
| `Basic/skills/Magery/Spells/Paralyze/ParalyzeEffectPlugin.ct` | `ParalyzeEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/Paralyze/ParalyzeEffectPlugin.cs | Basic/skills/Magery/Spells/Paralyze/ParalyzeEffectPlugin.cs | 0 | 0 | none |
| `Basic/skills/Magery/Spells/PoisonAndCure/DamagingPoisonPlugin.ct` | `DamagingPoisonEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/PoisonAndCure/DamagingPoisonPlugin.cs | Basic/skills/Magery/Spells/PoisonAndCure/DamagingPoisonPlugin.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/ReactiveArmor/ReactiveArmorPlugin.ct` | `ReactiveArmorPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/ReactiveArmor/ReactiveArmorPlugin.cs | Basic/skills/Magery/Spells/ReactiveArmor/ReactiveArmorPlugin.cs | 0 | 0 | none |
| `Basic/skills/Magery/Spells/SpellEffectItem.ct` | `SpellEffectItem` | `ThingTemplate` | Basic/skills/Magery/Spells/SpellEffectItem.cs | Basic/skills/Magery/Spells/SpellEffectItem.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.ct` | `IntModSpellEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.ct` | `StrModSpellEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.ct` | `DexModSpellEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.ct` | `BlessSpellEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.ct` | `CurseSpellEffectPlugin` | `PluginTemplate` | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | Basic/skills/Magery/Spells/StatModSpells/StatModSpellEffectPlugins.cs | 8 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/TeleportingSpells/RecallRune.ct` | `RecallRune` | `ThingTemplate` | Basic/skills/Magery/Spells/TeleportingSpells/RecallRune.cs | Basic/skills/Magery/Spells/TeleportingSpells/RecallRune.cs | 2 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Magery/Spells/WallSpells/WallSpellItem.ct` | `WallSpellItem` | `ThingTemplate` | Basic/skills/Magery/Spells/WallSpells/WallSpellItem.cs | Basic/skills/Magery/Spells/WallSpells/WallSpellItem.cs | 0 | 2 | none |
| `Basic/skills/Magery/SpellScroll.ct` | `SpellScroll` | `ThingTemplate` | Basic/skills/Magery/SpellScroll.cs | Basic/skills/Magery/SpellScroll.cs | 0 | 0 | none |
| `Basic/skills/Poisoning/PoisonedItemPlugin.ct` | `PoisonedItemPlugin` | `PluginTemplate` | Basic/skills/Poisoning/PoisonedItemPlugin.cs | Basic/skills/Poisoning/PoisonedItemPlugin.cs | 4 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Poisoning/PoisonPotion.ct` | `PoisonPotion` | `ThingTemplate` | Basic/skills/Poisoning/PoisonPotion.cs | Basic/skills/Poisoning/PoisonPotion.cs | 0 | 3 | none |
| `Basic/skills/Stealing/SnoopingPlugin.ct` | `SnoopingPlugin` | `PluginTemplate` | Basic/skills/Stealing/Snooping.cs | Basic/skills/Stealing/Snooping.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Basic/skills/Tracking/TrackingPlugin.ct` | `PlayerTrackingPlugin` | `PluginTemplate` | Basic/skills/Tracking/TrackingPlugins.cs | Basic/skills/Tracking/TrackingPlugins.cs | 5 | 0 | extra_instance_fields_not_in_ct_vars, ct_vars_missing_in_class_fields |
| `Basic/skills/Tracking/TrackingPlugin.ct` | `NPCTrackingPlugin` | `PluginTemplate` | Basic/skills/Tracking/TrackingPlugins.cs | Basic/skills/Tracking/TrackingPlugins.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Basic/Towns/NazgulBoard.ct` | `NazgulBoard` | `ThingTemplate` | Basic/Towns/NazgulRings.cs | Basic/Towns/NazgulRings.cs | 0 | 0 | none |
| `Basic/Towns/NazgulGate.ct` | `NazgulGate` | `ThingTemplate` | Basic/Towns/NazgulRings.cs | Basic/Towns/NazgulRings.cs | 0 | 0 | none |
| `Basic/Towns/NazgulShrine.ct` | `NazgulShrine` | `ThingTemplate` | Basic/Towns/NazgulRings.cs | Basic/Towns/NazgulRings.cs | 0 | 0 | none |
| `Basic/Towns/PowerBadge.ct` | `PowerBadge` | `ThingTemplate` | Basic/Towns/RealmOffice.cs | Basic/Towns/RealmOffice.cs | 0 | 0 | none |
| `Basic/Towns/RealmStone.ct` | `RealmStone` | `ThingTemplate` | Basic/Towns/RealmOffice.cs | Basic/Towns/RealmOffice.cs | 0 | 0 | none |
| `Basic/Towns/TaxChest.ct` | `TaxChest` | `ThingTemplate` | Basic/Towns/TownStone.cs | Basic/Towns/TownStone.cs | 0 | 0 | none |
| `Basic/Towns/TownStone.ct` | `TownStone` | `ThingTemplate` | Basic/Towns/TownStone.cs | Basic/Towns/TownStone.cs | 0 | 0 | none |
| `Basic/Travel/TravelStone.ct` | `TravelStone` | `ThingTemplate` | Basic/Travel/TravelStone.cs | Basic/Travel/TravelStone.cs | 0 | 0 | none |
| `Basic/VarGate/VarGate.ct` | `VarGate` | `ThingTemplate` | Basic/VarGate/VarGate.cs | - | 23 | 0 | ct_vars_missing_in_class_fields |
| `Optional/PlayerVendor/playerVendor.ct` | `PlayerVendor` | `ThingTemplate` | Optional/PlayerVendor/PlayerVendor.cs | Optional/PlayerVendor/PlayerVendor.cs | 1 | 0 | ct_vars_missing_in_class_fields |
| `Optional/PlayerVendor/playerVendor.ct` | `PlayerVendorStockEntry` | `ThingTemplate` | Optional/PlayerVendor/PlayerVendorStockEntry.cs | Optional/PlayerVendor/PlayerVendorStockEntry.cs | 3 | 0 | ct_vars_missing_in_class_fields |
| `Optional/StorageBoxes/storageBoxes.ct` | `RegBox` | `ThingTemplate` | Optional/StorageBoxes/RegBox.cs | Optional/StorageBoxes/RegBox.cs | 2 | 1 | ct_vars_missing_in_class_fields |
| `Optional/StorageBoxes/storageBoxes.ct` | `GemBox` | `ThingTemplate` | Optional/StorageBoxes/GemBox.cs | Optional/StorageBoxes/GemBox.cs | 2 | 1 | ct_vars_missing_in_class_fields |
| `Optional/Treasures/treasures.ct` | `TreasureChest` | `ThingTemplate` | Optional/Treasures/Treasures.cs | Optional/Treasures/Treasures.cs | 8 | 0 | ct_vars_missing_in_class_fields |
