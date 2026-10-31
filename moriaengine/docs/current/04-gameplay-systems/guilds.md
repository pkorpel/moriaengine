# Guilds and guildstones

Port of Erebor's guild (#884): `systems/guilds.scp`. A guild is run from its stone. Code is in
`scripts/Basic/Roles/Guild`, the menu texts are Erebor's, unchanged.

## The stone

`i_deed_GUILDSTONE`: double click, target the ground, and `i_guildstone` stands there, fixed
(`attr_move_never`). The deed is used up. There's no terrain check on the spot.

The first to double click a stone nobody belongs to becomes its master, unless they're in another
guild. Everyone else gets the menu at their own rank. If the last member leaves, the next
double click starts over. Destroying the stone destroys the guild.

## Ranks and the menu

`D_GuildMenu` (`d_guild_menu`) shows a line only from the rank that may use it. A GM counts as the
master of every guild (`SrcIsMaster`).

| Rank | Adds |
| --- | --- |
| Verejnost | Zobrazit seznam clenu |
| Kandidat | seznam kandidatu, Vzdat se clenstvi, zobrazeni zkratky |
| Clen | Pridat kandidata na clenstvi |
| Master / Mistress | Nastavit jmeno guildy, Nastavit zkratku, Predat vedeni |

- A member targets a player to make them a candidate; the master accepts them in the candidate
  list (`D_GuildCandidates`). A candidate keeps their place in line when accepted, and
  `GuildMembership.seniority` keeps the eldest first.
- Anyone who sees the candidate list can remove a candidate, as on Erebor. Only the master
  removes members (`D_GuildMembers`). Both ask first (`D_GuildConfirm`, `d_potvrdit`).
- Renaming also renames the stone and fixes the name in other guilds' war and alliance lists. A
  name another guild has is refused.
- When the master leaves, the eldest member takes over. Erebor's vote buttons did nothing (their
  line is commented out), so that's all `refreshmaster` amounted to, and the list has no vote buttons.

## Name, chat, commands

With the abbreviation on, a single click shows `Name [BR]`. `Character.On_Click` and
`On_AosClick` both do this: a client with AOS tooltips uses the second, and a cliloc name can't
hold the abbreviation, so such a name goes out as plain text. It's off by default, as on Erebor.
Members are told when another logs in or out (`--*Name se pripojil*--`).

| Command | Does |
| --- | --- |
| `.guildMembers` | who's online and where, full members only (`d_guild_showGuildMembers`). Erebor listed only those with the guild chat on; there's no such switch here |
| `.toggleabbrev` | the abbreviation, same as the menu line |
| `.guildleave` | leave without going to the stone. Ours, not Erebor's |
| `.guildsay`, `.guildinfo`, `.guildabbreviation` | ours, from #14 |
| `.guilddeclarewar`, `.guildmakepeace`, `.guilddeclarealliance`, `.guildbreakalliance` | master only, ours from #14. Erebor had wars and alliances only as a "future" comment, so the menu has no lines for them |

`.guildcreate`, `.guildinvite`, `.guildaccept`, `.guilddecline` and `.guilddisband` are gone: the
stone does their work.

## Saving

Nothing leads the save to a Role by itself, so `GuildStone.Save` saves its guild, and the guild its
memberships. Before #884 no guild ever survived a restart. A guild without a stone isn't saved,
and `GuildDef.Bootstrap` registers `r_guild` before the world loads.

The stone's guild isn't a `.ct` field, so a GM's `.dupe` of a stone is a new stone with no guild.

## Not done

- The client's guild button (0xD4).
- A way for players to get the deed. The archive has none either: its only source is a test kit
  in `funkce.scp`, so for now a GM hands one out with `.add i_deed_GUILDSTONE`.
