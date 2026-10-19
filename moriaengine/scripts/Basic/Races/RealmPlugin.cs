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
	/// Holds a character's Realm (#410) - kept as its OWN plugin, separate from
	/// RaceNationPlugin, per the user's explicit design call: Realm may need to change
	/// independently of Race/Nation later (e.g. a renegade mechanic). Installed on
	/// Character, not Player - Realm is also used on plain NPCs (guard-type aggro, see
	/// NPCAiTimer.ShouldAggroOn), confirmed via existing test usage like
	/// "guard.Realm = Realm.Gondor".
	/// </summary>
	[ViewableClass]
	public partial class RealmPlugin {
		internal static PluginKey realmKey = PluginKey.Acquire("_realmplugin_");

		public static readonly RealmPluginDef defInstance = (RealmPluginDef)
			new RealmPluginDef("p_realm", "C# scripts", -1).Register();

		public Realm Realm {
			get {
				return this.realm;
			}
			set {
				this.realm = value;
			}
		}

		public static RealmPlugin GetInstalledPlugin(Character character) {
			return (RealmPlugin) character.GetPlugin(realmKey);
		}

		public static RealmPlugin GetOrInstallPlugin(Character character) {
			RealmPlugin plugin = GetInstalledPlugin(character);
			if (plugin == null) {
				plugin = (RealmPlugin) defInstance.Create();
				character.AddPlugin(realmKey, plugin);
			}
			return plugin;
		}
	}

	[ViewableClass]
	public partial class RealmPluginDef {
		/// <summary>#434 - same fix/reasoning as RaceNationPluginDef's own static cctor (see
		/// RaceNationPlugin.cs): forces RealmPlugin's static cctor (defInstance.Register(),
		/// which registers "p_realm") to run during ClassManager.InitClasses's early
		/// Bootstrap-method scan, not lazily on first GetOrInstallPlugin call - otherwise
		/// "p_realm" is still unregistered when WorldSaver.Load() needs to resolve an existing
		/// RealmPlugin's "def=#p_realm" reference on a cold boot, and the plugin gets silently
		/// deleted as "orphaned".</summary>
		static RealmPluginDef() {
			System.GC.KeepAlive(RealmPlugin.defInstance);
		}
	}
}
