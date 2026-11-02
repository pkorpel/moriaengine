using System;
using Xunit;

namespace SteamEngine.Tests.Protocol {

	/// <summary>#1009 - a ring asks for kills of the last 3 days, whenever they happened, with no click to start them.</summary>
	[Collection(RingQuestCollection.Name)]
	public class RingQuestTests {
		private const int FourDays = 4 * 24 * 60 * 60, TwoDays = 2 * 24 * 60 * 60;
		private readonly GameServerPairFixture fixture;

		public RingQuestTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		private string Ask(TestGameClient client, string speech, string marker) {
			return CommandPermissionTests.Ask(this.fixture, client, speech, marker);
		}

		[Fact]
		public void TheLastThreeDaysCount() {
			using var killer = CommandPermissionTests.Connect("ringkiller", "Ring Killer");
			this.Ask(killer, ".TestEnableAutoAccountCreation", "AUTO_ACCOUNT_TEST: ENABLED");
			using var victim = CommandPermissionTests.Connect("ringvictim", "Ring Victim");
			this.Ask(killer, ".TestRingKiller", "RING_KILLER:");
			this.Ask(victim, ".TestRingVictim \"30\"", "RING_VICTIM:");

			Assert.Contains("recent=0 passed=False", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));

			//kills from before anyone clicked count; four aren't enough
			this.Ask(victim, ".TestRingKilled \"4\"", "RING_KILLED:");
			Assert.Contains("recent=4 passed=False", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));

			//two days old they're still good, and a fifth does it
			this.Ask(killer, ".TestRingAge \"" + TwoDays + "\"", "RING_AGED:");
			this.Ask(victim, ".TestRingKilled \"1\"", "RING_KILLED:");
			Assert.Contains("recent=5 passed=True", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));

			//past three days the oldest four drop out on their own
			this.Ask(killer, ".TestRingAge \"" + (TwoDays / 2 + 60) + "\"", "RING_AGED:");
			Assert.Contains("recent=1 passed=False", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));
			this.Ask(killer, ".TestRingAge \"" + FourDays + "\"", "RING_AGED:");
			Assert.Contains("recent=0 passed=False", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));

			//a victim below the level doesn't count
			this.Ask(victim, ".TestRingVictim \"29\"", "RING_VICTIM:");
			this.Ask(victim, ".TestRingKilled \"5\"", "RING_KILLED:");
			Assert.Contains("recent=0 passed=False", this.Ask(killer, ".TestRingReport", "RING_REPORT:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class RingQuestCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "RingQuest";
	}
}
