# Errands

Step 6 of the living world (#1237, #1250) builds the engine behind the small dynamic quests residents give (design sections 5 and 8). Code: `scripts/Basic/LivingWorld/Errands/`. The templates themselves arrive in step 7; until then nothing is offered.

## Asking for one

A **double click on a resident** opens their dialog (`D_ResidentTalk`). It shows:
- a greeting that follows friendship: a stranger, a known face at 3+, a friend at 7+;
- one line per errand step the player has with this resident. A choose step shows its two or three answers instead;
- **"Potrebujes neco?"**, which opens an offer (`D_ErrandOffer`: the giver's words, then "Rad pomuzu" or "Ted ne"). If there is no offer, the resident says why;
- **"Sbohem."**

Steps are done only by picking a line in the dialog, never by a typed word (design 5.5).

## What an errand is

An `Errand`, saved on the player in the `_errands_` tag (an `ErrandLog`), holds:
- its template, name, giver, city and tier;
- its steps and the step it is at;
- when it runs out;
- its share of the pay.

| Step kind | Done when, at the step's resident |
|---|---|
| Talk | the player picks the line |
| Deliver | a message: the line. An item (made in the pack when the step begins, marked as the errand's) is taken back |
| Fetch | the pack holds N of an item def; they are taken |
| Collect | a [collector bag](../quests/collectors.md) of a def has N or more; the bag is taken |
| Wait | it is between two game hours (`VenueClock`) |
| Choose | the player picks one of the answers; it stays on the errand for later steps or chains |

## Templates

A template is an `ErrandTemplate` subclass. It registers itself from a class's `Bootstrap` (`ErrandTemplates.Register`), the same way journal pages do. Each template has:
- a key, a name and a tier;
- how many real minutes it lasts;
- a weight;
- `TryBuild(context, errand)`, which fills the steps or says this giver can't offer it here and now.

`ErrandContext` gives the template the giver, the city's other residents (`PickOther`, with a filter) and `Pick` for text pools. `Errands.Talk/Deliver/Fetch/Collect/Wait/Choose` build the steps.

A giver offers one template, drawn by weight among those that can be built for them.

## Limits (`.settings` "Ukoly")

| Setting | Default | Meaning |
|---|---|---|
| maxActive | 3 | errands taken at once |
| perCityPerDay, perGiverPerDay | 6, 1 | errands taken per game day (24 real minutes) in a city and from one resident |
| repeatPenalty, payFloor | 25, 25 | the same template again in a row pays 25% less each time, down to 25% |

The giver won't offer a second errand while the first is open. An errand that runs out is dropped, and its item is taken back, the next time the player's errands are read.

## Pay

The pay is scaled by `(1 + level/50) x (0.5 + 0.25 x steps) x pay share`:

| Tier | Gold | Experience | Friendship | City reputation | Extra |
|---|---|---|---|---|---|
| Small | 60 | 40 | +1 | +5 | none |
| Medium | 150 | 120 | +2 | +10 | a magic pouch, 1 in 3 |
| Large | 400 | 350 | +2 | +20 | a magic bag, and a booster 1 in 4 |

Gold goes to the pack, experience through `LevelingSystem`, friendship to the giver, reputation to the giver's city.

## The journal

The Ukoly tab (section 31) lists each errand with:
- its name and step (1/4);
- the task;
- whom to see and where they are at this hour (their venue from the schedule);
- the minutes left.

Each errand has two buttons: one points the quest arrow (0xBA) at the resident, the other gives the errand up (its item goes back).

## Tests

`Tests/Protocol/ErrandTests.cs` (server side `scripts/Tests/ErrandTests.cs`, account `errandtests`) uses a test template: a letter to a second resident, two apples, come back in the evening, then answer a question. A brewery in Edoras with three residents stands in for the city.

**In the client:**
- double click the giver, ask, see the offer naming the second resident, take it;
- the journal's Ukoly tab shows "(1/4)" and the task;
- the second resident's dialog has the letter's line, and picking it delivers the letter.

**On the server:**
- the giver offers no second one, and the wrong resident can't do the step;
- no apples means no step, and two of three are taken;
- not in the morning, but in the evening;
- the answer is kept;
- done and paid: gold, friendship +2 and reputation +10;
- one a day from a giver, and a new day brings a new one, paying 75%;
- the letter is in the pack, and giving up takes it back;
- an errand runs out;
- the cap on taken errands holds.
