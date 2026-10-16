# Combat

Melee/ranged combat lives in `scripts/Basic/skills/Combat/`. The core pieces are
`WeaponSkillDef` (the skill-sequence state machine driving an attack), `CombatCalculator`
(armor/weapon stat aggregation), `DamageManager` (the actual damage pipeline and triggers),
and `CombatSettings` (the GM-tunable `.settings` category, "Combat" - see
[settings-dialog.md](../03-admin-tooling/settings-dialog.md)).

## The attack loop: `WeaponSkillDef`

`WeaponSkillDef` (`scripts/Basic/skills/Combat/WeaponSkillDef.cs`) is a `SkillDef`
subclass (see [skills-overview.md](skills-overview.md) for the generic
Select/Start/Stroke/Success/Fail machinery it plugs into). It runs its own two-phase state
machine on top of that, stored in `SkillSequenceArgs.Param1`/`Param2`:

- **`On_Start`** (line 56) aborts any skill already running, installs a
  `WeaponSkillTargetTrackerPlugin` on the target (so the target can later detect it's being
  tracked), and immediately re-enters `On_Stroke` via a 0-second `DelayStroke()` in the
  `Drawing` phase.
- **`On_Stroke`** (line 71) is re-entrant - it keeps re-scheduling itself every ~1 second
  (line 158) until either the attacker gives up (target out of
  `WeaponStrikeStopRange`/`CanInteractWith` fails) or gets close enough
  (`WeaponStrikeStartRange`) to flip into the `Striking` phase, which waits out the real
  weapon delay (`Character.WeaponDelay`, fed by `CombatCalculator`) before resolving the
  swing.
- Once in `Striking` phase and the delay has elapsed, it resolves projectiles (an arrow from
  `self.WeaponProjectile`, consumed and optionally dropped on the ground/in the target's pack
  per a 3-way dice roll, line 111), rolls `CheckSuccess` (inherited from `SkillDef`), and
  either continues to `On_Success` (`DamageManager.ProcessSwing`, line 181) or `On_Fail`
  (a miss sound + `Trigger_HostileAction`, line 184).

**Re-entrancy guard, a real fix not a cosmetic one** (lines 74-85, 188-192): a killing blow's
own `On_Stroke` re-adds the target via `WeaponSkillTargetQueuePlugin.AddTarget` (line 148)
*before* `On_Success`'s damage actually applies - `AddTarget` restarts the sequence
(`On_Start` -> 0s `DelayStroke`), which can re-enter `On_Stroke` for an already-deleted
target in the same tick, since a non-player `CauseDeath` deletes the character immediately
(see [notoriety.md](notoriety.md) and [corpses-and-death.md](corpses-and-death.md) for that
deletion timing). Both `On_Stroke` and `On_Fail` check `target.IsDeleted` first and bail out
cleanly instead of calling `CanInteractWith`/`CanSeeLOS` on a deleted `Thing`, which would
throw.

## Stat aggregation: `CombatCalculator`

`CombatCalculator.CalculateCombatArmorValues` (`CombatCalculator.cs:25`) only computes real
worn-armor totals **for players** (`self.IsPlayerForCombat`, line 39) - NPCs get their flat
`ArmorVsP`/`ArmorVsM` straight from `DefForCombat` with no per-layer aggregation. For
players, it walks `VisibleEquip`, buckets each `Wearable` by `LayerNames` into body-zone
accumulators (head/neck/back/chest/arms/hands/legs/feet - lines 40-83), then combines them
with fixed per-zone weights (head 10%, neck 5%, back 10%, chest 30%, arms 10%, hands 10%,
legs 20%, feet 5% - lines 221-271) into one `armorVsP`/`armorVsM`/`mindDefenseVsP`/
`mindDefenseVsM` total. A shield (`LayerNames.Hand2`) is handled separately (line 209) -
its `ArmorVsP` is scaled by the wearer's own `Parry` skill value rather than contributing to
the weighted-zone sum.

