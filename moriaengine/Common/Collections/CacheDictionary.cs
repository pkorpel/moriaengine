using System;
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine {
	/// <summary>
	/// A Dictionary that forgets entries that it receieved if they haven't been used in the last 'maxCacheItems' usages of the dictionary.
	/// The maxCacheItems number should typically be pretty big, in thousands or more.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public sealed class CacheDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
		private Dictionary<TKey, CacheDictionaryKeyEntry> dict;
		private LinkedList<TKey> linkedList = new LinkedList<TKey>();
		private readonly int maxCacheItems;
		private readonly bool disposeOnRemove;

		public CacheDictionary(int maxCacheItems, bool disposeOnRemove) {
			if (maxCacheItems < 1) {
				throw new SEException("maxCacheItems must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>();
			this.maxCacheItems = maxCacheItems;
			this.disposeOnRemove = disposeOnRemove;
		}

		public CacheDictionary(int maxCacheItems, bool disposeOnRemove, IEqualityComparer<TKey> comparer) {
			if (maxCacheItems < 1) {
				throw new SEException("maxCacheItems must be higher than 0");
			}
			this.dict = new Dictionary<TKey, CacheDictionaryKeyEntry>(comparer);
			this.maxCacheItems = maxCacheItems;
			this.disposeOnRemove = disposeOnRemove;
		}

		public int MaxCacheItems {
			get {
				return this.maxCacheItems;
			}
		}

		public bool DisposeOnRemove {
			get {
				return this.disposeOnRemove;
			}
		}

		private struct CacheDictionaryKeyEntry {
			internal TValue value;
			internal LinkedListNode<TKey> node;
			internal CacheDictionaryKeyEntry(TValue value, LinkedListNode<TKey> node) {
				this.value = value;
				this.node = node;
			}
		}

		#region IDictionary<TKey, TValue> Members
		public void Add(TKey key, TValue value) {
			if (this.dict.ContainsKey(key)) {
				throw new SEException("Adding duplicate");
			}
			this.linkedList.AddFirst(key);
			this.dict.Add(key, new CacheDictionaryKeyEntry(value, this.linkedList.First));
			this.PurgeLastIfNeeded();
		}

		private void PurgeLastIfNeeded() {
			if (this.linkedList.Count > this.maxCacheItems) {
				LinkedListNode<TKey> node = this.linkedList.Last;
				this.linkedList.RemoveLast();
				if (this.disposeOnRemove) {
					IDisposable disposable = this.dict[node.Value] as IDisposable;
					if (disposable != null) {
						disposable.Dispose();
					}
				}
				this.dict.Remove(node.Value);
			}
		}

		public bool ContainsKey(TKey key) {
			CacheDictionaryKeyEntry valueEntry;
			if (this.dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
				return true;
			}
			return false;
		}

		public bool Remove(TKey key) {
			CacheDictionaryKeyEntry valueEntry;
			if (this.dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				this.dict.Remove(key);
				if (this.disposeOnRemove) {
					IDisposable disposable = valueEntry.value as IDisposable;
					if (disposable != null) {
						disposable.Dispose();
					}
				}
				return true;
			}
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value) {
			CacheDictionaryKeyEntry valueEntry;
			if (this.dict.TryGetValue(key, out valueEntry)) {
				this.linkedList.Remove(valueEntry.node);
				this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
				value = valueEntry.value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key] {
			get {
				return this.dict[key].value;
			}

			set {
				CacheDictionaryKeyEntry valueEntry;
				if (this.dict.TryGetValue(key, out valueEntry)) {
					this.linkedList.Remove(valueEntry.node);
					this.linkedList.AddFirst(valueEntry.node);//put our node on the first place
					this.dict[key] = new CacheDictionaryKeyEntry(value, valueEntry.node);
				} else {
					this.linkedList.AddFirst(key);
					this.dict[key] = new CacheDictionaryKeyEntry(value, this.linkedList.First); //should be Add(,) here, but it reveals some hideous .NET 2.0 bug in optimized build
					this.PurgeLastIfNeeded();
				}
			}
		}

		public ICollection<TKey> Keys {
			get {
				return this.dict.Keys;
			}
		}

		public ICollection<TValue> Values {
			get {
				throw new SEException("The method or operation is not implemented.");
			}
		}
		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item) {
			this.Add(item.Key, item.Value);
		}

		public void Clear() {
			this.dict.Clear();
			this.linkedList.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			return this.ContainsKey(item.Key);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			throw new SEException("The method or operation is not implemented.");
		}

		public int Count {
			get {
				return this.dict.Count;
			}
		}

		public bool IsReadOnly {
			get {
				return false;
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item) {
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>> Members

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			throw new SEException("The method or operation is not implemented.");
		}

		#endregion
	}
}