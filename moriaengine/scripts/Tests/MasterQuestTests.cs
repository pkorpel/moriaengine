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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - server-side helpers for Tests/Protocol/MasterQuestTests.cs.</summary>
	public static class MasterQuestTests {
		private static readonly ImmutableRectangle island = new ImmutableRectangle(1488, 3860, 1548, 3920);

		private static List<Thing> Find(string defname, ImmutableRectangle area, int plane = 0) {
			ThingDef def = ThingDef.GetByDefname(defname);
			List<Thing> found = new List<Thing>();
			foreach (Thing t in Map.GetMap(plane).GetThingsInRectangle(area)) {
				if (t.Def == def) {
					found.Add(t);
				}
			}
			return found;
		}

		private static NPC GondorLenastir() {
			return (NPC) Find(MasterQuest.LenastirDef, new ImmutableRectangle(2755, 2745, 2795, 2785))[0];
		}

		private static Item Teleport() {
			return (Item) Find(MasterQuest.TeleportDef, new ImmutableRectangle(5320, 2735, 5345, 2755), MasterQuest.TeleportPlane)[0];
		}

		private static void GoBeside(Character self, Thing thing) {
			int[] at = ExpertQuest.NearestStandable(thing.GetMap(), thing.X + 1, thing.Y, 3);
			self.Go(at[0], at[1], at[2], thing.M);
		}

		private static void RemoveFromBackpack(Character self, string defname) {
			Item item;
			while ((item = MasterQuest.FindInBackpack(self, defname)) != null) {
				item.Delete();
			}
		}

		/// <summary>What the setup placed, and whether the queen can be walked to from where the teleport lands.</summary>
		[SteamFunction]
		public static void TestMasterQuestWorld(Character self) {
			List<Thing> queens = Find(MasterQuest.QueenDef, island);
			int serpents = Find("c_serpent_silver_mistr", island).Count + Find("c_serpent_gold_mistr", island).Count + Find("c_serpent_black_mistr", island).Count;
			bool reachable = false;
			if (queens.Count > 0) {
				AStar.Options options = new AStar.Options { MaxIterations = 50000, WindowMargin = 40, AllowPartial = false, StopWithin = 1 };
				AStar.Result path = AStar.FindPathOnMap(new Point3D(1508, 3860, 10), queens[0], Map.GetMap(0), self.MovementSettings, options);
				reachable = (path != null) && path.ReachedGoal;
			}
			AStar.Options dungeonOptions = new AStar.Options { MaxIterations = 400000, WindowMargin = 300, AllowPartial = false, StopWithin = 1 };
			AStar.Result dungeonPath = AStar.FindPathOnMap(new Point3D(5466, 2669, 10), Teleport(), Map.GetMap(MasterQuest.TeleportPlane), self.MovementSettings, dungeonOptions); //from a_Beridor_Level_3's spawn point
			Console.WriteLine("MASTER_WORLD: queens=" + queens.Count + " serpents=" + serpents + " queenReachable=" + reachable +
				" teleportReachable=" + ((dungeonPath != null) && dungeonPath.ReachedGoal) +
				" lenastirInvulnerable=" + GondorLenastir().Flag_Invulnerable + " teleportNonMovable=" + Teleport().Flag_NonMovable);
		}

		/// <summary>Args (one string): "rank level reachedIslands" - also empties my pack of quest items and takes me to
		/// the Gondor Lenastir.</summary>
		[SteamFunction]
		public static void TestMasterQuestPrepare(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] args = sa.Args.Trim().Trim('"').Split(' ');
			player.SkillRank = (SkillRank) int.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
			player.Level = int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
			if (args[2] == "1") {
				MasterQuest.MarkReachedIslands(player);
			} else {
				MasterQuest.ForgetReachedIslands(player);
			}
			RemoveFromBackpack(player, MasterQuest.EggDef);
			RemoveFromBackpack(player, MasterQuest.KeyDef);
			GoBeside(player, GondorLenastir());
			Console.WriteLine("MASTER_PREPARE: los=" + player.CanSeeLOS(GondorLenastir()).Allow);
		}

		[SteamFunction]
		public static void TestMasterQuestBackToLenastir(Character self) {
			GoBeside(self, GondorLenastir());
			Console.WriteLine("MASTER_BACK: los=" + self.CanSeeLOS(GondorLenastir()).Allow);
		}

		/// <summary>An egg in my pack, owned by nobody, or by Lenastir ("other": some other hero).</summary>
		[SteamFunction]
		public static void TestMasterQuestGiveEgg(Character self, ScriptArgs sa) {
			RemoveFromBackpack(self, MasterQuest.EggDef);
			Item egg = (Item) ThingDef.GetByDefname(MasterQuest.EggDef).Create(self.Backpack);
			if (sa.Args.Trim().Trim('"') == "other") {
				MasterQuest.SetEggOwner(egg, GondorLenastir());
			}
			Console.WriteLine("MASTER_EGG: " + egg.Uid);
		}

		[SteamFunction]
		public static void TestMasterQuestGiveKey(Character self) {
			ThingDef.GetByDefname(MasterQuest.KeyDef).Create(self.Backpack);
			Console.WriteLine("MASTER_KEY: given");
		}

		/// <summary>Onto the teleport's tile, the way a step gets there.</summary>
		[SteamFunction]
		public static void TestMasterQuestStepOnTeleport(Character self) {
			Item teleport = Teleport();
			self.Go(teleport.X, teleport.Y, teleport.Z, teleport.M);
			Console.WriteLine("MASTER_STEPPED: done");
		}

		/// <summary>The real queen killed next to me: reports the egg lying in her corpse.</summary>
		[SteamFunction]
		public static void TestMasterQuestKillQueen(Character self) {
			RemoveFromBackpack(self, MasterQuest.EggDef);
			int[] at = ExpertQuest.NearestStandable(self.GetMap(), self.X + 1, self.Y, 3);
			Character queen = (Character) ThingDef.GetByDefname(MasterQuest.QueenDef).Create(at[0], at[1], at[2], self.M);
			queen.CauseDeath(self);
			foreach (Thing t in self.GetMap().GetThingsInRectangle(new ImmutableRectangle(at[0] - 1, at[1] - 1, at[0] + 1, at[1] + 1))) {
				Corpse corpse = t as Corpse;
				if (corpse != null) {
					foreach (Item item in corpse.EnumShallow()) {
						if (item.Def == ThingDef.GetByDefname(MasterQuest.EggDef)) {
							Console.WriteLine("MASTER_QUEEN: egg=" + item.Uid + " owner=" + MasterQuest.GetEggOwner(item) + " corpse=" + corpse.Uid + " pack=" + self.Backpack.Uid);
							return;
						}
					}
				}
			}
			Console.WriteLine("MASTER_QUEEN: egg=none");
		}

		[SteamFunction]
		public static void TestMasterQuestState(Character self) {
			Player player = (Player) self;
			Item egg = MasterQuest.FindInBackpack(player, MasterQuest.EggDef);
			Item key = MasterQuest.FindInBackpack(player, MasterQuest.KeyDef);
			Region region = player.Region;
			Console.WriteLine("MASTER_STATE: region=" + ((region == null) ? "none" : region.Defname) + " rank=" + player.SkillRank + " reached=" + MasterQuest.HasReachedIslands(player) +
				" key=" + (key != null) + " keyNewbie=" + ((key != null) && key.HasAttribute(ItemAttributes.attr_newbie)) +
				" egg=" + (egg != null) + " eggMine=" + ((egg != null) && (MasterQuest.GetEggOwner(egg) == player)));
		}
	}
}
