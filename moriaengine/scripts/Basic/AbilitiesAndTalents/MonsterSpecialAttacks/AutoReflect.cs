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
	/// #797 e_autoreflect (10 monsters). Archive: flag_reflection=1 after every swing it takes,
	/// so its magic reflection comes straight back after it bounced a spell. Here: the real
	/// Magic Reflection effect (MagicReflectionPlugin) is put back on whenever it's missing.</summary>
	[ViewableClass]
	public class AutoReflectAbilityDef : PassiveAbilityDef {
		private FieldValue power;

		public AutoReflectAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.power = this.InitTypedField("autoReflectPower", (double) 1, typeof(double));
		}

		/// <summary>Fraction of a reflected spell's power sent back (Sphere's flag_reflection
		/// reflects the whole spell; the Magic Reflection spell itself uses 0.75).</summary>
		public double Power {
			get { return (double) this.power.CurrentValue; }
			set { this.power.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		//"permanent" - re-armed on every swing taken anyway
		private static readonly TimeSpan autoReflectDuration = TimeSpan.FromHours(1);

		private static AutoReflectAbilityDef autoReflectAbilityDef;
		internal static AutoReflectAbilityDef AutoReflectDef {
			get {
				if (autoReflectAbilityDef == null) {
					autoReflectAbilityDef = AbstractScript.GetByDefname("a_tevent_e_autoreflect") as AutoReflectAbilityDef;
				}
				return autoReflectAbilityDef;
			}
		}

		/// <summary>#797 e_autoreflect - defender side, after a landed swing. Returns true when
		/// the reflection was (re)armed.</summary>
		public static bool TryApplyAutoReflect(Character defender) {
			AutoReflectAbilityDef def = AutoReflectDef;
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid || MagicReflectionPluginDef.HasMagicReflection(defender)) {
				return false;
			}
			DurableCharEffectSpellDef spell = MagicReflectionPluginDef.MagicReflectionSpellDef;
			MagicReflectionPlugin plugin = (MagicReflectionPlugin) MagicReflectionPluginDef.instance.Create();
			//FromAbility with the ability as its source: EffectDurationPlugin.EffectName names it after the ability
			plugin.Init(defender, EffectFlag.FromAbility | EffectFlag.BeneficialEffect, def.Power, autoReflectDuration, def);
			defender.AddPlugin(spell.EffectPluginKey_Spell, plugin);
			return true;
		}
	}
}
