# Known gaps & tech debt

What's deliberately incomplete, deferred, or flagged as needing a future decision -
consolidated from across `CLAUDE.md`'s session history so it's visible in one place
rather than scattered across many issue numbers. Each entry notes whether it's a
deliberate scope choice (not a bug) or a genuine gap worth picking up.

## Deliberately deferred, not bugs

- **A Nazgûl-specific "shade of black" notoriety color** (user-requested, 2026-10-16) -
  deferred until Nazgûl creatures themselves actually exist in this codebase (check
  Erebor's original Sphere source for them when that work starts). No OSI precedent for
  this at all (real UO has no black notoriety hue) - same spirit as the wild-monster
  orange color already added, a deliberate lore-flavor house rule, not a restoration of a
  real mechanic. See [Notoriety & murder counts](../04-gameplay-systems/notoriety.md).
- **No real player-facing dialog to spend skill/ability/attribute points** beyond the
  `.explevel` buttons wired directly to `LevelingSystem`'s spend methods - a fuller
  redesign is a known follow-up, not started. See
  [Leveling & experience](../04-gameplay-systems/leveling-experience.md).
- **Abilities/talent-trees** - untouched by both the Professions and Races passes,
  pending a possible future redesign the user flagged but hasn't started. See
  [Abilities & talents](../04-gameplay-systems/abilities-and-talents.md).
- **Skill-gain-through-use** for the crafting/gathering skill list (Mining,
  Lumberjacking, Fishing, Cooking, Taming, Lockpicking, Blacksmithing, Carpentry,
  Bowcraft, Alchemy, Tailoring, Tinkering, Inscription) - explicitly deferred until
  that content area becomes the active focus (tracked historically as #140).
- **Experience gates/quests** - the shared `AddExperience` entry point this would call
  already exists; actual gated content is deferred indefinitely, tracked only so the
  idea isn't lost.
- **Multi-killer experience-credit splitting on death** - `CauseDeath` only ever
  identifies one killer; there's no damage-contribution-list mechanism to split credit
  from. Not invented speculatively ahead of such a mechanism existing.
- **House friend/co-owner/ban list editing UI** - the data model (`AddCoOwner`/
  `AddFriend`/`AddBanned`) is ready; no dialog exists yet. See
  [Housing](../04-gameplay-systems/housing.md).
- **House decay / idle-time destruction, interior customization** - out of scope for the
  Housing system as built.
- **A secure container's own open/view action is unrestricted** - only taking items out
  of it is gated by `attr_secure`. A deliberate, defensible scope choice, not an
  oversight. See [House lockdown & security](../04-gameplay-systems/house-lockdown.md).
- **No physical race-selection stone editing** - the selection flow is fixed, not
  reconfigurable via content authoring.
- **Home has no recall mechanic** - `Player.Home` is a one-time placement coordinate
  only; a separate recall feature is explicitly out of scope until designed. See
  [Races & nations](../04-gameplay-systems/races-and-nations.md).
- **No per-vendor configurable NPC stock** - `NpcVendorStock` is one small fixed list
  shared by every basic vendor, a deliberate simplification until real content needs
  per-vendor variety. See
  [Economy: banking & vendors](../04-gameplay-systems/economy-banking-vendors.md).
- **`attr_opened`/`attr_norepair`** - enum values kept for `.def`-compatibility
  completeness, deliberately unused (see the design-decisions log for why each would be
  a net negative to implement).

## Real, open gaps (worth picking up, not yet scheduled)

- **Armor mitigation (#96)** - `CombatCalculator.CalculateArmorClassEffect` discards
  the real armor-class value for a random placeholder in every case except monster-vs-
  player combat, and even that one surviving case shows far less damage reduction from
  equipped armor than hand-calculating from `CombatSettings`'s own weights would predict
  (~1% measured vs. ~20-25% expected). Needs a real design decision (what should PvP/MvM
  mitigation look like) plus further root-causing of the MvP shortfall - deliberately
  not auto-fixed, since the design question isn't this agent's to decide unsupervised.
- **NPC content ~55-60% incomplete** - many monster `.def`s are missing data (experience
  worth, correct weapon types, etc) entirely, a long-tail content-authoring gap, not a
  code gap.
- **Converted weapon `WeaponType` data quality** - almost entirely a blanket
  `//guessed by Converter` value (`OneHandSpike` on nearly everything) rather than the
  real per-weapon type, a direct consequence of
  [SphereShardConverter](../06-tooling/sphere-shard-converter.md)'s inherent "can't
  verify what it can't resolve" limitation.
- **Necromancer's entire spell identity is unimplemented** - Animate Dead, Bone Armor,
  Summon Undead, Mana Drain/Vampire, Energy Vortex, Summon Creature, Reveal, Fire Bolt
  all absent; the profession exists with no spells of its own.
- **Region attack rules cover only a player's attack request** (`Notoriety.IsAttackBlockedByRegion`,
  tested since #1019 by `RegionAttackTests`): monsters, pets, harmful spells and a fight started
  outside aren't checked against safe or no-PvP areas.
- **House "GM release" checklist item has no test coverage** - deed placement,
  overlap rejection, and co-owner/stranger access are all covered; the GM-release path
  specifically isn't.
- **CI hang root cause unconfirmed** - a real, intermittent (roughly 1-in-3 at one point)
  full-boot-tier hang on GitHub Actions' shared runners, striking a different test class
  each time, strongly suggesting runner resource contention rather than a bug in any one
  test. Contained (job-level timeout + `--blame-hang`) but not root-caused.
- **A handful of separate intermittent CI-only connection-race failures**
  (`EconomyTestsA/B`, `SocialTests`, a stuck-NPC-pathing case) - none reproduce locally,
  seen only on CI, not root-caused.
- **The LScript command/property name-shadowing footgun is unfixed at the engine
  level** - every instance found so far (`.kill`/`Character.Kill()`, `.Race`/
  `Player.Race`) was worked around by renaming the colliding *command*, not by changing
  LScript's own member-resolution order. The next collision will hit the same silent
  no-op symptom.
- **RemoteConsole has no automated test** - it is on .NET 8 and in the solution since #29,
  so it compiles everywhere, but it's a WinForms app that only runs on Windows and nothing
  drives it headless. See [RemoteConsole](../06-tooling/remote-console.md).
- **`FieldValue`'s `isChangedManually` tracking has a narrow but real engine-level
  persistence bug class** (#168) - a custom tag-backed Def property loaded from a
  baseline `.def` file gets marked "changed" even when it wasn't, causing it to be
  needlessly re-persisted; a subsequent reload through the generic (not the type's own)
  load path can then silently re-type the value (e.g. `double` -> `int`), breaking the
  property's own type check. Partially addressed (an `isLoadingFromScript` flag now
  prevents the *write* side for one specific load path) but the general mechanism is
  still fragile for any future custom tag-backed Def property following the same
  pattern.
- **Fishing has no loadable skill content at all** in this engine - a pre-existing
  content gap, not caused by anything documented in these pages, discovered while
  authoring the Leveling system's spendable-skills list (which had to switch its
  worked example away from Fishing). See
  [Crafting & gathering](../04-gameplay-systems/crafting-gathering.md).

## Pre-existing regressions found but not always fixed in the same pass

- **`HousingTests.cs`'s hardcoded "+5,+5 from spawn" house-placement offset** has no
  passable footprint near the current starting position (changed separately, for an
  unrelated reason) - a genuine pre-existing regression in that test file, confirmed via
  direct reproduction with zero production-code changes. The fix (reuse
  `TestTeleportToOpenGround` instead of a hardcoded offset, as newer tests already do)
  is known but wasn't applied to the older file in the pass that found it.
