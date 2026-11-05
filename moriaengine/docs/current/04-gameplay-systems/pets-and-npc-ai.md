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

### NPCs sleep where no player is near (#1201)

As Sphere's sleeping sectors. `Regions.ActivityGrid` (GameCore) cuts each map plane into
areas of 64x64 tiles. An area is awake while a player (online, or lingering after logout)
stands in it or in one of its 8 neighbours, and for `npcAreaAwakeSeconds` after the last
of them left: 300 s, in steamengine.ini's `[features]`; 0 turns sleeping off. At boot
nothing is awake until a player arrives. Players are counted where a sector's player
list changes (`Sector.AddPlayer`/`RemovePlayer`), so steps, teleports, logins and logouts
all count.

- **Falling asleep** (`NPCAiTimer.TryFallAsleep`, at a think, after `CaptureHome`): in an
  asleep area, an NPC with nothing to do pauses its own timer (`DueInSeconds = -1`) and
  costs nothing until it wakes. Nothing to do means: no target, not fleeing, not held or
  frozen, no "go" point or guarded item, and not following someone it could follow (a
  pet of an offline owner, or a wild NPC a GM once stopped, only stands, so it sleeps).
  `NeverSleeps` (saved) keeps one awake. One beyond its leash is put back home as it
  falls asleep: the walk nobody would see.
- **Waking** (`NpcSleep`): an area that wakes resumes every sleeper in it; a sleeper
  moved into an awake area (a GM's summon, a teleport, a dismount, a pet out of a
  vendor) wakes at once; `EngageTarget` (pet orders, guards called, a hit from another
  NPC) and `FleeFrom` wake it; a load wakes every saved sleeper (`NPC.On_AfterLoad`),
  and each falls asleep again at its first think if nobody is near. The first think
  after waking is staggered within one think period.
- **While asleep:** nothing from `OnTimeout` runs, so neither `Think` nor
  `MonsterSpecialAttacks.OnAiTick` (dragon regeneration, backstab hiding, mass leap and
  dispel bomb ticks, dragon brain sight, @NPCSeeNewPlayer): all of them need a player
  near or a fight. Its other timers go on: regeneration (`RegenerationPlugin`), poison
  and bleeding, spawn gems, an elite's escape and its 2 s regeneration tick (which only
  heals while hurt). Guard pursuit and boss timers only run in fights.
- A pet whose owner logs in far from it stays asleep until someone comes near; before,
  it would have walked across the map to them.

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

### Home-return: a straight step first, A* when refused (#211, #1199)

Since #1199 (`NPCAiTimer.WalkHome`) an NPC past its leash takes a straight step toward home, as
Sphere's NPCs walk, and calls `StepTowardsSmart()` only when that step is refused or while it is
still walking a path it planned to home, past the usual 8-step replan (so it doesn't step back
into the pocket it was routed out of).
Before, leash-edge NPCs searched on nearly every step: 1,475 searches per 30 s on the idle design
world, 15 after (performance-review.md, problem 7). The history below is #211's.


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

### Ridden mounts don't think (#1152)

A mount being ridden is out of the world (`Disconnect()` at the spot where it was mounted), and so is a pet stocked in a
player vendor. `NPCAiTimer` does nothing for a disconnected NPC, as Sphere doesn't tick one: no thinking, no abilities.
Before, a ridden mount kept "following" its own rider out of the world, and after the rider logged off it tried to path
to them every tick (the user's ridden zostrich filling the log with "found NO path"). A pet on foot whose owner logs out
just waits: a disconnected owner isn't followed.

### Unseen targets (#1038)

NPCs don't see hidden or invisible characters, as in Sphere. One that was fighting such a character breaks off, leaves war mode and goes back to wandering, and none picks it as a new target. It sees it again the moment that ends. A monster with `e_npc_detect` keeps at it, as that ability reveals people while fighting.

### How NPCs pick spells (#1033)

Monster abilities aren't chosen by this AI. Each fires from its own hook (a hit, a swing, a think
tick) with its own chance and cooldown. Only spells are chosen, in `NpcSpellChoice`: every spell the
NPC could cast this tick gets a score, the highest goes off, and if none scores above 0 the NPC
just fights. One cast every 4 s or the spell's cast time, shared by all its spells.

| Spell | Cast when |
|---|---|
| heal | under `npcHealBelowPercent` of its hits (75) and the heal wouldn't be wasted; the more hurt, the higher it scores |
| cure | it's poisoned |
| buff (reflection, protection...) | it doesn't have that effect; scores above most attacks, so it buffs first |
| dispel | the target is a summon |
| mana drain | the target has Magery and over half its mana |
| paralyze | one time in 3, target not paralyzed |
| curse | target not cursed; first choice while it's at full health |
| poison | one time in 4, target not poisoned or immune, poison resist under 75% |
| damage | by damage after armor and the target's resists, a little random; more likely on a paralyzed target |
| weaken, clumsy, feeblemind | target doesn't have it, only when nothing better is available |
| summon | fewer than `npcMaxSummons` (2) of its own summons nearby |
| field | one time in 4 |

