# Boosters

Code: `scripts/Basic/Quests/Boosters/Boosters.cs`, from Erebor's `questy/System/boosters.scp` and the journal's boosters page
(`questy/System/questmenu.scp`). #1090.

A booster gives more experience for a while. Erebor had six, all for experience:

| Kind | Bonus | Hours |
|---|---|---|
| 0 | +25% zkusenosti | 1 |
| 1 | +25% zkusenosti | 2 |
| 2 | +25% zkusenosti | 4 |
| 3 | +50% zkusenosti | 1 |
| 4 | +50% zkusenosti | 2 |
| 5 | +50% zkusenosti | 4 |

- **Keeping them**: a player keeps unused boosters as counts, one row per kind, in the order first had. They never expire.
- **Using one**: in `.questy`, the Boostery tab shows the running booster with its time left, and the stored ones three to a
  page. A stored booster's icon starts it; while one runs, the icons are greyed and nothing else starts (Erebor refused
  silently). Nothing stacks. The log gets `pouzil Booster <kind> na pozici <spot> (<region>)`.
- **The bonus**: every experience gain gets the running booster's percent of it on top (`BoosterPlugin`'s
  `@gainExperience`, raised in `LevelingSystem.AddExperience`; Erebor's `addexp`): kills, quests, weekly quests, everything. Losses get nothing.
- **The end**: "Tvuj bonus do zkusenosti vyprsel!" when its hours are over.
- **Offline**: a running booster's hours count only while its player is logged in; logging out pauses it with its time left,
  and logging in resumes it. Sphere doesn't tick an offline character.
- **Where it lives** (#1471): a `BoosterPlugin` on the player (`BoosterPlugin.cs`) holds the stored rows, the running kind
  (Erebor's `i_booster`, the plugin's timer) and the seconds left while offline. Its own `@logout` pauses, its `@login`
  resumes, and its `@gainExperience` adds the running one's percent of the gain's base. A player without boosters has no
  plugin.

## Where boosters come from

On Erebor: taking part in conquest (`dobyvani`: 3 points gave kind 0, 7 gave kind 1) and the final rewards of the gq058
Zaskodnice and mq057 Zaskodnik quests (kind 1). Conquest gives them since #1116 (`ConquestRewards`, see
[conquest](../conquest.md#rewards)); the numbered quests aren't to be started yet. A GM gives one with `.AddBooster <kind>` and then targets the player;
`.AddBooster` alone lists the kinds.

## Differences from Erebor

- The icons of stored boosters are greyed with a hue while one runs, not Erebor's checkertrans, which the client lets through
  to the whole window.
- Erebor's page also cleared expired dungeon marks, a leftover copied from the dungeons page; dungeon marks aren't ported.

Tests: `QuestJournalTests` (protocol: the empty page, stored boosters and their counts, starting one by its button, the bonus,
no second one, a logout's pause and a login's resume).
