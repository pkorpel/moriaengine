using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#708 - see VerticalLosBlockingTests.cs's own doc comment for the full root-cause
	/// writeup. Own dedicated collection/account (not sharing LineOfSightBlockingTests.cs's) -
	/// adding these 3 Facts to that file's existing shared account/collection hit the same
	/// documented reconnect-race flakiness class other multi-Fact files in this codebase have
	/// already worked around the same way.</summary>
	[Collection(RoofLosCollection.Name)]
	public class RoofLosTests {
		private const string Account = "rooflostest";
		private const string Password = "rooflostest";

		private readonly GameServerPairFixture fixture;

		public RoofLosTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RoofDoesNotBlockGroundLevelLineOfSightAtRange() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosRoofTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRoofDoesNotBlockGroundLevelLosAtRange");
				this.AssertSeen("ROOF_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ROOF_LOS_TEST:");

				Assert.Contains("openBeforeRoof=True", line);
				Assert.Contains("openAfterRoof=True", line);
			}
		}

		/// <summary>#708 follow-up (multi-floor buildings) - the roof fix must not also unblock
		/// a genuine cross-floor sightline straight through the floor/ceiling separator.</summary>
		[Fact]
		public void FloorBetweenLevelsStillBlocksCrossFloorLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosCrossFloorTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestFloorBetweenLevelsStillBlocksCrossFloorLos");
				this.AssertSeen("CROSS_FLOOR_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "CROSS_FLOOR_LOS_TEST:");

				Assert.Contains("openBeforeFloor=True", line);
				Assert.Contains("openAfterFloor=False", line);
			}
		}

		/// <summary>#708 follow-up (multi-floor buildings) - two characters standing on the SAME
		/// upper floor, with that very floor's own roof-flagged tile on their shared sightline,
		/// must still see each other at range exactly like two characters on open ground do.</summary>
		[Fact]
		public void RoofOverSameFloorDoesNotBlockLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosSameFloorTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRoofOverSameFloorDoesNotBlockLos");
				this.AssertSeen("SAME_FLOOR_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SAME_FLOOR_LOS_TEST:");

				Assert.Contains("openOnSameFloor=True", line);
			}
		}

		/// <summary>#721 - see VerticalLosBlockingTests.cs's own doc comment for the full
		/// root-cause writeup. Reproduces the user's real-map symptom with a synthetic flat
		/// floor (same model, same flags, same zero height as the real one found at the
		/// reported coordinates).</summary>
		[Fact]
		public void FlatSurfaceFloorDoesNotBlockGroundLevelLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosFlatFloorTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestFlatSurfaceFloorDoesNotBlockGroundLevelLos");
				this.AssertSeen("FLAT_FLOOR_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "FLAT_FLOOR_LOS_TEST:");

				Assert.Contains("openBeforeFloor=True", line);
				Assert.Contains("openAfterFloor=True", line);
			}
		}

		/// <summary>#721 follow-up - see VerticalLosBlockingTests.cs's own doc comment for the
		/// full root-cause writeup (a second real location, different flooring, same symptom:
		/// a raised floor/platform overhead had its blocking band wrongly extended all the way
		/// down to the ground below it).</summary>
		[Fact]
		public void RaisedSurfaceFloorDoesNotBlockGroundLevelLineOfSight() {
			using (var client = this.ConnectLoginAndCreateCharacter("LosRaisedFloorTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				client.SendSpeech(".TestTeleportToOpenGround");
				this.AssertSeen("OPEN_GROUND_FOUND:", 0, TimeSpan.FromSeconds(10));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestRaisedSurfaceFloorDoesNotBlockGroundLevelLos");
				this.AssertSeen("RAISED_FLOOR_LOS_TEST:", mark, TimeSpan.FromSeconds(10));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "RAISED_FLOOR_LOS_TEST:");

				Assert.Contains("openBeforeFloor=True", line);
				Assert.Contains("openAfterFloor=True", line);
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

	[CollectionDefinition(RoofLosCollection.Name, DisableParallelization = true)]
	public class RoofLosCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RoofLosPair";
	}
}
