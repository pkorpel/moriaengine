using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Fishing's full implementation (#271) - the fish-variety + junk-fish/treasure-map catch
	/// system (FishingCatchSystem), the water-monster ambush (FishingAmbushSystem, mirrors Mining
	/// #335 / Lumberjacking #431), and the GM-tunable FishingSettings write-through. Mirrors
	/// LumberjackingVeinSystemTests / LumberjackingSettingsTests. Since #813 the catch comes from
	/// the water spot's region resources (the archive's w_fish/w_jf tiers, per-spot pools).
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

		/// <summary>#813 - the world's water rolls from the archive's r_default_water region type
		/// (100 nothing / 29 fish / 10 junk fish): about seven in ten spots hold nothing, fish beat junk.</summary>
		[Fact]
		public void WaterRegionRollFollowsTheArchive() {
			using (var client = this.ConnectLoginAndCreateCharacter("FishCatchChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleWaterRegionRoll 4000");
				Assert.True(this.fixture.WaitForGameOutputSince("WATER_REGION_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "WATER_REGION_SAMPLE:");
				int nothing = ExtractInt(line, "nothing=");
				int fish = ExtractInt(line, "fish=");
				int junk = ExtractInt(line, "junk=");
				Assert.True(nothing > 2400 && nothing < 3200, "Expected about 70 percent nothing: " + line);
				Assert.True(fish > junk && junk > 0, "Expected fish more common than junk, and some junk: " + line);
			}
		}

		/// <summary>#813 - a spot holds what it rolled until its pool runs out, then nothing; an
		/// under-skilled fisher gets w_fish1 instead (Erebor's fixresource); junk fish keep the junk
		/// crate payoff.</summary>
		[Fact]
		public void SpotPoolSkillGateAndJunk() {
			this.EnsureAutoAccountCreationEnabled(); //AccountB - "protocoltest" is at its 5-character cap
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "FishSpotChecker")) {
				AssertHas(this.SpotCatches(client, "w_fish3", 1000), "Fish:i_fish_big_3 Fish:i_fish_big_3 Nothing:none");
				AssertHas(this.SpotCatches(client, "w_jf1", 0), "Fish:i_fish_big_1 Fish:i_fish_big_1 Nothing:none");
				string junk = this.SpotCatches(client, "w_jf1", 1000);
				Assert.True(junk.Contains("JunkCrate", StringComparison.Ordinal) && junk.Contains("i_crate_small", StringComparison.Ordinal) && junk.EndsWith("Nothing:none", StringComparison.Ordinal), "Expected two junk crates then nothing: " + junk);
			}
		}

		private string SpotCatches(TestGameClient client, string resource, int skill) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestFishingSpotCatches \"" + resource + "\" " + skill);
			string marker = "FISHING_SPOT_CATCHES: " + resource + " skill=" + skill;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, TimeSpan.FromSeconds(15)), "no " + marker);
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker).TrimEnd('\r');
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
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
