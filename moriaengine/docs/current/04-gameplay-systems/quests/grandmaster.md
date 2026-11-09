# Grandmaster quest

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
wrongly throws the player out; saying "Rinald" turns him into a daemon 3 seconds later, and he can be killed. Each
profession weakens him its own way (`e_rinald`, #1140). As he turns he breathes a wall of fire (#837, `RinaldBreath.cs`, Erebor's
`f_dragon_fire`): 9 flames across his front run 10 tiles, straight on or turning to one side, each leaving a short fire behind; a
flame burns whoever it reaches (35 by default, `.settings` RinaldBreathDamage) and its fire half that. A hero who dies in the
cave is taken out at once, and their body is laid where they came in from (Erebor's `@DeathCorpse`). A Rinald who already
turned is replaced when the next hero enters.

Stage 12 is optional: Unas and farmer Borek tell a hero who saved the sisters (stage 11) that the envoy is looking for them
(Erebor's `f_q000_shouldBePromoted_*`). The other NPCs who said it on Erebor (q010, q013, q034, Gondor's Borek) have no
dialogs here.

The dialogs (`GrandmasterQuestDialogs.cs`) are Erebor's texts byte for byte, generated from the archive once and meant to
be read like normal code. They sit on `QuestDialogDef` and `QuestDialogContent` (`Common/QuestDialog.cs`), the part of Erebor's
`questDialog_framework.scp` this quest uses, ready for other quests. Everything the player reads in this quest is Czech.

## Setting it up

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

## Differences from Erebor

- Rinald's breath does 35, as Erebor's comment meant ("give them smaller damage"): its call put the 35 where the
  function doesn't read it, so Erebor's breath did 50.
- The breath hurts players and creatures alike but not GMs or Rinald himself, as Erebor's `@step`. A flame also burns
  whoever already stands where it arrives (as the dragons' fire here).
- The envoy's own dialog, beyond the promotion, is the q30 quest (#1276).

Also: "guarded" means any hostile creature within 10 tiles, where Erebor's guardit system listed spawns by hand.

Rank quest 3 of 3; shared parts in the [quests README](README.md).

Tests: `GrandmasterQuestTests` (protocol: the setup and that its spots are walkable, the whole story in both orders through real
gumps, an upset Elqira, guards, the closed cave, a wrong name, Rinald's change and death, the reward, the envoy's realm; the
envoy hint on Unas and Borek, the breath, a death in the cave).
