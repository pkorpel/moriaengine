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
using System.Threading;
using SteamEngine.Common;

namespace SteamEngine.Communication {
	public abstract class AbstractConnection<TConnection, TState, TEndPoint> : Poolable
		where TConnection : AbstractConnection<TConnection, TState, TEndPoint>, new()
		where TState : IConnectionState<TConnection, TState, TEndPoint>, new() {

		private AsyncCore<TConnection, TState, TEndPoint> core;

		protected Buffer receivingBuffer;
		private Buffer decryptBuffer;
		private Buffer decompressBuffer;

		protected int receivedDataLength;

		private int decompressedDataOffset;
		private int decompressedDataLength;
		private bool encryptionInitialised;
		private bool useEncryption;

		private TState state;

		public AbstractConnection() {
			if (this.GetType() != typeof(TConnection)) {
				throw new SEException("The type must be the same as the TConnection generic parameter");
			}

			this.joinedPGTimer = new Timer(this.JoinedPGTimerMethod, null, joinedPGintervalInMS, Timeout.Infinite);
		}

		public AsyncCore<TConnection, TState, TEndPoint> Core {
			get {
				return this.core;
			}
		}

		protected override void On_Reset() {
			this.receivingBuffer = Pool<Buffer>.Acquire();
			this.decryptBuffer = Pool<Buffer>.Acquire();
			this.decompressBuffer = Pool<Buffer>.Acquire();
			this.decompressedDataOffset = 0;
			this.decompressedDataLength = 0;

			this.receivedDataLength = 0;

			this.encryptionInitialised = false;
			this.useEncryption = true;

			base.On_Reset();
		}

		public TState State {
			get {
				return this.state;
			}
		}

		public sealed override void Dispose() {
			this.Close("Dispose() called");
		}

		protected override void On_DisposeManagedResources() {
			this.receivingBuffer.Dispose();
			this.receivingBuffer = null;
			this.decryptBuffer.Dispose();
			this.decryptBuffer = null;
			this.decompressBuffer.Dispose();
			this.decompressBuffer = null;

			this.core = null;

			base.On_DisposeManagedResources();
		}

		public void Close(string reason) {

			if (!this.IsDisposed) {
				lock (this.core.LockObject) {
					this.state.On_Close(reason);
				}
				//this.state.Dispose();
			}
			//On_Close(reason);
			base.Dispose();
		}

		//public virtual void On_Close(string reason) {

		//}

		public abstract bool IsConnected { get; }

		public abstract TEndPoint EndPoint { get; }


		internal void Init(AsyncCore<TConnection, TState, TEndPoint> core) {
			this.core = core;
			this.On_Init();
		}

		protected virtual void On_Init() {
			this.state = new TState();
			this.state.On_Init((TConnection) this);
		}

		#region Receiving data
		protected void ProcessReceievedData(int length) {
			//ss.receievedDataLength += newBytesLength;
			byte[] bytes = this.receivingBuffer.bytes;
			length += this.receivedDataLength;
			int offset = 0;

			if (this.ProcessDecryption(ref length, ref bytes, ref offset)) {
				ICompression compression = this.State.Compression;
				if (compression != null) {
					length = compression.Decompress(bytes, offset, this.decompressBuffer.bytes,
						this.decompressedDataOffset + this.decompressedDataLength, length);
				} else {
					System.Buffer.BlockCopy(bytes, offset, this.decompressBuffer.bytes,
						this.decompressedDataOffset + this.decompressedDataLength, length);
				}

				bytes = this.decompressBuffer.bytes;
				offset = this.decompressedDataOffset;
				length += this.decompressedDataLength;


				//read all posible packets outa those data
				bool loop = true;
				while (loop && (length > 0)) {
					byte id = bytes[offset];
					bool discardAfterReading;
					IncomingPacket<TConnection, TState, TEndPoint> packet = this.core.protocol.GetPacketImplementation(id, (TConnection) this, this.state, out discardAfterReading);
					length--;
					offset++;

					int read;
					ReadPacketResult result;
					if (packet != null) {
						result = packet.Read(bytes, offset, length, out read);
					} else {
						result = ReadPacketResult.DiscardAll;
						read = 0;
						Logger.WriteDebug("Unknown packet 0x" + id.ToString("x"));

						CommunicationUtils.OutputPacketLog(bytes, offset, length);
					}

					switch (result) {
						case ReadPacketResult.NeedMoreData:
							length++;
							offset--;
							loop = false;
							break;
						case ReadPacketResult.Success:
							if (discardAfterReading) {
								Logger.WriteDebug("Discarding packet 0x" + id.ToString("x"));
							} else {
								Logger.WriteDebug("Handling packet 0x" + id.ToString("x"));
								this.core.HandlePacket((TConnection) this, this.state, packet);
							}
							goto default;
						case ReadPacketResult.DiscardAll:
							Logger.WriteDebug("Discarding packet 0x" + id.ToString("x") + " (and all following)");
							read = length;
							goto default;
						case ReadPacketResult.DiscardSingle:
							Logger.WriteDebug("Discarding packet 0x" + id.ToString("x"));
							goto default;
						default:
							Sanity.IfTrueThrow(read < 0, "read bytes can't be negative!");
							if (read > 0) {
								offset += read;
								length -= read;
							}
							break;
					}
				}

				this.decompressedDataOffset = offset;
				this.decompressedDataLength = length;

				RecycleBufferIfNeeded(bytes, ref this.decompressedDataOffset, ref this.decompressedDataLength);
			} //else init failed with not enough data, we queue the data
		}

		private static void RecycleBufferIfNeeded(byte[] bytes, ref int offset, ref int length) {
			if (length > 0) {
				//if over half the buffer, we copy to the start
				if (offset > (Buffer.bufferLen / 2)) {
					System.Buffer.BlockCopy(bytes, offset, bytes, 0, length);
					offset = 0;
				} else if ((offset + length) == Buffer.bufferLen) {
					throw new SEException("Incoming data buffer full. This is bad.");
				}
			} else {//we read exactly what we had, perfect.
				offset = 0;
				length = 0;
			}
		}

		private bool ProcessDecryption(ref int length, ref byte[] bytes, ref int offset) {
			if (this.useEncryption) {
				IEncryption encryption = this.State.Encryption;
				if (encryption != null) {
					if (!this.encryptionInitialised) {
						int read;
						EncryptionInitResult result = encryption.Init(bytes, offset, length, out read);
						switch (result) {
							case EncryptionInitResult.SuccessUseEncryption:
								Logger.WriteDebug(this.State + " using " + encryption);
								offset += read;
								length -= read;
								this.receivedDataLength = 0;
								this.encryptionInitialised = true;
								break;
							case EncryptionInitResult.SuccessNoEncryption:
								Logger.WriteDebug(this.State + " using no encryption");
								offset += read;
								length -= read;
								this.receivedDataLength = 0;
								this.useEncryption = false;
								break;
							case EncryptionInitResult.InvalidData:
								throw new SEException("Encryption not recognised");
							case EncryptionInitResult.NotEnoughData:
								this.receivedDataLength = length;
								return false;
						}
					}

					if (this.useEncryption) {
						length = encryption.Decrypt(bytes, offset, this.decryptBuffer.bytes, 0, length);
						bytes = this.decryptBuffer.bytes;
						offset = 0;
					}
				}
			}
			return true;
		}
		#endregion Receiving data

		#region Sending data

		const int joinedPGintervalInMS = 333;
		//TimeSpan maxJoinedPGInterval = TimeSpan.FromMilliseconds(intervalInMS);
		PacketGroup joinedPG;

		Timer joinedPGTimer;

		//called from main loop
		public void SendPacketGroup(PacketGroup group) {
			this.ThrowIfDisposed();

			if (!group.IsEmpty) {
				if (!this.state.PacketGroupsJoiningAllowed) {
					this.core.EnqueueOutgoing((TConnection) this, group);
				} else {
					lock (this.joinedPGTimer) {
						if (this.joinedPG == null) {//no last PG. This one will be the first
							this.joinedPG = group;
						} else if (!this.joinedPG.SafeAddGroup(group)) {//last PG too big already. We send it and mark the new one
							this.SendJoinedPG();
							this.joinedPG = group;
						}
					}
				}
			} else {
				Logger.WriteWarning("Sending an empty PacketGroup? Ignoring."); //display stack trace?
			}
		}

		private void JoinedPGTimerMethod(object ignored) {
			try {
				if ((this.state == null) || (this.state.PacketGroupsJoiningAllowed)) {
					lock (this.joinedPGTimer) {
						if (this.joinedPG != null) {
							this.SendJoinedPG();
						}
					}
					this.joinedPGTimer.Change(joinedPGintervalInMS, Timeout.Infinite);
				} else {
					this.joinedPGTimer.Dispose();
				}
			} catch (Exception e) {
				Logger.WriteError("Unexpected error in timer callback method", e);
			}
		}

		private void SendJoinedPG() {
			PacketGroup group = this.joinedPG;
			this.joinedPG = null;
			if (this.IsConnected) {
				this.core.EnqueueOutgoing((TConnection) this, group);
			}
		}

		public void SendSinglePacket(OutgoingPacket packet) {
			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AddPacket(packet);
			this.SendPacketGroup(pg);
		}

		protected class BufferToSend {
			public Buffer buffer;
			public int offset, len;

			internal BufferToSend(Buffer buffer, int offset, int len) {
				this.buffer = buffer;
				this.offset = offset;
				this.len = len;
			}
		}

		//caled in worker thread
		internal void ProcessSending(PacketGroup group) {
			try {
				byte[] unencrypted;
				int unencryptedLen = group.GetFinalBytes(this.state.Compression, out unencrypted);

				int encryptedLen;
				Buffer encryptedBuffer = Pool<Buffer>.Acquire();

				if (this.useEncryption) {
					IEncryption encryption = this.state.Encryption;
					if (encryption != null) {
						if (this.encryptionInitialised) {
							encryptedLen = encryption.Encrypt(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
						} else {
							this.Close("Tried sending data with encryption not initialised.");
							throw new SEException("Sending failed - encryption not initialised. Closing.");
						}
					} else {
						System.Buffer.BlockCopy(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
						encryptedLen = unencryptedLen;
					}
				} else {
					System.Buffer.BlockCopy(unencrypted, 0, encryptedBuffer.bytes, 0, unencryptedLen);
					encryptedLen = unencryptedLen;
				}
				group.Dequeued();

				BufferToSend toSend = new BufferToSend(encryptedBuffer, 0, encryptedLen);
				this.BeginSend(toSend);

				
			} catch (Exception e) {
				Logger.WriteError("Exception while sending", e);
				this.Close(e.Message);
			}
		}

		protected abstract void BeginSend(BufferToSend toSend);

		#endregion Sending data
	}
}