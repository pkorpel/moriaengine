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

### Pathfinding (#769)

`StepTowardsSmart` (follow, combat approach, "go", home-return) uses
`scripts/Basic/AI/PathFinding/AStar.cs`. Before #769 the "A*" was really a greedy best-first
search: its heuristic was the squared distance times 121, which swamped the step cost. It also
needed an exact-Z goal match, so a target standing slightly off its tile's walkable Z burned the
whole 1000-iteration budget and failed. On top of that it had a colliding hash, used static
shared state, and replanned from scratch every think-tick. Now:

- **Real A\***. UO steps cost 1 diagonally too, so the heuristic is the Chebyshev distance,
  with ties broken toward the target. A node gets re-queued when a cheaper route to it turns up.
  Paths are shortest: `AStarTests` checks them against a BFS on 60 random grids.
- **Goal tolerance** (`AStar.Options`): `ZTolerance` (default 10) and `StopWithin`. A character
  target counts as reached when adjacent, because its own tile is occupied.
- **Bounded**: the search stays within `WindowMargin` (16) tiles of the start/target box, capped
  at `MaxIterations` (1000).
- **Partial paths**: when the target can't be reached, the path to the closest node reached is
  returned (`Result.ReachedGoal = false`), so the NPC still closes in. A null result only means
  no first step exists at all; then `StepTowards`' direct step/sidestep is used.
- **Path caching**: `NpcPathCache` on the `NPCAiTimer` (transient) walks the planned path. It
  re-plans when a step fails, when the NPC isn't where the path expects, when the target has
  moved more than 2 tiles, or every 8 steps.
- **Testable core**: `AStar.FindPath` only takes a step delegate. `FindPathOnMap` adapts
  `Map.CheckMovement` to it, and unit tests run the core on synthetic grids. All search state
  is per call, so the core is reentrant.

## Taming and pet ownership

Taming a valid target sets the NPC's `ControlMaster` to the tamer - verified via a real
end-to-end gathering/taming test (`GatheringTests.TamingSetsControlMaster`). `IsPetOf`
is the general ownership check other systems reuse rather than inventing their own (see
[Economy: banking & vendors](economy-banking-vendors.md)'s player-vendor
`CanVendorBeControlledBy`, which is just `IsPetOf` under a different name).

## Pet commands (#768)

Spoken commands are parsed by `PetCommandParser` (`scripts/Basic/CharClasses/`) the way Sphere
0.99z8 does it: `<addressee> <command phrase>`. The addressee is `all`/`pets` (every pet you own
within speech range), or one pet's own name or creature type name ("horse come"), ignoring case
and a leading a/an/the. Commands can be several words. The old parser looked only at the *last
word*, so "all follow me", "all guard me" and "all stay" were silently ignored.

| Say | Effect |
|---|---|
| come, follow me | follow the speaker |
| guard me, defend me | follow the speaker and join fights against whoever attacks them (`IsGuarding`) |
| guard | target an item lying on the ground: the pet stays by it (`GuardedItem`), and while it's within 6 tiles nobody but its owner/friends/GMs can pick the item up or take anything out of it - whoever tries gets "<pet> is guarding that." and is attacked. Target a character to protect them instead, or yourself for "guard me". Guarding ends with any new order or once the item is off the ground (#774) |
| stay, stay here, stop | stand put, drop any fight, stop guarding |
| follow | target someone to follow |
| go | target a spot; given up with "Sorry" after 5 think-ticks without progress |
| kill, attack | target someone; refused for the speaker's own pets |
| transfer | target the new owner (owner only) |
| release, dismiss | release into the wild (owner only) |
| friend | target a player who may give the pet orders too (`PetFriends`, saved); owner only. Friends can't transfer, release or add friends. Cleared on transfer/release (#774) |
| drop | the pet empties its backpack at its feet, keeping its own `attr_newbie` starting gear; "I'm carrying nothing." otherwise (#774) |
| status | each pet's health and current orders, as a sysmessage |

Every order replaces what the pet was doing, a fight included (Sphere's `Skill_Start`). The pet
turns to its master and says "Yes Master" or "Sorry". Non-human pets play their idle/anger sound
instead. The owner, friends, and GMs (any NPC in range) can give orders.

**Following keeps up now.** A pet following or under a "go" order that's still more than 1 tile
away thinks again after `CatchUpWalkSeconds` (0.45s), or `CatchUpRunSeconds` (0.25s) and running
once more than `RunWhenBehindBy` (3) tiles behind. Before, it took one step per 1.5s think-tick
regardless, far slower than a walking player.

**Fighting back** (`.settings` -> "NPC Aggro"). `npcsDefendThemselves` makes an attacked NPC fight
its attacker unless it's already fighting (Sphere's `OnHarmedBy`). That includes a rebel pet
fighting the owner who hit it. `petsAssistOwner` sends idle pets following their master after
whatever the master attacks, like Sphere's following pets. Hitting your own pet isn't a fight
your other pets join.

## See also

[Spawning](spawning.md) for how NPCs come into existence and get replaced on death.
