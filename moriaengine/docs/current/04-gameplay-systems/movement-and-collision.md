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
passes AStar a `FirstStepBlocked` check (`CharacterCollision.IsTileBlockedFor`), so a
character-occupied *first* step is treated as impassable and the path routes around it. Only
the first step is checked, because characters move. A cached path that runs into a character
later just fails that step and gets re-planned (see
[Pets & NPC AI](pets-and-npc-ai.md#pathfinding-769)).

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
| `fatigueAndOverload` | true | #779 rules below; off = stamina and load never stop or slow anyone |
| `maxLoadPercent` | 200 | players carrying more than this percent of their max can't move |
| `staminaLossAtWeight` | 150 | load percent at which a step costs 1 stamina half the time (Sphere's `STAMINALOSSATWEIGHT`; Erebor ran 150) |
| `runningStaminaPenalty` | 50 | percent added to the load while running (Sphere's `RUNNINGPENALTY`) |

## Fatigue and overload (#779)

Ported from Sphere 0.99z8 (EreborNext `CheckMoveWalkDir`, `Calc_MaxCarryWeight`,
`Calc_DropStamWhileMoving`), in `scripts/Basic/Movement/MovementStamina.cs`. Players only:
NPC stamina and carried weight aren't kept Sphere-accurate here.

- **At 0 stamina** a player can't move: "You are too fatigued to move." (or "Your mount is too
  fatigued to move." when riding).
- **Max carry weight** is `STR*4 + 30` stones. Carried weight is worn items plus the backpack
  (layers below `Mount`), so it excludes the bank box. `Character.Weight` itself includes the
  bank, so it isn't used for this. Above `maxLoadPercent` (200%): "You are carrying too much
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
