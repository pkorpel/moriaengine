using System;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#902 - server-side helpers for Tests/Protocol/SharedExperienceTests.cs.</summary>
	public static class SharedExperienceTests {
		private static NPC monster;
		private static Player first;

		/// <summary>A plain level 5 player at the spot everyone in the test shares, in a realm and with experience.</summary>
		[SteamFunction]
		public static void TestExpReady(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string realm = sa.Args.Trim().Trim('"');
			first = first ?? player;
			player.P(first.P());
			player.Realm = (realm == "M") ? Realm.Mordor : Realm.Gondor;
			player.Level = 5;
			player.Experience = 1000;
			Console.WriteLine("EXP_READY: uid=" + (uint) self.FlaggedUid + " end");
		}

		/// <summary>The test's monster, worth 1000. "cap" keeps the 100 a Gondor or Mordor area pays at most.</summary>
		[SteamFunction]
		public static void TestExpMonster(Character self, ScriptArgs sa) {
			string mode = sa.Args.Trim().Trim('"');
			LevelingSettings.instance.monsterExpCap = (mode == "cap") ? 100 : 0;
			first.P(TownSetup.CityArea("Minas_Tirith").P);
			self.P(first.P());
			monster = (NPC) ThingDef.GetByDefname("c_a_jackrabbit").Create(first.P());
			monster.ExperienceReward = 1000;
			Console.WriteLine("EXP_MONSTER: done");
		}

		[SteamFunction]
		public static void TestExpFight(Character self) {
			FightMemory.Record(monster, self);
			Console.WriteLine("EXP_FOUGHT: done");
		}

		[SteamFunction]
		public static void TestExpKill(Character self) {
			monster.CauseDeath(self);
			Console.WriteLine("EXP_KILLED: done");
		}

		[SteamFunction]
		public static void TestExpParty(Character self) {
			Party party = Party.GetParty(first) ?? PartyDef.NewParty(first);
			if (self != first) {
				RolesManagement.Assign(self, party);
			}
			Console.WriteLine("EXP_PARTY: done");
		}

		[SteamFunction]
		public static void TestExpReport(Character self) {
			Player player = (Player) self;
			Console.WriteLine("EXP_REPORT: exp=" + player.Experience + " end");
		}

		/// <summary>I fought `first`, and I'm reading what `first` paying for it left.</summary>
		[SteamFunction]
		public static void TestExpFightFirst(Character self) {
			FightMemory.Record(first, self);
			Console.WriteLine("EXP_FOUGHT: done");
		}

		[SteamFunction]
		public static void TestExpKillFirst(Character self) {
			first.CauseDeath(self);
			Console.WriteLine("EXP_KILLED: done");
		}
	}
}
