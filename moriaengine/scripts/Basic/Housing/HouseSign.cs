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
			clicker.SysMessage("V tomto sektoru nalezen dum " + house.Title + ". Menu prirazeno.");
			return house;
		}

		public void On_DClick(Item self, Character clicker) {
			Player player = clicker as Player;
			if (player == null) {
				return;
			}
			House house = HouseOf(self, player);
			if (house == null) {
				return;
			}
			if (NazgulRings.IsNazgul(player) && (house.Owner != null)) { //against a house being locked up unseen
				self.Message("Mym majitelem je " + house.Owner.Name);
			}
			if (house.Owner == null) {
				player.SysMessage("Dum je bez majitele");
				if (!player.IsGM) {
					D_TownConfirm.Ask(player, "Chces obsadit tento dum?", () => {
						if (house.Owner == null) {
							house.Claim(player);
							D_House.Show(player, house, self);
						}
					});
					return;
				}
			}
			D_House.Show(player, house, self);
		}
	}
}
