<!-- Research notes for #1235, written from the Erebor archive (pkorpel/erebor-sphere-archive, scripts/questy). File:line cites are archive paths. Summary: ../erebor-catalogue.md -->

**Corrections after the fact-check (the catalogue has these right):**
- q017: the collector `c_q017_sberatel` buys none of the current 9 items: for 6 he pays only when the name differs from the current one ("Tyto novy nesbiram, jenom stary!"), for war, thief and mystic he looks for old item types. Every run after the first costs 200 deer eyes and 10,000 gold.
- q020: the rewards also include 21,000 gold and 1,125 exp.
- q022: works: Kovar Gorug hands out the ingots and sets stage 3 (gq002_borek.scp:339-340); only the ingot heap shortcut is dead.
- q026: stage 5 is skipped but harmless: Unas accepts stage 4 or 5, the quest can be finished.

# Erebor quests, group B (q017–q033): catalogue

Source: the local clone of `pkorpel/erebor-archive` (Sphere 0.99z8, cp1250). File paths below are relative to `scripts/questy/`, unless they start with `scripts/` or say otherwise. Line numbers are the original file lines.

Shared notes that apply to every quest:
- **No spawn points anywhere.** None of the quest NPCs or quest world items are placed by any script. `save/sphereworld.scp` contains none of them, so GMs placed them all by hand. Coordinates below come only from comments in the `*_chars.scp` files. Region names were looked up in `scripts/spheremap.scp` `[AREA]` rects. "PlanSto" is a catch-all area name in that file.
- **Journal:** progress lives in quest tags (`qtag`) on an invisible `i_quest_rune` (`System/qTag.scp`). The 3rd column of `questMenu[n]` in `System/questmenu.scp` is 1 for qtag and 0 for a plain char tag. `f_quest_complete(n)` only bumps a GM statistics counter on the quest stone (`System/quest_system.scp:1-12`).
- **Magic properties** (`scripts/systems/magicITEMS.scp`): `tag.imi_value[n]`. For n<50 it is skill id n, boosted by value×10 tenths, so `10` means **+10.0 skill points** (`mif_skilladd`, l.71-80). 60/61/62 = +Str/+Dex/+Int (raw). 70/71/72 = HP/stamina/mana regeneration: the status screen shows **2× the value per 10 s** (`scripts/experiences.scp:1088-1094,1173`). 102 = +N % damage (melee and spell) vs dragons (`magicITEMS.scp:318-361`). 131 = N % chance to reflect a melee hit back at the attacker. 134 = N % chance to fully block a melee hit (`scripts/combat.scp:454-480`). 136 = N % chance to stop a hostile spell (`magicITEMS.scp:826-837`). `mif_finishitem` marks the item as identified and names it.
- **"Collector" bags (`t_collector`):** you target a fresh corpse whose name matches `target_id` and get one trophy, with a skill-like success check. Many of these quests use them. The port has no generic collector system (no matches in `moriaengine/scripts`).
- **Port check:** I grepped every quest's NPC/item/tag defnames in `/workspace/moria-final/moriaengine/scripts`. Every group-B file has a converter output in `scripts/converted/questy/*.def`. Those files hold ItemDefs, NPCDefs and `[SpeechStub]`s only; every function, dialog and event is commented out by the converter. None of these quests has C# logic. Exceptions are noted per quest (q026 monsters, q030 envoys).

## Summary table

