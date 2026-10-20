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

using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>#483/#491 - Standcast's toggle marker: its mere presence means the caster's next
	/// successful Magery cast gets a one-shot spell-power boost (see SpellDef.
	/// GetSpellPowerAgainstChar/AgainstNonChar), consumed right after in Magery.On_Success. No
	/// data fields needed, so this is a hand-written Plugin/PluginDef pair (same minimal shape as
	/// the existing TimerPlugin.cs) rather than a [PluginTemplate]/.ct-generated one - there's
	/// nothing for the generator to generate.</summary>
	public class StandcastPluginDef : PluginDef {
		public StandcastPluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new StandcastPlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(StandcastPluginDef), typeof(StandcastPlugin));
		}
	}

	[DeepCopyableClass]
	[SaveableClass]
	public class StandcastPlugin : Plugin {
		[DeepCopyImplementation]
		public StandcastPlugin(StandcastPlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public StandcastPlugin() {
		}
	}
}
