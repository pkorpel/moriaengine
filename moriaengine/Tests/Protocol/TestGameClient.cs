using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Speaks the GameCore login-stage handshake just well enough to get a character list back:
	/// connect -> GameServerLoginInPacket (0x91) -> CharactersListOutPacket (0xA9).
	///
	/// Uses GameEncryption's "no encryption" fast path (the 4 bytes right after the packet ID
	/// must exactly echo the seed) the same way TestLoginClient does for the login stage - no
	/// Twofish needed. Server->client traffic here IS always Huffman-compressed though (see
	/// HuffmanDecompressor's own comment), unlike the login stage.
	/// </summary>
	internal sealed class TestGameClient : IDisposable {
		private readonly TcpClient tcp;
		private readonly NetworkStream stream;

		public TestGameClient(string host, int port) {
			this.tcp = new TcpClient();
			this.tcp.Connect(host, port);
			this.stream = this.tcp.GetStream();
		}

		public void SendLogin(string account, string password) {
			byte[] packet = new byte[69];
			// bytes 0-3: seed, left zero; bytes 5-8 must echo it exactly (both zero here too).
			packet[4] = 0x91;
			EncodeAsciiFixed(account, packet, 9, 30);
			EncodeAsciiFixed(password, packet, 39, 30);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Reads one Huffman-compressed frame and returns the decoded character names (nulls for empty slots).</summary>
		public string[] ReadCharacterList() {
			byte[] decoded = ReadOneCompressedFrame();

			if (decoded.Length < 3 || decoded[0] != 0xA9) {
				throw new InvalidOperationException("Expected CharactersListOutPacket (0xA9), got " +
					(decoded.Length == 0 ? "no data" : "0x" + decoded[0].ToString("x2")));
			}

			const int maxCharacters = 5; // AbstractAccount.maxCharactersPerGameAccount
			var names = new string[maxCharacters];
			int pos = 3 + 1; // skip id(1) + length(2) + numOfCharacters(1)
			for (int i = 0; i < maxCharacters; i++) {
				string name = Encoding.ASCII.GetString(decoded, pos, 30).TrimEnd('\0');
				names[i] = name.Length > 0 ? name : null;
				pos += 60; // 30-byte name + 30 bytes of zeros
			}
			return names;
		}

		private byte[] ReadOneCompressedFrame() {
			var buffer = new List<byte>();
			while (true) {
				int b = this.stream.ReadByte();
				if (b < 0) {
					throw new InvalidOperationException("Connection closed after " + buffer.Count + " compressed bytes, before a full frame arrived.");
				}
				buffer.Add((byte) b);

				try {
					var (decoded, bytesConsumed) = HuffmanDecompressor.DecompressOneFrame(buffer.ToArray(), 0, buffer.Count);
					if (bytesConsumed == buffer.Count) {
						return decoded;
					}
					// Shouldn't happen here (we only ever feed it exactly what it's consumed so
					// far), but if it ever did, it would mean a second frame already started
					// arriving - not a case Phase 11's scope needs to handle.
				} catch (InvalidOperationException) {
					// Not a complete frame yet - read another byte and try again.
				}
			}
		}

		private static void EncodeAsciiFixed(string text, byte[] buffer, int offset, int fieldLength) {
			byte[] ascii = Encoding.ASCII.GetBytes(text ?? "");
			int copyLen = Math.Min(ascii.Length, fieldLength - 1);
			Array.Copy(ascii, 0, buffer, offset, copyLen);
		}

		public void Dispose() {
			this.stream.Dispose();
			this.tcp.Dispose();
		}
	}
}
