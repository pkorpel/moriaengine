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

using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>Server-side helpers for the RegenerationPlugin reload regression test (#436) -
	/// mirrors #434/#438's own real save -> restart -> reload pattern.</summary>
	public static class RegenerationPluginTests {

		/// <summary>Damages self below MaxHits - RegenerationPlugin.TryInstallPlugin installs
		/// the plugin automatically as a side effect of the Hits setter whenever hits < maxHits
		/// AND regen speed is positive. HitsRegenSpeed defaults to 0.0 for a fresh character
		/// (only ever adjusted via abilities/effects, no baseline) - set it explicitly here so
		/// damaging self actually triggers the install, same as a real regen-granting ability
		/// would in production.</summary>
		[SteamFunction]
		public static void TestDamageSelfToTriggerRegen(Character self) {
			self.HitsRegenSpeed = 1.0;
			self.Hits = (short) (self.MaxHits - 10);
			Console.WriteLine("REGEN_DAMAGE: hits=" + self.Hits + " maxHits=" + self.MaxHits +
				" hasRegenPlugin=" + self.HasPlugin(RegenerationPlugin.regenerationsPluginKey));
		}

		[SteamFunction]
		public static void TestCheckRegenPlugin(Character self) {
			Console.WriteLine("REGEN_PLUGIN_STATE: hasRegenPlugin=" + self.HasPlugin(RegenerationPlugin.regenerationsPluginKey) +
				" hits=" + self.Hits + " maxHits=" + self.MaxHits);
		}
	}
}
