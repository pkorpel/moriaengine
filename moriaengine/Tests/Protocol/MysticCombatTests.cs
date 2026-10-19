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

		[Fact]
		public void SwingSpeedScalesWithMana() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticSpeedChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticSwingSpeedScalesWithMana");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_SWINGSPEED_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_SWINGSPEED_TEST:");

				double delayLowMana = ExtractDouble(line, "delayLowMana");
				double delayHighMana = ExtractDouble(line, "delayHighMana");
				double nonMysticDelayLowMana = ExtractDouble(line, "nonMysticDelayLowMana");
				double nonMysticDelayHighMana = ExtractDouble(line, "nonMysticDelayHighMana");

				Assert.True(delayHighMana < delayLowMana,
					"Expected higher mana to mean a shorter (faster) swing delay for a Mystic. Line: " + line);
				Assert.Equal(nonMysticDelayLowMana, nonMysticDelayHighMana, 3);
			}
		}

		[Fact]
		public void SwingDrainsMana() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticManaChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMysticSwingDrainsMana");
				Assert.True(this.fixture.WaitForGameOutputSince("MYSTIC_MANADRAIN_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MYSTIC_MANADRAIN_TEST:");

				double manaBefore = ExtractDouble(line, "manaBefore");
				double manaAfter = ExtractDouble(line, "manaAfter");

				Assert.True(manaAfter < manaBefore, "Expected a landed Mystic swing to drain mana. Line: " + line);
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

	/// <summary>Separate fixture/account from MysticCombatTests above, purely to stay under
	/// AbstractAccount.maxCharactersPerGameAccount (a hard 5) - MysticCombatTests' own account
	/// is already exactly at that cap from its own 5 Facts (same class of mistake #421's own
	/// GatheringDelaySystemTests hit, per CLAUDE.md).</summary>
	[Collection(MysticCombatRangeCollection.Name)]
	public class MysticCombatRangeTests {
		private const string Account = "mystictestb";
		private const string Password = "mystictestb";

		private readonly GameServerPairFixture fixture;

		public MysticCombatRangeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>#451 - end-to-end real-attack version of the range check (not a direct
		/// formula call like the other Mystic tests), confirming WeaponSkillDef's existing
		/// Drawing->Striking loop works for a real Mystic player attack.</summary>
		[Fact]
		public void StandsOffAtRange() {
			using (var client = this.ConnectLoginAndCreateCharacter("MysticRangeChecker")) {
				client.SendSpeech(".TestMysticStandsOffAtRange");
				Assert.True(this.fixture.WaitForGameOutput("MYSTIC_STANDOFF_TEST: PASS", TimeSpan.FromSeconds(30)),
					"Expected the Mystic to stay at range and land damage. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		/// <summary>Testing combat messages: off by default - attacking produces no [Combat]
		/// system messages at all. Reads several system-message packets directly off the wire
		/// (not server-console markers - SysMessage has no console-log equivalent) and asserts
		/// none of them are combat-diagnostic lines.</summary>
		[Fact]
		public void MessagesAreSilentByDefault() {
			using (var client = this.ConnectLoginAndCreateCharacter("CombatMsgSilent")) {
				client.SendSpeech(".TestCombatMessagesSetup False");
				Assert.True(this.fixture.WaitForGameOutput("COMBAT_MESSAGES_TEST: STARTED", TimeSpan.FromSeconds(10)));

				bool sawCombatMessage = false;
				try {
					for (int i = 0; i < 5; i++) {
						string message = client.ReadSystemMessage(TimeSpan.FromSeconds(2));
						if (message.StartsWith("[Combat]", StringComparison.Ordinal)) {
							sawCombatMessage = true;
							break;
						}
					}
				} catch (TimeoutException) {
					// Expected once the (short) stream of unrelated messages, if any, runs dry.
				}

				Assert.False(sawCombatMessage, "Expected no [Combat] system messages with showCombatMessages left at its default (false).");
			}
		}

		/// <summary>Testing combat messages: enabled - both the attack-start and landed-hit
		/// lines appear, plus the Mystic-specific mana-drain line.</summary>
		[Fact]
		public void MessagesShowWhenEnabled() {
			using (var client = this.ConnectLoginAndCreateCharacter("CombatMsgEnabled")) {
				client.SendSpeech(".TestCombatMessagesSetup True");
				Assert.True(this.fixture.WaitForGameOutput("COMBAT_MESSAGES_TEST: STARTED", TimeSpan.FromSeconds(10)));

				string startMessage = ReadMessageContaining(client, "start attacking");
				Assert.StartsWith("[Combat]", startMessage);

				string hitMessage = ReadMessageContaining(client, "You hit");
				Assert.Contains("damage", hitMessage);

				string manaMessage = ReadMessageContaining(client, "channels");
				Assert.Contains("mana", manaMessage);
			}
		}

		/// <summary>Testing combat messages: the Drawing-phase "still too far, come closer"
		/// line, throttled to avoid spamming once per second while the attacker fails to close
		/// distance (players don't auto-path toward a target the way NPCs do).</summary>
		[Fact]
		public void TooFarMessageShowsWhenEnabled() {
			using (var client = this.ConnectLoginAndCreateCharacter("CombatMsgTooFar")) {
				client.SendSpeech(".TestCombatMessagesTooFarSetup");
				Assert.True(this.fixture.WaitForGameOutput("COMBAT_MESSAGES_TOOFAR_TEST: STARTED", TimeSpan.FromSeconds(10)));

				string message = ReadMessageContaining(client, "get closer");
				Assert.Contains("get closer", message);
			}
		}

		private static string ReadMessageContaining(TestGameClient client, string expectedSubstring) {
			for (int attempt = 0; attempt < 10; attempt++) {
				string message = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				if (message.IndexOf(expectedSubstring, StringComparison.Ordinal) >= 0) {
					return message;
				}
			}
			throw new TimeoutException("Never saw a system message containing '" + expectedSubstring + "'.");
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
	public class MysticCombatRangeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MysticCombatRangePair";
	}
}
