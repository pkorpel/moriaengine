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

	/// <summary>#483/#491 - Standcast is blocked while Energy Shield is active, matching the real
	/// Erebor `standcast` function's own active-shield check (energyShield.scp's own plugin
	/// already blocks movement/teleport spells while active, but had no reciprocal check against
	/// Standcast - this is the other half of that interaction).</summary>
	public class StandcastAbilityDef : ActivableAbilityDef {
		private static ActivableAbilityDef energyShieldDef;
		private static ActivableAbilityDef EnergyShieldDef {
			get {
				if (energyShieldDef == null) {
					energyShieldDef = (ActivableAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
				}
				return energyShieldDef;
			}
		}

		public StandcastAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override void On_DenyActivate(DenyAbilityArgs args) {
			ActivableAbilityDef energyShield = EnergyShieldDef;
			if ((energyShield != null) && energyShield.IsActive(args.abiliter)) {
				args.Result = DenyResultMessages_Abilities.Deny_NotAllowedToHaveThisAbility;
				return;
			}
			base.On_DenyActivate(args);
		}
	}
}
