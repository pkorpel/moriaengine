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
using System.Net;
using System.Net.Sockets;
using SteamEngine.Common;

namespace SteamEngine.Communication.TCP {
	public sealed class TcpConnection<TState> :
		AbstractConnection<TcpConnection<TState>, TState, IPEndPoint>
		where TState : IConnectionState<TcpConnection<TState>, TState, IPEndPoint>, new() {


		internal Socket socket;

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;

		public TcpConnection() {
			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;
		}

		public override IPEndPoint EndPoint {
			get {
				return (IPEndPoint) this.socket.RemoteEndPoint;
			}
		}

		/// <summary>#1222 - whether the socket sends small packets at once (Nagle off), as the server sets it at accept.</summary>
		public bool NoDelay {
			get {
				Socket s = this.socket;
				try {
					return (s != null) && s.NoDelay;
				} catch (ObjectDisposedException) {
					return false;
				}
			}
		}

		public override bool IsConnected {
			get {
				if (this.socket != null) {
					return this.socket.Connected;
				}
				return false;
			}
		}

		protected override void On_Init() {
			base.On_Init();
			this.BeginReceive();
		}

		/// <summary>#100 - what an async call was started on: the socket and the connection's generation. A pooled connection can
		/// be closed and given to the next client before the call's callback comes, and then the callback is stale.</summary>
		private sealed class CallState {
			internal readonly Socket socket;
			internal readonly int generation;
			internal readonly Buffer buffer;

			internal CallState(Socket socket, int generation, Buffer buffer) {
				this.socket = socket;
				this.generation = generation;
				this.buffer = buffer;
			}
		}

		private bool IsStale(CallState call) {
			return (call.generation != this.Generation) || (call.socket != this.socket);
		}

		private void BeginReceive() {
			int offset = this.receivedDataLength;
			byte[] buffer = this.receivingBuffer.bytes;
			Socket socket = this.socket;

			socket.BeginReceive(buffer, offset,
				buffer.Length - offset, SocketFlags.None, this.onReceieve, new CallState(socket, this.Generation, null));
		}

		private void OnReceieve(IAsyncResult asyncResult) {
			CallState call = (CallState) asyncResult.AsyncState;
			if (this.IsStale(call)) {
				try {
					call.socket.EndReceive(asyncResult); //finishes the old socket's call; this object is someone else's now
				} catch { }
				return;
			}
			try {
				if ((this.socket != null) && (this.socket.Handle != IntPtr.Zero)) {
					int length = call.socket.EndReceive(asyncResult);

					if (length > 0) {
						//we have new data, but still possibly have some old data.
						this.ProcessReceievedData(length);
					} else {
						this.CloseUnlessStale(call, "Connection lost");
					}

					if (this.IsConnected && !this.IsStale(call)) {
						this.BeginReceive();
					}
				}
			} catch (FatalException e) {
				Logger.WriteFatal("TcpConnection.OnReceieve", e);
				this.CloseUnlessStale(call, e.Message);
			} catch (SocketException e) {
				//expected churn - client disconnects/resets/times out constantly, not a bug
				Logger.WriteDebug(e);
				this.CloseUnlessStale(call, e.Message);
			} catch (ObjectDisposedException e) {
				//the socket was closed under this call: the connection is gone already
				Logger.WriteDebug(e);
				this.CloseUnlessStale(call, e.Message);
			} catch (Exception e) {
				//anything else here is unexpected - a real bug, not routine network churn
				Logger.WriteError("TcpConnection.OnReceieve", e);
				this.CloseUnlessStale(call, e.Message);
			}
		}

		protected override void On_DisposeUnmanagedResources() {
			try {
				this.socket.Shutdown(SocketShutdown.Both);
			} catch { }
			try {
				this.socket.Close();
			} catch { }
			this.socket = null;

			base.On_DisposeUnmanagedResources();
		}

		protected override void BeginSend(BufferToSend toSend) {
			Socket socket = this.socket;
			if (socket == null) { //#100 - closed meanwhile
				toSend.buffer.Dispose();
				return;
			}
			try {
				socket.BeginSend(toSend.buffer.bytes, toSend.offset, toSend.len, SocketFlags.None, this.onSend, new CallState(socket, this.Generation, toSend.buffer));
			} catch (ObjectDisposedException) { //#100 - the receiving thread closed it a moment ago: nobody to send to
				toSend.buffer.Dispose();
			}
		}

		private void OnSend(IAsyncResult asyncResult) {
			CallState call = (CallState) asyncResult.AsyncState;

			try {
				SocketError err;
				call.socket.EndSend(asyncResult, out err);

				if ((err != SocketError.Success) && !this.IsStale(call)) {
					this.Close(err.ToString());
				}
			} catch (FatalException e) {
				Logger.WriteFatal("TcpConnection.OnSend", e);
				this.CloseUnlessStale(call, e.Message);
			} catch (SocketException e) {
				Logger.WriteDebug(e);
				this.CloseUnlessStale(call, e.Message);
			} catch (ObjectDisposedException e) {
				Logger.WriteDebug(e); //the socket was closed while this was under way: the connection is gone already
				this.CloseUnlessStale(call, e.Message);
			} catch (Exception e) {
				Logger.WriteError("TcpConnection.OnSend", e);
				this.CloseUnlessStale(call, e.Message);
			} finally {
				call.buffer.Dispose();
			}
		}

		/// <summary>#100 - a stale callback must not close this object: it's the next client's connection by now.</summary>
		private void CloseUnlessStale(CallState call, string reason) {
			if (!this.IsStale(call)) {
				this.Close(reason);
			}
		}
	}
}