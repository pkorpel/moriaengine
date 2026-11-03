using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - a house's sign, any item of Erebor's type t_housemenu. One house can have several.</summary>
	public class t_housemenu : CompiledTriggerGroup {
		private static readonly TagKey houseTK = TagKey.Acquire("_house_of_sign_");
		private static readonly TagKey levelTK = TagKey.Acquire("_house_sign_level_");

		/// <summary>The rank from which a sign shows the menu, below it only the plaque: Erebor's hm_lockit.</summary>
		public static HouseRank LevelOf(Item sign) {
			object level = sign.GetTag(levelTK);
			return (level == null) ? HouseRank.Friend : (HouseRank) (int) level;
		}

		public static void SetLevel(Item sign, HouseRank level) {
			sign.SetTag(levelTK, (int) level);
		}

		public static void Link(Item sign, House house) {
			sign.SetTag(houseTK, house);
		}

		public static bool IsSignOf(Item item, House house) {
			return IsAnySign(item) && (item.GetTag(houseTK) == house);
		}

		/// <summary>#1017 - a house's, a stall's (t_vendormenu) or a field's (t_fieldmenu) sign.</summary>
		public static bool IsAnySign(Item item) {
			return (item.Type is t_housemenu) || (item.Type is t_vendormenu) || (item.Type is t_fieldmenu);
		}

		/// <summary>Its house, or the house of the region it stands in, made if there's none yet.</summary>
		private static House HouseOf(Item sign, Character clicker) {
			House house = sign.GetTag(houseTK) as House;
			if ((house != null) && !house.IsDeleted) {
				return house;
			}
			FlaggedRegion region = sign.GetMap().GetRegionFor(sign) as FlaggedRegion;
			if ((region == null) || (region.HouseNumber == 0)) {
				return null;
			}
			house = House.Create(region);
			Link(sign, house);
			clicker.SysMessage("V tomto sektoru nalezen " + (house.HasMembers ? "dum " : "prostor ") + house.Title + ". Menu prirazeno.");
			return house;
		}

		public void On_DClick(Item self, Character clicker) {
			Use(self, clicker);
		}

		internal static void Use(Item self, Character clicker) {
			Player player = clicker as Player;
			if ((player == null) || MysticHypnosis.RefusesMenu(player)) { //#1070 - not while possessing
				return;
			}
			House house = HouseOf(self, player);
			if (house == null) {
				return;
			}
			if (NazgulRings.IsNazgul(player) && (house.Owner != null)) { //against a house being locked up unseen
				self.Message("Mym majitelem je " + house.Owner.Name);
			}
			house.EnsureRentChest(self, player);
			house.CheckRent(player);
			if (house.Owner == null) {
				player.SysMessage("Dum je bez majitele");
				if (!player.IsGM) {
					if (!player.MayEnterHouse(house.Area)) {
						player.SysMessage("Tento dum nemuzes obsadit.");
						return;
					}
					D_TownConfirm.Ask(player, house.ClaimQuestion, () => {
						if (house.Owner == null) {
							house.Claim(player);
							ShowMenu(player, house, self);
						}
					});
					return;
				}
			}
			ShowMenu(player, house, self);
		}

		private static void ShowMenu(Player player, House house, Item sign) {
			if (house.HasMembers) {
				D_House.Show(player, house, sign);
			} else {
				D_RentedPlace.Show(player, house, sign);
			}
		}
	}
}
