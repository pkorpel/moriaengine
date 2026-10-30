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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#841 - server-side helpers for Tests/Protocol/MultiMapTests.cs.</summary>
	public static class MultiMapTests {
		/// <summary>Walks out of every plane 2 region's spawn point: on map0 most of them are open sea, and nobody can take a
		/// step. Reports the regions a walk of 30 steps can't start from.</summary>
		[SteamFunction]
		public static void TestMultiMapWalks(Character self) {
			int checkedRegions = 0;
			string stuck = "";
			foreach (StaticRegion region in StaticRegion.AllRegions) {
				if (region.Mapplane == 2) {
					checkedRegions++;
					Map map = Map.GetMap(2);
					Point4D p = region.P;
					List<int[]> ring = WorldSpots.WalkableRing(map, p, self.MovementSettings, 30);
					if ((ring[0][0] == p.X) && (ring[0][1] == p.Y)) {
						stuck += " " + region.Defname;
					}
				}
			}
			Map plane2 = Map.GetMap(2);
			Console.WriteLine("MULTIMAP_WALKS: regions=" + checkedRegions + " sizeX=" + plane2.SizeX + " sizeY=" + plane2.SizeY + " plane0=" + Map.GetMap(0).SizeX + "," + Map.GetMap(0).SizeY +
				" land=" + plane2.GetTileZ(19, 1196) + " stuck=[" + stuck.Trim() + "]");
		}

		/// <summary>Args (one string): "x y z m" - takes me there.</summary>
		[SteamFunction]
		public static void TestMultiMapGo(Character self, ScriptArgs sa) {
			string[] a = sa.Args.Trim().Trim('"').Split(' ');
			self.Go(int.Parse(a[0]), int.Parse(a[1]), int.Parse(a[2]), byte.Parse(a[3]));
			Console.WriteLine("MULTIMAP_AT: m=" + self.M + " region=" + self.Region.Name);
		}
	}
}
