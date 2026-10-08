using System;
using System.Text;
using Xunit;
using SteamEngine.Networking;
using SteamEngine.Tests.Fixtures;

namespace SteamEngine.Tests.Protocol {
	[Collection(EngineCollection.Name)]
	public class HuffmanDecompressorTests {
		// Empty input isn't covered here: GameCompression.Compress itself throws
		// IndexOutOfRangeException on zero-length input (pre-existing, unrelated to this
		// decompressor - packet groups are never actually empty in real server operation,
		// so it's never been hit before).
		[Theory]
		[InlineData("A")]
		[InlineData("Hello, world!")]
		[InlineData("The quick brown fox jumps over the lazy dog. 0123456789.")]
		public void RoundTripsThroughRealEncoder(string text) {
			byte[] input = Encoding.ASCII.GetBytes(text);
			byte[] compressed = new byte[input.Length * 2 + 16];
			int compressedLen = GameCompression.instance.Compress(input, 0, compressed, 0, input.Length);
			Array.Resize(ref compressed, compressedLen);

			var (decoded, bytesConsumed) = HuffmanDecompressor.DecompressOneFrame(compressed, 0, compressed.Length);

			Assert.Equal(input, decoded);
			Assert.Equal(compressed.Length, bytesConsumed);
		}

		[Fact]
		public void RoundTripsAllByteValues() {
			byte[] input = new byte[256];
			for (int i = 0; i < 256; i++) {
				input[i] = (byte) i;
			}
			byte[] compressed = new byte[input.Length * 2 + 16];
			int compressedLen = GameCompression.instance.Compress(input, 0, compressed, 0, input.Length);
			Array.Resize(ref compressed, compressedLen);

			var (decoded, bytesConsumed) = HuffmanDecompressor.DecompressOneFrame(compressed, 0, compressed.Length);

			Assert.Equal(input, decoded);
			Assert.Equal(compressed.Length, bytesConsumed);
		}

		[Fact]
		public void DecodesOnlyTheFirstOfTwoConsecutiveFrames() {
			byte[] inputA = Encoding.ASCII.GetBytes("first");
			byte[] inputB = Encoding.ASCII.GetBytes("second-frame");

			byte[] compressedA = new byte[64];
			int lenA = GameCompression.instance.Compress(inputA, 0, compressedA, 0, inputA.Length);

			byte[] compressedB = new byte[64];
			int lenB = GameCompression.instance.Compress(inputB, 0, compressedB, 0, inputB.Length);

			byte[] combined = new byte[lenA + lenB];
			Array.Copy(compressedA, 0, combined, 0, lenA);
			Array.Copy(compressedB, 0, combined, lenA, lenB);

			var (decodedA, consumedA) = HuffmanDecompressor.DecompressOneFrame(combined, 0, combined.Length);
			Assert.Equal(inputA, decodedA);
			Assert.Equal(lenA, consumedA);

			var (decodedB, consumedB) = HuffmanDecompressor.DecompressOneFrame(combined, consumedA, combined.Length - consumedA);
			Assert.Equal(inputB, decodedB);
			Assert.Equal(lenB, consumedB);
		}
	}
}
