/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See then
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using SteamEngine.Common;
using SteamEngine.Regions;

namespace SteamEngine {
	public sealed class MultiItemComponent : AbstractInternalItem, IPoint4D {
		internal MultiItemComponent prevInList;
		internal MultiItemComponent nextInList;
		internal MultiComponentLinkedList collection;

		private readonly MultiComponentDescription mcd;
		private readonly int multiFlags;
		private byte m;

		internal MultiItemComponent(MultiComponentDescription mcd, int id, Map map, int multiFlags)
			: base(id, map.Facet) {

			this.mcd = mcd;
			this.multiFlags = multiFlags;
			this.m = map.M;
		}

		internal void SetRelativePos(int centerX, int centerY, int centerZ) {
			checked {
				this.X = centerX + this.mcd.OffsetX;
				this.Y = centerY + this.mcd.OffsetY;
				this.Z = centerZ + this.mcd.OffsetZ;
			}
		}

		//useless?
		public int MultiFlags {
			get {
				return this.multiFlags;
			}
		}

		public MultiComponentDescription Mcd {
			get {
				return this.mcd;
			}
		}

		public byte M {
			get {
				return this.m;
			}
			internal set {
				this.m = value;
				this.Facet = Map.GetMap(value).Facet;
			}
		}

		public IPoint4D TopPoint {
			get {
				return this;
			}
		}

		public Map GetMap() {
			return Map.GetMap(this.m);
		}


		IPoint2D IPoint2D.TopPoint {
			get {
				return this;
			}
		}
	}

	//info about one item of multiItem
	public class MultiComponentDescription {
		private readonly int itemId;
		private readonly int offsetX;
		private readonly int offsetY;
		private readonly int offsetZ;
		private readonly int flags;

		public MultiComponentDescription(int id, int offsetX, int offsetY, int offsetZ, int flags) {
			this.itemId = id;
			this.offsetX = offsetX;
			this.offsetY = offsetY;
			this.offsetZ = offsetZ;
			this.flags = flags;
		}

		public int ItemId {
			get {
				return this.itemId;
			}
		}

		public int OffsetX {
			get {
				return this.offsetX;
			}
		}

		public int OffsetY {
			get {
				return this.offsetY;
			}
		}

		public int OffsetZ {
			get {
				return this.offsetZ;
			}
		}

		public int Flags {
			get {
				return this.flags;
			}
		}

		internal MultiItemComponent Create(int centerX, int centerY, int centerZ, Map map) {
			MultiItemComponent retVal = new MultiItemComponent(this, this.itemId, map, this.flags);
			retVal.SetRelativePos(centerX, centerY, centerZ);
			return retVal;
		}
	}

	public class MultiData {
		static Dictionary<int, MultiData> multiItems = new Dictionary<int, MultiData>();

		private readonly List<MultiComponentDescription> parts;

		public MultiData(int numI) {
			this.parts = new List<MultiComponentDescription>(numI);
		}

		public static MultiData GetByModel(int id) {
			MultiData retVal;
			multiItems.TryGetValue(id, out retVal);
			return retVal;
		}

		internal MultiItemComponent[] Create(ushort x, ushort y, sbyte z, Map map) {
			int n = this.parts.Count;
			MultiItemComponent[] retVal = new MultiItemComponent[n];
			for (int i = 0; i < n; i++) {
				retVal[i] = this.parts[i].Create(x, y, z, map);
			}
			return retVal;
		}

