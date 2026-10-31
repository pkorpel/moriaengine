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
	/// GM-tunable settings for the leveling/experience system (#125), shown as a "Level
	/// System" category in the .settings dialog (see SettingsCategories in MetaSettings.cs -
	/// same pattern as CombatSettings/MagerySettings). Persists across restarts via the
	/// generic SaveableClass/HasSavedMembers pipeline, same as those.
	/// </summary>
	[SaveableClass]
	[HasSavedMembers]
	[ViewableClass]
	public class LevelingSettings : SettingsMetaCategory {

		[SavedMember]
		public static LevelingSettings instance = new LevelingSettings();

		[LoadingInitializer]
		public LevelingSettings() {
		}

		/// <summary>The multiplier in the level threshold formula: level N needs N * this many experience points. Erebor's own real constant (nextlevel=330 in its experiences.scp) is the default.</summary>
		[SaveableData]
		public int nextLevelCoefficient = 330;

		/// <summary>#902 - f_countfight: who fought a victim counts within this many tiles of it, on its plane.</summary>
		[SaveableData]
		public int fightCreditRange = 20;

		/// <summary>#902 - how long ago the last blow can be for someone to still count as having fought.</summary>
		[SaveableData]
		public int fightCreditSeconds = 120;

		/// <summary>#902 - in Gondor's and Mordor's areas a monster pays each fighter at most this, elites
		/// excepted. 0 turns the cap off.</summary>
		[SaveableData]
		public int monsterExpCap = 100;

		/// <summary>#902 - f_ztratitfame: what a death costs, as a percentage of what the next level takes.</summary>
		[SaveableData]
		public int deathExpLossPercent = 5;

		/// <summary>#902 - how much of what a player lost goes to those who fought them, shared evenly.</summary>
		[SaveableData]
		public int deathExpPaidPercent = 100;
	}
}