| id | name | giver | realm | shape / size | special reward? | port status |
|---|---|---|---|---|---|---|
| q017 | Alchymista Terlak (the alchemist Terlak) | Innkeeper `c_hospod` (from q007); gates; buyer `c_q017_sberatel` | both | mixed (fetch + timed dungeon run), large | **YES**: one profession-locked magic item (9 variants, e.g. +10 Magery/+10 Int hat; +10 % melee block/+5 % spell block shield), lvl 35 | converted only |
| q018 | Zlaty losos (the golden salmon) | `c_q018_rybar` Rybar podivin | both | delivery/fetch + teleport rune, medium | **YES**: +3.0 Tailoring skill gain (also 3000 exp, 20k gold) | converted only |
| q019 | Potulny pocestny (the wandering traveller) | `c_q019_pocestny` Pocestny (roams 70 spots) | both | exploration (find NPC), small | no (100 exp) | converted only (`e_q019_pocestny` stub) |
| q020 | Carodejka (the witch) | `c_q020_bard` Bard → `c_q020_roxana` Roxana | both | mixed (fetch ×3, craft via q021, kill), large | no: potion bag + flavour-only flask `i_q020_lahvicka` | converted only |
| q021 | Remeslnik Navin (Navin the craftsman) | `c_q021_remeslnik` Navin | both | fetch/craft, small | no (500 g, 100 exp) | converted only |
| q022 | Transport | `c_q022_velitel_g` Farin / `c_q022_velitel_m` | both (separate givers) | delivery chain, large | no (20k g, 1750 exp) | converted only |
| q023 | Kouzelna dyka (the magic dagger) | `c_q023_ShamanQworet` Qworet | both | fetch/delivery, medium | no (15k g, 1200 exp) | converted only |
| q024 | Zasnubni prsten (the engagement ring) | `c_q024_valecnik_gregor` Gregor | both, lvl >15 | mixed (fetch, kill-collect, deliver), large | no (20k g, 3550 exp; costs 50k g) | converted only |
| q025 | Palirna (the distillery) | `c_q025_hospodska` Eraveth / `c_q025_palic` Miruvil | **Gondor only** | moral-choice puzzle (sabotage vs repair), medium | no (≤5000 g + level-scaled exp) | converted only |
| q026 | Otrava z hrobek (poison from the tombs) | triggered by HCN wraiths; Andor, Unas, Roxana | both | mixed (curse, investigation, exploration, fetch), large | **YES**: permanent immunity to the tomb curse (cure `i_q026_hcnPoison_Cure`) | converted only; HCN tomb monsters ported (#1022), curse not |
| q029 | Murlay namornik (Murlay the sailor) | `c_q029_murlay_namornik` | both | kill-collect + delivery, medium | no: standard treasure map `i_u_treasure_map` + 1300 exp | converted only |
| q29 | alchnezdar (Popletalius, the bungling alchemist) | `c_q029_alchnezdar` | both | kill-collect, then GM-event content, medium (broken) | no (3 potions, all exp commented out) | converted only |
| q30 | Zmocnenec Gondoru / Mordoru (the realm's envoy) | `c_q30_zmocnenecg` / `c_q30_zmocnenecm` | per realm | kill-collect ladder, 14 stages, large | **YES**: "Plast bojovnika" cloak, +10 HP regen, +20 % dmg vs dragons, owner-bound | envoy NPCs ported and placed, but for the Grandmaster rank quest only; the q030 ladder is not ported |
| q31 | Kuchar Mrkvicka (Carrot the cook) | `c_q31_kuchar` / `c_q31_kuchara` | per realm | kill-collect, small | **YES (mount)**: llama figurine `i_pet_llama` | converted only |
| q033 | Volani more (call of the sea) | `c_q033_vetesnikignac` Ignac | both | delivery chain + 1 boss kill, medium | no (15k g, 1250 exp) | converted only |

Potions from issue #1069: **`i_q026_hcnpoison_cure` comes from q026** (this group). `i_lek_pro_liguka` and `i_bottle_rostok_muznosti` both come from `kvest0001.scp`, which is outside this group.

---

## q017: Alchymista Terlak ("Alchemist Terlak")  [designer's reference reward]

1. **Files:** `q017_alchymista_terlak.scp` (779 l.), `q017_alchymista_terlak_chars.scp`. The start dialog lives in another quest's file: `q007_HaradirovoSelatko.scp` (pages 6-12, buttons 8-9, l.786-1182). Journal: `questMenu[18]`, `d_alchymista_terlak` (`System/questmenu.scp:1301-1325`), tags `q017_terlak_ochrana`, `q017_terlak`.
2. **NPCs:**
   - Innkeeper `c_hospod` "Hospodsky" (q007's NPC, on the island that suffered the dryad attack). He tells the Terlak story and brews the protection potion.
   - Boss `c_crunch_zoldak` "Gargamel sberatel lebek" (Gargamel the skull collector), inside the quest dungeon.
   - Buyer `c_q017_sberatel` "Sberatel" (collector). His title is a copy-paste leftover, "Velitel strazi".
   - All are GM-placed. The dungeon destination 5719,111 lies in area "Jamy smrti level 1" (Pits of Death).
3. **Who:**
   - Needs q007 Haradirovo selatko past stage 6 before the innkeeper offers it.
   - Needs **level ≥ 35** to pass the entry gate (`q017_alchymista_terlak.scp:47-51`). Any profession; both realms.
   - **Repeatable**, with rising cost. After each completion `q017_terlak` += 2, and the gate wants `q017_terlak` red **and** blue crystals: 1+1 the first time, then 2+2, 4+4, 6+6…
   - Gate lockout: the gate stores `posledni_vstup`, and nobody may enter within **4 h** (144000 ds) of the last entry, because "another hero is doing the tasks" (l.52-57). Bug: only repeat entrants set the timestamp (l.74); first-timers don't (l.81-102).
4. **Story / steps:**
   - The innkeeper reminisces about his alchemist friend Terlak. A drunk boasted of a meeting with Terlak in caves north of Belfalas bay, hired the mercenary Gargamel, and played with "gate crystals". The innkeeper brews you a "body-hardening" potion that absorbs Terlak's guardian spell. The **first potion is free**, +200 exp (`q007_HaradirovoSelatko.scp:1171-1175`).
   - Every later potion (one is used up per entry) costs **200 deer eyes, each from a different deer** (collector `i_q017_kolektor_srnky`, target "Body of Srnec") **plus 10 000 gold**, +200 exp (`q007…:1157-1166`, collector `q017…:177-199`).
   - Get the red and blue gate crystals (`i_scout_crystal_red`, `i_scout_crystal_blue`). **No script creates them.** The only two in the archive sit in a container in `scripts/temporary_arjan.scp`, a save dump. They must have been GM- or event-supplied.
   - Find the entry gate `i_scout_vstupni_gate` ("Only for the chosen…!"). After every entry it **jumps to a random one of 4 dungeon spots**: Etheria L2 5286,730; Snagum L1 4988,451; Arachnon L1 478,1524; Mount Dorlas L2 5219,170 (l.75-80).
   - Step on the gate. Without the potion you take 24-30 god damage ("a wave of energy tears your body") and are refused (l.62-64, 85-87). With it, the potion and crystals are used up and you go to 5719,111.
   - Inside you get a **4-hour timer** `i_alchymistuv_timer` (14400 s, countdown message every 30 s, l.116-143). Time out and you are moved to 1350,3027. Die and you fail, `e_scout_alchymista` ports your ghost to 1350,3027 (l.109-114).
   - Gargamel guards the place. Entering resets the gate's linked spawn so he is up for the next player (l.72). Killing him is **not** checked.
   - Reach the exit gate `i_scout_vystupni_gate` ("Reward or death…?"). Terlak's freed spirit hands out the profession reward (l.155-174).
5. **Rewards** (exit gate `q017…:155-174`; `src.<profession>ova_odmena`). No exp or gold at the exit; only the 200 exp per potion from the innkeeper. Every reward is **SPECIAL/MAGICAL**, colour 0531, obsidian/adamantine base. Shared rules, from the typedefs at l.218-515:
   - **Class-locked:** worn by the wrong profession it is force-unequipped (`f_svlikaci_pauza`).
   - **Level ≥ 35**, or it is force-unequipped.
   - **Only one Terlak item worn at a time**: equipping a second bounces the first (tag `terlak_item`).
   - It gets `craftedby` = wearer, so the magic-item exchange (`isrealmagicitem`) doesn't treat it as loot.

   | profession | item (base defname) | name | effect | lines |
   |---|---|---|---|---|
   | war | `i_obsidian_heater_shield` | Stit obrance (Defender's shield) | **10 % chance to fully block a melee hit, 5 % chance to stop a hostile spell** | 205-216 |
   | mag | `i_hat_wizards` | Klobouk maga (Mage's hat) | **+10 Magery, +10 Int** | 240-248 |
   | priest | `i_obsidian_heater_shield` | Stit lecitele (Healer's shield) | **+10 Healing, +5 stamina regen** | 271-279 |
   | necro | `i_robe` | Roba temneho (Robe of the dark one) | **+5 Eval Int, +5 Healing, +10 Magery**, durability 350-450 | 303-313 |
   | thief | `i_obsidian_metal_shield` | Stit neplechy (Shield of mischief) | **5 % melee reflect, +15 Magic Resist** | 337-349 |
   | mystik | `i_necklace_gold` | Nahrdelnik mysli (Necklace of the mind) | **+15 mana regen (shown +30/10 s), +10 Wrestling** | 374-386 |
   | craft | `i_pickaxe` made of adamantinum | Adamantiovy krumpac (Adamantine pickaxe) | **+10 Mining**, adamantine durability, hits 400/400 (`more1L/H`) | 409-419 |
   | ranger | `i_obsidian_CHAINMAIL_gloves` | Rukavice presneho zasahu (Gloves of the true shot) | **+5 Hiding, +10 Tactics, +10 Archery** | 446-455 |
   | shaman | `i_obsidian_RINGMAIL_leggings` | Voodoo kalhoty (Voodoo trousers) | **+10 Healing, +10 mana regen** | 482-490 |

   Commented-out older versions: war got an obsidian double axe with +15 (2H+tactics); thief got an obsidian scimitar with +15 Dex/+10 Tactics/+10 Fencing; mystic got a cape with +10 Magery/+15 Wrestling.
   - **Sell-back:** the Sberatel (`spk_q017_sberatel`, l.518-778) buys a Terlak item for a **cashier's cheque of 50 000 gold** (`i_cashiers_check more1=50000`). He also lowers `q017_terlak` by 2, which makes the next run cheaper.
6. **Quest-only items/monsters:**
   - The 2 crystals, both gates, `i_alchymistuv_timer`, deer-eye collector, protection potion (qtag only, no item).
   - Gargamel `c_crunch_zoldak`: STR 3500, maxhits 3500, Wrestling 120, Fencing/Tactics 100, Poisoning 250, valorite plate, bardiche. Resists: poison 1000, para 560, summon 1000, bleed 800, fire/elect 400, cold 200. tag.experience 150-200. Attacks on sight.
7. **Engine systems:** linked dialog in another quest (q007), collector bag, gate items with step triggers and random relocation, a single-occupancy instance with a global 4 h lockout, a per-player countdown timer, death hook, profession-locked magic items, cashier's cheques.
8. **Shape:** mixed (fetch + timed dungeon run + repeatable artifact grind), **large**.
9. **Broken/odd:**
   - The Sberatel's buy-back looks for the **old** items: war hammer/double axe for war, `i_scimitar_obsidian` for thief, `i_cape` for mystic (l.531-568, 651-695). War, thief and mystic therefore can **never sell** their current reward.
   - The exit gate default `morep=1000 1000` is marked "set to the dungeon exit!" (l.153). The placed instance must have been edited by a GM.
   - Crystal source missing. First-time entries don't set the lockout.
- **Port:** converted only (`converted/questy/q017_alchymista_terlak*.def`: crystals, gates, timer, collector, both NPCs, SpeechStub). Every reward function and typedef is commented out. q007, the starting quest, isn't ported either.

---

## q018: Zlaty losos ("The golden salmon")

1. **Files:** `q018_zlaty_losos.scp`, `_chars.scp`, `_items.scp`. Journal `questMenu[31]` `d_questy_zlaty_losos`, qtag `q018`.
2. **NPCs** (coords from chars comments):
   - `c_q018_rybar` "Rybar podivin" (the odd fisherman), 3916,3609, area Nurnen.
   - `c_q018_Mirrana` "Mirrana", 3014,2467, area Mrtve mocaly (Dead Marshes); the dialog says "on the road from the bridge between the Minases toward Rhun".
   - `c_q018_elnassir` "Elnassir mastickar" (the ointment seller), 2462,3324, area Ebenove ostrovy (Ebony islands).
   - All GM-placed.
3. **Who:** anyone, no level check, once (stage 6 is final).
4. **Steps:**
   - The fisherman can't catch a giant salmon and wants golden thread for a lure. He gives you a small fish (`i_fish_small`, item 0dd6, `q018_zlaty_losos.scp:100`).
   - Mirrana knows where the magic spinning wheel is, but first wants a cure for a festering lizard bite.
   - Elnassir brews it for **20 spider silk, 80 nightshade, 150 bone and 10 000 gold** (l.302-312) and gives you `i_q018_mirranin_odvar`.
   - Hand the brew to Mirrana: +500 exp (l.208-213). She reveals a golden rune hidden in the ruins on the Mordor side at the Minas bridge (the journal says Osgiliath).
   - Use the rune `i_q018_portujici_runa` (works only at stage 4): you are teleported to 5390,35 (inside "Jamy smrti level 1"). The script comment says it "will probably be guarded", but no guards are scripted.
   - Double-click the wheel `i_q018_zlate_kolo`: you get the thread, +1500 exp and **+3.0 Tailoring**, then you are moved to 6008,3860 (Osgiliath east bank) (`_items.scp:19-27`).
   - Give the thread to the fisherman: +1000 exp, +20 000 gold (`q018_zlaty_losos.scp:104-111`).
5. **Rewards:** 3000 exp total and 20 000 gold (net +10 000 after Elnassir's fee).
   - **SPECIAL: +3.0 Tailoring** (a permanent skill gain, `_items.scp:26`).
   - Small fish: plain.
   - Golden thread: quest item, handed back.
6. **Quest items:** `i_q018_zlata_snura`, `i_q018_zlate_kolo`, `i_q018_portujici_runa`, `i_q018_mirranin_odvar`.
7. **Systems:** gumps, placed world items, teleport, skill gain.
8. **Shape:** delivery/fetch chain, **medium**.
9. **Broken:** the wheel has **no stage check**. Anyone who reaches 5390,35 (it sits in a dungeon area) can double-click it again for +1500 exp and +3.0 Tailoring each time, and it resets the tag to 5.
- **Port:** converted only.

---

## q019: Potulny pocestny ("The wandering traveller")

1. **Files:** `q019_pocestny.scp`, `_chars.scp`. Journal `questMenu[32]`, qtag `q019_pocestny`. The header notes the live server's spawn UID #040008794.
2. **NPC:** `c_q019_pocestny` "Pocestny", a tough mage (10-11k HP, Magery 120, mass curse/dispel, flamestrike…). He sits on a spawn that **teleports itself** to one of 70 hard-coded spots: 30 Gondor, 30 Mordor, 10 neutral (`f_q019_spawnPort`, l.181-256). He moves after each quest hand-in and on death.
3. **Who:** anyone. Repeatable whenever you find him again: the tag is set once, but the exp is paid every time.
4. **Steps:**
   - Find the old tramp. He flatters you with crude jokes and asks for a recall scroll so he can get home.
   - Give 1 `i_scroll_recall`: he casts "Heru Morgul Kel", is paralysed 4 s and vanishes to a new random spot (`i_q019_portMe`, l.158-175).
5. **Rewards:** 100 exp, quest-complete counter (l.140-149). No items.
6. **Quest items:** `i_q019_portMe` (an internal timer).
7. **Systems:** self-relocating spawn, gump.
8. **Shape:** exploration (find a roaming NPC), **small**.
9. **Odd:** the random line pool includes a gendered sex joke; the dialog text is crude in places. Insulting him (two of three answers) ends the conversation, but the quest stays retryable.
- **Port:** converted only. `e_q019_pocestny` exists only as a `PassiveAbilityDef` stub (`Basic/AbilitiesAndTalents/teventStubs.scp:530`).

---

## q020: Carodejka ("The witch")

1. **Files:** `q020_carodejka.scp`, `_chars.scp`, `_items.scp`. Stages 6-8 run through q021's NPC Navin (`q021_Remeslnik.scp:142-156, 259-274`). Journal `questMenu[19]`, qtag `q020`.
2. **NPCs:**
   - `c_q020_bard` "Bard": Mordor copy at 3618,2756 (Barad-dur), Gondor copy at 5564,3899 (Minas Tirith).
   - `c_q020_roxana` "Roxana, stara carodejnice" (the old witch), 2913,1988 (Temny hvozd / Mirkwood, south). She also hosts q022 and q026 dialog hooks.
   - `c_q021_remeslnik` Navin (see q021).
   - Monster `c_q020_lich` "Nemrtvy carodej" (undead sorcerer) on the Paths of the Dead.
   - All GM-placed.
3. **Who:** both realms. Stage 4 requires level ≥ 5 (`q020…:218`); the final task requires level ≥ 6 (l.141). Once.
4. **Steps:**
   - The bard tells Roxana's tragic backstory (sister killed in an experiment, face burned, exiled to Mirkwood) → tag 1.
   - Winter food: **100 cooked ribs, 100 cooked fish, 30 cooked birds, 50 onions, 50 carrots** → 1500 g, 75 exp (l.280-288).
   - Firewood and comfort: **100 copper ("spruce") logs, 500 feathers, 10 brown-bear furs** (`i_fur_dark`) → 2000 g, 100 exp (l.290-297).
   - Herbs, from a list item `i_q020_seznam`: **100 mandrake, 100 garlic, 50 ginseng, 25 dead wood, 25 serpent scale** → 4000 g, 150 exp (l.306-316). Her mortar then cracks.
   - Navin forges a new mortar, but needs rare ore. He lends the dwarven pickaxe `i_q020_krumpac`.
   - In the old dwarf mines NW of Erebor (Grey Mountains, "shaft 3"), lever the boulder `i_q020_kamen` with the pickaxe: after 10 s you are moved to 5566,575 (area "Sluje").
   - Mine the vein `i_q020_zila`: you get `i_q020_ruda` and the pickaxe breaks. Navin turns the ore into `i_q020_hmozdir`.
   - Bring the mortar to Roxana → potion bag + 6000 g + 350 exp (l.318-324).
   - Kill the undead sorcerer and carve out his heart (`RESOURCES=i_q020_srdce`) → flask + 7500 g + 450 exp, quest complete (l.328-335).
5. **Rewards:** 21 000 gold, 1125 exp total.
   - `tmp_q020_odmena`: a bag of **5 Greater Heal, 10 Heal, 10 Cure potions** (`_items.scp:208-238`). Plain stock.
   - `i_q020_lahvicka` "Lahvicka s podivnou mlhou" (flask of strange mist, `_items.scp:187-199`). **Flavour only**: double-clicking shows "a tiny bone dragon appears in the mist for a moment". It has no function, so it is a unique keepsake, not a magic item.
6. **Quest items/monsters:**
   - Items: `i_q020_seznam`, `i_q020_krumpac`, `i_q020_kamen`, `i_delay_carodejka`, `i_q020_zila`, `i_q020_ruda`, `i_q020_hmozdir`, `i_q020_srdce`.
   - Lich `c_q020_lich`: 800-1200 HP, Magery 200, Parry/Mace 85-100, spells harm/fireball/e-bolt/lightning/para/reflect/flamestrike, exp 60-90.
   - Test templates `tmp_q020_jidlo/drevo/regy` are GM test kits.
7. **Systems:** new-style quest dialog framework (`questDialog_*`), placed world items, carve resource, delayed teleport, cross-quest NPC.
8. **Shape:** mixed (3× bulk fetch, craft errand, boss kill), **large**.
9. **Broken:** nothing major. The journal says "spruce logs"; the code takes `i_log_copper`.
- **Port:** converted only.

---

## q021: Remeslnik Navin ("Navin the craftsman")

1. **Files:** `q021_Remeslnik.scp`, `_chars.scp`, `_items.scp`. Journal `questMenu[20]`, qtag `q021`.
2. **NPCs:**
   - `c_q021_remeslnik` "Navin, stary remeslnik" (old craftsman), 3578,1061, near Erebor. He is also the hub for q020 and q022.
   - `c_q021_lecitel` "Horad", healer and vendor, 3099,877, "wanders above Erebor".
   - Animal `c_q021_koza` "Tlusta koza" (fat goat).
   - All GM-placed.
3. **Who:** anyone, once.
4. **Steps:**
   - Navin's back hurts. He gives you a recipe `i_q021_recept`.
   - Pick 10 healing herbs `i_q021_bylinka` from placed bushes `i_q021_kerik`: 2 per bush, refill after 9 min, and the herb vanishes if dropped.
   - Carve goat fat `i_q021_sadlo` from a fat goat (its `RESOURCES`).
   - Horad mixes the ointment `i_q021_mast`.
   - Bring it to Navin → 500 g, 100 exp (`q021…:245-256`).
5. **Rewards:** 500 gold, 100 exp. No items.
6. **Quest items:** recipe, herb, bush, fat, ointment; the goat NPC.
7. **Systems:** placed harvest nodes with regrow, carve resources, vendor/healer NPC.
8. **Shape:** fetch/craft, **small**.
9. **Broken:** `spk_q021_roxana` calls an undefined `f_q021_roxana_zaklad` and is attached to no NPC (dead code, l.401-407).
- **Port:** converted only.

---

## q022: Transport

1. **Files:** `q022_Transport.scp`, `_chars.scp`, `_items.scp`. Also pieces in `q021_Remeslnik.scp` (Navin), `q020_carodejka.scp:117-120,262-268,337-338` (Roxana's spice), `gq002_borek.scp:194,267-271,338-340` and `mq002_borek.scp:199,284-288,357-359` (Gorug's ingots). Journal `questMenu[21]`, qtag `q022`.
2. **NPCs:**
   - Commander `c_q022_velitel_g` "Farin" (Minas Tirith, no coords) or `c_q022_velitel_m` "Velitel strazi" (guard commander), 3681,2689,-50 (Barad-dur).
   - Navin (q021).
   - Smith Gorug (`gq002`/`mq002` files): Gondor hint "went to the hobbits", Mordor hint "Rhun".
   - Carpenter `c_q022_truhlar` "Miro", 2604,2163 (Vychodni Marka, west of Edoras).
   - Miner `c_q022_hornik` "Valduk", 1231,910 (Osamely dul, the Lonely Mine).
   - Cook `c_q022_kuchar` "Jindra Jecminek", a hobbit, 1650,1187 (Hurecko).
   - Roxana (q020).
   - Beehives `i_q022_vcely` in the woods near "Radovsko".
   - All GM-placed.
3. **Who:** both realms, level ≥ 7 (`q022…:68`). Once.
4. **Steps:**
   - The commander sends you to fetch the recruits' weapon order from Navin. Navin is behind and short of materials.
   - Get 150 iron ingots from Gorug: free, `i_q022_ingoty`.
   - Get spear shafts `i_q022_ratiste` from Miro for **100 iron ("chestnut") logs**.
   - Get coal from Valduk, who wants a keg of Jindra's mead.
   - Jindra brews `i_q022_medovina` from **3 forest honey** (`i_q022_med`, from the beehives), **spice from Roxana** (`i_q022_koreni`), **1 empty keg** (`i_keg_potion`) and **1000 gold** (l.443-458).
   - Mead to Valduk → coal `i_q022_uhli`. Coal to Navin → weapon crate `i_q022_bedna`.
   - Crate to the commander → 20 000 g, 1750 exp (`q022…:110-121`).
5. **Rewards:** 20 000 gold, 1750 exp. No items.
6. **Quest items:** heap of ingots `i_q022_hromada`, ingots, shafts, beehive, honey, spice, mead keg, coal, crate. All except the honey are move-never or newbie.
7. **Systems:** many NPCs across both realms, placed harvest nodes, gumps, realm-split hints.
8. **Shape:** delivery chain (7 NPCs), **large**.
9. **Broken:**
   - The placed ingot heap `i_q022_hromada` lets non-Gondor players "steal" ingots at stage 2 but **doesn't set stage 3**, so Navin won't take them (dead branch, `_items.scp:16-25`).
   - The cook checks an undefined `i_q22_med` ("old honey", l.380, 408).
   - The commander silently removes a leftover crate at stage 13 (bug-fix comment, l.18-20).
- **Port:** converted only.

---

## q023: Kouzelna dyka ("The magic dagger")

1. **Files:** `q023_kouzelna_dyka.scp`, `_chars.scp`, `_items.scp`. Journal `questMenu[17]`, qtag `q023`.
2. **NPCs:**
   - `c_q023_ShamanQworet` "Shaman Qworet" (no coords). He also hosts a q006 hook (l.36-39, 144-146).
   - `c_q023_CarodejUnas` "Carodej Unas" (Unas the wizard), "around the great swamp north of Rhun". He also hosts q006 and q026 hooks.
   - Placed item: ice elemental remains `i_q023_Pozustatky` at 1560,312 (area Severni Spoust, the Northern Waste). A GM links it to a guardian spawn.
   - All GM-placed.
3. **Who:** anyone, once.
4. **Steps:**
   - Qworet lost his dagger in an ice elemental's body in the Northern Waste. He gives you a sealed letter `i_q023_List` for Unas.
   - Unas casts a protective spell on you (stage 2).
   - At the remains you get the frozen dagger `i_q023_Kouzelna_Dyka_Z`. This is blocked while the linked guardian spawn has a live monster (`link.more2`).
   - Qworet says it froze, go back to Unas. Unas wants a chunk of the remains: get `i_q023_Pozustatky_Kus` from the same body.
   - Unas thaws the dagger → `i_q023_Kouzelna_Dyka`, +5000 g, +400 exp (l.267-275).
   - Return the dagger to Qworet → 10 000 g, 800 exp (l.136-142).
5. **Rewards:** 15 000 gold, 1200 exp. No items; the dagger goes back to Qworet.
6. **Quest items:** letter, frozen dagger, dagger, remains, chunk. No dedicated monster; the guardian is whatever spawn the GM links.
7. **Systems:** GM-linkable guarded object (target a spawn), gumps (old style + framework).
8. **Shape:** fetch/delivery, **medium**.
9. **Broken:** the "touching the dagger will kill you" warning is flavour only; nothing enforces it.
- **Port:** converted only.

---

## q024: Zasnubni prsten ("The engagement ring")

1. **Files:** `q024_zasnubni_prsten.scp`, `_chars.scp`, `_items.scp`. Journal `questMenu[34]`, qtag `q024`. Fiflena also hosts `q036_jonas` hooks.
2. **NPCs** (all GM-placed):
   - `c_q024_valecnik_gregor` "Valecnik Gregor" (Gregor the warrior).
   - `c_q024_zlodej_boris` "zlodejicek Boris" (Boris the petty thief), "near some elven town".
   - `c_q024_klenotnik` "Klenotnik Hugo" (Hugo the jeweller), "at the fountain on the Island of Emperors".
   - `c_q024_Fiflena` "Fiflena, nevesta" (the bride), "sulking on the Rhun island".
   - Treasure chest `i_q024_poklad` in "underground complexes" (the journal names them; unclear).
3. **Who:** level > 15 (l.517). Both realms. Once.
4. **Steps:**
   - Gregor's partner ran ahead to loot a treasure. Find the odd-coloured chest: you get a locked little box `i_q024_truhlicka`. Back at Gregor: +100 exp.
   - Boris knows the lock but wants **2 Guardian arms**: collector `i_q024_vacek_strazci`, target "Body of Strazce" = `c_minotaur_strazce`. Done: +800 exp.
   - Boris then wants **120 lockpicks**. He opens the box: ruby `i_q024_sutr`, +300 exp.
   - Ruby to Gregor: +500 exp. Then to Hugo, who makes the ring for **50 000 gold**: +400 exp, `i_q024_prsten`.
   - Ring to Gregor: +500 exp. Gregor sends you to propose on his behalf.
   - Fiflena grudgingly accepts the ring: +150 exp.
   - Report to Gregor: **+800 exp, +20 000 gold** (l.656-663).
5. **Rewards:** 3550 exp, 20 000 gold. Net gold −30 000, plus 120 lockpicks spent. No items.
6. **Quest items:** collector, chest, box, ruby, ring.
7. **Systems:** collector bag, placed chest, gumps.
8. **Shape:** mixed (find, kill-collect, fetch, deliver), **large**.
9. **Broken:** Hugo's button 1 sets stage 8 without re-checking (only the gump guards it). Gregor's stage-9 refusal line says "ruby" instead of "ring". Minor.
- **Port:** converted only.

---

## q025: Palirna ("The distillery")

1. **Files:** `q025_palirna.scp`, `_chars.scp` (ignore the schema png). Journal `questMenu[35]` `d_questy_palirna`: **27 distinct outcome texts**. qtag `q025` is a **bitfield** (l.16-30).
2. **NPCs:**
   - `c_q025_hospodska` "Eraveth Ufaile", innkeeper "near the bank".
   - `c_q025_palic` "Miruvil", distillery owner, "at the north end of the walls". Both in **Minas Tirith** (journal).
   - Placed: door typedef `t_q025_dvere`, guard tile `i_q025_hlidac_dveri`, pipe `i_q025_trubka`, boiler `i_q025_kotel`. The GM must link the door and tile to Miruvil's spawn.
   - All GM-placed.
3. **Who:** **Gondor only**. Mordor players are told "get lost", and Miruvil attacks them. Once; every path closes the quest.
4. **Steps / branches:**
   - Eraveth offers **5000 g to sabotage** the rival distillery. Accept or refuse; either way you learn of the plot.
   - Getting to the boilers:
     - Pick the locked door: Lockpicking ≥ 45.0 (thieves get 100+5×level exp).
     - Bash it: Str ≥ 80. Miruvil sees it and attacks.
     - Or let Miruvil open it (pipe-repair branch).
     - Stepping on the guard tile without permission makes Miruvil attack if he faces you.
   - **Sabotage:** wreck the boiler, report to Eraveth → **5000 g + (100+5×level) exp** (l.481-487).
   - Wreck it without having accepted: she denies everything; **250+12×level exp**, no gold (l.492-498).
   - **Kill Miruvil:** she pays **5000 g** to get rid of you (l.503-507). You become criminal.
   - **Loyalty:** tell Miruvil about the plot; he asks you to fix a leaking pipe and walks you to unlock the door (scripted walk, l.719-729, `i_q025_ovladani`).
   - Fix the pipe: Tinkering ≥ 35.0 (crafters +100+5×level exp) → **5000 g + (500+25×level) exp** (l.743-749).
   - Give up honestly → **1000 g + (200+10×level) exp** (l.731-738).
5. **Rewards:** gold and level-scaled exp only, as above. No items.
6. **Quest items:** door/tile/pipe/boiler world objects, NPC movement controller.
7. **Systems:** bitfield state, skill checks (Lockpicking, Tinkering, Str), facing/line-of-sight checks, NPC walking, criminal flag, attack hooks on NPCs, GM linking of objects to a spawn.
8. **Shape:** moral-choice puzzle with multiple endings, **medium** (766 lines).
9. **Broken/odd:** forged button presses send the player to jail (`src.go(klec)`, "bugger"). Otherwise this is complete and the most systemic quest in the group.
- **Port:** converted only.

---

## q026: Otrava z hrobek ("Poison from the tombs")

1. **Files:** `q026_hcnPoison.scp`, `q026_hcnPoison_items.scp`. Hooks in other files:
   - Andor: `q016_prastary_poklad.scp:334,381,404`.
   - Healers: `q010_mastickar.scp`, `q013_experimentator.scp`, `q006_erioluv_vnuk.scp`.
   - Unas: `q023_kouzelna_dyka.scp`. Roxana: `q020_carodejka.scp`.
   - Poisoning monsters: `scripts/chars/specNPC_HCN.scp:520,610,701,793`.
   - Journal `questMenu[36]` `d_questy_HCNPoison` (`questmenu.scp:2275-2312`), qtag `q026_hcnPoison`.
2. **Who gives it:** nobody. The curse is applied by the wraiths `c_hcn_wraith_level_1..4` "Prizrak" in the HCN tombs (Hrobky Cernych Numenorejcu, the tombs of the Black Númenóreans; event `e_q026_hcnPoisoning_NPC`).
   - NPCs: the healers (Gregoriel q010, the q013 experimenter, q006 NPC); cleric `c_q016_Andor` "Klerik Andor"; Unas; Roxana.
   - Places: the Temple of Knowledge in the swamps NW of Rhun (3769,1490 and 3941,1503), and a Dol Guldur secret room (5396,3384, area "Dol Guldur bonus").
   - All GM-built. The sliding doors, tapestry, secret room and box come with GM setup dialogs.
3. **Who:** any realm (dialog text exists for both). Once: stage 15 is final.
4. **Steps:**
   - A wraith hit curses you: memory item `i_q026_hcn_memoryPoison`.
   - Normal cure potions don't work; drinking one moves you to stage 2. Healers send you to a cleric (3). Andor sees dark magic and sends you to a wizard (4). Unas tells the lore of the Maiar and the Númenórean necromancers and sends you to the Temple of Knowledge (6).
   - Read the tapestry: +300 exp (`_items.scp:238-247`, stage 7). The temple door can't be opened.
   - In Dol Guldur, step on a hidden alarm tile (8), push the secret wall and open the box: you get the key (9).
   - The temple's sliding doors now open (10). A pillar of force and 30 damage stop anyone without the key (`_items.scp:180-198`).
   - Search the book pile (1 in 6 chance per click) for the cipher book (11).
   - Unas deciphers it and writes a recipe (12).
   - Roxana brews the cure. Deliver in parts: **20 batwing, 30 bone, 100 obsidian (reagent), 50 sulfurous ash, 2 obsidian ingots, 4500 copper ("spruce") logs, 2500 iron ("chestnut") logs**, then pay **20 000 gold** (`def_q026_CureResources`, l.15; l.288-305).
   - Drink the cure (stage 15).
5. **Rewards:**
   - +300 exp at the tapestry.
   - **SPECIAL: `i_q026_hcnPoison_Cure` "Lek na jed z hrobek"** (`q026_hcnPoison.scp:379-392`). Drinking it removes the curse and grants **permanent immunity to it**: `f_q026_HCNPoison_give` does nothing once the tag reaches 15 (l.23-36).
   - What the curse does: every 400-800 s you lose 1 food, or take 25 god damage when food ≤ 4 (`_items.scp:9-38`). 70 % of wraith hits deal ×(30..50)/30 damage, i.e. up to +67 % (l.26-30).
   - So the reward is "no more curse, ever"; there is no gear. **This is the #1069 potion `i_q026_hcnpoison_cure`.**
6. **Quest items/objects:** poison memory, cure, sliding doors (3 defs), tapestry, book pile, Dol Guldur alarm tile, secret wall, key box. The wraiths come from the HCN tomb set.
7. **Systems:** status curse with a periodic tick, potion hook, many cross-quest dialog hooks, GM-configurable secret room (multi-floor linking, hidden or revealed floors), sliding doors, step traps, random search, partial-delivery tracking (comma-list tag), `pay_from_char`.
8. **Shape:** mixed (curse → investigation → exploration → bulk fetch), **large**.
9. **Broken/odd:**
   - Stage 5 ("Unas wanders by the Bystra river") is never set by any script.
   - Debug messages "found: N of X" and "consuming…" are left in (l.347, 353).
   - The journal's tag list names `q026_hcnPoison_givenResources`, but the script uses `q026_hcnPoison_resources`.
   - The header lists "?Pocestny?" as an unimplemented side NPC.
- **Port:** converted only (`converted/questy/q026_hcnPoison*.def`; the cure is a `PotionDef` with no effect).
   - The HCN tomb monsters and their **own** abilities are ported (`Basic/AbilitiesAndTalents/MonsterSpecialAttacks/HcnTombs.cs`, #1022).
   - `e_q026_hcnpoisoning_npc` is only a `PassiveAbilityDef` stub (`teventStubs.scp:533`). The curse and quest are absent.

---

## q029: Murlay namornik ("Murlay the sailor")

1. **Files:** `q029_murlay_namornik.scp`, `_npc.scp`, `_items.scp` (author "Peto"). Journal `questMenu[39]`, qtag `q029_murlay_namornik`.
2. **NPCs:**
   - `c_q029_murlay_namornik` "Namornik Murlay", a shipwrecked captain on the reefs by the ice island. He is also a map vendor (`vendor_s_mapmaker`).
   - `c_q029_murlay_etiras` "Etiras, podivny tulak" (a strange vagrant), on the Windy Islands.
   - `c_q029_murlay_sevedia` "Sevedia, zvlastna zena" (a strange woman), on the pirate island.
   - No coords; GM-placed.
3. **Who:** anyone, once. Story tie-in: Murlay sails to the innkeeper for a cure for Eriol's grandson (q006).
4. **Steps:**
   - A storm tore Murlay's sails. Find the weaver on the Windy Islands.
   - Only her husband Etiras is there. Bring him **dust from 8 iron spirits**: collector, "Body of Spirit" colour 0763 → +300 exp.
   - He says she fled to the pirate island. Sevedia (disfigured by an insect bite) wants **3 adamantium-eye eyeballs** (collector, "Body of Eye" colour 0999) for a beauty elixir → sail, +500 exp.
   - Sail to Murlay → +500 exp + treasure map (l.189-196).
5. **Rewards:** 1300 exp; `i_u_treasure_map` "Tajemna mapa" (`scripts/systems/treasures.scp:149-185`). This is the **standard random treasure map**: an unidentified map with a random map level 300-1000 and one of 49 random spots, which spawns a treasure chest. Plain stock: monsters and regions drop the same item.
6. **Quest items:** `i_q029_murlay_kolektor_1`, `_2`.
7. **Systems:** collector bags, quest dialog framework, vendor NPC.
8. **Shape:** kill-collect + delivery, **medium**.
9. **Broken:** Murlay's "not yet" branch is unreachable (an identical nested `if`, l.87-95). Harmless.
- **Port:** converted only.

---

## q29: alchnezdar ("Popletalius, the bungling alchemist")

1. **Files:** `q29_alchnezdar.scp` (1400 l.), `q29_alchnezdar_chars.scp`. Several items it needs live in `questy/verbatim.scp` (outside this list): `i_q029_alchnezdar_planek`, `i_qv1_midnight_past`, `i_qv1_navnada`, `i_qv1_kamen_noci`, and the Ajda body parts `i_qv1_*_ajdy` / `_ajda`. Smith hook: `mq002_borek.scp:246-249, 277-282`. **Not in the journal** (`questMenu` has no entry). qtag `q029_alchnezdar` is set without a quest index.
2. **NPC:** `c_q029_alchnezdar` "Alchymista Popletalius". Monster `c_qv1_slizoun` "Slizoun" (a giant slime: 1500-1640 HP, resist_phys 986, Magery 500-600). No coords.
3. **Who:** anyone, once.
4. **Steps:**
   - His enlarged slimes split: bring the remains of **10 Slizoun** (crate collector) → 1 Greater Cure, 1 Greater Heal, 1 Invisibility potion (l.220-232).
   - His midnight stones spawned elementals: dust **6 live Midnight Elementals** (bag collector) → 1 Invisibility potion (l.351-360).
   - Take a trap blueprint to the smith… **the chain breaks here** (see 9).
   - Planned continuation: bait and a silver-spike trap, kill the elementals, bring 5 midnight stones.
   - Then "mummification" (stages 8-13): bring a rotting body part of "Ajda" plus **250 bandages + 10 Greater Heal** (+10k/20k/30k/40k/50k gold for parts 2-6) and get the embalmed part back. These parts belong to GM Verbatim's event "QV1" (`scripts/speech/verbatimspeech.scp`).
5. **Rewards:** 1 GCure, 1 GHeal and 4 Invisibility potions, plus 3 more Invisibility at step 4 as supplies. All plain. **Every `addexp` is commented out** (l.223, 355, 476, 597). The embalmed body parts are flavour items for the GM event.
6. **Quest items:** collectors; blueprint, trap, bait, midnight shard in verbatim.scp; the Slizoun monster.
7. **Systems:** collector bags, old-style gumps.
8. **Shape:** kill-collect, then GM-event content, **medium** (broken).
9. **Broken:**
   - The Mordor smith only says "let's look at the plan"; nobody gives the trap or sets stage 6.
   - Step 4 references **undefined** items `i_q029_alchnezdar_navnada`, `i_q029_alchnezdar_past`, `i_qv1_midnight_stone`. The real defs are named `i_qv1_navnada`, `i_qv1_midnight_past`, `i_qv1_kamen_noci`.
   - "Midnight Elemental" has no chardef anywhere.
   - Two of the three supply potions are created without being bounced (l.584-586).
   - Stage 8's first branch doesn't advance. Stages 8-14 need GM-event items.
   - Effectively **only stages 0-4 are playable**.
- **Port:** converted only.

---

## q30: Zmocnenec Gondoru / Mordoru ("The realm's envoy", internally "Do zbrane!", "To arms!")

1. **Files:** `q30_zmocnenec_dialogy.scp`, `q30_zmocnenec_collectory.scp`, `q30_zmocnenec_items.scp`, `q30_zmocnenec_chars.scp`. Journal `questMenu[2]` `d_questy_zmocnenecg` and `questMenu[3]` `d_questy_zmocnenecm`. qtags `gq030_zmocnenec` / `mq030_zmocnenec`, `q030_finished`, `q030_presel`. The envoys also host the q000 (Grandmaster) promotion branch (`f_q000_zmocnenec_*`), which is outside this group.
2. **NPCs:** `c_q30_zmocnenecg` "Zmocnenec Gondoru" and `c_q30_zmocnenecm` "Zmocnenec Mordoru". No coords in the archive.
3. **Who:**
   - Own realm only; the envoy threatens the other realm.
   - Level gates per stage: 1, 3, 6, 12, 15, 20, 25, 30, 40, 50, 60, 70, 80, 90 (l.216-244).
   - Realm switchers (`tag.presel`) restart from 0 and get half exp until they pass their old stage. Thieves who started the other realm's ladder are refused.
   - The final stage can be **repeated** ("I lost my reward"): 8, then 12 dragon hearts, no exp, another cloak (l.182-185, 278-290).
4. **Steps:** the envoy tests recruits with 14 trophy hunts, each with a collector bag.
   - **Gondor** (target corpse name, count): Potkan teeth 20 · Kostlivec Sekernik bones 9 · Liche eyes 8 · Infikovana Krysa claws 10 · Cerna Vdova eggs 7 · Titan nails 7 · Titan Lord nails 6 · Phoenix (poison, colour 097F) feathers 10 · Strazce Ohne hairs 5 · Gladiator Lord fingers 8 · "Evil" (Pan kobky) hearts 3 · Excelent twigs 3 · snow Gladiator Lord (colour 0481) ears 5 · Drak hearts 4.
   - **Mordor:** Lity Vlk teeth 20 · Troll fingers 7 · Velky Skorpion claws 15 · Skorpion Kral claws 10 · Liche King fingers 5 · Cyclop eyes 7 · Cyclops King nails 12 · Phoenix feathers 10 · Minotaur hides 5 · Gladiator Lord fingers 8 · Elder Spider hearts 4 · Excelent twigs 3 · snow Gladiator Lord ears 4 · Dragon hearts 4.
5. **Rewards:**
   - Exp per stage: 150, 300, 600, 1000, 1200, 1400, 1800, 2200, 3000, 4000, 6000, 8000, 10 000, 15 000 = **54 650 exp** (Gondor l.252-281; Mordor l.562-603).
   - **SPECIAL: "Plast bojovnika"** (warrior's cloak), base `i_plast_bojovnika`, a cape (`q30_zmocnenec_items.scp:1-28`; created by `f_q030_final_reward`, `q30_zmocnenec_dialogy.scp:729-738`):
     - **+10 HP regeneration** (shown +20 HP/10 s).
     - **+20 % melee and spell damage vs dragons** (imi 102).
     - Coloured in the realm uniform colour.
     - **Owner-bound**: only the owner can equip it.
     - Durability resets to 100 on every equip, so in practice it never breaks.
   - Unused older defs `i_q30_zmocnenecg_odmena` / `m_odmena` (+10 HP regen, +10 % vs dragons, weight 50, 10000 hits) are still in the collectors file (l.631-661).
6. **Quest items:** 28 collector bags, the cloak. `f_q030_repairCollector` swaps outdated bags.
7. **Systems:** collector bags, realm split, level gates, realm-switch penalties, repeatable last stage, owner-bound magic item.
8. **Shape:** kill-collect ladder, **large** (14 stages, levels 1→90).
9. **Broken:** the Mordor "Lost reward" button handler is empty (l.611-613); the re-request goes through `work_info` instead and works. The author's comment admits the stage-28 re-ask may show an empty dialog.
- **Port:**
   - The envoy NPCs **are** used and placed: `Basic/Quests/Grandmaster/GrandmasterQuest.cs:45-46,198-204,633-634` and `Setup/GrandmasterQuestSetup.cs:58-60` (placed beside Minas Tirith and Rhun by `.SetupWorld`).
   - Only the q000 promotion branch is ported. The **q030 trophy ladder, collectors and cloak are not ported** (converted defs only).

---

## q31: Kuchar Mrkvicka ("Carrot the cook")

1. **Files:** `q31_kuchar.scp`, `q31_kuchar_chars.scp` ("crafted by Darken"). Journal `questMenu[4]` `d_questy_kuchar`. Uses **plain char tags** `q31_kuchar`, `q31_neuspel`, not qtags.
2. **NPCs:** `c_q31_kuchar` (Gondor) and `c_q31_kuchara` (Mordor copy), both "Kuchar Mrkvicka, Mistr Delikatesa" (master of delicacies). No coords.
3. **Who:**
   - Own realm; each copy shrieks at the other realm.
   - Level ≥ 1. Once.
   - **Permanently lost** if you refuse, lie to him or give up: `q31_neuspel=1`, and he never speaks to you again.
4. **Steps:**
   - A rich client wants roast birds: take the bag `i_q31_kuchar_kolektor`.
   - Collect **50 bird corpses** ("Body of Ptacek").
   - Hand in. Optional lore chat about elf Ariadne's lost "Refreshing liqueur" (regeneration, near-invulnerability). It is a teaser only, with no follow-up anywhere.
   - If you claim to know the recipe, he calls you a liar and the quest is lost.
5. **Rewards:** 50 exp + **`i_pet_llama`** (`q31_kuchar.scp:256-264`).
   - **SPECIAL (mount):** a llama figurine (`scripts/sphereitemb7.scp:1893-1901`, `T_FIGURINE` → `c_llama`). Llamas are rideable in Erebor (`isjezditko`, `scripts/funkce.scp:3052`).
   - Wild llamas also spawn, so it isn't unique.
6. **Quest items:** the collector.
7. **Systems:** collector, figurine pets/mounts, gumps.
8. **Shape:** kill-collect, **small**.
9. **Broken:** none. It has a "replace broken bag" button.
- **Port:** converted only.

---

## q033: Volani more ("Call of the sea")

1. **Files:** `q033_volani_more.scp`, `_chars.scp`, `_items.scp`. Journal `questMenu[33]`, qtag `q033`.
2. **NPCs** (no coords, GM-placed):
   - `c_q033_vetesnikignac` "Vetesnik Ignac" (Ignac the junk dealer).
   - `c_q033_piratrenfer` "Pirat Renfer", on his sinking ship.
   - `c_q033_rybarvolek` "Rybar Volek" (Volek the fisherman), "in a quiet corner by the water".
   - Boss `c_q033_hirga` "Cernokneznik Hirga" (Hirga the warlock), "in a dungeon in the middle of the desert".
3. **Who:** anyone, once.
4. **Steps:**
   - Ignac is building a boat for a shady deal at sea. He needs rigging from Renfer.
   - Renfer wants the special recall scroll the mage Hirga stole. Kill Hirga: `i_q033_podivny_recall` drops with chance {0 1}.
   - Scroll to Renfer → rope `i_q033_piratovo_lano`, +150 exp. Rope to Ignac → +300 exp.
   - Volek wants a **magic-item bag (`i_mi_bag`)** for sailcloth. Ignac refuses to give one of his and pays 10 000 g "towards finding one" (l.178-183). You get a bag yourself; it is a monster drop.
   - Volek → cloth `i_q033_plachtova_latka`, +300 exp. Cloth to Ignac → +500 exp, +5000 g (l.185-195).
5. **Rewards:** 1250 exp, 15 000 gold. You **pay** one `i_mi_bag`. No items.
6. **Quest items/monsters:**
   - Items: rope, odd recall, cloth.
   - Hirga: liche body, 600-800 HP, Magery 200, harm/fireball/para/poison, undead, resist fire −250 / sharp −250, exp 60-90.
7. **Systems:** loot-drop quest item, gumps.
8. **Shape:** delivery chain + 1 boss kill, **medium**.
9. **Broken:** none found.
- **Port:** converted only. `i_mi_bag` itself is ported (`Basic/MagicItems/MagicBags.def:23`).
