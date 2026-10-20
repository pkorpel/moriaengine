using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression tests for the #456/#457/run-stand-archery follow-up on Ranger's base attack
	/// (RangerCombatTests.cs covers the base attack itself): RangerSettings.bowDamage actually
	/// overrides the weapon's own content value, the #457 arrow-tier placeholder bonus applies,
	/// and the stand-still toggle's default behavior (crossbow=locked, bow=free) actually holds
	/// under a real mid-fight attempt to walk.
	/// </summary>
	[Collection(RangerSettingsCollection.Name)]
	public class RangerSettingsTests {
		private const string Account = "rangersettingstest";
		private const string Password = "rangersettingstest";

		private readonly GameServerPairFixture fixture;

		public RangerSettingsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BowDamageSettingDrivesWeaponAttack() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerSettingsDamageChecker")) {
				this.EquipRealBow(client);

				client.SendSpeech(".TestSetRangerBowDamage100");
				int at100 = this.ReportWeaponAttackVsP(client);

				client.SendSpeech(".TestSetRangerBowDamage1000");
				int at1000 = this.ReportWeaponAttackVsP(client);

				client.SendSpeech(".TestResetRangerBowDamage");

				Assert.True(at100 > 0, "Expected a nonzero attack value at bowDamage=100.");
				double ratio = (double) at1000 / at100;
				Assert.True((ratio > 9.0) && (ratio < 11.0),
					"Expected WeaponAttackVsP to scale ~10x with a 10x RangerSettings.bowDamage change " +
					"(#456 - settings-only override, not weapon content). at100=" + at100 + " at1000=" + at1000 +
					" ratio=" + ratio);
			}
		}

		/// <summary>#457 - a higher-tier arrow (valorite, tier rank 3) should apply the
		/// placeholder per-tier bonus (default +5%/tier) on top of whatever RangerSettings.
		/// bowDamage already contributes, confirming the bonus is real and tier-sensitive (not
		/// just a constant applied regardless of ammo).</summary>
		[Fact]
		public void HigherTierArrowGrantsDamageBonus() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerSettingsArrowTierChecker")) {
				this.EquipRealBow(client);

				client.SendSpeech(".TestSetRangerBowDamage1000"); // large magnitude keeps int-truncation noise small
				int withCopper = this.ReportWeaponAttackVsP(client);

				client.SendSpeech(".TestReplaceArrowsWithValorite");
				int withValorite = this.ReportWeaponAttackVsP(client);

				client.SendSpeech(".TestResetRangerBowDamage");

				Assert.True(withCopper > 0, "Expected a nonzero baseline attack value with copper arrows.");
				double ratio = (double) withValorite / withCopper;
				// valorite is tier rank 3, default bonus 0.05/tier -> expected ratio 1.15
				Assert.True((ratio > 1.10) && (ratio < 1.20),
					"Expected valorite arrows (tier rank 3) to grant roughly a 15% bonus over copper " +
					"(#457 placeholder, default 0.05/tier). withCopper=" + withCopper + " withValorite=" + withValorite +
					" ratio=" + ratio);
			}
		}

		/// <summary>#512 - crossbow defaults to RangerSettings.crossbowMustStandStill=true:
		/// reworked from the old snap-back-teleport mechanic (see git history) after the user
		/// found the teleport itself was a bug (#513). Now: moving fails the attack (no damage,
		/// no ammo spent, no position change) for as long as the attacker keeps moving; standing
		/// still again resumes it on its own.</summary>
		[Fact]
		public void CrossbowBlocksAttackWhileMovingThenResumes() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerStandStillCrossbowChecker")) {
				this.EquipRealCrossbow(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRangerCrossbowStandStillCheck");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: STARTED_MOVING", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: MIDMOVE_PASS", mark, TimeSpan.FromSeconds(10)),
					"Expected a crossbow user to land zero damage/spend no ammo while moving. Game output:\n" + this.fixture.GameOutputSnapshot());
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: STOPPED_MOVING", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: PASS", mark, TimeSpan.FromSeconds(15)),
					"Expected the attack to resume and land damage once standing still again. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>#512 - bow defaults to RangerSettings.bowMustStandStill=false: the same
		/// mid-fight movement should NOT block the attack at all.</summary>
		[Fact]
		public void BowIsUnaffectedByMovingMidAttack() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangerStandStillBowChecker")) {
				this.EquipRealBow(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRangerBowStandStillCheck");
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: STARTED_MOVING", mark, TimeSpan.FromSeconds(10)));
				Assert.True(this.fixture.WaitForGameOutputSince("RANGER_STANDSTILL_TEST: MIDMOVE_PASS", mark, TimeSpan.FromSeconds(10)),
					"Expected a bow user to keep landing damage/spending ammo despite moving. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		private int ReportWeaponAttackVsP(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportWeaponAttackVsP");
			Assert.True(this.fixture.WaitForGameOutputSince("RANGER_SETTINGS_TEST: attackVsP=", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RANGER_SETTINGS_TEST: attackVsP=");
			return ExtractInt(line, "attackVsP");
		}

		private void EquipRealBow(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
			uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

			mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestGrantBowForSettingsCheck");
			Assert.True(this.fixture.WaitForGameOutputSince("RANGER_SETTINGS_BOW_GRANTED:", mark, TimeSpan.FromSeconds(10)));
			uint bowUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "RANGER_SETTINGS_BOW_GRANTED: uid=");

			client.SendPickUpItem(bowUid, 1);
			Thread.Sleep(200);
			client.SendDropToWearItem(bowUid, selfUid);
			Thread.Sleep(200);
		}

		private void EquipRealCrossbow(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
			uint selfUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "MY_UID: ");

			mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestGrantCrossbowForStandStillCheck");
			Assert.True(this.fixture.WaitForGameOutputSince("RANGER_SETTINGS_CROSSBOW_GRANTED:", mark, TimeSpan.FromSeconds(10)));
			uint crossbowUid = ExtractMarkedUid(this.fixture.GameOutputSnapshot(), mark, "RANGER_SETTINGS_CROSSBOW_GRANTED: uid=");

			client.SendPickUpItem(crossbowUid, 1);
			Thread.Sleep(200);
			client.SendDropToWearItem(crossbowUid, selfUid);
			Thread.Sleep(200);
		}

		private static int ExtractInt(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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
	public class RangerSettingsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RangerSettingsPair";
	}
}
