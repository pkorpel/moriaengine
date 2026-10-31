using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#884 - a guild, its stone, ranks and abbreviation switch after a real save and restart.</summary>
	[Collection(GuildStoneRestartCollection.Name)]
	public class GuildStoneRestartTests {
		private const string Account = "guildstonerestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void GuildSurvivesARealRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Guild Saver")) {
					Ask(fixture1, client, ".TestGuildStoneForSave", "GUILD_SAVED:");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Guild Loader")) {
					string loaded = Ask(fixture2, client, ".TestGuildStoneAfterRestart", "GUILD_LOADED:");
					Assert.Contains("found=True stoneName=Bratrstvo sameGuild=True abbrev=BR", loaded);
					Assert.Contains("master=Guild_Saver members=1 candidates=1 shown=Guild_Saver_[BR]", loaded);
				}
			}
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort();
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);

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
	public class GuildStoneRestartCollection {
		public const string Name = "GuildStoneRestart";
	}
}
