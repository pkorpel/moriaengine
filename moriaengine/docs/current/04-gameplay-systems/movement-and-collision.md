# Movement & character collision

Every step a character takes goes through `AbstractCharacter.WalkRunOrFly`
(`GameCore/AbstractCharacter.cs`), whether it's a player's move-request packet
(`MovementState.Movement`), an NPC AI step (`NPCAiTimer.TryStep`) or an AStar walk. The order is:

1. `CheckCanMove` - can the character take a step at all: held by a strangler vine, too
   exhausted, too overloaded (#778/#779, below). GM players skip it.
2. `Map.CheckMovement` - terrain, statics, items, climb height. Characters are not considered here.
3. `CheckCharacterObstacles` - other characters on the destination tile (#767, below). GM players skip it.
4. `@step` / `On_Step`, then region exit/enter.
5. The actual move and the 0x77 broadcast, then `On_StepTaken` (#779's stamina drain).

A refused player step sends the move-rejection packet (0x21), so the client snaps back.

## Character collision (#767)

Before #767 nothing checked characters, so players and NPCs walked straight through each other.
Now `Character.CheckCharacterObstacles` hands off to `CharacterCollision.CheckStep`
(`scripts/Basic/Movement/CharacterCollision.cs`), a port of Sphere 0.99z8's rules as reconstructed in
EreborNext (`CChar.Movement.Pushing.cs`, `CheckMoveWalkDir`).

**Who counts as an obstacle**: another character on the same X/Y within 16 Z of the destination
(the same body height `Map.CanFit` assumes). Dead, insubstantial and disconnected characters
never block anyone.

**Ghost movers** (dead or insubstantial) walk through everyone.

**NPC movers** never shove. Any obstacle stops them, including pets and summons. When a direct
step is refused only because of a character, `NPCAiTimer.TrySidestepCharacter` tries the two
neighbouring directions (Sphere's `NPC_WalkToPoint` sidestep). `StepTowardsSmart`
passes AStar a `Blocked` check (`CharacterCollision.IsTileBlockedFor`), so a tile a character
stands on within `npcPathCharacterRadius` tiles of the NPC is impassable for the whole search
and the path routes around it. Further away characters are ignored, because they move, and a
cached path that runs into one just fails that step and gets re-planned. If a character leaves
no way to the target for `npcDetourWaitSeconds`, the NPC runs one much wider search and walks
that detour to its end (#1031). If even that finds nothing, it waits at the blockage: NPCs
can't shove. See [Pets & NPC AI](pets-and-npc-ai.md#pathfinding-769).

**Player movers**, against the first obstacle found:

| Obstacle | Result |
|---|---|
| invisible (`Flag_InvisByMagic`) | walked through, free |
| hidden | "You stumble upon X hidden." - revealed (`HidingSkillDef.UnHide`), shoved, stamina charged |
| anyone else, mover not at full stamina | "You are not strong enough to push X out of the way." - step refused |
| anyone else, mover at full stamina | "You shove X out of the way." - stamina charged |

The stamina cost is Sphere's `Calc_WalkThroughChar`: `log2(obstacle STR) + 1`, which gives 6
for STR 50, 7 for STR 100 and 8 for STR 200. After paying it, `@personalSpace` fires on the
shoved character with the pusher as `argv[0]` (`Character.Trigger_PersonalSpace` /
`On_PersonalSpace`). As in 0.99z8, its return value is ignored.

### Why "full stamina" and not Sphere's server rule

Sphere 0.99z8's server only required `Stam >= cost + 1`. The full-stamina rule players
remember is the **client's**: TazUO/ClassicUO's `Pathfinder` treats mobiles as solid unless
`Stamina >= StaminaMax`. TazUO also has an "ignore stamina check" option that switches this off.
Erebor policed that bypass after the fact with an `@personalspace` script (`e_allplayers`, the
"GM step" detection). Enforcing full stamina on the server makes that script unnecessary.
`requireFullStamina = false` restores the plain 0.99z8 rule.

Deliberate deviation from 0.99z8: Sphere also lets ghosts block NPCs. Here they don't, so a
dead player can't body-block monsters in a doorway.

### Settings: `.settings` -> "Movement" (`CharCollisionSettings`)

| Field | Default | Meaning |
|---|---|---|
| `requireFullStamina` | true | players need full stamina to shove; off = Sphere's `Stam > cost` |
| `flatPushCost` | 0 | 0 = `log2(STR)+1`; above 0 = that flat stamina cost |
| `playersBlockedByCharacters` | true | off = players walk through characters (pre-#767) |
| `npcsBlockedByCharacters` | true | off = NPCs walk through characters (pre-#767) |
| `npcPathCharacterRadius` | 8 | tiles around an NPC where characters count as obstacles when it plans a path |
| `npcDetourWaitSeconds` | 2 | how long an NPC stays stuck behind a character before the long search |
| `npcDetourRetrySeconds` | 10 | seconds between one NPC's long searches |
| `npcDetourSearchMargin` | 64 | tiles beyond the NPC and its target the long search may wander |
| `npcDetourMaxIterations` | 4000 | nodes the long search may expand |
| `npcFailedPathRetrySeconds` | 8 | #1199: seconds an NPC doesn't search again toward a spot it found no way to; 0 = every step |
| `fatigueAndOverload` | true | #779 rules below; off = stamina and load never stop or slow anyone |
| `maxLoadPercent` | 200 | players carrying more than this percent of their max can't move |
| `staminaLossAtWeight` | 150 | load percent at which a step costs 1 stamina half the time (Sphere's `STAMINALOSSATWEIGHT`; Erebor ran 150) |
| `runningStaminaPenalty` | 50 | percent added to the load while running (Sphere's `RUNNINGPENALTY`) |

## Forced moves: knockback and pull (#781)

Knockback (`e_npc_knockback`, `PushAway`) and roundattack's pull (`e_npc_roundattack_pullall`,
`PullToward`) move their victim by forced steps (`MonsterSpecialAttacks.ForceMove`). Each step
has to pass `Map.CheckMovement` with the victim's own movement settings, the same check walking
uses, so statics/walls, diagonal corners, water and climb height all count. A step onto a
living character's tile (`CharacterCollision.FindObstacle`) is refused too. The move stops at the
last tile reached, and the victim lands on that tile's real Z.

- Knockback pushes up to `knockbackDistance` (3) tiles straight away from the attacker. Standing
  right by a wall means no push; 2 tiles from one means exactly 2.
- It follows only a blow that did damage (#1155, the user's call). Erebor's pushed on any blow that
  landed, even one the armor took whole.
- The pull drags the target toward the monster until adjacent, stopping at the first wall.

Before #781, both only looked at the terrain tile under each step and ignored statics: a
knockback could push someone into a house through its wall, and the pull teleported its victim
straight next to the monster. The walk itself is the pure `MonsterSpecialAttacks.WalkForced`
(step delegate), tested in `Tests/KnockbackPushDistanceTests.cs`.

## Floors between levels (#1148)

The user's report: a player on a house floor (2946,1229,45, Kralovstvi lesnich elfu) and a titan, titan mage and titan
ranger on the street right below. The mage cast through the floor, the ranger shot through it, and the titan's swing
reached up and its knockback threw the player down to the street. Three fixes:

- **Line of sight:** a floor or ceiling (a `Surface` or `Roof` tile, by its top) that the sight line passes through
  blocks sight, when the two are more than half a character (8) apart in height.
  - The line runs between the characters' middles, 8 above their feet, where Sphere's sight starts (`Map.CrossesAFloor`).
  - In each column on the way it crosses every height between where it enters and where it leaves. In each one's own
    column it crosses the heights between their middle and where it leaves.
  - A level line crosses nothing, so floors and ceilings never block sight between two people on one level.
  - One right over the other (one column): a floor between them blocks (`Map.IsFloorBetween`). Sphere's own rule there
    is "only within 8 in height".
  - Floors are rarely `Noshoot`, which is all the older vertical check looked at.
  - #1158, the user's second spot: a player 5 tiles in on a floor at z 45 (3000,1251) and a titan ranger on the street
    just past its edge (3001,1256) saw each other, and the player looted its corpse. The first fix looked only right
    over and under the two of them.
  - From the edge of such a floor, the street about 3 tiles out is in sight; the foot of the wall isn't.
- **Swing reach:** `WeaponSkillDef.StrikeDistance` is Sphere's `GetTopDist3D`: the flat distance, or the height
  difference / 8 when that's more. A swing needs 1 (or the weapon's range), so 45 below is 5 away. The AI's "close
  enough to attack" check uses the same distance.
- **Where `Go` puts you:** `Map.GetFixedZ` (behind every `Go(x, y, z)`) gave the terrain height, so any `Go` into a
  building dropped to the ground floor: knockback's landing, gates, teleports. It is now the highest walkable surface
  at or below the asked height + 8, as movement counts them. `Go(x, y)` with no height starts from the terrain.

Test: `FloorAttackTests` (protocol), at the user's spot.

## Weight (#889)

Everything is in stones, as a float, as `.def` files write it, as Sphere's scripts read it. Sphere's
native unit is a tenth of a stone: `WEIGHT=3.0` and `WEIGHT=.1` are raw tenths with the point
ignored, and an undotted `WEIGHT=10` is 10 stones. The converter writes all of them as stones.

- An item without a `WEIGHT` line takes it from tiledata. Tiledata's 255 (immovable) and water count
  as 1 stone.
- A stack weighs unit weight x amount. `i_gold` has weight 0, and then a stack is a tenth of a stone
  per 50 coins (99z8), which is the "1 stone per 500" of Erebor's purses.
- `Container.weight` is the contents only, Sphere's `m_totalweight`; `Weight` adds the container's own.
- A bank box is `IsWeighed = false`, so its owner doesn't carry what's in it.
- A character weighs what they carry, and the status window shows that in whole stones, rounded down.

## Fatigue and overload (#779)

Ported from Sphere 0.99z8 (EreborNext `CheckMoveWalkDir`, `Calc_MaxCarryWeight`,
`Calc_DropStamWhileMoving`), in `scripts/Basic/Movement/MovementStamina.cs`. Players only:
NPC stamina and carried weight aren't kept Sphere-accurate here.

- **At 0 stamina** a player can't move: "You are too fatigued to move." (or "Your mount is too
  fatigued to move." when riding).
- **Max carry weight** is `STR*4 + 30` stones, and the status window shows it. Carried weight is worn items plus the backpack
  (layers below `Mount`). Above `maxLoadPercent` (200%): "You are carrying too much
  to move."
- **Every step** costs 1 stamina with a chance from Sphere's S-curve: 50% at
  `staminaLossAtWeight` percent load, halving every 10 percent below and mirrored above. With
  Erebor's 150 that's practically nothing for a normal load. Running counts as
  `runningStaminaPenalty` (50) percent more load.

## Strangler vine (#778)

`a_tevent_e_ovinovac` (`StranglerVineAbilityDef`, in
`scripts/Basic/AbilitiesAndTalents/MonsterSpecialAttacks/StranglerVine.cs`), ported from
Erebor's `e_ovinovac` (`systems/new_events.scp`). A player who shoves past the monster
(`NPC.On_PersonalSpace`) is entangled: "You have been entangled and cannot move!". Neither of
them can step (`CheckCanMove`), and the vine attacks. The grip loosens ("The grip loosens...")
once the vine has taken `vineHitsToBreak` (5) landed hits, when either one dies, or when they
no longer share a tile (e.g. the victim is teleported or knocked back). Erebor checked the
last two with a 2s timer item; here they're checked when the victim tries to move. One victim
at a time, and invulnerable characters aren't grabbed.

### Not ported

- Erebor's "GM step" detection script (#777, closed): the server enforces full stamina itself now.

Tests: `Tests/Protocol/CharCollisionTests.cs`, which drives `scripts/Tests/CharCollisionTests.cs`
through real `WalkRunOrFly` steps; `Tests/MovementStaminaTests.cs` (fast tier) for the #779 math and the
#778 hold rules.
