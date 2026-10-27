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
				//CreateLosPath can't handle a single column (nothing horizontal to trace a path
				//across) - but "nothing to block" doesn't follow: a FLOOR/roof is a HORIZONTAL
				//plane that blocks a perfectly VERTICAL sightline at this exact column, unlike a
				//wall (a vertical plane, which needs horizontal distance to intersect a
				//horizontal sightline - that case is what this early return was actually
				//guarding against). Real user-reported bug: an NPC directly above/below another
				//(same real static building, different floor) could still attack straight
				//through the floor between them.
				return !this.IsVerticalLosBlockedBetween(from.X, from.Y, from.Z, to.Z);
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
		/// Checks terrain, static/multi map data, AND real dynamically-placed ground items
		/// (e.g. a GM-placed i_wall_stone) for something solid enough to block sight at this
		/// single point.
		///
		/// Real bug found and fixed here: this used to deliberately skip dynamic Things/Items
		/// ("there's no real placeable-blocking content in the game yet to validate that
		/// against") - but i_wall_stone IS real, already-placeable content (confirmed working
		/// content elsewhere, e.g. HousingTests), and a user reported a titan mage caster
		/// walled in with it could still cast spells at (and damage) a target outside the
		/// walls. Real Item blocking already works correctly for MOVEMENT (see
		/// MapCalculations.IsOk's own `items` loop, using the exact same
		/// ItemDispidInfo.GetByModel(item.Model).Flags pattern mirrored below) - walking into a
		/// placed wall was never broken, only sight THROUGH one was.
		///
		/// Second real bug found and fixed here (#692): statics/items were gated on `Impassable`
		/// (a MOVEMENT flag - "can you walk here") rather than `Noshoot` (the real UO tiledata
		/// flag for "can you see/shoot through this", confirmed populated on walls - and
		/// confirmed absent on windows, which SHOULD be see-through despite being impassable to
		/// movement - and never confused with `Impassable`-but-not-`Noshoot` content).
		/// `Impassable`'s own Z-band [s.Z, s.Z+height) also let a ranged attacker standing BELOW
		/// an elevated wall's own registered base (e.g. a wall on a raised platform, attacker at
		/// true ground level outside) shoot/cast straight under it - the ray's interpolated Z at
		/// that column never entered the wall's band. Real UO buildings achieve elevation purely
		/// via static objects placed on unmodified, flat base terrain (confirmed by sampling real
		/// map sectors - terrain under elevated platforms stays at the true low Z, only the
		/// static's own Z is raised) - so a Noshoot-flagged static can safely be treated as
		/// physically continuous all the way down to the real terrain at its own column, closing
		/// that gap. (Terrain/land tiles themselves are NOT touched by this - confirmed via a
		/// full scan that `Noshoot` is never set in the land-tile flag table, only in the
		/// static/item dispid table, so the terrain check below must keep using `Impassable`.)
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
				if (TileData.HasFlag(s.Flags, TileFlag.Noshoot) &&
					(z >= GetNoshootBandLow(s.Flags, s.Z, landZ)) && (z < s.Z + GetNoshootHeight(s.Flags, s.Height))) {
					return true;
				}
			}

			foreach (AbstractItem item in this.GetItemsInRange(x, y, 0)) {
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);
				if (TileData.HasFlag(idi.Flags, TileFlag.Noshoot) &&
					(z >= GetNoshootBandLow(idi.Flags, item.Z, landZ)) && (z < item.Z + GetNoshootHeight(idi.Flags, idi.Height))) {
					return true;
				}
			}

			return false;
		}

		/// <summary>Same data sources and Noshoot/Impassable split as IsLosBlockedAt (see its own
		/// doc comment for the full #692 writeup), but for the same-column case: is anything
		/// blocking strictly between fromZ and toZ (exclusive of both - standing on your own
		/// floor, or the target's, never self-blocks, same convention as CanSeeLosFromTo's main
		/// loop skipping both endpoint columns), regardless of which one is higher.</summary>
		private bool IsVerticalLosBlockedBetween(int x, int y, int fromZ, int toZ) {
			if (!this.IsValidPos(x, y)) {
				return true;
			}

			int zLow = Math.Min(fromZ, toZ);
			int zHigh = Math.Max(fromZ, toZ);

			//real bug found verifying the fix above (#692): fromZ==toZ is a genuinely degenerate
			//query (e.g. Character.CanReach on an item sitting in your OWN backpack - same exact
			//point as yourself) - nothing can be "strictly between" a value and itself, so this
			//must always be clear regardless of what's at this column. Without this, a Noshoot
			//static whose band got extended down to a much lower landZ (the whole point of the
			//fix above, for a real elevation gap) could wrongly report "blocked" for someone
			//simply standing on TOP of it, since Z sits inside that wide extended band even
			//though there's zero distance - and therefore nothing to see through - between the
			//two query points.
			if (zLow == zHigh) {
				return false;
			}

			int landZ = this.GetTileZ(x, y);
			int landId = this.GetTileId(x, y);
			if (TileData.HasFlag(TileData.GetTileFlags(landId), TileFlag.Impassable) && (landZ > zLow) && (landZ < zHigh)) {
				return true;
			}

			foreach (AbstractInternalItem s in this.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				if (TileData.HasFlag(s.Flags, TileFlag.Noshoot) &&
					(s.Z + GetNoshootHeight(s.Flags, s.Height) > zLow) && (GetNoshootBandLow(s.Flags, s.Z, landZ) < zHigh)) {
					return true;
				}
			}

			foreach (AbstractItem item in this.GetItemsInRange(x, y, 0)) {
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);
				if (TileData.HasFlag(idi.Flags, TileFlag.Noshoot) &&
					(item.Z + GetNoshootHeight(idi.Flags, idi.Height) > zLow) && (GetNoshootBandLow(idi.Flags, item.Z, landZ) < zHigh)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>#708 - a Noshoot-flagged static's blocking band normally extends down to the
		/// real terrain Z at its own column (see IsLosBlockedAt's own #692 writeup) - correct for
		/// a WALL (a vertical plane that should block floor-to-ceiling even when its own
		/// registered base sits on a raised platform), but wrong for a ROOF (a thin horizontal
		/// plane far overhead) - extending a roof's band down to the ground made it block an
		/// ordinary ground-level ray between two characters both standing well below the actual
		/// roof, real-user-reported as "a ranged NPC has to walk all the way to melee range to
		/// attack a target standing under any roof, but not in the open". A Roof-flagged static
		/// (the real, separate TileFlag.Roof - "Don't fall through me!") uses its own Z as the
		/// lower bound instead, so it only blocks a sightline that actually tries to pass near its
		/// own real height, never one far beneath it.</summary>
		private static int GetNoshootBandLow(TileFlag flags, int staticZ, int landZ) {
			if (TileData.HasFlag(flags, TileFlag.Roof)) {
				return staticZ;
			}
			return Math.Min(staticZ, landZ);
		}

		/// <summary>#721 - a walkable `Surface` static (an ordinary floor/ground decoration, not a
		/// wall) with a tiledata `Height` of 0 was still treated as occupying a full 1-unit-tall
		/// blocking slab (the `Math.Max(height, 1)` minimum below, originally meant so a height-0
		/// WALL still blocks something despite messy source height data) - real-user-reported as
		/// "two characters standing a few tiles apart on open, flat, paved ground still can't see
		/// each other" once the floor tile they're both standing ON (same Z as their own feet)
		/// counted as solid right at that exact Z. A `Surface` tile is something you stand ON TOP
		/// of by definition - it should never block a sightline at its own real height when that
		/// height is genuinely 0; a non-walkable (wall/other) static still gets the old minimum-1
		/// floor so an all-zero-height obstacle still blocks at least something.</summary>
		private static int GetNoshootHeight(TileFlag flags, int height) {
			if (TileData.HasFlag(flags, TileFlag.Surface)) {
				return height;
			}
			return Math.Max(height, 1);
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