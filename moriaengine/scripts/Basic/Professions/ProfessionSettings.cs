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
	/// GM-tunable settings for the professions system (#134), shown as a "Profession System"
	/// category in the .settings dialog - same pattern as LevelingSettings/CombatSettings.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class ProfessionSettings : SettingsMetaCategory {

		[SavedMember]
		public static ProfessionSettings instance = new ProfessionSettings();

		[LoadingInitializer]
		public ProfessionSettings() {
		}

		/// <summary>Whether ProfessionPlugin.On_DenyEquip (#138) actually blocks equipping a
		/// weapon/armor type the current profession's .def doesn't allow. Default on; a GM can
		/// turn this off to test equipment freely without reassigning professions.</summary>
		[SaveableData]
		public bool enforceWeaponArmorRestrictions = true;

		/// <summary>Whether assigning a profession (ProfessionPlugin.On_Assign, #135) grants the
		/// .def's starting skill minimums immediately. Default on; a GM can turn this off to
		/// test switching a character's profession without the skill side effect.</summary>
		[SaveableData]
		public bool grantSkillsOnAssign = true;
	}
}
