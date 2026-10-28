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
	/// Real-user-reported follow-up - e_npc_curse ("Dark Curse"). Has the cleanest doc comment
	/// of any ability in the whole archive: "hurts all the enemies in the distance of 8 using
	/// dark and dirty magic". Real defaults: chance(110,3)=11%, delay {12,15} real seconds
	/// (randombetween then x10 - already real seconds, same convention as RoundattackAbilityDef's
	/// own Cooldown), dmg {50,60}.</summary>
	[ViewableClass]
	public class NpcCurseAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue damage;
		private FieldValue range;

		public NpcCurseAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("npcCurseChance", new double[] { 110, 110 }, typeof(double[]));
			this.cooldown = this.InitTypedField("npcCurseCooldown", new double[] { 12, 15 }, typeof(double[]));
			this.damage = this.InitTypedField("npcCurseDamage", new double[] { 50, 60 }, typeof(double[]));
			this.range = this.InitTypedField("npcCurseRange", (double) 8, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between pulses, real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>AoE magic damage range.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>AoE radius in tiles.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static NpcCurseAbilityDef npcCurseAbilityDef;
		private static NpcCurseAbilityDef NpcCurseDef {
			get {
				if (npcCurseAbilityDef == null) {
					npcCurseAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_curse") as NpcCurseAbilityDef;
				}
				return npcCurseAbilityDef;
			}
		}

		private static readonly TagKey nextNpcCurseTimeTK = TagKey.Acquire("_npcNextCurseTime_");

		/// <summary>#npc_curse ("Dark Curse") - periodic ranged AoE magic damage pulse, same
		/// once-per-think-tick-while-fighting pattern as roundattack/parakrik. Real archive
		/// target filter: any living, non-GM player, OR any tamed pet - wild monsters other
		/// than the caster itself are never hit (matches f_npcCurse_effect's own
		/// isPlayer-or-isPet check).</summary>
		public static void TryApplyNpcCurse(NPC self) {
			NpcCurseAbilityDef def = NpcCurseDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			if (Globals.TimeInSeconds < NextNpcCurseTime(self)) {
				return;
			}

			NPCDef npcDef = (NPCDef) self.Def;
			double cooldown = RollTunable(npcDef.NpcCurseCooldown, def.Cooldown);
			SetNextNpcCurseTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.NpcCurseChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double damage = RollTunable(npcDef.NpcCurseDamage, def.Damage);
			int range = (int) def.Range;
			foreach (AbstractCharacter ch in self.GetMap().GetCharsInRange(self.X, self.Y, range)) {
				Character target = ch as Character;
				if (!IsValidNpcCurseTarget(target)) {
					continue;
				}
				//bare DamageType.Magic has no real-vs-phase-2 resist "subtype" bit and throws -
				//MagicMystical is this engine's existing generic bucket for "magic damage, no
				//specific element" (already used for the Mystic profession's own attack), which
				//fits "dark and dirty magic" just as well as any other non-elemental label would.
				DamageManager.CauseDamage(self, target, DamageType.MagicMystical, damage);
			}
		}

		private static bool IsValidNpcCurseTarget(Character target) {
			if ((target == null) || target.IsDeleted || !target.IsAliveAndValid || target.IsNotVisible) {
				return false;
			}
			if (target.IsPlayerForCombat) {
				return !target.IsGM;
			}
			NPC npc = target as NPC;
			return (npc != null) && (npc.ControlMaster != null); //a tamed pet
		}

		private static double NextNpcCurseTime(NPC self) {
			object tag = self.GetTag(nextNpcCurseTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextNpcCurseTime(NPC self, double value) {
			self.SetTag(nextNpcCurseTimeTK, value);
		}
	}
}
