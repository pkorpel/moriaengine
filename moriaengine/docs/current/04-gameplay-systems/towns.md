# Towns

Port of Erebor's towns (#892): `systems/starostove.scp` and `starostove-items.scp`. Code is in
`scripts/Basic/Towns`, the menu texts are Erebor's. The numbers are on `.settings`, category "Towns",
with Erebor's values as defaults.

## Cities and stones

A map area belongs to a city by Erebor's `tag.jmeno_mesta`, kept as `FlaggedRegion.City`, and to a realm
by `tag.realm`, kept as `FlaggedRegion.RealmTag`. Several areas can share a city: Pelargir and Linhir are
Dol Amroth's. A city is one `Town`, and its stone stands in the area named after it.

`.SetupTowns`, part of `.SetupWorld`, places `i_townstone` in the 9 Gondor cities, the realm stones, the
Mordor shrines and the First Nazgul's pieces. Erebor's stones were
placed by GMs and the archive has no positions, so the spots are proposals, a few steps from each
city's spawn point. A GM moves a stone like any other world piece. The tax chest appears beside
the stone at its first use, and comes back if it's gone.

A stone only works in a Gondor area, for someone in the same map area.

## Ranks

`Town.GetRank`, Erebor's `tm_findrank`: Verejnost, Obcan, Starosta, GM. What the menu offers depends on it.

- **Citizenship** needs Gondor, level 20, Grandmaster rank and 100 000 gp, and one town at a time.
  Erebor's votes on admitting and expelling citizens were switched off in its own menu: the handlers
  exist, the buttons are commented out. So they aren't ported; a GM removes a citizen.
- **Tax** falls due every 30 days, and the time runs out when someone uses the stone, as on
  Erebor. A citizen who hasn't paid stops being one; one who has starts the next period owing. The switch
  in "Stav platby dani" pays automatically, at once and every period, and a warning replaces it if the
  money isn't there. The mayor pays nothing.
- **The mayor** sets the tax once a week, between `1500 x citizens` and 500 000, and writes up to 3 news
  items. If the tax is under the minimum, the next use of the stone raises it.
- **Treasury**: tax and home fees. Gold that gets into the tax chest counts when the chest is used.
- **Home**: free for citizens, 100 000 gp for others, going to the treasury.

Paying follows Erebor's `pay`: the pack first, then the bank, and a GM pays nothing.

## Elections

The only vote Erebor kept. A citizen stands in "Kandidovat na starostu", and the first candidate starts an
election, once the mayor's term of 30 days is over or if there's no mayor. It runs 7 days and, like the tax,
ends when someone uses the stone after that.

A citizen who has paid the tax votes once, and not for themselves. The most votes win. Of a tie, a sitting
mayor stays and otherwise the lot decides. A mayor who resigns or is removed leaves no term to wait for.

## Blacklists

A citizen suggests a name with `.navrhban`, with a reason; the mayor takes or refuses it in "Hraci navrzeni
na Blacklist". A mayor can also add a name directly, from the menu or with `.blacklist`. Citizens can't be
listed. Everyone sees the list, and the mayor or a GM can take a name off.

A ban is on the character under that name, as on Erebor, so a masked character isn't recognized.

As a mask changes the name, `.remember` (#905) keeps a targeted player and the name they have now. `.remembered` answers
an open cursor with that player from anywhere (Sphere's `last`, as Erebor's), and a ban or suggestion of them then files
the remembered name: the mayor's, `.navrhban` and the General's. `.rememberclear` forgets them. All three are player
commands. Two changes from Erebor: a player's `.remembered` answers only those blacklist cursors (Erebor's answered any
cursor from anywhere, so a pact or a house friend could reach across the world; a GM's still does), and the mayor's and
a Nazgul's own ban use the remembered name too (Erebor's `blacklist_him` overwrote it with the current one).

Every blacklist target (the mayor's, `.navrhban`, the General's) also takes a player's severed head and treats it
as that player, as Erebor's `@targon_item` (#906). A head that isn't a player's, or any other item, is refused and
ends the target with Erebor's lines ("To neni hlava hrace" / "Zameruj lidi, nebo jejich hlavy"; for `.navrhban`
"Toto neni hlava hrace" / "Navrhuj lidi, nebo jejich hlavy").

