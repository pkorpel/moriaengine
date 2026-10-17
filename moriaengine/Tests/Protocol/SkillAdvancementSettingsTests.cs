using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// GM-tunable skill-advancement-rate settings (#330) - confirms the write-through mechanism
	/// itself (a settings-category edit actually changes the live data other code reads), not
	/// the generic ViewableClassGenerator-based dialog editing plumbing underneath it (already
	/// proven by every other existing settings category in production, and independently
	/// verified by reading the generator's own codegen directly before relying on it - see
	/// SkillAdvancementSettings.cs's own doc comment).
	/// </summary>
	[Collection(SkillAdvancementSettingsCollection.Name)]
	public class SkillAdvancementSettingsTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public SkillAdvancementSettingsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MiningAdvRateEditWritesThroughToLiveSkillDef() {
			using (var client = this.ConnectLoginAndCreateCharacter("AdvSettingsMiningEditor")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetMiningAdvRateViaSettings 10 2 2");
				Assert.True(this.fixture.WaitForGameOutputSince("ADV_SETTINGS_MINING_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportAdvRate \"Mining\"");
				Assert.True(this.fixture.WaitForGameOutputSince("ADVRATE_REPORT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADVRATE_REPORT:");
				Assert.Contains("values=10,2,2", line);
			}
		}

		[Fact]
		public void LockpickingAdvRateEditIsReadBackImmediately() {
			using (var client = this.ConnectLoginAndCreateCharacter("AdvSettingsLockpickEditor")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetLockpickingAdvRateViaSettings 50");
				Assert.True(this.fixture.WaitForGameOutputSince("ADV_SETTINGS_LOCKPICKING_SET:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADV_SETTINGS_LOCKPICKING_SET:");
				Assert.Contains("rate=50", line);
			}
		}

		[Fact]
		public void TamingAdvRateDefaultMatchesRealErebor() {
			// Deliberately checks only tamingAdvRate here, never edited by any other Fact in
			// this shared-fixture collection (unlike lockpickingAdvRate/miningLow-mid-high above)
			// - safe regardless of xUnit's actual execution order.
			using (var client = this.ConnectLoginAndCreateCharacter("AdvSettingsDefaultsChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCustomFormulaDefaults");
				Assert.True(this.fixture.WaitForGameOutputSince("ADV_SETTINGS_CUSTOM_DEFAULTS:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADV_SETTINGS_CUSTOM_DEFAULTS:");
				Assert.Contains("taming=600", line);
			}
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

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

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
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SkillAdvancementSettingsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SkillAdvancementSettingsPair";
	}
}
