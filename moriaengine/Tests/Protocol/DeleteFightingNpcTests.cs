using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #786 - deleting an NPC in the middle of a swing used to overflow GameCore's stack (exit 134):
	/// its WeaponSkillTargetQueuePlugin's teardown aborted the skill, which touched the half-deleted
	/// NPC, whose ThrowIfDeleted saw "limbo" and restarted the delete, forever. This is what killed
	/// the server in CombatDepthTestsA/MagerySpellTests whenever a test's cleanup deleted a
	/// still-fighting NPC.
	/// </summary>
	[Collection(DeleteFightingNpcCollection.Name)]
	public class DeleteFightingNpcTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public DeleteFightingNpcTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void DeletingAnNpcMidSwingDoesNotCrashTheServer() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("NpcDeleter");
				game.SendSpeech(".TestDeleteFightingNpc");
				Assert.True(this.fixture.WaitForGameOutput("DELETE_FIGHTING_NPC: deleted=", TimeSpan.FromSeconds(15)),
					"Never finished deleting the fighting NPC. Game output:\n" + this.fixture.GameOutputSnapshot());
			}
			string output = this.fixture.GameOutputSnapshot();
			Assert.Contains("DELETE_FIGHTING_NPC: swinging=True", output); //the scenario really had a swing in flight
			Assert.Contains("DELETE_FIGHTING_NPC: deleted=True", output);
			Thread.Sleep(1000);
			Assert.False(this.fixture.GameProcessHasExited, "GameCore died (code " + this.fixture.GameProcessExitCode + "). Game output:\n" + output);
			Assert.DoesNotContain("Stack overflow", output);
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						if (login.ReadServersList() < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}
						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
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
	public class DeleteFightingNpcCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "DeleteFightingNpc";
	}
}
