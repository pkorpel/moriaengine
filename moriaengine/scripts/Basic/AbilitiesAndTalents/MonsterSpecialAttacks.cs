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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_stun/
	/// e_npc_critic/e_npc_roundattack(_pullall) are upgraded from zero-effect PassiveAbilityDef
	/// stubs to real typed abilities with genuine combat behavior, same defnames (existing
	/// NPCDef.StartingAbilities grants via NPC.TryResolveUnknownTriggerGroupName keep working
	/// unchanged - only teventStubs.scp's own [XxxAbilityDef ...] header line per ability
	/// changes). Fields hold the engine-wide DEFAULT values (same numbers the original Sphere
	/// archive scripts themselves default to when a monster's own tag.xxx= override is
	/// unset) - a specific NPCDef can override any of these via its own matching per-species
	/// range property (NPC.cs's StunChance/StunDuration/etc, same two-layer precedent as
	/// NPCDef.SpellCastRange). GM-editable per-field via .info's existing Abilities ->
	/// "Def info" reflection-based editor (StatDrainingAbilityDef's own precedent), no new
	/// dialog code needed. Consumed by WeaponSkillDef.On_Stroke (stun/critic, on a landed
	/// melee hit) and NPCAiTimer (roundattack/pullall, a periodic AoE pulse) - see those
	/// files for the actual trigger logic; this file only declares the tunable data shape.
	/// </summary>
	[ViewableClass]
	public class StunAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue duration;
		private FieldValue cooldown;

		public StunAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("stunChance", new double[] { 250, 250 }, typeof(double[]));
			//#stun real archive unit: Sphere's own act.paralyze() built-in and tag.npc_stun_delay
			//both take DECISECONDS (raw 20 = 2.0s, raw 30 = 3.0s per the source script's own
			//comments) - Duration/Cooldown here stay in that SAME raw unit (divided by 10 at the
			//point of use in WeaponSkillDef, not here) so a .def author copying a real archive
			//tag.npc_stun_time/_delay value in verbatim just works, no silent unit mismatch.
			this.duration = this.InitTypedField("stunDuration", new double[] { 20, 20 }, typeof(double[]));
			this.cooldown = this.InitTypedField("stunCooldown", new double[] { 30, 30 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to stun on a landed melee swing -
		/// deliberately shadows AbilityDef.Chance (a single 0.0-1.0 fraction, different shape/
		/// scale, unrelated semantics) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Stun duration, raw archive deciseconds (divide by 10 for real seconds).</summary>
		public double[] Duration {
			get { return (double[]) this.duration.CurrentValue; }
			set { this.duration.CurrentValue = value; }
		}

		/// <summary>Minimum time between two stuns from the same attacker, raw archive
		/// deciseconds (divide by 10 for real seconds) - deliberately shadows AbilityDef.
		/// Cooldown (a single real-seconds scalar, different shape/unit) rather than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}
	}

	[ViewableClass]
	public class CriticAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue damage;

		public CriticAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("criticChance", new double[] { 250, 250 }, typeof(double[]));
			this.damage = this.InitTypedField("criticDamage", new double[] { 50, 60 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to land a critical hit, melee vs.
		/// players only (same real archive restriction) - deliberately shadows AbilityDef.Chance
		/// (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Bonus damage range added to the landed swing on a critical.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}
	}

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

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - e_npc_bleed. Default
	/// {1000,1000} (100%) deliberately matches the real archive's own default: tag.npc_bleed
	/// is unset on most monsters, and the source script's own chance check only runs "if
	/// (<eval tag.npc_bleed>)" at all - unset means no chance roll, i.e. always bleeds; the tag
	/// only ever LOWERS the chance on specific species. NPCDef.BleedChance (same two-layer
	/// override precedent as the other 3 abilities) is how that per-species lowering is
	/// recovered from the archive.</summary>
	[ViewableClass]
	public class BleedAbilityDef : PassiveAbilityDef {
		private FieldValue chance;

		public BleedAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("bleedChance", new double[] { 1000, 1000 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to bleed on a landed melee swing -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}
	}

	/// <summary>
	/// Consumer side of StunAbilityDef/CriticAbilityDef - rolled from DamageManager.ProcessSwing
	/// on every landed melee swing (RoundattackAbilityDef/RoundattackPullAbilityDef are consumed
	/// separately, from NPCAiTimer's own periodic tick - a recharge-gated pulse, not a per-swing
	/// roll). Each roll checks the attacker's own per-species NPCDef override range first
	/// (NPC.cs's StunChance/CriticChance/etc - null if unconfigured), falling back to whichever
	/// of these two AbilityDefs the attacker was actually granted.
	/// </summary>
	public static class MonsterSpecialAttacks {
		//internal (not private) so a test can confirm the stun actually landed (ability
		//granted + chance roll + resist check all passed) by checking for this plugin
		//directly, same "internal for direct test invocation" precedent as NPCAiTimer.Fight.
		internal static readonly PluginKey stunPluginKey = PluginKey.Acquire("_npcStun_");

		private static StunAbilityDef stunAbilityDef;
		private static StunAbilityDef StunDef {
			get {
				if (stunAbilityDef == null) {
					stunAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_stun") as StunAbilityDef;
				}
				return stunAbilityDef;
			}
		}

		private static CriticAbilityDef criticAbilityDef;
		private static CriticAbilityDef CriticDef {
			get {
				if (criticAbilityDef == null) {
					criticAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_critic") as CriticAbilityDef;
				}
				return criticAbilityDef;
			}
		}

		/// <summary>Rolls `permille` chance in [0, 1000) - e.g. 250 means 25%.</summary>
		private static bool RollChance(double permille) {
			return Globals.dice.Next(1000) < permille;
		}

		/// <summary>Resolves a tunable range: the attacker's own NPCDef override if configured
		/// (per-species, e.g. NPCDef.StunChance), otherwise the granted AbilityDef's own default
		/// field. `npcOverride` is null when that specific NPCDef never set the override.</summary>
		private static double RollTunable(int[] npcOverride, double[] abilityDefault) {
			if (npcOverride != null) {
				return NPCDef.RollStatRange(npcOverride);
			}
			return ScriptUtil.GetRandInRange(abilityDefault[0], abilityDefault[1]);
		}

		/// <summary>#stun - on a landed melee swing, a chance to paralyze the defender for a
		/// short time, gated by its own per-attacker cooldown (so it can't stun-lock every
		/// swing) - see StunAbilityDef's own doc comment for the real archive source this
		/// mirrors. No-op if `attacker` was never granted a_tevent_e_npc_stun (every player and
		/// most NPCs) or StunDef failed to resolve (teventStubs.scp not loaded - shouldn't
		/// happen outside a broken boot).</summary>
		public static void TryApplyStun(Character attacker, Character defender) {
			StunAbilityDef def = StunDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}
			//real archive guard: a target heavily resistant to paralysis never gets stunned at
			//all, regardless of roll (ResistParalyse is the same 0-1000 permille scale the
			//archive's own tag.resist_para check used).
			if (defender.ResistParalyse > 900) {
				return;
			}
			if (Globals.TimeInSeconds < NextStunTime(attacker)) {
				return; //still on this attacker's own stun cooldown
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.StunChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double duration = RollTunable(npcDef == null ? null : npcDef.StunDuration, def.Duration);
			double cooldown = RollTunable(npcDef == null ? null : npcDef.StunCooldown, def.Cooldown);
			//real archive unit: raw deciseconds (see StunAbilityDef.Duration/Cooldown's own doc
			//comment) - divide by 10 for real seconds, here at the point of use.
			SetNextStunTime(attacker, Globals.TimeInSeconds + (cooldown / 10.0));
			ParalyzeEffectPluginDef.Paralyze(defender, stunPluginKey, TimeSpan.FromSeconds(duration / 10.0));
		}

		private static readonly TagKey nextStunTimeTK = TagKey.Acquire("_npcNextStunTime_");

		private static double NextStunTime(Character attacker) {
			object tag = attacker.GetTag(nextStunTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextStunTime(Character attacker, double value) {
			attacker.SetTag(nextStunTimeTK, value);
		}

		/// <summary>#critic - on a landed melee swing against a player specifically (same real
		/// archive restriction), a chance to add bonus damage to the swing that's already about
		/// to land. Must be called BEFORE DamageManager.CauseDamage, while swingArgs.DamageAfterAC
		/// is still mutable.</summary>
		public static void TryApplyCritic(Character attacker, WeaponSwingArgs swingArgs) {
			CriticAbilityDef def = CriticDef;
			if (def == null) {
				return;
			}
			if (!swingArgs.defender.IsPlayerForCombat) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.CriticChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double bonus = RollTunable(npcDef == null ? null : npcDef.CriticDamage, def.Damage);
			swingArgs.DamageAfterAC += bonus;
			CombatMessages.Send(attacker, "[Combat] Critical hit!");
			CombatMessages.Send(swingArgs.defender, "[Combat] " + attacker.Name + " critically hits you!");
		}

		private static BleedAbilityDef bleedAbilityDef;
		private static BleedAbilityDef BleedDef {
			get {
				if (bleedAbilityDef == null) {
					bleedAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_bleed") as BleedAbilityDef;
				}
				return bleedAbilityDef;
			}
		}

		/// <summary>#bleed - on a landed melee swing, a chance to apply/stack a bleed DoT
		/// (NpcBleedEffectPluginDef) scaled off the damage that swing actually dealt - same
		/// real archive ratio (bleedpower = actualDamage/3). Must be called AFTER
		/// DamageManager.CauseDamage, since it needs the real landed-damage amount, not the
		/// pre-armor swing value. A miss or a fully-blocked swing (actualDamage &lt;= 0) has
		/// nothing to scale off of - same as the real archive's own `wasHarmed` guard.</summary>
		public static void TryApplyBleed(Character attacker, Character defender, int actualDamage) {
			BleedAbilityDef def = BleedDef;
			if (def == null) {
				return;
			}
			if (actualDamage <= 0) {
				return;
			}
			Ability ability;
			if (!attacker.HasAbility(def, out ability)) {
				return;
			}

			NPCDef npcDef = attacker.Def as NPCDef;
			double chance = RollTunable(npcDef == null ? null : npcDef.BleedChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double effectPower = actualDamage / 3.0;
			NpcBleedEffectPluginDef bleedingDef = SingletonScript<NpcBleedEffectPluginDef>.Instance;
			bleedingDef.Apply(attacker, defender, EffectFlag.HarmfulEffect, effectPower, bleedingDef.MaxTicks);
		}

		private static RoundattackAbilityDef roundattackAbilityDef;
		private static RoundattackAbilityDef RoundattackDef {
			get {
				if (roundattackAbilityDef == null) {
					roundattackAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack") as RoundattackAbilityDef;
				}
				return roundattackAbilityDef;
			}
		}

		private static RoundattackPullAbilityDef roundattackPullAbilityDef;
		private static RoundattackPullAbilityDef RoundattackPullDef {
			get {
				if (roundattackPullAbilityDef == null) {
					roundattackPullAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_roundattack_pullall") as RoundattackPullAbilityDef;
				}
				return roundattackPullAbilityDef;
			}
		}

		private static readonly TagKey nextRoundattackTimeTK = TagKey.Acquire("_npcNextRoundattackTime_");

		//North,NorthEast,East,SouthEast,South,SouthWest,West,NorthWest (GameCore/Enums.cs's own
		//Direction order) - used by PullToward below to find the one tile adjacent to the
		//attacker, on the side the target is already standing.
		private static readonly int[] directionDx = { 0, 1, 1, 1, 0, -1, -1, -1 };
		private static readonly int[] directionDy = { -1, -1, 0, 1, 1, 1, 0, -1 };

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

			NPCDef npcDef = (NPCDef) self.Def;
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
					DamageManager.CauseDamage(self, target, self.WeaponDamageType, damage);
				}
			}
		}

		private static bool IsValidRoundattackTarget(NPC self, Character target) {
			return (target != null) && (target != self) && !target.IsDeleted && target.IsAliveAndValid &&
				!target.IsNotVisible && !target.IsGM;
		}

		private static void PullToward(NPC self, Character target) {
			if (Point2D.GetSimpleDistance(self, target) <= 1) {
				return; //already adjacent - nothing to pull
			}
			Direction dir = Point2D.GetDirFromTo(self, target);
			int x = self.X + directionDx[(int) dir];
			int y = self.Y + directionDy[(int) dir];
			Map map = self.GetMap();
			if (!map.IsValidPos(x, y) || map.IsMapTileWater(x, y)) {
				return; //no safe spot to land the pull - skip it rather than clip into something
			}
			int tileId = map.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(tileId), TileFlag.Impassable)) {
				return;
			}
			int z = map.GetTileZ(x, y);
			target.Go(x, y, z, target.M);
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
