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
	/// GM-tunable settings for the notoriety/highlight-color system, shown as a "Notoriety"
	/// category in the .settings dialog - same pattern as DecaySettings/ProfessionSettings.
	/// The real OSI hue values themselves (Notoriety.nameHues) are NOT exposed here - those
	/// are fixed client-protocol constants, not a gameplay knob - only the house-rule/
	/// gameplay-tunable pieces are.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class NotorietySettings : SettingsMetaCategory {

		[SavedMember]
		public static NotorietySettings instance = new NotorietySettings();

		[LoadingInitializer]
		public NotorietySettings() {
		}

		/// <summary>Classic UO threshold at which a player's notoriety turns from innocent to
		/// "murderer" (red) - was a compile-time const before this, now GM-tunable.</summary>
		[SaveableData]
		public int murdererThreshold = 5;

		/// <summary>How long (real seconds) a criminal flag lasts after a single hostile act -
		/// the real 2-minute classic-UO default.</summary>
		[SaveableData]
		public double criminalFlagSeconds = 120;

		/// <summary>Whether a wild monster (NPCDef.AggroMode.Hostile) renders
		/// HighlightColor.Enemy (orange) instead of falling through to NoColor - a deliberate
		/// house-rule, not real OSI behavior (see Notoriety.GetHighlightColor's own comment).
		/// Default on; a GM who wants strict OSI-accurate notoriety can turn this off.</summary>
		[SaveableData]
		public bool wildMonstersRenderOrange = true;

		/// <summary>#918 - whether taking from the corpse of a fellow Gondorian is a crime, unless the
		/// owner is in the looter's party with "can loot me" on. A house rule: Erebor ran
		/// LOOTINGISACRIME=0.</summary>
		[SaveableData]
		public bool gondorLootingIsACrime = true;

		/// <summary>The same for Mordor, off: Mordor shows red and has no use for a criminal flag.</summary>
		[SaveableData]
		public bool mordorLootingIsACrime = false;
	}
}
