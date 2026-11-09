using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1331 - Erebor's .presun through a GM client: a torch out of the GM's pack to a spot on the ground, a hare onto a
	/// rock inside a chest (to the chest's place), and the ground as the first target, which asks again before the bones go to a
	/// second spot. The pack and the chest are opened first: the server takes a target only on what the client can see.
	/// Server side: scripts/Tests/PresunTests.cs.</summary>
	[Collection(PresunCollection.Name)]
	public class PresunTests : LivingWorldTestBase {
		private const int Pause = 500;

		public PresunTests(GameServerPairFixture servers)
			: base(servers, "presuntests", "PRESUN") {
		}

		private static void Presun(TestGameClient game, uint what, System.Action where) {
			game.SendSpeech(".presun");
			Thread.Sleep(Pause);
			game.SendTargetUid(what);
			Thread.Sleep(Pause);
			where();
			Thread.Sleep(Pause);
		}

		[Fact]
		public void AGmMovesCharactersAndItems() {
			using (TestGameClient game = this.Login("Stehovak")) {
				this.Run(game, ".TestPresunSetup", "PRESUN_SETUP:");
				string setup = this.LastLine("PRESUN_SETUP:");
				game.SendDoubleClick((uint) Number(setup, "pack")); //a GM targets what they see: things in a container once it's open
				game.SendDoubleClick((uint) Number(setup, "chest"));
				Thread.Sleep(Pause);

				Presun(game, (uint) Number(setup, "torch"), () => game.SendTargetGround(Number(setup, "sx"), Number(setup, "sy"), Number(setup, "sz")));
				Presun(game, (uint) Number(setup, "hare"), () => game.SendTargetUid((uint) Number(setup, "rock")));

				game.SendSpeech(".presun");
				Thread.Sleep(Pause);
				game.SendTargetGround(Number(setup, "sx"), Number(setup, "sy"), Number(setup, "sz")); //nothing to move there: asked again
				Thread.Sleep(Pause);
				game.SendTargetUid((uint) Number(setup, "bones"));
				Thread.Sleep(Pause);
				game.SendTargetGround(Number(setup, "tx"), Number(setup, "ty"), Number(setup, "tz"));
				Thread.Sleep(Pause);

				this.Run(game, ".TestPresunState", "PRESUN_DONE");
				this.AssertChecks("anItemLeavesThePack", "aCreatureGoesToAThingsPlace", "theGroundFirstAsksAgain");
			}
		}
	}

	[CollectionDefinition(Name)]
	public class PresunCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Presun";
	}
}
