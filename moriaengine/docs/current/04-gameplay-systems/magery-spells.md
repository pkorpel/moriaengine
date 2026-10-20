# Magery & spells

Spellcasting is `MagerySkillDef` (`scripts/Basic/skills/Magery/Magery.cs`) driving the
generic skill-sequence machinery from [skills-overview.md](skills-overview.md), where each
individual spell is a `SpellDef` (`scripts/Basic/skills/Magery/Spells/SpellDef.cs`) with its
own nested trigger chain. Casting restriction by profession is enforced separately, in
`ProfessionPlugin`.

## Casting flow

Two entry points start a cast: `MagerySkillDef.TryCastSpellFromBook` (`Magery.cs:39`, used
when double-clicking a spell icon in an open spellbook - `SpellBook.HasSpell` confirms the
book actually contains it) and `TryCastSpellFromScroll` (line 66, requires the scroll be in
the caster's own top-level inventory). Both just build a `SkillSequenceArgs` for
`SkillName.Magery` with the `SpellDef` as `Param1` and the book/scroll as `Tool`, then call
`PhaseSelect()` - from there it's the same 5-phase sequence every skill uses, with Magery's
own overrides:

- **`On_Select`** (`Magery.cs:86`) immediately deflects to `SpellDef.Trigger_Select`
  (`SpellDef.cs:378`) and cancels the base sequence - the rest of Select is the spell's own
  job, not Magery's generically. `SpellDef.Trigger_Select` either auto-targets self
  (`SpellFlag.AlwaysTargetSelf`) or opens a real target cursor
  (`SpellTargetDef`/`On_TargonChar`/`On_TargonGround`/etc, lines 918-979) for spells that can
  target something.
- **`On_Start`** (`Magery.cs:110`) is resource/mana gating: checks `Mana >= manaUse`
  (`SpellDef.GetManaUse`, scrolls cost half, line 242), then - for book casts only, scrolls
  skip this - validates `Requirements` (must be present, not consumed) and consumes
  `Resources` (`ResourcesList.ConsumeResourcesOnce`), showing the resource-list's own
  "missing X" message on failure. On success it deducts mana, plays the cast animation,
  aborts any other running skill, and schedules the Stroke after `spell.CastTime` seconds -
  along with speaking the spell's rune words out loud if it has any (`GetRuneWords`, a
  fixed per-letter LOTR-flavored syllable table, lines 256-286 - purely a localization/flavor
  detail of this shard, not functional).
- **`On_Stroke`** (`Magery.cs:160`) re-validates the book/scroll is still valid and re-checks
  reachability, then rolls `CheckSuccess(self, spell.GetDifficulty(isFromScroll))` (scrolls
  are also half difficulty).
- **`On_Success`/`On_Fail`/`On_Abort`** delegate to `spell.Trigger_Success` or a shared
  `Fizzle` effect (visual puff + sound + "The spell fizzles." cliloc, lines 209-213).

## `SpellDef`'s own effect-application chain

Once Magery's skill-sequence calls `SpellDef.Trigger_Success` (`SpellDef.cs:430`), the spell
resolves its OWN target-type-specific effect chain, independent of the generic skill
triggers:

1. Determines spell power: either mind-power-based (`SpellFlag.UseMindPower`, using
   `MindPowerVsP`/`MindPowerVsM` minus the target's `MindDefenseVsP`/`MindDefenseVsM`, scaled
   `*10` to match skill-value units) or the caster's raw `Magery` skill value
   (`GetSpellPowerAgainstChar`/`GetSpellPowerAgainstNonChar`, lines 593-635).
2. Dispatches to `Trigger_EffectChar`/`Trigger_EffectItem`/`Trigger_EffectGround` based on
   what the resolved target actually is and which `SpellFlag.CanEffect*` bits the spell
   declares (lines 472-507) - each is its own cancellable trigger chain
   (`@causeSpellEffect` on the caster, then `@spellEffect` and `@effectChar`/`@effectItem`/
   `@effectGround` on the target).
3. For `SpellFlag.IsAreaSpell` spells, additionally walks every `Thing` within
   `EffectRange` of the target point (line 517) and re-runs the same per-target dispatch,
   with one notoriety-aware skip: a beneficial area spell skips anyone "more enemy" than the
   main target, a harmful one skips anyone "more friendly" (via
   `Notoriety.GetCharRelation` - see [notoriety.md](notoriety.md)), so an area heal doesn't
   accidentally buff a hostile bystander and an area damage spell doesn't hit an ally.
4. `CheckSpellPowerWithMessage` (line 567) gates the whole thing: `CanEffectDeadChar` must be
   set to affect a dead character at all, and a harmful spell with `SpellPower < 1` (fully
   resisted) sends "You feel yourself resisting magical energy" and does nothing.

