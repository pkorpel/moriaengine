using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#941 - the rest of what a house does to people in it.</summary>
	public static class HouseEvents {
		/// <summary>e_zabitivdome: a killing in a house, which only a player can do, is told and logged.</summary>
		public static void OnDeath(Character victim, Character killer) {
			if ((killer == null) || (killer == victim) || !killer.IsPlayer || !victim.IsPlayer || !RegionEventSystem.IsIn(victim, RegionEventSystem.PlayerHouse)) {
				return;
			}
			victim.SysMessage("Byl si zabit v dome hracem " + killer.Name + " na pozici " + victim.P() + "!");
			Console.WriteLine(victim.Name + " (" + victim.Account.Name + " " + victim.P() + ") byl zabit v DOMOVNI ZONE hracem " + killer.Name
				+ " (" + killer.Account.Name + " " + killer.P() + ") v " + victim.Region.Name);
		}

		/// <summary>Erebor's i_logout_timer, in whatever has a house number.</summary>
		public static double LogoutDelayFor(Character ch) {
			FlaggedRegion area = ch.Region as FlaggedRegion;
			return ((area != null) && (area.HouseNumber != 0)) ? HouseSettings.instance.logoutDelaySeconds : LogoutSettings.instance.logoutDelaySeconds;
		}
	}
}
