# Weekly quests (Tydenni questy)

Code: `scripts/Basic/Quests/Weekly/`, from Erebor's `questy/weekly.scp` and the journal's weekly section
(`questy/System/questmenu.scp`). #1090.

## How a player gets them

No NPC gives them. A player of **level 10 or more who isn't a craftsman** gets a set of tasks, checked at each level-up,
on opening `.questy` (not for GMs), on any button of the journal, on each step of a task, and at login for a player who
has had a set. The set lasts **7 days from when it came**, per player. Then "Cas na plneni questu vyprsel." and a new set
comes at once; unfinished tasks are lost. There's no giving up or rerolling. A new set says "Prijal jsi novy seznam ukolu.
Pro zobrazeni zadej prikaz .questy".

| Level | Tasks |
|---|---|
| 10-19 | 3 |
| 20-39 | 4 |
| 40-59 | 5 |
| 60-79 | 6 |
| 80-99 | 7 |
| 100+ | 8 |

Slots 1-4 draw from the low pool, 5-6 from the medium pool, 7-8 from the high pool. A draw starts at a random task of the
pool and takes the next one when that one is already in the set or blocked, wrapping at the end, so the task after a
blocked one comes up more often. Erebor's quirk, kept. The level is taken at the draw, capped at 200; levelling up
mid-week adds no tasks.

## Rewards

Fixed at the draw. With L the level: `start = (L <= 100 ? L*100 : (L-100)*50 + 10000) + 200000/(L+20)`; a task with
reward size RS pays `single = start*RS`:

- **experience** `single/2200` (through the normal gain, so a [booster](boosters.md) adds to it),
- **gold** `single/250`, into the bank in piles of 60000,
- **magic bags** (`i_mi_bag`) into the bank, medium and high tasks only: `min(single/850000, the task's most)`.

All tasks done adds the set's reward: half the sum of the tasks' singles, rounded up, as `prize`; gold `prize/250`,
experience `L*2 + prize/2200`, bags `prize/850000 + 1`. Each finished task shows "Splnen Weekly Quest" with a button to the
journal, and "Splnil jsi ukol tydenniho questu. Odmena byla vlozena do banky." (or "...vsechny ukoly...").

## The tasks

