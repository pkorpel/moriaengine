# World setup

`.SetupWorld` (GM) is how anything that must exist in the world gets there: quest stones, NPCs, teleports, spawn gems. Run it
after a deploy that adds content. It can be run at any time, as often as you like.

**Rule for new work:** if a feature needs something placed in the world, it gets a setup here and joins `.SetupWorld`. It
doesn't get its own one-off placement code, and a GM shouldn't have to place it by hand.

## What a run does

Every piece has a permanent id, like `grandmaster/elvira`, kept in a tag on the piece itself.

| The piece... | A run... |
|---|---|
| Doesn't exist yet | Creates it |
| Exists, wherever it is now | Leaves it alone, so a piece a GM moved stays moved |
| Was deleted by a GM | Creates it again |
| Stands near its spot, from before pieces had ids | Takes it as that piece, so it isn't doubled |

Nothing is ever removed or replaced, and nothing the setups didn't ask for is touched. Ids survive a save and restart.

The run reports what it did: `SETUP_DONE: World added=1 kept=37`.

Two consequences:

- **To remove a piece for good**, take it out of its setup and delete it in the world. Otherwise the next run brings it back.
- **Changing a spot in code doesn't move a piece that exists.** A GM moves it, or deletes it so the next run creates it at the new
  spot.

## Commands

| Command | Does |
|---|---|
| `.SetupWorld` | Every setup, in order |
| `.SetupExpertQuest`, `.SetupMasterQuest`, `.SetupGrandmasterQuest`, `.SetupDungeonEntrances` | One setup, by the same rules |

All are GM only. What each quest places is in [Rank quests](../04-gameplay-systems/rank-quests.md).

## Adding a setup

Code: `scripts/Setup/`.

1. Add a `WorldSetup` subclass with a `Name` and a `Place`, plus a `[SteamFunction]` for its own command.
2. Add it to the end of `WorldSetups.All`. That's all `.SetupWorld` needs.

```csharp
protected internal override void Place(WorldPieces pieces, Character gm) {
	Map world = Map.GetMap(0);
	int[] at = WorldSpots.NearestStandable(world, 2769, 2763, 5);
	pieces.Npc("myquest/guide", "c_my_guide", at, 0, npc => npc.Flag_Invulnerable = true);
	pieces.SpawnGem("myquest/wolves", new SpawnInfo(4, (IThingFactory) ThingDef.GetByDefname("c_wolf"), 20, 40, 8), at, 0);
}
```

- **Ids** are permanent: never reuse or rename one, or the next run sees a new piece and places a second.
- **`onNew`** (the last argument) runs only on a piece just created, for things like `Flag_Invulnerable`. Don't set up a piece after the
  call, or a GM's later changes get overwritten on each run. Linking pieces to each other after the call is fine, if it's safe to
  repeat.
- **Spots:** `WorldSpots.NearestStandable` for open ground, `WorldSpots.WalkableRing` to walk in from a known tile. Check a walk really
  went somewhere: a spawn point can be a tile nobody can leave, and then every ring is that one tile. That's how a quest piece
  once ended up in open sea (#841).
- **Planes:** check the region's `Mapplane`. Most planes are copies of the surface map's terrain with their own contents. Plane 2
  is a different, smaller map: `map2.mul`, 2304x1600 (`Map.GetFacetOfMapplane`).
- **A piece that gets replaced while the game runs**, as Rinald is when the next hero enters his cave, calls `WorldPieces.MarkAsPiece` on the
  replacement. Otherwise the next run places a second one.
