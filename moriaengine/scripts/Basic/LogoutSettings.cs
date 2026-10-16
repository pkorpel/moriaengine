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

using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// GM-tunable settings for the post-disconnect logout delay (#261) - shown as a "Logout"
	/// category in the .settings dialog, same pattern as NotorietySettings/DecaySettings.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class LogoutSettings : SettingsMetaCategory {

		[SavedMember]
		public static LogoutSettings instance = new LogoutSettings();

		[LoadingInitializer]
		public LogoutSettings() {
		}

		/// <summary>How long (real seconds) a disconnected player's body stays standing in the
		/// world, fully visible/interactable, before actually disappearing (classic UO-style
		/// logout delay) - matches Player.CharLingeringTimer's own previously-hardcoded 5s,
		/// now GM-tunable and defaulting to 60s per explicit request.</summary>
		[SaveableData]
		public double logoutDelaySeconds = 60;

		/// <summary>Whether a visual effect plays at the moment of disconnect (not at the
		/// moment of actual disappearance) - lets nearby players see that this character is in
		/// the process of logging out, matching real UO's own logout sparkle.</summary>
		[SaveableData]
		public bool showLogoutEffect = true;
	}
}
