using System;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_npc_summonKiller (summonKiller.scp): its blows hit summons twice as hard.</summary>
	[ViewableClass]
	public class SummonKillerAbilityDef : PassiveAbilityDef {
		private FieldValue boostPercent;

		public SummonKillerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.boostPercent = this.InitTypedField("boostPercent", (double) 100, typeof(double));
		}

		public double BoostPercent {
			get { return (double) this.boostPercent.CurrentValue; }
			set { this.boostPercent.CurrentValue = value; }
		}
	}

	/// <summary>#1022 e_elem_terror (new_events.scp): dying, it blasts everyone who fought it, by their distance. The sound
	/// when it sees a player is left out.</summary>
	[ViewableClass]
	public class ElemTerrorAbilityDef : PassiveAbilityDef {
		private FieldValue deathDamage;

		public ElemTerrorAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.deathDamage = this.InitTypedField("deathDamage", new double[] { 50, 50 }, typeof(double[]));
		}

		public double[] DeathDamage {
			get { return (double[]) this.deathDamage.CurrentValue; }
			set { this.deathDamage.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		private const int ExplosionSound = 0x207;

		internal static SummonKillerAbilityDef SummonKillerDef { get { return Lookup<SummonKillerAbilityDef>("a_tevent_e_npc_summonkiller"); } }
		internal static ElemTerrorAbilityDef ElemTerrorDef { get { return Lookup<ElemTerrorAbilityDef>("a_tevent_e_elem_terror"); } }
		private static AbilityDef SummonFrenzyDef { get { return Lookup<AbilityDef>("a_tevent_e_summonfrenzy"); } }

		private static double DamageAgainstSummons(Character attacker, Character defender, double damage) {
			SummonKillerAbilityDef def = SummonKillerDef;
			if (HasAbilityDef(attacker, def) && SummonSpellDef.IsSummonedCreature(defender)) {
				return damage + ((damage * def.BoostPercent) / 100);
			}
			return damage;
		}

		/// <summary>e_summonfrenzy: a masterless summon that hits it loses the will to fight, and so does it.</summary>
		internal static void TrySummonFrenzy(NPC self, Character attacker) {
			if (HasAbilityDef(self, SummonFrenzyDef) && (attacker is NPC summon) && SummonSpellDef.IsSummonedCreature(summon) && (summon.ControlMaster == null)) {
				NPCAiTimer.GetFor(summon)?.Disengage();
				NPCAiTimer.GetFor(self)?.Disengage();
				FightMemory.Forget(summon);
				self.Say("all release");
			}
		}

		internal static void OnMonsterDeath(NPC self, Character killedBy) {
			OnSummonerDeath(self, killedBy); //#1022 the summoning bosses
			OnDeathBossDeath(self); //#1022 the dying bosses
			OnHaldaMolleDeath(self); //#1022 e_warniak_drop, e_warniak_degrade
			ElemTerrorAbilityDef def = ElemTerrorDef;
			if (!HasAbilityDef(self, def)) {
				return;
			}
			int damage = (int) RollTunable((self.Def as NPCDef)?.DeathDamage, def.DeathDamage);
			foreach (Character fighter in LevelingSystem.FindFighters(self, killedBy, false)) {
				if (self.CanSeeLOS(fighter).Allow) {
					int distance = Point2D.GetSimpleDistance(self, fighter);
					EffectFactory.StationaryEffect(fighter, BlowUpEffect, 6, 15);
					fighter.Sound(ExplosionSound);
					Character.SetHitsBy(fighter, (short) Math.Max(0, fighter.Hits - ((distance > 0) ? (damage / distance) : damage)), self); //#1464 - killed by the blast
				}
			}
		}
	}
}
