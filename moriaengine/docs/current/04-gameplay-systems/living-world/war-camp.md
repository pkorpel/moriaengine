# Barad-dur's war camp

Step 21 of the living world (#1237, #1280). The design's section 17 runs Barad-dur as option B: **orders, not favours**. It uses the
same engine as the other cities (venues, residents, errand templates, reputation) under the camp's own rules. Code:
`scripts/Basic/LivingWorld/WarCamp/` (`WarCamp.cs`, `RivalCaptains.cs`, `warcamp.def`). Numbers: `.settings` "Tabor".

## Rank

Barad-dur's city reputation ([reputation.md](reputation.md)) is read as a rank in the camp:

| Reputation level | Rank | Biggest order |
|---|---|---|
| Nepritel | Zradce | none (the enemy gets no orders) |
| Podezrely | Cerv | small |
| Neznamy | Rekrut | small |
| Znamy | Vojak | medium |
| Vazeny | Desatnik | medium |
| Cteny | Setnik | large |
| Hrdina | Nacelnik | large |

A Mordor hero starts as Vojak. Gondor's start of -600 makes them Zradce.

- **Promotion:** for Mordor's own, a rise to a rank never reached before pays `promotionGold` (300) times the rank's number (Vojak 3
  ... Nacelnik 6), once per rank. The hero is told "Byl jsi povysen: ...". A loss never pays, whatever drift or a GM did before it.
- **Demotion:** a fall is told; it costs nothing more.
- **Where it shows:** `.povest` and the journal's Mesto page show the rank in place of Barad-dur's level. The journal page adds the
  captain the hero serves, and calls the residents' friendship "Prizen" (favour).

## Orders

In Barad-dur a resident's errands ([errands.md](errands.md)) are orders:

- **The dialog:**
  - The resident's dialog asks "Rozkaz?" instead of "Potrebujes neco?".
  - The greeting follows the superior's favour (the resident's friendship) and the hero's rank: "Co tu stojis, vojak? Hni sebou!", up
    to "Ty jsi muj nejlepsi skret".
  - The offer reads "Rozumim." / "Odmitnout.".
- **Refusing** an order outright costs `orderRefuseLoss` (25) standing in Barad-dur. Closing the offer only puts it off.
- **Giving up** an order, or letting it run out, costs `orderFailLoss` (40).
- **Deadlines:** `orderMinutesPercent` (50%) of the errand's usual time, at least 5 minutes.
- **Size:** the rank caps the tier offered (the table above).
- **Pay:** each rank above Vojak raises an order's share of the pay by `rankPayBonus` (10%).

The other cities' errands are untouched.

## The rival captains

**Gorbag** and **Shagrat** (`c_warcamp_gorbag`, `c_warcamp_shagrat`) lead the two rival bands, the reputation factions
`bd_gorbag` and `bd_shagrat`. `.SetupWorld` (`WarCampSetup`) places them a few steps either side of Barad-dur's spawn point. They are
invulnerable and stand still; a GM can move them. A double click opens a captain's dialog. He talks only to Mordor's own: Gondor gets
"Gondorska krysa v tabore?...", outcasts and those of no realm "S vyvrheli a tulaky nemluvim.".

- **Serving** ("Slouzit ti" / "Budu slouzit tobe."):
  - The first time, the hero's band gains `serveFavour` (40) and the camp the same; the rival band loses half (reputation's faction
    rivalry).
  - Switching sides costs instead: the old band and the camp lose 40 (the new band gains half), an order from the old captain fails,
    and he remembers: his greeting becomes "Zradce!". Going back and forth only ever loses standing.
