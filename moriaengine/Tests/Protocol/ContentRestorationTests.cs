using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-9 (#77): Phase 17 content-restoration spot-check.
	///
	/// Movement scenario matches the issue as worded: a flight-capable NPC crosses a real water
	/// tile a grounded one can't, exercising the real Map.CheckMovement(this.MovementSettings,
	/// ...) validation via AbstractCharacter.WalkRunOrFly - not a teleport.
	///
	/// Armor scenario ("a high-ArmorVsP/M NPC measurably takes less damage than a zero-armor
	/// one") is deliberately NOT covered here - see issue #96. Real finding while building this:
	/// CombatCalculator.CalculateArmorClassEffect only ever applies the defender's real
	/// ArmorClassVsP/M when the attacker is a monster and the defender is a player - every other
	/// combination (including NPC-vs-NPC, the literal wording here) discards it for a random
	/// placeholder instead. Worse, even in that one surviving combination (monster attacks
	/// player), equipping a full plate set empirically moved measured swing damage by under 1%
	/// across 300 trials, nowhere near the ~20-25% a hand-calculation of the formula predicts -
	/// so something beyond the known isMvP-gating issue is also suppressing armor's effect for
	/// players, not fully root-caused here. Rather than merge a test whose pass/fail would be
	/// asserting a claim this investigation couldn't actually confirm, this is left to #96 for a
	/// deeper, dedicated pass.
	/// </summary>
	[Collection(ContentRestorationCollection.Name)]
	public class ContentRestorationTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public ContentRestorationTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule (Phase 17 content-restoration movement): a flight-capable
		/// creature can cross water a grounded one is blocked by.</summary>
		[Fact]
		public void FlightCapableNpcCrossesWaterGroundedCannot() {
			using (var client = this.ConnectLoginAndCreateCharacter("Scout")) {
				int markFind = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestFindWaterCrossing");
				Assert.True(this.fixture.WaitForGameOutputSince("WATER_CROSSING_FOUND:", markFind, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "WATER_CROSSING_FOUND:");
				Assert.DoesNotContain("none", foundLine);
				int wx = ParseLabelledInt(foundLine, "wx=");
				int wy = ParseLabelledInt(foundLine, "wy=");
				int ax = ParseLabelledInt(foundLine, "ax=");
				int ay = ParseLabelledInt(foundLine, "ay=");
				int dir = ParseLabelledInt(foundLine, "dir=");

				int markFlier = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnMovementTestNpc 1 " + ax + " " + ay);
				Assert.True(this.fixture.WaitForGameOutputSince("MOVE_NPC_SETUP:", markFlier, TimeSpan.FromSeconds(10)));
				uint flierUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markFlier, "MOVE_NPC_SETUP: npc=#");

				int markFlierStep = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestStepNpc " + flierUid + " " + dir);
				Assert.True(this.fixture.WaitForGameOutputSince("STEP_RESULT:", markFlierStep, TimeSpan.FromSeconds(10)));
				string flierResult = LastLineContaining(this.fixture.GameOutputSnapshot(), "STEP_RESULT:");
				Assert.Contains("moved=True", flierResult);
				Assert.Equal(wx, ParseLabelledInt(flierResult, "x="));
				Assert.Equal(wy, ParseLabelledInt(flierResult, "y="));

				int markGrounded = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnMovementTestNpc 0 " + ax + " " + ay);
				Assert.True(this.fixture.WaitForGameOutputSince("MOVE_NPC_SETUP:", markGrounded, TimeSpan.FromSeconds(10)));
				uint groundedUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markGrounded, "MOVE_NPC_SETUP: npc=#");

				int markGroundedStep = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestStepNpc " + groundedUid + " " + dir);
				Assert.True(this.fixture.WaitForGameOutputSince("STEP_RESULT:", markGroundedStep, TimeSpan.FromSeconds(10)));
				string groundedResult = LastLineContaining(this.fixture.GameOutputSnapshot(), "STEP_RESULT:");
				Assert.Contains("moved=False", groundedResult);
				Assert.Equal(ax, ParseLabelledInt(groundedResult, "x="));
				Assert.Equal(ay, ParseLabelledInt(groundedResult, "y="));
			}
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			return ParseLeadingUInt(rest);
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

		private static uint ParseLeadingUInt(string text) {
			int end = 0;
			while (end < text.Length && char.IsDigit(text[end])) {
				end++;
			}
			return uint.Parse(text.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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
	public class ContentRestorationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ContentRestorationPair";
	}
}
