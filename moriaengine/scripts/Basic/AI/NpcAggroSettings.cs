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
	/// GM-tunable settings for NPCAiTimer's autonomous-aggro distances - shown as an "NPC
	/// Aggro" category in the .settings dialog, same pattern as LogoutSettings/
	/// NotorietySettings. Both fields replace what used to be hardcoded `const int`s on
	/// NPCAiTimer itself (AggroRange=8, GiveUpRange=12) - defaults bumped to 14/18 per
	/// explicit user request.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class NpcAggroSettings : SettingsMetaCategory {

		[SavedMember]
		public static NpcAggroSettings instance = new NpcAggroSettings();

		[LoadingInitializer]
		public NpcAggroSettings() {
		}

		/// <summary>Tiles an NPC scans (Map.GetCharsInRange, checked once per 1.5s AI tick) to
		/// autonomously notice and pick a target (NPCAiTimer.FindTarget) - a candidate still
		/// needs NPC.ShouldAggroOn and real line-of-sight to actually trigger. Also the radius
		/// AlertGuardiansOf uses to find nearby guardians when their ward is attacked.</summary>
		[SaveableData]
		public int aggroRange = 14;

		/// <summary>Tiles a target can get from an already-engaged NPC before it's dropped and
		/// the NPC goes back to wandering (NPCAiTimer.IsValidTarget) - covers a target fleeing,
		/// teleporting, or just outrunning the NPC.</summary>
		[SaveableData]
		public int giveUpRange = 18;
	}
}
