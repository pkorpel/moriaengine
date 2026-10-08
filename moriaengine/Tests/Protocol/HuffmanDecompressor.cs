using System;
using System.Collections.Generic;
using SteamEngine.Networking;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// GameCore compresses every outgoing GameState packet (GameCompression.Compress, always
	/// on - see GameState.Compression) using a fixed Huffman-style bit table, and pads each
	/// compressed frame's trailing byte with zero bits after an explicit end-of-stream symbol
	/// (256). GameCompression itself only implements the encode direction (its own Decompress
	/// is a no-op passthrough, since the server never needs to decompress its own output); a
	/// real UO client needs the inverse. Rather than hand-transcribing the 257-entry bitTable,
	/// this builds the decode side directly from GameCompression's own (now internal, see
	/// GameCompression.cs) table - guaranteed bit-for-bit consistent with the server.
	///
	/// Decodes bit-by-bit (not via a lookahead table) specifically so it consumes *exactly* the
	/// bits of one frame, stopping the moment the end-of-stream symbol is matched - since this
	/// reads from a live TCP stream where multiple packet groups can arrive back-to-back in a
	/// single read, overshooting into the next frame's bytes would silently corrupt it. Huffman
	/// codes are prefix-free, so trying each bit-length in turn and stopping at the first match
	/// is both correct and simple.
	/// </summary>
	internal static class HuffmanDecompressor {
		private const int MaxCodeBits = 11;
		private const int EndOfStreamSymbol = 256;

		private static readonly Dictionary<(int bits, int code), int> codeToSymbol = BuildCodeMap();

		private static Dictionary<(int bits, int code), int> BuildCodeMap() {
			var map = new Dictionary<(int bits, int code), int>();
			for (int symbol = 0; symbol <= EndOfStreamSymbol; symbol++) {
				int numBits = GameCompression.bitTable[symbol, 0];
				int code = GameCompression.bitTable[symbol, 1];
				map[(numBits, code)] = symbol;
			}
			return map;
		}

		/// <summary>
		/// Decodes exactly one compressed frame starting at offset, stopping at the
		/// end-of-stream symbol. Returns the decoded bytes and how many input bytes the frame
		/// (including its zero-padding to the next byte boundary) actually occupied, so the
		/// caller can safely resume reading right after it for the next frame.
		/// </summary>
		public static (byte[] decoded, int bytesConsumed) DecompressOneFrame(byte[] input, int offset, int count) {
			var output = new List<byte>();
			int bitPos = 0;
			int totalBits = count * 8;

			int ReadBit() {
				if (bitPos >= totalBits) {
					throw new InvalidOperationException("Huffman decode: ran out of input before finding the end-of-stream symbol.");
				}
				int byteIndex = offset + (bitPos / 8);
				int bitIndexFromMsb = 7 - (bitPos % 8);
				bitPos++;
				return (input[byteIndex] >> bitIndexFromMsb) & 1;
			}

			while (true) {
				int code = 0;
				int symbol = -1;
				for (int bits = 1; bits <= MaxCodeBits; bits++) {
					code = (code << 1) | ReadBit();
					if (codeToSymbol.TryGetValue((bits, code), out symbol)) {
						break;
					}
				}
				if (symbol < 0) {
					throw new InvalidOperationException("Huffman decode: no matching code found within " + MaxCodeBits + " bits.");
				}
				if (symbol == EndOfStreamSymbol) {
					break;
				}
				output.Add((byte) symbol);
			}

			int bytesConsumed = (bitPos + 7) / 8; // round up - the encoder zero-pads to a full byte
			return (output.ToArray(), bytesConsumed);
		}
	}
}
