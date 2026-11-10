using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#1228 item 7, #1364 - one Delete of a container of up to 20,000 items (flat, in bags, each with a timer and a tag,
	/// open to a viewer) takes everything with it quickly; a client with a container open gets the container's delete, not one
	/// per item (a dagger deleted alone still reaches it). Server side: scripts/Tests/BigContainerDeleteTests.cs.</summary>
	[Collection(BigContainerDeleteCollection.Name)]
	public class BigContainerDeleteTests : LivingWorldTestBase {
		public BigContainerDeleteTests(GameServerPairFixture servers)
			: base(servers, "bigdelete", "BIGDELETE") {
		}

		[Fact]
		public void ABigContainerIsDeletedQuickly() {
			using (TestGameClient game = this.Login("Mazac")) {
				this.Run(game, ".TestBigContainerDelete", "BIGDELETE_DONE");
				this.AssertChecks("allGone", "twentyThousandTimedUnderHalfASecond", "twentyThousandOpenUnderATenthOfASecond");
			}
		}

		[Fact]
		public void AViewerGetsOnlyTheContainersDelete() {
			using (TestGameClient game = this.Login("Divak")) {
				//#1364 - a client with the chest open gets the chest's delete, which takes its contents with it on the client,
				//and no delete for each item (before, one each: 50 here, 20,000 in a big one)
				this.Run(game, ".TestBigContainerViewSetup", "BIGDELETE_VIEW:");
				string view = this.LastLine("BIGDELETE_VIEW:");
				uint chest = (uint) Number(view, "chest");
				var daggers = new HashSet<uint>();
				string list = view.Substring(view.IndexOf("daggers=", StringComparison.Ordinal) + 8);
				foreach (string uid in list.Substring(0, list.IndexOf(' ')).Split(',')) {
					daggers.Add(uint.Parse(uid, CultureInfo.InvariantCulture));
				}
				game.SendDoubleClick(chest);
				Thread.Sleep(1000);

				//the control: a dagger deleted alone from the open chest does reach its viewer, so the loop below can see them
				this.Run(game, ".TestBigContainerViewDeleteOne", "BIGDELETE_VIEWONE:");
				uint one = (uint) Number(this.LastLine("BIGDELETE_VIEWONE:"), "dagger");
				Assert.Contains(one, ReadDeletes(game, TimeSpan.FromSeconds(3)));
				daggers.Remove(one);

				this.Run(game, ".TestBigContainerViewDelete", "BIGDELETE_VIEWDELETED:");
				Assert.Contains("wasOpen=True gone=True", this.LastLine("BIGDELETE_VIEWDELETED:"));
				List<uint> deletes = ReadDeletes(game, TimeSpan.FromSeconds(3));
				Assert.True(deletes.Contains(chest), "the client never got the chest's delete");
				Assert.Equal(0, deletes.FindAll(daggers.Contains).Count);
			}
		}

		/// <summary>The uids of the deletes (0x1D) the client gets within `wait`.</summary>
		private static List<uint> ReadDeletes(TestGameClient game, TimeSpan wait) {
			var uids = new List<uint>();
			DateTime until = DateTime.UtcNow + wait;
			while (DateTime.UtcNow < until) {
				byte[] p;
				try {
					p = game.ReadPacketPayload(0x1d, until - DateTime.UtcNow);
				} catch (TimeoutException) {
					break;
				}
				uids.Add((uint) ((p[0] << 24) | (p[1] << 16) | (p[2] << 8) | p[3]) & ~0x40000000u); //an item's serial carries the item flag
			}
			return uids;
		}
	}

	[CollectionDefinition(Name)]
	public class BigContainerDeleteCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "BigContainerDelete";
	}
}
