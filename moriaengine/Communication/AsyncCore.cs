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
using System.Collections.Concurrent;
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication {
	public abstract class AsyncCore<TConnection, TState, TEndPoint> : Disposable//,
		//IAsyncCore<TConnection, TState, TEndPoint>
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {



		private AutoResetEvent outgoingPacketEnqueued = new AutoResetEvent(false);
		private ManualResetEvent outgoingPacketsSentEvent = new ManualResetEvent(true);


		private BlockingCollection<OutgoingMessage> outgoingPackets;

		internal readonly IProtocol<TConnection, TState, TEndPoint> protocol;

		private object lockObject;
		private CancellationToken exitToken;

		public AsyncCore(IProtocol<TConnection, TState, TEndPoint> protocol, object lockObject, CancellationToken exitToken) {
			this.protocol = protocol;
			this.lockObject = lockObject;
			this.exitToken = exitToken;

			this.outgoingPackets = new BlockingCollection<OutgoingMessage>(new ConcurrentQueue<OutgoingMessage>());

			string threadsName = Tools.TypeToString(this.GetType());
			this.CreateAndStartWorkerThread(threadsName + "_Worker");
		}

		private Thread CreateAndStartWorkerThread(string name) {
			Thread t = new Thread(this.WorkerThreadMethod);
			t.Name = name;
			t.IsBackground = true;
			t.Start();
			return t;
		}

		public object LockObject {
			get { return this.lockObject; }
		}

		internal class OutgoingMessage {
			internal readonly TConnection conn;
			internal readonly PacketGroup group;

			internal OutgoingMessage(TConnection conn, PacketGroup group) {
				this.conn = conn;
				this.group = group;
			}
		}

		protected void InitNewConnection(TConnection newConn) {
			try {
				lock (this.lockObject) {
					newConn.Init(this);
				}
			} catch (Exception e) {
				Logger.WriteError(e);
				newConn.Close(e.Message);
			}
		}

		//from main loop
		internal void EnqueueOutgoing(TConnection conn, PacketGroup group) {
			group.Enqueued();

			this.outgoingPackets.Add(new OutgoingMessage(conn, group));
			this.outgoingPacketEnqueued.Set();

			this.outgoingPacketsSentEvent.Reset();
		}

		//from async background thread
		internal void HandlePacket(TConnection conn, TState state, IncomingPacket<TConnection, TState, TEndPoint> packet) {
			//lock (this.incomingPackets) {
			//    this.incomingPackets.Enqueue(new IncomingMessage(conn, packet));
			//}
			lock (this.lockObject) {
				try {
					state.On_PacketBeingHandled(packet);
					packet.Handle(conn, state);
				} catch (FatalException) {
					throw;
				} catch (Exception e) {
					Logger.WriteCritical("Exception while handling packet " + packet, e);
				}
			}
			packet.Dispose();
		}

		public void WaitForAllSent() {
			this.outgoingPacketsSentEvent.WaitOne();
		}

		//outgoing packets
		private void WorkerThreadMethod() {
			try {

				foreach (var msg in this.outgoingPackets.GetConsumingEnumerable(this.exitToken)) {
					msg.conn.ProcessSending(msg.group);

					if (this.outgoingPackets.Count == 0) {
						this.outgoingPacketsSentEvent.Set();
					}
				}
			} catch (OperationCanceledException) { }
		}
	}
}
