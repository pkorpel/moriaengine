using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - .SetupHouses, the GM's tools, and who gets into a house.</summary>
	[Collection(HouseSetupCollection.Name)]
	public class HouseSetupTests {
		private const string Account = "housesetup";
		private const int Yes = 1, LockButton = 2, PublicLevelButton = 1, RattanButton = 5, FourthFacingButton = 4;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		private readonly GameServerPairFixture fixture;

		public HouseSetupTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryDwellingGetsASignAndKeepsTheRightPeopleOut() {
			using (var client = this.Connect("House Setter")) {
				//every dwelling, though most spawn points lie outside the region and have no z
				Assert.Contains("added=281 kept=0", this.Ask(client, ".SetupHouses", "SETUP_DONE:"));
				string audit = this.Ask(client, ".TestHousesAudit", "HOUSES_AUDIT:");
				Assert.Contains("regions=281 wrongArea=0 signs=281 chestsIn=278 chestsOut=3 noChest=0 signsInAir=0", audit);

				string outdoors = this.Ask(client, ".TestHouseOutdoors", "HOUSE_OUTDOORS:");
				Assert.Contains("signOutside=True", outdoors);
				uint sign = (uint) Number(outdoors, "sign");
				int x = Number(outdoors, "x"), y = Number(outdoors, "y"), z = Number(outdoors, "z");

				//a GM moves a piece, and the setup leaves it there
				client.SendSpeech(".HouseMove");
				Thread.Sleep(500);
				client.SendTargetUid(sign);
				Thread.Sleep(500);
				client.SendTargetGround(x + 1, y, z);
				Thread.Sleep(500);
				Assert.Contains("signAt=" + (x + 1) + "," + y, this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));
				Assert.Contains("added=0 kept=281", this.Ask(client, ".SetupHouses", "SETUP_DONE:"));
				Assert.Contains("signAt=" + (x + 1) + "," + y, this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));

				//every door in the dialog works, in all its facings
				Assert.Contains("kinds=19 good=19 bad=", this.Ask(client, ".TestHouseDoorKinds", "DOOR_KINDS:"));

				//a door by name, only inside a house
				client.SendSpeech(".HouseDoor \"i_door_metal\"");
				Thread.Sleep(500);
				client.SendTargetGround(Number(outdoors, "inX"), Number(outdoors, "inY"), z);
				Thread.Sleep(500);
				client.SendSpeech(".HouseDoor \"i_door_metal\"");
				Thread.Sleep(500);
				client.SendTargetGround(x, y + 3, z);
				Thread.Sleep(500);
				Assert.Contains("doors=1 doorsOutside=0", this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));

				//the dialog: rattan, the fourth way round, and one door is the end of it
				client.SendSpeech(".HouseDoor");
				var doors = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("Bambusove", doors.Strings);
				Assert.Contains("Zel. branka 1", doors.Strings);
				client.SendGumpResponse(doors.GumpUid, RattanButton);
				var facings = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains(facings.Strings, line => line.StartsWith("Bambusove", StringComparison.Ordinal));
				client.SendGumpResponse(facings.GumpUid, FourthFacingButton);
				Thread.Sleep(500);
				client.SendTargetGround(Number(outdoors, "inX") + 1, Number(outdoors, "inY"), z);
				Thread.Sleep(500);
				client.SendTargetGround(Number(outdoors, "inX") + 2, Number(outdoors, "inY"), z);
				Thread.Sleep(500);
				Assert.Contains("doors=2 doorsOutside=0", this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));
				Assert.Contains("fourthRattans=1", this.Ask(client, ".TestHouseSetupReport", "SETUP_DOOR:"));

				//the house's own realm walks in
				this.Ask(client, ".TestHousePlayer \"own\"", "HOUSE_PLAYER:");
				Assert.Contains("moved=True inside=True", this.Ask(client, ".TestHouseWalkIn", "HOUSE_WALK:"));
				Assert.Contains("landed=True", this.Ask(client, ".TestHouseTeleport", "HOUSE_TELE:"));

				//the other realm doesn't, nor teleports in, nor claims it from the street
				this.Ask(client, ".TestHousePlayer \"enemy\"", "HOUSE_PLAYER:");
				Assert.Contains("moved=False inside=False", this.Ask(client, ".TestHouseWalkIn", "HOUSE_WALK:"));
				Assert.Contains("landed=False", this.Ask(client, ".TestHouseTeleport", "HOUSE_TELE:"));
				this.Ask(client, ".TestHouseOutdoors", "HOUSE_OUTDOORS:");
				client.SendDoubleClick(sign);
				SeeMessage(client, "Tento dum nemuzes obsadit.");
				Assert.Contains("owner=none", this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));

				//its own realm claims it, and a sign outside the region still takes its level
				this.Ask(client, ".TestHousePlayer \"own\"", "HOUSE_PLAYER:");
				client.SendDoubleClick(sign);
				var gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, Yes);
				gump = client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("owner=House Setter", this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));
				client.SendGumpResponse(gump.GumpUid, LockButton);
				Thread.Sleep(500);
				client.SendTargetUid(sign);
				gump = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(gump.GumpUid, PublicLevelButton);
				Thread.Sleep(500);
				Assert.Contains("signLevel=Public", this.Ask(client, ".TestHouseSetupReport", "SETUP_REPORT:"));

				//a ghost stays out
				this.Ask(client, ".TestHouseDie", "HOUSE_DEAD:");
				Assert.Contains("moved=False inside=False", this.Ask(client, ".TestHouseWalkIn", "HOUSE_WALK:"));
			}
		}

		private static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, name + @"=(-?\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
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

		private TestGameClient Connect(string charName) {
			for (int attempt = 0; ; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Account);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
						client.SendLogin(Account, Account);
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
	public class HouseSetupCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseSetup";
	}
}
