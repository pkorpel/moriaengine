using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#892 - a town's hired guards, by a mayor through the menus.</summary>
	[Collection(TownGuardCollection.Name)]
	public class TownGuardTests {
		private const string Account = "townguardtests";
		private const string AccountB = "townguardtestsb";
		private const int Citizenship = 25, Recruiter = 20, Posts = 19, Deposit = 5, HireFirst = 1000, MoveFirst = 1000;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public TownGuardTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void EveryCityHasGuardsToHire() {
			using (var client = this.Connect("Guard Table Tester")) {
				this.Ask(client, ".TestGuardTables", "GUARD_TABLES:");
				string output = this.fixture.GameOutputSnapshot();
				foreach (string city in new[] { "Minas_Tirith", "Edoras", "Dol_Amroth", "Fornost", "Hobitin", "Erebor", "Dol", "Sede_pristavy", "Kralovstvi_lesnich_elfu",
						"Barad-dur", "Nurn", "Rhun", "Harad", "Umbar", "king" }) {
					string line = LastLine(output, "GUARD_TABLE: " + city + " ");
					Assert.DoesNotContain(" 0 end", line);
				}
			}
		}

		[Fact]
		public void AMayorHiresPostsAndCommands() {
			using (var mayor = this.Connect("Guard Mayor Tester")) {
				this.Ask(mayor, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var other = this.Connect("Guard Other Tester", AccountB);
				this.Ask(mayor, ".SetupTowns", "SETUP_DONE:");
				uint stone = (uint) Number(this.Ask(mayor, ".TestTownPrepare 1", "TOWN_READY:"), "stone");
				this.Ask(other, ".TestTownPrepare 0", "TOWN_READY:");
				uint otherUid = (uint) Number(this.Ask(other, ".TestTownReportUid", "TOWN_UID:"), "uid");
				this.Press(mayor, stone, Citizenship);
				this.Yes(mayor);
				this.Ask(mayor, ".TestTownSetMayor", "TOWN_MAYOR:");

				//money into the treasury, a guard out of it
				this.Press(mayor, stone, Recruiter);
				var recruiter = mayor.ReadGumpDialogWithStrings(Wait);
				mayor.SendGumpResponse(recruiter.GumpUid, Deposit, new Dictionary<int, string> { { 1, "100000" } });
				recruiter = mayor.ReadGumpDialogWithStrings(Wait);
				mayor.SendGumpResponse(recruiter.GumpUid, HireFirst);
				recruiter = mayor.ReadGumpDialogWithStrings(Wait);
				mayor.SendGumpResponse(recruiter.GumpUid, 0);
				string hired = this.Report(mayor);
				Assert.Contains("guards=1 standing=False", hired);
				Assert.Contains("treasury=85000", hired); //15 000 for a Minas Tirith halberd rider, tag.guardhire

				//posted: outside the town it's refused, inside it stands
				string here = this.Report(mayor);
				this.Press(mayor, stone, Posts);
				var posts = mayor.ReadGumpDialogWithStrings(Wait);
				mayor.SendGumpResponse(posts.GumpUid, MoveFirst);
				Thread.Sleep(300);
				mayor.SendTargetGround((int) Number(here, "posX"), (int) Number(here, "posY"), (int) Number(here, "posZ"));
				mayor.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("guards=1 standing=True held=False", this.Report(mayor));

				//not on an honest Gondoran, on a criminal yes
				mayor.SendSpeech(".townguardskill");
				Thread.Sleep(300);
				mayor.SendTargetUid(otherUid);
				Thread.Sleep(300);
				Assert.Contains("fightsMe=False", this.Report(other));
				this.Ask(other, ".TestTownCriminalNow", "TOWN_CRIMINAL_NOW:");
				mayor.SendSpeech(".townguardskill");
				Thread.Sleep(300);
				mayor.SendTargetUid(otherUid);
				Thread.Sleep(300);
				Assert.Contains("fightsMe=True", this.Report(other));

				mayor.SendSpeech(".townguardsstop");
				Thread.Sleep(300);
				Assert.Contains("held=True", this.Report(mayor));
				mayor.SendSpeech(".townguardscontinue");
				Thread.Sleep(300);
				Assert.Contains("held=False", this.Report(mayor));

				//killed, they come back; unpaid, they go
				this.Ask(mayor, ".TestGuardTime die", "GUARD_TIME:");
				Assert.Contains("guards=1 standing=True", this.Report(mayor));
				this.Ask(mayor, ".TestGuardTime unpaid", "GUARD_TIME:");
				Assert.Contains("guards=0 standing=False", this.Report(mayor));
			}
		}

		private void Press(TestGameClient client, uint stone, int button) {
			client.SendDoubleClick(stone);
			var menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, button);
		}

		private void Yes(TestGameClient client) {
			var confirm = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(confirm.GumpUid, 1);
			Thread.Sleep(300);
		}

		private string Report(TestGameClient client) {
			return this.Ask(client, ".TestGuardReport", "GUARD_REPORT:");
		}

		private string Ask(TestGameClient client, string command, string marker) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(command);
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "No " + marker + " after " + command + ":\n" + this.fixture.GameOutputSnapshot());
			return LastLine(this.fixture.GameOutputSnapshot(), marker);
		}

		private static string LastLine(string output, string marker) {
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(index >= 0, "Could not find '" + marker + "'");
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

		private TestGameClient Connect(string charName, string account = Account) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, account);
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
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TownGuardCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TownGuards";
	}
}
