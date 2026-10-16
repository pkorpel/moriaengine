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
	/// GM-tunable settings for the generic decay system (#215/#219, redesigned per user
	/// follow-up request), shown as an "Item Decay" category in the .settings dialog - same
	/// pattern as LevelingSettings/CombatSettings. Persists across restarts via the generic
	/// SaveableClass/HasSavedMembers pipeline.
	///
	/// Three separate rates, not one - a player's corpse, an NPC's corpse, and a loose
	/// attr_decay item are three different real-world defaults (per explicit user request),
	/// not one generic "decay interval" the way the first pass had it.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class DecaySettings : SettingsMetaCategory {

		[SavedMember]
		public static DecaySettings instance = new DecaySettings();

		[LoadingInitializer]
		public DecaySettings() {
		}

		/// <summary>How long (real minutes) a player's own corpse survives on the ground
		/// before decaying away (ItemDecaySystem) - distinguished from an NPC's corpse via
		/// Corpse.Owner being non-null (set only for player deaths, see Corpse.InitFromChar).</summary>
		[SaveableData]
		public double playerCorpseDecayMinutes = 10;

		/// <summary>Same as above, for an NPC's corpse (Corpse.Owner == null).</summary>
		[SaveableData]
		public double npcCorpseDecayMinutes = 5;

		/// <summary>How long (real minutes) a plain attr_decay-flagged item survives once it's
		/// found lying loose on the ground.</summary>
		[SaveableData]
		public double itemDecayMinutes = 5;
	}
}
