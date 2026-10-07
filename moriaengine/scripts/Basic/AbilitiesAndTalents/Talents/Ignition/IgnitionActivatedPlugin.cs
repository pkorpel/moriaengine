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
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	//represents the activated ignition ability
	public partial class IgnitionActivatedPlugin {
		private static ActivableAbilityDef a_ignition;
		private static ActivableAbilityDef IgnitionDef {
			get {
				if (a_ignition == null) {
					a_ignition = (ActivableAbilityDef) AbilityDef.GetByDefname("a_ignition");
				}
				return a_ignition;
			}
		}

		private static PassiveAbilityDef a_ignition_bonus;
		private static PassiveAbilityDef IgnitionBonusDef {
			get {
				if (a_ignition_bonus == null) {
					a_ignition_bonus = (PassiveAbilityDef) AbilityDef.GetByDefname("a_ignition_bonus");
				}
				return a_ignition_bonus;
			}
		}

		private static SpellDef s_meteor_swarm;
		private static SpellDef MeteorSwarmDef {
			get {
				if (s_meteor_swarm == null) {
					s_meteor_swarm = SpellDef.GetByDefname("s_meteor_swarm");
				}
				return s_meteor_swarm;
			}
		}

		static PluginKey pkIgnitionEffect = PluginKey.Acquire("_ignition_effect_");

		public override void On_Assign() {
			//apply effect of the bonus talent, if present
			Character self = (Character) this.Cont;
			this.EffectPower *= 1 + (self.GetAbility(IgnitionBonusDef) * IgnitionBonusDef.EffectPower);
			base.On_Assign();
		}

		public void On_CauseSpellEffect(SpellEffectArgs spellEffectArgs) {
			if (spellEffectArgs.SpellDef == MeteorSwarmDef) {
				Character target = spellEffectArgs.CurrentTarget as Character;
				if (target != null) {
					ActivableAbilityDef ignition = IgnitionDef;

					int points = spellEffectArgs.Caster.GetAbility(ignition);
					if (points > 0) {
						if (ignition.CheckSuccess(points)) {

							double power = this.EffectPower;
							power *= DamageManager.GetResistModifier(target, DamageType.MagicFire); //apply fire resist

							IgnitionEffectPlugin ignitionPlugin = target.GetPlugin(pkIgnitionEffect) as IgnitionEffectPlugin;
							if (ignitionPlugin != null) {
								power += Math.Max(0, ignitionPlugin.EffectPower); //we add the power of previous ignition instance, if any
							}

							if (power > IgnitionEffectPlugin.minimumIgnitionEffect) { //else it does nothing to this target, so it's effectively immune
								ignitionPlugin = (IgnitionEffectPlugin) IgnitionEffectPluginDef.instance.Create();
								ignitionPlugin.Init(spellEffectArgs.Caster, EffectFlag.FromAbility | EffectFlag.HarmfulEffect,
									power, TimeSpan.FromSeconds(ignition.EffectDuration), IgnitionDef);
								target.AddPlugin(pkIgnitionEffect, ignitionPlugin);
							}
						}
					}
				}
			}
		}
	}

	[ViewableClass]
	public partial class IgnitionActivatedPluginDef {
	}
}