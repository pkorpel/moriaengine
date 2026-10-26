using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Regression for a real user-reported bug: an NPC spellcaster walled in with
	/// i_wall_stone could still see through the walls and cast at/damage a target outside them.
	/// See scripts/Tests/LineOfSightBlockingTests.cs for the full root-cause writeup -
	/// Map.IsLosBlockedAt (GameCore/Regions/LineOfSightImpl.cs) never checked real
	/// dynamically-placed Items, only terrain and pre-baked map statics.</summary>
	[Collection(LineOfSightBlockingCollection.Name)]
	public class LineOfSightBlockingTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public LineOfSightBlockingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void PlacedWallBlocksLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosWallTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				// MovementSpeedTests' TestTeleportToOpenGround finds a tile guaranteed free of
				// any static within 7 tiles (see its own doc comment) - this test's spawn point
				// is real, cluttered map data (same "hemmed in on all sides" finding from V-9/
				// V-10) and a 6-tile straight line from it would otherwise already be
				// LOS-blocked by pre-existing terrain/statics before the wall is even placed.
				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckLosThroughPlacedWall");
				this.AssertSeen("LOS_WALL_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "LOS_WALL_TEST:");

				Assert.Contains("openBeforeWall=True", line);
				Assert.Contains("openAfterWall=False", line);
			}
		}

		/// <summary>Companion to PlacedWallBlocksLineOfSight above, but stacked instead of
		/// offset - see VerticalLosBlockingTests.cs's own doc comment for the exact root cause
		/// this is pinning down.</summary>
		[Fact]
		public void SameColumnFloorBlocksLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosFloorTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckLosThroughSameColumnFloor");
				this.AssertSeen("VERTICAL_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "VERTICAL_LOS_TEST:");

				Assert.Contains("openBeforeFloor=True", line);
				Assert.Contains("openAfterFloor=False", line);
			}
		}

		private void AssertSeen(string marker, int since, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutputSince(marker, since, timeout);
			Assert.True(saw, "Expected to see '" + marker + "' within " + timeout + ". Game output:\n" + this.fixture.GameOutputSnapshot());
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
			int redirectPort = this.WaitForServerListingAndGetRedirectPort();
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(Account, Password);
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

	[CollectionDefinition(LineOfSightBlockingCollection.Name, DisableParallelization = true)]
	public class LineOfSightBlockingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "LineOfSightBlockingPair";
	}
}
