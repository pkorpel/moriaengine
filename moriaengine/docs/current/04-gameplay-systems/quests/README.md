# Quests

One file per quest, for an overview. Quest systems that aren't a quest of their own (the journal, boosters) have a file here too.

| Quest | File | Ported |
|---|---|---|
| Expert, rank 1 | [expert.md](expert.md) | #833 |
| Master, rank 2 | [master.md](master.md) | #833 |
| Grandmaster, rank 3 (the q000 line) | [grandmaster.md](grandmaster.md) | #833 |
| The quest journal, `.questy` | [journal.md](journal.md) | #839 |
| Boosters, more experience for a while | [boosters.md](boosters.md) | #1090 |
| Weekly quests (Tydenni questy) | [weekly.md](weekly.md) | #1090 |

Not ported: the numbered quests (q006-q100, gq*, mq*: the user wants to revisit
them first), and the rest listed on #1069. All of Erebor's quests, their rewards and
what state they were in: [erebor-catalogue.md](erebor-catalogue.md) (#1235).

The design for what comes next, living cities and dynamic quests (a proposal, #1237):
[living-world-design.md](living-world-design.md).

## Rules for quest ports

- **Quest items are tags** (the user's rule, 2026-11-03, changing Erebor and the quests ported before): a special item a quest
  gives the player (a token, letter, key, trophy, egg) isn't added to the game; the player gets a tag that says they have it.
  Ordinary goods a quest only counts or takes (gold, ingots, common loot) stay real items.
- **One file per quest** in this folder, with what it does, how it's set up, and how it differs from Erebor.
- Texts the player reads are Erebor's, in Czech without diacritics.

## Rank quests

A player's rank (`Player.SkillRank`: Novice, Expert, Master, Grandmaster) comes from Erebor's three rank quests. The
skill teachers can require it for their lessons, see [NPC speech](../npc-speech.md). All three are ported (#833).

**To turn the system on**, a GM runs [`.SetupWorld`](../../03-admin-tooling/world-setup.md). `requireRank` in `.settings` ->
"Skill Teachers" is on by default since #882; a world saved earlier keeps its old value until it's switched there.

The archive holds the quest scripts but not Erebor's world: its stones, signs and NPCs were placed by GMs on the live
server. So each quest has a [world setup](../../03-admin-tooling/world-setup.md) that places its pieces, and the pieces stay
ordinary world objects that GMs can move and relink.

Every rank-up gets Erebor's level-up celebration, `QuestNpc.CelebrateRank`: the gate sound, the Manwe's blessing effect and a
circle of sparkles, white for Gondor and black otherwise.

## Gump text colour

In the client, an HTML element with no background is white only if it has a scrollbar, and black without one. Erebor's gumps
always set the scrollbar flag, which is why their text is readable on dark gumps. Use `AddHtml(..., false, true)`, or start the
text with a `<basefont>`.

## Gotcha: `On_Create` in a compiled trigger group

`ThingDef.Trigger_Create` fires `TryTrigger(TriggerKey.create, null)`, and the generated `CompiledTriggerGroup` code reads
`sa.Argv`, so a compiled `On_Create` throws. `Item.On_Create` also runs right after that trigger and overwrites
`attributes`, so an attribute set there would be lost anyway.
