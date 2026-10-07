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

using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	[HasSavedMembers]
	public static class LightAndWeather {
		[SavedMember]
		private static int dayLight;
		[SavedMember]
		private static int nightLight = 12; //unused for now...?
		[SavedMember]
		private static int undergroundLight = 26;

		public static int GetLightAt(IPoint4D point) {
			point = point.TopPoint;
			FlaggedRegion asFlagged = point.GetMap().GetRegionFor(point) as FlaggedRegion;
			if (asFlagged != null) {
				return GetLightIn(asFlagged);
			}
			return dayLight;
		}

		public static int GetLightIn(FlaggedRegion region)
		{
			if (region.Flag_Underground) {
				return undergroundLight;
			}
			return dayLight;
		}

		public static int DayLight {
			get {
				return dayLight;
			}
			set { 
				dayLight = value;
				RefreshAllPlayers();
			}
		}

		public static int NightLight {
			get {
				return nightLight;
			}
			set { 
				nightLight = value;
				RefreshAllPlayers();
			}
		}

		public static int UndergroundLight {
			get {
				return undergroundLight;
			}
			set { 
				undergroundLight = value;
				RefreshAllPlayers();
			}
		}

		private static void RefreshAllPlayers() {
			foreach (Player p in GameServer.GetAllPlayers()) {
				p.SendGlobalLightLevel(GetLightAt(p));
			}
		}
	}
}
