using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Generic skill-gain-through-use system (#140/#327/#328) - the native ADVRATE-curve gain
	/// mechanic, its gates (profession cap, NPC exclusion), and the real per-skill curve data.
	/// Safe-region anti-macro exclusion is deliberately NOT covered by an automated test here -
	/// constructing an ad-hoc safe-flagged region at runtime in a shared test fixture was already
	/// flagged as risky elsewhere in this codebase's own V-5 region-enforcement work (#73) and the
	/// Item Decay system's own flag_nodecay gap - same reasoning applies, left unverified on
	/// purpose, not overlooked.
	/// </summary>
	[Collection(SkillGainSystemCollection.Name)]
	public class SkillGainSystemTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltestb";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public SkillGainSystemTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Theory]
		[InlineData("Mining", "10,400,1600")]
		[InlineData("Blacksmith", "10,400,1600")]
		[InlineData("Alchemy", "10,200,750")]
		[InlineData("Skill_Fishing", "10,250,800")]
		public void AdvRateDataMatchesRealEreborValues(string skillDefname, string expected) {
			// SkillName enum values don't all match the .def KEY text 1:1 (e.g. "Blacksmith" is
			// the enum name, "Blacksmithing" is the KEY) - report by the raw skill id instead to
			// avoid that mismatch; easiest is to just drive this for a couple of representative
			// skills via their real enum name directly.
			string enumName = MapToEnumName(skillDefname);
			using (var client = this.ConnectLoginAndCreateCharacter("AdvRateChecker" + enumName)) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportAdvRate \"" + enumName + "\"");
				Assert.True(this.fixture.WaitForGameOutputSince("ADVRATE_REPORT:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ADVRATE_REPORT:");
				Assert.Contains("values=" + expected, line);
			}
		}

		private static string MapToEnumName(string skillDefname) {
			switch (skillDefname) {
				case "Skill_Fishing": return "Fishing";
				default: return skillDefname;
			}
		}

		[Fact]
		public void SkillGainHappensOverManyAttemptsAtLowSkill() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SkillGainSampler")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession \"profession_craftsman\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSkillValue \"Mining\" 50");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGain \"Mining\" 400");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_SAMPLE:");
				Assert.Contains("before=50", line);
				Assert.DoesNotContain("after=50", line); // some gain happened over 400 attempts at a 1-in-11-ish chance
			}
		}

		[Fact]
		public void SkillGainNeverHappensForNpc() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SkillGainNpcChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainOnNpc \"Mining\" 200");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_NPC_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_NPC_SAMPLE:");
				Assert.Contains("gains=0", line);
			}
		}

		[Fact]
		public void SkillGainNeverHappensAtProfessionCap() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SkillGainCapChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession \"profession_craftsman\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainAtCap \"Mining\" 200");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_CAP_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_CAP_SAMPLE:");
				Assert.Contains("gains=0", line);
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
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		/// <summary>AccountB (needs EnsureAutoAccountCreationEnabled called once first, same
		/// pattern as AttrFlagTests.cs/ProfessionTests.cs/SocialTests.cs) - this test class
		/// needs 7 total character creations (4 Theory cases + 3 Facts), over the shared
		/// Account's 5-slot cap, so the 3 sampler Facts get their own account.</summary>
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

		/// <summary>AccountB's characters are never the server's literal first-ever account, so
		/// they stay below Globals.PlevelForLscriptCommands and every chat command they send
		/// routes through SimpleCommandParser - which does not split a quoted-string + numeric
		/// argument pair the way the full LScript parser does. Promoting to GM first (same
		/// SocialTests.TestPromoteToGM helper already used elsewhere) routes subsequent commands
		/// through the full parser so multi-argument Test commands parse correctly.</summary>
		private void PromoteToGm(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestPromoteToGM");
			Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));
		}

		private static bool autoAccountCreationEnabled;

		/// <summary>Flips Globals.AutoAccountCreation on (server-wide, via the already-existing
		/// first account) so AccountB's first-ever login can succeed - same pattern as
		/// AttrFlagTests.cs/ProfessionTests.cs. Guarded by a static flag so repeated calls (one
		/// per AccountB-using Fact) create the "AutoAccountEnabler" character on Account only
		/// once.</summary>
		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
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
	public class SkillGainSystemCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SkillGainSystemPair";
	}
}