Region-based magic blocking (`CheckPermissionOutgoing`/`CheckPermissionIncoming`, lines
718-771) checks `RegionFlags.NoMagicIn/Out`, `NoBeneficialMagicIn/Out`,
`NoHarmfulMagicIn/Out` on both the caster's region and the target's region, independent of
the ATTR-flag/notoriety checks elsewhere.

## Profession-based spell restriction

Nothing in `SpellDef`/`MagerySkillDef` itself restricts which spells a character can cast -
that's layered on separately by `ProfessionPlugin.CheckCancelSkill`
(`scripts/Basic/Professions/ProfessionPlugin.cs:72`), hooked into both `On_SkillSelect` and
`On_SkillStart` (the generic per-character skill hooks every `SkillDef` fires through, see
[skills-overview.md](skills-overview.md)). For a non-GM character attempting `SkillName.
Magery` specifically, it checks `profession.AllowedSpells.Contains(spell)` and cancels with
a localized "you can't cast this" message if the spell isn't on that profession's list. This
means spellbook *contents* are irrelevant to what a character can actually cast - a
profession's `AllowedSpells` `.def` list is the real gate, decoupled from whichever
spellbook item happens to be in a character's pack (every profession, including non-casters,
is given the same plain empty `i_spellbook_3` at creation - see
[professions.md](professions.md) and [races-and-nations.md](races-and-nations.md) for why).

## Generic summon-pet infrastructure (#483/#485)

`SummonSpellDef : DurableSpellDef` (`scripts/Basic/skills/Magery/Spells/SummonSpells/
SummonSpellDef.cs`) is the one shared mechanism every summon-shaped spell needs - Blade
Spirits/Summon Creature (Circle V, not yet authored), Energy Vortex/the 4 elementals/Summon
Daemon (Circle VIII, not yet authored), and eventually Necromancer's Animate Dead/Summon
Undead (#158). A `.def` entry just needs a `summonChar=<NPCDef>` line (e.g. the real
mage-tier summon content already in `scripts/converted/chars/summons.def` - `c_vortex_mage`/
`c_elem_fire_m`/etc, NOT the overpowered wild monster chardefs of a similar name) plus the
usual `DurableSpellDef` `DURATION=`.

