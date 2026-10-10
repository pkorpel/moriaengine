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

		/// <summary>
		/// Sends CreateCharacterInPacket (0x00, 104 bytes incl. id - see
		/// GameCore/Networking/IncomingPackets.cs's CreateCharacterInPacket.Read for the exact
		/// field layout this mirrors). Field values are the minimum that pass
		/// CreateCharacterInPacket.Handle's validation (name is letters-only; skin/hair/shirt/
		/// pants colors are each the lowest value in their accepted range; hairStyle/facialHair
		/// are 0, which skips their range checks entirely) - visual appearance doesn't matter for
		/// what this test client is for. The server synchronously logs the new character in as
		/// part of handling this packet (CreateCharacterInPacket.Handle calls
		/// state.LoginCharacter at the end), so no separate "select character" (0x5D) packet is
		/// needed.
		/// </summary>
		public void SendCreateCharacter(string charName) {
			byte[] packet = new byte[104];
			packet[0] = 0x00;
			// bytes 1-9: unknown/account block, left zero.
			EncodeAsciiFixed(charName, packet, 10, 30);
			// bytes 40-69: password, left zero (ignored by Handle).
			packet[70] = 0; // gender: male
			packet[71] = 10; // startStr
			packet[72] = 10; // startDex
			packet[73] = 10; // startInt
			// bytes 74-79: three (skillId, skillValue) pairs, left zero.
			EncodeUShortBigEndian(0x3ea, packet, 80); // skinColor - lowest accepted value
			EncodeUShortBigEndian(0, packet, 82); // hairStyle - 0 skips the range check
			EncodeUShortBigEndian(0x44e, packet, 84); // hairColor - still range-checked even with no hair
			EncodeUShortBigEndian(0, packet, 86); // facialHair - 0 skips the range check
			EncodeUShortBigEndian(0, packet, 88); // facialHairColor - 0 skips the range check
			EncodeUShortBigEndian(0, packet, 90); // locationNum - ignored by this server's f_createPlayerCharacter
			// bytes 92-99: unknown block, left zero.
			EncodeUShortBigEndian(0x02, packet, 100); // shirtColor - lowest accepted value
			EncodeUShortBigEndian(0x02, packet, 102); // pantsColor - lowest accepted value
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Sends TalkRequestInPacket (0x03) - plain ASCII speech, interpreted server-side as a
		/// "." command if it starts with Globals.CommandPrefix (the default), same as any
		/// player typing in their chat bar. Requires a character to already be selected/created
		/// (state.CharacterNotNull), i.e. call after SendCreateCharacter.
		/// </summary>
		public void SendSpeech(string text) {
			byte[] textBytes = Encoding.ASCII.GetBytes(text);
			byte[] packet = new byte[8 + textBytes.Length];
			packet[0] = 0x03;
			EncodeUShortBigEndian((ushort) packet.Length, packet, 1); // blockSize includes the id byte
			packet[3] = 0; // speech type (SpeechType.Normal)
			EncodeUShortBigEndian(0, packet, 4); // color
			EncodeUShortBigEndian(3, packet, 6); // font
			Array.Copy(textBytes, 0, packet, 8, textBytes.Length);
			this.stream.Write(packet, 0, packet.Length);
		}

		private static void EncodeUShortBigEndian(int value, byte[] buffer, int offset) {
			buffer[offset] = (byte) ((value >> 8) & 0xFF);
			buffer[offset + 1] = (byte) (value & 0xFF);
		}

		public void Dispose() {
			this.stream.Dispose();
			this.tcp.Dispose();
		}
	}
}
