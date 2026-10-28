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
`ITEM=` glued on. Not converted (not loot): vendor `SELL=` and the `ITEMNEWBIE=`/`COLOR=`
clothing of 189 human NPCs (vendors, trainers, quest NPCs) that Erebor keeps in `@NPCRestock` -
a known follow-up.

## 2. `Equip = i_monster_create` - Erebor's `creaturestart`

In Erebor, every monster gets an `i_monster_create` script item whose timer runs `creaturestart`
once after creation. Here the item is no longer created; `NPC.On_Create` runs
`MonsterLoot.ApplyCreatureStart` instead, after the loot:

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

Not ported (not loot): the elite-monster transform, combat-type tags, spawn bookkeeping.

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
templates keep the backpack, no zero-amount stacks).
