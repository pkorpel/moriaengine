using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1022 - whom a guard attacks when it sees them.</summary>
	[Collection(GuardSightCollection.Name)]
	public class GuardSightTests {
		private readonly GameServerPairFixture fixture;

		public GuardSightTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheyAttackWhomTheyDontProtect() {
			using var client = CommandPermissionTests.Connect("guardsight", "Sight Tester");

			//a Gondor guard: its own criminals yes, no realm no, a criminal of no realm no
			Assert.Contains("realm=Gondor", this.Ask(client, ".TestSightReady \"c_vguard_rohan_hala_jezdec\"", "SIGHT_READY:"));
			string gondor = this.Ask(client, ".TestSightReport", "SIGHT_REPORT:");
			Assert.Contains("Neutral=False NeutralCrim=False", gondor);
			Assert.Contains("Gondor=False GondorCrim=True", gondor);
			Assert.Contains("Mordor=True MordorCrim=True", gondor);
			Assert.Contains("attacked=False", this.Ask(client, ".TestSightGm", "SIGHT_GM:"));
			Assert.Contains("dead=True", this.Ask(client, ".TestSightSummon", "SIGHT_SUMMON:"));

			//a Mordor guard leaves Mordor's criminals alone
			Assert.Contains("realm=Mordor", this.Ask(client, ".TestSightReady \"c_vguard_mordor_hala_jezdec\"", "SIGHT_READY:"));
			string mordor = this.Ask(client, ".TestSightReport", "SIGHT_REPORT:");
			Assert.Contains("Mordor=False MordorCrim=False", mordor);
			Assert.Contains("Gondor=True", mordor);

			//an ent minds your axe, not your realm
			this.Ask(client, ".TestSightReady \"C_m_GUARD_ent1\"", "SIGHT_READY:");
			Assert.Contains("without=False with=True", this.Ask(client, ".TestSightAxe", "SIGHT_AXE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GuardSightCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GuardSight";
	}
}
