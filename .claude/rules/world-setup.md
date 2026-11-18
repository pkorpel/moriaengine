---
paths:
  - "moriaengine/scripts/Setup/**"
  - "moriaengine/scripts/Basic/**/*Setup*.cs"
---

# World setup

- Anything that must exist in the world (quest stones, NPCs, teleports, gates, spawn gems, signs) gets a
  `WorldSetup` in `scripts/Setup` and a line in `WorldSetups.All`, so `.SetupWorld` places it. Never write
  one-off placement code. Doc: `moriaengine/docs/current/03-admin-tooling/world-setup.md`.
- Setups only **add** what's missing. They never replace or remove anything.
- Each piece has a **permanent id**. Never reuse or rename an id. A piece a GM moved stays where it is;
  a piece a GM deleted is created again. A piece replaced at runtime calls `WorldPieces.MarkAsPiece`.
- Item pieces are fixed in place (`attr_move_never`) on every run, which stops decay and pickup. GMs can
  still drag them, with `.gm` on.
- NPCs are placed as spawn gems (`WorldPieces.Npc` through `SpawnGem`), so they respawn. Homedist 6, or 0
  for those that stand still.
- The world is rebuilt from an empty save. The archive has the scripts but not Erebor's world, so
  GM-placed pieces (teleports, signs, venue spots) are our proposals. Mark them unreviewed and say so.
  GM event tools (arenas, tournaments) aren't placed automatically.
- When placing content, check the region's Mapplane, and check that a walk from the spot really goes
  somewhere. A "reachable" check from a point that can't move passes on a zero-length path; use
  `WalkableRing` (`NearestStandable` fails in caves). Two gates must not lead onto each other.
- Changing the pieces means updating the counts in `WorldSetupTests` and `WorldSetupRestartTests`.
