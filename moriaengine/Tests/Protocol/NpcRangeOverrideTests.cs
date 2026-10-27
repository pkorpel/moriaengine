using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#716 - see scripts/Tests/NpcRangeOverrideTests.cs's own doc comment for the full
	/// writeup. Own dedicated collection/account (same precedent as RoofLosTests.cs).</summary>
	[Collection(NpcRangeOverrideCollection.Name)]
	public class NpcRangeOverrideTests {
		private const string Account = "npcrangeoverridetest";
		private const string Password = "npcrangeoverridetest";

		private readonly GameServerPairFixture fixture;

		public NpcRangeOverrideTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RangerWeaponRangeOverrideDrivesStrikeStartAndStopRangeToo() {
			using (var client = this.ConnectLoginAndCreateCharacter("WeaponRangeOverrideChecker")) {
				client.SendSpeech(".TestReportRangerWeaponRangeOverride");
				Assert.True(this.fixture.WaitForGameOutput("WEAPON_RANGE_OVERRIDE_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "WEAPON_RANGE_OVERRIDE_TEST:");

				Assert.Contains("defaultRange=8", line);
				Assert.Contains("defaultStrikeStart=8", line);
				Assert.Contains("defaultStrikeStop=10", line);
				Assert.Contains("overriddenRange=15", line);
				Assert.Contains("overriddenStrikeStart=15", line);
				Assert.Contains("overriddenStrikeStop=17", line);
			}
		}

		[Fact]
		public void MageSpellCastRangeOverrideForcesCloserEngagement() {
			using (var client = this.ConnectLoginAndCreateCharacter("SpellCastRangeOverrideChecker")) {
				//both halves matter here, found the hard way: a longer (15-tile) distance from
				//an "open ground" spot can still hit real, unrelated LOS-blocking terrain beyond
				//that helper's own 7-tile-radius guarantee; the DEFAULT spawn point (no teleport
				//at all) is itself a narrow/boxed-in antechamber (#210) where even a short
				//real movement attempt can fail for unrelated reasons. Combining a short (6-
				//tile) distance - the same magnitude NpcRangedApproachTests already uses
				//successfully - with a real teleport to verified-clear-within-7-tiles ground
				//avoids both failure modes.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestReportMageSpellCastRangeOverride");
				Assert.True(this.fixture.WaitForGameOutput("SPELLCAST_RANGE_OVERRIDE_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SPELLCAST_RANGE_OVERRIDE_TEST:");

				Assert.Contains("defaultHeld=True", line);
				Assert.Contains("overriddenApproached=True", line);
			}
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
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(NpcRangeOverrideCollection.Name, DisableParallelization = true)]
	public class NpcRangeOverrideCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcRangeOverridePair";
	}
}
