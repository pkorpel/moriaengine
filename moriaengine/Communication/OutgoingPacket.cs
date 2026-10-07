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
using System.Text;
using SteamEngine.Common;

namespace SteamEngine.Communication {
	public abstract class OutgoingPacket : Poolable {
		private byte[] buffer;
		private int offset;
		private int position;
		private int rightmostPosition;

		public abstract byte Id { get; }

		public virtual string Name {
			get {
				return Tools.TypeToString(this.GetType());
			}
		}

		protected override void On_Reset() {
			base.On_Reset();
		}

		public int Write(byte[] bytes, int offset) {
			this.buffer = bytes;
			this.offset = offset;
			this.position = offset;
			this.rightmostPosition = offset;

			this.EncodeByte(this.Id);
			this.Write();

			int retVal = this.rightmostPosition - offset;
			Sanity.IfTrueThrow(retVal < 0, "OutgoingPacket.Write: lastPosition < start. This should not happen.");
			return retVal;
		}

		public int CurrentSize {
			get {
				return this.rightmostPosition - this.offset;
			}
		}

		protected abstract void Write();

		public virtual string FullName {
			get {
				return string.Concat(this.Name, " ( 0x", this.Id.ToString("x"), " )");
			}
		}

		public override string ToString() {
			return "0x" + this.Id.ToString("X");
		}

		public void SeekFromStart(int count) {
			this.position = this.offset + count;
			this.rightmostPosition = Math.Max(this.position, this.rightmostPosition);
		}

		public void SeekFromCurrent(int count) {
			this.position += count;
			this.rightmostPosition = Math.Max(this.position, this.rightmostPosition);
		}

		protected void EncodeBytes(byte[] array) {
			int len = array.Length;
			System.Buffer.BlockCopy(array, 0, this.buffer, this.position, len);
			this.SeekFromCurrent(len);
		}

		//protected void EncodeBytes(byte[] array, int length) {
		//	this.position += len;
		//    System.Buffer.BlockCopy(array, 0, this.buffer, this.position, length);
		//}

		protected void EncodeBytesReversed(byte[] array) {
			int len = array.Length - 1;
			for (int i = 0; i <= len; i++) {
				this.buffer[this.position + i] = array[len - i];
			}
			this.SeekFromCurrent(len + 1);
		}

		/**
		    Encodes the string as unicode data at the specified start position, and returns the number of bytes
		    written.
			
		    You can do something like this, if you want: blockSize+=EncodeUnicodeString(line, blockSize);
			
		    But you can't do that with the EncodeUnicodeString method which takes a maxlen parameter.

		    @returns the number of bytes written, which is actually always double the length of the string.
			
		    Example:
		        int numBytes = EncodeUnicodeString("foo", 0);
		        That writes "foo" in unicode, which takes 6 bytes, so numBytes would be 6.
		*/
		protected void EncodeBigEndianUnicodeString(string value) {
			value = string.Concat(value);
			int len = value.Length;
			this.SeekFromCurrent(Encoding.BigEndianUnicode.GetBytes(value, 0, len, this.buffer, this.position));
		}

		protected void EncodeLittleEndianUnicodeString(string value) {
			value = string.Concat(value);
			int len = value.Length;
			this.SeekFromCurrent(Encoding.Unicode.GetBytes(value, 0, len, this.buffer, this.position));
		}

		protected void EncodeLittleEndianUnicodeStringWithLen(string value) {
			value = string.Concat(value);
			int bytesCount = Encoding.Unicode.GetBytes(value, 0, value.Length, this.buffer, this.position + 2);
			this.EncodeUShort((ushort) bytesCount);
			this.SeekFromCurrent(bytesCount);
		}

		/**
		    Encodes the string as unicode data at the specified start position, 
		    with the specified maximum length (in characters, NOT bytes),
		    and if less characters are written than the maximum length, then nulls are written
		    to fill up the space.
		*/
		protected void EncodeBigEndianUnicodeString(string value, int maxlen) {
			value = string.Concat(value);
			int len = Math.Min(value.Length, maxlen);
			int written = Encoding.BigEndianUnicode.GetBytes(value, 0, len, this.buffer, this.position);
			this.SeekFromCurrent(written);
			this.EncodeZeros(maxlen - written);
		}

