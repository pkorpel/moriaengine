using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #168: a def value from the scripts must NOT be written into the save
	/// as if it were a live GM edit, or the next boot reads it back from the save instead (where a
	/// tag's number can come back as another type). Since #1375 the NPCDef values that were tags are
	/// typed fields and TamingDifficulty is gone, so the check is general: a fresh world, where
	/// nobody changed any def, saves no NPCDef section at all. Own isolated fixture/collection:
	/// deliberately triggers a real Globals.Save(), which other Facts sharing a fixture
	/// shouldn't have to deal with mid-run.
	/// </summary>
	[Collection(DefPersistenceRegressionCollection.Name)]
	public class DefPersistenceRegressionTests {
		// Deliberately NOT "protocoltest" - this test triggers a real Globals.Save(), which
		// persists whatever account/character it uses into the shared saves/ directory on disk.
		// Several OTHER test classes' own fixtures boot separate server processes that also load
		// the most recent save there and assert "protocoltest" has zero pre-existing characters
		// (see CombatScenarioTests.cs/LoginHandshakeTests.cs) - using a unique account name here
		// keeps this test's persisted leftovers from breaking that assumption in a later-booted
		// fixture. accounts.Count==0 on this fixture's own fresh boot means this still
		// auto-creates on first login regardless of name, same as every other test's account.
		private const string Account = "defpersistencetest";
		private const string Password = "defpersistencetest";

		private readonly GameServerPairFixture fixture;

		public DefPersistenceRegressionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BaselineDefTagIsNotWrittenAsManuallyChanged() {
			using (var client = this.ConnectLoginAndCreateCharacter("SaveTrigger")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTriggerGlobalSave");
				Assert.True(this.fixture.WaitForGameOutputSince("TEST_SAVE: done", mark, TimeSpan.FromSeconds(30)));

				string savesRoot = Path.Combine(this.fixture.RepoRoot, "saves");
				string latestDir = Directory.GetDirectories(savesRoot)
					.SelectMany(Directory.GetDirectories)
					.OrderByDescending(Directory.GetLastWriteTimeUtc)
					.First();
				string globalsSav = File.ReadAllText(Path.Combine(latestDir, "globals.sav"));

				int at = globalsSav.IndexOf("[NPCDef ", StringComparison.OrdinalIgnoreCase);
				Assert.True(at < 0, "A fresh world saved an NPCDef section:\n" + ((at < 0) ? "" : globalsSav.Substring(at, Math.Min(1500, globalsSav.Length - at))));
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
	public class DefPersistenceRegressionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DefPersistenceRegressionPair";
	}
}
