# Erebor's quests: a catalogue

Every quest in the Erebor archive (`scripts/questy` in pkorpel/erebor-sphere-archive), what it asks and what it gives, so we can pick which to port, rework or drop (#1235). It isn't a plan; the plan follows from your picks.

Detailed notes per quest (giver, requirements, steps, rewards with archive file:line, quest items, bugs) are in [erebor-notes/](erebor-notes/): A = kvest0001-4, q006-q016, q1, q14; B = q017-q033, q29-q31; C = q034-q100, Halda Molle, Murdoc, Darkmoon, Paserak; D = realm quests, tutorial, labyrinth, events and the journal index.

## What we have already

- Ported: Expert, Master, Grandmaster (journal #38 "Ztracene Dcery"), weekly quests, boosters, RP rank, PvP league, Murdoc's training arena, the quest journal (`.questy`).
- The boss fights of Halda Molle, Darkmoon, the OCS king and the dragons are ported (#1022), but not the quests around them. Fifteen quest-bound monster events are still empty stubs (five e_ocs_*, e_guard_ocs, e_shadow_weapon_brother, e_q040_dragon_death, e_hatlend, e_hm_pruvodce, e_kaloc_help, e_malica_help, e_goblin_treasure, e_q019_pocestny, e_q026_hcnpoisoning_npc).
- Every other quest exists only as converted definitions: its NPCs and items are there, its scripts are commented out. Special items like the robe, ring or hammer have their defs but none of their bonuses.
- Ready for rewards: all six magic item bags (`i_mi_pouch` up to `i_mi_bag_superHigh`), boosters, mount powers (#925, the black mustang), pet figurines, magic item properties, the four dragon potions (`DragonPotions.cs`).

## What every quest has in common

- **No spots.** Every quest giver, prop and quest spawn was placed by a GM on the live server. The archive's save holds globals, the admin character, the start stone and the tutorial's gatekeeper, no quest NPC. A few scripts have coordinates in comments. Each quest we port needs its spots chosen and placed through `.SetupWorld`.
- **The corpse collecting bag is missing.** About 20 quests give a `t_collector` bag: carve a part off a kind of monster and it goes into the bag, which counts them (gq012, q006, q010, q011, q013, q015, q017, q024, q029, q035, q036, q037, q040, q075, q099, q1, q29, q30, q31 and the tutorial's necromancer). We have only an empty `t_collector` typedef that keeps the converter quiet. It's one mechanic, built once, and it would carry most of the "kill N" quests.
- **Quest items become tags** on the player, not items (our rule, [README](README.md)).
- **Many scripts were broken on Erebor itself:** unstartable, unfinishable, exploitable, rewards commented out. They're marked below; "works" means the script runs start to end as written.
- **Most quests are fetch and kill-N.** The few with a different shape: q025 Palirna (sabotage or repair, five endings), q016 (a moral choice weighted by realm), gq058/mq057 (timed infiltration in the evening hours, server-wide announcements), q017 (a timed dungeon run with a lockout), the Goblin Labyrinth (a daily puzzle run that scales), q039 (a long crafting chase for a bound weapon).

## Special rewards

Rewards beyond exp and gold. The first group is what you asked about: items that exist nowhere else.

### Unique and special items

| Quest | Reward | What it does | Cost to get | On Erebor |
|---|---|---|---|---|
| q017 Alchymista Terlak | one magic item for your profession, 9 kinds | profession and level 35 locked, one worn at a time. Warrior: obsidian heater shield, 10% to block a melee hit, 5% a hostile spell. Mage: wizard's hat +10 Magery +10 Int. Necro: robe +5 Eval Int +5 Healing +10 Magery. Ranger: gloves +5 Hiding +10 Tactics +10 Archery. Craftsman: adamantine pickaxe +10 Mining. Mystic: necklace +15 mana regen +10 Wrestling. Priest, thief, shaman items in the notes | needs q007 past stage 6, level 35; a timed dungeon run behind a gate that wants red and blue gate crystals (1+1, then 2+2, 4+4...), 4 h lockout per run | works, but nothing makes the crystals and the exit gate's target is a placeholder. Every run after the first also costs 200 deer eyes and 10,000 gold for the protection potion. A collector pays a 50,000 cheque only for older versions of these items, so the current 9 can't be sold |
| q039 Stinove zbrane | a shadow weapon for your profession | obsidian, soul-bound: +20 to the class's weapon skill and +10 Tactics (casters +10 Magery +10 Eval Int, the mystic +20 Wrestling +10 Eval Int), +10% against five monster families, about x2 damage against monsters; 15 lives, one lost per death of its owner; repairable at a cost to its maximum; comes back to its owner if moved | level 41; boss skulls, timed kills of traitors, rare materials incl. "Slza hor" from q040 blazen, an hour of crafting | the gem's quest is broken, so the weapon couldn't be finished. The warrior's longsword gives Fencing, which is right: on Erebor one-handed swords use Fencing |
| q30 Zmocnenec Gondoru / Mordoru | "Plast bojovnika" cloak | +10 hit regen, +20% damage against dragons, only its owner wears it, practically never breaks | 14 stages of collecting monster parts per realm, 54,650 exp in all; the last stage repeats for another cloak (8, then 12 dragon hearts) | works (needs `t_collector`) |
| q006 Erioluv vnuk | Robe of the Alchemist | +10.0 Alchemy, owner-bound, only for a GM alchemist; a lost robe can be bought again for 2,000,000 | 24 stages of fetch, talk and craft | works |
| q009 Mistr krejci | Tailor's Ring | +10.0 Tailoring, owner-bound, only for a GM tailor | a 5,000,000 cheque and a wait of about 2.4 h, meant as a day (Erebor's clock counts tenths of a second); each early visit adds 18 min | works |
| q007c Adamantinove kladivko | adamantine smith's hammer | +10.0 Blacksmithing for a GM smith: reaches the 110.0 mithril armour needs | three rare stones (shared with q007b, from a GM treasure only) | works if a GM hands out the stones |
| q007b Kniha inskripce | the Inscription book, or the magic pen | the book is required to inscribe 8th-circle scrolls; traded to the drunk Divous it becomes a pen that turns a blank rune into a recall rune (Inscription 85). One or the other | the same three stones | works; note our port dropped the book requirement from 8th-circle scrolls |
| q026 Otrava z hrobek | the tomb poison cure | permanent immunity to the HCN tombs' curse | a long investigation after being cursed by the tomb wraiths | works (stage 5 is skipped, debug messages left in); the curse itself isn't ported |
| q040 kovar | dragon armour and the dragonslayer weapon (bought) | armour up to 56% dragon breath resist for a full set; weapon +20 skill, +30% against dragons | a boss skull, then a paid crafting service | the armour works; the weapon can't be had: it needs a gold dragon scale, and nothing drops one |
| q040 alchymista | a shop of 4 dragon potions | e.g. +10% dragon resist for 300 s | 120 elixirs | works |

### Skill and stat gains

| Quest | Reward | On Erebor |
|---|---|---|
| q018 Zlaty losos | +3.0 Tailoring for good (and 3,000 exp, 20,000 gold) | the spinning wheel never checks the quest: anyone reaching it can repeat it for 1,500 exp and +3.0 each time |
| kvest0003 Golden Sheaf | a ranger's Taming rises by a tenth of the gap to 130.0 (100 to 103) | fragment; a non-ranger repeats the ending for 500 exp |
| kvest0001 (side prop) | `i_bottle_rostok_muznosti`: +10 Str, -30 Int for good, from a GM-placed cauldron | not a quest reward; the cauldron refills any empty bottle, so it can be drunk again and again |

### Mounts and pets

| Quest | Reward | On Erebor |
|---|---|---|
| q008 Black Mustang / tameblack | a black mustang to tame, rangers with Taming and Animal Lore 95: a magic mount, +20% against demons and undead, animals and spiders, humanoids, +5 Hiding and +10 Stealth while riding | works, repeatable; mount powers already ported |
| q31 Kuchar Mrkvicka | llama figurine (a rideable llama) for 50 dead birds | works (needs `t_collector`); wild llamas exist |
| mq043 / gq043 Murdocovi ucni | llama figurine with a magic bag | works |

### Status changes

| Quest | Reward | On Erebor |
|---|---|---|
| q041 Paserak + zmodrani gate | change realm, Gondor to Mordor or back (kills and karma reset) | works; costs 10k + level x 10k + 1M gold + 1-10 magic bags; Gondor players murder an innocent elf on the way |
| q099 Vyvrhel | an outcast gets their realm back | unstartable: nothing sets `tag.vykoupeni` |
| tutorial exit (start_dung) | leaving the tutorial at level 3+ gives Expert rank and 15,000 gold | works; our new characters follow newbie_portals.scp instead (2,000 gold) |
| Christmas 2016 | RP rank up 1-3 steps for the event | key items commented out on Erebor itself |
| q040 main chain | entry to the dragonslayers' fortress and the dragon lair portals, 49,000 exp | works |

### Random magic item bags (already ported as items)

| Quest | Bags |
|---|---|
| gq058 Zaskodnice / mq057 Zaskodnik | 10 items: 8 x `i_mi_pouch` over the stages, 2 x `i_mi_bag_high` at the end, a +25% exp booster for 2 h, about 109,550 gold a series, every 4 days. This is the booster source our boosters are missing |
| q044 Zachrante Murdoca | 7 bags (3 pouches, 2 bags, a high pouch, a superhigh pouch), 24,100 exp, 150,000 gold |
| q14 Jeskyne poznani | 4 x `i_mi_bag`, 5,000 exp, 80,000 gold |
| q037 Opily Zarik | `i_mi_bag` + `i_mi_bag_high` (level 90, after q036, costs 57k gold) |
| q040 rakchin | `i_mi_bag_superHigh`, 10,000 exp: unreachable, no gold dragon exists |
| Goblin Labyrinth | from the 6th run a chance of `i_mi_bag` (20% + 0.4% a run, up to double for precision), daily |
| q013, q1, q035, q042 Hatlend, mq043/gq043 | one `i_mi_bag` each |
| Halda Molle (Kaloc) | `i_mi_bag` every 12 h after the Nameless dies |
| Christmas / Easter 2016 | `i_mi_bag` for 1,000 baubles or eggs, a treasure map for 2,500 |

## The full list

### The quest journal (59 entries)

Size: S small, M medium, L large. Realm: G Gondor, M Mordor, both.

| # | Quest | Realm | Shape, size | Reward | On Erebor |
|---|---|---|---|---|---|
| 0 | Zemedelec Borek (farmer Borek), gq002/mq002 | G / M | fetch, craft service, S | gold, exp | works |
| 1 | Alchymistka Mikkena (alchemist), gq001 | both | fetch, delivery, S | gold, exp | no-realm and outcast characters repeat step 1 for 500 gold |
| 2-3 | Zmocnenec Gondoru / Mordoru (envoy), q30 | G / M | kill-collect ladder, 14 stages, L | **Plast bojovnika** cloak, 54,650 exp | works |
| 4 | Kuchar Mrkvicka (the cook), q31 | per realm | kill-collect, S | **llama figurine** | works |
| 5 | Lecitel Ceren (lava lizard), q1 | both | exploration, kill-collect, S | magic bag, 2,000 exp | works |
| 6 | Haradirovo selatko (Haradir's piglet), q007 | both | talk, fetch, boss, M-L | 1,000 exp; opens #7, #8 and Terlak | works |
| 7 | Kniha inskripce (inscription book), q007 | both | fetch, S | **book or magic pen** | works |
| 8 | Adamantinove kladivko (adamantine hammer), q007 | both | fetch, S | **+10 Blacksmithing hammer** | works |
| 9 | Mastickar Gregoriel (ointment maker), q010 | both | kill-collect, S | 150 exp, 1,500 gold | works |
| 10-11 | Prekupnik Hruon / Akranon (the fence), q011 | G / M | 3 chained kill-collects, M, repeatable | 1,200 exp, 14,000 gold a run | works; Gondor completions miss the quest statistics only |
| 12 | Experimentator, q013 | G / M | kill-collect, S | magic bag | works |
| 13 | Paradnice Minny (the show-off), q015 | not M | kill-collect, S | 1,000 exp, 2,500 gold | works |
| 14 | Prastary poklad (ancient treasure), q016 | both | talk, exploration, a moral choice, M | up to 1,950 exp, 50,000 gold; truth pays Mordor more, a lie pays Gondor more | works; the refuse button on the in-progress page kills the player |
| 15 | Hobitin (Ambra's manure), q100 | both | fetch, S, newbie | 30 exp, an onion | works |
| 16 | Kovar Gorug (smith Gorug), gq002/mq002 | G / M | kill, fetch, S | food, reagents | works |
| 17 | Kouzelna dyka (magic dagger), q023 | both | fetch, delivery, M | 15,000 gold, 1,200 exp | works |
| 18 | Alchymista Terlak, q017 | both | fetch, timed dungeon run, L, repeatable | **profession magic item** | works, with gaps (above) |
| 19 | Carodejka (the witch), q020 | both | fetch x3, craft, kill, L | 21,000 gold, 1,125 exp, a potion bag, a flask that does nothing | works |
| 20 | Remeslnik Navin (craftsman), q021 | both | fetch, craft, S | 500 gold, 100 exp | works |
| 21 | Transport, q022 | both | delivery chain, L | 20,000 gold, 1,750 exp | works; the ingot heap shortcut is dead (Gorug's ingots work) |
| 22 | Erioluv vnuk (Eriol's grandson), q006 | both | fetch, talk, craft, 24 stages, L | **Robe of the Alchemist** | works |
| 23-29 | Tutorial: Odlier, Mirana, Beorgrad, Grarad, Broket, Necromancer, Imro, nb001-nb007 | tutorial | fetch, craft, gather, one boss, S each | tools; the exit gives Expert rank at level 3+ | works; the fish hand-in takes up to 20 |
| 30 | Vyvrhel (outcast), q099 | outcasts | kill-collect, 250k cheque, M | **realm restored** | unstartable |
| 31 | Zlaty losos (golden salmon), q018 | both | delivery, fetch, M | **+3.0 Tailoring**, 3,000 exp, 20,000 gold | repeatable by exploit |
| 32 | Potulny pocestny (wandering traveller), q019 | both | find a roaming NPC, S | 100 exp | works |
| 33 | Volani more (call of the sea), q033 | both | delivery, one boss, M | 15,000 gold, 1,250 exp; you hand over an `i_mi_bag` | works |
| 34 | Zasnubni prsten (engagement ring), q024 | both, level 16 | fetch, kill-collect, delivery, L | 20,000 gold, 3,550 exp (costs 50,000) | works; a story quest about a ring, not the wedding ring system you ruled out |
| 35 | Palirna (the distillery), q025 | G | sabotage or repair, five endings, M | up to 5,000 gold, exp by level | works |
| 36 | Otrava z hrobek (tomb poison), q026 | both | curse, investigation, fetch, L | **curse immunity** | works |
| 37 | Umbarska hadka (the Umbar quarrel), mq027 | both, despite "mq" | talk, M | small exp | dead end at stage 19 of 37 |
| 38 | Ztracene Dcery (Grandmaster) | both | | Grandmaster rank | **ported** |
| 39 | Murlay namornik (the sailor), q029 | both | kill-collect, delivery, M | a treasure map, 1,300 exp | works |
| 40 | Predajca Menegil (merchant), mq032 | M | delivery, talk chain, L | gold, exp | works |
| 41 | Herman (beetle hunter), q035 | both, level 6-30 | 9 tiers of heads, a group lair, M | magic bag, 7,650 exp, 92,500 gold | works; a missing egg at the hand-in jails you |
| 42 | Uhrovity Jonas (the family sword), q036 | both, level 15 | talk, delivery, 4 titan lord heads, S-M | 3,000 exp, 15,000 gold (a male character pays Fiflena 30,000 on the way) | works |
| 43 | Opily Zarik (drunk Zarik's night), q037 | both, level 90 | talk, 57k gold, cave, travel, collect, L | 2 magic bags, 12,000 exp | works |
| 44 | Oblast cerne smrti (Black Death, OCS), q038 | both | instanced group dungeon, two branches, a king, L | a starter kit, GM-placed treasure | the city ending could never be finished; the king's fight is ported |
| 45 | Stinove zbrane (shadow weapons), q039 | both, level 41 | boss skulls, timed kills, materials, craft, L | **bound shadow weapon** | couldn't be finished (the gem) |
| 46 | Spolecnost drakobijcu (dragonslayers), q040 | both | puzzle, kills, boss chain, L | fortress and lair access, 49,000 exp | works |
| 47 | Drakobijec alchymista, q040 | both | 120 elixirs, S | **dragon potion shop** | works |
| 48 | Drakobijec Rakchin, q040 | both | gold dragon scale, S | superhigh bag, 10,000 exp | unreachable |
| 49 | Blaznivy drakobijec, q040 | both | boss, Marguth's blood, S | "Slza hor" for q039, 2,000 exp | broken |
| 50 | Drakobijci - brneni (armour), q040 | both | boss skull, paid crafting, M | **dragon armour**; the dragonslayer weapon | armour works; the weapon can't be had (no gold scale) |
| 51 | Paserak (the smuggler), q041 | G / M, level 15 | payments, realm-specific kill or delivery, M | **realm change** | works |
| 52 | Halda Molle: Hatlend, q042 | both, level 25 | 150 rulik berries, S | magic bag, 2,000 exp | works; refuse him twice and he attacks on sight forever |
| 53 | Murdocovi ucni (Murdoc's pupils), q043 tutorial + mq043 + gq043 + Torwin | per realm | fetch, delivery, talk across 5 towns, ~30 stages, L | magic bag, **llama**, 15,000 gold, 3,400 exp (+1,675 Torwin) | works |
| 54 | Zachrante Murdoca (save Murdoc), q044 | both, not craftsmen | 5 invisible chests in 5 dungeons, M | **7 magic bags**, 24,100 exp, 150,000 gold | works |
| 55 | Labyrint Zlateho Goblina (Golden Goblin's labyrinth) | both, level 7 | puzzle run, daily, scaling, M | gold, exp, a chance of a magic bag | works |
| 56 | Lovecka vyveska (hunting board) | per board | kill, daily, S | exp, gold | works; not converted at all |
| 57-58 | Zaskodnik / Zaskodnice (the saboteur), mq057 / gq058 | M / G, level 40, not craftsmen | timed infiltration, 18 stages, 17:00-21:59 only, L, every 4 days | **10 magic items, a booster**, ~109,550 gold | works; witnesses must be GM-linked |

### Outside the journal

| Quest | Realm | Shape, size | Reward | On Erebor |
|---|---|---|---|---|
| kvest0001 Mordor quests 001-005 (Burlug, Reyla and Akhbar, medicine for Liguk) | M | fragments, kill, fetch | 10-400 exp | Burlug pays 10 exp on every greeting; Liguk's medicine is never given |
| kvest0002 Gondor quests 001-005 | G | headers only | none | empty |
| kvest0003 neutral quests (vampire story, Golden Sheaf, Dragon Degran) | both | GM event fragments | **Taming gain**; Degran's "Dragon Sword of Doom" (20,000 damage, +1,000 Str and Dex, invulnerable, breaks after one hit) | GM-run |
| kvest0004 order memberships, Irbis's eggs | both | headers; one unfinished kill-fetch | 500-1,500 exp | Irbis can't start |
| q14 Jeskyne poznani (cave of knowledge) | both | fetch and craft chain incl. growing 50 golden sheaves, L | **4 magic bags**, 5,000 exp, 80,000 gold | works |
| q29 alchnezdar (the bungling alchemist) | both | kill-collect, then GM event content, M | 3 potions | only the first task works without a GM (the second wants a Midnight Elemental, which doesn't exist) |
| gq012 Selka Enrica (the farmer's wife and the voles) | both | kill-collect, S | 50 exp | works (needs `t_collector`) |
| q034 port quests (10 givers) | per giver | each teleports you somewhere with a story | none | no rewards at all; one is a dev template |
| q075 Valecnik Arkan | both | kill, S | 15,000 exp, 15,000 gold | no giver exists |
| Halda Molle dungeon (Malica, Kaloc, guide) | both | rescue, a cooking puzzle, boss, L | Kaloc's bag every 12 h, a gate to a GM area | the boss fight is ported; a cauldron likely always fails at Cooking 90+; eating a turnip kills |
| Darkmoon | both | GM boss event | boss loot | quest part never written; boss ported |
| eq_002 Beh (Frodo's journey) | both | map-wide exploration, L | 1,500 exp | works |
| Beauova hlava (Beau's head, crunch) | both | kill, fetch, S | 1,500 exp | can't be finished (item name typo) |
| Knihy pravdy (books of truth) | both | talk chain, 2 kills, M | 2,000 exp, 50,000 gold in the boss | works |
| Christmas 2016 / Easter 2016 | both | collect and exchange, seasonal | magic bag, treasure map, RP rank (Christmas) | key items commented out |
| eq_001 craft avenger's lair | both | event dungeon, boss guarded by four stone guardians | loot | an event, not a quest |

Not quests: Moria_port_gump (GM travel menu), etheria (a gate puzzle that can never open), zmodrani (the realm change gate of q041), krvave_prizraky (lore NPCs), povysujici_brany (old rank gates, replaced by our rank quests), muina_oronti (empty stub), crunch_vampirismus (a GM event item), arena (ported), vyjmecni, verbatim, qv3_verbatim, enrico, orfeus (GM tools and props), noe (props, a GM maze contest and a necro wand: wands aren't ported), knihy (book texts).
