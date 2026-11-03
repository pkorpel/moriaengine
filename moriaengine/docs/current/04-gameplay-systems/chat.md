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