- **His orders** ("Rozkaz" / "Rozumim."): one at a time, `captainOrderMinutes` (60) to do it, sized by the captain's power (1-5).
  After each order done or failed he gives the hero nothing for `captainOrderCooldownMinutes` (20):

  | Order | Chance | What he wants |
  |---|---|---|
  | Heads | 20% | the head of a Gondor player (from a carved body, not the hero's own); two from power 4 |
  | Tribute | 40% | `tributePerPower` (1000) x power gold, paid through `GoldUtils.Pay` |
  | Iron | 40% | `ironBase` + `ironPerPower` x power (20 + 10 x power) iron ingots |

  - **Done ("Splneno."):** what the order wants is taken. The captain earns a point and pays `captainOrderGold` (200) x power gold
    (not for a tribute) and `captainOrderExp` (150) x power experience. His band and the camp gain `captainOrderFavour` (30).
  - **Given up ("Vzdavam to.") or run out:** his band and the camp lose 30 (the rival band gains half).
- **The weekly settlement:** every `challengeDays` (7), the captain whose men did more orders wins:
  - his power rises a rank and the loser's falls (pobocnik, kapitan, velitel, nacelnik, valecny pan);
  - Mordor's chat hears of it;
  - a tie changes nothing, and points start again either way.

  It is worked out when anyone talks to a captain.
- **Cutthroats:** when a captain's man takes an order, the rival sends cutthroats (`c_warcamp_cutthroat`, named "Hrdlorez
  Shagratuv") one time in `cutthroatChance` (30%).
  - Their number is `cutthroatCount` (2); they come `cutthroatDelaySeconds` (60) later, beside the hero.
  - They only come for a hero still of Mordor, online, alive, not jailed, invulnerable or in an arena, and not serving the captain who
    sent them by then.
  - They are calm towards everyone else and hunt only that hero. Every 5 s they check, and leave when the hero is dead, gone, offline,
    on another map or over 24 tiles away, or after `cutthroatStaySeconds` (300).
- **Nemesis lite:** the hero the cutthroats were sent after, killed by them, makes their captain rise a rank, once an ambush (the
  cutthroats leave at once); Mordor's chat tells it. A bystander's death counts nothing, and it isn't a settlement win. The captain
  remembers: his greeting becomes "Zase ty, <name>? Moji hrdlorezove te posledne nechali lezet v prachu...".
- **Not capped:** a hero with gold or iron can do one captain's order after another (with the pause between), so one busy player can
  decide a week's settlement. Deliberately left open, a question for the user.

## Not done here

- Barad-dur's venues and residents still need marking in game (`.venues navrh`, then `.venue`); without them nobody gives orders
  there, but the captains stand anyway.
- The pits of Nurn, the Eye's spies and the Mouth's dispatches came in step 31 (#1302, see [errands](errands.md), "Mordor's
  stories"). The warg pup and Black Speech aren't built.
- The captains' spots are mine (Erebor had no such captains), unreviewed.

## Test

`Tests/Protocol/WarCampTests.cs` (server side `scripts/Tests/WarCampTests.cs`, account `warcamptests`) sets up a mess hall in
Barad-dur with its residents and two test orders (small and large).

**In the client:** the cook's dialog asks "Rozkaz?", the offer reads "Rozumim." / "Odmitnout.", and refusing costs 25.

**On the server:**
- **Orders:**
  - ranks by value;
  - a promotion paid once;
  - no large order for a Vojak, but one for a Setnik;
  - the halved deadline and the rank's 120% share, with Edoras unchanged;
  - giving up and running out cost standing.
- **Captains:**
  - Mordor only, and no order before serving;
  - serving and the factions;
  - each of the three orders sized, refused while not done, then paid (iron's gold, the tribute not paid back, only a Gondor head
    counts and never the hero's own);
  - giving up and running out, and the pause after;
  - switching sides with an order held (the order fails, the switch costs), the old captain's "Zradce!", and back and forth losing
    standing;
  - the weekly settlement not before the week, then winner and loser;
  - the cutthroats (none for Gondor, an invulnerable hero or the sender's own man; two calm ones hunting a Mordor hero, with their
    timer, leaving when their time is up);
  - the nemesis (not for a bystander, once an ambush, not a win).
- **The cutthroats' timer:** two arrive by it.
