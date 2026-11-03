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

## Shaman's voodoo and carving bodies

A blade used on a body carves it (`CorpseCarving.cs`, f_weapon_sharp_targon_item): daggers and other fencing blades
through their knife target, swords, axes and sharp maces with no tool skill of their own on a double click. A carved
body is marked: bandages can't raise it and a monster's own rising (`e_selfreser`) doesn't come. Each body only once: a
player's falls apart into a head named after them, a torso, arms and legs, all knowing whose they were, and blood, gone
in 5 minutes (addflesh); any other gives what its NPC's RESOURCES list (#587).

A coloured dagger used on a head curses its owner (`ShamanVoodoo.cs`, woodoo), the metal deciding how: copper a fifth of
the hits, iron all the stamina, verite bleeding, valorite poison (Sphere's poison(1000), here 8% of the hits a tick),
obsidian no casting ("Jsi v soku, nemuzes kouzlit!"), adamantinum no recall or travel stone, mithril no riding.
A shaman of level 20, `250 + 20 * woodoo_curse` out of 1000; a failure curses the shaman, a success waits 300 s. The
owner must be in the shaman's area; it reaches them in 10 s, comes back every 30 s, and ends when they're blessed or
dead. One already cursed, or with magic reflection, throws it back at the shaman. After the head's first use every use
costs the shaman all stamina and half the mana, and wears the head until it falls apart.

A potion used on a head (double click the head) blesses its owner: Strength, Cunning and Agility potions their spell
at 1.334 * the shaman's Spirit Speak, Stoneskin protection at 1.4 * it (and the chest armour comes off). `woodoo`
points * 900 out of 10000, 3 s between.

`.astralmorph` (a GM command, as it wasn't on Erebor's lists): a shaman takes a monster corpse's body and colour and
wears nothing while morphed; again gives the own body back.

## Mount powers (#925)

A mount whose chardef has `tag(magic_horse,name)` (`NPCDef.MagicHorse`, from the converter) gives its rider that
`<name>_mif` table of jezditka.scp while ridden (`MountPowers.cs`), as a worn magic item would: slayers 101-105, melee
and ranged block 134-135, and for the black horse Hiding and Stealth. The rider's mount knowledge adds its share of the
slayers again (Erebor's `i_memory_taming_bonus`): the known share of all mounts, at most 50 a point of the ranger's
`mountsKnowledge`, and never past Taming or Animal Lore. `.boosts` shows them by the ridden mount.

## Icons

Erebor's small icons at the top left (`AbilityIcons.cs`): a charge (magic charges, killing spree, the war fork's round
strike) as its picture with a small one per charge (`d_charge`), a readied split shot (`d_splitShot`) and a readied
crushing arrow (`d_crushingArrow`). They stand side by side, 50 apart, each keeping its place while open.

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

## Ranger

A bow takes only the ammunition its TDATA3 names (`WeaponDef.AmmoType`, from the converter): bows arrows, the heavy
crossbow plain bolts, the light crossbow jagged bolts.

Bleed bolts (`e_bleed_bolt`, on every light crossbow): a landed Archery shot bleeds at `bleed_bolt` points * 45 out of
1000 (`bleedBoltChancePerPoint`), with `blow * (bleedBolt (250) * the bolt's BleedCoefficient / 1000) / 1000`. The
coefficient is `nastaveni_arrowdamage_` 25 places after the bolt's damage slot: copper 600, iron 700, verite 950,
valorite 1150, obsidian 1500, adamantinum and mithril 0 (Erebor's data). In the saddle it doesn't bleed.

The crushing arrow needs ammunition ("Nemas zadne strelivo.") and uses one when it lands. Its resist reductions are
Erebor's records: the strongest running one is what's off, a weaker one that lasts longer takes over when it ends, and a
weaker, shorter one is dropped.

## Thief

A thief's war fork on a monster also has the double swing, at any level (e_thief's @AfterSwing). Any fencing blow
spends a readied throat slit (`.podriznuti`), whether it came from behind or not.

`.mask` is refused in the areas with `tag.nomask` (the start rooms), "ty hnusnej bugere. Nic nebude", as on Erebor.
The naming dialog suggests a random name of the player's sex from Erebor's name lists (`NameList`). Under the Incognito
spell the mask renames the name the incognito hides (Erebor's `i_rune_incognito`), so the incognito ends on the mask's
name. `.rentalinfo` shows a thief now of another realm than the viewer under the other identity, in the viewer's realm's
colour (`getrealname`).

A poisoned throwing knife poisons what it hits with all its poison (`f_poisoning_weapon_checkPoisoningValues`,
`f_poison_doPoison`). With a long range running, the knife also stops a recall or gate being cast (`fizzmagictravel`);
so does a knockback, and a mystic's special strike on a player turns into that instead of the freeze when the mystic
has a long range. The long range interrupts nothing else.

`.throw`: a target's worn magic ranged reflex (132) throws the knife back, costing the thief a fifth of their stamina,
and may drop it at their feet; ranged block (135) stops it. Both are rolled before the hit.
