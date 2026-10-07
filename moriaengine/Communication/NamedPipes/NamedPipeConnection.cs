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
using System.IO.Pipes;
using SteamEngine.Common;

namespace SteamEngine.Communication.NamedPipes {
	public sealed class NamedPipeConnection<TState> :
#if !MSWIN
		//temporarily, we fake named pipes via tcp in MONO
		AbstractConnection<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>, new() {


		internal System.Net.Sockets.Socket socket;

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;

		public NamedPipeConnection() {
			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;
		}

		public override System.Net.IPEndPoint EndPoint {
			get {
				return (System.Net.IPEndPoint) this.socket.RemoteEndPoint;
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

		private void BeginReceive() {
			int offset = this.receivedDataLength;
			byte[] buffer = this.receivingBuffer.bytes;

			this.socket.BeginReceive(buffer, offset,
				buffer.Length - offset, System.Net.Sockets.SocketFlags.None, onReceieve, null);
		}

		private void OnReceieve(IAsyncResult asyncResult) {
			try {
				if ((this.socket != null) && (this.socket.Handle != IntPtr.Zero)) {
					int length = this.socket.EndReceive(asyncResult);

					if (length > 0) {
						//we have new data, but still possibly have some old data.
						base.ProcessReceievedData(length);
					} else {
						this.Close("Connection lost");
					}

					if (this.IsConnected) {
						this.BeginReceive();
					}
				}
			} catch (Exception e) {
				Logger.WriteDebug(e);
				this.Close(e.Message);
			}
		}

		protected override void On_DisposeUnmanagedResources() {
			try {
				this.socket.Shutdown(System.Net.Sockets.SocketShutdown.Both);
			} catch { }
			try {
				this.socket.Close();
			} catch { }
			this.socket = null;

			base.On_DisposeUnmanagedResources();
		}

		protected override void BeginSend(BufferToSend toSend) {
			this.socket.BeginSend(toSend.buffer.bytes, toSend.offset, toSend.len, System.Net.Sockets.SocketFlags.None, onSend, toSend.buffer);
		}

		private void OnSend(IAsyncResult asyncResult) {
			Buffer toDispose = (Buffer) asyncResult.AsyncState;

			try {
				System.Net.Sockets.SocketError err;
				this.socket.EndSend(asyncResult, out err);

				if (err != System.Net.Sockets.SocketError.Success) {
					this.Close(err.ToString());
				}
			} catch (Exception e) {
				this.Close(e.Message);
			} finally {
				toDispose.Dispose();
			}
		}
#else
		AbstractConnection<NamedPipeConnection<TState>, TState, string>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, string>, new() {

		private PipeStream pipe;
		private string pipeName;

		private AsyncCallback onSend;
		private AsyncCallback onReceieve;


		public NamedPipeConnection() {
			this.onSend = this.OnSend;
			this.onReceieve = this.OnReceieve;
		}

		internal void SetFields(string pipeName, PipeStream pipe) {
			this.pipeName = pipeName;
			this.pipe = pipe;
		}

		protected override void On_Init() {
			base.On_Init();
			this.BeginReceive();
		}

		public override string EndPoint {
			get {
				return this.pipeName;
			}
		}

		public override bool IsConnected {
			get {
				if (this.pipe != null) {
					return this.pipe.IsConnected;
				}
				return false;
			}
		}

		private void BeginReceive() {
			int offset = this.receivedDataLength;
			byte[] buffer = this.receivingBuffer.bytes;

			this.pipe.BeginRead(buffer, offset,
				buffer.Length - offset, this.onReceieve, null);
		}

		private void OnReceieve(IAsyncResult asyncResult) {
			try {
				if (this.pipe != null) {
					int length = this.pipe.EndRead(asyncResult);

					if (length > 0) {
						//we have new data, but still possibly have some old data.
						this.ProcessReceievedData(length);
					} else {
						this.Close("Connection lost");
					}

					if (this.IsConnected) {
						this.BeginReceive();
					}
				}
			} catch (Exception e) {
				Logger.WriteDebug(e);
				this.Close(e.Message);
			}
		}

		protected override void On_DisposeUnmanagedResources() {
			try {
				this.pipe.Close();
			} catch { }
			this.pipe = null;

			base.On_DisposeUnmanagedResources();
		}

		protected override void BeginSend(BufferToSend toSend) {
			this.pipe.BeginWrite(toSend.buffer.bytes, toSend.offset, toSend.len, this.onSend, toSend.buffer);
		}

		private void OnSend(IAsyncResult asyncResult) {
			Buffer toDispose = (Buffer) asyncResult.AsyncState;

			try {
				this.pipe.EndWrite(asyncResult);
			} catch (Exception e) {
				this.Close(e.Message);
			} finally {
				toDispose.Dispose();
			}
		}

#endif
	}
}
