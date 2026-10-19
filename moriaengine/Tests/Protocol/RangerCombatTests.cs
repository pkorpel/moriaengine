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
				this.EquipRealBow(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRangerStandsOffAtRange");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDOFF_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutput("RANGER_STANDOFF_TEST: PASS", TimeSpan.FromSeconds(30)),
					"Expected the Ranger to stay at range, land damage, and consume ammo. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>#458 (#96 follow-up) - a real bow attack's damage is measurably reduced by
		/// the target's real ArmorVsP, now that #96 no longer discards it for player-vs-monster
		/// combat.</summary>
		[Fact]
		public void ArmorReducesRealBowDamage() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerArmorChecker")) {
				this.EquipRealBow(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRangerArmorReducesDamage");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_ARMOREFFECT_TEST:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RANGER_ARMOREFFECT_TEST:");

				double avgUnarmored = ExtractDouble(line, "avgUnarmored");
				double avgArmored = ExtractDouble(line, "avgArmored");

				Assert.True(avgUnarmored > 0, "Expected some damage against the unarmored dummy. Line: " + line);
				Assert.True(avgArmored < avgUnarmored,
					"Expected real ArmorVsP to measurably reduce a real bow attack's damage (#458/#96). Line: " + line);
			}
		}

		/// <summary>Grants a real bow + arrows (TestGrantBowAndArrows) and equips the bow
		/// through the actual pickup/drop-to-wear client protocol - shared by every Fact in
		/// this class that needs a player with a real, worn bow.</summary>
		private void EquipRealBow(TestGameClient client) {
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
		}

		private static double ExtractDouble(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = line.IndexOf(' ', start);
			if (end < 0) {
				end = line.Length;
			}
			return double.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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
