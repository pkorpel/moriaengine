using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// A1 (#102): ".usehand" was completely unimplemented - no method, no allowlist entry, so
	/// a non-GM player speaking it got "No permission" (or, pre-allowlisting, nothing at all).
	/// Uses i_torch (ShiningItem.def, layer=2/Hand2) as the equipped item because its
	/// On_DClick has a real, observable side effect (Model -> 0xa12) even while already
	/// equipped - the generic Equippable.On_DClick is a no-op in that case, so a plain
	/// weapon/wearable couldn't prove UseHand actually reached Trigger_DClick.
	/// </summary>
	[Collection(BasicCommandsCollection.Name)]
	public class BasicCommandsTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public BasicCommandsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real rule (A1): UseHand double-clicks whatever's equipped in hand1,
		/// falling back to hand2 if hand1 is empty - same as a client double-clicking the
		/// item directly.</summary>
		[Fact]
		public void UseHandTriggersDoubleClickOnOffhandItem() {
			using (var client = this.ConnectLoginAndCreateCharacter("HandUser")) {
				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantAndEquipTorch");
				Assert.True(this.fixture.WaitForGameOutputSince("TORCH_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint torchUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TORCH_SETUP: uid=");

				client.SendSpeech(".UseHand");
				Thread.Sleep(500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckItemModel " + torchUid);
				Assert.True(this.fixture.WaitForGameOutputSince("ITEM_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				string checkLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "ITEM_CHECK:");
				Assert.Contains("model=2578", checkLine); // 0xa12 - ShiningItem.On_DClick's lit model
				Assert.Contains("equippedOnSelf=True", checkLine); // Equippable.On_DClick's no-op-while-equipped path left it in place
			}
		}

		/// <summary>#102 - Erebor's usering: a double click on the worn ring.</summary>
		[Fact]
		public void UseRingDoubleClicksTheWornRing() {
			using (var client = this.ConnectLoginAndCreateCharacter("RingUser")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEquipNotingRing");
				Assert.True(this.fixture.WaitForGameOutputSince("RING_SETUP:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("layer=8", LastLineContaining(this.fixture.GameOutputSnapshot(), "RING_SETUP:"));
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNotingRing");
				Assert.True(this.fixture.WaitForGameOutputSince("RING_USED:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("used=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "RING_USED:"));
				client.SendSpeech(".UseRing");
				Thread.Sleep(300);
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNotingRing");
				Assert.True(this.fixture.WaitForGameOutputSince("RING_USED:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("used=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "RING_USED:"));
			}
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
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
	public class BasicCommandsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BasicCommandsPair";
	}
}
