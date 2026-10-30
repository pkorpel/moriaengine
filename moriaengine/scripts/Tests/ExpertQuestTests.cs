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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - server-side helpers for Tests/Protocol/ExpertQuestTests.cs.</summary>
	public static class ExpertQuestTests {
		private static Item newSite, newSign;

		private static Item GondorStone() {
			Map map = Map.GetMap(0);
			ThingDef def = ThingDef.GetByDefname(ExpertQuest.StartStoneDef);
			foreach (Thing t in map.GetThingsInRectangle(new ImmutableRectangle(2768, 2758, 2774, 2764))) {
				if (t.Def == def) {
					return (Item) t;
				}
			}
			return null;
		}

		/// <summary>A Novice of level 3 standing by the Gondor start stone; reports the stone's uid.</summary>
		[SteamFunction]
		public static void TestExpertQuestPrepare(Character self) {
			Player player = (Player) self;
			player.Level = 3;
			player.SkillRank = SkillRank.Novice;
			player.Realm = Realm.Gondor;
			Item stone = GondorStone();
			player.Go(stone.X + 1, stone.Y, WorldSpots.StandZ(stone.GetMap(), stone.X + 1, stone.Y), stone.M);
			Console.WriteLine("EXPERT_PREPARE: stone=" + stone.Uid + " sites=" + ExpertQuest.GetSites(stone).Count);
		}

		/// <summary>Back next to the Gondor start stone, keeping my rank.</summary>
		[SteamFunction]
		public static void TestExpertQuestGoToStone(Character self) {
			Item stone = GondorStone();
			self.Go(stone.X + 1, stone.Y, WorldSpots.StandZ(stone.GetMap(), stone.X + 1, stone.Y), stone.M);
			Console.WriteLine("EXPERT_AT_STONE: " + stone.Uid);
		}

		/// <summary>An unlinked site and sign next to me at the Gondor start stone, for the GM linking tools.</summary>
		[SteamFunction]
		public static void TestExpertQuestNewPieces(Character self) {
			Item stone = GondorStone();
			TestExpertQuestGoToStone(self);
			Map map = stone.GetMap();
			newSite = (Item) ThingDef.GetByDefname(ExpertQuest.SiteDef).Create(stone.X + 2, stone.Y, WorldSpots.StandZ(map, stone.X + 2, stone.Y), stone.M);
			newSign = (Item) ThingDef.GetByDefname(ExpertQuest.SignDef).Create(stone.X, stone.Y + 1, WorldSpots.StandZ(map, stone.X, stone.Y + 1), stone.M);
			Console.WriteLine("EXPERT_PIECES: stone=" + stone.Uid + " site=" + newSite.Uid + " sign=" + newSign.Uid + " invisibleBefore=" + newSite.Flag_Invisible);
		}

		/// <summary>Whether the pieces of TestExpertQuestNewPieces got linked; removes them.</summary>
		[SteamFunction]
		public static void TestExpertQuestLinked(Character self) {
			Console.WriteLine("EXPERT_LINKED: siteLinked=" + ExpertQuest.GetSites(GondorStone()).Contains(newSite) + " signLinked=" + ExpertQuest.GetSigns(newSite).Contains(newSign) +
				" siteInvisible=" + newSite.Flag_Invisible + " signName=" + newSign.Name.Replace(' ', '_'));
			newSite.Delete();
			newSign.Delete();
		}

		/// <summary>Makes my quest site's real minute timer fire now ("stay"), or after taking me out of the cave ("leave").</summary>
		[SteamFunction]
		public static void TestExpertQuestTick(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			bool leave = sa.Args.Trim().Trim('"') == "leave";
			foreach (Item site in ExpertQuest.GetSites(GondorStone())) {
				if (ExpertQuest.GetQuestPlayer(site) == player) {
					if (leave) {
						TestExpertQuestGoToStone(self);
					}
					site.GetTimer(ExpertQuest.timerTK).DueInSeconds = 0;
				}
			}
			Console.WriteLine("EXPERT_TICK: done");
		}

		/// <summary>The GM's .SetupExpertQuest again, from a plain account.</summary>
		[SteamFunction]
		public static void TestExpertQuestResetup(Character self) {
			new ExpertQuestSetup().Run(self);
		}

		/// <summary>Where I am, my rank, and - while I'm on the quest - its answers and a sign with its clue.</summary>
		[SteamFunction]
		public static void TestExpertQuestState(Character self) {
			Player player = (Player) self;
			Region region = player.Region;
			string state = "EXPERT_STATE: region=" + ((region == null) ? "none" : region.Defname) + " rank=" + player.SkillRank;
			foreach (Item site in ExpertQuest.GetSites(GondorStone())) {
				if (ExpertQuest.GetQuestPlayer(site) == player) {
					List<Item> signs = ExpertQuest.GetSigns(site);
					int clue;
					string name;
					ExpertQuest.TryGetClue(signs[0], out clue, out name);
					state += " onQuest=True signs=" + signs.Count + " answers=" + ExpertQuest.GetAnswer(site, 0) + "," +
						ExpertQuest.GetAnswer(site, 1) + "," + ExpertQuest.GetAnswer(site, 2) + " sign=" + signs[0].Uid + " clueName=" + name +
						" signName=" + signs[0].Name.Replace(' ', '_') + " siteInvisible=" + site.Flag_Invisible + " master=" + MasterUid(site);
				}
			}
			int busy = 0;
			foreach (Item site in ExpertQuest.GetSites(GondorStone())) {
				if (ExpertQuest.GetQuestPlayer(site) != null) {
					busy++;
				}
			}
			Console.WriteLine(state + " busySites=" + busy + " masters=" + CountMasters());
		}

		private static int MasterUid(Item site) {
			foreach (Thing t in site.GetMap().GetThingsInRectangle(new ImmutableRectangle(site.X - 8, site.Y - 8, site.X + 8, site.Y + 8))) {
				if (IsMaster(t)) {
					return t.Uid;
				}
			}
			return -1;
		}

		private static int CountMasters() {
			int masters = 0;
			Map map = Map.GetMap(0);
			foreach (ImmutableRectangle rect in StaticRegion.GetByDefname("a_expert").Rectangles) {
				foreach (Thing t in map.GetThingsInRectangle(rect)) {
					if (IsMaster(t)) {
						masters++;
					}
				}
			}
			return masters;
		}

		private static bool IsMaster(Thing t) {
			return t.Def == ThingDef.GetByDefname(ExpertQuest.MasterDef);
		}
	}
}
