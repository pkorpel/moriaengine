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
	/// <summary>#1023, #1409 - Erebor's i_looter_immobilizer: every pick at another's body holds the looter for a while (each
	/// pick adds to what's left), refusing steps in @denyMove until its timer runs out.</summary>
	public partial class LootHoldPlugin {
		public static readonly PluginKey key = PluginKey.Acquire("_lootHold_");

		public static bool IsHeld(Character ch) {
			return ch.HasPlugin(key);
		}

		/// <summary>Holds for `seconds` more than what's left (or from now).</summary>
		public static void Extend(Character ch, double seconds) {
			LootHoldPlugin hold = ch.GetPlugin(key) as LootHoldPlugin;
			if (hold == null) {
				hold = (LootHoldPlugin) LootHoldPluginDef.instance.Create();
				ch.AddPlugin(key, hold);
				hold.Timer = seconds;
				return;
			}
			hold.Timer = System.Math.Max(hold.Timer, 0) + seconds;
		}

		public TriggerResult On_DenyMove(bool running) {
			return TriggerResult.Cancel;
		}

		public void On_Timer() {
			this.Delete();
		}
	}

	public partial class LootHoldPluginDef {
		public static readonly LootHoldPluginDef instance =
			(LootHoldPluginDef) new LootHoldPluginDef("p_loot_hold", "C# scripts", -1).Register();
	}
}
