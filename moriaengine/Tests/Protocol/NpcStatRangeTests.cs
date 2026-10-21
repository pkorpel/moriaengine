using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #559 - proves NPCDef.StrRange/DexRange/IntRange (scripts/Basic/CharClasses/NPC.cs)
	/// actually apply on NPC.On_Create, over the real client protocol (not just a unit test of
	/// the parsing/rolling helpers). npc_test_stat_range (scripts/Tests/NpcStatRangeTest.def) has
	/// a single-value Str (no roll ambiguity) and a {min,max} range for Dex/Int.
	/// </summary>
	[Collection(NpcStatRangeCollection.Name)]
	public class NpcStatRangeTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcStatRangeTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StrDexIntApplyFromTheConfiguredRangesOnCreate() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "StatRangeChecker")) {
				client.SendSpeech(".TestCreateStatRangeNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NPC_STAT_RANGE_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_STAT_RANGE_TEST_RESULT:");

				Assert.Contains("str=777", line);
				Assert.Contains("dex=100", line);

				int intValue = ParseIntField(line, "int=");
				Assert.InRange(intValue, 50, 60);
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

	[CollectionDefinition(NpcStatRangeCollection.Name, DisableParallelization = true)]
	public class NpcStatRangeCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcStatRangePair";
	}
}
