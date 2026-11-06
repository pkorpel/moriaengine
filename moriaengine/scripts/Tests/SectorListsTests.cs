using System;
using System.Collections.Generic;
using System.Text;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {
	/// <summary>
	/// #1214 - a map sector keeps characters and ground items in two lists. Every range query must still find each thing once,
	/// and only things of its kind, as they are made, moved across sectors, disconnected, picked up, dropped and deleted.
	/// Client side: Tests/Protocol/SectorListsTests.cs.
	/// </summary>
	public static class SectorListsTests {
		private const int Range = 3;

		[SteamFunction]
		public static void TestSectorLists(Character self) {
			Map map = self.GetMap();
			byte m = self.M;
			int x = self.X + 40, y = self.Y; //two sectors east of the player, nothing else there
			int y2 = y + 17;
			int x2 = FindOpenRow(map, x + 33, y2); //two sectors further east, one south, three free tiles in a row
			StringBuilder report = new StringBuilder("SECTOR_LISTS:");

			NPC npc = (NPC) ((NPCDef) ThingDef.GetByDefname("c_a_jackrabbit")).Create(x, y, map.GetTileZ(x, y), m);
			NPCAiTimer.GetFor(npc).NeverSleeps = true;
			Item item = (Item) ((ItemDef) ThingDef.GetByDefname("i_dagger")).Create(x + 1, y, map.GetTileZ(x + 1, y), m);
			Point4D home = new Point4D(self.X, self.Y, self.Z, self.M);
			try {
				Check(report, "made", Finds(map, x, y, npc, item, true, true));

				npc.P(x2, y2, map.GetTileZ(x2, y2), m);
				item.P(x2 + 1, y2, map.GetTileZ(x2 + 1, y2), m);
				Check(report, "movedAway", Finds(map, x, y, npc, item, false, false));
				Check(report, "movedHere", Finds(map, x2, y2, npc, item, true, true));

				npc.Disconnect();
				bool inDisconnects = Count(map.GetDisconnectsInRange(x2, y2, Range), npc) == 1;
				Check(report, "disconnected", Finds(map, x2, y2, npc, item, false, true) && inDisconnects);
				npc.Reconnect();
				bool outOfDisconnects = Count(map.GetDisconnectsInRange(x2, y2, Range), npc) == 0;
				Check(report, "reconnected", Finds(map, x2, y2, npc, item, true, true) && outOfDisconnects);

				//a wall or field spell asks CanFit with characters: the NPC's own tile is taken, without them it's free
				bool fitsWithoutChars = map.CanFit(npc.X, npc.Y, npc.Z, 16, false, false, false);
				bool fitsWithChars = map.CanFit(npc.X, npc.Y, npc.Z, 16, false, true, false);
				Check(report, "fit", fitsWithoutChars && !fitsWithChars);
				if (!fitsWithoutChars || fitsWithChars) {
					report.Append("(at=" + npc.X + "/" + npc.Y + "/" + npc.Z + ",withoutChars=" + fitsWithoutChars + ",withChars=" + fitsWithChars + ")");
				}

				item.Cont = self.Backpack;
				Check(report, "pickedUp", Finds(map, x2, y2, npc, item, true, false));
				item.P(x2 + 1, y2, map.GetTileZ(x2 + 1, y2), m);
				Check(report, "dropped", Finds(map, x2, y2, npc, item, true, true));

				//a player is a character too, and also in the sector's players
				self.P(x2 - 1, y2, map.GetTileZ(x2 - 1, y2), m);
				bool player = (Count(map.GetCharsInRange(x2, y2, Range), self) == 1) && (Count(map.GetPlayersInRange(x2, y2), self) == 1) &&
					(Count(map.GetThingsInRange(x2, y2, Range), self) == 1) && (Count(map.GetNPCsInRectangle(new ImmutableRectangle(x2, y2, Range)), self) == 0) &&
					(Count(map.GetItemsInRange(x2, y2, Range), self) == 0);
				self.P(home);
				player &= Count(map.GetCharsInRange(x2, y2, Range), self) == 0;
				Check(report, "player", player);
			} finally {
				self.P(home);
				npc.Delete();
				item.Delete();
			}
			Check(report, "deleted", Finds(map, x2, y2, npc, item, false, false));
			Console.WriteLine(report.Append(" end SECTOR_LISTS_END").ToString());
		}

		/// <summary>The first x from x0 east where x-1, x and x+1 on row y have nothing blocking and nobody standing there,
		/// so CanFit at x answers only for what the test puts there.</summary>
		private static int FindOpenRow(Map map, int x0, int y) {
			for (int x = x0; x < x0 + 12; x++) {
				bool open = true;
				for (int dx = -1; dx <= 1; dx++) {
					open &= map.CanFit(x + dx, y, map.GetTileZ(x + dx, y), 16, false, true, false); //no surface asked: a slope's land Z isn't its average
				}
				if (open) {
					return x;
				}
			}
			return x0;
		}

		/// <summary>Every query around (x, y) finds the NPC exactly once when expected and never otherwise, the same for the item.
		/// A thing in the wrong list makes the query's cast throw, so the command fails before it reports.</summary>
		private static bool Finds(Map map, int x, int y, NPC npc, Item item, bool npcThere, bool itemThere) {
			int npcs = npcThere ? 1 : 0;
			int items = itemThere ? 1 : 0;
			ImmutableRectangle rect = new ImmutableRectangle(x, y, Range);
			return (Count(map.GetCharsInRange(x, y, Range), npc) == npcs) &&
				(Count(map.GetNPCsInRectangle(rect), npc) == npcs) &&
				(Count(map.GetNPCsInLoadedSectors(rect), npc) == npcs) &&
				(Count(map.GetThingsInRange(x, y, Range), npc) == npcs) &&
				(Count(map.GetItemsInRange(x, y, Range), item) == items) &&
				(Count(map.GetThingsInRange(x, y, Range), item) == items);
		}

		private static int Count<T>(IEnumerable<T> found, Thing wanted) {
			int count = 0;
			foreach (T thing in found) {
				if (ReferenceEquals(thing, wanted)) {
					count++;
				}
			}
			return count;
		}

		private static void Check(StringBuilder report, string step, bool ok) {
			report.Append(' ').Append(step).Append('=').Append(ok ? "True" : "False");
		}
	}
}
