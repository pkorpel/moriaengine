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
using SteamEngine.Common;

namespace SteamEngine {

	[Flags]
	public enum RunLevel {
		None = 0x00, 
		Startup = 0x01,
		Running = 0x02,
		Paused = 0x04,
		AwaitingRetry = 0x08,
		Shutdown = 0x10,
		Dead = 0x20,
		Recompiling = 0x40
	}

	//typical life cycle:
	//	None
	//	Paused //after ini is read
	//	Paused | Startup
	//	Running
	//	...
	//	Running | Paused //world save, or loading of single script file
	//	Running
	//	...
	//	Running | Startup //resync
	//	Running
	//	...
	//	Running | Paused | Recompiling //world save before recompiling
	//	Running | Paused | Recompiling | Shutdown //clearing world before recompiling
	//	Running | Paused | Recompiling | Startup //...successfuly recompile
	//	Running
	//	...
	//	Running | Paused | Recompiling | Startup //fails somewhere
	//	Running | Paused | Recompiling | Startup | IsAwaitingRetry //any key pressed
	//	Running | Paused | Recompiling | Startup //succeeds this time
	//	Running
	//	...
	//	Running | Paused | Shutdown
	//	Dead

	public static class RunLevelManager {
		private static RunLevel currentLevel = RunLevel.None;

		public static RunLevel RunLevel {
			get {
				return currentLevel;
			}
		}

		internal static void SetStartup() {
			ThrowIfDead();
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Startup;
			}
			Globals.PauseServerTime();
		}

		internal static void UnsetStartup() {
			Sanity.IfTrueThrow((currentLevel & RunLevel.Startup) != RunLevel.Startup,
				"currentLevel == " + currentLevel + " when UnsetStartup called");

#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Startup;
			}
			Globals.UnPauseServerTime();
		}

		internal static void SetRunning() {
			ThrowIfDead();
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Running;
			}
		}

		private static void ThrowIfDead() {
			Sanity.IfTrueThrow((currentLevel & RunLevel.Dead) == RunLevel.Dead,
				"currentLevel == " + currentLevel);
		}

		public static bool IsRunning {
			get {
				return currentLevel == RunLevel.Running;
			}
		}

		internal static void SetAwaitingRetry() {
			ThrowIfDead();
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.AwaitingRetry;
			}
			Globals.PauseServerTime();
		}

		public static bool IsAwaitingRetry {
			get {
				return (currentLevel & RunLevel.AwaitingRetry) == RunLevel.AwaitingRetry;
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UnsetAwaitingRetry() {
			Sanity.IfTrueThrow((currentLevel & RunLevel.AwaitingRetry) != RunLevel.AwaitingRetry,
				"currentLevel == " + currentLevel + " when UnsetAwaitingRetry called");
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.AwaitingRetry;
			}
			Globals.UnPauseServerTime();
		}

		internal static void SetRecompiling() {
			ThrowIfDead();
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Recompiling;
			}
			Globals.PauseServerTime();
		}

		[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		internal static void UnsetRecompiling() {
			Sanity.IfTrueThrow((currentLevel & RunLevel.Recompiling) != RunLevel.Recompiling,
				"currentLevel == " + currentLevel + " when UnsetRecompiling called");

#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Recompiling;
			}
			Globals.UnPauseServerTime();
		}

		internal static void SetPaused() {
			ThrowIfDead();
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Paused;
			}
		}

		internal static void UnsetPaused() {
			Sanity.IfTrueThrow((currentLevel & RunLevel.Paused) != RunLevel.Paused,
				"currentLevel == " + currentLevel + " when UnsetPaused called");
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Paused;
			}
		}

		internal static void SetShutdown() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel |= RunLevel.Shutdown;
			}
			Globals.PauseServerTime();
		}

		internal static void UnsetShutdown() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel &= ~RunLevel.Shutdown;
			}
			Globals.UnPauseServerTime();
		}

		internal static void SetDead() {
#if DEBUG
			using (new MagicObject())
#endif
 {
				currentLevel = RunLevel.Dead;
			}
			Globals.PauseServerTime();
		}

		private class MagicObject : IDisposable {
			private RunLevel runLevel = currentLevel;

			public void Dispose() {
				//Logger.WriteDebug("Changing state: '" + runLevel + "' -> '" + currentLevel + "'");
			}
		}
	}
}