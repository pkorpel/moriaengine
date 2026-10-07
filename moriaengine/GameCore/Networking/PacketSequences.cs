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
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.Communication;
using SteamEngine.Communication.TCP;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.Networking {
	public static class PacketSequences {

		internal class DelayedLoginTimer : Timer {
			AbstractCharacter ch;

			internal DelayedLoginTimer(AbstractCharacter ch) {
				this.ch = ch;
			}

			protected sealed override void OnTimeout() {
				GameState state = this.ch.GameState;
				if (state == null) {
					this.Delete();
				}
				if (state.Version != ClientVersion.nullValue) {
					SendStartGameSequence(this.ch);
					this.Delete();
				}
			}
		}

		internal static void SendStartGameSequence(AbstractCharacter ch) {
			GameState state = ch.GameState;
			if (state == null) {
				return;
			}
			TcpConnection<GameState> conn = state.Conn;

			Logger.WriteDebug("Starting game for character " + ch);

			PacketGroup pg = PacketGroup.AcquireSingleUsePG();

			Map map = ch.GetMap();
			pg.AcquirePacket<LoginConfirmationOutPacket>().Prepare(ch, map.SizeX, map.SizeY); //0x1B

			//pg.AcquirePacket<SetFacetOutPacket>().Prepare(map.Facet);//0xBF 0x08
			//conn.SendPacketGroup(pg);

			//PreparedPacketGroups.SendEnableMapDiffFiles(conn); //0xBF 0x18

			//pg = PacketGroup.AcquireSingleUsePG();
			//pg.AcquirePacket<SeasonalInformationOutPacket>().Prepare(ch.Season, ch.Cursor); //0xBC

			//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20
			//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20
			//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20
			//pg.AcquirePacket<DrawObjectOutPacket>().Prepare(ch, ch.GetHighlightColorFor(ch));
			//pg.AcquirePacket<StatusBarInfoOutPacket>().Prepare(ch, StatusBarType.Me); //0x11

			//pg.AcquirePacket<LoginCompleteOutPacket>(); //0x55
			//pg.AcquirePacket<LoginCompleteOutPacket>(); //0x55
			//TODO? 0x5B - current time

			//pg.AcquirePacket<DrawGamePlayerOutPacket>().Prepare(state, ch); //0x20

			conn.SendPacketGroup(pg);

			//PreparedPacketGroups.SendWarMode(conn, ch.Flag_WarMode);


			new DelayedResyncTimer(ch).DueInSeconds = 1;
		}

		internal class DelayedResyncTimer : Timer {
			AbstractCharacter ch;

			internal DelayedResyncTimer(AbstractCharacter ch) {
				this.ch = ch;
			}

			protected sealed override void OnTimeout() {
				GameState state = this.ch.GameState;
				if (state != null) {
					PacketGroup pg = PacketGroup.AcquireSingleUsePG();
					pg.AcquirePacket<LoginCompleteOutPacket>(); //0x55 //jak se ukazalo tohle musi bejt az po ty pauze, i kdyz tezko rict proc. Vyssi klienti (402+) to tezko snasely v jedny davce s 0x1B
					state.Conn.SendPacketGroup(pg);

					this.ch.Resync();


					state.WriteLine(string.Format(CultureInfo.InvariantCulture,
						Loc<PacketSequencesLoc>.Get(state.Language).WelcomeToShard,
						Globals.ServerName));
				}
			}
		}

		public static void SendCharInfoWithPropertiesTo(AbstractCharacter viewer, GameState viewerState,
			TcpConnection<GameState> viewerConn, AbstractCharacter target) {

			DrawObjectOutPacket packet = Pool<DrawObjectOutPacket>.Acquire();
			packet.Prepare(target, target.GetHighlightColorFor(viewer));
			viewerConn.SendSinglePacket(packet);

			TrySendPropertiesTo(viewerState, viewerConn, target);
		}

		public static void SendContainerContentsWithPropertiesTo(AbstractCharacter viewer, GameState viewerState,
			TcpConnection<GameState> viewerConn, AbstractItem container) {

			if (container.Count > 0) {
				ItemsInContainerOutPacket iicp = Pool<ItemsInContainerOutPacket>.Acquire();

				using (ListBuffer<AbstractItem> listBuffer = Pool<ListBuffer<AbstractItem>>.Acquire()) {
					if (iicp.PrepareContainer(container, viewer, listBuffer.list)) {
						viewerConn.SendSinglePacket(iicp);
						if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
							foreach (AbstractItem contained in listBuffer.list) {
								AosToolTips toolTips = contained.GetAosToolTips(viewer.Language);
								if (toolTips != null) {
									toolTips.SendIdPacket(viewerState, viewerConn);
								}
							}
						}
					} else {
						iicp.Dispose();
					}
				}
			}
		}

		public static void TrySendPropertiesTo(GameState viewerState, TcpConnection<GameState> viewerConn, Thing target) {
			if (Globals.UseAosToolTips && viewerState.Version.AosToolTips) {
				AosToolTips toolTips = target.GetAosToolTips(viewerState.Language);
				if (toolTips != null) {
					toolTips.SendIdPacket(viewerState, viewerConn);
				}
			}
		}

		[CLSCompliant(false)]
		public static void SendRemoveFromView(TcpConnection<GameState> conn, uint flaggedUid) {
			DeleteObjectOutPacket packet = Pool<DeleteObjectOutPacket>.Acquire();
			packet.Prepare(flaggedUid);
			conn.SendSinglePacket(packet);
		}

		public static void SendRemoveFromView(TcpConnection<GameState> conn, int flaggedUid) {
			DeleteObjectOutPacket packet = Pool<DeleteObjectOutPacket>.Acquire();
			packet.Prepare(flaggedUid);
			conn.SendSinglePacket(packet);
		}

		#region messages
		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendSystemMessage(TcpConnection<GameState> c, string msg, int color) {
			if (c != null) {
				InternalSendMessage(c, null, msg, "System", SpeechType.Speech, ClientFont.Unified, color);
			}
		}

		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(TcpConnection<GameState> c, int msg, int color, string args) {
			if (c != null) {
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(null, msg, "System", SpeechType.Speech, ClientFont.Unified, color, args);
				c.SendSinglePacket(packet);
			}
		}

		/* 
			Method: SendSystemMessage
				Sends a system message to a particular game-connection.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - The cliloc # to send.
				color - The color of the message.
				args - Additional arguments needed for the cliloc message, if any.
		*/
		public static void SendClilocSysMessage(TcpConnection<GameState> c, int msg, int color, params string[] args) {
			if (c != null) {
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(null, msg, "System", SpeechType.Speech, ClientFont.Unified, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		/* 
			Method: SendOverheadMessage
				Sends an overhead message to a particular game-connection. The message
				will appear above the head of that connection's currently logged in character.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessage(TcpConnection<GameState> c, string msg, int color) {
			if (c != null) {
				AbstractCharacter cre = c.State.CharacterNotNull;
				InternalSendMessage(c, cre, msg, "System", SpeechType.Speech, ClientFont.Unified, color);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send.
				color - The color of the message.
		*/

		public static void SendNameFrom(TcpConnection<GameState> c, Thing from, string msg, int color) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				InternalSendMessage(c, from, msg, "", SpeechType.Name, ClientFont.Unified, color);
			}
		}


		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/
		public static void SendClilocNameFrom(TcpConnection<GameState> c, Thing from, int msg, int color, string args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, ClientFont.Unified, color, args);
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/
		public static void SendClilocNameFrom(TcpConnection<GameState> c, Thing from, int msg, int color, string arg1, string arg2) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, ClientFont.Unified, color, string.Concat(arg1, "\t", arg2));
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendNameFrom
				Sends the message to a particular game-connection, but sends it as a name.
				It'll still appear over that thing's head, but in the journal it won't say "The Name: The Name"
				- It'll say "You see: The Name" instead.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing whose name is being sent.
				msg - What to send (A cliloc #).
				color - The color of the message.
		*/
		public static void SendClilocNameFrom(TcpConnection<GameState> c, Thing from, int msg, int color, params string[] args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "", SpeechType.Name, ClientFont.Unified, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		public static void SendClilocMessageFrom(TcpConnection<GameState> c, Thing from, int msg, int color, string args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, ClientFont.Unified, color, args);
				c.SendSinglePacket(packet);
			}
		}

		public static void SendClilocMessageFrom(TcpConnection<GameState> c, Thing from, int msg, int color, string arg1, string arg2) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, ClientFont.Unified, color, string.Concat(arg1, "\t", arg2));
				c.SendSinglePacket(packet);
			}
		}

		public static void SendClilocMessageFrom(TcpConnection<GameState> c, Thing from, int msg, int color, params string[] args) {
			if (c != null) {
				Sanity.IfTrueThrow(from == null, "from == null");
				ClilocMessageOutPacket packet = Pool<ClilocMessageOutPacket>.Acquire();
				packet.Prepare(from, msg, "System", SpeechType.Speech, ClientFont.Unified, color, string.Join("\t", args));
				c.SendSinglePacket(packet);
			}
		}

		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular thing, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The thing the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(TcpConnection<GameState> c, Thing from, string msg, int color) {
			if (c != null) {
				//if (from == null) 
				//if (from is AbstractCharacter) {
				//    SendMessage(c, from, from.Model, from.Name, msg, SpeechType.Speech, 3, color);
				//} else if (from is AbstractItem) {
				//    SendMessage(c, from, 0, from.Name, msg, SpeechType.Speech, 3, color);
				//}

				InternalSendMessage(c, from, msg, from.Name, SpeechType.Speech, ClientFont.Unified, color);
			}
		}
		/*
			Method: SendOverheadMessageFrom
				Displays a message above a particular static, but only to one game-connection. This works
				like speech, but is only sent to the chosen connection, so only that player will see it.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				from - The static the message will appear above.
				msg - What to send.
				color - The color of the message.
		*/
		public static void SendOverheadMessageFrom(TcpConnection<GameState> c, AbstractInternalItem from, string msg, int color) {
			if (c != null) {
				//if (from == null) throw new ArgumentNullException("from cannot be null in SendOverheadMessageFrom.");
				InternalSendMessage(c, null, msg, from.Name, SpeechType.Speech, ClientFont.Unified, color);
			}
		}

		/*
			Method: SendServerMessage
				Displays a server message, but only to that client.
			Parameters:
				c - The Connection to send to. (Get from character.conn, if it's not null)
				msg - What to send.
				color - The color of the message.
		*/
		public static GameOutgoingPacket PrepareServerMessagePacket(string msg) {
			return PrepareMessagePacket(null, msg, "System", SpeechType.Server, ClientFont.Server, -1, null);
		}

		/*
			Method: BroadCast
				Broadcast a message to all clients (as a server message).
			Parameters:
				msg - What to send.
		*/
		internal static void BroadCast(string msg) {
			Console.WriteLine("Broadcasting: " + msg);
			using (PacketGroup pg = PacketGroup.AcquireMultiUsePG()) {
				pg.AddPacket(PrepareServerMessagePacket(msg));

				foreach (GameState state in GameServer.AllClients) {
					state.Conn.SendPacketGroup(pg);
				}
			}
		}

		internal static void InternalSendMessage(TcpConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ClientFont font, int color) {
			InternalSendMessage(c, from, msg, sourceName, type, font, color, c.State.ClientLanguage);
		}

		//For use by Server's various message sending methods (Which send to one client).
		internal static void InternalSendMessage(TcpConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ClientFont font, int color, string lang) {
			c.SendSinglePacket(PrepareMessagePacket(from, msg, sourceName, type, font, color, lang));
		}

		internal static GameOutgoingPacket PrepareMessagePacket(Thing from, string msg, string sourceName, SpeechType type, ClientFont font, int color, string lang) {
			if ((type != SpeechType.Spell) && (font == ClientFont.Unified)) {	//if it's another font than 3, send it as ASCII
				UnicodeSpeechOutPacket packet = Pool<UnicodeSpeechOutPacket>.Acquire();
				packet.Prepare(from, msg, sourceName, type, font, color, lang);
				return packet;
			} else {
				SendSpeechOutPacket packet = Pool<SendSpeechOutPacket>.Acquire();
				packet.Prepare(from, msg, sourceName, type, font, color);
				return packet;
			}
		}

		//public static void SendAsciiMessage(TCPConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ClientFont font, int color, string lang) {
		//    SendSpeechOutPacket packet = Pool<SendSpeechOutPacket>.Acquire();
		//    packet.Prepare(from, msg, sourceName, type, font, color);
		//    c.SendSinglePacket(packet);
		//}

		//public static void SendUnicodeMessage(TCPConnection<GameState> c, Thing from, string msg, string sourceName, SpeechType type, ClientFont font, int color, string lang) {
		//    UnicodeSpeechOutPacket packet = Pool<UnicodeSpeechOutPacket>.Acquire();
		//    packet.Prepare(from, msg, sourceName, type, font, color, lang);
		//    c.SendSinglePacket(packet);
		//}

		#endregion messages

		public static void SendStatLocks(AbstractCharacter ch) {
			GameState state = ch.GameState;
			if (state != null) {
				ExtendedStatsOutPacket p = Pool<ExtendedStatsOutPacket>.Acquire();
				p.Prepare(ch.FlaggedUid, ch.StatLockByte);
				state.Conn.SendSinglePacket(p);
			}
		}

		public static void SendSound(IPoint4D top, int soundId, int range) {
			PlaySoundEffectOutPacket p = Pool<PlaySoundEffectOutPacket>.Acquire();
			p.Prepare(top, soundId);
			GameServer.SendToClientsInRange(top, range, p);
		}
	}

	internal class PacketSequencesLoc : CompiledLocStringCollection<PacketSequencesLoc> {
		public string WelcomeToShard = "Welcome to {0}.";
	}
}