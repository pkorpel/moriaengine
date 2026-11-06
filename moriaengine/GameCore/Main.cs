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
using System.Globalization;
using System.Reflection;
using System.Threading;
using SteamEngine.AuxServerPipe;
using SteamEngine.Common;
using SteamEngine.CompiledScripts;
using SteamEngine.CompiledScripts.ClassTemplates;
using SteamEngine.LScript;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using Timer = SteamEngine.Timers.Timer;

namespace SteamEngine {
	public static class MainClass {

		//public static Logger logger;

		//Lock ordering (#17): globalLock must always be acquired BEFORE any other lock that's
		//also held nested with it (e.g. MovementState.queue) - never the reverse. This matters
		//because globalLock is taken implicitly by every incoming packet handler
		//(GameCore/Networking/GameServer.cs passes it to AsyncCore as its per-connection lock),
		//so any code path that's reachable from packet handling and also takes a second lock
		//fixes that second lock's position as "nested inside globalLock" for every other caller
		//of that same lock to stay consistent with. See MovementState.Cycle for a concrete case
		//that used to invert this (queue taken first, globalLock nested inside) and was fixed to
		//match.
		internal static readonly object globalLock = new object();

		private static readonly CancellationTokenSource exitTokenSource = new CancellationTokenSource();

		public static CancellationToken ExitToken {
			get { return exitTokenSource.Token; }
		}

		//public static bool nativeConsole = false;

		//private static Queue nativeCommands=null;

		//internal static ConsConn winConsole;

		//method: winConsoleCommand
		//caled by the wincosole via reflection
		//public static void winConsoleCommand(string data) {
		//    if (RunLevelManager.RunLevel == RunLevel.Running || RunLevelManager.RunLevel == RunLevel.AwaitingRetry) {
		//        if (nativeConsole && nativeCommands!=null) {
		//            lock (nativeCommands.SyncRoot) {
		//                nativeCommands.Enqueue(data);
		//            }
		//        } else {
		//            Logger.WriteWarning("SteamEngine.MainClass.winConsoleCommand() method called, even if SteamEngine is not runnig under native console!");
		//        }
		//    }
		//}

		//method: WinStart
		//invoked instead of the Main() by the Winconsole if the server is run as console
		//as the argument it gets Methodinfo of a method that sends its string argument to the console
		//this also creates a ConsConn instance with an fake acc with admin plevel to represent the console
		//public static void WinStart(object consSend) {
		//    //this is run if the server is started by the WinConsole
		//    try {
		//        nativeCommands=new Queue();
		//        nativeConsole=true;
		//        winConsole=new ConsConn((StringToSend) consSend);
		//    } catch (Exception e) {
		//        Console.WriteLine (e);
		//        return;
		//    }
		//    SteamMain();
		//}

		public static void Main() {
			ParentProcessWatchdog.StartFromEnvironment(); //#785 - exit with the test run that started us, if any
			SteamMain();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void SteamMain() {
			//name the console window for better recognizability
			Console.Title = "SE Game Server - " + Assembly.GetExecutingAssembly().Location;

			//backstop for any exception that escapes a background thread's own try/catch
			//(e.g. the timer thread, a socket callback) - without this, such an exception
			//crashes the process with nothing written to the structured log file, only a
			//raw runtime dump to stderr.
			AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
				Logger.WriteFatal("Unhandled exception on thread '" + Thread.CurrentThread.Name +
					"' (IsTerminating=" + e.IsTerminating + ")",
					e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));
			};

			try {
				DynamicAssemblyResolver.Register();
				HighPerformanceTimer.Init();
				Tools.ExitBinDirectory();
				if (!Init()) {
					RunLevelManager.SetDead();
					return;
				}

				//Thread t = new Thread(Cycle);
				//t.IsBackground = true;
				//t.Name = "Main cycle thread";
				//t.Start();lk

				Console.WriteLine("Init done.");

				Thread t = new Thread(delegate () {
					Console.ReadLine();
					lock (globalLock) { //#1173 - Enter at the server's own console is a shutdown too, it saves as .exit does
						SaveAndExit();
					}
				});
				t.IsBackground = true;
				t.Start();

				exitTokenSource.Token.WaitHandle.WaitOne();

			} catch (ShowMessageAndExitException smaee) {
				Logger.WriteFatal(smaee);
				smaee.Show();
			} catch (ThreadAbortException) {
				RunLevelManager.SetShutdown();
				Logger.WriteFatal("Initialization process aborted");
			} catch (Exception globalexp) {
				Logger.WriteFatal(globalexp);
			} finally {
				Exit();
			}
		}

