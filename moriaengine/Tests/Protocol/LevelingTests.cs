using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling system Phases 1-3 (#126-#128): the level * coefficient threshold formula (with
	/// overflow carry across multiple levels in one gain), monster-kill experience grants, and
	/// death's experience-loss/pay-the-killer mechanic (including the post-death immunity
	/// window). The .explevel dialog itself is covered separately in ExpLevelDialogTests.cs -
	/// real finding while building this file: it needs full isolation in its own
	/// GameServerPairFixture/collection, not shared with these Facts (see that file's own doc
	/// comment for why).
	/// </summary>
	[Collection(LevelingCollection.Name)]
	public class LevelingTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltest2";
		// Separate account for the Phase 3 tests' victim characters - AbstractAccount caps each
		// account at maxCharactersPerGameAccount (5), and Account above already creates 4 other
		// characters across this file's other Facts, so a 5th+6th here would intermittently fail
		// to create (silently closes the connection - see CreateCharacterInPacket.Handle's
		// HasFreeSlot check) depending on xUnit's test execution order. Real bug found while
		// debugging exactly this: 3 of 6 Facts failed with 10s timeouts whenever they happened to
		// run after the account's 5 slots were already used up by other Facts.
		private const string AccountC = "protocoltest3";
		// Dedicated account for LevelUpSendsMessagesAndPlaysEffect - Account above was already at
		// its 5-slot cap (LevelOneUpper/LevelMultiUpper/LevelNpcKiller/TitanExperienceChecker/
		// LevelPointsGrantee), and a 6th character creation under it intermittently fails the
		// same way the AccountC doc comment above describes.
		private const string AccountD = "protocoltest4";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LevelingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SingleLevelUpCarriesOverflowExperience() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelOneUpper")) {
				client.SendSpeech(".TestSetLevelAndExperience 1 600");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 100");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_TEST_RESULT:");
				// level 1, threshold to level 2 is 660 - 600+100=700 crosses it, 40 carries over.
				Assert.Contains("level=2", line);
				Assert.Contains("experience=40", line);
			}
		}

		/// <summary>Requested directly: players should see a system message both for gaining
		/// experience and for leveling up, plus a celebratory effect/sound on the level-up
		/// itself. All three happen inside LevelingSystem.AddExperience's existing loop, so this
		/// covers every call site (monster kills, the killer's cut of a PK death) uniformly.</summary>
		[Fact]
		public void LevelUpSendsMessagesAndPlaysEffect() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountD, "LevelCelebrant")) {
				client.SendSpeech(".TestSetLevelAndExperience 1 600");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 100");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));

				// Same cascade as SingleLevelUpCarriesOverflowExperience: level 1/600 exp + 100 ->
				// level 2/40 exp - exactly one gain message, then one level-up message/effect/sound.
				string gainMessage = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				Assert.Equal("You have gained 100 experience.", gainMessage);

				string levelUpMessage = client.ReadSystemMessage(TimeSpan.FromSeconds(10));
				Assert.Equal("You have advanced to level 2!", levelUpMessage);

				// If the effect/sound calls never fired, these would time out - that's the real
				// signal here, the exact payload content is already covered by the fixed-size
				// registration matching the real packets' own Write() methods.
				client.ReadPacketPayload(0xC0, TimeSpan.FromSeconds(10)); // GraphicalEffectOutPacket
				client.ReadPacketPayload(0x54, TimeSpan.FromSeconds(10)); // PlaySoundEffectOutPacket
			}
		}

		[Fact]
		public void OneLargeGainCascadesThroughMultipleLevels() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelMultiUpper")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_TEST_RESULT:");
				// level 0->1 (330, 1670 left) ->2 (660, 1010 left) ->3 (990, 20 left) -> stop, 20 < 1320.
				Assert.Contains("level=3", line);
				Assert.Contains("experience=20", line);
			}
		}

		[Fact]
		public void KillingNpcGrantsExperienceToKiller() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelNpcKiller")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestKillNpcGrantsExperience 50 50");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_KILL_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_KILL_TEST_RESULT:");
				Assert.Contains("level=0", line);
				Assert.Contains("experience=50", line);

				// Leaves a corpse behind - clean it up now, while still connected near it, rather
				// than letting it accumulate for whichever connection happens to spawn nearby next
				// (see ExpLevelDialogOpensWithoutError's own real finding on why that matters).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCleanupNearbyDebris");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_CLEANUP:", mark, TimeSpan.FromSeconds(10)));
			}
		}

		/// <summary>Requested directly for the user's own testing: the three titan variants
		/// (ogres.def) now have real .def-content-authored experience values, anchored to the
		/// Erebor reference archive's own c_titan (tag.experience={110.0 120.0}, confirmed by an
		/// exact WeaponAttack=80 stat match) and scaled proportionally for the mage/lord variants
		/// using moriaengine's own relative WeaponAttack/ArmorVsP/M stats (no exact 1:1 name
		/// match exists in the reference for those two - Erebor names its variants differently,
		/// e.g. c_titan_overlord/c_titan_archmage). This is also the first real test coverage of
		/// the "experience min max" LoadScriptLine parsing path from actual .def text - every
		/// other leveling test sets ExperienceWorth programmatically in C#.</summary>
		[Fact]
		public void TitanVariantsHaveExperienceValuesAuthored() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "TitanExperienceChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportTitanExperience");
				Assert.True(this.fixture.WaitForGameOutputSince("TITAN_EXPERIENCE: defname=c_titan_lord", mark, TimeSpan.FromSeconds(10)));
				string output = this.fixture.GameOutputSnapshot().Substring(mark);
				Assert.Contains("TITAN_EXPERIENCE: defname=c_titan min=110 max=120", output);
				Assert.Contains("TITAN_EXPERIENCE: defname=c_titan_mage min=140 max=150", output);
				Assert.Contains("TITAN_EXPERIENCE: defname=c_titan_lord min=180 max=195", output);
			}
		}

		/// <summary>Phase 3 (#128): the default-settings case - victim loses 100% of experience
		/// (full reset), killer gets 10% of what was LOST (not of the victim's original total).
		/// Needs two real, simultaneously-connected characters (same pattern as SocialTests.cs,
		/// including its real finding that a second real account needs
		/// Globals.AutoAccountCreation flipped on first) since CauseDeath's killer attribution is
		/// a real Character reference, not something a single connection can fake.</summary>
		[Fact]
		public void DeathCausesExperienceLossAndPaysTheKiller() {
			using (var victimClient = this.ConnectLoginAndCreateCharacter(AccountC, "DeathLossVictim")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				victimClient.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST: ENABLED", mark, TimeSpan.FromSeconds(10)));

				using (var killerClient = this.ConnectLoginAndCreateCharacter(AccountB, "DeathLossKiller")) {
					mark = this.fixture.GameOutputSnapshot().Length;
					victimClient.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
					uint victimUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark);

					mark = this.fixture.GameOutputSnapshot().Length;
					victimClient.SendSpeech(".TestSetLevelAndExperience 1 1000");
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestSetLevelAndExperience 0 0");
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestKillPlayerAndReportResult " + victimUid);
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_DEATH_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_DEATH_TEST_RESULT:");
					// #902 - level 1 loses (1 + 1) * 330 / 20 = 33, all of it to the one who fought them.
					Assert.Contains("victimLevel=1", line);
					Assert.Contains("victimExperience=967", line);
					Assert.Contains("killerLevel=0", line);
					Assert.Contains("killerExperience=33", line);

					// Leaves a corpse behind - clean it up now (see KillingNpcGrantsExperienceToKiller's
					// identical comment for why).
					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestCleanupNearbyDebris");
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_CLEANUP:", mark, TimeSpan.FromSeconds(10)));
				}
			}
		}

		/// <summary>Phase 3 (#128): mirrors Erebor's i_checkstats_timer - within 5 minutes of a
		/// previous death, a second death loses NO further experience (and so pays the killer
		/// nothing either), even though the victim has plenty of experience to lose at the time
		/// of the second death.</summary>
		[Fact]
		public void PostDeathImmunityPreventsASecondLossWithinFiveMinutes() {
			using (var victimClient = this.ConnectLoginAndCreateCharacter(AccountC, "ImmunityVictim")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				victimClient.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST: ENABLED", mark, TimeSpan.FromSeconds(10)));

				using (var killerClient = this.ConnectLoginAndCreateCharacter(AccountB, "ImmunityKiller")) {
					mark = this.fixture.GameOutputSnapshot().Length;
					victimClient.SendSpeech(".TestReportMyUid");
					Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
					uint victimUid = ExtractUid(this.fixture.GameOutputSnapshot(), mark);

					mark = this.fixture.GameOutputSnapshot().Length;
					victimClient.SendSpeech(".TestSetLevelAndExperience 1 1000");
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestSetLevelAndExperience 0 0");
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestKillPlayerAndReportResult " + victimUid);
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_DEATH_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
					// first death: victim 1000 -> 967, killer 0 -> 33. Sets the victim's immunity timer.

					mark = this.fixture.GameOutputSnapshot().Length;
					victimClient.SendSpeech(".TestSetLevelAndExperience 5 2000"); // victim has plenty to lose again
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

					mark = this.fixture.GameOutputSnapshot().Length;
					killerClient.SendSpeech(".TestKillPlayerAndReportResult " + victimUid);
					Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_DEATH_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_DEATH_TEST_RESULT:");
					// within 5 minutes of the first death - immunity suppresses the loss entirely, killer gets nothing more.
					Assert.Contains("victimLevel=5", line);
					Assert.Contains("victimExperience=2000", line);
					Assert.Contains("killerLevel=0", line);
					Assert.Contains("killerExperience=33", line);
				}
			}
		}

		/// <summary>Requested directly: Level/Experience should show up in the .info dialog
		/// "alongside other attributes". Real finding while scoping this: a working ".info"
		/// chat command already existed (Dialogs.D_Info.Info, scripts/Basic/administration/
		/// InfoDialog/D_Info.cs) - an earlier pass missed it entirely (same "grep missed a
		/// whole folder" mistake flagged elsewhere this session) and mistook the unrelated,
		/// fully-commented-out legacy OldInfo in Commands.cs for the only candidate, nearly
		/// shipping a duplicate SteamFunction named "Info" that collided with the real one at
		/// the generated-code level (CS0101 in Generated/CompiledScriptHolders.Generated.cs).
		/// No production code needed to change at all: D_Info's own ViewableClassGenerator
		/// already auto-discovers every public property on a [ViewableClass]-marked type via
		/// reflection with no per-property opt-in needed (see ViewableClassAttributes.cs's own
		/// doc comment, "all other attributes will be displayed") - Player already carries
		/// [ViewableClass], so Level/Experience/the point-pool properties already show up here
		/// with zero wiring. TestPromoteToGM sidesteps this file's established "which account
		/// happened to be first" plevel fragility, since .info is GM-only (unlisted in
		/// CommandsAuthorization, so it falls to the default-deny-below-plevel-4
		/// fallback).</summary>
		[Fact]
		public void InfoCommandShowsLevelAndExperience() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "InfoChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestPromoteToGM");
				Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetLevelAndExperience 5 123");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_SETUP:", mark, TimeSpan.FromSeconds(10)));

				// .info already exists (Dialogs.D_Info.Info, scripts/Basic/administration/
				// InfoDialog/D_Info.cs) - with no arguments it opens the caller's own info
				// dialog immediately, no target cursor involved. Player.Level/Experience are
				// plain public properties on a [ViewableClass]-tagged type, so they show up
				// here automatically via ViewableClassGenerator - no new command was needed,
				// just confirming the existing one surfaces them.
				client.SendSpeech(".Info");
				// If Construct() threw, no gump packet would ever arrive at all - that's the real
				// signal here (same reasoning as ExpLevelDialogOpensWithoutError), not a
				// field-by-field decode of the dialog's text-strings list.
				var (gumpUid, layoutText) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(gumpUid != 0 || layoutText.Length > 0, "Expected a real gump dialog to open for .info.");
			}
		}

		/// <summary>Phase 4 (#129): skill points (+5/level) and ability points (+2/level) are flat
		/// per-level grants, added to the running UnspentSkillPoints/UnspentAbilityPoints pools
		/// on every level-up inside LevelingSystem.AddExperience's loop.</summary>
		[Fact]
		public void LevelUpGrantsSkillAndAbilityPoints() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "LevelPointsGrantee")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_TEST_RESULT:");
				// level 0->3 (same cascade as OneLargeGainCascadesThroughMultipleLevels) = 3 level-ups.
				Assert.Contains("level=3", line);
				Assert.Contains("unspentSkillPoints=15", line); // 3 * 5
				Assert.Contains("unspentAbilityPoints=6", line); // 3 * 2
			}
		}

		/// <summary>Phase 4 (#129): LevelingSystem.LevelStatBonus's tiered formula at its own
		/// boundaries - verified against Erebor's f_levelstatbonus. This formula takes a level as
		/// a plain parameter (not self's own level), so a single connection is enough.</summary>
		[Fact]
		public void AttributePointBudgetFollowsLevelTieredFormula() {
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "StatBonusChecker")) {
				AssertLevelStatBonus(client, 0, 0);
				AssertLevelStatBonus(client, 9, 36); // 9 * 4
				AssertLevelStatBonus(client, 10, 40); // 40 + (10-10)*3 - continuous with the level-9 tier
				AssertLevelStatBonus(client, 29, 97); // 40 + (29-10)*3
				AssertLevelStatBonus(client, 30, 100); // 100 + (30-30)*2 - continuous with the level-29 tier
				AssertLevelStatBonus(client, 39, 118); // 100 + (39-30)*2
				AssertLevelStatBonus(client, 40, 120); // flat from here on
				AssertLevelStatBonus(client, 60, 120);
			}
		}

		private void AssertLevelStatBonus(TestGameClient client, int level, int expectedBonus) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestLevelStatBonus " + level);
			Assert.True(this.fixture.WaitForGameOutputSince("LEVEL_STAT_BONUS:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVEL_STAT_BONUS:");
			Assert.Equal("LEVEL_STAT_BONUS: " + expectedBonus, line);
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
	public class LevelingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelingPair";
	}
}
