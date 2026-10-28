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

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_stun is
	/// upgraded from a zero-effect PassiveAbilityDef stub to a real typed ability with
	/// genuine combat behavior, same defname (existing NPCDef.StartingAbilities grants via
	/// NPC.TryResolveUnknownTriggerGroupName keep working unchanged - only
	/// teventStubs.scp's own [StunAbilityDef ...] header line changes). Fields hold the
	/// engine-wide DEFAULT values (same numbers the original Sphere archive scripts
	/// themselves default to when a monster's own tag.xxx= override is unset) - a specific
	/// NPCDef can override any of these via its own matching per-species range property
	/// (NPC.cs's StunChance/StunDuration/etc, same two-layer precedent as
	/// NPCDef.SpellCastRange). GM-editable per-field via .info's existing Abilities ->
	/// "Def info" reflection-based editor (StatDrainingAbilityDef's own precedent), no new
	/// dialog code needed. Consumed by WeaponSkillDef.On_Stroke (on a landed melee hit) -
	/// see that file for the actual trigger logic; this file only declares the tunable data
	/// shape plus the consumer logic in MonsterSpecialAttacks.TryApplyStun below.
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

	public static partial class MonsterSpecialAttacks {
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
	}
}
