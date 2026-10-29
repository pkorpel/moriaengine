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
	/// #828 - GM-tunable rules of Erebor's skill teachers (spk_mistr, SkillTeacherSpeech), shown as a
	/// "Skill Teachers" category in the .settings dialog. Defaults are the archive's (systems/mistri.scp,
	/// [defnames mistr_settings]) except requireRank: the player ranks come from Erebor's rank quests (#833),
	/// so until those are ported teachers don't ask for one.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class SkillTeacherSettings : SettingsMetaCategory {

		[SavedMember]
		public static SkillTeacherSettings instance = new SkillTeacherSettings();

		[LoadingInitializer]
		public SkillTeacherSettings() {
		}

		/// <summary>A lesson needs the player's rank (Player.SkillRank): Expert for 25->30, Master for 50->55,
		/// Grandmaster for 75->80, as in Erebor. Off until the rank quests (#833) let players earn ranks.</summary>
		[SaveableData]
		public bool requireRank = false;

		/// <summary>Erebor's roundskill: greeting a teacher rounds the taught skill to the nearest 5.0 (never
		/// below the profession's starting value) - 27.4 becomes 25.0, 27.6 becomes 30.0.</summary>
		[SaveableData]
		public bool roundSkillOnGreeting = true;

		[SaveableData]
		public int expertPrice = 5000;

		[SaveableData]
		public int masterPrice = 10000;

		[SaveableData]
		public int grandmasterPrice = 20000;

		/// <summary>The lowest player level for each lesson (archive: level &gt; 2 / 5 / 11).</summary>
		[SaveableData]
		public int expertMinLevel = 3;

		[SaveableData]
		public int masterMinLevel = 6;

		[SaveableData]
		public int grandmasterMinLevel = 12;
	}
}
