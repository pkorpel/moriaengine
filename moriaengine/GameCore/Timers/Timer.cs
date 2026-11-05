/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SteamEngine.Common;
using SteamEngine.Persistence;

namespace SteamEngine.Timers {

	public abstract class Timer : IDeletable {
		//written by whoever changes a timer, read by the timer thread. #960 - changesLock is the only thing guarding it, as each
		//side otherwise holds just its own timer: a burst grew the array under the reader, which got null and killed the thread.
		//Taken last and held for the one queue operation, so nothing waits for another lock inside it (#17, #798).
		private static SimpleQueue<Timer> changes = new SimpleQueue<Timer>();
		private static readonly object changesLock = new object();

		private static TimerPriorityQueue priorityQueue = new TimerPriorityQueue();
		//#1187 - sets its event only while the timer thread sleeps (no timer scheduled at all); a Set on every change was a
		//syscall on every re-arm, though the thread is nearly always awake, polling every 5 ms
		private static readonly WakeSignal wakeSignal = new WakeSignal();
		private static readonly Func<bool> hasChanges = HasChanges;

		private bool isDeleted;
		private bool isInChangesQueue;

		internal int index = -1; //index in the Priorityqueue. do not touch!
		internal TimeSpan fireAt = negativeOneSecond;//internal (instead of private) because of the priorityqueue. do not touch!
		private TimeSpan period = negativeOneSecond;//if this is > -1, it will be repeated.

		private static Thread timerThread;

		internal static void StartTimerThread() {
			timerThread = new Thread(TimerThread);
			timerThread.IsBackground = true;
			timerThread.Start();
		}

		private static void TimerThread() {
			while (true) {
				wakeSignal.Wait();
				bool cutShort;
				try {
					cutShort = Cycle();
				} catch (Exception e) {
					//this dedicated thread has no other protection - an exception escaping
					//here (e.g. a rethrown FatalException from ProcessTimingOut) kills it
					//silently otherwise, taking down every timer-driven system (decay,
					//regen, skill-gain delays, NPC AI...) with nothing in the log to explain
					//why. Log loudly, then let it keep propagating - same end behavior,
					//just no longer silent.
					Logger.WriteFatal("Timer thread dying, CurrentTimer=" + currentTimer, e);
					throw;
				}

				if (cutShort) {
					continue; //#1194 - more is due: the next round takes in the changes first, then goes on, no pause
				}
				if (priorityQueue.Count == 0) {
					wakeSignal.Sleep(hasChanges); //it re-checks the changes after its reset: checking first lost a change made between
				}

				Thread.Sleep(5);
			}
		}

		/// <summary>One round; true if it stopped with timers still due (see ProcessTimingOut).</summary>
		private static bool Cycle() {
			ProcessChanges();
			return ProcessTimingOut();
		}

		private static bool HasChanges() {
			lock (changesLock) {
				return changes.Count > 0;
			}
		}

		private static void QueueChange(Timer timer) {
			lock (changesLock) {
				changes.Enqueue(timer);
			}
		}

		//#1188 - Clear() with the timer thread running bumps this instead of touching priorityQueue, which only that thread may
		//touch (Clear from the recompiling thread nulled the heap under its Peek, and the thread died). Its own heap belongs to
		//seenGeneration, which only it reads and writes once it runs.
		private static volatile int generation;
		private static int seenGeneration;

		/// <summary>The next change, and whether a Clear came since the last one: then the heap goes before this change goes in,
		/// as the changes queued before that Clear were dropped with it.</summary>
		private static Timer NextChange(out bool cleared) {
			lock (changesLock) {
				cleared = (generation != seenGeneration);
				seenGeneration = generation;
				return (changes.Count > 0) ? changes.Dequeue() : null;
			}
		}

		private static void ProcessChanges() {
			while (true) {
				Timer timer = NextChange(out bool cleared);
				if (cleared) {
					priorityQueue.Clear();
				}
				if (timer == null) {
					return;
				}
				lock (timer) {
					timer.isInChangesQueue = false;

					priorityQueue.Remove(timer);
					if (!timer.isDeleted && timer.fireAt >= TimeSpan.Zero) {
						priorityQueue.Enqueue(timer);
					}
				}
			}
		}

