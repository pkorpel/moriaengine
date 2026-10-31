# Potions

Erebor's potions (`sphereitem_potions.scp`, `magie.scp`). Code: `scripts/Basic/Potions`, one file per kind. Numbers are
on `.settings` (Potions), in Erebor's own form: `seconds,min,max`.

`Potion` is the item. What it does is a `PotionKind`, found by the def in `PotionKinds`. A new potion is a new kind and
one line there.

## Drinking

A double click, or a macro like `.potionheal`, which takes the strongest one carried.

- One wait between all potions: 5 seconds after most, 20 after heal, refresh, invisibility, reflection and reactive. Too
  soon and nothing happens but a sound. The wait shows under Cooldowny on `.boosts`.
- The 20-second ones punish hammering: a third try within 1.5 seconds adds 5 seconds, up to 100.
- The bottle comes back, or breaks 1 time in 10.

## Effects

| Potion | Does | Numbers |
|---|---|---|
| Agility, Strength, Cleverness | Dex, Str, Int | less 120 s +12-20, normal 300 s +14-20, great 600 s +16-20, elixir 900 s +30 |
| Energy | Vit | as above |
| Spell Shield | Magic Resist | 600 s +28.0, elixir 900 s +42.0 |
| Mobility | Resists paralysis | 30 s, 1000 |
| Stoneskin, its elixir (Steelskin) | Armor | 600 s +5, 900 s +7-8 |
| Nightsight | Full light | 1800 s |
| Invisibility | The spell's effect | 240 s |
| Reflection, Reactive armor | The spell's effect at full power | |
| Heal | Hitpoints | 30, 50, 80, elixir 1000 |
| Refresh | Stamina, permille of the maximum | 200, 400, 700, elixir 1000 |
| Cure | Lowers a poison, as the Cure spell does | 50, 200, 400 |
| Perception | See hitpoints, see below. Adds to time left | 5, 15, 30, 60 min |
| Dragon resist | 10% against dragons | 300 s |
| Purification (q006) | Only its message | |

Erebor's old plain bottles (`i_bottle_orange` and so on) cast their spell on the drinker.

**One potion at a time.** Erebor's runes all sit in Sphere's one potion layer and each replaces the last, so stat
potions never add up: Agility replaces Strength, and Invisibility, Mobility, Spell Shield and Dragon resist share that
slot too. Nightsight and the skins have slots of their own. Spell and potion effects stand side by side, and potions
aren't dispelled.

## Bombs

Explosion 40, 60, 80, Lava 100, Dispel. Put down or double-clicked on the ground, one counts 3, 2, 1 and goes off.
Lifted, it goes off at once. Double-clicked in a pack it hits its user 3 seconds later, unresisted.

Within 5 tiles it does `strength - armor`, divided by the distance past 1, as magic fire. Nobody answers it and it's no
crime. It also destroys webs, and rocks by chance. A dispel bomb dispels those around and removes spell items.

`.throwexplosion` and `.throwdispel` throw the strongest one at a target within 6 tiles and in sight, every 4.5 seconds.
It lands a second later and counts down there, fixed in place. Without the `a_throwing` ability it can land anywhere near.

## Shrink, kegs

Shrink turns your own pet within 3 tiles into a figurine, not a summoned one. A double click brings it back, as the
owner's who clicks it, with its name, colour and hitpoints.

A keg (`i_keg_potion`) holds 1000 of one potion. Target a potion to pour, empty bottles to draw, a garbage can to empty it.

## Perception

While it lasts a player sees `-1,5% [85,2%]` over those around being hit or healed, within 18 tiles, and hitpoints over a
character they click. `.boosts` has a Vnimani button for its options: on or off, compact, colours by target, colour-blind.

## Numbers and gaps

- The Cure potions' defs aren't in the archive, only their strengths, so what those mean is read from the Cure spell.
  They're much stronger than the spell's 0-10, so they likely end any poison.
- Perception's times aren't in the archive either. They're mine, on `.settings`.
- Stoneskin's +5 and Steelskin's +7-8 are `nastaveni_spelleffect_s_Stoneskin` and `_s_Steelskin`.
- The dragon cures have nothing to cure, and dragon resist nothing to resist, until the fire dragons' effects are ported.
- Erebor's throw unequips a second-hand item first, unless it's a staff. Not done here.
- Not ported, they belong to quests that aren't: `i_lek_pro_liguka`, `i_q026_hcnpoison_cure`, `i_bottle_rostok_muznosti`.
