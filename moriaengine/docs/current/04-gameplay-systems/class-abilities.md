# Class abilities

Erebor's vedlejsi abilities (`experiences.scp`'s `ra_` tags, `specabilities.scp`), #1017 and its leftovers #1070. Code in
`scripts/Basic/ClassAbilities`, one file per class, numbers on `.settings` under Class Abilities.

## Points

Two points a level to spend (`d_rearability`), each ability limited by class and level (`RearAbilityTable.cs`).
`d_class_info` (`.class_info`) lists a class's abilities and commands. Abilities the engine already had as an `AbilityDef`
keep it (`ClassAbilities.abilityDefs`); the rest are tags named as on Erebor. `ClassAbilities.Points(ch, name)` reads
either.

The class counter (`.berserker`, `.sniper`, `.ripper`, `.vigour`, the necromancer's and the mage's, Erebor's
`specability`) is a field of the profession plugin every class has (#1438). Each class can switch on only its own;
death takes it, in the plugin's `@death`; a new profession starts without one. Before, a counter left on through a
profession change kept firing for the new class.

The necromancer's darkpower and the mage's concentration give mana back after a successful cast (f_spelldone: 85%,
half from a scroll, never for recall, mark or gate). The profession plugin answers Magery's `@spellSuccess` for both
classes (#1440); it ran as a direct call on the line before that trigger.

## Spell absorption

`speldodge` (`a_spell_absorption`), e_allplayers' @beforegeteffect: a monster's spell on a player is swallowed whole,
"Kouzlo pohlceno.", at `points * 10` out of 1000, the permille weapon dodge uses too (`nastaveni_ability_pvMdodge`).
A player's spell never is, nor is anything while the player is possessing a monster.

## Shaman's own state (#1442)

- The shaman has its own profession plugin, `ShamanProfessionPlugin` (`p_profession_shaman` in shaman.def), on the
  warrior's pattern. It holds the cooldowns of voodoo, the pacts and the life pact.
- The stats ritual (`.voodooint`, `.voodoovit`) is a plugin of its own on the character, `VoodooStatsPlugin`, which is
  Erebor's `i_voodoo_stats`. It isn't on the profession plugin: it changes the character's own stats, so it outlasts a
  profession change, and `.voodoorovnovaha` needs no class. Its own `@death` puts the stats back.
- The voodoo shield (`.voodooshield`) is `VoodooShieldPlugin`, built like the energy shield. Its pool is 5 a point and
  lasts 30 s; then, or when it's cancelled or a spell breaks through, the shield rests 180 s. A monster's spell eats the pool first, at its fixed point in the damage. The
  shield stays through death, as Erebor's memory item did, and `.boosts` shows it.

## Shaman's pacts (#1445)

A blood, mana or stamina pact is held by one `PactPlugin` on each side. Each character has one, holding whatever it gives
(type and bearer), whatever it bears (type, giver, and the regen every 5 s with its end) and the blood sharing's switch.
It goes once it holds neither side. One plugin, not one per side: a mana pact may be the shaman's own.
- **Death and logout:** the plugin's own `@death` and `@logout` end the pacts, the borne one first, otherwise the given
  one. They were direct calls before.
- **Blood sharing:** a monster's blow moves 15%, at most 20, from the one hit to the other side. It answers
  `@afterGetHit`, raised once the blow's damage is dealt.
- **Experience:** the blood pact's experience (the giver earns through the bearer) is still read by the leveling code.

## Shaman's life pact and empathy elixir (#1447)

- **The life pact** is a `LifePactPlugin` on its receiver: Erebor's `i_soul_remover`. It holds the shaman who gave it and
  when. Its timer is the soul's end (1200 s, plus 60 s a `woodoo_lifepact` point); then the soul and the plugin go.
  - It stays through death, since rising from death is its use.
  - The dead receiver rises with `.lifepact` once the soul is 20 s old and 5 s after dying. In an area that resurrects at
    once (`region_cf_instantRes`, the arenas), the 5 s wait is skipped, as on Erebor.
  - Rising ends the pact.
- **The empathy elixir** is an `EmpathyPlugin` for 8 s (in `LifePactPlugin.cs`): Erebor's `i_getphysresists` with
  `e_getphysresists`.
  - Its own `@swingLanding` reads the struck monster's physical resists by Arms Lore. Erebor's `@AfterSwing` ran for a
    dodged blow and a landed one alike, and `@swingLanding` runs before the dodge, so a dodged blow is still read.
  - It stays through death and shows in `.boosts`. It ends with Erebor's line ("Nyni jiz nejsi schopen rozpoznat
    rezisty.").

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

How these are held (#1450):
- **On its way**, a curse or a blessing is a `VoodooHandlePlugin` on its owner: Erebor's `i_mem_woodoo`. Each has a key
  of its own, since several may be on their way at once.
- **Arrived**, it is a `VoodooEffectPlugin`: Erebor's `i_woodooeffect`.
  - Each has a key of its own, so they stack as Erebor's memories did: a blessing beside a curse (it used to cure it), and
    a curse thrown back beside one already there (it used to replace it).
  - `.boosts` shows each as "Voodoo" with its kind, as Erebor's boosts menu did.
  - A shaman with either can't curse ("Prokleti ti brani proklinat nekoho jinyho."), and someone with either throws a
    curse back.
  - A curse's events are the plugin's own triggers: no casting and no Recall at `@denySpellCast`, after the casting
    rules, as Erebor's events ran after e_allplayers. No riding is at `@stepTaken`.
  - The travel stone and the ferry still ask the curse directly, as nothing there raises a trigger.
- **Not ported:** the extra `i_warcry_effect` Erebor's woodooeffect gave a curse. Its time reads `var(warcry_src)`, a
  global left by whoever last used a warcry anywhere. Also e_noport's telepad refusal (`t_telepad2` does nothing here).

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

## Mage's and necromancer's energy shield (#1401)

`.energyshield` is Erebor's `[function energyshield]` (`EnergyShieldAbilityDef`, `EnergyShieldPlugin`), its points the
`energyshield` class points (`a_energy_shield`). Its checks, in order, each with Erebor's line: a mage or necromancer
("Tohle nemuzes pouzivat."), points ("Tohle zatim neumis pouzivat."), not resting ("Tva magicka moc je jeste prilis
oslabena."), not up ("Jeste stale mas stit kolem sebe vytvoren."), an amethyst in the pack ("Nemas potrebne prisady."),
then over 60 mana raises it for 59 mana and the amethyst ("Heru Beleg Del Tir"), under 60 says "Nemas dost energie!"
and exactly 60 does nothing, as Erebor's.

- It holds 45 s and half a second a point, with 5 energy a point. As Erebor's damage_final ran it
  (`EnergyShieldPlugin.Absorb`, called by `DamageManager.CauseDamage` after the resists, before the mana shield, armor
  wear and minimal damage): any damage from a source that isn't a player (a blow, a spell, a monster's ability such as
  a round attack) takes its whole amount off the energy and lets through a share of it: 100 less a tenth of i_energyshield's
  table (21 a point to 10, 18 to 20, 15 to 30, 12 to 40, 10 to 50), so 79 % at 10 points and 24 % at 50. A spell goes
  through it twice (e_energy_shield's @aftergetswing, then f_dmgFinal_applyEnergyShield while energy is left): 20
  becomes 15 and then 11 at 10 points, and costs 35 energy. Damage that never reached Erebor's damage_final isn't
  touched: a player's, damage with no source (traps, bombs, poison ticks, starving, the wraith curse; Sphere raises no
  @GetHit then), and damage_noresponse (`DamageManager.CauseDamageNoResponse`: bleed ticks, dragon fire and stamp,
  hazard tiles, multishot).
- The blow that empties it: the energy left covers that much of the blow at the shield's share, the rest goes through
  whole. The energy stays below zero, so the shield still counts as up for that blow's knockback, bleeding and eye
  drain, until the timer's next tick starts the rest (Erebor's timer=0). Fixed from Erebor, whose formula divided by the
  covered share: a nearly empty shield made the last hit many times stronger (100 damage on 10 energy hit for 790).
- When it ends, by time, emptied or by `.cancelenergyshield`, "Stit se vycerpal." and 180 s of rest, then "Nyni muzes
  pouzit stit znovu.". It stays through death, as Erebor's memory item.
- While it's up: Recall is refused ("Nelze cestovat kouzlem.", the caster's `@denySpellCast`, after the casting
  rules); Gate isn't, Erebor's `argn==32 || argn==32` typo, kept as it ran. No knockback, eye drain, monster bleeding or wraith curse (q026). Standcast can't be switched on, and
  raising the shield switches it off. Moving is allowed: Erebor's `flag_immobile` line was commented out (ours used to
  drop the shield on a step or any travel spell).

