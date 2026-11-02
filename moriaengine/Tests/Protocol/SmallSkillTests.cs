using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Begging, Spirit Speak, Herding, and a fire from kindling.</summary>
	[Collection(SmallSkillCollection.Name)]
	public class SmallSkillTests {
		private const int Begging = 6, SpiritSpeak = 32;
		private static readonly TimeSpan Wait = TimeSpan.FromSeconds(10);
		private readonly GameServerPairFixture fixture;

		public SmallSkillTests(GameServerPairFixture servers) {
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

		private static uint Uid(string line) {
			return uint.Parse(Regex.Match(line, @"#(\d+)").Groups[1].Value);
		}

		[Fact]
		public void EachDoesItsLittle() {
			using var client = CommandPermissionTests.Connect("smalltest", "Small Tester");
			this.Ask(client, ".TestSmallPrepare", "SMALL_PREPARED:");
			uint sheep = Uid(this.Ask(client, ".TestSmallThing \"c_a_jackrabbit\"", "SMALL_THING:"));

			client.SendSkillUse(Begging);
			Thread.Sleep(300);
			client.SendTargetUid(sheep);
			Expect(client, "U koho chces zebrat", "You grovel at Test Sheep's feet");

			client.SendSkillUse(SpiritSpeak);
			Thread.Sleep(3000);
			Assert.Contains("spirits=True", this.Ask(client, ".TestSmallReport " + sheep, "SMALL_REPORT:"));

			uint crook = Uid(this.Ask(client, ".TestSmallCrook", "SMALL_CROOK:"));
			client.SendDoubleClick(crook);
			Expect(client, "What would you like to herd");
			client.SendTargetUid(sheep);
			Expect(client, "Where do you want them to go");
			client.SendTargetUid(sheep);
			Thread.Sleep(3000);
			Assert.DoesNotContain("goal= ", this.Ask(client, ".TestSmallReport " + sheep, "SMALL_REPORT:"));

			uint kindling = Uid(this.Ask(client, ".TestSmallThing \"i_kindling\"", "SMALL_THING:"));
			client.SendDoubleClick(kindling);
			Thread.Sleep(500);
			Assert.Contains("True", this.Ask(client, ".TestSmallFire", "SMALL_FIRE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class SmallSkillCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "SmallSkills";
	}
}
