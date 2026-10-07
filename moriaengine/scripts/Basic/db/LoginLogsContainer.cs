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
using SteamEngine.Common;
using SteamEngine.Networking;

//using MySql.Data.MySqlClient;

namespace SteamEngine.CompiledScripts {
	public static class DbMethods {
		public static LoginLogsContainer loginLogs = new LoginLogsContainer();

		public class LoginLogsContainer : MultiInsertContainer {
			public override string TableName {
				get {
					return "log_logins";
				}
			}


			private static string[] colNames = {
				"account", "ip", "charname", "charuid", "inorout", "time", "servertime", "gameorconsole", "clientuid" };

			public override string[] ColumnNames {
				get {
					return colNames;
				}
			}

			public void GameLogin(GameState state) {
				AbstractCharacter ch = state.Character;
				AbstractAccount acc = state.Account;
				Sanity.IfTrueThrow(ch == null, "CurCharacter can't be null in LoginLogsContainer.GameLogin");
				Sanity.IfTrueThrow(acc == null, "Account can't be null in LoginLogsContainer.GameLogin");
				this.AddLine(acc.Name, state.Conn.EndPoint.ToString(), ch.Name, ch.Uid.ToString(), "1",
					DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Globals.TimeInTicks.ToString(),
					"0", state.Uid.ToString());
			}

			public void GameLogout(GameState state) {
				AbstractCharacter ch = state.Character;
				AbstractAccount acc = state.Account;
				Sanity.IfTrueThrow(ch == null, "CurCharacter can't be null in LoginLogsContainer.GameLogout");
				Sanity.IfTrueThrow(acc == null, "Account can't be null in LoginLogsContainer.GameLogin");
				this.AddLine(acc.Name, state.Conn.EndPoint.ToString(), ch.Name, ch.Uid.ToString(), "0",
					DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Globals.TimeInTicks.ToString(),
					"0", state.Uid.ToString());
			}

			public void ItemMove(GameState state) {
				AbstractCharacter ch = state.Character;
				AbstractAccount acc = state.Account;
				Sanity.IfTrueThrow(ch == null, "CurCharacter can't be null in LoginLogsContainer.GameLogin");
				Sanity.IfTrueThrow(acc == null, "Account can't be null in LoginLogsContainer.GameLogin");
				this.AddLine(acc.Name, state.Conn.EndPoint.ToString(), ch.Name, ch.Uid.ToString(), "1",
					DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Globals.TimeInTicks.ToString(),
					"0", state.Uid.ToString());
			}

			//needs implementing of the calling side, too lazy for that now

			//public void ConsoleLogin(ConsConn conn) {
			//    GameAccount acc = conn.Account;
			//    Sanity.IfTrueThrow(acc == null, "Account can't be null in LoginLogsContainer.ConsoleLogin");
			//    this.AddLine(acc.Name, conn.IP.ToString(), null, null, "1",
			//        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Globals.TimeInTicks.ToString(), 
			//        "1", conn.uid.ToString());
			//}

			//public void ConsoleLogout(ConsConn conn) {
			//    GameAccount acc = conn.Account;
			//    Sanity.IfTrueThrow(acc == null, "Account can't be null in LoginLogsContainer.ConsoleLogout");
			//    this.AddLine(acc.Name, conn.IP.ToString(), null, null, "0",
			//        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Globals.TimeInTicks.ToString(), 
			//        "1", conn.uid.ToString());
			//}
		}

		private static void SendPending() {
			loginLogs.SendAll();
		}

		public class E_DbMethods_Global : CompiledTriggerGroup {
			public void On_AfterSave(Globals ignored1, ScriptArgs sa) {
				bool success = Convert.ToBoolean(sa.Argv[1]);
				if (success) {
					SendPending();
				}
			}
		}
	}
}