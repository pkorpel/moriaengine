using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#813 - static regions come from the converted archive map (scripts/converted/map.def,
	/// generated from the Erebor archive's spheremap.scp) on every boot, and the world save only
	/// holds what GMs changed in-game: an edited archive region, a deleted one (tombstone) and a
	/// newly created one all survive a real save + restart, while everything else is still the
	/// archive's. Server-side helpers: scripts/Tests/RegionArchiveTests.cs.</summary>
	[Collection(RegionArchiveCollection.Name)]
	public class RegionArchiveTests {
		private const string Account = "regionarchivetest";
		private const string Password = "regionarchivetest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		[Fact]
		public void ArchiveRegionsLoad_GmEditsSurviveARealRestart() {
			using (var fixture1 = new GameServerPairFixture()) {
				using (var client = Connect(fixture1, "Region Checker")) {
					string count = Ask(fixture1, client, ".TestRegionCount", "REGION_COUNT:");
					AssertHas(count, "all=769 fromScripts=769 gmModified=0");

					string mine = Ask(fixture1, client, ".TestRegionReport \"a_Ereborsky_dul\"", "REGION_REPORT: a_Ereborsky_dul");
					AssertHas(mine, "exists=True name=Ereborsky dul fromScripts=True gmModified=False flags=0x812 events=r_doly_nohorse resources=r_doly group=Doly parent=a_world rects=1");
					//a region the old frozen regions.sav never had (Barad-dur), with a child house
					string house = Ask(fixture1, client, ".TestRegionReport \"a_dum_1\"", "REGION_REPORT: a_dum_1");
					AssertHas(house, "events=r_playerhouse+r_nosumon");
					AssertHas(house, "parent=a_Barad_dur");
					AssertHas(Ask(fixture1, client, ".TestRegionAt 0 4740 2190", "REGION_AT: 0,4740,2190"), "-> a_Ereborsky_dul");

					AssertHas(Ask(fixture1, client, ".TestRegionGmEdits", "REGION_GM_EDITS:"), "created=True");
					Ask(fixture1, client, ".TestTriggerGlobalSave", "TEST_SAVE: done");
				}
			}

			using (var fixture2 = GameServerPairFixture.BootPreservingSaves()) {
				using (var client = Connect(fixture2, "Region Rechecker")) {
					string mine = Ask(fixture2, client, ".TestRegionReport \"a_Ereborsky_dul\"", "REGION_REPORT: a_Ereborsky_dul");
					AssertHas(mine, "fromScripts=True gmModified=True flags=0x8812 events=r_doly_nohorse resources=r_doly");
					AssertHas(Ask(fixture2, client, ".TestRegionReport \"a_dum_2\"", "REGION_REPORT: a_dum_2"), "exists=False");
					AssertHas(Ask(fixture2, client, ".TestRegionReport \"a_test_gm_region\"", "REGION_REPORT: a_test_gm_region"), "exists=True name=Test GM Region fromScripts=False");
					//769 archive - 1 deleted + 1 GM-created; only the edited archive region counts as GM-modified (a GM-created
					//region is saved anyway, being from the save not the archive)
					AssertHas(Ask(fixture2, client, ".TestRegionCount", "REGION_COUNT:"), "all=769 fromScripts=768 gmModified=1");
					AssertHas(Ask(fixture2, client, ".TestRegionAt 0 100 100", "REGION_AT: 0,100,100"), "-> a_test_gm_region");
					AssertHas(Ask(fixture2, client, ".TestRegionAt 0 4740 2190", "REGION_AT: 0,4740,2190"), "-> a_Ereborsky_dul");
				}
			}
		}

		private static TestGameClient Connect(GameServerPairFixture fixture, string charName) {
			int redirectPort = WaitForServerListingAndGetRedirectPort(fixture, Account);
			TestGameClient client = new TestGameClient("127.0.0.1", redirectPort);
			client.SendLogin(Account, Password);
			client.ReadCharacterList();
			client.SendCreateCharacter(charName);
			Thread.Sleep(500);
			return client;
		}

		private static string Ask(GameServerPairFixture fixture, TestGameClient client, string speech, string marker) {
			int mark = fixture.GameOutputSnapshot().Length;
			client.SendSpeech(speech);
			Assert.True(fixture.WaitForGameOutputSince(marker, mark, Wait), "no '" + marker + "' after " + speech);
			return LastLineContaining(fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
		}

		private static int WaitForServerListingAndGetRedirectPort(GameServerPairFixture fixture, string account) {
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
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RegionArchiveCollection {
		public const string Name = "RegionArchivePair";
	}
}
