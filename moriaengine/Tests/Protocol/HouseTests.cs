using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - a house's sign: claiming, the menu by rank, members, name, look, transfer, blocking.</summary>
	[Collection(HouseCollection.Name)]
	public class HouseTests {
		private const string AccountA = "housetesta", AccountB = "housetestb";
		private const int FriendAdd = 3, FriendList = 4, CoOwnerAdd = 5, Rename = 7, ChangeSign = 8, Transfer = 10, Block = 11;
		private const int Yes = 1, RemoveFirst = 100, TextEntry = 1;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public HouseTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ClaimMenuMembersAndTransfer() {
			using (var owner = this.Connect(AccountA, "House Owner")) {
				this.Ask(owner, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var other = this.Connect(AccountB, "House Friend");

				string made = this.Ask(owner, ".TestHouseMake", "HOUSE_MADE:");
				uint sign = (uint) Number(made, "sign");
				Assert.Equal(89000, Number(made, "rent")); //99 tiles at 900, to the nearest 1000
				uint friend = (uint) Number(this.Ask(other, ".TestHouseComeOver", "HOUSE_HERE:"), "me");
				this.Ask(other, ".TestHouseGm \"off\"", "HOUSE_GM:");

				//nobody's: asks first, then it's theirs with the grace days
				owner.SendDoubleClick(sign);
				var gump = owner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Chces obsadit tento dum?", gump.LayoutText);
				owner.SendGumpResponse(gump.GumpUid, Yes);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Vlastnik", gump.LayoutText);
				AssertHas(gump.Strings, "Predat vlastnictvi domu", gump.LayoutText);
				AssertLacks(gump.Strings, "Zablokovat dum", gump.LayoutText);
				Assert.Contains("owner=House Owner", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));
				Assert.Contains("days=2", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));

				//the public sees only the plaque
				other.SendDoubleClick(sign);
				var plaque = other.ReadGumpDialogWithStrings(Wait);
				AssertLacks(plaque.Strings, "Seznam pratel domu", plaque.LayoutText);

				//a friend sees lists, not what's above them
				owner.SendGumpResponse(gump.GumpUid, FriendAdd);
				Thread.Sleep(300);
				owner.SendTargetUid(friend);
				Thread.Sleep(300);
				Assert.Contains("friends=1", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));
				other.SendDoubleClick(sign);
				var menu = other.ReadGumpDialogWithStrings(Wait);
				AssertHas(menu.Strings, "Pritel", menu.LayoutText);
				AssertHas(menu.Strings, "Seznam pratel domu", menu.LayoutText);
				AssertLacks(menu.Strings, "Prejmenovat dum", menu.LayoutText);

				//nobody holds two places, and the owner takes one away
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, CoOwnerAdd);
				Thread.Sleep(300);
				owner.SendTargetUid(friend);
				Thread.Sleep(300);
				Assert.Contains("friends=1 coowners=0", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, FriendList);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "House Friend", gump.LayoutText);
				owner.SendGumpResponse(gump.GumpUid, RemoveFirst);
				owner.ReadGumpDialogWithStrings(Wait);
				Assert.Contains("friends=0", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));

				//name and look
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, Rename);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, Yes, new System.Collections.Generic.Dictionary<int, string> { { TextEntry, "Hnizdo" } });
				Thread.Sleep(300);
				Assert.Contains("name=Hnizdo", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, ChangeSign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, 2981);
				Thread.Sleep(300);
				Assert.Contains("signModel=2981", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));

				//transfer
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, Transfer);
				Thread.Sleep(300);
				owner.SendTargetUid(friend);
				Thread.Sleep(300);
				Assert.Contains("owner=House Friend", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));

				//a GM blocks it, and then the owner has no menu
				this.Ask(owner, ".TestHouseGm \"on\"", "HOUSE_GM:");
				owner.SendDoubleClick(sign);
				gump = owner.ReadGumpDialogWithStrings(Wait);
				AssertHas(gump.Strings, "Zablokovat dum", gump.LayoutText);
				owner.SendGumpResponse(gump.GumpUid, Block);
				Thread.Sleep(300);
				Assert.Contains("blocked=True", this.Ask(owner, ".TestHouseReport", "HOUSE_REPORT:"));
				other.SendDoubleClick(sign);
				var blocked = other.ReadGumpDialogWithStrings(Wait);
				AssertLacks(blocked.Strings, "Tvuj status:", blocked.LayoutText);
			}
		}

		private static void AssertHas(System.Collections.Generic.IEnumerable<string> strings, string text, string layout) {
			Assert.True(System.Linq.Enumerable.Any(strings, s => s.Contains(text)) || layout.Contains(text), "Dialog lacks '" + text + "'");
		}

		private static void AssertLacks(System.Collections.Generic.IEnumerable<string> strings, string text, string layout) {
			Assert.False(System.Linq.Enumerable.Any(strings, s => s.Contains(text)) || layout.Contains(text), "Dialog has '" + text + "'");
		}

		private static long Number(string line, string name) {
			return long.Parse(Regex.Match(line, name + @"=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
		}

		private TestGameClient Connect(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(account, account);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, account);
						if (login.ReadServersList() < 1) {
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
	public class HouseCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "House";
	}

	/// <summary>A house after a real save and restart.</summary>
	[Collection(HouseRestartCollection.Name)]
	public class HouseRestartTests {
		private const string Account = "houserestart";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

		[Fact]
		public void AHouseSurvivesARestart() {
			using (var first = new GameServerPairFixture()) {
				using (var client = Connect("House Saver")) {
					Ask(first, client, ".TestHouseForSave", "HOUSE_SAVED:");
					Ask(first, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}
			using (var second = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect("House Loader")) {
					string loaded = Ask(second, client, ".TestHouseAfterRestart", "HOUSE_LOADED:");
					Assert.Contains("found=True owner=House Saver name=Hnizdo rent=123000 days=11", loaded);
				}
			}
		}

		private static TestGameClient Connect(string charName) {
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HouseRestartCollection {
		public const string Name = "HouseRestart";
	}
}
