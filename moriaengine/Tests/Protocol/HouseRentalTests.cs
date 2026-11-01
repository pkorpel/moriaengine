using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - .rentalinfo, who sees what, and the bans from it.</summary>
	[Collection(HouseRentalCollection.Name)]
	public class HouseRentalTests {
		private const string OwnerAccount = "houserentala", ViewerAccount = "houserentalb";
		private const int TownBanOfFirstHouse = 2000, RealmBanOfFirstHouse = 1000, FriendsOfFirstHouse = 4000, OkButton = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public HouseRentalTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RentalInfoShowsEachOfficialTheirColumns() {
			using var viewer = this.Connect(ViewerAccount, "Rental Viewer");
			this.Ask(viewer, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var owner = this.Connect(OwnerAccount, "Rental Owner");
			this.Ask(viewer, ".SetupTowns", "SETUP_DONE:");
			Assert.Contains("city=Dol_Amroth here=Dol_Amroth", this.Ask(owner, ".TestRentalOwner", "RENTAL_OWNER:"));

			//nobody in particular gets nothing
			this.Ask(viewer, ".TestRentalViewer \"nobody\"", "RENTAL_VIEWER:");
			viewer.SendSpeech(".TestRentalShow");
			SeeMessage(viewer, "Nemas na to pravo.");

			//a mayor sees owners and town bans, not realm bans or members
			this.Ask(viewer, ".TestRentalViewer \"mayor\"", "RENTAL_VIEWER: mayor=True");
			viewer.SendSpeech(".TestRentalShow");
			var gump = viewer.ReadGumpDialogWithStrings(Wait);
			Assert.Contains(gump.Strings, s => s.Contains("Pocet domu v oblasti Dol_Amroth", StringComparison.Ordinal));
			Assert.Contains("Dum 4", gump.Strings);
			Assert.Contains("Rental Owner", gump.Strings);
			Assert.Contains("TownBan", gump.Strings);
			Assert.DoesNotContain("RealmBan", gump.Strings);
			Assert.DoesNotContain("Spoluvl.", gump.Strings);

			//a town ban and back
			viewer.SendGumpResponse(gump.GumpUid, TownBanOfFirstHouse);
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			viewer.SendGumpResponse(gump.GumpUid, OkButton, new System.Collections.Generic.Dictionary<int, string> { { OkButton, "dluh" } });
			Thread.Sleep(500);
			Assert.Contains("townBan=True", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));
			viewer.SendSpeech(".TestRentalShow");
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			viewer.SendGumpResponse(gump.GumpUid, TownBanOfFirstHouse);
			Thread.Sleep(500);
			Assert.Contains("townBan=False", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));

			//a GM sees all columns, by the real command, and a house's friends
			this.Ask(viewer, ".TestRentalGm", "RENTAL_GM: True");
			this.Ask(viewer, ".TestRentalOffice", "RENTAL_OFFICE: True");
			viewer.SendSpeech(".rentalinfo \"Dol_Amroth\"");
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			viewer.SendGumpResponse(gump.GumpUid, RealmBanOfFirstHouse);
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			viewer.SendGumpResponse(gump.GumpUid, OkButton, new System.Collections.Generic.Dictionary<int, string> { { OkButton, "zrada" } });
			Thread.Sleep(500);
			Assert.Contains("realmBan=True", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));
			viewer.SendSpeech(".rentalinfo \"Dol_Amroth\"");
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			viewer.SendGumpResponse(gump.GumpUid, RealmBanOfFirstHouse);
			Thread.Sleep(500);
			Assert.Contains("realmBan=False", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));

			viewer.SendSpeech(".rentalinfo \"Dol_Amroth\"");
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("RealmBan", gump.Strings);
			Assert.Contains("Spoluvl.", gump.Strings);
			Assert.Contains("Pratele", gump.Strings);
			viewer.SendGumpResponse(gump.GumpUid, FriendsOfFirstHouse);
			gump = viewer.ReadGumpDialogWithStrings(Wait);
			Assert.Contains("Rental Viewer", gump.Strings);

			//a GM's command makes the owner of a house a citizen
			Assert.Contains("citizen=False", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));
			viewer.SendSpeech(".HouseCitizens \"Dol_Amroth\"");
			Thread.Sleep(500);
			Assert.Contains("citizen=True", this.Ask(viewer, ".TestRentalReport", "RENTAL_REPORT:"));
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
	public class HouseRentalCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseRental";
	}
}
