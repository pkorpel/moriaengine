using System;

namespace SteamEngine.CompiledScripts {

	/// <summary>#951 - server-side helpers for Tests/Protocol/CommandPermissionTests.cs.</summary>
	public static class CommandPermissionTests {
		private static Player tested;

		/// <summary>The caller, an admin account, sets another account's highest plevel.</summary>
		[SteamFunction]
		public static void TestPermissionPlevel(Character self, ScriptArgs sa) {
			tested.Account.Promote(Convert.ToInt32(sa.Argv[0]));
			Console.WriteLine("PERMISSION_PLEVEL: " + tested.Account.MaxPLevel);
		}

		[SteamFunction]
		public static void TestPermissionMe(Character self) {
			tested = (Player) self;
			Console.WriteLine("PERMISSION_ME: " + self.Account.MaxPLevel);
		}

		[SteamFunction]
		public static void TestPermissionReport(Character self) {
			Console.WriteLine("PERMISSION_REPORT: level=" + tested.Level + " rank=" + tested.SkillRank + " name=" + tested.Name
				+ " plevel=" + tested.Account.MaxPLevel + " exp=" + tested.Experience);
		}

		[SteamFunction]
		public static void TestPermissionCloseTestCommands(Character self) {
			E_CommandsAuthorization_Global.testCommandsOpen = false;
			Console.WriteLine("PERMISSION_CLOSED:");
		}

		/// <summary>Anything a player could do with this writes this line.</summary>
		[SteamFunction]
		public static void TestPermissionProbe(Character self) {
			Console.WriteLine("PERMISSION_PROBE: " + self.Name);
		}

		/// <summary>A rank from before #951 sits in a tag, on someone who has a profession.</summary>
		[SteamFunction]
		public static void TestRankOldSave(Character self) {
			Player player = (Player) self;
			player.Profession = (ProfessionDef) ProfessionDef.GetByDefname("profession_warrior");
			player.SetTag(TagKey.Acquire("_skillRank_"), SkillRank.Master);
			Console.WriteLine("RANK_OLD: read=" + player.SkillRank + " plugin=" + player.ProfessionPlugin.SkillRank
				+ " tagLeft=" + (player.GetTag(TagKey.Acquire("_skillRank_")) != null));
		}

		[SteamFunction]
		public static void TestRankChangeProfession(Character self) {
			Player player = (Player) self;
			player.Profession = (ProfessionDef) ProfessionDef.GetByDefname("profession_mage");
			string changed = player.SkillRank + "/" + player.ProfessionName;
			player.Profession = null;
			string none = player.SkillRank.ToString();
			player.Profession = (ProfessionDef) ProfessionDef.GetByDefname("profession_warrior");
			Console.WriteLine("RANK_CHANGED: changed=" + changed + " none=" + none + " back=" + player.SkillRank + " title=" + player.Title);
		}

		[SteamFunction]
		public static void TestRankSave(Character self) {
			((Player) self).SkillRank = SkillRank.Grandmaster;
			Console.WriteLine("RANK_SAVE: " + ((Player) self).ProfessionPlugin.SkillRank);
		}

		[SteamFunction]
		public static void TestRankLoaded(Character self) {
			foreach (AbstractCharacter ch in self.Account.Characters) {
				Player player = ch as Player;
				if ((player != null) && (player.ProfessionPlugin != null)) {
					Console.WriteLine("RANK_LOADED: " + player.Name + " " + player.ProfessionPlugin.SkillRank);
				}
			}
		}
	}
}
