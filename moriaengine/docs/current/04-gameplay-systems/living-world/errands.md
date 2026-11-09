# Errands

Step 6 of the living world (#1237, #1250) builds the engine behind the small dynamic quests residents give (design sections 5 and 8). Code: `scripts/Basic/LivingWorld/Errands/`. Step 7 (#1252) adds the first eight templates, listed below.

## Asking for one

A **double click on a resident** opens their dialog (`D_ResidentTalk`). It shows:
- a greeting that follows friendship: a stranger, a known face at 3+, a friend at 7+;
- one line per errand step the player has with this resident. A choose step shows its two or three answers instead;
- what the resident's venue offers, such as a tavern's rumours, room and dice (#1254, see [venue-life.md](venue-life.md));
- **"Potrebujes neco?"**, which opens an offer (`D_ErrandOffer`: the giver's words, then "Rad pomuzu" or "Ted ne"). If there is no offer, the resident says why;
- **"Sbohem."**

Steps are done only by picking a line in the dialog, never by a typed word (design 5.5). The player must be alive, within 4 tiles of the resident and see them (3 until #1312: residents wander while talked to, and a player one step behind was out of reach), both to open the dialog and when a button is pressed. A button names its errand by the errand's number (`Errand.id`, given when taken), not by its place in a list, so a list changed while the dialog was open can't send it to another errand. The offer travels in its own dialog's args, and a new offer closes the old one.

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
| Fetch | the pack holds N of an item def; they are taken. The task names the def in brackets, as a single item shows it ("20 polen dreva (log)"), and the offer adds "Prines: 20x log" (#1312: only that def counts, "polena" could have meant any wood) |
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

## The talking templates (#1291)

Code: `scripts/Basic/LivingWorld/Errands/TalkErrands.cs`. These are design 9.1's numbers 11, 12, 17, 20, 21, 22, 25 and 26.

| Template | Key | Who gives it | Steps |
|---|---|---|---|
| Hadka (the quarrel) | `quarrel` | anyone | hear the other neighbour, then decide back at the giver |
| Tajny ctitel (the secret admirer) | `admirer` | anyone | deliver a bottle of wine; the receiver asks who sent it; report back |
| Pozvanka (the invitation) | `invitation` | anyone | invite three residents; one comes only if you bring wine, bread or cheese; report back |
| Milostny dopis (the love letter) | `loveletter` | anyone | a jealous rival stops you first; deliver the letter; report back |
| Vypujcene knihy (return the books) | `books` | the library | two borrowers, each with an excuse; the pile (a book) back to the giver |
| Znameni (the omen) | `omen` | an elderly woman | bring three odd things: garlic, spider silk, black pearl, mandrake root or a feather |
| Pivo pro strazne (ale for the guards) | `ale` | a tavern, brewery or mess hall | a mug of ale to a guard, within 15 minutes |
| Scitani (census) | `census` | the town hall | ask households of three or four other workplaces, then bring the count back |

**What the choices do**, on top of the giver's usual friendship for a finished errand:
- **The quarrel:** deciding for the giver: +2 giver, -1 the other. For the other: -1 giver, +2 the other. Both a bit right: +1
  each.
- **The admirer:** keeping the secret +1 with the giver; naming the giver -2.
- **The love letter:** refusing the rival +1 with the giver. Selling the letter out pays the rival's 50 gold on top, and costs 3
  with the giver and 1 with the receiver.

The war camp's residents give orders, not these, except the ale ([war-camp.md](war-camp.md)).

## The doing templates (#1293)

Code: `scripts/Basic/LivingWorld/Errands/DoingErrands.cs`, design 9.1's numbers 7, 10, 23, 24 and 27, on the engine's own
steps. The ones with things in the world (the runaway, the lamplighter, the cellar, stop thief, the missing shipment, press
gang) follow in step 27b.

| Template | Key | Who gives it | Steps |
|---|---|---|---|
| Zakazka (a commission) | `commission` | any worker | bring a dagger, a shirt or a loaf, made or bought |
| Kuryr (the courier race) | `courier` | any worker (less often than the rest) | a sealed package to a worker in another city of the giver's realm; medium, 30 minutes |
| Slova mrtvych (words for the dead) | `ghostwords` | the temple's priest (the one there at night), to a player with Spirit Speak 30.0 or more | at night (22-4) the priest feels a spirit; carry its message to a living relative |
| Rvacka (the brawl) | `brawl` | a tavern worker | split two brawlers, or bet 50 gold on one; report back |
| Cerny pasazer (the stowaway) | `stowaway` | a harbour worker | find the stowaway; hand them over or let them go; report back |

- **The courier:** arriving with half the time or more left pays 130%. It's medium, not large: a travel stone makes it quick, and
  a large errand's magic bag isn't scaled by the pay.
- **The brawl:** splitting the brawlers gives +1 with the innkeeper. A bet is paid at the end through `GoldUtils.Pay`, comes
  back double half the time, and the innkeeper pays nothing for it (-1 with them). Without the money, nobody takes the bet. A
  GM can't bet: a GM pays nothing, so it would be free gold.
- **The stowaway:** handing them over gives +1 with the giver. Letting them go gives -1 with the giver, +2 with the stowaway,
  and half the pay.

The war camp gives orders, not these.

## Things in the world (#1293)

**A Things step** puts NPCs or items in the world as it begins: in a venue, in the streets a few steps from the city's centre,
or on a road out of the city, beside the player if no spot is found. "The city" is the town the giver works in: a city of several
towns on the map (Dol Amroth with Pelargir and Linhir, the Shire's, Dale with Esgaroth, the woodland realm with Lorien) centres
the streets and the road on the smallest of its areas holding the giver's workplace (#1317; before, a Pelargir harbourmaster's
spies stood in Dol Amroth, 600 tiles away). The player then catches, kills or uses them:
- **Catch** an NPC with a double click right beside it. Such NPCs can't be hurt, so nobody can strike one down and leave the
  step undoable.
- **Use** an item with a double click within 2 tiles, on the same floor and in sight.
- **Kill** an NPC, by the player or with the player among its fighters. A kill by anyone else alone doesn't count, and
  another takes its place, but only twice the step's count over (#1300); after that, anyone's kill counts for the player, so
  a friend can't farm a road of thieves for the errand's whole time.

They're the errand's player's alone: anyone else's double click is told it's none of their business. A thing that flees runs
from the player each second when they come within 3 tiles, three times; then it tires and can be caught. When the last one is
done, the step is done.

**Clean-up:** the things go with the errand: done, given up or run out. Each also checks every minute, and goes when its
errand is over, its step passed, or its player gone, even after a restart. A thing is only ever found by its uid together
with its errand's mark, so a uid freed by a caught or killed thing and reused for anything else is never touched.

**In the journal**, a Things step shows its place (the venue, the streets, the road out), and the arrow points to the
nearest of its things. Code: `Errands.Things(...)`, `Errands.UseThing`, `Errands.OnThingDeath`; a template's
`OnThing` decides what a catch or a use does (by default the thing goes).

Templates (`ThingErrands.cs`, design 9.1's numbers 4, 13, 14, 16, 28 and 29):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Uteklo (the runaway) | `runaway` | any worker | the giver's cat hides in another venue; caught the first time, she slips away to another and stays there; then back to the giver |
| Lampar (the lamplighter) | `lamplighter` | any worker | light 4 lamps in the streets within 15 minutes; a lit one stays lit until the errand ends |
| Krysy ve sklepe (the cellar) | `cellar` | a tavern, brewery, bakery, market or house worker | kill 3 rats in the giver's venue |
| Zlodej (stop thief) | `thief` | any worker | catch the fleeing pickpocket in the streets (he tires after three runs) and bring the purse back |
| Ztracena zasilka (the missing shipment) | `shipment` | a market, caravanserai, harbour or tailor worker | search the overturned cart on the road out of the city, then carry the cloth back |
| Verbovani (press gang) | `pressgang` | the war camp's mess hall or barracks only | drag 3 drunk orcs out of the mess hall |

The war camp gives orders, not these, except press gang, which is its own.

## Regional stories (#1300)

Design 9.3's story quests, as templates only their city's people give (`RegionalStories.cs`, things in `regionalstories.def`):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Hobiti posta (the hobbit post) | `hobbitpost` | any worker in Hobitin | three letters to three other residents within 20 minutes; each round raises the post rank |
| Zlodeji koni (horse thieves) | `horsethieves` | a stable, barracks, town hall or market worker in Edoras | kill the 2 thieves on the road out of the city, catch the 2 horses they left there, then report |
| Ztracena prilba (the Citadel helm) | `citadelhelm` | a guard or recruit in Minas Tirith | find the helm he left in one of the city's taverns (the streets if it has none), then bring it back, within 30 minutes |
| Dopisy elfu (the elves' letters) | `greyhavens` | any worker in Hobitin | put the letters into the elves' chest at the Grey Havens within 60 minutes (a large errand) |

**The post's ranks:** the completed rounds are counted in a player tag. The ranks are Postak (from 0 rounds), Starsi postak
(5), Postmistr (15) and Mistr posty (40). A promotion is told to the player. Each rank adds 15% to the round's pay share, by
the rank held as the round is done (`ErrandTemplate.PayPercent(player, errand)`).

**Things in another city:** a Things step can name a city other than the errand's (`ErrandStep.thingCity`, the builder's
`inCity`). Its streets or road things go there, and the journal names that city. A template placing things far away
checks first that a spot exists (`Errands.CanPlace`), or it isn't offered: a thing with no spot would stand beside the player. The Grey Havens have no residents, so the
elves' letters are given in Hobitin and their chest stands about 330 tiles west, at Sede_pristavy. The errand ends at the
chest, so the thanks come as a message.

## The remaining cities' stories (#1317)

The rest of design 9.3, one region at a time, on the same machinery.

**The Shire** (`ShireStories.cs`, things in `shirestories.def`):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Zlodeji hub (mushroom thieves) | `mushrooms` | a farm, market, tavern, bakery or brewery worker in Hobitin | catch the 3 young hobbits who flee on the road out of town, then tell the giver: turned in (full pay) or let off with a share of the mushrooms (half the pay and 6 mushrooms) |
| Narozeninova oslava (the birthday party) | `birthday` | any worker in Hobitin | two invitations, fetch the fireworks from a third resident and bring them, a cake (`i_cake`, cooked), then come in the evening (19-23); 3 hours. The party: the host's fireworks, and every player within 12 tiles and in the host's sight is a guest (150 exp, once in 24 real hours each: a game day is only 24 minutes), the errand's player included |
| Stribrne lzicky (the silver spoons) | `spoons` | any worker in Hobitin | catch the relative in the streets (she flees), pick up the 3 spoons she drops there, bring them back |

**Fornost** (`FornostStories.cs`, things in `fornoststories.def`):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Probuzena mohyla (the barrow at night) | `barrow` | a barracks, stable or town hall worker in Fornost | come back after dark (22-4), lay the barrow wight on the road out, find the old blade it guarded by the road, bring it back; 2 hours |
| Hlidka hranicaru (the Rangers' watch) | `rangerwatch` | a barracks or stable worker in Fornost | offered from Tracking 30.0; read the 3 orc tracks on the road out (below it a track stays unread), each saying which way they went, then tell the giver: the right way pays, a wrong one nothing (and no friendship) |

**Edoras** (`EdorasStories.cs`, things in `edorasstories.def`; the horse thieves are step 30's):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Sraz eoredu (the eored's muster) | `eored` | a stable or barracks worker in Edoras | ride to the eored's 3 banners on the road out within 20 minutes, mounted at each (on foot a banner doesn't count), then report |
| Pisen pro sin (a song for the hall) | `hallsong` | a tavern or town hall worker in Edoras, offered from Musicianship 30.0 | learn the old verse from another resident, then play it at the bard's stool in the giver's venue (an instrument in the pack and Erebor's Musicianship roll, `Bard.Plays`; a poor try can be played again after 3 s), then hear the giver's thanks |

**Minas Tirith** (`MinasTirithStories.cs`, things in `minastirithstories.def`; the beacons are the world event, the Citadel helm step 30's):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Zapomenuta bylina (the forgotten herb) | `forgottenherb` | the healer or herbalist of the Houses of Healing in Minas Tirith | ask an old resident (an elder if the city has one), who remembers the rhyme (kingsfoil, athelas), gather 3 plants on the road out, bring them |
| Zahada v archivu (the Steward's archives) | `archives` | a librarian, scholar or scribe in Minas Tirith | search the 3 dusty crates of records in the giver's venue (the last holds a note naming who borrowed the missing page), get the page from that resident (a worker), return it to the giver |

**Dol Amroth, Pelargir and Linhir** (one city on the map; `DolAmrothStories.cs`, things in `dolamrothstories.def`):

| Template | Key | Who gives it | What |
|---|---|---|---|
| Turnaj labutich rytiru (the swan knights' tourney) | `swantourney` | a barracks or armoury worker in Dol Amroth, Pelargir or Linhir | ride at the 3 quintains in the streets, mounted and with a weapon in hand (a tool such as a pickaxe counts, as in combat; otherwise a run doesn't count), then report |
| Spehove korzaru (corsair spies) | `corsairspies` | a harbour or barracks worker in Dol Amroth | catch the 2 spies in the streets (they run), then bring the giver the letter for Umbar one carried |
| Majak bez strazce (the lighthouse keeper fell ill) | `lighthouse` | a harbour worker in Dol Amroth | come in the evening (18-23), light the lighthouse fire on the road out, bring 5 ginseng for the keeper; 4 hours |

## Mordor's stories (#1302)

Design 9.4's, in `MordorStories.cs` (things in `mordorstories.def`). The two in Barad-dur are orders, under the war camp's rules
([war camp](war-camp.md)): rank caps, shorter deadlines, a cost for refusing or failing.

| Template | Key | Who gives it | What |
|---|---|---|---|
| Jamy Nurnu (the pits of Nurn) | `nurnquota` | an overseer's post in Nurn | catch the 3 runaway slaves in Nurn's streets (they flee and can't be hurt), then hand them all back, or say one ran |
| Speh v taboru (the Eye's spies) | `eyespy` | an overseer's post in the war camp | ask two witnesses, then name the Gondor spy among three of the camp's residents |
| Rozkazy Usta (the Mouth's dispatches) | `dispatches` | any worker in the war camp | put the sealed orders into the box at Minas Morgul's tower (60 minutes, halved as an order) |

- **Letting a slave go** pays half. It raises karma by 50 and counts the slave in a player tag.
  - One time in four the overseer finds out: -60 standing in Nurn, -50 net with the errand's own +10, so freeing slaves
    costs standing on average. Since #1304 the overseers' faction (`nu_overseers`, which these errands serve) loses as much.
  - Every fifth slave freed leaves a magic pouch in the pack, with a note. The medium errand's own pouch chance still applies.
  - A GM's choice changes nothing.
- **The spy's clues:** the first witness says where the spy works; the second says the first letter of the spy's name. Each
  answer names a suspect with their workplace. The two decoys never match both clues and never share a name with the spy or
  each other, so the clues single the spy out.
  - Naming the right one pays.
  - A wrong one pays nothing, costs the giver's favour (-1), and costs the camp's standing as a failed order would
    (`orderFailLoss`, one change).
- **An errand that earns nothing** (pay share 0) now gets nothing at all from `Errands.Finish`: no pouch, bag or booster, no
  standing (and no hero's deed mark), and no courier deed. The message says it ended without a reward. The brawl's bet (share 0)
  follows the same rule now.
- **The dispatch** uses the other-city things of the regional stories. The errand ends at the box, so the thanks come as a message.

Not built (design 9.4): the warg pup, Black Speech.

Not built yet (design 9.3, being added under #1317): the other stories (the woodland realm, Erebor, Esgaroth, Umbar and Harad, Rhun, the wild).

## Limits (`.settings` "Ukoly")

Both `.settings` categories, "Ukoly" and #1243's "Povest", are now in the settings dialog.

| Setting | Default | Meaning |
|---|---|---|
| maxActive | 3 | errands taken at once |
| perCityPerDay, perGiverPerDay | 6, 1 | errands taken per game day (24 real minutes) in a city and from one resident |
| repeatPenalty, payFloor | 25, 25 | the same template again in a row pays 25% less each time, down to 25% |

The giver won't offer a second errand while the first is open. Nobody offers errands in the enemy realm's towns (#1312: a Gondor player in Barad-dur, Harad or Umbar and Mordor's in Minas Tirith, Edoras or Hobitin; `Reputation.IsEnemyCity`, outcasts and those of no realm aren't anyone's enemy here), nor to the city's enemy (reputation Nepritel): an enemy earns no standing this way, and the shops refuse them too. The repeat penalty counts the same template done in a row and also the same one held now, so holding three at once doesn't avoid it. An errand's time runs from when it's taken. An errand that runs out is dropped, and its item is taken back, the next time the player's errands are read.

Harad and Umbar add two templates of their own (`caravan` and `contraband`), and their errands also serve the faction of the giver's workplace: see [harad-umbar.md](harad-umbar.md) (#1282). In Barad-dur errands are orders: [war-camp.md](war-camp.md).

A fetch never counts an errand's own item, such as a letter to deliver, nor a magic item (#1293: a commission for a dagger
mustn't take the player's magic one). A collect step takes a bag in the pack that holds enough, not merely the first one found. A deliver step whose item def doesn't exist becomes a message, so it can still be done.

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

Each errand has two buttons: one points the quest arrow (0xBA) at the resident, the other gives the errand up (its item goes back) after a confirm dialog (#1312: one click gave it up by accident; "Ne" shows the journal again). `.arrowend` (Erebor's, a player command) takes the arrow away.

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

`Tests/Protocol/RegionalStoryTests.cs` (server side `scripts/Tests/RegionalStoryTests.cs`, account `storytests`) sets up open-air venues in Hobitin, Edoras and Minas Tirith:
- **through a client:** the elves' letters are taken from a Hobitin innkeeper. The chest stands at the Grey Havens, the journal names them, a double click on the chest finishes the errand, and it pays;
- **on the server:**
  - the post: three letters to three others, 20 minutes, Hobitin only; a round counted, the rank reached with its share, and a ranked round paying more (at the same level);
  - the horse thieves: only from the right venues; the thieves can be hurt and are killed, the horses can't be hurt and are caught and go home; the report ends it;
  - the helm: guards only; it lies in the tavern; found, returned, 30 minutes;
  - the letters: Hobitin only.

`Tests/Protocol/MordorStoryTests.cs` (server side `scripts/Tests/MordorStoryTests.cs`, account `mordorstorytests`) sets up overseer posts in Nurn and the camp, a mess hall in each, and a forge in the camp:
- **through a client:** the dispatch is taken as an order from the camp's overseer ("Rozkaz?"). The box stands at Minas Morgul, the journal names it, a double click on the box finishes the order, and it pays;
- **on the server:**
  - the slaves: in Nurn, they flee and can't be hurt; only Nurn's overseers give the errand;
  - all slaves handed back, or one let go for half the pay and karma; found out when the roll says so; a gift on the fifth;
  - the spy: only the camp's overseers give it; over ten builds, the clues fit exactly the right suspect;
  - the right spy pays; a wrong one pays nothing, costs standing, gives no pouch, counts no deed and doesn't renew the hero's mark;
  - the dispatches come from the camp only.
