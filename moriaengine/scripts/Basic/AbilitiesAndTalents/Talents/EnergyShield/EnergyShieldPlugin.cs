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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	//represents the activated ignition ability
	public partial class EnergyShieldPlugin {
		private static ActivableAbilityDef a_energy_shield;
		private static ActivableAbilityDef EnergyShieldDef {
			get {
				if (a_energy_shield == null) {
					a_energy_shield = (ActivableAbilityDef) AbilityDef.GetByDefname("a_energy_shield");
				}
				return a_energy_shield;
			}
		}

		//TODO? Effectivity
		public void On_Damage(DamageArgs args) {
			if (!args.attacker.IsPlayer) {
				this.EffectPower -= args.damage;

				Character self = (Character) this.Cont;
				if (Math.Round(this.EffectPower) > 0) {
					//do no damage at all
					self.Hits += (short) Math.Round(args.damage);
					self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).shieldSavedYou);
				} else {
					//do some damage still (damage + effectpower, but the effectpower part is negative...)
					self.Hits += (short) Math.Round(args.damage + this.EffectPower);
					self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).shieldSavedYouPartially);
					this.Delete();
				}
			}
		}

		public TriggerResult On_SkillSelect(SkillSequenceArgs skillSeq) {
			return this.CheckAllowedSpells(skillSeq);
		}

		public TriggerResult On_SkillStart(SkillSequenceArgs skillSeq) {
			return this.CheckAllowedSpells(skillSeq);
		}

		private TriggerResult CheckAllowedSpells(SkillSequenceArgs skillSeq) {
			if (skillSeq.SkillDef.Id == (int) SkillName.Magery) {
				SpellDef spell = (SpellDef) skillSeq.Param1;
				switch (spell.Id) {
					case 22: //teleport
					case 32: //recall
					case 52: //gate
						Character self = skillSeq.Self;
						self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).cantTeleport);
						//return true; //cancel
						this.Delete(); //cancel travelling spell, or cancel the shield?
						break;
				}
			}
			return TriggerResult.Continue;
		}

		protected override void EffectEndedMessage(Character cont) {
			if (this.EffectPower <= 0.5) {
				cont.WriteLine(Loc<EnergyShieldLoc>.Get(cont.Language).shieldWasted);
			} else {
				base.EffectEndedMessage(cont);
			}
		}

		public TriggerResult On_Step(Direction direction, bool running) {
			Character self = (Character) this.Cont;
			self.WriteLine(Loc<EnergyShieldLoc>.Get(self.Language).cantMove);
			//return true; //cancel
			this.Delete(); //cancel movement, or cancel the shield?
			return TriggerResult.Continue;
		}
	}

	[ViewableClass]
	public partial class EnergyShieldPluginDef {
	}

	public class EnergyShieldLoc : CompiledLocStringCollection<EnergyShieldLoc> {
		public string cantTeleport = "Bìhem trvání kouzla Energy shield nemùžeš používat teleportaèní kouzla";
		public string cantMove = "Bìhem trvání kouzla Energy shield se nemùžeš pohybovat";
		public string shieldWasted = "Tvùj Energy shield se vyèerpal";
		public string shieldSavedYou = "Energy shield tì uchránil pøed zranìním";
		public string shieldSavedYouPartially = "Energy shield tì èásteènì ochránil pøed zranìním";
	}
}