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

**Starting values**, worked out rather than stored: 0 in your own realm's cities and in neutral ones, and `enemyRealmStart` (-600)
in the other realm's.

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
- **Crimes:** `Notoriety.FlagCriminal` costs `crimeLoss` (50) in the city where it happened. Every blow at an innocent flags again, so it counts once a minute (`harmResidentEverySeconds`).
- **Murder:** `Notoriety.RegisterMurder` costs `murderLoss` (300) in the city where the victim fell.
- **Residents:**
  - hurting one (`DamageManager`, damage done) costs `harmResidentLoss` (20), once a minute;
  - killing one (`NPC.On_Death`) costs `killResidentLoss` (150).
- **Drift:** bad standing drifts back toward 0 by `driftPerDay` (50) a real day, worked out when read. Good standing doesn't decay.

## Effects

- **Shops:** the basic vendor, the alchemists and the Prodejce charge the level's price where the player stands, and refuse an enemy. A GM pays list price.
- **Guards:** your own realm's guards stop protecting a player the city counts as Nepritel (`GuardSpeech.IsProtectedBy`). Since guards attack on sight whoever they don't protect, such a player is attacked.
- **A suspect entering a city** hears a guard nearby say "Mam te na ocich, <name>." (or gets a red line if no guard is near). Once in five minutes, as a city is many areas.
- **Citizenship** at a town stone wants Vazeny or better in that city (`Town.TryJoin`).
- **`.povest`** (a player command) lists the six pilot cities, any other city where the player has a standing, and their factions, with level and value. The journal's Mesto tab (step 4) will show it too.

## Tests

`Tests/Protocol/ReputationTests.cs` (server side `scripts/Tests/ReputationTests.cs`, account `reputationtests`):
- **Starting values and levels:** realm starts, word keys, raise, cap, the hero's recent deed, drift and its stop at 0, faction rivalry.
- **Effects:** prices by level, an enemy refused, a GM untouched, guards protecting a known citizen but not the city's enemy.
- **Losses:** a crime counted once a minute; a resident hurt (once a minute) and killed.
- **`.povest`** opened by a plain player.

`TownTests`' qualified citizen now gets a respected name first.
