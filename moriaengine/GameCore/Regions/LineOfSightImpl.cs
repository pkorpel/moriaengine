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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using SteamEngine.Common;

namespace SteamEngine.Regions {
	public partial class Map {
		public bool CanSeeLosFromTo(IPoint3D from, IPoint3D to) {
#if TRACE
			int distance = Point2D.GetSimpleDistance(from, to);
			if (distance > Globals.MaxUpdateRange) {
				Logger.WriteWarning("CanSeeLosFromTo for 2 points outside update range...?", new StackTrace());
			}
#endif
			if ((from.X == to.X) && (from.Y == to.Y)) {
				return true; //CreateLosPath can't handle a single column, and there's nothing horizontal to block
			}

			// CreateLosPath returns a pooled PointsInLine under the IEnumerable<Point3D> interface -
			// it must be disposed (released back to its pool) however we leave this method.
			IEnumerable<Point3D> path = CreateLosPath(from, to);
			try {
				foreach (Point3D point in path) {
					if (((point.X == from.X) && (point.Y == from.Y)) ||
						((point.X == to.X) && (point.Y == to.Y))) {
						continue; //standing on your own tile, or the target's own tile, never self-blocks
					}
					if (this.IsLosBlockedAt(point.X, point.Y, point.Z)) {
						return false;
					}
				}
				return true;
			} finally {
				(path as IDisposable)?.Dispose();
			}
		}

		/// <summary>
		/// Checks terrain and static/multi map data for something solid enough to block sight at
		/// this single point. Deliberately does NOT check dynamic Things/Items yet (e.g. a
		/// player-placed crate or a future house wall) - there's no real placeable-blocking
		/// content in the game yet to validate that against (see the housing gap, issue #19);
		/// this covers the actual common case today, which is terrain and building statics.
		/// </summary>
		private bool IsLosBlockedAt(int x, int y, int z) {
			if (!this.IsValidPos(x, y)) {
				return true; //off the edge of the map - nothing to see through
			}

			int landZ = this.GetTileZ(x, y);
			int landId = this.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(landId), TileFlag.Impassable) && (z <= landZ)) {
				return true;
			}

			foreach (AbstractInternalItem s in this.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				if (TileData.HasFlag(s.Flags, TileFlag.Impassable) &&
					(z >= s.Z) && (z < s.Z + Math.Max(s.Height, 1))) {
					return true;
				}
			}

			return false;
		}


		public static double losPathWidth = 0.2; //can be 0..<0.5 - the closer to 0.5, the wider the path

		public static IEnumerable<Point3D> CreateLosPath(IPoint3D org, IPoint3D dest) {
			double diffx = dest.X - org.X;
			double diffy = dest.Y - org.Y;
			double diffz = dest.Z - org.Z;

			//org == dest should be handled elsewhere
			Sanity.IfTrueThrow((diffx == 0) && (diffy == 0), "(diffx == 0) && (diffy == 0)"); 

			double absdiffx = Math.Abs(diffx);
			double absdiffy = Math.Abs(diffy);

			double deltax, deltay, b, deltab, desta, deltaz,
				z = org.Z;
			int a, deltaa;
			bool useX;

			if (absdiffx > absdiffy) {
				deltax = diffx / absdiffx;
				deltay = diffy / absdiffx;
				deltaz = diffz / absdiffx;
				a = org.X;
				deltaa = (int) deltax;
				desta = dest.X;
				b = org.Y;
				deltab = deltay;
				useX = true;
			} else {
				deltax = diffx / absdiffy;
				deltay = diffy / absdiffy;
				deltaz = diffz / absdiffy;
				a = org.Y;
				deltaa = (int) deltay;
				desta = dest.Y;
				b = org.X;
				deltab = deltax;
				useX = false;
			}

			bool ascending;
			PointsInLine path = Pool<PointsInLine>.Acquire();
			switch (deltaa) {
				case 1:
					ascending = true; break;
				case -1:
					ascending = false; break;
				default:
					throw new SEException("deltaa != 1 || -1");
			}
			path.InitSorting(useX, ascending);

			int lastSinglePointX = 0,
				lastSinglePointY = 0; //need to init to 0, or compiler bitches, which it wouldn't if it was smarter
			bool lastPointWasSingle = false;

			for (int i = 0; i < 30; i++) {
				int b1 = (int) Math.Round(b + losPathWidth);
				int b2 = (int) Math.Round(b - losPathWidth);

				int roundedZ = (int) Math.Round(z);
				int p1x, p1y;
				GetPointXY(a, b1, useX, out p1x, out p1y);

				//first try to add the diagonal points, then add our new 1-2 points, 
				//otherwise we're breaking sort in PointsInLine 
				if (b1 == b2) { //we're only adding 1 point
					if (lastPointWasSingle) { //last time, we also added only 1 point
						int lastdx = p1x - lastSinglePointX;
						int lastdy = p1y - lastSinglePointY;

						//echo("lastdx:<lastdx>, lastdy:<lastdy>")

						//this and last point are diagonal
						if ((Math.Abs(lastdx) == 1) && (Math.Abs(lastdy) == 1)) {
							int halfWayZ = (int) Math.Round(z - (deltaz / 2)); 

							//add 2 diagonal points, we don't want to see through
							if (useX) {
								path.Add(lastSinglePointX, lastSinglePointY + lastdy, halfWayZ);
								path.Add(lastSinglePointX + lastdx, lastSinglePointY, halfWayZ);
							} else { //the same, just different order
								path.Add(lastSinglePointX + lastdx, lastSinglePointY, halfWayZ);
								path.Add(lastSinglePointX, lastSinglePointY + lastdy, halfWayZ);
							}
						}
					}
					lastSinglePointX = p1x;
					lastSinglePointY = p1y;
					lastPointWasSingle = true;
				} else {
					int p2x, p2y;
					GetPointXY(a, b2, useX, out p2x, out p2y);
					path.Add(p2x, p2y, roundedZ);
					lastPointWasSingle = false;
				}

				path.Add(p1x, p1y, roundedZ);

				if (a == desta) {
					return path;
				}

				a += deltaa;
				b += deltab;
				z += deltaz;
			}

			//this happens when diffa>30 or some error occurrs
			throw new SEException("LOS check boundary fail");
		}


		private static void GetPointXY(int a, int b, bool useX, out int x, out int y) {
			if (useX) {
				x = a;
				y = b;
			} else {
				x = b;
				y = a;
			}
		}
	}
}