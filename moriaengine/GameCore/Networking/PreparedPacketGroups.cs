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
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Regions;

namespace SteamEngine.Networking {

	public static class PreparedPacketGroups {

		#region LoginDenied
		private static PacketGroup[] loginDeniedPGs = InitLoginDeniedPGs();

		private static PacketGroup[] InitLoginDeniedPGs() {
			PacketGroup[] retVal = new PacketGroup[Tools.GetEnumLength<LoginDeniedReason>()];
			for (int i = 0, n = retVal.Length; i < n; i++) {
				retVal[i] = PacketGroup.CreateFreePG();
				retVal[i].AcquirePacket<LoginDeniedOutPacket>().Prepare((LoginDeniedReason) i);
			}
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendLoginDenied(TcpConnection<GameState> conn, LoginDeniedReason why) {
			PacketGroup pg = loginDeniedPGs[(int) why];
			conn.SendPacketGroup(pg);
		}
		#endregion LoginDenied

		#region TargetCursorCommands
		private static PacketGroup targetGround = InitTargetGround();

		private static PacketGroup InitTargetGround() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<TargetCursorCommandsOutPacket>().Prepare(true);
			return retVal;
		}

		private static PacketGroup targetXYZ = InitTargetXYZ();

		private static PacketGroup InitTargetXYZ() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<TargetCursorCommandsOutPacket>().Prepare(false);
			return retVal;
		}
		private static PacketGroup targetCancelled = InitTargetCancelled();

		private static PacketGroup InitTargetCancelled() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<TargetCursorCommandsOutPacket>().PrepareAsCancel();
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendTargettingCursor(TcpConnection<GameState> conn, bool ground) {
			if (ground) {
				conn.SendPacketGroup(targetGround);
			} else {
				conn.SendPacketGroup(targetXYZ);
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendCancelTargettingCursor(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(targetCancelled);
		}
		#endregion TargetCursorCommands

		#region SetFacet
		private static PacketGroup[] facetChange = InitFacetChange();

		private static PacketGroup[] InitFacetChange() {
			PacketGroup[] retVal = new PacketGroup[Map.FacetCount];
			for (int i = 0, n = retVal.Length; i < n; i++) {
				retVal[i] = PacketGroup.CreateFreePG();
				retVal[i].AcquirePacket<SetFacetOutPacket>().Prepare(i);
			}
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendFacetChange(TcpConnection<GameState> conn, int facet) {
			conn.SendPacketGroup(facetChange[facet]);
		}
		#endregion SetFacet

		#region SetWarMode
		private static PacketGroup[] warMode = InitWarMode();

		private static PacketGroup[] InitWarMode() {
			PacketGroup[] retVal = new PacketGroup[2];
			retVal[0] = PacketGroup.CreateFreePG();
			retVal[0].AcquirePacket<SetWarModeOutPacket>().Prepare(false);
			retVal[1] = PacketGroup.CreateFreePG();
			retVal[1].AcquirePacket<SetWarModeOutPacket>().Prepare(true);
			return retVal;
		}

		public static void SendWarMode(TcpConnection<GameState> conn, bool enabled) {
			conn.SendPacketGroup(warMode[enabled ? 1 : 0]);
		}
		#endregion SetWarMode

		#region RejectMoveItemRequest
		private static PacketGroup[] pickUpFailed = InitPickupFailed();

		private static PacketGroup[] InitPickupFailed() {
			int n = Tools.GetEnumLength<PickupItemResult>();
			PacketGroup[] retVal = new PacketGroup[n];
			for (int i = 0; i < n; i++) {
				retVal[i] = PacketGroup.CreateFreePG();
				retVal[i].AcquirePacket<RejectMoveItemRequestOutPacket>().Prepare((PickupItemResult) i);
			}
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendRejectMoveItemRequest(TcpConnection<GameState> conn, PickupItemResult msg) {
			conn.SendPacketGroup(pickUpFailed[(int) msg]);
		}
		#endregion RejectMoveItemRequest

		#region RejectDeleteCharacter
		private static PacketGroup[] rejectDeleteCharacter = InitRejectDeleteCharacter();

		private static PacketGroup[] InitRejectDeleteCharacter() {
			PacketGroup[] retVal = new PacketGroup[Tools.GetEnumLength<DeleteCharacterResult>()];
			for (int i = 0, n = retVal.Length; i < n; i++) {
				retVal[i] = PacketGroup.CreateFreePG();
				retVal[i].AcquirePacket<RejectDeleteCharacterOutPacket>().Prepare((DeleteCharacterResult) i);
			}
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendRejectDeleteCharacter(TcpConnection<GameState> conn, DeleteCharacterResult msg) {
			conn.SendPacketGroup(rejectDeleteCharacter[(int) msg]);
		}
		#endregion RejectDeleteCharacter

		#region ResurrectionMenu
		private static PacketGroup deathMessage = InitDeathMessages();
		private static PacketGroup resurrectMessage = InitResurrectMessages();

		private static PacketGroup InitDeathMessages() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<ResurrectionMenuOutPacket>().Prepare(1);
			return retVal;
		}

		private static PacketGroup InitResurrectMessages() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<ResurrectionMenuOutPacket>().Prepare(2);
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendResurrectMessage(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(deathMessage);
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendYouAreDeathMessage(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(resurrectMessage);
		}
		#endregion ResurrectionMenu

		#region EnableMapDiffFiles
		private static PacketGroup enableMapDiffFiles = InitMapDiff();

		private static PacketGroup InitMapDiff() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<EnableMapDiffFilesOutPacket>().Prepare();
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendEnableMapDiffFiles(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(enableMapDiffFiles);
		}
		#endregion EnableMapDiffFiles

		#region ClientVersion
		private static PacketGroup clientVersion = InitClientVersion();

		private static PacketGroup InitClientVersion() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<ClientVersionOutPacket>();
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendClientVersionQuery(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(clientVersion);
		}
		#endregion ClientVersion

		#region EnableLockedClientFeatures
		private static PacketGroup clientFeatures = InitClientFeatures();

		private static PacketGroup InitClientFeatures() {
			PacketGroup retVal = PacketGroup.CreateFreePG();
			retVal.AcquirePacket<EnableLockedClientFeaturesOutPacket>().Prepare(Globals.FeaturesFlags);
			return retVal;
		}

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendClientFeatures(TcpConnection<GameState> conn) {
			conn.SendPacketGroup(clientFeatures);
		}
		#endregion EnableLockedClientFeatures

		#region OverallLightLevel
		private static PacketGroup[] overallLightLevel = new PacketGroup[0x100];

		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
		public static void SendOverallLightLevel(TcpConnection<GameState> conn, int lightLevel) {
			PacketGroup pg = overallLightLevel[lightLevel];
			if (pg == null) {
				pg = PacketGroup.CreateFreePG();
				pg.AcquirePacket<OverallLightLevelOutPacket>().Prepare(lightLevel);
				overallLightLevel[lightLevel] = pg;
			}	
			
			conn.SendPacketGroup(pg);
		}
		#endregion OverallLightLevel
	}
}