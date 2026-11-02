using System;
using System.Collections.Generic;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_npc_summoner (new_events.scp), not e_npc_summoning: one blow in 20 the monster calls its list around it,
	/// for 20 s, and they go for whoever it's fighting.</summary>
	[ViewableClass]
	public class BattleSummonerAbilityDef : PassiveAbilityDef {
		private FieldValue summonOneIn;
		private FieldValue lifetime;

		public BattleSummonerAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.summonOneIn = this.InitTypedField("summonOneIn", 20, typeof(int));
			this.lifetime = this.InitTypedField("battleSummonLifetime", (double) 20, typeof(double));
		}

		public int SummonOneIn {
			get { return (int) this.summonOneIn.CurrentValue; }
			set { this.summonOneIn.CurrentValue = value; }
		}

		public double BattleSummonLifetime {
			get { return (double) this.lifetime.CurrentValue; }
			set { this.lifetime.CurrentValue = value; }
		}
	}

	public static partial class MonsterSpecialAttacks {
		internal static BattleSummonerAbilityDef BattleSummonerDef { get { return Lookup<BattleSummonerAbilityDef>("a_tevent_e_npc_summoner"); } }

		internal static void TryBattleSummon(NPC self, Character attacker) {
			BattleSummonerAbilityDef def = BattleSummonerDef;
			List<NPCDef> list = ((NPCDef) self.Def).BattleSummons;
			if (!HasAbilityDef(self, def) || (list == null) || !OneIn(def.SummonOneIn)) {
				return;
			}
			self.Say("Heru Ruin Val");
			foreach (NPCDef summonDef in list) {
				NPC summon = (NPC) summonDef.Create((ushort) (self.X + Globals.dice.Next(-1, 2)), (ushort) (self.Y + Globals.dice.Next(-1, 2)), self.Z, self.M);
				SummonSpellDef.MarkAsSummonedCreature(summon);
				summon.AddTimer(summonedCreatureDespawnTimerKey, new SummonSpellDef.SummonDespawnTimer()).DueInSpan = TimeSpan.FromSeconds(def.BattleSummonLifetime);
				NPCAiTimer.Attack(summon, attacker);
			}
		}
	}
}
