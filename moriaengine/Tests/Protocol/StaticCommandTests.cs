using System;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Coverage for the new GM ".static" command (#697) -
	/// scripts/Basic/CharClasses/Player.cs's Static(...) overloads. Confirmed against the real
	/// Sphere 0.56b engine source (necr0potenc3/Sphere) that ".static" is identical to ".add" in
	/// every respect except the created item gets attr_move_never set.</summary>
	[Collection(StaticCommandCollection.Name)]
	public class StaticCommandTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public StaticCommandTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>".static i_rock_plain" placed on the ground must create an item that is
		/// both Flag_NonMovable AND exempt from the per-item decay timer (#697 - the decay
		/// exemption is the real bug this issue also found: Flag_NonMovable previously had no
		/// bearing on decay eligibility at all).</summary>
		[Fact]
		public void StaticCommandCreatesNonMovableNonDecayingItem() {
			using (var client = this.ConnectLoginAndCreateCharacter("GmStaticTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);
				client.SendSpeech(".TestCleanupNearbyRocks");
				Thread.Sleep(300);

				client.SendSpeech(".TestReportPositionXYZ");
				this.AssertSeen("STATIC_POSITION:", TimeSpan.FromSeconds(10));
				string posLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "STATIC_POSITION:");
				int x = ExtractIntAfter(posLine, "x=");
				int y = ExtractIntAfter(posLine, "y=");
				int z = ExtractIntAfter(posLine, "z=");

				client.SendSpeech(".static i_rock_plain");
				Thread.Sleep(300);
				client.SendTargetGround(x + 1, y, z);
				Thread.Sleep(500);

				client.SendSpeech(".TestCheckNearestRockAttrs");
				this.AssertSeen("STATIC_TEST: nonMovable=True hasDecayTimer=False", TimeSpan.FromSeconds(10));
			}
		}

		/// <summary>No-regression companion: a plain ".add"-created item of the same type must
		/// still be movable and still get a real decay timer - the #697 decay-exemption fix is
		/// scoped to Flag_NonMovable items only, not a blanket change.</summary>
		[Fact]
		public void AddCommandStillCreatesMovableDecayingItem() {
			using (var client = this.ConnectLoginAndCreateCharacter("GmAddTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);
				client.SendSpeech(".TestCleanupNearbyRocks");
				Thread.Sleep(300);

				client.SendSpeech(".TestReportPositionXYZ");
				this.AssertSeen("STATIC_POSITION:", TimeSpan.FromSeconds(10));
				string posLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "STATIC_POSITION:");
				int x = ExtractIntAfter(posLine, "x=");
				int y = ExtractIntAfter(posLine, "y=");
				int z = ExtractIntAfter(posLine, "z=");

				client.SendSpeech(".add i_rock_plain");
				Thread.Sleep(300);
				client.SendTargetGround(x + 1, y, z);
				Thread.Sleep(500);

				client.SendSpeech(".TestCheckNearestRockAttrs");
				this.AssertSeen("STATIC_TEST: nonMovable=False hasDecayTimer=True", TimeSpan.FromSeconds(10));
			}
		}

		private void AssertSeen(string marker, TimeSpan timeout) {
			bool saw = this.fixture.WaitForGameOutput(marker, timeout);
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

		private static int ExtractIntAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + line);
			string rest = line.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return int.Parse(rest.Substring(0, end), CultureInfo.InvariantCulture);
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

	[CollectionDefinition(StaticCommandCollection.Name, DisableParallelization = true)]
	public class StaticCommandCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "StaticCommandPair";
	}
}
