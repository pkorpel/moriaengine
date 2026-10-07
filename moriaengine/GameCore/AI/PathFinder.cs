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

//using System;
//using System.Collections;
//
//namespace SteamEngine.AI {
//	public class PathFinder {
//		public static int HeapSize = 10000;   // (TODO): make some stuff for generating heap size
//
//		private BinaryHeap OpenList;
//		private ArrayList ClosedList;
//
//		public ArrayList Path;
//
//		private Point4D goal;
//		public Point4D Goal {
//			get {
//				return(goal);
//			}
//			set {
//				goal = value;
//				
//				OpenList = new BinaryHeap(HeapSize);
//				ClosedList = new ArrayList();
//				Path = null;
//			}
//		}
//
//		public PathFinder() {
//		}
//
//		public bool IsClosed(Node current) {
//			foreach(Node n in ClosedList) {
//				if(current == n) return(true);
//			}	
//		
//			return(false);
//		}
//
//		private static int GetGcost() {
//			return(10);
//		}
//
//		private static int GetHcost(Point4D first, Point4D second) {
//			return(10 * (Math.Abs(first.x - second.x) + Math.Abs(first.y - second.y)));
//		}
//
//		public Point4D GetNextStep(Point4D start) {
//			OpenList = new BinaryHeap(8);
//			Node StartNode = new Node(start);
//			Node GoalNode = new Node((Point4D)goal);
//
//			if(ClosedList.Count == 0) {
//					ClosedList.Add(StartNode);
//			}
//
//			foreach(Node NewNode in StartNode.GetAdjacentNodes()) {
//				if(GoalNode == NewNode) {
//					return(NewNode.Point);
//				}
//				if(!IsClosed(NewNode)) {
//					NewNode.Hcost = GetHcost(NewNode.Point, GoalNode.Point);
//					OpenList.Add(NewNode);
//				}
//			}
//
//			ClosedList.Add(OpenList[1]);
//			return(OpenList[1].Point);
//		}
//
//		public static bool IsValidPos(int x, int y) {
//			return(x >= 0 && x <= 11 && y >= 0 && y <= 10);
//		}
//
//		public bool GeneratePath(Point4D start) {
//			Node StartNode = new Node(start);
//			OpenList.Add(StartNode);
//
//			Node Current = StartNode;
//			Current.Gcost = 0;
//
//			Node GoalNode = new Node((Point4D)goal);
//            
//			int UpdateIndex;
//
//			do {
//				if((Current = OpenList.GetLowestFcost()) == GoalNode) {
//					break;
//				}
//				ClosedList.Add(Current);
//
//				foreach(Node NewNode in Current.GetAdjacentNodes()) {
//					if(!IsClosed(NewNode)) {
//						if((UpdateIndex = OpenList.Contains(NewNode)) != 0) {
//							if(OpenList[UpdateIndex].Gcost > Current.Gcost + GetGcost()) {
//								OpenList[UpdateIndex].Gcost = Current.Gcost + GetGcost();
//								OpenList[UpdateIndex].Parent = Current;
//								OpenList[UpdateIndex].DirectionFromParent = NewNode.DirectionFromParent;
//								OpenList.UpdateNodePosition(UpdateIndex);
//							}
//						}
//						else {
//							NewNode.Parent = Current;
//							NewNode.Gcost = Current.Gcost + GetGcost();
//							NewNode.Hcost = GetHcost(NewNode.Point, GoalNode.Point);
//							OpenList.Add(NewNode);
//						}
//					}
//				}
//
//			} while(OpenList.Count != 0);
//
//			if(Current == GoalNode) {
//				Path = new ArrayList();
//
//				while(Current != StartNode) {
//					Path.Add(Current.Point);
//					Current = Current.Parent;
//				}
//				Path.Reverse();
//
//				return(true);
//			}
//
//			return(false);
//		}
//	}
//
//	public class Node {
//		private static sbyte[,] DirArray = {{0, 1}, {1, 1}, {1, 0}, {1, -1}, {0, -1}, {-1, -1}, {-1, 0}, {-1, 1}};
//
//		public Point4D Point;
//		public Node Parent;
//		public Direction DirectionFromParent;
//
//		public int Fcost {
//			get {
//				return(Gcost + Hcost);
//			}
//		}
//
//		public int Gcost, Hcost;
//
//		public Node(int _x, int _y) {
//			Point = new Point4D((ushort)_x, (ushort)_y);
//		}
//
//		public Node(Point4D p) {
//			Point = p;
//		}
//
//		public ArrayList GetAdjacentNodes() {
//			ArrayList nodes = new ArrayList();
//			Node tempNode;
//			ushort tmpX, tmpY;
//			bool[] walkable = new bool[4];
//
//			for(int i = 0; i < 8; i += 2) {
//				tmpX = (ushort)(Point.x + DirArray[i, 0]);
//				tmpY = (ushort)(Point.y + DirArray[i, 1]);
//
//				if(Map.IsWalkable(tmpX, tmpY, this.Point.m) && Map.IsValidPos(tmpX, tmpY, this.Point.m)) {
//					walkable[i / 2] = true;
//					tempNode = new Node(tmpX, tmpY);
//					tempNode.DirectionFromParent = (Directions)i;
//
//					nodes.Add(tempNode);
//				}
//			}
//
//			if(walkable[0] && walkable[1] && Map.IsWalkable((ushort)(Point.x + DirArray[1, 0]), (ushort)(Point.y + DirArray[1, 1]), this.Point.m)) {
//				tempNode = new Node(Point.x + DirArray[1, 0], Point.y + DirArray[1, 1]);
//				tempNode.DirectionFromParent = Directions.NorthEast;
//				nodes.Add(tempNode);
//			}
//			if(walkable[1] && walkable[2] && Map.IsWalkable((ushort)(Point.x + DirArray[3, 0]), (ushort)(Point.y + DirArray[3, 1]), this.Point.m)) {
//				tempNode = new Node(Point.x + DirArray[3, 0], Point.y + DirArray[3, 1]);
//				tempNode.DirectionFromParent = Directions.SouthEast;
//				nodes.Add(tempNode);
//			}
//			if(walkable[2] && walkable[3] && Map.IsWalkable((ushort)(Point.x + DirArray[5, 0]), (ushort)(Point.y + DirArray[5, 1]), this.Point.m)) {
//				tempNode = new Node(Point.x + DirArray[5, 0], Point.y + DirArray[5, 1]);
//				tempNode.DirectionFromParent = Directions.SouthWest;
//				nodes.Add(tempNode);
//			}
//			if(walkable[3] && walkable[0] && Map.IsWalkable((ushort)(Point.x + DirArray[7, 0]), (ushort)(Point.y + DirArray[7, 1]), this.Point.m)) {
//				tempNode = new Node(Point.x + DirArray[7, 0], Point.y + DirArray[7, 1]);
//				tempNode.DirectionFromParent = Directions.NorthWest;
//				nodes.Add(tempNode);
//			}
//
//			return(nodes);
//		}
//
//		public static bool operator ==(Node first, Node second) {
//			return(first.Point == second.Point);
//		}
//
//		public static bool operator !=(Node first, Node second) {
//			return(!(first == second));
//		}
//		
//		public override bool Equals(object o) {
//			if (o is Node) {
//				Node n=(Node)o;
//				return(Point == n.Point);
//			}
//			return false;
//		}
//		
//		public override int GetHashCode() {
//			return 37+(17*Point.GetHashCode());
//		}
//
//	}
//
//	public class BinaryHeap {
//		public Node[] Heap;
//
//		private int count = 0;
//		public int Count { get{return(count); }}
//
//		public BinaryHeap(int capacity) {
//			Heap = new Node[capacity + 1];
//		}
//		
//		public Node this [int index] {
//			get {
//				return(Heap[index]);
//			}
//		}
//		
//		public void Add(Node NewNode) {
//			Heap[++count] = NewNode;
//
//			int index = count;
//			int index1 = index / 2;
//
//			while((index > 1) && (Heap[index].Fcost < Heap[index1].Fcost)) {
//				Swap(ref Heap[index], ref Heap[index1]);
//				index = index1;
//				index1 = index / 2;
//			}
//		}
//
//		public Node GetLowestFcost() {
//			if(count == 0) {
//				return(null);
//			}
//
//			Node lowest = Heap[1];
//			if(count == 1) {
//				Heap[1] = null;
//				count--;
//				return(lowest);
//			}
//
//			Heap[1] = Heap[count];
//			Heap[count] = null;
//
//			count--;
//
//			int index = 1, index2 = 2;
//			bool change = true;
//
//			while((index2 <= count) && change){
//				if((index2) == count) {
//					if(Heap[index].Fcost > Heap[index2].Fcost)
//					Swap(ref Heap[index], ref Heap[index2]);
//					index = index2;
//				}
//				else {
//					int swapindex;
//
//					if(Heap[index2].Fcost < Heap[index2 + 1].Fcost) {
//						swapindex = index2;
//					}
//					else{
//						swapindex = index2 + 1;
//					}
//
//					if(Heap[index].Fcost > Heap[swapindex].Fcost) {
//						Swap(ref Heap[index], ref Heap[swapindex]);
//						index = swapindex;
//					}
//					else {
//						change = false;
//					}
//				}
//
//				index2 = index * 2;
//			}
//
//			return(lowest);
//		}
//
//		public int Contains(Node node) {         // returns zero if it's not there
//			for(int i = 1; i <= count; i++){
//				if(Heap[i] == node) {
//					return(i);
//				}
//			}
//			return(0);
//		}
//
//		public void UpdateNodePosition(int index) {
//			if((index < 1) || (index > count)) {
//				return;
//			}
//
//			if((index > 1) && (Heap[index].Fcost < Heap[index / 2].Fcost)) {
//				int index1 = index / 2;
//
//				while((index > 1) && (Heap[index].Fcost < Heap[index1].Fcost)) {
//					Swap(ref Heap[index], ref Heap[index1]);
//					index = index1;
//					index1 = index / 2;
//				}
//			}
//			else {
//            	int index2 = 2 * index;
//				bool change = true;
//
//				while((index2 <= count) && change){
//					if((index2) == count) {
//						if(Heap[index].Fcost > Heap[index2].Fcost)
//						Swap(ref Heap[index], ref Heap[index2]);
//						index = index2;
//					}
//					else {
//						int swapindex;
//
//						if(Heap[index2].Fcost < Heap[index2 + 1].Fcost) {
//							swapindex = index2;
//						}
//						else {
//							swapindex = index2 + 1;
//						}
//
//						if(Heap[index].Fcost > Heap[swapindex].Fcost) {
//							Swap(ref Heap[index], ref Heap[swapindex]);
//							index = swapindex;
//						}
//						else {
//							change = false;
//						}
//					}
//
//					index2 = index * 2;
//				}
//			}
//		}
//
//		private void Swap(ref Node First, ref Node Second) {
//			Node temp = First;
//			First = Second;
//			Second = temp;			
//		}
//		
//		public void Print() {
//			for(int i = 1; i <= count; i++) {
//				Console.Write(i+". "+Heap[i].Fcost + "["+Heap[i].Point.x+","+Heap[i].Point.y+"]; ");
//			}
//
//			Console.WriteLine();
//		}
//	}
//}