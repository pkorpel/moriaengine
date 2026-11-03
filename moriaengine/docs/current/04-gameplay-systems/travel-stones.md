# Travel stones

Erebor's coach stations, `systems/travelstone.scp`, in `scripts/Basic/Travel`. A stone (`i_stone_travel`) stands at each of the 30 stops.

## Using one

Double click lists the stops you may go to, 14 to a page, with prices. Taking one pays at once, then a count runs above your head for 15 s. Be within 6 tiles of the stone when it ends, or you're left behind and the money is gone. A step after the list opens makes its buttons do nothing, so open it again. What's really following you goes along: not a pet told to stay, nor an NPC told to stop. Payment is `GoldUtils.Pay`, pack then bank.

- Price: longest axis of the distance, times 1000, divided by `priceDivisor` (3500), so about 1 gp per 3.5 tiles. It's measured between price points, the user's call: Minas Tirith (2977,2951) and Helmuv Zleb (2099,2559) arrive in a far corner of the map, so each has one apart from its arrival point. A stone in such a city counts as standing at it. Any other stop's price point is where you arrive.
- Enemy land isn't listed. Mordor's characters can't go to Gondor's areas, everyone else can't go to Mordor's.
- Leaving from a stone on enemy land costs 5 times as much (`escortPrice` 50, in tenths). The script's comment says 3, its number says 5.
- Outcasts are refused, a possessing mystic gets nothing. GMs see every stop, pay nothing and go at once.
- Realm alone decides, kills don't. That's the user's call: Erebor tests `ismurderer`, so there a Gondor player with enough murders changed sides. A character with no realm counts as not Mordor's, as on Erebor.

## Stops

`TravelStops.All`, from `st_pos`, in Erebor's order. A name means that area's `P`. Heights in the script are dropped, as the closest place to stand within 6 tiles is looked up, which gives the same height at the four stops that state one. A point with no height is z 0 to Sphere, which isn't always ground.

Changing a stop means changing `TravelStops.All`: the `st_pos` lines in `travelstone.def` aren't read.

## Placing

`.SetupTravelStones`, part of `.SetupWorld`, puts a stone 2 steps from each arrival point. The archive has no positions, as Erebor's GMs placed them live, so all 30 spots are unreviewed. A GM can move one and it stays moved.

## Settings

`.settings` -> Travel: `priceDivisor`, `escortPrice`, `rideSeconds`, `strayTiles`.

## Left out

- `e_noport` refusing a ride only matters to mounts, which don't use stones.
- Casting no longer needs its own check: a spell only matters if it moved you, and the move is what's checked.
- Followers aren't tested for being able to walk to you first, as Sphere does.
