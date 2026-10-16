# Crafting & gathering

Crafting (turning materials into items via a skill) and gathering (collecting raw
materials from the world) - two related skill families, covered together since gathering
output largely feeds crafting input.

## Crafting

`CraftingProcessPlugin` (`scripts/Basic/skills/Crafting/CraftingProcessPlugin.cs`) drives
one character's in-progress crafting queue - installed on demand (`InstallCraftingPlugin`/
`UnInstallCraftingPlugin`), holding a `CraftingOrder` whose `SelectionQueue` lists each
`CraftingSelection` (an item def plus how many to make).

`StartCrafting` kicks off the skill sequence (`SkillSequenceArgs.Acquire` +
`PhaseSelect()`) for the queue's current item. `MakeFinished` runs after each individual
item's crafting attempt completes, success or failure: on success, decrements that
selection's remaining count (dequeuing it once it reaches zero) then restarts for the
next queued item; on failure, just restarts immediately for the same item. This is a
plain queue-drain loop, not a batch operation with its own separate state machine - each
individual craft is a full, ordinary skill-sequence run.

`Craftmenu` (`scripts/Basic/skills/Crafting/Craftmenu/`) is the player-facing menu gump
(`CraftmenuDialog.cs`) that builds the queue in the first place, plus a settings dialog
(`CraftmenuSettingsDialog.cs`) for its own display options. See
[Dialog framework](../03-admin-tooling/dialogs-framework.md) for the general gump-
building toolkit both use.

`Bowcraft.cs` and `type_crafting.cs` hold crafting-skill-specific content (Bowcraft is one
of the real craft skills; `type_crafting` groups the shared item-type plumbing crafted
content hooks into).

## Gathering

Gathering skills (Mining, Lumberjacking - and Fishing, though it has no loadable content
in this engine, see below) follow a simpler shape than crafting: target a tile or object,
run a stroke check, produce or fail.

### Mining (#21)

`MiningSkillDef` (`scripts/Basic/skills/Gathering/MiningSkillDef.cs`) was a shell with
only an `@Fail` message before this - no success path at all. Modeled on
`LockpickSkillDef`'s shape (target -> stroke check -> success/fail), the simplest
existing single-check non-combat skill pattern in this codebase.

**Deliberate simplification**: real UO identifies specific "ore vein" map tiles; this
engine has no per-tile vein registry. Rather than guess at a vein-tile-ID allowlist with
no way to verify it against real client data, mining works on **any** nearby impassable
land tile (`TileFlag.Impassable` - mountains/rock, per `tiledata.mul`'s own flag naming),
the closest real signal this engine actually has for "rocky terrain." There is no
per-tile depletion/respawn system - the skill's own stroke delay is the only throttle
against spamming the same spot, the same simplification basis used for Lumberjacking.

### Lumberjacking

Shares the gathering shape above (target -> stroke -> yield a log) - verified via the
same end-to-end `GatheringTests` suite as Mining and Taming.

### Fishing - a real pre-existing content gap, not a code bug

`AbstractSkillDef.GetById` returns `null` for Fishing - **there is no loadable skill
definition for it at all** in this engine's current content, confirmed while working on
an unrelated system ([Leveling & experience](leveling-experience.md)'s spendable-skills
list originally used Fishing as its example, matching Erebor's own source, and had to be
switched to Alchemy once this gap was found). This is a pre-existing content gap, not
caused by any pass documented in these docs - flagged here rather than silently worked
around everywhere it's hit.

## Test coverage

`GatheringTests` (`scripts/Tests/GatheringTests.cs`, `Tests/Protocol/`) is the real
end-to-end coverage for this whole area: taming sets `ControlMaster` (see
[Pets & NPC AI](pets-and-npc-ai.md)), mining yields ore, lumberjacking yields a log - all
verified via a live client connection and real skill-use packets, not a logic-only check.

## See also

[Pets & NPC AI](pets-and-npc-ai.md) for taming's own mechanic (grouped there, not here,
since its real payload is NPC ownership, not a material).
