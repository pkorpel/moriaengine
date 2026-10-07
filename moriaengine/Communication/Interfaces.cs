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



namespace SteamEngine.Communication {

	public interface IClientFactory<TConnection, TState, TEndPoint> //:
		//IAsyncCore<TProtocol, TConnection, TState, TEndPoint>
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		TConnection Connect(TEndPoint endpoint);
	}

	public interface IServer<TConnection, TState, TEndPoint> //:
		//IAsyncCore<TProtocol, TConnection, TState, TEndPoint>
		//where TProtocol : IProtocol<TConnection, TState, TEndPoint>, new()
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		void Bind(TEndPoint endpoint);
		TEndPoint BoundTo { get; }
		bool IsBound { get; }
		void UnBind();
	}

	//public interface IAsyncCore<TProtocol, TConnection, TState, TEndPoint>
	//    where TProtocol : IProtocol<TProtocol, TConnection, TState, TEndPoint>, new()
	//    where TConnection : AbstractConnection<TProtocol, TConnection, TState, TEndPoint>, new()
	//    where TState : IConnectionState<TProtocol, TConnection, TState, TEndPoint>, new() {

	//    object LockObject { get; }
	//}

	public interface IProtocol<TConnection, TState, TEndPoint>
		//where TProtocol : IProtocol<TProtocol, TConnection, TState, TEndPoint>, new()
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		IncomingPacket<TConnection, TState, TEndPoint> GetPacketImplementation(byte id, TConnection conn, TState state, out bool discardAfterReading);
	}

	public interface IConnectionState<TConnection, TState, TEndPoint> //: IDisposable
		//where TProtocol : IProtocol<TProtocol, TConnection, TState, TEndPoint>, new()
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		IEncryption Encryption { get; }

		ICompression Compression { get; }

		void On_Init(TConnection conn);

		void On_Close(string reason);

		void On_PacketBeingHandled(IncomingPacket<TConnection, TState, TEndPoint> packet);

		bool PacketGroupsJoiningAllowed { get; }
	}

	public interface IEncryption {
		// 
		EncryptionInitResult Init(byte[] bytesIn, int offsetIn, int lengthIn, out int bytesUsed);

		// Encrypt outgoing data
		int Encrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);

		// Decrypt incoming data
		int Decrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);
	}

	public interface ICompression {
		// Compress outgoing data
		int Compress(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);

		// Decompress incoming data
		int Decompress(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length);
	}
}