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
using SteamEngine.Common;

namespace SteamEngine.Regions {

	//class specialised for LOS computing. IEnumerable implementaion is for testing purposes only
	public class PointsInLine : Poolable, IEnumerable<Point3D> {

		private MutablePoint3D[] array = new MutablePoint3D[8];
		private int firstIndex;
		private int lastIndex;
		private bool sortByX;
		private bool ascending;
		MutablePoint3DComparer comparer;

		protected override void On_Reset() {
			this.Clear();
			base.On_Reset();
		}

		public void InitSorting(bool sortByX, bool ascending) {
			Sanity.IfTrueThrow(this.firstIndex <= this.lastIndex, 
				"this.firstIndex <= this.lastIndex"); //only init when empty
			this.sortByX = sortByX;
			if (sortByX) {
				this.comparer = ComparerByX.instance;
			} else {
				this.comparer = ComparerByY.instance;
			}
			this.ascending = ascending;
		}

		public void Clear() {
			this.firstIndex = this.array.Length / 2;
			this.lastIndex = this.firstIndex - 1;
			this.sortByX = true;
			this.comparer = ComparerByX.instance;
		}

		private abstract class MutablePoint3DComparer : IComparer<MutablePoint3D> {
			public abstract int Compare(MutablePoint3D a, MutablePoint3D b);
			public abstract int Compare(MutablePoint3D p, int x, int y);
		}

		private class ComparerByX : MutablePoint3DComparer {
			public static readonly ComparerByX instance = new ComparerByX();

			public override int Compare(MutablePoint3D a, MutablePoint3D b) {
				return a.x - b.x;
			}

			public override int Compare(MutablePoint3D p, int x, int y) {
				return p.x - x;
			}
		}

		private class ComparerByY : MutablePoint3DComparer {
			public static readonly ComparerByY instance = new ComparerByY();

			public override int Compare(MutablePoint3D a, MutablePoint3D b) {
				return a.y - b.y;
			}

			public override int Compare(MutablePoint3D p, int x, int y) {
				return p.y - y;
			}
		}

		private class EqualityComparerByXY : IEqualityComparer<MutablePoint3D> {
			public static readonly EqualityComparerByXY instance = new EqualityComparerByXY();

			public bool Equals(MutablePoint3D a, MutablePoint3D b) {
				return (a.x == b.x) && (a.y == b.y);
			}

			public int GetHashCode(MutablePoint3D obj) {
				throw new NotImplementedException();
			}
		}

		internal static bool EqualsXY(MutablePoint3D a, MutablePoint3D b) {
			return a.x == b.x && a.y == b.y;
		}

		internal static bool EqualsXY(MutablePoint3D p, int x, int y) {
			return p.x == x && p.y == y;
		}

		public void Add(int x, int y, int z) {
			if (this.ascending) {
				this.AddAsLast(x, y, z);
			} else {
				this.AddAsFirst(x, y, z);
			}
		}

		private void AddAsFirst(int x, int y, int z) {
			MutablePoint3D first = this.array[this.firstIndex];
			if (EqualsXY(first, x, y)) {
				//we do nothing, Z is not so relevant really
			} else if ((this.lastIndex >= this.firstIndex) && //comparing only makes sense if there's at least 1 point already
				(this.comparer.Compare(first, x, y) < 0)) {
				//breaking the sort is not allowed
				throw new SEException("this.comparer.Compare(first, x, y) < 0. first.x=" +
					first.x + ", first.y=" + first.y + ", x=" + x + ", y=" + y + ". sortByX=" + this.sortByX+", ascending="+this.ascending);
			} else {
				if (this.firstIndex <= 0) {
					this.Grow();
				}
				this.firstIndex--;
				this.array[this.firstIndex].SetXYZ(x, y, z);
			}
		}

