using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-6 (#74): housing scenarios - deed double-click + target-cursor placement and overlap
	/// rejection, over real game-protocol packets. Friend/co-owner/ban list access control has
	/// no packet-level UI yet (see House.cs's own doc comment), so that part is exercised via
	/// direct calls to the real House methods from a Test* helper rather than a packet
	/// round-trip - there's no packet to send yet.
	///
	/// Scope note: .ReleaseHouse (HouseCommands.ReleaseHouse) is deferred, not covered here -
	/// it strictly requires the releasing character to be standing inside the house's own
	/// MultiRegion (FindOwnedHouseNearby checks self.Region, not merely "nearby"), and the
	/// house's real footprint (MULTIREGION = -3,-3,3,4 for the deed used here) turned out to
	/// sit entirely on impassable tiles for several tiles around this fixture's spawn point -
	/// every offset within the footprint's own radius failed TileFlag.Impassable, and the one
	/// offset that is passable (+5,+5) is outside the footprint. Actually walking there needs
	/// real movement packets (sequence-numbered MoveRequestInPacket tracking), which is V-10's
	/// own deliverable, not duplicated here.
	///
	/// Real finding (#90, fixed here): placing ANY house via this deed/target-ground flow threw
	/// "This Dynamic region has already been placed to the map" on every single attempt, even
	/// the very first ever - DynamicRegion.Place(Point4D p) checked whether its PARAMETER (the
	/// new target position, always non-null for a real call) was non-null, instead of checking
	/// this.P (the region's OWN current placement state, actually null before first placement).
	/// House placement was completely broken end-to-end despite passing every unit-level check,
	/// because nothing had ever driven the real deed-click-to-placed-house flow protocol-level
	/// before this phase.
	/// </summary>
	[Collection(HousingCollection.Name)]
	public class HousingTests {
		private const string AccountA = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string AccountC = "protocoltest3";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public HousingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: placing a house deed on open ground creates a real, owned
		/// house; placing a second deed overlapping an existing house's footprint is rejected
		/// (no second house is created).</summary>
		[Fact]
		public void DeedPlacementRejectsOverlap() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountA, "Homesteader")) {
				int markCleanup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCleanupNearbyHouses");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CLEANUP: DONE", markCleanup, TimeSpan.FromSeconds(10)));

				var (x, y, z) = client.ReadSpawnPosition(TimeSpan.FromSeconds(10));
				// The exact spawn tile (and everything within the house's own footprint radius
				// around it) is impassable real map data - (+5,+5) is the nearest offset this
				// fixture's spawn point that's actually passable for a house.
				ushort px = (ushort) (x + 5);
				ushort py = (ushort) (y + 5);

				int markSetup1 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantHouseDeed");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_SETUP:", markSetup1, TimeSpan.FromSeconds(10)));
				(uint deed1Uid, uint backpackUid) = ExtractHouseSetup(this.fixture.GameOutputSnapshot(), markSetup1);

				client.SendDoubleClick(backpackUid); // open own backpack - required for reachability, see EconomyTests' identical note
				Thread.Sleep(300);
				client.SendDoubleClick(deed1Uid); // opens the target-ground cursor (t_deed_house.On_DClick)
				Thread.Sleep(300);
				client.SendTargetGround(px, py, z);
				Thread.Sleep(1000);

				int markCheck1 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHouseNearby");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CHECK:", markCheck1, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1 owner=Homesteader",
					LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_CHECK:"));

				// Second deed, same ground - real UO rejects this (footprint already occupied).
				int markSetup2 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGrantHouseDeed");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_SETUP:", markSetup2, TimeSpan.FromSeconds(10)));
				(uint deed2Uid, _) = ExtractHouseSetup(this.fixture.GameOutputSnapshot(), markSetup2);

				client.SendDoubleClick(deed2Uid);
				Thread.Sleep(300);
				client.SendTargetGround(px, py, z);
				Thread.Sleep(1000);

				int markCheck2 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHouseNearby");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CHECK:", markCheck2, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1 owner=Homesteader",
					LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_CHECK:"));
			}
		}

		/// <summary>Real UO rule: a house's owner, co-owners, and friends can all access it; an
		/// unrelated player cannot. Exercised via House.HasAccess/AddCoOwner directly (no
		/// packet-level friend/co-owner UI exists yet).</summary>
		[Fact]
		public void CoOwnerGetsAccessButStrangerDoesNot() {
			using (var owner = this.ConnectLoginAndCreateCharacter(AccountA, "HouseOwner")) {
				owner.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST: ENABLED", TimeSpan.FromSeconds(10)));
				int markCleanup = this.fixture.GameOutputSnapshot().Length;
				owner.SendSpeech(".TestCleanupNearbyHouses");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_CLEANUP: DONE", markCleanup, TimeSpan.FromSeconds(10)));

				var (x, y, z) = owner.ReadSpawnPosition(TimeSpan.FromSeconds(10));

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				owner.SendSpeech(".TestGrantHouseDeed");
				Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				(uint deedUid, uint backpackUid) = ExtractHouseSetup(this.fixture.GameOutputSnapshot(), markSetup);

				owner.SendDoubleClick(backpackUid);
				Thread.Sleep(300);
				owner.SendDoubleClick(deedUid);
				Thread.Sleep(300);
				owner.SendTargetGround(x + 5, y + 5, z);
				Thread.Sleep(1000);

				using (var coOwner = this.ConnectLoginAndCreateCharacter(AccountB, "CoOwnerChar")) {
					int markCoUid = this.fixture.GameOutputSnapshot().Length;
					coOwner.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markCoUid, TimeSpan.FromSeconds(10)));
					uint coOwnerUid = ExtractUid(this.fixture.GameOutputSnapshot(), markCoUid);

					using (var stranger = this.ConnectLoginAndCreateCharacter(AccountC, "StrangerChar")) {
						int markStrangerUid = this.fixture.GameOutputSnapshot().Length;
						stranger.SendSpeech(".TestReportMyUid");
						Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markStrangerUid, TimeSpan.FromSeconds(10)));
						uint strangerUid = ExtractUid(this.fixture.GameOutputSnapshot(), markStrangerUid);

						int markAdd = this.fixture.GameOutputSnapshot().Length;
						owner.SendSpeech(".TestAddCoOwnerToNearbyHouse " + coOwnerUid);
						Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_ACCESS_SETUP:", markAdd, TimeSpan.FromSeconds(10)));

						int markCheckCo = this.fixture.GameOutputSnapshot().Length;
						owner.SendSpeech(".TestCheckHouseAccess " + coOwnerUid);
						Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_ACCESS_CHECK:", markCheckCo, TimeSpan.FromSeconds(10)));
						Assert.Contains("hasAccess=True",
							LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_ACCESS_CHECK:"));

						int markCheckStranger = this.fixture.GameOutputSnapshot().Length;
						owner.SendSpeech(".TestCheckHouseAccess " + strangerUid);
						Assert.True(this.fixture.WaitForGameOutputSince("HOUSE_ACCESS_CHECK:", markCheckStranger, TimeSpan.FromSeconds(10)));
						Assert.Contains("hasAccess=False",
							LastLineContaining(this.fixture.GameOutputSnapshot(), "HOUSE_ACCESS_CHECK:"));
					}
				}
			}
		}

		private static (uint DeedUid, uint BackpackUid) ExtractHouseSetup(string output, int since) {
			int idx = output.IndexOf("HOUSE_SETUP: deed=#", since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find HOUSE_SETUP after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + "HOUSE_SETUP: deed=#".Length);
			uint deedUid = ParseLeadingUInt(rest);
			int bpIdx = rest.IndexOf("backpack=#", StringComparison.Ordinal);
			Assert.True(bpIdx >= 0, "Could not find backpack uid in:\n" + rest);
			uint backpackUid = ParseLeadingUInt(rest.Substring(bpIdx + "backpack=#".Length));
			return (deedUid, backpackUid);
		}

		private static uint ParseLeadingUInt(string text) {
			int end = 0;
			while (end < text.Length && char.IsDigit(text[end])) {
				end++;
			}
			return uint.Parse(text.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

		private static uint ExtractUid(string output, int since) {
			int idx = output.IndexOf("MY_UID: ", since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find MY_UID after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + "MY_UID: ".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HousingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "HousingPair";
	}
}
