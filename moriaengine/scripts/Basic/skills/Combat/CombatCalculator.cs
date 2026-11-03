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

		private const int bodyPartCount = 8;
		private const int partHead = 0, partNeck = 1, partBack = 2, partChest = 3, partArms = 4, partHands = 5, partLegs = 6, partFeet = 7;

		/// <summary>#860 - coverage of the body parts in percent, Sphere's race body part table.</summary>
		private static readonly double[] bodyPartCoverage = { 14, 6, 18, 18, 14, 7, 17, 6 };

		/// <summary>#860 - tag.armordef: every worn piece adds its share, without the best-piece rule.</summary>
		private static double SumOverBodyPartsOfMindDefense(Character self) {
			double[] parts = new double[bodyPartCount];
			foreach (Item equipped in self.VisibleEquip) {
				Wearable wearable = equipped as Wearable;
				if ((wearable != null) && ((LayerNames) wearable.Z != LayerNames.Hand2)) {
					foreach (int part in GetBodyPartsOfLayer((LayerNames) wearable.Z)) {
						parts[part] = Math.Max(parts[part], wearable.MindDefenseVsP);
					}
				}
			}
			return SumOverBodyParts(parts);
		}

		private static double SumOverBodyParts(double[] values) {
			double sum = 0;
			for (int i = 0; i < bodyPartCount; i++) {
				sum += values[i] * bodyPartCoverage[i];
			}
			return sum / 100;
		}

		/// <summary>#860 - the switch in Sphere's CalcArmorDefense.</summary>
		private static int[] GetBodyPartsOfLayer(LayerNames layer) {
			switch (layer) {
				case LayerNames.Helmet:
					return new[] { partHead };
				case LayerNames.Collar:
					return new[] { partNeck };
				case LayerNames.Cape:
				case LayerNames.Robe:
				case LayerNames.Chest:
				case LayerNames.HalfApron:
					return new[] { partChest, partBack };
				case LayerNames.Shirt:
				case LayerNames.Tunic:
					return new[] { partChest, partBack, partArms };
				case LayerNames.Arms:
					return new[] { partArms };
				case LayerNames.Gloves:
					return new[] { partHands };
				case LayerNames.Pants:
				case LayerNames.Skirt:
				case LayerNames.Shoes:
				case LayerNames.Leggins:
					return new[] { partLegs, partFeet };
			}
			return new int[0];
		}

		internal static CombatArmorValues CalculateCombatArmorValues(Character self) {
			int armorClassVsP, mindDefenseVsP, armorClassVsM, mindDefenseVsM;
			int resist = SkillDef.SkillValueOfChar(self, SkillName.MagicResist);
			double resistEffect = SkillDef.GetBySkillName(SkillName.MagicResist).GetEffectForChar(self);

			// #860 - f_getminddef and f_minddefrefresh: a monster's is plain Magic Resistance / 10
			if (self.IsPlayerForCombat) {
				MagerySettings magery = MagerySettings.instance;
				double fromResist = RoundLikeSphere((resist * resistEffect) / 1000);
				mindDefenseVsP = (int) RoundLikeSphere((fromResist + (SumOverBodyPartsOfMindDefense(self) * magery.armorMindDefense)) * magery.mindDefenseGlobal);
				mindDefenseVsM = mindDefenseVsP;
			} else {
				mindDefenseVsP = resist / 10;
				mindDefenseVsM = mindDefenseVsP;
			}

			armorClassVsP = self.DefForCombat.ArmorVsP;
			armorClassVsM = self.DefForCombat.ArmorVsM;

			// #860 - Sphere's CalcArmorDefense: the best piece on each body part by coverage, plus
			// the shield by Parrying, whoever wears them. On Erebor that's on top of the def's
			// armor (armorsum), for monsters too.
			double[] armorVsP = new double[bodyPartCount], armorVsM = new double[bodyPartCount];
			double[] materials = new double[bodyPartCount];
			double shieldVsP = 0, shieldVsM = 0, materialTotal = 0;

			foreach (Item equipped in self.VisibleEquip) {
				Wearable wearable = equipped as Wearable;
				if (wearable == null) {
					continue;
				}
				LayerNames layer = (LayerNames) wearable.Z;
				ColoredArmor colored = wearable as ColoredArmor;
				int material = (colored != null) ? (int) colored.TypeDef.Material : 0;
				if (layer == LayerNames.Hand2) {
					int parrying = SkillDef.SkillValueOfChar(self, SkillName.Parry);
					shieldVsP = (wearable.ArmorVsP * parrying) / 1000.0;
					shieldVsM = (wearable.ArmorVsM * parrying) / 1000.0;
					materialTotal = material;
					continue;
				}
				foreach (int part in GetBodyPartsOfLayer(layer)) {
					armorVsP[part] = Math.Max(armorVsP[part], wearable.ArmorVsP);
					armorVsM[part] = Math.Max(armorVsM[part], wearable.ArmorVsM);
					materials[part] = Math.Max(materials[part], material);
				}
			}

			armorClassVsP += (int) (shieldVsP + SumOverBodyParts(armorVsP));
			armorClassVsM += (int) (shieldVsM + SumOverBodyParts(armorVsM));
			materialTotal += SumOverBodyParts(materials);

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

		/// <summary>#860 - a player's weapflag in f_getatack_nonmystik, by Sphere weapon type:
		/// t_weapon_sword (our Sword and Axe) slashes, t_weapon_fence (our Spike) is sharp.</summary>
		public static DamageType GetWeaponDamageType(WeaponType weapType) {
			switch (weapType) {
				case WeaponType.Bow:
				case WeaponType.XBow:
					return DamageType.PhysicalStabbing;
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					return DamageType.PhysicalSlashing;
				case WeaponType.BareHands:
				case WeaponType.OneHandBlunt:
				case WeaponType.TwoHandBlunt:
					return DamageType.PhysicalBlunt;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return DamageType.PhysicalSharp;
			}
			throw new SEException("weapType out of range");
		}

		private const int warForkModel = 0x1404, scimitarModel = 0x13b5, axeExecModel = 0xf45;

		/// <summary>#860 - the three fencing weapons f_getatack_nonmystik and creaturestart name
		/// by display id, either facing. A monster's sword and fence hits also count as sharp.</summary>
		private static DamageType GetDamageTypeOfWeapon(Weapon weapon, WeaponType weaponType, bool isPlayer) {
			int model = weapon.Model;
			if ((model == warForkModel) || (model == warForkModel + 1)) {
				return DamageType.PhysicalStabbing;
			}
			if (isPlayer) {
				if ((model == scimitarModel) || (model == scimitarModel + 1) || (model == axeExecModel) || (model == axeExecModel + 1)) {
					return DamageType.PhysicalSlashing;
				}
				return GetWeaponDamageType(weaponType);
			}
			switch (weaponType) {
				case WeaponType.OneHandSword:
				case WeaponType.TwoHandSword:
				case WeaponType.OneHandAxe:
				case WeaponType.TwoHandAxe:
					return DamageType.PhysicalSharp | DamageType.Slashing;
				case WeaponType.OneHandSpike:
				case WeaponType.TwoHandSpike:
					return DamageType.PhysicalSharp | DamageType.Stabbing;
			}
			return GetWeaponDamageType(weaponType);
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

		private static Projectile TryFindProjectile(Character self, Weapon weapon) {
			ProjectileType type = weapon.ProjectileType;
			if (type != ProjectileType.None) {
				Projectile retVal = self.weaponProjectile;
				if ((retVal != null) &&
						(retVal.IsDeleted ||
						(retVal.TopObj() != self) ||
						(retVal.Amount < 1))) {
					retVal = null;
				}
				if ((retVal == null) || (!CheckProjectile(retVal, weapon))) {
					foreach (Item i in self.Backpack.EnumShallow()) {
						retVal = i as Projectile;
						if ((retVal != null) && (CheckProjectile(retVal, weapon))) {
							return retVal;
						}
					}
				} else {
					return retVal;
				}
			}
			return null;
		}

		/// <summary>The projectile kind fits, and the weapon's own ammunition type if it has one (#1070, TDATA3): a light
		/// crossbow shoots jagged bolts only, a heavy one plain bolts.</summary>
		internal static bool CheckProjectile(Projectile projectile, Weapon weapon) {
			ProjectileType type = weapon.ProjectileType;
			ProjectileType actualType = projectile.ProjectileType;
			if ((type & actualType) != type) {
				return false;
			}
			string ammoType = ((WeaponDef) weapon.Def).AmmoType;
			return string.IsNullOrEmpty(ammoType) || ((projectile.Type != null) && string.Equals(projectile.Type.Defname, ammoType, StringComparison.OrdinalIgnoreCase));
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
				double ammoMultiplier = 1;
				if (weapon != null) {
					retVal.weaponType = weapon.WeaponType;
					retVal.weaponAnimType = weapon.WeaponAnimType;
					retVal.rangeVsM = weapon.RangeVsM;
					retVal.rangeVsP = weapon.RangeVsP;
					retVal.strikeStartRange = weapon.StrikeStartRange;
					retVal.strikeStopRange = weapon.StrikeStopRange;
					retVal.piercing = weapon.Piercing;
					retVal.projectileType = weapon.ProjectileType;
					self.weaponProjectile = TryFindProjectile(self, weapon);
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

					// #454 follow-up - Ranger's bow/crossbow range is a GM-tunable settings
					// lookup (CombatSettings.Ranges) rather than per-weapon-content RangeVsP/
					// VsM, which is still the converter's placeholder 1 for every bow/crossbow
					// today (confirmed in #454's own research) - same "wire the mechanism, let
					// the GM tune via .settings instead of hand-editing placeholder content"
					// pattern Mystic used. Applies to ANY player wielding one of these, not
					// profession-gated - unlike Mystic's mind-power attack, a bow/crossbow's
					// range is a property of the weapon itself, not the wielder's profession.
					// Light and heavy crossbow deliberately share WeaponType.XBow (confirmed
					// correct, no new enum value needed - see #454) - distinguished here by
					// defname, since this engine's content consistently names every heavy
					// crossbow item "i_crossbow_heavy_<material>" across all 7 material tiers
					// and there's no dedicated field for the distinction.
					if (retVal.weaponType == WeaponType.Bow) {
						SetRangedWeaponRange(retVal, RangesSettings.instance.bow);
						retVal.rangeVsP = RangesSettings.instance.bowVsPlayers;
					} else if (retVal.weaponType == WeaponType.XBow) {
						bool isHeavy = weapon.TypeDef.Defname.IndexOf("heavy", StringComparison.OrdinalIgnoreCase) >= 0;
						SetRangedWeaponRange(retVal, isHeavy ? RangesSettings.instance.heavyCrossbow : RangesSettings.instance.crossbow);
					}

					// #860 - nastaveni_arrowdamage_<tdata1> of the ammo
					if ((retVal.weaponType == WeaponType.Bow || retVal.weaponType == WeaponType.XBow) &&
							(self.weaponProjectile != null)) {
						ammoMultiplier = self.weaponProjectile.DamageMultiplier;
					}
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
				if (Professions.HasProfession(self, Professions.Mystic)) {
					// #450 - confirmed from the Erebor source: a Mystic's swing speed is driven
					// by current mana, not Dex - "the longer you fight, the slower you get" as
					// mana drains (DamageManager.ProcessSwing drains it after each swing). Mana
					// can legitimately hit 0 in sustained combat (no hard mana gate on this
					// attack per #447's research) - Math.Max guards the same division-by-zero
					// the existing Dex-based formula below would also hit at Dex=0, just far
					// more likely to actually occur here.
					retVal.delay = CalculateSwingDelay(SqrtInt(Math.Max(self.Mana / 2, 1)), MagerySettings.instance.mysticWeaponSpeedModifier);
				} else {
					retVal.delay = CalculateSwingDelay(SqrtInt(self.Dex) - 2, weapSpeed);
				}

				double sum = GetAttackStatMultiplier(self);
				double runBow = (retVal.weaponType == WeaponType.Bow) ? RangerSettings.instance.runBowDamage : 1;
				retVal.attackVsP = RoundLikeSphere(RoundLikeSphere(RoundLikeSphere(weapAttackVsP * sum) * runBow) * ammoMultiplier);
				retVal.attackVsM = RoundLikeSphere(RoundLikeSphere(RoundLikeSphere(weapAttackVsM * sum) * runBow) * ammoMultiplier);

				sum = GetMindPowerStatMultiplier(self);
				retVal.mindPowerVsM = RoundLikeSphere(weapMindPowerVsM * sum);
				retVal.mindPowerVsP = RoundLikeSphere(weapMindPowerVsP * sum);
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
					retVal.piercing = npcDef.WeaponPiercing;
					self.weaponProjectile = null;

					// #600/#716 - npcDef.WeaponRange's own unconfigured default (1, NPC.ct) left
					// every NPC wielding a real bow/crossbow stuck at melee range - this branch
					// never consulted the equipped weapon's own WeaponType at all, unlike the
					// player branch above. Reuses the SAME GM-tunable CombatSettings.Ranges
					// default players get for the identical WeaponType, rather than inventing a
					// separate NPC-only range mechanism - UNLESS a specific monster's own
					// WeaponRange is explicitly configured above the unconfigured sentinel (1),
					// in which case that per-monster override wins outright (#716 - lets content
					// give one specific archer/crossbowman a bigger - or smaller - range than
					// every other bow/crossbow user gets by default).
					//
					// Always routes through SetRangedWeaponRange (never a raw field assignment)
					// regardless of which value wins - #716 found that an EARLIER version of this
					// override only applied SetRangedWeaponRange for the global-default case,
					// silently skipping it whenever WeaponRange was explicitly configured. That
					// left StrikeStartRange/StopRange at NPC.ct's own melee-shaped defaults
					// (5/10) while WeaponRange sat at the content-authored value - WeaponSkillDef.
					// On_Stroke's Drawing phase would then refuse to transition to Striking until
					// within 5 tiles, while NPCAiTimer.Fight (reading the now-larger
					// WeaponRangeVsM/VsP) considered itself already "close enough" and never
					// approached past the bigger configured range - a real deadlock (never
					// actually fires) for any override bigger than the unmatched StrikeStartRange,
					// not just a missed convenience.
					if ((weapon != null) &&
							((weapon.WeaponType == WeaponType.Bow) || (weapon.WeaponType == WeaponType.XBow))) {
						int configuredRange = npcDef.WeaponRange;
						if (weapon.WeaponType == WeaponType.Bow) {
							SetRangedWeaponRange(retVal, (configuredRange > 1) ? configuredRange : RangesSettings.instance.bow);
						} else {
							bool isHeavy = weapon.TypeDef.Defname.IndexOf("heavy", StringComparison.OrdinalIgnoreCase) >= 0;
							int globalDefault = isHeavy ? RangesSettings.instance.heavyCrossbow : RangesSettings.instance.crossbow;
							SetRangedWeaponRange(retVal, (configuredRange > 1) ? configuredRange : globalDefault);
						}
						retVal.projectileType = weapon.ProjectileType;
						self.weaponProjectile = TryFindProjectile(self, weapon);
						if (self.weaponProjectile != null) {
							retVal.projectileAnim = weapon.ProjectileAnim;
						}
					}

					// #860 - f_getspeed_npc: tag.npc_speed, which defaults to the max stamina
					NPC npc = self as NPC;
					int swingStamina = ((npc != null) && (npc.SwingStamina > 0)) ? npc.SwingStamina : self.MaxStam;
					retVal.delay = CalculateSwingDelay(SqrtInt(swingStamina / 2), CombatSettings.instance.weaponSpeedNPC * 1000);

					// #860 - combat_main_npc takes the def's attack as it is, no stats (undoes #711)
					retVal.attackVsM = npcDef.WeaponAttack;
					retVal.attackVsP = retVal.attackVsM;

					// #860 - f_mindpowrefresh: a monster's is plain Evaluate Intelligence
					retVal.mindPowerVsM = SkillDef.SkillValueOfChar(self, SkillName.EvalInt);
					retVal.mindPowerVsP = retVal.mindPowerVsM;
				} else {
					//else ?!
					Logger.WriteError("Can't calculate combat values for '" + self + "'. It says it's a NPC but has no NPCDef.");
				}
			}

			// #447 follow-up - a Mystic's real attack reach is their own mind power, not
			// whatever weapon they happen to have equipped (confirmed from the Erebor source:
			// mystikrangepvp/pvm is a profession-level constant, not a per-staff stat) - this
			// overrides the weapon-/bare-hands-derived range above outright, same spirit as the
			// damage-type override in DamageManager.ProcessSwing. strikeStartRange is raised to
			// match so the attacker doesn't needlessly walk closer than the real range before
			// WeaponSkillDef's Drawing phase will let it strike; strikeStopRange gets a small
			// buffer above that so a target's minor repositioning doesn't immediately abort the
			// whole engagement (see WeaponSkillDef.On_Stroke's own give-up check).
			if (self.IsPlayerForCombat && Professions.HasProfession(self, Professions.Mystic)) {
				SetRangedWeaponRange(retVal, RangesSettings.instance.mystic);
				retVal.rangeVsP = RangesSettings.instance.mysticVsPlayers;
			}

			int rangeModifier = self.WeaponRangeModifier;
			retVal.rangeVsM += rangeModifier;
			retVal.rangeVsP += rangeModifier;

			retVal.damageType = (weapon != null) ? GetDamageTypeOfWeapon(weapon, retVal.weaponType, self.IsPlayerForCombat)
				: GetWeaponDamageType(retVal.weaponType);

			// #607/#860 - the archive's tag.weapflag, a monster's own damage type. Creaturestart
			// looks at the weapon only for a monster without one, so it wins over a held weapon.
			if (!self.IsPlayerForCombat) {
				NPCDef naturalDamageDef = self.DefForCombat as NPCDef;
				if ((naturalDamageDef != null) && (naturalDamageDef.NaturalDamageType != DamageType.Irresistable)) {
					retVal.damageType = naturalDamageDef.NaturalDamageType;
				}
			}

			return retVal;
		}

		/// <summary>#711 - the Tactics/Anatomy/ArmsLore/Str attack-scaling formula, shared between
		/// the player and NPC branches of CalculateCombatWeaponValues (previously player-only;
		/// NPCs used their bare .def WeaponAttack value with zero stat scaling at all).</summary>
		private static double GetAttackStatMultiplier(Character self) {
			double tacticsAttack = SkillDef.GetBySkillName(SkillName.Tactics).GetEffectForChar(self);
			double anatomyAttack = SkillDef.GetBySkillName(SkillName.Anatomy).GetEffectForChar(self);
			double armsloreAttack = SkillDef.GetBySkillName(SkillName.ArmsLore).GetEffectForChar(self);
			double strAttack = self.Str * CombatSettings.instance.attackStrModifier;
			return (tacticsAttack + anatomyAttack + armsloreAttack + strAttack) / 1000;
		}

		/// <summary>#711 - the EvalInt/SpiritSpeak/Int mind-power-scaling formula, shared the
		/// same way as GetAttackStatMultiplier above.</summary>
		private static double GetMindPowerStatMultiplier(Character self) {
			double evalIntMP = SkillDef.GetBySkillName(SkillName.EvalInt).GetEffectForChar(self);
			double spiritSpeakMP = SkillDef.GetBySkillName(SkillName.SpiritSpeak).GetEffectForChar(self);
			double intMP = self.Int * MagerySettings.instance.mindPowerIntModifier;
			return (evalIntMP + spiritSpeakMP + intMP) / 1000;
		}

		/// <summary>Shared by the Mystic (#447) and Ranger (#454) range overrides above -
		/// raises strikeStartRange to match so the attacker doesn't needlessly close distance
		/// before WeaponSkillDef's Drawing phase will let it strike, and gives strikeStopRange
		/// a small buffer above that so a target's minor repositioning doesn't immediately abort
		/// the whole engagement.</summary>
		private static void SetRangedWeaponRange(CombatWeaponValues retVal, int range) {
			retVal.rangeVsM = range;
			retVal.rangeVsP = range;
			retVal.strikeStartRange = range;
			retVal.strikeStopRange = range + 2;
		}

		/// <summary>#868 - f_setmainability: the chance out of 100000 of a class ability, by statslevel.</summary>
		public static int CalculateClassAbility(int statsLevel) {
			int missing = 60 - statsLevel;
			return ((3600 - (missing * missing)) * 264) / 10;
		}

		/// <summary>#860 - Sphere's integer maths: (x*1000+500)/1000.</summary>
		internal static double RoundLikeSphere(double value) {
			return Math.Floor(value + 0.5);
		}

		/// <summary>#860 - Erebor's sqrt_int function (funkce.scp). It's Newton's method on
		/// value*10000, so it returns about 100*sqrt(value), not an integer square root.</summary>
		public static int SqrtInt(int value) {
			if (value <= 0) {
				return 0;
			}
			int target = value * 10000;
			int root = target;
			for (int i = 0; i < 30; i++) {
				int last = root;
				root = (root / 2) + ((target / root) / 2);
				if ((last == root) || (last == (root - 1))) {
					return last;
				}
			}
			return -1;
		}

		/// <summary>#860 - f_getspeed_*: hitspeed is in server ticks, and scripts set it on both
		/// halves of a swing (skill @Start and @HitTry), so a whole swing takes twice that.</summary>
		public static TimeSpan CalculateSwingDelay(int root, double weaponSpeed) {
			CombatSettings settings = CombatSettings.instance;
			return CalculateSwingDelay(root, weaponSpeed, settings.weaponSpeedGlobal, settings.swingTicksPerSecond);
		}

		public static TimeSpan CalculateSwingDelay(int root, double weaponSpeed, double globalSpeed, double ticksPerSecond) {
			int speed = (int) RoundLikeSphere(root * weaponSpeed * globalSpeed);
			if (speed <= 0) {
				speed = 50;
			}
			int hitspeed = Math.Max(0xfffff / speed, 1);
			return TimeSpan.FromSeconds((2 * hitspeed) / ticksPerSecond);
		}

		/// <summary>#860 - Erebor's damagecust: a player's swing, or a monster's on a monster.
		/// armor is after armorClassP/M, armorRoll is the Parrying effect 0.945-1.05. Armor above
		/// the attack gives 0 here; a negative result would read as a heal in CauseDamage.</summary>
		public static double CalculateDamageCust(double attack, double armor, double piercing, double armorRoll, double damageMod) {
			double obrana = RoundLikeSphere(Math.Max(0, armor) * armorRoll * (1000 - piercing));
			double koef = Math.Truncate(((attack * 1000) - obrana + 500) / 1000);
			return Math.Max(0, RoundLikeSphere(koef * damageMod));
		}

		/// <summary>#860 - Erebor's damagecust_MvP: armor cuts a percentage of a monster's attack,
		/// armorFactor is nastaveni_global_MvP_armorfactor (200: armor 100 halves it).</summary>
		public static double CalculateDamageMvP(double attack, double armor, double piercing, double armorFactor, double randomFactor, double damageMod) {
			double obrana = Math.Max(0, armor) * (1000 - piercing);
			double utok = RoundLikeSphere(attack * damageMod);
			double ubrano = RoundLikeSphere(utok * (1000 - Math.Truncate(obrana / armorFactor)) / 1000);
			return Math.Max(0, RoundLikeSphere(ubrano * randomFactor));
		}

		/// <summary>#860 - what's between a spell's power and the target: mind defense * 10, against
		/// players' spells on a player changed by the profession (armorchangeom).</summary>
		public static int GetSpellDefense(Character caster, Character target) {
			int mindDefense = caster.IsPlayerForCombat ? target.MindDefenseVsP : target.MindDefenseVsM;
			int change = 0;
			if (caster.IsPlayerForCombat && target.IsPlayerForCombat) {
				Player player = target as Player;
				ProfessionDef profession = (player != null) ? player.Profession : null;
				change = (profession != null) ? profession.PvPMindDefenseChange : 0;
			}
			return (mindDefense * 10) - change;
		}

		/// <summary>#860 - a spell's damagecust, armor and rolls as for a swing. It's the caster's spell,
		/// so a monster's on a player still uses this formula, not damagecust_MvP.</summary>
		public static double CalculateSpellArmorEffect(Character caster, Character target, double attack, double piercing) {
			CombatSettings settings = CombatSettings.instance;
			bool casterIsPlayer = caster.IsPlayerForCombat;
			double armor, damageMod;
			if (target.IsPlayerForCombat) {
				armor = (casterIsPlayer ? Math.Max(0, target.ArmorClassVsP + DamageManager.GetPvPArmorChange(target)) : target.ArmorClassVsM) * settings.armorClassP;
				damageMod = casterIsPlayer ? settings.swingDamagePvP : settings.swingDamageM;
			} else {
				armor = (casterIsPlayer ? target.ArmorClassVsP : target.ArmorClassVsM) * settings.armorClassM;
				damageMod = casterIsPlayer ? settings.swingDamageVsM : (settings.swingDamageVsM * settings.swingDamageM);
			}
			return CalculateDamageCust(attack, armor, piercing,
				ScriptUtil.GetRandInRange(settings.armorRandEffectMin, settings.armorRandEffectMax), damageMod);
		}

		/// <summary>#860 - Sphere's Calc_CombatChanceToHit: the upper bound of the difficulty roll.</summary>
		public static int CalculateHitDifficultyLimit(int skill, int tactics, int targetTactics, int targetStamina, bool targetShootsBack) {
			int attack = (skill + tactics) / 2;
			int defend = (targetTactics + (targetStamina * (targetShootsBack ? 9 : 10))) / 2;
			int difference = (skill - ((attack - defend) / 5)) / 10;
			return Math.Max(0, Math.Min(100, difference));
		}

		public static double CalculateArmorClassEffect(Character attacker, Character defender, double attack, double piercing, double armorClass) {
			bool defenderIsPlayer = defender.IsPlayerForCombat;
			bool attackerIsPlayer = attacker.IsPlayerForCombat;
			CombatSettings settings = CombatSettings.instance;

			if (defenderIsPlayer && !attackerIsPlayer) {
				return CalculateDamageMvP(attack, armorClass, piercing, settings.armorClassMvP,
					ScriptUtil.GetRandInRange(settings.swingDamageRandMvPMin, settings.swingDamageRandMvPMax), settings.swingDamageM);
			}
			double damageMod;
			if (defenderIsPlayer) {
				damageMod = settings.swingDamagePvP;
			} else {
				damageMod = settings.swingDamageVsM;
				if (!attackerIsPlayer) {
					damageMod *= settings.swingDamageM;
				}
			}
			return CalculateDamageCust(attack, armorClass, piercing,
				ScriptUtil.GetRandInRange(settings.armorRandEffectMin, settings.armorRandEffectMax), damageMod);
		}
	}
}

