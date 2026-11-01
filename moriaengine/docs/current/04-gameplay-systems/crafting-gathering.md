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


### What opens a menu (#962)

Never the skill list: as in Sphere, those buttons say the skill can't be used directly.

| Skill | Opened by |
|---|---|
| Blacksmithing | ingots, or a one-handed mace-type tool targeted on ingots |
| Bowcraft | feathers, shafts, or a knife targeted on logs, ingots, bolts or arrows |
| Carpentry | carpentry tools |
| Tinkering | tinker's tools |
| Tailoring | a sewing kit targeted on cloth, a hide or leather |
| Alchemy | a mortar, or any reagent |
| Inscription | a blank scroll |

Cooking has no menu on Erebor: `cmprep_cooking` exists but nothing opens it. Raw food is double clicked and targeted on a fire, forge or campfire. It takes 2 s, a difficulty up to 50.0, and uses up one raw piece either way.

### Spinning, the loom, mending (#962)

Erebor's `f_vyroba_latky`, `scripts/Basic/skills/Crafting/Spinning`. Double click wool or cotton on you and target a spinning wheel, or thread or yarn and target a loom, in sight. A batch of the source goes at once, the wheel turns, and 3 s later the product is in your pack. One job at a time. Each job can raise Tailoring by the `skillgain` formula.

A batch is 6 of the source, times Tailoring / `skillPerBatch`, and at least 6. Wool and cotton give `yieldPerFibre` each, thread and yarn give `yieldPerThread` of cloth. These three are `mnozkoef`, `pocet_z_vlny` and `pocet_z_nite`, which aren't in the archive: **the values on .settings are guesses**.

A sewing kit on clothes repairs them from 40.0 Tailoring with thread on you, each repair taking a point of maximum durability. The roll is Erebor's to the letter and looks backwards: it fails when a roll of 0 to 1200 is under your skill, so a higher skill fails more. Failing takes a point of durability and a thread; succeeding restores all durability, takes magic thread if you have any, and no thread. Clothes reaching 0 are destroyed.

### Stages (#962)

As Sphere's `Skill_<name>` for each skill. Success is rolled at the start and shown after all the strokes, so a failure takes as long as a success. Difficulty 0 can't fail, a GM never does.

| Skill | Delay (s) | Strokes | Sound and animation |
|---|---|---|---|
| Blacksmithing | 1.0 | 3-6 | hammer each stroke, a swing on all but the last |
| Carpentry | 1.0 | 3-5 | saw every stage, a swing on all but the last stroke |
| Bowcraft | 2.0 | 1 | sound + salute every stage |
| Tailoring | 2.5 | 1 | scissors on success only |
| Tinkering | 2.5 | 1 | none |
| Alchemy | 2.0-1.0 | one per reagent type + 1 | grinding each stroke, an emote if under 5 people are in the sector |
| Cooking | 2.0 | 1 | none |
| Inscription | 2.5 | 1 | one sound at the start |

A made item gets Sphere's quality, 1 to 200, except scrolls and potions: skill picks one of 7 bands, a roll moves it up to 2 either way, and every band but average has its message. Nothing reads quality yet: Sphere's only use is the vendor price, and item values aren't ported. Erebor's enhanced items (quality over 170 at 100.0 skill) wait for magic items. A bundle comes out loose as it's made, as Erebor's @SkillMakeItem fires its @dropon_obj. A failure says Erebor's @Fail line.

Alchemy takes each reagent as its stroke passes and the bottle only on success, so a failure loses the reagents and keeps the bottle. The menu shows a potion only to the professions on Erebor's `potions_` list. Starting drops a run bow from the hands to the pack.

Inscription needs what casting the spell needs: full mana, the skill, the spell in a book on you, its reagents. It costs half the spell's mana at the start of each attempt, and the menu shows only what the profession's `spells_scribe_` list allows. A failure costs the whole recipe on top of Sphere's part. Recipes are generated, see [the converter](../06-tooling/sphere-shard-converter.md).

Any new attempt checks the requirements and materials first, as Sphere does, instead of finding out after the strokes.

Blacksmithing needs a forge (`t_forge`, item or static) within 3 tiles at every stage and turns to face it; lose it and the work stops with nothing lost. The menu needs a smith's weapon (`t_weapon_mace_smith`) in the hand. It opens from ingots, or from the hammer targeted on ingots; the hammer on anything else repairs it. An anvil does nothing on a click, repairing needs one near.

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
