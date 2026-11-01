using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#956 - GoldUtils.Pay, the one way money leaves a player, and the commands going through it.</summary>
	[Collection(GoldPaymentCollection.Name)]
	public class GoldPaymentTests {
		private const string Account = "goldpayment";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public GoldPaymentTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private string Pay(TestGameClient client, int amount, string source) {
			this.Ask(client, ".TestPayReset", "PAY_RESET:");
			return this.Ask(client, ".TestPay \"" + amount + " " + source + "\"", "PAY_RESULT:");
		}

		private static void SeeMessage(TestGameClient client, string text) {
			DateTime deadline = DateTime.UtcNow + Wait;
			string seen = "";
			while (DateTime.UtcNow < deadline) {
				string message = client.ReadSystemMessage(Wait);
				if (message.Contains(text, StringComparison.Ordinal)) {
					return;
				}
				seen += message + " | ";
			}
			Assert.Fail("No '" + text + "', saw: " + seen);
		}

		[Fact]
		public void MoneyLeavesOneWay() {
			using var client = CommandPermissionTests.Connect(Account, "Gold Payer");
			Assert.Contains("pack=100 bag=200 bank=400 locked=800 gm=False", this.Ask(client, ".TestPayReset", "PAY_RESET:"));

			//the pack, its bags, then the bank
			Assert.Contains("paid=True pack=50 bag=200 bank=400 locked=800", this.Pay(client, 50, "Usual"));
			Assert.Contains("paid=True pack=0 bag=50 bank=400 locked=800", this.Pay(client, 250, "Usual"));
			Assert.Contains("paid=True pack=0 bag=0 bank=100 locked=800", this.Pay(client, 600, "Usual"));

			//all or nothing, and what's locked away isn't money to hand
			Assert.Contains("paid=False pack=100 bag=200 bank=400 locked=800", this.Pay(client, 701, "Usual"));
			SeeMessage(client, "Potrebujes 701 zlatych.");

			//one source means that source
			Assert.Contains("paid=False pack=100 bag=200 bank=400", this.Pay(client, 301, "Pack"));
			Assert.Contains("paid=True pack=0 bag=0 bank=400", this.Pay(client, 300, "Pack"));
			Assert.Contains("paid=False pack=100 bag=200 bank=400", this.Pay(client, 401, "Bank"));
			Assert.Contains("paid=True pack=100 bag=200 bank=0", this.Pay(client, 400, "Bank"));

			//a GM pays nothing, whatever the amount
			this.Ask(client, ".TestPayReset", "PAY_RESET:");
			this.Ask(client, ".TestPayGm \"on\"", "PAY_GM: True");
			Assert.Contains("paid=True pack=100 bag=200 bank=400 locked=800", this.Ask(client, ".TestPay \"999999 Usual\"", "PAY_RESULT:"));
			SeeMessage(client, "Jako GM nic neplatis.");
		}

		[Fact]
		public void AChequeDoesNotDrawOnThePack() {
			using var client = CommandPermissionTests.Connect(Account, "Gold Writer");
			this.Ask(client, ".TestPayReset", "PAY_RESET:");
			client.SendSpeech(".check 5000");
			SeeMessage(client, "V bance nemas dostatek penez. Potrebujes 5000 gp.");
			Assert.Contains("pack=100 bag=200 bank=400", this.Ask(client, ".TestPay \"0 Usual\"", "PAY_RESULT:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GoldPaymentCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GoldPayment";
	}
}
