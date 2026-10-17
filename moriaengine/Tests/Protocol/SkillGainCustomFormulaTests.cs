using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Lockpicking/Taming's own custom-formula skill-gain path (#140/#327/#329) - the 2 of #140's
	/// 13 skills whose real Erebor .def disables the native ADVRATE curve in favor of a flat
	/// per-skill advrate constant and a quadratic chance formula. Mirrors SkillGainSystemTests.cs's
	/// own structure/gates for the native-curve path (#328).
	/// </summary>
	[Collection(SkillGainCustomFormulaCollection.Name)]
	public class SkillGainCustomFormulaTests {
		private const string Account = "protocoltest";
		private const string AccountB = "protocoltestb";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public SkillGainCustomFormulaTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void LockpickingGainHappensOverManyAttemptsAtLowSkill() {
			using (var client = this.ConnectLoginAndCreateCharacter("CustomGainLockpickSampler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession \"profession_thief\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetSkillValue \"Lockpicking\" 200");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_VALUE_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainCustomFormula \"Lockpicking\" 600 400");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_CUSTOM_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_CUSTOM_SAMPLE:");
				Assert.Contains("before=200", line);
				Assert.DoesNotContain("after=200", line); // some gain happened over 400 attempts at a 1-in-25-ish chance
			}
		}

		[Fact]
		public void TamingGainHappensOverManyAttemptsAtGrantedStartingSkill() {
			using (var client = this.ConnectLoginAndCreateCharacter("CustomGainTamingSampler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession \"profession_ranger\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				// Ranger's own AnimalTaming minimum (300) is granted automatically on assignment -
				// no need to TestSetSkillValue first, unlike Lockpicking (Thief's minimum is 0).
				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainCustomFormula \"AnimalTaming\" 600 400");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_CUSTOM_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_CUSTOM_SAMPLE:");
				Assert.Contains("before=300", line);
				Assert.DoesNotContain("after=300", line); // some gain happened over 400 attempts at a 1-in-55-ish chance
			}
		}

		[Fact]
		public void CustomFormulaGainNeverHappensForNpc() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "CustomGainNpcChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainCustomFormulaOnNpc \"Lockpicking\" 600 200");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_CUSTOM_NPC_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_CUSTOM_NPC_SAMPLE:");
				Assert.Contains("gains=0", line);
			}
		}

		[Fact]
		public void CustomFormulaGainNeverHappensAtProfessionCap() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "CustomGainCapChecker")) {
				this.PromoteToGm(client);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetProfession \"profession_thief\"");
				Assert.True(this.fixture.WaitForGameOutputSince("LEVELING_PROFESSION_SET:", mark, TimeSpan.FromSeconds(10)));

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSampleSkillGainCustomFormulaAtCap \"Lockpicking\" 600 200");
				Assert.True(this.fixture.WaitForGameOutputSince("SKILL_GAIN_CUSTOM_CAP_SAMPLE:", mark, TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SKILL_GAIN_CUSTOM_CAP_SAMPLE:");
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

		private static bool autoAccountCreationEnabled;

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

		/// <summary>AccountB's characters are never the server's literal first-ever account, so
		/// they stay below Globals.PlevelForLscriptCommands and every chat command routes
		/// through SimpleCommandParser (no real argument splitting) - multi-argument Test
		/// commands need the full LScript parser instead, same lesson as SkillGainSystemTests.cs.</summary>
		private void PromoteToGm(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestPromoteToGM");
			Assert.True(this.fixture.WaitForGameOutputSince("PROMOTE_TO_GM:", mark, TimeSpan.FromSeconds(10)));
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
	public class SkillGainCustomFormulaCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SkillGainCustomFormulaPair";
	}
}
