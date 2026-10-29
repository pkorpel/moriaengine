# Monster abilities (teventStubs)

Erebor's monsters get their special mechanics from Sphere `TEVENTS=e_*` event scripts. This
engine never runs those scripts. Instead, `NPC.TryResolveUnknownTriggerGroupName` grants a
**passive ability** `a_tevent_<name>` for each `TEVENTS` line
(`scripts/Basic/AbilitiesAndTalents/teventStubs.scp`). An ability with no implementation yet
is a zero-effect `PassiveAbilityDef` stub: it shows up in `.info`'s Abilities view and does
nothing. A real one is a subclass with its own tunables and a `MonsterSpecialAttacks.TryApply*`
consumer, one file per ability under `scripts/Basic/AbilitiesAndTalents/MonsterSpecialAttacks/`.

## Tuning: three layers

1. **Engine-wide default**: the ability def's own `FieldValue`s, editable in `.info` (Abilities ->
   Def info) or on `.settings` -> "Monster Abilities" (`MonsterAbilitiesSettings`,
   `<Ability><Property>`, a pass-through to the same values). Every tunable must be on that page;
   the fast-tier `MonsterAbilitiesSettingsCoverageTests` enforces it.
2. **Per species**: an `NPCDef` override (`{min max}` range, e.g. `ExplodeDamage = 40`), filled
   in by the converter from the archive's `tag.*` lines in `ON=@Create` (the table in
   `ConvertedCharDef.abilityTuningFieldNames`).
3. Chances are permille (0-1000), cooldowns are real seconds, unless a row says otherwise.

