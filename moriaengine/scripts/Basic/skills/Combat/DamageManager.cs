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

namespace SteamEngine.CompiledScripts {

	public static class DamageManager {
		public static double GetResistModifier(Character resistingChar, DamageType damageType) {
			int intDamageType = (int) damageType;

			//phase 1
			double isMagic = ((intDamageType & 0x0001) == 0) ? 0.0 : 1;
			double isPhysical = ((intDamageType & 0x0002) == 0) ? 0.0 : 1;

			double modifier = 1;
			double resistCount = isMagic + isPhysical;
			modifier = modifier * resistCount;
			Sanity.IfTrueThrow(resistCount == 0, "Attack of type '" + damageType + "' - neither magical nor physical");

			modifier -= isMagic * resistingChar.ResistMagic;
			modifier -= isPhysical * resistingChar.ResistPhysical;

			//phase 2
			double isFire = ((intDamageType & (int) DamageType.Fire) == 0) ? 0.0 : 1;
			double isElectric = ((intDamageType & (int) DamageType.Electric) == 0) ? 0.0 : 1;
			double isAcid = ((intDamageType & (int) DamageType.Acid) == 0) ? 0.0 : 1;
			double isCold = ((intDamageType & (int) DamageType.Cold) == 0) ? 0.0 : 1;
			double isPoison = ((intDamageType & (int) DamageType.Poison) == 0) ? 0.0 : 1;
			double isMystical = ((intDamageType & (int) DamageType.Mystical) == 0) ? 0.0 : 1;
			double isSlashing = ((intDamageType & (int) DamageType.Slashing) == 0) ? 0.0 : 1;
			double isStabbing = ((intDamageType & (int) DamageType.Stabbing) == 0) ? 0.0 : 1;
			double isBlunt = ((intDamageType & (int) DamageType.Blunt) == 0) ? 0.0 : 1;
			double isArchery = ((intDamageType & (int) DamageType.Archery) == 0) ? 0.0 : 1;
			double isBleed = ((intDamageType & (int) DamageType.Bleed) == 0) ? 0.0 : 1;
			double isSummon = ((intDamageType & (int) DamageType.Summon) == 0) ? 0.0 : 1;
			double isDragon = ((intDamageType & (int) DamageType.Dragon) == 0) ? 0.0 : 1;

			resistCount = isFire + isElectric + isAcid + isCold +
				isPoison + isMystical + isSlashing + isStabbing +
				isBlunt + isArchery + isBleed + isSummon + isDragon;
			Sanity.IfTrueThrow(resistCount == 0, "Attack of type '" + damageType + "' - has no resist subtype");

			modifier = modifier * resistCount;

			modifier -= isFire * resistingChar.ResistFire;
			modifier -= isElectric * resistingChar.ResistElectric;
			modifier -= isAcid * resistingChar.ResistAcid;
			modifier -= isCold * resistingChar.ResistCold;
			modifier -= isPoison * resistingChar.ResistPoison;
			modifier -= isMystical * resistingChar.ResistMystical;
			modifier -= isSlashing * resistingChar.ResistSlashing;
			modifier -= isStabbing * resistingChar.ResistStabbing;
			modifier -= isBlunt * resistingChar.ResistBlunt;
			modifier -= isArchery * resistingChar.ResistArchery;
			modifier -= isBleed * resistingChar.ResistBleed;
			modifier -= isSummon * resistingChar.ResistSummon;
			modifier -= isDragon * resistingChar.ResistDragon;

			return modifier;
		}

		/// <summary>Cause damage to defender. Returns number of hitpoints that was actually removed. Runs @Damage triggers.</summary>
		public static int CauseDamage(Character attacker, Character defender, DamageType flags, double damage) {
			if (!defender.IsDeleted && !defender.Flag_Dead && !defender.Flag_Insubst) {
				defender.Trigger_HostileAction(attacker);
				defender.Trigger_Disrupt();//nebo jen pri damage > 0 ?

				damage *= GetResistModifier(defender, flags);

				int previousHits = defender.Hits;

				DamageArgs damageArgs = new DamageArgs(attacker, defender, flags, damage);
				Trigger_Damage(damageArgs);

				damage = damageArgs.damage;
				int newHits = (int) Math.Round(defender.Hits - damage);
				defender.Hits = (short) newHits;

				int actualDamage = previousHits - newHits;
				if (actualDamage > 0) {
					//TODO create blood?
					if (newHits > 0) {
						SoundCalculator.PlayHurtSound(defender);
						AnimCalculator.PerformAnim(defender, GenericAnim.GetHit);
					} //otherwise death sound+anim, is done elsewhere
				}

				return actualDamage;
			}

			return 0;
		}

		public static void ProcessSwing(Character attacker, Character defender) {
			WeaponSwingArgs swingArgs = GetWeaponSwingArgs(attacker, defender);
			SoundCalculator.PlayAttackSound(attacker);

			if (TriggerResult.Cancel != Trigger_BeforeSwing(swingArgs)) {
				int actualDamage = CauseDamage(attacker, defender, attacker.WeaponDamageType, swingArgs.DamageAfterAC);

				swingArgs.InternalSetFinalDamage(actualDamage);

				Trigger_AfterSwing(swingArgs);
			}
		}

