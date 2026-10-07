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

//**********************************************************
//* PriorityQueue                                          *
//* Copyright (c) Julian M Bucknall 2004                   *
//* All rights reserved.                                   *
//* This code can be used in your applications, providing  *
//*    that this copyright comment box remains as-is       *
//**********************************************************
//* .NET priority queue class (heap algorithm)             *
//**********************************************************

//the queue has been specialized for use with our Timer class
//the original file still most probably lies in the 3dParty directory

using System;
using System.Diagnostics.CodeAnalysis;

namespace SteamEngine.Timers {
	[SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public class TimerPriorityQueue {
		private int count;
		private int capacity;
		//private int version;
		private Timer[] heap;

		public TimerPriorityQueue() {
			this.capacity = 15;
			this.heap = new Timer[this.capacity];
		}

		public void Clear() {
			Array.Clear(this.heap, 0, this.heap.Length);
			this.count = 0;
		}

		public Timer Dequeue() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			Timer result = this.heap[0];
			this.count--;
			this.TrickleDown(0, this.heap[this.count]);
			this.heap[this.count] = null;
			//this.version++;
			result.index = -1;
			return result;
		}

		public void Remove(Timer timer) {
			if (timer.index > -1) {
				this.RemoveAt(timer.index);
				timer.index = -1;
			}
		}

		private void RemoveAt(int i) {
			this.count--;
			this.TrickleDown(i, this.heap[this.count]);
			this.heap[this.count] = null;
			//this.version++;
		}

		public Timer Peek() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			return this.heap[0];
		}

		public void Enqueue(Timer timer) {
			if (this.count == this.capacity) {
				this.Grow();
			}
			this.count++;
			this.BubbleUp(this.count - 1, timer);
			//this.version++;
		}

		private void BubbleUp(int index, Timer timer) {
			int parent = GetParentIndex(index);
			// note: (index > 0) means there is a parent
			while ((index > 0) &&
					(this.heap[parent].fireAt > timer.fireAt)) {
				//original line: (heap[parent].Priority.CompareTo(he.Priority) < 0)
				//is true when parent priority is lower than our current priority - in case of timers means it higher time
				Timer parentTimer = this.heap[parent];
				parentTimer.index = index;
				this.heap[index] = parentTimer;
				index = parent;
				parent = GetParentIndex(index);
			}
			timer.index = index;
			this.heap[index] = timer;
		}

		private static int GetLeftChildIndex(int index) {
			return (index * 2) + 1;
		}

		private static int GetParentIndex(int index) {
			return (index - 1) / 2;
		}

		private void Grow() {
			this.capacity = (this.capacity * 2) + 1;
			Timer[] newHeap = new Timer[this.capacity];
			Array.Copy(this.heap, 0, newHeap, 0, this.count);
			this.heap = newHeap;
		}

		private void TrickleDown(int index, Timer timer) {
			int child = GetLeftChildIndex(index);
			while (child < this.count) {
				if (((child + 1) < this.count) &&
						(this.heap[child].fireAt > this.heap[child + 1].fireAt)) {
					//orig. line: (heap[child].Priority.CompareTo(heap[child + 1].Priority) < 0))
					//is true when left child has lower priority than right one
					child++;
				}
				Timer childTimer = this.heap[child];
				childTimer.index = index;
				this.heap[index] = childTimer;
				index = child;
				child = GetLeftChildIndex(index);
			}
			this.BubbleUp(index, timer);
		}

		//public IEnumerator GetEnumerator() {
		//    return new PriorityQueueEnumerator(this);
		//}

		public int Count {
			get {
				return this.count;
			}
		}

		//private class PriorityQueueEnumerator : IEnumerator<Timer> {
		//    private int index;
		//    private TimerPriorityQueue pq;
		//    private int version;

		//    public PriorityQueueEnumerator(TimerPriorityQueue pq) {
		//        this.pq = pq;
		//        this.Reset();
		//    }

		//    private void checkVersion() {
		//        if (this.version != this.pq.version) {
		//            throw new InvalidOperationException();
		//        }
		//    }

		//    public void Reset() {
		//        this.index = -1;
		//        this.version = this.pq.version;
		//    }

		//    public object Current {
		//        get {
		//            this.checkVersion();
		//            return this.pq.heap[index]; 
		//        }
		//    }

		//    public bool MoveNext() {
		//        this.checkVersion();
		//        if (this.index + 1 == this.pq.count) 
		//            return false;
		//        this.index++;
		//        return true;
		//    }

		//    Timer IEnumerator<Timer>.Current {
		//        get {
		//            this.checkVersion();
		//            return this.pq.heap[index]; 
		//        }
		//    }

		//    public void Dispose() {
		//    }
		//}
	}
}
