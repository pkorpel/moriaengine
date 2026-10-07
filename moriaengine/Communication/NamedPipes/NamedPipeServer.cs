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
	// System.IO.Pipes has been cross-platform (Unix-domain-socket-backed on Linux/macOS)
	// since .NET Core 2.1, so this is a real named pipe implementation on every platform
	// .NET 8 runs on - no TCP-loopback workaround needed.
	public class NamedPipeServer<TState> :
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
	}
}
