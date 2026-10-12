using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-5 (#73): notoriety scenarios needing two real, simultaneously-connected characters.
	/// Unlike V-4's social systems, these are driven by real combat packets (0x05 RequestAttack)
	/// rather than speech commands - the Notoriety system only ever engages for player-vs-player
	/// (Notoriety.OnHostileAction/RegisterMurder both bail out early unless both sides are real
	/// players), so this needed its own 2-account harness exercising actual PvP combat, not just
	/// NPC-vs-player as every earlier combat test (V-1) did.
	/// </summary>
	[Collection(NotorietyCollection.Name)]
	public class NotorietyTests {
		private const string AccountA = "protocoltest";
		private const string AccountB = "protocoltest2";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NotorietyTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: attacking an innocent player (not already criminal/murderer)
		/// flags the attacker criminal; the flag expires again after its duration passes, once
		/// the fight stops (Notoriety.CriminalFlagSeconds shortened here, same test-configurable
		/// pattern as LightAndWeather.DayLengthSeconds, so this doesn't need to wait out the real
		/// 2-minute default).</summary>
		[Fact]
		public void HostileActionFlagsAttackerCriminalThenExpires() {
			using (var clientA = this.ConnectLoginAndCreateCharacter(AccountA, "CrimeWatcher")) {
				clientA.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST: ENABLED", TimeSpan.FromSeconds(10)));

				int markDur = this.fixture.GameOutputSnapshot().Length;
				clientA.SendSpeech(".TestSetCriminalFlagSeconds 4");
				Assert.True(this.fixture.WaitForGameOutputSince("CRIMINAL_DURATION_TEST: SET", markDur, TimeSpan.FromSeconds(10)));

				using (var clientB = this.ConnectLoginAndCreateCharacter(AccountB, "InnocentVictim")) {
					int markB = this.fixture.GameOutputSnapshot().Length;
					clientB.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markB, TimeSpan.FromSeconds(10)));
					uint victimUid = ExtractUid(this.fixture.GameOutputSnapshot(), markB);

					clientA.SendRequestAttack(victimUid);

					bool becameCriminal = this.PollUntil(clientA, ".TestCheckCriminal",
						"CRIMINAL_CHECK: CrimeWatcher isCriminal=True", TimeSpan.FromSeconds(65));
					Assert.True(becameCriminal,
						"Expected the attacker to become flagged criminal after a real hostile action. Game output:\n" +
						this.fixture.GameOutputSnapshot());

					int markStop = this.fixture.GameOutputSnapshot().Length;
					clientA.SendSpeech(".TestStopAttacking " + victimUid);
					Assert.True(this.fixture.WaitForGameOutputSince("STOP_ATTACK_TEST: DONE", markStop, TimeSpan.FromSeconds(10)));

					bool expired = this.PollUntil(clientA, ".TestCheckCriminal",
						"CRIMINAL_CHECK: CrimeWatcher isCriminal=False", TimeSpan.FromSeconds(25));
					Assert.True(expired,
						"Expected the shortened 4s criminal flag to expire once the fight stopped. Game output:\n" +
						this.fixture.GameOutputSnapshot());
				}
			}
		}

		/// <summary>Real UO rule: killing the same innocent player repeatedly (without legal
		/// justification) flips the killer's notoriety to Murderer once MurdererThreshold kills
		/// are registered.</summary>
		[Fact]
		public void RepeatedUnjustifiedKillsMakeAMurderer() {
			using (var clientA = this.ConnectLoginAndCreateCharacter(AccountA, "Executioner")) {
				clientA.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutput("AUTO_ACCOUNT_TEST: ENABLED", TimeSpan.FromSeconds(10)));

				using (var clientB = this.ConnectLoginAndCreateCharacter(AccountB, "RepeatVictim")) {
					int markB = this.fixture.GameOutputSnapshot().Length;
					clientB.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", markB, TimeSpan.FromSeconds(10)));
					uint victimUid = ExtractUid(this.fixture.GameOutputSnapshot(), markB);

					int markSim = this.fixture.GameOutputSnapshot().Length;
					clientA.SendSpeech(".TestSimulateMurders " + victimUid);
					Assert.True(this.fixture.WaitForGameOutputSince("MURDER_SIM:", markSim, TimeSpan.FromSeconds(10)));
					Assert.Contains("isMurderer=True",
						LastLineContaining(this.fixture.GameOutputSnapshot(), "MURDER_SIM:"));
				}
			}
		}

		/// <summary>Resends <paramref name="command"/> every couple seconds until
		/// <paramref name="truePattern"/> shows up in the game output, or the budget runs out -
		/// needed because the exact moment a real auto-combat swing lands/misses (and so the
		/// moment Notoriety actually updates) isn't directly observable any other way.</summary>
		private bool PollUntil(TestGameClient client, string command, string truePattern, TimeSpan budget) {
			DateTime deadline = DateTime.UtcNow + budget;
			while (DateTime.UtcNow < deadline) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(command);
				if (this.fixture.WaitForGameOutputSince(truePattern, mark, TimeSpan.FromSeconds(2))) {
					return true;
				}
				Thread.Sleep(500);
			}
			return false;
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

		private static uint ExtractUid(string output, int since) {
			int idx = output.IndexOf("MY_UID: ", since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find MY_UID after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + "MY_UID: ".Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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
	public class NotorietyCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NotorietyPair";
	}
}
