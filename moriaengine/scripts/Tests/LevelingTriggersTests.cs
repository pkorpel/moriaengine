using System;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1416 - @killCredit on each credited fighter (a monster's death and a player's), @gainExperience in
	/// LevelingSystem.AddExperience (sees the base and the amount, may change it or cancel). Protocol side:
	/// Tests/Protocol/LevelingTriggersTests.cs.</summary>
	public static class LevelingTriggersTests {
		private static readonly PluginKey probeKey = PluginKey.Acquire("_killCreditProbe_");
		internal static int credits, gains;
		internal static Character creditVictim;
		internal static int creditShare = -1, gainBase = -1, gainAmount = -1;
		internal static int addToGain;
		internal static bool cancelGain;

		private static void Check(string name, bool ok) {
			Console.WriteLine("KILLCREDIT_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestLevelingTriggers(Character self) {
			Player player = (Player) self;
			int plevel = self.Account.PLevel;
			int experience = player.Experience;
			KillCreditProbePlugin probe = (KillCreditProbePlugin) KillCreditProbePluginDef.instance.Create();
			NPC orc = null, killer = null;
			try {
				self.Account.PLevel = 1;
				MovementSpeedTests.TestTeleportToOpenGround(self);
				VerificationTestUtils.CleanupNearbyNpcs(self, 10);
				credits = 0;
				gains = 0;
				self.AddTriggerGroup(SingletonScript<E_KillCreditProbe>.Instance);
				self.AddPlugin(probeKey, probe);

				//a monster's death: the fighter gains its share, raising @gainExperience, then @killCredit
				orc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(self.X + 1, self.Y, self.Z, self.M);
				orc.ExperienceReward = 80;
				addToGain = 7;
				int before = player.Experience;
				LevelingSystem.ShareMonsterExperience(orc, self);
				int got = player.Experience - before;
				Console.WriteLine("KILLCREDIT_MONSTER: credits=" + credits + " share=" + creditShare + " plugin=" + probe.heard
					+ " gains=" + gains + " base=" + gainBase + " amount=" + gainAmount + " got=" + got);
				Check("monsterKillCredit", (credits == 1) && (creditVictim == orc) && (creditShare == 80) && (probe.heard == 1)
					&& (probe.victim == orc) && (probe.share == 80));
				Check("gainExperienceChangesIt", (gains == 1) && (gainBase == 80) && (gainAmount == 80) && (got == 87));

				//a cancelled gain adds nothing
				addToGain = 0;
				cancelGain = true;
				before = player.Experience;
				LevelingSystem.AddExperience(player, 50);
				cancelGain = false;
				Check("gainExperienceCancelled", (gains == 2) && (gainBase == 50) && (player.Experience == before));

				//a monster worth nothing still credits its fighter, with 0, and pays nothing
				orc.ExperienceReward = 0;
				int gainsBefore = gains;
				LevelingSystem.ShareMonsterExperience(orc, self);
				Check("worthlessKillCredit", (credits == 2) && (creditShare == 0) && (gains == gainsBefore));

				//a player's death: the killer (an NPC of no realm here) is credited with its share, though an NPC is given nothing
				killer = (NPC) ((NPCDef) ThingDef.GetByDefname("c_orc")).Create(self.X - 1, self.Y, self.Z, self.M);
				killer.AddTriggerGroup(SingletonScript<E_KillCreditProbe>.Instance);
				player.Experience = 5000;
				int creditsBefore = credits;
				LevelingSystem.ApplyDeathExperienceLoss(player, killer);
				int lost = 5000 - player.Experience;
				Console.WriteLine("KILLCREDIT_PLAYER: credits=" + (credits - creditsBefore) + " victim=" + creditVictim + " share=" + creditShare
					+ " lost=" + lost);
				Check("playerKillCredit", (credits == creditsBefore + 1) && (creditVictim == self) && (lost > 0) && (creditShare == lost));

				//dying again within the immunity loses nothing, but the killer is still credited, with 0 (Erebor's addfame 0)
				LevelingSystem.ApplyDeathExperienceLoss(player, killer);
				Check("immuneDeathCredit", (credits == creditsBefore + 2) && (creditShare == 0) && (player.Experience == 5000 - lost));
			} finally {
				addToGain = 0;
				cancelGain = false;
				self.RemoveTriggerGroup(SingletonScript<E_KillCreditProbe>.Instance);
				self.DeletePlugin(probeKey);
				if ((orc != null) && !orc.IsDeleted) {
					orc.Delete();
				}
				if ((killer != null) && !killer.IsDeleted) {
					killer.Delete();
				}
				player.Experience = experience;
				self.Account.PLevel = (byte) plevel;
			}
			Console.WriteLine("KILLCREDIT_DONE");
		}
	}

	public class E_KillCreditProbe : CompiledTriggerGroup {
		public void On_KillCredit(Character self, KillCreditArgs args) {
			LevelingTriggersTests.credits++;
			LevelingTriggersTests.creditVictim = args.victim;
			LevelingTriggersTests.creditShare = args.share;
		}

		public TriggerResult On_GainExperience(Character self, GainExperienceArgs args) {
			LevelingTriggersTests.gains++;
			LevelingTriggersTests.gainBase = args.BaseAmount;
			LevelingTriggersTests.gainAmount = args.Amount;
			if (LevelingTriggersTests.cancelGain) {
				return TriggerResult.Cancel;
			}
			args.Amount += LevelingTriggersTests.addToGain;
			return TriggerResult.Continue;
		}
	}

	public class KillCreditProbePluginDef : PluginDef {
		public static readonly KillCreditProbePluginDef instance =
			(KillCreditProbePluginDef) new KillCreditProbePluginDef("p_killCredit_probe", "C# scripts", -1).Register();

		public KillCreditProbePluginDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		protected override Plugin CreateImpl() {
			return new KillCreditProbePlugin();
		}

		public new static void Bootstrap() {
			RegisterPluginDef(typeof(KillCreditProbePluginDef), typeof(KillCreditProbePlugin));
		}
	}

	/// <summary>Records @killCredit. Never saved.</summary>
	[DeepCopyableClass]
	[SaveableClass]
	public class KillCreditProbePlugin : Plugin {
		internal int heard;
		internal Character victim;
		internal int share;

		[DeepCopyImplementation]
		public KillCreditProbePlugin(KillCreditProbePlugin copyFrom)
			: base(copyFrom) {
		}

		[LoadingInitializer]
		public KillCreditProbePlugin() {
		}

		public void On_KillCredit(KillCreditArgs args) {
			this.heard++;
			this.victim = args.victim;
			this.share = args.share;
		}
	}
}
