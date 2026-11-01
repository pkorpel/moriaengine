using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#958 - gathering on real map spots, the rent chest, and where a click's text comes from.</summary>
	[Collection(GatherTilesCollection.Name)]
	public class GatherTilesTests {
		private const string AdminAccount = "gathertilesa", VisitorAccount = "gathertilesb";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

		private readonly GameServerPairFixture fixture;

		public GatherTilesTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void CaveFloorAndRockMineAndTrunksChop() {
			using var client = CommandPermissionTests.Connect(AdminAccount, "Gather Tester");
			string mine = this.Ask(client, ".TestGatherSpot \"5311 35 mine\"", "GATHER_SPOT:");
			Assert.Contains("0x53b=True", mine); //t_rock itself
			Assert.Contains("0x53d=True", mine); //its dupes
			Assert.Contains("0x53e=True", mine);
			Assert.Contains("0x8e0=True", mine); //stalagmites
			Assert.Contains("0x8e6=True", mine); //flowstone, their dupe
			Assert.Contains("0x274=False", mine); //a cave wall is a wall, as on Erebor
			Assert.Contains("region=r_doly", mine.Replace("[", "").Replace("RegionTypeDef ", ""));
			Assert.True(Number(mine, "yielded") > 0, mine);

			string forest = this.Ask(client, ".TestGatherSpot \"3050 1200 chop\"", "GATHER_SPOT:");
			Assert.Contains("0xcd0=True", forest); //the trunk
			Assert.Contains("0xcd1=True", forest); //its leaves
			Assert.Contains("0xc97=False", forest); //a plant isn't wood

			//about a third of trees hold nothing, as Erebor's r_default_tree says; the rest give wood
			string chopped = this.Ask(client, ".TestChopForest", "CHOP_FOREST:");
			int trees = Number(chopped, "trees"), wood = Number(chopped, "wood");
			Assert.True(trees > 50, chopped);
			Assert.True((wood * 2) > trees, chopped);
		}

		[Fact]
		public void AnyoneOpensTheChestAndPaysInIt() {
			using var admin = CommandPermissionTests.Connect(AdminAccount, "Chest Admin");
			this.Ask(admin, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			uint chest = (uint) Number(this.Ask(admin, ".TestChestSetup", "CHEST_SETUP:"), "chest");
			using var visitor = CommandPermissionTests.Connect(VisitorAccount, "Chest Visitor");
			string came = this.Ask(visitor, ".TestChestVisitor", "CHEST_VISITOR:");
			Assert.Contains("gm=False", came);
			uint gold = (uint) Number(came, "gold"), junk = (uint) Number(came, "junk");

			visitor.SendDoubleClick(chest);
			visitor.ReadPacketPayload(0x24, Wait);

			//it takes money only
			visitor.SendPickUpItem(junk, 1);
			Thread.Sleep(300);
			visitor.SendDropItem(junk, 0xFFFF, 0xFFFF, 0, (int) chest);
			SeeFrom(visitor, "Do truhly patri jen penize.", null);
			Assert.Contains("items=0 goldIn=False junkIn=False", this.Ask(visitor, ".TestChestReport", "CHEST_REPORT:"));

			//in goes the money, and nobody takes it out again
			visitor.SendPickUpItem(gold, 50000);
			Thread.Sleep(300);
			visitor.SendDropItem(gold, 0xFFFF, 0xFFFF, 0, (int) chest);
			Thread.Sleep(300);
			Assert.Contains("items=1 goldIn=True", this.Ask(visitor, ".TestChestReport", "CHEST_REPORT:"));
			visitor.SendPickUpItem(gold, 50000);
			SeeFrom(visitor, "uz patri k najmu", null);
			Assert.Contains("items=1 goldIn=True", this.Ask(visitor, ".TestChestReport", "CHEST_REPORT:"));

			//a click counts it, and says so from the chest, not as a system message
			visitor.SendSingleClick(chest);
			SeeFrom(visitor, "Najem je zaplacen na", chest);
			Assert.Contains("paidGone=True", this.Ask(visitor, ".TestChestReport", "CHEST_REPORT:"));
		}

		/// <summary>A message with this text, from that thing if one's given. uint.MaxValue is the system.</summary>
		private static void SeeFrom(TestGameClient client, string text, uint? speaker) {
			DateTime deadline = DateTime.UtcNow + Wait;
			string seen = "";
			while (DateTime.UtcNow < deadline) {
				byte[] payload = client.ReadPacketPayload(0xAE, Wait);
				const int prefix = 2 + 4 + 2 + 1 + 2 + 2 + 4 + 30;
				string message = Encoding.BigEndianUnicode.GetString(payload, prefix, payload.Length - prefix - 2);
				if (message.Contains(text, StringComparison.Ordinal)) {
					if (speaker.HasValue) {
						uint from = (uint) ((payload[2] << 24) | (payload[3] << 16) | (payload[4] << 8) | payload[5]);
						Assert.Equal(speaker.Value & 0x7fffffff, from & 0x7fffffff);
					}
					return;
				}
				seen += message + " | ";
			}
			Assert.Fail("No '" + text + "', saw: " + seen);
		}

		private static int Number(string line, string name) {
			return int.Parse(Regex.Match(line, name + @"=(\d+)").Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class GatherTilesCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "GatherTiles";
	}
}
