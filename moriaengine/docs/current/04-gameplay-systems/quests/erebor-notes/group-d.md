<!-- Research notes for #1235, written from the Erebor archive (pkorpel/erebor-sphere-archive, scripts/questy). File:line cites are archive paths. Summary: ../erebor-catalogue.md -->

**Corrections after the fact-check (the catalogue has these right):**
- gq058/mq057: 10 magic items in a series (8 pouches + 2 high bags).

# Erebor quests, group D (realm-split and miscellaneous)

Source: local clone of the Erebor archive (`scripts/questy/`, Windows-1250, read through iconv). All `file:line` references are to
`scripts/questy/<file>` unless another path is given. Port references are to `/workspace/moria-final/moriaengine/scripts/`.

Conventions in the scripts:
- `tag(realm)`: 1 = Gondor, 2 = Mordor, 0 = no realm yet (tutorial), negative = outcast (vyvrhel). Values come from the code itself, for example
  `mq032_menegil.scp:67` (`realm==2` = Mordor-only) and `zmodrani.scp` (2 = mordoran, 1 = gondoran, < 0 = vyvrhel).
- `qtag(x)` is a per-player tag stored on an invisible "quest rune" item (`System/qTag.scp:1-26`). A third argument (`qtag(x,v,n)`) pops up the
  journal entry `questMenu[n]` (`System/questmenu.scp:3729`).
- `f_quest_complete(n)` only increments a statistics counter on the GM quest stone (`System/quest_system.scp:1-12`). It does not give anything.
- No NPC in this group has a spawn spot in the archive. `save/sphereworld.scp` holds only global variables (no WORLDCHAR/WORLDITEM), and no NPC or
  quest-prop defname occurs outside its own quest file. **Every quest giver, prop and quest spawn in this group was GM-placed.** Where a town is named
  below, it comes from the dialog or journal text, not from coordinates (the exceptions with hardcoded coordinates are noted).
- "Converted" in the port column means the item/NPC definitions exist in `scripts/converted/questy/<file>.def`. The converter comments out every
  dialog, function and speech body, so no quest logic exists there.

---

## 1. Complete questMenu index (`System/questmenu.scp:29-88`)

`questMenu_rows 59`. Columns: index, journal name, journal dialog (all 59 dialogs are defined in `System/questmenu.scp` itself), tag kind
(1 = qtag on the quest rune, 0 = plain character tag), the tags that mark the quest relevant, and where those tags are set.

| # | Name | Dialog | Kind | Tags | Quest file(s) found | Group / note |
|---|---|---|---|---|---|---|
| 0 | Zemedelec Borek | d_questy_borek | 1 | gq002, mq002 | gq002_borek*.scp, mq002_borek*.scp | **D** |
| 1 | Alchymistka Mikkena | d_questy_mikena | 0 | gq001 | gq001_mikkena*.scp | **D** |
| 2 | Zmocnenec Gondoru | d_questy_zmocnenecg | 1 | gq030_zmocnenec | q30_zmocnenec_*.scp | other |
| 3 | Zmocnenec Mordoru | d_questy_zmocnenecm | 1 | mq030_zmocnenec | q30_zmocnenec_*.scp | other |
| 4 | Kuchar Mrkvicka | d_questy_kuchar | 0 | q31_kuchar, q31_neuspel | q31_kuchar*.scp | other |
| 5 | Lecitel Ceren | d_questy_lavovyjester | 0 | q1_lavovyjester, q1_neuspel | q1_lavovyzostrich*.scp | other |
| 6 | Haradirovo selatko | d_questy_Haradirovo_selatko | 1 | q007_haradirovo_selatko | q007_HaradirovoSelatko.scp | other. The file contains NUL bytes, so a plain grep treats it as binary and misses the tag; use `grep -a` |
| 7 | Kniha inskripce | d_questy_kniha_inscripce | 1 | q007_stary_inscripter | q007_HaradirovoSelatko.scp (NPC c_maginscript "Stary Inscripter") | other (same NUL-byte note) |
| 8 | Adamantinove kladivko | d_questy_adamantinove_kladivko | 1 | q007_prastary_kovar | q007_HaradirovoSelatko.scp (NPC q007 kovar, i_adamantinum_kladivko) | other (same NUL-byte note) |
| 9 | Mastickar Gregoriel | d_questy_mastickar | 1 | q010_mastickar | q010_mastickar*.scp | other |
| 10 | Prekupnik Hruon | d_questy_prekupnik | 0 | gq011_prekupnik, gq011_neuspel | q011_prekupnik*.scp | other |
| 11 | Prekupnik Akranon | d_questy_prekupnik | 0 | mq011_prekupnik, mq011_neuspel | q011_prekupnik*.scp | other |
| 12 | Experimentator | d_questy_experimentator | 1 | gq013_experimentator, mq013_experimentator | q013_experimentator*.scp | other |
| 13 | Paradnice Minny | d_questy_paradnice | 0 | q015_paradnice, q015_neuspel | q015_paradnice*.scp | other |
| 14 | Prastary poklad | d_questy_prastary_poklad | 1 | q016_prastary_poklad | q016_prastary_poklad*.scp | other |
| 15 | Hobitin | d_questy_hobitin | 0 | q100_hobitin_ambra, q100_hobitin_ambra_neuspel | q100_hobitin*.scp | other |
| 16 | Kovar Gorug | d_questy_gorug | 1 | gq002_kovar, mq002_kovar | gq002_borek*.scp, mq002_borek*.scp | **D** |
| 17 | Kouzelna dyka | d_questy_kouzelnadyka | 1 | q023 | q023_kouzelna_dyka*.scp | other |
| 18 | Alchymista Terlak | d_alchymista_terlak | 1 | q017_terlak_ochrana, q017_terlak | q017_alchymista_terlak*.scp | other |
| 19 | Carodejka | d_questy_carodejka | 1 | q020 | q020_carodejka*.scp | other |
| 20 | Remeslnik Navin | d_questy_remeslnik_navin | 1 | q021 | q021_Remeslnik*.scp | other |
| 21 | Transport | d_questy_transport | 1 | q022 | q022_Transport*.scp (Kovar Gorug also hands out its ingots: gq002_borek.scp:338-340) | other |
| 22 | Erioluv vnuk | d_questy_erioluv_vnuk | 1 | q006 | q006_erioluv_vnuk*.scp | other |
| 23 | Lovec Odlier | d_questy_lovec_odlier | 1 | nb001 | start_dung*.scp | **D** (tutorial) |
| 24 | Akolytka Mirana | d_questy_akolytka_mirana | 1 | nb002 | start_dung*.scp | **D** (tutorial) |
| 25 | Hornik Beorgrad | d_questy_hornik_beorgrad | 1 | nb003 | start_dung*.scp | **D** (tutorial) |
| 26 | Lesnik Grarad | d_questy_lesnik_grarad | 1 | nb004 | start_dung*.scp | **D** (tutorial) |
| 27 | Rybar Broket | d_questy_rybar_broket | 1 | nb005 | start_dung*.scp | **D** (tutorial) |
| 28 | Nekromant | d_questy_necromancer | 1 | nb006 | start_dung*.scp | **D** (tutorial) |
| 29 | Remeslnik Imro | d_questy_remeslnik_imro | 1 | nb007 | start_dung*.scp | **D** (tutorial) |
| 30 | Vyvrhel | d_questy_vyvrhel | 1 | q099 | q099_vyvrhel*.scp | other |
| 31 | Zlaty losos | d_questy_zlaty_losos | 1 | q018 | q018_zlaty_losos*.scp | other |
| 32 | Potulny pocestny | d_questy_pocestny | 1 | q019_pocestny | q019_pocestny*.scp | other |
| 33 | Volani more | d_questy_volani_more | 1 | q033 | q033_volani_more*.scp | other |
| 34 | Zasnubni prsten | d_questy_zasnubni_prsten | 1 | q024 | q024_zasnubni_prsten*.scp | other (an "engagement ring" quest, so the other group should check it against the no-wedding-rings rule) |
| 35 | Palirna | d_questy_palirna | 1 | q025 | q025_palirna*.scp | other |
| 36 | Otrava z hrobek | d_questy_HCNPoison | 1 | q026_hcnPoison, q026_hcnPoison_givenResources | q026_hcnPoison*.scp | other. **`q026_hcnPoison_givenResources` is never written by any script in the archive** (it appears only in questmenu.scp:66) |
| 37 | Umbarska hadka | d_questy_umbarskaHadka | 1 | mq027_umbarskaHadka | mq027_umbarskaHadka*.scp | **D** |
| 38 | Ztracene Dcery | d_questy_velmistr | 1 | q000_velmistr | velmistr*.scp | other (already ported: Grandmaster, Basic/Quests/Grandmaster) |
| 39 | Murlay Namornik | d_questy_murlay_namornik | 1 | q029_murlay_namornik | q029_murlay_namornik*.scp | other |
| 40 | Predajca Menegil | d_questy_menegil | 1 | mq032_menegil, mq032_menegil_prevozca | mq032_menegil*.scp | **D** |
| 41 | Herman | d_questy_herman | 1 | q035_herman | q035_herman*.scp | other |
| 42 | Uhrovity Jonas | d_questy_jonas | 1 | q036_jonas | q036_jonas*.scp | other |
| 43 | Opily Zarik | d_questy_zarik | 1 | q037_zarik | q037_zarik*.scp | other |
| 44 | Oblast cerne smrti | d_ocs | 1 | q038_ocs | q038_ocs*.scp | other |
| 45 | Stinove zbrane | d_shadow_weapon | 1 | q039_shadow_weapon | q039_shadow*.scp | other |
| 46 | Spol. Drakobijcu | d_dragons | 1 | q040_dragons | q040_dragons*.scp | other |
| 47 | Drakobijec Alechymista | d_dragons_alchymista | 1 | q040_dragons_alchymista | q040_dragons*.scp | other |
| 48 | Drakobijec Rakchin | d_dragons_rakchin | 1 | q040_dragons_rakchin | q040_dragons*.scp | other |
| 49 | Blaznivy Drakobijec | d_dragons_blazen | 1 | q040_dragons_blazen | q040_dragons*.scp | other |
| 50 | Drakobijci - brneni | d_dragons_kovar | 1 | q040_dragons_kovar | q040_dragons*.scp | other |
| 51 | Paserak | d_prestupak | 1 | q041_paserak | prestupak*.scp. The final realm-switch gate is in zmodrani.scp (one-line note in section 5) | other |
| 52 | Halda Molle Hatlend | d_hm_hatlend | 1 | q042_hatlend_rulik | q042_halda_molle*.scp | other |
| 53 | Murdocovi Ucni | d_murdoc_ucni | 1 | q043_tut, mq043, gq043 | q043_*.scp. q043_murdocovi_ucni.scp has a byte iconv rejects as cp1250 (position 5288). gq043/mq043 are first set when a character leaves the tutorial (start_dung.scp:1989-1993) | other |
| 54 | Zachrante Murdoca | d_murdoc_zachrana | 1 | q044 | q044*.scp | other |
| 55 | Labyrint Zlateho Goblina | d_zlaty_goblin | 1 | Goblin_Labyrint | goblin_labyrint*.scp | **D** |
| 56 | Lovecka Vyveska | d_lovecka_vyveska | 0 | loveckasoutez | q00macros.scp (hunting board i_sign_gquest) | no group listed it, so it gets a short section here (D-extra) |
| 57 | Zaskodnik | d_zaskodnik | 1 | mq057_zaskodnik | mq057_zaskodnik*.scp | **D** |
| 58 | Zaskodnice | d_zaskodnice | 1 | gq058_zaskodnice | gq058_zaskodnice*.scp | **D** |

