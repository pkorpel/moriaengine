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
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {

	/// <summary>Utility class for managing account information or crime notes</summary>
	public static class AccountRegister {
		public static readonly string ALL_CHARS = "all";

		public static List<AccountNote> GetNotes(ScriptedAccount acc, SortingCriteria sortBy) {
			List<AccountNote> notes = acc.AccNotes;
			NotesListSort(notes, sortBy); //sort, if necessary
			return notes;
		}

		public static List<AccountCrime> GetCrimes(ScriptedAccount acc, SortingCriteria sortBy) {
			List<AccountCrime> crimes = acc.AccCrimes;
			NotesListSort(crimes, sortBy); //sort, if necessary
			return crimes;
		}

		/// <summary>Sorting of the account notes/crimes list</summary>
		private static void NotesListSort<T>(List<T> list, SortingCriteria criteria) where T : AccountNote {
			switch (criteria) {
				case SortingCriteria.TimeAsc:
					list.Sort(NotesTimeComparer<T>.instance);
					break;
				case SortingCriteria.TimeDesc:
					list.Sort(NotesTimeComparer<T>.instance);
					list.Reverse();
					break;
				case SortingCriteria.RefCharAsc:
					list.Sort(NotesRefCharComparer<T>.instance);
					break;
				case SortingCriteria.RefCharDesc:
					list.Sort(NotesRefCharComparer<T>.instance);
					list.Reverse();
					break;
				case SortingCriteria.IssuerAsc:
					list.Sort(NotesIssuerComparer<T>.instance);
					break;
				case SortingCriteria.IssuerDesc:
					list.Sort(NotesIssuerComparer<T>.instance);
					list.Reverse();
					break;
				case SortingCriteria.AFKAsc:
					list.Sort(CrimesAFKComparer<T>.instance);
					break;
				case SortingCriteria.AFKDesc:
					list.Sort(CrimesAFKComparer<T>.instance);
					list.Reverse();
					break;
			}
		}
	}

	/// <summary>Comparer for sorting account notes by time</summary>
	public class NotesTimeComparer<T> : IComparer<T> where T : AccountNote {
		public static readonly NotesTimeComparer<T> instance = new NotesTimeComparer<T>();

		private NotesTimeComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(T x, T y) {
			return x.time.CompareTo(y.time);
		}
	}

	/// <summary>Comparer for sorting account notes by referered character</summary>
	public class NotesRefCharComparer<T> : IComparer<T> where T : AccountNote {
		public static readonly NotesRefCharComparer<T> instance = new NotesRefCharComparer<T>();

		private NotesRefCharComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(T x, T y) {
			//check if we have the reffered character, otherwise return "all" - the note or crime is related to the whole account
			string refXName = (x.referredChar != null ? x.referredChar.Name : "all");
			string refYName = (y.referredChar != null ? y.referredChar.Name : "all");
			return StringComparer.OrdinalIgnoreCase.Compare(refXName, refYName);
		}
	}

	/// <summary>Comparer for sorting account notes by note issuer</summary>
	public class NotesIssuerComparer<T> : IComparer<T> where T : AccountNote {
		public static readonly NotesIssuerComparer<T> instance = new NotesIssuerComparer<T>();

		private NotesIssuerComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(T x, T y) {
			return StringComparer.OrdinalIgnoreCase.Compare(x.issuer.Name, y.issuer.Name);
		}
	}

	/// <summary>Comparer for sorting account notes its AFK or nonAFK type</summary>
	public class CrimesAFKComparer<T> : IComparer<T> where T : AccountNote {
		public static readonly CrimesAFKComparer<T> instance = new CrimesAFKComparer<T>();

		private CrimesAFKComparer() {
			//soukromy konstruktor, pristupovat budeme pres instanci
		}

		public int Compare(T x, T y) {
			AccountCrime a = (AccountCrime) (AccountNote) x;
			AccountCrime b = (AccountCrime) (AccountNote) y;
			return a.isAFK.CompareTo(b.isAFK);
		}
	}

	[SaveableClass]
	public class AccountNote {
		[LoadingInitializer]
		public AccountNote() {
		}

		[SaveableData]
		public AbstractCharacter issuer; //GM who created the issue
		[SaveableData]
		public AbstractCharacter referredChar; //character on the acccount connected with the note (or crime)
		[SaveableData]
		public string text; //the notes text or the crimes description
		[SaveableData]
		public DateTime time; //time when the issue was created

		public AccountNote(AbstractCharacter issuer, AbstractCharacter referredChar, string text) {
			this.issuer = issuer;
			this.referredChar = referredChar;
			this.text = text;
			this.time = DateTime.Now;
		}
	}

	[SaveableClass]
	public class AccountCrime : AccountNote {
		[LoadingInitializer]
		public AccountCrime() {
		}

		[SaveableData]
		public string punishment; //the punishment description		

		[SaveableData]
		public bool isAFK; //is it the AFK crime?

		public AccountCrime(AbstractCharacter issuer, AbstractCharacter referredChar, string punishment, string crime)
			: base(issuer, referredChar, crime) {
			this.punishment = punishment;
		}

		public bool AFK { //setter for the AFK property
			set {
				this.isAFK = value;
			}
		}
	}
}
