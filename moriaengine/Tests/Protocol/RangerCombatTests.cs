using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression test for Ranger's ranged attack (#454 follow-up): equips a REAL bow via the
	/// actual pickup/drop-to-wear client protocol (not a server-side shortcut - same reasoning
	/// as ProfessionTests' own equip tests), then confirms the real WeaponSkillDef swing loop
	/// fires it at range, consumes ammo, and lands damage - the single biggest gap #454's own
	/// research flagged (zero prior test coverage for player-side bow usage, only NPC archers).
	/// </summary>
	[Collection(RangerCombatCollection.Name)]
	public class RangerCombatTests {
		private const string Account = "rangertest";
		private const string Password = "rangertest";

		private readonly GameServerPairFixture fixture;

		public RangerCombatTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StandsOffAtRangeWithRealBow() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerRangeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantBowAndArrows");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_BOW_GRANTED:", mark, TimeSpan.FromSeconds(10)));
				uint bowUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "RANGER_BOW_GRANTED: uid=");

				client.SendPickUpItem(bowUid, 1);
				Thread.Sleep(200);
				client.SendDropToWearItem(bowUid, selfUid);
				Thread.Sleep(200);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRangerStandsOffAtRange");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDOFF_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutput("RANGER_STANDOFF_TEST: PASS", TimeSpan.FromSeconds(30)),
					"Expected the Ranger to stay at range, land damage, and consume ammo. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		private static uint ExtractMarkedUid(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while ((end < rest.Length) && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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
	public class RangerCombatCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RangerCombatPair";
	}
}
