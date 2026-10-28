using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// User-requested follow-up (#758) to the teventStubs batches - a new "Monster Abilities"
	/// .settings page, plus a configurable multi-tile knockback push. See scripts/Tests/
	/// MonsterAbilitiesSettingsTests.cs's own doc comment for the full writeup.
	/// </summary>
	[Collection(MonsterAbilitiesSettingsCollection.Name)]
	public class MonsterAbilitiesSettingsTests {
		private const string Account = "monsterabilitiessettingstest";
		private const string Password = "monsterabilitiessettingstest";

		private readonly GameServerPairFixture fixture;

		public MonsterAbilitiesSettingsTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void KnockbackDistanceSettingDrivesRealPushDistance() {
			using (var client = this.ConnectLoginAndCreateCharacter("KnockbackSettingsChecker")) {
				//same "teleport to confirmed open ground" precedent as
				//MonsterAbilitiesBatchTests.KnockbackPushesDefenderFartherAway - both trials
				//need real open room so a map-edge/water tile never masks the setting's effect.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestKnockbackDistanceSettingScalesPush");
				Assert.True(this.fixture.WaitForGameOutput("KNOCKBACK_DISTANCE_SETTING_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "KNOCKBACK_DISTANCE_SETTING_TEST:");
				int pushedAt1 = ParseIntAfter(line, "pushedAt1=");
				int pushedAt5 = ParseIntAfter(line, "pushedAt5=");
				//both start 1 tile apart - a KnockbackDistance=1 push should land them 2 apart
				//(1 extra tile), and a KnockbackDistance=5 push strictly farther than that, on
				//real open ground. Not hardcoding the exact numbers since the push direction
				//(and thus whether Point2D's distance adds up perfectly linearly) depends on
				//whichever open-ground spot .TestTeleportToOpenGround happened to find.
				Assert.True(pushedAt1 > 1, "Full line: " + line);
				Assert.True(pushedAt5 > pushedAt1, "Full line: " + line);
			}
		}

		[Fact]
		public void SettingsPageWritesThroughToLiveAbilityDef() {
			using (var client = this.ConnectLoginAndCreateCharacter("SettingsWritethroughChecker")) {
				client.SendSpeech(".TestMonsterAbilitiesSettingsWritesThroughToLiveAbility");
				Assert.True(this.fixture.WaitForGameOutput("SETTINGS_WRITETHROUGH_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SETTINGS_WRITETHROUGH_TEST:");
				Assert.Contains("liveMatches=True", line);
				Assert.Contains("readBackMatches=True", line);
			}
		}

		[Fact]
		public void KnockbackWorksOnARealRangedBowWieldingAttacker() {
			using (var client = this.ConnectLoginAndCreateCharacter("RangedKnockbackChecker")) {
				//same "teleport to confirmed open ground" precedent as the other knockback
				//tests in this file/MonsterAbilitiesBatchTests - the default spawn point is a
				//known narrow/boxed-in antechamber (#210), and the dummy here is placed 5
				//tiles out, well beyond what that default spot can guarantee.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestKnockbackWorksOnARangedBowWieldingAttacker");
				Assert.True(this.fixture.WaitForGameOutput("KNOCKBACK_RANGED_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "KNOCKBACK_RANGED_TEST:");
				Assert.Contains("isBow=True", line);
				Assert.Contains("pushedAway=True", line);
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

		private static int ParseIntAfter(string line, string prefix) {
			int idx = line.IndexOf(prefix, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + prefix + "' in: " + line);
			int start = idx + prefix.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start));
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

	[CollectionDefinition(MonsterAbilitiesSettingsCollection.Name, DisableParallelization = true)]
	public class MonsterAbilitiesSettingsCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterAbilitiesSettingsPair";
	}
}
