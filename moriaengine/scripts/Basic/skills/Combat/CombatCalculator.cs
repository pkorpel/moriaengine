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

	public static class CombatCalculator {

		internal static CombatArmorValues CalculateCombatArmorValues(Character self) {
			int armorClassVsP, mindDefenseVsP, armorClassVsM, mindDefenseVsM;
			int resist = SkillDef.SkillValueOfChar(self, SkillName.MagicResist);
			double resistEffect = SkillDef.GetBySkillName(SkillName.MagicResist).GetEffectForChar(self);
			mindDefenseVsP = (int) (
				(resist * resistEffect) / 1000);
			mindDefenseVsM = mindDefenseVsP;

			armorClassVsP = self.DefForCombat.ArmorVsP;
			armorClassVsM = self.DefForCombat.ArmorVsM;

			double materialTotal = 0;

			//we calculate worn armor only for players
			if (self.IsPlayerForCombat) {
				double armorVsPHead = 0;
				double armorVsPNeck = 0;
				double armorVsPBack = 0;
				double armorVsPChest = 0;
				double armorVsPArms = 0;
				double armorVsPHands = 0;
				double armorVsPLegs = 0;
				double armorVsPFeet = 0;

				double armorVsMHead = 0;
				double armorVsMNeck = 0;
				double armorVsMBack = 0;
				double armorVsMChest = 0;
				double armorVsMArms = 0;
				double armorVsMHands = 0;
				double armorVsMLegs = 0;
				double armorVsMFeet = 0;

				double mindDefVsPHead = 0;
				double mindDefVsPNeck = 0;
				double mindDefVsPBack = 0;
				double mindDefVsPChest = 0;
				double mindDefVsPArms = 0;
				double mindDefVsPHands = 0;
				double mindDefVsPLegs = 0;
				double mindDefVsPFeet = 0;

				double mindDefVsMHead = 0;
				double mindDefVsMNeck = 0;
				double mindDefVsMBack = 0;
				double mindDefVsMChest = 0;
				double mindDefVsMArms = 0;
				double mindDefVsMHands = 0;
				double mindDefVsMLegs = 0;
				double mindDefVsMFeet = 0;

				double materialHead = 0;
				double materialNeck = 0;
				double materialBack = 0;
				double materialChest = 0;
				double materialArms = 0;
				double materialHands = 0;
				double materialLegs = 0;
				double materialFeet = 0;

				double armorVsPTotal = 0;
				double armorVsMTotal = 0;

				foreach (Item equipped in self.VisibleEquip) {
					Wearable wearable = equipped as Wearable;
					if (wearable != null) {
						LayerNames layer = (LayerNames) wearable.Z;
						int armorVsP = wearable.ArmorVsP;
						int mindDefVsP = wearable.MindDefenseVsP;
						int armorVsM = wearable.ArmorVsM;
						int mindDefVsM = wearable.MindDefenseVsM;
						
						int material = 0;
						ColoredArmor colored = wearable as ColoredArmor;
						if (colored != null) {
							material = (int) colored.TypeDef.Material;
						}

						if ((armorVsP > 0) || (mindDefVsP > 0) || (armorVsM > 0) || (mindDefVsM > 0)) {
							switch (layer) {
								case LayerNames.Helmet:
									armorVsPHead = Math.Max(armorVsPHead, armorVsP);
									mindDefVsPHead = Math.Max(mindDefVsPHead, mindDefVsP);
									armorVsMHead = Math.Max(armorVsMHead, armorVsM);
									mindDefVsMHead = Math.Max(mindDefVsMHead, mindDefVsM);
									materialHead = Math.Max(materialHead, material);
									break;
								case LayerNames.Collar:
									armorVsPNeck = Math.Max(armorVsPNeck, armorVsP);
									mindDefVsPNeck = Math.Max(mindDefVsPNeck, mindDefVsP);
									armorVsMNeck = Math.Max(armorVsMNeck, armorVsM);
									mindDefVsMNeck = Math.Max(mindDefVsMNeck, mindDefVsM);
									materialHead = Math.Max(materialHead, material);
									break;
								case LayerNames.Shirt:
								case LayerNames.Chest:       // 13 = armor chest
								case LayerNames.Tunic:       // 17 = jester suit
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPChest = Math.Max(armorVsPChest, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPChest = Math.Max(mindDefVsPChest, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMChest = Math.Max(armorVsMChest, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMChest = Math.Max(mindDefVsMChest, mindDefVsM);
									materialBack = Math.Max(materialBack, material);
									materialChest = Math.Max(materialChest, material);
									break;
								case LayerNames.Arms:                // 19 = armor
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM);
									materialArms = Math.Max(materialArms, material);
									break;
								case LayerNames.Pants:
								case LayerNames.Skirt:
								case LayerNames.HalfApron:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsM);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM);
									materialLegs = Math.Max(materialLegs, material);
									break;
								case LayerNames.Shoes:
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM);
									materialFeet = Math.Max(materialFeet, material);
									break;
								case LayerNames.Gloves:      // 7
									armorVsPHands = Math.Max(armorVsPHands, armorVsP);
									mindDefVsPHands = Math.Max(mindDefVsPHands, mindDefVsP);
									armorVsMHands = Math.Max(armorVsMHands, armorVsM);
									mindDefVsMHands = Math.Max(mindDefVsMHands, mindDefVsM);
									materialHands = Math.Max(materialHands, material);
									break;
								case LayerNames.Cape:                // 20 = cape
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM);
									materialBack = Math.Max(materialBack, material);
									materialArms = Math.Max(materialArms, material);
									break;
								case LayerNames.Robe:                // 22 = robe over all.
									armorVsPBack = Math.Max(armorVsPBack, armorVsP);
									armorVsPChest = Math.Max(armorVsPChest, armorVsP);
									armorVsPArms = Math.Max(armorVsPArms, armorVsP);
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP); ;
									mindDefVsPBack = Math.Max(mindDefVsPBack, mindDefVsP);
									mindDefVsPChest = Math.Max(mindDefVsPChest, mindDefVsP);
									mindDefVsPArms = Math.Max(mindDefVsPArms, mindDefVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsP);
									armorVsMBack = Math.Max(armorVsMBack, armorVsM);
									armorVsMChest = Math.Max(armorVsMChest, armorVsM);
									armorVsMArms = Math.Max(armorVsMArms, armorVsM);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM); ;
									mindDefVsMBack = Math.Max(mindDefVsMBack, mindDefVsM);
									mindDefVsMChest = Math.Max(mindDefVsMChest, mindDefVsM);
									mindDefVsMArms = Math.Max(mindDefVsMArms, mindDefVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM);
									materialBack = Math.Max(materialBack, material);
									materialChest = Math.Max(materialChest, material);
									materialArms = Math.Max(materialArms, material);
									materialLegs = Math.Max(materialLegs, material);
									break;
								case LayerNames.Leggins:
									armorVsPLegs = Math.Max(armorVsPLegs, armorVsP);
									armorVsPFeet = Math.Max(armorVsPFeet, armorVsP);
									mindDefVsPLegs = Math.Max(mindDefVsPLegs, mindDefVsP);
									mindDefVsPFeet = Math.Max(mindDefVsPFeet, mindDefVsP);
									armorVsMLegs = Math.Max(armorVsMLegs, armorVsM);
									armorVsMFeet = Math.Max(armorVsMFeet, armorVsM);
									mindDefVsMLegs = Math.Max(mindDefVsMLegs, mindDefVsM);
									mindDefVsMFeet = Math.Max(mindDefVsMFeet, mindDefVsM);
									materialLegs = Math.Max(materialLegs, material);
									materialFeet = Math.Max(materialFeet, material);
									break;
								case LayerNames.Hand2: //shield
									int parrying = SkillDef.SkillValueOfChar(self, SkillName.Parry);
									armorVsPTotal = (armorVsP * parrying) / 1000;
									armorVsMTotal = (armorVsP * parrying) / 1000;
									//no mindDef with shield
									materialTotal = material;
									break;
							}
						}
					}
				}

				armorVsPTotal +=
					(armorVsPHead * 0.1) +
					(armorVsPNeck * 0.05) +
					(armorVsPBack * 0.1) +
					(armorVsPChest * 0.3) +
					(armorVsPArms * 0.1) +
					(armorVsPHands * 0.1) +
					(armorVsPLegs * 0.2) +
					(armorVsPFeet * 0.05);
				armorClassVsP += (int) armorVsPTotal;

				mindDefenseVsP += (int) (
					(mindDefVsPHead * 0.1) +
					(mindDefVsPNeck * 0.05) +
					(mindDefVsPBack * 0.1) +
					(mindDefVsPChest * 0.3) +
					(mindDefVsPArms * 0.1) +
					(mindDefVsPHands * 0.1) +
					(mindDefVsPLegs * 0.2) +
					(mindDefVsPFeet * 0.05));

				armorVsMTotal +=
					(armorVsMHead * 0.1) +
					(armorVsMNeck * 0.05) +
					(armorVsMBack * 0.1) +
					(armorVsMChest * 0.3) +
					(armorVsMArms * 0.1) +
					(armorVsMHands * 0.1) +
					(armorVsMLegs * 0.2) +
					(armorVsMFeet * 0.05);
				armorClassVsM += (int) armorVsMTotal;

				mindDefenseVsM += (int) (
					(mindDefVsMHead * 0.1) +
					(mindDefVsMNeck * 0.05) +
					(mindDefVsMBack * 0.1) +
					(mindDefVsMChest * 0.3) +
					(mindDefVsMArms * 0.1) +
					(mindDefVsMHands * 0.1) +
					(mindDefVsMLegs * 0.2) +
					(mindDefVsMFeet * 0.05));

				materialTotal +=
					(materialHead * 0.1) +
					(materialNeck * 0.05) +
					(materialBack * 0.1) +
					(materialChest * 0.3) +
					(materialArms * 0.1) +
					(materialHands * 0.1) +
					(materialLegs * 0.2) +
					(materialFeet * 0.05);
			}
			CombatArmorValues retVal = new CombatArmorValues();
			retVal.armorVsP = armorClassVsP;
			retVal.armorVsM = armorClassVsM;
			retVal.mindDefenseVsP = mindDefenseVsP;
			retVal.mindDefenseVsM = mindDefenseVsM;
			retVal.material = materialTotal;

			int acModifier = self.ArmorClassModifier;
			int mdModifier = self.MindDefenseModifier;

			retVal.armorVsP += acModifier;
			retVal.armorVsM += acModifier;
			retVal.mindDefenseVsP += mdModifier;
			retVal.mindDefenseVsM += mdModifier;

			return retVal;
		}

		public static DamageType GetWeaponDamageType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.Bow:
				case WeaponType.XBow:
					return DamageType.PhysicalArchery;
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
					return DamageType.PhysicalSharp;
				case WeaponType.BareHands:
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return DamageType.PhysicalBlunt;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return DamageType.PhysicalStabbing;
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					return DamageType.PhysicalSlashing;
			}
			throw new SEException("weapType out of range");
		}

		internal class CombatArmorValues {
			internal int armorVsP;
			internal int mindDefenseVsP;
			internal int armorVsM;
			internal int mindDefenseVsM;
			internal double material;
		}

		internal class CombatWeaponValues {
			internal Weapon weapon;
			internal WeaponType weaponType;
			internal DamageType damageType;
			internal int rangeVsP;
			internal int rangeVsM;
			internal int strikeStartRange;
			internal int strikeStopRange;
			internal TimeSpan delay;
			internal double attackVsP;
			internal double attackVsM;
			internal double piercing;
			internal WeaponAnimType weaponAnimType;
			internal int projectileAnim = -1;
			internal ProjectileType projectileType;
			internal double mindPowerVsP;
			internal double mindPowerVsM;
		}

		private static Projectile TryFindProjectile(Character self, ProjectileType type) {
			if (type != ProjectileType.None) {
				Projectile retVal = self.weaponProjectile;
				if ((retVal != null) &&
						(retVal.IsDeleted ||
						(retVal.TopObj() != self) ||
						(retVal.Amount < 1))) {
					retVal = null;
				}
				if ((retVal == null) || (!CheckProjectile(retVal, type))) {
					foreach (Item i in self.Backpack.EnumShallow()) {
						retVal = i as Projectile;
						if ((retVal != null) && (CheckProjectile(retVal, type))) {
							return retVal;
						}
					}
				} else {
					return retVal;
				}
			}
			return null;
		}

		private static bool CheckProjectile(Projectile projectile, ProjectileType type) {
			ProjectileType actualType = projectile.ProjectileType;
			return ((type & actualType) == type);
		}

		internal static CombatWeaponValues CalculateCombatWeaponValues(Character self) {
			CombatWeaponValues retVal = new CombatWeaponValues();

			Weapon weapon = self.FindLayer(LayerNames.Hand1) as Weapon;
			if (weapon == null) {
				weapon = self.FindLayer(LayerNames.Hand2) as Weapon;
			}
			retVal.weapon = weapon;

			if (self.IsPlayerForCombat) {
				double weapSpeed, weapAttackVsP, weapAttackVsM, weapMindPowerVsP, weapMindPowerVsM;
				if (weapon != null) {
					retVal.weaponType = weapon.WeaponType;
					retVal.weaponAnimType = weapon.WeaponAnimType;
					retVal.rangeVsM = weapon.RangeVsM;
					retVal.rangeVsP = weapon.RangeVsP;
					retVal.strikeStartRange = weapon.StrikeStartRange;
					retVal.strikeStopRange = weapon.StrikeStopRange;
					retVal.piercing = weapon.Piercing;
					retVal.projectileType = weapon.ProjectileType;
					self.weaponProjectile = TryFindProjectile(self, weapon.ProjectileType);
					if (self.weaponProjectile != null) {
						retVal.piercing += self.weaponProjectile.Piercing;
						retVal.projectileAnim = weapon.ProjectileAnim;
						self.weaponProjectile.Trigger_CoupledWithWeapon(self, weapon);
					}

					weapSpeed = weapon.Speed;
					weapAttackVsP = weapon.AttackVsP;
					weapAttackVsM = weapon.AttackVsM;
					weapMindPowerVsP = weapon.MindPowerVsP;
					weapMindPowerVsM = weapon.MindPowerVsM;
				} else {
					retVal.weaponType = WeaponType.BareHands;
					retVal.weaponAnimType = WeaponAnimType.BareHands;
					retVal.rangeVsM = CombatSettings.instance.bareHandsRange;
					retVal.rangeVsP = retVal.rangeVsM;
					retVal.strikeStartRange = CombatSettings.instance.bareHandsStrikeStartRange;
					retVal.strikeStopRange = CombatSettings.instance.bareHandsStrikeStopRange;
					retVal.piercing = CombatSettings.instance.bareHandsPiercing;
					self.weaponProjectile = null;
					weapSpeed = CombatSettings.instance.bareHandsSpeed;
					weapAttackVsP = CombatSettings.instance.bareHandsAttackVsP;
					weapAttackVsM = CombatSettings.instance.bareHandsAttackVsM;
					weapMindPowerVsP = MagerySettings.instance.bareHandsMindPowerVsP;
					weapMindPowerVsM = MagerySettings.instance.bareHandsMindPowerVsP;
				}
				double delay = Math.Sqrt(self.Dex);
				delay *= weapSpeed;
				delay *= CombatSettings.instance.weaponSpeedGlobal;
				retVal.delay = TimeSpan.FromSeconds((0xfffff / 1000.0) / delay);//dedictvi z morie. funguje to tak proc to menit :)

				double tacticsAttack = SkillDef.GetBySkillName(SkillName.Tactics).GetEffectForChar(self);
				double anatomyAttack = SkillDef.GetBySkillName(SkillName.Anatomy).GetEffectForChar(self);
				double armsloreAttack = SkillDef.GetBySkillName(SkillName.ArmsLore).GetEffectForChar(self);
				double strAttack = self.Str * CombatSettings.instance.attackStrModifier;
				double sum = (tacticsAttack + anatomyAttack + armsloreAttack + strAttack) / 1000;
				retVal.attackVsP = weapAttackVsP * sum;
				retVal.attackVsM = weapAttackVsM * sum;

				double evalIntMP = SkillDef.GetBySkillName(SkillName.EvalInt).GetEffectForChar(self);
				double spiritSpeakMP = SkillDef.GetBySkillName(SkillName.SpiritSpeak).GetEffectForChar(self);
				double intMP = self.Int * MagerySettings.instance.mindPowerIntModifier;
				sum = (evalIntMP + spiritSpeakMP + intMP) / 1000;
				retVal.mindPowerVsM = weapMindPowerVsM * sum;
				retVal.mindPowerVsP = weapMindPowerVsP * sum;
			} else {
				NPCDef npcDef = self.DefForCombat as NPCDef;
				if (npcDef != null) {
					retVal.weaponType = npcDef.WeaponType;
					if (retVal.weaponType == WeaponType.Undefined) {
						if (weapon == null) {
							retVal.weaponType = WeaponType.BareHands;
						} else {
							retVal.weaponType = weapon.WeaponType;
						}
					}
					if (weapon != null) {
						retVal.weaponAnimType = weapon.WeaponAnimType;
						retVal.projectileAnim = weapon.ProjectileAnim;
					} else {
						retVal.weaponAnimType = WeaponAnimType.BareHands;
						retVal.projectileAnim = npcDef.ProjectileAnim;
					}
					retVal.rangeVsM = npcDef.WeaponRange;
					retVal.rangeVsP = retVal.rangeVsM;
					retVal.strikeStartRange = npcDef.StrikeStartRange;
					retVal.strikeStopRange = npcDef.StrikeStopRange;
					retVal.delay = TimeSpan.FromSeconds(npcDef.WeaponDelay);
					retVal.attackVsM = npcDef.WeaponAttack;
					retVal.attackVsP = retVal.attackVsM;
					retVal.mindPowerVsM = npcDef.MindPower;
					retVal.mindPowerVsP = retVal.mindPowerVsM;
					retVal.piercing = npcDef.WeaponPiercing;
					self.weaponProjectile = null;
				} else {
					//else ?!
					Logger.WriteError("Can't calculate combat values for '" + self + "'. It says it's a NPC but has no NPCDef.");
				}
			}

			int rangeModifier = self.WeaponRangeModifier;
			retVal.rangeVsM += rangeModifier;
			retVal.rangeVsP += rangeModifier;

			retVal.damageType = GetWeaponDamageType(retVal.weaponType);

			return retVal;
		}


		public static double CalculateArmorClassEffect(Character attacker, Character defender, double attack, double piercing, double armorClass) {
			bool defenderIsPlayer = defender.IsPlayerForCombat;
			bool attackerIsPlayer = attacker.IsPlayerForCombat;
			CombatSettings settings = CombatSettings.instance;

			double damageMod;

			bool isMvP = false;
			if (defenderIsPlayer) {
				if (attackerIsPlayer) {
					damageMod = settings.swingDamagePvP;
				} else {
					damageMod = ScriptUtil.GetRandInRange(settings.swingDamageRandMvPMin, settings.swingDamageRandMvPMax);
					isMvP = true;
				}
			} else {
				damageMod = settings.swingDamageM;
			}

			double armor = armorClass * (1000 - piercing) / 1000;
			if (!isMvP) {
				armor = ScriptUtil.GetRandInRange(settings.armorRandEffectMin, settings.armorRandEffectMax);
			}

			armor = Math.Max(0, armor);
			attack = Math.Max(0, attack);
			damageMod = Math.Max(0, damageMod);

			//!!TODO!! ruznej vzorec pro pvm a pvm
			//if (defenderIsPlayer && !attackerIsPlayer) { //damagecust_MvP 
			//	armorClass *= settings.armorClassMvP; 
			//}
			return (attack - armor) * damageMod;
		}
	}
}

