# Parties

A party is a short-term group, for a dungeon or for exping; a guild is the long-term one (#916). So a
party is quick to make, leave and change. Code is in `scripts/Basic/Roles/Party`. Erebor had the client's
party and no dialog; `.party` is ours, in the guildstone's style.

## .party

The dialog shows the party's leader and members, with how far each is and their health, then the players
nearby, a click from an invite, then the way out.

The nearby list holds players you can see, of your own realm, within update range, up to the 8 closest.
Nobody hidden, invisible or a GM is on it, and enemies never are: the server has PvP between two realms, so
it mustn't help anyone find one. That's also why there's no list of open groups.

Having no party, "Zalozit druzinu" makes one, and so does a first invite.

## Rules

- Up to 10, invitees included.
- Any member invites, from the dialog or the client's party menu. Gondor and Mordor can't party together.
- Accepting an invite leaves the party you were in.
- Only the leader kicks, promotes and disbands.
- When the leader leaves, the longest-standing member leads. A party ends when one person is left.
- Members show yellow to each other, and hitting one isn't a crime.
- Members in range share experience with a fighter, see [Leveling](leveling-experience.md).

## Looting

Taking from the corpse of a fellow Gondorian is a crime, see [Notoriety](notoriety.md#looting). The exception is
a party member who has "can loot me" on, set by the client's party switch or on `.party`.

The setting stays with the character from party to party, because a client keeps its switch that way, and goes
back to off at login, when a client's does. A client can't be told of a change, so after using `.party` its own
switch can show the opposite. `.party` shows what counts.
