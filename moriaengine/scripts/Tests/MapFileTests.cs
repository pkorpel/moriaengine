using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1217 - map sectors are read from memory-mapped .mul files. Every tile id and height and every static of a set of
	/// sectors on facet 0 and facet 2 must equal what this test reads from the same files with plain FileStream reads (the
	/// old loader's way, sharing no code with the new one). Client side: Tests/Protocol/MapFileTests.cs.
	/// </summary>
	public static class MapFileTests {

		[SteamFunction]
		public static void TestMapFiles(Character self) {
			var report = new StringBuilder("MAP_FILES:");
			var wrong = new List<string>();
			int sectors = 0;
			if (!Globals.UseMap) {
				wrong.Add("map use is off");
			} else {
				//spawn, the corners, and some spread over each map (facet 0 = 6144x4096 tiles, facet 2 = 2304x1600)
				var points = new List<int[]> {
					new[] { 0, self.X, self.Y }, new[] { 0, 0, 0 }, new[] { 0, 6143, 4095 }, new[] { 0, 1000, 1000 },
					new[] { 0, 3000, 2000 }, new[] { 0, 5000, 3500 }, new[] { 0, 6143, 0 }, new[] { 0, 0, 4095 },
					new[] { 2, 0, 0 }, new[] { 2, 1150, 800 }, new[] { 2, 2303, 1599 }, new[] { 2, 500, 1200 }
				};
				foreach (int[] p in points) {
					Map map = Map.GetMap((byte) p[0]);
					CheckSector(map, p[1] & ~15, p[2] & ~15, wrong);
					sectors++;
				}
			}
			report.Append(" sectors=").Append(sectors).Append(" same=").Append(wrong.Count == 0);
			if (wrong.Count > 0) {
				report.Append("(").Append(string.Join(";", wrong.GetRange(0, Math.Min(wrong.Count, 10)))).Append(")");
			}
			Console.WriteLine(report.Append(" end MAP_FILES_END").ToString());
		}

		/// <summary>The 16x16 sector at (x0, y0): its 256 tiles and its statics, in the order the files hold them.</summary>
		private static void CheckSector(Map map, int x0, int y0, List<string> wrong) {
			int facet = map.Facet;
			int blocksHigh = Map.GetMapSizeY(facet) >> 3;
			string mul = Globals.MulPath;
			using (var mapFile = new BinaryReader(File.OpenRead(Path.Combine(mul, "map" + facet + ".mul"))))
			using (var idxFile = new BinaryReader(File.OpenRead(Path.Combine(mul, "staidx" + facet + ".mul"))))
			using (var staticsFile = new BinaryReader(File.OpenRead(Path.Combine(mul, "statics" + facet + ".mul")))) {
				for (int x = x0; x < x0 + 16; x++) {
					for (int y = y0; y < y0 + 16; y++) {
						long block = ((long) (x >> 3) * blocksHigh) + (y >> 3);
						mapFile.BaseStream.Seek((block * 196) + 4 + ((((y & 7) * 8) + (x & 7)) * 3), SeekOrigin.Begin);
						int id = mapFile.ReadUInt16();
						int z = mapFile.ReadSByte();
						int gotId = map.GetTileId(x, y), gotZ = map.GetTileZ(x, y);
						if ((gotId != id) || (gotZ != z)) {
							wrong.Add("tile " + facet + ":" + x + "," + y + " " + gotId + "/" + gotZ + " not " + id + "/" + z);
						}
					}
				}

				var expected = new List<string>();
				for (int bx = x0 >> 3; bx < (x0 >> 3) + 2; bx++) {
					for (int by = y0 >> 3; by < (y0 >> 3) + 2; by++) {
						idxFile.BaseStream.Seek((((long) bx * blocksHigh) + by) * 12, SeekOrigin.Begin);
						uint start = idxFile.ReadUInt32();
						uint length = idxFile.ReadUInt32();
						if (start == 0xffffffff) {
							continue;
						}
						staticsFile.BaseStream.Seek(start, SeekOrigin.Begin);
						for (uint i = 0; i < length / 7; i++) {
							int id = staticsFile.ReadUInt16();
							int x = (bx << 3) + staticsFile.ReadByte();
							int y = (by << 3) + staticsFile.ReadByte();
							int z = staticsFile.ReadSByte();
							int color = staticsFile.ReadUInt16();
							expected.Add(id + "@" + x + "," + y + "," + z + "#" + color);
						}
					}
				}
				var got = new List<string>();
				foreach (AbstractInternalItem item in map.GetStaticsAndMultiComponentsInRectangle(new ImmutableRectangle(x0, y0, x0 + 15, y0 + 15))) {
					if (item is StaticItem s) {
						got.Add(s.Id + "@" + s.X + "," + s.Y + "," + s.Z + "#" + s.Color);
					}
				}
				if (!string.Join("|", got).Equals(string.Join("|", expected), StringComparison.Ordinal)) {
					wrong.Add("statics " + facet + ":" + x0 + "," + y0 + " got " + got.Count + " expected " + expected.Count);
				}
			}
		}
	}
}
