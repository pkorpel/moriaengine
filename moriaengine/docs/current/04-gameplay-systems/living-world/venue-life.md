# Venue life

Phase 3 of the living world (#1237) gives each kind of venue something to do there (design section 9). Code: `scripts/Basic/LivingWorld/VenueLife/`. Step 8 (#1254) builds the shared part and the tavern; step 9 (#1256) the bath house, the theatre and Mordor's fighting pits; step 10 (#1258) the armoury, the smithy, the temple and the Houses of Healing; step 11 (#1260) the harbour, the stable, the market, the bakery and the barracks.

## Venue actions

A venue's people offer extra lines in their dialog (`D_ResidentTalk`, see [errands](errands.md)). They come between the lines of the player's errand steps and **"Potrebujes neco?"**.

Each line is a `VenueAction`, registered from a class's `Bootstrap()` with `VenueActions.Register`. An action has:
- a key and the label the dialog shows;
- the venue types and roles that offer it;
- an optional `Shows` check for whether it's offered right now (the bard sings only in the evening);
- what it does.

A resident offers actions only while standing in their own workplace. A patron met at home has no dice. The city's enemy (reputation Nepritel) is offered nothing, as with errands and shops; a GM always is. A button is 50 plus the action's place in the registry, so it names the same action whatever else the dialog showed. On a click, the dialog checks the reach again (as for errands) and checks that the resident still offers that action.

## Shared pieces

- **`VenueGoods.Buy`** (#1258): a resident buys goods from the player's pack for wages: the stoker's logs, the smith's ore. There is a cap of pieces a real day per player and kind of goods, and friendship +1 for the first load of the day. Only goods nobody sells belong here, or the wages could be bought.
- **`VenueLabour.Work`** (#1260): paid work for stamina: the bellows, the docks, the stable, the dough.
  - Each turn takes `labourStamina` and pays `labourWage`.
  - The same kind of work rests `labourMinutes` real minutes, each kind on its own. A player has at most `labourPerDay` turns a real day of all kinds together, so going round four kinds doesn't give four days' wages.
  - The first turn of a day adds 1 to the friendship of the resident it was done for.
- **`VenueStay`** (#1260): staying in a venue for a while (`VenueStayTimer` on the player, checked every 10 s): the temple's vigil, a market stall, the night sentry.
  - Leaving the venue, dying or logging out breaks the stay with no reward.
  - A player has one stay at a time, and each kind once a real day.
  - A player at the door is asked in.

## The tavern

The tavern also covers Mordor's mess hall (`messhall`) where noted.

| Line | Who | What |
|---|---|---|
| Co je noveho? | innkeeper, server, cook, patron (tavern); cook, grunt (mess hall) | a rumour |
| Pokoj na noc | innkeeper | a room for the night |
| Zahrajeme kostky? | innkeeper, patron (tavern); grunt (mess hall) | dice |
| Zahraj neco | the bard, in the evening, while not singing | a song |
| Spropitne | the bard | a tip |

**Rumours.** Half of them are about another resident of the same city: what they're like, by one of their traits, and where to find them. The other half are the region's news. Cities are matched by `FlaggedRegion.City`'s key, such as `Minas_Tirith`.
- Minas Tirith, Edoras, Hobitin, Harad and Umbar have their own news. They also get the common pool one time in three, and a city with no news of its own gets only that pool.
- Mordor's cities (Barad-dur, Nurn, Rhun) share Mordor's news and never get the common pool, which is Gondor-side talk.

Each resident tells a player one rumour a minute (`rumourSeconds`); another player can still ask at once. Asked again sooner, they say they have nothing new. This limit is kept in memory and starts afresh with each server start.

**A room for the night.** The room costs `roomPrice`, taken as any payment through `GoldUtils.Pay`, and can be had once a real day. At the player's next login (`Player.On_LogIn`), the night turns into a stored booster of kind `roomBoosterKind` (0 = +25% experience for an hour; an out-of-range setting is clamped to 0-5). The player starts it from the journal's booster tab. A player who has paid and not yet logged in again can't pay twice.

**Dice.** A small dialog (`D_TavernDice`) offers three bets (`betSmall`, `betMedium`, `betLarge`). The stake comes from the pack. Both sides throw 2d6: the higher sum wins double the stake back, and a tie returns it. Each game says the throws overhead, and the dialog comes back for another game. A player has `gamesPerHour` games in a real hour. A GM doesn't play: a GM pays nothing, so a GM's game would make money from nothing. Tests put throws in `Tavern.RollOverride`.

**The bard.** "Zahraj neco" starts one of three songs. The first line comes at once, then one every `songLineSeconds`, overhead (`BardSongTimer`, a periodic timer that deletes itself after the last line). "Spropitne" pays `tip` from the pack. The first tip to a bard in a real day adds 1 to that bard's friendship.

## The bath house

| Line | Who | What |
|---|---|---|
| Vykoupat se | attendant | a bath |
| Co se povida v pare? | attendant, bathers | a rumour, the same as the tavern's |
| Prinest drevo | stoker | logs for wages |

**A bath** costs `bathPrice` and makes the player **fresh** (Svezest) for `freshMinutes` minutes of play. A player who is already fresh can't pay again. While fresh:
- shop prices in a city are `freshDiscount` percent lower. This is applied in `Reputation.PriceFor` after the reputation's own percent. A shop outside any city has no reputation price, so no discount either, and a price never drops to nothing;
- a finished errand gives its giver 1 more friendship (`Errands.Finish`).

The freshness shows in `.boosts` as an effect with its time left. It counts on the server's clock (`Globals.TimeInSeconds`), which runs while the server is up whether the player is online or not and stops only while the server is down.

**The stoker** buys through `VenueGoods.Buy`, the shared buyer of goods. It takes logs (`i_log`) from the pack and pays `woodWage` for each, up to `woodPerDay` logs a real day. The first load of the day adds 1 to the stoker's friendship. No vendor sells logs; they come only from lumberjacking, so the wages can't be bought.

## The theatre

| Line | Who | What |
|---|---|---|
| Kdy hrajete? | everyone there | when the next play is, that one is on now, or (at dusk, after it) that tonight's is over |
| Muzu hrat v komparzu? | the director, until the player has signed up | an extra's part in the next play |
| Napovez! | the prompter, while an actor has forgotten a line | the prompt |

**The play.** At dusk (19:00 to 21:00 on `VenueClock`), the residents' scheduler (`ResidentSchedule`'s timer, every 20 s) starts a play in each theatre that has a director and a player within 18 tiles. A theatre doesn't play again for `playEveryMinutes` real minutes. A play runs on the director's `TheatrePlayTimer`:
- the director opens it and the actors in the theatre speak its lines overhead, one every `playLineSeconds`;
- at one forgotten line the actor stammers and waits `promptLines` lines for a prompt. With no prompt, the prompter whispers the line and the actor says it;
- at the end the spectators in the theatre clap and the director thanks them.

There are three short plays, each with three speaking parts. A theatre with fewer actors present gives the parts to those it has, and one with none gives them to the director.

**An extra** needs Musicianship `extraMusicianship` (30.0). Anyone in the theatre at the end of the play gets `extraWage` gold, `extraExp` experience and 1 friendship with the director. The list is cleared after each play.

**The prompt.** "Napovez!" opens a dialog of three lines for the forgotten one. The actor says whichever the player picks:
- the right one pays `promptReward` gold and 1 friendship with the prompter;
- a wrong one makes the audience laugh.

Only the first answer counts.

**Errands** (director only):
- **Chybejici herec:** find one of the theatre's actors, wherever their day has taken them, then report back.
- **Zapomenuta rekvizita:** a prop (a script, a crown ring, a goblet) lies in another venue of the city. Bring it to the director. It works like the lost-item errand: the item is the player's alone to pick up.

## The fighting pits (Mordor)

The pitmaster offers **"Vsadit na zapasnika"**. A dialog lists three bets (`pitBetSmall`, `pitBetMedium`, `pitBetLarge`) on each of the pit's two fighters: residents in the fighter role, or two stock names when the pit has fewer. The fight is rolled round by round:
- both fighters roll a d6, and the higher hits for the difference;
- the first to take 10 falls. After 12 rounds the one hurt less wins.

The pitmaster tells the fight overhead. A bet on the winner takes double back, and an even fight returns the stake, so neither fighter is the better bet. The stake comes from the pack. A player has `pitBetsPerHour` bets in a real hour, and a GM doesn't bet. Tests put rolls in `Pits.RollOverride`.

## The armoury

The armoury's life is in its errands, both given by the quartermaster:
- **Rekvizice** (medium): bring two iron weapons that smiths make: kryssy, dyky, dlouhe mece or kopi (`i_kryss_iron` and the rest).
- **Kradez ze skladu** (medium): the stock doesn't add up.
  - The armourer says who borrowed the key: another working resident of the city. Patrons, pilgrims, patients, prisoners and the guard aren't suspects.
  - The player then picks one of two answers at the suspect:
    - let them return it quietly: 100% of the pay, and the thief's friendship +2;
    - turn them in: 125% of the pay.
  - Last, report back to the quartermaster.

Lending gear to the militia during an event waits for the events (phase 6).

## The smithy

| Line | Who | What |
|---|---|---|
| Pomoct u mechu | smith, apprentice | work the bellows |
| Prinest rudu | smith | iron ore for wages |

**The bellows** are labour (`VenueLabour`, kind `smithy`). A player without the stamina is sent to rest.

**Ore:** `VenueGoods.Buy` takes iron ore (`i_ore_iron`) for `oreWage` a piece, up to `orePerDay` a real day. No vendor sells ore.

**Opraveny mec** (small errand, the smith): take a repaired iron long sword to the city's barracks captain or a guard. The sword goes into the pack when the errand is taken. If the errand is given up or runs out, the sword is deleted wherever it went (see [errands.md](errands.md)), so it can't be kept.

## The temple

| Line | Who | What |
|---|---|---|
| Obetovat | priest, acolyte | an offering |
| Nocni bdeni | the priest, at night, unless a vigil is on | a vigil |

**An offering** costs `offeringPrice` and can be made once a real day. It gives `offeringKarma` karma and 1 friendship with that resident.

**A vigil** is a stay (`VenueStay`, kind `vigil`), begun inside the temple at night (`VenueClock`'s night shift). After `vigilSeconds` it ends with `vigilExp` experience, `vigilKarma` karma and the priest's friendship.

**Poutnik** (small errand, a pilgrim): the pilgrim asks the way, for example "Boli me nohy. Kde najdu lecitele?". The player picks one of three venues of the city:
- the right one (the healer's, here) pays 100%;
- a wrong one still ends the errand, for 25%.

A place that would answer the ask as well is never offered as a wrong way: a tavern, bakery and market for food; the barracks and armoury for the guard; a tailor for the smith's buckle; a farm for the stable.

The right answer's place is kept in the choose step's `count`, which a choose step doesn't otherwise use.

The restless ghost waits for the events (phase 6).

## The Houses of Healing

**Obvazat** (patients): needs Healing `bandageHealing` (20.0) and a bandage (`i_bandage`) in the pack. One bandage is used. Once per patient a real day, the house pays `bandageWage` and the patient's friendship goes up by 1. A patient with a fresh dressing says so and uses nothing.

**Horecka** (medium errand, the healer):
1. sit with one of the house's patients;
2. come back to them at night (22:00 to 6:00 on `VenueClock`; earlier, the step is refused);
3. tell the healer.

Gathering herbs for the house is the general ingredients errand.

## The harbour

| Line | Who | What |
|---|---|---|
| Pomoct s vykladkou | harbourmaster, dockers, sailors | labour (kind `harbour`) |

**Errands:**
- **Rybi zakazka** (small; the harbourmaster or a fisher): three big fish (`i_fish_big_1`, from fishing).
- **Pasovani** (medium; the harbourmaster):
  1. A docker says which crate was "heavy as lead", far too heavy for its label (feathers, hay, wool and the like).
  2. The player then names the crate to the harbourmaster, out of three. The right one pays 100%, a wrong one 25%.

The stowaway waits for the events (phase 6).

## The stable

**Pomoct ve stajich** (stablemaster, groom) is labour (kind `stable`). Hay is sold by vendors, so the stable pays for work, never for hay. The runaway horse and the horse race wait for the events (phase 6).

## The market

**Pohlidat stanek** (merchants, by day) is a stay (kind `stall`): `stallSeconds` at the market, then `stallWage` and the merchant's friendship.

**Falesne mince** (medium errand, a merchant):
1. The crier names the three people who paid today (three different names), and says whose coins shone too much.
2. The player names that one to the merchant. The right one pays 100%, a wrong one 25%.

The crier's calls come in step 12, and the travelling merchant with the events.

## The bakery

**Pomoct s testem** (baker) is labour (kind `bakery`).

**Ranni roznaska** (medium errand, the baker): bread (`i_bread_loaf`) to three residents of the city, each a deliver step, in 20 minutes ("before it cools"; the template's `Minutes`). The errand's loaf never stacks with the player's own bread (see [errands.md](errands.md)). Eaten, it is gone, and the errand can only be given up.

## The barracks

| Line | Who | What |
|---|---|---|
| Strelba do terce | captain, guards | target practice |
| Nocni hlidka | guards, at night | a stay (kind `sentry`) |

**Target practice:** five shots. Each hits when a roll of 0-999 comes under the player's Archery (in tenths), and each hit gives `archeryExp` experience. A player with no Archery is turned away. There is a round every `archeryMinutes` real minutes.

**The night sentry:** `sentrySeconds` at the barracks, then `sentryWage`, `sentryReputation` with the city, and the guard's friendship. The reputation doesn't count as a hero's deed (`Reputation.Change`'s `deed` false). It is given by a guard, as the captain works by day.

**Prvni hlidka** (medium errand, the captain): a recruit walks with the player (the errand engine's companion) to one of the city's working people, then back to the captain.

## Settings: "Podniky"

`VenueLifeSettings`:

| Setting | Default | |
|---|---|---|
| bathPrice, freshMinutes, freshDiscount | 20, 30, 5 | a bath, how long it lasts, the shops' percent off |
| woodWage, woodPerDay | 3, 30 | the stoker's pay per log and the daily cap |
| playLineSeconds, promptLines | 5, 2 | between a play's lines; how long an actor waits for a prompt |
| playEveryMinutes | 20 | real minutes between two plays in one theatre |
| extraWage, extraExp, extraMusicianship | 40, 60, 300 | an extra's pay and the Musicianship they need (tenths) |
| promptReward | 25 | gold for the right prompt |
| pitBetSmall, pitBetMedium, pitBetLarge, pitBetsPerHour | 10, 50, 100, 10 | the pits' bets and hourly cap |
| oreWage, orePerDay | 4, 20 | the smith's pay per ore and the daily cap |
| labourStamina, labourWage, labourMinutes, labourPerDay | 10, 5, 5, 12 | a turn of labour: stamina, pay, rest per kind, turns a day of all kinds |
| offeringPrice, offeringKarma | 25, 50 | a temple offering |
| vigilSeconds, vigilExp, vigilKarma | 120, 100, 50 | a night vigil |
| bandageHealing, bandageWage | 200, 10 | dressing a patient (Healing in tenths) |
| stallSeconds, stallWage | 120, 30 | tending a market stall |
| sentrySeconds, sentryWage, sentryReputation | 120, 30, 2 | a night sentry |
| archeryExp, archeryMinutes | 5, 30 | target practice: exp a hit, minutes between rounds |

## Settings: "Hospoda"

`TavernSettings`:

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
  - rumours about others, in plain ASCII, each city's news (Mordor only its own), one a minute;
  - the room's price, once a real day, and the booster from a real `On_LogIn`, only once;
  - dice won, lost and tied, the hourly cap (a new hour starts afresh even when its first bet can't be paid), and no game for a GM;
  - the song starting, and a tip's friendship once a day;
- **later:** a few seconds on, the song has gone on line by line.

`Tests/Protocol/VenueLifeTests.cs`, with `scripts/Tests/VenueLifeTests.cs`, sets up a bath house, a theatre and a pit in Edoras. It then checks:
- **through a client:** the attendant's dialog lines and a bath;
- **on the server:**
  - the bath: who offers what, its price, once while fresh, `.boosts`, cheaper shops, the warmer errand;
  - the stoker's wages and daily cap;
  - the extra's Musicianship;
  - three plays stepped by hand: a wrong prompt, the right prompt, no prompt, and the extra's pay;
  - a play at dusk only, once;
  - both errand templates;
  - the pit's winning, losing and even bets, its cap, and no bets for a GM;
- **later:** the scheduler starts a play by itself and it runs to its end on its real timer.

`Tests/Protocol/WorkVenueTests.cs`, with `scripts/Tests/WorkVenueTests.cs`, sets up an armoury, a smithy, a temple, Houses of Healing and a barracks in Edoras. It then checks:
- **through a client:** the smith's dialog lines and a turn at the bellows;
- **on the server:**
  - who offers what;
  - the bellows' stamina, rest, pay and daily cap, and the ore cap;
  - an offering once a day;
  - a vigil at night only, run to its end, and broken by leaving;
  - dressing a patient: the skill, a bandage, once a day;
  - all five errands, each asked only by its role: the requisition, both endings of the theft (compared as pay shares), the blade going to the barracks and a given-up one deleted from a chest on the ground, the pilgrim's right and wrong ways (never a sensible place called wrong), and the fever night.

`Tests/Protocol/TradeVenueTests.cs`, with `scripts/Tests/TradeVenueTests.cs`, sets up a harbour, a stable, a market, a bakery and a barracks in Edoras. It then checks:
- **through a client:** the harbourmaster's dialog line and a turn of unloading;
- **on the server:**
  - who offers what;
  - the city's enemy offered nothing;
  - each kind of labour resting on its own, and the daily cap covering all kinds;
  - the stall by day and the sentry by night;
  - a stall paid at its end, once a day, one stay at a time;
  - a sentry broken by leaving, then its pay and reputation, not a deed;
  - target practice by Archery, with its rest;
  - all five errands, each only from its role: the fish, the two puzzles (the hint names the answer, the right one pays in full, three distinct payers), the bread round's three homes in 20 minutes with its loaf kept apart from the player's own, and the recruit walking the patrol.
