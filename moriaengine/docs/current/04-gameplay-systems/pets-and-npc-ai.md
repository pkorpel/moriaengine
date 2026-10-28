# Pets & NPC AI

The NPC "brain" tick (#18) and the taming/pet-ownership mechanic built on top of it.

## `NPCAiTimer` - the think loop

`NPCAiTimer` (`scripts/Basic/AI/NPCAiTimer.cs`) is attached once per NPC
(`EnsureInstalled`, called from `NPC.On_Create`) as a plain recurring `BoundTimer` - the
same proven mechanism used elsewhere in this codebase (`CriminalExpiryTimer`), chosen
deliberately over the separate Plugin/`TimerPluginDef` machinery
(`RegenerationPlugin`-style) because that relies on a hand-paired `*.Generated.cs` file
whose generation pipeline isn't exercised for `scripts/Basic`'s dynamic boot-compile
step - a plain `[SaveableClass] BoundTimer` has no such dependency. See
[Tags & timers](../02-engine-core/tags-and-timers.md) for `BoundTimer` in general.

Each tick (`ThinkIntervalSeconds = 1.5`): if the NPC has, or finds, a living target,
either fight it (walk into weapon range, then let the existing
`WeaponSkillTargetQueuePlugin`/`WeaponSkillDef` machinery handle actual swings) or flee
it (below `FleeHealthFraction = 0.2` health); otherwise wander loosely around its home
point. Target selection is delegated to `NPC.ShouldAggroOn`, kept overridable per
NPC/NPCDef without touching the tick loop itself.

`AIState` (Idle/Wandering/Fighting/Fleeing/Following/GoingToPoint) is informational only
- recomputed fresh every tick from current conditions, never persisted.

### A real "NPC swings but never hits" bug, found and fixed

`AddTarget` -> `FightCurrentTarget` always starts a *brand new* `SkillSequenceArgs` and
calls `PhaseSelect()` on it, and starting a new skill sequence aborts whatever sequence
was already running. Calling `AddTarget` on every single 1.5-second think-tick (instead
of only once per engagement) was aborting the in-progress Drawing/Striking phase before
it could land a hit, since the weapon skill's own internal retry loop runs on a 1-second
cadence - the external 1.5s tick restart interrupted it almost every time. Fixed by
tracking `lastEngagedTarget` separately from `target`, so `AddTarget` is only called
once per new engagement, not every tick.

## Wandering and home

`hasHome`/`homeX`/`homeY`/`homeZ`/`homeM` are persisted - an NPC remembers where it was
spawned. `WanderLeash` (`NPCAiTimer.cs:70`, default 6 tiles) bounds how far it wanders
from home; see [Spawning](spawning.md) for how a spawn gem's own `Homedist` setting now
actually reaches this field (it didn't, before #206).

### Home-return now uses A* (#211)

The "too far from home, head back" branch now calls `StepTowardsSmart()` - the same
A*-backed helper (`scripts/Basic/AI/PathFinding/AStar.cs`) already used by Follow/"come"
and combat-approach - instead of a bare direct step. A* already existed in the engine but
was never wired into this one movement behavior; it now is the only movement path that
*wasn't* using it before this fix.

**Terrain-fragility lesson from building/testing this**: several attempts at a live
protocol test proving an NPC routes *around* a placed obstacle failed, not because the
fix was wrong, but because the test characters kept landing on a tiny elevated platform
with zero legal moves in any of the 8 directions (unrelated terrain data, documented
elsewhere in this codebase's combat test files). The fix shipped on the strength of being
a 1:1 reuse of an already-proven production helper, plus a clean regression suite run,
rather than a dedicated multi-tile-obstacle test - a future session building a genuine
multi-tile-movement test should probe for open terrain first (`Map.CheckMovement` in all
8 directions) rather than assuming "near an existing spawn point" is safe.

### Characters block NPC steps (#767)

NPCs can't step onto a tile another living character stands on. A refused direct step tries
the two neighbouring directions, and `StepTowardsSmart`'s AStar routes around a character-occupied
first step. Pets and summons follow the same rule. See
[Movement & character collision](movement-and-collision.md).

## Taming and pet ownership

Taming a valid target sets the NPC's `ControlMaster` to the tamer - verified via a real
end-to-end gathering/taming test (`GatheringTests.TamingSetsControlMaster`). `IsPetOf`
is the general ownership check other systems reuse rather than inventing their own (see
[Economy: banking & vendors](economy-banking-vendors.md)'s player-vendor
`CanVendorBeControlledBy`, which is just `IsPetOf` under a different name).

## See also

[Spawning](spawning.md) for how NPCs come into existence and get replaced on death.
