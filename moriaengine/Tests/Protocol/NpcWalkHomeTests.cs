using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #1199 - walking home: straight steps on open ground with no A* search, round a wall with one, out of a
	/// dead-end tube without stepping back in, and with no way home one search per back-off, not one per step.
	/// Own collection: NpcDetourTests leaves a player standing in its wall's gap on the same ground. Server side in
	/// scripts/Tests/NpcWalkHomeTests.cs.
	/// </summary>
	[Collection(NpcWalkHomeCollection.Name)]
	public class NpcWalkHomeTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcWalkHomeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void NpcWalksHomeStraightAndBacksOffWhenThereIsNoWay() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter("Homeward");
				game.SendSpeech(".TestNpcWalkHome");
				Assert.True(this.fixture.WaitForGameOutput("NPC_WALK_HOME_DONE", TimeSpan.FromSeconds(60)), this.fixture.GameOutputSnapshot());
			}
			string output = this.fixture.GameOutputSnapshot();

			(bool reached, int searches) open = Case(output, "open");
			Assert.True(open.reached, output);
			Assert.Equal(0, open.searches); //straight steps only

			(bool reached, int searches) wall = Case(output, "wall");
			Assert.True(wall.reached, output);
			Assert.Equal(1, wall.searches); //one path round it, walked to its end

			(bool reached, int searches) pocket = Case(output, "pocket");
			Assert.True(pocket.reached, output); //the way out, never a straight step back into the tube
			Assert.Equal(1, pocket.searches); //one path, walked to its end past ReplanEvery
			Match west = Regex.Match(output, "NPC_WALK_HOME: case=pocket .* westmostOffset=(-?\\d+)");
			Assert.True(west.Success && (int.Parse(west.Groups[1].Value) <= -10), "the pocket walk never left the tube westward:\n" + output);

			(bool reached, int searches) enclosed = Case(output, "enclosed");
			Assert.False(enclosed.reached, output);
			Assert.Equal(1, enclosed.searches); //20 steps inside one back-off

			(bool reached, int searches) control = Case(output, "enclosedNoBackOff");
			Assert.False(control.reached, output);
			Assert.True(control.searches >= 10, "without the back-off every step should search: " + control.searches);
		}

		private static (bool, int) Case(string output, string name) {
			Match m = Regex.Match(output, "NPC_WALK_HOME: case=" + name + " reached=(True|False) searches=(\\d+)");
			Assert.True(m.Success, "no report for case " + name + ":\n" + output);
			return (m.Groups[1].Value == "True", int.Parse(m.Groups[2].Value));
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
	public class NpcWalkHomeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcWalkHome";
	}
}
