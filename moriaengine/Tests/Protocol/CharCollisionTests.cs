using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #767 - walking onto a tile another character stands on: players shove only at full
	/// stamina (cost log2(STR)+1, @personalSpace fires), hidden obstacles are revealed, invisible
	/// and insubstantial ones never block, NPCs never shove, AStar routes NPCs around characters.
	/// The whole scenario runs server-side in scripts/Tests/CharCollisionTests.cs through the real
	/// AbstractCharacter.WalkRunOrFly; this side just checks every reported line.
	/// </summary>
	[Collection(CharCollisionCollection.Name)]
	public class CharCollisionTests {
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";
		private const string TestCharName = "Shover";

		private static readonly string[] Checks = {
			"weakPlayerBlocked",
			"fullStamPlayerShoves",
			"shoveCostsLog2StrPlusOne",
			"personalSpaceFired",
			"hiddenObstacleShovedAndRevealed",
			"invisibleObstacleIgnored",
			"npcBlockedByNpc",
			"insubstObstacleIgnored",
			"npcSettingOffLetsNpcThrough",
			"astarRoutesAroundCharacter",
			"flatPushCostSetting",
		};

		private readonly GameServerPairFixture fixture;

		public CharCollisionTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void CharactersBlockMovementWithSphereShoveRules() {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();

			bool sawDone;
			using (var game = new TestGameClient("127.0.0.1", redirectPort)) {
				game.SendLogin(TestAccount, TestPassword);
				game.ReadCharacterList();
				game.SendCreateCharacter(TestCharName);
				game.SendSpeech(".TestCharCollision");
				sawDone = this.fixture.WaitForGameOutput("CHAR_COLLISION_DONE", TimeSpan.FromSeconds(30));
			}

			string output = this.fixture.GameOutputSnapshot();
			Assert.True(sawDone, "Expected the collision scenario to finish. Game output:\n" + output);
			foreach (string check in Checks) {
				string marker = "CHAR_COLLISION: " + check + "=";
				int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
				Assert.True(idx >= 0, "Check '" + check + "' never reported. Game output:\n" + output);
				Assert.True(string.CompareOrdinal(output, idx + marker.Length, "True", 0, 4) == 0,
					"Check '" + check + "' failed. Game output:\n" + output);
			}
		}

		private int WaitForServerListingAndGetRedirectPort() {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);

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
	public class CharCollisionCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "CharCollision";
	}
}
