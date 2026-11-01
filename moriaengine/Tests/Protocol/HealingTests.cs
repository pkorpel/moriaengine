using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Erebor's bandages: treating, washing, raising the dead.</summary>
	[Collection(HealingCollection.Name)]
	public class HealingTests {
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(12);
		private readonly GameServerPairFixture fixture;

		public HealingTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		private static uint Uid(string report, string name) {
			return uint.Parse(Regex.Match(report, name + @"=(\d+)").Groups[1].Value);
		}

		private static int Number(string report, string name) {
			return int.Parse(Regex.Match(report, @"\b" + name + @"=(-?\d+)").Groups[1].Value);
		}

		private static void WaitForMessage(TestGameClient client, string pattern) {
			DateTime deadline = DateTime.UtcNow + Wait;
			while (!Regex.IsMatch(client.ReadSystemMessage(deadline - DateTime.UtcNow), pattern)) {
			}
		}

		private static void Use(TestGameClient client, uint bandage, uint on) {
			client.SendDoubleClick(bandage);
			WaitForMessage(client, "Koho chces osetrit|wash these");
			client.SendTargetUid(on);
		}

		[Fact]
		public void BandagesHealWashAndRaise() {
			using var client = CommandPermissionTests.Connect("healtest", "Heal Tester");
			string ready = this.Ask(client, ".TestHealReady \"priest\"", "HEAL_READY:");
			uint me = Uid(ready, "me"), bandage = Uid(ready, "bandage");
			int hurt = Number(ready, "hits");
			client.SendDoubleClick(Uid(ready, "pack"));
			Thread.Sleep(300);

			//5 s for one bandage, and a second one cuts the first short
			Use(client, bandage, me);
			WaitForMessage(client, "Pokousis se vylecit");
			Use(client, bandage, me);
			WaitForMessage(client, "Leceni nesmis uspechat");
			WaitForMessage(client, "uspesne osetren");
			string healed = this.Ask(client, ".TestHealState", "HEAL_STATE:");
			Assert.True(Number(healed, "hits") > hurt, healed);
			Assert.Contains("clean=58 bloody=1", healed);

			//a ranger raises a beast, 20 bandages gone and 20 back bloody
			this.Ask(client, ".TestHealReady \"ranger\"", "HEAL_READY:");
			string corpse = this.Ask(client, ".TestHealAnimalCorpse", "HEAL_CORPSE:");
			Use(client, bandage, Uid(corpse, "corpse"));
			WaitForMessage(client, "ozivit mrtvolu zvirete");
			WaitForMessage(client, "Oziveni uspesne");
			Thread.Sleep(200);
			string raised = this.Ask(client, ".TestHealRaised", "HEAL_RAISED:");
			Assert.Contains("rabbits=1 corpseGone=True bloody=21", raised);

			//a priest raises a person whose ghost stands by in war mode
			client.SendSpeech(".TestEnableAutoAccountCreation");
			Thread.Sleep(300);
			using var ghost = CommandPermissionTests.Connect("healghost", "Heal Ghost");
			this.Ask(client, ".TestHealReady \"priest\"", "HEAL_READY:");
			string died = this.Ask(ghost, ".TestHealDie", "HEAL_DIED:");
			string at = Number(died, "x") + " " + Number(died, "y") + " " + Number(died, "z");
			this.Ask(client, ".TestHealGhostHere \"" + at + "\"", "HEAL_GHOST_HERE:");
			Use(client, bandage, Uid(died, "body"));
			WaitForMessage(client, "navrati k zivotu");
			WaitForMessage(client, "Ozivil jsi");
			Assert.Contains("dead=False", this.Ask(ghost, ".TestHealAlive", "HEAL_ALIVE:"));

			//a tub of blood turns the whole stack bloody
			string tub = this.Ask(client, ".TestHealTub", "HEAL_TUB:");
			Use(client, bandage, Uid(tub, "tub"));
			Thread.Sleep(300);
			Assert.Contains("clean=0 ", this.Ask(client, ".TestHealState", "HEAL_STATE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class HealingCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Healing";
	}
}
