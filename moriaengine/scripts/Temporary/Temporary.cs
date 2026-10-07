using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	public static class SomeScript {

		[SteamFunction]
		public static void S0x1B() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);
			Map map = ch.GetMap();

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<LoginConfirmationOutPacket>().Prepare(ch, map.SizeX, map.SizeY); //0x1B
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0xBF0x08() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);
			Map map = ch.GetMap();

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<SetFacetOutPacket>().Prepare(map.Facet);//0xBF 0x08
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0xBF0x18() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			//PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			PreparedPacketGroups.SendEnableMapDiffFiles(conn); //0xBF 0x18
			//conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0xBC() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(ch.Season, ch.Cursor); //0xBC
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0xB9() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PreparedPacketGroups.SendClientFeatures(conn);
		}

		[SteamFunction]
		public static void S0x20() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0x78() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<DrawObjectOutPacket>().Prepare(ch, ch.GetHighlightColorFor(ch)); //0x78
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0x11() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<StatusBarInfoOutPacket>().Prepare(ch, StatusBarType.Me); //0x11
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0x55() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<LoginCompleteOutPacket>(); //0x55
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void S0x72() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PreparedPacketGroups.SendWarMode(conn, ch.Flag_WarMode); //0x72
		}

		[SteamFunction]
		public static void SR() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);
			//Regions.Map map = ch.GetMap();

			ch.Resync();
		}

		[SteamFunction]
		public static void S0xC8() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);
			//Regions.Map map = ch.GetMap();

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();
			pg.AcquirePacket<ClientViewRangeOutPacket>().Prepare(state.UpdateRange); //0x55
			conn.SendPacketGroup(pg);
		}

		[SteamFunction]
		public static void SCHI() {
			GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
			GetClient(out state, out conn, out ch);

			PacketSequences.SendCharInfoWithPropertiesTo(ch, state, conn, ch);
		}

		//[SteamFunction]
		//public static void PLight(object ignoredSelf, int lightLevel) {
		//    GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
		//    GetClient(out state, out conn, out ch);
		//    //Regions.Map map = ch.GetMap();

		//    PacketGroup pg = PacketGroup.AcquireSingleUsePG();
		//    pg.AcquirePacket<PersonalLightLevelOutPacket>().Prepare(ch.FlaggedUid, lightLevel); //0x4e
		//    conn.SendPacketGroup(pg);
		//}

		//[SteamFunction]
		//public static void GLight(object ignoredSelf, int lightLevel) {
		//    GameState state; TcpConnection<GameState> conn; AbstractCharacter ch;
		//    GetClient(out state, out conn, out ch);
		//    //Regions.Map map = ch.GetMap();

		//    PacketGroup pg = PacketGroup.AcquireSingleUsePG();
		//    pg.AcquirePacket<OverallLightLevelOutPacket>().Prepare(lightLevel); //0x4f
		//    conn.SendPacketGroup(pg);
		//}

		private static void GetClient(out GameState state, out TcpConnection<GameState> conn, out AbstractCharacter ch) {
			if (GameServer.AllClients.Count > 0) {
				state = null;
				foreach (GameState gs in GameServer.AllClients) {
					state = gs;
					break;
				}
				conn = state.Conn;
				ch = state.Character;
			} else {
				throw new SEException("No client");
			}
		}
	}
}