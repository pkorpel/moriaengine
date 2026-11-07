# Errands

Step 6 of the living world (#1237, #1250) builds the engine behind the small dynamic quests residents give (design sections 5 and 8). Code: `scripts/Basic/LivingWorld/Errands/`. Step 7 (#1252) adds the first eight templates, listed below.

## Asking for one

A **double click on a resident** opens their dialog (`D_ResidentTalk`). It shows:
- a greeting that follows friendship: a stranger, a known face at 3+, a friend at 7+;
- one line per errand step the player has with this resident. A choose step shows its two or three answers instead;
- what the resident's venue offers, such as a tavern's rumours, room and dice (#1254, see [venue-life.md](venue-life.md));
- **"Potrebujes neco?"**, which opens an offer (`D_ErrandOffer`: the giver's words, then "Rad pomuzu" or "Ted ne"). If there is no offer, the resident says why;
- **"Sbohem."**

Steps are done only by picking a line in the dialog, never by a typed word (design 5.5). The player must be alive, within 3 tiles of the resident and see them, both to open the dialog and when a button is pressed. A button names its errand by the errand's number (`Errand.id`, given when taken), not by its place in a list, so a list changed while the dialog was open can't send it to another errand. The offer travels in its own dialog's args, and a new offer closes the old one.

## What an errand is

An `Errand`, saved on the player in the `_errands_` tag (an `ErrandLog`), holds:
- its template, name, giver, city and tier;
- its steps and the step it is at;
- when it runs out;
- its share of the pay.

| Step kind | Done when, at the step's resident |
|---|---|
| Talk | the player picks the line |
| Deliver | a message: the line. An item (made in the pack when the step begins, marked as the errand's) is taken back. When the errand ends any other way, the item is deleted wherever it went: worn, dropped, in a chest (#1258; before that only the pack and bank were searched, so an errand's sword could be kept). It never stacks with the player's own items of its kind (#1260, `Item.CanStackWith`) |
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

## The eight templates (#1252)

`scripts/Basic/LivingWorld/Errands/BasicErrands.cs` holds design 9.1's first eight, with Czech text pools. The player's lines are worded so they fit a woman or a man, and residents' own lines follow their gender (`BasicErrands.Sex`).

| Key | Name | Tier | Steps |
|---|---|---|---|
| letter | Dopis | small (medium with a reply) | Deliver a letter to another resident, then tell the giver. One time in three the answer goes back to the giver as a second letter. |
| askaround | Vyptavani | medium | News of someone: two or three residents point onward, the last one knows, then back to the giver. |
| market | Nakup | small | Bring N of a shop good: bread, apples, cheese, wine or candles. |
| ingredients | Suroviny | medium | By the giver's venue: ingots (smithy, armoury, forge), raw fish (tavern, messhall, bakery), ginseng (healing, temple), logs (harbour, stable, farm), apples (brewery). Other venues don't offer it. |
| lost | Ztracena vec | small | The giver's lost ring, necklace, book or hammer lies at a spot in another resident's venue (`ErrandStep.venueId`). Pick it up and bring it back. Giving up or running out clears it away. |
| debt | Dluh | medium | At the debtor, choose one of three answers. Ask to pay: full pay. Forgive: half pay, and the debtor's friendship +2. Threaten: 120%, the debtor's friendship -2 and karma -100. Then back to the giver. |
| walk | Doprovod | medium | An old resident (scholar, librarian, priest, pilgrim, patient, farmer, patron) follows the player (`ErrandStep.companionId`, `FollowTarget`) to a worker at another venue, and must be within 6 tiles there. When the errand ends they go back to their day; the schedule leaves a follower alone meanwhile. |
| shift | Smena | small | A worker hands out mugs (tavern), bread (bakery, market) or papers to up to three people of their own venue, then pays wages. |

Templates can scale their pay (`PayPercent`) and act at the end (`OnDone`), as the debt does.

**The lost item:**
- Only its errand's player can pick it up (a GM can too). Others are told "To hleda nekdo jiny."
- When the errand ends, the item goes wherever it is: pack, worn, bank, another's pack.
- The template offers it only in a venue with a spot to lie on.

**A follower:** the resident schedule lets a follower go once no running errand of that player still needs them. That covers a player who is offline or deleted, and an errand that ended or ran out, even if nobody reads the player's errands again. A GM's "come" is untouched.

**Other details:**
- A step's companion is checked before anything is taken or spent.
- An errand's own items are taken back from the bank too.
- Friendship is floored at 0, so a threatened stranger can't fall further.
- Only working people give ingredients; guests have no trade.
- A shift goes to the venue's working people, in a random order.

## Limits (`.settings` "Ukoly")

Both `.settings` categories, "Ukoly" and #1243's "Povest", are now in the settings dialog.

| Setting | Default | Meaning |
|---|---|---|
| maxActive | 3 | errands taken at once |
| perCityPerDay, perGiverPerDay | 6, 1 | errands taken per game day (24 real minutes) in a city and from one resident |
| repeatPenalty, payFloor | 25, 25 | the same template again in a row pays 25% less each time, down to 25% |

The giver won't offer a second errand while the first is open. Nobody offers errands to the city's enemy (reputation Nepritel): an enemy earns no standing this way, and the shops refuse them too. The repeat penalty counts the same template done in a row and also the same one held now, so holding three at once doesn't avoid it. An errand's time runs from when it's taken. An errand that runs out is dropped, and its item is taken back, the next time the player's errands are read.

A fetch never counts an errand's own item, such as a letter to deliver. A collect step takes a bag in the pack that holds enough, not merely the first one found. A deliver step whose item def doesn't exist becomes a message, so it can still be done.

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
- beside the second resident, their dialog has the letter's line, and picking it delivers the letter.

**On the server:**
- the giver offers no second one, and the wrong resident can't do the step;
- no apples means no step, and two of three are taken;
- not from 8 tiles away;
- not in the morning, but in the evening;
- the answer is kept;
- done and paid: gold, friendship +2 and reputation +10;
- one a day from a giver, and a new day brings a new one, paying 75%;
- the letter is in the pack, and giving up takes it back;
- the city's enemy gets no errand;
- errands are numbered, and found by their number;
- an errand runs out;
- the cap on taken errands holds.

**Known gaps:** a game day for the limits is the server clock / 24 minutes, which isn't aligned with LightAndWeather's dawn. Expiry runs on real time, so it keeps running while the server is down.

`Tests/Protocol/ErrandTemplateTests.cs` (server side `scripts/Tests/ErrandTemplateTests.cs`, account `errandtpltests`) sets up a tavern, a smithy and a temple in Edoras. It checks:
- all eight are registered, and the innkeeper offers one to a client;
- each template's steps, and that its texts are plain ASCII;
- the lost item lies in the venue, only its player may pick it up, bringing it back ends the errand, and giving up clears it even from the bank;
- the debt's three answers (pay, friendship, karma);
- the walk: the follower, no ending without them, back to their day, and let go by the schedule once no errand holds them;
- a guest has no trade to supply.
