using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for #407, UPDATED for #410: Player.Race/Nation (and Character.Realm,
	/// via Nation's own cascade) originally showed "None" after a real save/reload - a
	/// tag-based enum round-trip bug, fixed generically in ObjectSaver (#407/#408). #410 then
	/// reworked Race/Nation into a combined RaceNationPlugin and Realm into its own
	/// RealmPlugin (mirroring ProfessionPlugin's architecture), per the user's explicit
	/// request - so these three properties are no longer tags at all. This test now confirms
	/// the real save path produces a typed plugin reference
	/// (@@_racenation_=(RaceNationPlugin)N / @@_realmplugin_=(RealmPlugin)N, each with its own
	/// [RaceNationPlugin N]/[RealmPlugin N] block) rather than a tag line - the plugin's own
	/// internal enum fields still benefit from #407's "(Enum:Name)value" format, confirmed via
	/// the same literal substring check. Own isolated fixture, same reasoning as
	/// DefPersistenceRegressionTests.cs - this deliberately triggers a real Globals.Save().
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

				Assert.Contains("@@_racenation_=(RaceNationPlugin)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("@@_realmplugin_=(RealmPlugin)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("race=(Enum:Race)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("nation=(Enum:Nation)", thingsSav, StringComparison.Ordinal);
				Assert.Contains("realm=(Enum:Realm)", thingsSav, StringComparison.Ordinal);
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