**FieldValue key gotcha (#741):** a subclass must not reuse a base `AbilityDef` key such as
`"chance"`/`"cooldown"`. The duplicate silently aborts the constructor at boot. Every key is
prefixed (`stunChance`, `npcCurseManaChance`...).

## Where they hook in

| Hook | Abilities |
|---|---|
| `DamageManager.ProcessSwing`, before `CauseDamage` | summon eater (+ vamp) - swing ends, critic, sniper, magic shield and eye deflect (defender) |
| `ProcessSwing`, after a landed hit, attacker side | stun, knockback, bleed, life leech, phoenix fire, eye drain, backstab ambush |
| `ProcessSwing`, after a landed hit, defender side (only if it survived) | reactive armor, stamina drain, mana gain, phoenix rebirth, lightnings, push all, auto reflect, strangler-vine hit count, backstab-hide interrupt, multishot, dispel bomb; and unless reactive armor killed the attacker: craft monster, eye attention, golem retarget, mass leap, duplicator |
| `DamageManager.CauseDamage` | explode records who hurt it; `DamageType.Irresistable` skips resists (Sphere's `dam_god`) |
| `NPCAiTimer.Fight` (once per think-tick while fighting) | roundattack/pull, detect, paralyzing scream, the three curses, summoner, infinite mana, leaper, warcry |
| `NPCAiTimer.OnTimeout` (every tick, any state, `MonsterSpecialAttacks.OnAiTick`) | dragon regeneration, backstab hiding, mass leap hops, dispel bomb fuses |
| `Character.CauseDeath` (position still valid) | self-res, explode |
| `Character.On_StepTaken` | hazard tiles, inexhaustible |
| `Corpse.On_Step` / `NPC.On_PersonalSpace` | looter / strangler vine |

## Hazard tiles (#797)

`HazardTiles.cs` stands in for the archive's `i_fire_column_damage`/`i_lightning_damage`. These
were items lasting 2 s that hurt the first player or pet (summons and mounts included) to step
on them, then went out. No item is created here. The server keeps a tile registry and the
client gets a stationary fire-column or lightning effect. **User decision:** a tile also hits
whoever is already standing on it when it appears (the archive was step-on only).

## The abilities

| Ability (`e_*`) | Monsters | What it does | Per-species fields |
|---|---|---|---|
| npc_stun | 169 | landed hit may paralyze, own cooldown | StunChance/Duration/Cooldown |
| npc_critic | 115 | bonus damage vs players | CriticChance/Damage |
| npc_roundattack(_pullall) | | periodic AoE pulse (pull variant drags targets in) | Roundattack*, PullDistance |
| npc_bleed | 119 | stacking bleed DoT, damage/3 power | BleedChance |
| npc_knockback | 40 | pushes the defender up to 3 tiles, walls stop it | KnockbackChance |
| selfreser | 78 | the monster respawns at its corpse after a delay | SelfresDelay |
| npc_detect | 60 | reveals hidden/invisible characters nearby | |
| npc_leaper | 93 | extra pathfinding steps per tick | |
| looter | 146 | takes one item per step from a corpse it walks over | |
| summoneater | 177 | a landed hit deletes a summon | |
| parakrik | 105 | "Paralyzing Scream" - paralyzes nearby players | ParaKrikChance/Cooldown |
| npc_curse | 71 | "Dark Curse" - area life drain | NpcCurseChance/Cooldown/Damage |
| npc_curse_mana / _stam | 15 / 6 | same, drains mana / stamina | NpcCurseMana*/NpcCurseStam* |
| npc_summoning | 41 | spawns its configured reinforcements | Summoning*, AddSummon |
| npc_sniper | 20 | bonus ranged damage vs players | SniperChance/Damage |
| npc_infiniteMana | 54 | mana always full | |
| ovinovac | | strangler vine - holds whoever pushes past it | |
| phoenix_fire | 51 | 1 in 15 hits: 10 fire tiles within 5 (default 20 dmg) | PhoenixFireDamage |
| phoenix | 30 | under 1/5 hits and maxhits > 250: hits = maxhits = maxhits/2, fire ring; repeats; a cursed one fails 1 in 3 | |
| explode | 28 | on death, everyone who hurt it takes damage/distance (full on its tile), LOS, irresistible | ExplodeDamage |
| dragon_regeneration | 25 | +hp every interval s while hurt, in any AI state | RegenerationHp/Interval |
| lifeleech | 20 | heals per landed hit; unset = {maxhits/300+20, maxhits/200+35} | LifeLeechAmount |
| npc_reactivearmor | 19 | reflects % (default 20) of a landed hit to an attacker within 2; Mystics immune | ReactiveArmorPercent |
| npc_stam_dmg | 19 | 20% to drain the attacker's stamina | StamDmgChance/Power |
| lightnings | 19 | 5% when hit: "+" or "x" of lightning tiles at a random spot within 5 | LightningsChance/Range |
| summoneater_vamp | 18 | summon eater that also absorbs the summon's stats (cap 30000) | |
| managain | 17 | +25% maxmana per 5% maxhits lost | |
| inexhaustible | 13 | +3 stamina per step | |
| pushall | 11 | 5% when hit, 30 s cooldown: players within 6 thrown away by (6 - distance) | PushallChance/Cooldown |
| npc_magicshield | 11 | absorbs 90% of each swing; a Mystic pierces it by level/200. The archive's "silenced" bypass has nothing to hook (no silence effect yet) | MagicShieldStrength (percent) |
| autoreflect | 10 | magic reflection re-armed after every swing taken | |
| explosiondispell | 36 | 5% when hit, 30 s cooldown: throws a dispel bomb within 5; 3 s later everything within 5 is dispelled (summons vanish, dispellable effects end), thrower included | DispelBombChance/Cooldown/Range |
| npc_multishot | 29 | 11% when hit, 3 s cooldown: a split shot at every player/pet within 8 in LOS except its target, {40,80} physical, 5x to pets (shares `AreaCurseAbilityDef`) | MultishotChance/Cooldown/Damage |
| eye | 29 | its hits sap {1,5} stamina and, while a player target has mana, leech 2x the damage as hits (cap 3x maxhits) for 4 of the player's mana; 15% of physical blows bounce off; switches to an attacker whose int beats {100,1250} | |
| golem | 20 | turns on whoever hit it: 35/30/25/20/10% above 75/50/25/10/below 10% hits | |
| backstub | 20 | hides a few s after noticing a player; its first hit from hiding adds (maxhits - damage) x 3/5 and reveals it, re-hides 8 s later; a hit taken while hiding interrupts for 10 s. **User decision:** faithful, the 3/5 factor is on `.settings` | BackstabDamageReducer (permille) |
| npc_massleap | 13 | 1 in 14 player hits: marks every non-GM player within 12, then teleports onto each in turn (first after 3 s, then every {3,4} s) and attacks them, skipping anyone out of sight. **User decision:** faithful teleports | |
| duplicator | 13 | splits into a copy (attacks the same target) each time it is hit, up to DupeCount copies; a copy never splits. Unset = 0 = never | DupeCount |
| craft_monster | 13 | Craftsman-profession attackers do +{10,20}; with a weapon they crafted themselves, 1 in 4 is a +{15,25} critical | |
| npc_warcry | 11 | at most once a minute while fighting: everyone within 8 is shocked and their spellcasting interrupted. The archive's range is a runtime setting, 8 is ours | WarcryCooldown (deciseconds) |
| item_refuser | 119 | an item dropped on it is turned down, even from a GM: it says its message (`{0}` = the item's name; the archive's Czech "Hmm, <item> nepotrebuju...") and the item goes back to the giver's pack. Hook: `NPC.On_PutItemOn` (#807); a cancelled `TryPutItemOnChar` now returns a deny so the drop bounces back instead of sticking in the hand | ItemRefuserMessage |
| healer | 18 | a ghost that single-clicks the healer from at most 4 tiles (the archive's `distance<5`) is resurrected - `s_resurrection`'s effect at full power via `SpellDef.ApplyEffectChar` (Sphere's SPELLEFFECT, no region rules); a realm outcast is told they're cursed by the gods instead and the name click is swallowed. This, not the "res" speech, is how Erebor's ghosts got resurrected - see [NPC speech](npc-speech.md). Hook: `Character.On_Click` (#827) | HealerClickRange |

### Area curse rules (#797)

All three curses share `AreaCurseAbilityDef` (`NpcCurse.cs`): a chance roll once the recharge
is up, then every player/pet within range and in line of sight. Following the archive, they
**spare the character the monster is currently fighting** (its `NPCAiTimer.Target`) and do **5x
to pets**. Both are switchable per curse on `.settings` (user request): `NpcCurseSpareCurrentTarget`,
`NpcCursePetMultiplier`, and the same for `NpcCurseMana*` and `NpcCurseStam*`. Until #797, Dark
Curse hit everyone, including the current target, at 1x.

## Not abilities / not done

- `e_character`/`e_monster` (3,147 grants) are Sphere's base player/monster event hooks, not
  special attacks. So are the empty monster-type tags (`e_undead`, `e_daemon`, `e_spider`...).
- Roughly 77 rare boss-specific hooks remain stubs.

**Crafted-by record (#799):** craft monster needs to know who made a weapon, so
`CraftingSkillDef` now tags every crafted item with its crafter (`SetCrafter`/`GetCrafter`,
Erebor's `tag.craftedby`). Items crafted before #799 have no record.

Tests: `Tests/Protocol/MonsterSpecialAttacksTests`, `MonsterAbilitiesBatchTests` (2),
`MonsterAbilitiesBatch3Tests`, `MonsterAbilitiesBatch4Tests`, `MonsterAbilitiesBatch5Tests`, `ItemRefuserTests`, `HealerTests`, `NpcBleedTests`;
fast tier `MonsterAbilitiesSettingsCoverageTests`, `MagicShieldFormulaTests`, `KnockbackPushDistanceTests`,
`BackstabFormulaTests`.