`On_EffectGround` creates the NPC at the target point (`NPCDef.Create`, no fit-check -
follows `MonsterAmbushSystem.TryAmbush`'s own simpler established precedent for spawning a
monster near a point rather than `SpellEffectItem.CheckPositionForItem`'s item-height-based
check), sets `ControlMaster` to the caster (reusing exactly what Taming already proved
works - `NPCAiTimer`'s Following AI state, `ShouldFollowOwner` defaulting true, no new pet-AI
code needed) and `Flag_WarMode = false`, then attaches a `SummonDespawnTimer` (mirrors
`SpellEffectItem.SpellEffectTimer`'s own "delete on timeout" shape) for
`GetDurationForValue(spellPower)` seconds.

**Dispel integration, #486 (shipped)**: see the next section - `Character.On_Dispell` now
deletes a summoned creature outright (checked via `SummonSpellDef.IsSummonedCreature`) rather
than only being removable by its own timer or normal death.

## Dispel actually affects characters now (#483/#486)

`Character.On_Dispell` and `Item.On_Dispell` were both empty virtuals. `DispellDef` (`s_dispel`/
`s_mass_dispel`/`s_dispel_field`, `scripts/Basic/skills/Magery/Spells/Dispell/Dispell.cs`)
correctly calls `target.On_Dispell(...)` on whatever it hits, but only `WallSpellItem.
On_Dispell` (deletes the field/wall item) and `EffectDurationPlugin.On_Dispell` (removes a
book-sourced buff, but only if the PLUGIN itself is the target - which nothing could ever
arrange, since a plugin can't be a spell's direct target) did anything. Casting Dispel
directly on a buffed character removed nothing.

Fixed: `Character.On_Dispell` now (1) deletes the character outright if it's a summoned
creature (`SummonSpellDef.IsSummonedCreature`, #485) - dispelling a summon means the whole
creature goes away, not a buff on it - or (2) otherwise snapshots every dispellable
`EffectDurationPlugin` the character is carrying into a list FIRST, then calls `On_Dispell` on
each from that snapshot - `On_Dispell` deletes the plugin, and `GetAllPlugins()` enumerates the
live backing tags dictionary directly, so deleting while still iterating it would throw.
`Dispellable` is unchanged - still keyed off `EffectFlag.FromBook` (potion effects stay
non-dispellable, matching that property's own pre-existing rule).

`Item.On_Dispell` investigated and left as-is - confirmed no item currently ever receives an
`EffectDurationPlugin`-based effect (only characters do, via `DurableCharEffectSpellDef`), so
there's genuinely nothing to dispel on an item yet beyond `WallSpellItem`'s own existing
override.

## Fizzle/difficulty (#483/#484)

`SpellDef.Difficulty` used to return `0` whenever a spell's `difficulty` field wasn't
explicitly set in its `.def` - true for every spell except the ones in `DamageSpells/
damageSpells.def` (the only file that ever sets `difficulty=` directly). `CheckSuccess`
(the same S-curve roll every other skill uses) was correctly wired but fed `0`, meaning
casting essentially never fizzled once mana was sufficient - a standing `//TODO extract
from requirement ResList` sat on the getter. Fixed: when `difficulty` is unset, it falls
back to the spell's own `Requirements=X Magery` skill threshold (if present) via
`ResourcesList.NonMultiplicablesSublist`/`SkillResource`, scaled by a GM-tunable
`MagerySettings.spellDifficultyFallbackMultiplier` (default `1.0`). At exactly that skill
threshold, `CheckSuccess` now gives the same ~50% chance every other skill's own difficulty
model gives at its own threshold - scaling naturally as skill rises above/falls below it,
instead of being unconditionally guaranteed.

**Known follow-up, not fixed here**: the `Requirements=`/`difficulty=` numbers themselves
are inconsistently authored across spell `.def` files (e.g. Harm's `Requirements=200`
vs Fireball's `Requirements=30.0` for a lower circle) - likely converter noise, not real
Erebor data. This affects the ABSOLUTE difficulty each spell now derives, not whether the
mechanism itself works. Left for the per-circle verification phases (#487-490) to clean up
spell-by-spell, not addressed centrally.

## Skill-gain-on-cast - confirmed not wired, by design

`MagerySkillDef` never calls `SkillGainSystem.TryGainSkill` anywhere in its trigger chain -
casting a spell does not raise Magery skill through use. Confirmed intentional: Magery is a
leveling-point skill under the profession model (`LevelingSystem.TrySpendSkillPoints`), not
one of #140's 13 use-trained skills (Mining/Lumberjacking/Fishing/Cooking/Taming/Lockpicking/
Blacksmithing/Carpentry/Bowcraft/Alchemy/Tailoring/Tinkering/Inscription). No code change
needed or planned here.

## Reagent-less casting - confirmed intentional, not a gap

Every `RESOURCES=` line across every spell `.def` is commented out, so `spell.Resources` is
always `null` and `On_Start`'s `ConsumeResourcesOnce` call never actually consumes anything -
only `spell.Requirements` (a skill-threshold gate, not a consumable) is checked. Confirmed
against the real Erebor source (`magie.scp`): casting there is mana-only too, no reagent
check at all. This is a faithful port, not something to "fix" by wiring reagent consumption
back in.

## Settings and known content gaps

`MagerySettings` (referenced from `CombatCalculator`/`Magery.cs` as `mindPowerIntModifier`/
`bareHandsMindPowerVsP`) is the matching `.settings` category for the mind-power side of
spellcasting math - see [settings-dialog.md](../03-admin-tooling/settings-dialog.md). A
nested `SpellsSettings` page (`MagerySettings.spells`) covers damage-spell `Effect`/
`DamageType` mass-editing and the new `spellDifficultyFallbackMultiplier` above.

**Combat-system initiative, Mage/spellcasting (#483)**: a much larger follow-up than the
content gaps below - full spell-by-spell analysis against the real Erebor source/wiki,
covering fixes, missing spells (Invisibility/Reveal/Mana Drain/8 summon-shaped spells), and
a generic summon-pet infrastructure. See #483 and CLAUDE.md's own tracking entry for the
full phase breakdown - this page will get further updates as each phase ships.

Two real, already-fixed content bugs (both closed under #159, found while authoring
profession `.def` files): `mage.def`'s `allowedSpells` had a typo
(`s_paralyze_field` instead of the real `s_paralyzation_field`) and listed two
spells that aren't actually implemented (`s_invisibility`/`s_reveal`) as castable. Both
fixed in the `.def` content itself, not in `SpellDef`/`MagerySkillDef` code.

**Necromancer's entire spell identity is unimplemented** (#158, open) - Animate Dead, Bone
Armor, Summon Undead, Mana Drain, Mana Vampire, Energy Vortex, Summon Creature, Reveal, and
Fire Bolt have no `SpellDef` content at all in this engine. The Necromancer profession
exists and can be assigned, but has nothing resembling its real spell kit to cast.
