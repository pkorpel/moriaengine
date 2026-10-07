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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Networking {

	//oddeleno od gamestate pro prehlednost
	public class MovementState {
		//These numbers are based on me running/walking/etc in game and recording the "Time between movement"
		//output of the movement info stuff. They're slightly higher than the highest values I saw,
		//which means that like this they should slow all clients to the same rate, so perhaps different
		//clients with different graphics-drawing speeds will actually move at the same rates instead of
		//at different ones. Or maybe not, since I have a pretty good graphics card. (SL)
		private readonly long RunStepTime = HighPerformanceTimer.SecondsToTicks(0.18);
		//private readonly long WalkStepTime = HighPerformanceTimer.SecondsToTicks(0.35);
		private readonly long RidingRunStepTime = HighPerformanceTimer.SecondsToTicks(0.09);
		//private readonly long RidingWalkStepTime = HighPerformanceTimer.SecondsToTicks(0.18);

		private static LinkedList<MovementState> queue = new LinkedList<MovementState>();
		[SuppressMessage("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
		private static Thread thread = InitThread();
		private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);

		private readonly LinkedListNode<MovementState> listNode;

		private readonly SimpleQueue<MoveRequest> moveRequests = new SimpleQueue<MoveRequest>();
		private long lastStepReserve;
		private long secondLastStepReserve;
		private long thirdLastStepReserve;
		//private long lastMovementTime;
		private long nextMovementTime;

		private GameState gameState;

		private struct MoveRequest {
			internal Direction direction;
			internal bool running;
			internal byte sequence;

			internal MoveRequest(Direction direction, bool running, byte sequence) {
				this.direction = direction;
				this.running = running;
				this.sequence = sequence;
			}
		}

		private static Thread InitThread() {
			Thread thread = new Thread(Cycle);
			thread.IsBackground = true;
			thread.Start();
			return thread;
		}

		static void Cycle() {
			while (manualResetEvent.WaitOne()) {
				lock (queue) {
					LinkedListNode<MovementState> currentNode = queue.First;
					while (currentNode != null) {
						LinkedListNode<MovementState> nextNode = currentNode.Next;

						MovementState ms = currentNode.Value;
						if (ms.moveRequests.Count > 0) {
							lock (MainClass.globalLock) {
								ms.ProcessMovement(ms.moveRequests.Dequeue());
							}
							if (ms.moveRequests.Count == 0) {
								queue.Remove(currentNode);
							}
						} else {
							queue.Remove(currentNode);
						}

						currentNode = nextNode;
					}

					if (queue.Count == 0) {
						manualResetEvent.Reset();
					}
				}

				Thread.Sleep(5);
			}
		}

		internal MovementState(GameState gameState) {
			this.listNode = new LinkedListNode<MovementState>(this);

			this.gameState = gameState;
		}

		//called from incomingpacket.Handle, so we're also under lock(globallock)
		internal void MovementRequest(Direction direction, bool running, byte sequence) {
			lock (queue) {
				int requestCount = this.moveRequests.Count;
				if (requestCount == 0) {
					this.ProcessMovement(new MoveRequest(direction, running, sequence));
				} else {
					this.moveRequests.Enqueue(new MoveRequest(direction, running, sequence));
				}
				if ((requestCount == 0) && (this.moveRequests.Count == 1)) {
					queue.AddFirst(this.listNode);
					manualResetEvent.Set();
				}
			}
		}

		private void ProcessMovement(MoveRequest mr) {
			AbstractCharacter ch = this.gameState.Character;
			if (ch == null) {
				this.moveRequests.Clear();
				return;
			}

			if (mr.direction != ch.Direction) {//no speedcheck if we're just changing direction
				if (this.moveRequests.Count == 0) {
					this.Movement(mr.direction, mr.running, mr.sequence);
				}
				return;
			}

			if ((this.CanMoveAgain()) || (this.gameState.Account.PLevel >= Globals.PlevelOfGM)) {
				this.Movement(mr.direction, mr.running, mr.sequence);
			} else {
				this.moveRequests.Enqueue(mr);
			}
		}

		private bool CanMoveAgain() {
			long currentTime = HighPerformanceTimer.TickCount;
			long diff = currentTime - this.nextMovementTime;

			//Logger.WriteInfo(MovementTracingOn, "Time between movement = "+HighPerformanceTimer.TicksToSeconds(currentTime-lastMovementTime));

			long reserves = this.lastStepReserve + this.secondLastStepReserve + this.thirdLastStepReserve;

			if (diff + reserves >= 0) {
				//Logger.WriteInfo(MovementTracingOn, "Time later than allowed = "+HighPerformanceTimer.TicksToSeconds(diff + reserves));

				if (this.gameState.CharacterNotNull.Flag_Riding) {
					this.nextMovementTime = (currentTime + this.RidingRunStepTime);
				} else {
					this.nextMovementTime = (currentTime + this.RunStepTime);
				}
				this.thirdLastStepReserve = this.secondLastStepReserve;
				this.secondLastStepReserve = this.lastStepReserve;
				this.lastStepReserve = Math.Max(diff, 0);
				//lastMovementTime = currentTime;//...because sometimes the client tends to send more steps at once (or it looks like that), but it still isn't speedhacking
				return true;
			}
			////Logger.WriteInfo(MovementTracingOn, "Delaying movement, the packet is "+seconds+" seconds early.");
			//if (MovementTracingOn) { 
			//    //we need not spam the console with warnings. Players simply can't speedhack, end of story.
			//    Logger.WriteWarning("The client "+LogStr.Ident(this)+" is moving too fast. The packet came "+
			//        LogStr.Number(HighPerformanceTimer.TicksToSeconds(-(diff + reserves)))+" s earlier than allowed.");
			//}
			return false;
		}

		private void Movement(Direction dir, bool running, byte sequence) {
			AbstractCharacter ch = this.gameState.CharacterNotNull;

			//Logger.WriteInfo(MovementTracingOn, "Moving.");
			bool success = ch.WalkRunOrFly(dir, running, true);	//true = we don't want to get a 0x77 for ourself, our client knows we're moving if it gets the verify move packet.
			if (success) {
				CharacterMoveAcknowledgeOutPacket packet = Pool<CharacterMoveAcknowledgeOutPacket>.Acquire();
				packet.Prepare(sequence, ch.GetHighlightColorFor(ch));
				this.gameState.Conn.SendSinglePacket(packet);
				CharSyncQueue.ProcessChar(ch);//I think this is really needed. We can't wait to the end of the cycle, because movement 
				//should be as much synced between clients as possible
			} else {

				CharMoveRejectionOutPacket packet = Pool<CharMoveRejectionOutPacket>.Acquire();
				packet.Prepare(sequence, ch);
				this.gameState.Conn.SendSinglePacket(packet);

				this.ResetMovementSequence();
			}
		}

		internal void ResetMovementSequence() {
			this.moveRequests.Clear();
		}
	}
}