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
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#828 - server-side helpers for Tests/Protocol/SkillTeacherTests.cs.</summary>
	public static class SkillTeacherTests {
		/// <summary>Args (one string): "profession anatomy level rank requireRank gold" - gives me that profession
		/// (profession_mage: Anatomy 0-75, profession_priest: 30-100), Anatomy (tenths), level, SkillRank and exactly
		/// that much gold, sets the requireRank switch and puts the real converted anatomy teacher (c_m_anatomy)
		/// 2 tiles away on a spot it sees me from.</summary>
		[SteamFunction]
		public static void TestTeacherSetup(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim().Trim('"').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Player player = (Player) self;
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);

			player.Profession = ProfessionDef.GetByDefname(args[0]);
			player.SetRealSkillValue((int) SkillName.Anatomy, int.Parse(args[1], CultureInfo.InvariantCulture));
			player.Level = int.Parse(args[2], CultureInfo.InvariantCulture);
			player.SkillRank = (SkillRank) Enum.Parse(typeof(SkillRank), args[3]);
			SkillTeacherSettings.instance.requireRank = args[4] == "1";
			SetGold(player, int.Parse(args[5], CultureInfo.InvariantCulture));

			Map map = self.GetMap();
			NPC teacher = (NPC) ((NPCDef) ThingDef.GetByDefname("c_m_anatomy")).Create(self.X, self.Y - 2, map.GetTileZ(self.X, self.Y - 2), self.M);
			int[][] spots = { new[] { 0, -2 }, new[] { -2, 0 }, new[] { 0, 2 }, new[] { 2, 0 } };
			foreach (int[] d in spots) {
				int x = self.X + d[0], y = self.Y + d[1];
				teacher.P(x, y, map.GetTileZ(x, y), self.M);
				if (teacher.CanSeeLOS(self).Allow) {
					break;
				}
			}
			Console.WriteLine("TEACHER_SETUP: seesMe=" + teacher.CanSeeLOS(self).Allow + " teaches=" + ((NPCDef) teacher.Def).TeachesSkill);
		}

		private static void SetGold(Player player, int amount) {
			List<AbstractItem> golds = new List<AbstractItem>();
			foreach (AbstractItem container in new AbstractItem[] { player.Backpack, player.Bank }) {
				foreach (AbstractItem item in container) {
					if (item.Def == GoldUtils.GoldDef) {
						golds.Add(item);
					}
				}
			}
			foreach (AbstractItem gold in golds) {
				gold.Delete();
			}
			if (amount > 0) {
				Item gold = (Item) ThingDef.GetByDefname("i_gold").Create(player.Backpack);
				gold.Amount = amount;
			}
		}

		[SteamFunction]
		public static void TestTeacherReport(Character self) {
			Player player = (Player) self;
			Console.WriteLine("TEACHER_REPORT: anatomy=" + player.GetRealSkillValue((int) SkillName.Anatomy) +
				" gold=" + (GoldUtils.CountGoldIn(player.Backpack) + GoldUtils.CountGoldIn(player.Bank)));
		}
	}
}
