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

//using System;
//using System.Collections;
//using System.Timers;
//
//namespace SteamEngine.AI {
//	public class AI
//	{
//		private static int ElapsedTime = 0;
//
//		public static ArrayList MovingNPCs = new ArrayList();
//		public static System.Timers.Timer NPCMoveTimer = new System.Timers.Timer();
//
//		public static void Init() {
//			NPCMoveTimer.Interval = 25;
//			NPCMoveTimer.Elapsed += new ElapsedEventHandler(OnNPCMoveTimer);
//			NPCMoveTimer.Enabled = true;
//		}
//
//		public static void OnNPCMoveTimer(object source, ElapsedEventArgs e) {
//			ElapsedTime += 25;
//
////			foreach(Character cre in MovingNPCs) {
////				if(ElapsedTime % cre.Speed == 0) {
////					 call DoAction from brain script
////				}
////			}
//		}
//	}
//}
