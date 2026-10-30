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

using System.Collections.Generic;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - the Expert quest's pieces: Erebor's two start stones, and one quest site per rectangle of the a_expert
	/// cave with six clue signs around it, all linked.</summary>
	public sealed class ExpertQuestSetup : WorldSetup {
		public override string Name {
			get {
				return "Expert quest";
			}
		}

		[SteamFunction]
		public static void SetupExpertQuest(Character self) {
			new ExpertQuestSetup().Run(self);
		}

		protected internal override void Place(WorldPieces pieces, Character gm) {
			Map map = Map.GetMap(0);
			StaticRegion cave = StaticRegion.GetByDefname("a_expert");
			if (cave == null) {
				gm.SysMessage("The a_expert region doesn't exist - the Expert quest has nowhere to go.");
				return;
			}
			List<Item> sites = new List<Item>();
			int siteNumber = 0;
			foreach (ImmutableRectangle rect in cave.Rectangles) {
				Item site = PlaceSite(pieces, map, rect, siteNumber);
				if (site != null) {
					sites.Add(site);
					siteNumber++;
				}
			}
			foreach (Item stone in new[] { PlaceStone(pieces, map, "gondor", 2771, 2761), PlaceStone(pieces, map, "mordor", 3723, 2953) }) {
				foreach (Item site in sites) {
					ExpertQuest.LinkSite(stone, site);
				}
			}
		}

		private static Item PlaceStone(WorldPieces pieces, Map map, string name, int x, int y) {
			return pieces.Item("expert/stone/" + name, ExpertQuest.StartStoneDef, new[] { x, y, WorldSpots.StandZ(map, x, y) }, 0);
		}

		//a site on the standable spot closest to the rectangle's middle, and six signs on standable spots around it,
		//at least 4 tiles from the site and from each other
		private static Item PlaceSite(WorldPieces pieces, Map map, ImmutableRectangle rect, int number) {
			int cx = (rect.MinX + rect.MaxX) / 2, cy = (rect.MinY + rect.MaxY) / 2;
			List<int[]> spots = new List<int[]>();
			for (int x = rect.MinX; x <= rect.MaxX; x++) {
				for (int y = rect.MinY; y <= rect.MaxY; y++) {
					int z;
					if (WorldSpots.CanStand(map, x, y, out z)) {
						spots.Add(new[] { x, y, z });
					}
				}
			}
			if (spots.Count == 0) {
				return null;
			}
			spots.Sort((a, b) => WorldSpots.Distance(a, cx, cy).CompareTo(WorldSpots.Distance(b, cx, cy)));
			string id = "expert/site" + number;
			Item site = pieces.Item(id, ExpertQuest.SiteDef, spots[0], 0);

			List<int[]> taken = new List<int[]> { spots[0] };
			foreach (int[] spot in spots) {
				if (taken.Count > 6) {
					break;
				}
				if (taken.TrueForAll(p => WorldSpots.Distance(p, spot[0], spot[1]) >= 4)) {
					ExpertQuest.LinkSign(site, pieces.Item(id + "/sign" + taken.Count, ExpertQuest.SignDef, spot, 0));
					taken.Add(spot);
				}
			}
			return site;
		}
	}
}
