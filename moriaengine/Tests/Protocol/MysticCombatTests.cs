using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Regression tests for Mystic's mind-power attack (#447/#448/#449): mindPowerVsP/VsM and
	/// mindDefenseVsP/VsM were already fully computed on both the attacker and defender side
	/// (CombatCalculator.CalculateCombatWeaponValues/CalculateCombatArmorValues) but had zero
	/// consumers in DamageManager before this. Covers the three things that fix needed to prove:
	/// damage actually moves with mind power, mind defense actually reduces it (subtractive, per
	/// the confirmed Erebor source formula), and the damage type is magic/Mystical rather than
	/// physical.
	/// </summary>
	[Collection(MysticCombatCollection.Name)]
	public class MysticCombatTests {
		private const string Account = "mystictest";
		private const string Password = "mystictest";

		private readonly GameServerPairFixture fixture;

		public MysticCombatTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MindPowerScalesWithInt() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticPowerChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticMindPowerScalesWithInt");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_MINDPOWER_SCALES_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_MINDPOWER_SCALES_TEST:");

				Assert.Contains("isMystic=True", line);

				double mindPowerLow = ExtractDouble(line, "mindPowerLow");
				double damageLow = ExtractDouble(line, "damageLow");
				double mindPowerHigh = ExtractDouble(line, "mindPowerHigh");
				double damageHigh = ExtractDouble(line, "damageHigh");

				Assert.True(mindPowerLow > 0, "Expected non-zero mind power even at low Int. Line: " + line);
				Assert.True(mindPowerHigh > mindPowerLow, "Expected mind power to rise with Int. Line: " + line);
				Assert.True(damageHigh > damageLow, "Expected damage to rise with mind power. Line: " + line);
			}
		}

		[Fact]
		public void MindDefenseReducesDamage() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticDefenseChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticMindDefenseReducesDamage");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_MINDDEFENSE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_MINDDEFENSE_TEST:");

				double mindDefenseLow = ExtractDouble(line, "mindDefenseLow");
				double mindDefenseHigh = ExtractDouble(line, "mindDefenseHigh");
				double damageAgainstLowDefense = ExtractDouble(line, "damageAgainstLowDefense");
				double damageAgainstHighDefense = ExtractDouble(line, "damageAgainstHighDefense");

				Assert.True(mindDefenseHigh > mindDefenseLow, "Expected the MindDefenseModifier override to raise mindDefenseVsM. Line: " + line);
				Assert.True(damageAgainstLowDefense > 0, "Expected some damage against the low-defense dummy. Line: " + line);
				Assert.True(damageAgainstHighDefense < damageAgainstLowDefense,
					"Expected higher mind defense to reduce damage (subtractive formula). Line: " + line);
			}
		}

		[Fact]
		public void DamageIsMagicalNotPhysical() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticTypeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticDamageIsMagicalNotPhysical");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_DAMAGETYPE_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_DAMAGETYPE_TEST:");

				double physicalResistDamage = ExtractDouble(line, "physicalResistDamage");
				double magicResistDamage = ExtractDouble(line, "magicResistDamage");

				Assert.True(physicalResistDamage > 0,
					"Expected full damage against a target with maxed physical resist but zero magic/Mystical resist - a Mystic's attack must not be physical. Line: " + line);
				Assert.True(magicResistDamage < physicalResistDamage,
					"Expected maxed magic+Mystical resist to meaningfully reduce damage. Line: " + line);
			}
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
			return double.Parse(line.Substring(start, end - start), CultureInfo.InvariantCulture);
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
	public class MysticCombatCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MysticCombatPair";
	}
}
