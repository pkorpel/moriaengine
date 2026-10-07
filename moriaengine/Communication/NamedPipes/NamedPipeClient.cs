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
	// System.IO.Pipes has been cross-platform (Unix-domain-socket-backed on Linux/macOS)
	// since .NET Core 2.1, so this is a real named pipe implementation on every platform
	// .NET 8 runs on - no TCP-loopback workaround needed.
	public sealed class NamedPipeClientFactory<TState> :
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
	}
}
