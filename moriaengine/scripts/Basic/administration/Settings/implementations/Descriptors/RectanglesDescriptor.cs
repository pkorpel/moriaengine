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

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(MutableRectangle), "Rectangle",
	 new[] { "MinX", "MinY", "MaxX", "MaxY" }
		)]
	public static class MutableRectangleDescriptor {
		//nic nezobrazovat, jen zakazat gettery z parenta
		/*[GetMethod("MinX", typeof(ushort))]
		public static object GetMinX(object target) {
			return ((MutableRectangle)target).MinX;
		}
		[SetMethod("MinX", typeof(ushort))]
		public static void SetMinX(object target, object value) {
			((MutableRectangle)target).minX = (ushort)value;
		}

		[GetMethod("MinY", typeof(ushort))]
		public static object GetMinY(object target) {
			return ((MutableRectangle)target).MinY;
		}
		[SetMethod("MinY", typeof(ushort))]
		public static void SetMinY(object target, object value) {
			((MutableRectangle)target).minY = (ushort)value;
		}

		[GetMethod("MaxX", typeof(ushort))]
		public static object GetMaxX(object target) {
			return ((MutableRectangle)target).MaxX;
		}
		[SetMethod("MaxX", typeof(ushort))]
		public static void SetMaxX(object target, object value) {
			((MutableRectangle)target).maxX = (ushort)value;
		}

		[GetMethod("MaxY", typeof(ushort))]
		public static object GetMaxY(object target) {
			return ((MutableRectangle)target).MaxY;
		}
		[SetMethod("MaxY", typeof(ushort))]
		public static void SetMaxY(object target, object value) {
			((MutableRectangle)target).maxY = (ushort)value;
		}*/
	}
}