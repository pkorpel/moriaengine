using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1333 - the roads out of the towns are walked by .SetupWorld and kept with the world: after a real save and
	/// restart they're all there, and placing errand things outside every town walks nothing; .ResetRoadsOut forgets them, and
	/// the next use walks again. Server side: scripts/Tests/RoadsOutTests.cs.</summary>
	[Collection(RoadsOutCollection.Name)]
	public class RoadsOutTests {
		private const string Account = "roadsout";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(90);

		[Fact]
		public void TheRoadsOutSurviveARestart() {
			int known, roads;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Cestovatel")) {
					string before = Ask(fixture1, client, ".TestRoadsOutReport", "ROADS_REPORT:");
					Assert.True(Number(before, "known") == 0, "a fresh world knows no roads: " + before);
					Ask(fixture1, client, ".SetupWorld", "SETUP_DONE:");
					string filled = Ask(fixture1, client, ".TestRoadsOutReport", "ROADS_REPORT:");
					Assert.True((Number(filled, "missing") == 0) && (Number(filled, "expected") >= 30) && (Number(filled, "roads") > 0), "SetupWorld walks every town: " + filled);
					known = Number(filled, "known");
					roads = Number(filled, "roads");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Navratilec")) {
					string loaded = Ask(fixture2, client, ".TestRoadsOutReport", "ROADS_REPORT:");
					Assert.True((Number(loaded, "known") == known) && (Number(loaded, "roads") == roads) && (Number(loaded, "missing") == 0) && (Number(loaded, "walked") == 0),
						"every road came back, none walked: " + loaded);
					string used = Ask(fixture2, client, ".TestRoadsOutUse", "ROADS_USE:");
					Assert.True((Number(used, "walked") == 0) && (Number(used, "placed") > 0) && (Number(used, "outside") == Number(used, "placed")), "placing walks nothing: " + used);

					Ask(fixture2, client, ".ResetRoadsOut", "'" + Account + "' commands 'ResetRoadsOut'");
					string reset = Ask(fixture2, client, ".TestRoadsOutReport", "ROADS_REPORT:");
					Assert.True(Number(reset, "known") == 0, "the reset forgets them: " + reset);
					string again = Ask(fixture2, client, ".TestRoadsOutUse", "ROADS_USE:");
					Assert.True(Number(again, "walked") > 0, "after a reset, using walks again: " + again);
				}
			}
		}

		private static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, " " + name + @"=(-?\d+)").Groups[1].Value);
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture);
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
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int at = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', at);
			return output.Substring(at, ((end < 0) ? output.Length : end) - at);
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture) {
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
	public class RoadsOutCollection {
		public const string Name = "RoadsOut";
	}
}
