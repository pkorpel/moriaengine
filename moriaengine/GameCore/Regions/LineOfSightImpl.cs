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
				return !this.IsVerticalLosBlockedBetween(from.X, from.Y, from.Z, to.Z) &&
					!this.IsFloorBetween(from.X, from.Y, from.Z, to.Z, true); //#1148
			}

			//#728 - real-user-reported: a ranged NPC one tile over but on a different floor (e.g.
			//(x,y,40) vs (x,y+1,20), well within an adjacent-ish column) shot straight through a
			//real floor between them - confirmed even a solid stone wall placed at the exact same
			//spot didn't block either, so this isn't about any particular static's height data.
			//Root cause: CreateLosPath's per-step sampling produces exactly one (x,y,z) sample per
			//horizontal step - when the TOTAL horizontal distance is only 1 tile, the only two
			//samples it can produce land exactly on the "from" and "to" columns, and the skip
			//below ("standing on your own tile never self-blocks", there for the ordinary case of a
			//wall under your own feet not blocking your own sideways view) then discards BOTH of
			//them - so nothing ever gets checked between two adjacent-but-different-level columns,
			//regardless of what's really there. A genuine vertical separator between fromZ and toZ
			//at either endpoint's own column must still block, exactly like the already-handled
			//exact-same-column case right above - this check is a no-op whenever fromZ == toZ (the
			//overwhelmingly common same-level case), since IsVerticalLosBlockedBetween's own
			//zLow==zHigh guard always clears that immediately.
			if (this.IsVerticalLosBlockedBetween(from.X, from.Y, from.Z, to.Z) ||
				this.IsVerticalLosBlockedBetween(to.X, to.Y, from.Z, to.Z)) {
				return false;
			}

			//#1148 - a floor between two levels: under the upper one it is the floor they stand on, up to their own feet;
			//over the upper one's own column their own floor isn't in the way of a look down sideways
			bool fromIsLower = from.Z <= to.Z;
			if (this.IsFloorBetween(from.X, from.Y, from.Z, to.Z, fromIsLower) ||
				this.IsFloorBetween(to.X, to.Y, from.Z, to.Z, !fromIsLower)) {
				return false;
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
		/// blocking between fromZ and toZ, regardless of which one is higher.
		///
		/// #728 - the lower and upper bounds are deliberately NOT symmetric. A real floor/roof's
		/// own Z always coincides exactly with whichever character stands ON TOP of it (standing
		/// on a zero-height Surface tile means Z == the tile's own Z) - so "blocking" must be
		/// INCLUSIVE of zHigh (a floor exactly at the upper character's own feet still separates
		/// them from anyone below - you can't shoot an arrow down through the floor you're
		/// standing on) but stays EXCLUSIVE of zLow (a floor exactly at the LOWER character's own
		/// feet does not block their own view upward - nothing from their own floor extends
		/// further down to get in the way). Real-user-reported bug this fixed: a ranged NPC one
		/// tile over but on a different floor (adjacent column, large Z gap) shot straight through
		/// a real zero-height floor between the two levels - the floor's own Z exactly equalled
		/// the shooter's own standing Z, which the old fully-exclusive-both-ends formula never
		/// treated as "in the way" regardless of the static's real height (an empty-or-not
		/// zero-height band simply never starts strictly below zHigh when it starts exactly AT
		/// zHigh).</summary>
		/// <summary>#1148 - a floor or ceiling (anything you stand on: Surface, or a Roof) in this column between two heights
		/// more than half a character apart. Floors are rarely Noshoot, so IsVerticalLosBlockedBetween missed them, and a
		/// monster on the street saw, shot and cast at a player on the house floor right above it (user's report, a house
		/// in Kralovstvi lesnich elfu, floor 0x2E6C at z 45). `upToHigh` counts a floor exactly at the upper height: in the
		/// lower one's column, or a column both stand in, that is the upper one's floor and it's between them; in the
		/// upper one's own column it is only what they stand on. Sphere's own rule for one column: in sight only within
		/// half a character's height (ccharstatus.cpp CanSeeLOS, dist 0).</summary>
		private bool IsFloorBetween(int x, int y, int fromZ, int toZ, bool upToHigh) {
			int zLow = Math.Min(fromZ, toZ);
			int zHigh = Math.Max(fromZ, toZ);
			if ((zHigh - zLow) <= (PlayerHeight / 2) || !this.IsValidPos(x, y)) {
				return false;
			}
			foreach (AbstractInternalItem s in this.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				if (IsFloorAt(s.Flags, s.Z + s.Height, zLow, zHigh, upToHigh)) {
					return true;
				}
			}
			foreach (AbstractItem item in this.GetItemsInRange(x, y, 0)) {
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);
				if (IsFloorAt(idi.Flags, item.Z + idi.Height, zLow, zHigh, upToHigh)) {
					return true;
				}
			}
			return false;
		}

		//by its top: a stair step's base lies below whoever stands on it
		private static bool IsFloorAt(TileFlag flags, int top, int zLow, int zHigh, bool upToHigh) {
			if (!TileData.HasFlag(flags, TileFlag.Surface) && !TileData.HasFlag(flags, TileFlag.Roof)) {
				return false;
			}
			return (top > zLow) && (upToHigh ? (top <= zHigh) : (top < zHigh));
		}

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
					(s.Z + GetNoshootHeight(s.Flags, s.Height) > zLow) && (GetNoshootBandLow(s.Flags, s.Z, landZ) <= zHigh)) {
					return true;
				}
			}

			foreach (AbstractItem item in this.GetItemsInRange(x, y, 0)) {
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);
				if (TileData.HasFlag(idi.Flags, TileFlag.Noshoot) &&
					(item.Z + GetNoshootHeight(idi.Flags, idi.Height) > zLow) && (GetNoshootBandLow(idi.Flags, item.Z, landZ) <= zHigh)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>#708/#721 - a Noshoot-flagged static's blocking band normally extends down
		/// to the real terrain Z at its own column, and pads a zero-height static up to a full
		/// 1-unit-tall slab (see IsLosBlockedAt's own #692 writeup) - both correct for a WALL (a
		/// vertical obstacle that should block floor-to-ceiling even on a raised platform, and
		/// still block something even with messy zero-height source data), but wrong for anything
		/// that's really a THIN, WALKABLE, HORIZONTAL surface - a roof far overhead, or any one of
		/// countless real floor/platform/pavement tiles (every one of which carries the real UO
		/// tiledata `Surface` flag - "you can stand on this"). #708 first fixed this narrowly for
		/// just `Roof`-flagged statics; #721 then found the SAME mechanism breaking ordinary floor
		/// tiles at ground level (same Z as both characters - the padded-up band always covered
		/// their own stand height) and fixed the height-padding half for `Surface` too, but missed
		/// a second real-map case: a `Surface` floor/platform sitting ABOVE two characters standing
		/// on the raw ground below it still had its band extended all the way DOWN to that ground
		/// (the same "wall on a raised platform" extension #692 added), blocking an ordinary
		/// same-level shot between them. Generalized: `Roof` OR `Surface` (i.e. anything you stand
		/// ON rather than walk INTO) now uses its own literal Z range - no ground-extension, no
		/// height-padding - so this covers every real flooring/platform/pavement tile by its real
		/// tiledata flag instead of needing a per-model fix. Only a true wall/obstacle (Noshoot,
		/// but not Surface/Roof) still gets the old ground-extension + minimum-1-height
		/// treatment.</summary>
		private static bool IsThinWalkableSurface(TileFlag flags) {
			return TileData.HasFlag(flags, TileFlag.Roof) || TileData.HasFlag(flags, TileFlag.Surface);
		}

		private static int GetNoshootBandLow(TileFlag flags, int staticZ, int landZ) {
			if (IsThinWalkableSurface(flags)) {
				return staticZ;
			}
			return Math.Min(staticZ, landZ);
		}

		private static int GetNoshootHeight(TileFlag flags, int height) {
			if (IsThinWalkableSurface(flags)) {
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