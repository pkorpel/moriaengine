# Class abilities

Erebor's vedlejsi abilities (`experiences.scp`'s `ra_` tags, `specabilities.scp`), #1017 and its leftovers #1070. Code in
`scripts/Basic/ClassAbilities`, one file per class, numbers on `.settings` under Class Abilities.

## Points

Two points a level to spend (`d_rearability`), each ability limited by class and level (`RearAbilityTable.cs`).
`d_class_info` (`.class_info`) lists a class's abilities and commands. Abilities the engine already had as an `AbilityDef`
keep it (`ClassAbilities.abilityDefs`); the rest are tags named as on Erebor. `ClassAbilities.Points(ch, name)` reads
either.

## Spell absorption

`speldodge` (`a_spell_absorption`), e_allplayers' @beforegeteffect: a monster's spell on a player is swallowed whole,
"Kouzlo pohlceno.", at `points * 10` out of 1000, the permille weapon dodge uses too (`nastaveni_ability_pvMdodge`).
A player's spell never is, nor is anything while the player is possessing a monster.

## Mystic hypnosis and the world

The body swap itself is in `MysticHypnosis.cs`. While a mystic possesses a monster:

- Guards leave it alone unless the body is evil (karma -800 or less), as hypnoswap's karma (-10000 for an evil body)
  against `e_guard`'s "karma above -10000". It is never a murderer to them: hypnoswap sets kills to 0.
- Every monster with Erebor's `e_monster` goes for it on sight, as e_monster's @npcseenewplayer.
- Its name shows in the body's notoriety colour, with no guild.
- It can't open travel stones, house, stall and field signs, town and tax stones, the General's and Lord Nazgul's stones
  or the Nazgul shrines; Erebor's @userdclick returns at once.
- A var gate refuses it unless the gate has "Hypno-NPC muze projit" (`vg_hypnoGoThrough`).
- Chat and `.explevel` refuse it, and so do telepads, potions and bandages.
- A monster's disarming blow (`e_getwpn`) leaves its weapon alone.

## Thief's knife

`.throw`: a target's worn magic ranged reflex (132) throws the knife back, costing the thief a fifth of their stamina,
and may drop it at their feet; ranged block (135) stops it. Both are rolled before the hit.
