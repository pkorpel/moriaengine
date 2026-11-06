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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SteamEngine.Persistence {
	/// <summary>
	/// #1209 - a save file's bytes, held in memory between formatting (world paused) and writing (a worker thread). Write-only,
	/// in 1 MB chunks taken from a pool, so a 94 MB things.sav isn't one array grown by doubling and copied each time, and the
	/// next save reuses the chunks.
	/// </summary>
	internal sealed class SaveBuffer : Stream {
		private const int ChunkSize = 1 << 20;
		private const int MaxPooledChunks = 512; //512 MB kept at most between saves

		private static readonly Stack<byte[]> pool = new Stack<byte[]>();

		private readonly List<byte[]> chunks = new List<byte[]>();
		private int usedInLast = ChunkSize;
		private long length;

		public override bool CanRead {
			get { return false; }
		}

		public override bool CanSeek {
			get { return false; }
		}

		public override bool CanWrite {
			get { return true; }
		}

		public override long Length {
			get { return this.length; }
		}

		public override long Position {
			get { return this.length; }
			set { throw new NotSupportedException(); }
		}

		public override void Write(byte[] buffer, int offset, int count) {
			this.Write(new ReadOnlySpan<byte>(buffer, offset, count));
		}

		public override void Write(ReadOnlySpan<byte> buffer) {
			while (buffer.Length > 0) {
				if (this.usedInLast == ChunkSize) {
					this.chunks.Add(Rent());
					this.usedInLast = 0;
				}
				int n = Math.Min(buffer.Length, ChunkSize - this.usedInLast);
				buffer.Slice(0, n).CopyTo(new Span<byte>(this.chunks[this.chunks.Count - 1], this.usedInLast, n));
				this.usedInLast += n;
				this.length += n;
				buffer = buffer.Slice(n);
			}
		}

		public override void WriteByte(byte value) {
			Span<byte> one = stackalloc byte[1];
			one[0] = value;
			this.Write(one);
		}

		/// <summary>Writes every byte held to `destination`, in order.</summary>
		public void CopyBytesTo(Stream destination) {
			for (int i = 0; i < this.chunks.Count; i++) {
				destination.Write(this.chunks[i], 0, (i == this.chunks.Count - 1) ? this.usedInLast : ChunkSize);
			}
		}

		/// <summary>The bytes as text, for a target that only takes text.</summary>
		public string GetText(Encoding encoding) {
			using (MemoryStream all = new MemoryStream(checked((int) this.length))) {
				this.CopyBytesTo(all);
				return encoding.GetString(all.GetBuffer(), 0, (int) all.Length);
			}
		}

		/// <summary>Gives the chunks back to the pool; the buffer is empty after.</summary>
		public void Release() {
			lock (pool) {
				foreach (byte[] chunk in this.chunks) {
					if (pool.Count < MaxPooledChunks) {
						pool.Push(chunk);
					}
				}
			}
			this.chunks.Clear();
			this.usedInLast = ChunkSize;
			this.length = 0;
		}

		private static byte[] Rent() {
			lock (pool) {
				if (pool.Count > 0) {
					return pool.Pop();
				}
			}
			return new byte[ChunkSize];
		}

		public override void Flush() {
		}

		public override int Read(byte[] buffer, int offset, int count) {
			throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength(long value) {
			throw new NotSupportedException();
		}
	}
}
