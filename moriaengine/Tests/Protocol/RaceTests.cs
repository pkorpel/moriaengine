using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Races system (#161-165): the race/profession/nation selection flow (#162), realm-based
	/// notoriety (#163), the Home property (#164), and per-profession title ladders (#165).
	/// </summary>
	[Collection(RaceCollection.Name)]
	public class RaceTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public RaceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Mage's own strRange/dexRange/intRange/vitRange (mage.def): {50,80,90},
		/// {60,90,100}, {100,150,230}, {50,80,100} - min/startingMax/hardCap. These 4 values sum
		/// to exactly 300 and each sits inside its own [min, hardCap], so D_RaceClassStats (#176)
		/// accepts them.</summary>
		private static readonly Dictionary<int, string> MageValidStats = new Dictionary<int, string> {
			{ 10, "60" }, // Str entry id
			{ 11, "70" }, // Dex entry id
			{ 12, "120" }, // Int entry id
			{ 13, "50" }, // Vit entry id
		};

		/// <summary>Full end-to-end flow for a race with a REAL nation choice (Human: 7
		/// nations) - drives the whole thing through the real i_stone_raceclass item (#174),
		/// not a test-only command, confirming the dclick trigger itself works. Order matches
		/// Erebor's own stone exactly: profession -> race -> nation -> stats (#173/#175/#176).
		/// Button ids are computed directly (FirstOptionButtonId=1 + known index) rather than
		/// parsed from the gump layout text - safe since this test controls D_OptionPicker's own
		/// button-id scheme.</summary>
		[Fact]
		public void FullRaceSelectionFlowForHuman() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "HumanFlowTester")) {
				int mark0 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnRaceStone");
				Assert.True(this.fixture.WaitForGameOutputSince("RACE_STONE_SPAWNED:", mark0, TimeSpan.FromSeconds(10)));
				uint stoneUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark0, "RACE_STONE_SPAWNED: uid=");

				client.SendDoubleClick(stoneUid);
				var (gumpUid1, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid1, 1); // profession index 0 = Mage

				var (gumpUid2, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid2, 1); // race index 0 = Human

				var (gumpUid3, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid3, 1); // nation index 0 = Gondoran (Human's first choice)

				var (gumpUid4, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid4, 1, MageValidStats); // stats submit button id = 1

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckRaceState");
				Assert.True(this.fixture.WaitForGameOutputSince("RACE_STATE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RACE_STATE:");

				Assert.Contains("race=Human", line);
				Assert.Contains("nation=Gondoran", line);
				Assert.Contains("realm=Gondor", line);
				Assert.Contains("home=5678,3871,-80", line);
				Assert.Contains("title=ucen", line); // Mage's level-0 title
				Assert.Contains("str=60", line);
				Assert.Contains("dex=70", line);
				Assert.Contains("int=120", line);
				Assert.Contains("vit=50", line);
			}
		}

		/// <summary>Sibling of the Human flow - Dwarf is locked to exactly one nation
		/// (Ereboran), so the nation-picker gump should never appear at all; the race picker is
		/// followed directly by the stats dialog.</summary>
		[Fact]
		public void FixedNationRaceSkipsNationPicker() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "DwarfFlowTester")) {
				int mark0 = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnRaceStone");
				Assert.True(this.fixture.WaitForGameOutputSince("RACE_STONE_SPAWNED:", mark0, TimeSpan.FromSeconds(10)));
				uint stoneUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark0, "RACE_STONE_SPAWNED: uid=");

				client.SendDoubleClick(stoneUid);
				var (gumpUid1, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid1, 1); // profession index 0 = Mage

				var (gumpUid2, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid2, 3); // race index 2 = Dwarf

				var (gumpUid3, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(gumpUid3, 1, MageValidStats); // stats submit, Dwarf skipped nation picker

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckRaceState");
				Assert.True(this.fixture.WaitForGameOutputSince("RACE_STATE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RACE_STATE:");

				Assert.Contains("race=Dwarf", line);
				Assert.Contains("nation=Ereboran", line);
				Assert.Contains("realm=Gondor", line);
				Assert.Contains("home=3157,965,40", line);
			}
		}

		/// <summary>Direct logic check of the realm-based notoriety rules (#163) - see
		/// RaceTests.TestCheckRealmNotoriety's own doc comment for why this avoids packet-level
		/// highlight-color decoding entirely.</summary>
		[Fact]
		public void RealmDrivesNotoriety() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "NotorietyChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckRealmNotoriety");
				Assert.True(this.fixture.WaitForGameOutputSince("REALM_NOTORIETY:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "REALM_NOTORIETY:");

				Assert.Contains("mordorSeenByGondor=Murderer", line);
				Assert.Contains("gondorSeenByMordor=Innocent", line);
				Assert.Contains("crossRelation=AlwaysHostile", line);
				Assert.Contains("sameRealmRelation=Allied", line);
			}
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}

		private static uint ExtractUid(string output, int sincePosition, string marker) {
			int idx = output.IndexOf(marker, Math.Max(0, sincePosition - marker.Length), StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int start = idx + marker.Length;
			int end = start;
			while ((end < output.Length) && char.IsDigit(output[end])) {
				end++;
			}
			return uint.Parse(output.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	[CollectionDefinition(RaceCollection.Name, DisableParallelization = true)]
	public class RaceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RacePair";
	}
}
