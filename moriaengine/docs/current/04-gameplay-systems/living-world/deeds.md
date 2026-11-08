# Deeds and titles

Step 28 of the living world (#1237, #1296) builds the design's section 9.6. A deed counts something players do anyway.
Each tier a deed reaches pays gold and experience once and gives a title. The journal's Skutky tab shows every deed:
the count toward the next tier and the highest title reached. A title is a record, not a cosmetic reward: the reward is
the pay.

Code is in `scripts/Basic/LivingWorld/Deeds/Deeds.cs`. The numbers are in `.settings` "Skutky".

## The deeds

| Deed | Counts | Tiers | Titles |
|---|---|---|---|
| Lovec (`lovec`) | monsters killed | 10, 100, 1000, 10000 | Lovec zverat, Lovec, Postrach divociny, Legenda lovu |
| Posel (`posel`) | errands done | 5, 25, 100, 500 | Pomocnik, Spolehlivy posel, Mistr poslu, Hlas mest |
| Cestovatel (`cestovatel`) | different cities entered | 3, 8, 15, 30 | Poutnik, Cestovatel, Svetobeznik, Znalec Stredozeme |
| Obrance (`obrance`) | world events won as a hero | 1, 5, 20, 50 | Obrance, Strazce, Hrdina mest, Stit kraje |
| Host slavnosti (`slavnosti`) | festivals attended | 1, 5, 15, 30 | Host, Stamgast slavnosti, Duse slavnosti, Kral slavnosti |
| Pritel mest (`pritel`) | cities where the player is at least Vazeny | 1, 2, 4, 6 | Vitany host, Pritel mesta, Pritel mest, Milacek Stredozeme |

How each one counts:
- **Lovec:** from `NPC.On_Death`. Every player who fought the monster counts it, the same fighters that experience
  goes to (`LevelingSystem.FindFighters`, without parties). Not counted: a monster worth no experience, a resident, a
  pet, a summoned creature, or a death nobody caused. A pet its own owner attacks turns on them and is wild again
  (`NPC.On_HostileAction`), so killing your own pet counts, just as it already pays experience. The troll is worth no
  experience, so it doesn't count.
- **Posel:** from `Errands.Finish`, so only a finished errand counts. Giving up doesn't, and neither does running out.
- **Cestovatel:** from `FlaggedRegion.On_Enter`. The first time the player enters an area of a city, that city counts.
  The cities seen are kept in a player tag.
- **Obrance:** from `WorldEvents.Reward`, which every event win pays through: raids, corsairs, the caravan, the
  beacons, the troll, the fire, the fever. Each hero is paid once per event, so each counts it once.
- **Host slavnosti:** counted for every guest at a festival's end. The animal game's prize counts toward no deed.
- **Pritel mest:** worked out whenever it's read, not counted. It is checked after every gain in a city's standing
  (`Reputation.Change`) and when the Skutky tab opens. A standing that later drops keeps the tiers already paid.

GMs don't do deeds. Enemies of the city get nothing from an event, so they get no deed from it either
(`WorldEvents.IsOnCitysSide`).

## Pay

`.settings` "Skutky":
- `gold` (300) and `exp` (500): what the first tier pays;
- `growth` (3): each later tier pays this many times the tier before it.

So the tiers pay 300, 900, 2700 and 8100 gold. Gold comes through `GoldUtils.Grant`, experience through
`LevelingSystem.AddExperience`, so boosters and event bonuses apply. A count that jumps past several tiers at once pays
each tier, in order. A tier reached is kept in a player tag, so it is paid once.

## Storage

Each deed's count and tier are player tags (`_deed_<key>`, `_deed_tier_<key>`), and the cities seen are another
(`_deed_cities_`). They survive saves like any tag.

## Not built

- **Lore** (read the library's books): no book reading is tracked yet.
- **Slayer by kinds of monsters** and **explorer by landmarks of a region**, as the design describes them. The first
  version counts kills and cities instead.
- **The crier telling deeds.** A new title is told to the player alone.

## Tests

`Tests/Protocol/DeedTests.cs` and `scripts/Tests/DeedTests.cs`. The server side plays:
- real kills, including a pet, a worthless monster and a GM's kill, which don't count;
- one errand;
- walks into three cities, entering some of them more than once;
- a troll beaten at night;
- an enemy's reward;
- a festival with a catch;
- a gain in standing that makes the player Vazeny.

It checks the counts, the tiers, the pay and the titles. The client then reads the Skutky tab.
