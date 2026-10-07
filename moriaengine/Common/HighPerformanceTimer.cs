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

//Uses System.Diagnostics.Stopwatch (cross-platform, backed by the OS's own high
//resolution timer on every platform .NET runs on) instead of P/Invoking Windows'
//QueryPerformanceCounter/Frequency directly, and provides methods for converting
//to and from milliseconds, seconds, and ticks.

using System;
using System.Diagnostics;

namespace SteamEngine.Common {
	public static class HighPerformanceTimer {
		private static readonly double dFrequency = Stopwatch.Frequency;
		private static readonly double dmFrequency = dFrequency / 1000.0;
		private static readonly double timeSpanTicksFrequency = Stopwatch.Frequency / 10000000.0;

		public static void Init() {

		}

		public static long TickCount {
			get {
				return Stopwatch.GetTimestamp();
			}
		}

		public static double TicksToSeconds(long count)
		{
			return count / dFrequency;
		}

		public static long TicksToMilliseconds(long count)
		{
			return (long) (count / dmFrequency);
		}

		public static double TicksToDMilliseconds(long count)
		{
			return (count / dmFrequency);
		}

		//TimeSpan ticks have 100 nanoseconds, that means frequency 10 000 000 ticks per second
		//our ticks probably have the same
		public static TimeSpan TicksToTimeSpan(long count)
		{
			return new TimeSpan((long) (count / timeSpanTicksFrequency));
		}

		public static long TimeSpanToTicks(TimeSpan span)
		{
			return (long) (span.Ticks * timeSpanTicksFrequency);
		}

		public static long SecondsToTicks(double count)
		{
			return (long) (count * dFrequency);
		}
		public static long MillisecondsToTicks(long count)
		{
			return (long) (count * dmFrequency);	//(count/1000.0)*dFrequency);
		}
	}

	public sealed class StopWatch : IDisposable {
		long ticksOnStart = HighPerformanceTimer.TickCount;
		private bool disposed;

		private StopWatch() {
		}

		public static StopWatch Start() {
			Logger.indentation += "\t";
			return new StopWatch();
		}

		public static StopWatch StartAndDisplay(object message) {
			Logger.StaticWriteLine(message);
			Logger.indentation += "\t";
			return new StopWatch();
		}

		public void Dispose() {
			if (!this.disposed) {
				long ticksOnEnd = HighPerformanceTimer.TickCount;
				long diff = ticksOnEnd - this.ticksOnStart;
				Logger.indentation = Logger.indentation.Substring(0, Logger.indentation.Length - 1);
				Logger.StaticWriteLine("...took " + HighPerformanceTimer.TicksToTimeSpan(diff));
				this.disposed = true;
			}
		}
	}
}
