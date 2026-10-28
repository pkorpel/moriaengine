using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Speaks the AuxiliaryServer login-stage handshake just well enough to get redirected to
	/// a GameCore instance: connect -> GameLoginPacket (0x80) -> ServersListPacket (0xA8) ->
	/// ServerSelectPacket (0xA0) -> LoginToServerPacket (0x8c).
	///
	/// Deliberately uses the "no encryption" fast path both LoginEncryption.Init and
	/// GameEncryption.Init already support (checked by matching a specific plaintext byte
	/// pattern right after the 4-byte seed) rather than re-implementing either cipher -
	/// AuxiliaryServer's login stage is unencrypted server->client anyway (LoginClient.
	/// Compression is also null, so no Huffman framing here either).
	/// </summary>
	internal sealed class TestLoginClient : IDisposable {
		private readonly TcpClient tcp;
		private readonly NetworkStream stream;

		/// <summary>See TestGameClient.DefaultReadTimeoutMillis's own doc comment - this
		/// stream had the identical gap (no read timeout at all, relying on NetworkStream's
		/// default of Timeout.Infinite) across every read helper below, covering the entire
		/// AuxiliaryServer login-stage handshake every protocol test goes through first.</summary>
		private const int DefaultReadTimeoutMillis = 15000;

		public TestLoginClient(string host, int port) {
			ServerHealth.BeforeConnect(); //#785 - fail fast instead of retrying against a dead server pair
			this.tcp = new TcpClient();
			this.tcp.Connect(host, port);
			this.stream = this.tcp.GetStream();
			this.stream.ReadTimeout = DefaultReadTimeoutMillis;
		}

		public void SendLogin(string account, string password) {
			byte[] packet = new byte[66];
			// bytes 0-3: seed, left zero - the no-encryption check only cares that byte 4 is
			// 0x80 and that the account/password fields look like real ASCII, not the seed value.
			packet[4] = 0x80;
			EncodeAsciiFixed(account, packet, 5, 30);
			EncodeAsciiFixed(password, packet, 35, 31);
			this.stream.Write(packet, 0, packet.Length);
		}

		public int ReadServersList() {
			byte id = ReadByte(this.stream);
			if (id != 0xA8) {
				throw new InvalidOperationException("Expected ServersListPacket (0xA8), got 0x" + id.ToString("x2"));
			}
			int length = ReadUShort(this.stream);
			byte[] rest = ReadExactly(this.stream, length - 3);
			// rest[0] = flag byte, rest[1..2] = server count (BE ushort)
			int serverCount = (rest[1] << 8) | rest[2];
			if (serverCount > 0) {
				ServerHealth.MarkHealthy();
			}
			return serverCount;
		}

		public void SendServerSelect(int serverIndex) {
			byte[] packet = new byte[3];
			packet[0] = 0xA0;
			packet[1] = (byte) (serverIndex >> 8);
			packet[2] = (byte) serverIndex;
			this.stream.Write(packet, 0, packet.Length);
		}

		public (int port, byte[] seed) ReadRedirect() {
			byte id = ReadByte(this.stream);
			if (id != 0x8c) {
				throw new InvalidOperationException("Expected LoginToServerPacket (0x8c), got 0x" + id.ToString("x2"));
			}
			byte[] rest = ReadExactly(this.stream, 10); // 4 (ip) + 2 (port) + 4 ("new key" - the real game-server seed)
			int port = (rest[4] << 8) | rest[5];
			byte[] seed = new byte[4];
			Array.Copy(rest, 6, seed, 0, 4);
			return (port, seed);
		}

		private static void EncodeAsciiFixed(string text, byte[] buffer, int offset, int fieldLength) {
			byte[] ascii = Encoding.ASCII.GetBytes(text ?? "");
			int copyLen = Math.Min(ascii.Length, fieldLength - 1); // leave at least one trailing zero
			Array.Copy(ascii, 0, buffer, offset, copyLen);
			// remaining bytes in the field are already zero (fresh array)
		}

		internal static byte ReadByte(NetworkStream stream) {
			int b;
			try {
				b = stream.ReadByte();
			} catch (IOException e) {
				// InvalidOperationException, not TimeoutException, deliberately - matches the
				// "connection closed" throw right below, which is what every
				// WaitForServerListingAndGetRedirectPort retry loop across the test suite
				// already catches. See TestGameClient.ReadOneCompressedFrame's own identical
				// note for the full reasoning.
				throw new InvalidOperationException("Did not receive a packet ID byte within " +
					DefaultReadTimeoutMillis + "ms.", e);
			}
			if (b < 0) {
				throw new InvalidOperationException("Connection closed while expecting a packet ID byte.");
			}
			return (byte) b;
		}

		internal static int ReadUShort(NetworkStream stream) {
			byte[] bytes = ReadExactly(stream, 2);
			return (bytes[0] << 8) | bytes[1];
		}

		internal static byte[] ReadExactly(NetworkStream stream, int count) {
			byte[] buffer = new byte[count];
			int totalRead = 0;
			while (totalRead < count) {
				int read;
				try {
					read = stream.Read(buffer, totalRead, count - totalRead);
				} catch (IOException e) {
					throw new InvalidOperationException("Did not receive the remaining " + (count - totalRead) +
						" of " + count + " expected bytes within " + DefaultReadTimeoutMillis + "ms.", e);
				}
				if (read == 0) {
					throw new InvalidOperationException("Connection closed after reading " + totalRead + " of " + count + " expected bytes.");
				}
				totalRead += read;
			}
			return buffer;
		}

		public void Dispose() {
			this.stream.Dispose();
			this.tcp.Dispose();
		}
	}
}
