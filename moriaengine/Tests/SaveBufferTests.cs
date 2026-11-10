using System;
using System.IO;
using System.Text;
using SteamEngine.Persistence;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #1209 - a save file's bytes held in memory: the same bytes a plain StreamWriter writes to a file (as the save did
	/// before), across chunk boundaries, and a released buffer starts empty.
	/// </summary>
	public class SaveBufferTests {
		private static string SampleText(int lines) {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < lines; i++) {
				sb.Append("[Item 0x").Append(i.ToString("x")).Append("]\nname=\"Erebor ").Append(i).Append(" áčž — \U0001F409\"\nP=(1234,567,8,0)\n\n");
			}
			return sb.ToString();
		}

		private static byte[] AsAPlainStreamWriterWritesIt(string text) {
			using (MemoryStream file = new MemoryStream()) {
				using (StreamWriter writer = new StreamWriter(file, leaveOpen: true)) { //the default encoding, as File.Create's writer had
					writer.Write(text);
				}
				return file.ToArray();
			}
		}

		private static byte[] ThroughASaveBuffer(string text, out long length) {
			SaveBuffer buffer = new SaveBuffer();
			using (StreamWriter writer = new StreamWriter(buffer, SaveStream.SaveEncoding, 1 << 16, true)) {
				writer.Write(text);
			}
			length = buffer.Length;
			using (MemoryStream copy = new MemoryStream()) {
				buffer.CopyBytesTo(copy);
				buffer.Release();
				Assert.Equal(0, buffer.Length);
				return copy.ToArray();
			}
		}

		[Fact]
		public void HoldsTheBytesAPlainStreamWriterWrites() {
			string small = SampleText(3);
			long length;
			byte[] buffered = ThroughASaveBuffer(small, out length);
			Assert.Equal(AsAPlainStreamWriterWritesIt(small), buffered);
			Assert.Equal(buffered.Length, length);
			Assert.NotEqual(0xEF, buffered[0]); //no BOM
		}

		[Fact]
		public void KeepsEveryByteAcrossChunks() {
			string big = SampleText(60000); //about 5 MB: several 1 MB chunks, a 4-byte character certainly split
			long length;
			byte[] buffered = ThroughASaveBuffer(big, out length);
			byte[] plain = AsAPlainStreamWriterWritesIt(big);
			Assert.True(plain.Length > 3 * (1 << 20), "the sample should span chunks: " + plain.Length);
			Assert.Equal(plain.Length, buffered.Length);
			Assert.Equal(plain, buffered);
			Assert.Equal(big, SaveStream.SaveEncoding.GetString(buffered));
		}

		[Fact]
		public void AReleasedBufferIsReusedEmpty() {
			SaveBuffer first = new SaveBuffer();
			first.Write(new byte[(1 << 20) + 5], 0, (1 << 20) + 5);
			first.Release();
			SaveBuffer second = new SaveBuffer();
			second.WriteByte(42);
			using (MemoryStream copy = new MemoryStream()) {
				second.CopyBytesTo(copy);
				Assert.Equal(new byte[] { 42 }, copy.ToArray()); //a pooled chunk, only what was written into it
			}
			second.Release();
		}

		[Fact]
		public void AReserveFillsThePoolAndTheBuffersTakeFromIt() { //#1360
			SaveBuffer.Reserve(3 * (1 << 20) + 1);
			int reserved = SaveBuffer.PooledChunks;
			Assert.True(reserved >= 4, "pooled " + reserved);
			SaveBuffer.Reserve(1); //never shrinks
			Assert.Equal(reserved, SaveBuffer.PooledChunks);

			SaveBuffer buffer = new SaveBuffer();
			byte[] data = new byte[3 * (1 << 20)];
			new Random(1360).NextBytes(data);
			buffer.Write(data, 0, data.Length);
			Assert.Equal(reserved - 3, SaveBuffer.PooledChunks); //taken from the pool, nothing new allocated
			using (MemoryStream copy = new MemoryStream()) {
				buffer.CopyBytesTo(copy);
				Assert.Equal(data, copy.ToArray());
			}
			buffer.Release();
			Assert.Equal(reserved, SaveBuffer.PooledChunks);
		}
	}
}