Guards go by their home's town: a guard attacks a blacklisted player on sight and answers calls about
them, shouting a warning first at someone still outside the walls. A guard protects a citizen of its own
town even when criminal, though not a murderer.

## The General of Gondor

`systems/generals.scp`. A GM appoints one on `i_generalstone` (`.SetupTowns` stands it in Minas Tirith): the
character becomes "General Name" and wears `i_generals_bracelet`, which can't come off by hand. The
stone shows everyone the realm's blacklist. The General and GMs edit it. Guards of the realm attack
those names anywhere, unless they're citizens of the guard's own town.

`.honor` gives everyone not of Mordor within 8 tiles +5 Str, Dex, Int and Vit for 240 s, and the General
waits 300 s to use it again. It needs the uniform: robe, cape, gloves, shoes, hat and a mount. Complete, the
clothes take the office's colors and looks, and each goes back when the uniform stops being complete (the
mount too, #908: the General's in 0x481, a Nazgul's in 0x690, both looking like `i_mt_horse_tan`; back to its own at a
dismount). The
numbers are on `.settings`, category "Realm Offices".

## Nazguls and the First Nazgul

`systems/nazguls.scp`, `lordnazguls.scp` and `player_functions.scp`. Mordor has no citizens: a city has a
shrine (`i_shrine_power`, placed by `.SetupTowns` in Barad-dur, Nurn, Rhun, Harad and Umbar), and whoever wears
its ring is its Nazgul. They rule it through the same `Town` as a mayor: blacklist, suggestions, news, home.

**Getting a ring.** With no Nazgul in the city, someone of Mordor at level 30, not a craftsman, double clicks
the shrine. It asks for 5 kills of Gondor players of level 30 or more in the last 3 days, rolling, and gives
the ring at once if they're there (#1009, the user's call). A Mordor player's kills are always remembered, so
there's no click to start it and nothing is wiped. On Erebor, clicking a ring on the ground started a 3 day
count from that moment, and the shrine itself skipped the quest. `ringQuestKills` 0 turns it off.

A Nazgul shows a red NAZGUL over the name, and their name in that color.

**Keeping it** (`mayhavering`). The first day is safe. Otherwise the ring leaves:

- after 7 days away,
- when offline time reaches 11 times online time,
- when `deaths x 7` reaches `kills x 5` (`x 4` for the First): a Nazgul has to fight.

It's checked on death and every 7 days (5 for the First), when 7 kills also come off the total. A kill counts
for everyone who fought the victim, if the victim is of the other realm, not a craftsman, Grandmaster, not in
an arena, not one of that Nazgul's last 3 victims, and hadn't just died. Only a player's kill counts as a
death, and not one right after another.

**The First Nazgul.** A GM appoints a Nazgul on `i_lordnazgulsstone`, or a Nazgul takes it: with no First,
`i_lordnazguls_gate` in Barad-dur lets a Nazgul open the way to Fela Nazgul, 3 of Mordor pass, and there's
half an hour before the trial throws them out. A Nazgul who reaches `i_leading_shrine_power` inside swaps
their ring for the leading one, leaving their city without a Nazgul, and becomes "Prvni Nazgul Name". The
gate is red and shut while there's a First. The First rules every Mordor city and edits the realm's blacklist.
`i_lordnazgulsboard` shows their record to anyone.

`.fear` needs the uniform (robe, cape, gloves, shoes, a mount; the robe turns into a death shroud). Everyone
not of Mordor loses 3 Str, Dex, Int and Vit within 10 tiles for 300 s, or 5 within 12 around the First, and
the Nazgul waits 180 s.

## Hired guards

`systems/starostove.scp`, `towns.scp`, `seznam_guards.scp`. A town's mayor or Nazgul hires in "Najimat straze"
from what `seznam_guards.scp` allows that city, paying the treasury, which anyone can add to there. The General
and First Nazgul have their own force and funds at the realm stone, the king's and Marghul's guards, up to
`max_guards_<city>`. A wage buys 7 days and can be extended; an unpaid guard leaves.

