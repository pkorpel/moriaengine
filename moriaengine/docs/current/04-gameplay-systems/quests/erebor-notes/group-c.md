<!-- Research notes for #1235, written from the Erebor archive (pkorpel/erebor-sphere-archive, scripts/questy). File:line cites are archive paths. Summary: ../erebor-catalogue.md -->

**Corrections after the fact-check (the catalogue has these right):**
- q039: the warrior's longsword giving Fencing is right (on Erebor one-handed swords use Fencing, zbrane/IDs.scp:484); the mystic gets +20 Wrestling +10 Eval Int; the level gate is > 40.
- q040 kovar: the dragonslayer weapon needs one scale of each of 7 colours incl. gold (aldaron.scp:5053), and nothing drops `i_dragon_scale_gold`: the weapon can't be had, the armour can.
- q044: 7 bags, not 6 (pouch, bag, pouch + bag, pouch + high pouch, superhigh pouch).
- q099: `[function srazitlevely]` exists (systems/zakladskills.scp:59); the quest is unstartable only because nothing sets `tag.vykoupeni`.

# Erebor quests, group C: catalogue for the port

Source: the Erebor archive (Sphere 0.99z8 scripts; files are cp1250/CRLF, and q043_murdocovi_ucni.scp is UTF-8/LF). Line numbers refer to the archive files (stripping CR keeps the line numbers). Port = /workspace/moria-final/moriaengine/scripts. Research only: nothing was changed in any repo.

**Conventions**
- `gc/<file>` in a citation means a UTF-8 copy of `erebor-archive/scripts/questy/<file>`. The copy keeps the line numbers, so `file:line` citations hold for the archive too.
- "GM-placed" means no spawn or world spot for the NPC or static exists anywhere in the archive. The archive's save/sphereworld.scp contains none of this group's quest NPCs.
- The magic bags (`i_mi_pouch` / `i_mi_bag` + `_high` / `_superHigh`) each unpack into one random, unidentified magic item: a weapon, armour piece or accessory.
  - pouch = 1 property roll; bag = 2 rolls (the second roll has a 5/6 chance).
  - Metal tier 0 (plain): copper 500, iron 400, verite 200, valorite 100, obsidian 20, adamantium 10, mithril 1 (out of 1231).
  - Tier 1 (high): verite 300, valorite 200, obsidian 80, adamantium 30, mithril 1.
  - Tier 2 (superHigh): valorite 200, obsidian 80, adamantium 30, mithril 1.
  - Sources: systems/magicITEMS.scp:882-975, systems/magic_defs.scp:383-411. All six bags already exist in our port (`Basic/MagicItems/MagicBags.def`).
- `t_collector` = a quest bag or crate. You target a corpse with a given name ("Body of X") to cut a head or part from it, and the bag counts up to a maximum. The port has no C# implementation of this type.
- In every quest of this group, the converter turned the dialogs and functions into comments and the speech blocks into `SpeechStub`s. "Converted only" means the NPCDef and ItemDef exist under `scripts/converted/questy/` but none of the logic does.

**Quest-bound monster events still on zero-effect stubs** (`scripts/Basic/AbilitiesAndTalents/teventStubs.scp`), belonging to this group:
- q038 OCS: `e_ocs_capitan` (:467), `e_ocs_citizen` (:470), `e_ocs_freak` (:473), `e_ocs_guard_npc` (:476), `e_ocs_town_guard` (:479), `e_guard_ocs` (:183). Each is described in the q038 section.
- q039 Shadow weapons: `e_shadow_weapon_brother` (:572). It makes the 4 traitor bosses vanish 50-200 s after they first see a player.
- q040 Dragons: `e_q040_dragon_death` (:536). This is the kill credit: the player who opened the lair moves to the next stage.
- q042 / Halda Molle: `e_hatlend` (:189), `e_hm_pruvodce` (:208), `e_kaloc_help` (:233), `e_malica_help` (:258).
- The other quest-bound stubs from the #1022 plan are outside this group: e_q019_pocestny, e_q026_hcnpoisoning_npc and e_goblin_treasure.
- The boss combat events of Halda Molle, Darkmoon, the OCS boss and the dragons are real in the port.

## Summary

