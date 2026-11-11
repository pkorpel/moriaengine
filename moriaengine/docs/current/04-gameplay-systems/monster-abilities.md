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
| `ProcessSwing`, after a landed hit, defender side (only if it survived) | reactive armor, stamina drain, mana gain, phoenix rebirth, lightnings, push all, auto reflect, backstab-hide interrupt, multishot, dispel bomb; and unless reactive armor killed the attacker: craft monster, eye attention, golem retarget, mass leap, duplicator |
| `DamageManager.CauseDamage` | explode records who hurt it; `DamageType.Irresistable` skips resists (Sphere's `dam_god`) |
| `DamageManager.CauseSpellDamage`, after a caster's damage spell (only if the target survived) | the same `TryReactToHit` reactions as a landed blow, and e_emotedmg (#1022, the user's call: Erebor's @aftergetswing ran for spells too) |
| `NPCAiTimer.Fight` (once per think-tick while fighting) | roundattack/pull, detect, paralyzing scream, the three curses, summoner, infinite mana, leaper, warcry |
| `NPCAiTimer.OnTimeout` (every tick, any state, `MonsterSpecialAttacks.OnAiTick`) | dragon regeneration, backstab hiding, mass leap hops, dispel bomb fuses |
| `Character.CauseDeath` (position still valid) | self-res, explode |
| `Character.On_StepTaken` | hazard tiles, inexhaustible |
| `Corpse.On_Step` / `NPC.On_PersonalSpace` | looter / strangler vine |
| `@getHit` on the vine (#1411, `VineHoldPlugin`) | strangler-vine hit count |

## Hazard tiles (#797)

`HazardTiles.cs` stands in for the archive's `i_fire_column_damage`/`i_lightning_damage`. These
were items lasting 2 s that hurt the first player or pet (summons and mounts included) to step
on them, then went out. No item is created here. The server keeps a tile registry and the
client gets a stationary fire-column or lightning effect. A tile also hits whoever is already
standing on it when it appears. That is faithful to Erebor: Sphere fires an item's `@step` again
about once a second for whoever stands on it. #797 had called it a change from a step-on-only
archive; the #1022 spec checked it in EreborNext (`CChar.Tick` -> `CheckLocation`).

## The abilities

| Ability (`e_*`) | Monsters | What it does | Per-species fields |
|---|---|---|---|
| npc_stun | 169 | landed hit may paralyze, own cooldown | StunChance/Duration/Cooldown |
| npc_critic | 115 | bonus damage vs players | CriticChance/Damage |
| npc_roundattack(_pullall) | | periodic AoE pulse (pull variant drags targets in) | Roundattack*, PullDistance |
| npc_bleed | 119 | adds (damage/3)/2 to the target's bleed, the one Bleeding Strike makes and a bandage stops (#745) | BleedChance |
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

## Hit reactions (#1022)

`HitReactions.cs`, tunables on `.settings`. `e_npc_golddrop`, `e_npc_reagdrop` and `e_npc_diamonddrop` spill a little on a blow. `e_scales` absorbs one blow in three down to 1-4. `e_npc_manashield` raises a 16 s shield one blow in 15, then blows cost mana until one can't be covered. `e_npc_regeneration` mends under half hits, a tenth or `tag.regespeed` every 8 s, 12 times at most. `e_resist_countdown` wears a boss's resists down by the player's kind of attack. `e_phoenix_25` is `PhoenixAbilityDef` with `RebornShare` 750. `e_no_player_attack` leaves players alone until one hits it. `e_noport` needs nothing here.

The 19 stubs with an empty block on Erebor and the 3 with no source stay as labels, the user's call. What's left is on #1022.

## Potions, berserk and more (#1022)

`NpcDrinkers.cs`, `Berserk.cs`, `WeaponKnockout.cs`, `BattleSummoner.cs`, `Acid.cs`. `e_healdrinker` and `e_potiondrinker` heal or drink one potion at a time, the first blow never counts. `e_berserk_potvora` trades resists for speed in three stages. `e_getwpn` knocks a player's weapon into the pack. `e_npc_summoner` calls its own list for 20 s. `e_lifeleech_vampire` can climb past maxhits. `e_acid_damage` splashes players within 4 tiles: dodging it waits for class abilities, and the Strength potion does nothing, as `e_strboost` isn't in the archive.

## Bombs, illusions and shapeshifters (#1022)

`BlowUp.cs`, `Illusionist.cs`, `Shapeshifter.cs`, `SummonFoes.cs`. `e_npc_blow_up` lights a two-stage fuse next to a player and blasts `16 * (9 - distance)` past armor, ten times that to NPCs, leaving no corpse. `e_illusionist` makes five one-hit copies for 30 s, with no wait, as Erebor's never worked. The three `e_menavec` abilities change the look for 20 s, not the stats. `e_npc_summonkiller` doubles blows on summons. `e_summonfrenzy` stops a masterless summon. `e_elem_terror` blasts its fighters as it dies. `e_undeads_ability_life` waits for #1017; `e_necroslave` is under Dying bosses.

## Guards on sight (#1022)

`GuardSpeech.AttacksOnSight`: a guard attacks anyone it doesn't protect (`IsProtectedBy`, Sphere's `ismyguard`), so Gondor's guards go for Gondor's criminals and murderers unless they're citizens of the guard's town. Players of no realm are left alone unless banned, the user's call, and GMs always. Guards shout when they pick a target. A summon that strikes one dies. Ents attack anyone carrying a `t_weapon_sword`, swords and axes alike. Ostranena's guards go with the bosses.

## Dragons (#1022)

`DragonFire.cs` (the fire tiles and their lingering effects), `DragonEvents.cs` (the 8 abilities), `DragonCageAndBlood.cs`.
Erebor's `systems/aldaron.scp` and `systems/dragon_events.scp`. Every tunable is on `.settings`, and each species' own
values come from its archive tags through the converter (`NPCDef.AbilityTuning`).

They react to a landed blow, Erebor's `@aftergetswing`.

**The fire tiles:**
- **Types:** 1 fire, 2 cold, 3 poison, 4 black, 5 white.
- **Hitting:** a tile burns 2 s and hits the first living player on it (never pets, NPCs or GMs). A strong tile does its
  damage, a weak one half, less the player's dragon resist percent. "Draci ohen te nezranil." when the resist is higher.
- **Lingering effect:** a strong tile's victim takes the type's effect for 6 ticks, a weak tile's for 4:
  - fire and poison: damage;
  - cold: a second of paralysis;
  - black: drains stamina;
  - white: drains mana.

  The first tick comes after 1 s, then one every 4 s, each weaker. One effect of a type at a time; a new hit renews its
  count.

**The abilities:**
- `e_fire_dragon`: " se zhluboka nadychuje". The dragon stands still 1 s (a frozen state the AI keeps,
  `NPCAiTimer.Freeze`), then breathes Erebor's cone where it faces: 22 or 25 tiles. Tiles out of its line of sight are
  dropped.
- `e_fire_dragon_bomb`: a cannonball onto its target and 29 tiles of fire there, with no line of sight needed. Fixed:
  the ball flies from the dragon (Erebor's started at the attacker).
- `e_fire_dragon_round`: 2 s later it spins: a turn clockwise and a breath each second, 9 per turn. These tiles skip line
  of sight, resist and lingering effects.
- `e_fire_dragon_cage`: every player in the dragon's 64x64 map cell is caged; a warrior slips out 66% of the time.
  - Fire stands on the 8 tiles around the player for the cage's duration and doesn't burn.
  - Players can't step through it: "Zar je prilis silny, neda se tam projit!". Erebor pushed them back; here the step is
    refused.
  - A double click weakens a tile, at most once a second; the duration/2+1th click puts it out.
- `e_fire_dragon_aura`: every player in the attacker's 64x64 cell gets the aura, or has it renewed.
  - It ticks every 5 s, each tick weaker, with fire damage less the dragon resist percent.
  - It costs stamina too: all of it if less than the tick, else a third.
  - Mages, necromancers and mystics take three quarters of the damage and lose a quarter of it in mana.
  - Fixed: the resist counts once, and only where the aura resist is set (Erebor took it twice).
- `e_bahenny_drak_aura`: the swamp dragon's aura, plain damage with no resist.
- `e_bahenny_drak_krev`: below 85, 65, 45, 25 and 5% of its hits it bleeds 3 puddles within 3 tiles.
  - A second later, each puddle hurts everyone next to it: players 80, pets 800.
  - For 30 minutes, stepping on it (once every 10 s) does 80 fire and poisons a player with dragon-blood poison (40,
    then less every 4 s, 8 times).
  - Fixed: a blow that jumps past a threshold still bleeds; Erebor's then blocked every later stage.
- `e_dragon_brain` (c_dragon_swamp): once it has seen a player within 18 or been hit, it thinks every 4-8 s.
  - Each thought heals 50 hits and picks by weight 50/20/20/10:
    - a round attack: a sound and a ripple. Erebor's swamp dragon had no round-attack damage set, so it hurt nobody;
    - a stamp: it stands still 1 s, then everyone in its map cell takes 120 less 6 a tile (sometimes a piece of the
      ceiling too) and 2 s of paralysis;
    - a poison breath at its target, if within 10;
    - acid on players within 4.
  - Fixed: it stops thinking when no player is near (Erebor's ran forever).

The 64x64 map cell is Erebor's `sector.allchars`, kept as is: a player 2 tiles away across a cell border is spared, one
60 tiles away in the cell isn't.

Not ported:
- `e_q040_dragon_death`: waits for the q040 quest.
- Dragon armor (q040's reward): so dragon resist comes from the potion only.

Nothing to port, the stubs kept as labels:
- `e_aldaron_vamp`: its vampire part is commented out on Erebor;
- `e_berserkbrain_distance_fix`: a Sphere swing-range fix this engine doesn't need;
- `e_miniq_kun`: the rider's horse already goes with it here;
- `e_brinbor`: sets a tag nothing reads.

Test: `DragonEventsTests` (protocol): breath with the species' own damage, resist, bomb, spin, aura tick, cage and its
clicks, blood stages, the brain's stamp.

## Summoning bosses (#1022)

`BossSummoners.cs`. Erebor's `systems/macros.scp`, `systems/npc_ai.scp` and `questy/verbatim.scp`.

**Shared pieces:**
- A batch summon puts each def around a spot for a lifetime. Conjured summons give no loot or experience. Owned ones are
  the boss's own (`ControlMaster`, Erebor's MEMORY_IPET), so they never hurt it.
- Erebor's `fireexplosion` is fire columns on the hazard tiles.
- A death blast hits everyone who fought the boss and is in sight, its damage divided by distance.
- Sphere's `@NPCSeeNewPlayer`: from the AI's tick, a monster that isn't fighting reacts once to each player it sees
  within 18 tiles, and remembers them for 5 minutes. GMs are left out.

**The bosses:**
- `e_fire_lord`, when hit:
  - 1 in 12: "Heru Beleg Ruin Val" and 10 fire columns;
  - 1 in 20: "Heru Ruin Val" and two fire phoenixes and a fire eye for 20 s.

  Dying, it blasts 180.
- `e_strach_lord2`:
  - one swing in 3, a taunt;
  - when hit, the first of: a fire daemon for 150 s (1 in 30), a fire tarantula for 60 s (1 in 30), a Fire Lord for 40 s
    (1 in 100), fire columns (1 in 20).
- `e_serpentdruid`:
  - one swing in 10, three snakes of its own for 30 s;
  - below half its hits, "hissing.", the serpent's look (`NPCDef.SerpentForm`, the archive's `tag.had`, through the
    converter), no more snakes, and gone 2000 s later.

  Changed from Erebor, the user's call: the look only, so the Arcidruid's serpent doesn't start regenerating.
- `e_cajun`, when hit, unless asleep:
  - 1 in 50 its three children around the attacker, and 1 in 50 the third, for 600 s.

  Dying, it blasts 50, says "Ruth Mor" and two children rise. Fixed: those two also vanish after 600 s; Erebor's timer
  landed on another item.
- `e_drone_large`, when hit:
  - 1 in 25 three drones, 1 in 20 one, for 60 s ("Heru Val Drone");
  - then it goes back home.
- `e_wormdrop`:
  - a new player in sight: three worms, and it attacks;
  - dying: six worms;
  - Poison or Poison Field on it: "resist a poison attack", and the spell is cancelled;
  - any other spell on it: two worms.
- `e_sumoner_strazce_spicich`:
  - a new player in sight: a necromancer may pass; a priest is attacked; anyone else brings a sand golem;
  - hit by a warrior: "Valecniku blaznivej...";
  - his blows and spells on him give him a Greater Heal;
  - dying: 10 fire within 2, then gone, with no body, loot or experience.

  Fixed from Erebor:
  - the heal goes to himself (Erebor's went to his target);
  - the warrior line goes to warriors (Erebor's test was turned round);
  - the necromancer's line comes once per sighting.
- `e_ps_fanatik`: "Nyni zemrete!" to each new player it sees, and one swing in 5 a taunt.

Erebor's sleep and silence checks are Peacemaking's sleep and the mage's silence here.

Test: `SummoningBossTests` (protocol):
- fire lord: summons, columns, death blast;
- cajun: children;
- drone: home;
- serpent druid: snakes, form, no snakes after;
- SwARPUS: worms on sight once, poison refused, worms on a heal;
- guardian: golem, vanishing without experience.


## Dying bosses (#1022)

`BossDeaths.cs`. Erebor's `systems/aldaron.scp`, `systems/new_events.scp` and `systems/macros.scp`. A boss that is "gone"
is deleted: no death, body, loot or experience. "Zarici brana" is a variable gate (`i_varGate`) that goes after its time.

**OCS (Rascal, `c_ocs_boss`):**
- `e_explosionlava`, when hit:
  - Chance 50 permille, at most once in 30 s, and the first one only 30 s after the first blow (Rascal's own: 350, 10 s);
  - "throws lava bomb": a lit Lava Bomb on a random tile within 5 (Rascal: 6), fixed to the ground, which goes off as
    the potion does.

  Fixed from Erebor: the throw no longer sets the attacker's potion delay, and the spent bottle doesn't stay as litter.
- `e_ocs_boss_resurrection_able` (the two helpers): a helper's body is remembered for 200 s, then it goes with what's
  left in it.
- `e_ocs_boss_resser` (Rascal), when hit, 100 permille: "Pomocnici moji, povstante!", and every remembered body rises as a
  fresh helper worth no experience, kept within 3 tiles of the spot, gone after 7200 s. Erebor kept the bodies in a
  container a GM set up by uid in the world; here the server remembers them.
- `e_bomba_boy` (Duse Nakazeneho): after its first swing, landed or dodged, "Umrete!" and an explosion; a second later
  every player within 4 takes 60 (the species' `tag.bomba_boy_dmg`), 5 less a tile beyond the first, and it's gone.

**Gates:**
- `e_ztracene_mesto_paladin_boss`: dying, black fire columns and a gate to 807,633,0 on plane 2 for 300 s.
- `e_strach_lord1` (`c_strach_mage_chief`): below a sixth of his hits, "Beleg Faroth Kel", "Pokud mate odvahu se mi
  postavit tak mne nasledujte!", he stands in fire, opens a gate to 5348,4063,1 on plane 10 for 1000 s, and is gone 3 s
  later.

  Fixed from Erebor: once (every blow in those 3 s opened another gate), and on plane 10 (Erebor's target had no plane,
  so it led to plane 0).

**Beridor:**
- `e_beridornecro` (Arghelior): his first three blows taken get a line each, then "Aaaargh !", "Zhynte !" or "Neeee!".
  Below half his hits, "Konec legrace !", "Heru Val Foalooke !", "Necht plamen pohlti vase duse!", a ring of fire, the
  Beridor dragon where he stood, and he's gone 4 s later. Erebor fired a spawn a GM had placed; here the dragon comes at
  his spot.
- `e_necroslave` (Beridor's warriors, mages and dragon, and players dying in an `e_beridor` area): a while after death
  (the monster's `SelfresDelay`, 10 s for a player) the body rises as an undead, picked by what the dead held:
  - a bow or crossbow: an archer; a two-handed sword or axe: an axeman; a shield: a knight;
  - else a one-handed sword, axe or fencing blade (not a dagger or skinning knife): a knight; else a mage;
  - Erebor read Sphere's second hand for the two-handed weapons; they sit on the first hand here, so it goes by the
    weapon's kind;
  - a dragon: a bone dragon;
  - level 2 or 3 for a monster; for a player by level: up to 11 the plain one, to 30 level 2, to 50 level 2 or 3, above
    that level 3 or 4.

  It takes the dead one's name and everything left in the body, gives no experience, and is gone after 3600 s. A carved
  body doesn't rise: "Mas stesti, zes telo rozrezal!". Summons never rise.

  Fixed from Erebor: a death with no body leaves nothing waiting, and the empty body goes.

Test: `DyingBossTests` (protocol): the lava bomb's first delay and its one bomb, the ghost's blast, the paladin's and Lord
Strachu's gates (once), OCS helpers rising, Arghelior's dragon, the undead by weapon and a carved body.

## Bosses with damage rules (#1022)

`DamageRules.cs`, `Shapeshifter.cs`. Erebor's `systems/new_events.scp`, `systems/macros.scp`, `systems/aldaron.scp`,
`sphereevents.scp`.

- `e_balrog` (Balrogh, Zatraceny, Vidlak Ochrance):
  - its swings, dodged ones too: 1 in 6 "Popalil te Balroghuv ohen!" and 10-26 fire, else 1 in 10 "Balroghuv ohen te
    oslabil!" and no stamina left;
  - a blow at it: 1 in 8 "Cast tveho uderu byla odrazena zpet!" and 10-40 magic back; 1 in 6 that kills an attacker with
    25 hits or less;
  - a spell on it: 1 in 6 its shadow curse on its target (Erebor's: the Balrog's target, not the caster), one at a time,
    for 30 s: every 4 s (first after 2 s) "Dusis se!", 10 mana and 10-20 hits; trying to cast fails, for 10 hits and 15
    mana.

  Fixed from Erebor: the curse's mana loss read the curse item's mana, which zeroed the victim's; here it's 10 a tick.
- `e_rinald` (the Grandmaster quest's Rinald, the GM event boss Rinald): only players hurt him, any other damage is lost.
  Each player's blow or spell weakens him by their profession, as Erebor's:
  - necromancer: summon, fire and cold resists -25; mage: summon, fire and electric -25;
  - warrior: sharp, slashing, blunt -50; priest: blunt -50 and 1 in 3 a second's stun;
  - ranger: stabbing -50 and 1 in 3 a second's stun; thief: poison and bleed -25 and 50 hits;
  - mystic: 1-5 Dex, 1-10 Str, electric and cold -25; shaman: 25-50 hits, fire, electric and cold -50, 2 in 3 15 mana
    to the shaman;
  - craftsman: 75-100 hits, and for each weapon held, 1 in 20 his own work kills Rinald at once, else 2 in 3 another
    50-100 hits.

  Still invulnerable (before his name is spoken), each try gives "Pche! Pokud neznas me jmeno, tak me neporazis!" and
  200 more hits, up to 5000. Fixed from Erebor: hits taken off directly leave him at 1 at least; the blow does the killing.
- `e_odinvis_krik` (the living helm): when hit, 1 in 12 "crying.", and a second later every hidden or invisible player
  in its 64x64 part of the map is found ("Byl jsi objeven!").
- `e_ztracene_mesto_druid_medved`: Menivec's mechanism with a bear and a wolf, as "Druid", "changing!". A look only, as
  Menivec. The shapeshifters' sparkle is now Erebor's `i_fx_sparkle` (0x375A); it was 0x373A.
- `e_emotedmg` (the test titan, or anyone a GM gives it in .info): says each landed blow's damage and type, "-37: flags
  0402". Erebor's number was before armor, this is what the blow did; Erebor's `.emotedmg` toggle isn't ported.

Test: `DamageRuleBossTests` (protocol): the Balrog's burn, weakening, reflection and curse; Rinald's invulnerable growth,
players only, a warrior's and a mage's weakening; the helm finding the hidden; the druid's forms.

## Darkmoon's forms (#1022)

`Darkmoon.cs`, `darkmoonForms.def`. Erebor's `questy/darkmoon_chars.scp` (e_darkmoon_boss, getMorfBody, the f_c_* form
functions).

- When hit, 200 permille, at most once in 20 s, Darkmoon takes another of her forms, never the one she has: the fire ant
  worker, the warrior, the giant beetle, the giant bug, the fire ant queen, or herself.
- A form is an NPCDef that's never spawned (`c_darkmoon_worker` ... `c_darkmoon_queen`; her own is `c_darkmoon`). It
  gives her its look, colour, a new maximum of hits, stamina and mana, Magery and Eval Int, resists, abilities, and the
  numbers those abilities use.
- The abilities are the form function's EVENTS and the creature's own TEVENTS, as Erebor's `BODY=` brought both: the
  beetle and the bug self-resurrect, the queen throws lightnings and holds like the strangler vine.
- `MonsterSpecialAttacks.TuningDef(npc)` is where an ability reads its per-species numbers: the form's while she has one,
  else the NPC's own def. Bleed, critic, the three curses, summoning, multishot, round attack, dispel bomb, para krik and
  lightnings read through it.

Fixed from Erebor, as the plan on #1022 says:
- a form replaces the last form's abilities and numbers. Erebor's `EVENTS=` only added, so after a few changes she had
  every form's abilities at once with the latest numbers, and e_monster and the morph itself ran twice per trigger;
- her hits never stay above a new, lower maximum.

Not carried over: the forms' monster type (for slayers) and clothes coming off; her name stays "Darkmoon".

Test: `DamageRuleBossTests.DarkmoonTakesAFormWithItsAbilitiesAndNumbers` (protocol): the worker's look, bleed and numbers
without her dispel bomb, the queen's lightnings and Magery, back to herself, a blow and the cooldown.

## Halda Molle's fights (#1022)

`HaldaMolle.cs`. Erebor's `questy/halda_molle.scp` and `questy/halda_molle_char.scp`. None of these monsters is placed
in the world yet: Halda Molle has no spawns (the user's question).

- `e_rozdeleni` (Bezejmenny, the boss), when hit at 15% of its hits or more, 35 permille (the species' own,
  `tag.e_copy_chance`):
  - it loses a tenth of its hits, and a copy appears beside it with 90% of what's left, for 12 hours, worth a third of
    its experience ("Mostrum se rozdelilo.");
  - while the copy lives nothing hurts the boss ("Monstrum ti odolava."); damage with no resist type at all still does.

  Fixed from Erebor:
  - the guard ends when the copy is gone for any reason (Erebor's stayed for good if the copy timed out);
  - the boss keeps its own resists (Erebor's guard functions blanked its summon and bleed resists on the first blow,
    and could leave it immune).

  Copies keep their loot, as Erebor's.
- `e_frosty_wave` (Bezejmenny), when hit and once on sight of each new player, 240 permille: cold tiles on the 8 rays.
  - Timing: tiles at 1-2 tiles, a second later at 3-4, then at 5-6 (the species' strength 2), each lasting a second.
  - Effect: a player on one (not a GM) gets "Byl si zmrazen.", loses 59 hits whatever the armor, and is paralysed for
    6 s.
- `e_drop_egg` (the warniaks): on sight of a new player, half the time it lays an egg ("Vejce").
  - Hatching: 60 s later a warniak of its kind (the species' `tag.warniak_type`) hatches, and is gone 700 s after the
    egg was laid.
  - Stomping: someone carrying more than 400 stones can stomp the egg ("Rozslapl si vejce."); anyone lighter gets
    "Nedokazes rozeslapnout vejce."
- `e_warniak_degrade` (warniak 4): dying, the next smaller warniak rises on the spot, and so on down to the smallest.
- `e_warniak_drop` (Marali koren):
  - when hit, 100 permille at most once in 10 s, a warniak (tier 2, 1 or 3, 3:1:1);
  - on sight of a new player, at most once in 50 s, a tier 2 that attacks them;
  - dying, one of each tier 1-4.

  Each lives an hour.

Converter: `tag.e_copy_chance`, `tag.e_frosty_wave_chance`, `tag.e_frosty_wave_strong`, `tag.warniak_type`,
`tag.warniak_count`, `tag.e_warniak_hit_chance`.

Test: `HaldaMolleTests` (protocol, account hmtests): the split and its guard, the frost, the eggs (too light, stomped,
hatched), the degrading, Marali koren's warniaks.

## The HCN tombs' monsters (#1022)

`HcnTombs.cs`. Erebor's `systems/dungeon_bonusy_hcn_npcEvents.scp`. The tombs themselves (puzzles, spawner pads) aren't
ported, so nothing places these monsters yet.

- `e_hcn_teletotarget` (the rat, the wraiths): while it fights a player, every second it dashes at its target, within
  20 tiles and in sight.
  - Dash length: the species' own steps (rat 8, wraiths 3-6: Erebor's `tag.npc_port_distance`, converted for every
    monster as `NpcPortDistance`), else up to 3 short of it.
  - Retargeting: every 5 s it goes for the player it hates most among those it remembers within 30 tiles. Mage and
    necromancer 1000, priest and shaman 950, mystic 930, the rest 900, less 10 a tile. It forgets them every 30 s and
    looks again.

  Fixed from Erebor:
  - rangers count (Erebor's table left them out);
  - GMs are left out (one as a target broke Erebor's memory);
  - a player is remembered once.
- `e_hcn_npcpoison` (the snake): a blow that does damage poisons with 14% of the victim's hits every 2 s (Erebor's
  poison_c through the poison core). The snake's `tag.jed_sila(14)` is the default; the converter doesn't read that
  call form.
- `e_hcn_madnessability` (the mummies), on each swing, dodged ones too, at the species' chance (2%, 20%, 2 in 3):
  - the player it swings at goes mad at the species' strength (25%, 40%, 65%) plus 0.4% a level, and turns their
    profession on the players and pets around them;
  - warrior, armed: up to 8 within 2, their own blow;
  - thief, armed: one within 2, blow and bleed;
  - mage: 5 within 8, a fire burn of a sixth of their mind power x10;
  - ranger with a bow: one within 8, blow and knockback; without, a plain blow on one within 2;
  - necromancer: 5 within 4, poison of their mind power;
  - priest: 5 within 4, Curse at their Healing;
  - shaman: a random voodoo on one within 8 who has none;
  - mystic: 5 within 4, their mind-power blow and 20-30 s paralysis;
  - anyone else: a plain blow on one within 2.

  It costs them 10-20 stamina.

  Departures:
  - the mage's and necromancer's power is their mind power (Erebor's `tag.sm`);
  - the mystic's strike is their own blow (Erebor's spell 103 isn't ported);
  - all of it lands at once (Erebor's came 0.1 s later).

Converter fix: `KARMA=-{4000 4999}` (the minus outside the braces) gave positive karma. It's `-4999 -4000` now: the HCN
rat and two crunch monsters.

Test: `HcnTombTests` (protocol, account hcntests): the pursuit's memory, dash and hate, the snake's poison, a mad
warrior and mage, the rat's karma.

## Proactive bosses (#1146)

`Proactive.cs`. The user's design, beyond Erebor: the boss reactions of #1022 also act on the monster's own initiative,
not only when it is hit. They include the dragons, the summoning bosses, the lava bomb, the OCS raising, the helm's cry,
Darkmoon's forms, the shapeshifters, and Halda Molle's frost wave and warniaks.

- While a boss fights, every `monsterInitiativeSeconds` (NPC Aggro settings, 6 s; 0 turns it off) it rolls its
  reactions against its target, at their own odds and cooldowns, as if the target had hit it. The first roll comes a
  full interval into the fight.
- Its own landed blow rolls them too (`monsterInitiativeOnOwnBlows`, on).
- Some stay reactions only: the Balrog's reflection, Bezejmenny's split, Arghelior's per-blow lines, the sand
  guardian's warrior line. `monsterInitiativeOff` lists more, by ability defname, comma separated.
- Being hit, or a damage spell (#1145), rolls them as before. The dragons' reactions now run on spells too.

How: the reaction checks go through `MonsterSpecialAttacks.Reacts(npc, ability)`, which is `HasAbilityDef` except while a
monster acts on its own initiative.

Found on the way: a shapeshifter picking a form that doesn't exist (Menivec's `c_eye_adamantium_menavec` isn't in the
archive) did nothing, one change in eight; it now picks among the forms that exist.

Test: `HcnTombTests.BossesActOnTheirOwnInitiative` (protocol): the tick's timing, the fire lord's summons on the tick and
on its own blow, the off switch and list, the Balrog's reflection staying a reaction.
