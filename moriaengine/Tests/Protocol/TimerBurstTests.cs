using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>#960 - a burst of timer changes used to kill the timer thread, and with it the server.</summary>
	[Collection(TimerBurstCollection.Name)]
	public class TimerBurstTests {
		private readonly GameServerPairFixture fixture;

		public TimerBurstTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ABurstOfTimersLeavesThemRunning() {
			using var client = CommandPermissionTests.Connect("timerburst", "Timer Tester");
			for (int round = 0; round < 3; round++) {
				CommandPermissionTests.Ask(this.fixture, client, ".TestTimerBurst", "TIMER_BURST: done");
			}
			Thread.Sleep(500);
			Assert.DoesNotContain("Timer thread dying", this.fixture.GameOutputSnapshot());
			Assert.Contains("open=True", CommandPermissionTests.Ask(this.fixture, client, ".TestTimerAfterBurst", "TIMER_ARMED:"));
			Thread.Sleep(2500);
			Assert.Contains("open=False", CommandPermissionTests.Ask(this.fixture, client, ".TestTimerFired", "TIMER_FIRED:"));
		}
	}

	/// <summary>A Thing in the last slot of the array of all Things used to make enumerating them, saves included, throw.</summary>
	[Collection(ThingsArrayCollection.Name)]
	public class ThingsArrayTests {
		private readonly GameServerPairFixture fixture;

		public ThingsArrayTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ThingsFillingTheArray() {
			using var client = CommandPermissionTests.Connect("thingsarray", "Array Tester");
			Assert.Contains("good made=2100", CommandPermissionTests.Ask(this.fixture, client, ".TestThingsFillingTheArray", "THINGS_ARRAY:"));
		}
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class ThingsArrayCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "ThingsArray";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TimerBurstCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TimerBurst";
	}
}
