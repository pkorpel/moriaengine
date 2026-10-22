using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #586 - proves NPCDef.ButcherResources (scripts/Basic/CharClasses/NPC.cs) actually
	/// parses the real archive's "Resources = amount defname, amount defname" format and
	/// round-trips correctly. This is data-template-only - there is no butchering
	/// INTERACTION yet (see the separate deferred follow-up issue) to consume it, so this
	/// test only confirms the parsed entries themselves, not any gameplay effect.
	/// </summary>
	[Collection(NpcResourcesCollection.Name)]
	public class NpcResourcesTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcResourcesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ResourcesEntriesRoundTripCorrectly() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ResourcesChecker")) {
				client.SendSpeech(".TestReportNpcResources");
				Assert.True(this.fixture.WaitForGameOutput("NPC_RESOURCES_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_RESOURCES_TEST_RESULT:");

				Assert.Contains("count=2", line);
				Assert.Contains("entry0=50 i_bag", line);
				Assert.Contains("entry1=20 i_dagger_copper", line);
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

	[CollectionDefinition(NpcResourcesCollection.Name, DisableParallelization = true)]
	public class NpcResourcesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcResourcesPair";
	}
}
