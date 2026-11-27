using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1558 - chain probes for a death: a trigger group on the killer and the victim records which death triggers fire,
	/// in what order, and what a refused death leaves behind. Written against the code before #1558, then the rework changed
	/// what Tests/Protocol/TriggerChainDeathTests.cs expects. Before: a killing blow was it:denyDeath,it:death,me:killCredit,
	/// me:kill; a refused death kept the hits from before the blow (refusedHits=30) while CauseDamage still reported the whole
	/// intended blow (10000, the hits it would have gone down to), and a refused .kill left the victim at full hits.</summary>
	public static class TriggerChainDeathTests {
		internal static readonly List<string> log = new List<string>();
		internal static Thing me;

		internal static void Log(Thing who, string what) {
			log.Add(((who == me) ? "me" : "it") + ":" + what);
		}

		private static void Check(string name, bool ok) {
			Console.WriteLine("CHAIND_CHECK: " + name + "=" + ok);
		}

		private static string Logged() {
			string s = string.Join(",", log);
			log.Clear();
			return s;
		}

		private static NPC Victim(Character self, int dx) {
			NPC rabbit = (NPC) ThingDef.GetByDefname("c_rabbit").Create(self.X + dx, self.Y, self.Z, self.M);
			NPCAiTimer.Freeze(rabbit, 300);
			rabbit.AddTriggerGroup(SingletonScript<E_ChainProbeDeath>.Instance);
			return rabbit;
		}

		private static void Ready(Character self) {
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 10);
			self.Hits = self.MaxHits;
			me = self;
			log.Clear();
			E_ChainProbeDeath.refuse = false;
			E_ChainProbeDeath.setHits = 0;
			self.AddTriggerGroup(SingletonScript<E_ChainProbeDeath>.Instance);
		}

		private static void Done(Character self, params NPC[] victims) {
			self.RemoveTriggerGroup(SingletonScript<E_ChainProbeDeath>.Instance);
			E_ChainProbeDeath.refuse = false;
			E_ChainProbeDeath.setHits = 0;
			foreach (NPC victim in victims) {
				if (!victim.IsDeleted) {
					victim.Delete();
				}
			}
		}

		/// <summary>The player's killing blow on a rabbit: the death triggers on both, in order.</summary>
		[SteamFunction]
		public static void TestChainDeath(Character self) {
			Ready(self);
			NPC rabbit = Victim(self, 1);
			try {
				DamageManager.CauseDamage(self, rabbit, DamageType.Irresistable, 10000);
				string died = Logged();
				Console.WriteLine("CHAIND_DIED: " + died + " deleted=" + rabbit.IsDeleted + " |");
				Check("deathOrder", rabbit.IsDeleted && (died == "it:denyDeath,it:death,me:killCredit,me:kill"));
			} finally {
				Done(self, rabbit);
			}
			Console.WriteLine("CHAIND_DEATH_DONE");
		}

		/// <summary>A refused death: by a blow (the hits left and the damage reported), with a handler setting the hits, and
		/// by .kill (InstaKill).</summary>
		[SteamFunction]
		public static void TestChainRefusedDeath(Character self) {
			Ready(self);
			NPC blown = Victim(self, 1);
			NPC healed = Victim(self, 2);
			NPC killed = Victim(self, 3);
			try {
				E_ChainProbeDeath.refuse = true;
				blown.Hits = 30;
				int reported = DamageManager.CauseDamage(self, blown, DamageType.Irresistable, 10000);
				string refused = Logged();
				bool alive = !blown.IsDeleted && !blown.Flag_Dead;
				Console.WriteLine("CHAIND_REFUSED: " + refused + " alive=" + alive + " refusedHits=" + blown.Hits + " reported=" + reported + " |");
				Check("refusedIsOnlyTheDeny", alive && (refused == "it:denyDeath"));
				Check("refusedLeavesOneHit", alive && (blown.Hits == 1));
				Check("refusedReportsRealDamage", reported == 29);

				E_ChainProbeDeath.setHits = 20;
				healed.Hits = 30;
				DamageManager.CauseDamage(self, healed, DamageType.Irresistable, 10000);
				Logged();
				Console.WriteLine("CHAIND_HEALED: hits=" + healed.Hits + " |");
				Check("handlersHitsStay", !healed.IsDeleted && (healed.Hits == 20));
				E_ChainProbeDeath.setHits = 0;

				int full = killed.Hits;
				killed.InstaKill();
				Logged();
				Console.WriteLine("CHAIND_KILLED: full=" + full + " hits=" + killed.Hits + " |");
				Check("refusedKillLeavesOneHit", !killed.IsDeleted && (killed.Hits == 1));
			} finally {
				Done(self, blown, healed, killed);
			}
			Console.WriteLine("CHAIND_REFUSED_DONE");
		}
	}

	/// <summary>On the killer and the victim: the death triggers. `refuse` cancels the victim's @denyDeath; `setHits` makes the
	/// refusing handler set the victim's hits itself.</summary>
	public partial class E_ChainProbeDeath : CompiledTriggerGroup {
		internal static bool refuse;
		internal static short setHits;

		public TriggerResult On_DenyDeath(Character self, ScriptArgs args) {
			TriggerChainDeathTests.Log(self, "denyDeath");
			if (refuse && (self != TriggerChainDeathTests.me)) {
				if (setHits > 0) {
					self.Hits = setHits;
				}
				return TriggerResult.Cancel;
			}
			return TriggerResult.Continue;
		}
		public void On_Death(Character self, ScriptArgs args) { TriggerChainDeathTests.Log(self, "death"); }
		public void On_Kill(Character self, ScriptArgs args) { TriggerChainDeathTests.Log(self, "kill"); }
		public void On_KillCredit(Character self, ScriptArgs args) { TriggerChainDeathTests.Log(self, "killCredit"); }
	}
}
