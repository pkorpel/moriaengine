# The other cities

Step 32 of the living world (#1237, #1304) is the last of phase 7: design section 13, item 6, "roll out to the other cities".
The pilots were Minas Tirith, Edoras, Hobitin, Barad-dur, Harad and Umbar. Their content now reaches the map's other
cities (`City =` in map.def):

| City key | City | Realm | Folk |
|---|---|---|---|
| `Dol_Amroth` | Dol Amroth | Gondor | Men |
| `Erebor` | Erebor | Gondor | dwarves |
| `Dol` | Dale | Gondor | Men |
| `Fornost` | Fornost | Gondor | Men |
| `Kralovstvi_lesnich_elfu` | the woodland realm | Gondor | elves |
| `Sede_pristavy` | the Grey Havens | Gondor | elves |
| `Nurn` | Nurn | Mordor | Men (the slaves), orcs in its posts |
| `Rhun` | Rhun | Mordor | Easterlings |

Dain's kingdom (`Dainovo_kralovstvi`, the abandoned dwarf city under the mountain, realm 3) gets only the dwarves' names and
greeting, for venues a GM might mark there. London is a test city on map plane 100 and is left out.

## What each city gets

- **Names** (`Residents.NameList` by `CityFlavour.FolkOf`): elves from the converted `NAMES_ELF_*` lists; dwarves from new
  `NAMES_DWARF_*` lists (the old northern lay's dwarf names, Norse-sounding women's names); Easterlings from new
  `NAMES_EASTERLING_*` lists, in `residents.def`. Men keep the human lists. Bodies stay human: the 3.0.6m client has no dwarf or
  elf body.
- **A stranger's greeting** (`CityFlavour.StrangerGreeting`), three lines a city: the swan knights' Dol Amroth, gruff dwarves,
  Dale's bells, Fornost's ruins, the wary woodland elves, the Havens' farewells, Rhun's horse lords, Nurn's fear of the overseers.
- **The tavern's news** (`Tavern.LocalLore`), three lines a city. Erebor and Dain's kingdom share the dwarves' news. Rhun keeps
  to Mordor's side like Barad-dur and Nurn: it never gets the common pool, which is Gondor-side talk.
- **Factions** (`Faction.All`), served by errands from these workplaces (`CityFlavour.FactionOf`, as Harad's and Umbar's):

| Faction | City | Workplaces |
|---|---|---|
| Labuti rytiri | Dol Amroth | barracks, armoury |
| Pristav Dol Amrothu | Dol Amroth | harbour, market |
| Kovari pod horou | Erebor | smithy, forge |
| Kupci Ereboru | Erebor | market |
| Vorari z jezera | Dale | harbour |
| Trh v Dolu | Dale | market |
| Hranicari severu | Fornost | barracks, stable |
| Dvur lesniho krale | woodland realm | tavern, library |
| Stavitele lodi | Grey Havens | harbour |
| Konari z Rhunu | Rhun | stable, kennels |
| Dozorci Nurnu | Nurn | overseer's post |

- **World events:** `WorldEvents.Cities` holds them all, and each kind keeps its own rules. The orc raid and the troll stay on
  Gondor's side (the woodland realm and the Havens included). The caravan goes from any city. The fire and the fever need
  marked venues and residents.
- **Festivals:**
  - midsummer adds Dol Amroth, Fornost and Dale;
  - the harvest adds Dale and Fornost;
  - Yule adds Dol Amroth, Erebor, Dale and Fornost;
  - the day of the Eye adds Nurn and Rhun.
- **`.povest` and the journal:** a city shows once the player has any standing there, as before.

## Still for a GM

As in the pilots, nobody lives in a city until a GM marks its venues (`.venues navrh`, then `.venue`). Then residents come
with the city's names, greetings, news and factions. Erebor's centre lies under the mountain: its venues belong in its halls,
and the test keeps its dwarves only for their names.

## Tests

`Tests/Protocol/OtherCitiesTests.cs` (server side `scripts/Tests/OtherCitiesTests.cs`, account `othercitiestests`):
- **through a client:** a Dol Amroth merchant greets a stranger with Dol Amroth's words;
- **on the server:**
  - the name lists by folk, and an Erebor smithy's people with dwarf names;
  - greetings and news for every new city, plain ASCII; Rhun never gets the common news;
  - factions for every new city, Erebor's smith serving its smiths;
  - the events' city list (not London, not Dain's kingdom); the raid on Gondor's side only; a caravan starting from each new
    city;
  - the festivals' places;
  - standing in Fornost showing in `.povest`'s list.
