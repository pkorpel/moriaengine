using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #607 - "Converter: map tag.weapflag (monster natural-attack damage type) - needs a
	/// new NPCDef field". Confirms NPCDef.NaturalDamageType overrides a bare-handed NPC's
	/// usual WeaponType-derived damage type (CombatCalculator.GetWeaponDamageType, which
	/// can only express a fixed physical sub-type - PhysicalBlunt for BareHands - with no
	/// way to represent an elemental/multi-flag combo like the real archive's
	/// "dam_physical|dam_fire"), and that an NPC which never configures it keeps today's
	/// exact unconfigured default (zero regression for existing content).
	/// </summary>
	[Collection(NaturalDamageTypeCollection.Name)]
	public class NaturalDamageTypeTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NaturalDamageTypeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ConfiguredOverridesBareHandsDefaultUnconfiguredStaysDefault() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "NaturalDamageChecker")) {
				client.SendSpeech(".TestCreateNaturalDamageNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NATURAL_DAMAGE_TYPE_TEST_RESULT: unconfigured", TimeSpan.FromSeconds(10)));
				string output = this.fixture.GameOutputSnapshot();

				string configuredLine = LastLineContaining(output, "NATURAL_DAMAGE_TYPE_TEST_RESULT: configured");
				Assert.Contains("Physical", configuredLine);
				Assert.Contains("Fire", configuredLine);

				string tripleLine = LastLineContaining(output, "NATURAL_DAMAGE_TYPE_TEST_RESULT: triple");
				Assert.Contains("Physical", tripleLine);
				Assert.Contains("Sharp", tripleLine);
				Assert.Contains("Summon", tripleLine);

				string unconfiguredLine = LastLineContaining(output, "NATURAL_DAMAGE_TYPE_TEST_RESULT: unconfigured");
				Assert.Contains("PhysicalBlunt", unconfiguredLine);
			}
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

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);
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

	[CollectionDefinition(NaturalDamageTypeCollection.Name, DisableParallelization = true)]
	public class NaturalDamageTypeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NaturalDamageTypePair";
	}
}
