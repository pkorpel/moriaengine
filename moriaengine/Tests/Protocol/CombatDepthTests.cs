using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Shared plumbing for the V-1 (#69) combat-depth scenario classes below. Split across two
	/// collections/fixtures (CombatDepthCollectionA/B) rather than one, each with its own fresh
	/// server process and "protocoltest" account: AbstractAccount.maxCharactersPerGameAccount is
	/// a hard 5, and this phase alone needs 7 distinct test characters - one shared account would
	/// run out of slots partway through (and steamengine.ini's autoAccountCreation=False means a
	/// second account name can't just be made up to work around that on a shared fixture).
	/// </summary>
	public abstract class CombatDepthTestBase {
		protected const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		protected CombatDepthTestBase(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		protected void RunScenario(string charName, string command, string marker, TimeSpan timeout) {
			bool saw;
			using (var game = this.ConnectLoginAndCreateCharacter(charName)) {
				game.SendSpeech(command);
				saw = this.fixture.WaitForGameOutput(marker, timeout);
			}
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
		}

		/// <summary>#1504 - two players at once: the second on its own account, created on the way (TestEnableAutoAccountCreation
		/// first), as one account plays one character at a time.</summary>
		private protected void RunWithTwoPlayers(string charName, string secondAccount, string secondCharName, Action<TestGameClient, TestGameClient> scenario) {
			using (var game = this.ConnectLoginAndCreateCharacter(charName)) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				game.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST: ENABLED", mark, TimeSpan.FromSeconds(10)));
				using (var second = this.ConnectLoginAndCreateCharacter(secondCharName, secondAccount)) {
					scenario(game, second);
				}
			}
		}

		protected GameServerPairFixture Fixture {
			get {
				return this.fixture;
			}
		}

		/// <summary>
		/// Logging in and out in rapid succession across several test methods on one account:
		/// AbstractAccount.SetLoggedOut() only runs from the connection's own On_Close handler,
		/// which fires asynchronously relative to the previous test's `using` block disposing its
		/// socket - back-to-back tests can genuinely race that server-side cleanup, getting either
		/// a flat "connection closed" (0 bytes) or a real LoginDeniedOutPacket (0x82,
		/// AlreadyOnline) instead of the character list. Retrying absorbs that race.
		/// </summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName, string account = TestAccount) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, (account == TestAccount) ? TestPassword : account);
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
						login.SendLogin(account, (account == TestAccount) ? TestPassword : account);

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
	public class CombatDepthCollectionA : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CombatDepthPairA";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class CombatDepthCollectionB : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CombatDepthPairB";
	}

	[Collection(CombatDepthCollectionA.Name)]
	public class CombatDepthTestsA : CombatDepthTestBase {
		public CombatDepthTestsA(GameServerPairFixture servers) : base(servers) {
		}

		[Fact]
		public void HostileMonsterAttacksPlayerWithoutBeingOrdered() {
			this.RunScenario("HostileAggroer", ".TestHostileAggro", "via autonomous aggro", TimeSpan.FromSeconds(65));
		}

		[Fact]
		public void CalmAnimalNeverAttacksPlayer() {
			this.RunScenario("CalmAggroer", ".TestCalmNoAggro", "CALM_AGGRO_TEST: PASS", TimeSpan.FromSeconds(65));
		}

		[Fact]
		public void NpcSpellcasterLandsDamageOnItsTarget() {
			this.RunScenario("Spellcaster", ".TestSpellcasterDamage", "SPELLCASTER_TEST: PASS", TimeSpan.FromSeconds(65));
		}

		[Fact]
		public void ArcherKeepsItsDistanceInsteadOfRushingToMelee() {
			this.RunScenario("ArcherWatcher", ".TestArcherStandsOff", "ARCHER_STANDOFF_TEST: PASS", TimeSpan.FromSeconds(65));
		}

		[Fact]
		public void SpellcasterKeepsItsDistanceInsteadOfRushingToMelee() {
			this.RunScenario("SpellcasterWatcher", ".TestSpellcasterStandsOff", "SPELLCASTER_STANDOFF_TEST: PASS", TimeSpan.FromSeconds(65));
		}
	}

	[Collection(CombatDepthCollectionB.Name)]
	public class CombatDepthTestsB : CombatDepthTestBase {
		public CombatDepthTestsB(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>#1504 - #58's question asked for real: a dragon's breath (its cone of fire) burns the player it faces and
		/// a player beside them in the cone. Server side: CombatDepthTests.TestDragonBreathCone.</summary>
		[Fact]
		public void DragonBreathBurnsEveryPlayerInItsCone() {
			this.RunWithTwoPlayers("DragonWatcher", "breathbeside", "BreathBeside", (engaged, beside) => {
				int mark = this.Fixture.GameOutputSnapshot().Length;
				beside.SendSpeech(".TestReportMyUid");
				Assert.True(this.Fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				string output = this.Fixture.GameOutputSnapshot();
				int at = output.IndexOf("MY_UID: ", mark, StringComparison.Ordinal) + "MY_UID: ".Length;
				string uid = output.Substring(at, output.IndexOf(' ', at) - at);
				mark = this.Fixture.GameOutputSnapshot().Length;
				engaged.SendSpeech(".TestDragonBreathCone " + uid);
				Assert.True(this.Fixture.WaitForGameOutputSince("DRAGON_CONE: STARTED", mark, TimeSpan.FromSeconds(10)), this.Fixture.GameOutputSnapshot());
				Thread.Sleep(3000); //the breath is 1 s after the inhale
				mark = this.Fixture.GameOutputSnapshot().Length;
				engaged.SendSpeech(".TestDragonBreathConeReport");
				Assert.True(this.Fixture.WaitForGameOutputSince("DRAGON_CONE: RESULT", mark, TimeSpan.FromSeconds(10)));
				output = this.Fixture.GameOutputSnapshot();
				string line = output.Substring(output.LastIndexOf("DRAGON_CONE: RESULT", StringComparison.Ordinal));
				line = line.Substring(0, line.IndexOf('\n') < 0 ? line.Length : line.IndexOf('\n'));
				int lostEngaged = int.Parse(Regex.Match(line, @"lostEngaged=(-?\d+)").Groups[1].Value);
				int lostBeside = int.Parse(Regex.Match(line, @"lostBeside=(-?\d+)").Groups[1].Value);
				Assert.True(line.Contains("besideInCone=True") && (lostEngaged > 0) && (lostBeside > 0), "Both players in the cone should burn: " + line);
			});
		}

		[Fact]
		public void LowHealthMonsterFleesInsteadOfFighting() {
			this.RunScenario("FleeWatcher", ".TestLowHealthFlee", "NPCAiTimer.Flee", TimeSpan.FromSeconds(65));
		}

		[Fact]
		public void GuardsRealmMonsterAggroesOnlyOppositeRealm() {
			this.RunScenario("GuardWatcher", ".TestGuardsRealmAggro", "via autonomous aggro", TimeSpan.FromSeconds(65));
		}
	}
}
