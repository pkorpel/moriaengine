# New moriaengine trigger catalog

This catalog covers extension events added in this fork, separately from the
[legacy SteamEngine families](trigger-catalog.md). “New” describes introduction into the
fork, not whether a name existed in Sphere. The contracts below describe current behaviour.
Scripts can define additional local events.

[Trigger architecture](trigger-system-architecture.md) explains dispatch and argument mapping.
Regular events do not stop on cancel returns; cancellable events have caller-defined semantics.

## Movement and pickup

| Event | Receiver / payload | Contract |
|---|---|---|
| `@denyMove` | Character; no payload | Cancellable, before calculating the step. Cancel prevents movement; no `DenyResult`. |
| `@stepTaken` | Moving character; `ScriptArgs(running)` | Regular, before `On_StepTaken`; actual steps, not teleports, and not GMs. |
| `@pickingUp` | Picker; `ScriptArgs(item, amountToPick)` | Regular, after permission checks, before placement changes, beside `On_PickingUp`. |

Use `@denyMove` for an early immobilisation gate. Legacy `@step` comes after collision/shove
work and is not equivalent. Revalidate participants after handlers; pickup checks deletion again.

Sources: [Character](../../../scripts/Basic/CharClasses/Character.cs),
[AbstractCharacter](../../../GameCore/AbstractCharacter.cs),
[ThingManipulationTriggers](../../../GameCore/ThingManipulationTriggers.cs).

## Weapon swings

| Event | Receiver / payload | Contract |
|---|---|---|
| `@hitTry` | Attacking player; skill sequence's `ScriptArgs` | Regular while drawing a swing; excludes GMs. |
| `@swingLanding` | Attacker; `WeaponSwingArgs` | Regular after armour/monster modifiers, before dodge; mutable `DamageAfterAC`. |
| `@getSwingLanding` | Defender; same payload | Regular after attacker landing work, before dodge. |
| `@hit` | Attacker; `WeaponSwingArgs` | Cancellable after dodge/swing modifiers, before `CauseDamage`. |
| `@getHit` | Defender; same payload | Cancellable, reached only if attacker-side hit handling continues. |
| `@afterHit` | Attacker; `ScriptArgs(swingArgs, damage)` | Regular after damage and stun/knockback/bleeding/leech, before monster after-swing reactions. |
| `@afterGetHit` | Defender; same payload | Regular, follows attacker-side after-hit processing for a non-deleted receiver. |

Hit handlers can modify `DamageAfterAC` or cancel. Cancellation takes the dodged-blow path:
no hit damage, but legacy `@afterSwing` and relevant dodge reactions still run. These are
swing events, including mystic strike, not generic spell/ability damage events.

Landing events also see blows later dodged. After-hit events exclude dodged/cancelled blows;
the normal path is also skipped if damage already deleted the defender, as on an NPC killing
blow. Zero damage can still reach them. Count kills using death/credit events.

Legacy `@causeDamage` / `@damage` use `DamageArgs`, now distinguishing swing, spell and custom
damage. None of these APIs promises to establish `SRC`.

Sources: [DamageManager](../../../scripts/Basic/skills/Combat/DamageManager.cs),
[WeaponSkillDef](../../../scripts/Basic/skills/Combat/WeaponSkillDef.cs),
[combat](../04-gameplay-systems/combat.md).

## Casting and spell effects

| Event | Receiver / payload | Contract |
|---|---|---|
| `@spellCastTry` | Caster; skill sequence's `ScriptArgs` | Regular after resource availability checks, before casting rules. |
| `@denySpellCast` | Caster; same form | Cancellable after casting rules; cancel stops casting. |
| `@spellCast` | Caster; same form | Regular after the spell-start gate, when casting is under way. |
| `@monsterSpellPower` | Caster; `ScriptArgs(MonsterSpellPowerArgs)` | Regular; mutable `LevelBonus` in player-versus-monster power calculation. |
| `@spellPower` | Caster; `ScriptArgs(SpellPowerArgs)` | Regular; mutable power after inline pre-defense modifiers, before defense. |
| `@beforeDoEffect` | Caster; `ScriptArgs(SpellPowerArgs)` | Regular; mutable power after target defense/resist. |
| `@beforeGetEffect` | Target; `SpellEffectArgs.scriptArgs` | Cancellable before normal effect application; cancel prevents it landing there. |
| `@spellSuccess` | Caster; `ScriptArgs(SpellSuccessArgs)` | Regular after successful Magery effect/after-cast work if caster still exists. |

Book/ordinary-scroll Magery uses the skill-start sequence. Custom scrolls raise `@spellCast`
through their own path; NPC casting does not use the same sequence. Do not expect every
spell-like operation to emit all these events.

Area power is evaluated for filtered targets. `Target` and `MainTarget` help distinguish
per-target work, but once-per-cast effects must handle multi-target/ground paths explicitly.
Reflection can bypass pre-reflection power-side effects and recompute from the reflected
source. Keep formula stages explicit when their order matters.

Sources: [Magery](../../../scripts/Basic/skills/Magery/Magery.cs),
[SpellDef](../../../scripts/Basic/skills/Magery/Spells/SpellDef.cs),
[magery guide](../04-gameplay-systems/magery-spells.md).

## Death and experience

| Event | Receiver / payload | Contract |
|---|---|---|
| `@denyDeath` | Living character; `ScriptArgs(killedBy)` | Cancellable at entry to `CauseDeath`; cancel skips that death and `@death`. |
| `@kill` | Non-deleted killer, other than victim; `ScriptArgs(victim)` | Regular after victim's legacy `@death`. No event for null/self/deleted killer. |
| `@killCredit` | Each credited fighter; `KillCreditArgs(victim, share)` | Regular; credit rather than final blow. Share can be zero. |
| `@gainExperience` | Player; `GainExperienceArgs` | Cancellable in `AddExperience` after inline bonuses; cancel adds nothing; compiled handlers may change `Amount`. |

`BaseAmount` is the pre-bonus input; `Amount` is what will be added, possibly negative.
These typed members are not aliases into `Argv`; changing an LScript slot does not update
`Amount`. `AddExperience` returns early for invalid/deleted players and zero input.

A denied lethal `Hits` setter change retains previous hits. `Character.SetHitsBy` scopes
explicit killer attribution; otherwise the setter falls back to `Globals.SrcCharacter`.
Timers clear that ambient source, so pass the attacker explicitly. A deny-death handler must
not recursively kill its holder, and cancellation cannot undo work preceding `CauseDeath`.

Sources: [Character](../../../scripts/Basic/CharClasses/Character.cs),
[LevelingTriggers](../../../scripts/Basic/Leveling/LevelingTriggers.cs),
[LevelingSystem](../../../scripts/Basic/Leveling/LevelingSystem.cs).

## Maintenance

For each new event record receiver, actual argument wrapper, mutable fields, timing, cancel
meaning and raising method. Keep inherited events in the legacy reference; link interactions
instead of duplicating both catalogs in the plugin guide.
