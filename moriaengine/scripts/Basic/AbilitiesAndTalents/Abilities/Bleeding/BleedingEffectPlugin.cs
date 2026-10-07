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

using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	//the similarity to DamagingPoison is not random :)
	public partial class BleedingEffectPlugin {

		public override void On_Assign() {

			//announce bleeding to both attacker and prey
			Character self = (Character) this.Cont;
			self.RedMessage(Loc<BleedingLoc>.Get(self.Language).YoureBleeding);

			Character attacker = this.SourceThing as Character;
			self.GreenMessage(string.Format(CultureInfo.InvariantCulture,
				Loc<BleedingLoc>.Get(self.Language).YourTargetIsBleeding, self.Name));


			base.On_Assign();
		}

		public override void On_FadingEffectTick() {
			Character self = this.Cont as Character;
			if (self != null) {

				double damage = DamageManager.CauseDamage(this.SourceThing as Character, self,
					DamageType.PhysicalBleed, this.EffectPower);

				if (damage < 1) { //once there's no damage (like when target is immune), remove
					this.Delete();
				}
			} else {
				this.Delete();
			}

			//0-4 are valid messages
			//this.AnnouncePoisonStrength((PoisonStrengthMessage) (this.EffectPower / 2));
		}

		//the names aren't really important, the relevant part here is that it's 0-4
		//public enum PoisonStrengthMessage {
		//    Zero = 0, Weakest = 0, Nauseous = 0, Ill = 0,
		//    One = 1, Weak = 1, DisorientedAndNauseous = 1, ExtremelyIll = 1,
		//    Two = 2, Normal = 2, FeelingPain = 2, StumblesAround = 2,
		//    Three = 3, Strong = 3, ExtremelyWeak = 3, WrackedWithPain = 3,
		//    Four = 4, Strongest = 4, ExtremePain = 4, SpasmingUncontrollably = 4
		//}

		//public void AnnouncePoisonStrength(PoisonStrengthMessage strength) {
		//    Character self = this.Cont as Character;
		//    if (self != null) {
		//        int roundedEffect = (int) strength;
		//        if (roundedEffect < 0) {
		//            roundedEffect = 0;
		//        } else if (roundedEffect > 4) {
		//            roundedEffect = 4;
		//        }

		//        self.ClilocEmote(1042858 + (roundedEffect * 2), 0x21, self.Name); //shouldn't see one's own cliloc emote?
		//        self.ClilocSysMessage(1042857 + (roundedEffect * 2), 0x21);
		//    }
		//}
	}

	[ViewableClass]
	public partial class BleedingEffectPluginDef {
		private static PassiveAbilityDef a_bleeding_maxpower_bonus;
		private static PassiveAbilityDef BleedingMaxPowerBonusDef {
			get {
				if (a_bleeding_maxpower_bonus == null) {
					a_bleeding_maxpower_bonus = (PassiveAbilityDef) AbilityDef.GetByDefname("a_bleeding_maxpower_bonus");
				}
				return a_bleeding_maxpower_bonus;
			}
		}

		public static bool IsBleeding(Character ch) {
			return ch.GetPlugin(SingletonScript<BleedingEffectPluginDef>.Instance.PluginKey) is BleedingEffectPlugin;
		}

		public static void StopBleeding(Character ch) {
			BleedingEffectPlugin p = ch.GetPlugin(SingletonScript<BleedingEffectPluginDef>.Instance.PluginKey) as BleedingEffectPlugin;
			if (p != null) {
				p.Delete();
			}
		}

		public override double GetMaxPower(Thing source, Character target, EffectFlag sourceType) {
			Character sourceChar = source as Character;
			if (sourceChar != null) {
				var def = BleedingMaxPowerBonusDef;
				return base.GetMaxPower(source, target, sourceType) *
					(1 + (sourceChar.GetAbility(def) * def.EffectPower));
			}

			return base.GetMaxPower(source, target, sourceType);
		}
	}


	public class BleedingLoc : CompiledLocStringCollection<BleedingLoc> {
		internal readonly string YoureBleeding = "Krvácíš!";
		internal readonly string YourTargetIsBleeding = "{0} krvácí!";
	}
}