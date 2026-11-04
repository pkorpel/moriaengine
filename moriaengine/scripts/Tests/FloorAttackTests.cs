using System;
using System.Collections.Generic;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1148 - server side of Tests/Protocol/FloorAttackTests.cs: monsters on the ground under a player on a house
	/// floor (the user's spot, a house in Kralovstvi lesnich elfu).</summary>
	public static class FloorAttackTests {
		private const int X = 2946, Y = 1229, UpperZ = 45;

		private static string Column(Map map, int x, int y) {
			string line = "land=" + map.GetTileZ(x, y);
			foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				line += " [0x" + s.Id.ToString("X") + " z" + s.Z + " h" + s.Height + " " + s.Flags + "]";
			}
			return line;
		}

		private static NPC Below(Character self, string defname, int dx) {
			NPC npc = (NPC) ThingDef.GetByDefname(defname).Create(X + dx, Y, 0, 0);
			npc.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
			return npc;
		}

		private static int hitsBefore;

		[SteamFunction]
		public static void TestFloorAttack(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			Player player = (Player) self;
			string line;
			switch (what) {
				case "probe": {
						self.Account.PLevel = 1;
						self.P(X, Y, UpperZ);
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						Map map = self.GetMap();
						line = "at=" + self.X + "," + self.Y + "," + self.Z + " col=" + Column(map, X, Y) + " | east=" + Column(map, X + 1, Y);
						for (int dy = 1; dy <= 3; dy++) {
							line += " | north" + dy + "=" + Column(map, X, Y - dy);
						}
						for (int dy = 1; dy <= 3; dy++) {
							int nx, ny, nz;
							bool ok = map.CheckMovement(new Point3D(X, Y - dy + 1, UpperZ), self.MovementSettings, Direction.North, false, out nx, out ny, out nz);
							line += " step" + dy + "=" + ok + "@" + nz;
						}
						break;
					}
				case "los": {
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						NPC mage = Below(self, "c_titan_mage", 0);
						NPC side = Below(self, "c_titan_mage", 1);
						NPC upstairs = Below(self, "c_titan_mage", 2);
						upstairs.P(X + 2, Y, UpperZ);
						line = "sameColumnSees=" + mage.CanSeeLOS(self).Allow + " sameColumnInteract=" + mage.CanInteractWith(self).Allow +
							" besideSees=" + side.CanSeeLOS(self).Allow + " playerSeesMage=" + self.CanSeeLOS(mage).Allow +
							" sameFloorSees=" + upstairs.CanSeeLOS(self).Allow;
						break;
					}
				case "swing": {
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						NPC titan = Below(self, "c_titan", 0);
						NPC upstairs = Below(self, "c_titan", 1);
						upstairs.P(X + 1, Y, UpperZ);
						line = "fromBelow=" + WeaponSkillDef.StrikeDistance(titan, self) + " sameFloor=" + WeaponSkillDef.StrikeDistance(upstairs, self);
						break;
					}
				case "knock": {
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						MonsterSpecialAttacks.ForceMove(self, 3, (x, y) => Direction.North, (x, y) => false);
						line = "to=" + self.X + "," + self.Y + "," + self.Z;
						self.P(X, Y, UpperZ);
						break;
					}
				case "go": {
						self.Go(X, Y - 2, UpperZ, 0);
						int floor = self.Z;
						self.Go(X, Y - 2, 0, 0);
						int ground = self.Z;
						self.Go(X, Y - 2, UpperZ + 30, 0);
						int fromAbove = self.Z;
						self.Go(X, Y - 2);
						int flat = self.Z;
						self.P(X, Y, UpperZ);
						line = "floor=" + floor + " ground=" + ground + " fromAbove=" + fromAbove + " flat=" + flat;
						break;
					}
				case "live": {
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						self.Hits = self.MaxHits;
						hitsBefore = self.Hits;
						foreach (string defname in new[] { "c_titan", "c_titan_mage", "c_titan_ranger" }) {
							NPC npc = (NPC) ThingDef.GetByDefname(defname).Create(X, Y, 0, 0);
							NPCAiTimer.EnsureInstalled(npc);
							NPCAiTimer.Attack(npc, self);
						}
						line = "started";
						break;
					}
				case "liveafter":
					line = "lost=" + (hitsBefore - self.Hits) + " z=" + self.Z;
					VerificationTestUtils.CleanupNearbyNpcs(self, 10);
					break;
				case "edge": {
						//#1158 - the user's second spot: player on a floor, ranger outside a few tiles off
						self.P(3000, 1251, 45);
						VerificationTestUtils.CleanupNearbyNpcs(self, 10);
						NPC ranger = (NPC) ThingDef.GetByDefname("c_titan_ranger").Create(3001, 1256, 0, 0);
						ranger.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
						NPC upstairs = (NPC) ThingDef.GetByDefname("c_titan_ranger").Create(3001, 1254, 0, 0);
						upstairs.DeleteTimer(TimerKey.Acquire("_npcAiTimer_"));
						upstairs.P(3001, 1254, 45);
						line = "at=" + self.X + "," + self.Y + "," + self.Z + " rangerSees=" + ranger.CanSeeLOS(self).Allow + " playerSees=" + self.CanSeeLOS(ranger).Allow +
							" rangerInteract=" + ranger.CanInteractWith(self).Allow + " floorRangerSees=" + upstairs.CanSeeLOS(self).Allow;
						//the corpse below the edge, from a tile inside the railing
						Item loot = (Item) ThingDef.GetByDefname("i_gold").Create(3001, 1256, 0, 0);
						self.P(3000, 1254, 45);
						line += " lootFromEdge=" + self.CanReach(loot).Allow;
						loot.Delete();
						self.P(X, Y, UpperZ);
						break;
					}
				default:
					line = "unknown";
					break;
			}
			Console.WriteLine("FLOOR_TEST: " + what + " " + line + " |");
		}
	}
}
