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

using System.IO.Pipes;
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication.NamedPipes {
	public sealed class NamedPipeClientFactory<TState> :
#if !MSWIN
		//temporarily, we fake named pipes via tcp in MONO
		AsyncCore<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>,
		IClientFactory<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint>, new() {

		public NamedPipeClientFactory(IProtocol<NamedPipeConnection<TState>, TState, System.Net.IPEndPoint> protocol, object lockObject)
			: base(protocol, lockObject) {

		}

		public NamedPipeConnection<TState> Connect(System.Net.IPEndPoint endpoint) {
			System.Net.Sockets.Socket socket = NamedPipeServer<TState>.CreateSocket(endpoint.AddressFamily);
			socket.Connect(endpoint);

			NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
			newConn.socket = socket;
			InitNewConnection(newConn);

			return newConn;
		}
#else
 AsyncCore<NamedPipeConnection<TState>, TState, string>,
		IClientFactory<NamedPipeConnection<TState>, TState, string>
		where TState : IConnectionState<NamedPipeConnection<TState>, TState, string>, new() {

		public NamedPipeClientFactory(IProtocol<NamedPipeConnection<TState>, TState, string> protocol, object lockObject, CancellationToken exitToken)
			: base(protocol, lockObject, exitToken) {
		}

		public NamedPipeConnection<TState> Connect(string pipeName) {

			NamedPipeClientStream pipe = new NamedPipeClientStream(".", pipeName,
				PipeDirection.InOut, PipeOptions.Asynchronous);
			pipe.Connect();

			NamedPipeConnection<TState> newConn = Pool<NamedPipeConnection<TState>>.Acquire();
			newConn.SetFields(pipeName, pipe);
			this.InitNewConnection(newConn);

			return newConn;
		}
#endif
	}
}
