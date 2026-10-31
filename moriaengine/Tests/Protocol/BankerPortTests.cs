using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#883 - Erebor's banker: both dialogs, cheques, purses, through a client.</summary>
	[Collection(BankerPortCollection.Name)]
	public class BankerPortTests {
		private const string Account = "bankerporttest";
		private const string Password = "bankerporttest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public BankerPortTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void DialogsFollowRealmDressBalanceAndMoving() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerDialogChecker")) {
				this.Ask(client, ".TestBankerPortSetup \"mordor dressed 100\"", "BANKER_SETUP:");
				Assert.Contains("realm=Gondor", this.Line("BANKER_SETUP:"));
				client.SendSpeech("ahoj");
				Expect(client, "Jedes potvoro !!!!");

				this.Ask(client, ".TestBankerPortSetup \"gondor naked 100\"", "BANKER_SETUP:");
				client.SendSpeech("ahoj");
				Expect(client, "Tahni uchyle !!!!");

				this.Ask(client, ".TestBankerPortSetup \"gondor dressed 100\"", "BANKER_SETUP:");
				string setup = this.Line("BANKER_SETUP:");
				client.SendSpeech("ahoj");
				var gump = client.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "O cem presne chces hovorit ?");
				AssertHas(gump.Strings, "Bankovni sluzby");

				client.SendGumpResponse(gump.GumpUid, 3); //Hledam praci
				gump = client.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Pokud mas odvahu, hledej praci u zmocnence Gondoru.");

				client.SendGumpResponse(gump.GumpUid, 2); //Nejake drby ?
				gump = client.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Kdepak pane. Banka je seriozni instituce");

				client.SendGumpResponse(gump.GumpUid, 4); //Prohlednout
				gump = client.ReadGumpDialogWithStrings(Wait);

				client.SendGumpResponse(gump.GumpUid, 1); //Bankovni sluzby
				gump = client.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "A jeje. Pan z Nemanic.");
				AssertHas(gump.Strings, "Vase hotovost v bance cini 100 zlatych.");

				client.SendGumpResponse(gump.GumpUid, 1); //Otevrit banku
				uint bank = (uint) ExtractDouble(setup, "bank");
				byte[] open = client.ReadPacketPayload(0x24, Wait);
				Assert.Equal(bank & 0x3FFFFFFF, ReadUid(open, 0) & 0x3FFFFFFF);

				client.SendSpeech("zdravym");
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 1);
				gump = client.ReadGumpDialogWithStrings(Wait);
				this.Ask(client, ".TestBankerPortStep", "BANKER_STEP:");
				client.SendGumpResponse(gump.GumpUid, 1);
				Expect(client, "Behem hovoru se nesmis hybat !");
			}
		}

		[Fact]
		public void ChequeGoesFromBankToPackToBank() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerChequeChecker")) {
				this.Ask(client, ".TestBankerPortSetup \"gondor dressed 8000\"", "BANKER_SETUP:");
				client.SendDoubleClick((uint) ExtractDouble(this.Line("BANKER_SETUP:"), "pack"));
				Thread.Sleep(300);
				client.SendSpeech(".check 100");
				Expect(client, "Hodnota seku je prilis mala. Minimalni hodnota je 5000 gp.");
				client.SendSpeech(".check 9000");
				Expect(client, "V bance nemas dostatek penez. Potrebujes 9000 gp.");
				client.SendSpeech(".check(6000)");
				Expect(client, "Sek na 6000 gp byl umisten do tveho batohu.");
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				string report = this.Line("BANKER_REPORT:");
				Assert.Contains("bank=2000 ", report);
				Assert.Contains("i_cashiers_check:6000", report);

				client.SendDoubleClick((uint) ExtractDouble(report, "check"));
				Expect(client, "Proplatit sek ti muze jen banker");

				client.SendSpeech("sek");
				var gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 1);
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 2); //Proplatit upisy
				Expect(client, "Do banky bylo vlozeno 6000 gp.");
				Expect(client, "Rad jsem poslouzil.");
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				Assert.Contains("bank=8000 ", this.Line("BANKER_REPORT:"));

				this.Ask(client, ".TestBankerPortBadCheck", "BANKER_BADCHECK:");
				client.SendSpeech("sek");
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 1);
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 2);
				Expect(client, "Tvoj sek je bugly, pis page!");
			}
		}

		[Fact]
		public void PursesTakeGoldSplitAndEmpty() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankerPurseChecker")) {
				this.Ask(client, ".TestBankerPortSetup \"gondor dressed 0\"", "BANKER_SETUP:");
				client.SendDoubleClick((uint) ExtractDouble(this.Line("BANKER_SETUP:"), "pack"));
				Thread.Sleep(300);
				this.Ask(client, ".TestBankerPortGive \"3000\"", "BANKER_GOLD:");
				client.SendSpeech(".mesec 400");
				Expect(client, "Takto maly obnos nelze umistit do mesce");
				client.SendSpeech(".mesec 2000");
				Thread.Sleep(500);
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				string report = this.Line("BANKER_REPORT:");
				Assert.Contains("pack=1000 ", report);
				Assert.Contains("i_cashiers_mesec:2000", report);
				uint purse = (uint) ExtractDouble(report, "purse");

				this.Ask(client, ".TestBankerPortGive \"500\"", "BANKER_GOLD:");
				uint gold = (uint) ExtractDouble(this.Line("BANKER_GOLD:"), "uid");
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				client.SendPickUpItem(gold, 500);
				Thread.Sleep(300);
				client.SendDropItem(gold, 0xFFFF, 0xFFFF, 0, (int) purse);
				Expect(client, "Do mesce bylo vlozeno 500 gp.");

				client.SendDoubleClick(purse);
				var gump = client.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Kolik gp chces vyjmout ?");
				client.SendGumpResponse(gump.GumpUid, 1, new Dictionary<int, string> { { 1, "1000" } });
				Thread.Sleep(500);
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				report = this.Line("BANKER_REPORT:");
				Assert.Contains("i_cashiers_mesec:1500", report);
				Assert.Contains("i_cashiers_mesec:1000", report);

				client.SendDoubleClick(purse);
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, 2);
				Expect(client, "Vysypal jsi");
				this.Ask(client, ".TestBankerPortReport", "BANKER_REPORT:");
				Assert.Contains("pack=2500 ", this.Line("BANKER_REPORT:"));
			}
		}

		/// <summary>Skips other messages; a sysmessage and an NPC's speech come the same way.</summary>
		private static void Expect(TestGameClient client, string text) {
			string seen = "";
			while (true) {
				string message;
				try {
					message = client.ReadSystemMessage(Wait);
				} catch (TimeoutException e) {
					throw new TimeoutException("No '" + text + "', saw: " + seen, e);
				}
				if (message.Contains(text)) {
					return;
				}
				seen += " | " + message;
			}
		}

		private static void AssertHas(List<string> strings, string text) {
			Assert.True(strings.Exists(s => s.Contains(text)), "No '" + text + "' in: " + string.Join(" | ", strings));
		}

		private void Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
		}

		private string Line(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "Could not find '" + marker + "'");
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static uint ReadUid(byte[] payload, int offset) {
			return (uint) ((payload[offset] << 24) | (payload[offset + 1] << 16) | (payload[offset + 2] << 8) | payload[offset + 3]) & 0x7FFFFFFF;
		}

		private static double ExtractDouble(string line, string key) {
			string marker = key + "=";
			int start = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(start >= 0, "Could not find '" + marker + "' in: " + line);
			start += marker.Length;
			int end = line.IndexOf(' ', start);
			string text = (end < 0) ? line.Substring(start) : line.Substring(start, end - start);
			return double.Parse(text.Trim(), CultureInfo.InvariantCulture);
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
	public class BankerPortCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BankerPortPair";
	}
}
