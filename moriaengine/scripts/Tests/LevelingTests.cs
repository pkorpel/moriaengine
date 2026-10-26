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
			string[] parts = SplitArgs(sa);
			first = Convert.ToInt32(parts[0], CultureInfo.InvariantCulture);
			second = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
		}

		/// <summary>General-purpose version of the same ScriptArgs dual-shape problem described
		/// above - handles any number of space-separated tokens, string or numeric, regardless
		/// of which command-parsing path the sender's account plevel routes through. Real bug
		/// found while adding TestLevelStatBonus (a single-argument command): the original
		/// version of this method assumed Argv.Length == 1 always meant "the whole un-split
		/// remainder as one string" - true for the primitive parser's multi-token case, but NOT
		/// true for a genuinely single numeric argument reaching here via the full LScript
		/// parser (elevated accounts), where Argv[0] is a boxed int, not a string, and casting
		/// it to string threw an InvalidCastException. Only treat Argv[0] as a combined string
		/// to split if it actually IS one.</summary>
		private static string[] SplitArgs(ScriptArgs sa) {
			if ((sa.Argv.Length == 1) && (sa.Argv[0] is string combined)) {
				return combined.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
			}
			string[] parts = new string[sa.Argv.Length];
			for (int i = 0; i < sa.Argv.Length; i++) {
				parts[i] = Convert.ToString(sa.Argv[i], CultureInfo.InvariantCulture);
			}
			return parts;
		}

		/// <summary>Reports the real .def-content-authored ExperienceWorth for the three titan
		/// variants (ogres.def) - requested directly for the user's own testing, see #12's note
		/// that NPC content is otherwise ~55-60% incomplete (no other monster has this set yet).
		/// Unlike TestKillNpcGrantsExperience's c_a_jackrabbit (which sets ExperienceWorth
		/// programmatically in C#), this exercises the "experience min max" LoadScriptLine
		/// parsing path from REAL .def text for the first time - that path had no test
		/// coverage before.</summary>
		[SteamFunction]
		public static void TestReportTitanExperience(Character self) {
			ReportTitanExperience("c_titan");
			ReportTitanExperience("c_titan_mage");
			ReportTitanExperience("c_titan_lord");
		}

		private static void ReportTitanExperience(string defname) {
			NPCDef def = (NPCDef) ThingDef.GetByDefname(defname);
			int[] range = def.ExperienceWorth;
			Console.WriteLine("TITAN_EXPERIENCE: defname=" + defname +
				" min=" + (range != null ? range[0].ToString(CultureInfo.InvariantCulture) : "null") +
				" max=" + (range != null ? range[1].ToString(CultureInfo.InvariantCulture) : "null"));
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
			Console.WriteLine("LEVELING_TEST_RESULT: level=" + player.Level + " experience=" + player.Experience +
				" unspentSkillPoints=" + player.UnspentSkillPoints + " unspentAbilityPoints=" + player.UnspentAbilityPoints);
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
		/// directly from within a SteamFunction invoked AS a chat command. Filed as #146 (real,
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

		/// <summary>Phase 4 (#129): assigns (or clears, with "none") self's profession directly -
		/// there's no real assignment flow yet (#135, professions implementation not started),
		/// this is the minimum viable path to exercise the profession-gated spend checks.</summary>
		[SteamFunction]
		public static void TestSetProfession(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string defname = SplitArgs(sa)[0];
			player.Profession = defname.Equals("none", StringComparison.OrdinalIgnoreCase)
				? null
				: ProfessionDef.GetByDefname(defname);
			Console.WriteLine("LEVELING_PROFESSION_SET: profession=" + (player.Profession != null ? player.Profession.Defname : "none"));
		}

		/// <summary>Phase 4 (#129): reports LevelingSystem.LevelStatBonus(level) for the given
		/// level - this formula has no pure in-process unit test (LevelingSystem lives in the
		/// Scripts assembly, which Tests.csproj doesn't reference), so it's verified this way
		/// like everything else this session.</summary>
		[SteamFunction]
		public static void TestLevelStatBonus(Character self, ScriptArgs sa) {
			int level = Convert.ToInt32(SplitArgs(sa)[0], CultureInfo.InvariantCulture);
			Console.WriteLine("LEVEL_STAT_BONUS: " + LevelingSystem.LevelStatBonus(level));
		}

		[SteamFunction]
		public static void TestSpendSkillPoint(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] parts = SplitArgs(sa);
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), parts[0], true);
			int points = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			bool success = LevelingSystem.TrySpendSkillPoints(player, skill, points);
			Console.WriteLine("LEVELING_SPEND_SKILL_RESULT: success=" + success +
				" unspentSkillPoints=" + player.UnspentSkillPoints +
				" skillValue=" + player.GetRealSkillValue(skill));
		}

		/// <summary>Real-user-reported: drives Character.ModifySkillValue (a TEMPORARY buff,
		/// same mechanism equipment/potions use - distinct from the leveling dialog's own fix
		/// above, which stopped using this path entirely) straight to exactly a +1000 modifier
		/// on a skill that starts at real value 0 - the precise shape that used to trigger
		/// Skill.DisposeIfEmpty's "modification==1000" typo (should have been "==0") and
		/// silently delete the Skill object outright.</summary>
		[SteamFunction]
		public static void TestModifySkillValueByExactly1000(Character self, ScriptArgs sa) {
			SkillName skill = (SkillName) Enum.Parse(typeof(SkillName), SplitArgs(sa)[0], true);
			self.ModifySkillValue(skill, 1000);
			Console.WriteLine("SKILL_MODIFY_TEST: realValue=" + self.GetRealSkillValue(skill) +
				" modifiedValue=" + self.GetModifiedSkillValue(skill));
		}

		/// <summary>Defers the actual spend to a near-immediate timer (#156) - same reason as
		/// DeferredNpcKillTimer below (#146): LevelingSystem.TrySpendAttributePoints now reads
		/// ProfessionDef.GetStatRange, and a profession's strRange/dexRange/etc FieldValue is a
		/// lazy, unresolved LScript expression until its first access. Resolving it
		/// SYNCHRONOUSLY inside this chat command's own call stack hits the same
		/// Commands.AuthorizeCommandThrow denial #146 already found (OpNode_Lazy_Expression.Run
		/// treats the resolution as a command-authorization check) - harmless in real gameplay,
		/// since a real player only reaches GetStatRange via D_LevelSpendAttributes' gump
		/// response handling, not a chat command's call stack.</summary>
		[SteamFunction]
		public static void TestSpendAttributePoint(Character self, ScriptArgs sa) {
			Player player = (Player) self;
			string[] parts = SplitArgs(sa);
			AttributeStat stat = (AttributeStat) Enum.Parse(typeof(AttributeStat), parts[0], true);
			int points = Convert.ToInt32(parts[1], CultureInfo.InvariantCulture);
			player.AddTimer(DeferredAttributeSpendTimer.TimerKey, new DeferredAttributeSpendTimer(player, stat, points)).DueInSeconds = 0;
		}
	}

	/// <summary>See TestKillNpcGrantsExperience's doc comment (#146) for why this exists - runs
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

	/// <summary>See LevelingTests.TestSpendAttributePoint's own doc comment (#156) for why this
	/// exists - runs the actual spend outside the chat command's own call stack.</summary>
	[SaveableClass]
	public class DeferredAttributeSpendTimer : BoundTimer {
		internal static readonly TimerKey TimerKey = TimerKey.Acquire("_deferredAttributeSpendTimer_");

		private Player player;
		private AttributeStat stat;
		private int points;

		[LoadingInitializer]
		public DeferredAttributeSpendTimer() {
		}

		public DeferredAttributeSpendTimer(Player player, AttributeStat stat, int points) {
			this.player = player;
			this.stat = stat;
			this.points = points;
		}

		protected override void OnTimeout(TagHolder cont) {
			bool success = LevelingSystem.TrySpendAttributePoints(this.player, this.stat, this.points);
			Console.WriteLine("LEVELING_SPEND_ATTRIBUTE_RESULT: success=" + success +
				" attributePointBudget=" + this.player.AttributePointBudget +
				" str=" + this.player.Str + " dex=" + this.player.Dex + " int=" + this.player.Int + " vit=" + this.player.Vit);
		}
	}
}
