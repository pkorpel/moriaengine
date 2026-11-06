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
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Timers;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup/report helpers for #590's protocol test
	/// (Tests/Protocol/NpcMoveRateTests.cs) - spawns two otherwise-identical NPCs (one with
	/// NPCDef.MoveRate=6, one left at the unconfigured 3.0 baseline), sets each chasing a quarry
	/// along its row, and reports each one's real displacement after a live wait - proving
	/// MoveRate actually drives NPCAiTimer's think cadence, not just that the data field parses.
	/// #1174 - a chase, not a "go" order: since #768 an NPC catching up on a go point or a
	/// followed master thinks on Sphere's fixed walk/run cadence (NPCAiTimer.CatchUpInterval),
	/// whatever its MoveRate, so both NPCs went to a far go point at the same speed. A fight
	/// steps once per think at the MoveRate cadence. (In Sphere 0.99z8 itself MOVERATE is
	/// stored and never read; it's this engine's own knob.)
	/// </summary>
	public static class NpcMoveRateTests {
		private static readonly TagKey startXTK = TagKey.Acquire("_moveRateTestStartX_");
		private static readonly TagKey startYTK = TagKey.Acquire("_moveRateTestStartY_");

		[SteamFunction]
		public static void TestSpawnMoveRateNpcs(Character self, ScriptArgs sa) {
			NPCDef fastDef = (NPCDef) ThingDef.GetByDefname("npc_test_moverate_fast");
			NPCDef baselineDef = (NPCDef) ThingDef.GetByDefname("npc_test_moverate_baseline");

			//each on its own row with a straight run of RunLength walkable steps east or west (the open ground
			//around the spawn is narrow: a chase into terrain it can't cross backs off its path search
			//and stands still), chasing a quarry at the run's end: inside NpcAggroSettings.giveUpRange
			//(18), so the target stays valid, and too far to reach within the test's few seconds
			//an earlier attempt's chasers and quarries would stand in this one's rows (the open ground spot is the same)
			foreach (Thing old in spawned) {
				if (!old.IsDeleted) {
					old.Delete();
				}
			}
			spawned.Clear();
			NPC fast = (NPC) fastDef.Create(self.X, self.Y, self.Z, self.M);
			NPC baseline = (NPC) baselineDef.Create(self.X, self.Y, self.Z, self.M);
			spawned.Add(fast);
			spawned.Add(baseline);
			int? usedRow = null;
			string runs = "";
			foreach (NPC chaser in new[] { fast, baseline }) {
				if (FindRun(self, chaser, usedRow, out int runX, out int runY, out int runZ, out int runDx, out int endZ)) {
					usedRow = runY;
					chaser.P(runX, runY, runZ);
					ChaseQuarry(self, chaser, runDx * RunLength, endZ);
					runs += " run=" + runX + "," + runY + "," + runZ + (runDx > 0 ? ",east" : ",west");
				} else {
					runs += " run=none(best " + bestSteps + " at " + bestAt + ")";
					Console.WriteLine("MOVERATE_TEST_NO_RUN: no row with " + RunLength + " walkable steps near " + self.X + "," + self.Y);
				}
			}

			Console.WriteLine("MOVERATE_TEST_SPAWNED: fastUid=" + fast.Uid + " baselineUid=" + baseline.Uid + " self=" + self.X + "," + self.Y + "," + self.Z + runs);
		}

		//the quarry RunLength tiles away: the fast NPC (a step each 0.75 s) is still short of it after
		//the tests' 4.5 s, and the rows around the open ground spot rarely hold more than 9-10 straight steps
		private const int RunLength = 9;
		private static readonly List<Thing> spawned = new List<Thing>();
		private static int bestSteps;
		private static string bestAt;

		/// <summary>The nearest row to self (not its own, not usedRow nor next to it) holding RunLength steps east or west, walkable one by one; endZ = the height at the run's end.</summary>
		private static bool FindRun(Character self, NPC chaser, int? usedRow, out int x, out int y, out int z, out int dx, out int endZ) {
			Map map = self.GetMap();
			bestSteps = -1;
			bestAt = "";
			for (int dy = 1; dy <= 8; dy++) { //not self's own row: the player would stand in the way
				foreach (int row in new[] { self.Y - dy, self.Y + dy }) {
					if (usedRow.HasValue && (Math.Abs(row - usedRow.Value) <= 1)) {
						continue;
					}
					for (int offset = -8; offset <= 8; offset++) {
						x = self.X + offset;
						y = row;
						//open land, or a floor (the spawn's is a static at z 10 over land at 0): try both heights
						foreach (int startZ in new[] { map.GetTileZ(x, y), map.GetFixedZ(new Point3D(x, y, self.Z)) }) {
							foreach (Direction dir in new[] { Direction.East, Direction.West }) {
								z = startZ;
								IPoint3D at = new Point3D(x, y, z);
								int steps = 0;
								while ((steps < RunLength) && map.CheckMovement(at, chaser.MovementSettings, dir, false, out int nx, out int ny, out int nz) && (ny == y)) {
									at = new Point3D(nx, ny, nz);
									steps++;
								}
								if (steps == RunLength) {
									dx = (dir == Direction.East) ? 1 : -1;
									endZ = at.Z;
									return true;
								}
								if (steps > bestSteps) {
									bestSteps = steps;
									bestAt = x + "," + y + "," + z + "," + dir;
								}
							}
						}
					}
				}
			}
			x = y = z = dx = endZ = 0;
			return false;
		}

		private static void ChaseQuarry(Character self, NPC chaser, int dx, int z) {
			int x = chaser.X + dx, y = chaser.Y;
			NPC quarry = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(x, y, z, self.M);
			spawned.Add(quarry);
			quarry.DeleteTimer(TimerKey.Acquire("_npcAiTimer_")); //stands still
			quarry.Flag_Invulnerable = true;
			chaser.SetTag(startXTK, chaser.X);
			chaser.SetTag(startYTK, chaser.Y);
			NPCAiTimer.Attack(chaser, quarry);
		}

		/// <summary>#599 - simulates a GM editing MoveRate on one specific already-spawned
		/// NPC via .info (which reflects over the live NPC instance, not its NPCDef) - sets
		/// the per-instance override directly and resets the displacement baseline so the
		/// test can measure movement under the NEW rate cleanly, not polluted by whatever
		/// ground was already covered under the old one.</summary>
		[SteamFunction]
		public static void TestOverrideNpcMoveRate(Character self, ScriptArgs sa) {
			string[] parts = SplitArgs(sa);
			uint uid = Convert.ToUInt32(parts[0], CultureInfo.InvariantCulture);
			double newRate = Convert.ToDouble(parts[1], CultureInfo.InvariantCulture);
			NPC npc = Thing.UidGetThing(uid) as NPC;
			npc.MoveRate = newRate;
			npc.SetTag(startXTK, npc.X);
			npc.SetTag(startYTK, npc.Y);
			Console.WriteLine("MOVERATE_TEST_OVERRIDDEN: uid=" + uid + " newInstanceRate=" + npc.MoveRate);
		}

		//Same dual-shape ScriptArgs problem documented elsewhere in this codebase - a
		//genuinely single numeric argument reaching an elevated account via the full LScript
		//parser is a boxed value in Argv[0], not a pre-split string; only a multi-token
		//un-split string needs splitting here.
		private static string[] SplitArgs(ScriptArgs sa) {
			if ((sa.Argv.Length == 1) && (sa.Argv[0] is string single)) {
				return single.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			}
			string[] result = new string[sa.Argv.Length];
			for (int i = 0; i < sa.Argv.Length; i++) {
				result[i] = Convert.ToString(sa.Argv[i], CultureInfo.InvariantCulture);
			}
			return result;
		}

		[SteamFunction]
		public static void TestReportNpcDisplacement(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC npc = Thing.UidGetThing(uid) as NPC;
			int displacement = -1;
			if (npc != null) {
				object startXTag = npc.GetTag(startXTK);
				object startYTag = npc.GetTag(startYTK);
				if ((startXTag is int startX) && (startYTag is int startY)) {
					displacement = Point2D.GetSimpleDistance(npc.X, npc.Y, startX, startY);
				}
			}
			NPCAiTimer timer = (npc != null) ? NPCAiTimer.GetFor(npc) : null;
			Console.WriteLine("MOVERATE_TEST_DISPLACEMENT: uid=" + uid + " displacement=" + displacement +
				" state=" + ((timer != null) ? timer.LastState.ToString() : "none") + " target=" + (((timer != null) && (timer.Target != null)) ? timer.Target.Name : "none") +
				" at=" + ((npc != null) ? (npc.X + "," + npc.Y + "," + npc.Z) : "-"));
		}
	}
}
