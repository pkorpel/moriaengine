using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - who gets into a house. Erebor's doorstep tiles (i_duch_dlazba) stopped ghosts and anyone not of the
	/// house's realm, and aren't in the archive, so the rule holds for the whole region: no tile to walk around, and it stops a
	/// teleport inside too. A claim follows it, as a sign can stand on the street.</summary>
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

		/// <summary>For s_teleport's @effectGround: a walk is stopped by the region, a teleport can't be.</summary>
		public bool TeleportRefusedAt(IPoint3D target) {
			FlaggedRegion area = this.GetMap().GetRegionFor(target.X, target.Y) as FlaggedRegion;
			if (this.MayEnterHouse(area)) {
				return false;
			}
			this.SysMessage("Do tohoto domu nemuzes.");
			return true;
		}
	}
}
