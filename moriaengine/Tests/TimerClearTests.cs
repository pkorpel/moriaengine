using System;
using SteamEngine.Timers;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#1452 - Timer.Clear drops the queued changes; a timer whose change it dropped can queue one again (it stayed
	/// marked as queued, so its later re-arms and deletes did nothing).</summary>
	public class TimerClearTests {
		private sealed class ProbeTimer : Timer {
			protected override void OnTimeout() {
			}
		}

		[Fact]
		public void ATimerWhoseChangeWasDroppedQueuesAgain() {
			Timer.Clear();
			var timer = new ProbeTimer();
			timer.DueInSpan = TimeSpan.FromSeconds(-1); //a change, queued: no clock needed for a pause
			Assert.Equal(1, Timer.PendingChangesCount);
			Timer.Clear();
			Assert.Equal(0, Timer.PendingChangesCount);
			timer.DueInSpan = TimeSpan.FromSeconds(-1);
			Assert.Equal(1, Timer.PendingChangesCount);
			Timer.Clear();
		}

		[Fact]
		public void ATimerWhoseChangeWasDroppedCanStillBeDeleted() {
			Timer.Clear();
			var timer = new ProbeTimer();
			timer.DueInSpan = TimeSpan.FromSeconds(-1);
			Timer.Clear();
			timer.Delete(); //its delete used to be skipped as well
			Assert.Equal(1, Timer.PendingChangesCount);
			Timer.Clear();
		}
	}
}
