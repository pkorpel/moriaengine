using System;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Unlike LoginHandshakeTests (which deliberately uses the server's "no encryption" fast
	/// path), this exercises a REAL, properly-seeded Twofish-MD5-encrypted game-server login -
	/// the path every modern ClassicUO-family client (TazUO included) actually takes, since
	/// EncryptionHelper.CalculateEncryption in ClassicUO's own source never produces
	/// EncryptionType.NONE for any client version. This is here specifically to answer: does
	/// our server round-trip a *correctly* encrypted packet, or is something broken server-side
	/// regardless of what the client does?
	/// </summary>
	[Collection(GameServerPairCollection.Name)]
	public class RealEncryptionHandshakeTests {
		// GameCore only ever auto-creates an account the first time any account connects in a
		// given server session (that's how the bootstrap admin account is created) - any other
		// unrecognized account name is flatly rejected ("No account 'x'"). Reusing the same
		// account LoginHandshakeTests uses means whichever test runs first creates it and the
		// other just logs into the existing one, regardless of xUnit's execution order.
		private const string TestAccount = "protocoltest";
		private const string TestPassword = "protocoltest";

		public RealEncryptionHandshakeTests(GameServerPairFixture servers) {
		}

		[Fact]
		public void RealTwofishEncryptedGameLoginRoundTrips() {
			byte[] seed;
			int redirectPort;

			Exception lastFailure = null;
			(int, byte[]) redirect = default;
			bool got = false;
			for (int attempt = 0; attempt < 20 && !got; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(TestAccount, TestPassword);
						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet.");
						}
						login.SendServerSelect(0);
						redirect = login.ReadRedirect();
						got = true;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					System.Threading.Thread.Sleep(250);
				}
			}
			if (!got) {
				throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
			}
			(redirectPort, seed) = redirect;

			var cipher = new ClientGameCipher(seed);

			using (var tcp = new TcpClient()) {
				tcp.Connect("127.0.0.1", redirectPort);
				var stream = tcp.GetStream();

				// The 4-byte seed itself is never encrypted - it's what both sides derive the
				// cipher from in the first place.
				stream.Write(seed, 0, 4);

				byte[] packet = new byte[65]; // 0x91 + 4-byte echo-seed + 30 account + 30 password
				packet[0] = 0x91;
				Array.Copy(seed, 0, packet, 1, 4);
				EncodeAsciiFixed(TestAccount, packet, 5, 30);
				EncodeAsciiFixed(TestPassword, packet, 35, 30);

				byte[] encrypted = new byte[packet.Length];
				cipher.ClientEncrypt(packet, encrypted);
				stream.Write(encrypted, 0, encrypted.Length);

				byte[] decoded = ReadOneCompressedFrame(stream, cipher);

				Assert.True(decoded.Length > 0, "Got an empty response.");
				Assert.Equal(0xA9, decoded[0]); // CharactersListOutPacket - not "Unknown packet"
			}
		}

		private static byte[] ReadOneCompressedFrame(NetworkStream stream, ClientGameCipher cipher) {
			var rawBuffer = new System.Collections.Generic.List<byte>();
			var decryptedBuffer = new System.Collections.Generic.List<byte>();

			while (true) {
				int b = stream.ReadByte();
				if (b < 0) {
					throw new InvalidOperationException("Connection closed after " + rawBuffer.Count + " raw bytes, before a full frame arrived. Decrypted so far: " +
						BitConverter.ToString(decryptedBuffer.ToArray()));
				}
				rawBuffer.Add((byte) b);

				byte[] oneDecrypted = new byte[1];
				cipher.ClientDecrypt(new[] { (byte) b }, 0, oneDecrypted, 1);
				decryptedBuffer.Add(oneDecrypted[0]);

				try {
					byte[] decryptedSoFar = decryptedBuffer.ToArray();
					var (decoded, bytesConsumed) = HuffmanDecompressor.DecompressOneFrame(decryptedSoFar, 0, decryptedSoFar.Length);
					if (bytesConsumed == decryptedSoFar.Length) {
						return decoded;
					}
				} catch (InvalidOperationException) {
					// Not a complete frame yet.
				}
			}
		}

		private static void EncodeAsciiFixed(string text, byte[] buffer, int offset, int fieldLength) {
			byte[] ascii = Encoding.ASCII.GetBytes(text ?? "");
			int copyLen = Math.Min(ascii.Length, fieldLength - 1);
			Array.Copy(ascii, 0, buffer, offset, copyLen);
		}
	}
}
