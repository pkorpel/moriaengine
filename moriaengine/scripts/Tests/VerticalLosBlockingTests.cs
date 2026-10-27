/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Real-user-reported: an NPC on an upper floor directly above (same X/Y column,
	/// different Z) an enemy on the floor below could still cast/attack straight through the
	/// floor. Root cause: Map.CanSeeLosFromTo (GameCore/Regions/LineOfSightImpl.cs) has an early
	/// return "if ((from.X == to.X) && (from.Y == to.Y)) return true" with the reasoning
	/// "nothing horizontal to block" - true for a WALL (a vertical plane that needs horizontal
	/// distance to intersect a horizontal sightline) but wrong for a FLOOR/roof (a horizontal
	/// plane that blocks a perfectly VERTICAL sightline at that exact column). Reproduced here
	/// with a real i_wall_stone placed at the same column, between the two Z values - the same
	/// deterministic mechanism as LineOfSightBlockingTests.cs, just stacked instead of
	/// offset.</summary>
	public static class VerticalLosBlockingTests {
		[SteamFunction]
		public static void TestCheckLosThroughSameColumnFloor(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_wall_stone"), 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC lower = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC upper = (NPC) npcDef.Create(self.X, self.Y, self.Z + 20, self.M);

			bool openBeforeFloor = lower.CanInteractWith(upper).Allow;

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X, self.Y, self.Z + 10, self.M);

			bool openAfterFloor = lower.CanInteractWith(upper).Allow;

			Console.WriteLine("VERTICAL_LOS_TEST: openBeforeFloor=" + openBeforeFloor + " openAfterFloor=" + openAfterFloor);
		}

		/// <summary>Real-user-reported (#692, observation 2): a ranged/caster NPC standing
		/// outside and below a slightly-elevated static house (attacker Z=0, target+wall Z=2)
		/// could attack straight through the wall. Root cause and fix: see
		/// IsLosBlockedAt's own doc comment in GameCore/Regions/LineOfSightImpl.cs - the wall's
		/// blocking Z-band now extends down to the real (flat, unmodified) terrain Z at its own
		/// column, closing the gap a ray starting below the wall's registered base used to slip
		/// through.</summary>
		[SteamFunction]
		public static void TestCheckLosThroughElevatedWall(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyItems(self, (ItemDef) ThingDef.GetByDefname("i_wall_stone"), 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC outside = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC inside = (NPC) npcDef.Create(self.X + 6, self.Y, self.Z + 2, self.M);

			bool openBeforeWall = outside.CanInteractWith(inside).Allow;

			ItemDef wallDef = (ItemDef) ThingDef.GetByDefname("i_wall_stone");
			wallDef.Create(self.X + 3, self.Y, self.Z + 2, self.M);

			bool openAfterWall = outside.CanInteractWith(inside).Allow;

			Console.WriteLine("ELEVATED_WALL_LOS_TEST: openBeforeWall=" + openBeforeWall + " openAfterWall=" + openAfterWall);
		}

		//a real roof-edge piece (0x18) - confirmed via a live ItemDispidInfo scan to carry both
		//TileFlag.Roof and TileFlag.Noshoot, height=3.
		private const int RoofModel = 0x18;
		private const int BuildingHeight = 20; //roof sits well above head height, same order of magnitude as a real one-story building

		/// <summary>#708 - real-user-reported: a ranged NPC had to walk all the way to melee
		/// range to attack a target standing under ANY roof, even with plenty of open ground in
		/// between and no walls at all - but the exact same attacker/target pair worked fine at
		/// range in the open. Root cause: IsLosBlockedAt's Noshoot-blocking-band lower bound
		/// (Math.Min(s.Z, landZ), from #692's wall fix) also applied to ROOF-flagged statics,
		/// extending a roof's block all the way down to the ground - so an ordinary ground-level
		/// ray between two characters both standing well below the actual roof got wrongly
		/// blocked by it. Fixed via GetNoshootBandLow - a Roof-flagged static's band now starts
		/// at its own real Z, never extended down to the ground.</summary>
		[SteamFunction]
		public static void TestRoofDoesNotBlockGroundLevelLosAtRange(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ItemDef roofDef = (ItemDef) ThingDef.FindItemDef(RoofModel);
			VerificationTestUtils.CleanupNearbyItems(self, roofDef, 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC outside = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC underRoof = (NPC) npcDef.Create(self.X + 6, self.Y, self.Z, self.M);

			bool openBeforeRoof = outside.CanInteractWith(underRoof).Allow;

			roofDef.Create(self.X + 3, self.Y, self.Z + BuildingHeight, self.M);

			bool openAfterRoof = outside.CanInteractWith(underRoof).Allow;

			Console.WriteLine("ROOF_LOS_TEST: openBeforeRoof=" + openBeforeRoof + " openAfterRoof=" + openAfterRoof);
		}

		/// <summary>#708 follow-up (user's own explicit ask - multi-floor buildings): confirms
		/// the roof fix above does NOT also unblock a genuine cross-floor sightline - someone on
		/// the ground floor still can't see straight through the floor/ceiling separator to
		/// someone standing on top of it on the floor above. Same vertical same-column mechanism
		/// as TestCheckLosThroughSameColumnFloor above, just using the real Roof-flagged model
		/// instead of a plain wall, and placing the upper character realistically ON TOP of the
		/// floor piece (Z = floor's own Z + height) rather than an arbitrary offset.</summary>
		[SteamFunction]
		public static void TestFloorBetweenLevelsStillBlocksCrossFloorLos(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ItemDef roofDef = (ItemDef) ThingDef.FindItemDef(RoofModel);
			VerificationTestUtils.CleanupNearbyItems(self, roofDef, 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC groundFloor = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC upperFloor = (NPC) npcDef.Create(self.X, self.Y, self.Z + BuildingHeight + 3, self.M);

			bool openBeforeFloor = groundFloor.CanInteractWith(upperFloor).Allow;

			roofDef.Create(self.X, self.Y, self.Z + BuildingHeight, self.M);

			bool openAfterFloor = groundFloor.CanInteractWith(upperFloor).Allow;

			Console.WriteLine("CROSS_FLOOR_LOS_TEST: openBeforeFloor=" + openBeforeFloor + " openAfterFloor=" + openAfterFloor);
		}

		/// <summary>#708 follow-up (multi-floor sanity check): two characters BOTH standing on
		/// the SAME upper floor - with the very floor they're standing on (an intermediate
		/// roof-flagged tile between them, same as the room below's own ceiling) directly on
		/// their shared sightline - must see each other at range exactly like two characters on
		/// open ground do. Confirms the fix's new band (starting at the floor's own Z, not
		/// extended down) still correctly excludes anyone standing ON TOP of that same surface
		/// (Z = floor's own Z + height, same convention used throughout this engine for "standing
		/// on a surface").</summary>
		[SteamFunction]
		public static void TestRoofOverSameFloorDoesNotBlockLos(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ItemDef roofDef = (ItemDef) ThingDef.FindItemDef(RoofModel);
			VerificationTestUtils.CleanupNearbyItems(self, roofDef, 20);

			int floorTopZ = self.Z + BuildingHeight + 3; //standing ON TOP of the floor piece below
			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC a = (NPC) npcDef.Create(self.X, self.Y, floorTopZ, self.M);
			NPC b = (NPC) npcDef.Create(self.X + 6, self.Y, floorTopZ, self.M);

			roofDef.Create(self.X + 3, self.Y, self.Z + BuildingHeight, self.M);

			bool openOnSameFloor = a.CanInteractWith(b).Allow;

			Console.WriteLine("SAME_FLOOR_LOS_TEST: openOnSameFloor=" + openOnSameFloor);
		}

		//a real flat ground/floor decoration (0x4aa) - confirmed via a live ItemDispidInfo scan
		//to carry Background+Surface+Noshoot with Height=0 (no Wall/Impassable/Roof at all).
		private const int FlatFloorModel = 0x4aa;

		/// <summary>#721 - real-user-reported, reproduced at exact real-map coordinates: a
		/// ranged NPC standing just 5 tiles from its target on open, flat, paved ground still
		/// had to walk all the way to melee before firing - no wall, no roof overhead, nothing
		/// a GM could see blocking the shot. Root cause: IsLosBlockedAt's Noshoot-blocking-band
		/// upper bound padded a zero-height static to occupy at least 1 full Z unit
		/// (Math.Max(height, 1), meant so a height-0 WALL still blocks something despite messy
		/// source height data) - but this floor tile is Surface-flagged (something characters
		/// stand ON TOP of) with a real Height of 0, and its own Z exactly equals the ground Z
		/// both characters stand at - so the padded band always included the characters' own
		/// stand height, blocking every ordinary same-level shot across it. Fixed via
		/// GetNoshootHeight - a Surface-flagged static's band now uses its literal (possibly
		/// zero) height, never padded up, since a walkable floor's own surface is never "inside"
		/// itself for someone standing on top of it.</summary>
		[SteamFunction]
		public static void TestFlatSurfaceFloorDoesNotBlockGroundLevelLos(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			ItemDef floorDef = (ItemDef) ThingDef.FindItemDef(FlatFloorModel);
			VerificationTestUtils.CleanupNearbyItems(self, floorDef, 20);

			NPCDef npcDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			NPC a = (NPC) npcDef.Create(self.X, self.Y, self.Z, self.M);
			NPC b = (NPC) npcDef.Create(self.X + 5, self.Y, self.Z, self.M);

			bool openBeforeFloor = a.CanInteractWith(b).Allow;

			//same flat floor tile placed at every intermediate column, same Z as both
			//characters' own feet - exactly the real-world layout that reproduced #721.
			for (int x = self.X + 1; x < self.X + 5; x++) {
				floorDef.Create(x, self.Y, self.Z, self.M);
			}

			bool openAfterFloor = a.CanInteractWith(b).Allow;

			Console.WriteLine("FLAT_FLOOR_LOS_TEST: openBeforeFloor=" + openBeforeFloor + " openAfterFloor=" + openAfterFloor);
		}
	}
}
