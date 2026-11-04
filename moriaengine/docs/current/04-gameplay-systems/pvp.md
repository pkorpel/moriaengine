# PvP

Erebor's PvP: the league, arenas, RP rank, and the keep conquest. Code: `scripts/Basic/Pvp/`. Plan and the two specs:
#1116.

## What counts as PvP

`PvpRules.IsEvaluable(victim, attacker)`, Erebor's `isPvPEvaluable`, decides whether a player's death counts. The
Nazgul rings' kill count, the league and the conquest all use it. A death counts when:
- the two are of different realms;
- the victim isn't in an arena (the region's Arena flag, from Erebor's `tag.arena`, #1117);
- the victim isn't a craftsman;
- the victim's skill rank is 3 or more.

A player who dies again within 5 minutes of dying counts for nothing (`LevelingSystem.IsRecentlyDead`).

## PvP league

Erebor's `questy/pvpLeague.scp`. Points for killing players of the other realm, season by season.

- **Points.** When a player dies, everyone who fought them (`LevelingSystem.FindFighters`: the last 120 s, within 20
  tiles) gets 2, and the one who struck the killing blow 1 more. A kill is worth 3 and an assist 2, as in Erebor,
  where the killer had both the killing blow and a fight memory. GMs get nothing.
- **Seasons.** A season lasts `monthsInSeason` (2) calendar months. It ends at the end of day 1 of the month two on
  from its start: Erebor's `isTimeDateExceeded` is true only past the day. The next point after that starts a new
  season, and the old points are gone.