"Rozmistit straze" gives a hired guard a post: inside the city, at least 6 tiles from another post. A guard who
dies comes back after 30 minutes.

Orders come from "Pokyny strazim" or the commands, for the city you stand in:

| | Town | Realm |
| --- | --- | --- |
| Gondor | `.towncontrol`, `.townguardskill`, `.townguardsstop`, `.townguardscontinue`, `.townguardsflee`, `.townguardscome` | `.guardscontrol`, `.kingguardskill` and so on |
| Mordor | `.mordortowncontrol`, `.mordortownguardskill` and so on | `.mordorguardscontrol`, `.marghulsguardskill` and so on |

Kill sends up to 5 guards near you, and in Gondor not at an honest Gondoran: the target has to be criminal,
a murderer or on the blacklist. Stop holds them until a hit or the next order, flee sends them to their post,
come brings those within 15 tiles. The General and First also command any town's guards in their realm.

## Proposed spots

The archive records no positions, so these are `.SetupTowns`'s proposals, each 3 or more steps from the
area's spawn point. Moving a piece in game keeps it there.

| Piece | Spot |
| --- | --- |
| Minas Tirith stone | 5678,3868,-80 |
| Edoras stone | 2312,2512,0 |
| Dol Amroth stone | 2262,3202,0 |
| Fornost stone | 1424,1087,4 |
| Hobitin stone | 1079,1324,0 |
| Erebor stone | 3157,967,40 |
| Dol stone | 3192,1061,1 |
| Sede pristavy stone | 755,1368,0 |
| Kralovstvi lesnich elfu stone | 2976,1245,0 |
| General's stone | 5679,3866,-80 |
| Barad-dur shrine | 3647,2714,-50 |
| Nurn shrine | 3472,3261,12 |
| Rhun shrine | 3824,2131,0 |
| Harad shrine | 4186,3957,0 |
| Umbar shrine | 2502,4014,11 |
| First Nazgul's stone | 3647,2712,-50 |
| First Nazgul's board | 3647,2710,-50 |
| Gate to Fela Nazgul | 3647,2708,-50 |
| Gate back | 5850,2104,10, plane 10 |
| Leading shrine | 5854,2104,10, plane 10 |

## Saving

`Town` is saved with its stone, as a guild is. Destroying the stone ends the town.

## Not done

- "Najemni informace", the rent overview, belongs to Erebor's house system.
- `.nazghulfell`, the First's fell beast (`i_mt_drake` in 0x692).
- The gates of `i_nazgulsonly_gate`: Erebor's GMs placed them, for what isn't recorded.

## Realm uniform

Erebor's paid uniform (`systems/uniforms.scp`, `RealmUniform`, #1133). For a number of months, a player of skill rank 3
or more wears their realm's colours.

The set:
- needed: legs, gloves, chest, arms and cape;
- joining in: helm, gorget and sash;
- each armor piece verite or better; the cape and sash may be any.

Worn complete:
- Every piece takes the realm's colour: Gondor's armor 0x57C and clothes 0x98F, Mordor's 0x5EB and 0x993.
- Every piece is named "Gondorska Uniforma" or "Mordorska Uniforma".
- A piece taken off gets its own colour and name back, and so does the rest when the set is no longer complete.

Who gets it:
- A GM gives or lengthens it in months from a player's `.info` ("Uniforma", Erebor's `d_uniform_realm`), or ends it.
- `.adminunif` lists everyone with one: name, account, until, last editor.
- A war lord who becomes a keep's captain gets 12 months; a dismissed captain loses it.
- Someone in an office's uniform (General, Nazgul) is left out, as Erebor's office uniform came first.

The time runs on real dates. A uniform that ran out goes at the next equip, as on Erebor.

Not ported:
- Erebor's `denyDye`/`denyScissoring` tags, which nothing here reads;
- the player switch `deniedUniform`, which nothing on Erebor set.

Test: `RealmUniformTests.ACompleteSetTakesTheRealmsColoursForItsMonths`:
- a set without its cape stays as it is; the cape completes it;
- taking the cape off restores every piece;
- three months on it has run out;
- the GM dialog gives 3 months.