A target with reflection gets the NPC's weakest damage spell, as it comes back at the caster, and no
paralyze, poison, curse, debuff or mana drain. Summons and fields land at the target's feet.

The order is Erebor's `ai_returnspell`. Not ported: "cast fast when attacked", as NPC casts here go
off at once and can't be interrupted; polymorphed targets, as Dispel doesn't end a polymorph here;
`tag.lecitel` healers, who heal friends. Spell power is a flat 700.

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
  moved more than 2 tiles, or every 8 steps. A detour around a blocked passage (#1031) is walked
  to its end, unless the target moves 8 tiles.
- **Back-off after a failed search** (#1199): a search that doesn't reach its target because of
  terrain is remembered, and for `npcFailedPathRetrySeconds` (8) the NPC doesn't search again from
  within 4 tiles of where it searched toward a spot within 4 tiles of that target: it steps straight
  at the target or stands. Toward a spot (home, a "go" point, a guarded item) the partial way it got
  is walked to its end, from where it may search afresh; toward a character it's replanned as usual,
  as the character may move to where it can be reached. Searches turned down by characters keep
  #1031's own timing (they're retried every think, so a blocker stepping aside is noticed). Applies
  to every caller: follow, go, guard item, combat approach, walking home.
- **Testable core**: `AStar.FindPath` only takes a step delegate. `FindPathOnMap` adapts
  `Map.CheckMovement` to it, and unit tests run the core on synthetic grids. All search state
  is per call, so the core is reentrant.

## Taming and pet ownership

See "Taming" in skills-overview.md for the rules. Taming sets the NPC's `ControlMaster` to the tamer - verified via a real
end-to-end gathering/taming test (`GatheringTests.TamingSetsControlMaster`).

`Character.IsPetOf(cre)` is the general ownership check - Sphere 0.99z8's `NPC_IsOwnedBy`
(#808): `cre` owns this character if it's the `ControlMaster`, on the pet's friend list
(`PetFriends`, #774), or a GM above its plevel. Until #808 it was a `//TODO` that only said yes
to GMs, so **players couldn't mount their own tamed horse, rename it, equip it or touch its
pack**. Everything that asks "is this mine?" goes through it: mounting (`IsMountableBy`),
renaming, equipping items on a pet, putting items into / opening a pet's pack (an owner's
double-click on a pet's pack opens it instead of snooping) and giving items. (This page used to
say player vendors reuse it via `CanVendorBeControlledBy` - no such method exists any more.)

Double-clicking your own pack horse/llama (models 0x123/0x124, `IsPackAnimal`) opens its pack -
that's how a pack animal is loaded (Sphere). A mountable pet is mounted; anyone else gets the
paperdoll as before.

## Giving items to NPCs (#807, #808)

Dropping an item on an NPC (`NPC.On_PutItemOn`) follows Sphere 0.99z8's `NPC_OnItemGive` as far
as Erebor's content reaches it (`scripts/Basic/CharClasses/NpcItemGive.cs`):

1. `e_item_refuser` NPCs turn it down with their line (#807).
2. A GM's item goes into the NPC's pack.
3. The NPC's owner/friend:
   - a **humanoid** pet eats food its `FoodTypes` match while hungry ("Mmm. Thank you."; not
     hungry: "No thank you."), otherwise carries the item in its pack ("Tell me to drop this when
     you want it back."; gold also gets "Thank thee."), unless that would overload it ("I'm too
     weak to carry that.", `MovementStamina.MaxCarryWeight`);
   - an **animal** pet eats food it's hungry for (same formula as `.Feed`,
     `PetFeedingSystem.TryFeedWith`); anything else is refused like below - load a pack animal
     through its opened pack instead.
4. Everyone else is refused. Sphere first asks `NPC_WantThisItem` (DESIRES, food while hungry,
   vendor buys it), but on Erebor's content that's never true for an unowned NPC (DESIRES only on
   mounts/elementals and not converted; only pets track hunger) - so gold dropped on a banker is
   refused too (user decision). The NPC's `@NPCRefuseItem` runs (Erebor's guards: "Mne nemuzes
   podplatit" - ported by the converter for the 5 simple `SAY("...")` bodies); unless it returns 1,
   the giver sees "They don't appear to want the item." over the NPC.

A refused (or partly eaten) item bounces back to the giver. Tests: `Tests/Protocol/NpcItemGiveTests.cs`.

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