		public MultiComponentDescription this[int index] {
			get {
				return this.parts[index];
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		public static void Init() {
			if (Globals.UseMultiItems) {
				Console.WriteLine("Loading " + LogStr.File("Multi.mul") + " - multi items info.");

				string mulFileP = Path.Combine(Globals.MulPath, "multi.mul");
				string idxFileP = Path.Combine(Globals.MulPath, "multi.idx");

				if (File.Exists(mulFileP) && File.Exists(idxFileP)) {
					FileStream idxfs = new FileStream(idxFileP, FileMode.Open, FileAccess.Read);
					FileStream mulfs = new FileStream(mulFileP, FileMode.Open, FileAccess.Read);
					BinaryReader idxbr = new BinaryReader(idxfs);
					BinaryReader mulbr = new BinaryReader(mulfs);

					int pos, length;
					int slots = 0, items = 0;
					MultiData listOfItems;
					MultiComponentDescription part;

					try {
						while (true) {
							if ((pos = idxbr.ReadInt32()) != -1) {
								length = idxbr.ReadInt32();
								idxbr.BaseStream.Seek(4, SeekOrigin.Current);

								if (mulbr.BaseStream.Position != pos) {
									mulbr.BaseStream.Seek(pos, SeekOrigin.Begin);
								}
								listOfItems = new MultiData(length / 12);
								for (int i = 0; i < length / 12; i++) {
									ushort id = mulbr.ReadUInt16();
									short offsetX = mulbr.ReadInt16();
									short offsetY = mulbr.ReadInt16();
									short offsetZ = mulbr.ReadInt16();
									int flags = mulbr.ReadInt32();

									//s nulou jsou "stiny" ocekavanych dynamickych itemu, jako treba dveri.
									if (flags > 0) {
										part = new MultiComponentDescription(id, offsetX, offsetY, offsetZ, flags);
										listOfItems.parts.Add(part);
									}
								}
								multiItems[slots + 16384] = listOfItems;
								items++;
							} else {
								idxbr.BaseStream.Seek(8, SeekOrigin.Current);
							}
							slots++;
						}
					} catch (EndOfStreamException) {
					} catch (Exception e) {
						Logger.WriteWarning("Exception while reading Multi.mul/idx", e);
					} finally {
						Logger.WriteDebug("Num of multiItem slots: " + slots);
						Logger.WriteDebug("Num of multiItems: " + items);
					}

					Logger.WriteDebug("Finished loading multi.mul");
					mulbr.Close();
					idxbr.Close();

					if (Globals.WriteMulDocsFiles) {
						StreamWriter docsw = File.CreateText(Globals.GetMulDocPathFor("MultiItems.txt"));

						foreach (KeyValuePair<int, MultiData> entry in multiItems) {
							docsw.WriteLine("Item: " + entry.Key + "\t Num parts: " + entry.Value.parts.Count);
							foreach (MultiComponentDescription p in entry.Value.parts) {
								docsw.WriteLine("\t ItemId: " + p.ItemId);
								docsw.WriteLine("\t Offset X: " + p.OffsetX);
								docsw.WriteLine("\t Offset Y: " + p.OffsetY);
								docsw.WriteLine("\t Offset Z: " + p.OffsetZ);
								//docsw.WriteLine("\t Visible: "+p.flags);
								docsw.WriteLine();
							}
							docsw.WriteLine();
						}
						docsw.Close();
					}
				} else {
					Logger.WriteCritical("Unable to locate multi.idx or multi.mul. We're gonna crash soon ;)");
				}
			} else {
				Logger.WriteWarning("Ignoring multi.mul");
			}

		}
	}

	//for storing of MultiItemComponents in sectors
	internal class MultiComponentLinkedList : IEnumerable<MultiItemComponent> {
		internal MultiItemComponent firstMultiComponent;
		internal ushort count;

		internal void Add(MultiItemComponent multiComponent) {
			Sanity.IfTrueThrow((multiComponent.prevInList != null || multiComponent.nextInList != null),
				"'" + multiComponent + "' being added into a MultiComponentList while being in another cont already");
			MultiItemComponent next = this.firstMultiComponent;
			this.firstMultiComponent = multiComponent;
			multiComponent.prevInList = null;
			multiComponent.nextInList = next;
			if (next != null) {
				next.prevInList = multiComponent;
			}
			multiComponent.collection = this;
			this.count++;
		}

		internal bool Remove(MultiItemComponent multiComponent) {
			if (multiComponent.collection == this) {
				if (this.firstMultiComponent == multiComponent) {
					this.firstMultiComponent = multiComponent.nextInList;
				} else {
					multiComponent.prevInList.nextInList = multiComponent.nextInList;
				}
				if (multiComponent.nextInList != null) {
					multiComponent.nextInList.prevInList = multiComponent.prevInList;
				}
				multiComponent.prevInList = null;
				multiComponent.nextInList = null;
				this.count--;
				multiComponent.collection = null;
				return true;
			}
			return false;
		}

		internal MultiItemComponent Find(int x, int y, int z, int id) {
			MultiItemComponent mic = this.firstMultiComponent;
			while (mic != null) {
				if ((mic.X == x) && (mic.Y == y) && (mic.Z == z) && (mic.Id == id)) {
					return mic;
				}
				mic = mic.nextInList;
			}
			return null;
		}

		//internal MultiItemComponent this[int index] {
		//    get {
		//        if ((index >= this.count) || (index < 0)) {
		//            return null;
		//        }
		//        MultiItemComponent i = this.firstMultiComponent;
		//        int counter = 0;
		//        while (i != null) {
		//            if (index == counter) {
		//                return i;
		//            }
		//            i = i.nextInList;
		//            counter++;
		//        }
		//        return null;
		//    }
		//}

		public IEnumerator<MultiItemComponent> GetEnumerator() {
			return new MultiComponentListEnumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return new MultiComponentListEnumerator(this);
		}

		private class MultiComponentListEnumerator : IEnumerator<MultiItemComponent> {
			MultiComponentLinkedList cont;
			MultiItemComponent current;
			MultiItemComponent next;//this is because of the possibility 
			//that the current will be removed from the container during the enumeration
			public MultiComponentListEnumerator(MultiComponentLinkedList c) {
				this.cont = c;
				this.next = this.cont.firstMultiComponent;
			}

			public void Reset() {
				this.current = null;
				this.next = this.cont.firstMultiComponent;
			}

			public bool MoveNext() {
				this.current = this.next;
				if (this.current == null) {
					return false;
				}
				this.next = this.current.nextInList;
				return true;
			}

			public MultiItemComponent Current {
				get {
					return this.current;
				}
			}

			object IEnumerator.Current {
				get {
					return this.current;
				}
			}

			public void Dispose() {				
			}
		}
	}
}
