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
using System.Diagnostics.CodeAnalysis;

namespace SteamEngine {
	public interface IMovementSettings {
		bool CanCrossLand {
			get;
		}
		bool CanSwim {
			get;
		}
		bool CanCrossLava {
			get;
		}
		bool CanFly {
			get;
		}
		bool IgnoreDoors {
			get;
		}
		int ClimbPower {
			get;
		} //max positive difference in 1 step
	}
}

namespace SteamEngine.Regions {

	//I think the movement implementation is important enough to move it into another 
	//file even though it's still the Map class
	public partial class Map {

		public const int PersonHeight = 16;
		//private const int StepHeight = 2;

		//private static bool m_AlwaysIgnoreDoors;
		//private static bool m_IgnoreMovableImpassables;

		#region CanFit
		public bool CanFit(IPoint3D p, int height, bool checkBlocksFit) {
			return this.CanFit(p.X, p.Y, p.Z, height, checkBlocksFit, true, true);
		}

		public bool CanFit(IPoint3D p, int height, bool checkBlocksFit, bool checkCharacters) {
			return this.CanFit(p.X, p.Y, p.Z, height, checkBlocksFit, checkCharacters, true);
		}

		public bool CanFit(IPoint2D p, int z, int height, bool checkBlocksFit) {
			return this.CanFit(p.X, p.Y, z, height, checkBlocksFit, true, true);
		}

		public bool CanFit(IPoint3D p, int height) {
			return this.CanFit(p.X, p.Y, p.Z, height, false, true, true);
		}

		public bool CanFit(IPoint2D p, int z, int height) {
			return this.CanFit(p.X, p.Y, z, height, false, true, true);
		}

		public bool CanFit(int x, int y, int z, int height) {
			return this.CanFit(x, y, z, height, false, true, true);
		}

		public bool CanFit(int x, int y, int z, int height, bool checksBlocksFit) {
			return this.CanFit(x, y, z, height, checksBlocksFit, true, true);
		}

		public bool CanFit(int x, int y, int z, int height, bool checkBlocksFit, bool checkCharacters) {
			return this.CanFit(x, y, z, height, checkBlocksFit, checkCharacters, true);
		}

		public bool CanFit(int x, int y, int z, int height, bool checkBlocksFit, bool checkCharacters, bool requireSurface) {
			if (!this.IsValidPos(x, y)) {
				return false;
			}

			bool hasSurface = false;

			int tileId;
			int tileZ;
			this.GetTile(x, y, out tileZ, out tileId);

			int lowZ = 0, avgZ = 0, topZ = 0;

			this.GetAverageZ(x, y, ref lowZ, ref avgZ, ref topZ);
			TileFlag landFlags = TileData.GetTileFlags(tileId & 0x3FFF);

			if (((landFlags & TileFlag.Impassable) != 0) && (avgZ > z) && ((z + height) > lowZ)) {
				return false;
			}
			if ((landFlags & TileFlag.Impassable) == 0 && z == avgZ && !TileData.IsIgnoredId(tileId)) {
				hasSurface = true;
			}

			bool surface, impassable;

			foreach (AbstractInternalItem staticTile in this.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				ItemDispidInfo dispidInfo = staticTile.DispidInfo;
				TileFlag staticFlag = dispidInfo.Flags;
				int staticZ = staticTile.Z;
				surface = (staticFlag & TileFlag.Surface) == TileFlag.Surface;
				impassable = (staticFlag & TileFlag.Impassable) == TileFlag.Impassable;

				if ((surface || impassable) && (staticZ + dispidInfo.CalcHeight) > z && (z + height) > staticZ)
					return false;
				if (surface && !impassable && z == (staticZ + dispidInfo.CalcHeight))
					hasSurface = true;
			}

			Sector sector = this.GetSector(x >> sectorFactor, y >> sectorFactor);
			foreach (Thing t in sector.Things) {
				AbstractItem item = t as AbstractItem;
				if (item != null) {
					int itemX = item.X;
					int itemY = item.Y;
					int itemZ = item.Z;
					int itemModel = item.Model;
					if ((itemModel < 0x4000) && (itemX == x) && (itemY == y)) {
						ItemDispidInfo dispidInfo = ItemDispidInfo.GetByModel(itemModel);
						TileFlag staticFlag = dispidInfo.Flags;
						surface = (staticFlag & TileFlag.Surface) == TileFlag.Surface;
						impassable = (staticFlag & TileFlag.Impassable) == TileFlag.Impassable;
						int itemHeight = item.Height;

						if ((surface || impassable || (checkBlocksFit && item.BlocksFit)) && (itemZ + itemHeight) > z && (z + height) > itemZ) {
							return false;
						}
						if (surface && !impassable && /*!item.Flag_Disconnected &&*/ z == (itemZ + itemHeight)) {
							hasSurface = true;
						}
					}
				} else if (checkCharacters) {
					AbstractCharacter ch = (AbstractCharacter) t;
					if (ch.X == x && ch.Y == y && (!ch.Flag_Insubst)) {
						int chZ = ch.Z;
						if ((chZ + 16) > z && (z + height) > chZ) {
							return false;
						}
					}
				}
			}

			return !requireSurface || hasSurface;
		}

