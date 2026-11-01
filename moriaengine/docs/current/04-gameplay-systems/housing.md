# Houses

Erebor's houses (`systems/housemenu.scp`, `systems/najem.scp`), issue #941. Code: `scripts/Basic/Housing`.

A house is a building on the map, a region with a house number, and a hidden gem, `House`, holding who it belongs to. It's not a
multi. The archive has 281 houses (`dum`), 61 stalls (`stanek`) and 38 fields (`pole`). Stalls, fields, multis and deeds aren't
ported yet.

## Ranks

| Rank | Who | Can |
|---|---|---|
| Blocked | Everybody but a GM in a blocked house | See the plaque |
| Public | Everybody else | See the plaque |
| Friend | On the list | Friend and co-owner lists |
| Co-owner | On the list | + add friends, rename, change the sign |
| Owner | | + add co-owners, transfer the house |
| GM | | + block, autorent |

Whoever has a place in a house loses it before getting another. Lists remove a friend from a co-owner up, a co-owner from the owner up.

## Signs

Any item of type `t_housemenu`. One linked to no house takes the house of the region it stands in. A sign shows the menu from the
friend rank up, below that only the plaque.

## Claiming

A house without an owner asks "Chces obsadit tento dum?". The new owner has `claimGraceDays` to pay.

Differences from Erebor: the question also comes the first time, and there's a grace period. Erebor's first click took the house,
and the next unpaid click emptied it.

## Rent

`.settings` Houses: `rentPerTile` a tile, to the nearest `rentRounding`, at least `minimumRent`. Erebor's GMs set each house's rent by hand and those
values aren't in the archive; the median 112 tiles comes to about Erebor's default 100k.

The rest of the system follows as #941 goes on.
