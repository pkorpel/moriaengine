using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#843 - the pieces' ids are permanent: after a real save and restart, .SetupWorld still knows every piece, the one
	/// a GM moved included, and adds only the one a GM deleted.</summary>
	[Collection(WorldSetupRestartCollection.Name)]
	public class WorldSetupRestartTests {
		private const string Account = "worldsetuprestart";
		private const string Password = "worldsetuprestart";
		private const int AllPieces = 493; //371 + 99 stall and field signs + 2 alchemists (#1017) + 2 Prodejce (#1070) + the camp guard (#1107) + 6 arena pieces + 2 war declarators + 2 war lieutenants + 4 palantirs + 4 cash boxes (#1116)
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void PieceIdsSurviveARealRestart() {
			string moved;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Setup Saver")) {
					AssertHas(Ask(fixture1, client, ".SetupWorld", "SETUP_DONE:"), "added=" + AllPieces + " kept=0");
					moved = Field(Ask(fixture1, client, ".TestWorldSetupEdit", "WORLD_EDIT:"), "moved=");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Setup Loader")) {
					AssertHas(Ask(fixture2, client, ".SetupWorld", "SETUP_DONE:"), "added=1 kept=" + (AllPieces - 1));
					string counts = Ask(fixture2, client, ".TestWorldSetupCheck", "WORLD_CHECK:");
					AssertHas(counts, "elviras=1");
					string[] at = Field(counts, "elviraAt=").Split(','), to = moved.Split(',');
					Assert.True(Math.Abs(int.Parse(at[0]) - int.Parse(to[0])) < 15, "Elvira went back from " + moved + ": " + counts); //she walks about
					AssertHas(counts, "doors=1");
					AssertHas(counts, "stones=2 lenastirs=2 rinalds=1 elqiras=2");
				}
			}
		}

		private static string Field(string line, string key) {
			int idx = line.IndexOf(key, StringComparison.Ordinal);
			Assert.True(idx >= 0, "no '" + key + "' in: " + line);
			int start = idx + key.Length;
			int end = line.IndexOf(' ', start);
			return (end < 0) ? line.Substring(start).Trim() : line.Substring(start, end - start);
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture, Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Password);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech);
			return LastLineContaining(fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
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
	public class WorldSetupRestartCollection {
		public const string Name = "WorldSetupRestart";
	}
}
