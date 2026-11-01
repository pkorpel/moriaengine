using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - a house's bank, its death log and its short logout.</summary>
	[Collection(HouseExtrasCollection.Name)]
	public class HouseExtrasTests {
		private const string OwnerAccount = "houseextrasa", KillerAccount = "houseextrasb";
		private const int Yes = 1, BankButton = 9;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public HouseExtrasTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void BankDeathsAndLogout() {
			TestGameClient owner = this.Connect(OwnerAccount, "Extra Owner");
			this.Ask(owner, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var killer = this.Connect(KillerAccount, "Extra Killer");

			string setup = this.Ask(owner, ".TestExtrasSetup", "EXTRAS_SETUP:");
			Assert.Contains("inside=True", setup);
			uint sign = uint.Parse(Regex.Match(setup, @"sign=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);

			//it costs 100k, which isn't there yet
			this.PressBank(owner, sign);
			owner.SendGumpResponse(owner.ReadGumpDialogWithStrings(Wait).GumpUid, Yes);
			SeeMessage(owner, "Nemas dost penez.");
			Assert.Contains("bank=False gold=60000", this.Ask(owner, ".TestExtrasReport", "EXTRAS_REPORT:"));

			this.Ask(owner, ".TestExtrasMoreGold", "EXTRAS_GOLD:");
			this.PressBank(owner, sign);
			owner.SendGumpResponse(owner.ReadGumpDialogWithStrings(Wait).GumpUid, Yes);
			owner.ReadGumpDialogWithStrings(Wait); //the menu again
			Assert.Contains("bank=True gold=20000", this.Ask(owner, ".TestExtrasReport", "EXTRAS_REPORT:"));

			//opens from inside, to the house's realm
			this.PressBank(owner, sign);
			owner.ReadPacketPayload(0x24, Wait);

			owner.SendDoubleClick(sign);
			var gump = owner.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("inside=False", this.Ask(owner, ".TestExtrasStand \"outside\"", "EXTRAS_STAND:"));
			owner.SendGumpResponse(gump.GumpUid, BankButton);
			SeeMessage(owner, "Musis byt uvnitr domu.");

			this.Ask(owner, ".TestExtrasStand \"inside\"", "EXTRAS_STAND:");
			owner.SendDoubleClick(sign);
			gump = owner.ReadGumpDialogWithStrings(Wait);
			this.Ask(owner, ".TestExtrasStand \"inside enemy\"", "EXTRAS_STAND:");
			owner.SendGumpResponse(gump.GumpUid, BankButton);
			SeeMessage(owner, "K teto funkci nemas prava.");
			this.Ask(owner, ".TestExtrasStand \"inside\"", "EXTRAS_STAND:");

			//a killing in a house is told and logged, one elsewhere isn't
			int mark = this.fixture.GameOutputSnapshot().Length;
			this.Ask(killer, ".TestExtrasKill \"outside\"", "EXTRAS_KILLED:");
			Assert.DoesNotContain("DOMOVNI ZONE", this.fixture.GameOutputSnapshot().Substring(mark));
			this.Ask(killer, ".TestExtrasKill \"inside\"", "EXTRAS_KILLED:");
			Assert.Contains("Extra Owner", this.Line("DOMOVNI ZONE"));
			Assert.Contains("hracem Extra Killer", this.Line("DOMOVNI ZONE"));
			SeeMessage(owner, "Byl si zabit v dome hracem Extra Killer");

			//logging out in a house doesn't take the usual minute
			this.Ask(killer, ".TestExtrasLogoutDelay", "EXTRAS_DELAY: 2");
			owner.Dispose();
			Thread.Sleep(3500);
			Assert.Contains("disconnected=True", this.Ask(killer, ".TestExtrasLinger", "EXTRAS_LINGER:"));

			//an eviction takes the bank with it
			Assert.Contains("bank=False", this.Ask(killer, ".TestExtrasEvict", "EXTRAS_EVICTED:"));
		}

		private void PressBank(TestGameClient client, uint sign) {
			client.SendDoubleClick(sign);
			client.SendGumpResponse(client.ReadGumpDialogWithStrings(Wait).GumpUid, BankButton);
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

		private string Line(string marker) {
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "No '" + marker + "'");
			int start = output.LastIndexOf('\n', index) + 1;
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(start) : output.Substring(start, end - start);
		}

		private TestGameClient Connect(string account, string charName) {
			for (int attempt = 0; ; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
						client.SendLogin(account, account);
						client.ReadCharacterList();
						client.SendCreateCharacter(charName);
						Thread.Sleep(500);
						return client;
					}
				} catch (InvalidOperationException) when (attempt < 40) {
					Thread.Sleep(300);
				}
			}
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = this.fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HouseExtrasCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseExtras";
	}
}