`CalculateCombatWeaponValues` (line 368) resolves the equipped weapon (hand1, falling back
to hand2) into a `CombatWeaponValues` struct - range, strike-start/stop range, piercing,
projectile type/anim, and the swing `delay` (`TimeSpan.FromSeconds((0xfffff / 1000.0) /
delay)`, line 418, a direct carry-over from the original Sphere-derived engine, flagged in
the code itself as "dedictvi z morie, funguje to tak proc to menit" - "inherited from Moria,
it works, why change it"). For NPCs it reads `NPCDef`'s own flat fields instead
(`WeaponRange`/`WeaponDelay`/`WeaponAttack`/etc, line 435) rather than deriving from worn
items.

`CalculateArmorClassEffect` (line 479) is the actual to-hit/mitigation formula - see the open
gap below.

## Damage pipeline: `DamageManager`

`DamageManager.CauseDamage` (`DamageManager.cs:83`) is the single choke point every damage
source routes through (weapon swings, spells - see [magery-spells.md](magery-spells.md)).
In order:
1. `Flag_Invulnerable` short-circuits to 0 damage with a generic "attack bounced off" visual
   effect (line 84-91, reusing `MagicReflectionPlugin`'s own eye-candy rather than inventing
   a new one).
2. A hard backstop (lines 93-101) blocks a creature from ever damaging its own current
   owner/commander, regardless of how the attack got triggered - target selection
   (`NPC.ShouldAggroOn`/`NPCAiTimer`) already prevents this in practice, but this is the one
   place everything funnels through, so it's also checked here as defense in depth.
3. `GetResistModifier` (line 28) applies the character's resist stats - a two-phase model:
   phase 1 is magic-vs-physical (`ResistMagic`/`ResistPhysical`), phase 2 is the damage's
   finer-grained subtype (fire/electric/acid/cold/poison/mystical/slashing/stabbing/blunt/
   archery/bleed/summon/dragon). Each phase's modifier scales by how many subtype bits are
   actually set, then subtracts the matching resist value(s) - `Sanity.IfTrueThrow` guards
   against a `DamageType` with zero bits set in either phase (malformed content, not a
   runtime possibility with correct `.def` data).
4. `Trigger_Damage` fires `@causeDamage`/`@damage` (attacker/defender respectively) - this is
   the generic deny/observe point other systems hook (see
   [triggers-and-deny-chains.md](../02-engine-core/triggers-and-deny-chains.md)).
5. HP is reduced, and if actual damage was dealt, a hurt sound/anim plays (or nothing, if the
   `Hits` setter's own synchronous `CauseDeath` call already deleted a non-player defender -
   see the `IsDeleted` guard at line 132, same ordering gotcha as the re-entrancy guard
   above) and an overhead `"-N"` message is shown (line 139) - this is the ONLY damage
   feedback that exists; there's no dedicated UO-style floating damage-number packet, since
   this shard's client era doesn't confirm supporting one.

`ProcessSwing` (line 149) is what `WeaponSkillDef.On_Success` actually calls - plays the
attack sound and a swing animation picked by weapon type (`GetSwingAnim`, line 167; this
animation call didn't exist before a documented fix - previously an attacker only made a
sound, never visibly swung), then runs the deny-able `@beforeSwing`/`@beforeGetSwing`
trigger pair before calling `CauseDamage` with the pre-computed `DamageAfterAC`.

## Fixed bug: NPC spellcasting ignored walls made of real placed items

User-reported: an NPC spellcaster walled in with `i_wall_stone` could still cast at and
damage a target outside the walls. `NPCAiTimer.TryCastSpell`'s own `self.CanInteractWith(
target).Allow` check (`NPCAiTimer.cs`) was correct - the bug was one level deeper, in
`Map.IsLosBlockedAt` (`GameCore/Regions/LineOfSightImpl.cs`), which deliberately checked
only terrain and pre-baked map statics, explicitly skipping real dynamically-placed Items
("there's no real placeable-blocking content in the game yet to validate that against" -
no longer true, `i_wall_stone` is real content used elsewhere). Movement already correctly
blocked on a placed wall (`MapCalculations.IsOk`'s own `items` loop) - only sight through
one was broken. Fixed by extending `IsLosBlockedAt` to also check real ground items along
the LOS ray, mirroring the exact `ItemDispidInfo.GetByModel(item.Model).Flags`/
`TileFlag.Impassable` pattern the movement check already used. This affects every caller
of `CanInteractWith`/`CanSeeLOS`, not just spellcasting - any LOS-gated interaction now
correctly respects a real placed wall.

## Open gap: armor mitigation is largely inert (#96)

Tracked as issue #96, **deliberately left open, not fixed** - the user wants armor
mitigation revisited only once armor/material/skill content is more complete, to avoid
redoing the formula twice. Two confirmed problems in
`CombatCalculator.CalculateArmorClassEffect` (line 479):

1. The real, correctly-derived `armorClass` argument is **discarded for a random
   placeholder** (`ScriptUtil.GetRandInRange(settings.armorRandEffectMin,
   settings.armorRandEffectMax)`, line 500) in every case except monster-attacks-player
   (`isMvP`, line 486). NPC-vs-NPC, player-vs-NPC, and PvP combat all ignore the defender's
   actual armor value entirely. A commented-out line (`armorClass *=
   settings.armorClassMvP`, line 508-510) and the otherwise-completely-unused
   `CombatSettings.armorClassMvP = 200` field (`CombatSettings.cs:86`) suggest this was
   meant to be finished, not a deliberate design choice.
2. Even in the one case that DOES use the real armor value (MvP), measuring actual swing
   damage with full plate equipped versus bare skin (300 trials via direct
   `DamageManager.GetWeaponSwingArgs` calls) showed under 1% difference, where
   hand-calculating from `CombatSettings`'s own per-zone weights predicts roughly 20-25%.
   This is reproducible and unexplained by problem #1 (MvP already uses the real armor
   value) - an open mystery, not a clean fix, and not chased further.

Don't attempt a quick fix to either of these without raising it with the user first - the
armor/material/skill systems these numbers depend on are explicitly still maturing.

## Settings

`CombatSettings` (`CombatSettings.cs:24`) is the GM-tunable category backing all the
multipliers referenced above (`weapAttack`, `weapAttackPvP`, `armorClassP`/`armorClassM`,
`swingDamagePvP`/`swingDamageM`, `attackStrModifier`, bare-hands fallback stats, and the mass
per-weapon-model settings for WeaponType/Speed/Range/Material/etc - these back the
`.settings` dialog's per-item-model editing grids, not simple scalar fields). See
[settings-dialog.md](../03-admin-tooling/settings-dialog.md) for how a new field like this
gets added/exposed.