| id | name | giver | realm | shape/size | special reward yes/no + what | port status |
|---|---|---|---|---|---|---|
| q034/1 fenix | Soska fenixe (phoenix statuette) | c_q034_fenix "Zasmusily muz" / Reyton | Gondor | exploration (teleport only, fetch story) / small | no: no reward at all | items/NPCs converted only |
| q034/2 sekyra | Sekyra (stolen axe) | c_q034_sekyra "Drevorubec" / Nuurx | Mordor | exploration (teleport only, fetch story) / small | no: no reward at all | items/NPCs converted only |
| q034/3 krysyg | Otcova posledni vule (father's last will), Gondor brother | c_q034_krysyg "Norbert" | Gondor | exploration (teleport only, fetch story, once only) / small | no: no reward at all | items/NPCs converted only |
| q034/4 krysym | Otcova posledni vule, Mordor brother | c_q034_krysym "Robert" | Mordor | exploration (teleport only, fetch story, once only) / small | no: no reward at all | items/NPCs converted only |
| q034/5 TT1 | Kristalova koule (archmage's crystal ball) | c_q034_TT1 "Arcimag" | both (tutorial) | exploration (teleport only, fetch story) / small | no: no reward at all | items/NPCs converted only |
| q034/6 TT_shaman | Hlava pro voodoo (head for a voodoo spell) | c_q034_TT_shaman "Hlavni Saman" / Afi | both, shamans only | exploration (teleport only, fetch story) / small | no: the promised "I'll empower you" is not coded | items/NPCs converted only |
| q034/7 spiderG + spiderM | Pavouci sekta (spider cult trap) | c_q034_spiderG / c_q034_spiderM "Podivny muz" | Gondor / Mordor (one NPC each, shared questid 7) | boss/exploration (teleport into a cult lair) / medium | no: no quest reward; the 4 cult monsters drop normal loot | NPCs + 4 monsters converted only |
| q034/8 Ghost | Strasidelny dum (haunted house, magic book) | c_q034_Ghost "Placici Zena" | both | exploration (teleport only, clear + fetch story) / small-medium | no: no reward at all | items/NPCs converted only |
| q034/9 LizardG | Soska z kaple (chapel statuette, lizardmen) | c_q034_LizardG "Smutna divka" | Gondor | exploration (teleport only, group content) / small-medium | no: no reward at all | items/NPCs converted only |
| q034/10 LizardM | Poklad v chramu (swamp temple treasure) | c_q034_LizardM "Tajemny dobrodruh" | Mordor | exploration (teleport only, group content) / small-medium | no: no reward at all | items/NPCs converted only |
| (q034/0 quake) | dev template "MOJE MENO" | c_q034_quake "Quake" | n/a | template, not a quest | no | NPC converted only (should not be) |
| q035 Herman | Herman (beetle hunter's chain) | c_q035_herman "Herman" (GM-placed) | both; level 6 to start, rising to 30 | kill (9 head-collection tiers) + group lair fetch / medium | yes: 1x i_mi_bag at the end (random magic item); also 7650 exp + 92500 gp in total | items/NPCs converted only |
| q036 Jonas | Uhrovity Jonas (pimply Jonas, the family sword) | c_q036_jonas_uhrovity "Jonas" (+Evzen, Hubert, Fiflena of q024) | both; level 15 | talk/delivery + 4 titan-lord heads / small-medium | no: 3000 exp + 15000 gp only | items/NPCs converted only |
| q037 Zarik | Opily Zarik (drunk Zarik's lost night) | c_q037_zarik "Zarik" (+6 NPCs) | both; level 90; needs q036 done | mixed chain (talk, 57k gp sink, cave, travel, collect) / large | yes: i_mi_bag (chest) + i_mi_bag_high (end), 12000 exp, no gold | items/NPCs converted only |
| q038_ocs | Oblast cerne smrti (Area of Black Death): an instanced group dungeon run | c_q038_ocs_hospodsky "Hospodsky" (innkeeper) sells the entry bottle; c_q038_ocs_zalarnik "Zalarnik" (Etkin, the jailer) gives the real quest | both (region tag.realm=3, no realm check) | mixed (dungeon run: talk/fetch/use-objects/kill/boss) + branching, large | no scripted reward. Only a starter kit (robe, night sight potion, 30 fish, a copper weapon kit for your profession, a temporary spellbook) and kill exp. The "treasure in the camp bank" reward is GM-placed and not in the archive | items/NPCs converted only (defs exist, no quest logic). Boss abilities ported (#1022) |
| q038 stages 0-2 | Entry + prison escape | Hospodsky -> Edgar (cell password) -> Zalarnik | both | puzzle/talk, small | no (starter kit only) | not ported |
| q038 stages 3-16 | Salmat branch (help Melichar) | Melichar -> Arnost | both | mixed (fetch, use-objects, kill mini-boss Houbovec, kill captain Hungar), medium | no (GM-placed "bank" treasure, contents unknown) | not ported |
| q038 stages 17-32 | Sermat branch (help Baltazar) | Baltazar -> Adrian | both | mixed (fetch, poison barrels, kill Houbovec, delivery for Hungar), medium | no (GM-placed treasure "in the dwelling next door", contents unknown) | not ported |
| q038 stages 33-38 | Plague city + the King (shared ending) | Sfenuk -> Umogon -> Kubrogin -> escort Umogon -> King "Rascal" (c_ocs_boss) | both | talk/escort/boss, medium | no (boss exp 850-950 + generic loot) | not ported (only the boss's combat abilities are ported). On Erebor this part was also broken and could not be finished |
| q039 Shadow | Stinove zbrane (shadow weapons) | c_q039_kovar "Kovar" (smith) | both; level > 40 | mixed: boss skulls + timed traitor kills + rare materials + 1 h craft / large | YES: unique soul-bound "Stinova zbran" per profession: obsidian weapon, +20 class skill / +10 Tactics (casters +10 Magery/EvalInt), +10% vs 5 monster families, x2 to x2.25 damage vs monsters, 15 lives, repairable; plus 9000 exp | items/NPCs converted only; e_shadow_weapon_brother stub |
| q040_dragons | Spolecnost drakobijcu (Dragonslayers' Society) | c_q040_doorman "Drakobijec" (Ikreon), then c_q040_vartian, c_q040_predstaveny (portals) | no realm check (areas tagged realm=3) | mixed (puzzle + kill + boss chain), large | yes: access to the dragonslayer fortress, lair portals, the dragon-armour service; 49,000 exp total; no items | items/NPCs converted only (defs exist, no quest logic); dragons + boss abilities ported (#1022) |
| q040_dragons_alchymista | Drakobijec Alchymista (Alchemist) | c_q040_alchymista "Alchymista" (hobbit) | no realm check | fetch (120 elixirs), small | yes: unlocks a shop of 4 dragon potions (incl. +10% dragon resist for 300 s); no exp/items | not ported (potions themselves ported in Basic/Potions) |
| q040_dragons_rakchin | Drakobijec Rakchin | c_q040_Rakchin "Rakchin" | no realm check | kill/fetch (gold dragon scale), small | yes: 10,000 exp + i_mi_bag_superHigh (top-tier magic item bag, 2 properties) - UNREACHABLE (no gold dragon exists) | not ported (bag def exists) |
| q040_dragons_blazen | Blaznivy Drakobijec (Mad Dragonslayer) | c_q040_blazen "Blazen" ("Drakobijec" in the dialog) | no realm check | boss/fetch (Marguth's blood), small | 2,000 exp + i_q039_shadow_weapon_gem "Slza hor" (q039 crafting ingredient) - UNREACHABLE/broken | not ported (gem def converted) |
| q040_dragons_kovar | Drakobijci - brneni (Dragonslayers - armour) | c_q040_kovar "Kovar" or c_q040_dratenik "Dratenik" | no realm check | kill/fetch (boss skull) + paid crafting service, medium | yes: unlocks dragon armour (up to 56% dragon-breath resist) and a "Zbran drakobijce" weapon (+20 skill, +30% vs dragons) | not ported (scale itemdefs converted only) |
| q041 Paserak (prestupak*) | Paserak (the smuggler: switching realms) | c_q041_prestupak_paserak "Paserak" | Gondor or Mordor, level 15, not outcasts | mixed payment + realm-specific kill/delivery / medium | yes (status): realm change; costs 10k + level x 10k + 1M gp + 1-10 magic bags; repeatable | items/NPCs converted only |
| q042 Hatlend | Halda Molle Hatlend (150 rulik berries) | c_hm_hatlend "Hatlend" (in Halda Molle) | both; level 25 | fetch (monster drops) / small | yes: 1x i_mi_bag, 2000 exp | items/NPCs converted only; e_hatlend stub; HM combat ported |
| HM-dungeon | Halda Molle rescue + cooking (no journal entry) | c_hm_malica, c_hm_kaloc, c_hm_pruvodce (guide) | anyone | exploration/boss dungeon + cooking puzzle / large | yes: Kaloc gives i_mi_bag every 12 h after the Nameless dies; Malica opens a gate to a GM-built bonus area | combat ported (HaldaMolle.cs); NPC logic not; 3 stubs |
| q043_tut | Murdocovi ucni - tutorial (Argol) | c_q043_tut_argol "Argol" (tutorial area) | any (tutorial, before realm matters) | fetch/tutorial, small (3 pickups) | no (600 exp only) | not ported (NPC + 3 item defs converted only; port has no tutorial quests at all) |
| mq043 | Murdocovi ucni (Mordor chain) | c_mq043_dran "Dran" (Barad-Dur, by the bank) | Mordor (realm 2); crafters refused | mixed fetch/delivery/talk chain, large (5 towns, ~30 stages, 16 statics) | yes: i_mi_bag (random magic item, 1-2 props), i_pet_llama (llama figurine); plus 15000 gold, 3400 exp | items/NPCs converted only (defs exist, no quest logic) |
| gq043 | Murdocovi ucni (Gondor chain) | c_gq043_Anton "Anton" (Hobitin) | Gondor (realm 1); crafters refused | same as mq043 (mirror) | yes: same as mq043 | items/NPCs converted only |
| mq043/gq043 250-261 | Torwin bonus (Kronikar Torwin) - tail of the same tags | c_q043_Kronikar "Kronikar Torwin" (Umbar for Mordor, Dol Amroth for Gondor) | both, per region | exploration/fetch, medium (5 caves in 5 towns) | no (1675 exp only) | items/NPC converted only |
| q044 Murdoc | Zachrante Murdoca (save Murdoc) | c_q044_ember "Ember" | both, crafters refused | exploration: 5 invisible chests in 5 dungeons / medium | yes: 6 magic bags (3 pouches, 2 bags, high pouch, superhigh pouch) + 24100 exp + 150000 gp | items/NPCs converted only |
| q075 Valecnik | Valecnik Arkan (a traitor mage's guts) | "Valecnik Arkan": NO chardef exists | both | kill / small | no: 15000 exp + 15000 gp | not ported (unstartable on Erebor too) |
| q099 Vyvrhel | Vyvrhel (outcast redemption) | c_q099_vyvrhel "Podivin" | outcasts only, with tag.vykoupeni | kill-collect 4 bags + 250k cheque + Mining 40 / medium | yes (status): outcast status lifted, realm restored; costs 15 levels | items/NPCs converted only |
| q100 Hobitin | Hobitin (gardener Ambra's manure) | c_q100_hobitin_ambra "Ambra" | anyone | fetch / small (newbie) | no: 30 exp + 1 onion | items/NPCs converted only |
| Darkmoon | Darkmoon beetle event (no journal entry) | c_darkmoon_doktor "Lennor", messengers Kester/Gilthol | anyone | GM boss event; quest part unfinished / medium | no (boss loot: gold + reagents) | boss fight ported (Darkmoon.cs); NPCs converted only |


---

## q034 "port questy": NPCs that teleport you to a quest spot

Files (in the archive's `scripts/questy/`):
`q034_port_questy.scp` (dialogs, `QP:`), `q034_port_questy_chars.scp` (NPCs + 4 cult monsters, `QC:`), `q034_port_items.scp` (`QI:`). `Moria_port_gump.scp` is **not a quest**: it is the stock Sphere GM travel menu and command reference (see the last section).

**What q034 really is.** All 10 "quests" (plus one dev template) use the same pattern. A story NPC tells you an item was stolen or a monster is a problem. You click "Ano" (yes), the NPC checks level, realm and cooldown tags, a 5-second countdown starts, and you are **teleported** to a dungeon spot. **That is all.** None of the 10 has a hand-in: no "Nesu ti" (I bring you) button, no check for an item, no exp, gold or item reward. The stolen items named in the stories (phoenix statuette, axe, father's will, crystal ball, voodoo head, magic book, chapel statuette, temple treasure) exist nowhere in the archive. The only "reward" is whatever the destination's monsters drop. q034 has no entry in the quest journal: `questmenu.scp` has no q034 or port line (checked in both copies), and q034 calls `qtag` without the third argument that reports progress to the journal.


---

## Shared mechanics (applies to every quest below)

All 10 dialogs were copied from the template, so I describe the template logic once and give only each quest's differences in its own section.

- **Start.** Every giver has a TSPEECH that reacts to "ahoj/zdravi/dobry/ukol" (hello/greetings/good day/task) within 3 tiles, plus `@UserDClick` (double-click), and both call `f_q034_<x>`. That function refuses enemies when `tag.realm` is set ("S nepritelem se nebudu bavit", "I won't talk to an enemy"; GMs bypass this, except in TT_shaman). It then opens a `questDialog`-system dialog with pages Pozdrav (greeting), Mohu pomoci? (can I help? = the quest offer, with Ano/Ne), Drby... (gossip, hints about other content) and Prohlednout (look at the NPC).
- **Config is NPC tags set in the chardef's @Create:** `questid`, `realm` (1=G, 2=M), `portto` (x,y,z,mapplane), `minlevel`/`maxlevel` (vs `src.tag.level`), `minskillrank`/`maxskillrank` (1 expert, 2 master, 3 grandmaster; unused by every real quest), `canrepeat` + `timeragain` (per-player cooldown in seconds), `timeragainall` (global lock: nobody else can be sent until it expires), `canrepeatafterdeny` + `timeragainafterdeny` (cooldown after refusing), `porttimer` (countdown seconds, always 5). The template documents them at QC:24-42.
- **Per-player state** goes in qtags on the player's `i_quest_rune` (`qtag` function, qTag.scp:1-26): `q034_<questid>_canrepeat`, `_again` (expiry time), `_canrepeatafterdeny`, `_againafterdeny`. Times are `serv.time` + seconds*10 (tenths of a second).
- **Accepting ("Ano")**, template QP:67-160: GM check, then level and skill-rank checks, then deny checks, then repeat and per-player cooldown checks (**the per-player cooldown is written here**), then `canrepeat` is written, then the global `timeragainall` check and write. After that, if `porttimer` is set, the player gets `i_q034_port_timer`, otherwise is sent straight away with `src.go(portto)`.
- **`i_q034_port_timer`** (QI:2-19): an invisible `i_memory` `t_eq_script` item. Every second it prints "N sekund do presunu" (N seconds until transfer) and at 0 runs `cont.go(<tag.portto>)` and removes itself. Moving, fighting or dying does not cancel it.
- **Refusing ("Ne")** writes the deny qtags.
- Every dialog also calls `f_q000_shouldBePromoted_Request/Answer` (velmistr.scp:955-1005), the shared hook that sends players who qualify on to the Grandmaster rank quest. That quest is **already ported** (rank quests), so this part is done. Only fenix calls `f_q000_shouldBePromoted_Button`; the other 9 dialogs drop that call, so the "go to the Zmocnenec" (realm envoy) reply in their dialogs does nothing.
- **Shared bugs:**
  1. The per-player `_again` cooldown and the `_canrepeat` flag are written **before** the global `timeragainall` check (e.g. QP:1558/1561 before 1566-1569 for Ghost). A player turned away by "come back later" has still used up their 1-day cooldown and, for once-only quests, their single try.
  2. The deny cooldowns are only read when `canrepeatafterdeny==1`, so with `canrepeatafterdeny=0` a single "Ne" locks the quest forever. No real quest uses 0.
  3. Nothing checks that you return or succeed. "Once only" means you can be teleported once.
- **e_* events on the givers:** `e_character` and `e_monster`. Both are still zero-effect `PassiveAbilityDef` stubs in our `scripts/Basic/AbilitiesAndTalents/teventStubs.scp` (`a_tevent_e_character` line 70, `a_tevent_e_monster` line 288). They are generic NPC events, not quest logic.
- **Placement:** none of the NPC defnames appears anywhere else in the archive, neither in the saves (the archive's `save/` is a stripped dev world with no chars) nor in spawn or GM files. **All givers were GM-placed. The archive has no spot for any of them.**

---

## q034/1 fenix: "Soska fenixe"

1. **Id/name:** questid 1, *Soska fenixe* (the phoenix statuette). Files QP:170-333, QC:67-140.
2. **Giver:** `c_q034_fenix`, chardef NAME "Zasmusily muz" (gloomy man), introduces himself in the dialog as Reyton, a scholar (QP:171-204). GM-placed, no spot in the archive.
3. **Who:** Gondor only (`tag(realm,1)`, QC:91). `maxlevel 25` (QC:105), no minimum (the `//tag(minleve,l0)` line is commented out and misspelt). Repeatable at once: `canrepeat 1`, no `timeragain`. Can retry after refusing, no cooldown.
4. **Story:** "An Ettin lord stole my phoenix statuette, an old family heirloom. Bring it back and I'll transport you near his lair." Accept, wait 5 seconds, and you are teleported to **4881,1528 (plane 0)**, inside area "Stezky mrtvych" (Paths of the Dead). Gossip: rat-men are spreading into Edoras and were seen under the ancient cemetery.
5. **Rewards:** none coded. No exp, gold or items. The statuette has no itemdef anywhere in the archive.
6. **Quest items/monsters:** none. The Ettin lord is the stock `c_ettin_lord` (ogres.scp:544), not a quest monster, and nothing makes it drop a statuette.
7. **Engine needs:** questDialog gump, NPC tags, qtag storage, the countdown memory item, teleport.
8. **Shape:** exploration (teleport only), small.
9. **Broken:** only this dialog lacks `return 1` after the GM's instant port (QP:209-212). A GM is teleported, then falls through all the checks and is teleported a second time by the timer. There is no completion.
10. **Port status:** items/NPCs converted only. `converted/questy/q034_port_questy_chars.def:32` keeps `Realm = Gondor` but **drops questid, portto, maxlevel, canrepeat and porttimer**. The dialogs are commented out in `converted/questy/q034_port_questy.def` and only a `[SpeechStub spk_q034_fenix]` is left. `i_q034_port_timer` is an `EquippableDef` with no trigger logic (`converted/questy/q034_port_items.def`). The region `Stezky mrtvych` is converted (`converted/map.def:5151`).

## q034/2 sekyra: "Sekyra"

1. **Id/name:** questid 2, *Sekyra* (the stolen axe). QP:334-497, QC:142-215.
2. **Giver:** `c_q034_sekyra` "Drevorubec" (woodcutter), named Nuurx in the dialog. GM-placed, no spot.
3. **Who:** Mordor only (QC:166), `maxlevel 25` (QC:181; refusal line: "...kill a few Gondorians instead"). Repeatable at once, can retry after refusing.
4. **Story:** "Rat-men stole my axe. Without it I have no work and my master will punish me." You are teleported to **3661,2555 (plane 0)**, inside the area "Mordor". Gossip: rats have also occupied a gorge in Rhun.
5. **Rewards:** none coded.
6. **Items/monsters:** none. There is no axe quest item.
7. **Engine needs:** same as fenix.
8. **Shape:** exploration (teleport only), small.
9. **Broken:** no completion. The Mordor mirror of fenix.
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:69`, the same tags are dropped). SpeechStub only.

## q034/3 krysyg and q034/4 krysym: "Otcova posledni vule" (the two brothers)

1. **Id/name:** questid 3 (Gondor) and 4 (Mordor), *Otcova posledni vule* (father's last will). QP:498-828, QC:217-366.
2. **Givers:** `c_q034_krysyg` "Norbert", a richly dressed merchant, and `c_q034_krysym` "Robert", a shabby adventurer. Brothers. GM-placed, no spot.
3. **Who:** Norbert is Gondor only, Robert Mordor only. Both `maxlevel 15`. **Once only:** `canrepeat 0`, and afterwards "Dekuji ti za tvoji minulou pomoc" (thanks for your earlier help). A refused offer can be taken later.
4. **Story:** each brother says "I came home and found my lonely house overrun by rat vermin. I give you **only one chance**. As proof, bring me father's last will." Gossip: "My brother may have given this task to someone else, maybe even to an enemy!" It is designed as a cross-realm race for the same document. Norbert sends you to **3502,3312,45 plane 5**, Robert to **3499,3292,25 plane 5**, about 20 tiles apart. Those x,y lie inside "dum 284" (a rentable fortified hamlet in Nurn), but that area is on plane 0. **No area in the archive's spheremap.scp covers these spots on plane 5.**
5. **Rewards:** none coded. The will has no itemdef.
6. **Items/monsters:** none quest-specific.
7. **Engine needs:** same as fenix, plus the idea of a cross-realm competition, which was never implemented.
8. **Shape:** exploration (teleport only, once only), small.
9. **Broken:** the "once" is consumed by the teleport itself. If you die, you can never try again. The race and proof parts were never written. Robert's STR=1000 is probably a typo (Norbert has 100).
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:100`, `:139`).

## q034/5 TT1: "Kristalova koule" (tutorial archmage)

1. **Id/name:** questid 5, *Kristalova koule* (the crystal ball). QP:829-987, QC:368-442. Chardef DESCRIPTION is "Tutorial high 1".
2. **Giver:** `c_q034_TT1` "Arcimag", who won't give his name. GM-placed, no spot. He was presumably in the tutorial or starting dungeon, since he teleports within plane 35.
3. **Who:** no realm (commented out). `tag(minlevel,l5)` (QC:407) is a typo with the letter "l" instead of a digit. Its refusal line is "you wouldn't survive there". Sphere would compare level against the non-number "l5"; I can't determine what that evaluates to, probably 0, so effectively no gate. Repeatable every **25 hours** (`timeragain 3600*25`, QC:413). Can retry after refusing.
4. **Story:** "Treacherous monsters took my magic crystal ball again; without it I feel helpless." You are teleported to **5333,2444,-17 plane 35**, the entry point of the area "tutorial high old" (group "startovni dungeon", area tag.realm=4, tag.kick=1). Gossip: dark magic is seeping in, and "the Lich Lord here is only a fraction of the true evil".
5. **Rewards:** none coded.
6. **Items/monsters:** none quest-specific.
7. **Engine needs:** same, plus the tutorial plane 35.
8. **Shape:** exploration (teleport only), small.
9. **Broken:** the minlevel typo. The area is named "...old", so this was probably obsolete even on Erebor.
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:178`). The area "tutorial high old" is converted (`converted/map.def:4397`).

## q034/6 TT_shaman: "Hlava pro voodoo" (tutorial shaman)

1. **Id/name:** questid 6, the voodoo head. QP:988-1146, QC:444-520.
2. **Giver:** `c_q034_TT_shaman` "Hlavni Saman" (chief shaman), named Afi in the dialog. Carries `i_staff_gnarled_iron` as decoration. GM-placed, no spot.
3. **Who:** **shamans only** (`src.profession!=class_shaman` is refused unless GM, QP:1140). No realm. `tag(minlevel,l)` is a typo (QC:483); the refusal line suggests the intent was level 1. Per-player cooldown **1 hour** (QC:489). The refusal tags are commented out, so refusing has no penalty, and the "try again in an hour" line can never be shown.
4. **Story:** "A shaman's lot in the Tutorial is hard; let me give you work. I need a head for a powerful voodoo spell; you'll learn this ability yourself in time. **I'll strengthen you** so you can kill strong monsters." You are teleported to **5255,2377,-17 plane 35**, also inside "tutorial high old".
5. **Rewards:** none. **The promised strengthening is not coded**: no buff, spell or item.
6. **Items/monsters:** no head item.
7. **Engine needs:** profession check, plus the shared ones.
8. **Shape:** exploration (teleport only), small.
9. **Broken:** the minlevel typo, the missing buff, and the GM realm bypass is missing (irrelevant because realm is unset).
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:216`). `class_shaman` exists in our engine (`scripts/Basic/Professions/Definitions/shaman.def`).

## q034/7 spiderG and spiderM: "Pavouci sekta" (spider cult trap)

1. **Id/name:** questid 7, used by **both** NPCs, so they share one set of qtags. QP:1147-1460, QC:522-1004 (including the 4 monsters).
2. **Givers:** `c_q034_spiderG` (Gondor) and `c_q034_spiderM` (Mordor), both "Podivny muz" (strange man): an odd-looking man with pointed ears. GM-placed, no spot.
3. **Who:** realm-locked, one NPC per realm. Levels **10-20** (QC:561-562, 639-640). Repeatable **once a day** per player (`timeragain 86400`). Refusing has no penalty (the tags are commented out).
4. **Story:**
   - He offers "a trifle: a vicious wolfhound attacks the cows on our farm, kill it?"
   - It is a **trap**. On accepting he says "Muhaha! Nakrm pavouky!" (Muhaha! Feed the spiders!) and you are teleported to **4822,2003,-40 plane 5**, the spawn point of area **"Doupe sekty"** (the cult's lair, group questy).
   - His gossip line about orcs feeding their pet spiders on human flesh is the hint.
   - The lair is meant to hold orc cultists and their giant spider "Drapek".
   - If you come back within the day he says "What?! You survived? Don't show yourself today!"
5. **Rewards:** none coded. The monsters' loot (QC:746-748, 828-830, 907-908): leader and lord drop `i_gem_diamond` x0-1 + `i_gold`, the orc drops `i_gold`, plus normal `i_monster_create` loot and exp tags. All plain stock items.
6. **Quest monsters** (defined only here, spawned nowhere in the archive):
   - `c_orc_boss_spider` "Vudce pavouci sekty" (cult leader, QC:679-758): an orc lord with 300 STR, 900-950 HP and a two-handed axe; Swordsmanship 60-85; exp 60-65; `e_npc_leaper`; resists poison 800, pierce +150, blunt -350.
   - `c_orc_Lord_spider` "Clen pavouci sekty" (cult member, QC:761-840): 500-550 HP, exp 35-40, leaper.
   - `c_orc_spider` (cult member, QC:843-918): a club orc with 500-550 HP; exp line is malformed `<eval 27.0 30.0}>`.
   - **`c_spider_sekta` "Drapek"** (Clawy, QC:921-1004): a giant spider, "Spider Fake" in the GM menu. It has **11,000-15,000 HP** and attack 100. Resists are 950-1000 in everything (poison, paralysis, summons and bleed at 1000; physical 950-980; elements 950-980), so it is practically unkillable, and `INVUL=1` is commented out. Exp 500, `e_npc_detect` (reveals hidden players), and it emotes "is terrifying you".
   - The orcs taunt you while they fight: "I'll feed you to the spiders", "Drapek is looking forward to you".
7. **Engine needs:** a quest-flagged region, the special monsters, the leaper and detect AI, monster speech on hit. The archive spawns none of them; it would need GM-placed spawns.
8. **Shape:** boss/exploration (trap dungeon), medium.
9. **Broken:** the shared questid means the two realms share one cooldown, which is harmless because each NPC is realm-locked. The `c_orc_spider` exp is malformed. Drapek reads as a "fake" set-piece monster rather than a killable boss. There is no wolfhound and no completion.
10. **Port status:** items/NPCs converted only. The 4 monsters are converted as NPCDefs (`q034_port_questy_chars.def:338`, `:394`, `:450`, `:503`, with resists, loot and exp); the @hit taunts are not converted. `tag.npc_port_timer` is dropped; `NpcPortDistance` is kept, but no runtime C# reads it (only `SphereShardConverter/ConvertedCharDef.cs` mentions it). `e_npc_leaper` is a real `LeaperAbilityDef` (teventStubs.scp:372, simplified to a pursuit speed boost) and `e_npc_detect` is a real `DetectAbilityDef` (teventStubs.scp:336). The region "Doupe sekty" is converted (`converted/map.def:5320`, plane 5).

## q034/8 Ghost: "Strasidelny dum"

1. **Id/name:** questid 8, *Strasidelny dum* (the haunted house; chardef DESCRIPTION). QP:1461-1616, QC:1006-1084.
2. **Giver:** `c_q034_Ghost` "Placici Zena" (weeping woman), a beautiful, noble-looking woman holding `i_staff_gnarled_valorite`. GM-placed, no spot.
3. **Who:** any realm, **minlevel 20**. **Global lock of 1 hour** (`timeragainall 3600`): after anyone is sent, nobody else can be for an hour, which makes it an instance-like slot. Per-player **1 day**, and 1 hour after refusing.
4. **Story:** "Intruders took my house, including my magic book. Will you cleanse it of those creatures and return the book? Be careful!" You are teleported to **4927,1386,20 plane 5**. Those x,y are inside area "Strasidelny dum" (RECT 4921,1340-4961,1446, group Dungeony), but **that area is on mapplane 10 in the archive**, so the port's plane 5 does not match it. I can't determine which is correct. Gossip: "magic is a mighty weapon, but I'm only learning it."
5. **Rewards:** none coded. No book item.
6. **Items/monsters:** none quest-specific. Whatever the area holds is not in q034.
7. **Engine needs:** a global lock timer stored on the NPC, plus the shared ones.
8. **Shape:** exploration (teleport only, clear story), small-medium.
9. **Broken:** the plane mismatch. Shared bug 1 matters most here: a player refused by the global lock has still lost their 1-day cooldown.
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:560`). The area is converted (`converted/map.def:3459`).

## q034/9 LizardG: "Soska z kaple"

1. **Id/name:** questid 9, the stolen chapel statuette. QP:1617-1772, QC:1086-1164.
2. **Giver:** `c_q034_LizardG` "Smutna divka" (sad girl). GM-placed, no spot.
3. **Who:** Gondor, levels **15-35**, per-player **1 day**, 1 hour after refusing.
4. **Story:** "Strange lizard-like people burst in, killed all the guards and stole a rare statuette from the chapel. Help me get it back; **maybe you shouldn't go alone**." You are teleported to **1818,2479,-5 plane 0**, inside "Gondorske uzemi" (Gondor territory); no smaller area covers it. Gossip: about animal taming.
5. **Rewards:** none coded.
6. **Items/monsters:** none quest-specific.
7. **Engine needs:** shared ones only.
8. **Shape:** exploration (teleport only, group-suggested), small-medium.
9. **Broken:** no completion. "Cestu jiz znas" (you already know the way) is the line for a player who did it before, but with `canrepeat 1` it never shows.
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:594`).

## q034/10 LizardM: "Poklad v chramu"

1. **Id/name:** questid 10, the treasure in an ancient swamp temple. QP:1773-1928, QC:1166-1245.
2. **Giver:** `c_q034_LizardM` "Tajemny dobrodruh" (mysterious adventurer in a dark hood). GM-placed, no spot.
3. **Who:** Mordor, levels **15-35**, per-player **1 day**, 1 hour after refusing.
4. **Story:** "My parchment says there's treasure in an ancient temple in the swamp. Fetch it for me; maybe don't go alone." You are teleported to **3972,1373 plane 0**; no named area covers it beyond "Stredozeme" (Middle-earth). Gossip: about the arms trade in wartime. It is the Mordor counterpart of LizardG, with a different destination.
5. **Rewards:** none coded.
6. **Items/monsters:** none.
7. **Engine needs:** shared ones only.
8. **Shape:** exploration (teleport only), small-medium.
9. **Broken:** no completion.
10. **Port status:** items/NPCs converted only (`q034_port_questy_chars.def:629`).

## q034/0 quake: dev template (not a quest)

`c_q034_quake` "Quake" with dialog name "MOJE MENO" (my name) and placeholder texts "TEXT 0..4" / "NECO1..3". It ports to 4773,1349,10 and documents the tags. QP:1-169, QC:1-65; its SUBSECTION is "q024" by copy-paste. It is converted as an NPCDef (`q034_port_questy_chars.def:3`) and a SpeechStub, but it is not content and should not be ported.

## Moria_port_gump.scp: GM travel menu (not a quest)

This is the stock Sphere 0.99u "Travel Dialog" (donated by Catalan_Mistral). `gomap` opens `d_playertravel`, a map gump with buttons that `SRC.GO` to Middle-earth towns (Tharbad, Lorien, Roklinka, Harad, Rhun, Dol Amroth, Nurn, Minas Morgul, Barad-Dur, Umbar, Edoras, Minas Tirith, Erebor, Fornost, Sede Pristavy), dungeons and leftover OSI/Ilshenar locations. It also contains a GM command reference (`d_commandref`, `d_commandref2`, `commandhelp`). No quest logic. Converted fully commented out (`converted/questy/Moria_port_gump.def`).

Side note found while checking: `q040_dragons.scp:505` offers a reply labelled `"q034_goodbye"` that no button handler covers. It is a dead copy-paste button in q040, unrelated to q034.

## Port summary / designer notes

- In our engine, **nothing of q034's logic is ported**. NPCDefs (realm only; questid, portto, levels and cooldowns are dropped by the converter), the 4 spider-cult monsters, the timer item def and the regions exist. The dialogs are commented out and replaced by SpeechStubs.
- The reusable idea is a generic **"teleporting quest-giver" component**: NPC config of destination, level band, realm, per-player, global and refusal cooldowns, and a countdown. It is cheap to build from our existing dialog, tag and teleport pieces.
- None of the 10 stories has a payoff. Turning them into real quests would need new items, hand-in dialogs and rewards. That would be **new design, not a mirror of Erebor**.
- The most distinctive content is the **spider cult trap** (questid 7), with 4 bespoke monsters and the "fake" boss Drapek, and the **two brothers' cross-realm race** (questid 3/4), which was never finished.

---

## q035 Herman - "Herman" (beetle-hunter's chain)

1. **Id / files.** Journal questMenu[41] "Herman" (qtag `q035_herman`, quest no. 41). Files: scripts/questy/q035_herman.scp (498 lines), q035_herman_chars.scp, q035_herman_items.scp. Chardef SUBSECTION says "q005 - Brouci" (beetles).
2. **Giver.** `c_q035_herman` "Herman", title "bojovnik proti brouku" (beetle fighter), grumpy limping man. No location in the script; not in the archive save (save/sphereworld.scp) -> GM-placed, spot unknown. Talk by double-click or the words ukol/prace/zdravim/ahoj.
3. **Who.** Both realms (no realm check on the NPC). Player level gates: start at `tag.level >= 6`; each next stage i (2,4,...,16) needs level >= i+6 (so 8, 10, ... 22); the final egg task needs level 30 (q035_herman.scp:212-249). Once only. Refusing a task gives a 24 h "go away" memory (`i_q035_herman_false_delay`, TIMER=86400). Saying "I can't do it, I quit" at the egg stage ends the quest for good (qtag 23).
4. **Steps.** Herman hands out a crate (`t_collector`, target a corpse to cut its head) for each beetle tier, then a final lair run:
   - 20 heads of Brouk (c_bug) -> 20 Brouk delnik (worker) -> 15 Brouk Trubec (drone) -> 12 Brouk Strazce (guard) -> 10 Brouk mag -> 8 Brouk bojovnik (warrior) -> 5 invisible beetles (the colour-07FFF "Brouk Strazce" variant) -> 3 Brouk Zabijak (killer) -> 1 Brouk matka (mother). Beetle chardefs are in chars/INSECTS.SCP.
   - Final: fetch a beetle egg from "Brouci doupe" (beetle lair), reached through an entrance cave near "zvladla vresoviste". The lair gate `i_q035_herman_gate` (blue moongate, t_telepad2) opens only for a player on stage 19; it then lets in that player plus 2 more of the same realm for 20 s, then locks for 3 hours. Double-click the egg static (`i_q035_herman_egg_dclick`) to get the carried egg, return it.
   - If you entered but failed (stage 20) Herman will not let you in again; you can only go in as another questor's helper.
5. **Rewards** (q035_herman.scp):
   - Per head crate: 200 exp + 2000 gp (:433-434), 350 + 2500 (:439-440), 400 + 3000 (:445-446), 450 + 3500 (:451-452), 500 + 4000 (:457-458), 550 + 4500 (:463-464), 600 + 5000 (:469-470), 800 + 8000 (:475-476), 800 + 10000 (:481-482).
   - Egg: 3000 exp + 50000 gp + 1x `i_mi_bag` (:488-490).
   - Total: 7650 exp, 92500 gp, one magic bag.
   - **`i_mi_bag` = SPECIAL (random magic item).** systems/magicITEMS.scp:882: "Pytlik s magickym predmetem"; double-click unpacks one random unidentified magic item: weapon/armour/other (jewellery, hat, cape, sash) at 2:2:1 odds, metal tier 0 (copper 500, iron 400, verite 200, valorite 100, obsidian 20, adamantium 10, mithril 1 in 1231; magic_defs.scp:385), 2 magic-property rolls (first sure, second 5 in 6; magicITEMS.scp:957-975). Not a fixed artifact. Already exists in our port: `Basic/MagicItems/MagicBags.def` (`[MagicBagDef i_mi_bag]` Tier 0, Rolls 2).
6. **Quest-only items.** 9 head crates `i_q035_herman_collector_01..09` (t_collector, target_id "Body of Brouk ..."), `i_q035_herman_false_delay` (24 h memory), `i_q035_herman_egg_dclick` (static) / `i_q035_herman_egg_bounce` (carried egg), `i_q035_herman_gate` (lair gate). No quest-made monsters; uses stock beetles (c_bug ... c_bug_matka).
7. **Systems.** Quest dialog framework, t_collector "cut a head off a corpse" crates, level gating, a realm-locked group gate with 20 s window + 3 h cooldown, GM-built lair area.
8. **Shape.** kill (head collection, 9 tiers) + small group dungeon fetch; medium.
9. **Problems.** If the egg is missing at hand-in the script jails the player ("Chyba v questu! Pis page." + `src.jail`, :494-495). `tag(pocet)` counter on the NPC is never read. The gate stores the realm of the first questor; `tag(pustit)` counts down per entry. Gate target (`morep`) and egg static were GM-placed: no spot in the archive.
10. **Port.** Items/NPCs converted only: `scripts/converted/questy/q035_herman_chars.def` (NPCDef, speech is a SpeechStub), `q035_herman_items.def` (crates/gate/egg as plain defs), `q035_herman.def` has the dialog commented out. Beetles converted (`converted/chars/insects.def`, c_bug ... c_bug_matka). No t_collector logic in the port (no C# collector). `i_mi_bag` reward already works (MagicBags.def). NPC events e_character/e_monster are zero-effect stubs.

---

## q036 Uhrovity Jonas - "Pimply Jonas" (the family sword)

1. **Id / files.** Journal questMenu[42] "Uhrovity Jonas" (qtag `q036_jonas`, no. 42). scripts/questy/q036_jonas.scp (492), q036_jonas_chars.scp, q036_jonas_items.scp. The 4th NPC, Fiflena, lives in q024_zasnubni_prsten(.scp/_chars.scp) (another group's quest): her q036 dialog lines are hooked into her q024 dialog (q024_zasnubni_prsten.scp:54,70,86,107).
2. **Givers.** `c_q036_jonas_uhrovity` "Jonas" (cocky youth; per q037 text he lives "by the rocks where the road under Lorien ends"); `c_q036_jonas_evzen` "Evzen" his father, "in the desert in front of the Lost City"; `c_q036_jonas_hubert` "Hubert", hiding old hunter (HIDING 100) in the worm cave above Oasis Hagir; `c_q024_Fiflena` on her island castle. None in the archive save -> GM-placed.
3. **Who.** Both realms. Level >= 15 to accept the titan-lord stage (q036_jonas.scp:177). Once only. No cooldown.
4. **Steps.**
   - Jonas wants his father's heirloom sword; talk to Evzen: "he hasn't earned it".
   - Jonas gives a chest: kill 4 Titan Lords, cut their heads (`i_q036_tl_collector`), give them to Evzen as "Jonas killed them bare-handed". Evzen: "any milk-sop can do that".
   - Find Hubert in the worm cave; his advice: monsters won't impress Evzen, seducing an unattainable woman will.
   - Go to Fiflena for her knickers as proof. Male character: pay her 30,000 gp. Female character: bring a women's valorite plate chest (`i_valorite_female_platemail`).
   - Give the knickers to Evzen, get the sword (only a message, no item), give it to Jonas.
5. **Rewards.** Evzen on titan heads: 750 exp (:303). Fiflena: 750 exp (:471 male / :482 female). Jonas at the end: 1500 exp + 15000 gp (:167-168). Total 3000 exp, 15000 gp (a male player is net -15000 gp after paying Fiflena 30000). **No item reward**; the sword and knickers are text only. Special: no.
6. **Quest-only items.** `i_q036_tl_collector` (head chest, 4 "Body of Titan Lord"). Consumes 30000 gp or 1 `i_valorite_female_platemail` (player-crafted stock armour).
7. **Systems.** Quest dialogs, t_collector, gender branch (by body c_man), hidden NPC, cross-quest dialog hook into q024's Fiflena.
8. **Shape.** talk/delivery with one kill-collect step; small-medium.
9. **Problems.** Header comment says 4 Scorpion Kings; code uses Titan Lords. "Reward not thought up yet" comment (:20) - the final reward is just exp+gold. Evzen's journal/tag 10 step has no item, so nothing stops a skip except the tag. Hubert has empty "Prohlednout"/trade pages. The design notes are Slovak/Czech mix.
10. **Port.** Items/NPCs converted only (`converted/questy/q036_jonas_chars.def`, `q036_jonas_items.def`, dialogs commented out in `q036_jonas.def`). Fiflena converted with q024. e_item_refuser is real in the port (ItemRefuserAbilityDef); e_character/e_monster stubs.

---

## q037 Opily Zarik - "Drunk Zarik" (the lost night)

1. **Id / files.** Journal questMenu[43] "Opily Zarik" (qtag `q037_zarik`, no. 43). scripts/questy/q037_zarik.scp (917), q037_zarik_chars.scp, q037_zarik_items.scp. Authors noted "Enzo + Skleena". George's dialog is in q037_zarik.scp but the NPC is `c_snow_poustevnik` "George" in chars/snowmonsters.scp (calls f_q037_zarik_george at :4124; speech hook in speech/verbatimspeech.scp:100).
2. **Givers.** `c_q037_zarik` "Zarik" (naked hung-over adventurer). Chain NPCs: `c_q037_zarik_hospodsky` "Hospodskeho Pomocnik" (innkeeper's helper, inn on the Ice Island), `c_q037_zarik_sabrina` "Sabrina", `c_q037_zarik_urmughorg` "Urmughorg" (orc, by a half-collapsed cave entrance), `c_snow_poustevnik` "George" (hermit in the Ice Cave), `c_q037_zarik_wohy` "Wohy" (woodcutter in a magic forest), `c_q037_zarik_varjusa` "Varjusa" (thief). None in the archive save -> GM-placed.
3. **Who.** Both realms. **Level >= 90** to even open Zarik's dialog (q037_zarik.scp:38). **Prerequisite: q036 Jonas completed** - Sabrina gives Zarik's shirt only if `q036_jonas == 11`, otherwise she sends you to Jonas (:395-413). Once only.
4. **Steps** (each friend took some of Zarik's stuff):
   - Ask the innkeeper's helper what happened; he tells of Bert -> find Bert's corpse (`i_q037_zarik_bert` static), take Zarik's trousers.
   - Helper wants **57,000 gp** for the next story (:318-325) -> Sabrina; she gives the shirt and rum (needs q036 done) -> rum to the helper.
   - Orc Urmughorg: Zarik's scarf is in a monster cave behind him; clear it and open the bloody chest (`i_q037_zarik_satek`) -> scarf + a magic bag.
   - Urmughorg owes Zarik "wood"; ask hermit George in the Ice Cave to teleport you (to 5393,2471,32 map 11, :676) to woodcutter Wohy's magic forest; Wohy gives rare wood -> to the helper.
   - Helper names Varjusa, who stole Zarik's purse: bring him 4 armpit hairs of "Strazce" (guardian) monsters (`i_q037_zarik_collector_strazce`) for the purse.
   - Return everything to Zarik.
5. **Rewards.**
   - Bloody chest: 1x `i_mi_bag` (q037_zarik_items.scp:40) - **SPECIAL**, random magic item tier 0, 2 property rolls (see q035).
   - Zarik: 12000 exp + 1x `i_mi_bag_high` (q037_zarik.scp:120-121) - **SPECIAL**: same bag, tier 1 metals (verite 300, valorite 200, obsidian 80, adamantium 30, mithril 1; magic_defs.scp:388), 2 property rolls. In port: `MagicBags.def` `i_mi_bag_high` Tier 1 Rolls 2.
   - No gold, although the text says you hand over "the purse with money"; the player pays 57000 gp along the way.
6. **Quest-only items.** `i_q037_zarik_bert` (corpse static), `i_q037_zarik_satek` (chest static), `i_q037_zarik_collector_strazce` (bag, 4x "Body of Strazce"). Trousers, shirt, rum, scarf, wood, purse are messages only.
7. **Systems.** Quest dialogs, statics with tag checks, t_collector, NPC teleport to another map (map 11), a GM-built monster cave.
8. **Shape.** mixed (talk chain, gold sink, dungeon clear, travel, kill-collect); large (14 stages, 6+1 NPCs).
9. **Problems.** Header says 67k, code 57000. The chest only checks the tag (stage 7), not that the cave was cleared; the "beams hold only 1-2 people" collapse is flavour, not coded. The chest and Bert statics give per-player results via tags, fine. Comment in the finish line says "splnenie questu Uhrovity Jonas" (copy-paste). Statics, cave and forest are GM-placed with no spots except George's teleport target.
10. **Port.** Items/NPCs converted only (`converted/questy/q037_zarik_chars.def`, `q037_zarik_items.def`, dialogs commented out in `q037_zarik.def`; George via converted/chars/snowmonsters + converted/speech/verbatimspeech.def). Magic bags exist in the port.

---

## q038_ocs - Oblast cerne smrti ("Area of Black Death")

### 1. Id, name, files
- Quest tag `q038_ocs`. Journal row: `questMenu[44] Oblast cerne smrti, d_ocs, 1, q038_ocs` (gc/questmenu.scp:74). The progress reports use journal index 44 (`src.qtag(q038_ocs,N,44)`).
- Czech name "Oblast cerne smrti" = "Area / Region of Black Death" (OCS). The story is a plague-stricken desert land with two warring clans and a sealed royal city. I found no "Ostranena" name in these files.
- Quest files: gc/q038_ocs.scp (dialogs and speech, 2017 lines), gc/q038_ocs_chars.scp (15 quest NPCs), gc/q038_ocs_items.scp (barrels, barrel controller, mushroom).
- **Most of the engine side is not in the quest files. It is in `scripts/systems/aldaron.scp` lines 271-1582 of the archive:** the entry bottle and its controller, group and password logic, weapon racks, qtag doors, gates and tiles, group-wide qtag setting, the escort walker tiles, the freak and captain death events, the boss events, the exit/reset, the citizen and guard events, plus traps (2438-2581) and a cannon (2583-2656).
- Monsters are in `scripts/chars/specNPCaldaron.scp` lines 1808-4260: the boss, its helpers, camp guards, the freak, sand and undead mobs, citizens and town guards.
- Regions are in `scripts/spheremap.scp` 2721-2775: `Tajemna Hospoda` (map 0, 232,2073-248,2089, the entry tavern), `Oblast Cerne Smrti` (map plane 2, a large desert at 1468-1890 x 120-721), `... Labyrint` (plane 12), `... Boss` (plane 10, 5968,4036-6026,4083) and `... Jeskyne` (cave, plane 10). All carry `events=r_oblast_cerne_smrti` (sphereregion.scp:1263, on @exit -> f_ocs_exit).
- Journal: the dialog `d_ocs` (questmenu.scp:2742-2747) shows only the title and portrait 03164. **It has no per-stage text**, and `f_isDone_d_ocs` always returns 0, so the journal never marks the quest done.

### 2. Quest givers and where
None of these NPCs or controllers is in the archive's world save (sphereworld.scp/spherechars.scp have no c_q038/i_ocs/c_ocs objects). **All of them were GM-placed spawns** and their spawn points are not in the archive. Several NPCs also depend on GM-set tags on their spawn item (`ovladac`, `freak_spawn`, `capitan_spawn`).

| Stage | defname | name | where (from dialog text) |
|---|---|---|---|
| entry | c_q038_ocs_hospodsky | Hospodsky (innkeeper) | tavern; the bottle only works inside region "Tajemna Hospoda" (map 0 ~235,2085, inside Gondor territory per our converter's parent region) |
| prison | c_q038_ocs_edgar | Edgar (prisoner) | prison cell with the players |
| prison | c_q038_ocs_zalarnik | Zalarnik (Etkin, jailer) | prison |
| Salmat | c_q038_ocs_melichar | Melichar (jailer's son) | Salmat desert camp next to the prison |
| Salmat | c_q038_ocs_arnost | Arnost (Salmat clan leader) | command tent in the larger Salmat camp |
| Salmat | c_q038_ocs_liecitelka | Karena (healer) | Salmat camp |
| both | c_q038_ocs_johana | Johana (old woman, neutral) | lives alone |
| Sermat | c_q038_ocs_baltazar | Baltazar (other son) | Sermat camp in the pass to the city |
| Sermat | c_q038_ocs_adrian | Adrian (Sermat leader) | tent in the middle of the Sermat camp |
| Sermat | c_q038_ocs_carodejnica | Neferity (witch) | Sermat camp |
| Sermat | c_q038_ocs_hungar | Hungar (captain of the Sermat army) | Sermat camp. He is also the kill target of the Salmat branch |
| Sermat | c_q038_ocs_traig | Traig (moonshiner) | small house out in the desert |
| city | c_q038_ocs_sfenuk | Sfenuk | city |
| city | c_q038_ocs_umogon | Umogon (keeper of the lord's storehouse) | storehouse on the lake shore |
| city | c_q038_ocs_kubrogin | Kubrogin (healer/alchemist) | plague-infested quarter of the city |

### 3. Who can take it
- Both realms. The regions are tag.realm=3 (neutral), and no script checks realm, level, rank or skill.
- **Entry costs 1000 gold:** buy "Podivny alkohol" (`i_ocs_portovatko`) from the innkeeper (q038_ocs.scp:97-111) and drink it inside Tajemna Hospoda (aldaron.scp:305-345).
- Group capacity and timing are set by the GM on the controller `i_ocs_portovatko_ovladac`:
  - `maximum` players per run.
  - `cas_na_prechod`: the window in which the rest of the group may follow the first player.
  - `cas_blok`: a lockout before the next group.
  - Each run gets a group number (`q038_ocs_mygroup`).
  - Outside the tavern the bottle does nothing ("Vypil jsi alkohol"). GMs are teleported straight in.
- Profession matters only for the starter weapon rack (stage 1->2): warrior/crafter, priest, thief, ranger, mage, necro, mystic, shaman. Any other profession gets nothing from the racks but can still progress, because the jailer accepts stage 1 or 2.
- Repeatable in practice: leaving the OCS regions wipes `q038_ocs` and all OCS tags (f_ocs_exit_equip, aldaron.scp:1479-1502). You can buy another bottle. No cooldown beyond the controller's lockout timer.

### 4. Story and steps
Setting: a plague started in the royal city years ago. The city was sealed, and two clans fight over the desert outside it: the Salmat (also spelled Selmat) and the Sermat (also spelled Sarmat). The jailer's two sons married into opposite clans.

**Stages 0-2 (entry and prison):**
- Buy the bottle (1000 gp) and drink it in the tavern. The player is teleported in, qtag=1, and all equipment is stripped:
  - Equipped layers and backpack contents go into a new bag in a GM-set chest (`allitmove`), and a mount is shrunk to a figurine.
  - The player gets a robe, a night sight potion and 30 cooked fish (`ocs_prepare_for_start`, aldaron.scp:347-352). Food and night sight are set to 0.
- Talk to the prisoner Edgar. He gives the cell door password. The password rotates through 5 GM-set passwords, one per group (`ocs_set_password`). The door uses the generic password-door type.
- Take starter gear from a rack or cabinet (qtag 1->2).
- Ask the jailer Zalarnik where your things are ("in the silver chest") and choose which son to help. **The choice is group-wide** (`ocs_qtag_set`): everyone in the group gets qtag 3 (Melichar) or 17 (Baltazar), plus `ocs_pomaha` 1 or 2. This decides which camp's guards treat you as a friend.

**Stages 3-16 (Salmat branch, helping Melichar):**
- Melichar sends you to Arnost (stage 4). Arnost suspects the Sermat poisoned their water. Go to the healer Karena (5->6).
- Find the rare desert plant: double-click a `i_q038_ocs_mushroom` "Muchotravka" placed in the desert (6->7). Bring it back to Karena and receive the "neutralizer". This is only a message; no item is given (7->8).
- Double-click all 6 water barrels `i_q038_ocs_sud` (numbered 1-6 by the GM) to neutralize them (8->9).
- Arnost then sends you to Johana, who tells you the plague monster nests in a cave in the rocks (10->11). Kill the monster "Houbovec" (`c_ocs_freak`) -> 12. Only players registered on its spawn when they accepted from Arnost get the advance.
- Arnost then asks you to kill the enemy captain and take the pass keys. Killing Hungar (`e_ocs_capitan`) advances registered players from 13 to 14.
- Arnost sends you into the city and mentions a reward "in the bank in our camp" (15). Stages 16 ("took the treasure") and 33 ("entered the city") are **not set by any script**. They must come from GM-configured generic objects (see point 7).

**Stages 17-32 (Sermat branch, helping Baltazar):**
- Baltazar sends you to Adrian (18). Adrian wants to poison the Salmat water. Go to the witch Neferity (19->20).
- Find the same mushroom (20->21). Neferity gives you "poison", again only a message (21->22).
- Double-click the 6 barrels to poison them (22->23).
- Adrian sends you to Johana (24->25). Kill Houbovec (25->26).
- Adrian sends you to captain Hungar for the gate keys (27). Hungar wants booze: visit Traig in the desert (28->29), bring it back, and get the "keys", once more only a message (29->30).
- Adrian sends you into the city with a reward "in the dwelling next door" (31). Stages 32 and 33 come from GM objects.

**Stages 33-38 (city and king):**
- Sfenuk says the lord distributes an antidote; find Umogon (33->34).
- Umogon says the antidote is made by the healer Kubrogin, whom the lord keeps in the infested quarter. Fight your way to him (34->35).
- Kubrogin reveals that the antidote is a hoax: just coloured water, and the tyrant keeps everyone inside with lies (35->36).
- Tell Umogon (36). He shouts "Follow me, let's get him!" and the whole group goes to 37 (`ocs_qtag_set`). **Umogon is meant to become an escort NPC:**
  - He walks a chain of GM-placed `i_home_changer` tiles to the king.
  - He waits ("Tak pojd, honem!") when no registered player is within 10 tiles.
  - Citizens with `e_ocs_guard_npc` are meant to call him a traitor and attack him.
- At the king (38, not set by any script): the boss "Rascal" (`c_ocs_boss`) with his "Kralov Pomocnik" (King's helper) adds. The archive has no completion code.

### 5. Rewards (exact)
- **No exp, gold or item reward appears anywhere in the quest code.** No script calls addexp or gives gold or items on completion of any stage.
- Cost: 1000 gp (q038_ocs.scp:100,105).
- Starter kit on entry (aldaron.scp:348-350), all plain stock items, temporary in practice: i_robe (equipped), i_potion_Nightsight, 30x i_fish_cut_cooked.
- Rack kits on stage 1->2 (aldaron.scp:627-754). All are plain stock copper-tier gear (copper is the lowest of 7 weapon materials, zbrane/1copper.scp):
  - warrior/crafter: i_copper_heater_shield + i_sword_long_copper
  - priest: i_copper_heater_shield only
  - thief: i_copper_metal_shield + i_kryss_copper
  - ranger: i_crossbow_heavy_copper + 100x i_xbolt_piercing_copper
  - mage: i_staff_gnarled_copper + i_spellbook (fireball, flamestrike, harm, energy bolt, mind blast, night sight, magic reflection)
  - necro: staff + spellbook (flamestrike, harm, night sight, fire bolt, magic reflection) + i_customscroll_frostbolt
  - mystic: staff + spellbook (magic arrow, bless, greater heal, magic reflection)
  - shaman: staff + spellbook (magic arrow, harm, energy bolt, mind blast) + i_customscroll_frostbolt
  - The spellbooks are normal spellbooks with those spells added. Nothing magical.
- `i_ocs_nuzky` (aldaron.scp:821-842): a scissors spot that gives one plain i_scissors, then refuses for 3600 s.
- The "camp bank" and "dwelling" treasure (stages 15->16 and 31->32) is GM-placed, and **its contents are not in the archive. I can't say what it is.** The generic `i_ocs_qtag_expgate` (aldaron.scp:1397-1427) could have been used for these transitions: it gives `MORE2` exp, at most once per 10 minutes per player per gate, and sets the next qtag. Its MORE2 values were in the world save, which is not in the archive.
- Kill exp (`tag.experience`, specNPCaldaron.scp):
  - Rascal 850-950
  - Houbovec 380-400
  - King's helpers 100-110 each, but a helper raised by the boss has experience 0
  - camp guards 40-60
- Boss and freak loot: generic `i_monster_create` and `ITEM=i_gold` restock. No quest item drops.
- No special, magical or unique reward items, titles, skills or access found.

### 6. Quest-only items and monsters
- Items:
  - `i_ocs_portovatko` (entry bottle) and `i_ocs_portovatko_ovladac` (run controller)
  - `i_ocs_player_zaznam` (per-player record in a "database" container)
  - `i_q038_ocs_sud` (6 barrels) and `i_q038_ocs_sud_ovladac` (barrel controller)
  - `i_q038_ocs_mushroom` (Muchotravka, immovable, reusable)
  - weapon racks `i_ocs_stojan_plate/chain/ranger/gn`
  - qtag doors `i_ocs_dvere_qtag` + `_zatvaratko` (open for the configured qtag values)
  - qtag gates `i_ocs_qtag_gate`, `i_ocs_qtag_expgate` and the vargate condition `f_ocs_vg_qtag`
  - `i_ocs_qtag_dlazka` (floor tile that pushes back anyone without the right qtag; dead players are always pushed back)
  - `i_home_changer` (escort path tiles)
  - `i_ocs_exiter` (exit/reset check)
  - `i_ocs_trap` (lethal tile) + `i_ocs_trap_controller` (torch that disables traps for 10 s)
  - `i_ocs_cannon` + `i_ocs_cannon_wall` (fire a stock i_cannon_ball to lower a wall for 7200 s)
  - The neutralizer, poison, booze and keys are **not items**, only qtag steps and a sysmessage.
- Monsters and bosses made for the area:
  - `c_ocs_boss` "Rascal", the King: 17000-18500 HP, STR 8500, armor 140, wrestling 130. Events: bleed, parakrik, roundattack-pullall (40-50 dmg), resser, lava bombs (35%/10 s, range 6), dispel bombs (30%/20 s), curse (30-35 dmg). Its `tag(databaza,#04012462f)` is a hard-coded UID.
  - `C_ocs_boss_helper_1/2` "Kralov Pomocnik": 1000 / 750 HP, stun; #2 also leaps.
  - `c_ocs_freak` "Houbovec", the plague monster: 6500-7000 HP, STR 8000, bleed 30%, stun, phoenix fire 40-75, roundattack 50-60.
  - `c_ocs_guard_selmat` / `c_ocs_guard_sarmat` (mounted camp guards, 2500 HP).
  - `c_ocs_guard_npc_killer`, `c_ocs_citizen_fork/maul` (citizens that hunt the escort).
  - `c_ocs_citizen_hammer/bow` (citizens), `c_ocs_town_guard_1..3`.
  - Area mobs: c_golem_sand, c_elem_sand, c_corpser_sand, c_vulture, c_Sphynx (+c_sphynx_summon), c_zombie_ocs, c_ghost_ocs, c_liche_ocs, c_drapac_ocs, c_ocs_dusa_nakazeneho, c_ocs_duch_nakazeneho.
- The tevents that are still zero-effect stubs in our teventStubs.scp do the following on Erebor:
  - **e_ocs_capitan** (on Hungar, aldaron.scp:1086-1098): on @death, every player registered on Hungar's spawn (by accepting the task from Arnost) who is at qtag 13 goes to 14 with a message. Quest-bound stub.
  - **e_ocs_freak** (on c_ocs_freak, 1067-1083): on @death, registered players at 11 go to 12 (Salmat) or 25 to 26 (Sermat) with "the monster breathed its last". Quest-bound stub.
  - **e_ocs_citizen** (c_ocs_citizen_hammer/bow, 1535-1550): the stacked on=@hittry/@aftergetswing/@NPCSeeNewPlayer share one body, so it just says "Ne! Tady nemuzete byt! Vsichni zemreme!" (No! You can't be here! We'll all die!). Flavor, quest-area bound.
  - **e_ocs_town_guard** (c_ocs_town_guard_1..3, 1553-1568): the same pattern, says "Tady uvnitr mesta mohou zit jen zdravi! Zhynte!" (Only the healthy may live inside the city! Perish!). Flavor, quest-area bound.
  - **e_ocs_guard_npc** (c_ocs_citizen_fork/maul, c_ocs_guard_npc_killer, 945-1002): escort ambush. When it sees a player whose tag `ocs_guardi`==1, it shouts "<NPC> is a traitor! Kill him! / Protect our lord!" and attacks the NPC stored in the player's `ocs_guard_npc` tag. When hit, it attacks that NPC instead, and kills it instantly if it is conjured. It walks home if more than 10 tiles from its spawn. Quest-bound stub. **On Erebor it was already dead code: nothing ever sets ocs_guardi/ocs_guard_npc.**
  - **e_guard_ocs** (c_ocs_guard_selmat tag.guard=1, c_ocs_guard_sarmat tag.guard=2, 529-620): camp guards. On seeing a player whose `ocs_pomaha` does not match the guard's side, it says "<name>, odejdi nebo zemri!" or "nemas tu co pohledavat!" and attacks with teleport-to-target. Hitting a guard turns you grey. Summons that swing at a guard die instantly. Guards return home when drifting more than 10 tiles or fighting other city NPCs. Quest-bound stub: it needs the q038 faction tag.

### 7. Engine systems needed
- **Instanced group run:** a paid entry item usable only in one region, a group counter with a max size, a join window and a lockout timer, and a per-group record container. Group-wide qtag setting (`ocs_qtag_set`).
- **Gear stripping** into a stored bag in a chest, with mounts shrunk. **The controller deletes any stored bag still in the chest when its timer ends** (f_ocs_port_remove_bags, aldaron.scp:457-465).
- A password door with a rotating per-group password (generic t_door_password from dungeon_bonusy_yavanna.scp).
- Profession-based kit racks.
- Region exit hook that wipes progress (`r_oblast_cerne_smrti` @exit + a 2 s delayed check across the 7 OCS region names).
- Faction-aware guards that read a player quest tag. Spawn-linked kill registration: players get registered on a spawn item, and the monster's death advances them.
- Multi-object "use all 6" tasks (per-player tags on a controller).
- qtag-gated doors, gates and floor tiles, plus an exp gate with a cooldown.
- Escort walker on waypoint tiles.
- Lethal clothing-check traps with a torch to switch them off, and a cannon that breaks a wall.
- Boss summon/resurrection of helpers (already ported).
- Map planes 2, 10 and 12.
- Branching dialog quest UI (questDialog_*).

### 8. Shape label
mixed (instanced group dungeon run with a 2-way faction branch, talk/fetch/use-objects/kill/escort/boss), large.

### 9. Broken, unfinished or disabled on Erebor
- **The city chain cannot be finished from the scripts:**
  - `f_q038_ocs_umogon` and `f_q038_ocs_kubrogin` are called by both NPCs' speech and double-click handlers but **defined nowhere** in the archive, so their dialogs never open.
  - In `d_q038_ocs_kubrogin`, the "Protilatka" answer links to page `q038_umogon_send_him`, which does not exist (the reveal page is named `q038_who_is_autor`). The 35->36 button is on that unreachable page. The "Kdo jsi?" page is gated on qtag 34 while the buttons show at 35.
  - In the `d_q038_ocs_umogon` button handler, the `q038_there_is_no_cure` branch (36->37) is nested inside the `q038_try_to_do_it` if (q038_ocs.scp:1904-1923), so it can never fire.
- No script sets stages 16, 32, 33 or 38. They depend on GM-configured objects that are not in the archive. Nothing ever finishes the quest, and the journal "done" check is hard-coded to 0.
- `c_q038_ocs_sfenuk` is a copy-paste of Traig: `name=Traig` and `TSPEECH=spk_q038_ocs_traig` (q038_ocs_chars.scp:672-679). Speaking to him runs Traig's speech; double-click works.
- The escort is unfinished:
  - Nothing sets `ocs_guardi`/`ocs_guard_npc` (for e_ocs_guard_npc) or Umogon's `walker` tag.
  - `f_ocs_save_players_on_npc` is called on `arg(ovladac).tag(databaza)`; the controller's `databaza` must be GM-set.
- Kick logic (`f_ocs_port_kick_players`, aldaron.scp:467-490) tests `tag(player).region`, which is never set, instead of `tag(hrac<i>)`. The nested conditions also require being in two regions at once, so nobody is ever kicked.
- `f_ocs_qtag_set` sends a debug "NEFACHA" message to every player record of other groups, and tells the chooser himself "someone in your group decided for you".
- `i_customscroll_frostbolt` (necro and shaman kits) has **no itemdef anywhere in the archive**, so that scroll could not be created.
- The exit wipe skips players who land in region "Stredozeme". Whether that is intended can't be determined.
- Text slips: Arnost calls the healer "Santana" (she is Karena). The Sarmat guard's DESCRIPTION is a copy-paste ("Marghulova jizdni se sekerou"). Salmat/Selmat and Sermat/Sarmat spellings are mixed.
- The boss and its helpers carry a hard-coded database UID `#04012462f`. It would need re-setting in any new world.

### 10. Port status in moriaengine
- **Items/NPCs converted only (defs exist, no quest logic).** No quest logic in scripts/Basic/Quests.
- Converted defs:
  - `scripts/converted/questy/q038_ocs_chars.def` (all 15 c_q038_ocs_* NPCs)
  - `scripts/converted/questy/q038_ocs_items.def` (i_q038_ocs_sud, _ovladac, _mushroom)
  - `scripts/converted/questy/q038_ocs.def` (only `[SpeechStub]` placeholders; dialogs and functions commented out by the converter)
  - `scripts/converted/systems/aldaron.def` (i_ocs_* items, i_home_changer, c_ocs_guard_npc_killer, all behaviour dropped)
  - `scripts/converted/chars/specNPCaldaron.def` (c_ocs_boss, C_ocs_boss_helper_1/2, guards, freak, citizens, town guards, area mobs)
  - `scripts/converted/map.def` (regions a_Tajemna_Hospoda and a_Oblast_Cerne_Smrti*; their `events = r_oblast_cerne_smrti` has no implementation)
- Partly ported (boss combat only, #1022):
  - `scripts/Basic/AbilitiesAndTalents/MonsterSpecialAttacks/BossDeaths.cs`: e_ocs_boss_resser, e_ocs_boss_resurrection_able, e_explosionlava
  - `DispelBomb.cs`: e_explosiondispell
- Still zero-effect stubs in `scripts/Basic/AbilitiesAndTalents/teventStubs.scp`: e_guard_ocs (line 183), e_ocs_capitan (467), e_ocs_citizen (470), e_ocs_freak (473), e_ocs_guard_npc (476), e_ocs_town_guard (479). All are quest-bound; see point 6.
- Missing systems: the entry, group and instance controller, gear stripping, the region exit reset, the password door (only converted defs in dungeon_bonusy_yavanna.def), qtag doors/gates/tiles, the barrels and mushroom, the escort walker, the traps and the cannon.

---

## q039 Stinove zbrane - "Shadow weapons"

1. **Id / files.** Journal questMenu[45] "Stinove zbrane" (qtag `q039_shadow_weapon`, no. 45). scripts/questy/q039_shadow.scp (368), q039_shadow_items.scp (skulls, heads, materials + 5 lore books), q039_shadow_weapon_chars.scp (smith + 4 "siblings"). The weapon itself and its events are in **systems/aldaron.scp:1875-2060** (outside the list); spawn groups in chars/groups.scp (s_shadow_quest_low/middle/top) and a spawn randomizer `i_shadow_quest_randomizer` (aldaron.scp:2412).
2. **Giver.** `c_q039_kovar` "Kovar" (ageless nameless smith, mithril smith hammer). No location; not in the save -> GM-placed.
3. **Who.** Both realms. Level > 40 (q039_shadow.scp:115). Weapon type depends on profession. Repeatable only in the sense that a destroyed weapon can be re-made (stage 6 -> back to stage 3, materials again).
4. **Steps.**
   - Bring 5 intact boss skulls: Lord of Mahat (`i_boss_skull_mahat`), Mighty Demon / Carn Dum (`i_boss_skull_cd`), Lord of Frost / Ice Cave (`i_boss_skull_md`), Aresion / Tumma Osto (`i_boss_skull_to`), the Paladin of the Lost City (`i_boss_skull_ztracene_mesto`). They drop from those bosses (chars/specNPCMarek.scp:112,478,822,1001; specNPCaldaron.scp:865) and have a 40% chance to crumble when picked up from the corpse (`t_head_in_corpse`, aldaron.scp), and crumble if dropped on the ground.
   - Bring the heads of the 4 traitors Verlix, Bakchor, Gloata, Mercelis: quest bosses spawned by a randomizer, who **vanish if not killed fast** (50/50/80/200 s after seeing a player, `e_shadow_weapon_brother`).
   - Bring materials: 10 shadow-golem ore (`i_ore_shadow`, ore not ingots), 25 fire stones (`i_coal_elemental`), 1 highly flammable oil (`i_q039_shadow_weapon_oil`, dropped by c_reaper_fire, specNPCsarig.scp:2159), 1 hero's blood (`i_q039_shadow_weapon_blood_bag`, taken from a GM-placed static `i_q039_shadow_weapon_blood` with respawn timer), 5 fire-resistant skins (`i_q039_shadow_weapon_skin`, 0-1 from c_Ogre_boss, ogres.scp:1169), 1 "Tear of the Mountains" gem (`i_q039_shadow_weapon_gem`, reward of q040 Blazen).
   - Smith jokes "now 20 mithril ingots and 10 elven logs... just kidding", then works for 1 hour (timer memory on the player), then hands over the weapon.
5. **Rewards.** 3000 exp for skulls (:249), 4000 for heads (:258), 2000 for materials (:268); then `f_shadow_weapon_create` (:289). **SPECIAL - "Stinova zbran" (Shadow weapon), unique, soul-bound** (aldaron.scp:1899-1944):
   - Item by profession, all obsidian: thief `i_kryss_obsidian` (+20 Fencing, +10 Tactics); warrior `i_sword_long_obsidian` (+20 Fencing [skill 42, probably meant Swordsmanship], +10 Tactics); priest `i_mace_obsidian` (+20 Mace Fighting, +10 Tactics); ranger and crafter `i_crossbow_heavy_obsidian` (+20 Archery, +10 Tactics); necro/mage/shaman `i_staff_gnarled_obsidian` (+10 Magery, +10 Eval Int); mystic `i_staff_gnarled_obsidian` (+20 Wrestling, +10 Eval Int). Skill values are imi_value x10, i.e. +20.0 skill.
   - Plus +10% damage each vs undead+demons, dragons, humanoids, animals (incl. spiders), eyes/gazers/golems (imi 101-105).
   - Shadow bonus while equipped (`e_shadow_weapon_dmg`): melee classes add damage x (1000 + 25 x power)/1000 against NPCs with resist_magic < 1000, where power = 10 x remaining lives / 15; i.e. **+100% to +125% damage vs monsters** (live setting `nastaveni_efekt_shadow=1000`, save/sphereworld.scp:2995). Casters get the same multiplier on spell power (magie.scp:187-189).
   - Bound: colour 07FFF, can't be dropped, given or picked up by others; returns to the owner's pack in 10 s if moved. **15 lives**: every owner death costs one; at 15 it dissolves and the quest goes to stage 6 (re-make for full materials). Repair: 2 shadow ore + 5 fire stones + 1 fire-resistant skin resets the death counter but lowers max lives by 1 and the weapon's max damage by 1.
6. **Quest-only items / monsters.** 5 boss skulls (+ unused `i_boss_skull_ps` Lord of Fear), 4 heads, oil, hero's blood (static + bottle), skin, gem, `i_mem_workingTimer`, 5 lore books (Kniha_011-015_klaj: about the skin, oil, gem, blood). Monsters: `c_q039_verlix` (bleed 70%, crits 25%, vanishes 50 s), `c_q039_mercelis` (archer, sniper + multishot, 50 s), `c_q039_gloata` (mage, curse + lightnings, 80 s), `c_q039_bakchor` (summon-eater vampire, curse, life leech, detect, 200 s). Spawn groups s_shadow_quest_low/middle/top; note Verlix is in none of the visible groups' rows except a long block of c_q039_verlix lines (groups.scp:926+).
7. **Systems.** Quest dialogs, boss loot skulls with break chance, timed vanishing bosses + spawn randomizer, NPC work timer, soul-bound item with death counter and owner-return, profession-based item creation, magic item property system, damage hook vs NPCs.
8. **Shape.** mixed: boss-loot fetch + timed kill + gather + craft-wait; large.
9. **Problems.** Warrior gets +Fencing on a longsword. The crystal stand `i_shadow_weapon_krystal_giver` and 7 crystals in aldaron.scp are related "shadow" content but nothing in q039 uses them (its GM dialog also writes chance1 into all 7 slots - bug). The repair line has unbalanced parentheses (`...-1))`). Smith's work timer goes on the player, so it is per-player (fine). `i_boss_skull_ps` drop-on-ground `remove` is commented out.
10. **Port.** Items/NPCs converted only: `converted/questy/q039_*.def`; the bag `i_shadow_weapon_bag` is a ContainerDef in `converted/systems/aldaron.def:337` but `f_shadow_weapon_create`, `e_shadow_weapon`, `e_shadow_weapon_dmg`, `t_weapon_shadow` are commented out (:343-360). Boss skull drop converted for the Paladin (`converted/chars/specNPCaldaron.def:627`). `i_ore_shadow` and `i_coal_elemental` exist (mining, monster loot). **`e_shadow_weapon_brother` is a quest-bound zero-effect stub** (teventStubs.scp:572). Monster events e_npc_bleed, e_npc_critic, e_npc_sniper, e_npc_multishot, e_npc_curse, e_lightnings, e_summoneater_vamp, e_lifeleech, e_npc_detect, e_looter are real in the port (MonsterSpecialAttacks).

---

## q040 - Spolecnost drakobijcu (Dragonslayers' Society) family

Files read: `gc/q040_dragons.scp`, `gc/q040_dragons_chars.scp`, `gc/q040_dragons_items.scp`, journal `gc/questmenu.scp` 76-80 and 2775-2930. Code outside the quest files, all in the archive: `scripts/chars/specNPC_dragons.scp` (the quest dragons, header "MEGAQUEST - DRAKOBIJCI (c) Marek"), `scripts/systems/aldaron.scp` (scale itemdefs and the dragon-armour/weapon crafting), `scripts/sphereitem_potions.scp` (dragon potions), `scripts/systems/magicITEMS.scp` + `magic_defs.scp` (weapon properties), `scripts/spheremap.scp` / `sphereregion.scp` (fortress and lair areas), and `save/sphereworld.scp` (the armour resist values are stored only there, as VarNames).


---

## q040_dragons - Spolecnost drakobijcu

1. **Id / name / files.** qtag `q040_dragons`, journal questMenu[46] `d_dragons`. Czech "Spolecenstvi drakobijcu" = "Fellowship of Dragonslayers". Files: q040_dragons.scp (doorman 33-361, Vartian 363-543, Predstaveny + portals 732-1375, death event 1377-1387), q040_dragons_items.scp (parchment 25-548, poison-bag collector 1-23, fortress gate 545-564), q040_dragons_chars.scp, specNPC_dragons.scp (dragons).
2. **Givers.**
   - `c_q040_doorman` "Drakobijec", who reveals his name as Ikreon (q040_dragons.scp:163). He guards the fortress entrance. The archive has an area `Pevnost drakobijcu vstup` (map 0, RECT 3939,21-3964,47, P 3952,35,20, spheremap.scp:5079).
   - `c_q040_vartian` "Vartian": the dragon-hunt task giver.
   - `c_q040_predstaveny` "Predstaveny" (the Order's leader): lore and lair portals.
   - All of these stand inside `Pevnost drakobijcu` (mapplane 11, RECT 5417,3527-5463,3568, spheremap.scp:5091). That is implied: the texts say "in our fortress" and `i_q040_gate` admits only qtag 4-49.
   - All NPCs are GM-placed. The archive has **no spawn or world spot** for any c_q040_* char or i_q040_* item: the save holds only VarNames, and only the three quest files mention the defnames.
   - The Predstaveny must be spawned from a spawn whose `tag(ovladac)` points at a GM-configured `i_q040_dragons_port_controller` (q040_dragons.scp:886-990). A GM uses its dialog to register the spawn uid of each lair's dragon.
3. **Who can take it.**
   - No realm check. The fortress and lair areas carry `tag.realm=3`.
   - Character `tag(level) >= 40` to talk to the doorman (q040_dragons.scp:74).
   - Start: double-click the `i_q040_pergamen` "Pergamen o drakoch" while qtag is 0 (items.scp:441-446). Where the parchment lies is not in the archive (GM-placed, presumably).
   - Once only: no reset anywhere.
   - Lair portals have a 3-hour per-lair cooldown, and each player may pass a portal once per game day.
4. **Story / steps.**
   - **Password.** Reading the parchment, a long history of dragon hunters (Glaurung, Ancalagon, Scatha, the founders Beroner and Cistor), stores a random leader name: 1 of 20 names, items.scp:25-47. The name appears in book line 343 ("...byl zvolen <name> z jezerniho lidu") and the qtag becomes 100. The doorman then asks in a text-entry gump "What is the name of our last elected leader?" (q040_dragons.scp:327-361). The right answer gives qtag 101.
   - **Introduce yourself.** Under "Hledam praci" the player tells their name and accepts the task: bring the head of the traitor Gloata, `i_q039_shadow_weapon_head_Gloata`, from q039's Gloata (`c_q039_gloata`). That sets qtag 1.
     - Side branch: refusing to give your name sets qtag 50. You then persuade him 4 times. Each "give up" adds +2 to `tag(dragons_doorman_refuse)` and so raises the price. Finally you bring a roast pig `i_pig_roast` plus (2 + refuse count) `i_ingot_adamantinum`, which puts you back at qtag 101 (q040_dragons.scp:59-68, 108-153, 295-316).
   - **Head handed in** gives qtag 2. Ask about his armour, "Co je to za brneni?", then "I want to be a dragonslayer". He hands you `i_q040_poison_bag_collector` and the qtag becomes 3.
   - **Poison sac.** Kill the Bahenni drak (`c_drak_bahno`, 30,000 HP sea-serpent "mud dragon", area `Doupe bahenniho draka` plane 11) and use the bag on its corpse. Handing it in gives qtag 4: fortress access through `i_q040_gate`.
   - **Six dragons in order.** Vartian assigns brown, then green, fire, ice, black and white. Each acceptance moves the qtag to the odd value. Predstaveny opens a portal (`i_q040_dragons_gate`) to that lair, and to any lair already unlocked:
     - The portal opens only if the lair's spawn currently holds a dragon (`more2 > 0`).
     - Per-lair cooldown is 3 hours after an opening.
     - The portal lives 120 s, takes at most 5 players, and each player may pass once per game day.
     - The dragon's spawn is tagged `killer` = the player who opened the portal.
   - **Kill credit.** When the dragon dies, `e_q040_dragon_death` advances that opener's qtag to the even value. Lair positions are listed in q040_dragons.scp:771-788.
   - Each dragon has its own hints in Predstaveny's dialog: weapon types, immunities (black = immune to all magic, white = immune to physical), potions.
   - **Gold dragon** (qtag 17 to 18), then **Marguth Zapomenuty** ("the Forgotten", a named ancient dragon that burned Gondolin; qtag 19 to 20). The Marguth fight is meant for a group of 6 in full dragonslayer armour. Neither stage can be completed (see 9).
5. **Rewards.**
   - Gloata head: `addexp(4000)` (q040_dragons.scp:273).
   - Poison sac: `addexp(3000)` (q040_dragons.scp:289).
   - Vartian pays `addexp(1000 + qtag*500)` when you **accept** each hunt, not when you finish it (q040_dragons.scp:536). That is 3,000 / 4,000 / 5,000 / 6,000 / 7,000 / 8,000 / 9,000 at qtag 4/6/8/10/12/14/16. The seventh payment (9,000, at qtag 16) is for the gold dragon, which cannot be finished.
   - Total exp: 49,000. No gold, no items from this quest itself.
   - SPECIAL non-item rewards:
     - **area access** to the dragonslayer fortress (`i_q040_gate`, items.scp:553-560);
     - **lair portals** through Predstaveny;
     - progress gates for the armour pieces that Kovar/Dratenik will make (q040_dragons_kovar);
     - the right to deal with the Alchemist, Rakchin and Blazen (all need qtag 4-49).
   - Kills drop loot themselves: each quest dragon drops gold and its colour of scale, `i_dragon_scale_brown/green/red/blue/black/white` (specNPC_dragons.scp @NPCRestock). The scales are plain stackless items that crumble if dropped on the ground (aldaron.scp:4517-4645). They have no use except the armour crafting.
6. **Quest-only items, monsters and helpers.**
   - Items:
     - `i_q040_pergamen` (lore book + password);
     - `i_q040_poison_bag_collector` (t_collector, target "Body of Bahenni drak");
     - `i_q040_gate` (fortress gate, qtag 4-49);
     - `i_q040_dragons_gate` (timed lair portal);
     - `i_q040_dragons_port_controller` + `i_target_dragon_port_controller` (GM setup tool);
     - `i_mem_name_info` (memory on Ortlion that stores who told him their name).
   - Monsters (specNPC_dragons.scp): `c_drak_bahno`, `c_drak_hnedy`, `c_drak_Zeleny`, `c_drak_ohen`, `c_drak_led`, `c_drak_cerny`, `c_drak_bily`.
     - All have 30,000-30,500 HP and `tag(monstertype,dragon,50)`.
     - The six true dragons carry `TEVENTS=e_q040_dragon_death` and `tag(q040_dragons_qtag_wanted, 6/8/10/12/14/16)`.
     - There is also the GM test dragon `c_drake_enzo_test` (aldaron.scp, wanted=16).
   - **`e_q040_dragon_death`** (q040_dragons.scp:1377-1387). On the dragon's death:
     - it takes the spawn's `tag(killer)` (the last player to open that lair's portal);
     - if that player's `q040_dragons == wanted-1`, it sets it to `wanted` and sends "Zabil si draka, Vartian bude stastny";
     - it always bumps the global stat counter `dragons_<wanted>` on `#0400075A4`.
     - The "killer must be in the lair" check is commented out. So the opener gets credit even if somebody else killed the dragon. Nobody else in the party advances.
   - Flavour NPC `c_q040_ortlion` "Ortlion":
     - Telling him your name only sets a flag. Refusing makes him ignore you forever.
     - Calling him a weakling gets you **killed outright** (`src.kill`, q040_dragons.scp:1726).
     - No quest, no reward.
   - Lair areas (spheremap.scp:5103 ff.): `Doupe bahenniho/hnedeho/zeleneho/ohniveho/ledoveho/cerneho/bileho draka`, on planes 11/12.
7. **Engine systems.**
   - Quest dialogs (questDialog_*) and the journal.
   - A text-entry password gump plus a per-player random value.
   - A readable multi-page book gump.
   - The collector item type (`t_collector`: target a named corpse, fill a bag).
   - Two linked regions: the entrance on map 0 and the fortress on plane 11.
   - `r_dragon_fortress`, which adds `e_dragons_fortress` to players. Inside the fortress player swings and hostile spells do 0, and snooping/stealing is detected.
   - Map planes 11/12.
   - Spawn-linked portals: a GM controller item maps lair to spawn, plus cooldowns by game day/hour, a pass counter and a 120 s timer.
   - Kill credit through a death event plus the spawn's killer tag.
   - Boss dragons with the #1022 breath/aura abilities. The gold dragon was meant to morph between breaths.
   - Global stat counters.
8. **Shape.** mixed (puzzle + fetch + kill chain + boss), large.
9. **Broken / unfinished.**
   - **Gold dragon and Marguth do not exist.**
     - No chardef in the archive for a gold dragon or for "Marguth/Margut Zapomenuty".
     - Lair positions 7 and 8 are the placeholder `2,2,2,2` (q040_dragons.scp:784-787).
     - Nothing drops `i_dragon_scale_gold`: it is referenced only in the crafting check and in Rakchin.
     - So qtag 17 to 18 can never happen.
   - **Vartian can never set 19**: `want_to_kill_dragon` lists 4-16 only (q040_dragons.scp:395-401). So the Marguth stage, and everything gated on qtag 19 or higher, is dead. The predstaveny portal-8 branch is dead too.
   - Gloata journal mismatch. The journal for stage 1 says "bring Gloata's head", but stage 2 says "you brought Bakchor's head". The header comments mention "Pan Strachu + Bakchor". The code consumes only Gloata's head, with the skull check commented out (q040_dragons.scp:48-57).
   - The journal says "bring its scale as proof" for each dragon. No scale is checked anywhere: progress comes only from the death event.
   - Predstaveny's button "Zabiti Margutha?" opens a page named "Zabiti Margutha Zapomenuteho?" (q040_dragons.scp:1049 vs 1165). The labels differ, so that page is probably unreachable.
   - `q040_npcIsTired` is fully commented out and always returns 0.
   - The stage-20 look text is the placeholder "ZABIL VSETKYCH DRAKOV - DOPLNIT TEXT" (q040_dragons.scp:1027).
   - Predstaveny says the Marguth portal holds 6 people; the portal code allows 5.
   - Vartian's "Nesu ti" goodbye uses the copy-pasted id `q034_goodbye`, which is harmless.
   - The parchment and all NPCs need GM placement plus port-controller setup. There are no spots in the archive.
10. **Port status: items/NPCs converted only.**
    - Converted defs (all of them, defs only):
      - NPCDefs for all 9 c_q040_* in `scripts/converted/questy/q040_dragons_chars.def`;
      - `[SpeechStub]` for every spk_q040_*, plus `i_q040_dragons_gate`, `i_q040_dragons_port_controller` and `i_mem_name_info`, in `converted/questy/q040_dragons.def`. All dialogs, functions and `[events e_q040_dragon_death]` there are commented out by the converter (line 136);
      - `i_q040_pergamen` (without its book text or name function), the collectors and `i_q040_gate` in `converted/questy/q040_dragons_items.def`;
      - the journal rows in `converted/questy/System/questmenu.def:59-63`;
      - the fortress and lair regions in `converted/map.def:5418-5539`. The region type `r_dragon_fortress` is commented out (`converted/region.def:643`).
    - The dragons are converted with scale loot (`converted/chars/specNPC_dragons.def`, `Loot = i_dragon_scale_*`) and have their #1022 combat abilities.
    - **`e_q040_dragon_death` is the zero-effect stub** `[PassiveAbilityDef a_tevent_e_q040_dragon_death]` in `scripts/Basic/AbilitiesAndTalents/teventStubs.scp:536`. On Erebor it is the quest's kill-credit hook described in 6; it does nothing else. The only other q040 event is `e_dragons_fortress`, from the fortress region, which is not stubbed or ported.
    - No `t_collector` implementation exists in C#.
    - No dragons or Society NPCs are placed via scripts/Setup.

## q040_dragons_alchymista - Drakobijec Alchymista

1. **Id / name / files.** qtag `q040_dragons_alchymista`, questMenu[47] `d_dragons_alchymista` ("Alechymista" typo in the index). "Dragonslayer Alchemist". q040_dragons.scp:1389-1596, chars.scp:473-559.
2. **Giver.** `c_q040_alchymista` "Alchymista", a small hobbit with an acid-burnt apron, inside the dragonslayer fortress. GM-placed, no spot in the archive.
3. **Who.** `tag(level) >= 40` and `q040_dragons` 4-49 (fortress access). No realm check. Once only. Refusing sets qtag 100, and then he never sells to you: "Odmitl jsi mi pomoci, ted ti neprodam nic!". The journal says "try to persuade him", but there is no persuade path.
4. **Steps.**
   - Ask for work. He needs elixir stock: **30 i_potion_HealElixir, 30 i_potion_RefreshElixir, and 10 each of i_potion_StrengthElixir, i_potion_CleverElixir, i_potion_AgilityElixir, i_potion_EnergyElixir, i_potion_Stoneskinelixir, i_potion_Shieldelixir** (120 total; q040_dragons.scp:1429-1444).
   - Accept (qtag 1), bring them, hand them in (qtag 2).
5. **Rewards.**
   - No exp, gold or item. `f_quest_complete(47)` on acceptance is only a quest-stone statistic.
   - SPECIAL: it **unlocks his shop** (q040_dragons.scp:1475-1490, 1543-1582):
     - `i_potion_dragon_poison` "Protijed na draci jed", 2,000 gp: removes the dragon-poison effect `i_fire_dragon_efect_poison`.
     - `i_potion_dragon_fire` "Lek na popaleniny", 2,000 gp: removes the dragon-burn effect `i_fire_dragon_efect_fire`.
     - `i_potion_dragon_white` "Ochrana mysli", 2,000 gp: removes the white-dragon effect `i_fire_dragon_efect_white`.
     - `i_potion_dragon_resist` "Lektvar ochrany proti drakum", 10,000 gp: **+10% dragon_resist for 300** (f_eqrune timer). Dragon breath damage is cut by that percentage (sphereitem_potions.scp:923-1040).
   - All four have `SKILLMAKE=ALCHEMY 200.0`, so in practice they cannot be crafted and this shop is their source. They break if dropped.
6. **Quest-only items.** None besides the four potions above. The elixirs are stock items.
7. **Systems.** Quest dialog, a resource count + consume, a dialog-driven gold shop, and timed potion buffs.
8. **Shape.** fetch, small.
9. **Broken.**
   - Nothing blocking.
   - The "refused" state is final despite the journal text.
   - The dialog stores the alchemist qtag in a local named `q040_dragons` (cosmetic).
10. **Port: not ported.**
    - NPCDef `c_q040_alchymista` is converted with a SpeechStub.
    - The four dragon potions are **really ported** as potions: `scripts/Basic/Potions/DragonPotions.cs`, `PotionSettings.dragonResist = "300,10,10"`, `PotionEffectPlugin.DragonResist`, and the `.DragonResist` player command (#927). Dragon breath reads that resist in `DragonFire.cs`, `DragonEvents.cs` and `DragonCageAndBlood.cs`.
    - The quest and the shop gate are missing.

## q040_dragons_rakchin - Drakobijec Rakchin

1. **Id / name / files.** qtag `q040_dragons_rakchin`, questMenu[48] `d_dragons_rakchin`. Rakchin is a one-armed veteran: the gold dragon bit off his right hand. q040_dragons.scp:1732-1891, chars.scp:661-752.
2. **Giver.** `c_q040_Rakchin` "Rakchin", in the fortress. GM-placed, no spot.
3. **Who.**
   - `tag(level) >= 40` and `q040_dragons` 4-49.
   - The offer appears **only while `q040_dragons` is 10-15**, i.e. after the fire dragon and before the white one (q040_dragons.scp:1814). If you miss that window you can never take it.
   - Hand-in needs `q040_dragons >= 18`.
   - Once only. Refusing sets 5.
4. **Steps.**
   - Accept "kill the gold dragon and bring me its scale" (qtag 1).
   - Later he gives advice: the gold dragon morphs and copies other dragons' breaths; flee when it glows pale.
   - Bring 1 `i_dragon_scale_gold` (qtag 2).
5. **Rewards** (q040_dragons.scp:1871-1879):
   - `addexp(10000)`.
   - 1x `i_mi_bag_superHigh`, SPECIAL. "Pytlik s magickym predmetem" is the top-tier ("cierne") magic-item bag: `more1=2`, `more2=2`, so 2 magic properties. Double-clicking it generates a random magic item through `f_mif_create` (magicITEMS.scp:903-912 and the `mit_main` typedef).
6. **Quest-only items.** `i_dragon_scale_gold` (aldaron.scp itemdef "Zlata draci supina"; no source exists).
7. **Systems.** Quest dialog, the magic-item generator, a dependency on the main chain's qtag.
8. **Shape.** kill/fetch (boss), small.
9. **Broken.**
   - **Unreachable**: there is no gold dragon or gold scale source, and qtag 18 cannot be reached (see q040_dragons, point 9).
   - The journal's "refused" line tests `q040_dragons_alchymista == 5` instead of rakchin (questmenu.scp:2884).
   - The window-only offer is probably unintended.
10. **Port: not ported.** NPCDef + SpeechStub are converted. `i_mi_bag_superHigh` exists as a real bag (`scripts/Basic/MagicItems/MagicBags.def:37`). `i_dragon_scale_gold` is converted (`converted/systems/aldaron.def:665`).

## q040_dragons_blazen - Blaznivy Drakobijec

1. **Id / name / files.** qtag `q040_dragons_blazen`, questMenu[49] `d_dragons_blazen`. "Mad Dragonslayer": a survivor of Marguth driven mad by greed for Marguth's artefact. q040_dragons.scp:1893-2032, chars.scp:758-832, collector items.scp:566-588.
2. **Giver.** `c_q040_blazen`, def name "Blazen", dialog title "Drakobijec". In the fortress. GM-placed, no spot.
3. **Who.** `tag(level) >= 40`, `q040_dragons` 4-49, and the offer needs `q040_dragons >= 19`. Once only. Refusing is meant to set 5.
4. **Steps.**
   - Before stage 19 he only raves about Marguth, his teeth and fire, and "the mightiest weapon".
   - At stage 19 or later: accept "bring me a sample of the Forgotten's blood, it heals" (qtag 1).
   - Collect the blood with `i_q040_dragon_blazen`: a collector targeting "Body of Margut Zapomenuty" that does not remove the corpse and can be filled repeatedly.
   - Hand it in (qtag 2).
5. **Rewards** (q040_dragons.scp:2016-2023):
   - `addexp(2000)`.
   - 1x `i_q039_shadow_weapon_gem` "Slza hor" ("Tear of the Mountains", q039_shadow_items.scp:358). SPECIAL only as a quest ingredient: one of the materials of q039's shadow-weapon craft (q039_shadow.scp:316). It has no stats and vanishes if dropped.
6. **Quest-only items / monsters.** `i_q040_dragon_blazen` blood collector. Boss "Margut(h) Zapomenuty": never defined.
7. **Systems.** Quest dialog, the collector, a boss.
8. **Shape.** boss/fetch, small.
9. **Broken: completely unreachable.**
   - Marguth does not exist and qtag 19 cannot be reached.
   - The accept handler never gives the player the collector (no `newitem`).
   - The dialog copies the qtag into `arg(q040_dragons_rakchin)` but tests `arg(q040_dragons_blazen)` (q040_dragons.scp:1937). That variable is never set, so it evaluates empty/0. As a result:
     - the hand-in option (`== 1`) never shows;
     - accept is offered again every time.
   - The refuse button id `q40_refuse` does not match the handler's `q040_refuse`, so refusing does nothing.
10. **Port: not ported.** NPCDef + SpeechStub and the `i_q040_dragon_blazen` def are converted. The gem is converted (`converted/questy/q039_shadow_items.def:100`).

## q040_dragons_kovar - Drakobijci - brneni

1. **Id / name / files.** qtag `q040_dragons_kovar`, questMenu[50] `d_dragons_kovar`. "Dragonslayers - armour". q040_dragons.scp:545-730, chars.scp:192-367. The crafting is in aldaron.scp:4485-4660 (scale check/consume, scale itemdefs, `f_dragon_armor_creator`, `t_dragon_armor`), 4753-5087 (Kovar) and 5089-5320 (Dratenik). Resist values: save/sphereworld.scp VarNames.
2. **Givers.**
   - `c_q040_kovar` "Kovar" (smith): plate, chain and ring armour, and every weapon except staves.
   - `c_q040_dratenik` "Dratenik" (tinker/leatherworker): studded and bone armour, and staves only.
   - They share the speech, the dialog and the qtag, so one completion unlocks both. Both stand in the fortress. GM-placed, no spot.
3. **Who.**
   - `q040_dragons` 4-49. No level check on them. No realm check.
   - Each armour piece is gated by main-chain progress (see 5).
   - Once only for the unlock. The crafting is repeatable for as long as you pay.
4. **Steps.**
   - "Vylepsit veci?": both have a score to settle with the lord of the Fortress of Fear. Accept (qtag 1).
   - Kill **Lord Strachu** (`c_strach_mage_chief2`, specNPCsarig.scp:1893). His loot has `i_boss_skull_PS` "Lebka Pana Strachu". It has `t_head_in_corpse`: a **40% chance it crumbles** when picked out of the corpse (aldaron.scp:2383-2398).
   - Hand the skull in (qtag 2).
   - Then hand over the piece to convert, plus gold and scales. A target cursor picks the item, which must be in your pack, within 6 tiles of the NPC.
5. **Rewards.**
   - No exp or items for the unlock. SPECIAL: a **paid crafting service**. The unlock is the reward.
   - Armour costs **50,000 gp** per piece. Requirements, with the scales from the main chain:

   | piece | needs `q040_dragons` > | scales | dragon_resist added |
   |---|---|---|---|
   | gorget (krk) | 5 (after brown) | 1 brown | 5% |
   | helm (helma) | 7 (after green) | 1 brown + 1 green | 8% |
   | gloves (rukavice) | 9 (after fire) | 1 green + 1 red | 8% |
   | arms (lokte) | 11 (after ice) | 1 blue + 1 red | 10% |
   | legs (nohy) | 13 (after black) | 2 black + 1 blue | 10% |
   | chest (hrud) | 15 (after white) | 2 white + 1 black | 15% |

   - A full set gives **56% dragon resist**. With the alchemist potion it is 66%.
   - Resist values: VarNames `nastaveni_dragon_resist_krk=5`, `helma=8`, `rukavice=8`, `lokte=10`, `nohy=10`, `hrud=15` (save/sphereworld.scp:2661-3116). These names are not defined in any script.
   - What converting a piece does (`f_dragon_armor_creator`, aldaron.scp:4646): renames it "Draci brneni", colour 09CE, adds `events +t_dragon_armor` and `tag(dragon_resist,N)`. While worn it adds N to the wearer's `dragon_resist` tag and reports the total. The base item's AR and other properties are untouched.
   - What dragon_resist does: dragon fire, breath and aura damage is reduced by that percentage. If the hit is no larger than the resist value it is ignored entirely ("Draci ohen te nezranil", aldaron.scp:3008-3010 and later blocks).
   - Kovar accepts, per slot:
     - plate: chest / female plate, legs, arms, gloves, helm (nose, closed, plate, bascinet), gorget;
     - chain: tunic, legs, coif. The arms, gloves and gorget slots of the chain set take leather sleeves, leather gloves and leather gorget;
     - ring: tunic, legs, sleeves, gloves, open helm.
   - Dratenik accepts:
     - studded: tunic / female, legs, sleeves, gloves, leather cap, gorget;
     - bone: chest, legs, arms, gloves, helmet.
   - Kovar's "no magic items" check is commented out, so magic armour can be converted there.
   - **Weapon "Zbran drakobijce"**, needs `q040_dragons > 17`, i.e. after the gold dragon. Cost: 1 scale of each of the 7 colours including gold, and **200,000 gp in total** (50,000 + 150,000). Kovar does any weapon except staves; Dratenik does staves only.
   - Weapon effect (`f_dragon_armor_create_weapon`, aldaron.scp:4467-4483, magic-item tags):
     - bow: +20 Archery;
     - one-handed fencing/sword: +20 Fencing;
     - two-handed fencing/sword: +20 Swordsmanship;
     - mace: +20 Macefighting;
     - staff/crook: +20 Meditation and +5 Magery;
     - all of them: `imi_value_102=30`, **+30% damage vs dragons** ("of Dragonslayer", physical damage and spell power ×(100+bonus)/100, magicITEMS.scp:318-362).
     - Colour 09CE. Skill bonuses are applied as value×10 tenths, i.e. +20.0 skill points (magicITEMS.scp:69-78).
     - The quest dragons carry `monstertype dragon,50` ("50% item influence"), so against them the bonus is probably halved via `getmonstertypebonus`. I did not trace that function.
6. **Quest-only items.**
   - `i_boss_skull_ps` (converted from q039 items).
   - The 7 `i_dragon_scale_*`.
   - `i_zamerovatko_dragon_armor_kovar_acreate` / `..._dratenik_acreate` (target cursors).
   - Boss: Lord Strachu (existing Fortress of Fear boss).
7. **Systems.**
   - Quest dialog plus a target cursor.
   - Item conversion by graphic and type checks.
   - An equip/unequip event adding a character tag.
   - Dragon-breath damage reduced by `dragon_resist`.
   - Magic-item properties (skill bonus, slayer bonus 102).
   - Gold payment, which in our port must go through `GoldUtils.Pay`.
8. **Shape.** kill/fetch (boss skull) + crafting service, medium.
9. **Broken / odd.**
   - The weapon is unreachable because no gold scale exists. All 6 armour pieces are reachable.
   - The chain set's arms, gloves and gorget slots are leather items.
   - The weapon skill rule is by hand layer, not by weapon type: every one-handed sword gets Fencing.
   - The resist values exist only as world-save variables, not in scripts.
   - Kovar's "Nelzi mi!" line is missing a quote (harmless).
   - An older generic `dragon_armor_creator` / `i_zamerovatko_dragon_armor_acreate` (aldaron.scp:4153-4465) is dead code. It has no progress gates and calls the undefined `f_dragon_armor_skin_check`.
10. **Port: not ported.**
    - Converted: NPCDefs `c_q040_kovar` and `c_q040_dratenik` with SpeechStubs; the scale ItemDefs (`converted/systems/aldaron.def:623-665`); `c_strach_mage_chief2` and `i_boss_skull_ps`.
    - No `t_dragon_armor` or crafting code.
    - Building blocks already in the port:
      - `PotionEffectPlugin.DragonResist`, the tag `dragon_resist`, is already read by the ported dragon breath. An armour piece only has to add to that tag, coordinated with the potion's own tag arithmetic.
      - Magic property 102 → `_dragonbonus_` is mapped in `scripts/Basic/MagicItems/MagicItems.cs:38`, and the port reads `imi_value_N` tags (line 48).

---

## q041 Paserak - "The Smuggler" (switching realms) - files prestupak*

1. **Id / files.** Journal questMenu[51] "Paserak" (qtag `q041_paserak`, no. 51). scripts/questy/prestupak.scp (300), prestupak_chars.scp. Outside the list: the realm switch itself is `i_level_gate` and the sanctuary portal in **scripts/questy/zmodrani.scp:1-60, 235-260**; the Mordor branch's farmer is Borek of **mq002_borek.scp:36,123**.
2. **Giver.** `c_q041_prestupak_paserak` "Paserak" (heavily armed smuggler; e_npc_sniper, e_NPC_multishot). Not in the save -> GM-placed.
3. **Who.** Level >= 15, realm Gondor (1) or Mordor (2); outcasts refused (prestupak.scp:33-43). **Repeatable** (stage 10 can start again); each switch costs more (tag `presel` counts switches).
4. **Steps.**
   - Pay him 10,000 gp to talk; confirm you really want to defect.
   - Gondor player: kill an innocent, much-loved elf near Rivendell (`c_q041_prestupak_elf`; its @death advances the tag). Mordor player: bring farmer Borek near Nurn 40 cooked fish steaks + 5 refresh potions.
   - Pay for a new identity: 10 iron ingots, N x `i_mi_bag` (N = switches so far + 1, max 10), 1 roast pig, and a bank transfer of **level x 10,000 + 1,000,000 gp**.
   - Walk through the sanctuary portal and the realm gate: realm flipped, kills/karma reset (Mordor: kills 30000, karma -3000), home moved, journal stage 10.
5. **Rewards.** **Realm change** (a status change, special). No exp or gold; it is a gold sink (1.16M+ for a level-15, more later) that consumes magic bags.
6. **Quest items / monsters.** `c_q041_prestupak_elf` (victim). Consumes stock items. Gates `i_level_gate`, `i_port_prechod_2` (zmodrani.scp).
7. **Systems.** Realm system (home, karma, kills), bank payment (`canPay_from_bank`), cross-quest hook into Borek's dialog, GM-placed gates.
8. **Shape.** mixed (payment + kill/delivery by realm + payment), medium.
9. **Problems.** `i_level_gate` advertises "drops 10 levels" (more=10) but its step code never lowers a level. Gates are GM-placed. Wording/realm numbers assume realm 1/2.
10. **Port.** NPCs converted only (`converted/questy/prestupak*.def`). The port's one-pay rule (GoldUtils.Pay) would apply to the 10k and the bank transfer. No realm-switch path in the port.

---

## q042 Halda Molle Hatlend + the Halda Molle dungeon NPCs

The journal has one entry, questMenu[52] "Halda Molle Hatlend" (qtag `q042_hatlend_rulik`, no. 52). The rest of the Halda Molle files are a quest-like dungeon with two rescue NPCs, a cooking puzzle and a guide; they are catalogued here as **HM-dungeon** (no journal entry).

### q042 Hatlend - "Hatlend's rulik berries"

1. **Id / files.** scripts/questy/q042_halda_molle.scp (243), q042_halda_molle_chars.scp.
2. **Giver.** `c_hm_hatlend` "Hatlend", a soldier in light mithril armour (CATEGORY Dungy / SUBSECTION HM, so he stands in the Halda Molle dungeon). He is a real fighter: 6000-7000 HP, 50% crits for 25-28, e_summoneater. Not in the archive save -> GM-placed.
3. **Who.** Both realms, level >= 25 (q042_halda_molle.scp:121). Once. Saying "find someone else" gives a 24 h silence memory (`i_q042_hatlend_delay`) and stage 3 (can apologise later); refusing him a second time sets stage 4 and **he attacks you on sight from then on** (`e_hatlend` @NPCSeeNewPlayer, and on double-click).
4. **Steps.** Bring 150 rulik berries (`i_hm_prisada_4`, "kulicky ruliku"), which drop 1 at a time from the dungeon's warniaks (c_hm_warniak_1..5, halda_molle_char.scp:393,486,584,798). While on the quest he gives tactical advice pages about the dungeon (warniaks lay eggs - crush them, kill the wolf-pack leader first, minions: kill mages fast / ranged; pages on cajuns, the Nameless and "the root" are placeholders "Neco o ...").
5. **Rewards.** 2000 exp + 1x `i_mi_bag` (q042_halda_molle.scp:197-198) - **SPECIAL** random magic item, tier 0, 2 property rolls (see q035).
6. **Quest-only items.** `i_hm_prisada_4` (rulik berries, weight 18 each - 150 of them are heavy), `i_q042_hatlend_delay`.
7. **Systems.** Quest dialog, NPC that turns hostile by quest state, dungeon loot drop.
8. **Shape.** fetch (monster drops), small.
9. **Problems.** Three advice pages are placeholder text. Weight 18 per berry looks like a typo or a deliberate burden; can't tell.
10. **Port.** NPC/item converted only (`converted/questy/q042_halda_molle_chars.def`, dialogs commented out in `q042_halda_molle.def`). **`e_hatlend` is a quest-bound zero-effect stub** (teventStubs.scp:189). e_npc_critic and e_summoneater are real.

### HM-dungeon - Halda Molle rescue + cooking (no journal entry)

1. **Files.** scripts/questy/halda_molle.scp (556: Malica, Kaloc, guide, boss events), halda_molle_char.scp (1979: 3 NPCs + ~20 monsters), halda_molle_items.scp (469). Areas in scripts/spheremap.scp:9568-9604: "Halda Molle Level 1/2/3" (map 10) and "Halda Molle skrys" (map 11).
2. **NPCs.** `c_hm_pruvodce` "Pruvodce" (guide; leads the group, `e_hm_pruvodce`), `c_hm_malica` "Malica" (dying sorceress), `c_hm_kaloc` "Kaloc" (blacksmith locked behind bars, Malica's husband). Boss `c_hm_boss` "Bezejmenny" (the Nameless, "weakest demon", 30000-32000 HP, 1900-2000 exp, e_rozdeleni/e_frosty_wave/e_getwpn/e_npc_stun/e_summoneater/e_npc_critic). All GM-placed; positions/links are tags set by GMs (`tag(gate_position)`, `tag(bonus_position)`, `tag(hm_boss_spawn)`).
3. **Who.** Anyone who gets in; no level, realm or tag checks on Malica/Kaloc.
4. **Steps.**
   - Follow the guide through the levels (warniaks that keep spawning and lay eggs you must stomp - weight > 400 needed, Tera terathans, wolf packs with a leader, minions, cajuns, golem, rats).
   - Cooking puzzle for Malica: gather 1 Marali root piece (`i_hm_prisada_1`, from guard c_hm_guard "Marali koren"), 1 licorice bark (`i_hm_prisada_2`, from a special tree `i_hm_lekorice` that wakes guardian spawns; bark only when they are dead; 30 min reset), 1 lichen (`i_hm_prisada_3`, from c_hm_golem), 20 rulik berries, 5 strange turnips (`i_hm_prisada_5`, harvested from `i_hm_prisada_5_crop`, 60%/80% poison chance on picking; **eating one kills you**). Light the fire (`i_hm_drevonaohen`), put everything in the cauldron (`i_hm_kotlik`, Cooking >= 50.0 to add), cook (success chance from Cooking; +0.1 Cooking per try).
   - Give the food to Malica: she opens a `i_vargate` to the bonus area for a GM-set time ("I can't hold it long").
   - Kill the Nameless; then caged Kaloc lets you out with a reward and teleports you to 6096,2555,17 map 10 (halda_molle.scp:184).
5. **Rewards.** Kaloc: 1x `i_mi_bag` (halda_molle.scp:177) - **SPECIAL** (tier 0, 2 rolls), at most once per 12 h per character (`qtag hm_last_reward` = serv.time + 432000 tenths of a second, :179); repeatable as long as the boss spawn is empty. Malica's gate leads to a GM-built "bonus" area whose contents are not in the archive. No exp/gold from the NPCs.
6. **Quest items / monsters.** Ingredients above, `i_hm_jidlo_bonus` (the food), `i_hm_kotlik`, fire items, `i_hm_warniak_egg`, `i_hm_bow_malica` (only makes Malica bow and shout while the gate is open), `i_hm_npc_remove`/`i_hm_spawn_remove` (1 h cleanup of summoned monsters), `i_hm_die`. ~20 HM-only monsters (c_hm_terathan_*, c_hm_warniak_1..5, c_hm_guard, c_hm_prisluhovac(+_mage/_ranger), c_hm_boss, c_hm_vudce_smecky, 3 wolves, c_hm_krysa, c_hm_cajun(+_child), c_hm_golem).
7. **Systems.** Guide NPC (escort-like), spawn-on-use tree, crop regrowth, cauldron crafting with skill check, timed var-gate, caged-NPC reward with cooldown, multi-level dungeon areas.
8. **Shape.** exploration/boss dungeon with a cooking puzzle; large.
9. **Problems.** `hm_dokoncit_jidlo` returns nothing for Cooking >= 90.0 (halda_molle_items.scp:344-349), so a master cook probably always fails. `e_leader_ability` (pack leader boost, halda_molle.scp:424) is attached to no chardef in the archive. Many `scylla.delayedmessage` GM logs.
10. **Port.** Combat is ported: `Basic/AbilitiesAndTalents/MonsterSpecialAttacks/HaldaMolle.cs` (e_rozdeleni, e_frosty_wave, e_drop_egg, e_warniak_degrade, e_warniak_drop). Areas converted (`converted/map.def:10910+`). NPCs/items converted only (`converted/questy/halda_molle*.def`, cauldron/dialog functions commented out). **Quest-bound zero-effect stubs: `e_hm_pruvodce` (:208), `e_kaloc_help` (:233), `e_malica_help` (:258)**; `e_leader_ability` was commented out by the converter and has no stub. No boss is placed (GM work, #1022).

---

## q043 Murdocovi ucni family (tutorial + Gondor/Mordor newbie chains)

Source copies read: `gc/q043_murdocovi_ucni.scp` (4275 lines), `gc/q043_tut.scp`, `gc/q043_tut_chars.scp`, `gc/q043_chars.scp`, journal `gc/questmenu.scp` (index line 83, journal 3026-3420). Tutorial hand-off in the original archive `scripts/questy/start_dung.scp` (cp1250). Line numbers below are in the gc/ copies unless a path is given.

## Shared structure of q043_murdocovi_ucni.scp (summary, not transcription)

| lines | content |
|---|---|
| 1-170 | header: item/NPC/dialog lists, full stage table for mq043 (54-109) and gq043 (114-170) |
| 173-182 | `f_q043_checkRealm(realm)`: 1 if `src.tag(realm)` == argument and the player has no tag of the other realm's chain (Gondor=1 blocks if mq043 set, Mordor=2 blocks if gq043 set) |
| 190-1305 | Mordor dialogs + button handlers: Dran (190/404), Bran (492/676), Wran (718/892), Tran (932/1095), Oran (1118/1279) |
| 1306-2422 | Gondor mirrors: Anton (1306/1522), Alena (1609/1793), Helena (1835/2009), Magdalena (2049/2212), Lenka (2235/2396) |
| 2423-3180 | shared side NPC dialogs (one dialog, serves both realms by whichever tag is set): hospodsky 2423/2506, ozrala 2546/2645, hrobar 2678/2718, zlodejicek (Bart) 2753/2799, namornik 2852/2901, kronikar (Torwin) 2954/3112 |
| 3181-3555 | speech blocks (`*ahoj*`, `*murdoc*`, `*ukol*`, `*quest*`, `*hi*`...) and `f_*_DClick` openers: dead -> nothing; wrong realm (`region.tag(realm)` of the NPC) -> "Pomahas druhe strane!"; Dran/Anton also refuse `class_craft` |
| 3556-4275 | 18 quest statics (ITEMDEF, all `attr_move_never`, color 084d). Double-click within 1 tile advances the tag by one; nothing is put in the backpack. Each checks `f_q043_checkRealm(<tag.q043_realm>)`, so every placed copy needs a GM-set `TAG.q043_realm` 1 or 2 |

Dialog framework: `questDialog_create` (face gump), `questDialog_baseButt` (Kdo jsi / Mohu pomoci / Informace / Hledam praci / Nesu ti / Prohlednout), `questDialog_addResponse`, pages keyed by name; every handler re-checks the exact stage value ("Zkousis me osalit?!" otherwise). Progress tags via `src.qtag(name,value,53)` stored on the `i_quest_rune` item (gc/qTag.scp:1-27); the third argument 53 is the questMenu index and reports progress to the journal.

## q043_tut - Murdocovi ucni: tutorial (Argol)

1. **Id / name / files**: tag `q043_tut`; "Murdocovi ucni" = "Murdoc's apprentices" (tutorial part, Argol's torn parchment). Files `q043_tut.scp` (400 lines), `q043_tut_chars.scp` (33 lines); journal `questmenu.scp:3042-3061`.
2. **Giver**: `c_q043_tut_argol` "Argol", title "Murdocuv ucen", necromancer look (robe, wizard hat, necro spellbook), STR/DEX/INT 1000, `brain_human`, `noanimate`. Lives in the tutorial (region `tutorial`, home 5412,2555,13; start_dung.scp `f_port_tutorial`). Not in the archive's save files (`save/sphereworld.scp`, `spherechars.scp` have no q043 objects): GM-placed, no stored spot. Statics: `i_q043_tut_pergamenA/B/C` (graphic 0x0E34, colors 029/0ea/0ed), also GM-placed.
3. **Who**: anyone in the tutorial; no realm/level/profession check (only "not dead"). Once (stages only advance). The tag is wiped when the player leaves the tutorial: `startfinal` -> `nbOffTtQTags_remove` deletes the whole `i_quest_rune` (start_dung.scp:154-159, 2027).
4. **Steps** (q043_tut.scp:21-28, 223-258):
   - 0->1 accept from Argol ("parchment near the tower on the north beach / NW tip").
   - 1->2 double-click `i_q043_tut_pergamenA` (top part); 2->3 hand in, sent south over the bridge to the Ettin.
   - 3->4 `i_q043_tut_pergamenB` (middle part, "held by the giant Ettin" - only text, the static just lies there); 4->5 hand in, sent to the undead crypt in the sand pass.
   - 5->6 `i_q043_tut_pergamenC` (bottom part); 6->7 hand in, Argol "translates" it: a link to wiki.uoerebor.cz "Murdocuv denik".
   - Info pages point the player to Murdoc's apprentices: "Cecil" at the Edoras gate and Dran at the Barad-Dur bank.
5. **Rewards**: exp 100 (q043_tut.scp:229), 100 (:238), 400 (:247) = 600 exp. No gold, no items.
6. **Quest-only items**: the 3 parchment statics (pickup is virtual, tag only). No quest monsters (Ettin and undead are ordinary tutorial spawns, not defined here).
7. **Engine systems**: quest NPC dialog framework, speech trigger, quest tags + journal, GM-placed clickable statics, tutorial region and its exit portal.
8. **Shape**: tutorial / fetch, small.
9. **Broken / odd**: Argol names the Gondor apprentice "Cecil in Edoras" (q043_tut.scp:151-163) but the Gondor chain's giver is Anton in Hobitin (journal line 3416 even says "Argol told you about Anton in Hobitin"). Argol does not set the chain tag himself: leaving the tutorial (`i_port_start2` @Step, start_dung.scp:1989-1993) sets `gq043=1` or `mq043=1` by realm - this needs level >= 3 (`def_tutorial_min_level`). Journal `d_murdoc_ucni` shows the tutorial branch whenever `q043_tut` > 0; works only because the rune is wiped on exit.
10. **Port status**: not ported. `scripts/converted/questy/q043_tut_chars.def` (NPCDef c_q043_tut_argol) and `scripts/converted/questy/q043_tut.def` (3 ItemDefs, triggers dropped) exist; dialog/functions commented out by the converter. Our port has its own new-player flow with no tutorial quests: `scripts/Basic/Races/RaceSelection.cs` (i_stone_raceclass -> profession/race/nation/stats -> `player.P(player.Home)`, newbie gear), nothing equivalent to Erebor's tutorial island quests (nb001-nb007 are converted only as SpeechStubs in `scripts/converted/questy/start_dung.def`). The `Tutorial` region exists only as converted map data (`converted/map.def:4410`) and is referenced by `Basic/Moderation/Camp.cs` (jail strip). No overlap with q043. Events: `e_character` -> zero-effect stub `a_tevent_e_character` (`Basic/AbilitiesAndTalents/teventStubs.scp:70`).

## mq043 - Murdocovi ucni, Mordor chain (Dran and brothers)

1. **Id / name / files**: tag `mq043` (questMenu[53] "Murdocovi Ucni", dialog `d_murdoc_ucni`, gc/questmenu.scp:83). "Murdoc's apprentices": collect pages of the lost necromancer Murdoc's diary from his five apprentice brothers. Files `q043_murdocovi_ucni.scp`, `q043_chars.scp`; journal questmenu.scp:3066-3245.
2. **NPCs** (all `c_man`/`c_woman`, `brain_human`, STR/DEX/INT 1000, `TEVENTS=e_character`, q043_chars.scp). Header says 15 NPCs and 13 statics; actual defs are 16 NPCs (+ Kronikar) and 18 statics (+ runa1-5 for Torwin). Header defnames partly wrong (`c_mq043_vran` is really `c_mq043_wran`, `c_q043_potulny_hrobar` is `c_q043_hrobar`, `c_q043_zlodejicek_bart` is `c_q043_zlodejicek`, `c_q043_vesely_namornik` is `c_q043_namornik`, `i_q043_necromantova_kniha` is `i_q043_kniha`, `i_q043_necromantuv_brk` is `i_q043_brk`).
   - Mordor: `c_mq043_dran` Dran "Murdocuv ucen" (Barad-Dur, by the bank), `c_mq043_bran` Bran (Nurn), `c_mq043_wran` Wran (Rhun), `c_mq043_tran` Tran (Harad), `c_mq043_oran` Oran (Umbar), all titled "Bratr Drana" except Dran.
   - Shared side NPCs (one def, one copy per realm town): `c_q043_hospodsky` Hospodsky "Jitrnicka" (innkeeper, town B), `c_q043_ozrala` Ozrala "Na hadry" (drunk, same inn), `c_q043_hrobar` Hrobar (wandering gravedigger, town C), `c_q043_zlodejicek` "Zlodejicek Bart" "Grazl" (thief, town D), `c_q043_namornik` "Vesely Namornik" "Prvni dustojnik" (sailor, port E), `c_q043_Kronikar` "Kronikar Torwin" (Umbar).
   - None placed in the archive saves: all GM-placed, no stored spots. Cave coordinates come only from the journal (see steps).
   - Statics (13 for the chain + 5 for Torwin): `i_q043_kniha` (book), `i_q043_brk` (quill), `i_q043_runaA/B/C` (runes), `i_q043_pergamen` (parchment), `i_q043_sud` (beer barrel), `i_q043_hul` (staff), `i_q043_sperhaky` (lockpicks), `i_q043_boty` (boots), `i_q043_heslo` (password note), `i_q043_dalekohled` (spyglass), `i_q043_denik` (ship's log), `i_q043_runa1-5`.
3. **Who**: Mordor realm only (`tag.realm`=2) and never having started gq043; Dran refuses `class_craft` ("crafters will be needed later", 3199, 408). No level, rank or skill gate to start. Players leaving the tutorial get `mq043=1` (Dran greets them as Argol's), but `mq043=0` can also start. Once, no cooldown. The Torwin tail needs `tag.skillrank >= 2` (Master).
4. **Story / steps** (stage table 54-109, handlers cited):
   - **Dran, Barad-Dur** (1-7): prove yourself - book in the cave X 3504 Y 2747 (`i_q043_kniha` 2->3), hand in (3->4); quill X 3661 Y 2838 (4->5, 5->6); copy the faded rune X 3528 Y 2568 (`i_q043_runaA` 6->7), hand in (7->50), sent to Bran.
   - **Bran, Nurn** (50-58): 50->51 sent to the innkeeper; innkeeper 51->52 "get rid of the drunk"; **Ozrala**: talk-puzzle with 2 winning dialog paths (talk him into going home, or "buy" him 2 shots and a beer - free, text only), 52->53; innkeeper 53->54 gives part of the parchment; Bran 54->55: other part in a cave (X 3478 Y 3126 / 3345 3151 / 3383 3228), `i_q043_pergamen` 55->56; Bran 56->57: copy rune `i_q043_runaB` 57->58; Bran 58->100.
   - **Wran, Rhun** (100-108): 100->101 hire a new gravedigger; **Hrobar** 101->102 wants the beer barrel stolen by trolls (caves X 3921 Y 2215 / 3702 2093 / 3837 2237), `i_q043_sud` 102->103, Hrobar 103->104; Wran 104->105 (llama reward), fetch his staff `i_q043_hul` 105->106 (header says "clean the graveyard, staff behind a locked door" - no cleaning mechanic exists, just the static); Wran 106->107, copy rune `i_q043_runaC` 107->108; Wran 108->150.
   - **Tran, Harad** (150-158): parchment stolen by **Bart**; Bart 151->152 wants his lockpicks (`i_q043_sperhaky` 152->153; caves X 4175 Y 4074 / 4198 3973 / 4133 3886), 153->154 boots in a spider cave (`i_q043_boty` 154->155), 155->156 poker club password "under the bridge" (`i_q043_heslo` 156->157), 157->158 throws the parchment at you; Tran 158->200.
   - **Oran, Umbar** (200-207): sailor has it; **Namornik** 201->202 bring 20 raw fish steaks (consumed), 202->203; spyglass from the mutineer in a cave (`i_q043_dalekohled` 203->204; caves X 2578 Y 4068 / 2649 3982 / 2839 3753), 204->205; ship's log (`i_q043_denik` 205->206), 206->207; Oran 207->222.
   - **Dran finale** 222->223: hands over all pages; Dran "translates" (wiki link page). From 223 Dran offers two follow-ups: Torwin (see below) and Ember = q044 "Zachrante Murdoca" (sets `q044=1` if q044 <= 10, 459-468; text says lvl 20+, no check).
5. **Rewards** (all in `q043_murdocovi_ucni.scp`):
   | stage | from | reward | line |
   |---|---|---|---|
   | 3->4 | Dran | 100 exp | 421 |
   | 5->6 | Dran | 100 exp | 431 |
   | 7->50 | Dran | 300 exp + 1000 gold | 441-442 |
   | 50->51 | Bran | 50 exp | 682 |
   | 51->52, 53->54 | Hospodsky | 100 + 100 exp | 2521, 2529 |
   | 54->55, 56->57 | Bran | 100 + 100 exp | 691, 700 |
   | 58->100 | Bran | 200 exp + 2000 gold | 709-710 |
   | 100->101 | Wran | 50 exp | 898 |
   | 101->102, 103->104 | Hrobar | 50 + 50 exp | 2735, 2744 |
   | 104->105 | Wran | 50 exp + **1x `i_pet_llama`** | 906-907 |
   | 106->107 | Wran | 100 exp | 915 |
   | 108->150 | Wran | 300 exp + 3000 gold | 923-924 |
   | 150->151 | Tran | 50 exp | 1101 |
   | 153->154, 155->156, 157->158 | Bart | 100 + 100 + 100 exp (151->152: 0) | 2825, 2834, 2842 |
   | 158->200 | Tran | 150 exp + 4000 gold | 1109-1110 |
   | 200->201 | Oran | 50 exp | 1285 |
   | 202->203 | Namornik | 50 exp, costs 20x `i_fish_cut_raw` | 2925-2927 |
   | 204->205, 206->207 | Namornik | 150 + 150 exp | 2936, 2944 |
   | 207->222 | Oran | 250 exp + 5000 gold | 1293-1294 |
   | 222->223 | Dran | 500 exp + **1x `i_mi_bag`** | 452-453 |
   Totals: **3400 exp, 15000 gold, 1 llama figurine, 1 magic-item bag.** Ozrala gives nothing.
   - `i_mi_bag` - **SPECIAL/MAGICAL (random)**: "Pytlik s magickym predmetem" (archive `scripts/systems/magicITEMS.scp:882`), bag color 0430, `type=mit_main`, more1=0 (lowest "green" tier), more2=2. Double-click (`f_mif_create`, magicITEMS.scp:961) creates one random item from `mif_chooseitem(0)` with 1 guaranteed random magic property (`mif_randtype`) and a second one with 5/6 chance, unidentified (`tag.unidentified=1`, named "Magicky predmet", must be identified at a vendor). Exact item/property depend on the magic-item tables, not fixed.
   - `i_pet_llama` - **pet figurine** (archive `sphereitemb7.scp:1894`, `T_FIGURINE`, TDATA3=c_llama, value 300-380): double-click gives a tame llama. Plain stock item (vendors sell it, `temp_vend`), so a pet but not unique.
6. **Quest-only items / monsters**: the 13 statics above (virtual pickup: no item enters the pack, only the tag moves). No quest monsters: trolls, spiders and the mutineer's corpse are only in the text (no spawns or corpse defined).
7. **Engine systems**: quest NPC dialog framework with stage-gated pages + speech keywords; quest tags on a quest rune + journal with per-stage text and cave coordinates ("marked on your map" is emote only, no map pins); realm tags on regions (`region.tag(realm)`) and on placed statics (`TAG.q043_realm`); exp (`addexp`), gold, item give, `consume` of a stack; GM placement of 16 NPCs (x2 copies for shared ones) and statics (one copy per realm) in ~15 caves.
8. **Shape**: mixed chain (fetch + delivery + talk, one talk-puzzle, one gather-and-deliver), large.
9. **Broken / unfinished**:
   - Ozrala stage marked `<<<DODELAT` (to do) in the header (64, 124), yet the dialog works.
   - Header promises Bart's "pay 100g or threat" choice and Tran "returning the 100g" (87, 89); not implemented - Bart just gives it (2839-2843), no gold changes hands.
   - Header says 10 fish steaks; code takes 20 (2924-2925).
   - Gondor header stage numbers for Helena (104-107) differ from the code, which uses the Mordor numbers (104->105->106->107->108->150) for both.
   - Header item/NPC defnames wrong (see 2).
   - Every static needs a GM-set `TAG.q043_realm` (nothing in the archive sets it); unset -> `f_q043_checkRealm("")` fails and the item silently does nothing.
   - Journal `f_isDone_d_murdoc_ucni` is true only at exactly 223 (questmenu.scp:3030-3031), so once the player takes the Torwin tail (250+) the quest no longer shows as done.
   - Dran's text says "Torwin [lvl 6-12]" (200) but the gate is `skillrank >= 2` (474); Ember "lvl 20+" has no check.
   - Dran/Anton's `Pozdrav` link "qmur_web" is a wiki link, not in-game text.
   - None of the NPCs or statics are in the archive saves.
10. **Port status**: items/NPCs converted only (defs exist, no quest logic). `scripts/converted/questy/q043_chars.def` (16 NPCDefs incl. Kronikar), `scripts/converted/questy/q043_murdocovi_ucni.def` (18 ItemDefs without triggers; `i_q043_sud` became an `EquippableDef` with `layer = 0` and the copy-pasted description "Nekromantova hul" - converter/source quirk), all dialogs and DClick functions commented out by the converter; speech blocks are `[SpeechStub spk_mq043_*]` (the #825 GM-hint stubs). Journal entry `d_murdoc_ucni` commented out in `converted/questy/System/questmenu.def:533-`. No C# references to mq043/gq043/q043 tags. Reusable pieces in the port: `Basic/Quests/Common/QuestDialog.cs` (QuestNpc), `QuestItems.cs` (quest items as tags), `QuestJournal.cs`; `i_mi_bag` is real (`Basic/MagicItems/MagicBags.def`, MagicBag class, used by weekly/grandmaster quests); `i_pet_llama` exists (converted/itemb7.def:1448, figurines work - `Basic/BasicTypes/type_character_icon.cs`); realms exist (`Basic/Races/RealmPlugin.cs`). Events: all 16 NPCs use only `e_character` -> zero-effect stub `a_tevent_e_character` in `Basic/AbilitiesAndTalents/teventStubs.scp:70`. Link: q044 "Zachrante Murdoca" (Ember) starts from the finale here.

## gq043 - Murdocovi ucni, Gondor chain (Anton and sisters)

Mirror of mq043 with the same stages, statics, side NPCs and rewards; only the apprentices and towns differ. Treat as the same quest content with a realm switch.

1. **Id / name / files**: tag `gq043`; same files and journal (questmenu.scp:3218-3420, `f_questmenu_murdoc_gondor`).
2. **NPCs**: `c_gq043_Anton` Anton "Murdocuv ucen" (Hobitin), `c_gq043_Alena` (Lorien), `c_gq043_Helena` (Edoras), `c_gq043_Magdalena` (Minas Tirith), `c_gq043_Lenka` (port Dol Amroth), all `c_woman` titled "Sestra Antona" except Anton (q043_chars.scp:363-523). Shared side NPCs and Torwin (Dol Amroth copy) as in mq043. GM-placed, no spots in saves.
3. **Who**: Gondor realm (`tag.realm`=1), never started mq043, not `class_craft` (1525, 3317). Once. Torwin tail needs `skillrank >= 2`.
4. **Steps**: same as mq043. Journal cave coordinates: Anton book X 856 Y 1471, quill X 990 Y 1114, rune X 1130 Y 1158; Alena caves X 2290 Y 1834 / 2381 1788 / 2551 1726; Helena (graveyard in Edoras) X 2080 Y 2491 / 2219 2350 / 2359 2650; Magdalena/Bart X 5582 Y 4028 / 5763 3950 / 5733 3908; Lenka/sailor X 2467 Y 3027 / 2351 3142 / 2343 3298. Finale 222->223 with Anton, then Torwin in Dol Amroth or Ember (q044, 1590-1599).
5. **Rewards** (identical amounts): Anton 100 exp (1539), 100 (1549), 300 + 1000 gold (1559-1560), finale 500 exp + 1x `i_mi_bag` (1570-1571); Alena 50 (1799), 100 (1808), 100 (1817), 200 + 2000 gold (1826-1827); Helena 50 (2015), 50 + 1x `i_pet_llama` (2023-2024), 100 (2032), 300 + 3000 gold (2040-2041); Magdalena 50 (2218), 150 + 4000 gold (2226-2227); Lenka 50 (2402), 250 + 5000 gold (2410-2411); shared side NPCs as in mq043. Totals 3400 exp, 15000 gold, `i_mi_bag` (special: random magic item), `i_pet_llama` (pet figurine, stock).
6. **Quest-only items / monsters**: same statics, a second copy of each with `TAG.q043_realm=1`. No monsters.
7. **Engine systems**: same as mq043.
8. **Shape**: mixed chain, large (mirror).
9. **Broken / odd**: copy-paste text from the Mordor version: Alena and Helena's "where are your siblings" page lists "Anton in Barad-Dur, Alena in Nurn..." (1730, 1946) next to the correct Hobitin/Lorien/Edoras version; Lenka's greeting says "bring the pages back to Anton in Barad-Dur" (2258) while her hand-in says Hobitin (2408); Anton says "Dol Amrod" once (1587). Argol's tutorial names "Cecil in Edoras" instead of Anton. Plus all mq043 items in point 9.
10. **Port status**: items/NPCs converted only (same files as mq043; `c_gq043_*` NPCDefs in `converted/questy/q043_chars.def:260-371`). Events `e_character` stub only.

## Torwin bonus - stages 250-261 of mq043/gq043 (Kronikar Torwin)

1. **Id / name / files**: not a separate tag - continues `mq043`/`gq043` from 250 to 261. "Kronikar Torwin" = Torwin the chronicler; copy five faded runes. Dialog `d_q043_kronikar` (2954-3180), statics `i_q043_runa1-5` (4078-4275), journal `f_questy_murdoc_ucni_m_kronikar` (questmenu.scp:3093-3120) and `_g_kronikar`.
2. **Giver**: `c_q043_Kronikar` "Kronikar Torwin", title "Kronikar Murdocova Bratrstva" (q043_chars.scp:526-556). One def, two placed copies: Umbar port (Mordor) and Dol Amroth (Gondor); realm by the NPC's region. Sent there by Dran/Anton from stage 223. GM-placed, no spot.
3. **Who**: finished the chain (exactly 223) and `tag.skillrank >= 2` (Master rank, 474 / 1578). Once ("come back later, maybe more parchments" at 261 - no second set exists).
4. **Steps**: Dran/Anton 223->250; Torwin 250->251 gives first parchment; then for each rune: double-click the rune static in a cave (251->252, 253->254, 255->256, 257->258, 259->260) and return to Torwin. Mordor caves: Umbar X 3112 Y 3328, Harad X 4035 Y 3810, Nurn X 3445 Y 3533, Rhun X 3671 Y 2210, Barad-Dur X 3663 Y 2554. Gondor: Dol Amroth X 2560 Y 3081, Hobitin X 1253 Y 1099, Lorien X 2368 Y 1635, Minas Tirith X 2827 Y 2829, Edoras X 2492 Y 2672. Torwin warns "it is dangerous there" (high-level caves, not checked).
5. **Rewards**: Dran/Anton 100 exp (478 / 1582); Torwin 75 exp (3127), 250 x4 (3136, 3145, 3154, 3163), 500 (3172). Total 1675 exp. No gold, no items - nothing special.
6. **Quest-only items**: `i_q043_runa1`-`runa5` statics (graphics 0x1824-0x1828). No monsters.
7. **Engine systems**: as mq043, plus a skill-rank gate.
8. **Shape**: exploration/fetch, medium (travel to 5 towns' caves).
9. **Broken / odd**: journal stops showing the quest as done (isDone == 223 only); Dran's "lvl 6-12" text vs the Master-rank gate; the 261 "come back later" promises content that does not exist.
10. **Port status**: NPC and 5 rune ItemDefs converted only; no logic. Our port's Master rank quest (`Basic/Quests/Master`) and `SkillRank` would supply the gate.

---

## q044 Zachrante Murdoca - "Save Murdoc"

1. **Id / files.** Journal questMenu[54] "Zachrante Murdoca" (qtag `q044`, no. 54). scripts/questy/q044_zachrante_murdoca.scp (458), q044_chars.scp. Journal texts: questmenu.scp:3440-3540.
2. **Giver.** `c_q044_ember` "Ember", title Informator (obsidian chainmail, obsidian kryss), "on the border between Mordor and Gondor where the Rhun desert ends" (journal). Not in the save -> GM-placed.
3. **Who.** Both realms. **Crafters refused** (class_craft, :272). No level check. The q043 finale hands over to Ember (sets q044=1 per the q043 report), but Ember accepts anyone with q044 < 10. Episodic: the journal shows "quest continues in the coming days" until a global `var(q044_phase)` is raised by staff (questmenu.scp:3473-3526); Ember's own dialog does not check the phase. Once.
4. **Steps.** Five identical rounds, one per dungeon: Beridor, Snagum, Doupe Kyklopu (Cyclops den), Moina Filya, Ledova jeskyne (Ice Cave):
   - find Murdoc's invisible chest (`i_q044_truhla`, static whose `tag.q044_dung` = 1-5), feel three dials;
   - find the scrap of parchment nearby (`i_q044_kombinace`) which shows the code (666, 526, 625, 656, 626);
   - back at the chest press "enter code" (no real input), take the note fragment, return to Ember.
   - Murdoc's diary itself lived on the wiki (link in the dialog).
5. **Rewards** (q044_zachrante_murdoca.scp): accept 100 exp (:200); A: 2000 exp, 1x `i_mi_pouch`, 10000 gp (:208-210); B: 3000 exp, 1x `i_mi_bag`, 20000 gp (:219-221); C: 5000 exp, 1x `i_mi_pouch` + 1x `i_mi_bag`, 30000 gp (:230-233); D: 6000 exp, 1x `i_mi_pouch` + 1x `i_mi_pouch_high`, 40000 gp (:242-245); E: 8000 exp, 1x `i_mi_pouch_superhigh`, 50000 gp (:254-256). Total 24100 exp, 150000 gp, 6 magic bags. **All bags SPECIAL** (random magic item): pouch = 1 property roll, bag = 2 rolls; plain/high/superhigh = metal tiers 0/1/2 (superhigh: valorite 200, obsidian 80, adamantium 30, mithril 1). All six exist in our port (`Basic/MagicItems/MagicBags.def`).
6. **Quest items / monsters.** `i_q044_truhla` (5 statics), `i_q044_kombinace` (5 statics), each needing GM-set `q044_dung`. `c_q044_demon_zved` "Demon Zved" (a 1000-1250 HP mongbat "demon scout") is defined but nothing spawns or references it.
7. **Systems.** Quest dialog, hidden statics with per-dungeon tags, staff-released episodes (global var), journal.
8. **Shape.** exploration (5 dungeon hunts), medium.
9. **Problems.** Story stops on a cliffhanger ("come back soon, I'll know more") - no sequel in the archive. The code is shown, never typed: no puzzle. Demon Zved unused. Statics and their tags are GM-placed, no spots.
10. **Port.** Items/NPCs converted only (`converted/questy/q044_chars.def`, `q044_zachrante_murdoca.def` with dialogs commented out, speech stub). Rewards' bags exist. e_character stub.

---

## q075 Valecnik - "Warrior Arkan" (a mage's guts)

1. **Id / files.** scripts/questy/q075_valecnik.scp (206). Not in the journal list (it reports to quest no. 9, which is the journal's "Mastickar Gregoriel" slot).
2. **Giver.** Dialog title "Valecnik Arkan" (old warrior, also a vendor: "Nakoupit" opens `buy`). **No chardef in the archive**: the speech `spk_q075_valecnik` is not attached to any NPC anywhere, so a GM would have had to give some NPC TSPEECH=spk_q075_valecnik.
3. **Who.** Both realms, no level check. Once. An unused `f_q075_checkOldTags` migrates an older tag version (`tag.q075_valecnik`, `splnen_kvest_075`).
4. **Steps.** Accept a bag (`i_q075_valecnik_kolektor`), kill "the traitor Cephalos" (a mage) and tear out his guts (t_collector target "Body of Cephalos"), return.
5. **Rewards.** 15000 exp + 15000 gp (q075_valecnik.scp:197-198). No item. Special: no (the "potion of superhuman strength and eternal youth" is only talk).
6. **Quest items / monsters.** `i_q075_valecnik_kolektor`. Monster "Cephalos": **no chardef with that name anywhere in the archive**.
7. **Systems.** Quest dialog, t_collector, vendor.
8. **Shape.** kill, small.
9. **Problems.** Not startable as shipped: no giver NPC, no Cephalos. Wrong journal number (9). Leftover `elseif q010_mastickar` branches copied from q010. Big reward for a one-kill quest.
10. **Port.** Only `converted/questy/q075_valecnik.def` (collector item def; dialogs commented out). Not ported.

---

## q099 Vyvrhel - "Outcast" (buying back your realm)

1. **Id / files.** Journal questMenu[30] "Vyvrhel" (qtag `q099`, no. 30). scripts/questy/q099_vyvrhel.scp (199), q099_vyvrhel_items.scp (NPC + 4 bags). Uses `function vyvrhel` in scripts/funkce.scp:615.
2. **Giver.** `c_q099_vyvrhel` "Podivin" (odd man with a "Book of Darkness"). Not in the save -> GM-placed.
3. **Who.** Only **outcasts** (negative `tag.realm`; on Erebor a Gondor player with 20+ kills is outcast automatically, funkce.scp ~600) **who also have `tag.vykoupeni`** ("redemption"). Once (tags cleared at the end).
4. **Steps.** Custom gump (not the quest-dialog framework). Collect with bags: 100 grizzly "thighs" (Body of Grizzly), 80 earth-elemental essences, 50 eye essences (Body of Eye), 5 feathers of the poison phoenix (Body of Phoenix). Then have Mining >= 40.0, pay a bank cheque of exactly 250,000 gp and lose 15 levels.
5. **Rewards.** Not an item: **realm restored** (`src.vyvrhel` flips the sign of tag.realm: back to Gondor with kills 0, or back to Mordor with kills 30000 / karma -3000; restores the original name, removing the "Vyvrhel" prefix) (q099_vyvrhel.scp:185). Costs 250k + 15 levels (:187). Special: yes, a status change (un-outcasting). No exp/gold.
6. **Quest items.** `i_q099_vyvrhel_1..4` collector bags.
7. **Systems.** Realm/outcast system, custom gump, t_collector, cheque check, level loss.
8. **Shape.** fetch (kill-collect) + payment; medium.
9. **Problems.** **`tag.vykoupeni` is never set anywhere in the archive**, so only a GM could open the quest. **`srazitlevely` (the 15-level loss) is defined nowhere in the archive.** The cheque must be exactly 250000 (more is "too little"). The Mining 40 gate is flavour ("be strong enough to survive the transformation").
10. **Port.** NPC/items converted only (`converted/questy/q099_vyvrhel*.def`). Our engine has outcast realms (`Basic/Enums.cs` Realm.GondorOutcast/MordorOutcast, used by Healer.cs, TravelStone.cs) but no redemption path.

---

## q100 Hobitin - "Hobbiton" (gardener Ambra's manure)

1. **Id / files.** Journal questMenu[15] "Hobitin" (plain tags `q100_hobitin_ambra`, `q100_hobitin_ambra_neuspel`, no. 15). scripts/questy/q100_hobitin.scp (181), q100_hobitin_char.scp. Authors "Orfeus (Moria - www.moria.cz)", updated 2013 by Nara.
2. **Giver.** `c_q100_hobitin_ambra` "Ambra", Zahradnice (gardener) at her turnip field, Hobbiton (by the quest name). Not in the save -> GM-placed.
3. **Who.** Anyone (no level/realm check). Once; quitting ("find someone else") blocks the quest for good (`q100_hobitin_ambra_neuspel`).
4. **Steps.** Take an empty sack, double-click a manure pile (`i_q100_hobitin_ambra_hnuj`, GM-placed static) to fill it, bring it back.
5. **Rewards.** 30 exp + 1 onion `i_fruit_onion` (q100_hobitin.scp:111-112). Plain.
6. **Quest items.** `i_q100_hobitin_ambra_pytel`, `_pytel_2`, `_hnuj`.
7. **Systems.** Quest dialog, static use.
8. **Shape.** fetch, small (newbie).
9. **Problems.** None beyond the manure static being GM-placed.
10. **Port.** NPC/items converted only (`converted/questy/q100_hobitin*.def`). Hobitin town exists in the port (Setup/TownSetup.cs), Ambra is not placed.

---

## Darkmoon - "Mother of all Mothers" beetle event (no journal entry)

1. **Files.** scripts/questy/darkmoon.scp (379), darkmoon_chars.scp (697). Linked to q035 Herman's beetle story.
2. **NPCs.** `c_darkmoon_doktor` "Lennor" (warrior-scientist muttering while he works, speech `spk_q039_first_quest`), messengers `c_darkmoon_poskok` "Kester" and `c_darkmoon_poskok_m` "Gilthol" (mounted, title Posel). Boss `c_darkmoon` "Darkmoon" (30000-32000 HP, 1550-2000 exp; shapeshifts through 6 forms f_c_fireant_worker ... f_c_darkmoon). All GM-placed.
3. **Who.** Anyone.
4. **Steps (as built).** Messengers tell that beetles act as if commanded by "Darkmoon, Mother of all Mothers" and will attack the towns "on the fifth day at dusk", counting down `29 - getDay` days, and ask you to spread the word. Lennor only reacts to "Herman sent me" if `qtag q039_darkmoon == 1`. GMs use `spawn_darkmoon_bug <level>` to create beetle holes (black floor, boulders, a poison tile, spawner releasing 4/4/3/3/2/1 x level beetles: c_bug ... c_bug_elite).
5. **Rewards.** None scripted. Boss loot: gold + 45-55 random reagents. Special: no.
6. **Items.** `i_q039_lennor_speech` (ambient talk), `i_darkmoon_nastavovac_spawnu` + spawner.
7. **Systems.** GM event tooling, shapeshifting boss, day counter.
8. **Shape.** boss event (GM-run), medium as built, the quest part is a stub.
9. **Problems.** **Unfinished.** `qtag q039_darkmoon` is set nowhere; the planned per-profession tasks (craft/magic/undead/spying/strength/mind/voodoo/pets/healing, comment block darkmoon.scp ~33-45) were never written; `d_darkmoon_01` / `f_darkmoon` are used by no NPC. Speech/def names reuse "q039" though unrelated to q039 Shadow weapons.
10. **Port.** Boss fight ported: `MonsterSpecialAttacks/Darkmoon.cs` + `darkmoonForms.def` (e_darkmoon_boss), plus e_explosiondispell, e_summoneater, e_npc_curse, e_npc_detect, e_illusionist, e_parakrik, e_autoreflect, e_managain, e_looter real. NPCs converted only (`converted/questy/darkmoon*.def`). Boss not placed.
