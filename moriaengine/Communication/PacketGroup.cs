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

using System.Collections.Generic;
using System.Text;
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication {
	public enum PacketGroupType {
		SingleUse,
		MultiUse,
		Free
	}

	public sealed class PacketGroup : Disposable {
		Buffer uncompressed;
		Buffer compressed;

		List<OutgoingPacket> packets = new List<OutgoingPacket>();

		private bool isWritten;
		private bool compressionDone;
		private int isQueued;
		private bool isEmpty;

		private bool isMadeFree;

		int uncompressedLen;
		int compressedLen;

		internal PacketGroupType type;

		private PacketGroup() {
			this.isWritten = false;
			this.compressionDone = false;
			this.isQueued = 0;
			this.type = PacketGroupType.SingleUse;
			this.isMadeFree = false;
			this.isEmpty = true;

			//this.uncompressed = Pool<Buffer>.Acquire();
			//this.compressed = Pool<Buffer>.Acquire();
		}

		public void SetType(PacketGroupType type) {
			if (this.isMadeFree) {
				throw new SEException("Can't change type once the group is made free");
			}
			this.type = type;
		}

		public void AddPacket(OutgoingPacket packet) {
			Sanity.IfTrueSay((this.isQueued > 0 || this.compressionDone || this.isWritten), "Can't add new packets to a locked group. They're ignored.");
			this.packets.Add(packet);
			this.isEmpty = false;
		}

		public T AcquirePacket<T>() where T : OutgoingPacket, new() {
			T packet = Pool<T>.Acquire();
			this.AddPacket(packet);
			return packet;
		}


		public bool SafeAddGroup(PacketGroup addedGroup) {
			Sanity.IfTrueThrow(addedGroup.isWritten, "addedGroup.isWritten");
			Sanity.IfTrueThrow(addedGroup.isQueued > 0, "addedGroup.isQueued = " + addedGroup.isQueued);
			Sanity.IfTrueThrow(addedGroup.isMadeFree, "addedGroup.isMadeFree");

			this.WritePackets();
			if (this.uncompressedLen < Buffer.bufferLen / 2) {

				//write the other group's packet to our buffer
				foreach (OutgoingPacket packet in addedGroup.packets) {
					this.packets.Add(packet);
					this.uncompressedLen += packet.Write(this.uncompressed.bytes, this.uncompressedLen);
				}
				return true;
			}
			return false;
		}

		private void WritePackets() {
			if (!this.isWritten) {
				this.uncompressed = Pool<Buffer>.Acquire();

				int position = 0;
				foreach (OutgoingPacket packet in this.packets) {
					position += packet.Write(this.uncompressed.bytes, position);
				}

				this.uncompressedLen = position;

				this.isWritten = true;
			}
		}

		internal int GetFinalBytes(ICompression compression, out byte[] bytes) {
			this.ThrowIfDisposed();

			this.WritePackets();

			if (!this.compressionDone) {
				if (compression != null) {
					this.compressed = Pool<Buffer>.Acquire();

					this.compressedLen = compression.Compress(
						this.uncompressed.bytes, 0, this.compressed.bytes, 0, this.uncompressedLen);

					this.uncompressed.Dispose();
					this.uncompressed = null;
				} else {
					this.compressed = this.uncompressed;
					this.compressedLen = this.uncompressedLen;

					this.uncompressed = null;
				}
				this.compressionDone = true;
			}

			if (this.type == PacketGroupType.Free) {
				Buffer newCompressed = new Buffer(this.compressedLen);
				System.Buffer.BlockCopy(this.compressed.bytes, 0, newCompressed.bytes, 0, this.compressedLen);

				this.compressed.Dispose();
				this.compressed = null;

				foreach (OutgoingPacket packet in this.packets) {
					packet.Dispose();
				}
				this.packets.Clear();

				this.compressed = newCompressed;

				this.isMadeFree = true;

				this.type = PacketGroupType.MultiUse;
			}

			bytes = this.compressed.bytes;

			return this.compressedLen;
		}

		public override void Dispose() {
			if (this.isQueued > 0) {
				this.SetType(PacketGroupType.SingleUse);
			} else {
				base.Dispose();
			}
		}

		internal void Enqueued() {
			Interlocked.Increment(ref this.isQueued);
		}

		internal void Dequeued() {
			Interlocked.Decrement(ref this.isQueued);
			if (this.isQueued < 1) {
				if (this.type == PacketGroupType.SingleUse) {
					base.Dispose();
				}
			}
		}

		protected override void On_DisposeManagedResources() {
			if (this.compressed != null) {
				this.compressed.Dispose();
				this.compressed = null;
			}
			if (this.uncompressed != null) {
				this.uncompressed.Dispose();
				this.uncompressed = null;
			}

			foreach (OutgoingPacket packet in this.packets) {
				packet.Dispose();
			}
			//this.packets.Clear();

			base.On_DisposeManagedResources();
		}

		public static PacketGroup CreateFreePG() {
			PacketGroup pg = new PacketGroup();
			pg.SetType(PacketGroupType.Free);
			return pg;
		}

		public static PacketGroup AcquireMultiUsePG() {
			PacketGroup pg = new PacketGroup();
			pg.SetType(PacketGroupType.MultiUse);
			return pg;
		}

		public static PacketGroup AcquireSingleUsePG() {
			PacketGroup pg = new PacketGroup();
			pg.SetType(PacketGroupType.SingleUse);
			return pg;
		}

		public bool IsEmpty {
			get {
				return this.isEmpty;
			}
		}

		public override string ToString() {
			StringBuilder sb = new StringBuilder("PacketGroup (");
			sb.Append(this.packets.Count).Append(" packets - ");
			foreach (OutgoingPacket p in this.packets) {
				sb.Append(p).Append(", ");
			}
			sb.Length -= 2;
			sb.Append(")");
			return sb.ToString();
		}
	}
}
