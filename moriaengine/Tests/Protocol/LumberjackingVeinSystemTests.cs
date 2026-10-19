using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Lumberjacking's per-tile vein system (#421/#422) - real Erebor wood-tier mechanics: a
	/// vein has one assigned wood type, insufficient skill downgrades to copper rather than
	/// failing, the pool depletes per extraction, and regenerates (with a fresh wood roll)
	/// once its timer elapses. Mirrors MiningVeinSystemTests.cs - no cave-in equivalent here
	/// (confirmed Mining-specific). The monster ambush mechanic (#431, Phase C of #421) is
	/// also covered here, mirroring Mining's own ambush Fact.
	/// </summary>
	[Collection(LumberjackingVeinSystemCollection.Name)]
	public class LumberjackingVeinSystemTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltestb";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LumberjackingVeinSystemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void UntouchedVeinYieldsTheSameWoodTypeUntilDepleted() {
			using (var client = this.ConnectLoginAndCreateCharacter("WoodVeinSameChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Iron's real skill threshold is 400 (x10-scaled) - set above it so the
				// skill-gated downgrade never fires, isolating the "same wood every time" check.
				client.SendSpeech(".TestSetSkillValue \"Lumberjacking\" 500");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceWoodVein 1000 2000 \"iron\" 3");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				for (int i = 0; i < 3; i++) {
					mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestChopAtTile 1000 2000");
					Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:");
					Assert.Contains("wood=i_log_iron", line); //same wood every time, pool draining
				}

				// 4th attempt - pool is now exhausted (started at 3, drained to 0), regen hasn't
				// elapsed (30 real minutes away) - no wood.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestChopAtTile 1000 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string exhaustedLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:");
				Assert.Contains("wood=null", exhaustedLine);
			}
		}

		[Fact]
		public void UnderSkilledChopperGetsCopperInsteadOfTheRealAssignedWood() {
			using (var client = this.ConnectLoginAndCreateCharacter("WoodVeinDowngradeChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Mithril's real skill threshold is 1050 (x10-scaled) - a brand new character's
				// Lumberjacking skill (0) is nowhere close, so the extraction must downgrade.
				client.SendSpeech(".TestForceWoodVein 1100 2200 \"mithril\" 5");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestChopAtTile 1100 2200");
				Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:");
				Assert.Contains("wood=i_log_copper", line); //downgraded, not denied outright
			}
		}

		[Fact]
		public void VeinRegeneratesWithAFreshWoodRollOnceReady() {
			using (var client = this.ConnectLoginAndCreateCharacter("WoodVeinRegenChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSkillValue \"Lumberjacking\" 500");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceWoodVein 1200 2300 \"iron\" 1");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_VEIN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				// Drain the forced single-unit pool.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestChopAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("wood=i_log_iron", LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:"));

				// Confirm exhausted before forcing regen-ready.
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestChopAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("wood=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:"));

				// Force the regen timer to have already elapsed - the next hit should roll
				// fresh wood (random reroll, so we just confirm wood comes back at all).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestForceWoodVeinRegenReady 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_VEIN_REGEN_FORCED:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestChopAtTile 1200 2300");
				Assert.True(this.fixture.WaitForGameOutputSince("CHOP_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.DoesNotContain("wood=null", LastLineContaining(this.fixture.GameOutputSnapshot(), "CHOP_RESULT:"));
			}
		}

		[Fact]
		public void UntouchedVeinRollFavorsCommonWoodOverRareOnes() {
			using (var client = this.ConnectLoginAndCreateCharacter("WoodVeinDistributionChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// 2000 samples - same margin reasoning as Mining's own UntouchedVeinRollFavors
				// CommonOresOverRareOnes (mithril's small expected count has enough binomial
				// variance at 500 samples to occasionally violate a 5x-ratio bound by chance).
				client.SendSpeech(".TestSampleWoodVeinDistribution 2000 5000");
				Assert.True(this.fixture.WaitForGameOutputSince("WOOD_DISTRIBUTION_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "WOOD_DISTRIBUTION_SAMPLE:");
				int copper = ExtractInt(line, "copper=");
				int mithril = ExtractInt(line, "mithril=");
				Assert.True(copper > mithril * 5, "Expected copper to be rolled far more often than mithril: " + line);
			}
		}

		[Fact]
		public void AmbushTriggersAtRoughlyTheRealRateAndFavorsWolfOverReaper() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "LumberjackAmbushChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleLumberjackAmbushChance 40000");
				Assert.True(this.fixture.WaitForGameOutputSince("LUMBERJACK_AMBUSH_CHANCE_SAMPLE:", mark, TimeSpan.FromSeconds(30)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LUMBERJACK_AMBUSH_CHANCE_SAMPLE:");
				int total = ExtractInt(line, "total=");
				int wolf = ExtractInt(line, "wolf=");
				int reaperSpruce = ExtractInt(line, "reaperSpruce=");
				// Expected ~53 total ambushes over 40000 attempts at 1/750 - generous bounds
				// (>0, <10%) catch a completely broken roll without being flaky on the real one.
				Assert.True(total > 0 && total < 4000, "Expected roughly 1/750 of 40000 attempts to ambush: " + line);
				// Wolf is ~49% of triggers, the Reaper bucket only ~3% - wolf should clearly dominate.
				Assert.True(wolf > reaperSpruce * 3, "Expected wolf ambushes to be rolled far more often than Reaper: " + line);
			}
		}

		private static int ExtractInt(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while (end < line.Length && char.IsDigit(line[end])) {
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
		/// pattern as Mining's own MiningVeinSystemTests.cs) - the ambush Fact gets its own
		/// account rather than sharing the other Facts' character-slot usage on the shared
		/// Account.</summary>
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

		private int WaitForServerListingAndGetRedirectPort() {
			return this.WaitForServerListingAndGetRedirectPort(Account);
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
	public class LumberjackingVeinSystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LumberjackingVeinSystemPair";
	}
}
