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
	/// Real-user-reported follow-up - e_parakrik ("Paralyzing Scream"). Real archive default
	/// is chance(70,3)=7% per swing ATTEMPT (not just a landed one, same as roundattack), own
	/// cooldown default 50 raw deciseconds (5s, same raw-decisecond convention as StunAbilityDef's
	/// own Duration/Cooldown). Real mechanic pulses every non-GM, non-dead PLAYER in the
	/// monster's own map sector - approximated here with a flat tile Range (default 8, same
	/// precedent as RoundattackAbilityDef/DetectAbilityDef's own Range fields) since this engine
	/// has no equivalent "same sector" concept exposed for an AoE query.</summary>
	[ViewableClass]
	public class ParaKrikAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue range;

		public ParaKrikAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("parakrikChance", new double[] { 70, 70 }, typeof(double[]));
			this.cooldown = this.InitTypedField("parakrikCooldown", new double[] { 50, 50 }, typeof(double[]));
			this.range = this.InitTypedField("parakrikRange", (double) 8, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Minimum time between two screams, raw archive deciseconds (divide by 10 for
		/// real seconds) - deliberately shadows AbilityDef.Cooldown (a single real-seconds
		/// scalar, different shape/unit) rather than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles, approximating the real archive's "same sector" scope.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static ParaKrikAbilityDef paraKrikAbilityDef;
		internal static ParaKrikAbilityDef ParaKrikDef {
			get {
				if (paraKrikAbilityDef == null) {
					paraKrikAbilityDef = AbstractScript.GetByDefname("a_tevent_e_parakrik") as ParaKrikAbilityDef;
				}
				return paraKrikAbilityDef;
			}
		}

		private static readonly TagKey nextParaKrikTimeTK = TagKey.Acquire("_npcNextParaKrikTime_");

		/// <summary>#parakrik - periodic (checked once per NPCAiTimer.Fight think-tick while
		/// fighting, same "faithful simpler equivalent of a per-swing-attempt recharge-gated
		/// roll" precedent as roundattack) chance to paralyze every non-GM, living player within
		/// Range - pets/summons/other monsters are untouched, matching the real archive's own
		/// player-only filter. Reuses the exact same ParalyzeEffectPluginDef.Paralyze call stun
		/// already uses, including the same ResistParalyse&gt;900 immunity check.</summary>
		public static void TryApplyParaKrik(NPC self) {
			ParaKrikAbilityDef def = ParaKrikDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextParaKrikTime(self)) {
				return;
			}

			NPCDef npcDef = (NPCDef) self.Def;
			double cooldown = RollTunable(npcDef.ParaKrikCooldown, def.Cooldown);
			//real archive unit: raw deciseconds (see StunAbilityDef's own doc comment for this
			//same convention) - divide by 10 for real seconds, here at the point of use.
			SetNextParaKrikTime(self, Globals.TimeInSeconds + (cooldown / 10.0));

			double chance = RollTunable(npcDef.ParaKrikChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if ((target == null) || target.IsGM || !target.IsPlayerForCombat || !target.IsAliveAndValid) {
					continue;
				}
				if (target.ResistParalyse > 900) {
					continue;
				}
				ParalyzeEffectPluginDef.Paralyze(target, paraKrikPluginKey, TimeSpan.FromSeconds(3));
			}
		}

		//a dedicated key (not stunPluginKey) - a target can legitimately be stunned AND
		//screamed-at independently, each needs its own installed-plugin slot. internal (not
		//private) so a test can confirm the scream actually landed, same precedent as
		//stunPluginKey above.
		internal static readonly PluginKey paraKrikPluginKey = PluginKey.Acquire("_npcParaKrik_");

		private static double NextParaKrikTime(NPC self) {
			object tag = self.GetTag(nextParaKrikTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextParaKrikTime(NPC self, double value) {
			self.SetTag(nextParaKrikTimeTK, value);
		}
	}
}
