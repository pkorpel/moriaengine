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

using SteamEngine.CompiledScripts.Dialogs;

using System;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class DamageSpellDef : SpellDef {

		#region FieldValues
		private FieldValue damageType;
		private FieldValue armorPiercing;

		public DamageSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.damageType = this.InitTypedField("damageType", DamageType.Magic, typeof(DamageType));
			this.armorPiercing = this.InitTypedField("armorPiercing", -1, typeof(int));
		}

		/// <summary>#860 - the third number of Erebor's spell effect: such a spell goes through
		/// damagecust, so armor stops it like a swing, less this. -1 is a spell armor ignores.</summary>
		public int ArmorPiercing {
			get {
				return (int) this.armorPiercing.CurrentValue;
			}
		}

		public DamageType DamageType {
			get {
				return (DamageType) this.damageType.CurrentValue;
			}
			set {
				this.damageType.CurrentValue = value;
			}
		}
		#endregion FieldValues

		protected override void On_EffectChar(Character target, SpellEffectArgs spellEffectArgs) {
			double dam = this.CalculateDamage(spellEffectArgs.Caster, target, spellEffectArgs.SpellPower);
			DamageManager.CauseSpellDamage(spellEffectArgs.Caster, target, this.DamageType, dam);
		}

		/// <summary>#1033 - what this would do to target after armor and resists, for an NPC choosing
		/// a spell. A roll against armor can make it vary a little.</summary>
		public double EstimateDamage(Character caster, Character target, int spellPower) {
			double damage = this.CalculateDamage(caster, target, spellPower);
			if (this.DamageType == DamageType.Irresistable) {
				return damage;
			}
			return damage * DamageManager.GetResistModifier(target, this.DamageType);
		}

		protected virtual double CalculateDamage(Character caster, Character target, int spellPower) {
			//#860 - promilerange: power above 1000 keeps growing, in whole numbers
			double[] effect = this.Effect;
			double low = (effect.Length > 0) ? effect[0] : 0;
			double high = (effect.Length > 1) ? effect[1] : low;
			double damage = low + Math.Truncate(((high - low) * spellPower) / 1000);
			if (this.ArmorPiercing >= 0) {
				return CombatCalculator.CalculateSpellArmorEffect(caster, target, damage, this.ArmorPiercing);
			}
			return damage;
		}
	}
}