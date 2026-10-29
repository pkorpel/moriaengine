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
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#813 - server-side helpers for Tests/Protocol/RegionArchiveTests.cs.</summary>
	public static class RegionArchiveTests {
		[SteamFunction]
		public static void TestRegionCount(Character self) {
			int all = 0, fromScripts = 0, gmModified = 0;
			foreach (StaticRegion r in StaticRegion.AllRegions) {
				all++;
				if (r.IsFromScripts) { fromScripts++; }
				if (r.IsGmModified) { gmModified++; }
			}
			Console.WriteLine("REGION_COUNT: all=" + all + " fromScripts=" + fromScripts + " gmModified=" + gmModified);
		}

		[SteamFunction]
		public static void TestRegionReport(Character self, ScriptArgs sa) {
			string defname = (string) sa.Argv[0];
			FlaggedRegion r = StaticRegion.GetByDefname(defname) as FlaggedRegion;
			if (r == null) {
				Console.WriteLine("REGION_REPORT: " + defname + " exists=False");
				return;
			}
			Console.WriteLine("REGION_REPORT: " + defname + " exists=True name=" + r.Name + " fromScripts=" + r.IsFromScripts +
				" gmModified=" + r.IsGmModified + " flags=0x" + ((int) r.Flags).ToString("x", CultureInfo.InvariantCulture) +
				" events=" + string.Join("+", r.RegionEvents) + " resources=" + string.Join("+", r.RegionResources) +
				" group=" + r.Group + " parent=" + ((r.Parent == null) ? "none" : r.Parent.Defname) + " rects=" + r.Rectangles.Count);
		}

		/// <summary>#818 - teleports self into the archive region (a spot the map lookup really
		/// puts in it) and reports whether a spell (Clumsy) passes the cast's region check there.</summary>
		[SteamFunction]
		public static void TestRegionCanCast(Character self, ScriptArgs sa) {
			string defname = sa.Args.Trim().Trim('"');
			FlaggedRegion region = (FlaggedRegion) StaticRegion.GetByDefname(defname);
			Map map = Map.GetMap(region.Mapplane);
			foreach (ImmutableRectangle rect in region.Rectangles) {
				for (int x = rect.MinX; x <= rect.MaxX; x++) {
					for (int y = rect.MinY; y <= rect.MaxY; y++) {
						if (map.GetRegionFor(x, y) == region) {
							self.Go(x, y, map.GetTileZ(x, y), region.Mapplane);
							Console.WriteLine("REGION_CAN_CAST: " + defname + " in=" + (self.Region == region) +
								" cast=" + SpellDef.GetById(1).CheckPermissionOutgoing(self));
							return;
						}
					}
				}
			}
			Console.WriteLine("REGION_CAN_CAST: " + defname + " in=False");
		}

		/// <summary>Which region is at m/x/y (what the map lookup really returns).</summary>
		[SteamFunction]
		public static void TestRegionAt(Character self, ScriptArgs sa) {
			byte m = Convert.ToByte(sa.Argv[0], CultureInfo.InvariantCulture);
			int x = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			int y = Convert.ToInt32(sa.Argv[2], CultureInfo.InvariantCulture);
			Region r = Map.GetMap(m).GetRegionFor(x, y);
			Console.WriteLine("REGION_AT: " + m + "," + x + "," + y + " -> " + ((r == null) ? "none" : r.Defname));
		}

		/// <summary>Like a GM would in-game: turns NoPvp on for an archive region, deletes another
		/// archive region, and creates a brand new one inside the world region.</summary>
		[SteamFunction]
		public static void TestRegionGmEdits(Character self) {
			FlaggedRegion edited = (FlaggedRegion) StaticRegion.GetByDefname("a_Ereborsky_dul");
			edited.Flags = edited.Flags | RegionFlags.NoPvp;
			StaticRegion.GetByDefname("a_dum_2").Delete();
			FlaggedRegion created = new FlaggedRegion("a_test_gm_region", StaticRegion.GetByDefname("a_world"));
			bool ok = created.InitializeNewRegion("Test GM Region", new Point4D(100, 100, 0, 0),
				new List<ImmutableRectangle> { new ImmutableRectangle(90, 90, 110, 110) });
			Console.WriteLine("REGION_GM_EDITS: done created=" + ok);
		}
	}
}
