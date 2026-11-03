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

		/// <summary>#1100 - Erebor's e_tele_to_target: a guard runs 8 steps a second at a player it fights, goes home
		/// from beyond 70 tiles of its spawn, teleports home from beyond 7 once the fight is over, and drops a town's NPC.</summary>
		[Fact]
		public void TheyRunAtTheirTargetAndGoHome() {
			using var client = CommandPermissionTests.Connect("guardsight", "Pursuit Tester");

			string run = this.Ask(client, ".TestPursuitRun", "PURSUIT_RUN:");
			Assert.True(run.Contains("guard=True pursuing=True"), run);
			int before = Number(run, "before="), afterRun = Number(run, "afterRun=");
			Assert.True((before >= 9) && (afterRun == before - 4), run); //capped at 4 for the test; the walking AI alone takes one step
			Assert.Contains("afterSecond=1 cap=8", run); //then the rest at once, up to its side

			string far = this.Ask(client, ".TestPursuitFarFromSpawn", "PURSUIT_FAR:");
			Assert.True(Number(far, "fromSpawn=") > 70, far);
			Assert.Contains("wentHome=True", far);
			System.Threading.Thread.Sleep(2500); //the pursuit's next second
			Assert.Contains("ended=True nearStays=True farGoes=True", this.Ask(client, ".TestPursuitHome", "PURSUIT_HOME:"));
			Assert.Contains("townGem=True wildFought=True dropped=True", this.Ask(client, ".TestPursuitTownNpc", "PURSUIT_TOWN:"));
		}

		private static int Number(string text, string key) {
			int at = text.IndexOf(key) + key.Length;
			int end = at;
			while ((end < text.Length) && char.IsDigit(text[end])) {
				end++;
			}
			return int.Parse(text.Substring(at, end - at));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GuardSightCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GuardSight";
	}
}