		private static bool Init() {

			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

			RunLevelManager.SetStartup();

			using (StopWatch.StartAndDisplay("Server Initialisation")) {
				AuxServerPipeClient.Init();
				Thread.Sleep(1000);//wait before namedpipe link to auxserver is initialised. 1 second should be enough

				Console.WriteLine($"Starting SteamEngine ({Globals.Version}, {Build.Type} build)" + " - " + Globals.ServerName);
				Console.WriteLine("https://sourceforge.net/projects/steamengine/");
				Console.WriteLine($"Running under {Environment.OSVersion}, Framework version: {Environment.Version}.");

				Globals.Init(); //reads .ini
				if (exitTokenSource.Token.IsCancellationRequested) {
					return false;
				}

				ClassTemplateParser.Init();

				if (!CompilerInvoker.CompileScripts(true)) {
					return false;
				}
				Tools.EnsureDirectory(Globals.MulPath, true);
				//ExportImport.Init();

				//if (!Globals.fastStartUp) {
				using (StopWatch.StartAndDisplay("Loading .idx and .mul files from " + LogStr.File(Globals.MulPath) + "...")) {
					TileData.Init();
					SoundMul.Init();
					MultiData.Init();
				}
				ScriptLoader.Load();

				TileData.GenerateMissingDefs();
				CharData.GenerateMissingDefs();
				ClassManager.InitStaticDefs(); //#1163 - before the world, so saved plugins find their defs

				WorldSaver.Load();
				//}

				//Server.Init();
				GameServer.Init();

				//Region.ResolveLoadedRegions();
				Map.Init();   //Sectors are created and items sorted on startup.
				ClassManager.InitScripts();
				ScriptedLocResxBridge.RegisterAll();
				PluginDef.Init();

				//Globals.UnPauseServerTime();
				RunLevelManager.UnsetStartup();
				RunLevelManager.SetRunning();

				Logger.WriteDebug("triggering @startup");
				Globals.Instance.TryTrigger(TriggerKey.startup, new ScriptArgs(true));

				//if (!Globals.fastStartUp) {
				Console.WriteLine("Checking to see if any scripts have changed.");
				Globals.Resync();
				//}

				Timer.StartTimerThread();

				return true;
			}
		}

		public static void CollectGarbage() {
			PoolBase.ClearAll();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			WeakRefDictionaryUtils.PurgeAll();
		}

		//this clears any static fields that could reference any game objects.
		//this is used for clearing a failed load attempt.
		public static void ClearWorld() {
			//Logger.WriteWarning("Clearing the world.");

			Thing.ClearAll();
			AbstractAccount.ClearAll();
			Globals.ClearAll();
			Map.ClearAllDynamicStuff();
			ObjectSaver.ClearJobs();
			OpenedContainers.ClearAll();
			Commands.ClearGMCommandsCache();
			AosToolTips.ClearCache();
			ItemOnGroundUpdater.ClearCache();

			Console.WriteLine("World cleared");
		}

		//returns false if nothing was changing, otherwise true
		internal static bool TryResyncCompiledScripts() {
			ClassTemplateParser.Resync();
			if (CompilerInvoker.FindIfSourcesHaveChanged()) {
				RecompileScripts();
				return true;
			}
			return false;
		}

