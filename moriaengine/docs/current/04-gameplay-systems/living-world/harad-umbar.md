# Harad and Umbar

Step 22 of the living world (#1237, #1282). Harad and Umbar are cities of Men allied with Mordor. They run on the normal city model
(design section 17, option A: venues, residents, errands and reputation), with their own colour. Code:
`scripts/Basic/LivingWorld/Errands/CityFlavour.cs`, the name lists in `Residents/residents.def`, the two venue types in
`Venues/VenueType.cs`.

## People

- **Names:**
  - Harad's residents get names in the style of the south (`NAMES_HARAD_MALE` / `NAMES_HARAD_FEMALE`: Azrak, Kazim, Amira, Laila...);
  - Umbar's get names of its corsairs and Black Numenoreans (`NAMES_UMBAR_MALE` / `NAMES_UMBAR_FEMALE`: Azrubel, Belzagar, Inzilbeth...).
- **Looks:** Harad's people are dark-skinned, from the darkest of the client's skin hues (1050-1058). The same person keeps the same
  shade each time their body comes back.
- **Greetings:** a stranger is greeted in the city's own voice:
  - Harad: "Pokoj s tebou, poutniku z dalky.";
  - Umbar: "Co chces, suchozemce? Mluv rychle.".
  Friends get the usual words.

## Venues

Two new types for the GMs to mark (`.venue`), both among Mordor's own:

| Type | Roles | Hours |
|---|---|---|
| Karavanseraj (`caravanserai`) | caravan master, 2 drovers, 2 travellers (evening and night guests, no trade) | always |
| Korzarske doupe (`corsairden`) | corsair captain (always there), 3 corsairs (evening and night) | 16-6 |

## Factions

An errand from a working resident (not a guest such as a traveller, patron or slave) also raises the faction of the giver's workplace, by the errand's city reputation gain (a rival faction loses half,
as with any faction, [reputation.md](reputation.md)):

| City | Workplace | Faction |
|---|---|---|
| Harad | caravanserai, market | Vudci karavan (`ha_caravans`) |
| Harad | stable, kennels | Krotitele olifantu (`ha_mumak`) |
| Harad | temple, healing | Chram hada (`ha_serpent`) |
| Umbar | harbour | Pani pristavu (`um_harbour`), rival of the corsair captains |
| Umbar | corsair den, tavern | Korzarsti kapitani (`um_corsairs`) |
| Umbar | market, slave pen | Otrokari a paseraci (`um_fences`) |

Other workplaces, and the other cities, serve no faction this way yet.

## Two errands of their own

| Key | Name | Tier | Where | Steps |
|---|---|---|---|---|
| caravan | Karavana | medium | Harad, from a caravanserai, market or stable worker | Deliver the caravan's cargo (a marked bolt of cloth, not a container, so nothing of the hero's goes with it) to a worker of another trade, then tell the giver. |
| contraband | Kontraband | medium, 40 minutes | Umbar, from a market, slave pen, corsair den or tavern worker | Pass the harbourmaster (or another harbour worker) with the bundle (cloth), then deliver it to the buyer (a worker not of the harbour), then back to the giver. |

**Passing the harbourmaster** is a choice:
- **"Tohle je pro tebe":** a 150 gold bribe, paid through `GoldUtils.Pay` when the errand ends.
  - Paid: the harbour lords think a little better of the hero (+5).
  - Nothing to pay with: they remember (-40).
- **"Nic nevezu":** a denial, which rolls Hiding/10 percent, at least 20. A failure costs 40 with the harbour lords.

These change only the harbour lords' standing and, as with any faction, their rivals' the other way by half: the corsair captains gain
20 when the hero is caught (and lose 2 on a bribe). Umbar's own standing moves only by the errand's usual +10.

Either way the bundle reaches the buyer and the fence pays as usual. The template's own rules are mine; the design named it
(9.3 "Contraband"), Erebor had none.

## Test

`Tests/Protocol/HaradUmbarTests.cs` (server side `scripts/Tests/HaradUmbarTests.cs`, account `haradumbartests`) sets up a caravanserai
and a temple in Harad, a harbour and a market in Umbar, with only the two new errands.

**In the client:** a Mordor hero double clicks the caravan master. The greeting is one of Harad's; the hero asks for work and takes the
Karavana.

**On the server:**
- Harad's and Umbar's names and Harad's dark skins;
- the venue types, the traveller with no trade, the greetings (none of them for Edoras);
- the factions by workplace, none for a guest or a giver gone since;
- the caravan: delivered, reported, city and caravan masters each +10; never offered in Umbar or from a temple;
- contraband (never offered in Harad):
  - a bribe paid (+5 harbour, +10 fences);
  - a bribe with no gold (-40 harbour);
  - a denial with Hiding 100 passing;
  - a denial caught (the roll forced): harbour -40, corsairs +20, the city only +10;
  - the chance's floor of 20, and no errand cargo left in the pack;
- a guest offers no caravan.
