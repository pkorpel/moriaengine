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

//the queue has been made generic
//the original file still most probably lies in the 3dParty directory

using System;
using System.Collections;

namespace SteamEngine.CompiledScripts {

	public class PriorityQueue<TNode, TPriority>
			where TPriority : IComparable<TPriority> {

		private int count;
		private int capacity;
		private int version;
		private Node<TNode, TPriority>[] heap;

		struct Node<TNNode, TNPriority> {
			internal TNNode value;
			internal TNPriority priority;
		}

		public PriorityQueue() {
			this.capacity = 15;
			this.heap = new Node<TNode, TPriority>[this.capacity];
		}

		public void Clear() {
			Array.Clear(this.heap, 0, this.count);
			this.count = 0;
		}

		public TNode Dequeue() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			TNode result = this.heap[0].value;
			this.count--;
			this.TrickleDown(0, this.heap[this.count]);
			//heap[count] = default(TNode);
			this.version++;
			return result;
		}

		private void RemoveAt(int i) {
			this.count--;
			this.TrickleDown(i, this.heap[this.count]);
			//heap[count] = default(TNode);
			this.version++;
		}

		public TNode Peek() {
			if (this.count == 0) {
				throw new SEException("count == 0");
			}
			return this.heap[0].value;
		}

		public void Enqueue(TNode nodeValue, TPriority priority) {
			if (this.count == this.capacity) {
				this.Grow();
			}
			this.count++;
			Node<TNode, TPriority> node = new Node<TNode, TPriority>();
			node.value = nodeValue;
			node.priority = priority;
			this.BubbleUp(this.count - 1, node);
			this.version++;
		}

		private void BubbleUp(int index, Node<TNode, TPriority> node) {
			int parent = this.GetParent(index);
			// note: (index > 0) means there is a parent

			while ((index > 0) &&
					(this.heap[parent].priority.CompareTo(node.priority) > 0)) {
				Node<TNode, TPriority> parentAStarNode = this.heap[parent];
				this.heap[index] = parentAStarNode;
				index = parent;
				parent = this.GetParent(index);
			}
			this.heap[index] = node;
		}

		private int GetLeftChild(int index) {
			return (index * 2) + 1;
		}

		private int GetParent(int index) {
			return (index - 1) / 2;
		}

		private void Grow() {
			this.capacity = (this.capacity * 2) + 1;
			Node<TNode, TPriority>[] newHeap = new Node<TNode, TPriority>[this.capacity];
			Array.Copy(this.heap, 0, newHeap, 0, this.count);
			this.heap = newHeap;
		}

		private void TrickleDown(int index, Node<TNode, TPriority> node) {
			int child = this.GetLeftChild(index);
			while (child < this.count) {
				if (((child + 1) < this.count) &&
						(this.heap[child].priority.CompareTo(this.heap[child + 1].priority) > 0)) {
					child++;
				}
				Node<TNode, TPriority> childAStarNode = this.heap[child];
				this.heap[index] = childAStarNode;
				index = child;
				child = this.GetLeftChild(index);
			}
			this.BubbleUp(index, node);
		}

		public IEnumerator GetEnumerator() {
			int startVersion = this.version;
			for (int i = 0; i < this.count; i++) {
				if (startVersion != this.version) {
					throw new SEException("Do not touch while enumerating");
				}
				yield return this.heap[i];
			}
		}

		public int Count {
			get {
				return this.count;
			}
		}
	}
}
