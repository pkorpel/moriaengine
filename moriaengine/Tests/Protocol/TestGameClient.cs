using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Speaks the GameCore login-stage handshake just well enough to get a character list back:
	/// connect -> GameServerLoginInPacket (0x91) -> CharactersListOutPacket (0xA9).
	///
	/// Uses GameEncryption's "no encryption" fast path (the 4 bytes right after the packet ID
	/// must exactly echo the seed) the same way TestLoginClient does for the login stage - no
	/// Twofish needed. Server->client traffic here IS always Huffman-compressed though (see
	/// HuffmanDecompressor's own comment), unlike the login stage.
	/// </summary>
	internal sealed class TestGameClient : IDisposable {
		private readonly TcpClient tcp;
		private readonly NetworkStream stream;
		private readonly Queue<(byte Id, byte[] Payload)> packetQueue = new Queue<(byte, byte[])>();
		private readonly List<byte> seenIds = new List<byte>();

		public TestGameClient(string host, int port) {
			this.tcp = new TcpClient();
			this.tcp.Connect(host, port);
			this.stream = this.tcp.GetStream();
		}

		/// <summary>
		/// Payload byte count (everything after the id byte, NOT including it) for every
		/// fixed-size outgoing packet this test harness has had to recognize so far - derived
		/// mechanically from each class's own Write() method in
		/// GameCore/Networking/OutgoingPackets.cs (sum of its EncodeByte/EncodeUShort/EncodeUInt/
		/// EncodeZeros(n)/etc. calls), not guessed. Deliberately NOT exhaustive - scenario tests
		/// that hit an id missing from this table (or from <see cref="DynamicPacketIds"/>) get a
		/// clear "add it" exception from <see cref="FillQueueFromOneFrame"/> rather than silent
		/// stream corruption, so the table grows by actual need instead of upfront guesswork.
		/// </summary>
		private static readonly Dictionary<byte, int> FixedPacketPayloadSizes = new Dictionary<byte, int> {
			{ 0x1B, 36 }, // LoginConfirmationOutPacket
			{ 0x55, 0 },  // LoginCompleteOutPacket
			{ 0x20, 18 }, // DrawGamePlayerOutPacket
			{ 0x77, 16 }, // UpdatePlayerPacket
			{ 0x72, 4 },  // SetWarModeOutPacket
			{ 0xC8, 1 },  // ClientViewRangeOutPacket
			{ 0x2d, 16 }, // MobAttributesOutPacket
			{ 0xa1, 8 },  // UpdateCurrentHealthOutPacket
			{ 0xa2, 8 },  // UpdateCurrentManaOutPacket
			{ 0xa3, 8 },  // UpdateCurrentStaminaOutPacket
			{ 0x1d, 4 },  // DeleteObjectOutPacket
			{ 0x2e, 14 }, // WornItemOutPacket
			{ 0x6c, 18 }, // TargetCursorCommandsOutPacket (server->client "pick a target" prompt)
			{ 0xBC, 2 },  // SeasonalInformationOutPacket
			{ 0x4E, 5 },  // PersonalLightLevelOutPacket
			{ 0x4f, 1 },  // OverallLightLevelOutPacket
			{ 0x82, 1 },  // LoginDeniedOutPacket
			{ 0x6E, 13 }, // CharacterAnimationOutPacket
		};

		/// <summary>Wire ids whose payload is self-length-prefixed (2-byte big-endian total size,
		/// including the id byte, right after the id - DynamicLengthOutPacket's own framing).
		/// 0xBF covers every GeneralInformationOutPacket subclass (party/skills/etc. sub-commands
		/// all share that one wire id with a sub-command byte inside the payload).</summary>
		private static readonly HashSet<byte> DynamicPacketIds = new HashSet<byte> {
			0xA9, // CharactersListOutPacket
			0x78, // DrawObjectOutPacket
			0xb0, // SendGumpMenuDialogPacket
			0x11, // StatusBarInfoOutPacket
			0x6F, // SecureTradeOutPacket
			0x3c, // ItemsInContainerOutPacket
			0x1A, // ObjectInfoOutPacket
			0x3A, // SendSkillsOutPacket
			0x1c, // SendSpeechOutPacket
			0xAE, // UnicodeSpeechOutPacket
			0xBF, // GeneralInformationOutPacket (party, etc. - sub-command inside the payload)
			0xBD, // ClientVersionOutPacket (server requesting the client report its version)
		};

		/// <summary>
		/// Decodes one more Huffman frame off the wire and splits it into individual packets
		/// (a single frame/TCP read can carry several packets sent back-to-back - e.g. the whole
		/// world-entry sequence), queuing each as (id, payload-without-id-byte).
		/// </summary>
		private void FillQueueFromOneFrame() {
			byte[] frame = this.ReadOneCompressedFrame();
			int pos = 0;
			while (pos < frame.Length) {
				byte id = frame[pos];
				this.seenIds.Add(id);
				int payloadLen;
				if (DynamicPacketIds.Contains(id)) {
					int totalLen = (frame[pos + 1] << 8) | frame[pos + 2]; // includes the id byte itself
					payloadLen = totalLen - 1;
				} else if (FixedPacketPayloadSizes.TryGetValue(id, out int sz)) {
					payloadLen = sz;
				} else {
					throw new InvalidOperationException(
						"Unrecognized fixed-size outgoing packet 0x" + id.ToString("x2") +
						" - add its payload size to TestGameClient.FixedPacketPayloadSizes (derive it from " +
						"the class's own Write() method in GameCore/Networking/OutgoingPackets.cs).");
				}
				byte[] payload = new byte[payloadLen];
				Array.Copy(frame, pos + 1, payload, 0, payloadLen);
				this.packetQueue.Enqueue((id, payload));
				pos += 1 + payloadLen;
			}
		}

		/// <summary>
		/// Reads (and discards) packets off the wire until one with <paramref name="expectedId"/>
		/// arrives, returning its payload (everything after the id byte - for a dynamic packet
		/// this still includes its own 2-byte length field, same convention ReadCharacterList
		/// already uses). Throws TimeoutException, listing every id actually seen, if none arrives
		/// in time - real UO traffic during world-entry/resync is a mix of many packet types, so
		/// "nothing matched" needs to be diagnosable, not a bare timeout.
		/// </summary>
		public byte[] ReadPacketPayload(byte expectedId, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (true) {
				if (this.packetQueue.Count > 0) {
					var (id, payload) = this.packetQueue.Dequeue();
					if (id == expectedId) {
						return payload;
					}
					continue;
				}
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					throw new TimeoutException("Did not see packet 0x" + expectedId.ToString("x2") +
						" within " + timeout + ". Ids actually seen: " +
						string.Join(",", this.seenIds.ConvertAll(b => "0x" + b.ToString("x2"))));
				}
				this.stream.ReadTimeout = Math.Max(1, (int) remaining.TotalMilliseconds);
				try {
					this.FillQueueFromOneFrame();
				} catch (IOException e) {
					throw new TimeoutException("Did not see packet 0x" + expectedId.ToString("x2") +
						" within " + timeout + ". Ids actually seen: " +
						string.Join(",", this.seenIds.ConvertAll(b => "0x" + b.ToString("x2"))), e);
				}
			}
		}

		public void SendLogin(string account, string password) {
			byte[] packet = new byte[69];
			// bytes 0-3: seed, left zero; bytes 5-8 must echo it exactly (both zero here too).
			packet[4] = 0x91;
			EncodeAsciiFixed(account, packet, 9, 30);
			EncodeAsciiFixed(password, packet, 39, 30);
			this.stream.Write(packet, 0, packet.Length);

			// A real client reports its version unprompted right after logging in (and again
			// whenever the server's own 0xbd query asks) - GameState.Version stays
			// ClientVersion.nullValue until this arrives, and PacketSequences.DelayedLoginTimer
			// (the thing that eventually sends LoginConfirmationOutPacket/0x1B) silently
			// re-schedules itself forever rather than proceeding while that's true. Sending this
			// immediately, unprompted, avoids every single scenario test needing to remember a
			// separate step just to not hang forever waiting for world-entry packets that will
			// never come.
			this.SendClientVersion("7.0.15.1");
		}

		/// <summary>Sends ClientVersionInPacket (0xbd) - see SendLogin's own comment for why this
		/// has to happen before the server will proceed past character creation into the world.</summary>
		public void SendClientVersion(string version) {
			byte[] verBytes = Encoding.ASCII.GetBytes(version);
			byte[] packet = new byte[3 + verBytes.Length];
			packet[0] = 0xbd;
			EncodeUShortBigEndian(packet.Length, packet, 1);
			Array.Copy(verBytes, 0, packet, 3, verBytes.Length);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Reads one Huffman-compressed frame and returns the decoded character names (nulls for empty slots).</summary>
		public string[] ReadCharacterList() {
			byte[] decoded = ReadOneCompressedFrame();

			if (decoded.Length < 3 || decoded[0] != 0xA9) {
				throw new InvalidOperationException("Expected CharactersListOutPacket (0xA9), got " +
					(decoded.Length == 0 ? "no data" : "0x" + decoded[0].ToString("x2")));
			}

			const int maxCharacters = 5; // AbstractAccount.maxCharactersPerGameAccount
			var names = new string[maxCharacters];
			int pos = 3 + 1; // skip id(1) + length(2) + numOfCharacters(1)
			for (int i = 0; i < maxCharacters; i++) {
				string name = Encoding.ASCII.GetString(decoded, pos, 30).TrimEnd('\0');
				names[i] = name.Length > 0 ? name : null;
				pos += 60; // 30-byte name + 30 bytes of zeros
			}
			return names;
		}

		private byte[] ReadOneCompressedFrame() {
			var buffer = new List<byte>();
			while (true) {
				int b = this.stream.ReadByte();
				if (b < 0) {
					throw new InvalidOperationException("Connection closed after " + buffer.Count + " compressed bytes, before a full frame arrived.");
				}
				buffer.Add((byte) b);

				try {
					var (decoded, bytesConsumed) = HuffmanDecompressor.DecompressOneFrame(buffer.ToArray(), 0, buffer.Count);
					if (bytesConsumed == buffer.Count) {
						return decoded;
					}
					// Shouldn't happen here (we only ever feed it exactly what it's consumed so
					// far), but if it ever did, it would mean a second frame already started
					// arriving - not a case Phase 11's scope needs to handle.
				} catch (InvalidOperationException) {
					// Not a complete frame yet - read another byte and try again.
				}
			}
		}

		private static void EncodeAsciiFixed(string text, byte[] buffer, int offset, int fieldLength) {
			byte[] ascii = Encoding.ASCII.GetBytes(text ?? "");
			int copyLen = Math.Min(ascii.Length, fieldLength - 1);
			Array.Copy(ascii, 0, buffer, offset, copyLen);
		}

		/// <summary>
		/// Sends CreateCharacterInPacket (0x00, 104 bytes incl. id - see
		/// GameCore/Networking/IncomingPackets.cs's CreateCharacterInPacket.Read for the exact
		/// field layout this mirrors). Field values are the minimum that pass
		/// CreateCharacterInPacket.Handle's validation (name is letters-only; skin/hair/shirt/
		/// pants colors are each the lowest value in their accepted range; hairStyle/facialHair
		/// are 0, which skips their range checks entirely) - visual appearance doesn't matter for
		/// what this test client is for. The server synchronously logs the new character in as
		/// part of handling this packet (CreateCharacterInPacket.Handle calls
		/// state.LoginCharacter at the end), so no separate "select character" (0x5D) packet is
		/// needed.
		/// </summary>
		public void SendCreateCharacter(string charName) {
			byte[] packet = new byte[104];
			packet[0] = 0x00;
			// bytes 1-9: unknown/account block, left zero.
			EncodeAsciiFixed(charName, packet, 10, 30);
			// bytes 40-69: password, left zero (ignored by Handle).
			packet[70] = 0; // gender: male
			packet[71] = 10; // startStr
			packet[72] = 10; // startDex
			packet[73] = 10; // startInt
			// bytes 74-79: three (skillId, skillValue) pairs, left zero.
			EncodeUShortBigEndian(0x3ea, packet, 80); // skinColor - lowest accepted value
			EncodeUShortBigEndian(0, packet, 82); // hairStyle - 0 skips the range check
			EncodeUShortBigEndian(0x44e, packet, 84); // hairColor - still range-checked even with no hair
			EncodeUShortBigEndian(0, packet, 86); // facialHair - 0 skips the range check
			EncodeUShortBigEndian(0, packet, 88); // facialHairColor - 0 skips the range check
			EncodeUShortBigEndian(0, packet, 90); // locationNum - ignored by this server's f_createPlayerCharacter
			// bytes 92-99: unknown block, left zero.
			EncodeUShortBigEndian(0x02, packet, 100); // shirtColor - lowest accepted value
			EncodeUShortBigEndian(0x02, packet, 102); // pantsColor - lowest accepted value
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Sends TalkRequestInPacket (0x03) - plain ASCII speech, interpreted server-side as a
		/// "." command if it starts with Globals.CommandPrefix (the default), same as any
		/// player typing in their chat bar. Requires a character to already be selected/created
		/// (state.CharacterNotNull), i.e. call after SendCreateCharacter.
		/// </summary>
		public void SendSpeech(string text) {
			byte[] textBytes = Encoding.ASCII.GetBytes(text);
			byte[] packet = new byte[8 + textBytes.Length];
			packet[0] = 0x03;
			EncodeUShortBigEndian((ushort) packet.Length, packet, 1); // blockSize includes the id byte
			packet[3] = 0; // speech type (SpeechType.Normal)
			EncodeUShortBigEndian(0, packet, 4); // color
			EncodeUShortBigEndian(3, packet, 6); // font
			Array.Copy(textBytes, 0, packet, 8, textBytes.Length);
			this.stream.Write(packet, 0, packet.Length);
		}

		private static void EncodeUShortBigEndian(int value, byte[] buffer, int offset) {
			buffer[offset] = (byte) ((value >> 8) & 0xFF);
			buffer[offset + 1] = (byte) (value & 0xFF);
		}

		private static void EncodeIntBigEndian(int value, byte[] buffer, int offset) {
			buffer[offset] = (byte) ((value >> 24) & 0xFF);
			buffer[offset + 1] = (byte) ((value >> 16) & 0xFF);
			buffer[offset + 2] = (byte) ((value >> 8) & 0xFF);
			buffer[offset + 3] = (byte) (value & 0xFF);
		}

		/// <summary>
		/// Sends TargetCursorCommandsInPacket (0x6c) resolving the cursor to a ground point -
		/// the response to whatever server-side Target() call opened the cursor (pet "go"/"kill"/
		/// "transfer", taming/mining/lumberjacking skill use, house-deed placement, vendor
		/// "sell"). Mirrors GameCore/Networking/IncomingPackets.cs's
		/// TargetCursorCommandsInPacket.Read exactly: targGround(1) + skip(5) + uid(4) + x(2) +
		/// y(2) + skip(1) + z(1) + model(2).
		/// </summary>
		public void SendTargetGround(int x, int y, int z) {
			byte[] packet = new byte[19];
			packet[0] = 0x6c;
			packet[1] = 1; // targGround = true
			// bytes 2-6: skip, zero.
			// bytes 7-10: uid - ignored by the server when targGround is true.
			EncodeUShortBigEndian(x, packet, 11);
			EncodeUShortBigEndian(y, packet, 13);
			// byte 15: skip (high byte of z in the real client's encoding, ignored server-side).
			packet[16] = (byte) z;
			// bytes 17-18: model - informational only, left zero.
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Same packet as <see cref="SendTargetGround"/>, but resolving to a specific
		/// thing (a creature to tame/kill/sell-to, an item, etc.) by uid instead of a ground point.</summary>
		public void SendTargetUid(uint uid) {
			byte[] packet = new byte[19];
			packet[0] = 0x6c;
			packet[1] = 0; // targGround = false
			EncodeIntBigEndian((int) uid, packet, 7);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Sends DoubleClickInPacket (0x06) - id(1) + uid(4). Used for e.g. opening a
		/// bankbox or a deed, the same packet a real client sends on a double-click.</summary>
		public void SendDoubleClick(uint uid) {
			byte[] packet = new byte[5];
			packet[0] = 0x06;
			EncodeIntBigEndian((int) uid, packet, 1);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Sends PickUpItemInPacket (0x07) - id(1) + uid(4) + amount(2). Mirrors
		/// GameCore/Networking/IncomingPackets.cs's PickUpItemInPacket.Read exactly.</summary>
		public void SendPickUpItem(uint itemUid, ushort amount) {
			byte[] packet = new byte[7];
			packet[0] = 0x07;
			EncodeIntBigEndian((int) itemUid, packet, 1);
			EncodeUShortBigEndian(amount, packet, 5);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Sends DropItemInPacket (0x08) - id(1) + itemUid(4) + x(2) + y(2) + z(1) + contUid(4).
		/// contUid of -1 means "drop on the ground at x/y/z"; any other uid can be a container
		/// (puts the item inside, x/y are the slot position) or a character (not the dropper -
		/// the classic UO gesture for starting/adding to a secure trade, see SecureTrade.cs).
		/// Mirrors DropItemInPacket.Read exactly. Requires a prior SendPickUpItem for the same
		/// itemUid - the server only honors a drop for an item it has already recorded this
		/// connection as "holding" (AbstractCharacter.HasPickedUp).
		/// </summary>
		public void SendDropItem(uint itemUid, ushort x, ushort y, sbyte z, int contUid) {
			byte[] packet = new byte[14];
			packet[0] = 0x08;
			EncodeIntBigEndian((int) itemUid, packet, 1);
			EncodeUShortBigEndian(x, packet, 5);
			EncodeUShortBigEndian(y, packet, 7);
			packet[9] = (byte) z;
			EncodeIntBigEndian(contUid, packet, 10);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>Sends SecureTradeInPacket (0x6f) type 2 - toggle this side's accepted
		/// checkbox. Mirrors SecureTradeInPacket.Read exactly (type(1) + tradeBoxUid(4) +
		/// state(4)); the server only ever expects type 1 (cancel) or 2 (accept toggle) from a
		/// real client, per that class's own doc comment.</summary>
		public void SendSecureTradeAccept(uint tradeBoxUid, bool accepted) {
			byte[] packet = new byte[9];
			packet[0] = 0x6f;
			packet[1] = 2;
			EncodeIntBigEndian((int) tradeBoxUid, packet, 2);
			EncodeIntBigEndian(accepted ? 1 : 0, packet, 6);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Reads SecureTradeOutPacket (0x6f) type 0 - the "trade window opened" message every
		/// participant gets exactly once, immediately and synchronously when a trade starts (see
		/// SecureTrade's constructor), before any accepted-state/gold update. Returns this
		/// client's own offered-items box uid (needed to send SendSecureTradeAccept later) and
		/// the other side's box uid/name. Mirrors SecureTradeOutPacket.WriteDynamicPart's type-0
		/// branch: type(1) + myBoxUid(4) + myBoxUid-again(4) + otherBoxUid(4) + hasName(1) +
		/// [name, ASCII null-terminated].
		/// </summary>
		public (uint MyBoxUid, uint OtherBoxUid, string OtherName) ReadSecureTradeOpen(TimeSpan timeout) {
			byte[] payload = this.ReadPacketPayload(0x6f, timeout);
			byte type = payload[2];
			if (type != 0) {
				throw new InvalidOperationException("Expected SecureTradeOutPacket type 0 (open), got type " + type);
			}
			uint myBoxUid = (uint) ((payload[3] << 24) | (payload[4] << 16) | (payload[5] << 8) | payload[6]);
			uint otherBoxUid = (uint) ((payload[11] << 24) | (payload[12] << 16) | (payload[13] << 8) | payload[14]);
			bool hasName = payload[15] != 0;
			string otherName = "";
			if (hasName) {
				string rest = Encoding.ASCII.GetString(payload, 16, payload.Length - 16);
				int nul = rest.IndexOf('\0');
				otherName = nul >= 0 ? rest.Substring(0, nul) : rest;
			}
			return (myBoxUid, otherBoxUid, otherName);
		}

		/// <summary>
		/// Sends RequestSkillEtcUseInPacket (0x12), type 0x24 ("use skill by id") - the same
		/// packet a real client's skill-list double-click or macro sends. Needed for taming,
		/// mining, and lumberjacking, all of which SelectSkill() + immediately open a target
		/// cursor server-side (see TargetCursorCommandsOutPacket/SendTargetUid/SendTargetGround).
		/// </summary>
		public void SendSkillUse(int skillId) {
			byte[] idBytes = Encoding.ASCII.GetBytes(skillId.ToString(System.Globalization.CultureInfo.InvariantCulture));
			byte[] packet = new byte[4 + idBytes.Length];
			packet[0] = 0x12;
			EncodeUShortBigEndian(packet.Length, packet, 1);
			packet[3] = 0x24;
			Array.Copy(idBytes, 0, packet, 4, idBytes.Length);
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Sends GumpMenuSelectionInPacket (0xB1) with no checkboxes/switches and no text entry
		/// responses - just "I clicked button N on gump G", which is all every vendor-buy/simple
		/// confirmation dialog in this codebase actually needs from a response.
		/// </summary>
		public void SendGumpResponse(int gumpUid, int buttonId) {
			byte[] packet = new byte[23];
			packet[0] = 0xB1;
			EncodeUShortBigEndian(23, packet, 1);
			// bytes 3-6: focus uid, skipped by the server, left zero.
			EncodeIntBigEndian(gumpUid, packet, 7);
			EncodeIntBigEndian(buttonId, packet, 11);
			EncodeIntBigEndian(0, packet, 15); // switchesCount
			EncodeIntBigEndian(0, packet, 19); // entriesCount
			this.stream.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Reads SendGumpMenuDialogPacket (0xB0) - the server opening a gump - and decodes just
		/// the gump uid and its ASCII layout string (the strings list, used for free-text entry
		/// gumps, is skipped; none of this test harness's scenarios need it). The layout string
		/// is the same grammar a real client parses to build the dialog, including each
		/// "button"/"checkbox" element's numeric id - callers scan it themselves for whatever
		/// element id they need (kept generic here rather than baking in one dialog's grammar).
		/// </summary>
		public (int GumpUid, string LayoutText) ReadGumpDialog(TimeSpan timeout) {
			byte[] payload = this.ReadPacketPayload(0xb0, timeout);
			// payload[0..1] = this packet's own length field (part of the stored payload by this
			// harness's convention - see ReadPacketPayload's doc comment); real content starts
			// at payload[2].
			int gumpUid = (payload[6] << 24) | (payload[7] << 16) | (payload[8] << 8) | payload[9];
			int layoutByteLen = (payload[18] << 8) | payload[19];
			string layoutText = Encoding.ASCII.GetString(payload, 20, layoutByteLen);
			int nul = layoutText.IndexOf('\0');
			if (nul >= 0) {
				layoutText = layoutText.Substring(0, nul);
			}
			return (gumpUid, layoutText);
		}

		/// <summary>
		/// Reads either DrawObjectOutPacket (0x78, dynamic) or UpdatePlayerPacket (0x77, fixed) -
		/// whichever arrives first - for the given character uid, and returns its HighlightColor
		/// byte (0=innocent, criminal/murderer/enemy/etc. per the real client's own hue-mapping -
		/// see GameCore's HighlightColor enum). Both encode the same field order
		/// (uid/model/x/y/z/dir/color/flags/highlight), but 0x78's stored payload has a 2-byte
		/// length prefix ahead of that (per this harness's "payload includes the dynamic length
		/// field" convention - see ReadPacketPayload) that 0x77's fixed payload doesn't, so the
		/// two offsets differ by exactly that prefix.
		/// </summary>
		public byte ReadHighlightColor(uint forUid, TimeSpan timeout) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (true) {
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					throw new TimeoutException("Did not see a highlight-color update for uid 0x" + forUid.ToString("x"));
				}
				byte[] payload = this.TryReadEither(0x78, 0x77, remaining, out byte whichId);
				int uidOffset = (whichId == 0x78) ? 2 : 0;
				uint seenUid = (uint) ((payload[uidOffset] << 24) | (payload[uidOffset + 1] << 16) | (payload[uidOffset + 2] << 8) | payload[uidOffset + 3]);
				if (seenUid != forUid) {
					continue;
				}
				int highlightOffset = uidOffset + 15; // uid(4)+model(2)+x(2)+y(2)+z(1)+dir(1)+color(2)+flags(1) = 15 bytes after uid starts
				return payload[highlightOffset];
			}
		}

		private byte[] TryReadEither(byte idA, byte idB, TimeSpan timeout, out byte whichId) {
			DateTime deadline = DateTime.UtcNow + timeout;
			while (true) {
				if (this.packetQueue.Count > 0) {
					var (id, payload) = this.packetQueue.Dequeue();
					if (id == idA || id == idB) {
						whichId = id;
						return payload;
					}
					continue;
				}
				TimeSpan remaining = deadline - DateTime.UtcNow;
				if (remaining <= TimeSpan.Zero) {
					throw new TimeoutException("Did not see packet 0x" + idA.ToString("x2") + " or 0x" + idB.ToString("x2") +
						" within " + timeout + ". Ids actually seen: " +
						string.Join(",", this.seenIds.ConvertAll(b => "0x" + b.ToString("x2"))));
				}
				this.stream.ReadTimeout = Math.Max(1, (int) remaining.TotalMilliseconds);
				this.FillQueueFromOneFrame();
			}
		}

		/// <summary>Reads SeasonalInformationOutPacket (0xBC) and returns its season byte.</summary>
		public byte ReadSeason(TimeSpan timeout) {
			return this.ReadPacketPayload(0xBC, timeout)[0];
		}

		/// <summary>Reads OverallLightLevelOutPacket (0x4f) and returns the light level byte
		/// (0 = full daylight, higher = darker).</summary>
		public byte ReadOverallLightLevel(TimeSpan timeout) {
			return this.ReadPacketPayload(0x4f, timeout)[0];
		}

		public void Dispose() {
			this.stream.Dispose();
			this.tcp.Dispose();
		}
	}
}
