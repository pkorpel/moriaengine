using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#808 - items dropped on NPCs, Sphere 0.99z8's NPC_OnItemGive as far as Erebor's
	/// content reaches it (scripts/Basic/CharClasses/NpcItemGive.cs), plus what the real
	/// Character.IsPetOf unlocked: opening your pack animal's pack, loading it, mounting your own
	/// horse. All with real pick-up/drop/double-click packets from a non-GM player.
	/// Server-side helpers: scripts/Tests/NpcItemGiveTests.cs, NPCs: NpcItemGiveTest.def.</summary>
	[Collection(NpcItemGiveCollection.Name)]
	public class NpcItemGiveTests {
		private const string Account = "npcitemgivetest";
		private const string Password = "npcitemgivetest";
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);

		private readonly GameServerPairFixture fixture;

		public NpcItemGiveTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>An NPC that isn't yours turns the item down: "They don't appear to want the
		/// item", or its own @NPCRefuseItem line (Erebor's guards) instead. The item comes back.</summary>
		[Fact]
		public void StrangerRefuses_WithItsOwnLineIfItHasOne() {
			using (var client = this.Connect("Give Tester One")) {
				var items = this.Items(client);
				uint plain = this.Spawn(client, "npc_test_give_plain", false).Npc;
				uint bribe = this.Spawn(client, "npc_test_give_bribe", false).Npc;

				this.Drop(client, items.Sword, plain);
				Assert.True(SawSpeechContaining(client, "don't appear to want", Wait), "no refusal message");
				AssertHas(this.Report(client, items.Sword, plain), "inMyPack=True");

				this.Drop(client, items.Sword, bribe);
				Assert.True(SawSpeechContaining(client, "You cannot bribe me!", Wait), "the NPC's own @NPCRefuseItem line never came");
				AssertHas(this.Report(client, items.Sword, bribe), "inMyPack=True");

				//real converted Erebor content - ents.scp's guard, its @NPCRefuseItem ported by the converter
				uint entGuard = this.Spawn(client, "C_f_GUARD_ent2", false).Npc;
				this.Drop(client, items.Sword, entGuard);
				Assert.True(SawSpeechContaining(client, "Mne nemuzes podplatit.", Wait), "the ent guard's own line never came");
				AssertHas(this.Report(client, items.Sword, entGuard), "inMyPack=True");
			}
		}

		/// <summary>My animal pet eats food it's hungry for; anything else is refused (Sphere).</summary>
		[Fact]
		public void AnimalPetEatsFoodAndRefusesTheRest() {
			using (var client = this.Connect("Give Tester Two")) {
				var items = this.Items(client);
				uint dog = this.Spawn(client, "npc_test_give_animal", true).Npc;

				this.Drop(client, items.Ribs, dog);
				Thread.Sleep(300);
				string ribs = this.Report(client, items.Ribs, dog);
				Assert.True(!ribs.Contains("amount=3", StringComparison.Ordinal), "the dog didn't eat: " + ribs);
				Assert.True(!ribs.Contains("food=1 ", StringComparison.Ordinal), "hunger didn't go up: " + ribs);

				this.Drop(client, items.Sword, dog);
				Assert.True(SawSpeechContaining(client, "don't appear to want", Wait), "no refusal message");
				AssertHas(this.Report(client, items.Sword, dog), "inMyPack=True");
			}
		}

		/// <summary>My humanoid pet (hireling) eats food ("Mmm. Thank you.") and carries anything
		/// else in its pack ("Tell me to drop this..."); I can open that pack and take it back.</summary>
		[Fact]
		public void HumanoidPetEatsAndCarries_OwnerOpensItsPack() {
			using (var client = this.Connect("Give Tester Three")) {
				var items = this.Items(client);
				var (npc, npcPack) = this.Spawn(client, "npc_test_give_human", true);

				this.Drop(client, items.Ribs, npc);
				Assert.True(SawSpeechContaining(client, "Mmm. Thank you.", Wait), "the hireling didn't thank for the food");

				this.Drop(client, items.Sword, npc);
				Assert.True(SawSpeechContaining(client, "Tell me to drop this", Wait), "the hireling didn't take the item");
				AssertHas(this.Report(client, items.Sword, npc), "inNpcPack=True");

				client.SendDoubleClick(npcPack); //my pet's pack opens - not a snoop
				Thread.Sleep(500);
				client.SendPickUpItem(items.Sword, 1);
				Thread.Sleep(300);
				client.SendDropItem(items.Sword, 0xFFFF, 0xFFFF, 0, (int) items.Pack);
				Thread.Sleep(300);
				AssertHas(this.Report(client, items.Sword, npc), "inMyPack=True");
			}
		}

		/// <summary>Double-clicking my pack horse opens its pack and I can load it; double-clicking
		/// my own horse mounts it - both were GM-only while IsPetOf was a stub.</summary>
		[Fact]
		public void PackHorseOpensAndLoads_OwnHorseCanBeMounted() {
			using (var client = this.Connect("Give Tester Four")) {
				var items = this.Items(client);
				var (packHorse, horsePack) = this.Spawn(client, "c_horse_pack", true);

				client.SendDoubleClick(packHorse);
				Thread.Sleep(500);
				client.SendPickUpItem(items.Sword, 1);
				Thread.Sleep(300);
				client.SendDropItem(items.Sword, 50, 50, 0, (int) horsePack);
				Thread.Sleep(300);
				AssertHas(this.Report(client, items.Sword, packHorse), "inNpcPack=True");

				uint horse = this.Spawn(client, "c_horse_brown_dk", true).Npc;
				client.SendDoubleClick(horse);
				Thread.Sleep(500);
				AssertHas(this.Report(client, items.Sword, horse), "mountedOn=");
				string mounted = this.Report(client, items.Sword, horse);
				Assert.True(!mounted.Contains("mountedOn=none", StringComparison.Ordinal), "my own horse didn't let me ride: " + mounted);
				client.SendSpeech(".TestGiveDismount");
			}
		}

		private struct GiveItems {
			public uint Pack, Sword, Ribs;
		}

		private GiveItems Items(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestGiveItems");
			Assert.True(this.fixture.WaitForGameOutputSince("GIVE_ITEMS:", mark, Wait));
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "GIVE_ITEMS:");
			GiveItems g = new GiveItems { Pack = ExtractUidAfter(line, "pack="), Sword = ExtractUidAfter(line, "sword="), Ribs = ExtractUidAfter(line, "ribs=") };
			client.SendDoubleClick(g.Pack); //open own pack first - needed for reachability
			Thread.Sleep(300);
			return g;
		}

		private (uint Npc, uint Pack) Spawn(TestGameClient client, string defname, bool tame) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestGiveSpawn \"" + defname + "\" " + (tame ? 1 : 0));
			string marker = "GIVE_SPAWN: " + defname;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait), "no " + marker);
			string line = LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
			return (ExtractUidAfter(line, "npc="), ExtractUidAfter(line, "pack="));
		}

		private void Drop(TestGameClient client, uint item, uint onChar) {
			client.SendPickUpItem(item, 1);
			Thread.Sleep(300);
			client.SendDropItem(item, 0, 0, 0, (int) onChar);
		}

		private string Report(TestGameClient client, uint item, uint npc) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestGiveReport " + item + " " + npc);
			string marker = "GIVE_REPORT: item=" + item;
			Assert.True(this.fixture.WaitForGameOutputSince(marker, mark, Wait));
			return LastLineContaining(this.fixture.GameOutputSnapshot(), marker);
		}

		private static void AssertHas(string line, string expected) {
			Assert.True(line.Contains(expected, StringComparison.Ordinal), "Expected '" + expected + "' in: " + line);
		}

		private TestGameClient Connect(string charName) {
			TestGameClient client = this.ConnectLoginAndCreateCharacter(charName);
			client.SendSpeech(".TestTeleportToOpenGround");
			Thread.Sleep(500);
			client.SendSpeech(".TestSetGmOff");
			Thread.Sleep(500);
			return client;
		}

		private static bool SawSpeechContaining(TestGameClient client, string text, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (DateTime.UtcNow < deadline) {
				string message;
				try {
					message = client.ReadSystemMessage(deadline - DateTime.UtcNow);
				} catch (TimeoutException) {
					return false;
				}
				if (message.Contains(text, StringComparison.Ordinal)) {
					return true;
				}
			}
			return false;
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

		//character names may only hold letters and spaces - a digit gets the client dropped
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
			throw new TimeoutException("Login server never listed a game server.", lastFailure);
		}
	}

	[CollectionDefinition(NpcItemGiveCollection.Name, DisableParallelization = true)]
	public class NpcItemGiveCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "NpcItemGivePair";
	}
}
