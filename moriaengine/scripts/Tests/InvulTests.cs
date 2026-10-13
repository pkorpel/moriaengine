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
using System.Globalization;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side helper for the ".invul" protocol test (Tests/Protocol/InvulTests.cs). Calls
	/// DamageManager.CauseDamage directly (self-on-self) rather than simulating a full weapon
	/// swing - the thing under test is the Flag_Invulnerable short-circuit at the top of
	/// CauseDamage itself, not weapon-swing mechanics, which already has its own coverage
	/// elsewhere (CombatDepthTests).
	/// </summary>
	public static class InvulTests {
		[SteamFunction]
		public static void TestApplyDamage(Character self, ScriptArgs sa) {
			int amount = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			int actualDamage = DamageManager.CauseDamage(self, self, DamageType.PhysicalBlunt, amount);
			Console.WriteLine("DAMAGE_TEST: actualDamage=" + actualDamage + " hits=" + self.Hits);
		}
	}
}
