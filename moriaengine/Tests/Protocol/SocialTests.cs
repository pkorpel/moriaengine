using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-4 (#72): social-system scenarios needing two real, simultaneously-connected characters.
	/// Real finding while building this: steamengine.ini's autoAccountCreation defaults to false,
	/// and AbstractAccount.HandleLoginAttempt only ever auto-creates the very first account on a
	/// fresh server - so getting a SECOND real account for client B required a new
	/// Globals.AutoAccountCreation setter (mirrors Globals.PaymentSource's existing
	/// settable-at-runtime pattern) flipped on by client A before client B's first login.
	/// </summary>
	[Collection(SocialCollection.Name)]
	public class SocialTests {
		private const string AccountA = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public SocialTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: a guild founder can invite another player (by targeting them),
		/// and once that player accepts, both report membership in the same guild.</summary>
		[Fact]
		public void TwoPlayersCanFormAGuildTogether() {
			using (var clientA = this.ConnectLoginAndCreateCharacter(AccountA, "GuildFounder")) {
				clientA.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST: ENABLED", TimeSpan.FromSeconds(10)));

				using (var clientB = this.ConnectLoginAndCreateCharacter(AccountB, "GuildJoiner")) {
					int markB = this.fixture.GameOutputSnapshot().Length;
					clientB.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markB, TimeSpan.FromSeconds(10)),
						"Expected MY_UID after position " + markB + ". Game output:\n" + this.fixture.GameOutputSnapshot());
					uint joinerUid = ExtractUid(this.fixture.GameOutputSnapshot(), markB);

					Thread.Sleep(300);
					clientA.SendSpeech(".GuildCreate TestGuild");
					Thread.Sleep(300);
					clientA.SendSpeech(".GuildInvite");
					Thread.Sleep(300);
					clientA.SendTargetUid(joinerUid);
					Thread.Sleep(500);
					clientB.SendSpeech(".GuildAccept");
					Thread.Sleep(500);

					int markA2 = this.fixture.GameOutputSnapshot().Length;
					clientA.SendSpeech(".TestCheckGuild");
					Assert.True(this.fixture.WaitForGameOutputSince("GUILD_CHECK: GuildFounder", markA2, TimeSpan.FromSeconds(10)));

					int markB2 = this.fixture.GameOutputSnapshot().Length;
					clientB.SendSpeech(".TestCheckGuild");
					Assert.True(this.fixture.WaitForGameOutputSince("GUILD_CHECK: GuildJoiner", markB2, TimeSpan.FromSeconds(10)));

					string output = this.fixture.GameOutputSnapshot();
					string founderLine = LastLineContaining(output, "GUILD_CHECK: GuildFounder");
					string joinerLine = LastLineContaining(output, "GUILD_CHECK: GuildJoiner");
					Assert.Contains("guild=TestGuild", founderLine);
					Assert.Contains("guild=TestGuild", joinerLine);
				}
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

		private static uint ExtractUid(string output, int since) {
			int idx = output.IndexOf("MY_UID: ", since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find MY_UID after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + "MY_UID: ".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
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
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SocialCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SocialPair";
	}
}
