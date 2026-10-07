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
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	[SaveableClass]
	[ViewableClass]
	public class ScriptedAccount : AbstractAccount {
		/// <summary>GM written notes for this account</summary>
		[NoShow]
		List<AccountNote> accNotes = new List<AccountNote>();

		/// <summary>Crimes commited by this account (AFK, bugs, roughing etc.)</summary>
		[NoShow]
		List<AccountCrime> accCrimes = new List<AccountCrime>();


		string email;

		[LoadSection]
		public ScriptedAccount(PropsSection input)
			: base(input) {

		}

		public ScriptedAccount(string name)
			: base(name) {

		}

		/// <summary>Returns a copy of the AccNotes Dictionary (usable for sorting etc.)</summary>
		[NoShow]
		public List<AccountNote> AccNotes {
			get {
				return new List<AccountNote>(this.accNotes);
			}
		}

		/// <summary>Returns a copy of the AccCrimes Dictionary (usable for sorting etc.)</summary>
		[NoShow]
		public List<AccountCrime> AccCrimes {
			get {
				return new List<AccountCrime>(this.accCrimes);
			}
		}

		/// <summary>Add a new note</summary>
		public void AddNote(AccountNote note) {
			this.accNotes.Add(note);
		}
		/// <summary>Remove one selected note (cannot be removed from AccNotes property</summary>
		public void RemoveNote(AccountNote note) {
			this.accNotes.Remove(note);
		}
		/// <summary>Add a new crime</summary>
		public void AddCrime(AccountCrime crime) {
			this.accCrimes.Add(crime);
		}
		/// <summary>Remove one selected crime (cannot be removed from AccCrimes property</summary>
		public void RemoveCrime(AccountCrime crime) {
			this.accCrimes.Remove(crime);
		}

		public override void LoadLine(string filename, int line, string valueName, string valueString) {
			switch (valueName) {
				//added loading of reg. mail
				case "email":
					this.email = ConvertTools.LoadSimpleQuotedString(valueString);
					break;
				default:
					base.LoadLine(filename, line, valueName, valueString);
					break;
			}
		}

		public override void Save(SaveStream output) {
			if (this.email != null) {
				output.WriteValue("email", this.email);
			}
			base.Save(output);
		}

		/// <summary>
		/// Method for retreiving a sublist of GameAccounts which names contain 
		/// a specified string
		/// </summary>
		public static List<ScriptedAccount> RetreiveByStr(string searched) {
			List<ScriptedAccount> retList = new List<ScriptedAccount>();
			if (searched == null) searched = ""; //can be null
			searched = searched.ToUpper();

			foreach (ScriptedAccount acc in AllAccounts) {
				if (acc.Name.ToUpper().Contains(searched)) {
					//accounts containing the searched text
					retList.Add(acc);
				}
			}
			return retList;
		}

		[SteamFunction]
		public static ScriptedAccount CreateGameAccount(object ignored, ScriptArgs sa) {
			string name = string.Concat(sa.Argv[0]);
			ScriptedAccount acc = new ScriptedAccount(name);

			if (sa.Argv.Length > 1) {
				acc.Password(string.Concat(sa.Argv[1]));
				if (sa.Argv.Length > 2) {
					acc.email = string.Concat(sa.Argv[2]);
				}
			}
			return acc;
		}

		public static ScriptedAccount CreateGameAccount(string name, string password, string email) {
			ScriptedAccount acc = new ScriptedAccount(name);
			acc.Password(password);
			acc.email = email;

			return acc;
		}
	}
}