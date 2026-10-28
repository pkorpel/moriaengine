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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #767 - server side of Tests/Protocol/CharCollisionTests.cs. Every check goes through the
	/// real AbstractCharacter.WalkRunOrFly (or AStar for the pathing check) and prints one
	/// "CHAR_COLLISION: name=True/False" line. The caller's account is dropped to plevel 1 for
	/// the duration, because GMs skip the character check entirely.
	/// </summary>
	public static class CharCollisionTests {
		private static readonly TagKey personalSpacePusherTK = TagKey.Acquire("personalSpacePusher");

		[SteamFunction]
		public static void TestCharCollision(Character self) {
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			AbstractAccount acc = self.Account;
			byte oldPlevel = acc.PLevel;
			CharCollisionSettings settings = CharCollisionSettings.instance;
			bool oldRequireFull = settings.requireFullStamina;
			int oldFlatCost = settings.flatPushCost;
			bool oldPlayers = settings.playersBlockedByCharacters;
			bool oldNpcs = settings.npcsBlockedByCharacters;
			try {
				acc.PLevel = 1;
				settings.requireFullStamina = true;
				settings.flatPushCost = 0;
				settings.playersBlockedByCharacters = true;
				settings.npcsBlockedByCharacters = true;
				RunScenario(self);
			} finally {
				acc.PLevel = oldPlevel;
				settings.requireFullStamina = oldRequireFull;
				settings.flatPushCost = oldFlatCost;
				settings.playersBlockedByCharacters = oldPlayers;
				settings.npcsBlockedByCharacters = oldNpcs;
				VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			}
			Console.WriteLine("CHAR_COLLISION_DONE");
		}

		private static void RunScenario(Character self) {
			CharCollisionSettings settings = CharCollisionSettings.instance;
			Map map = self.GetMap();
			int x0 = self.X, y0 = self.Y, z0 = self.Z;
			byte m = self.M;
			Console.WriteLine("CHAR_COLLISION_INFO: start=" + x0 + "," + y0 + "," + z0 + " stam=" + self.Stam + "/" + self.MaxStam);

			//players: shove only at full stamina, cost log2(STR)+1, @personalSpace fires
			NPC rabbit = Spawn(x0 + 1, y0, z0, m);
			rabbit.Str = 100;
			rabbit.AddTriggerGroup(TriggerGroup.GetByDefname("e_test_personalspace"));
			self.Stam = (short) (self.MaxStam - 1);
			Report("weakPlayerBlocked", !Step(self, Direction.East) && self.X == x0);

			self.Stam = self.MaxStam;
			int stamBefore = self.Stam;
			bool shoved = Step(self, Direction.East);
			Report("fullStamPlayerShoves", shoved && self.X == x0 + 1);
			Console.WriteLine("CHAR_COLLISION_INFO: stamBefore=" + stamBefore + " stamAfter=" + self.Stam);
			Report("shoveCostsLog2StrPlusOne", self.Stam == stamBefore - 7);
			Report("personalSpaceFired", rabbit.GetTag(personalSpacePusherTK) == self);

			//hidden obstacles get revealed and shoved regardless of stamina
			NPC hidden = Spawn(self.X + 1, y0, z0, m);
			hidden.Flag_Hidden = true;
			self.Stam = 1;
			Report("hiddenObstacleShovedAndRevealed", Step(self, Direction.East) && !hidden.Flag_Hidden);

			//invisible obstacles are walked through for free
			NPC invisible = Spawn(self.X + 1, y0, z0, m);
			invisible.Flag_InvisByMagic = true;
			self.Stam = 1;
			Report("invisibleObstacleIgnored", Step(self, Direction.East) && self.Stam == 1);

			//NPCs never shove; ghosts never block; the setting turns NPC blocking off
			int yNpc = y0 + 2;
			NPC blocker = Spawn(x0, yNpc, z0, m);
			NPC walker = Spawn(x0 + 1, yNpc, z0, m);
			Report("npcBlockedByNpc", !Step(walker, Direction.West) && walker.X == x0 + 1);

			blocker.Flag_Insubst = true;
			Report("insubstObstacleIgnored", Step(walker, Direction.West) && walker.X == x0);
			blocker.Flag_Insubst = false;

			walker.Go(x0 + 1, yNpc, walker.Z, m);
			settings.npcsBlockedByCharacters = false;
			Report("npcSettingOffLetsNpcThrough", Step(walker, Direction.West) && walker.X == x0);
			settings.npcsBlockedByCharacters = true;

			//AStar routes around a character standing on the direct first step. AStar's goal needs
			//an exact Z and this terrain isn't flat, so the tiles are found by walking the real
			//terrain 3 tiles south (or north) of the start point, clear of the rows used above, then east.
			IMovementSettings moveSettings = self.MovementSettings;
			Point3D a = null;
			foreach (Direction away in new[] { Direction.South, Direction.North }) {
				a = Walk(map, moveSettings, Walk(map, moveSettings, Walk(map, moveSettings, new Point3D(x0, y0, z0), away), away), away);
				if (a != null) {
					break;
				}
			}
			Point3D b = Walk(map, moveSettings, a, Direction.East);
			Point3D target = Walk(map, moveSettings, Walk(map, moveSettings, b, Direction.East), Direction.East);
			Direction[] path = null;
			if (target != null) {
				NPC pathfinder = Spawn(a.X, a.Y, a.Z, m);
				Spawn(b.X, b.Y, b.Z, m);
				path = AStar.GetPathFromTo(pathfinder, target, map, pathfinder.MovementSettings);
			}
			Console.WriteLine("CHAR_COLLISION_INFO: path=" + (path == null ? "null" : string.Join(",", path)) + " from=" + a + " target=" + target);
			Report("astarRoutesAroundCharacter", (path != null) && (path.Length > 0) && (path[0] != Direction.East));

			settings.flatPushCost = 10;
			Report("flatPushCostSetting", CharacterCollision.GetPushCost(rabbit) == 10);
			settings.flatPushCost = 0;
		}

		private static NPC Spawn(int x, int y, int z, byte m) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			return (NPC) def.Create(x, y, z, m);
		}

		/// <summary>Where one terrain-only step from p in dir lands, or null if p is null or the step is refused.</summary>
		private static Point3D Walk(Map map, IMovementSettings settings, Point3D p, Direction dir) {
			int x, y, z;
			if ((p == null) || !map.CheckMovement(p, settings, dir, false, out x, out y, out z)) {
				return null;
			}
			return new Point3D(x, y, z);
		}

		private static bool Step(Character ch, Direction dir) {
			ch.Direction = dir;
			return ch.WalkRunOrFly(dir, false, ch.IsPlayer);
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("CHAR_COLLISION: " + name + "=" + ok);
		}
	}
}
