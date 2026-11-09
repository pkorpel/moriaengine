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

		/// <summary>Regression for a real user-reported bug: the health-bar/status-overlay
		/// notoriety color (DrawObjectOutPacket's highlight byte, GetHighlightColorFor) was
		/// already correctly rendering blue/red for Gondor/Mordor, but the SEPARATE floating
		/// overhead NAME text from a single click (a different packet, with its own explicit
		/// hue field the client does NOT derive from notoriety itself) stayed uncolored -
		/// Thing.On_Click/On_AosClick always hardcoded hue 0. Fixed by Character.On_Click/
		/// On_AosClick overrides using the same GetHighlightColorFor result, converted to the
		/// separate hue value via Notoriety.GetNameHue. Covers both the real OSI hue values
		/// (0x0059 blue/Innocent, 0x0022 red/Murderer, matching ServUO's own reference Notoriety
		/// hue table) and both possible response packets (0xAE/0xC1 - see
		/// TestGameClient.ReadNameMessageHue).</summary>
		[Fact]
		public void SingleClickNameTextMatchesRealmNotoriety() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountA, "NameHueChecker")) {
				int markGondor = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnRealmNpc G"); // Realm.Gondor
				this.AssertSeenSince("REALM_NPC_SPAWNED:", markGondor, TimeSpan.FromSeconds(10));
				uint gondorUid = ExtractNpcUid(this.fixture.GameOutputSnapshot(), markGondor);

				client.SendSingleClick(gondorUid);
				int gondorHue = client.ReadNameMessageHue(gondorUid, TimeSpan.FromSeconds(10));
				Assert.Equal(0x0059, gondorHue);

				int markMordor = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnRealmNpc M"); // Realm.Mordor
				this.AssertSeenSince("REALM_NPC_SPAWNED:", markMordor, TimeSpan.FromSeconds(10));
				uint mordorUid = ExtractNpcUid(this.fixture.GameOutputSnapshot(), markMordor);

				client.SendSingleClick(mordorUid);
				int mordorHue = client.ReadNameMessageHue(mordorUid, TimeSpan.FromSeconds(10));
				Assert.Equal(0x0022, mordorHue);
			}
		}

		/// <summary>User-requested feature (deliberately not real OSI behavior - see
		/// Notoriety.GetHighlightColor's own comment): a wild monster (AggroMode.Hostile)
		/// renders orange (HighlightColor.Enemy, hue 0x0090), distinct from a Mordor player's
		/// red, on both the health bar and the single-click name text.</summary>
		[Fact]
		public void SingleClickNameTextShowsHostileMonsterAsOrange() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountA, "MonsterColorChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnHostileMonster");
				Assert.True(this.fixture.WaitForGameOutputSince("HOSTILE_MONSTER_SPAWNED:", mark, TimeSpan.FromSeconds(10)));
				uint monsterUid = ExtractNpcUid(this.fixture.GameOutputSnapshot(), mark, "HOSTILE_MONSTER_SPAWNED: uid=");

				byte highlightByte = client.ReadHighlightColor(monsterUid, TimeSpan.FromSeconds(10));
				Assert.Equal((byte) 5, highlightByte); // HighlightColor.Enemy

				client.SendSingleClick(monsterUid);
				int nameHue = client.ReadNameMessageHue(monsterUid, TimeSpan.FromSeconds(10));
				Assert.Equal(0x0090, nameHue);
			}
		}

		/// <summary>Regression for the 2026-10-16 GetHighlightColor redesign - see
		/// NotorietyTests.TestCheckHighlightColorRedesign (script side) for the full scenario
		/// setup. Confirms, in one real server-side check: Mordor stays red even while
		/// criminal-flagged (Mordor is checked before criminal status at all); a Gondor
		/// character DOES still show the ordinary criminal grey; self-view now follows the
		/// exact same logic as viewing anyone else (no more special-cased NoColor); and an
		/// at-war guild member no longer renders the old Attackable/grey, falling through to
		/// their own realm color instead.</summary>
		[Fact]
		public void HighlightColorRedesignMatchesNewPriorityOrder() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountA, "RedesignChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHighlightColorRedesign");
				this.AssertSeenSince("HIGHLIGHT_REDESIGN_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "HIGHLIGHT_REDESIGN_TEST:");

				Assert.Contains("mordorCriminalSeenBySelf=Murderer", line);
				Assert.Contains("gondorCriminalSeenBySelf=Criminal", line);
				Assert.Contains("selfViewingSelf=Innocent", line);
				Assert.Contains("warEnemySeenBySelf=Innocent", line);
			}
		}

		private void AssertSeenSince(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		private static uint ExtractNpcUid(string output, int since, string marker = "REALM_NPC_SPAWNED: uid=") {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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
