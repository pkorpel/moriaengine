using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1004 - Sphere's picture menu beside the dialogs, and .menustyle to flip between them.</summary>
	[Collection(MenuStyleCollection.Name)]
	public class MenuStyleTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public MenuStyleTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, string text) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (client.ReadSystemMessage(deadline - DateTime.UtcNow) != text) {
			}
		}

		//the seeking stroke can fail
		private (int MenuUid, string Header, System.Collections.Generic.List<(int Model, int Color, string Text)> Rows) TrackAnimals(TestGameClient client) {
			for (int tries = 0; ; tries++) {
				this.Ask(client, ".TestMenuTrack", "MENU_TRACK:");
				var categories = client.ReadItemMenu(Wait);
				client.SendItemMenuChoice(categories.MenuUid, 1);
				try {
					return client.ReadItemMenu(TimeSpan.FromSeconds(4));
				} catch (TimeoutException) when (tries < 5) {
				}
			}
		}

		[Fact]
		public void BothWaysAndTheSwitch() {
			using var client = CommandPermissionTests.Connect("menutest", "Menu Tester");
			string ready = this.Ask(client, ".TestMenuReady", "MENU_READY:");
			string rabbit = Regex.Match(ready, @"rabbit=(.*?) model=").Groups[1].Value;
			string daemon = "model=" + Regex.Match(ready, @"daemon=(\d+)").Groups[1].Value + " ";
			string own = "model=" + Regex.Match(ready, @" model=(\d+)").Groups[1].Value + " ";

			//dialogs by default, and a player can flip it
			this.Ask(client, ".TestMenuTrack", "MENU_TRACK:");
			var gump = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(gump.GumpUid, 0);
			this.Ask(client, ".TestMenuPlevel \"1\"", "MENU_PLEVEL:");
			client.SendSpeech(".menustyle");
			Expect(client, "Menu se ted otevira jako obrazkove menu.");

			//Sphere's five rows with pictures, a cancel ends the skill
			this.Ask(client, ".TestMenuTrack", "MENU_TRACK:");
			var menu = client.ReadItemMenu(Wait);
			Assert.Equal("Tracking", menu.Header);
			Assert.Equal(new[] { "Animals", "Monsters", "Humans", "Players", "Anything that moves" }, menu.Rows.Select(row => row.Text));
			Assert.Equal(new[] { 0x2120, 0x20df, 0x2106, 0x20cd, 0x2100 }, menu.Rows.Select(row => row.Model));
			client.SendItemMenuChoice(menu.MenuUid, 0);
			Thread.Sleep(300);
			Assert.Contains("skill=False", this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));

			//a pick of the names tracks, which 99z8 didn't, and each row has a picture for the client to take it as a picture menu
			var names = this.TrackAnimals(client);
			int row = names.Rows.FindIndex(name => name.Text == rabbit);
			Assert.True(row >= 0, rabbit + " not among " + string.Join(", ", names.Rows.Select(name => name.Text)));
			Assert.All(names.Rows, name => Assert.NotEqual(0, name.Model));
			client.SendItemMenuChoice(names.MenuUid, row + 1);
			Thread.Sleep(1500);
			Assert.Contains("tracking=True", this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));
			this.Ask(client, ".TestMenuEnd", "MENU_END:");

			//a second answer to the same menu does nothing
			client.SendItemMenuChoice(names.MenuUid, row + 1);
			Thread.Sleep(1500);
			Assert.Contains("tracking=False", this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));

			//Summon's rows, as a GM for the staves and the skill
			this.Ask(client, ".TestMenuPlevel \"7\"", "MENU_PLEVEL:");
			this.Ask(client, ".TestMenuCast \"s_summon_creature\"", "MENU_CAST:");
			var summon = client.ReadItemMenu(Wait);
			Assert.Equal("What do you want to summon ?", summon.Header);
			Assert.Contains(summon.Rows, one => one.Text == "Black Bear");
			Assert.All(summon.Rows, one => Assert.NotEqual(0, one.Model));
			client.SendItemMenuChoice(summon.MenuUid, 0);

			//both at once: the first answer counts, from either
			client.SendSpeech(".menustyle");
			Expect(client, "Menu se ted otevira obema zpusoby, plati prvni volba.");
			string report = "";
			for (int tries = 0; (tries < 5) && !report.Contains(daemon); tries++) {
				this.Ask(client, ".TestMenuCast \"s_polymorph\"", "MENU_CAST:");
				client.ReadGumpDialogWithStrings(Wait);
				menu = client.ReadItemMenu(Wait);
				Assert.Equal(7, menu.Rows.Count);
				Assert.Contains("open=True", this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));
				client.SendItemMenuChoice(menu.MenuUid, menu.Rows.FindIndex(one => one.Text == "Daemon") + 1);
				Thread.Sleep(300);
				Assert.Contains("open=False", this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));
				Thread.Sleep(4000);
				report = this.Ask(client, ".TestMenuReport", "MENU_REPORT:");
			}
			Assert.Contains(daemon, report);
			this.Ask(client, ".TestMenuEnd", "MENU_END:");

			report = "";
			for (int tries = 0; (tries < 5) && !report.Contains(daemon); tries++) {
				this.Ask(client, ".TestMenuCast \"s_polymorph\"", "MENU_CAST:");
				gump = client.ReadGumpDialogWithStrings(Wait);
				menu = client.ReadItemMenu(Wait);
				client.SendGumpResponse(gump.GumpUid, 6); //Daemon, in the dialog
				Thread.Sleep(300);
				client.SendItemMenuChoice(menu.MenuUid, 1); //Man, too late
				Thread.Sleep(4000);
				report = this.Ask(client, ".TestMenuReport", "MENU_REPORT:");
			}
			Assert.Contains(daemon, report);
			this.Ask(client, ".TestMenuEnd", "MENU_END:");
			Assert.Contains(own, this.Ask(client, ".TestMenuReport", "MENU_REPORT:"));

			//and round to where it began
			client.SendSpeech(".menustyle");
			Expect(client, "Menu se ted otevira jako dialog.");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MenuStyleCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MenuStyle";
	}
}
