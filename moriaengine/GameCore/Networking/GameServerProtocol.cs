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

using System.Diagnostics.CodeAnalysis;
using System.Net;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;

namespace SteamEngine.Networking {
	public class GameServerProtocol : IProtocol<TcpConnection<GameState>, GameState, IPEndPoint> {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly GameServerProtocol instance = new GameServerProtocol();

		[SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures"), SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		public IncomingPacket<TcpConnection<GameState>, GameState, IPEndPoint> GetPacketImplementation(byte id, TcpConnection<GameState> conn, GameState state, out bool discardAfterReading) {
			bool isLoggedIn = state.IsLoggedIn;
			bool hasChar = state.Character != null;

			discardAfterReading = !isLoggedIn || !hasChar;
			//general rule for most packets: if not logged in completely (i.e. both account and char), discard.
			//exceptions are obviously the login packets and some "innocent" info packets

			switch (id) {
				case 0x00:
					discardAfterReading = !isLoggedIn; //discard if not yet logged into account
					return Pool<CreateCharacterInPacket>.Acquire();
				case 0x01:
					discardAfterReading = false;
					return Pool<DisconnectNotificationInPacket>.Acquire();
				case 0x02:
					return Pool<MoveRequestInPacket>.Acquire();
				case 0x03:
					return Pool<TalkRequestInPacket>.Acquire();
				case 0x05:
					return Pool<RequestAttackInPacket>.Acquire();
				case 0x06:
					return Pool<DoubleClickInPacket>.Acquire();
				case 0x07:
					return Pool<PickUpItemInPacket>.Acquire();
				case 0x08:
					return Pool<DropItemInPacket>.Acquire();
				case 0x09:
					return Pool<SingleClickInPacket>.Acquire();
				case 0x12:
					return Pool<RequestSkillEtcUseInPacket>.Acquire();
				case 0x1a:
					return Pool<StatLockChangeInPacket>.Acquire();
				case 0x0b:
					return Pool<SetLanguageInPacket>.Acquire();
				case 0x13:
					return Pool<DropToWearItemInPacket>.Acquire();
				case 0x22:
					return Pool<ResyncRequestInPacket>.Acquire();
				case 0x3a:
					return Pool<SetSkillLockStateInPacket>.Acquire();
				case 0x34:
					return Pool<GetPlayerStatusInPacket>.Acquire();
				case 0x5d:
					discardAfterReading = !isLoggedIn; //discard if not yet logged into account
					return Pool<LoginCharacterInPacket>.Acquire();
				case 0x6c:
					return Pool<TargetCursorCommandsInPacket>.Acquire();
				case 0x72:
					return Pool<RequestWarModeInPacket>.Acquire();
				case 0x73:
					return Pool<PingPongInPacket>.Acquire();
				case 0x7D:
					return Pool<ResponseToDialogBox>.Acquire();
				case 0x83:
					discardAfterReading = !isLoggedIn || hasChar; //discard if not yet logged into account, or if already logged in char
					return Pool<DeleteCharacterInPacket>.Acquire();
				case 0x91:
					discardAfterReading = isLoggedIn; //discard if already logged in
					return Pool<GameServerLoginInPacket>.Acquire();
				case 0x98:
					return Pool<AllNamesInPacket>.Acquire();
				case 0x9b:
					return Pool<RequestHelpInPacket>.Acquire();
				case 0xAD:
					return Pool<UnicodeOrAsciiSpeechRequestInPacket>.Acquire();
				case 0xb1:
					return Pool<GumpMenuSelectionInPacket>.Acquire();
				case 0xbd:
					discardAfterReading = false; //never discard
					return Pool<ClientVersionInPacket>.Acquire();
				case 0xbf:
					return Pool<GeneralInformationInPacket>.Acquire();
				case 0xc8:
					return Pool<ClientViewRangeInPacket>.Acquire();
				case 0xd6:
					return Pool<RequestMultiplePropertiesInPacket>.Acquire();
			}

			return null;
		}
	}
}