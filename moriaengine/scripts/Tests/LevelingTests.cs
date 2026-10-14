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
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	/// <summary>
	/// Server-side setup/inspection helpers for the leveling system's protocol tests
	/// (Tests/Protocol/LevelingTests.cs).
	/// </summary>
	public static class LevelingTests {
		/// <summary>Real finding while building Phase 3's two-account tests: Commands.InvokeCommand
		/// only runs a chat command through the full LScript parser (which splits multiple
		/// space-separated arguments into separate Argv entries) when the SENDING account's
		/// MaxPlevel is >= Globals.PlevelForLscriptCommands (default 2) - see
		/// GameCore/Commands.cs's InvokeCommand and SimpleCommandParser.TryRunSnippet. Below that
		/// threshold (the default for any newly-auto-created account - AbstractAccount.
		/// HandleLoginAttempt only grants MaximalPlevel to the very FIRST account ever created on
		/// the server, accounts.Count==1 at that moment, never to any later one), the primitive
		/// SimpleCommandParser passes the ENTIRE remainder after the command name as a single
		/// Argv[0] string. Every two-argument test command in this file needs to tolerate both
		/// shapes, since which account happens to be "first" depends on test execution order
		/// across the whole file, not just within one Fact.</summary>
		private static void ParseTwoInts(ScriptArgs sa, out int first, out int second) {
			if (sa.Argv.Length >= 2) {
				first = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
				second = Convert.ToInt32(sa.Argv[1], CultureInfo.InvariantCulture);
			} else {
				string[] parts = ((string) sa.Argv[0]).Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
				first = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);
				second = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			}
		}

		/// <summary>Cleans up leftover NPCs/corpses near self - call before any scenario that opens
		/// a dialog or otherwise waits on a specific packet, since this file's Phase 2/3 scenarios
		/// (real NPC and player deaths) leave corpses behind that a later, unrelated connection's
		/// normal nearby-object sync burst then has to wade through, risking pushing the packet
		/// actually being waited for past a fixed timeout (see ExpLevelDialogOpensWithoutError in
		/// the protocol test file).</summary>
		[SteamFunction]
		public static void TestCleanupNearbyDebris(Character self) {
			VerificationTestUtils.CleanupNearbyNpcs(self, 20);
			VerificationTestUtils.CleanupNearbyCorpses(self, 20);
			Console.WriteLine("LEVELING_CLEANUP: DONE");
		}

		[SteamFunction]
		public static void TestSetLevelAndExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			int level, experience;
			ParseTwoInts(sa, out level, out experience);
			player.Level = level;
			player.Experience = experience;
			Console.WriteLine("LEVELING_TEST_SETUP: level=" + player.Level + " experience=" + player.Experience);
		}

		[SteamFunction]
		public static void TestAddExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			int amount = Convert.ToInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			LevelingSystem.AddExperience(player, amount);
			Console.WriteLine("LEVELING_TEST_RESULT: level=" + player.Level + " experience=" + player.Experience);
		}

		/// <summary>Phase 2 (#127): kills a freshly-spawned NPC directly via CauseDeath (bypassing
		/// real combat RNG/timing entirely - this is testing the experience-grant hook, not combat
		/// itself) and reports the killer's resulting level/experience. c_a_jackrabbit is used
		/// because it's a Calm animal (won't fight back) not used by any other test file sharing
		/// this long-lived server process (see CombatDepthTests.cs's TestGuardsRealmAggro comment
		/// for why that matters - mutating a shared NPCDef that no other scenario touches avoids
		/// leaking state between tests).
		///
		/// Real finding while building this: CauseDeath (via SoundCalculator.PlayDeathSound ->
		/// CharacterDef.DeathSound) lazily evaluates the .def file's "snd_ANIMALS_RABBIT3 + 4"
		/// LScript expression - EVERY animal .def in this codebase uses that same "snd_X + N"
		/// convention for DeathSound, not just this one. OpNode_Lazy_Expression.Run authorization-
		/// checks that evaluation against Commands.AuthorizeCommandThrow(Globals.Src, ...) - and
		/// while a chat command is executing (Commands.commandRunning == true, set for the whole
		/// duration of InvokeCommand, including any SteamFunction it calls synchronously),
		/// Globals.Src is the CURRENT non-GM command sender, which isn't authorized for that
		/// internal symbol name, so it throws "No permission to run that command". This can never
		/// happen in real gameplay (a monster's real death from weapon/spell damage happens via
		/// async combat timers, never synchronously nested inside a player's own command
		/// dispatch) - it only surfaces here because this test deliberately calls CauseDeath
		/// directly from within a SteamFunction invoked AS a chat command. Filed as #145 (real,
		/// narrow-scope latent engine quirk - not fixed here, same deferral precedent as #96/#73).
		/// Worked around by deferring the actual kill to a near-immediate timer, so it runs after
		/// the command has finished (commandRunning back to false) - which also more accurately
		/// mirrors how a real monster death actually happens.</summary>
		[SteamFunction]
		public static void TestKillNpcGrantsExperience(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			int min, max;
			ParseTwoInts(sa, out min, out max);

			NPCDef rabbitDef = (NPCDef) ThingDef.GetByDefname("c_a_jackrabbit");
			rabbitDef.ExperienceWorth = new int[] { min, max };
			NPC rabbit = (NPC) rabbitDef.Create(self.X + 1, self.Y, self.Z, self.M);
			player.AddTimer(DeferredNpcKillTimer.TimerKey, new DeferredNpcKillTimer(rabbit, player)).DueInSeconds = 0;
		}

		/// <summary>Phase 3 (#128): kills the player identified by `sa.Argv[0]`'s uid directly via
		/// CauseDeath(self) (bypassing combat RNG/timing - this is testing the death-exp-loss/
		/// pay-the-killer hook, not combat) and reports BOTH sides' resulting level/experience in
		/// one line, since this single server process already holds real references to both
		/// simultaneously-connected characters (see SocialTests.cs's TestReportMyUid/ExtractUid
		/// pattern for how the two protocol-test connections learn each other's uid).
		///
		/// Resurrects the victim first if already dead - Character.CauseDeath is a no-op once
		/// Flag_Dead is set (real finding while building PostDeathImmunityPreventsASecondLoss...:
		/// without this, that test's second kill call would silently do nothing at all, and the
		/// test would "pass" for completely the wrong reason - CauseDeath's own already-dead
		/// guard, not the actual immunity-timer logic under test).</summary>
		[SteamFunction]
		public static void TestKillPlayerAndReportResult(Character self, ScriptArgs sa) {
			Player killer = (Player) self;
			uint victimUid = Convert.ToUInt32(sa.Argv[0], CultureInfo.InvariantCulture);
			Player victim = (Player) Thing.UidGetCharacter(victimUid);
			if (victim.Flag_Dead) {
				victim.Resurrect();
			}
			victim.CauseDeath(killer);
			Console.WriteLine("LEVELING_DEATH_TEST_RESULT: victimLevel=" + victim.Level + " victimExperience=" + victim.Experience +
				" killerLevel=" + killer.Level + " killerExperience=" + killer.Experience);
		}
	}

	/// <summary>See TestKillNpcGrantsExperience's doc comment (#145) for why this exists - runs
	/// the kill outside the chat command's own call stack.</summary>
	[SaveableClass]
	public class DeferredNpcKillTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_deferredNpcKillTimer_");

		private NPC victim;
		private Player killer;

		[LoadingInitializer]
		public DeferredNpcKillTimer() {
		}

		public DeferredNpcKillTimer(NPC victim, Player killer) {
			this.victim = victim;
			this.killer = killer;
		}

		protected override void OnTimeout(TagHolder cont) {
			this.victim.CauseDeath(this.killer);
			Console.WriteLine("LEVELING_KILL_TEST_RESULT: level=" + this.killer.Level + " experience=" + this.killer.Experience);
		}
	}
}
