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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace SteamEngine.Common {
	[SuppressMessage("Microsoft.Design", "CA1039:ListsAreStronglyTyped"),
	SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix"),
	SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix"),
	SuppressMessage("Microsoft.Design", "CA1035:ICollectionImplementationsHaveStronglyTypedMembers"),
	SuppressMessage("Microsoft.Design", "CA1038:EnumeratorsShouldBeStronglyTyped")]
	public class EmptyReadOnlyCollection : IEnumerable, IEnumerator, IList, ICollection, IDictionary, IDictionaryEnumerator {
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Member")]
		[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
		public static readonly EmptyReadOnlyCollection instance = new EmptyReadOnlyCollection();

		public void CopyTo(Array array, int index) {
		}

		public int Count {
			get { return 0; }
		}

		public bool IsSynchronized {
			get { return true; }
		}

		public object SyncRoot {
			get { return this; }
		}

		public IEnumerator GetEnumerator() {
			return this;
		}

		public int Add(object value) {
			throw new NotSupportedSEException();
		}

		public void Clear() {
		}

		public bool Contains(object value) {
			return false;
		}

		public int IndexOf(object value) {
			return -1;
		}

		public void Insert(int index, object value) {
			throw new NotSupportedSEException();
		}

		public bool IsFixedSize {
			get {
				return true;
			}
		}

		public bool IsReadOnly {
			get { return true; }
		}

		public void Remove(object value) {
		}

		public void RemoveAt(int index) {
		}

		public object this[int index] {
			get {
				throw new NotSupportedSEException();
			}
			set {
				throw new NotSupportedSEException();
			}
		}

		public object Current {
			get {
				throw new NotSupportedSEException();
			}
		}

		public bool MoveNext() {
			return false;
		}

		public void Reset() {
		}

		public void Add(object key, object value) {
			throw new NotSupportedSEException();
		}

		IDictionaryEnumerator IDictionary.GetEnumerator() {
			return this;
		}

		public ICollection Keys {
			get { return this; }
		}

		public ICollection Values {
			get { return this; }
		}

		public object this[object key] {
			get {
				return null;
			}
			set {
				throw new NotSupportedSEException();
			}
		}

		public DictionaryEntry Entry {
			get {
				throw new NotSupportedSEException();
			}
		}

		public object Key {
			get {
				throw new NotSupportedSEException();
			}
		}

		public object Value {
			get {
				throw new NotSupportedSEException();
			}
		}
	}

	public class EmptyReadOnlyGenericCollection<T> : EmptyReadOnlyCollection, IEnumerable<T>, IEnumerator<T>, ICollection<T>, IList<T> {
		public new static readonly EmptyReadOnlyGenericCollection<T> instance = new EmptyReadOnlyGenericCollection<T>();

		public void Add(T item) {
			throw new NotSupportedSEException();
		}

		public bool Contains(T item) {
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex) {
		}

		public bool Remove(T item) {
			return false;
		}

		public new IEnumerator<T> GetEnumerator() {
			return this;
		}

		public new T Current {
			get {
				throw new NotSupportedSEException();
			}
		}

		public void Dispose() {
		}

		public int IndexOf(T item) {
			return -1;
		}

		public void Insert(int index, T item) {
			throw new NotSupportedSEException();
		}

		public new T this[int index] {
			get {
				throw new NotSupportedSEException();
			}
			set {
				throw new NotSupportedSEException();
			}
		}
	}

	public class EmptyReadOnlyDictionary<TKey, TValue> : EmptyReadOnlyGenericCollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue> {
		public new static readonly EmptyReadOnlyDictionary<TKey, TValue> instance = new EmptyReadOnlyDictionary<TKey, TValue>();

		public void Add(TKey key, TValue value) {
			throw new NotSupportedSEException();
		}

		public bool ContainsKey(TKey key) {
			return false;
		}

		public new ICollection<TKey> Keys {
			get { return EmptyReadOnlyGenericCollection<TKey>.instance; }
		}

		public bool Remove(TKey key) {
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value) {
			value = default(TValue);
			return false;
		}

		public new ICollection<TValue> Values {
			get { return EmptyReadOnlyGenericCollection<TValue>.instance; }
		}

		public TValue this[TKey key] {
			get {
				throw new NotSupportedSEException();
			}
			set {
				throw new NotSupportedSEException();
			}
		}
	}
}