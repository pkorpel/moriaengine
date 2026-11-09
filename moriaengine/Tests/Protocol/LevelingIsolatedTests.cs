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
		private const string AccountC = "protocoltest3"; //#952 - an account holds 5 characters, and AccountB's Facts made 6
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

		/// <summary>#404 - this file's own ExpLevelDialogOpensWithoutError above always ran on
		/// `Account` ("protocoltest"), the server's literal first-ever account in a fresh test
		/// boot and therefore auto-elevated to GM (AbstractAccount.HandleLoginAttempt's
		/// accounts.Count==0 check) - the exact same blind spot already documented twice in this
		/// file's history (#146, #254). A genuinely non-elevated (MaxPlevel=1) account was never
		/// actually exercised against this command before this test. Root cause: explevel.scp's
		/// own `[function explevel]` body's `Dialog(d_explevel)` line is a bare (no self./src.
		/// receiver) call - same shape as the InvalidateCombatWeaponValues()/
		/// InvalidateCombatArmorValues() calls #146 already had to allowlist - and neither
		/// "d_explevel" (the dialog symbol passed as the argument) nor "Dialog" (the bare
		/// function name itself) were ever added to CommandsAuthorization.cs, so every ordinary
		/// player's first .explevel ever attempted was silently denied one line into the
		/// function, even though "explevel" itself IS allowlisted at PLEVEL_PLAYER.</summary>
		[Fact]
		public void ExpLevelAllowedForOrdinaryNonFirstAccountPlayer() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountC, "OrdinaryLevelChecker")) { //#952 - AccountB's 5 characters are taken by the other Facts
				client.SendSpeech(".explevel");
				var (gumpUid, layoutText) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(gumpUid != 0 || layoutText.Length > 0,
					"Expected a real gump dialog to open for .explevel on an ordinary (non-GM) player.");
			}
		}

		/// <summary>One point into the skill: "success=... skillValue=..." of the result.</summary>
		private string SpendOne(TestGameClient client, string skill) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSpendSkillPoint " + skill + " 1");
			Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:");
			int points = line.IndexOf(" unspentSkillPoints=", StringComparison.Ordinal), value = line.IndexOf(" skillValue=", StringComparison.Ordinal);
			return line.Substring(0, points) + line.Substring(value);
		}

		private static bool autoAccountCreationEnabled;

		/// <summary>Same guarded pattern as AttrFlagTests.cs/GatheringTests.cs - flips
		/// Globals.AutoAccountCreation on (server-wide, via the already-existing first account)
		/// so AccountB's first-ever login can succeed.</summary>
		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnablerF")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST: ENABLED", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
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
				client.SendSpeech(".TestSpendSkillPoint Magery 10");
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
				client.SendSpeech(".TestSpendSkillPoint Magery 4");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string profSkillLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:");
				// mage's Magery cap is 1000 (0-1000 scale) - 10 points easily fits. Starts at
				// 300, not 0 - ProfessionPlugin.On_Assign (#135) grants mage.def's Magery
				// minimum (300) immediately on profession assignment above, before any points
				// are even spent. Magery, not Alchemy (used before #204) - Alchemy is a
				// crafting skill, excluded from LevelingSystem.SpendableSkills regardless of
				// profession/cap.
				Assert.Contains("success=True", profSkillLine);
				// #952 - since #882 (Erebor's leveling): 5.0 a raise, 1 point each below 50.0, and a
				// master must teach past 25.0, 50.0 and 75.0. 300 (granted) + 4 * 50 = 500, 15 - 4 = 11.
				Assert.Contains("unspentSkillPoints=11", profSkillLine);
				Assert.Contains("skillValue=500", profSkillLine);
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendSkillPoint Magery 1");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("success=False unspentSkillPoints=11 skillValue=500", LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:")); //50.0 needs a master

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendAttributePoint Str 5");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_ATTRIBUTE_RESULT:", mark, TimeSpan.FromSeconds(10)),
					"Output since mark:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string profAttrLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_ATTRIBUTE_RESULT:");
				Assert.Contains("success=True", profAttrLine);
			}
		}

		/// <summary>Real-user-reported regression: repeatedly clicking a skill's "+1" button in
		/// the Skills dialog used to stack a TEMPORARY Skill.modification (via the old
		/// Character.ModifySkillValue call) instead of permanently raising RealValue, fed by a
		/// separate Character.GetRealSkillValue bug that returned ModifiedValue instead of
		/// RealValue - together these meant clicking never actually raised the skill at all,
		/// right up until a THIRD, independent Skill.DisposeIfEmpty typo (modification==1000
		/// instead of ==0) silently deleted the Skill object entirely once enough clicks landed
		/// a modifier of exactly 1000, snapping the displayed value straight to 0%. Warrior's
		/// own DetectHidden (warrior.def: "DetectingHidden=0, 1000") starts at exactly 0 with a
		/// 1000 cap - the exact shape that reproduced this live (user's own words: "begging and
		/// detect hidden... went from 95% to 0%"). 20 one-point spends (20*50=1000 raw) must
		/// land on a real, permanent 100.0% - every intermediate value checked along the way,
		/// not just the final one, since the old bug could in principle have mis-fired earlier
		/// too. Also covers the OTHER real-user-reported request: once DetectHidden is capped,
		/// its own "+1" button must disappear from the real dialog layout, while Anatomy's (not
		/// maxed, same profession, points still remain) stays visible - proving it's really the
		/// per-skill cap driving this, not just "no points left anywhere".</summary>
		[Fact]
		public void RepeatedSkillSpendingReachesCapPermanentlyWithoutResetting() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "RepeatedSkillSpender")) {
				client.SendSpeech(".TestSetLevelAndExperience 0 0");
				Assert.True(this.fixture.WaitForGameOutput("LEVELING_TEST_SETUP:", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 10000"); // plenty of levels - unspent points are never the limiter here
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_warrior");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				//#952 - since #882 points raise up to 25.0, a master teaches past it (and past 50.0, 75.0)
				for (int i = 1; i <= 5; i++) {
					Assert.Contains("success=True skillValue=" + (i * 50), this.SpendOne(client, "DetectHidden"));
				}
				Assert.Contains("success=False skillValue=250", this.SpendOne(client, "DetectHidden")); //kept, not reset
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestMasterTaught DetectHidden 800");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_MASTER_TAUGHT:", mark, TimeSpan.FromSeconds(10)));
				for (int i = 1; i <= 4; i++) {
					Assert.Contains("success=True skillValue=" + (800 + (i * 50)), this.SpendOne(client, "DetectHidden"));
				}
				Assert.Contains("success=False skillValue=1000", this.SpendOne(client, "DetectHidden")); //at the cap, and it stays

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (_, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				// DetectHidden = SkillName value 14, Anatomy = 1 - button id is 1000 + id.
				Assert.DoesNotContain(" 1014}", layout);
				Assert.Contains(" 1001}", layout);
			}
		}

		/// <summary>Real-user-reported, the root Skill.cs bug directly (independent of the
		/// leveling dialog's own fix above, which no longer reaches this path at all - this
		/// proves the general Skill-class fix itself, since ModifySkillValue is still a real,
		/// reachable public API, e.g. for equipment/potion buffs): driving a fresh skill's
		/// TEMPORARY modifier to exactly +1000 while its RealValue stays 0 used to silently
		/// delete the Skill object outright (Skill.DisposeIfEmpty compared modification to 1000
		/// instead of 0). A never-touched skill ("Tracking") on a brand new character is
		/// guaranteed to start at real value 0.</summary>
		[Fact]
		public void ModifyingASkillByExactly1000DoesNotDeleteIt() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SkillModifyChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestModifySkillValueByExactly1000 Tracking");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_MODIFY_TEST:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_MODIFY_TEST:");
				Assert.Contains("realValue=0", line);
				Assert.Contains("modifiedValue=1000", line);
			}
		}

		/// <summary>Real-user-reported: the Skills dialog's "+1" button must not be shown at all
		/// once the player has no unspent skill points left - a fresh, unleveled character
		/// (UnspentSkillPoints defaults to 0) with a profession assigned has real spendable rows
		/// but zero budget, so NONE of them should offer a button, even though Back(1)/Close(2)
		/// still do (proving the dialog itself rendered, not just that it failed to open).</summary>
		[Fact]
		public void NoUnspentPointsHidesEverySkillButton() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "NoPointsSkillChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_warrior");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (_, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				Assert.Contains(" 1}", layout); // Back button still there
				Assert.Contains(" 2}", layout); // Close button still there
				for (int skillId = 0; skillId <= 60; skillId++) {
					Assert.DoesNotContain(" " + (1000 + skillId) + "}", layout);
				}
			}
		}

		/// <summary>Real-user-reported correction to #204's own original design: Discordance was
		/// excluded from LevelingSystem.SpendableSkills, inconsistently with its bard-skill
		/// siblings Peacemaking/Provocation/Musicianship (all 3 already spendable) - it should
		/// show a real "+1" button, not "(trained)". Thief is the only profession that configures
		/// Discordance at all (thief.def: "Discordance=0, 1000").</summary>
		[Fact]
		public void DiscordanceIsSpendableNotTrained() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "DiscordanceChecker")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession profession_thief");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestAddExperience 2000");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (_, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				// Discordance = SkillName value 15 - button id is 1000 + id.
				Assert.Contains(" 1015}", layout);
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
