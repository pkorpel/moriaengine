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

using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1107, #1409 - a GM's .freeze (and .summoncage): the frozen character refuses every step in its @denyMove,
	/// with the frozen line.</summary>
	public partial class GmFreezePlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_gmFrozen_");

		public static bool IsFrozen(Character ch) {
			return ch.HasPlugin(key);
		}

		public static void Freeze(Character ch, bool on) {
			if (!on) {
				ch.DeletePlugin(key);
			} else if (!IsFrozen(ch)) {
				ch.AddPlugin(key, GmFreezePluginDef.instance.Create());
			}
		}

		public TriggerResult On_DenyMove(bool running) {
			Character self = (Character) this.Cont;
			self.SysMessage(Loc<GmToolsLoc>.Get(self.Language).Frozen);
			return TriggerResult.Cancel;
		}
	}

	public partial class GmFreezePluginDef {
		public static readonly GmFreezePluginDef instance =
			(GmFreezePluginDef) new GmFreezePluginDef("p_gm_freeze", "C# scripts", -1).Register();
	}
}
