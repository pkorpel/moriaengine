using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression coverage for #533: GatheringDelaySystem used to be a plain static class with
	/// bare mutable static fields - a live `.settings` edit took effect immediately but was never
	/// actually saved, silently resetting to the hardcoded default on every restart. Converted to
	/// a real [SaveableClass] singleton (same pattern as CombatSettings); this test proves the
	/// live-tuned value survives an actual Globals.Save(), not just an in-memory read-back -
	/// same "inspect the real globals.sav on disk" approach as DefPersistenceRegressionTests.
	/// </summary>
	[Collection(GatheringDelayPersistenceCollection.Name)]
	public class GatheringDelayPersistenceTests {
		private const string Account = "gatherdelaypersisttest";
		private const string Password = "gatherdelaypersisttest";

		private readonly GameServerPairFixture fixture;

		public GatheringDelayPersistenceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void LiveTunedBaseDelayIsActuallyWrittenToTheSaveFile() {
			using (var client = this.ConnectLoginAndCreateCharacter("DelaySaveChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// A distinctive value that's neither the old 20.0 default nor the new 2.0 default,
				// so a false-positive match against either default would be obvious.
				client.SendSpeech(".TestSetGatherDelayBaseViaSettings 7.5");
				Assert.True(this.fixture.WaitForGameOutputSince("MINING_SETTINGS_DELAY_SET: baseSeconds=7.5", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTriggerGlobalSave");
				Assert.True(this.fixture.WaitForGameOutputSince("TEST_SAVE: done", mark, TimeSpan.FromSeconds(30)));

				string savesRoot = Path.Combine(this.fixture.RepoRoot, "saves");
				string latestDir = Directory.GetDirectories(savesRoot)
					.SelectMany(Directory.GetDirectories)
					.OrderByDescending(Directory.GetLastWriteTimeUtc)
					.First();
				string globalsSav = File.ReadAllText(Path.Combine(latestDir, "globals.sav"));

				Assert.Contains("GatheringDelaySystem", globalsSav, StringComparison.OrdinalIgnoreCase);
				Assert.Contains("7.5", globalsSav, StringComparison.OrdinalIgnoreCase);
			}
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
	public class GatheringDelayPersistenceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatheringDelayPersistencePair";
	}
}
