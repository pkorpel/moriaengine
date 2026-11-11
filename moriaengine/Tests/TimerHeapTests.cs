using System;
using System.Collections.Generic;
using SteamEngine.Timers;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#1451 - the timer heap keeps its own key. A game thread moves a queued timer's fireAt in place
	/// (PrivateSetFireAt) and only queues a change; the timer thread's heap work for other timers goes on until that change
	/// is taken in. Ordered by fireAt itself, that work sorted against a moved key and left due timers below later ones.</summary>
	public class TimerHeapTests {
		private sealed class ProbeTimer : Timer {
			protected override void OnTimeout() {
			}
		}

		private static TimeSpan Ms(int ms) {
			return TimeSpan.FromMilliseconds(ms);
		}

		[Fact]
		public void MovingAQueuedTimersTimeKeepsTheHeapInOrder() {
			for (int seed = 0; seed < 200; seed++) {
				var random = new Random(seed);
				var heap = new TimerPriorityQueue();
				var queued = new List<ProbeTimer>();
				var moved = new List<ProbeTimer>(); //what the change queue holds: moved in place, not yet taken in
				for (int i = 0; i < 40; i++) {
					var timer = new ProbeTimer();
					timer.fireAt = Ms(random.Next(1000));
					heap.Enqueue(timer);
					queued.Add(timer);
				}
				for (int step = 0; step < 300; step++) {
					int what = random.Next(3);
					if ((what == 0) && (queued.Count > 0)) { //a game thread re-arms a queued timer: fireAt moves, a change is queued
						ProbeTimer timer = queued[random.Next(queued.Count)];
						timer.fireAt = Ms(random.Next(1000));
						if (!moved.Contains(timer)) {
							moved.Add(timer);
						}
					} else if (what == 1) { //the timer thread takes in the changes (ProcessChanges)
						foreach (ProbeTimer timer in moved) {
							heap.Remove(timer);
							heap.Enqueue(timer);
						}
						moved.Clear();
					} else if (heap.Count > 0) { //the timer thread takes the head: one with a change pending is skipped (the change
						var timer = (ProbeTimer) heap.Dequeue(); //puts it back), any other fires and is re-armed (a periodic timer)
						if (!moved.Contains(timer)) {
							timer.fireAt = Ms(random.Next(1000));
							heap.Enqueue(timer);
						}
					}
				}
				foreach (ProbeTimer timer in moved) {
					heap.Remove(timer);
					heap.Enqueue(timer);
				}
				TimeSpan last = TimeSpan.MinValue;
				int count = heap.Count;
				for (int i = 0; i < count; i++) {
					Timer next = heap.Dequeue();
					Assert.True(next.fireAt >= last, "seed " + seed + ": a timer due at " + next.fireAt.TotalMilliseconds +
						" ms came out after one due at " + last.TotalMilliseconds + " ms");
					last = next.fireAt;
				}
			}
		}
	}
}
