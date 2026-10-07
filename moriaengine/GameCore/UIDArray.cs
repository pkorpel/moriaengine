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

//SimpleQueue :D

namespace SteamEngine {
	internal class UIDArray : IEnumerable<Thing> {
		private Thing[] array = new Thing[minimalLength];
		private SimpleQueue<int> freeSlots = new SimpleQueue<int>();
		private int highestUsedIndex;
		private int count;
		private bool loadingFinished;

		private int version;

		private List<bool> fakeSlots = new List<bool>(minimalLength);
		private SimpleQueue<int> freeFakeSlots = new SimpleQueue<int>();

		private const int minimalLength = 1000;
		private const int startOffset = 1; //low uids (particularly 0) have been found to cause problems in client.
		private const int queueMaxCount = 1000;

		private const int highestPossibleUid = 0x00ffffff - 1;

		internal int HighestUsedIndex {
			get {
				return this.highestUsedIndex;
			}
		}

		internal int Count {
			get {
				return this.count;
			}
		}

		internal void Clear() {
			this.freeSlots.Clear();
			this.freeFakeSlots.Clear();
			this.fakeSlots.Clear();
			this.highestUsedIndex = 0;
			this.count = 0;
			this.array = new Thing[this.array.Length];
			this.loadingFinished = false;
			this.version++;
		}

		internal void AddLoaded(Thing o, int loadedUid) {
			int index = loadedUid - startOffset;
			if (this.loadingFinished) {
				throw new SEException("Add(object,index) disabled after LoadingFinished");
			}
			if (index < 0) {
				throw new SEException("Object with non-positive UID " + index + " found");
			}
			if (index >= this.array.Length) { //index is too high, make the array bigger!
				this.ResizeArray(index);
			}
			if (this.array[index] != null) {
				throw new SEException("Two objects attempted to take the same UID");
			}
			if (index > this.highestUsedIndex) {
				this.highestUsedIndex = index;
			}
			this.array[index] = o;
			this.count++;
			this.version++;
		}

		internal void Add(Thing o) {
			if (this.freeSlots.Count == 0) {	//no indexes in the freeSlots queue
				this.FillFreeSlotQueue();
			}
			int index = this.freeSlots.Dequeue();
			int uid = index + startOffset;
			if (uid >= (highestPossibleUid - this.fakeSlots.Count)) {
				throw new SEException("We're out of UIDs. This is baaaad.");
			}
			o.InternalSetUid(uid);
			this.array[index] = o;
			this.count++;

			if (uid > this.highestUsedIndex) {
				this.highestUsedIndex = uid;
			}
			this.version++;
		}

		private void FillFreeSlotQueue() {
			this.freeSlots.Clear();

			int counter = 0;
			for (int i = 0, n = this.array.Length; i < n; i++) {
				if (this.array[i] == null) {
					this.freeSlots.Enqueue(i);
					counter++;
					if (counter >= queueMaxCount) {
						break;
					}
				}
			}
			if (counter == 0) {
				this.ResizeArray();
				this.FillFreeSlotQueue();//try again
			}
		}

		private void ResizeArray() {
			this.ResizeImpl(this.array.Length * 2);
		}

		private void ResizeArray(int newHighestElement) {
			int newSize = Math.Max(newHighestElement + minimalLength, this.array.Length * 2);
			this.ResizeImpl(newSize);
		}

		private void ResizeImpl(int newSize) {
			Thing[] temp = new Thing[newSize];
			Array.Copy(this.array, temp, this.array.Length);
			this.array = temp;
		}

		internal void LoadingFinished() {
			this.loadingFinished = true;
		}

		internal Thing Get(int uid) { //may return a null object
			int index = uid - startOffset;
			if (index < this.array.Length && index >= 0) {
				return this.array[index];
			}
			return null;
		}

		internal void RemoveAt(int uid) {
			int index = uid - startOffset;
			if (this.array[index] != null) { //only add to queue if not already null
				this.array[index] = null;
				if (index == this.highestUsedIndex) {
					//we find the next highest used index (below the one so far)
					int i = this.highestUsedIndex;
					for (; i >= 0; i--) {
						if (this.array[i] != null) {
							break;
						}
					}
					this.highestUsedIndex = i;
				} else {
					this.freeSlots.Enqueue(index);
				}
				this.count--;
				this.version++;
			}
		}

		internal void ReIndexAll() {
			Thing[] origArray = this.array;
			int n = origArray.Length;
			Thing[] newArray = new Thing[n];

			for (int i = 0, newI = 0; i < n; i++) {
				Thing elem = origArray[i];
				if (!Equals(elem, null)) {
					newArray[newI] = elem;
					elem.InternalSetUid(newI + startOffset);
					newI++;
				}
			}
			this.version++;
		}

		internal int GetFakeUid() {
			if (this.freeFakeSlots.Count == 0) {	//no indexes in the freeSlots queue
				this.FillFreeFakeSlotQueue();
			}
			int index = this.freeFakeSlots.Dequeue();
			this.fakeSlots[index] = true;
			return highestPossibleUid - index;
		}

		private void FillFreeFakeSlotQueue() {
			this.freeFakeSlots.Clear();

			int counter = 0;
			for (int i = 0, n = this.fakeSlots.Capacity; i < n; i++) {
				if (i >= this.fakeSlots.Count) {
					this.fakeSlots.Add(false);
				}
				if (!this.fakeSlots[i]) {
					this.freeFakeSlots.Enqueue(i);
					counter++;
					if (counter >= queueMaxCount) {
						break;
					}
				}
			}
			if (counter == 0) {
				this.fakeSlots.Capacity = this.fakeSlots.Capacity * 2;
				this.FillFreeFakeSlotQueue();//try again
			}
		}

		internal void DisposeFakeUid(int uid) {
			int index = highestPossibleUid - uid;
			if (index < this.fakeSlots.Count) {
				if (this.fakeSlots[index]) {
					this.fakeSlots[index] = false;
					this.freeFakeSlots.Enqueue(index);
				}
			}
		}

		public IEnumerator<Thing> GetEnumerator() {
			int v = this.version;

			for (int i = 0, n = this.highestUsedIndex; i <= n; i++) {
				if (v != this.version) {
					throw new InvalidOperationException("The collection was modified after the enumerator was created.");
				}

				Thing elem = this.array[i];
				if (elem != null) {
					yield return elem;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
	}
}