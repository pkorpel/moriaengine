using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>Real user-reported bug: dropping one gold pile directly onto another (the
	/// classic UO "combine stacks" gesture) didn't merge them. Root cause: AbstractItemDef.
	/// IsStackable just returned the "stackable" FieldValue's coded default (false) - real
	/// Sphere/UO content (including gold itself) essentially never sets STACKABLE= explicitly;
	/// stackability is meant to come from the item's own GRAPHIC's tiledata flag
	/// (UFLAG2_STACKABLE/CAN_I_PILE), same source TileData.GenerateMissingDefs already reads
	/// correctly for its own auto-stub fallback path - just never consulted for any real,
	/// script-authored itemdef. Fixed: IsStackable now falls back to the real tiledata flag for
	/// this item's Model whenever the script never explicitly overrode it either way.</summary>
	[Collection(ItemStackingCollection.Name)]
	public class ItemStackingTests {
		private const string Account = "protocoltest";
		private const string Password = "protocoltest";

		private readonly GameServerPairFixture fixture;

		public ItemStackingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void StackableFlagGeneralizesBeyondGold() {
			using (var client = this.ConnectLoginAndCreateCharacter("StackableFlagTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportStackableFlags");
				this.AssertSeen("STACKABLE_FLAG:", mark, TimeSpan.FromSeconds(10));
				string output = this.fixture.GameOutputSnapshot().Substring(mark);

				Assert.Contains("STACKABLE_FLAG: i_gold=True", output);
				Assert.Contains("STACKABLE_FLAG: i_bandage=True", output);
				Assert.Contains("STACKABLE_FLAG: i_arrow=True", output);
				Assert.Contains("STACKABLE_FLAG: i_bolt_jagged=True", output);
				Assert.Contains("STACKABLE_FLAG: i_xbolt=True", output);
				Assert.Contains("STACKABLE_FLAG: i_scroll_blank=True", output);
				//non-stackable control case - a sword must NOT get swept up by the fallback
				Assert.Contains("STACKABLE_FLAG: i_sword_broad_iron=False", output);
			}
		}

		[Fact]
		public void DroppingOneGoldPileOnAnotherMergesThem() {
			using (var client = this.ConnectLoginAndCreateCharacter("GoldStackTester")) {
				client.SendSpeech(".TestSetGmOn");
				Thread.Sleep(500);

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupTwoGoldPiles");
				this.AssertSeen("GOLD_STACK_SETUP:", mark, TimeSpan.FromSeconds(10));
				string setupLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "GOLD_STACK_SETUP:");
				uint pileA = ExtractUidAfter(setupLine, "pileA=");
				uint pileB = ExtractUidAfter(setupLine, "pileB=");
				uint backpackUid = ExtractUidAfter(setupLine, "backpack=");

				//open own backpack first - required for reachability, same note as EconomyTests/
				//HousingTests' identical setup step.
				client.SendDoubleClick(backpackUid);
				Thread.Sleep(500);

				//classic UO "combine stacks" gesture: pick up pile A (its whole amount, so it
				//drags as one item rather than splitting), then drop it directly onto pile B -
				//the client sends pile B's own uid as the drop packet's "container" field (see
				//DropItemInPacket.Handle's own comment: "client put it on some other item").
				client.SendPickUpItem(pileA, 50);
				Thread.Sleep(300);
				client.SendDropItem(pileA, 0, 0, 0, (int) pileB);
				Thread.Sleep(500);

				int reportMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportGoldPile " + pileA);
				this.AssertSeen("GOLD_STACK_RESULT: uid=" + pileA, reportMark, TimeSpan.FromSeconds(10));
				string pileALine = LastLineContaining(this.fixture.GameOutputSnapshot(), "GOLD_STACK_RESULT: uid=" + pileA);
				Assert.Contains("exists=False", pileALine);

				reportMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportGoldPile " + pileB);
				this.AssertSeen("GOLD_STACK_RESULT: uid=" + pileB, reportMark, TimeSpan.FromSeconds(10));
				string pileBLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "GOLD_STACK_RESULT: uid=" + pileB);
				Assert.Contains("amount=80", pileBLine);
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

		private static uint ExtractUidAfter(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			string rest = line.Substring(idx + marker.Length);
			int end = 0;
			while ((end < rest.Length) && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
		}

		/// <summary>See CombatDepthTestBase's identical helper for why this retries - the same
		/// account-reuse/connection race applies here.</summary>
		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(Account);
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

	[CollectionDefinition(ItemStackingCollection.Name, DisableParallelization = true)]
	public class ItemStackingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ItemStackingPair";
	}
}
