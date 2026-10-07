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
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication.NamedPipes {
	public class NamedPipeServer<TState> :
#if !MSWIN
		//temporarily, we fake named pipes via tcp in MONO
		
		AsyncCore<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>,
		IServer<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>, new() {

		private AsyncCallback onAccept;
		System.Net.Sockets.Socket listener;

		public NamedPipeServer(IProtocol<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint> protocol, object lockObject)
			: base(protocol, lockObject) {
			this.onAccept = this.OnAccept;

		}

		internal static System.Net.Sockets.Socket CreateSocket(System.Net.Sockets.AddressFamily addressFamily) {
			return new System.Net.Sockets.Socket(addressFamily, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
		}

		public void Bind(System.Net.IPEndPoint ipep) {
			if (this.IsBound) {
				throw new SEException("Already bound");
			}

			listener = CreateSocket(ipep.AddressFamily);

			try {
				listener.LingerState.Enabled = false;
#if !MONO
				listener.ExclusiveAddressUse = false;
#endif

				listener.Bind(ipep);
				listener.Listen(8);

				Console.WriteLine("Listening on port " + ipep.Port);

				listener.BeginAccept(CreateSocket(ipep.AddressFamily), 0, onAccept, listener);
			} catch (Exception e) {
				throw new FatalException("Server socket bind failed.", e);
			}
		}

		public System.Net.IPEndPoint BoundTo {
			get {
				try {
					return (System.Net.IPEndPoint) this.listener.LocalEndPoint;
				} catch {
					return null;
				}
			}
		}

		public bool IsBound {
			get {
				return this.BoundTo != null;
			}
		}

		private void OnAccept(IAsyncResult asyncResult) {
			System.Net.Sockets.Socket listener = (System.Net.Sockets.Socket) asyncResult.AsyncState;

			System.Net.Sockets.Socket accepted = null;

			try {
				accepted = listener.EndAccept(asyncResult);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			if (accepted != null) {
				NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
				newConn.socket = accepted;
				InitNewConnection(newConn);
			}

			//continue in accepting
			try {
				listener.BeginAccept(CreateSocket(this.listener.AddressFamily), 0, onAccept, listener);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		public void UnBind() {
			if (this.IsBound) {
				Console.WriteLine("Stopped listening on port " + this.BoundTo.Port);
			}
			try {
				listener.Close();
			} catch { }
		}

		protected override void On_DisposeUnmanagedResources() {
			UnBind();

			base.On_DisposeUnmanagedResources();
		}
#else
 AsyncCore<NamedPipeConnection<TState>, TState, string>,
		IServer<NamedPipeConnection<TState>, TState, string>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, string>, new() {

		private string pipeName;

		private AsyncCallback onAccept;
		private NamedPipeServerStream listener;

		public NamedPipeServer(IProtocol<NamedPipeConnection<TState>, TState, string> protocol, object lockObject, CancellationToken cancelToken)
			: base(protocol, lockObject, cancelToken) {
			this.onAccept = this.OnAccept;
		}

		public void Bind(string pipeName) {
			if (this.IsBound) {
				throw new SEException("Already bound");
			}

			this.pipeName = pipeName;


			try {
				Console.WriteLine("Listening on named pipe " + pipeName);

				this.NewBeginWaitForConnection();
			} catch (Exception e) {
				throw new FatalException("Server socket bind failed.", e);
			}
		}

		private void NewBeginWaitForConnection() {
			try {
				this.listener = new NamedPipeServerStream(this.pipeName, PipeDirection.InOut, 100, //why is there a maximum is beyond me
					PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
				this.listener.BeginWaitForConnection(this.onAccept, this.listener);
			} catch {
				this.listener = null;
				throw;
			}
		}

		public string BoundTo {
			get {
				return this.pipeName;
			}
		}

		public bool IsBound {
			get {
				return (this.listener != null);
			}
		}

		private void OnAccept(IAsyncResult asyncResult) {
			NamedPipeServerStream accepted = (NamedPipeServerStream) asyncResult.AsyncState;

			try {
				this.listener.EndWaitForConnection(asyncResult);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			if (accepted != null) {
				NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
				newConn.SetFields(this.pipeName, accepted);
				this.InitNewConnection(newConn);
			}

			//continue in accepting
			try {
				this.NewBeginWaitForConnection();
			} catch (ObjectDisposedException) {
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		public void UnBind() {
			if (this.IsBound) {
				Console.WriteLine("Stopped listening on named pipe " + this.pipeName);
			}
			try {
				this.listener.Close();
			} catch { }
			this.listener = null;
		}

		protected override void On_DisposeUnmanagedResources() {
			this.UnBind();

			base.On_DisposeUnmanagedResources();
		}

#endif
	}
}
