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
	/// #797 e_phoenix_fire (51 monsters). Archive: on 1 in 15 of its own swings the monster
	/// "outblazes" - fireexplosion(10, dmg) drops 10 fire-column hazard tiles at random spots
	/// within 5 tiles (HazardTiles.cs), each dealing tag.phoenix_fire_damage (default 20) fire
	/// damage to the player/pet it hits.</summary>
	[ViewableClass]
	public class PhoenixFireAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue columns;
		private FieldValue radius;
		private FieldValue damage;

		public PhoenixFireAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("phoenixFireChance", new double[] { 67, 67 }, typeof(double[]));
			this.columns = this.InitTypedField("phoenixFireColumns", 10, typeof(int));
			this.radius = this.InitTypedField("phoenixFireRadius", 5, typeof(int));
			this.damage = this.InitTypedField("phoenixFireDamage", new double[] { 20, 20 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed swing (archive: 1 in 15).</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>How many fire columns one outburst drops.</summary>
		public int Columns {
			get { return (int) this.columns.CurrentValue; }
			set { this.columns.CurrentValue = value; }
		}

		/// <summary>Max distance (tiles) of a column from the monster.</summary>
		public int Radius {
			get { return (int) this.radius.CurrentValue; }
			set { this.radius.CurrentValue = value; }
		}

		/// <summary>Fire damage of each column.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static PhoenixFireAbilityDef phoenixFireAbilityDef;
		internal static PhoenixFireAbilityDef PhoenixFireDef {
			get {
				if (phoenixFireAbilityDef == null) {
					phoenixFireAbilityDef = AbstractScript.GetByDefname("a_tevent_e_phoenix_fire") as PhoenixFireAbilityDef;
				}
				return phoenixFireAbilityDef;
			}
		}

		/// <summary>#797 e_phoenix_fire - attacker side, after a landed swing.</summary>
		public static void TryApplyPhoenixFire(Character attacker) {
			PhoenixFireAbilityDef def = PhoenixFireDef;
			if (!HasAbilityDef(attacker, def)) {
				return;
			}
			NPCDef npcDef = attacker.Def as NPCDef;
			if (!RollChance(RollTunable(null, def.Chance))) {
				return;
			}
			ForcePhoenixFire(attacker, RollTunable(npcDef == null ? null : npcDef.PhoenixFireDamage, def.Damage));
		}

		/// <summary>The outburst itself, without the chance roll - for tests.</summary>
		public static void ForcePhoenixFire(Character attacker, double damage) {
			PhoenixFireAbilityDef def = PhoenixFireDef;
			int radius = (def == null) ? 5 : def.Radius;
			int columns = (def == null) ? 10 : def.Columns;
			attacker.Emote("outblazes!");
			for (int i = 0; i < columns; i++) {
				int x = attacker.X + Globals.dice.Next(-radius, radius + 1);
				int y = attacker.Y + Globals.dice.Next(-radius, radius + 1);
				PlaceHazardTile(attacker, x, y, damage, DamageType.MagicFire, false);
			}
		}
	}
}
