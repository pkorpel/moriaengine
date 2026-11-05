using System;
using System.Text.RegularExpressions;
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

	/// <summary>#1187 - a recompile saves the world and loads it again with the timer thread running. The loaded timers wait in
	/// the change queue for that thread, so a door opened before the recompile still closes after it. #1188 - and no timer of
	/// the old world fires after it.</summary>
	[Collection(TimerRecompileCollection.Name)]
	public class TimerRecompileTests {
		private static readonly TimeSpan RecompileWait = TimeSpan.FromSeconds(240);
		private readonly GameServerPairFixture fixture;

		public TimerRecompileTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void ASavedTimerFiresAfterARecompile() {
			using var client = CommandPermissionTests.Connect("timerrecompile", "Recompile Tester");
			string armed = CommandPermissionTests.Ask(this.fixture, client, ".TestTimerArmSaved 20", "TIMER_SAVED:");
			Assert.Contains("open=True", armed);
			string uid = Regex.Match(armed, @"uid=(\d+)").Groups[1].Value;
			Assert.Contains("count=500", CommandPermissionTests.Ask(this.fixture, client, ".TestStaleProbeArm 500", "STALE_ARMED:"));

			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".serv.recompile");
			Assert.True(this.fixture.WaitForGameOutputSince("Recompiling scripts...", mark, TimeSpan.FromSeconds(30)),
				"No recompile:\n" + this.fixture.GameOutputSnapshot().Substring(mark));

			//commands wait while it recompiles: ask until the reloaded door answers, then until it has closed
			DateTime until = DateTime.UtcNow + RecompileWait;
			string state = null;
			while (DateTime.UtcNow < until) {
				int askMark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestTimerDoorAt " + uid);
				if (this.fixture.WaitForGameOutputSince("TIMER_DOOR:", askMark, TimeSpan.FromSeconds(10))) {
					string output = this.fixture.GameOutputSnapshot();
					int index = output.LastIndexOf("TIMER_DOOR:", StringComparison.Ordinal);
					int end = output.IndexOf('\n', index);
					state = (end < 0) ? output.Substring(index) : output.Substring(index, end - index);
					if (state.Contains("open=False")) {
						break;
					}
				}
				Thread.Sleep(2000);
			}
			Assert.True(state != null, "The server never answered after the recompile:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
			Assert.Contains("found=True", state); //the door came back from the save
			Assert.Contains("open=False", state); //and its timer, loaded during the recompile, fired

			//#1188 - the probe timers ticked through the recompile: only their reloaded copies fire, none of the old world's
			string stopped = CommandPermissionTests.Ask(this.fixture, client, ".TestStaleProbeStop 500", "STALE_STOPPED:");
			Assert.Contains("found=500 ", stopped); //all came back from the save
			Assert.DoesNotContain("fired=0 ", stopped); //the reloaded ones did fire (the count restarts with the new scripts)
			Assert.DoesNotContain("STALE_PROBE: a timer of the old world fired", this.fixture.GameOutputSnapshot().Substring(mark));
			Assert.DoesNotContain("Timer thread dying", this.fixture.GameOutputSnapshot());
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
	public class TimerRecompileCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TimerRecompile";
	}

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class TimerBurstCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "TimerBurst";
	}
}
