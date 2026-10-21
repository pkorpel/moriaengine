using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #561 - proves NPCDef.MaxHitsRange/MaxStamRange/MaxManaRange (scripts/Basic/CharClasses/
	/// NPC.cs) actually apply on NPC.On_Create, over the real client protocol, AND that current
	/// Hits/Stam/Mana get raised to match the new max pool (full-heal-on-seed) rather than being
	/// left at the old 100-point Character.ct default. npc_test_hp_pool (scripts/Tests/
	/// NpcHpPoolTest.def) has {min,max} ranges for MaxHits/MaxStam and a single-value MaxMana=0
	/// (matching the real archive's own c_titan, to prove 0 is a real configured value, not
	/// mistaken for "unconfigured").
	/// </summary>
	[Collection(NpcHpPoolCollection.Name)]
	public class NpcHpPoolTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public NpcHpPoolTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void MaxPoolsApplyAndCurrentValuesAreRaisedToFull() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "HpPoolChecker")) {
				client.SendSpeech(".TestCreateHpPoolNpcAndReport");
				Assert.True(this.fixture.WaitForGameOutput("NPC_HP_POOL_TEST_RESULT:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPC_HP_POOL_TEST_RESULT:");

				Assert.Contains("maxhits=500", line);
				Assert.Contains("hits=500", line);
				Assert.Contains("maxmana=0", line);
				Assert.Contains("mana=0", line);

				int maxStam = ParseIntField(line, "maxstam=");
				int stam = ParseIntField(line, "stam=");
				Assert.InRange(maxStam, 200, 300);
				Assert.Equal(maxStam, stam);
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

	[CollectionDefinition(NpcHpPoolCollection.Name, DisableParallelization = true)]
	public class NpcHpPoolCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcHpPoolPair";
	}
}
