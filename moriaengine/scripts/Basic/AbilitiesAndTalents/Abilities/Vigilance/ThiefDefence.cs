using System;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	/// <summary>#866 - f_dmgFinal_dodge's abilities before plain dodge: the thief's rebound
	/// and vigilance, and the magic reflex and block. Magic items aren't ported yet, so the
	/// four percentages come from the tags here (Erebor's tag.mi_*).</summary>
	public static class ThiefDefence {
		private static readonly TagKey vigilanceTK = TagKey.Acquire("_vigilance_");
		private static readonly TagKey meleeReflexTK = TagKey.Acquire("_mi_meleereflex_");
		private static readonly TagKey meleeBlockTK = TagKey.Acquire("_mi_meleeblock_");
		private static readonly TagKey rangedReflexTK = TagKey.Acquire("_mi_rangedreflex_");
		private static readonly TagKey rangedBlockTK = TagKey.Acquire("_mi_rangedblock_");
		private const int kryssModel = 0x1400, scimitarModel = 0x13b5, cutlassModel = 0x1440;

		private static PassiveAbilityDef reboundDef, defStanceDef;

		private static int Points(Character self, ref PassiveAbilityDef def, string defname) {
			if (def == null) {
				def = (PassiveAbilityDef) AbilityDef.GetByDefname(defname);
			}
			return self.GetAbility(def);
		}

		public static int GetTagPercent(Character self, TagKey key) {
			return Convert.ToInt32(self.GetTag(key) ?? 0);
		}

		/// <summary>Set by magic items once they exist.</summary>
		public static void SetMagicDefence(Character self, int meleeReflex, int meleeBlock, int rangedReflex, int rangedBlock) {
			self.SetTag(meleeReflexTK, meleeReflex);
			self.SetTag(meleeBlockTK, meleeBlock);
			self.SetTag(rangedReflexTK, rangedReflex);
			self.SetTag(rangedBlockTK, rangedBlock);
		}

		internal static bool IsVigilant(Character self) {
			return self.HasTag(vigilanceTK);
		}

		/// <summary>e_vigilance's @AfterSwing: the stance costs a thief with the ability 30% vs monsters.</summary>
		internal static bool WeakensOwnBlow(Character attacker, Character defender) {
			return IsVigilant(attacker) && !defender.IsPlayerForCombat && (Points(attacker, ref defStanceDef, "a_thief_defstance") > 0);
		}

		/// <summary>The vigilance function: half of max stamina, on and off alike.</summary>
		[SteamFunction]
		public static void Vigilance(Character self) {
			ThiefDefenceLoc loc = Loc<ThiefDefenceLoc>.Get(self.Language);
			if (!Professions.HasProfession(self, Professions.Thief)) {
				self.SysMessage(loc.NotForYou);
				return;
			}
			if (self.Stam < 50) {
				self.SysMessage(loc.CantNow);
				return;
			}
			self.Stam = (short) Math.Max(0, self.Stam - (self.MaxStam / 2));
			if (IsVigilant(self)) {
				self.RemoveTag(vigilanceTK);
				self.SysMessage(loc.StanceOff);
			} else {
				self.SetTag(vigilanceTK, true);
				self.SysMessage(loc.StanceOn);
			}
		}

		[SteamFunction]
		public static void Stance(Character self) {
			if (WarriorAbilities.IsWarrior(self)) {
				WarriorAbilities.DefStance(self); //#1017 - class_info gives warriors .stance too
				return;
			}
			Vigilance(self);
		}

		/// <summary>f_dmgFinal_thief_rebound: chance out of 10000.</summary>
		public static int CalculateReboundChance(int weaponChance, int reboundPoints, bool vigilant, int defStancePoints) {
			return (weaponChance + (reboundPoints * 5)) * (vigilant ? (10 + defStancePoints) : 10);
		}

		private static int GetWeaponReboundChance(Character self) {
			Weapon weapon = self.FindLayer(LayerNames.Hand1) as Weapon;
			if (weapon == null) {
				return 0;
			}
			int model = weapon.Model;
			if ((model == kryssModel) || (model == kryssModel + 1)) {
				return 100;
			}
			if ((model == scimitarModel) || (model == scimitarModel + 1)) {
				return 50;
			}
			if ((model == cutlassModel) || (model == cutlassModel + 1)) {
				return 150;
			}
			return 0;
		}

		/// <summary>Whether the defender sends this monster's blow back.</summary>
		internal static bool RollRebound(Character attacker, Character defender) {
			if (attacker.IsPlayerForCombat) {
				return false;
			}
			int points = Points(defender, ref reboundDef, "a_thief_rebound");
			int weaponChance = GetWeaponReboundChance(defender);
			if ((points <= 0) || (weaponChance <= 0)) {
				return false;
			}
			int chance = CalculateReboundChance(weaponChance, points, IsVigilant(defender), Points(defender, ref defStanceDef, "a_thief_defstance"));
			return Globals.dice.Next(1, 10001) <= chance;
		}

		/// <summary>Returns whether the blow was avoided, and how much of it goes back to the attacker.</summary>
		internal static bool TryAvoid(Character attacker, Character defender, DamageType damageType, double damage, out double reflected) {
			reflected = 0;
			if ((damageType & DamageType.Physical) != DamageType.Physical) {
				return false;
			}
			double afterResists = CombatCalculator.RoundLikeSphere(damage * DamageManager.GetResistModifier(defender, damageType));
			ThiefDefenceLoc loc = Loc<ThiefDefenceLoc>.Get(defender.Language);
			if (RollRebound(attacker, defender)) {
				defender.SysMessage(loc.Rebounded);
				SoundCalculator.PlayMissSound(attacker);
				reflected = afterResists;
				return true;
			}
			bool ranged = attacker.WeaponProjectileType != ProjectileType.None;
			if (GetTagPercent(defender, ranged ? rangedReflexTK : meleeReflexTK) > Globals.dice.Next(100)) {
				defender.SysMessage(loc.MagicallyReflected);
				reflected = afterResists;
				return true;
			}
			if (GetTagPercent(defender, ranged ? rangedBlockTK : meleeBlockTK) > Globals.dice.Next(100)) {
				defender.SysMessage(loc.MagicallyBlocked);
				return true;
			}
			return false;
		}
	}

	public class ThiefDefenceLoc : CompiledLocStringCollection<ThiefDefenceLoc> {
		internal string NotForYou = "Tohle neni pro tebe";
		internal string CantNow = "Tohle ted nedokazes...";
		internal string StanceOn = "Zaujal jsi obranny postoj.";
		internal string StanceOff = "Zrusil jsi obranny postoj.";
		internal string Rebounded = "Odrazil jsi uder!";
		internal string MagicallyReflected = "Uder magicky odrazen";
		internal string MagicallyBlocked = "Uder magicky zastaven";
	}
}
