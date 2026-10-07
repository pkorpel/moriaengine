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

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class DamageSpellDef : SpellDef {

		#region FieldValues
		private FieldValue damageType;

		public DamageSpellDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.damageType = this.InitTypedField("damageType", DamageType.Magic, typeof(DamageType));
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
			double dam = this.CalculateDamage(target, spellEffectArgs);
			DamageManager.CauseDamage(spellEffectArgs.Caster, target, this.DamageType, dam);
		}

		protected virtual double CalculateDamage(Character target, SpellEffectArgs spellEffectArgs) {
			return this.GetEffectForValue(spellEffectArgs.SpellPower);
		}
	}
}