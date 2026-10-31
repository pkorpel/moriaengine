# Towns

Port of Erebor's towns (#892): `systems/starostove.scp` and `starostove-items.scp`. Code is in
`scripts/Basic/Towns`, the menu texts are Erebor's. The numbers are on `.settings`, category "Towns",
with Erebor's values as defaults.

## Cities and stones

A map area belongs to a city by Erebor's `tag.jmeno_mesta`, kept as `FlaggedRegion.City`, and to a realm
by `tag.realm`, kept as `FlaggedRegion.RealmTag`. Several areas can share a city: Pelargir and Linhir are
Dol Amroth's. A city is one `Town`, and its stone stands in the area named after it.

`.SetupTowns`, part of `.SetupWorld`, places `i_townstone` in the 9 Gondor cities. Erebor's stones were
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

## Blacklists

A citizen suggests a name with `.navrhban`, with a reason; the mayor takes or refuses it in "Hraci navrzeni
na Blacklist". A mayor can also add a name directly, from the menu or with `.blacklist`. Citizens can't be
listed. Everyone sees the list, and the mayor or a GM can take a name off.

A ban is on the character under that name, as on Erebor, so a masked character isn't recognized.

Guards go by their home's town: a guard attacks a blacklisted player on sight and answers calls about
them, shouting a warning first at someone still outside the walls. A guard protects a citizen of its own
town even when criminal, though not a murderer.

## Saving

`Town` is saved with its stone, as a guild is. Destroying the stone ends the town.

## Not done

- Messages to someone offline are lost: Erebor's `delayedmessage` isn't ported.
- `.remember` and targeting a severed head to suggest a ban: conveniences that need Erebor's masks and heads.
- "Najemni informace", the rent overview, belongs to Erebor's house system.
