using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// #219 (redesigned per explicit user request): confirms the per-item decay timer actually
	/// survives a real save, not just assumed because the generic BoundTimer/TagHolder.tags
	/// save mechanism "should" carry it (that mechanism is already proven for other per-item
	/// timers - NPCAiTimer, SpawnTimer - but this is new code using it for the first time for
	/// decay specifically, and persistence was the user's own explicit motivation for moving
	/// away from the original global-sweep design). Own isolated fixture/collection, same
	/// reasoning as DefPersistenceRegressionTests.cs - this triggers a real Globals.Save(),
	/// which other Facts sharing a fixture shouldn't have to deal with mid-run.
	/// </summary>
	[Collection(ItemDecayPersistenceCollection.Name)]
	public class ItemDecayPersistenceTests {
		// Deliberately not "protocoltest" - same reasoning as DefPersistenceRegressionTests.cs.
		private const string Account = "decaypersisttest";
		private const string Password = "decaypersisttest";

		private readonly GameServerPairFixture fixture;

		public ItemDecayPersistenceTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void DecayTimerIsIncludedInARealSave() {
			using (var client = this.ConnectLoginAndCreateCharacter("SaveTrigger")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestSetupPersistentDecayItemAndSave");
				Assert.True(this.fixture.WaitForGameOutputSince("ATTR_DECAY_PERSIST_SETUP:", mark, TimeSpan.FromSeconds(30)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "ATTR_DECAY_PERSIST_SETUP:");
				uint uid = ExtractUidAfter(line, "uid=");

				string savesRoot = Path.Combine(this.fixture.RepoRoot, "saves");
				string latestDir = Directory.GetDirectories(savesRoot)
					.SelectMany(Directory.GetDirectories)
					.OrderByDescending(Directory.GetLastWriteTimeUtc)
					.First();
				string thingsSav = File.ReadAllText(Path.Combine(latestDir, "things.sav"));

				// Real example of what this looks like (confirmed by direct inspection while
				// building this): the item's own block gets a "%_itemdecay_=(ItemDecayTimer)N"
				// reference line, and a separate "[ItemDecayTimer N]" block with its own
				// absolute "fireAt=" tick - the same generic referenced-object save shape every
				// other BoundTimer subclass in this engine already uses.
				int itemIdx = thingsSav.IndexOf("uid=" + uid, StringComparison.Ordinal);
				Assert.True(itemIdx >= 0, "Could not find the test item (uid=" + uid + ") in things.sav at all:\n" + thingsSav);
				string nearby = thingsSav.Substring(itemIdx, Math.Min(300, thingsSav.Length - itemIdx));
				Assert.Contains("(ItemDecayTimer)", nearby);
				Assert.Contains("[ItemDecayTimer", thingsSav);
				Assert.Contains("fireAt=", thingsSav);
			}
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx).TrimEnd('\r');
		}

		private static uint ExtractUidAfter(string text, string marker) {
			int idx = text.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + text);
			string rest = text.Substring(idx + marker.Length);
			int end = 0;
			while (end < rest.Length && char.IsDigit(rest[end])) {
				end++;
			}
			return uint.Parse(rest.Substring(0, end), System.Globalization.CultureInfo.InvariantCulture);
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

	[CollectionDefinition(ItemDecayPersistenceCollection.Name, DisableParallelization = true)]
	public class ItemDecayPersistenceCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ItemDecayPersistencePair";
	}
}
