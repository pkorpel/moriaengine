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
	/// #797 e_lightnings (19 monsters). Archive: when hit, tag.e_lightnings_chance (permille,
	/// default 50 = 5%) to strike a random spot within tag.e_lightnings_range (default 5) with
	/// a pattern of lightning hazard tiles (HazardTiles.cs) - either a "+" or an "x", each arm 3
	/// tiles long - each tile dealing {40,80} electric damage to the player/pet it hits.</summary>
	[ViewableClass]
	public class LightningsAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue range;
		private FieldValue damage;

		public LightningsAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("lightningsChance", new double[] { 50, 50 }, typeof(double[]));
			this.range = this.InitTypedField("lightningsRange", new double[] { 5, 5 }, typeof(double[]));
			this.damage = this.InitTypedField("lightningsDamage", new double[] { 40, 80 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed swing taken.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Max distance (tiles) of the strike's centre from the monster.</summary>
		public double[] Range {
			get { return (double[]) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}

		/// <summary>Electric damage of each lightning tile (rolled per tile).</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int LightningsArm = 3;

		private static LightningsAbilityDef lightningsAbilityDef;
		internal static LightningsAbilityDef LightningsDef {
			get {
				if (lightningsAbilityDef == null) {
					lightningsAbilityDef = AbstractScript.GetByDefname("a_tevent_e_lightnings") as LightningsAbilityDef;
				}
				return lightningsAbilityDef;
			}
		}

		/// <summary>#797 e_lightnings - defender side, after a landed swing. Returns true when it struck.</summary>
		public static bool TryApplyLightnings(Character defender) {
			LightningsAbilityDef def = LightningsDef;
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid) {
				return false;
			}
			NPCDef npcDef = TuningDef(defender); //#1022 a form's while Darkmoon has one
			if (!RollChance(RollTunable(npcDef == null ? null : npcDef.LightningsChance, def.Chance))) {
				return false;
			}
			int range = (int) RollTunable(npcDef == null ? null : npcDef.LightningsRange, def.Range);
			int cx = defender.X + Globals.dice.Next(-range, range + 1);
			int cy = defender.Y + Globals.dice.Next(-range, range + 1);
			ForceLightnings(defender, cx, cy, Globals.dice.Next(2) == 0);
			return true;
		}

		/// <summary>The strike itself at (cx, cy), without the chance roll - for tests. `cross`
		/// picks the "+" pattern, otherwise the "x".</summary>
		public static void ForceLightnings(Character owner, int cx, int cy, bool cross) {
			LightningsAbilityDef def = LightningsDef;
			double[] damage = (def == null) ? new double[] { 40, 80 } : def.Damage;
			for (int i = -LightningsArm; i <= LightningsArm; i++) {
				if (cross) {
					PlaceHazardTile(owner, cx + i, cy, ScriptUtil.GetRandInRange(damage[0], damage[1]), DamageType.MagicElectric, true);
					if (i != 0) {
						PlaceHazardTile(owner, cx, cy + i, ScriptUtil.GetRandInRange(damage[0], damage[1]), DamageType.MagicElectric, true);
					}
				} else {
					PlaceHazardTile(owner, cx + i, cy + i, ScriptUtil.GetRandInRange(damage[0], damage[1]), DamageType.MagicElectric, true);
					if (i != 0) {
						PlaceHazardTile(owner, cx + i, cy - i, ScriptUtil.GetRandInRange(damage[0], damage[1]), DamageType.MagicElectric, true);
					}
				}
			}
		}
	}
}
