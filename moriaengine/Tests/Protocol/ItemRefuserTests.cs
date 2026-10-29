using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#807 - e_item_refuser: an NPC with it turns down an item dropped on it (a real
	/// pick-up + drop-on-character packet pair), says so, and the item goes back to the giver's
	/// pack. Who may put items into an NPC's pack at all is Character.CanPutItemsInContainer: today
	/// only a GM (IsPetOf is still a TODO that checks IsGM), everyone else gets "That does not
	/// belong to you". So the keeper half runs as a GM: the NPC without the ability takes the item,
	/// the refuser still turns it down - its trigger runs before that permission check.
	/// Server-side helpers: scripts/Tests/ItemRefuserTests.cs.</summary>
	[Collection(ItemRefuserCollection.Name)]
	public class ItemRefuserTests {
		private const string Account = "itemrefusertest";
		private const string Password = "itemrefusertest";

		private readonly GameServerPairFixture fixture;

		public ItemRefuserTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RefuserGivesTheItemBackEvenToAGmAndKeeperKeepsIt() {
			using (var client = this.ConnectLoginAndCreateCharacter("ItemRefuserTester")) {
				client.SendSpeech(".TestTeleportToOpenGround");
				Thread.Sleep(500);
				client.SendSpeech(".TestSetGmOff");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupItemRefuser");
				this.AssertSeen("REFUSER_SETUP:", mark, TimeSpan.FromSeconds(10));
				string setup = LastLineContaining(this.fixture.GameOutputSnapshot(), "REFUSER_SETUP:");
				uint refuser = ExtractUidAfter(setup, "refuser=");
				uint keeper = ExtractUidAfter(setup, "keeper=");
				uint itemA = ExtractUidAfter(setup, "itemA=");
				uint itemB = ExtractUidAfter(setup, "itemB=");
				uint itemC = ExtractUidAfter(setup, "itemC=");
				uint backpack = ExtractUidAfter(setup, "backpack=");

				client.SendDoubleClick(backpack); //open own pack first - needed for reachability
				Thread.Sleep(500);

				client.SendPickUpItem(itemA, 1);
				Thread.Sleep(300);
				client.SendDropItem(itemA, 0, 0, 0, (int) refuser);
				Assert.True(SawSpeechContaining(client, "don't need", TimeSpan.FromSeconds(5)), "the refuser never said its line");

				string a = this.Report(client, itemA);
				Assert.True(a.Contains("inMyPack=True", StringComparison.Ordinal), "refused item not back in my pack: " + a);
				Assert.True(a.Contains("dragging=False", StringComparison.Ordinal), "refused item stuck in hand: " + a);

				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendPickUpItem(itemB, 1);
				Thread.Sleep(300);
				client.SendDropItem(itemB, 0, 0, 0, (int) keeper);
				Thread.Sleep(500);
				string b = this.Report(client, itemB);
				Assert.True(b.Contains("holder=Keeper", StringComparison.Ordinal), "an NPC without the ability should take a GM's item: " + b);

				client.SendPickUpItem(itemC, 1);
				Thread.Sleep(300);
				client.SendDropItem(itemC, 0, 0, 0, (int) refuser);
				Thread.Sleep(500);
				string c = this.Report(client, itemC);
				Assert.True(c.Contains("inMyPack=True", StringComparison.Ordinal), "the refuser took a GM's item: " + c);
			}
		}

		private string Report(TestGameClient client, uint item) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportItemHolder " + item);
			this.AssertSeen("REFUSER_RESULT: uid=" + item, mark, TimeSpan.FromSeconds(10));
			return LastLineContaining(this.fixture.GameOutputSnapshot(), "REFUSER_RESULT: uid=" + item);
		}

		private static bool SawSpeechContaining(TestGameClient client, string text, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				string message;
				try {
					message = client.ReadSystemMessage(deadline - DateTime.UtcNow);
				} catch (TimeoutException) {
					return false;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return true;
				}
			}
			return false;
		}

		private void AssertSeen(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
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
			string rest = line.Substring(idx + marker.Length);
			int end = 0;
			while ((end < rest.Length) && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		//character names may only hold letters and spaces - a digit gets the client dropped
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(Account, Password);

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

	[CollectionDefinition(ItemRefuserCollection.Name, DisableParallelization = true)]
	public class ItemRefuserCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ItemRefuserPair";
	}
}
