using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1070 - auctions survive a real save and restart: the hidden box is found again by its tag, both listings keep
	/// their seller, bidder, bid, prices, realm and end, the box keeps its own sweep timer, and that timer settles them both
	/// to the banks afterwards. Server side in scripts/Tests/AuctionTests.cs.</summary>
	[Collection(AuctionRestartCollection.Name)]
	public class AuctionRestartTests {
		private const string Account = "auctionrestart";
		private const string Password = "auctionrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan SweepWait = TimeSpan.FromSeconds(100);

		[Fact]
		public void AuctionsSurviveARealRestart() {
			string ends;
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Auction Saver")) {
					string ready = Ask(fixture1, client, ".TestAuctionRestartPrepare", "AUC_RESTART_READY");
					AssertHas(ready, "listedA=True listedB=True bid=1500 bidderGold=18500 ");
					ends = Field(ready, "ends=");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Auction Loader")) {
					string check = Ask(fixture2, client, ".TestAuctionRestartCheck", "AUC_RESTART_CHECK");
					AssertHas(check, "box=True timer=True listedA=True listedB=True magic=True seller=Auction_Saver bidder=Auction_Bidder realm=Gondor " +
						"start=1000 buyout=9000 bid=1500 bidderGold=18500 ends=" + ends + " ");

					//the restored timer, not a test call, settles them; it fires within its minute
					string settled = null;
					DateTime until = DateTime.UtcNow + SweepWait;
					while (DateTime.UtcNow < until) {
						settled = Ask(fixture2, client, ".TestAuctionRestartSettled", "AUC_RESTART_SETTLED");
						if (settled.Contains("settled=True", StringComparison.Ordinal)) {
							break;
						}
						Thread.Sleep(5000);
					}
					AssertHas(settled, "settled=True aTo=Auction_Bidder aInBank=True bTo=Auction_Saver bInBank=True sellerBankGold=1500 ");
				}
			}
		}

		private static string Field(string line, string key) {
			int idx = line.IndexOf(key, StringComparison.Ordinal);
			Assert.True(idx >= 0, "no '" + key + "' in: " + line);
			int start = idx + key.Length;
			int end = line.IndexOf(' ', start);
			return (end < 0) ? line.Substring(start).Trim() : line.Substring(start, end - start);
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture, Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Password);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech + "\n" + fixture.GameOutputSnapshot());
			return LastLineContaining(fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True((line != null) && line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture, string account) {
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class AuctionRestartCollection {
		public const string Name = "AuctionRestart";
	}
}
