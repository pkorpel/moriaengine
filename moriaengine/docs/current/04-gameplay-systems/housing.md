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

## Paying

Gold, cheques, purses and strongboxes dropped into the rent chest (`i_najem_chest`) buy time. It's counted when someone clicks the
chest or a sign, and in a daily look at each house.

- Gold: `amount * 2592 / rent`, as a whole number, times 1000 seconds. A month is 30 days.
- A cheque, purse or strongbox pays whole months, days, hours and minutes, less than a minute's worth is lost. Above 80 months it pays
  80 and keeps the rest. Full time is about 6.8 years of server time, as on Erebor.
- A GM's new rent rescales the time left, so what was paid keeps its worth.
- The time runs with the server, like Sphere's `serv.time`.

The chest is locked: only a GM opens it. It comes back at a sign if it's gone.

## Eviction

Unpaid with autorent on, the house empties: what's on the ground goes to a hidden backpack in the chest, tame animals are shrunk into
another, doors stay and unlock, the lists and owner clear. Signs and the chest stay. Only a GM reaches the backpacks, and a backpack
goes after a year.

GMs also have "Vyprazdnit dum", "Vynulovat zaplaceni" and "Nastavit najem".

Differences from Erebor:

- A daily look, as Erebor only checked on a click.
- The last partial day counts: below a whole day read as nothing paid, so a house went a day early.
- A second cheque in the chest pays its own value, Erebor's loop gave it the first one's.

## Locking

"Zamknout/odemknout vec" on the sign menu, from friend up, targets something on the ground in the house, then asks for a level: public,
friend, co-owner, owner, GM (nobody is offered one above their own rank, a friend gets public and friend) or personal. Targeting it
again unlocks it, which wants the level or being the person. In `HouseLock.cs`, and nothing limits how many.

A locked thing:

- can't be lifted by anyone but a GM, who can move it and it stays locked. To move it, unlock it.
- doesn't decay.
- wants the level to double-click. Personal wants the person, or a rank above theirs.
- refuses taking things out of it the same way, and logs a `ZLODEJ !!!` warning. That only reaches someone who had it open before it
  was locked, as a closed one won't open.
- turns black while the house is blocked, and nobody but a GM can use it then, the person included.

A locked door becomes `t_door_locked`. It opens for friends and up whatever its level, so there are no keys to hand out.

Locking a sign sets who sees its menu instead. Ingots, logs, clothes, figurines, chairs, ore, rocks and runes can't be locked except by a GM.
An eviction unlocks everything, as does deleting the house.

Differences from Erebor:

- Ranks replace its hidden keys, as elsewhere in the house.
- Deleting a house unlocks its things. Erebor's redeed deleted them, and it has no multis here yet.
- An eviction unlocks the rent chest too if someone locked it. It stays in place anyway.

The rest of the system follows as #941 goes on.
