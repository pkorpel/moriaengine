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

using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_npc_reactivearmor (19 monsters). Archive: a swing that lands on the monster from
	/// within 2 tiles bounces tag.reactivearmor_damagePerc percent (default 20) of the damage
	/// back onto the attacker, with the same damage type. Mystics are immune (their staff
	/// attack is mind power, not a blow).</summary>
	[ViewableClass]
	public class ReactiveArmorAbilityDef : PassiveAbilityDef {
		private FieldValue percent;
		private FieldValue range;

		public ReactiveArmorAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.percent = this.InitTypedField("reactiveArmorPercent", new double[] { 20, 20 }, typeof(double[]));
			this.range = this.InitTypedField("reactiveArmorRange", 2, typeof(int));
		}

		/// <summary>Percent of the landed damage reflected.</summary>
		public double[] Percent {
			get { return (double[]) this.percent.CurrentValue; }
			set { this.percent.CurrentValue = value; }
		}

		/// <summary>Only attackers within this many tiles get hit back.</summary>
		public int Range {
			get { return (int) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int HarmEffect = 0x374A;

		private static ReactiveArmorAbilityDef reactiveArmorAbilityDef;
		internal static ReactiveArmorAbilityDef ReactiveArmorDef {
			get {
				if (reactiveArmorAbilityDef == null) {
					reactiveArmorAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_reactivearmor") as ReactiveArmorAbilityDef;
				}
				return reactiveArmorAbilityDef;
			}
		}

		/// <summary>#797 e_npc_reactivearmor - defender side, after a landed swing. Returns the
		/// damage reflected onto the attacker.</summary>
		public static int TryApplyReactiveArmor(Character attacker, Character defender, DamageType damageType, int landedDamage) {
			ReactiveArmorAbilityDef def = ReactiveArmorDef;
			if ((landedDamage <= 0) || !HasAbilityDef(defender, def) || !attacker.IsAliveAndValid) {
				return 0;
			}
			if ((Point2D.GetSimpleDistance(attacker, defender) > def.Range) || Professions.HasProfession(attacker, Professions.Mystic)) {
				return 0;
			}
			NPCDef npcDef = defender.Def as NPCDef;
			double percent = RollTunable(npcDef == null ? null : npcDef.ReactiveArmorPercent, def.Percent);
			EffectFactory.StationaryEffect(attacker, HarmEffect, 6, 15);
			if (Globals.dice.Next(5) == 0) {
				attacker.SysMessage("Your blow slams hard into " + defender.Name + "'s armor.");
			}
			return DamageManager.SendBlowBack(defender, attacker, damageType, (landedDamage * percent + 50) / 100); //#1519 - src.damage_noresponse (new_events.scp)
		}
	}
}