## Priest's and everyone's resurrection

The Resurrection spell on a player who died less than 5 s ago does nothing, "Na oziveni jeste nejsi pripraven."
(e_allplayers' @beforegeteffect), unless the area has `region_cf_instantRes`: the four arenas and Domov drakulu.

The priest's self-resurrection (`.resurrection`) keeps its cooldown on the priest's profession plugin,
`PriestProfessionPlugin` (`p_profession_priest` in priest.def, #1435). The time of death and the 5 s rule above are
every player's, so they stay on the character. A GM of another class may still use it, without a cooldown. Setting
the profession again starts a fresh plugin, and with it no cooldown.

## Warrior

Double swing (e_war) is the warrior's alone, from level 30, with a long or broad sword or a large battle, double or
two-handed axe. A taunted monster has karma -100 until the taunt ends (`f_taunt`'s `restore_karma`). Round attack's
strike charges are the thief's war fork ones (`.strike`). Leap's GM-step resist isn't here: it only fed Erebor's GM-step
detection, left out in #777.

The warrior has its own profession plugin, `WarriorProfessionPlugin` (`p_profession_warrior` in warrior.def, #1429). It
holds the stance and the cooldowns of round attack, leap, war cry and both taunts. Erebor kept these in tags. It also
changes the warrior's blows through `@swingLanding` and `@getSwingLanding`: double swing and the stance on its own blow,
the stance on a monster's. A blow's order is unchanged. The critical blow (the shared counter) runs first, then
`@swingLanding`, where each class's plugin does its own part (warrior, ranger #1431, thief #1433), then the jagged bolt;
after the mystic's stolen talents and the magic items' slayers, `@getSwingLanding`, then the dodge. Shield
blocking (`vykryti`) is open to six classes, so every profession plugin does it in `@getSwingLanding`, after the stance.
The stun blow and the critical blow are shared by several classes and are still plain calls (the counter itself is the
profession plugin's since #1438). A profession
change starts a fresh plugin: a stance and cooldowns don't carry over.

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

The ranger has its own profession plugin, `RangerProfessionPlugin` (`p_profession_ranger` in ranger.def, #1431), on the
warrior's pattern. It holds the cooldowns of knockback, split shot and crushing arrow, and the killing spree's last
target. Erebor kept these in tags. The spree adds to the ranger's blow in `@swingLanding`, after the critical shot. It gets a charge in `@kill`, Sphere's trigger on the killer (Erebor's `@finalBlow`), and two when
its own target dies. A dodged blow still uses a charge up, as on Erebor. Shared with other classes and still plain
calls: the critical shot (the counter), the bleed bolt, and the readied effects' timers on the player.

## Thief

A thief's war fork on a monster also has the double swing, at any level (e_thief's @AfterSwing). Any fencing blow
spends a readied throat slit (`.podriznuti`), whether it came from behind or not.

The thief has its own profession plugin, `ThiefProfessionPlugin` (`p_profession_thief` in thief.def, #1433), on the
warrior's pattern. It holds what Erebor kept in the thief's tags:
- when hiding began, and a backstab under way;
- the readied throat slit and shield strike, with their cooldowns;
- the knife throw's and round strike's cooldowns.

e_thief's @AfterSwing is its `@swingLanding`, in Erebor's order: the blow from behind and the throat slit, the war fork's
charge, then the war fork's double swing, which spends the slit. Only a thief charges a war fork or strikes from behind
now. Before, anyone fencing a war fork with a class counter could charge it, and stray `utok_do_zad` points struck from
behind; on Erebor both were the thief's own event. The shield strike still runs
from a direct call after the damage, beside the stun blow other classes share, with its state on the plugin. The mask
and the knife immunity stay tags: the mask swaps the character's own identity and must outlast a profession change, and
the immunity belongs to the target.

A backstab whose target is gone before its 0.1 s wait no longer blocks the next one (#1436).

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
