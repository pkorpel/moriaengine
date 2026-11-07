<!-- Research notes for #1235, written from the Erebor archive (pkorpel/erebor-sphere-archive, scripts/questy). File:line cites are archive paths. Summary: ../erebor-catalogue.md -->

**Corrections after the fact-check (the catalogue has these right):**
- q009: the ring's wait is about 2.4 h (serv.time counts tenths of a second: 86400 = 2.4 h), each early visit adds 18 min.
- kvest0001: `i_bottle_rostok_muznosti` comes from a GM-placed cauldron that refills any empty bottle, so the permanent +10 Str/-30 Int can be had again and again.

# Erebor quests, group A: catalogue for the port decision

Source: Erebor archive `scripts/questy/` (Sphere 0.99z8). File:line citations point to the archive files. The line numbers match the originals because only `\r` was stripped. The port was checked in `/workspace/moria-final/moriaengine/scripts`.

General notes that apply to every quest below:
- **No NPC in this group has a world spot in the archive.** `save/sphereworld.scp` holds no NPCs, so every giver was GM-placed. The only placement information is the coordinate comments in the scripts, quoted where they exist.
- **Journal ("questMenu") row numbers** come from `System/questmenu.scp:31-89`. `f_quest_complete(n)` only increments a GM statistics counter on the quest stone. `src.qtag(name,value,row)` writes the tag to the invisible quest rune and refreshes journal row `row`.
- **`t_collector`** (`systems/uzitecne_nezaraditelne_itemy.scp:2`) is the shared "bag" mechanic. The player double-clicks the bag and targets a corpse whose name equals `tag.target_id`. The chance per corpse is `value_to_check / absolute_success`, and the bag counts up to `maxcontents`. **The port has no C# implementation of `t_collector`** (no hits for `contentscount`/`t_collector` in the `.cs` files). Quests marked "collector" below need it built first.
- **`i_mi_bag`** (`systems/magicITEMS.scp:882`) is a bag that unpacks into a random basic-tier ("green") magic item with 2 magic properties. **It is already ported**: `scripts/Basic/MagicItems/MagicBags.def:23`.
- **Port status, all quests:** every file of this group has a converted twin in `scripts/converted/questy/`. Those twins hold the item and NPC defs only. Speech, dialogs, functions and item triggers are commented out by the converter, so for example the robe, hammer and ring defs exist but carry no skill bonus. **None of these quests has quest logic in the port.** Exceptions are noted per quest.

## Summary table

