
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
using System.Collections.Generic;

namespace SteamEngine {

	//adds a field the value of which is unique among all instances of TDef
	public abstract class AbstractIndexedDef<TDef, TIndex> : AbstractDef
		where TDef : AbstractIndexedDef<TDef, TIndex> {

		//for string indices, be case insensitive
		private static ConcurrentDictionary<TIndex, TDef> byIndex = (typeof(TIndex) == typeof(string)) ?
			new ConcurrentDictionary<TIndex, TDef>((IEqualityComparer<TIndex>) StringComparer.OrdinalIgnoreCase) :
			new ConcurrentDictionary<TIndex, TDef>();

		private TIndex index;

		protected AbstractIndexedDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
		}

		public static TDef GetByDefIndex(TIndex index) {
			TDef retVal;
			byIndex.TryGetValue(index, out retVal);
			return retVal;
		}

		public TIndex DefIndex {
			get {
				return this.index;
			}
			protected set {
				this.index = value;
			}
		}

		public static int IndexedCount {
			get {
				return byIndex.Count;
			}
		}

		public static ICollection<TDef> AllIndexedDefs {
			get {
				return byIndex.Values;
			}
		}

		public override AbstractScript Register() {
			try {

				TDef previous = byIndex.GetOrAdd(this.index, (TDef) this);
				if (previous != this) {
					throw new SEException("previous != this when registering AbstractIndexedDef '" + this.index + "'");
				}

			} finally {
				base.Register();
			}
			return this;
		}

		protected override void Unregister() {
			try {

				TDef previous;
				if (byIndex.TryRemove(this.index, out previous)) {
					if (previous != this) {
						if (!byIndex.TryAdd(this.index, previous)) {
							throw new FatalException("Parallel loading fucked up.");
						}
						throw new SEException("previous != this when unregistering AbstractScript '" + this.index + "'");
					}
				}

			} finally {
				base.Unregister();
			}
		}
	}
}
