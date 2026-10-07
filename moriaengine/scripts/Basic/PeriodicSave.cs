//functionality of this script has been moved to PeriodicSave.scp
//it stays here as a commented example

//using System;
//using System.Collections;
//using System.Reflection;
//using SteamEngine;
//using SteamEngine.Timers;
//
//namespace SteamEngine.CompiledScripts {
//	//If something looks difficult, make suggestions for ways to make it easier!
//	public class E_Periodicsave_Global : CompiledTriggerGroup {	//Our script must extend 'Script'
//		int minutes;			//An INI variable
//		TimerKey timerKey = TimerKey.Get("autoSaveTimer");
//		//TimerKey are handles for timers. More efficient than passing
//		//a string timer name every time we do HasTimer, RemoveTimer, etc.
//		
//		TimeSpan interval;
//		MethodInfo method = MemberWrapper.GetWrapperFor(
//		typeof(Globals).GetMethod("Save", BindingFlags.Static|BindingFlags.Public));
//		
//		//base("something") means this is a triggerGroup named "something", and can have triggers in  it.
//		//Without that, you can't write on_ methods, they're illegal if they aren't in a triggerGroup.
//		//Note that if you need a reference to the triggerGroup, it is 'this.triggerGroup'.
//		public E_Periodicsave_Global() {
//			//Specify a section for the INI, and specify name, default values, and comments for keys in that section
//			IniDataSection saving=IniSection("saving");
//			saving.Comment("This is a simple script that saves your world every x minutes.");
//			
//			//IniEntry returns the value of the setting. If it's not in the INI, it returns the default value.
//			minutes=(int) 		saving.IniEntry("minutes", 	60, 	"How many minutes between saves");
//			IniDone();	//We're done with the INI. If the file didn't exist, then this will trigger writing the INI with
//						//the default values, our comments, etc.
//			interval = new TimeSpan(0, minutes, 0);
//		}
//		
//		/*
//			Method: on_startup
//			This is a script for the "startup" trigger on our triggerGroup (e_periodicsave_global)
//			It gets run whenever @startup is triggered on something that our triggerGroup is on.
//			In other words, it gets run when the server finishes starting up.
//			(Capitalization of trigger/function names is irrelevant.)
//			
//			Parameters:
//				globals - The TagHolder the trigger was executed on, which is always 'globals' for this script,
//					so it is named that for readability.
//		*/
//		
//		public void on_startup(TagHolder globals) {
//			if (globals.HasTimer(timerKey)) {
//				globals.RemoveTimer(timerKey);
//			}
//			StartTimer();
//		}
//		
//		private void StartTimer() {
//			Timer timer = new MethodTimer(Globals.Instance, timerKey, interval, method);
//			timer.Enqueue();
//		}
//		
//		/*
//			Method: on_afterSave
//			This is a script for the "afterSave" trigger on our triggerGroup (e_periodicsave_global)
//			It gets run whenever @afterSave is triggered on something that our triggerGroup is on.
//			In other words, it gets run when the server finishes saving!
//			(Capitalization of trigger/function names is irrelevant.)
//			
//			Parameters:
//				globals - The TagHolder the trigger was executed on, which is always 'globals' for this script,
//					so it is named that for readability.
//		*/
//		public void on_afterSave(TagHolder globals) {
//			StartTimer();
//		}
//	}
//}