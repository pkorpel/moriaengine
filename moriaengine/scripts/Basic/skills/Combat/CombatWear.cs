using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#865 - what a fight costs weapons and armor: on=@hit, the staff's @spellcast and
	/// damage_armors in combat.scp. bodypart_layers isn't in the archive; its weights 14, 6, 36, 7,
	/// 7, 23 are read as head, neck, torso, arms, hands, legs.</summary>
	public static class CombatWear {
		private static readonly int[] partWeights = { 14, 6, 36, 7, 7, 23 };
		private static readonly LayerNames[][] partLayers = {
			new[] { LayerNames.Helmet },
			new[] { LayerNames.Collar },
			new[] { LayerNames.Chest, LayerNames.Shirt, LayerNames.Tunic, LayerNames.Robe, LayerNames.Cape },
			new[] { LayerNames.Arms },
			new[] { LayerNames.Gloves },
			new[] { LayerNames.Leggins, LayerNames.Pants, LayerNames.Skirt, LayerNames.Shoes }
		};

		internal static void OnSwing(Character attacker) {
			Weapon weapon = attacker.Weapon;
			if ((weapon != null) && (attacker is Player)) {
				weapon.TakeWear(CombatSettings.instance.weaponWearPerHit, attacker);
			}
		}

		internal static void OnHarmfulCast(Character caster) {
			ColoredStaff staff = caster.Weapon as ColoredStaff;
			if (staff != null) {
				staff.TakeWear(CombatSettings.instance.staffWearPerSpell, caster);
			}
		}

		private static int RollPart() {
			int roll = Globals.dice.Next(93);
			for (int i = 0; i < partWeights.Length; i++) {
				if (roll < partWeights[i]) {
					return i;
				}
				roll -= partWeights[i];
			}
			return partWeights.Length - 1;
		}

		/// <summary>armor_localdam: the piece's share of the armor on that part of the body.</summary>
		public static int CalculateArmorWear(int damage, int wearPerDamage, int armorSum, int armor) {
			if ((armorSum <= 0) || (armor <= 0)) {
				return 0;
			}
			int divisor = (armorSum * 1000) / armor;
			return (divisor > 0) ? ((damage * wearPerDamage) / divisor) : 0;
		}

		/// <summary>damage_armors: a blow or spell landing on one part of the body.</summary>
		internal static void OnDamageTaken(Character defender, int damage, Character source) {
			if (damage <= 0) {
				return;
			}
			List<Wearable> pieces = new List<Wearable>();
			int armorSum = 0;
			foreach (LayerNames layer in partLayers[RollPart()]) {
				Wearable piece = defender.FindLayer(layer) as Wearable;
				if (piece != null) {
					pieces.Add(piece);
					armorSum += piece.TypeDef.ArmorVsP;
				}
			}
			Wearable shield = defender.FindLayer(LayerNames.Hand2) as Wearable;
			if (shield != null) {
				armorSum += 2 * shield.TypeDef.ArmorVsP;
			}
			int perDamage = CombatSettings.instance.armorWearPerDamage;
			if (shield != null) {
				shield.TakeWear(CalculateArmorWear(damage, perDamage, armorSum, 2 * shield.TypeDef.ArmorVsP), source);
			}
			foreach (Wearable piece in pieces) {
				piece.TakeWear(CalculateArmorWear(damage, perDamage, armorSum, piece.TypeDef.ArmorVsP), source);
			}
		}
	}
}
