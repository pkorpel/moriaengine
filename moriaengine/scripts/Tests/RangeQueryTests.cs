using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SteamEngine.Communication.TCP;
using SteamEngine.Networking;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1216 - the map's range queries are structs. Every one must find exactly what a brute-force look over Thing.AllThings
	/// finds, through both its ...InRange and ...InRectangle overload, and a foreach over it must allocate nothing.
	/// Client side: Tests/Protocol/RangeQueryTests.cs.
	/// </summary>
	public static class RangeQueryTests {
		private const int Calls = 50;

		[SteamFunction]
		public static void TestRangeQueries(Character self) {
			Map map = self.GetMap();
			byte m = self.M;
			Point4D home = new Point4D(self.X, self.Y, self.Z, self.M);
			//a sector corner two sectors east of the player: the things around it lie in four sectors
			int cx = (self.X + 40) & ~15;
			int cy = self.Y & ~15;
			var made = new List<Thing>();
			var report = new StringBuilder("RANGE_QUERIES:");
			try {
				NPCDef rabbit = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
				ItemDef dagger = (ItemDef) ThingDef.GetByDefname("i_dagger");
				int[] offsets = { -7, -3, -1, 0, 2, 6 };
				foreach (int dx in offsets) {
					foreach (int dy in offsets) {
						int x = cx + dx, y = cy + dy;
						if (((dx + dy) & 3) == 0) {
							NPC npc = (NPC) rabbit.Create(x, y, map.GetTileZ(x, y), m);
							NPCAiTimer.GetFor(npc).NeverSleeps = true;
							made.Add(npc);
						} else {
							made.Add(dagger.Create(x, y, map.GetTileZ(x, y), m));
						}
					}
				}
				NPC gone = (NPC) rabbit.Create(cx + 1, cy - 2, map.GetTileZ(cx + 1, cy - 2), m);
				made.Add(gone);
				gone.Disconnect();
				self.P(cx - 2, cy + 1, map.GetTileZ(cx - 2, cy + 1), m);

				//around the corner, one tile, wider than a sector, off-centre, and over the map's edge
				var areas = new List<int[]> {
					new[] { cx, cy, 5 }, new[] { cx + 2, cy, 0 }, new[] { cx - 1, cy + 1, 20 }, new[] { 3, 3, 8 }
				};
				var mismatches = new List<string>();
				foreach (int[] a in areas) {
					CompareAll(map, m, new ImmutableRectangle(a[0], a[1], a[2]), a[0], a[1], a[2], mismatches);
				}
				CompareRectangle(map, m, new ImmutableRectangle(cx - 10, cy - 2, cx + 3, cy + 9), mismatches);
				int hx = home.X, hy = home.Y; //statics: the spawn point has one just east of it (#788)
				CompareStatics(map, hx, hy, 6, mismatches);
				//order too, against the sectors' own lists read by reflection (an oracle that shares no code with the queries)
				CompareInOrder("thingsOrder", map.GetThingsInRange(cx, cy, 20), SectorWalk(map, cx - 20, cy - 20, cx + 20, cy + 20, "Chars", "Items"), mismatches);
				CompareInOrder("charsOrder", map.GetCharsInRange(cx, cy, 20), SectorWalk(map, cx - 20, cy - 20, cx + 20, cy + 20, "Chars"), mismatches);
				CompareInOrder("staticsOrder", map.GetStaticsAndMultiComponentsInRange(hx, hy, 18), SectorWalk(map, hx - 18, hy - 18, hx + 18, hy + 18, "Statics", "MultiComponents"), mismatches);
				CompareInOrder("multisOrder", map.GetMultiComponentsInRange(hx, hy, 18), SectorWalk(map, hx - 18, hy - 18, hx + 18, hy + 18, "MultiComponents"), mismatches);
				//LScript reaches a query by reflection and walks it as an IEnumerable (OpNode_Foreach): the boxed struct
				int inCSharp = 0;
				foreach (Thing t in map.GetThingsInRange(cx, cy, 5)) {
					inCSharp++;
				}
				object inLScript = ScriptHolder.GetFunction("f_rangequery_lscript_count").Run(self, cx, cy); //RangeQueryTests.scp
				if ((inCSharp == 0) || (Convert.ToInt32(inLScript) != inCSharp)) {
					mismatches.Add("lscript found " + inLScript + " expected " + inCSharp);
				}
				report.Append(" match=").Append(mismatches.Count == 0);
				if (mismatches.Count > 0) {
					report.Append("(").Append(string.Join(";", mismatches)).Append(")");
				}

				var allocating = new List<string>();
				Thing seen = made[0];
				ImmutableRectangle rect14 = new ImmutableRectangle(cx, cy, 14); //made once: a rectangle is an allocation of its own
				var queries = new List<KeyValuePair<string, Func<int>>> {
					Query("chars", () => { int c = 0; foreach (AbstractCharacter t in map.GetCharsInRange(cx, cy, 14)) { c++; } return c; }),
					Query("things", () => { int c = 0; foreach (Thing t in map.GetThingsInRange(cx, cy, 18)) { c++; } return c; }),
					Query("items", () => { int c = 0; foreach (AbstractItem t in map.GetItemsInRange(cx, cy, 18)) { c++; } return c; }),
					Query("npcs", () => { int c = 0; foreach (AbstractCharacter t in map.GetNPCsInRectangle(rect14)) { c++; } return c; }),
					Query("npcsLoaded", () => { int c = 0; foreach (AbstractCharacter t in map.GetNPCsInLoadedSectors(rect14)) { c++; } return c; }),
					Query("players", () => { int c = 0; foreach (AbstractCharacter t in map.GetPlayersInRange(cx, cy, 18)) { c++; } return c; }),
					Query("disconnects", () => { int c = 0; foreach (Thing t in map.GetDisconnectsInRange(cx, cy, 18)) { c++; } return c; }),
					Query("connections", () => { int c = 0; foreach (TcpConnection<GameState> t in map.GetConnectionsInRange(cx, cy, 18)) { c++; } return c; }),
					Query("whoCanSee", () => { int c = 0; foreach (TcpConnection<GameState> t in map.GetConnectionsWhoCanSee(seen)) { c++; } return c; }),
					Query("statics", () => { int c = 0; foreach (AbstractInternalItem t in map.GetStaticsAndMultiComponentsInRange(hx, hy, 18)) { c++; } return c; }),
					Query("onCoords", () => { int c = 0; foreach (AbstractInternalItem t in map.GetStaticsAndMultiComponentsOnCoords(hx + 1, hy)) { c++; } return c; }),
					Query("multis", () => { int c = 0; foreach (MultiItemComponent t in map.GetMultiComponentsInRange(cx, cy, 18)) { c++; } return c; })
				};
				foreach (var q in queries) {
					int found = q.Value(); //warm-up: compiles the code and makes the sectors
					long before = GC.GetAllocatedBytesForCurrentThread();
					for (int i = 0; i < Calls; i++) {
						q.Value();
					}
					long bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / Calls;
					if (bytes != 0) {
						allocating.Add(q.Key + "=" + bytes + "B");
					}
					if ((q.Key != "multis") && (q.Key != "onCoords") && (found == 0)) {
						allocating.Add(q.Key + " found nothing"); //a query that finds nothing proves nothing about its walk
					}
				}
				report.Append(" noAlloc=").Append(allocating.Count == 0);
				if (allocating.Count > 0) {
					report.Append("(").Append(string.Join(";", allocating)).Append(")");
				}

				//the caller deletes each NPC it's handed: the walk read the next one before, so it still meets every one
				var npcsBefore = new List<object>();
				foreach (AbstractCharacter ch in map.GetNPCsInRectangle(rect14)) {
					npcsBefore.Add(ch);
				}
				int deleted = 0;
				foreach (AbstractCharacter ch in map.GetNPCsInRectangle(rect14)) {
					ch.Delete();
					deleted++;
				}
				report.Append(" deleteWhileWalking=").Append((npcsBefore.Count >= 3) && (deleted == npcsBefore.Count));
				if (deleted != npcsBefore.Count) {
					report.Append("(met " + deleted + " of " + npcsBefore.Count + ")");
				}
			} finally {
				self.P(home);
				foreach (Thing t in made) {
					if (t.IsDeleted) {
						continue;
					}
					if (t.Flag_Disconnected) {
						((Character) t).Reconnect();
					}
					t.Delete();
				}
			}
			Console.WriteLine(report.Append(" end RANGE_QUERIES_END").ToString());
		}

		private static KeyValuePair<string, Func<int>> Query(string name, Func<int> query) {
			return new KeyValuePair<string, Func<int>>(name, query);
		}

		/// <summary>Each query by range and by the same rectangle against the brute-force look.</summary>
		private static void CompareAll(Map map, byte m, ImmutableRectangle rect, int x, int y, int range, List<string> mismatches) {
			string at = x + "," + y + "," + range;
			Compare("chars@" + at, map.GetCharsInRange(x, y, range), Brute(m, rect, t => (t is AbstractCharacter) && !t.Flag_Disconnected), mismatches);
			Compare("things@" + at, map.GetThingsInRange(x, y, range), Brute(m, rect, t => !t.Flag_Disconnected), mismatches);
			Compare("items@" + at, map.GetItemsInRange(x, y, range), Brute(m, rect, t => t is AbstractItem), mismatches);
			Compare("players@" + at, map.GetPlayersInRange(x, y, range), Brute(m, rect, t => (t is AbstractCharacter) && t.IsPlayer && !t.Flag_Disconnected), mismatches);
			Compare("disconnects@" + at, map.GetDisconnectsInRange(x, y, range), Brute(m, rect, t => t.Flag_Disconnected), mismatches);
			CompareRectangle(map, m, rect, mismatches);
			var conns = new List<object>();
			foreach (AbstractCharacter player in Brute(m, rect, t => (t is AbstractCharacter) && t.IsPlayer && !t.Flag_Disconnected)) {
				if (player.GameState != null) {
					conns.Add(player.GameState.Conn);
				}
			}
			Compare("connections@" + at, map.GetConnectionsInRange(x, y, range), conns, mismatches);
		}

		private static void CompareRectangle(Map map, byte m, ImmutableRectangle rect, List<string> mismatches) {
			string at = rect.ToString();
			Compare("charsRect@" + at, map.GetCharsInRectangle(rect), Brute(m, rect, t => (t is AbstractCharacter) && !t.Flag_Disconnected), mismatches);
			Compare("thingsRect@" + at, map.GetThingsInRectangle(rect), Brute(m, rect, t => !t.Flag_Disconnected), mismatches);
			Compare("itemsRect@" + at, map.GetItemsInRectangle(rect), Brute(m, rect, t => t is AbstractItem), mismatches);
			Compare("npcsRect@" + at, map.GetNPCsInRectangle(rect), Brute(m, rect, t => (t is AbstractCharacter) && !t.IsPlayer && !t.Flag_Disconnected), mismatches);
			Compare("npcsLoaded@" + at, map.GetNPCsInLoadedSectors(rect), Brute(m, rect, t => (t is AbstractCharacter) && !t.IsPlayer && !t.Flag_Disconnected), mismatches);
			Compare("playersRect@" + at, map.GetPlayersInRectangle(rect), Brute(m, rect, t => (t is AbstractCharacter) && t.IsPlayer && !t.Flag_Disconnected), mismatches);
			Compare("disconnectsRect@" + at, map.GetDisconnectsInRectangle(rect), Brute(m, rect, t => t.Flag_Disconnected), mismatches);
		}

		/// <summary>Statics have no list to look over by brute force: the rectangle's query must equal its tiles' queries put together.</summary>
		private static void CompareStatics(Map map, int x, int y, int range, List<string> mismatches) {
			var byTile = new List<object>();
			for (int tx = x - range; tx <= x + range; tx++) {
				for (int ty = y - range; ty <= y + range; ty++) {
					foreach (AbstractInternalItem s in map.GetStaticsAndMultiComponentsOnCoords(tx, ty)) {
						byTile.Add(s);
					}
				}
			}
			Compare("statics", map.GetStaticsAndMultiComponentsInRange(x, y, range), byTile, mismatches);
			Compare("staticsRect", map.GetStaticsAndMultiComponentsInRectangle(new ImmutableRectangle(x, y, range)), byTile, mismatches);
		}

		/// <summary>What the sectors covering the rectangle hold in the named lists (Sector's own properties, by reflection),
		/// sectors x outer and y inner, each list in its order, filtered by the rectangle: what each query should hand out.</summary>
		private static List<object> SectorWalk(Map map, int minX, int minY, int maxX, int maxY, params string[] lists) {
			const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			Array sectors = (Array) typeof(Map).GetField("sectors", any).GetValue(map);
			FieldInfo firstThing = typeof(Thing).Assembly.GetType("SteamEngine.ThingLinkedList").GetField("firstThing", any);
			FieldInfo nextThing = typeof(Thing).GetField("nextInList", any);
			FieldInfo firstComponent = typeof(MultiItemComponent).Assembly.GetType("SteamEngine.MultiComponentLinkedList").GetField("firstMultiComponent", any);
			FieldInfo nextComponent = typeof(MultiItemComponent).GetField("nextInList", any);
			var found = new List<object>();
			int factor = (int) typeof(Map).GetField("sectorFactor", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			int sxEnd = Math.Min(maxX >> factor, sectors.GetLength(0) - 1), syEnd = Math.Min(maxY >> factor, sectors.GetLength(1) - 1);
			for (int sx = Math.Max(0, minX) >> factor; sx <= sxEnd; sx++) {
				for (int sy = Math.Max(0, minY) >> factor; sy <= syEnd; sy++) {
					object sector = sectors.GetValue(sx, sy);
					if (sector == null) {
						continue;
					}
					foreach (string list in lists) {
						object value = sector.GetType().GetProperty(list, any).GetValue(sector);
						var inList = new List<object>();
						if (value is StaticItem[] statics) {
							inList.AddRange(statics);
						} else if (list == "MultiComponents") {
							for (object c = (value == null) ? null : firstComponent.GetValue(value); c != null; c = nextComponent.GetValue(c)) {
								inList.Add(c);
							}
						} else {
							for (object t = firstThing.GetValue(value); t != null; t = nextThing.GetValue(t)) {
								inList.Add(t);
							}
						}
						foreach (object o in inList) {
							IPoint2D p = (IPoint2D) o;
							if ((minX <= p.X) && (minY <= p.Y) && (maxX >= p.X) && (maxY >= p.Y)) {
								found.Add(o);
							}
						}
					}
				}
			}
			return found;
		}

		private static void CompareInOrder<T>(string what, IEnumerable<T> query, List<object> expected, List<string> mismatches) {
			var got = new List<object>();
			foreach (T t in query) {
				got.Add(t);
			}
			bool same = got.Count == expected.Count;
			for (int i = 0; same && (i < got.Count); i++) {
				same = ReferenceEquals(got[i], expected[i]);
			}
			if (!same || ((what == "staticsOrder") && (got.Count == 0))) {
				mismatches.Add(what + " got " + got.Count + " expected " + expected.Count + (same ? " (none at all)" : " or another order"));
			}
		}

		/// <summary>Everything on the plane inside the rectangle, lying on the ground (characters always do), that the filter takes.</summary>
		private static List<object> Brute(byte m, ImmutableRectangle rect, Func<Thing, bool> filter) {
			var found = new List<object>();
			foreach (Thing t in Thing.AllThings) {
				if ((t.M == m) && t.IsOnGround && rect.Contains(t) && filter(t)) {
					found.Add(t);
				}
			}
			return found;
		}

		/// <summary>The same things, each found once.</summary>
		private static void Compare<T>(string what, IEnumerable<T> query, List<object> expected, List<string> mismatches) {
			var counts = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
			int total = 0;
			foreach (T t in query) {
				counts.TryGetValue(t, out int n);
				counts[t] = n + 1;
				total++;
			}
			bool ok = total == expected.Count;
			foreach (object e in expected) {
				ok &= counts.TryGetValue(e, out int n) && (n == 1);
			}
			if (!ok) {
				mismatches.Add(what + " found " + total + " expected " + expected.Count);
			}
		}
	}
}
