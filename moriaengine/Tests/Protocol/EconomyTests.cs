using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Shared plumbing for V-3 (#71) economy scenarios. Split across two fresh collections for
	/// the same reason as the earlier verification phases - the account's 5-character-slot cap.
	/// </summary>
	public abstract class EconomyTestBase {
		protected const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		protected EconomyTestBase(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		protected string GameOutputSnapshot() {
			return this.fixture.GameOutputSnapshot();
		}

		protected void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.WaitFor(marker, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		protected bool WaitFor(string marker, TimeSpan timeout) {
			return this.fixture.WaitForGameOutput(marker, timeout);
		}

		protected bool WaitForSince(string marker, int since, TimeSpan timeout) {
			return this.fixture.WaitForGameOutputSince(marker, since, timeout);
		}

		/// <summary>Current length of the fixture's accumulated output - capture this BEFORE
		/// triggering whatever action a later wait/check needs to be scoped to, so a marker that
		/// more than one test can print (like "GOLD_CHECK:") can't be satisfied by an earlier
		/// test's leftover occurrence. See GameServerPairFixture.WaitForGameOutputSince.</summary>
		protected int OutputMark() {
			return this.fixture.GameOutputSnapshot().Length;
		}

		/// <summary>Waits for a "GOLD_CHECK: backpack=X bank=Y" line printed at or after `since`
		/// and parses out X/Y.</summary>
		protected (long Backpack, long Bank) WaitForGoldCheck(int since, TimeSpan timeout) {
			Assert.True(this.fixture.WaitForGameOutputSince("GOLD_CHECK:", since, timeout),
				"Expected a GOLD_CHECK line after position " + since + ". Game output:\n" + this.fixture.GameOutputSnapshot());
			string output = this.fixture.GameOutputSnapshot();
			int idx = output.LastIndexOf("GOLD_CHECK: backpack=", StringComparison.Ordinal);
			Assert.True(idx >= since, "Could not find a fresh GOLD_CHECK line (after position " + since + ") in:\n" + output);
			string rest = output.Substring(idx + "GOLD_CHECK: backpack=".Length);
			long backpack = ParseLeadingNumber(rest);
			int bankIdx = rest.IndexOf("bank=", StringComparison.Ordinal);
			long bank = ParseLeadingNumber(rest.Substring(bankIdx + "bank=".Length));
			return (backpack, bank);
		}

		private static long ParseLeadingNumber(string s) {
			int end = 0;
			while (end < s.Length && char.IsDigit(s[end])) {
				end++;
			}
			return long.Parse(s.Substring(0, end), CultureInfo.InvariantCulture);
		}

		internal TestGameClient ConnectAndRun(string charName, params string[] speechCommands) {
			TestGameClient game = this.ConnectLoginAndCreateCharacter(charName);
			foreach (string command in speechCommands) {
				Thread.Sleep(300);
				game.SendSpeech(command);
			}
			return game;
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 20; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(TestAccount, TestPassword);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(250);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		private int WaitForServerListingAndGetRedirectPort() {
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class EconomyCollectionA : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "EconomyPairA";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class EconomyCollectionB : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "EconomyPairB";
	}

	[Collection(EconomyCollectionA.Name)]
	public class EconomyTestsA : EconomyTestBase {
		public EconomyTestsA(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>Real UO rule: speaking "deposit N" to a banker moves gold from backpack to bank.</summary>
		[Fact]
		public void DepositMovesGoldFromBackpackToBank() {
			using (var game = this.ConnectAndRun("EcoDeposit", ".TestSetupBankerAndGold")) {
				Thread.Sleep(500); // let the freshly-spawned banker's own AI/placement settle
				game.SendSpeech("deposit 200");
				Thread.Sleep(500);
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, bank) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.Equal(300, backpack);
				Assert.Equal(200, bank);
			}
		}

		/// <summary>Real UO rule: speaking "withdraw N" to a banker moves gold from bank to backpack.</summary>
		[Fact]
		public void WithdrawMovesGoldFromBankToBackpack() {
			using (var game = this.ConnectAndRun("EcoWithdraw", ".TestSetupBankerAndBankGold")) {
				Thread.Sleep(500);
				game.SendSpeech("withdraw 200");
				Thread.Sleep(500);
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, bank) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.Equal(200, backpack);
				Assert.Equal(300, bank);
			}
		}

		// Direct-double-click-your-own-bankbox tests were dropped here, not just left failing -
		// see #71's writeup: AbstractCharacter.sentLayers (25) vs LayerNames.Bankbox (29) means
		// CanSeeForUpdate ALWAYS denies a bankbox as an "invis layer" regardless of GM status,
		// so BankBox.On_DClick's own GM-exemption code is unreachable via the real wire protocol
		// in this engine - a real client can never successfully double-click its own bankbox at
		// all (classic UO's paperdoll bank icon is a different, dedicated interaction this
		// codebase doesn't implement). Deposit/Withdraw above already cover the real, reachable
		// in-game path (via a banker's speech trigger).
	}

	[Collection(EconomyCollectionB.Name)]
	public class EconomyTestsB : EconomyTestBase {
		public EconomyTestsB(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>Real UO rule: "sell" + target an item in your own backpack pays
		/// NpcVendorStock's SellPrice for it (i_bandage: buy 2, sell 1).</summary>
		[Fact]
		public void VendorSellGrantsGoldForItem() {
			using (var game = this.ConnectAndRun("EcoSell", ".TestSetupVendorAndSellableItem")) {
				Assert.True(this.WaitFor("sellable item #", TimeSpan.FromSeconds(10)));
				string setupOutput = this.GameOutputSnapshot();
				uint itemUid = ExtractSellableItemUid(setupOutput);
				// Targeting an item inside a container requires that container to already be
				// "open" (CanSeeImplementation.CanReachFromAt -> OpenedContainers.
				// HasContainerOpenFromAt) - a real client auto-opens its own backpack on login,
				// which this minimal test client never does on its own, so it has to be done
				// explicitly here (double-clicking it is the real mechanism - see
				// AbstractItem.OpenTo's call to OpenedContainers.SetContainerOpened).
				uint backpackUid = ExtractBackpackUid(setupOutput);
				game.SendDoubleClick(backpackUid);
				Thread.Sleep(500);
				game.SendSpeech("sell");
				Thread.Sleep(500);
				game.SendTargetUid(itemUid);
				Thread.Sleep(500);
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, _) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.True(1 == backpack, "backpack=" + backpack + ". Game output:\n" + this.GameOutputSnapshot()); // i_bandage sells for BuyPrice/2 = 1
			}
		}

		/// <summary>
		/// Real UO rule: "buy" opens a shop gump; picking an entry deducts its BuyPrice and
		/// grants the item. D_VendorShop.cs assigns each stock row's buy button a deterministic
		/// id (10 + its index into NpcVendorStock.Entries, verified by reading that class's own
		/// Construct() method) - not a generic assumption about gump button numbering in
		/// general, just this one concrete, source-verified dialog. i_bandage is index 1
		/// (torch=0, bandage=1, dagger_verite=2), so its buy button is id 11.
		/// </summary>
		[Fact]
		public void VendorBuyDeductsGoldAndGrantsItem() {
			const int bandageButtonId = 11;
			using (var game = this.ConnectAndRun("EcoBuy", ".TestSetupVendorOnly")) {
				GoldUtils_Grant(game);
				Thread.Sleep(500);
				game.SendSpeech("buy");
				var (gumpUid, _) = game.ReadGumpDialog(TimeSpan.FromSeconds(10));
				game.SendGumpResponse(gumpUid, bandageButtonId);
				Thread.Sleep(500);
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, _) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.Equal(498, backpack); // 500 granted - 2 (i_bandage's BuyPrice)
			}
		}

		/// <summary>Buying with zero gold on hand must be rejected, not silently grant the item anyway.</summary>
		[Fact]
		public void VendorBuyRejectsInsufficientFunds() {
			const int bandageButtonId = 11;
			using (var game = this.ConnectAndRun("EcoBuyPoor", ".TestSetupVendorOnly")) {
				Thread.Sleep(500);
				game.SendSpeech("buy");
				var (gumpUid, _) = game.ReadGumpDialog(TimeSpan.FromSeconds(10));
				game.SendGumpResponse(gumpUid, bandageButtonId);
				string refused = "";
				while (!refused.Contains("not have enough gold", StringComparison.Ordinal)) {
					refused = game.ReadSystemMessage(TimeSpan.FromSeconds(10));
				}
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, _) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.Equal(0, backpack); // never had any gold - purchase must have been rejected, not granted on credit
			}
		}

		/// <summary>Real UO rule (this engine's own BankOnly mode, #36): with PaymentSource set to
		/// BankOnly, a purchase draws strictly from the bank even when the backpack also has
		/// enough gold on its own - the backpack amount must stay untouched.</summary>
		[Fact]
		public void PaymentSourceBankOnlyDrawsFromBankNotBackpack() {
			const int bandageButtonId = 11;
			using (var game = this.ConnectAndRun("EcoPaySource", ".PaymentSource BankOnly", ".TestSetupVendorOnly", ".TestGrantBackpackAndBankGold")) {
				Thread.Sleep(500);
				game.SendSpeech("buy");
				var (gumpUid, _) = game.ReadGumpDialog(TimeSpan.FromSeconds(10));
				game.SendGumpResponse(gumpUid, bandageButtonId);
				Thread.Sleep(500);
				int mark = this.OutputMark();
				game.SendSpeech(".TestCheckGold");
				var (backpack, bank) = this.WaitForGoldCheck(mark, TimeSpan.FromSeconds(10));
				Assert.Equal(500, backpack); // untouched
				Assert.Equal(498, bank); // 500 - 2
				game.SendSpeech(".PaymentSource BackpackFirst"); // restore the default for any later test sharing this account
				Thread.Sleep(300); // give the server time to process the restore before this connection closes
			}
		}

		private static void GoldUtils_Grant(TestGameClient game) {
			game.SendSpeech(".TestGrantBackpackAndBankGold");
		}

		private static uint ExtractSellableItemUid(string output) {
			int idx = output.LastIndexOf("sellable item #", StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find sellable item uid in:\n" + output);
			string rest = output.Substring(idx + "sellable item #".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
		}

		private static uint ExtractBackpackUid(string output) {
			int idx = output.LastIndexOf("backpack #", StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find backpack uid in:\n" + output);
			string rest = output.Substring(idx + "backpack #".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
		}
	}
}
