using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#879, #880 - what a real client needs: raw protocol tests that only asked the
	/// server whether a box was "open" passed while nothing ever showed up on screen.</summary>
	[Collection(BankAndBookCollection.Name)]
	public class BankAndBookTests {
		private const string Account = "bankandbooktest";
		private const string Password = "bankandbooktest";

		private readonly GameServerPairFixture fixture;

		public BankAndBookTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>TazUO's OpenContainer drops a 0x24 for an item it doesn't know.</summary>
		[Fact]
		public void BankBoxIsSentBeforeItOpensAndTakesItems() {
			using (var client = this.ConnectLoginAndCreateCharacter("BankClientChecker")) {
				client.SendSpeech(".TestSetupNearbyBankerForDialogCheck");
				Thread.Sleep(500);
				client.SendSpeech("bank");
				byte[] worn = client.ReadPacketPayload(0x2E, TimeSpan.FromSeconds(10));
				while (worn[7] != 29) { //the bank box layer; the character's other items came first
					worn = client.ReadPacketPayload(0x2E, TimeSpan.FromSeconds(10));
				}
				uint bankUid = ReadUid(worn, 0);
				byte[] open = client.ReadPacketPayload(0x24, TimeSpan.FromSeconds(5));
				Assert.Equal(bankUid, ReadUid(open, 0));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGiveItemForBank");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_ITEM:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "BANK_ITEM:");
				uint itemUid = (uint) ExtractDouble(line, "uid");
				client.SendDoubleClick((uint) ExtractDouble(line, "pack")); //as a real client, which has it open
				Thread.Sleep(300);

				client.SendPickUpItem(itemUid, 1);
				Thread.Sleep(300);
				client.SendDropItem(itemUid, 30, 30, 0, (int) bankUid);
				Thread.Sleep(500);
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportBankItems");
				Assert.True(this.fixture.WaitForGameOutputSince("BANK_ITEMS:", mark, TimeSpan.FromSeconds(10)));
				Assert.Equal(1, ExtractDouble(LastLineContaining(this.fixture.GameOutputSnapshot(), "BANK_ITEMS:"), "count"));
			}
		}

		[Fact]
		public void SpellbookInTheBackpackOpens() {
			using (var client = this.ConnectLoginAndCreateCharacter("BookClientChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGiveSpellbookForOpening");
				Assert.True(this.fixture.WaitForGameOutputSince("BOOK_ITEM:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "BOOK_ITEM:");
				uint bookUid = (uint) ExtractDouble(line, "uid");
				uint packUid = (uint) ExtractDouble(line, "pack");

				client.SendDoubleClick(packUid); //as a real client, which has it open
				byte[] open = client.ReadPacketPayload(0x24, TimeSpan.FromSeconds(10));
				Assert.Equal(packUid & 0x3FFFFFFF, ReadUid(open, 0) & 0x3FFFFFFF);

				client.SendDoubleClick(bookUid);
				open = client.ReadPacketPayload(0x24, TimeSpan.FromSeconds(10));
				Assert.Equal(bookUid & 0x3FFFFFFF, ReadUid(open, 0) & 0x3FFFFFFF);
				Assert.Equal(0xFF, open[4]); //0xFFFF: a spellbook
				Assert.Equal(0xFF, open[5]);
			}
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

		private static string LastLineContaining(string output, string marker) {
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "Could not find '" + marker + "'");
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
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
	public class BankAndBookCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BankAndBookPair";
	}
}
