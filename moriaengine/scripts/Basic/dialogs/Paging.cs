using System.Collections;
using System.Collections.Generic;

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

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>
	/// Data source for all paging dialogs (those, where there are displayed larger 
	/// collections of data that need to be divided to more pages)
	/// </summary>
	public interface IPageableCollection {
		/// <summary>Field for checking the OutOfBounds problem :)</summary>
		int LineCount { get; }
		/// <summary>Returns an enumeration for a subset of data (for one single page)</summary>
		IEnumerable GetPage(int firstLineIndex, int maxLinesOnPage);
	}

	public interface IPageableCollection<T> : IPageableCollection {
		new IEnumerable<T> GetPage(int firstLineIndex, int maxLinesOnPage);
	}

	/// <summary>Wrapper class for List{T} which allows us to use the paging</summary>
	public class PageableList<T> : IPageableCollection<T> {
		private List<T> wrappedList;

		public PageableList(List<T> wrappedList) {
			this.wrappedList = wrappedList;
		}

		#region IPageableCollection<T> Members
		public IEnumerable<T> GetPage(int firstLineIndex, int maxLinesOnPage) {
			return new Page(firstLineIndex, maxLinesOnPage, this.wrappedList);
		}
		#endregion

		#region IPageableCollection Members
		public int LineCount {
			get {
				return this.wrappedList.Count;
			}
		}

		IEnumerable IPageableCollection.GetPage(int firstLineIndex, int maxLinesOnPage) {
			return new Page(firstLineIndex, maxLinesOnPage, this.wrappedList);
		}
		#endregion

		/// <summary>Inner class for getting the exact range from the wrappedList. Similar to AbstractPage.</summary>
		public class Page : IEnumerator<T>, IEnumerable<T> {
			private int nextIndex, upperBound;
			private List<T> wrappedList;
			private T current;
			public Page(int startIndex, int pageSize, List<T> wrappedList) {
				this.nextIndex = startIndex; //index of the first item from the list tht will be returned
				this.upperBound = startIndex + pageSize; //index of the first item from the list tht will NOT be returned (the upper bound)
				this.wrappedList = wrappedList; //the list itself, for getting the items :)
			}

			#region IEnumerator Members
			object IEnumerator.Current {
				get {
					return this.current;
				}
			}

			/// <summary>
			/// The main iteration method. Make checks if we are not exceeding the page boundaries
			/// and check also if we are not exceeding the wrapped list boundaries...
			/// </summary>
			public bool MoveNext() {
				if (this.nextIndex < this.upperBound && this.nextIndex < this.wrappedList.Count) {
					this.current = this.wrappedList[this.nextIndex];
					this.nextIndex++; //prepare index for the next iteration round
					return true;
				}
				return false;
			}

			public void Reset() {
				throw new SEException("The method or operation is not implemented.");
			}
			#endregion

			#region IEnumerator<T> Members
			public T Current {
				get {
					return this.current;
				}
			}
			#endregion

			#region IDisposable Members
			public void Dispose() {
				//we dont care
			}
			#endregion

			#region IEnumerable<T> Members
			public IEnumerator<T> GetEnumerator() {
				return this;
			}
			#endregion

			#region IEnumerable Members
			IEnumerator IEnumerable.GetEnumerator() {
				return this;
			}
			#endregion
		}
	}
}