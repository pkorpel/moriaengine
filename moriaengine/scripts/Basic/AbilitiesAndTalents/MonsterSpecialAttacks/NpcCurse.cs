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
	/// Shared shape of the archive's three area curses - e_npc_curse ("Dark Curse", drains
	/// life), e_npc_curse_mana and e_npc_curse_stam (#797). All three come from the same
	/// archive template: a recharge-gated chance to hit every player/pet within range and in
	/// line of sight, EXCEPT the one the monster is currently fighting, with pets taking 5x.
	/// Real defaults: chance(110,3)=11%, delay {12,15} real seconds, range 8. Each subclass
	/// registers its FieldValues under its own key prefix (a FieldValue key must be unique
	/// per instance, see the #741 "chance"/"cooldown" collision).</summary>
	[ViewableClass]
	public abstract class AreaCurseAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue damage;
		private FieldValue range;
		private FieldValue spareCurrentTarget;
		private FieldValue petMultiplier;

		protected AreaCurseAbilityDef(string defname, string filename, int headerLine, string keyPrefix, double[] defaultDamage, double[] defaultCooldown)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField(keyPrefix + "Chance", new double[] { 110, 110 }, typeof(double[]));
			this.cooldown = this.InitTypedField(keyPrefix + "Cooldown", defaultCooldown, typeof(double[]));
			this.damage = this.InitTypedField(keyPrefix + "Damage", defaultDamage, typeof(double[]));
			this.range = this.InitTypedField(keyPrefix + "Range", (double) 8, typeof(double));
			//#797 user decision: faithful to the archive by default, but GM-switchable on .settings
			this.spareCurrentTarget = this.InitTypedField(keyPrefix + "SpareCurrentTarget", true, typeof(bool));
			this.petMultiplier = this.InitTypedField(keyPrefix + "PetMultiplier", (double) 5, typeof(double));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger once the recharge is up -
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

		/// <summary>How much life/mana/stamina each victim loses.</summary>
		public double[] Damage {
			get { return (double[]) this.damage.CurrentValue; }
			set { this.damage.CurrentValue = value; }
		}

		/// <summary>Radius in tiles.</summary>
		public double Range {
			get { return (double) this.range.CurrentValue; }
			set { this.range.CurrentValue = value; }
		}

		/// <summary>When true (archive), the character this monster is currently fighting is
		/// left alone - the curse only hits the bystanders.</summary>
		public bool SpareCurrentTarget {
			get { return (bool) this.spareCurrentTarget.CurrentValue; }
			set { this.spareCurrentTarget.CurrentValue = value; }
		}

		/// <summary>Damage multiplier against pets (archive: 5).</summary>
		public double PetMultiplier {
			get { return (double) this.petMultiplier.CurrentValue; }
			set { this.petMultiplier.CurrentValue = value; }
		}
	}

	/// <summary>e_npc_curse ("Dark Curse") - the archive's cleanest doc comment: "hurts all the
	/// enemies in the distance of 8 using dark and dirty magic". Real damage default {50,60}.</summary>
	[ViewableClass]
	public class NpcCurseAbilityDef : AreaCurseAbilityDef {
		public NpcCurseAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "npcCurse", new double[] { 50, 60 }, new double[] { 12, 15 }) {
		}
	}

	/// <summary>#797 e_npc_curse_mana - drains mana instead of life. Archive fallback amount {40,80}.</summary>
	[ViewableClass]
	public class NpcCurseManaAbilityDef : AreaCurseAbilityDef {
		public NpcCurseManaAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "npcCurseMana", new double[] { 40, 80 }, new double[] { 12, 15 }) {
		}
	}

	/// <summary>#797 e_npc_curse_stam - drains stamina instead of life. Archive fallback amount {40,80}.</summary>
	[ViewableClass]
	public class NpcCurseStamAbilityDef : AreaCurseAbilityDef {
		public NpcCurseStamAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine, "npcCurseStam", new double[] { 40, 80 }, new double[] { 12, 15 }) {
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int CurseEffect = 0x374A;

		private static NpcCurseAbilityDef npcCurseAbilityDef;
		internal static NpcCurseAbilityDef NpcCurseDef {
			get {
				if (npcCurseAbilityDef == null) {
					npcCurseAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_curse") as NpcCurseAbilityDef;
				}
				return npcCurseAbilityDef;
			}
		}

		private static NpcCurseManaAbilityDef npcCurseManaAbilityDef;
		internal static NpcCurseManaAbilityDef NpcCurseManaDef {
			get {
				if (npcCurseManaAbilityDef == null) {
					npcCurseManaAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_curse_mana") as NpcCurseManaAbilityDef;
				}
				return npcCurseManaAbilityDef;
			}
		}

		private static NpcCurseStamAbilityDef npcCurseStamAbilityDef;
		internal static NpcCurseStamAbilityDef NpcCurseStamDef {
			get {
				if (npcCurseStamAbilityDef == null) {
					npcCurseStamAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_curse_stam") as NpcCurseStamAbilityDef;
				}
				return npcCurseStamAbilityDef;
			}
		}

		private static readonly TagKey nextNpcCurseTimeTK = TagKey.Acquire("_npcNextCurseTime_");
		private static readonly TagKey nextNpcCurseManaTimeTK = TagKey.Acquire("_npcNextCurseManaTime_");
		private static readonly TagKey nextNpcCurseStamTimeTK = TagKey.Acquire("_npcNextCurseStamTime_");

		/// <summary>#npc_curse ("Dark Curse") - periodic area magic damage, same
		/// once-per-think-tick-while-fighting pattern as roundattack/parakrik.</summary>
		public static void TryApplyNpcCurse(NPC self) {
			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			TryApplyAreaCurse(self, NpcCurseDef, nextNpcCurseTimeTK,
				npcDef.NpcCurseChance, npcDef.NpcCurseCooldown, npcDef.NpcCurseDamage,
				delegate (Character target, double amount) {
					//bare DamageType.Magic has no resist "subtype" bit and throws - MagicMystical
					//is this engine's generic "magic, no specific element" bucket
					DamageManager.CauseDamage(self, target, DamageType.MagicMystical, amount);
					target.SysMessage("You feel a dark and mighty force draining your life!");
				});
		}

		/// <summary>#797 e_npc_curse_mana - same pulse, drains mana.</summary>
		public static void TryApplyNpcCurseMana(NPC self) {
			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			TryApplyAreaCurse(self, NpcCurseManaDef, nextNpcCurseManaTimeTK,
				npcDef.NpcCurseManaChance, npcDef.NpcCurseManaCooldown, npcDef.NpcCurseManaDamage,
				delegate (Character target, double amount) {
					target.Mana = (short) Math.Max(0, target.Mana - (int) Math.Round(amount));
					target.SysMessage("You feel a dark and mighty force draining your magical energy!");
				});
		}

		/// <summary>#797 e_npc_curse_stam - same pulse, drains stamina.</summary>
		public static void TryApplyNpcCurseStam(NPC self) {
			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			TryApplyAreaCurse(self, NpcCurseStamDef, nextNpcCurseStamTimeTK,
				npcDef.NpcCurseStamChance, npcDef.NpcCurseStamCooldown, npcDef.NpcCurseStamDamage,
				delegate (Character target, double amount) {
					target.Stam = (short) Math.Max(0, target.Stam - (int) Math.Round(amount));
					target.SysMessage("You feel a dark and mighty force draining your stamina!");
				});
		}

		private static bool TryApplyAreaCurse(NPC self, AreaCurseAbilityDef def, TagKey nextTimeTK,
				int[] chanceOverride, int[] cooldownOverride, int[] damageOverride, Action<Character, double> apply) {
			if (!HasAbilityDef(self, def)) {
				return false;
			}
			object nextTime = self.GetTag(nextTimeTK);
			if ((nextTime is double) && (Globals.TimeInSeconds < (double) nextTime)) {
				return false;
			}
			self.SetTag(nextTimeTK, Globals.TimeInSeconds + RollTunable(cooldownOverride, def.Cooldown));

			if (!RollChance(RollTunable(chanceOverride, def.Chance))) {
				return false;
			}

			Character spared = null;
			if (def.SpareCurrentTarget) {
				NPCAiTimer ai = NPCAiTimer.GetFor(self);
				spared = (ai == null) ? null : ai.Target;
			}
			double amount = RollTunable(damageOverride, def.Damage);
			foreach (Character target in CharsAround(self.GetMap(), self.X, self.Y, (int) def.Range)) {
				if ((target == self) || (target == spared) || !IsPlayerOrPetTarget(target)) {
					continue;
				}
				if (!self.CanSeeLOS(target).Allow) {
					continue;
				}
				EffectFactory.StationaryEffect(target, CurseEffect, 6, 15);
				apply(target, target.IsPlayerForCombat ? amount : amount * def.PetMultiplier);
			}
			return true;
		}
	}
}
