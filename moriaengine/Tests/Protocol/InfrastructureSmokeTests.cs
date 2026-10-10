using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Smoke-tests the shared protocol-test infrastructure added for the full verification pass
	/// (V-0, #68) before any later phase builds scenarios on top of it: TestGameClient's generic
	/// packet scanner (<see cref="TestGameClient.ReadPacketPayload"/>) has to correctly split a
	/// single Huffman frame into multiple individual packets and skip ones it isn't looking for,
	/// since world-entry alone sends several packet types back-to-back (see
	/// GameCore/Networking/PacketSequences.cs's SendStartGameSequence/DelayedResyncTimer).
	/// Shares CombatScenarioCollection's fixture/account rather than spinning up a third whole
	/// server-process pair just for this - each test method here creates its own distinct
	/// character name, which is fine (the account has 5 free slots).
	/// </summary>
	[Collection(CombatScenarioCollection.Name)]
	public class InfrastructureSmokeTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		public InfrastructureSmokeTests(GameServerPairFixture servers) {
		}

		[Fact]
		public void PacketScannerFindsBothWorldEntryMarkersAmongOtherTraffic() {
			int redirectPort = WaitForServerListingAndGetRedirectPort();

			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("Scanner");

				// LoginConfirmationOutPacket (0x1B) comes first, then (after the server's own
				// 1s delay - see DelayedLoginTimer) LoginCompleteOutPacket (0x55) alongside
				// whatever Character.Resync() sends for a freshly-created character standing in
				// the open (at minimum DrawGamePlayerOutPacket/MobAttributesOutPacket-shaped
				// traffic) - finding 0x55 at all proves the scanner skipped over that surrounding
				// traffic correctly rather than mis-sizing a packet and corrupting the stream.
				game.ReadPacketPayload(0x1B, TimeSpan.FromSeconds(10));
				game.ReadPacketPayload(0x55, TimeSpan.FromSeconds(10));
			}
		}

		private static int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 20; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);

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
					Thread.Sleep(250);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}
}
