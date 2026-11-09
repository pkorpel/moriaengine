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
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#827 - server-side helpers for Tests/Protocol/HealerTests.cs.</summary>
	public static class HealerTests {
		/// <summary>Args (one string): "realm distance dead" - puts me on open ground in that realm, a fresh real
		/// converted healer (C_m_HEALER) `distance` tiles away on a spot it sees me from, and kills me if dead=1.</summary>
		[SteamFunction]
		public static void TestHealerSetup(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim().Trim('"').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Realm realm = (Realm) Enum.Parse(typeof(Realm), args[0]);
			int distance = int.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
			bool dead = args[2] == "1";

			if (self.Flag_Dead) {
				self.Resurrect();
			}
			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			self.Realm = realm;

			Map map = self.GetMap();
			NPC healer = (NPC) ((NPCDef) ThingDef.GetByDefname("C_m_HEALER")).Create(self.X, self.Y - distance, map.GetTileZ(self.X, self.Y - distance), self.M);
			int[][] spots = { new[] { 0, -1 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 1, 0 } };
			foreach (int[] d in spots) {
				int x = self.X + d[0] * distance, y = self.Y + d[1] * distance;
				healer.P(x, y, map.GetTileZ(x, y), self.M);
				if (healer.CanSeeLOS(self).Allow) {
					break;
				}
			}
			bool seesMe = healer.CanSeeLOS(self).Allow; //before dying - no NPC sees a ghost
			if (dead) {
				self.CauseDeath(null);
				self.SetTag(TagKey.Acquire("_diedAt_"), Globals.TimeInSeconds - 10); //#1122 - past #1070's 5 s before a ghost may rise
			}
			Console.WriteLine("HEALER_SETUP: healer=" + healer.Uid + " seesMe=" + seesMe + " dead=" + self.Flag_Dead + " realm=" + self.Realm);
		}

		[SteamFunction]
		public static void TestHealerReport(Character self) {
			Console.WriteLine("HEALER_REPORT: dead=" + self.Flag_Dead + " hits=" + self.Hits);
		}
	}
}
