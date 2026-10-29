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
using System.Globalization;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts {

	/// <summary>#826 - server-side helpers for Tests/Protocol/GuardSpeechTests.cs.</summary>
	public static class GuardSpeechTests {
		private static NPC lastGuard;

		/// <summary>Args (one string): "otherUid guardDef myRealm otherRealm fight" - fight is "other" (the other player
		/// attacked me, a real hostile act: they turn criminal), "me" (I attacked them) or "none" (no fight; the other
		/// one is flagged criminal anyway). Puts both players on open ground with a fresh real guard 2 tiles away.</summary>
		[SteamFunction]
		public static void TestGuardCallSetup(Character self, ScriptArgs sa) {
			string[] args = sa.Args.Trim().Trim('"').Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			Character other = (Character) Thing.UidGetThing(int.Parse(args[0], CultureInfo.InvariantCulture));
			string guardDef = args[1];
			Realm myRealm = (Realm) Enum.Parse(typeof(Realm), args[2]);
			Realm otherRealm = (Realm) Enum.Parse(typeof(Realm), args[3]);
			string fight = args[4];

			MovementSpeedTests.TestTeleportToOpenGround(self);
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			Map map = self.GetMap();
			other.P(self.X + 1, self.Y, map.GetTileZ(self.X + 1, self.Y), self.M); //the ground's own height - it can be a hillside
			foreach (Character ch in new[] { self, other }) {
				Notoriety.ClearCriminal(ch);
				FightMemory.Forget(ch);
			}
			self.Realm = myRealm;
			other.Realm = otherRealm;
			if (fight == "other") {
				self.Trigger_HostileAction(other);
			} else if (fight == "me") {
				other.Trigger_HostileAction(self);
			} else {
				Notoriety.FlagCriminal(other);
			}

			//2 tiles off, on a spot it really sees me from - even "open ground" can block terrain LOS between two tiles
			lastGuard = (NPC) ((NPCDef) ThingDef.GetByDefname(guardDef)).Create(self.X, self.Y - 2, map.GetTileZ(self.X, self.Y - 2), self.M);
			int[][] spots = { new[] { 0, -2 }, new[] { -2, 0 }, new[] { 0, 2 }, new[] { -2, -2 }, new[] { -2, 2 }, new[] { 2, -2 }, new[] { 2, 2 }, new[] { 3, 0 } };
			foreach (int[] d in spots) {
				int x = self.X + d[0], y = self.Y + d[1];
				lastGuard.P(x, y, map.GetTileZ(x, y), self.M);
				if (lastGuard.CanSeeLOS(self).Allow) {
					break;
				}
			}
			Console.WriteLine("GUARD_CALL_SETUP: guardSeesMe=" + lastGuard.CanSeeLOS(self).Allow + " guard=" + lastGuard.Name + " realm=" + lastGuard.Realm +
				" otherCriminal=" + Notoriety.IsCriminal(other) + " meCriminal=" + Notoriety.IsCriminal(self));
		}

		[SteamFunction]
		public static void TestGuardCallReport(Character self) {
			NPCAiTimer timer = NPCAiTimer.GetFor(lastGuard);
			Character target = (timer == null) ? null : timer.Target;
			Console.WriteLine("GUARD_CALL_TARGET: " + ((target == null) ? "none" : target.Name));
		}
	}
}
