# Notoriety & murder counts

`Notoriety` (`scripts/Basic/Notoriety.cs`) is a static helper class, not a per-character
component - all state it reads/writes lives as tags/timers on the `Character` itself
(murder count, a criminal-flag timer), everything else is computed live from relationships
(party/guild/realm).

## Criminal flag and murder count

`FlagCriminal` (line 57) attaches a `CriminalExpiryTimer` (a `BoundTimer`, see
[tags-and-timers.md](../02-engine-core/tags-and-timers.md)) for `CriminalFlagSeconds`
(default 120 - the real classic-UO 2-minute window; settable, mirroring
`LightAndWeather.DayLengthSeconds`'s own pattern, specifically so a test can shorten it
without waiting out 2 real minutes). `IsCriminal` is just `HasTimer` on that key - the flag
expires itself for free the moment the timer fires, no explicit "unflag" step needed.
Murderers (`IsMurderer`, `GetMurderCount(self) >= MurdererThreshold` where
`MurdererThreshold = 5`, the classic UO value) never get criminal-flagged on top - already
the worst notoriety tier, flagging would add nothing (line 58).

`OnHostileAction(victim, enemy)` (line 64) is the entry point that actually flags someone
criminal - called whenever `enemy` commits a hostile act against `victim`. It no-ops unless
both parties are players (**"no AI/monster notoriety yet"**, line 69, a stated scope
boundary, not a bug), and skips flagging entirely if the act was justified: victim already
criminal/murderer (line 71), same party (`Party.AreInOneParty`), same guild, or a declared
guild war between the two guilds (lines 74-84).

`RegisterMurder(victim, killedBy)` (line 89) is the matching death-time entry point -
increments `killedBy`'s murder count tag unless the victim was already
criminal/murderer (killing a criminal isn't a murder). Same player-only scope restriction as
`OnHostileAction`.

## Realm-based coloring (#163)

`GetHighlightColor(viewer, self)` (line 102) is what actually drives the in-client nameplate
color. Order of checks: self-view is colorless, then murderer (red) / criminal (gray-ish
"Criminal" color) take priority over everything else, then party (yellow), then guild
relation (allied/attackable via `Guild.AreAllied`/`AreInWar`) - and only after all of that,
a **realm-based baseline** (lines 132-149): Gondor renders `HighlightColor.Innocent`
(blue), Mordor renders `HighlightColor.Murderer` (red) purely as a rendering choice,
completely decoupled from the real murder-count mechanic above (a Mordor character who's
never killed anyone still renders red, by design - the same visual slot a *real* murderer
of either realm already occupies, which is always checked first and wins).

This is a deliberate, confirmed-with-the-user departure from how the original Sphere
reference (Erebor) did it: Erebor's own source fakes Mordor-red by seeding every
Mordor-aligned character with a starting `kills=30000` to trip the real murderer threshold
(confirmed via `newbie_portals.scp`'s `startnation` function). moriaengine instead checks
`Character.Realm` directly as an independent rendering layer, leaving the real murder-count
mechanic honest (a Mordor character's actual `GetMurderCount` stays 0 until they really
commit one). `GetCharRelation(a, b)` (line 152) carries the same realm logic into the
non-visual "are these two hostile" question used by area-spell targeting (see
[magery-spells.md](magery-spells.md)) - opposing realms are `AlwaysHostile`, same realm is
`Allied`, checked after guild relations so an explicit guild war/alliance between
same-realm or cross-realm characters still overrides the realm default.

`Realm` itself, and race-to-realm/nation assignment, are covered in
[races-and-nations.md](races-and-nations.md).

## Region-based combat/exp-loss exemptions

Two related but deliberately different-scoped checks:

- `IsAttackBlockedByRegion(attacker, target)` (line 194) - true if either party stands in a
  `Flag_Safe`/`Flag_Guarded`/`Flag_NoPvp` region, UNLESS that region is also `Flag_Arena`
  (arenas explicitly override the other three). Used to bar combat from even starting.
- `IsExemptFromExperienceLoss(self)` (line 219) - a **narrower** set of flags
  (`Flag_Safe`/`Flag_Arena` only, deliberately excluding `Flag_Guarded`/`Flag_NoPvp`) used by
  the Leveling system's death-experience-loss mechanic (see
  [leveling-experience.md](leveling-experience.md)/#128). The user's own stated design: a
  guarded town shouldn't grant exp-loss immunity, only true safe zones and arenas should.

Don't conflate these two - a region that blocks combat entirely is not automatically exempt
from the (unrelated) death-experience-loss penalty, and vice versa.

## Known real-world ordering gotcha: `CauseDeath`/`Flag_Dead`

Not part of `Notoriety.cs` itself, but directly relevant to anything that calls
`RegisterMurder`/checks notoriety around a death: `Character.CauseDeath` is a no-op once
`Flag_Dead` is already true, and for a non-player character, death deletes the `Thing`
**synchronously**, inside the same call that reduced HP below 1 (see
[combat.md](combat.md)'s `DamageManager.CauseDamage`/`IsDeleted` guard, and
[corpses-and-death.md](corpses-and-death.md)). Two concrete traps this has caused in test
code (not production bugs, but worth knowing before writing anything that kills a character
and immediately inspects notoriety/skill-sequence state):

1. Killing the same character twice without resurrecting between calls silently does
   nothing the second time - a test (or script) that assumes a second `CauseDeath` call
   re-triggers everything will be wrong.
2. Code re-entering a weapon-skill or spell sequence against a target that a killing blow
   just deleted (same tick, before the caller's own stack unwinds) must check `IsDeleted`
   before touching the target further - see [combat.md](combat.md)'s re-entrancy guard for
   the concrete instance of this.

## Known CI-only flake (#121)

`OwnerAggroTests.HostileFollowerNeverAutonomouslyTargetsItsCommander` fails reproducibly on
CI only (never locally) - a titan-class NPC occasionally acquires its own commander as a
combat target, which `DamageManager.CauseDamage`'s owner/commander backstop (see
[combat.md](combat.md)) should prevent. Root cause not yet confirmed; a draft diagnostic PR
(#119, not merged) exists with instrumentation ready to go once CI capacity allows
re-running it. Treat a failure of this specific test on CI as this known flake first, not
evidence of a fresh regression, until #121 is actually root-caused.
