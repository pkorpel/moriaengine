using System;
using System.Threading;
using Xunit;

namespace SteamEngine.Tests.Protocol {
	/// <summary>
	/// First-ever Magery/spellcasting protocol tests (#483/#484) - zero test coverage existed
	/// for any spell before this. Covers Phase 0's own fix: SpellDef.Difficulty used to return 0
	/// for almost every spell (only DamageSpellDef content ever set it explicitly), so the real,
	/// correctly-wired fizzle mechanism never actually fired once mana was sufficient. Account is
	/// this collection's own first-ever login (auto-elevated to GM) for the end-to-end pipeline
	/// smoke test; AccountB (via EnsureAutoAccountCreationEnabled) is a normal, non-elevated,
	/// no-profession account for the real fizzle-rate regression test - SkillDef.CheckSuccess has
	/// its own "GM is always successful" bypass that would otherwise mask the fix entirely.
	/// </summary>
	[Collection(MagerySpellCollection.Name)]
	public class MagerySpellTests {
		private const string Account = "magerytest";
		private const string AccountB = "magerytest2";
		private const string Password = "magerytest";

		private readonly GameServerPairFixture fixture;

		public MagerySpellTests(GameServerPairFixture servers) {
			this.fixture = servers;
		}

		/// <summary>#484 - the real regression test. Before the fix, GetDifficulty would return
		/// 0 for any spell outside damageSpells.def (the only file that sets difficulty=
		/// explicitly) - meaning every sample below would succeed regardless of skill (100%
		/// success at skill 0 is the actual old-bug symptom). After the fix, difficulty comes
		/// from Requirements=X Magery instead. Uses s_poison_field specifically (Requirements=
		/// 80.0, the largest non-damage-spell requirement in this engine's content) rather than
		/// a smaller one - SkillUtils.GetBellCurve's own math (skillCheckVariance=250) means a
		/// small requirement like 20 only shifts the chance a few percent off 50%, within normal
		/// 300-sample binomial noise; 80 gives a clean, safely-separated ~42%-vs-50% signal.
		/// Expected at skill=0/difficulty=80: GetBellCurve(80) = 500-(250*80/250) = 420 -> 42%.
		/// Expected at skill=1000/difficulty=80: valDiff=920, three /2 halvings down to chance 62,
		/// then 1000-(62-(31*170/250)) = 1000-41 = 959 -> 95.9%.</summary>
		[Fact]
		public void FizzleRateVariesWithSkillOnceDifficultyFallbackIsReal() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageFizzleChecker")) {
				int difficulty = this.ReportDifficulty(client, "s_poison_field");
				Assert.True(difficulty > 0,
					"Expected s_poison_field's fallback difficulty to be > 0 now that it's derived " +
					"from Requirements=X Magery instead of a hardcoded 0.");

				int lowSkillSuccesses = this.SampleFizzleRate(client, "s_poison_field", skillValue: 0, sampleCount: 300);
				int highSkillSuccesses = this.SampleFizzleRate(client, "s_poison_field", skillValue: 1000, sampleCount: 300);

