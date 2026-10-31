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

## Where the numbers come from (#860)

Erebor set most combat values at runtime from the `nastaveni_*` variables in
`save/sphereworld.scp`, overriding its item definitions. The converter reads them
(`SphereWorldVars`), so weapons, armor and ammo in `scripts/converted` carry Erebor's real
values, marked with the variable's name in a comment:

| Item | Field | Variable |
|---|---|---|
| Weapon | `attackVsP/VsM` | `nastaveni_weapdam_<defname>` |
| Weapon | `speed`, `piercing` | `nastaveni_weapspeed_<model>`, `nastaveni_weappiercing_<model>` (shared by all materials) |
| Staff | `MindPowerVsP/VsM` | `nastaveni_staffSMbonus_<defname>` |
| Armor set piece | `armorVsP/VsM` | `nastaveni_armorarmor_<metal>_<type>`, shields get a sixth, a plate set's shield then `nastaveni_global_plateshield_armor_decrease` |
| Armor set piece | `mindDefenseVsP/VsM` | `nastaveni_armorparts_OM_<defname>` |
| Armor set piece | `WEIGHT` | `nastaveni_armorweight_<metal>_<type>`, a shield's a sixth, by the layer's share (`layerar[N]`, spheredefs.scp) |
| Profession | `PvPArmorChange` | `nastaveni_global_armorchange_vP_<profession>`, hand-kept in the `.def`s |
| Ammo | `DamageMultiplier`, `Piercing` | `nastaveni_arrowdamage_<tdata1>` (permille), `nastaveni_arrowdamage_<tdata2>` |

Bows and crossbows use these like any other weapon; `RangerSettings`' damage, speed and
tier bonus fields are unused. Bare hands (attack 5, speed 200) are on `.settings`. Spell
effects are hand-kept in the spell `.def`s from `nastaveni_spelleffect_*`, with the third number
Erebor gives some of them as `ArmorPiercing`.

On Erebor the adamantinum and mithril jagged bolts really have a multiplier of 0, so they do no
damage.

## Stat aggregation: `CombatCalculator`

`CombatCalculator.CalculateCombatArmorValues` follows Sphere's `CalcArmorDefense`, for players
and NPCs alike: a character's armor is its def's `ArmorVsP`/`ArmorVsM` plus what it wears. Each
worn `Wearable` covers body parts by layer, the best piece per part counts, and the parts weigh
head 14%, neck 6%, back 18%, chest 18%, arms 14%, hands 7%, legs 17%, feet 6%. So plate on the
chest layer alone counts 36%. A shield (`LayerNames.Hand2`) adds its armor scaled by the
wearer's `Parry` instead. Mind defense from armor counts on players only.

