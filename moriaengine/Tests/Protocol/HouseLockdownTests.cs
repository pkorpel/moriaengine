using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#221: the house lockdown mechanic (attr_lockeddown/attr_secure), built entirely
	/// on top of the existing #74 Housing system. Reuses HousingTests.cs's own deed-placement
	/// flow (.TestCleanupNearbyHouses/.TestGrantHouseDeed, same global [SteamFunction] dispatch
	/// table - not duplicated here) to get a REAL placed house with a REAL MultiRegion, then
	/// exercises the new behavior on top of it.
	///
	/// attr_lockeddown alone does NOT restrict access/viewing (only movement) - not covered by
	/// a dedicated negative test here, since On_DenyPickupItemFrom's own code only ever checks
	/// attr_secure, never attr_lockeddown; the absence is directly verifiable by reading
	/// Item.cs rather than needing its own round-trip.</summary>
	[Collection(HouseLockdownCollection.Name)]
	public class HouseLockdownTests {
		private const string AccountA = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string AccountC = "protocoltest3";
		private const string AccountD = "protocoltest4";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public HouseLockdownTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: an attr_lockeddown item inside a house can only be picked up
		/// by the house's owner or a co-owner - NOT a friend, despite a friend otherwise having
		/// full use of the house. Single shared item, tested in denied-then-denied-then-allowed
		/// order so each earlier (denied) check leaves state untouched for the next.</summary>
		[Fact]
		public void LockedDownItemOnlyMovableByOwnerOrCoOwner() {
			using (var owner = this.ConnectLoginAndCreateCharacter(AccountA, "LockOwner")) {
				owner.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST:", TimeSpan.FromSeconds(10)));

				var (x, y, z) = this.PlaceHouse(owner);

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				owner.SendSpeech(".TestSetupLockedDownItem");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_SETUP: uid=", markSetup, TimeSpan.FromSeconds(10)));
				uint itemUid = ExtractUidAfter(LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_LOCKDOWN_SETUP: uid="), "uid=");

				using (var coOwner = this.ConnectLoginAndCreateCharacter(AccountB, "LockCoOwner")) {
					uint coOwnerUid = this.ReportMyUid(coOwner);
					this.TeleportTo(coOwner, x, y, z);

					using (var friend = this.ConnectLoginAndCreateCharacter(AccountC, "LockFriend")) {
						uint friendUid = this.ReportMyUid(friend);
						this.TeleportTo(friend, x, y, z);

						using (var stranger = this.ConnectLoginAndCreateCharacter(AccountD, "LockStranger")) {
							this.TeleportTo(stranger, x, y, z);

							int markAddCo = this.fixture.GameOutputSnapshot().Length;
							owner.SendSpeech(".TestAddCoOwnerToNearbyHouse " + coOwnerUid);
							Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_ACCESS_SETUP:", markAddCo, TimeSpan.FromSeconds(10)));

							int markAddFriend = this.fixture.GameOutputSnapshot().Length;
							owner.SendSpeech(".TestAddFriendToNearbyHouse " + friendUid);
							Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_SETUP: added friend", markAddFriend, TimeSpan.FromSeconds(10)));

							//stranger: denied, item stays on the ground
							Assert.Equal("none", this.AttemptPickupAndReportContainer(stranger, itemUid));

							//friend: also denied - friends can use the house but not move locked items
							Assert.Equal("none", this.AttemptPickupAndReportContainer(friend, itemUid));

							//co-owner: allowed
							Assert.Equal(coOwnerUid.ToString(System.Globalization.CultureInfo.InvariantCulture),
								this.AttemptPickupAndReportContainer(coOwner, itemUid));
						}
					}
				}
			}
		}

		/// <summary>Real UO rule: attr_secure is "stronger" than attr_lockeddown in two
		/// independent ways - (1) movement of the secure container itself is still
		/// owner/co-owner-only (same as lockeddown), but (2) ACCESS to what's inside it is also
		/// restricted, to owner/co-owners/friends (not strangers) - exercised via
		/// On_DenyPickupItemFrom, a different hook than plain lockdown ever touches.</summary>
		[Fact]
		public void SecureContainerRestrictsAccessAndMovement() {
			using (var owner = this.ConnectLoginAndCreateCharacter(AccountA, "SecureOwner")) {
				owner.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST:", TimeSpan.FromSeconds(10)));

				var (x, y, z) = this.PlaceHouse(owner);

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				owner.SendSpeech(".TestSetupSecureContainer");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_SETUP: containerUid=", markSetup, TimeSpan.FromSeconds(10)));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_LOCKDOWN_SETUP: containerUid=");
				uint containerUid = ExtractUidAfter(setupLine, "containerUid=");
				uint innerUid = ExtractUidAfter(setupLine, "innerUid=");

				using (var coOwner = this.ConnectLoginAndCreateCharacter(AccountB, "SecureCoOwner")) {
					uint coOwnerUid = this.ReportMyUid(coOwner);
					this.TeleportTo(coOwner, x, y, z);

					using (var friend = this.ConnectLoginAndCreateCharacter(AccountC, "SecureFriend")) {
						uint friendUid = this.ReportMyUid(friend);
						this.TeleportTo(friend, x, y, z);

						using (var stranger = this.ConnectLoginAndCreateCharacter(AccountD, "SecureStranger")) {
							this.TeleportTo(stranger, x, y, z);

							int markAddCo = this.fixture.GameOutputSnapshot().Length;
							owner.SendSpeech(".TestAddCoOwnerToNearbyHouse " + coOwnerUid);
							Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_ACCESS_SETUP:", markAddCo, TimeSpan.FromSeconds(10)));

							int markAddFriend = this.fixture.GameOutputSnapshot().Length;
							owner.SendSpeech(".TestAddFriendToNearbyHouse " + friendUid);
							Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_SETUP: added friend", markAddFriend, TimeSpan.FromSeconds(10)));

							//stranger can't even take the inner item OUT of the secure container
							Assert.Equal(containerUid.ToString(System.Globalization.CultureInfo.InvariantCulture),
								this.AttemptPickupAndReportContainer(stranger, innerUid));

							//friend CAN - friends have real house access, unlike a stranger. Needs to
							//open the container first (same "a connection can only reach an item
							//inside a container it has already opened" rule documented elsewhere
							//in this codebase, AbstractCharacter.CanReach's own
							//OpenedContainers.HasContainerOpenFromAt check) - the stranger's own
							//denial above never gets that far, since On_DenyPickupItemFrom's own
							//access-denied Result short-circuits before CanReach is ever
							//evaluated, so the stranger case didn't need this.
							friend.SendDoubleClick(containerUid);
							Thread.Sleep(300);
							Assert.Equal(friendUid.ToString(System.Globalization.CultureInfo.InvariantCulture),
								this.AttemptPickupAndReportContainer(friend, innerUid));

							//friend can't pick up the secure CONTAINER itself though - movement
							//restriction is still owner/co-owner-only, same as plain lockdown
							Assert.Equal("none", this.AttemptPickupAndReportContainer(friend, containerUid));

							//co-owner can
							Assert.Equal(coOwnerUid.ToString(System.Globalization.CultureInfo.InvariantCulture),
								this.AttemptPickupAndReportContainer(coOwner, containerUid));
						}
					}
				}
			}
		}

		/// <summary>Real deed-double-click + target-ground placement flow, same as
		/// HousingTests.cs's own DeedPlacementRejectsOverlap - returns the spawn-relative point
		/// the house was actually placed at (owner.P() isn't necessarily inside the house's own
		/// footprint afterward, but TestSetupLockedDownItem/TestSetupSecureContainer place their
		/// items at the house's own anchor point directly, so that doesn't matter here).</summary>
		private (int X, int Y, int Z) PlaceHouse(TestGameClient owner) {
			owner.ReadSpawnPosition(TimeSpan.FromSeconds(10)); //drain the spawn packet, value unused - see below

			//#210's startingPosition is a deliberately narrow, boxed-in antechamber - reuse
			//MovementSpeedTests' own expanding-ring open-ground search (#78) rather than a
			//hardcoded offset, since the new spawn point has no passable house-sized footprint
			//nearby at all.
			int markTeleport = this.fixture.GameOutputSnapshot().Length;
			owner.SendSpeech(".TestTeleportToOpenGround");
			Assert.True(this.fixture.WaitForGameOutputSince("OPEN_GROUND_FOUND:", markTeleport, TimeSpan.FromSeconds(10)));
			string openGroundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:");
			Assert.False(openGroundLine.Contains("none"), "No open ground found: " + openGroundLine);
			ushort px = (ushort) ExtractSignedIntAfter(openGroundLine, "x=");
			ushort py = (ushort) ExtractSignedIntAfter(openGroundLine, "y=");
			int z = ExtractSignedIntAfter(openGroundLine, "z=");

			int markCleanup = this.fixture.GameOutputSnapshot().Length;
			owner.SendSpeech(".TestCleanupNearbyHouses");
			Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CLEANUP: DONE", markCleanup, TimeSpan.FromSeconds(10)));

			int markSetup = this.fixture.GameOutputSnapshot().Length;
			owner.SendSpeech(".TestGrantHouseDeed");
			Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
			string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_SETUP:");
			uint deedUid = ExtractUidAfter(setupLine, "deed=#");
			uint backpackUid = ExtractUidAfter(setupLine, "backpack=#");

			owner.SendDoubleClick(backpackUid);
			Thread.Sleep(300);
			owner.SendDoubleClick(deedUid);
			Thread.Sleep(300);
			owner.SendTargetGround(px, py, z);
			Thread.Sleep(1000);

			int markCheck = this.fixture.GameOutputSnapshot().Length;
			owner.SendSpeech(".TestCheckHouseNearby");
			Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
			Assert.True(LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_CHECK:").Contains("count=1"),
				"House placement failed. Output since cleanup:\n" + this.fixture.GameOutputSnapshot().Substring(markCleanup));

			return (px, py, z);
		}

		/// <summary>Real finding while building this: a denied pickup and a pickup denied only
		/// because the picker is too far away produce the IDENTICAL observable outcome ("item
		/// stays put") - a test connecting a fresh character (always spawns at its own default
		/// position, nowhere near the house) and immediately attempting a pickup would "pass"
		/// for entirely the wrong reason. CanReach is checked AFTER the deny-trigger chain
		/// (AbstractCharacter.CanPickup), so every test character needs to actually be
		/// standing at the house first.</summary>
		private void TeleportTo(TestGameClient client, int x, int y, int z) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestTeleportTo " + x + "," + y + "," + z);
			Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_SETUP: teleported", mark, TimeSpan.FromSeconds(10)));
		}

		private uint ReportMyUid(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
			return ExtractUidAfter(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");
		}

		/// <summary>Sends a real pickup packet (0x07) for the given item, then reports back
		/// whatever currently contains it - "none" if still on the ground/untouched.</summary>
		private string AttemptPickupAndReportContainer(TestGameClient client, uint itemUid) {
			client.SendPickUpItem(itemUid, 1);
			Thread.Sleep(300);

			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportItemContainer " + itemUid);
			Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_LOCKDOWN_RESULT: uid=" + itemUid, mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_LOCKDOWN_RESULT: uid=" + itemUid);
			int idx = line.IndexOf("contUid=", StringComparison.Ordinal);
			return line.Substring(idx + "contUid=".Length).TrimEnd('\r');
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		/// <summary>z coordinates can be negative (basements, dungeons) - unlike every other
		/// marker this file parses (all uids/counts, always non-negative), so this tolerates an
		/// optional leading '-'.</summary>
		private static int ExtractSignedIntAfter(string text, string marker) {
			int idx = text.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + text);
			string rest = text.Substring(idx + marker.Length);
			int end = (rest.Length > 0 && rest[0] == '-') ? 1 : 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static uint ExtractUidAfter(string text, string marker) {
			int idx = text.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + text);
			string rest = text.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);
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

	[CollectionDefinition(HouseLockdownCollection.Name, DisableParallelization = true)]
	public class HouseLockdownCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HouseLockdownPair";
	}
}
