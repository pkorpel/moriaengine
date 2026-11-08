# World events

Step 23 of the living world (#1237, #1284) builds the design's section 6: the world itself starts things. Code is in
`scripts/Basic/LivingWorld/Events/`: `WorldEvents.cs` (the framework), `OrcRaid.cs`, `Caravan.cs` and `events.def`. The
numbers are in `.settings` "Udalosti". Steps 24 and 25 add more kinds (beacons, fire, fever, the troll at the ford,
corsairs, festivals) on the same framework.

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

## Outcomes

**Prices:** an outcome changes the city's prices by a percentage for a while. `Reputation.PriceFor` applies it after the
standing and bath-house discounts, so the vendor dialogs show it. Several outcomes in one city add up, within -50% to
+100%. They expire on the server clock and aren't saved.

**The criers:** every outcome goes to `CityNews`, and the start of an event does too.

**The journal:** the Udalosti tab shows what's happening now (each running event's line) and the latest outcomes.

## GM command

- `.udalost` lists the running events with their minutes left.
- `.udalost "start <najezd|karavana> [City]"` starts one now, whatever the hour. Without a city, it starts in the city
  the GM stands in. As with other GM commands, the words go in quotes.
- `.udalost stop` ends the events in the GM's city (all events outside a city), with no outcome.

## Not done here

- The NPCs' looks and strength are mine. The archive's own bandits (`c_zm_bandita`, ~2000 hits) are a dungeon's.
- The route is a breadth-first walk from the centre, so it can come from any direction, roads or not.
- Outcomes and events are in memory, like `CityNews`.
- Nothing has been tried in a real client yet.

## Test

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
