# Venue life

Phase 3 of the living world (#1237) gives each kind of venue something to do there (design section 9). Code: `scripts/Basic/LivingWorld/VenueLife/`. Step 8 (#1254) builds the shared part and the tavern.

## Venue actions

A venue's people offer extra lines in their dialog (`D_ResidentTalk`, see [errands](errands.md)). They come between the lines of the player's errand steps and **"Potrebujes neco?"**.

Each line is a `VenueAction`, registered from a class's `Bootstrap()` with `VenueActions.Register`. An action has:
- a key and the label the dialog shows;
- the venue types and roles that offer it;
- an optional `Shows` check for whether it's offered right now (the bard sings only in the evening);
- what it does.

A resident offers actions only while standing in their own workplace. A patron met at home has no dice. A button is 50 plus the action's place in the registry, so it names the same action whatever else the dialog showed. On a click, the dialog checks the reach again (as for errands) and checks that the resident still offers that action.

## The tavern

The tavern also covers Mordor's mess hall (`messhall`) where noted.

| Line | Who | What |
|---|---|---|
| Co je noveho? | innkeeper, server, cook, patron (tavern); cook, grunt (mess hall) | a rumour |
| Pokoj na noc | innkeeper | a room for the night |
| Zahrajeme kostky? | innkeeper, patron (tavern); grunt (mess hall) | dice |
| Zahraj neco | the bard, in the evening, while not singing | a song |
| Spropitne | the bard | a tip |

**Rumours.** Half of them are about another resident of the same city: what they're like, by one of their traits, and where to find them. The other half are the region's news. Minas Tirith, Edoras, Hobitin, Barad-dur, Harad and Umbar have their own news; any city also gets a common pool, one time in three. Each resident tells one rumour a minute (`rumourSeconds`). Asked again sooner, they say they have nothing new. This limit is kept in memory and starts afresh with each server start.

**A room for the night.** The room costs `roomPrice`, taken as any payment through `GoldUtils.Pay`, and can be had once a real day. At the player's next login (`Player.On_LogIn`), the night turns into a stored booster of kind `roomBoosterKind` (0 = +25% experience for an hour). The player starts it from the journal's booster tab. A player who has paid and not yet logged in again can't pay twice.

**Dice.** A small dialog (`D_TavernDice`) offers three bets (`betSmall`, `betMedium`, `betLarge`). The stake comes from the pack. Both sides throw 2d6: the higher sum wins double the stake back, and a tie returns it. Each game says the throws overhead, and the dialog comes back for another game. A player has `gamesPerHour` games in a real hour. A GM doesn't play: a GM pays nothing, so a GM's game would make money from nothing. Tests put throws in `Tavern.RollOverride`.

**The bard.** "Zahraj neco" starts one of three songs. The first line comes at once, then one every `songLineSeconds`, overhead (`BardSongTimer`, a periodic timer that deletes itself after the last line). "Spropitne" pays `tip` from the pack. The first tip to a bard in a real day adds 1 to that bard's friendship.

## Settings

`.settings`, **"Hospoda"** (`TavernSettings`):

| Setting | Default | |
|---|---|---|
| roomPrice | 100 | gold for a room |
| roomBoosterKind | 0 | the booster a night gives (Boosters' kinds 0-5) |
| betSmall, betMedium, betLarge | 10, 50, 100 | the dice bets |
| gamesPerHour | 20 | dice games a player has in a real hour |
| tip | 10 | the bard's tip |
| rumourSeconds | 60 | a resident's rumours are this far apart |
| songLineSeconds | 4 | between a song's lines |

## Tests

`Tests/Protocol/TavernTests.cs`, with `scripts/Tests/TavernTests.cs` on the server, sets up a tavern in Edoras in the evening and fills its roles. It then checks:
- **through a client:** the innkeeper's dialog shows the tavern's lines. A click on the rumour gets one told, and a click on the room takes it;
- **on the server:**
  - who offers what, and nothing outside the tavern;
  - the bard only in the evening;
  - rumours about others, in plain ASCII, one a minute;
  - the room's price, once a real day, and the booster from a real `On_LogIn`, only once;
  - dice won, lost and tied, the hourly cap, and no game for a GM;
  - the song starting, and a tip's friendship once a day;
- **later:** a few seconds on, the song has gone on line by line.
