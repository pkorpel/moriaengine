using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#889 - the gold, weight and capacity a client's status window shows, and the bank's gump.
	/// All read from the packets, as a client gets them.</summary>
	[Collection(BankStatusCollection.Name)]
	public class BankStatusTests {
		private const string Account = "bankstatustest";
		private const int GoldOffset = 57, WeightOffset = 63, MaxWeightOffset = 65, FlagOffset = 41;
		private const int ChestGump = 0x42;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public BankStatusTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StatusShowsGoldWeightAndCapacity() {
			using (var client = this.Connect("Bank Status Tester")) {
				double baseWeight = Number(this.Ask(client, ".TestBankStatusBase", "BANK_BASE:"), "weight");

				string gold = this.Ask(client, ".TestBankStatusGold", "BANK_GOLD:");
				//1000 coins are 2 stones, and the 5000 in the bank are nothing
				Assert.Equal(baseWeight + 2, Number(gold, "weight"), 2);
				Assert.Equal(Number(gold, "weight"), Number(gold, "recounted"), 2);
				Assert.Contains("gold=6000", gold);
				Assert.Equal((int) ((baseWeight + 2) * 100 / 430), (int) Number(gold, "load"));

				byte[] status = ReadStatusWithGold(client, 6000);
				Assert.Equal(5, status[FlagOffset]);
				Assert.Equal((int) (baseWeight + 2), Word(status, WeightOffset));
				Assert.Equal(430, Word(status, MaxWeightOffset)); //Str 100 * 4 + 30
			}
		}

		[Fact]
		public void StackWeighsItsAmountAndNothingStaysBehind() {
			using (var client = this.Connect("Bank Stack Tester")) {
				string stack = this.Ask(client, ".TestBankStatusStack", "BANK_STACK:");
				Assert.Equal(1, Number(stack, "ten"), 2);
				Assert.Equal(3, Number(stack, "thirty"), 2);
				Assert.Equal(0, Number(stack, "after"), 2);
			}
		}

		[Fact]
		public void SplittingAPileAndDupingABagKeepTheCount() {
			using (var client = this.Connect("Bank Pile Tester")) {
				string pile = this.Ask(client, ".TestBankStatusPile", "BANK_PILE:");
				uint uid = (uint) Number(pile, "uid"), pack = (uint) Number(pile, "pack");
				client.SendDoubleClick(pack);
				Thread.Sleep(300);
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendPickUpItem(uid, 40);
				Thread.Sleep(300);
				client.SendDropItem(uid, 50, 50, 0, (int) pack);
				Thread.Sleep(300);
				string counts = this.Ask(client, ".TestBankStatusCounts", "BANK_COUNTS:");
				Assert.Contains("gold=100", counts);
				Assert.Equal(Number(counts, "weight"), Number(counts, "recounted"), 2);

				string dupe = this.Ask(client, ".TestBankStatusBagDupe", "BANK_DUPE:");
				Assert.Contains("bagAlive=True", dupe);
				Assert.DoesNotContain("Limbo", this.fixture.GameOutputSnapshot().Substring(mark));
			}
		}

		[Fact]
		public void DefWeightsAreStonesAndUnsetOnesComeFromTiledata() {
			using (var client = this.Connect("Bank Defs Tester")) {
				string defs = this.Ask(client, ".TestBankStatusDefs", "BANK_DEFS:");
				Assert.Equal(3, Number(defs, "backpack"), 2); //WEIGHT=3.0
				Assert.Equal(0.1, Number(defs, "lockpick"), 2); //WEIGHT=.1
				double wanted = Number(defs, "wanted");
				Assert.Equal((wanted == 255) ? 1 : wanted, Number(defs, "tiledata"), 2);
			}
		}

		[Fact]
		public void BankOpensAsAChest() {
			using (var client = this.Connect("Bank Gump Tester")) {
				client.SendSpeech(".TestBankStatusOpen");
				byte[] open = client.ReadPacketPayload(0x24, Wait);
				Assert.Equal(ChestGump, Word(open, 4));
			}
		}

		private static int Word(byte[] payload, int offset) {
			return (payload[offset] << 8) | payload[offset + 1];
		}

		/// <summary>The bank's gold has to reach the status by itself, nothing here asks for one.</summary>
		private static byte[] ReadStatusWithGold(TestGameClient client, int gold) {
			DateTime deadline = DateTime.UtcNow + Wait;
			List<int> seen = new List<int>();
			while (DateTime.UtcNow < deadline) {
				byte[] status = client.ReadPacketPayload(0x11, deadline - DateTime.UtcNow);
				if (status.Length > GoldOffset + 3) {
					int shown = (status[GoldOffset] << 24) | (status[GoldOffset + 1] << 16) | (status[GoldOffset + 2] << 8) | status[GoldOffset + 3];
					if (shown == gold) {
						return status;
					}
					seen.Add(shown);
				}
			}
			throw new TimeoutException("No status with " + gold + " gold, saw: " + string.Join(",", seen));
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}

		private static double Number(string line, string key) {
			string marker = key + "=";
			int start = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(start >= 0, "Could not find '" + marker + "' in: " + line);
			start += marker.Length;
			int end = line.IndexOf(' ', start);
			string text = (end < 0) ? line.Substring(start) : line.Substring(start, end - start);
			return double.Parse(text.Trim(), CultureInfo.InvariantCulture);
		}

		private TestGameClient Connect(string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Account);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					Thread.Sleep(500);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private static int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);

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
	public class BankStatusCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BankStatus";
	}
}
