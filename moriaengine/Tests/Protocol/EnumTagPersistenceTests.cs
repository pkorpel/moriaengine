using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #407: Player.Race/Nation (and Character.Realm, via Nation's own
	/// cascade) all showed "None" after a real save/reload, even though the save file had
	/// real data - ObjectSaver saved any enum as a bare, type-erased number, so the next load
	/// re-typed it as a plain int, failing the properties' own "tag is SomeEnum" check. This
	/// confirms the real save path (TagHolder.Save -> ObjectSaver.Save) now writes the
	/// type-preserving format in an actual save file, not just via the direct pure-logic
	/// round-trip already covered by Tests/ObjectSaverEnumTests.cs. Own isolated fixture,
	/// same reasoning as DefPersistenceRegressionTests.cs - this deliberately triggers a real
	/// Globals.Save().
	/// </summary>
	[Collection(EnumTagPersistenceCollection.Name)]
	public class EnumTagPersistenceTests {
		private const string Account = "enumtagpersisttest";
		private const string Password = "enumtagpersisttest";

		private readonly GameServerPairFixture fixture;

		public EnumTagPersistenceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RaceAndNationTagsSurviveARealSave() {
			using (var client = this.ConnectLoginAndCreateCharacter("EnumTagChecker")) {
				int setMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetRaceAndNation");
				Assert.True(this.fixture.WaitForGameOutputSince("RACE_NATION_SET:", setMark, TimeSpan.FromSeconds(10)));

				int saveMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTriggerGlobalSave");
				Assert.True(this.fixture.WaitForGameOutputSince("TEST_SAVE: done", saveMark, TimeSpan.FromSeconds(30)));

				string savesRoot = Path.Combine(this.fixture.RepoRoot, "saves");
				string latestDir = Directory.GetDirectories(savesRoot)
					.SelectMany(Directory.GetDirectories)
					.OrderByDescending(Directory.GetLastWriteTimeUtc)
					.First();
				string thingsSav = File.ReadAllText(Path.Combine(latestDir, "things.sav"));

				Assert.Contains("tag._race_=(Enum:Race)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("tag._nation_=(Enum:Nation)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("tag._realm_=(Enum:Realm)", thingsSav, StringComparison.Ordinal);
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
	public class EnumTagPersistenceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "EnumTagPersistencePair";
	}
}
