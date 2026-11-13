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
using SteamEngine.Persistence;
using SteamEngine.Regions;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public class RoundattackAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;
		private FieldValue cooldown;
		private FieldValue range;

		public RoundattackAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("roundattackChance", new double[] { 250, 250 }, typeof(double[]));
			//null (not a {min,max} default) is deliberate - the real archive source falls back
			//to the monster's OWN weapon damage (<def.dam>) when tag.roundattack_damage is
			//unset, not a fixed flat number. NPCAiTimer's consuming code checks for null and
			//reads self.WeaponAttack instead in that case.
			this.damage = this.InitTypedField("roundattackDamage", null, typeof(double[]));
			//real archive unit: already real seconds (the source script's own f_npc_rndAtt_
			//setVals multiplies the raw tag value by 10 "ticks", and 1 Sphere tick is 0.1s, so
			//the two conversions cancel out - unlike stun's Duration/Cooldown above, this one
			//is consumed directly as seconds, no division needed).
			this.cooldown = this.InitTypedField("roundattackCooldown", new double[] { 2, 4 }, typeof(double[]));
			this.range = this.InitTypedField("roundattackRange", (double) 3, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each recharge window -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>AoE damage range, or null to fall back to this NPC's own weapon damage
		/// (the real archive's own default behavior).</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between pulses, in real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class RoundattackPullAbilityDef : RoundattackAbilityDef {
		private FieldValue pullDistance;

		public RoundattackPullAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.pullDistance = this.InitTypedField("roundattackPullDistance", (double) 9, typeof(double));
		}

		/// <summary>How far away (tiles) a target still gets yanked toward this NPC right
		/// before the AoE pulse.</summary>
		public double PullDistance {
			get { return (double) this.pullDistance.CurrentValue; }
			set { this.pullDistance.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static RoundattackAbilityDef roundattackAbilityDef;
		internal static RoundattackAbilityDef RoundattackDef {
			get {
				if (roundattackAbilityDef == null) {
					roundattackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack") as RoundattackAbilityDef;
				}
				return roundattackAbilityDef;
			}
		}

		private static RoundattackPullAbilityDef roundattackPullAbilityDef;
		internal static RoundattackPullAbilityDef RoundattackPullDef {
			get {
				if (roundattackPullAbilityDef == null) {
					roundattackPullAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack_pullall") as RoundattackPullAbilityDef;
				}
				return roundattackPullAbilityDef;
			}
		}

		private static readonly TagKey nextRoundattackTimeTK = TagKey.Acquire("_npcNextRoundattackTime_");

		/// <summary>#roundattack - unlike stun/critic (rolled per landed swing), this is a
		/// periodic AoE pulse independent of whether this NPC's own swing just landed or missed
		/// - the real archive source fires its chance-check on practically every combat-related
		/// trigger this character experiences (attacking OR defending OR casting), gated by its
		/// own internal recharge timer either way. Checking once per NPCAiTimer think-tick while
		/// actively fighting (NPCAiTimer.Fight already only runs then) is a faithful, much
		/// simpler equivalent - the recharge timer is what actually controls the real-world
		/// pacing, not the exact trigger that happens to re-check it.</summary>
		public static void TryRoundattack(NPC self) {
			RoundattackPullAbilityDef pullDef = RoundattackPullDef;
			RoundattackAbilityDef def;
			bool pull;
			Ability ability;
			if ((pullDef != null) && self.HasAbility(pullDef, out ability)) {
				def = pullDef;
				pull = true;
			} else {
				RoundattackAbilityDef plainDef = RoundattackDef;
				if ((plainDef == null) || !self.HasAbility(plainDef, out ability)) {
					return;
				}
				def = plainDef;
				pull = false;
			}

			if (Globals.TimeInSeconds < NextRoundattackTime(self)) {
				return;
			}

			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			//real archive unit: roundattack_time is already real seconds (the source script's
			//own f_npc_rndAtt_setVals multiplies the raw tag by 10 "ticks" before adding to
			//serv.time, and a Sphere tick is 0.1s - the two conversions cancel out), unlike
			//stun's Duration/Cooldown which stay in raw deciseconds - see StunAbilityDef's doc
			//comment for that one.
			double cooldown = RollTunable(npcDef.RoundattackCooldown, def.Cooldown);
			SetNextRoundattackTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.RoundattackChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			if (pull) {
				RoundattackPullAbilityDef pullDefResolved = (RoundattackPullAbilityDef) def;
				int[] pullOverride = npcDef.PullDistance;
				double pullDistance = (pullOverride != null) ? NPCDef.RollStatRange(pullOverride) : pullDefResolved.PullDistance;
				foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, (int) pullDistance)) {
					Character pullTarget = ch as Character;
					if (IsValidRoundattackTarget(self, pullTarget)) {
						PullToward(self, pullTarget);
					}
				}
			}

			double damage;
			if (npcDef.RoundattackDamage != null) {
				damage = NPCDef.RollStatRange(npcDef.RoundattackDamage);
			} else if (def.Damage != null) {
				damage = ScriptUtil.GetRandInRange(def.Damage[0], def.Damage[1]);
			} else {
				//real archive default: falls back to the monster's own weapon damage
				//(<def.dam>) rather than a fixed flat number when unconfigured either way.
				damage = npcDef.WeaponAttack;
			}

			int range = (int) def.Range;
			AnimCalculator.PerformAnim(self, GenericAnim.AttackOverhead);
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if (IsValidRoundattackTarget(self, target)) {
					DamageManager.CauseCustomDamage(self, target, self.WeaponDamageType, damage); //#1511 - the native damage(), e_gethit
				}
			}
		}

		private static bool IsValidRoundattackTarget(NPC self, Character target) {
			return (target != null) && (target != self) && !target.IsDeleted && target.IsAliveAndValid &&
				!target.IsNotVisible && !target.IsGM;
		}

		/// <summary>#781 - drags `target` toward `self` until adjacent, one real step at a time
		/// (MonsterSpecialAttacks.ForceMove): it stops at the first wall, closed door, cliff or
		/// character in the way. It used to teleport the target straight next to the monster, so
		/// a monster inside a house pulled people in through the wall.</summary>
		private static void PullToward(NPC self, Character target) {
			int distance = Point2D.GetSimpleDistance(self, target);
			if (distance <= 1) {
				return; //already adjacent - nothing to pull
			}
			int monsterX = self.X, monsterY = self.Y;
			ForceMove(target, distance - 1,
				(x, y) => Point2D.GetDirFromTo(new Point2D(x, y), new Point2D(monsterX, monsterY)),
				(x, y) => Math.Max(Math.Abs(x - monsterX), Math.Abs(y - monsterY)) <= 1);
		}

		private static double NextRoundattackTime(NPC self) {
			object tag = self.GetTag(nextRoundattackTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextRoundattackTime(NPC self, double value) {
			self.SetTag(nextRoundattackTimeTK, value);
		}
	}
}
