using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#892 - town stones, driven through their menus by a plain player.</summary>
	[Collection(TownTestsCollection.Name)]
	public class TownTests {
		private const string Account = "towntests";
		private const int MakeCitizenship = 25, PayTax = 4, TaxStatus = 6, SetTax = 14, Resign = 15, Home = 24, News = 23, Yes = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public TownTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SetupPutsAStoneInEveryCityOnce() {
			using (var client = this.Connect("Town Setup Tester")) {
				string first = this.Ask(client, ".SetupTowns", "SETUP_DONE:"); //another test may have run it already
				Assert.Equal(9, Number(first, "added") + Number(first, "kept"));
				Assert.Contains("added=0 kept=9", this.Ask(client, ".SetupTowns", "SETUP_DONE:"));
				this.Ask(client, ".TestTownStones", "TOWN_STONES:");
				string output = this.fixture.GameOutputSnapshot();
				foreach (string city in new[] { "Minas_Tirith", "Edoras", "Dol_Amroth", "Fornost", "Hobitin", "Erebor", "Dol", "Sede_pristavy", "Kralovstvi_lesnich_elfu" }) {
					string line = LastLine(output, "TOWN_STONE: " + city + " ");
					Assert.DoesNotContain("at=none", line);
					Assert.Contains("area=a_" + city + " ", line);
				}
			}
		}

		[Fact]
		public void CitizenPaysTheirWayAndLosesItWhenTheyDont() {
			using (var client = this.Connect("Town Citizen Tester")) {
				this.Ask(client, ".SetupTowns", "SETUP_DONE:");
				uint stone = (uint) Number(this.Ask(client, ".TestTownPrepare 0", "TOWN_READY:"), "stone");

				//not yet good enough: the fee stays where it is
				this.Press(client, stone, MakeCitizenship);
				this.Yes_(client);
				Assert.Contains("citizen=False", this.Report(client));

				this.Ask(client, ".TestTownPrepare 1", "TOWN_READY:");
				this.Press(client, stone, MakeCitizenship);
				this.Yes_(client);
				string joined = this.Report(client);
				Assert.Contains("citizens=1 citizen=True paid=False", joined);
				Assert.Contains("chest=True", joined);
				Assert.Contains("gold=200000", joined);

				this.Press(client, stone, PayTax);
				this.Yes_(client);
				Assert.Contains("paid=True", this.Report(client));
				Assert.Contains("treasury=10000 gold=190000", this.Report(client));

				//paid, so they stay, and start the next period owing
				this.Ask(client, ".TestTownTaxDue", "TOWN_DUE:");
				client.SendDoubleClick(stone);
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("citizen=True paid=False", this.Report(client));

				//autopay pays at once and carries on through the next period
				this.Press(client, stone, TaxStatus);
				var list = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(list.GumpUid, 100);
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("paid=True autoPay=True", this.Report(client));
				this.Ask(client, ".TestTownTaxDue", "TOWN_DUE:");
				client.SendDoubleClick(stone);
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("citizen=True paid=True", this.Report(client));
				Assert.Contains("treasury=30000 gold=170000", this.Report(client));

				//free home for a citizen
				this.Press(client, stone, Home);
				this.Yes_(client);
				Assert.Contains("homeHere=True", this.Report(client));

				//a mayor doesn't pay, sets the tax and writes the news
				this.Ask(client, ".TestTownSetMayor", "TOWN_MAYOR:");
				this.Press(client, stone, SetTax);
				var input = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(input.GumpUid, 1, new Dictionary<int, string> { { 1, "20000" } });
				Thread.Sleep(300);
				Assert.Contains("tax=20000", this.Report(client));
				this.Press(client, stone, News);
				var news = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(news.GumpUid, 3);
				input = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(input.GumpUid, 1, new Dictionary<int, string> { { 1, "Otevreno" } });
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("news=1", this.Report(client));

				//no longer mayor and not paid: out
				this.Press(client, stone, Resign);
				this.Yes_(client);
				Assert.Contains("mayor=none", this.Report(client));
				this.Press(client, stone, TaxStatus);
				list = client.ReadGumpDialogWithStrings(Wait);
				client.SendGumpResponse(list.GumpUid, 100); //automatic payment off
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("autoPay=False", this.Report(client));
				this.Ask(client, ".TestTownTaxDue", "TOWN_DUE:");
				client.SendDoubleClick(stone); //paid above, so this only clears it
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("citizen=True paid=False", this.Report(client));
				this.Ask(client, ".TestTownTaxDue", "TOWN_DUE:");
				client.SendDoubleClick(stone);
				client.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("citizen=False", this.Report(client));
			}
		}

		private void Press(TestGameClient client, uint stone, int button) {
			client.SendDoubleClick(stone);
			var menu = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(menu.GumpUid, button);
		}

		private void Yes_(TestGameClient client) {
			var confirm = client.ReadGumpDialogWithStrings(Wait);
			client.SendGumpResponse(confirm.GumpUid, Yes);
			Thread.Sleep(300);
		}

		private string Report(TestGameClient client) {
			return this.Ask(client, ".TestTownReport", "TOWN_REPORT:");
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

		private TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
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
	}

	/// <summary>A real save and restart: a town is saved with its stone.</summary>
	[Collection(TownRestartCollection.Name)]
	public class TownRestartTests {
		private const string Account = "townrestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void TownSurvivesARealRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect("Town Saver")) {
					Ask(fixture1, client, ".SetupTowns", "SETUP_DONE:");
					Ask(fixture1, client, ".TestTownForSave", "TOWN_SAVED:");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect("Town Loader")) {
					string loaded = Ask(fixture2, client, ".TestTownAfterRestart", "TOWN_LOADED:");
					Assert.Contains("found=True stone=True citizens=1 mayor=Town_Saver treasury=777 news=1 taxDue=True", loaded);
				}
			}
		}

		private static TestGameClient Connect(string charName) {
			int redirectPort = TownTestsLogin.RedirectPort(Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "No '" + marker + "' after " + speech);
			string output = fixture.GameOutputSnapshot();
			int index = output.LastIndexOf(marker, StringComparison.Ordinal);
			int end = output.IndexOf('\n', index);
			return (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
		}
	}

	internal static class TownTestsLogin {
		internal static int RedirectPort(string account) {
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
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TownTestsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TownTests";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TownRestartCollection {
		public const string Name = "TownRestart";
	}
}
