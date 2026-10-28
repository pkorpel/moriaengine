using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Real-user-reported follow-up to #653's teventStubs batch - a_tevent_e_npc_knockback/
	/// e_selfreser/e_npc_detect/e_npc_leaper/e_looter are upgraded from zero-effect stubs to
	/// real behavior (MonsterSpecialAttacks.cs, Character.CauseDeath, NPCAiTimer.Fight,
	/// Corpse.On_Step). See scripts/Tests/MonsterAbilitiesBatchTests.cs's own doc comment for
	/// the full writeup. e_item_refuser is deliberately left out of this batch - no feasible
	/// hook exists in this engine for it (no give/drop-onto-character interaction at all).
	/// </summary>
	[Collection(MonsterAbilitiesBatchCollection.Name)]
	public class MonsterAbilitiesBatchTests {
		private const string Account = "monsterabilitiesbatchtest";
		private const string Password = "monsterabilitiesbatchtest";

		private readonly GameServerPairFixture fixture;

		public MonsterAbilitiesBatchTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void KnockbackPushesDefenderFartherAway() {
			using (var client = this.ConnectLoginAndCreateCharacter("KnockbackChecker")) {
				//same "teleport to confirmed open ground" precedent as
				//MonsterSpecialAttacksTests.RoundattackPullDragsDistantTargetCloser - the push
				//needs real open room to be observable (PushAway correctly skips rather than
				//clips into an adjacent impassable tile).
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				client.SendSpeech(".TestKnockbackPushesTargetAway");
				Assert.True(this.fixture.WaitForGameOutput("KNOCKBACK_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "KNOCKBACK_TEST:");
				Assert.True(line.Contains("pushedAway=True"), "Full line: " + line);
			}
		}

		[Fact]
		public void SelfResRespawnsAFreshInstanceAfterTheConfiguredDelay() {
			using (var client = this.ConnectLoginAndCreateCharacter("SelfResChecker")) {
				client.SendSpeech(".TestKillSelfResMonster");
				Assert.True(this.fixture.WaitForGameOutput("SELFRES_KILL_TEST:", TimeSpan.FromSeconds(10)));

				int beforeTimerLength = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSelfResMonsterCount");
				Assert.True(this.fixture.WaitForGameOutputSince("SELFRES_COUNT_TEST:", beforeTimerLength, TimeSpan.FromSeconds(10)));
				string immediateLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "SELFRES_COUNT_TEST:");
				Assert.Contains("count=0", immediateLine); //dead and not yet respawned - delay hasn't elapsed

				//npc_test_selfres_attacker's own SelfresDelay is 1 second - wait well past it.
				Thread.Sleep(2500);
				int beforeRecheckLength = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportSelfResMonsterCount");
				Assert.True(this.fixture.WaitForGameOutputSince("SELFRES_COUNT_TEST:", beforeRecheckLength, TimeSpan.FromSeconds(10)));
				//the LAST occurrence of this marker is always the most recent one, since this
				//is sent strictly after the earlier count=0 check above.
				string afterLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "SELFRES_COUNT_TEST:");
				Assert.Contains("count=1", afterLine);
			}
		}

		[Fact]
		public void DetectRevealsAHiddenTarget() {
			using (var client = this.ConnectLoginAndCreateCharacter("DetectChecker")) {
				client.SendSpeech(".TestDetectRevealsHiddenTarget");
				Assert.True(this.fixture.WaitForGameOutput("DETECT_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "DETECT_TEST:");
				Assert.Contains("stillHidden=False", line);
			}
		}

		[Fact]
		public void LeaperGrantsExtraStepsOnlyToNpcsWithTheAbility() {
			using (var client = this.ConnectLoginAndCreateCharacter("LeaperChecker")) {
				client.SendSpeech(".TestLeaperExtraSteps");
				Assert.True(this.fixture.WaitForGameOutput("LEAPER_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEAPER_TEST:");
				Assert.Contains("leaperSteps=10", line);
				Assert.Contains("nonLeaperSteps=0", line);
			}
		}

		[Fact]
		public void LooterMovesOneItemFromALootableCorpseIntoItsOwnBackpack() {
			using (var client = this.ConnectLoginAndCreateCharacter("LooterChecker")) {
				client.SendSpeech(".TestLooterMovesItemFromCorpse");
				Assert.True(this.fixture.WaitForGameOutput("LOOTER_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LOOTER_TEST:");
				Assert.Contains("itemsBefore=1", line);
				Assert.Contains("lootedItem=True", line);
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

	[CollectionDefinition(MonsterAbilitiesBatchCollection.Name, DisableParallelization = true)]
	public class MonsterAbilitiesBatchCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterAbilitiesBatchPair";
	}
}