`CalculateCombatWeaponValues` (line 368) resolves the equipped weapon (hand1, falling back
to hand2) into a `CombatWeaponValues` struct - range, strike-start/stop range, piercing,
projectile type/anim, and the swing `delay` (see [Swing formulas](#swing-formulas-860)). For
NPCs it reads `NPCDef`'s own flat fields instead (`WeaponRange`/`WeaponAttack`/etc) rather
than deriving from worn items.

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
3. `GetResistModifier` applies the character's resists, Erebor's `handleresists`. Resists are
   permille (150 = 15%, clamped to -40000..1000). The physical subtypes of the damage
   (blunt, sharp, slashing, stabbing, bleed, archery) are averaged, then `ResistPhysical`
   multiplies them; the magic ones likewise with `ResistMagic`. Damage with both kinds
   averages the two, and summon damage is multiplied by `ResistSummon`. Damage with no
   subtype at all isn't reduced. On a swing the result is rounded, and the
   [minimum damage](#swing-formulas-860) rules apply.
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

## Swing formulas (#860)

These mirror Erebor's `combat.scp`, integer rounding included (`RoundLikeSphere`). Pure parts
are in `CombatCalculator` (`CalculateDamageCust`, `CalculateDamageMvP`, `CalculateSwingDelay`)
and are tested in `Tests/EreborDamageFormulaTests.cs`.

**Attack.** A player's is `weapon attack * (tactics, anatomy, armslore and Str parts) * ammo
multiplier`, then `* weapAttack` (1.1), then `* weapAttackPvP` (0.9) vs a player or
`* (weapAttackVsM + min(level, 200) * 0.01)` vs a monster. A monster's is the def's
`WeaponAttack * weapAttackM`, with no stats and whatever weapon it holds, `* weapAttackVsM`
vs a monster.

**Armor.** Against a player's swing, a player's armor first changes by their profession's
`PvPArmorChange`, floored at 0. A player's swing, or a monster's on a monster (`damagecust`):
`armor * armorClassP/M * roll(0.945-1.05) * (1 - piercing/1000)` is subtracted from the attack,
and the rest is multiplied by `swingDamagePvP` (0.9) or `swingDamageVsM` (1.35). A monster
hitting a player (`damagecust_MvP`) keeps a percentage instead:
`attack * (1 - armor * (1 - piercing/1000) / armorClassMvP) * roll(0.85-1.0)`, so armor 100
halves the hit and armor 200 stops it.

**Dodge.** Each point of `a_weapon_dodge` dodges 1% of monsters' physical blows
(`weaponDodgePerPoint`). A dodged blow does nothing and fires no on-hit abilities. Players'
blows can't be dodged this way.

**Interrupting a cast.** Only a player's melee blow does: 5 damage on a monster, a tenth of the
target's hit points on a player. Ammo and monsters' blows don't. Other damage still disrupts.

**Hit roll.** Sphere's `Calc_CombatChanceToHit`: `difficulty = rand(limit)`, where
`limit = (skill - (attack - defend)/5) / 10`, 0 to 100. `attack` is the average of weapon skill and
Tactics; `defend` the average of the target's Tactics and stamina * 10 (* 9 if the target shoots
at a melee attacker). A paralyzed target uses `rand(10)`, difficulty 0 always hits, and otherwise
the skill check runs against `difficulty * 10`. Guards (`AggroMode.GuardsRealm`) always hit, as
`GUARDSINSTANTKILL=1` did.

**Spells.** A mind power spell's power is `mind power * 10`, multiplied by `spellPowerVsM` (2.0) vs
a monster, `spellPowerPvP` between players, then less `mind defense * 10`. Player mind power comes
from Evaluate Intelligence, Spirit Speak and Int by the weapon's `MindPower`; a monster's is its
plain Evaluate Intelligence. Player mind defense is `(Resist/10 + armor's * armorMindDefense) *
mindDefenseGlobal`, a monster's `Resist/10`. Vs monsters, field spells add
`fieldSpellPowerPerLevel` (0.02) per fourth of the level, at most 50, and under Standcast other
spells add it per level up to 200, half for explosion, chain lightning and meteor swarm. Damage is `low + (high - low) * power / 1000`, growing past 1000. Spells with
`ArmorPiercing` go through armor like a swing (`CalculateSpellArmorEffect`, always `damagecust`,
even from a monster); the others ignore armor. A monster's spell on a monster does at least 5.

**Mystic strike.** Power is `mind power * 10`, `* mysticPowerVsM * (spellPowerVsM + level * 0.01)`
vs a monster, less the spell defense, and damage is `power * mysticStrikeEffect / 1000`, as cold
magic. `.switchmagic` turns strikes on monsters to lightning, for 20 stamina; PvP is always cold.

`.astralpower` toggles Erebor's class ability, for 3 stamina and then
`astralPowerStaminaPerSecond` (16) until it's under 5. Each strike rolls `MysticAbilities.
CalculateAbility(statsLevel)` out of 100000. A hit deals up to 13 (cold) or 15 (lightning) instead
of 16, and then a cold strike freezes for a second (always on monsters, 30% on players, not
through magic reflection), and a lightning one can add a lightning spell of power 2000 + mind
power * 8-16. Crushing Blow (`a_mystik_doubleswing`) vs monsters: `10% + 0.5% per point` to add
75% power, for 30 stamina.

**Reactive armor.** On a physical blow, after resists and minimum damage, the spell absorbs
`reactiveArmorAbsorbPvM/PvP` (20/10%) of it and sends `reactiveArmorReturnPvM/PvP` (10/10%) back to
the attacker. A Mystic's strike ends their mana shield and costs half of Int in mana.

**Not ported.** Energy shield's strength and voodoo shield: `i_energyshield`, `shield_power` and
`i_voodooshield` aren't in the archive. A bare-handed player's spell keeps
its damage: Erebor's bare hands rule (`wrestling/200 + 1`) also runs on spells, but it would
cap spells of anyone with free hands, so it applies to swings only here. Item wear: `Durability` scales armor and weapons as on Erebor, but nothing lowers
it, because there's no repair skill yet (#865). Thief rebound and the magic items' reflex and
block have no ability or item here yet (#866).

**Minimum damage**, after resists, on swings, not against a target immune to the damage type:
a monster's swing on a monster that would do less than 1 does 5, unless it's summon damage. A
player's bare hands do 1 when the result is 0, otherwise exactly `wrestling/200 + 1`, however
high the result was. Mystics are exempt.

**Damage type.** A player's weapon: swords and axes slash, fencing weapons are sharp, maces and
bare hands blunt, bows and crossbows stab; a war fork stabs, a scimitar and an executioner's axe
slash. A monster's own `NaturalDamageType` (`tag.weapflag`) wins over a held weapon; without one,
its swords and axes are sharp and slashing, its fencing weapons sharp and stabbing.

**Speed.** `hitspeed = 0xfffff / (root * speed)` server ticks, and a swing takes
`2 * hitspeed` because Erebor's scripts set it on both halves (`@Start` and `@HitTry`).
`swingTicksPerSecond` is 9.14, Erebor's Windows rate. Root and speed:

| Who | Root | Speed |
|---|---|---|
| Player | `sqrt_int(Dex) - 2` | weapon's, bare hands 200 |
| Mystic | `sqrt_int(mana / 2)` | `mysticWeaponSpeedModifier` (60) |
| NPC | `sqrt_int(NpcSpeed / 2)`, or max stamina without one | `weaponSpeedNPC` * 1000 (150) |

`sqrt_int` is Erebor's own function and returns about 100 times the root. Speed 0 falls back
to 50. A Mystic's mana counts as at least 2 here: at 0 Erebor would give a swing of over an hour. `NPCDef.WeaponDelay` isn't used.

## Settings

`CombatSettings` (`CombatSettings.cs:24`) is the GM-tunable category backing all the
multipliers referenced above (`weapAttack`, `weapAttackPvP`, `armorClassP`/`armorClassM`,
`swingDamagePvP`/`swingDamageM`, `attackStrModifier`, bare-hands fallback stats, and the mass
per-weapon-model settings for WeaponType/Speed/Range/Material/etc - these back the
`.settings` dialog's per-item-model editing grids, not simple scalar fields). See
[settings-dialog.md](../03-admin-tooling/settings-dialog.md) for how a new field like this
gets added/exposed.
