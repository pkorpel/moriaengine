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
using System.Diagnostics;

namespace SteamEngine {

	//a very simple queue class, which uses sort of "circular" algorhitm, 
	[DebuggerDisplay("Count = {Count}")]
	public class SimpleQueue<T> {
		private T[] array;
		private int headindex;
		private int tailindex;
		private int count;

		public SimpleQueue() {
			this.array = new T[32];
			this.headindex = 0;
			this.tailindex = 0;
		}

		public int Count {
			get {
				return this.count;
			}
		}

		public T Peek() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			return this.array[this.headindex];
		}

		public T Dequeue() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			T t = this.array[this.headindex];
			this.array[this.headindex] = default(T);
			this.headindex = ((this.headindex + 1) % this.array.Length);
			this.count--;
			return t;
		}

		public void Enqueue(T obj) {
			if (this.count == this.array.Length) {
				this.Grow();
			}
			this.array[this.tailindex] = obj;
			this.tailindex = ((this.tailindex + 1) % this.array.Length);
			this.count++;
		}

		public void Clear() {
			Array.Clear(this.array, 0, this.array.Length);
			this.headindex = 0;
			this.tailindex = 0;
			this.count = 0;
		}

		private void Grow() {
			T[] newArray = new T[this.array.Length * 2 + 1];
			if (this.count > 0) {
				if (this.headindex < this.tailindex) {
					Array.Copy(this.array, this.headindex, newArray, 0, this.count);
				} else {
					Array.Copy(this.array, this.headindex, newArray, 0, (this.array.Length - this.headindex));
					Array.Copy(this.array, 0, newArray, (this.array.Length - this.headindex), this.tailindex);
				}
			}
			this.array = newArray;
			this.headindex = 0;
			this.tailindex = this.count;
		}

		public bool Contains(T item) {
			int index = this.headindex;
			int num2 = this.count;
			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			while (num2-- > 0) {
				if (item == null) {
					if (this.array[index] == null) {
						return true;
					}
				} else if ((this.array[index] != null) && comparer.Equals(this.array[index], item)) {
					return true;
				}
				index = (index + 1) % this.array.Length;
			}
			return false;
		}
	}
}