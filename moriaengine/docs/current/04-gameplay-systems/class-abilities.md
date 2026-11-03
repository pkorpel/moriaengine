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

## Mage's silence

Mind Blast's silence (`silence.scp`) meets a monster's `tag.resist_silence`, `NPCDef.ResistSilence` from the converter:
it comes off the chance, and at 1000 or more the monster can't be silenced, "Cil odolava umlceni".

## Priest's and everyone's resurrection

The Resurrection spell on a player who died less than 5 s ago does nothing, "Na oziveni jeste nejsi pripraven."
(e_allplayers' @beforegeteffect), unless the area has `region_cf_instantRes`: the four arenas and Domov drakulu.

## Warrior

Double swing (e_war) is the warrior's alone, from level 30, with a long or broad sword or a large battle, double or
two-handed axe. A taunted monster has karma -100 until the taunt ends (`f_taunt`'s `restore_karma`). Round attack's
strike charges are the thief's war fork ones (`.strike`). Leap's GM-step resist isn't here: it only fed Erebor's GM-step
detection, left out in #777.

## Thief

A thief's war fork on a monster also has the double swing, at any level (e_thief's @AfterSwing). Any fencing blow
spends a readied throat slit (`.podriznuti`), whether it came from behind or not.

`.mask` is refused in the areas with `tag.nomask` (the start rooms), "ty hnusnej bugere. Nic nebude", as on Erebor.

`.throw`: a target's worn magic ranged reflex (132) throws the knife back, costing the thief a fifth of their stamina,
and may drop it at their feet; ranged block (135) stops it. Both are rolled before the hit.
