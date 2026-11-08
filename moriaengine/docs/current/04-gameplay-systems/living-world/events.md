# World events

Step 23 of the living world (#1237, #1284) builds the design's section 6: the world itself starts things. Code is in
`scripts/Basic/LivingWorld/Events/`: `WorldEvents.cs` (the framework), `OrcRaid.cs`, `Caravan.cs` and `events.def`. The
numbers are in `.settings` "Udalosti". Step 24 adds more kinds on the same framework (#1286): the corsairs, the beacons, the troll, fire and fever. Step 25 adds
the festivals.

## How an event starts

The scheduler runs on Globals and ticks every 5 s (`WorldEventTimer`). It starts at a player's login and at a GM's
`.udalost`, and survives a save.

An event starts by itself only when all of these hold:
- `enabled` is on;
- fewer than `maxRunning` (1) events are running;
- at least `gapMinutes` (45) real minutes have passed since the last start;
- each minute after that, a roll of `startChance` (25%) succeeds.

When the roll succeeds, the scheduler picks at random among the kinds whose time it is and the pilot cities they're for.
A city qualifies only if it has no event running and at least one non-GM player stands within `playerRange` (40) tiles of
its centre. The centre is the spawn point of the city's main area, `a_<City>`. If an event finds no room to start
(no route out of the city), the scheduler tries the next choice.

The players near the centre at the start decide the event's size.

**In memory:** events aren't saved. A restart ends them. On the scheduler's first tick after a boot, it deletes any NPC
that still carries an event's tag (`_wev_`) while no event of that id is running.

## The orc raid (`najezd`)

**Where and when:** Gondor's pilot cities (Minas Tirith, Edoras, Hobitin), at dusk: game hours `raidDuskFrom` to
`raidDuskTo` (17 to 21).

**Start:** a warband comes in from a tile `raidSteps` (24) real walking steps out of the city and makes for the centre:
- size: `raidBase` (3), plus `raidPerPlayer` (2) for each player near, at most `raidMax` (12);
- the first one is a chief (`c_event_raid_chief`), the rest are raiders (`c_event_raider`);
- they're hostile, and attack anyone they meet;
- each raider's home and go point are the centre (leash 6). A go order given up on a blocked street is given again
  while the raider is still far off.

**Won** (every raider dead within `raidMinutes`, 15):
- every player who fought a raider (as experience counts fighters, `LevelingSystem.FindFighters`) gets `raidGold` (300),
  plus `raidKillGold` (50) for each raider they killed;
- they also get `raidExp` (500) experience and `raidFavour` (20) standing in the city (not for the enemy realm);
- the city feasts: prices change by `raidWonPercent` (-5%) for `raidWonMinutes` (60).

**Lost** (time runs out):
- the raiders leave with the harvest;
- prices change by `raidLostPercent` (+15%) for `raidLostMinutes` (a day).

A raid beaten by the city's guards alone still counts as won, with nobody paid.

## The caravan (`karavana`)

**Where and when:** any pilot city, at any hour.

**Start:** a merchant (`c_event_merchant`, named from the city's name list) and `caravanPack` (2) pack horses start
`caravanSteps` (36) walking steps out. The merchant walks the route into the centre a few steps at a time, and the
horses follow him.

**The ambush:** halfway along the route, bandits (`c_event_bandit`) come out on both sides of the road and go for the
merchant:
- their number is `banditBase` (2), plus `banditPerPlayer` (1) for each player near, at most `banditMax` (8);
- they're hostile to anyone;
- the merchant stops and calls for help, and walks on only when they're all dead.

**Arrived** (within 3 tiles of the centre, inside `caravanMinutes`, 15):
- paid: everyone who fought one of the caravan's attackers, and every player within `escortRange` (15) tiles of the
  merchant;
- each gets `caravanGold` (250), `caravanExp` (400) and `caravanFavour` (15) standing;
- prices change by `caravanPercent` (-10%) for `caravanOutcomeMinutes` (120).

**Lost** (the merchant dies, or time runs out): the caravan is gone, with no price change.

## Corsairs at the harbour (`korzari`, #1286)

**Where and when:** Umbar's ships land raiders at Pelargir, Dol Amroth's harbour (city `Dol_Amroth`, area `a_Pelargir`), at
any hour.

**The fight:** it runs as the orc raid does, on the same `Warband` code with its own numbers (`corsair*`):
- a captain (`c_event_corsair_captain`) and corsairs (`c_event_corsair`);
- size: `corsairBase` (4), plus `corsairPerPlayer` (2) for each player near, at most `corsairMax` (14);
- they come from `corsairSteps` (20) walking steps out, and there are `corsairMinutes` (15) to beat them.

**Beaten:** the defenders are paid as in the raid (300, plus 50 a kill, 500 experience, 20 standing), and the harbour stays
open: prices -5% for 60 minutes.

**Not beaten:** the harbour closes, and prices rise `corsairLostPercent` (+20%) for `corsairLostMinutes` (6 hours).

Both outcomes count for all of Dol Amroth's city (Pelargir is one of its areas): prices are kept per city.

## The beacons of Gondor (`majaky`, #1286)

**Where and when:** Minas Tirith, at any hour.

**The chain:** `beaconCount` (5) beacons (`i_event_beacon`, kindling) follow a line from the city's centre towards Edoras's,
one point every `beaconSpacing` (30) tiles. If both cities aren't on one map, the line runs west. Gondor's chat calls for them to be lit.

**Lighting:** a double click within 2 tiles lights a beacon, but only the next one in the chain: the others answer which
must burn first. A lit beacon turns into a campfire, and Gondor's chat counts them. A ghost can't light one. Neither can
anyone of the enemy realm, though a GM can.

**Spots:** a beacon stands on the tile nearest its point on the line that a walker reaches from the beacon before it (from
the centre, for the first) within twice the spacing, and at least half a spacing on. That keeps beacons off ledges, islands
and closed houses, and the chain bends round walls and hills while still heading towards Edoras.

**All lit in time** (`beaconMinutes`, 20):
- every lighter of the city's side gets `beaconGold` (200), `beaconExp` (300) and `beaconFavour` (15);
- every Gondor player gets `beaconBonusPercent` (10%) more of each experience gain for `beaconBonusMinutes` (60). It's
  added in `LevelingSystem.AddExperience` beside boosters and RP rank, as a percentage of the base gain, and only to gains.
  The bonus is kept in memory: a restart ends it early;
- a GM can light beacons, but isn't paid. A chain lit by GMs alone gives no bonus.

**Not lit in time:** the beacons go, and the criers say Rohan never heard.

## The troll at the ford (`trol`, #1286)

**Where and when:** Gondor's pilot cities, at night (game hours `trollNightFrom` to `trollNightTo`, 22 to 4).

**The troll:** a strong hostile troll (`c_event_troll`, ~2000 hits) stands on a road `trollSteps` (30) walking steps out of the
city, at least 10 tiles from the centre. When nobody keeps it busy, it walks back to its spot.

**At sunrise:** the first morning after a night, on the game clock. A GM's start in the morning waits for the next sunrise.
- **Still fought:** if players of the city's side fought it lately (as experience counts fighters), it turns into a stone
  troll (`c_event_stone_troll`: invulnerable, frozen, grey). The stone stays as a landmark for `trollStoneMinutes` (4
  hours), or until a restart. The fighters get `trollGold` (400), `trollExp` (800) and `trollFavour` (25).
- **Nobody fought it:** it walks off into the hills, and nobody is paid.

**Killed before dawn:** those who fought it are paid the same, and no stone is left.

**No sunrise in time:** after `trollMinutes` (30) without one (for example, with a long night setting), the troll leaves as if
nobody had fought it. A troll a GM removes ends the event with nothing told.

## Fire in the city (`pozar`, #1286)

**Where and when:** any pilot city, at any hour.

**The fire:** it starts in a house, a marked venue of the city picked at random (on a spot inside it). With no venues marked,
it starts on a spot a few steps from the centre. `fireStart` (3) fires (`i_event_fire`) burn there. Every `fireSpreadSeconds`
(30), one more starts beside a burning one, on a free tile.

**Beating it out:** a double click within 2 tiles, alive, on the same floor (within 15 of height) and in sight, is a blow.
New fires spread from a burning one's own tile, so indoors they stay on its floor. It costs `fireBeatStamina` (5) stamina, and a tired
player can't strike. `fireBeats` (3) blows put a fire out.

**All out in time** (`fireMinutes`, 10):
- every player of the city's side who struck at least `fireBeats` blows (one fire's worth) gets `fireGold` (200), `fireExp`
  (300) and `fireFavour` (15) once, however many more blows;
- each also gains `fireFriendship` (2, on the residents' 0-10 scale) with every resident of the house.

**Lost** (`fireMax`, 12, fires burning at once, at least one more than the start; or time up): the house burns down, and the criers tell of the ruins.

## Fever (`horecka`, #1286)

**Where and when:** any pilot city, at any hour.

**The sick:** `feverCount` (4) of the city's residents fall ill, picked from those with a living body. The criers and the
journal name them, and they cough now and then. They stay the city's residents, not the event's NPCs, so nothing of theirs goes when it ends.

**Curing:** a double click on a sick resident, near enough and in sight, tries to cure them instead of opening their usual
talk. A cure takes one clean bandage (`i_bandage`) and one ginseng root (`i_reag_ginseng`, kingsfoil's stand-in). Without
them, the player is told what's needed and the resident's usual talk opens, so errands with them go on.

**All cured in time** (`feverMinutes`, 30):
- each curer of the city's side gets `feverGold` (100) and `feverExp` (150) per cure, and `feverFavour` (15) standing. In
  Minas Tirith, the Houses of Healing (`mt_healers`) rise as much;
- each cure also gives `feverFriendship` (2) with the one cured, once the fever is beaten;
- a GM can cure, but isn't paid.

**Lost:**
- time up: nobody dies; the criers say the sick get better only slowly;
- a sick resident who dies or is taken away before being cured: the fever isn't beaten, whatever the other cures, and
  nobody is paid.

## Outcomes

**Prices:** an outcome changes the city's prices by a percentage for a while. `Reputation.PriceFor` applies it after the
standing and bath-house discounts, so the vendor dialogs show it. Several outcomes in one city add up, within -50% to
+100%. They expire on the server clock and aren't saved.

**The criers:** every outcome goes to `CityNews`, and the start of an event does too.

**The journal:** the Udalosti tab shows what's happening now (each running event's line) and the latest outcomes.

## GM command

- `.udalost` lists the running events with their minutes left.
- `.udalost "start <najezd|karavana|korzari|majaky|trol|pozar|horecka> [City]"` starts one now, whatever the hour. Without a city, it starts in the city
  the GM stands in. As with other GM commands, the words go in quotes.
- `.udalost stop` ends the events in the GM's city (all of them outside a city, or with `"stop vse"`), with no
  outcome.

## Not done here

- The NPCs' looks and strength are mine. The archive's own bandits (`c_zm_bandita`, ~2000 hits) are a dungeon's.
- The route is a breadth-first walk from the centre, so it can come from any direction, roads or not.
- Outcomes and events are in memory, like `CityNews`.
- Nothing has been tried in a real client yet.

## Test

`Tests/Protocol/WorldEventsTests.cs` has three facts, on one server.

**CityEventsTests** (#1286):
- **In the client:**
  - three double clicks beat the first fire out;
  - a double click on a sick resident cures them, carrying a bandage and a ginseng.
- **On the server:**
  - the fever: the cure takes both things, an empty pack is refused, every cure is paid, and friendship with the cured
    goes up;
  - the fever not cured in time: the sick stay;
  - the fire at the test tavern: blows from afar and tired blows refused, stamina spent, the fire spreading;
  - the fire put out: paid once, friendship with the house;
  - too many fires.

**OutdoorEventsTests** (#1286):
- **In the client:** a GM starts the beacons. A double click on the second beacon is refused (not its turn), and one on the
  first lights it.
- **On the server:**
  - the chain's spots;
  - out of order refused;
  - the rest lit by a player, who's paid; a GM isn't;
  - the realm's bonus through `AddExperience` (Gondor only);
  - the beacons not lit in time;
  - the corsairs at Pelargir (a captain first, the harbour's centre, the harbour closing);
  - the troll fought until dawn: the stone, the pay, the landmark expiring;
  - the troll left alone;
  - the troll killed.

**RaidsAndCaravansComeAndGo** (#1284) is below.

`Tests/Protocol/WorldEventsTests.cs` (server side: `scripts/Tests/WorldEventsTests.cs`, account `worldeventtests`).

**In the client:**
1. A GM starts a raid on Edoras with `.udalost`.
2. The journal's Udalosti tab lists it.

**On the server:**
- the kinds' rules and sizes;
- the raid as the GM started it (the chief, no sleeping, home at the centre);
- a raid won (gold per kill, standing, the feast price, the criers);
- a raid lost (the prices through `Reputation.PriceFor`);
- refused starts;
- the caravan: on the road, ambushed, the merchant waits at his waypoint, arrives, escorts paid, the price;
- a caravan lost;
- leftovers deleted after a restart;
- the scheduler: off, on, only a caravan at noon, one event at a time.