		internal static void RecompileScripts() {
			using (StopWatch.StartAndDisplay("Recompiling scripts...")) {
				Commands.commandRunning = false;

				RunLevelManager.SetRecompiling();

				if (WorldSaver.Save()) {

					PacketSequences.BroadCast("Server is pausing for script recompiling...");

					Logger.WriteDebug("triggering @shutdown");
					Globals.Instance.TryTrigger(TriggerKey.shutdown, new ScriptArgs(false));
					OpenedContainers.SendRemoveAllOpenedContainersFromView();
					GameServer.BackupLinksToCharacters();

					ForgetAll();
					if (!LoadAll()) {
						//RunLevels.SetAwaitingRetry pauses everything except console connections & listening for console
						//connections & native commands, though SE doesn't care what they type, and whatever it is,
						//SE calls RetryRecompilingScripts. So, "retry", "recompile", "resync", "r", and "die you evil compiler"
						//(and whatever else they try) would all make SE attempt to recompile. Except for "exit", which, well,
						//exits.
						RunLevelManager.SetAwaitingRetry();
						PacketSequences.BroadCast("Script recompiling failed, pausing until it has been retried and succeeds.");
						/*while ((keepRunning) && commandsPaused) {
							System.Windows.Forms.Application.DoEvents();
							System.Threading.Thread.Sleep(20);
						}*/
					} else {    //If recompiling fails, we do not run this section. Instead, after a recompile succeeds,
								//the same code will be run in RetryRecompilingScripts.
						ScriptRecompilingSucceeded();
					}
				} else {//saving failed
					RunLevelManager.UnsetRecompiling();
				}
			}
		}

		//This is only for use by RetryRecompilingScripts and RecompileScripts, and exists only because
		//they run the same code, and it's always good to avoid unnecessary duplication, which could lead
		//to the separate versions getting out of sync when someone changes one but misses the other.
		private static void ScriptRecompilingSucceeded() {
			CollectGarbage();
			//RunLevelManager.SetPaused();	//Switch to paused until relinking & garbage collection have completed.
			GameServer.RemoveBackupLinks();

			RunLevelManager.UnsetRecompiling();

			PacketSequences.BroadCast("Script recompiling finished.");
		}

		internal static void RetryRecompilingScripts() {
			Sanity.IfTrueThrow(!RunLevelManager.IsAwaitingRetry, "!RunLevelManager.IsAwaitingRetry in RetryRecompilingScripts()");
			RunLevelManager.UnsetAwaitingRetry();

			ForgetAll();
			if (!LoadAll()) {

				RunLevelManager.SetAwaitingRetry();
				PacketSequences.BroadCast("Script recompiling failed, remaining paused.");
			} else {
				ScriptRecompilingSucceeded();
			}
		}

		private static void ForgetAll() {
			RunLevelManager.SetShutdown();

			ClearWorld();
			Timer.Clear();
			LocManager.ForgetInstancesFromAssembly(ClassManager.ScriptsAssembly);
			LocManager.ForgetInstancesOfType(typeof(ScriptedLocStringCollection));
			//CompilerInvoker.UnLoadScripts();//bye-bye to all stored assemblies and such that are not core-related
			ClassManager.ForgetScripts();//bye-bye to all storec types
			GeneratedCodeUtil.ForgetScripts();//bye-bye to scripted code generators
											  //TriggerGroup.UnloadAll();//bye-bye to all triggergroups and their triggers
			ScriptHolder.ForgetAllFunctions();//bye-bye to all scripted functions
			ThingDef.ForgetAll();//clear thingdef constructors etc.
			PluginDef.ForgetAll();//clear plugindef constructors etc.
								  //GroundTileType.ForgetScripts();			//unload all the Script objects which Script itself keeps (for getting scripts by name - used by Map for asking t_rock, etc, if it is the type of a specific map tileID).
			AbstractScript.ForgetAll();//all abstractscripts go bye-bye. This includes triggergroups, gumps, etc.
			Constant.ForgetAll();
			TestSuite.ForgetAll();
			ObjectSaver.ForgetScripts();
			DeepCopyFactory.ForgetScripts();
			ScriptLoader.ForgetScripts();//unload scripted loaders :)
			AbstractDef.ForgetScripts();//unload scripted defGetters
			AbstractSkillDef.ForgetAll();
			//Region.UnloadScripts();
			//ExportImport.UnloadScripts();
			Map.ForgetScripts();
			FieldValue.ForgetScripts();

			RunLevelManager.UnsetShutdown();
			Console.WriteLine("Scripts unloaded");
		}

