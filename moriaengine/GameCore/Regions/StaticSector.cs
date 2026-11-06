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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using SteamEngine.Common;

namespace SteamEngine.Regions {

	internal class StaticSector {
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private ushort[,] tileMatrix;
		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Member")]
		private sbyte[,] zMatrix;
		internal StaticItem[] statics;

		private static List<StaticSector[,]> staticMaps = new List<StaticSector[,]>(1);
		//by facet: map0.mul is entry 0, map2.mul entry 2

		//Map.sectorWidth>>3 is the number of MUL sectors per SE sector. (Map.sectorFactor-3 would NOT be.)
		private static StaticSector emptySector = new StaticSector(new ushort[0, 0], new sbyte[0, 0], new StaticItem[0]);

		private StaticSector(ushort[,] tile, sbyte[,] z, StaticItem[] statics) {
			this.tileMatrix = tile;
			this.zMatrix = z;
			this.statics = statics;
		}

		//internal static StaticSector GetStaticSectorFromRealCoords(int x, int y, int m) {
		//    return GetStaticSector((x >> Map.sectorFactor), (y >> Map.sectorFactor), m);
		//}

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body"), SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
		internal static StaticSector GetStaticSector(int sx, int sy, int m) {
			int facet = Map.GetMap(m).Facet;

			bool exists = (staticMaps.Count > facet);
			StaticSector[,] staticMap = null;
			if (exists) {
				staticMap = staticMaps[facet];
				exists = (staticMap != null);
			}

			if (!exists) {
				while (staticMaps.Count <= facet) {
					staticMaps.Add(null);
				}
				staticMap = new StaticSector[Map.GetMapNumXSectors(facet), Map.GetMapNumYSectors(facet)];
				staticMaps[facet] = staticMap;
			}

			//Console.WriteLine("Getting StaticSector "+sx+","+sy);

			StaticSector sector = staticMap[sx, sy];
			if (sector == null) {
				if (Globals.useMap) {
					try {
						Logger.WriteDebug("Loading map sector " + sx + "," + sy);

						int numMulSectors = Map.sectorWidth >> 3;

						//Console.WriteLine("Map.sectorWidth="+Map.sectorWidth+" and numMulSectors="+numMulSectors);
						ushort mulsx = (ushort) ((sx << Map.sectorFactor) >> 3);
						ushort mulsy = (ushort) ((sy << Map.sectorFactor) >> 3);
						Logger.WriteDebug("Load sector sx,sy=" + sx + "," + sy + " mulsx,mulsy=" + mulsx + "," + mulsy);
						uint[,] versions;
						StaticItem[] statics = LoadStaticsSector(mulsx, mulsy, (ushort) (sx << Map.sectorFactor),
							(ushort) (sy << Map.sectorFactor), numMulSectors, facet, out versions);

						ushort[,] tile; 
						sbyte[,] z;
						LoadMapSector(mulsx, mulsy, numMulSectors, facet, out tile, out z);
						sector = new StaticSector(tile, z, statics);
					} catch (Exception e) {
						Logger.WriteCritical("Exception while loading map sector " + sx + "," + sy, e + ". Disabling map use.");
						Globals.useMap = false;
					}
					staticMap[sx, sy] = sector;
				}
			}
			if (sector == null) {
				sector = emptySector;
			}
			return sector;
		}

		/// <summary>#1217 - one .mul file, memory-mapped once and kept for the life of the process. Reads copy by position,
		/// so they keep no state and any thread can make them; there's no file open or seek when a sector loads.</summary>
		private sealed class MappedMul {
			private readonly MemoryMappedViewAccessor view; //null for an empty file
			private readonly long length; //the file's: the view's capacity is rounded up to a page
			private readonly string name;

			internal MappedMul(string path) {
				this.name = Path.GetFileName(path);
				this.length = new FileInfo(path).Length;
				if (this.length > 0) {
					using (MemoryMappedFile file = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read)) {
						this.view = file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read); //the view keeps the mapping alive
					}
				}
			}

