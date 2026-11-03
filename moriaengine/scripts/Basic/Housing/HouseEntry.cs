using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - who belongs to a house: a claim and the house bank ask it, as a sign can stand on the street. Walking in
	/// is the doorstep tiles' (HouseDoorstep): the user dropped the whole region's rule, as zones overlap (Barad-dur's).</summary>
	public partial class Character {
		public bool MayEnterHouse(FlaggedRegion area) {
			if ((area == null) || !area.IsHouse || this.IsGM) {
				return true;
			}
			if (this.Flag_Dead) {
				return false;
			}
			return (area.HouseRealm == Realm.None) || (this.Realm == area.HouseRealm);
		}
	}
}
