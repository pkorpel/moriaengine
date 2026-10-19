using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Lumberjacking's anti-macro delay wiring (#424, Phase D of #421) - confirms
	/// LumberjackingSkillDef.On_Start is wired to the same shared GatheringDelaySystem formula/
	/// wrapper Mining already uses (GatheringDelaySystemTests.cs covers the formula itself and
	/// Mining's own wrapper - this just confirms Lumberjacking's own call site). Own isolated
	/// fixture/account rather than reusing GatheringDelaySystemTests.cs's own collection - that
	/// file's Account is already at the 5-character-slot cap (AbstractAccount.
	/// maxCharactersPerGameAccount) from its own existing Facts.
	/// </summary>
	[Collection(LumberjackingDelayCollection.Name)]
	public class LumberjackingDelayTests {
		private const string Account = "lumberdelaytest";
		private const string Password = "lumberdelaytest";

		private readonly GameServerPairFixture fixture;

		public LumberjackingDelayTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void WrapperCorrectlyExtractsDisplayPercentFromInternalSkillValue() {
			using (var client = this.ConnectLoginAndCreateCharacter("DelayLumberjackWrapperChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Same 1500 internal = 150.0% display -> 18.5s case as Mining's own wrapper
				// check (GatheringDelaySystemTests.cs) - confirms LumberjackingSkillDef.On_Start
				// (#424) is wired to the exact same shared formula/wrapper, not a separate
				// reimplementation.
				client.SendSpeech(".TestSetSkillValue \"Lumberjacking\" 1500");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportLumberjackingDelayForChar");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACKING_DELAY_FOR_CHAR:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LUMBERJACKING_DELAY_FOR_CHAR:");
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
	public class LumberjackingDelayCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LumberjackingDelayPair";
	}
}
