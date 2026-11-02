using System;
using System.Globalization;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - helpers for Tests/Protocol/BardTests.cs.</summary>
	public static class BardTestHelpers {
		private static NPC Npc(string uid) {
			return (NPC) Thing.UidGetCharacter(int.Parse(uid, CultureInfo.InvariantCulture));
		}

		private static string[] Words(ScriptArgs sa) {
			return sa.Args.Trim('"', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
		}

		[SteamFunction]
		public static void TestBardPrepare(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			self.Account.PLevel = 1;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			int music = int.Parse(words[0], CultureInfo.InvariantCulture), bard = int.Parse(words[1], CultureInfo.InvariantCulture);
			self.SetRealSkillValue((int) SkillName.Musicianship, music);
			self.SetRealSkillValue((int) SkillName.Discordance, bard);
			self.SetRealSkillValue((int) SkillName.Peacemaking, bard);
			self.SetRealSkillValue((int) SkillName.Provocation, bard);
			Console.WriteLine("BARD_PREPARED: " + self.Uid);
		}

		[SteamFunction]
		public static void TestBardInstrument(Character self) {
			Item drum = (Item) ThingDef.GetByDefname("i_drum").Create(self.Backpack);
			MusicalDef def = (MusicalDef) drum.Def;
			Console.WriteLine("BARD_INSTRUMENT: #" + drum.Uid + " good=" + def.SuccessSound + " bad=" + def.FailureSound);
		}

		/// <summary>Even no skill plays well some of the time, so enough goes for the fail to come.</summary>
		[SteamFunction]
		public static void TestBardPlayMany(Character self) {
			Item drum = (Item) ThingDef.GetByDefname("i_drum").Create(self.Backpack);
			for (int i = 0; i < 100; i++) {
				drum.On_DClick(self);
				SkillSequenceArgs.AbortSkill(self);
			}
			Console.WriteLine("BARD_PLAYED: done");
		}

		[SteamFunction]
		public static void TestBardAnimal(Character self, ScriptArgs sa) {
			string[] words = Words(sa);
			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(self.X + 1, self.Y, self.Z, self.M);
			npc.ExperienceReward = int.Parse(words[0], CultureInfo.InvariantCulture);
			npc.SetRealSkillValue((int) SkillName.Discordance, 0);
			npc.SetRealSkillValue((int) SkillName.Peacemaking, 0);
			npc.SetRealSkillValue((int) SkillName.Provocation, 0);
			if (words.Length > 1) {
				npc.SetRealSkillValue((int) SkillName.Discordance, 100);
				npc.SetRealSkillValue((int) SkillName.Peacemaking, 2000);
				npc.SetRealSkillValue((int) SkillName.Provocation, 100);
			}
			npc.Str = 100;
			npc.MaxHits = 100;
			npc.Hits = 50;
			npc.SetRealSkillValue((int) SkillName.Wrestling, 1000);
			Console.WriteLine("BARD_ANIMAL: npc=#" + npc.Uid);
		}

		[SteamFunction]
		public static void TestBardReport(Character self, ScriptArgs sa) {
			NPC npc = Npc(Words(sa)[0]);
			NPCAiTimer ai = NPCAiTimer.GetFor(npc);
			Console.WriteLine("BARD_REPORT: str=" + npc.Str + " maxhits=" + npc.MaxHits + " hits=" + npc.Hits + " wrestling=" + npc.GetSkill(SkillName.Wrestling)
				+ " weak=" + npc.HasPlugin(DiscordanceSkillDef.effectPluginKey) + " asleep=" + PeacemakingSkillDef.IsAsleep(npc) + " target=" + (ai?.Target?.Name ?? "none"));
		}

		[SteamFunction]
		public static void TestBardHurry(Character self, ScriptArgs sa) {
			NPC npc = Npc(Words(sa)[0]);
			BoundTimer sleep = npc.GetTimer(PeacefulSleepTimer.Key);
			if (sleep != null) {
				sleep.DueInSeconds = 0.1;
			}
			Plugin weak = npc.GetPlugin(DiscordanceSkillDef.effectPluginKey);
			if (weak != null) {
				weak.Delete();
			}
			Console.WriteLine("BARD_HURRIED: done");
		}

		[SteamFunction]
		public static void TestBardHurt(Character self) {
			DamageManager.CauseDamage(self, self, DamageType.Physical, 1);
			Console.WriteLine("BARD_HURT: done");
		}

		[SteamFunction]
		public static void TestBardSkill(Character self, ScriptArgs sa) {
			self.SelectSkill((SkillName) int.Parse(Words(sa)[0], CultureInfo.InvariantCulture));
			Console.WriteLine("BARD_SKILL: done");
		}
	}
}
