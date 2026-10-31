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
	[ViewableClass]
	//represents the activated bleeding strike ability
	public partial class BleedingStrikePlugin {
		private static ActivableAbilityDef a_bleeding_strike;
		private static ActivableAbilityDef BleedingStrikeDef {
			get {
				if (a_bleeding_strike == null) {
					a_bleeding_strike = (ActivableAbilityDef) AbilityDef.GetByDefname("a_bleeding_strike");
				}
				return a_bleeding_strike;
			}
		}

		private static PassiveAbilityDef a_bleeding_bonus;
		private static PassiveAbilityDef BleedingBonusDef {
			get {
				if (a_bleeding_bonus == null) {
					a_bleeding_bonus = (PassiveAbilityDef) AbilityDef.GetByDefname("a_bleeding_bonus");
				}
				return a_bleeding_bonus;
			}
		}

		public void On_AfterSwing(WeaponSwingArgs swingArgs) {
			Character attacker = swingArgs.attacker;
			Weapon weapon = attacker.Weapon;
			Player player = attacker as Player;
			if ((swingArgs.FinalDamage > 0) && (weapon != null) && (player != null) && !swingArgs.defender.IsDeleted &&
					(swingArgs.defender.ResistBleed < 1000) && (Globals.dice.Next(100001) < CombatCalculator.CalculateClassAbility(player.EffectiveLevel))) {
				//#872 - f_thief_doBleed: the blow's share by weapon, nastaveni_weapbleed_*
				double bleedPower = swingArgs.FinalDamage * weapon.BleedingEfficiency * BleedingStrikeDef.EffectPower
					* (1 + (attacker.GetAbility(BleedingBonusDef) * BleedingBonusDef.EffectPower)); //bonus effect
				if (bleedPower > 0) {
					SingletonScript<BleedingEffectPluginDef>.Instance.Strike(attacker, swingArgs.defender, bleedPower);
				}
			}
		}
	}

	[ViewableClass]
	public partial class BleedingStrikePluginDef {
	}
}