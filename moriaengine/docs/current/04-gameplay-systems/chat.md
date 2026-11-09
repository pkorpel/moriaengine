# Chat

Erebor's chat (`systems/chat.scp`). Code: `scripts/Basic/Chat`. Numbers and colours are on `.settings` (Chat).

`.zprava` opens an input box. OK sends to your realm, anywhere on the server, as `*Name* text` in the system message
area: Gondor's blue, Mordor's red. A guild member also has a GuildChat box, which sends to the guild and is remembered. A
GM has a button per channel.

`.chat` has the channel switches and a list of up to 10 names not to hear. Names are kept as typed, so someone offline
can be on it, in any case. A player sees only the channels that mean something to them.

| Channel | Who hears it | Sent with |
|---|---|---|
| Gondor, Mordor | That realm, and GMs | `.zprava`, a GM's `.goch`, `.evch` |
| Guild | Its members | `.zprava`, `.guildScream`, `.GuildSay` |
| GM | GMs | `.gmch` |
| All | Everyone | `.all` |

A channel must be on to hear it and to send on it. Everything is on by default but the other realm's, and a GM has both
realms. What's kept is only what a player changed, so a new realm brings its own channel.

Outcasts and players of no realm have no realm channel, as on Erebor.

- A message is cut at 73 characters, and the sender is told.
- A player sends one per 3 seconds. GMs aren't limited.
- A GM's `.mute` on a target blocks their realm chat. Again un-mutes.
- Realm and guild messages go to the server log.
- `.guildScream` sends your coordinates and region to the guild.
- A guild's join and leave notices are on its channel, so the switch silences them.
- Losing a Ring of Power is announced to Mordor.

A GM's commands are parsed as scripts, so text is quoted: `.goch "text"`. A plain player's `.GuildSay` takes it as it is.

## Differences from Erebor

- Any character can be sent. Erebor dropped `( [ ] < {` without a word, to protect Sphere's scripts.
- A player is told why a message didn't go.
- A GM can send from the dialog. Erebor's dropped theirs.
- The rate limit is real: on Erebor it was only a rule in the help text.
- The list is edited in the dialog, not one input box per name.
- No Mordor switch for Gondor, or the reverse: it never did anything.
- A possessing mystic can't use chat ("Ve svem stavu nemuzes pouzivat masovou komunikaci"), as on Erebor.

Erebor's personal settings panel showed the same switches. Here they're only on `.chat`, and its minipanel button isn't ported.

## Waiting messages (#901)

Erebor's `delayedmessage` and `delayedredMessage` (`funkce.scp`, the dialogs in `various_gumps.scp`). Code:
`scripts/Basic/Chat/DelayedMessages.cs`; `Player.DelayedMessage` and `DelayedRedMessage` go through it.

- Someone online reads the line at once, in the system message area (red for an important one). Nothing is kept.
- For someone offline it waits on the character, the important ones apart. At login a list of each opens, important
  ones first, 25 a page; a click on a message deletes it.
- `.ms` opens the waiting messages again, `.rms` the important ones; `.clearms 1` empties the important ones, `.clearms`
  the others.
- Sent this way: everything Erebor sent so in the towns (bans, suggestions, taxes, citizenship lost, elections, a mayor
  removed), the realm's blacklist, a Ring of Power leaving its wearer, `.rentalinfo`'s bans, the conquest's and PvP
  season's rewards, and the auction's outbid and sold notices.
- Not ported: Erebor's staff exceptions (a few GMs always got the list), the citizens' votes on expelling or admitting
  someone (not built here), and the stall and field owners' notices (stalls are claimed, not auctioned).

The older SteamEngine message board (`scripts/Basic/mailingsystem`, `.messages`) is no longer used by anything, and `.messages`
is GM-only now: its buttons threw (`OnResponse` reads its list as an `ArrayList`).
