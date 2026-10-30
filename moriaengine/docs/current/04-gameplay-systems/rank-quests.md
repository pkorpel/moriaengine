# Rank quests

A player's rank (`Player.SkillRank`: Novice, Expert, Master, Grandmaster) comes from Erebor's three rank quests. The
skill teachers can require it for their lessons, see [NPC speech](npc-speech.md). The quests are being ported one at a
time (#833): **Expert is done**; Master (`questy/mistr.scp`) and Grandmaster (`questy/velmistr.scp`) are not.

The archive holds the quest scripts but not Erebor's world: its stones, signs and NPCs were placed by GMs on the live
server. So each quest comes with a GM command that places its pieces, and the pieces stay ordinary world objects that
GMs can move and relink.

## Expert quest

Code: `scripts/Basic/Quests/ExpertQuest.cs`, from `questy/expert.scp`.

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

### Setting it up

`.SetupExpertQuest` (GM) places both of Erebor's start stones (Gondor 2771,2761 and Mordor 3723,2953) and one quest
site with six signs in each rectangle of `a_expert`, all linked. Running it again replaces what it placed, and any quest
running at those sites ends with its player taken home.

Links can also be made by hand, as on Erebor. A GM adds the piece, double-clicks a site and targets a start stone, or
double-clicks a sign and targets a site. Linking is what hides a site from players and names a sign: Erebor did that
in `@create`, which the converter drops, and a compiled `On_Create` trigger group can't do it (see below).

### Differences from Erebor

- Only the player on the quest gets the master's gump; on Erebor anyone talking to it could answer.
- The signs stay visible. Erebor's `@create` made them invisible, which no player can read.
- The sign gump stays open after a step (`e_expert_signWatch` is not ported).
- The quest stone statistics (`questMenu_questStone`) are not kept.

### Gotcha: `On_Create` in a compiled trigger group

`ThingDef.Trigger_Create` fires `TryTrigger(TriggerKey.create, null)`, and the generated `CompiledTriggerGroup` code reads
`sa.Argv`, so a compiled `On_Create` throws. `Item.On_Create` also runs right after that trigger and overwrites
`attributes`, so an attribute set there would be lost anyway.

Tests: `ExpertQuestTests` (protocol: the setup command, the GM links, the whole quest through real gumps, a wrong answer,
the real minute timer, leaving the cave, re-running the setup mid-quest).