- **The season reward** (#1133, the user's call; Erebor never wrote one, a TODO in its script). When the next season
  starts, the old board's players get boosters by place, with a delayed message:
  - places 1-3 (`bigBoosterPlaces`) get kind 1 (+25% for 2 h, `bigBoosterKind`);
  - places 4-10 (`smallBoosterPlaces`) get kind 0 (+25% for 1 h, `smallBoosterKind`).

  A player tied with the one above gets the same. Tested in `PvpLeagueTests` (the 1st place gets the 2 h booster).
- **The board** keeps the first `playersStored` (30), and anyone tied with the 30th. A player moves up past everyone
  with fewer points, so among equals the one who got there first stays above, as Erebor's insertion.
- **Settings:** `.settings` -> PvP League.
- **The journal** (`.questy`, the PvP side) shows Erebor's `f_questy_buildPvPLeagueDialog` (`PvpLeaguePage`).
  - The title "PvP Sezona <n>".
  - "Hrac" and "Rank", ten rows a page. Places 1-3 are in hue 94, 4-6 in 89, the rest in 51. A name over 14
    letters is cut to 15 and "...".
  - Below: "Tvuj Rank" with the player's points and "Konec Sezony" with the end date.
  - Pages are the journal's sections 21-29, as Erebor's. Opening it starts a new season if the old one has ended.
- **Storage:** Erebor kept the board on a chest of season books at 2282,2597,80,10. Here it's saved with the world
  (`PvpLeague`), and nothing stands in the world for it.

Test: `PvpLeagueTests.KillsAssistsRulesBoardAndSeasons` (protocol, two players and a real death). It checks 3 for a
kill; nothing for a death right after dying, the same realm, a craftsman, rank 2, an arena or a GM; the board with
ties; and a season roll.

## RP rank

Erebor's `questy/System/RP_rank.scp` (`RpRanks`). A GM rates a character's role-play from -3 to 7, with a short
reason.

**The bonus.** The rank adds the converted `RP_rank[n]` percent to every experience gain:
- -3: -30%, -2: -20%, -1: -10%, 0: 0%;
- 1: 5%, 2: 10%, 3: 20%, 4: 30%, 5: 40%, 6: 55%, 7: 70%.

It's taken from the base amount and added next to a booster's (`LevelingSystem.AddExperience`, Erebor's `addexp`).
Losses aren't changed.

**For GMs.** `.rprank` (cursor), or the "RP Rank" button of a character's `.info` (Erebor's character-info row), opens
"RP Rank - <name>":
- the rank, with a button to change it;
- "Poznamky:", the last 12 changes as "yyyy/mm/dd: old->new - reason";
- "Prehled postav na acc:", every character of the account with race, level and rank.

The changer asks for "Nova Hodnota Ranku:" and "Kratky Popis:". A rank outside -3 to 7 is refused with "RP Rank musi
byt cele cislo mezi -3 a 7 !"; a word counts as 0, as Erebor's eval.

**For players.** `.explevel` shows "RP Rank:".

**Erebor quirks not copied:** its log keys started at "00" in some places and "0" in others, which could leave the
newest line out of the dialog, and a comma cut the reason. Erebor's Christmas event also wrote its bonus into this
rank; that event isn't ported.

Test: `RpRankTests.GmSetsItThroughTheDialogsAndItMovesExperience` (protocol: Erebor's percentages, a refused 8, the
real `.rprank` dialogs, `.explevel`).

## Arenas

Erebor's `systems/arena.scp`, `questy/arena_crunch.scp`, `questy/arena_chars.scp` (`Arena`, `ArenaSpeech`).

**Arena areas.** 21 areas carry the Arena flag (Erebor's `tag.arena`, kept by the converter since #1117). Inside them:
- a death doesn't count for PvP (no league points, no Nazgul kills);
- a death costs no experience;
- the region's attack rules allow fighting your own realm.

**The training arena.**
- The Umbar Company's agent (`c_arena_obchodnik`) stands in Minas Tirith and in Barad-dur. To *hi*/*ahoj*/*dobry den*
  he offers the arena; to *arena* he takes 200 gold (`GoldUtils.Pay`). Then "Cas do presunu: n s" counts 30 seconds,
  and the player goes to the Gondorska bojova arena (plane 5) if of Gondor, the Mordorska (plane 55) if of Mordor.
- A player of neither realm is refused before paying. On Erebor the gold was taken and nothing happened.
- His lines on seeing a new player aren't ported: there's no NPC "sees a new player" hook here.
- In each arena stand its guard (`c_arena_strazce`) and the red exit gate (`I_gate_arena_vystup`). The guard answers
  *hi*, *help*/*pomoc*, *arena* and *exit*/*vychod* with Erebor's lines.

**The arena state.**
- Going in keeps the player's karma, murders, fame, realm, deaths, player kills and monster kills. Leaving puts them
  all back and forgives a crime.
- Leaving is through the exit gate, which then sends the player home, or by being moved out of the arena area any
  other way. Erebor's net was "a double-click outside an arena ends it and sends you home".
- Fixed from Erebor: a 0 is put back too. Erebor's `if (TAG(x))` skipped zeroes, so arena murders stuck to anyone who
  came in with none.
- While inside: Recall, Mark and Gate Travel say "V arene nelze pouzit."; another player's body can't be looted
  ("Tady nesmis okradat cizi tela.") or carved ("Tady nesmis rozrezavat cizi tela.").

`.SetupWorld` places the agents, guards and gates (`ArenaSetup`). The spots are chosen here and unreviewed: Erebor's
were GM-placed.

**Dead on Erebor, not ported:**
- the older "pits" arena (`spk_go_arena*`, whose NPCs no chardef used, `i_delay_arena`, `I_gate_arena_vstup`), which
  sent players to 5521,3701,35, a spot inside no arena;
- `tim/turnaj.scp`'s telepads, which no item used.

**Not ported yet, the user's call (#1116):** the team tournament GM tool and the Murdoc PvP gates.

Test: `ArenaTests.InForAFeeOutWithEverythingBack` (protocol). It checks the real *arena* speech and fee, the
countdown, the state with recall refused, the way out through the gate with a 0 restored, and the way out by moving.

### PortZone arenas

Erebor's `questy/pvp_events.scp` (GM Yavanna), through the `r_PvP_PortZone` region type (`PortZone`). Four areas have it:
Arena Gondor, Arena Gondor Podzemi, Arena Mordor and Arena Mordor Podzemi.

A player who dies in one of them comes back 3 seconds later (Erebor's `i_PvP_PortZonePorter`):
1. back to the spot of death, where the body is, and resurrected (`s_resurrection` at 1000), so the things come back;
   these areas resurrect at once (`region_cf_instantRes`), so the 5 s rule after a death doesn't apply;
2. the last ridden creature (Erebor's `lastjezdik`, `Character.LastMount`, kept at every dismount) comes along if it's
   the player's pet within 20 tiles, and is ridden again; further away, "Tve zvire je prilis daleko.";
3. to the area's PortMeTo point if a GM set one, else 5280,3472,35 on plane 0 for a Gondor player, plane 55 for anyone
   else (both inside the arena itself);
4. paralysis goes.

As in every arena area, the death costs no experience and counts for no PvP.

`.PortZonePoint` (GM) makes where the GM stands the area's PortMeTo; `.PortZonePoint "clear"` goes back to the default.
On Erebor it was a region tag set by hand, and none is in the archive. It's saved with the region (`portmeto`).

Not ported: Erebor's `stone=0`, as nothing here sets Sphere's STONE (Erebor only set it during a name change).

Test: the same `ArenaTests` fact, after the training arena. It covers a death on a horse (alive 3 s later at Mordor's
point, gold from the body back, riding), a horse 30 tiles off (stays), and a GM's point, then cleared. It's red with the
death hook off.

## PvP event kit

Erebor's `funkce.scp` kit functions, for a GM's [variable gate](variable-gate.md) (`PvpEventKit`). Nothing in
the archive called them: GMs typed them into gates' function lists.

- `f_varGate_pvp_equip(<metal>)`, for any metal with an `i_<metal>_platemail` (iron, copper, verite, valorite, obsidian,
  adamantinum, mithril). With another one: "Nelze volat funkci s timto parametrem: ...", and nothing happens.
  1. What's worn goes to the pack. Everything in the pack that isn't fixed goes to the bank, in a red backpack named
     "Vybava z turnaje".
  2. 20 empty bottles, the PvP spellbook, 40 bandages, 15 cooked fish steaks, a llama.
  3. The profession's armor and weapons of that metal, each named PVPEVENT:
     - warrior, priest and craftsman get plate;
     - thief gets chain;
     - mage and mystic get studded;
     - necromancer gets bone;
     - shaman and ranger get ring.

     The weapons follow Erebor's table, with knives for a thief and bolts and arrows for a craftsman and a ranger.
- `f_varGate_pvp_clearequip`: what's worn goes to the pack, then everything in the pack that isn't fixed is deleted.
- `i_spellbook_pvp` ("PVPEVENT"):
  - 23 magery spells: harm, fireball, dispel, energy bolt, magic reflection, mind blast, night sight, poison, cure, heal,
    greater heal, clumsy, feeblemind, magic arrow, weaken, paralyze, flamestrike, curse, teleport, reactive armor,
    reveal, cunning, protection;
  - for a necromancer it opens as the necro book with every necro spell (Erebor's `morem=1`, `morex=0ff`).

  It's hand-written now; the converter comments out the archive's.

Nothing reads the PVPEVENT name back, as on Erebor: it's only a marker.

Fixed from Erebor: worn things that can't be moved (a badge of office, a Nazgul's ring) stay on. Erebor's bounce took them off
into the pack, where the kit then left them.

**Not ported, the user's call (#1116):** `e_qv3_dejloot`. On Erebor, leaving Mahat extra L1/L2 and Novorocni Dungeon
cleared the pack with `f_varGate_pvp_clearequip`.

Test: `VarGateTests.EveryRuleWalkedByARealStep` walks a warrior through gates with `f_varGate_pvp_equip(iron)`, then
`f_varGate_pvp_clearequip`, then a metal with no plate.
