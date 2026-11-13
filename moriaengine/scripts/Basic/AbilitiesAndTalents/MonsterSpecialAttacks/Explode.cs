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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SteamEngine.Common;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_explode (28 monsters). Archive: on death, everyone the monster has a fight memory
	/// of (memoryfindtype(0209c) - everyone who fought it) takes multi_damage(tag.explode_dmg,
	/// default 100): the full amount when standing on its tile, otherwise damage / distance,
	/// only with line of sight, as unresistable dam_god damage. This engine has no fight
	/// memories, so DamageManager.CauseDamage records everyone who damages an exploder.</summary>
	[ViewableClass]
	public class ExplodeAbilityDef : PassiveAbilityDef {
		private FieldValue damage;

		public ExplodeAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.damage = this.InitTypedField("explodeDamage", new double[] { 100, 100 }, typeof(double[]));
		}

		/// <summary>Damage at distance 0 (divided by the distance further out).</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int ExplosionEffect = 0x36BD;

		private static ExplodeAbilityDef explodeAbilityDef;
		internal static ExplodeAbilityDef ExplodeDef {
			get {
				if (explodeAbilityDef == null) {
					explodeAbilityDef = AbstractScript.GetByDefname("a_tevent_e_explode") as ExplodeAbilityDef;
				}
				return explodeAbilityDef;
			}
		}

		//transient on purpose - a fight memory isn't worth saving, and a weak table can't leak
		//a deleted monster
		private static readonly ConditionalWeakTable<Character, HashSet<Character>> explodeFoes =
			new ConditionalWeakTable<Character, HashSet<Character>>();

		/// <summary>DamageManager.CauseDamage - remembers who hurt an exploding monster.</summary>
		internal static void RecordExplodeFoe(Character attacker, Character defender) {
			if ((attacker == null) || (attacker == defender) || !HasAbilityDef(defender, ExplodeDef)) {
				return;
			}
			explodeFoes.GetOrCreateValue(defender).Add(attacker);
		}

		/// <summary>#797 e_explode - Character.CauseDeath, while the dying monster's position is
		/// still valid. Returns how many foes were hit.</summary>
		public static int TryApplyExplode(Character dying) {
			ExplodeAbilityDef def = ExplodeDef;
			HashSet<Character> foes;
			if (!HasAbilityDef(dying, def) || !explodeFoes.TryGetValue(dying, out foes)) {
				return 0;
			}
			explodeFoes.Remove(dying);
			NPCDef npcDef = dying.Def as NPCDef;
			double damage = RollTunable(npcDef == null ? null : npcDef.ExplodeDamage, def.Damage);
			int hit = 0;
			foreach (Character foe in new List<Character>(foes)) {
				if (foe.IsDeleted || !foe.IsAliveAndValid || (foe.M != dying.M) || !dying.CanSeeLOS(foe).Allow) {
					continue;
				}
				int distance = Point2D.GetSimpleDistance(dying, foe);
				EffectFactory.StationaryEffect(foe, ExplosionEffect, 6, 15);
				DamageManager.CauseDamageNoResponse(dying, foe, DamageType.Irresistable, (distance == 0) ? damage : damage / distance); //#1519 - BlowUp.scp's damage_noresponse
				hit++;
			}
			return hit;
		}
	}
}
