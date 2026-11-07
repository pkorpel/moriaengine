# Residents

Step 2 of the living world (#1237, #1242). A resident is a named person who belongs to a city: a role at a venue
([venues.md](venues.md)), a home, a few traits, and a daily schedule on the game clock. Code: `scripts/Basic/LivingWorld/Residents/`.

## A resident is its spawn gem

Each resident is a world piece: a spawn gem of one (`WorldPieces.Npc`, the gem `.SetupWorld`'s NPCs already use).

**The gem is the person.** The resident's identity is kept as tags on the gem:
- the name;
- the sex;
- the role key;
- the workplace venue's id;
- the home venue's id;
- the traits.

**The NPC is only the body.** `SpawnedPieces.Prepare` calls `Residents.Prepare` for every body the gem spawns. It sets the name, gives the title from the role ("hostinsky"), keeps the body on the gem, and places it by the schedule. A killed resident comes back a minute or two later as the same person.

**Ids are permanent:** `<venue id>/<role>/<n>`, for example `venue/Edoras/1/innkeeper/1`. Filling a venue's roles again only adds the ones missing.

**Who they are, by city:**

| City | Body | Names |
|---|---|---|
| Barad-dur | an orc (`c_resident_orc`) | `NAMES_ORC` |
| Hobitin | a man or woman | the new `NAMES_HOBBIT_MALE` / `NAMES_HOBBIT_FEMALE` |
| Elsewhere | a man or woman (`c_resident_man` / `c_resident_woman`) | Erebor's human lists |

- Names don't repeat within a city when the list allows it.
- Every resident is calm (`AggroMode = Calm`).
- A resident takes its realm from the venue's area.

**Traits:** two of `spolecensky`, `zbozny`, `morous`, `uzvaneny`, `lakomy`, `romanticky`, `zvedavy`, `pracovity`. The schedule reads the first two; errands (step 6) will read the rest.

**Home:** the city's `house` venue with the fewest residents, or none, in which case the workplace is home too.

## Friendship

`Residents.Friendship(player, residentId)` is 0 to 10, kept as a tag on the player (`_rf_<resident id>`), so it outlives the body.
`AddFriendship` clamps the value, and 0 removes the tag. Nothing raises it yet: errands do that from step 6.

## The schedule

`ResidentSchedule.PlaceFor(gem, shift)` decides where a resident should be:
- at work in the role's shifts (an innkeeper morning to evening, the bard and patrons in the evening, a temple's priest always);
- otherwise, in the evening, a sociable resident goes to the city's tavern (Mordor's mess hall) and a pious one to the temple;
- otherwise at home.

**Apply** moves the NPC's home point to the resident's spot in that venue, with a leash of 2 (4 under the open sky). The AI walks there by itself (C1's walk home).

**The body is placed at once** when:
- no player is near (NPC sleep), so an area wakes with everyone in place;
- the body is on another plane;
- it's a fresh body.

**Each resident's spot is fixed:** a standable tile of the venue chosen by its id, so the people of one venue spread over it.

**The scheduler** is a saved timer on Globals, `ResidentSchedulerTimer`, every 20 s, started by the first resident. It looks up each body through the gem's tag rather than searching the map.

## GM tools

- **`.venue`'s dialog:** *Obsadit role (n/m)* fills the venue's missing roles, by the type picked now (it saves first).
- **`.venues obsadit`:** fills every venue of the city you stand in.
- **`.resident`:** with the cursor on a resident, shows the name (editable), role, workplace, home, traits, where they should be now and their friendship with you. *Poslat na misto* puts them there.
- **Deleting a venue** in its dialog removes its residents too.
- **Export and setup:** `.venues export` writes resident lines (`R|id|venue|role|female|name|home|traits`) after the venues. `VenueSetup` brings back the missing ones with the same names, roles, homes and traits, once their venues exist. Their first body is named at once.

## Tests

`Tests/Protocol/ResidentTests.cs` (server side `scripts/Tests/ResidentTests.cs`, account `residenttests`):
- **Filling:** a tavern and a house from Edoras's two biggest rooms. Seven people with names, bodies, titles, the house as home, ids by role, Gondor's realm, calm.
- **Hours:** midday (the innkeeper at work, a patron at home), evening (the patron heads for the tavern, the cook home), night (the innkeeper home, small leash, a fixed spot).
- **Friendship:** 0, raised, capped at 10, back to 0.
- **Death:** a body deleted, and the gem spawns the same person.
- **Export and setup:** removing the venue removes its seven; setup brings back the same seven, named, and a second run adds nobody.
- **Races:** orcs of Mordor in Barad-dur, hobbit names in Hobitin.
- **`.resident` through the client:** the dialog renames the innkeeper.
