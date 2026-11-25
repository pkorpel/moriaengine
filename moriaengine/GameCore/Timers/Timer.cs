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
		private static SimpleQueue<Timer> changes = new SimpleQueue<Timer>();

		private static TimerPriorityQueue priorityQueue = new TimerPriorityQueue();
		private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

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
			while (manualResetEvent.WaitOne()) {
				Cycle();

				if ((priorityQueue.Count == 0) && (changes.Count == 0)) {
					manualResetEvent.Reset();
				}

				Thread.Sleep(5);
			}
		}

		private static void Cycle() {
			ProcessChanges();
			ProcessTimingOut();
		}

		private static void ProcessChanges() {
			while (changes.Count > 0) {
				Timer timer = changes.Dequeue();
				lock (timer) {
					timer.isInChangesQueue = false;

					priorityQueue.Remove(timer);
					if (!timer.isDeleted && timer.fireAt >= TimeSpan.Zero) {
						priorityQueue.Enqueue(timer);
						manualResetEvent.Set();
					}
				}
			}
		}

		private static Timer currentTimer;
		public static Timer CurrentTimer {
			get { return currentTimer; }
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void ProcessTimingOut() {
			TimeSpan now = Globals.TimeAsSpan;
			while (priorityQueue.Count > 0) {
				Timer timer = priorityQueue.Peek();
				//Console.WriteLine("TimingOut timer "+timer);

				if (timer.fireAt < now) {
					timer = priorityQueue.Dequeue();
					TimeSpan fireAt = timer.fireAt;
					if ((!timer.isInChangesQueue) && (!timer.isDeleted)) {
						lock (timer) {
							if (timer.period >= TimeSpan.Zero) {
								timer.fireAt = fireAt + timer.period;
								changes.Enqueue(timer);
								manualResetEvent.Set();
								timer.isInChangesQueue = true;
							}
							currentTimer = timer;
							try {
								lock (MainClass.globalLock) {
									timer.OnTimeout();
								}
							} catch (FatalException) {
								throw;
							} catch (Exception e) {
								Logger.WriteError(e);
							}
						}
						currentTimer = null;
					}
				} else {
					return;
				}
			}
		}

		public static void Clear() {
			priorityQueue.Clear();
			changes.Clear();
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
					changes.Enqueue(this);
					manualResetEvent.Set();
					this.isInChangesQueue = true;
				}
			}
		}

		public virtual void Delete() {
			lock (this) {
				this.isDeleted = true;
				if (!this.isInChangesQueue) {
					changes.Enqueue(this);
					manualResetEvent.Set();
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
			Logger.WriteDebug("Loaded " + priorityQueue.Count + " timers.");

			ProcessChanges();
			changes = new SimpleQueue<Timer>();//it could have been unnecessary big...
		}
		#endregion save/load
	}
}