		private static Timer currentTimer;
		public static Timer CurrentTimer {
			get { return currentTimer; }
		}

		//#1194 - a round of due timers stops after this long and lets the next round take in new and re-armed timers first.
		//Before, it ran until none was due: overloaded, it never ended, and a new timer (a skill stroke, a spell) or a re-armed
		//one waited in the change queue for the whole backlog, so the world froze in multi-second rounds instead of slowing down.
		private static readonly long roundBudgetTicks = System.Diagnostics.Stopwatch.Frequency / 50; //20 ms

		/// <summary>Runs the due timers; true if it stopped at the round's budget with more still due.</summary>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static bool ProcessTimingOut() {
			TimeSpan now = Globals.TimeAsSpan;
			long roundStart = System.Diagnostics.Stopwatch.GetTimestamp();
			while (priorityQueue.Count > 0) {
				Timer timer = priorityQueue.Peek();
				//Console.WriteLine("TimingOut timer "+timer);

				if (timer.fireAt < now) {
					timer = priorityQueue.Dequeue();
					TimeSpan fireAt = timer.fireAt;
					if ((!timer.isInChangesQueue) && (!timer.isDeleted)) {
						//#798 - globalLock first, then the timer (lock order #17, see Main.cs). This used to
						//lock the timer and wait for globalLock inside it, while a packet handler holding
						//globalLock deleted the timer's owner and waited for the same timer in Timer.Delete -
						//a deadlock that hung the server whenever the timer came due at that moment.
						long perfW0 = PerfCounters.Now(); //#1179
						lock (MainClass.globalLock) {
							long perfT0 = PerfCounters.Acquired(perfW0);
							try {
								if (generation != seenGeneration) {
									//#1188 - the world was cleared (a recompile) while this waited for globalLock: the timer is the
									//old world's, and things aren't deleted by a clear, only forgotten. Fired, it would run for that
									//world, and a periodic one would keep doing so from the new heap. The next round clears the rest.
									return true;
								}
								lock (timer) {
									//re-check: it may have been deleted or rescheduled while this waited for globalLock
									if ((!timer.isInChangesQueue) && (!timer.isDeleted)) {
										if (timer.period >= TimeSpan.Zero) {
											timer.fireAt = fireAt + timer.period;
											QueueChange(timer); //the timer thread itself: the next round takes it, no signal needed
											timer.isInChangesQueue = true;
										}
										currentTimer = timer;
										try {
											timer.OnTimeout();
										} catch (FatalException) {
											throw;
										} catch (Exception e) {
											Logger.WriteError(e);
										}
									}
								}
							} finally {
								PerfCounters.Held(timer.GetType(), perfT0);
							}
						}
						currentTimer = null;
						if (System.Diagnostics.Stopwatch.GetTimestamp() - roundStart >= roundBudgetTicks) {
							return (priorityQueue.Count > 0) && (priorityQueue.Peek().fireAt < now);
						}
					}
				} else {
					return false;
				}
			}
			return false;
		}

		public static void Clear() {
			lock (changesLock) {
				changes.Clear();
				if (timerThread != null) {
					generation++; //#1188 - the timer thread clears its own heap, see NextChange
					return;
				}
			}
			priorityQueue.Clear(); //no timer thread yet: nothing else touches it
		}

		protected abstract void OnTimeout();

		/// <summary>The time interval between invocations, using TimeSpan values to measure time intervals.</summary>
		/// <remarks>Specify negative one (-1) second (or any other negative number) to disable periodic signaling.</remarks>
		public TimeSpan PeriodSpan {
			get {
				TimeSpan p = this.period;
				if (p < TimeSpan.Zero) {
					return negativeOneSecond;
				}
				return p;
			}
			set {
				if (value < TimeSpan.Zero) {
					this.period = negativeOneSecond;
				} else {
					this.period = value;
					if (this.fireAt == negativeOneSecond) { //if we aren't ticking, we start.
						this.PrivateSetFireAt(Globals.TimeAsSpan + this.period);
					}
				}
			}
		}

