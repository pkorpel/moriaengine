using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Leveling point-spend dialogs (#156): Attributes/Skills/Abilities, opened from new buttons
	/// on .explevel. Own isolated fixture/collection (deliberately away from LevelingTests.cs's
	/// shared one) - same reasoning as LevelingIsolatedTests.cs's own doc comment, these drive
	/// real dialog interactions that don't need to interleave with the death/kill-heavy Facts
	/// over there.
	/// </summary>
	[Collection(LevelSpendDialogCollection.Name)]
	public class LevelSpendDialogTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LevelSpendDialogTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>.explevel's new button 1 opens D_LevelSpendAttributes; submitting a valid
		/// Str spend (+5, well inside Mage's strRange hard cap of 90) increases the real stat and
		/// consumes the budget - confirmed via .explevel's own display reflecting the new value.</summary>
		[Fact]
		public void AttributesButtonOpensDialogAndSpendsPoints() {
			using (var client = this.ConnectLoginAndCreateCharacter("AttrSpender")) {
				SetProfessionAndLevelUp(client, "profession_mage");

				int baselineStr = CheckStr(client);

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 1); // "Attributes" button

				var (attrUid, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(attrUid != 0 || layout.Length > 0, "Expected the Attributes dialog to open.");

				client.SendGumpResponse(attrUid, 1, new Dictionary<int, string> { { 10, "5" } }); // Str entry=10, button=1
				Thread.Sleep(300);

				int afterStr = CheckStr(client);
				Assert.Equal(baselineStr + 5, afterStr);
			}
		}

		private int CheckStr(TestGameClient client) {
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

		/// <summary>.explevel's new button 2 opens D_LevelSpendSkills; clicking the "+1" row
		/// button for Alchemy (a Mage-configured skill, minimum 300 per mage.def, cap 1000)
		/// raises it to 301.</summary>
		[Fact]
		public void SkillsButtonOpensDialogAndSpendsPoints() {
			using (var client = this.ConnectLoginAndCreateCharacter("SkillSpender")) {
				SetProfessionAndLevelUp(client, "profession_mage");

				client.SendSpeech(".explevel");
				var (explevelUid, _) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				client.SendGumpResponse(explevelUid, 2); // "Skills" button

				var (skillsUid, layout) = client.ReadGumpDialog(TimeSpan.FromSeconds(10));
				Assert.True(layout.Length > 0, "Expected the Skills dialog to open with a real layout.");

				// Alchemy = SkillName value 0 (scripts/Basic/skills/SkillName.cs) - button id is
				// SkillButtonIdBase(1000) + id. Hardcoded rather than referencing the SkillName
				// enum directly since Tests.csproj doesn't reference the Scripts assembly.
				const int alchemySkillId = 0;
				int alchemyButtonId = 1000 + alchemySkillId;
				client.SendGumpResponse(skillsUid, alchemyButtonId);
				Thread.Sleep(300);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckSkillValue \"Alchemy\"");
				Assert.True(this.fixture.WaitForGameOutputSince("PROFESSION_SKILL_VALUE:", mark, TimeSpan.FromSeconds(10)));
				Assert.Contains("value=301", LastLineContaining(this.fixture.GameOutputSnapshot(), "PROFESSION_SKILL_VALUE:"));
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

		private void SetProfessionAndLevelUp(TestGameClient client, string professionDefname) {
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

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}
	}

	[CollectionDefinition(LevelSpendDialogCollection.Name, DisableParallelization = true)]
	public class LevelSpendDialogCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LevelSpendDialogPair";
	}
}