		protected void EncodeLittleEndianUnicodeString(string value, int maxlen) {
			value = string.Concat(value);
			int len = Math.Min(value.Length, maxlen);
			int written = Encoding.Unicode.GetBytes(value, 0, (maxlen > len ? len : maxlen), this.buffer, this.position);
			this.SeekFromCurrent(written);
			this.EncodeZeros(maxlen - written);
		}

		protected void EncodeASCIIStringTerminated(string value) {
			value = string.Concat(value);
			this.SeekFromCurrent(Encoding.ASCII.GetBytes(value, 0, value.Length, this.buffer, this.position));
			this.EncodeByte(0);
		}

		protected void EncodeASCIIString(string value, int maxlen) {
			value = string.Concat(value);
			int len = Math.Min(value.Length, maxlen);
			int written = Encoding.ASCII.GetBytes(value, 0, len, this.buffer, this.position);
			this.SeekFromCurrent(written);
			this.EncodeZeros(maxlen - written);
		}

		protected void EncodeASCIIStringWithLenByte(string value) {
			value = string.Concat(value);
			int len = value.Length;
			this.EncodeByte((byte) len);
			Encoding.ASCII.GetBytes(value, 0, len, this.buffer, this.position);
			this.SeekFromCurrent(len);
		}

		static readonly byte[] zeroBytes = new byte[Buffer.bufferLen];

		//This method is used to encode more than one zeros. You should use this instead of any of the other
		//encode methods, because this is much faster because it doesn't have to do >>s. Additionally, if you're
		//doing more than 4 zeros, it is also faster than repeatedly calling EncodeByte.
		protected void EncodeZeros(int amount) {
			System.Buffer.BlockCopy(zeroBytes, 0, this.buffer, this.position, amount);
			this.SeekFromCurrent(amount);
		}

		protected void EncodeInt(int value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value >> 24);	//first byte
			packet[startpos + 1] = (byte) (value >> 16);	//second byte
			packet[startpos + 2] = (byte) (value >> 8);	//third byte
			packet[startpos + 3] = (byte) (value);		//fourth byte
			this.SeekFromCurrent(4);
		}

		[CLSCompliant(false)]
		protected void EncodeUInt(uint value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value >> 24);	//first byte
			packet[startpos + 1] = (byte) (value >> 16);	//second byte
			packet[startpos + 2] = (byte) (value >> 8);	//third byte
			packet[startpos + 3] = (byte) (value);		//fourth byte
			this.SeekFromCurrent(4);
		}

		protected void EncodeShort(short value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value >> 8);		//first byte
			packet[startpos + 1] = (byte) (value);		//second byte
			this.SeekFromCurrent(2);
		}

		[CLSCompliant(false)]
		protected void EncodeUShort(ushort value) {
			byte[] packet = this.buffer;
			int startpos = this.position;
			packet[startpos] = (byte) (value >> 8);		//first byte
			packet[startpos + 1] = (byte) (value);		//second byte
			this.SeekFromCurrent(2);
		}

		[CLSCompliant(false)]
		protected void EncodeSByte(sbyte value) {
			this.buffer[this.position] = (byte) value;
			this.SeekFromCurrent(1);
		}

		protected void EncodeByte(byte value) {
			this.buffer[this.position] = value;
			this.SeekFromCurrent(1);
		}

		protected void EncodeBool(bool value) {
			this.buffer[this.position] = (byte) (value ? 1 : 0);
			this.SeekFromCurrent(1);
		}

		//non-UO
		protected void EncodeUTF8String(string value) {
			int valueLength = value.Length;
			int encodedLength = Encoding.UTF8.GetBytes(value, 0, valueLength, this.buffer, this.position + 4);
			this.EncodeInt(encodedLength);
			this.SeekFromCurrent(encodedLength);
		}
	}
}
