using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's f_alore.</summary>
	[Collection(AnimalLoreCollection.Name)]
	public class AnimalLoreTests {
		private const int AnimalLoreSkill = 2;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public AnimalLoreTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static void Expect(TestGameClient client, params string[] patterns) {
			DateTime deadline = DateTime.UtcNow + Wait;
			foreach (string pattern in patterns) {
				string seen = "";
				try {
					string message;
					while (!Regex.IsMatch(message = client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
						seen += message + " | ";
					}
				} catch (TimeoutException) {
					throw new TimeoutException("No '" + pattern + "', only: " + seen);
				}
			}
		}

		private static void Look(TestGameClient client, uint target) {
			client.SendSkillUse(AnimalLoreSkill);
			Thread.Sleep(300);
			client.SendTargetUid(target);
		}

		private uint Animal(TestGameClient client, string how) {
			return uint.Parse(Regex.Match(this.Ask(client, ".TestLoreAnimal \"" + how + "\"", "LORE_ANIMAL:"), @"npc=#(\d+)").Groups[1].Value);
		}

		[Fact]
		public void TellsOwnerHungerAndFoodBySkill() {
			using var client = CommandPermissionTests.Connect("loretest", "Lore Tester");
			this.Ask(client, ".TestLorePrepare \"1100\"", "LORE_PREPARED:");

			//a wild one is its own master, and always fed
			Look(client, this.Animal(client, "0"));
			Expect(client, "Test Rabbit je svym vlastnim panem", "Test Rabbit ma dost a funi", "Test Rabbit se zivi vecmi jako: .+\\.");

			//a pet's hunger shows, and its owner
			string max = Regex.Match(this.Ask(client, ".TestLoreAnimal \"0\"", "LORE_ANIMAL:"), @"max=(\d+)").Groups[1].Value;
			Look(client, this.Animal(client, "0 " + max));
			Expect(client, "Test Rabbit patri osobe jmenem Lore Tester", "Test Rabbit ma velky hlad");

			//knowing less than the animal does
			Look(client, this.Animal(client, "1101"));
			Expect(client, "svym vlastnim panem", "ma dost a funi", "jen Yavanna vi");

			//no lore, nothing more than the owner
			this.Ask(client, ".TestLorePrepare \"0\"", "LORE_PREPARED:");
			Look(client, this.Animal(client, "0"));
			Expect(client, "svym vlastnim panem", "Nic jsi nezjistil");

			//something else entirely
			uint item = uint.Parse(Regex.Match(this.Ask(client, ".TestLoreItem", "LORE_ITEM:"), @"#(\d+)").Groups[1].Value);
			Look(client, item);
			Expect(client, "To neni zvire");
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class AnimalLoreCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "AnimalLore";
	}
}
