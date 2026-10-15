using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling tests that need full isolation from LevelingTests.cs's other Facts - deliberately
	/// their own fully isolated GameServerPairFixture/collection. Real finding while building
	/// this: sharing a server process with several Facts that cause real player/NPC deaths
	/// (Phases 2/3) left both ExpLevelDialogOpensWithoutError (couldn't receive the explevel
	/// gump) and SpendingPointsRequiresAProfession (ProfessionDef.GetByDefname's own lookup
	/// started returning null for a defname confirmed correct and working standalone) failing
	/// deterministically - same result every time once the other Facts had run first, not a
	/// transient timing issue (generous timeouts and fresh-connection retries didn't help
	/// either). Both pass reliably completely on their own. The exact mechanism of the
	/// interference was never fully root-caused for either - isolating is the clean, principled
	/// fix (these Facts don't depend on anything the other file's Facts do), not a workaround to
	/// revisit later.
	///
	/// ExpLevelDialogOpensWithoutError's own real bug - explevel.scp's body calling
	/// InvalidateCombatWeaponValues()/InvalidateCombatArmorValues() bare, missing from
	/// CommandsAuthorization.cs's allowlist and so silently denied for every non-GM account
	/// (#146, fixed there) - is unrelated to and already fixed independently of this isolation.
	/// </summary>
	[Collection(LevelingIsolatedCollection.Name)]
	public class LevelingIsolatedTests {
		private const string Account = "protocoltest";
		// Real bug found while debugging SpendingPointsRequiresAProfession: sharing Account with
		// ExpLevelDialogOpensWithoutError (a SECOND character on the same account) made
		// ProfessionDef.GetByDefname return null for a defname confirmed correct and working
		// when this test runs completely alone - never root-caused exactly why (something about
		// a second character's profession-plugin installation on an account that already has
		// one connected character), but a dedicated account for this test eliminates it
		// entirely, same principle as LevelingTests.cs's own AccountB/AccountC/account-sharing
		// lessons.
		private const string AccountB = "protocoltest2";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LevelingIsolatedTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ExpLevelDialogOpensWithoutError() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelDialogChecker")) {
				client.SendSpeech(".explevel");
				// If Construct() threw (e.g. a bad <Token> in the TEXT section, or an LScript
				// syntax error at load time), no gump packet would ever arrive at all and this
				// would time out/throw - that's the real signal here, not a log-string match.
				var (gumpUid, layoutText) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(gumpUid != 0 || layoutText.Length > 0, "Expected a real gump dialog to open for .explevel.");
			}
		}

		/// <summary>Phase 4 (#129): the user's own stated design - a player with no profession
		/// assigned can't spend EITHER skill or attribute points (Skill.Cap already returns 0
		/// with no profession; attribute spending needs its own explicit check, since there's no
		/// existing "no profession = cap 0" mechanism for attributes the way there is for
		/// skills). Once a profession IS assigned, both become spendable (not bounded by a real
		/// attribute RANGE yet - that's #135, not implemented - just the overall budget). Uses
		/// the same single Account as ExpLevelDialogOpensWithoutError above - this isolated
		/// fixture only ever has these two Facts, no account-slot-cap or
		/// AutoAccountCreation-ordering concerns like LevelingTests.cs's shared fixture has.</summary>
		[Fact]
		public void SpendingPointsRequiresAProfession() {
			using (var enablerClient = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnablerE")) {
				int enableMark = this.fixture.GameOutputSnapshot().Length;
				enablerClient.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST: ENABLED", enableMark, TimeSpan.FromSeconds(10)));
			}

			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "PointSpender")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 2000"); // level 0->3, 15 skill points, 6 ability points
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendSkillPoint Alchemy 10");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string noProfSkillLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:");
				Assert.Contains("success=False", noProfSkillLine); // no profession -> cap 0

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendAttributePoint Str 5");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_ATTRIBUTE_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string noProfAttrLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_ATTRIBUTE_RESULT:");
				Assert.Contains("success=False", noProfAttrLine); // no profession -> can't spend at all

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_mage");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("profession=profession_mage",
					LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_PROFESSION_SET:"));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendSkillPoint Alchemy 10");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string profSkillLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:");
				// mage's Alchemy cap is 1000 (0-1000 scale) - 10 points easily fits. Starts at
				// 300, not 0 - ProfessionPlugin.On_Assign (#135) grants mage.def's Alchemy
				// minimum (300) immediately on profession assignment above, before any points
				// are even spent.
				Assert.Contains("success=True", profSkillLine);
				Assert.Contains("unspentSkillPoints=5", profSkillLine); // 15 - 10
				// 300 (granted) + 10 * LevelingSystem.SkillPointGain (50, i.e. 5.0% per point,
				// matching Erebor's own rate - #199) = 800.
				Assert.Contains("skillValue=800", profSkillLine);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendAttributePoint Str 5");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_ATTRIBUTE_RESULT:", mark, TimeSpan.FromSeconds(10)),
					"Output since mark:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string profAttrLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_ATTRIBUTE_RESULT:");
				Assert.Contains("success=True", profAttrLine);
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
	public class LevelingIsolatedCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelingIsolatedPair";
	}
}
