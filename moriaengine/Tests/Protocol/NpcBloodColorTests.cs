using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// New GitHub issue (#583, "NPCDef needs a BloodColor field") - proves NPCDef.BloodColor
	/// (scripts/Basic/CharClasses/NPC.ct's Defs: section) correctly resolves BOTH real archive
	/// shapes over the real client protocol: a leading-zero hex-style number (BLOODCOLOR=020)
	/// and a symbolic color constant already defined in scripts/Basic/defs.scp
	/// (BLOODCOLOR=colors_green, which itself expands to a Sphere {1401 1447} random range -
	/// a hand-written int.Parse on the raw string would have thrown on this case, which is
	/// exactly why BloodColor is a generic Defs: field instead of a hand-parsed one).
	/// </summary>
	[Collection(NpcBloodColorCollection.Name)]
	public class NpcBloodColorTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcBloodColorTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void HexAndSymbolicColorConstantsBothResolve() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "BloodColorChecker")) {
				client.SendSpeech(".TestReportBloodColors");
				Assert.True(this.fixture.WaitForGameOutput("NPC_BLOOD_COLOR_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_BLOOD_COLOR_TEST_RESULT:");

				// "020" is Sphere-hex (leading zero) - 0x20 = 32 decimal.
				Assert.Contains("hex=32", line);

				// colors_green expands to the real archive's own {1401 1447} range.
				int symbolicValue = ParseIntField(line, "symbolic=");
				Assert.InRange(symbolicValue, 1401, 1447);
			}
		}

		private static int ParseIntField(string line, string prefix) {
			int start = line.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length;
			int end = start;
			while (end < line.Length && (char.IsDigit(line[end]) || line[end] == '-')) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start));
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
		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
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

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

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

	[CollectionDefinition(NpcBloodColorCollection.Name, DisableParallelization = true)]
	public class NpcBloodColorCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcBloodColorPair";
	}
}
