using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// GM-tunable Mining settings (#337) - confirms the write-through mechanism itself for a
	/// representative sample across all 4 sub-systems (#333-336), not the generic
	/// ViewableClassGenerator-based dialog editing plumbing underneath (already proven by
	/// every other existing settings category, same scope decision as #330).
	/// </summary>
	[Collection(MiningSettingsCollection.Name)]
	public class MiningSettingsTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public MiningSettingsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void CopperAmountRangeEditWritesThroughToLiveOreTier() {
			using (var client = this.ConnectLoginAndCreateCharacter("MiningSettingsOreChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetCopperAmountRangeViaSettings 5 6");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_COPPER_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCopperTierDirectly");
				Assert.True(this.fixture.WaitForGameOutputSince("ORE_TIER_REPORT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ORE_TIER_REPORT:");
				Assert.Contains("min=5", line);
				Assert.Contains("max=6", line);
			}
		}

		[Fact]
		public void CaveInChanceEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("MiningSettingsCaveInChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetCaveInChanceViaSettings 50");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_CAVEIN_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("denominator=50", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINING_SETTINGS_CAVEIN_SET:"));
			}
		}

		[Fact]
		public void AmbushRatDefnameEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("MiningSettingsAmbushChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetAmbushRatDefnameViaSettings \"c_troll\"");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_AMBUSH_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("ratDefname=c_troll", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINING_SETTINGS_AMBUSH_SET:"));
			}
		}

		[Fact]
		public void GatherDelayBaseEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("MiningSettingsDelayChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetGatherDelayBaseViaSettings 5");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_DELAY_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("baseSeconds=5", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINING_SETTINGS_DELAY_SET:"));
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
	public class MiningSettingsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MiningSettingsPair";
	}
}
