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
using System.Collections.Generic;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Real-user-reported follow-up - e_npc_summoning ("Summoner"). Real defaults: chance 50
	/// (5%), cooldown 30s, summoned-creature lifetime 7200s (2h). The real archive's own
	/// per-species summon LIST (`tag.npc_summoning_char[i]`/`_count`) is recovered as
	/// NPCDef.SummonedCreatures - same list-of-NPCDef shape as the already-shipped
	/// NPCDef.Spells (#564).</summary>
	[ViewableClass]
	public class SummonerAbilityDef : PassiveAbilityDef {
		private FieldValue chance;
		private FieldValue cooldown;
		private FieldValue summonLifetime;

		public SummonerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("summonerChance", new double[] { 50, 50 }, typeof(double[]));
			this.cooldown = this.InitTypedField("summonerCooldown", new double[] { 30, 30 }, typeof(double[]));
			this.summonLifetime = this.InitTypedField("summonerLifetime", new double[] { 7200, 7200 }, typeof(double[]));
		}

		/// <summary>Permille (0-1000) {min,max} chance range to trigger on each swing attempt -
		/// deliberately shadows AbilityDef.Chance (different shape/scale) rather than reusing it.</summary>
		public new double[] Chance {
			get { return (double[]) this.chance.CurrentValue; }
			set { this.chance.CurrentValue = value; }
		}

		/// <summary>Recharge interval {min,max} range between summon waves, real seconds -
		/// deliberately shadows AbilityDef.Cooldown (a single scalar, different shape) rather
		/// than reusing it.</summary>
		public new double[] Cooldown {
			get { return (double[]) this.cooldown.CurrentValue; }
			set { this.cooldown.CurrentValue = value; }
		}

		/// <summary>{min,max} seconds a summoned reinforcement survives before auto-despawning.</summary>
		public double[] SummonLifetime {
			get { return (double[]) this.summonLifetime.CurrentValue; }
			set { this.summonLifetime.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private static SummonerAbilityDef summonerAbilityDef;
		internal static SummonerAbilityDef SummonerDef {
			get {
				if (summonerAbilityDef == null) {
					summonerAbilityDef = AbstractScript.GetByDefname("a_tevent_e_npc_summoning") as SummonerAbilityDef;
				}
				return summonerAbilityDef;
			}
		}

		private static readonly TagKey nextSummoningTimeTK = TagKey.Acquire("_npcNextSummoningTime_");
		private static readonly TimerKey summonedCreatureDespawnTimerKey = TimerKey.Acquire("_npcSummonedCreatureDespawn_");

		/// <summary>#npc_summoning ("Summoner") - periodic (once-per-think-tick-while-fighting,
		/// same pattern as roundattack/parakrik/npc_curse) chance to spawn reinforcements. Real
		/// archive spawns ONE INSTANCE OF EVERY configured chardef per proc (a loop over all
		/// indices up to a count), not a single random pick - NPCDef.SummonedCreatures mirrors
		/// that shape directly. No-op if the NPCDef never configured any (same "incomplete
		/// content defaults permissive/inert" convention used elsewhere). Spawned reinforcements
		/// are autonomous Hostile (no ControlMaster) - same choice SummonSpellDef's own Hostile
		/// branch already makes for a "popular kill-all" summon - and despawn automatically via
		/// the same generic SummonSpellDef.SummonDespawnTimer the Mage summon spells use.</summary>
		public static void TryApplySummoning(NPC self) {
			SummonerAbilityDef def = SummonerDef;
			if (def == null) {
				return;
			}
			Ability ability;
			if (!self.HasAbility(def, out ability)) {
				return;
			}
			NPCDef npcDef = TuningDef(self); //#1022 a form's while Darkmoon has one
			List<NPCDef> toSummon = npcDef.SummonedCreatures;
			if ((toSummon == null) || (toSummon.Count == 0)) {
				return;
			}
			if (Globals.TimeInSeconds < NextSummoningTime(self)) {
				return;
			}

			double cooldown = RollTunable(npcDef.SummoningCooldown, def.Cooldown);
			SetNextSummoningTime(self, Globals.TimeInSeconds + cooldown);

			double chance = RollTunable(npcDef.SummoningChance, def.Chance);
			if (!RollChance(chance)) {
				return;
			}

			double lifetimeSecs = RollTunable(npcDef.SummoningLifetime, def.SummonLifetime);
			foreach (NPCDef reinforcementDef in toSummon) {
				NPC reinforcement = (NPC) reinforcementDef.Create(self.X, self.Y, self.Z, self.M);
				reinforcement.AggroMode = AggroMode.Hostile;
				SummonSpellDef.MarkAsSummonedCreature(reinforcement);
				reinforcement.AddTimer(summonedCreatureDespawnTimerKey, new SummonSpellDef.SummonDespawnTimer())
					.DueInSpan = TimeSpan.FromSeconds(lifetimeSecs);
			}
		}

		private static double NextSummoningTime(NPC self) {
			object tag = self.GetTag(nextSummoningTimeTK);
			return tag is double ? (double) tag : 0;
		}

		private static void SetNextSummoningTime(NPC self, double value) {
			self.SetTag(nextSummoningTimeTK, value);
		}
	}
}
