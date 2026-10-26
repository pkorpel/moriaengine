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
		/// <summary>#333 - mining now rolls a real weighted ore tier per vein instead of
		/// always "i_ore_iron"; checks for "i_ore_copper" instead, since a brand-new character
		/// (Mining skill 0) always ends up with copper regardless of which ore the vein itself
		/// rolled - copper's own real skill threshold is 0, the only tier a 0-skill miner ever
		/// qualifies for, so this stays fully deterministic despite the underlying roll being
		/// random.</summary>
		[SteamFunction]
		public static void TestCheckHasOre(Character self) {
			CheckBackpackCount(self, "i_ore_copper");
		}

		/// <summary>#422 - lumberjacking now rolls a real weighted wood tier per vein instead
		/// of always "i_log_lager" (which was never the real default to begin with - that item
		/// is exclusively a labor-camp/prisoner-punishment flavor item, confirmed via #270's
		/// corrected comment); checks for "i_log_copper" instead, since a brand-new character
		/// (Lumberjacking skill 0) always ends up with copper regardless of which wood the vein
		/// itself rolled - copper's own real skill threshold is 0, the only tier a 0-skill
		/// chopper ever qualifies for, same reasoning as TestCheckHasOre's own #333 fix.</summary>
		[SteamFunction]
		public static void TestCheckHasLog(Character self) {
			CheckBackpackCount(self, "i_log_copper");
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

		//#370 - dclick-equips-and-uses / mandatory-tool-gate test helpers. One dedicated
		//no-arg function per item type, same established reason as the comment above this
		//class (a defname string argument gets auto-resolved to the live ThingDef object by
		//the elevated-account LScript parser instead of staying literal text).

		[SteamFunction]
		public static void TestGrantPickaxeToBackpack(Character self) {
			Item pickaxe = (Item) ThingDef.GetByDefname("i_pickaxe").Create(self.Backpack);
			Console.WriteLine("TOOL_SETUP: uid=" + pickaxe.Uid + " backpack=" + self.Backpack.Uid);
		}

		[SteamFunction]
		public static void TestGrantAndEquipPickaxe(Character self) {
			Equippable pickaxe = self.NewEquip(ThingDef.GetByDefname("i_pickaxe"));
			Console.WriteLine("TOOL_SETUP: uid=" + pickaxe.Uid + " backpack=" + self.Backpack.Uid);
		}

		[SteamFunction]
		public static void TestGrantAndEquipHatchet(Character self) {
			Equippable hatchet = self.NewEquip(ThingDef.GetByDefname("i_hatchet"));
			Console.WriteLine("TOOL_SETUP: uid=" + hatchet.Uid + " backpack=" + self.Backpack.Uid);
		}

		/// <summary>#390 - equips a one-handed Hand1-layer weapon directly (bypassing the
		/// dclick/deny-trigger path, matching NewEquip's own "already equipped at creation"
		/// shape) so a following hatchet dclick can be checked for proper hand-slot
		/// displacement.</summary>
		[SteamFunction]
		public static void TestGrantAndEquipMismatchedAxe(Character self) {
			Equippable axe = self.NewEquip(ThingDef.GetByDefname("i_axe_battle_copper"));
			Console.WriteLine("TOOL_SETUP: uid=" + axe.Uid + " backpack=" + self.Backpack.Uid);
		}

		/// <summary>#390 - leaves the hatchet in the backpack (not auto-equipped) so a real
		/// dclick exercises the actual equip-collision path (Equippable.On_DClick ->
		/// TryPickupItem + TryEquipItemOnChar), not just NewEquip's creation-time path.</summary>
		[SteamFunction]
		public static void TestGrantHatchetToBackpack(Character self) {
			Item hatchet = (Item) ThingDef.GetByDefname("i_hatchet").Create(self.Backpack);
			Console.WriteLine("TOOL_SETUP: uid=" + hatchet.Uid + " backpack=" + self.Backpack.Uid);
		}

		/// <summary>#392 - a plain converted axe that has NEVER had any associatedskill=/
		/// tooltype= tag (unlike i_hatchet, which used to need one) - its own WeaponType.
		/// TwoHandAxe is what should now automatically grant Lumberjacking on dclick.</summary>
		[SteamFunction]
		public static void TestGrantAndEquipPlainAxe(Character self) {
			Equippable axe = self.NewEquip(ThingDef.GetByDefname("i_axe_copper"));
			Console.WriteLine("TOOL_SETUP: uid=" + axe.Uid + " backpack=" + self.Backpack.Uid);
		}

		[SteamFunction]
		public static void TestGrantAndEquipFishingPole(Character self) {
			Equippable pole = self.NewEquip(ThingDef.GetByDefname("i_fishing_pole"));
			Console.WriteLine("TOOL_SETUP: uid=" + pole.Uid + " backpack=" + self.Backpack.Uid);
		}

		/// <summary>Real-user-reported: same hand-slot displacement bug as #390's hatchet, this
		/// time on the fishing pole - leaves it in the backpack (not auto-equipped) so a real
		/// dclick exercises the actual equip-collision path.</summary>
		[SteamFunction]
		public static void TestGrantFishingPoleToBackpack(Character self) {
			Item pole = (Item) ThingDef.GetByDefname("i_fishing_pole").Create(self.Backpack);
			Console.WriteLine("TOOL_SETUP: uid=" + pole.Uid + " backpack=" + self.Backpack.Uid);
		}

		/// <summary>Reports whether the item at the given uid is currently equipped on self
		/// (Cont==self and sitting at its own TypeDef.Layer, matching Equippable.On_DClick's own
		/// "already equipped" check) - lets a protocol test confirm the #370 dclick flow really
		/// did equip a backpack item, not just that SOME skill got selected afterward.</summary>
		[SteamFunction]
		public static void TestCheckEquipped(Character self, ScriptArgs sa) {
			uint uid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Item item = (Item) Thing.UidGetItem(uid);
			bool equipped = (item != null) && (item.Cont == self) && (item.Z == item.Layer);
			Console.WriteLine("TOOL_EQUIP_CHECK: uid=" + uid + " equipped=" + equipped);
		}
	}
}
