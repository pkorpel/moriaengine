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

//I dont really think that the fastwalk prevention (using the stack etc.) is a good idea.
//the only thing we really need to check is if the walk request packets are not being sent too fast, 
//while this thingy checks only if they are well synced (kinda)

//using System;
//using System.Collections;
//using SteamEngine.Common;
//
//namespace SteamEngine {
//	public class FastWalkStack : Stack {
//		string AccName;
//		int corruptedScale=0;
//
//		/*public bool PassedOnTimer;
//		public byte OldDir = 20;
//		*/
//		public FastWalkStack(string _AccName): base() {
//			AccName = _AccName;
//		}
//
//		public void InitValues() {
//			for(int i = 0; i < 6; i++)
//				this.Push(Server.dice.Next(1, Int32.MaxValue));
//		}
//
//		public void Add() {
//			this.Push(Server.dice.Next(1, Int32.MaxValue));
//		}
//
//
//		public bool Check(int ReceivedKey) {
//			bool bad=false;
//			if(ReceivedKey == 0) {
//				Logger.WriteWarning("GameAccount "+LogStr.Ident(AccName)+" - Fastwalk: Detected");
//				bad=true;
//			}
//			if(this.Contains(ReceivedKey)) {
//				Logger.WriteDebug("GameAccount "+AccName+" - Fastwalk: OK ["+this.Count+"].");
//				bad=false;
//				if (corruptedScale>0) {
//					corruptedScale--;
//				}
//			} else {
//				corruptedScale+=2;
//				if (corruptedScale>10) {
//					bad=true;
//				}
//				Logger.WriteDebug("GameAccount "+AccName+" - Fastwalk: Corrupted Key");
//			}
//			
//			this.Pop();
//			this.Add();
//			return bad;
//		}
//	}
//}
