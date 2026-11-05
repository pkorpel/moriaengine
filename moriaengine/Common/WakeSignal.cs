using System;
using System.Threading;

namespace SteamEngine.Common {
	/// <summary>#1187 - wakes a consumer thread that sleeps only when it's out of work, with a system call only when it really
	/// sleeps. Producers: make the work visible (queue it), then <see cref="Signal"/>. The consumer: <see cref="Wait"/> before
	/// each round, and <see cref="Sleep"/> when a round leaves it with nothing to do.
	/// A ManualResetEvent.Set on every change, the timer thread's old way, is a syscall each time on Linux, although the
	/// consumer is nearly always awake.</summary>
	public sealed class WakeSignal {
		private readonly ManualResetEvent wakeEvent = new ManualResetEvent(false);
		private volatile bool asleep = true; //from Sleep until the next Wait returns; starts asleep, in its first Wait
		private long sets;

		/// <summary>Call after the work is visible to the consumer. Sets the event only while the consumer sleeps.</summary>
		public void Signal() {
			if (this.asleep) {
				Interlocked.Increment(ref this.sets);
				this.wakeEvent.Set();
			}
		}

		/// <summary>The consumer: blocks while asleep, returns at once otherwise.</summary>
		public void Wait() {
			this.wakeEvent.WaitOne();
			this.asleep = false;
		}

		/// <summary>The consumer, out of work: it sleeps from its next <see cref="Wait"/> unless hasWork finds some. hasWork runs
		/// after the event is reset, so work signalled at any point is never lost: either hasWork sees it, or its producer
		/// reads asleep as true afterwards (hasWork must read the work under the same lock its producers write it under, or
		/// with a full fence) and sets the event again. Checking first and resetting after would wipe a Set made between the two.</summary>
		public void Sleep(Func<bool> hasWork) {
			this.asleep = true;
			this.wakeEvent.Reset();
			if (hasWork()) {
				this.wakeEvent.Set();
			}
		}

		/// <summary>How many times Signal really set the event.</summary>
		public long SetCount {
			get { return Interlocked.Read(ref this.sets); }
		}
	}
}
