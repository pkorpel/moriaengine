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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// V-7 (verification pass, #75): server-side helpers for taming/mining/lumberjacking
	/// scenarios driven by real skill-use (0x12) + target-cursor (0x6c) packets from
	/// Tests/Protocol/GatheringTests.cs. Every scenario here first flips the test account to GM
	/// (reusing V-2's TestSetGmOn) - SkillDef.CheckSuccess(Character,int) always returns true for
	/// a GM, the same real production shortcut the game itself uses, so success is deterministic
	/// without needing to grind real skill points first; the reachability/validity gates
	/// (IsValidTameTarget/IsValidVein/IsValidTree) run unconditionally before that check, so this
	/// doesn't skip testing any of the real "is this actually a legal target" logic.
	/// </summary>
	public static class GatheringTests {
		[SteamFunction]
		public static void TestSpawnTameable(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			NPCDef def = ThingDef.GetByDefname("C_BIRD_CROW") as NPCDef;
			NPC npc = (NPC) def.Create(self.X + 1, self.Y, self.Z, self.M);
			Console.WriteLine("TAME_SETUP: npc=#" + npc.Uid);
		}

		[SteamFunction]
		public static void TestCheckControlMaster(Character self, ScriptArgs sa) {
			int npcUid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			NPC npc = (NPC) Thing.UidGetCharacter(npcUid);
			Console.WriteLine("TAME_CHECK: controlMaster=" + (npc.ControlMaster != null ? npc.ControlMaster.Name : "none"));
		}

		/// <summary>Finds a real world static flagged TileFlag.Foliage and reports its exact
		/// x/y/z/model - lumberjacking's target-cursor packet needs all four (statics are
		/// map-tile decorations, not Things with a normal uid; the client names them by
		/// coordinate + tile id instead, see GameState.HandleTarget). Real finding while
		/// building this: this test fixture's spawn point has essentially no decorative statics
		/// within LumberjackingSkillDef.MaxReach (or even within a thousand tiles) - the nearest
		/// foliage found is far outside chopping range, so self is moved there directly (the
		/// same category as every other "Test*" setup helper spawning a resource near self in
		/// this session, just inverted - moving the character to the resource instead of the
		/// resource to the character; nothing about the real lumberjacking logic is bypassed).</summary>
		[SteamFunction]
		public static void TestGoToNearbyFoliage(Character self) {
			foreach (AbstractInternalItem item in self.GetMap().GetStaticsAndMultiComponentsInRange(self.X, self.Y, 1000)) {
				if (TileData.HasFlag(item.Flags, TileFlag.Foliage)) {
					self.Go(item.X, item.Y, item.Z, self.M);
					Console.WriteLine("FOLIAGE_FOUND: x=" + item.X + " y=" + item.Y + " z=" + item.Z + " model=" + item.Id);
					return;
				}
			}
			Console.WriteLine("FOLIAGE_FOUND: none");
		}

		/// <summary>Real finding while building this: passing a defname-shaped string as a
		/// speech-command argument doesn't stay a string - SimpleCommandParser/the LScript
		/// argument binding auto-resolves identifier-like tokens to the actual ThingDef object,
		/// so a single generic "check for item X" helper can't take the item type as an
		/// argument the way TestCheckHasItem (uid, a plain number) could in V-4. Two dedicated,
		/// no-argument functions instead - the only two item types this phase needs.</summary>
		[SteamFunction]
		public static void TestCheckHasOre(Character self) {
			CheckBackpackCount(self, "i_ore_iron");
		}

		[SteamFunction]
		public static void TestCheckHasLog(Character self) {
			CheckBackpackCount(self, "i_log_lager");
		}

		private static void CheckBackpackCount(Character self, string defname) {
			ThingDef wantDef = ThingDef.GetByDefname(defname);
			int count = 0;
			foreach (AbstractItem item in self.Backpack) {
				if (item.Def == wantDef) {
					count++;
				}
			}
			Console.WriteLine("BACKPACK_CHECK: " + defname + " count=" + count);
		}
	}
}
