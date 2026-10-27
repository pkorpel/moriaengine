using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_stun/
	/// e_npc_critic/e_npc_roundattack(_pullall) are upgraded from zero-effect stubs to real
	/// combat behavior (MonsterSpecialAttacks.cs). See scripts/Tests/
	/// MonsterSpecialAttacksTests.cs's own doc comment for the full writeup.
	/// </summary>
	[Collection(MonsterSpecialAttacksCollection.Name)]
	public class MonsterSpecialAttacksTests {
		private const string Account = "monsterspecialattackstest";
		private const string Password = "monsterspecialattackstest";

		private readonly GameServerPairFixture fixture;

		public MonsterSpecialAttacksTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StunLandsOnAttackWithGuaranteedChance() {
			using (var client = this.ConnectLoginAndCreateCharacter("StunChecker")) {
				client.SendSpeech(".TestStunLandsOnHit");
				Assert.True(this.fixture.WaitForGameOutput("STUN_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "STUN_TEST:");
				Assert.Contains("stunned=True", line);
			}
		}

		[Fact]
		public void StunNeverLandsOnHighResistTarget() {
			using (var client = this.ConnectLoginAndCreateCharacter("StunResistChecker")) {
				client.SendSpeech(".TestStunNeverAppliesToHighResistTarget");
				Assert.True(this.fixture.WaitForGameOutput("STUN_RESIST_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "STUN_RESIST_TEST:");
				Assert.Contains("stunned=False", line);
			}
		}

		[Fact]
		public void CriticOnlyAddsBonusDamageAgainstPlayers() {
			using (var client = this.ConnectLoginAndCreateCharacter("CriticChecker")) {
				client.SendSpeech(".TestCriticAddsBonusDamageVsPlayerOnly");
				Assert.True(this.fixture.WaitForGameOutput("CRITIC_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "CRITIC_TEST:");
				Assert.Contains("vsPlayerDamage=150", line);
				Assert.Contains("vsMonsterDamage=100", line);
			}
		}

		[Fact]
		public void RoundattackDamagesNearbyTargets() {
			using (var client = this.ConnectLoginAndCreateCharacter("RoundattackChecker")) {
				client.SendSpeech(".TestRoundattackHitsNearbyTargets");
				Assert.True(this.fixture.WaitForGameOutput("ROUNDATTACK_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ROUNDATTACK_TEST:");
				Assert.Contains("damaged=True", line);
			}
		}

		[Fact]
		public void RoundattackPullDragsDistantTargetCloser() {
			using (var client = this.ConnectLoginAndCreateCharacter("RoundattackPullChecker")) {
				//same "teleport to confirmed open ground" precedent as
				//NpcRangeOverrideTests.MageSpellCastRangeOverrideForcesCloserEngagement - the
				//default spawn point can have impassable terrain immediately adjacent, which
				//PullToward correctly refuses to pull a target into (skips rather than clips
				//into a wall) - this test needs real open room for the pull to be observable.
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestRoundattackPullDragsTargetCloser");
				Assert.True(this.fixture.WaitForGameOutput("ROUNDATTACK_PULL_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ROUNDATTACK_PULL_TEST:");
				Assert.True(line.Contains("pulledCloser=True"), "Full line: " + line);
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

	[CollectionDefinition(MonsterSpecialAttacksCollection.Name, DisableParallelization = true)]
	public class MonsterSpecialAttacksCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterSpecialAttacksPair";
	}
}
