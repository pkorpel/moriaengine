using System;
using System.Collections;
using System.Collections.Generic;

namespace SteamEngine {
	internal struct WeakRefDictionaryKeyEntry {
		internal int hashCode;
		internal WeakReference weakKey;
		internal WeakRefDictionaryKeyEntry(object key) {
			this.hashCode = key.GetHashCode();
			this.weakKey = new WeakReference(key);
		}
	}

	public interface IPurgable {
		void Purge();
	}

	/// <summary>We purge the caches when saving world</summary>
	public static class WeakRefDictionaryUtils {
		internal static List<WeakReference> allCaches = new List<WeakReference>();

		public static void PurgeAll() {
			List<WeakReference> aliveCaches = new List<WeakReference>(allCaches.Count);
			foreach (WeakReference weakCache in allCaches) {
				IPurgable cache = weakCache.Target as IPurgable;
				if (cache != null) {
					cache.Purge();
					aliveCaches.Add(weakCache);
				}
			}
			allCaches = aliveCaches;
		}
	}

	/// <summary>
	/// Dictionary of weakly referenced keys and values. 
	/// That means you can put inside stuff and then later it may or may not be still inside :)
	/// Depending on whether it has been deleted meanwhile (if it's IDeletable) or eaten by system garbage collection.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TValue">The type of the value.</typeparam>
	public class WeakRefDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IPurgable
		where TKey : class
		where TValue : class {
		private Dictionary<WeakRefDictionaryKeyEntry, WeakReference> dict;
		private IEqualityComparer<TKey> comparer;

		public WeakRefDictionary()
			: this(null) {
		}

		public WeakRefDictionary(IEqualityComparer<TKey> comparer) {
			this.dict = new Dictionary<WeakRefDictionaryKeyEntry, WeakReference>(new CacheComparer(this));
			WeakRefDictionaryUtils.allCaches.Add(new WeakReference(this));
			if (comparer == null) {
				this.comparer = EqualityComparer<TKey>.Default;
			} else {
				this.comparer = comparer;
			}
		}

		private class CacheComparer : IEqualityComparer<WeakRefDictionaryKeyEntry> {
			private WeakRefDictionary<TKey, TValue> cache;

			internal CacheComparer(WeakRefDictionary<TKey, TValue> cache) {
				this.cache = cache;
			}

			public bool Equals(WeakRefDictionaryKeyEntry entryA, WeakRefDictionaryKeyEntry entryB) {
				TKey keyA = (TKey) entryA.weakKey.Target;
				TKey keyB = (TKey) entryB.weakKey.Target;
				if ((keyA != null) && (keyB != null)) {
					IDeletable deletable = keyA as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					deletable = keyB as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					return this.cache.comparer.Equals(keyA, keyB);
				}
				return false;
			}

			public int GetHashCode(WeakRefDictionaryKeyEntry entry) {
				object key = entry.weakKey.Target;
				if (key != null) {
					return key.GetHashCode();
				}
				return -1;
			}
		}

		#region IPurgable Members
		/// <summary>Clears out the entries where either the key or the value have become deleted (if they're IDeletable) or have been memory-collected</summary>
		public void Purge() {
			List<KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference>> aliveEntries = new List<KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference>>(this.dict.Count);
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.dict) {
				if (this.IsAlive(pair.Key.weakKey) && this.IsAlive(pair.Value)) {
					aliveEntries.Add(pair);
				}
			}
			if (aliveEntries.Count < this.dict.Count) { //something was deleted, we must rebuild
				this.dict.Clear();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in aliveEntries) {
					this.dict.Add(pair.Key, pair.Value);
				}
			}
		}

		private bool IsAlive(WeakReference weak) {
			object o = weak.Target;
			if (o != null) {
				IDeletable deletable = o as IDeletable;
				if (deletable != null) {
					if (deletable.IsDeleted) {
						return false;
					}
				}
				return true;
			}
			return false;
		}
		#endregion

		#region IDictionary<TKey, TValue> Members
		public void Add(TKey key, TValue value) {
			if (key == null) {
				throw new SEException("key is null");
			}
			if (value == null) {
				throw new SEException("value is null");
			}
			this.dict.Add(new WeakRefDictionaryKeyEntry(key),
				new WeakReference(value));
		}

		public bool ContainsKey(TKey key) {
			return this.dict.ContainsKey(new WeakRefDictionaryKeyEntry(key));
		}

		public bool Remove(TKey key) {
			return this.dict.Remove(new WeakRefDictionaryKeyEntry(key));
		}

		public bool TryGetValue(TKey key, out TValue value) {
			WeakReference weakWal;
			value = default(TValue);
			if (this.dict.TryGetValue(new WeakRefDictionaryKeyEntry(key), out weakWal)) {
				value = (TValue) weakWal.Target;
				if (value != null) {
					IDeletable deletable = value as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		public TValue this[TKey key] {
			get {
				WeakReference weakWal = this.dict[new WeakRefDictionaryKeyEntry(key)];
				TValue value = (TValue) weakWal.Target;
				if (value != null) {
					IDeletable deletable = value as IDeletable;
					if (deletable != null) {
						if (deletable.IsDeleted) {
							return default(TValue);
						}
					}
					return value;
				}
				return default(TValue);
			}
			set {
				if (key == null) {
					throw new SEException("key is null");
				}
				if (value == null) {
					throw new SEException("value is null");
				}
				this.dict[new WeakRefDictionaryKeyEntry(key)] = new WeakReference(value);
			}
		}

		private KeysCollection keys;
		public ICollection<TKey> Keys {
			get {
				if (this.keys == null) {
					this.keys = new KeysCollection(this);
				}
				return this.keys;
			}
		}

		private ValuesCollection values;
		public ICollection<TValue> Values {
			get {
				if (this.values == null) {
					this.values = new ValuesCollection(this);
				}
				return this.values;
			}
		}

		private class KeysCollection : ICollection<TKey> {
			private WeakRefDictionary<TKey, TValue> cache;

			internal KeysCollection(WeakRefDictionary<TKey, TValue> cache) {
				this.cache = cache;
			}

			public void Add(TKey item) {
				throw new SEException("readonly");
			}

			public void Clear() {
				throw new SEException("readonly");
			}

			public bool Remove(TKey item) {
				throw new SEException("readonly");
			}

			public bool Contains(TKey key) {
				return this.cache.dict.ContainsKey(new WeakRefDictionaryKeyEntry(key));
			}

			public void CopyTo(TKey[] array, int arrayIndex) {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					array[arrayIndex] = (TKey) pair.Key.weakKey.Target;
					arrayIndex++;
				}
			}

			public int Count {
				get {
					return this.cache.Count;
				}
			}

			public bool IsReadOnly {
				get {
					return true;
				}
			}

			public IEnumerator<TKey> GetEnumerator() {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					yield return (TKey) pair.Key.weakKey.Target;
				}
			}

			IEnumerator IEnumerable.GetEnumerator() {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					yield return pair.Key.weakKey.Target;
				}
			}
		}

		private class ValuesCollection : ICollection<TValue> {
			private WeakRefDictionary<TKey, TValue> cache;

			internal ValuesCollection(WeakRefDictionary<TKey, TValue> cache) {
				this.cache = cache;
			}

			public void Add(TValue item) {
				throw new SEException("readonly");
			}

			public void Clear() {
				throw new SEException("readonly");
			}

			public bool Remove(TValue item) {
				throw new SEException("readonly");
			}

			public bool Contains(TValue value) {
				return this.cache.dict.ContainsValue(new WeakReference(value));
			}

			public void CopyTo(TValue[] array, int arrayIndex) {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					array[arrayIndex] = (TValue) pair.Value.Target;
					arrayIndex++;
				}
			}

			public int Count {
				get {
					return this.cache.Count;
				}
			}

			public bool IsReadOnly {
				get {
					return true;
				}
			}

			public IEnumerator<TValue> GetEnumerator() {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					yield return (TValue) pair.Value.Target;
				}
			}

			IEnumerator IEnumerable.GetEnumerator() {
				this.cache.Purge();
				foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.cache.dict) {
					yield return pair.Value.Target;
				}
			}
		}

		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>> Members

		public void Add(KeyValuePair<TKey, TValue> item) {
			this.Add(item.Key, item.Value);
		}

		public void Clear() {
			this.dict.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item) {
			throw new SEException("The method or operation is not implemented.");
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			this.Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.dict) {
				array[arrayIndex] = new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
				checked {
					arrayIndex++;
				}
			}
		}

		public int Count {
			get {
				this.Purge();
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
			this.Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.dict) {
				yield return new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() {
			this.Purge();
			foreach (KeyValuePair<WeakRefDictionaryKeyEntry, WeakReference> pair in this.dict) {
				yield return new KeyValuePair<TKey, TValue>(
					(TKey) pair.Key.weakKey.Target, (TValue) pair.Value.Target);
			}
		}

		#endregion
	}
}