		#endregion

		#region CheckMovement

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "5#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "4#"), SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "6#")]
		public bool CheckMovement(IPoint3D point, IMovementSettings settings, Direction d, bool hackMove, out int xForward, out int yForward, out int newZ) {
			int xStart = point.X;
			int yStart = point.Y;
			xForward = xStart;
			yForward = yStart;
			int xRight = xStart, yRight = yStart;
			int xLeft = xStart, yLeft = yStart;

			bool checkDiagonals = ((int) d & 0x1) == 0x1;

			Offset(d, ref xForward, ref yForward);

			if (!this.IsValidPos(xForward, yForward)) {	//off the map?
				newZ = 0;
				return false;
			}

			Offset((d - 1) & Direction.Mask, ref xLeft, ref yLeft);
			Offset((d + 1) & Direction.Mask, ref xRight, ref yRight);

			if (xForward < 0 || yForward < 0 || xForward >= this.sizeX || yForward >= this.sizeY) {
				newZ = 0;
				return false;
			}

			int startZ, startTop;

			TileFlag reqFlags = TileFlag.ImpassableSurface;

			if (settings.CanSwim) {
				reqFlags |= TileFlag.Wet;
			}

			if (checkDiagonals) {
				Sector sectorStart = this.GetSector(xStart >> sectorFactor, yStart >> sectorFactor);
				Sector sectorForward = this.GetSector(xForward >> sectorFactor, yForward >> sectorFactor);
				Sector sectorLeft = this.GetSector(xLeft >> sectorFactor, yLeft >> sectorFactor);
				Sector sectorRight = this.GetSector(xRight >> sectorFactor, yRight >> sectorFactor);

				//List<Sector> sectorsPool = sectorsPool;

				sectorsPool.Add(sectorStart);

				if (!sectorsPool.Contains(sectorForward)) {
					sectorsPool.Add(sectorForward);
				}

				if (!sectorsPool.Contains(sectorLeft)) {
					sectorsPool.Add(sectorLeft);
				}

				if (!sectorsPool.Contains(sectorRight)) {
					sectorsPool.Add(sectorRight);
				}

				for (int i = 0; i < sectorsPool.Count; ++i) {
					Sector sector = sectorsPool[i];

					foreach (Thing t in sector.Things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						int model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.GetByModel(model);
						if ((idi.Flags & reqFlags) == 0)
							continue;
						int itemX = item.X;
						int itemY = item.Y;

						if (sector == sectorStart && (itemX == xStart && itemY == yStart) && model < 0x4000) {
							itemsPoolStart.Add(item);
						} else if (sector == sectorForward && (itemX == xForward && itemY == yForward) && model < 0x4000) {
							itemsPoolForward.Add(item);
						} else if (sector == sectorLeft && (itemX == xLeft && itemY == yLeft) && model < 0x4000) {
							itemsPoolLeft.Add(item);
						} else if (sector == sectorRight && (itemX == xRight && itemY == yRight) && model < 0x4000) {
							itemsPoolRight.Add(item);
						}
					}
				}

				if (sectorsPool.Count > 0)
					sectorsPool.Clear();
			} else {
				Sector sectorStart = this.GetSector(xStart >> sectorFactor, yStart >> sectorFactor);
				Sector sectorForward = this.GetSector(xForward >> sectorFactor, yForward >> sectorFactor);

				if (sectorStart == sectorForward) {
					foreach (Thing t in sectorStart.Things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						int model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.GetByModel(model);
						if ((idi.Flags & reqFlags) == 0) {
							continue;
						}
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xStart && itemY == yStart && model < 0x4000) {
							itemsPoolStart.Add(item);
						} else if (itemX == xForward && itemY == yForward && model < 0x4000) {
							itemsPoolForward.Add(item);
						}
					}
				} else {
					foreach (Thing t in sectorForward.Things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						int model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.GetByModel(model);
						if ((idi.Flags & reqFlags) == 0) {
							continue;
						}
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xForward && itemY == yForward && model < 0x4000) {
							itemsPoolForward.Add(item);
						}
					}

					foreach (Thing t in sectorStart.Things) {
						AbstractItem item = t as AbstractItem;
						if (item == null) {
							continue;
						}

						int model = item.Model;
						ItemDispidInfo idi = ItemDispidInfo.GetByModel(model);
						if ((idi.Flags & reqFlags) == 0) {
							continue;
						}
						int itemX = item.X;
						int itemY = item.Y;

						if (itemX == xStart && itemY == yStart && model < 0x4000) {
							itemsPoolStart.Add(item);
						}
					}
				}
			}

