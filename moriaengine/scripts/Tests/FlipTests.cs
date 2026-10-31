using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#938 - server-side helpers for Tests/Protocol/FlipTests.cs. The numbers expected there are what the
	/// real 0.99z8 gave EreborNext's FlipOracleTests.</summary>
	public static class FlipTests {
		private static Item Make(string defname, Character self) {
			return (Item) ((ItemDef) ThingDef.GetByDefname(defname)).Create(self.X + 1, self.Y, self.Z, self.M);
		}

		private static void Report(string marker, Item item) {
			Console.WriteLine(marker + " uid=" + (uint) item.FlaggedUid + " plain=" + item.Uid + " model=" + item.Model);
		}

		/// <summary>The whole of a real wall's group: every model once and back to where it started.</summary>
		[SteamFunction]
		public static void TestFlipWholeGroup(Character self) {
			Item wall = Make("i_wall_stone_4", self);
			int start = wall.Model;
			HashSet<int> seen = new HashSet<int>();
			int steps = 0;
			do {
				seen.Add(wall.Model);
				wall.Flip();
				steps++;
			} while ((wall.Model != start) && (steps < 500));
			Console.WriteLine("FLIP_GROUP: steps=" + steps + " models=" + seen.Count + " dupes=" + wall.TypeDef.DupeList.Count);
			wall.Delete();
		}

		[SteamFunction]
		public static void TestFlipList(Character self) {
			Item item = Make("i_flip_probe", self);
			string models = "";
			for (int i = 0; i < 3; i++) {
				item.Flip();
				models += item.Model + "|";
			}
			Console.WriteLine("FLIP_LIST: " + models);
			item.Delete();
		}

		[SteamFunction]
		public static void TestFlipDoor(Character self) {
			string models = "";
			foreach (string command in new[] { null, "ALIGN", "hAnD", "SWING" }) {
				Item door = Make("i_flip_door_probe", self);
				door.Flip(command);
				models += door.Model + "|";
				door.Delete();
			}
			Console.WriteLine("FLIP_DOOR: " + models);
		}

		[SteamFunction]
		public static void TestFlipLight(Character self) {
			Item light = Make("i_flip_light_probe", self);
			light.LightPattern = 54;
			light.Flip();
			int first = light.LightPattern;
			light.Flip();
			Console.WriteLine("FLIP_LIGHT: " + first + "|" + light.LightPattern + "|model=" + light.Model + "|dir=" + light.DirectionByte);
			light.Delete();
		}

		[SteamFunction]
		public static void TestFlipCorpse(Character self) {
			Character dummy = (Character) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			dummy.Direction = Direction.NorthWest;
			dummy.CauseDeath(null);
			foreach (Thing thing in self.GetMap().GetThingsInRange(self.X, self.Y, 3)) {
				Corpse corpse = thing as Corpse;
				if (corpse != null) {
					corpse.Direction = Direction.NorthWest;
					corpse.Flip();
					Console.WriteLine("FLIP_CORPSE: model=" + corpse.Model + " dir=" + (int) corpse.Direction);
					corpse.Delete();
					return;
				}
			}
			Console.WriteLine("FLIP_CORPSE: none");
		}

		[SteamFunction]
		public static void TestFlipDropSetup(Character self) {
			self.Account.PLevel = 1;
			Item probe = Make("i_flip_probe", self);
			Item gold = Make("i_gold", self);
			gold.Amount = 5;
			Console.WriteLine("FLIP_DROP: probe=" + (uint) probe.FlaggedUid + " gold=" + (uint) gold.FlaggedUid + " goldModel=" + gold.Model
				+ " x=" + self.X + " y=" + self.Y + " z=" + self.Z);
		}

		[SteamFunction]
		public static void TestFlipSelfSetup(Character self) {
			self.Account.PLevel = 7;
			self.Direction = Direction.NorthWest;
			Item door = Make("i_flip_door_probe", self);
			Console.WriteLine("FLIP_CMD: self=" + (uint) self.FlaggedUid + " door=" + (uint) door.FlaggedUid + " plainDoor=" + door.Uid);
		}

		[SteamFunction]
		public static void TestFlipReport(Character self, ScriptArgs sa) {
			Item item = (Item) Thing.UidGetItem(Convert.ToInt32(sa.Argv[0], System.Globalization.CultureInfo.InvariantCulture));
			Console.WriteLine("FLIP_REPORT: model=" + (item == null ? -1 : item.Model) + " dir=" + (int) self.Direction);
		}
	}
}
