# Rank quests

A player's rank (`Player.SkillRank`: Novice, Expert, Master, Grandmaster) comes from Erebor's three rank quests. The
skill teachers can require it for their lessons, see [NPC speech](npc-speech.md). All three are ported (#833).

**To turn the system on**, a GM runs [`.SetupWorld`](../03-admin-tooling/world-setup.md), then switches on `requireRank` in
`.settings` -> "Skill Teachers".

The archive holds the quest scripts but not Erebor's world: its stones, signs and NPCs were placed by GMs on the live
server. So each quest has a [world setup](../03-admin-tooling/world-setup.md) that places its pieces, and the pieces stay
ordinary world objects that GMs can move and relink.

## Expert quest

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

### Setting it up

`.SetupExpertQuest` (GM) places both of Erebor's start stones (Gondor 2771,2761 and Mordor 3723,2953) and one quest
site with six signs in each rectangle of `a_expert`, all linked.

Links can also be made by hand, as on Erebor. A GM adds the piece, double-clicks a site and targets a start stone, or
double-clicks a sign and targets a site. Linking is what hides a site from players and names a sign: Erebor did that
in `@create`, which the converter drops, and a compiled `On_Create` trigger group can't do it (see below).

### Differences from Erebor

- Only the player on the quest gets the master's gump; on Erebor anyone talking to it could answer.
- The signs stay visible. Erebor's `@create` made them invisible, which no player can read.
- The sign gump stays open after a step (`e_expert_signWatch` is not ported).
- The quest stone statistics (`questMenu_questStone`) are not kept.
- The master is invulnerable, or killing it would leave the player stuck until the time limit. It still exists only for the
  attempt, as on Erebor.

Every rank-up gets Erebor's level-up celebration, `QuestNpc.CelebrateRank`: the gate sound, the Manwe's blessing effect and a circle of
sparkles, white for Gondor and black otherwise.

## Master quest

Code: `scripts/Basic/Quests/Master/`, from `questy/mistr.scp`. Its NPCs are in `questy/mistr_chars.scp`. Setup: `scripts/Setup/MasterQuestSetup.cs`.

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

`.SetupMasterQuest` (GM) places the pieces. **The archive doesn't record any of these spots, they are proposals**; move them
as you like, they are ordinary world objects:

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

## Grandmaster quest

Code: `scripts/Basic/Quests/Grandmaster/`, from `questy/velmistr.scp` (the q000 line). Setup: `scripts/Setup/GrandmasterQuestSetup.cs`. A Master of level 12+ starts it.
Progress is one number per player, `GrandmasterQuest.GetStage` (Erebor's `q000_velmistr`):

| Stage | How it's reached |
|---|---|
| 1 | Elqira tells of her lost daughters. Picking the wrong words or place upsets her, and she won't talk for 60-150 s |
| 2 or 3 | Found Elvira first (spellbound), or Elmira first, who asks to find Elvira |
| 4 | Found Elvira after Elmira |
| 5 | Elmira sends the player for the root of Este |
| 6 | Double-clicked the root in the Ered Mithrin cave, once its guards are dead |
| 7 | Double-clicked Elvira, who takes the root (only by hand, not by talking) |
| 8 | Elvira told of Rinald and his name |
| 9 | Killed Rinald and left his cave |
| 10 | Opened Elmira's cage |
| 11 | Told Elqira: a magic bag and 2500 gold |
| 12 | Optional, see below |
| 13 | The envoy of the player's realm makes them a Grandmaster |

**Rinald's cave** (`a_Slunecni_jeskyne`, plane 5, in the archive as the "vm quest" area) opens only at stage 8, and allows 60
minutes. Rinald is invulnerable and no way out opens while he lives. His hounds must be dead before he talks. Naming him
wrongly throws the player out; saying "Rinald" turns him into a daemon 3 seconds later, and he can be killed. A Rinald who
already turned is replaced when the next hero enters.

The dialogs (`GrandmasterQuestDialogs.cs`) are Erebor's texts byte for byte, generated from the archive once and meant to
be read like normal code. They sit on `QuestDialogDef` and `QuestDialogContent` (`Common/QuestDialog.cs`), the part of Erebor's
`questDialog_framework.scp` this quest uses, ready for other quests. Everything the player reads in this quest is Czech.

### Setting it up

`.SetupGrandmasterQuest` (GM) places the pieces. Pieces in caves are found by walking in from a
known starting tile by the real movement rules. The test also walks to the pieces from a spawn point, or the cave's mouth for
the root. A region's spawn point proves nothing by itself: see the note on plane 2 below.

| Piece | Spot | Source |
|---|---|---|
| Rinald, his cave's exit, 4 hellhounds | `a_Slunecni_jeskyne`, 30 and 3 steps from its spawn point | The area is the archive's, the spots are proposals |
| Elvira | The cedar forest at about 4004,512, on the east of the Ice Plains (`a_Severni_Spoust`) | The journal: "in the forest on the east of the Ice Plains". The only forest there, found in the map's statics |
| Elmira, the cage door, the cave's entrance | The rock face at about 4104,530, 100 tiles from Elvira | The journal: "in a cage by a rock", "a little way from" the forest. **There are no cage bars**: Erebor's were built by GMs. She stands still beside the door |
| Root of Este, 4 alarm tiles, 3 phoenixes | The far end of `a_Ered_Mithrinska_Jeskyne`, plane 2, found by walking in from its spawn point | The journal: "at the end of the dark cave", "on the eastern part of Ered Mithrin". The cave's gate is in `.SetupDungeonEntrances` |
| Elqira, twice | Beside both Expert stones | Proposal |
| The envoys | Spawn points of `a_Minas_Tirith` and `a_Rhun` | From the story's "v Minas Tirith / ve Rhunu" |

The cave is reached by a two-way gate at `a_Ered_Mithrin_vstup`, its mouth area on the surface. Erebor's own teleports were placed by
GMs and aren't in the archive. A root placed by an older version stands in the surface tunnels, and setups never move pieces:
delete it, its 4 alarm tiles and the phoenix gem (ids `grandmaster/root...`), then run `.SetupWorld`.

The two cave gates are this repo's own items (`GrandmasterQuest.def`): Erebor's general varGate isn't in the archive.

### Differences from Erebor

Tracked in #837:

- Rinald's breath and `e_rinald`'s weakening by profession are not ported, so he is a harder fight.
- Stage 12 can't happen: it comes from hints in other quests' NPC dialogs, which aren't ported. It's optional.
- Dying in the cave takes the player out on the next minute tick, and the corpse stays inside.
- The Zmocnenec only does the promotion; the rest of his dialog is the q30 quest.

Also: "guarded" means any hostile creature within 10 tiles, where Erebor's guardit system listed spawns by hand.

## Quest journal

Code: `scripts/Basic/Quests/Common/QuestJournal.cs`, from `questy/System/questmenu.scp`. It's the only place a player can look up what
to do next.

`.questy` (any player) opens the menu. Its Questy tab lists the quests a player has started: active in blue first, finished in
green. Clicking one opens its page, with the text for the player's current stage. When a quest moves on, a sound plays and a
scroll appears on the screen's edge, which opens that page; a newer scroll replaces one still showing.

Erebor's journal had the Grandmaster quest (13 texts, byte for byte, in `GrandmasterQuestJournal`), but no page for Expert or
Master. Those two pages are new text in the same style. Expert's scroll shows on starting and passing; a failed attempt
drops the quest from the list.

Open dungeons, boosters, weekly quests and the PvP league are placeholder pages saying the section isn't available yet (#840).
Their systems aren't ported.

To add a ported quest: one `QuestJournalEntry` in `QuestJournal.Entries` and a `Report` call on each step.

### Gump text colour

In the client, an HTML element with no background is white only if it has a scrollbar, and black without one. Erebor's gumps
always set the scrollbar flag, which is why their text is readable on dark gumps. Use `AddHtml(..., false, true)`, or start the
text with a `<basefont>`.

### Gotcha: `On_Create` in a compiled trigger group

`ThingDef.Trigger_Create` fires `TryTrigger(TriggerKey.create, null)`, and the generated `CompiledTriggerGroup` code reads
`sa.Argv`, so a compiled `On_Create` throws. `Item.On_Create` also runs right after that trigger and overwrites
`attributes`, so an attribute set there would be lost anyway.

Tests: `ExpertQuestTests` (protocol: the setup command, the GM links, the whole quest through real gumps, a wrong answer,
the real minute timer, leaving the cave, re-running the setup mid-quest).
`MasterQuestTests` (protocol: the setup and that its spots are walkable, Lenastir's answers, the key and teleport, the real
queen's egg lifted from her corpse, the reward, a plain egg, someone else's egg, a wasted key).
`GrandmasterQuestTests` (protocol: the setup and that its spots are walkable, the whole story in both orders through real
gumps, an upset Elqira, guards, the closed cave, a wrong name, Rinald's change and death, the reward, the envoy's realm).
`QuestJournalTests` (protocol: a plain player's `.questy`, the placeholder sections, the list and its colours, a page, the scroll).
