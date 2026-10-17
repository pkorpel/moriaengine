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
		private const string AccountB = "protocoltestb";
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
				// 2000 samples, not 500 - mithril's own expected count at 500 samples (~8) has
				// enough binomial variance to occasionally violate a 5x-ratio bound by pure
				// chance (confirmed by reproducing a flake, then rerunning the same assertion
				// in isolation clean) - 2000 pushes the margin from ~2.5 sigma to ~6+ sigma.
				client.SendSpeech(".TestSampleVeinOreDistribution 2000 5000");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_DISTRIBUTION_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "VEIN_DISTRIBUTION_SAMPLE:");
				int copper = ExtractInt(line, "copper=");
				int mithril = ExtractInt(line, "mithril=");
				Assert.True(copper > mithril * 5, "Expected copper to be rolled far more often than mithril: " + line);
			}
		}

		[Fact]
		public void CaveInBlocksMiningUntilSuccessfullyCleared() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "CaveInChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceVein 3000 4000 \"copper\" 20");
				Assert.True(this.fixture.WaitForGameOutputSince("VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceCaveIn 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckCaveInBlocked 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_BLOCKED_CHECK:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("blocked=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "CAVE_IN_BLOCKED_CHECK:"));

				// Mining a cave-in-blocked tile never yields ore, blocked or not.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("ore=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:"));

				// Real clear-roll needs skill >= 1000 (100.0%) to have ANY chance at all - a
				// brand-new character (skill 0) must always fail.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTryClearCaveIn 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_CLEAR_ATTEMPT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("cleared=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "CAVE_IN_CLEAR_ATTEMPT:"));

				// Set skill to the maximum of the real clear-roll range - guaranteed success.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSkillValue \"Mining\" 1300");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTryClearCaveIn 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_CLEAR_ATTEMPT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("cleared=True", LastLineContaining(this.fixture.GameOutputSnapshot(), "CAVE_IN_CLEAR_ATTEMPT:"));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckCaveInBlocked 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_BLOCKED_CHECK:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("blocked=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "CAVE_IN_BLOCKED_CHECK:"));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMineAtTile 3000 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("MINE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.DoesNotContain("ore=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "MINE_RESULT:"));
			}
		}

		[Fact]
		public void CaveInTriggersAtRoughlyTheRealOneIn120Rate() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "CaveInChanceChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleCaveInChance 3000 10000");
				Assert.True(this.fixture.WaitForGameOutputSince("CAVE_IN_CHANCE_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "CAVE_IN_CHANCE_SAMPLE:");
				int caveIns = ExtractInt(line, "caveIns=");
				// Expected ~25 over 3000 attempts at 1/120 - generous bounds (>0, <10%) catch a
				// completely broken roll in either direction without being flaky on the real one.
				Assert.True(caveIns > 0 && caveIns < 300, "Expected roughly 1/120 of 3000 attempts to cave in: " + line);
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
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		/// <summary>AccountB (needs EnsureAutoAccountCreationEnabled called once first, same
		/// pattern as AttrFlagTests.cs/SkillGainSystemTests.cs) - this file's 6 Facts each
		/// create their own character on the shared Account, over the 5-slot cap, so the 2
		/// cave-in Facts get their own account.</summary>
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

		private void PromoteToGm(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestPromoteToGM");
			Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));
		}

		private static bool autoAccountCreationEnabled;

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
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
	public class MiningVeinSystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MiningVeinSystemPair";
	}
}
