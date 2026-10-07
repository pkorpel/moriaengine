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
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {

	public sealed class TrackPoint : Disposable {
		public const ushort WORST_COLOR = 1827; //worst visible footsteps
		public const ushort BEST_COLOR = 1835; //best visible footsteps

		private readonly Point4D location;
		private readonly Player owner;
		private readonly int model;//model of the "footprint"
		private readonly TimeSpan createdAt;

		private TrackPoint newer;
		private TrackPoint older;
		private LinkedQueue queue;

		private uint fakeUID;

		public TrackPoint(IPoint4D location, Player owner, ushort model) {
			this.location = location as Point4D;
			if (this.location == null) {
				this.location = new Point4D(location);
			}
			this.owner = owner;
			this.model = model;
			this.createdAt = Globals.TimeAsSpan;
		}

		internal void TryDisposeFakeUID() {
			if (this.fakeUID != 0) {
				Thing.DisposeFakeUid(this.fakeUID);//return borrowed UID
				this.fakeUID = 0; //set to zero so no duplicities can occur!
			}
		}

		public Point4D Location {
			get {
				return this.location;
			}
		}

		public int Model {
			get {
				return this.model;
			}
		}

		public Player Owner {
			get {
				return this.owner;
			}
		}

		public TimeSpan CreatedAt {
			get {
				return this.createdAt;
			}
		}

		public uint FakeUID {
			get {
				if (this.fakeUID == 0) {
					this.fakeUID = Thing.GetFakeItemUid();
				}
				return this.fakeUID;
			}
		}

		public int GetColor(TimeSpan now, TimeSpan maxAge) {
			if (this.createdAt >= now) { //created in the future
				return BEST_COLOR;
			}
			double createdAtSeconds = this.createdAt.TotalSeconds;
			double nowSeconds = now.TotalSeconds;
			double maxAgeSeconds = maxAge.TotalSeconds;

			int color = (int) (BEST_COLOR - (BEST_COLOR - WORST_COLOR) * ((nowSeconds - createdAtSeconds) / maxAgeSeconds));

			if (BEST_COLOR > WORST_COLOR) {
				Sanity.IfTrueThrow(((color > BEST_COLOR) || (color < WORST_COLOR)), "color out of range");
				//} else { //commented because of unreachable code warning
				//    Sanity.IfTrueThrow(((color < BEST_COLOR) || (color > WORST_COLOR)), "color out of range");
			}
			return color;
		}

		public TrackPoint OlderNeighbor {
			get {
				return this.older;
			}
		}

		public TrackPoint NewerNeighbor {
			get {
				return this.newer;
			}
		}

		internal LinkedQueue Queue {
			get {
				return this.queue;
			}
		}

		//public override bool Equals(object o) {
		//    TrackPoint tp = o as TrackPoint;
		//    if (tp != null) {
		//        return (location.Equals(tp.location) && //same point
		//                (owner == tp.owner)); //for the same Character
		//    }
		//    return false;
		//}

		//public override int GetHashCode() {
		//    return location.GetHashCode();
		//}

		protected override void On_DisposeUnmanagedResources() { //it's not unmanaged resource but we want it to be called even when finalizing
			if (this.fakeUID != 0) {
				Thing.DisposeFakeUid(this.fakeUID);//dont forget to dispose the borrowed uid (if any) !!!
				this.fakeUID = 0;
			}
		}

		internal class LinkedQueue {
			private TrackPoint newest;
			private TrackPoint oldest;
			private int count;

			public TrackPoint Newest {
				get {
					return this.newest;
				}
			}

			public TrackPoint Oldest {
				get {
					return this.oldest;
				}
			}


			public int Count {
				get {
					return this.count;
				}
			}

			internal void AddNewest(TrackPoint tp) {
				Sanity.IfTrueThrow(tp.queue != null, "tp.queue != null");

				if (this.newest != null) {
					this.newest.newer = tp;
					tp.older = this.newest;
				} else {
					tp.older = null; //we're alone
					this.oldest = tp;
				}

				tp.newer = null; //no one is newer than newest
				this.newest = tp;
				tp.queue = this;
				this.count++;
			}

			internal void SliceOldest(TrackPoint newQueueOldest) {
				if (newQueueOldest == null) {
					this.Clear();
				} else {
					Sanity.IfTrueThrow(newQueueOldest.queue != this, "newQueueStart.queue != this");

					TrackPoint next = newQueueOldest.older;
					while (next != null) {
						TrackPoint tp = next;
						next = tp.older;
						tp.queue = null;
						tp.Dispose();
						this.count--;
					}
					newQueueOldest.older = null;
					this.oldest = newQueueOldest;
				}
			}


			public IEnumerable<TrackPoint> EnumerateFromOldest() {
				TrackPoint next = this.newest;
				if (next != null) {
					do {
						TrackPoint tp = next;
						next = tp.OlderNeighbor;
						yield return tp;
					} while (next != null);
				}
			}

			internal void RemoveAndDispose(TrackPoint tp) {
				Sanity.IfTrueThrow(tp.queue != this, "tp.queue != this");

				if (tp == this.oldest) {
					this.oldest = tp.newer;
					if (this.oldest != null) {
						this.oldest.older = null; //no one is older than oldest
					}
				} else if (tp.newer != null) {
					tp.newer.older = tp.older;
				}

				if (tp == this.newest) {
					this.newest = tp.older;
					if (this.newest != null) {
						this.newest.newer = null; //no one is newer than newest
					}
				} else if (tp.older != null) {
					tp.older.newer = tp.newer;
				}

				tp.queue = null;
				this.count--;
				tp.Dispose();
			}

			//not exactly invariant-safe, but we're all internal here anyway
			public void Clear() {
				TrackPoint next = this.newest;
				if (next != null) {
					do {
						TrackPoint tp = next;
						next = tp.OlderNeighbor;
						tp.queue = null;
						tp.Dispose();
					} while (next != null);
					this.newest = null;
				}
				this.oldest = null;

				this.count = 0;
			}
		}
	}
}