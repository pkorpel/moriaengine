# Expert quest

Code: `scripts/Basic/Quests/Expert/`, from `questy/expert.scp`. Setup: `scripts/Setup/ExpertQuestSetup.cs`.

A Novice of level 3+ double-clicks a start stone and is taken to a free quest site in the `a_expert` cave. Six clue
signs around the site get a random arrangement of clues about five names, and a quest master appears. Greeting it
("ahoj", "zdravim", "hi"...) or double-clicking it opens its gump, which asks three questions that follow from the clues.
Three right answers make the player an Expert. They are taken back to Gondor (2757,2757) or Mordor (3735,2962) by realm.

| Ending | Result |
|---|---|
| Three right answers | Rank Expert, back home |
| A wrong answer, or "Konec" in the gump | Back home, can try again |
| The 60-minute limit (a countdown message every minute) | Back home, can try again |
| Leaving the region of the site | The quest ends, the player stays where they walked to |

A site takes one player at a time; a start stone sends the next player to the first free site it is linked to, or
says to wait.

## Setting it up

`.SetupExpertQuest` (GM) places both of Erebor's start stones (Gondor 2771,2761 and Mordor 3723,2953) and one quest
site with six signs in each rectangle of `a_expert`, all linked.

Links can also be made by hand, as on Erebor. A GM adds the piece, double-clicks a site and targets a start stone, or
double-clicks a sign and targets a site. Linking is what hides a site from players and names a sign: Erebor did that
in `@create`, which the converter drops, and a compiled `On_Create` trigger group can't do it (see below).

## Differences from Erebor

- Only the player on the quest gets the master's gump; on Erebor anyone talking to it could answer.
- The signs stay visible. Erebor's `@create` made them invisible, which no player can read.
- The sign gump stays open after a step (`e_expert_signWatch` is not ported).
- The quest stone statistics (`questMenu_questStone`) are not kept.
- The master is invulnerable, or killing it would leave the player stuck until the time limit. It still exists only for the
  attempt, as on Erebor.

Rank quest 1 of 3; shared parts in the [quests README](README.md).

Tests: `ExpertQuestTests` (protocol: the setup command, the GM links, the whole quest through real gumps, a wrong answer,
the real minute timer, leaving the cave, re-running the setup mid-quest).
