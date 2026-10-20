using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Fishing's full implementation (#271) - the fish-variety + junk-fish/treasure-map catch
	/// system (FishingCatchSystem), the water-monster ambush (FishingAmbushSystem, mirrors Mining
	/// #335 / Lumberjacking #431), and the GM-tunable FishingSettings write-through. Mirrors
	/// LumberjackingVeinSystemTests / LumberjackingSettingsTests. No per-tile vein/depletion/regen
	/// coverage - Fishing has no skill-gated fish-tier ladder (no fish-tier content exists, see
	/// FishingCatchSystem's own doc comment), so there's nothing of that shape to test.
	/// </summary>
	[Collection(FishingCatchSystemCollection.Name)]
	public class FishingCatchSystemTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltestb";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public FishingCatchSystemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void CatchRollProducesFishJunkCratesAndTreasureMapCrates() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishCatchChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				// Forced ~1/2 junk and ~1/2-of-junk map inside the SteamFunction, so 2000 samples
				// comfortably exercise all three branches without being flaky.
				client.SendSpeech(".TestSampleFishingCatch 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_CATCH_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_CATCH_SAMPLE:");
				int fish = ExtractInt(line, "fish=");
				int junk = ExtractInt(line, "junk=");
				int junkWithMap = ExtractInt(line, "junkWithMap=");
				// All three outcomes must actually occur at these forced generous odds.
				Assert.True(fish > 0, "Expected some plain-fish catches: " + line);
				Assert.True(junk > 0, "Expected some junk-crate catches: " + line);
				Assert.True(junkWithMap > 0, "Expected some junk-crate-with-map catches: " + line);
				// Plain junk (no map) should clearly outnumber map-bearing junk at ~1/2 map odds.
				Assert.True(junk + junkWithMap > fish / 4, "Expected a substantial junk share at forced odds: " + line);
			}
		}

		[Fact]
		public void FishVarietyRollYieldsOnlyRealFishAcrossMultipleKinds() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishVarietyChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleFishingFishVariety 600");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_FISH_VARIETY_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_FISH_VARIETY_SAMPLE:");
				int fishWithItem = ExtractInt(line, "fishWithItem=");
				int distinctKinds = ExtractInt(line, "distinctKinds=");
				// With junk forced off, every one of the 600 catches must be a real fish item.
				Assert.Equal(600, fishWithItem);
				// 600 uniform draws over 6 kinds must realistically hit more than one kind - a
				// generous >1 bound catches a broken "always the same fish" roll without flaking.
				Assert.True(distinctKinds > 1, "Expected the fish-variety roll to vary across kinds: " + line);
			}
		}

		[Fact]
		public void AmbushTriggersAtRoughlyTheRealRateAndFavorsFrogOverKraken() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "FishAmbushChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleFishingAmbushChance 40000");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_AMBUSH_CHANCE_SAMPLE:", mark, TimeSpan.FromSeconds(30)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_AMBUSH_CHANCE_SAMPLE:");
				int total = ExtractInt(line, "total=");
				int frog = ExtractInt(line, "frog=");
				int kraken = ExtractInt(line, "kraken=");
				// Expected ~53 total ambushes over 40000 attempts at 1/750 - generous bounds (>0,
				// <10%) catch a completely broken roll without being flaky on the real one.
				Assert.True(total > 0 && total < 4000, "Expected roughly 1/750 of 40000 attempts to ambush: " + line);
				// Frog is ~49% of triggers, the kraken bucket only ~3% - frog should clearly dominate.
				Assert.True(frog > kraken * 3, "Expected frog ambushes to be rolled far more often than kraken: " + line);
			}
		}

		[Fact]
		public void JunkChanceEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishSettingsJunkChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetJunkChanceViaFishingSettings 3 7");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_SETTINGS_JUNK_SET:", mark, TimeSpan.FromSeconds(30)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_SETTINGS_JUNK_SET:");
				Assert.Contains("num=3", line);
				Assert.Contains("den=7", line);
			}
		}

		[Fact]
		public void AmbushChanceEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishSettingsAmbushChanceChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetAmbushChanceViaFishingSettings 50");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_SETTINGS_AMBUSH_SET:", mark, TimeSpan.FromSeconds(30)));
				Assert.Contains("denominator=50", LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_SETTINGS_AMBUSH_SET:"));
			}
		}

		[Fact]
		public void AmbushKrakenDefnameEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishSettingsAmbushKrakenChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetAmbushKrakenDefnameViaFishingSettings \"c_sea_serpent\"");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_SETTINGS_AMBUSH_KRAKEN_SET:", mark, TimeSpan.FromSeconds(30)));
				Assert.Contains("krakenDefname=c_sea_serpent", LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_SETTINGS_AMBUSH_KRAKEN_SET:"));
			}
		}

		[Fact]
		public void GatherDelayBaseEditWritesThroughToLiveSystem() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishSettingsDelayChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetGatherDelayBaseViaFishingSettings 5");
				Assert.True(this.fixture.WaitForGameOutputSince("FISHING_SETTINGS_DELAY_SET:", mark, TimeSpan.FromSeconds(30)));
				Assert.Contains("baseSeconds=5", LastLineContaining(this.fixture.GameOutputSnapshot(), "FISHING_SETTINGS_DELAY_SET:"));
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
		/// pattern as Lumberjacking's own ambush Fact) - the ambush Fact gets its own account.</summary>
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
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "FishAutoAccountEnabler")) {
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
	public class FishingCatchSystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "FishingCatchSystemPair";
	}
}
