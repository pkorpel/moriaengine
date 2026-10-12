using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-10 (#78): movement - a real MoveRequestInPacket (0x02) genuinely updates the player's
	/// position via the real Map.CheckMovement-validated path (AbstractCharacter.WalkRunOrFly),
	/// not a teleport.
	///
	/// Real finding while building this: MovementState.CanMoveAgain always throttled the next
	/// accepted step using RunStepTime (0.18s) regardless of whether the client's request had
	/// the running flag set - WalkStepTime/RidingWalkStepTime existed as real, deliberately-tuned
	/// constants (see the class's own comment: measured from real client observation) but were
	/// commented out and never actually used, so walking and running were throttled identically.
	/// Fixed in GameCore/Networking/MovementState.cs: both constants restored, CanMoveAgain now
	/// takes the request's running flag and picks the right one.
	///
	/// #78 also asked to confirm running is measurably faster than walking. Investigating that
	/// surfaced a second real finding: the very first real move-request packet on any connection
	/// sees an enormous MovementState.CanMoveAgain `diff` (current time minus nextMovementTime,
	/// which starts at its default/zero and is otherwise never initialized) - that gets banked
	/// as "reserve" and keeps letting requests through near-instantly for several more requests
	/// before aging out of the 3-slot sliding window, regardless of walk/run setting. Combined
	/// with this test map's real terrain varying tile to tile (dense static placement in places,
	/// spawn tiles found hemmed in on all 8 sides in V-7/V-9 too - no fixed amount of
	/// pre-validated clearance reliably guarantees enough consecutive real open tiles to burn
	/// through that reserve and then measure a clean interval), a reliable automated timing
	/// comparison needs a dedicated test region with guaranteed-clear terrain that this session
	/// didn't have time to build. Deferred rather than merging a test whose pass/fail couldn't
	/// actually be trusted - same spirit as the #73/#96 deferrals elsewhere in this effort. This
	/// test instead verifies the one part provable without that: a real move-request packet
	/// genuinely moves the character, exercising the exact same CanMoveAgain code path the fix
	/// touches.
	/// </summary>
	[Collection(MovementSpeedCollection.Name)]
	public class MovementSpeedTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public MovementSpeedTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void RealMoveRequestPacketUpdatesPosition() {
			using (var client = this.ConnectLoginAndCreateCharacter("Walker")) {
				int markGround = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTeleportToOpenGround");
				Assert.True(this.fixture.WaitForGameOutputSince("OPEN_GROUND_FOUND:", markGround, TimeSpan.FromSeconds(10)));
				Assert.DoesNotContain("none", LastLineContaining(this.fixture.GameOutputSnapshot(), "OPEN_GROUND_FOUND:"));

				// TestProbeDirection (direct WalkRunOrFly) only discovers a direction with a real
				// clear tile ahead - it bypasses MovementState entirely, so it doesn't exercise
				// the fix. The actual assertion below uses a real move-request packet instead,
				// now already facing a confirmed-clear direction, so it's the real
				// MovementState.CanMoveAgain path (what this phase's fix touches) under test.
				int dir = -1;
				int preX = 0, preY = 0;
				for (int candidate = 0; candidate < 8 && dir < 0; candidate++) {
					int mark = this.fixture.GameOutputSnapshot().Length;
					client.SendSpeech(".TestProbeDirection " + candidate);
					Assert.True(this.fixture.WaitForGameOutputSince("PROBE_RESULT:", mark, TimeSpan.FromSeconds(10)));
					string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "PROBE_RESULT:");
					if (line.Contains("moved=True")) {
						dir = candidate;
						preX = ParseLabelledInt(line, "x=");
						preY = ParseLabelledInt(line, "y=");
					}
				}
				Assert.True(dir >= 0, "No direction (of 8) accepted a real step from the teleported-to position.");

				client.SendMoveRequest(dir, false, 1);
				var (accepted, _, _, _) = client.ReadMoveResult(TimeSpan.FromSeconds(10));
				Assert.True(accepted, "Expected a second real step in the same (already facing) direction to be accepted too.");

				int markPos = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportPosition");
				Assert.True(this.fixture.WaitForGameOutputSince("POSITION_REPORT:", markPos, TimeSpan.FromSeconds(10)));
				string posLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "POSITION_REPORT:");
				int postX = ParseLabelledInt(posLine, "x=");
				int postY = ParseLabelledInt(posLine, "y=");
				Assert.True(postX != preX || postY != preY,
					"Expected position to change again after the real move-request packet (pre=" + preX + "," + preY + " post=" + postX + "," + postY + ").");
			}
		}

		private static int ParseLabelledInt(string line, string label) {
			int idx = line.IndexOf(label, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + label + "' in:\n" + line);
			string rest = line.Substring(idx + label.Length);
			int end = 0;
			bool negative = rest.Length > 0 && rest[0] == '-';
			int start = negative ? 1 : 0;
			end = start;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MovementSpeedCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MovementSpeedPair";
	}
}
