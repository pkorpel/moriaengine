using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1228 item 9 - .sethome through a GM client: first the NPC, then its new home. A spot on the ground; the NPC itself,
	/// after a move left its old home behind (where it stands now); a chest (its place) after the ground as the first target was
	/// asked again. Server side: scripts/Tests/NpcSetHomeTests.cs.</summary>
	[Collection(NpcSetHomeCollection.Name)]
	public class NpcSetHomeTests : LivingWorldTestBase {
		private const int Pause = 500;

		public NpcSetHomeTests(GameServerPairFixture servers)
			: base(servers, "sethometests", "SETHOME") {
		}

		private static void SetHome(TestGameClient game, uint npc, System.Action where) {
			game.SendSpeech(".sethome");
			Thread.Sleep(Pause);
			game.SendTargetUid(npc);
			Thread.Sleep(Pause);
			where();
			Thread.Sleep(Pause);
		}

		[Fact]
		public void AGmSetsAnNpcsHome() {
			using (TestGameClient game = this.Login("Domovnik")) {
				this.Run(game, ".TestSetHomeSetup", "SETHOME_SETUP:");
				string setup = this.LastLine("SETHOME_SETUP:");
				uint npc = (uint) Number(setup, "npc");
				int sx = Number(setup, "sx"), sy = Number(setup, "sy"), sz = Number(setup, "sz");

				SetHome(game, npc, () => game.SendTargetGround(sx, sy, sz));
				this.Run(game, ".TestSetHomeReport \"ground " + sx + " " + sy + " " + sz + "\"", "SETHOME_DONE ground");

				this.Run(game, ".TestSetHomeMoveNpc", "SETHOME_MOVED:");
				Assert.Contains("homeKept=True", this.LastLine("SETHOME_MOVED:")); //#1206: a move alone keeps the old home
				SetHome(game, npc, () => game.SendTargetUid(npc));
				this.Run(game, ".TestSetHomeReport \"npc\"", "SETHOME_DONE npc");

				game.SendSpeech(".sethome");
				Thread.Sleep(Pause);
				game.SendTargetGround(sx, sy, sz); //no NPC there: asked again
				Thread.Sleep(Pause);
				game.SendTargetUid(npc);
				Thread.Sleep(Pause);
				game.SendTargetUid((uint) Number(setup, "chest"));
				Thread.Sleep(Pause);
				this.Run(game, ".TestSetHomeReport \"chest\"", "SETHOME_DONE chest");

				this.AssertChecks("ground", "npc", "chest");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class NpcSetHomeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcSetHome";
	}
}