		/// <summary>The time interval between invocations, in seconds. </summary>
		/// <remarks>Specify negative one (-1) second (or any other negative TimeSpan) to disable periodic signaling.</remarks>
		public double PeriodInSeconds {
			get {
				TimeSpan p = this.period;
				if (p < TimeSpan.Zero) {
					return -1;
				}
				return p.TotalSeconds;
			}
			set {
				if (value < 0) {
					this.period = negativeOneSecond;
				} else {
					this.period = TimeSpan.FromSeconds(value);
					if (this.fireAt == negativeOneSecond) { //if we aren't ticking, we start.
						this.PrivateSetFireAt(Globals.TimeAsSpan + this.period);
					}
				}
			}
		}

		/// <summary>The amount of time to delay before the first invoking, in seconds.</summary>
		/// <remarks>Specify negative one (-1) second (or any other negative number) to prevent the timer from starting (i.e. to pause it). Specify 0 to start the timer immediately.</remarks>
		public double DueInSeconds {
			get {
				return this.DueInSpan.TotalSeconds;
			}
			set {
				if (value < 0) {
					this.PrivateSetFireAt(negativeOneSecond);
				} else {
					this.PrivateSetFireAt(Globals.TimeAsSpan + TimeSpan.FromSeconds(value));
				}
			}
		}

		/// <summary>The amount of time to delay before the first invoking, using TimeSpan values to measure time intervals.</summary>
		/// <remarks>Specify negative one (-1) second (or any other negative TimeSpan) to prevent the timer from starting (i.e. to pause it). Specify TimeSpan.Zero to start the timer immediately.</remarks>
		public TimeSpan DueInSpan {
			get
			{
				if (this.fireAt == negativeOneSecond) {
					return negativeOneSecond;
				}
				return this.fireAt - Globals.TimeAsSpan;
			}
			set {
				if (value < TimeSpan.Zero) {
					this.PrivateSetFireAt(negativeOneSecond);
				} else {
					this.PrivateSetFireAt(Globals.TimeAsSpan + value);
				}
			}
		}

		private void PrivateSetFireAt(TimeSpan value) {
			this.ThrowIfDeleted();
			lock (this) {
				this.fireAt = value;
				if (!this.isInChangesQueue) {
					QueueChange(this);
					wakeSignal.Signal();
					this.isInChangesQueue = true;
				}
			}
		}

		public virtual void Delete() {
			lock (this) {
				this.isDeleted = true;
				if (!this.isInChangesQueue) {
					QueueChange(this);
					wakeSignal.Signal();
					this.isInChangesQueue = true;
				}
			}
		}

		public bool IsDeleted {
			get {
				return this.isDeleted;
			}
		}

		protected void ThrowIfDeleted() {
			if (this.isDeleted) {
				throw new DeletedException("You can not manipulate a deleted timer (" + this + ")");
			}
		}

		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		public static readonly TimeSpan negativeOneSecond = TimeSpan.FromSeconds(-1);

		#region save/load
		internal static void StartingLoading() {
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods"), Save]
		public virtual void Save(SaveStream output) {
			if (this.fireAt != negativeOneSecond) {
				output.WriteValue("fireAt", this.fireAt.Ticks);
			}
			if (this.period != negativeOneSecond) {
				output.WriteValue("period", this.period.Ticks);
			}
		}

		[LoadLine]
		public virtual void LoadLine(string filename, int line, string name, string value) {
			switch (name) {
				case "fireat":
					this.PrivateSetFireAt(TimeSpan.FromTicks(ConvertTools.ParseInt64(value)));
					break;
				case "period":
					this.period = TimeSpan.FromTicks(ConvertTools.ParseInt64(value));
					break;
			}
		}

		internal static void LoadingFinished() {
			if (timerThread != null) {
				//#1187 - a recompile's reload, with the timer thread running: the loaded timers wait in changes for its next round
				//(taking them into priorityQueue here raced that thread and its sleep, and the swap below could drop one queued meanwhile)
				Logger.WriteDebug("Loaded timers, the timer thread takes them in.");
				wakeSignal.Signal();
				return;
			}
			Logger.WriteDebug("Loaded " + priorityQueue.Count + " timers.");

			ProcessChanges();
			lock (changesLock) {
				changes = new SimpleQueue<Timer>();//it could have been unnecessary big...
			}
		}
		#endregion save/load
	}
}