		public static WeaponSwingArgs GetWeaponSwingArgs(Character attacker, Character defender) {
			CombatSettings settings = CombatSettings.instance;
			double attack = settings.weapAttack;
			double piercing = attacker.WeaponPiercing;

			bool defenderIsPlayer = defender.IsPlayerForCombat;
			bool attackerIsPlayer = attacker.IsPlayerForCombat;

			double armorClass;
			if (attackerIsPlayer) {
				if (defenderIsPlayer) {
					attack *= settings.weapAttackPvP;
				}
				armorClass = defender.ArmorClassVsP;
			} else {
				attack *= settings.weapAttackM;
				armorClass = defender.ArmorClassVsM;
			}
			if (defenderIsPlayer) {
				attack *= attacker.WeaponAttackVsP;
				armorClass *= settings.armorClassP;
			} else {
				attack *= attacker.WeaponAttackVsM;
				attack *= settings.weapAttackVsM;
				armorClass *= settings.armorClassM;
			}

			double damageAfterAC = CombatCalculator.CalculateArmorClassEffect(attacker, defender, attack, piercing, armorClass);

			return new WeaponSwingArgs(attacker, defender, attack, piercing, armorClass, damageAfterAC);
		}


		static TriggerKey beforeSwingTK = TriggerKey.Acquire("beforeSwing");
		static TriggerKey beforeGetSwingTK = TriggerKey.Acquire("beforeGetSwing");

		/// <summary>Happens before applying armor, can be cancelled.</summary>
		public static TriggerResult Trigger_BeforeSwing(WeaponSwingArgs swingArgs) {
			var result = swingArgs.attacker.TryCancellableTrigger(beforeSwingTK, swingArgs);
			if (result != TriggerResult.Cancel) {
				try {
					result = swingArgs.attacker.On_BeforeSwing(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
				if (result != TriggerResult.Cancel) {
					result = swingArgs.defender.TryCancellableTrigger(beforeGetSwingTK, swingArgs);
					if (result != TriggerResult.Cancel) {
						try {
							result = swingArgs.defender.On_BeforeGetSwing(swingArgs);
						} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
					}
				}
			}
			return result;
		}

		static TriggerKey causeDamageTK = TriggerKey.Acquire("causeDamage");
		static TriggerKey damageTK = TriggerKey.Acquire("damage");

		/// <summary>Happens before applying armor. If a script should want to negate or alter the damage, it should 'manually' alter Hits, and/or create a new damaging event altogether.</summary>
		public static void Trigger_Damage(DamageArgs damageArgs) {
			damageArgs.attacker.TryTrigger(causeDamageTK, damageArgs);
			try {
				damageArgs.attacker.On_CauseDamage(damageArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			damageArgs.defender.TryTrigger(damageTK, damageArgs);
			try {
				damageArgs.defender.On_Damage(damageArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
		}

		static TriggerKey afterSwingTK = TriggerKey.Acquire("afterSwing");
		static TriggerKey afterGetSwingTK = TriggerKey.Acquire("afterGetSwing");

		/// <summary>Happens after applying armor, can be cancelled.</summary>
		public static void Trigger_AfterSwing(WeaponSwingArgs swingArgs) {
			swingArgs.attacker.TryTrigger(afterSwingTK, swingArgs);

			try {
				swingArgs.attacker.On_AfterSwing(swingArgs);
			} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }

			if (!swingArgs.defender.IsDeleted) {
				swingArgs.defender.TryTrigger(afterGetSwingTK, swingArgs);

				try {
					swingArgs.defender.On_AfterGetSwing(swingArgs);
				} catch (FatalException) { throw; } catch (Exception e) { Logger.WriteError(e); }
			}
		}
	}

	public class DamageArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public readonly DamageType flags;
		public readonly double damage;

		public DamageArgs(Character attacker, Character defender, DamageType flags, double damage)
			: base(attacker, defender, flags, damage) {
			this.defender = defender;
			this.attacker = attacker;
			this.flags = flags;
			this.damage = damage;
		}
	}

	public class WeaponSwingArgs : ScriptArgs {
		public readonly Character defender;
		public readonly Character attacker;
		public readonly double attack, piercing, armorClass;
		int finalDamage;

		public WeaponSwingArgs(Character attacker, Character defender, double attack, double piercing, double armorClass, double damageAfterAC)
			: base(attacker, defender, attack, piercing, armorClass, damageAfterAC, -1) {
			this.attacker = attacker;
			this.defender = defender;
			this.attack = attack;
			this.piercing = piercing;
			this.armorClass = armorClass;
		}

		public double DamageAfterAC {
			get {
				return Convert.ToDouble(this.Argv[5]);
			}
			set {
				this.Argv[5] = value;
			}
		}

		public int FinalDamage {
			get {
				return this.finalDamage;
			}
		}

		internal void InternalSetFinalDamage(int value) {
			this.Argv[6] = value;
			this.finalDamage = value;
		}
	}
}

