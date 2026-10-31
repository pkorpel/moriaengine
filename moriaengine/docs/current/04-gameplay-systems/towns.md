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
clothes take the office's colors and looks, and each goes back when the uniform stops being complete. The
numbers are on `.settings`, category "Realm Offices".

## Nazguls and the First Nazgul

`systems/nazguls.scp`, `lordnazguls.scp` and `player_functions.scp`. Mordor has no citizens: a city has a
shrine (`i_shrine_power`, placed by `.SetupTowns` in Barad-dur, Nurn, Rhun, Harad and Umbar), and whoever wears
its ring is its Nazgul. They rule it through the same `Town` as a mayor: blacklist, suggestions, news, home.

**Getting a ring.** With no Nazgul in the city, someone of Mordor at level 30, not a craftsman, double clicks
the shrine. The first time starts the quest: 5 Gondor players of level 30 killed within 3 days. Then the
ring. On Erebor the shrine itself skipped the quest, which only clicking a ring on the ground asked for.
Here a lost ring doesn't lie around, so the shrine asks; `ringQuestKills` 0 turns it off.

A Nazgul shows a red NAZGUL over the name, and their name in that color.

**Keeping it** (`mayhavering`). The first day is safe. Otherwise the ring leaves:

- after 7 days away,
- when offline time reaches 11 times online time,
- when `deaths x 7` reaches `kills x 5` (`x 4` for the First): a Nazgul has to fight.

It's checked on death and every 7 days (5 for the First), when 7 kills also come off the total. A kill
counts if the victim is of the other realm, not a craftsman, Grandmaster, and not in an arena, and not one of
the Nazgul's last 3 victims. Only a player's kill counts as a death.

**The First Nazgul.** A GM appoints a Nazgul on `i_lordnazgulsstone`, or a Nazgul takes it: with no First,
`i_lordnazguls_gate` in Barad-dur lets a Nazgul open the way to Fela Nazgul, 3 of Mordor pass, and there's
half an hour before the trial throws them out. A Nazgul who reaches `i_leading_shrine_power` inside swaps
their ring for the leading one, leaving their city without a Nazgul, and becomes "Prvni Nazgul Name". The
gate is red and shut while there's a First. The First rules every Mordor city and edits the realm's blacklist.
`i_lordnazgulsboard` shows their record to anyone.

`.fear` needs the uniform (robe, cape, gloves, shoes, a mount; the robe turns into a death shroud). Everyone
not of Mordor loses 3 Str, Dex, Int and Vit within 10 tiles for 300 s, or 5 within 12 around the First, and
the Nazgul waits 180 s.

## Saving

`Town` is saved with its stone, as a guild is. Destroying the stone ends the town.

## Not done

- Messages to someone offline are lost: Erebor's `delayedmessage` isn't ported.
- `.remember` and targeting a severed head to suggest a ban: conveniences that need Erebor's masks and heads.
- "Najemni informace", the rent overview, belongs to Erebor's house system.
- `.nazghulfell`, the First's fell beast, and the mount's uniform color.
- The gates of `i_nazgulsonly_gate`: Erebor's GMs placed them, for what isn't recorded.
- A thief's mask isn't ported, so nothing stops a Nazgul using it.
