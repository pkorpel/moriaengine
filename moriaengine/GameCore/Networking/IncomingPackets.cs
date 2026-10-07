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
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Networking {
	public abstract class GameIncomingPacket : IncomingPacket<TcpConnection<GameState>, GameState, IPEndPoint> {

	}

	public abstract class DynamicLenInPacket : GameIncomingPacket {
		protected sealed override ReadPacketResult Read() {
			int blockSize = this.DecodeShort();
			if ((blockSize - 1) > this.LengthIn) {//-1 because "start" is after the first byte
				return ReadPacketResult.NeedMoreData;
			}
			ReadPacketResult retVal = this.ReadDynamicPart(blockSize);
			this.SeekFromStart(blockSize - 1); //-1 because "start" is after the first byte
			return retVal;
		}

		protected abstract ReadPacketResult ReadDynamicPart(int blockSize);
	}

	public sealed class GeneralInformationInPacket : DynamicLenInPacket {
		SubPacket subPacket;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			int subCmd = this.DecodeShort();

			switch (subCmd) {
				case 0x05:
					this.subPacket = Pool<ScreenSizeSubPacket>.Acquire();
					break;
				case 0x0b:
					this.subPacket = Pool<SetLanguageSubPacket>.Acquire();
					break;
				case 0x06:
					this.subPacket = Pool<PartySubPacket>.Acquire();
					break;
				case 0x1c:
					this.subPacket = Pool<SpellSelectedSubPacket>.Acquire();
					break;
				case 0x24:
					this.subPacket = Pool<UnknownSubPacket>.Acquire();
					break;
				default:
					Logger.WriteDebug("Unknown packet 0xbf - subpacket 0x" + subCmd.ToString("x", CultureInfo.InvariantCulture) + " (len " + blockSize + ")");
					this.OutputPacketLog();
					return ReadPacketResult.DiscardSingle;
			}

			if (subCmd != 0x24) { //0x24 is boringly frequent, albeit ignored
				Logger.WriteDebug("Handling packet 0xbf - subpacket 0x" + subCmd.ToString("x", CultureInfo.InvariantCulture) + " (len " + blockSize + ")");
			}
			return this.subPacket.ReadSubPacket(this, blockSize);
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			this.subPacket.Handle(this, conn, state);
		}

		protected override void On_DisposeManagedResources() {
			try {
				if (this.subPacket != null) {
					this.subPacket.Dispose();
					this.subPacket = null;
				}
			} finally {
				base.On_DisposeManagedResources();
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public abstract class SubPacket : Poolable {
			protected internal abstract ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize);
			protected internal abstract void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state);
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class ScreenSizeSubPacket : SubPacket {
			short x, y;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				packet.SeekFromCurrent(2);
				this.x = packet.DecodeShort();
				this.y = packet.DecodeShort();
				packet.SeekFromCurrent(2);
#if DEBUG
				return ReadPacketResult.Success;
#else
				return ReadPacketResult.DiscardSingle; //why should we care about screen resolution, right?
#endif
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
				Logger.WriteDebug(state + " reports screen resolution " + this.x + "x" + this.y);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class UnknownSubPacket : SubPacket {
			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				return ReadPacketResult.DiscardSingle;
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
				throw new SEException("The method or operation is not implemented.");
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class SetLanguageSubPacket : SubPacket {
			string language;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				this.language = packet.DecodeAsciiString(4);
				return ReadPacketResult.Success;
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
				state.ClientLanguage = this.language;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class SpellSelectedSubPacket : SubPacket {
			short spellId;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				packet.SeekFromCurrent(2);
				this.spellId = packet.DecodeShort();
				return ReadPacketResult.Success;
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
				state.CharacterNotNull.TryCastSpellFromBook(this.spellId);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public sealed class PartySubPacket : SubPacket {
			SubPacket subSubPacket;

			protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
				byte subSubId = packet.DecodeByte();
				switch (subSubId) {
					case 0x01:
						this.subSubPacket = Pool<AddAPartyMemberSubSubPacket>.Acquire();
						break;
					case 0x02:
						this.subSubPacket = Pool<RemoveAPartyMemberSubSubPacket>.Acquire();
						break;
					case 0x03:
						this.subSubPacket = Pool<TellPartyMemberAMessageSubSubPacket>.Acquire();
						break;
					case 0x04:
						this.subSubPacket = Pool<TellFullPartyAMessageSubSubPacket>.Acquire();
						break;
					case 0x06:
						this.subSubPacket = Pool<PartyCanLootMeSubSubPacket>.Acquire();
						break;
					case 0x08:
						this.subSubPacket = Pool<AcceptJoinPartyInvitationSubSubPacket>.Acquire();
						break;
					case 0x09:
						this.subSubPacket = Pool<DeclineJoinPartyInvitationSubSubPacket>.Acquire();
						break;
					default:
						Logger.WriteDebug("Unknown packet 0xbf - subpacket 0x6 (Party) - subsubpacket " + subSubId.ToString("x", CultureInfo.InvariantCulture) + " (len " + blockSize + ")");
						packet.OutputPacketLog();
						return ReadPacketResult.DiscardSingle;
				}

				Logger.WriteDebug("Handling packet 0xbf - subpacket 0x6 (Party) - subsubpacket " + subSubId.ToString("x", CultureInfo.InvariantCulture) + " (len " + blockSize + ")");
				return this.subSubPacket.ReadSubPacket(packet, blockSize);
			}

			protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
				this.subSubPacket.Handle(packet, conn, state);
			}

			protected override void On_DisposeManagedResources() {
				try {
					if (this.subSubPacket != null) {
						this.subSubPacket.Dispose();
						this.subSubPacket = null;
					}
				} finally {
					base.On_DisposeManagedResources();
				}
			}			

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class AddAPartyMemberSubSubPacket : SubPacket {
				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.RequestAddMember(state.CharacterNotNull);
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class RemoveAPartyMemberSubSubPacket : SubPacket {
				int uid;

				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.uid = packet.DecodeInt();
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.RequestRemoveMember(state.CharacterNotNull, Thing.UidGetCharacter(this.uid));
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class TellPartyMemberAMessageSubSubPacket : SubPacket {
				int uid;
				string message;

				[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "blockSize-10")]
				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.uid = packet.DecodeInt();
					this.message = packet.DecodeBigEndianUnicodeString(blockSize - 10);
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.RequestPrivateMessage(state.CharacterNotNull, Thing.UidGetCharacter(this.uid), this.message);
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class TellFullPartyAMessageSubSubPacket : SubPacket {
				string message;
				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.message = packet.DecodeBigEndianUnicodeString(checked(blockSize - 6));
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.RequestPublicMessage(state.CharacterNotNull, this.message);
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class PartyCanLootMeSubSubPacket : SubPacket {
				bool canLoot;

				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.canLoot = packet.DecodeBool();
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.SetCanLoot(state.CharacterNotNull, this.canLoot);
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class AcceptJoinPartyInvitationSubSubPacket : SubPacket {
				int uid;

				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.uid = packet.DecodeInt();
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.AcceptJoinRequest(state.CharacterNotNull, Thing.UidGetCharacter(this.uid));
					}
				}
			}

			[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
			public sealed class DeclineJoinPartyInvitationSubSubPacket : SubPacket {
				int uid;

				protected internal override ReadPacketResult ReadSubPacket(GeneralInformationInPacket packet, int blockSize) {
					this.uid = packet.DecodeInt();
					return ReadPacketResult.Success;
				}

				protected internal override void Handle(GeneralInformationInPacket packet, TcpConnection<GameState> conn, GameState state) {
					PartyCommands instance = PartyCommands.Instance;
					if (instance != null) {
						instance.DeclineJoinRequest(state.CharacterNotNull, Thing.UidGetCharacter(this.uid));
					}
				}
			}
		}
	}

	//yes, this is a dirty copy from RunUO :)
	public abstract class PartyCommands {
		private static PartyCommands instance;

		public static PartyCommands Instance {
			get { return instance; }
		}

		protected PartyCommands() {
			instance = this;
		}

		public abstract void RequestAddMember(AbstractCharacter self);
		public abstract void RequestRemoveMember(AbstractCharacter self, AbstractCharacter target);
		public abstract void RequestPrivateMessage(AbstractCharacter self, AbstractCharacter target, string text);
		public abstract void RequestPublicMessage(AbstractCharacter self, string text);
		public abstract void SetCanLoot(AbstractCharacter self, bool canLoot);
		public abstract void AcceptJoinRequest(AbstractCharacter self, AbstractCharacter leader);
		public abstract void DeclineJoinRequest(AbstractCharacter self, AbstractCharacter leader);
	}

	public sealed class GameServerLoginInPacket : GameIncomingPacket {
		string accName, pass;

		protected override ReadPacketResult Read() {
			this.SeekFromStart(4);
			this.accName = this.DecodeAsciiString(30);
			this.pass = this.DecodeAsciiString(30);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractAccount acc;
			LoginAttemptResult result = AbstractAccount.HandleLoginAttempt(this.accName, this.pass, state, out acc);

			switch (result) {
				case LoginAttemptResult.Success:
					Console.WriteLine(LogStr.Ident(state) + " logged in.");

					PacketGroup pg = PacketGroup.AcquireSingleUsePG();
					//if (Globals.aos) {
					//	pg.AcquirePacket<EnableLockedClientFeaturesOutPacket>().Prepare(Globals.featuresFlags);
					//}
					pg.AcquirePacket<CharactersListOutPacket>().Prepare(acc, Globals.LoginFlags);
					conn.SendPacketGroup(pg);

					PreparedPacketGroups.SendClientVersionQuery(conn);

					break;
				case LoginAttemptResult.Failed_AlreadyOnline:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.SomeoneIsAlreadyUsingThisAccount);
					conn.Close("Account '" + acc + "' already online.");
					break;
				case LoginAttemptResult.Failed_BadPassword:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.InvalidAccountCredentials);
					conn.Close("Bad password for account '" + acc + "'");
					break;
				case LoginAttemptResult.Failed_Blocked:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.Blocked);
					conn.Close("Account '" + acc + "' blocked.");
					break;
				case LoginAttemptResult.Failed_NoSuchAccount:
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.NoAccount);
					conn.Close("No account '" + this.accName + "'");
					break;
			}
		}
	}

	public sealed class LoginCharacterInPacket : GameIncomingPacket {
		int charIndex;

		protected override ReadPacketResult Read() {
			this.SeekFromStart(67);
			this.charIndex = this.DecodeByte();
			this.SeekFromCurrent(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.LoginCharacter(this.charIndex);
		}
	}


	public sealed class ClientVersionInPacket : GameIncomingPacket {
		string ver;

		protected override ReadPacketResult Read() {
			ushort size = this.DecodeUShort();
			this.ver = this.DecodeAsciiString(size - 3);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			ClientVersion cv = ClientVersion.Acquire(this.ver);
			if (cv != state.Version) {
				Console.WriteLine(LogStr.Ident(state.ToString()) + (" claims to be: " + cv));
				state.InternalSetClientVersion(cv);
			}
		}
	}

	public sealed class GetPlayerStatusInPacket : GameIncomingPacket {
		byte type;
		int uid;

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(4); //BYTE[4] pattern (0xedededed) 

			this.type = this.DecodeByte();
			this.uid = this.DecodeInt();

			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractCharacter viewer = state.CharacterNotNull;

			AbstractCharacter target = Thing.UidGetCharacter(this.uid);
			if (target != null) {
				switch (this.type) {
					case 0x04:
						target.ShowStatusBarTo(viewer, conn);
						return;
					case 0x05:
						if ((viewer == target) || viewer.IsPlevelAtLeast(Globals.PlevelOfGM)) {
							target.ShowSkillsTo(conn, state);
							//} else {
							//    Server.SendSystemMessage(c, "Illegal skills request.", 0);
						} else {
							Logger.WriteDebug(state + ": Illegal skills request.");
						}
						return;

					default:
						//Server.SendSystemMessage(c, "Unknown type=" + getType.ToString("x") + " in GetPlayerStatusPacket.", 0);
						Logger.WriteDebug(state + ": Unknown status/skills request type=" + this.type.ToString("x", CultureInfo.InvariantCulture) + " in GetPlayerStatusPacket.");
						return;
				}
			}
			PacketSequences.SendRemoveFromView(conn, this.uid);
		}
	}

	public sealed class RequestMultiplePropertiesInPacket : DynamicLenInPacket {
		List<int> uids = new List<int>();

		[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "blockSize-3")]
		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.uids.Clear();
			if (Globals.UseAosToolTips) {
				int dataBlockSize = blockSize - 3;
				if ((dataBlockSize >= 0) && ((dataBlockSize % 4) == 0)) {
					int count = dataBlockSize / 4;
					for (int i = 0; i < count; i++) {
						this.uids.Add(this.DecodeInt());
					}
				}
				return ReadPacketResult.Success;
			}
			return ReadPacketResult.DiscardSingle;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if (Globals.UseAosToolTips) {
				AbstractCharacter curChar = state.CharacterNotNull;
				foreach (int uid in this.uids) {
					Thing t = Thing.UidGetThing(uid);
					if ((t != null) && (!t.IsDeleted)) {
						AosToolTips toolTips = t.GetAosToolTips(state.Language);
						if (toolTips != null) {
							if (curChar.CanSeeForUpdate(t).Allow) {
								toolTips.SendDataPacket(conn);
							}
						}
					}
				}
			}
		}
	}

	public sealed class TalkRequestInPacket : DynamicLenInPacket {
		byte type;
		ushort color, font;
		string speech;

		[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "blockSize-8")]
		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.color = this.DecodeUShort();
			this.font = this.DecodeUShort();
			this.speech = this.DecodeAsciiString(blockSize - 8);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if (this.speech.IndexOf(Globals.CommandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.CommandPrefix.Length));
			} else if (this.speech.IndexOf(Globals.AlternateCommandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.AlternateCommandPrefix.Length));
			} else {
				//this.font = 3;	//We don't want them speaking in runic or anything. -SL
				//we don't? not sure here, we'll see -tar

				//actually speak it
				state.CharacterNotNull.Speech(this.speech, 0, (SpeechType) this.type, this.color, (ClientFont) this.font, null, null);
			}
		}
	}

	public sealed class UnicodeOrAsciiSpeechRequestInPacket : DynamicLenInPacket {
		byte type;
		ushort color, font;
		string speech, language;
		int[] keywords;

		private HashSet<int> keywordsSet = new HashSet<int>();

		private static int[] emptyInts = new int[0];

		[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "blockSize-12")]
		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.color = this.DecodeUShort();
			this.font = this.DecodeUShort();
			this.language = this.DecodeAsciiString(4);

			this.keywords = emptyInts;

			if ((this.type & 0xc0) == 0xc0) {
				int value = this.DecodeShort();
				int numKeywords = (value & 0xFFF0) >> 4;
				int hold = value & 0xF;

				for (int i = 0; i < numKeywords; ++i) {
					int speechID;
					if ((i & 1) == 0) {
						hold <<= 8;
						hold |= this.DecodeByte();
						speechID = hold;
						hold = 0;
					} else {
						value = this.DecodeShort();
						speechID = (value & 0xFFF0) >> 4;
						hold = value & 0xF;
					}

					this.keywordsSet.Add(speechID);
				}

				int n = this.keywordsSet.Count;
				if (n > 0) {
					this.keywords = new int[n];
					this.keywordsSet.CopyTo(this.keywords, 0);
					this.keywordsSet.Clear();
				}
				this.type = (byte) (this.type & ~0xc0);

				int speechlen = blockSize - this.Position;

				this.speech = this.DecodeAsciiString(speechlen);
			} else {
				this.speech = this.DecodeBigEndianUnicodeString(blockSize - 12);
			}

			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if (this.speech.IndexOf(Globals.CommandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.CommandPrefix.Length));
			} else if (this.speech.IndexOf(Globals.AlternateCommandPrefix) == 0) {
				Commands.PlayerCommand(state, this.speech.Substring(Globals.AlternateCommandPrefix.Length));
			} else {
				//actually speak it
				state.CharacterNotNull.Speech(this.speech, 0, (SpeechType) this.type, this.color, (ClientFont) this.font, this.language, this.keywords, null);
			}
		}
	}

	public sealed class SetLanguageInPacket : GameIncomingPacket {
		string language;

		protected override ReadPacketResult Read() {
			this.language = this.DecodeAsciiString(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.ClientLanguage = this.language;
		}
	}


	public sealed class TargetCursorCommandsInPacket : GameIncomingPacket {
		bool targGround;
		uint uid;
		ushort x, y;
		sbyte z;
		ushort model;

		protected override ReadPacketResult Read() {
			this.targGround = this.DecodeBool();
			this.SeekFromCurrent(5);
			this.uid = this.DecodeUInt();
			this.x = this.DecodeUShort();
			this.y = this.DecodeUShort();
			this.SeekFromCurrent(1);//skip the first byte of Z
			this.z = this.DecodeSByte();
			this.model = this.DecodeUShort();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.HandleTarget(this.targGround, this.uid, this.x, this.y, this.z, this.model);
		}
	}

	public sealed class PingPongInPacket : GameIncomingPacket {

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(1);
			return ReadPacketResult.DiscardSingle;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			throw new SEException("The method or operation is not implemented.");
		}
	}

	public sealed class ClientViewRangeInPacket : GameIncomingPacket {
		byte range;

		protected override ReadPacketResult Read() {
			this.range = this.DecodeByte();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.RequestedUpdateRange = this.range;
		}
	}

	public sealed class SingleClickInPacket : GameIncomingPacket {
		int uid;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			Thing thing = Thing.UidGetThing(this.uid);
			if (thing != null) {
				if (Globals.UseAosToolTips && state.Version.AosToolTips) {
					thing.Trigger_AosClick(state.CharacterNotNull);
				} else {
					thing.Trigger_Click(state.CharacterNotNull);
				}
			}
		}
	}

	public sealed class DoubleClickInPacket : GameIncomingPacket {
		uint uid;
		bool paperdollFlag;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeUInt();
			this.paperdollFlag = ((this.uid & 0x80000000) == 0x80000000);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractCharacter ch = state.CharacterNotNull;
			Thing thing;
			if (this.uid == 0x3cfff) {
				thing = ch;
			} else {
				thing = Thing.UidGetThing(this.uid);
				if ((thing == null) || (!ch.CanSeeForUpdate(thing).Allow)) {
					PacketSequences.SendRemoveFromView(conn, this.uid);
					return;
				}
			}

			if (thing.IsItem) {
				thing.Trigger_DClick(ch);
			} else {
				if (this.paperdollFlag) {
					((AbstractCharacter) thing).ShowPaperdollTo(ch, state, conn);
				} else {
					thing.Trigger_DClick(ch);
				}
			}
		}
	}

	public sealed class CreateCharacterInPacket : GameIncomingPacket {
		CreateCharArguments args = new CreateCharArguments();

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(9);
			this.args.charname = this.DecodeAsciiString(30, true);
			this.SeekFromCurrent(30);//password
			this.args.gender = this.DecodeByte();
			this.args.startStr = this.DecodeByte();
			this.args.startDex = this.DecodeByte();
			this.args.startInt = this.DecodeByte();
			this.args.skillId1 = this.DecodeByte();
			this.args.skillValue1 = this.DecodeByte();
			this.args.skillId2 = this.DecodeByte();
			this.args.skillValue2 = this.DecodeByte();
			this.args.skillId3 = this.DecodeByte();
			this.args.skillValue3 = this.DecodeByte();
			this.args.skinColor = (ushort) (this.DecodeUShort() & ~0x8000);
			this.args.hairStyle = this.DecodeUShort();
			this.args.hairColor = this.DecodeUShort();
			this.args.facialHair = this.DecodeUShort();
			this.args.facialHairColor = this.DecodeUShort();
			this.args.locationNum = this.DecodeUShort();

			this.SeekFromCurrent(8);//some versions may not be sending this part... we'll see how it turns out
			//2 unknown, 2 slot, 4 ip

			this.args.shirtColor = this.DecodeUShort();
			this.args.pantsColor = this.DecodeUShort();

			return ReadPacketResult.Success;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractAccount acc = state.Account;
			if (!acc.HasFreeSlot) {
				state.WriteLine(Loc<IncomingPacketsLoc>.Get(state.Language).MaxAccCharsReached);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Tried to create more chars than allowed");
				return;
			}

			string charname = this.args.Charname;
			for (int a = 0; a < charname.Length; a++) {
				if (!(charname[a] == ' ' || (charname[a] >= 'a' && charname[a] <= 'z') || (charname[a] >= 'A' && charname[a] <= 'Z'))) {
					state.WriteLine(Loc<IncomingPacketsLoc>.Get(state.Language).IllegalCharsInName);
					PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
					conn.Close("Illegal name - Contains something other than letters and spaces.");
					return;
				}
			}
			if (charname[0] == ' ' || charname[charname.Length - 1] == ' ') {
				state.WriteLine(Loc<IncomingPacketsLoc>.Get(state.Language).IllegalTrailingSpacesInName);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal name - Starts or ends with spaces.");
				return;
			}
			if (charname.IndexOf("  ") > -1) {
				state.WriteLine(Loc<IncomingPacketsLoc>.Get(state.Language).IllegalLongSpacesInName);
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal name - More than one space in a row.");
				return;
			}
			if (this.args.gender < 0 || this.args.gender > 1) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalGender,
					this.args.gender));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal gender=" + this.args.gender);
				return;
			}
			if (this.args.SkinColor < 0x3ea || this.args.SkinColor > 0x422) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalSkinColor,
					this.args.SkinColor));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal skinColor=" + this.args.SkinColor);
				return;
			}
			if (this.args.HairStyle == 0) {
			} else if (this.args.HairStyle < 0x203B || this.args.HairStyle > 0x204A ||
					   (this.args.HairStyle > 0x203D && this.args.HairStyle < 0x2044)) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalHairStyle,
					this.args.HairStyle));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal hairStyle=" + this.args.HairStyle);
				return;
			}
			if (this.args.HairColor < 0x44e || this.args.HairColor > 0x4ad) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalHairColor,
					this.args.HairColor));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal hairColor=" + this.args.HairColor);
				return;
			}
			if (this.args.FacialHair == 0) {
			} else if (this.args.FacialHair < 0x203E || this.args.FacialHair > 0x204D || (this.args.FacialHair > 0x2041 && this.args.FacialHair < 0x204B)) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalFacialhair,
					this.args.FacialHair));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal facialHair=" + this.args.FacialHair);
				return;
			}
			if (this.args.FacialHairColor == 0) {
			} else if (this.args.FacialHairColor < 0x44e || this.args.FacialHairColor > 0x4ad) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalFacialHairColor,
					this.args.FacialHairColor));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal facialHairColor=" + this.args.FacialHairColor);
				return;
			}
			if (this.args.ShirtColor < 0x02 || this.args.ShirtColor > 0x3e9) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalShirtColor,
					this.args.ShirtColor));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal shirtColor=" + this.args.ShirtColor);
				return;
			}
			if (this.args.PantsColor < 0x02 || this.args.PantsColor > 0x3e9) {
				state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
					Loc<IncomingPacketsLoc>.Get(state.Language).IllegalPantsColor,
					this.args.PantsColor));
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("Illegal pantsColor=" + this.args.PantsColor);
				return;
			}

			//TODO: check skills and stats? Can't be bothered for moria, for now... -tar

			ScriptArgs sa = new ScriptArgs(this.args);
			object o = CreatePlayerCharacterFunction.TryRun(Globals.Instance, sa);//creates the character

			AbstractCharacter cre = o as AbstractCharacter;
			if (cre == null) {
				PreparedPacketGroups.SendLoginDenied(conn, LoginDeniedReason.CommunicationsProblem);
				conn.Close("The CreatePlayerCharacter function failed to create a new character.");
				return;
			}
			int charSlot = cre.MakeBePlayer(acc);
			Globals.SetSrc(cre);

			Logger.WriteDebug("Logging in character " + cre);
			state.LoginCharacter(charSlot);
		}

		private static ScriptHolder f_createPlayerCharacter;
		public static ScriptHolder CreatePlayerCharacterFunction {
			get {
				if (f_createPlayerCharacter == null) {
					f_createPlayerCharacter = ScriptHolder.GetFunction("f_createPlayerCharacter");
					if (f_createPlayerCharacter == null) {
						throw new SEException("f_createPlayerCharacter function not declared! It needs to have 1 parameter of type CreateCharArguments, and return an AbstractCharacter instance");
					}
				}
				return f_createPlayerCharacter;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
		public class CreateCharArguments {
			internal string charname;
			public string Charname {
				get {
					return this.charname;
				}
			}

			internal byte gender;
			public bool IsFemale {
				get {
					return this.gender != 0;
				}
			}

			internal int startStr;
			public int StartStr {
				get {
					return this.startStr;
				}
			}
			internal int startDex;
			public int StartDex {
				get {
					return this.startDex;
				}
			}
			internal int startInt;
			public int StartInt {
				get {
					return this.startInt;
				}
			}

			internal int skinColor;
			public int SkinColor {
				get {
					return this.skinColor;
				}
			}
			internal int hairStyle;
			public int HairStyle {
				get {
					return this.hairStyle;
				}
			}
			internal int hairColor;
			public int HairColor {
				get {
					return this.hairColor;
				}
			}
			internal int facialHair;
			public int FacialHair {
				get {
					return this.facialHair;
				}
			}
			internal int facialHairColor;
			public int FacialHairColor {
				get {
					return this.facialHairColor;
				}
			}

			internal int shirtColor;
			public int ShirtColor {
				get {
					return this.shirtColor;
				}
			}
			internal int pantsColor;
			public int PantsColor {
				get {
					return this.pantsColor;
				}
			}

			internal int locationNum;
			public int LocationNum {
				get {
					return this.locationNum;
				}
			}

			internal int skillId1;
			public int SkillId1 {
				get {
					return this.skillId1;
				}
			}
			internal int skillValue1;
			public int SkillValue1 {
				get {
					return this.skillValue1;
				}
			}
			internal int skillId2;
			public int SkillId2 {
				get {
					return this.skillId2;
				}
			}
			internal int skillValue2;
			public int SkillValue2 {
				get {
					return this.skillValue2;
				}
			}
			internal int skillId3;
			public int SkillId3 {
				get {
					return this.skillId3;
				}
			}
			internal int skillValue3;
			public int SkillValue3 {
				get {
					return this.skillValue3;
				}
			} 
		}
	}

	public sealed class DisconnectNotificationInPacket : GameIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(4);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			conn.Close("Client logoff - returning to main menu");
		}
	}

	public sealed class MoveRequestInPacket : GameIncomingPacket {
		byte dir, sequence;

		protected override ReadPacketResult Read() {
			this.dir = (byte) (this.DecodeByte() & 0x87); //we only want 0x80 and 0..7
			this.sequence = this.DecodeByte();
			//TODO? check this.LengthIn for fastwalk presence
			this.SeekFromCurrent(4);//skip the fastwalk key, we don't use it
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			MovementState ms = state.movementState;

			bool running = ((this.dir & 0x80) == 0x80);
			Direction direction = (Direction) (this.dir & 0x07);

			ms.MovementRequest(direction, running, this.sequence);
		}
	}


	public sealed class RequestAttackInPacket : GameIncomingPacket {
		uint uid;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeUInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractCharacter cre = Thing.UidGetCharacter(this.uid);
			if ((cre == null) || (cre.IsDeleted)) {
				PacketSequences.SendRemoveFromView(conn, this.uid);
			} else {
				AbstractCharacter self = state.CharacterNotNull;
				self.Trigger_PlayerAttackRequest(cre);
			}
		}
	}

	public sealed class PickUpItemInPacket : GameIncomingPacket {
		int uid;
		ushort amount;

		protected override ReadPacketResult Read() {
			this.uid = this.DecodeInt();
			this.amount = this.DecodeUShort();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractItem item = Thing.UidGetItem(this.uid);
			if (item != null) {
				AbstractCharacter cre = state.CharacterNotNull;
				DenyResult result = cre.TryPickupItem(item, this.amount);
				if (!result.Allow) {
					PickupItemResult numeric;
					if (resultsTable.TryGetValue(result, out numeric)) {
						PreparedPacketGroups.SendRejectMoveItemRequest(conn, numeric);
					} else {
						result.SendDenyMessage(conn, state);
						PreparedPacketGroups.SendRejectMoveItemRequest(conn, PickupItemResult.Deny_NoMessage);
					}
				}
			} else {
				PreparedPacketGroups.SendRejectMoveItemRequest(conn, PickupItemResult.Deny_RemoveFromView);
				//PacketSequences.SendRemoveFromView(conn, this.uid);
			}
		}

		private static Dictionary<DenyResult, PickupItemResult> resultsTable = InitTable();
		private static Dictionary<DenyResult, PickupItemResult> InitTable() {
			Dictionary<DenyResult, PickupItemResult> retVal = new Dictionary<DenyResult, PickupItemResult>();
			retVal.Add(DenyResultMessages.Allow, PickupItemResult.Allow);
			retVal.Add(DenyResultMessages.Deny_NoMessage, PickupItemResult.Deny_NoMessage);
			retVal.Add(DenyResultMessages.Deny_ThatDoesNotBelongToYou, PickupItemResult.Deny_ThatDoesNotBelongToYou);
			retVal.Add(DenyResultMessages.Deny_ThatDoesntExist, PickupItemResult.Deny_RemoveFromView);
			retVal.Add(DenyResultMessages.Deny_ThatIsInvisible, PickupItemResult.Deny_RemoveFromView);
			retVal.Add(DenyResultMessages.Deny_ThatIsOutOfLOS, PickupItemResult.Deny_ThatIsOutOfSight);
			retVal.Add(DenyResultMessages.Deny_ThatIsTooFarAway, PickupItemResult.Deny_ThatIsTooFarAway);
			retVal.Add(DenyResultMessages.Deny_YouAreAlreadyHoldingAnItem, PickupItemResult.Deny_YouAreAlreadyHoldingAnItem);
			retVal.Add(DenyResultMessages.Deny_YouCannotPickThatUp, PickupItemResult.Deny_YouCannotPickThatUp);
			return retVal;
		}
	}

	public sealed class DropItemInPacket : GameIncomingPacket {
		int itemUid, contUid;
		ushort x, y;
		sbyte z;

		protected override ReadPacketResult Read() {
			this.itemUid = this.DecodeInt();
			this.x = this.DecodeUShort();
			this.y = this.DecodeUShort();
			this.z = this.DecodeSByte();
			this.contUid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			//always an item, may or may not have the item-flag.
			AbstractItem i = Thing.UidGetItem(this.itemUid);
			AbstractCharacter cre = state.CharacterNotNull;
			if (i != null && cre.HasPickedUp(i)) {
				DenyResult result;
				if (this.contUid == -1) {//dropping on ground
					result = cre.TryPutItemOnGround(this.x, this.y, this.z);
				} else {
					Thing co = Thing.UidGetThing(this.contUid);
					if (co != null) {
						//Console.WriteLine("HandleDropItem: x = "+x+", y = "+y+", z = "+z+", cont = "+co);

						AbstractItem coAsItem = co as AbstractItem;
						if (coAsItem != null) {
							if (coAsItem.IsContainer && (this.x != 0xFFFF) && (this.y != 0xFFFF)) {
								//client put it to some coords inside container
								result = cre.TryPutItemInItem(coAsItem, this.x, this.y, false);
							} else {
								//client put it on some other item. The client probably thinks the other item is either a container or that they can be stacked. We'll see ;)
								result = cre.TryPutItemOnItem(coAsItem);//we ignore the x y
							}
						} else {
							result = cre.TryPutItemOnChar((AbstractCharacter) co);
						}
					} else {
						result = DenyResultMessages.Deny_ThatDoesntExist;
					}
				}

				if (!result.Allow) {
					result.SendDenyMessage(cre, state, conn);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.itemUid);
			}
		}
	}

	public sealed class DropToWearItemInPacket : GameIncomingPacket {
		int itemUid, contUid;

		protected override ReadPacketResult Read() {
			this.itemUid = this.DecodeInt();
			this.SeekFromCurrent(1); //we don't care which layer the client thinks it belongs to
			this.contUid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractItem i = Thing.UidGetItem(this.itemUid);
			AbstractCharacter cre = state.CharacterNotNull;
			if (i != null && cre.HasPickedUp(i)) {
				AbstractCharacter contChar = Thing.UidGetCharacter(this.contUid);
				if (contChar == null) {
					contChar = cre;
				}
				DenyResult result = cre.TryEquipItemOnChar(contChar);

				if ((!result.Allow) && cre.HasPickedUp(i)) { //we check if the player still has it in hand after the triggers
					result.SendDenyMessage(cre, state, conn);
					cre.TryGetRidOfDraggedItem();
				}
			} else {
				PacketSequences.SendRemoveFromView(conn, this.itemUid);
			}
		}
	}

	public sealed class RequestSkillEtcUseInPacket : DynamicLenInPacket {
		byte type;
		string actionStr;
		int actionParseResult;
		bool actionParsed;

		[SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "blockSize-4")]
		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.type = this.DecodeByte();
			this.actionStr = this.DecodeAsciiString(blockSize - 4);
			string firstPart = this.actionStr.Split(Tools.whitespaceChars)[0];
			this.actionParsed = ConvertTools.TryParseInt32(firstPart, out this.actionParseResult);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			switch (this.type) {
				//case 0: //God mode teleport?
				//case 0x6b: //God mode command?

				case 0x24: //skill
					if (this.actionParsed) {
						int skillId;
						if (this.actionParseResult == 0) {//last skill
							skillId = state.LastSkillMacroId;
						} else {
							skillId = this.actionParseResult;
						}
						AbstractSkillDef skillDef = AbstractSkillDef.GetById(skillId);
						if (skillDef.StartByMacroEnabled) {
							state.LastSkillMacroId = skillId;
							state.CharacterNotNull.SelectSkill(skillDef);
							return;
						}
					}
					break;
				case 0x56: //spell
					if (this.actionParsed) {
						int spellId;
						if (this.actionParseResult == 0) {//last spell
							spellId = state.LastSpellMacroId;
						} else {
							spellId = this.actionParseResult;
							state.LastSpellMacroId = spellId;
						}
						state.CharacterNotNull.TryCastSpellFromBook(spellId);
						return;
					}
					break;
				case 0x58: //opendoor
					Temporary.OpenDoorMacroRequest(state.CharacterNotNull);
					return;
				case 0x7c: //anim
					switch (this.actionStr) {
						case "bow":
							Temporary.AnimRequest(state.CharacterNotNull, RequestableAnim.Bow);
							return;
						case "salute":
							Temporary.AnimRequest(state.CharacterNotNull, RequestableAnim.Salute);
							return;
					}
					break;
			}


			this.OutputPacketLog("Error or unexpected value in Request skill/spell/opendoor/anim packet.");
		}
	}

	public sealed class StatLockChangeInPacket : GameIncomingPacket {
		byte stat;
		StatLockType lockType;

		protected override ReadPacketResult Read() {
			this.stat = this.DecodeByte();
			byte l = this.DecodeByte();
			if (l > 2) {
				l = 0;
			}
			this.lockType = (StatLockType) l;
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			switch (this.stat) {
				case 0:
					state.CharacterNotNull.StrLock = this.lockType;
					break;
				case 1:
					state.CharacterNotNull.DexLock = this.lockType;
					break;
				case 2:
					state.CharacterNotNull.IntLock = this.lockType;
					break;
			}
		}
	}

	public sealed class SetSkillLockStateInPacket : DynamicLenInPacket {
		short skillId;
		SkillLockType lockType;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.skillId = this.DecodeShort();
			byte l = this.DecodeByte();
			if (l > 2) {
				l = 0;
			}
			this.lockType = (SkillLockType) l;
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if ((this.skillId >= 0) && (this.skillId < AbstractSkillDef.SkillsCount)) {
				state.CharacterNotNull.SetSkillLockType(this.skillId, this.lockType);
			} else {
				this.OutputPacketLog("SkillLock of skillid " + this.skillId + " requested out of range.");
			}
		}
	}

	public sealed class RequestWarModeInPacket : GameIncomingPacket {
		bool warModeEnabled;

		protected override ReadPacketResult Read() {
			this.warModeEnabled = this.DecodeBool();
			this.SeekFromCurrent(3);//unknown
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.CharacterNotNull.Flag_WarMode = this.warModeEnabled;
		}
	}

	public sealed class DeleteCharacterInPacket : GameIncomingPacket {
		int charSlot;

		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(30);//password
			this.charSlot = this.DecodeInt();
			this.SeekFromCurrent(4);//IP
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if (this.charSlot < 0 || this.charSlot >= AbstractAccount.maxCharactersPerGameAccount) {
				//Server.DisconnectAndLog(c, "Illegal char index "+charIndex);
				PreparedPacketGroups.SendRejectDeleteCharacter(conn, DeleteCharacterResult.Deny_NonexistantCharacter);
				return;
			}
			AbstractAccount acc = state.Account;
			DeleteCharacterResult result = acc.RequestDeleteCharacter(this.charSlot);
			if (result == DeleteCharacterResult.Allow) {
				ResendCharactersAfterDeleteOutPacket p = Pool<ResendCharactersAfterDeleteOutPacket>.Acquire();
				p.Prepare(acc.Characters);
				conn.SendSinglePacket(p);
			} else if (result != DeleteCharacterResult.Deny_NoMessage) {
				PreparedPacketGroups.SendRejectDeleteCharacter(conn, result);
			}
		}
	}

	public sealed class ResyncRequestInPacket : GameIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(2);
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractCharacter ch = state.CharacterNotNull;

			DrawGamePlayerOutPacket dgpot = Pool<DrawGamePlayerOutPacket>.Acquire();
			dgpot.Prepare(state, ch); //0x20
			conn.SendSinglePacket(dgpot);

			DrawObjectOutPacket doop = Pool<DrawObjectOutPacket>.Acquire();
			doop.Prepare(ch, ch.GetHighlightColorFor(ch)); //0x78							
			conn.SendSinglePacket(doop);

			//TODO? also send nearby stuff?
		}
	}

	public sealed class AllNamesInPacket : DynamicLenInPacket {
		int uid;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.uid = this.DecodeInt();
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			AbstractCharacter c = Thing.UidGetCharacter(this.uid);
			if ((c != null) && (state.CharacterNotNull.CanSeeForUpdate(c).Allow)) {
				AllNamesOutPacket p = Pool<AllNamesOutPacket>.Acquire();
				p.Prepare(this.uid, c.Name);
				conn.SendSinglePacket(p);
			}
		}
	}

	public sealed class RequestHelpInPacket : GameIncomingPacket {
		protected override ReadPacketResult Read() {
			this.SeekFromCurrent(257);//some worthless data
			return ReadPacketResult.Success;
		}

		private static ScriptHolder help;
		
		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			if (help == null) {
				help = ScriptHolder.GetFunction("help");
				if (help == null) {
					state.WriteLine("Sorry, there isn't a help menu yet.");
					return;
				}
			}
			help.TryRun(state.CharacterNotNull);
		}
	}

	public sealed class GumpMenuSelectionInPacket : DynamicLenInPacket {
		int gumpUid;
		int pressedButton;
		int[] selectedSwitches;
		ResponseText[] responseTexts;

		protected override ReadPacketResult ReadDynamicPart(int blockSize) {
			this.SeekFromCurrent(4); //skip the focus uid
			this.gumpUid = this.DecodeInt();
			this.pressedButton = this.DecodeInt();

			int switchesCount = this.DecodeInt();
			this.selectedSwitches = new int[switchesCount];
			for (int i = 0; i < switchesCount; i++) {
				this.selectedSwitches[i] = this.DecodeInt();
			}

			uint entriesCount = this.DecodeUInt();
			this.responseTexts = new ResponseText[entriesCount];
			for (int i = 0; i < entriesCount; i++) {
				ushort id = this.DecodeUShort();
				int len = this.DecodeUShort() * 2;
				string text = this.DecodeBigEndianUnicodeString(len);
				this.responseTexts[i] = new ResponseText(id, text);
			}
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			Gump gi = state.PopGump(this.gumpUid);
			if (gi != null) {
				int n = (gi.numEntryIDs != null) ? gi.numEntryIDs.Count : 0;
				ResponseNumber[] responseNumbers = new ResponseNumber[n];
				for (int i = 0; i < n; i++) {
					foreach (ResponseText rt in this.responseTexts) {
						if (gi.numEntryIDs[i] == rt.Id) {
							decimal number;
							if (ConvertTools.TryParseDecimal(rt.Text, out number)) {
								responseNumbers[i] = new ResponseNumber(rt.Id, number);
							} else {
								state.WriteLine(string.Format(CultureInfo.InvariantCulture, 
									Loc<IncomingPacketsLoc>.Get(state.Language).NotANumber,
									rt.Text));
								SendGumpBack(conn, state, gi, this.responseTexts);
								return;
							}
							break;
						}
					}
					//we could fill the possible gap here... or not. The clients should expect nulls in the array.
				}
				gi.OnResponse(this.pressedButton, this.selectedSwitches, this.responseTexts, responseNumbers);
			}

			this.selectedSwitches = null;
			this.responseTexts = null;
		}

		private static void SendGumpBack(TcpConnection<GameState> conn, GameState state, Gump gi, ResponseText[] responseTexts) {
			//first we copy the responsetext into the default texts for the textentries so that they don't change.
			foreach (ResponseText rt in responseTexts) {
				int defaultTextId;
				if (gi.entryTextIds.TryGetValue(rt.Id, out defaultTextId)) {
					if (defaultTextId < gi.textsList.Count) {//one can never be too sure
						gi.textsList[defaultTextId] = rt.Text;
					}
				}
			}
			//and then we send the gump back again
			state.SentGump(gi);
			SendGumpMenuDialogPacket p = Pool<SendGumpMenuDialogPacket>.Acquire();
			p.Prepare(gi);
			conn.SendSinglePacket(p);
		}
	}
	
	public sealed class ResponseToDialogBox : GameIncomingPacket {
		int menuUid;
		int index;

		protected override ReadPacketResult Read() {
			this.menuUid = this.DecodeInt();
			this.SeekFromCurrent(2); //second uid, we ignore it
			this.index = this.DecodeUShort(); //1-based index of choice 
			this.SeekFromCurrent(4);
			//BYTE[2] model # of choice 
			//BYTE[2] color
			return ReadPacketResult.Success;
		}

		protected override void Handle(TcpConnection<GameState> conn, GameState state) {
			state.HandleMenu(this.menuUid, this.index);
		}
	}


	internal class IncomingPacketsLoc : CompiledLocStringCollection<IncomingPacketsLoc> {
		public string NotANumber = "'{0}' is not a number!";
		public string MaxAccCharsReached = "You already have the maximum number of characters in your account.";
		public string IllegalCharsInName = "Illegal name - Contains something other than letters and spaces.";
		public string IllegalTrailingSpacesInName = "Illegal name - Starts or ends with spaces.";
		public string IllegalLongSpacesInName = "Illegal name - More than one space in a row.";
		public string IllegalGender = "Illegal gender={0}";
		public string IllegalSkinColor = "Illegal skinColor={0}";
		public string IllegalHairStyle = "Illegal hairStyle={0}";
		public string IllegalHairColor = "Illegal hairColor={0}";
		public string IllegalFacialhair = "Illegal facialHair={0}";
		public string IllegalFacialHairColor = "Illegal facialHairColor={0}";
		public string IllegalShirtColor = "Illegal shirtColor={0}";
		public string IllegalPantsColor = "Illegal pantsColor={0}";
	}
}