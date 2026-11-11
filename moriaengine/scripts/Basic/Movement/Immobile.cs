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

namespace SteamEngine.CompiledScripts {
	/// <summary>#1409 - Sphere's flag_immobile, as a plugin refusing every step in @denyMove (no line): a mystic's catch
	/// and catch field, a hypnotized body and its alter ego, Erebor's player vendors. One flag, as on Erebor: whoever
	/// clears it clears it. A blow is still possible.</summary>
	public partial class ImmobilePlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_immobile_");

		public static bool IsImmobile(Character ch) {
			return ch.HasPlugin(key);
		}

		public static void Set(Character ch, bool on) {
			if (!on) {
				ch.DeletePlugin(key);
			} else if (!IsImmobile(ch)) {
				ch.AddPlugin(key, ImmobilePluginDef.instance.Create());
			}
		}

		public TriggerResult On_DenyMove(bool running) {
			return TriggerResult.Cancel;
		}
	}

	public partial class ImmobilePluginDef {
		public static readonly ImmobilePluginDef instance =
			(ImmobilePluginDef) new ImmobilePluginDef("p_immobile", "C# scripts", -1).Register();
	}
}
