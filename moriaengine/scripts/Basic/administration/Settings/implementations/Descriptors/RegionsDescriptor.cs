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

using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	[ViewDescriptor(typeof(Region), "Region",
	 new[] { "Parent", "Rectangles", "WorldRegion", "IsWorldRegion", "P", "HierarchyName", "CreatedAt" }
		)]
	public static class RegionDescriptor {
		//automaticky se zobrazi defname, createdAt, hierarchy index, mapplane
		[Button("Parent")]
		public static void Parent(object target) {
			Region parent = ((Region) target).Parent;
			if (parent != null) {
				Globals.SrcCharacter.Dialog(SingletonScript<D_Info>.Instance, new DialogArgs(parent));
			} else {
				D_Display_Text.ShowError("Neexistuje rodièovský region");
			}
		}

		[Button("Rectangles")]
		public static void Rectangles(object target) {
			DialogArgs newArgs = new DialogArgs();
			newArgs.SetTag(D_Region_Rectangles.regionTK, (Region) target);
			Globals.SrcCharacter.Dialog(SingletonScript<D_Region_Rectangles>.Instance, newArgs);
		}

		[GetMethod("Home Point", typeof(Point4D))]
		public static object GetPosition(object target) {
			return ((Region) target).P;
		}

		[SetMethod("Home Point", typeof(Point4D))]
		public static void SetPosition(object target, object value) {
			Region reg = (Region) target;
			Point4D point = null;
			if (value.GetType().IsAssignableFrom(typeof(Point4D))) {
				point = (Point4D) value;
			} else if (value is string) {
				point = (Point4D) ObjectSaver.Load((string) value);
			}
			if (reg.ContainsPoint(point)) {
				reg.P = point;
			} else {
				throw new SEException("Specified point " + point + " must lay in the region");
			}
		}

		[GetMethod("Parent name", typeof(string))]
		public static object GetParentName(object target)
		{
			if (((Region) target).Parent != null) {
				return ((Region) target).Parent.Name;
			}
			return "";
		}

		//Tohle v sobe nese informaci v podstate o poslednim resyncu :) (tehdy se reloadnou a znovu vzniknou)
		//hmm tak nebrat, v podstate je to k nicemu, a nejde to nejak dost dobre zformatovat
		//[GetMethod("Created at", typeof(string))]
		//public static object CretingTime(object target) {
		//    TimeSpan tms = HighPerformanceTimer.TicksToTimeSpan(((Region)target).CreatedAt);
		//    return tms.ToString();			
		//}
	}

	[ViewDescriptor(typeof(StaticRegion), "Region")]
	public static class StaticRegionDescriptor {

	}
}