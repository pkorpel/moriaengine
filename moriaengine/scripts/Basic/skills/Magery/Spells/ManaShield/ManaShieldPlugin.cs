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
	public partial class ManaShieldPlugin {

		//private static DurableCharEffectSpellDef s_mana_shield;
		//private static DurableCharEffectSpellDef ManaShieldDef {
		//    get {
		//        if (s_mana_shield == null) {
		//            s_mana_shield = (DurableCharEffectSpellDef) SpellDef.GetByDefname("s_mana_shield");
		//        }
		//        return s_mana_shield;
		//    }
		//}

		public void On_Damage(DamageArgs args) {
			Character self = (Character) this.Cont;

			int damage = (int) Math.Round(args.damage);
			int newMana = self.Mana - damage;

			if (newMana > 0) {
				//all damage goes to mana
				self.Hits += (short) damage; //add to hits what is about to be deducted in the script that calls this trigger
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).shieldSavedYou);
				self.Mana = (short) newMana;
			} else {
				//dmg partially routed to mana (damage + newMana, but the newMana part is negative...)
				self.Hits += (short) (damage + newMana);
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).shieldSavedYouPartially);
				self.Mana = 0;
				this.Delete();
			}			
		}

		//interrupt on meditation attempt
		public TriggerResult On_SkillSelect(SkillSequenceArgs skillSeq) {
			return this.CheckStartingSkill(skillSeq);
		}

		public TriggerResult On_SkillStart(SkillSequenceArgs skillSeq) {
			return this.CheckStartingSkill(skillSeq);
		}

		private TriggerResult CheckStartingSkill(SkillSequenceArgs skillSeq) {
			if (skillSeq.SkillDef.Id == (int) SkillName.Meditation) {
				Character self = (Character) this.Cont;
				self.WriteLine(Loc<ManaShieldLoc>.Get(self.Language).cantMeditateWithManaShield);
				this.Delete();
			}
			return TriggerResult.Continue;
		}

		public override void On_UnAssign(Character cont) {
			//lower his mana if it's not already lowered. It's kind of weird but I got it verbatim from sphere script :o
			int maxMana = cont.MaxMana;
			if (cont.Mana > maxMana / 2) {
				cont.Mana -= (short) (maxMana / 3);
			} else {
				cont.Mana = 0;
			}

			base.On_UnAssign(cont);
		}
	}

	[ViewableClass]
	public partial class ManaShieldPluginDef {
	}

	public class ManaShieldLoc : CompiledLocStringCollection<ManaShieldLoc> {
		public string cantMeditateWithManaShield = "Soustøedit se zároveò na meditaci i mana shield není v lidských silách.";
		public string shieldSavedYou = "Mana shield tì uchránil pøed zranìním";
		public string shieldSavedYouPartially = "Mana shield tì èásteènì ochránil pøed zranìním";
	}
}