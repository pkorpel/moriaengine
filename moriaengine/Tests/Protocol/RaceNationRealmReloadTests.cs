using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #434: the user reported that after #410/#414 (Race+Nation and Realm
	/// reworked into RaceNationPlugin/RealmPlugin), a real save file correctly shows both
	/// plugins installed with real enum values, but the in-game .info dialog shows Race/Nation
	/// as "None" and Realm as "Neutral" - and the plugin list in .info doesn't show
	/// RaceNationPlugin/RealmPlugin at all (only ProfessionPlugin).
	///
	/// EnumTagPersistenceTests (#407/#410) only ever inspected the SAVE FILE's contents - no
	/// existing test anywhere in the suite actually reloads a character from a real save and
	/// re-reads a property. This test closes that gap directly: boots a server, sets Race/
	/// Nation (which cascades Realm), saves for real, kills the process, boots a SECOND server
	/// pointed at the SAME save (GameServerPairFixture's preserveSaves:true, added for this
	/// test), reselects the SAME character (TestGameClient.SendSelectCharacter, also added for
	/// this test - no prior test ever needed to resume an EXISTING character after a restart),
	/// and re-reads Race/Nation/Realm via the real property getters.
	///
	/// Own isolated collection, same reasoning as EnumTagPersistenceTests/
	/// DefPersistenceRegressionTests - this manages its own fixture instances directly (TWO of
	/// them, sequentially) rather than via ICollectionFixture's single-instance-per-collection
	/// model, since no existing test needed more than one boot.
	/// </summary>
	[Collection(RaceNationRealmReloadCollection.Name)]
	public class RaceNationRealmReloadTests {
		private const string Account = "racenationrealmreloadtest";
		private const string Password = "racenationrealmreloadtest";
		private const string CharName = "ReloadChecker";

		[Fact]
		public void RaceNationAndRealmSurviveARealServerRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture1, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(CharName);

					int mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestSetRaceAndNation");
					Assert.True(fixture1.WaitForGameOutputSince("RACE_NATION_SET:", mark, TimeSpan.FromSeconds(10)));
					// Confirm the live, same-session read-after-write works BEFORE the restart -
					// isolates whether a failure below is really about reload, not a regression
					// in the live path RaceTests.cs already covers.
					string setLine = LastLineContaining(fixture1.GameOutputSnapshot(), "RACE_NATION_SET:");
					Assert.Contains("race=Dwarf", setLine);
					Assert.Contains("nation=Ereboran", setLine);
					Assert.Contains("realm=Gondor", setLine);

					mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestTriggerGlobalSave");
					Assert.True(fixture1.WaitForGameOutputSince("TEST_SAVE: done", mark, TimeSpan.FromSeconds(30)));
				}
			} // fixture1 disposed here - kills the process pair, saves/ is left on disk.

			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture2, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					string[] names = client.ReadCharacterList();
					int slot = Array.IndexOf(names, CharName);
					Assert.True(slot >= 0, "Expected '" + CharName + "' to still exist after restart. Names: " +
						string.Join(",", names.Select(n => n ?? "<empty>")));
					client.SendSelectCharacter(slot);

					int mark = fixture2.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCheckRaceState");
					Assert.True(fixture2.WaitForGameOutputSince("RACE_STATE:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(fixture2.GameOutputSnapshot(), "RACE_STATE:");

					Assert.Contains("race=Dwarf", line);
					Assert.Contains("nation=Ereboran", line);
					Assert.Contains("realm=Gondor", line);
				}
			}
		}

		/// <summary>#1027 - Mordor's realm is -1, which threw in the middle of the save.</summary>
		[Fact]
		public void MordorsRealmSurvivesARestart() {
			Assert.Contains("realm=Mordor", this.SaveMordorAndComeBack(false));
		}

		/// <summary>#1027 - a save written before the fix: "realm=" and nothing after it. The nation brings it back.</summary>
		[Fact]
		public void ACharacterWhoseSaveLostTheRealmGetsItBack() {
			Assert.Contains("realm=Mordor", this.SaveMordorAndComeBack(true));
		}

		private string SaveMordorAndComeBack(bool damageTheSave) {
			const string mordorChar = "MordorChecker";
			string repoRoot;
			using (var fixture1 = new GameServerPairFixture()) {
				repoRoot = fixture1.RepoRoot;
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture1, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(mordorChar);
					int mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestSetMordorNation");
					Assert.True(fixture1.WaitForGameOutputSince("RACE_NATION_SET:", mark, TimeSpan.FromSeconds(10)));
					Assert.Contains("realm=Mordor", LastLineContaining(fixture1.GameOutputSnapshot(), "RACE_NATION_SET:"));
					mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestTriggerGlobalSave");
					Assert.True(fixture1.WaitForGameOutputSince("TEST_SAVE: done", mark, TimeSpan.FromSeconds(30)));
				}
			}
			if (damageTheSave) {
				int damaged = 0;
				foreach (string file in Directory.GetFiles(Path.Combine(repoRoot, "saves"), "*.sav", SearchOption.AllDirectories)) {
					string text = File.ReadAllText(file);
					string broken = Regex.Replace(text, @"realm=\(Enum:Realm\)-1\r?\n(def=#p_realm\r?\n)?", "realm=\n");
					if (broken != text) {
						File.WriteAllText(file, broken);
						damaged++;
					}
				}
				Assert.True(damaged > 0, "No saved Mordor realm found to damage.");
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture2, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					string[] names = client.ReadCharacterList();
					int slot = Array.IndexOf(names, mordorChar);
					Assert.True(slot >= 0, "Expected the character to still exist after restart.");
					client.SendSelectCharacter(slot);
					int mark = fixture2.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCheckRaceState");
					Assert.True(fixture2.WaitForGameOutputSince("RACE_STATE:", mark, TimeSpan.FromSeconds(10)));
					return LastLineContaining(fixture2.GameOutputSnapshot(), "RACE_STATE:");
				}
			}
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture, string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}

						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RaceNationRealmReloadCollection {
		public const string Name = "RaceNationRealmReloadPair";
	}
}
