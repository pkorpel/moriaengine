# Monster loot (#791)

A killed monster's corpse holds the loot it was created with. Ported from Erebor
(pkorpel/erebor-sphere-archive) in two parts, in the same order Sphere ran them.

## 1. `Loot =` lines - Erebor's `ON=@NPCRestock`

Sphere runs a chardef's `@NPCRestock` trigger when the NPC is created, and its `ITEM=` lines
put items into the NPC's backpack. The converter turns each into a `Loot = item[,amount]` line on
the NPCDef (`ConvertedCharDef.ConvertRestockLoot`); 1,975 such lines across 68 files. The item can be:

- a plain itemdef (`Loot = i_arrow,{30, 40}`),
- a `RANDOM_*` weighted pick-list constant (`Loot = random_reagent,{50, 75}`, from `temp_loot.def`;
  lists can nest),
- a `loot_*`/`tm_*` template (`Loot = loot_ORC`; the templates are `[TemplateDef]`s in
  `temp_loot.def`, themselves lists of `ITEM =` lines),
- the gold marker `Loot = i_gold` (see part 2).

`NPC.On_Create` calls `NPCDef.CreateLoot(backpack)`, which runs every line through the template
`ITEM` op (`OpNode_TemplateItem`) - so loot behaves exactly like `ITEM=` in templates. Each line is
compiled once per NPCDef; a broken one is logged and skipped. Backpack contents that aren't
`attr_newbie` fall to the corpse on death (`Corpse.InitFromChar`).

Two template-engine fixes this needed:
- a rolled amount of 0 (`ITEM=RANDOM_JEWEL,{0 1}`) now means **no item**, as in Sphere - it used to
  create an empty stack;
- a template without its own `CONTAINER` fills the given container and returns it;
  `Container.NewItem` used to treat that as a misplaced new item and delete it - i.e. the backpack
  (`loot_cyclops`'s nested `tm_rocks`).

Converter repairs: archive typos `i_gold{700 999}` (missing comma) and lines with a second
`ITEM=` glued on. Not converted: vendor `SELL=`/`PRICE=` and axis `CATEGORY=`-style lines.

### NPC gear in `@NPCRestock` (#793)

Erebor keeps the clothing of its human NPCs (vendors, trainers, quest NPCs - about 190 chardefs)
in `@NPCRestock` too, as `ITEMNEWBIE=` lines. The converter reads the block in order:

- `ITEMNEWBIE=` -> `Equip =`, exactly as from `@Create`: worn, and `attr_newbie`, so it **never
  falls to the corpse** (players can't loot it). A defname already given by `@Create` (e.g.
  `i_hair_long`) is skipped.
- `COLOR=` / `NAME=` belong to the latest item line, even with other lines in between (Sphere
  applies them to the last created item): `EquipColor =` / `EquipName =` after an `ITEMNEWBIE`,
  dropped after a loot `ITEM`.
- `ADDSPELL=` -> `AddSpell =` (16 NPCs keep their spells here).

825 `Equip`, 543 `EquipColor`, 13 `EquipName`, 23 `AddSpell` lines. The engine resolves two more
Sphere forms in equip names: a hex graphic (`Equip = 01f07`, Sphere's leading-0 hex - the itemdef
of that graphic) and a `random_*` pick-list constant (`Equip = RANDOM_LIGHT` - candle, torch or
lantern), besides the hair/pants/shoes/skirt pools from #730. Three archive typos
(`i_lire`, `i_hat_feathered`, `i_bear_short`) still log "not a known ItemDef" and are skipped.

## 2. `Equip = i_monster_create` - Erebor's `creaturestart`

In Erebor, every monster gets an `i_monster_create` script item whose 0-second timer runs
`creaturestart` once after creation. Here the item is no longer created; `NPC.On_Create` adds a
`CreatureStartTimer` (also due at once, #794) that runs `MonsterLoot.ApplyCreatureStart` - after
the loot, and after a spawn gem has tagged the new monster, which the elite roll needs. Code that
needs the final state right after creating a monster calls `MonsterLoot.FlushCreatureStart(npc)`.

- **Elite roll** (#794, outside dungeons, before the gold): see [Elite monsters](elite-monsters.md).

- **Gold**: the `i_gold` marker's amount = `ExperienceReward × goldPermille / 1000` - 750 outside
  dungeons, 900 in them (map planes 2 and 10-15, Erebor's `isDung`). Experience is in Sphere's ×10
  fixed point (`Experience = 180 195`), so a 180-experience monster drops 135 gold.
- **Magic items - PLACEHOLDER**: above 30 gold, a chance of `(gold - 30) / (120000 - 30)`
  (`/ 105000` from 500 gold up), Erebor's `f_creatureStart_giveMIBag`. Magic items aren't designed
  yet, so a hit only logs (`MonsterLoot.DropMagicItemPlaceholder`); Erebor gave an `i_mi_bag`.
- **Rare extras**, gold-scaled: elemental coal (extra chance for fire monsters), ebony crystal,
  ebony bowstring, treasure map.
- `i_nofame` (Erebor's marker on a creature resurrected with Veterinary): no experience, no gold.
  Nothing hands it out in this engine yet, so this is dormant.
- An experience reward between 0 and 10 is raised to 10.
- A **self-resurrected** monster (`e_selfreser`) gets its loot items again but no gold - its marker
  stays at 1, like Erebor's `isResurrectedNPC`.

Not ported (not loot): combat-type tags, spawn bookkeeping.

## Settings: `.settings` -> "Loot" (`LootSettings`)

| Field | Default | Meaning |
|---|---|---|
| `goldPermilleSurface` | 750 | gold per 1000 experience outside dungeons |
| `goldPermilleDungeon` | 900 | the same in dungeons |
| `magicItemMinGold` | 30 | no magic-item chance at or below this gold |
| `magicItemRollMax` | 120000 | magic item when gold > roll of {min .. this} |
| `magicItemRollMaxRich` | 105000 | the roll's top from `magicItemRichGold` up |
| `magicItemRichGold` | 500 | |
| `rareExtras` | true | the gold-scaled rare extras |

Tests: `Tests/MonsterLootFormulaTests.cs` (fast tier - formulas), `Tests/Protocol/MonsterLootTests.cs`
(real `c_orc`/`c_cyclops`/`c_ogre`: gold matches the formula, loot reaches the corpse, nested
templates keep the backpack, no zero-amount stacks), `Tests/Protocol/NpcGearEliteTests.cs`
(`c_h_banker` dressed, everything newbie, nothing on its corpse, `RANDOM_LIGHT` resolved;
`c_alchymista`'s named robe).
