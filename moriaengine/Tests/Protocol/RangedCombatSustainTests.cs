using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #709 - proves a real c_titan_ranger (equipped bow, 50-75 arrows) sustains a real fight
	/// against c_titan without its combat sequence getting stuck - see
	/// scripts/Tests/RangedCombatSustainTests.cs's own doc comment for the full root-cause
	/// writeup (a stacking-merge-deletes-the-incoming-item bug in WeaponSkillDef.On_Stroke's
	/// "arrow appears in target's backpack" branch, exposed by #667 making arrows stackable).
	/// </summary>
	[Collection(RangedCombatSustainCollection.Name)]
	public class RangedCombatSustainTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public RangedCombatSustainTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RangerKeepsAttackingAcrossManyShotsWithoutGettingStuck() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(Account, Password);
				game.ReadCharacterList();
				game.SendCreateCharacter("RangedSustainChecker");

				//the default character-creation spawn point is a deliberately narrow/boxed-in
				//antechamber (see #210) - real open ground is needed for a ranged NPC to stand
				//off at range from its target instead of being stuck unable to move at all.
				game.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutput("OPEN_GROUND_FOUND:", TimeSpan.FromSeconds(15)));

				game.SendSpeech(".TestRangerSustainedAttack");
				Assert.True(this.fixture.WaitForGameOutput("RANGED_SUSTAIN_TEST: STARTED", TimeSpan.FromSeconds(15)));

				Assert.True(this.fixture.WaitForGameOutput("check=4", TimeSpan.FromSeconds(60)));

				string output = this.fixture.GameOutputSnapshot();
				Assert.DoesNotContain("deleted object (arrows", output);

				string lastCheck = LastLineContaining(output, "RANGED_SUSTAIN_TEST: check=4");
				Assert.Contains("rangerDead=False", lastCheck);
				Assert.Contains("titanDead=False", lastCheck);
				Assert.Contains("engaging=True", lastCheck);
				Assert.DoesNotContain("currentSkill=none", lastCheck);
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
	}

	[CollectionDefinition(RangedCombatSustainCollection.Name, DisableParallelization = true)]
	public class RangedCombatSustainCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RangedCombatSustainPair";
	}
}
