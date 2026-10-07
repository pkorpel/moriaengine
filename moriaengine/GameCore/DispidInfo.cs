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

using System.Collections.Generic;
using SteamEngine.Common;

namespace SteamEngine {
	public class ItemDispidInfo {
		private static List<ItemDispidInfo> array = new List<ItemDispidInfo>();

		private readonly int id;

		private readonly TileFlag flags;
		private readonly byte weight;
		private readonly byte quality;
		private readonly int unknown1;
		private readonly byte minItemsToDisplayThisArt;
		private readonly byte quantity;
		private readonly int animId;
		private readonly byte unknown2;
		private readonly byte hue;
		private readonly int unknown3;
		private readonly byte height;
		private readonly byte calcHeight; //half for bridges
		private readonly string singularName;
		private readonly string pluralName;
		private readonly bool isEmpty;

		public int Id {
			get {
				return this.id;
			}
		}

		public TileFlag Flags {
			get {
				return this.flags;
			}
		}

		public byte Weight {
			get {
				return this.weight;
			}
		}

		public byte Quality {
			get {
				return this.quality;
			}
		}

		public int Unknown1 {
			get {
				return this.unknown1;
			}
		}

		public byte MinItemsToDisplayThisArt {
			get {
				return this.minItemsToDisplayThisArt;
			}
		}

		public byte Quantity {
			get {
				return this.quantity;
			}
		}

		public int AnimId {
			get {
				return this.animId;
			}
		}

		public byte Unknown2 {
			get {
				return this.unknown2;
			}
		}

		public byte Hue {
			get {
				return this.hue;
			}
		}

		public int Unknown3 {
			get {
				return this.unknown3;
			}
		}

		public byte Height {
			get {
				return this.height;
			}
		}

		public byte CalcHeight {
			get {
				return this.calcHeight;
			}
		}

		public string SingularName {
			get {
				return this.singularName;
			}
		}

		public string PluralName {
			get {
				return this.pluralName;
			}
		}

		public bool IsEmpty {
			get {
				return this.isEmpty;
			}
		}

		internal ItemDispidInfo(TileFlag flags, byte weight, byte quality, int unknown, byte minItemsToDisplayThisArt, byte quantity, int animID, byte unknown2, byte hue, int unknown3, byte height, string name) {
			this.flags = flags;
			this.weight = weight;
			this.quality = quality;
			this.unknown1 = unknown;
			this.minItemsToDisplayThisArt = minItemsToDisplayThisArt;
			this.quantity = quantity;
			this.animId = animID;
			this.unknown2 = unknown2;
			this.hue = hue;
			this.unknown3 = unknown3;
			this.height = height;
			if ((flags & TileFlag.Bridge) == TileFlag.Bridge) {
				this.calcHeight = (byte) (height / 2);
			} else {
				this.calcHeight = height;
			}

			ParseName(name, out this.singularName, out this.pluralName);
			this.singularName = string.Intern(this.singularName);
			this.pluralName = string.Intern(this.pluralName);

			this.id = array.Count;
			array.Add(this);
			this.isEmpty = ((flags == 0 || flags == TileFlag.Unknown2) && (weight == 1 || weight == 0 || weight == 255) &&
						quality == 0 && unknown == 0 && minItemsToDisplayThisArt == 0 && quantity == 0 && animID == 0 &&
						unknown2 == 0 && hue == 0 && unknown3 == 0 && name.Length == 0);
			//height is sometimes not 0 for these.
		}

		public override bool Equals(object obj) {
			ItemDispidInfo idi = obj as ItemDispidInfo;
			if (idi != null) {
				return (this.flags == idi.flags && this.weight == idi.weight && this.quality == idi.quality && this.unknown1 == idi.unknown1 &&
						this.minItemsToDisplayThisArt == idi.minItemsToDisplayThisArt && this.quantity == idi.quantity &&
						this.animId == idi.animId && this.unknown2 == idi.unknown2 && this.hue == idi.hue && this.unknown3 == idi.unknown3 &&
						this.height == idi.height && this.singularName == idi.singularName);
			}
			return false;
		}

		//This method exists to suppress stupid warnings (We have Equals for dupeitem detection in TileData).
		public override int GetHashCode() {
			throw new SEException("ItemDispidInfo cannot return a hash code.");
		}

		public static int Count {
			get {
				return array.Count;
			}
		}

		public static ItemDispidInfo GetByModel(int num)
		{
			if (num >= 0 && num < array.Count) {
				return array[num];
			}
			return null;
		}

		internal static bool ParseName(string name, out string singular, out string plural) {
			int percentPos = name.IndexOf("%");
			if (percentPos == -1) {
				singular = name;
				plural = name;
			} else {
				string before = name.Substring(0, percentPos);
				string singadd = "";
				string pluradd = "";
				int percentPos2 = name.IndexOf("%", percentPos + 1);
				int slashPos = name.IndexOf("/", percentPos + 1);
				string after = "";
				if (percentPos2 == -1) {	//This is sometimes the case in the tiledata info...
					pluradd = name.Substring(percentPos + 1);
				} else if (slashPos == -1 || slashPos > percentPos2) {
					if (percentPos2 == name.Length - 1) {
						after = "";
					} else {
						after = name.Substring(percentPos2 + 1);
					}
					pluradd = name.Substring(percentPos + 1, percentPos2 - percentPos - 1);
				} else { //This is: if (slashPos<percentPos2) {
					Sanity.IfTrueThrow(!(slashPos < percentPos2), "Expected that this else would mean slashPos<percentPos2, but it is not the case now. slashPos=" + slashPos + " percentPos2=" + percentPos2);
					if (slashPos == name.Length - 1) {
						after = "";
					} else {
						after = name.Substring(slashPos + 1);
					}
					pluradd = name.Substring(percentPos + 1, slashPos - percentPos - 1);
					singadd = name.Substring(slashPos + 1, percentPos2 - slashPos - 1);
				}
				singular = before + singadd + after;
				plural = before + pluradd + after;
				return true;
			}
			return false;
		}
	}
}