**Entries with no quest file:** none. Every entry has a script in `scripts/questy`. The only gap is the second tag of #36,
`q026_hcnPoison_givenResources`, which nothing writes. Journal "done" checks worth knowing about: #55 and #56 are never "done"
(`f_isDone` returns 0, so both are repeatable activities). #1 Mikkena counts as "done" as soon as the quest is accepted (`f_isDone_d_questy_mikena`:
`tag(gq001) > 0`). #37's check wants phase 35-37, which the script never reaches (see mq027).

Quest files in my list that are **not** in the journal: gq012 Selka, beh (eq_002), tameblack, crunch (neutral 009), knihy_pravdy_neutral (neutral 008),
event_christmas, event_quests, eq_001.

---

## 2. Summary table

Port status: "conv" = items/NPCs converted only (no logic).

| Id | Name (gloss) | Giver | Realm | Shape / size | Special reward? | Port status |
|---|---|---|---|---|---|---|
| gq001 | Alchymistka Mikkena (alchemist Mikkena) | c_gq001_mikkena Mikkena | both (step 2 differs by realm) | fetch + delivery / small | no (gold, exp) | conv |
| gq002 / mq002 | Zemedelec Borek (farmer Borek) | c_gq002_borek / c_mq002_borek Borek | gq002 Gondor, mq002 Mordor | mixed (fetch/craft-service) / small | no | conv |
| gq002_kovar / mq002_kovar | Kovar Gorug (smith Gorug) | c_gq002_kovar / c_mq002_kovar Kovar Gorug | Gondor / Mordor | kill + fetch / small | no (random food and reagents) | conv |
| gq012 | Selka Enrica (farmer's wife Enrica) | c_gq012_selka Selka Enrica | any (no check) | kill (collect corpses) / small | no (50 exp only) | conv; t_collector system not ported |
| gq058 / mq057 | Zaskodnice / Zaskodnik (the saboteur) | c_gq058_zaskodnice Zaskodnice / c_mq057_zaskodnik Zaskodnik | gq058 Gondor (+non-Mordor), mq057 Mordor (+non-Gondor) | mixed (timed infiltration, 18 stages) / large, repeatable | **yes**: 8x i_mi_pouch, 2x i_mi_bag_high, XP booster (+25 % XP for 2 h), about 109,550 gold per series | conv; booster/magic bags ported separately |
| mq027 | Umbarska hadka (the Umbar quarrel) | c_mq027_Antonio Antonio | any (no check) | talk / medium, **unfinished** | no (small exp per step) | conv |
| mq032 | Predajca Menegil (merchant Menegil) | c_mq032_menegil Menegil | Mordor only | delivery/talk chain / large | no (gold, exp) | conv |
| nb001-nb007 | Tutorial quests (7 NPCs) | c_nb001..c_nb007 | tutorial characters | fetch / craft / gather / one boss kill / small each | tools (stock); tutorial exit can grant Expert rank | conv; character finalisation partly ported (RaceSelection.cs, other source) |
| Goblin_Labyrint | Labyrint Zlateho Goblina (Golden Goblin's labyrinth) | moving gate i_goblin_labyrint_entry guarded by c_goblin_labyrint | any, level > 6 | puzzle/exploration / medium, daily | **yes**: chance of i_mi_bag from the 6th run | conv |
| eq_002 beh | "Beh" (Frodo's journey) | i_eq_002_beh_zacatek stone at Bag End | any | exploration / large (map-wide) | no (1,500 exp) | conv |
| tameblack | Jandoruv black (the black mustang) | c_crunch_alenor Alenor | any ranger | kill + access / small | **yes**: access to a corral holding the black mustang, a magic mount | conv; mount + mount powers ported |
| crunch (neutral 009) | Beauova hlava (Beau's head) | c_irbis_08 Alchymista Beau | any | kill + fetch / small, **broken** | no (1,500 exp) | conv |
| knihy_pravdy (neutral 008) | Knihy pravdy (books of truth) | c_crunch_Karenus Karenus | any | talk chain + 2 kills / medium | no (2,000 exp, 50,000 gold in the boss corpse) | conv |
| event_christmas | Christmas Quest 2016 (bulb collector) | c_vanocni_sberac Vasnivy sberatel | any | collect/exchange event / medium, seasonal | **yes**: i_mi_bag, i_u_treasure_map, temporary RP-rank XP bonus | conv; item defs commented out in Erebor itself |
| event_quests (Easter) | Easter Quest 2016 (egg collector) | c_sberac_vajec Farmar Olda | any | collect/exchange event / medium, seasonal | **yes**: i_mi_bag, i_u_treasure_map | conv; item defs commented out in Erebor itself |
| loveckasoutez | Lovecka vyveska (hunting board) | item i_sign_gquest | per board realm, level >= 4 | kill / small, daily | no | not ported (WeeklyQuests.cs:68 notes it) |

---

## 3. Quest sections

### 3.1 gq001 Alchymistka Mikkena ("alchemist Mikkena")

1. **Id / files:** gq001; `gq001_mikkena.scp`, `gq001_mikkena_chars.scp`. Journal #1 (plain tag `gq001`).
2. **Givers:** `c_gq001_mikkena` "Mikkena, Vehlasna alchymistka" (also a healer vendor, `SELL=VENDOR_S_HEALER`, chars:42). Delivery targets: `c_gq001_edwin`
   "Edwin, hospodsky" in **Hurka** (Gondor branch) and `c_gq001_charton` "Charton, hospodsky" in **Nurn** (Mordor branch); the towns come from the dialog text
   (gq001_mikkena.scp:149, 251). All GM-placed; there is one Mikkena for both realms.
3. **Who:** anyone; no realm/level check for step 1. Step 2 picks Edwin or Charton by `tag(realm)` 1 or 2 (gq001_mikkena.scp:361-372). Once only.
4. **Steps:**
   - Mikkena needs 10 pieces of bark from the dead trees of **Temny hvozd** (the Dark Forest) for a rush order.
   - Double-click `i_gq001_strom` (GM-placed dead tree). Each tree gives 2 `i_gq001_kura` and then refills 90 minutes later. The timer is per tree and
     shared by all players (gq001_mikkena.scp:426-441).
   - Hand in 10 bark: tag 1 → 2.
   - Step 2: she gives a potion parcel `i_gq001_lektvary`. Say "lektvary" to Edwin (Gondor) or Charton (Mordor), who swap it for an empty jar `i_gq001_sklenice`.
   - Bring the jar back: tag 3.
5. **Rewards:** step 1: 500 gold + (150 + level×10) exp (gq001_mikkena.scp:85-86). Step 2: 500 gold + (50 + level×10) exp (Gondor :184-185, Mordor :286-287).
   No items. No special reward.
6. **Quest items:** i_gq001_strom (dead tree prop), i_gq001_kura (bark, crumbles if double-clicked), i_gq001_lektvary, i_gq001_sklenice. All `attr_move_never`.
   No monsters.
7. **Systems:** NPC dialog gumps, speech keywords, per-object timers, realm branch.
8. **Shape:** fetch + delivery, small.
9. **Issues:** the Mordor hand-in has no `f_quest_complete(1)` (statistics only). A character with realm 0 or a negative (outcast) realm at tag 2 falls through to
   the first dialog and can re-accept and re-hand-in step 1 for another 500 gold. This loop is unbounded (gq001_mikkena.scp:361-372). The journal counts the
   quest as "done" as soon as it is accepted.
   **Port:** items/NPCs converted only (`converted/questy/gq001_mikkena*.def`).

### 3.2 gq002 / mq002 Zemedelec Borek ("farmer Borek")

1. **Id / files:** gq002 (Gondor) `gq002_borek.scp` + `_chars`; mq002 (Mordor) `mq002_borek.scp` + `_chars`. The two are near-identical copies with different
   texts and locations. Journal #0.
2. **Givers:** `c_gq002_borek` "Borek, chudy gondorsky zemedelec" / `c_mq002_borek` "Borek, chudy mordorsky zemedelec". Props: the plough `i_gq002_pluh` /
   `i_mq002_pluh` next to him. Smith: Kovar Gorug (3.3). Gondor smithy "south of Hurka"; Mordor smithy "on the square in Rhun" (dialog
   gq002_borek.scp:105, mq002_borek.scp:105). GM-placed.
3. **Who:** gq002 refuses realm 2 and anyone who has started mq002 (gq002_borek.scp:367). mq002 refuses only realm 1 (mq002_borek.scp:382), so realm-0 and
   outcast characters can take either. **Requires Kovar Gorug's quest to be finished first**: Gorug repairs the share only when `gq002_kovar >= 3`
   (gq002_borek.scp:256-262). Once only.
4. **Steps:**
   - Borek's plough share is cracked. He asks you to take it to the smith.
   - Ask him how to remove it (tag 1 → 2), then double-click the plough to get `i_gq002_radlice0`, the cracked share (tag 3).
   - Give it to Gorug, who repairs it for free *if* you got his hammer back: `i_gq002_radlice1` (tag 4).
   - Double-click the plough to refit it (tag 5), then tell Borek (tag 6).
5. **Rewards:** 500 gold + (50 + level×10) exp (gq002_borek.scp:148-149; mq002_borek.scp:149-150). No items, nothing special.
6. **Quest items:** i_gq002_pluh / i_mq002_pluh (plough prop), i_gq002_radlice0/1 (cracked/repaired share). No monsters.
7. **Systems:** questDialog framework (`argo.questDialog_*`), qtags + journal popup, realm gating. Borek's dialog also hosts the Grandmaster promotion hook
   (`f_q000_shouldBePromoted_*`, defined in velmistr.scp:955-996, already ported). Mordor Borek also hosts q041 Paserak's bargain hook
   (mq002_borek.scp:35-38, 123, 159-162).
8. **Shape:** mixed (fetch/service), small.
9. **Issues:** none blocking. Mordor Borek doesn't block a character who did gq002, so a realm switcher (q041) could do both.
   **Port:** items/NPCs converted only (`converted/questy/gq002_borek*.def`, `mq002_borek*.def`).

### 3.3 gq002_kovar / mq002_kovar Kovar Gorug ("smith Gorug's hammer")

1. **Id / files:** qtags `gq002_kovar` / `mq002_kovar`, in the same files as Borek. Journal #16.
2. **Givers:** `c_gq002_kovar` "Kovar Gorug, zrucny gondorsky remeslnik" / `c_mq002_kovar` (mordorsky). Gondor: smithy south of Hurka, trolls in a forest
   clearing to the north. Mordor: Rhun square, trolls in the thicket south of the walls, east of the road (gq002_borek.scp:282, mq002_borek.scp:299).
   GM-placed. Gorug also runs a step of q022 Transport (150 iron ingots) and, in Mordor, reacts to q029's plan `i_q029_alchnezdar_planek`.
3. **Who:** Gondor version refuses realm 2; Mordor version refuses realm 1. No level check. Once only.
4. **Steps:**
   - Local trolls stole Gorug's hammer because his hammering keeps them awake. Accept (tag 1).
   - Find the loot chest `i_gq002_bedna` at the trolls' camp. A GM links it to a troll `t_spawn_char`; it won't open while the linked spawn has a living
     troll (`link.more2`, gq002_borek.scp:519-520).
   - Open the chest: you get the hammer `i_gq002_kladivo` plus loot (tag 2).
   - Return the hammer (tag 3).
5. **Rewards:** chest: `i_gq002_kladivo` (quest item) + 500 gold + 2× random_FOOD {5-10} + 4× RANDOM_REAGENT {5-20} (gq002_borek.scp:525-532;
   mq002_borek.scp:539-546). Hand-in: (150 + level×10) exp (gq002_borek.scp:322; mq002_borek.scp:341). Unlocks Borek's free repair. Nothing special.
6. **Quest items / monsters:** i_gq002_bedna / i_mq002_bedna (chest; a GM double-click links it to a spawn), i_gq002_kladivo / i_mq002_kladivo. The trolls
   are whatever troll spawn the GM linked; no custom troll exists.
7. **Systems:** item→spawn link with an alive check, dialog gumps, qtags.
8. **Shape:** kill + fetch, small.
9. **Issues:** none found. **Port:** items/NPCs converted only.

### 3.4 gq012 Selka Enrica ("the farmer's wife and the voles")

1. **Id / files:** gq012; `gq012_selka.scp`, `gq012_selka_chars.scp`. **Not in the journal.**
2. **Giver:** `c_gq012_selka` "Selka Enrica" on a farm (no town named). GM-placed. Monster: `c_gq012_selka_hrabos` "Hrabos" (giant rat body, STR 150-170,
   DAM 6-15, exp 1-2) on her fields. GM spawn.
3. **Who:** anyone (no realm, level or profession check). Once (plain tag `gq012_selka`).
4. **Steps:** her husband is away and voles are eating the crop. She gives a crate `i_gq012_selka_collector` (gq012_selka.scp:131-139). Kill voles, target
   their corpses ("Body of Hrabos") with the crate, then hand in 10 or more (gq012_selka.scp:143).
5. **Rewards:** 50 exp (gq012_selka.scp:145). Nothing else.
6. **Quest items / monsters:** the collector crate (holds up to 20; t_collector) and the Hrabos monster.
7. **Systems:** Erebor's generic **t_collector** type (`scripts/systems/uzitecne_nezaraditelne_itemy.scp:2`): double-click, target a corpse by name,
   success chance from a tag. It is also used by nb006 and q30.
8. **Shape:** kill, small.
9. **Issues:** a lost crate leaves tag 1 forever with no way to get a new one (stuck). The reward is negligible.
   **Port:** items/NPCs converted only. t_collector not ported (no C# reference).

### 3.5 gq058 Zaskodnice / mq057 Zaskodnik ("the saboteur", Gondor/Mordor mirror)

1. **Id / files:** gq058 `gq058_zaskodnice.scp` + `_chars`; mq057 `mq057_zaskodnik.scp` + `_chars`. These are line-for-line mirrors (same line numbers).
   Journal #58 / #57.
2. **Givers:** `c_gq058_zaskodnice` "Zaskodnice, tajemna elfka" (an ex-captain of the guard demoted to a desk job "by the sea", in obsidian plate) and
   `c_mq057_zaskodnik` "Zaskodnik, tajemny Haradan" (in obsidian chainmail). GM-placed. The 18 sabotage targets (`i_gq058_1..18`, `i_mq057_1..18`) are
   GM-placed props in the **enemy** realm's towns.
3. **Who:** gq058 refuses realm 2, mq057 refuses realm 1, so realm-0 and outcast characters get through. Craft profession refused. **Level ≥ 40.**
   **Only between 17:00 and 21:59 server real time** (`isHourOK(getHour(),17,21)`; getHour reads `servertime`, `scripts/dobyvani/dobyvani-utils.scp:15-50`)
   (gq058_zaskodnice.scp:20-31). The sabotage props also refuse the wrong realm and refuse "too many witnesses" (below). **Repeatable**: after the full series
   a 4-day memory item runs (345,600 s, :71), then the series restarts at stage 1 (tag 190).
4. **Steps (18 stages, strictly in order; each stage has a 30-minute limit):**
   - Accept a stage. A timer memory `i_gq058_zaskodnice_casomira` (TIMER=1800 s = 30 min; the header comment says 60) is equipped, and a **server-wide
     realm message** announces "the Gondorian saboteur heads towards X" (`i_realmMessage_announcer`, `scripts/dobyvani/dobyvani-declarator.scp:53`).
   - Go to the target in enemy territory and double-click it. It works only if no "witness" is alive: each prop has GM-linked spawns, and
     `f_guarditAdv_CheckSpawns` (`scripts/systems/dungeon_bonusy_yavanna.scp:2817`) fails while any linked spawn has a living NPC. Some stages consume
     materials. Success broadcasts "the saboteur completed the task".
   - If the 30 minutes run out: failure broadcast. The stage can be retaken.
   - Report back for the stage reward, then accept the next stage.
   - Gondor targets (gq058, from the dialog :394-449): east gate of Mordor; plank bridge over the lava at Mount Doom; Black Gate; rum in the Umbar harbour tavern;
     spy records in the fort west of the Black Gate; ritual site in the Rhun rocks; religious texts in the Nurn temple; betting records in Tahabar arena; Barad-dur
     guard roster; poison the Hagir oasis pond (10 i_potion_PoisonLess); explosive in the Nurn mill (1 i_potion_Lava); smear the Rhun shaman's kettle with
     200 i_reag_nightshade; poison the Bagronk well (5 i_potion_Poison); poison the Harad ruler's wine (3 i_potion_PoisonGreat); 20 i_zmrzlavoda into
     Barad-dur's foundations; kill the Umbar oliphant with 1 i_potion_PoisonDeadly; spike the Barad-dur tavern beer with all six special poisons (fizz, weaken,
     clumsy, para, feeblemind, stamina); bribe the Harad guard captain with 2 i_ingot_adamantinum.
   - Mordor targets (mq057, journal :37+): Linhir dam sluice; hero's tomb at Forondon; Tharbad wheeled stall; Bard's remains in Dol; Grey Havens ship's brake;
     Erebor kings' tomb; Baggins manuscript in Hobitin; Pelargir tax records; Fornost guard roster; poison Esgaroth fish (10 PoisonLess); Calembel mill
     (1 Lava); plant 200 i_reag_mandrake_root in Lorien's grove; Helm's Deep well (5 Poison); Hurka mead (3 PoisonGreat); Dol Amroth lighthouse fuel
     (20 zmrzlavoda); Woodland Realm wine (1 PoisonDeadly); Minas Tirith poor-kitchen soup (six special poisons); light the Edoras beacon with
     2 i_log_adamantinum.
5. **Rewards:**
   - Each of stages 1-17: (420 + level×2) exp, gold 2500 + 25 × (stage tag after the increment, i.e. 14, 24, … 174) = 2,850 … 6,850, and **i_mi_pouch** when
     `(tag % 20) == 4`, i.e. after stages 2, 4, 6, 8, 10, 12, 14, 16 = **8 pouches** (gq058_zaskodnice.scp:588-592; mq057_zaskodnik.scp:588-592).
   - Stage 18 (final): (1420 + level×2) exp, **2× i_mi_bag_high**, **`playerBooster_add(1)`**, 27,100 gold, and the 4-day cooldown memory
     (gq058_zaskodnice.scp:595-603; mq057_zaskodnik.scp:595-603).
   - Totals for one full series: 82,450 + 27,100 = **109,550 gold**; 8,560 + 36×level exp; 8 pouches + 2 bags + 1 booster.
   - **Special items (looked up):**
     - `i_mi_pouch` "Vacek s magickym predmetem" (`scripts/systems/magicITEMS.scp:850`): double-click → one random unidentified magic weapon/armour with
       **1 magic property**, base tier (item metal weighted copper 500 / iron 400 / verite 200 / valorite 100 / obsidian 20 / adamantinum 10 / mithril 1 in the
       port's table, MagicItemTables.cs:101-104).
     - `i_mi_bag_high` "Pytlik s magickym predmetem" (magicITEMS.scp:892): **2 magic properties**, "high" tier (verite 300 / valorite 200 / obsidian 80 /
       adamantinum 30 / mithril 1).
     - Booster 1 = **"+25 % zkusenosti na 2h"**, i.e. +25 % XP for 2 hours, stored until the player activates it from the journal
       (`System/boosters.scp:49-53`). This confirms the port's note that gq058/mq057 are a missing booster source, and they give **kind 1**.
6. **Quest-only items:** 18 props per realm; three memory items (pauza / casomira / dokonceni). `f_gq058_predmety` (:645-659) is a GM/test helper that hands
   out every consumable the series needs; it is not a reward. No custom monsters (the "witnesses" are whatever the GM spawns next to each prop).
   i_zmrzlavoda (frozen water, `verbatim.scp:1849`) drops from snow monsters (`scripts/chars/snowmonsters.scp`).
7. **Systems:** qtags with the 10s/units stage encoding, timed memory items, real-clock hour gating, server-wide realm broadcast, prop→spawn witness links
   (dungeon_bonusy guardedadv), profession/level/realm gating, magic bags, boosters, journal.
8. **Shape:** mixed (timed infiltration/sabotage chain into enemy territory), large, repeatable every 4 days.
9. **Issues:** the 5-minute "pause" item is checked (:32) but its equip is commented out (:586), so it is dead. The timer comment says 60 min while the code says
   30. Negative-realm (outcast) and realm-0 characters pass the realm check on both sides.
   **Port:** items/NPCs converted only (`converted/questy/gq058_zaskodnice*.def`, `mq057_zaskodnik*.def`). The rewards' building blocks exist: magic bags
   (`Basic/MagicItems/MagicBags.def`) and boosters (`Basic/Quests/Boosters/Boosters.cs:13`, which lists gq058/mq057 as an unported source). The saboteur
   logic, witness check and realm announcer are not ported.

### 3.6 mq027 Umbarska hadka ("the Umbar quarrel")

1. **Id / files:** mq027_umbarskaHadka; `mq027_umbarskaHadka.scp`, `_chars`. Journal #37. The chars file's SUBSECTION wrongly says "mq002".
2. **Givers:** `c_mq027_Antonio` "Antonio" (a farmer) and `c_mq027_Vincent` "Vincent", Antonio's brother, who hangs around the **Umbar harbour tavern**
   (journal text). GM-placed.
3. **Who:** **no realm, level or other check at all**, despite the mq prefix (mq027_umbarskaHadka.scp:280-286, 594-600). Once.
4. **Steps:** a comic go-between chain.
   - Antonio wants back the shovel he lent Vincent.
   - You shuttle insults and demands between the brothers 19 times: liar and gambler, apologies refused, threats of the guards, insults about wives and their
     mother, Vincent disowning his brother.
   - Vincent finally admits he lost the shovel at dice and asks Antonio for a 1,000-gold loan to win it back.
   - The journal (questmenu.scp, `d_questy_umbarskaHadka`) describes planned phases 20-37: the loan, Vincent losing it again, a house seizure, both brothers
     offering you 1,000-2,500 gold to kill the other, and an ending where Vincent thanks you. **None of 20-37 is scripted.**
5. **Rewards:** each step gives `level/6 + 3` exp (`f_mq027_stepExperience`, mq027_umbarskaHadka.scp:607-609), about 19 steps. No gold, no items.
6. **Quest items / monsters:** none.
7. **Systems:** questDialog framework, qtags + journal, a "you are armed" check (`src.getArmed`) to unlock the intimidation branch.
8. **Shape:** talk, medium (long dialog chain).
9. **Issues:** **unfinished.** Phase 19 has a response button (`mq027_gotLoanDetails`) with no handler, so the quest dead-ends at 19. The journal's done check
   wants 35-37, which is unreachable. Vincent's handler at phase 18 sets 18 again (harmless, no page offers it).
   **Port:** items/NPCs converted only.

### 3.7 mq032 Predajca Menegil ("merchant Menegil")

1. **Id / files:** mq032_menegil (+ mq032_menegil_prevozca); `mq032_menegil.scp` (1,618 lines), `mq032_menegil_npc.scp`, `mq032_menegil_items.scp`.
   Journal #40. The script comments are in Slovak.
2. **Givers / NPCs (all GM-placed):** `c_mq032_menegil` Menegil, a merchant starting a shop in **Umbar**; `c_mq032_kapitan` ship captain; `c_mq032_hospodsky`
   innkeeper; `c_mq032_rybar` Darthan, a fisherman "somewhere above Umbar"; `c_mq032_latkarka` Rhalla, seamstress; `c_mq032_latkarka_pomocnik` Siran, her
   drunk helper; `c_mq032_prevozca` Herrar, porter in **Tahabar**; `c_mq032_prevozca_pomocnik` Tomald; `c_mq032_skladnik` manager of the **Harad**
   warehouse; `c_mq032_skladnik_manzelka` Elurien, his wife. The "Snubny prsten" on the warehouse couple is only costume (worn silver ring), not a
   wedding-ring system.
3. **Who:** **Mordor only**. Every NPC function requires `realm==2`, otherwise "Vyhni se mi obloukem, ty prasivy Gondore!" (mq032_menegil.scp:64-73 and
   the same in each f_mq032_*). Once.
4. **Steps (five errands, tag 1-28):**
   - Ship: find out when his goods ship arrives. The captain wants a barrel of spirits, which the innkeeper sells for **3,000 gold** (`i_mq032_menegil_sud`).
     Answer: in 3 days.
   - Fish: the captain wants fish from Darthan. Darthan claims he wasn't paid; Menegil says he paid; remind Darthan; deliver the fish.
   - Cloth: Rhalla's thread never arrived. Find the drunk helper Siran for the thread. Optionally pay Herrar 3,000 gold to carry the finished cloth.
   - Bananas: order bananas from the Harad warehouse. The manager refuses; either pay **5,000 gold** or threaten him via his wife (+150 exp, free).
   - Cloth again: it hasn't arrived. Find Tomald and get your 3,000 back from Herrar, then report.
5. **Rewards (all at Menegil, mq032_menegil.scp):** ship 200 exp + 1,000 gold (:636-637); fish 250 + 1,500 (:641-642); cloth 1: 200 + 1,000 (:646-647);
   bananas 300 + 1,500 (:657-658); final 400 + 5,000 (:651-652). Elurien branch +150 exp (:1612); Herrar refund 3,000 gold (:1302). Totals: 1,350 exp
   (1,500 with the wife branch), 10,000 gold, minus the 3,000 barrel (and minus 5,000 if you pay the warehouse). No items, nothing special.
6. **Quest items:** `i_mq032_menegil_sud` (barrel). Fish, thread and cloth are tag-only, never actual items. No monsters.
7. **Systems:** questDialog framework, qtags + journal, gold payments (GoldUtils.Pay in the port), realm gating.
8. **Shape:** delivery/talk chain, large.
9. **Issues:** none blocking found. The journal text has typos ("Relka"). **Port:** items/NPCs converted only.

### 3.8 Tutorial quests nb001-nb007 (start_dung, "starter dungeon")

1. **Id / files:** nb001-nb007; `start_dung.scp` (2,029 lines), `start_dung_chars.scp`, `start_dung_items.scp`. Journal #23-#29.
2. **NPCs (in the tutorial region, GM-placed, no coordinates for the NPCs):** `c_nb001` Lovec Odlier (hunter), `c_nb002` Akolytka Mirana (acolyte of Este),
   `c_nb003` Hornik Beorgrad (miner), `c_nb004` Lesnik Grarad (forester), `c_nb005` Rybar Broket (fisherman), `c_nb006` Necromancer (unnamed),
   `c_nb007` Remeslnik Imro (craftsman), `c_nb_gatekeeper` Gatekeeper, `c_nb_healer` Lecitel, `c_uvitac_tut` "Pomocnik, Trener novacku" (greeter).
   Hardcoded tutorial positions: `home=5412,2555,13,35` (tutorial), exit `go(4763,1362,10,35)` (start_dung.scp:1929-1957).
3. **Who:** new characters in the tutorial region. nb007 is for the **craft profession** only (f_nb007 :1210-1216; others are told "nejsi remeslnik", :1319).
   nb003 steps 2-3 need **Mining > 39.9 / > 44.9**; nb004 step 2 needs **Lumberjacking > 39.9**; nb007 items must carry your own `craftedby` tag.
   All nb qtags are wiped when you leave (nbTags_remove :139-152), so each runs once per character.
4. **Steps / rewards (exp; tools given at accept):**
   - nb001 Odlier: 15 cut hides `i_hides_cut` → 100 exp (:328); then 10 cooked ribs `i_ribs_cooked` → 75 exp (:340). Gives i_dagger_copper + i_scissors.
   - nb002 Mirana: 50 bandages → 75 exp (:473). Gives i_scissors.
   - nb003 Beorgrad: 30 copper ore → 100 (:660); 15 iron ore (Mining 40) → 450 (:673); 10 silica sand `i_ore_pisek` (Mining 45) → 750 (:689). Gives i_pickaxe.
   - nb004 Grarad: 30 spruce logs `i_log_copper` → 100 (:857); 15 chestnut logs `i_log_iron` (Lumberjacking 40) → 450 (:870). Gives i_hatchet.
   - nb005 Broket: 5 fish → 75 (:1045); 10 cooked fish steaks → 100 (:1057). Gives i_fishing_pole.
   - nb006 Necromancer: kill the **Liche Lord** near the tutorial, burn its body with the collector bag `i_nb_collector_lichelord`, bring the ash → 200 exp
     (:1190).
   - nb007 Imro (crafters): self-crafted 50 copper wire → 75 (:1631); 1 copper heavy crossbow → 225 (:1643); 2 copper gnarled staves → 250 (:1655);
     10 kilts → 250 (:1667); 10 copper viking swords → 250 (:1679); 5 iron closed helms → 500 (:1691). Imro also trades 30 cooked fish steaks →
     1 `i_tetiva_spruce` (:1699) and 50 copper ingots → 1 `i_krystal_spruce` (:1707).
   - Tutorial "first time" XP (tutAddActionXP, :65-130): 20 each for resurrecting at an ankh, equipping armour, equipping a weapon, drinking a potion, killing an
     NPC; 10 each for equipping a fishing pole and mounting.
   - Gatekeeper (level ≥ 3, `def_tutorial_min_level`): choose a realm and leave. On leaving (`startfinal`, :2002-2027): gear wiped, **15,000 gold**,
     i_rune_base, `i_crystal_<class>`, the class newbie kit, `level×5` skill points (i_dovedbod), and **Expert rank directly if level ≥ 3** (`tag(skillrank,1)`,
     :2004). The realm's Murdoc tutorial quest is also started (gq043/mq043, :1989-1993).
   - **Special:** the tools are plain stock items. `i_tetiva_spruce` ("bowstring for spruce weapons") and `i_krystal_spruce` ("weak activation crystal")
     (`scripts/systems/uhli.scp:56, 182`) are crafting components with no script of their own. The Expert rank grant at exit is a rank/skill-cap reward.
5. **Quest items:** i_tut_rune (separate tag rune), i_nb_collector_lichelord (t_collector), i_regenpad (tutorial pad: +100 hits/mana/stamina regeneration for
   10 s, then a 60 s lockout). The Liche Lord is a regular monster.
6. **Systems:** tutorial region with no PvP (e_nbdung cancels player-vs-player swings and spells), action-XP hooks, collector, craftedby tag, character creation
   (`I_stone_start` class/stat picker), gatekeeper, realm choice.
7. **Shape:** fetch/gather/craft tutorial, one boss kill; small each, medium as a set.
8. **Issues:** nb005's fish hand-in uses `rescount(i_fish_big[<i>])` to count but `consume(...,i_fish_big_<i>)` to take, so it can take up to 5 of *each* of
   the 4 fish kinds instead of 5 total (:1031-1043). nb007's button handlers don't re-check the stage, and `craftedby` is checked on the first found stack
   only. `def_tutorial_max_level 5` is defined but never used.
   **Port:** items/NPCs converted only (`converted/questy/start_dung*.def`). New-character finalisation exists in `Basic/Races/RaceSelection.cs`, but it is
   modelled on `newbie_portals.scp`'s `startclass` (2,000 gold, no rune/crystal, no Expert-at-3), not on this tutorial's `startfinal` (15,000 gold).
   `Basic/Moderation/Camp.cs` knows a "tutorial" region. The nb quests, gatekeeper and tutorial XP hooks are not ported.

### 3.9 Labyrint Zlateho Goblina ("the Golden Goblin's labyrinth") + Pokladovy Goblin (treasure goblin)

1. **Id / files:** qtag `Goblin_Labyrint`; `goblin_labyrint.scp`, `goblin_labyrint_chars.scp` ("automated event" by Jeffrey / GM Iustus). Journal #55.
2. **Entrance:** `i_goblin_labyrint_entry` "Brana do neznama". It **moves to a random spot in the world every 3-6 h** (`timer={10800 21600}`, :245; 408
   hardcoded positions in `f_goblin_labyrint_random_position`) and is blocked while its guard `c_goblin_labyrint` "Zlaty Goblin" (400-500 hits) lives.
   The gate starts a 30 s countdown on first use and takes at most 10 players per location. A "Zlata mapa" `i_goblin_labyrint_map` shows the current gate
   location once; the archive does not say where it comes from. A memory stone with a fixed UID (#040041AEC) keeps statistics.
3. **Who:** players with level > 6 (:100). **Once per calendar day** (24 h memory, cleared on a new day, :64, :221). Any realm.
4. **Steps:**
   - Kill the Golden Goblin and step through the gate into the labyrinth (map planes 30-34, start 5900,355).
   - Inside, "fires of insight" change colour (green/blue/red/white/yellow, plus brown three times as often). Each pass teleports you to a random labyrinth spot
     and counts that colour (max 10 per colour).
   - After 25 coloured passes, or 3 of each of the 5 non-brown colours, the next fire drops you on the final floor (plane 35).
   - Find the golden exit gate `i_goblin_labyrint_prizegate`.
5. **Rewards (`f_goblin_labyrint_prize`, :291-338):** runs 1-5: exp 500 + 100×n and gold 10,000 + 2,000×n (n = earlier completions, :303-304). From the 6th
   run on: 1,000 exp, 20,000 gold (:306-307) and a chance of **i_mi_bag** (:310). The chance is (200 + 4×n)/1000, raised by the precision bonus (:286-287).
   A precision bonus of ((35 − coloured passes) − brown passes) × 5 % (minimum 0, up to +100 %) multiplies exp and gold (:315-316). Gold goes to the bank as a
   purse (:318). There is a leaderboard of the most completions.
   - **Special:** `i_mi_bag` (magicITEMS.scp:882): double-click → random unidentified magic weapon/armour with **2 magic properties**, base tier.
6. **Treasure goblin (same file):** every time players kill dungeon monsters worth 500,000 exp **server-wide** (counter on the memory stone, hooked from
   `scripts/systems/npc_systems.scp:36`), a `c_goblin_treasure` "Pokladovy Goblin" (5,000-6,000 hits) appears at the last kill. About 120 s later it starts
   casting an escape gate; hitting it interrupts the cast. If killed, a GM-pre-filled chest is moved to its spot: silver 9 times in 10, gold 1 time in 10
   (the chests are linked by fixed UIDs `def_goblin_treasure_*_treasurer`). Labyrinth monsters: c_goblin_forest/fire/silver/diamond/dark.
7. **Systems:** moving portal with a guard spawn, multi-plane instanced labyrinth, colour-state teleports, daily memory, bank purse, magic bags, global
   kill-exp counter, escape AI.
8. **Shape:** puzzle/exploration, medium, daily (the treasure goblin part is a world event).
9. **Issues:** the gold map's source is not in the archive. The chest contents depend on GM-stocked containers with fixed UIDs.
   **Port:** items/NPCs converted only (`converted/questy/goblin_labyrint*.def`). `e_goblin_treasure` exists only as a stub passive
   (`Basic/AbilitiesAndTalents/teventStubs.scp:169`).

### 3.10 eq_002 "Beh" ("Frodo's journey", the run)

1. **Id / files:** `beh.scp` (eq_002, an "event quest"). Not in the journal.
2. **Props (hardcoded coordinates, placed by `f_eq_002_beh_rozmisteni`, :1615):** start column `i_eq_002_beh_zacatek` at Bag End "Dno pytle" (1074,1300,6);
   15 stops `i_eq_002_beh_zastavka11..24`: west/east ferry, Hurka, Vetrov, Roklinka (Rivendell), Moria, the Pass, Lorien, Marka, the Marshes, Henneth Annun,
   Minas Morgul, Cirith Ungol, Mount Doom (:1327-1585); end `i_eq_002_beh_konec` (684,1336, Frodo's departure point).
3. **Who:** anyone, any realm. The route crosses both realms (Minas Morgul, Cirith Ungol and Mount Doom are in Mordor). Repeatable once the 3-day memory
   (`timer=259200`, :1304) expires.
4. **Steps:** read the start column; a 3-day memory starts. Follow riddle clues from stone to stone in the right order. A stone visited out of order shows a
   "not yet" text. Finish at the end column.
5. **Rewards:** exp per stop 30, 40, … 160, and 170 at the end (beh.scp:973-1281) = **1,500 exp** total. No items.
6. **Quest items:** the 17 stones and the timer memory. No monsters.
7. **Systems:** hardcoded world props, ordered progress counter, timed memory, riddle text gumps.
8. **Shape:** exploration, large (map-wide), small reward.
9. **Issues:** none found. **Port:** items converted only (`converted/questy/beh.def`).

### 3.11 tameblack ("Jandor's black mustang")

1. **Id / files:** `tameblack.scp` (tag `jandoruv_black`). Not in the journal.
2. **NPCs:** `c_crunch_alenor` "Alenor, Potulny ranger" (invulnerable vendor next to a corral) and `crunch_elm_black`, a special "Ice Elemental" among the ice
   elementals on the **snowy island** (file comment). GM-placed. Corral coordinates hardcoded: in 810,3545,10, out 817,3538,10.
3. **Who:** **rangers**. Killing the special elemental with taming > 81.0 (`act.taming>810`, tameblack.scp:36) sets the tag. Alenor lets you in with taming
   > 95.0 (:94). Repeatable (the tag is removed on entry).
4. **Steps:** kill the special ice elemental as a ranger, greet Alenor, and he lets you into the corral with the black mustang to try to tame it. Say "pustit"
   to get out.
5. **Rewards:** **access** to the black mustang `c_a_mustang_black` (`scripts/chars/mounts.scp:1279-1320`: STR 1200, DEX 150, Taming 95-100, `tag(nores,1)`,
   `e_noport`, `tag(magic_horse,black)`).
   - **Special:** a magic mount. Per the port's mount-power table (`Basic/MagicItems/MountPowers.cs:26`, written from the archive) it gives its rider +20 %
     damage vs undead/demons, +20 % vs animals and spiders, +20 % vs humanoid monsters, +5 % Hiding and +10 % Stealth while riding.
6. **Quest-only:** crunch_elm_black (STR 650, exp 38-42, loot_elem).
7. **Systems:** profession/skill-gated kill tag, corral teleports, taming, magic mount powers.
8. **Shape:** kill + access (taming challenge), small.
9. **Issues:** Alenor's dialog says you must bring "all the ingredients found on the snowy island", but the script checks only the one tag. The speech checks
   `act.taming` where the speaker is `src`, which may not be the intended object.
   **Port:** quest converted only (`converted/questy/tameblack.def`). The black mustang chardef is converted (`converted/chars/mounts.def`) and its mount
   power is ported (MountPowers.cs).

### 3.12 crunch: Kvest Neutralni-009 "Beauova hlava" ("Beau's head")

1. **Id / files:** `crunch.scp` (tag `splnenkvest_004`). Not in the journal.
2. **NPCs:** `c_irbis_08` "Alchymista Beau"; `c_crunch_zlodejicek` "Zlodejicek Bell", a thief on level 2 of the **Etherka** dungeon (file comment) who carries
   `i_Crunch_hlava2` "tlejici hlava" (a rotting head) via NPCRestock. The same file has `c_crunch_kecal` "Urbannus, Ukecanek" (a chatty Mordor NPC that names
   real players, flavour only) and `i_Crunch_bank`, a chest that opens your bank. GM-placed.
3. **Who:** anyone. Repeatable in practice (the done tag is checked only for a thank-you line).
4. **Steps:** Beau needs a rotting head as an ingredient. Kill Bell in Etherka, loot the head, bring it back.
5. **Rewards:** 1,500 exp (crunch.scp:191). Nothing else.
6. **Quest items / monsters:** i_Crunch_hlava2, Bell (STR 600, plate/studded).
7. **Systems:** speech keywords only.
8. **Shape:** kill + fetch, small.
9. **Issues:** **broken.** The hand-in checks `RESTEST 1 i_Crunch__hlava2` (double underscore, :187) while the item is `i_Crunch_hlava2`, so it can never
   complete. **Port:** converted only.

### 3.13 Kvest Neutralni-008 "Knihy pravdy" ("the books of truth")

1. **Id / files:** `knihy_pravdy_neutral.scp` (tags `crunch_informace_008` 1-8, `splnenkvest_008`). Not in the journal.
2. **NPCs (GM-placed):** `c_crunch_Karenus` "Karenus, byvaly knihovnik" (an exiled librarian of Minas Tirith); `c_crunch_Bvutannus` "Bvutannus, mestsky
   knihovnik" (the current librarian); `c_crunch_Gurmug` "Gurmug, orci vudce" (orc chief); `c_crunch_Hurtan` "Hurtan, stary mag". Monsters:
   `c_crunch_snowminotaur` "Snow Minotaur" in a cave on the ice plains (disarms the target on hit; the description says "nepouzivat!!", do not use) and
   `c_crunch_thoragala` "Thoragala", a harpy and Bvutannus's sister.
3. **Who:** anyone. Once (`splnenkvest_008`).
4. **Steps:**
   - Karenus sends you to ask Bvutannus why Karenus lost his post. Bvutannus gives clue 1 "pribeh" (the story).
   - Karenus tells the "true story" (clue 2) and sends you to Gurmug, who led the orcs that burned the library.
   - Gurmug wants **20,000 gold** for his version (clue 3): Bvutannus knocked Karenus out.
   - Karenus sends you to Hurtan for Bvutannus's weakness. Hurtan wants the Snow Minotaur dead first, then names the sister Thoragala ("cut up the body, she
     tends to get up again").
   - Kill Thoragala.
5. **Rewards:** on Thoragala's death, the killer at stage 8 gets 2,000 exp (:29), and `i_crunch_indicie4` plus **50,000 gold** are put in the corpse (:33-34).
   Net +30,000 gold. The four clue items (`i_crunch_indicie1-4`, "slova na web", words for a web puzzle) are flavour scrolls. Nothing special.
6. **Systems:** speech-driven stage tags, gold payment, kill-credit on death, a transform memory (Gurmug becomes an orc body).
7. **Shape:** talk chain + 2 kills, medium.
8. **Issues:** the gold goes into the corpse, so anyone can loot it. Only the killer gets credit, not the party. **Port:** converted only.

### 3.14 Christmas Quest 2016 ("collector of Christmas baubles"), event_christmas.scp

1. **Id / files:** `event_christmas.scp`. Seasonal GM-started event. **This is not Christmas CTF.** Not in the journal.
2. **NPCs:** `c_vanocni_sberac` "Vasnivy sberatel, Sberatel vanocnich banek" (invulnerable); `c_christmas_pig` "Zlate prasatko" (golden piglet, 500 hits,
   drops 1-20 baubles and has a pig head as a resource). Memory stone with a fixed UID #0400514A1. GM-placed.
3. **Who:** anyone, during the event.
4. **Steps:** bring the collector a **fresh** golden pig's head (`i_christmas_pig_head`, rots on a timer) → 500 exp and the event memory `i_christmasQuest`
   (3,000,000 s ≈ 34.7 days, :462). Then trade baubles `i_christmas_bulb`. Baubles drop from piglets, from NPCs at start (1 per 50 gold they carry, 1 time
   in 3) and from PvP (25 per kill unit).
5. **Rewards:** 100 baubles → 10,000 gold (:301); 1,000 → **i_mi_bag** (:324); 2,500 → **i_u_treasure_map** (:347). Lifetime turn-ins of 500 / 3,000 / 7,500
   raise the character's `RP_rank` by 1 / 2 / 3 steps while the memory lasts (:416-425), announced as +5 / +10 / +20 % exp (`System/RP_rank.scp:141-143`).
   The steps are removed when the memory ends (:470-473).
   - **Special:** i_mi_bag (2 magic properties, base tier). i_u_treasure_map is an unidentified treasure map (a stock Sphere item; the port's MonsterLoot.cs
     already gives it). The **temporary RP-rank XP bonus** is a special reward.
6. **Systems:** event memory, exchange by speech keywords, RP rank, leaderboard, PvP hook, world-wide NPC seeding function.
7. **Shape:** collect/exchange event, medium, seasonal.
8. **Issues:** **disabled as shipped**: the `i_christmas_pig_head` and `i_christmas_bulb` itemdefs are commented out (:7-60).
   **Port:** converted only (`converted/questy/event_christmas.def`).

### 3.15 Easter Quest 2016 ("egg collector"), event_quests.scp

1. **Id / files:** `event_quests.scp` (Easter part). Seasonal. Not in the journal.
2. **NPCs:** `c_sberac_vajec` "Farmar Olda, Dodavatel vajec" (invulnerable); `c_easter_rabbit` "Velikonocni zajic" (drops 1-10 eggs); `i_easter_port` "Port k
   zajickovi", a gate that hops around 2235-2284 / 2641-2685 and leads to the rabbit's lair (476,611,-20,2). Memory stone with a fixed UID #040034663.
3. **Who:** anyone, during the event.
4. **Steps:** prove yourself by reaching the end of the Easter rabbit's lair, which should give `i_easterQuest_wentThrough` (that itemdef is commented out, and
   nothing in the file gives it). Then trade eggs `i_easter_egg`: from rabbits, from NPCs at start (1 per 75 gold, 1 time in 3) and from PvP (25 per kill unit).
5. **Rewards:** 100 eggs → 10,000 gold (:380); 1,000 → **i_mi_bag** (:403); 2,500 → **i_u_treasure_map** (:426). Leaderboard.
6. **Systems:** as 3.14.
7. **Shape:** collect/exchange event, medium, seasonal.
8. **Issues:** **disabled as shipped**: `i_easter_egg` and `i_easterQuest_wentThrough` are commented out (:78, :493), and the lair's finish trigger is missing.
   The same file also has a Silvester pig `c_pig_silvester` (32,000 hits, drops sausages and ale), `i_opilcova_kosile` "Opilcova kosile" (jester suit with
   +10 % Musicianship, no giver in the file) and `i_realm_gate`, an event port to 6091/6115,3198 by realm.
   **Port:** converted only.

### 3.16 Lovecka vyveska ("hunting board"), q00macros.scp (journal #56; no group listed it)

1. **Files:** `q00macros.scp:1-1237` (`i_sign_gquest`, `i_qf_sign`, result board `i_sign_gquest_vysledky`, the fq_* monster tables).
2. **Giver:** GM-placed notice boards, each tagged Gondor or Mordor (`realmQ`).
3. **Who:** level ≥ 4; only boards of your own side. **Daily.**
4. **Steps:** each day a board picks a random monster family (64 tables: wolves, trolls, titans, scorpions, …). Take the hunt and kill at least 3 of it that day.
   Resurrected NPCs don't count.
5. **Rewards:** exp = `huntedexperience × kills / 2`, gold = `huntedgold × kills × 2` (purse to the bank) (`f_sign_gquest_odmen_lovce`, :82-106). This also feeds
   the weekly quest `f_weekly_quest_HuntingSign` and the season score `loveckasoutez`. Nothing special.
6. **Shape:** kill, small, daily.
7. **Port:** not ported. `Basic/Quests/Weekly/WeeklyQuests.cs:68` says "HuntingSign: i_sign_gquest isn't ported".

---

## 4. Port check method

`grep -rli` for every quest's NPC, item and tag defnames under `/workspace/moria-final/moriaengine/scripts`, excluding `converted/`, `bin/` and `obj/`. Only
these hits exist outside `converted/`:
- i_mi_pouch / i_mi_bag / i_mi_bag_high: ported (`Basic/MagicItems/MagicBags.def`, `MagicItems.cs`).
- Boosters: ported (`Basic/Quests/Boosters/Boosters.cs`). It notes gq058/mq057 as a missing source, and this report confirms they give booster kind 1.
- Black mustang mount power: ported (`Basic/MagicItems/MountPowers.cs:26`).
- Arena entrance (arena_crunch/arena_chars): ported (`Basic/Pvp/Arena.cs`, `Setup/ArenaSetup.cs`).
- e_goblin_treasure: stub only (`Basic/AbilitiesAndTalents/teventStubs.scp:169`).
- Quest journal: only the three rank quests are listed (`Basic/Quests/Common/QuestJournal.cs`).

Everything else in this group is "items/NPCs converted only".

---

## 5. Non-quest files (one line each)

- **eq_001_craftMstitel_chars.scp**: an event dungeon (the "doupe" lair), not a quest. It has 25 craft/peasant-themed monsters (cow, chicken, thieves with
  "[Artefakt]"-named but plain newbie weapons, weaver, tanner, fletcher, mages; loot gold/reagents/loot_DAEMON, one `i_mi_bag` on "Vidlak Ochrance").
  The boss is `c_eq_cm_mocichtivy_zbohatlik` "Mocichtivy Zbohatlik". A placeholder dialog "Prines mi prsten!" (Yes turns him into the boss, No ports you out),
  and four stone guardians awaken at 80/60/40/20 % hits; while one lives, the boss can't be hurt. GM reset tile. No reward beyond loot. Converted only.
- **etheria.scp**: Etherka dungeon gate puzzle. Four floor tiles each set `etherka_dlazka` to 1-4; the gate should port to level 4 and give 2,000 exp. **Broken:**
  the gate requires the tag to equal 1 AND 2 AND 3 AND 4 at once (:102), so it never opens. Converted only.
- **zmodrani.scp + zmodranichar.scp**: the realm-switch ("prestupni") gate, the final step of q041 Paserak (other group). It needs `q041_paserak == 9` and
  level ≥ 15 at the sanctuary. It swaps Gondor↔Mordor and resets kills/karma (Mordor gets kills 30000, karma −3000). Outcasts are refused. There is a unicorn
  key-giver `c_jednorozec_prechod` and a monk NPC. Converted only.
- **krvave_prizraky.scp**: lore NPCs (bards Kerdalion and Soterios recite a "Blood Wraiths" poem, speech in `scripts/speech/krvaveprizraky.scp`; Soterios also
  serves q020). It also defines `c_crunch_krvavec`, a raven whose `i_srdiczko` heart q006 uses, and `c_crunch_strazcebrany`, a gate guard who ports you on
  the word "Vorbis". Not a quest.
- **povysujici_brany.scp**: old rank-promotion gates and timers for the Master/Grandmaster path (`i_mistr_stone`, `i_gate_mistr` needs Expert and level > 5,
  plus countdown items). Superseded by the ported rank quests.
- **muina_oronti.scp**: an unfinished stub, one NPC `c_mo_malica` "Malica, kouzelnik". Its double-click is commented out and its speech `spk_mo_malicia` is
  defined nowhere.
- **crunch_vampirismus_items.scp**: GM event item `I_CRUNCH_PREMENA_VAMP` "srdce na metamorfozu". Using it permanently turns the user into a gargoyle body
  with vit=500. No source in the archive. Not a quest.
- **arena_crunch.scp + arena_chars.scp**: Murdoc's training arena entrance (200 gold fee; karma/kills restored on exit). System, already ported
  (`Basic/Pvp/Arena.cs`).
- **vyjmecni.scp**: GM-only fire-column/explosion effect functions (`stribrny1-3`) and a GM port dagger. Not a quest.
- **verbatim.scp**: GM Verbatim's toolkit (Shagar dungeon props, web/bleed/spawn pads, advanced spawn pad dialog, AFK countdown). It also has qv1/qv2 props for
  GM-run events: "Ajda" artefact clothes with +100 Magery / +100 Str that refuse to be equipped by players, crystals, a midnight-elemental trap and bait, and a
  4-player snow-altar gate puzzle whose altar tags are never given ("doplnit skutecne tagy", to be filled in). It also hosts `i_zmrzlavoda` (needed by gq058/mq057)
  and `i_q029_alchnezdar_planek` (q029). Not a quest.
- **qv3_verbatim.scp**: New Year's Eve (Silvestr) GM reward bags: random iron armour, adamantinum armour or obsidian weapon. Event loot, not a quest.
- **enrico.scp**: GM/dev toolbox (follow-script test, aura, mass move, gates filtered by level/profession/time, drunk-NPC speeches, `i_srdiczko` used by q006).
  Not a quest.
- **noe.scp**: Noe's props.
  - Recipe books (shrink potion, bottle form).
  - `I_NECRO_WAND`: turns a mage into a red dragon or a necromancer into a lich for 40 s, +100 STR, +50 DEX, −50 INT. **A wand, so not to be ported (user's
    rule).**
  - A "Black Riders" transformation (Mordor ruler's rune, gate to an island, 60-minute Mordor-wraith disguise).
  - A Morthia crack teleport.
  - An **invisible-maze GM contest**: the winner gets 1,000 exp, a 40,000-gold cashier's check and a "Ridgeback mount voucher, page a GM" scroll (noe.scp:329-334).
  - None of it is a scripted quest.
- **orfeus.scp**: theatre props for the RP character Orfeus (key-locked gate, curtains). Not a quest.
- **event_quests.scp (Silvester part)**: see the end of 3.15.
- **knihy.scp**: 27,288 lines of in-game book texts (`[BOOK kniha_*]`, 167 itemdefs). Lore only, skipped as instructed.

None of my files is CTF, Christmas CTF, bomberman, the sheep game or a wedding-ring system. The only item on the do-not-port list is noe.scp's necro wand.
