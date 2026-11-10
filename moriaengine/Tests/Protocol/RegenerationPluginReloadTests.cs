using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #436 (#1369: the def is now RegenerationPluginDef.instance): it was declared on the Plugin
	/// class (not the Def class with the generated Bootstrap() method), the same vulnerable
	/// shape RaceNationPlugin/RealmPlugin had in #434 - "p_regenerations" was only registered
	/// lazily, on first TryInstallPlugin call, so a cold boot couldn't resolve an existing
	/// RegenerationPlugin's "def=#p_regenerations" reference. A player logging out while still
	/// damaged (hits &lt; maxHits) keeps this plugin installed, so this is a real, common
	/// scenario, not a rare edge case. Same real save -> restart -> reload pattern as #434's
	/// RaceNationRealmReloadTests / #438's GmFlagsReloadTests.
	/// </summary>
	[Collection(RegenerationPluginReloadCollection.Name)]
	public class RegenerationPluginReloadTests {
		private const string Account = "regenreloadtest";
		private const string Password = "regenreloadtest";
		private const string CharName = "RegenReloadChecker";

		[Fact]
		public void RegenerationPluginSurvivesARealServerRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				int redirectPort = WaitForServerListingAndGetRedirectPort(fixture1, Account);
				using (var client = new TestGameClient("127.0.0.1", redirectPort)) {
					client.SendLogin(Account, Password);
					client.ReadCharacterList();
					client.SendCreateCharacter(CharName);

					int mark = fixture1.GameOutputSnapshot().Length;
					client.SendSpeech(".TestDamageSelfToTriggerRegen");
					Assert.True(fixture1.WaitForGameOutputSince("REGEN_DAMAGE:", mark, TimeSpan.FromSeconds(10)));
					// Confirm the live, same-session install works BEFORE the restart - isolates
					// whether a failure below is really about reload.
					string damageLine = LastLineContaining(fixture1.GameOutputSnapshot(), "REGEN_DAMAGE:");
					Assert.Contains("hasRegenPlugin=True", damageLine);

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
					Assert.True(slot >= 0, "Expected '" + CharName + "' to still exist after restart.");
					client.SendSelectCharacter(slot);

					int mark = fixture2.GameOutputSnapshot().Length;
					client.SendSpeech(".TestCheckRegenPlugin");
					Assert.True(fixture2.WaitForGameOutputSince("REGEN_PLUGIN_STATE:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(fixture2.GameOutputSnapshot(), "REGEN_PLUGIN_STATE:");

					Assert.Contains("hasRegenPlugin=True", line);
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
	public class RegenerationPluginReloadCollection {
		public const string Name = "RegenerationPluginReloadPair";
	}
}
