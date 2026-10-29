using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#810 - secure trade against Sphere 0.99z8's rules (EreborNext CItemContainer.Trade.cs /
	/// CClientGame.CharacterDrop.cs). Real pick-up/drop/0x6F packets between two logged-in players;
	/// SocialTests.TwoPlayersCanCompleteASecureTrade covers the plain completed trade.
	/// Server-side helpers: scripts/Tests/SecureTradeTests.cs.</summary>
	[Collection(SecureTradeCollection.Name)]
	public class SecureTradeTests {
		private const string AccountA = "securetradea";
		private const string AccountB = "securetradeb";
		private const string AccountC = "securetradec"; //each account holds 5 characters at most
		private const string AdminAccount = "securetradeadmin"; //the server's first account - can turn on auto-creation
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private static bool autoAccountCreationEnabled;
		private const uint ItemUidFlag = 0x40000000; //packets carry items' flagged uids

		private readonly GameServerPairFixture fixture;

		public SecureTradeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>The client only opens the trade gump if it knows both boxes, so both must be
		/// sent (as items on their owners) before the open packet; the offered item shows up in
		/// the initiator's box for the partner too. A cancel closes BOTH windows and returns the item.
		/// The boxes are equipped on their owners (Special layer), never lying on the ground.</summary>
		[Fact]
		public void PartnerSeesBothBoxesAndTheItem_CancelClosesBothAndReturnsIt() {
			using (var a = this.Connect(AccountA, "Trader Alpha"))
			using (var b = this.Connect(AccountB, "Trader Beta")) {
				Setup setup = this.SetupTrader(a);
				uint bUid = this.MyUid(b);

				this.Offer(a, setup, setup.ItemA, bUid);

				var boxOfA = b.ReadContainerItem((item, model, cont) => model == 0x1e5e && cont == setup.Me, Wait);
				var boxOfB = b.ReadContainerItem((item, model, cont) => model == 0x1e5e && cont == bUid, Wait);
				var (myBoxB, otherBoxB, nameForB) = b.ReadSecureTradeOpen(Wait);
				Assert.Equal(boxOfB.ItemUid, myBoxB);
				Assert.Equal(boxOfA.ItemUid, otherBoxB);
				Assert.Equal("Trader Alpha", nameForB);
				b.ReadContainerItem((item, model, cont) => (item & ~ItemUidFlag) == setup.ItemA && cont == boxOfA.ItemUid, Wait);

				var (myBoxA, _, _) = a.ReadSecureTradeOpen(Wait);
				string state = this.State(a, "Trader Alpha");
				AssertHas(state, "trades=1");
				AssertHas(state, "myBoxOnMe=True myBoxLayer=30 myBoxCount=1");

				a.SendSecureTradeCancel(myBoxA);
				Assert.Equal(myBoxA, a.ReadSecureTradeClose(Wait));
				Assert.Equal(myBoxB, b.ReadSecureTradeClose(Wait));
				AssertHas(this.State(a, "Trader Alpha"), "trades=0");
				string item = this.Item(a, setup.ItemA);
				AssertHas(item, "holder=Trader Alpha inPack=True inTradeBox=False dragging=False");
			}
		}

		/// <summary>The partner can't take items out of my box. Taking my own item back out
		/// unticks both accepts, and the partner then has to wait ~2 s before accepting again
		/// (Sphere's anti-swap stabilize timer), after which accepting works.</summary>
		[Fact]
		public void PartnerCannotTakeMyItem_TakingItBackResetsAndDelaysAccept() {
			using (var a = this.Connect(AccountA, "Trader Gamma"))
			using (var b = this.Connect(AccountB, "Trader Delta")) {
				Setup setup = this.SetupTrader(a);
				uint bUid = this.MyUid(b);
				this.Offer(a, setup, setup.ItemA, bUid);
				var (myBoxA, _, _) = a.ReadSecureTradeOpen(Wait);
				var (myBoxB, _, _) = b.ReadSecureTradeOpen(Wait);

				b.SendPickUpItem(setup.ItemA, 1);
				Thread.Sleep(500);
				AssertHas(this.Item(a, setup.ItemA), "holder=Trader Gamma inPack=False inTradeBox=True dragging=False");

				b.SendSecureTradeAccept(myBoxB, true);
				Thread.Sleep(300);
				AssertHas(this.State(b, "Trader Delta"), "myAccepted=True");

				a.SendPickUpItem(setup.ItemA, 1);
				Thread.Sleep(300);
				a.SendDropItem(setup.ItemA, 0xFFFF, 0xFFFF, 0, (int) setup.Pack);
				Thread.Sleep(300);
				AssertHas(this.Item(a, setup.ItemA), "holder=Trader Gamma inPack=True");
				AssertHas(this.State(b, "Trader Delta"), "myAccepted=False partnerAccepted=False");

				b.SendSecureTradeAccept(myBoxB, true);
				Thread.Sleep(300);
				AssertHas(this.State(b, "Trader Delta"), "myAccepted=False");

				Thread.Sleep(2500);
				b.SendSecureTradeAccept(myBoxB, true);
				Thread.Sleep(300);
				AssertHas(this.State(b, "Trader Delta"), "myAccepted=True");

				//log out while holding my own item taken back out of the box (trade still open)
				this.Offer(a, setup, setup.ItemB, bUid);
				Thread.Sleep(300);
				AssertHas(this.Item(b, setup.ItemB), "inTradeBox=True");
				a.SendPickUpItem(setup.ItemB, 1);
				Thread.Sleep(300);
				AssertHas(this.Item(b, setup.ItemB), "dragging=True");
				a.Dispose();
				b.ReadSecureTradeClose(Wait);
				Thread.Sleep(500);
				AssertHas(this.Item(b, setup.ItemB), "holder=Trader Gamma inPack=True inTradeBox=False dragging=False");
			}
		}

		/// <summary>A trade only starts within reach: from 8 tiles away the drop is refused, no
		/// window opens and the item goes back to the pack (not stuck in the hand).</summary>
		[Fact]
		public void TradeDoesNotStartOutOfReach() {
			using (var a = this.Connect(AccountA, "Trader Epsilon"))
			using (var b = this.Connect(AccountB, "Trader Zeta")) {
				Setup setup = this.SetupTrader(a);
				uint bUid = this.MyUid(b);
				b.SendSpeech(".TestTradeStepAway");
				Thread.Sleep(500);

				this.Offer(a, setup, setup.ItemA, bUid);
				Thread.Sleep(500);
				AssertHas(this.State(a, "Trader Epsilon"), "trades=0");
				AssertHas(this.Item(a, setup.ItemA), "holder=Trader Epsilon inPack=True inTradeBox=False dragging=False");
			}
		}

		/// <summary>Dying mid-trade cancels it (Sphere deletes trade windows on death; Erebor's
		/// @death logs it): the partner's window closes and the item is out of the trade box.
		/// Logging out mid-trade does the same (Sphere's CancelAllTrades on client detach).</summary>
		[Fact]
		public void DeathOrLogoutCancelsTheTrade() {
			using (var b = this.Connect(AccountB, "Trader Eta")) {
				uint bUid = this.MyUid(b);
				Setup setup;
				using (var a = this.Connect(AccountA, "Trader Theta")) {
					setup = this.SetupTrader(a);
					this.Offer(a, setup, setup.ItemA, bUid);
					a.ReadSecureTradeOpen(Wait);
					var (myBoxB, _, _) = b.ReadSecureTradeOpen(Wait);

					a.SendSpeech(".TestTradeKillMe");
					Assert.Equal(myBoxB, b.ReadSecureTradeClose(Wait));
					AssertHas(LastLineContaining(this.fixture.GameOutputSnapshot(), "SecureTrade: Trader Theta"), "died during an active trade with Trader Eta");
					string item = this.Item(b, setup.ItemA);
					AssertHas(item, "inTradeBox=False dragging=False");
				}

				using (var a = this.Connect(AccountC, "Trader Iota")) {
					setup = this.SetupTrader(a);
					this.Offer(a, setup, setup.ItemA, bUid);
					a.ReadSecureTradeOpen(Wait);
					var (myBoxB, _, _) = b.ReadSecureTradeOpen(Wait);
					a.SendPickUpItem(setup.ItemB, 1); //holding something from the pack while logging out
					Thread.Sleep(300);
					a.Dispose(); //logs out
					Assert.Equal(myBoxB, b.ReadSecureTradeClose(Wait));
				}
				AssertHas(this.Item(b, setup.ItemA), "holder=Trader Iota inPack=True inTradeBox=False");
				AssertHas(this.Item(b, setup.ItemB), "holder=Trader Iota inPack=True inTradeBox=False dragging=False");
			}
		}

		/// <summary>A trade box left behind with no live trade (the server went down mid-trade -
		/// boxes are saved with their owner) is emptied into the owner's pack at their next login.</summary>
		[Fact]
		public void OrphanedTradeBoxIsReturnedAtLogin() {
			const string name = "Trader Kappa";
			uint itemUid;
			using (var a = this.Connect(AccountA, name)) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				a.SendSpeech(".TestTradeMakeOrphanBox");
				Assert.True(this.fixture.WaitForGameOutputSince("TRADE_ORPHAN:", mark, Wait));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "TRADE_ORPHAN:");
				AssertHas(line, "boxOnMe=True");
				itemUid = ExtractUidAfter(line, "item=");
				AssertHas(this.Item(a, itemUid), "inTradeBox=True");
			}

			using (var a = this.Relogin(AccountA, name)) {
				Thread.Sleep(500);
				AssertHas(this.Item(a, itemUid), "holder=" + name + " inPack=True inTradeBox=False");
			}
		}

		private struct Setup {
			public uint Me, Pack, ItemA, ItemB;
		}

		private Setup SetupTrader(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTradeSetup");
			Assert.True(this.fixture.WaitForGameOutputSince("TRADE_SETUP2:", mark, Wait));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "TRADE_SETUP2:");
			Setup s = new Setup {
				Me = ExtractUidAfter(line, "me="), Pack = ExtractUidAfter(line, "pack="),
				ItemA = ExtractUidAfter(line, "itemA="), ItemB = ExtractUidAfter(line, "itemB=")
			};
			client.SendDoubleClick(s.Pack); //open own pack first - needed for reachability
			Thread.Sleep(300);
			return s;
		}

		private void Offer(TestGameClient client, Setup setup, uint item, uint partner) {
			client.SendPickUpItem(item, 1);
			Thread.Sleep(300);
			client.SendDropItem(item, 0, 0, 0, (int) partner);
		}

		private uint MyUid(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, Wait));
			string output = this.fixture.GameOutputSnapshot();
			int idx = output.IndexOf("MY_UID:", mark, StringComparison.Ordinal);
			return ExtractUidAfter(output.Substring(idx), "MY_UID:");
		}

		private string State(TestGameClient client, string charName) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTradeState");
			string marker = "TRADE_STATE: " + charName;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait));
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
		}

		private string Item(TestGameClient client, uint item) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTradeItem " + item);
			string marker = "TRADE_ITEM: uid=" + item;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait));
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
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

		private static uint ExtractUidAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			string rest = line.Substring(idx + marker.Length).TrimStart(' ', '#');
			int end = 0;
			while ((end < rest.Length) && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectOnce(AdminAccount, "Trade Admin")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, Wait));
			}
			autoAccountCreationEnabled = true;
		}

		private TestGameClient Connect(string account, string charName) {
			this.EnsureAutoAccountCreationEnabled();
			return this.ConnectOnce(account, charName);
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private TestGameClient ConnectOnce(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
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

		private TestGameClient Relogin(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
					string[] names = game.ReadCharacterList();
					int slot = Array.IndexOf(names, charName);
					Assert.True(slot >= 0, "Expected '" + charName + "' in the character list.");
					game.SendSelectCharacter(slot);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log back in as '" + charName + "'.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(SecureTradeCollection.Name, DisableParallelization = true)]
	public class SecureTradeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SecureTradePair";
	}
}
