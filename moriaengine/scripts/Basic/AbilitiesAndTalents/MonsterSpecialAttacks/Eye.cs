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

	/// <summary>
	/// #799 e_eye (29 monsters - floating eyes / gazers). Archive, three parts:
	/// - its own landed hits sap the target's stamina by {1,5}, and while a player target has
	///   mana left it leeches twice the damage dealt as hit points (up to 3x its maxhits),
	///   costing the player 4 mana; a non-player target with over 200 mana just loses 35;
	/// - 15% of the physical blows aimed at it bounce off completely;
	/// - a clever attacker catches its eye: it switches to anyone hitting it whose int beats a
	///   {100,1250} roll.</summary>
	[ViewableClass]
	public class EyeAbilityDef : PassiveAbilityDef {
		private FieldValue stamDrain;
		private FieldValue leechMultiplier;
		private FieldValue hitsCapMultiplier;
		private FieldValue playerManaCost;
		private FieldValue deflectChance;
		private FieldValue attentionInt;

		public EyeAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.stamDrain = this.InitTypedField("eyeStamDrain", new double[] { 1, 5 }, typeof(double[]));
			this.leechMultiplier = this.InitTypedField("eyeLeechMultiplier", (double) 2, typeof(double));
			this.hitsCapMultiplier = this.InitTypedField("eyeHitsCapMultiplier", (double) 3, typeof(double));
			this.playerManaCost = this.InitTypedField("eyePlayerManaCost", 4, typeof(int));
			this.deflectChance = this.InitTypedField("eyeDeflectChance", (double) 150, typeof(double));
			this.attentionInt = this.InitTypedField("eyeAttentionInt", new double[] { 100, 1250 }, typeof(double[]));
		}

		/// <summary>Stamina taken from its target per landed hit.</summary>
		public double[] StamDrain {
			get { return (double[]) this.stamDrain.CurrentValue; }
			set { this.stamDrain.CurrentValue = value; }
		}

		/// <summary>Hit points leeched per point of damage dealt.</summary>
		public double LeechMultiplier {
			get { return (double) this.leechMultiplier.CurrentValue; }
			set { this.leechMultiplier.CurrentValue = value; }
		}

		/// <summary>Leeching can push its hits up to this many times its maxhits.</summary>
		public double HitsCapMultiplier {
			get { return (double) this.hitsCapMultiplier.CurrentValue; }
			set { this.hitsCapMultiplier.CurrentValue = value; }
		}

		/// <summary>Mana a player target loses each time it leeches.</summary>
		public int PlayerManaCost {
			get { return (int) this.playerManaCost.CurrentValue; }
			set { this.playerManaCost.CurrentValue = value; }
		}

		/// <summary>Permille chance a physical blow bounces off.</summary>
		public double DeflectChance {
			get { return (double) this.deflectChance.CurrentValue; }
			set { this.deflectChance.CurrentValue = value; }
		}

		/// <summary>An attacker whose int beats a roll in this range gets its attention.</summary>
		public double[] AttentionInt {
			get { return (double[]) this.attentionInt.CurrentValue; }
			set { this.attentionInt.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int EyeNpcManaThreshold = 200, EyeNpcManaCost = 35;

		private static EyeAbilityDef eyeAbilityDef;
		internal static EyeAbilityDef EyeDef {
			get {
				if (eyeAbilityDef == null) {
					eyeAbilityDef = AbstractScript.GetByDefname("a_tevent_e_eye") as EyeAbilityDef;
				}
				return eyeAbilityDef;
			}
		}

		/// <summary>#799 e_eye, attacker side after its own landed hit. Returns the hit points leeched.</summary>
		public static int TryApplyEyeDrain(Character eye, Character target, int landedDamage) {
			EyeAbilityDef def = EyeDef;
			if (!HasAbilityDef(eye, def) || !eye.IsAliveAndValid || target.IsDeleted) {
				return 0;
			}
			int stamLoss = (int) Math.Round(ScriptUtil.GetRandInRange(def.StamDrain[0], def.StamDrain[1]));
			target.Stam = (short) Math.Max(0, target.Stam - stamLoss);

			int hitsCap = (int) Math.Min(short.MaxValue, eye.MaxHits * def.HitsCapMultiplier);
			if (target.IsPlayerForCombat) {
				if ((target.Mana >= def.PlayerManaCost) && (eye.Hits < hitsCap)) {
					int leeched = Math.Min((int) (landedDamage * def.LeechMultiplier), hitsCap - eye.Hits);
					eye.Hits = (short) (eye.Hits + leeched);
					target.Mana = (short) (target.Mana - def.PlayerManaCost);
					return leeched;
				}
				target.Mana = 0;
			} else if (target.Mana > EyeNpcManaThreshold) {
				target.Mana = (short) (target.Mana - EyeNpcManaCost);
			}
			return 0;
		}

		/// <summary>#799 e_eye, defender side before CauseDamage: a physical blow may bounce off.</summary>
		public static bool TryApplyEyeDeflect(Character attacker, Character eye, WeaponSwingArgs swingArgs) {
			EyeAbilityDef def = EyeDef;
			if (!HasAbilityDef(eye, def) || Professions.HasProfession(attacker, Professions.Mystic)) {
				return false; //a Mystic's blow is mind power, not physical
			}
			if (!RollChance(def.DeflectChance)) {
				return false;
			}
			swingArgs.DamageAfterAC = 0;
			attacker.SysMessage("Your blow bounces off " + eye.Name + "!");
			return true;
		}

		/// <summary>#799 e_eye, defender side after a landed swing: a clever attacker draws its eye.</summary>
		public static void TryApplyEyeAttention(Character attacker, Character eye) {
			EyeAbilityDef def = EyeDef;
			if (!HasAbilityDef(eye, def) || !eye.IsAliveAndValid) {
				return;
			}
			if (attacker.Int > ScriptUtil.GetRandInRange(def.AttentionInt[0], def.AttentionInt[1])) {
				SwitchTargetTo(eye, attacker);
			}
		}
	}
}
