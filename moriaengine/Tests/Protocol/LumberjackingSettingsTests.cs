using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// GM-tunable Lumberjacking settings (#425) - confirms the write-through mechanism itself
	/// for a representative sample across the sub-systems (#422/#431/#424), not the generic
	/// ViewableClassGenerator-based dialog editing plumbing underneath (already proven by every
	/// other existing settings category, same scope decision as MiningSettingsTests/#337).
	/// </summary>
	[Collection(LumberjackingSettingsCollection.Name)]
	public class LumberjackingSettingsTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LumberjackingSettingsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SpruceAmountRangeEditWritesThroughToLiveWoodTier() {
			using (var client = this.ConnectLoginAndCreateCharacter("LumberSettingsWoodChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSpruceAmountRangeViaLumberjackingSettings 5 6");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACKING_SETTINGS_SPRUCE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportWoodTierDirectly");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_TIER_REPORT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "WOOD_TIER_REPORT:");
				Assert.Contains("min=5", line);
				Assert.Contains("max=6", line);
			}
		}

		[Fact]
		public void AmbushChanceEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("LumberSettingsAmbushChanceChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetAmbushChanceViaLumberjackingSettings 50");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACKING_SETTINGS_AMBUSH_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("denominator=50", LastLineContaining(this.fixture.GameOutputSnapshot(), "LUMBERJACKING_SETTINGS_AMBUSH_SET:"));
			}
		}

		[Fact]
		public void AmbushWolfDefnameEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("LumberSettingsAmbushWolfChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetAmbushWolfDefnameViaLumberjackingSettings \"c_bear_grizzly\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACKING_SETTINGS_AMBUSH_WOLF_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("wolfDefname=c_bear_grizzly", LastLineContaining(this.fixture.GameOutputSnapshot(), "LUMBERJACKING_SETTINGS_AMBUSH_WOLF_SET:"));
			}
		}

		[Fact]
		public void GatherDelayBaseEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("LumberSettingsDelayChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSharedGatherDelayBaseViaLumberjackingSettings 5");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACKING_SETTINGS_DELAY_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("baseSeconds=5", LastLineContaining(this.fixture.GameOutputSnapshot(), "LUMBERJACKING_SETTINGS_DELAY_SET:"));
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
	public class LumberjackingSettingsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LumberjackingSettingsPair";
	}
}