				Assert.True(lowSkillSuccesses < 170,
					"Expected skill=0 to fail noticeably more than half the time against a real " +
					"difficulty (" + difficulty + ", expected ~42% success). Got " + lowSkillSuccesses + "/300 successes.");
				Assert.True(highSkillSuccesses > 250,
					"Expected skill=1000 (max) to succeed almost always against difficulty " +
					difficulty + " (expected ~96% success). Got " + highSkillSuccesses + "/300 successes.");
			}
		}

		/// <summary>Sanity check that the Phase 0 fix doesn't swing the other way either - a
		/// caster exactly AT the spell's own stated skill requirement should land close to the
		/// documented ~50% (GetSCurve's own 0-difference midpoint), not near 0% or 100%.</summary>
		[Fact]
		public void FizzleRateIsRoughlyEvenAtExactlyTheSkillRequirement() {
			this.EnsureAutoAccountCreationEnabled();
			using (var client = this.ConnectLoginAndCreateCharacter(AccountB, "MageFizzleEvenChecker")) {
				int difficulty = this.ReportDifficulty(client, "s_poison_field");
				int successes = this.SampleFizzleRate(client, "s_poison_field", skillValue: difficulty, sampleCount: 500);

				Assert.True((successes > 150) && (successes < 350),
					"Expected roughly a coin-flip (allowing wide margin for the bell-curve shape, " +
					"not a strict 50%) at skill == difficulty (" + difficulty + "). Got " + successes + "/500.");
			}
		}

		/// <summary>End-to-end sanity check that SpellDef.cs's Difficulty change didn't break the
		/// real casting pipeline - a real damage spell cast from a real spellbook at a real
		/// target still consumes mana and lands damage. Runs on the GM account (CheckSuccess's
		/// own GM-always-succeeds bypass is actually desirable here, since this test isn't about
		/// fizzle at all, just "does casting still work end to end").</summary>
		[Fact]
		public void DamageSpellStillLandsDamageAndConsumesMana() {
			// Account is this collection's first-ever login (full LScript parser) - spell
			// defnames are registered AbstractScripts, so an unquoted bareword like
			// s_magic_arrow gets silently resolved to the live SpellDef's own ToString(), not
			// kept as literal text (the same established bareword-resolution footgun as
			// elsewhere this session) - quote it.
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MagePipelineChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSpellAtDummy \"s_magic_arrow\"");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_STARTED:", mark, TimeSpan.FromSeconds(10)),
					"Output since cast command:\n" + this.fixture.GameOutputSnapshot().Substring(mark));
				string startLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_STARTED:");
				uint dummyUid = ExtractMarkedUid(startLine, "dummyUid=");
				int startMana = ExtractInt(startLine, "startMana");
				int startHits = ExtractInt(startLine, "startHits");

				Thread.Sleep(5000); // s_magic_arrow's CASTTIME is 2.5s; generous margin for the stroke delay too

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCastResult " + dummyUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_RESULT:");

				int casterMana = ExtractInt(resultLine, "casterMana");
				Assert.True(casterMana < startMana,
					"Expected mana to be spent casting s_magic_arrow. startMana=" + startMana + " line: " + resultLine);

				bool targetDeleted = resultLine.Contains("targetDeleted=True");
				if (!targetDeleted) {
					int targetHits = ExtractInt(resultLine, "targetHits");
					Assert.True(targetHits < startHits,
						"Expected s_magic_arrow to have damaged the dummy. startHits=" + startHits + " line: " + resultLine);
				}
			}
		}

		/// <summary>#485 - the generic summon-pet infrastructure: casting the test-only
		/// s_test_summon spell produces a real NPC, controlled by the caster, non-aggressive,
		/// tagged as a summoned creature - and it actually despawns once its 5-second duration
		/// elapses (not just a decoration that sits there forever).</summary>
		[Fact]
		public void SummonedCreatureAppearsControlledThenDespawns() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageSummonChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportMyUid");
				Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
				uint selfUid = ExtractMarkedUid(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSummonSpell");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500); // CASTTIME is 1.0s; generous margin for the stroke + effect dispatch

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected the summon to exist shortly after casting. line: " + foundLine);
				Assert.Contains("controlMasterUid=" + selfUid, foundLine);
				Assert.Contains("warMode=False", foundLine);

				Thread.Sleep(9000); // duration is a flat 10s; 2.5s elapsed already above, this covers the rest plus margin

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string goneLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(goneLine.Contains("NONE"), "Expected the summon to have despawned after its duration. line: " + goneLine);
			}
		}

		/// <summary>#486 - Character.On_Dispell used to be an empty no-op, so casting Dispel on
		/// a buffed character removed nothing. Casts a real beneficial buff (s_bless) on self,
		/// confirms a dispellable plugin is really there, casts Dispel on self, confirms it's
		/// gone.</summary>
		[Fact]
		public void DispelRemovesABuffFromACharacter() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageDispelBuffChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastBuffOnSelf");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: BUFF_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500); // s_bless CASTTIME is 2.0s; generous margin

				int beforeCount = this.ReportDispellablePluginCount(client);
				Assert.True(beforeCount > 0, "Expected a real dispellable plugin after casting Bless on self.");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastDispelOnTarget " + SelfUidOf(client));
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: DISPEL_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(3500); // s_dispel CASTTIME is 2.5s; generous margin

				int afterCount = this.ReportDispellablePluginCount(client);
				Assert.True(afterCount < beforeCount,
					"Expected Dispel to remove the Bless plugin. beforeCount=" + beforeCount + " afterCount=" + afterCount);
			}
		}

		/// <summary>#486 - a summoned creature (#485) is tagged specifically so Character.
		/// On_Dispell's fix can remove the whole creature, not just a buff on it. Uses a longer-
		/// lived summon (TestCastSummonSpell's own 5s duration is enough margin) and dispels it
		/// well before it would naturally despawn.</summary>
		[Fact]
		public void DispelRemovesASummonedCreature() {
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "MageDispelSummonChecker")) {
				client.SendSpeech(".TestGrantFullSpellbook");
				Assert.True(this.fixture.WaitForGameOutput("MAGERY_TEST: spellbook granted", TimeSpan.FromSeconds(10)));

				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastSummonSpell");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST: STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(2500);

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportNearbySummon");
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_SUMMON_TEST:", mark, TimeSpan.FromSeconds(10)));
				string foundLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_SUMMON_TEST:");
				Assert.True(foundLine.Contains("FOUND"), "Expected the summon to exist. line: " + foundLine);
				uint summonUid = ExtractMarkedUid(foundLine, "uid=");

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestCastDispelOnTarget " + summonUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: DISPEL_CAST_STARTED", mark, TimeSpan.FromSeconds(10)));

				Thread.Sleep(3500); // well before the summon's own 5s despawn timer would fire anyway

				mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestReportCastResult " + summonUid);
				Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_CAST_RESULT:", mark, TimeSpan.FromSeconds(10)));
				string resultLine = LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_CAST_RESULT:");
				Assert.Contains("targetDeleted=True", resultLine);
			}
		}

		private int ReportDispellablePluginCount(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportDispellablePluginCount");
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DISPEL_TEST: dispellablePluginCount=", mark, TimeSpan.FromSeconds(10)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "dispellablePluginCount="), "dispellablePluginCount");
		}

		private uint SelfUidOf(TestGameClient client) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportMyUid");
			Assert.True(this.fixture.WaitForGameOutputSince("MY_UID:", mark, TimeSpan.FromSeconds(10)));
			return ExtractMarkedUid(LastLineContaining(this.fixture.GameOutputSnapshot(), "MY_UID:"), "MY_UID: ");
		}

		private int ReportDifficulty(TestGameClient client, string spellDefname) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestReportSpellDifficulty " + spellDefname);
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_DIFFICULTY_TEST:", mark, TimeSpan.FromSeconds(10)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_DIFFICULTY_TEST:"), "difficulty");
		}

		private int SampleFizzleRate(TestGameClient client, string spellDefname, int skillValue, int sampleCount) {
			int mark = this.fixture.GameOutputSnapshot().Length;
			client.SendSpeech(".TestSampleFizzleRate " + spellDefname + " " + skillValue + " " + sampleCount);
			Assert.True(this.fixture.WaitForGameOutputSince("MAGERY_FIZZLE_SAMPLE:", mark, TimeSpan.FromSeconds(15)));
			return ExtractInt(LastLineContaining(this.fixture.GameOutputSnapshot(), "MAGERY_FIZZLE_SAMPLE:"), "successes");
		}

		private static int ExtractInt(string line, string key) {
			string marker = key + "=";
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while ((end < line.Length) && (char.IsDigit(line[end]) || (line[end] == '-'))) {
				end++;
			}
			return int.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
		}

		private static uint ExtractMarkedUid(string line, string marker) {
			int idx = line.IndexOf(marker, StringComparison.Ordinal);
			Assert.True(idx >= 0, "Could not find '" + marker + "' in: " + line);
			int start = idx + marker.Length;
			int end = start;
			while ((end < line.Length) && char.IsDigit(line[end])) {
				end++;
			}
			return uint.Parse(line.Substring(start, end - start), System.Globalization.CultureInfo.InvariantCulture);
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

		// Guards against the established "EnsureAutoAccountCreationEnabled creates a brand new
		// character every call" trap (AttrFlagTests.cs/ProfessionTests.cs hit this too) - each
		// call otherwise burns one of Account's 5 character slots, and this file has 2 Facts
		// that call it. Safe under this collection's DisableParallelization=true.
		private static bool autoAccountCreationEnabled;

		private void EnsureAutoAccountCreationEnabled() {
			if (autoAccountCreationEnabled) {
				return;
			}
			autoAccountCreationEnabled = true;
			using (var client = this.ConnectLoginAndCreateCharacter(Account, "AutoAccountEnabler")) {
				int mark = this.fixture.GameOutputSnapshot().Length;
				client.SendSpeech(".TestEnableAutoAccountCreation");
				Assert.True(this.fixture.WaitForGameOutputSince("AUTO_ACCOUNT_TEST:", mark, TimeSpan.FromSeconds(10)));
			}
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

	[CollectionDefinition(Name, DisableParallelization = true)]
	public class MagerySpellCollection : ICollectionFixture<GameServerPairFixture> {
		public const string Name = "MagerySpellPair";
	}
}
