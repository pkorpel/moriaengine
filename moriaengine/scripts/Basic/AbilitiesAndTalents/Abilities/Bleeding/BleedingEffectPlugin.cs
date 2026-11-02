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
using System;
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

		private static readonly TagKey bleedPowerTK = TagKey.Acquire("_bleedPower_");
		private static readonly TagKey bleedTicksTK = TagKey.Acquire("_bleedTicks_");

		internal double BleedPower {
			get { return Convert.ToDouble(this.GetTag(bleedPowerTK) ?? 0.0); }
			set { this.SetTag(bleedPowerTK, value); }
		}

		internal int BleedTicks {
			get { return Convert.ToInt32(this.GetTag(bleedTicksTK) ?? 0); }
			set { this.SetTag(bleedTicksTK, value); }
		}

		/// <summary>#872 - i_krvaceni's timer: fades by f_bleed_calculateCurrentValue.</summary>
		public override void On_FadingEffectTick() {
			Character self = this.Cont as Character;
			int ticks = this.BleedTicks;
			if ((self == null) || (ticks >= BleedingEffectPluginDef.BleedTickCount)) {
				this.Delete();
				return;
			}
			double damage = BleedingEffectPluginDef.CalculateCurrentValue(this.BleedPower, ticks, BleedingEffectPluginDef.DamageCoefficient);
			bool interrupts = Globals.dice.Next(1000) < CombatSettings.instance.bleedFizzleChance;
			double dealt = DamageManager.CauseBleedDamage(this.SourceThing as Character, self, DamageType.PhysicalBleed, damage, interrupts);
			if (dealt < 1) { //once there's no damage (like when target is immune), remove
				this.Delete();
				return;
			}
			if (!self.IsDeleted) {
				this.BleedTicks = ticks + 1;
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

		internal const int BleedTickCount = 10;
		internal const int DamageCoefficient = 1, CapCoefficient = 2;

		/// <summary>#872 - f_bleed_calculateCurrentValue: a quadratic fade over the ten ticks. The cap
		/// fades half as fast, so a stack that's been running keeps some of what it reached.</summary>
		public static double CalculateCurrentValue(double power, int ticks, int coefficient) {
			double fade = 1000 - Math.Truncate((ticks * ticks * 1000.0) / (BleedTickCount * BleedTickCount * coefficient));
			return Math.Truncate((power * fade) / 1000);
		}

		/// <summary>#872 - f_thief_doBleed on a target already bleeding: what's left plus the new
		/// blow, up to the attacker's cap or what the stack could still hold.</summary>
		public static double CalculateReapply(double power, int ticks, double addedPower, double cap) {
			double stackCap = CalculateCurrentValue(power, ticks, CapCoefficient);
			return Math.Min(CalculateCurrentValue(power, ticks, DamageCoefficient) + addedPower, Math.Max(cap, stackCap));
		}

		/// <summary>#872 - f_thief_doBleed. Every hit restarts the ten ticks.</summary>
		public void Strike(Character attacker, Character target, double addedPower) {
			EffectFlag flags = EffectFlag.HarmfulEffect | EffectFlag.FromAbility;
			TimeSpan duration = TimeSpan.FromSeconds(this.TickInterval * (BleedTickCount + 1));
			double cap = this.GetMaxPower(attacker, target, flags);
			BleedingEffectPlugin bleeding = target.GetPlugin(this.PluginKey) as BleedingEffectPlugin;
			if (bleeding != null) {
				double power = CalculateReapply(bleeding.BleedPower, bleeding.BleedTicks, addedPower, cap);
				bleeding.Init(attacker, flags, power, duration);
				bleeding.BleedPower = power;
				bleeding.BleedTicks = 0;
				return;
			}
			bleeding = (BleedingEffectPlugin) this.Create();
			double firstPower = Math.Min(addedPower, cap);
			bleeding.Init(attacker, flags, firstPower, duration);
			bleeding.BleedPower = firstPower;
			target.AddPlugin(this.PluginKey, bleeding);
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
				double cap = base.GetMaxPower(source, target, sourceType);
				Player player = sourceChar as Player;
				if ((player != null) && Professions.HasProfession(player, Professions.Thief)) {
					cap = CombatSettings.instance.thiefBleedCap + Math.Min(player.Level, 200);
				}
				return cap * (1 + (sourceChar.GetAbility(def) * def.EffectPower));
			}

			return base.GetMaxPower(source, target, sourceType);
		}
	}


	public class BleedingLoc : CompiledLocStringCollection<BleedingLoc> {
		internal readonly string YoureBleeding = "Krvacis!";
		internal readonly string YourTargetIsBleeding = "{0} krvaci!";
	}
}