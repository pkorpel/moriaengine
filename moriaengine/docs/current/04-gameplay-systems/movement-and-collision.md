# Movement & character collision

Every step a character takes goes through `AbstractCharacter.WalkRunOrFly`
(`GameCore/AbstractCharacter.cs`), whether it's a player's move-request packet
(`MovementState.Movement`), an NPC AI step (`NPCAiTimer.TryStep`) or an AStar walk. The order is:

1. `Map.CheckMovement` - terrain, statics, items, climb height. Characters are not considered here.
2. `CheckCharacterObstacles` - other characters on the destination tile (#767, below). GM players skip it.
3. `@step` / `On_Step`, then region exit/enter.
4. The actual move and the 0x77 broadcast.

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
neighbouring directions (Sphere's `NPC_WalkToPoint` sidestep). AStar called with the mover
(`AStar.GetPathFromTo(Character, ...)`) treats a character-occupied *first* step as impassable,
so `StepTowardsSmart` routes around it. Only the first step is checked, because characters
move and the path is recomputed every step anyway.

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

### Not ported (yet)

- `e_ovinovac` (strangler vine) entangling whoever steps on it via `@personalSpace` - still a
  stub in `teventStubs.scp`.
- Sphere's stamina drain while walking overloaded (`Calc_DropStamWhileMoving`), and its
  "too fatigued to move" refusal at 0 stamina.

Test: `Tests/Protocol/CharCollisionTests.cs`, which drives `scripts/Tests/CharCollisionTests.cs`
through real `WalkRunOrFly` steps.
