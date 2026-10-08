using System.Security.Cryptography;
using SteamEngine.Networking;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Mirrors GameCore/Networking/GameEncryption.cs exactly, but playing the CLIENT's role
	/// instead of the server's (confirmed identical to ClassicUO's own TwofishBehaviour.cs by
	/// reading its source directly - same key derivation, same table construction, same roles
	/// just swapped). Built to test a real, properly-seeded Twofish-MD5 handshake end to end
	/// against this server, since the "no encryption" fast path turned out to be unreachable
	/// from any modern ClassicUO-family client (EncryptionHelper.CalculateEncryption never
	/// returns EncryptionType.NONE for any configured client version).
	///
	/// Direction naming here follows the *server's* convention from GameEncryption.cs:
	/// "Encrypt" = server->client direction (so this class's ClientDecrypt mirrors the
	/// server's Encrypt), "Decrypt" = client->server direction (so ClientEncrypt mirrors the
	/// server's Decrypt).
	/// </summary>
	internal sealed class ClientGameCipher {
		private readonly TwofishEncryption engine;
		private readonly byte[] cipherTable = new byte[0x100];
		private readonly byte[] xorData;
		private ushort recvPos;
		private byte sendPos;

		public ClientGameCipher(byte[] seed4Bytes) {
			byte[] key = new byte[16];
			key[0] = key[4] = key[8] = key[12] = seed4Bytes[0];
			key[1] = key[5] = key[9] = key[13] = seed4Bytes[1];
			key[2] = key[6] = key[10] = key[14] = seed4Bytes[2];
			key[3] = key[7] = key[11] = key[15] = seed4Bytes[3];

			byte[] iv = new byte[0];
			this.engine = new TwofishEncryption(128, ref key, ref iv, CipherMode.ECB, TwofishBase.EncryptionDirection.Decrypting);

			for (int i = 0; i < 256; ++i) {
				this.cipherTable[i] = (byte) i;
			}
			this.RefreshCipherTable();

			MD5 md5 = MD5.Create();
			this.xorData = md5.ComputeHash(this.cipherTable);
		}

		private void RefreshCipherTable() {
			uint[] block = new uint[4];
			for (int i = 0; i < 256; i += 16) {
				System.Buffer.BlockCopy(this.cipherTable, i, block, 0, 16);
				this.engine.blockEncrypt(ref block);
				System.Buffer.BlockCopy(block, 0, this.cipherTable, i, 16);
			}
			this.recvPos = 0;
		}

		/// <summary>Client -> server direction (mirrors the server's own Decrypt).</summary>
		public void ClientEncrypt(byte[] bytesIn, byte[] bytesOut) {
			for (int i = 0; i < bytesIn.Length; i++) {
				if (this.recvPos >= 0x100) {
					this.RefreshCipherTable();
				}
				bytesOut[i] = (byte) (bytesIn[i] ^ this.cipherTable[this.recvPos++]);
			}
		}

		/// <summary>Server -> client direction (mirrors the server's own Encrypt).</summary>
		public void ClientDecrypt(byte[] bytesIn, int offset, byte[] bytesOut, int length) {
			for (int i = 0; i < length; i++) {
				bytesOut[i] = (byte) (bytesIn[offset + i] ^ this.xorData[this.sendPos]);
				this.sendPos++;
				this.sendPos &= 0x0F;
			}
		}
	}
}
