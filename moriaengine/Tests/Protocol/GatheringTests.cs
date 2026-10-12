using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// V-7 (#75): taming/mining/lumberjacking, over real skill-use (0x12) + target-cursor (0x6c)
	/// packets. Every scenario flips the test account to GM first (SkillDef.CheckSuccess always
	/// returns true for a GM) so skill success is deterministic without grinding real skill
	/// points - the real reachability/validity checks (IsValidTameTarget/IsValidVein/
	/// IsValidTree) still run unconditionally before that, so this doesn't skip testing whether
	/// the target is actually legal, only the RNG success roll itself.
	/// </summary>
	[Collection(GatheringCollection.Name)]
	public class GatheringTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public GatheringTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>Real UO rule: taming a valid, unowned tameable creature sets its
		/// ControlMaster to the tamer.</summary>
		[Fact]
		public void TamingSetsControlMaster() {
			using (var client = this.ConnectLoginAndCreateCharacter("Tamer")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				int markSetup = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSpawnTameable");
				Assert.True(this.fixture.WaitForGameOutputSince("TAME_SETUP:", markSetup, TimeSpan.FromSeconds(10)));
				uint npcUid = ExtractUidAfter(this.fixture.GameOutputSnapshot(), markSetup, "TAME_SETUP: npc=#");

				client.SendSkillUse(35); // AnimalTaming
				Thread.Sleep(300);
				client.SendTargetUid(npcUid);
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckControlMaster " + npcUid);
				Assert.True(this.fixture.WaitForGameOutputSince("TAME_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("controlMaster=Tamer", LastLineContaining(this.fixture.GameOutputSnapshot(), "TAME_CHECK:"));
			}
		}

		/// <summary>Real UO rule (this engine's own simplification: any impassable land tile
		/// stands in for a real ore vein, see MiningSkillDef's own doc comment): mining a valid
		/// vein yields a real ore item in the backpack.</summary>
		[Fact]
		public void MiningYieldsOre() {
			using (var client = this.ConnectLoginAndCreateCharacter("Miner")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				var (x, y, z) = client.ReadSpawnPosition(TimeSpan.FromSeconds(10));

				client.SendSkillUse(45); // Mining
				Thread.Sleep(300);
				client.SendTargetGround(x, y, z); // the exact spawn tile is impassable real map data (see V-6's identical finding)
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHasOre");
				Assert.True(this.fixture.WaitForGameOutputSince("BACKPACK_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1", LastLineContaining(this.fixture.GameOutputSnapshot(), "BACKPACK_CHECK:"));
			}
		}

		/// <summary>Real UO rule (this engine's own simplification: any world static flagged
		/// TileFlag.Foliage stands in for a real tree, see LumberjackingSkillDef's own doc
		/// comment): chopping a valid tree yields a real log item in the backpack.</summary>
		[Fact]
		public void LumberjackingYieldsWood() {
			using (var client = this.ConnectLoginAndCreateCharacter("Lumberjack")) {
				client.SendSpeech(".TestSetGmOn");
				Assert.True(this.fixture.WaitForGameOutput("GM_MODE_TEST: SET to GM (IsGM=True)", TimeSpan.FromSeconds(10)));

				int markFind = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestGoToNearbyFoliage");
				Assert.True(this.fixture.WaitForGameOutputSince("FOLIAGE_FOUND:", markFind, TimeSpan.FromSeconds(10)));
				string foliageLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "FOLIAGE_FOUND:");
				Assert.DoesNotContain("none", foliageLine);
				var (fx, fy, fz, model) = ExtractFoliage(foliageLine);

				client.SendSkillUse(44); // Lumberjacking
				Thread.Sleep(300);
				client.SendTargetStatic(fx, fy, fz, model);
				Thread.Sleep(1500);

				int markCheck = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCheckHasLog");
				Assert.True(this.fixture.WaitForGameOutputSince("BACKPACK_CHECK:", markCheck, TimeSpan.FromSeconds(10)));
				Assert.Contains("count=1", LastLineContaining(this.fixture.GameOutputSnapshot(), "BACKPACK_CHECK:"));
			}
		}

		private static uint ExtractUidAfter(string output, int since, string marker) {
			int idx = output.IndexOf(marker, since, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' after position " + since + " in:\n" + output);
			string rest = output.Substring(idx + marker.Length);
			return ParseLeadingUInt(rest);
		}

		private static (int X, int Y, int Z, int Model) ExtractFoliage(string line) {
			int x = ParseLabelledInt(line, "x=");
			int y = ParseLabelledInt(line, "y=");
			int z = ParseLabelledInt(line, "z=");
			int model = ParseLabelledInt(line, "model=");
			return (x, y, z, model);
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
	public class GatheringCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatheringPair";
	}
}
