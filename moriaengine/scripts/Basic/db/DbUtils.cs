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

using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	public static class DbUtils {
		public static void CreateAllTables() {
			CreateLoginsTable();
		}

		public static void CreateLoginsTable() {
			string sql = @"CREATE TABLE log_logins (
				id INT AUTO_INCREMENT NOT NULL,
				account TINYTEXT ASCII NOT NULL,
				ip TINYTEXT ASCII NOT NULL,
				charname TINYTEXT ASCII NULL,
				charuid INT NULL,
				inorout TINYINT NOT NULL,
				time DATETIME NOT NULL,
				servertime BIGINT NOT NULL,
				gameorconsole TINYINT NOT NULL,
				clientuid INT NOT NULL,
				PRIMARY KEY (id)
				);";
			ExecuteNonQuery(sql);
		}

		public static void ExecuteNonQuery(string sql) {
			using (MySqlCommand command = new MySqlCommand(sql, DbManager.Connection)) {
				command.ExecuteNonQuery();
			}
		}
	}

	public abstract class MultiInsertContainer {
		private List<string[]> lines = new List<string[]>();

		public abstract string TableName { get; }
		public abstract string[] ColumnNames { get; }

		public void AddLine(params string[] values) {
			this.lines.Add(values);
		}

		public void SendAll() {
			if (this.lines.Count > 0) {
				StringBuilder sql = new StringBuilder("INSERT INTO ");
				sql.Append(this.TableName);

				sql.Append(" ( ");
				sql.Append(string.Join(" , ", this.ColumnNames));
				sql.Append(" ) VALUES ");

				foreach (string[] line in this.lines) {
					sql.Append(" ( '");
					sql.Append(string.Join("' , '", line));
					sql.Append("' ) , ");
				}
				sql.Length -= 2;

				DbUtils.ExecuteNonQuery(sql.ToString());

				Logger.WriteDebug("Sent " + this.lines.Count + " rows into database table '" + this.TableName + "'");

				this.lines.Clear();
			}
		}
	}
}

//INSERT IGNORE

//insert into table (atr1,atr2) values (1,2),(1,3),(1,4),(2,8)
