using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamEngine.Common;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>#1187 - WakeSignal, how the timer thread is woken: a Set only while the consumer sleeps, and no work signalled
	/// while it goes to sleep is ever lost.</summary>
	public class WakeSignalTests {
		[Fact]
		public void SetsTheEventOnlyWhileTheConsumerSleeps() {
			var signal = new WakeSignal();
			signal.Signal(); //it starts asleep
			Assert.Equal(1, signal.SetCount);
			signal.Wait();
			for (int i = 0; i < 1000; i++) {
				signal.Signal(); //awake: nothing to set
			}
			Assert.Equal(1, signal.SetCount);

			signal.Sleep(() => false);
			Assert.False(Task.Run(signal.Wait).Wait(200), "Wait returned with nothing signalled");
			signal.Signal();
			Assert.Equal(2, signal.SetCount);
		}

		[Fact]
		public void SleepWithWorkWaitingDoesntSleep() {
			var signal = new WakeSignal();
			signal.Signal();
			signal.Wait();
			signal.Sleep(() => true); //work arrived before the reset: the next Wait returns at once
			Assert.True(Task.Run(signal.Wait).Wait(2000), "Wait blocked although Sleep found work");
		}

		/// <summary>A consumer that sleeps as soon as it has drained the queue, and a producer posting one item at a time, so
		/// nearly every item arrives while the consumer is going to sleep. Every item must be taken.</summary>
		[Fact]
		public void NoSignalIsLostWhileTheConsumerGoesToSleep() {
			const int Items = 20000;
			var signal = new WakeSignal();
			var queue = new Queue<int>();
			var taken = new SemaphoreSlim(0);
			bool HasWork() {
				lock (queue) {
					return queue.Count > 0;
				}
			}
			var consumer = new Thread(() => {
				while (true) {
					signal.Wait();
					while (true) {
						int item;
						lock (queue) {
							if (queue.Count == 0) {
								break;
							}
							item = queue.Dequeue();
						}
						if (item < 0) {
							return;
						}
						taken.Release();
					}
					signal.Sleep(HasWork);
				}
			}) { IsBackground = true };
			consumer.Start();

			int lost = 0;
			for (int i = 0; i < Items; i++) {
				lock (queue) {
					queue.Enqueue(i);
				}
				signal.Signal();
				if (!taken.Wait(2000)) {
					lost++;
					break;
				}
			}
			lock (queue) {
				queue.Enqueue(-1);
			}
			signal.Signal();
			consumer.Join(2000);
			Assert.True(lost == 0, "an item waited 2 s for a consumer that slept through its signal");
		}
	}
}