		//reload everything, including recompile
		//this does basically the same as Init(), only less :)
		private static bool LoadAll() {
			RunLevelManager.SetStartup();
			ObjectSaver.ClearJobs();

			ClassTemplateParser.Init();
			if (!CompilerInvoker.CompileScripts(false)) {
				RunLevelManager.UnsetStartup();
				return false;
			}
			//ExportImport.Init();
			//if (!Globals.fastStartUp) {
			ScriptLoader.Load();
			TriggerGroup.ReAddGlobals();
			ClassManager.InitStaticDefs(); //#1163

			WorldSaver.Load();
			//}
			GameServer.ReLinkCharacters();
			Map.Init();
			ClassManager.InitScripts();
			PluginDef.Init();

			RunLevelManager.UnsetStartup();
			//Globals.UnPauseServerTime();
			//Region.ResolveLoadedRegions();


			Logger.WriteDebug("triggering @startup");
			Globals.Instance.TryTrigger(TriggerKey.startup, new ScriptArgs(false));
			return true;
		}

		//private static void HandleNativeCmds() {
		//    if (nativeCommands!=null && nativeCommands.Count>0) {
		//        string cmd;
		//        lock (nativeCommands.SyncRoot) {
		//            cmd=nativeCommands.Dequeue() as string;
		//        }
		//        if (cmd!=null) {
		//            winConsole.DoCommand(cmd);
		//        }
		//    }
		//}

		//private static void Cycle() {
		//    Console.WriteLine("Starting Main Loop");
		//    Timers.Timer.StartTimerThread();

		//    Thread.Sleep(5);

		//    while (!signalExit.WaitOne(5, false)) {
		//        if (RunLevelManager.IsRunning) {
		//            lock (globalLock) {
		//                SteamEngine.Packets.NetState.ProcessAll();
		//            }
		//        }
		//    }

		//    Console.WriteLine("Leaving Main Loop");
		//}

		internal static void CommandExit() {
			exitTokenSource.Cancel();
		}

		/// <summary>#1173 - a deliberate shutdown (.exit, the console's exit) saves the world first. Called with globalLock
		/// held, as a command is. A save that fails, or throws, only warns: the server goes down anyway, as the user chose.</summary>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static void SaveAndExit() {
			if (exitTokenSource.IsCancellationRequested) {
				return; //already going down
			}
			if (RunLevelManager.IsRunning) {
				bool saved = false;
				try {
					saved = Globals.TrySave();
				} catch (Exception e) {
					Logger.WriteError(e);
				}
				if (!saved) {
					Logger.WriteWarning("The shutdown save failed, the world is NOT saved. Shutting down anyway.");
				}
			} else {
				Logger.WriteWarning("Shutting down without a save: the server isn't running (" + RunLevelManager.RunLevel + ").");
			}
			GoDown();
		}

		/// <summary>#1173 - .exitnosave: down at once, no save. Called with globalLock held.</summary>
		internal static void ExitWithoutSave() {
			if (exitTokenSource.IsCancellationRequested) {
				return; //already going down
			}
			GoDown();
		}

		//#1173 - the run level drops from Running while globalLock is still held, so nothing queued behind this command (a
		//GM's .save, an autosave on the timer thread) saves a world the main thread is already taking apart in Exit()
		private static void GoDown() {
			RunLevelManager.SetShutdown();
			exitTokenSource.Cancel();
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		private static void Exit() {
			try {
				WorldSaver.FinishPendingWrite(); //#1209 - an autosave still being written finishes first
			} catch (Exception e) {
				Logger.WriteError(e); //the rest of the shutdown must run
			}
			RunLevelManager.SetShutdown();
			GameServer.Exit();
			AuxServerPipeClient.Exit();
			Console.WriteLine("Shutdown...");
			if (Globals.Instance != null) { //is null when first run (and writing steamengine.ini)
				Logger.WriteDebug("triggering @shutdown");
				Globals.Instance.TryTrigger(TriggerKey.shutdown, new ScriptArgs(true));
			}
			Timer.Clear();
			RunLevelManager.SetDead();
		}
	}
}
