# Abilities & talents

A mostly-built mechanism with deliberately incomplete content and no real profession
integration. Covered here as its own page because [Professions](professions.md) and
[Leveling & experience](leveling-experience.md) both touch it but explicitly treat it as
out of scope for their own passes - this page documents what exists, not what's missing,
since what's missing is already tracked as a standing deferral.

## The data model

`AbilityDef` (`scripts/Basic/AbilitiesAndTalents/AbilityDef.cs`) is an
`AbstractIndexedDef` (see
[The Thing model](../02-engine-core/thing-model.md)/
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md) for the
general Def pattern) with `chance`, `cooldown`, `resources`/`requirements`
(`ResourcesList`s - the same ingredient/consumption-list mechanism crafting uses),
`effectPower`, `effectDuration`. Three concrete subclasses exist:

- `ImmediateAbilityDef` - fires once, no ongoing state.
- `ActivableAbilityDef` - has explicit start/stop semantics (a toggled or timed effect).
- `PassiveAbilityDef` - always active once points are allocated, no activation step.

`Ability` (`scripts/Basic/AbilitiesAndTalents/Ability.cs`) is the per-character instance -
`RealPoints` (the persisted value) and `ModifiedPoints` (real points plus a temporary
`sbyte modification`, for equipped-item/buff-style adjustments, never allowed to go
negative). Changing either fires `AbilityDef.Trigger_ValueChanged`. `Character.
ModifyAbilityPoints`/`GetAbility` are the read/write API other systems use.

## Real, working abilities and talents exist

Several concrete abilities are fully implemented under `AbilitiesAndTalents/Abilities/`
and `AbilitiesAndTalents/Talents/` - Bleeding, Warcry, LongRange, ManaSaving,
SpellPowerBonuses, CoolDownSpeedup, WhirlWind, Bloodritual, ClearCast, SkillMasteries,
SpellManaDiscounts, Ignition, SkillDelaySpeedup, EnergyShield, WeaponSkillDmgBonus,
regeneration variants (Hits/Stam/Mana) - each its own small folder with a `*Def`/`*Plugin`
pair (plugins following the generic per-trigger-group codegen pattern, see
[Scripting & code generation pipeline](../02-engine-core/scripting-pipeline.md)). These
are real, working mechanics, not stubs - they're just not wired into any profession's
talent tree yet (see below).

## Talent trees: structure exists, content is half-built

`TalentTreeBranchDef` (`scripts/Basic/Professions/TalentTreeBranchDef.cs`) models one
branch as a set of `TalentTreeEntry` leaves - each leaf an `AbilityDef` plus its tier,
position-within-tier, max points, and a `ResourcesList` dependency (what must already be
unlocked before this leaf is available). `ProfessionDef` has 3 branch slots
(`TTB1`/`TTB2`/`TTB3`).

This is a real, working data structure - but **no profession's `.def` file populates any
of the 3 branch slots today**. Only Mage and Thief had any ability-related content before
the Professions pass, and Thief's own talent trees are roughly half real shared abilities
and half Czech design notes with a point cost decided but no `AbilityDef` ever created
(Backstab bonuses, Blind, Disarm, Taunt, several named strike-finishers).

## Why this stays out of scope

**Explicit, repeated user decision, both during the Professions pass (#134) and the
Races pass (#161)**: abilities/talent-trees are out of scope for all 9 professions,
including not touching Mage's/Thief's existing half-finished trees. The user indicated a
possible future proper ability-tree *redesign*, and didn't want new work built on top of
the current half-done shape in the meantime.

Concrete consequences of this standing deferral, visible elsewhere in these docs:
- [Leveling & experience](leveling-experience.md)'s `TrySpendAbilityPoints` has no
  profession-based gating at all - it just spends the existing points pool, with no
  allowlist check the way skill-spending has one.
- [Professions](professions.md)'s `ProfessionDef.GetAbilityEntry`/`AllAbilitiesSorted`
  machinery (display order, max points per profession) is real and reachable, but with
  nothing populating it for 8 of 9 professions.
- No ability-selection UI exists in the [Races & nations](races-and-nations.md) creation
  flow - profession/race/nation/stats are the only steps.

## What a future pass would need to decide

Not decided, not started - listed here only so the open questions aren't lost:
- Whether to keep the existing tier/branch/dependency model or redesign it.
- Whether to finish Thief's half-built tree (several named abilities have a point cost
  decided in the source but no `AbilityDef`) or start over.
- How ability-point spending should validate against a profession once gating exists -
  presumably the same `TrySpendAttributePoints`-style "requires profession, requires
  within budget, requires within a per-item cap" shape already established for attributes.
