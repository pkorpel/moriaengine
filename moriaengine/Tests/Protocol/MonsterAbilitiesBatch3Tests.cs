using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// Real-user-reported follow-up ("what are some other monster abilities we can
	/// implement?") - a_tevent_e_summoneater/e_parakrik/e_npc_curse/e_npc_summoning/
	/// e_npc_sniper/e_npc_infiniteMana are upgraded from zero-effect stubs to real behavior
	/// (MonsterSpecialAttacks.cs). See scripts/Tests/MonsterAbilitiesBatch3Tests.cs's own doc
	/// comment for the full writeup.
	/// </summary>
	[Collection(MonsterAbilitiesBatch3Collection.Name)]
	public class MonsterAbilitiesBatch3Tests {
		private const string Account = "monsterabilitiesbatch3test";
		//6 Facts in this file would push the shared Account over its 5-character-slot cap
		//(the already-documented class of intermittent flakiness) - NpcCurse/Sniper spread
		//onto this second account instead, same precedent as GatheringTests.cs's AccountB.
		private const string AccountB = "monsterabilitiesbatch3testb";
		private const string Password = "monsterabilitiesbatch3test";

		private static bool autoAccountCreationEnabled;

		private readonly GameServerPairFixture fixture;

		public MonsterAbilitiesBatch3Tests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		[Fact]
		public void SummonEaterDeletesASummonButLeavesAnOrdinaryMonsterAlone() {
			using (var client = this.ConnectLoginAndCreateCharacter("SummonEaterChecker")) {
				client.SendSpeech(".TestSummonEaterDeletesSummonButNotOrdinaryTarget");
				Assert.True(this.fixture.WaitForGameOutput("SUMMONEATER_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SUMMONEATER_TEST:");
				Assert.Contains("consumedSummon=True", line);
				Assert.Contains("summonDeleted=True", line);
				Assert.Contains("consumedOrdinary=False", line);
				Assert.Contains("ordinaryDeleted=False", line);
			}
		}

		[Fact]
		public void ParaKrikParalyzesANearbyPlayer() {
			using (var client = this.ConnectLoginAndCreateCharacter("ParaKrikChecker")) {
				client.SendSpeech(".TestParaKrikParalyzesNearbyPlayer");
				Assert.True(this.fixture.WaitForGameOutput("PARAKRIK_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "PARAKRIK_TEST:");
				Assert.Contains("paralyzed=True", line);
			}
		}

		[Fact]
		public void NpcCurseDamagesPlayersAndPetsButNotWildMonsters() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "NpcCurseChecker")) {
				client.SendSpeech(".TestNpcCurseDamagesPlayerAndPetButNotWildMonster");
				Assert.True(this.fixture.WaitForGameOutput("NPCCURSE_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "NPCCURSE_TEST:");
				Assert.Contains("playerDamaged=True", line);
				Assert.Contains("petDamaged=True", line);
				Assert.Contains("wildMonsterDamaged=False", line);
			}
		}

		[Fact]
		public void SummonerSpawnsConfiguredReinforcements() {
			using (var client = this.ConnectLoginAndCreateCharacter("SummonerChecker")) {
				client.SendSpeech(".TestSummonerSpawnsConfiguredReinforcements");
				Assert.True(this.fixture.WaitForGameOutput("SUMMONER_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SUMMONER_TEST:");
				Assert.Contains("countBefore=0", line);
				Assert.Contains("countAfter=1", line);
				Assert.Contains("foundHostileSummonedOne=True", line);
			}
		}

		[Fact]
		public void SniperOnlyAddsBonusDamageAgainstPlayers() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "SniperChecker")) {
				client.SendSpeech(".TestSniperAddsBonusDamageVsPlayerAtRangeOnly");
				Assert.True(this.fixture.WaitForGameOutput("SNIPER_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "SNIPER_TEST:");
				Assert.Contains("vsPlayerDamage=150", line);
				Assert.Contains("vsMonsterDamage=100", line);
			}
		}

		[Fact]
		public void InfiniteManaOnlyAppliesToNpcsWithTheAbility() {
			using (var client = this.ConnectLoginAndCreateCharacter("InfiniteManaChecker")) {
				client.SendSpeech(".TestInfiniteManaKeepsManaFullOnlyWithAbility");
				Assert.True(this.fixture.WaitForGameOutput("INFINITEMANA_TEST:", TimeSpan.FromSeconds(10)));
				string line = LastLineContaining(this.fixture.GameOutputSnapshot(), "INFINITEMANA_TEST:");
				Assert.Contains("attackerMana=1000", line);
				Assert.Contains("dummyMana=1", line);
			}
		}

		/// <summary>Real engine quirk found while debugging this file, same class as #146's
		/// own documented finding (LevelingTests.cs) but via a DIFFERENT path: #406's "only the
		/// literal top-level command word gets gated" fix only clears Commands.
		/// topLevelCommandPending from AuthorizeCommandThrow's own lazy-expression-resolution
		/// call site - Commands.AuthorizeCommand (the SEPARATE entry point SimpleCommandParser
		/// itself uses for a non-elevated account's TOP-LEVEL command dispatch) never touches
		/// that flag at all. So on AccountB specifically (MaxPlevel below
		/// Globals.PlevelForLscriptCommands, routes through SimpleCommandParser), the flag stays
		/// true straight through the Test-prefix exemption, and the VERY FIRST cold lazy
		/// FieldValue resolution triggered by the SteamFunction's own body (e.g. a brand-new
		/// NPCDef's never-before-touched ANIM/WeaponType/etc expression) gets wrongly
		/// re-gated as if IT were the real top-level command, using its own internal expression
		/// name (not "Test"-prefixed) - "No permission to run that command". Promoting to GM
		/// first (via the existing, account-order-independent .TestPromoteToGM) raises MaxPlevel
		/// so the SAME command instead routes through the LScript parser, whose own top-level
		/// symbol resolution clears topLevelCommandPending correctly before any of this test's
		/// own code ever runs - then demoting back to a plain player (.TestSetGmOff, which only
		/// touches PLevel, not MaxPlevel) restores the "not a GM" precondition several of these
		/// abilities' own target filters need, without re-triggering the bug (MaxPlevel, which
		/// controls ROUTING, stays elevated).</summary>
		private void PromoteThenDemoteToAvoidColdLazyFieldAuthBug(TestGameClient client) {
			client.SendSpeech(".TestPromoteToGM");
			Assert.True(this.fixture.WaitForGameOutput("PROMOTE_TO_GM:", TimeSpan.FromSeconds(10)));
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSetGmOff");
			Assert.True(this.fixture.WaitForGameOutputSince("GM_MODE_TEST:", mark, TimeSpan.FromSeconds(10)));
		}

		private static string LastLineContaining(string output, string marker) {
			int idx = output.LastIndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in:\n" + output);
			int end = output.IndexOf('\n', idx);
			if (end < 0) {
				end = output.Length;
			}
			return output.Substring(idx, end - idx);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string charName) {
			return this.ConnectLoginAndCreateCharacter(Account, charName);
		}

		private TestGameClient ConnectLoginAndCreateCharacter(string account, string charName) {
			int redirectPort = this.WaitForServerListingAndGetRedirectPort(account);
			InvalidOperationException lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				TestGameClient game = new TestGameClient("127.0.0.1", redirectPort);
				try {
					game.SendLogin(account, Password);
					game.ReadCharacterList();
					game.SendCreateCharacter(charName);
					return game;
				} catch (InvalidOperationException e) {
					game.Dispose();
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Could not log in and create character '" + charName + "' after retrying.", lastFailure);
		}

		//AccountB is never the server's literal first-ever account, so it needs auto-account-
		//creation explicitly enabled first - same precedent as GatheringTests.cs/AttrFlagTests.cs.
		//Guarded by a static bool so this only actually runs once per test process (creating
		//"AutoAccountEnabler" itself consumes one of Account's own 5 slots every call, not just
		//the first).
		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
			autoAccountCreationEnabled = true;
		}

		private int WaitForServerListingAndGetRedirectPort(string account) {
			Exception lastFailure = null;
			for (int attempt = 0; attempt < 40; attempt++) {
				try {
					using (var login = new TestLoginClient("127.0.0.1", GameServerPairFixture.LoginPort)) {
						login.SendLogin(account, Password);

						int serverCount = login.ReadServersList();
						if (serverCount < 1) {
							throw new InvalidOperationException("No identified game server yet (server count 0).");
						}

						login.SendServerSelect(0);
						var (redirectPort, _) = login.ReadRedirect();
						Assert.True(redirectPort > 0, "Expected a real port in the login redirect.");
						return redirectPort;
					}
				} catch (InvalidOperationException e) {
					lastFailure = e;
					Thread.Sleep(300);
				}
			}
			throw new TimeoutException("Game server never appeared in AuxiliaryServer's list.", lastFailure);
		}
	}

	[CollectionDefinition(MonsterAbilitiesBatch3Collection.Name, DisableParallelization = true)]
	public class MonsterAbilitiesBatch3Collection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MonsterAbilitiesBatch3Pair";
	}
}
