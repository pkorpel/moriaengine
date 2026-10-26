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

	/// <summary>Real-user-reported (hit via c_f_healer's granted "(e_healer)" stub ability,
	/// #679): Ability's own "never used" sentinel is TimeSpan.MinValue (see Ability.cs's ctor
	/// comment) - but AbilityDef.On_DenyActivate used to compute Globals.TimeAsSpan -
	/// ab.LastUsage unconditionally, which always throws OverflowException when LastUsage is
	/// still that sentinel (TimeSpan.MinValue has no positive counterpart, same class of bug
	/// as int.MinValue negation). Confirmed via direct repro: activating ANY never-yet-used
	/// Cooldown-bearing ability threw this, repeating every time the engine tried again.</summary>
	public static class AbilityCooldownOverflowTests {
		[SteamFunction]
		public static void TestActivateNeverUsedAbility(Character self) {
			AbilityDef abilityDef = AbilityDef.GetByDefname("a_tevent_e_healer");
			self.SetRealAbilityPoints(abilityDef, 1);
			abilityDef.Activate(self); //would throw OverflowException before the fix
			Console.WriteLine("ABILITY_ACTIVATE_TEST: no exception");
		}
	}
}
