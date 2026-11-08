# Venues

Step 1 of the living world (#1237, #1240; the design is [quests/living-world-design.md](../quests/living-world-design.md)). A venue is a
building or an open place of a city with a type, a Czech name, opening hours and the roles it wants filled. Residents (step 2, [residents.md](residents.md)) fill
the roles and errands send players to venues. Code: `scripts/Basic/LivingWorld/Venues/`, setup: `scripts/Setup/VenueSetup.cs`.

## What a venue is

A hidden marker item, `CityVenue` (`i_venue_marker`, the same invisible, fixed gem a house uses), standing in the middle of the venue.
Everything a venue knows is a tag on it: type, name, city, hours, rectangles, the height band and a permanent id `venue/<city>/<n>`,
which is also its world piece id.

It is deliberately **not a region**, although the design said so first. `FlaggedRegion.City` and `RealmTag` aren't inherited from a
parent, and much of the game asks which area a player stands in (the town stone works only in its own area; guards, houses, the
labour camp and conquest look at the region too). A venue region inside a city would quietly change all those answers. A venue
answers only its own questions: `VenueRegistry.At(point)` gives the smallest venue holding a point.

A room-scanned venue covers its floor and a little above (`ZMin - 5` to `ZMax + 15`), so the floor above a tavern isn't the tavern.
An open-air venue (set by corners) takes any height.

## Types

27 types in `VenueType.All`, each with default hours on the game clock and roles:

- **Common:** tavern, bath house, theatre, armoury, smithy, temple, Houses of Healing, library, harbour, stable, market, bakery,
  brewery, barracks, prison, bank, town hall, tailor, farm, mine, house (a home, no roles).
- **Mordor's own:** mess hall, fighting pits, forge, warg kennels, overseer's post, slave pen, and Harad's caravanserai and Umbar's corsair den (#1282, [harad-umbar.md](harad-umbar.md)).

Roles say how many people and when (`VenueShift`: night, morning, day, evening).

**The game clock (`VenueClock`):** the first half of LightAndWeather's cycle is day, 6:00 to 18:00, and the second half is night.
At the default 24-minute day, a real minute is a game hour. The shifts are morning 6-10, day 10-18, evening 18-22, and night.
Opening hours can run past midnight (a tavern opens 8 to 2), and 0 to 24 means always.

## GM commands

| Command | What it does |
|---|---|
| `.venue` | Inside a venue: edit it. Elsewhere: a new venue from the room you stand in, its outline shown with sparkles |
| `.venues` | The venues of the city you stand in: go there, edit |
| `.venues vse` | All venues |
| `.venues navrh` | Roofed rooms of the city that aren't venues yet, biggest first: go, mark as a venue, skip |
| `.venues export` | Write every venue to `scripts/Setup/venues.txt` |

**The dialog** (`D_Venue`):
- the type, browsed with arrows; the name; opening hours;
- *Najit mistnost odtud* scans the room again from where you stand;
- *Nastavit rohy kurzorem* replaces the area with two cursor corners, which open-air venues use;
- *Pridat dalsi obdelnik* adds one, for an L-shaped house;
- *Ukazat obrys* shows the outline;
- *Smazat* deletes the venue at once; *Ulozit* saves the rest.

The city is found by itself: the smallest area with Erebor's city name holding the spot, as houses find theirs.

## Finding a room

`RoomScan.Scan` spreads from the tile you stand on over the floor, by the real movement rules and in straight steps only, so walls
stop it. It takes only tiles with something overhead: a static with the Roof or Surface flag at least 12 above the floor. A doorway
with the street beyond ends the room. It stops at 1600 tiles or 48 tiles from the start, and the GM is told the room was cut short.

**What the map gives** (a statics scan of the pilot cities, rooms of 12 tiles or more):
- Minas Tirith about 75, Harad 25, Umbar 26, Barad-dur 5. Edoras 8 in the town; the engine finds 77 over all of Edoras's areas,
  Helm's Deep included.
- **Hobitin none.** Hobbit holes have no roofed inside on our map, so the Shire's venues are set by corners, as are Barad-dur's
  open camps.

## Keeping venues across a rebuild

The world is rebuilt from an empty save now and then, so venues live in a file too.

**The export:** `.venues export` writes one line per venue, `id|city|type|opens|closes|m|zmin|zmax|rects|title`, with rectangles
as `x1,y1,x2,y2;...`.

**The setup:** `VenueSetup`, part of `.SetupWorld`, places each line's venue where it's missing, by its id. A venue already in the
world keeps what a GM changed, as every setup only adds. A new venue's id skips the file's ids too, so one marked on a rebuilt
world before `.SetupWorld` ran can't take a filed venue's place. No venues are marked yet, so no file is committed: the first export
starts it.

## Tests

`Tests/Protocol/VenueTests.cs` (server side `scripts/Tests/VenueTests.cs`, own collection, account `venuetests`):
- **The room scan** of a real Edoras building: bounded, every tile roofed, the rectangles exactly the tiles.
- **A venue from it:** city, id, hidden marker, inside and outside, the floor above, hours and shifts, the next id, the list,
  and candidates that don't overlap it.
- **The export and setup round trip:** a deleted venue comes back, a second run adds nothing and keeps a GM's rename.
- **An open-air venue through the real dialog:** `.venue`, the corners cursor, the type arrow, typed name and hours, then the
  `.venues` list.

`LivingWorldTestBase` holds the login and check helpers that later steps reuse.
