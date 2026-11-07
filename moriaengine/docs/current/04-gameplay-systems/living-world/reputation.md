# Reputation

Step 3 of the living world (#1237, #1243), the design's section 15 as the user accepted it. A city's view of a player, and of the
factions inside it. Code: `scripts/Basic/LivingWorld/Reputation/Reputation.cs`.

## What is kept

- **City reputation** per city (Erebor's city name, `FlaggedRegion.City`), -1000 to +1000.
- **Faction reputation**, the same scale, for the pilot cities' factions (`Faction.All`):

| City | Factions (rivals marked) |
|---|---|
| Minas Tirith | Straz Citadely, Kupci dolnich kruhu, Domy uzdraveni, Archivari |
| Edoras | Kraluv dvur, Jezdci, Chovatele koni |
| Hobitin | Hobitinska posta, Sedlaci (rival: Hostinsti), Hostinsti |
| Barad-dur | Gorbagova tlupa and Shagratova tlupa (rivals), Dozorci, Ucho Sauronova Hlasu |
| Harad | Vudci karavan, Krotitele olifantu, Chram hada |
| Umbar | Korzarsti kapitani and Pani pristavu (rivals), Otrokari a paseraci |

- **Friendship** with single residents stays on its own ([residents.md](residents.md)).

All of it lives on the player as tags with word-only names (`_rep_c_barad_dur`): a saved tag's name is read back by `\w+`, so a
dash or slash would lose it at a restart.

**Starting values**, worked out rather than stored: 0 in your own realm's cities and in neutral ones, `enemyRealmStart` (-600)
in the other realm's, and `outcastStart` (-200, Podezrely) everywhere for an outcast.

## Levels

| Level | From (`.settings` "Povest") | Price |
|---|---|---|
| Nepritel | below Podezrely | refused (`priceEnemy` 0) |
| Podezrely | -300 | 150% |
| Neznamy | -100 | 110% |
| Znamy | 0 | 100% |
| Vazeny | 200 | 95% |
| Cteny | 450 | 90% |
| Hrdina mesta | 750 | 85% |

A hero with no good deed in the city for `heroRecentDays` (30) shows as Cteny.

## Gains and losses

- **`Reputation.Change(player, city, delta, faction)`:** the one way up or down.
  - A faction's change also changes its rival's the other way, by half.
  - Errands, events and story quests (later steps) call it.
  - `Set` sets a value outright, for a GM or a test, without counting as a deed.
- **Crimes:** a hostile act at an innocent (`Notoriety.OnHostileAction`), looting (`OnLooting`) and snooping (seen by a human NPC,
  `CrimeWitness.Notice`, or failed while invisible) cost `crimeLoss` (50) in the city where it happened: for looting where the
  looter stands, not where the dead owner's ghost went. Every blow flags again, so it counts once a minute
  (`harmResidentEverySeconds`), one timer for all cities.
- **Murder:** `Notoriety.RegisterMurder` costs `murderLoss` (300) in the city where the victim fell.
- **The enemy realm:** fighting, robbing or killing a Gondor player or resident as Mordor (or the other way) costs nothing: war
  isn't a crime. The enemy city's start of -600 is its judgement.
- **Pets:** a pet's blow at a resident, or its kill of one, counts for its owner. A pet's attack on a player is no crime for
  Notoriety, so it costs nothing either.
- **One act, two losses:** a killing blow is also a hostile act, so a one-blow murder costs 300 + 50, and a one-blow resident
  kill 20 + 150.
- **Residents:**
  - hurting one (`DamageManager`, damage done) costs `harmResidentLoss` (20), once a minute;
  - killing one (`NPC.On_Death`) costs `killResidentLoss` (150).
- **Drift:** bad standing drifts back toward the start (0, or -600 in the enemy realm's cities) by `driftPerDay` (50) a real day,
  worked out when read. Good standing doesn't decay. A clock set back doesn't make it worse.

## Effects

- **Shops:** the basic vendor, the alchemists and the Prodejce charge the level's price where the player stands, and refuse an enemy. A GM pays list price. A price setting of 0 refuses only at Nepritel; at any other level it means list price, never free.
- **Where:** the city of a point is `VenueRegistry.CityAt` (the smallest city area holding it), remembered per tile.
- **Guards:** your own realm's guards stop protecting a player the city counts as Nepritel (`GuardSpeech.IsProtectedBy`). Since guards attack on sight whoever they don't protect, such a player is attacked. The enemy realm's guards attack them anyway.
- **A suspect entering a city** hears a guard nearby say "Mam te na ocich, <name>." (or gets a red line if no guard is near). Once in five minutes, as a city is many areas.
- **Citizenship** at a town stone wants `citizenshipMinLevel` or better in that city (`Reputation.MayBecomeCitizen`, from
  `Town.TryJoin`). The default is Znamy, which a stranger of the realm already is, so nobody is locked out before errands can
  raise standing; the design's Vazeny is a GM's setting until then.
- **`.povest`** (a player command) lists the six pilot cities, any other city where the player has a standing, and their factions,
  with level and value, two cities a page. The journal's Mesto tab ([journal.md](../quests/journal.md#zivy-svet-1245)) shows it
  too, a city a page, with the player's friends there.

## Tests

`Tests/Protocol/ReputationTests.cs` (server side `scripts/Tests/ReputationTests.cs`, account `reputationtests`):
- **Starting values and levels:** realm starts, word keys, raise, cap, the hero's recent deed, drift and its stop at the start
  (0, and -600 in Barad-dur for Gondor), a clock set back, faction rivalry, an outcast's start.
- **Effects:** prices by level, an enemy refused, nothing costs nothing, a zero price setting never free, the citizenship
  setting, a GM untouched, guards protecting a known citizen but not the city's enemy.
- **Losses:** a crime counted once a minute, none against the enemy realm, counted where it happened; a murder; a resident hurt
  (exactly once a minute, by the player or their pet, not by the enemy realm) and killed.
- **`.povest`** opened by a plain player.

`TownTests`' qualified citizen now gets a respected name first.
