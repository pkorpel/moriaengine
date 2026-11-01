using System;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#962 - Sphere's meditation: mana a stroke at a time, a step ends it.</summary>
	[Collection(MeditationCollection.Name)]
	public class MeditationTests {
		private readonly GameServerPairFixture fixture;

		public MeditationTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void ManaAStrokeAtATimeUntilAStep() {
			using var client = CommandPermissionTests.Connect("medtest", "Med Tester");
			//the opening roll can fail, so a few goes
			bool filled = false;
			for (int i = 0; (i < 8) && !filled; i++) {
				this.Ask(client, ".TestMedReady \"2\"", "MED_READY:");
				Thread.Sleep(1600);
				filled = this.Ask(client, ".TestMedState", "MED_STATE:").Contains("missing=0");
			}
			Assert.True(filled);

			//a trance under way stops at a step, however much is missing
			string state = "";
			for (int i = 0; i < 8; i++) {
				this.Ask(client, ".TestMedReady \"50\"", "MED_READY:");
				Thread.Sleep(1200);
				state = this.Ask(client, ".TestMedState", "MED_STATE:");
				if (state.Contains("skill=Meditation")) {
					break;
				}
			}
			Assert.Contains("skill=Meditation", state);
			this.Ask(client, ".TestMedStep", "MED_STEPPED:");
			Assert.DoesNotContain("skill=Meditation", this.Ask(client, ".TestMedState", "MED_STATE:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MeditationCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "Meditation";
	}
}
