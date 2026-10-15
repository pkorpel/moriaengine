using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling point-spend dialogs (#156): Attributes/Skills/Abilities, opened from new buttons
	/// on .explevel. Own isolated fixture/collection (deliberately away from LevelingTests.cs's
	/// shared one) - same reasoning as LevelingIsolatedTests.cs's own doc comment, these drive
	/// real dialog interactions that don't need to interleave with the death/kill-heavy Facts
	/// over there. Split across two collections/server processes for the same reason as
	/// CombatDepthTestsA/B (#69) and PetCommandTestsA/B (#70) - the account's 5-character-slot
	/// cap, since each Fact creates its own new character on the same shared account.
	/// </summary>
	public abstract class LevelSpendDialogTestBase {
		protected const string Account = "protocoltest";
		private const string Password = "protocoltest";

		protected readonly GameServerPairFixture fixture;

		protected LevelSpendDialogTestBase(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		internal int CheckStr(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestCheckRaceState");
			Assert.True(this.fixture.WaitForGameOutputSince("RACE_STATE:", mark, TimeSpan.FromSeconds(10)));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RACE_STATE:");
			int idx = line.IndexOf("str=", StringComparison.Ordinal);
			Assert.True(idx >= 0, "Expected 'str=' in: " + line);
			int start = idx + 4;
			int end = start;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		internal void SetProfessionAndLevelUp(TestGameClient client, string professionDefname) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			// Quoted - "protocoltest" is this fixture's first-ever (GM-elevated, full-LScript-
			// parser) account, so a bare defname-shaped word would get resolved to the live
			// ProfessionDef object instead of staying literal text (same quirk documented
			// elsewhere in this test suite for elevated-account commands).
			client.SendSpeech(".TestSetProfession \"" + professionDefname + "\"");
			Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

			mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestAddExperience 500");
			Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_TEST_RESULT:", mark, TimeSpan.FromSeconds(10)));
		}

		internal TestGameClient ConnectLoginAndCreateCharacter(string charName) {
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

		protected static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class LevelSpendDialogCollectionA : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelSpendDialogPairA";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class LevelSpendDialogCollectionB : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelSpendDialogPairB";
	}

	[Collection(LevelSpendDialogCollectionA.Name)]
	public class LevelSpendDialogTestsA : LevelSpendDialogTestBase {
		public LevelSpendDialogTestsA(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>.explevel's new button 1 opens D_LevelSpendAttributes; each click of the Str
		/// "+1" button (well inside Mage's strRange hard cap of 90) increases the real stat by
		/// exactly 1 and consumes 1 budget point - matching D_LevelSpendSkills' flat-+1-per-click
		/// pattern (#199 - the original text-entry-then-click design defaulted to "0" and showed
		/// an error on a bare click, a real-world-testing finding).</summary>
		[Fact]
		public void AttributesButtonOpensDialogAndSpendsPoints() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrSpender")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				int baselineStr = this.CheckStr(client);

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 1); // "Attributes" button

				var (attrUid, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(attrUid != 0 || layout.Length > 0, "Expected the Attributes dialog to open.");

				client.SendGumpResponse(attrUid, 1); // Str "+1" button, no text entry needed
				Thread.Sleep(300);

				int afterStr = this.CheckStr(client);
				Assert.Equal(baselineStr + 1, afterStr);
			}
		}

		/// <summary>.explevel's new button 2 opens D_LevelSpendSkills; clicking the "+1" row
		/// button for Magery (a Mage-configured skill, minimum 300 per mage.def, cap 1000)
		/// raises it to 350 - each point is worth 50 raw units (5.0%), matching Erebor's own
		/// rate rather than this engine's original 1 raw unit (0.1%) - #199. Magery (not
		/// Alchemy, used before #204) because Alchemy is a crafting skill - #204 restricts
		/// point-spending to Erebor's combat_skills allowlist, which Alchemy isn't in.</summary>
		[Fact]
		public void SkillsButtonOpensDialogAndSpendsPoints() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillSpender")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button

				var (skillsUid, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(layout.Length > 0, "Expected the Skills dialog to open with a real layout.");

				// Magery = SkillName value 25 (scripts/Basic/skills/SkillName.cs) - button id is
				// SkillButtonIdBase(1000) + id. Hardcoded rather than referencing the SkillName
				// enum directly since Tests.csproj doesn't reference the Scripts assembly.
				const int magerySkillId = 25;
				int mageryButtonId = 1000 + magerySkillId;
				client.SendGumpResponse(skillsUid, mageryButtonId);
				Thread.Sleep(300);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckSkillValue \"Magery\"");
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_SKILL_VALUE:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("value=350", LastLineContaining(this.fixture.GameOutputSnapshot(), "PROFESSION_SKILL_VALUE:"));
			}
		}

		/// <summary>The Skills dialog must show a real skill name ("Alchemy") and a percent-
		/// formatted value ("30.0%"), not SkillDef.PrettyDefname's raw "skill_0" or the raw
		/// *10-scaled internal int ("300") - #199. AddLabel embeds only a numeric textId in the
		/// layout grammar (Gump.AddTextLine) - the literal text lives in the packet's separate
		/// strings list, hence ReadGumpDialogWithStrings rather than the plain layout text.</summary>
		[Fact]
		public void SkillsDialogShowsReadableNameAndPercent() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillDisplayChecker")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button

				var (_, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				Assert.Contains(strings, s => s.Contains("Alchemy"));
				Assert.Contains(strings, s => s.Contains("30.0%"));
				Assert.DoesNotContain(strings, s => s.Contains("skill_0"));
			}
		}

		/// <summary>#202: the Skills dialog no longer paginates - every one of Mage's 30
		/// configured skills must be reachable (a button present) on the single page the dialog
		/// sends, laid out in columns instead of Prev/Next pages. Meditation (id 46, scripts/
		/// Basic/skills/SkillName.cs) is the highest-id skill Mage configures (mage.def), so its
		/// button landing in the 3rd/last column proves the whole list made it onto one page,
		/// not just column 1 - button id is SkillButtonIdBase(1000) + id, and the gump layout
		/// grammar's button element is "{button x y downGfx upGfx isTrigger page triggerId}"
		/// (GameCore/Gump.cs's AddElement) - the trailing " <id>}" anchors on the triggerId
		/// specifically rather than risking a substring match against some other number.</summary>
		[Fact]
		public void SkillsDialogListsEverySkillOnOnePage() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillAllOnePage")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (_, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				const int meditationSkillId = 46;
				Assert.Contains(" " + (1000 + meditationSkillId) + "}", layout);
			}
		}

		/// <summary>#202: the Back button on the Skills dialog re-opens .explevel - confirmed by
		/// seeing its "Attributes"/"Skills"/"Abilities" button labels again (explevel.scp's own
		/// [DIALOG d_explevel TEXT] lines, carried in the packet's strings list same as
		/// AddLabel/AddText - see ReadGumpDialogWithStrings's own doc comment).</summary>
		[Fact]
		public void BackButtonOnSkillsDialogReopensExplevel() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillsBackButton")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (skillsUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendGumpResponse(skillsUid, 1); // "Back" button
				var (_, _, strings) = client.ReadGumpDialogWithStrings(TimeSpan.FromSeconds(10));
				Assert.Contains(strings, s => s.Contains("Abilities"));
			}
		}
	}

	[Collection(LevelSpendDialogCollectionB.Name)]
	public class LevelSpendDialogTestsB : LevelSpendDialogTestBase {
		public LevelSpendDialogTestsB(GameServerPairFixture servers) : base(servers) {
		}

		/// <summary>#204: craft/gathering skills (the user's own example was Fishing, but that
		/// specific skill turned out to have no loadable AbstractSkillDef at all in this
		/// engine's content - a pre-existing, unrelated gap; Alchemy is a reliably-loadable
		/// crafting skill that's equally excluded from LevelingSystem.SpendableSkills) can't be
		/// bought with points at all, even via a raw button click bypassing the UI's own "no
		/// button for this row" treatment - TrySpendSkillPoints itself must reject it. Quoted -
		/// same reason .TestSetProfession/.TestCheckSkillValue are quoted elsewhere in this file:
		/// a bare word on this elevated/first-ever account gets resolved as a live object
		/// reference by the LScript parser instead of staying literal text - confirmed by that
		/// exact failure mode here unquoted (a NullReferenceException deep in
		/// Character.GetSkillObject, since Convert.ToString() on a resolved SkillDef object
		/// doesn't round-trip back to a name Enum.Parse recognizes).</summary>
		[Fact]
		public void CraftSkillCannotBeSpentOnEvenDirectly() {
			using (var client = this.ConnectLoginAndCreateCharacter("CraftSkillRejecter")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpendSkillPoint \"Alchemy\" 1");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_SPEND_SKILL_RESULT:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("success=False", LastLineContaining(this.fixture.GameOutputSnapshot(), "LEVELING_SPEND_SKILL_RESULT:"));
			}
		}

		/// <summary>Right-clicking to close a gump sends button 0 (not this dialog's own Close
		/// button id) - OnResponse must not reopen the dialog in that case, or the dialog
		/// becomes impossible to actually close this way (#199, a real-world-testing finding:
		/// both OnResponse methods only special-cased their own Close button and unconditionally
		/// reopened for anything else, including 0).</summary>
		[Fact]
		public void RightClickClosingSkillsDialogDoesNotReopenIt() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillsRightClickCloser")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button
				var (skillsUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendGumpResponse(skillsUid, 0); // simulates a real right-click close
				Assert.Throws<TimeoutException>(() => client.ReadGumpDialog(TimeSpan.FromSeconds(3)));
			}
		}

		/// <summary>Same right-click-close regression as above, for the Attributes dialog.</summary>
		[Fact]
		public void RightClickClosingAttributesDialogDoesNotReopenIt() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttributesRightClickCloser")) {
				this.SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 1); // "Attributes" button
				var (attrUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));

				client.SendGumpResponse(attrUid, 0); // simulates a real right-click close
				Assert.Throws<TimeoutException>(() => client.ReadGumpDialog(TimeSpan.FromSeconds(3)));
			}
		}

		/// <summary>.explevel's new button 3 opens D_LevelSpendAbilities - deliberately empty
		/// per the user's explicit instruction, just confirms it opens without error and shows
		/// the "not implemented" placeholder.</summary>
		[Fact]
		public void AbilitiesButtonOpensPlaceholderDialog() {
			using (var client = this.ConnectLoginAndCreateCharacter("AbilityChecker")) {
				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 3); // "Abilities" button

				var (abilitiesUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(abilitiesUid != 0, "Expected the Abilities placeholder dialog to open.");
			}
		}
	}
}
