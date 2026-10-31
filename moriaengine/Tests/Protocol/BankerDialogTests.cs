using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #509 - two independent ways to open your own bank box via a banker (double-click, or
	/// saying "bank"), both gated to BankerRange.MaxDistance (6) rather than the engine's global
	/// Globals.SpeechDistance (10 by default). See scripts/Basic/Economy/BankerDClick.cs and
	/// BankerSpeech.cs's "bank" keyword for the implementation, and this class's own server-side
	/// counterpart scripts/Tests/EconomyTests.cs for the setup/report commands used below.
	/// Deliberately verifies via OpenedContainers.HasContainerOpen (a real server-side query of
	/// what actually happened) rather than trying to parse the raw DrawContainerOutPacket wire
	/// format, matching this suite's existing GOLD_CHECK-style "ask the server what it did"
	/// pattern (see EconomyTests.cs) instead of a new packet-parsing path.
	/// </summary>
	[Collection(BankerDialogCollection.Name)]
	public class BankerDialogTests {
		private const string Account = "bankerdialogtest";
		private const string Password = "bankerdialogtest";

		private readonly GameServerPairFixture fixture;

		public BankerDialogTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SayingBankWithinRangeOpensBankBox() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerSpeechNearChecker")) {
				client.SendSpeech(".TestSetupNearbyBankerForDialogCheck");
				Thread.Sleep(500); // let the freshly-spawned banker's own AI/placement settle
				client.SendSpeech("bank");
				Thread.Sleep(500);
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportBankOpenToMe");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_OPEN_TEST: open=True", mark, TimeSpan.FromSeconds(10)),
					"Expected saying 'bank' within range to open the bank box. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		[Fact]
		public void SayingBankBeyondRangeDoesNotOpenBankBox() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerSpeechFarChecker")) {
				int setupMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupFarBankerForDialogCheck");
				Assert.True(this.fixture.WaitForGameOutputSince("ECONOMY_SETUP: STARTED", setupMark, TimeSpan.FromSeconds(10)),
					"Setup never printed. Game output:\n" + this.fixture.GameOutputSnapshot());
				Thread.Sleep(500);
				client.SendSpeech("bank");
				string message;
				try {
					message = ReadMessageContaining(client, "closer");
				} catch (TimeoutException) {
					throw new Exception("No 'closer' message seen. Setup diagnostics:\n" + this.fixture.GameOutputSnapshot());
				}
				Assert.Contains("closer", message, StringComparison.OrdinalIgnoreCase);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportBankOpenToMe");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_OPEN_TEST: open=False", mark, TimeSpan.FromSeconds(10)),
					"Expected saying 'bank' beyond range 6 (but still within SpeechDistance) to NOT open the bank box. " +
					"Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		[Fact]
		public void DoubleClickingBankerWithinRangeLeadsToTheBankBox() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerDClickNearChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupNearbyBankerForDialogCheck");
				Assert.True(this.fixture.WaitForGameOutputSince("ECONOMY_SETUP: STARTED", mark, TimeSpan.FromSeconds(10)));
				uint bankerUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark, "uid=");
				Thread.Sleep(500);

				client.SendSpeech(".TestBankerPortDress"); //no pants, no banker
				Thread.Sleep(500);
				client.SendDoubleClick(bankerUid);
				var gump = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gump.GumpUid, 1); //Bankovni sluzby
				gump = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gump.GumpUid, 1); //Otevrit banku
				Thread.Sleep(500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportBankOpenToMe");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_OPEN_TEST: open=True", mark, TimeSpan.FromSeconds(10)),
					"Expected double-clicking a nearby banker to lead to the bank box. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		[Fact]
		public void DoubleClickingBankerBeyondRangeDoesNotOpenBankBox() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerDClickFarChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupFarBankerForDialogCheck");
				Assert.True(this.fixture.WaitForGameOutputSince("ECONOMY_SETUP: STARTED", mark, TimeSpan.FromSeconds(10)));
				uint bankerUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark, "uid=");
				Thread.Sleep(500);

				client.SendDoubleClick(bankerUid);
				Thread.Sleep(500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportBankOpenToMe");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_OPEN_TEST: open=False", mark, TimeSpan.FromSeconds(10)),
					"Expected double-clicking a far-away banker to fall back to the default (no bank box opened). " +
					"Game output:\n" + this.fixture.GameOutputSnapshot());
			}
		}

		private static string ReadMessageContaining(TestGameClient client, string expectedSubstring) {
			for (int attempt = 0; attempt < 10; attempt++) {
				string message = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				if (message.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) >= 0) {
					return message;
				}
			}
			throw new TimeoutException("Never saw a system message containing '" + expectedSubstring + "'.");
		}

		private static uint ExtractUid(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			int start = idx + marker.Length;
			int end = start;
			while ((end < output.Length) && char.IsDigit(output[end])) {
				end++;
			}
			return uint.Parse(output.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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
	public class BankerDialogCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BankerDialogPair";
	}
}
