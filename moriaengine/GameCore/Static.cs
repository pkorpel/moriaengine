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

using System.Globalization;
using SteamEngine.Networking;

namespace SteamEngine {
	public abstract class AbstractInternalItem : IPoint3D {
		private readonly ItemDispidInfo dispidInfo;
		private int x;
		private int y;
		private int z;
		private byte facet;

		internal AbstractInternalItem(int id, int facet) {
			this.dispidInfo = ItemDispidInfo.GetByModel(id);
			if (this.dispidInfo == null) {
				throw new SEException("No ItemDispidInfo for id 0x" + id.ToString("x", CultureInfo.InvariantCulture) + ". Something's wrong.");
			}
			this.facet = (byte) facet;
		}

		internal AbstractInternalItem(int id, int x, int y, int z, int facet)
			: this(id, facet) {
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public string Name {
			get {
				return this.dispidInfo.SingularName;
			}
		}

		public int Id {
			get {
				return this.dispidInfo.Id;
			}
		}

		public ItemDispidInfo DispidInfo {
			get {
				return this.dispidInfo;
			}
		} 

		public int X {
			get {
				return this.x;

			}
			internal set {
				this.x = value; 
			}
		}

		public int Y {
			get {
				return this.y;
			}
			internal set {
				this.y = value; 
			}
		}

		public int Z {
			get {
				return this.z;
			}
			internal set {
				this.z = value; 
			}
		}

		public int Facet {
			get {
				return this.facet;
			}
			internal set {
				this.facet = (byte) value;
			}
		}

		public int Height {
			get {
				return this.dispidInfo.Height;
			}
		}

		public TileFlag Flags {
			get {
				return this.dispidInfo.Flags;
			}
		}

		public override string ToString() {
			return this.Name + " at " + this.x + "," + this.y + "," + this.z + " (facet " + this.facet + ")";
		}

		public void OverheadMessage(string arg) {
			this.OverheadMessage(arg, 0);
		}

		public void OverheadMessage(string arg, int color) {
			PacketSequences.SendOverheadMessageFrom(Globals.SrcTcpConnection, this, arg, (ushort) color);
		}

		IPoint3D IPoint3D.TopPoint {
			get {
				return this;
			}
		}

		IPoint2D IPoint2D.TopPoint {
			get {
				return this;
			}
		}
	}

	public sealed class StaticItem : AbstractInternalItem {
		private readonly int color;

		internal StaticItem(ushort id, ushort x, ushort y, sbyte z, byte facet, int color)
			: base(id, x, y, z, facet) {

			this.color = color;
		}

		public int Color {
			get { return this.color; }
		}
	}
}
