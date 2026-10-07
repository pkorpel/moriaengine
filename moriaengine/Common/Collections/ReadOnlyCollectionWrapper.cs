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

using System.Collections;
using System.Collections.Generic;

namespace SteamEngine.Common {
	public sealed class ReadOnlyCollectionWrapper<T> : ICollection<T> /*, ICollection */ {
		ICollection<T> genericCollection;
		IEnumerable nonGenericEnumerable;

		public ReadOnlyCollectionWrapper(ICollection<T> collection) {
			this.genericCollection = collection;
			this.nonGenericEnumerable = collection;
		}

		public bool Contains(T item) {
			return this.genericCollection.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex) {
			this.genericCollection.CopyTo(array, arrayIndex);
		}

		public int Count {
			get { return this.genericCollection.Count; }
		}

		public IEnumerator<T> GetEnumerator() {
			return this.genericCollection.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.nonGenericEnumerable.GetEnumerator();
		}

		//public void CopyTo(Array array, int index) {
		//    this.nonGenericEnumerable.CopyTo(array, index);
		//}

		//bool ICollection.IsSynchronized {
		//    get { return false; }
		//}

		//object ICollection.SyncRoot {
		//    get { throw new SEException("The method or operation is not implemented."); }
		//}

		bool ICollection<T>.IsReadOnly {
			get { return true; }
		}

		void ICollection<T>.Add(T item) {
			throw new SEException("This Collection is read only.");
		}

		void ICollection<T>.Clear() {
			if (this.genericCollection.Count > 0) {
				throw new SEException("This Collection is read only.");
			}
		}

		bool ICollection<T>.Remove(T item) {
			throw new SEException("This Collection is read only.");
		}
	}
}