			/// <summary>Copies count bytes from pos into buffer; past the end of the file throws, as BinaryReader's end of stream did.</summary>
			internal void Read(long pos, byte[] buffer, int count) {
				if ((pos < 0) || (pos + count > this.length)) {
					throw new EndOfStreamException("Reading " + count + " bytes at " + pos + " past the end of " + this.name + " (" + this.length + " bytes)");
				}
				if (count > 0) {
					this.view.ReadArray(pos, buffer, 0, count);
				}
			}
		}

		/// <summary>A facet's map, staidx and statics files.</summary>
		private sealed class FacetMuls {
			internal readonly MappedMul map, staidx, statics;

			internal FacetMuls(MappedMul map, MappedMul staidx, MappedMul statics) {
				this.map = map;
				this.staidx = staidx;
				this.statics = statics;
			}
		}

		private static readonly List<FacetMuls> facetMuls = new List<FacetMuls>(1); //by facet, as staticMaps
		private static readonly object facetMulsLock = new object();

		/// <summary>Opens the facet's files the first time; a missing one throws, with the messages loading always had.</summary>
		private static FacetMuls GetFacetMuls(int facet) {
			lock (facetMulsLock) {
				while (facetMuls.Count <= facet) {
					facetMuls.Add(null);
				}
				FacetMuls muls = facetMuls[facet];
				if (muls == null) {
					string mapPath = Path.Combine(Globals.MulPath, "map" + facet + ".mul");
					string staidxPath = Path.Combine(Globals.MulPath, "staidx" + facet + ".mul");
					string staticsPath = Path.Combine(Globals.MulPath, "statics" + facet + ".mul");
					if (!File.Exists(staidxPath) || !File.Exists(staticsPath)) {
						throw new SEException("Unable to locate statics" + facet + ".mul or staidx" + facet + ".mul. Using map disabled.");
					}
					if (!File.Exists(mapPath)) {
						throw new SEException("Unable to locate map file.");
					}
					muls = new FacetMuls(new MappedMul(mapPath), new MappedMul(staidxPath), new MappedMul(staticsPath));
					facetMuls[facet] = muls;
				}
				return muls;
			}
		}

		private const int MapBlockBytes = 196; //a 4 byte header, then 8x8 cells of tile id (2) and z (1)
		private const int MapCellsBytes = 192;
		private const int StaidxEntryBytes = 12; //start, length, extra
		private const int StaticBytes = 7; //id (2), x (1), y (1), z (1), colour (2)

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body")]
		private static void LoadMapSector(int sx, int sy, int numMulSectors, int facet, out ushort[,] tile, out sbyte[,] z) {
			MappedMul map = GetFacetMuls(facet).map;
			int numTiles = numMulSectors << 3;
			tile = new ushort[numTiles, numTiles];
			z = new sbyte[numTiles, numTiles];

			byte[] cells = ArrayPool<byte>.Shared.Rent(MapCellsBytes);
			try {
				for (int xblock = 0; xblock < numMulSectors; xblock++) {
					long partialFilePos = ((xblock + sx) * Map.GetMulNumYSectors(facet));
					int xblockamt = xblock << 3;
					for (int yblock = 0; yblock < numMulSectors; yblock++) {
						int yblockamt = yblock << 3;
						long blockIndex = partialFilePos + yblock + sy;
						map.Read((blockIndex * MapBlockBytes) + 4, cells, MapCellsBytes); //the 4 skips the block's header
						int at = 0;
						for (int y = 0; y < 8; y++) {
							for (int x = 0; x < 8; x++) {
								tile[xblockamt + x, yblockamt + y] = BinaryPrimitives.ReadUInt16LittleEndian(cells.AsSpan(at, 2));
								z[xblockamt + x, yblockamt + y] = (sbyte) cells[at + 2];
								at += 3;
							}
						}
					}
				}
			} finally {
				ArrayPool<byte>.Shared.Return(cells);
			}
		}

		[SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body")]
		private static StaticItem[] LoadStaticsSector(int mulsX, int mulsY, int sx, int sy, int numMulSectors, int facet, out uint[,] versions) {
			versions = new uint[numMulSectors, numMulSectors];
			FacetMuls muls = GetFacetMuls(facet);
			List<StaticItem> statics = new List<StaticItem>();

			byte[] entry = ArrayPool<byte>.Shared.Rent(StaidxEntryBytes);
			byte[] block = null;
			try {
				for (int xblock = 0; xblock < numMulSectors; xblock++) {
					long partialFilePos = ((xblock + mulsX) * Map.GetMulNumYSectors(facet));
					int xblockamt = xblock << 3;
					for (int yblock = 0; yblock < numMulSectors; yblock++) {
						int yblockamt = yblock << 3;
						long blockIndex = partialFilePos + yblock + mulsY;
						muls.staidx.Read(blockIndex * StaidxEntryBytes, entry, StaidxEntryBytes);
						uint blockStart = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(0, 4));
						uint blockLen = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(4, 4));
						versions[xblock, yblock] = BinaryPrimitives.ReadUInt32LittleEndian(entry.AsSpan(8, 4));
						if (blockStart != 0xffffffff) {
							int blockNumStatics = (int) (blockLen / StaticBytes);
							int bytes = blockNumStatics * StaticBytes;
							if ((block == null) || (block.Length < bytes)) {
								if (block != null) {
									ArrayPool<byte>.Shared.Return(block);
								}
								block = ArrayPool<byte>.Shared.Rent(Math.Max(bytes, 1));
							}
							muls.statics.Read(blockStart, block, bytes);
							for (int a = 0, at = 0; a < blockNumStatics; a++, at += StaticBytes) {
								ushort tileID = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(at, 2));
								byte relX = block[at + 2];
								byte relY = block[at + 3];
								sbyte staticZ = (sbyte) block[at + 4];
								ushort color = BinaryPrimitives.ReadUInt16LittleEndian(block.AsSpan(at + 5, 2));
								ushort x = (ushort) (xblockamt + relX + sx);
								ushort y = (ushort) (yblockamt + relY + sy);
								statics.Add(new StaticItem(tileID, x, y, staticZ, 255, color));
								//the diff files would have statics which aren't on mapplane 255. Etc.
							}
						}
					}
				}
			} finally {
				ArrayPool<byte>.Shared.Return(entry);
				if (block != null) {
					ArrayPool<byte>.Shared.Return(block);
				}
			}
			return statics.ToArray();
		}

		internal bool HasStaticId(int x, int y, int staticId) {
			for (int a = 0; a < this.statics.Length; a++) {
				if (this.statics[a] != null) {
					StaticItem sta = this.statics[a];
					if (x == sta.X && y == sta.Y) {
						if (sta.Id == staticId) {
							return true;
						}
					}
				}
			}
			return false;
		}

		internal StaticItem GetStatic(int x, int y, int z, int tileID) {
			foreach (StaticItem stat in this.statics) {
				//Logger.WriteDebug("Compare static ("+stat.X+","+stat.Y+","+stat.Z+","+stat.M+":"+stat.id+" ("+stat.Name+")) to ("+x+","+y+","+z+":"+tileID+")");
				if (stat.X == x && stat.Y == y && stat.Z == z && stat.Id == tileID) {
					return stat;
				}
			}
			return null;
		}

		//internal void GetRMS(int x, int y, out int rmsx, out int rmsy) {
		//    //basex/basey are the base x/y coordinates of this sector.
		//    int basex = x & Map.sectorAnd;
		//    int basey = y & Map.sectorAnd;

		//    //mbasex/mbasey are the base x/y coordinates of this MUL sector.
		//    int mbasex = x & Map.mulSectorAnd;
		//    int mbasey = y & Map.mulSectorAnd;

		//    //diffbasex/y are the difference between the base x/y coords,
		//    //which we need to determine the relative map sector inside this SE sector.
		//    int diffbasex = mbasex - basex;
		//    int diffbasey = mbasey - basey;

		//    //rmsx/rmsy are the coordinates to Richard M. Stallman's homepage.
		//    //Actually, they specify what MUL map sector inside this SE sector we're working on,
		//    //their value being relative, from 0 to the maximum number of MUL sectors per SE sector.
		//    rmsx = diffbasex >> 3;
		//    rmsy = diffbasey >> 3;
		//}

		internal int GetTileId(int relX, int relY)
		{
			if (Globals.useMap) {
				return this.tileMatrix[relX, relY];
			}
			return 0;
		}

		internal int GetTileZ(int relX, int relY)
		{
			if (Globals.useMap) {
				return this.zMatrix[relX, relY];
			}
			return 0;
		}

		//static ArrayList numStaticsPerSector = new ArrayList();
		//static int maxStaticsPerSector = 0;
		//internal void CollectStatistics(Static[] statics) {
		//	numStaticsPerSector.Add(statics.Length);
		//	if (statics.Length>maxStaticsPerSector) {
		//		maxStaticsPerSector=statics.Length;
		//	}
		//	//determine number of sectors with 0 statics, with 1, with more than 1.
		//	//determine average # of statics per sector
		//
		//}
		//
		//static internal void AnalyzeStatistics() {
		//	Console.WriteLine("maxStaticsPerSector: "+maxStaticsPerSector);
		//	int change=maxStaticsPerSector/100;
		//	// >>12 = /4096
		//	Console.WriteLine("# sectors with 0 statics: "+CountNumSectorsWithStaticsBetween(-1,0));
		//	int ns=0;
		//	int lastns=-change;
		//
		//	while (ns<=maxStaticsPerSector) {
		//		lastns=ns;
		//		ns+=change;
		//		Console.WriteLine("# sectors with >"+lastns+" and <="+ns+" statics: "+CountNumSectorsWithStaticsBetween(lastns, ns));
		//	}
		//}
		//
		//static private int CountNumSectorsWithStaticsBetween(int min, int max) {
		//	int total=0;
		//	for (int ss=0; ss<numStaticsPerSector.Count; ss++) {
		//		int i=(int) numStaticsPerSector[ss];
		//		if (i>min && i<=max) total++;
		//	}
		//	return total;
		//}

	}
}