| Pool | Task | Counted when | Target |
|---|---|---|---|
| Low | Snez jidlo: any food (RS 60) / fruit (RS 65) | double-clicking that food while there's room to eat | `qCount*{4..qCount+1}/weight` in tenths of a stone, at least 1; ribs x8, cut fish x2 |
| Low | Pozdrav Mistra skillu X (RS 53) | greeting the teacher of that skill; it says "Taky Te zdravim, <name>." | 1 |
| Low | Vypij lektvar: Nightsight (RS 55) / a lesser boost potion (RS 65) | double-clicking it off the potion cooldown | `qCount*{mult..mult-2+qCount}`, mult 6 / 2 |
| Low | Stoupni na pavucinu (RS 65) | stepping onto a web no one with the task has stepped on | `qCount*{30..50}/10` |
| Low | Lootni predmet: gold (RS 53) / any fruit (60) / reagents (65) / gems (75) | lifting from a monster's corpse what it had from its `creaturestart` (its gold and extras, any monster with `i_monster_create` not raised again, spawned or added by hand: Erebor's `spawned_item`, a misleading name), with no more than it had at death; what came into its pack later doesn't count | `(L*mult + {-500..0})/(delim + L)` |
| Low | Zabij npc v dungeonu s alespon 20xp (RS 80) | a kill credited in a dungeon (planes 2, 10-15) with that much experience share | `(L*550 + {-500..100})/(200+L)` |
| Low | Osetri bandazemi poskozeni (RS 75) | hit points a bandage heals on a person of the healer's realm, the healer too | `L*k*(Healing/2)*(Anatomy/10)/10000`, k 100 for priests and shamans, else 15; at least 100 |
| Medium | Vypij lektvar: a boost potion (RS 80) | as above | mult 4 |
| Medium | Udel poskozeni monstrum (RS 100) | damage the player's blows do, to anything (Erebor never checked) | `qCount*{L*10000..(L+1)*10000}*35/1000` (15 for priests and craftsmen) |
| Medium | Utrz prime / jakekoli poskozeni (RS 100) | blows taken from a player or spawned monster of another realm, not a GM; "jakekoli" also poison and bleeding | `qCount*50*hits`, 30% of it for all but thieves and warriors |
| Medium | Utrz neprime poskozeni / poskozeni jedem / krvacenim (RS 110) | poison (its strength each tick) and bleeding taken | `qCount*10*hits`, the same 30%; poison or bleeding alone half |
| Medium | Zabij npc v dungeonu s alespon 100xp (RS 115) | as above; of several such tasks only the highest one a kill reaches | `(L*160 + {-500..100})/(200+L)` |
| Medium | Zabij v kratkem sledu N monster (RS 100) | kills each with a share of at least qCount+2, each within 15 s of the last; N = qCount*2 | `qCount*2/3` streaks |
| Medium | Zucastni se dobyvani / aktivuj Palantir (RS 100) | a double click on a palantir while the war on its keep runs, any realm, alive and not paralysed (#1116) | 1 |
| High | Vypij lektvar: a greater boost potion (RS 100) | as above | mult 5 |
| High | Zabij npc v dungeonu s alespon 200xp (RS 160) | as above | `(L*85 + {-500..100})/(200+L)` |

Potions are chosen by profession: necromancers, mages, mystics and shamans get refresh, heal, cure or clever (2x as likely);
the rest get refresh, agility (2x), heal, strength (2x) or cure. Most tasks say how far along they are on each step;
damage dealt and taken are silent until done.

**Never drawn**, as Erebor kept out its own player-kill tasks (medium 5, high 1 and 2):

- the hunting sign (medium 7): `i_sign_gquest` isn't ported;
- "Dokonci Dungeon" (medium 8, high 4): Erebor never called its hook (`f_vg_finishDng`), so it could never be done there
  either, and the whole set's reward was lost whenever it was drawn.

Each comes back when its system is ported. Weekly quests give no quest items, so the [quest-item rule](README.md) changes
nothing here: gold and magic bags are ordinary rewards.

## The journal's weekly section

`.questy`, its second side tile. The open tasks in blue first, the done ones in grey with "S P L N E N O", four a page;
each open one's button shows its reward. The slots a higher level opens show "Potrebna uroven : N" with a lock. Below,
the set's reward and "Zbyva: <time>", or "S P L N E N O" and "Nova Sada: <time>". Below level 10: "Plneni se otevre na 10
urovni."; a craftsman: "Plneni Tydennich Questu je prozatim pro povolani kraft nepristupne".

## Differences from Erebor

- The week runs on real time, not Erebor's server clock, which a tool had to shift on every clock change.
- Erebor's quirk that a set renewed during an enemy's blow could take the enemy's level is gone: a set is always its
  player's.
- One detail gump; Erebor alternated two to work round a client race.
- An item counted for a loot task can't be counted again (Erebor's tags stayed on it).
- The statistics stone (`def_weekly_stone`, nothing ever read it) isn't kept; the generated, completed and reward lines go
  to the server log as Erebor's account messages.
- Erebor's dead code isn't ported: the player-kill tasks, `i_magic_place` (its functions never existed) and the unused
  "lucky reward".

Tests: `QuestJournalTests.WeeklyQuestsFollowErebor` (protocol: counts by level, the pools, no blocked or repeated task, Erebor's
worked reward example, no set for a craftsman, the week and its renewal, eating, a teacher's greeting, a web once, a spawned
monster's loot and not a plain one's, blows both ways, bleeding, a streak, the payouts into the bank, the journal's page,
a task's detail and the popup of a task done).