| id | name (gloss) | giver | realm | shape / size | special reward? | port status |
|---|---|---|---|---|---|---|
| kvest0001 | Mordorske kvesty 001-005 (Barad-dur captain, Reyla & Akhbar, Medicine for Liguk, ...) | Orc Burlug `c_h_poskok_bratrstva` (Rhun); captain/guards at Barad-dur | Mordor (by setting) | mixed fragments; Reyla&Akhbar = kill/fetch medium; rest stubs | Only exp (10/200/400). Side props: a potion with a permanent +10 Str / -30 Int, trap items | items/NPCs converted only |
| kvest0002 | Gondorske kvesty 001-005 (Gondor quests) | none (citadel guards, bard Kardusaj) | Gondor | none: empty sections, only Minas Tirith citadel pass/guard props | none | items/NPCs converted only |
| kvest0003 | Neutralni kvesty 001-008 (Vampire story, Golden Sheaf, Dragon Degran, ...) | Pan Zvirat `c_Pan_zvirat`, Zlaty mustang, pirate Grager, Degran's guard | both | GM-event fragments; Golden Sheaf = fetch/talk small; Degran = boss + craft | **Yes**: ranger Taming boost (+10% of the gap to 130.0); "Dragon Sword of Doom" (20000 dmg, +1000 Str/Dex, invulnerable while held) | items/NPCs converted only |
| kvest0004 | Clenstvi v uskupenich (Order memberships) + Irbis's eggs + experiments | Irbis `c_irbis`, Hrobeus `c_Hrobeus` | both | memberships = header only; Irbis eggs = kill/fetch small (unfinished) | no (exp only, 500/500/1500) | items/NPCs converted only |
| q006 | Erioluv vnuk (Eriol's grandson) | healer Eriol `c_q006_eriol` (+ Qworet, Unas, innkeeper) | both | fetch chain + talk + craft, **large** (24 stages) | **Yes**: Robe of the Alchemist, +10.0 Alchemy (owner-bound, needs GM alchemy) | converted only (+ a dummy `i_potion_ocisteni` potion kind) |
| q007a | Haradirovo selatko (Haradir's piglet) | hunter Haradir `c_Haradir` | both | talk/fetch/boss, medium-large | no (1000 exp). Unlocks q007b/c | converted only |
| q007b | Kniha inskripce (Inscription book) + Magicke pero (magic pen) | Old Inscripter `c_maginscript`; drunk Divous `c_divous` | both | fetch (3 rare stones), small | **Yes**: book unlocks 8th-circle scroll inscription; can be traded for a pen that makes recall runes | converted only |
| q007c | Adamantinove kladivko (Adamantine hammer) | Ancient Smith `c_kovarskej` | both | fetch (same 3 stones), small | **Yes**: smith's hammer +10.0 Blacksmithing (needs GM smith), which reaches the 110.0 needed for mithril armor | converted only |
| q008 | Black Mustang | horse breeder Ancam `c_q008_old_man` (571,3547,10) | both, **ranger only** | kill + collect + tame, medium; repeatable | **Yes**: a tamed black mustang (magic mount: +20 slayer vs demons/animals+spiders/humanoids, +5 Hiding, +10 Stealth) | converted only; mount powers already ported (#925) |
| q009 | Mistr krejci (Master tailor) | tailor Talathwen `c_q009_krejci` | both | buy (5M gold check) + wait 1 day, small | **Yes**: Tailor's Ring, +10.0 Tailoring (owner-bound, needs GM tailor) | converted only |
| q010 | Mastickar Gregoriel (the ointment maker) | Gregoriel `c_q010_mastickar_mastickar` | both | kill + collect, small | no (150 exp, 1500 gold) | converted only |
| q011 | Prekupnik Hruon / Akranon (the fence) | Hruon `c_gq011_prekupnik` (G) / Akranon `c_mq011_prekupnik` (M) | Gondor / Mordor variants | 3 chained collector kill tasks, medium; repeatable | no (1200 exp, 14000 gold per run) | converted only |
| q013 | Experimentator Theorius / Theorianulus | Theorius `c_gq013_...` (G) / Theorianulus `c_mq013_...` (M) | Gondor / Mordor variants | kill + collect, small | random magic item bag `i_mi_bag` | converted only (`i_mi_bag` ported) |
| q015 | Paradnice Minny (Minny the show-off) | Minny `c_q015_paradnice` | not Mordor | kill + collect, small | no (1000 exp, 2500 gold) | converted only |
| q016 | Prastary poklad (Ancient treasure) | dying necromancer `c_q016_StaryNegr` (3366,3819) + Orillus, Andor | both (exp depends on realm and moral choice) | talk/fetch/exploration with a moral choice, medium | no (up to 1950 exp + 50000 gold) | converted only |
| q1 | Lecitel Ceren / lava lizard (healer Ceren) | healer Ceren `c_q1_lavovyjester_lecitel` | both | exploration + kill + collect, small | random magic item bag `i_mi_bag` + 2000 exp | converted only |
| q14 | Jeskyne poznani (Cave of Knowledge) | Dalmara `c_q14_dalmara`, Pepe `c_q14_pepe`, Malkor `c_q14_malkor` | both | big fetch/craft chain (incl. farming), **large** | 4x `i_mi_bag`, 5000 exp, 80000 gold, one-time cave visit | converted only |

---

## kvest0001 + kvest0001NPC: "Mordorske kvesty" (Mordor quests 001-010)

1. **Files:** `kvest0001.scp`, `kvest0001NPC.scp`. Headers list Mordor-001 to 010. Only 001-005 have any content and 006-010 are empty placeholders.
   - Mordor-001 "Baradsky kapitan" (the Barad-dur captain)
   - Mordor-002 "Reyla a Akhbar"
   - Mordor-003 "Lek pro Liguka" (medicine for Liguk)
   - Mordor-004 "Rhunske problemy" (Rhun troubles)
   - Mordor-005 "Toulky divocinou" (wandering the wilds)
   - Not in the journal.
2. **Givers:**
   - Burlug `c_h_poskok_bratrstva`, an orc vendor, comment "at the Brotherhood's house in Rhun" (kvest0001NPC.scp:792-835).
   - Liguk `c_h_poskok_Baradske_kasarny`, Barad-dur barracks, comment "3670,2724" (kvest0001NPC.scp:850-893).
   - Barad-dur palace guard `C_m_kvest0001_guard_01` and captain `C_m_kvest0001_Kapitan_straze`.
   - **No NPC "Reyla" or "Akhbar" exists anywhere in the archive.** All are GM-placed and have no spot.
3. **Who:** no realm, level or skill checks in code. The setting is Mordor (orc NPCs, Barad-dur). Burlug's turn-in has no once-only guard: `kvest0001dokoncen` is set, but the branches do not check it before paying.
4. **Story / steps (Mordor-002, the only playable one):**
   - Young orcs robbed the old jeweller Akbhaar Shakis. A task scroll `i_kvest0001_zadani` describes it, but no script hands that scroll out.
   - Kill Harghalagh's gang: orcs `c_kvest_orc_nevychovanec_01..04`. Each drops one of Akhbar's necklaces `i_Akhbaruv_retizek_0N`. Harghalagh (04) also drops a letter, `i_kvest_Harghalaghuv_dopis`, that points to the Bagrong camp south of Barad-dur.
   - Kill Urghalagh's gang at Bagrong: orcs 05-09 drop necklaces 05-09. Urghalagh's diary `i_kvest_Urghalaghuv_denik` points to the ogres of the Nasgard pass, **but no NPC drops the diary**.
   - Kill the ogres `c_kvest_zlobr_01..03`; Barghlin (03) is the leader. His death message says "if you have everything, go back to Reyla" (kvest0001NPC.scp:789).
   - Bring Reyla's red dress `i_kvest_Reyliny_saty` and the necklaces to Burlug. **No NPC drops the dress either.** It is craftable instead: `SKILLMAKE=TAILORING 33.6` (kvest0001.scp:452).
   - Mordor-003: Burlug then asks the player to carry a medicine to his brother Liguk in the Barad-dur barracks (kvest0001.scp:571-583). No item is ever given, and Liguk's speech `spk_poskok_barad_kasarny` is empty (kvest0001.scp:615).
5. **Rewards (Burlug, kvest0001.scp):**
   - 400 exp for the dress plus all 9 necklaces (505).
   - 10 exp per necklace (510-543).
   - 200 exp for the dress alone (546).
   - No items.
   - Side props (not rewards, special effects):
     - Cauldron `I_hrnec_z_roztokem_muznosti_kvest0001` (43-68): anyone with an empty bottle can fill a "potion of manliness" `i_bottle_rostok_muznosti`.
     - Drinking that potion (71-98) **permanently** sets Str +10 and Int -30, plus `vit+10`, a non-standard property.
     - Trap skull `I_okrasna_lebka_kvest001` (101-118): 100-500 god damage on equip or double-click.
     - Floor-trap typedefs `T_nastrazna_past_001..003` (709-815): 5-20 / 20-45 / 40-90 damage, disarmed with Tinkering 30/50/80.
6. **Quest-only items and monsters:**
   - Items: 9 necklaces, task scroll, letter, diary, dress, `I_lek_pro_Liguka` (drinking it teleports to 3003,506,20).
   - Orc gang `c_kvest_orc_nevychovanec_01..09` (STR 200-300, ~15-20 exp each); Harghalagh and Urghalagh are named.
   - Ogres (`c_giant`, ARMOR 200-300, 120-150 exp); Barghlin is the leader.
   - Liche Sarfeos `c_m_kvest001_Liche_Sarfeos` (Mordor-004).
   - Ogre Lurgug `c_kvest0001_Lurgug` (Mordor-005).
   - Mordor-001: Marghul's palace guards; the captain is supposed to issue passes.
7. **Systems:** speech keywords, kill drops, item-trigger traps, teleports.
8. **Shape:** kill/fetch, medium (Mordor-002); the rest are stubs.
9. **Broken or unfinished:**
   - **Burlug's turn-in condition is bugged.** It tests `(<i_Akhbaruv_retizek_01>)` etc. (kvest0001.scp:503, 508), which evaluate the defname rather than an inventory check, so they are always true. As written:
     - the dress alone already pays 400 exp, and the necklaces are never checked;
     - without the dress, every greeting pays 10 exp, which is repeatable;
     - the "dress only", "letter" and "diary" hint branches and the default greeting can never run.
   - The captain's speech `spk_kvest0001_Kapitan_straze_Barad`, Sarfeos's event `e_Lihe_Sarfeos` and his speech `spk_kvest001_Liche_Sarfeos` are not defined anywhere.
   - Mordor-004 and 005 consist of a single monster def each.
   - **The bribe to the palace guard is free.** The guard checks that the player carries 50k or 100k gold but never takes it (kvest0001.scp:153-165). Offering "10" twice sets the outcast tag `src.TAG.vyvrhel=1` (143).
- **Port:** items/NPCs converted only (`converted/questy/kvest0001*.def`).

## kvest0002 + kvest0002NPC: "Gondorske kvesty" (Gondor quests 001-005)

1. **Files:** `kvest0002.scp`, `kvest0002NPC.scp`. Headers list five Gondor quests, all empty:
   - Gondor-001 Problemy v Minas Tirith
   - Gondor-002 Gondorske problemy
   - Gondor-003 Rohanske problemy
   - Gondor-004 Krajske problemy
   - Gondor-005 Elfove
2. **NPCs:**
   - Bard Kardusaj `c_h_quest_postava_0007`: "anywhere in Minas Tirith". He sells jokes, verses and quotes for 5 gold, and also carries q020's bard speech.
   - Citadel guards `C_m_vypravec_0001`: comment "two in front of the gate to the royal gardens and citadel, 5840 3874 20".
3. **Who:** n/a.
4. **Content:**
   - Only the citadel access system exists. The guard checks for passes: the one-use pass `i_propustka_bila_vez_jednorazova` is swapped for a medallion `i_medajlon_propusteni`, and the permanent pass `i_propustka_bila_vez_stala` works as is (kvest0002.scp:151-179). The guard then teleports the player to 5835,3875,25.
   - The passes are "signed by Trungulin", but nobody hands them out.
5. **Rewards:** none.
6. **Quest-only items:** the passes (also used by kvest0001's Mordor guards), the medallion.
7. **Systems:** speech plus teleport.
8. **Shape:** none (framework/props), small.
9. **Broken:** all five quest sections are empty headers.
- **Port:** items/NPCs converted only.

## kvest0003 + kvest0003NPC: "Neutralni kvesty" (neutral quests 001-010)

1. **Files:** `kvest0003.scp`, `kvest0003NPC.scp`. Neutral-007 "Inskripce" was moved into q007. The parts that contain anything:
   - **Neutral-001 Vampiri pribeh (Vampire story):** props only.
     - Silver dagger `i_Vampiri_Pribeh_dyka` (DAM 8, created as mithril).
     - Strobe-light runes.
     - GM trapped chest `I_Truhla_Past` / `T_truhla_Past`.
     - Bookcase "Knihovna noci", which deals 120 damage and shows a scroll.
     - Umbar pirates as flavour NPCs.
   - **Neutral-002 Hrobky Cernych Numenorejcu (Tombs of the Black Numenoreans):** pirate Grager `c_kvest003_Grager` in Umbar port sells a rumour for 10000 gold (kvest0003.scp:69-78): "the Haradric Shah's daughter is missing".
   - **Neutral-004 Hora plamene (Fire mountain):** dialog `D_Duch_Alrondhirona` and its speech only. No NPC uses them.
   - **Neutral-006 Zlata otep (Golden Sheaf):** playable, details below.
   - **Neutral-008 Drak (Dragon Degran):** playable as a GM event, details below.
   - Neutral-005 "Irbisova vejce" lives in kvest0004NPC (see below).

### Neutral-006 Zlata otep (Golden Sheaf)
2. **NPCs:** Pan Zvirat ("Lord of Beasts") `c_Pan_zvirat`; the mustang teleports the player to 2455,855, presumably next to him. The Golden Mustang `Zlaty_mustang` has no spot. Both are GM-placed.
3. **Who:** anyone can finish. The real reward goes to **rangers only**. The taming boost is once only (`TAG.GOTGOLD`).
4. **Steps:**
   - Find the Golden Sheaf `Zlata_otep`. It is GM-placed; no loot table drops it. Double-clicking it sets `TAG.GOT`.
   - Talk to the Golden Mustang, who teleports the player to his master.
   - Pan Zvirat: rangers and thieves are sent to his house at 2456,839,7, where the elixir `I_port_tamer` stands. Everyone else gets 500 exp and is teleported to 3003,506,20 (kvest0003.scp:209-225).
5. **Rewards:**
   - Non-ranger/thief: 500 exp (kvest0003.scp:223).
   - **Ranger** drinking the elixir: `taming += (130.0 - taming)/10` plus 500 exp (kvest0003.scp:266-268). This is a permanent skill gain, for example 100.0 to 103.0. **Special: a permanent skill raise above the normal cap.** A thief is sent to the house but cannot drink.
6. **Quest-only items:** `Zlata_otep` (its type `t_otep` is undefined), `I_port_tamer`.
7. **Systems:** speech, teleports, a skill raise.
8. **Shape:** fetch/talk, small.
9. **Broken:**
   - `TAG.GOT` is never cleared and the non-ranger branch never sets `GOTGOLD`. A non-ranger can therefore repeat mustang to Pan Zvirat for 500 exp each time, which is an exploit.
   - The thief branch is a dead end.

### Neutral-008 Drak Degran (dragon)
2. **NPCs:** boss wyrm "Degran nicitel" `c_drak_degran` (DAM 20-50, berserk brain) and his guard `c_h_strazce`. When hit, the guard tells how to kill Degran (speech `joptravel`). Both are GM-placed.
4. **Steps:**
   - Collect the dragon heart `i_srdce`, the dragon blood `i_krev` and the dead dragon sword `i_kamen`. The lore says they are "at the bottom of the Mount Dorlas, Dol Guldur and Beridor dungeons", but nothing drops them, so they must be GM-placed.
   - Double-click the dead sword to forge "Draci mec zkazy" `i_sword_zkazy` (kvest0003.scp:416-425).
   - Kill Degran. A gate `i_gate_exp2` opens; stepping in gives 1000 exp and teleports to the lair at 5445,655,2 (kvest0003NPC.scp:411-418; kvest0003.scp:428-441).
5. **Rewards:**
   - Degran's loot: `loot_DAEMON` plus 3000-5000 gold (kvest0003NPC.scp:422-423).
   - 1000 exp from the gate.
   - **Special:** the Dragon Sword of Doom `i_sword_zkazy` (kvest0003.scp:338-365):
     - DAM 20000, HITPOINTS 1 (breaks after one hit, per the lore), newbie-flagged;
     - on equip: Str +1000, Dex +1000, invulnerable; removed on unequip;
     - `@Damage: src.kill=1`.
   - It is a one-shot GM-event weapon.
8. **Shape:** boss + craft, medium (GM event).
9. **Broken:** the section label is inconsistent (007 vs 008). The three parts have no source.
- **Port (all of kvest0003):** items/NPCs converted only.

## kvest0004 + kvest0004NPC: "Clenstvi v ruznych uskupenich a vyznani" (Order memberships) + Neutral-005 Irbisova vejce (Irbis's eggs)

1. **Files:** `kvest0004.scp`, `kvest0004NPC.scp`.
   - The header lists 11 order or faith membership quests (Orome's servant for druids and rangers, Aule for dwarves, Tulkas, Ulmo, Arien, Mandos, Manwe, Yavanna, Varda, Lorien, Morgoth). **None is implemented.**
   - kvest0004.scp contains only experiments:
     - wand "Druid ucen" `I_kristal_Druid_ucen`: transforms a mage into a red dragon or a necromancer into a lich for 40 s, Str +100 / Dex +50 / Int -50;
     - race-pick and cleric-spell test dialogs from a public script.
2. **Irbis's eggs:** breeder Irbis `c_irbis` and his friend Hrobeus `c_Hrobeus`. GM-placed, no spot.
3. **Who:** anyone.
4. **Steps (intended):**
   - Irbis asks for three rare eggs and sends the player to Hrobeus.
   - Hrobeus gives positions (placeholders "xxxx.xxxx") and passwords.
   - Kill the female toad `crunch_zabka`, the female lava serpent `crunch_had` and the female harpy `crunch_harpy`. They drop the eggs `i_Crunch_vejce_01..03`.
   - Return the eggs to Irbis.
5. **Rewards:** 500 exp per password (kvest0004NPC.scp:236, 243) and 1500 exp for all three eggs (295). No items.
8. **Shape:** kill/fetch, small.
9. **Broken:**
   - `[SPEECH Irbis]` has no `ON=` trigger lines (kvest0004NPC.scp:288), so it never fires.
   - Hrobeus's chardef uses `TSPEECH=Hrobus`, but the speech is named `Hrobeus`, so it never fires either.
   - The egg positions are placeholders.
   - The membership quests are a header only.
- **Port:** items/NPCs converted only.

---

## q006: Erioluv vnuk (Eriol's grandson)

1. **Journal:** row 22, "Erioluv vnuk", qtag `q006`.
   - Files: `q006_erioluv_vnuk.scp`, `_chars.scp`, `_items.scp`.
   - Also uses NPCs from `q023_kouzelna_dyka.scp` (shaman Qworet, wizard Unas) and from `q007_HaradirovoSelatko.scp` (the innkeeper).
   - The potion ingredients come from `krvave_prizraky.scp` (bird `c_crunch_krvavec`, which drops a heart `i_srdiczko` defined in `enrico.scp:647`) and `velmistr.scp:1236` (rare herb `i_bylina`).
2. **NPCs** (all GM-placed; positions from the GM info dialog, q006_erioluv_vnuk.scp:739-749):
   - Eriol `c_q006_eriol`, title "Ranhojic" (healer).
   - Shaman Qworet `c_q023_ShamanQworet` at 2893,908, "near the Lonely Mountain and the mines".
   - Wizard Unas `c_q023_CarodejUnas` at 3769,1490, "near the Rhun marshes".
   - Innkeeper `c_hospod` at 164,2150.
3. **Who:** both realms. Unas only cooperates at **level >= 15** (q006_erioluv_vnuk.scp:646). The final crafting branch needs **Alchemy 100**. Once only, except that the robe can be bought again (see rewards).
4. **Steps:**
   - Eriol is searching for his daughter's lost son. Bring 10 torches, then a freshly tamed, shrunk dog `i_pet_dog`.
   - The boy is found but pale. Bring 3 lesser cure potions, then 10 greater cure potions.
   - Bring 10 grizzly thighs (collector bag `i_q006_vacek_stehno`, 75% per corpse) and 3 greater refresh potions.
   - The boy raves about wraiths. Talk to shaman Qworet (no help), then go to wizard Unas.
   - Unas wants 20 essences each from ice, stone, earth and snow elementals, collected in four successive collector bags (65/60/55/50% per corpse). He then hands over a parchment: the boy met **barrow-wights**.
   - Show the parchment to the innkeeper, friend of the alchemist Terlak. He names the "Potion of Purification" `i_potion_ocisteni`: 2 blood-bird hearts, 3 greater cures and the rare herb (`SKILLMAKE=ALCHEMY 100.0`).
   - Bring the ingredients to Eriol. Without Alchemy 100 the quest ends here ("ending without robe"). An alchemist brews the potion and hands it in for the robe.
5. **Rewards** (q006_erioluv_vnuk.scp unless noted):
   - Exp along the way:
     - torches 50 (402), dog 100 (416), 3 lesser cures 50 (430), 10 greater cures 125 (444), thighs and refresh 200 (460);
     - Qworet 50 (q023_kouzelna_dyka.scp:146-147);
     - sent to Unas 100 (472);
     - Unas 4 x 500 (673, 681, 689, 698);
     - sent to innkeeper 300 (478);
     - innkeeper 300 (q007_HaradirovoSelatko.scp:1190);
     - accept the potion task 100 (486);
     - hand in the ingredients 2000 (495) **or** start brewing 2000 (502).
   - **Total about 5375 exp. No gold.**
   - Final reward: **Robe of the Alchemist `i_roba_alchemisty`** (519-521, tagged with the owner's uid). Buyable again for **2,000,000 gold** at the final stage with Alchemy >99.9 (527-532).
   - **Special.** Item def at q006_erioluv_vnuk_items.scp:142-181:
     - on equip by the **owner** with Alchemy >99.9: **+10.0 Alchemy**, i.e. 100 to 110;
     - removed on unequip;
     - a robe without an owner tag rejects everyone;
     - click text: "+ 10% ke skillu Alchemy".
   - The archive has no recipe above Alchemy 100.0, so the gain is success and quality headroom rather than new recipes.
6. **Quest-only items and monsters:**
   - 5 collector bags, the parchment `i_q006_pergamen` "Mohylovi duchove" with lore text, the Potion of Purification (it has no effect of its own).
   - A decoy bird `c_krkavec` named "Krvavec" that drops no heart. The GM note calls it "fake"; the real heart source is `c_crunch_krvavec`.
7. **Systems:** quest dialog framework (`questDialog_*`), collectors, shrunk-pet check, crafting (alchemy), level gate, cross-quest NPC hooks.
   - Eriol's dialog also serves q026's healer hook and the rank-promotion hook (`f_q000_shouldBePromoted`).
8. **Shape:** mixed fetch chain + talk + craft, **large**.
9. **Broken:**
   - `src.consume.(i_pet_dog)` (415) is suspicious syntax; it may not consume the dog.
   - The rest is clean. A GM info page exists (723-760).
- **Port:**
  - converted only;
  - plus `scripts/Basic/Potions/CurePotions.cs:20-30` registers `i_potion_ocisteni` as a potion that does nothing ("q006's potion: its worth is to the quest, which isn't ported");
  - Qworet and Unas are converted in q023 defs.

## q007a: Haradirovo selatko (Haradir's piglet)

1. **Journal:** row 6, qtag `q007_haradirovo_selatko`.
   - Files: `q007_HaradirovoSelatko.scp` (3227 lines; also holds q007b/c, the innkeeper's q017 and q006 branches, and Rodrigo), `_chars.scp`, `_items.scp`.
   - Credits: Sir Glorg, original idea by Noe.
2. **NPCs** (all GM-placed, no spots in the archive; locations from dialog text only):
   - Haradir `c_Haradir`: a hunter "in the north".
   - Innkeeper `c_hospod`: "Ice Island" per Rachel's dialog; q006's GM note says 164,2150.
   - Witch Rachel `c_rachel`: "on the Sun Islands this season", per Rodrigo.
   - Boss Evil Dryad `c_Zla_Dryada`: on the innkeeper's island.
   - Optional: pirate Rodrigo `c_rodrigo`, in the desert near Umbar.
3. **Who:** anyone, no checks. Once only.
4. **Steps:**
   - Haradir: the innkeeper owes him a smoked piglet and denied it. The player promises to collect it.
   - The innkeeper denies everything. Haradir suggests the witch Rachel, who has influence over him.
   - Rachel wants **1000 bat wings** `i_reag_Batwing` and then "tells" the innkeeper. The joke: claiming she sent you is all it takes.
   - The innkeeper confesses: an evil Dryad terrorises the island and takes his food. Kill her and bring **her bow** `i_lukdryady` as proof.
   - He gives the piglet `i_selatko`; deliver it to Haradir.
   - Haradir then tells two stories that unlock q007b (Old Inscripter) and q007c (Ancient Smith).
   - Optional: Rodrigo sells rumours for 10k gold (8k after haggling) about the stolen stones' location ("pirate island in the north") and Rachel's whereabouts. He also offers "adopt a dolphin" for 50000 gold, which pays 200 exp.
5. **Rewards:**
   - 500 exp from the innkeeper for the bow (q007_HaradirovoSelatko.scp:1149).
   - 500 exp from Haradir (597).
   - Dolphin adoption 200 exp for 50000 gold (3152).
   - No item reward. The "reward" is access to q007b and q007c.
6. **Quest-only items and monsters:**
   - Items: `i_selatko`, `i_lukdryady` (a real bow with DAM 4 and 5000 hit points; it could be equipped).
   - **Boss "Zla Dryada"** `c_Zla_Dryada` (q007_HaradirovoSelatko_chars.scp:411-475):
     - 4000-5000 HP, STR 1000, DEX 400, Archery/Wrestling/Tactics/Resist 150, ARMOR 120, DAM 80;
     - rides a forest ostard, paralysis resist 950;
     - drops the bow plus 400-700 arrows.
7. **Systems:** gump dialogs (old style), boss monster, sea travel between islands. Innkeeper extras: drinks with a cooldown, and he also hosts q017 and q006 steps.
8. **Shape:** talk/fetch/boss, medium-large (8 stages).
9. **Broken / unfinished:**
   - `qtag(q007_haradir_Nemethen,1)` (235) is set, but the Nemethen thread never continues.
   - Author note at lines 10-14: an idea to earn a second inscription book via Rodrigo's dolphin tag `q007_delfin`. It is not implemented, and the dolphin tag only changes Rodrigo's menu.
- **Port:** converted only.

## q007b: Kniha inskripce (Inscription book) and Magicke pero (magic pen)

1. **Journal:** row 7, qtag `q007_stary_inscripter`; it lives in `q007_HaradirovoSelatko.scp:2050-2711`.
2. **NPCs:**
   - Old Inscripter `c_maginscript`: "in his cellar somewhere in the Northern Waste" (Haradir's story, 675).
   - Drunk mage apprentice Divous `c_divous`: no location given.
   - Both GM-placed.
3. **Who:** q007a completed (qtag 8) and **Inscription > 79.9** (2074). Once only.
4. **Steps:**
   - Bring the three stones: Meteorite `i_meteorit`, Moon stone `i_mesicni_kamen`, Blood stone `i_kamen_krve`.
   - Their only source in the archive is the template `TM_poklad_piratu` (q007_HaradirovoSelatko_items.scp:160-175). It is marked "nepouzivat!" (do not use) and contains 1 meteorite, 1 moon stone and 0-1 blood stone. The dump `temporary_arjan.scp` shows them sitting in containers, so they were GM loot.
   - The Inscripter writes the book.
   - Optional: drunk Divous wants the book and offers his "magic pen". The trade is one-way; he refuses to trade back.
5. **Rewards:**
   - 2000 exp plus **Kniha Inscription 8 kruhu** `i_book_inscription` (2393-2398).
   - Or, after the trade, **Magicke pero** `i_magicke_pero` (2614-2615); the book is consumed.
   - **Both special:**
     - **Book** (items 116-130): the craft menu requires it to open the **8th-circle scroll menu** (`systems/craftmenu/inscrip.scp:88`, `TESTIF (src.findid(i_book_inscription))`). Without it nobody can inscribe 8th-circle scrolls.
     - **Pen** (items 133-155, also `sphereitem_tinker.scp:48-74`): with **Inscription >= 85.0** it turns an "inactive rune" `i_rune_tinker` into a working recall rune `i_rune_marker`. The inactive rune is crafted with Tinkering 99 from 1 adamantinum ingot. The pen is newbie-flagged.
   - The trade is a real design choice: the 8th-circle scrolls versus the ability to make recall runes.
6. **Quest-only items:** the 3 stones, which q007c also needs, so the two quests compete for them.
7. **Systems:** dialogs, a crafting-menu gate, tinkering runes, GM loot.
8. **Shape:** fetch, small (but the stones are scarce).
9. **Broken:** the journal text claims the pen came from "Rodrigo" (questmenu.scp), but it comes from Divous.
- **Port:**
  - converted only;
  - the 8th-circle scroll defs in `scripts/Basic/skills/Magery/scrolls.def:628-705` have the `t_book_inscription` requirement commented out by the converter;
  - `scripts/Basic/skills/Crafting/Inscription.cs` has no book check, so the gate the book unlocks does not exist in the port today.

## q007c: Adamantinove kladivko (Adamantine hammer)

1. **Journal:** row 8, qtag `q007_prastary_kovar`; in `q007_HaradirovoSelatko.scp:1617-2043`.
2. **NPC:** Ancient Smith `c_kovarskej`, "goes off towards the Iron Mountains" (Haradir's story, 676). GM-placed. The smith himself wears the hammer (chars 211).
3. **Who:** q007a completed and **Blacksmithing > 99.9** (1641). Once only.
4. **Steps:** prove your mastery, then bring the same three stones as q007b.
5. **Rewards:**
   - 2000 exp plus **Adamantinove kladivko** `i_adamantinum_kladivko` (1961-1966).
   - **Special.** Item def at items 82-113:
     - a smith's hammer, HP 60, auto-repairs at 0 on equip;
     - on equip with Blacksmithing > 99.9: **+10.0 Blacksmithing**, removed on unequip;
     - it works as the blacksmithing tool (`@targon_item` calls `blacksmithing_targon_item`).
   - Its message says a GM smith can now forge **mithril**. Mithril armor indeed requires `BLACKSMITHING 110.0, TINKERING 100.0` (`brneni/9mithril_armor.scp:16` and following), so **this hammer is the key to mithril armor**.
6. **Quest-only items:** the stones.
7. **Systems:** dialogs, crafting skill thresholds above 100.
8. **Shape:** fetch, small.
9. **Broken:** none found.
- **Port:**
  - converted only; the def exists as a plain `ColoredWeaponDef` without the bonus (`converted/questy/q007_HaradirovoSelatko_items.def:63`).
  - Whether mithril recipes in the port need 110 was not checked.

## q008: Black Mustang

1. **Not in the journal.** Uses plain tags `q008_black*`. Files: `q008_black.scp`, `_chars.scp`, `_items.scp`. Credits: Sir Glorg, idea by Jandor.
2. **NPC:** old horse breeder Ancam `c_q008_old_man`. Comment: house at 571,3547,10. Monster spots (q008_black.scp:11-16):
   - snow elemental and ice eye at 106,2099;
   - snow eye at 1005,101,5;
   - ice elemental at 955,112,10.
   - The GM must set `tag mustspawnid` on Ancam, pointing to the spawn of a black mustang.
3. **Who:** **rangers only**, with **Taming >= 95.0 and Animal Lore >= 95.0** (q008_black.scp:50, 81).
   - Rude answers ban the player for good (`q008_black_rejected`).
   - **Repeatable:** after a successful tame Ancam offers a new crate when a new mustang has spawned (dialog 10, 796-806).
4. **Steps:**
   - Ancam refuses to sell his black stallion but will let you try to tame one if you bring the herbs needed to raise another.
   - Take his crate `i_q008_benda_regy`. Kill the four quest monsters on the snow islands: Snow Elemental, Ice Elemental, Snow Eye, Ice Eye. Each sets a kill tag on death.
   - Fill the crate with reagents: **any `t_reagent` counts**, totalling 2500 or more, plus one more deposit after that (q008_black_items.scp:37-46).
   - Return the crate. When the mustang is present, the corral gate opens. Stepping on the inner pad closes the gate behind you ("no way back").
   - Tame the black mustang. Leave via the outer pad.
5. **Rewards:**
   - 400 exp (q008_black.scp:518, only on the "mustang not there yet" path).
   - 100 exp for entering the corral (q008_black_items.scp:160-161).
   - **The tamed black mustang `C_A_MUSTANG_BLACK`.**
   - **Special: a magic mount** (`chars/mounts.scp:1279-1320`): STR 1200, DEX 150, `tag(nores,1)`, `e_noport`. Its mount item gets the `black_mif` powers (`systems/jezditka.scp:725-731`):
     - +20 slayer vs demons, +20 vs spiders and animals, +20 vs humanoids (`imi_value` 101/104/103, which map to `daemonbonus`/`spiderbonus`/`humanbonus`);
     - +5 Hiding and +10 Stealth (`imi_value[21]`, `[47]`).
   - It is the strongest of the ordinary mustang tables, behind crimson's +25 and the zostriches.
6. **Quest-only items and monsters:**
   - Items: crate, corral gates `i_q008_vrata_1/2`, step pads `i_q008_pad_closeDoor_inside/outside`, `i_dlazka_rmpeacemade` (removes the peacemade sleep from the mustang).
   - 4 quest monsters `c_q008_elem_snow/elem_ice/eye_snow/eye_ice` (STR 450-660, 350-1100 HP).
7. **Systems:** spawn-memory link to the mustang spawn, taming, step-trigger floor pads, a door that locks behind you, kill tags.
8. **Shape:** kill + collect + tame, medium. Repeatable.
9. **Broken:**
   - The 400 exp is skipped on the "mustang already present" path (dialog 7, 596-603).
   - "Only one chance" is promised but not enforced.
   - The reagent check accepts any reagent and ignores the "herbs from snow islands" story.
- **Port:**
  - converted only;
  - but **the reward mount already works**: `C_A_MUSTANG_BLACK` is converted (`converted/chars/mounts.def:1010`) and its powers are ported (`scripts/Basic/MagicItems/MountPowers.cs:26`, #925).

## q009: Mistr krejci (Master tailor)

1. **Not in the journal.** Uses plain tags `q009_krejci`, `q009_krejci_ringtime`. Files: `q009_mistr_krejci.scp`, `_chars.scp`. Credits: Sir Glorg, idea by GM Stin.
2. **NPC:** tailor Talathwen `c_q009_krejci`, a vendor (`VENDOR_S_TAILOR`). GM-placed, no spot.
3. **Who:** anyone; the ring is only usable at Tailoring 100. Once only (tag 3).
4. **Steps:**
   - Her shop is going bankrupt. Flatter her ("your dress can't be the ring's work alone") to unlock the family ring. Answering "my shop matters more" just ends the conversation.
   - Bring a **cashier's check worth exactly 5,000,000 gold** `i_cashiers_check` with `more1 == 5000000`.
   - Come back "tomorrow" for the ring.
5. **Reward:**
   - **Tailor's Ring `i_q009_ring_tailor`** (q009_mistr_krejci.scp:383-384, owner-bound via `tag.ownerid`). No exp.
   - **Special.** Item def at 456-489:
     - on equip by the owner with **Tailoring > 99.9**: **+10.0 Tailoring**, removed on unequip;
     - the owner below GM tailoring gets "not enough experience" and the ring bounces;
     - anyone else sees "it doesn't fit, it's engraved with <owner>".
   - No tailoring recipe above 100 was found in the archive, so the gain is quality and success headroom. Her text promises "more precise work, stronger, better cloth".
6. **Quest-only items:** none besides the ring.
7. **Systems:** cashier's check, a real-time wait, owner binding.
8. **Shape:** buy + wait (a gold sink), small.
9. **Odd design:**
   - The ring is ready 86400 `serv.time` units after payment. The comment says "tomorrow"; whether the unit is seconds or tenths of a second cannot be determined from the archive.
   - **Every early visit adds 10800 units** (33), and pressing "*Odejit*" (leave) subtracts 3600 (327). Nagging delays the order.
- **Port:** converted only. The cashier's check exists in the port (`scripts/Basic/Economy/CashiersItem.cs:20`).

## q010: Mastickar Gregoriel (the ointment maker)

1. **Journal:** row 9, qtag `q010_mastickar`. Files: `q010_mastickar.scp`, `q010_mastickar_npc.scp`.
2. **NPC:** Gregoriel `c_q010_mastickar_mastickar`, a healer-vendor that also resurrects (`e_healer`, `spk_healer`). GM-placed, no spot.
3. **Who:** anyone. The player must answer "I like ointments" to get the task; insulting him only ends that conversation. Once only.
4. **Steps:**
   - His new wound-healing ointment needs a plant digested in the stomach of the rare **Medved Zabijak** ("killer bear").
   - Kill the bear and put its stomach in the collector bag (1 item, 100%).
   - Return.
5. **Rewards:** 150 exp plus 1500 gold (q010_mastickar.scp:187-188). No items.
6. **Quest-only items and monsters:**
   - Collector `i_q010_mastickar_kolektor`.
   - Killer bear `c_q010_mastickar_medved`: red grizzly, 330-380 HP, 30% bleed, stun.
7. **Systems:** quest dialog framework, collector, a special monster; also the q026 and rank hooks.
8. **Shape:** kill + collect, small.
9. **Broken:** none.
- **Port:** converted only.

## q011: Prekupnik Hruon / Akranon (the fence)

1. **Journal:** rows 10 (Hruon, Gondor) and 11 (Akranon, Mordor), plain tags `gq011_prekupnik`/`mq011_prekupnik` and `..._neuspel`. Files: `q011_prekupnik.scp`, `_chars.scp`.
2. **NPCs:** Hruon `c_gq011_prekupnik` (Gondor) and Akranon `c_mq011_prekupnik` (Mordor). GM-placed, no spots.
3. **Who:**
   - Hruon talks only to realm 1 and Akranon only to realm 2. Having started with one locks you out of the other.
   - Refusing mid-task bans the player for good (`_neuspel`).
   - **Repeatable:** at stage 6 the "take the first task" button restarts the chain.
4. **Steps** (three collector tasks):
   - 12 skeleton skulls ("Body of Kostlivec", 75%).
   - 10 tails of "Lity vlk" (fierce wolf).
   - 8 mandibles: **Terathan Avengers** for Gondor or **Skorpion Princ** for Mordor (62.5%).
5. **Rewards per run (Gondor / Mordor identical):**
   - 200 exp + 2000 gold (q011_prekupnik.scp:327-329 / 604-606).
   - 400 exp + 5000 gold (338-340 / 615-617).
   - 600 exp + 7000 gold (349-351 Gondor; 642-644 Mordor).
   - Total 1200 exp and 14000 gold per run. No items.
6. **Quest-only items:** 4 collector bags.
7. **Systems:** collectors, realm gating.
8. **Shape:** kill + collect chain, medium. Repeatable.
9. **Broken:**
   - Hruon's journal "complete" call `f_quest_complete(10)` sits only on the scorpion branch (365-368), which needs realm 2 and is therefore unreachable for Hruon. Gondor completions are never counted.
   - Both dialogs carry dead branches for the other realm.
- **Port:** converted only.

## q013: Experimentator Theorius (Gondor) / Theorianulus (Mordor)

1. **Journal:** row 12, qtags `gq013_experimentator`/`mq013_experimentator`. Files: `q013_experimentator.scp`, `_chars.scp`.
2. **NPCs:** Theorius `c_gq013_experimentator` and Theorianulus `c_mq013_experimentator`, eccentric ointment makers. GM-placed, no spots.
3. **Who:** each NPC serves one realm. An unfinished other-realm version blocks; a failed one does not. Saying "I don't care" bans the player for good (tag 3). Once only.
4. **Steps:** take the bag and fill it with **10 brains of "Zlobr Valecnik"** (ogre warrior; collector, 87.5%). Return it.
5. **Rewards:** 700 exp plus **`i_mi_bag`** (q013_experimentator.scp:150-152 Gondor, 314-316 Mordor).
   - **Special-ish:** a random basic-tier magic item with 2 properties, rolled when the bag is opened.
6. **Quest-only items:** collector `i_q013_experimentator_kolektor`.
7. **Systems:** collector, magic item generator; also the q026 and rank hooks.
8. **Shape:** kill + collect, small.
9. **Broken:** none. The Mordor block message at line 197 lacks `return`, which is harmless.
- **Port:** converted only. The reward bag is ported.

## q015: Paradnice Minny (Minny the show-off)

1. **Journal:** row 13, plain tags `q015_paradnice`/`q015_neuspel`. Files: `q015_paradnice.scp`, `_chars.scp`.
2. **NPC:** Minny `c_q015_paradnice`, a vain wife. GM-placed, no spot.
3. **Who:** **not realm 2** (Mordor is refused at q015_paradnice.scp:43-46). Refusing bans the player for good. Once only.
4. **Steps:** she wants a necklace of **12 Poison Phoenix feathers**. Collector beads; the target is "Body of Phoenix" in colour 097F, 87.5% per corpse. Bring them back.
5. **Rewards:** 1000 exp plus 2500 gold (174-176). No items. She does not even give the necklace.
6. **Quest-only items:** collector `i_q015_paradnice_kolektor`.
7. **Systems:** collector with a colour-filtered target (`f_collectorFixTargetId`).
8. **Shape:** kill + collect, small.
9. **Broken:** none.
- **Port:** converted only.

## q016: Prastary poklad (Ancient treasure)

1. **Journal:** row 14, qtag `q016_prastary_poklad`. Files: `q016_prastary_poklad.scp`, `_chars.scp`, `_items.scp`. Credits: Sir Glorg, idea by Crunch.
2. **NPCs** (GM-placed; coordinates from comments):
   - Dying necromancer `c_q016_StaryNegr`, in a spider cave, 3366,3819.
   - Mage Orillus `c_q016_Orillus`, in a Mordor fortress south-east of Barad-dur behind titans, 3796,2859,6.
   - Drunk cleric Andor `c_q016_Andor`, in Minas Morgul, 3200,2971,20.
   - Placed items: the water barrel `i_Crunch_voda` at a lake above a waterfall, up the river north of Harad; the chest `i_Crunch_truhla_s_chlastem` in an Osgiliath cellar; the gate `i_Crunch_magicka_brana` in the Nurn forests; the coffin `i_Crunch_rakev`.
3. **Who:** anyone, no checks. The exp depends on realm and choice. Once only.
4. **Steps:**
   - The dying necromancer tells of a treasure hidden in a tomb and asks only to hear whether it still shines. Refusing makes him kill you and himself (`i_q016_delayedKill`).
   - Orillus will reveal the dungeon if you bring water from the healing lake: fill an empty bottle at `i_Crunch_voda`. Refusing ends the quest for good.
   - He reveals the entrance in the Nurn forests behind a magic gate that needs a blessing from the cleric Andor.
   - Andor is too drunk. Bring him the "exotic liquor" from a red chest in Osgiliath. He blesses you.
   - The magic gate lets a blessed player through and teleports to 3190,3640. Anyone else is killed.
   - The coffin is almost empty ("someone beat you to it"): 50000 gold. Anyone opening it at a wrong stage is killed.
   - **Moral choice:** tell the necromancer the truth (he dies heartbroken) or lie that the treasure is magnificent (he dies happy).
5. **Rewards:**
   - Orillus 150 exp (q016_prastary_poklad.scp:289).
   - Andor 100 exp (398).
   - Coffin 500 exp + 50000 gold (q016_prastary_poklad_items.scp:16-17).
   - Ending, truth: **1200 exp if Mordor, else 500** (q016_prastary_poklad.scp:157-163).
   - Ending, lie: **1200 exp if Gondor, else 500** (166-172).
   - Maximum 1950 exp plus 50000 gold. No items.
6. **Quest-only items:**
   - Items: healing water bottle, liquor, chest, magic gate, coffin, a delayed-death memory item.
   - No quest monsters (titans guard Orillus, according to the text).
7. **Systems:** dialogs, placed interactive items, teleport, instant-death traps, a realm-weighted moral choice.
8. **Shape:** mixed talk/fetch/exploration, medium.
9. **Broken:**
   - The "in progress" page reuses the necromancer's line "I don't believe a word, you never fought" as the player's **refuse** button. Pressing it gets the player killed (q016_prastary_poklad.scp:64-66).
   - Otherwise clean.
- **Port:** converted only.

## q1: Lecitel Ceren / Lavovy jester (healer Ceren / the lava lizard)

1. **Journal:** row 5 "Lecitel Ceren", plain tags `q1_lavovyjester`/`q1_neuspel`. Files: `q1_lavovyzostrich.scp`, `_chars.scp`.
2. **NPC:** healer Ceren `c_q1_lavovyjester_lecitel`. GM-placed, no spot.
3. **Who:** anyone. Rudeness bans the player for good. Once only.
4. **Steps:**
   - Ceren's wife died. Legend says the blood of a lizard born from lava can bring back the dead.
   - Find the fire island **Arda Nare** deep underground.
   - Kill a **Lavovy Jester** (lava zostrich) and draw its blood into the special flask (collector, 100%).
   - Return; the wife lives again.
   - The area has a damaging border tile `i_q1_lavovyjester_ohraniceni`: 5-10 fire damage per step near the lava edge.
5. **Rewards:**
   - 2000 exp plus **`i_mi_bag`** (q1_lavovyzostrich.scp:185-187).
   - Special-ish: a random basic magic item.
6. **Quest-only items and monsters:**
   - Collector flask `i_q1_lavovyjester_kolektor`.
   - Lava lizard `c_q1_lavovyjester_lavovyjester`: red zostrich, STR 300, all combat skills 100, Taming 200.
7. **Systems:** collector, a hazard floor tile, a hidden area. Arda Nare appears in `spheremap.scp`.
8. **Shape:** exploration + kill + collect, small.
9. **Broken:** typo `src.tag.q1__lavovyjester` (122, double underscore), which is always 0 and therefore harmless.
- **Port:** converted only.

## q14: Jeskyne poznani (Cave of Knowledge)

1. **Not in the journal.** Plain tags `q14`, `q14_2`. Files: `q14_jeskyne.scp`, `_chars.scp`.
2. **NPCs:** Dalmara `c_q14_dalmara` ("the clever woman"), Pepe Dungar `c_q14_pepe` and Malkor `c_q14_malkor`. Placed items: the live-water barrel `i_q14_sud`, the entry gate `i_q14_gate`, the reward pillar `i_q14_odmena` and the exit gate `i_q14_gate_vystup`. All GM-placed. The cave entrance is written on a scroll: 4613,16. Inside: 5250,54; the exit leads to 4612,13.
3. **Who:** anyone. Once only (q14 = 3). Each NPC is triggered by a spoken keyword:
   - "Bystra zena" (Dalmara);
   - "Dalmara" (Pepe);
   - "Jeskyne" (Malkor);
   - "Lahev" / "Dopis" (Dalmara, for replacements).
4. **Steps:**
   - Dalmara gives keyword scroll "Pisen mladi" and a letter to Pepe.
   - Pepe takes the letter and gives scroll "Experiment". For **10 gold ingots** he gives scroll "Zlate klasy" and a bag of **60 golden-wheat saplings** plus a scarecrow "Field owner: <you>", and sets `q14_2`. Saplings grow into plants after 18000 timer units; harvest **50 golden sheaves**.
   - Dalmara step 1: 100 spider silk, 100 nightshade, 100 blood moss, 300 bones and 50 sheaves give scroll "Material" and the blue receipt.
   - Dalmara step 2: blue receipt, 100 `i_log_iron` logs, 50 silver ingots, 30 **grey-horse** hides and a shrunk **pack llama** give scroll "Potrebna cutora", an empty flask and the red receipt.
   - Fill the flask at the "live water" barrel.
   - Dalmara step 3: full flask, both receipts, 500 gold, 20 greater heal and 10 poison potions give the antidote `i_q14_cutora` and scroll "Vzacny protijed".
   - Malkor gives scroll "Syn" early on. With the antidote he takes all the scrolls and gives the cave coordinates and scroll "Uzdraveni"; his son Tomy recovers.
   - Enter the cave through the gate (needs `q14=1` and `q14_2`) and click the stone pillar.
5. **Rewards:** 5000 exp, **4x `i_mi_bag`**, 80000 gold (q14_jeskyne.scp:168-173), plus a one-time visit to the cave.
6. **Quest-only items:**
   - 9 keyword scrolls, coordinates scroll, red and blue receipts, letter, saplings, plant, sheaf, flasks, antidote.
   - GM test template `tmp_q14_drevo` (443-452): a backpack holding the step-2 materials.
7. **Systems:** speech keywords, a farming/growth timer, shrunk-pet and hide-source checks (`i_hide.more2 == c_horse_gray`), gates and teleports.
8. **Shape:** big fetch/craft chain with farming, **large**.
9. **Broken / odd:**
   - The keyword scrolls are never used for anything ("use it where it must be used"); they are just collected and consumed.
   - The red receipt's text says chestnut logs and 100 silver ingots, but the code checks `i_log_iron` and 50 (68-76, 346).
   - Order is barely enforced. Only the gate checks `q14`/`q14_2`.
- **Port:** converted only.
