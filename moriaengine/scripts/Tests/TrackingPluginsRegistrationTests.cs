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

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helper for #444's regression test - confirms "p_tracking"/
	/// "p_NPCtracking" resolve via the real registry on the very first command any client
	/// sends (no prior setup action touching TrackingPlugins at all), proving both the missing
	/// .Register() fix and the early-touch timing fix (#436) actually took effect, rather than
	/// relying on some earlier test action having incidentally registered them first.</summary>
	public static class TrackingPluginsRegistrationTests {
		[SteamFunction]
		public static void TestCheckTrackingDefsRegistered(Character self) {
			bool playerTrackingRegistered = PluginDef.GetByDefname("p_tracking") != null;
			bool npcTrackingRegistered = PluginDef.GetByDefname("p_NPCtracking") != null;
			Console.WriteLine("TRACKING_DEFS_REGISTERED: playerTracking=" + playerTrackingRegistered +
				" npcTracking=" + npcTrackingRegistered);
		}
	}
}
