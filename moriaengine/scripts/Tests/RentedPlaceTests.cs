using System;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1017 - stalls and fields: kinds, rent by size, claim, eviction of locked things, the field's dog.</summary>
	public static class RentedPlaceTests {
		private static FlaggedRegion Region(string defname) {
			return (FlaggedRegion) StaticRegion.GetByDefname(defname);
		}

		[SteamFunction]
		public static void TestRentedPlaces(Character self) {
			Player player = (Player) self;
			player.Account.PLevel = 1;
			House stall = House.Create(Region("a_stanek_13"));
			House big = House.Create(Region("a_stanek_2"));
			House field = House.Create(Region("a_pole_8"));
			Report("kindsByRegion", (stall is Stall) && (field is Field) && !stall.HasMembers);
			Report("rentBySize", (stall.Rent == 15000) && (big.Rent == 51000) && (field.Rent == 251000));
			stall.Claim(player);
			Report("claimed", stall.Owner == player);
			Report("theOwnersMenuRank", stall.RankOf(player) == HouseRank.Owner);

			FlaggedRegion area = stall.Area;
			ImmutableRectangle rect = null;
			foreach (ImmutableRectangle r in area.Rectangles) {
				rect = r;
				break;
			}
			stall.EnsureRentChest(stall, null);
			Item barrel = (Item) ((ItemDef) ThingDef.GetByDefname("i_barrel")).Create(rect.MinX, rect.MinY, stall.Z, stall.M);
			Item loose = (Item) ((ItemDef) ThingDef.GetByDefname("i_barrel")).Create(rect.MinX, rect.MinY, stall.Z, stall.M);
			stall.Lock(barrel, HouseRank.Public, null);
			Report("locked", barrel.LockedTo == stall);
			stall.Empty();
			Report("evictionTakesTheLocked", (stall.Owner == null) && (barrel.TopObj() == stall.RentChest));
			Report("andLeavesTheRest", loose.IsOnGround);
			loose.Delete();

			Field pole = (Field) field;
			pole.Claim(player);
			NPC dog = (NPC) ((NPCDef) ThingDef.GetByDefname("c_dog")).Create(self.X + 1, self.Y, self.Z, self.M);
			dog.ControlMaster = player;
			RentedPlaces.SetGuard(player, pole, dog);
			Report("theDogGuards", (pole.Guard == dog) && RentedPlaces.GuardOwnerPasses(player, pole));
			pole.Empty();
			dog.Delete();
			Console.WriteLine("RENTED_DONE");
		}

		private static void Report(string name, bool ok) {
			Console.WriteLine("RENTED: " + name + "=" + ok);
		}
	}
}