		private void AddAsLast(int x, int y, int z) {
			MutablePoint3D last = this.array[this.lastIndex];
			if (EqualsXY(last, x, y)) {
				//we do nothing, Z is not so relevant really
			} else if ((this.lastIndex >= this.firstIndex) && //comparing only makes sense if there's at least 1 point already
				(this.comparer.Compare(last, x, y) > 0)) {
				//breaking the sort is not allowed
				throw new SEException("this.comparer.Compare(last, x, y) > 0. last.x=" +
					last.x + ", last.y=" + last.y + ", x=" + x + ", y=" + y + ". sortByX=" + this.sortByX + ", ascending=" + this.ascending);
			} else {
				if (this.lastIndex >= this.array.Length - 1) {
					this.Grow();
				}
				this.lastIndex++;
				this.array[this.lastIndex].SetXYZ(x, y, z);
			}
		}

		//grow the array and put the old contents somewhere in the middle
		private void Grow() {
			MutablePoint3D[] old = this.array;
			int oldFirstIndex = this.firstIndex;
			int diff = this.lastIndex - this.firstIndex;

			int len = old.Length * 4;
			this.array = new MutablePoint3D[len];
			this.firstIndex = len / 2;
			this.lastIndex = this.firstIndex + diff;

			Array.Copy(old, oldFirstIndex, this.array, this.firstIndex, diff + 1);
		}

		public bool Contains(IPoint2D p) {
			int z;
			return this.Contains(p.X, p.Y, out z);
		}

		public bool Contains(IPoint2D p, out int z) {
			return this.Contains(p.X, p.Y, out z);
		}

		public bool Contains(int x, int y, out int z) {
			MutablePoint3D p = new MutablePoint3D { x = (ushort) x, y = (ushort) y};

#if DEBUG
			//debug check using normal foreach. Could be normally in some kind of unittest but meh...
			int debugIndex = -1;
			int lastA = int.MinValue;
			for (int i = this.firstIndex; i <= this.lastIndex; i++) {

				//check if we're really sorted the way we should be
				if (this.sortByX) {
					Sanity.IfTrueThrow(this.array[i].x < lastA, "this.array[i].x < lastA");
					lastA = this.array[i].x;
				} else {
					Sanity.IfTrueThrow(this.array[i].y < lastA, "this.array[i].y < lastA");
					lastA = this.array[i].y;
				}

				if (EqualsXY(this.array[i], x, y)) {
					if (debugIndex == -1) {
						debugIndex = i;
					} else {
						throw new SanityCheckException("2 points with the same xy in one path?!");
					}
				}
			}
#endif
			int index = Array.BinarySearch(this.array, this.firstIndex, (this.lastIndex - this.firstIndex) + 1,
				p, this.comparer);			

			if (index >= 0) {
				//also check previous and next ones
				//there can only be 1-3 points on one A coordination
				int checkStart = Math.Max(index - 2, this.firstIndex);
				int checkEnd = Math.Min(index + 2, this.lastIndex);
				for (int i = checkStart; i <= checkEnd; i++) {
					if (EqualsXY(this.array[i], x, y)) {
#if DEBUG
						MutablePoint3D ip = this.array[i];
						MutablePoint3D debugp = this.array[debugIndex];
						Sanity.IfTrueThrow(debugIndex != i, "debugIndex != i.	" +
							"debugIndex=" + debugIndex + ", i=" + i + ",	" +
							"ip.x=" + ip.x + ", ip.y=" + ip.y + ",	" +
							"debugp.x=" + debugp.x + ", debugp.y=" + debugp.y + ",	" +
							"x=" + x + ", y=" + y + ", sortByX=" + this.sortByX); //can't use debugger right now :\
#endif
						z = this.array[i].z;
						return true;
					}
				}
			}

#if DEBUG
			Sanity.IfTrueThrow(debugIndex != -1, "debugIndex != -1. debugIndex=" + debugIndex + ", index=" + index);
#endif

			z = -1;
			return false;
		}

		#region IEnumerable<Point3D> Members

		IEnumerator<Point3D> IEnumerable<Point3D>.GetEnumerator() {
			for (int i = this.firstIndex; i <= this.lastIndex; i++) {
				MutablePoint3D p = this.array[i];
				yield return new Point3D(p.x, p.y, p.z);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			for (int i = this.firstIndex; i <= this.lastIndex; i++) {
				MutablePoint3D p = this.array[i];
				yield return new Point3D(p.x, p.y, p.z);
			}
		}

		#endregion
	}
}
