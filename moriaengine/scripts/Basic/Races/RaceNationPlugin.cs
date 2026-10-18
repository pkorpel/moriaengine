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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Holds a player's chosen Race and Nation (#410) - a combined plugin per the user's
	/// explicit design call, since both are set together at character creation and never
	/// change independently of each other. Mirrors ProfessionPlugin's install/read/delete-
	/// via-PluginKey shape, but uses the simpler single-registered-instance pattern
	/// (RegenerationPlugin/SingletonScript&lt;T&gt;) since there's no per-race/per-nation
	/// authored .def content the way Profession has one file per profession.
	/// </summary>
	[ViewableClass]
	public partial class RaceNationPlugin {
		internal static PluginKey raceNationKey = PluginKey.Acquire("_racenation_");

		public static readonly RaceNationPluginDef defInstance = (RaceNationPluginDef)
			new RaceNationPluginDef("p_racenation", "C# scripts", -1).Register();

		public Race Race {
			get {
				return this.race;
			}
			set {
				this.race = value;
			}
		}

		public Nation Nation {
			get {
				return this.nation;
			}
			set {
				this.nation = value;
			}
		}

		public static RaceNationPlugin GetInstalledPlugin(Player player) {
			return (RaceNationPlugin) player.GetPlugin(raceNationKey);
		}

		public static RaceNationPlugin GetOrInstallPlugin(Player player) {
			RaceNationPlugin plugin = GetInstalledPlugin(player);
			if (plugin == null) {
				plugin = (RaceNationPlugin) defInstance.Create();
				player.AddPlugin(raceNationKey, plugin);
			}
			return plugin;
		}
	}

	[ViewableClass]
	public partial class RaceNationPluginDef {
	}
}
