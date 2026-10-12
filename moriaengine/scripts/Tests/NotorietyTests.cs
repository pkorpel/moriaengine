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

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// V-5 (verification pass, #73): server-side helpers for notoriety scenarios driven by real
	/// player-vs-player combat packets from Tests/Protocol/NotorietyTests.cs.
	/// </summary>
	public static class NotorietyTests {
		[SteamFunction]
		public static void TestSetCriminalFlagSeconds(Character self, ScriptArgs sa) {
			double seconds = Convert.ToDouble(sa.Argv[0], CultureInfo.InvariantCulture);
			Notoriety.CriminalFlagSeconds = seconds;
			Console.WriteLine("CRIMINAL_DURATION_TEST: SET seconds=" + seconds);
		}

		[SteamFunction]
		public static void TestCheckCriminal(Character self) {
			Console.WriteLine("CRIMINAL_CHECK: " + self.Name + " isCriminal=" + Notoriety.IsCriminal(self));
		}

		/// <summary>Directly drives Character.CauseDeath (the same real method a lethal hit calls)
		/// against the named, already-connected victim character 5 times in a row - the real
		/// MurdererThreshold - resurrecting the victim between each so every death is registered
		/// against a genuinely innocent (not yet criminal/murderer) player, same as 5 separate
		/// real unjustified kills would. Skips re-proving that a weapon swing can connect (V-1
		/// already did that thoroughly); what's under test here is Notoriety's own threshold and
		/// IsMurderer logic, exercised through the exact same production code path a real lethal
		/// hit uses.</summary>
		[SteamFunction]
		public static void TestSimulateMurders(Character self, ScriptArgs sa) {
			int victimUid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character victim = (Character) Thing.UidGetCharacter(victimUid);
			for (int i = 0; i < Notoriety.MurdererThreshold; i++) {
				victim.CauseDeath(self);
				victim.Resurrect();
			}
			Console.WriteLine("MURDER_SIM: killedBy=" + self.Name + " isMurderer=" + Notoriety.IsMurderer(self));
		}

		/// <summary>Stops self's queued auto-combat against the named target - real auto-combat
		/// keeps re-triggering Trigger_HostileAction (and so re-arming the criminal-flag expiry
		/// timer) on every swing for as long as the fight continues, so a criminal-expiry
		/// scenario needs a real, reliable way to end the fight before waiting for the flag to
		/// expire. Calls the same WeaponSkillTargetQueuePlugin.RemoveTarget a real client's
		/// "stop attacking" (e.g. switching out of war mode) ultimately goes through.</summary>
		[SteamFunction]
		public static void TestStopAttacking(Character self, ScriptArgs sa) {
			int targetUid = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Character target = (Character) Thing.UidGetCharacter(targetUid);
			WeaponSkillTargetQueuePlugin.RemoveTarget(self, target);
			Console.WriteLine("STOP_ATTACK_TEST: DONE");
		}
	}
}
