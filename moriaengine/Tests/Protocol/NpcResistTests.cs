using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #606 - "Converter: map real tag.resist_X damage-resist tags onto NPCDef's existing
	/// ResistX fields". The engine's Character.ResistX properties (ResistFire/ResistPoison/
	/// etc, Character.ct's Defs: section) were already fully live and consumed in
	/// DamageManager.GetResistModifier - they just had zero .def content setting them
	/// anywhere. This proves the exact field names the converter now emits (ResistPoison/
	/// ResistFire/ResistElectric/ResistSlashing/ResistStabbing - the last two covering the
	/// "sharp" dual-write case, DamageType.Sharp = Slashing|Stabbing) actually apply on a
	/// real NPC.
	/// </summary>
	[Collection(NpcResistCollection.Name)]
	public class NpcResistTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcResistTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ResistFieldsApplyOnARealNpc() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "ResistChecker")) {
				client.SendSpeech(".TestCreateResistNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NPC_RESIST_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_RESIST_TEST_RESULT:");

				Assert.Contains("poison=850", line);
				Assert.Contains("fire=200", line);
				Assert.Contains("electric=100", line);
				Assert.Contains("slashing=-100", line);
				Assert.Contains("stabbing=-100", line);
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

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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

	[CollectionDefinition(NpcResistCollection.Name, DisableParallelization = true)]
	public class NpcResistCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcResistPair";
	}
}
