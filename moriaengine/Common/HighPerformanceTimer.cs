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

//This basically uses Windows' high performance timer via P/Invoke, and provides methods for 
//converting to and from milliseconds, seconds, and ticks.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace SteamEngine.Common {
	public static class HighPerformanceTimer {

		private static class NativeMethods {
			[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"),
			DllImport("kernel32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool QueryPerformanceCounter(out long counter);

			[SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "0#"),
			DllImport("kernel32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			internal static extern bool QueryPerformanceFrequency(out long frequency);
		}
		

		private static long frequency;
		private static double dFrequency;
		private static double dmFrequency;

		private static double timeSpanTicksFrequency;
		private static bool fallback;

		static HighPerformanceTimer() {
			try {
				bool success = NativeMethods.QueryPerformanceFrequency(out frequency);
				if (success) {
					dFrequency = frequency;
					dmFrequency = dFrequency / 1000.0;
					timeSpanTicksFrequency = frequency / 10000000.0;
					return;
				} 
			} catch { }

			fallback=true;
			Logger.WriteWarning("Unable to access high performance timer. Using a less accurate timing mechanism instead.");
		}

		public static void Init() {

		}

		public static long TickCount {
			get {
				if (fallback) {
				    return Environment.TickCount;
				}
				long count;
				bool success = NativeMethods.QueryPerformanceCounter(out count);
				if (success) {
					return count;
				}
				throw new FatalException("Unable to access high performance timer.");
				//Debug.WriteLine("Attempt to read the high performance timer failed.");
				//Sanity.IfTrueSay(true, "Attempt to read the high performance timer failed.");
				//return Environment.TickCount;
			}
		}

		public static double TicksToSeconds(long count)
		{
			if (fallback) {
			    return count/1000.0;
			}
			return count / dFrequency;
		}
		
		public static long TicksToMilliseconds(long count)
		{
			if (fallback) {
			    return count;
			}
			return (long) (count / dmFrequency);
		}
		
		public static double TicksToDMilliseconds(long count)
		{
			if (fallback) {
			    return count;
			}
			return (count / dmFrequency);
		}

		//TimeSpan ticks have 100 nanoseconds, that means frequency 10 000 000 ticks per second
		//our ticks probably have the same
		public static TimeSpan TicksToTimeSpan(long count)
		{
			if (fallback) {
			    return new TimeSpan(count);
			}
			return new TimeSpan((long) (count / timeSpanTicksFrequency));
		}
		
		public static long TimeSpanToTicks(TimeSpan span)
		{
			if (fallback) {
			    return span.Ticks;
			}
			return (long) (span.Ticks * timeSpanTicksFrequency);
		}

		public static long SecondsToTicks(double count)
		{
			if (fallback) {
			    return (long) (count*1000);
			}
			return (long) (count * dFrequency);
		}
		public static long MillisecondsToTicks(long count)
		{
			if (fallback) {
			    return count;
			}
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