			this.GetStartZ(settings, point, itemsPoolStart, out startZ, out startTop);

			bool moveIsOk = this.Check(point, settings, itemsPoolForward, xForward, yForward, startTop, startZ, out newZ);
			if (moveIsOk && checkDiagonals) {
				int hold;
				//ani monstra ani hraci nemuzou projit sikmo pres roh, natoz pres diagonalni zed
				if (!this.Check(point, settings, itemsPoolLeft, xLeft, yLeft, startTop, startZ, out hold) || !this.Check(point, settings, itemsPoolRight, xRight, yRight, startTop, startZ, out hold)) {
					moveIsOk = false;
				}
			}

			if (!moveIsOk) {
				if (hackMove) {
					moveIsOk = true;
				}
			}

			itemsPoolStart.Clear();
			itemsPoolForward.Clear();
			if (checkDiagonals) {
				itemsPoolLeft.Clear();
				itemsPoolRight.Clear();
			}

			if (!moveIsOk) {
				newZ = startZ;
			}

			return moveIsOk;
		}

		private static bool IsOk(bool ignoreDoors, int ourZ, int ourTop, List<AbstractInternalItem> tiles, List<AbstractItem> items) {
			for (int i = 0, n = tiles.Count; i < n; i++) {
				AbstractInternalItem check = tiles[i];
				ItemDispidInfo itemData = check.DispidInfo;

				if ((itemData.Flags & TileFlag.ImpassableSurface) != 0) {// Impassable || Surface
					int checkZ = check.Z;
					int checkTop = checkZ + itemData.CalcHeight;

					if (checkTop > ourZ && ourTop > checkZ)
						return false;
				}
			}

			for (int i = 0; i < items.Count; ++i) {
				AbstractItem item = items[i];

				int model = item.Model;
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(model);
				TileFlag flags = idi.Flags;

				if ((flags & TileFlag.ImpassableSurface) != 0) {// Impassable || Surface
					if (ignoreDoors && ((flags & TileFlag.Door) != 0
							|| model == 0x692 || model == 0x846 || model == 0x873 || (model >= 0x6F5 && model <= 0x6F6)))
						//^^^^ ve standartnich tiledata.mul nemaj tyhle modely flag_door i kdyz sou to dvere
						continue;

					int checkZ = item.Z;
					int checkTop = checkZ + idi.CalcHeight;

					if (checkTop > ourZ && ourTop > checkZ)
						return false;
				}
			}

			return true;
		}


		private static List<AbstractItem> itemsPoolStart = new List<AbstractItem>();
		private static List<AbstractItem> itemsPoolForward = new List<AbstractItem>();
		private static List<AbstractItem> itemsPoolLeft = new List<AbstractItem>();
		private static List<AbstractItem> itemsPoolRight = new List<AbstractItem>();
		private static List<Sector> sectorsPool = new List<Sector>();
		private static List<AbstractInternalItem> staticsPool = new List<AbstractInternalItem>();

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private bool Check(IPoint3D point, IMovementSettings settings, List<AbstractItem> items, int x, int y, int startTop, int startZ, out int newZ) {
			newZ = 0;

			int landTile = this.GetTileId(x, y);
			int landZ = 0, landCenter = 0, landTop = 0;
			TileFlag tileFlags = TileData.GetTileFlags(landTile);

			bool canSwim = settings.CanSwim;
			bool canFly = settings.CanFly;
			bool canCrossLand = settings.CanCrossLand;
			bool canCrossLava = settings.CanCrossLava;
			bool ignoreDoors = settings.IgnoreDoors;

			bool landBlocks = TileData.HasFlag(tileFlags, TileFlag.Impassable);
			bool considerLand = !TileData.IsIgnoredId(landTile);

			int pointZ = point.Z;

			//we can't go over land or it's no land...let's try swimming, lavawalking or flying
			if ((landBlocks) || (!canCrossLand)) {
				landBlocks = true; //land blocks us if we can't cross land
				bool isWater = ((tileFlags & TileFlag.Wet) == TileFlag.Wet);
				bool isLava = t_lava.IsTypeOfMapTile(landTile);
				if ((canSwim && isWater) ||
						(canCrossLava && isLava) ||
						(canFly)) {
					landBlocks = false;
				}
			}



			this.GetAverageZ(x, y, ref landZ, ref landCenter, ref landTop);

			bool moveIsOk = false;

			int stepHeight = settings.ClimbPower;
			int stepTop = startTop + stepHeight;
			int checkTop = startZ + PersonHeight;

			staticsPool.Clear();
			foreach (AbstractInternalItem staticItem in this.GetStaticsAndMultiComponentsOnCoords(x, y)) {
				staticsPool.Add(staticItem);
			}

			for (int i = 0, n = staticsPool.Count; i < n; i++) {
				AbstractInternalItem staticItem = staticsPool[i];

				ItemDispidInfo idi = staticItem.DispidInfo;
				TileFlag flags = idi.Flags;

				bool staticIsWater = ((flags & TileFlag.Wet) == TileFlag.Wet);
				bool staticIsLava = t_lava.IsTypeOfMapTile(idi.Id);

				if ((flags & TileFlag.ImpassableSurface) == TileFlag.Surface || // Surface && !Impassable
						(canSwim && staticIsWater) || //je to voda a my umime plavat
						(canCrossLava && staticIsLava) || //je to lava a nam nevadi
						(canFly)) {//umime litat a tak nam nevadi nic

					if (!canFly && !canCrossLand && !staticIsWater && !staticIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					int itemZ = staticItem.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + idi.CalcHeight;
					//int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if (moveIsOk) {
						int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

						if (cmp > 0 || (cmp == 0 && ourZ > newZ))
							continue;
					}

					if (ourZ + PersonHeight > testTop)
						testTop = ourZ + PersonHeight;

					if ((flags & TileFlag.Bridge) == 0)
						itemTop += idi.Height;

					if (stepTop >= itemTop) {
						int landCheck = itemZ;

						if (idi.Height >= stepHeight)
							landCheck += stepHeight;
						else
							landCheck += idi.Height;

						if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
							continue;

						if (IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}

			for (int i = 0; i < items.Count; ++i) {
				AbstractItem item = items[i];
				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);
				TileFlag flags = idi.Flags;

				bool itemIsWater = ((flags & TileFlag.Wet) == TileFlag.Wet);
				bool itemIsLava = t_lava.IsTypeOfMapTile(idi.Id);

				if (/*item.Flag_NeverMovable && */((flags & TileFlag.ImpassableSurface) == TileFlag.Surface || // Surface && !Impassable && !Movable
						(canSwim && itemIsWater) || //je to voda a my umime plavat
						(canCrossLava && itemIsLava) || //je to lava a nam nevadi
						(canFly))) {//umime litat a tak nam nevadi nic

					if (!canFly && !canCrossLand && !itemIsWater && !itemIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					int itemZ = item.Z;
					int itemTop = itemZ;
					int ourZ = itemZ + idi.CalcHeight;
					//int ourTop = ourZ + PersonHeight;
					int testTop = checkTop;

					if (moveIsOk) {
						int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

						if (cmp > 0 || (cmp == 0 && ourZ > newZ))
							continue;
					}

					if (ourZ + PersonHeight > testTop)
						testTop = ourZ + PersonHeight;

					if ((flags & TileFlag.Bridge) == 0)
						itemTop += idi.Height;

					if (stepTop >= itemTop) {
						int landCheck = itemZ;

						if (idi.Height >= stepHeight)
							landCheck += stepHeight;
						else
							landCheck += idi.Height;

						if (considerLand && landCheck < landCenter && landCenter > ourZ && testTop > landZ)
							continue;

						if (IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
							newZ = ourZ;
							moveIsOk = true;
						}
					}
				}
			}

			if (considerLand && !landBlocks && stepTop >= landZ) {
				int ourZ = landCenter;
				//int ourTop = ourZ + PersonHeight;
				int testTop = checkTop;

				if (ourZ + PersonHeight > testTop)
					testTop = ourZ + PersonHeight;

				bool shouldCheck = true;

				if (moveIsOk) {
					int cmp = Math.Abs(ourZ - pointZ) - Math.Abs(newZ - pointZ);

					if (cmp > 0 || (cmp == 0 && ourZ > newZ))
						shouldCheck = false;
				}

				if (shouldCheck && IsOk(ignoreDoors, ourZ, testTop, staticsPool, items)) {
					newZ = ourZ;
					moveIsOk = true;
				}
			}

			return moveIsOk;
		}

		[SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private void GetStartZ(IMovementSettings settings, IPoint3D point, List<AbstractItem> itemList, out int zLow, out int zTop) {
			int xCheck = point.X, yCheck = point.Y;

			int landTile = this.GetTileId(xCheck, yCheck);
			int landZ = 0, landCenter = 0, landTop = 0;
			TileFlag tileFlags = TileData.GetTileFlags(landTile);

			bool canSwim = settings.CanSwim;
			bool canFly = settings.CanFly;
			bool canCrossLand = settings.CanCrossLand;
			bool canCrossLava = settings.CanCrossLava;

			bool landBlocks = TileData.HasFlag(tileFlags, TileFlag.Impassable);

			//we can't go over land or it's no land...let's try swimming, lavawalking or flying
			if ((landBlocks) || (!canCrossLand)) {
				landBlocks = true; //land blocks us if we can't cross land
				bool isWater = ((tileFlags & TileFlag.Wet) == TileFlag.Wet);
				bool isLava = t_lava.IsTypeOfMapTile(landTile);
				if ((canSwim && isWater) ||
						(canCrossLava && isLava) ||
						(canFly)) {
					landBlocks = false;
				}
			}

			this.GetAverageZ(xCheck, yCheck, ref landZ, ref landCenter, ref landTop);

			bool considerLand = !TileData.IsIgnoredId(landTile);

			int zCenter = zLow = zTop = 0;
			bool isSet = false;

			int pointZ = point.Z;

			if (considerLand && !landBlocks && pointZ >= landCenter) {
				zLow = landZ;
				zCenter = landCenter;

				if (landTop > zTop)
					zTop = landTop;

				isSet = true;
			}

			foreach (AbstractInternalItem staticItem in this.GetStaticsAndMultiComponentsOnCoords(xCheck, yCheck)) {

				ItemDispidInfo idi = staticItem.DispidInfo;

				int calcTop = (staticItem.Z + idi.CalcHeight);

				bool staticIsWater = ((idi.Flags & TileFlag.Wet) == TileFlag.Wet);
				bool staticIsLava = t_lava.IsTypeOfMapTile(idi.Id);

				if ((!isSet || calcTop >= zCenter) &&
						((idi.Flags & TileFlag.Surface) != 0 || //je to stul (eh?)
							(canSwim && staticIsWater) || //je to voda a my umime plavat
							(canCrossLava && staticIsLava) || //je to lava a nam nevadi
							(canFly) //umime litat a tak nam nevadi nic
							)
						&& pointZ >= calcTop) {

					if (!canFly && !canCrossLand && !staticIsWater && !staticIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					zLow = staticItem.Z;
					zCenter = calcTop;

					int top = staticItem.Z + idi.Height;

					if (!isSet || top > zTop)
						zTop = top;

					isSet = true;
				}
			}

			for (int i = 0; i < itemList.Count; ++i) {
				AbstractItem item = itemList[i];

				ItemDispidInfo idi = ItemDispidInfo.GetByModel(item.Model);

				bool itemIsWater = ((idi.Flags & TileFlag.Wet) == TileFlag.Wet);
				bool itemIsLava = t_lava.IsTypeOfMapTile(idi.Id);

				int calcTop = item.Z + idi.CalcHeight;

				if ((!isSet || calcTop >= zCenter) &&
						((idi.Flags & TileFlag.Surface) != 0 || //je to stul (eh?)
							(canSwim && itemIsWater) || //je to voda a my umime plavat
							(canCrossLava && itemIsLava) || //je to lava a nam nevadi
							(canFly) //umime litat a tak nam nevadi nic
							)
						&& pointZ >= calcTop) {

					if (!canFly && !canCrossLand && !itemIsWater && !itemIsLava)
						continue;//neumime chodit/litat a neni to voda/lava (he?)

					zLow = item.Z;
					zCenter = calcTop;

					int top = item.Z + idi.Height;

					if (!isSet || top > zTop)
						zTop = top;

					isSet = true;
				}
			}

			if (!isSet)
				zLow = zTop = pointZ;
			else if (pointZ > zTop)
				zTop = pointZ;
		}

		#endregion CheckMovement

		public static void Offset(Direction d, ref int x, ref int y) {
			switch (d) {
				case Direction.North: --y; break;
				case Direction.South: ++y; break;
				case Direction.West: --x; break;
				case Direction.East: ++x; break;
				case Direction.NorthEast: ++x; --y; break;
				case Direction.SouthWest: --x; ++y; break;
				case Direction.SouthEast: ++x; ++y; break;
				case Direction.NorthWest: --x; --y; break;
			}
		}

		private void GetAverageZ(int x, int y, ref int z, ref int avg, ref int top) {
			int zTop = this.GetTileZ(x, y);
			int zLeft = this.GetTileZ(x, y + 1);
			int zRight = this.GetTileZ(x + 1, y);
			int zBottom = this.GetTileZ(x + 1, y + 1);
			z = zTop;
			if (zLeft < z) {
				z = zLeft;
			}
			if (zRight < z) {
				z = zRight;
			}
			if (zBottom < z) {
				z = zBottom;
			}
			top = zTop;
			if (zLeft > top) {
				top = zLeft;
			}
			if (zRight > top) {
				top = zRight;
			}
			if (zBottom > top) {
				top = zBottom;
			}
			if (Math.Abs(zTop - zBottom) > Math.Abs(zLeft - zRight)) {
				avg = FloorAverage(zLeft, zRight);
			} else {
				avg = FloorAverage(zTop, zBottom);
			}
		}

		private static int FloorAverage(int a, int b) {
			int v = a + b;
			if (v < 0) {
				v--;
			}
			return (v / 2);
		}

		//TODO needs work
		[SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters"), SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public int GetFixedZ(IPoint3D point) {
			//int oldZ = point.Z;
			int x = point.X;
			int y = point.Y;
			//if (this.IsValidPos(x, y)) {
				return this.GetTileZ(x, y);
			//}
			//return oldZ;
		}
	}
}