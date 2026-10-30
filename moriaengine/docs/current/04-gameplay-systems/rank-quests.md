# Rank quests

A player's rank (`Player.SkillRank`: Novice, Expert, Master, Grandmaster) comes from Erebor's three rank quests. The
skill teachers can require it for their lessons, see [NPC speech](npc-speech.md). The quests are being ported one at a
time (#833): **Expert and Master are done**; Grandmaster (`questy/velmistr.scp`) is not.

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

## Master quest

Code: `scripts/Basic/Quests/MasterQuest.cs`, from `questy/mistr.scp`. Its NPCs are in `questy/mistr_chars.scp`.

An Expert of level 6+ greets Mistr Lenastir, says "ano" and gets the task: a fertilized egg of the snake queen. They also
get a key (`i_klic_teleportu`, newbie, handed out again if lost) to an old locked teleport. Asking about it ("kde",
"klic", "teleport") gets Erebor's hint: underground near the snake altars, and there is another way too.

| Step | What happens |
|---|---|
| Stepping on the teleport with the key | The key is used up. An Expert lands on the Snake Islands at 1508,3860,10; anyone else gets "You can't go through here!" |
| Entering the Snake Islands, by any way | Marks the player (`r_hadiostrovy`, Erebor's `tag.mistrquest`) |
| Killing the queen (`c_hadi_kralovna`) | Her corpse holds an egg |
| Lifting the egg | It belongs to the first player to lift it |
| Greeting Lenastir with the egg | Rank Master, 500 experience, egg and key taken |

Lenastir refuses a player who has an egg but was never on the islands ("a plain bought egg"). A player who has been there
but carries someone else's egg is called a cheat and sent to the bug area (370,2536,45), keeping the egg.

### Setting it up

`.SetupMasterQuest` (GM) places the pieces. Running it again replaces them. **The archive doesn't record any of these
spots, they are proposals**; move them as you like, they are ordinary world objects:

| Piece | Spot | Why |
|---|---|---|
| Lenastir (invulnerable), twice | Beside both Expert start stones | Both realms can reach him |
| Teleport | Beridor Level 3, plane 10, between the serpent pillars around 5331,2745 | The hint says underground near snake altars. The map has serpent pillars here. Walkable from the level's spawn point |
| Queen, spawn gem | Snake Islands, near 1518,3890, respawn 30-60 min | Walkable from where the teleport lands |
| Her serpents, spawn gem | Beside the queen, 6 of the `_mistr` serpents, respawn 10-20 min | Those chardefs exist only for this quest |

A GM adding Lenastir by hand needs to make him invulnerable (Erebor's `@create`, dropped by the converter).

### Differences from Erebor

- The egg belongs to the first player to lift it from anywhere. Erebor's `@pickup_pack` only fired when lifting from a
  container.
- The quest stone statistics (`questMenu_questStone`) are not kept.

### Gotcha: `On_Create` in a compiled trigger group

`ThingDef.Trigger_Create` fires `TryTrigger(TriggerKey.create, null)`, and the generated `CompiledTriggerGroup` code reads
`sa.Argv`, so a compiled `On_Create` throws. `Item.On_Create` also runs right after that trigger and overwrites
`attributes`, so an attribute set there would be lost anyway.

Tests: `ExpertQuestTests` (protocol: the setup command, the GM links, the whole quest through real gumps, a wrong answer,
the real minute timer, leaving the cave, re-running the setup mid-quest).
`MasterQuestTests` (protocol: the setup and that its spots are walkable, Lenastir's answers, the key and teleport, the real
queen's egg lifted from her corpse, the reward, a plain egg, someone else's egg, a wasted key).
