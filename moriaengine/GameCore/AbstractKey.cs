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
using System.Collections.Concurrent;
using System.Threading;

namespace SteamEngine {

	public interface IAbstractKey {
		string Name { get; }
		int Uid { get; }
	}

	/// <summary>
	/// AbstractKey is used as an ID for tags, to make tag lookups
	/// fast and not use a lot of memory, etc. base class for all
	/// "key" classes
	/// </summary>
	public abstract class AbstractKey<T> : IAbstractKey where T : IAbstractKey {
		private static int uids;

		private static ConcurrentDictionary<string, T> byName = new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);

		private readonly string name;
		private readonly int uid;

		protected AbstractKey(string name, int uid) {
			this.name = name;
			this.uid = uid;
		}

		public string Name {
			get { return this.name; }
		}

		public int Uid {
			get { return this.uid; }
		}

		public sealed override int GetHashCode() {
			return this.uid;
		}

		public sealed override string ToString() {
			return this.name;
		}

		public sealed override bool Equals(object obj) {
			return ReferenceEquals(this, obj);
		}

		protected static T Acquire(string name, Func<string, int, T> factory) {
			var k = byName.GetOrAdd(name,
				n => {
					var uid = Interlocked.Increment(ref uids);
					return factory(n, uid);
				});

			return k;
		}
	}
}