using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;
using SteamEngine.Communication;
using Buffer = SteamEngine.Communication.Buffer;

namespace SteamEngine.AuxiliaryServer.LoginServer {
	public class LoginEncryption : IEncryption {
		private string name;
		private uint key1;
		private uint key2;
		private uint table1;
		private uint table2;

		public override string ToString() {
			return this.name + " encryption";
		}


		[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "offsetIn+64"),
	   SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "offsetIn+4"),
	   SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "offsetIn+34")]
		public EncryptionInitResult Init(byte[] bytesIn, int offsetIn, int lengthIn, out int bytesUsed) {
			bytesUsed = 0;
			if (lengthIn < 66) {
				return EncryptionInitResult.NotEnoughData;
			}


			if ((bytesIn[offsetIn + 4] == 0x80 || bytesIn[offsetIn + 4] == 0xcf) && bytesIn[offsetIn + 34] == 0x00 && bytesIn[offsetIn + 64] == 0x00) {
				if (CheckCorrectASCIIString(bytesIn, 5, 30) && CheckCorrectASCIIString(bytesIn, 35, 30)) {
					bytesUsed = 4;
					return EncryptionInitResult.SuccessNoEncryption;
				}
			}

			if (this.InitEncrypion(bytesIn, offsetIn, lengthIn)) {
				bytesUsed = 4;
				return EncryptionInitResult.SuccessUseEncryption;
			}
			return EncryptionInitResult.InvalidData;
		}

		private bool InitEncrypion(byte[] buffer, int offset, int length) {
			uint seed = (uint) ((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]);

			// Try to find a valid key

			// Initialize our tables (cache them, they will be modified)
			uint orgTable1 = (((~seed) ^ 0x00001357) << 16) | ((seed ^ 0xffffaaaa) & 0x0000ffff);
			uint orgTable2 = ((seed ^ 0x43210000) >> 16) | (((~seed) ^ 0xabcdffff) & 0xffff0000);

			using (Buffer b = Pool<Buffer>.Acquire()) {
				byte[] bytes = b.bytes;

				for (int i = 0, n = LoginKey.loginKeys.Length; i < n; i++) {
					this.table1 = orgTable1;
					this.table2 = orgTable2;
					this.key1 = (uint) LoginKey.loginKeys[i].Key1;
					this.key2 = (uint) LoginKey.loginKeys[i].Key2;


					this.Decrypt(buffer, 4, bytes, 0, length - 4);

					// Check if it decrypted correctly
					if ((bytes[0] == 0x80 || bytes[0] == 0xcf) && CheckCorrectASCIIString(bytes, 1, 30) && CheckCorrectASCIIString(bytes, 31, 30)) {
						// Reestablish our current state
						this.table1 = orgTable1;
						this.table2 = orgTable2;
						this.key1 = (uint) LoginKey.loginKeys[i].Key1;
						this.key2 = (uint) LoginKey.loginKeys[i].Key2;
						this.name = LoginKey.loginKeys[i].Name;
						return true;
					}
				}
			}

			return false;
		}

		private static bool CheckCorrectASCIIString(byte[] bytes, int start, int len) {
			bool nullsFromNowOn = false;
			for (int i = start, n = start + len; i < n; i++) {
				byte value = bytes[i];
				if (nullsFromNowOn) {
					if (value != 0) {
						return false;
					}
				} else if (value == 0) {
					nullsFromNowOn = true;
				} else if (value < 32 || value == 127) { //unprintable ASCII
					return false;
				}
			}
			return true;
		}

		public int Encrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {
			// There is no server->client encryption in the login stage

			System.Buffer.BlockCopy(bytesIn, offsetIn, bytesOut, offsetOut, length);

			return length;
		}

		public int Decrypt(byte[] bytesIn, int offsetIn, byte[] bytesOut, int offsetOut, int length) {
			uint eax, ecx, edx, esi;

			for (int i = 0; i < length; i++) {
				bytesOut[i] = (byte) (bytesIn[offsetIn + i] ^ (byte) (this.table1 & 0xFF));
				edx = this.table2;
				esi = this.table1 << 31;
				eax = this.table2 >> 1;
				eax |= esi;
				eax ^= this.key1 - 1;
				edx <<= 31;
				eax >>= 1;
				ecx = this.table1 >> 1;
				eax |= esi;
				ecx |= edx;
				eax ^= this.key1;
				ecx ^= this.key2;
				this.table1 = ecx;
				this.table2 = eax;
			}

			return length;
		}
	}
}