using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Mining's anti-macro delay scaling (#140/#332/#336) - real Erebor's own flat 20-second
	/// base delay, reduced once skill exceeds 100.0% ("1% above 100 = 0.03s less delay",
	/// funkce.scp's own comment).
	/// </summary>
	[Collection(GatheringDelaySystemCollection.Name)]
	public class GatheringDelaySystemTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public GatheringDelaySystemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Theory]
		[InlineData("Below", "50", "20")] // below threshold - flat base delay
		[InlineData("AtThreshold", "100", "20")] // exactly at threshold - still flat
		[InlineData("AboveSmall", "150", "18.5")] // (150-100)*0.03 = 1.5 less
		[InlineData("AboveLarge", "200", "17")] // (200-100)*0.03 = 3 less
		public void DelayScalesCorrectlyAboveTheAntiMacroThreshold(string caseName, string skillPercent, string expectedDelay) {
			// Character names can't contain digits (letters and spaces only, same already-
			// documented rule as the no-underscores one) - a digit-embedding name here would
			// silently fail creation and produce a confusing "command never ran" timeout
			// instead of a clear denial, same failure class already hit once this session.
			using (var client = this.ConnectLoginAndCreateCharacter("DelayFormulaChecker" + caseName)) {
				// #533 - these expected values assume a 20s base delay, but BaseDelaySeconds is
				// now a shared, live-tunable, persisted default (currently 2.0, not 20.0) - pin
				// it explicitly so this test checks the FORMULA's shape, not the live default.
				int baseMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetGatherDelayBaseViaSettings 20");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_DELAY_SET:", baseMark, TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportDelayForSkillPercent " + skillPercent);
				Assert.True(this.fixture.WaitForGameOutputSince("DELAY_FOR_PERCENT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "DELAY_FOR_PERCENT:");
				Assert.Contains("delay=" + expectedDelay, line);
			}
		}

		[Fact]
		public void CharacterWrapperCorrectlyExtractsDisplayPercentFromInternalSkillValue() {
			using (var client = this.ConnectLoginAndCreateCharacter("DelayCharWrapperChecker")) {
				// #533 - pin the base delay to 20 for the same reason as the Theory above.
				int baseMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetGatherDelayBaseViaSettings 20");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_DELAY_SET:", baseMark, TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				// Internal skill values are x10 - 1500 internal = 150.0% display, matching one
				// of the pure-formula cases above (expected delay 18.5s) - confirms the wrapper's
				// own /10.0 extraction, not just the formula already covered by the Theory.
				client.SendSpeech(".TestSetSkillValue \"Mining\" 1500");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMiningDelayForChar");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_DELAY_FOR_CHAR:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MINING_DELAY_FOR_CHAR:");
				Assert.Contains("delay=18.5", line);
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
	public class GatheringDelaySystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatheringDelaySystemPair";
	}
}
