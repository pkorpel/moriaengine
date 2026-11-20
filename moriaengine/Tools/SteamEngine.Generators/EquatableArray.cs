using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace SteamEngine.Generators;

internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T> {
	private readonly ImmutableArray<T> values;

	internal EquatableArray(ImmutableArray<T> values) {
		this.values = values;
	}

	internal static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

	internal static EquatableArray<T> From(ImmutableArray<T> values) {
		return new EquatableArray<T>(values);
	}

	internal int Length => values.Length;

	internal T this[int index] => values[index];

	public bool Equals(EquatableArray<T> other) {
		if (Length != other.Length) {
			return false;
		}

		var comparer = EqualityComparer<T>.Default;
		for (var i = 0; i < Length; i++) {
			if (!comparer.Equals(values[i], other.values[i])) {
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) {
		return obj is EquatableArray<T> other && Equals(other);
	}

	public override int GetHashCode() {
		var comparer = EqualityComparer<T>.Default;
		var hash = 17;
		hash = (hash * 31) + Length;
		for (var i = 0; i < Length; i++) {
			var itemHash = values[i] is null ? 0 : comparer.GetHashCode(values[i]);
			hash = (hash * 31) + itemHash;
		}
		return hash;
	}

	public ImmutableArray<T>.Enumerator GetEnumerator() {
		return values.GetEnumerator();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator() {
		return ((IEnumerable<T>) values).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator() {
		return ((IEnumerable) values).GetEnumerator();
	}
}
