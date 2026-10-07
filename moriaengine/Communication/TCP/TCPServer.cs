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
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication.TCP {
	public class TcpServer<TState> :
		AsyncCore<TcpConnection<TState>, TState, IPEndPoint>,
		IServer<TcpConnection<TState>, TState, IPEndPoint>
		where TState : IConnectionState<TcpConnection<TState>, TState, IPEndPoint>, new() {

		private AsyncCallback onAccept;
		Socket listener;

		public TcpServer(IProtocol<TcpConnection<TState>, TState, IPEndPoint> protocol, object lockObject, CancellationToken cancelToken)
			: base(protocol, lockObject, cancelToken) {
			this.onAccept = this.OnAccept;

		}

		internal static Socket CreateSocket(AddressFamily addressFamily) {
			return new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
		}

		public void Bind(IPEndPoint ipep) {
			if (this.IsBound) {
				throw new SEException("Already bound");
			}

			this.listener = CreateSocket(ipep.AddressFamily);

			try {
				this.listener.LingerState.Enabled = false;
#if !MONO
				this.listener.ExclusiveAddressUse = false;
#endif

				this.listener.Bind(ipep);
				this.listener.Listen(8);

				Console.WriteLine("Listening on port " + ipep.Port);

				this.listener.BeginAccept(CreateSocket(ipep.AddressFamily), 0, this.onAccept, this.listener);
			} catch (Exception e) {
				throw new FatalException("Server socket bind failed.", e);
			}
		}

		public IPEndPoint BoundTo {
			get {
				try {
					return (IPEndPoint) this.listener.LocalEndPoint;
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
			Socket listener = (Socket) asyncResult.AsyncState;

			Socket accepted = null;

			try {
				accepted = listener.EndAccept(asyncResult);
			} catch (ObjectDisposedException) {
				return;
			} catch (Exception e) {
				Logger.WriteError(e);
			}

			if (accepted != null) {
				TcpConnection<TState> newConn = Pool<TcpConnection<TState>>.Acquire();
				newConn.socket = accepted;
				this.InitNewConnection(newConn);
			}

			//continue in accepting
			try {
				listener.BeginAccept(CreateSocket(this.listener.AddressFamily), 0, this.onAccept, listener);
			} catch (ObjectDisposedException) {
			} catch (Exception e) {
				Logger.WriteError(e);
			}
		}

		public void UnBind() {
			if (this.IsBound) {
				Console.WriteLine("Stopped listening on port " + this.BoundTo.Port);
			}
			try {
				this.listener.Close();
			} catch { }
		}

		protected override void On_DisposeUnmanagedResources() {
			this.UnBind();

			base.On_DisposeUnmanagedResources();
		}
	}
}
