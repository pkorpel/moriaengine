using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Mining's per-tile vein system (#140/#332/#333) - real Erebor ore-tier mechanics: a vein
	/// has one assigned ore type, insufficient skill downgrades to copper rather than failing,
	/// the pool depletes per extraction, and regenerates (with a fresh ore roll) once its timer
	/// elapses. Uses the test-only ForceVein/ForceVeinRegenReady hooks (scripts/Tests/
	/// MiningVeinSystemTests.cs) for deterministic coverage of a system whose real ore
	/// assignment is randomized.
	/// </summary>
	[Collection(MiningVeinSystemCollection.Name)]
	public class MiningVeinSystemTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public MiningVeinSystemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void UntouchedVeinYieldsTheSameOreTypeUntilDepleted() {
			using (var client = this.ConnectLoginAndCreateCharacter("VeinSameOreChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Iron's real skill threshold is 300 (x10-scaled) - set above it so the
				// skill-gated downgrade never fires, isolating the "same ore every time" check.
				client.SendSpeech(".TestSetSkillValue \"Mining\" 400");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceVein 1000 2000 \"iron\" 3");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				for (int i = 0; i < 3; i++) {
					mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestMineAtTile 1000 2000");
					Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:");
					Assert.Contains("ore=i_ore_iron", line); //same ore every time, pool draining
				}

				// 4th attempt - pool is now exhausted (started at 3, drained to 0), regen hasn't
				// elapsed (30/60 real minutes away) - no ore.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 1000 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string exhaustedLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:");
				Assert.Contains("ore=null", exhaustedLine);
			}
		}

		[Fact]
		public void UnderSkilledMinerGetsCopperInsteadOfTheRealAssignedOre() {
			using (var client = this.ConnectLoginAndCreateCharacter("VeinDowngradeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Mithril's real skill threshold is 1050 (x10-scaled) - a brand new character's
				// Mining skill (0) is nowhere close, so every extraction must downgrade to copper.
				client.SendSpeech(".TestForceVein 1100 2200 \"mithril\" 5");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 1100 2200");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:");
				Assert.Contains("ore=i_ore_copper", line); //downgraded, not denied outright
			}
		}

		[Fact]
		public void VeinRegeneratesWithAFreshOreRollOnceReady() {
			using (var client = this.ConnectLoginAndCreateCharacter("VeinRegenChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSkillValue \"Mining\" 400");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceVein 1200 2300 \"iron\" 1");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				// Drain the forced single-unit pool.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("ore=i_ore_iron", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:"));

				// Confirm exhausted before forcing regen-ready.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("ore=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:"));

				// Force the regen timer to have already elapsed - the next hit should roll a
				// FRESH ore (we forced "iron" originally, but the real mechanic rerolls on
				// regen - a high enough sample would show variety; here we just confirm ore
				// actually comes back, not the exact new type, since the reroll is random).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceVeinRegenReady 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_REGEN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.DoesNotContain("ore=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:"));
			}
		}

		[Fact]
		public void UntouchedVeinRollFavorsCommonOresOverRareOnes() {
			using (var client = this.ConnectLoginAndCreateCharacter("VeinDistributionChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleVeinOreDistribution 500 5000");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_DISTRIBUTION_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "VEIN_DISTRIBUTION_SAMPLE:");
				int copper = ExtractInt(line, "copper=");
				int mithril = ExtractInt(line, "mithril=");
				Assert.True(copper > mithril * 5, "Expected copper to be rolled far more often than mithril: " + line);
			}
		}

		private static int ExtractInt(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while (end < line.Length && (char.IsDigit(line[end]))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MiningVeinSystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MiningVeinSystemPair";
	}
}
