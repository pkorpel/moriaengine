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
using SteamEngine.Persistence;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// #797 e_pushall (11 monsters). Archive: when hit, tag.e_pushall_chance (permille,
	/// default 50) - at most once per tag.e_pushall_time seconds (default 30) - to throw every
	/// player closer than 6 tiles away from it, by (6 - distance) tiles: the closer they
	/// stand, the further they fly. Uses Knockback's forced walk, so walls stop it.</summary>
	[ViewableClass]
	public class PushallAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue range;

		public PushallAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("pushallChance", new double[] { 50, 50 }, typeof(double[]));
			this.cooldown = this.InitTypedField("pushallCooldown", new double[] { 30, 30 }, typeof(double[]));
			this.range = this.InitTypedField("pushallRange", 6, typeof(int));
		}

		/// <summary>Permille (0-1000) {min,max} chance per landed swing taken.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Seconds before it can push again.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>Players closer than this are pushed, by (range - distance) tiles.</summary>
		public int Range {
			get { return (int) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static PushallAbilityDef pushallAbilityDef;
		internal static PushallAbilityDef PushallDef {
			get {
				if (pushallAbilityDef == null) {
					pushallAbilityDef = AbstractScript.GetByDefname("a_tevent_e_pushall") as PushallAbilityDef;
				}
				return pushallAbilityDef;
			}
		}

		private static readonly TagKey nextPushallTimeTK = TagKey.Acquire("_npcNextPushallTime_");

		/// <summary>#797 e_pushall - defender side, after a landed swing. Returns how many
		/// players were pushed.</summary>
		public static int TryApplyPushall(Character defender) {
			PushallAbilityDef def = PushallDef;
			if (!HasAbilityDef(defender, def) || !defender.IsAliveAndValid) {
				return 0;
			}
			NPCDef npcDef = defender.Def as NPCDef;
			if (!RollChance(RollTunable(npcDef == null ? null : npcDef.PushallChance, def.Chance))) {
				return 0;
			}
			object due = defender.GetTag(nextPushallTimeTK);
			if ((due is double) && (Globals.TimeInSeconds < (double) due)) {
				return 0;
			}
			defender.SetTag(nextPushallTimeTK, Globals.TimeInSeconds + RollTunable(npcDef == null ? null : npcDef.PushallCooldown, def.Cooldown));
			return ForcePushall(defender);
		}

		/// <summary>The push itself, without chance/cooldown - for tests.</summary>
		public static int ForcePushall(Character self) {
			PushallAbilityDef def = PushallDef;
			int range = (def == null) ? 6 : def.Range;
			int pushed = 0;
			foreach (Character target in CharsAround(self.GetMap(), self.X, self.Y, range - 1)) {
				if ((target == self) || !target.IsPlayerForCombat || target.IsGM || !target.IsAliveAndValid) {
					continue;
				}
				int distance = Point2D.GetSimpleDistance(self, target);
				if ((distance >= range) || (distance == 0)) {
					continue;
				}
				PushAway(self, target, range - distance);
				pushed++;
			}
			return pushed;
		}
	}
}
