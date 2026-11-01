using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#941 - locking things to a house, with two real clients.</summary>
	[Collection(HouseLockCollection.Name)]
	public class HouseLockTests {
		private const string OwnerAccount = "houselocka", VisitorAccount = "houselockb";
		private const int LockButton = 2, FriendLevelButton = 2, OwnerLevelButton = 4, PersonalButton = 6;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public HouseLockTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void LockedThingsAnswerToRanks() {
			using (var owner = this.Connect(OwnerAccount, "Lock Owner")) {
				this.Ask(owner, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
				using var visitor = this.Connect(VisitorAccount, "Lock Visitor");

				string setup = this.Ask(owner, ".TestLockSetup", "LOCK_SETUP:");
				Assert.Contains("inRegion=True", setup);
				uint sign = Uid(setup, "sign"), bag = Uid(setup, "bag"), coin = Uid(setup, "coin"), door = Uid(setup, "door"), ingot = Uid(setup, "ingot");
				this.Ask(visitor, ".TestLockVisit \"friend\"", "LOCK_VISIT:");
				visitor.SendDoubleClick(bag); //the bag is open when it gets locked, or a drag from it would go nowhere
				Thread.Sleep(300);

				//locked at the owner's level: a friend's hand in it is a thief's, and neither of them can lift it
				this.LockWith(owner, sign, bag, OwnerLevelButton);
				string report = this.Ask(owner, ".TestLockReport", "LOCK_REPORT:");
				Assert.Contains("bag=Owner/True/True person=none list=1", report);
				int mark = this.fixture.GameOutputSnapshot().Length;
				visitor.SendPickUpItem(coin, 1);
				Assert.True(this.fixture.WaitForGameOutputSince("ZLODEJ !!!", mark, Wait), "Reaching into a locked bag wasn't taken for a theft.");
				SeeMessage(visitor, "Nemas na to pravo.");
				visitor.SendPickUpItem(bag, 1);
				SeeMessage(visitor, "Vec je zamcena k domu.");
				owner.SendPickUpItem(bag, 1);
				SeeMessage(owner, "Vec je zamcena k domu.");
				visitor.SendDoubleClick(bag);
				SeeMessage(visitor, "Nemas na to pravo.");
				Assert.Contains("coinIn=True bagGround=True", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));

				//a blocked house turns it black, and it comes back
				Assert.Contains("color=1", this.Ask(owner, ".TestLockBlock \"on\"", "LOCK_BLOCK:"));
				Assert.DoesNotContain("color=1", this.Ask(owner, ".TestLockBlock \"off\"", "LOCK_BLOCK:"));

				//unlocked, it's as it was
				this.Unlock(owner, sign, bag);
				Assert.Contains("bag=Public/False/False person=none list=0", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));

				//at the friend's level the same hand in the same bag is fine
				this.LockWith(owner, sign, bag, FriendLevelButton);
				visitor.SendPickUpItem(coin, 1);
				Thread.Sleep(300);
				Assert.Contains("coinIn=False", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));
				this.Unlock(owner, sign, bag);

				//a personal lock: their own, not a friend's, and a blocked house stops the person too
				this.LockWith(owner, sign, bag, PersonalButton);
				Assert.Contains("person=Lock Owner", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));
				Assert.Contains("locked=False", this.Ask(owner, ".TestLockCheck", "LOCK_CHECK:"));
				Assert.Contains("locked=True", this.Ask(visitor, ".TestLockCheck", "LOCK_CHECK:"));
				this.Ask(owner, ".TestLockBlock \"on\"", "LOCK_BLOCK:");
				Assert.Contains("locked=True", this.Ask(owner, ".TestLockCheck", "LOCK_CHECK:"));
				this.Ask(owner, ".TestLockBlock \"off\"", "LOCK_BLOCK:");

				//not everything can be locked
				owner.SendDoubleClick(sign);
				var gump = owner.ReadGumpDialogWithStrings(Wait);
				owner.SendGumpResponse(gump.GumpUid, LockButton);
				Thread.Sleep(300);
				owner.SendTargetUid(ingot);
				Thread.Sleep(300);
				Assert.Contains("ingot=False", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));

				//a locked door opens for a friend, not for the public
				this.LockWith(owner, sign, door, OwnerLevelButton);
				Assert.Contains("door=t_door_locked/False", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));
				visitor.SendDoubleClick(door);
				Thread.Sleep(300);
				Assert.Contains("door=t_door_locked/True", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));
				this.Ask(owner, ".TestLockCloseDoor", "LOCK_DOOR:");
				this.Ask(visitor, ".TestLockVisit \"public\"", "LOCK_VISIT:");
				visitor.SendDoubleClick(door);
				Thread.Sleep(300);
				Assert.Contains("door=t_door_locked/False", this.Ask(owner, ".TestLockReport", "LOCK_REPORT:"));

				//an eviction unlocks everything
				string evicted = this.Ask(owner, ".TestLockEvict", "LOCK_EVICTED:");
				Assert.Contains("list=0 door=t_door bagInChest=True attr=False", evicted);

				//nothing stays fixed for good when the house itself goes
				Assert.Contains("fixed=False", this.Ask(owner, ".TestLockDestroy", "LOCK_DESTROYED:"));
			}
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

		private void LockWith(TestGameClient owner, uint sign, uint thing, int levelButton) {
			owner.SendDoubleClick(sign);
			var gump = owner.ReadGumpDialogWithStrings(Wait);
			owner.SendGumpResponse(gump.GumpUid, LockButton);
			Thread.Sleep(300);
			owner.SendTargetUid(thing);
			gump = owner.ReadGumpDialogWithStrings(Wait);
			owner.SendGumpResponse(gump.GumpUid, levelButton);
			Thread.Sleep(300);
		}

		private void Unlock(TestGameClient owner, uint sign, uint thing) {
			owner.SendDoubleClick(sign);
			var gump = owner.ReadGumpDialogWithStrings(Wait);
			owner.SendGumpResponse(gump.GumpUid, LockButton);
			Thread.Sleep(300);
			owner.SendTargetUid(thing);
			Thread.Sleep(300);
		}

		private static uint Uid(string line, string name) {
			return uint.Parse(Regex.Match(line, name + @"=(\d+)").Groups[1].Value, CultureInfo.InvariantCulture);
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
	public class HouseLockCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseLock";
	}
}
