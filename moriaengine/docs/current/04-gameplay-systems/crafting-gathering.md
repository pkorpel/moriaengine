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

Gathering skills (Mining, Lumberjacking, Fishing) follow a simpler shape than crafting:
target a tile or object, run a stroke check, produce or fail. **All three now require
their matching tool equipped** (`#370`, a moriaengine-only design decision, not ported
from Erebor) - see [Item double-click & the "use" action
framework](item-use-and-dclick.md) for the shared `Equippable.IsHoldingToolFor` gate and
how dclicking the tool itself equips it and starts the skill in one step.

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

Requires `i_pickaxe` equipped (`#370`) - new content, didn't exist in moriaengine before
this issue; also a real `WeaponType.OneHandSpike` weapon, matching real Erebor's own
dual-purpose item.

### Lumberjacking

Shares the gathering shape above (target -> stroke -> yield a log) - verified via the
same end-to-end `GatheringTests` suite as Mining and Taming. Requires `i_hatchet`
equipped (`#370`).

### Fishing

Requires `i_fishing_pole` equipped (`#370` - the pole itself was a plain, non-equippable
`ItemDef` before this, a real content gap, fixed alongside the tool requirement).

**Historical note, now resolved**: this page previously said Fishing had no loadable
skill definition at all (`AbstractSkillDef.GetById` returning `null`) - that was true
before `#328` added `FishingSkillDef`'s minimal shell, but the claim was never corrected
here afterward. Confirmed current: `[FishingSkillDef 18]` is a real, loadable
registration in `scripts/Basic/skills/Skills.scp`. A real but narrower gap was still
found and fixed while building `#370`'s test coverage: that same block was missing
`StartByMacroEnabled = true` (unlike Mining/Lumberjacking's own blocks), so the real
client's skill-list/macro packet for Fishing was silently rejected server-side
(`#375`).

## Test coverage

`GatheringTests` (`scripts/Tests/GatheringTests.cs`, `Tests/Protocol/`) is the real
end-to-end coverage for this whole area: taming sets `ControlMaster` (see
[Pets & NPC AI](pets-and-npc-ai.md)), mining yields ore, lumberjacking yields a log - all
verified via a live client connection and real skill-use packets, not a logic-only check.

## See also

[Pets & NPC AI](pets-and-npc-ai.md) for taming's own mechanic (grouped there, not here,
since its real payload is NPC ownership, not a material).
